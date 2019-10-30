using System;
using System.Linq;
using System.Text;
using KLogger.Libs;

namespace KLoggerEncryptKey
{
    internal class Program
    {
        private static void Main(String[] args)
        {
            Console.Write("KEK: ");
            String kekString = Console.ReadLine();
            Console.Write("Key: ");
            String keyString = Console.ReadLine();

            if (String.IsNullOrEmpty(kekString) || String.IsNullOrEmpty(keyString))
            {
                Console.WriteLine("NotCompletePut KEK or Key.");
                return;
            }

            Byte[] kekBytes = new Byte[SimpleAES4String.AES_KEY_SIZE];
            Encoding.UTF8.GetBytes(kekString).ToArray().CopyTo(kekBytes, 0);

            try
            {
                String encryptedKey = SimpleAES4String.Encrypt(keyString, kekString);
                Console.WriteLine("Encrypted Key: " + encryptedKey);

                String plainKey = SimpleAES4String.Decrypt(encryptedKey, kekString);
                if (String.CompareOrdinal(keyString, plainKey) != 0)
                {
                    Console.WriteLine("Fail Test!!!");
                    return;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }

            Console.WriteLine("ok.");
            Console.ReadLine();
        }
    }
}
