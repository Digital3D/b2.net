using System.IO;
using System.Linq;
using com.wibblr.utils;
using NUnit.Framework;

namespace com.wibblr.b2
{
    [TestFixture]
    public class FilesystemScannerTests
    {
        [Test]
        public void SingleFileShouldHaveNoRelativeParents()
        {
            using (var t = new TempDirectory())
            {
                t.CreateFile("a");
                var scan = FilesystemScanner.Scan($"{t.FullPath}\\a").ToArray();
                Assert.AreEqual("a", scan[0].Info.Name);
            }
        }

        [Test]
        public void DirectoryShouldBeRootOfRelativePath()
        {
            using (var t = new TempDirectory())
            {
                t.CreateFile("a");
                var scan = FilesystemScanner.Scan(t.FullPath).ToArray();

                Assert.AreEqual(t.Name, scan[0].RelativePath);
                Assert.AreEqual($"{t.Name}\\a", scan[1].RelativePath);
            }
        }

        [Test]
        public void DirectoryWithSlashShouldNotBeRootOfRelativePath()
        {
            using (var t = new TempDirectory())
            {
                t.CreateFile("a");
                t.CreateFile("b");
                var scan = FilesystemScanner.Scan(t.FullPath + Path.DirectorySeparatorChar).ToArray();
                
                Assert.AreEqual("a", scan[0].RelativePath);
                Assert.AreEqual("b", scan[1].RelativePath);
            }
        }

        [Test]
        public void OrderShouldBeFilesThenDirectores()
        {
            using (var t = new TempDirectory())
            {
                t.CreateDir("b");
                t.CreateDir("a");
                t.CreateFile("b\\b.txt");
                t.CreateFile("b\\a.txt");
                t.CreateFile("a\\b.txt");
                t.CreateFile("a\\a.txt");
                t.CreateFile("z.txt");
                t.CreateFile("y.txt");
                t.CreateFile("x.txt");
                var scan = FilesystemScanner.Scan(t.FullPath + Path.DirectorySeparatorChar).ToArray();

                Assert.AreEqual("x.txt", scan[0].RelativePath); // files first
                Assert.AreEqual("y.txt", scan[1].RelativePath);
                Assert.AreEqual("z.txt", scan[2].RelativePath); 
                Assert.AreEqual("a", scan[3].RelativePath); // then all of directory 'a'
                Assert.AreEqual("a\\a.txt", scan[4].RelativePath);
                Assert.AreEqual("a\\b.txt", scan[5].RelativePath);
                Assert.AreEqual("b", scan[6].RelativePath); // then all of directory 'b'
                Assert.AreEqual("b\\a.txt", scan[7].RelativePath);
                Assert.AreEqual("b\\b.txt", scan[8].RelativePath);
            }
        }
    }
}
