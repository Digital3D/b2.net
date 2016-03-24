using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Common.Logging;
using Common.Logging.Configuration;
using Common.Logging.Simple;

using NUnit.Framework;

using com.wibblr.utils;

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
        /// <summary>
        /// Class that creates a new bucket for testing, and deletes it when finished.
        /// </summary>
        class TestBucket : IDisposable
        {
            public B2Http api = new B2Http();
            public AuthorizeAccountResponse authorizeAccountResponse { get; private set; }
            public CreateBucketResponse createBucketResponse { get; private set; }

            public TestBucket()
            {
                authorizeAccountResponse = api.AuthorizeAccount(credentials.accountId, credentials.applicationKey).Result;
                createBucketResponse = api.CreateBucket(authorizeAccountResponse.apiUrl, authorizeAccountResponse.authorizationToken, authorizeAccountResponse.accountId, RandomString.Next(16), "allPrivate").Result;
            }

            public string ApiUrl { get { return authorizeAccountResponse.apiUrl; } }
            public string AuthToken { get { return authorizeAccountResponse.authorizationToken; } }
            public string AccountId { get { return authorizeAccountResponse.accountId; } }
            public string DownloadUrl { get { return authorizeAccountResponse.downloadUrl; } }
            public string BucketId { get { return createBucketResponse.bucketId; } }

            /// <summary>
            /// Delete the bucket. Swallow any exceptions, failure to delete the bucket is not itself a test failure,
            /// and might hide an actual problem.
            /// </summary>
            public void Dispose()
            {
                try { api.DeleteBucket(ApiUrl, AuthToken, AccountId, BucketId).Wait(); } catch (Exception) { }
            }

            public async Task<DeleteFileVersionResponse> DeleteFileVersion(string fileName, string fileId)
                => await api.DeleteFileVersion(ApiUrl, AuthToken, fileName, fileId);

            public async Task<B2File> DownloadFileById(string fileId, long? rangeLower = null, long? rangeUpper = null)
               => await api.DownloadFileById(DownloadUrl, AuthToken, fileId, rangeLower, rangeUpper);

            public async Task<B2File> DownloadFileByName(string fileName, long? rangeLower = null, long? rangeUpper = null)
                => await api.DownloadFileByName(DownloadUrl, AuthToken, createBucketResponse.bucketName, fileName, rangeLower, rangeUpper);

            public async Task<GetFileInfoResponse> GetFileInfo(string fileId)
                => await api.GetFileInfo(ApiUrl, AuthToken, fileId);

            public async Task<GetUploadUrlResponse> GetUploadUrl()
                => await api.GetUploadUrl(ApiUrl, AuthToken, BucketId);

            public async Task<HideFileResponse> HideFile(string fileName)
                => await api.HideFile(ApiUrl, AuthToken, BucketId, fileName);

            public async Task<ListBucketsResponse> ListBuckets()
                => await api.ListBuckets(ApiUrl, AuthToken, AccountId);

            public async Task<ListFileVersionsResponse> ListFileVersions(string startFileName = null, string startFileId = null)
                => await api.ListFileVersions(ApiUrl, AuthToken, BucketId, startFileName, startFileId, 2);

            public async Task<ListFileNamesResponse> ListFileNames(string startFileName = null)
                => await api.ListFileNames(ApiUrl, AuthToken, BucketId, startFileName, 2);

            public async Task<UpdateBucketResponse> UpdateBucket(string bucketType)
                => await api.UpdateBucket(ApiUrl, AuthToken, AccountId, BucketId, bucketType);

            public async Task<UploadFileResponse> UploadFile(string fileName = null, string content = null, string sha1 = null)
            {
                var u = await GetUploadUrl();

                if (fileName == null)
                    fileName = RandomString.Next(10);

                if (content == null)
                    content = RandomString.Next(10);

                if (sha1 == null)
                    sha1 = content.Sha1();

                var contentBytes = Encoding.UTF8.GetBytes(content);

                return await api.UploadFile(
                    u.uploadUrl,
                    u.authorizationToken,
                    fileName,
                    "application/octet-stream",
                    contentBytes.Length,
                    sha1,
                    new Dictionary<string, string> { { "asdf", "qwer" } },
                    new MemoryStream(contentBytes));
            }
        }

        static Credentials credentials = Credentials.Read();

        static async Task<AuthorizeAccountResponse> AuthorizeAccount(B2Http b2http)
            => await b2http.AuthorizeAccount(credentials.accountId, credentials.applicationKey);

        //static async Task<DeleteBucketResponse> DeleteBucket(B2Http b2http, AuthorizeAccountResponse a, string bucketId)
        //    => await b2http.DeleteBucket(a.apiUrl, a.authorizationToken, credentials.accountId, bucketId);

        //static async Task<CreateBucketResponse> CreateBucket(B2Http b2http, AuthorizeAccountResponse a, string bucketName)
        //    => await b2http.CreateBucket(a.apiUrl, a.authorizationToken, a.accountId, bucketName, "allPrivate");

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
        }

        /// <summary>
        /// Test the Authorize API method
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task AuthorizeAccount()
        {
            var a = await AuthorizeAccount(new B2Http());
            Assert.IsTrue(a.apiUrl.EndsWith("backblaze.com"));
            Assert.Greater(a.authorizationToken.Length, 0);
        }

        /// <summary>
        /// Create bucket.
        /// </summary>
        /// <returns></returns>
        [Test]
        public void CreateBucket()
        {
            using (var b = new TestBucket())
                Assert.IsNotNull(b.BucketId);
        }

        /// <summary>
        /// Delete bucket.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task DeleteBucket()
        {
            var b2http = new B2Http();
            var bucketName = RandomString.Next(16);
            var a = await AuthorizeAccount(b2http);
            var c = await b2http.CreateBucket(a.apiUrl, a.authorizationToken, a.accountId, bucketName, "allPrivate");
            Assert.IsNotNull(c.bucketId);
            await b2http.DeleteBucket(a.apiUrl, a.authorizationToken, a.accountId, c.bucketId);
        }

        /// <summary>
        /// Delete one particular version of a file
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task DeleteFileVersion()
        {
            using (var b = new TestBucket())
            {
                var uploadTasks = new[] { b.UploadFile(), b.UploadFile(), b.UploadFile(), b.UploadFile() };
                await Task.WhenAll(uploadTasks);

                var deleteTasks = uploadTasks.Select(u => b.DeleteFileVersion(u.Result.fileName, u.Result.fileId)).ToArray();
                await Task.WhenAll(deleteTasks);
            }
        }

        /// <summary>
        /// Download a file by using the file ID
        /// </summary>
        [Test]
        public async Task DownloadFileById()
        {
            using (var b = new TestBucket())
            {
                var uploadTasks = new[] { b.UploadFile("file0", "content0"), b.UploadFile("file1", "content1"), b.UploadFile("file2", "content2") };
                await Task.WhenAll(uploadTasks);

                var downloadTasks = uploadTasks.Select(u => b.DownloadFileById(u.Result.fileId)).ToArray();
                await Task.WhenAll(downloadTasks);

                for (int i = 0; i < downloadTasks.Length; i++)
                    Assert.AreEqual($"content{i}", new StreamReader(downloadTasks[i].Result.content).ReadToEnd());

                var deleteTasks = uploadTasks.Select(u => b.DeleteFileVersion(u.Result.fileName, u.Result.fileId)).ToArray();
                await Task.WhenAll(deleteTasks);
            }
        }

        /// <summary>
        /// Download part of a file by using the file ID
        /// </summary>
        [Test]
        public async Task DownloadFileRangeById()
        {
            using (var b = new TestBucket())
            {
                var u = await b.UploadFile("file0", "content0");
                var d = await b.DownloadFileById(u.fileId, 0, 3);
                Assert.AreEqual($"cont", new StreamReader(d.content).ReadToEnd());
                await b.DeleteFileVersion(u.fileName, u.fileId);
            }
        }

        /// <summary>
        /// Download a file by using the bucket name and file name
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task DownloadFileByName()
        {
            using (var b = new TestBucket())
            {
                var uploadTasks = new[] { b.UploadFile("file0", "content0"), b.UploadFile("file1", "content1"), b.UploadFile("file2", "content2") };
                await Task.WhenAll(uploadTasks);

                var downloadTasks = uploadTasks.Select(u => b.DownloadFileByName(u.Result.fileName)).ToArray();
                await Task.WhenAll(downloadTasks);

                for (int i = 0; i < downloadTasks.Length; i++)
                    Assert.AreEqual($"content{i}", new StreamReader(downloadTasks[i].Result.content).ReadToEnd());

                var deleteTasks = uploadTasks.Select(u => b.DeleteFileVersion(u.Result.fileName, u.Result.fileId)).ToArray();
                await Task.WhenAll(deleteTasks);
            }
        }

        /// <summary>
        /// Download part of a file by using the bucket name and file name
        /// </summary>
        [Test]
        public async Task DownloadFileRangeByName()
        {
            using (var b = new TestBucket())
            {
                var u = await b.UploadFile("file0", "content0");
                var d = await b.DownloadFileByName(u.fileName, 3, null);
                Assert.AreEqual($"tent0", new StreamReader(d.content).ReadToEnd());
                await b.DeleteFileVersion(u.fileName, u.fileId);
            }
        }

        /// <summary>
        /// Call the GetFileInfo api
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task GetFileInfo()
        {
            using (var b = new TestBucket())
            {
                var r = await b.UploadFile();
                var f = await b.GetFileInfo(r.fileId);
                Assert.IsNotNull(f.fileName);
            }
        }

        /// <summary>
        /// Call the GetFileInfo api
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task GetUploadUrl()
        {
            using (var b = new TestBucket())
            {
                var r = await b.GetUploadUrl();
                Assert.IsNotNull(r.uploadUrl);
            }
        }

        /// <summary>
        /// Call the GetFileInfo api
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task HideFile()
        {
            using (var b = new TestBucket())
            {
                var u0 = await b.UploadFile("file", "content0");
                var u1 = await b.UploadFile("file", "content1");
                var v = await b.ListFileVersions();

                Assert.AreEqual(2, v.files.Count);
                Assert.AreEqual(u0.fileName, v.files[0].fileName);
                Assert.AreEqual(u1.fileName, v.files[1].fileName);

                var h = await b.HideFile(u0.fileName);

                v = await b.ListFileVersions();

                Assert.AreEqual(2, v.files.Count);
                Assert.AreEqual("hide", v.files[0].action);
                Assert.AreEqual("upload", v.files[1].action);
            }
        }

        /// <summary>
        /// List buckets
        /// </summary>
        [Test]
        public async Task ListBuckets()
        {
            using (var b = new TestBucket())
            {
                var r = await b.ListBuckets();
                Assert.Contains(b.BucketId, r.buckets.Select(x => x.bucketId).ToArray());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Test]
        public async Task ListFileNames()
        {
            using (var b = new TestBucket())
            {
                var uploadTasksA = new[] { b.UploadFile("file2", "contentA2"), b.UploadFile("file1", "contentA1"), b.UploadFile("file0", "contentA0") };
                await Task.WhenAll(uploadTasksA);

                var uploadTasksB = new[] { b.UploadFile("file0", "contentB0"), b.UploadFile("file1", "contentB1"), b.UploadFile("file2", "contentB2") };
                await Task.WhenAll(uploadTasksA);

                var r = await b.ListFileNames();
                Assert.AreEqual(2, r.files.Count);
                r = await b.ListFileNames(r.nextFileName);
                Assert.AreEqual(1, r.files.Count);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Test]
        public async Task ListFileVersions()
        {
            using (var b = new TestBucket())
            {
                var uploadTasksA = new[] { b.UploadFile("file2", "contentA2"), b.UploadFile("file1", "contentA1"), b.UploadFile("file0", "contentA0") };
                await Task.WhenAll(uploadTasksA);

                var uploadTasksB = new[] { b.UploadFile("file0", "contentB0"), b.UploadFile("file1", "contentB1"), b.UploadFile("file2", "contentB2") };
                await Task.WhenAll(uploadTasksA);

                // ListFileVersions returns files in batches. The batch size is set to 2 in TestBucket.
                // Do not specify a start file name or id. Should be returned in alphabetical name order and then reverse upload time order
                var r = await b.ListFileVersions(null, null);
                Assert.AreEqual(2, r.files.Count);
                Assert.AreEqual("file0", r.files[0].fileName);
                Assert.AreEqual("file0", r.files[1].fileName);

                r = await b.ListFileVersions(r.nextFileName, r.nextFileId);
                Assert.AreEqual("file1", r.files[0].fileName);
                Assert.AreEqual("file1", r.files[1].fileName);

                r = await b.ListFileVersions(r.nextFileName, r.nextFileId);
                Assert.AreEqual("file2", r.files[0].fileName);
                Assert.AreEqual("file2", r.files[1].fileName);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Test]
        public async Task UpdateBucket()
        {
            using (var t = new TestBucket())
            {
                Assert.AreEqual("allPrivate", t.createBucketResponse.bucketType);
                var u = await t.UpdateBucket("allPublic");
                var b = await t.ListBuckets();
                Assert.AreEqual("allPublic", b.buckets.First(x => x.bucketId == t.BucketId).bucketType);
            }
        }

        /// <summary>
        /// Upload a file
        /// </summary>
        [Test]
        public async Task UploadFile()
        {
            using (var t = new TestBucket())
                await t.UploadFile("hello.txt", "hello world!", "430ce34d020724ed75a196dfc2ad67c77772d169");
        }
    }
}
