using System;
using System.Threading.Tasks;
using log4net;
using SharedComponents.DependencyInjection;

namespace Keyblock
{
    class Program
    {
        readonly ILog _logger;
        readonly Settings _settings;
        readonly Keyblock _keyblock;

        public Program(ILog logger, Settings settings, Keyblock keyblock)
        {
            _logger = logger;
            _settings = settings;
            _keyblock = keyblock;
        }

        static void Main()
        {
            var container = SharedContainer.CreateAndFill<DependencyConfig>("Log4net.config");
            var prog = container.GetInstance<Program>();
            prog.Run();
        }

        void Run()
        {
            try
            {
                _logger.Info("Welcome to Keyblock");
                _settings.Load();
                LoadKeyBlock();
                _logger.Info("Done");
            }
            catch (Exception ex)
            {
                _logger.Fatal("An unhandled exception occured", ex);
            }
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
    }
}
