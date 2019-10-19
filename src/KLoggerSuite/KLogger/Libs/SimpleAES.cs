using System;
using System.IO;
using System.Security.Cryptography;

namespace KLogger.Libs
{
    // https://msdn.microsoft.com/en-us/library/system.security.cryptography.aesmanaged(v=vs.110).aspx
    internal static class SimpleAES
    {
        public const Int32 AES_KEY_SIZE = 32;     // 256 bit. AES256.
        public const Int32 AES_BLOCK_SIZE = 16;   // 128 bit.

        public static Byte[] Encrypt(String plainText, Byte[] key, Byte[] iv = null)
        {
            if (String.IsNullOrEmpty(plainText))
            {
                throw new ArgumentNullException(nameof(plainText));
            }

            if (key == null || key.Length <= 0)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (iv == null || iv.Length <= 0)
            {
                iv = new Byte[AES_BLOCK_SIZE];
            }

            Byte[] encrypted;
            using (var aesManaged = new AesManaged())
            {
                aesManaged.Mode = CipherMode.CBC;
                aesManaged.Key = key;
                aesManaged.IV = iv;

                ICryptoTransform encryptor = aesManaged.CreateEncryptor(aesManaged.Key, aesManaged.IV);

                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }

                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            return encrypted;
        }

        public static String Decrypt(Byte[] cipher, Byte[] key, Byte[] iv = null)
        {
            if (cipher == null || cipher.Length <= 0)
            {
                throw new ArgumentNullException(nameof(cipher));
            }

            if (key == null || key.Length <= 0)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (iv == null || iv.Length <= 0)
            {
                iv = new Byte[AES_BLOCK_SIZE];
            }

            String plainText;
            using (var aesManaged = new AesManaged())
            {
                aesManaged.Mode = CipherMode.CBC;
                aesManaged.Key = key;
                aesManaged.IV = iv;

                ICryptoTransform decryptor = aesManaged.CreateDecryptor(aesManaged.Key, aesManaged.IV);

                using (var msDecrypt = new MemoryStream(cipher))
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (var srDecrypt = new StreamReader(csDecrypt))
                {
                    plainText = srDecrypt.ReadToEnd();
                }
            }

            return plainText;
        }
    }
}
