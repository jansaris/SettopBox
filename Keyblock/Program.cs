using System;
using System.Threading.Tasks;
using log4net;
using SharedComponents.DependencyInjection;

namespace Keyblock
{
    class Program
    {
        readonly ILog _logger;
        readonly IniSettings _settings;
        readonly IKeyblock _keyblock;

        public Program(ILog logger, IniSettings settings, IKeyblock keyblock)
        {
            _logger = logger;
            _settings = settings;
            _keyblock = keyblock;
        }

        static void Main()
        {
            var container = SharedContainer.CreateAndFill<DependencyConfig>("Log4net.config");
            var logger = container.GetInstance<ILog>();
            try
            {
                logger.Info("Welcome to Keyblock.exe");
                var prog = container.GetInstance<Program>();
                prog.Run();
                logger.Info("Done: Exit");
            }
            catch (Exception ex)
            {
                logger.Fatal("An unhandled exception occured", ex);
            }
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
                _logger.Info($"Start loading keyblock at run {i}/{_settings.MaxRetries}");
                if (_keyblock.DownloadNew())
                {
                    _logger.Info($"Succesfully loaded a new keyblock at run {i}/{_settings.MaxRetries}");
                    return;
                }
                _logger.Error($"Failed to download a new keyblock at run {i}/{_settings.MaxRetries}");
                _keyblock.CleanUp();
                _logger.Info($"Give the server '{_settings.WaitOnFailingBlockRetrievalInMilliseconds}ms' time");
                Task.Delay(_settings.WaitOnFailingBlockRetrievalInMilliseconds).Wait();
            }
            _logger.Error($"Failed to retrieve the keyblock after {_settings.WaitOnFailingBlockRetrievalInMilliseconds} times, stop trying");
        }

        void Close()
        {
            _settings.Save();
        }
    }
}
