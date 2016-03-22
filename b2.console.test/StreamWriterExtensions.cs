using System.IO;

namespace com.wibblr.b2.console
{
    /// <summary>
    /// Extension methods for StreamWriter
    /// </summary>
    public static class StreamWriterExtensions
    {
        public static StreamWriter WithAutoFlush(this StreamWriter streamWriter, bool autoFlush = true)
        {
            streamWriter.AutoFlush = autoFlush;
            return streamWriter;
        }
    }
}
