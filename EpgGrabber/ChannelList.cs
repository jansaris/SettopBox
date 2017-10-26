using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EpgGrabber.Models;
using log4net;
using SharedComponents.Models;
// ReSharper disable InconsistentlySynchronizedField

namespace EpgGrabber
{
    public class ChannelList
    {
        private readonly Settings _settings;
        private readonly ILog _logger;
        static readonly object Lock = new object();
        private FileInfo File => new FileInfo(Path.Combine(_settings.DataFolder, _settings.EpgChannelListFile));
        public List<string> Channels { get; private set; } = new List<string>();


        public ChannelList(Settings settings, ILog logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public void LoadChannelsFromDisk()
        {
            lock (Lock)
            {
                if (!File.Exists)
                {
                    _logger.Warn($"No channel list file available at '{File.FullName}'. No channels will be filtered");
                    return;
                }
                try
                {
                    Channels = System.IO.File.ReadAllLines(File.FullName)
                        .Where(item => !string.IsNullOrWhiteSpace(item))
                        .Distinct()
                        .ToList();
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Failed to read channel list file '{File.FullName}'. No channels will be filtered", ex);
                    Channels = new List<string>();
                }
            }
        }

        public List<Channel> FilterOnSelectedChannels(List<Channel> epgData)
        {
            if (Channels.Count == 0) return epgData;

            var current = epgData.Count;
            var filtered = epgData.Where(e => Channels.Any(c => c.Equals(e.Name, StringComparison.InvariantCultureIgnoreCase))).ToList();
            if (current != filtered.Count)
            {
                _logger.Debug($"Filtered {current - filtered.Count} channels");
            }
            return filtered;
        }

        internal void ToggleChannel(EpgChannelUpdate epgChannelUpdate)
        {
            if (epgChannelUpdate == null) return;
            if (epgChannelUpdate.Enabled)
            {
                if (!Channels.Contains(epgChannelUpdate.Id))
                {
                    _logger.Info($"Add channel {epgChannelUpdate.Id} - {epgChannelUpdate.Name}");
                    Channels.Add(epgChannelUpdate.Id);
                }
            }
            else
            {
                if (Channels.Contains(epgChannelUpdate.Id))
                {
                    _logger.Info($"Remove channel {epgChannelUpdate.Id} - {epgChannelUpdate.Name}");
                    Channels.Remove(epgChannelUpdate.Id);
                }
            }
            SaveChannelBocksToDisk();
        }

        private void SaveChannelBocksToDisk()
        {
            lock (Lock)
            {
                _logger.Info($"Save channel list ({Channels.Count}) to disk");
                try
                {
                    using (var writer = new StreamWriter(File.OpenWrite()))
                    {
                        Channels.ForEach(writer.WriteLine);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to write channel list file '{File.FullName}'.", ex);
                }
            }
        }
    }
}