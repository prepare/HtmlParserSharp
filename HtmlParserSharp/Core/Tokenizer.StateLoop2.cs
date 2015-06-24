///*
// * Copyright (c) 2005-2007 Henri Sivonen
// * Copyright (c) 2007-2010 Mozilla Foundation
// * Portions of comments Copyright 2004-2010 Apple Computer, Inc., Mozilla 
// * Foundation, and Opera Software ASA.
// * Copyright (c) 2012 Patrick Reisert
// * 
// * 2015, WinterDev
// *
// * Permission is hereby granted, free of charge, to any person obtaining a 
// * copy of this software and associated documentation files (the "Software"), 
// * to deal in the Software without restriction, including without limitation 
// * the rights to use, copy, modify, merge, publish, distribute, sublicense, 
// * and/or sell copies of the Software, and to permit persons to whom the 
// * Software is furnished to do so, subject to the following conditions:
// *
// * The above copyright notice and this permission notice shall be included in 
// * all copies or substantial portions of the Software.
// *
// * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// * THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// * DEALINGS IN THE SOFTWARE.
// */

//using System;
//using System.Text;
//using System.Collections.Generic;

//using System.Diagnostics;
//using HtmlParserSharp.Common;


//namespace HtmlParserSharp.Core
//{
//    partial class Tokenizer
//    {
//        int StateLoop2(TokenizerState state, char c,
//           int pos, char[] buf, bool reconsume, TokenizerState returnState,
//           int endPos)
//        {

//            TextSnapshotReader textSnapshot = new TextSnapshotReader(buf, pos, endPos - pos);
//            do
//            {
//                //switch by state 
//                switch (state)
//                {
//                    case TokenizerState.DATA:
//                        {
//                            //data state
//                            MyParseDataState(textSnapshot, ref state);
//                        } break;
//                    case TokenizerState.TAG_OPEN:
//                        {
//                            MyParseTagOpen(textSnapshot, ref state);
//                        } break;
//                    case TokenizerState.TAG_NAME:
//                        {
//                            MyParseTagName(textSnapshot, ref state);
//                        } break;
//                    case TokenizerState.BEFORE_ATTRIBUTE_NAME:
//                        {
//                            MyParseBeforeAttribute(textSnapshot, ref state);
//                        } break;
//                    case TokenizerState.ATTRIBUTE_NAME:
//                        {
//                            MyParseAttributeName(textSnapshot, ref state);
//                        } break;
//                    case TokenizerState.BEFORE_ATTRIBUTE_VALUE:
//                        {
//                            MyParseBeforeAttributeValue(textSnapshot, ref state);
//                        } break;
//                    case TokenizerState.ATTRIBUTE_VALUE_DOUBLE_QUOTED:
//                        {
//                            MyParseAttributeValueDoubleQuote(textSnapshot, ref state);
//                        } break;
//                    case TokenizerState.AFTER_ATTRIBUTE_VALUE_QUOTED:
//                        {
//                            MyParseAfterAttributeValueQuote(textSnapshot, ref state);
//                        } break;
//                    case TokenizerState.SELF_CLOSING_START_TAG:
//                        {
//                            MyParseSelfClosingTag(textSnapshot, ref state);
//                        } break;
//                    case TokenizerState.CLOSE_TAG_OPEN:
//                        {
//                            MyParseCloseTagOpen(textSnapshot, ref state);
//                        } break;
//                    case TokenizerState.BOGUS_COMMENT:
//                        {
//                            MyParseBogusComment(textSnapshot, ref state);
//                        } break;
//                    case TokenizerState.MARKUP_DECLARATION_OPEN:
//                        {
//                            MyParseMarkupDeclarationOpen(textSnapshot, ref state);

//                        } break;
//                    default:
//                        {
//                            throw new NotSupportedException();
//                        }
//                }

//            } while (textSnapshot.ReadNext(out c));
//            return 0;
//        }
//        void MyParseMarkupDeclarationOpen(TextSnapshotReader textSnapshot, ref TokenizerState state)
//        {

//            /*
//             * If the next two characters are both U+002D
//             * HYPHEN-MINUS characters (-), consume those two
//             * characters, create a comment token whose data is the
//             * empty string, and switch to the comment start state.
//             * 
//             * Otherwise, if the next seven characters are an ASCII
//             * case-insensitive match for the word "DOCTYPE", then
//             * consume those characters and switch to the DOCTYPE
//             * state.
//             * 
//             * Otherwise, if the insertion mode is
//             * "in foreign content" and the current node is not an
//             * element in the HTML namespace and the next seven
//             * characters are an case-sensitive match for the string
//             * "[CDATA[" (the five uppercase TokenizerState.letters "CDATA" with a
//             * U+005B LEFT SQUARE BRACKET character before and
//             * after), then consume those characters and switch to
//             * the CDATA section state.
//             * 
//             * Otherwise, is is a parse error. Switch to the bogus
//             * comment state. The next character that is consumed,
//             * if any, is the first character that will be in the
//             * comment.
//             */

//            char c = textSnapshot.CurrentChar;
//            switch (c)
//            {
//                case '-':
//                    ClearLongStrBufAndAppend(c);
//                    //state = Transition(state, Tokenizer.MARKUP_DECLARATION_HYPHEN, reconsume, pos);
//                    state = TokenizerState.MARKUP_DECLARATION_HYPHEN;
//                    MyParseDeclHyphen(textSnapshot, ref state);
//                    return;
//                case 'd':
//                case 'D':
//                    ClearLongStrBufAndAppend(c);
//                    index = 0;
//                    //state = Transition(state, Tokenizer.MARKUP_DECLARATION_OCTYPE, reconsume, pos);
//                    state = TokenizerState.MARKUP_DECLARATION_OCTYPE;

//                    MyParseDocType(textSnapshot, ref state);
//                    return;
//                case '[':
//                    if (TokenListener.IsCDataSectionAllowed)
//                    {
//                        ClearLongStrBufAndAppend(c);
//                        index = 0;
//                        //state = Transition(state, Tokenizer.CDATA_START, reconsume, pos);
//                        state = TokenizerState.CDATA_START;
//                        MyParseCData(textSnapshot, ref state);
//                        return;
//                    }
//                    else
//                    {
//                        // else fall through
//                        goto default;
//                    }
//                default:
//                    ErrBogusComment();
//                    ClearLongStrBuf();
//                    //state = Transition(state, Tokenizer.BOGUS_COMMENT, reconsume, pos);
//                    state = TokenizerState.BOGUS_COMMENT;
//                    textSnapshot.StepBack();
//                    //reconsume = true;
//                    return;
//            }
//        }
//        void MyParseDocType(TextSnapshotReader textSnapshot, ref TokenizerState state)
//        {
//            //check next 6 chars forward
//            char[] buffer = new char[6];
//            if (textSnapshot.Copy(buffer, textSnapshot.Position, 6))
//            {
//                bool matchAll = true;
//                for (int i = 0; i < 6; ++i)
//                {
//                    char folded = buffer[i];
//                    if (folded >= 'A' && folded <= 'Z')
//                    {
//                        //to upper
//                        folded += (char)0x20;
//                    }
//                    if (folded != Tokenizer.OCTYPE[i])
//                    {
//                        matchAll = false;
//                        break;
//                    }
//                }
//                if (matchAll)
//                {
//                    //accept
//                    char c = '\0';
//                    for (int i = 0; i < 6; ++i)
//                    {
//                        AppendLongStrBuf(buffer[i]);
//                        textSnapshot.ReadNext(out c);
//                    }
//                    state = TokenizerState.DOCTYPE;
//                    //-------
//                    InitDoctypeFields();
//                    //-----------------                    
//                    //parse next 
//                    MyParseBeforeDocTypeName(textSnapshot, ref state);

//                }
//                else
//                {
//                    ErrBogusComment();
//                }
//            }
//        }

//        void MyParseBeforeDocTypeName(TextSnapshotReader textSnapshot, ref TokenizerState state)
//        {
//            char c;
//            if (!textSnapshot.ReadNext(out c))
//            {
//                return;
//            }

//            switch (c)
//            {
//                case '\r':
//                    SilentCarriageReturn();
//                    //state = Transition(state, Tokenizer.BEFORE_DOCTYPE_NAME, reconsume, pos);
//                    state = TokenizerState.BEFORE_DOCTYPE_NAME;
//                    break;
//                case '\n':
//                    SilentLineFeed();
//                    goto case ' ';
//                // fall thru
//                case ' ':
//                case '\t':
//                case '\u000C':
//                    /*
//                     * U+0009 CHARACTER TABULATION U+000A LINE FEED
//                     * (LF) U+000C FORM FEED (FF) U+0020 SPACE
//                     * Switch to the before DOCTYPE name state.
//                     */
//                    //state = Transition(state, Tokenizer.BEFORE_DOCTYPE_NAME, reconsume, pos);
//                    state = TokenizerState.BEFORE_DOCTYPE_NAME;
//                    break;
//                default:
//                    /*
//                     * Anything else Parse error.
//                     */
//                    ErrMissingSpaceBeforeDoctypeName();
//                    /*
//                     * Reconsume the current character in the before
//                     * DOCTYPE name state.
//                     */
//                    //state = Transition(state, Tokenizer.BEFORE_DOCTYPE_NAME, reconsume, pos);
//                    state = TokenizerState.BEFORE_DOCTYPE_NAME;
//                    break;
//                // goto continueStateloop;
//            }
//            //----------------------------------------------------------------------



//        }
//        void MyParseCData(TextSnapshotReader textSnapshot, ref TokenizerState state)
//        {

//        }

//        void MyParseDeclHyphen(TextSnapshotReader textSnapshot, ref TokenizerState state)
//        {

//        }
//        void MyParseBogusComment(TextSnapshotReader textSnapshot, ref TokenizerState state)
//        {

//        }

//        void MyParseCloseTagOpen(TextSnapshotReader textSnapshot, ref TokenizerState state)
//        {

//            char c = textSnapshot.CurrentChar;

//            switch (c)
//            {
//                case '>':
//                    /* U+003E GREATER-THAN SIGN (>) Parse error. */
//                    ErrLtSlashGt();
//                    /*
//                     * Switch to the data state.
//                     */
//                    cstart = textSnapshot.Position + 1;
//                    //state = Transition(state, Tokenizer.DATA, reconsume, pos);
//                    state = TokenizerState.DATA;
//                    return;
//                case '\r':
//                    SilentCarriageReturn();
//                    /* Anything else Parse error. */
//                    ErrGarbageAfterLtSlash();
//                    /*
//                     * Switch to the bogus comment state.
//                     */
//                    ClearLongStrBufAndAppend('\n');
//                    //state = Transition(state, Tokenizer.BOGUS_COMMENT, reconsume, pos);
//                    state = TokenizerState.BOGUS_COMMENT;
//                    return;
//                case '\n':
//                    SilentLineFeed();
//                    /* Anything else Parse error. */
//                    ErrGarbageAfterLtSlash();
//                    /*
//                     * Switch to the bogus comment state.
//                     */
//                    ClearLongStrBufAndAppend('\n');
//                    //state = Transition(state, Tokenizer.BOGUS_COMMENT, reconsume, pos);
//                    state = TokenizerState.BOGUS_COMMENT;
//                    return;
//                case '\u0000':
//                    c = '\uFFFD';
//                    // fall thru
//                    goto default;
//                default:
//                    if (c >= 'A' && c <= 'Z')
//                    {
//                        c += (char)0x20;
//                    }
//                    if (c >= 'a' && c <= 'z')
//                    {
//                        /*
//                         * U+0061 LATIN SMALL LETTER A through to U+007A
//                         * LATIN SMALL LETTER Z Create a new end tag
//                         * token,
//                         */
//                        endTag = true;
//                        /*
//                         * set its tag name to the input character,
//                         */
//                        ClearStrBufAndAppend(c);
//                        /*
//                         * then switch to the tag name state. (Don't
//                         * emit the token yet; further details will be
//                         * filled in before it is emitted.)
//                         */
//                        //state = Transition(state, Tokenizer.TAG_NAME, reconsume, pos);
//                        state = TokenizerState.TAG_NAME;
//                        return;
//                    }
//                    else
//                    {
//                        /* Anything else Parse error. */
//                        ErrGarbageAfterLtSlash();
//                        /*
//                         * Switch to the bogus comment state.
//                         */
//                        ClearLongStrBufAndAppend(c);
//                        //state = Transition(state, Tokenizer.BOGUS_COMMENT, reconsume, pos);
//                        state = TokenizerState.BOGUS_COMMENT;
//                        return;
//                    }
//            }


//        }
//        void MyParseAfterAttributeValueQuote(TextSnapshotReader textSnapshot, ref TokenizerState state)
//        {
//            char c = textSnapshot.CurrentChar;

//            switch (c)
//            {
//                case '\r':
//                    SilentCarriageReturn();
//                    //state = Transition(state, Tokenizer.BEFORE_ATTRIBUTE_NAME, reconsume, pos);
//                    state = TokenizerState.BEFORE_ATTRIBUTE_NAME;
//                    return;
//                case '\n':
//                    SilentLineFeed();
//                    // fall thru
//                    goto case ' ';
//                case ' ':
//                case '\t':
//                case '\u000C':
//                    /*
//                     * U+0009 CHARACTER TABULATION U+000A LINE FEED
//                     * (LF) U+000C FORM FEED (FF) U+0020 SPACE
//                     * Switch to the before attribute name state.
//                     */
//                    //state = Transition(state, Tokenizer.BEFORE_ATTRIBUTE_NAME, reconsume, pos);
//                    state = TokenizerState.BEFORE_ATTRIBUTE_NAME;
//                    return;
//                case '/':
//                    /*
//                     * U+002F SOLIDUS (/) Switch to the self-closing
//                     * start tag state.
//                     */
//                    //state = Transition(state, Tokenizer.SELF_CLOSING_START_TAG, reconsume, pos);
//                    state = TokenizerState.SELF_CLOSING_START_TAG;
//                    return;
//                // goto continueStateloop;
//                case '>':
//                    /*
//                     * U+003E GREATER-THAN SIGN (>) Emit the current
//                     * tag token.
//                     */
//                    //state = Transition(state, EmitCurrentTagToken(false, pos), reconsume, pos);
//                    state = EmitCurrentTagToken(false, textSnapshot.Position);
//                    if (shouldSuspend)
//                    {
//                        return;
//                    }
//                    /*
//                     * Switch to the data state.
//                     */
//                    return;
//                default:
//                    /*
//                     * Anything else Parse error.
//                     */
//                    ErrNoSpaceBetweenAttributes();
//                    /*
//                     * Reconsume the character in the before
//                     * attribute name state.
//                     */
//                    //state = Transition(state, Tokenizer.BEFORE_ATTRIBUTE_NAME, reconsume, pos);
//                    state = TokenizerState.BEFORE_ATTRIBUTE_NAME;
//                    //reconsume = true;
//                    //goto continueStateloop;
//                    textSnapshot.StepBack();
//                    return;
//            }
//        }
//        void MyParseAttributeValueDoubleQuote(TextSnapshotReader textSnapshot, ref TokenizerState state)
//        {
//            char c = textSnapshot.CurrentChar;
//            do
//            {
//                /*
//                 * Consume the next input character:
//                 */
//                switch (c)
//                {
//                    case '"':
//                        /*
//                         * U+0022 QUOTATION MARK (") Switch to the after
//                         * attribute value (quoted) state.
//                         */
//                        AddAttributeWithValue();

//                        //state = Transition(state, Tokenizer.AFTER_ATTRIBUTE_VALUE_QUOTED, reconsume, pos);
//                        state = TokenizerState.AFTER_ATTRIBUTE_VALUE_QUOTED;
//                        return;
//                    // goto continueStateloop;
//                    case '&':
//                        /*
//                         * U+0026 AMPERSAND (&) Switch to the character
//                         * reference in attribute value state, with the
//                         * additional allowed character being U+0022
//                         * QUOTATION MARK (").
//                         */
//                        ClearStrBufAndAppend(c);
//                        SetAdditionalAndRememberAmpersandLocation('\"');
//                        //returnState = state;
//                        //state = Transition(state, Tokenizer.CONSUME_CHARACTER_REFERENCE, reconsume, pos);
//                        state = TokenizerState.CONSUME_CHARACTER_REFERENCE;

//                        //TODO: review here again!
//                        throw new NotSupportedException();
//                        return;
//                    case '\r':
//                        AppendLongStrBufCarriageReturn();
//                        return;
//                    case '\n':
//                        AppendLongStrBufLineFeed();
//                        return;
//                    case '\u0000':
//                        c = '\uFFFD';
//                        // fall thru
//                        goto default;
//                    default:
//                        /*
//                         * Anything else Append the current input
//                         * character to the current attribute's value.
//                         */
//                        AppendLongStrBuf(c);
//                        /*
//                         * Stay in the attribute value (double-quoted)
//                         * state.
//                         */
//                        continue;
//                }
//            } while (textSnapshot.ReadNext(out c));

//        }
//        void MyParseBeforeAttributeValue(TextSnapshotReader textSnapshot, ref TokenizerState state)
//        {
//            char c = textSnapshot.CurrentChar;
//            do
//            {
//                /*
//                * Consume the next input character:
//                    */
//                switch (c)
//                {
//                    case '\r':
//                        SilentCarriageReturn();
//                        break;
//                    case '\n':
//                        SilentLineFeed();
//                        goto case ' ';
//                    // fall thru
//                    case ' ':
//                    case '\t':
//                    case '\u000C':
//                        /*
//                         * U+0009 CHARACTER TABULATION U+000A LINE FEED
//                         * (LF) U+000C FORM FEED (FF) U+0020 SPACE Stay
//                         * in the before attribute value state.
//                         */
//                        break;
//                    case '"':
//                        /*
//                         * U+0022 QUOTATION MARK (") Switch to the
//                         * attribute value (double-quoted) state.
//                         */
//                        ClearLongStrBuf();
//                        //state = Transition(state, Tokenizer.ATTRIBUTE_VALUE_DOUBLE_QUOTED, reconsume, pos);
//                        state = TokenizerState.ATTRIBUTE_VALUE_DOUBLE_QUOTED;
//                        return;
//                    // goto continueStateloop;
//                    case '&':
//                        /*
//                         * U+0026 AMPERSAND (&) Switch to the attribute
//                         * value (unquoted) state and reconsume this
//                         * input character.
//                         */
//                        ClearLongStrBuf();
//                        //state = Transition(state, Tokenizer.ATTRIBUTE_VALUE_UNQUOTED, reconsume, pos);
//                        state = TokenizerState.ATTRIBUTE_VALUE_UNQUOTED;
//                        NoteUnquotedAttributeValue();

//                        //reconsume = true;
//                        textSnapshot.StepBack();
//                        //goto continueStateloop;
//                        return;
//                    case '\'':
//                        /*
//                         * U+0027 APOSTROPHE (') Switch to the attribute
//                         * value (single-quoted) state.
//                         */
//                        ClearLongStrBuf();
//                        //state = Transition(state, Tokenizer.ATTRIBUTE_VALUE_SINGLE_QUOTED, reconsume, pos);
//                        state = TokenizerState.ATTRIBUTE_VALUE_SINGLE_QUOTED;
//                        return;
//                    case '>':
//                        /*
//                         * U+003E GREATER-THAN SIGN (>) Parse error.
//                         */
//                        ErrAttributeValueMissing();
//                        /*
//                         * Emit the current tag token.
//                         */
//                        AddAttributeWithoutValue();
//                        //state = Transition(state, EmitCurrentTagToken(false, pos), reconsume, pos);
//                        state = EmitCurrentTagToken(false, textSnapshot.Position);
//                        if (shouldSuspend)
//                        {
//                            return;
//                            //goto breakStateloop;
//                        }
//                        /*
//                         * Switch to the data state.
//                         */
//                        //goto continueStateloop;
//                        return;
//                    case '\u0000':
//                        c = '\uFFFD';
//                        // fall thru
//                        goto case '<';
//                    case '<':
//                    case '=':
//                    case '`':
//                        /*
//                         * U+003C LESS-THAN SIGN (<) U+003D EQUALS SIGN
//                         * (=) U+0060 GRAVE ACCENT (`)
//                         */
//                        ErrLtOrEqualsOrGraveInUnquotedAttributeOrNull(c);
//                        /*
//                         * Treat it as per the "anything else" entry
//                         * below.
//                         */
//                        goto default;
//                    default:
//                        // [NOCPP[
//                        ErrHtml4NonNameInUnquotedAttribute(c);
//                        // ]NOCPP]
//                        /*
//                         * Anything else Append the current input
//                         * character to the current attribute's value.
//                         */
//                        ClearLongStrBufAndAppend(c);
//                        /*
//                         * Switch to the attribute value (unquoted)
//                         * state.
//                         */
//                        //state = Transition(state, Tokenizer.ATTRIBUTE_VALUE_UNQUOTED, reconsume, pos);
//                        state = TokenizerState.ATTRIBUTE_VALUE_UNQUOTED;

//                        NoteUnquotedAttributeValue();
//                        return;
//                }
//            } while (textSnapshot.ReadNext(out c));

//        }
//        void MyParseAttributeName(TextSnapshotReader textSnapshot, ref TokenizerState state)
//        {
//            char c = textSnapshot.CurrentChar;
//            do
//            {
//                switch (c)
//                {
//                    case '\r':
//                        SilentCarriageReturn();
//                        AttributeNameComplete();
//                        //state = Transition(state, Tokenizer.AFTER_ATTRIBUTE_NAME, reconsume, pos);
//                        state = TokenizerState.AFTER_ATTRIBUTE_NAME;
//                        return;
//                    case '\n':
//                        SilentLineFeed();
//                        goto case ' ';
//                    // fall thru
//                    case ' ':
//                    case '\t':
//                    case '\u000C':
//                        /*
//                         * U+0009 CHARACTER TABULATION U+000A LINE FEED
//                         * (LF) U+000C FORM FEED (FF) U+0020 SPACE
//                         * Switch to the after attribute name state.
//                         */
//                        AttributeNameComplete();
//                        //state = Transition(state, Tokenizer.AFTER_ATTRIBUTE_NAME, reconsume, pos);
//                        state = TokenizerState.AFTER_ATTRIBUTE_NAME;
//                        return;
//                    case '/':
//                        /*
//                         * U+002F SOLIDUS (/) Switch to the self-closing
//                         * start tag state.
//                         */
//                        AttributeNameComplete();
//                        AddAttributeWithoutValue();
//                        //state = Transition(state, Tokenizer.SELF_CLOSING_START_TAG, reconsume, pos);
//                        state = TokenizerState.SELF_CLOSING_START_TAG;
//                        return;
//                    case '=':
//                        /*
//                         * U+003D EQUALS SIGN (=) Switch to the before
//                         * attribute value state.
//                         */
//                        AttributeNameComplete();
//                        //state = Transition(state, Tokenizer.BEFORE_ATTRIBUTE_VALUE, reconsume, pos);
//                        state = TokenizerState.BEFORE_ATTRIBUTE_VALUE;
//                        return;
//                    // goto continueStateloop;
//                    case '>':
//                        /*
//                         * U+003E GREATER-THAN SIGN (>) Emit the current
//                         * tag token.
//                         */
//                        AttributeNameComplete();
//                        AddAttributeWithoutValue();
//                        //state = Transition(state, EmitCurrentTagToken(false, pos), reconsume, pos);
//                        state = EmitCurrentTagToken(false, textSnapshot.Position);
//                        if (shouldSuspend)
//                        {
//                            return;
//                        }
//                        /*
//                         * Switch to the data state.
//                         */
//                        return;
//                    case '\u0000':
//                        c = '\uFFFD';
//                        // fall thru
//                        goto case '\"';
//                    case '\"':
//                    case '\'':
//                    case '<':
//                        /*
//                         * U+0022 QUOTATION MARK (") U+0027 APOSTROPHE
//                         * (') U+003C LESS-THAN SIGN (<) Parse error.
//                         */
//                        ErrQuoteOrLtInAttributeNameOrNull(c);
//                        /*
//                         * Treat it as per the "anything else" entry
//                         * below.
//                         */
//                        goto default;
//                    default:
//                        {
//                            if (c >= 'A' && c <= 'Z')
//                            {
//                                /*
//                                 * U+0041 LATIN CAPITAL LETTER A through to
//                                 * U+005A LATIN CAPITAL LETTER Z Append the
//                                 * lowercase TokenizerState.version of the current input
//                                 * character (add 0x0020 to the character's
//                                 * code point) to the current attribute's
//                                 * name.
//                                 */
//                                c += (char)0x20;
//                            }
//                            /*
//                             * Anything else Append the current input
//                             * character to the current attribute's name.
//                             */
//                            AppendStrBuf(c);
//                            /*
//                             * Stay in the attribute name state.
//                             */
//                        } break;
//                }

//            } while (textSnapshot.ReadNext(out c));

//        }
//        void MyParseSelfClosingTag(TextSnapshotReader textSnapshot, ref TokenizerState state)
//        {
//            char c = textSnapshot.CurrentChar;
//            switch (c)
//            {
//                case '>':
//                    /*
//                     * U+003E GREATER-THAN SIGN (>) Set the self-closing
//                     * flag of the current tag token. Emit the current
//                     * tag token.
//                     */
//                    // [NOCPP[
//                    ErrHtml4XmlVoidSyntax();
//                    // ]NOCPP]
//                    //state = Transition(state, EmitCurrentTagToken(true, pos), reconsume, pos);
//                    state = EmitCurrentTagToken(true, textSnapshot.Position);
//                    if (shouldSuspend)
//                    {
//                        return;
//                    }
//                    /*
//                     * Switch to the data state.
//                     */
//                    return;
//                default:
//                    /* Anything else Parse error. */
//                    ErrSlashNotFollowedByGt();
//                    /*
//                     * Reconsume the character in the before attribute
//                     * name state.
//                     */
//                    //state = Transition(state, Tokenizer.BEFORE_ATTRIBUTE_NAME, reconsume, pos);
//                    state = TokenizerState.BEFORE_ATTRIBUTE_NAME;
//                    //reconsme

//                    //reconsume = true;
//                    textSnapshot.StepBack();
//                    return;
//            }
//        }
//        void MyParseTagOpen(TextSnapshotReader textSnapshot, ref TokenizerState state)
//        {
//            char c = textSnapshot.CurrentChar;
//            //just after open tag <
//            if (c >= 'A' && c <= 'Z')
//            {
//                /*
//                 * U+0041 LATIN CAPITAL LETTER A through to U+005A
//                 * LATIN CAPITAL LETTER Z Create a new start tag
//                 * token,
//                 */
//                endTag = false;
//                /*
//                 * set its tag name to the lowercase TokenizerState.version of the
//                 * input character (add 0x0020 to the character's
//                 * code point),
//                 */
//                ClearStrBufAndAppend((char)(c + 0x20));
//                /* then switch to the tag name state. */
//                //state = Transition(state, Tokenizer.TAG_NAME, reconsume, pos);
//                state = TokenizerState.TAG_NAME;
//                /*
//                 * (Don't emit the token yet; further details will
//                 * be filled in before it is emitted.)
//                 */
//                return;
//            }
//            else if (c >= 'a' && c <= 'z')
//            {
//                /*
//                 * U+0061 LATIN SMALL LETTER A through to U+007A
//                 * LATIN SMALL LETTER Z Create a new start tag
//                 * token,
//                 */
//                endTag = false;
//                /*
//                 * set its tag name to the input character,
//                 */
//                ClearStrBufAndAppend(c);
//                /* then switch to the tag name state. */
//                //state = Transition(state, Tokenizer.TAG_NAME, reconsume, pos);
//                state = TokenizerState.TAG_NAME;
//                /*
//                 * (Don't emit the token yet; further details will
//                 * be filled in before it is emitted.)
//                 */
//                return;
//            }
//            switch (c)
//            {
//                case '!':
//                    /*
//                     * U+0021 EXCLAMATION MARK (!) Switch to the
//                     * markup declaration open state.
//                     */
//                    //state = Transition(state, Tokenizer.MARKUP_DECLARATION_OPEN, reconsume, pos);
//                    state = TokenizerState.MARKUP_DECLARATION_OPEN;
//                    return;
//                case '/':
//                    /*
//                     * U+002F SOLIDUS (/) Switch to the close tag
//                     * open state.
//                     */
//                    //state = Transition(state, Tokenizer.CLOSE_TAG_OPEN, reconsume, pos);
//                    state = TokenizerState.CLOSE_TAG_OPEN;
//                    return;
//                case '?':
//                    /*
//                     * U+003F QUESTION MARK (?) Parse error.
//                     */
//                    ErrProcessingInstruction();
//                    /*
//                     * Switch to the bogus comment state.
//                     */
//                    ClearLongStrBufAndAppend(c);
//                    //state = Transition(state, Tokenizer.BOGUS_COMMENT, reconsume, pos);
//                    state = TokenizerState.BOGUS_COMMENT;
//                    return;
//                case '>':
//                    /*
//                     * U+003E GREATER-THAN SIGN (>) Parse error.
//                     */
//                    ErrLtGt();
//                    /*
//                     * Emit a U+003C LESS-THAN SIGN character token
//                     * and a U+003E GREATER-THAN SIGN character
//                     * token.
//                     */
//                    TokenListener.Characters(LT_GT, 0, 2);
//                    /* Switch to the data state. */
//                    cstart = textSnapshot.Position + 1;
//                    //state = Transition(state, Tokenizer.DATA, reconsume, pos);
//                    state = TokenizerState.DATA;
//                    return;
//                default:
//                    /*
//                     * Anything else Parse error.
//                     */
//                    ErrBadCharAfterLt(c);
//                    /*
//                     * Emit a U+003C LESS-THAN SIGN character token
//                     */
//                    TokenListener.Characters(LT_GT, 0, 1);
//                    /*
//                     * and reconsume the current input character in
//                     * the data state.
//                     */
//                    cstart = textSnapshot.Position;
//                    //state = Transition(state, Tokenizer.DATA, reconsume, pos);
//                    state = TokenizerState.DATA;
//                    textSnapshot.StepBack();//reconsume
//                    return;
//            }


//        }
//        void MyParseDataState(TextSnapshotReader textSnapshot, ref TokenizerState state)
//        {
//            char c = textSnapshot.CurrentChar;
//            do
//            {
//                switch (c)
//                {
//                    case '&':
//                        /*
//                         * U+0026 AMPERSAND (&) Switch to the character
//                         * reference in data state.
//                         */
//                        FlushChars(textSnapshot);
//                        ClearStrBufAndAppend(c);

//                        SetAdditionalAndRememberAmpersandLocation('\u0000');
//                        //returnState = state;
//                        //state = Transition(state, Tokenizer.CONSUME_CHARACTER_REFERENCE, reconsume, pos);
//                        state = TokenizerState.CONSUME_CHARACTER_REFERENCE;

//                        //return;
//                        //parse character reference ***
//                        //TODO:
//                        MyParseCharacterReference(textSnapshot, ref state);
//                        return;
//                    case '<':
//                        /*
//                         * U+003C LESS-THAN SIGN (<) Switch to the tag
//                         * open state.
//                         */
//                        FlushChars(textSnapshot);
//                        state = TokenizerState.TAG_OPEN;
//                        //goto breakDataloop; // FALL THROUGH continue
//                        return;
//                    // stateloop;
//                    case '\u0000':
//                        EmitReplacementCharacter(textSnapshot.InteralBuff, textSnapshot.Position);
//                        continue;
//                    case '\r':
//                        EmitCarriageReturn(textSnapshot.InteralBuff, textSnapshot.Position);
//                        return;
//                    case '\n':
//                        SilentLineFeed();
//                        goto default;
//                    default:
//                        /*
//                         * Anything else Emit the input character as a
//                         * character token.
//                         * 
//                         * Stay in the data state.
//                         */
//                        continue;
//                }
//            } while (textSnapshot.ReadNext(out c));
//        }


//        void MyParseCharacterReference(TextSnapshotReader textSnapshot, ref TokenizerState state)
//        {
//            throw new MyNotImplementException();
//            char c = textSnapshot.CurrentChar;
//            do
//            {


//            } while (textSnapshot.ReadNext(out c));
//        }

//        void MyParseTagName(TextSnapshotReader textSnapshot, ref TokenizerState state)
//        {
//            char c = textSnapshot.CurrentChar;
//            do
//            {
//                switch (c)
//                {
//                    case '\r':
//                        SilentCarriageReturn();
//                        StrBufToElementNameString();
//                        //state = Transition(state, Tokenizer.BEFORE_ATTRIBUTE_NAME, reconsume, pos);
//                        state = TokenizerState.BEFORE_ATTRIBUTE_NAME;
//                        return;
//                    case '\n':
//                        SilentLineFeed();
//                        goto case ' ';
//                    case ' ':
//                    case '\t':
//                    case '\u000C':
//                        /*
//                         * U+0009 CHARACTER TABULATION U+000A LINE FEED
//                         * (LF) U+000C FORM FEED (FF) U+0020 SPACE
//                         * Switch to the before attribute name state.
//                         */
//                        StrBufToElementNameString();
//                        //state = Transition(state, Tokenizer.BEFORE_ATTRIBUTE_NAME, reconsume, pos);
//                        state = TokenizerState.BEFORE_ATTRIBUTE_NAME;
//                        return;
//                    // goto continueStateloop;
//                    case '/':
//                        /*
//                         * U+002F SOLIDUS (/) Switch to the self-closing
//                         * start tag state.
//                         */
//                        StrBufToElementNameString();
//                        //state = Transition(state, Tokenizer.SELF_CLOSING_START_TAG, reconsume, pos);
//                        state = TokenizerState.SELF_CLOSING_START_TAG;
//                        return;
//                    case '>':
//                        /*
//                         * U+003E GREATER-THAN SIGN (>) Emit the current
//                         * tag token.
//                         */
//                        StrBufToElementNameString();
//                        //state = Transition(state, EmitCurrentTagToken(false, pos), reconsume, pos);
//                        state = EmitCurrentTagToken(false, textSnapshot.Position);
//                        if (shouldSuspend)
//                        {
//                            return;
//                        }
//                        /*
//                         * Switch to the data state.
//                         */
//                        return;
//                    case '\u0000':
//                        c = '\uFFFD';
//                        goto default;
//                    // fall thru
//                    default:
//                        {
//                            if (c >= 'A' && c <= 'Z')
//                            {
//                                /*
//                                 * U+0041 LATIN CAPITAL LETTER A through to
//                                 * U+005A LATIN CAPITAL LETTER Z Append the
//                                 * lowercase TokenizerState.version of the current input
//                                 * character (add 0x0020 to the character's
//                                 * code point) to the current tag token's
//                                 * tag name.
//                                 */
//                                c += (char)0x20;
//                            }
//                            /*
//                             * Anything else Append the current input
//                             * character to the current tag token's tag
//                             * name.
//                             */
//                            AppendStrBuf(c);
//                            /*
//                             * Stay in the tag name state.
//                             */
//                        } break;
//                }

//            } while (textSnapshot.ReadNext(out c));
//        }

//        void MyParseBeforeAttribute(TextSnapshotReader textSnapshot, ref TokenizerState state)
//        {
//            char c = textSnapshot.CurrentChar;
//            do
//            {
//                switch (c)
//                {

//                    case '\r':
//                        SilentCarriageReturn();
//                        break;
//                    case '\n':
//                        SilentLineFeed();
//                        // fall thru
//                        goto case ' ';
//                    case ' ':
//                    case '\t':
//                    case '\u000C':
//                        /*
//                         * U+0009 CHARACTER TABULATION U+000A LINE FEED
//                         * (LF) U+000C FORM FEED (FF) U+0020 SPACE Stay
//                         * in the before attribute name state.
//                         */
//                        break;
//                    case '/':
//                        /*
//                         * U+002F SOLIDUS (/) Switch to the self-closing
//                         * start tag state.
//                         */
//                        //state = Transition(state, Tokenizer.SELF_CLOSING_START_TAG, reconsume, pos);
//                        state = TokenizerState.SELF_CLOSING_START_TAG;
//                        return;
//                    case '>':
//                        /*
//                         * U+003E GREATER-THAN SIGN (>) Emit the current
//                         * tag token.
//                         */
//                        //state = Transition(state, EmitCurrentTagToken(false, pos), reconsume, pos);
//                        state = EmitCurrentTagToken(false, textSnapshot.Position);
//                        if (shouldSuspend)
//                        {
//                            return;
//                        }
//                        /*
//                         * Switch to the data state.
//                         */
//                        return;
//                    case '\u0000':
//                        c = '\uFFFD';
//                        // fall thru
//                        goto case '\"';
//                    case '\"':
//                    case '\'':
//                    case '<':
//                    case '=':
//                        /*
//                         * U+0022 QUOTATION MARK (") U+0027 APOSTROPHE
//                         * (') U+003C LESS-THAN SIGN (<) U+003D EQUALS
//                         * SIGN (=) Parse error.
//                         */
//                        ErrBadCharBeforeAttributeNameOrNull(c);
//                        /*
//                         * Treat it as per the "anything else" entry
//                         * below.
//                         */
//                        goto default;
//                    default:
//                        {
//                            /*
//                        * Anything else Start a new attribute in the
//                        * current tag token.
//                        */
//                            if (c >= 'A' && c <= 'Z')
//                            {
//                                /*
//                                 * U+0041 LATIN CAPITAL LETTER A through to
//                                 * U+005A LATIN CAPITAL LETTER Z Set that
//                                 * attribute's name to the lowercase TokenizerState.version
//                                 * of the current input character (add
//                                 * 0x0020 to the character's code point)
//                                 */
//                                c += (char)0x20;
//                            }
//                            /*
//                             * Set that attribute's name to the current
//                             * input character,
//                             */
//                            ClearStrBufAndAppend(c);
//                            /*
//                             * and its value to the empty string.
//                             */
//                            // Will do later.
//                            /*
//                             * Switch to the attribute name state.
//                             */
//                            //state = Transition(state, Tokenizer.ATTRIBUTE_NAME, reconsume, pos);
//                            state = TokenizerState.ATTRIBUTE_NAME;

//                        } return;
//                }
//            } while (textSnapshot.ReadNext(out c));


//        }

//        //TODO: remove  MyNotImplementException
//        class MyNotImplementException : Exception { }
//    }
//}