using System;
using System.Collections.Generic;
using System.Diagnostics;
using log4net;
using SharedComponents.DependencyInjection;
using SharedComponents.Module;

namespace SettopBox
{
    class Program
    {
        readonly ILog _logger;
        readonly Settings _settings;
        readonly IEnumerable<IModule> _modules;
        readonly ModuleCommunication _moduleCommunication;

        public Program(ILog logger, Settings settings, IEnumerable<IModule> modules, ModuleCommunication moduleCommunication)
        {
            _logger = logger;
            _settings = settings;
            _modules = modules;
            _moduleCommunication = moduleCommunication;
        }
        static void Main()
        {
            var container = SharedContainer.CreateAndFill<DependencyConfig, 
                                                          NewCamd.DependencyConfig, 
                                                          Keyblock.DependencyConfig,
                                                          RunAndMonitor.DependencyConfig,
                                                          EpgGrabber.DependencyConfig,
                                                          WebUi.DependencyConfig>("Log4net.config");
            var prog = container.GetInstance<Program>();
            prog.Start();
            Console.WriteLine("Hit 'Enter' to exit");
            Console.ReadLine();
            prog.Stop();
            container.Dispose();
            Environment.Exit(0);
        }

        void Stop()
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
        void Start()
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
