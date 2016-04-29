using System;
using System.Diagnostics;
using log4net;
using SharedComponents.DependencyInjection;
using SharedComponents.Module;

namespace RunAndMonitor
{
    class Program : BaseModule
    {
        static Process _process;
        readonly Settings _settings;
        readonly ILog _logger;

        public Program(Settings settings, ILog logger)
        {
            _settings = settings;
            _logger = logger;
        }

        static void Main()
        {
            var container = SharedContainer.CreateAndFill<DependencyConfig>("Log4net.config");
            var prog = container.GetInstance<Program>();
            prog.Start();
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
            prog.Stop();
        }

        public override IModuleInfo GetModuleInfo()
        {
            _process?.Refresh();
            return new RunAndMonitorInfo
            {
                PID = $"{_process?.Id}",
                ProcessName = _process?.ProcessName,
                Status = GetStatus()
            };
        }

        string GetStatus()
        {
            if (_process == null) return "Not started";
            return _process.HasExited ? "Exited" : "Running";
        }

        protected override void StartModule()
        {
            _logger.Info("Welcome to RunAndMonitor");
            _settings.Load();
            if (string.IsNullOrWhiteSpace(_settings.Executable))
            {
                _logger.Info("No program to run and monitor");
                return;
            }
            StartProcess();
        }

        void StartProcess()
        {
            _logger.Debug($"Create process for {_settings.Executable}");
            var startInfo = new ProcessStartInfo(_settings.Executable)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };
            if (!string.IsNullOrWhiteSpace(_settings.Arguments))
            {
                _logger.Debug($"Use command line argumens {_settings.Arguments}");
                startInfo.Arguments = _settings.Arguments;
            }
            if (!string.IsNullOrWhiteSpace(_settings.WorkingDirectory))
            {
                _logger.Debug($"Use working directory {_settings.WorkingDirectory}");
                startInfo.WorkingDirectory = _settings.WorkingDirectory;
            }

            _process = new Process { StartInfo = startInfo };
            _logger.Info($"Start {_settings.Executable}");
            //Register on output events
            _process.OutputDataReceived += (sender, args) => Log(args.Data, _logger.Debug);
            _process.ErrorDataReceived += (sender, args) => Log(args.Data, _logger.Error);
            _process.Exited += (sender, args) => Log($"{_process.ProcessName} exited with code {_process.ExitCode}", _logger.Info);
            //Start process
            var result = _process.Start();
            //Start listening on output 
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
            if (result)
            {
                _logger.Info($"Succesfully started {_process.ProcessName}");
            }
            else
            {
                _logger.Error($"Failed to start {_settings.Executable}");
            }
        }

        void Log(string message, Action<string> logger)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            logger(message);
        }

        protected override void StopModule()
        {
            if(_process == null) return;
            var name = _process.ProcessName;
            var id = _process.Id;
            try
            {
                if (KillApplication(name)) return;
                if (!_process.HasExited)
                {
                    _logger.Fatal($"Failed to stop process {name} with Id {id}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to stop {name} with Id {id}", ex);
            }
        }

        bool KillApplication(string name)
        {
            try
            {
                _logger.Debug($"Close {name} by killing it");
                _process.Kill();
            }
            catch (Exception ex)
            {
                _logger.Warn($"Failed to kill {name}", ex);
            }
            return _process.HasExited;
        }
    }
}
