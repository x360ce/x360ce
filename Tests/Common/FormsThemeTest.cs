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
					var box = new TextBox();
					var button = new Button { Text = "Copy prompt", UseVisualStyleBackColor = true };
					var tabs = new TabControl();
					tabs.TabPages.Add("Page");
					form.Controls.AddRange(new Control[] { box, button, tabs });
					FormsTheme.SetTheme(ThemeType.Dark);
					FormsTheme.Apply(form);
					Assert.IsFalse(box.BackColor.IsSystemColor, "A native control kept the Windows brush, which paints it light.");
					Assert.AreEqual(FormsTheme.Palette["BackgroundWhite"].ToArgb(), box.BackColor.ToArgb());
					Assert.AreEqual(FlatStyle.Flat, button.FlatStyle, "A push button kept the light visual style.");
					Assert.AreEqual(0, button.FlatAppearance.BorderSize, "A flat border takes room from the text.");
					// Shown light again, as the light theme does with the open windows.
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.Apply(form);
					Assert.IsTrue(box.BackColor.IsSystemColor, "The text box did not get the Windows colour back.");
					Assert.AreEqual(SystemColors.Window, box.BackColor);
					Assert.AreEqual(FlatStyle.Standard, button.FlatStyle);
					Assert.AreEqual(1, button.FlatAppearance.BorderSize);
					Assert.IsTrue(button.UseVisualStyleBackColor);
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
		[Description("A slider's rail is drawn dark in the dark theme, and Windows draws it light again in light")]
		public void Slider_goes_dark_and_comes_back()
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
					var dark = RailCentre(track);
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.Apply(form);
					var light = RailCentre(track);
					Assert.IsTrue(dark.GetBrightness() < 0.5f, "The rail is light in the dark theme: " + dark);
					Assert.IsTrue(light.GetBrightness() > 0.5f, "The rail is dark in the light theme: " + light);
				}
			});
		}

		[DllImport("user32.dll")]
		static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref RECT rect);

		struct RECT
		{
			public int Left, Top, Right, Bottom;
		}

		/// <summary>The colour in the middle of a slider's rail, where Windows says the rail is.</summary>
		static Color RailCentre(TrackBar track)
		{
			const int TBM_GETCHANNELRECT = 0x041A;
			var rail = new RECT();
			SendMessage(track.Handle, TBM_GETCHANNELRECT, IntPtr.Zero, ref rail);
			using (var bitmap = new Bitmap(track.Width, track.Height))
			{
				track.DrawToBitmap(bitmap, new Rectangle(Point.Empty, track.Size));
				return bitmap.GetPixel((rail.Left + rail.Right) / 2, (rail.Top + rail.Bottom) / 2);
			}
		}

		[TestMethod, TestCategory("interface")]
		[Description("A control the dark theme never changed is left as it is")]
		public void Light_from_the_start_changes_nothing()
		{
			OnUiThread(() =>
			{
				using (var form = new Form())
				{
					var button = new Button { UseVisualStyleBackColor = true };
					form.Controls.Add(button);
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.Apply(form);
					Assert.AreEqual(FlatStyle.Standard, button.FlatStyle);
					Assert.IsTrue(button.UseVisualStyleBackColor);
					Assert.IsTrue(button.BackColor.IsSystemColor);
				}
			});
		}
	}
}
