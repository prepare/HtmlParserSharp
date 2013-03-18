using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml.Linq;

namespace HtmlParserSharp.Tests
{
    public static class XNodeHelpers
    {
        public static bool DeepEquals(XNode lhs, XNode rhs)
        {
            return XNode.DeepEquals(lhs, rhs);
        }

        public static bool DeepEquals(XNode[] lhs, XNode[] rhs)
        {
            Assert.AreEqual(lhs.Length, rhs.Length);
            for (int i = 0; i < lhs.Length; i++)
            {
                if (!XNode.DeepEquals(lhs[i], rhs[i]))
                {
                    Assert.Fail("XNode {0} does not equal {1}", lhs[i], rhs[i]);
                }
            }

            return true;
        }
    }
}