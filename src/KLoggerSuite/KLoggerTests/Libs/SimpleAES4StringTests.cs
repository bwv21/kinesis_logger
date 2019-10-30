using System;
using KLogger.Libs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KLoggerTests.Libs
{
    [TestClass]
    public class SimpleAES4StringTests
    {
        [TestMethod]
        public void 암호화_복호화_테스트()
        {
            const String KEY = "test-key";
            const String PLAIN_TEXT = "O5X3j3OeNw6/uHN+J/1HY6B3zyo8EOMAisfKbzyPt4Y=";

            {
                String encrypted = SimpleAES4String.Encrypt(PLAIN_TEXT, KEY);
                String decrypted = SimpleAES4String.Decrypt(encrypted, KEY);
                Assert.IsTrue(PLAIN_TEXT == decrypted);
            }

            Assert.ThrowsException<ArgumentNullException>(() => SimpleAES4String.Encrypt(null, KEY));

            Assert.ThrowsException<ArgumentNullException>(() => SimpleAES4String.Encrypt("...", String.Empty));

            Assert.ThrowsException<ArgumentNullException>(() => SimpleAES4String.Decrypt(null, KEY));

            Assert.ThrowsException<ArgumentNullException>(() => SimpleAES4String.Decrypt("...", String.Empty));
        }
    }
}
