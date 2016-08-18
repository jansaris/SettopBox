using System;
using System.Threading;
using log4net;
using Mono.Unix;
using Mono.Unix.Native;
using SharedComponents.Module;

namespace SharedComponents.Helpers
{
    public class LinuxSignal : IDisposable
    {
        readonly ILog _logger;
        readonly IThreadHelper _threadHelper;
        bool _disposing;
        bool _running;
        Thread _listeningThread;

        public LinuxSignal(ILog logger, IThreadHelper threadHelper)
        {
            _logger = logger;
            _threadHelper = threadHelper;
        }

        public event EventHandler Exit;

        public void Listen()
        {
            _listeningThread = _threadHelper.RunSafeInNewThread(ListenForSignal, _logger);
        }

        public void WaitForListenThreadToComplete()
        {
            while (_listeningThread.IsAlive)
            {
                Thread.Sleep(1000);
            }
        }

        void ListenForSignal()
        {
            try
            {
                var intr = new UnixSignal(Signum.SIGINT);
                var term = new UnixSignal(Signum.SIGTERM);
                var hup = new UnixSignal(Signum.SIGHUP);
                var usr2 = new UnixSignal(Signum.SIGUSR2);
                UnixSignal[] signals = { intr, term, hup, usr2 };

                _logger.Info("Start listening for unix signals");

                for (_running = true; _running;)
                {
                    var idx = UnixSignal.WaitAny(signals, 1000);
                    if (idx < 0 || idx >= signals.Length) continue;
                    if (!_running) return;

                    _logger.Info("LinuxSignal: received signal " + signals[idx].Signum);

                    if ((intr.IsSet || term.IsSet))
                    {
                        intr.Reset();
                        term.Reset();

                        _logger.Info("LinuxSignal: stopping...");

                        _running = false;
                        OnExit();
                        Environment.Exit(0);
                    }
                    else if (hup.IsSet)
                    {
                        // Ignore. Could be used to reload configuration.
                        hup.Reset();
                    }
                    else if (usr2.IsSet)
                    {
                        usr2.Reset();
                        // do something
                    }
                }
            }
            catch
            {
                _logger.Info("Unable to listen on unix signals");
            }
            _logger.Info("Finished listening to unix signals");
        }

        protected virtual void OnExit()
        {
            Exit?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            if (_disposing) return;
            _disposing = true;
            _running = false;
            OnExit();
            WaitForListenThreadToComplete();
            _listeningThread = null;
        }
    }
}