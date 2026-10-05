// @under-test: App.v4/Properties/Icons.resx, Engine/JocysCom/Controls/Themes/ThemeResourceManager.cs, scripts/art/build_icons.py
// @area: ui   @layer: unit
using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.Themes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
	/// <remarks>The icons are written by scripts\art\build_icons.py, which lists them in Properties\Icons.resx.</remarks>
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
