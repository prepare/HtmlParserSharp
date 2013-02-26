﻿/*
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using HtmlParserSharp.Portable;
using System.Xml;
using System.Text;

namespace HtmlParserSharp.Console
{
	/// <summary>
	/// This is contains a sample entry point for testing and benchmarks.
	/// </summary>
	public class Program
	{
		static SimpleHtmlParser parser = new SimpleHtmlParser();

		private static IEnumerable<FileInfo> GetTestFiles()
		{
			//DirectoryInfo dir = new DirectoryInfo("SampleData");
			//return dir.GetFiles("*.html", SearchOption.AllDirectories);
			for (int i = 0; i < 10; i++)
			{
				yield return new FileInfo(Path.Combine("SampleData", "test2.html"));
			}
		}

		public static void Main(string[] args)
		{
			//var fragment1 = parser.ParseStringFragment("<td>foo", "");
			//var fragment2 = parser.ParseStringFragment("<td>foo", "table");

			Stopwatch sw = new Stopwatch();



			System.Console.Write("Parsing ... ");
			var result = GetTestFiles().Select((file) =>
			    {
					sw.Restart();
				    var doc = parser.Parse(new StreamReader(file.FullName));
					sw.Stop();
					var parseTime = sw.Elapsed;
                    using (var stream = File.OpenWrite("test.xml"))
                    {
                        using (var writer = new XmlTextWriter(stream, Encoding.UTF8))
                        {
                            doc.Save(writer);
                        }
                    }
					//doc.Save("test.xml", SaveOptions.DisableFormatting);
					sw.Restart();
                    using (var streamRead = File.OpenRead("test.xml"))
                    {
                        using (var streamReader = new XmlTextReader(streamRead))
                        {
                            XDocument.Load(streamReader);
                        }
                    }
                    //XDocument.Load("test.xml");
					sw.Stop();
					var reparseTime = sw.Elapsed;
					return new { Document = doc, Time = parseTime, ReparseTime = reparseTime };
				}
				).ToList();

			TimeSpan total = result.Aggregate(new TimeSpan(), (passed, current) => passed + current.Time);
			TimeSpan reparseTotal = result.Aggregate(new TimeSpan(), (passed, current) => passed + current.ReparseTime);

			System.Console.WriteLine("done.");
			System.Console.WriteLine("Found " + result.Count + " documents.");
			System.Console.WriteLine();
			PrintTime("Total", total);
			PrintTime("First", result.First().Time);
			PrintTime("Average", TimeSpan.FromTicks(total.Ticks / result.Count));
			PrintTime("Average (without first)", TimeSpan.FromTicks((total.Ticks - result.First().Time.Ticks) / (result.Count - 1)));
			PrintTime("Min", result.Min(val => val.Time));
			PrintTime("Max", result.Max(val => val.Time));

			System.Console.WriteLine();
			System.Console.WriteLine("=== Reparsing (XDocument) ===");

			// note: reparsing using XmlDocument instead gives similar results

			PrintTime("Total", reparseTotal);
			PrintTime("First", result.First().ReparseTime);
			PrintTime("Average", TimeSpan.FromTicks(reparseTotal.Ticks / result.Count));
			PrintTime("Average (without first)", TimeSpan.FromTicks((reparseTotal.Ticks - result.First().ReparseTime.Ticks) / (result.Count - 1)));
			PrintTime("Min", result.Min(val => val.ReparseTime));
			PrintTime("Max", result.Max(val => val.ReparseTime));
			System.Console.ReadKey();
		}

		private static void PrintTime(string caption, TimeSpan time)
		{
			System.Console.WriteLine("{0}:\n  {1} ({2} ms)", caption, time.ToString(), time.TotalMilliseconds);
		}


	}
}
