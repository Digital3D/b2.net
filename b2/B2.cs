﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;

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

        public async Task UploadFile(string sourcePath, string destinationPath, string contentType = "text/plain")
        {
            // Ensure the file cannot be altered whilst being uploaded.
            using (var fs = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // The B2 HTTP api requires the SHA1 checksum before uploading; this means the file must be
                // read twice, doh!
                var e = new B2UrlEncoder();

                var sha1bytes = SHA1.Create().ComputeHash(fs);
                var sha1hex = new StringBuilder(40);

                foreach (var b in sha1bytes)
                    sha1hex.Append(b.ToString("X2"));

                fs.Position = 0;

                var getUploadUrlResponse = await b2api.GetUploadUrl(ApiUrl, AuthorizationToken, BucketId).ConfigureAwait(false);

                var url = getUploadUrlResponse.uploadUrl;

                var attributes = new Dictionary<string, string>();
                var fileInfo = new FileInfo(sourcePath);
                attributes["last_modified_millis"] = fileInfo.LastWriteTimeUtc.ToUnixTimeMillis().ToString();

                await b2api.UploadFile(url, getUploadUrlResponse.authorizationToken, destinationPath, contentType, fileInfo.Length, sha1hex.ToString(), attributes, fs);
            }
        }
    }
}