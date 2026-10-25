// @under-test: Engine/JocysCom/Controls/Themes/Default.Forms.cs, Engine/JocysCom/Controls/Themes/Default_DarkTheme.xaml
// @area: interface   @layer: unit
using JocysCom.ClassLibrary.Controls.Themes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

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

		[TestMethod, TestCategory("interface")]
		[Description("The dark colours are the ones of the WPF dark theme the program carries")]
		public void Dark_colours_come_from_the_wpf_theme()
		{
			var keys = new[]
			{
				"BackgroundWhite", "BackgroundLight", "BackgroundDark", "BorderDark", "ForegroundBlack",
				"ForegroundGray", "SelectedBackground", "SelectedForeground", "BackgroundHelp", "ColorBrand",
				"ForegroundSuccess", "BackgroundTabSelected", "ForegroundTab", "ForegroundTabSelected",
				"MouseOverBackground", "BackgroundDarkPressed",
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
		/// The colour in the middle of the first cell's check box, where Default.xaml's CheckBoxCell puts it: 3 in from the
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
				return bitmap.GetPixel(cell.X + (int)Math.Round(3 * scale) + CheckBoxWidth(grid.Font) / 2, cell.Y + cell.Height / 2);
			}
		}

		/// <summary>
		/// The width of Default.xaml's check box beside text in a font, in pixels: 18 beside Default.xaml's own text, Segoe UI
		/// at 12, whose line is 15.96 high, and in proportion beside any other.
		/// </summary>
		static int CheckBoxWidth(Font font)
		{
			return (int)Math.Round(18 * font.Height / 15.96);
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
						Assert.AreEqual(palette["BackgroundDark"].ToArgb(), rail.ToArgb(), "The slider's track is not Default.xaml's in the " + theme + " theme: " + rail);
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
					Assert.AreEqual((int)Math.Round(18 * scale) + (int)Math.Round(6 * scale), track.Height, "The slider is not as tall as its thumb and ticks.");
					using (var bitmap = new Bitmap(track.Width, track.Height))
					{
						track.DrawToBitmap(bitmap, new Rectangle(Point.Empty, track.Size));
						// The thumb at the lowest value: from its top edge, in the edge colour, to its tip.
						var thumb = Runs(bitmap, (int)(11 * scale / 2), track.BackColor);
						var edge = bitmap.GetPixel((int)(11 * scale / 2), thumb[0].X);
						var fill = FormsTheme.LightPalette["BackgroundDark"];
						Assert.IsTrue(edge.R < fill.R - 40, "The thumb's top edge is cut: its first row is " + edge + ".");
						// The tick at the far end, where the thumb is not: below the track, and four long.
						var runs = Runs(bitmap, (int)(track.ClientSize.Width - 11 * scale / 2), track.BackColor);
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
					Assert.IsTrue(check.Height >= (int)Math.Round(17.1 * size), "The check box is " + check.Height + " high, shorter than Default.xaml's box.");
					Assert.IsTrue(radio.Height >= (int)Math.Round(18 * size), "The radio button is " + radio.Height + " high, shorter than Default.xaml's.");
				}
			});
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
					var button = new Button { Top = 40 };
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
					var height = box.Font.Height + (int)Math.Round(10 * JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale);
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
					// WPF's thumb, 11 wide, travels from half its width in from one end to as far from the other.
					var first = 11 * JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale / 2;
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
					var height = combo.Font.Height + (int)Math.Round(10 * JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale);
					Assert.AreEqual(height, (int)SendMessage(combo.Handle, CB_GETITEMHEIGHT, IntPtr.Zero, IntPtr.Zero), "The items are not as tall as Default.xaml's.");
					var info = new COMBOBOXINFO { cbSize = Marshal.SizeOf(typeof(COMBOBOXINFO)) };
					Assert.IsTrue(GetComboBoxInfo(combo.Handle, ref info), "The drop-down list has no list.");
					const int GWL_STYLE = -16;
					const int WS_BORDER = 0x00800000;
					Assert.AreEqual(0, GetWindowLong(info.hwndList, GWL_STYLE) & WS_BORDER, "The list keeps Windows' edge.");
				}
			});
		}

		[TestMethod, TestCategory("interface")]
		[Description("Every mapping box on a controller's General tab is as tall as the text boxes beside it, after the theme changes too")]
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
						var panel = page.Controls.Find("GeneralLeftPanel", true)[0];
						var height = panel.Controls.OfType<TextBox>().First().Height;
						var wrong = new[] { "GeneralLeftPanel", "GeneralCenterPanel", "GeneralRightPanel" }
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
					var tabs = (TabControl)page.Controls.Find("PadTabControl", true).Single();
					tabs.SelectedTab = (TabPage)page.Controls.Find("LeftThumbTabPage", true).Single();
					Application.DoEvents();
					var panel = page.Controls.Find("LeftThumbXUserControl", true).Single();
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
						// Default.xaml pads a button by 5; a pixel is allowed for the text's soft edge.
						var room = (int)Math.Round(5 * JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale) - 1;
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
					var margin = (int)Math.Round(3 * JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale);
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
		[Description("A grid's pictures stand Default.xaml's cell padding in from each side of their cells, as its text does")]
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
					var padding = (int)Math.Round(3 * JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale);
					using (var bitmap = new Bitmap(grid.Width, grid.Height))
					{
						grid.DrawToBitmap(bitmap, new Rectangle(Point.Empty, grid.Size));
						var row = grid.GetRowDisplayRectangle(0, false);
						var pictures = Runs(bitmap, row.Top + row.Height / 2, grid.BackgroundColor, true).Where(x => x.Y == image.Width).ToList();
						Assert.AreEqual(2, pictures.Count, "The row shows " + pictures.Count + " whole pictures, not two.");
						Assert.AreEqual(padding, pictures[0].X, "The first picture stands " + pictures[0].X + " px from the grid's edge.");
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
					var tabs = (TabControl)page.Controls.Find("PadTabControl", true).Single();
					tabs.SelectedTab = (TabPage)page.Controls.Find("LeftThumbTabPage", true).Single();
					foreach (var name in new[] { "LeftThumbXUserControl", "LeftThumbYUserControl" })
					{
						var axis = (x360ce.App.Controls.AxisMapUserControl)page.Controls.Find(name, true).Single();
						var chart = axis.Controls.Find("MainPictureBox", true).Single();
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
					Assert.AreEqual((int)Math.Round(5 * scale), edit.Left, "The text box is not 5 in from the side.");
					Assert.AreEqual(combo.ClientSize.Width - (int)Math.Round(21 * scale), edit.Right, "The text box does not end clear of the triangle.");
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
					Assert.AreEqual(box.Font.Height + (int)Math.Round(10 * JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale), box.Height, "The text box is not as tall as Default.xaml's.");
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
	}
}
