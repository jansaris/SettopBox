using System;
using System.Linq;
using System.Net.Sockets;
using log4net;

namespace Test.NewCamdClient
{
    public class NewCamdClient
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(NewCamdClient));
        TcpClient _client;
        NetworkStream _stream;
        byte[] _buffer;
        byte[] _loginKey = new byte[14];

        public void Connect(string host, int port)
        {
            Disconnect();
            Logger.Info($"Connect to {host}:{port}");
            _client = new TcpClient();
            _client.Connect(host, port);
            _stream = _client.GetStream();
            _buffer = new byte[_client.ReceiveBufferSize];

            ReadLoginKey();
        }

        public void Disconnect()
        {
            if (_client == null)
            {
                return;
            }

            if (!_client.Connected)
            {
                Logger.Info("Client is not connected");
                _client = null;
                return;
            }

            Logger.Info("Close client stream");
            _stream.Close();
            _stream.Dispose();
            _stream = null;

            Logger.Info("Close client");
            _client.Close();
            _client.Dispose();
            _client = null;
        }

        public void Login()
        {
            throw new System.NotImplementedException();
        }

        public void GetKey()
        {
            throw new System.NotImplementedException();
        }

        void ReadLoginKey()
        {
            Read("the login key", 14);
            _loginKey = _buffer.Take(14).ToArray();
        }

        int Read(string message, int expectedBytes = -1, int timeout = 3000)
        {
            Logger.Info($"Wait for '{message}' from the server");
            _stream.ReadTimeout = timeout;
            var bytes = _stream.Read(_buffer, 0, _buffer.Length);
            Logger.Info($"Received {bytes} from the server");
            if (expectedBytes > -1 && bytes != expectedBytes)
            {
                Logger.Error($"Received wrong amount of bytes (expected: {expectedBytes}): {_buffer}");
            }
            else
            {
                Logger.Info($"Received '{message}' from the server");
            }
            return bytes;
        }
    }
}