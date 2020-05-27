using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
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

        public async Task UploadFile(string sourcePath, string destinationPath, string contentType = "application/octet-stream", IProgress<StreamProgress> checksumProgress = null, IProgress<StreamProgress> uploadProgress = null)
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

        public delegate void ProgressHandler(long totalBytesSent, long localFileSize, string partNumber);

        public event ProgressHandler OnLargeFileUploadProgress;
        public async Task<ArrayList> UploadPartOfFile(string fullNameOfFile, string uploadUrl, string authorizationToken)
        {
            string json = null;
            FileInfo fileInfo = new FileInfo(fullNameOfFile);
            long localFileSize = fileInfo.Length;
            long totalBytesSent = 0;
            long bytesSentForPart = 100 * (1000 * 1000);
            long minimumPartSize = bytesSentForPart;
            ArrayList partSha1Array = new ArrayList();
            byte[] data = new byte[100 * (1000 * 1000)];
            int partNo = 1;
            while (totalBytesSent < localFileSize)
            {
                if ((localFileSize - totalBytesSent) < minimumPartSize)
                {
                    bytesSentForPart = (localFileSize - totalBytesSent);
                }

                // Generate SHA1
                FileStream f = File.OpenRead(fullNameOfFile);
                f.Seek(totalBytesSent, SeekOrigin.Begin);
                f.Read(data, 0, (int)bytesSentForPart);
                SHA1 sha1 = SHA1.Create();
                byte[] hashData = sha1.ComputeHash(data, 0, (int)bytesSentForPart);
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashData)
                {
                    sb.Append(b.ToString("x2"));
                }
                f.Close();
                partSha1Array.Add(sb.ToString());

                HttpWebRequest uploadPartRequest = (HttpWebRequest)WebRequest.Create(uploadUrl);
                uploadPartRequest.Method = "POST";
                uploadPartRequest.Headers.Add("Authorization", authorizationToken);
                uploadPartRequest.Headers.Add("X-Bz-Part-Number", partNo.ToString());
                uploadPartRequest.Headers.Add("X-Bz-Content-Sha1", (String)partSha1Array[(partNo - 1)]);
                uploadPartRequest.ContentType = "application/json; charset=utf-8";
                uploadPartRequest.ContentLength = bytesSentForPart;
                using (Stream stream = uploadPartRequest.GetRequestStream())
                {
                    stream.Write(data, 0, (int)bytesSentForPart);
                    stream.Close();
                }
                HttpWebResponse uploadPartResponse = null;
                try
                {
                    uploadPartResponse = (HttpWebResponse) await uploadPartRequest.GetResponseAsync();

                    OnLargeFileUploadProgress?.Invoke(totalBytesSent + bytesSentForPart, localFileSize, sb.ToString());
                }
                catch (WebException e)
                {
                    using (HttpWebResponse errorResponse = (HttpWebResponse)e.Response)
                    {
                        using (StreamReader reader = new StreamReader(errorResponse.GetResponseStream()))
                        {
                            json = reader.ReadToEnd();
                        }
                    }
                }
                catch (Exception e)
                {
                    json = e.Message;
                    while (e.InnerException != null)
                    {
                        json += "\r\n" + e.InnerException.Message;
                        e = e.InnerException;
                    }
                }
                finally
                {
                    uploadPartResponse.Close();
                }

                partNo++;
                totalBytesSent = totalBytesSent + bytesSentForPart;
            }

            return partSha1Array;
        }
        public async Task<string> UploadLargeFile(string bucketId, string fileName, string apiUrl, string authorizationToken, string contentType = "application/octet-stream")
        {
            string json = null;
            // Setup JSON to post.
            string startLargeFileJsonStr = "{\"bucketId\":\"" + bucketId + "\",\"fileName\":\"" + fileName + "\",\"contentType\":\"" + contentType + "\"}";
            byte[] jsonData = Encoding.UTF8.GetBytes(startLargeFileJsonStr);

            // Send over the wire
            HttpWebRequest startLargeFileRequest = (HttpWebRequest)WebRequest.Create(apiUrl + "/b2api/v2/b2_start_large_file");
            startLargeFileRequest.Method = "POST";
            startLargeFileRequest.Headers.Add("Authorization", authorizationToken);
            startLargeFileRequest.ContentType = "application/json; charset=utf-8";
            startLargeFileRequest.ContentLength = jsonData.Length;
            using (Stream stream = startLargeFileRequest.GetRequestStream())
            {
                stream.Write(jsonData, 0, jsonData.Length);
                stream.Close();
            }

            // Handle the response and print the json
            try
            {

                HttpWebResponse startLargeFileResponse = (HttpWebResponse) await startLargeFileRequest.GetResponseAsync();
                using (StreamReader responseReader = new StreamReader(startLargeFileResponse.GetResponseStream()))
                {
                    json = responseReader.ReadToEnd();
                }
                startLargeFileResponse.Close();

            }
            catch (WebException e)
            {
                using (HttpWebResponse errorResponse = (HttpWebResponse) e.Response)
                {
                    using (StreamReader reader = new StreamReader(errorResponse.GetResponseStream()))
                    {
                        json = reader.ReadToEnd();
                    }
                }
            }
            catch (Exception e)
            {
                json = e.Message;
                while (e.InnerException != null)
                {
                    json += "\r\n" + e.InnerException.Message;
                    e = e.InnerException;
                }
            }

            return json;
        }

        public async Task<string> UploadLargeFilePartUrl(string fileId, string apiUrl, string authorizationToken)
        {
            string json = null;
            // Get Upload URL
            String getUploadUrlJsonStr = "{\"fileId\":\"" + fileId + "\"}";
            byte[] getUloadUrlJsonData = Encoding.UTF8.GetBytes(getUploadUrlJsonStr);
            HttpWebRequest getUploadUrlRequest = (HttpWebRequest)WebRequest.Create(apiUrl + "/b2api/v2/b2_get_upload_part_url");
            getUploadUrlRequest.Method = "POST";
            getUploadUrlRequest.Headers.Add("Authorization", authorizationToken);
            getUploadUrlRequest.ContentType = "application/json; charset=utf-8";
            getUploadUrlRequest.ContentLength = getUloadUrlJsonData.Length;
            using (Stream stream = getUploadUrlRequest.GetRequestStream())
            {
                stream.Write(getUloadUrlJsonData, 0, getUloadUrlJsonData.Length);
                stream.Close();
            }

            // Handle the response and print the json
            try
            {
                HttpWebResponse getUploadUrlResponse = (HttpWebResponse) await getUploadUrlRequest.GetResponseAsync();
                using (StreamReader responseReader = new StreamReader(getUploadUrlResponse.GetResponseStream()))
                {
                    json = responseReader.ReadToEnd();
                }
                getUploadUrlResponse.Close();
            }
            catch (WebException e)
            {
                using (HttpWebResponse errorResponse = (HttpWebResponse)e.Response)
                {
                    using (StreamReader reader = new StreamReader(errorResponse.GetResponseStream()))
                    {
                        json = reader.ReadToEnd();
                    }
                }
            }
            catch (Exception e)
            {
                json = e.Message;
                while (e.InnerException != null)
                {
                    json += "\r\n" + e.InnerException.Message;
                    e = e.InnerException;
                }
            }

            return json;
        }

        public async Task<string> LargeFileUploadFinished(string fileId, string apiUrl, string authorizationToken, ArrayList partSha1Array)
        {
            string json = null;

            // Create a request object and copy it to the memory stream.
            B2FinishLargeFileRequest finishLargeFileData = new B2FinishLargeFileRequest();
            finishLargeFileData.fileId = fileId;
            finishLargeFileData.partSha1Array = partSha1Array;
            MemoryStream finishLargeFileMemStream = new MemoryStream();
            DataContractJsonSerializer finishLargeFileSerializer = new DataContractJsonSerializer(typeof(B2FinishLargeFileRequest));
            finishLargeFileSerializer.WriteObject(finishLargeFileMemStream, finishLargeFileData);

            HttpWebRequest finishLargeFileRequest = (HttpWebRequest)WebRequest.Create(apiUrl + "/b2api/v2/b2_finish_large_file");
            finishLargeFileRequest.Method = "POST";
            finishLargeFileRequest.Headers.Add("Authorization", authorizationToken);
            finishLargeFileRequest.ContentType = "application/json; charset=utf-8";
            finishLargeFileRequest.ContentLength = finishLargeFileMemStream.Length;
            finishLargeFileMemStream.WriteTo(finishLargeFileRequest.GetRequestStream());
            HttpWebResponse finishLargeFileResponse = null;
            try
            {
                finishLargeFileResponse = (HttpWebResponse)await finishLargeFileRequest.GetResponseAsync();
            }
            catch (WebException e)
            {
                using (HttpWebResponse errorResponse = (HttpWebResponse)e.Response)
                {
                    using (StreamReader reader = new StreamReader(errorResponse.GetResponseStream()))
                    {
                        json = reader.ReadToEnd();
                    }
                }
            }
            catch (Exception e)
            {
                json = e.Message;
                while (e.InnerException != null)
                {
                    json += "\r\n" + e.InnerException.Message;
                    e = e.InnerException;
                }
            }

            return json;


        }
    }

    // Class used with Json Serialization
    [DataContract]
    class B2FinishLargeFileRequest
    {
        [DataMember]
        public String fileId;
        [DataMember]
        public ArrayList partSha1Array;
    }
}
