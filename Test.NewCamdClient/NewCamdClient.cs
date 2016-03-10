using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using log4net;
using NewCamd;

namespace Test.NewCamdClient
{
    //https://3color.googlecode.com/svn/trunk/cardservproxy/etc/protocol.txt
    public class NewCamdClient
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(NewCamdClient));
        readonly TripleDes _tripleDes;
        TcpClient _client;
        NetworkStream _stream;

        public NewCamdClient()
        {
            _tripleDes = new TripleDes();
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
            /*
                Client -> Server 1/5 - 090f - Thu Jan  8 17:20:18 CET 2004
                encryption: login
                ----------------------------------------------------------
                00: e0 00 29 64 75 6d 6d 79 00 24 31 24 61 62 63 64     )dummy $1$abcd
                10: 65 66 67 68 24 6e 70 53 45 54 51 73 72 49 6d 33   efgh$npSETQsrIm3
                20: 35 4d 51 66 69 55 49 41 64 6e 2e 00               5MQfiUIAdn. 

                Next the client has to send a packet with cmd = MSG_CLIENT_2_SERVER_LOGIN
                including username and password in the data field.
                The username is sent as a C-String (NULL terminated), the password
                follows directly after the zero termination byte of the username. The
                password has to be put through the glibc crypt() function, using salt
                $1$abcdefgh$. The password in the data field has to be NULL terminated and the
                packet encrypted with the login key.
            */
            var msg = new List<byte>();

            msg.Add((byte)NewCamdMessage.MsgClient2ServerLogin);
            msg.AddRange(Encoding.ASCII.GetBytes(UserName));
            msg.Add(0);
            msg.AddRange(Encoding.ASCII.GetBytes(_tripleDes.Encrypt(Password, "$1$abcdefgh$")));
            msg.Add(0);
            var bytes = msg.ToArray();
            _stream.Write(bytes,0,bytes.Length);
            Read("Login awnser", 1);
            var response = (NewCamdMessage) _buffer[0];
            Logger.Info("Received: " + response);
        }

        public void GetKey()
        {
            throw new System.NotImplementedException();
        }

        void ReadLoginKey()
        {
            /*
                Client <- Server 1/5 - 090f - Thu Jan  8 17:20:17 CET 2004
                encryption: none
                ----------------------------------------------------------
                00: 77 9d cc 5d d2 0d 59 2e dc ed b8 17 c1 ab         w  ]  Y.      

                After opening a TCP connection to the server, the client first receives 14
                random bytes. These bytes are to be XORed to the Triple-DES key from the config
                file. (newcamd: CWS = ..., cardserver: DESKEY = ...). The result forms the
                Triple DES key to be used to send Username and Password to the cardserver, I
                call it the login key.
            */
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
                Logger.Error($"Received wrong amount of bytes (expected: {expectedBytes})");
            }
            else
            {
                Logger.Info($"Received '{message}' from the server");
            }
            return bytes;
        }
    }
}