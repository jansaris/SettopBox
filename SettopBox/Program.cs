using System;
using log4net;
using SharedComponents.DependencyInjection;
using SimpleInjector;

namespace SettopBox
{
    class Program
    {
        readonly ILog _logger;
        readonly Settings _settings;
        readonly NewCamd.Program _newCamd;
        readonly Keyblock.Program _keyblock;

        public Program(ILog logger, Settings settings, NewCamd.Program newCamd, Keyblock.Program keyblock)
        {
            _logger = logger;
            _settings = settings;
            _newCamd = newCamd;
            _keyblock = keyblock;
        }
        static void Main()
        {
            var container = SharedContainer.CreateAndFill<DependencyConfig, NewCamd.DependencyConfig, Keyblock.DependencyConfig>("Log4net.config");
            var prog = container.GetInstance<Program>();
            prog.Start();
            Console.WriteLine("Hit 'Enter' to exit");
            Console.ReadLine();
            prog.Stop();
        }

        void Stop()
        {
            if (_settings.NewCamdEnabled)
            {
                _newCamd.Stop();
            }
        }

        void Start()
        {
            _logger.Info("Welcome to Settopbox");
            _settings.Load();
            if (_settings.KeyblockEnabled)
            {
                StartKeyBlock();
            }
            if (_settings.NewCamdEnabled)
            {
                StartNewCamd();
            }
        }

        void StartNewCamd()
        {
            _logger.Info("Start NewCamd");
            _newCamd.Start();
            _newCamd.Listen();
        }

        void StartKeyBlock()
        {
            _logger.Info("Start Keyblock");
            _keyblock.Run();
        }
    }
}
