using System;
using System.IO;

using NUnit.Framework;

using com.wibblr.utils;

namespace com.wibblr.b2
{
    [TestFixture]
    public class CredentialsTests
    {
        private string ToJson(string accountId, string applicationKey) => $"{{\"accountId\":\"{accountId}\",\"applicationKey\":\"{applicationKey}\"}}";

        [Test]
        public void WriteCredentialsTest()
        {
            using (var t = new TempDirectory())
            {
                var path = Path.Combine(t.FullPath, "credentials.json");

                Credentials.Write("123", "456", path);
                var actualText = File.ReadAllText(path);
                Assert.AreEqual(ToJson("123", "456"), actualText);

                Credentials.Write("78", "90", path);
                actualText = File.ReadAllText(path);
                Assert.AreEqual(ToJson("78", "90"), actualText);

                Credentials.Write("78", "", path);
                actualText = File.ReadAllText(path);
                Assert.AreEqual(ToJson("78", ""), actualText);

                Credentials.Write(null, "123", path);
                actualText = File.ReadAllText(path);
                Assert.AreEqual(ToJson("", "123"), actualText);
            }
        }

        [Test]
        public void ReadCredentialsTest()
        {
            using (var t = new TempDirectory())
            {
                var path = Path.Combine(t.FullPath, "credentials.json");

                File.WriteAllText(path, ToJson("1", "2"));
                var c = Credentials.Read(path);
                Assert.AreEqual("1", c.accountId);
                Assert.AreEqual("2", c.applicationKey);

                File.WriteAllText(path, "{\"applicationKey\":\"3\"}");
                c = Credentials.Read(path);
                Assert.AreEqual("", c.accountId);
                Assert.AreEqual("3", c.applicationKey);

                File.WriteAllText(path, "{\"accountId\":\"4\"}");
                c = Credentials.Read(path);
                Assert.AreEqual("4", c.accountId);
                Assert.AreEqual("", c.applicationKey);

                File.WriteAllText(path, "Some invalid json");
                Assert.Throws<Exception>(() => Credentials.Read(t.FullPath));
            }
        }

        [Test]
        public void DeleteCredentialsTest()
        {
            using (var t = new TempDirectory())
            {
                var path = Path.Combine(t.FullPath, "credentials.json");

                Credentials.Write("123", "456", path);
                var actualText = File.ReadAllText(path);
                Assert.AreEqual(ToJson("123", "456"), actualText);

                Credentials.Delete(path);
                Assert.IsFalse(File.Exists(path));
            }
        }
    }
}
