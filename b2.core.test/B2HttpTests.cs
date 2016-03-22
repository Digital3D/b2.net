using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


using Common.Logging;
using Common.Logging.Configuration;
using Common.Logging.Simple;

using NUnit.Framework;
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

            credentials = Credentials.Read(Path.Combine(new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName, "credentials.json"));
        }

        /// <summary>
        /// Very simple test that calls all the HTTP methods at least once
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task AllMethods()
        {
            var b2http = new B2Http();

            // Authorize
            var ar = await b2http.AuthorizeAccount(credentials.accountId, credentials.applicationKey);
            Assert.IsTrue(ar.apiUrl.EndsWith("backblaze.com"));
            Assert.Greater(ar.authorizationToken.Length, 0);

            // List buckets; delete all the files in the test bucket, and then the bucket itself, if it exists
            var lbr = await b2http.ListBuckets(ar.apiUrl, ar.authorizationToken, ar.accountId);
            var bucket = lbr.buckets.FirstOrDefault(b => b.bucketName == bucketName);
            if (bucket != null)
            {
                var lfvr = await b2http.ListFileVersions(ar.apiUrl, ar.authorizationToken, bucket.bucketId);
                foreach (var f in lfvr.files)
                    await b2http.DeleteFileVersion(ar.apiUrl, ar.authorizationToken, f.fileName, f.fileId);

                await b2http.DeleteBucket(ar.apiUrl, ar.authorizationToken, ar.accountId, bucket.bucketId);
            }

            // Create bucket
            var cbr = await b2http.CreateBucket(ar.apiUrl, ar.authorizationToken, ar.accountId, bucketName, "allPrivate");

            // List buckets (again)
            var lbr2 = await b2http.ListBuckets(ar.apiUrl, ar.authorizationToken, ar.accountId);
            var bucketId = lbr2.buckets.First(b => b.bucketName == bucketName).bucketId;

            // Get upload Url
            var guur = await b2http.GetUploadUrl(ar.apiUrl, ar.authorizationToken, bucketId);

            // Upload file
            await b2http.UploadFile(
                guur.uploadUrl,
                guur.authorizationToken,
                "hello.txt",
                "text/plain",
                12,
                "430ce34d020724ed75a196dfc2ad67c77772d169",
                new Dictionary<string, string> { { "asdf", "qwer" } },
                new MemoryStream(Encoding.UTF8.GetBytes("hello world!")));

            // Upload another version of file
            await b2http.UploadFile(
                guur.uploadUrl,
                guur.authorizationToken,
                "hello.txt",
                "text/plain",
                19,
                "81b716c0e4e892836ff2ba9f98c7f00aac0c7656",
                new Dictionary<string, string> { { "zxcv", "uiop" } },
                new MemoryStream(Encoding.UTF8.GetBytes("hello again, world!")));

            // List file names
            var lfnr = await b2http.ListFileNames(ar.apiUrl, ar.authorizationToken, bucketId);
            Assert.AreEqual(1, lfnr.files.Count);
            Assert.AreEqual("upload", lfnr.files.First().action);
            Assert.AreEqual(19, lfnr.files.First().size);

            // List file versions. Sort order is (name, uploadtime DESC)
            var lfv2 = await b2http.ListFileVersions(ar.apiUrl, ar.authorizationToken, bucketId);
            Assert.AreEqual(2, lfv2.files.Count);
            Assert.AreEqual(19, lfv2.files.First().size);
            Assert.AreEqual(12, lfv2.files.Last().size);

            // GetFileInfo
            var gfi = await b2http.GetFileInfo(ar.apiUrl, ar.authorizationToken, lfv2.files.First().fileId);
            Assert.AreEqual(19, gfi.contentLength);

            // Download file by ID

            // Download file by name

            // Hide File

            // UpdateBucket

            // Delete bucket
            await b2http.DeleteBucket(ar.apiUrl, ar.authorizationToken, ar.accountId, bucket.bucketId);

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
        /// Create bucket. The bucket must not already exist (run the DeleteBucket test if 
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

        /// <summary>
        /// Delete bucket. The bucket must already exist and be empty
        /// </summary>
        /// <returns></returns>
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
        /// Delete one particular version of a file
        /// </summary>
        /// <returns></returns>
        public async Task DeleteFileVersion()
        {
            var b2http = new B2Http();
            var authorizationResponse = await b2http.AuthorizeAccount(credentials.accountId, credentials.applicationKey);
            var listBucketsResponse = await b2http.ListBuckets(authorizationResponse.apiUrl, authorizationResponse.authorizationToken, credentials.accountId);
            var bucketId = listBucketsResponse.buckets.First(b => b.bucketName == bucketName).bucketId;
            var listFileVersionsResponse = await b2http.ListFileVersions(authorizationResponse.apiUrl, authorizationResponse.authorizationToken, bucketId);
        }

        public async Task DeleteFileById()
        {
            throw new NotImplementedException();
        }

        public async Task DeleteFileByName()
        {
            throw new NotImplementedException();
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

        public async Task DownloadFileByName()
        {
            throw new NotImplementedException();
        }

        public async Task GetFileInfo()
        {
            throw new NotImplementedException();
        }

        public async Task GetUploadUrl()
        {
            throw new NotImplementedException();
        }

        public async Task HideFile()
        {
            throw new NotImplementedException();
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


        public async Task ListFileNames()
        {
            throw new NotImplementedException();
        }

        [Test]
        public async Task ListFileVersions()
        {
            var b2http = new B2Http();

            var authorizationResponse = await b2http.AuthorizeAccount(credentials.accountId, credentials.applicationKey);
            var listBucketsResponse = await b2http.ListBuckets(authorizationResponse.apiUrl, authorizationResponse.authorizationToken, credentials.accountId);
            var bucketId = listBucketsResponse.buckets.First(b => b.bucketName == bucketName).bucketId;
            var listFileVersionsResponse = await b2http.ListFileVersions(authorizationResponse.apiUrl, authorizationResponse.authorizationToken, bucketId);
            foreach (var f in listFileVersionsResponse.files)
            {
                Console.WriteLine($"{f.fileName} {f.fileId}");
            }

        }

        public async Task UpdateBucket()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Upload a file - will only work if the bucket already exists
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
    }
}
