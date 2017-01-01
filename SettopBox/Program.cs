using System;
using System.IO;
using System.Reflection;
using log4net;
using SharedComponents.DependencyInjection;
using SharedComponents.Helpers;

namespace SettopBox
{
    class Program
    {
        static readonly ILog Logger = LogManager.GetLogger("SettopBox");

        static void Main()
        {
            Directory.SetCurrentDirectory(AssemblyDirectory);
            var container = SharedContainer.CreateAndFill<DependencyConfig,
                                                          NewCamd.DependencyConfig,
                                                          Keyblock.DependencyConfig,
                                                          RunAndMonitor.DependencyConfig,
                                                          ChannelList.DependencyConfig,
                                                          EpgGrabber.DependencyConfig,
                                                          TvHeadendIntegration.DependencyConfig,
                                                          WebUi.DependencyConfig>("Log4net.config");

            var settopBox = container.GetInstance<SettopBox>();
            var signal = container.GetInstance<LinuxSignal>();
            var pid = container.GetInstance<PidMonitor>();
            if (pid.Start())
            {
                Run(settopBox, signal);
                pid.Stop();
            }
            signal.Dispose();
            container.Dispose();
        }

        static void Run(SettopBox settopBox, LinuxSignal signal)
        {
            settopBox.Start();
            if (Console.IsInputRedirected)
            {
                Logger.Info("Wait for kill-signal");
                signal.WaitForListenThreadToComplete();
            }
            else
            {
                Logger.Info("Wait for keyboard input");
                Console.WriteLine("Press enter to exit");
                Console.ReadLine();
            }
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
