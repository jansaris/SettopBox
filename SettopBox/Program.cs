using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using log4net;
using SharedComponents.DependencyInjection;
using SharedComponents.Helpers;

namespace SettopBox
{
    class Program
    {
        static readonly string PidFile = Path.Combine(AssemblyDirectory, "SettopBox.pid");
        static readonly ILog Logger = LogManager.GetLogger("SettopBox");


        static void Main()
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
            if (CreatePidFile())
            {
                Run(settopBox, signal);
                RemovePidFile();
            }
            signal.Dispose();
            container.Dispose();
        }

        static bool CreatePidFile()
        {
            try
            {
                var file = new FileInfo(PidFile);
                if (!file.Exists)
                {
                    File.WriteAllText(file.FullName, $"{Process.GetCurrentProcess().Id}");
                    Logger.Info($"Created pid file {PidFile}");
                    return true;
                }
                var pid = File.ReadAllText(file.FullName);
                Logger.Warn($"SettopBox is already running under PID {pid} based on {file.FullName}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to create or validate pid file {PidFile}: {ex.Message}");
            }
            return false;
        }

        static void RemovePidFile()
        {
            try
            {
                var file = new FileInfo(PidFile);
                if (!file.Exists) return;

                file.Delete();
                Logger.Info($"Removed pid file {PidFile}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to remove the pid file {PidFile}: {ex.Message}");
            }
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
