using NUnit.Framework;

namespace com.wibblr.utils
{
    [TestFixture]
    public class StringExtensionTests
    {
        [Test]
        public void Sha1()
        {
            Assert.AreEqual("hello world!".Sha1(), "430CE34D020724ED75A196DFC2AD67C77772D169");
        }
    }
}
