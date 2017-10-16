using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace ChannelList
{
    public class RtspDataReceiver
    {
        public byte[] ReadDataFromServer(string host, int port)
        {
            var sock = new Socket(SocketType.Stream, ProtocolType.Tcp) {ReceiveTimeout = 500};
            sock.Connect(host, port);
            sock.Send(Encoding.ASCII.GetBytes($"DESCRIBE rtsp://{host}/vqe-channels/ RTSP/1.0\r\nCSeq: 1\r\nAccept: application/sdp\r\n\r\n"));
            var buffer = new byte[1024];
            var data = new List<byte>();
            while (true)
            {
                try
                {
                    var receivedData = sock.Receive(buffer);
                    if (receivedData > 0) data.AddRange(buffer.Take(receivedData));
                    if (receivedData == 0) break;
                }
                catch (SocketException)
                {
                    //We got no data for 500ms, so we are at the end of the stream
                    break;
                }
                
            }

            return data.ToArray();
        }
    }
}
