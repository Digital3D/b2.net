using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace com.wibblr.b2
{
    [TestFixture]
    public class StreamProgressCalculatorTests
    {
        // calculate ticks from seconds
        private long Ticks(string s)
        {
            return (long)(decimal.Parse(s) * 10000000);
        }

        private void AssertProgress(float progress, float bytesPerSecond, float? completedBytesPerSecond, StreamProgress actual)
        {
            Assert.AreEqual(progress, actual.progress);
            Assert.AreEqual(bytesPerSecond, actual.bytesPerSecond);
            Assert.AreEqual(completedBytesPerSecond, actual.completedBytesPerSecond);
        }

        [Test]
        public void ProgressBeforeAnyBytesTransferredShouldBeZero()
        {
            var c = new StreamProgressCalculator(Ticks("1.0"), 3, 100);
            AssertProgress(0, 0, null, c.Progress());
        }

        [Test]
        public void ProgressTest()
        {
            var c = new StreamProgressCalculator(Ticks("1.01"), 3, 100);

            c.SetBytesTransferred(Ticks("1.01"), 20 /* bytes */, 20 /* position */, 80 /* length */);
            AssertProgress(20 / 80f, 0, null, c.Progress());

            c.SetBytesTransferred(Ticks("1.01"), 10, 30, 80);
            AssertProgress(30 / 80f, 0, null, c.Progress());

            c.SetBytesTransferred(Ticks("1.11"), 10, 40, 80);
            AssertProgress(40 / 80f, 10 * 30, null, c.Progress()); // progress = 40/80, bandwidth = 30 bytes in 1 window (1.01)

            c.SetBytesTransferred(Ticks("1.11"), 10, 50, 80);
            AssertProgress(50 / 80f, 10 * 30, null, c.Progress()); // progress = 50/80, bandwidth = 30 bytes in 1 window (1.01)

            c.SetBytesTransferred(Ticks("1.31"), 10, 60, 80);
            AssertProgress(60 / 80f, 10 * 50 / 3, null, c.Progress()); // progress = 60/80, bandwidth = 50 bytes in 3 windows (1.01-1.21)

            c.SetBytesTransferred(Ticks("1.31"), 10, 70, 80);
            AssertProgress(70 / 80f, 10 * 50 / 3, null, c.Progress()); // progress = 70/80, bandwidth = 50 bytes in 3 windows (1.01-1.21)

            c.SetBytesTransferred(Ticks("1.41"), 10, 80, 80);
            AssertProgress(80 / 80f, 10 * 40 / 3, 800 / 4f, c.Progress()); // progress = 80/80, bandwidth = 40 bytes in 3 windows (1.11-1.31), total bandwidth = 80 bytes in 0.4 seconds (1.01-1.41)
        }
    }
}
