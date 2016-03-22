using System.IO;
using com.wibblr.utils;
using NUnit.Framework;

namespace com.wibblr.b2.console
{
    [TestFixture]
    public class AuthTests
    {
        [Test]
        public void CredentialsFileIsCreatedIfNotExists()
        {
            using (var t = new TempDirectory("b2.net"))
            {
                var credentialsPath = Path.Combine(t.FullPath, "credentials.json");

                using (var cc = new ConsoleCapture())
                {
                    Assert.AreEqual(0, new Program().Run("auth", "--path", credentialsPath, "--account", "wallace", "--appkey", "gorgonzola" ));
                    Assert.AreEqual("", cc.StandardOutput);
                    Assert.AreEqual("", cc.StandardError);
                }

                using (var cc = new ConsoleCapture())
                {
                    Assert.AreEqual(0, new Program().Run("auth", "--path", credentialsPath));
                    Assert.AreEqual("Account Id: 'wallace',  Application Key: 'gorgonzola'\r\n", cc.StandardOutput);
                    Assert.AreEqual("", cc.StandardError);
                }
            }
        }

        [Test]
        public void CredentialsFileIsOverwrittenIfExists()
        {
            using (var t = new TempDirectory("b2.net"))
            {
                var credentialsPath = Path.Combine(t.FullPath, "credentials.json");

                using (var cc = new ConsoleCapture())
                {
                    Assert.AreEqual(0, new Program().Run("auth", "--path", credentialsPath, "--account", "wallace", "--appkey", "gorgonzola"));
                    Assert.AreEqual("", cc.StandardOutput);
                    Assert.AreEqual("", cc.StandardError);
                }

                using (var cc = new ConsoleCapture())
                {
                    Assert.AreEqual(0, new Program().Run("auth", "--path", credentialsPath));
                    Assert.AreEqual("Account Id: 'wallace',  Application Key: 'gorgonzola'\r\n", cc.StandardOutput);
                    Assert.AreEqual("", cc.StandardError);
                }

                using (var cc = new ConsoleCapture())
                {
                    Assert.AreEqual(0, new Program().Run("auth", "--path", credentialsPath, "--account", "gromit", "--appkey", "wensleydale"));
                    Assert.AreEqual("", cc.StandardOutput);
                    Assert.AreEqual("", cc.StandardError);
                }

                using (var cc = new ConsoleCapture())
                {
                    Assert.AreEqual(0, new Program().Run("auth", "--path", credentialsPath));
                    Assert.AreEqual("Account Id: 'gromit',  Application Key: 'wensleydale'\r\n", cc.StandardOutput);
                    Assert.AreEqual("", cc.StandardError);
                }
            }
        }

        [Test]
        public void CredentialsFileIsDeleted()
        {
            using (var t = new TempDirectory("b2.net"))
            {
                var credentialsPath = Path.Combine(t.FullPath, "credentials.json");

                using (var cc = new ConsoleCapture())
                {
                    Assert.AreEqual(0, new Program().Run("auth", "--path", credentialsPath, "--account", "wallace", "--appkey", "gorgonzola"));
                    Assert.AreEqual("", cc.StandardOutput);
                    Assert.AreEqual("", cc.StandardError);
                }

                using (var cc = new ConsoleCapture())
                {
                    Assert.AreEqual(0, new Program().Run("auth", "--path", credentialsPath));
                    Assert.AreEqual("Account Id: 'wallace',  Application Key: 'gorgonzola'\r\n", cc.StandardOutput);
                    Assert.AreEqual("", cc.StandardError);
                }

                using (var cc = new ConsoleCapture())
                {
                    Assert.AreEqual(0, new Program().Run("auth", "--path", credentialsPath, "--delete"));
                    Assert.AreEqual("", cc.StandardOutput);
                    Assert.AreEqual("", cc.StandardError);
                }

                using (var cc = new ConsoleCapture())
                {
                    Assert.AreEqual(1, new Program().Run("auth", "--path", credentialsPath));
                    Assert.AreEqual("", cc.StandardOutput);
                    Assert.IsTrue(cc.StandardError.Contains("Unable to read credentials file"));
                }
            }
        }
    }
}
