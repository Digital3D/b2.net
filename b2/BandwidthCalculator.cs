using System;

namespace com.wibblr.b2
{
    /// <summary>
    /// Calculate the bandwidth of a stream.
    /// 
    /// The caller should repeatedly specify a count of transferred bytes, and the time window in which
    /// the transfer took place. The time window is simply an incrementing number which
    /// represents a period of time, e.g. window 1 is 0ms to 100ms, window 2 is 100ms to 200ms, etc.
    /// 
    /// The bandwidth (bytes-per-window) is simply the average value of all completed windows.
    /// The current (partially completed) window is excluded.
    /// </summary>
    public class BandwidthCalculator
    {
        private AccumulatingCircularBuffer<long, float> previousBytesTransferred;

        internal long currentWindow = -1; // the time window represented by the top of the queue
        internal Accumulator<long, long> currentBytesAccumulator = new SumAccumulator();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="capacity">Number of windows over which to calculate the average bandwidth</param>
        public BandwidthCalculator(int capacity)
        {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException("capacity");

            previousBytesTransferred = new AccumulatingCircularBuffer<long, float>(capacity, new AverageAccumulator());
        }

        /// <summary>
        /// Clear all internal state
        /// </summary>
        public void Clear()
        {
            previousBytesTransferred.Clear();
            currentBytesAccumulator.Clear();
            currentWindow = -1;
        }

        /// <summary>
        /// Specify that some bytes were transferred in a particular window.
        /// </summary>
        /// <param name="window">Number representing time, e.g. 1 = 100ms to 200ms, 2 = 200ms to 300ms, etc</param>
        /// <param name="value">Count of bytes transferred</param>
        public void Accumulate(long window, long value)
        {
            if (window < 0)
                throw new ArgumentOutOfRangeException("window");

            // Special case: initialize the top window on the first call to Accumulate()
            if (currentWindow == -1)
                currentWindow = window;

            // If trying to accumulate old data, put it in the current window
            if (window < currentWindow) 
                window = currentWindow;

            // If these bytes are not in the current window, put the total bytes transferred
            // in the current window to the previous bytes queue
            if (window > currentWindow)
            {
                previousBytesTransferred.Add(currentBytesAccumulator.Total());
                currentBytesAccumulator.Clear();
                currentWindow++;
            }

            // If there is a gap between the current window and this window, enqueue
            // the appropriate number of empty windows
            if (window != currentWindow)
            {
                previousBytesTransferred.AddN(window - currentWindow);
                currentWindow = window;
            }

            // finally accumulate the bytes transferred
            currentBytesAccumulator.Accumulate(value);
        }

        /// <summary>
        /// Return the average bytes-per-window. If each window is 100ms, then this 
        /// should be multiplied by 10 to get bytes-per-second.
        /// </summary>
        /// <returns>Average bandwidth over the previous n windows, where n is the 
        /// capacity of the class, in units of bytes per window</returns>
        public float BytesPerWindow()
        {
            return previousBytesTransferred.AccumulatedValue();
        }
    }
}
