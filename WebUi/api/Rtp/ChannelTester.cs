using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using WebUi.api.Models;

namespace WebUi.api.Rtp
{
    public class ChannelTester
    {
        ILog _log;
        const int MaxCycles = 2048;

        public ChannelTester(ILog logger)
        {
            _log = logger;
        }

        public RtpInfo ReadInfo(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            var data = ReadData(url);
            return data;
        }

        public RtpInfo ReadData(string url)
        {
            var previous = new byte[0];
            var current = new byte[0];
            var info = new RtpInfo();

            try
            {
                _log.Info($"Try to read data from {url}");
                using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    s.ReceiveTimeout = 100;

                    var port = ExtractPort(url);
                    var strippedUrl = StripUrl(url);

                    var ipep = new IPEndPoint(IPAddress.Any, port);
                    var ip = IPAddress.Parse(strippedUrl);

                    s.Bind(ipep);
                    s.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(ip, IPAddress.Any));

                    for(var count = 0; count < MaxCycles; count++)
                    {
                        var buffer = new byte[2048];
                        var length = s.Receive(buffer);
                        current = buffer.Take(length).ToArray();
                        UpdateRtpInfo(info, previous, current);
                        if (info.Complete())
                        {
                            _log.Debug($"Found IPTV Info after {count} blocks");
                            break;
                        }
                        previous = current;
                    }
                }
            }
            catch(Exception ex)
            {
                _log.Error($"Failed to read data from {url}: {ex.Message}");
            }

            return info;
        }

        private void UpdateRtpInfo(RtpInfo info, byte[] previous, byte[] current)
        {
            var data = new byte[previous.Length + current.Length];
            previous.CopyTo(data, 0);
            current.CopyTo(data, previous.Length);

            if(FindChannelInfo(data, out string provider, out string name))
            {
                info.Provider = provider;
                info.Name = name;
            }

            if(FindChannelNumber(data, out int channel))
            {
                info.Number = channel;
            }
        }

        private bool FindChannelNumber(byte[] data, out int channel)
        {
            channel = -1;
            //Find VMECM
            var vmecmbytes = Encoding.ASCII.GetBytes("VMECM");
            var index = SearchBytes(data, vmecmbytes, 0);
            if (index == -1)
            {
                _log.Debug("VMECM not found in the bytes");
                return false;
            }
            index += vmecmbytes.Length + 6;
            if (data.Length <= index)
            {
                _log.Info("Found VMECM but the message was truncated");
                return false;
            }
            channel = (data[index - 1] << 8) + data[index];
            _log.Info($"Found channel {channel} in the bytes");
            return true;
        }

        private bool FindChannelInfo(byte[] data, out string provider, out string name)
        {
            name = null;
            provider = null;
            var headerbytes = new byte[] { 0, 0, 0, 1, 255 };

            var index = SearchBytes(data, headerbytes, 0);
            if (index == -1) return false;
            index += headerbytes.Length + 8;

            index = ReadStringFromData(data, index, out provider);
            if (index == -1)
            {
                _log.Debug("Failed to read the provider");
                return false;
            }

            index = ReadStringFromData(data, index, out name);
            if (index == -1)
            {
                _log.Debug("Failed to read the channel name");
                return false;
            }

            return true;
        }

        public int SearchBytes(byte[] haystack, byte[] needle, int start_index)
        {
            int len = needle.Length;
            int limit = haystack.Length - len;
            for (int i = start_index; i <= limit; i++)
            {
                int k = 0;
                for (; k < len; k++)
                {
                    if (needle[k] != haystack[i + k]) break;
                }
                if (k == len) return i;
            }
            return -1;
        }

        /// <summary>
        /// Tries to read the string from the data block
        /// And returns the index after the string
        /// </summary>
        /// <param name="data">Data block</param>
        /// <param name="index">starting index of the message</param>
        /// <param name="message">The message if reading is successfull</param>
        /// <returns>The new index, or -1 if failed</returns>
        private int ReadStringFromData(byte[] data, int index, out string message)
        {
            message = null;
            if (data.Length <= index)
            {
                _log.Debug("Failed to read string length because the message was truncated");
                return -1;
            }

            int length = data[index];
            index++;
            if (data.Length < index + length)
            {
                _log.Debug("Failed to read string block because the message was truncated");
                return -1;
            }
            //Read the provider name (excluding the length bit)
            message = Encoding.ASCII.GetString(data, index, length);
            if (message.EndsWith("\0")) message = message.Substring(0, message.Length - 1);
            _log.Debug($"Read string '{message}' from the data");
            return index + length;
        }

        private int ExtractPort(string url)
        {
            var index = url.LastIndexOf(":");
            if (index > 0)
            {
                index++;
                return int.Parse(url.Substring(index, url.Length - index));
            }
            _log.Warn($"Failed to extract port from {url}");
            return 0;
        }

        private void ParseData(byte[] b, int length)
        {
            _log.Info($"Start reading {length} bytes of data");
        }

        private string StripUrl(string url)
        {
            return StripPort(StripProtocol(url));
        }

        private string StripPort(string url)
        {
            var index = url.IndexOf(":");
            if (index > 0)
            {
                return url.Substring(0, index);
            }
            _log.Warn($"Failed to strip port from {url}");
            return url;
        }

        private string StripProtocol(string url)
        {
            var index = url.IndexOf("://");
            if(index > 0)
            {
                index += 3;
                return url.Substring(index, url.Length - index);
            }
            _log.Warn($"Failed to strip protocol from {url}");
            return url;
        }
    }
}
