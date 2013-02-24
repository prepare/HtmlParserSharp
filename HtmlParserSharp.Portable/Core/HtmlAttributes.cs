/*
 * Copyright (c) 2007 Henri Sivonen
 * Copyright (c) 2008-2011 Mozilla Foundation
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
using System.Diagnostics;
using HtmlParserSharp.Portable.Common;

namespace HtmlParserSharp.Portable.Core
{
	/// <summary>
	/// Be careful with this class. QName is the name in from HTML tokenization.
	/// Otherwise, please refer to the interface doc.
	/// </summary>
	public sealed class HtmlAttributes : IEquatable<HtmlAttributes> /* : Sax.IAttributes*/ {

		// [NOCPP[

		private static readonly AttributeName[] EMPTY_ATTRIBUTENAMES = new AttributeName[0];

		private static readonly string[] EMPTY_stringS = new string[0];

		// ]NOCPP]

		public static readonly HtmlAttributes EMPTY_ATTRIBUTES = new HtmlAttributes(AttributeName.HTML);

		private int _mode;

		private int _length;

		private AttributeName[] _names;

		private string[] _values;

		// [NOCPP[

		private string _idValue;

		private int _xmlnsLength;

		private AttributeName[] _xmlnsNames;

		private string[] _xmlnsValues;

		// ]NOCPP]

		public HtmlAttributes(int mode)
		{
			this._mode = mode;
			this._length = 0;
			/*
			 * The length of 5 covers covers 98.3% of elements
			 * according to Hixie
			 */
			this._names = new AttributeName[5];
			this._values = new string[5];

			// [NOCPP[

			this._idValue = null;

			this._xmlnsLength = 0;

			this._xmlnsNames = HtmlAttributes.EMPTY_ATTRIBUTENAMES;

			this._xmlnsValues = HtmlAttributes.EMPTY_stringS;

			// ]NOCPP]
		}
		/*
		public HtmlAttributes(HtmlAttributes other) {
			this.mode = other.mode;
			this.length = other.length;
			this.names = new AttributeName[other.length];
			this.values = new string[other.length];
			// [NOCPP[
			this.idValue = other.idValue;
			this.xmlnsLength = other.xmlnsLength;
			this.xmlnsNames = new AttributeName[other.xmlnsLength];
			this.xmlnsValues = new string[other.xmlnsLength];
			// ]NOCPP]
		}
		*/

		/// <summary>
		/// Only use with a static argument
		/// </summary>
		public int GetIndex(AttributeName name)
		{
			for (int i = 0; i < _length; i++)
			{
				if (_names[i] == name)
				{
					return i;
				}
			}
			return -1;
		}

		// [NOCPP[

		public int GetIndex(string qName)
		{
			for (int i = 0; i < _length; i++)
			{
				if (_names[i].GetQName(_mode) == qName)
				{
					return i;
				}
			}
			return -1;
		}

		public int GetIndex(string uri, string localName)
		{
			for (int i = 0; i < _length; i++)
			{
				if (_names[i].GetLocal(_mode) == localName
						&& _names[i].GetUri(_mode) == uri)
				{
					return i;
				}
			}
			return -1;
		}

		public string GetType(string qName)
		{
			int index = GetIndex(qName);
			if (index == -1)
			{
				return null;
			}
			else
			{
				return GetType(index);
			}
		}

		public string GetType(string uri, string localName)
		{
			int index = GetIndex(uri, localName);
			if (index == -1)
			{
				return null;
			}
			else
			{
				return GetType(index);
			}
		}

		public string GetValue(string qName)
		{
			int index = GetIndex(qName);
			if (index == -1)
			{
				return null;
			}
			else
			{
				return GetValue(index);
			}
		}

		public string GetValue(string uri, string localName)
		{
			int index = GetIndex(uri, localName);
			if (index == -1)
			{
				return null;
			}
			else
			{
				return GetValue(index);
			}
		}

		// ]NOCPP]

		public int Length
		{
			get
			{
				return _length;
			}
		}

		[Local]
		public string GetLocalName(int index)
		{
			if (index < _length && index >= 0)
			{
				return _names[index].GetLocal(_mode);
			}
			else
			{
				return null;
			}
		}

		// [NOCPP[

		public string GetQName(int index)
		{
			if (index < _length && index >= 0)
			{
				return _names[index].GetQName(_mode);
			}
			else
			{
				return null;
			}
		}

		public string GetType(int index)
		{
			if (index < _length && index >= 0)
			{
				return (_names[index] == AttributeName.ID) ? "ID" : "CDATA";
			}
			else
			{
				return null;
			}
		}

		// ]NOCPP]

		public AttributeName GetAttributeName(int index)
		{
			if (index < _length && index >= 0)
			{
				return _names[index];
			}
			else
			{
				return null;
			}
		}

		[NsUri]
		public string GetURI(int index)
		{
			if (index < _length && index >= 0)
			{
				return _names[index].GetUri(_mode);
			}
			else
			{
				return null;
			}
		}

		[Prefix]
		public string GetPrefix(int index)
		{
			if (index < _length && index >= 0)
			{
				return _names[index].GetPrefix(_mode);
			}
			else
			{
				return null;
			}
		}

		public string GetValue(int index)
		{
			if (index < _length && index >= 0)
			{
				return _values[index];
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Only use with static argument.
		/// </summary>
		public string GetValue(AttributeName name)
		{
			int index = GetIndex(name);
			if (index == -1)
			{
				return null;
			}
			else
			{
				return GetValue(index);
			}
		}

		// [NOCPP[

		public string Id
		{
			get
			{
				return _idValue;
			}
		}

		public int XmlnsLength
		{
			get
			{
				return _xmlnsLength;
			}
		}

		[Local]
		public string GetXmlnsLocalName(int index)
		{
			if (index < _xmlnsLength && index >= 0)
			{
				return _xmlnsNames[index].GetLocal(_mode);
			}
			else
			{
				return null;
			}
		}

		[NsUri]
		public string GetXmlnsURI(int index)
		{
			if (index < _xmlnsLength && index >= 0)
			{
				return _xmlnsNames[index].GetUri(_mode);
			}
			else
			{
				return null;
			}
		}

		public string GetXmlnsValue(int index)
		{
			if (index < _xmlnsLength && index >= 0)
			{
				return _xmlnsValues[index];
			}
			else
			{
				return null;
			}
		}

		public int GetXmlnsIndex(AttributeName name)
		{
			for (int i = 0; i < _xmlnsLength; i++)
			{
				if (_xmlnsNames[i] == name)
				{
					return i;
				}
			}
			return -1;
		}

		public string GetXmlnsValue(AttributeName name)
		{
			int index = GetXmlnsIndex(name);
			if (index == -1)
			{
				return null;
			}
			else
			{
				return GetXmlnsValue(index);
			}
		}

		public AttributeName GetXmlnsAttributeName(int index)
		{
			if (index < _xmlnsLength && index >= 0)
			{
				return _xmlnsNames[index];
			}
			else
			{
				return null;
			}
		}

		// ]NOCPP]

		internal void AddAttribute(AttributeName name, string value
			// [NOCPP[
				, XmlViolationPolicy xmlnsPolicy
			// ]NOCPP]        
		)
		{
			// [NOCPP[
			if (name == AttributeName.ID)
			{
				_idValue = value;
			}

			if (name.IsXmlns)
			{
				if (_xmlnsNames.Length == _xmlnsLength)
				{
					int newLen = _xmlnsLength == 0 ? 2 : _xmlnsLength << 1;
					AttributeName[] newNames = new AttributeName[newLen];
					Array.Copy(_xmlnsNames, newNames, _xmlnsNames.Length);
					_xmlnsNames = newNames;
					string[] newValues = new string[newLen];
					Array.Copy(_xmlnsValues, newValues, _xmlnsValues.Length);
					_xmlnsValues = newValues;
				}
				_xmlnsNames[_xmlnsLength] = name;
				_xmlnsValues[_xmlnsLength] = value;
				_xmlnsLength++;
				switch (xmlnsPolicy)
				{
					case XmlViolationPolicy.Fatal:
						// this is ugly (TODO)
						throw new Exception("Saw an xmlns attribute.");
					case XmlViolationPolicy.AlterInfoset:
						return;
					case XmlViolationPolicy.Allow:
						break; // fall through
				}
			}

			// ]NOCPP]

			if (_names.Length == _length)
			{
				int newLen = _length << 1; // The first growth covers virtually
				// 100% of elements according to
				// Hixie
				AttributeName[] newNames = new AttributeName[newLen];
				Array.Copy(_names, newNames, _names.Length);
				_names = newNames;
				string[] newValues = new string[newLen];
				Array.Copy(_values, newValues, _values.Length);
				_values = newValues;
			}
			_names[_length] = name;
			_values[_length] = value;
			_length++;
		}

		internal void Clear(int m)
		{
			for (int i = 0; i < _length; i++)
			{
				_names[i] = null;
				_values[i] = null;
			}
			_length = 0;
			_mode = m;
			// [NOCPP[
			_idValue = null;
			for (int i = 0; i < _xmlnsLength; i++)
			{
				_xmlnsNames[i] = null;
				_xmlnsValues[i] = null;
			}
			_xmlnsLength = 0;
			// ]NOCPP]
		}

		/// <summary>
		/// This is only used for <code>AttributeName</code> ownership transfer
		/// in the isindex case to avoid freeing custom names twice in C++.
		/// </summary>
		internal void ClearWithoutReleasingContents()
		{
			for (int i = 0; i < _length; i++)
			{
				_names[i] = null;
				_values[i] = null;
			}
			_length = 0;
		}

		public bool Contains(AttributeName name)
		{
			for (int i = 0; i < _length; i++)
			{
				if (name.EqualsAnother(_names[i]))
				{
					return true;
				}
			}
			// [NOCPP[
			for (int i = 0; i < _xmlnsLength; i++)
			{
				if (name.EqualsAnother(_xmlnsNames[i]))
				{
					return true;
				}
			}
			// ]NOCPP]
			return false;
		}

		public void AdjustForMath()
		{
			_mode = AttributeName.MATHML;
		}

		public void AdjustForSvg()
		{
			_mode = AttributeName.SVG;
		}

		public HtmlAttributes CloneAttributes()
		{
			Debug.Assert((_length == 0 && _xmlnsLength == 0) || _mode == 0 || _mode == 3);
			HtmlAttributes clone = new HtmlAttributes(0);
			for (int i = 0; i < _length; i++)
			{
				clone.AddAttribute(_names[i].CloneAttributeName(), _values[i]
					// [NOCPP[
					   , XmlViolationPolicy.Allow
					// ]NOCPP]
				);
			}
			// [NOCPP[
			for (int i = 0; i < _xmlnsLength; i++)
			{
				clone.AddAttribute(_xmlnsNames[i],
						_xmlnsValues[i], XmlViolationPolicy.Allow);
			}
			// ]NOCPP]
			return clone; // XXX!!!
		}

		public bool Equals(HtmlAttributes other)
		{
			Debug.Assert(_mode == 0 || _mode == 3, "Trying to compare attributes in foreign content.");
			int otherLength = other.Length;
			if (_length != otherLength)
			{
				return false;
			}
			for (int i = 0; i < _length; i++)
			{
				// Work around the limitations of C++
				bool found = false;
				// The comparing just the local names is OK, since these attribute
				// holders are both supposed to belong to HTML formatting elements
				/*[Local]*/
				string ownLocal = _names[i].GetLocal(AttributeName.HTML);
				for (int j = 0; j < otherLength; j++)
				{
					if (ownLocal == other._names[j].GetLocal(AttributeName.HTML))
					{
						found = true;
						if (_values[i] != other._values[j])
						{
							return false;
						}
					}
				}
				if (!found)
				{
					return false;
				}
			}
			return true;
		}

		// [NOCPP[

		internal void ProcessNonNcNames<T>(TreeBuilder<T> treeBuilder, XmlViolationPolicy namePolicy) where T : class
		{
			for (int i = 0; i < _length; i++)
			{
				AttributeName attName = _names[i];
				if (!attName.IsNcName(_mode))
				{
					string name = attName.GetLocal(_mode);
					switch (namePolicy)
					{
						case XmlViolationPolicy.AlterInfoset:
							_names[i] = AttributeName.Create(NCName.EscapeName(name));
							goto case XmlViolationPolicy.Allow; // fall through
						case XmlViolationPolicy.Allow:
							if (attName != AttributeName.XML_LANG)
							{
								treeBuilder.Warn("Attribute \u201C" + name + "\u201D is not serializable as XML 1.0.");
							}
							break;
						case XmlViolationPolicy.Fatal:
							treeBuilder.Fatal("Attribute \u201C" + name + "\u201D is not serializable as XML 1.0.");
							break;
					}
				}
			}
		}

		public void Merge(HtmlAttributes attributes)
		{
			int len = attributes.Length;
			for (int i = 0; i < len; i++)
			{
				AttributeName name = attributes.GetAttributeName(i);
				if (!Contains(name))
				{
					AddAttribute(name, attributes.GetValue(i), XmlViolationPolicy.Allow);
				}
			}
		}

		// ]NOCPP]
	}
}
