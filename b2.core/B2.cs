using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;

using com.wibblr.utils;

namespace com.wibblr.b2
{
    /// <summary>
    /// A high level, stateful wrapper around the backblaze B2 cloud storage service.
    /// </summary>
    public class B2
    {
        private B2Http b2api = new B2Http();

        public string AccountId { get; private set; }
        public string ApiUrl { get; private set; }
        public string DownloadUrl { get; private set; }
        public string AuthorizationToken { get; private set; }
        public string BucketId { get; private set; }
        
        public async Task Login(string accountId, string applicationKey, string bucketName)
        {
            var res = await b2api.AuthorizeAccount(accountId, applicationKey).ConfigureAwait(false);
            AccountId   = res.accountId;
            ApiUrl      = res.apiUrl;
            DownloadUrl = res.downloadUrl;
            AuthorizationToken = res.authorizationToken;

            var listBucketsResponse = await b2api.ListBuckets(ApiUrl, AuthorizationToken, AccountId).ConfigureAwait(false);

            var bucket = listBucketsResponse.buckets.FirstOrDefault(b => b.bucketName == bucketName);

            if (bucket != null)
            {
                BucketId = bucket.bucketId;
            }
            else
            {
                var createBucketResponse = await b2api.CreateBucket(ApiUrl, AuthorizationToken, AccountId, bucketName, "allPrivate").ConfigureAwait(false);
                BucketId = createBucketResponse.bucketId;
            }
        }

        public async Task UploadFile(string sourcePath, string destinationPath, string contentType = "text/plain", IProgress<StreamProgress> checksumProgress = null, IProgress<StreamProgress> uploadProgress = null)
        {
            // Ensure the file cannot be altered whilst being uploaded.
            using (var fs = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var qs = new ProgressReportingStream(fs))
            {
                // The B2 HTTP api requires the SHA1 checksum before uploading; this means the file must be
                // read twice, doh!
                var e = new B2UrlEncoder();

                var shaTask = Task.Run(() => SHA1.Create().ComputeHash(qs));
                if (checksumProgress != null)
                    while (await Task.WhenAny(shaTask, Task.Delay(100)) != shaTask)
                        checksumProgress.Report(qs.Progress());

                await shaTask;
                if (checksumProgress != null)
                    checksumProgress.Report(qs.Progress());

                var sha1hex = new StringBuilder(40);

                foreach (var b in shaTask.Result)
                    sha1hex.AppendFormat("{0:X2}", b);

                qs.Position = 0;

                var getUploadUrlResponse = await b2api.GetUploadUrl(ApiUrl, AuthorizationToken, BucketId).ConfigureAwait(false);

                var url = getUploadUrlResponse.uploadUrl;

                var attributes = new Dictionary<string, string>();
                var fileInfo = new FileInfo(sourcePath);
                attributes["last_modified_millis"] = fileInfo.LastWriteTimeUtc.ToUnixTimeMillis().ToString();

                var uploadFileTask = b2api.UploadFile(url, getUploadUrlResponse.authorizationToken, destinationPath, contentType, fileInfo.Length, sha1hex.ToString(), attributes, qs);
                if (uploadProgress != null)
                    while (await Task.WhenAny(uploadFileTask, Task.Delay(100)) != uploadFileTask)
                        uploadProgress.Report(qs.Progress());

                await uploadFileTask;
                if (uploadProgress != null)
                    uploadProgress.Report(qs.Progress());
            }
        }
    }
}
