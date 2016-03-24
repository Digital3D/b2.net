using System;
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
            using (var s = new FileStream(testDataFile, FileMode.Open, FileAccess.Read))
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

        [Test]
        public void ShouldThrowOnInvalidHexDigit()
        {
            Assert.That(() => B2UrlEncoder.Decode("%2!2"),
                Throws.Exception
                    .TypeOf<ArgumentException>()
                    .With.Message.EqualTo("Invalid URL encoded string '%2!2' at position 2 - Unable to parse '!' as a hex digit"));
        }

        [Test]
        public void ShouldThrowOnTruncatedString()
        {
            Assert.That(() => B2UrlEncoder.Decode("%2"),
                Throws.Exception
                    .TypeOf<ArgumentException>()
                    .With.Message.EqualTo("Invalid URL encoded string '%2' - Expected hex digit but string was truncated"));
        }

        [Test]
        public void ShouldThrowOnInvalidAsciiCharacter()
        {
            Assert.That(() => B2UrlEncoder.Decode("abc\n"),
                Throws.Exception
                    .TypeOf<ArgumentException>()
                    .With.Message.EqualTo("Invalid URL encoded string 'abc\n' - invalid character '\n' at position 3"));
        }

        [Test]
        public void ShouldThrowOn16BitCharacter()
        {
            Assert.That(() => B2UrlEncoder.Decode("abc\u263A"),
                Throws.Exception
                    .TypeOf<ArgumentException>()
                    .With.Message.EqualTo("Invalid URL encoded string 'abc\u263A': found 16-bit code point at position 3"));
        }

        [Test]
        public void ShouldThrowOnEncodeInvalidHexDigit()
        {
            Assert.That(() => B2UrlEncoder.EncodeHexDigit(-1),
                Throws.Exception
                    .TypeOf<ArgumentException>()
                    .With.Message.EqualTo("Cannot convert integer -1 to hex digit"));
        }
    }
}
