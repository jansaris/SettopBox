using System;
using System.IO;
using log4net;
using SharedComponents.DependencyInjection;
using SharedComponents.Module;

namespace TvHeadendIntegration
{
    class Program : BaseModule
    {
        readonly TvHeadendIntegrationInfo _info = new TvHeadendIntegrationInfo();
        readonly ILog _logger;
        readonly Settings _settings;
        readonly UpdateEpg _epg;

        Program(ILog logger, Settings settings, UpdateEpg epg)
        {
            _logger = logger;
            _settings = settings;
            _epg = epg;
        }

        static void Main()
        {
            var container = SharedContainer.CreateAndFill<DependencyConfig>("Log4net.config");
            var prog = container.GetInstance<Program>();
            prog.Start();
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
            prog.Stop();
            container.Dispose();
        }

        public override IModuleInfo GetModuleInfo()
        {
            return (TvHeadendIntegrationInfo)_info.Clone();
        }

        protected override void StartModule()
        {
            _logger.Info("Welcome to TvHeadendIntegration");
            _settings.Load();
            if (!string.IsNullOrWhiteSpace(_settings.InitialEpgFile))
            {
                _epg.SendToTvheadend(Path.Combine(_settings.DataFolder, _settings.InitialEpgFile));
            }
        }

        public override void ProcessDataFromOtherModule(string moduleName, CommunicationData data)
        {
            base.ProcessDataFromOtherModule(moduleName, data);
            switch (data.Type)
            {
                case DataType.Epg:
                    _info.LastEpgUpdateSuccessfull = _epg.SendToTvheadend(data.Data.ToString());
                    _info.LastEpgUpdate = DateTime.Now;
                    break;
                default:
                    //Ignore other 
                    break;
            }
        }

        protected override void StopModule()
        {
        }
    }
}
