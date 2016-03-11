using System;
using System.Security.Cryptography;
using System.Text;

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

        public string EncryptTripleDes(string toEncrypt, string key)
        {
            byte[] keyArray;
            var toEncryptArray = Encoding.UTF8.GetBytes(toEncrypt);

            //If hashing use get hashcode regards to your key
            if (UseHashing(key))
            {
                var hashmd5 = new MD5CryptoServiceProvider();
                keyArray = hashmd5.ComputeHash(Encoding.UTF8.GetBytes(key));
                //Always release the resources and flush data
                // of the Cryptographic service provide. Best Practice
                hashmd5.Clear();
            }
            else
            {
                keyArray = Encoding.UTF8.GetBytes(key);
            }

            //set the secret key for the tripleDES algorithm
            //mode of operation. there are other 4 modes.
            //We choose ECB(Electronic code Book)
            //padding mode(if any extra byte added)
            var tdes = new TripleDESCryptoServiceProvider
            {
                Key = keyArray,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };

            var cTransform = tdes.CreateEncryptor();
            //transform the specified region of bytes array to resultArray
            var resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            //Release resources held by EncryptionHelpers Encryptor
            tdes.Clear();
            //Return the encrypted data into unreadable string format
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
            //return Encoding.ASCII.GetString(resultArray);
        }

        public string DecryptTripleDes(string cipherString, string key)
        {
            byte[] keyArray;
            //get the byte code of the string
            var toEncryptArray = Encoding.ASCII.GetBytes(cipherString);

            if (UseHashing(key))
            {
                //if hashing was used get the hash code with regards to your key
                var hashmd5 = new MD5CryptoServiceProvider();
                keyArray = hashmd5.ComputeHash(Encoding.UTF8.GetBytes(key));
                //release any resource held by the MD5CryptoServiceProvider
                hashmd5.Clear();
            }
            else
            {
                //if hashing was not implemented get the byte code of the key
                keyArray = Encoding.UTF8.GetBytes(key);
            }

            //set the secret key for the tripleDES algorithm
            //mode of operation. there are other 4 modes. 
            //We choose ECB(Electronic code Book)
            //padding mode(if any extra byte added)
            var tdes = new TripleDESCryptoServiceProvider
            {
                Key = keyArray,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };
            
            var cTransform = tdes.CreateDecryptor();
            var resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            //Release resources held by EncryptionHelpers Encryptor                
            tdes.Clear();
            //return the Clear decrypted TEXT
            return Encoding.UTF8.GetString(resultArray);
        }

        static bool UseHashing(string key)
        {
            return key.Contains("$1$") && key.EndsWith("$", StringComparison.Ordinal);
        }
    }
}