using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using log4net;
using NewCamd.Encryption;

namespace NewCamd
{
    public class Decrypt
    {
        byte[] _random;
        byte[] _ivec;
        byte[] _keyblock;
        byte[] _unencrypted;
        byte[] _encrypted;
        byte[] _key1;
        byte[] _key2;

        readonly EncryptionHelpers _crypto = new EncryptionHelpers();
        NewCamdClient _client;

        public void Run(Settings getInstance)
        {
            _client = new NewCamdClient(LogManager.GetLogger(typeof(Decrypt)), getInstance, _crypto);

            ReadData();
            TestFiles("r.dat", "e.dat", "u.dat");
            //_keyblock = _crypto.CreateKeySpread(_random);
            //var client = new NewCamdClient(LogManager.GetLogger(typeof(Decrypt)),null,_crypto);
            //client._keyblock = _keyblock;
            //var mes = client.ParseMessage(_encrypted);
            Validate(Run1());
            Validate(Run2());
            Validate(Run3());
            Console.ReadKey();
        }

        void TestFiles(string key, string encrypted, string unencrypted)
        {
            var random = File.ReadAllBytes(GetPath(key));
            var enc = File.ReadAllBytes(GetPath(encrypted));
            var unenc = File.ReadAllBytes(GetPath(unencrypted));
            var keys = File.ReadAllBytes(GetPath("keybytes1.dat"));
            var written = File.ReadAllBytes(GetPath("randomwritten1.dat"));
            
            var generated = _client.InitializeKeys();
            _client._keyblock = _crypto.CreateKeySpread(random);
            var mes = _client.ParseMessage(enc);
            Console.WriteLine(mes.Type);
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
            _random = File.ReadAllBytes(GetPath("r.dat"));
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
            return Path.Combine(@".\decrypttest", file);
        }
    }
}