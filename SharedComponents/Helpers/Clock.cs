using System;
using System.Threading;
using log4net;
using SharedComponents.Module;

namespace SharedComponents.Helpers
{
    public class Clock : IDisposable
    {
        readonly ILog _logger;
        readonly IThreadHelper _threadHelper;
        Thread _thread;
        bool _disposed;

        DateTime _time;
        string _waitingFor;
        CancellationTokenSource _cancelSource;

        public Clock(ILog logger, IThreadHelper threadHelper)
        {
            _logger = logger;
            _threadHelper = threadHelper;
        }

        void TickTack()
        {
            var counter = 0;
            while (_time > DateTime.Now && !_cancelSource.IsCancellationRequested)
            {
                if (counter > 60)
                {
                    counter = 0;
                    _logger.Debug($"WaitForTimestamp: Still waiting for  {_waitingFor} - {_time:yyyy-MM-dd hh:mm:ss}");
                }
                //Wait for 1 second, and re-evaluate the Cancellation Token
                Thread.Sleep(1000);
                counter++;
            }
        }

        public void WaitForTimestamp(DateTime time, CancellationTokenSource cancelSource, string caller = null)
        {
            _logger.Debug($"WaitForTimestamp: {caller} - {time:yyyy-MM-dd hh:mm:ss}");
            if (cancelSource.IsCancellationRequested) return;
            if (time < DateTime.Now) return;
            _logger.Info($"WaitForTimestamp: {caller} - {time:yyyy-MM-dd hh:mm:ss}");
            _time = time;
            _waitingFor = caller;
            _cancelSource = cancelSource;
            _thread = _threadHelper.RunSafeInNewThread(TickTack, _logger);
            _thread.Join();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                _logger.Warn("Clock is already disposed");
                return;
            }
            _logger.Info("Dispose the clock");
            _disposed = true;
            _threadHelper.AbortThread(_thread, _logger, 1100);
            _thread = null;
        }
    }
}