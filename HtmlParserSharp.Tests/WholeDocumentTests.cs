using HtmlParserSharp.Portable;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HtmlParserSharp.Tests
{
    [TestClass]
    public class WholeDocumentTests
    {
        [TestMethod]
        [DeploymentItem("TestData")]
        public async Task ParseHtml5Specification()
        {
            var parser = new SimpleHtmlParser();
            using (var reader = new StreamReader("LargeDocuments\\Html5Specification.html"))
            {
                XDocument doc = await parser.Parse(reader);
                Assert.IsNotNull(doc);

                // TODO: parser is probably the wrong place for this property today. It should
                // be somewhere on the document, but not sure where on the XDocument it belongs
                Assert.AreEqual("ISO-8859-1", parser.DocumentEncoding);
            }
        }

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

        [TestMethod]
        [DeploymentItem("TestData")]
        public async Task EncodingTests()
        {
            List<EncodingTestCase> testCases = ParseTestData("Encodings\\tests1.dat");
            var parser = new SimpleHtmlParser();
            int counter = 0;
            List<int> errorOffsets = new List<int>();
            foreach (var testCase in testCases)
            {
                var doc = await parser.ParseString(testCase.Html);
                if (testCase.Encoding != parser.DocumentEncoding)
                {
                    errorOffsets.Add(counter);
                }
                counter++;
            }
            Assert.AreEqual(0, errorOffsets.Count, "Number of errors is {0}/{1}", errorOffsets.Count, testCases.Count);
        }
    }
}