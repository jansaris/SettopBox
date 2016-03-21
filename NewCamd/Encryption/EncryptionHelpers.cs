using System;
using System.Security.Cryptography;

namespace NewCamd.Encryption
{
    public class EncryptionHelpers
    {
        public string UnixEncrypt(string toEncrypt, string salt)
        {
            if (!salt.StartsWith("$1$", StringComparison.Ordinal) || !salt.EndsWith("$", StringComparison.Ordinal))
            {
                throw new NotImplementedException("Only MD5 Unix encryption is implemented");
            }
            salt = salt.Substring(3, salt.Length - 4);
            var md5 = new MD5();
            var encrypted = md5.Encrypt(toEncrypt, salt);
            return encrypted;
        }

        public byte[] AesDecrypt(byte[] data, byte[] key)
        {
            using (var rijAlg = Rijndael.Create())
            {
                rijAlg.Mode = CipherMode.ECB;
                rijAlg.Padding = PaddingMode.Zeros;
                rijAlg.Key = key;
                rijAlg.IV = new byte[16];

                var decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);
                return decryptor.TransformFinalBlock(data, 0, data.Length);
            }
        }

        public byte[] Decrypt(byte[] encryptedMessage, int messageLength, byte[] keyblock, byte[] initializationVector)
        {
            TripleDES td = new TripleDESCryptoServiceProvider();
            td.KeySize = 128;
            td.IV = initializationVector;
            td.Mode = CipherMode.CBC;
            td.Padding = PaddingMode.Zeros;
            td.Key = keyblock;
            var decrypter = td.CreateDecryptor();
            return decrypter.TransformFinalBlock(encryptedMessage, 0, messageLength);
        }

        public byte[] Encrypt(byte[] unEncryptedMessage, byte[] keyblock, byte[] initializationVector)
        {
            TripleDES td = new TripleDESCryptoServiceProvider();
            td.KeySize = 128;
            td.IV = initializationVector;
            td.Mode = CipherMode.CBC;
            td.Padding = PaddingMode.Zeros;
            td.Key = keyblock;
            var decrypter = td.CreateEncryptor();
            return decrypter.TransformFinalBlock(unEncryptedMessage, 0, unEncryptedMessage.Length);
        }

        public byte[] CreateKeySpread(byte[] key)
        {
            var spread = new byte[16];
            spread[0] = (byte)(key[0] & 0xfe);
            spread[1] = (byte)(((key[0] << 7) | (key[1] >> 1)) & 0xfe);
            spread[2] = (byte)(((key[1] << 6) | (key[2] >> 2)) & 0xfe);
            spread[3] = (byte)(((key[2] << 5) | (key[3] >> 3)) & 0xfe);
            spread[4] = (byte)(((key[3] << 4) | (key[4] >> 4)) & 0xfe);
            spread[5] = (byte)(((key[4] << 3) | (key[5] >> 5)) & 0xfe);
            spread[6] = (byte)(((key[5] << 2) | (key[6] >> 6)) & 0xfe);
            spread[7] = (byte)(key[6] << 1);
            spread[8] = (byte)(key[7] & 0xfe);
            spread[9] = (byte)(((key[7] << 7) | (key[8] >> 1)) & 0xfe);
            spread[10] = (byte)(((key[8] << 6) | (key[9] >> 2)) & 0xfe);
            spread[11] = (byte)(((key[9] << 5) | (key[10] >> 3)) & 0xfe);
            spread[12] = (byte)(((key[10] << 4) | (key[11] >> 4)) & 0xfe);
            spread[13] = (byte)(((key[11] << 3) | (key[12] >> 5)) & 0xfe);
            spread[14] = (byte)(((key[12] << 2) | (key[13] >> 6)) & 0xfe);
            spread[15] = (byte)(key[13] << 1);

            return spread;
        }
    }
}