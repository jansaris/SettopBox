using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
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

        private static string _appDataFolder = @".\";
        internal static string MacAddress = "00029BE0880D";
        private static string _fallbackWebServer = "http://webapp.stb.itvonline.nl";
        private static string _fallbackKeyServer = "10.10.10.10";
        private static string _wwwRoot;

        private const int VcasPort = 12697;
        private const int VcasPasswordPort = 12698;
        private const int VksPort = 12700;
        private const int WebPort = 8080;

        private SslTcpServer _ssl;
        private TcpListener _vcs;
        private TcpListener _vks;
        private TcpListener _password;
        private TcpListener _start;
        private SimpleHttpServer _webServer;
        private bool _stopping;
        public static bool OnlySearchForSettopBoxInfo;
        public static SettopBoxInfo SettopBoxInfo = null;


        static void Main(string[] args)
        {
            try
            {
                XmlConfigurator.ConfigureAndWatch(new FileInfo("Log4net.config"));
                if (!ParseArguments(args))
                {
                    Logger.Error("Please use: KeyblockTestServer.exe <Fallback web-server> [MacAddress] [appdata folder] [Fallback keyblock] [OpenSSL folder]");
                    return;
                }
                var program = new Program();
                program.StartTcpServer();
                if (OnlySearchForSettopBoxInfo)
                {
                    while (SettopBoxInfo == null)
                    {
                        Thread.Sleep(50);
                    }
                    Logger.Info("We found the SettopBoxInfo, generate Keyblock settings");
                    var inf = new FileInfo("Settings.ini");
                    Logger.Info($"Write keyblock settings to: {inf.FullName}");
                    File.WriteAllText(inf.FullName, SettopBoxInfo.Serialize());
                    Logger.Info(SettopBoxInfo.Serialize());
                    Logger.Info("");
                    Logger.Info("We are done here. You can use these settings to get the Keyblock.");
                    Logger.Info("KTHXBYE!");
                }
                else
                {
                    Logger.Info("Hit 'Enter' to exit");
                    Console.ReadLine();
                }
                program.Stop();
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Something bad happened: {ex.Message}");
                Logger.Fatal(ex.StackTrace);
                Console.ReadKey();
            }
        }

        private void Stop()
        {
            _stopping = true;
            Safe(() => _vcs.Stop(), $"port {VcasPort}");
            Safe(() => _vks.Stop(), $"port {VksPort}");
            Safe(() => _start.Stop(), "port 12686");
            Safe(() => _password.Stop(), $"port {VcasPasswordPort}");
            Safe(() => _webServer.Stop(), $"port {WebPort}");
        }

        private void Safe(Action action, string name)
        {
            try
            {
                Logger.Info($"Stop {name}");
                action();
            }
            catch (Exception ex)
            {
                Logger.Info($"Failed to stop a listener: {ex.Message}");
            }
        }

        private static bool ParseArguments(string[] args)
        {
            if (args.Length < 1) return false;

            Logger.Info($"Found fallback web address for web server on the commandline: {args[0]}");
            _fallbackWebServer = args[0];

            if (args.Length > 1)
            {
                Logger.Info($"Found MacAddress on the commandline: {args[1]}");
                MacAddress = args[1];
            }

            if (args.Length > 2)
            {
                Logger.Info($"Found appdata folder on the commandline: {args[2]}");
                _appDataFolder = args[2];
            }
            else
            {
                OnlySearchForSettopBoxInfo = true;
                _appDataFolder = ".";
            }

            if (args.Length > 3)
            {
                Logger.Info($"Found fallback key server on the commandline: {args[3]}");
                _fallbackKeyServer = args[3];
            }

            if (args.Length > 4)
            {
                Logger.Info($"Use custom OpenSSL folder {args[4]}");
                OpenSslFolder = args[4];
            }
            else
            {
                OpenSslFolder = Path.Combine(_appDataFolder, "Openssl");
            }
            
            CommunicationsFolder = Path.Combine(_appDataFolder, "KeyblockMessages");
            CommunicationLogFolder = Path.Combine(_appDataFolder, "ServerCommunication");
            _certificateFile = Path.Combine(_appDataFolder, "Certificates", "servercert.pfx");
            _wwwRoot = Path.Combine(_appDataFolder, "www");
            return true;
        }

        private void StartTcpServer()
        {
            if (!Directory.Exists(CommunicationLogFolder)) Directory.CreateDirectory(CommunicationLogFolder);

            Logger.Info($"Start listening at port:{VksPort}");
            _vks = new TcpListener(GetIpAddress(), VksPort);
            _vks.Start();
            StartAccept(_vks, HandleKeyblock);

            Logger.Info($"Start listening at port:{VcasPasswordPort}");
            _password = new TcpListener(GetIpAddress(), VcasPasswordPort);
            _password.Start();
            StartAccept(_password, HandlePassword);

            Logger.Info($"Start listening at port:{VcasPort}");
            _ssl = new SslTcpServer(_certificateFile, CertificatePassword);
            _vcs = new TcpListener(GetIpAddress(), VcasPort);
            _vcs.Start();
            StartAccept(_vcs, HandleVcas);

            Logger.Info($"Start listening at port:{12686} with keyblock server at {_fallbackKeyServer}");
            _start = new TcpListener(GetIpAddress(), 12686);
            _start.Start();
            StartAccept(_start, HandleStartBox);

            Logger.Info($"Start listening at port:{WebPort} with a fallback to {_fallbackWebServer}");
            _webServer = new SimpleHttpServer(_wwwRoot, WebPort, _fallbackWebServer);
        }

        private void StartAccept(TcpListener listener, Action<IAsyncResult> handler)
        {
            try
            {
                listener.BeginAcceptTcpClient(a => handler(a), listener);
            }
            catch (Exception ex)
            {
                if (!_stopping) Logger.Warn($"Failed to listen for TCP request: {ex.Message}");
            }
            
        }

        private void HandlePassword(IAsyncResult res)
        {
            Logger.Info("start handling password request");
            StartAccept(_password, HandlePassword); //listen for new connections again
            try
            {
                var client = _password.EndAcceptTcpClient(res);
                //proceed
                var vcas = new KeyblockCall();
                vcas.Handle(client.GetStream(), true);
                client.Close();
            }
            catch (Exception ex)
            {
                if (!_stopping) Logger.Warn($"Failed to handle Password: {ex.Message}");
            }
        }

        private void HandleVcas(IAsyncResult res)
        {
            Logger.Info("start handling vcas request");
            StartAccept(_vcs, HandleVcas); //listen for new connections again
            try
            {
                var client = _vcs.EndAcceptTcpClient(res);
                //proceed
                _ssl.ProcessClient(client);
            }
            catch (Exception ex)
            {
                if(!_stopping) Logger.Warn($"Failed to handle Vcas: {ex.Message}");
            }
        }

        private void HandleKeyblock(IAsyncResult res)
        {
            Logger.Info("start handling keyblock request");
            StartAccept(_vks, HandleKeyblock); //listen for new connections again
            try
            {
                var client = _vks.EndAcceptTcpClient(res);
                //proceed
                var vks = new KeyblockCall();
                vks.Handle(client.GetStream(), true);
                client.Close();
            }
            catch (Exception ex)
            {
                if (!_stopping) Logger.Warn($"Failed to handle Keyblock: {ex.Message}");
            }
        }

        private void HandleStartBox(IAsyncResult res)
        {
            Logger.Info("start handling start box request");
            StartAccept(_start, HandleStartBox); //listen for new connections again
            
            try
            {
                var client = _start.EndAcceptTcpClient(res);
                //proceed
                Logger.Debug("Handle the start box stream");
                var clientStream = client.GetStream();
                var data = KeyblockCall.Read(clientStream);
                Logger.Debug($"Read {data.Length} bytes from the start box stream");
                File.WriteAllBytes(Path.Combine(_appDataFolder, "KeyblockMessages", "12686.request"), data);

                Logger.Info("Connect to remote to handle start box request");
                var remote = new TcpClient();
                remote.Connect(_fallbackKeyServer, 12686);
                var remoteStream = remote.GetStream();
                remoteStream.Write(data, 0, data.Length);
                var buff = new byte[2048];
                var size = remoteStream.Read(buff, 0, buff.Length);
                File.WriteAllBytes(Path.Combine(_appDataFolder, "KeyblockMessages", "12686.response"), buff.Take(size).ToArray());

                var response = File.ReadAllBytes(Path.Combine(_appDataFolder, "KeyblockMessages", "12686.response"));
                Logger.Info($"Write {response.Length} bytes to the start box stream");
                clientStream.Write(response, 0, response.Length);
                clientStream.Flush();
                client.Close();
            }
            catch (Exception ex)
            {
                if(!_stopping) Logger.Warn($"Something went wrong when handling start box request: {ex.Message}");   
            }  
        }

        IPAddress GetIpAddress()
        {
            return IPAddress.Any;
        }
    }
}
