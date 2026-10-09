using System;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace TCT_App.PrintUtils
{
    public class ClassLibAES
    {
        public static byte[] Encrypt(string plainText, byte[] Key, byte[] IV)
        {
            byte[] encrypted;
            // Create a new AesManaged.    
            using (AesManaged aes = new AesManaged())
            {
                aes.Mode = CipherMode.CBC;
                // Create encryptor    
                ICryptoTransform encryptor = aes.CreateEncryptor(Key, IV);
                // Create MemoryStream    
                using (MemoryStream ms = new MemoryStream())
                {
                    // Create crypto stream using the CryptoStream class. This class is the key to encryption    
                    // and encrypts and decrypts data from any given stream. In this case, we will pass a memory stream    
                    // to encrypt    
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        // Create StreamWriter and write data to a stream    
                        using (StreamWriter sw = new StreamWriter(cs))
                            sw.Write(plainText);
                        encrypted = ms.ToArray();
                    }
                }
            }
            // Return encrypted data    
            if (LogInfo.IsEnabled(LogLevel.TRACE))
                LogInfo.WriteLog(LogLevel.TRACE, $"+ ClassLibAES.Encrypt : plainLen={plainText?.Length}, keyLen={Key?.Length}, ivLen={IV?.Length}, cipherLen={encrypted?.Length}");
            return encrypted;
        }

        public static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");
            byte[] encrypted;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                aesAlg.Mode = CipherMode.CBC;

                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            // Return the encrypted bytes from the memory stream.
            if (LogInfo.IsEnabled(LogLevel.TRACE))
                LogInfo.WriteLog(LogLevel.TRACE, $"+ ClassLibAES.EncryptStringToBytes_Aes : plainLen={plainText.Length}, keyLen={Key.Length}, ivLen={IV.Length}, cipherLen={encrypted?.Length}");
            return encrypted;
        }


        public static string DecryptString(string key, string iv, string cipherText)  
        { 
            byte[] buffer = Convert.FromBase64String(cipherText);  
            if (LogInfo.IsEnabled(LogLevel.TRACE))
                LogInfo.WriteLog(LogLevel.TRACE, $"+ ClassLibAES.DecryptString : cipherLen={buffer.Length}, keyLen={key?.Length}, ivLen={iv?.Length}");
  
            using (Aes aes = Aes.Create())  
            {  
                aes.Key     = Encoding.UTF8.GetBytes(key);  
                aes.IV      = Encoding.UTF8.GetBytes(iv);
                aes.Mode    = CipherMode.CBC;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);  
  
                using (MemoryStream memoryStream = new MemoryStream(buffer))  
                {  
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Read))  
                    {  
                        using (StreamReader streamReader = new StreamReader((Stream)cryptoStream))  
                        {  
                            return streamReader.ReadToEnd();  
                        }  
                    }  
                }  
            }  
        } 
    }
}
