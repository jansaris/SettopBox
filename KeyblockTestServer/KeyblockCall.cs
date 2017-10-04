using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Keyblock;
using log4net;

namespace KeyblockTestServer
{
    public class KeyblockCall
    {
        private static readonly object SyncRoot = new object();
        private static readonly ILog Logger = LogManager.GetLogger(typeof(KeyblockCall));
        private readonly byte[] _sessionKey;

        public KeyblockCall()
        {
            _sessionKey = File.ReadAllBytes(GetPath("GetSessionKey.response"))
                .Skip(4)
                .Take(16)
                .ToArray();
        }
   
        private int GetCallCount()
        {
            lock (SyncRoot)
            {
                var file = Path.Combine(Program.CommunicationLogFolder, "call.count");
                if (!File.Exists(file))
                {
                    File.WriteAllText(file, "1");
                    return 1;
                }
                var count = Convert.ToInt32(File.ReadAllText(file));
                count++;
                File.WriteAllText(file, count.ToString());
                return count;
            }
        }

        public bool Handle(Stream stream, bool rc4Encrypted)
        {
            try
            {
                Logger.Debug("Waiting for client message...");
                var data = Read(stream);
                var message = Encoding.ASCII.GetString(data);
                if (rc4Encrypted)
                {
                    message = Decrypt(message, data);
                }
                Logger.Info($"Received {data.Length} bytes: {message}");
                var response = GetResponseMessage(message);

                var call = GetCallCount();

                //Save communication for analysis
                File.WriteAllBytes(Path.Combine(Program.CommunicationLogFolder, $"{call}_{response.Item1}.request"), data);
                File.WriteAllBytes(Path.Combine(Program.CommunicationLogFolder, $"{call}_{response.Item1}.response"), response.Item2);

                // Write a message to the client.
                Logger.Info($"Sending message: {response.Item2.Length} bytes");
                stream.Write(response.Item2, 0, response.Item2.Length);
                stream.Flush();
                stream.Close();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Something went wrong: {ex.Message}");
                Logger.Error(ex.StackTrace);
                return false;
            }
            finally
            {
                stream.Dispose();
            }
        }

        private Tuple<string, byte[]> GetResponseMessage(string message)
        {
            if (message.Contains("CreateSessionKey"))
            {
                Logger.Info("Generate 'CreateSessionKey' response");
                var data = GenerateCreateSessionKeyResponse(File.ReadAllBytes(GetPath("GetSessionKey.response")));
                return new Tuple<string, byte[]>("CreateSessionKey", data);
            }
            if (message.Contains("getCertificate"))
            {
                Logger.Info("Generate 'getCertificate' response");
                var data = GenerateCertificateResponse(message);
                return new Tuple<string, byte[]>("getCertificate", data);
            }
            if (message.Contains("SaveEncryptedPassword"))
            {
                Logger.Info("Generate 'SaveEncryptedPassword' response");
                var data = File.ReadAllBytes(GetPath(@"SaveEncryptedPassword.response"));
                return new Tuple<string, byte[]>("SaveEncryptedPassword", data);
            }
            if (message.Contains("GetVKSConnectionInfo"))
            {
                Logger.Info("Generate 'GetVKSConnectionInfo' response");
                var data = File.ReadAllBytes(GetPath(@"GetVKSConnectionInfo.response"));
                return new Tuple<string, byte[]>("GetVKSConnectionInfo", data);
            }
            if (message.Contains("GetAllChannelKeys"))
            {
                Logger.Info("Generate 'GetAllChannelKeys' response");
                var data = File.ReadAllBytes(GetPath(@"GetAllChannelKeys.response"));
                return new Tuple<string, byte[]>("GetAllChannelKeys", data);
            }

            Logger.Info("Generate 'Unkown call' response");
            return new Tuple<string, byte[]>("Unkown call", Encoding.ASCII.GetBytes("Unknown message"));
        }

        private string Decrypt(string message, byte[] data)
        {
            var encrytedIndex = message.IndexOf(Program.MacAddress, StringComparison.Ordinal);
            if (encrytedIndex < 0) return string.Empty;
            var unEncryptedMessage = message.Substring(0, encrytedIndex + Program.MacAddress.Length + 1);
            var unEncryptedBytes = Encoding.ASCII.GetBytes(unEncryptedMessage);
            var encryptedBytes = data.Skip(unEncryptedBytes.Length).ToArray();
            var decrypted = RC4.Decrypt(_sessionKey, encryptedBytes);
            var decryptedMessage = Encoding.ASCII.GetString(decrypted);
            return unEncryptedMessage + decryptedMessage;
        }

        private byte[] GenerateCreateSessionKeyResponse(byte[] originalMessage)
        {
            var data = new List<byte>();
            data.AddRange(originalMessage.Take(20));
            data.AddRange(Encoding.ASCII.GetBytes($"{DateTime.Now:dd/MM/yyyy HH:mm:ss}"));
            data.AddRange(originalMessage.Skip(39));
            return data.ToArray();
        }

        private byte[] GenerateCertificateResponse(string clientMessage)
        {
            var data = new List<byte>();
            data.AddRange(GenerateCertificate(clientMessage));
            var messageLength = data.Count + 12;
            //There are 3 numbers calculated in 12 bytes
            //Third 4 bytes: lenght of certificate
            data.InsertRange(0, BitConverter.GetBytes(data.Count).ToArray());
            //Second 4 bytes: length of total message - 8 (reverted)
            data.InsertRange(0, BitConverter.GetBytes(messageLength - 8).Reverse().ToArray());
            //First 4 bytes: length of total message (reverted)
            data.InsertRange(0, BitConverter.GetBytes(messageLength).Reverse().ToArray());

            File.WriteAllBytes(Path.Combine(Program.CommunicationLogFolder, "ClientCertificate.crt"), data.Skip(12).ToArray());
            File.WriteAllBytes(Path.Combine(Program.CommunicationLogFolder, "ClientCertificate.Response"), data.ToArray());
            return data.ToArray();
        }


        private byte[] GenerateCertificate(string message)
        {
            var pemStart = message.IndexOf("-----", StringComparison.Ordinal);
            var pemEnd = message.LastIndexOf("-----", StringComparison.Ordinal) + 5;
            var pem = message.Substring(pemStart, pemEnd - pemStart);
            File.WriteAllText(Path.Combine(Program.CommunicationLogFolder, "ClientCertificate.csr"), pem);

            return new CertGenerator().SignWithOpenSsl(pem);
        }

        private string GetPath(string name)
        {
            return Path.Combine(Program.CommunicationsFolder, name);
        }

        public static byte[] Read(Stream stream)
        {
            // Read the  message sent by the server.
            Logger.Debug("Start reading bytes from the client");
            var buffer = new byte[2048];
            var receivedBytes = new List<byte>();
            do
            {
                stream.ReadTimeout = 1000;
                var bytes = stream.Read(buffer, 0, buffer.Length);
                receivedBytes.AddRange(buffer.Take(bytes));
            } while (receivedBytes.Count < 20);
            return receivedBytes.ToArray();
        }
    }
}