using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using log4net;
using SharedComponents.DependencyInjection;
using SharedComponents.Helpers;
using SharedComponents.Module;

namespace NewCamd
{
    public class Program : BaseModule
    {
        readonly ILog _logger;
        readonly IThreadHelper _threadHelper;
        readonly Settings _settings;
        readonly Func<NewCamdApi> _clientFactory;
        readonly Keyblock _keyblock;

        readonly object _syncObject = new object();
        readonly List<NewCamdApi> _activeClients;
        TcpListener _listener;
        bool _listening;
        Thread _listeningThread;
        string _listeningAdress;

        public Program(ILog logger, IThreadHelper threadHelper, Settings settings, Func<NewCamdApi> clientFactory, Keyblock keyblock, LinuxSignal signal, ModuleCommunication communication) : base(signal, communication)
        {
            _logger = logger;
            _threadHelper = threadHelper;
            _settings = settings;
            _clientFactory = clientFactory;
            _keyblock = keyblock;
            _activeClients = new List<NewCamdApi>();
        }

        static void Main()
        {
            var container = SharedContainer.CreateAndFill<DependencyConfig>("Log4net.config");
            var prog = container.GetInstance<Program>();

            prog.Start();
            Console.WriteLine("Hit 'Enter' to exit");
            Console.ReadLine();
            prog.Stop();
            container.Dispose();
        }

        public override IModuleInfo GetModuleInfo()
        {
            return new NewCamdInfo
            {
                NrOfClients = _activeClients.Count,
                NrOfChannels = _keyblock.NrOfChannels,
                ValidFrom = _keyblock.ValidFrom,
                ValidTo = _keyblock.ValidTo,
                DesKey = _settings.DesKey,
                Username = _settings.Username,
                Password = _settings.Password,
                ListeningAt = _listeningAdress
            };
        }

        protected override void StartModule()
        {
            try
            {
                _logger.Info("Welcome to NewCamd");
                _settings.Load();
                _keyblock.Prepare();
                StartServer();
                _listeningThread = _threadHelper.RunSafeInNewThread(Listen,_logger);
            }
            catch (Exception ex)
            {
                _logger.Fatal("An unhandled exception occured", ex);
                Error();
            }
        }

        protected override void StopModule()
        {
            try
            {
                _listening = false;
                if (_listener != null)
                {
                    _listener.Stop();
                    _logger.Info("Stopped listening");
                }               
                CloseClients();
                _threadHelper.AbortThread(_listeningThread,_logger,10000);
                _listeningThread = null;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to stop the tcp listener", ex);
            }
        }

        void StartServer()
        {
            var ip = GetIpAdress();
            _listener = new TcpListener(ip, _settings.Port);
            _listener.Start();
            _listening = true;
            _listeningAdress = $"{ip}:{_settings.Port}";
            _logger.Info($"Start listening at {_listeningAdress}");
        }

        void ReInitializeListener()
        {
            if (!_listening) return;
            _listener.Stop();
            StartServer();
        }

        IPAddress GetIpAdress()
        {
            if(string.IsNullOrWhiteSpace(_settings.IpAdress)) return IPAddress.Any;
            try
            {
                return IPAddress.Parse(_settings.IpAdress);
            }
            catch (Exception)
            {
                _logger.Warn($"Failed to parse IpAdress to listen on: {_settings.IpAdress}, use Any");
                return IPAddress.Any;
            }
        }

        void Listen()
        {
            _logger.Debug("Start listening thread");
            try
            {
                while (_listening)
                {
                    var client = _listener.AcceptTcpClient();
                    _logger.Debug("Try to accept new api");
                    var clientHandler = _clientFactory();
                    clientHandler.Closed += ClientClosed;
                    AddClientToWatchList(clientHandler);
                    clientHandler.HandleClient(client);
                    ReInitializeListener();
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
            _logger.Debug("Finished listening thread");
        }

        public override void ProcessDataFromOtherModule(string moduleName, CommunicationData data)
        {
            if (!ShouldWeProcessNewData(moduleName, data.Type)) return;
            _logger.Info($"Handle new {data.Type} from {moduleName} with value {data.Data}");
            string keyblockFile = null;
            if (data.Data != null) keyblockFile = data.Data.ToString();
            _keyblock.Prepare(keyblockFile);
            lock (_syncObject)
            {
                _activeClients.ForEach(c => c.RefreshKeyblock(keyblockFile));
            }
        }

        bool ShouldWeProcessNewData(string moduleName, DataType dataType)
        {
            _logger.Debug($"Validate if we need to handle {dataType} from {moduleName}");
            if (State != ModuleState.Running)
            {
                _logger.Debug($"Current state {State}, so we don't handle new data");
                return false;
            }
            if (dataType != DataType.KeyBlock)
            {
                _logger.Debug($"{dataType} is not relevant for us");
                return false;
            }
            return true;
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
            try
            {
                NewCamdApi[] clients;
                lock (_syncObject)
                {
                    clients = _activeClients.ToArray();
                }
                foreach (var client in clients)
                {
                    client.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to close one of the clients");
                _logger.Debug("CloseClients", ex);
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

