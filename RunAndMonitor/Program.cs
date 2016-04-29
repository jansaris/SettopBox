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
        string _status = "Initial";

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
                Status = _status
            };
        }

        protected override void StartModule()
        {
            _logger.Info("Welcome to RunAndMonitor");
            _status = "Starting";
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
            CreateProcess();
            _logger.Info($"Start {_settings.Executable}");
            //Register on output events
            _process.OutputDataReceived += (sender, args) => Log(args.Data, _logger.Debug);
            _process.ErrorDataReceived += (sender, args) => Log(args.Data, _logger.Error);
            _process.Exited += ProcessExited;
            //Start process
            var result = _process.Start();
            if (!result)
            {
                _logger.Error($"Failed to start {_settings.Executable}");
                _status = "Failed to start";
                return;
            }

            //Start listening on output 
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
            _logger.Info($"Succesfully started {_process.ProcessName} with Id {_process.Id}");
            _status = "Running";
        }

        void CreateProcess()
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
            _process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };
        }

        void ProcessExited(object sender, EventArgs e)
        {
            _logger.Info($"{_process.ProcessName} exited with code {_process.ExitCode}");
            _status = "Program exited";
            _process = null;
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
                    _logger.Error($"Failed to stop process {name} with Id {id}");
                }
                else
                {
                    _logger.Info($"Stopped process {name} with Id {id}");
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
