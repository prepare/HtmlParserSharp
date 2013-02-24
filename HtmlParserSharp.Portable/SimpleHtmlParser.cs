﻿/*
 * Copyright (c) 2012 Patrick Reisert
 * Copyright (c) 2005, 2006, 2007 Henri Sivonen
 * Copyright (c) 2007-2008 Mozilla Foundation
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

using System;
using System.IO;
using HtmlParserSharp.Portable.Core;
using System.Xml.Linq;

namespace HtmlParserSharp.Portable
{
	/// <summary>
	/// This is a simple API for the parsing process.
	/// Part of this is a port of the nu.validator.htmlparser.io.Driver class.
	/// The parser currently ignores the encoding in the html source and parses everything as UTF-8.
	/// </summary>
	public class SimpleHtmlParser
	{
		private Tokenizer _tokenizer;
		private DomTreeBuilder _treeBuilder;

        //public XmlDocumentFragment ParseStringFragment(string str, string fragmentContext)
        //{
        //    using (var reader = new StringReader(str))
        //        return ParseFragment(reader, fragmentContext);
        //}

		public XDocument ParseString(string str)
		{
			using (var reader = new StringReader(str))
				return Parse(reader);
		}

        //public XDocument Parse(string path)
        //{
        //    using (var reader = new StreamReader(path))
        //        return Parse(reader);
        //}

		public XDocument Parse(TextReader reader)
		{
			Reset();
			Tokenize(reader, swallowBom: true);
			return _treeBuilder.Document;
		}

        //public XmlDocumentFragment ParseFragment(TextReader reader, string fragmentContext)
        //{
        //    Reset();
        //    treeBuilder.SetFragmentContext(fragmentContext);
        //    Tokenize(reader);
        //    return treeBuilder.getDocumentFragment();
        //}

		private void Reset()
		{
			_treeBuilder = new DomTreeBuilder();
			_tokenizer = new Tokenizer(_treeBuilder, false);
			_treeBuilder.IsIgnoringComments = false;

			// optionally: report errors and more

			//treeBuilder.ErrorEvent +=
			//    (sender, a) =>
			//    {
			//        ILocator loc = tokenizer as ILocator;
			//        Console.WriteLine("{0}: {1} (Line: {2})", a.IsWarning ? "Warning" : "Error", a.Message, loc.LineNumber);
			//    };
			//treeBuilder.DocumentModeDetected += (sender, a) => Console.WriteLine("Document mode: " + a.Mode.ToString());
			//tokenizer.EncodingDeclared += (sender, a) => Console.WriteLine("Encoding: " + a.Encoding + " (currently ignored)");
		}

		private void Tokenize(TextReader reader, bool swallowBom)
		{
			if (reader == null)
			{
			    throw new ArgumentNullException("reader");
			}

			_tokenizer.Start();

			try
			{
				var buffer = new char[2048];
				var bufr = new UTF16Buffer(buffer, 0, 0);
				var lastWasCR = false;
				int len;
				if ((len = reader.Read(buffer, 0, buffer.Length)) != 0)
				{
					var streamOffset = 0;
					var offset = 0;
					var length = len;
					if (swallowBom)
					{
						if (buffer[0] == '\uFEFF')
						{
							streamOffset = -1;
							offset = 1;
							length--;
						}
					}
					if (length > 0)
					{
						_tokenizer.SetTransitionBaseOffset(streamOffset);
						bufr.Start = offset;
						bufr.End = offset + length;
						while (bufr.HasMore)
						{
							bufr.Adjust(lastWasCR);
							lastWasCR = false;
							if (bufr.HasMore)
							{
								lastWasCR = _tokenizer.TokenizeBuffer(bufr);
							}
						}
					}
					streamOffset = length;
					while ((len = reader.Read(buffer, 0, buffer.Length)) != 0)
					{
						_tokenizer.SetTransitionBaseOffset(streamOffset);
						bufr.Start = 0;
						bufr.End = len;
						while (bufr.HasMore)
						{
							bufr.Adjust(lastWasCR);
							lastWasCR = false;
							if (bufr.HasMore)
							{
								lastWasCR = _tokenizer.TokenizeBuffer(bufr);
							}
						}
						streamOffset += len;
					}
				}
				_tokenizer.Eof();
			}
			finally
			{
				_tokenizer.End();
			}
		}
	}
}