using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharedComponents.Models;

namespace ChannelList
{
    public class RtspDataParser
    {
        public List<ChannelInfo> ParseChannels(byte[] rtspData)
        {
            var data = Encoding.ASCII.GetString(rtspData).Replace("\r", "").Split('\n');
            var channels = new List<ChannelInfo>();

            var channelStart = Array.FindIndex(data, 0, s => s == string.Empty);
            while (channelStart > 0)
            {
                var channelEnd = Array.FindIndex(data, channelStart, s => s.StartsWith("--"));
                if (channelEnd == -1) channelEnd = data.Length;
                channels.Add(ParseChannel(data.Skip(channelStart).Take(channelEnd - channelStart)));
                channelStart = channelEnd != data.Length ? channelEnd + 1 : -1;
            }

            return channels;
        }

        private ChannelInfo ParseChannel(IEnumerable<string> messageBlock)
        {
            var channel = new ChannelInfo();
            foreach (var line in messageBlock)
            {
                if(line.Length == 0) continue;
                
                switch (line[0])
                {
                    case 's': channel.Name = ExtractChannelName(line);
                        channel.Number = ExtractChannelNumber(line);
                        break;
                }
            }
            return channel;
        }

        private int ExtractChannelNumber(string line)
        {
            var end = line.IndexOf(" ", StringComparison.Ordinal);
            return int.Parse(line.Substring(2, end - 2));
        }

        private string ExtractChannelName(string line)
        {
            var start = line.IndexOf(" ", StringComparison.Ordinal);
            return line.Substring(start + 1);
        }
    }
}
