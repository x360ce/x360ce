using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text.RegularExpressions;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	/// <summary>
	/// The navigation glyphs drawn over the controller picture.
	/// </summary>
	/// <remarks>
	/// Each glyph ships as a single 512 pixel master, which is the size the artwork was drawn
	/// at. They are always scaled down, never up: the controller picture sits in a layout that
	/// grows with the window, and on a 4K display an 18 pixel button already renders at around
	/// 130 physical pixels. Scaled copies are cached per size and colour so a repaint costs a blit.
	/// </remarks>
	public static class NavImages
	{

		static readonly object LoadLock = new object();
		static readonly Dictionary<string, Bitmap> Scaled = new Dictionary<string, Bitmap>();

		/// <summary>Every glyph name that ships, in the order they read on screen: the marks, blue and white, and the two indicators.</summary>
		public static readonly string[] Names =
		{
			"NavNormal", "NavUpNormal", "NavDownNormal", "NavLeftNormal", "NavRightNormal",
			"NavAxisActive", "NavTriggerActive",
		};

		/// <summary>
		/// Resource name of a mapping's mark, for example NavLeftNormal.
		/// </summary>
		/// <remarks>
		/// A code ending in a direction takes the matching arrow. Triggers point up, because
		/// they are drawn as a rising bar. Everything else takes the plain round glyph.
		/// </remarks>
		public static string GetName(MapCode code)
		{
			return string.Format("Nav{0}Normal", GetDirection(code));
		}

		public static string GetDirection(MapCode code)
		{
			if (code == MapCode.LeftTrigger || code == MapCode.RightTrigger)
				return "Up";
			var match = Regex.Match(code.ToString(), "(Up|Left|Right|Down)$");
			return match.Success ? match.Value : "";
		}

		/// <summary>
		/// The mapping a glyph is actually drawn for.
		/// </summary>
		/// <remarks>
		/// An axis and its positive direction share one glyph, because they mark the same place on
		/// the controller. Without this they would draw on top of each other and read twice as dark.
		/// </remarks>
		public static MapCode GetNameCode(MapCode code)
		{
			if (code == MapCode.LeftThumbAxisX)
				return MapCode.LeftThumbRight;
			if (code == MapCode.LeftThumbAxisY)
				return MapCode.LeftThumbUp;
			if (code == MapCode.RightThumbAxisX)
				return MapCode.RightThumbRight;
			if (code == MapCode.RightThumbAxisY)
				return MapCode.RightThumbUp;
			return code;
		}

		/// <summary>True when a glyph of this name ships.</summary>
		/// <remarks>Answered from the list rather than by decoding the image.</remarks>
		public static bool Exists(string name)
		{
			return !string.IsNullOrEmpty(name) && Names.Contains(name);
		}

		/// <summary>
		/// Glyph scaled to a square of the given size, cached for reuse.
		/// </summary>
		/// <remarks>
		/// The 512 pixel master is decoded, scaled and released again. Keeping all seven
		/// resident would hold about 7 MB of image memory to draw squares of roughly 18 pixels;
		/// the scaled copies cost a few kilobytes together. A size or a colour that has not been
		/// seen before decodes once more, which happens only when the window, the display scaling
		/// or the theme changes.
		/// </remarks>
		public static Bitmap Get(string name, int size)
		{
			return Get(name, size, Color.Empty);
		}

		/// <summary>A mapping's mark in a colour, scaled to a square of the given size, cached for reuse.</summary>
		public static Bitmap Get(MapCode code, Color color, int size)
		{
			return Get(GetName(code), size, color);
		}

		/// <summary>A glyph scaled to a square of the given size, its blue made the colour given unless that is empty.</summary>
		static Bitmap Get(string name, int size, Color color)
		{
			if (size < 1 || !Exists(name))
				return null;
			var key = name + ":" + size + ":" + color.ToArgb();
			lock (LoadLock)
			{
				Bitmap scaled;
				if (Scaled.TryGetValue(key, out scaled))
					return scaled;
				var stream = EngineHelper.GetResourceStream("Nav." + name + ".png");
				if (stream == null)
					return null;
				using (var master = new Bitmap(stream))
				using (var attributes = new ImageAttributes())
				{
					if (!color.IsEmpty)
						attributes.SetColorMatrix(Tint(color));
					scaled = new Bitmap(size, size);
					using (var g = Graphics.FromImage(scaled))
					{
						g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
						g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
						g.DrawImage(master, new Rectangle(0, 0, size, size), 0, 0, master.Width, master.Height, GraphicsUnit.Pixel, attributes);
					}
				}
				Scaled.Add(key, scaled);
				return scaled;
			}
		}

		/// <summary>The blue the marks are drawn in, with white arrows, in their masters.</summary>
		static readonly Color MasterBlue = Color.FromArgb(0x12, 0x9E, 0xEE);

		/// <summary>Makes a master's blue the colour given and leaves its white white.</summary>
		/// <remarks>
		/// Each channel is set from the master's red, which runs from <see cref="MasterBlue"/>'s red in the mark to full in its
		/// arrow, so a pixel between the two is as far between the new colour and white. One master a direction then gives a
		/// mark in every state's colour, and the colours are the theme's.
		/// </remarks>
		static ColorMatrix Tint(Color color)
		{
			var range = (255f - MasterBlue.R) / 255f;
			var r = (255 - color.R) / 255f / range;
			var g = (255 - color.G) / 255f / range;
			var b = (255 - color.B) / 255f / range;
			// Each channel is 1 - k (1 - red): k times the red, and 1 - k added.
			return new ColorMatrix(new[]
			{
				new[] { r, g, b, 0f, 0f },
				new[] { 0f, 0f, 0f, 0f, 0f },
				new[] { 0f, 0f, 0f, 0f, 0f },
				new[] { 0f, 0f, 0f, 1f, 0f },
				new[] { 1 - r, 1 - g, 1 - b, 0f, 1f },
			});
		}

	}
}
