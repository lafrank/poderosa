﻿// Copyright 2004-2017 The Poderosa Project.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.IO;
using System.Collections;
using System.Drawing;
using System.Diagnostics;
using System.Text;

//using Poderosa.Util;

namespace Poderosa.Document
{

	/// <summary>
	/// Color type for the background color and fore color.
	/// </summary>
	public enum ColorType
	{
		/// <summary>Use default color</summary>
		Default,
		/// <summary>Use 8 bit color code</summary>
		Custom8bit,
		/// <summary>Use 24 bit color</summary>
		Custom24bit,
	}

	/// <summary>
	/// Color specification
	/// </summary>
	public struct ColorSpec
	{
		private readonly ColorType _colorType;
		private readonly int _colorValue;

		/// <summary>
		/// Instance that indicates to use default color
		/// </summary>
		public static ColorSpec Default
		{
			get
			{
				return new ColorSpec(ColorType.Default, 0);
			}
		}

		/// <summary>
		/// Color type
		/// </summary>
		public ColorType ColorType
		{
			get
			{
				return _colorType;
			}
		}

		/// <summary>
		/// 24 bit color
		/// </summary>
		public Color Color
		{
			get
			{
				return (_colorType == ColorType.Custom24bit) ? Color.FromArgb(_colorValue) : Color.Empty;
			}
		}

		/// <summary>
		/// 8 bit color code
		/// </summary>
		public int ColorCode
		{
			get
			{
				return (_colorType == ColorType.Custom8bit) ? _colorValue : 0;
			}
		}

		/// <summary>
		/// Internal constructor
		/// </summary>
		/// <param name="colorType">color type</param>
		/// <param name="colorValue">color value (24 bit ARGB or 8 bit color code)</param>
		private ColorSpec(ColorType colorType, int colorValue)
		{
			_colorType = colorType;
			_colorValue = colorValue;
		}

		/// <summary>
		/// Constructor for creating an instance that indicates to use 24 bit color.
		/// </summary>
		/// <param name="color">color. empty color indicates to use default color.</param>
		public ColorSpec(Color color)
		{
			if (color.IsEmpty)
			{
				_colorType = ColorType.Default;
				_colorValue = 0;
			}
			else
			{
				_colorType = ColorType.Custom24bit;
				_colorValue = color.ToArgb();
			}
		}

		/// <summary>
		/// Constructor for creating an instance that indicates to use 8 bit color.
		/// </summary>
		/// <param name="color">color code</param>
		public ColorSpec(int color)
		{
			_colorType = ColorType.Custom8bit;
			_colorValue = color & 0xff;
		}

		public override string ToString()
		{
			switch (_colorType)
			{
				case ColorType.Default:
					return "Default";
				case ColorType.Custom8bit:
					return new StringBuilder()
							.Append('{').Append(_colorValue).Append('}').ToString();
				case ColorType.Custom24bit:
					Color c = Color.FromArgb(_colorValue);
					return new StringBuilder()
							.Append('{').Append(c.A).Append(',').Append(c.R).Append(',').Append(c.G).Append(',').Append(c.B).Append('}').ToString();
				default:
					return "???";
			}
		}
	}

	/// <summary>
	/// Text decoration.
	/// </summary>
	/// <remarks>
	/// The instance is immutable.
	/// </remarks>
	/// <exclude/>
	public sealed class TextDecoration
	{
		private readonly GAttr _attr;
		private readonly GColor24 _color24;

		private static readonly TextDecoration _default =
				new TextDecoration(GAttr.Default, new GColor24());

		/// <summary>
		/// Get a default decoration.
		/// "default decoration" means that text is displayed
		/// with default text color, default background color,
		/// no underline, and no bold.
		/// </summary>
		public static TextDecoration Default
		{
			get
			{
				return _default;
			}
		}

		internal GAttr Attr
		{
			get
			{
				return _attr;
			}
		}

		internal GColor24 Color24
		{
			get
			{
				return _color24;
			}
		}

		public bool Blink
		{
			get
			{
				return _attr.Has(GAttrFlags.Blink);
			}
		}

		public bool Hidden
		{
			get
			{
				return _attr.Has(GAttrFlags.Hidden);
			}
		}

		public bool Bold
		{
			get
			{
				return _attr.Has(GAttrFlags.Bold);
			}
		}

		public bool Underline
		{
			get
			{
				return _attr.Has(GAttrFlags.Underlined);
			}
		}

		public bool Inverted
		{
			get
			{
				return _attr.Has(GAttrFlags.Inverted);
			}
		}

		private TextDecoration(GAttr attr, GColor24 color24)
		{
			_attr = attr;
			_color24 = color24;
		}

		public TextDecoration(Color backColor, Color foreColor, bool underline, bool bold)
		{
			_color24 = new GColor24();
			_color24.ForeColor = foreColor;
			_color24.BackColor = backColor;
			GAttrFlags flags = GAttrFlags.Use24bitBackColor | GAttrFlags.Use24bitForeColor;
			if (underline) flags = flags | GAttrFlags.Underlined;
			if (bold) flags = flags | GAttrFlags.Bold;
			_attr = new GAttr(0, 1, flags);
		}

		/// <summary>
		/// Determines this instance was the default decoration.
		/// </summary>
		public bool IsDefault
		{
			get
			{
				return _attr == _default._attr
						&& _color24 == _default._color24;
			}
		}

		/// <summary>
		/// Gets <see cref="ColorSpec"/> of the fore color.
		/// </summary>
		/// <returns><see cref="ColorSpec"/> of the fore color</returns>
		public ColorSpec GetForeColorSpec()
		{
			if (_attr.Has(GAttrFlags.Use8bitForeColor))
			{
				return new ColorSpec(_attr.ForeColor);
			}
			if (_attr.Has(GAttrFlags.Use24bitForeColor))
			{
				return new ColorSpec(_color24.ForeColor);
			}
			return ColorSpec.Default;
		}

		/// <summary>
		/// Gets <see cref="ColorSpec"/> of the background color.
		/// </summary>
		/// <returns><see cref="ColorSpec"/> of the background color</returns>
		public ColorSpec GetBackColorSpec()
		{
			if (_attr.Has(GAttrFlags.Use8bitBackColor))
			{
				return new ColorSpec(_attr.BackColor);
			}
			if (_attr.Has(GAttrFlags.Use24bitBackColor))
			{
				return new ColorSpec(_color24.BackColor);
			}
			return ColorSpec.Default;
		}

		/// <summary>
		/// Get a new copy whose text color was set.
		/// </summary>
		/// <returns>new instance</returns>
		public TextDecoration GetCopyWithForeColor(ColorSpec foreColor)
		{
			GAttr newAttr;
			GColor24 newColor24 = new GColor24();
			newColor24.BackColor = _color24.BackColor;

			switch (foreColor.ColorType)
			{
				case ColorType.Custom8bit:
					newAttr = _attr.CopyWith8bitForeColor(foreColor.ColorCode);
					break;
				case ColorType.Custom24bit:
					newAttr = _attr.CopyWith24bitForeColor();
					newColor24.ForeColor = foreColor.Color;
					break;
				default:
				case ColorType.Default:
					newAttr = _attr.CopyWithDefaultForeColor();
					break;
			}
			return new TextDecoration(newAttr, newColor24);
		}

		/// <summary>
		/// Get a new copy whose background color was set.
		/// </summary>
		/// <returns>new instance</returns>
		public TextDecoration GetCopyWithBackColor(ColorSpec backColor)
		{
			GAttr newAttr;
			GColor24 newColor24 = new GColor24();
			newColor24.ForeColor = _color24.ForeColor;

			switch (backColor.ColorType)
			{
				case ColorType.Custom8bit:
					newAttr = _attr.CopyWith8bitBackColor(backColor.ColorCode);
					break;
				case ColorType.Custom24bit:
					newAttr = _attr.CopyWith24bitBackColor();
					newColor24.BackColor = backColor.Color;
					break;
				default:
				case ColorType.Default:
					newAttr = _attr.CopyWithDefaultBackColor();
					break;
			}
			return new TextDecoration(newAttr, newColor24);
		}

		/// <summary>
		/// Get a new copy whose inversion flag was set.
		/// </summary>
		/// <returns>new instance</returns>
		public TextDecoration GetCopyWithInverted(bool inverted)
		{
			return new TextDecoration(
					inverted ? _attr + GAttrFlags.Inverted : _attr - GAttrFlags.Inverted, _color24);
		}


		/// <summary>
		/// Get a new copy whose underline status was set.
		/// </summary>
		/// <param name="underline">new underline status</param>
		/// <returns>new instance</returns>
		public TextDecoration GetCopyWithUnderline(bool underline)
		{
			return new TextDecoration(
					underline ? _attr + GAttrFlags.Underlined : _attr - GAttrFlags.Underlined, _color24);
		}

		/// <summary>
		/// Get a new copy whose bold status was set.
		/// </summary>
		/// <param name="bold">new bold status</param>
		/// <returns>new instance</returns>
		public TextDecoration GetCopyWithBold(bool bold)
		{
			return new TextDecoration(
					bold ? _attr + GAttrFlags.Bold : _attr - GAttrFlags.Bold, _color24);
		}

		/// <summary>
		/// Get a new copy whose hidden status was set.
		/// </summary>
		/// <param name="hidden">new hidden status</param>
		/// <returns>new instance</returns>
		public TextDecoration GetCopyWithHidden(bool hidden)
		{
			return new TextDecoration(
					hidden ? _attr + GAttrFlags.Hidden : _attr - GAttrFlags.Hidden, _color24);
		}

		/// <summary>
		/// Get a new copy whose blink status was set.
		/// </summary>
		/// <param name="blink">new blink status</param>
		/// <returns>new instance</returns>
		public TextDecoration GetCopyWithBlink(bool blink)
		{
			return new TextDecoration(
					blink ? _attr + GAttrFlags.Blink : _attr - GAttrFlags.Blink, _color24);
		}

		public override string ToString()
		{
			StringBuilder s = new StringBuilder();
			s.Append("{Back=");
			if (_attr.Has(GAttrFlags.Use8bitBackColor))
			{
				s.Append(_attr.BackColor.ToString());
			}
			else if (_attr.Has(GAttrFlags.Use24bitBackColor))
			{
				s.Append(_color24.BackColor.ToString());
			}
			else
			{
				s.Append("default");
			}

			s.Append(",Fore=");
			if (_attr.Has(GAttrFlags.Use8bitForeColor))
			{
				s.Append(_attr.ForeColor.ToString());
			}
			else if (_attr.Has(GAttrFlags.Use24bitForeColor))
			{
				s.Append(_color24.ForeColor.ToString());
			}
			else
			{
				s.Append("default");
			}

			if (_attr.Has(GAttrFlags.Blink))
			{
				s.Append(",Blink");
			}
			if (_attr.Has(GAttrFlags.Bold))
			{
				s.Append(",Bold");
			}
			if (_attr.Has(GAttrFlags.Underlined))
			{
				s.Append(",Underlined");
			}
			if (_attr.Has(GAttrFlags.Inverted))
			{
				s.Append(",Inverted");
			}
			if (_attr.Has(GAttrFlags.Hidden))
			{
				s.Append(",Hidden");
			}

			s.Append('}');

			return s.ToString();
		}
	}

}
