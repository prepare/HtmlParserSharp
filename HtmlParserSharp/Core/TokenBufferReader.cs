using System;
using System.Text;
using System.Collections.Generic;

using System.Diagnostics;
using HtmlParserSharp.Common;
namespace HtmlParserSharp.Core
{

    class TokenBufferReader
    {
        int index = -1;
        int totalLength = 0;
        char[] buffer;
        int lineNumber;
        int columnNumber;

        public TokenBufferReader(char[] buffer)
        {
            this.buffer = buffer;
            this.totalLength = buffer.Length;
            this.index = -1;

            this.columnNumber = 1;//init at column 1
            this.lineNumber = 1; //init at line1
        }
        public int CurrentIndex
        {
            get { return this.index; }
        }
        public bool ReadNext(out char c)
        {
            //1. move 
            if (index < totalLength)
            {
                c = buffer[++index];
                switch (c)
                {
                    case '\n':
                        {
                            //reset 
                            this.lineNumber++;
                            this.columnNumber = 1;//reset
                        } break;
                }
                return true;
            }
            else
            {
                c = '\0';
                return false;
            }
        }
        public bool Peek(out char c)
        {
            if (index < totalLength)
            {
                c = buffer[index + 1];
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
            this.index--;
        }
        public void StartCollect()
        {
            //this.cstart = this.index
        }
        public void SkipOneAndStartCollect()
        {
            //this.cstart = this.index
        }
    }

}