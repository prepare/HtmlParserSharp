/*
 * Copyright (c) 2005-2007 Henri Sivonen
 * Copyright (c) 2007-2010 Mozilla Foundation
 * Portions of comments Copyright 2004-2010 Apple Computer, Inc., Mozilla 
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
 * comment are quotes from the WHATWG HTML 5 spec as of 2 June 2007 
 * amended as of June 18 2008 and May 31 2010.
 * That document came with this statement:
 * © Copyright 2004-2010 Apple Computer, Inc., Mozilla Foundation, and 
 * Opera Software ASA. You are granted a license to use, reproduce and 
 * create derivative works of this document."
 */


using System;
using System.Text;
using System.Collections.Generic;

using System.Diagnostics;
using HtmlParserSharp.Common;

#pragma warning disable 1591 // Missing XML comment
#pragma warning disable 1570 // XML comment on 'construct' has badly formed XML — 'reason'
#pragma warning disable 1587 // XML comment is not placed on a valid element



namespace HtmlParserSharp.Core
{
    //with html5 state 
    //review missing state

    public enum TokenizerState : byte
    {
        s01_DATA = 128,
        
        //TODO: 02_CharacterReferenceInData()

        s03_RCDATA = 129,

        //TODO: R04_CharacterReferenceInRcData();

        s05_RAWTEXT = 3,

        s06_SCRIPT_DATA = 2,

        s07_PLAINTEXT = 8,
        
        s08_TAG_OPEN = 9,

        s09_CLOSE_TAG_OPEN = 10,

        s10_TAG_NAME = 11,

        s11_RAWTEXT_RCDATA_LESS_THAN_SIGN = 65,
        
        //TODO: R12_RcDataEndTagOpen();

        //TODO:  R13_RcDataEndTagName();

        //TODO: R14_RawTextLessThan();

        //TODO: R15_RawTextEndTagOpen();

        //TODO: R16_RawTextEndTagName();

        s17_SCRIPT_DATA_LESS_THAN_SIGN = 59,

        //TODO: R18_ScriptDataEndTagOpen();

        //TODO: R19_ScriptDataEndTagName

        s20_SCRIPT_DATA_ESCAPE_START = 60,

        s21_SCRIPT_DATA_ESCAPE_START_DASH = 61,

        s22_SCRIPT_DATA_ESCAPED = 4,

        s23_SCRIPT_DATA_ESCAPED_DASH = 62,

        s24_SCRIPT_DATA_ESCAPED_DASH_DASH = 63,

        s25_SCRIPT_DATA_ESCAPED_LESS_THAN_SIGN = 66,

        //TODO: R26_ScriptDataEscapedEndTagOpen();

        //TODO: R27_ScriptDataEscapedEndTagName();

        s28_SCRIPT_DATA_DOUBLE_ESCAPE_START = 67,

        s29_SCRIPT_DATA_DOUBLE_ESCAPED = 68,

        s30_SCRIPT_DATA_DOUBLE_ESCAPED_DASH = 70,

        s31_SCRIPT_DATA_DOUBLE_ESCAPED_DASH_DASH = 71,

        s32_SCRIPT_DATA_DOUBLE_ESCAPED_LESS_THAN_SIGN = 69,

        s33_SCRIPT_DATA_DOUBLE_ESCAPE_END = 72,

        s34_BEFORE_ATTRIBUTE_NAME = 12,

        s35_ATTRIBUTE_NAME = 13,

        s36_AFTER_ATTRIBUTE_NAME = 14,

        s37_BEFORE_ATTRIBUTE_VALUE = 15,

        s38_ATTRIBUTE_VALUE_DOUBLE_QUOTED = 5,

        s39_ATTRIBUTE_VALUE_SINGLE_QUOTED = 6,

        s40_ATTRIBUTE_VALUE_UNQUOTED = 7,

        //TODO: R41_CharacterReferenceInAttributeValue()

        s42__AFTER_ATTRIBUTE_VALUE_QUOTED = 16,

        s43_SELF_CLOSING_START_TAG = 54,
        
        s44_BOGUS_COMMENT = 17,

        s45_MARKUP_DECLARATION_OPEN = 18,

        s46_COMMENT_START = 32,

        s47_COMMENT_START_DASH = 33,

        s48_COMMENT = 34,

        s49_COMMENT_END_DASH = 35,

        s50_COMMENT_END = 36,

        s51_COMMENT_END_BANG = 37,

        s52_DOCTYPE = 19,

        s53_BEFORE_DOCTYPE_NAME = 20,

        s54_DOCTYPE_NAME = 21,

        s55_AFTER_DOCTYPE_NAME = 22,

        s56_AFTER_DOCTYPE_PUBLIC_KEYWORD = 43,

        s57_BEFORE_DOCTYPE_PUBLIC_IDENTIFIER = 23,

        s58_DOCTYPE_PUBLIC_IDENTIFIER_DOUBLE_QUOTED = 24,

        s59_DOCTYPE_PUBLIC_IDENTIFIER_SINGLE_QUOTED = 25,

        s60_AFTER_DOCTYPE_PUBLIC_IDENTIFIER = 26,

        s61_BETWEEN_DOCTYPE_PUBLIC_AND_SYSTEM_IDENTIFIERS = 44,

        s62_AFTER_DOCTYPE_SYSTEM_KEYWORD = 45,

        s63_BEFORE_DOCTYPE_SYSTEM_IDENTIFIER = 27,

        s64_DOCTYPE_SYSTEM_IDENTIFIER_DOUBLE_QUOTED = 28,

        s65_DOCTYPE_SYSTEM_IDENTIFIER_SINGLE_QUOTED = 29,

        s66_AFTER_DOCTYPE_SYSTEM_IDENTIFIER = 30,

        s67_BOGUS_DOCTYPE = 31,

        s68_CDATA_SECTION = 56,

        NON_DATA_END_TAG_NAME = 38,

        MARKUP_DECLARATION_HYPHEN = 39,

        MARKUP_DECLARATION_OCTYPE = 40,

        DOCTYPE_UBLIC = 41,

        DOCTYPE_YSTEM = 42,
          
        CONSUME_CHARACTER_REFERENCE = 46,
         
        CONSUME_NCR = 47, //ncr->numeric character reference

        CHARACTER_REFERENCE_TAIL = 48,

        HEX_NCR_LOOP = 49,//ncr -> numeric character reference

        DECIMAL_NRC_LOOP = 50,

        HANDLE_NCR_VALUE = 51,

        HANDLE_NCR_VALUE_RECONSUME = 52,

        CHARACTER_REFERENCE_HILO_LOOKUP = 53,
         
        CDATA_START = 55,

        CDATA_RSQB = 57,

        CDATA_RSQB_RSQB = 58,

        BOGUS_COMMENT_HYPHEN = 64,
             

        PROCESSING_INSTRUCTION = 73,

        PROCESSING_INSTRUCTION_QUESTION_MARK = 74
    }

    /// <summary>
    /// An implementation of
    /// http://www.whatwg.org/specs/web-apps/current-work/multipage/tokenization.html     
    /// By default, the tokenizer may report data that XML 1.0 bans. The tokenizer
    /// can be configured to treat these conditions as fatal or to coerce the infoset
    /// to something that XML 1.0 allows.
    /// </summary>
    public sealed partial class Tokenizer
    {
        const byte DATA_AND_RCDATA_MASK = (byte)0xF0;
        /// <summary>
        /// Magic value for UTF-16 operations.
        /// </summary>
        const int LEAD_OFFSET = (0xD800 - (0x10000 >> 10));

        /// <summary>
        /// UTF-16 code unit array containing less than and greater than for emitting
        /// those characters on certain parse errors.
        /// </summary>
        static readonly char[] LT_GT = { '<', '>' };

        /// <summary>
        /// UTF-16 code unit array containing less than and solidus for emitting
        /// those characters on certain parse errors.
        /// </summary>
        static readonly char[] LT_SOLIDUS = { '<', '/' };

        /// <summary>
        /// UTF-16 code unit array containing ]] for emitting those characters on
        /// state transitions.
        /// </summary>
        static readonly char[] RSQB_RSQB = { ']', ']' };

        /// <summary>
        /// Array version of U+FFFD.
        /// </summary>
        static readonly char[] REPLACEMENT_CHARACTER = { '\uFFFD' };

        // [NOCPP[

        /// <summary>
        /// Array version of space.
        /// </summary>
        static readonly char[] SPACE = { ' ' };

        // ]NOCPP]

        /// <summary>
        /// Array version of line feed.
        /// </summary>
        static readonly char[] LF = { '\n' };

        /// <summary>
        /// Buffer growth parameter.
        /// </summary>
        const int BUFFER_GROW_BY = 1024;

        /// <summary>
        /// "CDATA[" as <code>char[]</code>
        /// </summary>
        static readonly char[] CDATA_LSQB = "CDATA[".ToCharArray();

        /// <summary>
        /// "octype" as <code>char[]</code>
        /// </summary>
        static readonly char[] OCTYPE = "octype".ToCharArray();

        /// <summary>
        /// "ublic" as <code>char[]</code>
        /// </summary>
        static readonly char[] UBLIC = "ublic".ToCharArray();

        /// <summary>
        /// "ystem" as  <code>char[]</code>
        /// </summary>
        static readonly char[] YSTEM = "ystem".ToCharArray();
        static readonly char[] TITLE_ARR = "title".ToCharArray();
        static readonly char[] SCRIPT_ARR = "script".ToCharArray();
        static readonly char[] STYLE_ARR = "style".ToCharArray();
        static readonly char[] PLAINTEXT_ARR = "plaintext".ToCharArray();
        static readonly char[] XMP_ARR = "xmp".ToCharArray();
        static readonly char[] TEXTAREA_ARR = "textarea".ToCharArray();
        static readonly char[] IFRAME_ARR = "iframe".ToCharArray();
        static readonly char[] NOEMBED_ARR = "noembed".ToCharArray();
        static readonly char[] NOSCRIPT_ARR = "noscript".ToCharArray();
        static readonly char[] NOFRAMES_ARR = "noframes".ToCharArray();

        public ITokenListener TokenListener { get; private set; }


        public event EventHandler<EncodingDetectedEventArgs> EncodingDeclared;


        public event EventHandler<ParserErrorEventArgs> ErrorEvent;



        /// <summary>
        /// Whether the previous char read was CR.
        /// </summary>
        bool lastCR;

        TokenizerState stateSave;

        TokenizerState returnStateSave;

        int index;

        bool forceQuirks;

        char additional;

        int entCol;

        int firstCharKey;

        int lo;

        int hi;

        int candidate;

        int strBufMark;

        int prevValue;

        int value;

        bool seenDigits;

        int cstart;



        /// <summary>
        ///  Buffer for short identifiers.
        /// </summary>
        StringBuilder strBuffer = new StringBuilder();
        /// <summary>
        /// buffer for long string
        /// </summary>
        StringBuilder longStrBuffer = new StringBuilder();

        readonly char[] bmpChar;

        /**
         * Buffer for expanding astral NCRs.
         */
        readonly char[] astralChar;

        /**
         * The element whose end tag closes the current CDATA or RCDATA element.
         */
        ElementName endTagExpectation = null;

        char[] endTagExpectationAsArray; // not @Auto!

        /**
         * <code>true</code> if tokenizing an end tag
         */
        bool endTag;

        /**
         * The current tag token name.
         */
        ElementName tagName = null;

        /// <summary>
        /// The current attribute name.
        /// </summary>
        AttributeName attributeName = null;

        // [NOCPP[

        /// <summary>
        /// Whether comment tokens are emitted.
        /// </summary>
        bool wantsComments = false;

        /// <summary>
        /// true when HTML4-specific additional errors are requested
        /// </summary>
        bool html4;

        /// <summary>
        /// Whether the stream is past the first 512 bytes.
        /// </summary>
        bool metaBoundaryPassed;

        // ]NOCPP]

        /**
         * The name of the current doctype token.
         */
        [Local]
        string doctypeName;

        /**
         * The public id of the current doctype token.
         */
        string publicIdentifier;

        /**
         * The system id of the current doctype token.
         */
        string systemIdentifier;

        /**
         * The attribute holder.
         */
        HtmlAttributes attributes;

        // [NOCPP[

        /**
         * The policy for vertical tab and form feed.
         */
        XmlViolationPolicy contentSpacePolicy = XmlViolationPolicy.AlterInfoset;


        XmlViolationPolicy commentPolicy = XmlViolationPolicy.AlterInfoset;

        XmlViolationPolicy xmlnsPolicy = XmlViolationPolicy.AlterInfoset;

        XmlViolationPolicy namePolicy = XmlViolationPolicy.AlterInfoset;

        bool html4ModeCompatibleWithXhtml1Schemata;

        readonly bool newAttributesEachTime;

        // ]NOCPP]

        int mappingLangToXmlLang;

        bool shouldSuspend;

        int line;

        // [NOCPP[ 
        //Location ampersandLocation;

        public Tokenizer(ITokenListener tokenHandler, bool newAttributesEachTime)
        {
            this.TokenListener = tokenHandler;
            this.newAttributesEachTime = newAttributesEachTime;
            this.bmpChar = new char[1];
            this.astralChar = new char[2];
            this.tagName = null;
            this.attributeName = null;
            this.doctypeName = null;
            this.publicIdentifier = null;
            this.systemIdentifier = null;
            this.attributes = null;
        }
        public bool IsMappingLangToXmlLang
        {
            get
            {
                return mappingLangToXmlLang == AttributeName.HTML_LANG;
            }
            set
            {
                this.mappingLangToXmlLang = value ? AttributeName.HTML_LANG : AttributeName.HTML;
            }
        }

        public XmlViolationPolicy CommentPolicy
        {
            get
            {
                return this.commentPolicy;
            }
            set
            {
                this.commentPolicy = value;
            }
        }

        public XmlViolationPolicy ContentNonXmlCharPolicy
        {
            set
            {
                if (value != XmlViolationPolicy.Allow)
                {
                    throw new ArgumentException("Must use ErrorReportingTokenizer to set contentNonXmlCharPolicy to non-ALLOW.");
                }
            }
        }
        public XmlViolationPolicy ContentSpacePolicy
        {
            get
            {
                return this.contentSpacePolicy;
            }
            set
            {
                this.contentSpacePolicy = value;
            }
        }

        public XmlViolationPolicy XmlnsPolicy
        {
            get
            {
                return this.xmlnsPolicy;
            }
            set
            {
                if (value == XmlViolationPolicy.Fatal)
                {
                    throw new ArgumentException("Can't use FATAL here.");
                }
                this.xmlnsPolicy = value;
            }
        }

        public XmlViolationPolicy NamePolicy
        {
            get
            {
                return this.namePolicy;
            }
            set
            {
                this.namePolicy = value;
            }
        }
        /// <summary>
        ///   the html4ModeCompatibleWithXhtml1Schemata.
        /// </summary>
        public bool Html4ModeCompatibleWithXhtml1Schemata
        {
            get
            {
                return this.html4ModeCompatibleWithXhtml1Schemata;
            }
            set
            {
                this.html4ModeCompatibleWithXhtml1Schemata = value;
            }
        }

        // ]NOCPP]

        // For the token handler to call
        /**
         * Sets the tokenizer state and the associated element name. This should 
         * only ever used to put the tokenizer into one of the states that have
         * a special end tag expectation.
         * 
         * @param specialTokenizerState
         *            the tokenizer state to set
         * @param endTagExpectation
         *            the expected end tag for transitioning back to normal
         */
        public void SetStateAndEndTagExpectation(TokenizerState specialTokenizerState,
                [Local] String endTagExpectation)
        {
            this.stateSave = specialTokenizerState;
            if (specialTokenizerState == TokenizerState.s01_DATA)
            {
                return;
            }
            this.endTagExpectation = ElementName.ElementNameByBuffer(endTagExpectation.ToCharArray());
            EndTagExpectationToArray();
        }

        /**
         * Sets the tokenizer state and the associated element name. This should 
         * only ever used to put the tokenizer into one of the states that have
         * a special end tag expectation.
         * 
         * @param specialTokenizerState
         *            the tokenizer state to set
         * @param endTagExpectation
         *            the expected end tag for transitioning back to normal
         */
        public void SetStateAndEndTagExpectation(TokenizerState specialTokenizerState,
                ElementName endTagExpectation)
        {
            this.stateSave = specialTokenizerState;
            this.endTagExpectation = endTagExpectation;
            EndTagExpectationToArray();
        }

        private void EndTagExpectationToArray()
        {
            switch (endTagExpectation.Group)
            {
                case DispatchGroup.TITLE:
                    endTagExpectationAsArray = TITLE_ARR;
                    return;
                case DispatchGroup.SCRIPT:
                    endTagExpectationAsArray = SCRIPT_ARR;
                    return;
                case DispatchGroup.STYLE:
                    endTagExpectationAsArray = STYLE_ARR;
                    return;
                case DispatchGroup.PLAINTEXT:
                    endTagExpectationAsArray = PLAINTEXT_ARR;
                    return;
                case DispatchGroup.XMP:
                    endTagExpectationAsArray = XMP_ARR;
                    return;
                case DispatchGroup.TEXTAREA:
                    endTagExpectationAsArray = TEXTAREA_ARR;
                    return;
                case DispatchGroup.IFRAME:
                    endTagExpectationAsArray = IFRAME_ARR;
                    return;
                case DispatchGroup.NOEMBED:
                    endTagExpectationAsArray = NOEMBED_ARR;
                    return;
                case DispatchGroup.NOSCRIPT:
                    endTagExpectationAsArray = NOSCRIPT_ARR;
                    return;
                case DispatchGroup.NOFRAMES:
                    endTagExpectationAsArray = NOFRAMES_ARR;
                    return;
                default:
                    Debug.Assert(false, "Bad end tag expectation.");
                    return;
            }
        }
        public int LineNumber
        {
            get
            {
                return line;
            }
        }
        public int ColumnNumber
        {
            get
            {
                return -1;
            }
        }



        public void NotifyAboutMetaBoundary()
        {
            metaBoundaryPassed = true;
        }

        // end of public API

        internal void TurnOnAdditionalHtml4Errors()
        {
            html4 = true;
        }

        internal HtmlAttributes EmptyAttributes()
        {
            // [NOCPP[
            if (newAttributesEachTime)
            {
                return new HtmlAttributes(mappingLangToXmlLang);
            }
            else
            {
                // ]NOCPP]
                return HtmlAttributes.EMPTY_ATTRIBUTES;
                // [NOCPP[
            }
            // ]NOCPP]
        }

        /*@Inline*/
        void ClearStrBufAndAppend(char c)
        {
            this.strBuffer.Length = 0;
            this.strBuffer.Append(c);

        }

        /*@Inline*/
        void ClearStrBuf()
        {
            this.strBuffer.Length = 0;
        }
        /// <summary>
        /// Appends to the smaller buffer.
        /// </summary>
        /// <param name="c"></param>
        void AppendStrBuf(char c)
        {
            this.strBuffer.Append(c);
        }

        /**
         * Returns the short buffer as a local name. The return value is released in
         * emitDoctypeToken().
         * 
         * @return the smaller buffer as local name
         */
        void StrBufToDoctypeName()
        {
            doctypeName = Portability.NewLocalNameFromBuffer(this.strBuffer.ToString());
        }
        /*@Inline*/
        void ClearLongStrBuf()
        {

            longStrBuffer.Length = 0;
        }

        /*@Inline*/
        private void ClearLongStrBufAndAppend(char c)
        {
            longStrBuffer.Length = 0;
            longStrBuffer.Append(c);
        }

        /**
         * Appends to the larger buffer.
         * 
         * @param c
         *            the UTF-16 code unit to append
         */
        void AppendLongStrBuf(char c)
        {
            this.longStrBuffer.Append(c);
        }
        void AppendLongStrBuf(StringBuilder stBuilder)
        {
            this.longStrBuffer.Append(stBuilder.ToString());
        }
        /*@Inline*/
        void AppendSecondHyphenToBogusComment()
        {
            // [NOCPP[
            switch (commentPolicy)
            {
                case XmlViolationPolicy.AlterInfoset:
                    // detachLongStrBuf();
                    AppendLongStrBuf(' ');
                    // FALLTHROUGH
                    goto case XmlViolationPolicy.Allow;
                case XmlViolationPolicy.Allow:
                    Warn("The document is not mappable to XML 1.0 due to two consecutive hyphens in a comment.");
                    // ]NOCPP]
                    AppendLongStrBuf('-');
                    // [NOCPP[
                    break;
                case XmlViolationPolicy.Fatal:
                    Fatal("The document is not mappable to XML 1.0 due to two consecutive hyphens in a comment.");
                    break;
            }
            // ]NOCPP]
        }

        // [NOCPP[
        void MaybeAppendSpaceToBogusComment()
        {
            switch (commentPolicy)
            {
                case XmlViolationPolicy.AlterInfoset:
                    // detachLongStrBuf();
                    AppendLongStrBuf(' ');
                    // FALLTHROUGH
                    goto case XmlViolationPolicy.Allow;
                case XmlViolationPolicy.Allow:
                    Warn("The document is not mappable to XML 1.0 due to a trailing hyphen in a comment.");
                    break;
                case XmlViolationPolicy.Fatal:
                    Fatal("The document is not mappable to XML 1.0 due to a trailing hyphen in a comment.");
                    break;
            }
        }

        // ]NOCPP]

        /*@Inline*/
        void AdjustDoubleHyphenAndAppendToLongStrBufAndErr(char c)
        {
            ErrConsecutiveHyphens();
            // [NOCPP[
            switch (commentPolicy)
            {
                case XmlViolationPolicy.AlterInfoset:
                    // detachLongStrBuf();
                    longStrBuffer.Length--;
                    AppendLongStrBuf(' ');
                    AppendLongStrBuf('-');

                    // FALLTHROUGH
                    goto case XmlViolationPolicy.Allow;
                case XmlViolationPolicy.Allow:
                    Warn("The document is not mappable to XML 1.0 due to two consecutive hyphens in a comment.");
                    // ]NOCPP]
                    AppendLongStrBuf(c);
                    // [NOCPP[
                    break;
                case XmlViolationPolicy.Fatal:
                    Fatal("The document is not mappable to XML 1.0 due to two consecutive hyphens in a comment.");
                    break;
            }
            // ]NOCPP]
        }

        void AppendLongStrBuf(char[] buffer, int offset, int length)
        {

            this.longStrBuffer.Append(buffer, offset, length);
        }

        /// <summary>
        /// Append the contents of the smaller buffer to the larger one.
        /// </summary>
        void AppendStrBufToLongStrBuf()
        {
            /*@Inline*/

            AppendLongStrBuf(this.strBuffer);
        }

        /**
         * The larger buffer as a string.
         * 
         * <p>
         * C++ memory note: The return value must be released.
         * 
         * @return the larger buffer as a string
         */
        string LongStrBufToString()
        {
            return this.longStrBuffer.ToString();
        }

        /// <summary>
        /// Flushes coalesced character tokens.
        /// </summary>
        /// <param name="buf">The buffer.</param>
        /// <param name="pos">The position.</param>
        void FlushChars(char[] buf, int pos)
        {
            if (pos > cstart)
            {
                TokenListener.Characters(buf, cstart, pos - cstart);
            }
            cstart = int.MaxValue;
        }
        void FlushChars(TextSnapshotReader reader)
        {
            if (reader.Position > cstart)
            {
                TokenListener.Characters(reader.InteralBuff, cstart, reader.Position - cstart);
            }
            cstart = int.MaxValue;
        }
        /**
         * Reports an condition that would make the infoset incompatible with XML
         * 1.0 as fatal.
         * 
         * @param message
         *            the message
         * @throws SAXException
         * @throws SAXParseException
         */
        public void Fatal(string message)
        {
            /*SAXParseException spe = new SAXParseException(message, this);
            if (errorHandler != null) {
                errorHandler.fatalError(spe);
            }
            throw spe;*/
            throw new Exception(message); // TODO
        }

        /**
         * Reports a Parse Error.
         * 
         * @param message
         *            the message
         * @throws SAXException
         */
        public void Err(string message)
        {
            if (ErrorEvent == null)
            {
                return;
            }
            ErrorEvent(this, new ParserErrorEventArgs(message, false));
        }

        public void ErrTreeBuilder(string message)
        {
            /*ErrorHandler eh = null;
            if (tokenHandler is TreeBuilder<T>) {
                TreeBuilder<?> treeBuilder = (TreeBuilder<?>) tokenHandler;
                eh = treeBuilder.getErrorHandler();
            }
            if (eh == null) {
                eh = errorHandler;
            }
            if (eh == null) {
                return;
            }
            SAXParseException spe = new SAXParseException(message, this);
            eh.error(spe);*/
            Err(message); // TODO
        }

        /**
         * Reports a warning
         * 
         * @param message
         *            the message
         * @throws SAXException
         */
        public void Warn(string message)
        {
            if (ErrorEvent == null)
            {
                return;
            }
            ErrorEvent(this, new ParserErrorEventArgs(message, true));
        }

        /**
         * 
         */
        private void ResetAttributes()
        {
            // [NOCPP[
            if (newAttributesEachTime)
            {
                // ]NOCPP]
                attributes = null;
                // [NOCPP[
            }
            else
            {
                attributes.Clear(mappingLangToXmlLang);
            }
            // ]NOCPP]
        }

        void StrBufToElementNameString()
        {
            // if (strBufOffset != -1) {
            // return ElementName.elementNameByBuffer(buf, strBufOffset, strBufLen);
            // } else {

            tagName = ElementName.ElementNameByBuffer(CopyFromStringBuiler(strBuffer, 0, this.strBuffer.Length));
            // }
        }


        void AttributeNameComplete()
        {
            // if (strBufOffset != -1) {
            // attributeName = AttributeName.nameByBuffer(buf, strBufOffset,
            // strBufLen, namePolicy != XmlViolationPolicy.ALLOW);
            // } else {
            char[] copyBuffer = CopyFromStringBuiler(this.strBuffer, 0, this.strBuffer.Length);
            attributeName = AttributeName.NameByBuffer(copyBuffer, 0, copyBuffer.Length
                    , namePolicy != XmlViolationPolicy.Allow
                    );
            // }

            if (attributes == null)
            {
                attributes = new HtmlAttributes(mappingLangToXmlLang);
            }

            /*
             * When the user agent leaves the attribute name state (and before
             * emitting the tag token, if appropriate), the complete attribute's
             * name must be compared to the other attributes on the same token; if
             * there is already an attribute on the token with the exact same name,
             * then this is a parse error and the new attribute must be dropped,
             * along with the value that gets associated with it (if any).
             */
            if (attributes.Contains(attributeName))
            {
                ErrDuplicateAttribute();
                attributeName = null;
            }
        }

        void AddAttributeWithoutValue()
        {
            NoteAttributeWithoutValue();

            // [NOCPP[
            if (metaBoundaryPassed && AttributeName.CHARSET == attributeName
                    && ElementName.META == tagName)
            {
                Err("A \u201Ccharset\u201D attribute on a \u201Cmeta\u201D element found after the first 512 bytes.");
            }
            // ]NOCPP]
            if (attributeName != null)
            {
                //TODO: not need to validate this on lexer
                //consider validate on the listenser side


                if (html4)
                {
                    if (attributeName.IsBoolean)
                    {
                        if (html4ModeCompatibleWithXhtml1Schemata)
                        {
                            attributes.AddAttribute(attributeName,
                                    attributeName.GetLocal(AttributeName.HTML),
                                    xmlnsPolicy);
                        }
                        else
                        {
                            attributes.AddAttribute(attributeName, "", xmlnsPolicy);
                        }
                    }
                    else
                    {
                        if (AttributeName.BORDER != attributeName)
                        {
                            Err("Attribute value omitted for a non-bool attribute. (HTML4-only error.)");
                            attributes.AddAttribute(attributeName, "", xmlnsPolicy);
                        }
                    }
                }
                else
                {
                    if (AttributeName.SRC == attributeName
                            || AttributeName.HREF == attributeName)
                    {
                        Warn("Attribute \u201C"
                                + attributeName.GetLocal(AttributeName.HTML)
                                + "\u201D without an explicit value seen. The attribute may be dropped by IE7.");
                    }
                    attributes.AddAttribute(attributeName,
                            String.Empty
                            , xmlnsPolicy
                    );
                }
                attributeName = null; // attributeName has been adopted by the
                // |attributes| object
            }
        }

        void AddAttributeWithValue()
        {
            // [NOCPP[
            if (metaBoundaryPassed && ElementName.META == tagName
                    && AttributeName.CHARSET == attributeName)
            {
                Err("A \u201Ccharset\u201D attribute on a \u201Cmeta\u201D element found after the first 512 bytes.");
            }
            // ]NOCPP]
            if (attributeName != null)
            {
                String val = LongStrBufToString(); // Ownership transferred to
                // HtmlAttributes

                // [NOCPP[
                if (!endTag && html4 && html4ModeCompatibleWithXhtml1Schemata
                        && attributeName.IsCaseFolded)
                {
                    val = NewAsciiLowerCaseStringFromString(val);
                }
                // ]NOCPP]
                attributes.AddAttribute(attributeName, val
                    // [NOCPP[
                        , xmlnsPolicy
                    // ]NOCPP]
                );
                attributeName = null; // attributeName has been adopted by the
                // |attributes| object
            }
        }

        // [NOCPP[

        static String NewAsciiLowerCaseStringFromString(String str)
        {
            if (str == null)
            {
                return null;
            }
            char[] buf = new char[str.Length];
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if (c >= 'A' && c <= 'Z')
                {
                    c += (char)0x20;
                }
                buf[i] = c;
            }
            return new String(buf);
        }


        // ]NOCPP]

        public void Start()
        {
            InitializeWithoutStarting();
            TokenListener.StartTokenization(this);
            // [NOCPP[
            StartErrorReporting();
            // ]NOCPP]
        }

        public bool TokenizeBuffer(UTF16Buffer buffer)
        {
            TokenizerState state = stateSave;
            TokenizerState returnState = returnStateSave;
            char c = '\u0000';
            shouldSuspend = false;
            lastCR = false;

            int start = buffer.Start;
            /**
             * The index of the last <code>char</code> read from <code>buf</code>.
             */
            int pos = start - 1;

            /**
             * The index of the first <code>char</code> in <code>buf</code> that is
             * part of a coalesced run of character tokens or
             * <code>Integer.MAX_VALUE</code> if there is not a current run being
             * coalesced.
             */
            switch (state)
            {
                case TokenizerState.s01_DATA:
                case TokenizerState.s03_RCDATA:
                case TokenizerState.s06_SCRIPT_DATA:
                case TokenizerState.s07_PLAINTEXT:
                case TokenizerState.s05_RAWTEXT:
                case TokenizerState.s68_CDATA_SECTION:
                case TokenizerState.s22_SCRIPT_DATA_ESCAPED:
                case TokenizerState.s20_SCRIPT_DATA_ESCAPE_START:
                case TokenizerState.s21_SCRIPT_DATA_ESCAPE_START_DASH:
                case TokenizerState.s23_SCRIPT_DATA_ESCAPED_DASH:
                case TokenizerState.s24_SCRIPT_DATA_ESCAPED_DASH_DASH:
                case TokenizerState.s28_SCRIPT_DATA_DOUBLE_ESCAPE_START:
                case TokenizerState.s29_SCRIPT_DATA_DOUBLE_ESCAPED:
                case TokenizerState.s32_SCRIPT_DATA_DOUBLE_ESCAPED_LESS_THAN_SIGN:
                case TokenizerState.s30_SCRIPT_DATA_DOUBLE_ESCAPED_DASH:
                case TokenizerState.s31_SCRIPT_DATA_DOUBLE_ESCAPED_DASH_DASH:
                case TokenizerState.s33_SCRIPT_DATA_DOUBLE_ESCAPE_END:
                    cstart = start;
                    break;
                default:
                    cstart = int.MaxValue;
                    break;
            }

            /**
             * The number of <code>char</code>s in <code>buf</code> that have
             * meaning. (The rest of the array is garbage and should not be
             * examined.)
             */

            // [NOCPP[
            pos = StateLoop(state, c, pos, buffer.Buffer, false, returnState, buffer.End);
            // ]NOCPP]
            if (pos == buffer.End)
            {
                // exiting due to end of buffer
                buffer.Start = pos;
            }
            else
            {
                buffer.Start = pos + 1;
            }
            return lastCR;
        }


        void InitDoctypeFields()
        {
            doctypeName = "";
            systemIdentifier = null;
            publicIdentifier = null;
            forceQuirks = false;
        }

        /*@Inline*/
        void AdjustDoubleHyphenAndAppendToLongStrBufCarriageReturn()
        {
            SilentCarriageReturn();
            AdjustDoubleHyphenAndAppendToLongStrBufAndErr('\n');
        }

        /*@Inline*/
        void AdjustDoubleHyphenAndAppendToLongStrBufLineFeed()
        {
            SilentLineFeed();
            AdjustDoubleHyphenAndAppendToLongStrBufAndErr('\n');
        }

        /*@Inline*/
        void AppendLongStrBufLineFeed()
        {
            SilentLineFeed();
            AppendLongStrBuf('\n');
        }

        /*@Inline*/
        void AppendLongStrBufCarriageReturn()
        {
            SilentCarriageReturn();
            AppendLongStrBuf('\n');
        }

        /*@Inline*/
        void SilentCarriageReturn()
        {
            ++line;
            lastCR = true;
        }
        /*@Inline*/
        void SilentLineFeed()
        {
            ++line;
        }
        void SetAdditionalAndRememberAmpersandLocation(char add)
        {
            additional = add;
            // [NOCPP[
            //ampersandLocation = new Location(this.LineNumber, this.ColumnNumber);
            // ]NOCPP]
        }
        void BogusDoctype()
        {
            ErrBogusDoctype();
            forceQuirks = true;
        }

        void BogusDoctypeWithoutQuirks()
        {
            ErrBogusDoctype();
            forceQuirks = false;
        }


        void HandleNcrValue(TokenizerState returnState)
        {
            /*
             * If one or more characters match the range, then take them all and
             * interpret the string of characters as a number (either hexadecimal or
             * decimal as appropriate).
             */
            if (value <= 0xFFFF)
            {
                if (value >= 0x80 && value <= 0x9f)
                {
                    /*
                     * If that number is one of the numbers in the first column of
                     * the following table, then this is a parse error.
                     */
                    ErrNcrInC1Range();
                    /*
                     * Find the row with that number in the first column, and return
                     * a character token for the Unicode character given in the
                     * second column of that row.
                     */
                    char[] val = NamedCharacters.WINDOWS_1252[value - 0x80];
                    EmitOrAppendOne(val, returnState);
                    // [NOCPP[
                }
                else if (value == 0xC
                      && contentSpacePolicy != XmlViolationPolicy.Allow)
                {
                    if (contentSpacePolicy == XmlViolationPolicy.AlterInfoset)
                    {
                        EmitOrAppendOne(SPACE, returnState);
                    }
                    else if (contentSpacePolicy == XmlViolationPolicy.Fatal)
                    {
                        Fatal("A character reference expanded to a form feed which is not legal XML 1.0 white space.");
                    }
                    // ]NOCPP]
                }
                else if (value == 0x0)
                {
                    ErrNcrZero();
                    EmitOrAppendOne(REPLACEMENT_CHARACTER, returnState);
                }
                else if ((value & 0xF800) == 0xD800)
                {
                    ErrNcrSurrogate();
                    EmitOrAppendOne(Tokenizer.REPLACEMENT_CHARACTER, returnState);
                }
                else
                {
                    /*
                     * Otherwise, return a character token for the Unicode character
                     * whose code point is that number.
                     */
                    char ch = (char)value;
                    // [NOCPP[
                    if (value == 0x0D)
                    {
                        ErrNcrCr();
                    }
                    else if ((value <= 0x0008) || (value == 0x000B)
                          || (value >= 0x000E && value <= 0x001F))
                    {
                        ch = ErrNcrControlChar(ch);
                    }
                    else if (value >= 0xFDD0 && value <= 0xFDEF)
                    {
                        ErrNcrUnassigned();
                    }
                    else if ((value & 0xFFFE) == 0xFFFE)
                    {
                        ch = ErrNcrNonCharacter(ch);
                    }
                    else if (value >= 0x007F && value <= 0x009F)
                    {
                        ErrNcrControlChar();
                    }
                    else
                    {
                        MaybeWarnPrivateUse(ch);
                    }
                    // ]NOCPP]
                    bmpChar[0] = ch;
                    EmitOrAppendOne(bmpChar, returnState);
                }
            }
            else if (value <= 0x10FFFF)
            {
                // [NOCPP[
                MaybeWarnPrivateUseAstral();
                if ((value & 0xFFFE) == 0xFFFE)
                {
                    ErrAstralNonCharacter(value);
                }
                // ]NOCPP]
                astralChar[0] = (char)(LEAD_OFFSET + (value >> 10));
                astralChar[1] = (char)(0xDC00 + (value & 0x3FF));
                EmitOrAppendTwo(astralChar, returnState);
            }
            else
            {
                ErrNcrOutOfRange();
                EmitOrAppendOne(REPLACEMENT_CHARACTER, returnState);
            }
        }

        public void Eof()
        {
            TokenizerState state = stateSave;
            TokenizerState returnState = returnStateSave;

            /*eofloop:*/
            for (; ; )
            {
                switch (state)
                {
                    case TokenizerState.s17_SCRIPT_DATA_LESS_THAN_SIGN:
                    case TokenizerState.s25_SCRIPT_DATA_ESCAPED_LESS_THAN_SIGN:
                        /*
                         * Otherwise, emit a U+003C LESS-THAN SIGN character token
                         */
                        TokenListener.Characters(LT_GT, 0, 1);
                        /*
                         * and reconsume the current input character in the data
                         * state.
                         */
                        goto breakEofloop;
                    case TokenizerState.s08_TAG_OPEN:
                        /*
                         * The behavior of this state depends on the content model
                         * flag.
                         */
                        /*
                         * Anything else Parse error.
                         */
                        ErrEofAfterLt();
                        /*
                         * Emit a U+003C LESS-THAN SIGN character token
                         */
                        TokenListener.Characters(LT_GT, 0, 1);
                        /*
                         * and reconsume the current input character in the data
                         * state.
                         */
                        goto breakEofloop;
                    case TokenizerState.s11_RAWTEXT_RCDATA_LESS_THAN_SIGN:
                        /*
                         * Emit a U+003C LESS-THAN SIGN character token
                         */
                        TokenListener.Characters(LT_GT, 0, 1);
                        /*
                         * and reconsume the current input character in the RCDATA
                         * state.
                         */
                        goto breakEofloop;
                    case TokenizerState.NON_DATA_END_TAG_NAME:
                        /*
                         * Emit a U+003C LESS-THAN SIGN character token, a U+002F
                         * SOLIDUS character token,
                         */
                        TokenListener.Characters(LT_SOLIDUS, 0, 2);
                        /*
                         * a character token for each of the characters in the
                         * temporary buffer (in the order they were added to the
                         * buffer),
                         */
                        EmitStrBuf();
                        /*
                         * and reconsume the current input character in the RCDATA
                         * state.
                         */
                        goto breakEofloop;
                    case TokenizerState.s09_CLOSE_TAG_OPEN:
                        /* EOF Parse error. */
                        ErrEofAfterLt();
                        /*
                         * Emit a U+003C LESS-THAN SIGN character token and a U+002F
                         * SOLIDUS character token.
                         */
                        TokenListener.Characters(LT_SOLIDUS, 0, 2);
                        /*
                         * Reconsume the EOF character in the data state.
                         */
                        goto breakEofloop;
                    case TokenizerState.s10_TAG_NAME:
                        /*
                         * EOF Parse error.
                         */
                        ErrEofInTagName();
                        /*
                         * Reconsume the EOF character in the data state.
                         */
                        goto breakEofloop;
                    case TokenizerState.s34_BEFORE_ATTRIBUTE_NAME:
                    case TokenizerState.s42__AFTER_ATTRIBUTE_VALUE_QUOTED:
                    case TokenizerState.s43_SELF_CLOSING_START_TAG:
                        /* EOF Parse error. */
                        ErrEofWithoutGt();
                        /*
                         * Reconsume the EOF character in the data state.
                         */
                        goto breakEofloop;
                    case TokenizerState.s35_ATTRIBUTE_NAME:
                        /*
                         * EOF Parse error.
                         */
                        ErrEofInAttributeName();
                        /*
                         * Reconsume the EOF character in the data state.
                         */
                        goto breakEofloop;
                    case TokenizerState.s36_AFTER_ATTRIBUTE_NAME:
                    case TokenizerState.s37_BEFORE_ATTRIBUTE_VALUE:
                        /* EOF Parse error. */
                        ErrEofWithoutGt();
                        /*
                         * Reconsume the EOF character in the data state.
                         */
                        goto breakEofloop;
                    case TokenizerState.s38_ATTRIBUTE_VALUE_DOUBLE_QUOTED:
                    case TokenizerState.s39_ATTRIBUTE_VALUE_SINGLE_QUOTED:
                    case TokenizerState.s40_ATTRIBUTE_VALUE_UNQUOTED:
                        /* EOF Parse error. */
                        ErrEofInAttributeValue();
                        /*
                         * Reconsume the EOF character in the data state.
                         */
                        goto breakEofloop;
                    case TokenizerState.s44_BOGUS_COMMENT:
                        EmitComment(0, 0);
                        goto breakEofloop;
                    case TokenizerState.BOGUS_COMMENT_HYPHEN:
                        // [NOCPP[
                        MaybeAppendSpaceToBogusComment();
                        // ]NOCPP]
                        EmitComment(0, 0);
                        goto breakEofloop;
                    case TokenizerState.s45_MARKUP_DECLARATION_OPEN:
                        ErrBogusComment();
                        ClearLongStrBuf();
                        EmitComment(0, 0);
                        goto breakEofloop;
                    case TokenizerState.MARKUP_DECLARATION_HYPHEN:
                        ErrBogusComment();
                        EmitComment(0, 0);
                        goto breakEofloop;
                    case TokenizerState.MARKUP_DECLARATION_OCTYPE:
                        if (index < 6)
                        {
                            ErrBogusComment();
                            EmitComment(0, 0);
                        }
                        else
                        {
                            /* EOF Parse error. */
                            ErrEofInDoctype();
                            /*
                             * Create a new DOCTYPE token. Set its force-quirks flag
                             * to on.
                             */
                            doctypeName = "";
                            if (systemIdentifier != null)
                            {
                                systemIdentifier = null;
                            }
                            if (publicIdentifier != null)
                            {
                                publicIdentifier = null;
                            }
                            forceQuirks = true;
                            /*
                             * Emit the token.
                             */
                            EmitDoctypeToken(0);
                            /*
                             * Reconsume the EOF character in the data state.
                             */
                            goto breakEofloop;
                        }
                        goto breakEofloop;
                    case TokenizerState.s46_COMMENT_START:
                    case TokenizerState.s48_COMMENT:
                        /*
                         * EOF Parse error.
                         */
                        ErrEofInComment();
                        /* Emit the comment token. */
                        EmitComment(0, 0);
                        /*
                         * Reconsume the EOF character in the data state.
                         */
                        goto breakEofloop;
                    case TokenizerState.s50_COMMENT_END:
                        ErrEofInComment();
                        /* Emit the comment token. */
                        EmitComment(2, 0);
                        /*
                         * Reconsume the EOF character in the data state.
                         */
                        goto breakEofloop;
                    case TokenizerState.s49_COMMENT_END_DASH:
                    case TokenizerState.s47_COMMENT_START_DASH:
                        ErrEofInComment();
                        /* Emit the comment token. */
                        EmitComment(1, 0);
                        /*
                         * Reconsume the EOF character in the data state.
                         */
                        goto breakEofloop;
                    case TokenizerState.s51_COMMENT_END_BANG:
                        ErrEofInComment();
                        /* Emit the comment token. */
                        EmitComment(3, 0);
                        /*
                         * Reconsume the EOF character in the data state.
                         */
                        goto breakEofloop;
                    case TokenizerState.s52_DOCTYPE:
                    case TokenizerState.s53_BEFORE_DOCTYPE_NAME:
                        ErrEofInDoctype();
                        /*
                         * Create a new DOCTYPE token. Set its force-quirks flag to
                         * on.
                         */
                        forceQuirks = true;
                        /*
                         * Emit the token.
                         */
                        EmitDoctypeToken(0);
                        /*
                         * Reconsume the EOF character in the data state.
                         */
                        goto breakEofloop;
                    case TokenizerState.s54_DOCTYPE_NAME:
                        ErrEofInDoctype();
                        StrBufToDoctypeName();
                        /*
                         * Set the DOCTYPE token's force-quirks flag to on.
                         */
                        forceQuirks = true;
                        /*
                         * Emit that DOCTYPE token.
                         */
                        EmitDoctypeToken(0);
                        /*
                         * Reconsume the EOF character in the data state.
                         */
                        goto breakEofloop;
                    case TokenizerState.DOCTYPE_UBLIC:
                    case TokenizerState.DOCTYPE_YSTEM:
                    case TokenizerState.s55_AFTER_DOCTYPE_NAME:
                    case TokenizerState.s56_AFTER_DOCTYPE_PUBLIC_KEYWORD:
                    case TokenizerState.s62_AFTER_DOCTYPE_SYSTEM_KEYWORD:
                    case TokenizerState.s57_BEFORE_DOCTYPE_PUBLIC_IDENTIFIER:
                        ErrEofInDoctype();
                        /*
                         * Set the DOCTYPE token's force-quirks flag to on.
                         */
                        forceQuirks = true;
                        /*
                         * Emit that DOCTYPE token.
                         */
                        EmitDoctypeToken(0);
                        /*
                         * Reconsume the EOF character in the data state.
                         */
                        goto breakEofloop;
                    case TokenizerState.s58_DOCTYPE_PUBLIC_IDENTIFIER_DOUBLE_QUOTED:
                    case TokenizerState.s59_DOCTYPE_PUBLIC_IDENTIFIER_SINGLE_QUOTED:
                        /* EOF Parse error. */
                        ErrEofInPublicId();
                        /*
                         * Set the DOCTYPE token's force-quirks flag to on.
                         */
                        forceQuirks = true;
                        /*
                         * Emit that DOCTYPE token.
                         */
                        publicIdentifier = LongStrBufToString();
                        EmitDoctypeToken(0);
                        /*
                         * Reconsume the EOF character in the data state.
                         */
                        goto breakEofloop;
                    case TokenizerState.s60_AFTER_DOCTYPE_PUBLIC_IDENTIFIER:
                    case TokenizerState.s63_BEFORE_DOCTYPE_SYSTEM_IDENTIFIER:
                    case TokenizerState.s61_BETWEEN_DOCTYPE_PUBLIC_AND_SYSTEM_IDENTIFIERS:
                        ErrEofInDoctype();
                        /*
                         * Set the DOCTYPE token's force-quirks flag to on.
                         */
                        forceQuirks = true;
                        /*
                         * Emit that DOCTYPE token.
                         */
                        EmitDoctypeToken(0);
                        /*
                         * Reconsume the EOF character in the data state.
                         */
                        goto breakEofloop;
                    case TokenizerState.s64_DOCTYPE_SYSTEM_IDENTIFIER_DOUBLE_QUOTED:
                    case TokenizerState.s65_DOCTYPE_SYSTEM_IDENTIFIER_SINGLE_QUOTED:
                        /* EOF Parse error. */
                        ErrEofInSystemId();
                        /*
                         * Set the DOCTYPE token's force-quirks flag to on.
                         */
                        forceQuirks = true;
                        /*
                         * Emit that DOCTYPE token.
                         */
                        systemIdentifier = LongStrBufToString();
                        EmitDoctypeToken(0);
                        /*
                         * Reconsume the EOF character in the data state.
                         */
                        goto breakEofloop;
                    case TokenizerState.s66_AFTER_DOCTYPE_SYSTEM_IDENTIFIER:
                        ErrEofInDoctype();
                        /*
                         * Set the DOCTYPE token's force-quirks flag to on.
                         */
                        forceQuirks = true;
                        /*
                         * Emit that DOCTYPE token.
                         */
                        EmitDoctypeToken(0);
                        /*
                         * Reconsume the EOF character in the data state.
                         */
                        goto breakEofloop;
                    case TokenizerState.s67_BOGUS_DOCTYPE:
                        /*
                         * Emit that DOCTYPE token.
                         */
                        EmitDoctypeToken(0);
                        /*
                         * Reconsume the EOF character in the data state.
                         */
                        goto breakEofloop;
                    case TokenizerState.CONSUME_CHARACTER_REFERENCE:
                        /*
                         * Unlike the definition is the spec, this state does not
                         * return a value and never requires the caller to
                         * backtrack. This state takes care of emitting characters
                         * or appending to the current attribute value. It also
                         * takes care of that in the case TokenizerState.when consuming the entity
                         * fails.
                         */
                        /*
                         * This section defines how to consume an entity. This
                         * definition is used when parsing entities in text and in
                         * attributes.
                         * 
                         * The behavior depends on the identity of the next
                         * character (the one immediately after the U+0026 AMPERSAND
                         * character):
                         */

                        EmitOrAppendStrBuf(returnState);
                        state = returnState;
                        continue;
                    case TokenizerState.CHARACTER_REFERENCE_HILO_LOOKUP:
                        ErrNoNamedCharacterMatch();
                        EmitOrAppendStrBuf(returnState);
                        state = returnState;
                        continue;
                    case TokenizerState.CHARACTER_REFERENCE_TAIL:
                        /*outer:*/
                        for (; ; )
                        {
                            char c = '\u0000';
                            entCol++;
                            /*
                             * Consume the maximum number of characters possible,
                             * with the consumed characters matching one of the
                             * identifiers in the first column of the named
                             * character references table (in a case-sensitive
                             * manner).
                             */
                            /*hiloop:*/
                            for (; ; )
                            {
                                if (hi == -1)
                                {
                                    goto breakHiloop;
                                }
                                if (entCol == NamedCharacters.NAMES[hi].Length)
                                {
                                    goto breakHiloop;
                                }
                                if (entCol > NamedCharacters.NAMES[hi].Length)
                                {
                                    goto breakOuter;
                                }
                                else if (c < NamedCharacters.NAMES[hi][entCol])
                                {
                                    hi--;
                                }
                                else
                                {
                                    goto breakHiloop;
                                }
                            }

                        breakHiloop:

                            /*loloop:*/
                            for (; ; )
                            {
                                if (hi < lo)
                                {
                                    goto breakOuter;
                                }
                                if (entCol == NamedCharacters.NAMES[lo].Length)
                                {
                                    candidate = lo;
                                    strBufMark = this.strBuffer.Length;
                                    lo++;
                                }
                                else if (entCol > NamedCharacters.NAMES[lo].Length)
                                {
                                    goto breakOuter;
                                }
                                else if (c > NamedCharacters.NAMES[lo][entCol])
                                {
                                    lo++;
                                }
                                else
                                {
                                    goto breakLoloop;
                                }
                            }

                        breakLoloop:

                            if (hi < lo)
                            {
                                goto breakOuter;
                            }
                            continue;
                        }

                    breakOuter:

                        if (candidate == -1)
                        {
                            /*
                             * If no match can be made, then this is a parse error.
                             */
                            ErrNoNamedCharacterMatch();
                            EmitOrAppendStrBuf(returnState);
                            state = returnState;
                            goto continueEofloop;
                        }
                        else
                        {
                            string candidateName = NamedCharacters.NAMES[candidate];
                            if (candidateName.Length == 0
                                    || candidateName[candidateName.Length - 1] != ';')
                            {
                                /*
                                 * If the last character matched is not a U+003B
                                 * SEMICOLON (;), there is a parse error.
                                 */
                                //if ((returnState & DATA_AND_RCDATA_MASK) != 0)
                                if (((byte)returnState & DATA_AND_RCDATA_MASK) == 0)
                                {
                                    /*
                                     * If the entity is being consumed as part of an
                                     * attribute, and the last character matched is
                                     * not a U+003B SEMICOLON (;),
                                     */
                                    char ch;
                                    if (strBufMark == this.strBuffer.Length)
                                    {
                                        ch = '\u0000';
                                    }
                                    else
                                    {
                                        ch = strBuffer[strBufMark];
                                    }
                                    if ((ch >= '0' && ch <= '9')
                                            || (ch >= 'A' && ch <= 'Z')
                                            || (ch >= 'a' && ch <= 'z'))
                                    {
                                        /*
                                         * and the next character is in the range
                                         * U+0030 DIGIT ZERO to U+0039 DIGIT NINE,
                                         * U+0041 LATIN CAPITAL LETTER A to U+005A
                                         * LATIN CAPITAL LETTER Z, or U+0061 LATIN
                                         * SMALL LETTER A to U+007A LATIN SMALL
                                         * LETTER Z, then, for historical reasons,
                                         * all the characters that were matched
                                         * after the U+0026 AMPERSAND (&) must be
                                         * unconsumed, and nothing is returned.
                                         */
                                        ErrNoNamedCharacterMatch();
                                        AppendStrBufToLongStrBuf();
                                        state = returnState;
                                        goto continueEofloop;
                                    }
                                }
                                //if ((returnState & DATA_AND_RCDATA_MASK) != 0)
                                if (((byte)returnState & DATA_AND_RCDATA_MASK) == 0)
                                {
                                    ErrUnescapedAmpersandInterpretedAsCharacterReference();
                                }
                                else
                                {
                                    ErrNotSemicolonTerminated();
                                }
                            }

                            /*
                             * Otherwise, return a character token for the character
                             * corresponding to the entity name (as given by the
                             * second column of the named character references
                             * table).
                             */
                            char[] val = NamedCharacters.VALUES[candidate];
                            if (val.Length == 1)
                            {
                                EmitOrAppendOne(val, returnState);
                            }
                            else
                            {
                                EmitOrAppendTwo(val, returnState);
                            }
                            // this is so complicated!
                            int strBufLen = this.strBuffer.Length;
                            if (strBufMark < strBufLen)
                            {
                                //if ((returnState & DATA_AND_RCDATA_MASK) != 0)
                                if (((byte)returnState & DATA_AND_RCDATA_MASK) == 0)
                                {
                                    for (int i = strBufMark; i < strBufLen; i++)
                                    {
                                        AppendLongStrBuf(strBuffer[i]);
                                    }
                                }
                                else
                                {

                                    TokenListener.Characters(
                                            CopyFromStringBuiler(this.strBuffer, strBufMark, strBufLen - strBufMark));
                                }
                            }
                            state = returnState;
                            goto continueEofloop;
                            /*
                             * If the markup contains I'm &notit; I tell you, the
                             * entity is parsed as "not", as in, I'm Â¬it; I tell
                             * you. But if the markup was I'm &notin; I tell you,
                             * the entity would be parsed as "notin;", resulting in
                             * I'm âˆ‰ I tell you.
                             */
                        }
                    case TokenizerState.CONSUME_NCR:
                    case TokenizerState.DECIMAL_NRC_LOOP:
                    case TokenizerState.HEX_NCR_LOOP:
                        /*
                         * If no characters match the range, then don't consume any
                         * characters (and unconsume the U+0023 NUMBER SIGN
                         * character and, if appropriate, the X character). This is
                         * a parse error; nothing is returned.
                         * 
                         * Otherwise, if the next character is a U+003B SEMICOLON,
                         * consume that too. If it isn't, there is a parse error.
                         */
                        if (!seenDigits)
                        {
                            ErrNoDigitsInNCR();
                            EmitOrAppendStrBuf(returnState);
                            state = returnState;
                            continue;
                        }
                        else
                        {
                            ErrCharRefLacksSemicolon();
                        }
                        // WARNING previous state sets reconsume
                        HandleNcrValue(returnState);
                        state = returnState;
                        continue;
                    case TokenizerState.CDATA_RSQB:
                        TokenListener.Characters(RSQB_RSQB, 0, 1);
                        goto breakEofloop;
                    case TokenizerState.CDATA_RSQB_RSQB:
                        TokenListener.Characters(RSQB_RSQB, 0, 2);
                        goto breakEofloop;
                    case TokenizerState.s01_DATA:
                    default:
                        goto breakEofloop;
                }

            continueEofloop:
                continue;
            } // eofloop

            breakEofloop:
            // case TokenizerState.DATA:
            /*
             * EOF Emit an end-of-file token.
             */
            TokenListener.Eof();
            return;
        }


        static char[] CopyFromStringBuiler(StringBuilder stBuilder, int start, int len)
        {
            char[] copyBuff = new char[len];
            stBuilder.CopyTo(start, copyBuff, 0, len);
            return copyBuff;
        }
        /*@Inline*/

        /* Note - the C# compiler can't be forced to inline (until 4.5) so this was just inlined to improve performance */

        //protected char CheckChar(char[] buf, int pos)
        //{
        //    return buf[pos];
        //}

        // [NOCPP[

        /**
         * Returns the alreadyComplainedAboutNonAscii.
         * 
         * @return the alreadyComplainedAboutNonAscii
         */
        public bool IsAlreadyComplainedAboutNonAscii
        {
            get
            {
                return true;
            }
        }

        // ]NOCPP] 

        public bool InternalEncodingDeclaration(string internalCharset)
        {
            bool accept = false;
            if (EncodingDeclared != null)
            {
                foreach (var inv in EncodingDeclared.GetInvocationList())
                {
                    var args = new EncodingDetectedEventArgs(internalCharset);
                    inv.DynamicInvoke(this, args);
                    if (args.AcceptEncoding)
                        accept = true;
                }
            }

            return accept;
        }


        public void End()
        {
            this.strBuffer = null;
            this.longStrBuffer.Length = 0;
            this.longStrBuffer = null;
            doctypeName = null;
            systemIdentifier = null;
            publicIdentifier = null;
            tagName = null;
            attributeName = null;
            TokenListener.EndTokenization();
            if (attributes != null)
            {
                attributes.Clear(mappingLangToXmlLang);
                attributes = null;
            }
        }

        public void RequestSuspension()
        {
            shouldSuspend = true;
        }


        public bool IsPrevCR
        {
            get
            {
                return lastCR;
            }
        }

        public void ResetToDataState()
        {
            this.strBuffer = new StringBuilder();
            this.longStrBuffer = new StringBuilder();
            stateSave = TokenizerState.s01_DATA;
            // line = 1; XXX line numbers
            lastCR = false;
            index = 0;
            forceQuirks = false;
            additional = '\u0000';
            entCol = -1;
            firstCharKey = -1;
            lo = 0;
            hi = 0; // will always be overwritten before use anyway
            candidate = -1;
            strBufMark = 0;
            prevValue = -1;
            value = 0;
            seenDigits = false;
            endTag = false;
            // Removed J. Treworgy 12/7/2012 - this should remain true so the parser can choose to abort 
            //shouldSuspend = false;
            InitDoctypeFields();
            if (tagName != null)
            {
                tagName = null;
            }
            if (attributeName != null)
            {
                attributeName = null;
            }
            // [NOCPP[
            if (newAttributesEachTime)
            {
                // ]NOCPP]
                if (attributes != null)
                {
                    attributes = null;
                }
                // [NOCPP[
            }
            // ]NOCPP]
        }

        public void InitializeWithoutStarting()
        {

            this.strBuffer = new StringBuilder();
            line = 1;
            this.longStrBuffer = new StringBuilder();
            // [NOCPP[
            html4 = false;
            metaBoundaryPassed = false;
            wantsComments = TokenListener.WantsComments;

            if (!newAttributesEachTime)
            {
                attributes = new HtmlAttributes(mappingLangToXmlLang);
            }
            // ]NOCPP]
            ResetToDataState();
        }


        // [NOCPP[

        /// <summary>
        /// Sets an offset to be added to the position reported to
        /// <code>TransitionHandler</code>.
        /// </summary>
        /// <param name="offset">The offset.</param>
        public void SetTransitionBaseOffset(int offset)
        {
            // TODO: nothing done here??
        }

        // ]NOCPP]

        /// <summary>
        /// Gets a value indicating whether the parsing has been suspended.
        /// </summary>

        public bool IsSuspended
        {
            get
            {
                return shouldSuspend;
            }
        }
    }
}
