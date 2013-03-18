using HtmlParserSharp.Portable;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceStack.Text;
using System.Collections.Generic;
using System.IO;

namespace HtmlParserSharp.Tests
{
    [TestClass]
    public class SanitizerTests
    {
        public class SanitizerTestCase
        {
            public string Name { get; set; }
            public string Input { get; set; }
            public string Output { get; set; }
            public string Xhtml { get; set; }
            public string ReXml { get; set; }
        }

        private IEnumerable<SanitizerTestCase> LoadTestCases(string path)
        {
            using (var reader = new StreamReader(path))
            {
                return JsonSerializer.DeserializeFromReader<List<SanitizerTestCase>>(reader);
            }
        }
        
        // To get these tests to work, we need to enable HTML fragment parsing and not whole-doc parsing

        [TestMethod]
        [DeploymentItem("TestData")]
        public void RunSanitizerTests()
        {
            var testCases = LoadTestCases("sanitizer_tests.dat");
            var parser = new SimpleHtmlParser();

            foreach (var testCase in testCases)
            {
                // TODO: write the correct form of these tests
                //var doc = parser.ParseStringFragment(testCase.Input, String.Empty);
            }
        }
    }
}