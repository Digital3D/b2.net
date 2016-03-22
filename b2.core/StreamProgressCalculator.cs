using System.Collections.Generic;

namespace com.wibblr.b2
{
    /// <summary>
    /// Struct containing the data that is reported in progress callbacks
    /// </summary>
    public struct StreamProgress
    {
        public float progress;
        public float bytesPerSecond;
        public float? completedBytesPerSecond;
    }

    /// <summary>
    /// Calculate the progress and bandwidth of a connection.
    /// Bandwidth is calculated by dividing time into windows of 300ms, and storing the total bytes transferred
    /// during each window. Only the 6 most recent values are kept.
    /// </summary>
    public class StreamProgressCalculator
    {
        private const int TICKS_PER_MILLISECOND = 10000;
        private const int TICKS_PER_SECOND = 10000000;

        private object l = new object();

        private BandwidthCalculator bandwidthCalculator;
        private long startTicks;
        private long position;
        private long length;
        private long endTicks;
        private int numWindows;
        private int millisecondsPerWindow;
        private int ticksPerWindow;
        private float windowsPerSecond;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ticks">The time from which to start calculating the bandwidth (e.g. DateTime.Now)</param>
        public StreamProgressCalculator(long ticks, int numWindows = 6, int millisecondsPerWindow = 300)
        {
            this.numWindows = numWindows;
            this.millisecondsPerWindow = millisecondsPerWindow;
            ticksPerWindow = TICKS_PER_MILLISECOND * millisecondsPerWindow;
            windowsPerSecond = 1000 / millisecondsPerWindow;
            bandwidthCalculator = new BandwidthCalculator(numWindows);
            Reset(ticks);
        }

        /// <summary>
        /// Reset the bandwidth calculation, e.g. after a stream seek
        /// </summary>
        /// <param name="ticks">The time at which the reset is done (e.g. DateTime.Now)</param>
        public void Reset(long ticks)
        {
            startTicks = ticks;
            bandwidthCalculator.Clear();
            endTicks = 0;
            position = 0;
        }

        /// <summary>
        /// The stream class uses this to indicate that some bytes were transferred at a specific time.
        /// This is used to calculate the bandwidth.
        /// </summary>
        /// <param name="ticks">The time at which the bytes were transferred (e.g. DateTime.Now)</param>
        /// <param name="count">The number of bytes that were transferred</param>
        public void SetBytesTransferred(long ticks, int count, long position, long length)
        {
            // convert tick to a window number
            int window = (int) ((ticks - startTicks) / ticksPerWindow);
            lock(l)
            {
                bandwidthCalculator.Accumulate(window, count);
                this.position = position;
                this.length = length;
                if (position == length)
                    endTicks = ticks;
            }
        }

        /// <summary>
        /// Return the average bandwidth, excluding the current window
        /// </summary>
        /// <returns>The average bytes per second over the previous n-1 windows (current window
        /// is not included in the calculation</returns>
        public StreamProgress Progress()
        {
            lock(l)
            {
                return new StreamProgress {
                    progress = length == 0 ? 0 : position / (float) length,
                    bytesPerSecond = (int) (bandwidthCalculator.BytesPerWindow() * windowsPerSecond),
                    completedBytesPerSecond = (endTicks == 0) ? (float?) null : (length / ((endTicks - startTicks) / (float)TICKS_PER_SECOND))
                };
            }
        }
    }
}
