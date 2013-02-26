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

		private static IEnumerable<FileInfo> GetTestFiles()
		{
			//DirectoryInfo dir = new DirectoryInfo("SampleData");
			//return dir.GetFiles("*.html", SearchOption.AllDirectories);
			for (int i = 0; i < 10; i++)
			{
				yield return new FileInfo(Path.Combine("SampleData", "test2.html"));
			}
		}

        private async static void Benchmark()
        {
            var sw = new Stopwatch();
            System.Console.WriteLine("Parsing");

            var results = new List<Tuple<TimeSpan, TimeSpan>>();
            for (int i = 0; i < 10; i++)
            {
                // Measure parsing time
                sw.Restart();
                var doc = await parser.Parse(new StreamReader("SampleData\\test2.html"));
                sw.Stop();
                var parseTime = sw.Elapsed;

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

                results.Add(new Tuple<TimeSpan, TimeSpan>(parseTime, reparseTime));
            }

            System.Console.WriteLine("Results ...");
            TimeSpan totalParseTime = TimeSpan.Zero, totalReparseTime = TimeSpan.Zero;
            for (int i = 0; i < results.Count; i++)
            {
                System.Console.WriteLine(String.Format("Iteration {0}: Parse: {1}ms Reparse: {2}ms", i, results[i].Item1.TotalMilliseconds, results[i].Item2.TotalMilliseconds));

                // Exclude first iteration
                if (i > 0)
                {
                    totalParseTime += results[i].Item1;
                    totalReparseTime += results[i].Item2;
                }
            }
            var averageParseTime = totalParseTime.TotalMilliseconds/results.Count - 1;
            var averageReparseTime = totalReparseTime.TotalMilliseconds/results.Count - 1;
            System.Console.Write(String.Format("Average parse time: {0}, Average reparse time: {1}, Ratio: {2}",
                                               averageParseTime, averageReparseTime, averageParseTime/averageReparseTime));
        }

		public static void Main(string[] args)
		{
		    Benchmark();
			System.Console.ReadKey();
		}
	}
}