// @under-test: Engine/JocysCom/Controls/Themes/Default.Forms.cs, Engine/JocysCom/Controls/Themes/Default_DarkTheme.xaml, App.v4/Controls/OptionsUserControl.cs
// @area: interface   @layer: unit
using JocysCom.ClassLibrary.Controls.Themes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using x360ce.App.Controls;

namespace x360ce.Tests
{
	/// <summary>The dark theme gives the system colours the WPF dark colours, and the light theme gives them back.</summary>
	/// <remarks>
	/// The system colours belong to the whole process, so every test ends light again, even when an
	/// assertion fails, or the tests after it would run dark.
	/// </remarks>
	[TestClass]
	public class FormsThemeTest
	{
		/// <summary>Runs the test on an interface thread and leaves the theme light afterwards.</summary>
		static void OnUiThread(Action test)
		{
			Ui.OnUiThread(() =>
			{
				try { test(); }
				finally { FormsTheme.SetTheme(ThemeType.Light); }
			});
		}

		/// <summary>
		/// The room Default.xaml's frame takes at each side of a box's text, in pixels: its 1-pixel line, wider only from 200 %,
		/// and 4 device-independent pixels of room inside it.
		/// </summary>
		static int FrameSide()
		{
			var scale = JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale;
			return Math.Max(1, (int)Math.Floor(scale)) + (int)Math.Round(4 * scale);
		}

		[TestMethod, TestCategory("interface")]
		[Description("The dark colours are the ones of the WPF dark theme the program carries")]
		public void Dark_colours_come_from_the_wpf_theme()
		{
			var keys = new[]
			{
				"BackgroundWhite", "BackgroundLight", "BackgroundDark", "BorderDark", "BorderGray", "ForegroundBlack",
				"ForegroundGray", "SelectedBackground", "SelectedForeground", "BackgroundHelp", "ColorBrand",
				"ForegroundSuccess", "ForegroundWarning", "ForegroundLink", "BackgroundTabSelected", "ForegroundTab", "ForegroundTabSelected",
				"MouseOverBackground", "BackgroundDarkPressed", "ForegroundCaution", "ForegroundInformation", "ForegroundActive",
				"ForegroundOutput", "ForegroundCode", "ForegroundCommand", "ColorNormal", "ColorActive", "ColorCurve",
				"BackgroundHeader", "BorderField", "BorderCheck", "BorderShadow", "BorderHighlight",
			};
			foreach (var key in keys)
				Assert.IsTrue(FormsTheme.Palette.ContainsKey(key), key + " is not in Default_DarkTheme.xaml.");
			Assert.AreEqual(ColorTranslator.FromHtml("#202020").ToArgb(), FormsTheme.Palette["BackgroundLight"].ToArgb());
		}

		[TestMethod, TestCategory("interface")]
		[Description("Dark turns the system colours dark, and light gives the Windows ones back")]
		public void System_colours_follow_the_theme()
		{
			OnUiThread(() =>
			{
				var window = SystemColors.Window.ToArgb();
				var text = SystemColors.ControlText.ToArgb();
				FormsTheme.SetTheme(ThemeType.Dark);
				Assert.IsTrue(FormsTheme.IsDark);
				Assert.AreEqual(FormsTheme.Palette["BackgroundWhite"].ToArgb(), SystemColors.Window.ToArgb());
				Assert.AreEqual(FormsTheme.Palette["ForegroundBlack"].ToArgb(), SystemColors.ControlText.ToArgb());
				FormsTheme.SetTheme(ThemeType.Light);
				Assert.IsFalse(FormsTheme.IsDark);
				Assert.AreEqual(window, SystemColors.Window.ToArgb(), "The Windows colours did not come back.");
				Assert.AreEqual(text, SystemColors.ControlText.ToArgb(), "The Windows colours did not come back.");
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("Controls take the dark look, and in light they are as they were")]
		public void Controls_go_dark_and_come_back()
		{
			OnUiThread(() =>
			{
				using (var form = new Form())
				{
					// A native control the theme leaves to Windows in the light theme.
					var tree = new TreeView();
					var button = new Button { Text = "Copy prompt", UseVisualStyleBackColor = true };
					var tabs = new TabControl();
					tabs.TabPages.Add("Page");
					form.Controls.AddRange(new Control[] { tree, button, tabs });
					FormsTheme.SetTheme(ThemeType.Dark);
					FormsTheme.Apply(form);
					Assert.IsFalse(tree.BackColor.IsSystemColor, "A native control kept the Windows brush, which paints it light.");
					Assert.AreEqual(FormsTheme.Palette["BackgroundWhite"].ToArgb(), tree.BackColor.ToArgb());
					Assert.AreEqual(FlatStyle.Flat, button.FlatStyle, "A push button kept the light visual style.");
					Assert.AreEqual(0, button.FlatAppearance.BorderSize, "A flat border takes room from the text.");
					Assert.AreEqual(FormsTheme.Palette["BackgroundDark"].ToArgb(), button.BackColor.ToArgb(), "The push button is not filled as the WPF programs fill one.");
					// Shown light again, as the light theme does with the open windows.
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.Apply(form);
					Assert.IsTrue(tree.BackColor.IsSystemColor, "The tree did not get the Windows colour back.");
					Assert.AreEqual(SystemColors.Window, tree.BackColor);
					// The push button keeps the WPF look, in the light colours.
					Assert.AreEqual(FlatStyle.Flat, button.FlatStyle);
					Assert.AreEqual(0, button.FlatAppearance.BorderSize);
					Assert.AreEqual(FormsTheme.LightPalette["BackgroundDark"].ToArgb(), button.BackColor.ToArgb(), "The light fill is not Default.xaml's BackgroundDark.");
				}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("A grid check box is drawn dark in the dark theme and light in the light one, in a grid x360ce sets up as it sets up its own")]
		public void Grid_check_box_goes_dark()
		{
			OnUiThread(() =>
			{
				// Shown, because a grid lays out only the cells it shows; out of sight, so nothing flashes.
				using (var form = new Form { ClientSize = new Size(120, 80), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				{
					// The theme is chosen before any window is made, as the program chooses it.
					FormsTheme.SetTheme(ThemeType.Dark);
					var grid = new DataGridView { Dock = DockStyle.Fill, RowHeadersVisible = false, ColumnHeadersVisible = false, AllowUserToAddRows = false };
					grid.Columns.Add(new DataGridViewCheckBoxColumn());
					grid.Rows.Add(false);
					JocysCom.ClassLibrary.Controls.ControlsHelper.ApplyBorderStyle(grid);
					form.Controls.Add(grid);
					form.Show();
					FormsTheme.Apply(form);
					var dark = CheckBoxCentre(grid);
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.Apply(form);
					var light = CheckBoxCentre(grid);
					Assert.IsTrue(dark.GetBrightness() < 0.5f, "The box is light in the dark theme: " + dark);
					Assert.IsTrue(light.GetBrightness() > 0.5f, "The box is dark in the light theme: " + light);
				}
			});
		}

		/// <summary>
		/// The colour in the middle of the first cell's check box, where Default.xaml's CheckBoxCell puts it: 4 in from the
		/// cell's left, as wide as the grid's text makes it, in the middle of the cell's height.
		/// </summary>
		static Color CheckBoxCentre(DataGridView grid)
		{
			using (var bitmap = new Bitmap(grid.Width, grid.Height))
			{
				grid.DrawToBitmap(bitmap, new Rectangle(Point.Empty, grid.Size));
				var cell = grid.GetCellDisplayRectangle(0, 0, false);
				Assert.IsFalse(cell.IsEmpty, "The grid shows no check box to look at.");
				var scale = JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale;
				return bitmap.GetPixel(cell.X + (int)Math.Round(4 * scale) + CheckBoxWidth(grid.Font) / 2, cell.Y + cell.Height / 2);
			}
		}

		/// <summary>
		/// The width of Default.xaml's check box beside text in a font, in pixels: 16 beside Default.xaml's own text, Segoe UI
		/// at 12, whose line is 15.96 high, and in proportion beside any other.
		/// </summary>
		static int CheckBoxWidth(Font font)
		{
			return (int)Math.Round(16 * font.Height / 15.96);
		}

		[TestMethod, TestCategory("interface")]
		[Description("A grid's check box stands to the grid's text as Default.xaml's does to its own: as big beside WPF's text, smaller beside smaller text")]
		public void Grid_check_box_is_in_proportion_to_its_text()
		{
			OnUiThread(() =>
			{
				foreach (var font in new[] { new Font("Segoe UI", 9f), new Font("Microsoft Sans Serif", 8.25f) })
					using (var form = new Form { ClientSize = new Size(120, 80), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false, Font = font })
					{
						FormsTheme.SetTheme(ThemeType.Light);
						var grid = new DataGridView { Dock = DockStyle.Fill, RowHeadersVisible = false, ColumnHeadersVisible = false, AllowUserToAddRows = false };
						grid.Columns.Add(new DataGridViewCheckBoxColumn());
						grid.Rows.Add(false);
						form.Controls.Add(grid);
						form.Show();
						FormsTheme.Apply(form);
						// No row chosen and no cell in focus, so only the box is drawn on the window colour.
						grid.ClearSelection();
						grid.CurrentCell = null;
						using (var bitmap = new Bitmap(grid.Width, grid.Height))
						{
							grid.DrawToBitmap(bitmap, new Rectangle(Point.Empty, grid.Size));
							var cell = grid.GetCellDisplayRectangle(0, 0, false);
							// The box's edge across the middle of the cell: the first and last mark darker than the window behind it.
							var y = cell.Y + cell.Height / 2;
							var marks = Enumerable.Range(cell.X, cell.Width).Where(x => bitmap.GetPixel(x, y).GetBrightness() < 0.95f).ToList();
							Assert.IsTrue(marks.Count > 0, "No check box is drawn in " + font.Name + ".");
							var width = marks.Last() - marks.First() + 1;
							Assert.IsTrue(Math.Abs(CheckBoxWidth(font) - width) <= 1,
								"The box beside " + font.Name + " is " + width + " wide, not " + CheckBoxWidth(font) + ".");
						}
					}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("A drop-down list in a table stands level with a text box in the same row, as tall as it, as WPF's grid row lines them up")]
		public void Drop_down_list_in_a_table_row_is_level_with_a_text_box()
		{
			OnUiThread(() =>
			{
				using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				{
					var table = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, RowCount = 1 };
					var box = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
					var combo = new ComboBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, DropDownStyle = ComboBoxStyle.DropDownList };
					combo.Items.Add("Virtual");
					table.Controls.Add(box, 0, 0);
					table.Controls.Add(combo, 1, 0);
					form.Controls.Add(table);
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.Apply(form);
					form.Show();
					Assert.AreEqual(box.Height, combo.Height, "The list is not as tall as the text box.");
					Assert.AreEqual(box.Top, combo.Top, "The list stands " + (combo.Top - box.Top) + " lower than the text box beside it.");
				}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("A slider and a progress bar are drawn as Default.xaml has WPF draw them, in the colours of the theme in use")]
		public void Slider_is_drawn_as_wpf_s_in_both_themes()
		{
			OnUiThread(() =>
			{
				// Shown, as the grid test is, so they paint; out of sight, so nothing flashes.
				using (var form = new Form { ClientSize = new Size(220, 90), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				{
					// Out of the tab order, so it shows no focus rectangle along its edges.
					var track = new TrackBar { Dock = DockStyle.Top, Maximum = 100, TickFrequency = 10, TabStop = false };
					var progress = new ProgressBar { Dock = DockStyle.Bottom, Value = 50 };
					form.Controls.AddRange(new Control[] { track, progress });
					form.Show();
					foreach (var theme in new[] { ThemeType.Dark, ThemeType.Light })
					{
						FormsTheme.SetTheme(theme);
						FormsTheme.Apply(form);
						var palette = theme == ThemeType.Dark ? FormsTheme.Palette : FormsTheme.LightPalette;
						var rail = TrackCentre(track);
						Assert.AreEqual(palette["BorderDark"].ToArgb(), rail.ToArgb(), "The slider's track is not Default.xaml's in the " + theme + " theme: " + rail);
						using (var bitmap = new Bitmap(progress.Width, progress.Height))
						{
							progress.DrawToBitmap(bitmap, new Rectangle(Point.Empty, progress.Size));
							var done = bitmap.GetPixel(progress.Width / 4, progress.Height / 2);
							var left = bitmap.GetPixel(progress.Width * 3 / 4, progress.Height / 2);
							Assert.AreEqual(palette["ColorActive"].ToArgb(), done.ToArgb(), "The progress made is not Default.xaml's colour in the " + theme + " theme: " + done);
							Assert.AreEqual(palette["BackgroundDark"].ToArgb(), left.ToArgb(), "The progress bar's track is not Default.xaml's in the " + theme + " theme: " + left);
						}
					}
				}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("A slider that sizes itself is as tall as WPF's thumb and ticks, and shows both whole")]
		public void Slider_is_as_tall_as_its_thumb_and_ticks()
		{
			OnUiThread(() =>
			{
				using (var form = new Form { ClientSize = new Size(300, 120), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				{
					var scale = JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale;
					// Out of the tab order, so it shows no focus rectangle along its edges.
					var track = new TrackBar { Width = 240, Maximum = 100, TickFrequency = 2, TabStop = false };
					form.Controls.Add(track);
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.Apply(form);
					form.Show();
					Assert.AreEqual((int)Math.Round(16 * scale) + (int)Math.Round(8 * scale), track.Height, "The slider is not as tall as its thumb and ticks.");
					using (var bitmap = new Bitmap(track.Width, track.Height))
					{
						track.DrawToBitmap(bitmap, new Rectangle(Point.Empty, track.Size));
						// The thumb at the lowest value: from its top edge, in the edge colour, to its tip.
						var thumb = Runs(bitmap, (int)(12 * scale / 2), track.BackColor);
						var edge = bitmap.GetPixel((int)(12 * scale / 2), thumb[0].X);
						Assert.AreEqual(FormsTheme.LightPalette["BorderGray"].ToArgb(), edge.ToArgb(), "The thumb's top edge is cut: its first row is " + edge + ".");
						// The tick at the far end, where the thumb is not: below the track, and four long.
						var runs = Runs(bitmap, (int)(track.ClientSize.Width - 12 * scale / 2), track.BackColor);
						Assert.AreEqual(2, runs.Count, "The far end shows " + runs.Count + " marks, not the track and its tick.");
						Assert.AreEqual((int)Math.Round(4 * scale), runs[1].Y, "The end tick is cut: " + runs[1].Y + " long, ending at "
							+ (runs[1].X + runs[1].Y) + " of " + track.Height + ".");
					}
				}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("A check box and a radio button are as tall as Default.xaml's box beside their text, as a grid's check box is, where their text is not")]
		public void Check_box_is_as_tall_as_its_box()
		{
			OnUiThread(() =>
			{
				// Text larger than Default.xaml's, so the box is taller than the text's own line.
				using (var form = new Form { ClientSize = new Size(200, 100), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false, Font = new Font("Segoe UI", 12f) })
				{
					var check = new CheckBox { AutoSize = true, Text = "Invert" };
					var radio = new RadioButton { AutoSize = true, Text = "Chosen", Top = 50 };
					form.Controls.AddRange(new Control[] { check, radio });
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.Apply(form);
					form.Show();
					var size = check.Font.Height / 15.96;
					Assert.IsTrue(check.Height >= (int)Math.Round(16 * size), "The check box is " + check.Height + " high, shorter than Default.xaml's box.");
					Assert.IsTrue(radio.Height >= (int)Math.Round(16 * size), "The radio button is " + radio.Height + " high, shorter than Default.xaml's.");
				}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("In the dark theme what is white in the light one, a text box, a check box, a radio button, a grid, a tree and a document, is the field colour, set darker than the page, and a text box at rest is edged in the field edge colour")]
		public void Dark_fields_are_darker_than_the_page()
		{
			OnUiThread(() =>
			{
				// Shown, so each draws as on the screen; out of sight, so nothing flashes; nothing takes the focus or the mouse.
				using (var form = new Form { ClientSize = new Size(420, 240), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false, Font = new Font("Segoe UI", 9f) })
				{
					FormsTheme.SetTheme(ThemeType.Dark);
					var box = new TextBox { Location = new Point(10, 10), Width = 120, TabStop = false };
					var check = new CheckBox { Text = "Exclude", AutoSize = true, Location = new Point(10, 50), TabStop = false };
					var radio = new RadioButton { Text = "Chosen", AutoSize = true, Location = new Point(10, 80), TabStop = false };
					// A document in the window colour, as the program's help and about documents are: read-only, it would otherwise take the page's.
					var document = new RichTextBox { Location = new Point(150, 10), Size = new Size(120, 60), BorderStyle = BorderStyle.None, ReadOnly = true, TabStop = false, BackColor = SystemColors.Window };
					var tree = new TreeView { Location = new Point(290, 10), Size = new Size(120, 60), TabStop = false };
					var grid = new DataGridView { Location = new Point(150, 110), Size = new Size(260, 120), RowHeadersVisible = false, AllowUserToAddRows = false, TabStop = false };
					grid.Columns.Add("Name", "Name");
					grid.Rows.Add("");
					form.Controls.AddRange(new Control[] { box, check, radio, document, tree, grid });
					FormsTheme.Apply(form);
					form.Show();
					form.ActiveControl = null;
					grid.ClearSelection();
					grid.CurrentCell = null;
					form.Refresh();
					Application.DoEvents();
					var field = FormsTheme.Palette["BackgroundWhite"];
					var page = FormsTheme.Palette["BackgroundLight"];
					var problems = new System.Collections.Generic.List<string>();
					if (field.GetBrightness() >= page.GetBrightness())
						problems.Add("The field colour " + Hex(field) + " is not darker than the page " + Hex(page) + ".");
					using (var picture = WindowPicture(form.Handle))
					{
						var boxSize = CheckBoxBox(check);
						var cell = grid.GetCellDisplayRectangle(0, 0, false);
						var below = grid.GetRowDisplayRectangle(0, false).Bottom;
						var places = new[]
						{
							Tuple.Create("text box", (Control)box, new Point(box.ClientSize.Width / 2, box.ClientSize.Height / 2)),
							Tuple.Create("check box", (Control)check, new Point(boxSize.Width / 2, check.Height / 2)),
							Tuple.Create("radio button", (Control)radio, new Point(boxSize.Width / 2, radio.Height / 2)),
							Tuple.Create("document", (Control)document, new Point(document.Width / 2, document.Height / 2)),
							Tuple.Create("tree", (Control)tree, new Point(tree.ClientSize.Width / 2, tree.ClientSize.Height / 2)),
							Tuple.Create("grid's cell", (Control)grid, new Point(cell.Right - 4, cell.Top + cell.Height / 2)),
							Tuple.Create("grid below its rows", (Control)grid, new Point(grid.Width / 2, (below + grid.Height) / 2)),
						};
						foreach (var place in places)
						{
							var colour = PixelAt(picture, form, place.Item2, place.Item3);
							if (colour.ToArgb() != field.ToArgb())
								problems.Add(string.Format("The {0} is {1}, not the field colour {2}.", place.Item1, Hex(colour), Hex(field)));
						}
						// The frame's line, at the box's left edge halfway down, which the window's edge holds.
						var edge = PixelAt(picture, form, box, new Point(-(box.Width - box.ClientSize.Width) / 2, box.ClientSize.Height / 2));
						if (edge.ToArgb() != FormsTheme.Palette["BorderField"].ToArgb())
							problems.Add(string.Format("The text box's edge is {0}, not the field edge colour {1}.", Hex(edge), Hex(FormsTheme.Palette["BorderField"])));
						// The headers keep their shading, from the header colour down to the page's.
						var header = PixelAt(picture, form, grid, new Point(cell.Left + 1, 0));
						Color headerColour;
						if (!FormsTheme.Palette.TryGetValue("BackgroundHeader", out headerColour))
							problems.Add("The theme has no header colour.");
						else if (Distance(header, headerColour) > 8)
							problems.Add("The grid's header starts at " + Hex(header) + ", not the header colour " + Hex(headerColour) + ".");
					}
					Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
				}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("The dark edge of a box to type in at rest, which a ticked check box shares, is the light theme's edge with its lightness inverted")]
		public void Dark_field_edge_is_the_light_edge_with_its_lightness_inverted()
		{
			var light = FormsTheme.LightPalette["BorderField"];
			var dark = FormsTheme.Palette["BorderField"];
			// The light edge is a grey, whose lightness inverted is each channel inverted.
			var inverted = Color.FromArgb(255 - light.R, 255 - light.G, 255 - light.B);
			Assert.AreEqual(Hex(inverted), Hex(dark), "The dark field edge is not the light " + Hex(light) + " with its lightness inverted.");
		}

		[TestMethod, TestCategory("interface")]
		[Description("In the dark theme a check box that is not ticked has the field colour for its edge, so it shows no edge at rest; in the light theme it keeps the field edge")]
		public void Dark_check_box_edge_at_rest_is_the_field_colour()
		{
			Assert.AreEqual(Hex(FormsTheme.Palette["BackgroundWhite"]), Hex(FormsTheme.Palette["BorderCheck"]), "The dark check box edge is not the field colour.");
			Assert.AreEqual(Hex(FormsTheme.LightPalette["BorderField"]), Hex(FormsTheme.LightPalette["BorderCheck"]), "The light check box edge is not the field edge.");
		}

		[TestMethod, TestCategory("interface")]
		[Description("A check box's and a radio button's edge is a text box's: at rest the field colour while not ticked in the dark theme, the field edge while ticked, and the selection colour with the focus, in both themes")]
		public void Check_box_edge_follows_a_text_box_s()
		{
			OnUiThread(() =>
			{
				// Out of sight, so nothing flashes and the mouse is never over it; the focus is given in code.
				using (var form = new Form { ClientSize = new Size(300, 160), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false, Font = new Font("Segoe UI", 9f) })
				{
					var check = new CheckBox { Text = "Exclude", AutoSize = true, Location = new Point(10, 10) };
					var radio = new RadioButton { Text = "Chosen", AutoSize = true, Location = new Point(10, 50) };
					var other = new TextBox { Location = new Point(10, 100) };
					form.Controls.AddRange(new Control[] { check, radio, other });
					form.Show();
					var problems = new System.Collections.Generic.List<string>();
					foreach (var theme in new[] { ThemeType.Dark, ThemeType.Light })
					{
						FormsTheme.SetTheme(theme);
						FormsTheme.Apply(form);
						var palette = theme == ThemeType.Dark ? FormsTheme.Palette : FormsTheme.LightPalette;
						var fill = palette["BackgroundWhite"];
						var rest = theme == ThemeType.Dark ? fill : palette["BorderField"];
						foreach (var button in new ButtonBase[] { check, radio })
						{
							var which = theme + ", " + button.GetType().Name;
							SetTicked(button, false);
							other.Focus();
							CheckEdge(button, rest, which + ", not ticked, at rest", problems);
							if (theme == ThemeType.Dark)
								CheckEdge(button, BoxCentre(button), which + ", not ticked, at rest, against its own fill", problems);
							SetTicked(button, true);
							CheckEdge(button, palette["BorderField"], which + ", ticked, at rest", problems);
							button.Focus();
							if (!button.Focused)
								problems.Add(which + " did not take the focus.");
							CheckEdge(button, palette["BackgroundDarkSelected"], which + ", ticked, with the focus", problems);
							SetTicked(button, false);
							CheckEdge(button, palette["BackgroundDarkSelected"], which + ", not ticked, with the focus", problems);
						}
					}
					Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
				}
			});
		}

		/// <summary>Ticks a check box, or chooses a radio button, or clears it.</summary>
		static void SetTicked(ButtonBase button, bool ticked)
		{
			if (button is CheckBox check)
				check.Checked = ticked;
			else
				((RadioButton)button).Checked = ticked;
		}

		/// <summary>The colour in the middle of the box the theme draws a check box or radio button with.</summary>
		static Color BoxCentre(ButtonBase button)
		{
			using (var bitmap = new Bitmap(button.Width, button.Height))
			{
				button.DrawToBitmap(bitmap, new Rectangle(Point.Empty, button.Size));
				return bitmap.GetPixel(CheckBoxBox(button).Width / 2, button.Height / 2);
			}
		}

		/// <summary>
		/// Checks the edge of the box the theme draws a check box or radio button with: its right side halfway down, clear of
		/// the focus mark round the control and of the tick. A radio button's round edge, smoothed, is matched within a few
		/// shades: at 100 % its 16-pixel circle covers that pixel a little less than whole.
		/// </summary>
		static void CheckEdge(ButtonBase button, Color expected, string which, System.Collections.Generic.List<string> problems)
		{
			using (var bitmap = new Bitmap(button.Width, button.Height))
			{
				button.DrawToBitmap(bitmap, new Rectangle(Point.Empty, button.Size));
				var edge = bitmap.GetPixel(CheckBoxBox(button).Width - 1, button.Height / 2);
				var allowed = button is RadioButton ? 6 : 0;
				if (Distance(edge, expected) > allowed)
					problems.Add(string.Format("{0}: the edge is {1}, not {2}.", which, Hex(edge), Hex(expected)));
			}
		}

		[TestMethod, TestCategory("interface")]
		[Description("A grid's check box has a text box's edge too: the selection colour in the current cell of the grid with the focus, the field edge while ticked and, in the dark theme, the field colour while not")]
		public void Grid_check_box_edge_follows_a_text_box_s()
		{
			OnUiThread(() =>
			{
				using (var form = new Form { ClientSize = new Size(200, 200), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false, Font = new Font("Segoe UI", 9f) })
				{
					var grid = new DataGridView { Dock = DockStyle.Fill, RowHeadersVisible = false, AllowUserToAddRows = false };
					grid.Columns.Add(new DataGridViewCheckBoxColumn());
					grid.Rows.Add(false);
					grid.Rows.Add(true);
					grid.Rows.Add(false);
					var other = new TextBox { Dock = DockStyle.Bottom };
					form.Controls.AddRange(new Control[] { grid, other });
					form.Show();
					var problems = new System.Collections.Generic.List<string>();
					foreach (var theme in new[] { ThemeType.Dark, ThemeType.Light })
					{
						FormsTheme.SetTheme(theme);
						FormsTheme.Apply(form);
						var palette = theme == ThemeType.Dark ? FormsTheme.Palette : FormsTheme.LightPalette;
						grid.ClearSelection();
						grid.CurrentCell = grid[0, 0];
						grid.ClearSelection();
						grid.Focus();
						if (!grid.Focused)
							problems.Add(theme + ": the grid did not take the focus.");
						using (var bitmap = new Bitmap(grid.Width, grid.Height))
						{
							grid.DrawToBitmap(bitmap, new Rectangle(Point.Empty, grid.Size));
							// The ticked box's left edge, the first mark from the left across the middle of its cell.
							var ticked = grid.GetCellDisplayRectangle(0, 1, false);
							var y = ticked.Y + ticked.Height / 2;
							var behind = bitmap.GetPixel(ticked.X + 1, y);
							var x = Enumerable.Range(ticked.X + 1, ticked.Width - 2).FirstOrDefault(i => bitmap.GetPixel(i, y).ToArgb() != behind.ToArgb());
							if (x == 0)
							{
								problems.Add(theme + ": no ticked box is drawn.");
								continue;
							}
							var expected = new[]
							{
								Tuple.Create(1, palette["BorderField"], "the ticked box at rest"),
								Tuple.Create(0, palette["BackgroundDarkSelected"], "the box in the current cell of the grid with the focus"),
								Tuple.Create(2, theme == ThemeType.Dark ? palette["BackgroundWhite"] : palette["BorderField"], "the box not ticked, at rest"),
							};
							foreach (var place in expected)
							{
								var cell = grid.GetCellDisplayRectangle(0, place.Item1, false);
								var edge = bitmap.GetPixel(x, cell.Y + cell.Height / 2);
								if (edge.ToArgb() != place.Item2.ToArgb())
									problems.Add(string.Format("{0}: {1} is edged in {2}, not {3}.", theme, place.Item3, Hex(edge), Hex(place.Item2)));
							}
						}
						other.Focus();
					}
					Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
				}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("A check box, a radio button and a grid's check box are drawn again as they take or lose the focus, and a check box as it is ticked, so the edge is never left as it was")]
		public void Check_box_is_drawn_again_as_its_edge_changes()
		{
			OnUiThread(() =>
			{
				using (var form = new Form { ClientSize = new Size(300, 200), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false, Font = new Font("Segoe UI", 9f) })
				{
					var check = new CheckBox { Text = "Exclude", AutoSize = true, Location = new Point(10, 10) };
					var radio = new RadioButton { Text = "Chosen", AutoSize = true, Location = new Point(10, 40) };
					var grid = new DataGridView { Location = new Point(10, 70), Size = new Size(200, 80), RowHeadersVisible = false, AllowUserToAddRows = false };
					grid.Columns.Add(new DataGridViewCheckBoxColumn());
					grid.Rows.Add(false);
					grid.Rows.Add(false);
					var other = new TextBox { Location = new Point(10, 160) };
					form.Controls.AddRange(new Control[] { check, radio, grid, other });
					FormsTheme.SetTheme(ThemeType.Dark);
					FormsTheme.Apply(form);
					form.Show();
					grid.CurrentCell = grid[0, 0];
					other.Focus();
					Application.DoEvents();
					// Out of sight a window is not painted, so what is counted is the asking: the box's place made invalid.
					var invalidated = 0;
					InvalidateEventHandler count = (s, e) => invalidated++;
					InvalidateEventHandler cell = (s, e) => { if (e.InvalidRect.IntersectsWith(grid.GetCellDisplayRectangle(0, 0, false))) invalidated++; };
					check.Invalidated += count;
					radio.Invalidated += count;
					grid.Invalidated += cell;
					var problems = new System.Collections.Generic.List<string>();
					Action<string, Action> drawnAgain = (which, change) =>
					{
						invalidated = 0;
						change();
						Application.DoEvents();
						if (invalidated == 0)
							problems.Add(which + " was not drawn again.");
					};
					drawnAgain("The check box taking the focus", () => check.Focus());
					drawnAgain("The check box ticked", () => check.Checked = true);
					drawnAgain("The check box losing the focus", () => other.Focus());
					// Chosen first: a radio button the focus comes to by code is chosen as it takes it.
					drawnAgain("The radio button chosen", () => radio.Checked = true);
					drawnAgain("The radio button taking the focus", () => radio.Focus());
					drawnAgain("The radio button losing the focus", () => other.Focus());
					drawnAgain("The grid's current check box taking the focus", () => grid.Focus());
					drawnAgain("The grid's check box the current cell moves from", () => grid.CurrentCell = grid[0, 1]);
					drawnAgain("The grid's check box the current cell moves to", () => grid.CurrentCell = grid[0, 0]);
					drawnAgain("The grid's current check box losing the focus", () => other.Focus());
					Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
				}
			});
		}
		[TestMethod, TestCategory("interface")]
		[Description("A check box's and a radio button's text stands Default.xaml's room from the box, 4 device-independent pixels, scaled with the screen, in both themes, with the box where it was and the text whole in the width the control sizes itself to")]
		public void Check_box_text_stands_its_room_from_the_box()
		{
			OnUiThread(() =>
			{
				using (var form = new Form { ClientSize = new Size(300, 80), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false, Font = new Font("Segoe UI", 9f) })
				{
					const string text = "Exclude Virtual Devices";
					var check = new CheckBox { Text = text, AutoSize = true, Location = new Point(10, 10), TabStop = false };
					var radio = new RadioButton { Text = text, AutoSize = true, Location = new Point(10, 40), TabStop = false };
					form.Controls.AddRange(new Control[] { check, radio });
					form.Show();
					var room = (int)Math.Round(4 * JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale);
					var problems = new System.Collections.Generic.List<string>();
					foreach (var theme in new[] { ThemeType.Light, ThemeType.Dark })
					{
						FormsTheme.SetTheme(theme);
						FormsTheme.Apply(form);
						foreach (var button in new ButtonBase[] { check, radio })
							using (var bitmap = new Bitmap(button.Width, button.Height))
							{
								button.DrawToBitmap(bitmap, new Rectangle(Point.Empty, button.Size));
								var behind = bitmap.GetPixel(button.Width - 1, 0);
								// The columns with anything drawn in them: the box's, then the text's.
								var drawn = DrawnColumns(bitmap, behind);
								var which = theme + ", " + button.GetType().Name;
								if (drawn.Count < 2)
								{
									problems.Add(which + ": no box and text were found.");
									continue;
								}
								var box = drawn[0];
								var size = CheckBoxBox(button);
								if (box.X != 0 || box.Y != size.Width)
									problems.Add(string.Format("{0}: the box is drawn at {1}, {2} wide, not at 0, {3} wide.", which, box.X, box.Y, size.Width));
								// The room before the text, less the room the first letter leaves before its ink.
								var gap = drawn[1].X - (box.X + box.Y) - InkStart(text, button.Font, button.ForeColor, behind);
								if (gap != room)
									problems.Add(string.Format("{0}: the text stands {1} px from the box, not {2}.", which, gap, room));
								// The text's layout box as it is drawn, measured on the picture's device context, from where its first
								// letter's ink starts less the room that letter leaves.
								int width;
								using (var g = Graphics.FromImage(bitmap))
									width = TextRenderer.MeasureText(g, text, button.Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
								if (drawn[1].X - InkStart(text, button.Font, button.ForeColor, behind) + width > button.ClientSize.Width)
									problems.Add(string.Format("{0}: the text, {1} wide from {2}, does not fit the {3} the control sized itself to.", which, width, drawn[1].X, button.ClientSize.Width));
							}
					}
					Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
				}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("Each folder in Options > General > Game Scan Locations stands with 4 device-independent pixels of room round its name, scaled with the screen, in both themes, as a framed list's items do with Default.xaml's 4")]
		public void Game_scan_locations_have_room_round_each_folder()
		{
			OnUiThread(() =>
			{
				using (var form = new Form { ClientSize = new Size(900, 700), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false, Font = new Font("Segoe UI", 9f) })
				{
					var page = new OptionsUserControl { Dock = DockStyle.Fill };
					var framed = new ListBox { BorderStyle = BorderStyle.FixedSingle, Dock = DockStyle.Bottom, Height = 80 };
					framed.Items.Add("Framed");
					form.Controls.AddRange(new Control[] { page, framed });
					form.Show();
					var list = page.GameScanLocationsListBox;
					if (list.Items.Count == 0)
						list.Items.AddRange(new object[] { @"C:\Program Files (x86)\Steam\steamapps\common", @"C:\Program Files\Epic Games", @"D:\Games" });
					var dpi = JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale;
					var room = (int)Math.Round(4 * dpi);
					var problems = new System.Collections.Generic.List<string>();
					foreach (var theme in new[] { ThemeType.Light, ThemeType.Dark })
					{
						FormsTheme.SetTheme(theme);
						FormsTheme.Apply(form);
						// Nothing chosen and the focus elsewhere, so the first folder is drawn on the list's colour without a focus mark.
						list.ClearSelected();
						framed.Focus();
						var item = list.GetItemRectangle(0);
						if (item.Height != list.Font.Height + 2 * room)
							problems.Add(string.Format("{0}: a folder is {1} px high, not its text's {2} and {3} above and below.", theme, item.Height, list.Font.Height, room));
						// Default.xaml's 4 above and below, 8 in all, in whole pixels.
						var framedHeight = framed.Font.Height + (int)Math.Round(8 * dpi);
						if (framed.ItemHeight != framedHeight)
							problems.Add(string.Format("{0}: a framed list's item is {1} px high, not Default.xaml's {2}.", theme, framed.ItemHeight, framedHeight));
						using (var bitmap = new Bitmap(list.Width, list.Height))
						{
							list.DrawToBitmap(bitmap, new Rectangle(Point.Empty, list.Size));
							using (var row = bitmap.Clone(item, bitmap.PixelFormat))
							{
								var behind = row.GetPixel(row.Width - 1, 0);
								if (behind.ToArgb() != list.BackColor.ToArgb())
									problems.Add(string.Format("{0}: a folder is drawn on {1}, not the list's {2}.", theme, Hex(behind), Hex(list.BackColor)));
								var drawn = DrawnColumns(row, behind);
								var text = (string)list.Items[0];
								var inset = drawn.Count == 0 ? -1 : drawn[0].X - InkStart(text, list.Font, list.ForeColor, behind);
								if (inset != room)
									problems.Add(string.Format("{0}: a folder's name stands {1} px in from the side, not {2}.", theme, inset, room));
							}
						}
					}
					Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
				}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("A group's frame is sunken in both themes, as Windows' etched edge: its line darker than the line a pixel right of it and below it")]
		public void Group_frame_is_sunken_in_both_themes()
		{
			OnUiThread(() =>
			{
				using (var form = new Form { ClientSize = new Size(260, 120), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false, Font = new Font("Segoe UI", 9f) })
				{
					var group = new GroupBox { Text = "Guide Button", Location = new Point(10, 10), Size = new Size(200, 90) };
					group.Controls.Add(new Panel { Dock = DockStyle.Fill });
					form.Controls.Add(group);
					form.Show();
					var problems = new System.Collections.Generic.List<string>();
					foreach (var theme in new[] { ThemeType.Light, ThemeType.Dark })
					{
						FormsTheme.SetTheme(theme);
						FormsTheme.Apply(form);
						using (var bitmap = new Bitmap(group.Width, group.Height))
						{
							group.DrawToBitmap(bitmap, new Rectangle(Point.Empty, group.Size));
							// Halfway down the left side, and halfway along the bottom: the frame's line, then the line a pixel right
							// of it or below it.
							var sides = new[]
							{
								Tuple.Create("left", bitmap.GetPixel(0, group.Height / 2), bitmap.GetPixel(1, group.Height / 2)),
								Tuple.Create("bottom", bitmap.GetPixel(group.Width / 2, group.Height - 2), bitmap.GetPixel(group.Width / 2, group.Height - 1)),
							};
							foreach (var side in sides)
								if (side.Item2.GetBrightness() >= side.Item3.GetBrightness())
									problems.Add(string.Format("{0}: on the {1} side the frame's line {2} is not darker than the line beside it {3}: the frame stands out.",
										theme, side.Item1, Hex(side.Item2), Hex(side.Item3)));
						}
					}
					Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
				}
			});
		}

		/// <summary>The size of the box the theme draws a check box or radio button with: Default.xaml's, beside the control's text.</summary>
		static Size CheckBoxBox(ButtonBase button)
		{
			var scale = button.Font.Height / (15.96 * JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale);
			var dip = new SizeF(16f, 16f);
			var dpi = JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale;
			return new Size((int)Math.Round(dip.Width * scale * dpi), (int)Math.Round(dip.Height * scale * dpi));
		}

		/// <summary>How far right of where it is drawn a text's ink starts, drawn as a check box draws it, without padding.</summary>
		static int InkStart(string text, Font font, Color fore, Color back)
		{
			var size = TextRenderer.MeasureText(text, font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
			using (var bitmap = new Bitmap(size.Width + 4, size.Height))
			{
				using (var g = Graphics.FromImage(bitmap))
				{
					g.Clear(back);
					TextRenderer.DrawText(g, text, font, Point.Empty, fore, back, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
				}
				var drawn = DrawnColumns(bitmap, back);
				return drawn.Count > 0 ? drawn[0].X : 0;
			}
		}

		/// <summary>The columns of a picture with anything but the colour given in them, as runs of first column and width, from the left.</summary>
		static System.Collections.Generic.List<Point> DrawnColumns(Bitmap bitmap, Color behind)
		{
			var runs = new System.Collections.Generic.List<Point>();
			Func<int, bool> drawn = x => Enumerable.Range(0, bitmap.Height).Any(y => bitmap.GetPixel(x, y).ToArgb() != behind.ToArgb());
			for (var x = 0; x < bitmap.Width; x++)
			{
				if (!drawn(x))
					continue;
				var start = x;
				while (x < bitmap.Width && drawn(x))
					x++;
				runs.Add(new Point(start, x - start));
			}
			return runs;
		}

		/// <summary>The colour of a point of a control, in its client's coordinates, in a picture of the window it is on.</summary>
		static Color PixelAt(Bitmap picture, Form form, Control control, Point point)
		{
			RECT window;
			GetWindowRect(form.Handle, out window);
			var p = control.PointToScreen(point);
			return picture.GetPixel(p.X - window.Left, p.Y - window.Top);
		}

		[TestMethod, TestCategory("interface")]
		[Description("A drop-down list painted as the WPF programs paint one has its font, and is as tall as a push button")]
		public void Drop_down_list_has_its_font_and_a_button_s_height()
		{
			OnUiThread(() =>
			{
				// Themed before its window is made, as a page is that is shown later.
				using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				{
					var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
					combo.Items.Add("Gamepad");
					// Sized by itself, as the theme sizes a push button to Default.xaml's height. Its text measured with GDI, as the
					// programs draw it (Application.SetCompatibleTextRenderingDefault(false)), which the test host cannot ask for once a
					// window exists: GDI+ measures Microsoft Sans Serif a row taller at 100 %, past the height a list has.
					var button = new Button { Top = 40, Text = "Apply", AutoSize = true, UseCompatibleTextRendering = false };
					form.Controls.AddRange(new Control[] { combo, button });
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.Apply(form);
					form.Show();
					const int WM_GETFONT = 0x0031;
					const int CB_GETITEMHEIGHT = 0x0154;
					Assert.AreNotEqual(IntPtr.Zero, SendMessage(combo.Handle, WM_GETFONT, IntPtr.Zero, IntPtr.Zero),
						"The list has no font, so Windows draws it in the system's bitmap font.");
					var field = (int)SendMessage(combo.Handle, CB_GETITEMHEIGHT, (IntPtr)(-1), IntPtr.Zero);
					Assert.AreEqual(button.Height, combo.Height, "The list is not as tall as a button; its field is " + field + " of " + combo.Height + ".");
				}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("A number box is as tall as Default.xaml's, and a layout makes room for all of it")]
		public void Number_box_is_as_tall_as_default_xaml_s()
		{
			OnUiThread(() =>
			{
				// Themed before its window is made, as the gallery and the program's pages are.
				using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				{
					var panel = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
					var box = new NumericUpDown();
					panel.Controls.Add(box);
					form.Controls.Add(panel);
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.Apply(form);
					form.Show();
					var height = box.Font.Height + FrameSide() * 2;
					Assert.AreEqual(height, box.Height, "The number box is not as tall as Default.xaml's.");
					Assert.AreEqual(box.Margin.Vertical + height, panel.Height, "The layout left no room for the whole number box.");
					// Windows Forms repaints its buttons as the mouse moves over them; drawn off the screen, they do not flicker.
					var buttons = box.Controls[0];
					var getStyle = typeof(Control).GetMethod("GetStyle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
					Assert.IsTrue((bool)getStyle.Invoke(buttons, new object[] { ControlStyles.OptimizedDoubleBuffer }), "The buttons are drawn on the screen, so they flicker.");
				}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("A tab strip drawn as the WPF programs draw one starts at the page's edge, and a click reaches the tab drawn under it")]
		public void Tab_click_reaches_the_tab_drawn_under_it()
		{
			OnUiThread(() =>
			{
				using (var form = new Form { ClientSize = new Size(400, 200), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				{
					var tabs = new TabControl { Dock = DockStyle.Fill };
					tabs.TabPages.Add("General");
					tabs.TabPages.Add("Buttons");
					form.Controls.Add(tabs);
					form.Show();
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.Apply(form);
					var first = tabs.GetTabRect(0);
					var second = tabs.GetTabRect(1);
					var y = first.Y + first.Height / 2;
					using (var bitmap = new Bitmap(tabs.Width, tabs.Height))
					{
						tabs.DrawToBitmap(bitmap, new Rectangle(Point.Empty, tabs.Size));
						var edge = bitmap.GetPixel(0, y);
						Assert.IsTrue(edge.GetBrightness() < form.BackColor.GetBrightness(), "The first tab does not start at the page's edge: " + edge);
					}
					// Drawn as far in from where Windows has it as Windows keeps the first tab from the edge.
					Click(tabs.Handle, second.X - first.X + 1, y);
					Assert.AreEqual(1, tabs.SelectedIndex, "A click on the tab drawn there chose another.");
				}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("A tab strip drawn as the WPF programs draw one holds its pages edge to edge inside the frame round them, rounded at the frame's rounded corners, so a text filling a page reaches the frame at any size; a strip Windows draws keeps its pages where Windows has them; in both themes")]
		public void Tab_pages_fill_the_frame_drawn_round_them()
		{
			OnUiThread(() =>
			{
				var line = Math.Max(1, (int)Math.Floor(JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale));
				// The light theme draws a strip the program draws itself as Windows does, and the dark theme draws every strip.
				foreach (var drawMode in new[] { TabDrawMode.Normal, TabDrawMode.OwnerDrawFixed })
					using (var form = new Form { ClientSize = new Size(400, 200), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
					{
						var tabs = new TabControl { Dock = DockStyle.Fill, DrawMode = drawMode };
						tabs.TabPages.Add("Program");
						tabs.TabPages.Add("License");
						var page = tabs.TabPages[0];
						var text = new RichTextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, Text = "x360ce" };
						page.Controls.Add(text);
						form.Controls.Add(tabs);
						form.Show();
						foreach (var theme in new[] { ThemeType.Light, ThemeType.Dark, ThemeType.Light })
						{
							FormsTheme.SetTheme(theme);
							FormsTheme.Apply(form);
							foreach (var size in new[] { new Size(400, 200), new Size(520, 260) })
							{
								form.ClientSize = size;
								Application.DoEvents();
								var name = string.Format("{0} {1} {2}", drawMode, theme, size);
								var painted = drawMode == TabDrawMode.Normal || theme == ThemeType.Dark;
								if (!painted)
								{
									Assert.IsNull(page.Region, name + ": a page of a strip Windows draws keeps its own shape.");
									Assert.IsTrue(page.Left > line, name + ": a page of a strip Windows draws stands where Windows has it.");
									continue;
								}
								var bottom = Enumerable.Range(0, tabs.TabCount).Max(i => tabs.GetTabRect(i).Bottom);
								var inside = new Rectangle(line, bottom + line, tabs.ClientSize.Width - line * 2, tabs.ClientSize.Height - bottom - line * 2);
								Assert.AreEqual(inside, page.Bounds, name + ": the page does not fill the frame round it.");
								Assert.AreEqual(page.ClientRectangle, text.Bounds, name + ": the text does not fill its page.");
								var shape = page.Region;
								Assert.IsNotNull(shape, name + ": the page covers the frame's rounded corners.");
								var last = new Point(page.Width - 1, page.Height - 1);
								Assert.IsTrue(shape.IsVisible(0, 0), name + ": the corner under the first tab, which the frame leaves square, is cut.");
								Assert.IsTrue(shape.IsVisible(last.X / 2, last.Y / 2), name + ": the middle of the page is cut.");
								foreach (var corner in new[] { new Point(last.X, 0), last, new Point(0, last.Y) })
									Assert.IsFalse(shape.IsVisible(corner), name + ": the page covers the frame's rounded corner at " + corner + ".");
							}
						}
					}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("A slider's thumb, drawn where WPF's stands, follows the mouse from one end to the other")]
		public void Slider_thumb_drawn_as_wpf_s_follows_the_mouse()
		{
			OnUiThread(() =>
			{
				using (var form = new Form { ClientSize = new Size(300, 60), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				{
					var track = new TrackBar { Dock = DockStyle.Top, Maximum = 100 };
					form.Controls.Add(track);
					form.Show();
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.Apply(form);
					// WPF's thumb, 12 wide, travels from half its width in from one end to as far from the other.
					var first = 12 * JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale / 2;
					var last = track.ClientSize.Width - first;
					const int TBM_GETTHUMBRECT = 0x0419;
					var thumb = new RECT();
					SendMessage(track.Handle, TBM_GETTHUMBRECT, IntPtr.Zero, ref thumb);
					var y = (thumb.Top + thumb.Bottom) / 2;
					SendMessage(track.Handle, WM_LBUTTONDOWN, (IntPtr)MK_LBUTTON, MousePlace((int)Math.Round(first), y));
					SendMessage(track.Handle, WM_MOUSEMOVE, (IntPtr)MK_LBUTTON, MousePlace((int)Math.Round(first + (last - first) / 4), y));
					var quarter = track.Value;
					SendMessage(track.Handle, WM_MOUSEMOVE, (IntPtr)MK_LBUTTON, MousePlace((int)Math.Round(last), y));
					SendMessage(track.Handle, WM_LBUTTONUP, IntPtr.Zero, MousePlace((int)Math.Round(last), y));
					Assert.AreEqual(25, quarter, "A quarter of the way along, the thumb is not at a quarter of the range.");
					Assert.AreEqual(track.Maximum, track.Value, "At the end, the thumb is not at the end of the range.");
				}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("A slider without ticks shows none, its thumb is a rectangle edged one line wide, and the slider with the focus has it in the selection colours, in both themes")]
		public void Slider_thumb_shows_the_focus_in_the_selection_colours()
		{
			OnUiThread(() =>
			{
				using (var form = new Form { ClientSize = new Size(300, 120), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				{
					var focused = new TrackBar { Width = 240, Maximum = 100, TickStyle = TickStyle.None };
					var other = new TrackBar { Width = 240, Maximum = 100, TickStyle = TickStyle.None, Top = 60 };
					form.Controls.AddRange(new Control[] { focused, other });
					form.Show();
					foreach (var theme in new[] { ThemeType.Dark, ThemeType.Light })
					{
						FormsTheme.SetTheme(theme);
						FormsTheme.Apply(form);
						focused.Focus();
						Assert.IsTrue(focused.Focused, "The slider did not take the focus.");
						var palette = theme == ThemeType.Dark ? FormsTheme.Palette : FormsTheme.LightPalette;
						AssertThumb(focused, palette["SelectedBackground"], palette["BackgroundDarkSelected"], "In the " + theme + " theme, the focused slider");
						AssertThumb(other, palette["BorderGray"], palette["BackgroundLight"], "In the " + theme + " theme, the slider without the focus");
					}
				}
			});
		}

		/// <summary>
		/// Checks the thumb of a slider at its lowest value, down the middle of its width: as tall as WPF's, its edge one line
		/// wide in the colour given and filled in the other, which may be the slider's own; and the slider's far end, where the
		/// track alone is drawn.
		/// </summary>
		static void AssertThumb(TrackBar track, Color edge, Color fill, string which)
		{
			var scale = JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale;
			var line = Math.Max(1, (int)Math.Floor(scale));
			using (var bitmap = new Bitmap(track.Width, track.Height))
			{
				track.DrawToBitmap(bitmap, new Rectangle(Point.Empty, track.Size));
				var x = (int)(12 * scale / 2);
				var edges = Enumerable.Range(0, bitmap.Height).Where(y => bitmap.GetPixel(x, y).ToArgb() == edge.ToArgb()).ToList();
				Assert.IsTrue(edges.Count > 1, which + " has no thumb edged in " + edge + ".");
				var top = edges.First();
				var bottom = edges.Last();
				Assert.AreEqual((int)Math.Round(16 * scale), bottom - top + 1, which + " has a thumb " + (bottom - top + 1) + " high.");
				var runs = Runs(bitmap, x, track.BackColor);
				Assert.IsTrue(runs.All(r => r.X >= top && r.X + r.Y - 1 <= bottom), which + " shows marks beside its thumb: it has ticks.");
				Assert.AreEqual(fill.ToArgb(), bitmap.GetPixel(x, top + line).ToArgb(), which + " has an edge wider than one line, or its thumb filled in " + bitmap.GetPixel(x, top + line) + ".");
				Assert.AreEqual(fill.ToArgb(), bitmap.GetPixel(x, (top + bottom) / 2).ToArgb(), which + " has its thumb filled in " + bitmap.GetPixel(x, (top + bottom) / 2) + ".");
				var far = Runs(bitmap, (int)(track.ClientSize.Width - 12 * scale / 2), track.BackColor);
				Assert.AreEqual(1, far.Count, which + " shows " + far.Count + " marks at its far end, not the track alone: it has ticks.");
			}
		}

		[TestMethod, TestCategory("interface")]
		[Description("A slider's thumb is whole at its lowest and at its highest value, at every width, with ticks and without, in both themes")]
		public void Slider_thumb_is_whole_at_both_ends()
		{
			OnUiThread(() =>
			{
				using (var form = new Form { ClientSize = new Size(300, 120), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				{
					// Out of the tab order, so neither takes the focus and shows it in the thumb's edge.
					var plain = new TrackBar { Maximum = 100, TickStyle = TickStyle.None, TabStop = false };
					var ticked = new TrackBar { Maximum = 100, TickFrequency = 10, TabStop = false, Top = 60 };
					form.Controls.AddRange(new Control[] { plain, ticked });
					form.Show();
					var scale = JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale;
					var width = (int)Math.Round(12 * scale);
					var length = (int)Math.Round(16 * scale);
					var problems = new System.Collections.Generic.List<string>();
					foreach (var theme in new[] { ThemeType.Dark, ThemeType.Light })
					{
						FormsTheme.SetTheme(theme);
						FormsTheme.Apply(form);
						var edge = (theme == ThemeType.Dark ? FormsTheme.Palette : FormsTheme.LightPalette)["BorderGray"];
						foreach (var track in new[] { plain, ticked })
						{
							// Widths of every remainder the thumb's place can be rounded with.
							for (var size = 200; size < 212; size++)
							{
								track.Width = size;
								foreach (var value in new[] { track.Minimum, track.Maximum })
								{
									track.Value = value;
									var low = value == track.Minimum;
									using (var bitmap = new Bitmap(track.Width, track.Height))
									{
										track.DrawToBitmap(bitmap, new Rectangle(Point.Empty, track.Size));
										// The thumb's ends along the way: the slider's first or last column, and as far in as the thumb is wide.
										var outer = low ? 0 : track.ClientSize.Width - 1;
										var inner = low ? width - 1 : track.ClientSize.Width - width;
										var square = track.TickStyle == TickStyle.None;
										var which = theme + ", " + (square ? "no ticks" : "ticks") + ", " + size + " wide, at its " + (low ? "lowest" : "highest") + " value";
										foreach (var column in new[] { outer, inner })
										{
											var rows = Enumerable.Range(0, bitmap.Height).Where(y => bitmap.GetPixel(column, y).ToArgb() == edge.ToArgb()).ToList();
											// A square thumb is edged down the whole of each end; a pointed one down its sides, five eighths of
											// its length, where it turns to the point.
											if (square ? rows.Count != length : rows.Count < length / 2)
											{
												problems.Add(which + ": column " + column + " shows " + rows.Count + " rows of the thumb's edge, not " + (square ? length.ToString() : "its side") + ".");
												continue;
											}
											// And its top edge, and a square thumb's bottom edge, across the middle of the thumb, no lower
											// and no higher than its sides reach.
											var middle = Math.Min(outer, inner) + width / 2;
											var across = Enumerable.Range(0, bitmap.Height).Where(y => bitmap.GetPixel(middle, y).ToArgb() == edge.ToArgb()).ToList();
											if (across.Count == 0 || across.First() > rows.First())
												problems.Add(which + ": the thumb has no top edge above its sides at " + rows.First() + ": it is cut there.");
											else if (square && across.Last() < rows.Last())
												problems.Add(which + ": the thumb has no bottom edge below its sides at " + rows.Last() + ": it is cut there.");
										}
									}
								}
							}
						}
					}
					Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems.Take(20)));
				}
			});
		}

		const int WM_MOUSEMOVE = 0x0200;
		const int WM_LBUTTONDOWN = 0x0201;
		const int WM_LBUTTONUP = 0x0202;
		const int MK_LBUTTON = 0x0001;

		/// <summary>The mouse's place in a window message.</summary>
		static IntPtr MousePlace(int x, int y)
		{
			return (IntPtr)((y << 16) | (x & 0xFFFF));
		}

		/// <summary>Presses and lets go of the left button at a place in a window.</summary>
		static void Click(IntPtr handle, int x, int y)
		{
			SendMessage(handle, WM_LBUTTONDOWN, (IntPtr)MK_LBUTTON, MousePlace(x, y));
			SendMessage(handle, WM_LBUTTONUP, IntPtr.Zero, MousePlace(x, y));
		}

		[TestMethod, TestCategory("interface")]
		[Description("The list a drop-down list opens has Default.xaml's items, as tall as a button, and no edge")]
		public void Drop_down_list_opens_a_list_as_default_xaml_s()
		{
			OnUiThread(() =>
			{
				using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				{
					var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
					combo.Items.AddRange(new object[] { "Gamepad", "Wheel" });
					form.Controls.Add(combo);
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.Apply(form);
					form.Show();
					const int CB_GETITEMHEIGHT = 0x0154;
					var height = combo.Font.Height + (int)Math.Round(8 * JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale);
					// The items are given their height as the list opens.
					combo.DroppedDown = true;
					Application.DoEvents();
					var items = (int)SendMessage(combo.Handle, CB_GETITEMHEIGHT, IntPtr.Zero, IntPtr.Zero);
					combo.DroppedDown = false;
					Assert.AreEqual(height, items, "The items are not as tall as Default.xaml's.");
					var info = new COMBOBOXINFO { cbSize = Marshal.SizeOf(typeof(COMBOBOXINFO)) };
					Assert.IsTrue(GetComboBoxInfo(combo.Handle, ref info), "The drop-down list has no list.");
					const int GWL_STYLE = -16;
					const int WS_BORDER = 0x00800000;
					Assert.AreEqual(0, GetWindowLong(info.hwndList, GWL_STYLE) & WS_BORDER, "The list keeps Windows' edge.");
				}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("Every mapping box on a controller's General tab is as tall as a text box in the same font, after the theme changes too")]
		public void Mapping_boxes_are_as_tall_as_the_text_boxes()
		{
			OnUiThread(() =>
			{
				using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				using (var page = new x360ce.App.Controls.PadControl(x360ce.Engine.MapTo.Controller1))
				{
					page.Dock = DockStyle.Fill;
					form.ClientSize = page.Size;
					form.Controls.Add(page);
					// The tab shows no text box of its own, so one in the window's font is themed beside it.
					var box = new TextBox();
					form.Controls.Add(box);
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.Apply(form);
					form.Show();
					Application.DoEvents();
					foreach (var theme in new[] { ThemeType.Light, ThemeType.Dark, ThemeType.Light })
					{
						FormsTheme.SetTheme(theme);
						FormsTheme.Apply(form);
						form.Width += 20;
						Application.DoEvents();
						var height = box.Height;
						var wrong = new[] { nameof(PadControl.GeneralTableLayoutPanel), nameof(PadControl.RemapTableLayoutPanel) }
							.SelectMany(x => page.Controls.Find(x, true)[0].Controls.OfType<ComboBox>())
							.Where(x => x.Height != height).Select(x => x.Name + " " + x.Height).ToList();
						Assert.AreEqual(0, wrong.Count, string.Format("Themed {0}, the text boxes are {1} px tall, but: {2}", theme, height, string.Join(", ", wrong)));
					}
				}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("On a stick's panel each row's percent box and number box stand side by side, the number box wide enough for 32767")]
		public void Stick_panel_rows_line_up()
		{
			OnUiThread(() =>
			{
				// The panel on the Left Thumb tab of a controller's page, shown after the theme, as the program shows it.
				using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				using (var page = new x360ce.App.Controls.PadControl(x360ce.Engine.MapTo.Controller1))
				{
					page.Dock = DockStyle.Fill;
					form.ClientSize = page.Size;
					form.Controls.Add(page);
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.Apply(form);
					form.Show();
					var tabs = (TabControl)page.Controls.Find(nameof(PadControl.PadTabControl), true).Single();
					tabs.SelectedTab = (TabPage)page.Controls.Find(nameof(PadControl.LeftThumbTabPage), true).Single();
					Application.DoEvents();
					var panel = page.Controls.Find(nameof(PadControl.LeftThumbXUserControl), true).Single();
					foreach (var name in new[] { "AntiDeadZone", "DeadZone" })
					{
						var percent = panel.Controls.Find(name + "TextBox", true).Single();
						var number = (NumericUpDown)panel.Controls.Find(name + "NumericUpDown", true).Single();
						Assert.AreEqual(percent.Top, number.Top, string.Format("{0}: the percent box is at {1}, the number box at {2}.", name, percent.Bounds, number.Bounds));
						var edit = number.Controls[1];
						var text = TextRenderer.MeasureText("32767", edit.Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
						Assert.IsTrue(edit.ClientSize.Width >= text, string.Format("{0}: the number box leaves {1} px for 32767, which is {2} px wide.", name, edit.ClientSize.Width, text));
					}
				}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("A drop-down list opens wide enough to show its longest item whole, however narrow the box")]
		public void Drop_down_list_opens_wide_enough_for_its_longest_item()
		{
			OnUiThread(() =>
			{
				using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				{
					// The stick panel's preset list: a narrow box with long names.
					var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 60 };
					combo.Items.AddRange(new object[] { "Apply Preset", "1% Dead Zone, 100% Controller Anti-Dead Zone" });
					form.Controls.Add(combo);
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.Apply(form);
					form.Show();
					combo.DroppedDown = true;
					Application.DoEvents();
					combo.DroppedDown = false;
					var longest = TextRenderer.MeasureText((string)combo.Items[1], combo.Font).Width;
					Assert.IsTrue(combo.DropDownWidth >= longest, string.Format("The list opens {0} px wide for an item {1} px wide.", combo.DropDownWidth, longest));
				}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("A tool bar button leaves Default.xaml's button padding between its fill and its picture and text")]
		public void Tool_bar_button_leaves_room_round_what_is_on_it()
		{
			OnUiThread(() =>
			{
				using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				using (var image = new Bitmap(16, 16))
				{
					using (var g = Graphics.FromImage(image))
						g.Clear(Color.Black);
					var strip = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
					var button = new ToolStripButton("Apply", image) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText };
					strip.Items.Add(button);
					form.Controls.Add(strip);
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.Apply(form);
					form.Show();
					Application.DoEvents();
					using (var bitmap = new Bitmap(strip.Width, strip.Height))
					{
						strip.DrawToBitmap(bitmap, new Rectangle(Point.Empty, strip.Size));
						var b = button.Bounds;
						// The fill's colour, from its top edge above the middle, where nothing is drawn on it.
						var fill = bitmap.GetPixel(b.Left + b.Width / 2, b.Top + 1);
						int fillLeft = int.MaxValue, fillRight = -1, inkLeft = int.MaxValue, inkRight = -1;
						for (var x = b.Left; x < b.Right; x++)
							for (var y = b.Top; y < b.Bottom; y++)
							{
								var c = bitmap.GetPixel(x, y);
								if (Math.Abs(c.R - fill.R) + Math.Abs(c.G - fill.G) + Math.Abs(c.B - fill.B) <= 6)
								{
									fillLeft = Math.Min(fillLeft, x);
									fillRight = Math.Max(fillRight, x);
								}
								if (c.R < 100 && c.G < 100 && c.B < 100)
								{
									inkLeft = Math.Min(inkLeft, x);
									inkRight = Math.Max(inkRight, x);
								}
							}
						// Default.xaml pads a button by 4; a pixel is allowed for the text's soft edge.
						var room = (int)Math.Round(4 * JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale) - 1;
						var message = string.Format("The fill runs {0} to {1}, the picture and text {2} to {3}; at least {4} px are wanted at each side.",
							fillLeft, fillRight, inkLeft, inkRight, room);
						Assert.IsTrue(inkLeft - fillLeft >= room, message);
						Assert.IsTrue(fillRight - inkRight >= room, message);
					}
				}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("Tool bar buttons stand Default.xaml's button margin from the tool bar's edge and twice that from each other, as push buttons do")]
		public void Tool_bar_buttons_are_spaced_as_buttons()
		{
			OnUiThread(() =>
			{
				using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				{
					// Laid out as the programs' tool bars are, with a padding and margins of their own.
					var strip = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, Padding = new Padding(6, 0, 2, 0) };
					strip.Items.Add(new ToolStripButton("Add") { Margin = new Padding(1, 1, 1, 2) });
					strip.Items.Add(new ToolStripButton("Remove") { Margin = new Padding(1, 1, 1, 2) });
					form.Controls.Add(strip);
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.Apply(form);
					form.Show();
					Application.DoEvents();
					var margin = (int)Math.Round(4 * JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale);
					using (var bitmap = new Bitmap(strip.Width, strip.Height))
					{
						strip.DrawToBitmap(bitmap, new Rectangle(Point.Empty, strip.Size));
						// The two fills, along the middle of the tool bar, where their rounded corners are not.
						var fills = Runs(bitmap, strip.Height / 2, strip.BackColor, true).Where(x => x.Y > margin * 4).ToList();
						Assert.AreEqual(2, fills.Count, "The tool bar shows " + fills.Count + " button fills, not two.");
						Assert.AreEqual(margin, fills[0].X, "The first button stands " + fills[0].X + " px from the tool bar's edge.");
						var gap = fills[1].X - fills[0].X - fills[0].Y;
						Assert.AreEqual(margin * 2, gap, "The buttons stand " + gap + " px apart.");
					}
				}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("A tool bar button stays as tall as Default.xaml's when its picture is enlarged after the theme, as x360ce enlarges them to the screen's scale once it has loaded")]
		public void Tool_bar_button_keeps_its_height_when_its_picture_grows()
		{
			OnUiThread(() =>
			{
				using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				using (var image = new Bitmap(16, 16))
				{
					var strip = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
					var button = new ToolStripButton("Add", image);
					strip.Items.Add(button);
					form.Controls.Add(strip);
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.Apply(form);
					form.Show();
					Application.DoEvents();
					var height = button.Height;
					// Still smaller than the text and its padding, so the button need not grow.
					strip.ImageScalingSize = new Size(18, 18);
					Application.DoEvents();
					Assert.AreEqual(height, button.Height, "The button grew with its picture.");
				}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("A push button showing ▾ and a drop-down button on a tool bar show a drop-down list's triangle, as large, in the middle of their height, in both themes")]
		public void Buttons_that_open_a_list_show_its_triangle()
		{
			OnUiThread(() =>
			{
				using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				{
					var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(10, 10), Width = 150 };
					var button = new Button { Text = "▾", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Location = new Point(10, 80) };
					var strip = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, Dock = DockStyle.Bottom };
					var drop = new ToolStripDropDownButton("Show: All") { DisplayStyle = ToolStripItemDisplayStyle.Text };
					drop.DropDownItems.Add("All");
					strip.Items.Add(drop);
					form.Controls.AddRange(new Control[] { combo, button, strip });
					foreach (var theme in new[] { ThemeType.Light, ThemeType.Dark })
					{
						FormsTheme.SetTheme(theme);
						FormsTheme.Apply(form);
						form.Show();
						Application.DoEvents();
						var column = (int)Math.Round(24 * JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale);
						var list = Mark(combo, new Rectangle(combo.Width - column, 0, column, combo.Height));
						var arrow = Mark(button, button.ClientRectangle);
						var bar = Mark(strip, new Rectangle(drop.Bounds.Right - column, drop.Bounds.Top, column, drop.Height));
						var text = Mark(strip, new Rectangle(drop.Bounds.Left, drop.Bounds.Top, drop.Width - column, drop.Height));
						var message = string.Format("{0}: the list's triangle is {1}, the push button's {2} on {3}, the tool bar button's {4} on {5}, its text {6}, padding {7}.",
							theme, list, arrow, button.ClientSize, bar, drop.Bounds, text, drop.Padding);
						foreach (var mark in new[] { arrow, bar })
							Assert.IsTrue(Math.Abs(mark.Width - list.Width) <= 1 && Math.Abs(mark.Height - list.Height) <= 1, message);
						// Each in the middle of its button's height, and the push button's in the middle of its width.
						Assert.IsTrue(Math.Abs(arrow.Top - (button.Height - arrow.Bottom)) <= 1, message);
						Assert.IsTrue(Math.Abs(arrow.Left - (button.Width - arrow.Right)) <= 1, message);
						Assert.IsTrue(Math.Abs(bar.Top - drop.Bounds.Top - (drop.Bounds.Bottom - bar.Bottom)) <= 1, message);
						// As far in from the tool bar button's right edge as a list's is from its own, and clear of the button's text by
						// at least Default.xaml's button margin.
						Assert.IsTrue(Math.Abs(drop.Bounds.Right - bar.Right - (combo.Width - list.Right)) <= 1, message);
						Assert.IsTrue(bar.Left - text.Right >= (int)Math.Round(4 * JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale), message);
					}
				}
			});
		}

		/// <summary>
		/// Where a control draws something on its face, within a part of it, in the control's own pixels: the pixels at least half
		/// as unlike the face as the most unlike one, the face's colour taken from the middle of the part's top edge.
		/// </summary>
		static Rectangle Mark(Control control, Rectangle part)
		{
			using (var bitmap = new Bitmap(control.Width, control.Height))
			{
				control.DrawToBitmap(bitmap, new Rectangle(Point.Empty, control.Size));
				var face = bitmap.GetPixel(part.X + part.Width / 2, part.Y + 2);
				Func<int, int, int> unlike = (x, y) =>
				{
					var c = bitmap.GetPixel(x, y);
					return Math.Abs(c.R - face.R) + Math.Abs(c.G - face.G) + Math.Abs(c.B - face.B);
				};
				var most = 0;
				for (var x = part.Left; x < part.Right; x++)
					for (var y = part.Top; y < part.Bottom; y++)
						most = Math.Max(most, unlike(x, y));
				int left = int.MaxValue, top = int.MaxValue, right = -1, bottom = -1;
				for (var x = part.Left; x < part.Right; x++)
					for (var y = part.Top; y < part.Bottom; y++)
						if (most > 0 && unlike(x, y) * 2 >= most)
						{
							left = Math.Min(left, x);
							top = Math.Min(top, y);
							right = Math.Max(right, x + 1);
							bottom = Math.Max(bottom, y + 1);
						}
				return right < 0 ? Rectangle.Empty : Rectangle.FromLTRB(left, top, right, bottom);
			}
		}

		[TestMethod, TestCategory("interface")]
		[Description("A grid's pictures stand Default.xaml's cell padding in from each side of their cells, as its text does, and the first column's 4 px more")]
		public void Grid_pictures_stand_in_from_their_cells()
		{
			OnUiThread(() =>
			{
				using (var form = new Form { ClientSize = new Size(300, 120), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				// Wider than the room a cell keeps for its error icon, which would set the column's width otherwise.
				using (var image = new Bitmap(24, 24))
				{
					using (var g = Graphics.FromImage(image))
						g.Clear(Color.Black);
					var grid = new DataGridView { Dock = DockStyle.Fill, RowHeadersVisible = false, AllowUserToAddRows = false };
					grid.Columns.Add(new DataGridViewImageColumn { AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCellsExceptHeader });
					grid.Columns.Add(new DataGridViewImageColumn { AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCellsExceptHeader });
					form.Controls.Add(grid);
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.Apply(form);
					form.Show();
					grid.Rows.Add(image, image);
					grid.ClearSelection();
					Application.DoEvents();
					var padding = (int)Math.Round(4 * JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale);
					using (var bitmap = new Bitmap(grid.Width, grid.Height))
					{
						grid.DrawToBitmap(bitmap, new Rectangle(Point.Empty, grid.Size));
						var row = grid.GetRowDisplayRectangle(0, false);
						var pictures = Runs(bitmap, row.Top + row.Height / 2, grid.BackgroundColor, true).Where(x => x.Y == image.Width).ToList();
						Assert.AreEqual(2, pictures.Count, "The row shows " + pictures.Count + " whole pictures, not two.");
						// The first column has the room before it that keeps it off the grid's edge, as wide as the padding.
						Assert.AreEqual(padding * 2, pictures[0].X, "The first picture stands " + pictures[0].X + " px from the grid's edge.");
						var gap = pictures[1].X - pictures[0].X - pictures[0].Y;
						Assert.AreEqual(padding * 2, gap, "The pictures stand " + gap + " px apart.");
					}
				}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("A disabled tool bar button's picture keeps its colour at half strength, as WPF draws it, rather than turning grey")]
		public void Disabled_tool_bar_picture_is_faded_not_grey()
		{
			OnUiThread(() =>
			{
				using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				using (var image = new Bitmap(16, 16))
				{
					using (var g = Graphics.FromImage(image))
						g.Clear(Color.Red);
					var strip = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
					var button = new ToolStripButton("Remove", image) { Enabled = false, ImageScaling = ToolStripItemImageScaling.None };
					strip.Items.Add(button);
					form.Controls.Add(strip);
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.Apply(form);
					form.Show();
					Application.DoEvents();
					using (var bitmap = new Bitmap(strip.Width, strip.Height))
					{
						strip.DrawToBitmap(bitmap, new Rectangle(Point.Empty, strip.Size));
						// The picture stands first on the button, in the middle of its height.
						var b = button.Bounds;
						var red = Enumerable.Range(b.Left, b.Width).Select(x => bitmap.GetPixel(x, b.Top + b.Height / 2))
							.FirstOrDefault(c => Math.Abs(c.R - c.G) > 20 || Math.Abs(c.G - c.B) > 20 || c.R < 120);
						Assert.IsTrue(red.R > red.G + 60 && red.R > red.B + 60, "The disabled picture is drawn " + red + ", not a faded red.");
					}
				}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("Every tool bar on a controller's page is in the font of what it stands on, as a WPF tool bar is")]
		public void Tool_bars_take_the_font_of_their_page()
		{
			OnUiThread(() =>
			{
				using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				using (var page = new x360ce.App.Controls.PadControl(x360ce.Engine.MapTo.Controller1))
				{
					page.Dock = DockStyle.Fill;
					form.ClientSize = page.Size;
					form.Controls.Add(page);
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.Apply(form);
					form.Show();
					var strips = AllControls(page).OfType<ToolStrip>().ToList();
					Assert.IsTrue(strips.Count > 0, "The page has no tool bar to look at.");
					foreach (var strip in strips)
						Assert.AreEqual(strip.Parent.Font, strip.Font, strip.Name + " is not in the font of " + strip.Parent.Name + ".");
				}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("A tool bar's pictures are enlarged with the screen once, to the size the main window's header shows its own at")]
		public void Tool_bar_pictures_are_scaled_once()
		{
			OnUiThread(() =>
			{
				using (var page = new x360ce.App.Controls.PadControl(x360ce.Engine.MapTo.Controller1))
				{
					// The main window enlarges every picture once its pages exist, and the header's buttons show their 16-pixel
					// pictures at their own size, which that enlarges by the screen's scale.
					JocysCom.ClassLibrary.Controls.ControlsHelper.ScaleImages(page);
					JocysCom.ClassLibrary.Controls.ControlsHelper.ScaleImages(page);
					var side = (int)Math.Round(16 * JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale);
					var strips = AllControls(page).OfType<ToolStrip>().ToList();
					Assert.IsTrue(strips.Count > 0, "The page has no tool bar to look at.");
					foreach (var strip in strips)
						Assert.AreEqual(new Size(side, side), strip.ImageScalingSize,
							strip.Name + " fits its pictures to another size than the header shows its own at, on a screen at "
							+ JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale * 100 + " %.");
				}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("A drop-down list on a tool bar is in the tool bar's font, as the game list on the main window's tool bar is")]
		public void List_on_a_tool_bar_takes_its_font()
		{
			OnUiThread(() =>
			{
				using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				{
					var strip = new ToolStrip();
					var list = new ToolStripComboBox();
					strip.Items.Add(list);
					form.Controls.Add(strip);
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.Apply(form);
					form.Show();
					Assert.AreEqual(form.Font, strip.Font, "The tool bar is not in the window's font.");
					Assert.AreEqual(strip.Font, list.ComboBox.Font, "The list is not in the tool bar's font.");
				}
			});
		}

		static System.Collections.Generic.IEnumerable<Control> AllControls(Control parent)
		{
			foreach (Control child in parent.Controls)
			{
				yield return child;
				foreach (var grandchild in AllControls(child))
					yield return grandchild;
			}
		}

		[TestMethod, TestCategory("interface")]
		[Description("Both charts on a stick's tab draw their line once the tab is shown, and an output of zero across their middle")]
		public void Stick_charts_draw_their_line()
		{
			OnUiThread(() =>
			{
				using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				using (var page = new x360ce.App.Controls.PadControl(x360ce.Engine.MapTo.Controller1))
				{
					page.Dock = DockStyle.Fill;
					form.ClientSize = page.Size;
					form.Controls.Add(page);
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.Apply(form);
					form.Show();
					var tabs = (TabControl)page.Controls.Find(nameof(PadControl.PadTabControl), true).Single();
					tabs.SelectedTab = (TabPage)page.Controls.Find(nameof(PadControl.LeftThumbTabPage), true).Single();
					foreach (var name in new[] { nameof(PadControl.LeftThumbXUserControl), nameof(PadControl.LeftThumbYUserControl) })
					{
						var axis = (x360ce.App.Controls.AxisMapUserControl)page.Controls.Find(name, true).Single();
						var chart = axis.Controls.Find(nameof(AxisMapUserControl.MainPictureBox), true).Single();
						// The line is drawn off the interface thread, so give it the time a person would, while the point
						// moves as often as the device loop moves it.
						var drawn = false;
						for (var waited = 0; !drawn && waited < 3000; waited += 50)
						{
							axis.DrawPoint(short.MaxValue, 0, false, false);
							Application.DoEvents();
							System.Threading.Thread.Sleep(50);
							using (var bitmap = new Bitmap(chart.Width, chart.Height))
							{
								chart.DrawToBitmap(bitmap, new Rectangle(Point.Empty, chart.Size));
								for (var x = 0; !drawn && x < bitmap.Width; x++)
									for (var y = 0; !drawn && y < bitmap.Height; y++)
									{
										var c = bitmap.GetPixel(x, y);
										drawn = c.R > 200 && c.G < 120 && c.B < 120;
									}
							}
						}
						Assert.IsTrue(drawn, name + " shows no red line.");
						// An output of zero is the middle of a stick's travel, so its line runs across the chart's
						// middle, whatever the chart's shape.
						var area = x360ce.App.AppHelper.ChartBounds(chart);
						var middle = area.Y + area.Height / 2f;
						var rows = new System.Collections.Generic.List<int>();
						using (var bitmap = new Bitmap(chart.Width, chart.Height))
						{
							chart.DrawToBitmap(bitmap, new Rectangle(Point.Empty, chart.Size));
							for (var y = area.Top; y < area.Bottom; y++)
							{
								var blue = 0;
								for (var x = area.Left; x < area.Right; x++)
								{
									var c = bitmap.GetPixel(x, y);
									if (c.B - c.R > 60 && c.B - c.G > 30)
										blue++;
								}
								if (blue > area.Width / 2)
									rows.Add(y);
							}
						}
						Assert.IsTrue(rows.Count > 0, name + " shows no line for the output.");
						Assert.AreEqual(middle, rows.Average(), 1.5, name + " draws an output of zero off the middle of its "
							+ area.Width + " by " + area.Height + " chart.");
					}
				}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("A text box and a number box leave room for a whole line of text, so a g or a p is not cut off")]
		public void Text_box_leaves_room_for_a_whole_line()
		{
			OnUiThread(() =>
			{
				// The program's own font at 150%: Windows draws its line 1 px taller than the font's height.
				using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false, Font = new Font("Microsoft Sans Serif", 12f) })
				{
					var box = new TextBox { Text = "Driving" };
					var number = new NumericUpDown { Top = 60 };
					form.Controls.AddRange(new Control[] { box, number });
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.Apply(form);
					form.Show();
					foreach (var edit in new Control[] { box, number.Controls[1] })
					{
						var line = TextRenderer.MeasureText(" ", edit.Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Height;
						Assert.IsTrue(edit.ClientSize.Height >= line, string.Format(
							"{0} leaves {1} px for a line of text that Windows draws {2} px tall.", edit.GetType().Name, edit.ClientSize.Height, line));
					}
				}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("Every disabled text, in any control, is a disabled label's colour, the theme's ForegroundDisabled: the text at half strength over the page, light #787878 and dark #6F6F6F; label, text box, box of many lines, password box, number box, drop-down list, editable drop-down list, button, check box and radio button, in both themes; a box that is only read-only keeps its text colour")]
		public void Every_disabled_text_is_a_disabled_label_colour()
		{
			OnUiThread(() =>
			{
				// Shown, so the controls draw as on the screen; out of sight, so nothing flashes. A font large enough for whole strokes.
				using (var form = new Form { ClientSize = new Size(440, 460), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false, Font = new Font("Segoe UI", 14f, FontStyle.Bold) })
				{
					const string text = "800 %";
					// The programs draw a label's text with GDI (Application.SetCompatibleTextRenderingDefault(false)).
					var label = new Label { Text = text, AutoSize = true, Location = new Point(10, 10), Enabled = false, UseCompatibleTextRendering = false };
					var box = new TextBox { Text = text, Location = new Point(10, 60), Width = 150, Enabled = false };
					var lines = new TextBox { Text = text, Multiline = true, Location = new Point(10, 110), Size = new Size(150, 60), Enabled = false };
					var number = new NumericUpDown { Maximum = 1000, Value = 800, Location = new Point(10, 190), Width = 150, Enabled = false };
					var list = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(10, 240), Width = 150, Enabled = false };
					list.Items.Add(text);
					list.SelectedIndex = 0;
					var editable = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Text = text, Location = new Point(10, 290), Width = 150, Enabled = false };
					var password = new TextBox { Text = "800", PasswordChar = '●', Location = new Point(220, 110), Width = 150, Enabled = false };
					var button = new Button { Text = text, Location = new Point(220, 160), Size = new Size(150, 40), Enabled = false };
					var check = new CheckBox { Text = text, AutoSize = true, Location = new Point(220, 220), Enabled = false };
					var radio = new RadioButton { Text = text, AutoSize = true, Location = new Point(220, 270), Enabled = false };
					// Without the focus, so its text is not shown chosen.
					var readOnly = new TextBox { Text = text, Location = new Point(220, 60), Width = 150, ReadOnly = true, TabStop = false };
					form.Controls.AddRange(new Control[] { label, box, lines, number, list, editable, password, button, check, radio, readOnly });
					form.Show();
					var problems = new System.Collections.Generic.List<string>();
					foreach (var theme in new[] { ThemeType.Light, ThemeType.Dark })
					{
						FormsTheme.SetTheme(theme);
						FormsTheme.Apply(form);
						form.Refresh();
						Application.DoEvents();
						// A disabled label's text: the text at half strength over the page, as measured.
						var disabled = theme == ThemeType.Light ? Color.FromArgb(0x78, 0x78, 0x78) : Color.FromArgb(0x6F, 0x6F, 0x6F);
						var key = FormsTheme.GetColor("ForegroundDisabled");
						if (Distance(key, disabled) > 0)
							problems.Add(string.Format("{0}: the theme's ForegroundDisabled is {1}, not the disabled label's {2}.", theme, Hex(key), Hex(disabled)));
						using (var picture = WindowPicture(form.Handle))
						{
							// The number box's text box, the lists' text clear of their triangle, the button's text clear of its
							// edge and the check box's and radio button's text clear of their box.
							var numberText = number.Controls.OfType<TextBox>().Single();
							var side = check.Height + 2;
							var parts = new[]
							{
								Tuple.Create("label", (Control)label, label.ClientRectangle),
								Tuple.Create("text box", (Control)box, box.ClientRectangle),
								Tuple.Create("box of many lines", (Control)lines, lines.ClientRectangle),
								Tuple.Create("password box", (Control)password, password.ClientRectangle),
								Tuple.Create("number box", (Control)numberText, numberText.ClientRectangle),
								Tuple.Create("drop-down list", (Control)list, new Rectangle(0, 0, list.Width / 2, list.Height)),
								Tuple.Create("editable drop-down list", (Control)editable, new Rectangle(0, 0, editable.Width / 2, editable.Height)),
								Tuple.Create("button", (Control)button, Rectangle.Inflate(button.ClientRectangle, -button.Width / 4, -button.Height / 4)),
								Tuple.Create("check box", (Control)check, Rectangle.FromLTRB(side, 0, check.Width, check.Height)),
								Tuple.Create("radio button", (Control)radio, Rectangle.FromLTRB(side, 0, radio.Width, radio.Height)),
							};
							foreach (var part in parts)
							{
								var ink = Ink(picture, form, part.Item2, part.Item3);
								if (Distance(ink, disabled) > 6)
									problems.Add(string.Format("{0}: the disabled {1}'s text is {2}, not the disabled label's {3}.", theme, part.Item1, Hex(ink), Hex(disabled)));
							}
							var readInk = Ink(picture, form, readOnly, readOnly.ClientRectangle);
							if (Distance(readInk, readOnly.ForeColor) > 6)
								problems.Add(string.Format("{0}: the read-only box's text is {1}, not its text colour {2}: it is not disabled.", theme, Hex(readInk), Hex(readOnly.ForeColor)));
						}
					}
					Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
				}
			});
		}

		/// <summary>The colour of the text in part of a control, in a picture of its window: the pixel farthest from the part's most common colour.</summary>
		static Color Ink(Bitmap picture, Form form, Control control, Rectangle part)
		{
			RECT window;
			GetWindowRect(form.Handle, out window);
			var r = control.RectangleToScreen(part);
			r.Offset(-window.Left, -window.Top);
			var colours = new System.Collections.Generic.Dictionary<int, int>();
			for (var x = r.Left; x < r.Right; x++)
				for (var y = r.Top; y < r.Bottom; y++)
				{
					var argb = picture.GetPixel(x, y).ToArgb();
					int n;
					colours.TryGetValue(argb, out n);
					colours[argb] = n + 1;
				}
			var back = Color.FromArgb(colours.OrderByDescending(x => x.Value).First().Key);
			// The text is grey: the coloured edges ClearType gives a stroke are left out, and of the rest the stroke's core is
			// farthest from the back in all three channels.
			return colours.Keys.Select(Color.FromArgb).Where(c => Math.Abs(c.R - c.G) <= 6 && Math.Abs(c.G - c.B) <= 6)
				.OrderByDescending(c => Math.Abs(c.R - back.R) + Math.Abs(c.G - back.G) + Math.Abs(c.B - back.B)).First();
		}

		static int Distance(Color a, Color b)
		{
			return Math.Max(Math.Abs(a.R - b.R), Math.Max(Math.Abs(a.G - b.G), Math.Abs(a.B - b.B)));
		}

		static string Hex(Color c)
		{
			return string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
		}

		[TestMethod, TestCategory("interface")]
		[Description("A number box a layout sizes is wide enough for its largest number")]
		public void Number_box_sized_by_a_layout_shows_its_largest_number()
		{
			OnUiThread(() =>
			{
				using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				{
					var table = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, RowCount = 1 };
					table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
					table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
					var number = new NumericUpDown { Maximum = 32767, AutoSize = true };
					table.Controls.Add(number, 0, 0);
					form.Controls.Add(table);
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.Apply(form);
					form.Show();
					Application.DoEvents();
					var edit = number.Controls[1];
					var text = TextRenderer.MeasureText("32767", edit.Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
					Assert.IsTrue(edit.ClientSize.Width >= text, string.Format(
						"The box leaves {0} px for 32767, which is {1} px wide.", edit.ClientSize.Width, text));
				}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("A drop-down list made editable is drawn as Default.xaml's editable one: its text box inside the frame, clear of the triangle, in the list's font")]
		public void Drop_down_list_made_editable_keeps_the_theme()
		{
			OnUiThread(() =>
			{
				using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				{
					var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
					combo.Items.Add("Axis 3");
					var box = new TextBox { Top = 60 };
					form.Controls.AddRange(new Control[] { combo, box });
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.Apply(form);
					form.Show();
					// As the formula switch on a mapping box does.
					combo.DropDownStyle = ComboBoxStyle.DropDown;
					combo.Text = "=a3";
					Application.DoEvents();
					var getStyle = typeof(Control).GetMethod("GetStyle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
					Assert.IsTrue((bool)getStyle.Invoke(combo, new object[] { ControlStyles.UserPaint }), "The editable list is left to Windows.");
					Assert.AreEqual(box.Height, combo.Height, "The editable list is not as tall as a text box.");
					var info = new COMBOBOXINFO { cbSize = Marshal.SizeOf(typeof(COMBOBOXINFO)) };
					Assert.IsTrue(GetComboBoxInfo(combo.Handle, ref info) && info.hwndItem != IntPtr.Zero, "The editable list has no text box.");
					RECT r;
					GetWindowRect(info.hwndItem, out r);
					var edit = combo.RectangleToClient(Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom));
					var scale = JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale;
					var line = TextRenderer.MeasureText(" ", combo.Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Height;
					Assert.AreEqual(FrameSide(), edit.Left, "The text box is not 4 inside the frame's line.");
					Assert.AreEqual(combo.ClientSize.Width - FrameSide() - (int)Math.Round(20 * scale), edit.Right, "The text box does not end clear of the triangle.");
					Assert.AreEqual(line, edit.Height, "The text box is not as tall as its line of text.");
					Assert.AreEqual((combo.ClientSize.Height - line) / 2, edit.Top, "The text box is not in the middle of the field.");
					const int WM_GETFONT = 0x0031;
					Assert.AreNotEqual(IntPtr.Zero, SendMessage(info.hwndItem, WM_GETFONT, IntPtr.Zero, IntPtr.Zero),
						"The text box has no font, so Windows draws it in the system's font.");
					// The text box takes its colours from the list, in both themes; without them it shows what was there before.
					foreach (var theme in new[] { ThemeType.Light, ThemeType.Dark })
					{
						FormsTheme.SetTheme(theme);
						FormsTheme.Apply(form);
						const int WM_CTLCOLOREDIT = 0x0133;
						const int NULL_BRUSH = 5;
						var dc = GetDC(info.hwndItem);
						try
						{
							var brush = SendMessage(combo.Handle, WM_CTLCOLOREDIT, dc, info.hwndItem);
							Assert.AreNotEqual(GetStockObject(NULL_BRUSH), brush, theme + ": the text box has no back, so it shows what was drawn there before.");
							Assert.AreEqual(ColorTranslator.ToWin32(combo.BackColor), GetBkColor(dc), theme + ": the text box is not in the field's colour.");
							Assert.AreEqual(ColorTranslator.ToWin32(combo.ForeColor), GetTextColor(dc), theme + ": the text is not in the list's colour.");
						}
						finally { ReleaseDC(info.hwndItem, dc); }
					}
				}
			});
		}

		[DllImport("user32.dll")]
		static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

		[DllImport("user32.dll")]
		static extern IntPtr GetDC(IntPtr hWnd);

		[DllImport("user32.dll")]
		static extern int ReleaseDC(IntPtr hWnd, IntPtr hdc);

		[DllImport("gdi32.dll")]
		static extern int GetBkColor(IntPtr hdc);

		[DllImport("gdi32.dll")]
		static extern int GetTextColor(IntPtr hdc);

		[DllImport("gdi32.dll")]
		static extern IntPtr GetStockObject(int index);

		struct COMBOBOXINFO
		{
			public int cbSize;
			public RECT rcItem, rcButton;
			public int stateButton;
			public IntPtr hwndCombo, hwndItem, hwndList;
		}

		[DllImport("user32.dll")]
		static extern bool GetComboBoxInfo(IntPtr hwndCombo, ref COMBOBOXINFO info);

		[DllImport("user32.dll")]
		static extern int GetWindowLong(IntPtr hWnd, int index);

		[DllImport("user32.dll")]
		static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref RECT rect);

		struct RECT
		{
			public int Left, Top, Right, Bottom;
		}

		/// <summary>
		/// The colour of a slider's track halfway along the rail Windows gives, in the middle of the first thing drawn there
		/// from the top, which is the track: the ticks are below it.
		/// </summary>
		static Color TrackCentre(TrackBar track)
		{
			const int TBM_GETCHANNELRECT = 0x041A;
			var rail = new RECT();
			SendMessage(track.Handle, TBM_GETCHANNELRECT, IntPtr.Zero, ref rail);
			using (var bitmap = new Bitmap(track.Width, track.Height))
			{
				track.DrawToBitmap(bitmap, new Rectangle(Point.Empty, track.Size));
				var x = (rail.Left + rail.Right) / 2;
				var runs = Runs(bitmap, x, track.BackColor);
				Assert.IsTrue(runs.Count > 0, "The slider draws no track.");
				return bitmap.GetPixel(x, runs[0].X + (runs[0].Y - 1) / 2);
			}
		}

		/// <summary>
		/// The rows of one column of a picture, or the columns of one row when across, that are not the colour given, as runs
		/// of first place and length, from the top or the left.
		/// </summary>
		static System.Collections.Generic.List<Point> Runs(Bitmap bitmap, int at, Color behind, bool across = false)
		{
			var runs = new System.Collections.Generic.List<Point>();
			var length = across ? bitmap.Width : bitmap.Height;
			Func<int, bool> marked = i => (across ? bitmap.GetPixel(i, at) : bitmap.GetPixel(at, i)).ToArgb() != behind.ToArgb();
			for (var i = 0; i < length; i++)
			{
				if (!marked(i))
					continue;
				var start = i;
				while (i < length && marked(i))
					i++;
				runs.Add(new Point(start, i - start));
			}
			return runs;
		}

		[TestMethod, TestCategory("interface")]
		[Description("In the light theme push buttons, tool bars, tab strips and text boxes are drawn as the WPF programs draw them, and anything else is left as it is")]
		public void Light_draws_wpf_controls_as_wpf_and_leaves_the_rest()
		{
			OnUiThread(() =>
			{
				using (var form = new Form())
				{
					var tree = new TreeView();
					var box = new TextBox();
					var button = new Button { UseVisualStyleBackColor = true };
					var flat = new Button { FlatStyle = FlatStyle.Flat };
					var bar = new ToolStrip();
					var tabs = new TabControl();
					tabs.TabPages.Add("Page");
					form.Controls.AddRange(new Control[] { tree, box, button, flat, bar, tabs });
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.Apply(form);
					Assert.IsTrue(tree.BackColor.IsSystemColor, "A tree was changed.");
					// The text box's edge is the frame the theme draws, and it stands as tall as Default.xaml's text box.
					Assert.AreEqual(BorderStyle.None, box.BorderStyle, "The text box kept Windows' edge.");
					Assert.AreEqual(box.Font.Height + FrameSide() * 2, box.Height, "The text box is not as tall as Default.xaml's.");
					Assert.AreEqual(FormsTheme.LightPalette["BackgroundWhite"].ToArgb(), box.BackColor.ToArgb(), "The text box is not filled with Default.xaml's BackgroundWhite.");
					Assert.AreEqual(FlatStyle.Flat, button.FlatStyle, "A push button kept the Windows look.");
					Assert.AreEqual(0, button.FlatAppearance.BorderSize, "A push button has a border.");
					Assert.AreEqual(FormsTheme.LightPalette["BackgroundDark"].ToArgb(), button.BackColor.ToArgb());
					Assert.AreEqual(FormsTheme.LightPalette["BackgroundDarkSelected"].ToArgb(), button.FlatAppearance.MouseOverBackColor.ToArgb(), "Under the mouse it is not Default.xaml's hover colour.");
					Assert.AreEqual(FormsTheme.LightPalette["BackgroundDarkPressed"].ToArgb(), button.FlatAppearance.MouseDownBackColor.ToArgb(), "Pressed it is not Default.xaml's pressed colour.");
					Assert.AreEqual(1, flat.FlatAppearance.BorderSize, "A button the program styled itself was changed.");
					Assert.IsTrue(flat.BackColor.IsSystemColor, "A button the program styled itself was filled.");
					Assert.AreNotEqual(typeof(ToolStripSystemRenderer), bar.Renderer.GetType(), "The tool bar kept the Windows renderer.");
					Assert.IsFalse(tabs.TabPages[0].UseVisualStyleBackColor, "The page of a painted strip kept the Windows texture.");
				}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("A scroll bar is shown whole in both themes while the mouse is not over it: both arrows, and a thumb at least a third as wide as the bar")]
		public void Scroll_bars_show_their_arrows_and_whole_thumb()
		{
			OnUiThread(() =>
			{
				// Shown, so the window draws its bars as on the screen; out of sight, so nothing flashes.
				using (var form = new Form { ClientSize = new Size(400, 200), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				{
					// A window with a bar in its edge, and a grid, whose bars are scroll bars of their own.
					var text = new TextBox
					{
						Multiline = true,
						ScrollBars = ScrollBars.Vertical,
						Dock = DockStyle.Left,
						Width = 150,
						Text = string.Join(Environment.NewLine, Enumerable.Range(1, 100).Select(i => "Line " + i)),
					};
					var grid = new DataGridView { Dock = DockStyle.Fill, ColumnCount = 2 };
					// And the list a drop-down list opens, a window of its own.
					var combo = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
					combo.Items.AddRange(Enumerable.Range(1, 30).Select(i => (object)("Item " + i)).ToArray());
					form.Controls.AddRange(new Control[] { grid, combo, text });
					for (var i = 0; i < 100; i++)
						grid.Rows.Add("Row " + i, "");
					form.Show();
					var problems = new System.Collections.Generic.List<string>();
					foreach (var theme in new[] { ThemeType.Dark, ThemeType.Light })
					{
						FormsTheme.SetTheme(theme);
						FormsTheme.Apply(form);
						Application.DoEvents();
						var gridBar = grid.Controls.OfType<VScrollBar>().First();
						using (var picture = WindowPicture(form.Handle))
						{
							CheckScrollBar(picture, form.Handle, text.Handle, OBJID_VSCROLL, theme + ", text box", problems);
							CheckScrollBar(picture, form.Handle, gridBar.Handle, OBJID_CLIENT, theme + ", grid", problems);
						}
						var info = new COMBOBOXINFO { cbSize = Marshal.SizeOf(typeof(COMBOBOXINFO)) };
						Assert.IsTrue(GetComboBoxInfo(combo.Handle, ref info));
						combo.DroppedDown = true;
						try
						{
							Application.DoEvents();
							using (var picture = WindowPicture(info.hwndList))
								CheckScrollBar(picture, info.hwndList, info.hwndList, OBJID_VSCROLL, theme + ", drop-down list", problems);
						}
						finally { combo.DroppedDown = false; }
					}
					Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
				}
			});
		}

		/// <summary>A window as it shows itself, its edge and everything on it.</summary>
		static Bitmap WindowPicture(IntPtr handle)
		{
			RECT r;
			GetWindowRect(handle, out r);
			var bitmap = new Bitmap(r.Right - r.Left, r.Bottom - r.Top);
			using (var g = Graphics.FromImage(bitmap))
			{
				var dc = g.GetHdc();
				try { PrintWindow(handle, dc, PW_RENDERFULLCONTENT); }
				finally { g.ReleaseHdc(dc); }
			}
			return bitmap;
		}

		/// <summary>
		/// Checks a scroll bar in a picture of the window given: the middle of each arrow's box holds a triangle, more than one
		/// colour, and the thumb is at least a third as wide as the bar across its middle.
		/// </summary>
		static void CheckScrollBar(Bitmap picture, IntPtr pictured, IntPtr handle, int bar, string which, System.Collections.Generic.List<string> problems)
		{
			var info = new SCROLLBARINFO { cbSize = Marshal.SizeOf(typeof(SCROLLBARINFO)) };
			RECT window;
			GetWindowRect(pictured, out window);
			if (!GetScrollBarInfo(handle, bar, ref info) || (info.Bar & STATE_SYSTEM_INVISIBLE) != 0)
			{
				problems.Add(which + ": the scroll bar is not shown.");
				return;
			}
			var r = Rectangle.FromLTRB(info.rcScrollBar.Left - window.Left, info.rcScrollBar.Top - window.Top,
				info.rcScrollBar.Right - window.Left, info.rcScrollBar.Bottom - window.Top);
			var arrow = info.dxyLineButton;
			foreach (var top in new[] { r.Top, r.Bottom - arrow })
			{
				var colours = new System.Collections.Generic.HashSet<int>();
				for (var y = top + arrow / 4; y < top + arrow - arrow / 4; y++)
					for (var x = r.Left + r.Width / 4; x < r.Right - r.Width / 4; x++)
						colours.Add(picture.GetPixel(x, y).ToArgb());
				if (colours.Count < 2)
					problems.Add(which + ": the " + (top == r.Top ? "first" : "last") + " arrow has no triangle.");
			}
			// Across the thumb's middle, beside it the track.
			var row = r.Top + (info.xyThumbTop + info.xyThumbBottom) / 2;
			var track = picture.GetPixel(r.Left + r.Width / 8, row);
			var thumb = Enumerable.Range(r.Left, r.Width).Count(x =>
			{
				var c = picture.GetPixel(x, row);
				return Math.Abs(c.R - track.R) + Math.Abs(c.G - track.G) + Math.Abs(c.B - track.B) > 24;
			});
			if (thumb * 3 < r.Width)
				problems.Add(which + ": the thumb is " + thumb + " of the bar's " + r.Width + " pixels wide.");
		}

		const int OBJID_VSCROLL = unchecked((int)0xFFFFFFFB);
		const int OBJID_CLIENT = unchecked((int)0xFFFFFFFC);
		const int STATE_SYSTEM_INVISIBLE = 0x00008000;
		const uint PW_RENDERFULLCONTENT = 0x00000002;

		struct SCROLLBARINFO
		{
			public int cbSize;
			public RECT rcScrollBar;
			public int dxyLineButton, xyThumbTop, xyThumbBottom, reserved;
			public int Bar, FirstArrow, PageBefore, Thumb, PageAfter, LastArrow;
		}

		[DllImport("user32.dll")]
		static extern bool GetScrollBarInfo(IntPtr hwnd, int idObject, ref SCROLLBARINFO info);

		/// <summary>The text's own colour: its darkest pixel on a light background, its lightest on a dark one, inside an area.</summary>
		static Color TextColour(Bitmap picture, Rectangle area, bool dark)
		{
			var found = dark ? Color.Black : Color.White;
			for (var x = area.Left; x < area.Right; x++)
				for (var y = area.Top; y < area.Bottom; y++)
				{
					var c = picture.GetPixel(x, y);
					if (dark ? c.R + c.G + c.B > found.R + found.G + found.B : c.R + c.G + c.B < found.R + found.G + found.B)
						found = c;
				}
			return found;
		}

		[TestMethod, TestCategory("ui")]
		[Description("A disabled text box's text and a disabled label's are the colour of every disabled text, the theme's ForegroundDisabled, in both themes, on the screen and in a picture printed without it (ClassLibrary FormsThemeTests compares them with WPF's; Windows Forms' dark theme is .NET Framework's)")]
		public void Disabled_text_keeps_its_colour_on_the_screen_and_printed()
		{
			var problems = new System.Collections.Generic.List<string>();
			OnUiThread(() =>
			{
				foreach (var theme in new[] { ThemeType.Light, ThemeType.Dark })
				{
					FormsTheme.SetTheme(theme);
					var font = new Font("Segoe UI", 18, FontStyle.Bold);
					using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false, Size = new Size(400, 300) })
					{
						var box = new TextBox { Text = "Ill", Width = 120, Font = font, Enabled = false, Location = new Point(10, 10) };
						var label = new Label { Text = "Ill", AutoSize = true, Font = font, Enabled = false, Location = new Point(10, 100), UseCompatibleTextRendering = false };
						form.Controls.Add(box);
						form.Controls.Add(label);
						form.Show();
						FormsTheme.Apply(form);
						Application.DoEvents();
						var dark = theme == ThemeType.Dark;
						var inset = (int)Math.Round(6 * JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale);
						var edge = (box.Width - box.ClientSize.Width) / 2;
						// The box as Windows composes it, taken once the compositor has its drawing.
						using (var picture = TextColourTest.Capture(box))
						{
							var text = TextColour(picture, new Rectangle(edge + inset, edge + inset, box.ClientSize.Width - inset * 2, box.ClientSize.Height - inset * 2), dark);
							var expected = FormsTheme.GetColor("ForegroundDisabled");
							if (Math.Abs(text.R - expected.R) > 1 || Math.Abs(text.G - expected.G) > 1 || Math.Abs(text.B - expected.B) > 1)
								problems.Add(theme + " text box: text " + text + ", not the disabled text colour " + expected);
						}
						// The box printed, as a picture of it is taken without the screen: the same text in the same colour.
						using (var picture = new Bitmap(box.Width, box.Height))
						{
							box.DrawToBitmap(picture, new Rectangle(Point.Empty, picture.Size));
							var text = TextColour(picture, new Rectangle(edge + inset, edge + inset, box.ClientSize.Width - inset * 2, box.ClientSize.Height - inset * 2), dark);
							var expected = FormsTheme.GetColor("ForegroundDisabled");
							if (Math.Abs(text.R - expected.R) > 1 || Math.Abs(text.G - expected.G) > 1 || Math.Abs(text.B - expected.B) > 1)
								problems.Add(theme + " text box printed: text " + text + ", not the disabled text colour " + expected);
						}
						using (var picture = new Bitmap(label.Width, label.Height))
						{
							label.DrawToBitmap(picture, new Rectangle(Point.Empty, picture.Size));
							var text = TextColour(picture, new Rectangle(Point.Empty, picture.Size), dark);
							var expected = FormsTheme.GetColor("ForegroundDisabled");
							if (Math.Abs(text.R - expected.R) > 1 || Math.Abs(text.G - expected.G) > 1 || Math.Abs(text.B - expected.B) > 1)
								problems.Add(theme + " label: text " + text + ", not the disabled text colour " + expected);
						}
					}
				}
			});
			Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
		}

		[TestMethod, TestCategory("ui")]
		[Description("A label and a check box that size themselves are as wide as WPF lays their text out, its letters' advance widths, within 2 pixels where Windows places letters on pixels its own way, also sized again for a new font: a label its text, a check box its box, the gap 4 and its text; a label a table may wrap still wraps")]
		public void Label_and_check_box_are_as_wide_as_their_text_is_drawn()
		{
			var problems = new System.Collections.Generic.List<string>();
			OnUiThread(() =>
			{
				FormsTheme.SetTheme(ThemeType.Light);
				var scale = JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale;
				using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false, Size = new Size(600, 300), Font = new Font("Segoe UI", 9f) })
				{
					var label = new Label { Text = "Start with Windows", AutoSize = true, UseCompatibleTextRendering = false, Location = new Point(10, 10) };
					var check = new CheckBox { Text = "Start with Windows", AutoSize = true, Location = new Point(10, 50) };
					var empty = new CheckBox { Text = "", AutoSize = true, Location = new Point(10, 90) };
					// A note across a table's columns, which the table wraps at the width it has.
					var table = new TableLayoutPanel { Location = new Point(10, 130), Size = new Size(200, 120), ColumnCount = 2 };
					table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
					table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
					var note = new Label { Text = "Plugins add devices. A plugin runs with all the rights of x360ce.", AutoSize = true, UseCompatibleTextRendering = false };
					table.Controls.Add(note, 0, 0);
					table.SetColumnSpan(note, 2);
					form.Controls.AddRange(new Control[] { label, check, empty, table });
					form.Show();
					FormsTheme.Apply(form);
					Application.DoEvents();
					var gap = (int)Math.Round(4 * scale);
					foreach (var size in new[] { 9f, 12f })
					{
						if (size != label.Font.Size)
						{
							label.Font = new Font("Segoe UI", size);
							check.Font = new Font("Segoe UI", size);
							empty.Font = new Font("Segoe UI", size);
							Application.DoEvents();
						}
						var font = label.Font;
						var formatted = new System.Windows.Media.FormattedText(label.Text, System.Globalization.CultureInfo.InvariantCulture, System.Windows.FlowDirection.LeftToRight,
							new System.Windows.Media.Typeface(font.FontFamily.Name), font.SizeInPoints * 96 / 72, System.Windows.Media.Brushes.Black, scale);
						var text = (int)Math.Round(formatted.WidthIncludingTrailingWhitespace * scale);
						if (Math.Abs(label.Width - text) > 2)
							problems.Add(string.Format("{0} pt: the label is {1} wide, its text {2}.", size, label.Width, text));
						if (Math.Abs(check.Width - empty.Width - gap - text) > 2)
							problems.Add(string.Format("{0} pt: the check box is {1} wide: its box {2}, the gap {3} and its text {4} make {5}.", size, check.Width, empty.Width, gap, text, empty.Width + gap + text));
					}
					if (note.Width > table.Width || note.Height < note.Font.Height * 3 / 2)
						problems.Add(string.Format("The note across the table is {0}, not wrapped inside the table's {1} width.", note.Size, table.Width));
				}
			});
			Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
		}

		[TestMethod, TestCategory("ui")]
		[Description("A tab strip the program draws itself is drawn here in the dark theme, sized by the theme's blank pictures, and given back to the program in the light theme with Windows' own picture list back")]
		public void Tab_strip_given_back_gets_its_own_picture_list_back()
		{
			const int TCM_GETIMAGELIST = 0x1302;
			OnUiThread(() =>
			{
				FormsTheme.SetTheme(ThemeType.Light);
				using (var pictures = new ImageList { ImageSize = new Size(16, 16) })
				using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false, Size = new Size(400, 200) })
				{
					pictures.Images.Add(new Bitmap(16, 16));
					var tabs = new TabControl { Size = new Size(300, 100), ImageList = pictures, DrawMode = TabDrawMode.OwnerDrawFixed };
					tabs.TabPages.Add(new TabPage("Controller 1") { ImageIndex = 0 });
					form.Controls.Add(tabs);
					form.Show();
					FormsTheme.Apply(form);
					Application.DoEvents();
					Assert.AreEqual(pictures.Handle, SendMessage(tabs.Handle, TCM_GETIMAGELIST, IntPtr.Zero, IntPtr.Zero), "Light, drawn by the program: Windows' own list");
					FormsTheme.SetTheme(ThemeType.Dark);
					Application.DoEvents();
					Assert.AreNotEqual(pictures.Handle, SendMessage(tabs.Handle, TCM_GETIMAGELIST, IntPtr.Zero, IntPtr.Zero), "Dark, drawn here: the theme's blank pictures");
					FormsTheme.SetTheme(ThemeType.Light);
					Application.DoEvents();
					Assert.AreEqual(pictures.Handle, SendMessage(tabs.Handle, TCM_GETIMAGELIST, IntPtr.Zero, IntPtr.Zero), "Light again: Windows' own list back");
				}
			});
		}

		[DllImport("user32.dll")]
		static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);
	}
}
