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
        static internal Tuple<long, Stream> ToJson(object o)
        {
            var stream = new MemoryStream();
            new DataContractJsonSerializer(o.GetType()).WriteObject(stream, o);
            var length = stream.Position;
            stream.Seek(0, 0);
            return new Tuple<long, Stream>(length, stream);
        }

        //-------------------------------------------------------------------
        // AuthorizeAccount
        //-------------------------------------------------------------------
        [DataContract] public class AuthorizeAccountResponse
        {
            [DataMember] public string accountId;
            [DataMember] public string authorizationToken;
            [DataMember] public string apiUrl;
            [DataMember] public string downloadUrl;

            static internal AuthorizeAccountResponse FromJson(Stream s) =>
                new DataContractJsonSerializer(typeof(AuthorizeAccountResponse)).ReadObject(s) as AuthorizeAccountResponse;
        }

        //-------------------------------------------------------------------
        // ListBuckets
        //-------------------------------------------------------------------
        [DataContract] public class ListBucketsRequest
        {
            [DataMember] public string accountId;
        }

        [DataContract] public class ListBucketsResponse
        {
            [DataMember] public IList<Bucket> buckets;

            static internal ListBucketsResponse FromJson(Stream s) =>
                new DataContractJsonSerializer(typeof(ListBucketsResponse)).ReadObject(s) as ListBucketsResponse;
        }

        [DataContract] public class Bucket
        {
            [DataMember] public string accountId;
            [DataMember] public string bucketId;
            [DataMember] public string bucketName;
            [DataMember] public string bucketType;
        }
    }
}
