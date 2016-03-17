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

            var mes = TestFiles("random3.dat", "rencrypted19.dat", "rivec20.dat");
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

        NewCamdMessage TestFiles(string key, string encrypted, string ivec)
        {
            var random = File.ReadAllBytes(GetPath(key));
            var encOld = File.ReadAllBytes(GetPath("rencrypted18.dat"));
            var len = ((encOld[0] << 8) | encOld[1]) & 0xFFFF;
            var enc = File.ReadAllBytes(GetPath(encrypted));
            var iv = File.ReadAllBytes(GetPath(ivec));

            
            var generated = _client.InitializeKeys();
            //_client._keyblock = _crypto.CreateKeySpread(random);
            var expectedPassword = _crypto.UnixEncrypt("pass", "$1$abcdefgh$");
            _client.InitializeKeys();
            _client.UpdateKeyBlock(expectedPassword);
            var mes = _client.ParseMessage(enc);
            Console.WriteLine(mes.Type);
            return mes;
        }

        public void UpdateKeyBlock(string encryptedPassword)
        {
            var random = File.ReadAllBytes(GetPath("random3.dat"));
            for (var i = 0; i < encryptedPassword.Length; i++)
            {
                random[i % 14] ^= (byte)encryptedPassword[i];
            }
            _keyblock = _crypto.CreateKeySpread(random);
        }

        void TestEncrypt(NewCamdMessage mes)
        {
            var expect = File.ReadAllBytes(GetPath("sToSend22.dat"));
            mes.Type = NewCamdMessageType.MsgCardData;
            mes.Data = new byte[26];
            mes.Data[0] = (byte)NewCamdMessageType.MsgCardData;

            //Provide CAID
            mes.Data[4] = 0x56;
            mes.Data[5] = 0x01;

            mes.Data[14] = 1; //Set number of cards
            mes.Data[17] = 1; //Set provider ID of card 1

            CompareArrays(expect, mes.Data);

            UpdateKeyBlock(_crypto.UnixEncrypt("pass", "$1$abcdefgh$"));
            //_keyblock = _crypto.CreateKeySpread(File.ReadAllBytes(GetPath("random3.dat")));

            var encrypted = ConvertToEncryptedMessage(mes);
            var toCompare = File.ReadAllBytes(GetPath("sencryptedForSend28.dat"));
            
            Console.WriteLine(mes.Type);
        }

        public byte[] ConvertToEncryptedMessage(NewCamdMessage message)
        {
            _logger.Debug($"Prepare send data of type {message.Type} for encryption for {Name}");
            var prepareData  = new List<byte>();
            _logger.Debug($"Prepare message headers for {Name}");
            prepareData.Add((byte)(message.MessageId >> 8));
            prepareData.Add((byte)(message.MessageId & 0xFF));
            prepareData.Add((byte)(message.ServiceId >> 8));
            prepareData.Add((byte)(message.ServiceId & 0xFF));
            prepareData.Add((byte)(message.ProviderId >> 16));
            prepareData.Add((byte)((message.ProviderId >> 8) & 0xFF));
            prepareData.Add((byte)(message.ProviderId & 0xFF));
            prepareData.Add(0);
            prepareData.Add(0);
            prepareData.Add(0);

            _logger.Debug($"Correct message headers for {Name}");
            message.Data[1] = (byte)((message.Data[1] & 240) | (((message.Data.Length - 3) >> 8) & 255));
            message.Data[2] = (byte)((message.Data.Length - 3) & 255);
            _logger.Debug($"Copy {message.Data.Length} bytes into the buffer for {Name}");
            prepareData.AddRange(message.Data);
            //Fill up
            while (prepareData.Count % 8 != 7) prepareData.Add(0);
            

            var compare = File.ReadAllBytes(GetPath("sToSendWithBuffer23.dat"));
            CompareArrays(prepareData.ToArray(),compare);

            _logger.Debug($"Encrypt data before sending to {Name}");

            var padding = new byte[8];
            _random.NextBytes(padding);
            padding = File.ReadAllBytes(GetPath("spadding24.dat"));

            //fill up bytes with padding data at the end
            var bufferLen = prepareData.Count;
            var paddingLen = (8 - ((bufferLen - 2) % 8)) % 8;
            var prepareDataArray = prepareData.ToArray();
            Buffer.BlockCopy(padding, 0, prepareDataArray, bufferLen-paddingLen, paddingLen);
            prepareData = prepareDataArray.ToList();
            //Add checksum at byte 16
            prepareData.Add(_client.XorSum(prepareData.ToArray()));

            //And validate again
            var withPadding = File.ReadAllBytes(GetPath("swithpaddingAndxor25.dat"));
            CompareArrays(prepareData.ToArray(), withPadding);

            var ivec = new byte[8];
            _random.NextBytes(ivec);
            ivec = File.ReadAllBytes(GetPath("sivecToSend26.dat"));

            var before = File.ReadAllBytes(GetPath("sbeforeEncrypt27.dat"));
            CompareArrays(prepareData.ToArray(), before);

            var dataToEncrypt = prepareData.ToArray();
            var encrypted = _crypto.Encrypt(dataToEncrypt, _keyblock, ivec).ToList();

            var dataToSend = new List<byte>();
            dataToSend.Add((byte)((encrypted.Count + ivec.Length) >> 8));
            dataToSend.Add((byte)((encrypted.Count + ivec.Length) & 0xFF));
            dataToSend.AddRange(encrypted);
            dataToSend.AddRange(ivec);

            var sending = File.ReadAllBytes(GetPath("sencryptedForSend28.dat"));
            CompareArrays(dataToSend.ToArray(), sending);

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

        void CompareArrays(byte[] array1, byte[] array2)
        {
            for (var i = 0; i < array1.Length; i++)
            {
                if (array2.Length <= i)
                {
                    _logger.Warn($"Array2 is shorter than array1, compare stops at {i}");
                    return;
                }
                if (array1[i] != array2[i])
                {
                    _logger.Warn($"First mismatch at byte {i}");
                    return;
                }
            }
            _logger.Info("Arrays are equal");
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
            return Path.Combine(@".\testfiles", file);
        }
    }
}