using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.IO;

using Common.Logging;

namespace com.wibblr.b2
{
    /// <summary>
    /// Class used to return downloaded file data
    /// </summary>
    public class B2File
    {
        public Stream content;
        public string id;
        public String name;
        public long length;
        public string contentType;
        public string sha1;
        public Dictionary<string, string> attributes = new Dictionary<string, string>();
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
        /// Call the B2 'Authorize Account' API (see https://www.backblaze.com/b2/docs/b2_authorize_account.html)
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <param name="applicationKey">Application key</param>
        /// <returns></returns>
        public async Task<AuthorizeAccountResponse> AuthorizeAccount(string accountId, string applicationKey)
        {
            log.Trace(m => m($"AuthorizeAccount: accountId={accountId}, applicationKey={applicationKey}"));

            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/b2_authorize_account")
                .WithBasicAuthorization($"{accountId}:{applicationKey}".ToBase64());

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            response.ThrowIfFailure(responseStream);

            var r = AuthorizeAccountResponse.FromJson(responseStream);
            log.Trace(m => m($"AuthorizeAccount response: accountId={r.accountId}, authorizationToken={r.authorizationToken}, apiUrl={r.apiUrl}, downloadUrl={r.downloadUrl}"));
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
            log.Trace(m => m($"CreateBucket: apiUrl={apiUrl}, authorizationToken={authorizationToken}, accountId={accountId}, bucketName={bucketName}, bucketType={bucketType}"));

            var request = new HttpRequestMessage(HttpMethod.Post, $"{apiUrl}/b2api/v1/b2_create_bucket")
                .WithAuthorization(authorizationToken)
                .WithContent(new CreateBucketRequest { accountId = accountId, bucketName = bucketName, bucketType = bucketType });

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            response.ThrowIfFailure(responseStream);

            var r = CreateBucketResponse.FromJson(responseStream);
            log.Trace(m => m($"CreateBucket response: accountId={r.accountId} bucketId={r.bucketId} bucketName={r.bucketName} bucketType={r.bucketType}"));
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
            var request = new HttpRequestMessage(HttpMethod.Post, $"{apiUrl}/b2api/v1/b2_delete_bucket")
                .WithAuthorization(authorizationToken)
                .WithContent(new DeleteBucketRequest { accountId = accountId, bucketId = bucketId });

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            response.ThrowIfFailure(responseStream);
            return DeleteBucketResponse.FromJson(responseStream);
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
            var request = new HttpRequestMessage(HttpMethod.Post, $"{apiUrl}/b2api/v1/b2_delete_file_version")
                .WithAuthorization(authorizationToken)
                .WithContent(new DeleteFileVersionRequest { fileName = fileName, fileId = fileId });

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            response.ThrowIfFailure(responseStream);
            return DeleteFileVersionResponse.FromJson(responseStream);
        }

        /// <summary>
        /// Call the B2 'Download File By ID Version' API (see https://www.backblaze.com/b2/docs/b2_download_file_by_id.html)
        /// </summary>
        /// <param name="apiUrl"></param>
        /// <param name="authorizationToken"></param>
        /// <param name="rangeLower"></param>
        /// <param name="rangeUpper"></param>
        /// <param name="fileId"></param>
        /// <returns></returns>
        public async Task<B2File> DownloadFileById(string apiUrl, string authorizationToken, string fileId, long? rangeLower = null, long? rangeUpper = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{apiUrl}/b2api/v1/b2_download_file_by_id")
               .WithAuthorization(authorizationToken)
               .WithContent(new DownloadFileByIdRequest { fileId = fileId })
               .WithRange(rangeLower, rangeUpper);

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            response.ThrowIfFailure(responseStream);

            var b2File = new B2File();
            b2File.content = responseStream;
            b2File.length = Convert.ToInt64(response.Content.Headers.ContentLength);
            b2File.contentType = response.Content.Headers.ContentType.MediaType;

            foreach (var h in response.Headers)
            {
                if (h.Key.Equals("X-Bz-File-Id",      StringComparison.OrdinalIgnoreCase)) b2File.id          = h.Value.First();
                if (h.Key.Equals("X-Bz-File-Name",    StringComparison.OrdinalIgnoreCase)) b2File.name        = h.Value.First();
                if (h.Key.Equals("X-Bz-Content-Sha1", StringComparison.OrdinalIgnoreCase)) b2File.sha1        = h.Value.First();
                if (h.Key.StartsWith("X-Bz-Info-",    StringComparison.OrdinalIgnoreCase)) b2File.attributes[h.Key.Substring("X-Bz-Info-".Length)] = h.Value.First();
            }
            return b2File;
        }

        /* TODO:
            b2_download_file_by_name
            b2_finish_large_file
            b2_get_file_info
            b2_get_upload_part_url
        */

        /// <summary>
        /// Call the B2 'Get Upload Url' API (see https://www.backblaze.com/b2/docs/b2_get_upload_url.html)
        /// </summary>
        /// <param name="apiUrl"></param>
        /// <param name="authorizationToken"></param>
        /// <param name="bucketId"></param>
        /// <returns></returns>
        public async Task<GetUploadUrlResponse> GetUploadUrl(string apiUrl, string authorizationToken, string bucketId)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{apiUrl}/b2api/v1/b2_get_upload_url")
                .WithAuthorization(authorizationToken)
                .WithContent(new GetUploadUrlRequest { bucketId = bucketId });

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            response.ThrowIfFailure(responseStream);
            return GetUploadUrlResponse.FromJson(responseStream);
        }

        /* TODO:
            b2_hide_file
        */

        /// <summary>
        /// Call the B2 'List Buckets' API (see https://www.backblaze.com/b2/docs/b2_list_buckets.html)
        /// </summary>
        /// <param name="apiUrl"></param>
        /// <param name="authorizationToken"></param>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public async Task<ListBucketsResponse> ListBuckets(string apiUrl, string authorizationToken, string accountId)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{apiUrl}/b2api/v1/b2_list_buckets")
                .WithAuthorization(authorizationToken)
                .WithContent(new ListBucketsRequest { accountId = accountId });

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            response.ThrowIfFailure(responseStream);
            return ListBucketsResponse.FromJson(responseStream);
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
            var request = new HttpRequestMessage(HttpMethod.Post, $"{apiUrl}/b2api/v1/b2_list_file_names")
                .WithAuthorization(authorizationToken)
                .WithContent(new ListFileNamesRequest { bucketId = bucketId, startFileName = startFileName, maxFileCount = maxFileCount });

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            response.ThrowIfFailure(responseStream);
            return ListFileNamesResponse.FromJson(responseStream);
        }

        /* TODO:
            b2_list_file_versions
            b2_list_parts
            b2_list_unfinished_large_files
            b2_start_large_file
            b2_update_bucket
        */

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
            response.ThrowIfFailure(responseStream);
            return UploadFileResponse.FromJson(responseStream);
        }
    }
}
