using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using log4net;
using SharedComponents.DependencyInjection;
using SharedComponents.Helpers;
using SharedComponents.Module;
using SharedComponents.Models;

namespace ChannelList
{
    public class Program : BaseModule
    {
        readonly Settings _settings;
        readonly IThreadHelper _threadHelper;
        readonly ChannelList _channelList;
        private readonly Func<IptvChannel> _channelFactory;
        DateTime? _lastRetrieval;
        List<ChannelInfo> _channels;
        string _lastRetrievalState;

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

        public Program(ILog logger, LinuxSignal signal, ModuleCommunication moduleCommunication, Settings settings, IThreadHelper threadHelper, ChannelList channelList, Func<IptvChannel> channelFactory) : base(logger, signal, moduleCommunication)
        {
            _settings = settings;
            _threadHelper = threadHelper;
            _channelList = channelList;
            _channelFactory = channelFactory;
        }

        public override IModuleInfo GetModuleInfo()
        {
            return new ChannelListInfo
            {
                LastRetrieval = _lastRetrieval,
                State = _lastRetrievalState
            };
        }

        public override IModuleInfo GetData()
        {
            var info = (ChannelListInfo)GetModuleInfo();
            info.Channels = _channels;
            return info;
        }

        protected override void StartModule()
        {
            Logger.Info("Welcome to Keyblock");
            _lastRetrievalState = string.Empty;
            _settings.Load();
            _threadHelper.RunSafeInNewThread(LoadChannelListLoop, Logger, ThreadPriority.BelowNormal);
        }

        private void LoadChannelListLoop()
        {
            while (!ModuleShouldStop())
            {
                WaitForSpecificState(ModuleState.Running, () => {});
                LoadChannelList();
                RetrieveKeyblockIds();
            }
        }

        private void RetrieveKeyblockIds()
        {
            foreach (var channel in _channels)
            {
                foreach (var location in channel.Locations)
                {
                    if (ModuleShouldStop()) return;
                    var iptv = _channelFactory();
                    var info = iptv.ReadInfo(location, channel.Name);
                    location.KeyblockId = info.Number.Value;
                }
                Logger.Info($"Resolved Keyblock ID's for {channel.Name}");
            }
        }

        private void LoadChannelList()
        {
            _channels = _channelList.Load();
            _lastRetrieval = DateTime.Now;
            _lastRetrievalState = _channels == null
                ? "Something went wrong, look in the log"
                : $"Downloaded {_channels.Count} channels";
            ChangeState(ModuleState.Idle);
        }

        protected override void StopModule()
        {
            _channels = null;
            _lastRetrieval = null;
        }
    }
}
