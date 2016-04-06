using System;
using System.Collections.Generic;
using log4net;
using SharedComponents;
using SharedComponents.DependencyInjection;
using SharedComponents.Module;

namespace SettopBox
{
    class Program
    {
        readonly ILog _logger;
        readonly Settings _settings;
        readonly IEnumerable<IModule> _modules;
        readonly ModuleInformation _moduleInformation;

        public Program(ILog logger, Settings settings, IEnumerable<IModule> modules, ModuleInformation moduleInformation)
        {
            _logger = logger;
            _settings = settings;
            _modules = modules;
            _moduleInformation = moduleInformation;
        }
        static void Main()
        {
            var container = SharedContainer.CreateAndFill<DependencyConfig, 
                                                          NewCamd.DependencyConfig, 
                                                          Keyblock.DependencyConfig,
                                                          WebUi.DependencyConfig>("Log4net.config");
            var prog = container.GetInstance<Program>();
            prog.Start();
            Console.WriteLine("Hit 'Enter' to exit");
            Console.ReadLine();
            prog.Stop();
        }

        void Stop()
        {
            foreach (var module in _modules)
            {
                _logger.Info($"Stop {module.Name}");
                _moduleInformation.UnRegister(module);
                module.Stop();
            }
        }

        void Start()
        {
            _logger.Info("Welcome to Settopbox");
            _settings.Load();
            foreach (var module in _modules)
            {
                if (_settings.GetModule(module.Name)) Start(module);
                else Disable(module);
                _moduleInformation.Register(module);
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
