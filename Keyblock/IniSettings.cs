using System;
using System.IO;
using System.Reflection;
using log4net;

namespace Keyblock
{
    public class IniSettings
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(IniSettings));

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

        public void Load()
        {
            Logger.Info("Read Keyblock.ini");
            try
            {
                using (var reader = new StreamReader("Keyblock.ini"))
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        if (line.StartsWith("#")) continue;
                        Logger.DebugFormat("Process line {0}", line);
                        ReadConfigItem(line);
                    }

            }
            catch (Exception ex)
            {
                throw new Exception("Failed to load the configuration", ex);
            }
        }

        private void ReadConfigItem(string line)
        {
            var keyvalue = line.Split('=');
            if (keyvalue.Length < 2)
            {
                Logger.WarnFormat("Failed to read configuration line: {0}", line);
                return;
            }
            SetValue(keyvalue[0], keyvalue[1]);
        }

        private void SetValue(string key, string value)
        {
            var propertyInfo = GetType().GetProperty(key, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.Public);
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
    }
}
