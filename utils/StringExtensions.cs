using System;
using System.IO;
using System.Text;

namespace com.wibblr.utils
{
    /// <summary>
    /// Extension methods for String
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Convert a string to base-64
        /// </summary>
        /// <param name="s">String to convert</param>
        /// <returns>Base-64 representation of the string</returns>
        public static string ToBase64(this string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string ToWindowsPath(this string s)
        {
            if (s.StartsWith("/") || s.StartsWith("\\") || s.Contains(":"))
                throw new ArgumentException("No drive letters or absolute paths allowed");

            return s.Replace('/', '\\');
        }

        /// <summary>
        /// Convert a file path to a format that has the separators correctly formatted for unix
        /// (note there may be other incompatibilities, e.g. case sensitivity, maximum lengths etc)
        /// Only relative paths are allowed
        /// e.g. a\b\c.txt -> a/b/c.txt
        ///      \a\b\c.txt -> invalid (needs a drive specification)
        ///      c:\a\b.txt -> /c/a/b.txt
        ///      c:a\b.txt -> invalid (needs an absolute path if drive is specified)
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string ToUnixPath(this string s)
        {
            if (s.StartsWith("/") || s.StartsWith("\\") || s.Contains(":"))
                throw new ArgumentException("No drive letters or absolute paths allowed");

            return s.Replace('\\', '/');
        }

        /// <summary>
        /// Convert a (relative) path to the native directory separator convention
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string ToNativePath(this string s) => Path.DirectorySeparatorChar == '/' ? ToUnixPath(s) : ToWindowsPath(s);
    }
}
