using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;

namespace com.wibblr.utils
{
    /// <summary>
    /// Extension methods for HttpRequestMessage
    /// </summary>
    public static class HttpRequestMessageExtensions
    {
        /// <summary>
        /// Add a basic authentication request header to the message in a fluent style
        /// </summary>
        /// <param name="message"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static HttpRequestMessage WithBasicAuthorization(this HttpRequestMessage message, string value)
        {
            if (value != null)
                message.Headers.Authorization = new AuthenticationHeaderValue("Basic", value);
            return message;
        }

        /// <summary>
        /// Add an authentication request header to the message in a fluent style
        /// </summary>
        /// <param name="message"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static HttpRequestMessage WithAuthorization(this HttpRequestMessage message, string value)
        {
            if (value != null)
                message.Headers.TryAddWithoutValidation("Authorization", value);
            return message;
        }

        /// <summary>
        /// Add a body to the request consisting of a JSON-serialized request object.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static HttpRequestMessage WithJsonSerializedContent(this HttpRequestMessage message, object obj)
        {
            var stream = new MemoryStream();
            new DataContractJsonSerializer(obj.GetType()).WriteObject(stream, obj);
            stream.Seek(0, 0);
            message.Content = new StreamContent(stream);
            return message;
        }

        /// <summary>
        /// Add a body to the request
        /// </summary>
        /// <param name="message"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static HttpRequestMessage WithContent(this HttpRequestMessage message, Stream content)
        {
            message.Content = new StreamContent(content);
            return message;
        }

        /// <summary>
        /// Add content headers to the request
        /// </summary>
        /// <param name="message"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public static HttpRequestMessage WithContentHeaders(this HttpRequestMessage message, Dictionary<string, string> headers)
        {
            foreach (var h in headers)
                message.Content.Headers.Add(h.Key, h.Value);

            return message;
        }

        /// <summary>
        /// Add a Range header to the request
        /// </summary>
        /// <param name="message"></param>
        /// <param name="lower"></param>
        /// <param name="upper"></param>
        /// <returns></returns>
        public static HttpRequestMessage WithRange(this HttpRequestMessage message, long? lower, long? upper)
        {
            if (lower.HasValue || upper.HasValue)
            {
                var strLower = lower.HasValue ? lower.Value.ToString() : "";
                var strUpper = upper.HasValue ? upper.Value.ToString() : "";

                message.Headers.Add("Range", $"bytes={strLower}-{strUpper}");
            }
            return message;
        }
    }
}
