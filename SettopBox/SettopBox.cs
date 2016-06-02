using System.Collections.Generic;
using System.Diagnostics;
using log4net;
using SharedComponents.Module;

namespace SettopBox
{
    public class SettopBox
    {
        readonly ILog _logger;
        readonly Settings _settings;
        readonly IEnumerable<IModule> _modules;
        readonly ModuleCommunication _moduleCommunication;
        public SettopBox(ILog logger, Settings settings, IEnumerable<IModule> modules, ModuleCommunication moduleCommunication)
        {
            _logger = logger;
            _settings = settings;
            _modules = modules;
            _moduleCommunication = moduleCommunication;
        }

        public void Stop()
        {
            foreach (var module in _modules)
            {
                _logger.Info($"Stop {module.Name}");
                _moduleCommunication.UnRegister(module);
                module.Stop();
                _logger.Debug($"Stopped {module.Name}");
            }
            _logger.Info("Bye bye");
        }
        public void Start()
        {
            _logger.Info($"Welcome to Settopbox ({Process.GetCurrentProcess().Id})");
            _settings.Load();
            foreach (var module in _modules)
            {
                if (_settings.GetModule(module.Name)) Start(module);
                else Disable(module);
                _moduleCommunication.Register(module);
            }
        }

        void Disable(IModule module)
        {
            _logger.Info($"{module.Name} disabled");
            module.Disable();
        }

        void Start(IModule module)
        {
            _logger.Info($"Start {module.Name}");
            module.Start();
        }
    }
}