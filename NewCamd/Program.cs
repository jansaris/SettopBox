using System;
using System.Collections.Generic;
using System.Linq;
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
        readonly Func<NewCamdClient> _clientFactory;

        readonly object _syncObject = new object();
        readonly List<NewCamdClient> _activeClients;
        TcpListener _listener;
        bool _listening;

        public Program(ILog logger, Settings settings, Func<NewCamdClient> clientFactory)
        {
            _logger = logger;
            _settings = settings;
            _clientFactory = clientFactory;
            _activeClients = new List<NewCamdClient>();
        }

        static void Main()
        {
            var container = SharedContainer.CreateAndFill<DependencyConfig>("Log4net.config");
            var prog = container.GetInstance<Program>();
            //var dc = new Decrypt();
            //dc.Run(container.GetInstance<Settings>());
            prog.Start();
            Console.WriteLine("Hit 'Enter' to exit");
            prog.Listen();
            Console.ReadLine();
            prog.Stop();
        }

        void Start()
        {
            try
            {
                _logger.Info("Welcome to NewCamd");
                _settings.Load();
                StartServer();
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
            _listening = true;
            _logger.Info($"Start listening at {IPAddress.Any}:{_settings.Port}");
        }

        async void Listen()
        {
            try
            {
                while (_listening)
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    _logger.Debug("Try to accept new client");
                    var clientHandler = _clientFactory();
                    clientHandler.Closed += ClientClosed;
                    AddClientToWatchList(clientHandler);
                    clientHandler.Handle(client);
                }
            }
            catch (ObjectDisposedException)
            {
                if (_listening)
                {
                    throw;
                }
                //Ignore because this is expected to happen when we stopped listening    
            }
        }

        void Stop()
        {
            try
            {
                _listening = false;
                _listener.Stop();
                _logger.Info("Stopped listening");
                CloseClients();
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to stop the tcp listener", ex);
            }
        }

        void ClientClosed(object sender, EventArgs e)
        {
            var client = (NewCamdClient)sender;
            _logger.Info($"Stop monitoring client {client.Name}");
            RemoveClientFromWatchList(client);
        }

        void CloseClients()
        {
            _logger.Info($"Close {_activeClients.Count} clients");
            while (_activeClients.Count > 0)
            {
                NewCamdClient client;
                lock (_syncObject)
                {
                    client = _activeClients.FirstOrDefault();
                }
                client?.Dispose();
            }
        }

        void AddClientToWatchList(NewCamdClient client)
        {
            lock (_syncObject)
            {
                _activeClients.Add(client);
            }
            _logger.Debug($"Added client {client.Name} to the watchlist");
        }

        void RemoveClientFromWatchList(NewCamdClient client)
        {
            lock (_syncObject)
            {
                _activeClients.Remove(client);
            }
            _logger.Debug($"Removed client {client.Name} from the watchlist");
        }
    }
}

