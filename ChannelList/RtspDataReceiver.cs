using System.IO;
using System.Net.Sockets;
using System.Text;

namespace ChannelList
{
    public class RtspDataReceiver
    {
        public byte[] ReadDataFromServer(string host, int port)
        {
            var message = Encoding.ASCII.GetBytes($"DESCRIBE rtsp://{host}/vqe-channels/ RTSP/1.0\r\nCSeq: 1\r\nAccept: application/sdp\r\n\r\n");

            var client = new TcpClient(host, port);
            using (var stream = client.GetStream())
            {
                stream.Write(message, 0, message.Length);
                var data = new byte[1024];
                using (var ms = new MemoryStream())
                {

                    int numBytesRead;
                    while ((numBytesRead = stream.Read(data, 0, data.Length)) > 0)
                    {
                        ms.Write(data, 0, numBytesRead);
                    }
                    return ms.ToArray();
                }
            }
        }
    }
}
