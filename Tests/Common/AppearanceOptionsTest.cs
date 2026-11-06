// @under-test: App.v4/Controls/OptionsUserControl.cs, App.v4/Common/AppFonts.cs, App.v4/Common/Options.cs
// @area: options   @layer: unit
using JocysCom.ClassLibrary.Controls.Themes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;

namespace x360ce.Tests
{
	/// <summary>
	/// The Appearance group on the Options page: the font and size lists show the saved choices and save the ones chosen,
	/// offer a restart while they are not the font and size in use, and the theme list works from its place in the same group.
	/// </summary>
	/// <remarks>
	/// The choices are picked by the text the list shows, as a person and the AI door pick them. The font list is filled
	/// again once the installed fonts are read, which must keep the choice and write nothing back.
	/// </remarks>
	[TestClass]
	public class AppearanceOptionsTest
	{
		[TestMethod, TestCategory("ui"), TestCategory("options")]
		[Description("The font list shows the saved choice, saves the one chosen by its text, and shows Restart while it is not the font in use")]
		public void Font_list_shows_and_saves_the_choice()
		{
			OnPage("", 0, (font, size, theme, restart) =>
			{
				var byDefault = AppFonts.ChoiceText("");
				StringAssert.StartsWith(byDefault, "Default (", "The default does not name its font.");
				Assert.AreEqual(byDefault, font.Text, "The list does not show the default font saved.");
				Assert.IsFalse(restart.Visible, "Restart shows while the font chosen is the one in use.");
				Choose(font, "Selawik (built in)");
				Assert.AreEqual(AppFonts.BuiltInName, SettingsManager.Options.InterfaceFont, "Selawik was not saved by its name.");
				Assert.IsTrue(restart.Visible, "Restart is not offered for a font not in use.");
				var installed = font.Items.Cast<string>().Skip(2).First();
				Choose(font, installed);
				Assert.AreEqual(installed, SettingsManager.Options.InterfaceFont, "An installed family was not saved by its name.");
				Choose(font, byDefault);
				Assert.AreEqual("", SettingsManager.Options.InterfaceFont, "The default font is not saved as empty.");
				Assert.IsFalse(restart.Visible, "Restart stays after going back to the font in use.");
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("options")]
		[Description("A saved font no longer installed is not kept at start: the lists show the default font and its size, and no restart is offered")]
		public void Font_not_installed_is_not_kept_at_start()
		{
			OnPage("No Such Font 4.29", 0, (font, size, theme, restart) =>
			{
				Assert.AreEqual(AppFonts.ChoiceText(""), font.Text, "The list does not show the default font.");
				Assert.AreEqual(AppFonts.SizeText("", 0), size.Text, "The list does not show the default font's size.");
				Assert.AreEqual("", SettingsManager.Options.InterfaceFont, "The font not installed was kept.");
				Assert.IsFalse(restart.Visible, "Restart is offered while the windows are drawn in the font shown.");
			}, true);
		}

		[TestMethod, TestCategory("ui"), TestCategory("options")]
		[Description("Once the installed fonts are listed, the font list opens wide enough to show its longest name whole")]
		public void Font_list_opens_as_wide_as_its_longest_name()
		{
			OnPage("", 0, (font, size, theme, restart) =>
			{
				var needs = FormsTheme.GetComboBoxWidth(font);
				Assert.IsTrue(font.DropDownWidth >= needs, string.Format(
					"The list opens {0} px wide and its longest name needs {1}.", font.DropDownWidth, needs));
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("options")]
		[Description("The size list shows the default first and then one size for each pixel, saves the size chosen by its text, shows Restart while it is not the size in use, and follows the default of the font chosen")]
		public void Size_list_shows_and_saves_the_size()
		{
			OnPage("", 0, (font, size, theme, restart) =>
			{
				var texts = size.Items.Cast<object>().Select(size.GetItemText).ToList();
				Assert.AreEqual(AppFonts.SizeText("", 0), size.Text, "The list does not show the default size saved.");
				CollectionAssert.AreEqual(AppFonts.Sizes("", 0).Select(x => AppFonts.SizeText("", x)).ToList(), texts, "The list does not offer the sizes.");
				StringAssert.StartsWith(texts[0], "Default (", "The default is not first.");
				var larger = AppFonts.Sizes("", 0).Last();
				Choose(size, AppFonts.SizeText("", larger));
				Assert.AreEqual(larger, SettingsManager.Options.InterfaceFontSize, "The size chosen was not saved.");
				Assert.IsTrue(restart.Visible, "Restart is not offered for a size not in use.");
				Assert.AreEqual(string.Format("{0} at {1:0.##} pt is used when x360ce starts again.", AppFonts.ChoiceText(""), larger),
					AppFonts.RestartNote("", larger), "The note does not say what is used when x360ce starts again.");
				// Another font is drawn at Windows' text size by default, and the size chosen stays chosen.
				Choose(font, "Selawik (built in)");
				Assert.AreEqual(AppFonts.SizeText(AppFonts.BuiltInName, 0), size.GetItemText(size.Items[0]), "The default size does not follow the font chosen.");
				Assert.AreEqual(AppFonts.SizeText(AppFonts.BuiltInName, larger), size.Text, "The size chosen is not kept when the font changes.");
				Choose(font, AppFonts.ChoiceText(""));
				Choose(size, AppFonts.SizeText("", 0));
				Assert.AreEqual(0f, SettingsManager.Options.InterfaceFontSize, "The default size is not saved as 0.");
				Assert.IsFalse(restart.Visible, "Restart stays after going back to the size in use.");
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("options")]
		[Description("A saved size the list does not offer is shown in its place, and showing it leaves it alone")]
		public void Size_not_in_the_list_is_shown_and_kept()
		{
			var saved = 20f;
			OnPage("", saved, (font, size, theme, restart) =>
			{
				Assert.AreEqual(AppFonts.SizeText("", saved), size.Text, "The list does not show the saved size.");
				Assert.AreEqual(saved, SettingsManager.Options.InterfaceFontSize, "Showing the page changed the saved size.");
				Assert.IsTrue(restart.Visible, "Restart is not offered for a size not in use.");
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("options")]
		[Description("The theme list in the Appearance group shows the saved theme and saves the one chosen")]
		public void Theme_list_works_from_the_appearance_group()
		{
			OnPage("", 0, (font, size, theme, restart) =>
			{
				Assert.AreEqual("AppearanceLayoutPanel", theme.Parent.Name, "The theme list is not in the Appearance group.");
				Choose(theme, "Dark");
				Assert.AreEqual(ThemeType.Dark, SettingsManager.Options.Theme, "The theme chosen was not saved.");
				SettingsManager.Options.Theme = ThemeType.Light;
				Assert.AreEqual("Light", theme.Text, "The list does not follow the saved theme.");
			});
		}

		/// <summary>Picks an item by the text the list shows, as the AI door does.</summary>
		static void Choose(ComboBox list, string text)
		{
			for (var i = 0; i < list.Items.Count; i++)
				if (list.GetItemText(list.Items[i]) == text)
				{
					list.SelectedIndex = i;
					return;
				}
			Assert.Fail("The list " + list.Name + " has no " + text + ".");
		}

		/// <summary>
		/// Builds the Options page with the font saved as <paramref name="saved"/> and the size as <paramref name="savedSize"/>,
		/// binds its Appearance group the way the main window does, waits for the installed fonts, and hands over the font,
		/// size and theme lists and the Restart button.
		/// </summary>
		/// <remarks>The options and the settings map are put back afterwards.</remarks>
		/// <param name="saved">The font the options hold.</param>
		/// <param name="savedSize">The size the options hold.</param>
		/// <param name="test">The test, given the font, size and theme lists and the Restart button.</param>
		/// <param name="started">Whether the options are applied first, as the program does when it starts.</param>
		static void OnPage(string saved, float savedSize, Action<ComboBox, ComboBox, ComboBox, Button> test, bool started = false)
		{
			var options = SettingsManager.Options;
			var originalFont = options.InterfaceFont;
			var originalSize = options.InterfaceFontSize;
			var originalTheme = options.Theme;
			// A page adds its boxes to the settings map and nothing takes them out, so the boxes of a
			// page built earlier in this process would hear the option instead of this one.
			var settingsMap = SettingsManager.Current.SettingsMap;
			var earlier = settingsMap.Where(IsOptionBox).ToArray();
			settingsMap.RemoveAll(IsOptionBox);
			try
			{
				options.InterfaceFont = saved;
				options.InterfaceFontSize = savedSize;
				options.Theme = ThemeType.Light;
				if (started)
					AppFonts.Apply(options);
				Ui.OnUiThread(() =>
				{
					using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-3000, -3000), ShowInTaskbar = false })
					using (var page = new OptionsUserControl())
					{
						form.Controls.Add(page);
						page.UpdateAppearanceMap();
						form.Show();
						var font = (ComboBox)page.Controls.Find("FontComboBox", true).Single();
						var size = (ComboBox)page.Controls.Find("SizeComboBox", true).Single();
						var theme = (ComboBox)page.Controls.Find("ThemeComboBox", true).Single();
						var restart = (Button)page.Controls.Find("RestartButton", true).Single();
						// Shown on its tab, as a person sees it, so Visible reads what the button is set to.
						((TabControl)page.Controls.Find("MainTabControl", true).Single()).SelectedIndex = 0;
						// The installed fonts are read on a worker and listed when they arrive.
						var watch = Stopwatch.StartNew();
						while (font.Items.Count <= 4 && watch.ElapsedMilliseconds < 10000)
							Application.DoEvents();
						Assert.IsTrue(font.Items.Count > 4, "The installed fonts were not listed within 10 s.");
						test(font, size, theme, restart);
					}
				});
			}
			finally
			{
				// Nothing is left bound, so putting the options back changes only the options.
				settingsMap.RemoveAll(IsOptionBox);
				options.InterfaceFont = originalFont;
				options.InterfaceFontSize = originalSize;
				options.Theme = originalTheme;
				settingsMap.AddRange(earlier);
			}
		}

		static bool IsOptionBox(SettingsMapItem item)
		{
			return item.Property != null && item.Property.DeclaringType == typeof(Options);
		}
	}
}
