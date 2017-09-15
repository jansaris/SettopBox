using System;
using System.IO;
using System.Threading;
using log4net;
using SharedComponents.DependencyInjection;
using SharedComponents.Helpers;
using SharedComponents.Module;
using SharedComponents.Models;

namespace Keyblock
{
    public class Program : BaseModule
    {
        readonly IThreadHelper _threadHelper;
        readonly Settings _settings;
        readonly Keyblock _keyblock;
        Thread _runningKeyblockThread;
        DateTime? _lastRetrieval;
        DateTime? _nextRetrieval;

        public Program(ILog logger, IThreadHelper threadHelper, Settings settings, Keyblock keyblock, LinuxSignal signal, ModuleCommunication communication) : base(logger, signal, communication)
        {
            _threadHelper = threadHelper;
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
            _nextRetrieval = _settings.InitialLoadKeyblock ? DateTime.Now : DetermineNextRetrieval();
            while (!ModuleShouldStop())
            {
                WaitAndRun();
            }
        }

        void WaitAndRun()
        {
            Logger.Info($"Next keyblock will be fetched at {_nextRetrieval:yyyy-MM-dd HH:mm:ss}");
            WaitForSpecificState(ModuleState.Running, UpdateStateAfterNextRetrievalTimestamp);
            if (ModuleShouldStop()) return;
            try
            {
                LoadKeyBlock();
            }
            catch (ThreadAbortException)
            {
                Logger.Warn("Load keyblock has been aborted");
            }
            catch (Exception ex)
            {
                Logger.Fatal($"An unhandled exception occured: {ex.Message}", ex);
                Error();
            }
            _nextRetrieval = DetermineNextRetrieval();
            if (!ModuleShouldStop()) ChangeState(ModuleState.Idle);
        }

        void UpdateStateAfterNextRetrievalTimestamp()
        {
            if (DateTime.Now < (_nextRetrieval ?? DateTime.MinValue)) return;
            Logger.Info($"Next retrieval window passed ({_nextRetrieval:yyyy-MM-dd HH:mm:ss}), switch state");
            if (!ModuleShouldStop()) ChangeState(ModuleState.Running);
        }

        DateTime DetermineNextRetrieval()
        {
            //Set default at next block validation hour
            var nextRetrieval = DateTime.Now.AddHours(_settings.KeyblockValidationInHours);
            if (!_keyblock.IsValid)
            {
                Logger.Warn("Can't calculate next retrieval because the keyblock is not valid");
                return nextRetrieval;
            }
            if (!_keyblock.BlockRefreshAfter.HasValue || _keyblock.BlockRefreshAfter.Value == DateTime.MinValue)
            {
                Logger.Warn("Can't calculate next retrieval because the loaded keyblock has no valid data");
                return nextRetrieval;
            }
            nextRetrieval = _keyblock.BlockRefreshAfter.Value.AddHours(-1*_settings.KeyblockValidationInHours);
            if (nextRetrieval >= DateTime.Now)
            {
                Logger.Debug($"Keyblock is valid for at least {_settings.KeyblockValidationInHours} hours");
                return nextRetrieval;
            }

            Logger.Warn("Keyblock contains possible old channels which are not blacklisted. Look at log!");
            return _keyblock.FirstRefreshDateInFuture();
        }

        void LoadKeyBlock()
        {
            if (!_settings.ForceInitialKeyblockDownload && _keyblock.ValidateKeyBlock())
            {
                return;
            }
            for (var i = 1; i <= _settings.MaxRetries; i++)
            {
                if(ModuleShouldStop()) return;
                Logger.Info($"Start loading keyblock at run {i}/{_settings.MaxRetries}");
                if (_keyblock.DownloadNew())
                {
                    Logger.Info($"Succesfully loaded a new keyblock at run {i}/{_settings.MaxRetries}");
                    _lastRetrieval = DateTime.Now;
                    SignalNewData(DataType.KeyBlock, new FileInfo(_keyblock.KeyblockFile).FullName);
                    return;
                }
                Logger.Error($"Failed to download a new keyblock at run {i}/{_settings.MaxRetries}");
                if (ModuleShouldStop()) return;
                if (_settings.AutoCleanUp)
                {
                    Logger.Info("Clean up data of previous unsuccessfull run");
                    _keyblock.CleanUp();
                }
                else
                {
                    Logger.Warn("Don't clean up data of previous run");
                }
                Logger.Info($"Give the server '{_settings.WaitOnFailingBlockRetrievalInMilliseconds}ms' time");
                if (ModuleShouldStop()) return;
                Thread.Sleep(_settings.WaitOnFailingBlockRetrievalInMilliseconds);
            }
            Logger.Error($"Failed to retrieve the keyblock after {_settings.WaitOnFailingBlockRetrievalInMilliseconds} times, stop trying");
            Error();
        }

        public override void ProcessDataFromOtherModule(string moduleName, CommunicationData data)
        {
            base.ProcessDataFromOtherModule(moduleName, data);
            switch (data.Type)
            {
                case DataType.KeyblockChannelUpdate:
                    Logger.Info($"Received KeyblockChannelUpdate from {moduleName}");
                    _settings.ToggleChannel(data.Data as KeyblockChannelUpdate);
                    _nextRetrieval = DateTime.Now;
                    break;
            }
        }

        protected override void StartModule()
        {
            Logger.Info("Welcome to Keyblock");
            _settings.Load();
            _runningKeyblockThread = _threadHelper.RunSafeInNewThread(LoadKeyBlockLoop, Logger, ThreadPriority.BelowNormal);
        }

        protected override void StopModule()
        {
            _threadHelper.AbortThread(_runningKeyblockThread, Logger, 10000);
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
                NextRetrieval = _nextRetrieval,
                ChannelsToMonitor = _settings.GetChannelsToMonitor()
            };
        }
    }
}
