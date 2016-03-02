using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
            if (_keyblock.DownloadNew())
            {
                Logger.Info("Succesfully loaded a new keyblock");
            }
            else
            {
                Logger.Error("Failed to download a new keyblock");
            }
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
      
    }
}
