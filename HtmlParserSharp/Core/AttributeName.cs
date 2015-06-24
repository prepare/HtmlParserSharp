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
 * THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
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
    public partial class AttributeName
    {
         
        /// <summary>
        /// The namespaces indexable by mode.
        /// </summary>
        [NsUri]
        private readonly string[] uri;

        /// <summary>
        /// The local names indexable by mode.
        /// </summary>
        [Local]
        private readonly string[] local;

        /// <summary>
        /// The prefixes indexably by mode.
        /// </summary>
        [Prefix]
        private readonly string[] prefix;

        // [NOCPP[

        private readonly int flags;

        /// <summary>
        /// The qnames indexable by mode.
        /// </summary>
        private readonly string[] qName;

        // ]NOCPP]

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeName"/> class (The startup-time constructor). 
        /// </summary>
        /// <param name="uri">The namespace.</param>
        /// <param name="local">The local name.</param>
        /// <param name="prefix">The prefix.</param>
        /// <param name="flags">The flags.</param>
        /*protected*/
        AttributeName([NsUri] string[] uri, [Local] string[] local, [Prefix] string[] prefix
            // [NOCPP[
  , int flags
            // ]NOCPP]        
)
        {
            this.uri = uri;
            this.local = local;
            this.prefix = prefix;
            // [NOCPP[
            this.qName = COMPUTE_QNAME(local, prefix);
            this.flags = flags;
            // ]NOCPP]
        }

        /// <summary>
        /// Creates an <code>AttributeName</code> for a local name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="checkNcName">Whether to check ncnameness.</param>
        /// <returns>An <code>AttributeName</code></returns>
        private static AttributeName CreateAttributeName([Local] string name
            // [NOCPP[
                , bool checkNcName
            // ]NOCPP]
        )
        {
            // [NOCPP[
            int flags = NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG;
            if (name.StartsWith("xmlns:"))
            {
                flags = IS_XMLNS;
            }
            else if (checkNcName && !NCName.IsNCName(name))
            {
                flags = 0;
            }
            // ]NOCPP]
            return new AttributeName(AttributeName.ALL_NO_NS, AttributeName.SAME_LOCAL(name), ALL_NO_PREFIX, flags);
        }

        /// <summary>
        /// TODO: remove this (?)
        /// Clones the attribute using an interner. Returns
        /// <code>this</code> in Java and for non-dynamic instances in C++.
        /// </summary>
        ///
        /// <returns>
        /// A clone.
        /// </returns>

        public /*virtual*/ AttributeName CloneAttributeName(/*Interner interner*/)
        {
            return this;
        }

        // [NOCPP[
        /// <summary>
        /// Creator for use when the XML violation policy requires an attribute name
        /// to be changed.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The name of the attribute to create</returns>
        internal static AttributeName Create(string name)
        {
            return new AttributeName(AttributeName.ALL_NO_NS,
                    AttributeName.SAME_LOCAL(name), ALL_NO_PREFIX,
                    NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
        }

        /// <summary>
        /// Determines whether this name is an XML 1.0 4th ed. NCName.
        /// </summary>
        /// <param name="mode">The SVG/MathML/HTML mode</param>
        /// <returns>
        ///   <c>true</c> if if this is an NCName in the given mode; otherwise, <c>false</c>.
        /// </returns>
        public bool IsNcName(int mode)
        {
            return (flags & (1 << mode)) != 0;
        }

        /// <summary>
        /// Queries whether this is an <code>xmlns</code> attribute.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this is an <code>xmlns</code> attribute; otherwise, <c>false</c>.
        /// </returns>
        public bool IsXmlns
        {
            get
            {
                return (flags & IS_XMLNS) != 0;
            }
        }

        /// <summary>
        /// Determines whether this attribute has a case-folded value in the HTML4 mode
        /// of the parser.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the value is case-folded; otherwise, <c>false</c>.
        /// </returns>
        internal bool IsCaseFolded
        {
            get
            {
                return (flags & CASE_FOLDED) != 0;
            }
        }

        internal bool IsBoolean
        {
            get
            {
                return (flags & BOOLEAN) != 0;
            }
        }

        public string GetQName(int mode)
        {
            return qName[mode];
        }

        // ]NOCPP]

        [NsUri]
        public string GetUri(int mode)
        {
            return uri[mode];
        }

        [Local]
        public string GetLocal(int mode)
        {
            return local[mode];
        }

        [Prefix]
        public string GetPrefix(int mode)
        {
            return prefix[mode];
        }

        public override int GetHashCode()
        {
            var name = GetLocal(0);
            return BufToHash(name.ToCharArray(), name.Length);
        }

        public override bool Equals(object obj)
        {
            var other = obj as AttributeName;

            return other != null && GetLocal(0) == other.GetLocal(0);
        }

        public static bool operator ==(AttributeName a, AttributeName b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            // Return true if the fields match:
            return a.Equals(b);
        }

        public static bool operator !=(AttributeName a, AttributeName b)
        {
            return !(a == b);
        }

        
    }

}
