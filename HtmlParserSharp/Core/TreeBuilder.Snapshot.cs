/*
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
 * comment are quotes from the WHATWG HTML 5 spec as of 27 June 2007 
 * amended as of June 28 2007.
 * That document came with this statement:
 * © Copyright 2004-2007 Apple Computer, Inc., Mozilla Foundation, and 
 * Opera Software ASA. You are granted a license to use, reproduce and 
 * create derivative works of this document."
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using HtmlParserSharp.Common;
using System.Text;

#pragma warning disable 1591 // Missing XML comment
#pragma warning disable 1570 // XML comment on 'construct' has badly formed XML — 'reason'
#pragma warning disable 1587 // XML comment is not placed on a valid element

namespace HtmlParserSharp.Core
{
    partial class TreeBuilder<T> 
    {
        
        /// <summary>
        /// Creates a comparable snapshot of the tree builder state. Snapshot
        /// creation is only supported immediately after a script end tag has been
        /// processed. In C++ the caller is responsible for calling
        /// <code>delete</code> on the returned object.
        /// </summary>
        /// <returns>A snapshot</returns>
        internal ITreeBuilderState<T> NewSnapshot()
        {
            StackNode<T>[] listCopy = new StackNode<T>[listPtr + 1];
            for (int i = 0; i < listCopy.Length; i++)
            {
                StackNode<T> node = listOfActiveFormattingElements[i];
                if (node != null)
                {
                    StackNode<T> newNode = new StackNode<T>(node.Flags, node.ns,
                            node.name, node.node, node.popName,
                            node.attributes.CloneAttributes()
                        // [NOCPP[
                            , node.Locator
                        // ]NOCPP]
                    );
                    listCopy[i] = newNode;
                }
                else
                {
                    listCopy[i] = null;
                }
            }
            StackNode<T>[] stackCopy = new StackNode<T>[stackIndex + 1];
            for (int i = 0; i < stackCopy.Length; i++)
            {
                StackNode<T> node = stack[i];
                int listIndex = FindInListOfActiveFormattingElements(node);
                if (listIndex == -1)
                {
                    StackNode<T> newNode = new StackNode<T>(node.Flags, node.ns,
                            node.name, node.node, node.popName,
                            null
                        // [NOCPP[
                            , node.Locator
                        // ]NOCPP]
                    );
                    stackCopy[i] = newNode;
                }
                else
                {
                    stackCopy[i] = listCopy[listIndex];
                    stackCopy[i].Retain();
                }
            }
            return new StateSnapshot<T>(stackCopy, listCopy, formPointer, headPointer, deepTreeSurrogateParent, mode, originalMode, framesetOk, needToDropLF, quirks);
        }

        internal bool SnapshotMatches(ITreeBuilderState<T> snapshot)
        {
            StackNode<T>[] stackCopy = snapshot.Stack;
            int stackLen = snapshot.Stack.Length;
            StackNode<T>[] listCopy = snapshot.ListOfActiveFormattingElements;
            int listLen = snapshot.ListOfActiveFormattingElements.Length;

            if (stackLen != stackIndex + 1
                    || listLen != listPtr + 1
                    || formPointer != snapshot.FormPointer
                    || headPointer != snapshot.HeadPointer
                    || deepTreeSurrogateParent != snapshot.DeepTreeSurrogateParent
                    || mode != snapshot.Mode
                    || originalMode != snapshot.OriginalMode
                    || framesetOk != snapshot.IsFramesetOk
                    || needToDropLF != snapshot.IsNeedToDropLF
                    || quirks != snapshot.IsQuirks)
            { // maybe just assert quirks
                return false;
            }
            for (int i = listLen - 1; i >= 0; i--)
            {
                if (listCopy[i] == null
                        && listOfActiveFormattingElements[i] == null)
                {
                    continue;
                }
                else if (listCopy[i] == null
                      || listOfActiveFormattingElements[i] == null)
                {
                    return false;
                }
                if (listCopy[i].node != listOfActiveFormattingElements[i].node)
                {
                    return false; // it's possible that this condition is overly
                    // strict
                }
            }
            for (int i = stackLen - 1; i >= 0; i--)
            {
                if (stackCopy[i].node != stack[i].node)
                {
                    return false;
                }
            }
            return true;
        }

        internal void LoadState(ITreeBuilderState<T> snapshot)
        {
            StackNode<T>[] stackCopy = snapshot.Stack;
            int stackLen = snapshot.Stack.Length;
            StackNode<T>[] listCopy = snapshot.ListOfActiveFormattingElements;
            int listLen = snapshot.ListOfActiveFormattingElements.Length;

            for (int i = 0; i <= listPtr; i++)
            {
                if (listOfActiveFormattingElements[i] != null)
                {
                    listOfActiveFormattingElements[i].Release();
                }
            }
            if (listOfActiveFormattingElements.Length < listLen)
            {
                listOfActiveFormattingElements = new StackNode<T>[listLen];
            }
            listPtr = listLen - 1;

            for (int i = 0; i <= stackIndex; i++)
            {
                stack[i].Release();
            }
            if (stack.Length < stackLen)
            {
                stack = new StackNode<T>[stackLen];
            }
            stackIndex = stackLen - 1;

            for (int i = 0; i < listLen; i++)
            {
                StackNode<T> node = listCopy[i];
                if (node != null)
                {
                    StackNode<T> newNode = new StackNode<T>(node.Flags, node.ns,
                            node.name, node.node,
                            node.popName,
                            node.attributes.CloneAttributes()
                        // [NOCPP[
                            , node.Locator
                        // ]NOCPP]
                    );
                    listOfActiveFormattingElements[i] = newNode;
                }
                else
                {
                    listOfActiveFormattingElements[i] = null;
                }
            }
            for (int i = 0; i < stackLen; i++)
            {
                StackNode<T> node = stackCopy[i];
                int listIndex = FindInArray(node, listCopy);
                if (listIndex == -1)
                {
                    StackNode<T> newNode = new StackNode<T>(node.Flags, node.ns,
                            node.name, node.node,
                            node.popName,
                            null
                        // [NOCPP[
                            , node.Locator
                        // ]NOCPP]       
                    );
                    stack[i] = newNode;
                }
                else
                {
                    stack[i] = listOfActiveFormattingElements[listIndex];
                    stack[i].Retain();
                }
            }
            formPointer = snapshot.FormPointer;
            headPointer = snapshot.HeadPointer;
            deepTreeSurrogateParent = snapshot.DeepTreeSurrogateParent;
            mode = snapshot.Mode;
            originalMode = snapshot.OriginalMode;
            framesetOk = snapshot.IsFramesetOk;
            needToDropLF = snapshot.IsNeedToDropLF;
            quirks = snapshot.IsQuirks;
        }

        private int FindInArray(StackNode<T> node, StackNode<T>[] arr)
        {
            for (int i = listPtr; i >= 0; i--)
            {
                if (node == arr[i])
                {
                    return i;
                }
            }
            return -1;
        }

        public T FormPointer
        {
            get
            {
                return formPointer;
            }
        }

        public T HeadPointer
        {
            get
            {
                return headPointer;
            }
        }

        public T DeepTreeSurrogateParent
        {
            get
            {
                return deepTreeSurrogateParent;
            }
        }

        /// <summary>
        /// Gets the list of active formatting elements.
        /// </summary>
        public StackNode<T>[] ListOfActiveFormattingElements
        {
            get
            {
                return listOfActiveFormattingElements;
            }
        }

        /// <summary>
        /// Gets the stack.
        /// </summary>
        public StackNode<T>[] Stack
        {
            get
            {
                return stack;
            }
        }

        public InsertionMode Mode
        {
            get
            {
                return mode;
            }
        }

        public InsertionMode OriginalMode
        {
            get
            {
                return originalMode;
            }
        }

        public bool IsFramesetOk
        {
            get
            {
                return framesetOk;
            }
        }

        public bool IsNeedToDropLF
        {
            get
            {
                return needToDropLF;
            }
        }

        public bool IsQuirks
        {
            get
            {
                return quirks;
            }
        }
    }
}
