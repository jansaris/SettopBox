using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using SharedComponents.Models;

namespace ChannelList
{
    public class RtspDataParser
    {
        private readonly ILog _logger;

        public RtspDataParser(ILog logger)
        {
            _logger = logger;
        }

        public List<ChannelInfo> ParseChannels(byte[] rtspData)
        {
            var data = Encoding.ASCII.GetString(rtspData).Replace("\r", "").Split('\n');
            var channels = new List<ChannelInfo>();

            int channelStart = 0, channelEnd = 0;
            try
            {
                channelStart = Array.FindIndex(data, 0, s => s == string.Empty);
                while (channelStart > 0)
                {
                    channelEnd = Array.FindIndex(data, channelStart, s => s.StartsWith("--"));
                    if (channelEnd == -1) channelEnd = data.Length;
                    channels.Add(ParseChannel(data.Skip(channelStart).Take(channelEnd - channelStart).ToList()));
                    channelStart = channelEnd != data.Length ? channelEnd + 1 : -1;
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"Something went wrong when parsing the rtspData between lines {channelStart} and {channelEnd}: {ex.Message}");
                _logger.Warn($"rtspData:\r\n{string.Join("\r\n", data)}");
            }

            channels = MergeChannels(channels);

            return channels;
        }

        private List<ChannelInfo> MergeChannels(List<ChannelInfo> channels)
        {
            var retValue = new List<ChannelInfo>();
            foreach (var channel in channels)
            {
                var match = retValue.FirstOrDefault(c => c.Name == channel.Name);
                if (match != null)
                {
                    foreach (var channelLocation in channel.Locations)
                    {
                        match.AddLocation(channelLocation);
                    }
                }
                else
                {
                    retValue.Add(channel);
                }
            }
            return retValue;
        }

        private ChannelInfo ParseChannel(IList<string> messageBlock)
        {
            var channel = new ChannelInfo();

            var nameAndLocation = messageBlock.First(m => m.StartsWith("s"));
            channel.Name = ExtractChannelName(nameAndLocation);
            channel.Number = ExtractChannelNumber(nameAndLocation);

            channel.AddLocation(ExtractLocation(messageBlock));
            if (channel.FirstLocationQuality < 3000) channel.Radio = true;

            return channel;
        }

        private ChannelLocation ExtractLocation(IList<string> messageBlock)
        {
            /*
             m=video 8248 RTP/AVPF 96
             i=Original Source Stream
             c=IN IP4 224.0.251.124/255
             b=AS:12750
            */
            var portAndProtocol = messageBlock.First(m => m.StartsWith("m=video")).Substring(8).Split(' ');
            var address = messageBlock.First(m => m.StartsWith("c=IN IP4")).Substring(9);
            var bitrate = messageBlock.First(m => m.StartsWith("b=AS:")).Substring(5);

            //Generate URL
            var port = portAndProtocol[0];
            //Extract RTP from RTP/AVPF
            if (portAndProtocol[1].Contains("/")) portAndProtocol[1] = portAndProtocol[1].Substring(0, portAndProtocol[1].IndexOf('/'));
            var protocol = portAndProtocol[1];
            //Extract /255 from 224.0.251.124/255
            if(address.Contains("/")) address = address.Substring(0, address.IndexOf('/'));


            var location = new ChannelLocation
            {
                Url = $"{protocol.ToLower()}://{address}:{port}",
                Bitrate = int.Parse(bitrate)
            };
            return location;
        }

        private int ExtractChannelNumber(string line)
        {
            var end = line.IndexOf(" ", StringComparison.Ordinal);
            return int.Parse(line.Substring(2, end - 2));
        }

        private string ExtractChannelName(string line)
        {
            var start = line.IndexOf(" ", StringComparison.Ordinal);
            var name = line.Substring(start + 1);
            if (name.EndsWith("Glas")) name = name.Substring(0, name.Length - 5);
            if (name.EndsWith("HD")) name = name.Substring(0, name.Length - 3);
            return name;
        }
    }
}
