// @under-test: Engine/JocysCom/Controls/UiTree/UiLanguage.cs, Engine/JocysCom/Controls/UiTree/UiLanguage.Forms.cs, App.v4/Common/AppLanguages.cs, App.v4/Languages
// @area: languages   @layer: unit
using JocysCom.ClassLibrary.Controls.UiTree;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using x360ce.App;

namespace x360ce.Tests
{
	/// <summary>
	/// The interface in other languages: the sync says what a language lacks, a translation is shown and the English comes
	/// back, a missing or stale translation shows the English, the globe's menu lists every language, and every language
	/// document in the repository is valid against the English the program has.
	/// </summary>
	[TestClass]
	public class LanguageTest
	{
		/// <summary>A panel of this program's own, as the designer makes one: a caption, a button, a bar with an entry, a grid column.</summary>
		public sealed class LanguageTestPanel : UserControl
		{
			public LanguageTestPanel()
			{
				Name = "LanguageTestPanel";
				Caption = new Label { Name = "Caption", Text = "Dead Zone" };
				Save = new Button { Name = "Save", Text = "&Save" };
				Bar = new ToolStrip { Name = "Bar" };
				Entry = new ToolStripButton { Name = "Entry", Text = "Test...", ToolTipText = "Test the controller" };
				Bar.Items.Add(Entry);
				Grid = new DataGridView { Name = "Grid" };
				Column = new DataGridViewTextBoxColumn { Name = "NameColumn", HeaderText = "Name" };
				Grid.Columns.Add(Column);
				Value = new Label { Name = "Value", Text = "12" };
				Controls.AddRange(new Control[] { Caption, Save, Bar, Grid, Value });
			}

			public Label Caption;
			public Button Save;
			public ToolStrip Bar;
			public ToolStripButton Entry;
			public DataGridView Grid;
			public DataGridViewTextBoxColumn Column;
			public Label Value;
		}

		static UiLanguageDocument German()
		{
			return new UiLanguageDocument
			{
				Culture = "de",
				Items = new List<UiLanguageEntry>
				{
					new UiLanguageEntry { Key = "LanguageTestPanel.Caption.Text", Source = "Dead Zone", Text = "Totzone" },
					new UiLanguageEntry { Key = "LanguageTestPanel.Save.Text", Source = "&Save", Text = "&Speichern" },
					new UiLanguageEntry { Key = "LanguageTestPanel.Entry.Text", Source = "Test...", Text = "Testen..." },
					new UiLanguageEntry { Key = "LanguageTestPanel.Entry.ToolTipText", Source = "Test the controller", Text = "Den Controller testen" },
					new UiLanguageEntry { Key = "LanguageTestPanel.NameColumn.HeaderText", Source = "Name", Text = "Name (de)" },
					// Translated from English the program no longer has: shown in English.
					new UiLanguageEntry { Key = "LanguageTestPanel.Stale", Source = "Old words", Text = "Alte Wörter" },
				},
			};
		}

		/// <summary>Runs a test with the panel's namespace counted as the program's own, and English shown before and after.</summary>
		static void WithOwnTypes(Action action)
		{
			var own = UiTreeWalker.OwnNamespaces;
			UiTreeWalker.OwnNamespaces = new[] { "x360ce", "JocysCom" };
			try
			{
				UiLanguage.Use(null);
				action();
			}
			finally
			{
				UiLanguage.Use(null);
				UiTreeWalker.OwnNamespaces = own;
			}
		}

		[TestMethod, TestCategory("languages")]
		[Description("A translation is shown at once, and switching back to English shows the English again")]
		public void Translation_is_shown_and_English_comes_back()
		{
			Ui.OnUiThread(() => WithOwnTypes(() =>
			{
				using (var panel = new LanguageTestPanel())
				{
					UiLanguage.Register(panel);
					UiLanguage.Use(German());
					Assert.AreEqual("Totzone", panel.Caption.Text);
					Assert.AreEqual("&Speichern", panel.Save.Text);
					Assert.AreEqual("Testen...", panel.Entry.Text);
					Assert.AreEqual("Den Controller testen", panel.Entry.ToolTipText);
					Assert.AreEqual("Name (de)", panel.Column.HeaderText);
					Assert.AreEqual("12", panel.Value.Text, "A number is not words, and is never recorded.");
					// What a reader that must not depend on the language reads: the English.
					Assert.AreEqual("Dead Zone", UiLanguage.SourceOf(panel.Caption, "Text", panel.Caption.Text));
					var node = UiTreeWalker.Read(panel.Save, false, null);
					Assert.AreEqual("Save", node.Name, "The interface description names a control in English whatever language is shown.");
					UiLanguage.Use(null);
					Assert.AreEqual("Dead Zone", panel.Caption.Text);
					Assert.AreEqual("&Save", panel.Save.Text);
					Assert.AreEqual("Test...", panel.Entry.Text);
					Assert.AreEqual("Test the controller", panel.Entry.ToolTipText);
					Assert.AreEqual("Name", panel.Column.HeaderText);
				}
			}));
		}

		[TestMethod, TestCategory("languages")]
		[Description("A control built in another language shows it as it is registered, and words the program wrote since are left alone")]
		public void Registered_in_a_language_and_program_words_kept()
		{
			Ui.OnUiThread(() => WithOwnTypes(() =>
			{
				UiLanguage.Use(German());
				using (var panel = new LanguageTestPanel())
				{
					UiLanguage.Register(panel);
					Assert.AreEqual("Totzone", panel.Caption.Text, "A control is shown in the language chosen as it is recorded.");
					// The program writes words of its own on the caption: a change of language must not overwrite them.
					panel.Caption.Text = "Range";
					UiLanguage.Use(null);
					Assert.AreEqual("Range", panel.Caption.Text);
					Assert.AreEqual("&Save", panel.Save.Text);
				}
			}));
		}

		[TestMethod, TestCategory("languages")]
		[Description("A text without a translation, or translated from other English, shows in English")]
		public void Missing_or_stale_translation_falls_back_to_English()
		{
			WithOwnTypes(() =>
			{
				UiLanguage.Use(German());
				Assert.AreEqual("Totzone", UiLanguage.T("LanguageTestPanel.Caption.Text", "Dead Zone"));
				Assert.AreEqual("Nothing here", UiLanguage.T("LanguageTestPanel.Missing", "Nothing here"));
				Assert.AreEqual("New words", UiLanguage.T("LanguageTestPanel.Stale", "New words"), "Translated from other English.");
				Assert.IsNull(UiLanguage.Find(null, "Words"));
			});
		}

		[TestMethod, TestCategory("languages")]
		[Description("The sync finds the texts to add, to update and to remove")]
		public void Compare_finds_add_update_and_remove()
		{
			var sources = new[]
			{
				new UiLanguageEntry { Key = "A.Same", Source = "Same" },
				new UiLanguageEntry { Key = "B.Changed", Source = "New English" },
				new UiLanguageEntry { Key = "C.New", Source = "Added" },
				new UiLanguageEntry { Key = "D.Empty", Source = "Untranslated" },
			};
			var document = new UiLanguageDocument
			{
				Culture = "de",
				Items = new List<UiLanguageEntry>
				{
					new UiLanguageEntry { Key = "A.Same", Source = "Same", Text = "Gleich" },
					new UiLanguageEntry { Key = "B.Changed", Source = "Old English", Text = "Alt" },
					new UiLanguageEntry { Key = "D.Empty", Source = "Untranslated", Text = "" },
					new UiLanguageEntry { Key = "E.Gone", Source = "Gone", Text = "Weg" },
				},
			};
			var report = UiLanguage.Compare(sources, document);
			CollectionAssert.AreEqual(new[] { "C.New", "D.Empty" }, report.Add.Select(x => x.Key).ToArray());
			Assert.AreEqual(1, report.Update.Count);
			Assert.AreEqual("B.Changed", report.Update[0].Key);
			Assert.AreEqual("New English", report.Update[0].Source);
			Assert.AreEqual("Old English", report.Update[0].Was);
			Assert.AreEqual("Alt", report.Update[0].Text);
			CollectionAssert.AreEqual(new[] { "E.Gone" }, report.Remove);
			Assert.AreEqual(1, report.Translated);
			Assert.AreEqual(4, report.Total);
			Assert.IsFalse(report.IsComplete);
		}

		[TestMethod, TestCategory("languages")]
		[Description("A language document reads back as written, one text to a line in the order of its keys")]
		public void Document_round_trips_in_key_order()
		{
			var document = German();
			document.Items.Reverse();
			document.Items.Add(new UiLanguageEntry { Key = "Z.Quote", Source = "Say \"hi\"\r\nC:\\x", Text = "Sag „hallo“\r\nC:\\x 中文" });
			var json = UiLanguage.Write(document);
			var keys = Regex.Matches(json, "\"Key\": \"([^\"]+)\"").Cast<Match>().Select(m => m.Groups[1].Value).ToArray();
			CollectionAssert.AreEqual(keys.OrderBy(x => x, StringComparer.Ordinal).ToArray(), keys, "The texts are written in the order of their keys.");
			StringAssert.Contains(json, "中文", "Letters are written as they are.");
			Assert.AreEqual(document.Items.Count, json.Split('\n').Count(x => x.Contains("\"Key\"")), "One text to a line.");
			var read = UiLanguage.Read(json);
			Assert.AreEqual("de", read.Culture);
			var quote = read.Items.Single(x => x.Key == "Z.Quote");
			Assert.AreEqual("Say \"hi\"\r\nC:\\x", quote.Source);
			Assert.AreEqual("Sag „hallo“\r\nC:\\x 中文", quote.Text);
			Assert.AreEqual(json, UiLanguage.Write(read), "Written again, a document is the same.");
			// A translator may write the fields in any order.
			var shuffled = UiLanguage.Read("{ \"Items\": [ { \"Text\": \"T\", \"Source\": \"S\", \"Key\": \"K\" } ], \"Culture\": \"de\" }");
			Assert.AreEqual("de", shuffled.Culture);
			Assert.AreEqual("K", shuffled.Items[0].Key);
			Assert.AreEqual("S", shuffled.Items[0].Source);
			Assert.AreEqual("T", shuffled.Items[0].Text);
		}

		/// <summary>A text built in code, as the program writes one, for the sync to find.</summary>
		static string CodeText()
		{
			return UiLanguage.T("LanguageTest.CodeText", "Built in code");
		}

		[TestMethod, TestCategory("languages")]
		[Description("The sync finds every text built in code from the compiled program")]
		public void Calls_in_code_are_found()
		{
			var t = typeof(UiLanguage).GetMethod(nameof(UiLanguage.T));
			var found = UiLanguage.FindCalls(typeof(LanguageTest).Assembly, t);
			var text = found.SingleOrDefault(x => x.Key == "LanguageTest.CodeText");
			Assert.IsNotNull(text, "The call in CodeText was not found.");
			Assert.AreEqual("Built in code", text.Source);
			Assert.AreEqual("Built in code", CodeText());
			var program = UiLanguage.FindCalls(typeof(AppLanguages).Assembly, t);
			Assert.IsTrue(program.Any(x => x.Key == "PadControl.ClearControllerSettings"), "A dialog's title in the program was not found.");
		}

		/// <summary>A control that shows one sentence through <see cref="UiLanguage.T"/>, one without, and logs a third.</summary>
		public sealed class UntranslatedTestLabel : Label
		{
			public void Show(int count)
			{
				Text = UiLanguage.T("UntranslatedTestLabel.Translated", "This one is translated.");
				System.Diagnostics.Debug.WriteLine("This one goes to the log.");
				AccessibleName = "Stays in English";
				Text = string.Format("{0: yyyy-MM-dd HH:mm:ss}: {1}", DateTime.Now, count);
				Describe("English accessibility helper");
				Text = "This one is not translated: " + count;
			}

			[System.ComponentModel.Localizable(false)]
			void Describe(string description)
			{
				AccessibleDescription = description;
			}
		}

		[TestMethod, TestCategory("languages")]
		[Description("The sync names a sentence a control shows without a translation, and leaves the translated, the logged and the accessible name alone")]
		public void Untranslated_texts_are_named()
		{
			var t = typeof(UiLanguage).GetMethod(nameof(UiLanguage.T));
			var found = UiLanguage.FindUntranslated(typeof(LanguageTest).Assembly, x => x == typeof(UntranslatedTestLabel), t);
			CollectionAssert.AreEqual(new[] { "UntranslatedTestLabel.Show: This one is not translated: " }, found.ToArray());
		}

		[TestMethod, TestCategory("languages")]
		[Description("Every issue on the Issues tab, and the tab itself, says its words in the language shown")]
		public void Issues_say_every_sentence_in_the_language_shown()
		{
			var t = typeof(UiLanguage).GetMethod(nameof(UiLanguage.T));
			var issues = typeof(JocysCom.ClassLibrary.Controls.IssuesControl.IssueItem);
			Func<Type, bool> shows = x => issues.IsAssignableFrom(x) || x == typeof(JocysCom.ClassLibrary.Controls.IssuesControl.IssuesUserControl)
				|| x.Namespace == "x360ce.App.Issues";
			var found = UiLanguage.FindUntranslated(typeof(AppLanguages).Assembly, shows, t)
				.Concat(UiLanguage.FindUntranslated(issues.Assembly, shows, t)).ToList();
			Assert.AreEqual(0, found.Count, "Not translated:" + Environment.NewLine + string.Join(Environment.NewLine, found));
		}

		[TestMethod, TestCategory("languages")]
		[Description("The globe's menu lists the eleven languages, each in its own words with its flag, the one shown ticked")]
		public void Globe_menu_lists_eleven_languages_with_flags()
		{
			Ui.OnUiThread(() =>
			{
				using (var strip = new ToolStrip())
				{
					var button = new ToolStripDropDownButton("Language");
					strip.Items.Add(button);
					AppLanguages.FillMenu(button, x => { });
					var items = button.DropDownItems.Cast<ToolStripMenuItem>().ToArray();
					Assert.AreEqual(11, items.Length);
					CollectionAssert.AreEqual(
						new[] { "English", "中文", "Українська", "Español", "Português", "Deutsch", "日本語", "Français", "Polski", "한국어", "Türkçe" },
						items.Select(x => x.Text).ToArray());
					foreach (var item in items)
					{
						Assert.IsNotNull(item.Image, item.Text + " has no flag.");
						Assert.AreEqual(32, item.Image.Width, item.Text + "'s flag is not at twice its size.");
						Assert.IsFalse(item.Text.Contains(" "), item.Text + " is not one word.");
					}
					Assert.AreEqual(1, items.Count(x => x.Checked), "One language is ticked.");
					Assert.AreEqual(AppLanguages.Current.Name, items.Single(x => x.Checked).Text);
				}
			});
		}

		[TestMethod, TestCategory("languages")]
		[Description("Windows' display language picks the language offered for it, else English")]
		public void Windows_language_picks_the_default()
		{
			Assert.AreEqual("de", AppLanguages.For(new CultureInfo("de-AT")).Culture);
			Assert.AreEqual("zh-Hans", AppLanguages.For(new CultureInfo("zh-CN")).Culture);
			Assert.AreEqual("pt-BR", AppLanguages.For(new CultureInfo("pt-PT")).Culture);
			Assert.AreEqual("uk", AppLanguages.For(new CultureInfo("uk-UA")).Culture);
			Assert.IsNull(AppLanguages.For(new CultureInfo("zh-TW")), "Chinese in traditional characters is not offered.");
			Assert.IsNull(AppLanguages.For(new CultureInfo("ru-RU")));
			Assert.AreEqual("ko", AppLanguages.For("ko").Culture, "A saved choice wins.");
		}

		/// <summary>The repository's Languages folder.</summary>
		static string LanguagesFolder { get { return Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Languages"); } }

		[TestMethod, TestCategory("languages")]
		[Description("Every language document is valid, is embedded, and holds no text the English no longer has")]
		public void Every_language_document_is_valid()
		{
			var english = UiLanguage.Read(File.ReadAllText(Path.Combine(LanguagesFolder, "en.json"), Encoding.UTF8));
			var sources = english.Items.ToDictionary(x => x.Key, x => x.Source);
			var problems = new List<string>();
			foreach (var language in AppLanguages.All.Skip(1))
			{
				var path = Path.Combine(LanguagesFolder, language.Culture + ".json");
				Assert.IsTrue(File.Exists(path), path + " is missing.");
				var document = UiLanguage.Read(File.ReadAllText(path, Encoding.UTF8));
				Assert.AreEqual(language.Culture, document.Culture, path);
				Assert.IsNotNull(AppLanguages.ReadEmbedded(language.Culture), language.Culture + " is not embedded in the program.");
				var translated = 0;
				foreach (var item in document.Items)
				{
					string source;
					if (!sources.TryGetValue(item.Key, out source))
					{
						problems.Add(language.Culture + ": " + item.Key + " is no longer in the program");
						continue;
					}
					if (item.Source != source || string.IsNullOrEmpty(item.Text))
						continue;
					translated++;
					if (!Placeholders(item.Source).SequenceEqual(Placeholders(item.Text)))
						problems.Add(language.Culture + ": " + item.Key + " changes the placeholders: " + item.Text);
					if (HasAccessKey(item.Source) && !HasAccessKey(item.Text))
						problems.Add(language.Culture + ": " + item.Key + " lost the access key: " + item.Text);
				}
				Console.WriteLine("{0}: {1} of {2} translated", language.Culture, translated, sources.Count);
			}
			Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
		}

		[TestMethod, TestCategory("languages"), TestCategory("ui")]
		[Description("Switching the main window's language never pushes Add Game or any other Game-bar item off the bar")]
		public void Game_bar_keeps_every_item_on_the_bar_in_every_language()
		{
			Ui.OnUiThread(() => WithOwnTypes(() =>
			{
				var oldCloud = Global.CloudClient;
				var oldTheme = JocysCom.ClassLibrary.Controls.Themes.FormsTheme.Theme;
				var oldLook = JocysCom.ClassLibrary.Controls.ControlsHelper.LookFromTheme;
				if (Global.CloudClient == null)
					Global.CloudClient = new CloudClient();
				var problems = new List<string>();
				try
				{
					// Match startup: the theme is active before MainControl is built so the Game bar gets WPF toolbar styling.
					JocysCom.ClassLibrary.Controls.Themes.FormsTheme.SetTheme(JocysCom.ClassLibrary.Controls.Themes.ThemeType.Light);
					using (var form = new x360ce.App.Controls.BaseFormWithHeader())
					using (var main = new MainControl { Dock = DockStyle.Fill })
					{
						form.Size = new System.Drawing.Size(960, 720);
						form.Controls.Add(main);
						form.Controls.SetChildIndex(main, 0);
						main.GameToCustomizeComboBox.Items.Add("Very Long Game Title That Should Never Push Buttons Off The Bar (C:\\Program Files\\Example\\Game.exe)");
						main.GameToCustomizeComboBox.SelectedIndex = 0;
						JocysCom.ClassLibrary.Controls.Themes.FormsTheme.Apply(form);
						UiLanguage.Register(main);
						form.Show();
						Application.DoEvents();
						var smallest = PadPageShortWindowTest.MinimumWindowSize();
						var sizes = new[] { form.Size, smallest };
						var bar = main.GamesToolStrip;
						Assert.AreEqual(ToolStripLayoutStyle.Table, bar.LayoutStyle, "Game bar must use Table layout so items cannot disappear.");
						Assert.IsFalse(bar.CanOverflow, "Game bar must not overflow items.");
						Assert.AreEqual("WpfToolStripRenderer", bar.Renderer.GetType().Name, "Game bar should use the themed toolbar renderer.");
						foreach (var theme in new[] { JocysCom.ClassLibrary.Controls.Themes.ThemeType.Light, JocysCom.ClassLibrary.Controls.Themes.ThemeType.Dark })
						{
							JocysCom.ClassLibrary.Controls.Themes.FormsTheme.SetTheme(theme);
							foreach (var showTest in new[] { false, true })
							{
								main.TestButton.Visible = showTest;
								foreach (var size in sizes)
								{
									form.Size = size;
									foreach (var language in AppLanguages.All)
									{
										AppLanguages.Use(language);
										Application.DoEvents();
										var visible = bar.Items.Cast<ToolStripItem>().Where(x => x.Available).ToList();
										ToolStripItem prev = null;
										foreach (var item in visible)
										{
											if (item.Placement != ToolStripItemPlacement.Main)
												problems.Add(string.Format("{0} ({1}, {2}, Test={3}): {4} pushed off the bar ({5})", language.Culture, theme, size.Width, showTest, item.Name, item.Placement));
											else if (item.Bounds.Width <= 0 || item.Bounds.Right > bar.ClientSize.Width + 1 || item.Bounds.Top < 0 || item.Bounds.Bottom > bar.ClientSize.Height + 1)
												problems.Add(string.Format("{0} ({1}, {2}, Test={3}): {4} bounds {5} outside bar {6}", language.Culture, theme, size.Width, showTest, item.Name, item.Bounds, bar.ClientSize));
											if (item != main.GameToCustomizeComboBox)
											{
												var preferred = item.GetPreferredSize(System.Drawing.Size.Empty).Width;
												if (item.Bounds.Width < preferred - 1)
													problems.Add(string.Format("{0} ({1}, {2}, Test={3}): {4} clipped ({5}px < preferred {6}px)", language.Culture, theme, size.Width, showTest, item.Name, item.Bounds.Width, preferred));
											}
											if (prev != null && item.Bounds.Left < prev.Bounds.Right)
												problems.Add(string.Format("{0} ({1}, {2}, Test={3}): {4} overlaps {5} ({6} vs {7})", language.Culture, theme, size.Width, showTest, item.Name, prev.Name, item.Bounds, prev.Bounds));
											prev = item;
										}
										if (main.GameToCustomizeComboBox.Bounds.Width < 100)
											problems.Add(string.Format("{0} ({1}, {2}, Test={3}): GameToCustomizeComboBox too narrow ({4}px)", language.Culture, theme, size.Width, showTest, main.GameToCustomizeComboBox.Bounds.Width));
										var gapToAdd = main.AddGameButton.Bounds.Left - main.GameToCustomizeComboBox.Bounds.Right;
										if (gapToAdd > 20)
											problems.Add(string.Format("{0} ({1}, {2}, Test={3}): GameToCustomizeComboBox did not fill column (gap={4}px)", language.Culture, theme, size.Width, showTest, gapToAdd));
										if (!showTest)
										{
											var gapToSave = main.SaveAllButton.Bounds.Left - main.AddGameButton.Bounds.Right;
											if (gapToSave > 20)
												problems.Add(string.Format("{0} ({1}, {2}): hidden TestButton left a wide gap ({3}px)", language.Culture, theme, size.Width, gapToSave));
										}
									}
								}
							}
						}
						AppLanguages.Use(AppLanguages.All[0]);
					}
				}
				finally
				{
					JocysCom.ClassLibrary.Controls.Themes.FormsTheme.SetTheme(oldTheme);
					JocysCom.ClassLibrary.Controls.ControlsHelper.LookFromTheme = oldLook;
					Global.CloudClient = oldCloud;
				}
				Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
			}));
		}

		static string[] Placeholders(string text)
		{
			return Regex.Matches(text, @"\{\d+[^}]*\}").Cast<Match>().Select(m => m.Value).OrderBy(x => x, StringComparer.Ordinal).ToArray();
		}

		static bool HasAccessKey(string text)
		{
			return Regex.IsMatch(text.Replace("&&", ""), @"&[^&\s]");
		}
	}
}
