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

        private Func<bool> _stopWaiting;
        private Action _callBackWhenTimestampPasses;

        public Clock(ILog logger, IThreadHelper threadHelper)
        {
            _logger = logger;
            _threadHelper = threadHelper;
        }

        void TickTack()
        {
            var counter = 0;
            while (_time > DateTime.Now && !_stopWaiting())
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
            
            if (!_stopWaiting())
            {
                _logger.Info($"WaitForTimestamp: finished for {_waitingFor} at {_time:yyyy-MM-dd hh:mm:ss}, callback to {_waitingFor}");
                _callBackWhenTimestampPasses();
            }
            else
            {
                _logger.Info($"WaitForTimestamp: finished for {_waitingFor} at {_time:yyyy-MM-dd hh:mm:ss}, because cancellation was requested");
            }
        }

        public void WaitForTimestamp(DateTime time, Func<bool> stopWaiting, Action callBackWhenTimestampPasses, string caller = null)
        {
            _logger.Debug($"WaitForTimestamp: {caller} - {time:yyyy-MM-dd hh:mm:ss}");
            if (stopWaiting()) return;
            if (time < DateTime.Now) return;
            _logger.Info($"WaitForTimestamp: {caller} - {time:yyyy-MM-dd hh:mm:ss}");
            _time = time;
            _waitingFor = caller;
            _stopWaiting = stopWaiting;
            _callBackWhenTimestampPasses = callBackWhenTimestampPasses;
            _thread = _threadHelper.RunSafeInNewThread(TickTack, _logger);
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