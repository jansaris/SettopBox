using System;
using System.Threading;
using EpgGrabber.IO;
using log4net;
using SharedComponents.DependencyInjection;
using SharedComponents.Helpers;
using SharedComponents.Module;
using WebHelper;
using SharedComponents.Models;

namespace EpgGrabber
{
    public class Program : BaseModule
    {
        private readonly IThreadHelper _threadHelper;
        private readonly Settings _settings;
        private readonly GrabberFactory _epgGrabberFactory;
        private readonly ChannelList _channelList;
        private readonly CachedWebDownloader _webDownloader;
        private Thread _runningEpgGrabTask;
        private DateTime? _lastRetrieval;
        private DateTime? _nextRetrieval;

        public Program(ILog logger, IThreadHelper threadHelper, Settings settings, GrabberFactory epgGrabberFactory, IWebDownloader webDownloader, ChannelList channelList, LinuxSignal signal, ModuleCommunication communication) : base(logger, signal, communication)
        {
            _threadHelper = threadHelper;
            _settings = settings;
            _epgGrabberFactory = epgGrabberFactory;
            _webDownloader = webDownloader as CachedWebDownloader;
            _channelList = channelList;
        }

        private static void Main()
        {
            var container = SharedContainer.CreateAndFill<DependencyConfig>("Log4net.config");
            var prog = container.GetInstance<Program>();
            var signal = container.GetInstance<LinuxSignal>();
            prog.Start();
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
            signal.Dispose();
            Console.WriteLine("Signal disposed, stop program");
            prog.Stop();
            container.Dispose();
        }

        public override IModuleInfo GetModuleInfo()
        {
            return new EpgGrabberInfo
            {
                LastRetrieval = _lastRetrieval,
                NextRetrieval = _nextRetrieval,
                Channels = _channelList.Channels.ToArray()
            };
        }

        protected override void StartModule()
        {
            Logger.Info("Welcome to EPG ObnGrabber");
            _settings.Load();
            _runningEpgGrabTask = _threadHelper.RunSafeInNewThread(DownloadEpgGrabberLoop, Logger, ThreadPriority.Lowest);
        }

        private void DownloadEpgGrabberLoop()
        {
            _nextRetrieval = _settings.InitialEpgGrab ? DateTime.Now : DetermineNextRetrieval();
            if (!_settings.InitialEpgGrab && !ModuleShouldStop()) ChangeState(ModuleState.Idle);
            while (!ModuleShouldStop())
            {
                try
                {
                    WaitAndRun();
                }
                catch (OperationCanceledException)
                {
                    //Ignore
                }
            }
        }

        private void WaitAndRun()
        {
            Logger.Info($"Next EPG will be fetched at {_nextRetrieval:yyyy-MM-dd HH:mm:ss}");
            WaitForSpecificState(ModuleState.Running, UpdateStateAfterNextRetrievalTimestamp);
            if (ModuleShouldStop()) return;
            try
            {

                var grabber = _epgGrabberFactory.Create();
                var epgFile = grabber.Download(ModuleShouldStop);
                if (epgFile != null)
                {
                    SignalNewData(DataType.Epg, epgFile);
                }
                _lastRetrieval = DateTime.Now;
            }
            catch (ThreadAbortException)
            {
                Logger.Warn("Load EPG has been aborted");
            }
            catch (Exception ex)
            {
                Logger.Fatal($"An unhandled exception occured: {ex.Message}", ex);
                Error();
            }
            _nextRetrieval = DetermineNextRetrieval();
            if (!ModuleShouldStop()) ChangeState(ModuleState.Idle);
        }

        private void UpdateStateAfterNextRetrievalTimestamp()
        {
            if (DateTime.Now < (_nextRetrieval ?? DateTime.MinValue)) return;
            Logger.Info($"Next retrieval window passed ({_nextRetrieval:yyyy-MM-dd HH:mm:ss}), switch state");
            if (!ModuleShouldStop()) ChangeState(ModuleState.Running);
        }

        private DateTime DetermineNextRetrieval()
        {
            //Set default at next block validation hour
            var today = DateTime.Today;
            var nextRun = new DateTime(today.Year, today.Month, today.Day, _settings.Hour, _settings.Minute, 0);
            if (DateTime.Now < nextRun)
            {
                Logger.Debug($"Next retrival calculated today at {nextRun:HH:mm:ss}");
                return nextRun;
            }
            var tomorrow = DateTime.Today.AddDays(1);
            nextRun = new DateTime(tomorrow.Year, tomorrow.Month, tomorrow.Day, _settings.Hour, _settings.Minute, 0);
            Logger.Debug($"Next retrival calculated tomorrow at {nextRun:HH:mm:ss}");
            return nextRun;
        }

        public override void ProcessDataFromOtherModule(string moduleName, CommunicationData data)
        {
            base.ProcessDataFromOtherModule(moduleName, data);
            switch (data.Type)
            {
                case DataType.EpgChannelUpdate:
                    Logger.Info($"Received EpgChannelUpdate from {moduleName}");
                    _channelList.ToggleChannel(data.Data as EpgChannelUpdate);
                    _nextRetrieval = DateTime.Now;
                    break;
            }

        }

        protected override void StopModule()
        {
            _threadHelper.AbortThread(_runningEpgGrabTask, Logger, 5000);
            _runningEpgGrabTask = null;
            _webDownloader?.SaveCache();
        }
    }
}
