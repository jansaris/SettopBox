using System;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using SharedComponents.DependencyInjection;
using SharedComponents.Module;

namespace Keyblock
{
    public class Program : BaseModule
    {
        readonly ILog _logger;
        readonly Settings _settings;
        readonly Keyblock _keyblock;
        readonly CancellationTokenSource _cancelSource = new CancellationTokenSource();
        Task _runningKeyblockTask;

        public Program(ILog logger, Settings settings, Keyblock keyblock)
        {
            _logger = logger;
            _settings = settings;
            _keyblock = keyblock;
        }

        static void Main()
        {
            var container = SharedContainer.CreateAndFill<DependencyConfig>("Log4net.config");
            var prog = container.GetInstance<Program>();
            prog.Start();
            prog._runningKeyblockTask.Wait();
        }

        void Run()
        {
            try
            {
                _logger.Info("Welcome to Keyblock");
                _settings.Load();
                LoadKeyBlock();
                _logger.Info("Done");
            }
            catch (Exception ex)
            {
                _logger.Fatal("An unhandled exception occured", ex);
                Error();
            }
        }

        void LoadKeyBlock()
        {
            if (!_settings.ForceInitialKeyblockDownload && _keyblock.ValidateKeyBlock())
            {
                return;
            }
            for (var i = 1; i <= _settings.MaxRetries; i++)
            {
                if(_cancelSource.IsCancellationRequested) return;
                _logger.Info($"Start loading keyblock at run {i}/{_settings.MaxRetries}");
                if (_keyblock.DownloadNew())
                {
                    _logger.Info($"Succesfully loaded a new keyblock at run {i}/{_settings.MaxRetries}");
                    return;
                }
                _logger.Error($"Failed to download a new keyblock at run {i}/{_settings.MaxRetries}");
                if (_cancelSource.IsCancellationRequested) return;
                _keyblock.CleanUp();
                _logger.Info($"Give the server '{_settings.WaitOnFailingBlockRetrievalInMilliseconds}ms' time");
                if (_cancelSource.IsCancellationRequested) return;
                Task.Delay(_settings.WaitOnFailingBlockRetrievalInMilliseconds).Wait();
            }
            _logger.Error($"Failed to retrieve the keyblock after {_settings.WaitOnFailingBlockRetrievalInMilliseconds} times, stop trying");
            Error();
        }

        protected override void StartModule()
        {
            _runningKeyblockTask = Task.Run(() => Run(), _cancelSource.Token);
        }

        protected override void StopModule()
        {
            _cancelSource.Cancel();
            if (_runningKeyblockTask == null || _runningKeyblockTask.Status != AsyncTaskIsRunning) return;

            _logger.Warn("Wait max 10 sec for Keyblock to stop");
            _runningKeyblockTask.Wait(10000);
        }
        public override IModuleInfo GetModuleInfo()
        {
            return null;
        }
    }
}
