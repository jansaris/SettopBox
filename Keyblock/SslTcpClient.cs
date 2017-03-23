using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using log4net;

namespace Keyblock
{
    public class SslTcpClient
    {
        readonly ILog _logger;
        readonly Settings _settings;
        string Communicationfolder => Path.Combine(_settings.DataFolder, _settings.CommunicationFolder);

        public SslTcpClient(Settings settings, ILog logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public byte[] SendAndReceive(string msg, string server, int port, bool useSsl = true, [CallerMemberName] string caller = null)
        {
            _logger.Debug("Convert ASCII message into bytes");
            // Encode a test message into a byte array.
            var messsage = Encoding.ASCII.GetBytes(msg);

            // ReSharper disable once ExplicitCallerInfoArgument
            // Just redirecting, we want the original caller
            return SendAndReceive(messsage, server, port, useSsl, caller);
        }

        public byte[] SendAndReceive(byte[] msg, string server, int port, bool useSsl = true, [CallerMemberName] string caller = null)
        {
            byte[] response = null;
            try
            {
                response = _settings.DontUseRealServerButMessagesFromDisk ? 
                    CommunicateWithDisk(caller) : 
                    CommunicateWithServer(msg, server, port, useSsl, caller);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed during send and receive", ex);
            }
            return response;
        }

        byte[] CommunicateWithDisk(string caller)
        {
            var path = ResponsePath(caller);
            _logger.Warn($"Read response from disk: {path}");
            if(!File.Exists(path)) throw new FileNotFoundException($"Failed to find communication response file: {path}");
            var response = File.ReadAllBytes(path);
            _logger.Warn($"Received {response.Length} bytes from disk");
            return response;
        }

        byte[] CommunicateWithServer(byte[] msg, string server, int port, bool useSsl, string caller)
        {
            _logger.Info($"Send message to {server}:{port}");
            byte[] response;
            WriteToDisk(RequestPath(caller), msg);
            using (var stream = OpenPort(server, port, useSsl))
            {
                Send(stream, msg);
                response = Read(stream);
            }
            WriteToDisk(ResponsePath(caller), response);
            _logger.Info($"Received {response.Length} bytes from the server");
            return response;
        }

        void WriteToDisk(string path, byte[] msg)
        {
            if (!_settings.WriteAllCommunicationToDisk)
            {
                _logger.Debug("WriteAllCommunicationToDisk is disabled in ini file");    
            }
            _settings.EnsureDataFolderExists(Communicationfolder);
            _logger.Debug($"Write {msg.Length} to disk '{path}'");
            File.WriteAllBytes(path, msg);
        }

        Stream OpenPort(string server, int port, bool useSsl)
        {
            //Open a tcp connection with the server
            _logger.Debug($"Open a TCP connection with {server}:{port}");
            var client = new TcpClient(server, port);
            // Create an SSL stream that will close the client's stream.
            Stream stream;
            if (useSsl)
            {
                _logger.Debug("Wrap the TCP connection in a SSL stream");
                /*
                 * TODO: Fix to support 1024 bit
                 I've managed to set the minimum key length for schannel using windows registry.
[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\KeyExchangeAlgorithms\Diffie-Hellman]
"ClientMinKeyBitLength"=dword:00000200
But it's not the desired solution, because it changes the setting for the whole system and I would like to set it temporarily and only in the scope of my application.

                https://msdn.microsoft.com/fr-fr/library/cc783349(v=ws.10).aspx
                 */
                /*
                   Latest version of Mono is also giving problems
                   https://bugzilla.xamarin.com/show_bug.cgi?id=44963
                   Add command: export MONO_TLS_PROVIDER="legacy"
                   Before starting the application on linux. This will allow mono to generate TLS SSL stream
                */
                var sslStream = new SslStream(client.GetStream(), true, ValidateServerCertificate, null);
                sslStream.AuthenticateAsClient(server, null, SslProtocols.Default, false);
                stream = sslStream;
            }
            else
            {
                _logger.Debug("Use TCP connection stream");
                stream = client.GetStream();
            }
            _logger.Debug($"Succesfully connected to {server}:{port}");
            return stream;
        }

        static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            //We already trust the server, so ignore this part
            return true;
        }

        byte[] Read(Stream stream)
        {
            // Read the  message sent by the server.
            _logger.Debug("Start reading bytes from the server");
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

        void Send(Stream stream, byte[] message)
        {
            //Send the bytes to the server
            _logger.Debug($"Write {message.Length} bytes to the server");
            stream.Write(message, 0, message.Length);
            stream.Flush();
        }

        string ResponsePath(string caller)
        {
            return Path.Combine(Communicationfolder, $"{caller}.response");
        }

        string RequestPath(string caller)
        {
            return Path.Combine(Communicationfolder, $"{caller}.request");
        }
    }
}