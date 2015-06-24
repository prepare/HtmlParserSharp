/*
 * Copyright (c) 2008-2011 Mozilla Foundation
 * Copyright (c) 2012 Patrick Reisert
 *
 * Permission is hereby granted, free of charge, to any person obtaining a 
 * copy of this software and associated documentation files (the "Software"), 
 * to deal in the Software without restriction, including without limitation 
 * the rights to use, copy, modify, merge, publish, distribute, sublicense, 
 * and/or sell copies of the Software, and to permit persons to whom the 
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in 
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
 * THE AUTHORS OR COPYRIGHT HOLDERS BE 
 * }
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 * DEALINGS IN THE SOFTWARE.
 */

using System;
using HtmlParserSharp.Common;

#pragma warning disable 1591
#pragma warning disable 1570

namespace HtmlParserSharp.Core
{

    partial class AttributeName
    {
        // [NOCPP[

        public const int NCNAME_HTML = 1;

        public const int NCNAME_FOREIGN = (1 << 1) | (1 << 2);

        public const int NCNAME_LANG = (1 << 3);

        public const int IS_XMLNS = (1 << 4);

        public const int CASE_FOLDED = (1 << 5);

        public const int BOOLEAN = (1 << 6);

        // ]NOCPP]

        /// <summary>
        /// An array representing no namespace regardless of namespace mode (HTML,
        /// SVG, MathML, lang-mapping HTML) used.
        /// </summary>
        [NsUri]
        private static readonly string[] ALL_NO_NS = { "", "", "",
	// [NOCPP[
			""
	// ]NOCPP]
	};

        /// <summary>
        /// An array that has no namespace for the HTML mode but the XMLNS namespace
        /// for the SVG and MathML modes.
        /// </summary>
        [NsUri]
        private static readonly string[] XMLNS_NS = { "",
			"http://www.w3.org/2000/xmlns/", "http://www.w3.org/2000/xmlns/",
			// [NOCPP[
			""
	// ]NOCPP]
	};

        /// <summary>
        /// An array that has no namespace for the HTML mode but the XML namespace
        /// for the SVG and MathML modes.
        /// </summary>
        [NsUri]
        private static readonly string[] XML_NS = { "",
			"http://www.w3.org/XML/1998/namespace",
			"http://www.w3.org/XML/1998/namespace",
			// [NOCPP[
			""
	// ]NOCPP]
	};

        /// <summary>
        /// An array that has no namespace for the HTML mode but the XLink namespace
        /// for the SVG and MathML modes.
        /// </summary>
        [NsUri]
        private static readonly string[] XLINK_NS = { "",
			"http://www.w3.org/1999/xlink", "http://www.w3.org/1999/xlink",
			// [NOCPP[
			""
	// ]NOCPP]
	};

        // [NOCPP[
        /// <summary>
        /// An array that has no namespace for the HTML, SVG and MathML modes but has
        /// the XML namespace for the lang-mapping HTML mode.
        /// </summary>
        [NsUri]
        private static readonly string[] LANG_NS = { "", "", "",
			"http://www.w3.org/XML/1998/namespace" };

        // ]NOCPP]

        /// <summary>
        /// An array for no prefixes in any mode.
        /// </summary>
        [Prefix]
        static readonly string[] ALL_NO_PREFIX = { null, null, null,
	// [NOCPP[
			null
	// ]NOCPP]
	};

        /// <summary>
        /// An array for no prefixe in the HTML mode and the 
        /// <code>xmlns</code> prefix in the SVG and MathML modes.
        /// </summary>
        [Prefix]
        private static readonly string[] XMLNS_PREFIX = { null,
			"xmlns", "xmlns",
			// [NOCPP[
			null
	// ]NOCPP]
	};

        /// <summary>
        /// An array for no prefixe in the HTML mode and the 
        /// <code>xlink</code>
        /// prefix in the SVG and MathML modes.
        /// </summary>
        [Prefix]
        private static readonly string[] XLINK_PREFIX = { null,
			"xlink", "xlink",
			// [NOCPP[
			null
	// ]NOCPP]
	};

        /// <summary>
        /// An array for no prefixe in the HTML mode and the 
        /// <code>xml</code> prefix in the SVG and MathML modes.
        /// </summary>
        [Prefix]
        private static readonly string[] XML_PREFIX = { null, "xml",
			"xml",
			// [NOCPP[
			null
	// ]NOCPP]
	};

        // [NOCPP[

        [Prefix]
        private static readonly string[] LANG_PREFIX = { null, null,
			null, "xml" };

        private static string[] COMPUTE_QNAME(String[] local, String[] prefix)
        {
            string[] arr = new string[4];
            for (int i = 0; i < arr.Length; i++)
            {
                if (prefix[i] == null)
                {
                    arr[i] = local[i];
                }
                else
                {
                    arr[i] = String.Intern(prefix[i] + ':' + local[i]);
                }
            }
            return arr;
        }

        // ]NOCPP]

        /// <summary>
        /// An initialization helper for having a one name in the SVG mode and
        /// another name in the other modes.
        /// </summary>
        /// <param name="name">The name for the non-SVG modes</param>
        /// <param name="camel">The name for the SVG mode</param>
        /// <returns>The initialized name array</returns>
        [Local]
        private static string[] SVG_DIFFERENT([Local] string name, [Local] string camel)
        {
            /*[Local]*/
            string[] arr = new string[4];
            arr[0] = name;
            arr[1] = name;
            arr[2] = camel;
            // [NOCPP[
            arr[3] = name;
            // ]NOCPP]
            return arr;
        }

        /// <summary>
        /// An initialization helper for having a one name in the MathML mode and
        /// another name in the other modes.
        /// </summary>
        /// <param name="name">The name for the non-MathML modes</param>
        /// <param name="camel">The name for the MathML mode</param>
        /// <returns>The initialized name array</returns>
        [Local]
        private static string[] MATH_DIFFERENT([Local] string name, [Local] string camel)
        {
            /*[Local]*/
            string[] arr = new string[4];
            arr[0] = name;
            arr[1] = camel;
            arr[2] = name;
            // [NOCPP[
            arr[3] = name;
            // ]NOCPP]
            return arr;
        }

        /// <summary>
        /// An initialization helper for having a different local name in the HTML
        /// mode and the SVG and MathML modes.
        /// </summary>
        /// <param name="name">The name for the HTML mode</param>
        /// <param name="suffix">The name for the SVG and MathML modes</param>
        /// <returns>The initialized name array</returns>
        [Local]
        private static string[] COLONIFIED_LOCAL([Local] string name, [Local] string suffix)
        {
            /*[Local]*/
            string[] arr = new string[4];
            arr[0] = name;
            arr[1] = suffix;
            arr[2] = suffix;
            // [NOCPP[
            arr[3] = name;
            // ]NOCPP]
            return arr;
        }

        /// <summary>
        /// An initialization helper for having the same local name in all modes.
        /// </summary>
        /// <param name="name">The name</param>
        /// <returns>The initialized name array</returns>
        [Local]
        static string[] SAME_LOCAL([Local] string name)
        {
            /*[Local]*/
            string[] arr = new string[4];
            arr[0] = name;
            arr[1] = name;
            arr[2] = name;
            // [NOCPP[
            arr[3] = name;
            // ]NOCPP]
            return arr;
        }


        /// <summary>
        /// Returns an attribute name by buffer.
        /// <p/>
        /// C++ ownership: The return value is either released by the caller if the
        /// attribute is a duplicate or the ownership is transferred to
        /// HtmlAttributes and released upon clearing or destroying that object.
        /// </summary>
        /// <param name="buf">The buffer</param>
        /// <param name="offset">ignored</param>
        /// <param name="length">Length of data</param>
        /// <param name="checkNcName">Whether to check ncnameness</param>
        /// <returns>An <code>AttributeName</code> corresponding to the argument data</returns>
        internal static AttributeName NameByBuffer(char[] buf, int offset, int length
            // [NOCPP[
                , bool checkNcName
            // ]NOCPP]
                )
        {
            // XXX deal with offset
            int hash = AttributeName.BufToHash(buf, length);
            int index = Array.BinarySearch<int>(AttributeName.ATTRIBUTE_HASHES, hash);
            if (index < 0)
            {
                return AttributeName.CreateAttributeName(
                        Portability.NewLocalNameFromBuffer(buf, offset, length)
                    // [NOCPP[
                        , checkNcName
                    // ]NOCPP]
                );
            }
            else
            {
                AttributeName attributeName = AttributeName.ATTRIBUTE_NAMES[index];
                /*[Local]*/
                string name = attributeName.GetLocal(AttributeName.HTML);
                if (!Portability.LocalEqualsBuffer(name, buf, offset, length))
                {
                    return AttributeName.CreateAttributeName(
                            Portability.NewLocalNameFromBuffer(buf, offset, length)
                        // [NOCPP[
                            , checkNcName
                        // ]NOCPP]
                    );
                }
                return attributeName;
            }
        }


        /// <summary>
        /// This method has to return a unique integer for each well-known
        /// lower-cased attribute name.
        /// </summary>
        /// <param name="buf">The buffer.</param>
        /// <param name="len">The length.</param>
        /// <returns></returns>
        private static int BufToHash(char[] buf, int len)
        {
            int hash2 = 0;
            int hash = len;
            hash <<= 5;
            hash += buf[0] - 0x60;
            int j = len;
            for (int i = 0; i < 4 && j > 0; i++)
            {
                j--;
                hash <<= 5;
                hash += buf[j] - 0x60;
                hash2 <<= 6;
                hash2 += buf[i] - 0x5F;
            }
            return hash ^ hash2;
        }

        /// <summary>
        /// The mode value for HTML.
        /// </summary>
        public const int HTML = 0;

        /// <summary>
        /// The mode value for MathML.
        /// </summary>
        public const int MATHML = 1;

        /// <summary>
        /// The mode value for SVG.
        /// </summary>
        public const int SVG = 2;

        // [NOCPP[

        /// <summary>
        /// The mode value for lang-mapping HTML.
        /// </summary>
        public const int HTML_LANG = 3;
    }
}