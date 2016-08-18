using System;
using System.Linq;
using System.Threading;
using EpgGrabber.IO;
using log4net;
using SharedComponents.DependencyInjection;
using SharedComponents.Helpers;
using SharedComponents.Module;

namespace EpgGrabber
{
    public class Program : BaseModule
    {
        readonly ILog _logger;
        readonly IThreadHelper _threadHelper;
        readonly Settings _settings;
        readonly Grabber _epgGrabber;
        readonly ChannelList _channelList;
        readonly CachedWebDownloader _webDownloader;
        readonly CancellationTokenSource _cancelSource = new CancellationTokenSource();
        readonly Clock _clock;
        Thread _runningEpgGrabTask;
        DateTime? _lastRetrieval;
        DateTime? _nextRetrieval;

        public Program(ILog logger, IThreadHelper threadHelper, Settings settings, Grabber epgGrabber, IWebDownloader webDownloader, ChannelList channelList, LinuxSignal signal, ModuleCommunication communication, Clock clock) : base(signal, communication)
        {
            _logger = logger;
            _threadHelper = threadHelper;
            _settings = settings;
            _epgGrabber = epgGrabber;
            _webDownloader = webDownloader as CachedWebDownloader;
            _channelList = channelList;
            _clock = clock;
        }

        static void Main()
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
                Channels = _channelList.Channels.Select(kv => kv.Value).ToArray()
            };
        }

        protected override void StartModule()
        {
            _logger.Info("Welcome to EPG Grabber");
            _settings.Load();
            _runningEpgGrabTask = _threadHelper.RunSafeInNewThread(DownloadEpgGrabberLoop, _logger, ThreadPriority.Lowest);
        }

        void DownloadEpgGrabberLoop()
        {
            _nextRetrieval = _settings.InitialEpgGrab ? DateTime.Now : DetermineNextRetrieval();
            while (!_cancelSource.IsCancellationRequested)
            {
                var nextRunAt = _nextRetrieval ?? DateTime.Now;
                try
                {
                    WaitAndRun(nextRunAt);
                }
                catch (OperationCanceledException)
                {
                    //Ignore
                }
            }
        }

        void WaitAndRun(DateTime executionTime)
        {
            _logger.Info($"Next EPG will be fetched at {executionTime:yyyy-MM-dd HH:mm:ss}");
            _clock.WaitForTimestamp(executionTime, _cancelSource, "EpgGrabber");
            if (_cancelSource.IsCancellationRequested) return;
            try
            {
                var epgFile = _epgGrabber.Download(_cancelSource);
                if (epgFile != null)
                {
                    SignalNewData(DataType.Epg, epgFile);
                }
                _lastRetrieval = DateTime.Now;
                _nextRetrieval = DetermineNextRetrieval();
            }
            catch (Exception ex)
            {
                _logger.Fatal("An unhandled exception occured", ex);
                Error();
                _nextRetrieval = DateTime.Now.AddHours(1);
            }
        }

        DateTime DetermineNextRetrieval()
        {
            //Set default at next block validation hour
            var today = DateTime.Today;
            var nextRun = new DateTime(today.Year, today.Month, today.Day, _settings.Hour, _settings.Minute, 0);
            if (DateTime.Now < nextRun)
            {
                _logger.Debug($"Next retrival calculated today at {nextRun:HH:mm:ss}");
                return nextRun;
            }
            var tomorrow = DateTime.Today.AddDays(1);
            nextRun = new DateTime(tomorrow.Year, tomorrow.Month, tomorrow.Day, _settings.Hour, _settings.Minute, 0);
            _logger.Debug($"Next retrival calculated tomorrow at {nextRun:HH:mm:ss}");
            return nextRun;
        }

        protected override void StopModule()
        {
            _cancelSource.Cancel();
            _threadHelper.AbortThread(_runningEpgGrabTask, _logger, 5000);
            _runningEpgGrabTask = null;
            _webDownloader?.SaveCache();
        }
    }
}
