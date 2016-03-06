using Common.Logging;
using Common.Logging.Configuration;
using Common.Logging.Simple;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace com.wibblr.b2
{
    /// <summary>
    /// Tests for the B2 HTTP api. These tests are all done against the production B2 service.
    /// There is one test for each api method; these are all marked explicit as they will only
    /// work if the server is in the correct state (e.g. a bucket must already exist before it
    /// can be deleted)
    /// 
    /// In addition there is a test method suitable for automated testing, which calls every api
    /// method and should work all the time (or at least, when the B2 service is up)
    /// </summary>
    [TestFixture]
    public class B2HttpTests
    {
        private string bucketName = "B2HttpTests";
        private Credentials credentials;

        /// <summary>
        /// Read the b2 credentials from the same directory as the test assembly.
        /// Obviously this will not work in a CI server without some more work.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            // Setup logging to write to console
            var properties = new NameValueCollection();
            properties["showDateTime"] = "true";
            properties["level"] = "TRACE";
            properties["showLogName"] = "true";
            properties["dateTimeFormat"] = "yyyy-MM-dd HH:mm:ss.fff";
            LogManager.Adapter = new ConsoleOutLoggerFactoryAdapter(properties);

            credentials = Credentials.Read(new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName);
        }

        [Test]
        public async Task AllMethods()
        {
            var b2http = new B2Http();

            // Authorize
            var ar = await b2http.AuthorizeAccount(credentials.accountId, credentials.applicationKey);
            Assert.IsTrue(ar.apiUrl.EndsWith("backblaze.com"));
            Assert.Greater(ar.authorizationToken.Length, 0);

            // List buckets; delete the test bucket if it exists
            var lbr = await b2http.ListBuckets(ar.apiUrl, ar.authorizationToken, ar.accountId);
            var bucket = lbr.buckets.FirstOrDefault(b => b.bucketName == bucketName);
            if (bucket != null)
            {
                await b2http.DeleteBucket(ar.apiUrl, ar.authorizationToken, ar.accountId, bucket.bucketId);
            }

            // Create bucket
            var cbr = await b2http.CreateBucket(ar.apiUrl, ar.authorizationToken, ar.accountId, bucketName, "allPrivate");

            // Get upload Url


            // Upload file

            // List file names

            // Download file

            // Delete bucket

        }

        //----------------------------------------------------------------------------------------
        // Following tests can only be used only during development and debugging, as they will 
        // only work if the server is set up correctly (e.g. CreateBucket will fail if the 
        // bucket must not already exists)
        //----------------------------------------------------------------------------------------

        /// <summary>
        /// Test the Authorize API method
        /// </summary>
        /// <returns></returns>
        [Test]
        [Explicit]
        public async Task Authorize()
        {
            var b2http = new B2Http();
            var ar = await b2http.AuthorizeAccount(credentials.accountId, credentials.applicationKey);
        }

        /// <summary>
        /// Test the CreateBucket method. The bucket must not already exist (run the DeleteBucket test if 
        /// necessary)
        /// </summary>
        /// <returns></returns>
        [Test]
        [Explicit]
        public async Task CreateBucket()
        {
            var b2http = new B2Http();
            var ar = await b2http.AuthorizeAccount(credentials.accountId, credentials.applicationKey);
            var createBucketResponse = await b2http.CreateBucket(ar.apiUrl, ar.authorizationToken, ar.accountId, bucketName, "allPrivate");
        }

        [Test]
        [Explicit]
        public async Task DeleteBucket()
        {
            var b2http = new B2Http();
            var authorizationResponse = await b2http.AuthorizeAccount(credentials.accountId, credentials.applicationKey);
            var listBucketsResponse = await b2http.ListBuckets(authorizationResponse.apiUrl, authorizationResponse.authorizationToken, credentials.accountId);
            var bucketId = listBucketsResponse.buckets.First(b => b.bucketName == bucketName).bucketId;
            var deleteBucketResponse = await b2http.DeleteBucket(authorizationResponse.apiUrl, authorizationResponse.authorizationToken, credentials.accountId, bucketId);
        }

        /// <summary>
        /// List buckets
        /// </summary>
        [Test]
        [Explicit]
        public async Task ListBuckets()
        {
            var b2http = new B2Http();
            var authorizationResponse = await b2http.AuthorizeAccount(credentials.accountId, credentials.applicationKey);
            var listBucketsResponse = await b2http.ListBuckets(authorizationResponse.apiUrl, authorizationResponse.authorizationToken, credentials.accountId);
        }

        /// <summary>
        /// Upload a file - will only work after calling the test method CreateBucket()
        /// </summary>
        [Test]
        [Explicit]
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
        [Explicit]
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
