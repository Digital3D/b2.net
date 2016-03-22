using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using NUnit.Framework;

namespace com.wibblr.utils
{
    [TestFixture]
    public class TempDirectoryTests
    {
        [Test]
        public void ThrowsOnMissingEnvironmentVariable()
        {
            Environment.SetEnvironmentVariable("TEMP", null);
            Assert.Throws<Exception>(() => new TempDirectory());
        }

        [Test]
        public void IntermediateDirectoryCanBeSpecified()
        {
            var intermediateDirectory = RandomString.Next(16);

            using (var t = new TempDirectory(intermediateDirectory))
            {
                var expectedParentOfTempDirectory = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), intermediateDirectory);
                var actualParentOfTempDirectory = new DirectoryInfo(t.FullPath).Parent.FullName;

                Assert.AreEqual(expectedParentOfTempDirectory, actualParentOfTempDirectory);
            }
        }

        [Test]
        public void DirectoryShouldBeDeletedOnDispose()
        {
            TempDirectory t;
            using (t = new TempDirectory())
            {
                Assert.IsTrue(Directory.Exists(t.FullPath));         
            }
            Assert.IsFalse(Directory.Exists(t.FullPath));
        }
    }
}
