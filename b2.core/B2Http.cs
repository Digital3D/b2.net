using Common.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace com.wibblr.b2
{
    /// <summary>
    /// Class used to return downloaded file data
    /// </summary>
    public class B2File
    {
        public Stream content;
        public string id;
        public string name;
        public long length;
        public string contentType;
        public string sha1;
        public Dictionary<string, string> attributes = new Dictionary<string, string>();

        public B2File(Stream responseStream, HttpHeaders headers, HttpContentHeaders contentHeaders)
        {
            content = responseStream;
            length = Convert.ToInt64(contentHeaders.ContentLength);
            contentType = contentHeaders.ContentType.MediaType;

            foreach (var h in headers.Select(x => new { key = x.Key.ToLower(), value = x.Value.First() }))
            {
                if (h.key == "x-bz-file-id") id = h.value;
                if (h.key == "x-bz-file-name") name = h.value;
                if (h.key == "x-bz-content-sha1") sha1 = h.value;
                if (h.key.StartsWith("x-bz-info-")) attributes[h.key.Substring("X-Bz-Info-".Length)] = h.value;
            }
        }
    }

    /// <summary>
    /// A fully asynchronous .net API for the BackBlaze B2 storage service. This class represents the B2
    /// HTTP api, i.e. it maps each HTTP method onto a C# method.
    /// </summary>
    public class B2Http
    {
        private ILog log = LogManager.GetLogger("com.wibblr.B2Http");

        private const string BaseUrl = "https://api.backblaze.com/b2api/v1";

        private HttpClient httpClient;

        public B2Http()
        {
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// When using the overloaded form of log.Trace that takes an Action<FormatMessageHandler>, the log message is passed to String.Format.
        /// This breaks if the log message contains braces. Use this wrapper method to avoid this.
        /// </summary>
        /// <param name="s"></param>
        private void Trace(Func<string> f)
        {
            if (log.IsTraceEnabled) log.Trace(f());
        }

        /// <summary>
        /// Convert a File object to a string suitable for logging
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        private string ToString(File f) =>
            $"{{fileId={f.fileId}, fileName={f.fileName}, action={f.action}, size={f.size}, uploadTimestamp={f.uploadTimestamp}}}";

        /// <summary>
        /// Convert a collection of File objects to a string suitable for logging
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        private string ToString(IEnumerable<File> files) => $"[{string.Join(",", files.Select(ToString))}]";

        /// <summary>
        ///  Convert a Dictionary object to a string suitable for logging
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        private string ToString(Dictionary<string, string> d) => $"{{{string.Join(", ", d.Select(a => a.Key + "=>" + a.Value))}}}";

        /// <summary>
        ///  Convert a Bucket object to a string suitable for logging
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        private string ToString(Bucket b) => $"{{accountId={b.accountId}, bucketId={b.bucketId}, bucketName={b.bucketName}, bucketType={b.bucketType}}}";

        /// <summary>
        /// Convert a collection of Bucket objects to a string suitable for logging
        /// </summary>
        /// <param name="buckets"></param>
        /// <returns></returns>
        private string ToString(IEnumerable<Bucket> buckets) => $"[{string.Join(",", buckets.Select(ToString))}]";

        /// <summary>
        /// Check the HTTP status code. If not 200 (OK), then throw an exception.
        /// If there is a JSON-encoded failure message in the response body, use
        /// that in the exception
        /// </summary>
        /// <param name="response">The response message to check</param>
        /// <param name="body">Stream containing the body of the response. This will be
        /// read only if the response status code is not 200 (OK)</param>
        public void ThrowIfFailure(HttpResponseMessage response, Stream body)
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                FailureResponse f = null;
                try
                {
                    f = FailureResponse.FromJson(body);            
                }
                catch (Exception e)
                {
                    f = new FailureResponse { code = null, status = (int)response.StatusCode, message = e.Message };
                }
                Trace(() => $"code={f.code}, status={f.status}, message={f.message}");
                throw new B2Exception(f.status, f.code, f.message);
            }
        }

        /// <summary>
        /// Call the B2 'Authorize Account' API (see https://www.backblaze.com/b2/docs/b2_authorize_account.html)
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <param name="applicationKey">Application key</param>
        /// <returns></returns>
        public async Task<AuthorizeAccountResponse> AuthorizeAccount(string accountId, string applicationKey)
        {
            Trace(() => $"AuthorizeAccount: accountId={accountId}, applicationKey={applicationKey}");

            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/b2_authorize_account")
                .WithBasicAuthorization($"{accountId}:{applicationKey}".ToBase64());

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            ThrowIfFailure(response, responseStream);

            var r = AuthorizeAccountResponse.FromJson(responseStream);
            Trace(() => $"AuthorizeAccount completed: accountId={r.accountId}, authorizationToken={r.authorizationToken}, apiUrl={r.apiUrl}, downloadUrl={r.downloadUrl}");
            return r;
        }

        /// <summary>
        /// Call the B2 'Create Bucket' API (see https://www.backblaze.com/b2/docs/b2_create_bucket.html)
        /// </summary>
        /// <param name="apiUrl"></param>
        /// <param name="authorizationToken"></param>
        /// <param name="accountId"></param>
        /// <param name="bucketName"></param>
        /// <param name="bucketType"></param>
        /// <returns></returns>
        public async Task<CreateBucketResponse> CreateBucket(string apiUrl, string authorizationToken, string accountId, string bucketName, string bucketType)
        {
            Trace(() => $"CreateBucket: apiUrl={apiUrl}, authorizationToken={authorizationToken}, accountId={accountId}, bucketName={bucketName}, bucketType={bucketType}");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{apiUrl}/b2api/v1/b2_create_bucket")
                .WithAuthorization(authorizationToken)
                .WithContent(new CreateBucketRequest { accountId = accountId, bucketName = bucketName, bucketType = bucketType });

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            ThrowIfFailure(response, responseStream);

            var r = CreateBucketResponse.FromJson(responseStream);
            Trace(() => $"CreateBucket completed: accountId={r.accountId} bucketId={r.bucketId} bucketName={r.bucketName} bucketType={r.bucketType}");
            return r;
        }

        /// <summary>
        /// Call the B2 'Delete Bucket' API (see https://www.backblaze.com/b2/docs/b2_delete_bucket.html)
        /// </summary>
        /// <param name="apiUrl"></param>
        /// <param name="authorizationToken"></param>
        /// <param name="accountId"></param>
        /// <param name="bucketId"></param>
        /// <returns></returns>
        public async Task<DeleteBucketResponse> DeleteBucket(string apiUrl, string authorizationToken, string accountId, string bucketId)
        {
            Trace(() => $"DeleteBucket: apiUrl={apiUrl}, authorizationToken={authorizationToken}, accountId={accountId}, bucketId={bucketId}");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{apiUrl}/b2api/v1/b2_delete_bucket")
                .WithAuthorization(authorizationToken)
                .WithContent(new DeleteBucketRequest { accountId = accountId, bucketId = bucketId });

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            ThrowIfFailure(response, responseStream);

            var r = DeleteBucketResponse.FromJson(responseStream);
            Trace(() => $"DeleteBucket completed: accountId={r.accountId}, bucketId={r.bucketId}, bucketName={r.bucketName}, bucketType={r.bucketType}");
            return r;
        }

        /// <summary>
        /// Call the B2 'Delete File Version' API (see https://www.backblaze.com/b2/docs/b2_delete_file_version.html)
        /// </summary>
        /// <param name="apiUrl"></param>
        /// <param name="authorizationToken"></param>
        /// <param name="fileName"></param>
        /// <param name="fileId"></param>
        /// <returns></returns>
        public async Task<DeleteFileVersionResponse> DeleteFileVersion(string apiUrl, string authorizationToken, string fileName, string fileId)
        {
            Trace(() => $"DeleteFileVersion: apiUrl={apiUrl}, authorizationToken={authorizationToken}, fileName={fileName}, fileId={fileId}");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{apiUrl}/b2api/v1/b2_delete_file_version")
                .WithAuthorization(authorizationToken)
                .WithContent(new DeleteFileVersionRequest { fileName = fileName, fileId = fileId });

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            ThrowIfFailure(response, responseStream);

            var r = DeleteFileVersionResponse.FromJson(responseStream);
            Trace(() => $"DeleteFileVersion completed: fileId={r.fileId}, fileName={r.fileName}");
            return r;
        }

        /// <summary>
        /// Call the B2 'Download File By ID' API (see https://www.backblaze.com/b2/docs/b2_download_file_by_id.html)
        /// </summary>
        /// <param name="apiUrl"></param>
        /// <param name="authorizationToken"></param>
        /// <param name="rangeLower"></param>
        /// <param name="rangeUpper"></param>
        /// <param name="fileId"></param>
        /// <returns></returns>
        public async Task<B2File> DownloadFileById(string apiUrl, string authorizationToken, string fileId, long? rangeLower = null, long? rangeUpper = null)
        {
            Trace(() => $"DownloadFileById: apiUrl={apiUrl}, authorizationToken={authorizationToken}, fileId={fileId}, rangeLower={rangeLower}, rangeUpper={rangeUpper}");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{apiUrl}/b2api/v1/b2_download_file_by_id")
               .WithAuthorization(authorizationToken)
               .WithContent(new DownloadFileByIdRequest { fileId = fileId })
               .WithRange(rangeLower, rangeUpper);

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            ThrowIfFailure(response, responseStream);

            var f = new B2File(responseStream, response.Headers, response.Content.Headers);
            Trace(() => $"DownloadFileById completed: id={f.id}, name={f.name}, length={f.length}, contentType={f.contentType}, sha1={f.sha1}, attributes=[{ToString(f.attributes)}]");
            return f;
        }

        /// <summary>
        /// Call the B2 'Download File By Name' API (see https://www.backblaze.com/b2/docs/b2_download_file_by_name.html)
        /// </summary>
        /// <param name="downloadUrl"></param>
        /// <param name="authorizationToken"></param>
        /// <param name="bucketName"></param>
        /// <param name="fileName"></param>
        /// <param name="rangeLower"></param>
        /// <param name="rangeUpper"></param>
        /// <returns></returns>
        public async Task<B2File> DownloadFileByName(string downloadUrl, string authorizationToken, string bucketName, string fileName, long? rangeLower = null, long? rangeUpper = null)
        {
            Trace(() => $"DownloadFileByName: downloadUrl={downloadUrl}, authorizationToken={authorizationToken}, bucketName={bucketName}, fileName={fileName}, rangeLower={rangeLower}, rangeUpper={rangeUpper}");

            var request = new HttpRequestMessage(HttpMethod.Get, $"{downloadUrl}/file/${bucketName}/${fileName}")
                .WithAuthorization(authorizationToken)
                .WithRange(rangeLower, rangeUpper);

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            ThrowIfFailure(response, responseStream);
            var f = new B2File(responseStream, response.Headers, response.Content.Headers);
            Trace(() => $"DownloadFileByName completed: id={f.id}, name={f.name}, length={f.length}, contentType={f.contentType}, sha1={f.sha1}, attributes=[{ToString(f.attributes)}]");
            return f;
        }

        /* TODO: Large file support - b2_finish_large_file */

        /// <summary>
        /// Call the B2 'Get File Info' API (see https://www.backblaze.com/b2/docs/b2_get_file_info.html)
        /// </summary>
        /// <param name="apiUrl"></param>
        /// <param name="authorizationToken"></param>
        /// <param name="fileId"></param>
        /// <returns></returns>
        public async Task<GetFileInfoResponse> GetFileInfo(string apiUrl, string authorizationToken, string fileId) {
            Trace(() => $"GetFileInfo: apiUrl={apiUrl}, authorizationToken={authorizationToken}, fileId={fileId}");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{apiUrl}/b2api/v1/b2_get_file_info")
                .WithAuthorization(authorizationToken)
                .WithContent(new GetFileInfoRequest { fileId = fileId });

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            ThrowIfFailure(response, responseStream);

            var r = GetFileInfoResponse.FromJson(responseStream);
            Trace(() => $"GetFileInfo completed: fileId={r.fileId}, fileName={r.fileName}, accountId={r.accountId}, contentSha1={r.contentSha1}, bucketId={r.bucketId}, contentLength={r.contentLength}, contentType={r.contentType}, fileInfo=[{ToString(r.fileInfo)}]");
            return r;
        }

        /* TODO: Large file support - b2_get_upload_part_url */

        /// <summary>
        /// Call the B2 'Get Upload Url' API (see https://www.backblaze.com/b2/docs/b2_get_upload_url.html)
        /// </summary>
        /// <param name="apiUrl"></param>
        /// <param name="authorizationToken"></param>
        /// <param name="bucketId"></param>
        /// <returns></returns>
        public async Task<GetUploadUrlResponse> GetUploadUrl(string apiUrl, string authorizationToken, string bucketId)
        {
            Trace(() => $"GetUploadUrl: apiUrl={apiUrl}, authorizationToken={authorizationToken}, bucketId={bucketId}");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{apiUrl}/b2api/v1/b2_get_upload_url")
                .WithAuthorization(authorizationToken)
                .WithContent(new GetUploadUrlRequest { bucketId = bucketId });

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            ThrowIfFailure(response, responseStream);

            var r = GetUploadUrlResponse.FromJson(responseStream);
            Trace(() => $"GetUploadUrl completed: bucketId={r.bucketId}, uploadUrl={r.uploadUrl}, authorizationToken={r.authorizationToken}");
            return r;
        }

        /// <summary>
        /// Call the B2 'Hide File' API (see https://www.backblaze.com/b2/docs/b2_hide_file.html)
        /// </summary>
        /// <param name="apiUrl"></param>
        /// <param name="authorizationToken"></param>
        /// <param name="bucketId"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task<HideFileResponse> HideFile(string apiUrl, string authorizationToken, string bucketId, string fileName)
        {
            Trace(() => $"HideFile: apiUrl={apiUrl}, authorizationToken={authorizationToken}, bucketId={bucketId}, fileName={fileName}");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{apiUrl}/b2api/v1/b2_hide_file")
                .WithAuthorization(authorizationToken)
                .WithContent(new HideFileRequest { bucketId = bucketId, fileName = fileName });

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            ThrowIfFailure(response, responseStream);

            var r = HideFileResponse.FromJson(responseStream);
            Trace(() => $"HideFile completed: fileId={r.fileId}, fileName={r.fileName}, contentLength={r.contentLength}, contentSha1={r.contentSha1}, fileInfo=[{ToString(r.fileInfo)}]");
            return r;
        }
      
        /// <summary>
        /// Call the B2 'List Buckets' API (see https://www.backblaze.com/b2/docs/b2_list_buckets.html)
        /// </summary>
        /// <param name="apiUrl"></param>
        /// <param name="authorizationToken"></param>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public async Task<ListBucketsResponse> ListBuckets(string apiUrl, string authorizationToken, string accountId)
        {
            Trace(() => $"ListBuckets: apiUrl={apiUrl}, authorizationToken={authorizationToken}, accountId={accountId}");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{apiUrl}/b2api/v1/b2_list_buckets")
                .WithAuthorization(authorizationToken)
                .WithContent(new ListBucketsRequest { accountId = accountId });

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            ThrowIfFailure(response, responseStream);

            var r = ListBucketsResponse.FromJson(responseStream);
            Trace(() => $"ListBuckets completed: buckets={ToString(r.buckets)}");
            return r;
        }

        /// <summary>
        /// Call the B2 'List File Names' API (see https://www.backblaze.com/b2/docs/b2_list_file_names.html)
        /// </summary>
        /// <param name="apiUrl"></param>
        /// <param name="authorizationToken"></param>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public async Task<ListFileNamesResponse> ListFileNames(string apiUrl, string authorizationToken, string bucketId, string startFileName = null, int maxFileCount = 1000)
        {
            Trace(() => $"ListFileNames: apiUrl={apiUrl}, authorizationToken={authorizationToken}, bucketId={bucketId}, startFileName={startFileName}, maxFileCount={maxFileCount}");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{apiUrl}/b2api/v1/b2_list_file_names")
                .WithAuthorization(authorizationToken)
                .WithContent(new ListFileNamesRequest { bucketId = bucketId, startFileName = startFileName, maxFileCount = maxFileCount });

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            ThrowIfFailure(response, responseStream);

            var r = ListFileNamesResponse.FromJson(responseStream);
            Trace(() => $"ListFileNames completed: files={ToString(r.files)}, nextFileName={r.nextFileName}");
            return r;
        }

        /// <summary>
        /// Call the B2 'List File Versions' API (see https://www.backblaze.com/b2/docs/b2_list_file_versions.html)
        /// </summary>
        /// <param name="apiUrl"></param>
        /// <param name="authorizationToken"></param>
        /// <param name="bucketId"></param>
        /// <param name="startFileName"></param>
        /// <param name="startFileId"></param>
        /// <param name="maxFileCount"></param>
        /// <returns></returns>
        public async Task<ListFileVersionsResponse> ListFileVersions(string apiUrl, string authorizationToken, string bucketId, string startFileName = null, string startFileId = null, int maxFileCount = 1000)
        {
            Trace(() => $"ListFileVersions: apiUrl={apiUrl}, authorizationToken={authorizationToken}, bucketId={bucketId}, startFileName={startFileName}, maxFileCount={maxFileCount}");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{apiUrl}/b2api/v1/b2_list_file_versions")
                .WithAuthorization(authorizationToken)
                .WithContent(new ListFileNamesRequest { bucketId = bucketId, startFileName = startFileName, maxFileCount = maxFileCount });

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            ThrowIfFailure(response, responseStream);

            var r = ListFileVersionsResponse.FromJson(responseStream);
            Trace(() => $"ListFileVersions completed: files={ToString(r.files)}, nextFileName={r.nextFileName}, nextFileId={r.nextFileId}");
            return r;
        }

        /* 
           TODO: Large file support - b2_list_parts
           TODO: Large file support - b2_list_unfinished_large_files
           TODO: Large file support - b2_start_large_file
        */

        /// <summary>
        /// Call the B2 'UpdateBucket' API (see https://www.backblaze.com/b2/docs/b2_update_bucket.html)
        /// </summary>
        /// <param name="apiUrl"></param>
        /// <param name="authorizationToken"></param>
        /// <param name="bucketId"></param>
        /// <param name="startFileName"></param>
        /// <param name="startFileId"></param>
        /// <param name="maxFileCount"></param>
        /// <returns></returns>
        public async Task<UpdateBucketResponse> UpdateBucket(string apiUrl, string authorizationToken, string accountId, string bucketId, string bucketType)
        {
            Trace(() => $"UpdateBucket: apiUrl={apiUrl}, authorizationToken={authorizationToken}, bucketId={bucketId}, bucketType={bucketType}");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{apiUrl}/b2api/v1/b2_update_bucket")
                .WithAuthorization(authorizationToken)
                .WithContent(new UpdateBucketRequest { accountId = accountId, bucketId = bucketId, bucketType = bucketType });

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            ThrowIfFailure(response, responseStream);

            var r = UpdateBucketResponse.FromJson(responseStream);
            Trace(() => $"UpdateBucket completed: accountId={r.accountId}, bucketId={r.bucketId}, bucketName={r.bucketName}, bucketType={r.bucketType}");
            return r;
        }

        /// <summary>
        /// Call the B2 'Upload File' API (see https://www.backblaze.com/b2/docs/b2_upload_file.html)
        /// </summary>
        /// <param name="uploadUrl"></param>
        /// <param name="authorizationToken"></param>
        /// <param name="fileName"></param>
        /// <param name="contentType"></param>
        /// <param name="contentLength"></param>
        /// <param name="contentSha1"></param>
        /// <param name="fileInfo"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task<UploadFileResponse> UploadFile(string uploadUrl, string authorizationToken, string fileName, string contentType, long contentLength, string contentSha1, Dictionary<string, string> attributes, Stream content)
        {
            if (attributes == null)
                throw new ArgumentNullException("attributes");

            Trace(() => $"UploadFile: uploadUrl={uploadUrl}, authorizationToken={authorizationToken}, fileName={fileName}, contentType={contentType}, contentLength={contentLength}, contentSha1={contentSha1}, attributes={ToString(attributes)}");

            var headers = (attributes.ToDictionary(a => $"X-Bz-Info-{a.Key}", a => a.Value));
            headers["X-Bz-File-Name"] = B2UrlEncoder.Encode(fileName.Replace('\\', '/'));
            headers["Content-Type"] = contentType;
            headers["Content-Length"] = contentLength.ToString();
            headers["X-Bz-Content-Sha1"] = contentSha1;

            var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl)
                .WithAuthorization(authorizationToken)
                .WithContent(content)
                .WithContentHeaders(headers);

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            ThrowIfFailure(response, responseStream);

            Trace(() => "UploadFile completed");
            return UploadFileResponse.FromJson(responseStream);
        }

        /* TODO: Large file support - b2_upload_part */
    }
}
