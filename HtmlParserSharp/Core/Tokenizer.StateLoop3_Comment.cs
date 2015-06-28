/*
 * Copyright (c) 2005-2007 Henri Sivonen
 * Copyright (c) 2007-2010 Mozilla Foundation
 * Portions of comments Copyright 2004-2010 Apple Computer, Inc., Mozilla 
 * Foundation, and Opera Software ASA.
 * Copyright (c) 2012 Patrick Reisert
 * 
 * 2015, WinterDev
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
    partial class Tokenizer
    {
        void StateLoop3_Comment(TokenizerState state, TokenizerState returnState)
        {

            /*
             * Idioms used in this code:
             * 
             * 
             * Consuming the next input character
             * 
             * To consume the next input character, the code does this: if (++pos ==
             * endPos) { goto breakStateloop; } c = buf[pos];
             * 
             * 
             * Staying in a state
             * 
             * When there's a state that the tokenizer may stay in over multiple
             * input characters, the state has a wrapper |for(;;)| loop and staying
             * in the state continues the loop.
             * 
             * 
             * Switching to another state
             * 
             * To switch to another state, the code sets the state variable to the
             * magic number of the new state. Then it either continues stateloop or
             * breaks out of the state's own wrapper loop if the target state is
             * right after the current state in source order. (This is a partial
             * workaround for Java's lack of goto.)
             * 
             * 
             * Reconsume support
             * 
             * The spec sometimes says that an input character is reconsumed in
             * another state. If a state can ever be entered so that an input
             * character can be reconsumed in it, the state's code starts with an
             * |if (reconsume)| that sets reconsume to false and skips over the
             * normal code for consuming a new character.
             * 
             * To reconsume the current character in another state, the code sets
             * |reconsume| to true and then switches to the other state.
             * 
             * 
             * Emitting character tokens
             * 
             * This method emits character tokens lazily. Whenever a new range of
             * character tokens starts, the field cstart must be set to the start
             * index of the range. The flushChars() method must be called at the end
             * of a range to flush it.
             * 
             * 
             * U+0000 handling
             * 
             * The various states have to handle the replacement of U+0000 with
             * U+FFFD. However, if U+0000 would be reconsumed in another state, the
             * replacement doesn't need to happen, because it's handled by the
             * reconsuming state.
             * 
             * 
             * LF handling
             * 
             * Every state needs to increment the line number upon LF unless the LF
             * gets reconsumed by another state which increments the line number.
             * 
             * 
             * CR handling
             * 
             * Every state needs to handle CR unless the CR gets reconsumed and is
             * handled by the reconsuming state. The CR needs to be handled as if it
             * were and LF, the lastCR field must be set to true and then this
             * method must return. The IO driver will then swallow the next
             * character if it is an LF to coalesce CRLF.
             */

            /*
             * As there is no support for labeled loops in C#, instead of break <loop>;
             * the port uses goto break<loop>; and a label after the loop.
             * Instead of continue <loop>; it uses goto continue<loop>; and a label
             * at the beginning or end of the loop (which doesn't matter in for(;;) loops)
             */

            /*stateloop:*/
            for (; ; )
            {


                //*************
            continueStateloop:
                //*************

                switch (state)
                {

                    case TokenizerState.s45_MARKUP_DECLARATION_OPEN:
                        /*markupdeclarationopenloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                /*
                                 * If the next two characters are both U+002D
                                 * HYPHEN-MINUS characters (-), consume those two
                                 * characters, create a comment token whose data is the
                                 * empty string, and switch to the comment start state.
                                 * 
                                 * Otherwise, if the next seven characters are an ASCII
                                 * case-insensitive match for the word "DOCTYPE", then
                                 * consume those characters and switch to the DOCTYPE
                                 * state.
                                 * 
                                 * Otherwise, if the insertion mode is
                                 * "in foreign content" and the current node is not an
                                 * element in the HTML namespace and the next seven
                                 * characters are an case-sensitive match for the string
                                 * "[CDATA[" (the five uppercase TokenizerState.letters "CDATA" with a
                                 * U+005B LEFT SQUARE BRACKET character before and
                                 * after), then consume those characters and switch to
                                 * the CDATA section state.
                                 * 
                                 * Otherwise, is is a parse error. Switch to the bogus
                                 * comment state. The next character that is consumed,
                                 * if any, is the first character that will be in the
                                 * comment.
                                 */
                                switch (c)
                                {
                                    case '-':
                                        ClearLongStrBufAndAppend(c);
                                        //state = Transition(state, Tokenizer.MARKUP_DECLARATION_HYPHEN, reconsume, pos);
                                        state = TokenizerState.MARKUP_DECLARATION_HYPHEN;
                                        goto breakMarkupdeclarationopenloop;
                                    // goto continueStateloop;
                                    case 'd':
                                    case 'D':
                                        ClearLongStrBufAndAppend(c);
                                        index = 0;
                                        //state = Transition(state, Tokenizer.MARKUP_DECLARATION_OCTYPE, reconsume, pos);
                                        state = TokenizerState.MARKUP_DECLARATION_OCTYPE;
                                        goto continueStateloop;
                                    case '[':
                                        if (TokenListener.IsCDataSectionAllowed)
                                        {
                                            ClearLongStrBufAndAppend(c);
                                            index = 0;
                                            //state = Transition(state, Tokenizer.CDATA_START, reconsume, pos);
                                            state = TokenizerState.CDATA_START;
                                            goto continueStateloop;
                                        }
                                        else
                                        {
                                            // else fall through
                                            goto default;
                                        }
                                    default:
                                        ErrBogusComment();
                                        ClearLongStrBuf();
                                        //state = Transition(state, Tokenizer.BOGUS_COMMENT, reconsume, pos);
                                        state = TokenizerState.s44_BOGUS_COMMENT;
                                        //reconsume = true;
                                        reader.StepBack();
                                        goto continueStateloop;
                                }
                            }
                            //-------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakMarkupdeclarationopenloop:
                            goto case TokenizerState.MARKUP_DECLARATION_HYPHEN;
                        }
                    // FALLTHRU DON'T REORDER
                    case TokenizerState.MARKUP_DECLARATION_HYPHEN:
                        /*markupdeclarationhyphenloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {
                                switch (c)
                                {
                                    case '\u0000':
                                        goto breakStateloop;
                                    case '-':
                                        ClearLongStrBuf();
                                        //state = Transition(state, Tokenizer.COMMENT_START, reconsume, pos);
                                        state = TokenizerState.s46_COMMENT_START;
                                        goto breakMarkupdeclarationhyphenloop;
                                    // goto continueStateloop;
                                    default:
                                        ErrBogusComment();
                                        //state = Transition(state, Tokenizer.BOGUS_COMMENT, reconsume, pos);
                                        state = TokenizerState.s44_BOGUS_COMMENT;
                                        //reconsume = true;
                                        reader.StepBack();
                                        goto continueStateloop;
                                }
                            }
                            //-------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakMarkupdeclarationhyphenloop:
                            goto case TokenizerState.s46_COMMENT_START;
                        }
                    // FALLTHRU DON'T REORDER
                    case TokenizerState.s46_COMMENT_START:
                        /*commentstartloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                switch (c)
                                {
                                    case '-':
                                        /*
                                         * U+002D HYPHEN-MINUS (-) Switch to the comment
                                         * start dash state.
                                         */
                                        AppendLongStrBuf(c);
                                        //state = Transition(state, Tokenizer.COMMENT_START_DASH, reconsume, pos);
                                        state = TokenizerState.s47_COMMENT_START_DASH;
                                        goto continueStateloop;
                                    case '>':
                                        /*
                                         * U+003E GREATER-THAN SIGN (>) Parse error.
                                         */
                                        ErrPrematureEndOfComment();
                                        /* Emit the comment token. */
                                        EmitComment(0);
                                        /*
                                         * Switch to the data state.
                                         */
                                        //state = Transition(state, Tokenizer.DATA, reconsume, pos);
                                        state = TokenizerState.s01_DATA;

                                        goto continueStateloop;
                                    case '\r':
                                        AppendLongStrBufCarriageReturn();
                                        // state = Transition(state, Tokenizer.COMMENT, reconsume, pos);
                                        state = TokenizerState.s48_COMMENT;
                                        goto breakStateloop;
                                    case '\n':
                                        AppendLongStrBufLineFeed();
                                        //state = Transition(state, Tokenizer.COMMENT, reconsume, pos);
                                        state = TokenizerState.s48_COMMENT;

                                        goto breakCommentstartloop;
                                    case '\u0000':
                                        c = '\uFFFD';
                                        // fall thru
                                        goto default;
                                    default:
                                        /*
                                         * Anything else Append the input character to
                                         * the comment token's data.
                                         */
                                        AppendLongStrBuf(c);
                                        /*
                                         * Switch to the comment state.
                                         */
                                        //state = Transition(state, Tokenizer.COMMENT, reconsume, pos);
                                        state = TokenizerState.s48_COMMENT;

                                        goto breakCommentstartloop;
                                    // goto continueStateloop;
                                }
                            }
                            //-------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakCommentstartloop:
                            goto case TokenizerState.s48_COMMENT;
                        }
                    // FALLTHRU DON'T REORDER
                    case TokenizerState.s48_COMMENT:
                        /*commentloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                switch (c)
                                {
                                    case '-':
                                        /*
                                         * U+002D HYPHEN-MINUS (-) Switch to the comment
                                         * end dash state
                                         */
                                        AppendLongStrBuf(c);
                                        //state = Transition(state, Tokenizer.COMMENT_END_DASH, reconsume, pos);
                                        state = TokenizerState.s49_COMMENT_END_DASH;
                                        goto breakCommentloop;
                                    // goto continueStateloop;
                                    case '\r':
                                        AppendLongStrBufCarriageReturn();
                                        goto breakStateloop;
                                    case '\n':
                                        AppendLongStrBufLineFeed();
                                        continue;
                                    case '\u0000':
                                        c = '\uFFFD';
                                        // fall thru
                                        goto default;
                                    default:
                                        /*
                                         * Anything else Append the input character to
                                         * the comment token's data.
                                         */
                                        AppendLongStrBuf(c);
                                        /*
                                         * Stay in the comment state.
                                         */
                                        continue;
                                }
                            }
                            //-------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakCommentloop:
                            goto case TokenizerState.s49_COMMENT_END_DASH;
                        }
                    // FALLTHRU DON'T REORDER
                    case TokenizerState.s49_COMMENT_END_DASH:
                        /*commentenddashloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                switch (c)
                                {
                                    case '-':
                                        /*
                                         * U+002D HYPHEN-MINUS (-) Switch to the comment
                                         * end state
                                         */
                                        AppendLongStrBuf(c);
                                        //state = Transition(state, Tokenizer.COMMENT_END, reconsume, pos);
                                        state = TokenizerState.s50_COMMENT_END;
                                        goto breakCommentenddashloop;
                                    // goto continueStateloop;
                                    case '\r':
                                        AppendLongStrBufCarriageReturn();
                                        //state = Transition(state, Tokenizer.COMMENT, reconsume, pos);
                                        state = TokenizerState.s48_COMMENT;
                                        goto breakStateloop;
                                    case '\n':
                                        AppendLongStrBufLineFeed();
                                        //state = Transition(state, Tokenizer.COMMENT, reconsume, pos);
                                        state = TokenizerState.s48_COMMENT;
                                        goto continueStateloop;
                                    case '\u0000':
                                        c = '\uFFFD';
                                        goto default;
                                    // fall thru
                                    default:
                                        /*
                                         * Anything else Append a U+002D HYPHEN-MINUS
                                         * (-) character and the input character to the
                                         * comment token's data.
                                         */
                                        AppendLongStrBuf(c);
                                        /*
                                         * Switch to the comment state.
                                         */
                                        //state = Transition(state, Tokenizer.COMMENT, reconsume, pos);
                                        state = TokenizerState.s48_COMMENT;
                                        goto continueStateloop;
                                }
                            }
                            //-------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakCommentenddashloop:
                            goto case TokenizerState.s50_COMMENT_END;
                        }
                    // FALLTHRU DON'T REORDER
                    case TokenizerState.s50_COMMENT_END:
                        /*commentendloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                switch (c)
                                {
                                    case '>':
                                        /*
                                         * U+003E GREATER-THAN SIGN (>) Emit the comment
                                         * token.
                                         */
                                        EmitComment(2);
                                        /*
                                         * Switch to the data state.
                                         */
                                        //state = Transition(state, Tokenizer.DATA, reconsume, pos);
                                        state = TokenizerState.s01_DATA;
                                        goto continueStateloop;
                                    case '-':
                                        /* U+002D HYPHEN-MINUS (-) Parse error. */
                                        /*
                                         * Append a U+002D HYPHEN-MINUS (-) character to
                                         * the comment token's data.
                                         */
                                        AdjustDoubleHyphenAndAppendToLongStrBufAndErr(c);
                                        /*
                                         * Stay in the comment end state.
                                         */
                                        continue;
                                    case '\r':
                                        AdjustDoubleHyphenAndAppendToLongStrBufCarriageReturn();
                                        //state = Transition(state, Tokenizer.COMMENT, reconsume, pos);
                                        state = TokenizerState.s48_COMMENT;
                                        goto breakStateloop;
                                    case '\n':
                                        AdjustDoubleHyphenAndAppendToLongStrBufLineFeed();
                                        //state = Transition(state, Tokenizer.COMMENT, reconsume, pos);
                                        state = TokenizerState.s48_COMMENT;
                                        goto continueStateloop;
                                    case '!':
                                        ErrHyphenHyphenBang();
                                        AppendLongStrBuf(c);
                                        //state = Transition(state, Tokenizer.COMMENT_END_BANG, reconsume, pos);
                                        state = TokenizerState.s51_COMMENT_END_BANG;
                                        goto continueStateloop;
                                    case '\u0000':
                                        c = '\uFFFD';
                                        // fall thru
                                        goto default;
                                    default:
                                        /*
                                         * Append two U+002D HYPHEN-MINUS (-) characters
                                         * and the input character to the comment
                                         * token's data.
                                         */
                                        AdjustDoubleHyphenAndAppendToLongStrBufAndErr(c);
                                        /*
                                         * Switch to the comment state.
                                         */
                                        //state = Transition(state, Tokenizer.COMMENT, reconsume, pos);
                                        state = TokenizerState.s48_COMMENT;
                                        goto continueStateloop;
                                }
                            }
                            //-------------------------------
                            //eof
                            goto breakStateloop;
                        }
                    // XXX reorder point
                    case TokenizerState.s51_COMMENT_END_BANG:
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {
                                switch (c)
                                {
                                    case '>':
                                        /*
                                         * U+003E GREATER-THAN SIGN (>) Emit the comment
                                         * token.
                                         */
                                        EmitComment(3);
                                        /*
                                         * Switch to the data state.
                                         */
                                        //state = Transition(state, Tokenizer.DATA, reconsume, pos);
                                        state = TokenizerState.s01_DATA;
                                        goto continueStateloop;
                                    case '-':
                                        /*
                                         * Append two U+002D HYPHEN-MINUS (-) characters
                                         * and a U+0021 EXCLAMATION MARK (!) character
                                         * to the comment token's data.
                                         */
                                        AppendLongStrBuf(c);
                                        /*
                                         * Switch to the comment end dash state.
                                         */
                                        //state = Transition(state, Tokenizer.COMMENT_END_DASH, reconsume, pos);
                                        state = TokenizerState.s49_COMMENT_END_DASH;
                                        goto continueStateloop;
                                    case '\r':
                                        AppendLongStrBufCarriageReturn();
                                        goto breakStateloop;
                                    case '\n':
                                        AppendLongStrBufLineFeed();
                                        continue;
                                    case '\u0000':
                                        c = '\uFFFD';
                                        // fall thru
                                        goto default;
                                    default:
                                        /*
                                         * Anything else Append two U+002D HYPHEN-MINUS
                                         * (-) characters, a U+0021 EXCLAMATION MARK (!)
                                         * character, and the input character to the
                                         * comment token's data. Switch to the comment
                                         * state.
                                         */
                                        AppendLongStrBuf(c);
                                        /*
                                         * Switch to the comment state.
                                         */
                                        //state = Transition(state, Tokenizer.COMMENT, reconsume, pos);
                                        state = TokenizerState.s48_COMMENT;
                                        goto continueStateloop;
                                }
                            }
                            //-------------------------------
                            //eof
                            goto breakStateloop;
                        }
                    // XXX reorder point
                    case TokenizerState.s47_COMMENT_START_DASH:
                        {
                            char c;
                            if (!reader.ReadNext(out c))
                            {
                                //-------------------------------
                                //eof
                                goto breakStateloop;
                            }
                            //----------------------

                            switch (c)
                            {
                                case '-':
                                    /*
                                     * U+002D HYPHEN-MINUS (-) Switch to the comment end
                                     * state
                                     */
                                    AppendLongStrBuf(c);
                                    //state = Transition(state, Tokenizer.COMMENT_END, reconsume, pos);
                                    state = TokenizerState.s50_COMMENT_END;
                                    goto continueStateloop;
                                case '>':
                                    ErrPrematureEndOfComment();
                                    /* Emit the comment token. */
                                    EmitComment(1);
                                    /*
                                     * Switch to the data state.
                                     */
                                    //state = Transition(state, Tokenizer.DATA, reconsume, pos);
                                    state = TokenizerState.s01_DATA;
                                    goto continueStateloop;
                                case '\r':
                                    AppendLongStrBufCarriageReturn();
                                    //state = Transition(state, Tokenizer.COMMENT, reconsume, pos);
                                    state = TokenizerState.s48_COMMENT;
                                    goto breakStateloop;
                                case '\n':
                                    AppendLongStrBufLineFeed();
                                    //state = Transition(state, Tokenizer.COMMENT, reconsume, pos);
                                    state = TokenizerState.s48_COMMENT;
                                    goto continueStateloop;
                                case '\u0000':
                                    c = '\uFFFD';
                                    // fall thru
                                    goto default;
                                default:
                                    /*
                                     * Append a U+002D HYPHEN-MINUS character (-) and
                                     * the current input character to the comment
                                     * token's data.
                                     */
                                    AppendLongStrBuf(c);
                                    /*
                                     * Switch to the comment state.
                                     */
                                    //state = Transition(state, Tokenizer.COMMENT, reconsume, pos);
                                    state = TokenizerState.s48_COMMENT;
                                    goto continueStateloop;
                            }
                        }
                    // XXX reorder point
                    case TokenizerState.CDATA_START:
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                if (index < 6)
                                { // CDATA_LSQB.Length
                                    if (c == Tokenizer.CDATA_LSQB[index])
                                    {
                                        AppendLongStrBuf(c);
                                    }
                                    else
                                    {
                                        ErrBogusComment();
                                        //state = Transition(state, Tokenizer.BOGUS_COMMENT, reconsume, pos);
                                        state = TokenizerState.s44_BOGUS_COMMENT;
                                        //reconsume = true;
                                        reader.StepBack();
                                        goto continueStateloop;
                                    }
                                    index++;
                                    continue;
                                }
                                else
                                {
                                    reader.StartCollect(); // start coalescing
                                    //state = Transition(state, Tokenizer.CDATA_SECTION, reconsume, pos);
                                    state = TokenizerState.s68_CDATA_SECTION;
                                    //reconsume = true;
                                    reader.StepBack();
                                    goto case TokenizerState.s68_CDATA_SECTION;
                                    //break; // FALL THROUGH goto continueStateloop;
                                }
                            }
                            //-------------------------------
                            //eof
                            goto breakStateloop;
                            //------------------------------------

                        }
                    // WARNING FALLTHRU case TokenizerState.TRANSITION: DON'T REORDER
                    case TokenizerState.s68_CDATA_SECTION:
                        /*cdatasectionloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {
                                switch (c)
                                {
                                    case ']':
                                        FlushChars();
                                        //state = Transition(state, Tokenizer.CDATA_RSQB, reconsume, pos);
                                        state = TokenizerState.CDATA_RSQB;
                                        goto breakCdatasectionloop; // FALL THROUGH
                                    case '\u0000':
                                        EmitReplacementCharacter();
                                        continue;
                                    case '\r':
                                        EmitCarriageReturn();
                                        goto breakStateloop;
                                    case '\n':
                                    default:
                                        continue;
                                }
                            }
                            goto breakStateloop;
                        //------------------------------------
                        breakCdatasectionloop:
                            goto case TokenizerState.CDATA_RSQB;
                        }
                    // WARNING FALLTHRU case TokenizerState.TRANSITION: DON'T REORDER
                    case TokenizerState.CDATA_RSQB:
                        /*cdatarsqb:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {
                                switch (c)
                                {
                                    case ']':
                                        //state = Transition(state, Tokenizer.CDATA_RSQB_RSQB, reconsume, pos);
                                        state = TokenizerState.CDATA_RSQB_RSQB;
                                        goto breakCdatarsqb;
                                    default:
                                        TokenListener.Characters(Tokenizer.RSQB_RSQB, 0, 1);
                                        reader.StartCollect();
                                        //state = Transition(state, Tokenizer.CDATA_SECTION, reconsume, pos);
                                        state = TokenizerState.s68_CDATA_SECTION;
                                        //reconsume = true;
                                        reader.StepBack();
                                        goto continueStateloop;
                                }
                            }
                            //-------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------ 
                        breakCdatarsqb:
                            goto case TokenizerState.CDATA_RSQB_RSQB;
                        }
                    // WARNING FALLTHRU case TokenizerState.TRANSITION: DON'T REORDER
                    case TokenizerState.CDATA_RSQB_RSQB:
                        {
                            char c;
                            if (!reader.ReadNext(out c))
                            {
                                goto breakStateloop;
                            }
                            switch (c)
                            {
                                case '>':
                                    //cstart = pos + 1;
                                    reader.SkipOneAndStartCollect();
                                    //state = Transition(state, Tokenizer.DATA, reconsume, pos);
                                    state = TokenizerState.s01_DATA;
                                    goto continueStateloop;
                                default:
                                    TokenListener.Characters(Tokenizer.RSQB_RSQB, 0, 2);
                                    reader.StartCollect();
                                    //state = Transition(state, Tokenizer.CDATA_SECTION, reconsume, pos);
                                    state = TokenizerState.s68_CDATA_SECTION;
                                    reader.StepBack();
                                    //reconsume = true;
                                    goto continueStateloop;

                            }
                        }

                    // XXX reorder point
                    // BEGIN HOTSPOT WORKAROUND
                    case TokenizerState.s44_BOGUS_COMMENT:
                        /*boguscommentloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                /*
                                 * Consume every character up to and including the first
                                 * U+003E GREATER-THAN SIGN character (>) or the end of
                                 * the file (EOF), whichever comes first. Emit a comment
                                 * token whose data is the concatenation of all the
                                 * characters starting from and including the character
                                 * that caused the state machine to switch into the
                                 * bogus comment state, up to and including the
                                 * character immediately before the last consumed
                                 * character (i.e. up to the character just before the
                                 * U+003E or EOF character). (If the comment was started
                                 * by the end of the file (EOF), the token is empty.)
                                 * 
                                 * Switch to the data state.
                                 * 
                                 * If the end of the file was reached, reconsume the EOF
                                 * character.
                                 */
                                switch (c)
                                {
                                    case '>':
                                        EmitComment(0);
                                        //state = Transition(state, Tokenizer.DATA, reconsume, pos);
                                        state = TokenizerState.s01_DATA;
                                        goto continueStateloop;
                                    case '-':
                                        AppendLongStrBuf(c);
                                        //state = Transition(state, Tokenizer.BOGUS_COMMENT_HYPHEN, reconsume, pos);
                                        state = TokenizerState.BOGUS_COMMENT_HYPHEN;
                                        goto breakBoguscommentloop;
                                    case '\r':
                                        AppendLongStrBufCarriageReturn();
                                        goto breakStateloop;
                                    case '\n':
                                        AppendLongStrBufLineFeed();
                                        continue;
                                    case '\u0000':
                                        c = '\uFFFD';
                                        // fall thru
                                        goto default;
                                    default:
                                        AppendLongStrBuf(c);
                                        continue;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakBoguscommentloop:
                            goto case TokenizerState.BOGUS_COMMENT_HYPHEN;
                        }
                    // FALLTHRU DON'T REORDER
                    case TokenizerState.BOGUS_COMMENT_HYPHEN:
                        /*boguscommenthyphenloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                switch (c)
                                {
                                    case '>':
                                        // [NOCPP[
                                        MaybeAppendSpaceToBogusComment();
                                        // ]NOCPP]
                                        EmitComment(0);
                                        //state = Transition(state, Tokenizer.DATA, reconsume, pos);
                                        state = TokenizerState.s01_DATA;
                                        goto continueStateloop;
                                    case '-':
                                        AppendSecondHyphenToBogusComment();
                                        goto continueBoguscommenthyphenloop;
                                    case '\r':
                                        AppendLongStrBufCarriageReturn();
                                        //state = Transition(state, Tokenizer.BOGUS_COMMENT, reconsume, pos);
                                        state = TokenizerState.s44_BOGUS_COMMENT;
                                        goto breakStateloop;
                                    case '\n':
                                        AppendLongStrBufLineFeed();
                                        //state = Transition(state, Tokenizer.BOGUS_COMMENT, reconsume, pos);
                                        state = TokenizerState.s44_BOGUS_COMMENT;
                                        goto continueStateloop;
                                    case '\u0000':
                                        c = '\uFFFD';
                                        // fall thru
                                        goto default;
                                    default:
                                        AppendLongStrBuf(c);
                                        //state = Transition(state, Tokenizer.BOGUS_COMMENT, reconsume, pos);
                                        state = TokenizerState.s44_BOGUS_COMMENT;
                                        goto continueStateloop;
                                }
                            //------------------------------------
                            continueBoguscommenthyphenloop:
                                continue;
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        }
                    // END HOTSPOT WORKAROUND
                }
            } // stateloop

       breakStateloop:
            //FlushChars(buf, pos);
            FlushChars();
            /*
             * if (prevCR && pos != endPos) { // why is this needed? pos--; col--; }
             */
            // Save locals
            stateSave = state;
            returnStateSave = returnState;
        }

    }

}