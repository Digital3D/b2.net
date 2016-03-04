using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace com.wibblr.b2
{
    [TestFixture]
    public class b2httpTests
    {
        private string bucketName = "asdjkhaskjdfhajksdfhakjsdfh";
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
        public async Task Authorize()
        {
            var b2http = new B2Http();
            var authorizationResponse = await b2http.AuthorizeAccount(credentials.accountId, credentials.applicationKey);

            Assert.IsTrue(authorizationResponse.apiUrl.EndsWith("backblaze.com"));
            Assert.Greater(authorizationResponse.authorizationToken.Length, 0);
        }

        [Test]
        public async Task CreateBucket()
        {
            var b2http = new B2Http();
            var authorizationResponse = await b2http.AuthorizeAccount(credentials.accountId, credentials.applicationKey);
            var createBucketResponse = await b2http.CreateBucket(authorizationResponse.apiUrl, authorizationResponse.authorizationToken, credentials.accountId, bucketName, "allPrivate");
        }

        [Test]
        public async Task DeleteBucket()
        {
            var b2http = new B2Http();
            var authorizationResponse = await b2http.AuthorizeAccount(credentials.accountId, credentials.applicationKey);
            var listBucketsResponse = await b2http.ListBuckets(authorizationResponse.apiUrl, authorizationResponse.authorizationToken, credentials.accountId);
            var bucketId = listBucketsResponse.buckets.First(b => b.bucketName == bucketName).bucketId;
            var deleteBucketResponse = await b2http.DeleteBucket(authorizationResponse.apiUrl, authorizationResponse.authorizationToken, credentials.accountId, bucketId);
        }

        /// <summary>
        /// List buckets - will only work after calling the test method CreateBucket()
        /// </summary>
        [Test]
        public async Task ListBuckets()
        {
            var b2http = new B2Http();
            var authorizationResponse = await b2http.AuthorizeAccount(credentials.accountId, credentials.applicationKey);
            var listBucketsResponse = await b2http.ListBuckets(authorizationResponse.apiUrl, authorizationResponse.authorizationToken, credentials.accountId);
            var bucket = listBucketsResponse.buckets.FirstOrDefault(b => b.bucketName == bucketName);
            Assert.IsNotNull(bucket);
        }

        /// <summary>
        /// Upload a file - will only work after calling the test method CreateBucket()
        /// </summary>
        [Test]
        public async Task UploadFile()
        {
            var b2http = new B2Http();
            var authorizationResponse = await b2http.AuthorizeAccount(credentials.accountId, credentials.applicationKey);
            var listBucketsResponse = await b2http.ListBuckets(authorizationResponse.apiUrl, authorizationResponse.authorizationToken, credentials.accountId);
            var bucketId = listBucketsResponse.buckets.First(b => b.bucketName == bucketName).bucketId;
            var getUploadUrlResponse = await b2http.GetUploadUrl(authorizationResponse.apiUrl, authorizationResponse.authorizationToken, bucketId);

            var uploadFileResponse = await b2http.UploadFile(
                getUploadUrlResponse.uploadUrl,
                getUploadUrlResponse.authorizationToken,
                "hello.txt",
                "text/plain",
                12,
                "430ce34d020724ed75a196dfc2ad67c77772d169",
                new Dictionary<string, string> { { "asdf", "qwer" } },
                new MemoryStream(Encoding.UTF8.GetBytes("hello world!")));
        }

        /// <summary>
        /// Download a file - will only work after calling the test methods CreateBucket() and UploadFile()
        /// </summary>
        [Test]
        public async Task DownloadFileById()
        {
            var b2http = new B2Http();

            var authorizationResponse = await b2http.AuthorizeAccount(credentials.accountId, credentials.applicationKey);
            var listBucketsResponse = await b2http.ListBuckets(authorizationResponse.apiUrl, authorizationResponse.authorizationToken, credentials.accountId);
            var bucketId = listBucketsResponse.buckets.First(b => b.bucketName == bucketName).bucketId;
            var fileNames = await b2http.ListFileNames(authorizationResponse.apiUrl, authorizationResponse.authorizationToken, bucketId);
            var fileId = fileNames.files.First(f => f.fileName == "hello.txt").fileId;

            var file = await b2http.DownloadFileById(authorizationResponse.downloadUrl, authorizationResponse.authorizationToken, fileId, null, null);

            Assert.AreEqual(12, file.length);

            using (var r = new StreamReader(file.content))
            {
                var text = r.ReadToEnd();
                Assert.AreEqual("hello world!", text);
            }

            Assert.AreEqual("qwer", file.attributes["asdf"]);              
        }
    }
}
