using HtmlParserSharp.Portable;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace HtmlParserSharp.Tests
{
    [TestClass]
    public class WholeDocumentTests
    {
        // This document results a parent XElement null exception
        [TestMethod]
        public void ParseErrorDoc1()
        {
            var parser = new SimpleHtmlParser();
            using (var reader = new StreamReader("LargeDocuments\\doc1.html"))
            {
                var doc = parser.Parse(reader);
                Assert.IsNotNull(doc);
            }
        }

        [TestMethod]
        [DeploymentItem("TestData")]
        public void ParseHtml5Specification()
        {
            var parser = new SimpleHtmlParser();
            using (var reader = new StreamReader("LargeDocuments\\Html5Specification.html"))
            {
                XDocument doc = parser.Parse(reader);
                Assert.IsNotNull(doc);

                // Baseline the # of types of elements that we find inside of this
                XNamespace xhtmlNamespace = "http://www.w3.org/1999/xhtml";
                var anchors = from anchor in doc.Root.Descendants(xhtmlNamespace + "a")
                              select new {AnchorNode = anchor, Uri = anchor.Attribute("href").ToString()};
                Assert.AreEqual(33886, anchors.Count());
                var paragraphs = from paragraph in doc.Root.Descendants(xhtmlNamespace + "p")
                                 select new {Paragraph = paragraph};
                Assert.AreEqual(11727, paragraphs.Count());
            }
        }
    }
}