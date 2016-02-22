using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Keyblock
{
    public class SslTcpClient
    {
        private static Hashtable certificateErrors = new Hashtable();

        // The following method is invoked by the RemoteCertificateValidationDelegate.
        public bool ValidateServerCertificate(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

            // Do not allow this client to communicate with unauthenticated servers.
            return true;
        }

        public byte[] ssl_client_send(string msg, string server, int port)
        {
            // Create a TCP/IP client socket.
            // machineName is the host running the server application.
            TcpClient client = new TcpClient(server, port);
            Console.WriteLine("Client connected.");
            // Create an SSL stream that will close the client's stream.
            using (SslStream sslStream = new SslStream(client.GetStream(), true, ValidateServerCertificate, null))
            {
                // The server name must match the name on the server certificate.
                try
                {
                    sslStream.AuthenticateAsClient(server);

                    var cert = sslStream.RemoteCertificate;
                    Console.WriteLine($"Got certificate from server {cert.Issuer} with subject {cert.Subject}");
                }
                catch (AuthenticationException e)
                {
                    Console.WriteLine("Exception: {0}", e.Message);
                    if (e.InnerException != null)
                    {
                        Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                    }
                    Console.WriteLine("Authentication failed - closing the connection.");
                    client.Close();
                    return null;
                }
                // Encode a test message into a byte array.
                // Signal the end of the message using the "<EOF>".
                byte[] messsage = Encoding.ASCII.GetBytes(msg);
                // Send hello message to the server. 
                sslStream.Write(messsage);
                sslStream.Flush();
                // Read message from the server.
                var serverMessage = ReadMessage(sslStream);

                // Close the client connection.
                client.Close();
                Console.WriteLine("Client closed.");
                return serverMessage;
            }
        }
        
        byte[] ReadMessage(SslStream sslStream)
        {
            // Read the  message sent by the server.
            // The end of the message is signaled using the
            // "<EOF>" marker.
            var buffer = new byte[2048];
            var receivedBytes = new List<byte>();
            int bytes;
            do
            {
                bytes = sslStream.Read(buffer, 0, buffer.Length);
                receivedBytes.AddRange(buffer.Take(bytes));

                //// Use Decoder class to convert from bytes to UTF8
                //// in case a character spans two buffers.
                //Decoder decoder = Encoding.ASCII.GetDecoder();
                //char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                //decoder.GetChars(buffer, 0, bytes, chars, 0);
                //messageData.Append(chars);
                // Check for EOF.
                //if (messageData.ToString().IndexOf("<EOF>") != -1)
                //{
                //    break;
                //}
            } while (bytes != 0);

            return receivedBytes.ToArray();
        }
    }
}