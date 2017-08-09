using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using log4net;
using log4net.Config;

namespace KeyblockTestServer
{
    class Program
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Program));

        private const string AppDataFolder = @"KeyblockServerPrograms";
        internal static readonly string OpenSslFolder = Path.Combine(AppDataFolder, "Openssl");
        internal static readonly string CommunicationsFolder = Path.Combine(AppDataFolder, "KeyblockMessages");
        internal static readonly string CommunicationLogFolder = Path.Combine(AppDataFolder, "ServerCommunication");

        private static readonly string CertificateFile = Path.Combine(AppDataFolder, "Certificates","servercert.pfx");
        private const string CertificatePassword = "settopbox";

        internal const string MacAdress = "MAC_ADDRESS";
        private const string IpAdress = "127.0.0.1";

        private const int VcasPort = 12697;
        private const int VcasPasswordPort = 12698;
        private const int VksPort = 12700;

        private SslTcpServer _ssl;
        private TcpListener _vcs;
        private TcpListener _vks;
        private TcpListener _password;

        static void Main()
        {
            try
            {
                XmlConfigurator.ConfigureAndWatch(new FileInfo("Log4net.config"));
                var program = new Program();
                program.StartTcpServer();
                while (true)
                {
                    Task.Delay(1000).Wait();
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Something bad happened: {ex.Message}");
                Logger.Fatal(ex.StackTrace);
                Console.ReadKey();
            }
        }

        private void StartTcpServer()
        {
            if (!Directory.Exists(CommunicationLogFolder)) Directory.CreateDirectory(CommunicationLogFolder);

            Logger.Info($"Start listening at {IpAdress}:{VksPort}");
            _vks = new TcpListener(GetIpAdress(), VksPort);
            _vks.Start();
            StartAccept(_vks, HandleKeyblock);

            Logger.Info($"Start listening at {IpAdress}:{VcasPasswordPort}");
            _password = new TcpListener(GetIpAdress(), VcasPasswordPort);
            _password.Start();
            StartAccept(_password, HandlePassword);

            Logger.Info($"Start listening at {IpAdress}:{VcasPort}");
            _ssl = new SslTcpServer(CertificateFile, CertificatePassword);
            _vcs = new TcpListener(GetIpAdress(), VcasPort);
            _vcs.Start();
            StartAccept(_vcs, HandleVcas);
        }

        private void StartAccept(TcpListener listener, Action<IAsyncResult> handler)
        {
            listener.BeginAcceptTcpClient(a => handler(a), listener);
        }

        private void HandlePassword(IAsyncResult res)
        {
            Logger.Info("start handling password request");
            StartAccept(_password, HandlePassword); //listen for new connections again
            var client = _password.EndAcceptTcpClient(res);
            //proceed
            var vcas = new KeyblockCall();
            vcas.Handle(client.GetStream(), true);
            client.Close();
        }

        private void HandleVcas(IAsyncResult res)
        {
            Logger.Info("start handling vcas request");
            StartAccept(_vcs, HandleVcas); //listen for new connections again
            var client = _vcs.EndAcceptTcpClient(res);
            //proceed
            _ssl.ProcessClient(client);

        }

        private void HandleKeyblock(IAsyncResult res)
        {
            Logger.Info("start handling password request");
            StartAccept(_vks, HandleKeyblock); //listen for new connections again
            var client = _vks.EndAcceptTcpClient(res);
            //proceed
            var vks = new KeyblockCall();
            vks.Handle(client.GetStream(), true);
            client.Close();
        }

        IPAddress GetIpAdress()
        {
            return IPAddress.Parse(IpAdress);
        }
    }
}
