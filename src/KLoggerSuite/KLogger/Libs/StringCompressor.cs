using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace KLogger.Libs
{
    internal static class StringCompressor
    {
        public static String Compress(String text)
        {
            if (String.IsNullOrEmpty(text))
            {
                return String.Empty;
            }

            String encryptedString;

            Byte[] bytes = Encoding.UTF8.GetBytes(text);
            using (var inputStream = new MemoryStream(bytes))
            {
                using (var outputStream = new MemoryStream())
                {
                    using (var gZipStream = new GZipStream(outputStream, CompressionMode.Compress))
                    {
                        inputStream.CopyTo(gZipStream);
                    }

                    encryptedString = Convert.ToBase64String(outputStream.ToArray());
                }
            }

            return encryptedString;
        }

        public static String Decompress(String compressedText)
        {
            if (String.IsNullOrEmpty(compressedText))
            {
                return String.Empty;
            }

            String decryptedString;

            Byte[] bytes = Convert.FromBase64String(compressedText);
            using (var inputStream = new MemoryStream(bytes))
            {
                using (var outputStream = new MemoryStream())
                {
                    using (var gZipStream = new GZipStream(inputStream, CompressionMode.Decompress))
                    {
                        gZipStream.CopyTo(outputStream);
                    }

                    decryptedString = Encoding.UTF8.GetString(outputStream.ToArray());
                }
            }

            return decryptedString;
        }
    }
}
