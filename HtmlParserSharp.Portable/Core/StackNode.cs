/*
 * Copyright (c) 2007 Henri Sivonen
 * Copyright (c) 2007-2011 Mozilla Foundation
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
using System.Diagnostics;
using HtmlParserSharp.Portable.Common;

namespace HtmlParserSharp.Portable.Core
{
    public sealed class StackNode<T>
    {
        private readonly int _flags;

        [Local] internal readonly string _name;

        [Local] internal readonly string _popName;

        [NsUri] internal readonly string _ns;

        internal readonly T _node;

        // Only used on the list of formatting elements
        internal HtmlAttributes _attributes;

        // [NOCPP[

        private readonly TaintableLocator _locator;

        public TaintableLocator Locator
        {
            get { return _locator; }
        }

        // ]NOCPP]

        public int Flags
        {
            get { return _flags; }
        }

        public DispatchGroup Group
        {
            get { return (DispatchGroup) (_flags & ElementName.GROUP_MASK); }
        }

        public bool IsScoping
        {
            get { return (_flags & ElementName.SCOPING) != 0; }
        }

        public bool IsSpecial
        {
            get { return (_flags & ElementName.SPECIAL) != 0; }
        }

        public bool IsFosterParenting
        {
            get { return (_flags & ElementName.FOSTER_PARENTING) != 0; }
        }

        public bool IsHtmlIntegrationPoint
        {
            get { return (_flags & ElementName.HTML_INTEGRATION_POINT) != 0; }
        }

        // [NOCPP[

        public bool IsOptionalEndTag
        {
            get { return (_flags & ElementName.OPTIONAL_END_TAG) != 0; }
        }

        // ]NOCPP]

        /// <summary>
        ///     Constructor for copying. This doesn't take another <code>StackNode</code>
        ///     because in C++ the caller is reponsible for reobtaining the local names
        ///     from another interner.
        /// </summary>
        internal StackNode(int flags, [NsUri] String ns, [Local] String name, T node,
                           [Local] String popName, HtmlAttributes attributes
                           // [NOCPP[
                           , TaintableLocator locator
            // ]NOCPP]
            )
        {
            _flags = flags;
            _name = name;
            _popName = popName;
            _ns = ns;
            _node = node;
            _attributes = attributes;
            // [NOCPP[
            _locator = locator;
            // ]NOCPP]
        }

        /// <summary>
        ///     Short hand for well-known HTML elements.
        /// </summary>
        internal StackNode(ElementName elementName, T node
                           // [NOCPP[
                           , TaintableLocator locator
            // ]NOCPP]
            )
        {
            _flags = elementName.Flags;
            _name = elementName.name;
            _popName = elementName.name;
            _ns = Namespaces.XHtml;
            _node = node;
            _attributes = null;
            Debug.Assert(!elementName.IsCustom, "Don't use this constructor for custom elements.");
            // [NOCPP[
            _locator = locator;
            // ]NOCPP]
        }

        /// <summary>
        ///     Constructor for HTML formatting elements.
        /// </summary>
        internal StackNode(ElementName elementName, T node, HtmlAttributes attributes
                           // [NOCPP[
                           , TaintableLocator locator
            // ]NOCPP]
            )
        {
            _flags = elementName.Flags;
            _name = elementName.name;
            _popName = elementName.name;
            _ns = Namespaces.XHtml;
            _node = node;
            _attributes = attributes;
            Debug.Assert(!elementName.IsCustom, "Don't use this constructor for custom elements.");
            // [NOCPP[
            _locator = locator;
            // ]NOCPP]
        }

        /// <summary>
        ///     The common-case HTML constructor.
        /// </summary>
        internal StackNode(ElementName elementName, T node, [Local] string popName
                           // [NOCPP[
                           , TaintableLocator locator
            // ]NOCPP]
            )
        {
            _flags = elementName.Flags;
            _name = elementName.name;
            _popName = popName;
            _ns = Namespaces.XHtml;
            _node = node;
            _attributes = null;
            // [NOCPP[
            _locator = locator;
            // ]NOCPP]
        }

        /// <summary>
        ///     Constructor for SVG elements. Note that the order of the arguments is
        ///     what distinguishes this from the HTML constructor. This is ugly, but
        ///     AFAICT the least disruptive way to make this work with Java's generics
        ///     and without unnecessary branches. :-(
        /// </summary>
        internal StackNode(ElementName elementName, [Local] string popName, T node
                           // [NOCPP[
                           , TaintableLocator locator
            // ]NOCPP]
            )
        {
            _flags = PrepareSvgFlags(elementName.Flags);
            _name = elementName.name;
            _popName = popName;
            _ns = "http://www.w3.org/2000/svg";
            _node = node;
            _attributes = null;
            // [NOCPP[
            _locator = locator;
            // ]NOCPP]
        }

        /// <summary>
        ///     Constructor for MathML.
        /// </summary>
        internal StackNode(ElementName elementName, T node, [Local] string popName,
                           bool markAsIntegrationPoint
                           // [NOCPP[
                           , TaintableLocator locator
            // ]NOCPP]
            )
        {
            _flags = PrepareMathFlags(elementName.Flags, markAsIntegrationPoint);
            _name = elementName.name;
            _popName = popName;
            _ns = "http://www.w3.org/1998/Math/MathML";
            _node = node;
            _attributes = null;
            // [NOCPP[
            _locator = locator;
            // ]NOCPP]
        }

        private static int PrepareSvgFlags(int flags)
        {
            flags &= ~(ElementName.FOSTER_PARENTING | ElementName.SCOPING
                       | ElementName.SPECIAL | ElementName.OPTIONAL_END_TAG);
            if ((flags & ElementName.SCOPING_AS_SVG) != 0)
            {
                flags |= (ElementName.SCOPING | ElementName.SPECIAL | ElementName.HTML_INTEGRATION_POINT);
            }
            return flags;
        }

        private static int PrepareMathFlags(int flags, bool markAsIntegrationPoint)
        {
            flags &= ~(ElementName.FOSTER_PARENTING | ElementName.SCOPING
                       | ElementName.SPECIAL | ElementName.OPTIONAL_END_TAG);
            if ((flags & ElementName.SCOPING_AS_MATHML) != 0)
            {
                flags |= (ElementName.SCOPING | ElementName.SPECIAL);
            }
            if (markAsIntegrationPoint)
            {
                flags |= ElementName.HTML_INTEGRATION_POINT;
            }
            return flags;
        }

        public void DropAttributes()
        {
            _attributes = null;
        }

        // [NOCPP[

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override String ToString()
        {
            return _name;
        }

        // ]NOCPP]
    }
}