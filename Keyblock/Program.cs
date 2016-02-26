using System;
using System.IO;
using log4net;
using log4net.Config;

namespace Keyblock
{
    class Program
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(Program));

        readonly Random _random = new Random();
        readonly IniSettings _settings;
        readonly IKeyblock _keyblock;

        Program()
        {
            _settings = new IniSettings();
            _keyblock = new Keyblock(_settings, new SslTcpClient());
        }

        static void Main()
        {
            try
            {
                XmlConfigurator.Configure(new FileInfo("Log4net.config"));
                var prog = new Program();
                prog.Run();
                Logger.Info("Done: Exit");

            }
            catch (Exception ex)
            {
                Logger.Fatal("An unhandled exception occured", ex);
            }
            Console.ReadKey();
        }

        void Run()
        {
            LoadIni();
            LoadClientId();
            LoadMachineId();
            if (!_keyblock.DownloadNew())
            {
                Logger.Error("Failed to download a new keyblock");
            }
            Logger.Info("Succesfully loaded a new keyblock");
            Close();
        }

        void LoadIni()
        {
            _settings.Load();
        }

        void Close()
        {
            _settings.Save();
        }

        void LoadMachineId()
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

        void LoadClientId()
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
