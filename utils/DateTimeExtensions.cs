using System;

namespace com.wibblr.utils
{
    /// <summary>
    /// Extension methods for DateTime
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Convert a UTC DateTime to a Unix/Java timestamp (i.e. milliseconds since 1970)
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static long ToUnixTimeMillis(this DateTime dt) => (dt.Ticks - 621355968000000000) / 10000;
    }
}
