using System;
using HtmlParserSharp.Portable;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HtmlParserSharp.Tests.TestData
{
    [TestClass]
    public class CharsetEncodingTests
    {
        class EncodingTestCase
        {
            public string Html { get; set; }
            public string Encoding { get; set; }
        }

        enum TestDataParserStates
        {
            ReadingHtml,
            ReaadingEncoding,
            ReadingWhitespace
        }

        private List<EncodingTestCase> ParseTestData(string path)
        {
            var result = new List<EncodingTestCase>();
            var parserState = TestDataParserStates.ReadingWhitespace;

            using (var reader = new StreamReader(path))
            {
                var testCase = new EncodingTestCase();
                var htmlBuilder = new StringBuilder();

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line.Contains("#data"))
                    {
                        parserState = TestDataParserStates.ReadingHtml;
                        htmlBuilder.Clear();
                    }
                    else if (line.Contains("#encoding"))
                    {
                        parserState = TestDataParserStates.ReaadingEncoding;
                        testCase.Html = htmlBuilder.ToString();
                    }
                    else
                    {
                        if (parserState == TestDataParserStates.ReaadingEncoding)
                        {
                            testCase.Encoding = line;
                            parserState = TestDataParserStates.ReadingWhitespace;
                            result.Add(testCase);
                            testCase = new EncodingTestCase();
                        } 
                        else if (parserState == TestDataParserStates.ReadingHtml)
                        {
                            htmlBuilder.Append(line + "\n");
                        }
                    }
                }
            }
            return result;
        }

        public async Task RunTestFile(string path)
        {
            List<EncodingTestCase> testCases = ParseTestData(path);
            var parser = new SimpleHtmlParser();
            int counter = 0;
            List<int> errorOffsets = new List<int>();
            foreach (var testCase in testCases)
            {
                var doc = await parser.ParseString(testCase.Html);
                if (!string.Equals(testCase.Encoding, parser.DocumentEncoding, StringComparison.CurrentCultureIgnoreCase))
                {
                    errorOffsets.Add(counter);
                }
                counter++;
            }
            Assert.AreEqual(0, errorOffsets.Count, "Number of errors is {0}/{1}", errorOffsets.Count, testCases.Count);
        }

        [TestMethod]
        [DeploymentItem("TestData")]
        public async Task EncodingTests1()
        {
            await RunTestFile("Encodings\\tests1.dat");
        }

        [TestMethod]
        [DeploymentItem("TestData")]
        public async Task EncodingTests2()
        {
            await RunTestFile("Encodings\\tests2.dat");
        }

        [TestMethod]
        [DeploymentItem("TestData")]
        public async Task EncodingTests3()
        {
            await RunTestFile("Encodings\\test-yahoo-jp.dat");
        }
    }
}
