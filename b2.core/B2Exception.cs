using System;

namespace com.wibblr.b2
{
    /// <summary>
    /// 
    /// </summary>
    public class B2Exception : Exception
    {
        public int HttpStatusCode { get; private set; }
        public string ErrorCode { get; private set; }

        public B2Exception(int httpStatusCode, string errorCode, string message)
            : base(message)
        {
            HttpStatusCode = httpStatusCode;
            ErrorCode = errorCode;
        }
    }
}
