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
        void EmitOrAppendStrBuf(TokenizerState returnState)
        {
            //if ((returnState & DATA_AND_RCDATA_MASK) != 0)
            if (((byte)returnState & DATA_AND_RCDATA_MASK) == 0)
            {
                AppendStrBufToLongStrBuf();
            }
            else
            {
                EmitStrBuf();
            }
        }
        TokenizerState EmitCurrentTagToken(bool selfClosing, int pos)
        {
            cstart = pos + 1;
            MaybeErrSlashInEndTag(selfClosing);
            stateSave = TokenizerState.s01_DATA;
            HtmlAttributes attrs = attributes ?? HtmlAttributes.EMPTY_ATTRIBUTES;

            if (endTag)
            {
                /*
                 * When an end tag token is emitted, the content model flag must be
                 * switched to the PCDATA state.
                 */
                MaybeErrAttributesOnEndTag(attrs);
                TokenListener.EndTag(tagName);
            }
            else
            {
                TokenListener.StartTag(tagName, attrs, selfClosing);
            }
            tagName = null;
            ResetAttributes();
            /*
             * The token handler may have called setStateAndEndTagExpectation
             * and changed stateSave since the start of this method.
             */
            return stateSave;
        }

        void EmitReplacementCharacter(char[] buf, int pos)
        {
            FlushChars(buf, pos);
            TokenListener.ZeroOriginatingReplacementCharacter();
            cstart = pos + 1;
        }

        void EmitPlaintextReplacementCharacter(char[] buf, int pos)
        {
            FlushChars(buf, pos);
            TokenListener.Characters(REPLACEMENT_CHARACTER, 0, 1);
            cstart = pos + 1;

        } 
        void EmitCarriageReturn(char[] buf, int pos)
        {
            SilentCarriageReturn();
            FlushChars(buf, pos);
            TokenListener.Characters(LF, 0, 1);
            cstart = int.MaxValue;
        }
        /// <summary>
        /// Emits the current comment token.
        /// </summary>
        /// <param name="provisionalHyphens">The provisional hyphens.</param>
        /// <param name="pos">The position.</param>
        void EmitComment(int provisionalHyphens, int pos)
        {
            // [NOCPP[
            if (wantsComments)
            {
                // ]NOCPP]
                // if (longStrBufOffset != -1) {
                // tokenHandler.comment(buf, longStrBufOffset, longStrBufLen
                // - provisionalHyphens);
                // } else {

                int copyLen = this.longStrBuffer.Length - provisionalHyphens;
                char[] copyBuffer = new char[copyLen];
                longStrBuffer.CopyTo(0, copyBuffer, 0, copyLen);
                TokenListener.Comment(copyBuffer, 0, copyLen);
                // }
                // [NOCPP[
            }
            // ]NOCPP]
            cstart = pos + 1;
        }
        void EmitDoctypeToken(int pos)
        {
            cstart = pos + 1;
            TokenListener.Doctype(doctypeName, publicIdentifier, systemIdentifier,
                    forceQuirks);
            // It is OK and sufficient to release these here, since
            // there's no way out of the doctype states than through paths
            // that call this method.
            doctypeName = null;
            publicIdentifier = null;
            systemIdentifier = null;
        }
        /// <summary>
        /// Emits the smaller buffer as character tokens.
        /// </summary>
        void EmitStrBuf()
        {

            int j = this.strBuffer.Length;
            if (j > 0)
            {
                TokenListener.Characters(CopyFromStringBuiler(strBuffer, 0, j));
            }
        }


        void EmitOrAppendTwo(char[] val, TokenizerState returnState)
        {
            //TODO: review here=>   use != or == ?
            //if ((returnState & DATA_AND_RCDATA_MASK) != 0)
            if (((byte)returnState & DATA_AND_RCDATA_MASK) == 0)
            {
                AppendLongStrBuf(val[0]);
                AppendLongStrBuf(val[1]);
            }
            else
            {
                TokenListener.Characters(val, 0, 2);
            }
        }

        void EmitOrAppendOne(char[] val, TokenizerState returnState)
        {
            if (((byte)returnState & DATA_AND_RCDATA_MASK) == 0)
            {
                AppendLongStrBuf(val[0]);
            }
            else
            {
                TokenListener.Characters(val, 0, 1);
            }
        }
    }


}
