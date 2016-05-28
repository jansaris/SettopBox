using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EpgGrabber.Models;
using log4net;

namespace EpgGrabber
{
    public class ChannelList
    {
        readonly Settings _settings;
        readonly ILog _logger;
        public List<string> Channels { get; private set; } = new List<string>();

        public ChannelList(Settings settings, ILog logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public void LoadChannelsFromDisk()
        {
            var file = new FileInfo(Path.Combine(_settings.DataFolder, _settings.EpgChannelListFile));
            if (!file.Exists)
            {
                _logger.Warn($"No channel list file available at '{file.FullName}'. No channels will be filtered");
                return;
            }
            try
            {
                Channels = File.ReadAllLines(file.FullName).ToList();
            }
            catch (Exception ex)
            {
                _logger.Warn($"Failed to read channel list file '{file.FullName}'. No channels will be filtered", ex);
                Channels = new List<string>();
            }
        }

        public List<Channel> FilterOnSelectedChannels(List<Channel> epgData)
        {
            if(Channels.Count == 0) return epgData;

            var current = epgData.Count;
            var filtered = epgData.Where(e => Channels.Contains(e.Name)).ToList();
            if (current != filtered.Count)
            {
                _logger.Debug($"Filtered {current - filtered.Count} channels");
            }
            return filtered;
        }
    }
}