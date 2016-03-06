using System;
using log4net;
using SharedComponents.DependencyInjection;

namespace NewCamd
{
    class Program
    {
        readonly ILog _logger;
        readonly Settings _settings;

        public Program(ILog logger, Settings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        static void Main()
        {
            var container = SharedContainer.CreateAndFill<DependencyConfig>("Log4net.config");
            var prog = container.GetInstance<Program>();
            prog.Run();
        }

        void Run()
        {
            try
            {
                _logger.Info("Welcome to Keyblock");
                _settings.Load();
                StartServer();
                _logger.Info("Done");
            }
            catch (Exception ex)
            {
                _logger.Fatal("An unhandled exception occured", ex);
            }
        }

        void StartServer()
        {
            throw new NotImplementedException();
        }
    }
}
