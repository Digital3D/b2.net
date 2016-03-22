using System;
using System.Numerics;
using System.Text;
using System.Linq;

namespace com.wibblr.utils
{
    /// <summary>
    /// Generate random strings containing hex characters
    /// </summary>
    public class RandomString
    {
        private static Random r = new Random((int)DateTime.Now.Ticks + Environment.TickCount);

        /// <summary>
        /// Return a random string where each char is a hex digit.
        /// </summary>
        /// <param name="numBytes">Number of random bytes in the returned string; the string will contain 2 hex digits for each byte</param>
        /// <returns></returns>
        public static string Next(int numBytes = 4)
        {
            if (numBytes < 1 || numBytes > 16)
                throw new ArgumentOutOfRangeException("numBytes must be between 1 and 16");

            var buffer = new byte[numBytes];
            var sb = new StringBuilder(numBytes * 2);

            r.NextBytes(buffer);
            foreach (var b in buffer)
                sb.AppendFormat("{0:X2}", b);

            return sb.ToString();
        }
    }
}
