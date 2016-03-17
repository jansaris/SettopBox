using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using log4net;
using NewCamd;
using NewCamd.Encryption;

namespace Test.NewCamdClient
{
    //https://3color.googlecode.com/svn/trunk/cardservproxy/etc/protocol.txt
    public class NewCamdClient
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(NewCamdClient));
        readonly EncryptionHelpers _encryptionHelpers;
        TcpClient _client;
        NetworkStream _stream;

        public NewCamdClient()
        {
            _encryptionHelpers = new EncryptionHelpers();
        }

        byte[] _buffer;
        byte[] _loginKey = new byte[14];

        public string UserName { get; set; } = "user";
        public string Password { get; set; } = "pass";
        public string DesKey { get; set; } = "0102030405060708091011121314";

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
            var val = SendAndValidate("rencrypted8.dat", "sencryptedForSend17.dat");
            if(val) Logger.Info("Login was successfull");
            else Logger.Warn("Login failed");
        }

        bool SendAndValidate(string fileToSend, string fileToReceive)
        {
            var send = ReadFile(fileToSend);
            var expected = ReadFile(fileToReceive);

            _stream.Write(new byte[] { 0 , (byte)send.Length, 0 }, 0, 2);
            _stream.Write(send, 0, send.Length);
            var data = Read("Login", expected.Length);
            return CompareArrays(data, expected);
        }

        bool CompareArrays(byte[] array1, byte[] array2)
        {
            for (var i = 0; i < array1.Length; i++)
            {
                if (array2.Length <= i)
                {
                    Logger.Warn($"Array2 is shorter than array1, compare stops at {i}");
                    return false;
                }
                if (array1[i] != array2[i])
                {
                    Logger.Warn($"First mismatch at byte {i}");
                    return false;
                }
            }
            Logger.Info("Arrays are equal");
            return true;
        }

        byte[] ReadFile(string file)
        {
            var path = Path.Combine("./testfiles", file);
            return File.ReadAllBytes(path);
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

        byte[] Read(string message, int expectedBytes = -1, int timeout = 3000)
        {
            Logger.Info($"Wait for '{message}' from the server");
            _stream.ReadTimeout = timeout;
            var bytes = _stream.Read(_buffer, 0, _buffer.Length);
            Logger.Info($"Received {bytes} from the server");
            if (expectedBytes > -1 && bytes != expectedBytes)
            {
                Logger.Error($"Received wrong amount of bytes (expected: {expectedBytes})");
            }
            else
            {
                Logger.Info($"Received '{message}' from the server");
            }
            return _buffer.Take(bytes).ToArray();
        }
    }
}