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
        void StateLoop3_ScriptData(TokenizerState state, TokenizerState returnState)
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
                    case TokenizerState.s06_SCRIPT_DATA:
                        /*scriptdataloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                switch (c)
                                {
                                    case '<':
                                        /*
                                         * U+003C LESS-THAN SIGN (<) Switch to the
                                         * script data less-than sign state.
                                         */
                                        FlushChars();
                                        returnState = state;
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_LESS_THAN_SIGN, reconsume, pos);
                                        state = TokenizerState.s17_SCRIPT_DATA_LESS_THAN_SIGN;
                                        goto breakScriptdataloop; // FALL THRU continue
                                    // stateloop;
                                    case '\u0000':
                                        EmitReplacementCharacter();
                                        continue;
                                    case '\r':
                                        EmitCarriageReturn();
                                        goto breakStateloop;
                                    case '\n':
                                    default:
                                        /*
                                         * Anything else Emit the current input
                                         * character as a character token. Stay in the
                                         * script data state.
                                         */
                                        continue;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakScriptdataloop:
                            goto case TokenizerState.s17_SCRIPT_DATA_LESS_THAN_SIGN;
                        }
                    // WARNING FALLTHRU case TokenizerState.TRANSITION: DON'T REORDER
                    case TokenizerState.s17_SCRIPT_DATA_LESS_THAN_SIGN:
                        /*scriptdatalessthansignloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                switch (c)
                                {
                                    case '/':
                                        /*
                                         * U+002F SOLIDUS (/) Set the temporary buffer
                                         * to the empty string. Switch to the script
                                         * data end tag open state.
                                         */
                                        index = 0;
                                        ClearStrBuf();
                                        //state = Transition(state, Tokenizer.NON_DATA_END_TAG_NAME, reconsume, pos);
                                        state = TokenizerState.NON_DATA_END_TAG_NAME;
                                        goto continueStateloop;
                                    case '!':
                                        TokenListener.Characters(LT_GT, 0, 1);
                                        reader.StartCollect();
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_ESCAPE_START, reconsume, pos);
                                        state = TokenizerState.s20_SCRIPT_DATA_ESCAPE_START;

                                        goto breakScriptdatalessthansignloop; // FALL THRU
                                    // continue
                                    // stateloop;
                                    default:
                                        /*
                                         * Otherwise, emit a U+003C LESS-THAN SIGN
                                         * character token
                                         */
                                        TokenListener.Characters(LT_GT, 0, 1);
                                        /*
                                         * and reconsume the current input character in
                                         * the data state.
                                         */
                                        reader.StartCollect();
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA, reconsume, pos);
                                        state = TokenizerState.s06_SCRIPT_DATA;
                                        //reconsume = true;
                                        reader.StepBack();
                                        goto continueStateloop;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakScriptdatalessthansignloop:
                            goto case TokenizerState.s20_SCRIPT_DATA_ESCAPE_START;
                        }
                    // WARNING FALLTHRU case TokenizerState.TRANSITION: DON'T REORDER
                    case TokenizerState.s20_SCRIPT_DATA_ESCAPE_START:
                        /*scriptdataescapestartloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                /*
                                 * Consume the next input character:
                                 */
                                switch (c)
                                {
                                    case '-':
                                        /*
                                         * U+002D HYPHEN-MINUS (-) Emit a U+002D
                                         * HYPHEN-MINUS character token. Switch to the
                                         * script data escape start dash state.
                                         */
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_ESCAPE_START_DASH, reconsume, pos);
                                        state = TokenizerState.s21_SCRIPT_DATA_ESCAPE_START_DASH;
                                        goto breakScriptdataescapestartloop; // FALL THRU
                                    // continue
                                    // stateloop;
                                    default:
                                        /*
                                         * Anything else Reconsume the current input
                                         * character in the script data state.
                                         */
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA, reconsume, pos);
                                        state = TokenizerState.s06_SCRIPT_DATA;
                                        //reconsume = true;
                                        reader.StepBack();
                                        goto continueStateloop;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakScriptdataescapestartloop:
                            goto case TokenizerState.s21_SCRIPT_DATA_ESCAPE_START_DASH;
                        }
                    // WARNING FALLTHRU case TokenizerState.TRANSITION: DON'T REORDER
                    case TokenizerState.s21_SCRIPT_DATA_ESCAPE_START_DASH:
                        /*scriptdataescapestartdashloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {
                                switch (c)
                                {
                                    case '-':
                                        /*
                                         * U+002D HYPHEN-MINUS (-) Emit a U+002D
                                         * HYPHEN-MINUS character token. Switch to the
                                         * script data escaped dash dash state.
                                         */
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_ESCAPED_DASH_DASH, reconsume, pos);
                                        state = TokenizerState.s24_SCRIPT_DATA_ESCAPED_DASH_DASH;
                                        goto breakScriptdataescapestartdashloop;
                                    // goto continueStateloop;
                                    default:
                                        /*
                                         * Anything else Reconsume the current input
                                         * character in the script data state.
                                         */
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA, reconsume, pos);
                                        state = TokenizerState.s06_SCRIPT_DATA;
                                        //reconsume = true;
                                        reader.StepBack();
                                        goto continueStateloop;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakScriptdataescapestartdashloop:
                            goto case TokenizerState.s24_SCRIPT_DATA_ESCAPED_DASH_DASH;
                        }
                    // WARNING FALLTHRU case TokenizerState.TRANSITION: DON'T REORDER
                    case TokenizerState.s24_SCRIPT_DATA_ESCAPED_DASH_DASH:
                        /*scriptdataescapeddashdashloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                switch (c)
                                {
                                    case '-':
                                        /*
                                         * U+002D HYPHEN-MINUS (-) Emit a U+002D
                                         * HYPHEN-MINUS character token. Stay in the
                                         * script data escaped dash dash state.
                                         */
                                        continue;
                                    case '<':
                                        /*
                                         * U+003C LESS-THAN SIGN (<) Switch to the
                                         * script data escaped less-than sign state.
                                         */
                                        FlushChars();
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_ESCAPED_LESS_THAN_SIGN, reconsume, pos);
                                        state = TokenizerState.s25_SCRIPT_DATA_ESCAPED_LESS_THAN_SIGN;
                                        goto continueStateloop;
                                    case '>':
                                        /*
                                         * U+003E GREATER-THAN SIGN (>) Emit a U+003E
                                         * GREATER-THAN SIGN character token. Switch to
                                         * the script data state.
                                         */
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA, reconsume, pos);
                                        state = TokenizerState.s06_SCRIPT_DATA;
                                        goto continueStateloop;
                                    case '\u0000':
                                        EmitReplacementCharacter();
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_ESCAPED, reconsume, pos);
                                        state = TokenizerState.s22_SCRIPT_DATA_ESCAPED;
                                        goto breakScriptdataescapeddashdashloop;
                                    case '\r':
                                        EmitCarriageReturn();
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_ESCAPED, reconsume, pos);
                                        state = TokenizerState.s22_SCRIPT_DATA_ESCAPED;
                                        goto breakStateloop;
                                    case '\n':
                                    default:
                                        /*
                                         * Anything else Emit the current input
                                         * character as a character token. Switch to the
                                         * script data escaped state.
                                         */
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_ESCAPED, reconsume, pos);
                                        state = TokenizerState.s22_SCRIPT_DATA_ESCAPED;
                                        goto breakScriptdataescapeddashdashloop;
                                    // goto continueStateloop;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakScriptdataescapeddashdashloop:
                            goto case TokenizerState.s22_SCRIPT_DATA_ESCAPED;
                        }
                    // WARNING FALLTHRU case TokenizerState.TRANSITION: DON'T REORDER
                    case TokenizerState.s22_SCRIPT_DATA_ESCAPED:
                        /*scriptdataescapedloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                switch (c)
                                {
                                    case '-':
                                        /*
                                         * U+002D HYPHEN-MINUS (-) Emit a U+002D
                                         * HYPHEN-MINUS character token. Switch to the
                                         * script data escaped dash state.
                                         */
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_ESCAPED_DASH, reconsume, pos);
                                        state = TokenizerState.s23_SCRIPT_DATA_ESCAPED_DASH;
                                        goto breakScriptdataescapedloop; // FALL THRU
                                    // continue
                                    // stateloop;
                                    case '<':
                                        /*
                                         * U+003C LESS-THAN SIGN (<) Switch to the
                                         * script data escaped less-than sign state.
                                         */
                                        FlushChars();
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_ESCAPED_LESS_THAN_SIGN, reconsume, pos);
                                        state = TokenizerState.s25_SCRIPT_DATA_ESCAPED_LESS_THAN_SIGN;
                                        goto continueStateloop;
                                    case '\u0000':
                                        EmitReplacementCharacter();
                                        continue;
                                    case '\r':
                                        EmitCarriageReturn();
                                        goto breakStateloop;
                                    case '\n':
                                    default:
                                        /*
                                         * Anything else Emit the current input
                                         * character as a character token. Stay in the
                                         * script data escaped state.
                                         */
                                        continue;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakScriptdataescapedloop:
                            goto case TokenizerState.s23_SCRIPT_DATA_ESCAPED_DASH;
                        }
                    // WARNING FALLTHRU case TokenizerState.TRANSITION: DON'T REORDER
                    case TokenizerState.s23_SCRIPT_DATA_ESCAPED_DASH:
                        /*scriptdataescapeddashloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                switch (c)
                                {
                                    case '-':
                                        /*
                                         * U+002D HYPHEN-MINUS (-) Emit a U+002D
                                         * HYPHEN-MINUS character token. Switch to the
                                         * script data escaped dash dash state.
                                         */
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_ESCAPED_DASH_DASH, reconsume, pos);
                                        state = TokenizerState.s24_SCRIPT_DATA_ESCAPED_DASH_DASH;
                                        goto continueStateloop;
                                    case '<':
                                        /*
                                         * U+003C LESS-THAN SIGN (<) Switch to the
                                         * script data escaped less-than sign state.
                                         */
                                        FlushChars();
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_ESCAPED_LESS_THAN_SIGN, reconsume, pos);
                                        state = TokenizerState.s25_SCRIPT_DATA_ESCAPED_LESS_THAN_SIGN;
                                        goto breakScriptdataescapeddashloop;
                                    // goto continueStateloop;
                                    case '\u0000':
                                        EmitReplacementCharacter();
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_ESCAPED, reconsume, pos);
                                        state = TokenizerState.s22_SCRIPT_DATA_ESCAPED;
                                        goto continueStateloop;
                                    case '\r':
                                        EmitCarriageReturn();
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_ESCAPED, reconsume, pos);
                                        state = TokenizerState.s22_SCRIPT_DATA_ESCAPED;
                                        goto breakStateloop;
                                    case '\n':
                                    default:
                                        /*
                                         * Anything else Emit the current input
                                         * character as a character token. Switch to the
                                         * script data escaped state.
                                         */
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_ESCAPED, reconsume, pos);
                                        state = TokenizerState.s22_SCRIPT_DATA_ESCAPED;
                                        goto continueStateloop;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakScriptdataescapeddashloop:
                            goto case TokenizerState.s25_SCRIPT_DATA_ESCAPED_LESS_THAN_SIGN;
                        }
                    // WARNING FALLTHRU case TokenizerState.TRANSITION: DON'T REORDER
                    case TokenizerState.s25_SCRIPT_DATA_ESCAPED_LESS_THAN_SIGN:
                        /*scriptdataescapedlessthanloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {
                                switch (c)
                                {
                                    case '/':
                                        /*
                                         * U+002F SOLIDUS (/) Set the temporary buffer
                                         * to the empty string. Switch to the script
                                         * data escaped end tag open state.
                                         */
                                        index = 0;
                                        ClearStrBuf();
                                        returnState = TokenizerState.s22_SCRIPT_DATA_ESCAPED;

                                        //state = Transition(state, Tokenizer.NON_DATA_END_TAG_NAME, reconsume, pos);
                                        state = TokenizerState.NON_DATA_END_TAG_NAME;
                                        goto continueStateloop;
                                    case 'S':
                                    case 's':
                                        /*
                                         * U+0041 LATIN CAPITAL LETTER A through to
                                         * U+005A LATIN CAPITAL LETTER Z Emit a U+003C
                                         * LESS-THAN SIGN character token and the
                                         * current input character as a character token.
                                         */
                                        TokenListener.Characters(LT_GT, 0, 1);
                                        reader.StartCollect();
                                        index = 1;
                                        /*
                                         * Set the temporary buffer to the empty string.
                                         * Append the lowercase TokenizerState.version of the current
                                         * input character (add 0x0020 to the
                                         * character's code point) to the temporary
                                         * buffer. Switch to the script data double
                                         * escape start state.
                                         */
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_DOUBLE_ESCAPE_START, reconsume, pos);
                                        state = TokenizerState.s28_SCRIPT_DATA_DOUBLE_ESCAPE_START;
                                        goto breakScriptdataescapedlessthanloop;
                                    // goto continueStateloop;
                                    default:
                                        /*
                                         * Anything else Emit a U+003C LESS-THAN SIGN
                                         * character token and reconsume the current
                                         * input character in the script data escaped
                                         * state.
                                         */
                                        TokenListener.Characters(LT_GT, 0, 1);
                                        reader.StartCollect();
                                        //reconsume = true;
                                        reader.StepBack();
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_ESCAPED, reconsume, pos);
                                        state = TokenizerState.s22_SCRIPT_DATA_ESCAPED;
                                        goto continueStateloop;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakScriptdataescapedlessthanloop:
                            goto case TokenizerState.s28_SCRIPT_DATA_DOUBLE_ESCAPE_START;
                        }
                    // WARNING FALLTHRU case TokenizerState.TRANSITION: DON'T REORDER
                    case TokenizerState.s28_SCRIPT_DATA_DOUBLE_ESCAPE_START:
                        /*scriptdatadoubleescapestartloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {
                                Debug.Assert(index > 0);
                                if (index < 6)
                                {
                                    // SCRIPT_ARR.Length
                                    char folded = c;
                                    if (c >= 'A' && c <= 'Z')
                                    {
                                        //make it lower case 
                                        folded += (char)0x20;
                                    }
                                    if (folded != Tokenizer.SCRIPT_ARR[index])
                                    {
                                        //reconsume = true;
                                        reader.StepBack();
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_ESCAPED, reconsume, pos);
                                        state = TokenizerState.s22_SCRIPT_DATA_ESCAPED;
                                        goto continueStateloop;
                                    }
                                    index++;
                                    continue;
                                }
                                switch (c)
                                {
                                    case '\r':
                                        EmitCarriageReturn();
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_DOUBLE_ESCAPED, reconsume, pos);
                                        state = TokenizerState.s29_SCRIPT_DATA_DOUBLE_ESCAPED;
                                        goto breakStateloop;
                                    case '\n':
                                    case ' ':
                                    case '\t':
                                    case '\u000C':
                                    case '/':
                                    case '>':
                                        /*
                                         * U+0009 CHARACTER TABULATION U+000A LINE FEED
                                         * (LF) U+000C FORM FEED (FF) U+0020 SPACE
                                         * U+002F SOLIDUS (/) U+003E GREATER-THAN SIGN
                                         * (>) Emit the current input character as a
                                         * character token. If the temporary buffer is
                                         * the string "script", then switch to the
                                         * script data double escaped state.
                                         */
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_DOUBLE_ESCAPED, reconsume, pos);
                                        state = TokenizerState.s29_SCRIPT_DATA_DOUBLE_ESCAPED;
                                        goto breakScriptdatadoubleescapestartloop;
                                    // goto continueStateloop;
                                    default:
                                        /*
                                         * Anything else Reconsume the current input
                                         * character in the script data escaped state.
                                         */
                                        //reconsume = true;
                                        reader.StepBack();
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_ESCAPED, reconsume, pos);
                                        state = TokenizerState.s22_SCRIPT_DATA_ESCAPED;
                                        goto continueStateloop;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakScriptdatadoubleescapestartloop:
                            goto case TokenizerState.s29_SCRIPT_DATA_DOUBLE_ESCAPED;
                        }
                    // WARNING FALLTHRU case TokenizerState.TRANSITION: DON'T REORDER
                    case TokenizerState.s29_SCRIPT_DATA_DOUBLE_ESCAPED:
                        /*scriptdatadoubleescapedloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                switch (c)
                                {
                                    case '-':
                                        /*
                                         * U+002D HYPHEN-MINUS (-) Emit a U+002D
                                         * HYPHEN-MINUS character token. Switch to the
                                         * script data double escaped dash state.
                                         */
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_DOUBLE_ESCAPED_DASH, reconsume, pos);
                                        state = TokenizerState.s30_SCRIPT_DATA_DOUBLE_ESCAPED_DASH;
                                        goto breakScriptdatadoubleescapedloop; // FALL THRU
                                    // continue
                                    // stateloop;
                                    case '<':
                                        /*
                                         * U+003C LESS-THAN SIGN (<) Emit a U+003C
                                         * LESS-THAN SIGN character token. Switch to the
                                         * script data double escaped less-than sign
                                         * state.
                                         */
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_DOUBLE_ESCAPED_LESS_THAN_SIGN, reconsume, pos);
                                        state = TokenizerState.s32_SCRIPT_DATA_DOUBLE_ESCAPED_LESS_THAN_SIGN;
                                        goto continueStateloop;
                                    case '\u0000':
                                        EmitReplacementCharacter();
                                        continue;
                                    case '\r':
                                        EmitCarriageReturn();
                                        goto breakStateloop;
                                    case '\n':
                                    default:
                                        /*
                                         * Anything else Emit the current input
                                         * character as a character token. Stay in the
                                         * script data double escaped state.
                                         */
                                        continue;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakScriptdatadoubleescapedloop:
                            goto case TokenizerState.s30_SCRIPT_DATA_DOUBLE_ESCAPED_DASH;
                        }
                    // WARNING FALLTHRU case TokenizerState.TRANSITION: DON'T REORDER
                    case TokenizerState.s30_SCRIPT_DATA_DOUBLE_ESCAPED_DASH:
                        /*scriptdatadoubleescapeddashloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {
                                switch (c)
                                {
                                    case '-':
                                        /*
                                         * U+002D HYPHEN-MINUS (-) Emit a U+002D
                                         * HYPHEN-MINUS character token. Switch to the
                                         * script data double escaped dash dash state.
                                         */
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_DOUBLE_ESCAPED_DASH_DASH, reconsume, pos);
                                        state = TokenizerState.s31_SCRIPT_DATA_DOUBLE_ESCAPED_DASH_DASH;
                                        goto breakScriptdatadoubleescapeddashloop;
                                    // goto continueStateloop;
                                    case '<':
                                        /*
                                         * U+003C LESS-THAN SIGN (<) Emit a U+003C
                                         * LESS-THAN SIGN character token. Switch to the
                                         * script data double escaped less-than sign
                                         * state.
                                         */
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_DOUBLE_ESCAPED_LESS_THAN_SIGN, reconsume, pos);
                                        state = TokenizerState.s32_SCRIPT_DATA_DOUBLE_ESCAPED_LESS_THAN_SIGN;
                                        goto continueStateloop;
                                    case '\u0000':
                                        EmitReplacementCharacter();
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_DOUBLE_ESCAPED, reconsume, pos);
                                        state = TokenizerState.s29_SCRIPT_DATA_DOUBLE_ESCAPED;
                                        goto continueStateloop;
                                    case '\r':
                                        EmitCarriageReturn();
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_DOUBLE_ESCAPED, reconsume, pos);
                                        state = TokenizerState.s29_SCRIPT_DATA_DOUBLE_ESCAPED;
                                        goto breakStateloop;
                                    case '\n':
                                    default:
                                        /*
                                         * Anything else Emit the current input
                                         * character as a character token. Switch to the
                                         * script data double escaped state.
                                         */
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_DOUBLE_ESCAPED, reconsume, pos);
                                        state = TokenizerState.s29_SCRIPT_DATA_DOUBLE_ESCAPED;
                                        goto continueStateloop;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakScriptdatadoubleescapeddashloop:
                            goto case TokenizerState.s31_SCRIPT_DATA_DOUBLE_ESCAPED_DASH_DASH;
                        }
                    // WARNING FALLTHRU case TokenizerState.TRANSITION: DON'T REORDER
                    case TokenizerState.s31_SCRIPT_DATA_DOUBLE_ESCAPED_DASH_DASH:
                        /*scriptdatadoubleescapeddashdashloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                switch (c)
                                {
                                    case '-':
                                        /*
                                         * U+002D HYPHEN-MINUS (-) Emit a U+002D
                                         * HYPHEN-MINUS character token. Stay in the
                                         * script data double escaped dash dash state.
                                         */
                                        continue;
                                    case '<':
                                        /*
                                         * U+003C LESS-THAN SIGN (<) Emit a U+003C
                                         * LESS-THAN SIGN character token. Switch to the
                                         * script data double escaped less-than sign
                                         * state.
                                         */
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_DOUBLE_ESCAPED_LESS_THAN_SIGN, reconsume, pos);
                                        state = TokenizerState.s32_SCRIPT_DATA_DOUBLE_ESCAPED_LESS_THAN_SIGN;
                                        goto breakScriptdatadoubleescapeddashdashloop;
                                    case '>':
                                        /*
                                         * U+003E GREATER-THAN SIGN (>) Emit a U+003E
                                         * GREATER-THAN SIGN character token. Switch to
                                         * the script data state.
                                         */
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA, reconsume, pos);
                                        state = TokenizerState.s06_SCRIPT_DATA;
                                        goto continueStateloop;
                                    case '\u0000':
                                        EmitReplacementCharacter();
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_DOUBLE_ESCAPED, reconsume, pos);
                                        state = TokenizerState.s29_SCRIPT_DATA_DOUBLE_ESCAPED;
                                        goto continueStateloop;
                                    case '\r':
                                        EmitCarriageReturn();
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_DOUBLE_ESCAPED, reconsume, pos);
                                        state = TokenizerState.s29_SCRIPT_DATA_DOUBLE_ESCAPED;
                                        goto breakStateloop;
                                    case '\n':
                                    default:
                                        /*
                                         * Anything else Emit the current input
                                         * character as a character token. Switch to the
                                         * script data double escaped state.
                                         */
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_DOUBLE_ESCAPED, reconsume, pos);
                                        state = TokenizerState.s29_SCRIPT_DATA_DOUBLE_ESCAPED;
                                        goto continueStateloop;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakScriptdatadoubleescapeddashdashloop:
                            goto case TokenizerState.s32_SCRIPT_DATA_DOUBLE_ESCAPED_LESS_THAN_SIGN;
                        }
                    // WARNING FALLTHRU case TokenizerState.TRANSITION: DON'T REORDER
                    case TokenizerState.s32_SCRIPT_DATA_DOUBLE_ESCAPED_LESS_THAN_SIGN:
                        /*scriptdatadoubleescapedlessthanloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {
                                switch (c)
                                {
                                    case '/':
                                        /*
                                         * U+002F SOLIDUS (/) Emit a U+002F SOLIDUS
                                         * character token. Set the temporary buffer to
                                         * the empty string. Switch to the script data
                                         * double escape end state.
                                         */
                                        index = 0;
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_DOUBLE_ESCAPE_END, reconsume, pos);
                                        state = TokenizerState.s33_SCRIPT_DATA_DOUBLE_ESCAPE_END;
                                        goto breakScriptdatadoubleescapedlessthanloop;
                                    default:
                                        /*
                                         * Anything else Reconsume the current input
                                         * character in the script data double escaped
                                         * state.
                                         */
                                        //reconsume = true;
                                        reader.StepBack();
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_DOUBLE_ESCAPED, reconsume, pos);
                                        state = TokenizerState.s29_SCRIPT_DATA_DOUBLE_ESCAPED;
                                        goto continueStateloop;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakScriptdatadoubleescapedlessthanloop:
                            goto case TokenizerState.s33_SCRIPT_DATA_DOUBLE_ESCAPE_END;
                        }
                    // WARNING FALLTHRU case TokenizerState.TRANSITION: DON'T REORDER
                    case TokenizerState.s33_SCRIPT_DATA_DOUBLE_ESCAPE_END:
                        /*scriptdatadoubleescapeendloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {
                                if (index < 6)
                                {
                                    // SCRIPT_ARR.Length
                                    char folded = c;
                                    if (c >= 'A' && c <= 'Z')
                                    {
                                        folded += (char)0x20;
                                    }
                                    if (folded != Tokenizer.SCRIPT_ARR[index])
                                    {
                                        reader.StepBack();
                                        //reconsume = true;
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_DOUBLE_ESCAPED, reconsume, pos);
                                        state = TokenizerState.s29_SCRIPT_DATA_DOUBLE_ESCAPED;
                                        goto continueStateloop;
                                    }
                                    index++;
                                    continue;
                                }
                                switch (c)
                                {
                                    case '\r':
                                        EmitCarriageReturn();
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_ESCAPED, reconsume, pos);
                                        state = TokenizerState.s22_SCRIPT_DATA_ESCAPED;
                                        goto breakStateloop;
                                    case '\n':
                                    case ' ':
                                    case '\t':
                                    case '\u000C':
                                    case '/':
                                    case '>':
                                        /*
                                         * U+0009 CHARACTER TABULATION U+000A LINE FEED
                                         * (LF) U+000C FORM FEED (FF) U+0020 SPACE
                                         * U+002F SOLIDUS (/) U+003E GREATER-THAN SIGN
                                         * (>) Emit the current input character as a
                                         * character token. If the temporary buffer is
                                         * the string "script", then switch to the
                                         * script data escaped state.
                                         */
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_ESCAPED, reconsume, pos);
                                        state = TokenizerState.s22_SCRIPT_DATA_ESCAPED;
                                        goto continueStateloop;
                                    default:
                                        /*
                                         * Reconsume the current input character in the
                                         * script data double escaped state.
                                         */
                                        //reconsume = true;
                                        reader.StepBack();
                                        //state = Transition(state, Tokenizer.SCRIPT_DATA_DOUBLE_ESCAPED, reconsume, pos);
                                        state = TokenizerState.s29_SCRIPT_DATA_DOUBLE_ESCAPED;
                                        goto continueStateloop;
                                }
                            }
                        }
                        //------------------------------------
                        //eof
                        goto breakStateloop; 
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