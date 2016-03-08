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
        readonly Func<NewCamdClientHandler> _clientFactory;

        readonly object _syncObject = new object();
        readonly List<NewCamdClientHandler> _activeClients;
        TcpListener _listener;
        bool _listening;

        public Program(ILog logger, Settings settings, Func<NewCamdClientHandler> clientFactory)
        {
            _logger = logger;
            _settings = settings;
            _clientFactory = clientFactory;
            _activeClients = new List<NewCamdClientHandler>();
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
            _logger.Info($"Start listening at {IPAddress.Any}:{_settings.Port}");
        }

        async void Listen()
        {
            while (_listening)
            {
                var client = await _listener.AcceptTcpClientAsync();
                _logger.Info($"Accept new client: {client.Client.LocalEndPoint}");
                var clientHandler = _clientFactory();
                clientHandler.Handle(client);
                clientHandler.Closed += ClientClosed;
                AddClientToWatchList(clientHandler);
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
            var client = (NewCamdClientHandler)sender;
            _logger.Info($"Stop monitoring client {client.Name}");
            RemoveClientFromWatchList(client);
        }

        void CloseClients()
        {
            _logger.Info($"Close {_activeClients.Count} clients");
            while (_activeClients.Count > 0)
            {
                NewCamdClientHandler client;
                lock (_syncObject)
                {
                    client = _activeClients.First();
                }
                client?.Close();
            }
        }

        void AddClientToWatchList(NewCamdClientHandler client)
        {
            lock (_syncObject)
            {
                _activeClients.Add(client);
            }
            _logger.Debug($"Added client {client.Name} to the watchlist");
        }

        void RemoveClientFromWatchList(NewCamdClientHandler client)
        {
            lock (_syncObject)
            {
                _activeClients.Remove(client);
            }
            _logger.Debug($"Removed client {client.Name} from the watchlist");
        }
    }
}

