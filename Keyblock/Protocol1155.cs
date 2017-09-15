using System.Linq;
using System.Text;
using log4net;

namespace Keyblock
{
    public class Protocol1155 : IProtocol
    {
        private readonly Settings _settings;
        private readonly ILog _logger;

        public Protocol1155(Settings settings, ILog logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public string GetCertificate(X509CertificateRequest certificateRequest)
        {
            var msg = $"{_settings.ClientId}~getCertificate~{_settings.Company}~NA~NA~{certificateRequest}~{_settings.Common}~{_settings.Address}~ ~{_settings.City}~{_settings.Province}~{_settings.ZipCode}~{_settings.Country}~{_settings.Telephone}~{_settings.Email}~{_settings.MachineId}~{_settings.ChallengePassword}~";
            msg = Add1155Header(msg);
            return msg;
        }

        public string GetSessionKey()
        {
            var msg = $"{_settings.ClientId}~CreateSessionKey~{_settings.Company}~{_settings.MachineId}~";
            msg = Add1155Header(msg);
            return msg;
        }

        public byte[] GetEncryptedPassword(string timestamp, string ski, byte[] sessionKey)
        {
            var unencryptedMsgPart = $"{_settings.Company}~{timestamp}~{_settings.MachineId}~";
            var encryptedMsgPart = $"{_settings.ClientId}~GetEncryptedPassword~{_settings.Company}~{ski}~";

            return Rc4Encrypt(unencryptedMsgPart, encryptedMsgPart, sessionKey, "Get encryption password");
        }

        public byte[] SaveEncryptedPassword(string timestamp, string ski, string password, byte[] sessionKey)
        {
            var unencryptedMsgPart = $"{_settings.Company}~{timestamp}~{_settings.MachineId}~";
            var encryptedMsgPart = $"{_settings.ClientId}~SaveEncryptedPassword~{_settings.Company}~{ski}~64~{password}~";

            return Rc4Encrypt(unencryptedMsgPart, encryptedMsgPart, sessionKey, "Save encryption password");
        }

        public byte[] LoadKeyBlock(string timestamp, string ski, string hash, byte[] sessionKey)
        {
            var unencryptedMsgPart = $"{_settings.Company}~{timestamp}~{_settings.MachineId}~";
            var encryptedMsgPart = $"{_settings.ClientId}~GetAllChannelKeys~{_settings.Company}~{ski.ToUpper()}~{hash}~{_settings.MachineId}~ ~ ~";

            return Rc4Encrypt(unencryptedMsgPart, encryptedMsgPart, sessionKey, "GetAllChannelKeys from server");
        }

        public byte[] GetVksConnectionInfo(string timestamp, string ski, byte[] sessionKey)
        {
            var unencryptedMsgPart = $"{_settings.Company}~{timestamp}~{_settings.MachineId}~";
            var encryptedMsgPart = $"{_settings.ClientId}~GetVKSConnectionInfo~{_settings.Company}~{ski.ToUpper()}~";

            return Rc4Encrypt(unencryptedMsgPart, encryptedMsgPart, sessionKey, "GetVKSConnectionInfo");
        }

        private byte[] Rc4Encrypt(string unencryptedMsgPart, string encryptedMsgPart, byte[] sessionKey, string debugMessage)
        {
            var encryptedData = RC4.Encrypt(sessionKey, Encoding.ASCII.GetBytes(encryptedMsgPart));
            unencryptedMsgPart = Add1155Header(unencryptedMsgPart, encryptedData.Length);

            _logger.Debug($"{debugMessage}: {unencryptedMsgPart}{encryptedMsgPart}");

            var msg = Encoding.ASCII.GetBytes(unencryptedMsgPart).ToList();
            msg.AddRange(encryptedData);

            return msg.ToArray();
        }

        private string Add1155Header(string message, int encryptedBytesCount = 0)
        {
            var length = Encoding.ASCII.GetByteCount("1155~00000~") + Encoding.ASCII.GetByteCount(message) + encryptedBytesCount;
            return $"1155~{length:D5}~{message}";
        }
    }
}