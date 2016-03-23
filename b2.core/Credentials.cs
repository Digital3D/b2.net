using System;
using System.IO;
using System.Runtime.Serialization.Json;

namespace com.wibblr.b2
{
    public class Credentials
    {
        // These fields are serialized into JSON
        public string accountId = "";
        public string applicationKey = "";

        public static string DefaultCredentialsPath() => Path.Combine(Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%"), ".b2.net", "credentials.json");

        private static Credentials ReadFromEnvironment()
        {
            var accountId = Environment.GetEnvironmentVariable("B2_ACCOUNT_ID");
            var applicationKey = Environment.GetEnvironmentVariable("B2_APPLICATION_KEY");

            if (accountId == null || applicationKey == null)
                throw new Exception("Environment variables B2_ACCOUNT_ID and B2_APPLICATION_KEY not set");

            return new Credentials { accountId = accountId, applicationKey = applicationKey };
        }

        private static Credentials ReadFromFile(string path = null)
        {
            var f = path ?? DefaultCredentialsPath();
            try
            {
                using (var s = new FileStream(f, FileMode.Open))
                {
                    return (Credentials) new DataContractJsonSerializer(typeof(Credentials)).ReadObject(s);
                }
            }
            catch (IOException ioe)
            {
                throw new Exception($"Unable to read credentials file {f}", ioe);
            }
            catch (Exception e)
            {
                throw new Exception($"Error deserializing credentials file {f}", e);
            }
        }

        /// <summary>
        /// Read credentials from environment variables. If they are not set, read from file instead.
        /// This should look like this:
        /// <code>
        /// {"accountId":"3248833834","applicationKey":"29319230812931920381092381023289"}
        /// </code>
        /// </summary>
        /// <param name="path">File from which to read credentials. Defaults to ~\.b2.net\credentials.json</param>
        /// <returns>Credentials object</returns>
        /// </summary>
        /// <returns></returns>
        public static Credentials Read(string path = null)
        {
            try { return ReadFromEnvironment(); } catch (Exception) { }

            return ReadFromFile(path);
        }

        /// <summary>
        /// Write a credentials file to disk
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="applicationKey"></param>
        /// <param name="path">File to write credentials to. Defaults to ~\.b2.net\credentials.json</param>
        public static void Write(string accountId, string applicationKey, string path = null)
        {
            var f = path ?? DefaultCredentialsPath();
            var c = new Credentials { accountId = accountId ?? "", applicationKey = applicationKey ?? "" };

            using (var stream = new FileStream(f, FileMode.Create))
            {
                new DataContractJsonSerializer(c.GetType()).WriteObject(stream, c);
            }
        }

        /// <summary>
        /// Delete the credentials file from disk
        /// </summary>
        /// <param name="path">File to delete. Defaults to ~\.b2.net\credentials.json</param>
        public static void Delete(string path = null)
        {
            var f = path ?? DefaultCredentialsPath();

            if (File.Exists(f))
                File.Delete(f);
        }
    }
}
