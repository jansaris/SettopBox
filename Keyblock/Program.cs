using System;
using System.IO;
using System.Threading.Tasks;
using log4net;
using log4net.Config;

namespace Keyblock
{
    class Program
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(Program));

        readonly IniSettings _settings;
        readonly IKeyblock _keyblock;

        Program()
        {
            _settings = new IniSettings();
            _keyblock = new Keyblock(_settings, new SslTcpClient(_settings));
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
            LoadKeyBlock();
            Close();
        }

        void LoadIni()
        {
            _settings.Load();
        }

        void LoadKeyBlock()
        {
            for (var i = 1; i <= _settings.MaxRetries; i++)
            {
                Logger.Info($"Start loading keyblock at run {i}/{_settings.MaxRetries}");
                if (_keyblock.DownloadNew())
                {
                    Logger.Info($"Succesfully loaded a new keyblock at run {i}/{_settings.MaxRetries}");
                    return;
                }
                Logger.Error($"Failed to download a new keyblock at run {i}/{_settings.MaxRetries}");
                _keyblock.CleanUp();
                Logger.Info($"Give the server '{_settings.WaitOnFailingBlockRetrievalInMilliseconds}ms' time");
                Task.Delay(_settings.WaitOnFailingBlockRetrievalInMilliseconds).Wait();
            }
            Logger.Error($"Failed to retrieve the keyblock after {_settings.WaitOnFailingBlockRetrievalInMilliseconds} times, stop trying");
        }

        void Close()
        {
            _settings.Save();
        }
      
    }
}
