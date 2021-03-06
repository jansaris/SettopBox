﻿using log4net;
using System;
using System.Net.Sockets;
using System.Text;
using SharedComponents.Iptv;
using SharedComponents.Models;

namespace ChannelList { 

    public class IptvChannel
    {
        private readonly ILog _log;
        private readonly Func<IptvSocket> _socketFactory;
        public bool OnlySearchForKeys = false;
        private const int MaxCycles = 2048;

        public IptvChannel(ILog logger, Func<IptvSocket> socketFactory)
        {
            _log = logger;
            _socketFactory = socketFactory;
        }

        public IptvInfo ReadInfo(ChannelLocation info, string channel)
        {
            if (string.IsNullOrWhiteSpace(info.Host))
            {
                _log.Warn("No valid host given");
                return null;
            }
            var data = ReadData(info, channel);
            return data;
        }

        private IptvInfo ReadData(ChannelLocation channelLocation, string channel)
        {
            var info = new IptvInfo { Url = channelLocation.Url };
            
            try
            {
                _log.Debug($"Try to read data from {channelLocation.Url} for {channel}");
                
                ReadDataFromSocket(channelLocation, info);

                _log.Info($"Found IPTV Info for {channel} at {channelLocation.Url}: {info.Provider} - {info.Name} (key: {info.Number})");

                return info;
            }
            catch(Exception ex)
            {
                _log.Warn($"Failed to read data from {channelLocation.Url} for {channel}");
                _log.Debug($"Failed to read data from {channelLocation.Url}: {ex.Message}", ex);
                return null;
            }
        }

        private void ReadDataFromSocket(ChannelLocation channelLocation, IptvInfo info, int retries = 3)
        {
            try
            {
                using (var s = _socketFactory())
                {
                    var start = DateTime.Now;
                    var previous = new byte[0];
                    s.Open(channelLocation.Host, channelLocation.Port);
                    var count = 0;
                    while (NotAllDataFound(info) && count < MaxCycles)
                    {
                        var data = s.Receive();
                        UpdateInfo(info, previous, data);
                        previous = data;
                        count++;
                    }
                    var end = DateTime.Now;
                    _log.Debug($"Found IPTV Info after {(int) (end - start).TotalMilliseconds}ms");
                }
            }
            catch (SocketException)
            {
                if (retries == 0)
                {
                    throw;
                }
                _log.Info($"Failed to read data from {channelLocation.Url}, retry ({retries}x allowed)");
                ReadDataFromSocket(channelLocation, info, retries - 1);
            }
        }

        private bool NotAllDataFound(IptvInfo info)
        {
            if (OnlySearchForKeys && info.Number.HasValue) return false;
            return !info.Complete();
        }

        private void UpdateInfo(IptvInfo info, byte[] previous, byte[] current)
        {
            var data = new byte[previous.Length + current.Length];
            previous.CopyTo(data, 0);
            current.CopyTo(data, previous.Length);

            if (FindChannelInfo(data, out string provider, out string name))
            {
                info.Provider = provider;
                info.Name = name;
            }

            if(!info.Number.HasValue && FindChannelNumber(data, out int channel))
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
                _log.Debug("Found VMECM but the message was truncated");
                return false;
            }
            channel = (data[index - 1] << 8) + data[index];
            _log.Debug($"Found channel {channel} in the bytes");
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

        private int SearchBytes(byte[] haystack, byte[] needle, int startIndex)
        {
            int len = needle.Length;
            int limit = haystack.Length - len;
            for (int i = startIndex; i <= limit; i++)
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
    }
}
