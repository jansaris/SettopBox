using System;
using System.Net;
using System.Net.Sockets;

namespace ChannelList
{
    public class IptvSocket : IDisposable
    {
        private readonly UdpClient _client;
        private IPEndPoint _localEndPoint;
        private IPAddress _multicastaddress;
        

        public IptvSocket()
        {
            _client = new UdpClient {Client = {ReceiveTimeout = 5000}};
        }

        internal void Open(string host, int port)
        {
            _client.ExclusiveAddressUse = false;
            _localEndPoint = new IPEndPoint(IPAddress.Any, port);

            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _client.ExclusiveAddressUse = false;

            _client.Client.Bind(_localEndPoint);

            _multicastaddress = IPAddress.Parse(host);
            _client.JoinMulticastGroup(_multicastaddress);
        }

        public void Dispose()
        {
            _client.DropMulticastGroup(_multicastaddress);
            _client.Close();
        }

        public byte[] Receive()
        {
            return _client.Receive(ref _localEndPoint);
        }
    }
}
