using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;

namespace com.wibblr.b2
{
    public class Api
    {
        static internal Stream ToJson(object o)
        {
            var stream = new MemoryStream();
            new DataContractJsonSerializer(o.GetType()).WriteObject(stream, o);
            stream.Seek(0, 0);
            return stream;
        }

        //-------------------------------------------------------------------
        // AuthorizeAccount
        //-------------------------------------------------------------------
        public class AuthorizeAccountResponse
        {
            public string accountId;
            public string authorizationToken;
            public string apiUrl;
            public string downloadUrl;

            static internal AuthorizeAccountResponse FromJson(Stream s) =>
                new DataContractJsonSerializer(typeof(AuthorizeAccountResponse)).ReadObject(s) as AuthorizeAccountResponse;
        }

        //-------------------------------------------------------------------
        // ListBuckets
        //-------------------------------------------------------------------
        public class ListBucketsRequest
        {
            public string accountId;
        }

        public class ListBucketsResponse
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
    }
}
