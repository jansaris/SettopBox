using System;
using System.IO;
using log4net;
using log4net.Config;

namespace Test.NewCamdClient
{
    class Program
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof (Program));
        readonly NewCamdClient _client = new NewCamdClient();

        const string IpAdress = "localhost";
        const int Port = 15050;

        static void Main(string[] args)
        {
            try
            {
                XmlConfigurator.ConfigureAndWatch(new FileInfo("Log4net.config"));
                Logger.Info("Welcome to the NewCamd Test Client");
                var program = new Program();
                program.Run();
            }
            catch (Exception ex)
            {
                Logger.Fatal("An unhandled exception occured", ex);
            }
            Logger.Info("Thanks for using the NewCamd Test Client");
        }

        void Run()
        {
            ShowHelp();
            var line = "";
            try
            {
                while (line != "5")
                {
                    line = Console.ReadLine();
                    HandleLine(line);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception occured during job: {line}", ex);
                _client.Disconnect();
            }
        }

        void HandleLine(string line)
        {
            switch (line)
            {
                case "1":
                    _client.Connect(IpAdress, Port);
                    break;
                case "2":
                    _client.Login();
                    break;
                case "3":
                    _client.GetKey();
                    break;
                case "4":
                    _client.Disconnect();
                    break;
                case "5":
                    Logger.Info("Exit test module");
                    break;
                default:
                    ShowHelp();
                    break;
            }
        }

        static void ShowHelp()
        {
            Logger.Info("Hit one of the following numbers and end with enter:");
            Logger.Info(" 1: Connect");
            Logger.Info(" 2: Login");
            Logger.Info(" 3: ReceiveKey");
            Logger.Info(" 4: Disconnect");
            Logger.Info(" 5: Exit");
        }
    }
}
