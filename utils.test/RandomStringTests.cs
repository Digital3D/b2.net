using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;


namespace com.wibblr.utils
{
    [TestFixture]
    public class RandomStringTests
    {
        [Test]
        public void ThrowsOnInvalidArgument()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RandomString.Next(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => RandomString.Next(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => RandomString.Next(17));
        }

        [Test]
        public void TestRandomString()
        {
            for (int i = 1; i < 16; i++)
            {
                var s = RandomString.Next(i);
                Assert.AreEqual(i * 2, s.Length);
            }
        }
    }
}
