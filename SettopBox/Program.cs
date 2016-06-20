using System;
using System.IO;
using System.Reflection;
using SharedComponents.DependencyInjection;
using SharedComponents.Module;
using SimpleInjector;

namespace SettopBox
{
    class Program
    {
        static void Main(string[] args)
        {
            Directory.SetCurrentDirectory(AssemblyDirectory);
            var container = SharedContainer.CreateAndFill<DependencyConfig,
                                                          NewCamd.DependencyConfig,
                                                          Keyblock.DependencyConfig,
                                                          RunAndMonitor.DependencyConfig,
                                                          EpgGrabber.DependencyConfig,
                                                          TvHeadendIntegration.DependencyConfig,
                                                          WebUi.DependencyConfig>("Log4net.config");
            var settopBox = container.GetInstance<SettopBox>();
            var signal = container.GetInstance<LinuxSignal>();
            settopBox.Start();
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
            signal.Dispose();
            container.Dispose();
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
