// @under-test: Engine/JocysCom/Controls/Themes/Default.Forms.cs, Engine/JocysCom/Controls/Themes/Default_DarkTheme.xaml
// @area: interface   @layer: unit
using JocysCom.ClassLibrary.Controls.Themes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

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
		[Description("A grid check box is drawn dark, also in a grid whose own painter paints the light box first")]
		public void Grid_check_box_goes_dark()
		{
			// Here, not on the interface thread, which reports an inconclusive result as a failure.
			if (!VisualStyleRenderer.IsSupported
				|| !VisualStyleRenderer.IsElementDefined(VisualStyleElement.CreateElement("DarkMode_Explorer::BUTTON", 3, 1)))
				Assert.Inconclusive("This process or this Windows has no dark visual style to draw a check box with.");
			OnUiThread(() =>
			{
				// Shown, because a grid lays out only the cells it shows; out of sight, so nothing flashes.
				using (var form = new Form { ClientSize = new Size(120, 80), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				{
					var grid = new DataGridView { Dock = DockStyle.Fill, RowHeadersVisible = false, ColumnHeadersVisible = false, AllowUserToAddRows = false };
					grid.Columns.Add(new DataGridViewCheckBoxColumn());
					grid.Rows.Add(false);
					// The painter x360ce gives its grids, which paints every cell, box included.
					JocysCom.ClassLibrary.Controls.ControlsHelper.ApplyBorderStyle(grid);
					form.Controls.Add(grid);
					form.Show();
					FormsTheme.SetTheme(ThemeType.Dark);
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

		/// <summary>The colour in the middle of the first cell's check box, as the grid paints it.</summary>
		static Color CheckBoxCentre(DataGridView grid)
		{
			using (var bitmap = new Bitmap(grid.Width, grid.Height))
			{
				grid.DrawToBitmap(bitmap, new Rectangle(Point.Empty, grid.Size));
				var cell = grid.GetCellDisplayRectangle(0, 0, false);
				var box = grid.Rows[0].Cells[0].ContentBounds;
				Assert.IsFalse(cell.IsEmpty || box.IsEmpty, "The grid shows no check box to look at.");
				return bitmap.GetPixel(cell.X + box.X + box.Width / 2, cell.Y + box.Y + box.Height / 2);
			}
		}

		[TestMethod, TestCategory("interface")]
		[Description("A slider is drawn as WPF draws one in both themes, its track in WPF's own colour")]
		public void Slider_is_drawn_as_wpf_s_in_both_themes()
		{
			OnUiThread(() =>
			{
				// Shown, as the grid test is, so the slider paints; out of sight, so nothing flashes.
				using (var form = new Form { ClientSize = new Size(220, 60), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				{
					var track = new TrackBar { Dock = DockStyle.Fill, Maximum = 100, TickFrequency = 10 };
					form.Controls.Add(track);
					form.Show();
					FormsTheme.SetTheme(ThemeType.Dark);
					FormsTheme.Apply(form);
					var dark = TrackCentre(track);
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.Apply(form);
					var light = TrackCentre(track);
					// WPF's own slider, which Default.xaml does not restyle, fills its track #E7EAEA in either theme.
					var wpf = ColorTranslator.FromHtml("#E7EAEA").ToArgb();
					Assert.AreEqual(wpf, dark.ToArgb(), "The track is not WPF's in the dark theme: " + dark);
					Assert.AreEqual(wpf, light.ToArgb(), "The track is not WPF's in the light theme: " + light);
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

		[DllImport("user32.dll")]
		static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref RECT rect);

		struct RECT
		{
			public int Left, Top, Right, Bottom;
		}

		/// <summary>The colour of a slider's track halfway along the rail Windows gives, through the middle of the thumb, where the track is drawn.</summary>
		static Color TrackCentre(TrackBar track)
		{
			const int TBM_GETTHUMBRECT = 0x0419;
			const int TBM_GETCHANNELRECT = 0x041A;
			var rail = new RECT();
			SendMessage(track.Handle, TBM_GETCHANNELRECT, IntPtr.Zero, ref rail);
			var thumb = new RECT();
			SendMessage(track.Handle, TBM_GETTHUMBRECT, IntPtr.Zero, ref thumb);
			using (var bitmap = new Bitmap(track.Width, track.Height))
			{
				track.DrawToBitmap(bitmap, new Rectangle(Point.Empty, track.Size));
				return bitmap.GetPixel((rail.Left + rail.Right) / 2, (thumb.Top + thumb.Bottom) / 2);
			}
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
