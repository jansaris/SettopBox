using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using log4net;

namespace NewCamd
{
    class NewCamdClient : IDisposable
    {
        readonly ILog _logger;
        readonly Settings _settings;
        TcpClient _client;
        public EventHandler Closed;
        NetworkStream _stream;

        public string Name { get; private set; }

        public NewCamdClient(ILog logger, Settings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        public void Handle(TcpClient client)
        {
            _logger.Debug("Start handling new client");
            _client = client;
            Name = _client.Client.RemoteEndPoint.ToString();
            _stream = _client.GetStream();
            _logger.Info($"Accepted client {Name}");
            HandleMessage();
        }

        void HandleMessage()
        {
            var buffer = new byte[_client.ReceiveBufferSize];
            var bytes = _stream.Read(buffer, 0, buffer.Length);
            _logger.Debug($"Received {bytes} from the client");
            var val = (NewCamdMessage) buffer[0];
            _logger.Debug($"Received message '{val}'");
        }

        public void Dispose()
        {
            if (_client == null)
            {
                _logger.Debug("Client connection was already closed");
            }
            else
            {
                _logger.Info($"Close connection with {Name}");
                _client?.Close();
            }
            Closed?.Invoke(this, null);
        }
    }
}