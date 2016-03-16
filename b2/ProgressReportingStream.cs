using System;
using System.IO;

namespace com.wibblr.b2
{
    /// <summary>
    /// A Stream wrapper that allows querying of progress and bandwidth
    /// </summary>
    public class ProgressReportingStream : Stream
    {
        private const int TICKS_PER_MILLISECOND = 10000;
        private const int TICKS_PER_SECOND = 10000000;

        private Stream s;
        private StreamProgressCalculator progressCalc;

        public ProgressReportingStream(Stream underlying)
        {
            s = underlying;
            progressCalc = new StreamProgressCalculator(DateTime.UtcNow.Ticks);
        }

        public override bool CanRead { get { return s.CanRead; } }

        public override bool CanSeek { get { return s.CanSeek; } }

        public override bool CanWrite { get { return s.CanWrite; } }

        public override long Length { get { return s.Length; } }

        public override long Position { get { return s.Position; }
            set
            {
                progressCalc.Reset(DateTime.UtcNow.Ticks);
                s.Position = value;
            }
        }

        public override void Flush() => s.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesRead = s.Read(buffer, offset, count);
            progressCalc.SetBytesTransferred(DateTime.UtcNow.Ticks, bytesRead, s.Position, s.Length);
            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            progressCalc.Reset(DateTime.UtcNow.Ticks);
            return s.Seek(offset, origin);
        }

        public override void SetLength(long value) => s.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
        {
            s.Write(buffer, offset, count);
            progressCalc.SetBytesTransferred(DateTime.UtcNow.Ticks, count, s.Position, s.Length);
        }

        public StreamProgress Progress()
        {
            return progressCalc.Progress();
        }
    }
}
