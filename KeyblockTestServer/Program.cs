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

        internal static string OpenSslFolder;
        internal static string CommunicationsFolder;
        internal static string CommunicationLogFolder;
        private static string _certificateFile;

        private const string CertificatePassword = "settopbox";

        private static string _appDataFolder = @".\KeyblockServerPrograms";
        internal static string MacAdress = "00E04C61BA1D";
        private static string IpAdress = "192.168.11.123";

        private const int VcasPort = 12697;
        private const int VcasPasswordPort = 12698;
        private const int VksPort = 12700;

        private SslTcpServer _ssl;
        private TcpListener _vcs;
        private TcpListener _vks;
        private TcpListener _password;
        private TcpListener _start;

        static void Main(string[] args)
        {
            try
            {
                XmlConfigurator.ConfigureAndWatch(new FileInfo("Log4net.config"));
                ParseArguments(args);
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

        private static void ParseArguments(string[] args)
        {
            if (args.Length > 0)
            {
                Logger.Info($"Found appdata folder on the commandline: {args[0]}");
                _appDataFolder = args[0];
            }
            if (args.Length > 1)
            {
                Logger.Info($"Found MacAdress on the commandline: {args[1]}");
                MacAdress = args[1];
            }
            if (args.Length > 2)
            {
                Logger.Info($"Found IpAdress on the commandline: {args[2]}");
                IpAdress = args[2];
            }

            OpenSslFolder = Path.Combine(_appDataFolder, "Openssl");
            CommunicationsFolder = Path.Combine(_appDataFolder, "KeyblockMessages");
            CommunicationLogFolder = Path.Combine(_appDataFolder, "ServerCommunication");
            _certificateFile = Path.Combine(_appDataFolder, "Certificates", "servercert.pfx");
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
            _ssl = new SslTcpServer(_certificateFile, CertificatePassword);
            _vcs = new TcpListener(GetIpAdress(), VcasPort);
            _vcs.Start();
            StartAccept(_vcs, HandleVcas);

            Logger.Info($"Start listening at {IpAdress}:{12686}");
            _start = new TcpListener(GetIpAdress(), 12686);
            _start.Start();
            StartAccept(_start, HandleStartBox);
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

        private void HandleStartBox(IAsyncResult res)
        {
            Logger.Info("start handling start box request");
            StartAccept(_start, HandleStartBox); //listen for new connections again
            var client = _start.EndAcceptTcpClient(res);
            //proceed
            try
            {
                Logger.Info("Handle the start box stream");
                var stream = client.GetStream();
                var data = KeyblockCall.Read(stream);
                Logger.Info($"Read {data.Length} bytes from the start box stream");
                var response = File.ReadAllBytes(Path.Combine(_appDataFolder, "KeyblockMessages", "12686.response"));
                Logger.Info($"Write {response.Length} bytes to the start box stream");
                stream.Write(response, 264, response.Length - 264);
                stream.Flush();
            }
            catch (Exception ex)
            {
                Logger.Warn($"Something went wrong when handling start box request: {ex.Message}");   
            }  
            client.Close();
        }

        IPAddress GetIpAdress()
        {
            return IPAddress.Parse(IpAdress);
        }
    }
}
