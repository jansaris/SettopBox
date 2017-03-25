using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using log4net;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;
using SharedComponents.Keyblock;

namespace Keyblock
{
    public class Keyblock
    {
        public bool IsValid { get; private set; }
        public DateTime? BlockValidFrom => IsValid ? _block.ValidFrom : (DateTime?)null;
        public DateTime? BlockValidTo => IsValid ? _block.ValidTo : (DateTime?)null;
        public DateTime? BlockRefreshAfter => IsValid ? _block.NeedsRefreshAfter : (DateTime?)null;

        //Filenames
        string SignedCertificateFile => Path.Combine(_settings.DataFolder, "SignedCert.der");
        public string KeyblockFile => Path.Combine(_settings.DataFolder, _settings.KeyblockFile);

        readonly ILog _logger;
        readonly Settings _settings;
        readonly SslTcpClient _sslClient;
        readonly X509CertificateRequest _certificateRequest;
        readonly Block _block;

        byte[] _sessionKey;
        string _timestamp;
        string _ski;
        byte[] _password;

        public Keyblock(Settings settings, SslTcpClient sslClient, ILog logger, X509CertificateRequest certificateRequest, Block block)
        {
            _settings = settings;
            _sslClient = sslClient;
            _logger = logger;
            _certificateRequest = certificateRequest;
            _block = block;
        }

        bool GetCertificate()
        {
            _logger.Debug("Generating message");
            var t64 = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            _settings.UpdateEmail($"{_settings.MachineId}.{t64}@{_settings.EmailHost}");
            _logger.Debug($"Using email: {_settings.Email}");
            _certificateRequest.Generate();
            var msg = $"{_settings.MessageFormat}~{_settings.ClientId}~getCertificate~{_settings.Company}~NA~NA~{_certificateRequest}~{_settings.Common}~{_settings.Address}~ ~{_settings.City}~{_settings.Province}~{_settings.ZipCode}~{_settings.Country}~{_settings.Telephone}~{_settings.Email}~{_settings.MachineId}~{_settings.ChallengePassword}~";

            _logger.Debug($"Requesting Certificate: {msg}");
            var response = _sslClient.SendAndReceive(msg, _settings.VcasServer, _settings.VcasPort);

            if (response == null || response.Length < 12) { return false; }
            var cert = new List<byte>(response).GetRange(12, (response.Length - 12)).ToArray();
            File.WriteAllBytes(SignedCertificateFile, cert);

            _logger.Info("Received Certificate");
            return true;
        }

        bool GetSessionKey()
        {
            var msg = $"{_settings.MessageFormat}~{_settings.ClientId}~CreateSessionKey~{_settings.Company}~{_settings.MachineId}~";
            _logger.Debug($"Requesting Session Key: {msg}");

            var response = _sslClient.SendAndReceive(msg, _settings.VcasServer, _settings.VcasPort);

            if (response == null) return false;
            _sessionKey = response.Skip(4).Take(16).ToArray();
            _timestamp = Encoding.ASCII.GetString(response.Skip(20).Take(19).ToArray());

            _logger.Info($"Session key obtained with timestamp: '{_timestamp}'");
            return true;
        }

        bool GenerateSki()
        {
            if (!File.Exists(SignedCertificateFile))
            {
                _logger.Warn($"Can't generate a SKI because there is no certiface at {SignedCertificateFile}");
                return false;
            }

            try
            {
                _logger.Debug($"Resolve SKI from {SignedCertificateFile}");
                var parser = new X509CertificateParser();
                var cert = parser.ReadCertificate(File.ReadAllBytes(SignedCertificateFile));
                var identifier = new SubjectKeyIdentifierStructure(cert.GetPublicKey());
                var bytes = identifier.GetKeyIdentifier();
                _ski = BytesAsHex(bytes);

                _logger.Debug($"Resolved SKI '{_ski}'");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to get the SKI from {SignedCertificateFile}", ex);
                return false;
            }
        }

        bool SaveEncryptedPassword()
        {
            _password = new byte[32];
            new Random().NextBytes(_password);

            var encodedBytes = RC4.Encrypt(_sessionKey, _password);
            var password = BytesAsHex(encodedBytes);
            
            var unencryptedMsgPart = $"{_settings.MessageFormat}~{_settings.Company}~{_timestamp}~{_settings.MachineId}~";
            var encryptedMsgPart = $"{_settings.ClientId}~SaveEncryptedPassword~{_settings.Company}~{_ski}~64~{password}~";

            _logger.Debug($"Save encryption password: {unencryptedMsgPart}{encryptedMsgPart}");

            var msg = Encoding.ASCII.GetBytes(unencryptedMsgPart).ToList();
            msg.AddRange(RC4.Encrypt(_sessionKey, Encoding.ASCII.GetBytes(encryptedMsgPart)));

            var response = _sslClient.SendAndReceive(msg.ToArray(), _settings.VcasServer, _settings.VcasPort + 1, false);

            if (response == null || response.Length < 8)
            {
                _logger.Error("Failed to SaveEncryptedPassword, no valid response!");
                return false;
            }
            
            _logger.Info($"SaveEncryptedPassword completed, size: {response.Length}");
            return true;
        }

        bool GetEncryptedPassword()
        {
            var unencryptedMsgPart = $"{_settings.MessageFormat}~{_settings.Company}~{_timestamp}~{_settings.MachineId}~";
            var encryptedMsgPart = $"{_settings.ClientId}~GetEncryptedPassword~{_settings.Company}~{_ski}~";

            _logger.Debug($"Get encryption password: {unencryptedMsgPart}{encryptedMsgPart}");

            var msg = Encoding.ASCII.GetBytes(unencryptedMsgPart).ToList();
            msg.AddRange(RC4.Encrypt(_sessionKey, Encoding.ASCII.GetBytes(encryptedMsgPart)));

            var response = _sslClient.SendAndReceive(msg.ToArray(), _settings.VcasServer, _settings.VcasPort + 1, false);

            if (response == null || response.Length < 8)
            {
                _logger.Error("Failed to GetEncryptedPassword, no valid response!");
                return false;
            }

            var encryptedPassword = response.Skip(4).Take(response.Length - 4).ToArray();
            var decrypted = RC4.Decrypt(_sessionKey, encryptedPassword);
            var passwordHex = Encoding.ASCII.GetString(decrypted.Skip(4).ToArray());

            _logger.Info($"GetEncryptedPassword completed: {passwordHex}");
            return true;
        }

        string GenerateSignedHash()
        {
            _logger.Debug($"Generate signed hash from {_timestamp}");
            var timestampBytes = Encoding.ASCII.GetBytes(_timestamp);
            
            // Use the generated key
            var sig = SignerUtilities.GetSigner(PkcsObjectIdentifiers.MD5WithRsaEncryption);
            _certificateRequest.Generate();
            sig.Init(true, _certificateRequest.KeyPair.Private);
            sig.BlockUpdate(timestampBytes,0, timestampBytes.Length);
            var signature = sig.GenerateSignature();
            //Return the hash as Hex
            var generated =  BytesAsHex(signature);

            _logger.Debug($"Generated signed hash from {generated}");
            return generated;
        }

        bool LoadKeyBlock()
        {
            var hash = GenerateSignedHash();

            var unencryptedMsgPart = $"{_settings.MessageFormat}~{_settings.Company}~{_timestamp}~{_settings.MachineId}~";
            var encryptedMsgPart = $"{_settings.ClientId}~GetAllChannelKeys~{_settings.Company}~{_ski.ToUpper()}~{hash}~{_settings.MachineId}~ ~ ~";

            var msg = Encoding.ASCII.GetBytes(unencryptedMsgPart).ToList();
            msg.AddRange(RC4.Encrypt(_sessionKey, Encoding.ASCII.GetBytes(encryptedMsgPart)));

            _logger.Debug($"GetAllChannelKeys from server: {unencryptedMsgPart}{encryptedMsgPart}");

            var response = _sslClient.SendAndReceive(msg.ToArray(), _settings.VksServer, _settings.VksPort + 1, false);

            if (response == null || response.Length < 10)
            {
                _logger.Error("Failed to GetAllChannelKeys, no valid response!");
                return false;
            }

            var encrypted = response.Skip(4).ToArray();
            var decrypted = RC4.Decrypt(_sessionKey, encrypted);
            
            File.WriteAllBytes(KeyblockFile, decrypted);

            _logger.Info($"GetAllChannelKeys completed: {decrypted.Length} bytes");

            return true;
        }

        public bool ValidateKeyBlock(bool afterDownload = false)
        {
            IsValid = LoadAndValidate(afterDownload);
            return IsValid;
        }

        private bool LoadAndValidate(bool afterDownload)
        {
            _logger.Debug("Start validating the keyblock data");
            if (!File.Exists(KeyblockFile))
            {
                _logger.Warn("No keyblock data found on disk");
                return false;
            }
            var data = File.ReadAllBytes(KeyblockFile);
            _block.Load(data, _settings.GetChannelNumbersToMonitor(), _settings.GetChannelsToIgnore());
            if (_block.NrOfChannels < 1)
            {
                _logger.Error("No channels found in the keyblock data");
                return false;
            }
            var expected = DateTime.Now.AddHours(_settings.KeyblockValidationInHours);
            if (_block.NeedsRefreshAfter < expected)
            {
                var channels = string.Join(";", _block.NeedsRefreshAfterChannelIds);
                _logger.Error($"The keyblock data is only valid till {_block.NeedsRefreshAfter} for channel(s) {channels}, we expected at least till {expected}");
                return afterDownload && _settings.KeepBlockIfChannelsAreOutdated;
            }
            _logger.Info($"Keyblock is valid between {_block.ValidFrom:yyyy-MM-dd} and {_block.ValidTo:yyyy-MM-dd} and needs a refresh after {_block.NeedsRefreshAfter:yyyy-MM-dd}");
            return true;
        }

        public bool DownloadNew()
        {
            _settings.EnsureDataFolderExists(_settings.DataFolder);
            var retValue = GetSessionKey();
            if (!retValue) return false;
            //Look if we already have a valid certificate
            if (GenerateSki())
            {
                //Load password
                retValue = GetEncryptedPassword();
            }
            else
            {
                //Get a certificate
                retValue = GetCertificate();
                retValue = retValue && GenerateSki();
                //And save the password
                retValue = retValue && SaveEncryptedPassword();
            }
            retValue = retValue && LoadKeyBlock();
            retValue = retValue && ValidateKeyBlock(true);
            return retValue;
        }

        public void CleanUp()
        {
            _logger.Warn("Clean up old data");
            var keyblock = new FileInfo(KeyblockFile);
            if (keyblock.Exists)
            {
                _logger.Warn($"Remove keyblock file {keyblock.FullName}");
                keyblock.Delete();
            }
            var certificate = new FileInfo(SignedCertificateFile);
            if (certificate.Exists)
            {
                _logger.Warn($"Remove certificate file {certificate.FullName}");
                certificate.Delete();
            }
            if (_certificateRequest != null)
            {
                _logger.Warn("Remove certificate request file");
                _certificateRequest.CleanUp();
            }
            _settings.GenerateClientId();
            _settings.GenerateMachineId();
        }
        
        static string BytesAsHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        public DateTime FirstRefreshDateInFuture()
        {
            var nextRetrieval = DateTime.Now.AddHours(_settings.KeyblockValidationInHours);
            var blockDate = _block.FirstFutureExpirationDate(_settings.GetChannelNumbersToMonitor(), _settings.GetChannelsToIgnore());
            return blockDate ?? nextRetrieval;
        }
    }
}