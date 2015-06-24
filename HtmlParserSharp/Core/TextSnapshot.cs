//2014,2015 ,BSD, WinterDev 
using System;
namespace HtmlParserSharp.Core
{

    class TextSnapshotReader
    {
        int _start;
        int _end;
        int _length;
        char[] _textBuffer;
        int _position;
        bool isEndOfFile = false;
        char currentChar;
        public TextSnapshotReader(char[] textBuffer, int start, int length)
        {
            this._position = start;
            this._start = start;
            this._length = length;
            this._end = start + length;
            this._textBuffer = textBuffer;
            this.isEndOfFile = this._start >= this._end;
            if (!this.isEndOfFile)
            {
                currentChar = textBuffer[start];
            }
        }
        public int Position
        {
            get { return this._position; }
        }
        public bool ReadNext(out char c)
        {
            if (this._start < this._end)
            {
                this._position++;
                this.currentChar = c = this._textBuffer[this._position];
                return true;
            }
            else
            {
                c = '\0';
                return false;
            }
        }
        public void StepBack()
        {
            if (this._position > _start)
            {
                this._position--;
            }
        }
        public char CurrentChar
        {
            get { return this.currentChar; }
        }
        public bool EndOfStream
        {
            get { return this._position >= this._end; }
        }
        internal char[] InteralBuff
        {
            get { return this._textBuffer; }
        }
        internal bool Copy(char[] output, int startIndex, int nChars)
        {
            if (startIndex + nChars < _length)
            {
                //ok copy
                Array.Copy(_textBuffer, startIndex, output, 0, nChars);
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    class TextSnapshot
    {
        readonly char[] textBuffer;
        readonly int length;
        public TextSnapshot(char[] textBuffer)
        {
            this.textBuffer = textBuffer;
            this.length = textBuffer.Length;
        }

        public TextSnapshot(string str)
        {
            this.textBuffer = str.ToCharArray();
            this.length = textBuffer.Length;
        }
        public int Length
        {
            get
            {
                return this.length;
            }
        }
        public char this[int index]
        {
            get
            {
                return this.textBuffer[index];
            }
        }

        //--------------
        public static char[] UnsafeGetInternalBuffer(TextSnapshot snap)
        {
            return snap.textBuffer;
        }
        public char[] Copy(int index, int length)
        {
            char[] newbuff = new char[length];
            Array.Copy(this.textBuffer, index, newbuff, 0, length);
            return newbuff;
        }
        public string Substring(int index, int length)
        {
            return new string(textBuffer, index, length);
        }
        public int IndexOf(char c)
        {
            return IndexOf(c, 0);
        }


        public int IndexOf(char c, int start)
        {
            char[] tmpChars = this.textBuffer;
            int lim = tmpChars.Length;
            unsafe
            {
                fixed (char* start0 = &this.textBuffer[0])
                {
                    char* curChar = start0 + start;
                    for (int i = start; i < lim; ++i)
                    {
                        if (*curChar == c)
                        {
                            return i;
                        }
                        curChar++;
                    }
                }
            }
            return -1;
        }

        internal int IndexOf(char c1, char c2, char c3, int start)
        {
            char[] tmpChars = this.textBuffer;
            int lim = length - 3;
            if (start < lim)
            {

                int i = start;
                char ex1 = tmpChars[i];
                char ex2 = tmpChars[i + 1];
                char ex3 = tmpChars[i + 2];

                do
                {
                    if (ex1 == c1 && ex2 == c2 && ex3 == c3)
                    {
                        return i;
                    }
                    i++;

                    ex1 = ex2;
                    ex2 = ex3;
                    ex3 = tmpChars[i];

                } while (i < lim);
            }
            //not found
            return -1;
        }
    }




}