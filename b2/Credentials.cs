using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;

namespace com.wibblr.b2
{
    internal class Credentials
    {
        public string accountId;
        public string applicationKey;

        internal static Credentials Read()
        {
            using (var s = new FileStream("credentials.json", FileMode.Open))
            {
                return new DataContractJsonSerializer(typeof(Credentials)).ReadObject(s) as Credentials;
            }
        }
    }
}
