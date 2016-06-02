using System;
using System.IO;
using System.Reflection;
using SharedComponents.DependencyInjection;
using Topshelf;

namespace SettopBox
{
    class Program
    {
        SettopBox _settopBox;

        static void Main()
        {
            HostFactory.Run(x =>                                 
            {
                x.Service<Program>(s =>                        
                {
                    s.ConstructUsing(name => new Program());     
                    s.WhenStarted(tc => tc.Start());              
                    s.WhenStopped(tc => tc.Stop());               
                });
                x.UseLog4Net();

                x.SetDescription(".Net SettopBox");       
                x.SetDisplayName("SettopBox");
                x.SetServiceName("settopbox");
            });
        }

        void Start()
        {
            Directory.SetCurrentDirectory(AssemblyDirectory);
            var container = SharedContainer.CreateAndFill<DependencyConfig,
                                                          NewCamd.DependencyConfig,
                                                          Keyblock.DependencyConfig,
                                                          RunAndMonitor.DependencyConfig,
                                                          EpgGrabber.DependencyConfig,
                                                          TvHeadendIntegration.DependencyConfig,
                                                          WebUi.DependencyConfig>("Log4net.config");
            _settopBox = container.GetInstance<SettopBox>();
            _settopBox.Start();
        }

        void Stop()
        {
            _settopBox?.Stop();
        }


        static string AssemblyDirectory
        {
            get
            {
                var codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var uri = new UriBuilder(codeBase);
                var path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
    }
}
