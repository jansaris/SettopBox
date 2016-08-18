using System;
using System.Collections.Generic;
using System.Diagnostics;
using log4net;
using SharedComponents.Helpers;
using SharedComponents.Module;

namespace SettopBox
{
    public class SettopBox
    {
        readonly ILog _logger;
        readonly IThreadHelper _threadHelper;
        readonly Settings _settings;
        readonly IEnumerable<IModule> _modules;
        readonly LinuxSignal _signal;

        public SettopBox(ILog logger, IThreadHelper threadHelper, Settings settings, IEnumerable<IModule> modules, LinuxSignal signal)
        {
            _logger = logger;
            _threadHelper = threadHelper;
            _settings = settings;
            _modules = modules;
            _signal = signal;
        }

        public void Start()
        {
            _logger.Info($"Welcome to Settopbox ({Process.GetCurrentProcess().Id})");
            _signal.Exit += Stop;
            _settings.Load();
            foreach (var module in _modules)
            {
                if (_settings.GetModule(module.Name)) Start(module);
                else Disable(module);
            }
        }

        void Stop(object sender, EventArgs e)
        {
            _signal.Exit -= Stop;
            _logger.Info("Bye bye");
        }

        void Disable(IModule module)
        {
            _logger.Info($"{module.Name} disabled");
            module.Disable();
        }

        void Start(IModule module)
        {
            _logger.Info($"Start {module.Name}");
            _threadHelper.RunSafeInNewThread(module.Start, _logger);
        }
    }
}