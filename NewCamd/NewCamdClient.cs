using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using log4net;
using log4net.Repository.Hierarchy;

namespace NewCamd
{
    class NewCamdClient : IDisposable
    {
        //Constructor variables
        readonly ILog _logger;
        readonly Settings _settings;
        readonly TripleDes _crypto;
        readonly byte[] _privateKey = new byte[14];
        readonly Random _random = new Random();
        readonly CancellationTokenSource _cancellationTokenSource;

        //Handle variables
        TcpClient _client;
        NetworkStream _stream;
        int _noDataCount;

        public EventHandler Closed;
        public string Name { get; private set; }

        public NewCamdClient(ILog logger, Settings settings, TripleDes crypto)
        {
            _logger = logger;
            _settings = settings;
            _crypto = crypto;
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
            HandleMessagesLoop();
        }

        void HandleMessagesLoop()
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

        void HandleMessage(byte[] bytes)
        {
            var val = (NewCamdMessage)bytes[0];
            var data = bytes.Skip(1).ToList();
            _logger.Info($"Handle message: {val} from {Name}");
            switch (val)
            {
                case NewCamdMessage.MsgClient2ServerLogin:
                    Login(data);
                    break;
                default:
                    _logger.Info($"Handle {val}");
                    break;
            }
        }

        bool ValidateMessage(byte[] bytes)
        {
            _logger.Debug("Validate message");
            if (bytes == null)
            {
                _logger.Error($"Received no valid message from {Name}");
                Dispose();
                return false;
            }

            if (bytes.Length == 0)
            {
                _logger.Warn($"Received no data from {Name}");
                if (++_noDataCount > 3)
                {
                    _logger.Error("Received no data 3 times, the client probably disconnected");
                    Dispose();
                }
                return false;
            }

            _logger.Debug("Validated message");
            _noDataCount = 0;
            return true;
        }

        void Login(List<byte> bytes)
        {
            string username;
            string encryptedPassword;

            if (!ParseLoginMessage(bytes, out username, out encryptedPassword))
            {
                _logger.Warn($"Couldn't read the login credentials from {Name}");
                Dispose();
            }
            var response = ValidateLogin(username, encryptedPassword);
            SendMessage("Login response", response);
        }

        NewCamdMessage ValidateLogin(string username, string encryptedPassword)
        {
            if (!_settings.Username.Equals(username))
            {
                _logger.Warn($"Login username {username} from {Name} is invalid");
                return NewCamdMessage.MsgClient2ServerLoginNak;
            }

            try
            {
                var password = _crypto.Decrypt(encryptedPassword, "$1$abcdefgh$");
                if (!_settings.Password.Equals(password))
                {
                    _logger.Warn($"Login password {password} from {Name} is invalid");
                    return NewCamdMessage.MsgClient2ServerLoginNak;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to validate the received username '{username}' and encrypted password '{encryptedPassword}' from {Name}", ex);
                return NewCamdMessage.MsgClient2ServerLoginNak;
            }
            return NewCamdMessage.MsgClient2ServerLoginAck;
        }

        bool ParseLoginMessage(List<byte> bytes, out string username, out string encryptedPassword)
        {
            username = string.Empty;
            encryptedPassword = string.Empty;
            try
            {
                var usernameEnd = bytes.IndexOf(0);
                if (usernameEnd <= 0)
                {
                    _logger.Error("Failed to find the username field in the login message");
                    return false;
                }
                var passwordStart = usernameEnd + 1;
                var passwordEnd = bytes.IndexOf(0, passwordStart);
                if (passwordEnd <= 0)
                {
                    _logger.Error("Failed to find the password field in the login message");
                    return false;
                }

                username = Encoding.ASCII.GetString(bytes.Take(usernameEnd).ToArray());
                encryptedPassword = Encoding.ASCII.GetString(bytes.Skip(passwordStart).Take(passwordEnd - passwordStart).ToArray());

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to parse the login details", ex);
                return false;
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
                    _logger.Warn($"Failed to receive a valid message from {Name} after {_settings.MaxWaitTimeInMs}ms");
                }
            }
            catch (Exception ex)
            {
                _logger.Error("An error occured while receiving data from the client", ex);
            }
            return result;
        }

        void SendMessage(string message, NewCamdMessage data)
        {
            message = $"{message}: {data}";
            SendMessage(message, new[] { (byte)data });
        }

        void SendMessage(string message, byte[] data)
        {
            _logger.Info($"Send '{message}' with {data.Length} bytes to {Name}");
            _stream.Write(data, 0, data.Length);
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