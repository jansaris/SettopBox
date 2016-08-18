using System;
using System.IO;
using System.Threading;
using log4net;
using SharedComponents.DependencyInjection;
using SharedComponents.Helpers;
using SharedComponents.Module;

namespace Keyblock
{
    public class Program : BaseModule
    {
        readonly ILog _logger;
        readonly IThreadHelper _threadHelper;
        readonly Settings _settings;
        readonly Keyblock _keyblock;
        readonly Clock _clock;
        readonly CancellationTokenSource _cancelSource = new CancellationTokenSource();
        Thread _runningKeyblockThread;
        DateTime? _lastRetrieval;
        DateTime? _nextRetrieval;

        public Program(ILog logger, IThreadHelper threadHelper, Settings settings, Keyblock keyblock, LinuxSignal signal, ModuleCommunication communication, Clock clock) : base(signal, communication)
        {
            _logger = logger;
            _threadHelper = threadHelper;
            _settings = settings;
            _keyblock = keyblock;
            _clock = clock;
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
            _nextRetrieval = _settings.InitialLoadKeyblock ? DateTime.Now : DetermineNextRetrieval();
            while (!_cancelSource.IsCancellationRequested)
            {
                var nextRunAt = _nextRetrieval ?? DateTime.Now;
                WaitAndRun(nextRunAt);
            }
        }

        void WaitAndRun(DateTime executionTime)
        {
            _logger.Info($"Next keyblock will be fetched at {executionTime:yyyy-MM-dd HH:mm:ss}");
            _clock.WaitForTimestamp(executionTime, _cancelSource, "Keyblock");
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
            if (!_keyblock.BlockRefreshAfter.HasValue || _keyblock.BlockRefreshAfter.Value == DateTime.MinValue)
            {
                _logger.Warn("Can't calculate next retrieval because the loaded keyblock has no valid data");
                return nextRetrieval;
            }
            _logger.Debug($"Keyblock needs to be valid for at least {_settings.KeyblockValidationInHours} hours");
            return _keyblock.BlockRefreshAfter.Value.AddHours(-1*_settings.KeyblockValidationInHours);
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
                    SignalNewData(DataType.KeyBlock, new FileInfo(_keyblock.KeyblockFile).FullName);
                    return;
                }
                _logger.Error($"Failed to download a new keyblock at run {i}/{_settings.MaxRetries}");
                if (_cancelSource.IsCancellationRequested) return;
                _keyblock.CleanUp();
                _logger.Info($"Give the server '{_settings.WaitOnFailingBlockRetrievalInMilliseconds}ms' time");
                if (_cancelSource.IsCancellationRequested) return;
                Thread.Sleep(_settings.WaitOnFailingBlockRetrievalInMilliseconds);
            }
            _logger.Error($"Failed to retrieve the keyblock after {_settings.WaitOnFailingBlockRetrievalInMilliseconds} times, stop trying");
            Error();
        }

        protected override void StartModule()
        {
            _logger.Info("Welcome to Keyblock");
            _settings.Load();
            _runningKeyblockThread = _threadHelper.RunSafeInNewThread(LoadKeyBlockLoop, _logger, ThreadPriority.BelowNormal);
        }

        protected override void StopModule()
        {
            _cancelSource.Cancel();
            _threadHelper.AbortThread(_runningKeyblockThread, _logger, 10000);
            _runningKeyblockThread = null;
        }

        public override IModuleInfo GetModuleInfo()
        {
            return new KeyblockInfo
            {
                HasValidKeyblock = _keyblock.IsValid,
                ValidFrom = _keyblock.BlockValidFrom,
                ValidTo = _keyblock.BlockValidTo,
                RefreshAfter = _keyblock.BlockRefreshAfter,
                LastRetrieval = _lastRetrieval,
                NextRetrieval = _nextRetrieval
            };
        }
    }
}
