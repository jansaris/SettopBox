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
        int _noDataCount;

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
                if (ValidateMessage(bytes))
                {
                    HandleMessage(bytes);
                }
            }
            _logger.Info($"Stop handling messages, connection with {Name} is closed");
        }

        bool ValidateMessage(byte[] bytes)
        {
            _logger.Debug("Validate message");
            if (bytes == null)
            {
                _logger.Error($"Received no valid message from {Name}, Disconnect client");
                Dispose();
                return false;
            }

            if (bytes.Length == 0)
            {
                _logger.Warn($"Received no data from {Name}");
                _noDataCount++;
                if (_noDataCount > 3)
                {
                    _logger.Error("Received no data 3 times, the client probably disconnected");
                    Dispose();
                }
                return false;
            }

            _logger.Info("Validated message");
            _noDataCount = 0;
            return true;
        }

        void HandleMessage(byte[] bytes)
        {
            _logger.Info("Handle message");
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
            byte[] result = null;
            try
            {
                var buffer = new byte[_client.ReceiveBufferSize];
                //Create read
                var byteTask = _stream.ReadAsync(buffer, 0, buffer.Length, _cancellationTokenSource.Token);
                //And wait for response from the server
                byteTask.Wait(_settings.MaxWaitTimeInMs, _cancellationTokenSource.Token);
                if (byteTask.IsCompleted)
                {
                    //Task succesfully completed, read data
                    _logger.Debug($"Received {byteTask.Result} from the client");
                    result = buffer.Take(byteTask.Result).ToArray();
                }
                else
                {
                    _logger.Warn($"Failed to receive a valid message from {Name}, state: {byteTask.Status}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error("An error occured while receiving data from the client", ex);
            }
            return result;
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