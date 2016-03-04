using System;
using System.IO;
using System.Runtime.Serialization.Json;

namespace com.wibblr.b2
{
    public class Credentials
    {
        public string accountId;
        public string applicationKey;

        /// <summary>
        /// Read the credentials for the b2 service from the disk file 'credentials.json'.
        /// This should look like this:
        /// <code>
        /// {
        ///     "accountId": "3248833834",
        ///     "applicationKey": "29319230812931920381092381023289"
        /// }
        /// </code>
        /// </summary>
        /// <param name="directory">Directory from which to read credentials. Defaults to the file ~\.cwb2\credentials.json</param>
        /// <returns>Credentials object</returns>
        internal static Credentials Read(string directory = null)
        {
            if (directory == null)
            {
                directory = Path.Combine(Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%"), ".cwb2"); // NOTE: currently works only on Windows.
            }

            var credentialsFile = Path.Combine(directory, "credentials.json");
            try
            {
                using (var s = new FileStream(credentialsFile, FileMode.Open))
                {
                    return (Credentials) new DataContractJsonSerializer(typeof(Credentials)).ReadObject(s);
                }
            }
            catch (IOException ioe)
            {
                throw new Exception($"Unable to read credentials file {credentialsFile}", ioe);
            }
            catch (Exception e)
            {
                throw new Exception($"Error deserializing credentials file {credentialsFile}", e);
            }
        }
    }
}
