using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace com.wibblr.b2
{
    [TestFixture]
    public class ExtensionMethodsTests
    {
        [Test]
        public void DateTime_ToUnixTimeMillis()
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            Assert.AreEqual(0, epoch.ToUnixTimeMillis());

            var dt = new DateTime(2016, 03, 07, 06, 28, 21, 20, DateTimeKind.Utc);
            Assert.AreEqual(1457332101020, dt.ToUnixTimeMillis());
        }
    }
}
