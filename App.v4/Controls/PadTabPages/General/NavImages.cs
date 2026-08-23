using System;
using System.Collections.Generic;
using System.Drawing;
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
	/// 130 physical pixels. Scaled copies are cached per size so a repaint costs a blit.
	/// </remarks>
	public static class NavImages
	{

		static readonly object LoadLock = new object();
		static readonly Dictionary<string, Bitmap> Masters = new Dictionary<string, Bitmap>();
		static readonly Dictionary<string, Bitmap> Scaled = new Dictionary<string, Bitmap>();

		/// <summary>Every glyph name that ships, in the order they read on screen.</summary>
		public static readonly string[] Names =
		{
			"NavNormal", "NavActive", "NavRecord",
			"NavUpNormal", "NavUpActive", "NavUpRecord",
			"NavDownNormal", "NavDownActive", "NavDownRecord",
			"NavLeftNormal", "NavLeftActive", "NavLeftRecord",
			"NavRightNormal", "NavRightActive", "NavRightRecord",
			"NavAxisActive", "NavTriggerActive",
		};

		/// <summary>
		/// Resource name for a mapping in a given state, for example NavLeftRecord.
		/// </summary>
		/// <remarks>
		/// A code ending in a direction takes the matching arrow. Triggers point up, because
		/// they are drawn as a rising bar. Everything else takes the plain round glyph.
		/// </remarks>
		public static string GetName(MapCode code, NavImageType type)
		{
			return string.Format("Nav{0}{1}", GetDirection(code), type);
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

		/// <summary>Full size glyph, or null when the name does not ship.</summary>
		public static Bitmap GetMaster(string name)
		{
			if (string.IsNullOrEmpty(name))
				return null;
			lock (LoadLock)
			{
				Bitmap master;
				if (Masters.TryGetValue(name, out master))
					return master;
				// A missing glyph must not take the interface down: the caller draws nothing.
				var stream = EngineHelper.GetResourceStream("Nav." + name + ".png");
				master = stream == null ? null : new Bitmap(stream);
				Masters.Add(name, master);
				return master;
			}
		}

		/// <summary>
		/// Glyph scaled to a square of the given size, cached for reuse.
		/// </summary>
		public static Bitmap Get(string name, int size)
		{
			if (size < 1)
				return null;
			var master = GetMaster(name);
			if (master == null)
				return null;
			var key = name + ":" + size;
			lock (LoadLock)
			{
				Bitmap scaled;
				if (Scaled.TryGetValue(key, out scaled))
					return scaled;
				scaled = new Bitmap(size, size);
				using (var g = Graphics.FromImage(scaled))
				{
					g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
					g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
					g.DrawImage(master, new Rectangle(0, 0, size, size));
				}
				Scaled.Add(key, scaled);
				return scaled;
			}
		}

		public static Bitmap Get(MapCode code, NavImageType type, int size)
		{
			return Get(GetName(code, type), size);
		}

	}
}
