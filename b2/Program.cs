using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace com.wibblr.b2
{
    public static class Extensions
    {
        public static string ToBase64(this string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
    }

    public class Test
    {
        public static void Main(string[] args)
        {
            try {
                MainAsync().Wait();
            }
            catch (AggregateException ae)
            {
                throw ae.InnerException;
            }
            Console.ReadLine();
        }

        private static async Task MainAsync()
        {
            // read accountId and applicationKey from file
            var credentials = Credentials.Read();

            var response = await B2.AuthorizeAccountAsync(credentials.accountId, credentials.applicationKey);

            var apiUrl = response.apiUrl;
            var authorizationToken = response.authorizationToken;

            var response2 = await B2.ListBucketsAsync(credentials.accountId, apiUrl, authorizationToken);

            foreach (var bucket in response2.buckets)
            {
                Console.WriteLine(bucket.bucketName);
            }
        }
    }

    class B2HttpClient : HttpClient
    {
        public B2HttpClient() : base() {
            DefaultRequestHeaders.Accept.Clear();
            DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }

    public class B2
    {
        private const string BaseUrl = "https://api.backblaze.com/b2api/v1";

        public static async Task<Api.AuthorizeAccountResponse> AuthorizeAccountAsync(string accountId, string apiKey)
        {
            using (var client = new B2HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", $"{accountId}:{apiKey}".ToBase64());
                var response = await client.GetAsync($"{BaseUrl}/b2_authorize_account").ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                return Api.AuthorizeAccountResponse.FromJson(stream);
            }
        }

        public static async Task<Api.ListBucketsResponse> ListBucketsAsync(string accountId, string apiUrl, string authorizationToken)
        {
            using (var client = new B2HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(authorizationToken);

                var json = Api.ToJson(new Api.ListBucketsRequest { accountId = accountId });

                var content = new StreamContent(json.Item2);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                content.Headers.ContentLength = json.Item1;
                
                var response = await client.PostAsync($"{apiUrl}/b2api/v1/b2_list_buckets", content).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                return Api.ListBucketsResponse.FromJson(stream);
            }
        }
    }
}
