using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceStack.Text;

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

        private List<SanitizerTestCase> LoadTestCases(string path)
        {
            string testCaseData;
            using (var reader = new StreamReader(path))
            {
                testCaseData = reader.ReadToEnd();
            }

            var result = JsonSerializer.DeserializeFromString<List<SanitizerTestCase>>(testCaseData);
            return result;
        }
        
        [TestMethod]
        [DeploymentItem("TestData")]
        public void TestMethod1()
        {
            var testCases = LoadTestCases("sanitizer_tests.dat");

        }
    }
}
