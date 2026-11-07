// @under-test: App.v4/Properties/Icons.resx, Engine/JocysCom/Controls/Themes/ThemeResourceManager.cs, Engine/JocysCom/Controls/Themes/ThemeResourceManager.resx, scripts/art/build_icons.py, scripts/art/build_controller.py, scripts/art/build_buttons.py, App.v4/Controls/AxisToButtonUserControl.cs
// @area: ui   @layer: unit
using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.Themes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using x360ce.App.Controls;

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
					var light = App.Properties.Resources.ok_32x32;
					FormsTheme.SetTheme(ThemeType.Dark);
					var dark = App.Properties.Resources.ok_32x32;
					Assert.AreNotSame(light, dark, "The dark theme handed out the light icon.");
					var folder = Path.Combine(Ui.RepoRoot.FullName, "Resources", "Images", "v4", "icons");
					AssertSamePixels(Path.Combine(folder, "ok_32x32.png"), ControlsHelper.GetOriginal(light));
					AssertSamePixels(Path.Combine(folder, "dark", "ok_32x32.png"), ControlsHelper.GetOriginal(dark));
					var swapped = manager.Themed(light);
					Assert.AreEqual(light.Size, swapped.Size);
					Assert.AreSame(ControlsHelper.GetOriginal(dark), ControlsHelper.GetOriginal(swapped), "An icon handed out in the light theme was not swapped for the dark one.");
				}
				finally
				{
					FormsTheme.SetTheme(ThemeType.Light);
					field.SetValue(null, before);
				}
			});
		}

		/// <summary>The pictures the designer knows from an xbox folder, by name, with the file of each.</summary>
		/// <remarks>The Buttons page's: a picture for each button, and the arrow beside it.</remarks>
		static Dictionary<string, string> DesignerGlyphs()
		{
			var resx = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Properties", "Resources.resx"));
			var images = Path.Combine(Ui.RepoRoot.FullName, "Resources", "Images");
			var glyphs = new Dictionary<string, string>();
			foreach (Match m in Regex.Matches(resx, @"<data name=""(\w+)"" type=""System\.Resources\.ResXFileRef[^>]*>\s*<value>[^;<]*\\Images\\(\w+)\\xbox\\(\w+\.png);"))
				glyphs.Add(m.Groups[1].Value, Path.Combine(images, m.Groups[2].Value, "xbox", m.Groups[3].Value));
			return glyphs;
		}

		[TestMethod, TestCategory("ui")]
		[Description("Every Buttons page picture is one file at twice the size it shows at 100%, and a dark one of the same size or none")]
		public void Every_glyph_is_one_picture_at_twice_its_size()
		{
			var versions = new ResourceManager("x360ce.App.Properties.Icons", typeof(App.Properties.Resources).Assembly);
			var glyphs = DesignerGlyphs();
			Assert.AreEqual(15, glyphs.Count, "Resources.resx does not name a picture for each of the 14 buttons and the arrow.");
			var wrong = new List<string>();
			foreach (var glyph in glyphs)
			{
				Size size;
				using (var image = Image.FromFile(glyph.Value))
					size = image.Size;
				// A picture drawn in the file's own colours only is the same in both themes and has no dark one.
				var dark = versions.GetObject(glyph.Key + "_dark") as Image;
				if (size != new Size(48, 48) || (dark != null && dark.Size != size))
					wrong.Add(glyph.Key);
			}
			Assert.AreEqual(0, wrong.Count, "Of the wrong size: " + string.Join(", ", wrong));
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
					var folder = Path.Combine(Ui.RepoRoot.FullName, "Resources", "Images", "v4", "xbox");
					// The arrow is drawn in a colour of the theme.
					var arrow = App.Properties.Resources.Axis_Arrow;
					AssertSamePixels(Path.Combine(folder, "dark", "Axis_Arrow_48x48.png"), ControlsHelper.GetOriginal(arrow));
					// A button is drawn in its own colours, the same in both themes.
					var button = App.Properties.Resources.Button_A;
					AssertSamePixels(Path.Combine(folder, "Button_A_48x48.png"), ControlsHelper.GetOriginal(button));
				}
				finally
				{
					FormsTheme.SetTheme(ThemeType.Light);
					field.SetValue(null, before);
				}
			});
		}

		[TestMethod, TestCategory("ui")]
		[Description("Every row of the Buttons page shows its button's picture and the arrow from the program's resources, in both themes, enabled and greyed out, with versions drawn at 1.5 and 2 times their size")]
		public void Buttons_page_pictures_are_embedded_for_each_theme_and_scale()
		{
			Ui.OnUiThread(() =>
			{
				var field = typeof(App.Properties.Resources).GetField("resourceMan", BindingFlags.NonPublic | BindingFlags.Static);
				var before = field.GetValue(null);
				try
				{
					ThemeResourceManager.Install(typeof(App.Properties.Resources), "x360ce.App.Properties.Icons");
					var folder = Path.Combine(Ui.RepoRoot.FullName, "Resources", "Images", "v4", "xbox");
					// The page's rows, one for each button of the controller.
					var buttons = new[]
					{
						GamepadButtonFlags.A, GamepadButtonFlags.B, GamepadButtonFlags.X, GamepadButtonFlags.Y,
						GamepadButtonFlags.LeftShoulder, GamepadButtonFlags.RightShoulder, GamepadButtonFlags.Back, GamepadButtonFlags.Start,
						GamepadButtonFlags.LeftThumb, GamepadButtonFlags.RightThumb,
						GamepadButtonFlags.DPadUp, GamepadButtonFlags.DPadDown, GamepadButtonFlags.DPadLeft, GamepadButtonFlags.DPadRight,
					};
					foreach (var theme in new[] { ThemeType.Light, ThemeType.Dark })
					{
						FormsTheme.SetTheme(theme);
						foreach (var button in buttons)
						{
							using (var row = new AxisToButtonUserControl { GamepadButton = button })
							{
								var when = theme + " theme, " + button;
								var picture = (PictureBox)row.Controls.Find(nameof(AxisToButtonUserControl.ButtonImagePictureBox), true).Single();
								var arrow = (PictureBox)row.Controls.Find(nameof(AxisToButtonUserControl.ArrowPictureBox), true).Single();
								AssertPicture(Path.Combine(folder, "Button_" + button + "_48x48.png"), picture.BackgroundImage, when);
								AssertPicture(Path.Combine(folder, theme == ThemeType.Dark ? "dark" : "", "Axis_Arrow_48x48.png"), arrow.Image, when + ", arrow");
								var enabled = ControlsHelper.GetOriginal(picture.BackgroundImage);
								row.Enabled = false;
								var greyed = ControlsHelper.GetOriginal(picture.BackgroundImage);
								Assert.IsNotNull(greyed, when + ": no picture when greyed out.");
								Assert.AreNotSame(enabled, greyed, when + ": the picture is not greyed out with its row.");
							}
						}
					}
				}
				finally
				{
					FormsTheme.SetTheme(ThemeType.Light);
					field.SetValue(null, before);
				}
			});
		}

		/// <summary>The picture shown is the file's.</summary>
		static void AssertPicture(string file, Image shown, string when)
		{
			Assert.IsNotNull(shown, when + ": no picture.");
			var original = ControlsHelper.GetOriginal(shown);
			AssertSamePixels(file, original);
		}

		[TestMethod, TestCategory("ui")]
		[Description("Every icon of the library's own controls is one picture at twice the size it shows at 100%, in both themes")]
		public void Every_library_icon_is_one_picture_in_both_themes()
		{
			var type = typeof(ThemeResourceManager);
			var set = new ResourceManager(type.FullName, type.Assembly)
				.GetResourceSet(System.Globalization.CultureInfo.InvariantCulture, true, true);
			var names = set.Cast<System.Collections.DictionaryEntry>().Select(x => (string)x.Key).Where(x => !x.EndsWith("_dark")).ToArray();
			Assert.IsTrue(names.Length >= 15, "Only " + names.Length + " library icons were found.");
			var wrong = new List<string>();
			foreach (var name in names)
			{
				var side = name.StartsWith("MessageBoxIcon_") ? 64 : 32;
				var size = new Size(side, side);
				var light = set.GetObject(name) as Image;
				var dark = set.GetObject(name + "_dark") as Image;
				if (light == null || dark == null || light.Size != size || dark.Size != size || !name.EndsWith("_" + side + "x" + side))
					wrong.Add(name);
			}
			Assert.AreEqual(0, wrong.Count, "Missing or of the wrong size: " + string.Join(", ", wrong));
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
					var light = (Image)icons.GetObject("refresh_32x32");
					FormsTheme.SetTheme(ThemeType.Dark);
					var dark = (Image)icons.GetObject("refresh_32x32");
					Assert.AreNotSame(light, dark, "The dark theme handed out the light icon.");
					var folder = Path.Combine(Ui.RepoRoot.FullName, "Engine", "JocysCom", "Controls", "Themes", "Images");
					AssertSamePixels(Path.Combine(folder, "refresh_32x32.png"), ControlsHelper.GetOriginal(light));
					AssertSamePixels(Path.Combine(folder, "dark", "refresh_32x32.png"), ControlsHelper.GetOriginal(dark));
					var swapped = icons.Themed(light);
					Assert.AreSame(ControlsHelper.GetOriginal(dark), ControlsHelper.GetOriginal(swapped), "An icon handed out in the light theme was not swapped for the dark one.");
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
