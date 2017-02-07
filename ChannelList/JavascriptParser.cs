using System;
using System.Collections.Generic;
using System.Globalization;
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


        private const string StartFunction = "function Rc(";
        private const string ChannelNumberStart = "a=";
       
        private const string ChannelLocationNameStart = "\"default\":";
        
        private const string IsRadioChannel = ",z:1,";
        

        public List<ChannelInfo> ParseChannels(string script)
        {
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
                Locations = ExtractLocations(scriptPart, key)
            };

            return channel;
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
                if (nameIndex != -1)
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

        /// <summary>
        /// Parses the channnels from the Tv menu script file
        /// </summary>
        /// <param name="script">The script to parse.</param>
        /// <returns></returns>
        public List<ChannelInfo> ParseChannnelsOld(string script)
        {
            var result = new List<ChannelInfo>();

            //Remove everything for the actual function
            var posStart = script.IndexOf(StartFunction, StringComparison.InvariantCultureIgnoreCase);
            if (posStart == -1) return result;

            script = script.Substring(posStart);
            //Split script by every e.push(" statement
            var channelParts = Regex.Split(script, ChannelSeparatorRegex, RegexOptions.IgnoreCase).ToList();
            channelParts.RemoveAt(0); // first item does not count

            //Loop through each channel part
            foreach (var channelPart in channelParts)
            {
                var channel = new ChannelInfo { Locations = new List<ChannelLocation>() };
                result.Add(channel);

                //Key
                posStart = channelPart.IndexOf("\"", StringComparison.Ordinal);
                channel.Key = channelPart.Substring(0, posStart);

                //Name
                posStart = channelPart.IndexOf(ChannelNameStart, StringComparison.Ordinal);
                int posEnd;
                if (posStart != -1)
                {
                    posStart = posStart + ChannelNameStart.Length;
                    posEnd = channelPart.IndexOf("}", posStart, StringComparison.Ordinal);
                    if (posEnd != -1)
                        channel.Name = RemoveInvalidCharacters(DecodeEncodedNonAsciiCharacters(channelPart.Substring(posStart, posEnd - posStart)));
                }

                //Icon
                var iconStart = channelPart.IndexOf(IconEnd, StringComparison.Ordinal);
                while (iconStart != -1)
                {
                    var begin = channelPart.LastIndexOf('"', iconStart);
                    if (begin == -1) break;
                    begin++;
                    var end = (iconStart + IconEnd.Length) - begin;
                    var icon = channelPart.Substring(begin, end);
                    channel.Icons.Add(icon);
                    iconStart = channelPart.IndexOf(IconEnd, iconStart + 1, StringComparison.Ordinal);
                }

                //Channel number
                posStart = channelPart.IndexOf(ChannelNumberStart, StringComparison.Ordinal);
                if (posStart != -1)
                {
                    posStart = posStart + ChannelNumberStart.Length;
                    posEnd = channelPart.IndexOf(";", posStart, StringComparison.Ordinal);
                    if (posEnd != -1)
                    {
                        try
                        {
                            channel.Number = int.Parse(channelPart.Substring(posStart, posEnd - posStart));
                        }
                        catch (Exception)
                        {
                            _logger.Error($"Failed to read the channel number for {channel.Key}");
                        }
                    }
                }

                //Radio
                channel.Radio = channelPart.Contains(IsRadioChannel);

                //Channels
                var posChannelsStart = posStart;
                while (true)
                {
                    if (posStart == -1 || posStart >= channelPart.Length)
                        break;

                    posStart = channelPart.IndexOf(ChannelLocationStart, posStart, StringComparison.Ordinal);
                    if (posStart == -1)
                        break;

                    var location = new ChannelLocation();
                    channel.Locations.Add(location);

                    //Name
                    posStart = channelPart.IndexOf(ChannelLocationNameStart, posStart, StringComparison.Ordinal);
                    if (posStart != -1)
                    {
                        posStart = posStart + ChannelLocationNameStart.Length;
                        posEnd = channelPart.IndexOf("}", posStart, StringComparison.Ordinal);
                        if (posEnd != -1)
                            location.Name = RemoveInvalidCharacters(DecodeEncodedNonAsciiCharacters(channelPart.Substring(posStart, posEnd - posStart)));
                    }

                    //Url
                    posStart = channelPart.IndexOf(ChannelLocationUrlStart, posStart, StringComparison.Ordinal);
                    if (posStart != -1)
                    {
                        posStart = posStart + ChannelLocationUrlStart.Length;
                        posEnd = channelPart.IndexOf(";", posStart, StringComparison.Ordinal);
                        if (posEnd != -1)
                            location.Url = RemoveInvalidCharacters(channelPart.Substring(posStart, posEnd - posStart));
                    }
                }

                //Check of we have found any locations
                if (channel.Locations.Count != 0 || posChannelsStart == -1) continue;

                //Check if there is maybe an Url present
                posStart = channelPart.IndexOf(ChannelLocationUrlStart, posChannelsStart, StringComparison.Ordinal);
                if (posStart == -1) continue;
                posStart = posStart + ChannelLocationUrlStart.Length;
                posEnd = channelPart.IndexOf(";", posStart, StringComparison.Ordinal);
                if (posEnd == -1) continue;
                var channelLocation = new ChannelLocation();
                channel.Locations.Add(channelLocation);
                channelLocation.Url = RemoveInvalidCharacters(channelPart.Substring(posStart, posEnd - posStart));
            }

            return result;
        }

        /// <summary>
        /// Removes the invalid characters.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        private static string RemoveInvalidCharacters(string value)
        {
            if (value != null)
            {
                value = value.Replace("'", "");
                value = value.Replace("\"", "");
                value = value.Replace("\n", "");
                value = value.Replace("\r", "");
            }

            return value;
        }

        string DecodeEncodedNonAsciiCharacters(string value)
        {
            return Regex.Replace(value, @"\\u(?<Value>[a-zA-Z0-9]{4})", m => ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString(CultureInfo.InvariantCulture));
        }
    }
}
