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
        readonly Random _random = new Random();
        readonly CancellationTokenSource _cancellationTokenSource;

        //Handle variables
        TcpClient _client;
        NetworkStream _stream;
        public byte[] _keyblock;

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
            message.Type = ValidateLogin(username, encryptedPassword);
            message.Data = new byte[3];
            SendMessage("Login response", message);
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
                if (messageLength > NewCamdMessage.Size) throw new InvalidNewcamdMessage($"Message from {Name} too long ({len} vs {NewCamdMessage.Size})");
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

        void SendMessage(string logMessage, NewCamdMessage message)
        {
            var log = $"{logMessage}: {message.Type}";
            var data = ConvertToEncryptedMessage(message);
            SendMessage(log, data);
        }

        public byte[] ConvertToEncryptedMessage(NewCamdMessage message)
        {
            _logger.Debug($"Prepare send data of type {message.Type} for encryption for {Name}");
            var buffer = new byte[NewCamdMessage.Size];
            _logger.Debug($"Copy {message.Data.Length} bytes into the buffer for {Name}");
            Buffer.BlockCopy(message.Data,0,buffer, NewCamdMessage.HeaderLength + 4, message.Data.Length);
            _logger.Debug($"Prepare header information for {Name}");

            buffer[NewCamdMessage.HeaderLength + 4 + 1] = (byte)((message.Data[1] & 0xF0) | (((message.Data.Length - 3) >> 8) & 0x0F));
            buffer[NewCamdMessage.HeaderLength + 4 + 2] = (byte)((message.Data.Length - 3) & 0xFF);

            buffer[2] = (byte)(message.MessageId >> 8);
            buffer[3] = (byte)(message.MessageId & 0xFF);
            buffer[4] = (byte)(message.ServiceId >> 8);
            buffer[5] = (byte)(message.ServiceId & 0xFF);
            buffer[6] = (byte)(message.ProviderId >> 16);
            buffer[7] = (byte)((message.ProviderId >> 8) & 0xFF);
            buffer[8] = (byte)(message.ProviderId & 0xFF);

            _logger.Debug($"Encrypt data before sending to {Name}");

            /*
            memset(buffer + 2, 0, NEWCAMD_HDR_LEN + 2);
	memcpy(buffer + NEWCAMD_HDR_LEN + 4, data, data_len);

	buffer[NEWCAMD_HDR_LEN + 4 + 1] = (data[1] & 0xF0) | (((data_len - 3) >> 8) & 0x0F);
	buffer[NEWCAMD_HDR_LEN + 4 + 2] = (data_len - 3) & 0xFF;

	buffer[2] = msg_id >> 8;
	buffer[3] = msg_id & 0xFF;
	buffer[4] = service_id >> 8;
	buffer[5] = service_id & 0xFF;
	buffer[6] = provider_id >> 16;
	buffer[7] = (provider_id >> 8) & 0xFF;
	buffer[8] = provider_id & 0xFF;
	
	LOG(DEBUG, "[NEWCAMD] Send message msgid: %d, serviceid: %d, providerid: %d, length: %d", msg_id, service_id, provider_id, data_len + 2 + NEWCAMD_HDR_LEN);
    */
            var padding = new byte[8];
            _random.NextBytes(padding);

            var bufferLen = message.Data.Length + 4 + NewCamdMessage.HeaderLength;
            var paddingLen = (8 - ((bufferLen - 1)%8))%8;
            Buffer.BlockCopy(padding, 0, buffer, bufferLen, paddingLen);
            bufferLen += paddingLen;
            buffer[bufferLen] = XorSum(buffer.Skip(2).ToArray());
            bufferLen++;

            var ivec = new byte[8];
            _random.NextBytes(ivec);

            Buffer.BlockCopy(ivec, 0, buffer, bufferLen, ivec.Length);
            bufferLen += 8;

            var dataToEncrypt = buffer.Skip(2).Take(bufferLen).ToArray();
            var encrypted = _crypto.Encrypt(dataToEncrypt, _keyblock, ivec);

            var dataToSend = new List<byte>();
            dataToSend.Add((byte)((bufferLen - 2) >> 8));
            dataToSend.Add((byte)((bufferLen - 2) & 0xFF));
            dataToSend.AddRange(encrypted);

            return dataToSend.ToArray();
            /*

	DES_cblock padding;
	buf_len = data_len + NEWCAMD_HDR_LEN + 4;
	padding_len = (8 - ((buf_len - 1) % 8)) % 8;

	DES_random_key(&padding);
	memcpy(buffer + buf_len, padding, padding_len);
	buf_len += padding_len;
	buffer[buf_len] = xor_sum(buffer + 2, buf_len - 2);
	buf_len++;

	DES_cblock ivec;
	DES_random_key(&ivec);
	memcpy(buffer + buf_len, ivec, sizeof(ivec));
	print_hex("sended data", buffer + 2, data_len + NEWCAMD_HDR_LEN + 4);
	DES_ede2_cbc_encrypt(buffer + 2, buffer + 2, buf_len - 2, &c->ks1, &c->ks2, (DES_cblock *)ivec, DES_ENCRYPT);

	buf_len += sizeof(DES_cblock);
	buffer[0] = (buf_len - 2) >> 8;
	buffer[1] = (buf_len - 2) & 0xFF;
            */
        }

        public byte XorSum(byte[] buffer)
        {
            byte res = 0;
            int i;

            for (i = 0; i < buffer.Length; i++)
                res ^= buffer[i];

            return res;
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