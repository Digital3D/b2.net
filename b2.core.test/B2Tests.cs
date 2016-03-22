using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using NUnit.Framework;

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
        [Test]
        public async Task B2UploadFile()
        {
            var path = Path.Combine(new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName, "credentials.json");
            credentials = Credentials.Read(path);
            var b2 = new B2();
            await b2.Login(credentials.accountId, credentials.applicationKey, bucketName).ConfigureAwait(false);
            var sourcePath = Path.Combine(new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName, "url-encoding.json");
            var destinationPath = "a/b/c/test.json";
            await b2.UploadFile(sourcePath, destinationPath).ConfigureAwait(false);
        }
    }
}
