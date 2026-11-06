// @under-test: App.v4/Common/AppFonts.cs, Engine/JocysCom/Controls/Themes/Default.Forms.cs, Resources/Fonts
// @area: interface   @layer: unit
using JocysCom.ClassLibrary.Controls.Themes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using x360ce.App;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// The interface is drawn in Segoe UI at Windows' text size, or in Selawik, which the program carries, where Windows has no
	/// Segoe UI; code in Inconsolata Medium, which it carries too. The theme draws the windows in them, and lays them out as
	/// in Windows' font at the default size and larger or smaller by as much at another.
	/// </summary>
	/// <remarks>
	/// The theme's fonts belong to the whole process, so the test that sets them gives Windows Forms' own back, even when
	/// an assertion fails, or the tests after it would run in the program's fonts.
	/// </remarks>
	[TestClass]
	public class AppFontsTest
	{
		[TestMethod, TestCategory("interface")]
		[Description("The program carries Selawik and Inconsolata Medium with their licences, and Windows finds both faces by name")]
		public void Fonts_are_carried_with_their_licences()
		{
			LoadFonts();
			Assert.AreEqual(AppFonts.BuiltInName, AppFonts.BuiltInFamily?.Name, "Selawik was not read from the program.");
			Assert.AreEqual(AppFonts.CodeName, AppFonts.CodeFamily?.Name, "Inconsolata Medium was not read from the program.");
			Assert.IsTrue(AppFonts.BuiltInFamily.IsStyleAvailable(FontStyle.Bold), "Selawik has no bold.");
			// Windows' own controls ask GDI for a face by name, and GDI gives another face when it does not know the one asked for.
			foreach (var family in new[] { AppFonts.BuiltInFamily, AppFonts.CodeFamily })
				using (var font = new Font(family, 9f))
					Assert.AreEqual(family.Name, GdiFace(font.ToHfont(), true), "GDI does not find " + family.Name + ".");
			var assembly = typeof(AppFonts).Assembly;
			foreach (var family in new[] { "Selawik", "Inconsolata" })
			{
				using (var stream = assembly.GetManifestResourceStream("x360ce.App.Resources.Fonts." + family + ".OFL.txt"))
				{
					Assert.IsNotNull(stream, family + "'s licence is not carried with it.");
					var text = new StreamReader(stream).ReadToEnd();
					StringAssert.Contains(text, "SIL OPEN FONT LICENSE Version 1.1", family + "'s licence");
					StringAssert.Contains(text, family, family + "'s licence names no copyright holder for it.");
				}
			}
		}

		[TestMethod, TestCategory("interface")]
		[Description("At the default size a window keeps its layout in the program's fonts, is drawn in them, also by Windows' own controls, a font a control names is drawn at the interface size, and a rich text box keeps its own")]
		public void Windows_keep_their_layout_in_the_programs_fonts()
		{
			LoadFonts();
			Ui.OnUiThread(() =>
			{
				FormsTheme.SetTheme(ThemeType.Light);
				var windows = Bounds(false);
				FormsTheme.SetFonts(AppFonts.InterfaceFamilyFor(""), AppFonts.CodeFamily, AppFonts.SizeFor("", 0));
				try
				{
					Assert.AreEqual(AppFonts.DefaultName, Control.DefaultFont.Name, "Controls without a font of their own are not drawn in Segoe UI.");
					using (var text = SystemFonts.MessageBoxFont)
						Assert.AreEqual(text.SizeInPoints, Control.DefaultFont.SizeInPoints, "Controls without a font of their own are not drawn at Windows' text size.");
					using (var menus = SystemFonts.MenuFont)
						Assert.AreEqual(menus.SizeInPoints, MenuFont().SizeInPoints, 0.01f, "The menus are not drawn at Windows' menu size.");
					var made = Bounds(false);
					CollectionAssert.AreEqual(windows, made, "A window made after the fonts changed is laid out otherwise: "
						+ string.Join(", ", made) + " instead of " + string.Join(", ", windows));
					// Themed after it is shown, as a dialog is when it first becomes active.
					var shown = Bounds(true);
					CollectionAssert.AreEqual(windows, shown, "A window themed once shown is laid out otherwise: "
						+ string.Join(", ", shown) + " instead of " + string.Join(", ", windows));
				}
				finally
				{
					FormsTheme.SetFonts(null, null);
					FormsTheme.SetTheme(ThemeType.Light);
				}
				Assert.AreNotEqual(AppFonts.DefaultName, Control.DefaultFont.Name, "Windows Forms' own font did not come back.");
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("At a size set larger than the default, in the program's font and in Windows' own, a window is laid out as in Windows' font made larger by as much, the menus are drawn larger than Windows' by as much, and every other font the theme replaces is drawn larger by as much as the interface font is larger than Windows Forms' own")]
		public void Windows_are_laid_out_larger_at_a_larger_size()
		{
			LoadFonts();
			Ui.OnUiThread(() =>
			{
				FormsTheme.SetTheme(ThemeType.Light);
				foreach (var choice in new[] { "", AppFonts.WindowsDefault })
				{
					var size = 12f;
					var ratio = size / AppFonts.DefaultSize(choice);
					var when = (choice == "" ? "the default font" : choice) + " at " + size + " pt: ";
					var windows = Control.DefaultFont;
					// Windows' font made larger by as much as the size is larger than the default.
					var expected = Bounds(false, new Font(windows.FontFamily, windows.SizeInPoints * ratio, windows.Style));
					FormsTheme.SetFonts(AppFonts.InterfaceFamilyFor(choice), AppFonts.CodeFamily, AppFonts.SizeFor(choice, size), ratio);
					try
					{
						Assert.AreEqual(size, Control.DefaultFont.SizeInPoints, when + "controls without a font of their own are not drawn at the size set.");
						var made = Bounds(false);
						CollectionAssert.AreEqual(expected, made, when + "a window is laid out " + string.Join(", ", made)
							+ " instead of as in Windows' font made larger: " + string.Join(", ", expected));
						var shown = Bounds(true);
						CollectionAssert.AreEqual(expected, shown, when + "a window themed once shown is laid out " + string.Join(", ", shown)
							+ " instead of " + string.Join(", ", expected));
						using (var menus = SystemFonts.MenuFont)
							Assert.AreEqual(menus.SizeInPoints * ratio, MenuFont().SizeInPoints, 0.01f,
								when + "the menus are not drawn larger than Windows' by as much as the size is larger than the default.");
					}
					finally
					{
						FormsTheme.SetFonts(null, null);
						FormsTheme.SetTheme(ThemeType.Light);
					}
				}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("The interface is drawn at Windows' text size by default, at Windows Forms' own size for Windows default, and at the size set, whatever the font chosen")]
		public void Size_is_windows_text_size_unless_one_is_set()
		{
			using (var text = SystemFonts.MessageBoxFont)
			using (var forms = SystemFonts.DefaultFont)
			{
				foreach (var choice in new[] { "", AppFonts.BuiltInName, "Arial" })
					Assert.AreEqual(text.SizeInPoints, AppFonts.SizeFor(choice, 0), "The default size of " + choice + " is not Windows' text size.");
				Assert.AreEqual(forms.SizeInPoints, AppFonts.SizeFor(AppFonts.WindowsDefault, 0), "The default size of Windows default is not Windows Forms' own.");
				foreach (var choice in new[] { "", AppFonts.BuiltInName, AppFonts.WindowsDefault, "Arial" })
					Assert.AreEqual(10.5f, AppFonts.SizeFor(choice, 10.5f), "The size set does not apply to " + choice + ".");
			}
		}

		[TestMethod, TestCategory("interface")]
		[Description("The size list starts with the default, then one size for each whole pixel from three quarters of the default to half as large again, each shown in points and pixels, and keeps a saved size that is not among them in its place")]
		public void Size_list_offers_the_default_then_one_size_a_pixel()
		{
			foreach (var choice in new[] { "", AppFonts.WindowsDefault })
			{
				var defaultSize = AppFonts.DefaultSize(choice);
				var sizes = AppFonts.Sizes(choice, 0);
				Assert.AreEqual(0f, sizes[0], "The default is not first.");
				Assert.AreEqual(string.Format("Default ({0:0.##} pt)", defaultSize), AppFonts.SizeText(choice, 0));
				var pixels = sizes.Skip(1).Select(AppFonts.Pixels).ToList();
				var from = (int)Math.Ceiling(AppFonts.Pixels(defaultSize) * 0.75);
				var to = (int)Math.Floor(AppFonts.Pixels(defaultSize) * 1.5);
				CollectionAssert.AreEqual(Enumerable.Range(from, to - from + 1).ToList(), pixels,
					choice + ": the sizes are not one a pixel from " + from + " to " + to + " px: " + string.Join(", ", pixels));
				foreach (var size in sizes.Skip(1))
					Assert.AreEqual(string.Format("{0:0.##} pt ({1} px)", size, AppFonts.Pixels(size)), AppFonts.SizeText(choice, size));
				// A size saved on a screen at another scale, or by hand, is kept in its place.
				var saved = defaultSize * 2.5f;
				var withSaved = AppFonts.Sizes(choice, saved);
				Assert.AreEqual(sizes.Count + 1, withSaved.Count, "The saved size is not listed.");
				CollectionAssert.AreEqual(withSaved.Skip(1).OrderBy(x => x).ToList(), withSaved.Skip(1).ToList(), "The saved size is not in its place.");
				CollectionAssert.Contains(withSaved, saved, "The saved size is not listed as saved.");
				CollectionAssert.AreEqual(sizes, AppFonts.Sizes(choice, sizes[3]), "A saved size the list holds is listed twice.");
			}
		}

		[TestMethod, TestCategory("interface")]
		[Description("The font chosen in the options gives Segoe UI when empty, Selawik for Selawik, Windows' own fonts for Windows default, an installed family by its name, and the default for a family not installed; where Windows has no Segoe UI, the default is Selawik")]
		public void Chosen_font_gives_the_family_the_windows_are_drawn_in()
		{
			LoadFonts();
			if (!AppFonts.DrawsDefault)
				Assert.Inconclusive(AppFonts.DefaultName + " is not installed here, so the default cannot be checked.");
			using (var family = AppFonts.InterfaceFamilyFor(null))
				Assert.AreEqual(AppFonts.DefaultName, family?.Name, "No choice is not Segoe UI.");
			using (var family = AppFonts.InterfaceFamilyFor(""))
				Assert.AreEqual(AppFonts.DefaultName, family?.Name, "The default choice is not Segoe UI.");
			Assert.AreSame(AppFonts.BuiltInFamily, AppFonts.InterfaceFamilyFor(AppFonts.BuiltInName), "Selawik is not the built-in Selawik.");
			Assert.IsNull(AppFonts.InterfaceFamilyFor(AppFonts.WindowsDefault), "Windows default does not give Windows' own fonts.");
			var installed = AppFonts.InstalledFamilies();
			var name = installed.Contains("Arial") ? "Arial" : installed.First();
			using (var family = AppFonts.InterfaceFamilyFor(name))
				Assert.AreEqual(name, family?.Name, "An installed family is not the one drawn in.");
			using (var family = AppFonts.InterfaceFamilyFor("No Such Font 4.29"))
				Assert.AreEqual(AppFonts.DefaultName, family?.Name, "A family not installed does not fall back to the default.");
			// A symbol font is installed, but would draw the windows in symbols.
			using (var collection = new System.Drawing.Text.InstalledFontCollection())
				if (collection.Families.Any(x => x.Name == "Wingdings"))
					using (var family = AppFonts.InterfaceFamilyFor("Wingdings"))
						Assert.AreEqual(AppFonts.DefaultName, family?.Name, "A symbol font is drawn in.");
			// A computer without Segoe UI.
			var defaultName = AppFonts.DefaultName;
			try
			{
				AppFonts.DefaultName = "No Such Font 4.29";
				Assert.IsFalse(AppFonts.DrawsDefault, "A family not installed is drawn.");
				Assert.AreSame(AppFonts.BuiltInFamily, AppFonts.InterfaceFamilyFor(""), "Without Segoe UI the default is not Selawik.");
				Assert.AreSame(AppFonts.BuiltInFamily, AppFonts.InterfaceFamilyFor("No Such Font 4.30"), "Without Segoe UI a family not installed does not fall back to Selawik.");
			}
			finally
			{
				AppFonts.DefaultName = defaultName;
			}
		}

		[TestMethod, TestCategory("interface")]
		[Description("The font list starts with the default, Segoe UI, then Selawik and Windows default, then the other installed families in order, each with a regular and a bold style that draws letters, and keeps a saved family that is not installed; where Windows has no Segoe UI, the default is Selawik and Selawik is not listed again")]
		public void Font_list_offers_the_default_selawik_windows_default_then_installed_families()
		{
			LoadFonts();
			var installed = AppFonts.InstalledFamilies();
			Assert.IsTrue(installed.Count > 0, "No installed family was found.");
			if (!installed.Contains(AppFonts.DefaultName))
				Assert.Inconclusive(AppFonts.DefaultName + " is not installed here, so the default cannot be checked.");
			CollectionAssert.AreEqual(installed.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(), installed, "The installed families are not in order.");
			CollectionAssert.DoesNotContain(installed, AppFonts.BuiltInName, "Selawik is listed twice: built in and installed.");
			foreach (var symbols in new[] { "Wingdings", "Webdings", "Marlett", "Symbol", "Segoe MDL2 Assets" })
				CollectionAssert.DoesNotContain(installed, symbols, symbols + " draws no letters, yet it is listed.");
			using (var collection = new System.Drawing.Text.InstalledFontCollection())
				foreach (var family in collection.Families.Where(x => installed.Contains(x.Name)))
					Assert.IsTrue(family.IsStyleAvailable(FontStyle.Regular) && family.IsStyleAvailable(FontStyle.Bold), family.Name + " has no regular or no bold style.");
			var others = installed.Where(x => x != AppFonts.DefaultName).ToList();
			var choices = AppFonts.Choices("", installed);
			Assert.AreEqual("", choices[0], "The default is not first.");
			Assert.AreEqual(AppFonts.BuiltInName, choices[1], "Selawik is not second.");
			Assert.AreEqual(AppFonts.WindowsDefault, choices[2], "Windows default is not third.");
			CollectionAssert.AreEqual(others, choices.Skip(3).ToList(), "The other installed families do not follow, or Segoe UI is listed twice.");
			Assert.AreEqual("Segoe UI (default)", AppFonts.ChoiceText(choices[0], installed));
			Assert.AreEqual("Selawik (built in)", AppFonts.ChoiceText(choices[1], installed));
			Assert.AreEqual("Windows default", AppFonts.ChoiceText(choices[2], installed));
			Assert.AreEqual(others[0], AppFonts.ChoiceText(choices[3], installed));
			// A family saved, then removed from Windows, is kept in its place and says it is missing.
			var saved = "No Such Font 4.29";
			choices = AppFonts.Choices(saved, installed);
			Assert.AreEqual(others.Count + 4, choices.Count, "The saved family is not listed.");
			CollectionAssert.AreEqual(choices.Skip(3).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(), choices.Skip(3).ToList(), "The saved family is not in its place.");
			Assert.AreEqual(saved + " (not installed)", AppFonts.ChoiceText(saved, installed));
			Assert.AreEqual(saved, AppFonts.ChoiceText(saved, null), "A family is said to be missing before the installed ones are read.");
			// A computer without Segoe UI: Selawik is the default, and Segoe UI here is one more installed family.
			var defaultName = AppFonts.DefaultName;
			try
			{
				AppFonts.DefaultName = "No Such Font 4.30";
				choices = AppFonts.Choices("", installed);
				Assert.AreEqual("", choices[0], "The default is not first.");
				Assert.AreEqual(AppFonts.WindowsDefault, choices[1], "Selawik is listed again after the default, or Windows default is not second.");
				CollectionAssert.AreEqual(installed, choices.Skip(2).ToList(), "The installed families do not follow.");
				Assert.AreEqual("Selawik (default, built in)", AppFonts.ChoiceText("", installed));
				CollectionAssert.Contains(AppFonts.Choices(AppFonts.BuiltInName, installed), AppFonts.BuiltInName, "Selawik saved is not listed.");
			}
			finally
			{
				AppFonts.DefaultName = defaultName;
			}
		}

		/// <summary>
		/// Reads the fonts as the program does, Selawik with them, into a settings folder of the tests' own: the fonts are written
		/// into its Temp folder, and stay there in use until the tests end.
		/// </summary>
		internal static void LoadFonts()
		{
			var appData = EngineHelper.AppDataPath;
			try
			{
				EngineHelper.AppDataPath = Path.Combine(Path.GetTempPath(), "x360ce-fonts-test");
				AppFonts.Load();
				Assert.IsNotNull(AppFonts.BuiltInFamily, "Selawik was not read from the program.");
			}
			finally
			{
				EngineHelper.AppDataPath = appData;
			}
		}

		/// <summary>The font Windows Forms draws menus and tool bars in, as the theme sets it.</summary>
		static Font MenuFont()
		{
			return (Font)typeof(ToolStripManager).GetField("defaultFont", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null);
		}

		/// <summary>
		/// Makes a window as a designer does, with a page added once it is made, as the program adds its pages; checks its
		/// fonts while the program's are set; and gives the bounds of everything on it that does not size itself to its text.
		/// </summary>
		/// <param name="shownFirst">Whether the window is shown before it is themed, as a dialog is.</param>
		/// <param name="layoutFont">A font the window is given as its own once it is made, which it is laid out in; null for none.</param>
		static List<Rectangle> Bounds(bool shownFirst, Font layoutFont = null)
		{
			using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
			{
				form.SuspendLayout();
				form.AutoScaleDimensions = new SizeF(6F, 13F);
				form.AutoScaleMode = AutoScaleMode.Font;
				form.ClientSize = new Size(400, 300);
				var heading = new Label { AutoSize = false, Bounds = new Rectangle(10, 10, 200, 20), Text = "Heading", Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold) };
				var box = new TextBox { Location = new Point(10, 40), Width = 200, Text = "Driving" };
				var rich = new RichTextBox { Bounds = new Rectangle(220, 10, 170, 60) };
				form.Controls.AddRange(new Control[] { heading, box, rich });
				form.ResumeLayout(false);
				if (layoutFont != null)
					form.Font = layoutFont;
				// The rich text box holds its document before the window is themed, as the help boxes do.
				rich.Rtf = @"{\rtf1\ansi{\fonttbl{\f0 Segoe UI;}}\f0\fs18 plain {\b bold}\par}";
				var page = new UserControl();
				page.SuspendLayout();
				page.AutoScaleDimensions = new SizeF(6F, 13F);
				page.AutoScaleMode = AutoScaleMode.Font;
				page.Bounds = new Rectangle(10, 80, 380, 200);
				var grid = new DataGridView { Bounds = new Rectangle(5, 5, 300, 100) };
				grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Value", DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Consolas", 8.25F) } });
				var button = new Button { Bounds = new Rectangle(5, 120, 100, 30), Text = "Apply" };
				page.Controls.AddRange(new Control[] { grid, button });
				page.ResumeLayout(false);
				form.Controls.Add(page);
				if (shownFirst)
					form.Show();
				FormsTheme.Apply(form);
				form.Show();
				Application.DoEvents();
				var font = FormsTheme.Font;
				if (font != null)
				{
					Assert.AreEqual(font.Name, form.Font.Name);
					Assert.AreEqual(font.Name, heading.Font.Name, "A label that names Microsoft Sans Serif is not drawn in " + font.Name + ".");
					// Named at Windows Forms' own size, which the interface size stands in for.
					Assert.IsTrue(heading.Font.Bold, "The label lost its own style.");
					Assert.AreEqual(font.SizeInPoints, heading.Font.SizeInPoints, "The label that names Windows Forms' own size is not drawn at the interface size.");
					var code = grid.Columns[0].DefaultCellStyle.Font;
					Assert.AreEqual(FormsTheme.CodeFamily?.Name ?? "Consolas", code.Name, "A column in Consolas is not drawn in the code font.");
					Assert.AreEqual(font.SizeInPoints, code.SizeInPoints, "A column in Consolas at Windows Forms' own size is not drawn at the interface size.");
					Assert.AreEqual(font.Name, GdiFace(SendMessage(box.Handle, WM_GETFONT, IntPtr.Zero, IntPtr.Zero), false),
						"Windows' own text box is drawn in another face.");
					rich.Select(rich.Text.IndexOf("bold", StringComparison.Ordinal), 4);
					Assert.IsTrue(rich.SelectionFont.Bold, "The rich text box lost the fonts its document gives it.");
				}
				return new List<Rectangle> { new Rectangle(Point.Empty, form.ClientSize), heading.Bounds, rich.Bounds, page.Bounds, grid.Bounds, button.Bounds };
			}
		}

		const int WM_GETFONT = 0x0031;

		[DllImport("user32.dll")]
		static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

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

		/// <summary>The face GDI draws a font handle with: the one it found for the name the font asks for.</summary>
		static string GdiFace(IntPtr font, bool delete)
		{
			var dc = CreateCompatibleDC(IntPtr.Zero);
			var old = SelectObject(dc, font);
			var face = new StringBuilder(64);
			GetTextFace(dc, face.Capacity, face);
			SelectObject(dc, old);
			DeleteDC(dc);
			if (delete)
				DeleteObject(font);
			return face.ToString();
		}
	}
}
