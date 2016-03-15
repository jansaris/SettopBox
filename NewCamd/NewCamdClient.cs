using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using log4net;
using NewCamd.Encryption;

namespace NewCamd
{
    class NewCamdClient : IDisposable
    {
        //Constructor variables
        readonly ILog _logger;
        readonly Settings _settings;
        readonly EncryptionHelpers _crypto;
        readonly CancellationTokenSource _cancellationTokenSource;

        //Handle variables
        TcpClient _client;
        NetworkStream _stream;
        public byte[] _keyblock;

        const int NewcamdMsgSize = 400;

        public EventHandler Closed;
        public string Name { get; private set; }

        public NewCamdClient(ILog logger, Settings settings, EncryptionHelpers crypto)
        {
            _logger = logger;
            _settings = settings;
            _crypto = crypto;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public byte[] InitializeKeys()
        {
            _keyblock = _crypto.CreateKeySpread(_settings.GetDesArray());
            //Send an empty array of 14 zero's to the client
            return new byte[14];
        }

        public void Handle(TcpClient client)
        {
            _logger.Debug("Start handling new client");
            _client = client;
            Name = _client.Client.RemoteEndPoint.ToString();
            _stream = _client.GetStream();
            _stream.ReadTimeout = _settings.MaxWaitTimeInMs;
            _logger.Info($"Accepted client {Name}");
            SendMessage("Login key", InitializeKeys());
            HandleMessagesLoop();
        }

        void HandleMessagesLoop()
        {
            while (_client.Connected)
            {
                _logger.Debug($"Wait for new message from {Name}");
                var message = ReceiveMessage();
                if (message == null || !HandleMessage(message)) Dispose();
            }
            _logger.Info($"Stop handling messages, connection with {Name} is closed");
        }

        bool HandleMessage(NewCamdMessage message)
        {
            _logger.Info($"Handle message: {message.Type} from {Name}");
            switch (message.Type)
            {
                case NewCamdMessageType.MsgClient2ServerLogin:
                    Login(message);
                    break;
                default:
                    _logger.Info($"Handle {message.Type}");
                    return false;
            }
            return true;
        }

        void Login(NewCamdMessage message)
        {
            string username;
            string encryptedPassword;

            if (!ParseLoginMessage(message.Data.ToList(), out username, out encryptedPassword))
            {
                _logger.Warn($"Couldn't read the login credentials from {Name}");
                Dispose();
            }
            var response = ValidateLogin(username, encryptedPassword);
            SendMessage("Login response", response, message);
        }

        NewCamdMessageType ValidateLogin(string username, string encryptedPassword)
        {
            if (!_settings.Username.Equals(username))
            {
                _logger.Warn($"Login username {username} from {Name} is invalid");
                return NewCamdMessageType.MsgClient2ServerLoginNak;
            }

            var expected = _crypto.UnixEncrypt(_settings.Password, "$1$abcdefgh$");
            if (!expected.Equals(encryptedPassword))
            {
                _logger.Warn($"Login password {encryptedPassword} from {Name} is invalid");
                return NewCamdMessageType.MsgClient2ServerLoginNak;
            }
            
            return NewCamdMessageType.MsgClient2ServerLoginAck;
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

        NewCamdMessage ReceiveMessage()
        {
            try
            {
                var buffer = new byte[_client.ReceiveBufferSize];
                //Read first two bytes to get the message length
                var len = _stream.Read(buffer, 0, 2);
                if(len != 2) throw new InvalidNewcamdMessage($"Expected to receive 2 bytes from {Name}, but got {len} bytes instaed");
                var messageLength = ((buffer[0] << 8) | buffer[1]) & 0xFFFF;
                _logger.Debug($"Received {len} bytes from {Name} with a new message length {messageLength}");
                if (messageLength > NewcamdMsgSize) throw new InvalidNewcamdMessage($"Message from {Name} too long ({len} vs {NewcamdMsgSize})");
                len = _stream.Read(buffer, 0, messageLength);
                File.WriteAllBytes("decrypttest/loginMessage.dat", buffer);
                if(len < messageLength) throw new InvalidNewcamdMessage($"Message from {Name} too short ({len} vs {messageLength})");
                _logger.Debug($"Received {len} bytes from {Name} with encrypted data");
                return ParseMessage(buffer.Take(len).ToArray());
            }
            catch (Exception ex)
            {
                _logger.Error("An error occured while receiving data from the client", ex);
                return null;
            }
        }

        public NewCamdMessage ParseMessage(byte[] buffer)
        {
            _logger.Debug("Read TripleDES Initialization Vector from encrypted message");
            var ivec = buffer.Skip(buffer.Length - 8).Take(8).ToArray();
            _logger.Debug("Decrypt the rest of the message");
            var decryptedData = _crypto.Decrypt(buffer, buffer.Length - 8, _keyblock, ivec);
            _logger.Debug("Parse decrypted message");
            var len = (((decryptedData[3 + NewCamdMessage.HeaderLength] << 8) | decryptedData[4 + NewCamdMessage.HeaderLength]) & 0x0FFF) + 3;
            if(len > decryptedData.Length) throw new InvalidNewcamdMessage($"Decryption of the message from {Name} failed");
            var retValue = new NewCamdMessage
            {
                MessageId = ((decryptedData[0] << 8) | decryptedData[1]) & 0xFFFF,
                ServiceId = ((decryptedData[2] << 8) | decryptedData[3]) & 0xFFFF,
                ProviderId = decryptedData[4] << 16 | decryptedData[5] << 8 | decryptedData[6],
                Data = decryptedData.Skip(2 + NewCamdMessage.HeaderLength).Take(len).ToArray(),
                Type = (NewCamdMessageType)decryptedData[2 + NewCamdMessage.HeaderLength]
            };
            _logger.Debug($"[NEWCAMD] Received decrypted message msgid: {retValue.MessageId}, serviceid: {retValue.ServiceId}, providerid: {retValue.ProviderId}, length: {len}, type {retValue.Type} from {Name}");
            return retValue;
        }

        void SendMessage(string logMessage, NewCamdMessageType response, NewCamdMessage message)
        {
            var log = $"{logMessage}: {response}";
            SendMessage(log, new[] { (byte)response });
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