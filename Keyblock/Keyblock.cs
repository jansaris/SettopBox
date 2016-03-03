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

namespace Keyblock
{
    public class Keyblock
    {
        //Filenames
        string SignedCertificateFile => Path.Combine(_settings.DataFolder, "SignedCert.der");
        string KeyblockFile => Path.Combine(_settings.DataFolder, "Keyblock.dat");

        readonly ILog _logger;
        readonly Settings _settings;
        readonly SslTcpClient _sslClient;

        byte[] _sessionKey;
        string _timestamp;
        string _ski;
        byte[] _password;
        X509CertificateRequest _certificateRequest;

        public Keyblock(Settings settings, SslTcpClient sslClient, ILog logger)
        {
            _settings = settings;
            _sslClient = sslClient;
            _logger = logger;
        }

        bool GetCertificate()
        {
            _logger.Debug("Generating message");
            var t64 = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            _settings.UpdateEmail($"{_settings.MachineId}.{t64}@{_settings.EmailHost}");
            _logger.Debug($"Using email: {_settings.Email}");
            var csr = GenerateCertificateRequest();
            var msg = $"{_settings.MessageFormat}~{_settings.ClientId}~getCertificate~{_settings.Company}~NA~NA~{csr}~{_settings.Common}~{_settings.Address}~ ~{_settings.City}~{_settings.Province}~{_settings.ZipCode}~{_settings.Country}~{_settings.Telephone}~{_settings.Email}~{_settings.MachineId}~{_settings.ChallengePassword}~";

            _logger.Debug($"Requesting Certificate: {msg}");
            var response = _sslClient.SendAndReceive(msg, _settings.VcasServer, _settings.VcasPort);

            if (response == null || response.Length < 12) { return false; }
            var cert = new List<byte>(response).GetRange(12, (response.Length - 12)).ToArray();
            File.WriteAllBytes(SignedCertificateFile, cert);

            _logger.Info("Received Certificate");
            return true;
        }

        X509CertificateRequest GenerateCertificateRequest()
        {
            if (_certificateRequest != null) return _certificateRequest;

            _certificateRequest = new X509CertificateRequest();
            _certificateRequest.AddCountry(_settings.Country);
            _certificateRequest.AddProvice(_settings.Province);
            _certificateRequest.AddCity(_settings.City);
            _certificateRequest.AddCompany(_settings.Company);
            _certificateRequest.AddOrganization(_settings.Organization);
            _certificateRequest.AddCommon(_settings.Common);
            _certificateRequest.AddEmail(_settings.Email);
            _certificateRequest.ChallangePassword(_settings.ChallengePassword);
            _certificateRequest.Generate();
            return _certificateRequest;
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
            var cert = GenerateCertificateRequest();
            sig.Init(true, cert.KeyPair.Private);
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
            /*
             msglen = sprintf((char*) msg,
                            "%s~%s~%s~%s~%s~GetAllChannelKeys~%s~%s~%s~%s~ ~ ~", api_msgformat,
                            api_company, timestamp, api_machineID, api_clientID, api_company, ski,
                            signedhash, api_machineID);
            */
            var encryptedMsgPart = $"{_settings.ClientId}~GetAllChannelKeys~{_settings.Company}~{_ski.ToUpper()}~{hash}~{_settings.MachineId}~ ~ ~";

            var msg = Encoding.ASCII.GetBytes(unencryptedMsgPart).ToList();
            msg.AddRange(RC4.Encrypt(_sessionKey, Encoding.ASCII.GetBytes(encryptedMsgPart)));

            _logger.Debug($"GetAllChannelKeys from server: {unencryptedMsgPart}{encryptedMsgPart}");

            // Validation
            var expectedUnEncrypted = File.ReadAllText("RC4/keyblock.unencrypted");
            var expectedEncrypted = File.ReadAllBytes("RC4/keyblock.encrypted");
            _logger.Debug($"{unencryptedMsgPart}{encryptedMsgPart}");
            _logger.Debug($"Messages are unencrypted equal: {expectedUnEncrypted == $"{unencryptedMsgPart}{encryptedMsgPart}"}");
            _logger.Debug($"Messages are encrypted equal: {expectedEncrypted.SequenceEqual(msg)}");
            // Validation

            var response = _sslClient.SendAndReceive(msg.ToArray(), _settings.VksServer, _settings.VksPort + 1, false);

            if (response == null || response.Length < 10)
            {
                _logger.Error("Failed to GetAllChannelKeys, no valid response!");
                return false;
            }

            var encrypted = response.Skip(4).Take(response.Length - 4).ToArray();
            var decrypted = RC4.Decrypt(_sessionKey, encrypted);
            
            File.WriteAllBytes(KeyblockFile, decrypted);

            _logger.Info($"GetAllChannelKeys completed: {decrypted.Length} bytes");

            return true;
        }

        public bool DownloadNew()
        {
            PreLoad();
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
            _settings.GenerateClientId();
            _settings.GenerateMachineId();
        }

        void PreLoad()
        {
            GenerateCertificateRequest();
            _certificateRequest.LoadKeyPairFromDisk("RC4/priv_key.pem");
        }

        static string BytesAsHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
    }
}