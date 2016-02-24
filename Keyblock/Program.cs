using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using log4net;
using log4net.Config;
using Org.BouncyCastle.Asn1.X509;

namespace Keyblock
{
    class Program
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Program));

        private bool noConnect = false;

        private readonly Random _random = new Random();
        private readonly IniSettings _settings = new IniSettings();
        private readonly SslTcpClient _sslClient = new SslTcpClient();

        private string _sessionKey;
        private string _timestamp;

        static void Main()
        {
            try
            {
                XmlConfigurator.Configure(new FileInfo("Log4net.config"));
                var prog = new Program();
                prog.LoadIni();
                prog.load_clientId();
                prog.load_machineid();
                if (!prog.API_GetSessionKey())
                {
                    Console.WriteLine("Failed to get the session key");
                    return;
                }
                prog.API_GetCertificate();
                prog.Close();
                Logger.Info("Done: Exit");

            }
            catch (Exception ex)
            {
                Logger.Fatal("An unhandled exception occured", ex);
            }
            Console.ReadKey();
        }

        private void LoadIni()
        {
            _settings.Load();
        }

        private void Close()
        {
            _settings.Save();
        }

        private void API_GetCertificate()
        {
            /******* Get the current time64 *******/
            var t64 = (long)(DateTime.UtcNow - new DateTime (1970, 1, 1)).TotalSeconds;

            /******* Generate the CSR *******/
            Logger.Debug("[API] Generating CSR");
            _settings.Email = $"{_settings.MachineId}.{t64}@{_settings.EmailHost}";
            Logger.Debug($"[API] Using email: {_settings.Email}");
            var csr = generate_csr();
            var pem = csr.CsrPem();

            /******* Generate the request string *******/
            var msg = $"{_settings.MessageFormat}~{_settings.ClientId}~getCertificate~{_settings.Company}~NA~NA~{pem}~{_settings.Common}~{_settings.Address}~ ~{_settings.City}~{_settings.Province}~{_settings.ZipCode}~{_settings.Country}~{_settings.Telephone}~{_settings.Email}~{_settings.MachineId}~{_settings.ChallengePassword}~";

            Logger.Debug($"[API] Requesting Certificate: {msg}");

            /******* SendAndReceive the request *******/
            var response = _sslClient.SendAndReceive(msg, _settings.VcasServer, _settings.VcasPort);

            if (response == null || response.Length < 12)
            {
                return;
            }

            /******* Get the Signed cert from the response *******/
            var cert = new List<byte>(response).GetRange(12, (response.Length - 12)).ToArray();
            File.WriteAllBytes("signed_cert.cer",cert);
        }

        private X509CertificateRequest generate_csr()
        {
            var subject = new X509SubjectAttributes();
            subject.Add(X509Name.C, _settings.Country);
            subject.Add(X509Name.ST, _settings.Province);
            subject.Add(X509Name.L, _settings.City);
            subject.Add(X509Name.O, _settings.Company);
            subject.Add(X509Name.OU, _settings.Organization);
            subject.Add(X509Name.CN, _settings.Common);
            subject.Add(X509Name.EmailAddress,_settings.Email);
            subject.ChallangePassword(_settings.ChallengePassword);

            var cer = X509Certificate2Builder.GeneratePkcs10(subject);

            return cer;
        }

        bool API_GetSessionKey()
        {
            var msg = $"{_settings.MessageFormat}~{_settings.ClientId}~CreateSessionKey~{_settings.Company}~{_settings.MachineId}~";

            Logger.Debug($"[API] Requesting Session Key: {msg}");
            
            var resp = noConnect ?
                File.ReadAllBytes("Session.txt")
                : _sslClient.SendAndReceive(msg, _settings.VcasServer, _settings.VcasPort);

            if (resp == null) return false;

            var encoding = Encoding.ASCII;

            var responseBuffer = encoding.GetChars(resp);
            _sessionKey = new string(responseBuffer, 4, 16);
            _timestamp = new string(responseBuffer, 20, 20);
            Logger.Debug($"[API] Session key '{_sessionKey}' obtained, timestamp: '{_timestamp}' with encoding {encoding.EncodingName}");
            
            return true;
        }

        void load_machineid()
        {
            _settings.MachineId = string.Empty;
            
            if (File.Exists("machineId"))
            {
                Logger.Debug("[API] MachineID found, reading MachineID");
                _settings.MachineId = File.ReadAllText("machineId");
                if (_settings.MachineId.Length == 28) return;
            }

            Logger.Debug("[API] No MachineID found, generating MachineID");
            var buf = new byte[20];
            _random.NextBytes(buf);
            _settings.MachineId = Convert.ToBase64String(buf);
            Logger.Debug($"[API] Your MachineID is: {_settings.MachineId}");
            File.WriteAllText("machineId",_settings.MachineId);
        }

        void load_clientId()
        {
            _settings.ClientId = string.Empty;

            if (File.Exists("clientId"))
            {
                Logger.Debug("[API] ClientID found, reading ClientId");
                _settings.ClientId = File.ReadAllText("clientId");
                if (_settings.ClientId.Length == 56) return;
            }

            Logger.Debug("[API] No ClientId found, generating ClientId");
            var buf = new byte[28];
            _random.NextBytes(buf);
            _settings.ClientId = string.Empty;
            foreach (var b in buf)
            {
                _settings.ClientId += (b & 0xFF).ToString("X2");
            }
            Logger.Debug($"[API] Your ClientID is: {_settings.ClientId}");
            File.WriteAllText("clientId", _settings.ClientId);
        }
    }
}
