using System;
using log4net;
using SharedComponents.DependencyInjection;
using Microsoft.Owin.Hosting;
using Owin;
using SharedComponents;
using SimpleInjector;

namespace WebUi
{
    class Program : BaseModule
    {
        readonly Settings _settings;
        readonly Container _container;
        readonly ModuleInformation _info;
        readonly ILog _logger;
        IDisposable _host;

        public Program(ILog logger, Settings settings, Container container, ModuleInformation info)
        {
            _logger = logger;
            _settings = settings;
            _container = container;
            _info = info;
        }

        static void Main()
        {
            var container = SharedContainer.CreateAndFill<DependencyConfig>("Log4net.config");
            var prog = container.GetInstance<Program>();

            prog.Start();
            Console.WriteLine("Hit 'Enter' to exit");
            Console.ReadLine();
            prog.Stop();
        }

        protected override void StartModule()
        {
            try
            {
                var uri = $"http://localhost:{_settings.Port}";
                _logger.Info($"Start WebUi at {uri}");
                _host = WebApp.Start(uri, StartWeb);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to start the Web interface");
                _logger.Debug("Exception", ex);
            }
        }

        void StartWeb(IAppBuilder app)
        {
            app.UseOwinContextInjector(_container);
            app.UseNancy();
        }

        protected override void StopModule()
        {
            try
            {
                _logger.Info("Exit WebUi");
                _host.Dispose();
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to stop the Web interface");
                _logger.Debug("Exception", ex);
            }
        }
    }
}
