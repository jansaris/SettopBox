using System;
using System.Net;
using System.Net.Sockets;
using log4net;
using SharedComponents.DependencyInjection;

namespace NewCamd
{
    class Program
    {
        readonly ILog _logger;
        readonly Settings _settings;
        TcpListener _listener;
        bool _listening;

        public Program(ILog logger, Settings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        static void Main()
        {
            var container = SharedContainer.CreateAndFill<DependencyConfig>("Log4net.config");
            var prog = container.GetInstance<Program>();
            prog.Start();
            Console.WriteLine("Hit 'Enter' to exit");
            prog.Listen();
            Console.ReadLine();
            prog.Stop();
        }

        void Listen()
        {
            while (_listening)
            {
                var client = _listener.AcceptSocket();
                
            }
        }

        void Stop()
        {
            try
            {
                _listening = false;
                _listener.Stop();
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to stop the tcp listener", ex);
            }
            _logger.Info("Exit NewCamd");
        }

        void Start()
        {
            try
            {
                _logger.Info("Welcome to NewCamd");
                _settings.Load();
                StartServer();
                _logger.Info("Done");
            }
            catch (Exception ex)
            {
                _logger.Fatal("An unhandled exception occured", ex);
            }
        }

        void StartServer()
        {
            _listener = new TcpListener(IPAddress.Any, _settings.Port);
            _listener.Start();
        }
    }
}
