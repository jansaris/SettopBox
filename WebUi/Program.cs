using System;
using System.Web.Http;
using log4net;
using Microsoft.Owin.FileSystems;
using SharedComponents.DependencyInjection;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.StaticFiles;
using Owin;
using SharedComponents.Module;
using SimpleInjector;
using SimpleInjector.Integration.WebApi;
using WebUi.api.Logging;

namespace WebUi
{
    class Program : BaseModule
    {
        readonly Settings _settings;
        readonly Container _container;
        readonly InMemoryLogger _logAppender;
        readonly ILog _logger;
        IDisposable _host;

        public Program(ILog logger, Settings settings, Container container, InMemoryLogger logAppender)
        {
            _logger = logger;
            _settings = settings;
            _container = container;
            _logAppender = logAppender;
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

        public override IModuleInfo GetModuleInfo()
        {
            return null;
        }

        protected override void StartModule()
        {
            try
            {
                _settings.Load();
                var uri = $"http://{_settings.Host}:{_settings.Port}";
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
            app.UseWebApi(GenerateHttpConfiguration());
            app.UseFileServer(GenerateFileServerConfig());
        }

        FileServerOptions GenerateFileServerConfig()
        {
            //var physicalFileSystem = new PhysicalFileSystem(@"./www");
            var physicalFileSystem = new PhysicalFileSystem(@"F:\GitHub\SettopBox\WebUi\www");
            //var physicalFileSystem = new PhysicalFileSystem(@"C:\Users\Jan\Documents\GitHub\SettopBox\WebUi\www");
            var options = new FileServerOptions
            {
                EnableDefaultFiles = true,
                FileSystem = physicalFileSystem
            };
            options.StaticFileOptions.FileSystem = physicalFileSystem;
            options.StaticFileOptions.ServeUnknownFileTypes = true;
            options.DefaultFilesOptions.DefaultFileNames = new[]
            {
                "index.html"
            };

            return options;
        }

        HttpConfiguration GenerateHttpConfiguration()
        {
            var config = new HttpConfiguration();
            config.DependencyResolver = new SimpleInjectorWebApiDependencyResolver(_container);
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
            return config;
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
