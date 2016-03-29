using System;
using log4net;
using SharedComponents.DependencyInjection;
using Microsoft.Owin.Hosting;
using Owin;
using SimpleInjector;

namespace WebUi
{
    class Program : IDisposable
    {
        readonly Settings _settings;
        readonly Container _container;
        readonly ILog _logger;
        IDisposable _host;

        public Program(ILog logger, Settings settings, Container container)
        {
            _settings = settings;
            _container = container;
            _logger = logger;

        }

        static void Main(string[] args)
        {
            var container = SharedContainer.CreateAndFill<DependencyConfig>("Log4net.config");
            var prog = container.GetInstance<Program>();

            prog.Start();
            Console.WriteLine("Hit 'Enter' to exit");
            Console.ReadLine();
            prog.Stop();
        }

        void Start()
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

        void Stop()
        {
            try
            {
                _logger.Info("Exit WebUi");
                Dispose();
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to stop the Web interface");
                _logger.Debug("Exception", ex);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        bool _disposing;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || _disposing) return;
            _disposing = true;
            _host.Dispose();
        }
    }
}
