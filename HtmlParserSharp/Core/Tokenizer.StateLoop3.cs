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
      
        
        void StateLoop3(TokenizerState state, TokenizerState returnState)
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

                    //TODO: remove  FALL THROUGH DON'T REORDER
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
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        }

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
                    // XXX reorder point
                    case TokenizerState.s07_PLAINTEXT:
                        /*plaintextloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                switch (c)
                                {
                                    case '\u0000':
                                        EmitPlaintextReplacementCharacter();
                                        continue;
                                    case '\r':
                                        EmitCarriageReturn();
                                        goto breakStateloop;
                                    case '\n':
                                    default:
                                        /*
                                         * Anything else Emit the current input
                                         * character as a character token. Stay in the
                                         * RAWTEXT state.
                                         */
                                        continue;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
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
                    // XXX reorder point
                    case TokenizerState.s03_RCDATA:
                        /*rcdataloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                switch (c)
                                {
                                    case '&':
                                        /*
                                         * U+0026 AMPERSAND (&) Switch to the character
                                         * reference in RCDATA state.
                                         */
                                        //FlushChars(buf, pos);
                                        FlushChars();
                                        ClearStrBufAndAppend(c);
                                        additional = '\u0000';
                                        returnState = state;
                                        //state = Transition(state, Tokenizer.CONSUME_CHARACTER_REFERENCE, reconsume, pos);
                                        state = TokenizerState.CONSUME_CHARACTER_REFERENCE;
                                        goto continueStateloop;
                                    case '<':
                                        /*
                                         * U+003C LESS-THAN SIGN (<) Switch to the
                                         * RCDATA less-than sign state.
                                         */
                                        //FlushChars(buf, pos);
                                        FlushChars();
                                        returnState = state;
                                        //state = Transition(state, Tokenizer.RAWTEXT_RCDATA_LESS_THAN_SIGN, reconsume, pos);
                                        state = TokenizerState.s11_RAWTEXT_RCDATA_LESS_THAN_SIGN;
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
                                         * Emit the current input character as a
                                         * character token. Stay in the RCDATA state.
                                         */
                                        continue;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        }
                    // XXX reorder point
                    case TokenizerState.s05_RAWTEXT:
                        /*rawtextloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                switch (c)
                                {
                                    case '<':
                                        /*
                                         * U+003C LESS-THAN SIGN (<) Switch to the
                                         * RAWTEXT less-than sign state.
                                         */
                                        FlushChars();

                                        returnState = state;
                                        //state = Transition(state, Tokenizer.RAWTEXT_RCDATA_LESS_THAN_SIGN, reconsume, pos);
                                        state = TokenizerState.s11_RAWTEXT_RCDATA_LESS_THAN_SIGN;
                                        goto breakRawtextloop;
                                    // FALL THRU goto continueStateloop;
                                    case '\u0000':
                                        EmitReplacementCharacter();
                                        continue;
                                    case '\r':
                                        EmitCarriageReturn();
                                        goto breakStateloop;
                                    case '\n':
                                    default:
                                        /*
                                         * Emit the current input character as a
                                         * character token. Stay in the RAWTEXT state.
                                         */
                                        continue;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakRawtextloop:
                            goto case TokenizerState.s11_RAWTEXT_RCDATA_LESS_THAN_SIGN;
                        }
                    // XXX fallthru don't reorder
                    case TokenizerState.s11_RAWTEXT_RCDATA_LESS_THAN_SIGN:
                        /*rawtextrcdatalessthansignloop:*/
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
                                        goto breakRawtextrcdatalessthansignloop;
                                    // FALL THRU goto continueStateloop;
                                    default:
                                        /*
                                         * Otherwise, emit a U+003C LESS-THAN SIGN
                                         * character token
                                         */
                                        TokenListener.Characters(Tokenizer.LT_GT, 0, 1);
                                        /*
                                         * and reconsume the current input character in
                                         * the data state.
                                         */
                                        reader.StartCollect();
                                        //state = Transition(state, returnState, reconsume, pos);
                                        state = returnState;
                                        //reconsume = true;
                                        reader.StepBack();
                                        goto continueStateloop;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakRawtextrcdatalessthansignloop:
                            goto case TokenizerState.NON_DATA_END_TAG_NAME;
                        }
                    // XXX fall thru. don't reorder.
                    case TokenizerState.NON_DATA_END_TAG_NAME:
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                /*
                                 * ASSERT! when entering this state, set index to 0 and
                                 * call clearStrBuf() assert (contentModelElement !=
                                 * null); Let's implement the above without lookahead.
                                 * strBuf is the 'temporary buffer'.
                                 */
                                if (index < endTagExpectationAsArray.Length)
                                {
                                    char e = endTagExpectationAsArray[index];
                                    char folded = c;
                                    if (c >= 'A' && c <= 'Z')
                                    {
                                        folded += (char)0x20;
                                    }
                                    if (folded != e)
                                    {

                                        ErrHtml4LtSlashInRcdata(folded);
                                        TokenListener.Characters(Tokenizer.LT_SOLIDUS,
                                                0, 2);
                                        EmitStrBuf();
                                        reader.StartCollect();
                                        //state = Transition(state, returnState, reconsume, pos);
                                        state = returnState;
                                        //reconsume = true;
                                        reader.StepBack();
                                        goto continueStateloop;
                                    }
                                    AppendStrBuf(c);
                                    index++;
                                    continue;
                                }
                                else
                                {
                                    endTag = true;
                                    // XXX replace contentModelElement with different
                                    // type
                                    tagName = endTagExpectation;
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
                                             * U+0009 CHARACTER TABULATION U+000A LINE
                                             * FEED (LF) U+000C FORM FEED (FF) U+0020
                                             * SPACE If the current end tag token is an
                                             * appropriate end tag token, then switch to
                                             * the before attribute name state.
                                             */
                                            //state = Transition(state, Tokenizer.BEFORE_ATTRIBUTE_NAME, reconsume, pos);
                                            state = TokenizerState.s34_BEFORE_ATTRIBUTE_NAME;
                                            goto continueStateloop;
                                        case '/':
                                            /*
                                             * U+002F SOLIDUS (/) If the current end tag
                                             * token is an appropriate end tag token,
                                             * then switch to the self-closing start tag
                                             * state.
                                             */
                                            //state = Transition(state, Tokenizer.SELF_CLOSING_START_TAG, reconsume, pos);
                                            state = TokenizerState.s43_SELF_CLOSING_START_TAG;
                                            goto continueStateloop;
                                        case '>':
                                            /*
                                             * U+003E GREATER-THAN SIGN (>) If the
                                             * current end tag token is an appropriate
                                             * end tag token, then emit the current tag
                                             * token and switch to the data state.
                                             */
                                            //state = Transition(state, EmitCurrentTagToken(false, pos), reconsume, pos);
                                            state = EmitCurrentTagToken(false);
                                            if (shouldSuspend)
                                            {
                                                goto breakStateloop;
                                            }
                                            goto continueStateloop;
                                        default:
                                            /*
                                             * Emit a U+003C LESS-THAN SIGN character
                                             * token, a U+002F SOLIDUS character token,
                                             * a character token for each of the
                                             * characters in the temporary buffer (in
                                             * the order they were added to the buffer),
                                             * and reconsume the current input character
                                             * in the RAWTEXT state.
                                             */
                                            // [NOCPP[
                                            ErrWarnLtSlashInRcdata();
                                            // ]NOCPP]
                                            TokenListener.Characters(LT_SOLIDUS, 0, 2);
                                            EmitStrBuf();
                                            if (c == '\u0000')
                                            {
                                                EmitReplacementCharacter();
                                            }
                                            else
                                            {
                                                reader.StartCollect(); // don't drop the
                                                // character
                                            }
                                            //state = Transition(state, returnState, reconsume, pos);
                                            state = returnState;
                                            goto continueStateloop;
                                    }
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
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
                    // XXX reorder point
                    case TokenizerState.MARKUP_DECLARATION_OCTYPE:
                        /*markupdeclarationdoctypeloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                if (index < 6)
                                { // OCTYPE.Length
                                    char folded = c;
                                    if (c >= 'A' && c <= 'Z')
                                    {
                                        folded += (char)0x20;
                                    }
                                    if (folded == Tokenizer.OCTYPE[index])
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
                                    // state = Transition(state, Tokenizer.DOCTYPE, reconsume, pos);
                                    state = TokenizerState.s52_DOCTYPE;
                                    //reconsume = true;
                                    reader.StepBack();
                                    goto breakMarkupdeclarationdoctypeloop;
                                    // goto continueStateloop;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakMarkupdeclarationdoctypeloop:
                            goto case TokenizerState.s52_DOCTYPE;
                        }
                    // FALLTHRU DON'T REORDER
                    case TokenizerState.s52_DOCTYPE:
                        /*doctypeloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {
                                InitDoctypeFields();
                                /*
                                 * Consume the next input character:
                                 */
                                switch (c)
                                {
                                    case '\r':
                                        SilentCarriageReturn();
                                        //state = Transition(state, Tokenizer.BEFORE_DOCTYPE_NAME, reconsume, pos);
                                        state = TokenizerState.s53_BEFORE_DOCTYPE_NAME;
                                        goto breakStateloop;
                                    case '\n':
                                    case ' ':
                                    case '\t':
                                    case '\u000C':
                                        /*
                                         * U+0009 CHARACTER TABULATION U+000A LINE FEED
                                         * (LF) U+000C FORM FEED (FF) U+0020 SPACE
                                         * Switch to the before DOCTYPE name state.
                                         */
                                        //state = Transition(state, Tokenizer.BEFORE_DOCTYPE_NAME, reconsume, pos);
                                        state = TokenizerState.s53_BEFORE_DOCTYPE_NAME;
                                        goto breakDoctypeloop;
                                    // goto continueStateloop;
                                    default:
                                        /*
                                         * Anything else Parse error.
                                         */
                                        ErrMissingSpaceBeforeDoctypeName();
                                        /*
                                         * Reconsume the current character in the before
                                         * DOCTYPE name state.
                                         */
                                        //state = Transition(state, Tokenizer.BEFORE_DOCTYPE_NAME, reconsume, pos);
                                        state = TokenizerState.s53_BEFORE_DOCTYPE_NAME;
                                        //reconsume = true;
                                        reader.StepBack();
                                        goto breakDoctypeloop;
                                    // goto continueStateloop;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakDoctypeloop:
                            goto case TokenizerState.s53_BEFORE_DOCTYPE_NAME;
                        }
                    // FALLTHRU DON'T REORDER
                    case TokenizerState.s53_BEFORE_DOCTYPE_NAME:
                        /*beforedoctypenameloop:*/
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
                                         * in the before DOCTYPE name state.
                                         */
                                        continue;
                                    case '>':
                                        /*
                                         * U+003E GREATER-THAN SIGN (>) Parse error.
                                         */
                                        ErrNamelessDoctype();
                                        /*
                                         * Create a new DOCTYPE token. Set its
                                         * force-quirks flag to on.
                                         */
                                        forceQuirks = true;
                                        /*
                                         * Emit the token.
                                         */
                                        EmitDoctypeToken();
                                        /*
                                         * Switch to the data state.
                                         */
                                        //state = Transition(state, Tokenizer.DATA, reconsume, pos);
                                        state = TokenizerState.s01_DATA;
                                        goto continueStateloop;
                                    case '\u0000':
                                        c = '\uFFFD';
                                        // fall thru
                                        goto default;
                                    default:
                                        if (c >= 'A' && c <= 'Z')
                                        {
                                            /*
                                             * U+0041 LATIN CAPITAL LETTER A through to
                                             * U+005A LATIN CAPITAL LETTER Z Create a
                                             * new DOCTYPE token. Set the token's name
                                             * to the lowercase TokenizerState.version of the input
                                             * character (add 0x0020 to the character's
                                             * code point).
                                             */
                                            c += (char)0x20;
                                        }
                                        /* Anything else Create a new DOCTYPE token. */
                                        /*
                                         * Set the token's name name to the current
                                         * input character.
                                         */
                                        ClearStrBufAndAppend(c);
                                        /*
                                         * Switch to the DOCTYPE name state.
                                         */
                                        //state = Transition(state, Tokenizer.DOCTYPE_NAME, reconsume, pos);
                                        state = TokenizerState.s54_DOCTYPE_NAME;
                                        goto breakBeforedoctypenameloop;
                                    // goto continueStateloop;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakBeforedoctypenameloop:
                            goto case TokenizerState.s54_DOCTYPE_NAME;
                        }
                    // FALLTHRU DON'T REORDER
                    case TokenizerState.s54_DOCTYPE_NAME:
                        /*doctypenameloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                switch (c)
                                {
                                    case '\r':
                                        SilentCarriageReturn();
                                        StrBufToDoctypeName();
                                        //state = Transition(state, Tokenizer.AFTER_DOCTYPE_NAME, reconsume, pos);
                                        state = TokenizerState.s55_AFTER_DOCTYPE_NAME;
                                        goto breakStateloop;
                                    case '\n':
                                    case ' ':
                                    case '\t':
                                    case '\u000C':
                                        /*
                                         * U+0009 CHARACTER TABULATION U+000A LINE FEED
                                         * (LF) U+000C FORM FEED (FF) U+0020 SPACE
                                         * Switch to the after DOCTYPE name state.
                                         */
                                        StrBufToDoctypeName();
                                        //state = Transition(state, Tokenizer.AFTER_DOCTYPE_NAME, reconsume, pos);
                                        state = TokenizerState.s55_AFTER_DOCTYPE_NAME;
                                        goto breakDoctypenameloop;
                                    // goto continueStateloop;
                                    case '>':
                                        /*
                                         * U+003E GREATER-THAN SIGN (>) Emit the current
                                         * DOCTYPE token.
                                         */
                                        StrBufToDoctypeName();
                                        EmitDoctypeToken();
                                        /*
                                         * Switch to the data state.
                                         */
                                        //state = Transition(state, Tokenizer.DATA, reconsume, pos);
                                        state = TokenizerState.s01_DATA;
                                        goto continueStateloop;
                                    case '\u0000':
                                        c = '\uFFFD';
                                        // fall thru
                                        goto default;
                                    default:
                                        /*
                                         * U+0041 LATIN CAPITAL LETTER A through to
                                         * U+005A LATIN CAPITAL LETTER Z Append the
                                         * lowercase TokenizerState.version of the input character (add
                                         * 0x0020 to the character's code point) to the
                                         * current DOCTYPE token's name.
                                         */
                                        if (c >= 'A' && c <= 'Z')
                                        {
                                            c += (char)0x0020;
                                        }
                                        /*
                                         * Anything else Append the current input
                                         * character to the current DOCTYPE token's
                                         * name.
                                         */
                                        AppendStrBuf(c);
                                        /*
                                         * Stay in the DOCTYPE name state.
                                         */
                                        continue;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakDoctypenameloop:
                            goto case TokenizerState.s55_AFTER_DOCTYPE_NAME;
                        }
                    // FALLTHRU DON'T REORDER
                    case TokenizerState.s55_AFTER_DOCTYPE_NAME:
                        /*afterdoctypenameloop:*/
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
                                         * in the after DOCTYPE name state.
                                         */
                                        continue;
                                    case '>':
                                        /*
                                         * U+003E GREATER-THAN SIGN (>) Emit the current
                                         * DOCTYPE token.
                                         */
                                        EmitDoctypeToken();
                                        /*
                                         * Switch to the data state.
                                         */
                                        //state = Transition(state, Tokenizer.DATA, reconsume, pos);
                                        state = TokenizerState.s01_DATA;
                                        goto continueStateloop;
                                    case 'p':
                                    case 'P':
                                        index = 0;
                                        //state = Transition(state, Tokenizer.DOCTYPE_UBLIC, reconsume, pos);
                                        state = TokenizerState.DOCTYPE_UBLIC;

                                        goto breakAfterdoctypenameloop;
                                    // goto continueStateloop;
                                    case 's':
                                    case 'S':
                                        index = 0;
                                        //state = Transition(state, Tokenizer.DOCTYPE_YSTEM, reconsume, pos);
                                        state = TokenizerState.DOCTYPE_YSTEM;
                                        goto continueStateloop;
                                    default:
                                        /*
                                         * Otherwise, this is the parse error.
                                         */
                                        BogusDoctype();

                                        /*
                                         * Set the DOCTYPE token's force-quirks flag to
                                         * on.
                                         */
                                        // done by bogusDoctype();
                                        /*
                                         * Switch to the bogus DOCTYPE state.
                                         */
                                        //state = Transition(state, Tokenizer.BOGUS_DOCTYPE, reconsume, pos);
                                        state = TokenizerState.s67_BOGUS_DOCTYPE;

                                        goto continueStateloop;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakAfterdoctypenameloop:
                            goto case TokenizerState.DOCTYPE_UBLIC;
                        }
                    // FALLTHRU DON'T REORDER
                    case TokenizerState.DOCTYPE_UBLIC:
                        /*doctypeublicloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                /*
                                 * If the six characters starting from the current input
                                 * character are an ASCII case-insensitive match for the
                                 * word "PUBLIC", then consume those characters and
                                 * switch to the before DOCTYPE public identifier state.
                                 */
                                if (index < 5)
                                { // UBLIC.Length
                                    char folded = c;
                                    if (c >= 'A' && c <= 'Z')
                                    {
                                        folded += (char)0x20;
                                    }
                                    if (folded != Tokenizer.UBLIC[index])
                                    {
                                        BogusDoctype();
                                        // forceQuirks = true;
                                        //state = Transition(state, Tokenizer.BOGUS_DOCTYPE, reconsume, pos);
                                        state = TokenizerState.s67_BOGUS_DOCTYPE;
                                        //reconsume = true;
                                        reader.StepBack();
                                        goto continueStateloop;
                                    }
                                    index++;
                                    continue;
                                }
                                else
                                {
                                    //state = Transition(state, Tokenizer.AFTER_DOCTYPE_PUBLIC_KEYWORD, reconsume, pos);
                                    state = TokenizerState.s56_AFTER_DOCTYPE_PUBLIC_KEYWORD;
                                    //reconsume = true;
                                    reader.StepBack();

                                    goto breakDoctypeublicloop;
                                    // goto continueStateloop;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakDoctypeublicloop:
                            goto case TokenizerState.s56_AFTER_DOCTYPE_PUBLIC_KEYWORD;
                        }
                    // FALLTHRU DON'T REORDER
                    case TokenizerState.s56_AFTER_DOCTYPE_PUBLIC_KEYWORD:
                        /*afterdoctypepublickeywordloop:*/
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
                                        //state = Transition(state, Tokenizer.BEFORE_DOCTYPE_PUBLIC_IDENTIFIER, reconsume, pos);
                                        state = TokenizerState.s57_BEFORE_DOCTYPE_PUBLIC_IDENTIFIER;
                                        goto breakStateloop;
                                    case '\n':
                                    case ' ':
                                    case '\t':
                                    case '\u000C':
                                        /*
                                         * U+0009 CHARACTER TABULATION U+000A LINE FEED
                                         * (LF) U+000C FORM FEED (FF) U+0020 SPACE
                                         * Switch to the before DOCTYPE public
                                         * identifier state.
                                         */
                                        //state = Transition(state, Tokenizer.BEFORE_DOCTYPE_PUBLIC_IDENTIFIER, reconsume, pos);
                                        state = TokenizerState.s57_BEFORE_DOCTYPE_PUBLIC_IDENTIFIER;
                                        goto breakAfterdoctypepublickeywordloop;
                                    // FALL THROUGH continue stateloop
                                    case '"':
                                        /*
                                         * U+0022 QUOTATION MARK (") Parse Error.
                                         */
                                        ErrNoSpaceBetweenDoctypePublicKeywordAndQuote();
                                        /*
                                         * Set the DOCTYPE token's public identifier to
                                         * the empty string (not missing),
                                         */
                                        ClearLongStrBuf();
                                        /*
                                         * then switch to the DOCTYPE public identifier
                                         * (double-quoted) state.
                                         */
                                        //state = Transition(state, Tokenizer.DOCTYPE_PUBLIC_IDENTIFIER_DOUBLE_QUOTED, reconsume, pos);
                                        state = TokenizerState.s58_DOCTYPE_PUBLIC_IDENTIFIER_DOUBLE_QUOTED;
                                        goto continueStateloop;
                                    case '\'':
                                        /*
                                         * U+0027 APOSTROPHE (') Parse Error.
                                         */
                                        ErrNoSpaceBetweenDoctypePublicKeywordAndQuote();
                                        /*
                                         * Set the DOCTYPE token's public identifier to
                                         * the empty string (not missing),
                                         */
                                        ClearLongStrBuf();
                                        /*
                                         * then switch to the DOCTYPE public identifier
                                         * (single-quoted) state.
                                         */
                                        //state = Transition(state, Tokenizer.DOCTYPE_PUBLIC_IDENTIFIER_SINGLE_QUOTED, reconsume, pos);
                                        state = TokenizerState.s59_DOCTYPE_PUBLIC_IDENTIFIER_SINGLE_QUOTED;
                                        goto continueStateloop;
                                    case '>':
                                        /* U+003E GREATER-THAN SIGN (>) Parse error. */
                                        ErrExpectedPublicId();
                                        /*
                                         * Set the DOCTYPE token's force-quirks flag to
                                         * on.
                                         */
                                        forceQuirks = true;
                                        /*
                                         * Emit that DOCTYPE token.
                                         */
                                        EmitDoctypeToken();
                                        /*
                                         * Switch to the data state.
                                         */
                                        //state = Transition(state, Tokenizer.DATA, reconsume, pos);
                                        state = TokenizerState.s01_DATA;
                                        goto continueStateloop;
                                    default:
                                        BogusDoctype();
                                        /*
                                         * Set the DOCTYPE token's force-quirks flag to
                                         * on.
                                         */
                                        // done by bogusDoctype();
                                        /*
                                         * Switch to the bogus DOCTYPE state.
                                         */
                                        //state = Transition(state, Tokenizer.BOGUS_DOCTYPE, reconsume, pos);
                                        state = TokenizerState.s67_BOGUS_DOCTYPE;
                                        goto continueStateloop;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakAfterdoctypepublickeywordloop:
                            goto case TokenizerState.s57_BEFORE_DOCTYPE_PUBLIC_IDENTIFIER;
                        }
                    // FALLTHRU DON'T REORDER
                    case TokenizerState.s57_BEFORE_DOCTYPE_PUBLIC_IDENTIFIER:
                        /*beforedoctypepublicidentifierloop:*/
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
                                         * in the before DOCTYPE public identifier
                                         * state.
                                         */
                                        continue;
                                    case '"':
                                        /*
                                         * U+0022 QUOTATION MARK (") Set the DOCTYPE
                                         * token's public identifier to the empty string
                                         * (not missing),
                                         */
                                        ClearLongStrBuf();
                                        /*
                                         * then switch to the DOCTYPE public identifier
                                         * (double-quoted) state.
                                         */
                                        //state = Transition(state, Tokenizer.DOCTYPE_PUBLIC_IDENTIFIER_DOUBLE_QUOTED, reconsume, pos);
                                        state = TokenizerState.s58_DOCTYPE_PUBLIC_IDENTIFIER_DOUBLE_QUOTED;
                                        goto breakBeforedoctypepublicidentifierloop;
                                    // goto continueStateloop;
                                    case '\'':
                                        /*
                                         * U+0027 APOSTROPHE (') Set the DOCTYPE token's
                                         * public identifier to the empty string (not
                                         * missing),
                                         */
                                        ClearLongStrBuf();
                                        /*
                                         * then switch to the DOCTYPE public identifier
                                         * (single-quoted) state.
                                         */
                                        //state = Transition(state, Tokenizer.DOCTYPE_PUBLIC_IDENTIFIER_SINGLE_QUOTED, reconsume, pos);
                                        state = TokenizerState.s59_DOCTYPE_PUBLIC_IDENTIFIER_SINGLE_QUOTED;
                                        goto continueStateloop;
                                    case '>':
                                        /* U+003E GREATER-THAN SIGN (>) Parse error. */
                                        ErrExpectedPublicId();
                                        /*
                                         * Set the DOCTYPE token's force-quirks flag to
                                         * on.
                                         */
                                        forceQuirks = true;
                                        /*
                                         * Emit that DOCTYPE token.
                                         */
                                        EmitDoctypeToken();
                                        /*
                                         * Switch to the data state.
                                         */
                                        //state = Transition(state, Tokenizer.DATA, reconsume, pos);
                                        state = TokenizerState.s01_DATA;
                                        goto continueStateloop;
                                    default:
                                        BogusDoctype();
                                        /*
                                         * Set the DOCTYPE token's force-quirks flag to
                                         * on.
                                         */
                                        // done by bogusDoctype();
                                        /*
                                         * Switch to the bogus DOCTYPE state.
                                         */
                                        //state = Transition(state, Tokenizer.BOGUS_DOCTYPE, reconsume, pos);
                                        state = TokenizerState.s67_BOGUS_DOCTYPE;
                                        goto continueStateloop;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakBeforedoctypepublicidentifierloop:
                            goto case TokenizerState.s58_DOCTYPE_PUBLIC_IDENTIFIER_DOUBLE_QUOTED;
                        }
                    // FALLTHRU DON'T REORDER
                    case TokenizerState.s58_DOCTYPE_PUBLIC_IDENTIFIER_DOUBLE_QUOTED:
                        /*doctypepublicidentifierdoublequotedloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {
                                switch (c)
                                {
                                    case '"':
                                        /*
                                         * U+0022 QUOTATION MARK (") Switch to the after
                                         * DOCTYPE public identifier state.
                                         */
                                        publicIdentifier = LongStrBufToString();
                                        //state = Transition(state, Tokenizer.AFTER_DOCTYPE_PUBLIC_IDENTIFIER, reconsume, pos);
                                        state = TokenizerState.s60_AFTER_DOCTYPE_PUBLIC_IDENTIFIER;
                                        goto breakDoctypepublicidentifierdoublequotedloop;
                                    // goto continueStateloop;
                                    case '>':
                                        /*
                                         * U+003E GREATER-THAN SIGN (>) Parse error.
                                         */
                                        ErrGtInPublicId();
                                        /*
                                         * Set the DOCTYPE token's force-quirks flag to
                                         * on.
                                         */
                                        forceQuirks = true;
                                        /*
                                         * Emit that DOCTYPE token.
                                         */
                                        publicIdentifier = LongStrBufToString();
                                        EmitDoctypeToken();
                                        /*
                                         * Switch to the data state.
                                         */
                                        //state = Transition(state, Tokenizer.DATA, reconsume, pos);
                                        state = TokenizerState.s01_DATA;
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
                                         * character to the current DOCTYPE token's
                                         * public identifier.
                                         */
                                        AppendLongStrBuf(c);
                                        /*
                                         * Stay in the DOCTYPE public identifier
                                         * (double-quoted) state.
                                         */
                                        continue;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakDoctypepublicidentifierdoublequotedloop:
                            goto case TokenizerState.s60_AFTER_DOCTYPE_PUBLIC_IDENTIFIER;
                        }
                    // FALLTHRU DON'T REORDER
                    case TokenizerState.s60_AFTER_DOCTYPE_PUBLIC_IDENTIFIER:
                        /*afterdoctypepublicidentifierloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                switch (c)
                                {
                                    case '\r':
                                        SilentCarriageReturn();
                                        //state = Transition(state, Tokenizer.BETWEEN_DOCTYPE_PUBLIC_AND_SYSTEM_IDENTIFIERS, reconsume, pos);
                                        state = TokenizerState.s61_BETWEEN_DOCTYPE_PUBLIC_AND_SYSTEM_IDENTIFIERS;
                                        goto breakStateloop;
                                    case '\n':
                                    case ' ':
                                    case '\t':
                                    case '\u000C':
                                        /*
                                         * U+0009 CHARACTER TABULATION U+000A LINE FEED
                                         * (LF) U+000C FORM FEED (FF) U+0020 SPACE
                                         * Switch to the between DOCTYPE public and
                                         * system identifiers state.
                                         */
                                        //state = Transition(state, Tokenizer.BETWEEN_DOCTYPE_PUBLIC_AND_SYSTEM_IDENTIFIERS, reconsume, pos);
                                        state = TokenizerState.s61_BETWEEN_DOCTYPE_PUBLIC_AND_SYSTEM_IDENTIFIERS;

                                        goto breakAfterdoctypepublicidentifierloop;
                                    // goto continueStateloop;
                                    case '>':
                                        /*
                                         * U+003E GREATER-THAN SIGN (>) Emit the current
                                         * DOCTYPE token.
                                         */
                                        EmitDoctypeToken();
                                        /*
                                         * Switch to the data state.
                                         */
                                        //state = Transition(state, Tokenizer.DATA, reconsume, pos);
                                        state = TokenizerState.s01_DATA;
                                        goto continueStateloop;
                                    case '"':
                                        /*
                                         * U+0022 QUOTATION MARK (") Parse error.
                                         */
                                        ErrNoSpaceBetweenPublicAndSystemIds();
                                        /*
                                         * Set the DOCTYPE token's system identifier to
                                         * the empty string (not missing),
                                         */
                                        ClearLongStrBuf();
                                        /*
                                         * then switch to the DOCTYPE system identifier
                                         * (double-quoted) state.
                                         */
                                        //state = Transition(state, Tokenizer.DOCTYPE_SYSTEM_IDENTIFIER_DOUBLE_QUOTED, reconsume, pos);
                                        state = TokenizerState.s64_DOCTYPE_SYSTEM_IDENTIFIER_DOUBLE_QUOTED;
                                        goto continueStateloop;
                                    case '\'':
                                        /*
                                         * U+0027 APOSTROPHE (') Parse error.
                                         */
                                        ErrNoSpaceBetweenPublicAndSystemIds();
                                        /*
                                         * Set the DOCTYPE token's system identifier to
                                         * the empty string (not missing),
                                         */
                                        ClearLongStrBuf();
                                        /*
                                         * then switch to the DOCTYPE system identifier
                                         * (single-quoted) state.
                                         */
                                        //state = Transition(state, Tokenizer.DOCTYPE_SYSTEM_IDENTIFIER_SINGLE_QUOTED, reconsume, pos);
                                        state = TokenizerState.s65_DOCTYPE_SYSTEM_IDENTIFIER_SINGLE_QUOTED;
                                        goto continueStateloop;
                                    default:
                                        BogusDoctype();
                                        /*
                                         * Set the DOCTYPE token's force-quirks flag to
                                         * on.
                                         */
                                        // done by bogusDoctype();
                                        /*
                                         * Switch to the bogus DOCTYPE state.
                                         */
                                        //state = Transition(state, Tokenizer.BOGUS_DOCTYPE, reconsume, pos);
                                        state = TokenizerState.s67_BOGUS_DOCTYPE;
                                        goto continueStateloop;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakAfterdoctypepublicidentifierloop:
                            goto case TokenizerState.s61_BETWEEN_DOCTYPE_PUBLIC_AND_SYSTEM_IDENTIFIERS;
                        }
                    // FALLTHRU DON'T REORDER
                    case TokenizerState.s61_BETWEEN_DOCTYPE_PUBLIC_AND_SYSTEM_IDENTIFIERS:
                        /*betweendoctypepublicandsystemidentifiersloop:*/
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
                                         * in the between DOCTYPE public and system
                                         * identifiers state.
                                         */
                                        continue;
                                    case '>':
                                        /*
                                         * U+003E GREATER-THAN SIGN (>) Emit the current
                                         * DOCTYPE token.
                                         */
                                        EmitDoctypeToken();
                                        /*
                                         * Switch to the data state.
                                         */
                                        //state = Transition(state, Tokenizer.DATA, reconsume, pos);
                                        state = TokenizerState.s01_DATA;
                                        goto continueStateloop;
                                    case '"':
                                        /*
                                         * U+0022 QUOTATION MARK (") Set the DOCTYPE
                                         * token's system identifier to the empty string
                                         * (not missing),
                                         */
                                        ClearLongStrBuf();
                                        /*
                                         * then switch to the DOCTYPE system identifier
                                         * (double-quoted) state.
                                         */
                                        //state = Transition(state, Tokenizer.DOCTYPE_SYSTEM_IDENTIFIER_DOUBLE_QUOTED, reconsume, pos);
                                        state = TokenizerState.s64_DOCTYPE_SYSTEM_IDENTIFIER_DOUBLE_QUOTED;
                                        goto breakBetweendoctypepublicandsystemidentifiersloop;
                                    // goto continueStateloop;
                                    case '\'':
                                        /*
                                         * U+0027 APOSTROPHE (') Set the DOCTYPE token's
                                         * system identifier to the empty string (not
                                         * missing),
                                         */
                                        ClearLongStrBuf();
                                        /*
                                         * then switch to the DOCTYPE system identifier
                                         * (single-quoted) state.
                                         */
                                        //state = Transition(state, Tokenizer.DOCTYPE_SYSTEM_IDENTIFIER_SINGLE_QUOTED, reconsume, pos);
                                        state = TokenizerState.s65_DOCTYPE_SYSTEM_IDENTIFIER_SINGLE_QUOTED;
                                        goto continueStateloop;
                                    default:
                                        BogusDoctype();
                                        /*
                                         * Set the DOCTYPE token's force-quirks flag to
                                         * on.
                                         */
                                        // done by bogusDoctype();
                                        /*
                                         * Switch to the bogus DOCTYPE state.
                                         */
                                        //state = Transition(state, Tokenizer.BOGUS_DOCTYPE, reconsume, pos);
                                        state = TokenizerState.s67_BOGUS_DOCTYPE;
                                        goto continueStateloop;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakBetweendoctypepublicandsystemidentifiersloop:
                            goto case TokenizerState.s64_DOCTYPE_SYSTEM_IDENTIFIER_DOUBLE_QUOTED;
                        }
                    // FALLTHRU DON'T REORDER
                    case TokenizerState.s64_DOCTYPE_SYSTEM_IDENTIFIER_DOUBLE_QUOTED:
                        /*doctypesystemidentifierdoublequotedloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {
                                switch (c)
                                {
                                    case '"':
                                        /*
                                         * U+0022 QUOTATION MARK (") Switch to the after
                                         * DOCTYPE system identifier state.
                                         */
                                        systemIdentifier = LongStrBufToString();
                                        //state = Transition(state, Tokenizer.AFTER_DOCTYPE_SYSTEM_IDENTIFIER, reconsume, pos);
                                        state = TokenizerState.s66_AFTER_DOCTYPE_SYSTEM_IDENTIFIER;
                                        goto continueStateloop;
                                    case '>':
                                        /*
                                         * U+003E GREATER-THAN SIGN (>) Parse error.
                                         */
                                        ErrGtInSystemId();
                                        /*
                                         * Set the DOCTYPE token's force-quirks flag to
                                         * on.
                                         */
                                        forceQuirks = true;
                                        /*
                                         * Emit that DOCTYPE token.
                                         */
                                        systemIdentifier = LongStrBufToString();
                                        EmitDoctypeToken();
                                        /*
                                         * Switch to the data state.
                                         */
                                        //state = Transition(state, Tokenizer.DATA, reconsume, pos);
                                        state = TokenizerState.s01_DATA;
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
                                         * character to the current DOCTYPE token's
                                         * system identifier.
                                         */
                                        AppendLongStrBuf(c);
                                        /*
                                         * Stay in the DOCTYPE system identifier
                                         * (double-quoted) state.
                                         */
                                        continue;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        }
                    // next 2 lines were unreachable; commented out
                    //breakDoctypesystemidentifierdoublequotedloop:
                    //	goto case TokenizerState.AFTER_DOCTYPE_SYSTEM_IDENTIFIER;
                    // FALLTHRU DON'T REORDER
                    case TokenizerState.s66_AFTER_DOCTYPE_SYSTEM_IDENTIFIER:
                        /*afterdoctypesystemidentifierloop:*/
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
                                         * in the after DOCTYPE system identifier state.
                                         */
                                        continue;
                                    case '>':
                                        /*
                                         * U+003E GREATER-THAN SIGN (>) Emit the current
                                         * DOCTYPE token.
                                         */
                                        EmitDoctypeToken();
                                        /*
                                         * Switch to the data state.
                                         */
                                        //state = Transition(state, Tokenizer.DATA, reconsume, pos);
                                        state = TokenizerState.s01_DATA;
                                        goto continueStateloop;
                                    default:
                                        /*
                                         * Switch to the bogus DOCTYPE state. (This does
                                         * not set the DOCTYPE token's force-quirks flag
                                         * to on.)
                                         */
                                        BogusDoctypeWithoutQuirks();
                                        //state = Transition(state, Tokenizer.BOGUS_DOCTYPE, reconsume, pos);
                                        state = TokenizerState.s67_BOGUS_DOCTYPE;
                                        goto breakAfterdoctypesystemidentifierloop;
                                    // goto continueStateloop;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakAfterdoctypesystemidentifierloop:
                            goto case TokenizerState.s67_BOGUS_DOCTYPE;
                        }
                    // FALLTHRU DON'T REORDER
                    case TokenizerState.s67_BOGUS_DOCTYPE:
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                switch (c)
                                {
                                    case '>':
                                        /*
                                         * U+003E GREATER-THAN SIGN (>) Emit that
                                         * DOCTYPE token.
                                         */
                                        EmitDoctypeToken();
                                        /*
                                         * Switch to the data state.
                                         */
                                        //state = Transition(state, Tokenizer.DATA, reconsume, pos);
                                        state = TokenizerState.s01_DATA;
                                        goto continueStateloop;
                                    case '\r':
                                        SilentCarriageReturn();
                                        goto breakStateloop;
                                    case '\n':
                                    default:
                                        /*
                                         * Anything else Stay in the bogus DOCTYPE
                                         * state.
                                         */
                                        continue;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        }
                    // XXX reorder point
                    case TokenizerState.DOCTYPE_YSTEM:
                        /*doctypeystemloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                /*
                                 * Otherwise, if the six characters starting from the
                                 * current input character are an ASCII case-insensitive
                                 * match for the word "SYSTEM", then consume those
                                 * characters and switch to the before DOCTYPE system
                                 * identifier state.
                                 */
                                if (index < 5)
                                { // YSTEM.Length
                                    char folded = c;
                                    if (c >= 'A' && c <= 'Z')
                                    {
                                        folded += (char)0x20;
                                    }
                                    if (folded != YSTEM[index])
                                    {
                                        BogusDoctype();
                                        //state = Transition(state, Tokenizer.BOGUS_DOCTYPE, reconsume, pos);
                                        state = TokenizerState.s67_BOGUS_DOCTYPE;
                                        reader.StepBack();
                                        //reconsume = true;
                                        goto continueStateloop;
                                    }
                                    index++;
                                    goto continueStateloop;
                                }
                                else
                                {
                                    //state = Transition(state, Tokenizer.AFTER_DOCTYPE_SYSTEM_KEYWORD, reconsume, pos);
                                    state = TokenizerState.s62_AFTER_DOCTYPE_SYSTEM_KEYWORD;
                                    //reconsume = true;
                                    reader.StepBack();
                                    goto breakDoctypeystemloop;
                                    // goto continueStateloop;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakDoctypeystemloop:
                            goto case TokenizerState.s62_AFTER_DOCTYPE_SYSTEM_KEYWORD;
                        }
                    // FALLTHRU DON'T REORDER
                    case TokenizerState.s62_AFTER_DOCTYPE_SYSTEM_KEYWORD:
                        /*afterdoctypesystemkeywordloop:*/
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                switch (c)
                                {
                                    case '\r':
                                        SilentCarriageReturn();
                                        //state = Transition(state, Tokenizer.BEFORE_DOCTYPE_SYSTEM_IDENTIFIER, reconsume, pos);
                                        state = TokenizerState.s63_BEFORE_DOCTYPE_SYSTEM_IDENTIFIER;

                                        goto breakStateloop;
                                    case '\n':
                                    case ' ':
                                    case '\t':
                                    case '\u000C':
                                        /*
                                         * U+0009 CHARACTER TABULATION U+000A LINE FEED
                                         * (LF) U+000C FORM FEED (FF) U+0020 SPACE
                                         * Switch to the before DOCTYPE public
                                         * identifier state.
                                         */
                                        //state = Transition(state, Tokenizer.BEFORE_DOCTYPE_SYSTEM_IDENTIFIER, reconsume, pos);
                                        state = TokenizerState.s63_BEFORE_DOCTYPE_SYSTEM_IDENTIFIER;
                                        goto breakAfterdoctypesystemkeywordloop;
                                    // FALL THROUGH continue stateloop
                                    case '"':
                                        /*
                                         * U+0022 QUOTATION MARK (") Parse Error.
                                         */
                                        ErrNoSpaceBetweenDoctypeSystemKeywordAndQuote();
                                        /*
                                         * Set the DOCTYPE token's system identifier to
                                         * the empty string (not missing),
                                         */
                                        ClearLongStrBuf();
                                        /*
                                         * then switch to the DOCTYPE public identifier
                                         * (double-quoted) state.
                                         */
                                        //state = Transition(state, Tokenizer.DOCTYPE_SYSTEM_IDENTIFIER_DOUBLE_QUOTED, reconsume, pos);
                                        state = TokenizerState.s64_DOCTYPE_SYSTEM_IDENTIFIER_DOUBLE_QUOTED;
                                        goto continueStateloop;
                                    case '\'':
                                        /*
                                         * U+0027 APOSTROPHE (') Parse Error.
                                         */
                                        ErrNoSpaceBetweenDoctypeSystemKeywordAndQuote();
                                        /*
                                         * Set the DOCTYPE token's public identifier to
                                         * the empty string (not missing),
                                         */
                                        ClearLongStrBuf();
                                        /*
                                         * then switch to the DOCTYPE public identifier
                                         * (single-quoted) state.
                                         */
                                        //state = Transition(state, Tokenizer.DOCTYPE_SYSTEM_IDENTIFIER_SINGLE_QUOTED, reconsume, pos);
                                        state = TokenizerState.s65_DOCTYPE_SYSTEM_IDENTIFIER_SINGLE_QUOTED;
                                        goto continueStateloop;
                                    case '>':
                                        /* U+003E GREATER-THAN SIGN (>) Parse error. */
                                        ErrExpectedPublicId();
                                        /*
                                         * Set the DOCTYPE token's force-quirks flag to
                                         * on.
                                         */
                                        forceQuirks = true;
                                        /*
                                         * Emit that DOCTYPE token.
                                         */
                                        EmitDoctypeToken();
                                        /*
                                         * Switch to the data state.
                                         */
                                        //state = Transition(state, Tokenizer.DATA, reconsume, pos);
                                        state = TokenizerState.s01_DATA;
                                        goto continueStateloop;
                                    default:
                                        BogusDoctype();
                                        /*
                                         * Set the DOCTYPE token's force-quirks flag to
                                         * on.
                                         */
                                        // done by bogusDoctype();
                                        /*
                                         * Switch to the bogus DOCTYPE state.
                                         */
                                        //state = Transition(state, Tokenizer.BOGUS_DOCTYPE, reconsume, pos);
                                        state = TokenizerState.s67_BOGUS_DOCTYPE;
                                        goto continueStateloop;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakAfterdoctypesystemkeywordloop:
                            goto case TokenizerState.s63_BEFORE_DOCTYPE_SYSTEM_IDENTIFIER;
                        }
                    // FALLTHRU DON'T REORDER
                    case TokenizerState.s63_BEFORE_DOCTYPE_SYSTEM_IDENTIFIER:
                        /*beforedoctypesystemidentifierloop:*/
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
                                         * in the before DOCTYPE system identifier
                                         * state.
                                         */
                                        continue;
                                    case '"':
                                        /*
                                         * U+0022 QUOTATION MARK (") Set the DOCTYPE
                                         * token's system identifier to the empty string
                                         * (not missing),
                                         */
                                        ClearLongStrBuf();
                                        /*
                                         * then switch to the DOCTYPE system identifier
                                         * (double-quoted) state.
                                         */
                                        //state = Transition(state, Tokenizer.DOCTYPE_SYSTEM_IDENTIFIER_DOUBLE_QUOTED, reconsume, pos);
                                        state = TokenizerState.s64_DOCTYPE_SYSTEM_IDENTIFIER_DOUBLE_QUOTED;
                                        goto continueStateloop;
                                    case '\'':
                                        /*
                                         * U+0027 APOSTROPHE (') Set the DOCTYPE token's
                                         * system identifier to the empty string (not
                                         * missing),
                                         */
                                        ClearLongStrBuf();
                                        /*
                                         * then switch to the DOCTYPE system identifier
                                         * (single-quoted) state.
                                         */
                                        //state = Transition(state, Tokenizer.DOCTYPE_SYSTEM_IDENTIFIER_SINGLE_QUOTED, reconsume, pos);
                                        state = TokenizerState.s65_DOCTYPE_SYSTEM_IDENTIFIER_SINGLE_QUOTED;
                                        goto breakBeforedoctypesystemidentifierloop;
                                    // goto continueStateloop;
                                    case '>':
                                        /* U+003E GREATER-THAN SIGN (>) Parse error. */
                                        ErrExpectedSystemId();
                                        /*
                                         * Set the DOCTYPE token's force-quirks flag to
                                         * on.
                                         */
                                        forceQuirks = true;
                                        /*
                                         * Emit that DOCTYPE token.
                                         */
                                        EmitDoctypeToken();
                                        /*
                                         * Switch to the data state.
                                         */
                                        //state = Transition(state, Tokenizer.DATA, reconsume, pos);
                                        state = TokenizerState.s01_DATA;
                                        goto continueStateloop;
                                    default:
                                        BogusDoctype();
                                        /*
                                         * Set the DOCTYPE token's force-quirks flag to
                                         * on.
                                         */
                                        // done by bogusDoctype();
                                        /*
                                         * Switch to the bogus DOCTYPE state.
                                         */
                                        //state = Transition(state, Tokenizer.BOGUS_DOCTYPE, reconsume, pos);
                                        state = TokenizerState.s67_BOGUS_DOCTYPE;
                                        goto continueStateloop;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        //------------------------------------
                        breakBeforedoctypesystemidentifierloop:
                            goto case TokenizerState.s65_DOCTYPE_SYSTEM_IDENTIFIER_SINGLE_QUOTED;
                        }
                    // FALLTHRU DON'T REORDER
                    case TokenizerState.s65_DOCTYPE_SYSTEM_IDENTIFIER_SINGLE_QUOTED:
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {
                                switch (c)
                                {
                                    case '\'':
                                        /*
                                         * U+0027 APOSTROPHE (') Switch to the after
                                         * DOCTYPE system identifier state.
                                         */
                                        systemIdentifier = LongStrBufToString();
                                        //state = Transition(state, Tokenizer.AFTER_DOCTYPE_SYSTEM_IDENTIFIER, reconsume, pos);
                                        state = TokenizerState.s66_AFTER_DOCTYPE_SYSTEM_IDENTIFIER;
                                        goto continueStateloop;
                                    case '>':
                                        ErrGtInSystemId();
                                        /*
                                         * Set the DOCTYPE token's force-quirks flag to
                                         * on.
                                         */
                                        forceQuirks = true;
                                        /*
                                         * Emit that DOCTYPE token.
                                         */
                                        systemIdentifier = LongStrBufToString();
                                        EmitDoctypeToken();
                                        /*
                                         * Switch to the data state.
                                         */
                                        //state = Transition(state, Tokenizer.DATA, reconsume, pos);
                                        state = TokenizerState.s01_DATA;
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
                                         * character to the current DOCTYPE token's
                                         * system identifier.
                                         */
                                        AppendLongStrBuf(c);
                                        /*
                                         * Stay in the DOCTYPE system identifier
                                         * (double-quoted) state.
                                         */
                                        continue;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                            // XXX reorder point
                        }
                    case TokenizerState.s59_DOCTYPE_PUBLIC_IDENTIFIER_SINGLE_QUOTED:
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {

                                switch (c)
                                {
                                    case '\'':
                                        /*
                                         * U+0027 APOSTROPHE (') Switch to the after
                                         * DOCTYPE public identifier state.
                                         */
                                        publicIdentifier = LongStrBufToString();
                                        //state = Transition(state, Tokenizer.AFTER_DOCTYPE_PUBLIC_IDENTIFIER, reconsume, pos);
                                        state = TokenizerState.s60_AFTER_DOCTYPE_PUBLIC_IDENTIFIER;
                                        goto continueStateloop;
                                    case '>':
                                        ErrGtInPublicId();
                                        /*
                                         * Set the DOCTYPE token's force-quirks flag to
                                         * on.
                                         */
                                        forceQuirks = true;
                                        /*
                                         * Emit that DOCTYPE token.
                                         */
                                        publicIdentifier = LongStrBufToString();
                                        EmitDoctypeToken();
                                        /*
                                         * Switch to the data state.
                                         */
                                        //state = Transition(state, Tokenizer.DATA, reconsume, pos);
                                        state = TokenizerState.s01_DATA;
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
                                         * character to the current DOCTYPE token's
                                         * public identifier.
                                         */
                                        AppendLongStrBuf(c);
                                        /*
                                         * Stay in the DOCTYPE public identifier
                                         * (single-quoted) state.
                                         */
                                        continue;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        }
                    // XXX reorder point
                    case TokenizerState.PROCESSING_INSTRUCTION:
                        //processinginstructionloop: 
                        {
                            char c;
                            while (reader.ReadNext(out c))
                            {
                                switch (c)
                                {
                                    case '?':
                                        //state = Transition(state,Tokenizer.PROCESSING_INSTRUCTION_QUESTION_MARK,reconsume, pos);
                                        state = TokenizerState.PROCESSING_INSTRUCTION_QUESTION_MARK;

                                        break;
                                    // continue stateloop;
                                    default:
                                        continue;
                                }
                            }
                            //------------------------------------
                            //eof
                            goto breakStateloop;
                        }
                    //breakProcessingInstructionLoop: 
                    case TokenizerState.PROCESSING_INSTRUCTION_QUESTION_MARK:
                        {
                            char c;
                            if (!reader.ReadNext(out c))
                            {
                                goto breakStateloop;

                            }

                            switch (c)
                            {
                                case '>':
                                    //state = Transition(state, Tokenizer.DATA,reconsume, pos);
                                    state = TokenizerState.s01_DATA;
                                    continue;
                                default:
                                    //state = Transition(state,Tokenizer.PROCESSING_INSTRUCTION,reconsume, pos);
                                    state = TokenizerState.PROCESSING_INSTRUCTION;
                                    continue;
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