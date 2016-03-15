using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

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

        public void Run()
        {
            ReadData();
            Validate(Run1());
            Validate(Run2());
            Validate(Run3());
            Console.ReadKey();
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
            _random = File.ReadAllBytes(GetPath("random1.dat"));
            _keyblock = File.ReadAllBytes(GetPath("keyblock4.dat"));
            _unencrypted = File.ReadAllBytes(GetPath("unencrypted8.dat"));
            _encrypted = File.ReadAllBytes(GetPath("encrypted5.dat"));
            _encrypted = File.ReadAllBytes(GetPath("encrypted6.dat"));
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