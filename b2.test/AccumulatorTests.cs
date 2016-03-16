using NUnit.Framework;

namespace com.wibblr.b2
{
    [TestFixture]
    public class AccumulatorTests
    {
        [Test]
        public void TestSumAccumulator()
        {
            var a = new SumAccumulator();
            Assert.AreEqual(0, a.Total());

            a.Rotate(10, 20);
            Assert.AreEqual(-10, a.Total());

            a.Rotate(55, 5);
            Assert.AreEqual(40, a.Total());

            a.Accumulate(20);
            Assert.AreEqual(60, a.Total());

            a.Clear();
            Assert.AreEqual(0, a.Total());

            a.Rotate(10, 20);
            Assert.AreEqual(-10, a.Total());

            a.Rotate(55, 5);
            Assert.AreEqual(40, a.Total());

            a.Accumulate(20);
            Assert.AreEqual(60, a.Total());
        }

        [Test]
        public void TestAverageAccumulator()
        {
            var a = new AverageAccumulator();
            Assert.AreEqual(0, a.Total());

            a.Rotate(10, 20);
            Assert.AreEqual(0f, a.Total()); // count 0, sum -10

            a.Accumulate(20);
            Assert.AreEqual(10f, a.Total()); // count 1, sum 10

            a.Accumulate(30);
            Assert.AreEqual(20f, a.Total()); // count 2, sum 40

            a.Rotate(35, 20);
            Assert.AreEqual(27.5f, a.Total()); // count 2, sum 55

            a.Clear();
            Assert.AreEqual(0, a.Total());

            a.Rotate(10, 20);
            Assert.AreEqual(0f, a.Total()); // count 0, sum -10

            a.Accumulate(20);
            Assert.AreEqual(10f, a.Total()); // count 1, sum 10

            a.Accumulate(30);
            Assert.AreEqual(20f, a.Total()); // count 2, sum 40

            a.Rotate(35, 20);
            Assert.AreEqual(27.5f, a.Total()); // count 2, sum 55
        }
    }
}
