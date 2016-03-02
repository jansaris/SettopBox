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
    public class Keyblock : IKeyblock
    {
        //Filenames
        string SignedCertificateFile => Path.Combine(_settings.DataFolder, "SignedCert.der");
        string KeyblockFile => Path.Combine(_settings.DataFolder, "Keyblock.dat");

        static readonly ILog Logger = LogManager.GetLogger(typeof(Keyblock));

        readonly IniSettings _settings;
        readonly SslTcpClient _sslClient;

        byte[] _sessionKey;
        string _timestamp;
        string _ski;
        byte[] _password;
        X509CertificateRequest _certificateRequest;

        public Keyblock(IniSettings settings, SslTcpClient sslClient)
        {
            _settings = settings;
            _sslClient = sslClient;
        }

        bool GetCertificate()
        {
            Logger.Debug("Generating message");
            var t64 = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            _settings.Email = $"{_settings.MachineId}.{t64}@{_settings.EmailHost}";
            Logger.Debug($"Using email: {_settings.Email}");
            var csr = GenerateCertificateRequest();
            var msg = $"{_settings.MessageFormat}~{_settings.ClientId}~getCertificate~{_settings.Company}~NA~NA~{csr}~{_settings.Common}~{_settings.Address}~ ~{_settings.City}~{_settings.Province}~{_settings.ZipCode}~{_settings.Country}~{_settings.Telephone}~{_settings.Email}~{_settings.MachineId}~{_settings.ChallengePassword}~";

            Logger.Debug($"Requesting Certificate: {msg}");
            var response = _sslClient.SendAndReceive(msg, _settings.VcasServer, _settings.VcasPort);

            if (response == null || response.Length < 12) { return false; }
            var cert = new List<byte>(response).GetRange(12, (response.Length - 12)).ToArray();
            File.WriteAllBytes(SignedCertificateFile, cert);

            Logger.Debug("Received Certificate");
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
            Logger.Debug($"Requesting Session Key: {msg}");

            var response = _sslClient.SendAndReceive(msg, _settings.VcasServer, _settings.VcasPort);

            if (response == null) return false;
            _sessionKey = response.Skip(4).Take(16).ToArray();
            _timestamp = Encoding.ASCII.GetString(response.Skip(20).Take(19).ToArray());

            Logger.Debug($"Session key obtained with timestamp: '{_timestamp}'");
            return true;
        }

        bool GenerateSki()
        {
            try
            {
                Logger.Debug($"Resolve SKI from {SignedCertificateFile}");
                var parser = new X509CertificateParser();
                var cert = parser.ReadCertificate(File.ReadAllBytes(SignedCertificateFile));
                var identifier = new SubjectKeyIdentifierStructure(cert.GetPublicKey());
                var bytes = identifier.GetKeyIdentifier();
                _ski = BytesAsHex(bytes);

                Logger.Debug($"Resolved SKI '{_ski}'");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get the SKI from {SignedCertificateFile}", ex);
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

            Logger.Debug($"Save encryption password: {unencryptedMsgPart}{encryptedMsgPart}");

            var msg = Encoding.ASCII.GetBytes(unencryptedMsgPart).ToList();
            msg.AddRange(RC4.Encrypt(_sessionKey, Encoding.ASCII.GetBytes(encryptedMsgPart)));

            var response = _sslClient.SendAndReceive(msg.ToArray(), _settings.VcasServer, _settings.VcasPort + 1, false);

            if (response == null || response.Length < 8)
            {
                Logger.Error("Failed to SaveEncryptedPassword, no valid response!");
                return false;
            }
            
            Logger.Debug($"SaveEncryptedPassword completed, size: {response.Length}");
            return true;
        }

        bool GetEncryptedPassword()
        {
            var unencryptedMsgPart = $"{_settings.MessageFormat}~{_settings.Company}~{_timestamp}~{_settings.MachineId}~";
            var encryptedMsgPart = $"{_settings.ClientId}~GetEncryptedPassword~{_settings.Company}~{_ski}~";

            Logger.Debug($"Get encryption password: {unencryptedMsgPart}{encryptedMsgPart}");

            var msg = Encoding.ASCII.GetBytes(unencryptedMsgPart).ToList();
            msg.AddRange(RC4.Encrypt(_sessionKey, Encoding.ASCII.GetBytes(encryptedMsgPart)));

            var response = _sslClient.SendAndReceive(msg.ToArray(), _settings.VcasServer, _settings.VcasPort + 1, false);

            if (response == null || response.Length < 8)
            {
                Logger.Error("Failed to GetEncryptedPassword, no valid response!");
                return false;
            }

            var encryptedPassword = response.Skip(4).Take(response.Length - 4).ToArray();
            var decrypted = RC4.Decrypt(_sessionKey, encryptedPassword);
            var passwordHex = Encoding.ASCII.GetString(decrypted.Skip(4).ToArray());

            Logger.Debug($"GetEncryptedPassword completed: {passwordHex}");
            return true;
        }

        string GenerateSignedHash()
        {
            Logger.Debug($"Generate signed hash from {_timestamp}");
            var timestampBytes = Encoding.ASCII.GetBytes(_timestamp);
            
            // Use the generated key
            var sig = SignerUtilities.GetSigner(PkcsObjectIdentifiers.MD5WithRsaEncryption);
            var cert = GenerateCertificateRequest();
            sig.Init(true, cert.KeyPair.Private);
            sig.BlockUpdate(timestampBytes,0, timestampBytes.Length);
            var signature = sig.GenerateSignature();
            //Return the hash as Hex
            var generated =  BytesAsHex(signature);

            Logger.Debug($"Generated signed hash from {generated}");
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

            Logger.Debug($"GetAllChannelKeys from server: {unencryptedMsgPart}{encryptedMsgPart}");

            // Validation
            var expectedUnEncrypted = File.ReadAllText("RC4/keyblock.unencrypted");
            var expectedEncrypted = File.ReadAllBytes("RC4/keyblock.encrypted");
            Logger.Info($"{unencryptedMsgPart}{encryptedMsgPart}");
            Logger.Info($"Messages are unencrypted equal: {expectedUnEncrypted == $"{unencryptedMsgPart}{encryptedMsgPart}"}");
            Logger.Info($"Messages are encrypted equal: {expectedEncrypted.SequenceEqual(msg)}");
            // Validation

            var response = _sslClient.SendAndReceive(msg.ToArray(), _settings.VksServer, _settings.VksPort + 1, false);

            if (response == null || response.Length < 10)
            {
                Logger.Error("Failed to GetAllChannelKeys, no valid response!");
                return false;
            }

            var encrypted = response.Skip(4).Take(response.Length - 4).ToArray();
            var decrypted = RC4.Decrypt(_sessionKey, encrypted);
            
            File.WriteAllBytes(KeyblockFile, decrypted);

            Logger.Debug($"GetAllChannelKeys completed: {decrypted.Length} bytes");

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