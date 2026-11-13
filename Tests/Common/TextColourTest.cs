// @under-test: Engine/JocysCom/Controls/Themes/Default.Forms.cs, Engine/JocysCom/Controls/Themes/Default_DarkTheme.xaml, Engine/JocysCom/Controls/ControlsHelper.Windows.cs, App.v4/Common/AppHelper.cs, Engine/JocysCom/IO/HardwareControl.cs, Engine/JocysCom/Controls/IssuesControl/IssuesUserControl.cs, Engine/Common/MarkdownRtf.cs, App.v4/Controls, App.v4/Forms, App.v3/Controls
// @area: ui   @layer: unit
using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.IssuesControl;
using JocysCom.ClassLibrary.Controls.Themes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.App.Forms;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// Coloured text, links and charts take their colour from the theme by purpose, one key for each purpose, so the dark theme
	/// never shows a light theme's colour, and each one reads: text 4.5:1 against what it is drawn on (WCAG 2.2 AA 1.4.3), a
	/// chart's lines and dots 3:1 against the chart (1.4.11).
	/// </summary>
	/// <remarks>
	/// What each key is for is written beside it in Default_DarkTheme.xaml. A key whose colour happens to suit is never taken
	/// for another purpose: <see cref="Each_colour_key_is_used_only_for_its_own_purpose"/> holds the code to that.
	/// </remarks>
	[TestClass]
	public class TextColourTest
	{
		public TestContext TestContext { get; set; }

		/// <summary>The contrast WCAG 2.2 AA asks of text (1.4.3).</summary>
		const double TextContrast = 4.5;

		/// <summary>The contrast WCAG 2.2 AA asks of the lines and dots of a chart (1.4.11).</summary>
		const double GraphicContrast = 3.0;

		/// <summary>The theme keys of text, each for one purpose; the first is the body text, which is a system colour.</summary>
		static readonly string[] Roles =
		{
			"ForegroundBlack", "ForegroundGray", "ForegroundWarning", "ForegroundCaution", "ForegroundInformation", "ForegroundSuccess",
			"ForegroundLink", "ForegroundActive", "ForegroundOutput", "ForegroundCode", "ForegroundCommand",
		};

		/// <summary>The theme keys text stands on: lists, boxes, documents and charts, pages, and column headers.</summary>
		static readonly string[] Backgrounds = { "BackgroundWhite", "BackgroundLight", "BackgroundHeader" };

		/// <summary>The theme keys a chart draws its lines in: the curve, the reading and what the curve gives for it.</summary>
		static readonly string[] ChartRoles = { "ColorCurve", "ForegroundActive", "ForegroundOutput" };

		[TestMethod, TestCategory("ui")]
		[Description("Each text purpose has a colour in both themes that reads on every background text stands on there, a chart's lines read on the chart, and so do secondary text and the text of a dimmed row selected")]
		public void Every_text_role_reads_on_its_backgrounds_in_both_themes()
		{
			var faults = new List<string>();
			Ui.OnUiThread(() =>
			{
				try
				{
					foreach (var theme in new[] { ThemeType.Light, ThemeType.Dark })
					{
						FormsTheme.SetTheme(theme);
						var palette = theme == ThemeType.Dark ? FormsTheme.Palette : FormsTheme.LightPalette;
						foreach (var role in Roles.Concat(Backgrounds).Concat(ChartRoles).Distinct())
							if (!palette.ContainsKey(role))
								faults.Add(theme + ": the theme has no colour for " + role + ".");
						var texts = Roles.Where(palette.ContainsKey).Select(x => new { Name = x, Color = palette[x] })
							.Concat(new[] { new { Name = "SystemColors.GrayText", Color = SystemColors.GrayText } });
						var backs = Backgrounds.Where(palette.ContainsKey).Select(x => new { Name = x, Color = palette[x] });
						foreach (var text in texts)
							foreach (var back in backs)
								Expect(faults, theme + ": " + text.Name, text.Color, back.Name, back.Color);
						// A chart is drawn on a box in the window colour.
						if (palette.ContainsKey("BackgroundWhite"))
							foreach (var role in ChartRoles.Where(palette.ContainsKey))
								Expect(faults, theme + ": " + role + " line", palette[role], "a chart", palette["BackgroundWhite"], GraphicContrast);
						// A selected row of something unavailable, which is dimmed, and in the dark theme any selected row. The light
						// theme's selection is Windows' own, white on #0078D7: 4.498:1.
						if (theme == ThemeType.Dark)
							Expect(faults, theme + ": selected text", SystemColors.HighlightText, "the selection", SystemColors.Highlight);
						Expect(faults, theme + ": selected text", SystemColors.HighlightText, "a dimmed row's selection", SystemColors.ControlDarkDark);
					}
				}
				finally
				{
					FormsTheme.SetTheme(ThemeType.Light);
				}
			}, "Reading the theme's text colours");
			Assert.AreEqual(0, faults.Count, string.Join(Environment.NewLine, faults));
		}

		[TestMethod, TestCategory("ui")]
		[Description("The text of an unplugged or switched-off device's row, on the Devices page and on a controller tab, is the theme's disabled text colour and stands further from a present row's text than secondary text does, in both themes")]
		public void Unavailable_rows_are_in_the_disabled_text_colour_fainter_than_secondary_text()
		{
			var faults = new List<string>();
			var counted = 0;
			Func<string, InputSourceType, bool, x360ce.Engine.Data.UserDevice> device = (name, source, online) => new x360ce.Engine.Data.UserDevice
			{
				InstanceGuid = Guid.NewGuid(), InstanceName = name, ProductName = name, InputSourceType = (int)source, IsOnline = online,
			};
			var devices = new[]
			{
				device("DirectInput present", InputSourceType.DirectInput, true),
				device("DirectInput away", InputSourceType.DirectInput, false),
				device("Raw Input present", InputSourceType.RawInput, true),
				device("Raw Input away", InputSourceType.RawInput, false),
				device("Plugin present", InputSourceType.Plugin, true),
				device("Plugin away", InputSourceType.Plugin, false),
				device("DirectInput switched off", InputSourceType.DirectInput, true),
			};
			devices[6].IsEnabled = false;
			Ui.OnUiThread(() =>
			{
				try
				{
					foreach (var theme in new[] { ThemeType.Light, ThemeType.Dark })
					{
						FormsTheme.SetTheme(theme);
						var palette = theme == ThemeType.Dark ? FormsTheme.Palette : FormsTheme.LightPalette;
						var unavailable = new List<Color>();
						Action<DataGridView, string> check = (grid, name) =>
						{
							var present = grid.DefaultCellStyle.ForeColor;
							foreach (DataGridViewRow row in grid.Rows)
							{
								if (ControlsHelper.IsItemAvailable(row.DataBoundItem))
									continue;
								foreach (DataGridViewCell cell in row.Cells)
								{
									var style = cell.Visible ? TextStyle(cell) : null;
									if (style == null)
										continue;
									var where = theme + " " + name + " row " + row.Index + " " + grid.Columns[cell.ColumnIndex].Name;
									counted++;
									if (style.ForeColor.ToArgb() != palette["ForegroundDisabled"].ToArgb())
										faults.Add(where + ": " + Hex(style.ForeColor) + " is not the disabled text colour " + Hex(palette["ForegroundDisabled"]) + ".");
									var secondary = palette["ForegroundGray"];
									if (Contrast(style.ForeColor, present) <= Contrast(secondary, present))
										faults.Add(string.Format(CultureInfo.InvariantCulture, "{0}: {1} is {2:0.00}:1 from the text of a present row {3}, no further than secondary text {4} ({5:0.00}:1).",
											where, Hex(style.ForeColor), Contrast(style.ForeColor, present), Hex(present), Hex(secondary), Contrast(secondary, present)));
									unavailable.Add(style.ForeColor);
								}
							}
						};
						foreach (var d in devices)
							x360ce.App.SettingsManager.UserDevices.Items.Add(d);
						try
						{
							using (var form = Show(new UserDevicesUserControl()))
								check(((UserDevicesUserControl)form.Controls[0]).DevicesDataGridView, "Devices page");
						}
						finally
						{
							foreach (var d in devices)
								x360ce.App.SettingsManager.UserDevices.Items.Remove(d);
						}
						RawInputInterfaceTest.ShowControllerTab(devices, (pad, rows) => check(pad.MappedDevicesDataGridView, "Controller 1"));
						if (unavailable.Count == 0)
							faults.Add(theme + ": no unavailable row was found, so this proves nothing.");
					}
				}
				finally
				{
					FormsTheme.SetTheme(ThemeType.Light);
				}
			}, "Reading the unavailable rows' colours");
			Assert.IsTrue(counted >= 20, "Only " + counted + " cells of unavailable rows were looked at.");
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s):" + Environment.NewLine + string.Join(Environment.NewLine, faults.Distinct()));
		}

		/// <summary>The pages and windows that show coloured text, links or charts, each made as the program makes it.</summary>
		static readonly Func<Control>[] Surfaces =
		{
			() => new PadControl(MapTo.Controller1),
			() => new OptionsUserControl(),
			() => new GamesGridUserControl(),
			() => new UserDevicesUserControl(),
			() => new XInputDevicesUserControl(),
			() => new SettingsGridUserControl(),
			() => new ProgramsGridUserControl(),
			() => IssuesWithLinksAndAllOk(),
			() => new AboutControl(),
			() => new HardwareForm(),
		};

		[TestMethod, TestCategory("ui")]
		[Description("A window switched to the dark theme while open shows every text, link, tree node, list cell and chart line in a dark theme colour that reads on what it stands on, on every page")]
		public void Dark_windows_show_text_and_links_in_the_dark_colours()
		{
			var faults = new List<string>();
			var counted = 0;
			var charts = 0;
			Ui.OnUiThread(() =>
			{
				var field = typeof(App.Properties.Resources).GetField("resourceMan", BindingFlags.NonPublic | BindingFlags.Static);
				var before = field.GetValue(null);
				var forms = new List<Form>();
				try
				{
					ThemeResourceManager.Install(typeof(App.Properties.Resources), "x360ce.App.Properties.Icons");
					// Made and filled in the light theme, then switched to dark, as Options > Appearance > Theme does.
					FormsTheme.SetTheme(ThemeType.Light);
					foreach (var make in Surfaces)
						forms.Add(Show(make()));
					WaitForTree(forms);
					foreach (var panel in forms.SelectMany(ButtonIconTest.All).Where(x => x.Name == nameof(IssuesUserControl.NoIssuesPanel)))
						panel.Visible = true;
					// A row with a link selected, as the Issues tab selects its first row.
					foreach (var grid in forms.SelectMany(ButtonIconTest.All).OfType<DataGridView>())
					{
						var linked = grid.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => r.Cells.OfType<DataGridViewLinkCell>().Any(c => c.Value != null));
						if (linked == null)
							continue;
						grid.ClearSelection();
						linked.Selected = true;
					}
					FormsTheme.SetTheme(ThemeType.Dark);
					Application.DoEvents();
					foreach (var form in forms)
						counted += CheckTabs(form, form.Name, faults);
					foreach (var chart in forms.SelectMany(ButtonIconTest.All).Where(x => x is AxisMapUserControl || x is WheelChartUserControl))
					{
						counted += CheckChart(chart, faults);
						charts++;
					}
				}
				finally
				{
					foreach (var form in forms)
						form.Dispose();
					FormsTheme.SetTheme(ThemeType.Light);
					field.SetValue(null, before);
				}
			}, "Looking at the dark windows' text");
			TestContext.WriteLine(counted + " colours looked at, " + charts + " charts among them.");
			Assert.IsTrue(counted > 100, "Only " + counted + " colours were looked at, so this proves little.");
			Assert.IsTrue(charts >= 6, "Only " + charts + " charts were looked at: the axis pages and the wheel page should give six or more.");
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s):" + Environment.NewLine + string.Join(Environment.NewLine, faults.Distinct()));
		}

		[TestMethod, TestCategory("ui")]
		[Description("A link in a help document, its words an address or not, is drawn in the theme's link colour, not Windows' own, and opens its address when it is clicked")]
		public void Help_links_take_the_theme_colour_and_still_open()
		{
			var clicked = new List<string>();
			var faults = new List<string>();
			Ui.OnUiThread(() =>
			{
				try
				{
					FormsTheme.SetTheme(ThemeType.Dark);
					using (var form = new Form { ClientSize = new Size(600, 200), StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), ShowInTaskbar = false })
					{
						// As the program's help boxes show their documents.
						var box = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None };
						form.Controls.Add(box);
						FormsTheme.Apply(form);
						form.Show();
						MarkdownRtf.Show(box, "Visit <https://www.x360ce.com> or [the wiki](https://github.com/x360ce/x360ce/wiki).", clicked.Add);
						Application.DoEvents();
						var link = FormsTheme.GetColor("ForegroundLink");
						// What Windows draws a link it knows of in, whatever colour the document gives it.
						var windowsLink = ColorTranslator.FromWin32(GetSysColor(COLOR_HOTLIGHT));
						using (var drawn = Capture(box))
							foreach (var words in new[] { "https://www.x360ce.com", "the wiki" })
							{
								var index = box.Text.IndexOf(words, StringComparison.Ordinal);
								var from = box.GetPositionFromCharIndex(index);
								var to = box.GetPositionFromCharIndex(index + words.Length);
								var area = Rectangle.FromLTRB(from.X, from.Y, to.X, from.Y + box.Font.Height);
								var theme = Count(drawn, area, link);
								var windows = Count(drawn, area, windowsLink);
								if (theme == 0 || windows > 0)
									faults.Add(string.Format("'{0}' is drawn with {1} pixels of the theme's link colour {2} and {3} of Windows' {4}.",
										words, theme, Hex(link), windows, Hex(windowsLink)));
								var lParam = (IntPtr)((from.Y + box.Font.Height / 2) << 16 | (from.X + 2));
								SendMessage(box.Handle, WM_LBUTTONDOWN, (IntPtr)1, lParam);
								SendMessage(box.Handle, WM_LBUTTONUP, IntPtr.Zero, lParam);
								Application.DoEvents();
							}
					}
				}
				finally
				{
					FormsTheme.SetTheme(ThemeType.Light);
				}
			}, "Clicking help links");
			Assert.AreEqual(0, faults.Count, string.Join(Environment.NewLine, faults));
			CollectionAssert.AreEqual(new[] { "https://www.x360ce.com", "https://github.com/x360ce/x360ce/wiki" }, clicked,
				"Clicked links: " + string.Join(", ", clicked));
		}

		/// <summary>The box as Windows draws it.</summary>
		/// <remarks>
		/// PrintWindow takes a child window's picture from the screen's compositor, which receives the box's drawing a frame
		/// after the box draws it, so a picture taken at once can still be blank. This takes one until something is drawn in it,
		/// for two seconds at most.
		/// </remarks>
		internal static Bitmap Capture(Control control)
		{
			control.Refresh();
			for (var attempt = 0; ; attempt++)
			{
				DwmFlush();
				var bitmap = new Bitmap(control.Width, control.Height);
				using (var g = Graphics.FromImage(bitmap))
				{
					var hdc = g.GetHdc();
					PrintWindow(control.Handle, hdc, PW_RENDERFULLCONTENT);
					g.ReleaseHdc(hdc);
				}
				if (attempt == 120 || IsDrawn(bitmap, control.BackColor))
					return bitmap;
				bitmap.Dispose();
				Application.DoEvents();
			}
		}

		/// <summary>True when the picture holds anything besides the background, or the black of nothing captured.</summary>
		static bool IsDrawn(Bitmap bitmap, Color background)
		{
			for (var y = 0; y < bitmap.Height; y++)
				for (var x = 0; x < bitmap.Width; x++)
				{
					var p = bitmap.GetPixel(x, y);
					if (p.ToArgb() != background.ToArgb() && p.ToArgb() != Color.Black.ToArgb())
						return true;
				}
			return false;
		}

		/// <summary>How many pixels of the area are the colour, give or take the smoothing of the letters' cores.</summary>
		static int Count(Bitmap bitmap, Rectangle area, Color color)
		{
			var count = 0;
			area.Intersect(new Rectangle(Point.Empty, bitmap.Size));
			for (var y = area.Top; y < area.Bottom; y++)
				for (var x = area.Left; x < area.Right; x++)
				{
					var p = bitmap.GetPixel(x, y);
					if (Math.Abs(p.R - color.R) + Math.Abs(p.G - color.G) + Math.Abs(p.B - color.B) <= 24)
						count++;
				}
			return count;
		}

		const int WM_LBUTTONDOWN = 0x0201;
		const int WM_LBUTTONUP = 0x0202;
		const int COLOR_HOTLIGHT = 26;
		const uint PW_RENDERFULLCONTENT = 2;

		[System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
		static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		static extern int GetSysColor(int index);

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		static extern bool PrintWindow(IntPtr hWnd, IntPtr hdc, uint flags);

		/// <summary>Waits until the compositor has taken in what was drawn before the call.</summary>
		[System.Runtime.InteropServices.DllImport("dwmapi.dll")]
		static extern int DwmFlush();

		#region Purposes

		/// <summary>
		/// The source files that may take each text or chart colour key, and what for. A key used anywhere else is taken for a
		/// purpose that is not its own, or for a new place that wants its purpose checked and written here.
		/// </summary>
		static readonly Dictionary<string, string[]> Purposes = new Dictionary<string, string[]>
		{
			// An error, a failure or a critical issue.
			{ "ForegroundWarning", new[]
			{
				"Engine/JocysCom/IO/HardwareControl.cs", // a device Windows hides
				"Engine/Common/MarkdownRtf.cs", // a warning in a help document
				"App.v4/MainForm.cs", // error reports waiting
				"App.v4/Forms/BaseFormWithHeader.cs", // an error in the header
				"App.v4/Controls/OptionsUserControl.cs", // a hotkey Windows refused
				"App.v4/Controls/MapExpressionToggle.cs", // a formula that does not work
				"App.v3/MainForm.cs", // an error in the header
			} },
			// An issue that needs attention but is not an error.
			{ "ForegroundCaution", new[] { "App.v4/Controls/PadTabPages/SdlUserControl.cs" } }, // a box that differs from SDL's map
			// A list row whose item is unplugged or switched off, given to the grids' shared dimming.
			// A status or a notice.
			{ "ForegroundInformation", new[]
			{
				"App.v4/Forms/BaseFormWithHeader.cs", // information in the header
				"App.v4/Controls/PadTabPages/General/XboxImageUserControl.cs", // what to press while mapping
				"App.v3/MainForm.cs", // information in the header
			} },
			// Something that succeeded.
			{ "ForegroundSuccess", new[]
			{
				"Engine/JocysCom/Controls/IssuesControl/IssuesUserControl.cs", // All OK
				"App.v4/Controls/MapExpressionToggle.cs", // a formula that works
				"App.v4/Controls/PadTabPages/General/XboxImageUserControl.cs", // Mapping Done
			} },
			// A link.
			{ "ForegroundLink", new[] { "Engine/JocysCom/Controls/Themes/Default.Forms.cs", "Engine/Common/MarkdownRtf.cs" } },
			// What a controller does now: a pressed control's name, a value read from it, a chart's line at the reading.
			{ "ForegroundActive", new[]
			{
				"App.v4/Controls/PadControl.cs",
				"App.v4/Controls/PadTabPages/General/PadControlImager.cs",
				"App.v4/Controls/AxisToButtonUserControl.cs",
				"App.v4/Controls/AxisMapUserControl.cs",
				"App.v4/Controls/PadTabPages/WheelChartUserControl.cs",
				"App.v3/Controls/PadControl.cs",
				"App.v3/Controls/AxisToButtonUserControl.cs",
				"App.v3/Controls/ThumbUserControl.cs",
			} },
			// What a chart's curve gives for the reading, and the line at it.
			{ "ForegroundOutput", new[]
			{
				"App.v4/Controls/AxisMapUserControl.cs",
				"App.v4/Controls/PadTabPages/WheelChartUserControl.cs",
				"App.v3/Controls/ThumbUserControl.cs",
			} },
			// A value as it is typed, and the name of a button to use, in a help document.
			{ "ForegroundCode", new[] { "Engine/Common/MarkdownRtf.cs" } },
			{ "ForegroundCommand", new[] { "Engine/Common/MarkdownRtf.cs" } },
			// A chart's curve.
			{ "ColorCurve", new[]
			{
				"App.v4/Controls/AxisMapUserControl.cs",
				"App.v4/Controls/PadTabPages/WheelChartUserControl.cs",
				"App.v3/Controls/ThumbUserControl.cs",
			} },
			// A control at rest and a control in use, as the controller picture marks them; and progress made.
			{ "ColorNormal", new[] { "App.v4/Controls/PadTabPages/AxisToDPadChart.cs" } },
			{ "ColorActive", new[] { "App.v4/Controls/PadTabPages/AxisToDPadChart.cs", "Engine/JocysCom/Controls/Themes/Default.Forms.cs" } },
			// The brand is the program's marks, which Windows Forms code does not draw.
			{ "ColorBrand", new string[0] },
			{ "ColorBrandCyan", new string[0] },
			{ "ColorBrandViolet", new string[0] },
			{ "ColorBrandHighlight", new string[0] },
		};

		/// <summary>A colour key read by the theme's helpers.</summary>
		static readonly Regex KeyUse = new Regex(@"\b(?:GetColor|Themed)\(\s*""(?<key>\w+)""");

		[TestMethod, TestCategory("ui")]
		[Description("Each text and chart colour key is taken only where its own purpose is shown, and the link colour only for a link")]
		public void Each_colour_key_is_used_only_for_its_own_purpose()
		{
			var faults = new List<string>();
			var root = Ui.RepoRoot.FullName;
			var uses = 0;
			foreach (var folder in new[] { "App.v4", "App.v3", "Engine" })
				foreach (var file in Directory.EnumerateFiles(Path.Combine(root, folder), "*.cs", SearchOption.AllDirectories))
				{
					var relative = file.Substring(root.Length + 1).Replace('\\', '/');
					if (relative.Contains("/bin/") || relative.Contains("/obj/"))
						continue;
					var lines = File.ReadAllLines(file);
					for (var i = 0; i < lines.Length; i++)
						foreach (Match m in KeyUse.Matches(lines[i]))
						{
							var key = m.Groups["key"].Value;
							string[] files;
							if (!Purposes.TryGetValue(key, out files))
								continue;
							uses++;
							var where = relative + ":" + (i + 1) + " " + key;
							if (!files.Contains(relative))
								faults.Add(where + ": not one of the places this key is for (" + (files.Length == 0 ? "none" : string.Join(", ", files)) + ").");
							// The link colour sets a link's colour and nothing else.
							if (key == "ForegroundLink" && !Regex.IsMatch(lines[i].Replace("\"ForegroundLink\"", ""), @"\b\w*Link\w*\s*="))
								faults.Add(where + ": the link colour is given to something that is not a link: " + lines[i].Trim());
						}
				}
			TestContext.WriteLine(uses + " uses of the keys looked at.");
			Assert.IsTrue(uses > 20, "Only " + uses + " uses were found, so the search looked in the wrong place.");
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s):" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		#endregion

		/// <summary>The Issues page with an issue that has a link and one that is a problem; the "All OK" note is shown once it is open.</summary>
		static Control IssuesWithLinksAndAllOk()
		{
			var control = new IssuesUserControl { Name = "Issues" };
			control.Warnings.Add(new IssueItem { Name = "Linked", Description = "An issue with a link.", MoreInfo = new Uri("https://www.x360ce.com"), FixName = "Fix", Severity = IssueSeverity.Moderate });
			control.Warnings.Add(new IssueItem { Name = "Critical", Description = "An issue.", FixName = "Fix", Severity = IssueSeverity.Critical });
			return control;
		}

		/// <summary>Shows a page in a window of its own, or a window as it is, themed, off the screens.</summary>
		static Form Show(Control made)
		{
			var form = made as Form ?? new Form { ClientSize = made.Size, Name = made.Name.Length > 0 ? made.Name : made.GetType().Name };
			form.StartPosition = FormStartPosition.Manual;
			form.Location = new Point(-5000, -5000);
			form.ShowInTaskbar = false;
			if (form != made)
			{
				made.Dock = DockStyle.Fill;
				form.Controls.Add(made);
			}
			FormsTheme.Apply(form);
			form.Show();
			Application.DoEvents();
			return form;
		}

		/// <summary>Waits for the Hardware window to list the devices, which it does a moment after it opens, and the lists their rows.</summary>
		static void WaitForTree(List<Form> forms)
		{
			var tree = forms.SelectMany(ButtonIconTest.All).OfType<TreeView>().First();
			for (var i = 0; i < 200 && tree.Nodes.Count == 0; i++)
			{
				Application.DoEvents();
				Thread.Sleep(50);
			}
			Assert.IsTrue(tree.Nodes.Count > 0, "The Hardware window listed no devices, so its tree proves nothing.");
			for (var i = 0; i < 10; i++)
			{
				Application.DoEvents();
				Thread.Sleep(50);
			}
		}

		/// <summary>Looks at a control's text and what it holds, each tab page of a tab strip in turn, shown.</summary>
		/// <returns>How many colours were looked at.</returns>
		static int CheckTabs(Control parent, string where, List<string> faults)
		{
			var counted = 0;
			foreach (Control control in parent.Controls)
			{
				if (control is TabControl tabs)
				{
					var selected = tabs.SelectedTab;
					foreach (TabPage page in tabs.TabPages)
					{
						tabs.SelectedTab = page;
						Application.DoEvents();
						counted += CheckTabs(page, where, faults);
					}
					tabs.SelectedTab = selected;
					continue;
				}
				if (!control.Visible)
					continue;
				counted += CheckControl(control, where, faults);
				counted += CheckTabs(control, where, faults);
			}
			return counted;
		}

		/// <summary>Looks at the colours one control draws its own text in.</summary>
		static int CheckControl(Control control, string where, List<string> faults)
		{
			var name = where + " " + ButtonIconTest.Path(control);
			var back = Behind(control);
			var counted = 0;
			if (control is LinkLabel link)
			{
				counted += Expect(faults, name + " link", link.LinkColor, "its background", back);
				counted += Expect(faults, name + " visited link", link.VisitedLinkColor, "its background", back);
			}
			else if (!string.IsNullOrEmpty(control.Text) && (control is Label || control is TextBoxBase || control is GroupBox || control is CheckBox || control is RadioButton))
				counted += Expect(faults, name, control.ForeColor, "its background", back);
			if (control is RichTextBox rich)
				counted += CheckRichText(rich, name, faults);
			if (control is ToolStrip strip)
				foreach (ToolStripItem item in strip.Items)
				{
					if (!item.Available || item is ToolStripSeparator || string.IsNullOrEmpty(item.Text) || item.DisplayStyle == ToolStripItemDisplayStyle.Image)
						continue;
					counted += Expect(faults, name + "." + item.Name, item.ForeColor, "its tool bar", Behind(strip));
					if (item is ToolStripLabel label && label.IsLink)
						counted += Expect(faults, name + "." + item.Name + " link", label.LinkColor, "its tool bar", Behind(strip));
				}
			if (control is TreeView tree)
				counted += CheckNodes(tree.Nodes, tree.BackColor, name, faults);
			if (control is DataGridView grid)
				counted += CheckCells(grid, name, faults);
			return counted;
		}

		/// <summary>
		/// The lines and dots a chart draws over its picture, and its curve, which must read on the chart's box: every pen and
		/// brush the chart keeps that is not a faint grid or band, and the colour of the curve in the picture.
		/// </summary>
		static int CheckChart(Control chart, List<string> faults)
		{
			var name = ButtonIconTest.Path(chart);
			var box = SystemColors.Window;
			var counted = 0;
			foreach (var f in chart.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
			{
				var value = f.GetValue(chart);
				var color = value is Pen pen ? pen.Color : value is SolidBrush brush ? brush.Color : Color.Empty;
				if (color.IsEmpty || color.A < 255)
					continue;
				counted += Expect(faults, name + " " + f.Name, color, "the chart", box, GraphicContrast);
			}
			// The curve is drawn into the picture on another thread, after the theme changed.
			if (chart is AxisMapUserControl)
			{
				var curve = FormsTheme.GetColor("ColorCurve");
				var picture = typeof(AxisMapUserControl).GetField("LastBackgroundImage", BindingFlags.Instance | BindingFlags.NonPublic);
				// Smoothed onto a clear picture, a colour may come back a few steps off in a channel.
				Func<Color, bool> isCurve = c => !c.IsEmpty && Math.Abs(c.R - curve.R) <= 4 && Math.Abs(c.G - curve.G) <= 4 && Math.Abs(c.B - curve.B) <= 4;
				var drawn = Color.Empty;
				for (var i = 0; i < 100 && !isCurve(drawn); i++)
				{
					Application.DoEvents();
					Thread.Sleep(20);
					drawn = CurveColour((Bitmap)picture.GetValue(chart));
				}
				if (!isCurve(drawn))
					faults.Add(name + " curve: drawn " + Hex(drawn) + ", not the theme's curve colour " + Hex(curve) + ".");
				counted += Expect(faults, name + " curve", drawn, "the chart", box, GraphicContrast);
			}
			return counted;
		}

		/// <summary>The colour most of the picture's solid pixels have: the curve, as the grid is faint and the rest clear.</summary>
		static Color CurveColour(Bitmap picture)
		{
			if (picture == null)
				return Color.Empty;
			var counts = new Dictionary<int, int>();
			for (var y = 0; y < picture.Height; y++)
				for (var x = 0; x < picture.Width; x++)
				{
					var p = picture.GetPixel(x, y);
					if (p.A < 128)
						continue;
					var rgb = p.ToArgb() | unchecked((int)0xFF000000);
					int n;
					counts.TryGetValue(rgb, out n);
					counts[rgb] = n + 1;
				}
			return counts.Count == 0 ? Color.Empty : Color.FromArgb(counts.OrderByDescending(x => x.Value).First().Key);
		}

		/// <summary>The colours a rich text box's document names, which must read on the box, and its links shown in them.</summary>
		static int CheckRichText(RichTextBox box, string name, List<string> faults)
		{
			var counted = 0;
			if (box.Text.Contains("://") && box.DetectUrls)
				faults.Add(name + ": finds addresses itself, so its links are drawn in the Windows link colour, not the theme's.");
			var table = Regex.Match(box.Rtf, @"\{\\colortbl ;([^}]*)\}");
			foreach (Match m in Regex.Matches(table.Groups[1].Value, @"\\red(\d+)\\green(\d+)\\blue(\d+);"))
			{
				var color = Color.FromArgb(int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
					int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture), int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture));
				counted += Expect(faults, name + " document colour", color, "the box", box.BackColor);
			}
			return counted;
		}

		static int CheckNodes(TreeNodeCollection nodes, Color back, string name, List<string> faults)
		{
			var counted = 0;
			foreach (TreeNode node in nodes)
			{
				if (!node.ForeColor.IsEmpty)
					counted += Expect(faults, name + " node '" + node.Text + "'", node.ForeColor, "the tree", back);
				counted += CheckNodes(node.Nodes, back, name, faults);
			}
			return counted;
		}

		static readonly MethodInfo GetFormattedValueMethod = typeof(DataGridViewCell).GetMethod("GetFormattedValue",
			BindingFlags.Instance | BindingFlags.NonPublic, null,
			new[] { typeof(object), typeof(int), typeof(DataGridViewCellStyle).MakeByRefType(), typeof(System.ComponentModel.TypeConverter), typeof(System.ComponentModel.TypeConverter), typeof(DataGridViewDataErrorContexts) }, null);

		/// <summary>The style a cell's text is drawn in, as the list formats it; null for a cell that shows no text.</summary>
		static DataGridViewCellStyle TextStyle(DataGridViewCell cell)
		{
			// Formatted twice: a list that dims a row writes the cell's own style while it formats it.
			DataGridViewCellStyle style = null;
			for (var i = 0; i < 2; i++)
			{
				var args = new object[] { cell.Value, cell.RowIndex, cell.InheritedStyle, null, null, DataGridViewDataErrorContexts.Formatting | DataGridViewDataErrorContexts.Display };
				var value = GetFormattedValueMethod.Invoke(cell, args);
				style = (DataGridViewCellStyle)args[2];
				if (!(value is string text) || text.Length == 0)
					style = null;
			}
			return style;
		}

		/// <summary>Each cell's text, as the list formats it, on its row and on the row selected; a link in its link colour on both.</summary>
		static int CheckCells(DataGridView grid, string name, List<string> faults)
		{
			var counted = 0;
			foreach (DataGridViewRow row in grid.Rows)
				foreach (DataGridViewCell cell in row.Cells)
				{
					if (!cell.Visible || cell.ColumnIndex < 0)
						continue;
					var style = TextStyle(cell);
					if (style == null)
						continue;
					var where = name + " row " + row.Index + " " + grid.Columns[cell.ColumnIndex].Name;
					if (cell is DataGridViewLinkCell link)
					{
						var selected = (cell.InheritedState & DataGridViewElementStates.Selected) != 0;
						counted += selected
							? Expect(faults, where + " selected link", link.LinkColor, "the selected row", style.SelectionBackColor)
							: Expect(faults, where + " link", link.LinkColor, "the row", style.BackColor);
						continue;
					}
					counted += Expect(faults, where, style.ForeColor, "the row", style.BackColor);
					counted += Expect(faults, where + " selected", style.SelectionForeColor, "the selected row", style.SelectionBackColor);
				}
			return counted;
		}

		/// <summary>The colour behind a control's text: its own, or that of the first control under it that is not see-through.</summary>
		static Color Behind(Control control)
		{
			for (var c = control; c != null; c = c.Parent)
				if (c.BackColor.A == 255)
					return c.BackColor;
			return SystemColors.Control;
		}

		/// <summary>
		/// Adds a fault when a colour does not read on its background, or while the theme is dark is a light theme colour or a
		/// fixed named colour, as Color.Red, which no theme gives.
		/// </summary>
		/// <returns>1, as one colour was looked at.</returns>
		static int Expect(List<string> faults, string what, Color color, string onWhat, Color back, double contrast = TextContrast)
		{
			var ratio = Contrast(color, back);
			if (ratio < contrast)
				faults.Add(string.Format(CultureInfo.InvariantCulture, "{0}: {1} on {2} {3} is {4:0.00}:1, below {5}:1.",
					what, Hex(color), onWhat, Hex(back), ratio, contrast));
			if (FormsTheme.IsDark)
			{
				foreach (var role in Roles.Skip(1).Concat(ChartRoles).Distinct())
					if (color.ToArgb() == FormsTheme.LightPalette[role].ToArgb() && color.ToArgb() != FormsTheme.Palette[role].ToArgb())
						faults.Add(what + ": " + Hex(color) + " is the light theme's " + role + " in the dark theme.");
				if (color.IsNamedColor && !color.IsSystemColor)
					faults.Add(what + ": " + color.Name + " is a fixed colour, the same in both themes.");
			}
			return 1;
		}

		static string Hex(Color c)
		{
			return string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
		}

		/// <summary>The WCAG 2.2 contrast ratio of two colours, from 1 to 21.</summary>
		internal static double Contrast(Color a, Color b)
		{
			var la = Luminance(a);
			var lb = Luminance(b);
			return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
		}

		/// <summary>The WCAG 2.2 relative luminance of a colour.</summary>
		static double Luminance(Color c)
		{
			Func<int, double> channel = v =>
			{
				var s = v / 255.0;
				return s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
			};
			return 0.2126 * channel(c.R) + 0.7152 * channel(c.G) + 0.0722 * channel(c.B);
		}
	}
}
