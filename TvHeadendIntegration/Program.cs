using System;
using System.IO;
using log4net;
using SharedComponents.DependencyInjection;
using SharedComponents.Helpers;
using SharedComponents.Module;
using TvHeadendIntegration.TvHeadend;
using SharedComponents.Models;

namespace TvHeadendIntegration
{
    class Program : BaseModule
    {
        readonly TvHeadendIntegrationInfo _info = new TvHeadendIntegrationInfo();
        readonly Settings _settings;
        readonly UpdateEpg _epg;
        readonly TvhModel _configuration;

        public Program(ILog logger, Settings settings, UpdateEpg epg, LinuxSignal signal, ModuleCommunication communication, TvhModel tvhConfiguration) : base(logger, signal, communication)
        {
            _settings = settings;
            _epg = epg;
            _configuration = tvhConfiguration;
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
            Logger.Info("Welcome to TvHeadendIntegration");
            _settings.Load();
            if (!string.IsNullOrWhiteSpace(_settings.InitialEpgFile))
            {
                UpdateEpg(Path.Combine(_settings.DataFolder, _settings.InitialEpgFile));
            }
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            _configuration.ReadFromWeb();
            _info.AuthenticationSuccessfull = _configuration.AuthenticationSuccessfull;
            _info.LastAuthentication = DateTime.Now;
            _info.Channels = _configuration.GetChannelInfo();
        }

        void UpdateEpg(string file)
        {
            _info.LastEpgUpdateSuccessfull = _epg.SendToTvheadend(file);
            _info.LastEpgUpdate = DateTime.Now;
        }

        public override void ProcessDataFromOtherModule(string moduleName, CommunicationData data)
        {
            base.ProcessDataFromOtherModule(moduleName, data);
            switch (data.Type)
            {
                case DataType.Epg:
                    UpdateEpg(data.Data.ToString());
                    break;
                case DataType.TvHeadendChannelUpdate:
                    UpdateTvhConfiguration(data.Data as TvHeadendChannelUpdate);
                    break;
            }
        }

        private void UpdateTvhConfiguration(TvHeadendChannelUpdate tcu)
        {
            if (string.IsNullOrWhiteSpace(tcu.TvhId))
            {
                _configuration.AddChannel(tcu.Number, tcu.Name, tcu.NewUrl, tcu.Epg);
            }
            else if (string.IsNullOrWhiteSpace(tcu.NewUrl))
            {
                _configuration.RemoveChannel(tcu.TvhId, tcu.Name);
            }
            else
            {
                _configuration.UpdateChannel(tcu.TvhId, tcu.Name, tcu.NewUrl, tcu.Epg);
            }
        }

        protected override void StopModule()
        {
        }
    }
}
