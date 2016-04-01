using System;
using System.Collections.Generic;
using System.Linq;
using SharedComponents.Settings;

namespace SettopBox
{
    public class Settings : IniSettings
    {
        protected override string Name => "SettopBox";

        readonly Dictionary<string, bool> _modules = new Dictionary<string, bool>();

        public bool NewCamd
        {
            get { return GetModule(nameof(NewCamd)); }
            set { UpdateModule(nameof(NewCamd), value); }
        }

        public bool Keyblock {
            get { return GetModule(nameof(Keyblock)); }
            set { UpdateModule(nameof(Keyblock), value); }
        }

        public bool WebUi
        {
            get { return GetModule(nameof(WebUi)); }
            set { UpdateModule(nameof(WebUi), value); }
        }

        public IReadOnlyList<Tuple<string, bool>> GetModules()
        {
            return _modules.Select(m => new Tuple<string, bool>(m.Key, m.Value)).ToList();
        }

        public bool GetModule(string module)
        {
            if(!_modules.ContainsKey(module)) UpdateModule(module, true);
            return _modules[module];
        }

        void UpdateModule(string module, bool enabled)
        {
            if (!_modules.ContainsKey(module)) _modules.Add(module, enabled);
            else _modules[module] = enabled;
        }
    }
}