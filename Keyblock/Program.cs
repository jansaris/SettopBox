using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;

namespace Keyblock
{
    class Program
    {
        private bool noConnect = false;

        private int DEBUG = 1;
        private readonly Random _random = new Random();
        private readonly SslTcpClient _sslClient = new SslTcpClient();
        private string _apiMachineId;
        private string _apiClientId;

        // Cert data
        const string szAddress = "6650 Lusk Blvd, Suite B203";
        const string szZipCode = "92021";
        const string szCountry = "US";
        const string szProvince = "CA";
        const string szCity = "San Diego";
        const string szOrganization = "vr2.3.1-candidate-amino-A130.11-hwonly";
        const string szCommon = "STB";
        const string szTelephone = "858-677-7800";
        string szEmail = "VODPassword";
        const string szChallengePassword = "VODPassword";

        // Connection data
        string vcasServerAddress = string.Empty;         // Your VCAS server address
        string vksServerAddress = string.Empty;          // Your VCAS server address
        int VCAS_Port_SSL;              // Your VCAS port
        int VKS_Port_SSL;               // Your VKS port

        // API data
        string api_company;               // Your company
        const string api_msgformat = "1154"; 		// Only 1154 is supported for now

        private string session_key;
        private string timestamp;

        static void Main(string[] args)
        {
            var prog = new Program();
            prog.load_clientId();
            prog.load_machineid();
            prog.vm_config(
                "vmx.stb.zt6.nl",
                12697,
                "vmx.stb.zt6.nl",
                12699,
                "Reggefiber",
                1,
                "dir");
            if (!prog.API_GetSessionKey())
            {
                Console.WriteLine("Failed to get the session key");
                return;
            }
            Console.WriteLine("Give the server some time to process");
            //Task.Delay(1000).Wait();
            prog.API_GetCertificate();
            prog.Close();
            Console.WriteLine("Done: Exit");
            Console.ReadKey();
        }

        private void Close()
        {
            _sslClient.Close();
        }

        private bool API_GetCertificate()
        {
            //FILE* fp;
            //const uchar* cert;
            //char* csr;
            //int response_len;
            //int msglen;
            //uchar msg[2048];
            //uchar* response_buffer = calloc(2048, 1);
            /******* Get the current time64 *******/
            var t64 = (long)(DateTime.UtcNow - new DateTime (1970, 1, 1)).TotalSeconds;

            /******* Generate the CSR *******/
            LOG(DEBUG, "[API] Generating CSR");
            //szEmail = calloc(64, 1);
            szEmail = $"{_apiMachineId}.{t64}@Verimatrix.com";
            //sprintf(szEmail, "%s.%llu@Verimatrix.com", api_machineId, t64);
            LOG(DEBUG, $"[API] Using email: {szEmail}");
            var csr = generate_csr();
            var pem = csr.CsrPem();

            /******* Generate the request string *******/
            var msg = $"{api_msgformat}~{_apiClientId}~getCertificate~{api_company}~NA~NA~{pem}~{szCommon}~{szAddress}~ ~{szCity}~{szProvince}~{szZipCode}~{szCountry}~{szTelephone}~{szEmail}~{_apiMachineId}~{szChallengePassword}~";
            //msglen = sprintf((char*)msg,
            //                "%s~%s~getCertificate~%s~NA~NA~%s~%s~%s~ ~%s~%s~%s~%s~%s~%s~%s~%s~",
            //                api_msgformat, api_clientID, api_company, csr,
            //                szCommon, szAddress, szCity, szProvince, szZipCode, szCountry, szTelephone, szEmail,
            //                api_machineID, szChallengePassword);
            File.WriteAllText("winCert.txt", msg);
            File.WriteAllText("cert.der", pem);
            File.WriteAllText("csr.cer",$"-----BEGIN CERTIFICATE REQUEST-----{csr.Csr}-----END CERTIFICATE REQUEST-----");
            LOG(DEBUG, $"[API] Requesting Certificate: {msg}");

            /******* Send the request *******/
            var response = _sslClient.Send(msg, vcasServerAddress, VCAS_Port_SSL);

            if (response == null || response.Length < 12)
            {
                return false;
            }

            /******* Get the Signed cert from the response *******/
            var cert = new List<byte>(response).GetRange(12, (response.Length - 12)).ToArray();
            File.WriteAllBytes("signed_cert.cer",cert);
            //cert = response_buffer + 12;
            
            return true;
        }

        private X509CertificateRequest generate_csr()
        {
            var subject = new X509SubjectAttributes();
            subject.Add(X509Name.C,szCountry);
            subject.Add(X509Name.ST, szProvince);
            subject.Add(X509Name.L, szCity);
            subject.Add(X509Name.O, api_company);
            subject.Add(X509Name.OU, szOrganization);
            subject.Add(X509Name.CN,szCommon);
            subject.Add(X509Name.EmailAddress,szEmail);
            subject.ChallangePassword(szChallengePassword);
            //subject.Add(X509Name.,szCountry);

            var cer = X509Certificate2Builder.GeneratePkcs10(subject);
            //var ca2 = new X509Certificate2Builder { SubjectName = subject }.Build();
            //return ca2.Thumbprint;
            return cer;
        }

        bool API_GetSessionKey()
        {
            var msg = $"{api_msgformat}~{_apiClientId}~CreateSessionKey~{api_company}~{_apiMachineId}~";

            LOG(DEBUG, $"[API] Requesting Session Key: {msg}");
            
            var resp = noConnect ?
                File.ReadAllBytes("Session.txt")
                : _sslClient.Send(msg, vcasServerAddress, VCAS_Port_SSL);
            //var resp = 

            if (resp == null) return false;

            var encoding = Encoding.ASCII;

            var responseBuffer = encoding.GetChars(resp);
            session_key = new string(responseBuffer, 4, 16);
            timestamp = new string(responseBuffer, 20, 20);
            LOG(DEBUG, $"[API] Session key '{session_key}' obtained, timestamp: '{timestamp}' with encoding {encoding.EncodingName}");
            
            return true;
        }

        void vm_config(string vcas_address, int vcas_port, string vks_address, int vks_port, string company, int interval, string dir)
        {
            vcasServerAddress = vcas_address;
            vksServerAddress = vks_address;

            api_company = company;

            VCAS_Port_SSL= vcas_port;
            VKS_Port_SSL = vks_port;


        }

        int load_machineid()
        {
            _apiMachineId = string.Empty;
            
            if (File.Exists("machineId"))
            {
                LOG(DEBUG, "[API] MachineID found, reading MachineID");
                _apiMachineId = File.ReadAllText("machineId");
                if (_apiMachineId.Length == 28) return 0;
            }
            
            LOG(DEBUG, "[API] No MachineID found, generating MachineID");
            var buf = new byte[20];
            _random.NextBytes(buf);
            _apiMachineId = Convert.ToBase64String(buf);
            LOG(DEBUG, $"[API] Your MachineID is: {_apiMachineId}");
            File.WriteAllText("machineId",_apiMachineId);
            return 0;
        }

        int load_clientId()
        {
            _apiClientId = string.Empty;

            if (File.Exists("clientId"))
            {
                LOG(DEBUG, "[API] ClientID found, reading ClientId");
                _apiClientId = File.ReadAllText("clientId");
                if (_apiClientId.Length == 56) return 0;
            }

            LOG(DEBUG, "[API] No ClientId found, generating ClientId");
            var buf = new byte[28];
            _random.NextBytes(buf);
            _apiClientId = string.Empty;
            foreach (var b in buf)
            {
                _apiClientId += (b & 0xFF).ToString("X2");
            }
            //_apiClientId = Convert.ToBase64String(buf);
            LOG(DEBUG, $"[API] Your ClientID is: {_apiClientId}");
            File.WriteAllText("clientId", _apiClientId);
            return 0;
        }

        private void LOG(int level, string message)
        {
            Console.WriteLine(message);
        }
    }
}
