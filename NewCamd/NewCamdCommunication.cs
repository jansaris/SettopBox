using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using log4net;
using NewCamd.Encryption;

namespace NewCamd
{
    public class NewCamdCommunication : IDisposable
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
        byte[] _keyblock;
        public string Name { get; private set; }

        public EventHandler Closed;
        public EventHandler<NewCamdMessage> MessageReceived;

        public NewCamdCommunication(ILog logger, Settings settings, EncryptionHelpers crypto)
        {
            _logger = logger;
            _settings = settings;
            _crypto = crypto;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Start(TcpClient client)
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

        public void SendMessage(string logMessage, NewCamdMessage message)
        {
            var log = $"{logMessage}: {message.Type}";
            var data = ConvertToEncryptedMessage(message);
            SendMessage(log, data);
        }

        void HandleMessagesLoop()
        {
            while (_client.Connected)
            {
                _logger.Debug($"Wait for new message from {Name}");
                var message = ReceiveMessage();
                if (message == null) Dispose();
                MessageReceived?.Invoke(this, message);
            }
            _logger.Info($"Stop handling messages, connection with {Name} is closed");
        }

        byte[] InitializeKeys()
        {
            _keyblock = _crypto.CreateKeySpread(_settings.GetDesArray());
            //Send an empty array of 14 zero's to the client
            return new byte[14];
        }

        static byte XorSum(IReadOnlyList<byte> buffer)
        {
            byte res = 0;
            foreach (var b in buffer)
            {
                res ^= b;
            }
            return res;
        }

        public void UpdateKeyBlock(string encryptedPassword)
        {
            var random = _settings.GetDesArray();
            for (var i = 0; i < encryptedPassword.Length; i++)
            {
                random[i % 14] ^= (byte)encryptedPassword[i];
            }
            _keyblock = _crypto.CreateKeySpread(random);
        }

        NewCamdMessage ReceiveMessage()
        {
            try
            {
                var buffer = new byte[_client.ReceiveBufferSize];
                //Read first two bytes to get the message length
                var len = _stream.Read(buffer, 0, 2);
                if (len != 2) throw new InvalidNewcamdMessage($"Expected to receive 2 bytes from {Name}, but got {len} bytes instaed");
                var messageLength = ((buffer[0] << 8) | buffer[1]) & 0xFFFF;
                _logger.Debug($"Received {len} bytes from {Name} with a new message length {messageLength}");
                if (messageLength > NewCamdMessage.Size) throw new InvalidNewcamdMessage($"Message from {Name} too long ({len} vs {NewCamdMessage.Size})");
                len = _stream.Read(buffer, 0, messageLength);
                if (len < messageLength) throw new InvalidNewcamdMessage($"Message from {Name} too short ({len} vs {messageLength})");
                _logger.Debug($"Received {len} bytes from {Name} with encrypted data");
                return ParseMessage(buffer.Take(len).ToArray());
            }
            catch (Exception ex)
            {
                _logger.Error($"Client disconnected or didn't respond withing {_settings.MaxWaitTimeInMs}ms", ex);
                return null;
            }
        }

        NewCamdMessage ParseMessage(byte[] buffer)
        {
            _logger.Debug("Read TripleDES Initialization Vector from encrypted message");
            var ivec = buffer.Skip(buffer.Length - 8).Take(8).ToArray();
            _logger.Debug("Decrypt the rest of the message");
            var decryptedData = _crypto.Decrypt(buffer, buffer.Length - 8, _keyblock, ivec);
            _logger.Debug("Parse decrypted message");
            var len = (((decryptedData[3 + NewCamdMessage.HeaderLength] << 8) | decryptedData[4 + NewCamdMessage.HeaderLength]) & 0x0FFF) + 3;
            if (len > decryptedData.Length) throw new InvalidNewcamdMessage($"Decryption of the message from {Name} failed");
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

        byte[] ConvertToEncryptedMessage(NewCamdMessage message)
        {
            _logger.Debug($"Prepare message headers for {Name}");
            var prepareData = new List<byte>();
            prepareData.Add((byte)(message.MessageId >> 8));
            prepareData.Add((byte)(message.MessageId & 0xFF));
            prepareData.Add((byte)(message.ServiceId >> 8));
            prepareData.Add((byte)(message.ServiceId & 0xFF));
            prepareData.Add((byte)(message.ProviderId >> 16));
            prepareData.Add((byte)((message.ProviderId >> 8) & 0xFF));
            prepareData.Add((byte)(message.ProviderId & 0xFF));
            prepareData.Add(0);
            prepareData.Add(0);
            prepareData.Add(0);

            _logger.Debug($"Correct message headers for {Name}");
            message.Data[1] = (byte)((message.Data[1] & 240) | (((message.Data.Length - 3) >> 8) & 255));
            message.Data[2] = (byte)((message.Data.Length - 3) & 255);
            _logger.Debug($"Copy {message.Data.Length} bytes into the buffer for {Name}");
            prepareData.AddRange(message.Data);
            //Fill up
            while (prepareData.Count % 8 != 7) prepareData.Add(0);

            _logger.Debug($"Encrypt data before sending to {Name}");

            var padding = new byte[8];
            _random.NextBytes(padding);

            //fill up bytes with padding data at the end
            var bufferLen = prepareData.Count;
            var paddingLen = (8 - ((bufferLen - 2) % 8)) % 8;
            var prepareDataArray = prepareData.ToArray();
            Buffer.BlockCopy(padding, 0, prepareDataArray, bufferLen - paddingLen, paddingLen);
            prepareData = prepareDataArray.ToList();
            //Add checksum
            prepareData.Add(XorSum(prepareData.ToArray()));

            var ivec = new byte[8];
            _random.NextBytes(ivec);

            var dataToEncrypt = prepareData.ToArray();
            var encrypted = _crypto.Encrypt(dataToEncrypt, _keyblock, ivec).ToList();

            var dataToSend = new List<byte>();
            dataToSend.Add((byte)((encrypted.Count + ivec.Length) >> 8));
            dataToSend.Add((byte)((encrypted.Count + ivec.Length) & 0xFF));
            dataToSend.AddRange(encrypted);
            dataToSend.AddRange(ivec);

            return dataToSend.ToArray();
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