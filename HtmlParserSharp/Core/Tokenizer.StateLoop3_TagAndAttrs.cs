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
        void StateLoop3_Tag(TokenizerState state, TokenizerState returnState)
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
                    case TokenizerState.s01_DATA:
                        /*dataloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {
                                switch (c)
                                {
                                    case '&':
                                        /*
                                         * U+0026 AMPERSAND (&) Switch to the character
                                         * reference in data state.
                                         */
                                        FlushChars();
                                        ClearStrBufAndAppend(c);
                                        SetAdditionalAndRememberAmpersandLocation('\u0000');
                                        returnState = state;
                                        //state = Transition(state, Tokenizer.CONSUME_CHARACTER_REFERENCE, reconsume, pos);
                                        state = TokenizerState.CONSUME_CHARACTER_REFERENCE;

                                        goto continueStateloop;
                                    case '<':
                                        /*
                                         * U+003C LESS-THAN SIGN (<) Switch to the tag
                                         * open state.
                                         */
                                        FlushChars();

                                        //state = Transition(state, Tokenizer.TAG_OPEN, reconsume, pos);
                                        state = TokenizerState.s08_TAG_OPEN;
                                        goto breakDataloop; // FALL THROUGH continue
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
                                         * Anything else Emit the input character as a
                                         * character token.
                                         * 
                                         * Stay in the data state.
                                         */
                                        continue;
                                }
                            }


                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------
                        breakDataloop:
                            goto case TokenizerState.s08_TAG_OPEN;
                            //------------      
                        }
                    // WARNING FALLTHRU case TokenizerState.TRANSITION: DON'T REORDER
                    case TokenizerState.s08_TAG_OPEN:
                        /*tagopenloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {
                                /*
                                 * The behavior of this state depends on the content
                                 * model flag.
                                 */

                                /*
                                 * If the content model flag is set to the PCDATA state
                                 * Consume the next input character:
                                 */
                                if (c >= 'A' && c <= 'Z')
                                {
                                    /*
                                     * U+0041 LATIN CAPITAL LETTER A through to U+005A
                                     * LATIN CAPITAL LETTER Z Create a new start tag
                                     * token,
                                     */
                                    endTag = false;
                                    /*
                                     * set its tag name to the lowercase TokenizerState.version of the
                                     * input character (add 0x0020 to the character's
                                     * code point),
                                     */
                                    ClearStrBufAndAppend((char)(c + 0x20));
                                    /* then switch to the tag name state. */
                                    //state = Transition(state, Tokenizer.TAG_NAME, reconsume, pos);
                                    state = TokenizerState.s10_TAG_NAME;
                                    /*
                                     * (Don't emit the token yet; further details will
                                     * be filled in before it is emitted.)
                                     */
                                    goto breakTagopenloop;
                                    // goto continueStateloop;
                                }
                                else if (c >= 'a' && c <= 'z')
                                {
                                    /*
                                     * U+0061 LATIN SMALL LETTER A through to U+007A
                                     * LATIN SMALL LETTER Z Create a new start tag
                                     * token,
                                     */
                                    endTag = false;
                                    /*
                                     * set its tag name to the input character,
                                     */
                                    ClearStrBufAndAppend(c);
                                    /* then switch to the tag name state. */
                                    //state = Transition(state, Tokenizer.TAG_NAME, reconsume, pos);
                                    state = TokenizerState.s10_TAG_NAME;
                                    /*
                                     * (Don't emit the token yet; further details will
                                     * be filled in before it is emitted.)
                                     */
                                    goto breakTagopenloop;
                                    // goto continueStateloop;
                                }
                                switch (c)
                                {
                                    case '!':
                                        /*
                                         * U+0021 EXCLAMATION MARK (!) Switch to the
                                         * markup declaration open state.
                                         */
                                        //state = Transition(state, Tokenizer.MARKUP_DECLARATION_OPEN, reconsume, pos);
                                        state = TokenizerState.s45_MARKUP_DECLARATION_OPEN;
                                        goto continueStateloop;
                                    case '/':
                                        /*
                                         * U+002F SOLIDUS (/) Switch to the close tag
                                         * open state.
                                         */
                                        //state = Transition(state, Tokenizer.CLOSE_TAG_OPEN, reconsume, pos);
                                        state = TokenizerState.s09_CLOSE_TAG_OPEN;
                                        goto continueStateloop;
                                    case '?':
                                        /*
                                         * U+003F QUESTION MARK (?) Parse error.
                                         */
                                        ErrProcessingInstruction();
                                        /*
                                         * Switch to the bogus comment state.
                                         */
                                        ClearLongStrBufAndAppend(c);
                                        //state = Transition(state, Tokenizer.BOGUS_COMMENT, reconsume, pos);
                                        state = TokenizerState.s44_BOGUS_COMMENT;
                                        goto continueStateloop;
                                    case '>':
                                        /*
                                         * U+003E GREATER-THAN SIGN (>) Parse error.
                                         */
                                        ErrLtGt();
                                        /*
                                         * Emit a U+003C LESS-THAN SIGN character token
                                         * and a U+003E GREATER-THAN SIGN character
                                         * token.
                                         */
                                        TokenListener.Characters(LT_GT, 0, 2);
                                        /* Switch to the data state. */
                                        //cstart = pos + 1;
                                        reader.SkipOneAndStartCollect();
                                        //state = Transition(state, Tokenizer.DATA, reconsume, pos);
                                        state = TokenizerState.s01_DATA;
                                        goto continueStateloop;
                                    default:
                                        /*
                                         * Anything else Parse error.
                                         */
                                        ErrBadCharAfterLt(c);
                                        /*
                                         * Emit a U+003C LESS-THAN SIGN character token
                                         */
                                        TokenListener.Characters(LT_GT, 0, 1);
                                        /*
                                         * and reconsume the current input character in
                                         * the data state.
                                         */
                                        reader.StartCollect();
                                        //state = Transition(state, Tokenizer.DATA, reconsume, pos);
                                        state = TokenizerState.s01_DATA;
                                        //reconsume = true;
                                        reader.StepBack();
                                        goto continueStateloop;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakTagopenloop:
                            goto case TokenizerState.s10_TAG_NAME;
                        }
                    //  FALL THROUGH DON'T REORDER
                    case TokenizerState.s10_TAG_NAME:
                        /*tagnameloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                /*
                                 * Consume the next input character:
                                 */
                                switch (c)
                                {
                                    case '\r':
                                        SilentCarriageReturn();
                                        StrBufToElementNameString();
                                        //state = Transition(state, Tokenizer.BEFORE_ATTRIBUTE_NAME, reconsume, pos);
                                        state = TokenizerState.s34_BEFORE_ATTRIBUTE_NAME;
                                        goto breakStateloop;
                                    case '\n':
                                    case ' ':
                                    case '\t':
                                    case '\u000C':
                                        /*
                                         * U+0009 CHARACTER TABULATION U+000A LINE FEED
                                         * (LF) U+000C FORM FEED (FF) U+0020 SPACE
                                         * Switch to the before attribute name state.
                                         */
                                        StrBufToElementNameString();
                                        //state = Transition(state, Tokenizer.BEFORE_ATTRIBUTE_NAME, reconsume, pos);
                                        state = TokenizerState.s34_BEFORE_ATTRIBUTE_NAME;
                                        goto breakTagnameloop;
                                    // goto continueStateloop;
                                    case '/':
                                        /*
                                         * U+002F SOLIDUS (/) Switch to the self-closing
                                         * start tag state.
                                         */
                                        StrBufToElementNameString();
                                        //state = Transition(state, Tokenizer.SELF_CLOSING_START_TAG, reconsume, pos);
                                        state = TokenizerState.s43_SELF_CLOSING_START_TAG;
                                        goto continueStateloop;
                                    case '>':
                                        /*
                                         * U+003E GREATER-THAN SIGN (>) Emit the current
                                         * tag token.
                                         */
                                        StrBufToElementNameString();
                                        //state = Transition(state, EmitCurrentTagToken(false, pos), reconsume, pos);
                                        state = EmitCurrentTagToken(false);
                                        if (shouldSuspend)
                                        {
                                            goto breakStateloop;
                                        }
                                        /*
                                         * Switch to the data state.
                                         */
                                        goto continueStateloop;
                                    case '\u0000':
                                        c = '\uFFFD';
                                        goto default;
                                    // fall thru
                                    default:
                                        if (c >= 'A' && c <= 'Z')
                                        {
                                            /*
                                             * U+0041 LATIN CAPITAL LETTER A through to
                                             * U+005A LATIN CAPITAL LETTER Z Append the
                                             * lowercase TokenizerState.version of the current input
                                             * character (add 0x0020 to the character's
                                             * code point) to the current tag token's
                                             * tag name.
                                             */
                                            c += (char)0x20;
                                        }
                                        /*
                                         * Anything else Append the current input
                                         * character to the current tag token's tag
                                         * name.
                                         */
                                        AppendStrBuf(c);
                                        /*
                                         * Stay in the tag name state.
                                         */
                                        continue;
                                }
                            }

                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakTagnameloop:
                            goto case TokenizerState.s34_BEFORE_ATTRIBUTE_NAME;
                        }
                    // FALLTHRU DON'T REORDER
                    case TokenizerState.s34_BEFORE_ATTRIBUTE_NAME:
                        /*beforeattributenameloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                switch (c)
                                {
                                    case '\r':
                                        SilentCarriageReturn();
                                        goto breakStateloop;
                                    case '\n':
                                    case ' ':
                                    case '\t':
                                    case '\u000C':
                                        /*
                                         * U+0009 CHARACTER TABULATION U+000A LINE FEED
                                         * (LF) U+000C FORM FEED (FF) U+0020 SPACE Stay
                                         * in the before attribute name state.
                                         */
                                        continue;
                                    case '/':
                                        /*
                                         * U+002F SOLIDUS (/) Switch to the self-closing
                                         * start tag state.
                                         */
                                        //state = Transition(state, Tokenizer.SELF_CLOSING_START_TAG, reconsume, pos);
                                        state = TokenizerState.s43_SELF_CLOSING_START_TAG;

                                        goto continueStateloop;
                                    case '>':
                                        /*
                                         * U+003E GREATER-THAN SIGN (>) Emit the current
                                         * tag token.
                                         */
                                        //state = Transition(state, EmitCurrentTagToken(false, pos), reconsume, pos);
                                        state = EmitCurrentTagToken(false);
                                        if (shouldSuspend)
                                        {
                                            goto breakStateloop;
                                        }
                                        /*
                                         * Switch to the data state.
                                         */
                                        goto continueStateloop;
                                    case '\u0000':
                                        c = '\uFFFD';
                                        // fall thru
                                        goto case '\"';
                                    case '\"':
                                    case '\'':
                                    case '<':
                                    case '=':
                                        /*
                                         * U+0022 QUOTATION MARK (") U+0027 APOSTROPHE
                                         * (') U+003C LESS-THAN SIGN (<) U+003D EQUALS
                                         * SIGN (=) Parse error.
                                         */
                                        ErrBadCharBeforeAttributeNameOrNull(c);
                                        /*
                                         * Treat it as per the "anything else" entry
                                         * below.
                                         */
                                        goto default;
                                    default:
                                        /*
                                         * Anything else Start a new attribute in the
                                         * current tag token.
                                         */
                                        if (c >= 'A' && c <= 'Z')
                                        {
                                            /*
                                             * U+0041 LATIN CAPITAL LETTER A through to
                                             * U+005A LATIN CAPITAL LETTER Z Set that
                                             * attribute's name to the lowercase TokenizerState.version
                                             * of the current input character (add
                                             * 0x0020 to the character's code point)
                                             */
                                            c += (char)0x20;
                                        }
                                        /*
                                         * Set that attribute's name to the current
                                         * input character,
                                         */
                                        ClearStrBufAndAppend(c);
                                        /*
                                         * and its value to the empty string.
                                         */
                                        // Will do later.
                                        /*
                                         * Switch to the attribute name state.
                                         */
                                        //state = Transition(state, Tokenizer.ATTRIBUTE_NAME, reconsume, pos);
                                        state = TokenizerState.s35_ATTRIBUTE_NAME;
                                        goto breakBeforeattributenameloop;
                                    // goto continueStateloop;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakBeforeattributenameloop:
                            goto case TokenizerState.s35_ATTRIBUTE_NAME;
                        }
                    // FALLTHRU DON'T REORDER
                    case TokenizerState.s35_ATTRIBUTE_NAME:
                        /*attributenameloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {
                                /*
                                 * Consume the next input character:
                                 */
                                switch (c)
                                {
                                    case '\r':
                                        SilentCarriageReturn();
                                        AttributeNameComplete();
                                        //state = Transition(state, Tokenizer.AFTER_ATTRIBUTE_NAME, reconsume, pos);
                                        state = TokenizerState.s36_AFTER_ATTRIBUTE_NAME;
                                        goto breakStateloop;
                                    case '\n':
                                    case ' ':
                                    case '\t':
                                    case '\u000C':
                                        /*
                                         * U+0009 CHARACTER TABULATION U+000A LINE FEED
                                         * (LF) U+000C FORM FEED (FF) U+0020 SPACE
                                         * Switch to the after attribute name state.
                                         */
                                        AttributeNameComplete();
                                        //state = Transition(state, Tokenizer.AFTER_ATTRIBUTE_NAME, reconsume, pos);
                                        state = TokenizerState.s36_AFTER_ATTRIBUTE_NAME;
                                        goto continueStateloop;
                                    case '/':
                                        /*
                                         * U+002F SOLIDUS (/) Switch to the self-closing
                                         * start tag state.
                                         */
                                        AttributeNameComplete();
                                        AddAttributeWithoutValue();
                                        //state = Transition(state, Tokenizer.SELF_CLOSING_START_TAG, reconsume, pos);
                                        state = TokenizerState.s43_SELF_CLOSING_START_TAG;
                                        goto continueStateloop;
                                    case '=':
                                        /*
                                         * U+003D EQUALS SIGN (=) Switch to the before
                                         * attribute value state.
                                         */
                                        AttributeNameComplete();
                                        //state = Transition(state, Tokenizer.BEFORE_ATTRIBUTE_VALUE, reconsume, pos);
                                        state = TokenizerState.s37_BEFORE_ATTRIBUTE_VALUE;
                                        goto breakAttributenameloop;
                                    // goto continueStateloop;
                                    case '>':
                                        /*
                                         * U+003E GREATER-THAN SIGN (>) Emit the current
                                         * tag token.
                                         */
                                        AttributeNameComplete();
                                        AddAttributeWithoutValue();
                                        //state = Transition(state, EmitCurrentTagToken(false, pos), reconsume, pos);
                                        state = EmitCurrentTagToken(false);
                                        if (shouldSuspend)
                                        {
                                            goto breakStateloop;
                                        }
                                        /*
                                         * Switch to the data state.
                                         */
                                        goto continueStateloop;
                                    case '\u0000':
                                        c = '\uFFFD';
                                        // fall thru
                                        goto case '\"';
                                    case '\"':
                                    case '\'':
                                    case '<':
                                        /*
                                         * U+0022 QUOTATION MARK (") U+0027 APOSTROPHE
                                         * (') U+003C LESS-THAN SIGN (<) Parse error.
                                         */
                                        ErrQuoteOrLtInAttributeNameOrNull(c);
                                        /*
                                         * Treat it as per the "anything else" entry
                                         * below.
                                         */
                                        goto default;
                                    default:
                                        if (c >= 'A' && c <= 'Z')
                                        {
                                            /*
                                             * U+0041 LATIN CAPITAL LETTER A through to
                                             * U+005A LATIN CAPITAL LETTER Z Append the
                                             * lowercase TokenizerState.version of the current input
                                             * character (add 0x0020 to the character's
                                             * code point) to the current attribute's
                                             * name.
                                             */
                                            c += (char)0x20;
                                        }
                                        /*
                                         * Anything else Append the current input
                                         * character to the current attribute's name.
                                         */
                                        AppendStrBuf(c);
                                        /*
                                         * Stay in the attribute name state.
                                         */
                                        continue;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakAttributenameloop:
                            goto case TokenizerState.s37_BEFORE_ATTRIBUTE_VALUE;
                        }
                    // FALLTHRU DON'T REORDER
                    case TokenizerState.s37_BEFORE_ATTRIBUTE_VALUE:
                        /*beforeattributevalueloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                switch (c)
                                {
                                    case '\r':
                                        SilentCarriageReturn();
                                        goto breakStateloop;
                                    case '\n':
                                    case ' ':
                                    case '\t':
                                    case '\u000C':
                                        /*
                                         * U+0009 CHARACTER TABULATION U+000A LINE FEED
                                         * (LF) U+000C FORM FEED (FF) U+0020 SPACE Stay
                                         * in the before attribute value state.
                                         */
                                        continue;
                                    case '"':
                                        /*
                                         * U+0022 QUOTATION MARK (") Switch to the
                                         * attribute value (double-quoted) state.
                                         */
                                        ClearLongStrBuf();
                                        //state = Transition(state, Tokenizer.ATTRIBUTE_VALUE_DOUBLE_QUOTED, reconsume, pos);
                                        state = TokenizerState.s38_ATTRIBUTE_VALUE_DOUBLE_QUOTED;

                                        goto breakBeforeattributevalueloop;
                                    // goto continueStateloop;
                                    case '&':
                                        /*
                                         * U+0026 AMPERSAND (&) Switch to the attribute
                                         * value (unquoted) state and reconsume this
                                         * input character.
                                         */
                                        ClearLongStrBuf();
                                        //state = Transition(state, Tokenizer.ATTRIBUTE_VALUE_UNQUOTED, reconsume, pos);
                                        state = TokenizerState.s40_ATTRIBUTE_VALUE_UNQUOTED;
                                        NoteUnquotedAttributeValue();
                                        //reconsume = true;
                                        reader.StepBack();
                                        goto continueStateloop;
                                    case '\'':
                                        /*
                                         * U+0027 APOSTROPHE (') Switch to the attribute
                                         * value (single-quoted) state.
                                         */
                                        ClearLongStrBuf();
                                        //state = Transition(state, Tokenizer.ATTRIBUTE_VALUE_SINGLE_QUOTED, reconsume, pos);
                                        state = TokenizerState.s39_ATTRIBUTE_VALUE_SINGLE_QUOTED;
                                        goto continueStateloop;
                                    case '>':
                                        /*
                                         * U+003E GREATER-THAN SIGN (>) Parse error.
                                         */
                                        ErrAttributeValueMissing();
                                        /*
                                         * Emit the current tag token.
                                         */
                                        AddAttributeWithoutValue();
                                        //state = Transition(state, EmitCurrentTagToken(false, pos), reconsume, pos);
                                        state = EmitCurrentTagToken(false);
                                        if (shouldSuspend)
                                        {
                                            goto breakStateloop;
                                        }
                                        /*
                                         * Switch to the data state.
                                         */
                                        goto continueStateloop;
                                    case '\u0000':
                                        c = '\uFFFD';
                                        // fall thru
                                        goto case '<';
                                    case '<':
                                    case '=':
                                    case '`':
                                        /*
                                         * U+003C LESS-THAN SIGN (<) U+003D EQUALS SIGN
                                         * (=) U+0060 GRAVE ACCENT (`)
                                         */
                                        ErrLtOrEqualsOrGraveInUnquotedAttributeOrNull(c);
                                        /*
                                         * Treat it as per the "anything else" entry
                                         * below.
                                         */
                                        goto default;
                                    default:
                                        // [NOCPP[
                                        ErrHtml4NonNameInUnquotedAttribute(c);
                                        // ]NOCPP]
                                        /*
                                         * Anything else Append the current input
                                         * character to the current attribute's value.
                                         */
                                        ClearLongStrBufAndAppend(c);
                                        /*
                                         * Switch to the attribute value (unquoted)
                                         * state.
                                         */

                                        //state = Transition(state, Tokenizer.ATTRIBUTE_VALUE_UNQUOTED, reconsume, pos);
                                        state = TokenizerState.s40_ATTRIBUTE_VALUE_UNQUOTED;

                                        NoteUnquotedAttributeValue();
                                        goto continueStateloop;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakBeforeattributevalueloop:
                            goto case TokenizerState.s38_ATTRIBUTE_VALUE_DOUBLE_QUOTED;
                        }

                    // FALLTHRU DON'T REORDER
                    case TokenizerState.s38_ATTRIBUTE_VALUE_DOUBLE_QUOTED:
                        /*attributevaluedoublequotedloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                switch (c)
                                {
                                    case '"':
                                        /*
                                         * U+0022 QUOTATION MARK (") Switch to the after
                                         * attribute value (quoted) state.
                                         */
                                        AddAttributeWithValue();

                                        //state = Transition(state, Tokenizer.AFTER_ATTRIBUTE_VALUE_QUOTED, reconsume, pos);
                                        state = TokenizerState.s42__AFTER_ATTRIBUTE_VALUE_QUOTED;
                                        goto breakAttributevaluedoublequotedloop;
                                    // goto continueStateloop;
                                    case '&':
                                        /*
                                         * U+0026 AMPERSAND (&) Switch to the character
                                         * reference in attribute value state, with the
                                         * additional allowed character being U+0022
                                         * QUOTATION MARK (").
                                         */
                                        ClearStrBufAndAppend(c);
                                        SetAdditionalAndRememberAmpersandLocation('\"');
                                        returnState = state;
                                        //state = Transition(state, Tokenizer.CONSUME_CHARACTER_REFERENCE, reconsume, pos);
                                        state = TokenizerState.CONSUME_CHARACTER_REFERENCE;

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
                                         * Anything else Append the current input
                                         * character to the current attribute's value.
                                         */
                                        AppendLongStrBuf(c);
                                        /*
                                         * Stay in the attribute value (double-quoted)
                                         * state.
                                         */
                                        continue;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakAttributevaluedoublequotedloop:
                            goto case TokenizerState.s42__AFTER_ATTRIBUTE_VALUE_QUOTED;
                        }
                    // FALLTHRU DON'T REORDER
                    case TokenizerState.s42__AFTER_ATTRIBUTE_VALUE_QUOTED:
                        /*afterattributevaluequotedloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                switch (c)
                                {
                                    case '\r':
                                        SilentCarriageReturn();
                                        //state = Transition(state, Tokenizer.BEFORE_ATTRIBUTE_NAME, reconsume, pos);
                                        state = TokenizerState.s34_BEFORE_ATTRIBUTE_NAME;
                                        goto breakStateloop;
                                    case '\n':
                                    case ' ':
                                    case '\t':
                                    case '\u000C':
                                        /*
                                         * U+0009 CHARACTER TABULATION U+000A LINE FEED
                                         * (LF) U+000C FORM FEED (FF) U+0020 SPACE
                                         * Switch to the before attribute name state.
                                         */
                                        //state = Transition(state, Tokenizer.BEFORE_ATTRIBUTE_NAME, reconsume, pos);
                                        state = TokenizerState.s34_BEFORE_ATTRIBUTE_NAME;
                                        goto continueStateloop;
                                    case '/':
                                        /*
                                         * U+002F SOLIDUS (/) Switch to the self-closing
                                         * start tag state.
                                         */
                                        //state = Transition(state, Tokenizer.SELF_CLOSING_START_TAG, reconsume, pos);
                                        state = TokenizerState.s43_SELF_CLOSING_START_TAG;
                                        goto breakAfterattributevaluequotedloop;
                                    // goto continueStateloop;
                                    case '>':
                                        /*
                                         * U+003E GREATER-THAN SIGN (>) Emit the current
                                         * tag token.
                                         */
                                        //state = Transition(state, EmitCurrentTagToken(false, pos), reconsume, pos);
                                        state = EmitCurrentTagToken(false);
                                        if (shouldSuspend)
                                        {
                                            goto breakStateloop;
                                        }
                                        /*
                                         * Switch to the data state.
                                         */
                                        goto continueStateloop;
                                    default:
                                        /*
                                         * Anything else Parse error.
                                         */
                                        ErrNoSpaceBetweenAttributes();
                                        /*
                                         * Reconsume the character in the before
                                         * attribute name state.
                                         */
                                        //state = Transition(state, Tokenizer.BEFORE_ATTRIBUTE_NAME, reconsume, pos);
                                        state = TokenizerState.s34_BEFORE_ATTRIBUTE_NAME;
                                        //reconsume = true;
                                        reader.StepBack();
                                        goto continueStateloop;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakAfterattributevaluequotedloop:
                            goto case TokenizerState.s43_SELF_CLOSING_START_TAG;
                        }
                    // FALLTHRU DON'T REORDER
                    case TokenizerState.s43_SELF_CLOSING_START_TAG:
                        {
                            char c;
                            if (!reader.ReadNext(out c))
                            {
                                goto breakStateloop;
                            }
                            //---------------------------------
                            /*
                             * Consume the next input character:
                             */
                            switch (c)
                            {
                                case '>':
                                    /*
                                     * U+003E GREATER-THAN SIGN (>) Set the self-closing
                                     * flag of the current tag token. Emit the current
                                     * tag token.
                                     */
                                    // [NOCPP[
                                    ErrHtml4XmlVoidSyntax();
                                    // ]NOCPP]
                                    //state = Transition(state, EmitCurrentTagToken(true, pos), reconsume, pos);
                                    state = EmitCurrentTagToken(true);
                                    if (shouldSuspend)
                                    {
                                        goto breakStateloop;
                                    }
                                    /*
                                     * Switch to the data state.
                                     */
                                    goto continueStateloop;
                                default:
                                    /* Anything else Parse error. */
                                    ErrSlashNotFollowedByGt();
                                    /*
                                     * Reconsume the character in the before attribute
                                     * name state.
                                     */
                                    //state = Transition(state, Tokenizer.BEFORE_ATTRIBUTE_NAME, reconsume, pos);
                                    state = TokenizerState.s34_BEFORE_ATTRIBUTE_NAME;
                                    reader.StepBack();
                                    //reconsume = true;
                                    goto continueStateloop;
                            }
                        }
                    // XXX reorder point
                    case TokenizerState.s40_ATTRIBUTE_VALUE_UNQUOTED:
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                switch (c)
                                {
                                    case '\r':
                                        SilentCarriageReturn();
                                        AddAttributeWithValue();
                                        //state = Transition(state, Tokenizer.BEFORE_ATTRIBUTE_NAME, reconsume, pos);
                                        state = TokenizerState.s34_BEFORE_ATTRIBUTE_NAME;
                                        goto breakStateloop;
                                    case '\n':
                                    case ' ':
                                    case '\t':
                                    case '\u000C':
                                        /*
                                         * U+0009 CHARACTER TABULATION U+000A LINE FEED
                                         * (LF) U+000C FORM FEED (FF) U+0020 SPACE
                                         * Switch to the before attribute name state.
                                         */
                                        AddAttributeWithValue();
                                        //state = Transition(state, Tokenizer.BEFORE_ATTRIBUTE_NAME, reconsume, pos);
                                        state = TokenizerState.s34_BEFORE_ATTRIBUTE_NAME;
                                        goto continueStateloop;
                                    case '&':
                                        /*
                                         * U+0026 AMPERSAND (&) Switch to the character
                                         * reference in attribute value state, with the
                                         * additional allowed character being U+003E
                                         * GREATER-THAN SIGN (>)
                                         */
                                        ClearStrBufAndAppend(c);
                                        SetAdditionalAndRememberAmpersandLocation('>');
                                        returnState = state;
                                        //state = Transition(state, Tokenizer.CONSUME_CHARACTER_REFERENCE, reconsume, pos);
                                        state = TokenizerState.CONSUME_CHARACTER_REFERENCE;
                                        goto continueStateloop;
                                    case '>':
                                        /*
                                         * U+003E GREATER-THAN SIGN (>) Emit the current
                                         * tag token.
                                         */
                                        AddAttributeWithValue();
                                        //state = Transition(state, EmitCurrentTagToken(false, pos), reconsume, pos);
                                        state = EmitCurrentTagToken(false);
                                        if (shouldSuspend)
                                        {
                                            goto breakStateloop;
                                        }
                                        /*
                                         * Switch to the data state.
                                         */
                                        goto continueStateloop;
                                    case '\u0000':
                                        c = '\uFFFD';
                                        goto case '<';
                                    // fall thru
                                    case '<':
                                    case '\"':
                                    case '\'':
                                    case '=':
                                    case '`':
                                        /*
                                         * U+0022 QUOTATION MARK (") U+0027 APOSTROPHE
                                         * (') U+003C LESS-THAN SIGN (<) U+003D EQUALS
                                         * SIGN (=) U+0060 GRAVE ACCENT (`) Parse error.
                                         */
                                        ErrUnquotedAttributeValOrNull(c);
                                        /*
                                         * Treat it as per the "anything else" entry
                                         * below.
                                         */
                                        // fall through
                                        goto default;
                                    default:
                                        // [NOCPP]
                                        ErrHtml4NonNameInUnquotedAttribute(c);
                                        // ]NOCPP]
                                        /*
                                         * Anything else Append the current input
                                         * character to the current attribute's value.
                                         */
                                        AppendLongStrBuf(c);
                                        /*
                                         * Stay in the attribute value (unquoted) state.
                                         */
                                        continue;
                                }
                            }
                            //-------------------------------
                            goto breakStateloop;
                        }
                    // XXX reorder point
                    case TokenizerState.s36_AFTER_ATTRIBUTE_NAME:
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {
                                switch (c)
                                {
                                    case '\r':
                                        SilentCarriageReturn();
                                        goto breakStateloop;
                                    case '\n':
                                    case ' ':
                                    case '\t':
                                    case '\u000C':
                                        /*
                                         * U+0009 CHARACTER TABULATION U+000A LINE FEED
                                         * (LF) U+000C FORM FEED (FF) U+0020 SPACE Stay
                                         * in the after attribute name state.
                                         */
                                        continue;
                                    case '/':
                                        /*
                                         * U+002F SOLIDUS (/) Switch to the self-closing
                                         * start tag state.
                                         */
                                        AddAttributeWithoutValue();
                                        //state = Transition(state, Tokenizer.SELF_CLOSING_START_TAG, reconsume, pos);
                                        state = TokenizerState.s43_SELF_CLOSING_START_TAG;
                                        goto continueStateloop;
                                    case '=':
                                        /*
                                         * U+003D EQUALS SIGN (=) Switch to the before
                                         * attribute value state.
                                         */
                                        //state = Transition(state, Tokenizer.BEFORE_ATTRIBUTE_VALUE, reconsume, pos);
                                        state = TokenizerState.s37_BEFORE_ATTRIBUTE_VALUE;
                                        goto continueStateloop;
                                    case '>':
                                        /*
                                         * U+003E GREATER-THAN SIGN (>) Emit the current
                                         * tag token.
                                         */
                                        AddAttributeWithoutValue();
                                        //state = Transition(state, EmitCurrentTagToken(false, pos), reconsume, pos);
                                        state = EmitCurrentTagToken(false);
                                        if (shouldSuspend)
                                        {
                                            goto breakStateloop;
                                        }
                                        /*
                                         * Switch to the data state.
                                         */
                                        goto continueStateloop;
                                    case '\u0000':
                                        c = '\uFFFD';
                                        goto case '\"';
                                    // fall thru
                                    case '\"':
                                    case '\'':
                                    case '<':
                                        ErrQuoteOrLtInAttributeNameOrNull(c);
                                        /*
                                         * Treat it as per the "anything else" entry
                                         * below.
                                         */
                                        goto default;
                                    default:
                                        AddAttributeWithoutValue();
                                        /*
                                         * Anything else Start a new attribute in the
                                         * current tag token.
                                         */
                                        if (c >= 'A' && c <= 'Z')
                                        {
                                            /*
                                             * U+0041 LATIN CAPITAL LETTER A through to
                                             * U+005A LATIN CAPITAL LETTER Z Set that
                                             * attribute's name to the lowercase TokenizerState.version
                                             * of the current input character (add
                                             * 0x0020 to the character's code point)
                                             */
                                            c += (char)0x20;
                                        }
                                        /*
                                         * Set that attribute's name to the current
                                         * input character,
                                         */
                                        ClearStrBufAndAppend(c);
                                        /*
                                         * and its value to the empty string.
                                         */
                                        // Will do later.
                                        /*
                                         * Switch to the attribute name state.
                                         */
                                        //state = Transition(state, Tokenizer.ATTRIBUTE_NAME, reconsume, pos);
                                        state = TokenizerState.s35_ATTRIBUTE_NAME;
                                        goto continueStateloop;
                                }
                            }
                            //-------------------------------
                            //eof
                            goto breakStateloop;
                        }

                    // XXX reorder point
                    case TokenizerState.s39_ATTRIBUTE_VALUE_SINGLE_QUOTED:
                        /*attributevaluesinglequotedloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {
                                /*
                                 * Consume the next input character:
                                 */
                                switch (c)
                                {
                                    case '\'':
                                        /*
                                         * U+0027 APOSTROPHE (') Switch to the after
                                         * attribute value (quoted) state.
                                         */
                                        AddAttributeWithValue();

                                        //state = Transition(state, Tokenizer.AFTER_ATTRIBUTE_VALUE_QUOTED, reconsume, pos);
                                        state = TokenizerState.s42__AFTER_ATTRIBUTE_VALUE_QUOTED;
                                        goto continueStateloop;
                                    case '&':
                                        /*
                                         * U+0026 AMPERSAND (&) Switch to the character
                                         * reference in attribute value state, with the
                                         * + additional allowed character being U+0027
                                         * APOSTROPHE (').
                                         */
                                        ClearStrBufAndAppend(c);
                                        SetAdditionalAndRememberAmpersandLocation('\'');
                                        returnState = state;
                                        //state = Transition(state, Tokenizer.CONSUME_CHARACTER_REFERENCE, reconsume, pos);
                                        state = TokenizerState.CONSUME_CHARACTER_REFERENCE;
                                        goto breakAttributevaluesinglequotedloop;
                                    // goto continueStateloop;
                                    case '\r':
                                        AppendLongStrBufCarriageReturn();
                                        goto breakStateloop;
                                    case '\n':
                                        AppendLongStrBufLineFeed();
                                        continue;
                                    case '\u0000':
                                        c = '\uFFFD';
                                        goto default;
                                    // fall thru
                                    default:
                                        /*
                                         * Anything else Append the current input
                                         * character to the current attribute's value.
                                         */
                                        AppendLongStrBuf(c);
                                        /*
                                         * Stay in the attribute value (double-quoted)
                                         * state.
                                         */
                                        continue;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakAttributevaluesinglequotedloop:
                            goto case TokenizerState.CONSUME_CHARACTER_REFERENCE;
                        }
                    // FALLTHRU DON'T REORDER
                    case TokenizerState.CONSUME_CHARACTER_REFERENCE:
                        {
                            char c;
                            if (!reader.ReadNext(out c))
                            {
                                //------------------------------------
                                //eof
                                goto breakStateloop;
                            }
                            if (c == '\u0000')
                            {
                                goto breakStateloop;
                            }
                            /*
                             * Unlike the definition is the spec, this state does not
                             * return a value and never requires the caller to
                             * backtrack. This state takes care of emitting characters
                             * or appending to the current attribute value. It also
                             * takes care of that in the case TokenizerState.when consuming the
                             * character reference fails.
                             */
                            /*
                             * This section defines how to consume a character
                             * reference. This definition is used when parsing character
                             * references in text and in attributes.
                             * 
                             * The behavior depends on the identity of the next
                             * character (the one immediately after the U+0026 AMPERSAND
                             * character):
                             */
                            switch (c)
                            {
                                case ' ':
                                case '\t':
                                case '\n':
                                case '\r': // we'll reconsume!
                                case '\u000C':
                                case '<':
                                case '&':
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
                                case '#':
                                    /*
                                     * U+0023 NUMBER SIGN (#) Consume the U+0023 NUMBER
                                     * SIGN.
                                     */
                                    AppendStrBuf('#');
                                    //state = Transition(state, Tokenizer.CONSUME_NCR, reconsume, pos);
                                    state = TokenizerState.CONSUME_NCR;
                                    goto continueStateloop;
                                default:
                                    if (c == additional)
                                    {
                                        EmitOrAppendStrBuf(returnState);
                                        //state = Transition(state, returnState, reconsume, pos);
                                        state = returnState;
                                        //reconsume = true;
                                        reader.StepBack();
                                        goto continueStateloop;
                                    }
                                    if (c >= 'a' && c <= 'z')
                                    {
                                        firstCharKey = c - 'a' + 26;
                                    }
                                    else if (c >= 'A' && c <= 'Z')
                                    {
                                        firstCharKey = c - 'A';
                                    }
                                    else
                                    {
                                        // No match
                                        /*
                                         * If no match can be made, then this is a parse
                                         * error.
                                         */
                                        ErrNoNamedCharacterMatch();
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
                                    // Didn't fail yet
                                    AppendStrBuf(c);
                                    //state = Transition(state, Tokenizer.CHARACTER_REFERENCE_HILO_LOOKUP, reconsume, pos);
                                    state = TokenizerState.CHARACTER_REFERENCE_HILO_LOOKUP;

                                    // FALL THROUGH goto continueStateloop;
                                    break;
                            }
                            //------------------------------------
                            goto case TokenizerState.CHARACTER_REFERENCE_HILO_LOOKUP;
                        }
                    // WARNING FALLTHRU case TokenizerState.TRANSITION: DON'T REORDER
                    case TokenizerState.CHARACTER_REFERENCE_HILO_LOOKUP:
                        {
                            char c;
                            if (reader.ReadNext(out c))
                            {
                                //------------------------------------
                                //eof
                                goto breakStateloop;
                            }

                            if (c == '\u0000')
                            {
                                goto breakStateloop;
                            }
                            /*
                             * The data structure is as follows:
                             * 
                             * HILO_ACCEL is a two-dimensional int array whose major
                             * index corresponds to the second character of the
                             * character reference (code point as index) and the
                             * minor index corresponds to the first character of the
                             * character reference (packed so that A-Z runs from 0
                             * to 25 and a-z runs from 26 to 51). This layout makes
                             * it easier to use the sparseness of the data structure
                             * to omit parts of it: The second dimension of the
                             * table is null when no character reference starts with
                             * the character corresponding to that row.
                             * 
                             * The int value HILO_ACCEL (by these indeces) is zero
                             * if there exists no character reference starting with
                             * that two-letter prefix. Otherwise, the value is an
                             * int that packs two shorts so that the higher short is
                             * the index of the highest character reference name
                             * with that prefix in NAMES and the lower short
                             * corresponds to the index of the lowest character
                             * reference name with that prefix. (It happens that the
                             * first two character reference names share their
                             * prefix so the packed int cannot be 0 by packing the
                             * two shorts.)
                             * 
                             * NAMES is an array of byte arrays where each byte
                             * array encodes the name of a character references as
                             * ASCII. The names omit the first two letters of the
                             * name. (Since storing the first two letters would be
                             * redundant with the data contained in HILO_ACCEL.) The
                             * entries are lexically sorted.
                             * 
                             * For a given index in NAMES, the same index in VALUES
                             * contains the corresponding expansion as an array of
                             * two UTF-16 code units (either the character and
                             * U+0000 or a suggogate pair).
                             */
                            int hilo = 0;
                            if (c <= 'z')
                            {
                                int[] row = NamedCharactersAccel.HILO_ACCEL[c];
                                if (row != null)
                                {
                                    hilo = row[firstCharKey];
                                }
                            }
                            if (hilo == 0)
                            {
                                /*
                                 * If no match can be made, then this is a parse
                                 * error.
                                 */
                                ErrNoNamedCharacterMatch();
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
                            // Didn't fail yet
                            AppendStrBuf(c);
                            lo = hilo & 0xFFFF;
                            hi = hilo >> 16;
                            entCol = -1;
                            candidate = -1;
                            strBufMark = 0;
                            //state = Transition(state, Tokenizer.CHARACTER_REFERENCE_TAIL, reconsume, pos);
                            state = TokenizerState.CHARACTER_REFERENCE_TAIL;
                            // FALL THROUGH goto continueStateloop;
                            goto case TokenizerState.CHARACTER_REFERENCE_TAIL;
                        }
                    case TokenizerState.CHARACTER_REFERENCE_TAIL:
                        /*outer:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {
                                if (c == '\u0000')
                                {
                                    goto breakStateloop;
                                }
                                entCol++;
                                /*
                                 * Consume the maximum number of characters possible,
                                 * with the consumed characters matching one of the
                                 * identifiers in the first column of the named
                                 * character references table (in a case-sensitive
                                 * manner).
                                 */
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

                                /*hiloop:*/
                                for (; ; )
                                {
                                    if (hi < lo)
                                    {
                                        goto breakOuter;
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

                                if (hi < lo)
                                {
                                    goto breakOuter;
                                }
                                AppendStrBuf(c);
                                continue;
                            }

                        breakOuter:

                            if (candidate == -1)
                            {
                                // reconsume deals with CR, LF or nul
                                /*
                                 * If no match can be made, then this is a parse error.
                                 */
                                ErrNoNamedCharacterMatch();
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
                                // c can't be CR, LF or nul if we got here
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
                                            ch = c;
                                        }
                                        else
                                        {
                                            // if (strBufOffset != -1) {
                                            // ch = buf[strBufOffset + strBufMark];
                                            // } else {
                                            ch = this.strBuffer[strBufMark];
                                            // }
                                        }
                                        if (ch == '=' || (ch >= '0' && ch <= '9')
                                                || (ch >= 'A' && ch <= 'Z')
                                                || (ch >= 'a' && ch <= 'z'))
                                        {
                                            /*
                                             * and the next character is either a U+003D
                                             * EQUALS SIGN character (=) or in the range
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
                                            //state = Transition(state, returnState, reconsume, pos);
                                            state = returnState;
                                            //reconsume = true;
                                            reader.StepBack();
                                            goto continueStateloop;
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
                                if (strBufMark < this.strBuffer.Length)
                                {
                                    // if (strBufOffset != -1) {
                                    // if ((returnState & (~1)) != 0) {
                                    // for (int i = strBufMark; i < strBufLen; i++) {
                                    // appendLongStrBuf(buf[strBufOffset + i]);
                                    // }
                                    // } else {
                                    // tokenHandler.Characters(buf, strBufOffset
                                    // + strBufMark, strBufLen
                                    // - strBufMark);
                                    // }
                                    // } else {
                                    //if ((returnState & DATA_AND_RCDATA_MASK) != 0)
                                    if (((byte)returnState & DATA_AND_RCDATA_MASK) == 0)
                                    {
                                        int j = this.strBuffer.Length;
                                        for (int i = strBufMark; i < j; i++)
                                        {
                                            AppendLongStrBuf(strBuffer[i]);
                                        }
                                    }
                                    else
                                    {
                                        TokenListener.Characters(CopyFromStringBuiler(this.strBuffer, strBufMark, this.strBuffer.Length - strBufMark));
                                    }
                                    // }
                                }
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
                                /*
                                 * If the markup contains I'm &notit; I tell you, the
                                 * entity is parsed as "not", as in, I'm Â¬it; I tell
                                 * you. But if the markup was I'm &notin; I tell you,
                                 * the entity would be parsed as "notin;", resulting in
                                 * I'm âˆ‰ I tell you.
                                 */
                            }

                        }
                    // XXX reorder point
                    case TokenizerState.s09_CLOSE_TAG_OPEN:
                        {
                            char c;
                            if (!reader.ReadNext(out c))
                            {
                                //------------------------------------
                                //eof
                                goto breakStateloop;
                            }

                            /*
                             * Otherwise, if the content model flag is set to the PCDATA
                             * state, or if the next few characters do match that tag
                             * name, consume the next input character:
                             */
                            switch (c)
                            {
                                case '>':
                                    /* U+003E GREATER-THAN SIGN (>) Parse error. */
                                    ErrLtSlashGt();
                                    /*
                                     * Switch to the data state.
                                     */
                                    reader.SkipOneAndStartCollect();
                                    //state = Transition(state, Tokenizer.DATA, reconsume, pos);
                                    state = TokenizerState.s01_DATA;
                                    goto continueStateloop;
                                case '\r':
                                    SilentCarriageReturn();
                                    /* Anything else Parse error. */
                                    ErrGarbageAfterLtSlash();
                                    /*
                                     * Switch to the bogus comment state.
                                     */
                                    ClearLongStrBufAndAppend('\n');
                                    //state = Transition(state, Tokenizer.BOGUS_COMMENT, reconsume, pos);
                                    state = TokenizerState.s44_BOGUS_COMMENT;
                                    goto breakStateloop;
                                case '\n':
                                    /* Anything else Parse error. */
                                    ErrGarbageAfterLtSlash();
                                    /*
                                     * Switch to the bogus comment state.
                                     */
                                    ClearLongStrBufAndAppend('\n');
                                    //state = Transition(state, Tokenizer.BOGUS_COMMENT, reconsume, pos);
                                    state = TokenizerState.s44_BOGUS_COMMENT;
                                    goto continueStateloop;
                                case '\u0000':
                                    c = '\uFFFD';
                                    // fall thru
                                    goto default;
                                default:
                                    if (c >= 'A' && c <= 'Z')
                                    {
                                        c += (char)0x20;
                                    }
                                    if (c >= 'a' && c <= 'z')
                                    {
                                        /*
                                         * U+0061 LATIN SMALL LETTER A through to U+007A
                                         * LATIN SMALL LETTER Z Create a new end tag
                                         * token,
                                         */
                                        endTag = true;
                                        /*
                                         * set its tag name to the input character,
                                         */
                                        ClearStrBufAndAppend(c);
                                        /*
                                         * then switch to the tag name state. (Don't
                                         * emit the token yet; further details will be
                                         * filled in before it is emitted.)
                                         */
                                        //state = Transition(state, Tokenizer.TAG_NAME, reconsume, pos);
                                        state = TokenizerState.s10_TAG_NAME;
                                        goto continueStateloop;
                                    }
                                    else
                                    {
                                        /* Anything else Parse error. */
                                        ErrGarbageAfterLtSlash();
                                        /*
                                         * Switch to the bogus comment state.
                                         */
                                        ClearLongStrBufAndAppend(c);
                                        //state = Transition(state, Tokenizer.BOGUS_COMMENT, reconsume, pos);
                                        state = TokenizerState.s44_BOGUS_COMMENT;
                                        goto continueStateloop;
                                    }
                            }
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