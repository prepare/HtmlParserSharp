using HtmlParserSharp.Portable;
using HtmlParserSharp.Portable.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Xml.Linq;

namespace HtmlParserSharp.Tests
{
    [TestClass]
    public class ParserTests
    {
        // Stripped down form of the HTML that led to the original parsing error
        [TestMethod]
        public void ParseHackerNews1()
        {
            const string SOURCE_HTML = @"<span class=""comment""><div color=#000000><i>a</i><p>b<p>c</div></span>";
            var parser = new SimpleHtmlParser();
            var fragment = parser.ParseStringFragment(SOURCE_HTML, String.Empty).ToArray();
            var answer = new XNode[]
                {
                    new XElement(XHtml.SPAN, new XAttribute("class", "comment"),
                        new XElement(XHtml.DIV, new XAttribute("color", "#000000"),
                            new XElement(XHtml.I, new XText("a")),
                            new XElement(XHtml.P, new XText("b")),
                            new XElement(XHtml.P, new XText("c")))), 
                };
            Assert.IsTrue(XNodeHelpers.DeepEquals(fragment, answer));
        }

        // Set of tests that validate the parsing rules per section 12.2.3.3
        // of HTML parsing spec.
        [TestMethod]
        public void ValidateParsingOfActiveFormattingElements1()
        {
            const string SOURCE_HTML = @"<i><p>repro</i>";
            var parser = new SimpleHtmlParser();
            var fragment = parser.ParseStringFragment(SOURCE_HTML, String.Empty).ToArray();
            var answer = new XNode[]
                {
                    new XElement(XHtml.I),
                    new XElement(XHtml.P,
                        new XElement(XHtml.I, new XText("repro")))
                };
            Assert.IsTrue(XNodeHelpers.DeepEquals(fragment, answer));
        }

        [TestMethod]
        public void ValidateParsingOfActiveFormattingElements2()
        {
            const string SOURCE_HTML = @"<font><p>repro</font>";
            var parser = new SimpleHtmlParser();
            var fragment = parser.ParseStringFragment(SOURCE_HTML, String.Empty).ToArray();
            var answer = new XNode[]
                {
                    new XElement(XHtml.FONT), 
                    new XElement(XHtml.P,
                        new XElement(XHtml.FONT, new XText("repro"))) 
                };
            Assert.IsTrue(XNodeHelpers.DeepEquals(fragment, answer));
        }

        [TestMethod]
        public void ValidateParsingOfBlockElements1()
        {
            const string SOURCE_HTML = @"<div><p>repro</div>";
            var parser = new SimpleHtmlParser();
            var fragment = parser.ParseStringFragment(SOURCE_HTML, String.Empty).ToArray();
            var answer = new XNode[]
                {
                    new XElement(XHtml.DIV,
                        new XElement(XHtml.P, new XText("repro")))
                };
            Assert.IsTrue(XNodeHelpers.DeepEquals(fragment, answer));
        }
    }
}