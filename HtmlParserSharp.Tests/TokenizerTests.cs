using System.Linq;
using HtmlParserSharp.Portable;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace HtmlParserSharp.Tests
{
    [TestClass]
    [DeploymentItem("TestData")]
    public class TokenizerTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void TestAngleBracketEncoding()
        {
            const string input = "&gt;";
            var parser = new SimpleHtmlParser();
            var fragment = parser.ParseStringFragment(input, string.Empty).ToArray();
            var answer = new XText(">");
            Assert.IsTrue(XNodeHelpers.DeepEquals(fragment[0], answer));
        }

        [TestMethod]
        public void TestAngleBracketEncoding2()
        {
            const string input = "&#62;";
            var parser = new SimpleHtmlParser();
            var fragment = parser.ParseStringFragment(input, string.Empty).ToArray();
            var answer = new XText(">");
            Assert.IsTrue(XNodeHelpers.DeepEquals(fragment[0], answer));
        }

        [TestMethod]
        public void ExemplarTokenizerTest()
        {
            const string input = "&AElig;";
            var parser = new SimpleHtmlParser();
            var fragment = parser.ParseStringFragment(input, string.Empty).ToArray();
            var answer = new XText("\u00c6");
            Assert.IsTrue(XNodeHelpers.DeepEquals(fragment[0], answer));
        }

        [TestMethod]
        public void ExemplarFailedTokenizerTest()
        {
            const string input = "&AElig";
            var parser = new SimpleHtmlParser();
            var fragment = parser.ParseStringFragment(input, string.Empty).ToArray();
            var answer = new XText("\u00c6");
            Assert.IsTrue(XNodeHelpers.DeepEquals(fragment[0], answer));
        }

        [TestMethod]
        public void RunAllTokenizerTests()
        {
            var parser = new SimpleHtmlParser();
            using (var reader = new StreamReader("Tokenizer\\namedEntities.test"))
            {
                var str = reader.ReadToEnd();
                var data = JObject.Parse(str);
                var tests = data["tests"].Value<JArray>();
                int testsRunCount = 0;
                int failedTestCount = 0;
                foreach (var test in tests)
                {
                    var input = test["input"].Value<string>();
                    var description = test["description"].Value<string>();
                    var output = test["output"].Value<JArray>();
                    var offset = output.Count == 2 ? 1 : 0;

                    // Skip all negative tests as we don't seem to do this right now
                    if (offset == 0)
                    {
                        var characters = output[offset].Value<JArray>();
                        if (characters.Count == 2)
                        {
                            var characterEntity = characters[1].Value<string>();
                            var fragment = parser.ParseStringFragment(input, string.Empty).ToArray();
                            var answer = new XText(characterEntity);
                            if (!XNodeHelpers.DeepEquals(fragment[0], answer))
                            {
                                failedTestCount++;
                                TestContext.WriteLine(description);
                            }
                        }
                        testsRunCount++;
                    }
                }
                Assert.IsTrue(failedTestCount == 0, string.Format("Fail rate {0}/{1}",
                                            failedTestCount, testsRunCount));
            }
        }
    }
}
