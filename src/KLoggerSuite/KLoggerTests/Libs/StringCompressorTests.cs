using System;
using KLogger.Libs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KLoggerTests.Libs
{
    [TestClass]
    public class StringCompressorTests
    {
        [TestMethod]
        public void 압축_테스트()
        {
            const String ORIGIN_TEXT = "Test text..... test test test !!!!!!!!!!! 1234567890";

            String compressed = StringCompressor.Compress(ORIGIN_TEXT);

            String decompressed = StringCompressor.Decompress(compressed);

            Assert.IsTrue(decompressed == ORIGIN_TEXT);

            Assert.IsTrue(StringCompressor.Compress(null) == String.Empty);

            Assert.IsTrue(StringCompressor.Decompress(null) == String.Empty);
        }
    }
}
