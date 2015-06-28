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

namespace HtmlParserSharp.Core
{
    partial class Tokenizer
    {
        void StateLoop3_NCR(TokenizerState state, TokenizerState returnState)
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
                    // XXX reorder point
                    case TokenizerState.CONSUME_NCR:
                        {
                            char c;
                            if (!reader.ReadNext(out c))
                            {
                                //------------------------------------
                                //eof
                                goto breakStateloop;
                            }

                            prevValue = -1;
                            value = 0;
                            seenDigits = false;
                            /*
                             * The behavior further depends on the character after the
                             * U+0023 NUMBER SIGN:
                             */
                            switch (c)
                            {
                                case 'x':
                                case 'X':

                                    /*
                                     * U+0078 LATIN SMALL LETTER X U+0058 LATIN CAPITAL
                                     * LETTER X Consume the X.
                                     * 
                                     * Follow the steps below, but using the range of
                                     * characters U+0030 DIGIT ZERO through to U+0039
                                     * DIGIT NINE, U+0061 LATIN SMALL LETTER A through
                                     * to U+0066 LATIN SMALL LETTER F, and U+0041 LATIN
                                     * CAPITAL LETTER A, through to U+0046 LATIN CAPITAL
                                     * LETTER F (in other words, 0-9, A-F, a-f).
                                     * 
                                     * When it comes to interpreting the number,
                                     * interpret it as a hexadecimal number.
                                     */
                                    AppendStrBuf(c);
                                    //state = Transition(state, Tokenizer.HEX_NCR_LOOP, reconsume, pos);
                                    state = TokenizerState.HEX_NCR_LOOP;

                                    goto continueStateloop;
                                default:
                                    /*
                                     * Anything else Follow the steps below, but using
                                     * the range of characters U+0030 DIGIT ZERO through
                                     * to U+0039 DIGIT NINE (i.e. just 0-9).
                                     * 
                                     * When it comes to interpreting the number,
                                     * interpret it as a decimal number.
                                     */
                                    //state = Transition(state, Tokenizer.DECIMAL_NRC_LOOP, reconsume, pos);
                                    state = TokenizerState.DECIMAL_NRC_LOOP;
                                    //reconsume = true;
                                    reader.StepBack();
                                    // FALL THROUGH goto continueStateloop;
                                    break;
                            }
                            //------------------------------------
                            // WARNING FALLTHRU case TokenizerState.TRANSITION: DON'T REORDER
                            goto case TokenizerState.DECIMAL_NRC_LOOP;
                        }
                    case TokenizerState.DECIMAL_NRC_LOOP:
                        /*decimalloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                // Deal with overflow gracefully
                                if (value < prevValue)
                                {
                                    value = 0x110000; // Value above Unicode range but
                                    // within int
                                    // range
                                }
                                prevValue = value;
                                /*
                                 * Consume as many characters as match the range of
                                 * characters given above.
                                 */
                                if (c >= '0' && c <= '9')
                                {
                                    seenDigits = true;
                                    value *= 10;
                                    value += c - '0';
                                    continue;
                                }
                                else if (c == ';')
                                {
                                    if (seenDigits)
                                    {
                                        //if ((returnState & DATA_AND_RCDATA_MASK) == 0)
                                        if (((byte)returnState & DATA_AND_RCDATA_MASK) != 0)
                                        {
                                            reader.SkipOneAndStartCollect();
                                        }
                                        //state = Transition(state, Tokenizer.HANDLE_NCR_VALUE, reconsume, pos);
                                        state = TokenizerState.HANDLE_NCR_VALUE;

                                        // FALL THROUGH goto continueStateloop;
                                        goto breakDecimalloop;
                                    }
                                    else
                                    {
                                        ErrNoDigitsInNCR();
                                        AppendStrBuf(';');
                                        EmitOrAppendStrBuf(returnState);
                                        //if ((returnState & DATA_AND_RCDATA_MASK) == 0)
                                        if (((byte)returnState & DATA_AND_RCDATA_MASK) != 0)
                                        {
                                            reader.SkipOneAndStartCollect();
                                        }
                                        //state = Transition(state, returnState, reconsume, pos);
                                        state = returnState;

                                        goto continueStateloop;
                                    }
                                }
                                else
                                {
                                    /*
                                     * If no characters match the range, then don't
                                     * consume any characters (and unconsume the U+0023
                                     * NUMBER SIGN character and, if appropriate, the X
                                     * character). This is a parse error; nothing is
                                     * returned.
                                     * 
                                     * Otherwise, if the next character is a U+003B
                                     * SEMICOLON, consume that too. If it isn't, there
                                     * is a parse error.
                                     */
                                    if (!seenDigits)
                                    {
                                        ErrNoDigitsInNCR();
                                        EmitOrAppendStrBuf(returnState);
                                        //if ((returnState & DATA_AND_RCDATA_MASK) == 0)
                                        if (((byte)returnState & DATA_AND_RCDATA_MASK) != 0)
                                        {
                                            reader.StartCollect();
                                        }
                                        //state = Transition(state, returnState, reconsume, pos);
                                        state = returnState;
                                        //reconsume = true;
                                        reader.StepBack();
                                        goto continueStateloop;
                                    }
                                    else
                                    {
                                        ErrCharRefLacksSemicolon();
                                        //if ((returnState & DATA_AND_RCDATA_MASK) == 0)
                                        if (((byte)returnState & DATA_AND_RCDATA_MASK) != 0)
                                        {
                                            reader.StartCollect();
                                        }
                                        //state = Transition(state, Tokenizer.HANDLE_NCR_VALUE, reconsume, pos);
                                        state = TokenizerState.HANDLE_NCR_VALUE;
                                        //reconsume = true;
                                        reader.StepBack();
                                        // FALL THROUGH goto continueStateloop;
                                        goto breakDecimalloop;
                                    }
                                }
                            }

                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //-------------------------------------
                        breakDecimalloop:
                            goto case TokenizerState.HANDLE_NCR_VALUE;
                        }
                    // WARNING FALLTHRU case TokenizerState.TRANSITION: DON'T REORDER
                    case TokenizerState.HANDLE_NCR_VALUE:
                        {
                            // WARNING previous state sets reconsume
                            // XXX inline this case TokenizerState.if the method size can take it
                            HandleNcrValue(returnState);
                            //state = Transition(state, returnState, reconsume, pos);
                            state = returnState;

                            goto continueStateloop;
                        }
                    // XXX reorder point
                    case TokenizerState.HEX_NCR_LOOP:
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                // Deal with overflow gracefully
                                if (value < prevValue)
                                {
                                    value = 0x110000; // Value above Unicode range but
                                    // within int
                                    // range
                                }
                                prevValue = value;
                                /*
                                 * Consume as many characters as match the range of
                                 * characters given above.
                                 */
                                if (c >= '0' && c <= '9')
                                {
                                    seenDigits = true;
                                    value *= 16;
                                    value += c - '0';
                                    continue;
                                }
                                else if (c >= 'A' && c <= 'F')
                                {
                                    seenDigits = true;
                                    value *= 16;
                                    value += c - 'A' + 10;
                                    continue;
                                }
                                else if (c >= 'a' && c <= 'f')
                                {
                                    seenDigits = true;
                                    value *= 16;
                                    value += c - 'a' + 10;
                                    continue;
                                }
                                else if (c == ';')
                                {
                                    if (seenDigits)
                                    {
                                        //if ((returnState & DATA_AND_RCDATA_MASK) == 0)
                                        if (((byte)returnState & DATA_AND_RCDATA_MASK) != 0)
                                        {
                                            reader.SkipOneAndStartCollect();
                                        }
                                        //state = Transition(state, Tokenizer.HANDLE_NCR_VALUE, reconsume, pos);
                                        state = TokenizerState.HANDLE_NCR_VALUE;
                                        goto continueStateloop;
                                    }
                                    else
                                    {
                                        ErrNoDigitsInNCR();
                                        AppendStrBuf(';');
                                        EmitOrAppendStrBuf(returnState);
                                        //if ((returnState & DATA_AND_RCDATA_MASK) == 0)
                                        if (((byte)returnState & DATA_AND_RCDATA_MASK) != 0)
                                        {
                                            reader.SkipOneAndStartCollect();
                                        }
                                        //state = Transition(state, returnState, reconsume, pos);
                                        state = returnState;
                                        goto continueStateloop;
                                    }
                                }
                                else
                                {
                                    /*
                                     * If no characters match the range, then don't
                                     * consume any characters (and unconsume the U+0023
                                     * NUMBER SIGN character and, if appropriate, the X
                                     * character). This is a parse error; nothing is
                                     * returned.
                                     * 
                                     * Otherwise, if the next character is a U+003B
                                     * SEMICOLON, consume that too. If it isn't, there
                                     * is a parse error.
                                     */
                                    if (!seenDigits)
                                    {
                                        ErrNoDigitsInNCR();
                                        EmitOrAppendStrBuf(returnState);
                                        //if ((returnState & DATA_AND_RCDATA_MASK) == 0)
                                        if (((byte)returnState & DATA_AND_RCDATA_MASK) != 0)
                                        {
                                            reader.StartCollect();
                                        }
                                        //state = Transition(state, returnState, reconsume, pos);
                                        state = returnState;
                                        //reconsume = true;
                                        reader.StepBack();
                                        goto continueStateloop;
                                    }
                                    else
                                    {
                                        ErrCharRefLacksSemicolon();
                                        //if ((returnState & DATA_AND_RCDATA_MASK) == 0)
                                        if (((byte)returnState & DATA_AND_RCDATA_MASK) != 0)
                                        {
                                            reader.StartCollect();
                                        }
                                        //state = Transition(state, Tokenizer.HANDLE_NCR_VALUE, reconsume, pos);
                                        state = TokenizerState.HANDLE_NCR_VALUE;
                                        //reconsume = true;
                                        reader.StepBack();
                                        goto continueStateloop;
                                    }
                                }
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
