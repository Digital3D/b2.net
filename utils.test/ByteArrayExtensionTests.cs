using NUnit.Framework;

namespace com.wibblr.utils
{
    [TestFixture]
    public class ByteArrayExtensionTests
    {
        [Test]
        public void ToHex()
        {
            var a = new byte[] { };
            Assert.AreEqual("", a.ToHex());

            a = new byte[] { (byte)'A' };
            Assert.AreEqual("41", a.ToHex()); // 'A' = ascii 65. 65/16 = 4. 65%16 = 1.

            a = new byte[] { (byte)'A', 40 };
            Assert.AreEqual("4128", a.ToHex()); // 40/16 = 2. 40%16 = 8.

            a = new byte[] { (byte)'A', 40, 175 };
            Assert.AreEqual("4128AF", a.ToHex()); // 165/16 = 10. 175%16 = 15.
        }
    }
}
