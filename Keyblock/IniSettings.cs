using System;
using System.IO;
using System.Reflection;
using System.Text;
using log4net;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Keyblock
{
    public class IniSettings
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(IniSettings));
        readonly Random _random = new Random();

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

        //Decode if ini contains flag
        private const string DecodeFlag = "-- Encoded from here --";
        private readonly Func<string, string> _noDecode = value => value;
        private readonly Func<string, string> _decode = value => string.IsNullOrWhiteSpace(value) ? value : Encoding.UTF8.GetString(Convert.FromBase64String(value));
        private Func<string, string> _decoder;

        public void Load()
        {
            Logger.Info("Read Keyblock.ini");
            _decoder = _noDecode;
            try
            {
                using (var reader = new StreamReader("Keyblock.ini"))
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        if (line.StartsWith("#")) continue;
                        if (line.Equals(DecodeFlag))
                        {
                            _decoder = _decode;
                            continue;
                        }
                        Logger.DebugFormat("Process line {0}", line);
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

        void GenerateMachineId()
        {
            Logger.Debug("No MachineId found, generating MachineID");
            var buf = new byte[20];
            _random.NextBytes(buf);
            MachineId = Convert.ToBase64String(buf);
            Logger.Debug($"Your MachineID is: {MachineId}");
        }

        void GenerateClientId()
        {
            Logger.Debug("No ClientId found, generating ClientId");
            var buf = new byte[28];
            _random.NextBytes(buf);
            ClientId = string.Empty;
            foreach (var b in buf)
            {
                ClientId += (b & 0xFF).ToString("X2");
            }
            Logger.Debug($"Your ClientID is: {ClientId}");
        }


        public void Save()
        {
            Logger.Info("Save Keyblock.ini");
            try
            {
                using (var writer = new StreamWriter("Keyblock.ini"))
                {
                    writer.WriteLine("#[Keyblock.ini]");
                    writer.WriteLine(DecodeFlag);
                    var properties = GetType().GetProperties(PropertyFlags);
                    foreach (var property in properties)
                    {
                        var key = property.Name;
                        var value = property.GetValue(this);
                        if (value == null)
                        {
                            Logger.Debug($"Key '{key}' has no value, skip writing to ini file");
                            continue;
                        }
                        var converted = Convert.ToBase64String(Encoding.UTF8.GetBytes(value.ToString()));
                        Logger.Debug($"Write '{key}' with value '{value}' to disk as '{converted}'");
                        writer.WriteLine($"{key}|{converted}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to save the configuration", ex);
            }
        }

        private void ReadConfigItem(string line)
        {
            var keyvalue = line.Split('|');
            if (keyvalue.Length < 2)
            {
                Logger.WarnFormat("Failed to read configuration line: {0}", line);
                return;
            }
            SetValue(keyvalue[0], _decoder(keyvalue[1]));
        }

        private void SetValue(string key, string value)
        {
            var propertyInfo = GetType().GetProperty(key, PropertyFlags);
            if (propertyInfo == null)
            {
                Logger.WarnFormat("Unknown configuration key: {0}", key);
                return;
            }
            try
            {
                Logger.DebugFormat("Read configuration item {0} with value {1}", key, value);
                propertyInfo.SetValue(this, Convert.ChangeType(value, propertyInfo.PropertyType), null);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to read {key} into {value} as {propertyInfo.PropertyType}", ex);
            }
        }

        private const BindingFlags PropertyFlags = BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.Public;
    }
}
