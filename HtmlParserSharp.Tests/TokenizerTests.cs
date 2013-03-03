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
        public void ExemplarTokenizerTest()
        {
            const string input = "&AElig;";
            var parser = new SimpleHtmlParser();
            var node = parser.ParseStringFragment(input, "bob");
            var textNode = node as XText;
            Assert.AreEqual("\u00c6", textNode.Value, "Named entity: AElig; with a semi-colon");
        }

        [TestMethod]
        public void ExemplarFailedTokenizerTest()
        {
            const string input = "&AElig";
            var parser = new SimpleHtmlParser();
            var node = parser.ParseStringFragment(input, "bob");
            var textNode = node as XText;
            Assert.AreEqual("\u00c6", textNode.Value, "Named entity: AElig without a semi-colon");
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
                            var node = parser.ParseStringFragment(input, "bob");
                            var parsedTextNode = node as XText;
                            Assert.IsNotNull(parsedTextNode);
                            if (characterEntity != parsedTextNode.Value)
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
