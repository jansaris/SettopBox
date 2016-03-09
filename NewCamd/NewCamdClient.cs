using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using log4net;

namespace NewCamd
{
    class NewCamdClient : IDisposable
    {
        //Constructor variables
        readonly ILog _logger;
        readonly Settings _settings;
        readonly byte[] _privateKey = new byte[14];
        readonly Random _random = new Random();
        readonly CancellationTokenSource _cancellationTokenSource;

        //Handle variables
        TcpClient _client;
        NetworkStream _stream;

        public EventHandler Closed;
        public string Name { get; private set; }

        public NewCamdClient(ILog logger, Settings settings)
        {
            _logger = logger;
            _settings = settings;
            _random.NextBytes(_privateKey);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Handle(TcpClient client)
        {
            _logger.Debug("Start handling new client");
            _client = client;
            Name = _client.Client.RemoteEndPoint.ToString();
            _stream = _client.GetStream();
            _logger.Info($"Accepted client {Name}");
            SendMessage("Login key", _privateKey);
            HandleAllMessages();
        }

        void HandleAllMessages()
        {
            while (_client.Connected)
            {
                _logger.Debug($"Wait for new message from {Name}");
                var bytes = ReceiveMessage();
                HandleMessage(bytes);
            }
            _logger.Info($"Stop handling messages, connection with {Name} is closed");
        }

        void HandleMessage(byte[] bytes)
        {
            _logger.Info("Handle message");

            if (bytes == null)
            {
                _logger.Warn($"Received no valid message from {Name}, Disconnect client");
                Dispose();
                return;
            }

            var val = (NewCamdMessage)bytes[0];
            switch (val)
            {
                default:
                    _logger.Info($"Handle {val}");
                    break;
            }
        }

        byte[] ReceiveMessage()
        {
            var buffer = new byte[_client.ReceiveBufferSize];
            //Create read
            var byteTask = _stream.ReadAsync(buffer, 0, buffer.Length, _cancellationTokenSource.Token);
            //And wait for response from the server
            byteTask.Wait(_settings.MaxWaitTimeInMs, _cancellationTokenSource.Token);
            if (!byteTask.IsCompleted)
            {
                _logger.Warn($"Failed to receive a valid message from {Name}, state: {byteTask.Status}");
                _cancellationTokenSource.Cancel();
                byteTask.Dispose();
                return null;
            }
            _logger.Debug($"Received {byteTask.Result} from the client");
            var val = (NewCamdMessage)buffer[0];
            _logger.Debug($"Received message '{val}'");
            return buffer.Take(byteTask.Result).ToArray();
        }

        void SendMessage(string message, byte[] privateKey)
        {
            _logger.Info($"Send '{message}' with {privateKey.Length} bytes");
            _stream.Write(privateKey, 0, privateKey.Length);
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();

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