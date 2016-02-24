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
    public class SslTcpClient : IDisposable
    {
        private static Hashtable certificateErrors = new Hashtable();
        private bool _clientOpen;
        private TcpClient _client;
        private SslStream _sslStream;
        private string _activeServer;
        private int _activePort;

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

        private void Open(string server, int port)
        {
            if (_clientOpen && ServerMatch(server, port)) return;
            if(_clientOpen && !ServerMatch(server,port)) Close();
            // Create a TCP/IP client socket.
            // machineName is the host running the server application.
            _client = new TcpClient(server, port);
            Console.WriteLine("Client connected.");
            // Create an SSL stream that will close the client's stream.
            _sslStream = new SslStream(_client.GetStream(), true, ValidateServerCertificate, null);
            
            // The server name must match the name on the server certificate.
            try
            {
                _sslStream.AuthenticateAsClient(server);

                var cert = _sslStream.RemoteCertificate;
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
                _client.Close();
                _clientOpen = false;
            }
            _clientOpen = true;
            _activeServer = server;
            _activePort = port;
        }

        private bool ServerMatch(string server, int port)
        {
            return _activeServer == server && _activePort == port;
        }

        public void Close()
        {
            if (!_clientOpen) return;
            try
            {
                _clientOpen = false;
                _activePort = -1;
                _activeServer = string.Empty;
                _sslStream.Close();
                _client.Close();
                Console.WriteLine("Client closed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
        }

        public byte[] Send(string msg, string server, int port)
        {
            Open(server, port);
            // Encode a test message into a byte array.
            // Signal the end of the message using the "<EOF>".
            byte[] messsage = Encoding.ASCII.GetBytes(msg);
            // Send hello message to the server. 
            _sslStream.Write(messsage);
            _sslStream.Flush();
            // Read message from the server.
            var serverMessage = ReadMessage();
            Close();

            return serverMessage;
        }

        public void Dispose()
        {
            Close();
        }
    
        
        byte[] ReadMessage()
        {
            // Read the  message sent by the server.
            // The end of the message is signaled using the
            // "<EOF>" marker.
            var buffer = new byte[2048];
            var receivedBytes = new List<byte>();
            int bytes;
            do
            {
                bytes = _sslStream.Read(buffer, 0, buffer.Length);
                receivedBytes.AddRange(buffer.Take(bytes));
            } while (bytes != 0);

            return receivedBytes.ToArray();
        }
    }
}