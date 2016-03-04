using NUnit.Framework;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace com.wibblr.b2
{
    [TestFixture]
    public class B2Tests
    {
        private string bucketName = "B2Tests";
        private Credentials credentials;

        /// <summary>
        /// Read the b2 credentials from the same directory as the test assembly.
        /// Obviously this will not work in a CI server without some more work.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            credentials = Credentials.Read(new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName);
        }

        [Test]
        public async Task B2UploadFile()
        {
            var b2 = new B2();
            await b2.Login(credentials.accountId, credentials.applicationKey, bucketName).ConfigureAwait(false);
            var sourcePath = Path.Combine(new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName, "url-encoding.json");
            var destinationPath = "a/b/c/test.json";
            await b2.UploadFile(sourcePath, destinationPath).ConfigureAwait(false);
        }
    }
}
