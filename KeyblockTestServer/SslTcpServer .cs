using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using log4net;

namespace KeyblockTestServer
{
    public class SslTcpServer 
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SslTcpServer));
        private readonly X509Certificate _serverCertificate;

        public SslTcpServer(string certificate, string password)
        {
            _serverCertificate = new X509Certificate2(certificate, password);
        }

        public bool ProcessClient(TcpClient client)
        {
            Logger.Debug("Handle a client...");
            // A client has connected. Create the 
            // SslStream using the client's network stream.
            SslStream sslStream = new SslStream(client.GetStream(), false);
            // Authenticate the server but don't require the client to authenticate.
            try
            {
                var vcas = new KeyblockCall();
                sslStream.AuthenticateAsServer(_serverCertificate, false, SslProtocols.Tls, false);
                // Read a message from the client. 
                return vcas.Handle(sslStream, false);
            }
            catch (Exception e)
            {
                Logger.Info($"Exception: {e.Message}");
                if (e.InnerException != null)
                {
                    Logger.Info($"Inner exception: {e.InnerException.Message}");
                }
                Logger.Info("Authentication failed - closing the connection.");
                sslStream.Close();
                client.Close();
            }
            finally
            {
                // The client stream will be closed with the sslStream
                // because we specified this behavior when creating
                // the sslStream.
                sslStream.Close();
                client.Close();
            }
            return false;
        }
    }
}