﻿using System;
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
        public Dictionary<string, string> Channels { get; private set; } = new Dictionary<string, string>();

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
                Channels = File.ReadAllLines(file.FullName)
                    .Select(ParseLine)
                    .Where(item => item != null)
                    .ToDictionary(k => k.Item1, v => v.Item2);
            }
            catch (Exception ex)
            {
                _logger.Warn($"Failed to read channel list file '{file.FullName}'. No channels will be filtered", ex);
                Channels = new Dictionary<string, string>();
            }
        }

        Tuple<string, string> ParseLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;

            var splitted = line.Split('|');
            if (splitted.Length == 2) return new Tuple<string, string>(splitted[0], splitted[1]);

            _logger.Warn("Failed to read line '{item}' in '{file.FullName}' (expect key|name)");
            return null;
        }

        public List<Channel> FilterOnSelectedChannels(List<Channel> epgData)
        {
            if(Channels.Count == 0) return epgData;

            var current = epgData.Count;
            var filtered = epgData.Where(e => Channels.ContainsKey(e.Name)).ToList();
            if (current != filtered.Count)
            {
                _logger.Debug($"Filtered {current - filtered.Count} channels");
            }
            return filtered;
        }
    }
}