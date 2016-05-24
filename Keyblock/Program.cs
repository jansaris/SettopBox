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
        DateTime? _lastRetrieval;
        DateTime? _nextRetrieval;

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
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
            prog.Stop();
            container.Dispose();
        }

        void LoadKeyBlockLoop()
        {
            while (!_cancelSource.IsCancellationRequested)
            {
                var nextRunAt = _nextRetrieval ?? DateTime.Now;
                WaitAndRun(nextRunAt).Wait(_cancelSource.Token);
            }
        }

        async Task WaitAndRun(DateTime executionTime)
        {
            var waitTime = executionTime - DateTime.Now;
            if (waitTime.TotalMilliseconds > 0)
            {
                _logger.Info($"Next keyblock will be fetched at {executionTime:yyyy-MM-dd HH:mm:ss}");
                await Task.Delay(executionTime - DateTime.Now, _cancelSource.Token);
            }
            if (_cancelSource.IsCancellationRequested) return;
            try
            {
                LoadKeyBlock();
                _nextRetrieval = DetermineNextRetrieval();
            }
            catch (Exception ex)
            {
                _logger.Fatal("An unhandled exception occured", ex);
                Error();
            }
        }

        DateTime DetermineNextRetrieval()
        {
            //Set default at next block validation hour
            var nextRetrieval = DateTime.Now.AddHours(_settings.KeyblockValidationInHours);
            if (!_keyblock.IsValid)
            {
                _logger.Warn("Can't calculate next retrieval because the keyblock is not valid");
                return nextRetrieval;
            }
            if (!_keyblock.BlockValidTo.HasValue || _keyblock.BlockValidTo.Value == DateTime.MinValue)
            {
                _logger.Warn("Can't calculate next retrieval because the loaded keyblock has no valid data");
                return nextRetrieval;
            }
            _logger.Debug($"Keyblock needs to be valid for at least {_settings.KeyblockValidationInHours} hours");
            return _keyblock.BlockValidTo.Value.AddHours(-1*_settings.KeyblockValidationInHours);
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
                    _lastRetrieval = DateTime.Now;
                    SignalNewData(Data.KeyBlock);
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
            _logger.Info("Welcome to Keyblock");
            _settings.Load();
            _runningKeyblockTask = Task.Run(() => LoadKeyBlockLoop(), _cancelSource.Token);
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
            return new KeyblockInfo
            {
                HasValidKeyblock = _keyblock.IsValid,
                ValidFrom = _keyblock.BlockValidFrom,
                ValidTo = _keyblock.BlockValidTo,
                LastRetrieval = _lastRetrieval,
                NextRetrieval = _nextRetrieval
            };
        }
    }
}
