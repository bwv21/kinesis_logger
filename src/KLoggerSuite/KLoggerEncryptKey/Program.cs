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
            Test();

            Console.Write("KEK: ");
            String kekString = Console.ReadLine();
            Console.Write("Key: ");
            String keyString = Console.ReadLine();

            if (String.IsNullOrEmpty(kekString) || String.IsNullOrEmpty(keyString))
            {
                Console.WriteLine("NotCompletePut KEK or Key.");
                return;
            }

            Byte[] kekBytes = new Byte[SimpleAES.AES_KEY_SIZE];
            Encoding.UTF8.GetBytes(kekString).ToArray().CopyTo(kekBytes, 0);

            try
            {
                Byte[] encryptedKey = SimpleAES.Encrypt(keyString, kekBytes);
                String encryptedKeyString = Convert.ToBase64String(encryptedKey);
                Console.WriteLine("Encrypted Key: " + encryptedKeyString);

                String plainKey = SimpleAES.Decrypt(encryptedKey, kekBytes);
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

        private static void Test()
        {
            const String KEK_STRING = "test-kek";
            const String PLAIN_TEXT = "O5X3j3OeNw6/uHN+J/1HY6B3zyo8EOMAisfKbzyPt4Y=";

            Byte[] kek = new Byte[SimpleAES.AES_KEY_SIZE];
            Encoding.UTF8.GetBytes(KEK_STRING).ToArray().CopyTo(kek, 0);

            Byte[] encrypt = SimpleAES.Encrypt(PLAIN_TEXT, kek);

            String decrypt = SimpleAES.Decrypt(encrypt, kek);

            if (PLAIN_TEXT != decrypt)
            {
                throw new Exception("Fail AES Test!");
            }
        }
    }
}

/*
             KLoggerAPI api = new KLoggerAPI(@"..\\..\\KLoggerConfigTest.json", null);

            api.Start();

            for (Int32 i = 0; i < 3000; ++i)
            {
                Object logObject = new
                                   {
                                       ValueInt = i,
                                       ValueString = Rand.RandString(Rand.RandInt32(1, 1000))
                                   };

                api.Push("test", logObject);
            }

*/