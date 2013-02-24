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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using HtmlParserSharp.Portable.Common;
using HtmlParserSharp.Portable.Core;
using System.Xml.Linq;

namespace HtmlParserSharp.Portable.Core
{
    /// <summary>
	/// The tree builder glue for building a tree through the public DOM APIs.
	/// </summary>
	class DomTreeBuilder : CoalescingTreeBuilder<XElement>
	{

		/// <summary>
		/// The current doc.
		/// </summary>
		private XDocument _document;

		override protected void AddAttributesToElement(XElement element, HtmlAttributes attributes)
		{
			for (int i = 0; i < attributes.Length; i++) {
				var localName = attributes.GetLocalName(i);
				var namespaceUri = attributes.GetURI(i);
                element.SetAttributeValue("{" + namespaceUri + "}" + localName, attributes.GetValue(i));
			}
		}

		override protected void AppendCharacters(XElement parent, string text)
		{
            parent.Add(new XText(text));
		}

		override protected void AppendChildrenToNewParent(XElement oldParent, XElement newParent) {
            foreach (var node in oldParent.Nodes())
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

		override protected void AppendComment(XElement parent, String comment)
		{
            parent.Add(new XComment(comment));
		}

		override protected void AppendCommentToDocument(String comment)
		{
            _document.Add(new XComment(comment));
		}

		override protected XElement CreateElement(string namespaceUri, string localName, HtmlAttributes attributes)
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

		override protected XElement CreateHtmlElementSetAsRoot(HtmlAttributes attributes)
		{
		    XElement htmlElement = CreateElement("http://www.w3.org/1999/xhtml", "html", attributes);
			_document.Add(htmlElement);
			return htmlElement;
		}

		override protected void AppendElement(XElement child, XElement newParent)
		{
            newParent.Add(child);
		}

		override protected bool HasChildren(XElement element)
		{
		    return element.Nodes().Any();
		}

		override protected XElement CreateElement(string ns, string name, HtmlAttributes attributes, XElement form) {
			return CreateElement(ns, name, attributes);
			//rv.setUserData("nu.validator.form-pointer", form, null); // TODO
		}

		override protected void Start(bool fragment) {
			_document = new XDocument(); // implementation.createDocument(null, null, null);
			// TODO: fragment?
		}

		protected override void ReceiveDocumentMode(DocumentMode mode, String publicIdentifier,
            String systemIdentifier, bool html4SpecificAdditionalErrorChecks)
		{
			//document.setUserData("nu.validator.document-mode", mode, null); // TODO
		}

		/// <summary>
		/// Returns the document.
		/// </summary>
		/// <returns>The document</returns>
		internal XDocument Document
		{
			get
			{
				return _document;
			}
		}

		/// <summary>
		/// Return the document fragment.
		/// </summary>
		/// <returns>The document fragment</returns>
		/// HUH???
        //internal XmlDocumentFragment getDocumentFragment() {
        //    XmlDocumentFragment rv = _document.CreateDocumentFragment();
        //    XmlNode rootElt = _document.FirstChild;
        //    while (rootElt.HasChildNodes) {
        //        rv.AppendChild(rootElt.FirstChild);
        //    }
        //    _document = null;
        //    return rv;
        //}

		override protected void InsertFosterParentedCharacters(string text,	XElement table, XElement stackParent)
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

		override protected void InsertFosterParentedChild(XElement child, XElement table, XElement stackParent) {
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

		override protected void DetachFromParent(XElement element)
		{
            element.Remove();
		}
	}
}
