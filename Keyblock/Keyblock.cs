using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using log4net;

namespace Keyblock
{
    public class Keyblock : IKeyblock
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(Keyblock));

        readonly IniSettings _settings;
        readonly SslTcpClient _sslClient;

        readonly bool noConnect = true;
        string _sessionKey;
        string _timestamp;

        public Keyblock(IniSettings settings, SslTcpClient sslClient)
        {
            _settings = settings;
            _sslClient = sslClient;
        }


        bool GetCertificate()
        {
            const string responseName = "getCertificate.response";
            /******* Get the current time64 *******/
            var t64 = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;

            /******* Generate the CSR *******/
            Logger.Debug("[API] Generating CSR");
            _settings.Email = $"{_settings.MachineId}.{t64}@{_settings.EmailHost}";
            Logger.Debug($"[API] Using email: {_settings.Email}");
            var csr = GenerateCertificateRequest();

            /******* Generate the request string *******/
            var msg = $"{_settings.MessageFormat}~{_settings.ClientId}~getCertificate~{_settings.Company}~NA~NA~{csr}~{_settings.Common}~{_settings.Address}~ ~{_settings.City}~{_settings.Province}~{_settings.ZipCode}~{_settings.Country}~{_settings.Telephone}~{_settings.Email}~{_settings.MachineId}~{_settings.ChallengePassword}~";

            Logger.Debug($"[API] Requesting Certificate: {msg}");

            /******* SendAndReceive the request *******/
            var response = noConnect ?
                File.ReadAllBytes(responseName)
                : _sslClient.SendAndReceive(msg, _settings.VcasServer, _settings.VcasPort);

            if (response == null || response.Length < 12) { return false; }

            File.WriteAllBytes(responseName, response);

            /******* Get the Signed cert from the response *******/
            var cert = new List<byte>(response).GetRange(12, (response.Length - 12)).ToArray();
            File.WriteAllBytes("SignedCert.der", cert);

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
            const string responseName = "CreateSessionKey.response";

            var msg = $"{_settings.MessageFormat}~{_settings.ClientId}~CreateSessionKey~{_settings.Company}~{_settings.MachineId}~";

            Logger.Debug($"[API] Requesting Session Key: {msg}");

            var response = noConnect ?
                File.ReadAllBytes(responseName)
                : _sslClient.SendAndReceive(msg, _settings.VcasServer, _settings.VcasPort);

            if (response == null) return false;
            File.WriteAllBytes(responseName, response);

            var encoding = Encoding.ASCII;

            var responseBuffer = encoding.GetChars(response);
            _sessionKey = new string(responseBuffer, 4, 16);
            _timestamp = new string(responseBuffer, 20, 20);
            Logger.Debug($"[API] Session key '{_sessionKey}' obtained, timestamp: '{_timestamp}' with encoding {encoding.EncodingName}");

            return true;
        }

        //int generate_ski_string()
        //{
        //    //FILE* fp;
        //    //int i, j = 0, loc = 0;
        //    //char* buf2 = ski = calloc(40 + 1, 1);
        //    //X509* signed_cert = 0;
        //    //X509_EXTENSION* ext;

        //    fp = fopen(f_signedcert, "r");
        //    if (fp)
        //    {
        //        signed_cert = d2i_X509_fp(fp, &signed_cert);
        //        fclose(fp);
        //    }
        //    else {  //Create new one
        //        return -1;
        //    }

        //    loc = X509_get_ext_by_NID(signed_cert, NID_subject_key_identifier, -1);
        //    ext = X509_get_ext(signed_cert, loc);

        //    OPENSSL_free(signed_cert);

        //    if (ext == NULL)
        //    {
        //        return -1;
        //    }

        //    for (i = 2; i < 22; i++)
        //    {
        //        j += sprintf(buf2 + j, "%02X", ext->value->data[i]);
        //    }
        //    return j + 2;
        //}

        public bool DownloadNew()
        {
            return RunInOrder(
                GetSessionKey,
                GetCertificate
            );
        }
        static bool RunInOrder(params Func<bool>[] functions)
        {
            return functions.All(function => function());
        }
    }
}