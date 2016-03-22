using System;
using System.Collections.Generic;

namespace com.wibblr.b2
{
    /// <summary>
    /// Buffer that stores the most recent N items added. If an item is added when the buffer
    /// is at its capacity, the oldest existing item will be dropped. 
    /// 
    /// The items are processed using an accumulator class that automatically deaccumulates
    /// dropped items, allowing easy calculation of the sum or average of items
    /// currently in the buffer.
    /// </summary>
    /// <typeparam name="T">The type of item to be stored/accumulated</typeparam>
    /// <typeparam name="U">The type of the accumulated total</typeparam>
    public class AccumulatingCircularBuffer<T, U>
    {
        private Queue<T> queue;
        private Accumulator<T, U> queueAccumulator;
        private int queueCapacity;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="capacity">Maximum number of items to store in buffer</param>
        /// <param name="accumulator">An accumulator to allow easy access to the sum or average of
        /// the items in the buffer</param>
        public AccumulatingCircularBuffer(int capacity, Accumulator<T, U> accumulator)
        {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException("capacity");

            queue = new Queue<T>(capacity);
            queueCapacity = capacity;
            queueAccumulator = accumulator;
        }

        internal T[] ToArray()
        {
            return queue.ToArray();
        }

        /// <summary>
        /// Remove all items from the buffer. The accumulator is also cleared.
        /// </summary>
        public void Clear()
        {
            queue.Clear();
            queueAccumulator.Clear();
        }

        /// <summary>
        /// Add an item to the buffer. It is also added to the accumulator.
        /// If the buffer is at its capacity, the oldest item is removed from
        /// both the buffer and the accumulator.
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            if (queue.Count >= queueCapacity)
                queueAccumulator.Rotate(item, queue.Dequeue());
            else
                queueAccumulator.Accumulate(item);

            queue.Enqueue(item);
        }

        /// <summary>
        /// Add a number of zero-valued items to the buffer.
        /// Optimised to simply clear the buffer if the number of items is large enough
        /// </summary>
        /// <param name="count">Number of zero-valued items to add</param>
        public void AddN(long count)
        {
            if (count == 0)
                return;
            else if (count >= queueCapacity)
                Clear();

            for (long i = 0; i < Math.Min(count, queueCapacity); i++)
                Add(default(T));
        }

        /// <summary>
        /// Get the accumulated value of the contents of the buffer.
        /// </summary>
        /// <returns></returns>
        public U AccumulatedValue() => queueAccumulator.Total();
    }
}
