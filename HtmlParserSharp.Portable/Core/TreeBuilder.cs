/*
 * Copyright (c) 2007 Henri Sivonen
 * Copyright (c) 2007-2011 Mozilla Foundation
 * Portions of comments Copyright 2004-2008 Apple Computer, Inc., Mozilla 
 * Foundation, and Opera Software ASA.
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

/*
 * The comments following this one that use the same comment syntax as this 
 * comment are quotes from the WHATWG HTML 5 spec as of 27 June 2007 
 * amended as of June 28 2007.
 * That document came with this statement:
 * © Copyright 2004-2007 Apple Computer, Inc., Mozilla Foundation, and 
 * Opera Software ASA. You are granted a license to use, reproduce and 
 * create derivative works of this document."
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HtmlParserSharp.Portable.Common;

namespace HtmlParserSharp.Portable.Core
{
    public abstract class TreeBuilder<T> : ITokenHandler, ITreeBuilderState<T> where T : class
    {
        private InsertionMode _mode = InsertionMode.INITIAL;

        private InsertionMode _originalMode = InsertionMode.INITIAL;

        /// <summary>
        ///     Used only when moving back to IN_BODY.
        /// </summary>
        private bool _framesetOk = true;

        protected Tokenizer _tokenizer;

        // [NOCPP[

        public event EventHandler<DocumentModeEventArgs> DocumentModeDetected;

        public DoctypeExpectation DoctypeExpectation { get; set; }

        // ]NOCPP]

        public bool IsScriptingEnabled { get; set; }

        private bool _needToDropLF;

        // [NOCPP[

        private bool _wantingComments;

        // ]NOCPP]

        private bool _fragment;

        [Local] private string _contextName;

        [NsUri] private string _contextNamespace;

        private T _contextNode;

        private StackNode<T>[] _stack;

        private int _currentPtr = -1;

        private StackNode<T>[] _listOfActiveFormattingElements;

        private int _listPtr = -1;

        private T _formPointer;

        private T _headPointer;

        /**
		 * Used to work around Gecko limitations. Not used in Java.
		 */
        private T _deepTreeSurrogateParent;

        protected char[] _charBuffer;

        protected int _charBufferLen = 0;

        private bool _quirks;

        // [NOCPP[

        public bool IsReportingDoctype { get; set; }

        public XmlViolationPolicy NamePolicy { get; set; }

        // stores the first occurrences of IDs
        private readonly Dictionary<string, Locator> _idLocations = new Dictionary<string, Locator>();

        private bool _html4;

        // ]NOCPP]

        protected TreeBuilder()
        {
            _fragment = false;
            IsReportingDoctype = true;
            DoctypeExpectation = DoctypeExpectation.Html;
            NamePolicy = XmlViolationPolicy.AlterInfoset;
            IsScriptingEnabled = false;
        }

        /// <summary>
        ///     Reports an condition that would make the infoset incompatible with XML
        ///     1.0 as fatal.
        /// </summary>
        protected void Fatal()
        {
            // TODO: why is this empty (in original code)?
        }

        // [NOCPP[

        protected void Fatal(Exception e)
        {
            //SAXParseException spe = new SAXParseException(e.getMessage(),
            //        tokenizer, e);
            //if (ErrorEvent != null) {
            //    errorHandler.fatalError(spe);
            //}
            //throw spe;

            throw e; // TODO
        }

        internal void Fatal(string s)
        {
            //SAXParseException spe = new SAXParseException(s, tokenizer);
            //if (ErrorEvent != null) {
            //    errorHandler.fatalError(spe);
            //}
            //throw spe;

            throw new Exception(s); // TODO
        }

        public event EventHandler<ParserErrorEventArgs> ErrorEvent;

        /// <summary>
        ///     Reports a Parse Error.
        /// </summary>
        /// <param name="message">The message.</param>
        private void Err(string message)
        {
            if (ErrorEvent != null)
            {
                ErrNoCheck(message);
            }
        }

        /// <summary>
        ///     Reports a Parse Error without checking if an error handler is present.
        /// </summary>
        /// <param name="message">The message.</param>
        private void ErrNoCheck(string message)
        {
            ErrorEvent(this, new ParserErrorEventArgs(message, false));
        }

        /// <summary>
        ///     Reports a stray start tag.
        /// </summary>
        /// <param name="name">The name of the stray tag.</param>
        private void ErrStrayStartTag(string name)
        {
            Err("Stray end tag \u201C" + name + "\u201D.");
        }

        /// <summary>
        ///     Reports a stray end tag.
        /// </summary>
        /// <param name="name">The name of the stray tag.</param>
        private void ErrStrayEndTag(string name)
        {
            Err("Stray end tag \u201C" + name + "\u201D.");
        }

        /// <summary>
        ///     Reports a state when elements expected to be closed were not.
        /// </summary>
        /// <param name="eltPos">
        ///     The position of the start tag on the stack of the element
        ///     being closed.
        /// </param>
        /// <param name="name">The name of the end tag.</param>
        private void ErrUnclosedElements(int eltPos, string name)
        {
            Err("End tag \u201C" + name + "\u201D seen, but there were open elements.");
            ErrListUnclosedStartTags(eltPos);
        }

        /// <summary>
        ///     Reports a state when elements expected to be closed ahead of an implied
        ///     end tag but were not.
        /// </summary>
        /// <param name="eltPos">
        ///     The position of the start tag on the stack of the element
        ///     being closed.
        /// </param>
        /// <param name="name">The name of the end tag.</param>
        private void ErrUnclosedElementsImplied(int eltPos, string name)
        {
            Err("End tag \u201C" + name + "\u201D implied, but there were open elements.");
            ErrListUnclosedStartTags(eltPos);
        }

        /// <summary>
        ///     Reports a state when elements expected to be closed ahead of an implied
        ///     table cell close.
        /// </summary>
        /// <param name="eltPos">
        ///     The position of the start tag on the stack of the element
        ///     being closed.
        /// </param>
        private void ErrUnclosedElementsCell(int eltPos)
        {
            Err("A table cell was implicitly closed, but there were open elements.");
            ErrListUnclosedStartTags(eltPos);
        }

        private void ErrListUnclosedStartTags(int eltPos)
        {
            if (_currentPtr != -1)
            {
                for (int i = _currentPtr; i > eltPos; i--)
                {
                    ReportUnclosedElementNameAndLocation(i);
                }
            }
        }

        /// <summary>
        ///     Reports arriving at/near end of document with unclosed elements remaining.
        /// </summary>
        /// <param name="message">The message.</param>
        private void ErrEndWithUnclosedElements(string message)
        {
            if (ErrorEvent == null)
            {
                return;
            }
            ErrNoCheck(message);
            // just report all remaining unclosed elements
            ErrListUnclosedStartTags(0);
        }

        /// <summary>
        ///     Reports the name and location of an unclosed element.
        /// </summary>
        /// <param name="pos">The position.</param>
        private void ReportUnclosedElementNameAndLocation(int pos)
        {
            StackNode<T> node = _stack[pos];
            if (node.IsOptionalEndTag)
            {
                return;
            }
            TaintableLocator locator = node.Locator;
            if (locator.IsTainted)
            {
                return;
            }
            locator.MarkTainted();
            //SAXParseException spe = new SAXParseException(
            //        "Unclosed element \u201C" + node.popName + "\u201D.", locator);
            //errorHandler.error(spe);
            ErrNoCheck("Unclosed element \u201C" + node._popName + "\u201D.");
        }

        /// <summary>
        ///     Reports a warning
        /// </summary>
        /// <param name="message">The message.</param>
        internal void Warn(string message)
        {
            if (ErrorEvent != null)
            {
                //SAXParseException spe = new SAXParseException(message, tokenizer);
                //errorHandler.warning(spe);
                ErrorEvent(this, new ParserErrorEventArgs(message, true));
            }
        }

        // ]NOCPP]

        public void StartTokenization(Tokenizer self)
        {
            _tokenizer = self;
            _stack = new StackNode<T>[64];
            _listOfActiveFormattingElements = new StackNode<T>[64];
            _needToDropLF = false;
            _originalMode = InsertionMode.INITIAL;
            _currentPtr = -1;
            _listPtr = -1;
            _formPointer = null;
            _headPointer = null;
            _deepTreeSurrogateParent = null;
            // [NOCPP[
            _html4 = false;
            _idLocations.Clear();
            _wantingComments = false;
            // ]NOCPP]
            Start(_fragment);
            _charBufferLen = 0;
            _charBuffer = new char[1024];
            _framesetOk = true;
            if (_fragment)
            {
                T elt = _contextNode ?? CreateHtmlElementSetAsRoot(_tokenizer.EmptyAttributes());
                var node = new StackNode<T>(ElementName.HTML, elt
                                            // [NOCPP[
                                            , ErrorEvent == null ? null : new TaintableLocator(_tokenizer)
                    // ]NOCPP]
                    );
                _currentPtr++;
                _stack[_currentPtr] = node;
                ResetTheInsertionMode();
                if ("title" == _contextName || "textarea" == _contextName)
                {
                    _tokenizer.SetStateAndEndTagExpectation(Tokenizer.RCDATA, _contextName);
                }
                else if ("style" == _contextName || "xmp" == _contextName
                         || "iframe" == _contextName || "noembed" == _contextName
                         || "noframes" == _contextName
                         || (IsScriptingEnabled && "noscript" == _contextName))
                {
                    _tokenizer.SetStateAndEndTagExpectation(Tokenizer.RAWTEXT, _contextName);
                }
                else if ("plaintext" == _contextName)
                {
                    _tokenizer.SetStateAndEndTagExpectation(Tokenizer.PLAINTEXT, _contextName);
                }
                else if ("script" == _contextName)
                {
                    _tokenizer.SetStateAndEndTagExpectation(Tokenizer.SCRIPT_DATA,
                                                            _contextName);
                }
                else
                {
                    _tokenizer.SetStateAndEndTagExpectation(Tokenizer.DATA, _contextName);
                }
                _contextName = null;
                _contextNode = null;
            }
            else
            {
                _mode = InsertionMode.INITIAL;
            }
        }

        public void Doctype([Local] string name, string publicIdentifier, string systemIdentifier, bool forceQuirks)
        {
            _needToDropLF = false;
            if (!IsInForeign)
            {
                switch (_mode)
                {
                    case InsertionMode.INITIAL:
                        // [NOCPP[
                        if (IsReportingDoctype)
                        {
                            // ]NOCPP]
                            AppendDoctypeToDocument(name ?? String.Empty,
                                                    publicIdentifier ?? String.Empty,
                                                    systemIdentifier ?? String.Empty);
                            // [NOCPP[
                        }
                        switch (DoctypeExpectation)
                        {
                            case DoctypeExpectation.Html:
                                // ]NOCPP]
                                if (IsQuirky(name, publicIdentifier,
                                             systemIdentifier, forceQuirks))
                                {
                                    Err("Quirky doctype. Expected \u201C<!DOCTYPE html>\u201D.");
                                    DocumentModeInternal(DocumentMode.QuirksMode,
                                                         publicIdentifier, systemIdentifier,
                                                         false);
                                }
                                else if (IsAlmostStandards(publicIdentifier,
                                                           systemIdentifier))
                                {
                                    Err("Almost standards mode doctype. Expected \u201C<!DOCTYPE html>\u201D.");
                                    DocumentModeInternal(
                                        DocumentMode.AlmostStandardsMode,
                                        publicIdentifier, systemIdentifier,
                                        false);
                                }
                                else
                                {
                                    // [NOCPP[
                                    if (("-//W3C//DTD HTML 4.0//EN" == publicIdentifier &&
                                         (systemIdentifier == null ||
                                          "http://www.w3.org/TR/REC-html40/strict.dtd" == systemIdentifier))
                                        || ("-//W3C//DTD HTML 4.01//EN" == publicIdentifier &&
                                            (systemIdentifier == null ||
                                             "http://www.w3.org/TR/html4/strict.dtd" == systemIdentifier))
                                        || ("-//W3C//DTD XHTML 1.0 Strict//EN" == publicIdentifier &&
                                            "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd" == systemIdentifier)
                                        || ("-//W3C//DTD XHTML 1.1//EN" == publicIdentifier &&
                                            "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd" == systemIdentifier)
                                        )
                                    {
                                        Warn("Obsolete doctype. Expected \u201C<!DOCTYPE html>\u201D.");
                                    }
                                    else if (!((systemIdentifier == null || "about:legacy-compat" == systemIdentifier) &&
                                               publicIdentifier == null))
                                    {
                                        Err("Legacy doctype. Expected \u201C<!DOCTYPE html>\u201D.");
                                    }
                                    // ]NOCPP]
                                    DocumentModeInternal(
                                        DocumentMode.StandardsMode,
                                        publicIdentifier, systemIdentifier,
                                        false);
                                }
                                // [NOCPP[
                                break;
                            case DoctypeExpectation.Html401Strict:
                                _html4 = true;
                                _tokenizer.TurnOnAdditionalHtml4Errors();
                                if (IsQuirky(name, publicIdentifier,
                                             systemIdentifier, forceQuirks))
                                {
                                    Err(
                                        "Quirky doctype. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01//EN\" \"http://www.w3.org/TR/html4/strict.dtd\">\u201D.");
                                    DocumentModeInternal(DocumentMode.QuirksMode,
                                                         publicIdentifier, systemIdentifier,
                                                         true);
                                }
                                else if (IsAlmostStandards(publicIdentifier,
                                                           systemIdentifier))
                                {
                                    Err(
                                        "Almost standards mode doctype. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01//EN\" \"http://www.w3.org/TR/html4/strict.dtd\">\u201D.");
                                    DocumentModeInternal(
                                        DocumentMode.AlmostStandardsMode,
                                        publicIdentifier, systemIdentifier,
                                        true);
                                }
                                else
                                {
                                    if ("-//W3C//DTD HTML 4.01//EN" == publicIdentifier)
                                    {
                                        if ("http://www.w3.org/TR/html4/strict.dtd" != systemIdentifier)
                                        {
                                            Warn(
                                                "The doctype did not contain the system identifier prescribed by the HTML 4.01 specification. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01//EN\" \"http://www.w3.org/TR/html4/strict.dtd\">\u201D.");
                                        }
                                    }
                                    else
                                    {
                                        Err(
                                            "The doctype was not the HTML 4.01 Strict doctype. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01//EN\" \"http://www.w3.org/TR/html4/strict.dtd\">\u201D.");
                                    }
                                    DocumentModeInternal(
                                        DocumentMode.StandardsMode,
                                        publicIdentifier, systemIdentifier,
                                        true);
                                }
                                break;
                            case DoctypeExpectation.Html401Transitional:
                                _html4 = true;
                                _tokenizer.TurnOnAdditionalHtml4Errors();
                                if (IsQuirky(name, publicIdentifier,
                                             systemIdentifier, forceQuirks))
                                {
                                    Err(
                                        "Quirky doctype. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\" \"http://www.w3.org/TR/html4/loose.dtd\">\u201D.");
                                    DocumentModeInternal(DocumentMode.QuirksMode,
                                                         publicIdentifier, systemIdentifier,
                                                         true);
                                }
                                else if (IsAlmostStandards(publicIdentifier,
                                                           systemIdentifier))
                                {
                                    if ("-//W3C//DTD HTML 4.01 Transitional//EN" == publicIdentifier
                                        && systemIdentifier != null)
                                    {
                                        if ("http://www.w3.org/TR/html4/loose.dtd" != systemIdentifier)
                                        {
                                            Warn(
                                                "The doctype did not contain the system identifier prescribed by the HTML 4.01 specification. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\" \"http://www.w3.org/TR/html4/loose.dtd\">\u201D.");
                                        }
                                    }
                                    else
                                    {
                                        Err(
                                            "The doctype was not a non-quirky HTML 4.01 Transitional doctype. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\" \"http://www.w3.org/TR/html4/loose.dtd\">\u201D.");
                                    }
                                    DocumentModeInternal(
                                        DocumentMode.AlmostStandardsMode,
                                        publicIdentifier, systemIdentifier,
                                        true);
                                }
                                else
                                {
                                    Err(
                                        "The doctype was not the HTML 4.01 Transitional doctype. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\" \"http://www.w3.org/TR/html4/loose.dtd\">\u201D.");
                                    DocumentModeInternal(
                                        DocumentMode.StandardsMode,
                                        publicIdentifier, systemIdentifier,
                                        true);
                                }
                                break;
                            case DoctypeExpectation.Auto:
                                _html4 = IsHtml4Doctype(publicIdentifier);
                                if (_html4)
                                {
                                    _tokenizer.TurnOnAdditionalHtml4Errors();
                                }
                                if (IsQuirky(name, publicIdentifier,
                                             systemIdentifier, forceQuirks))
                                {
                                    Err("Quirky doctype. Expected e.g. \u201C<!DOCTYPE html>\u201D.");
                                    DocumentModeInternal(DocumentMode.QuirksMode,
                                                         publicIdentifier, systemIdentifier,
                                                         _html4);
                                }
                                else if (IsAlmostStandards(publicIdentifier,
                                                           systemIdentifier))
                                {
                                    if ("-//W3C//DTD HTML 4.01 Transitional//EN" == publicIdentifier)
                                    {
                                        if ("http://www.w3.org/TR/html4/loose.dtd" != systemIdentifier)
                                        {
                                            Warn(
                                                "The doctype did not contain the system identifier prescribed by the HTML 4.01 specification. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\" \"http://www.w3.org/TR/html4/loose.dtd\">\u201D.");
                                        }
                                    }
                                    else
                                    {
                                        Err("Almost standards mode doctype. Expected e.g. \u201C<!DOCTYPE html>\u201D.");
                                    }
                                    DocumentModeInternal(
                                        DocumentMode.AlmostStandardsMode,
                                        publicIdentifier, systemIdentifier,
                                        _html4);
                                }
                                else
                                {
                                    if ("-//W3C//DTD HTML 4.01//EN" == publicIdentifier)
                                    {
                                        if ("http://www.w3.org/TR/html4/strict.dtd" != systemIdentifier)
                                        {
                                            Warn(
                                                "The doctype did not contain the system identifier prescribed by the HTML 4.01 specification. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01//EN\" \"http://www.w3.org/TR/html4/strict.dtd\">\u201D.");
                                        }
                                    }
                                    else
                                    {
                                        if (!(publicIdentifier == null && systemIdentifier == null))
                                        {
                                            Err("Legacy doctype. Expected e.g. \u201C<!DOCTYPE html>\u201D.");
                                        }
                                    }
                                    DocumentModeInternal(
                                        DocumentMode.StandardsMode,
                                        publicIdentifier, systemIdentifier,
                                        _html4);
                                }
                                break;
                            case DoctypeExpectation.NoDoctypeErrors:
                                if (IsQuirky(name, publicIdentifier,
                                             systemIdentifier, forceQuirks))
                                {
                                    DocumentModeInternal(DocumentMode.QuirksMode,
                                                         publicIdentifier, systemIdentifier,
                                                         false);
                                }
                                else if (IsAlmostStandards(publicIdentifier,
                                                           systemIdentifier))
                                {
                                    DocumentModeInternal(
                                        DocumentMode.AlmostStandardsMode,
                                        publicIdentifier, systemIdentifier,
                                        false);
                                }
                                else
                                {
                                    DocumentModeInternal(
                                        DocumentMode.StandardsMode,
                                        publicIdentifier, systemIdentifier,
                                        false);
                                }
                                break;
                        }
                        // ]NOCPP]

                        /*
						 * 
						 * Then, switch to the root element mode of the tree
						 * construction stage.
						 */
                        _mode = InsertionMode.BEFORE_HTML;
                        return;
                }
            }
            /*
			 * A DOCTYPE token Parse error.
			 */
            Err("Stray doctype.");
            /*
			 * Ignore the token.
			 */
        }

        // [NOCPP[

        private bool IsHtml4Doctype(string publicIdentifier)
        {
            if (publicIdentifier != null
                && (Array.BinarySearch(TreeBuilderConstants.HTML4_PUBLIC_IDS,
                                               publicIdentifier) > -1))
            {
                return true;
            }
            return false;
        }

        // ]NOCPP]

        /// <summary>
        ///     Receive a comment token. The data is junk if the<code>wantsComments()</code>
        ///     returned <code>false</code>.
        /// </summary>
        /// <param name="buf">The buffer holding the data.</param>
        /// <param name="start">The offset into the buffer.</param>
        /// <param name="length">The number of code units to read.</param>
        public void Comment(char[] buf, int start, int length)
        {
            _needToDropLF = false;
            // [NOCPP[
            if (!_wantingComments)
            {
                return;
            }
            // ]NOCPP]
            if (!IsInForeign)
            {
                switch (_mode)
                {
                    case InsertionMode.INITIAL:
                    case InsertionMode.BEFORE_HTML:
                    case InsertionMode.AFTER_AFTER_BODY:
                    case InsertionMode.AFTER_AFTER_FRAMESET:
                        /*
						 * A comment token Append a Comment node to the Document
						 * object with the data attribute set to the data given in
						 * the comment token.
						 */
                        AppendCommentToDocument(buf, start, length);
                        return;
                    case InsertionMode.AFTER_BODY:
                        /*
						 * A comment token Append a Comment node to the first
						 * element in the stack of open elements (the html element),
						 * with the data attribute set to the data given in the
						 * comment token.
						 */
                        FlushCharacters();
                        AppendComment(_stack[0]._node, buf, start, length);
                        return;
                }
            }
            /*
			 * A comment token Append a Comment node to the current node with the
			 * data attribute set to the data given in the comment token.
			 */
            FlushCharacters();
            AppendComment(_stack[_currentPtr]._node, buf, start, length);
        }

        /// <summary>
        ///     Receive character tokens. This method has the same semantics as the SAX
        ///     method of the same name.
        /// </summary>
        /// <param name="buf">A buffer holding the data.</param>
        /// <param name="start">The offset into the buffer.</param>
        /// <param name="length">The number of code units to read.</param>
        public void Characters(char[] buf, int start, int length)
        {
            if (_needToDropLF)
            {
                _needToDropLF = false;
                if (buf[start] == '\n')
                {
                    start++;
                    length--;
                    if (length == 0)
                    {
                        return;
                    }
                }
            }

            // optimize the most common case
            switch (_mode)
            {
                case InsertionMode.IN_BODY:
                case InsertionMode.IN_CELL:
                case InsertionMode.IN_CAPTION:
                    if (!IsInForeign)
                    {
                        ReconstructTheActiveFormattingElements();
                    }
                    // fall through
                    goto case InsertionMode.TEXT;
                case InsertionMode.TEXT:
                    AccumulateCharacters(buf, start, length);
                    return;
                case InsertionMode.IN_TABLE:
                case InsertionMode.IN_TABLE_BODY:
                case InsertionMode.IN_ROW:
                    AccumulateCharactersForced(buf, start, length);
                    return;
                default:
                    int end = start + length;
                    /*charactersloop:*/
                    for (int i = start; i < end; i++)
                    {
                        switch (buf[i])
                        {
                            case ' ':
                            case '\t':
                            case '\n':
                            case '\r':
                            case '\u000C':
                                /*
								 * A character token that is one of one of U+0009
								 * CHARACTER TABULATION, U+000A LINE FEED (LF),
								 * U+000C FORM FEED (FF), or U+0020 SPACE
								 */
                                switch (_mode)
                                {
                                    case InsertionMode.INITIAL:
                                    case InsertionMode.BEFORE_HTML:
                                    case InsertionMode.BEFORE_HEAD:
                                        /*
										 * Ignore the token.
										 */
                                        start = i + 1;
                                        continue;
                                    case InsertionMode.IN_HEAD:
                                    case InsertionMode.IN_HEAD_NOSCRIPT:
                                    case InsertionMode.AFTER_HEAD:
                                    case InsertionMode.IN_COLUMN_GROUP:
                                    case InsertionMode.IN_FRAMESET:
                                    case InsertionMode.AFTER_FRAMESET:
                                        /*
										 * Append the character to the current node.
										 */
                                        continue;
                                    case InsertionMode.FRAMESET_OK:
                                    case InsertionMode.IN_BODY:
                                    case InsertionMode.IN_CELL:
                                    case InsertionMode.IN_CAPTION:
                                        if (start < i)
                                        {
                                            AccumulateCharacters(buf, start, i
                                                                             - start);
                                            start = i;
                                        }

                                        /*
										 * Reconstruct the active formatting
										 * elements, if any.
										 */
                                        if (!IsInForeignButNotHtmlIntegrationPoint)
                                        {
                                            FlushCharacters();
                                            ReconstructTheActiveFormattingElements();
                                        }
                                        /*
										 * Append the token's character to the
										 * current node.
										 */
                                        goto continueCharactersloop;
                                    case InsertionMode.IN_SELECT:
                                    case InsertionMode.IN_SELECT_IN_TABLE:
                                        goto continueCharactersloop;
                                    case InsertionMode.IN_TABLE:
                                    case InsertionMode.IN_TABLE_BODY:
                                    case InsertionMode.IN_ROW:
                                        AccumulateCharactersForced(buf, i, 1);
                                        start = i + 1;
                                        continue;
                                    case InsertionMode.AFTER_BODY:
                                    case InsertionMode.AFTER_AFTER_BODY:
                                    case InsertionMode.AFTER_AFTER_FRAMESET:
                                        if (start < i)
                                        {
                                            AccumulateCharacters(buf, start, i
                                                                             - start);
                                            start = i;
                                        }
                                        /*
										 * Reconstruct the active formatting
										 * elements, if any.
										 */
                                        FlushCharacters();
                                        ReconstructTheActiveFormattingElements();
                                        /*
										 * Append the token's character to the
										 * current node.
										 */
                                        continue;
                                }
                                goto default;
                            default:
                                /*
								 * A character token that is not one of one of
								 * U+0009 CHARACTER TABULATION, U+000A LINE FEED
								 * (LF), U+000C FORM FEED (FF), or U+0020 SPACE
								 */
                                switch (_mode)
                                {
                                    case InsertionMode.INITIAL:
                                        /*
										 * Parse error.
										 */
                                        // [NOCPP[
                                        switch (DoctypeExpectation)
                                        {
                                            case DoctypeExpectation.Auto:
                                                Err(
                                                    "Non-space characters found without seeing a doctype first. Expected e.g. \u201C<!DOCTYPE html>\u201D.");
                                                break;
                                            case DoctypeExpectation.Html:
                                                Err(
                                                    "Non-space characters found without seeing a doctype first. Expected \u201C<!DOCTYPE html>\u201D.");
                                                break;
                                            case DoctypeExpectation.Html401Strict:
                                                Err(
                                                    "Non-space characters found without seeing a doctype first. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01//EN\" \"http://www.w3.org/TR/html4/strict.dtd\">\u201D.");
                                                break;
                                            case DoctypeExpectation.Html401Transitional:
                                                Err(
                                                    "Non-space characters found without seeing a doctype first. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\" \"http://www.w3.org/TR/html4/loose.dtd\">\u201D.");
                                                break;
                                            case DoctypeExpectation.NoDoctypeErrors:
                                                break;
                                        }
                                        // ]NOCPP]
                                        /*
										 * 
										 * Set the document to quirks mode.
										 */
                                        DocumentModeInternal(
                                            DocumentMode.QuirksMode, null,
                                            null, false);
                                        /*
										 * Then, switch to the root element mode of
										 * the tree construction stage
										 */
                                        _mode = InsertionMode.BEFORE_HTML;
                                        /*
										 * and reprocess the current token.
										 */
                                        i--;
                                        continue;
                                    case InsertionMode.BEFORE_HTML:
                                        /*
										 * Create an HTMLElement node with the tag
										 * name html, in the HTML namespace. Append
										 * it to the Document object.
										 */
                                        // No need to flush characters here,
                                        // because there's nothing to flush.
                                        AppendHtmlElementToDocumentAndPush();
                                        /* Switch to the main mode */
                                        _mode = InsertionMode.BEFORE_HEAD;
                                        /*
										 * reprocess the current token.
										 */
                                        i--;
                                        continue;
                                    case InsertionMode.BEFORE_HEAD:
                                        if (start < i)
                                        {
                                            AccumulateCharacters(buf, start, i
                                                                             - start);
                                            start = i;
                                        }
                                        /*
										 * /Act as if a start tag token with the tag
										 * name "head" and no attributes had been
										 * seen,
										 */
                                        FlushCharacters();
                                        AppendToCurrentNodeAndPushHeadElement(HtmlAttributes.EMPTY_ATTRIBUTES);
                                        _mode = InsertionMode.IN_HEAD;
                                        /*
										 * then reprocess the current token.
										 * 
										 * This will result in an empty head element
										 * being generated, with the current token
										 * being reprocessed in the "after head"
										 * insertion mode.
										 */
                                        i--;
                                        continue;
                                    case InsertionMode.IN_HEAD:
                                        if (start < i)
                                        {
                                            AccumulateCharacters(buf, start, i
                                                                             - start);
                                            start = i;
                                        }
                                        /*
										 * Act as if an end tag token with the tag
										 * name "head" had been seen,
										 */
                                        FlushCharacters();
                                        Pop();
                                        _mode = InsertionMode.AFTER_HEAD;
                                        /*
										 * and reprocess the current token.
										 */
                                        i--;
                                        continue;
                                    case InsertionMode.IN_HEAD_NOSCRIPT:
                                        if (start < i)
                                        {
                                            AccumulateCharacters(buf, start, i
                                                                             - start);
                                            start = i;
                                        }
                                        /*
										 * Parse error. Act as if an end tag with
										 * the tag name "noscript" had been seen
										 */
                                        Err("Non-space character inside \u201Cnoscript\u201D inside \u201Chead\u201D.");
                                        FlushCharacters();
                                        Pop();
                                        _mode = InsertionMode.IN_HEAD;
                                        /*
										 * and reprocess the current token.
										 */
                                        i--;
                                        continue;
                                    case InsertionMode.AFTER_HEAD:
                                        if (start < i)
                                        {
                                            AccumulateCharacters(buf, start, i
                                                                             - start);
                                            start = i;
                                        }
                                        /*
										 * Act as if a start tag token with the tag
										 * name "body" and no attributes had been
										 * seen,
										 */
                                        FlushCharacters();
                                        AppendToCurrentNodeAndPushBodyElement();
                                        _mode = InsertionMode.FRAMESET_OK;
                                        /*
										 * and then reprocess the current token.
										 */
                                        i--;
                                        continue;
                                    case InsertionMode.FRAMESET_OK:
                                        _framesetOk = false;
                                        _mode = InsertionMode.IN_BODY;
                                        i--;
                                        continue;
                                    case InsertionMode.IN_BODY:
                                    case InsertionMode.IN_CELL:
                                    case InsertionMode.IN_CAPTION:
                                        if (start < i)
                                        {
                                            AccumulateCharacters(buf, start, i
                                                                             - start);
                                            start = i;
                                        }
                                        /*
										 * Reconstruct the active formatting
										 * elements, if any.
										 */
                                        if (!IsInForeignButNotHtmlIntegrationPoint)
                                        {
                                            FlushCharacters();
                                            ReconstructTheActiveFormattingElements();
                                        }
                                        /*
										 * Append the token's character to the
										 * current node.
										 */
                                        goto continueCharactersloop;
                                    case InsertionMode.IN_TABLE:
                                    case InsertionMode.IN_TABLE_BODY:
                                    case InsertionMode.IN_ROW:
                                        AccumulateCharactersForced(buf, i, 1);
                                        start = i + 1;
                                        continue;
                                    case InsertionMode.IN_COLUMN_GROUP:
                                        if (start < i)
                                        {
                                            AccumulateCharacters(buf, start, i
                                                                             - start);
                                            start = i;
                                        }
                                        /*
										 * Act as if an end tag with the tag name
										 * "colgroup" had been seen, and then, if
										 * that token wasn't ignored, reprocess the
										 * current token.
										 */
                                        if (_currentPtr == 0)
                                        {
                                            Err("Non-space in \u201Ccolgroup\u201D when parsing fragment.");
                                            start = i + 1;
                                            continue;
                                        }
                                        FlushCharacters();
                                        Pop();
                                        _mode = InsertionMode.IN_TABLE;
                                        i--;
                                        continue;
                                    case InsertionMode.IN_SELECT:
                                    case InsertionMode.IN_SELECT_IN_TABLE:
                                        goto continueCharactersloop;
                                    case InsertionMode.AFTER_BODY:
                                        Err("Non-space character after body.");
                                        Fatal();
                                        _mode = _framesetOk ? InsertionMode.FRAMESET_OK : InsertionMode.IN_BODY;
                                        i--;
                                        continue;
                                    case InsertionMode.IN_FRAMESET:
                                        if (start < i)
                                        {
                                            AccumulateCharacters(buf, start, i - start);
/*
                                            start = i;
*/
                                        }
                                        /*
										 * Parse error.
										 */
                                        Err("Non-space in \u201Cframeset\u201D.");
                                        /*
										 * Ignore the token.
										 */
                                        start = i + 1;
                                        continue;
                                    case InsertionMode.AFTER_FRAMESET:
                                        if (start < i)
                                        {
                                            AccumulateCharacters(buf, start, i
                                                                             - start);
/*
                                            start = i;
*/
                                        }
                                        /*
										 * Parse error.
										 */
                                        Err("Non-space after \u201Cframeset\u201D.");
                                        /*
										 * Ignore the token.
										 */
                                        start = i + 1;
                                        continue;
                                    case InsertionMode.AFTER_AFTER_BODY:
                                        /*
										 * Parse error.
										 */
                                        Err("Non-space character in page trailer.");
                                        /*
										 * Switch back to the main mode and
										 * reprocess the token.
										 */
                                        _mode = _framesetOk ? InsertionMode.FRAMESET_OK : InsertionMode.IN_BODY;
                                        i--;
                                        continue;
                                    case InsertionMode.AFTER_AFTER_FRAMESET:
                                        /*
										 * Parse error.
										 */
                                        Err("Non-space character in page trailer.");
                                        /*
										 * Switch back to the main mode and
										 * reprocess the token.
										 */
                                        _mode = InsertionMode.IN_FRAMESET;
                                        i--;
                                        continue;
                                }
                                break;
                        }

                        continueCharactersloop:
                        ;
                    }
                    if (start < end)
                    {
                        AccumulateCharacters(buf, start, end - start);
                    }
                    break;
            }
        }

        /// <summary>
        ///     Reports a U+0000 that's being turned into a U+FFFD.
        /// </summary>
        public void ZeroOriginatingReplacementCharacter()
        {
            if (_mode == InsertionMode.TEXT)
            {
                AccumulateCharacters(TreeBuilderConstants.REPLACEMENT_CHARACTER, 0, 1);
                return;
            }
            if (_currentPtr >= 0)
            {
                StackNode<T> stackNode = _stack[_currentPtr];
                if (stackNode._ns == "http://www.w3.org/1999/xhtml")
                {
                    return;
                }
                if (stackNode.IsHtmlIntegrationPoint)
                {
                    return;
                }
                //if (stackNode.ns == "http://www.w3.org/1998/Math/MathML"
                //        && stackNode.Group == DispatchGroup.MI_MO_MN_MS_MTEXT)
                //{
                //    return;
                //}
                AccumulateCharacters(TreeBuilderConstants.REPLACEMENT_CHARACTER, 0, 1);
            }
        }

        /// <summary>
        ///     The end-of-file token.
        /// </summary>
        public void Eof()
        {
            FlushCharacters();
            /*eofloop:*/
            for (;;)
            {
                if (IsInForeign)
                {
                    Err("End of file in a foreign namespace context.");
                    goto continueEofloop; // TODO: endless loop???
                }
                switch (_mode)
                {
                    case InsertionMode.INITIAL:
                        /*
						 * Parse error.
						 */
                        // [NOCPP[
                        switch (DoctypeExpectation)
                        {
                            case DoctypeExpectation.Auto:
                                Err(
                                    "End of file seen without seeing a doctype first. Expected e.g. \u201C<!DOCTYPE html>\u201D.");
                                break;
                            case DoctypeExpectation.Html:
                                Err(
                                    "End of file seen without seeing a doctype first. Expected \u201C<!DOCTYPE html>\u201D.");
                                break;
                            case DoctypeExpectation.Html401Strict:
                                Err(
                                    "End of file seen without seeing a doctype first. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01//EN\" \"http://www.w3.org/TR/html4/strict.dtd\">\u201D.");
                                break;
                            case DoctypeExpectation.Html401Transitional:
                                Err(
                                    "End of file seen without seeing a doctype first. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\" \"http://www.w3.org/TR/html4/loose.dtd\">\u201D.");
                                break;
                            case DoctypeExpectation.NoDoctypeErrors:
                                break;
                        }
                        // ]NOCPP]
                        /*
						 * 
						 * Set the document to quirks mode.
						 */
                        DocumentModeInternal(DocumentMode.QuirksMode, null, null,
                                             false);
                        /*
						 * Then, switch to the root element mode of the tree
						 * construction stage
						 */
                        _mode = InsertionMode.BEFORE_HTML;
                        /*
						 * and reprocess the current token.
						 */
                        continue;
                    case InsertionMode.BEFORE_HTML:
                        /*
						 * Create an HTMLElement node with the tag name html, in the
						 * HTML namespace. Append it to the Document object.
						 */
                        AppendHtmlElementToDocumentAndPush();
                        // XXX application cache manifest
                        /* Switch to the main mode */
                        _mode = InsertionMode.BEFORE_HEAD;
                        /*
						 * reprocess the current token.
						 */
                        continue;
                    case InsertionMode.BEFORE_HEAD:
                        AppendToCurrentNodeAndPushHeadElement(HtmlAttributes.EMPTY_ATTRIBUTES);
                        _mode = InsertionMode.IN_HEAD;
                        continue;
                    case InsertionMode.IN_HEAD:
                        if (ErrorEvent != null && _currentPtr > 1)
                        {
                            ErrEndWithUnclosedElements("End of file seen and there were open elements.");
                        }
                        while (_currentPtr > 0)
                        {
                            PopOnEof();
                        }
                        _mode = InsertionMode.AFTER_HEAD;
                        continue;
                    case InsertionMode.IN_HEAD_NOSCRIPT:
                        ErrEndWithUnclosedElements("End of file seen and there were open elements.");
                        while (_currentPtr > 1)
                        {
                            PopOnEof();
                        }
                        _mode = InsertionMode.IN_HEAD;
                        continue;
                    case InsertionMode.AFTER_HEAD:
                        AppendToCurrentNodeAndPushBodyElement();
                        _mode = InsertionMode.IN_BODY;
                        continue;
                    case InsertionMode.IN_COLUMN_GROUP:
                        if (_currentPtr == 0)
                        {
                            Debug.Assert(_fragment);
                            goto breakEofloop;
                        }
                        PopOnEof();
                        _mode = InsertionMode.IN_TABLE;
                        continue;
                    case InsertionMode.FRAMESET_OK:
                    case InsertionMode.IN_CAPTION:
                    case InsertionMode.IN_CELL:
                    case InsertionMode.IN_BODY:
                        // [NOCPP[
                        /*openelementloop:*/
                        for (int i = _currentPtr; i >= 0; i--)
                        {
                            DispatchGroup group = _stack[i].Group;
                            switch (group)
                            {
                                case DispatchGroup.DD_OR_DT:
                                case DispatchGroup.LI:
                                case DispatchGroup.P:
                                case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
                                case DispatchGroup.TD_OR_TH:
                                case DispatchGroup.BODY:
                                case DispatchGroup.HTML:
                                    break;
                                default:
                                    ErrEndWithUnclosedElements("End of file seen and there were open elements.");
                                    goto breakOpenelementloop;
                            }
                        }

                        breakOpenelementloop:
                        // ]NOCPP]
                        goto breakEofloop;
                    case InsertionMode.TEXT:
                        if (ErrorEvent != null)
                        {
                            Err("End of file seen when expecting text or an end tag.");
                            ErrListUnclosedStartTags(0);
                        }
                        // XXX mark script as already executed
                        if (_originalMode == InsertionMode.AFTER_HEAD)
                        {
                            PopOnEof();
                        }
                        PopOnEof();
                        _mode = _originalMode;
                        continue;
                    case InsertionMode.IN_TABLE_BODY:
                    case InsertionMode.IN_ROW:
                    case InsertionMode.IN_TABLE:
                    case InsertionMode.IN_SELECT:
                    case InsertionMode.IN_SELECT_IN_TABLE:
                    case InsertionMode.IN_FRAMESET:
                        if (ErrorEvent != null && _currentPtr > 0)
                        {
                            ErrEndWithUnclosedElements("End of file seen and there were open elements.");
                        }
                        goto breakEofloop;
                    default:
                        // [NOCPP[
                        //if (currentPtr == 0) { // This silliness is here to poison
                        //    // buggy compiler optimizations in
                        //    // GWT
                        //    System.currentTimeMillis();
                        //}
                        // ]NOCPP]
                        goto breakEofloop;
                }

                continueEofloop:
                ;
            }

            breakEofloop:

            while (_currentPtr > 0)
            {
                PopOnEof();
            }
            if (!_fragment)
            {
                PopOnEof();
            }
            /* Stop parsing. */
        }

        /// <summary>
        ///     The perform final cleanup.
        /// </summary>
        public void EndTokenization()
        {
            _formPointer = null;
            _headPointer = null;
            _deepTreeSurrogateParent = null;
            if (_stack != null)
            {
                while (_currentPtr > -1)
                {
                    _currentPtr--;
                }
                _stack = null;
            }
            if (_listOfActiveFormattingElements != null)
            {
                while (_listPtr > -1)
                {
                    //if (listOfActiveFormattingElements[listPtr] != null) {
                    //    listOfActiveFormattingElements[listPtr].Release();
                    //}
                    _listPtr--;
                }
                _listOfActiveFormattingElements = null;
            }
            // [NOCPP[
            _idLocations.Clear();
            // ]NOCPP]
            _charBuffer = null;
            End();
        }

        public void StartTag(ElementName elementName, HtmlAttributes attributes, bool selfClosing)
        {
            FlushCharacters();

            // [NOCPP[
            if (ErrorEvent != null)
            {
                // ID uniqueness
                string id = attributes.Id;
                if (id != null)
                {
                    Locator oldLoc;
                    bool success = _idLocations.TryGetValue(id, out oldLoc);
                    if (success)
                    {
                        Err("Duplicate ID \u201C" + id + "\u201D.");
                        //errorHandler.warning(new SAXParseException(
                        //        "The first occurrence of ID \u201C" + id
                        //        + "\u201D was here.", oldLoc));
                        Warn("The first occurrence of ID \u201C" + id + "\u201D was here.");
                    }
                    else
                    {
                        _idLocations[id] = new Locator(_tokenizer);
                    }
                }
            }
            // ]NOCPP]

            _needToDropLF = false;
            /*starttagloop:*/
            for (;;)
            {
                DispatchGroup group = elementName.Group;
                /*[Local]*/
                string name = elementName.name;
                if (IsInForeign)
                {
                    StackNode<T> currentNode = _stack[_currentPtr];
                    /*[NsUri]*/
                    string currNs = currentNode._ns;
                    if (!(currentNode.IsHtmlIntegrationPoint || (currNs == "http://www.w3.org/1998/Math/MathML" &&
                                                                 ((currentNode.Group == DispatchGroup.MI_MO_MN_MS_MTEXT &&
                                                                   group != DispatchGroup.MGLYPH_OR_MALIGNMARK) ||
                                                                  (currentNode.Group == DispatchGroup.ANNOTATION_XML &&
                                                                   group == DispatchGroup.SVG)))))
                    {
                        switch (group)
                        {
                            case DispatchGroup.B_OR_BIG_OR_CODE_OR_EM_OR_I_OR_S_OR_SMALL_OR_STRIKE_OR_STRONG_OR_TT_OR_U:
                            case DispatchGroup.DIV_OR_BLOCKQUOTE_OR_CENTER_OR_MENU:
                            case DispatchGroup.BODY:
                            case DispatchGroup.BR:
                            case DispatchGroup.RUBY_OR_SPAN_OR_SUB_OR_SUP_OR_VAR:
                            case DispatchGroup.DD_OR_DT:
                            case DispatchGroup.UL_OR_OL_OR_DL:
                            case DispatchGroup.EMBED_OR_IMG:
                            case DispatchGroup.H1_OR_H2_OR_H3_OR_H4_OR_H5_OR_H6:
                            case DispatchGroup.HEAD:
                            case DispatchGroup.HR:
                            case DispatchGroup.LI:
                            case DispatchGroup.META:
                            case DispatchGroup.NOBR:
                            case DispatchGroup.P:
                            case DispatchGroup.PRE_OR_LISTING:
                            case DispatchGroup.TABLE:
                                Err("HTML start tag \u201C"
                                    + name
                                    + "\u201D in a foreign namespace context.");
                                while (!IsSpecialParentInForeign(_stack[_currentPtr]))
                                {
                                    Pop();
                                }
                                goto continueStarttagloop;
                            case DispatchGroup.FONT:
                                if (attributes.Contains(AttributeName.COLOR)
                                    || attributes.Contains(AttributeName.FACE)
                                    || attributes.Contains(AttributeName.SIZE))
                                {
                                    Err("HTML start tag \u201C"
                                        + name
                                        + "\u201D in a foreign namespace context.");
                                    while (!IsSpecialParentInForeign(_stack[_currentPtr]))
                                    {
                                        Pop();
                                    }
                                    goto continueStarttagloop;
                                }
                                // else fall through
                                goto default;
                            default:
                                if ("http://www.w3.org/2000/svg" == currNs)
                                {
                                    attributes.AdjustForSvg();
                                    if (selfClosing)
                                    {
                                        AppendVoidElementToCurrentMayFosterSVG(
                                            elementName, attributes);
                                        selfClosing = false;
                                    }
                                    else
                                    {
                                        AppendToCurrentNodeAndPushElementMayFosterSVG(
                                            elementName, attributes);
                                    }
                                    goto breakStarttagloop;
                                }
                                attributes.AdjustForMath();
                                if (selfClosing)
                                {
                                    AppendVoidElementToCurrentMayFosterMathML(
                                        elementName, attributes);
                                    selfClosing = false;
                                }
                                else
                                {
                                    AppendToCurrentNodeAndPushElementMayFosterMathML(
                                        elementName, attributes);
                                }
                                goto breakStarttagloop;
                        } // switch
                    } // foreignObject / annotation-xml
                }
                int eltPos;
                switch (_mode)
                {
                    case InsertionMode.IN_TABLE_BODY:
                        switch (group)
                        {
                            case DispatchGroup.TR:
                                ClearStackBackTo(FindLastInTableScopeOrRootTbodyTheadTfoot());
                                AppendToCurrentNodeAndPushElement(
                                    elementName,
                                    attributes);
                                _mode = InsertionMode.IN_ROW;
                                goto breakStarttagloop;
                            case DispatchGroup.TD_OR_TH:
                                Err("\u201C" + name
                                    + "\u201D start tag in table body.");
                                ClearStackBackTo(FindLastInTableScopeOrRootTbodyTheadTfoot());
                                AppendToCurrentNodeAndPushElement(
                                    ElementName.TR,
                                    HtmlAttributes.EMPTY_ATTRIBUTES);
                                _mode = InsertionMode.IN_ROW;
                                continue;
                            case DispatchGroup.CAPTION:
                            case DispatchGroup.COL:
                            case DispatchGroup.COLGROUP:
                            case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
                                eltPos = FindLastInTableScopeOrRootTbodyTheadTfoot();
                                if (eltPos == 0)
                                {
                                    ErrStrayStartTag(name);
                                    goto breakStarttagloop;
                                }
                                ClearStackBackTo(eltPos);
                                Pop();
                                _mode = InsertionMode.IN_TABLE;
                                continue;
                                // fall through to IN_TABLE (TODO: IN_ROW?)
                        }
                        goto case InsertionMode.IN_ROW;
                    case InsertionMode.IN_ROW:
                        switch (group)
                        {
                            case DispatchGroup.TD_OR_TH:
                                ClearStackBackTo(FindLastOrRoot(DispatchGroup.TR));
                                AppendToCurrentNodeAndPushElement(
                                    elementName,
                                    attributes);
                                _mode = InsertionMode.IN_CELL;
                                InsertMarker();
                                goto breakStarttagloop;
                            case DispatchGroup.CAPTION:
                            case DispatchGroup.COL:
                            case DispatchGroup.COLGROUP:
                            case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
                            case DispatchGroup.TR:
                                eltPos = FindLastOrRoot(DispatchGroup.TR);
                                if (eltPos == 0)
                                {
                                    Debug.Assert(_fragment);
                                    Err("No table row to close.");
                                    goto breakStarttagloop;
                                }
                                ClearStackBackTo(eltPos);
                                Pop();
                                _mode = InsertionMode.IN_TABLE_BODY;
                                continue;
                                // fall through to IN_TABLE
                        }
                        goto case InsertionMode.IN_TABLE;
                    case InsertionMode.IN_TABLE:
                        /*intableloop:*/
                        for (;;)
                        {
                            switch (group)
                            {
                                case DispatchGroup.CAPTION:
                                    ClearStackBackTo(FindLastOrRoot(DispatchGroup.TABLE));
                                    InsertMarker();
                                    AppendToCurrentNodeAndPushElement(
                                        elementName,
                                        attributes);
                                    _mode = InsertionMode.IN_CAPTION;
                                    goto breakStarttagloop;
                                case DispatchGroup.COLGROUP:
                                    ClearStackBackTo(FindLastOrRoot(DispatchGroup.TABLE));
                                    AppendToCurrentNodeAndPushElement(
                                        elementName,
                                        attributes);
                                    _mode = InsertionMode.IN_COLUMN_GROUP;
                                    goto breakStarttagloop;
                                case DispatchGroup.COL:
                                    ClearStackBackTo(FindLastOrRoot(DispatchGroup.TABLE));
                                    AppendToCurrentNodeAndPushElement(
                                        ElementName.COLGROUP,
                                        HtmlAttributes.EMPTY_ATTRIBUTES);
                                    _mode = InsertionMode.IN_COLUMN_GROUP;
                                    goto continueStarttagloop;
                                case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
                                    ClearStackBackTo(FindLastOrRoot(DispatchGroup.TABLE));
                                    AppendToCurrentNodeAndPushElement(
                                        elementName,
                                        attributes);
                                    _mode = InsertionMode.IN_TABLE_BODY;
                                    goto breakStarttagloop;
                                case DispatchGroup.TR:
                                case DispatchGroup.TD_OR_TH:
                                    ClearStackBackTo(FindLastOrRoot(DispatchGroup.TABLE));
                                    AppendToCurrentNodeAndPushElement(
                                        ElementName.TBODY,
                                        HtmlAttributes.EMPTY_ATTRIBUTES);
                                    _mode = InsertionMode.IN_TABLE_BODY;
                                    goto continueStarttagloop;
                                case DispatchGroup.TABLE:
                                    Err(
                                        "Start tag for \u201Ctable\u201D seen but the previous \u201Ctable\u201D is still open.");
                                    eltPos = FindLastInTableScope(name);
                                    if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
                                    {
                                        Debug.Assert(_fragment);
                                        goto breakStarttagloop;
                                    }
                                    GenerateImpliedEndTags();
                                    // XXX is the next if dead code?
                                    if (ErrorEvent != null && !IsCurrent("table"))
                                    {
                                        Err("Unclosed elements on stack.");
                                    }
                                    while (_currentPtr >= eltPos)
                                    {
                                        Pop();
                                    }
                                    ResetTheInsertionMode();
                                    goto continueStarttagloop;
                                case DispatchGroup.SCRIPT:
                                    // XXX need to manage much more stuff
                                    // here if
                                    // supporting
                                    // document.write()
                                    AppendToCurrentNodeAndPushElement(
                                        elementName,
                                        attributes);
                                    _originalMode = _mode;
                                    _mode = InsertionMode.TEXT;
                                    _tokenizer.SetStateAndEndTagExpectation(
                                        Tokenizer.SCRIPT_DATA, elementName);
                                    goto breakStarttagloop;
                                case DispatchGroup.STYLE:
                                    AppendToCurrentNodeAndPushElement(
                                        elementName,
                                        attributes);
                                    _originalMode = _mode;
                                    _mode = InsertionMode.TEXT;
                                    _tokenizer.SetStateAndEndTagExpectation(
                                        Tokenizer.RAWTEXT, elementName);
                                    goto breakStarttagloop;
                                case DispatchGroup.INPUT:
                                    if (!Portability.LowerCaseLiteralEqualsIgnoreAsciiCaseString(
                                        "hidden",
                                        attributes.GetValue(AttributeName.TYPE)))
                                    {
                                        goto breakIntableloop;
                                    }
                                    AppendVoidElementToCurrent(
                                        name, attributes,
                                        _formPointer);
                                    selfClosing = false;
                                    goto breakStarttagloop;
                                case DispatchGroup.FORM:
                                    if (_formPointer != null)
                                    {
                                        Err(
                                            "Saw a \u201Cform\u201D start tag, but there was already an active \u201Cform\u201D element. Nested forms are not allowed. Ignoring the tag.");
                                        goto breakStarttagloop;
                                    }
                                    Err("Start tag \u201Cform\u201D seen in \u201Ctable\u201D.");
                                    AppendVoidFormToCurrent(attributes);
                                    goto breakStarttagloop;
                                default:
                                    Err("Start tag \u201C" + name
                                        + "\u201D seen in \u201Ctable\u201D.");
                                    // fall through to IN_BODY (TODO: IN_CAPTION?)
                                    goto breakIntableloop;
                            }
                        }

                        breakIntableloop:
                        goto case InsertionMode.IN_CAPTION;

                    case InsertionMode.IN_CAPTION:
                        switch (group)
                        {
                            case DispatchGroup.CAPTION:
                            case DispatchGroup.COL:
                            case DispatchGroup.COLGROUP:
                            case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
                            case DispatchGroup.TR:
                            case DispatchGroup.TD_OR_TH:
                                ErrStrayStartTag(name);
                                eltPos = FindLastInTableScope("caption");
                                if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
                                {
                                    goto breakStarttagloop;
                                }
                                GenerateImpliedEndTags();
                                if (ErrorEvent != null && _currentPtr != eltPos)
                                {
                                    Err("Unclosed elements on stack.");
                                }
                                while (_currentPtr >= eltPos)
                                {
                                    Pop();
                                }
                                ClearTheListOfActiveFormattingElementsUpToTheLastMarker();
                                _mode = InsertionMode.IN_TABLE;
                                continue;
                                // fall through to IN_BODY (TODO: IN_CELL?)
                        }
                        goto case InsertionMode.IN_CELL;
                    case InsertionMode.IN_CELL:
                        switch (group)
                        {
                            case DispatchGroup.CAPTION:
                            case DispatchGroup.COL:
                            case DispatchGroup.COLGROUP:
                            case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
                            case DispatchGroup.TR:
                            case DispatchGroup.TD_OR_TH:
                                eltPos = FindLastInTableScopeTdTh();
                                if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
                                {
                                    Err("No cell to close.");
                                    goto breakStarttagloop;
                                }
                                CloseTheCell(eltPos);
                                continue;
                                // fall through to IN_BODY (TODO: FRAMESET_OK?)
                        }
                        goto case InsertionMode.FRAMESET_OK;
                    case InsertionMode.FRAMESET_OK:
                        switch (group)
                        {
                            case DispatchGroup.FRAMESET:
                                if (_mode == InsertionMode.FRAMESET_OK)
                                {
                                    if (_currentPtr == 0 || _stack[1].Group != DispatchGroup.BODY)
                                    {
                                        Debug.Assert(_fragment);
                                        ErrStrayStartTag(name);
                                        goto breakStarttagloop;
                                    }
                                    Err("\u201Cframeset\u201D start tag seen.");
                                    DetachFromParent(_stack[1]._node);
                                    while (_currentPtr > 0)
                                    {
                                        Pop();
                                    }
                                    AppendToCurrentNodeAndPushElement(
                                        elementName,
                                        attributes);
                                    _mode = InsertionMode.IN_FRAMESET;
                                    goto breakStarttagloop;
                                }
                                ErrStrayStartTag(name);
                                goto breakStarttagloop;
                                // NOT falling through!
                            case DispatchGroup.PRE_OR_LISTING:
                            case DispatchGroup.LI:
                            case DispatchGroup.DD_OR_DT:
                            case DispatchGroup.BUTTON:
                            case DispatchGroup.MARQUEE_OR_APPLET:
                            case DispatchGroup.OBJECT:
                            case DispatchGroup.TABLE:
                            case DispatchGroup.AREA_OR_WBR:
                            case DispatchGroup.BR:
                            case DispatchGroup.EMBED_OR_IMG:
                            case DispatchGroup.INPUT:
                            case DispatchGroup.KEYGEN:
                            case DispatchGroup.HR:
                            case DispatchGroup.TEXTAREA:
                            case DispatchGroup.XMP:
                            case DispatchGroup.IFRAME:
                            case DispatchGroup.SELECT:
                                if (_mode == InsertionMode.FRAMESET_OK
                                    &&
                                    !(group == DispatchGroup.INPUT &&
                                      Portability.LowerCaseLiteralEqualsIgnoreAsciiCaseString(
                                          "hidden",
                                          attributes.GetValue(AttributeName.TYPE))))
                                {
                                    _framesetOk = false;
                                    _mode = InsertionMode.IN_BODY;
                                }
                                // fall through to IN_BODY
                                break;
                                // fall through to IN_BODY
                        }
                        goto case InsertionMode.IN_BODY;
                    case InsertionMode.IN_BODY:
                        /*inbodyloop:*/
                        for (;;)
                        {
                            switch (group)
                            {
                                case DispatchGroup.HTML:
                                    ErrStrayStartTag(name);
                                    if (!_fragment)
                                    {
                                        AddAttributesToHtml(attributes);
                                    }
                                    goto breakStarttagloop;
                                case DispatchGroup.BASE:
                                case DispatchGroup.LINK_OR_BASEFONT_OR_BGSOUND:
                                case DispatchGroup.META:
                                case DispatchGroup.STYLE:
                                case DispatchGroup.SCRIPT:
                                case DispatchGroup.TITLE:
                                case DispatchGroup.COMMAND:
                                    // Fall through to IN_HEAD
                                    goto breakInbodyloop;
                                case DispatchGroup.BODY:
                                    if (_currentPtr == 0
                                        || _stack[1].Group != DispatchGroup.BODY)
                                    {
                                        Debug.Assert(_fragment);
                                        ErrStrayStartTag(name);
                                        goto breakStarttagloop;
                                    }
                                    Err(
                                        "\u201Cbody\u201D start tag found but the \u201Cbody\u201D element is already open.");
                                    _framesetOk = false;
                                    if (_mode == InsertionMode.FRAMESET_OK)
                                    {
                                        _mode = InsertionMode.IN_BODY;
                                    }
                                    goto breakStarttagloop;
                                case DispatchGroup.P:
                                case DispatchGroup.DIV_OR_BLOCKQUOTE_OR_CENTER_OR_MENU:
                                case DispatchGroup.UL_OR_OL_OR_DL:
                                case
                                    DispatchGroup
                                        .ADDRESS_OR_ARTICLE_OR_ASIDE_OR_DETAILS_OR_DIR_OR_FIGCAPTION_OR_FIGURE_OR_FOOTER_OR_HEADER_OR_HGROUP_OR_NAV_OR_SECTION_OR_SUMMARY
                                    :
                                    ImplicitlyCloseP();
                                    AppendToCurrentNodeAndPushElementMayFoster(
                                        elementName,
                                        attributes);
                                    goto breakStarttagloop;
                                case DispatchGroup.H1_OR_H2_OR_H3_OR_H4_OR_H5_OR_H6:
                                    ImplicitlyCloseP();
                                    if (_stack[_currentPtr].Group == DispatchGroup.H1_OR_H2_OR_H3_OR_H4_OR_H5_OR_H6)
                                    {
                                        Err("Heading cannot be a child of another heading.");
                                        Pop();
                                    }
                                    AppendToCurrentNodeAndPushElementMayFoster(
                                        elementName,
                                        attributes);
                                    goto breakStarttagloop;
                                case DispatchGroup.FIELDSET:
                                    ImplicitlyCloseP();
                                    AppendToCurrentNodeAndPushElementMayFoster(
                                        elementName,
                                        attributes, _formPointer);
                                    goto breakStarttagloop;
                                case DispatchGroup.PRE_OR_LISTING:
                                    ImplicitlyCloseP();
                                    AppendToCurrentNodeAndPushElementMayFoster(
                                        elementName,
                                        attributes);
                                    _needToDropLF = true;
                                    goto breakStarttagloop;
                                case DispatchGroup.FORM:
                                    if (_formPointer != null)
                                    {
                                        Err(
                                            "Saw a \u201Cform\u201D start tag, but there was already an active \u201Cform\u201D element. Nested forms are not allowed. Ignoring the tag.");
                                        goto breakStarttagloop;
                                    }
                                    ImplicitlyCloseP();
                                    AppendToCurrentNodeAndPushFormElementMayFoster(attributes);
                                    goto breakStarttagloop;
                                case DispatchGroup.LI:
                                case DispatchGroup.DD_OR_DT:
                                    eltPos = _currentPtr;
                                    for (;;)
                                    {
                                        StackNode<T> node = _stack[eltPos]; // weak
                                        // ref
                                        if (node.Group == group)
                                        {
                                            // LI or
                                            // DD_OR_DT
                                            GenerateImpliedEndTagsExceptFor(node._name);
                                            if (ErrorEvent != null
                                                && eltPos != _currentPtr)
                                            {
                                                ErrUnclosedElementsImplied(eltPos, name);
                                            }
                                            while (_currentPtr >= eltPos)
                                            {
                                                Pop();
                                            }
                                            break;
                                        }
                                        if (node.IsScoping
                                            || (node.IsSpecial
                                                && node._name != "p"
                                                && node._name != "address" && node._name != "div"))
                                        {
                                            break;
                                        }
                                        eltPos--;
                                    }
                                    ImplicitlyCloseP();
                                    AppendToCurrentNodeAndPushElementMayFoster(
                                        elementName,
                                        attributes);
                                    goto breakStarttagloop;
                                case DispatchGroup.PLAINTEXT:
                                    ImplicitlyCloseP();
                                    AppendToCurrentNodeAndPushElementMayFoster(
                                        elementName,
                                        attributes);
                                    _tokenizer.SetStateAndEndTagExpectation(
                                        Tokenizer.PLAINTEXT, elementName);
                                    goto breakStarttagloop;
                                case DispatchGroup.A:
                                    int activeAPos =
                                        FindInListOfActiveFormattingElementsContainsBetweenEndAndLastMarker("a");
                                    if (activeAPos != -1)
                                    {
                                        Err(
                                            "An \u201Ca\u201D start tag seen with already an active \u201Ca\u201D element.");
                                        StackNode<T> activeA = _listOfActiveFormattingElements[activeAPos];
                                        AdoptionAgencyEndTag("a");
                                        RemoveFromStack(activeA);
                                        activeAPos = FindInListOfActiveFormattingElements(activeA);
                                        if (activeAPos != -1)
                                        {
                                            RemoveFromListOfActiveFormattingElements(activeAPos);
                                        }
                                    }
                                    ReconstructTheActiveFormattingElements();
                                    AppendToCurrentNodeAndPushFormattingElementMayFoster(
                                        elementName,
                                        attributes);
                                    goto breakStarttagloop;
                                case
                                    DispatchGroup
                                        .B_OR_BIG_OR_CODE_OR_EM_OR_I_OR_S_OR_SMALL_OR_STRIKE_OR_STRONG_OR_TT_OR_U:
                                case DispatchGroup.FONT:
                                    ReconstructTheActiveFormattingElements();
                                    MaybeForgetEarlierDuplicateFormattingElement(elementName.name, attributes);
                                    AppendToCurrentNodeAndPushFormattingElementMayFoster(
                                        elementName,
                                        attributes);
                                    goto breakStarttagloop;
                                case DispatchGroup.NOBR:
                                    ReconstructTheActiveFormattingElements();
                                    if (TreeBuilderConstants.NOT_FOUND_ON_STACK != FindLastInScope("nobr"))
                                    {
                                        Err(
                                            "\u201Cnobr\u201D start tag seen when there was an open \u201Cnobr\u201D element in scope.");
                                        AdoptionAgencyEndTag("nobr");
                                        ReconstructTheActiveFormattingElements();
                                    }
                                    AppendToCurrentNodeAndPushFormattingElementMayFoster(
                                        elementName,
                                        attributes);
                                    goto breakStarttagloop;
                                case DispatchGroup.BUTTON:
                                    eltPos = FindLastInScope(name);
                                    if (eltPos != TreeBuilderConstants.NOT_FOUND_ON_STACK)
                                    {
                                        Err(
                                            "\u201Cbutton\u201D start tag seen when there was an open \u201Cbutton\u201D element in scope.");

                                        GenerateImpliedEndTags();
                                        if (ErrorEvent != null && !IsCurrent(name))
                                        {
                                            ErrUnclosedElementsImplied(eltPos, name);
                                        }
                                        while (_currentPtr >= eltPos)
                                        {
                                            Pop();
                                        }
                                        goto continueStarttagloop;
                                    }
                                    ReconstructTheActiveFormattingElements();
                                    AppendToCurrentNodeAndPushElementMayFoster(
                                        elementName,
                                        attributes, _formPointer);
                                    goto breakStarttagloop;
                                case DispatchGroup.OBJECT:
                                    ReconstructTheActiveFormattingElements();
                                    AppendToCurrentNodeAndPushElementMayFoster(
                                        elementName,
                                        attributes, _formPointer);
                                    InsertMarker();
                                    goto breakStarttagloop;
                                case DispatchGroup.MARQUEE_OR_APPLET:
                                    ReconstructTheActiveFormattingElements();
                                    AppendToCurrentNodeAndPushElementMayFoster(
                                        elementName,
                                        attributes);
                                    InsertMarker();
                                    goto breakStarttagloop;
                                case DispatchGroup.TABLE:
                                    // The only quirk. Blame Hixie and
                                    // Acid2.
                                    if (!_quirks)
                                    {
                                        ImplicitlyCloseP();
                                    }
                                    AppendToCurrentNodeAndPushElementMayFoster(
                                        elementName,
                                        attributes);
                                    _mode = InsertionMode.IN_TABLE;
                                    goto breakStarttagloop;
                                case DispatchGroup.BR:
                                case DispatchGroup.EMBED_OR_IMG:
                                case DispatchGroup.AREA_OR_WBR:
                                    ReconstructTheActiveFormattingElements();
                                    // FALL THROUGH to PARAM_OR_SOURCE_OR_TRACK
                                    goto case DispatchGroup.PARAM_OR_SOURCE_OR_TRACK;
                                case DispatchGroup.PARAM_OR_SOURCE_OR_TRACK:
                                    AppendVoidElementToCurrentMayFoster(
                                        elementName,
                                        attributes);
                                    selfClosing = false;
                                    goto breakStarttagloop;
                                case DispatchGroup.HR:
                                    ImplicitlyCloseP();
                                    AppendVoidElementToCurrentMayFoster(
                                        elementName,
                                        attributes);
                                    selfClosing = false;
                                    goto breakStarttagloop;
                                case DispatchGroup.IMAGE:
                                    Err("Saw a start tag \u201Cimage\u201D.");
                                    elementName = ElementName.IMG;
                                    goto continueStarttagloop;
                                case DispatchGroup.KEYGEN:
                                case DispatchGroup.INPUT:
                                    ReconstructTheActiveFormattingElements();
                                    AppendVoidElementToCurrentMayFoster(
                                        name, attributes,
                                        _formPointer);
                                    selfClosing = false;
                                    goto breakStarttagloop;
                                case DispatchGroup.ISINDEX:
                                    Err("\u201Cisindex\u201D seen.");
                                    if (_formPointer != null)
                                    {
                                        goto breakStarttagloop;
                                    }
                                    ImplicitlyCloseP();
                                    var formAttrs = new HtmlAttributes(0);
                                    int actionIndex = attributes.GetIndex(AttributeName.ACTION);
                                    if (actionIndex > -1)
                                    {
                                        formAttrs.AddAttribute(
                                            AttributeName.ACTION,
                                            attributes.GetValue(actionIndex)
                                            // [NOCPP[
                                            , XmlViolationPolicy.Allow
                                            // ]NOCPP]
                                            );
                                    }
                                    AppendToCurrentNodeAndPushFormElementMayFoster(formAttrs);
                                    AppendVoidElementToCurrentMayFoster(
                                        ElementName.HR,
                                        HtmlAttributes.EMPTY_ATTRIBUTES);
                                    AppendToCurrentNodeAndPushElementMayFoster(
                                        ElementName.LABEL,
                                        HtmlAttributes.EMPTY_ATTRIBUTES);
                                    int promptIndex = attributes.GetIndex(AttributeName.PROMPT);
                                    if (promptIndex > -1)
                                    {
                                        char[] prompt = attributes.GetValue(promptIndex).ToCharArray();
                                        AppendCharacters(_stack[_currentPtr]._node,
                                                         prompt, 0, prompt.Length);
                                    }
                                    else
                                    {
                                        AppendIsindexPrompt(_stack[_currentPtr]._node);
                                    }
                                    var inputAttributes = new HtmlAttributes(
                                        0);
                                    inputAttributes.AddAttribute(
                                        AttributeName.NAME,
                                        "isindex"
                                        // [NOCPP[
                                        , XmlViolationPolicy.Allow
                                        // ]NOCPP]
                                        );
                                    for (int i = 0; i < attributes.Length; i++)
                                    {
                                        AttributeName attributeQName = attributes.GetAttributeName(i);
                                        if (AttributeName.NAME == attributeQName
                                            || AttributeName.PROMPT == attributeQName)
                                        {
                                            //attributes.ReleaseValue(i);
                                        }
                                        else if (AttributeName.ACTION != attributeQName)
                                        {
                                            inputAttributes.AddAttribute(
                                                attributeQName,
                                                attributes.GetValue(i)
                                                // [NOCPP[
                                                , XmlViolationPolicy.Allow
                                                // ]NOCPP]
                                                );
                                        }
                                    }
                                    attributes.ClearWithoutReleasingContents();
                                    AppendVoidElementToCurrentMayFoster(
                                        "input",
                                        inputAttributes, _formPointer);
                                    Pop(); // label
                                    AppendVoidElementToCurrentMayFoster(
                                        ElementName.HR,
                                        HtmlAttributes.EMPTY_ATTRIBUTES);
                                    Pop(); // form
                                    selfClosing = false;
                                    // Portability.delete(formAttrs);
                                    // Portability.delete(inputAttributes);
                                    // Don't delete attributes, they are deleted
                                    // later
                                    goto breakStarttagloop;
                                case DispatchGroup.TEXTAREA:
                                    AppendToCurrentNodeAndPushElementMayFoster(
                                        elementName,
                                        attributes, _formPointer);
                                    _tokenizer.SetStateAndEndTagExpectation(
                                        Tokenizer.RCDATA, elementName);
                                    _originalMode = _mode;
                                    _mode = InsertionMode.TEXT;
                                    _needToDropLF = true;
                                    goto breakStarttagloop;
                                case DispatchGroup.XMP:
                                    ImplicitlyCloseP();
                                    ReconstructTheActiveFormattingElements();
                                    AppendToCurrentNodeAndPushElementMayFoster(
                                        elementName,
                                        attributes);
                                    _originalMode = _mode;
                                    _mode = InsertionMode.TEXT;
                                    _tokenizer.SetStateAndEndTagExpectation(
                                        Tokenizer.RAWTEXT, elementName);
                                    goto breakStarttagloop;
                                case DispatchGroup.NOSCRIPT:
                                    if (!IsScriptingEnabled)
                                    {
                                        ReconstructTheActiveFormattingElements();
                                        AppendToCurrentNodeAndPushElementMayFoster(
                                            elementName,
                                            attributes);
                                        goto breakStarttagloop;
                                    }
                                    // fall through
                                    goto case DispatchGroup.NOFRAMES;
                                case DispatchGroup.NOFRAMES:
                                case DispatchGroup.IFRAME:
                                case DispatchGroup.NOEMBED:
                                    AppendToCurrentNodeAndPushElementMayFoster(
                                        elementName,
                                        attributes);
                                    _originalMode = _mode;
                                    _mode = InsertionMode.TEXT;
                                    _tokenizer.SetStateAndEndTagExpectation(
                                        Tokenizer.RAWTEXT, elementName);
                                    goto breakStarttagloop;
                                case DispatchGroup.SELECT:
                                    ReconstructTheActiveFormattingElements();
                                    AppendToCurrentNodeAndPushElementMayFoster(
                                        elementName,
                                        attributes, _formPointer);
                                    switch (_mode)
                                    {
                                        case InsertionMode.IN_TABLE:
                                        case InsertionMode.IN_CAPTION:
                                        case InsertionMode.IN_COLUMN_GROUP:
                                        case InsertionMode.IN_TABLE_BODY:
                                        case InsertionMode.IN_ROW:
                                        case InsertionMode.IN_CELL:
                                            _mode = InsertionMode.IN_SELECT_IN_TABLE;
                                            break;
                                        default:
                                            _mode = InsertionMode.IN_SELECT;
                                            break;
                                    }
                                    goto breakStarttagloop;
                                case DispatchGroup.OPTGROUP:
                                case DispatchGroup.OPTION:
                                    if (IsCurrent("option"))
                                    {
                                        Pop();
                                    }
                                    ReconstructTheActiveFormattingElements();
                                    AppendToCurrentNodeAndPushElementMayFoster(
                                        elementName,
                                        attributes);
                                    goto breakStarttagloop;
                                case DispatchGroup.RT_OR_RP:
                                    /*
									 * If the stack of open elements has a ruby
									 * element in scope, then generate implied end
									 * tags. If the current node is not then a ruby
									 * element, this is a parse error; pop all the
									 * nodes from the current node up to the node
									 * immediately before the bottommost ruby
									 * element on the stack of open elements.
									 * 
									 * Insert an HTML element for the token.
									 */
                                    eltPos = FindLastInScope("ruby");
                                    if (eltPos != TreeBuilderConstants.NOT_FOUND_ON_STACK)
                                    {
                                        GenerateImpliedEndTags();
                                    }
                                    if (eltPos != _currentPtr)
                                    {
                                        if (ErrorEvent != null)
                                        {
                                            if (eltPos != TreeBuilderConstants.NOT_FOUND_ON_STACK)
                                            {
                                                Err("Start tag \u201C"
                                                    + name
                                                    + "\u201D seen without a \u201Cruby\u201D element being open.");
                                            }
                                            else
                                            {
                                                Err("Unclosed children in \u201Cruby\u201D.");
                                            }
                                        }
                                        while (_currentPtr > eltPos)
                                        {
                                            Pop();
                                        }
                                    }
                                    AppendToCurrentNodeAndPushElementMayFoster(
                                        elementName,
                                        attributes);
                                    goto breakStarttagloop;
                                case DispatchGroup.MATH:
                                    ReconstructTheActiveFormattingElements();
                                    attributes.AdjustForMath();
                                    if (selfClosing)
                                    {
                                        AppendVoidElementToCurrentMayFosterMathML(
                                            elementName, attributes);
                                        selfClosing = false;
                                    }
                                    else
                                    {
                                        AppendToCurrentNodeAndPushElementMayFosterMathML(
                                            elementName, attributes);
                                    }
                                    goto breakStarttagloop;
                                case DispatchGroup.SVG:
                                    ReconstructTheActiveFormattingElements();
                                    attributes.AdjustForSvg();
                                    if (selfClosing)
                                    {
                                        AppendVoidElementToCurrentMayFosterSVG(
                                            elementName,
                                            attributes);
                                        selfClosing = false;
                                    }
                                    else
                                    {
                                        AppendToCurrentNodeAndPushElementMayFosterSVG(
                                            elementName, attributes);
                                    }
                                    goto breakStarttagloop;
                                case DispatchGroup.CAPTION:
                                case DispatchGroup.COL:
                                case DispatchGroup.COLGROUP:
                                case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
                                case DispatchGroup.TR:
                                case DispatchGroup.TD_OR_TH:
                                case DispatchGroup.FRAME:
                                case DispatchGroup.FRAMESET:
                                case DispatchGroup.HEAD:
                                    ErrStrayStartTag(name);
                                    goto breakStarttagloop;
                                case DispatchGroup.OUTPUT_OR_LABEL:
                                    ReconstructTheActiveFormattingElements();
                                    AppendToCurrentNodeAndPushElementMayFoster(
                                        elementName,
                                        attributes, _formPointer);
                                    goto breakStarttagloop;
                                default:
                                    ReconstructTheActiveFormattingElements();
                                    AppendToCurrentNodeAndPushElementMayFoster(
                                        elementName,
                                        attributes);
                                    goto breakStarttagloop;
                            }
                        }

                        breakInbodyloop:
                        goto case InsertionMode.IN_HEAD;

                    case InsertionMode.IN_HEAD:
                        /*inheadloop:*/
                        for (;;)
                        {
                            switch (group)
                            {
                                case DispatchGroup.HTML:
                                    ErrStrayStartTag(name);
                                    if (!_fragment)
                                    {
                                        AddAttributesToHtml(attributes);
                                    }
                                    goto breakStarttagloop;
                                case DispatchGroup.BASE:
                                case DispatchGroup.COMMAND:
                                    AppendVoidElementToCurrentMayFoster(
                                        elementName,
                                        attributes);
                                    selfClosing = false;
                                    goto breakStarttagloop;
                                case DispatchGroup.META:
                                case DispatchGroup.LINK_OR_BASEFONT_OR_BGSOUND:
                                    // Fall through to IN_HEAD_NOSCRIPT
                                    goto breakInheadloop;
                                case DispatchGroup.TITLE:
                                    AppendToCurrentNodeAndPushElementMayFoster(
                                        elementName,
                                        attributes);
                                    _originalMode = _mode;
                                    _mode = InsertionMode.TEXT;
                                    _tokenizer.SetStateAndEndTagExpectation(
                                        Tokenizer.RCDATA, elementName);
                                    goto breakStarttagloop;
                                case DispatchGroup.NOSCRIPT:
                                    if (IsScriptingEnabled)
                                    {
                                        AppendToCurrentNodeAndPushElement(
                                            elementName,
                                            attributes);
                                        _originalMode = _mode;
                                        _mode = InsertionMode.TEXT;
                                        _tokenizer.SetStateAndEndTagExpectation(
                                            Tokenizer.RAWTEXT, elementName);
                                    }
                                    else
                                    {
                                        AppendToCurrentNodeAndPushElementMayFoster(
                                            elementName,
                                            attributes);
                                        _mode = InsertionMode.IN_HEAD_NOSCRIPT;
                                    }
                                    goto breakStarttagloop;
                                case DispatchGroup.SCRIPT:
                                    // XXX need to manage much more stuff
                                    // here if
                                    // supporting
                                    // document.write()
                                    AppendToCurrentNodeAndPushElementMayFoster(
                                        elementName,
                                        attributes);
                                    _originalMode = _mode;
                                    _mode = InsertionMode.TEXT;
                                    _tokenizer.SetStateAndEndTagExpectation(
                                        Tokenizer.SCRIPT_DATA, elementName);
                                    goto breakStarttagloop;
                                case DispatchGroup.STYLE:
                                case DispatchGroup.NOFRAMES:
                                    AppendToCurrentNodeAndPushElementMayFoster(
                                        elementName,
                                        attributes);
                                    _originalMode = _mode;
                                    _mode = InsertionMode.TEXT;
                                    _tokenizer.SetStateAndEndTagExpectation(
                                        Tokenizer.RAWTEXT, elementName);
                                    goto breakStarttagloop;
                                case DispatchGroup.HEAD:
                                    /* Parse error. */
                                    Err("Start tag for \u201Chead\u201D seen when \u201Chead\u201D was already open.");
                                    /* Ignore the token. */
                                    goto breakStarttagloop;
                                default:
                                    Pop();
                                    _mode = InsertionMode.AFTER_HEAD;
                                    goto continueStarttagloop;
                            }
                        }

                        breakInheadloop:
                        goto case InsertionMode.IN_HEAD_NOSCRIPT;

                    case InsertionMode.IN_HEAD_NOSCRIPT:
                        switch (group)
                        {
                            case DispatchGroup.HTML:
                                // XXX did Hixie really mean to omit "base"
                                // here?
                                ErrStrayStartTag(name);
                                if (!_fragment)
                                {
                                    AddAttributesToHtml(attributes);
                                }
                                goto breakStarttagloop;
                            case DispatchGroup.LINK_OR_BASEFONT_OR_BGSOUND:
                                AppendVoidElementToCurrentMayFoster(
                                    elementName,
                                    attributes);
                                selfClosing = false;
                                goto breakStarttagloop;
                            case DispatchGroup.META:
                                CheckMetaCharset(attributes);
                                AppendVoidElementToCurrentMayFoster(
                                    elementName,
                                    attributes);
                                selfClosing = false;
                                goto breakStarttagloop;
                            case DispatchGroup.STYLE:
                            case DispatchGroup.NOFRAMES:
                                AppendToCurrentNodeAndPushElement(
                                    elementName,
                                    attributes);
                                _originalMode = _mode;
                                _mode = InsertionMode.TEXT;
                                _tokenizer.SetStateAndEndTagExpectation(
                                    Tokenizer.RAWTEXT, elementName);
                                goto breakStarttagloop;
                            case DispatchGroup.HEAD:
                                Err("Start tag for \u201Chead\u201D seen when \u201Chead\u201D was already open.");
                                goto breakStarttagloop;
                            case DispatchGroup.NOSCRIPT:
                                Err(
                                    "Start tag for \u201Cnoscript\u201D seen when \u201Cnoscript\u201D was already open.");
                                goto breakStarttagloop;
                            default:
                                Err("Bad start tag in \u201C" + name
                                    + "\u201D in \u201Chead\u201D.");
                                Pop();
                                _mode = InsertionMode.IN_HEAD;
                                continue;
                        }
                    case InsertionMode.IN_COLUMN_GROUP:
                        switch (group)
                        {
                            case DispatchGroup.HTML:
                                ErrStrayStartTag(name);
                                if (!_fragment)
                                {
                                    AddAttributesToHtml(attributes);
                                }
                                goto breakStarttagloop;
                            case DispatchGroup.COL:
                                AppendVoidElementToCurrentMayFoster(
                                    elementName,
                                    attributes);
                                selfClosing = false;
                                goto breakStarttagloop;
                            default:
                                if (_currentPtr == 0)
                                {
                                    Debug.Assert(_fragment);
                                    Err("Garbage in \u201Ccolgroup\u201D fragment.");
                                    goto breakStarttagloop;
                                }
                                Pop();
                                _mode = InsertionMode.IN_TABLE;
                                continue;
                        }
                    case InsertionMode.IN_SELECT_IN_TABLE:
                        switch (group)
                        {
                            case DispatchGroup.CAPTION:
                            case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
                            case DispatchGroup.TR:
                            case DispatchGroup.TD_OR_TH:
                            case DispatchGroup.TABLE:
                                Err("\u201C"
                                    + name
                                    + "\u201D start tag with \u201Cselect\u201D open.");
                                eltPos = FindLastInTableScope("select");
                                if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
                                {
                                    Debug.Assert(_fragment);
                                    goto breakStarttagloop; // http://www.w3.org/Bugs/Public/show_bug.cgi?id=8375
                                }
                                while (_currentPtr >= eltPos)
                                {
                                    Pop();
                                }
                                ResetTheInsertionMode();
                                continue;
                                // fall through to IN_SELECT
                        }
                        goto case InsertionMode.IN_SELECT;
                    case InsertionMode.IN_SELECT:
                        switch (group)
                        {
                            case DispatchGroup.HTML:
                                ErrStrayStartTag(name);
                                if (!_fragment)
                                {
                                    AddAttributesToHtml(attributes);
                                }
                                goto breakStarttagloop;
                            case DispatchGroup.OPTION:
                                if (IsCurrent("option"))
                                {
                                    Pop();
                                }
                                AppendToCurrentNodeAndPushElement(
                                    elementName,
                                    attributes);
                                goto breakStarttagloop;
                            case DispatchGroup.OPTGROUP:
                                if (IsCurrent("option"))
                                {
                                    Pop();
                                }
                                if (IsCurrent("optgroup"))
                                {
                                    Pop();
                                }
                                AppendToCurrentNodeAndPushElement(
                                    elementName,
                                    attributes);
                                goto breakStarttagloop;
                            case DispatchGroup.SELECT:
                                Err("\u201Cselect\u201D start tag where end tag expected.");
                                eltPos = FindLastInTableScope(name);
                                if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
                                {
                                    Debug.Assert(_fragment);
                                    Err("No \u201Cselect\u201D in table scope.");
                                    goto breakStarttagloop;
                                }
                                while (_currentPtr >= eltPos)
                                {
                                    Pop();
                                }
                                ResetTheInsertionMode();
                                goto breakStarttagloop;
                            case DispatchGroup.INPUT:
                            case DispatchGroup.TEXTAREA:
                            case DispatchGroup.KEYGEN:
                                Err("\u201C"
                                    + name
                                    + "\u201D start tag seen in \u201Cselect\u2201D.");
                                eltPos = FindLastInTableScope("select");
                                if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
                                {
                                    Debug.Assert(_fragment);
                                    goto breakStarttagloop;
                                }
                                while (_currentPtr >= eltPos)
                                {
                                    Pop();
                                }
                                ResetTheInsertionMode();
                                continue;
                            case DispatchGroup.SCRIPT:
                                // XXX need to manage much more stuff
                                // here if
                                // supporting
                                // document.write()
                                AppendToCurrentNodeAndPushElementMayFoster(
                                    elementName,
                                    attributes);
                                _originalMode = _mode;
                                _mode = InsertionMode.TEXT;
                                _tokenizer.SetStateAndEndTagExpectation(
                                    Tokenizer.SCRIPT_DATA, elementName);
                                goto breakStarttagloop;
                            default:
                                ErrStrayStartTag(name);
                                goto breakStarttagloop;
                        }
                    case InsertionMode.AFTER_BODY:
                        switch (group)
                        {
                            case DispatchGroup.HTML:
                                ErrStrayStartTag(name);
                                if (!_fragment)
                                {
                                    AddAttributesToHtml(attributes);
                                }
                                goto breakStarttagloop;
                            default:
                                ErrStrayStartTag(name);
                                _mode = _framesetOk ? InsertionMode.FRAMESET_OK : InsertionMode.IN_BODY;
                                continue;
                        }
                    case InsertionMode.IN_FRAMESET:
                        switch (group)
                        {
                            case DispatchGroup.FRAMESET:
                                AppendToCurrentNodeAndPushElement(
                                    elementName,
                                    attributes);
                                goto breakStarttagloop;
                            case DispatchGroup.FRAME:
                                AppendVoidElementToCurrentMayFoster(
                                    elementName,
                                    attributes);
                                selfClosing = false;
                                goto breakStarttagloop;
                                // fall through to AFTER_FRAMESET
                        }
                        goto case InsertionMode.AFTER_FRAMESET;
                    case InsertionMode.AFTER_FRAMESET:
                        switch (group)
                        {
                            case DispatchGroup.HTML:
                                ErrStrayStartTag(name);
                                if (!_fragment)
                                {
                                    AddAttributesToHtml(attributes);
                                }
                                goto breakStarttagloop;
                            case DispatchGroup.NOFRAMES:
                                AppendToCurrentNodeAndPushElement(
                                    elementName,
                                    attributes);
                                _originalMode = _mode;
                                _mode = InsertionMode.TEXT;
                                _tokenizer.SetStateAndEndTagExpectation(
                                    Tokenizer.RAWTEXT, elementName);
                                goto breakStarttagloop;
                            default:
                                ErrStrayStartTag(name);
                                goto breakStarttagloop;
                        }
                    case InsertionMode.INITIAL:
                        /*
						 * Parse error.
						 */
                        // [NOCPP[
                        switch (DoctypeExpectation)
                        {
                            case DoctypeExpectation.Auto:
                                Err(
                                    "Start tag seen without seeing a doctype first. Expected e.g. \u201C<!DOCTYPE html>\u201D.");
                                break;
                            case DoctypeExpectation.Html:
                                Err(
                                    "Start tag seen without seeing a doctype first. Expected \u201C<!DOCTYPE html>\u201D.");
                                break;
                            case DoctypeExpectation.Html401Strict:
                                Err(
                                    "Start tag seen without seeing a doctype first. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01//EN\" \"http://www.w3.org/TR/html4/strict.dtd\">\u201D.");
                                break;
                            case DoctypeExpectation.Html401Transitional:
                                Err(
                                    "Start tag seen without seeing a doctype first. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\" \"http://www.w3.org/TR/html4/loose.dtd\">\u201D.");
                                break;
                            case DoctypeExpectation.NoDoctypeErrors:
                                break;
                        }
                        // ]NOCPP]
                        /*
						 * 
						 * Set the document to quirks mode.
						 */
                        DocumentModeInternal(DocumentMode.QuirksMode, null, null,
                                             false);
                        /*
						 * Then, switch to the root element mode of the tree
						 * construction stage
						 */
                        _mode = InsertionMode.BEFORE_HTML;
                        /*
						 * and reprocess the current token.
						 */
                        continue;
                    case InsertionMode.BEFORE_HTML:
                        switch (group)
                        {
                            case DispatchGroup.HTML:
                                // optimize error check and streaming SAX by
                                // hoisting
                                // "html" handling here.
                                if (attributes == HtmlAttributes.EMPTY_ATTRIBUTES)
                                {
                                    // This has the right magic side effect
                                    // that
                                    // it
                                    // makes attributes in SAX Tree mutable.
                                    AppendHtmlElementToDocumentAndPush();
                                }
                                else
                                {
                                    AppendHtmlElementToDocumentAndPush(attributes);
                                }
                                // XXX application cache should fire here
                                _mode = InsertionMode.BEFORE_HEAD;
                                goto breakStarttagloop;
                            default:
                                /*
								 * Create an HTMLElement node with the tag name
								 * html, in the HTML namespace. Append it to the
								 * Document object.
								 */
                                AppendHtmlElementToDocumentAndPush();
                                /* Switch to the main mode */
                                _mode = InsertionMode.BEFORE_HEAD;
                                /*
								 * reprocess the current token.
								 */
                                continue;
                        }
                    case InsertionMode.BEFORE_HEAD:
                        switch (group)
                        {
                            case DispatchGroup.HTML:
                                ErrStrayStartTag(name);
                                if (!_fragment)
                                {
                                    AddAttributesToHtml(attributes);
                                }
                                goto breakStarttagloop;
                            case DispatchGroup.HEAD:
                                /*
								 * A start tag whose tag name is "head"
								 * 
								 * Create an element for the token.
								 * 
								 * Set the head element pointer to this new element
								 * node.
								 * 
								 * Append the new element to the current node and
								 * push it onto the stack of open elements.
								 */
                                AppendToCurrentNodeAndPushHeadElement(attributes);
                                /*
								 * Change the insertion mode to "in head".
								 */
                                _mode = InsertionMode.IN_HEAD;
                                goto breakStarttagloop;
                            default:
                                /*
								 * Any other start tag token
								 * 
								 * Act as if a start tag token with the tag name
								 * "head" and no attributes had been seen,
								 */
                                AppendToCurrentNodeAndPushHeadElement(HtmlAttributes.EMPTY_ATTRIBUTES);
                                _mode = InsertionMode.IN_HEAD;
                                /*
								 * then reprocess the current token.
								 * 
								 * This will result in an empty head element being
								 * generated, with the current token being
								 * reprocessed in the "after head" insertion mode.
								 */
                                continue;
                        }
                    case InsertionMode.AFTER_HEAD:
                        switch (group)
                        {
                            case DispatchGroup.HTML:
                                ErrStrayStartTag(name);
                                if (!_fragment)
                                {
                                    AddAttributesToHtml(attributes);
                                }
                                goto breakStarttagloop;
                            case DispatchGroup.BODY:
                                if (attributes.Length == 0)
                                {
                                    // This has the right magic side effect
                                    // that
                                    // it
                                    // makes attributes in SAX Tree mutable.
                                    AppendToCurrentNodeAndPushBodyElement();
                                }
                                else
                                {
                                    AppendToCurrentNodeAndPushBodyElement(attributes);
                                }
                                _framesetOk = false;
                                _mode = InsertionMode.IN_BODY;
                                goto breakStarttagloop;
                            case DispatchGroup.FRAMESET:
                                AppendToCurrentNodeAndPushElement(
                                    elementName,
                                    attributes);
                                _mode = InsertionMode.IN_FRAMESET;
                                goto breakStarttagloop;
                            case DispatchGroup.BASE:
                                Err("\u201Cbase\u201D element outside \u201Chead\u201D.");
                                PushHeadPointerOntoStack();
                                AppendVoidElementToCurrentMayFoster(
                                    elementName,
                                    attributes);
                                selfClosing = false;
                                Pop(); // head
                                goto breakStarttagloop;
                            case DispatchGroup.LINK_OR_BASEFONT_OR_BGSOUND:
                                Err("\u201Clink\u201D element outside \u201Chead\u201D.");
                                PushHeadPointerOntoStack();
                                AppendVoidElementToCurrentMayFoster(
                                    elementName,
                                    attributes);
                                selfClosing = false;
                                Pop(); // head
                                goto breakStarttagloop;
                            case DispatchGroup.META:
                                Err("\u201Cmeta\u201D element outside \u201Chead\u201D.");
                                CheckMetaCharset(attributes);
                                PushHeadPointerOntoStack();
                                AppendVoidElementToCurrentMayFoster(
                                    elementName,
                                    attributes);
                                selfClosing = false;
                                Pop(); // head
                                goto breakStarttagloop;
                            case DispatchGroup.SCRIPT:
                                Err("\u201Cscript\u201D element between \u201Chead\u201D and \u201Cbody\u201D.");
                                PushHeadPointerOntoStack();
                                AppendToCurrentNodeAndPushElement(
                                    elementName,
                                    attributes);
                                _originalMode = _mode;
                                _mode = InsertionMode.TEXT;
                                _tokenizer.SetStateAndEndTagExpectation(
                                    Tokenizer.SCRIPT_DATA, elementName);
                                goto breakStarttagloop;
                            case DispatchGroup.STYLE:
                            case DispatchGroup.NOFRAMES:
                                Err("\u201C"
                                    + name
                                    + "\u201D element between \u201Chead\u201D and \u201Cbody\u201D.");
                                PushHeadPointerOntoStack();
                                AppendToCurrentNodeAndPushElement(
                                    elementName,
                                    attributes);
                                _originalMode = _mode;
                                _mode = InsertionMode.TEXT;
                                _tokenizer.SetStateAndEndTagExpectation(
                                    Tokenizer.RAWTEXT, elementName);
                                goto breakStarttagloop;
                            case DispatchGroup.TITLE:
                                Err("\u201Ctitle\u201D element outside \u201Chead\u201D.");
                                PushHeadPointerOntoStack();
                                AppendToCurrentNodeAndPushElement(
                                    elementName,
                                    attributes);
                                _originalMode = _mode;
                                _mode = InsertionMode.TEXT;
                                _tokenizer.SetStateAndEndTagExpectation(
                                    Tokenizer.RCDATA, elementName);
                                goto breakStarttagloop;
                            case DispatchGroup.HEAD:
                                ErrStrayStartTag(name);
                                goto breakStarttagloop;
                            default:
                                AppendToCurrentNodeAndPushBodyElement();
                                _mode = InsertionMode.FRAMESET_OK;
                                continue;
                        }
                    case InsertionMode.AFTER_AFTER_BODY:
                        switch (group)
                        {
                            case DispatchGroup.HTML:
                                ErrStrayStartTag(name);
                                if (!_fragment)
                                {
                                    AddAttributesToHtml(attributes);
                                }
                                goto breakStarttagloop;
                            default:
                                ErrStrayStartTag(name);
                                Fatal();
                                _mode = _framesetOk ? InsertionMode.FRAMESET_OK : InsertionMode.IN_BODY;
                                continue;
                        }
                    case InsertionMode.AFTER_AFTER_FRAMESET:
                        switch (group)
                        {
                            case DispatchGroup.HTML:
                                ErrStrayStartTag(name);
                                if (!_fragment)
                                {
                                    AddAttributesToHtml(attributes);
                                }
                                goto breakStarttagloop;
                            case DispatchGroup.NOFRAMES:
                                AppendToCurrentNodeAndPushElementMayFoster(
                                    elementName,
                                    attributes);
                                _originalMode = _mode;
                                _mode = InsertionMode.TEXT;
                                _tokenizer.SetStateAndEndTagExpectation(
                                    Tokenizer.SCRIPT_DATA, elementName);
                                goto breakStarttagloop;
                            default:
                                ErrStrayStartTag(name);
                                goto breakStarttagloop;
                        }
                    case InsertionMode.TEXT:
                        Debug.Assert(false);
                        goto breakStarttagloop; // Avoid infinite loop if the assertion fails (TODO: check)
                }

                continueStarttagloop:
                ;
            }

            breakStarttagloop:

            if (ErrorEvent != null && selfClosing)
            {
                Err(
                    "Self-closing syntax (\u201C/>\u201D) used on a non-void HTML element. Ignoring the slash and treating as a start tag.");
            }
        }

        private bool IsSpecialParentInForeign(StackNode<T> stackNode)
        {
            /*[NsUri]*/
            string ns = stackNode._ns;
            return ("http://www.w3.org/1999/xhtml" == ns)
                   || (stackNode.IsHtmlIntegrationPoint)
                   ||
                   (("http://www.w3.org/1998/Math/MathML" == ns) && (stackNode.Group == DispatchGroup.MI_MO_MN_MS_MTEXT));
        }

        public static string ExtractCharsetFromContent(string attributeValue)
        {
            // This is a bit ugly. Converting the string to char array in order to
            // make the portability layer smaller.
            var charsetState = CharsetState.CHARSET_INITIAL;
            int start = -1;
            int end = -1;
            char[] buffer = attributeValue.ToCharArray();

            /*charsetloop:*/
            for (int i = 0; i < buffer.Length; i++)
            {
                char c = buffer[i];
                switch (charsetState)
                {
                    case CharsetState.CHARSET_INITIAL:
                        switch (c)
                        {
                            case 'c':
                            case 'C':
                                charsetState = CharsetState.CHARSET_C;
                                continue;
                            default:
                                continue;
                        }
                    case CharsetState.CHARSET_C:
                        switch (c)
                        {
                            case 'h':
                            case 'H':
                                charsetState = CharsetState.CHARSET_H;
                                continue;
                            default:
                                charsetState = CharsetState.CHARSET_INITIAL;
                                continue;
                        }
                    case CharsetState.CHARSET_H:
                        switch (c)
                        {
                            case 'a':
                            case 'A':
                                charsetState = CharsetState.CHARSET_A;
                                continue;
                            default:
                                charsetState = CharsetState.CHARSET_INITIAL;
                                continue;
                        }
                    case CharsetState.CHARSET_A:
                        switch (c)
                        {
                            case 'r':
                            case 'R':
                                charsetState = CharsetState.CHARSET_R;
                                continue;
                            default:
                                charsetState = CharsetState.CHARSET_INITIAL;
                                continue;
                        }
                    case CharsetState.CHARSET_R:
                        switch (c)
                        {
                            case 's':
                            case 'S':
                                charsetState = CharsetState.CHARSET_S;
                                continue;
                            default:
                                charsetState = CharsetState.CHARSET_INITIAL;
                                continue;
                        }
                    case CharsetState.CHARSET_S:
                        switch (c)
                        {
                            case 'e':
                            case 'E':
                                charsetState = CharsetState.CHARSET_E;
                                continue;
                            default:
                                charsetState = CharsetState.CHARSET_INITIAL;
                                continue;
                        }
                    case CharsetState.CHARSET_E:
                        switch (c)
                        {
                            case 't':
                            case 'T':
                                charsetState = CharsetState.CHARSET_T;
                                continue;
                            default:
                                charsetState = CharsetState.CHARSET_INITIAL;
                                continue;
                        }
                    case CharsetState.CHARSET_T:
                        switch (c)
                        {
                            case '\t':
                            case '\n':
                            case '\u000C':
                            case '\r':
                            case ' ':
                                continue;
                            case '=':
                                charsetState = CharsetState.CHARSET_EQUALS;
                                continue;
                            default:
                                return null;
                        }
                    case CharsetState.CHARSET_EQUALS:
                        switch (c)
                        {
                            case '\t':
                            case '\n':
                            case '\u000C':
                            case '\r':
                            case ' ':
                                continue;
                            case '\'':
                                start = i + 1;
                                charsetState = CharsetState.CHARSET_SINGLE_QUOTED;
                                continue;
                            case '\"':
                                start = i + 1;
                                charsetState = CharsetState.CHARSET_DOUBLE_QUOTED;
                                continue;
                            default:
                                start = i;
                                charsetState = CharsetState.CHARSET_UNQUOTED;
                                continue;
                        }
                    case CharsetState.CHARSET_SINGLE_QUOTED:
                        switch (c)
                        {
                            case '\'':
                                end = i;
                                goto breakCharsetloop;
                            default:
                                continue;
                        }
                    case CharsetState.CHARSET_DOUBLE_QUOTED:
                        switch (c)
                        {
                            case '\"':
                                end = i;
                                goto breakCharsetloop;
                            default:
                                continue;
                        }
                    case CharsetState.CHARSET_UNQUOTED:
                        switch (c)
                        {
                            case '\t':
                            case '\n':
                            case '\u000C':
                            case '\r':
                            case ' ':
                            case ';':
                                end = i;
                                goto breakCharsetloop;
                            default:
                                continue;
                        }
                }
            }
            breakCharsetloop:

            string charset = null;
            if (start != -1)
            {
                if (end == -1)
                {
                    end = buffer.Length;
                }
                charset = new String(buffer, start, end - start);
            }
            return charset;
        }

        private void CheckMetaCharset(HtmlAttributes attributes)
        {
            string charset = attributes.GetValue(AttributeName.CHARSET);
            if (charset != null)
            {
                if (_tokenizer.InternalEncodingDeclaration(charset))
                {
                    RequestSuspension();
                    return;
                }
                return;
            }
            if (!Portability.LowerCaseLiteralEqualsIgnoreAsciiCaseString(
                "content-type",
                attributes.GetValue(AttributeName.HTTP_EQUIV)))
            {
                return;
            }
            string content = attributes.GetValue(AttributeName.CONTENT);
            if (content != null)
            {
                string extract = ExtractCharsetFromContent(content);
                // remember not to return early without releasing the string
                if (extract != null)
                {
                    if (_tokenizer.InternalEncodingDeclaration(extract))
                    {
                        RequestSuspension();
                    }
                }
            }
        }

        public void EndTag(ElementName elementName)
        {
            FlushCharacters();
            _needToDropLF = false;
            DispatchGroup group = elementName.Group;
            /*[Local]*/
            string name = elementName.name;
            /*endtagloop:*/
            for (;;)
            {
                int eltPos;
                if (IsInForeign)
                {
                    if (ErrorEvent != null && _stack[_currentPtr]._name != name)
                    {
                        Err("End tag \u201C"
                            + name
                            + "\u201D did not match the name of the current open element (\u201C"
                            + _stack[_currentPtr]._popName + "\u201D).");
                    }
                    eltPos = _currentPtr;
                    for (;;)
                    {
                        if (_stack[eltPos]._name == name)
                        {
                            while (_currentPtr >= eltPos)
                            {
                                Pop();
                            }
                            goto breakEndtagloop;
                        }
                        if (_stack[--eltPos]._ns == "http://www.w3.org/1999/xhtml")
                        {
                            break;
                        }
                    }
                }
                switch (_mode)
                {
                    case InsertionMode.IN_ROW:
                        switch (group)
                        {
                            case DispatchGroup.TR:
                                eltPos = FindLastOrRoot(DispatchGroup.TR);
                                if (eltPos == 0)
                                {
                                    Debug.Assert(_fragment);
                                    Err("No table row to close.");
                                    goto breakEndtagloop;
                                }
                                ClearStackBackTo(eltPos);
                                Pop();
                                _mode = InsertionMode.IN_TABLE_BODY;
                                goto breakEndtagloop;
                            case DispatchGroup.TABLE:
                                eltPos = FindLastOrRoot(DispatchGroup.TR);
                                if (eltPos == 0)
                                {
                                    Debug.Assert(_fragment);
                                    Err("No table row to close.");
                                    goto breakEndtagloop;
                                }
                                ClearStackBackTo(eltPos);
                                Pop();
                                _mode = InsertionMode.IN_TABLE_BODY;
                                continue;
                            case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
                                if (FindLastInTableScope(name) == TreeBuilderConstants.NOT_FOUND_ON_STACK)
                                {
                                    ErrStrayEndTag(name);
                                    goto breakEndtagloop;
                                }
                                eltPos = FindLastOrRoot(DispatchGroup.TR);
                                if (eltPos == 0)
                                {
                                    Debug.Assert(_fragment);
                                    Err("No table row to close.");
                                    goto breakEndtagloop;
                                }
                                ClearStackBackTo(eltPos);
                                Pop();
                                _mode = InsertionMode.IN_TABLE_BODY;
                                continue;
                            case DispatchGroup.BODY:
                            case DispatchGroup.CAPTION:
                            case DispatchGroup.COL:
                            case DispatchGroup.COLGROUP:
                            case DispatchGroup.HTML:
                            case DispatchGroup.TD_OR_TH:
                                ErrStrayEndTag(name);
                                goto breakEndtagloop;
                                // fall through to IN_TABLE (TODO: IN_TABLE_BODY?)
                        }

                        goto case InsertionMode.IN_TABLE_BODY;
                    case InsertionMode.IN_TABLE_BODY:
                        switch (group)
                        {
                            case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
                                eltPos = FindLastOrRoot(name);
                                if (eltPos == 0)
                                {
                                    ErrStrayEndTag(name);
                                    goto breakEndtagloop;
                                }
                                ClearStackBackTo(eltPos);
                                Pop();
                                _mode = InsertionMode.IN_TABLE;
                                goto breakEndtagloop;
                            case DispatchGroup.TABLE:
                                eltPos = FindLastInTableScopeOrRootTbodyTheadTfoot();
                                if (eltPos == 0)
                                {
                                    Debug.Assert(_fragment);
                                    ErrStrayEndTag(name);
                                    goto breakEndtagloop;
                                }
                                ClearStackBackTo(eltPos);
                                Pop();
                                _mode = InsertionMode.IN_TABLE;
                                continue;
                            case DispatchGroup.BODY:
                            case DispatchGroup.CAPTION:
                            case DispatchGroup.COL:
                            case DispatchGroup.COLGROUP:
                            case DispatchGroup.HTML:
                            case DispatchGroup.TD_OR_TH:
                            case DispatchGroup.TR:
                                ErrStrayEndTag(name);
                                goto breakEndtagloop;
                                // fall through to IN_TABLE
                        }
                        goto case InsertionMode.IN_TABLE;
                    case InsertionMode.IN_TABLE:
                        switch (group)
                        {
                            case DispatchGroup.TABLE:
                                eltPos = FindLast("table");
                                if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
                                {
                                    Debug.Assert(_fragment);
                                    ErrStrayEndTag(name);
                                    goto breakEndtagloop;
                                }
                                while (_currentPtr >= eltPos)
                                {
                                    Pop();
                                }
                                ResetTheInsertionMode();
                                goto breakEndtagloop;
                            case DispatchGroup.BODY:
                            case DispatchGroup.CAPTION:
                            case DispatchGroup.COL:
                            case DispatchGroup.COLGROUP:
                            case DispatchGroup.HTML:
                            case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
                            case DispatchGroup.TD_OR_TH:
                            case DispatchGroup.TR:
                                ErrStrayEndTag(name);
                                goto breakEndtagloop;
                            default:
                                ErrStrayEndTag(name);
                                // fall through to IN_BODY (TODO: IN_CAPTION?)
                                break;
                        }
                        goto case InsertionMode.IN_CAPTION;
                    case InsertionMode.IN_CAPTION:
                        switch (group)
                        {
                            case DispatchGroup.CAPTION:
                                eltPos = FindLastInTableScope("caption");
                                if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
                                {
                                    goto breakEndtagloop;
                                }
                                GenerateImpliedEndTags();
                                if (ErrorEvent != null && _currentPtr != eltPos)
                                {
                                    ErrUnclosedElements(eltPos, name);
                                }
                                while (_currentPtr >= eltPos)
                                {
                                    Pop();
                                }
                                ClearTheListOfActiveFormattingElementsUpToTheLastMarker();
                                _mode = InsertionMode.IN_TABLE;
                                goto breakEndtagloop;
                            case DispatchGroup.TABLE:
                                Err("\u201Ctable\u201D closed but \u201Ccaption\u201D was still open.");
                                eltPos = FindLastInTableScope("caption");
                                if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
                                {
                                    goto breakEndtagloop;
                                }
                                GenerateImpliedEndTags();
                                if (ErrorEvent != null && _currentPtr != eltPos)
                                {
                                    ErrUnclosedElements(eltPos, name);
                                }
                                while (_currentPtr >= eltPos)
                                {
                                    Pop();
                                }
                                ClearTheListOfActiveFormattingElementsUpToTheLastMarker();
                                _mode = InsertionMode.IN_TABLE;
                                continue;
                            case DispatchGroup.BODY:
                            case DispatchGroup.COL:
                            case DispatchGroup.COLGROUP:
                            case DispatchGroup.HTML:
                            case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
                            case DispatchGroup.TD_OR_TH:
                            case DispatchGroup.TR:
                                ErrStrayEndTag(name);
                                goto breakEndtagloop;
                                // fall through to IN_BODY (TODO: IN_CELL?)
                        }
                        goto case InsertionMode.IN_CELL;
                    case InsertionMode.IN_CELL:
                        switch (group)
                        {
                            case DispatchGroup.TD_OR_TH:
                                eltPos = FindLastInTableScope(name);
                                if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
                                {
                                    ErrStrayEndTag(name);
                                    goto breakEndtagloop;
                                }
                                GenerateImpliedEndTags();
                                if (ErrorEvent != null && !IsCurrent(name))
                                {
                                    ErrUnclosedElements(eltPos, name);
                                }
                                while (_currentPtr >= eltPos)
                                {
                                    Pop();
                                }
                                ClearTheListOfActiveFormattingElementsUpToTheLastMarker();
                                _mode = InsertionMode.IN_ROW;
                                goto breakEndtagloop;
                            case DispatchGroup.TABLE:
                            case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
                            case DispatchGroup.TR:
                                if (FindLastInTableScope(name) == TreeBuilderConstants.NOT_FOUND_ON_STACK)
                                {
                                    ErrStrayEndTag(name);
                                    goto breakEndtagloop;
                                }
                                CloseTheCell(FindLastInTableScopeTdTh());
                                continue;
                            case DispatchGroup.BODY:
                            case DispatchGroup.CAPTION:
                            case DispatchGroup.COL:
                            case DispatchGroup.COLGROUP:
                            case DispatchGroup.HTML:
                                ErrStrayEndTag(name);
                                goto breakEndtagloop;
                                // fall through to IN_BODY
                        }
                        goto case InsertionMode.IN_BODY;
                    case InsertionMode.FRAMESET_OK:
                    case InsertionMode.IN_BODY:
                        switch (group)
                        {
                            case DispatchGroup.BODY:
                                if (!IsSecondOnStackBody())
                                {
                                    Debug.Assert(_fragment);
                                    ErrStrayEndTag(name);
                                    goto breakEndtagloop;
                                }
                                Debug.Assert(_currentPtr >= 1);
                                if (ErrorEvent != null)
                                {
                                    /*uncloseloop1:*/
                                    for (int i = 2; i <= _currentPtr; i++)
                                    {
                                        switch (_stack[i].Group)
                                        {
                                            case DispatchGroup.DD_OR_DT:
                                            case DispatchGroup.LI:
                                            case DispatchGroup.OPTGROUP:
                                            case DispatchGroup.OPTION: // is this possible?
                                            case DispatchGroup.P:
                                            case DispatchGroup.RT_OR_RP:
                                            case DispatchGroup.TD_OR_TH:
                                            case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
                                                break;
                                            default:
                                                ErrEndWithUnclosedElements(
                                                    "End tag for \u201Cbody\u201D seen but there were unclosed elements.");
                                                goto breakUncloseloop1;
                                        }
                                    }
                                }
                                breakUncloseloop1:
                                _mode = InsertionMode.AFTER_BODY;
                                goto breakEndtagloop;
                            case DispatchGroup.HTML:
                                if (!IsSecondOnStackBody())
                                {
                                    Debug.Assert(_fragment);
                                    ErrStrayEndTag(name);
                                    goto breakEndtagloop;
                                }
                                if (ErrorEvent != null)
                                {
                                    /*uncloseloop2:*/
                                    for (int i = 0; i <= _currentPtr; i++)
                                    {
                                        switch (_stack[i].Group)
                                        {
                                            case DispatchGroup.DD_OR_DT:
                                            case DispatchGroup.LI:
                                            case DispatchGroup.P:
                                            case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
                                            case DispatchGroup.TD_OR_TH:
                                            case DispatchGroup.BODY:
                                            case DispatchGroup.HTML:
                                                break;
                                            default:
                                                ErrEndWithUnclosedElements(
                                                    "End tag for \u201Chtml\u201D seen but there were unclosed elements.");
                                                goto breakUncloseloop2;
                                        }
                                    }
                                }

                                breakUncloseloop2:
                                _mode = InsertionMode.AFTER_BODY;
                                continue;
                            case DispatchGroup.DIV_OR_BLOCKQUOTE_OR_CENTER_OR_MENU:
                            case DispatchGroup.UL_OR_OL_OR_DL:
                            case DispatchGroup.PRE_OR_LISTING:
                            case DispatchGroup.FIELDSET:
                            case DispatchGroup.BUTTON:
                            case
                                DispatchGroup
                                    .ADDRESS_OR_ARTICLE_OR_ASIDE_OR_DETAILS_OR_DIR_OR_FIGCAPTION_OR_FIGURE_OR_FOOTER_OR_HEADER_OR_HGROUP_OR_NAV_OR_SECTION_OR_SUMMARY
                                :
                                eltPos = FindLastInScope(name);
                                if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
                                {
                                    ErrStrayEndTag(name);
                                }
                                else
                                {
                                    GenerateImpliedEndTags();
                                    if (ErrorEvent != null && !IsCurrent(name))
                                    {
                                        ErrUnclosedElements(eltPos, name);
                                    }
                                    while (_currentPtr >= eltPos)
                                    {
                                        Pop();
                                    }
                                }
                                goto breakEndtagloop;
                            case DispatchGroup.FORM:
                                if (_formPointer == null)
                                {
                                    ErrStrayEndTag(name);
                                    goto breakEndtagloop;
                                }
                                _formPointer = null;
                                eltPos = FindLastInScope(name);
                                if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
                                {
                                    ErrStrayEndTag(name);
                                    goto breakEndtagloop;
                                }
                                GenerateImpliedEndTags();
                                if (ErrorEvent != null && !IsCurrent(name))
                                {
                                    ErrUnclosedElements(eltPos, name);
                                }
                                RemoveFromStack(eltPos);
                                goto breakEndtagloop;
                            case DispatchGroup.P:
                                eltPos = FindLastInButtonScope("p");
                                if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
                                {
                                    Err("No \u201Cp\u201D element in scope but a \u201Cp\u201D end tag seen.");
                                    // XXX Can the 'in foreign' case happen anymore?
                                    if (IsInForeign)
                                    {
                                        Err("HTML start tag \u201C"
                                            + name
                                            + "\u201D in a foreign namespace context.");
                                        while (_stack[_currentPtr]._ns != "http://www.w3.org/1999/xhtml")
                                        {
                                            Pop();
                                        }
                                    }
                                    AppendVoidElementToCurrentMayFoster(
                                        elementName,
                                        HtmlAttributes.EMPTY_ATTRIBUTES);
                                    goto breakEndtagloop;
                                }
                                GenerateImpliedEndTagsExceptFor("p");
                                Debug.Assert(eltPos != TreeBuilderConstants.NOT_FOUND_ON_STACK);
                                if (ErrorEvent != null && eltPos != _currentPtr)
                                {
                                    ErrUnclosedElements(eltPos, name);
                                }
                                while (_currentPtr >= eltPos)
                                {
                                    Pop();
                                }
                                goto breakEndtagloop;
                            case DispatchGroup.LI:
                                eltPos = FindLastInListScope(name);
                                if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
                                {
                                    Err("No \u201Cli\u201D element in list scope but a \u201Cli\u201D end tag seen.");
                                }
                                else
                                {
                                    GenerateImpliedEndTagsExceptFor(name);
                                    if (ErrorEvent != null && eltPos != _currentPtr)
                                    {
                                        ErrUnclosedElements(eltPos, name);
                                    }
                                    while (_currentPtr >= eltPos)
                                    {
                                        Pop();
                                    }
                                }
                                goto breakEndtagloop;
                            case DispatchGroup.DD_OR_DT:
                                eltPos = FindLastInScope(name);
                                if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
                                {
                                    Err("No \u201C"
                                        + name
                                        + "\u201D element in scope but a \u201C"
                                        + name + "\u201D end tag seen.");
                                }
                                else
                                {
                                    GenerateImpliedEndTagsExceptFor(name);
                                    if (ErrorEvent != null
                                        && eltPos != _currentPtr)
                                    {
                                        ErrUnclosedElements(eltPos, name);
                                    }
                                    while (_currentPtr >= eltPos)
                                    {
                                        Pop();
                                    }
                                }
                                goto breakEndtagloop;
                            case DispatchGroup.H1_OR_H2_OR_H3_OR_H4_OR_H5_OR_H6:
                                eltPos = FindLastInScopeHn();
                                if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
                                {
                                    ErrStrayEndTag(name);
                                }
                                else
                                {
                                    GenerateImpliedEndTags();
                                    if (ErrorEvent != null && !IsCurrent(name))
                                    {
                                        ErrUnclosedElements(eltPos, name);
                                    }
                                    while (_currentPtr >= eltPos)
                                    {
                                        Pop();
                                    }
                                }
                                goto breakEndtagloop;
                            case DispatchGroup.OBJECT:
                            case DispatchGroup.MARQUEE_OR_APPLET:
                                eltPos = FindLastInScope(name);
                                if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
                                {
                                    ErrStrayEndTag(name);
                                }
                                else
                                {
                                    GenerateImpliedEndTags();
                                    if (ErrorEvent != null && !IsCurrent(name))
                                    {
                                        ErrUnclosedElements(eltPos, name);
                                    }
                                    while (_currentPtr >= eltPos)
                                    {
                                        Pop();
                                    }
                                    ClearTheListOfActiveFormattingElementsUpToTheLastMarker();
                                }
                                goto breakEndtagloop;
                            case DispatchGroup.BR:
                                Err("End tag \u201Cbr\u201D.");
                                if (IsInForeign)
                                {
                                    Err("HTML start tag \u201C"
                                        + name
                                        + "\u201D in a foreign namespace context.");
                                    while (_stack[_currentPtr]._ns != "http://www.w3.org/1999/xhtml")
                                    {
                                        Pop();
                                    }
                                }
                                ReconstructTheActiveFormattingElements();
                                AppendVoidElementToCurrentMayFoster(
                                    elementName,
                                    HtmlAttributes.EMPTY_ATTRIBUTES);
                                goto breakEndtagloop;
                            case DispatchGroup.AREA_OR_WBR:
                            case DispatchGroup.PARAM_OR_SOURCE_OR_TRACK:
                            case DispatchGroup.EMBED_OR_IMG:
                            case DispatchGroup.IMAGE:
                            case DispatchGroup.INPUT:
                            case DispatchGroup.KEYGEN: // XXX??
                            case DispatchGroup.HR:
                            case DispatchGroup.ISINDEX:
                            case DispatchGroup.IFRAME:
                            case DispatchGroup.NOEMBED: // XXX???
                            case DispatchGroup.NOFRAMES: // XXX??
                            case DispatchGroup.SELECT:
                            case DispatchGroup.TABLE:
                            case DispatchGroup.TEXTAREA: // XXX??
                                ErrStrayEndTag(name);
                                goto breakEndtagloop;
                            case DispatchGroup.NOSCRIPT:
                                if (IsScriptingEnabled)
                                {
                                    ErrStrayEndTag(name);
                                    goto breakEndtagloop;
                                }
                                // fall through
                                goto case DispatchGroup.A;
                            case DispatchGroup.A:
                            case DispatchGroup.B_OR_BIG_OR_CODE_OR_EM_OR_I_OR_S_OR_SMALL_OR_STRIKE_OR_STRONG_OR_TT_OR_U:
                            case DispatchGroup.FONT:
                            case DispatchGroup.NOBR:
                                if (AdoptionAgencyEndTag(name))
                                {
                                    goto breakEndtagloop;
                                }
                                // else handle like any other tag
                                goto default;
                            default:
                                if (IsCurrent(name))
                                {
                                    Pop();
                                    goto breakEndtagloop;
                                }

                                eltPos = _currentPtr;
                                for (;;)
                                {
                                    StackNode<T> node = _stack[eltPos];
                                    if (node._name == name)
                                    {
                                        GenerateImpliedEndTags();
                                        if (ErrorEvent != null
                                            && !IsCurrent(name))
                                        {
                                            ErrUnclosedElements(eltPos, name);
                                        }
                                        while (_currentPtr >= eltPos)
                                        {
                                            Pop();
                                        }
                                        goto breakEndtagloop;
                                    }
                                    if (node.IsSpecial)
                                    {
                                        ErrStrayEndTag(name);
                                        goto breakEndtagloop;
                                    }
                                    eltPos--;
                                }
                        }
                    case InsertionMode.IN_COLUMN_GROUP:
                        switch (group)
                        {
                            case DispatchGroup.COLGROUP:
                                if (_currentPtr == 0)
                                {
                                    Debug.Assert(_fragment);
                                    Err("Garbage in \u201Ccolgroup\u201D fragment.");
                                    goto breakEndtagloop;
                                }
                                Pop();
                                _mode = InsertionMode.IN_TABLE;
                                goto breakEndtagloop;
                            case DispatchGroup.COL:
                                ErrStrayEndTag(name);
                                goto breakEndtagloop;
                            default:
                                if (_currentPtr == 0)
                                {
                                    Debug.Assert(_fragment);
                                    Err("Garbage in \u201Ccolgroup\u201D fragment.");
                                    goto breakEndtagloop;
                                }
                                Pop();
                                _mode = InsertionMode.IN_TABLE;
                                continue;
                        }
                    case InsertionMode.IN_SELECT_IN_TABLE:
                        switch (group)
                        {
                            case DispatchGroup.CAPTION:
                            case DispatchGroup.TABLE:
                            case DispatchGroup.TBODY_OR_THEAD_OR_TFOOT:
                            case DispatchGroup.TR:
                            case DispatchGroup.TD_OR_TH:
                                Err("\u201C"
                                    + name
                                    + "\u201D end tag with \u201Cselect\u201D open.");
                                if (FindLastInTableScope(name) != TreeBuilderConstants.NOT_FOUND_ON_STACK)
                                {
                                    eltPos = FindLastInTableScope("select");
                                    if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
                                    {
                                        Debug.Assert(_fragment);
                                        goto breakEndtagloop; // http://www.w3.org/Bugs/Public/show_bug.cgi?id=8375
                                    }
                                    while (_currentPtr >= eltPos)
                                    {
                                        Pop();
                                    }
                                    ResetTheInsertionMode();
                                    continue;
                                }
                                goto breakEndtagloop;
                                // fall through to IN_SELECT
                        }
                        goto case InsertionMode.IN_SELECT;
                    case InsertionMode.IN_SELECT:
                        switch (group)
                        {
                            case DispatchGroup.OPTION:
                                if (IsCurrent("option"))
                                {
                                    Pop();
                                    goto breakEndtagloop;
                                }
                                ErrStrayEndTag(name);
                                goto breakEndtagloop;
                            case DispatchGroup.OPTGROUP:
                                if (IsCurrent("option")
                                    && "optgroup" == _stack[_currentPtr - 1]._name)
                                {
                                    Pop();
                                }
                                if (IsCurrent("optgroup"))
                                {
                                    Pop();
                                }
                                else
                                {
                                    ErrStrayEndTag(name);
                                }
                                goto breakEndtagloop;
                            case DispatchGroup.SELECT:
                                eltPos = FindLastInTableScope("select");
                                if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
                                {
                                    Debug.Assert(_fragment);
                                    ErrStrayEndTag(name);
                                    goto breakEndtagloop;
                                }
                                while (_currentPtr >= eltPos)
                                {
                                    Pop();
                                }
                                ResetTheInsertionMode();
                                goto breakEndtagloop;
                            default:
                                ErrStrayEndTag(name);
                                goto breakEndtagloop;
                        }
                    case InsertionMode.AFTER_BODY:
                        switch (group)
                        {
                            case DispatchGroup.HTML:
                                if (_fragment)
                                {
                                    ErrStrayEndTag(name);
                                    goto breakEndtagloop;
                                }
                                _mode = InsertionMode.AFTER_AFTER_BODY;
                                goto breakEndtagloop;
                            default:
                                Err("Saw an end tag after \u201Cbody\u201D had been closed.");
                                _mode = _framesetOk ? InsertionMode.FRAMESET_OK : InsertionMode.IN_BODY;
                                continue;
                        }
                    case InsertionMode.IN_FRAMESET:
                        switch (group)
                        {
                            case DispatchGroup.FRAMESET:
                                if (_currentPtr == 0)
                                {
                                    Debug.Assert(_fragment);
                                    ErrStrayEndTag(name);
                                    goto breakEndtagloop;
                                }
                                Pop();
                                if ((!_fragment) && !IsCurrent("frameset"))
                                {
                                    _mode = InsertionMode.AFTER_FRAMESET;
                                }
                                goto breakEndtagloop;
                            default:
                                ErrStrayEndTag(name);
                                goto breakEndtagloop;
                        }
                    case InsertionMode.AFTER_FRAMESET:
                        switch (group)
                        {
                            case DispatchGroup.HTML:
                                _mode = InsertionMode.AFTER_AFTER_FRAMESET;
                                goto breakEndtagloop;
                            default:
                                ErrStrayEndTag(name);
                                goto breakEndtagloop;
                        }
                    case InsertionMode.INITIAL:
                        /*
						 * Parse error.
						 */
                        // [NOCPP[
                        switch (DoctypeExpectation)
                        {
                            case DoctypeExpectation.Auto:
                                Err(
                                    "End tag seen without seeing a doctype first. Expected e.g. \u201C<!DOCTYPE html>\u201D.");
                                break;
                            case DoctypeExpectation.Html:
                                Err("End tag seen without seeing a doctype first. Expected \u201C<!DOCTYPE html>\u201D.");
                                break;
                            case DoctypeExpectation.Html401Strict:
                                Err(
                                    "End tag seen without seeing a doctype first. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01//EN\" \"http://www.w3.org/TR/html4/strict.dtd\">\u201D.");
                                break;
                            case DoctypeExpectation.Html401Transitional:
                                Err(
                                    "End tag seen without seeing a doctype first. Expected \u201C<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\" \"http://www.w3.org/TR/html4/loose.dtd\">\u201D.");
                                break;
                            case DoctypeExpectation.NoDoctypeErrors:
                                break;
                        }
                        // ]NOCPP]
                        /*
						 * 
						 * Set the document to quirks mode.
						 */
                        DocumentModeInternal(DocumentMode.QuirksMode, null, null,
                                             false);
                        /*
						 * Then, switch to the root element mode of the tree
						 * construction stage
						 */
                        _mode = InsertionMode.BEFORE_HTML;
                        /*
						 * and reprocess the current token.
						 */
                        continue;
                    case InsertionMode.BEFORE_HTML:
                        switch (group)
                        {
                            case DispatchGroup.HEAD:
                            case DispatchGroup.BR:
                            case DispatchGroup.HTML:
                            case DispatchGroup.BODY:
                                /*
								 * Create an HTMLElement node with the tag name
								 * html, in the HTML namespace. Append it to the
								 * Document object.
								 */
                                AppendHtmlElementToDocumentAndPush();
                                /* Switch to the main mode */
                                _mode = InsertionMode.BEFORE_HEAD;
                                /*
								 * reprocess the current token.
								 */
                                continue;
                            default:
                                ErrStrayEndTag(name);
                                goto breakEndtagloop;
                        }
                    case InsertionMode.BEFORE_HEAD:
                        switch (group)
                        {
                            case DispatchGroup.HEAD:
                            case DispatchGroup.BR:
                            case DispatchGroup.HTML:
                            case DispatchGroup.BODY:
                                AppendToCurrentNodeAndPushHeadElement(HtmlAttributes.EMPTY_ATTRIBUTES);
                                _mode = InsertionMode.IN_HEAD;
                                continue;
                            default:
                                ErrStrayEndTag(name);
                                goto breakEndtagloop;
                        }
                    case InsertionMode.IN_HEAD:
                        switch (group)
                        {
                            case DispatchGroup.HEAD:
                                Pop();
                                _mode = InsertionMode.AFTER_HEAD;
                                goto breakEndtagloop;
                            case DispatchGroup.BR:
                            case DispatchGroup.HTML:
                            case DispatchGroup.BODY:
                                Pop();
                                _mode = InsertionMode.AFTER_HEAD;
                                continue;
                            default:
                                ErrStrayEndTag(name);
                                goto breakEndtagloop;
                        }
                    case InsertionMode.IN_HEAD_NOSCRIPT:
                        switch (group)
                        {
                            case DispatchGroup.NOSCRIPT:
                                Pop();
                                _mode = InsertionMode.IN_HEAD;
                                goto breakEndtagloop;
                            case DispatchGroup.BR:
                                ErrStrayEndTag(name);
                                Pop();
                                _mode = InsertionMode.IN_HEAD;
                                continue;
                            default:
                                ErrStrayEndTag(name);
                                goto breakEndtagloop;
                        }
                    case InsertionMode.AFTER_HEAD:
                        switch (group)
                        {
                            case DispatchGroup.HTML:
                            case DispatchGroup.BODY:
                            case DispatchGroup.BR:
                                AppendToCurrentNodeAndPushBodyElement();
                                _mode = InsertionMode.FRAMESET_OK;
                                continue;
                            default:
                                ErrStrayEndTag(name);
                                goto breakEndtagloop;
                        }
                    case InsertionMode.AFTER_AFTER_BODY:
                        ErrStrayEndTag(name);
                        _mode = _framesetOk ? InsertionMode.FRAMESET_OK : InsertionMode.IN_BODY;
                        continue;
                    case InsertionMode.AFTER_AFTER_FRAMESET:
                        ErrStrayEndTag(name);
                        _mode = InsertionMode.IN_FRAMESET;
                        continue;
                    case InsertionMode.TEXT:
                        // XXX need to manage insertion point here
                        Pop();
                        if (_originalMode == InsertionMode.AFTER_HEAD)
                        {
                            SilentPop();
                        }
                        _mode = _originalMode;
                        goto breakEndtagloop;
                }
            } // endtagloop

            breakEndtagloop:
            ;
        }

        private int FindLastInTableScopeOrRootTbodyTheadTfoot()
        {
            for (int i = _currentPtr; i > 0; i--)
            {
                if (_stack[i].Group == DispatchGroup.TBODY_OR_THEAD_OR_TFOOT)
                {
                    return i;
                }
            }
            return 0;
        }

        private int FindLast([Local] string name)
        {
            for (int i = _currentPtr; i > 0; i--)
            {
                if (_stack[i]._name == name)
                {
                    return i;
                }
            }
            return TreeBuilderConstants.NOT_FOUND_ON_STACK;
        }

        private int FindLastInTableScope([Local] string name)
        {
            for (int i = _currentPtr; i > 0; i--)
            {
                if (_stack[i]._name == name)
                {
                    return i;
                }
                if (_stack[i]._name == "table")
                {
                    return TreeBuilderConstants.NOT_FOUND_ON_STACK;
                }
            }
            return TreeBuilderConstants.NOT_FOUND_ON_STACK;
        }

        private int FindLastInButtonScope([Local] string name)
        {
            for (int i = _currentPtr; i > 0; i--)
            {
                if (_stack[i]._name == name)
                {
                    return i;
                }
                if (_stack[i].IsScoping || _stack[i]._name == "button")
                {
                    return TreeBuilderConstants.NOT_FOUND_ON_STACK;
                }
            }
            return TreeBuilderConstants.NOT_FOUND_ON_STACK;
        }

        private int FindLastInScope([Local] string name)
        {
            for (int i = _currentPtr; i > 0; i--)
            {
                if (_stack[i]._name == name)
                {
                    return i;
                }
                if (_stack[i].IsScoping)
                {
                    return TreeBuilderConstants.NOT_FOUND_ON_STACK;
                }
            }
            return TreeBuilderConstants.NOT_FOUND_ON_STACK;
        }

        private int FindLastInListScope([Local] string name)
        {
            for (int i = _currentPtr; i > 0; i--)
            {
                if (_stack[i]._name == name)
                {
                    return i;
                }
                if (_stack[i].IsScoping || _stack[i]._name == "ul" || _stack[i]._name == "ol")
                {
                    return TreeBuilderConstants.NOT_FOUND_ON_STACK;
                }
            }
            return TreeBuilderConstants.NOT_FOUND_ON_STACK;
        }

        private int FindLastInScopeHn()
        {
            for (int i = _currentPtr; i > 0; i--)
            {
                if (_stack[i].Group == DispatchGroup.H1_OR_H2_OR_H3_OR_H4_OR_H5_OR_H6)
                {
                    return i;
                }
                if (_stack[i].IsScoping)
                {
                    return TreeBuilderConstants.NOT_FOUND_ON_STACK;
                }
            }
            return TreeBuilderConstants.NOT_FOUND_ON_STACK;
        }

        private void GenerateImpliedEndTagsExceptFor([Local] string name)
        {
            for (;;)
            {
                StackNode<T> node = _stack[_currentPtr];
                switch (node.Group)
                {
                    case DispatchGroup.P:
                    case DispatchGroup.LI:
                    case DispatchGroup.DD_OR_DT:
                    case DispatchGroup.OPTION:
                    case DispatchGroup.OPTGROUP:
                    case DispatchGroup.RT_OR_RP:
                        if (node._name == name)
                        {
                            return;
                        }
                        Pop();
                        continue;
                    default:
                        return;
                }
            }
        }

        private void GenerateImpliedEndTags()
        {
            for (;;)
            {
                switch (_stack[_currentPtr].Group)
                {
                    case DispatchGroup.P:
                    case DispatchGroup.LI:
                    case DispatchGroup.DD_OR_DT:
                    case DispatchGroup.OPTION:
                    case DispatchGroup.OPTGROUP:
                    case DispatchGroup.RT_OR_RP:
                        Pop();
                        continue;
                    default:
                        return;
                }
            }
        }

        private bool IsSecondOnStackBody()
        {
            return _currentPtr >= 1 && _stack[1].Group == DispatchGroup.BODY;
        }

        private void DocumentModeInternal(DocumentMode m, string publicIdentifier,
                                          string systemIdentifier, bool html4SpecificAdditionalErrorChecks)
        {
            _quirks = (m == DocumentMode.QuirksMode);
            if (DocumentModeDetected != null)
            {
                DocumentModeDetected(this, new DocumentModeEventArgs(
                                               m
                                               // [NOCPP[
                                               , publicIdentifier, systemIdentifier,
                                               html4SpecificAdditionalErrorChecks
                                               // ]NOCPP]
                                               ));
            }
            // [NOCPP[
            ReceiveDocumentMode(m, publicIdentifier, systemIdentifier,
                                html4SpecificAdditionalErrorChecks);
            // ]NOCPP]
        }

        private bool IsAlmostStandards(string publicIdentifier, string systemIdentifier)
        {
            if (Portability.LowerCaseLiteralEqualsIgnoreAsciiCaseString(
                "-//w3c//dtd xhtml 1.0 transitional//en", publicIdentifier))
            {
                return true;
            }
            if (Portability.LowerCaseLiteralEqualsIgnoreAsciiCaseString(
                "-//w3c//dtd xhtml 1.0 frameset//en", publicIdentifier))
            {
                return true;
            }
            if (systemIdentifier != null)
            {
                if (Portability.LowerCaseLiteralEqualsIgnoreAsciiCaseString(
                    "-//w3c//dtd html 4.01 transitional//en", publicIdentifier))
                {
                    return true;
                }
                if (Portability.LowerCaseLiteralEqualsIgnoreAsciiCaseString(
                    "-//w3c//dtd html 4.01 frameset//en", publicIdentifier))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsQuirky([Local] string name, string publicIdentifier, string systemIdentifier, bool forceQuirks)
        {
            if (forceQuirks)
            {
                return true;
            }
            if (name != TreeBuilderConstants.HTML_LOCAL)
            {
                return true;
            }
            if (publicIdentifier != null)
            {
                if (TreeBuilderConstants.QUIRKY_PUBLIC_IDS.Any(t => Portability.LowerCaseLiteralIsPrefixOfIgnoreAsciiCaseString(
                    t, publicIdentifier)))
                {
                    return true;
                }
                if (Portability.LowerCaseLiteralEqualsIgnoreAsciiCaseString(
                    "-//w3o//dtd w3 html strict 3.0//en//", publicIdentifier)
                    || Portability.LowerCaseLiteralEqualsIgnoreAsciiCaseString(
                        "-/w3c/dtd html 4.0 transitional/en",
                        publicIdentifier)
                    || Portability.LowerCaseLiteralEqualsIgnoreAsciiCaseString(
                        "html", publicIdentifier))
                {
                    return true;
                }
            }
            if (systemIdentifier == null)
            {
                if (Portability.LowerCaseLiteralEqualsIgnoreAsciiCaseString(
                    "-//w3c//dtd html 4.01 transitional//en", publicIdentifier))
                {
                    return true;
                }
                if (Portability.LowerCaseLiteralEqualsIgnoreAsciiCaseString(
                    "-//w3c//dtd html 4.01 frameset//en", publicIdentifier))
                {
                    return true;
                }
            }
            else if (Portability.LowerCaseLiteralEqualsIgnoreAsciiCaseString(
                "http://www.ibm.com/data/dtd/v11/ibmxhtml1-transitional.dtd",
                systemIdentifier))
            {
                return true;
            }
            return false;
        }

        private void CloseTheCell(int eltPos)
        {
            GenerateImpliedEndTags();
            if (ErrorEvent != null && eltPos != _currentPtr)
            {
                ErrUnclosedElementsCell(eltPos);
            }
            while (_currentPtr >= eltPos)
            {
                Pop();
            }
            ClearTheListOfActiveFormattingElementsUpToTheLastMarker();
            _mode = InsertionMode.IN_ROW;
        }

        private int FindLastInTableScopeTdTh()
        {
            for (int i = _currentPtr; i > 0; i--)
            {
                /*[Local]*/
                string name = _stack[i]._name;
                if ("td" == name || "th" == name)
                {
                    return i;
                }
                if (name == "table")
                {
                    return TreeBuilderConstants.NOT_FOUND_ON_STACK;
                }
            }
            return TreeBuilderConstants.NOT_FOUND_ON_STACK;
        }

        private void ClearStackBackTo(int eltPos)
        {
            while (_currentPtr > eltPos)
            {
                // > not >= intentional
                Pop();
            }
        }

        private void ResetTheInsertionMode()
        {
            /*[Local]*/
            /*[NsUri]*/
            for (int i = _currentPtr; i >= 0; i--)
            {
                StackNode<T> node = _stack[i];
                string name = node._name;
                string ns = node._ns;
                if (i == 0)
                {
                    if (
                        !(_contextNamespace == "http://www.w3.org/1999/xhtml" &&
                          (_contextName == "td" || _contextName == "th")))
                    {
                        name = _contextName;
                        ns = _contextNamespace;
                    }
                    else
                    {
                        _mode = _framesetOk ? InsertionMode.FRAMESET_OK : InsertionMode.IN_BODY;
                            // XXX from Hixie's email
                        return;
                    }
                }
                if ("select" == name)
                {
                    _mode = InsertionMode.IN_SELECT;
                    return;
                }
                if ("td" == name || "th" == name)
                {
                    _mode = InsertionMode.IN_CELL;
                    return;
                }
                if ("tr" == name)
                {
                    _mode = InsertionMode.IN_ROW;
                    return;
                }
                if ("tbody" == name || "thead" == name || "tfoot" == name)
                {
                    _mode = InsertionMode.IN_TABLE_BODY;
                    return;
                }
                if ("caption" == name)
                {
                    _mode = InsertionMode.IN_CAPTION;
                    return;
                }
                if ("colgroup" == name)
                {
                    _mode = InsertionMode.IN_COLUMN_GROUP;
                    return;
                }
                if ("table" == name)
                {
                    _mode = InsertionMode.IN_TABLE;
                    return;
                }
                if ("http://www.w3.org/1999/xhtml" != ns)
                {
                    _mode = _framesetOk ? InsertionMode.FRAMESET_OK : InsertionMode.IN_BODY;
                    return;
                }
                if ("head" == name)
                {
                    _mode = _framesetOk ? InsertionMode.FRAMESET_OK : InsertionMode.IN_BODY; // really
                    return;
                }
                if ("body" == name)
                {
                    _mode = _framesetOk ? InsertionMode.FRAMESET_OK : InsertionMode.IN_BODY;
                    return;
                }
                if ("frameset" == name)
                {
                    _mode = InsertionMode.IN_FRAMESET;
                    return;
                }
                if ("html" == name)
                {
                    _mode = _headPointer == null ? InsertionMode.BEFORE_HEAD : InsertionMode.AFTER_HEAD;
                    return;
                }
                if (i == 0)
                {
                    _mode = _framesetOk ? InsertionMode.FRAMESET_OK : InsertionMode.IN_BODY;
                    return;
                }
            }
        }

        private void ImplicitlyCloseP()
        {
            int eltPos = FindLastInButtonScope("p");
            if (eltPos == TreeBuilderConstants.NOT_FOUND_ON_STACK)
            {
                return;
            }
            GenerateImpliedEndTagsExceptFor("p");
            if (ErrorEvent != null && eltPos != _currentPtr)
            {
                ErrUnclosedElementsImplied(eltPos, "p");
            }
            while (_currentPtr >= eltPos)
            {
                Pop();
            }
        }

        private bool ClearLastStackSlot()
        {
            _stack[_currentPtr] = null;
            return true;
        }

        private bool ClearLastListSlot()
        {
            _listOfActiveFormattingElements[_listPtr] = null;
            return true;
        }

        private void Push(StackNode<T> node)
        {
            _currentPtr++;
            if (_currentPtr == _stack.Length)
            {
                var newStack = new StackNode<T>[_stack.Length + 64];
                Array.Copy(_stack, newStack, _stack.Length);
                _stack = newStack;
            }
            _stack[_currentPtr] = node;
            ElementPushed(node._ns, node._popName, node._node);
        }

        private void SilentPush(StackNode<T> node)
        {
            _currentPtr++;
            if (_currentPtr == _stack.Length)
            {
                var newStack = new StackNode<T>[_stack.Length + 64];
                Array.Copy(_stack, newStack, _stack.Length);
                _stack = newStack;
            }
            _stack[_currentPtr] = node;
        }

        private void Append(StackNode<T> node)
        {
            _listPtr++;
            if (_listPtr == _listOfActiveFormattingElements.Length)
            {
                var newList = new StackNode<T>[_listOfActiveFormattingElements.Length + 64];
                Array.Copy(_listOfActiveFormattingElements, newList, _listOfActiveFormattingElements.Length);
                _listOfActiveFormattingElements = newList;
            }
            _listOfActiveFormattingElements[_listPtr] = node;
        }

        private void InsertMarker()
        {
            Append(null);
        }

        private void ClearTheListOfActiveFormattingElementsUpToTheLastMarker()
        {
            while (_listPtr > -1)
            {
                if (_listOfActiveFormattingElements[_listPtr] == null)
                {
                    --_listPtr;
                    return;
                }
                --_listPtr;
            }
        }

        private bool IsCurrent([Local] string name)
        {
            return name == _stack[_currentPtr]._name;
        }

        private void RemoveFromStack(int pos)
        {
            if (_currentPtr == pos)
            {
                Pop();
            }
            else
            {
                Fatal();
                Array.Copy(_stack, pos + 1, _stack, pos, _currentPtr - pos);
                Debug.Assert(ClearLastStackSlot());
                _currentPtr--;
            }
        }

        private void RemoveFromStack(StackNode<T> node)
        {
            if (_stack[_currentPtr] == node)
            {
                Pop();
            }
            else
            {
                int pos = _currentPtr - 1;
                while (pos >= 0 && _stack[pos] != node)
                {
                    pos--;
                }
                if (pos == -1)
                {
                    // dead code?
                    return;
                }
                Fatal();
                Array.Copy(_stack, pos + 1, _stack, pos, _currentPtr - pos);
                _currentPtr--;
            }
        }

        private void RemoveFromListOfActiveFormattingElements(int pos)
        {
            Debug.Assert(_listOfActiveFormattingElements[pos] != null);
            if (pos == _listPtr)
            {
                Debug.Assert(ClearLastListSlot());
                _listPtr--;
                return;
            }
            Debug.Assert(pos < _listPtr);
            Array.Copy(_listOfActiveFormattingElements, pos + 1, _listOfActiveFormattingElements, pos, _listPtr - pos);
            Debug.Assert(ClearLastListSlot());
            _listPtr--;
        }

        private bool AdoptionAgencyEndTag([Local] string name)
        {
            // If you crash around here, perhaps some stack node variable claimed to
            // be a weak ref isn't.
            for (int i = 0; i < 8; ++i)
            {
                int formattingEltListPos = _listPtr;
                while (formattingEltListPos > -1)
                {
                    StackNode<T> listNode = _listOfActiveFormattingElements[formattingEltListPos]; // weak
                    // ref
                    if (listNode == null)
                    {
                        formattingEltListPos = -1;
                        break;
                    }
                    if (listNode._name == name)
                    {
                        break;
                    }
                    formattingEltListPos--;
                }
                if (formattingEltListPos == -1)
                {
                    return false;
                }
                StackNode<T> formattingElt = _listOfActiveFormattingElements[formattingEltListPos]; // this
                // *looks*
                // like
                // a
                // weak
                // ref
                // to
                // the
                // list
                // of
                // formatting
                // elements
                int formattingEltStackPos = _currentPtr;
                bool inScope = true;
                while (formattingEltStackPos > -1)
                {
                    StackNode<T> node = _stack[formattingEltStackPos]; // weak ref
                    if (node == formattingElt)
                    {
                        break;
                    }
                    if (node.IsScoping)
                    {
                        inScope = false;
                    }
                    formattingEltStackPos--;
                }
                if (formattingEltStackPos == -1)
                {
                    Err("No element \u201C" + name + "\u201D to close.");
                    RemoveFromListOfActiveFormattingElements(formattingEltListPos);
                    return true;
                }
                if (!inScope)
                {
                    Err("No element \u201C" + name + "\u201D to close.");
                    return true;
                }
                // stackPos now points to the formatting element and it is in scope
                if (ErrorEvent != null && formattingEltStackPos != _currentPtr)
                {
                    Err("End tag \u201C" + name + "\u201D violates nesting rules.");
                }
                int furthestBlockPos = formattingEltStackPos + 1;
                while (furthestBlockPos <= _currentPtr)
                {
                    StackNode<T> node = _stack[furthestBlockPos]; // weak ref
                    if (node.IsSpecial)
                    {
                        break;
                    }
                    furthestBlockPos++;
                }
                if (furthestBlockPos > _currentPtr)
                {
                    // no furthest block
                    while (_currentPtr >= formattingEltStackPos)
                    {
                        Pop();
                    }
                    RemoveFromListOfActiveFormattingElements(formattingEltListPos);
                    return true;
                }
                StackNode<T> commonAncestor = _stack[formattingEltStackPos - 1]; // weak
                // ref
                StackNode<T> furthestBlock = _stack[furthestBlockPos]; // weak ref
                // detachFromParent(furthestBlock.node); XXX AAA CHANGE
                int bookmark = formattingEltListPos;
                int nodePos = furthestBlockPos;
                StackNode<T> lastNode = furthestBlock; // weak ref
                for (int j = 0; j < 3; ++j)
                {
                    nodePos--;
                    StackNode<T> node = _stack[nodePos]; // weak ref
                    int nodeListPos = FindInListOfActiveFormattingElements(node);
                    if (nodeListPos == -1)
                    {
                        Debug.Assert(formattingEltStackPos < nodePos);
                        Debug.Assert(bookmark < nodePos);
                        Debug.Assert(furthestBlockPos > nodePos);
                        RemoveFromStack(nodePos); // node is now a bad pointer in
                        // C++
                        furthestBlockPos--;
                        continue;
                    }
                    // now node is both on stack and in the list
                    if (nodePos == formattingEltStackPos)
                    {
                        break;
                    }
                    if (nodePos == furthestBlockPos)
                    {
                        bookmark = nodeListPos + 1;
                    }
                    // if (hasChildren(node.node)) { XXX AAA CHANGE
                    Debug.Assert(node == _listOfActiveFormattingElements[nodeListPos]);
                    Debug.Assert(node == _stack[nodePos]);
                    T clone = CreateElement("http://www.w3.org/1999/xhtml",
                                            node._name, node._attributes.CloneAttributes());
                    var newNode = new StackNode<T>(node.Flags, node._ns,
                                                   node._name, clone, node._popName, node._attributes
                                                   // [NOCPP[
                                                   , node.Locator
                        // ]NOCPP]       
                        ); // creation
                    // ownership
                    // goes
                    // to
                    // stack
                    node.DropAttributes(); // adopt ownership to newNode
                    _stack[nodePos] = newNode;
                    _listOfActiveFormattingElements[nodeListPos] = newNode;
                    node = newNode;
                    // } XXX AAA CHANGE
                    DetachFromParent(lastNode._node);
                    AppendElement(lastNode._node, node._node);
                    lastNode = node;
                }
                if (commonAncestor.IsFosterParenting)
                {
                    Fatal();
                    DetachFromParent(lastNode._node);
                    InsertIntoFosterParent(lastNode._node);
                }
                else
                {
                    DetachFromParent(lastNode._node);
                    AppendElement(lastNode._node, commonAncestor._node);
                }
                T clone2 = CreateElement("http://www.w3.org/1999/xhtml",
                                         formattingElt._name,
                                         formattingElt._attributes.CloneAttributes());
                var formattingClone = new StackNode<T>(
                    formattingElt.Flags, formattingElt._ns,
                    formattingElt._name, clone2, formattingElt._popName,
                    formattingElt._attributes
                    // [NOCPP[
                    , ErrorEvent == null ? null : new TaintableLocator(_tokenizer)
                    // ]NOCPP]
                    ); // Ownership
                // transfers
                // to
                // stack
                // below
                formattingElt.DropAttributes(); // transfer ownership to formattingClone
                AppendChildrenToNewParent(furthestBlock._node, clone2);
                AppendElement(clone2, furthestBlock._node);
                RemoveFromListOfActiveFormattingElements(formattingEltListPos);
                InsertIntoListOfActiveFormattingElements(formattingClone, bookmark);
                Debug.Assert(formattingEltStackPos < furthestBlockPos);
                RemoveFromStack(formattingEltStackPos);
                // furthestBlockPos is now off by one and points to the slot after it
                InsertIntoStack(formattingClone, furthestBlockPos);
            }
            return true;
        }

        private void InsertIntoStack(StackNode<T> node, int position)
        {
            Debug.Assert(_currentPtr + 1 < _stack.Length);
            Debug.Assert(position <= _currentPtr + 1);
            if (position == _currentPtr + 1)
            {
                Push(node);
            }
            else
            {
                Array.Copy(_stack, position, _stack, position + 1,
                           (_currentPtr - position) + 1);
                _currentPtr++;
                _stack[position] = node;
            }
        }

        private void InsertIntoListOfActiveFormattingElements(
            StackNode<T> formattingClone, int bookmark)
        {
            Debug.Assert(_listPtr + 1 < _listOfActiveFormattingElements.Length);
            if (bookmark <= _listPtr)
            {
                Array.Copy(_listOfActiveFormattingElements, bookmark,
                           _listOfActiveFormattingElements, bookmark + 1,
                           (_listPtr - bookmark) + 1);
            }
            _listPtr++;
            _listOfActiveFormattingElements[bookmark] = formattingClone;
        }

        private int FindInListOfActiveFormattingElements(StackNode<T> node)
        {
            for (int i = _listPtr; i >= 0; i--)
            {
                if (node == _listOfActiveFormattingElements[i])
                {
                    return i;
                }
            }
            return -1;
        }

        private int FindInListOfActiveFormattingElementsContainsBetweenEndAndLastMarker([Local] string name)
        {
            for (int i = _listPtr; i >= 0; i--)
            {
                StackNode<T> node = _listOfActiveFormattingElements[i];
                if (node == null)
                {
                    return -1;
                }
                if (node._name == name)
                {
                    return i;
                }
            }
            return -1;
        }


        private void MaybeForgetEarlierDuplicateFormattingElement([Local] string name, HtmlAttributes attributes)
        {
            int candidate = -1;
            int count = 0;
            for (int i = _listPtr; i >= 0; i--)
            {
                StackNode<T> node = _listOfActiveFormattingElements[i];
                if (node == null)
                {
                    break;
                }
                if (node._name == name && node._attributes.Equals(attributes))
                {
                    candidate = i;
                    ++count;
                }
            }
            if (count >= 3)
            {
                RemoveFromListOfActiveFormattingElements(candidate);
            }
        }

        private int FindLastOrRoot([Local] string name)
        {
            for (int i = _currentPtr; i > 0; i--)
            {
                if (_stack[i]._name == name)
                {
                    return i;
                }
            }
            return 0;
        }

        private int FindLastOrRoot(DispatchGroup group)
        {
            for (int i = _currentPtr; i > 0; i--)
            {
                if (_stack[i].Group == group)
                {
                    return i;
                }
            }
            return 0;
        }

        private void AddAttributesToHtml(HtmlAttributes attributes)
        {
            // [NOCPP[
            CheckAttributes(attributes, "http://www.w3.org/1999/xhtml");
            // ]NOCPP]
            AddAttributesToElement(_stack[0]._node, attributes);
        }

        private void PushHeadPointerOntoStack()
        {
            Debug.Assert(_headPointer != null);
            Debug.Assert(!_fragment);
            Debug.Assert(_mode == InsertionMode.AFTER_HEAD);
            Fatal();
            SilentPush(new StackNode<T>(ElementName.HEAD, _headPointer
                                        // [NOCPP[
                                        , ErrorEvent == null ? null : new TaintableLocator(_tokenizer)
                           // ]NOCPP]
                           ));
        }

        private void ReconstructTheActiveFormattingElements()
        {
            if (_listPtr == -1)
            {
                return;
            }
            StackNode<T> mostRecent = _listOfActiveFormattingElements[_listPtr];
            if (mostRecent == null || IsInStack(mostRecent))
            {
                return;
            }
            int entryPos = _listPtr;
            for (;;)
            {
                entryPos--;
                if (entryPos == -1)
                {
                    break;
                }
                if (_listOfActiveFormattingElements[entryPos] == null)
                {
                    break;
                }
                if (IsInStack(_listOfActiveFormattingElements[entryPos]))
                {
                    break;
                }
            }
            while (entryPos < _listPtr)
            {
                entryPos++;
                StackNode<T> entry = _listOfActiveFormattingElements[entryPos];
                T clone = CreateElement("http://www.w3.org/1999/xhtml", entry._name,
                                        entry._attributes.CloneAttributes());
                var entryClone = new StackNode<T>(entry.Flags,
                                                  entry._ns, entry._name, clone, entry._popName,
                                                  entry._attributes
                                                  // [NOCPP[
                                                  , entry.Locator
                    // ]NOCPP]
                    );
                entry.DropAttributes(); // transfer ownership to entryClone
                StackNode<T> currentNode = _stack[_currentPtr];
                if (currentNode.IsFosterParenting)
                {
                    InsertIntoFosterParent(clone);
                }
                else
                {
                    AppendElement(clone, currentNode._node);
                }
                Push(entryClone);
                // stack takes ownership of the local variable
                _listOfActiveFormattingElements[entryPos] = entryClone;
                // overwriting the old entry on the list, so release & retain
            }
        }

        private void InsertIntoFosterParent(T child)
        {
            int eltPos = FindLastOrRoot(DispatchGroup.TABLE);
            StackNode<T> node = _stack[eltPos];
            T elt = node._node;
            if (eltPos == 0)
            {
                AppendElement(child, elt);
                return;
            }
            InsertFosterParentedChild(child, elt, _stack[eltPos - 1]._node);
        }

        private bool IsInStack(StackNode<T> node)
        {
            for (int i = _currentPtr; i >= 0; i--)
            {
                if (_stack[i] == node)
                {
                    return true;
                }
            }
            return false;
        }

        private void Pop()
        {
            StackNode<T> node = _stack[_currentPtr];
            Debug.Assert(ClearLastStackSlot());
            _currentPtr--;
            ElementPopped(node._ns, node._popName, node._node);
        }

        private void SilentPop()
        {
            Debug.Assert(ClearLastStackSlot());
            _currentPtr--;
        }

        private void PopOnEof()
        {
            StackNode<T> node = _stack[_currentPtr];
            Debug.Assert(ClearLastStackSlot());
            _currentPtr--;
            MarkMalformedIfScript(node._node);
            ElementPopped(node._ns, node._popName, node._node);
        }

        // [NOCPP[
        private void CheckAttributes(HtmlAttributes attributes, [NsUri] string ns)
        {
            if (ErrorEvent != null)
            {
                int len = attributes.XmlnsLength;
                for (int i = 0; i < len; i++)
                {
                    AttributeName name = attributes.GetXmlnsAttributeName(i);
                    if (name == AttributeName.XMLNS)
                    {
                        if (_html4)
                        {
                            Err("Attribute \u201Cxmlns\u201D not allowed here. (HTML4-only error.)");
                        }
                        else
                        {
                            string xmlns = attributes.GetXmlnsValue(i);
                            if (ns != xmlns)
                            {
                                Err("Bad value \u201C"
                                    + xmlns
                                    + "\u201D for the attribute \u201Cxmlns\u201D (only \u201C"
                                    + ns + "\u201D permitted here).");
                                switch (NamePolicy)
                                {
                                    case XmlViolationPolicy.AlterInfoset:
                                        // fall through
                                    case XmlViolationPolicy.Allow:
                                        Warn("Attribute \u201Cxmlns\u201D is not serializable as XML 1.0.");
                                        break;
                                    case XmlViolationPolicy.Fatal:
                                        Fatal("Attribute \u201Cxmlns\u201D is not serializable as XML 1.0.");
                                        break;
                                }
                            }
                        }
                    }
                    else if (ns != "http://www.w3.org/1999/xhtml"
                             && name == AttributeName.XMLNS_XLINK)
                    {
                        string xmlns = attributes.GetXmlnsValue(i);
                        if ("http://www.w3.org/1999/xlink" != xmlns)
                        {
                            Err("Bad value \u201C"
                                + xmlns
                                +
                                "\u201D for the attribute \u201Cxmlns:link\u201D (only \u201Chttp://www.w3.org/1999/xlink\u201D permitted here).");
                            switch (NamePolicy)
                            {
                                case XmlViolationPolicy.AlterInfoset:
                                    // fall through
                                case XmlViolationPolicy.Allow:
                                    Warn(
                                        "Attribute \u201Cxmlns:xlink\u201D with a value other than \u201Chttp://www.w3.org/1999/xlink\u201D is not serializable as XML 1.0 without changing document semantics.");
                                    break;
                                case XmlViolationPolicy.Fatal:
                                    Fatal(
                                        "Attribute \u201Cxmlns:xlink\u201D with a value other than \u201Chttp://www.w3.org/1999/xlink\u201D is not serializable as XML 1.0 without changing document semantics.");
                                    break;
                            }
                        }
                    }
                    else
                    {
                        Err("Attribute \u201C" + attributes.GetXmlnsLocalName(i)
                            + "\u201D not allowed here.");
                        switch (NamePolicy)
                        {
                            case XmlViolationPolicy.AlterInfoset:
                                // fall through
                            case XmlViolationPolicy.Allow:
                                Warn("Attribute with the local name \u201C"
                                     + attributes.GetXmlnsLocalName(i)
                                     + "\u201D is not serializable as XML 1.0.");
                                break;
                            case XmlViolationPolicy.Fatal:
                                Fatal("Attribute with the local name \u201C"
                                      + attributes.GetXmlnsLocalName(i)
                                      + "\u201D is not serializable as XML 1.0.");
                                break;
                        }
                    }
                }
            }
            attributes.ProcessNonNcNames(this, NamePolicy);
        }

        private string CheckPopName([Local] string name)
        {
            if (NCName.IsNCName(name))
            {
                return name;
            }
            switch (NamePolicy)
            {
                case XmlViolationPolicy.Allow:
                    Warn("Element name \u201C" + name
                         + "\u201D cannot be represented as XML 1.0.");
                    return name;
                case XmlViolationPolicy.AlterInfoset:
                    Warn("Element name \u201C" + name
                         + "\u201D cannot be represented as XML 1.0.");
                    return NCName.EscapeName(name);
                case XmlViolationPolicy.Fatal:
                    Fatal("Element name \u201C" + name
                          + "\u201D cannot be represented as XML 1.0.");
                    break;
            }
            return null; // keep compiler happy
        }

        // ]NOCPP]

        private void AppendHtmlElementToDocumentAndPush(HtmlAttributes attributes)
        {
            // [NOCPP[
            CheckAttributes(attributes, "http://www.w3.org/1999/xhtml");
            // ]NOCPP]
            T elt = CreateHtmlElementSetAsRoot(attributes);
            var node = new StackNode<T>(ElementName.HTML,
                                        elt
                                        // [NOCPP[
                                        , ErrorEvent == null ? null : new TaintableLocator(_tokenizer)
                // ]NOCPP]
                );
            Push(node);
        }

        private void AppendHtmlElementToDocumentAndPush()
        {
            AppendHtmlElementToDocumentAndPush(_tokenizer.EmptyAttributes());
        }

        private void AppendToCurrentNodeAndPushHeadElement(HtmlAttributes attributes)
        {
            // [NOCPP[
            CheckAttributes(attributes, "http://www.w3.org/1999/xhtml");
            // ]NOCPP]
            T elt = CreateElement("http://www.w3.org/1999/xhtml", "head",
                                  attributes);
            AppendElement(elt, _stack[_currentPtr]._node);
            _headPointer = elt;
            var node = new StackNode<T>(ElementName.HEAD,
                                        elt
                                        // [NOCPP[
                                        , ErrorEvent == null ? null : new TaintableLocator(_tokenizer)
                // ]NOCPP]
                );
            Push(node);
        }

        private void AppendToCurrentNodeAndPushBodyElement(HtmlAttributes attributes)
        {
            AppendToCurrentNodeAndPushElement(ElementName.BODY,
                                              attributes);
        }

        private void AppendToCurrentNodeAndPushBodyElement()
        {
            AppendToCurrentNodeAndPushBodyElement(_tokenizer.EmptyAttributes());
        }

        private void AppendToCurrentNodeAndPushFormElementMayFoster(
            HtmlAttributes attributes)
        {
            // [NOCPP[
            CheckAttributes(attributes, "http://www.w3.org/1999/xhtml");
            // ]NOCPP]
            T elt = CreateElement("http://www.w3.org/1999/xhtml", "form",
                                  attributes);
            _formPointer = elt;
            StackNode<T> current = _stack[_currentPtr];
            if (current.IsFosterParenting)
            {
                Fatal();
                InsertIntoFosterParent(elt);
            }
            else
            {
                AppendElement(elt, current._node);
            }
            var node = new StackNode<T>(ElementName.FORM,
                                        elt
                                        // [NOCPP[
                                        , ErrorEvent == null ? null : new TaintableLocator(_tokenizer)
                // ]NOCPP]
                );
            Push(node);
        }

        private void AppendToCurrentNodeAndPushFormattingElementMayFoster(ElementName elementName,
                                                                          HtmlAttributes attributes)
        {
            // [NOCPP[
            CheckAttributes(attributes, "http://www.w3.org/1999/xhtml");
            // ]NOCPP]
            // This method can't be called for custom elements
            T elt = CreateElement("http://www.w3.org/1999/xhtml", elementName.name, attributes);
            StackNode<T> current = _stack[_currentPtr];
            if (current.IsFosterParenting)
            {
                Fatal();
                InsertIntoFosterParent(elt);
            }
            else
            {
                AppendElement(elt, current._node);
            }
            var node = new StackNode<T>(elementName, elt, attributes.CloneAttributes()
                                        // [NOCPP[
                                        , ErrorEvent == null ? null : new TaintableLocator(_tokenizer)
                // ]NOCPP]
                );
            Push(node);
            Append(node);
        }

        private void AppendToCurrentNodeAndPushElement(ElementName elementName, HtmlAttributes attributes)
        {
            // [NOCPP[
            CheckAttributes(attributes, "http://www.w3.org/1999/xhtml");
            // ]NOCPP]
            // This method can't be called for custom elements
            T elt = CreateElement("http://www.w3.org/1999/xhtml", elementName.name, attributes);
            AppendElement(elt, _stack[_currentPtr]._node);
            var node = new StackNode<T>(elementName, elt
                                        // [NOCPP[
                                        , ErrorEvent == null ? null : new TaintableLocator(_tokenizer)
                // ]NOCPP]
                );
            Push(node);
        }

        private void AppendToCurrentNodeAndPushElementMayFoster(ElementName elementName,
                                                                HtmlAttributes attributes)
        {
            /*[Local]*/
            string popName = elementName.name;
            // [NOCPP[
            CheckAttributes(attributes, "http://www.w3.org/1999/xhtml");
            if (elementName.IsCustom)
            {
                popName = CheckPopName(popName);
            }
            // ]NOCPP]
            T elt = CreateElement("http://www.w3.org/1999/xhtml", popName, attributes);
            StackNode<T> current = _stack[_currentPtr];
            if (current.IsFosterParenting)
            {
                Fatal();
                InsertIntoFosterParent(elt);
            }
            else
            {
                AppendElement(elt, current._node);
            }
            var node = new StackNode<T>(elementName, elt, popName
                                        // [NOCPP[
                                        , ErrorEvent == null ? null : new TaintableLocator(_tokenizer)
                // ]NOCPP]
                );
            Push(node);
        }

        private void AppendToCurrentNodeAndPushElementMayFosterMathML(
            ElementName elementName, HtmlAttributes attributes)
        {
            /*[Local]*/
            string popName = elementName.name;
            // [NOCPP[
            CheckAttributes(attributes, "http://www.w3.org/1998/Math/MathML");
            if (elementName.IsCustom)
            {
                popName = CheckPopName(popName);
            }
            // ]NOCPP]
            T elt = CreateElement("http://www.w3.org/1998/Math/MathML", popName,
                                  attributes);
            StackNode<T> current = _stack[_currentPtr];
            if (current.IsFosterParenting)
            {
                Fatal();
                InsertIntoFosterParent(elt);
            }
            else
            {
                AppendElement(elt, current._node);
            }
            bool markAsHtmlIntegrationPoint = ElementName.ANNOTATION_XML == elementName
                                              && AnnotationXmlEncodingPermitsHtml(attributes);
            var node = new StackNode<T>(elementName, elt, popName,
                                        markAsHtmlIntegrationPoint
                                        // [NOCPP[
                                        , ErrorEvent == null ? null : new TaintableLocator(_tokenizer)
                // ]NOCPP]
                );
            Push(node);
        }

        private bool AnnotationXmlEncodingPermitsHtml(HtmlAttributes attributes)
        {
            string encoding = attributes.GetValue(AttributeName.ENCODING);
            if (encoding == null)
            {
                return false;
            }
            return Portability.LowerCaseLiteralEqualsIgnoreAsciiCaseString(
                "application/xhtml+xml", encoding)
                   || Portability.LowerCaseLiteralEqualsIgnoreAsciiCaseString(
                       "text/html", encoding);
        }

        private void AppendToCurrentNodeAndPushElementMayFosterSVG(
            ElementName elementName, HtmlAttributes attributes)
        {
            /*[Local]*/
            string popName = elementName.camelCaseName;
            // [NOCPP[
            CheckAttributes(attributes, "http://www.w3.org/2000/svg");
            if (elementName.IsCustom)
            {
                popName = CheckPopName(popName);
            }
            // ]NOCPP]
            T elt = CreateElement("http://www.w3.org/2000/svg", popName, attributes);
            StackNode<T> current = _stack[_currentPtr];
            if (current.IsFosterParenting)
            {
                Fatal();
                InsertIntoFosterParent(elt);
            }
            else
            {
                AppendElement(elt, current._node);
            }
            var node = new StackNode<T>(elementName, popName, elt
                                        // [NOCPP[
                                        , ErrorEvent == null ? null : new TaintableLocator(_tokenizer)
                // ]NOCPP]
                );
            Push(node);
        }

        private void AppendToCurrentNodeAndPushElementMayFoster(ElementName elementName, HtmlAttributes attributes,
                                                                T form)
        {
            // [NOCPP[
            CheckAttributes(attributes, "http://www.w3.org/1999/xhtml");
            // ]NOCPP]
            // Can't be called for custom elements
            T elt = CreateElement("http://www.w3.org/1999/xhtml", elementName.name, attributes, _fragment
                                                                                                    ? null
                                                                                                    : form);
            StackNode<T> current = _stack[_currentPtr];
            if (current.IsFosterParenting)
            {
                Fatal();
                InsertIntoFosterParent(elt);
            }
            else
            {
                AppendElement(elt, current._node);
            }
            var node = new StackNode<T>(elementName, elt
                                        // [NOCPP[
                                        , ErrorEvent == null ? null : new TaintableLocator(_tokenizer)
                // ]NOCPP]
                );
            Push(node);
        }

        private void AppendVoidElementToCurrentMayFoster(
            [Local] string name, HtmlAttributes attributes, T form)
        {
            // [NOCPP[
            CheckAttributes(attributes, "http://www.w3.org/1999/xhtml");
            // ]NOCPP]
            // Can't be called for custom elements
            T elt = CreateElement("http://www.w3.org/1999/xhtml", name, attributes, _fragment ? null : form);
            StackNode<T> current = _stack[_currentPtr];
            if (current.IsFosterParenting)
            {
                Fatal();
                InsertIntoFosterParent(elt);
            }
            else
            {
                AppendElement(elt, current._node);
            }
            ElementPushed("http://www.w3.org/1999/xhtml", name, elt);
            ElementPopped("http://www.w3.org/1999/xhtml", name, elt);
        }

        private void AppendVoidElementToCurrentMayFoster(
            ElementName elementName, HtmlAttributes attributes)
        {
            /*[Local]*/
            string popName = elementName.name;
            // [NOCPP[
            CheckAttributes(attributes, "http://www.w3.org/1999/xhtml");
            if (elementName.IsCustom)
            {
                popName = CheckPopName(popName);
            }
            // ]NOCPP]
            T elt = CreateElement("http://www.w3.org/1999/xhtml", popName, attributes);
            StackNode<T> current = _stack[_currentPtr];
            if (current.IsFosterParenting)
            {
                Fatal();
                InsertIntoFosterParent(elt);
            }
            else
            {
                AppendElement(elt, current._node);
            }
            ElementPushed("http://www.w3.org/1999/xhtml", popName, elt);
            ElementPopped("http://www.w3.org/1999/xhtml", popName, elt);
        }

        private void AppendVoidElementToCurrentMayFosterSVG(
            ElementName elementName, HtmlAttributes attributes)
        {
            /*[Local]*/
            string popName = elementName.camelCaseName;
            // [NOCPP[
            CheckAttributes(attributes, "http://www.w3.org/2000/svg");
            if (elementName.IsCustom)
            {
                popName = CheckPopName(popName);
            }
            // ]NOCPP]
            T elt = CreateElement("http://www.w3.org/2000/svg", popName, attributes);
            StackNode<T> current = _stack[_currentPtr];
            if (current.IsFosterParenting)
            {
                Fatal();
                InsertIntoFosterParent(elt);
            }
            else
            {
                AppendElement(elt, current._node);
            }
            ElementPushed("http://www.w3.org/2000/svg", popName, elt);
            ElementPopped("http://www.w3.org/2000/svg", popName, elt);
        }

        private void AppendVoidElementToCurrentMayFosterMathML(ElementName elementName, HtmlAttributes attributes)
        {
            /*[Local]*/
            string popName = elementName.name;
            // [NOCPP[
            CheckAttributes(attributes, "http://www.w3.org/1998/Math/MathML");
            if (elementName.IsCustom)
            {
                popName = CheckPopName(popName);
            }
            // ]NOCPP]
            T elt = CreateElement("http://www.w3.org/1998/Math/MathML", popName, attributes);
            StackNode<T> current = _stack[_currentPtr];
            if (current.IsFosterParenting)
            {
                Fatal();
                InsertIntoFosterParent(elt);
            }
            else
            {
                AppendElement(elt, current._node);
            }
            ElementPushed("http://www.w3.org/1998/Math/MathML", popName, elt);
            ElementPopped("http://www.w3.org/1998/Math/MathML", popName, elt);
        }

        private void AppendVoidElementToCurrent(
            [Local] string name, HtmlAttributes attributes, T form)
        {
            // [NOCPP[
            CheckAttributes(attributes, "http://www.w3.org/1999/xhtml");
            // ]NOCPP]
            // Can't be called for custom elements
            T elt = CreateElement("http://www.w3.org/1999/xhtml", name, attributes, _fragment ? null : form);
            StackNode<T> current = _stack[_currentPtr];
            AppendElement(elt, current._node);
            ElementPushed("http://www.w3.org/1999/xhtml", name, elt);
            ElementPopped("http://www.w3.org/1999/xhtml", name, elt);
        }

        private void AppendVoidFormToCurrent(HtmlAttributes attributes)
        {
            // [NOCPP[
            CheckAttributes(attributes, "http://www.w3.org/1999/xhtml");
            // ]NOCPP]
            T elt = CreateElement("http://www.w3.org/1999/xhtml", "form",
                                  attributes);
            _formPointer = elt;
            // ownership transferred to form pointer
            StackNode<T> current = _stack[_currentPtr];
            AppendElement(elt, current._node);
            ElementPushed("http://www.w3.org/1999/xhtml", "form", elt);
            ElementPopped("http://www.w3.org/1999/xhtml", "form", elt);
        }

        // [NOCPP[

        private void AccumulateCharactersForced(char[] buf, int start, int length)
        {
            int newLen = _charBufferLen + length;
            if (newLen > _charBuffer.Length)
            {
                var newBuf = new char[newLen];
                Array.Copy(_charBuffer, newBuf, _charBufferLen);
                _charBuffer = newBuf;
            }
            Array.Copy(buf, start, _charBuffer, _charBufferLen, length);
            _charBufferLen = newLen;
        }

        // ]NOCPP]

        protected virtual void AccumulateCharacters(char[] buf, int start, int length)
        {
            AppendCharacters(_stack[_currentPtr]._node, buf, start, length);
        }

        // ------------------------------- //

        protected void RequestSuspension()
        {
            _tokenizer.RequestSuspension();
        }

        protected abstract T CreateElement([NsUri] string ns, [Local] string name,
                                           HtmlAttributes attributes);

        protected virtual T CreateElement([NsUri] string ns, [Local] string name,
                                          HtmlAttributes attributes, T form)
        {
            return CreateElement("http://www.w3.org/1999/xhtml", name, attributes);
        }

        protected abstract T CreateHtmlElementSetAsRoot(HtmlAttributes attributes);

        protected abstract void DetachFromParent(T element);

        protected abstract bool HasChildren(T element);

        protected abstract void AppendElement(T child, T newParent);

        protected abstract void AppendChildrenToNewParent(T oldParent, T newParent);

        protected abstract void InsertFosterParentedChild(T child, T table, T stackParent);

        protected abstract void InsertFosterParentedCharacters(
            char[] buf, int start, int length, T table, T stackParent);

        protected abstract void AppendCharacters(T parent, char[] buf,
                                                 int start, int length);

        protected abstract void AppendIsindexPrompt(T parent);

        protected abstract void AppendComment(T parent, char[] buf, int start, int length);

        protected abstract void AppendCommentToDocument(char[] buf, int start, int length);

        protected abstract void AddAttributesToElement(T element, HtmlAttributes attributes);

        protected void MarkMalformedIfScript(T elt)
        {
        }

        protected virtual void Start(bool fragmentMode)
        {
        }

        protected virtual void End()
        {
        }

        protected virtual void AppendDoctypeToDocument([Local] string name,
                                                       string publicIdentifier, string systemIdentifier)
        {
        }

        protected virtual void ElementPushed([NsUri] string ns, [Local] string name, T node)
        {
        }

        protected virtual void ElementPopped([NsUri] string ns, [Local] string name, T node)
        {
        }

        // [NOCPP[

        protected virtual void ReceiveDocumentMode(DocumentMode m, string publicIdentifier,
                                                   string systemIdentifier, bool html4SpecificAdditionalErrorChecks)
        {
            // is overridden is subclasses
        }

        /// <summary>
        ///     If this handler implementation cares about comments, return <code>true</code>.
        ///     If not, return <code>false</code>
        /// </summary>
        /// <returns>
        ///     Whether this handler wants comments
        /// </returns>
        bool ITokenHandler.WantsComments
        {
            get { return _wantingComments; }
        }

        public bool IsIgnoringComments
        {
            get { return !_wantingComments; }
            set { _wantingComments = !value; }
        }

        /**
		 * The argument MUST be an interned string or <code>null</code>.
		 * 
		 * @param context
		 */

        public void SetFragmentContext([Local] string context)
        {
            _contextName = context;
            _contextNamespace = "http://www.w3.org/1999/xhtml";
            _contextNode = null;
            _fragment = (_contextName != null);
            _quirks = false;
        }

        // ]NOCPP]

        /// <summary>
        ///     Checks if the CDATA sections are allowed.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if CDATA sections are allowed
        /// </returns>
        public bool IsCDataSectionAllowed
        {
            get { return IsInForeign; }
        }

        private bool IsInForeign
        {
            get { return _currentPtr >= 0 && _stack[_currentPtr]._ns != "http://www.w3.org/1999/xhtml"; }
        }

        private bool IsInForeignButNotHtmlIntegrationPoint
        {
            get
            {
                return _currentPtr >= 0
                       && _stack[_currentPtr]._ns != "http://www.w3.org/1999/xhtml"
                       && !_stack[_currentPtr].IsHtmlIntegrationPoint;
            }
        }

        /**
		 * The argument MUST be an interned string or <code>null</code>.
		 * 
		 * @param context
		 */

        public void SetFragmentContext([Local] string context,
                                       [NsUri] string ns, T node, bool quirks)
        {
            _contextName = context;
            _contextNamespace = ns;
            _contextNode = node;
            _fragment = (_contextName != null);
            _quirks = quirks;
        }

        protected T CurrentNode()
        {
            return _stack[_currentPtr]._node;
        }

        /// <summary>
        ///     Flushes the pending characters. Public for document.write use cases only.
        /// </summary>
        public void FlushCharacters()
        {
            if (_charBufferLen > 0)
            {
                if ((_mode == InsertionMode.IN_TABLE || _mode == InsertionMode.IN_TABLE_BODY ||
                     _mode == InsertionMode.IN_ROW)
                    && CharBufferContainsNonWhitespace())
                {
                    Err("Misplaced non-space characters insided a table.");
                    ReconstructTheActiveFormattingElements();
                    if (!_stack[_currentPtr].IsFosterParenting)
                    {
                        // reconstructing gave us a new current node
                        AppendCharacters(CurrentNode(), _charBuffer, 0,
                                         _charBufferLen);
                        _charBufferLen = 0;
                        return;
                    }
                    int eltPos = FindLastOrRoot(DispatchGroup.TABLE);
                    StackNode<T> node = _stack[eltPos];
                    T elt = node._node;
                    if (eltPos == 0)
                    {
                        AppendCharacters(elt, _charBuffer, 0, _charBufferLen);
                        _charBufferLen = 0;
                        return;
                    }
                    InsertFosterParentedCharacters(_charBuffer, 0, _charBufferLen,
                                                   elt, _stack[eltPos - 1]._node);
                    _charBufferLen = 0;
                    return;
                }
                AppendCharacters(CurrentNode(), _charBuffer, 0, _charBufferLen);
                _charBufferLen = 0;
            }
        }

        private bool CharBufferContainsNonWhitespace()
        {
            for (int i = 0; i < _charBufferLen; i++)
            {
                switch (_charBuffer[i])
                {
                    case ' ':
                    case '\t':
                    case '\n':
                    case '\r':
                    case '\u000C':
                        continue;
                    default:
                        return true;
                }
            }
            return false;
        }

        #region Snapshots

        /// <summary>
        ///     Creates a comparable snapshot of the tree builder state. Snapshot
        ///     creation is only supported immediately after a script end tag has been
        ///     processed. In C++ the caller is responsible for calling
        ///     <code>delete</code> on the returned object.
        /// </summary>
        /// <returns>A snapshot</returns>
        public ITreeBuilderState<T> NewSnapshot()
        {
            var listCopy = new StackNode<T>[_listPtr + 1];
            for (int i = 0; i < listCopy.Length; i++)
            {
                StackNode<T> node = _listOfActiveFormattingElements[i];
                if (node != null)
                {
                    var newNode = new StackNode<T>(node.Flags, node._ns,
                                                   node._name, node._node, node._popName,
                                                   node._attributes.CloneAttributes()
                                                   // [NOCPP[
                                                   , node.Locator
                        // ]NOCPP]
                        );
                    listCopy[i] = newNode;
                }
                else
                {
                    listCopy[i] = null;
                }
            }
            var stackCopy = new StackNode<T>[_currentPtr + 1];
            for (int i = 0; i < stackCopy.Length; i++)
            {
                StackNode<T> node = _stack[i];
                int listIndex = FindInListOfActiveFormattingElements(node);
                if (listIndex == -1)
                {
                    var newNode = new StackNode<T>(node.Flags, node._ns,
                                                   node._name, node._node, node._popName,
                                                   null
                                                   // [NOCPP[
                                                   , node.Locator
                        // ]NOCPP]
                        );
                    stackCopy[i] = newNode;
                }
                else
                {
                    stackCopy[i] = listCopy[listIndex];
                }
            }
            return new StateSnapshot<T>(stackCopy, listCopy, _formPointer, _headPointer, _deepTreeSurrogateParent, _mode,
                                        _originalMode, _framesetOk, _needToDropLF, _quirks);
        }

        public bool SnapshotMatches(ITreeBuilderState<T> snapshot)
        {
            StackNode<T>[] stackCopy = snapshot.Stack;
            int stackLen = snapshot.Stack.Length;
            StackNode<T>[] listCopy = snapshot.ListOfActiveFormattingElements;
            int listLen = snapshot.ListOfActiveFormattingElements.Length;

            if (stackLen != _currentPtr + 1
                || listLen != _listPtr + 1
                || _formPointer != snapshot.FormPointer
                || _headPointer != snapshot.HeadPointer
                || _deepTreeSurrogateParent != snapshot.DeepTreeSurrogateParent
                || _mode != snapshot.Mode
                || _originalMode != snapshot.OriginalMode
                || _framesetOk != snapshot.IsFramesetOk
                || _needToDropLF != snapshot.IsNeedToDropLF
                || _quirks != snapshot.IsQuirks)
            {
                // maybe just assert quirks
                return false;
            }
            for (int i = listLen - 1; i >= 0; i--)
            {
                if (listCopy[i] == null
                    && _listOfActiveFormattingElements[i] == null)
                {
                    continue;
                }
                if (listCopy[i] == null
                    || _listOfActiveFormattingElements[i] == null)
                {
                    return false;
                }
                if (listCopy[i]._node != _listOfActiveFormattingElements[i]._node)
                {
                    return false; // it's possible that this condition is overly
                    // strict
                }
            }
            for (int i = stackLen - 1; i >= 0; i--)
            {
                if (stackCopy[i]._node != _stack[i]._node)
                {
                    return false;
                }
            }
            return true;
        }

        public void LoadState(ITreeBuilderState<T> snapshot)
        {
            StackNode<T>[] stackCopy = snapshot.Stack;
            int stackLen = snapshot.Stack.Length;
            StackNode<T>[] listCopy = snapshot.ListOfActiveFormattingElements;
            int listLen = snapshot.ListOfActiveFormattingElements.Length;

            if (_listOfActiveFormattingElements.Length < listLen)
            {
                _listOfActiveFormattingElements = new StackNode<T>[listLen];
            }
            _listPtr = listLen - 1;

            if (_stack.Length < stackLen)
            {
                _stack = new StackNode<T>[stackLen];
            }
            _currentPtr = stackLen - 1;

            for (int i = 0; i < listLen; i++)
            {
                StackNode<T> node = listCopy[i];
                if (node != null)
                {
                    var newNode = new StackNode<T>(node.Flags, node._ns,
                                                   node._name, node._node,
                                                   node._popName,
                                                   node._attributes.CloneAttributes()
                                                   // [NOCPP[
                                                   , node.Locator
                        // ]NOCPP]
                        );
                    _listOfActiveFormattingElements[i] = newNode;
                }
                else
                {
                    _listOfActiveFormattingElements[i] = null;
                }
            }
            for (int i = 0; i < stackLen; i++)
            {
                StackNode<T> node = stackCopy[i];
                int listIndex = FindInArray(node, listCopy);
                if (listIndex == -1)
                {
                    var newNode = new StackNode<T>(node.Flags, node._ns,
                                                   node._name, node._node,
                                                   node._popName,
                                                   null
                                                   // [NOCPP[
                                                   , node.Locator
                        // ]NOCPP]       
                        );
                    _stack[i] = newNode;
                }
                else
                {
                    _stack[i] = _listOfActiveFormattingElements[listIndex];
                }
            }
            _formPointer = snapshot.FormPointer;
            _headPointer = snapshot.HeadPointer;
            _deepTreeSurrogateParent = snapshot.DeepTreeSurrogateParent;
            _mode = snapshot.Mode;
            _originalMode = snapshot.OriginalMode;
            _framesetOk = snapshot.IsFramesetOk;
            _needToDropLF = snapshot.IsNeedToDropLF;
            _quirks = snapshot.IsQuirks;
        }

        private int FindInArray(StackNode<T> node, StackNode<T>[] arr)
        {
            for (int i = _listPtr; i >= 0; i--)
            {
                if (node == arr[i])
                {
                    return i;
                }
            }
            return -1;
        }

        public T FormPointer
        {
            get { return _formPointer; }
        }

        public T HeadPointer
        {
            get { return _headPointer; }
        }

        public T DeepTreeSurrogateParent
        {
            get { return _deepTreeSurrogateParent; }
        }

        /// <summary>
        ///     Gets the list of active formatting elements.
        /// </summary>
        public StackNode<T>[] ListOfActiveFormattingElements
        {
            get { return _listOfActiveFormattingElements; }
        }

        /// <summary>
        ///     Gets the stack.
        /// </summary>
        public StackNode<T>[] Stack
        {
            get { return _stack; }
        }

        public InsertionMode Mode
        {
            get { return _mode; }
        }

        public InsertionMode OriginalMode
        {
            get { return _originalMode; }
        }

        public bool IsFramesetOk
        {
            get { return _framesetOk; }
        }

        public bool IsNeedToDropLF
        {
            get { return _needToDropLF; }
        }

        public bool IsQuirks
        {
            get { return _quirks; }
        }

        #endregion
    }
}