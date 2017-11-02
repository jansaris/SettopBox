using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using log4net;
using Newtonsoft.Json;
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
            _channels = null;
            _settings.Load();
            _threadHelper.RunSafeInNewThread(LoadChannelListLoop, Logger, ThreadPriority.BelowNormal);
        }

        private void LoadChannelListLoop()
        {
            while (!ModuleShouldStop())
            {
                WaitForSpecificState(ModuleState.Running, () => {});
                LoadChannelList();
                LoadChannelsFile();
                if(_settings.ScanForKeyblockIds) RetrieveKeyblockIds();
                SaveChannelsFile();
                if (!ModuleShouldStop()) ChangeState(ModuleState.Idle);
            }
        }

        private void SaveChannelsFile()
        {
            if (_channels == null) return;
            try
            {
                if (!Directory.Exists(_settings.DataFolder)) Directory.CreateDirectory(_settings.DataFolder);
                var file = Path.Combine(_settings.DataFolder, _settings.ChannelsFile);
                var json = JsonConvert.SerializeObject(_channels);
                File.WriteAllText(file, json);
                Logger.Info($"Saved updated channellist to {file}");
            }
            catch (Exception ex)
            {
                Logger.Warn($"Something went wrong when saving channels file to disk: {ex.Message}");
                Logger.Debug("SaveChannelsFile", ex);
            }
        }

        private void LoadChannelsFile()
        {
            if (ModuleShouldStop()) return;
            try
            {
                var file = Path.Combine(_settings.DataFolder, _settings.ChannelsFile);
                if (!File.Exists(file))
                {
                    Logger.Info($"No channels file available yet: {file}");
                    return;
                }

                var json = File.ReadAllText(file);
                var data = JsonConvert.DeserializeObject<List<ChannelInfo>>(json);
                Logger.Info($"Read channellist from disk {file}");

                if (_channels == null)
                {
                    Logger.Warn($"No channels loaded from the server, use the local channels ({data?.Count})");
                    _channels = data;
                    return;
                }

                foreach (var channel in data)
                {
                    var matchedChannel = _channels.FirstOrDefault(c => c.ToString() == channel.ToString());
                    if (matchedChannel == null)
                    {
                        Logger.Info($"Failed to find a match for channel '{channel}'");
                        continue;
                    }

                    foreach (var location in channel.Locations.Where(c => c.KeyblockId != -1))
                    {
                        var matchedLocation = matchedChannel.Locations.FirstOrDefault(l => l.Url == location.Url);
                        if (matchedLocation == null)
                        {
                            Logger.Info($"Failed to find a match for location '{location}'");
                            continue;
                        }

                        Logger.Info($"Resolved Keyblock ID {location.KeyblockId} from disk for {channel.Name}-{location}");
                        matchedLocation.KeyblockId = location.KeyblockId;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Something went wrong when reading channels file from disk: {ex.Message}");
                Logger.Debug("LoadChannelsFile", ex);
            }
        }

        private void RetrieveKeyblockIds()
        {
            if (ModuleShouldStop() || _channels == null) return;
            foreach (var channel in _channels)
            {
                if(channel.Locations.All(c => c.KeyblockId != -1)) continue;

                foreach (var location in channel.Locations)
                {
                    if (ModuleShouldStop()) return;
                    if(location.KeyblockId != -1) continue;
                    var iptv = _channelFactory();
                    iptv.OnlySearchForKeys = true;
                    var info = iptv.ReadInfo(location, channel.Name);
                    location.KeyblockId = info?.Number ?? -1;
                }
                Logger.Info($"Resolved Keyblock ID's for {channel.Name}");
            }
        }

        private void LoadChannelList()
        {
            if (ModuleShouldStop()) return;
            _channels = _channelList.Load();
            _lastRetrieval = DateTime.Now;
            _lastRetrievalState = _channels == null
                ? "Something went wrong, look in the log"
                : $"Downloaded {_channels.Count} channels";
        }

        protected override void StopModule()
        {
            _lastRetrieval = null;
        }
    }
}
