using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;
using SharedComponents.Models;

namespace ChannelList
{
    public class ChannelList
    {
        private readonly ILog _logger;
        private readonly Settings _settings;
        private readonly RtspDataReceiver _receiver;
        private readonly RtspDataParser _parser;


        public ChannelList(ILog logger, Settings settings, RtspDataReceiver receiver, RtspDataParser parser)
        {
            _logger = logger;
            _settings = settings;
            _receiver = receiver;
            _parser = parser;
        }

        public List<ChannelInfo> Load()
        {
            try
            {
                _logger.Info($"Start downloading data from {_settings.Host}:{_settings.Port}");
                var data = _receiver.ReadDataFromServer(_settings.Host, _settings.Port);
                if(data.Length == 0) throw new InvalidDataException($"Received no data from {_settings.Host}:{_settings.Port}");
                var file = Path.Combine(_settings.DataFolder, _settings.RawChannelsFile);
                File.WriteAllBytes(file, data);
                _logger.Info($"Received {data.Length} bytes of channel list data");
                var channels = _parser.ParseChannels(data);
                _logger.Info($"Parsed the data into {channels.Count} channels");
                return channels.Where(c => c.Number != -1).OrderBy(c => c.Number).ToList();
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load the channel list: {ex.Message}", ex);
                return null;
            }
        }
    }
}
