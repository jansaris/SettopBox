using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using log4net;
using NewCamd.Encryption;

namespace NewCamd
{
    class NewCamdApi : IDisposable
    {
        //Constructor variables
        readonly ILog _logger;
        readonly Settings _settings;
        readonly NewCamdCommunication _communication;
        readonly EncryptionHelpers _crypto;

        public EventHandler Closed;
        public string Name => _communication?.Name;

        public NewCamdApi(ILog logger, Settings settings, EncryptionHelpers crypto, NewCamdCommunication communication)
        {
            _logger = logger;
            _settings = settings;
            _crypto = crypto;
            _communication = communication;
            _communication.MessageReceived += (sender, message) => ReceiveMessage(message);
            _communication.Closed += (sender, message) => Closed?.Invoke(this, null);
        }

        public void HandleClient(TcpClient client)
        {
            _logger.Debug("Start handling new client");
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
                    MessageCardData(message);
                    break;
                case NewCamdMessageType.MsgKeepalive:
                    _logger.Debug($"{Name} - Keep connection alive");
                    _communication.SendMessage("Keep alive", message);
                    break;
                default:
                    _logger.Info($"Handle {message.Type}");
                    break;
            }
        }

        void MessageCardData(NewCamdMessage message)
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
            _communication.Dispose();
        }
    }
}