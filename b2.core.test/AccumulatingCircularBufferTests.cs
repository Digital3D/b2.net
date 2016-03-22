using System;

using NUnit.Framework;

namespace com.wibblr.b2
{
    [TestFixture]
    public class AccumulatingCircularBufferTests
    {
        [Test]
        public void CapacityShouldNotBeZero()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var a = new AccumulatingCircularBuffer<long, long>(0, new SumAccumulator());
            });
        }

        private void CheckBuffer(AccumulatingCircularBuffer<long, long> buffer, long[] values, long accumulatedValue)
        {
            CollectionAssert.AreEqual(values, buffer.ToArray());
            Assert.AreEqual(accumulatedValue, buffer.AccumulatedValue());
        }

        [Test]
        public void TestBufferWithSumAccumulator()
        {
            var a = new AccumulatingCircularBuffer<long, long>(3, new SumAccumulator());
            CheckBuffer(a, new long[] { }, 0);
            a.Add(10);
            CheckBuffer(a, new long[] { 10 }, 10);
            a.Add(20);
            CheckBuffer(a, new long[] { 10, 20 }, 30);
            a.Add(30);
            CheckBuffer(a, new long[] { 10, 20, 30 }, 60);
            a.Add(40);
            CheckBuffer(a, new long[] { 20, 30, 40 }, 90);
            a.Add(50);
            CheckBuffer(a, new long[] { 30, 40, 50 }, 120);
        }

        private void CheckBuffer(AccumulatingCircularBuffer<long, float> buffer, long[] values, float accumulatedValue)
        {
            CollectionAssert.AreEqual(values, buffer.ToArray());
            Assert.AreEqual(accumulatedValue, buffer.AccumulatedValue());
        }

        [Test]
        public void TestBufferWithAverageAccumulator()
        {
            var a = new AccumulatingCircularBuffer<long, float>(4, new AverageAccumulator());
            CheckBuffer(a, new long[] { }, 0f);
            a.Add(10);
            CheckBuffer(a, new long[] { 10 }, 10f);
            a.Add(21);
            CheckBuffer(a, new long[] { 10, 21 }, 15.5f);
            a.Add(30);
            CheckBuffer(a, new long[] { 10, 21, 30 }, 61/3f);
            a.Add(40);
            CheckBuffer(a, new long[] { 10, 21, 30, 40 }, 25.25f);
            a.Add(50);
            CheckBuffer(a, new long[] { 21, 30, 40, 50 }, 35.25f);
        }

        [Test]
        public void TestAddN()
        {
            var a = new AccumulatingCircularBuffer<long, long>(3, new SumAccumulator());
            CheckBuffer(a, new long[] { }, 0);

            a.AddN(0);
            CheckBuffer(a, new long[] { }, 0);

            a.AddN(1);
            CheckBuffer(a, new long[] { 0 }, 0);

            a.Add(10);
            a.Add(20);
            a.Add(30);
            CheckBuffer(a, new long[] { 10, 20, 30 }, 60);
            a.AddN(1);
            CheckBuffer(a, new long[] { 20, 30, 0 }, 50);

            a.Add(10);
            a.Add(20);
            a.Add(30);
            CheckBuffer(a, new long[] { 10, 20, 30 }, 60);
            a.AddN(2);
            CheckBuffer(a, new long[] { 30, 0, 0 }, 30);

            a.Add(10);
            a.Add(20);
            a.Add(30);
            CheckBuffer(a, new long[] { 10, 20, 30 }, 60);
            a.AddN(3);
            CheckBuffer(a, new long[] { 0, 0, 0 }, 0);

            // Test that adding a huge number is optimised away
            a.Add(10);
            a.Add(20);
            a.Add(30);
            CheckBuffer(a, new long[] { 10, 20, 30 }, 60);
            a.AddN(long.MaxValue);
            CheckBuffer(a, new long[] { 0, 0, 0 }, 0);
        }
    }
}
