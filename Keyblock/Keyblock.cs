using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using log4net;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;

namespace Keyblock
{
    public class Keyblock : IKeyblock
    {
        //Filenames
        const string GetCertificateResponseFile = "getCertificate.response";
        const string GetSessionKeyResponseFile = "CreateSessionKey.response";
        const string SignedCertificateFile = "SignedCert.der";

        static readonly ILog Logger = LogManager.GetLogger(typeof(Keyblock));

        readonly IniSettings _settings;
        readonly SslTcpClient _sslClient;

        readonly bool noConnect = true;
        string _sessionKey;
        string _timestamp;
        string _ski;

        public Keyblock(IniSettings settings, SslTcpClient sslClient)
        {
            _settings = settings;
            _sslClient = sslClient;
        }

        bool GetCertificate()
        {
            /******* Get the current time64 *******/
            var t64 = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;

            /******* Generate the CSR *******/
            Logger.Debug("Generating CSR");
            _settings.Email = $"{_settings.MachineId}.{t64}@{_settings.EmailHost}";
            Logger.Debug($"Using email: {_settings.Email}");
            var csr = GenerateCertificateRequest();

            /******* Generate the request string *******/
            var msg = $"{_settings.MessageFormat}~{_settings.ClientId}~getCertificate~{_settings.Company}~NA~NA~{csr}~{_settings.Common}~{_settings.Address}~ ~{_settings.City}~{_settings.Province}~{_settings.ZipCode}~{_settings.Country}~{_settings.Telephone}~{_settings.Email}~{_settings.MachineId}~{_settings.ChallengePassword}~";

            Logger.Debug($"Requesting Certificate: {msg}");

            /******* SendAndReceive the request *******/
            var response = noConnect ?
                File.ReadAllBytes(GetCertificateResponseFile)
                : _sslClient.SendAndReceive(msg, _settings.VcasServer, _settings.VcasPort);

            if (response == null || response.Length < 12) { return false; }

            File.WriteAllBytes(GetCertificateResponseFile, response);

            /******* Get the Signed cert from the response *******/
            var cert = new List<byte>(response).GetRange(12, (response.Length - 12)).ToArray();
            File.WriteAllBytes(SignedCertificateFile, cert);

            return true;
        }

        X509CertificateRequest GenerateCertificateRequest()
        {
            var certificateRequest = new X509CertificateRequest();
            certificateRequest.AddCountry(_settings.Country);
            certificateRequest.AddProvice(_settings.Province);
            certificateRequest.AddCity(_settings.City);
            certificateRequest.AddCompany(_settings.Company);
            certificateRequest.AddOrganization(_settings.Organization);
            certificateRequest.AddCommon(_settings.Common);
            certificateRequest.AddEmail(_settings.Email);
            certificateRequest.ChallangePassword(_settings.ChallengePassword);
            certificateRequest.Generate();

            return certificateRequest;
        }

        bool GetSessionKey()
        {
            var msg = $"{_settings.MessageFormat}~{_settings.ClientId}~CreateSessionKey~{_settings.Company}~{_settings.MachineId}~";

            Logger.Debug($"Requesting Session Key: {msg}");

            var response = noConnect ?
                File.ReadAllBytes(GetSessionKeyResponseFile)
                : _sslClient.SendAndReceive(msg, _settings.VcasServer, _settings.VcasPort);

            if (response == null) return false;
            File.WriteAllBytes(GetSessionKeyResponseFile, response);

            var encoding = Encoding.ASCII;

            var responseBuffer = encoding.GetChars(response);
            _sessionKey = new string(responseBuffer, 4, 16);
            _timestamp = new string(responseBuffer, 20, 20);
            Logger.Debug($"Session key '{_sessionKey}' obtained, timestamp: '{_timestamp}' with encoding {encoding.EncodingName}");

            return true;
        }

        bool GenerateSki()
        {
            var parser = new X509CertificateParser();
            var cert = parser.ReadCertificate(File.ReadAllBytes(SignedCertificateFile));
            var identifier = new SubjectKeyIdentifierStructure(cert.GetPublicKey());
            var bytes = identifier.GetKeyIdentifier();
            _ski = BitConverter.ToString(bytes).Replace("-", "");
            return true;
        }

        public bool DownloadNew()
        {
            return RunInOrder(
                GetSessionKey,
                GetCertificate,
                GenerateSki
            );
        }
        static bool RunInOrder(params Func<bool>[] functions)
        {
            return functions.All(function => function());
        }
    }
}