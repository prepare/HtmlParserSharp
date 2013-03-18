using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HtmlParserSharp.Portable;

namespace HtmlParserSharp.Tests
{
    [TestClass]
    public class ParserTests
    {
        [TestMethod]
        public void ParseHackerNews1()
        {
            // TODO: strip this down even further once I verify this is a bug
            const string SOURCE_HTML = @"<span class=""comment"">
  <font color=#000000>
    <i>Burp Intruder is the fuzzer inside of Burp. All the Burp-like tools let you capture requests your browser sends, edit them, and replay them. Burp Intruder lets you take a captured request and set up rules to send hundreds or thousands of variant requests.</i>
    <p>I am weird among Matasanos (and ex-Matasanos :|) in that I live inside of Burp Intruder; I use it instead of Repeater. Why replay a request once when I can replay it 1000 times? So for me, non-crippled Intruder isn't optional.
    <p>I wish Burp didn't have a Scanner. I might pay $25 more for a branded version of Burp that specifically didn't have that feature, so I could reassure clients I wasn't ever using it.
  </font>
</span>
";
            var parser = new SimpleHtmlParser();
            var node = parser.ParseStringFragment(SOURCE_HTML, String.Empty);
            var x = 42;
        }

        // This one is missing the <i> element. This test verifies that <i> is a red herring
        [TestMethod]
        public void ParseHackerNews2()
        {
            // TODO: strip this down even further once I verify this is a bug
            const string SOURCE_HTML = @"<span class=""comment"">
  <font color=#000000>
    <p>I am weird among Matasanos (and ex-Matasanos :|) in that I live inside of Burp Intruder; I use it instead of Repeater. Why replay a request once when I can replay it 1000 times? So for me, non-crippled Intruder isn't optional.
    <p>I wish Burp didn't have a Scanner. I might pay $25 more for a branded version of Burp that specifically didn't have that feature, so I could reassure clients I wasn't ever using it.
  </font>
</span>
";
            var parser = new SimpleHtmlParser();
            var node = parser.ParseStringFragment(SOURCE_HTML, String.Empty);
            var x = 42;
        }

        // This one has a single non-closed <p>
        // This one doesn't parse correctly either
        [TestMethod]
        public void ParseHackerNews3()
        {
            // TODO: strip this down even further once I verify this is a bug
            const string SOURCE_HTML = @"<span class=""comment"">
  <font color=#000000>
    <p>I wish Burp didn't have a Scanner. I might pay $25 more for a branded version of Burp that specifically didn't have that feature, so I could reassure clients I wasn't ever using it.
  </font>
</span>
";
            var parser = new SimpleHtmlParser();
            var node = parser.ParseStringFragment(SOURCE_HTML, String.Empty);
            var x = 42;
        }

        // This one has a single closed <p>
        // This one parses corectly.
        [TestMethod]
        public void ParseHackerNews4()
        {
            // TODO: strip this down even further once I verify this is a bug
            const string SOURCE_HTML = @"<span class=""comment"">
  <font color=#000000>
    <p>I wish Burp didn't have a Scanner. I might pay $25 more for a branded version of Burp that specifically didn't have that feature, so I could reassure clients I wasn't ever using it.</p>
  </font>
</span>
";
            var parser = new SimpleHtmlParser();
            var node = parser.ParseStringFragment(SOURCE_HTML, String.Empty);
            var x = 42;
        }

        // This one has a single closed <p> no attribute in font
        // This one does not parse. Attribute in font doesn't matter.
        [TestMethod]
        public void ParseHackerNews5()
        {
            // TODO: strip this down even further once I verify this is a bug
            const string SOURCE_HTML = @"<span class=""comment"">
  <font>
    <p>I wish Burp didn't have a Scanner. I might pay $25 more for a branded version of Burp that specifically didn't have that feature, so I could reassure clients I wasn't ever using it.
  </font>
</span>
";
            var parser = new SimpleHtmlParser();
            var node = parser.ParseStringFragment(SOURCE_HTML, String.Empty);
            var x = 42;
        }

        // This one has a single closed <p> no attribute in font with different minimal text
        // This one does not parse. Attribute in font doesn't matter.
        [TestMethod]
        public void ParseHackerNews6()
        {
            // TODO: strip this down even further once I verify this is a bug
            const string SOURCE_HTML = @"<span class=""comment"">
  <font>
    <p>repro
  </font>
</span>
";
            var parser = new SimpleHtmlParser();
            var node = parser.ParseStringFragment(SOURCE_HTML, String.Empty);
            var x = 42;
        }

        // This one has a single closed <p> no attribute in font with different minimal text. Strip out attribute in span
        // This one does not parse. Attribute in font doesn't matter.
        [TestMethod]
        public void ParseHackerNews7()
        {
            // TODO: strip this down even further once I verify this is a bug
            const string SOURCE_HTML = @"<span>
  <font>
    <p>repro
  </font>
</span>
";
            var parser = new SimpleHtmlParser();
            var node = parser.ParseStringFragment(SOURCE_HTML, String.Empty);
            var x = 42;
        }

        // This one has a single closed <p> no attribute in font with different minimal text. Strip out attribute in span. Strip out span
        // This one does not parse. Attribute in font doesn't matter.
        // However the entire <p> element doesn't appear.
        [TestMethod]
        public void ParseHackerNews8()
        {
            // TODO: strip this down even further once I verify this is a bug
            const string SOURCE_HTML = @"<font>
  <p>repro
</font>";
            var parser = new SimpleHtmlParser();
            var node = parser.ParseStringFragment(SOURCE_HTML, String.Empty);
            var x = 42;
        }

        // This one has a single closed <p> no attribute in font with different minimal text. Strip out attribute in span. Strip out span. Strip out text nodes -- single line
        // This one does not parse. Attribute in font doesn't matter.
        // However the entire <p> element doesn't appear.
        [TestMethod]
        public void ParseHackerNews9()
        {
            // TODO: strip this down even further once I verify this is a bug
            const string SOURCE_HTML = @"<font><p>repro</font>";
            var parser = new SimpleHtmlParser();
            var node = parser.ParseStringFragment(SOURCE_HTML, String.Empty);
            var x = 42;
        }

        // Replace <font> with <div> -- PARSES
        // This one has a single closed <p> no attribute in font with different minimal text. Strip out attribute in span. Strip out span. Strip out text nodes -- single line
        [TestMethod]
        public void ParseHackerNews10()
        {
            // TODO: strip this down even further once I verify this is a bug
            const string SOURCE_HTML = @"<div><p>repro</div>";
            var parser = new SimpleHtmlParser();
            var node = parser.ParseStringFragment(SOURCE_HTML, String.Empty);
            var x = 42;
        }

        // Replace <font> with <div>
        // Parses correctly. Something strange about <font>
        [TestMethod]
        public void ParseHackerNews11()
        {
            // TODO: strip this down even further once I verify this is a bug
            const string SOURCE_HTML = @"<span class=""comment"">
  <div color=#000000>
    <i>Burp Intruder is the fuzzer inside of Burp. All the Burp-like tools let you capture requests your browser sends, edit them, and replay them. Burp Intruder lets you take a captured request and set up rules to send hundreds or thousands of variant requests.</i>
    <p>I am weird among Matasanos (and ex-Matasanos :|) in that I live inside of Burp Intruder; I use it instead of Repeater. Why replay a request once when I can replay it 1000 times? So for me, non-crippled Intruder isn't optional.
    <p>I wish Burp didn't have a Scanner. I might pay $25 more for a branded version of Burp that specifically didn't have that feature, so I could reassure clients I wasn't ever using it.
  </div>
</span>
";
            var parser = new SimpleHtmlParser();
            var node = parser.ParseStringFragment(SOURCE_HTML, String.Empty);
            var x = 42;
        }

        // This is at the heart of the bug in the parser. 
        // Any of the active formatting elements:
        // a, b, big, code, em, font, i, nobr, s, small, strike, strong, tt, u
        // defines some different kind of context-dependent behavior in the parser.
        // This is probably where the problem is in the code.
        [TestMethod]
        public void ValidateActiveFormattingElementsThatContainNestedFormattingElements()
        {
            // Note that this bug will manifest itself with any of the active formatting elements defined above
            const string SOURCE_HTML = @"<i><p>repro</i>";
            var parser = new SimpleHtmlParser();
            var node = parser.ParseStringFragment(SOURCE_HTML, String.Empty);
            var x = 42;
        }
    }
}
