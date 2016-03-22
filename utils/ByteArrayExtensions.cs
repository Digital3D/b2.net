using System.Text;

namespace com.wibblr.utils
{
    /// <summary>
    /// Extension methods for byte arrays 
    /// </summary>
    public static class ByteArrayExtensions
    {
        /// <summary>
        /// Convert a byte array to a hex string
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static string ToHex(this byte[] buffer)
        {
            var sb = new StringBuilder(buffer.Length * 2);

            foreach (var b in buffer)
                sb.AppendFormat("{0:X2}", b);

            return sb.ToString();
        }
    }
}
