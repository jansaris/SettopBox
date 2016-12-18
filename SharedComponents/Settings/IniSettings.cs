using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        static readonly object SyncRoot = new object();

        protected IniSettings(ILog logger)
        {
            Logger = logger;
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
            var atLeastOneLineOfTheBlockFound = false;
            lock (SyncRoot)
            {
                try
                {
                    var correctBlock = false;
                    using (var reader = new StreamReader(Filename))
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            line = line.Trim();
                            if (line.StartsWith("#", StringComparison.Ordinal)) continue;
                            if (line.StartsWith("[", StringComparison.Ordinal))
                            {
                                correctBlock = line.Equals($"[{Name}]");
                                Logger.Debug($"Found block header {line} and this headerblock is correct: {correctBlock}");
                                continue;
                            }
                            if (!correctBlock) continue;
                            if (line.Equals(DecodeFlag))
                            {
                                _decoder = _decode;
                                continue;
                            }
                            Logger.Debug($"Process line {line}");
                            atLeastOneLineOfTheBlockFound = true;
                            ReadConfigItem(line);
                        }

                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to load the configuration", ex);
                }
            }
            if (!atLeastOneLineOfTheBlockFound)
            {
                Save();
            }
        }

        public void Save()
        {
            Logger.Info($"Save {Filename}");
            try
            {
                
                lock (SyncRoot)
                {
                    //Read current ini file
                    var iniLines = File.ReadAllLines(Filename).ToList();
                    //Generate lines for this block of the ini file
                    var configLines = ConfigurationLines();
                    //Find the index of this block
                    var index = iniLines.IndexOf(configLines.First());
                    if (index == -1)
                    {
                        //Just add the lines to the end of the ini file
                        iniLines.AddRange(configLines);
                    }
                    else
                    {
                        //Determine next block
                        var nextBlock = iniLines.FindIndex(index + 1, line => line.StartsWith("[", StringComparison.Ordinal));
                        //If no next block found, then just hit the full count, so nothing will end up in after
                        if (nextBlock == -1) nextBlock = iniLines.Count;

                        //Generate new ini file lines
                        var before = iniLines.Take(index);
                        var after = iniLines.Skip(nextBlock);
                        iniLines = before.Concat(configLines).Concat(after).ToList();
                    }
                    //Write everything to disk
                    File.WriteAllLines(Filename, iniLines);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to save the configuration", ex);
            }
        }

        private List<string> ConfigurationLines()
        {
            var list = new List<string> {$"[{Name}]"};

            var properties = GetType().GetProperties(PropertyFlags);
            foreach (var property in properties)
            {
                var key = property.Name;
                var value = property.GetValue(this);
                if (value == null)
                {
                    Logger.Debug($"Key '{key}' has no value, skip saving it to ini file");
                    continue;
                }
                Logger.Debug($"Save '{key}' with value '{value}' in ini file");
                list.Add($"{key}|{value}");
            }

            return list;
        }

        public List<Setting> GetAll()
        {
            var properties = GetType().GetProperties(PropertyFlags);
            return properties.Select(p => new Setting
            {
                Name = p.Name,
                Value = p.GetValue(this),
                Type = p.PropertyType.Name
            }).ToList();
        }

        void ReadConfigItem(string line)
        {
            var keyvalue = line.Split('|');
            if (keyvalue.Length < 2)
            {
                Logger.Warn($"Failed to read configuration line: {line}");
                return;
            }
            SetValue(keyvalue[0], _decoder(keyvalue[1]));
        }

        public object GetValue(string key)
        {
            var propertyInfo = GetType().GetProperty(key, PropertyFlags);
            if (propertyInfo == null)
            {
                Logger.Warn($"Unknown configuration key: {key}");
                return null;
            }
            try
            {
                Logger.Debug($"Read configuration item {key}");
                return propertyInfo.GetValue(this);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to read {key}", ex);
                return null;
            }
        }

        public void SetValue(string key, string value)
        {
            var propertyInfo = GetType().GetProperty(key, PropertyFlags);
            if (propertyInfo == null)
            {
                Logger.Warn($"Unknown configuration key: {key}");
                return;
            }
            try
            {
                Logger.Info($"Set configuration item {key} with value {value}");
                propertyInfo.SetValue(this, Convert.ChangeType(value, propertyInfo.PropertyType), null);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to set {key} into {value} as {propertyInfo.PropertyType}", ex);
            }
        }
    }
}