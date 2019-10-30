using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace KLogger.Libs
{
    // https://msdn.microsoft.com/en-us/library/system.security.cryptography.aesmanaged(v=vs.110).aspx
    internal static class SimpleAES4String
    {
        internal const Int32 AES_KEY_SIZE = 32;     // 256 bit. AES256.
        internal const Int32 AES_BLOCK_SIZE = 16;   // 128 bit.

        public static String Encrypt(String plain, String key, Byte[] iv = null)
        {
            if (String.IsNullOrEmpty(plain))
            {
                throw new ArgumentNullException(nameof(plain));
            }

            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            Byte[] encodedKey = new Byte[AES_KEY_SIZE];
            Encoding.UTF8.GetBytes(key).ToArray().CopyTo(encodedKey, 0);

            return EncryptImpl(plain, encodedKey, iv);
        }

        public static String Decrypt(String cipher, String key, Byte[] iv = null)
        {
            if (String.IsNullOrEmpty(cipher))
            {
                throw new ArgumentNullException(nameof(cipher));
            }

            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            Byte[] encodedKey = new Byte[AES_KEY_SIZE];
            Encoding.UTF8.GetBytes(key).ToArray().CopyTo(encodedKey, 0);

            return DecryptImpl(cipher, encodedKey, iv);
        }

        private static String EncryptImpl(String plain, Byte[] key, Byte[] iv)
        {
            if (iv == null || iv.Length <= 0)
            {
                iv = new Byte[AES_BLOCK_SIZE];
            }

            Byte[] encrypted = null;

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
                            swEncrypt.Write(plain);
                        }

                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            return Convert.ToBase64String(encrypted);
        }

        private static String DecryptImpl(String cipher, Byte[] key, Byte[] iv)
        {
            if (iv == null || iv.Length <= 0)
            {
                iv = new Byte[AES_BLOCK_SIZE];
            }

            Byte[] encodedCipher = Convert.FromBase64String(cipher);

            String plain = null;

            using (var aesManaged = new AesManaged())
            {
                aesManaged.Mode = CipherMode.CBC;
                aesManaged.Key = key;
                aesManaged.IV = iv;

                ICryptoTransform decryptor = aesManaged.CreateDecryptor(aesManaged.Key, aesManaged.IV);

                using (var msDecrypt = new MemoryStream(encodedCipher))
                {
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (var srDecrypt = new StreamReader(csDecrypt))
                        {
                            plain = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return plain;
        }
    }
}
