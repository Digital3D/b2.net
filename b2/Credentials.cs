using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;

namespace com.wibblr.b2
{
    [DataContract] internal class Credentials
    {
        [DataMember] internal string accountId;
        [DataMember] internal string applicationKey;

        internal static Credentials Read()
        {
            using (var s = new FileStream("credentials.json", FileMode.Open))
            {
                return new DataContractJsonSerializer(typeof(Credentials)).ReadObject(s) as Credentials;
            }
        }
    }
}
