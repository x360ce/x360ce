// @under-test: App.v4/Properties/Icons.resx, Engine/JocysCom/Controls/Themes/ThemeResourceManager.cs, Engine/JocysCom/Controls/Themes/ThemeResourceManager.resx, scripts/art/build_icons.py, scripts/art/build_controller.py
// @area: ui   @layer: unit
using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.Themes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;

namespace x360ce.Tests
{
	/// <summary>
	/// The interface icons: drawn at 1.5 and 2 times their size for screens set above 100%, with a version
	/// for the dark theme at each size, and handed out in the theme in use.
	/// </summary>
	/// <remarks>
	/// The icons are written by scripts\art\build_icons.py, which lists the program's in Properties\Icons.resx and
	/// those of the library's own controls in Controls\Themes\ThemeResourceManager.resx of the library.
	/// </remarks>
	[TestClass]
	public class IconTest
	{
		/// <summary>The icons the designer knows from the icons folders, by name, with the size each is asked for at.</summary>
		static Dictionary<string, int> DesignerIcons()
		{
			var resx = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Properties", "Resources.resx"));
			var icons = new Dictionary<string, int>();
			foreach (Match m in Regex.Matches(resx, @"<data name=""(\w+)_(\d+)x\2"" type=""System\.Resources\.ResXFileRef[^>]*>\s*<value>[^;<]*\\icons\\[^;<]+;"))
				icons.Add(m.Groups[1].Value, int.Parse(m.Groups[2].Value));
			// A blank picture, the same in both themes and at every size.
			icons.Remove("empty");
			return icons;
		}

		[TestMethod, TestCategory("ui")]
		[Description("Every interface icon comes at 1.5 and 2 times its size, and for the dark theme at all three")]
		public void Every_icon_has_its_sizes_and_a_dark_version()
		{
			var versions = new ResourceManager("x360ce.App.Properties.Icons", typeof(App.Properties.Resources).Assembly);
			var icons = DesignerIcons();
			Assert.IsTrue(icons.Count > 30, "Only " + icons.Count + " icons were found in Resources.resx.");
			var missing = new List<string>();
			foreach (var icon in icons)
			{
				var sizes = new[] { icon.Value, icon.Value * 3 / 2, icon.Value * 2 };
				foreach (var size in sizes)
				{
					foreach (var suffix in new[] { "", "_dark" })
					{
						if (size == icon.Value && suffix == "")
							continue;
						var name = icon.Key + "_" + size + "x" + size + suffix;
						var image = versions.GetObject(name) as Image;
						if (image == null || image.Size != new Size(size, size))
							missing.Add(name);
					}
				}
			}
			Assert.AreEqual(0, missing.Count, "Missing or of the wrong size: " + string.Join(", ", missing));
		}

		[TestMethod, TestCategory("ui")]
		[Description("In the dark theme the resources hand out the dark icon, and an icon handed out before is swapped for it")]
		public void Dark_theme_hands_out_the_dark_icons()
		{
			Ui.OnUiThread(() =>
			{
				var field = typeof(App.Properties.Resources).GetField("resourceMan", BindingFlags.NonPublic | BindingFlags.Static);
				var before = field.GetValue(null);
				try
				{
					var manager = ThemeResourceManager.Install(typeof(App.Properties.Resources), "x360ce.App.Properties.Icons");
					FormsTheme.SetTheme(ThemeType.Light);
					var light = App.Properties.Resources.ok_16x16;
					FormsTheme.SetTheme(ThemeType.Dark);
					var dark = App.Properties.Resources.ok_16x16;
					Assert.AreNotSame(light, dark, "The dark theme handed out the light icon.");
					var folder = Path.Combine(Ui.RepoRoot.FullName, "Resources", "Images", "v4", "icons");
					AssertSamePixels(Path.Combine(folder, "ok_16x16.png"), ControlsHelper.GetOriginal(light));
					AssertSamePixels(Path.Combine(folder, "dark", "ok_16x16.png"), ControlsHelper.GetOriginal(dark));
					var swapped = manager.Themed(light);
					Assert.AreEqual(light.Size, swapped.Size);
					Assert.AreSame(ControlsHelper.GetOriginal(dark), ControlsHelper.GetOriginal(swapped), "An icon handed out in the light theme was not swapped for the dark one.");
					Assert.AreEqual(2, ControlsHelper.GetDrawnSizes(dark).Length, "The dark icon does not carry its versions drawn at 24 and 32 pixels.");
				}
				finally
				{
					FormsTheme.SetTheme(ThemeType.Light);
					field.SetValue(null, before);
				}
			});
		}

		/// <summary>The controller glyphs the designer knows, by name, with the size of each.</summary>
		static Dictionary<string, Size> DesignerGlyphs()
		{
			var resx = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Properties", "Resources.resx"));
			var folder = Path.Combine(Ui.RepoRoot.FullName, "Resources", "Images", "shared", "xbox");
			var glyphs = new Dictionary<string, Size>();
			foreach (Match m in Regex.Matches(resx, @"<data name=""(\w+)"" type=""System\.Resources\.ResXFileRef[^>]*>\s*<value>[^;<]*\\xbox\\(\w+\.png);"))
				using (var image = Image.FromFile(Path.Combine(folder, m.Groups[2].Value)))
					glyphs.Add(m.Groups[1].Value, image.Size);
			return glyphs;
		}

		[TestMethod, TestCategory("ui")]
		[Description("Every controller glyph comes at 1.5 and 2 times its size, and for the dark theme at all three")]
		public void Every_glyph_has_its_sizes_and_a_dark_version()
		{
			var versions = new ResourceManager("x360ce.App.Properties.Icons", typeof(App.Properties.Resources).Assembly);
			var glyphs = DesignerGlyphs();
			Assert.AreEqual(14, glyphs.Count, "Resources.resx does not name a glyph for each of the 14 buttons.");
			var missing = new List<string>();
			foreach (var glyph in glyphs)
			{
				foreach (var m in new[] { 1.0, 1.5, 2.0 })
				{
					var size = new Size((int)Math.Round(glyph.Value.Width * m), (int)Math.Round(glyph.Value.Height * m));
					foreach (var suffix in new[] { "", "_dark" })
					{
						if (m == 1.0 && suffix == "")
							continue;
						var name = glyph.Key + (m == 1.0 ? "" : "_" + size.Width + "x" + size.Height) + suffix;
						var image = versions.GetObject(name) as Image;
						if (image == null || image.Size != size)
							missing.Add(name);
					}
				}
			}
			Assert.AreEqual(0, missing.Count, "Missing or of the wrong size: " + string.Join(", ", missing));
		}

		[TestMethod, TestCategory("ui")]
		[Description("A picture whose name carries no size is handed out in the theme in use, with its versions drawn at larger sizes")]
		public void Dark_theme_hands_out_the_dark_glyphs()
		{
			Ui.OnUiThread(() =>
			{
				var field = typeof(App.Properties.Resources).GetField("resourceMan", BindingFlags.NonPublic | BindingFlags.Static);
				var before = field.GetValue(null);
				try
				{
					ThemeResourceManager.Install(typeof(App.Properties.Resources), "x360ce.App.Properties.Icons");
					FormsTheme.SetTheme(ThemeType.Dark);
					var dark = App.Properties.Resources.Button_A;
					var folder = Path.Combine(Ui.RepoRoot.FullName, "Resources", "Images", "shared", "xbox");
					AssertSamePixels(Path.Combine(folder, "dark", "Button_A.png"), ControlsHelper.GetOriginal(dark));
					Assert.AreEqual(2, ControlsHelper.GetDrawnSizes(dark).Length, "The dark glyph does not carry its versions drawn at 30 and 40 pixels.");
				}
				finally
				{
					FormsTheme.SetTheme(ThemeType.Light);
					field.SetValue(null, before);
				}
			});
		}

		[TestMethod, TestCategory("ui")]
		[Description("Every icon of the library's own controls comes at 1.5 and 2 times its size, and each in both themes")]
		public void Every_library_icon_has_its_sizes_and_a_dark_version()
		{
			var type = typeof(ThemeResourceManager);
			var set = new ResourceManager(type.FullName, type.Assembly)
				.GetResourceSet(System.Globalization.CultureInfo.InvariantCulture, true, true);
			var names = new HashSet<string>(set.Cast<System.Collections.DictionaryEntry>().Select(x => (string)x.Key));
			var icons = names.Where(x => !x.EndsWith("_dark"))
				.Select(x => Regex.Match(x, @"^(\w+)_(\d+)x\2$"))
				.GroupBy(x => x.Groups[1].Value, x => int.Parse(x.Groups[2].Value))
				.ToDictionary(x => x.Key, x => x.Min());
			Assert.IsTrue(icons.Count >= 15, "Only " + icons.Count + " library icons were found.");
			var missing = new List<string>();
			foreach (var icon in icons)
			{
				foreach (var size in new[] { icon.Value, icon.Value * 3 / 2, icon.Value * 2 })
				{
					foreach (var suffix in new[] { "", "_dark" })
					{
						var name = icon.Key + "_" + size + "x" + size + suffix;
						var image = set.GetObject(name) as Image;
						if (image == null || image.Size != new Size(size, size))
							missing.Add(name);
					}
				}
			}
			Assert.AreEqual(0, missing.Count, "Missing or of the wrong size: " + string.Join(", ", missing));
		}

		[TestMethod, TestCategory("ui")]
		[Description("In the dark theme the library hands out the dark icon of its controls, and one handed out before is swapped for it")]
		public void Library_hands_out_the_dark_icons_in_the_dark_theme()
		{
			Ui.OnUiThread(() =>
			{
				try
				{
					var icons = ThemeResourceManager.Library;
					FormsTheme.SetTheme(ThemeType.Light);
					var light = (Image)icons.GetObject("refresh_16x16");
					FormsTheme.SetTheme(ThemeType.Dark);
					var dark = (Image)icons.GetObject("refresh_16x16");
					Assert.AreNotSame(light, dark, "The dark theme handed out the light icon.");
					var folder = Path.Combine(Ui.RepoRoot.FullName, "Engine", "JocysCom", "Controls", "Themes", "Images");
					AssertSamePixels(Path.Combine(folder, "refresh_16x16.png"), ControlsHelper.GetOriginal(light));
					AssertSamePixels(Path.Combine(folder, "dark", "refresh_16x16.png"), ControlsHelper.GetOriginal(dark));
					var swapped = icons.Themed(light);
					Assert.AreSame(ControlsHelper.GetOriginal(dark), ControlsHelper.GetOriginal(swapped), "An icon handed out in the light theme was not swapped for the dark one.");
					Assert.AreEqual(2, ControlsHelper.GetDrawnSizes(dark).Length, "The dark icon does not carry its versions drawn at 24 and 32 pixels.");
				}
				finally
				{
					FormsTheme.SetTheme(ThemeType.Light);
				}
			});
		}

		static void AssertSamePixels(string file, Image image)
		{
			using (var expected = new Bitmap(file))
			{
				var actual = (Bitmap)image;
				Assert.AreEqual(expected.Size, actual.Size, file);
				for (var y = 0; y < expected.Height; y++)
					for (var x = 0; x < expected.Width; x++)
						Assert.AreEqual(expected.GetPixel(x, y).ToArgb(), actual.GetPixel(x, y).ToArgb(), file + " differs at " + x + "," + y);
			}
		}
	}
}
