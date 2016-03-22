using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using Stream = System.IO.Stream;

namespace com.wibblr.b2
{
    public interface B2Request { }
    public interface B2Response { }

    //-------------------------------------------------------------------
    // FailureResponse
    //-------------------------------------------------------------------
    public class FailureResponse : B2Response
    {
        public string code;
        public string message;
        public int status;

        static internal FailureResponse FromJson(Stream s) =>
            new DataContractJsonSerializer(typeof(FailureResponse)).ReadObject(s) as FailureResponse;
    }

    //-------------------------------------------------------------------
    // AuthorizeAccount
    //-------------------------------------------------------------------
    public class AuthorizeAccountResponse : B2Response
    {
        public string accountId;
        public string authorizationToken;
        public string apiUrl;
        public string downloadUrl;

        static internal AuthorizeAccountResponse FromJson(Stream s) =>
            new DataContractJsonSerializer(typeof(AuthorizeAccountResponse)).ReadObject(s) as AuthorizeAccountResponse;
    }

    //-------------------------------------------------------------------
    // CancelLargeFile
    //-------------------------------------------------------------------
    public class CancelLargeFileRequest : B2Request
    {
        public string fileId;
    }

    public class CancelLargeFileResponse : B2Response
    {
        public string fileId;
        public string accountId;
        public string bucketId;
        public string fileName;

        static internal CancelLargeFileResponse FromJson(Stream s) =>
            new DataContractJsonSerializer(typeof(CancelLargeFileResponse)).ReadObject(s) as CancelLargeFileResponse;
    }
        
    //-------------------------------------------------------------------
    // CreateBucket
    //-------------------------------------------------------------------
    public class CreateBucketRequest : B2Request
    {
        public string accountId;
        public string bucketName;
        public string bucketType;
    }

    public class CreateBucketResponse : B2Response
    {
        public string accountId;
        public string bucketId;
        public string bucketName;
        public string bucketType;

        static internal CreateBucketResponse FromJson(Stream s) =>
            new DataContractJsonSerializer(typeof(CreateBucketResponse)).ReadObject(s) as CreateBucketResponse;
    }

    //-------------------------------------------------------------------
    // DeleteBucket
    //-------------------------------------------------------------------
    public class DeleteBucketRequest : B2Request
    {
        public string accountId;
        public string bucketId;
    }

    public class DeleteBucketResponse : B2Response
    {
        public string accountId;
        public string bucketId;
        public string bucketName;
        public string bucketType;

        static internal DeleteBucketResponse FromJson(Stream s) =>
            new DataContractJsonSerializer(typeof(DeleteBucketResponse)).ReadObject(s) as DeleteBucketResponse;
    }

    //-------------------------------------------------------------------
    // DeleteFileVersion
    //-------------------------------------------------------------------
    public class DeleteFileVersionRequest : B2Request
    {
        public string fileName;
        public string fileId;
    }

    public class DeleteFileVersionResponse : B2Response
    {
        public string fileId;
        public string fileName;

        static internal DeleteFileVersionResponse FromJson(Stream s) =>
            new DataContractJsonSerializer(typeof(DeleteFileVersionResponse)).ReadObject(s) as DeleteFileVersionResponse;
    }

    //-------------------------------------------------------------------
    // DownloadFileById
    //-------------------------------------------------------------------
    public class DownloadFileByIdRequest : B2Request
    {
        public string fileId;
    }

    //-------------------------------------------------------------------
    // FinishLargeFile
    //-------------------------------------------------------------------
    public class FinishLargeFileRequest : B2Request
    {
        public string fileId;
        public IList<string> partSha1Array;
    }

    public class FinishLargeFileResponse : B2Response
    {
        public string fileId;
        public string fileName;
        public string accountId;
        public string bucketId;
        public long contentLength;
        public string contentSha1;
        public string contentType;
        public Dictionary<string, string> fileInfo;

        static internal FinishLargeFileResponse FromJson(Stream s) =>
            new DataContractJsonSerializer(typeof(FinishLargeFileResponse)).ReadObject(s) as FinishLargeFileResponse;
    }

    //-------------------------------------------------------------------
    // GetFileInfo
    //-------------------------------------------------------------------
    public class GetFileInfoRequest : B2Request
    {
        public string fileId;
    }

    public class GetFileInfoResponse : B2Response
    {
        public string fileId;
        public string fileName;
        public string accountId;
        public string contentSha1;
        public string bucketId;
        public long contentLength;
        public string contentType;
        public Dictionary<string, string> fileInfo;

        static internal GetFileInfoResponse FromJson(Stream s) =>
            new DataContractJsonSerializer(typeof(GetFileInfoResponse)).ReadObject(s) as GetFileInfoResponse;
    }


    //-------------------------------------------------------------------
    // GetUploadPartUrl
    //-------------------------------------------------------------------
    public class GetUploadPartUrlRequest : B2Request
    {
        public string fileId;
    }

    public class GetUploadPartUrlResponse : B2Response
    {
        public string fileId;
        public string uploadUrl;
        public string authorizationToken;

        static internal GetUploadPartUrlResponse FromJson(Stream s) =>
            new DataContractJsonSerializer(typeof(GetUploadPartUrlResponse)).ReadObject(s) as GetUploadPartUrlResponse;
    }

    //-------------------------------------------------------------------
    // GetUploadUrl
    //-------------------------------------------------------------------
    public class GetUploadUrlRequest : B2Request
    {
        public string bucketId;
    }

    public class GetUploadUrlResponse : B2Response
    {
        public string bucketId;
        public string uploadUrl;
        public string authorizationToken;

        static internal GetUploadUrlResponse FromJson(Stream s) =>
            new DataContractJsonSerializer(typeof(GetUploadUrlResponse)).ReadObject(s) as GetUploadUrlResponse;
    }

    //-------------------------------------------------------------------
    // HideFile
    //-------------------------------------------------------------------
    public class HideFileRequest : B2Request
    {
        public string bucketId;
        public string fileName;
    }

    public class HideFileResponse : B2Response
    {
        public string fileId;
        public string fileName;
        public long contentLength;
        public string contentSha1;
        public Dictionary<string, string> fileInfo;

        static internal HideFileResponse FromJson(Stream s) =>
            new DataContractJsonSerializer(typeof(HideFileResponse)).ReadObject(s) as HideFileResponse;
    }

    //-------------------------------------------------------------------
    // ListBuckets
    //-------------------------------------------------------------------
    public class ListBucketsRequest : B2Request
    {
        public string accountId;
    }

    public class ListBucketsResponse : B2Response
    {
        public IList<Bucket> buckets;

        static internal ListBucketsResponse FromJson(Stream s) =>
            new DataContractJsonSerializer(typeof(ListBucketsResponse)).ReadObject(s) as ListBucketsResponse;
    }

    public class Bucket
    {
        public string accountId;
        public string bucketId;
        public string bucketName;
        public string bucketType;
    }

    //-------------------------------------------------------------------
    // ListFileNames
    //-------------------------------------------------------------------
    public class ListFileNamesRequest : B2Request
    {
        public string bucketId;
        public string startFileName;
        public int maxFileCount;
    }

    public class ListFileNamesResponse : B2Response
    {
        public IList<B2FileMetadata> files;
        public string nextFileName;

        static internal ListFileNamesResponse FromJson(Stream s) =>
            new DataContractJsonSerializer(typeof(ListFileNamesResponse)).ReadObject(s) as ListFileNamesResponse;
    }

    public class B2FileMetadata
    {
        public string fileId;
        public string fileName;
        // TODO: Not yet implemented
        // public long contentLength;
        // public string contentSha1;
        // public Dictionary<string, string> fileInfo;
        public string action;
        public long size;
        public string uploadTimestamp;
    }

    //-------------------------------------------------------------------
    // ListFileVersions
    //-------------------------------------------------------------------
    public class ListFileVersionsRequest : B2Request
    {
        public string bucketId;
        public string startFileName;
        public string startFileId;
        public int maxFileCount;
    }

    public class ListFileVersionsResponse : B2Response
    {
        public IList<B2FileMetadata> files;
        public string nextFileName;
        public string nextFileId;

        static internal ListFileVersionsResponse FromJson(Stream s) =>
            new DataContractJsonSerializer(typeof(ListFileVersionsResponse)).ReadObject(s) as ListFileVersionsResponse;
    }

    //-------------------------------------------------------------------
    // ListParts
    //-------------------------------------------------------------------
    public class ListPartsRequest : B2Request
    {
        public string fileId;
        public long startPartNumber;
        public int maxPartCount;
    }

    public class ListPartsReponse : B2Response
    {
        public IList<Part> parts;
        public long nextPartNumber;

        static internal ListPartsReponse FromJson(Stream s) =>
            new DataContractJsonSerializer(typeof(ListPartsReponse)).ReadObject(s) as ListPartsReponse;
    }

    public class Part
    {
        public string fileId;
        public long partNumber;
        public long contentLength;
        public string contentSha1;
    }

    //-------------------------------------------------------------------
    // ListUnfinishedLargeFiles
    //-------------------------------------------------------------------
    public class ListUnfinishedLargeFilesRequest : B2Request
    {
        public string bucketId;
        public string startFileId;
        public int maxUploadCount;
    }

    public class ListUnfinishedLargeFilesResponse : B2Response
    {
        public IList<Upload> uploads;
        public string nextFileId;

        static internal ListUnfinishedLargeFilesResponse FromJson(Stream s) =>
            new DataContractJsonSerializer(typeof(ListUnfinishedLargeFilesResponse)).ReadObject(s) as ListUnfinishedLargeFilesResponse;
    }

    public class Upload
    {
        public string fileId;
        public string fileName;
        public string accountId;
        public string bucketId;
        public string contentType;
        public Dictionary<string, string> fileInfo;
        public string uploadAuthToken;
        public IList<string> uploadUrls;
    }

    //-------------------------------------------------------------------
    // StartLargeFile
    //-------------------------------------------------------------------
    public class StartLargeFileRequest : B2Request
    {
        public string bucketId;
        public string fileName;
        public string contentType;
        public Dictionary<string, string> fileInfo;
    }

    public class StartLargeFileResponse : B2Response
    {
        public string fileId;
        public string fileName;
        public string accountId;
        public string bucketId;
        public string contentType;
        public Dictionary<string, string> fileInfo;
        public long minimumPartSize;

        static internal StartLargeFileResponse FromJson(Stream s) =>
            new DataContractJsonSerializer(typeof(StartLargeFileResponse)).ReadObject(s) as StartLargeFileResponse;
    }

    //-------------------------------------------------------------------
    // UpdateBucket
    //-------------------------------------------------------------------
    public class UpdateBucketRequest : B2Request
    {
        public string accountId;
        public string bucketId;
        public string bucketType;
    }

    public class UpdateBucketResponse : B2Response
    {
        public string accountId;
        public string bucketId;
        public string bucketName;
        public string bucketType;

        static internal UpdateBucketResponse FromJson(Stream s) =>
            new DataContractJsonSerializer(typeof(UpdateBucketResponse)).ReadObject(s) as UpdateBucketResponse;
    }

    //-------------------------------------------------------------------
    // UploadFile
    //-------------------------------------------------------------------
    public class UploadFileResponse : B2Response
    {
        public string fileId;
        public string fileName;
        public string accountId;
        public string bucketId;
        public long contentLength;
        public string contentSha1;
        public string contentType;
        public Dictionary<string, string> fileInfo;

        static internal UploadFileResponse FromJson(Stream s) =>
            new DataContractJsonSerializer(typeof(UploadFileResponse)).ReadObject(s) as UploadFileResponse;
    }

    //-------------------------------------------------------------------
    // UploadPart
    //-------------------------------------------------------------------
    public class UploadPartResponse : B2Response
    {
        public string fileId;
        public int partNumber;
        public long contentLength;
        public string contentSha1;

        static internal UploadPartResponse FromJson(Stream s) =>
            new DataContractJsonSerializer(typeof(UploadPartResponse)).ReadObject(s) as UploadPartResponse;
    }
}
