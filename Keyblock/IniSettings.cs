using System;
using System.IO;
using System.Reflection;
using System.Text;
using log4net;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Keyblock
{
    public class IniSettings
    {
        const BindingFlags PROPERTY_FLAGS = BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.Public;
        readonly ILog _logger;
        readonly Random _random;

        public string MachineId { get; set; }
        public string ClientId { get; set; }

        // Cert data
        public string Address { get; set; }
        public string ZipCode { get; set; }
        public string Country { get; set; }
        public string Province { get; set; }
        public string City { get; set; }
        public string Organization { get; set; }
        public string Common { get; set; }
        public string Telephone { get; set; }
        public string Email { get; set; }
        public string EmailHost { get; set; }
        public string ChallengePassword { get; set; }

        // Connection data
        public string VcasServer { get; set; }
        public string VksServer { get; set; }
        public int VcasPort { get; set; }
        public int VksPort { get; set; }

        // API data
        public string Company { get; set; }
        public string MessageFormat { get; set; }

        // Communication data
        public string DataFolder { get; set; } = "Data";
        public bool WriteAllCommunicationToDisk { get; set; } = false;
        public string CommunicationFolder { get; set; } = "Communication";
        public bool DontUseRealServerButMessagesFromDisk { get; set; } = false;
        public int MaxRetries { get; set; } = 3;
        public int WaitOnFailingBlockRetrievalInMilliseconds { get; set; } = 1000;

        //Decode if ini contains flag
        const string DECODE_FLAG = "-- Encoded from here --";
        readonly Func<string, string> _noDecode = value => value;
        readonly Func<string, string> _decode = value => string.IsNullOrWhiteSpace(value) ? value : Encoding.UTF8.GetString(Convert.FromBase64String(value));
        Func<string, string> _decoder;

        public IniSettings(ILog logger)
        {
            _logger = logger;
            _random = new Random();
        }

        public void Load()
        {
            _logger.Info("Read Keyblock.ini");
            _decoder = _noDecode;
            try
            {
                using (var reader = new StreamReader("Keyblock.ini"))
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        if (line.StartsWith("#")) continue;
                        if (line.Equals(DECODE_FLAG))
                        {
                            _decoder = _decode;
                            continue;
                        }
                        _logger.DebugFormat("Process line {0}", line);
                        ReadConfigItem(line);
                    }
                if (string.IsNullOrWhiteSpace(ClientId)) GenerateClientId();
                if (string.IsNullOrWhiteSpace(MachineId)) GenerateMachineId();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to load the configuration", ex);
            }
        }

        public void GenerateMachineId()
        {
            _logger.Debug("Generate new MachineID");
            var buf = new byte[10];
            _random.NextBytes(buf);
            MachineId = string.Empty;
            foreach (var b in buf)
            {
                MachineId += (b & 0xFF).ToString("x2");
            }
            MachineId = Convert.ToBase64String(Encoding.ASCII.GetBytes(MachineId));
            _logger.Debug($"Your MachineID is: {MachineId}");
        }

        public void GenerateClientId()
        {
            _logger.Debug("Generate new ClientId");
            var buf = new byte[28];
            _random.NextBytes(buf);
            ClientId = string.Empty;
            foreach (var b in buf)
            {
                ClientId += (b & 0xFF).ToString("x2");
            }
            _logger.Debug($"Your ClientID is: {ClientId}");
        }


        public void Save()
        {
            _logger.Info("Save Keyblock.ini");
            try
            {
                using (var writer = new StreamWriter("Keyblock.ini"))
                {
                    writer.WriteLine("#[Keyblock.ini]");
                    writer.WriteLine(DECODE_FLAG);
                    var properties = GetType().GetProperties(PROPERTY_FLAGS);
                    foreach (var property in properties)
                    {
                        var key = property.Name;
                        var value = property.GetValue(this);
                        if (value == null)
                        {
                            _logger.Debug($"Key '{key}' has no value, skip writing to ini file");
                            continue;
                        }
                        var converted = Convert.ToBase64String(Encoding.UTF8.GetBytes(value.ToString()));
                        _logger.Debug($"Write '{key}' with value '{value}' to disk as '{converted}'");
                        writer.WriteLine($"{key}|{converted}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to save the configuration", ex);
            }
        }
        public void EnsureDataFolderExists(string folder)
        {
            var directory = new DirectoryInfo(folder);
            if (directory.Exists) _logger.Debug($"Data folder '{directory.FullName}' exists");
            else
            {
                _logger.Debug($"Data folder '{directory.FullName}' doesn't exist, create it");
                directory.Create();
                _logger.Info($"Created data folder '{directory.FullName}'");
            }
        }

        void ReadConfigItem(string line)
        {
            var keyvalue = line.Split('|');
            if (keyvalue.Length < 2)
            {
                _logger.WarnFormat("Failed to read configuration line: {0}", line);
                return;
            }
            SetValue(keyvalue[0], _decoder(keyvalue[1]));
        }

        void SetValue(string key, string value)
        {
            var propertyInfo = GetType().GetProperty(key, PROPERTY_FLAGS);
            if (propertyInfo == null)
            {
                _logger.WarnFormat("Unknown configuration key: {0}", key);
                return;
            }
            try
            {
                _logger.DebugFormat("Read configuration item {0} with value {1}", key, value);
                propertyInfo.SetValue(this, Convert.ChangeType(value, propertyInfo.PropertyType), null);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to read {key} into {value} as {propertyInfo.PropertyType}", ex);
            }
        }
    }
}
