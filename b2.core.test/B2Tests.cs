using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using NUnit.Framework;

using com.wibblr.utils;

namespace com.wibblr.b2
{
    [TestFixture]
    public class B2Tests
    {
        private string bucketName = "B2Tests";
        private Credentials credentials;

        /// <summary>
        /// Upload a file
        /// </summary>
        [Test]
        public async Task B2UploadFile()
        {
            credentials = Credentials.Read();
            var b2 = new B2();
            await b2.Login(credentials.accountId, credentials.applicationKey, bucketName).ConfigureAwait(false);
            using (var t = new TempDirectory())
            {
                t.CreateFile("test.txt");
                var sourcePath = Path.Combine(t.FullPath, "test.txt");
                var destinationPath = "a/b/c/test.txt";

                await b2.UploadFile(sourcePath, destinationPath);
            }
        }
    }
}
