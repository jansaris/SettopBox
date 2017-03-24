using log4net;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace WebUi.api.Iptv
{
    public class IptvSocket : IDisposable
    {
        readonly ILog _log;
        string _url;
        readonly Socket _socket;
        readonly Stopwatch _stopwatch;
        long _receivedBytes;
        private double? _kbs;
        private double? _mbs;

        public int KBps {
            get {
                if (!_kbs.HasValue) CalculateBandwith();
                return (int)Math.Round(_kbs.Value, 0);
            }
        }
        public int MBps
        {
            get
            {
                if (!_mbs.HasValue) CalculateBandwith();
                return (int)Math.Round(_mbs.Value, 0);
            }
        }

        public IptvSocket(ILog log)
        {
            _log = log;
            _stopwatch = new Stopwatch();
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                ReceiveTimeout = 100
            };
        }

        internal void Open(string url)
        {
            _url = url;
            OpenIptvStream(url);
            _stopwatch.Start();
        }

        internal int Receive(byte[] buffer)
        {
            var length = _socket.Receive(buffer);
            _receivedBytes += length;
            return length;
        }

        public void Dispose()
        {
            _stopwatch?.Stop();
            _socket?.Close();
            _socket?.Dispose();
        }

        void CalculateBandwith()
        {
            double factor = ((double)1000) / _stopwatch.ElapsedMilliseconds;
            var aSecondBytes = _receivedBytes * factor;
            _kbs = aSecondBytes / 1024 * 8;
            _mbs = _kbs / 1024;
            _log.Debug($"Calculated kbs for '{_url}': {_kbs:0.####}");
        }

        void OpenIptvStream(string url)
        {
            var port = ExtractPort(url);
            var strippedUrl = StripUrl(url);

            var ipep = new IPEndPoint(IPAddress.Any, port);
            var ip = IPAddress.Parse(strippedUrl);

            _socket.Bind(ipep);
            _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(ip, IPAddress.Any));
        }

        int ExtractPort(string url)
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

        string StripUrl(string url)
        {
            return StripPort(StripProtocol(url));
        }

        string StripPort(string url)
        {
            var index = url.IndexOf(":");
            if (index > 0)
            {
                return url.Substring(0, index);
            }
            _log.Warn($"Failed to strip port from {url}");
            return url;
        }

        string StripProtocol(string url)
        {
            var index = url.IndexOf("://");
            if (index > 0)
            {
                index += 3;
                return url.Substring(index, url.Length - index);
            }
            _log.Warn($"Failed to strip protocol from {url}");
            return url;
        }
    }
}
