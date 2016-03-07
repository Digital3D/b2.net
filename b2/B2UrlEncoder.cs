using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.wibblr.b2
{
    /// <summary>
    /// A Url encoder that passes the vendor supplied test cases (see https://www.backblaze.com/b2/docs/string_encoding.html)
    /// 
    /// The library functions Uri.EscapeUriString or Uri.EscapeDataString both fail these tests.
    /// </summary>
    public class B2UrlEncoder
    {
        static HashSet<byte> literals = new HashSet<byte>("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789._-/~!$'()*;=:@".Select(c => (byte)c));

        private static char EncodeHexDigit(int b)
        {
            if (b > 15 || b < 0)
                throw new ArgumentException($"Cannot convert integer {b} to hex digit");

            return (char)((b >= 10) ? (b - 10 + 'A') : (b + '0'));
        }

        private static int DecodeHexDigit(char c)
        {
            if (c >= '0' && c <= '9')
                return c - '0';
            else if (c >= 'A' && c <= 'F')
                return c - 'A' + 10;

            throw new ArgumentException($"Cannot convert hex digit {c} to integer");
        }

        public static string Encode(string s)
        {
            var sb = new StringBuilder();

            foreach (var b in Encoding.UTF8.GetBytes(s))
            {
                if (literals.Contains(b))
                {
                    sb.Append((char)b);
                }
                else
                {
                    sb.Append('%');
                    sb.Append(EncodeHexDigit(b / 16));
                    sb.Append(EncodeHexDigit(b % 16));
                }
            }

            return sb.ToString();
        }

        public static string Decode(string s)
        {
            var len = s.Length;
            
            // Allocate enough space to hold the decoded bytes
            // This must be equal or less to the space used by the encoded string, so 
            // use that.
            var bytes = new byte[len];
            var pos = 0;

            for (int i = 0; i < s.Length; i++)
            {
                var c = s[i];
                var b = (byte)c;
                if (b != c)
                    throw new ArgumentException($"Invalid URL encoded string '{s}': non-ascii character at position {i}");

                if (literals.Contains(b))
                    bytes[pos++] = b;
                else if (c == '+') // special case
                    bytes[pos++] = (byte)' ';
                else
                {
                    if (c != '%')
                        throw new ArgumentException($"Invalid URL encoded string '{s}': expected '%' at position {i}");
                    if ((i + 2) >= len)
                        throw new ArgumentException($"Invalid URL encoded string '{s}': last encoded character is truncated");
                    var upperNybble = DecodeHexDigit(s[++i]);
                    var lowerNybble = DecodeHexDigit(s[++i]);
                    bytes[pos++] = (byte)((upperNybble * 16) + lowerNybble);
                }
            }
            return Encoding.UTF8.GetString(bytes, 0, pos);
        }
    }
}
