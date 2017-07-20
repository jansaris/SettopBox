using System;
using System.Linq;
using System.Text;
using log4net;

namespace Keyblock
{
    public class Protocol1155 : IProtocol
    {
        private readonly Settings _settings;
        private readonly ILog _logger;
        private string machineId = "0002024C7138";
        private string clientId = "";

        public Protocol1155(Settings settings, ILog logger)
        {
            _settings = settings;
            _logger = logger;

            GenerateClientId(17);
        }

        public string GetCertificate(X509CertificateRequest certificateRequest)
        {
            var msg = $"{_settings.MessageFormat}~{clientId}~getCertificate~{_settings.Company}~NA~NA~{certificateRequest}~{_settings.Common}~{_settings.Address}~ ~{_settings.City}~{_settings.Province}~{_settings.ZipCode}~{_settings.Country}~{_settings.Telephone}~{_settings.Email}~{machineId}~{_settings.ChallengePassword}~";
            return msg;
        }

        public string GetSessionKey()
        {
            return $"{_settings.MessageFormat}~{clientId}~CreateSessionKey~{_settings.Company}~{machineId}~";
        }

        public byte[] SaveEncryptedPassword(string timestamp, string ski, string password, byte[] sessionKey)
        {
            var unencryptedMsgPart = $"{_settings.MessageFormat}~{_settings.Company}~{timestamp}~{machineId}~";
            var encryptedMsgPart = $"{clientId}~SaveEncryptedPassword~{_settings.Company}~{ski}~64~{password}~";

            _logger.Debug($"Save encryption password: {unencryptedMsgPart}{encryptedMsgPart}");

            var msg = Encoding.ASCII.GetBytes(unencryptedMsgPart).ToList();
            msg.AddRange(RC4.Encrypt(sessionKey, Encoding.ASCII.GetBytes(encryptedMsgPart)));

            return msg.ToArray();
        }

        public byte[] GetEncryptedPassword(string timestamp, string ski, byte[] sessionKey)
        {
            var unencryptedMsgPart = $"{_settings.MessageFormat}~{_settings.Company}~{timestamp}~{machineId}~";
            var encryptedMsgPart = $"{clientId}~GetEncryptedPassword~{_settings.Company}~{ski}~";

            _logger.Debug($"Get encryption password: {unencryptedMsgPart}{encryptedMsgPart}");

            var msg = Encoding.ASCII.GetBytes(unencryptedMsgPart).ToList();
            msg.AddRange(RC4.Encrypt(sessionKey, Encoding.ASCII.GetBytes(encryptedMsgPart)));

            return msg.ToArray();
        }

        public byte[] LoadKeyBlockNew(string timestamp, string ski, string hash, byte[] sessionKey)
        {
            var unencryptedMsgPart = $"{_settings.MessageFormat}~00664~{_settings.Company}~{timestamp}~{machineId}~";
            var encryptedMsgPart = $"{clientId}~GetCurrentChannelKeys~{_settings.Company}~{ski.ToUpper()}~{hash}~{machineId}~ ~ ~";

            var msg = Encoding.ASCII.GetBytes(unencryptedMsgPart).ToList();
            msg.AddRange(RC4.Encrypt(sessionKey, Encoding.ASCII.GetBytes(encryptedMsgPart)));

            _logger.Debug($"GetAllChannelKeys from server: {unencryptedMsgPart}{encryptedMsgPart}");

            return msg.ToArray();
        }

        public byte[] LoadKeyBlock(string timestamp, string ski, string hash, byte[] sessionKey)
        {
            //var unencryptedMsgPart = $"{_settings.MessageFormat}~00664~{_settings.Company}~{timestamp}~{machineId}~";
            //var encryptedMsgPart = $"{clientId}~GetAllChannelKeys~{_settings.Company}~{ski}~{hash}~{machineId}~ ~";
            //1155~00664~KPN-GHM~07/20/2017 19:02:52~0002024C7138~
            var unencryptedMsgPart = $"{_settings.MessageFormat}~{_settings.Company}~{timestamp}~{machineId}~";
            var encryptedMsgPart = $"{clientId}~GetAllChannelKeys~{_settings.Company}~{ski}~{hash}~{machineId}~ ~ ~";

            var msg = Encoding.ASCII.GetBytes(unencryptedMsgPart).ToList();
            msg.AddRange(RC4.Encrypt(sessionKey, Encoding.ASCII.GetBytes(encryptedMsgPart)));

            _logger.Debug($"GetAllChannelKeys from server: {unencryptedMsgPart}{encryptedMsgPart}");

            return msg.ToArray();
        }

        public void GenerateClientId(int length)
        {

            var text = "";
            var possible = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            for (var i = 0; i < length; i++)
                text += possible[random.Next(0, possible.Length)];

            clientId = text;
            
            //var buf = new byte[length/2];
            //new Random().NextBytes(buf);
            //clientId = string.Empty;
            //foreach (var b in buf)
            //{
            //    clientId += (b & 0xFF).ToString("x2");
            //}
        }
    }
}