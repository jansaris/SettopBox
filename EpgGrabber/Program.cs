using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EpgGrabber.IO;
using log4net;
using SharedComponents;
using SharedComponents.DependencyInjection;
using SharedComponents.Module;

namespace EpgGrabber
{
    public class Program : BaseModule
    {
        readonly ILog _logger;
        readonly Settings _settings;
        readonly Grabber _epgGrabber;
        readonly ChannelList _channelList;
        readonly CachedWebDownloader _webDownloader;
        readonly CancellationTokenSource _cancelSource = new CancellationTokenSource();
        Task _runningEpgGrabTask;
        DateTime? _lastRetrieval;
        DateTime? _nextRetrieval;

        public Program(ILog logger, Settings settings, Grabber epgGrabber, IWebDownloader webDownloader, ChannelList channelList, LinuxSignal signal, ModuleCommunication communication) : base(signal, communication)
        {
            _logger = logger;
            _settings = settings;
            _epgGrabber = epgGrabber;
            _webDownloader = webDownloader as CachedWebDownloader;
            _channelList = channelList;
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
            _runningEpgGrabTask = Task.Factory.StartNew(DownloadEpgGrabberLoop, _cancelSource.Token, TaskCreationOptions.None, PriorityScheduler.Lowest);
        }

        void DownloadEpgGrabberLoop()
        {
            _nextRetrieval = _settings.InitialEpgGrab ? DateTime.Now : DetermineNextRetrieval();
            while (!_cancelSource.IsCancellationRequested)
            {
                var nextRunAt = _nextRetrieval ?? DateTime.Now;
                try
                {
                    WaitAndRun(nextRunAt).Wait(_cancelSource.Token);
                }
                catch (OperationCanceledException)
                {
                    //Ignore
                }
            }
        }

        async Task WaitAndRun(DateTime executionTime)
        {
            var waitTime = executionTime - DateTime.Now;
            if (waitTime.TotalMilliseconds > 0)
            {
                _logger.Info($"Next EPG will be fetched at {executionTime:yyyy-MM-dd HH:mm:ss}");
                await Task.Delay(executionTime - DateTime.Now, _cancelSource.Token);
            }
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
            Task.Delay(100).Wait();
            if (_runningEpgGrabTask != null && _runningEpgGrabTask.Status == AsyncTaskIsRunning)
            {
                _logger.Warn("Wait max 10 sec for EPG Grabber to stop");
                _runningEpgGrabTask.Wait(10000);
            }
            _webDownloader?.SaveCache();
        }
    }
}
