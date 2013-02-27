/*
 * Copyright (c) 2012 Patrick Reisert
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

using HtmlParserSharp.Portable;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace HtmlParserSharp.Console
{
	/// <summary>
	/// This is contains a sample entry point for testing and benchmarks.
	/// </summary>
	public class Program
	{
		static readonly SimpleHtmlParser parser = new SimpleHtmlParser();

	    private async static void Benchmark()
        {
            var sw = new Stopwatch();
            System.Console.WriteLine("Parsing");

            var results = new List<Tuple<TimeSpan, TimeSpan, TimeSpan>>();
            for (int i = 0; i < 10; i++)
            {
                // Measure parsing time
                sw.Restart();
                XDocument doc = await parser.Parse(new StreamReader("SampleData\\test2.html"));
                var encoding = parser.DocumentEncoding;
                sw.Stop();
                var parseTime = sw.Elapsed;

                // Find all of the anchor elements

                sw.Restart();
                XNamespace xhtmlNamespace = "http://www.w3.org/1999/xhtml";
                var metas = from meta in doc.Root.Descendants(xhtmlNamespace + "meta")
                            select new {Charset = meta.Attribute("charset")};
                if (metas.Count() > 0)
                {
                    foreach (var metatag in metas)
                    {
                        System.Console.WriteLine(metatag.Charset);
                    }
                }
                var anchors = from anchor in doc.Root.Descendants(xhtmlNamespace + "a")
                              select new {AnchorNode = anchor, Uri = anchor.Attribute("href").ToString()};
                sw.Stop();
                var queryTime = sw.Elapsed;

                // Exclude file write time
                using (var stream = File.OpenWrite("test" + i + ".xml"))
                {
                    using (var writer = new XmlTextWriter(stream, Encoding.UTF8))
                    {
                        doc.Save(writer);
                    }
                }

                // Measure reparse time
                sw.Restart();
                using (var streamRead = File.OpenRead("test" + i + ".xml"))
                {
                    using (var streamReader = new XmlTextReader(streamRead))
                    {
                        XDocument.Load(streamReader);
                    }
                }
                sw.Stop();
                var reparseTime = sw.Elapsed;

                results.Add(new Tuple<TimeSpan, TimeSpan, TimeSpan>(parseTime, queryTime, reparseTime));
            }

            System.Console.WriteLine("Results ...");
            TimeSpan totalParseTime = TimeSpan.Zero, totalQueryTime = TimeSpan.Zero, totalReparseTime = TimeSpan.Zero;
            for (int i = 0; i < results.Count; i++)
            {
                System.Console.WriteLine(String.Format("Iteration {0}: Parse: {1:0.0}ms Query: {2:0.000000}ms Reparse: {3:0.0}ms", i, results[i].Item1.TotalMilliseconds, results[i].Item2.TotalMilliseconds, results[i].Item3.TotalMilliseconds));

                // Exclude first iteration
                if (i > 0)
                {
                    totalParseTime += results[i].Item1;
                    totalQueryTime += results[i].Item2;
                    totalReparseTime += results[i].Item3;
                }
            }
            var averageParseTime = totalParseTime.TotalMilliseconds/(results.Count - 1);
            var averageQueryTime = totalQueryTime.TotalMilliseconds/(results.Count - 1);
            var averageReparseTime = totalReparseTime.TotalMilliseconds/(results.Count - 1);
            System.Console.Write(String.Format("Average parse time: {0:0.0}ms, Average reparse time: {1:0.0}ms, Ratio: {2:0.0}, Average query time: {3:0.000000}",
                                               averageParseTime, averageReparseTime, averageParseTime/averageReparseTime, averageQueryTime));
        }

		public static void Main(string[] args)
		{
		    Benchmark();
			System.Console.ReadKey();
		}
	}
}