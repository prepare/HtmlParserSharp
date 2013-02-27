using HtmlParserSharp.Portable;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
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
            }
        }
    }
}
