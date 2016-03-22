using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

using NUnit.Framework;

namespace com.wibblr.b2
{
    public class UrlEncodingTestData
    {
        public IList<UrlEncodingTestCase> testCases;
    }

    [DataContract]
    public class UrlEncodingTestCase
    {
        [DataMember] public string fullyEncoded;
        [DataMember] public string minimallyEncoded;
        [DataMember (Name="string")] public string s;
    }

    [TestFixture]
    public class UrlEncodingTests
    {
        private UrlEncodingTestData testData;

        [SetUp]
        public void Setup()
        {
            var testDataFile = Path.Combine(new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName, "url-encoding.json");
            using (var s = new FileStream(testDataFile, FileMode.Open))
            {
                testData = (UrlEncodingTestData)new DataContractJsonSerializer(typeof(UrlEncodingTestData)).ReadObject(s);
            }
        }

        [Test]
        public void UrlEncode()
        {
            foreach (var testCase in testData.testCases)
            {
                var encoded = B2UrlEncoder.Encode(testCase.s);
                var acceptableEncodings = new[] { testCase.minimallyEncoded, testCase.fullyEncoded };
                Assert.Contains(encoded, acceptableEncodings);
            } 
        }

        [Test]
        public void UrlDecodeFull()
        {
            foreach (var testCase in testData.testCases)
            {
                var decoded = B2UrlEncoder.Decode(testCase.fullyEncoded);
                Assert.AreEqual(testCase.s, decoded);
            }
        }

        [Test]
        public void UrlDecodeMinimal()
        {
            foreach (var testCase in testData.testCases)
            {
                var decoded = B2UrlEncoder.Decode(testCase.minimallyEncoded);
                Assert.AreEqual(testCase.s, decoded);
            }
        }
    }
}
