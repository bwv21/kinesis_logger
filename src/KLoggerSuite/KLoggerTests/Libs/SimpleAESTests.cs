using System;
using System.Linq;
using System.Text;
using KLogger.Libs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KLoggerTests.Libs
{
    [TestClass]
    public class SimpleAESTests
    {
        [TestMethod]
        public void 암호화_복호화_테스트()
        {
            const String KEK_STRING = "test-kek";
            const String PLAIN_TEXT = "O5X3j3OeNw6/uHN+J/1HY6B3zyo8EOMAisfKbzyPt4Y=";

            Byte[] kek = new Byte[SimpleAES.AES_KEY_SIZE];
            Encoding.UTF8.GetBytes(KEK_STRING).ToArray().CopyTo(kek, 0);

            Byte[] encrypt = SimpleAES.Encrypt(PLAIN_TEXT, kek);

            String decrypt = SimpleAES.Decrypt(encrypt, kek);

            Assert.IsTrue(PLAIN_TEXT == decrypt);

            Assert.ThrowsException<ArgumentNullException>(() => SimpleAES.Encrypt(null, kek));

            Assert.ThrowsException<ArgumentNullException>(() => SimpleAES.Encrypt(PLAIN_TEXT, null));

            Assert.ThrowsException<ArgumentNullException>(() => SimpleAES.Decrypt(null, kek));

            Assert.ThrowsException<ArgumentNullException>(() => SimpleAES.Decrypt(encrypt, null));
        }
    }
}
