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
        readonly Func<NewCamdApi> _clientFactory;

        readonly object _syncObject = new object();
        readonly List<NewCamdApi> _activeClients;
        TcpListener _listener;
        bool _listening;

        public Program(ILog logger, Settings settings, Func<NewCamdApi> clientFactory)
        {
            _logger = logger;
            _settings = settings;
            _clientFactory = clientFactory;
            _activeClients = new List<NewCamdApi>();
        }

        static void Main()
        {
            var container = SharedContainer.CreateAndFill<DependencyConfig>("Log4net.config");
            var prog = container.GetInstance<Program>();
            var keynlock = container.GetInstance<Keyblock>();
            keynlock.Prepare();
            keynlock.DecryptTest();

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
                    _logger.Debug("Try to accept new api");
                    var clientHandler = _clientFactory();
                    clientHandler.Closed += ClientClosed;
                    AddClientToWatchList(clientHandler);
                    clientHandler.HandleClient(client);
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
            var client = (NewCamdApi)sender;
            _logger.Info($"Stop monitoring client {client.Name}");
            RemoveClientFromWatchList(client);
        }

        void CloseClients()
        {
            _logger.Info($"Close {_activeClients.Count} clients");
            while (_activeClients.Count > 0)
            {
                NewCamdApi api;
                lock (_syncObject)
                {
                    api = _activeClients.FirstOrDefault();
                }
                api?.Dispose();
            }
        }

        void AddClientToWatchList(NewCamdApi api)
        {
            lock (_syncObject)
            {
                _activeClients.Add(api);
            }
            _logger.Debug($"Added client {api.Name} to the watchlist");
        }

        void RemoveClientFromWatchList(NewCamdApi api)
        {
            lock (_syncObject)
            {
                _activeClients.Remove(api);
            }
            _logger.Debug($"Removed client {api.Name} from the watchlist");
        }
    }
}

