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
        void StateLoop3_DocType(TokenizerState state, TokenizerState returnState)
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