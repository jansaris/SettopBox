using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using log4net;

namespace Keyblock
{
    public class SslTcpClient
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SslTcpClient));

        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            //We already trust the server, so ignore this part
            return true;
        }

        private Stream OpenPort(string server, int port, bool useSsl)
        {
            //Open a tcp connection with the server
            Logger.Debug($"Open a TCP connection with {server}:{port}");
            var client = new TcpClient(server, port);
            // Create an SSL stream that will close the client's stream.
            Stream stream;
            if (useSsl)
            {
                Logger.Debug("Wrap the TCP connection in a SSL stream");
                var sslStream = new SslStream(client.GetStream(), true, ValidateServerCertificate, null);
                sslStream.AuthenticateAsClient(server);
                stream = sslStream;
            }
            else
            {
                Logger.Debug("Use TCP connection stream");
                stream = client.GetStream();
            }
            Logger.Debug($"Succesfully connected to {server}:{port}");
            return stream;
        }

        public byte[] SendAndReceive(string msg, string server, int port, bool useSsl = true)
        {
            Logger.Debug("Convert ASCII message into bytes");
            // Encode a test message into a byte array.
            var messsage = Encoding.ASCII.GetBytes(msg);
            return SendAndReceive(messsage,server,port,useSsl);
        }

        public byte[] SendAndReceive(byte[] msg, string server, int port, bool useSsl = true)
        {
            byte[] response = null;
            try
            {
                Logger.Info($"Send message to {server}:{port}");
                using (var stream = OpenPort(server, port, useSsl))
                {
                    Send(stream, msg);
                    response = Read(stream);
                }
                Logger.Info($"Received {response.Length} bytes from the server");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed during send and receive", ex);
            }
            return response;
        }

        private static byte[] Read(Stream stream)
        {
            // Read the  message sent by the server.
            Logger.Debug("Start reading bytes from the server");
            var buffer = new byte[2048];
            var receivedBytes = new List<byte>();
            int bytes;
            do
            {
                bytes = stream.Read(buffer, 0, buffer.Length);
                receivedBytes.AddRange(buffer.Take(bytes));
            } while (bytes != 0);
            return receivedBytes.ToArray();
        }

        private static void Send(Stream stream, byte[] message)
        {
            //Send the bytes to the server
            Logger.Debug($"Write {message.Length} bytes to the server");
            stream.Write(message, 0, message.Length);
            stream.Flush();
        }
    }
}