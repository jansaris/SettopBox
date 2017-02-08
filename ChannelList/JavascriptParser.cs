using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using log4net;
using SharedComponents.Module;

namespace ChannelList
{
    public class JavascriptParser
    {
        readonly ILog _logger;

        public JavascriptParser(ILog logger)
        {
            _logger = logger;
        }

        private const string ChannelSeparatorRegex = "e.push\\(\"";
        private const string ChannelSeparator = "e.push(";
        private const string ChannelNameStart = "\"default\":\"";
        private const string IconEnd = ".png";
        private const string ChannelLocationUrlStart = "b.h=";
        private const string RtpSkip = ";rtpskip=yes";
        private const string ChannelLocationStart = "b.b={\"default\":\"";
        private const string IsRadioChannel = "q=\"radio";
        private const string ChannelNumberRegex = "a=\\d*;";

        public List<ChannelInfo> ParseChannels(string script)
        {
            script = Clean(script);
            var first = script.IndexOf(ChannelSeparator, StringComparison.InvariantCulture);
            if (first == -1)
            {
                _logger.Error($"Failed to find '{ChannelSeparator}' in script");
                return new List<ChannelInfo>();
            }
            script = script.Substring(first);
            //Split script by every e.push(" statement
            var channelParts = Regex.Split(script, ChannelSeparatorRegex, RegexOptions.IgnoreCase).ToList();
            channelParts.RemoveAt(0); // first item does not count
            return channelParts.Select(ConvertToChannel).Where(ci => ci != null).ToList();
        }

        private string Clean(string script)
        {
            return script.Replace("\r", "").Replace("\n", "");
        }

        private ChannelInfo ConvertToChannel(string scriptPart)
        {
            //Failed to find a name in this channel part, so we can't use it
            if (!scriptPart.Contains(ChannelNameStart)) return null;
            var keyEnd = scriptPart.IndexOf('"');
            if (keyEnd == -1)
            {
                _logger.Warn($"Failed to find key in scriptPart: {scriptPart}");
                return null;
            }
            var key = scriptPart.Substring(0, keyEnd);
            var channel = new ChannelInfo
            {
                Key = key,
                Name = ExtractChannelName(scriptPart, key),
                Icons = ExtractIcons(scriptPart, key),
                Locations = ExtractLocations(scriptPart, key),
                Radio = scriptPart.Contains(IsRadioChannel),
                Number = ExtractChannelNumber(scriptPart)
            };

            return channel;
        }

        private int ExtractChannelNumber(string scriptPart)
        {
            var part = Regex.Matches(scriptPart, ChannelNumberRegex, RegexOptions.IgnoreCase);
            if (part.Count <= 0) return -1;
            var channelNumber = part[0].Value.Replace("a=", "").Replace(";", "");
            if (string.IsNullOrWhiteSpace(channelNumber)) return -2;
            return int.Parse(channelNumber);
        }

        private string ExtractChannelName(string scriptPart, string key)
        {
            var start = scriptPart.IndexOf(ChannelNameStart, StringComparison.InvariantCulture) + ChannelNameStart.Length;
            var length = scriptPart.IndexOf('"', start) - start;
            var name = scriptPart.Substring(start, length);
            _logger.Debug($"Extracted name {name} for key {key}");
            return name;
        }

        private List<string> ExtractIcons(string scriptPart, string key)
        {
            var icons = new List<string>();
            var index = scriptPart.IndexOf(IconEnd, StringComparison.InvariantCulture);
            while (index != -1)
            {
                index += IconEnd.Length;
                var begin = scriptPart.LastIndexOf('"', index - 1) + 1;
                if (begin != -1)
                {
                    var end = index;
                    var length = end - begin;
                    var icon = scriptPart.Substring(begin, length);
                    _logger.Debug($"Extracted icon: {icon} for {key}");
                    icons.Add(icon);
                }
                else
                {
                    _logger.Warn($"Failed to extract icon at index {index} from scriptpart '{scriptPart}'");
                }
                index = scriptPart.IndexOf(IconEnd, index, StringComparison.InvariantCulture);
            }
            return icons.Distinct().ToList();
        }

        private List<ChannelLocation> ExtractLocations(string scriptPart, string key)
        {
            var channels = new List<ChannelLocation>();
            var previousUrlIndex = 0;
            var urlIndex = scriptPart.IndexOf(ChannelLocationUrlStart, StringComparison.InvariantCulture);
            while (urlIndex != -1)
            {
                var location = new ChannelLocation();
                urlIndex+= ChannelLocationUrlStart.Length + 1;
                var urlEnd = scriptPart.IndexOf('"', urlIndex);
                if (urlEnd != -1)
                {
                    location.Url = scriptPart.Substring(urlIndex, urlEnd - urlIndex);
                    if (location.Url.EndsWith(RtpSkip))
                    {
                        location.Url = location.Url.Substring(0, location.Url.Length - RtpSkip.Length);
                        location.RtpSkip = true;
                    }

                }
                var nameIndex = scriptPart.IndexOf(ChannelLocationStart, previousUrlIndex, StringComparison.InvariantCulture);
                if (nameIndex != -1 && nameIndex < urlIndex)
                {
                    nameIndex += ChannelLocationStart.Length;
                    var nameEnd = scriptPart.IndexOf('"', nameIndex);
                    if (nameEnd != -1)
                    {
                        location.Name = scriptPart.Substring(nameIndex, nameEnd - nameIndex);
                    }
                }
                _logger.Debug($"Add to channel {key} location: {location}");
                channels.Add(location);
                previousUrlIndex = urlIndex;
                urlIndex = scriptPart.IndexOf(ChannelLocationUrlStart, urlIndex, StringComparison.InvariantCulture);
            }
            return channels;
        }
    }
}
