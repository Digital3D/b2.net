using NUnit.Framework;

namespace com.wibblr.b2
{
    [TestFixture]
    public class BandwidthCalculatorTests
    {
        [Test]
        public void TestWithIncrementingWindows()
        {
            var c = new BandwidthCalculator(3);

            c.Accumulate(43, 100);
            Assert.AreEqual(0, c.BytesPerWindow());

            c.Accumulate(43, 200);
            Assert.AreEqual(0, c.BytesPerWindow());

            c.Accumulate(44, 300);
            Assert.AreEqual(300f, c.BytesPerWindow()); // Average is calculated as the average of the latest 4 windows excluding the current one

            c.Accumulate(44, 400);
            Assert.AreEqual(300f, c.BytesPerWindow()); // Average is calculated as the average of the latest 4 windows excluding the current one

            c.Accumulate(45, 500);
            Assert.AreEqual(500f, c.BytesPerWindow()); // 1000 bytes total in 2 windows (43-44)

            c.Accumulate(46, 700);
            Assert.AreEqual(1500 / 3f, c.BytesPerWindow()); // 1500 bytes total in 3 windows (43-45)

            c.Accumulate(47, 800);
            Assert.AreEqual(1900 / 3f, c.BytesPerWindow()); // 1700 bytes total in 3 windows (44-46); window 43 has dropped off

            // Test that Clear() works
            c.Clear();
            Assert.AreEqual(0, c.BytesPerWindow());

            c.Accumulate(43, 100);
            Assert.AreEqual(0, c.BytesPerWindow());

            c.Accumulate(43, 200);
            Assert.AreEqual(0, c.BytesPerWindow());

            c.Accumulate(44, 300);
            Assert.AreEqual(300f, c.BytesPerWindow()); // Average is calculated as the average of the latest 4 windows excluding the current one

            c.Accumulate(44, 400);
            Assert.AreEqual(300f, c.BytesPerWindow()); // Average is calculated as the average of the latest 4 windows excluding the current one

            c.Accumulate(45, 500);
            Assert.AreEqual(500f, c.BytesPerWindow()); // 1000 bytes total in 2 windows (43-44)
        }

        [Test]
        public void TestWithOneMissingWindow()
        {
            var c = new BandwidthCalculator(3);
            c.Accumulate(107, 100);
            Assert.AreEqual(0, c.BytesPerWindow());

            c.Accumulate(109, 100);
            Assert.AreEqual(100/2f, c.BytesPerWindow()); // 100 bytes in 2 windows (107-108)

            c.Clear();
            c.Accumulate(107, 100);
            c.Accumulate(108, 200);
            c.Accumulate(110, 300);
            Assert.AreEqual(300/3f, c.BytesPerWindow()); // 300 bytes in 3 windows (107-109)

            c.Clear();
            c.Accumulate(107, 100);
            c.Accumulate(108, 200);
            c.Accumulate(109, 300);
            c.Accumulate(111, 400);
            Assert.AreEqual(500 / 3f, c.BytesPerWindow()); // 500 bytes in 3 windows (108-110)
        }

        [Test]
        public void TestWithTwoMissingWindows()
        {
            var c = new BandwidthCalculator(3);
            c.Accumulate(107, 100);
            Assert.AreEqual(0, c.BytesPerWindow());

            c.Accumulate(110, 100);
            Assert.AreEqual(100 / 3f, c.BytesPerWindow()); // 100 bytes in 3 windows (107-109)

            c.Clear();
            c.Accumulate(107, 100);
            c.Accumulate(108, 200);
            c.Accumulate(111, 300);
            Assert.AreEqual(200 / 3f, c.BytesPerWindow()); // 200 bytes in 3 windows (108-110)

            c.Clear();
            c.Accumulate(107, 100);
            c.Accumulate(108, 200);
            c.Accumulate(109, 300);
            c.Accumulate(112, 400);
            Assert.AreEqual(300 / 3f, c.BytesPerWindow()); // 300 bytes in 3 windows (109-111)
        }

        [Test]
        public void TestWithThreeMissingWindows()
        {
            var c = new BandwidthCalculator(3);
            c.Accumulate(107, 100);
            Assert.AreEqual(0, c.BytesPerWindow());

            c.Accumulate(111, 100);
            Assert.AreEqual(0f, c.BytesPerWindow()); // 0 bytes in 3 windows (108-110)

            c.Clear();
            c.Accumulate(107, 100);
            c.Accumulate(108, 200);
            c.Accumulate(112, 300);
            Assert.AreEqual(0f, c.BytesPerWindow()); // 0 bytes in 3 windows (109-111)
        }
    }
}
