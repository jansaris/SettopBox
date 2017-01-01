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

        private const string StartFunction = "function Rc(";
        private const string ChannelSeparatorRegex = "e.push\\(\"";
        private const string ChannelNameStart = "\"default\":";
        private const string ChannelNumberStart = "a=";
        private const string ChannelLocationStart = "b.b=";
        private const string ChannelLocationNameStart = "\"default\":";
        private const string ChannelLocationUrlStart = "b.h=";
        private const string IsRadioChannel = ",z:1,";
        private const string IconEnd = ".png";

        /// <summary>
        /// Parses the channnels from the Tv menu script file
        /// </summary>
        /// <param name="script">The script to parse.</param>
        /// <returns></returns>
        public List<ChannelInfo> ParseChannnels(string script)
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
