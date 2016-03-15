using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using log4net;
using NewCamd.Encryption;

namespace NewCamd
{
    public class Decrypt
    {
        byte[] _randomKey;
        byte[] _ivec;
        byte[] _keyblock;
        byte[] _unencrypted;
        byte[] _encrypted;
        byte[] _key1;
        byte[] _key2;

        readonly ILog _logger = LogManager.GetLogger("Decrypt");
        const string Name = "Decryptor";
        readonly Random _random = new Random();

        readonly EncryptionHelpers _crypto = new EncryptionHelpers();
        NewCamdClient _client;

        public void Run(Settings getInstance)
        {
            _client = new NewCamdClient(LogManager.GetLogger(typeof(Decrypt)), getInstance, _crypto);

            //ReadData();
            var mes = TestFiles("random3.dat", "encrypted8.dat");
            TestEncrypt(mes);
            //_keyblock = _crypto.CreateKeySpread(_random);
            //var client = new NewCamdClient(LogManager.GetLogger(typeof(Decrypt)),null,_crypto);
            //client._keyblock = _keyblock;
            //var mes = client.ParseMessage(_encrypted);
            //Validate(Run1());
            //Validate(Run2());
            //Validate(Run3());
            Console.ReadKey();
        }

        NewCamdMessage TestFiles(string key, string encrypted)
        {
            var random = File.ReadAllBytes(GetPath(key));
            var enc = File.ReadAllBytes(GetPath(encrypted));
            
            var generated = _client.InitializeKeys();
            //_client._keyblock = _crypto.CreateKeySpread(random);
            var mes = _client.ParseMessage(enc);
            Console.WriteLine(mes.Type);
            return mes;
        }

        void TestEncrypt(NewCamdMessage mes)
        {
            var random = File.ReadAllBytes(GetPath("random3.dat"));
            var padding = File.ReadAllBytes(GetPath("padding13.dat"));
            var ivec = File.ReadAllBytes(GetPath("ivecToSend15.dat"));

            mes.Type = NewCamdMessageType.MsgClient2ServerLoginAck;
            mes.Data = File.ReadAllBytes(GetPath("toSend11.dat"));
            
            var encrypted = _client.ConvertToEncryptedMessage(mes);
            
            Console.WriteLine(mes.Type);
        }

        public byte[] ConvertToEncryptedMessage(NewCamdMessage message)
        {
            _logger.Debug($"Prepare send data of type {message.Type} for encryption for {Name}");
            var buffer = new byte[NewCamdMessage.Size];
            _logger.Debug($"Copy {message.Data.Length} bytes into the buffer for {Name}");
            Buffer.BlockCopy(message.Data, 0, buffer, NewCamdMessage.HeaderLength + 4, message.Data.Length);
            _logger.Debug($"Prepare header information for {Name}");

            buffer[NewCamdMessage.HeaderLength + 4 + 1] = (byte)((message.Data[1] & 0xF0) | (((message.Data.Length - 3) >> 8) & 0x0F));
            buffer[NewCamdMessage.HeaderLength + 4 + 2] = (byte)((message.Data.Length - 3) & 0xFF);

            buffer[2] = (byte)(message.MessageId >> 8);
            buffer[3] = (byte)(message.MessageId & 0xFF);
            buffer[4] = (byte)(message.ServiceId >> 8);
            buffer[5] = (byte)(message.ServiceId & 0xFF);
            buffer[6] = (byte)(message.ProviderId >> 16);
            buffer[7] = (byte)((message.ProviderId >> 8) & 0xFF);
            buffer[8] = (byte)(message.ProviderId & 0xFF);

            _logger.Debug($"Encrypt data before sending to {Name}");

            /*
            memset(buffer + 2, 0, NEWCAMD_HDR_LEN + 2);
	memcpy(buffer + NEWCAMD_HDR_LEN + 4, data, data_len);

	buffer[NEWCAMD_HDR_LEN + 4 + 1] = (data[1] & 0xF0) | (((data_len - 3) >> 8) & 0x0F);
	buffer[NEWCAMD_HDR_LEN + 4 + 2] = (data_len - 3) & 0xFF;

	buffer[2] = msg_id >> 8;
	buffer[3] = msg_id & 0xFF;
	buffer[4] = service_id >> 8;
	buffer[5] = service_id & 0xFF;
	buffer[6] = provider_id >> 16;
	buffer[7] = (provider_id >> 8) & 0xFF;
	buffer[8] = provider_id & 0xFF;
	
	LOG(DEBUG, "[NEWCAMD] Send message msgid: %d, serviceid: %d, providerid: %d, length: %d", msg_id, service_id, provider_id, data_len + 2 + NEWCAMD_HDR_LEN);
    */
            var padding = new byte[8];
            _random.NextBytes(padding);

            var bufferLen = message.Data.Length + 4 + NewCamdMessage.HeaderLength;
            var paddingLen = (8 - ((bufferLen - 1) % 8)) % 8;
            Buffer.BlockCopy(padding, 0, buffer, bufferLen, paddingLen);
            bufferLen += paddingLen;
            buffer[bufferLen] = _client.XorSum(buffer.Skip(2).ToArray());
            bufferLen++;

            var ivec = new byte[8];
            _random.NextBytes(ivec);

            Buffer.BlockCopy(ivec, 0, buffer, bufferLen, ivec.Length);
            bufferLen += 8;

            var dataToEncrypt = buffer.Skip(2).Take(bufferLen).ToArray();
            var encrypted = _crypto.Encrypt(dataToEncrypt, _keyblock, ivec);

            var dataToSend = new List<byte>();
            dataToSend.Add((byte)((bufferLen - 2) >> 8));
            dataToSend.Add((byte)((bufferLen - 2) & 0xFF));
            dataToSend.AddRange(encrypted);

            return dataToSend.ToArray();
            /*

	DES_cblock padding;
	buf_len = data_len + NEWCAMD_HDR_LEN + 4;
	padding_len = (8 - ((buf_len - 1) % 8)) % 8;

	DES_random_key(&padding);
	memcpy(buffer + buf_len, padding, padding_len);
	buf_len += padding_len;
	buffer[buf_len] = xor_sum(buffer + 2, buf_len - 2);
	buf_len++;

	DES_cblock ivec;
	DES_random_key(&ivec);
	memcpy(buffer + buf_len, ivec, sizeof(ivec));
	print_hex("sended data", buffer + 2, data_len + NEWCAMD_HDR_LEN + 4);
	DES_ede2_cbc_encrypt(buffer + 2, buffer + 2, buf_len - 2, &c->ks1, &c->ks2, (DES_cblock *)ivec, DES_ENCRYPT);

	buf_len += sizeof(DES_cblock);
	buffer[0] = (buf_len - 2) >> 8;
	buffer[1] = (buf_len - 2) & 0xFF;
            */
        }

        void Validate(byte[] toTest)
        {
            var equal = _unencrypted.SequenceEqual(toTest);
            Console.WriteLine($"Success: {equal}");
        }

        byte[] Run1()
        {
            var td = CreateTDes();
            var decrypter = td.CreateDecryptor();
            return decrypter.TransformFinalBlock(_encrypted, 0, 56);
        }

        byte[] Run2()
        {
            var td = CreateTDes();
            td.Key = _keyblock;
            var decrypter = td.CreateDecryptor();
            return decrypter.TransformFinalBlock(_encrypted, 0, 56);
        }
        byte[] Run3()
        {
            var td = CreateTDes();
            td.Key = _keyblock;
            td.Padding = PaddingMode.None;
            var decrypter = td.CreateDecryptor();
            return decrypter.TransformFinalBlock(_encrypted, 0, 56);
        }

        TripleDES CreateTDes()
        {
            TripleDES td = new TripleDESCryptoServiceProvider();
            td.KeySize = 128;
            td.IV = _ivec;
            td.Mode = CipherMode.CBC;
            td.Padding = PaddingMode.Zeros;
            td.Key = _keyblock;
            return td;
        }

        void ReadData()
        {
            _randomKey = File.ReadAllBytes(GetPath("r.dat"));
            _keyblock = File.ReadAllBytes(GetPath("keyblock4.dat"));
            _unencrypted = File.ReadAllBytes(GetPath("u.dat"));
            _encrypted = File.ReadAllBytes(GetPath("encrypted5.dat"));
            _encrypted = File.ReadAllBytes(GetPath("encrypted6.dat"));
            _encrypted = File.ReadAllBytes(GetPath("e.dat"));
            _ivec = File.ReadAllBytes(GetPath("ivec7.dat"));
            _key1 = File.ReadAllBytes(GetPath("key12.dat"));
            _key2 = File.ReadAllBytes(GetPath("key23.dat"));
        }

        static string GetPath(string file)
        {
            return Path.Combine(@".\encrypttest", file);
        }
    }
}