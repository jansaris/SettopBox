using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using SharedComponents.Settings;

namespace SettopBox
{
    public class Settings : IniSettings
    {
        public Settings(ILog logger) : base(logger)
        {
        }
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

        public bool RunAndMonitor
        {
            get { return GetModule(nameof(RunAndMonitor)); }
            set { UpdateModule(nameof(RunAndMonitor), value); }
        }

        public bool TvHeadendIntegration
        {
            get { return GetModule(nameof(TvHeadendIntegration)); }
            set { UpdateModule(nameof(TvHeadendIntegration), value); }
        }

        public bool EpgGrabber
        {
            get { return GetModule(nameof(EpgGrabber)); }
            set { UpdateModule(nameof(EpgGrabber), value); }
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