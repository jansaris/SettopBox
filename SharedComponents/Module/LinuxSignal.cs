using System;
using System.Threading.Tasks;
using log4net;
using Mono.Unix;
using Mono.Unix.Native;

namespace SharedComponents.Module
{
    public class LinuxSignal : IDisposable
    {
        readonly ILog _logger;
        bool _disposing;
        bool _running;

        public LinuxSignal(ILog logger)
        {
            _logger = logger;
        }

        public event EventHandler Exit;

        public void Listen()
        {
            Task.Run(() => ListenForSignal());
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

                    _logger.Info("daemon: received signal " + signals[idx].Signum);

                    if ((intr.IsSet || term.IsSet))
                    {
                        intr.Reset();
                        term.Reset();

                        _logger.Info("daemon: stopping...");

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
        }
    }
}