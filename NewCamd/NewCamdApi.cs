using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using log4net;
using NewCamd.Encryption;

namespace NewCamd
{
    public class NewCamdApi : IDisposable
    {
        //Constructor variables
        readonly ILog _logger;
        readonly Settings _settings;
        readonly NewCamdCommunication _communication;
        readonly Keyblock _keyblock;
        readonly EncryptionHelpers _crypto;

        public EventHandler Closed;
        public string Name => _communication?.Name;

        public NewCamdApi(ILog logger, Settings settings, EncryptionHelpers crypto, NewCamdCommunication communication, Keyblock keyblock)
        {
            _logger = logger;
            _settings = settings;
            _crypto = crypto;
            _communication = communication;
            _keyblock = keyblock;
            _communication.MessageReceived += (sender, message) => ReceiveMessage(message);
            _communication.Closed += (sender, message) => Closed?.Invoke(this, null);
        }

        public void HandleClient(TcpClient client)
        {
            _logger.Debug("Start handling new client");
            _keyblock.Prepare();
            _communication.Start(client);
        }

        void ReceiveMessage(NewCamdMessage message)
        {
            if (message == null) return;
            _logger.Debug($"Handle message: {message.Type} from {Name}");
            switch (message.Type)
            {
                case NewCamdMessageType.MsgClient2ServerLogin:
                    Login(message);
                    break;
                case NewCamdMessageType.MsgCardDataReq:
                    CardData(message);
                    break;
                case NewCamdMessageType.MsgKeepalive:
                    _logger.Debug($"{Name} - Keep connection alive");
                    _communication.SendMessage("Keep alive", message);
                    break;
                case NewCamdMessageType.MsgKeyblockReq1:
                case NewCamdMessageType.MsgKeyblockReq2:
                    KeyBlock(message);
                    break;
                default:
                    _logger.Error($"Unable to handle {message.Type}");
                    Dispose();
                    break;
            }
        }

        void KeyBlock(NewCamdMessage message)
        {
            _logger.Info($"{Name} - Give keyblock info");
            var header = new byte[] {(byte) message.Type, 1, 1};
            var keyInfo = _keyblock.DecryptBlock(message.Type, message.Data);
            message.Data = header.Concat(keyInfo).ToArray();
            _communication.SendMessage("Key info", message);
        }

        void CardData(NewCamdMessage message)
        {
            _logger.Info($"{Name} - Request card info");
            message.Type = NewCamdMessageType.MsgCardData;
            message.Data = new byte[26];
            //Provide CAID
            message.Data[4] = 0x56;
            message.Data[5] = 0x01;

            message.Data[14] = 1; //Set number of cards
            message.Data[17] = 1; //Set provider ID of card 1
            
            _communication.SendMessage("Card info", message);
        }

        void Login(NewCamdMessage message)
        {
            string username;
            string encryptedPassword;
            try
            {
                const int header = 3;
                var splitter = Array.IndexOf(message.Data, (byte)0, header);
                username = Encoding.ASCII.GetString(message.Data.Skip(header).Take(splitter - header).ToArray());
                splitter++;
                encryptedPassword = Encoding.ASCII.GetString(message.Data.Skip(splitter).Take(message.Data.Length - splitter - 1).ToArray());
            }
            catch (Exception ex)
            {
                _logger.Warn($"Couldn't read the login credentials from {Name}");
                _logger.Debug("Exception at login", ex);
                Dispose();
                return;
            }

            var expectedPassword = _crypto.UnixEncrypt(_settings.Password, "$1$abcdefgh$");
            var loginValid = _settings.Username.Equals(username) && expectedPassword.Equals(encryptedPassword);
            message.Type = loginValid ? NewCamdMessageType.MsgClient2ServerLoginAck : NewCamdMessageType.MsgClient2ServerLoginNak;
            _logger.Info($"{Name} - Login is {message.Type}");
            message.Data = new byte[3];
            _communication.SendMessage("Login response", message);
            if (!loginValid) return;
            _communication.UpdateKeyBlock(encryptedPassword);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        bool _disposing;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || _disposing) return;
            _disposing = true;
            _communication.Dispose();
        }

        public void RefreshKeyblock()
        {
            _keyblock.Prepare();
        }
    }
}