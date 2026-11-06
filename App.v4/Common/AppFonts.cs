using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.Themes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using x360ce.Engine;

namespace x360ce.App
{
	/// <summary>
	/// The font and size the windows are drawn in, and the two fonts the program carries: Selawik, an open font made to the
	/// measures of Segoe UI, for the interface where Windows has no Segoe UI or where it is chosen, and Inconsolata Medium for
	/// code and values whose letters line up. Both are read from the program itself, so a computer without them shows them
	/// too. The theme draws the windows in the font and size chosen on the Options page (<see cref="Apply"/>,
	/// <c>FormsTheme.SetFonts</c>): Segoe UI at Windows' text size unless another is chosen. A font that cannot be read leaves
	/// the one the program used before it.
	/// </summary>
	/// <remarks>
	/// A font added by a program is known to that program only, and twice: to GDI, which Windows' own controls and
	/// <c>TextRenderer</c> draw with and which finds a face by its name, and to GDI+, where a <see cref="Font"/> is made from
	/// a <see cref="FontFamily"/> of <see cref="Collection"/>, never from the name. Each is given the font as a file in the
	/// settings folder's Temp folder, written there once from the program: GDI+ given the same font from memory left
	/// Inconsolata Medium out, without an error, in about one load in four. The files and their SIL Open Font Licence are
	/// kept in Resources\Fonts.
	/// </remarks>
	public static class AppFonts
	{
		/// <summary>The family the interface is drawn in by default, where Windows draws it.</summary>
		/// <remarks>Settable, so the program can be tried as on a computer without it.</remarks>
		public static string DefaultName { get; set; } = "Segoe UI";

		/// <summary>The interface font the program carries, which is the default where Windows does not draw <see cref="DefaultName"/>.</summary>
		public const string BuiltInName = "Selawik";

		/// <summary>The code font's family name: Inconsolata's Medium weight is a family of its own to GDI.</summary>
		public const string CodeName = "Inconsolata Medium";

		/// <summary>Selawik's files, regular and bold, as the program carries them under Resources\Fonts.</summary>
		static readonly string[] BuiltInFiles = { "selawk.ttf", "selawkb.ttf" };

		/// <summary>Inconsolata Medium's file, as the program carries it under Resources\Fonts.</summary>
		static readonly string[] CodeFiles = { "Inconsolata-Medium.ttf" };

		/// <summary>The fonts for GDI+, kept for the life of the process.</summary>
		static readonly PrivateFontCollection Collection = new PrivateFontCollection();

		/// <summary>Selawik, read from the program the first time it is asked for; null when it could not be read.</summary>
		/// <remarks>Read only by a run that draws in it: chosen, or the default where Windows has no Segoe UI.</remarks>
		public static FontFamily BuiltInFamily
		{
			get
			{
				if (!_builtInRead)
				{
					_builtInRead = true;
					_builtInFamily = AddFonts(BuiltInFiles, BuiltInName);
				}
				return _builtInFamily;
			}
		}
		static bool _builtInRead;
		static FontFamily _builtInFamily;

		/// <summary>Inconsolata Medium, or null when it could not be read.</summary>
		public static FontFamily CodeFamily { get; private set; }

		/// <summary>Known to this process only, and gone with it.</summary>
		const uint FR_PRIVATE = 0x10;

		[DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
		static extern int AddFontResourceEx(string name, uint fl, IntPtr res);

		static bool _loaded;

		/// <summary>Makes the code font the program carries known to GDI and GDI+, once. Interface thread, before the first window.</summary>
		public static void Load()
		{
			if (_loaded)
				return;
			_loaded = true;
			CodeFamily = AddFonts(CodeFiles, CodeName);
		}

		/// <summary>Makes font files the program carries known to GDI and GDI+, and gives the family of a name among them; null when it could not be read.</summary>
		static FontFamily AddFonts(string[] files, string name)
		{
			foreach (var file in files)
			{
				FileInfo fi;
				if (EngineHelper.ExtractFile(file, out fi) != null)
					continue;
				// A face GDI does not know would be drawn by Windows' controls in another, so GDI+ is not given it either.
				if (AddFontResourceEx(fi.FullName, FR_PRIVATE, IntPtr.Zero) == 0)
					continue;
				// GDI+ refusing a file leaves its family out below, and Windows' fonts in use.
				try
				{
					Collection.AddFontFile(fi.FullName);
				}
				catch (ExternalException)
				{
				}
				catch (FileNotFoundException)
				{
				}
			}
			return Collection.Families.FirstOrDefault(x => x.Name == name);
		}

		#region The font chosen in the options

		/// <summary>
		/// What <see cref="Options.InterfaceFont"/> holds for Windows' own fonts. Empty holds the default, <see cref="DefaultName"/>,
		/// and <see cref="BuiltInName"/> the built-in Selawik.
		/// </summary>
		public const string WindowsDefault = "Windows default";

		/// <summary>The choice this run's windows are drawn in, as <see cref="Options.InterfaceFont"/> held it when they were made.</summary>
		public static string Applied { get; private set; } = "";

		/// <summary>The size this run's windows are drawn at, as <see cref="Options.InterfaceFontSize"/> held it when they were made.</summary>
		public static float AppliedSize { get; private set; }

		/// <summary>
		/// Draws the windows made from now on in the font and at the size chosen in the options, and code in Inconsolata Medium.
		/// At a size other than the default, windows are laid out larger or smaller by as much. Interface thread, after
		/// <see cref="Load"/> and before the first window: a window keeps the font it was made in.
		/// </summary>
		public static void Apply(string choice, float size)
		{
			Applied = choice ?? "";
			AppliedSize = size;
			var interfaceSize = SizeFor(choice, size);
			FormsTheme.SetFonts(InterfaceFamilyFor(choice), CodeFamily, interfaceSize, interfaceSize / DefaultSize(choice));
		}

		/// <summary>
		/// The family the interface is drawn in for a choice in <see cref="Options.InterfaceFont"/>: the default for none, the
		/// built-in Selawik for Selawik, null for Windows' own fonts, otherwise the installed family of that name. A family no
		/// longer installed, or one that cannot draw the interface's letters, gives the default; its name stays in the options,
		/// so it is used again once it is back. The default is Segoe UI, or Selawik where Windows does not draw Segoe UI.
		/// </summary>
		public static FontFamily InterfaceFamilyFor(string choice)
		{
			if (choice == WindowsDefault)
				return null;
			if (choice == BuiltInName)
				return BuiltInFamily;
			var family = string.IsNullOrEmpty(choice) ? null : Installed(choice);
			if (family != null)
				return family;
			return DrawsDefault ? new FontFamily(DefaultName) : BuiltInFamily;
		}

		/// <summary>Whether Windows draws the interface in <see cref="DefaultName"/>, which is then the default in place of Selawik.</summary>
		public static bool DrawsDefault
		{
			get
			{
				var name = DefaultName;
				if (_checkedName != name)
				{
					using (var family = Installed(name))
						_drawsDefault = family != null;
					_checkedName = name;
				}
				return _drawsDefault;
			}
		}
		static string _checkedName;
		static bool _drawsDefault;

		/// <summary>The installed family of a name, when it draws the interface's letters (<see cref="DrawsText"/>); null otherwise.</summary>
		static FontFamily Installed(string name)
		{
			FontFamily family;
			try
			{
				family = new FontFamily(name);
			}
			catch (ArgumentException)
			{
				// GDI+ has no family of that name.
				return null;
			}
			if (DrawsText(family))
				return family;
			family.Dispose();
			return null;
		}

		/// <summary>
		/// The installed families the interface can be drawn in, by name, in order: each with a regular and a bold style that
		/// draws the interface's letters. Selawik is left out, as the program carries it.
		/// </summary>
		/// <remarks>
		/// Reads every installed font: about a fifth of a second for two hundred families, so the Options page asks for it
		/// off the interface thread.
		/// </remarks>
		public static List<string> InstalledFamilies()
		{
			var names = new List<string>();
			using (var collection = new InstalledFontCollection())
				foreach (var family in collection.Families)
					using (family)
						if (family.Name != BuiltInName && DrawsText(family))
							names.Add(family.Name);
			names.Sort(StringComparer.OrdinalIgnoreCase);
			return names;
		}

		/// <summary>
		/// The choices the Options page lists, as <see cref="Options.InterfaceFont"/> holds them: the default, Selawik, Windows'
		/// own fonts, then the installed families in order, with a saved family that is not among them in its place, so the
		/// page shows what is saved. The default's family is not listed again, unless it is the one saved.
		/// </summary>
		/// <param name="saved">The choice the options hold.</param>
		/// <param name="installed">The installed families, as <see cref="InstalledFamilies"/> gives them; null while they are read.</param>
		public static List<string> Choices(string saved, IEnumerable<string> installed)
		{
			var drawsDefault = DrawsDefault;
			var names = new List<string>(installed ?? Enumerable.Empty<string>());
			if (drawsDefault)
				names.Remove(DefaultName);
			if (!string.IsNullOrEmpty(saved) && saved != WindowsDefault && saved != BuiltInName && !names.Contains(saved))
				names.Add(saved);
			names.Sort(StringComparer.OrdinalIgnoreCase);
			var first = new List<string> { "" };
			if (drawsDefault || saved == BuiltInName)
				first.Add(BuiltInName);
			first.Add(WindowsDefault);
			names.InsertRange(0, first);
			return names;
		}

		/// <summary>A choice as the Options page shows it; a family that is not installed says so once the installed ones are known.</summary>
		/// <param name="choice">A choice as <see cref="Options.InterfaceFont"/> holds it.</param>
		/// <param name="installed">The installed families; null while they are read.</param>
		public static string ChoiceText(string choice, ICollection<string> installed)
		{
			if (string.IsNullOrEmpty(choice))
				return DrawsDefault ? DefaultName + " (default)" : BuiltInName + " (default, built in)";
			if (choice == BuiltInName)
				return BuiltInName + " (built in)";
			if (choice == WindowsDefault || installed == null || installed.Contains(choice))
				return choice;
			return choice + " (not installed)";
		}

		/// <summary>
		/// The size the interface is drawn at by default, in points: Windows' text size, larger where Windows' text is made
		/// larger; for Windows' own fonts, Windows Forms' own size, so that choice keeps its look.
		/// </summary>
		public static float DefaultSize(string choice)
		{
			using (var font = choice == WindowsDefault ? SystemFonts.DefaultFont : SystemFonts.MessageBoxFont)
				return font.SizeInPoints;
		}

		/// <summary>
		/// The size the interface is drawn at, in points, for a choice in <see cref="Options.InterfaceFont"/> and a size in
		/// <see cref="Options.InterfaceFontSize"/>: the size set, whatever the font, or the font's default for 0.
		/// </summary>
		public static float SizeFor(string choice, float size)
		{
			return size > 0 ? size : DefaultSize(choice);
		}

		/// <summary>
		/// The sizes the Options page lists, as <see cref="Options.InterfaceFontSize"/> holds them: 0 for the default, then one
		/// for each whole pixel from three quarters of the default to half as large again, with a saved size
		/// that is not among them in its place.
		/// </summary>
		/// <remarks>
		/// Windows draws text at whole pixels, so a size between two of them looks as one of the two. The pixels are those of the
		/// scale windows are made at (<see cref="ControlsHelper.DpiScale"/>).
		/// </remarks>
		/// <param name="choice">The font chosen, whose default the sizes are around.</param>
		/// <param name="saved">The size the options hold.</param>
		public static List<float> Sizes(string choice, float saved)
		{
			var pixels = Pixels(DefaultSize(choice));
			var sizes = new List<float>();
			for (var px = (int)Math.Ceiling(pixels * 0.75); px <= (int)Math.Floor(pixels * 1.5); px++)
				sizes.Add(px * 72f / (96f * ControlsHelper.DpiScale));
			if (saved > 0)
			{
				var same = sizes.FindIndex(x => Math.Abs(x - saved) < 0.001f);
				if (same < 0)
					sizes.Add(saved);
				else
					sizes[same] = saved;
				sizes.Sort();
			}
			sizes.Insert(0, 0f);
			return sizes;
		}

		/// <summary>How many pixels high the letters of a size in points are drawn at the scale windows are made at.</summary>
		public static int Pixels(float sizeInPoints)
		{
			return (int)Math.Round(sizeInPoints * 96f * ControlsHelper.DpiScale / 72f, MidpointRounding.AwayFromZero);
		}

		/// <summary>A size as the Options page shows it: the default with its size, any other in points and in pixels.</summary>
		/// <param name="choice">The font chosen, whose default 0 stands for.</param>
		/// <param name="size">A size as <see cref="Options.InterfaceFontSize"/> holds it.</param>
		public static string SizeText(string choice, float size)
		{
			return size > 0
				? string.Format("{0:0.##} pt ({1} px)", size, Pixels(size))
				: string.Format("Default ({0:0.##} pt)", DefaultSize(choice));
		}

		/// <summary>What the Options page says while the font or size chosen is not the one in use: what x360ce uses when it starts again.</summary>
		public static string RestartNote(string choice, float size, ICollection<string> installed)
		{
			return string.Format("{0} at {1:0.##} pt is used when x360ce starts again.", ChoiceText(choice, installed), SizeFor(choice, size));
		}

		/// <summary>Letters and digits every family the interface is drawn in must draw itself.</summary>
		const string Letters = "AZaz09";

		const int FW_NORMAL = 400;
		const uint DEFAULT_CHARSET = 1;
		const int SYMBOL_CHARSET = 2;
		const uint GGI_MARK_NONEXISTING_GLYPHS = 1;
		const ushort MissingGlyph = 0xFFFF;

		/// <summary>
		/// Whether a family has a regular and a bold style, and Windows draws the interface's letters in it: a symbol font
		/// draws its symbols in their place, an icon font nothing, and a name Windows does not find is drawn in another face.
		/// </summary>
		static bool DrawsText(FontFamily family)
		{
			if (!family.IsStyleAvailable(FontStyle.Regular) || !family.IsStyleAvailable(FontStyle.Bold))
				return false;
			var dc = CreateCompatibleDC(IntPtr.Zero);
			var font = CreateFont(0, 0, 0, 0, FW_NORMAL, 0, 0, 0, DEFAULT_CHARSET, 0, 0, 0, 0, family.Name);
			var old = SelectObject(dc, font);
			try
			{
				// GDI+ gives a family at most 31 characters of its name, and GDI the whole name.
				var face = new StringBuilder(64);
				GetTextFace(dc, face.Capacity, face);
				var glyphs = new ushort[Letters.Length];
				return face.ToString().StartsWith(family.Name, StringComparison.OrdinalIgnoreCase)
					&& GetTextCharset(dc) != SYMBOL_CHARSET
					&& GetGlyphIndices(dc, Letters, Letters.Length, glyphs, GGI_MARK_NONEXISTING_GLYPHS) == Letters.Length
					&& !glyphs.Contains(MissingGlyph);
			}
			finally
			{
				SelectObject(dc, old);
				DeleteObject(font);
				DeleteDC(dc);
			}
		}

		[DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
		static extern IntPtr CreateFont(int height, int width, int escapement, int orientation, int weight, uint italic, uint underline,
			uint strikeOut, uint charSet, uint outPrecision, uint clipPrecision, uint quality, uint pitchAndFamily, string face);

		[DllImport("gdi32.dll")]
		static extern IntPtr CreateCompatibleDC(IntPtr hdc);

		[DllImport("gdi32.dll")]
		static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);

		[DllImport("gdi32.dll")]
		static extern bool DeleteObject(IntPtr obj);

		[DllImport("gdi32.dll")]
		static extern bool DeleteDC(IntPtr hdc);

		[DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
		static extern int GetTextFace(IntPtr hdc, int count, StringBuilder face);

		[DllImport("gdi32.dll")]
		static extern int GetTextCharset(IntPtr hdc);

		[DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
		static extern uint GetGlyphIndices(IntPtr hdc, string text, int count, [Out] ushort[] indices, uint flags);

		#endregion
	}
}
