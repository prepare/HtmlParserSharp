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




namespace HtmlParserSharp.Core
{

    partial class Tokenizer
    {

        void ErrGarbageAfterLtSlash()
        {
        }

        void ErrLtSlashGt()
        {
        }

        void ErrWarnLtSlashInRcdata()
        {
        }

        void ErrHtml4LtSlashInRcdata(char folded)
        {
        }

        void ErrCharRefLacksSemicolon()
        {
        }

        void ErrNoDigitsInNCR()
        {
        }

        void ErrGtInSystemId()
        {
        }

        void ErrGtInPublicId()
        {
        }

        void ErrNamelessDoctype()
        {
        }

        void ErrConsecutiveHyphens()
        {
        }

        void ErrPrematureEndOfComment()
        {
        }

        void ErrBogusComment()
        {
        }

        void ErrUnquotedAttributeValOrNull(char c)
        {
        }

        void ErrSlashNotFollowedByGt()
        {
        }

        void ErrHtml4XmlVoidSyntax()
        {
        }

        void ErrNoSpaceBetweenAttributes()
        {
        }

        void ErrHtml4NonNameInUnquotedAttribute(char c)
        {
        }

        void ErrLtOrEqualsOrGraveInUnquotedAttributeOrNull(char c)
        {
        }

        void ErrAttributeValueMissing()
        {
        }

        void ErrBadCharBeforeAttributeNameOrNull(char c)
        {
        }

        void ErrEqualsSignBeforeAttributeName()
        {
        }

        void ErrBadCharAfterLt(char c)
        {
        }

        void ErrLtGt()
        {
        }

        void ErrProcessingInstruction()
        {
        }

        void ErrUnescapedAmpersandInterpretedAsCharacterReference()
        {
        }

        void ErrNotSemicolonTerminated()
        {
        }

        void ErrNoNamedCharacterMatch()
        {
        }

        void ErrQuoteBeforeAttributeName(char c)
        {
        }

        void ErrQuoteOrLtInAttributeNameOrNull(char c)
        {
        }

        void ErrExpectedPublicId()
        {
        }

        void ErrBogusDoctype()
        {
        }

        void MaybeWarnPrivateUseAstral()
        {
        }

        void MaybeWarnPrivateUse(char ch)
        {
        }

        void MaybeErrAttributesOnEndTag(HtmlAttributes attrs)
        {
        }

        void MaybeErrSlashInEndTag(bool selfClosing)
        {
        }

        char ErrNcrNonCharacter(char ch)
        {
            return ch;
        }

        void ErrAstralNonCharacter(int ch)
        {
        }

        void ErrNcrSurrogate()
        {
        }

        char ErrNcrControlChar(char ch)
        {
            return ch;
        }

        void ErrNcrCr()
        {
        }

        void ErrNcrInC1Range()
        {
        }

        void ErrEofInPublicId()
        {
        }

        void ErrEofInComment()
        {
        }

        void ErrEofInDoctype()
        {
        }

        void ErrEofInAttributeValue()
        {
        }

        void ErrEofInAttributeName()
        {
        }

        void ErrEofWithoutGt()
        {
        }

        void ErrEofInTagName()
        {
        }

        void ErrEofInEndTag()
        {
        }

        void ErrEofAfterLt()
        {
        }

        void ErrNcrOutOfRange()
        {
        }

        void ErrNcrUnassigned()
        {
        }

        void ErrDuplicateAttribute()
        {
        }

        void ErrEofInSystemId()
        {
        }

        void ErrExpectedSystemId()
        {
        }

        void ErrMissingSpaceBeforeDoctypeName()
        {
        }

        void ErrHyphenHyphenBang()
        {
        }

        void ErrNcrControlChar()
        {
        }

        void ErrNcrZero()
        {
        }

        void ErrNoSpaceBetweenDoctypeSystemKeywordAndQuote()
        {
        }

        void ErrNoSpaceBetweenPublicAndSystemIds()
        {
        }

        void ErrNoSpaceBetweenDoctypePublicKeywordAndQuote()
        {
        }

        void NoteAttributeWithoutValue()
        {
        }

        void NoteUnquotedAttributeValue()
        {
        }

        void StartErrorReporting()
        {

        }



    }

}