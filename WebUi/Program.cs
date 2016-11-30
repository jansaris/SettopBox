using System;
using System.IO;
using System.Web.Http;
using log4net;
using Microsoft.Owin.FileSystems;
using SharedComponents.DependencyInjection;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.StaticFiles;
using Owin;
using SharedComponents.Helpers;
using SharedComponents.Module;
using SimpleInjector;
using SimpleInjector.Integration.WebApi;

namespace WebUi
{
    class Program : BaseModule
    {
        readonly Settings _settings;
        readonly Container _container;
        readonly ILog _logger;
        IDisposable _host;

        public Program(ILog logger, Settings settings, Container container, LinuxSignal signal, ModuleCommunication communication) : base(signal, communication)
        {
            _logger = logger;
            _settings = settings;
            _container = container;
        }

        static void Main()
        {
            var container = SharedContainer.CreateAndFill<DependencyConfig>("Log4net.config");
            var prog = container.GetInstance<Program>();

            prog.Start();
            Console.WriteLine("Hit 'Enter' to exit");
            Console.ReadLine();
            prog.Stop();
            container.Dispose();
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
            var folder = new DirectoryInfo(_settings.WwwRootFolder);
            _logger.Info($"Use public www root: {folder.FullName}");
            var physicalFileSystem = new PhysicalFileSystem(folder.FullName);
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
            config.EnableCors();
            config.DependencyResolver = new SimpleInjectorWebApiDependencyResolver(_container);
            config.MapHttpAttributeRoutes();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
            config.EnsureInitialized();
            return config;
        }

        protected override void StopModule()
        {
            try
            {
                _logger.Info("Exit WebUi");
                _host?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to stop the Web interface");
                _logger.Debug("Exception", ex);
            }
        }
    }
}
