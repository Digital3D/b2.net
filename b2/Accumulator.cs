namespace com.wibblr.b2
{
    public interface Accumulator<T, U>
    {
        void Accumulate(T newValue);
        void Rotate(T newValue, T oldValue);
        void Clear();
        U Total();
    }

    /// <summary>
    /// Calculates the average of a set of values.
    /// By rotating, instead of accumulating, the oldest value is removed from the 
    /// accumulator when a new one is added, giving the average of only the must recent values.
    /// </summary>
    public class AverageAccumulator : Accumulator<long, float>
    {
        private long sum;
        private int count;
        public void Accumulate(long newValue)
        {
            sum += newValue;
            count++;
        }

        public void Rotate(long newValue, long oldValue) => sum += (newValue - oldValue);

        public void Clear()
        {
            sum = 0;
            count = 0;
        }
        public float Total() => count == 0 ? 0 : sum / (float)count;
    }

    /// <summary>
    /// Calculates the sum of a set of values.
    /// By rotating, instead of accumulating, the oldest value is removed from the 
    /// accumulator when a new one is added, giving the sum of only the must recent values.
    /// </summary>
    public class SumAccumulator : Accumulator<long, long>
    {
        private long sum;
        public void Accumulate(long newValue) => sum += newValue;
        public void Clear() => sum = 0;
        public void Rotate(long newValue, long oldValue) => sum += (newValue - oldValue);
        public long Total() => sum;
    }

}
