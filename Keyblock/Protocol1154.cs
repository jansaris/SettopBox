using System.Linq;
using System.Text;
using log4net;

namespace Keyblock
{
    public class Protocol1154 : IProtocol
    {
        private readonly Settings _settings;
        private readonly ILog _logger;

        public Protocol1154(Settings settings, ILog logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public string GetCertificate(X509CertificateRequest certificateRequest)
        {
            var msg = $"{_settings.MessageFormat}~{_settings.ClientId}~getCertificate~{_settings.Company}~NA~NA~{certificateRequest}~{_settings.Common}~{_settings.Address}~ ~{_settings.City}~{_settings.Province}~{_settings.ZipCode}~{_settings.Country}~{_settings.Telephone}~{_settings.Email}~{_settings.MachineId}~{_settings.ChallengePassword}~";
            return msg;
        }

        public string GetSessionKey()
        {
            return $"{_settings.MessageFormat}~{_settings.ClientId}~CreateSessionKey~{_settings.Company}~{_settings.MachineId}~";
        }

        public byte[] SaveEncryptedPassword(string timestamp, string ski, string password, byte[] sessionKey)
        {
            var unencryptedMsgPart = $"{_settings.MessageFormat}~{_settings.Company}~{timestamp}~{_settings.MachineId}~";
            var encryptedMsgPart = $"{_settings.ClientId}~SaveEncryptedPassword~{_settings.Company}~{ski}~64~{password}~";

            _logger.Debug($"Save encryption password: {unencryptedMsgPart}{encryptedMsgPart}");

            var msg = Encoding.ASCII.GetBytes(unencryptedMsgPart).ToList();
            msg.AddRange(RC4.Encrypt(sessionKey, Encoding.ASCII.GetBytes(encryptedMsgPart)));

            return msg.ToArray();
        }

        public byte[] GetEncryptedPassword(string timestamp, string ski, byte[] sessionKey)
        {
            var unencryptedMsgPart = $"{_settings.MessageFormat}~{_settings.Company}~{timestamp}~{_settings.MachineId}~";
            var encryptedMsgPart = $"{_settings.ClientId}~GetEncryptedPassword~{_settings.Company}~{ski}~";

            _logger.Debug($"Get encryption password: {unencryptedMsgPart}{encryptedMsgPart}");

            var msg = Encoding.ASCII.GetBytes(unencryptedMsgPart).ToList();
            msg.AddRange(RC4.Encrypt(sessionKey, Encoding.ASCII.GetBytes(encryptedMsgPart)));

            return msg.ToArray();
        }

        public byte[] LoadKeyBlock(string timestamp, string ski, string hash, byte[] sessionKey)
        {
            var unencryptedMsgPart = $"{_settings.MessageFormat}~{_settings.Company}~{timestamp}~{_settings.MachineId}~";
            var encryptedMsgPart = $"{_settings.ClientId}~GetAllChannelKeys~{_settings.Company}~{ski.ToUpper()}~{hash}~{_settings.MachineId}~ ~ ~";

            var msg = Encoding.ASCII.GetBytes(unencryptedMsgPart).ToList();
            msg.AddRange(RC4.Encrypt(sessionKey, Encoding.ASCII.GetBytes(encryptedMsgPart)));

            _logger.Debug($"GetAllChannelKeys from server: {unencryptedMsgPart}{encryptedMsgPart}");

            return msg.ToArray();
        }

        public byte[] GetVksConnectionInfo(string timestamp, string ski, byte[] sessionKey)
        {
            return null;
        }
    }
}
