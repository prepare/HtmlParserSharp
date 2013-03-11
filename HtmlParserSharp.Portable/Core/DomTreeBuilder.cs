/*
 * Copyright (c) 2007 Henri Sivonen
 * Copyright (c) 2008-2010 Mozilla Foundation
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
using System.Linq;
using System.Xml.Linq;
using HtmlParserSharp.Portable.Common;

namespace HtmlParserSharp.Portable.Core
{
    internal class Namespaces
    {
        internal const string XHtml = "http://www.w3.org/1999/xhtml";
    }
    /// <summary>
    ///     The tree builder glue for building a tree through the public DOM APIs.
    /// </summary>
    internal class DomTreeBuilder : CoalescingTreeBuilder<XElement>
    {
        /// <summary>
        ///     The current doc.
        /// </summary>
        private XDocument _document;

        protected override void AddAttributesToElement(XElement element, HtmlAttributes attributes)
        {
            for (int i = 0; i < attributes.Length; i++)
            {
                string localName = attributes.GetLocalName(i);
                string namespaceUri = attributes.GetURI(i);
                XNamespace ns = namespaceUri;
                XName name = ns + localName;
                element.SetAttributeValue(name, attributes.GetValue(i));
            }
        }

        protected override void AppendCharacters(XElement parent, string text)
        {
            parent.Add(new XText(text));
        }

        protected override void AppendChildrenToNewParent(XElement oldParent, XElement newParent)
        {
            foreach (XNode node in oldParent.Nodes())
            {
                newParent.Add(node);
            }
        }

        protected override void AppendDoctypeToDocument(string name, string publicIdentifier, string systemIdentifier)
        {
            if (publicIdentifier == String.Empty)
                publicIdentifier = null;
            if (systemIdentifier == String.Empty)
                systemIdentifier = null;

            _document.Add(new XDocumentType(name, publicIdentifier, systemIdentifier, null));
        }

        protected override void AppendComment(XElement parent, String comment)
        {
            parent.Add(new XComment(comment));
        }

        protected override void AppendCommentToDocument(String comment)
        {
            _document.Add(new XComment(comment));
        }

        protected override XElement CreateElement(string namespaceUri, string localName, HtmlAttributes attributes)
        {
            XNamespace aw = namespaceUri;
            XName n = aw + localName;
            var rv = new XElement(n);
            for (int i = 0; i < attributes.Length; i++)
            {
                XNamespace attributeNamespace = attributes.GetURI(i);
                XName attributeName = attributeNamespace + attributes.GetLocalName(i);
                rv.Add(new XAttribute(attributeName, attributes.GetValue(i)));

                // BUGBUG: what is this?
                //if (attributes.GetType(i) == "ID")
                //{
                //    //rv.setIdAttributeNS(null, attributes.GetLocalName(i), true); // FIXME
                //}
            }
            return rv;
        }

        protected override XElement CreateHtmlElementSetAsRoot(HtmlAttributes attributes)
        {
            XElement htmlElement = CreateElement(Namespaces.XHtml, "html", attributes);
            _document.Add(htmlElement);
            return htmlElement;
        }

        protected override void AppendElement(XElement child, XElement newParent)
        {
            newParent.Add(child);
        }

        protected override bool HasChildren(XElement element)
        {
            return element.Nodes().Any();
        }

        protected override XElement CreateElement(string ns, string name, HtmlAttributes attributes, XElement form)
        {
            return CreateElement(ns, name, attributes);
            //rv.setUserData("nu.validator.form-pointer", form, null); // TODO
        }

        protected override void Start(bool fragment)
        {
            _document = new XDocument(); // implementation.createDocument(null, null, null);
            // TODO: fragment?
        }

        protected override void ReceiveDocumentMode(DocumentMode mode, String publicIdentifier,
                                                    String systemIdentifier, bool html4SpecificAdditionalErrorChecks)
        {
            //document.setUserData("nu.validator.document-mode", mode, null); // TODO
        }

        /// <summary>
        ///     Returns the document.
        /// </summary>
        /// <returns>The document</returns>
        internal XDocument Document
        {
            get { return _document; }
        }

        protected override void InsertFosterParentedCharacters(string text, XElement table, XElement stackParent)
        {
            XNode parent = table.Parent;
            if (parent != null)
            {
                table.AddBeforeSelf(new XText(text));
            }
            else
            {
                stackParent.Add(new XText(text));
            }
        }

        protected override void InsertFosterParentedChild(XElement child, XElement table, XElement stackParent)
        {
            XNode parent = table.Parent;
            if (parent != null)
            {
                table.AddBeforeSelf(child);
            }
            else
            {
                stackParent.Add(child);
            }
        }

        protected override void DetachFromParent(XElement element)
        {
            element.Remove();
        }
    }
}