using log4net;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using WebUi.api.Models;

namespace WebUi.api.Iptv
{
    public class IptvChannel
    {
        ILog _log;
        Func<IptvSocket> _socketFactory;
        const int MaxCycles = 2048;

        public IptvChannel(ILog logger, Func<IptvSocket> socketFactory)
        {
            _log = logger;
            _socketFactory = socketFactory;
        }

        public IptvInfo ReadInfo(string url, string channel)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            var data = ReadData(url, channel);
            return data;
        }

        IptvInfo ReadData(string url, string channel)
        {
            var buffer = new byte[2048];
            var previous = new byte[0];
            var current = new byte[0];
            var info = new IptvInfo { Url = url };

            try
            {
                _log.Debug($"Try to read data from {url} for {channel}");
                using (var s = _socketFactory())
                {
                    s.Open(url);
                    for (var count = 0; count < MaxCycles; count++)
                    {
                        var length = s.Receive(buffer);
                        current = buffer.Take(length).ToArray();
                        UpdateInfo(info, previous, current);
                        if (info.Complete())
                        {
                            info.KBps = s.KBps;
                            info.MBps = s.MBps;
                            _log.Debug($"Found IPTV Info after {count} blocks");
                            _log.Info($"Found IPTV Info for {channel} at {url}: {info.Provider} - {info.Name} - {info.KBps} KB/s (key: {info.Number})");
                            break;
                        }
                        previous = current;
                    }
                }
                return info;
            }
            catch(Exception ex)
            {
                _log.Info($"Failed to read data from {url} for {channel}");
                _log.Debug($"Failed to read data from {url}: {ex.Message}");
                return null;
            }
        }

        private void UpdateInfo(IptvInfo info, byte[] previous, byte[] current)
        {
            var data = new byte[previous.Length + current.Length];
            previous.CopyTo(data, 0);
            current.CopyTo(data, previous.Length);

            if(FindChannelInfo(data, out string provider, out string name))
            {
                info.Provider = provider;
                info.Name = name;
            }

            if(!info.Number.HasValue && FindChannelNumber(data, out int channel))
            {
                info.Number = channel;
            }
        }

        bool FindChannelNumber(byte[] data, out int channel)
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
                _log.Debug("Found VMECM but the message was truncated");
                return false;
            }
            channel = (data[index - 1] << 8) + data[index];
            _log.Debug($"Found channel {channel} in the bytes");
            return true;
        }

        bool FindChannelInfo(byte[] data, out string provider, out string name)
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

        int SearchBytes(byte[] haystack, byte[] needle, int start_index)
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
        int ReadStringFromData(byte[] data, int index, out string message)
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
    }
}
