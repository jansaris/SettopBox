using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using log4net;
using SharedComponents.Helpers;
using WebUi.api;

namespace SettopBox
{
    internal class PidMonitor
    {
        static readonly FileInfo PidFile = new FileInfo(Path.Combine(AssemblyDirectory, "SettopBox.pid"));
        static readonly object SyncRoot = new object();
        readonly ILog _logger;
        readonly LinuxSignal _signal;

        public PidMonitor(ILog logger, LinuxSignal signal)
        {
            _logger = logger;
            _signal = signal;
        }

        public bool Start()
        {
            if (IsNotRunning() && CreatePidFile())
            {
                _signal.Exit += (sender, args) => RemovePidFile();
                return true;
            }
            return false;
        }

        public void Stop()
        {
            RemovePidFile();
        }

        bool IsNotRunning()
        {
            if (!PidFile.Exists) return true;
            var pid = File.ReadAllText(PidFile.FullName);
            _logger.Info($"Found pid file {PidFile.FullName}, check process with PID {pid}");
            var splitted = pid.Split('|');
            int intPid;
            if (!int.TryParse(splitted[0], out intPid))
            {
                _logger.Warn($"Failed to parse {pid} to a number, ignore existing pid file");
                return true;
            }
            try
            {
                var process = Process.GetProcessById(intPid);
                _logger.Info($"Got process {intPid} which is {(process.HasExited ? "not" : "still")} running");
                if (splitted.Length <= 0) return process.HasExited;
                _logger.Info($"Check if processname {process.ProcessName} matches {splitted[1]}");
                if (process.ProcessName.Equals(splitted[1])) return process.HasExited;
                _logger.Info("Another process is running under this pid");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex.Message);
                _logger.Info($"Ignore PID {intPid}");
                return true;
            }
        }

        bool CreatePidFile()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                File.WriteAllText(PidFile.FullName, $"{process.Id}|{process.ProcessName}");
                _logger.Info($"Created pid file {PidFile}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to create pid file {PidFile.FullName}: {ex.Message}");
            }
            return false;
        }

        void RemovePidFile()
        {
            try
            {
                lock (SyncRoot)
                {
                    if (!PidFile.Exists) return;
                    PidFile.Delete();
                    _logger.Info($"Removed pid file {PidFile.FullName}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to remove the pid file {PidFile.FullName}: {ex.Message}");
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