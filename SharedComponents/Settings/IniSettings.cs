using System;
using System.IO;
using System.Reflection;
using System.Text;
using log4net;

namespace SharedComponents.Settings
{
    public abstract class IniSettings
    {
        const BindingFlags PropertyFlags = BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.Public;
        protected readonly ILog Logger;
        const string Filename = "Settings.ini";
        abstract protected string Name { get; }

        protected IniSettings()
        {
            Logger = LogManager.GetLogger(GetType());
        }

        //Decode if ini contains flag
        const string DecodeFlag = "-- Encoded from here --";
        readonly Func<string, string> _noDecode = value => value;
        readonly Func<string, string> _decode = value => string.IsNullOrWhiteSpace(value) ? value : Encoding.UTF8.GetString(Convert.FromBase64String(value));
        Func<string, string> _decoder;

        public virtual void Load()
        {
            Logger.Info($"Read {Filename}");
            _decoder = _noDecode;
            try
            {
                var inCorrectBlock = false;
                using (var reader = new StreamReader(Filename))
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        if (line.StartsWith("#", StringComparison.Ordinal)) continue;
                        if (line.StartsWith("[", StringComparison.Ordinal))
                        {
                            inCorrectBlock = line.Equals($"[{Name}]");
                            Logger.Debug($"Found block header {line} and this headerblock is correct: {inCorrectBlock}");
                            continue;
                        }
                        if(!inCorrectBlock) continue;
                        if (line.Equals(DecodeFlag))
                        {
                            _decoder = _decode;
                            continue;
                        }
                        Logger.DebugFormat("Process line {0}", line);
                        ReadConfigItem(line);
                    }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to load the configuration", ex);
            }
        }


        protected void Save()
        {
            Logger.Info($"Save {Filename}");
            try
            {
                using (var writer = new StreamWriter(Filename))
                {
                    writer.WriteLine($"#[{Name}]");
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
                        Logger.Debug($"Write '{key}' with value '{value}' to disk");
                        writer.WriteLine($"{key}|{value}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to save the configuration", ex);
            }
        }

        void ReadConfigItem(string line)
        {
            var keyvalue = line.Split('|');
            if (keyvalue.Length < 2)
            {
                Logger.WarnFormat("Failed to read configuration line: {0}", line);
                return;
            }
            SetValue(keyvalue[0], _decoder(keyvalue[1]));
        }

        void SetValue(string key, string value)
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
    }
}