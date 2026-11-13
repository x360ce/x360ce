// @under-test: App.v4/Controls/PadControl.cs, App.v4/Controls/PadControl.Designer.cs, App.v4/Common/ImageInfo.cs, App.v4/Common/UiTree/UiCatalog.Pad.cs, App.v4/Controls/PadTabPages/General/InputChipGroup.cs
// @area: ui   @layer: unit
using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.Themes;
using JocysCom.ClassLibrary.Controls.UiTree;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// Under each name on a controller's General tab a small green number says what the game is being given for that part,
	/// read from the state the controller picture is drawn from.
	/// </summary>
	[TestClass]
	public class PadValueLabelsTest
	{
		public TestContext TestContext { get; set; }

		[TestMethod, TestCategory("ui"), TestCategory("interface")]
		[Description("Every name on the General tab has a small green number under it, both at the edge next to its box and the number 2 px in from it at 100 %, its digits 4 px under the name's baseline at 100 % with the empty top of the number's line under the name and every digit whole, the names of a column lined up at that edge, the pair in the middle of its box's row and clear of every other control, in both themes, in Windows' fonts and in the program's")]
		public void General_tab_shows_a_value_under_each_name()
		{
			AppFontsTest.LoadFonts();
			Ui.OnUiThread(() =>
			{
				foreach (var programFonts in new[] { false, true })
				{
					if (programFonts)
						FormsTheme.SetFonts(AppFonts.InterfaceFamilyFor(AppFontsTest.Chosen), AppFonts.CodeFamily, AppFonts.SizeFor(0));
					try
					{
						using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
						using (var pad = new PadControl(MapTo.Controller1) { Dock = DockStyle.Fill })
						{
							form.ClientSize = pad.Size;
							form.Controls.Add(pad);
							FormsTheme.SetTheme(ThemeType.Light);
							FormsTheme.Apply(form);
							form.Show();
							Application.DoEvents();
							foreach (var theme in new[] { ThemeType.Light, ThemeType.Dark })
							{
								FormsTheme.SetTheme(theme);
								FormsTheme.Apply(form);
								Application.DoEvents();
								var where = (programFonts ? "In the program's fonts" : "In Windows' fonts") + ", " + theme + ": ";
								var green = FormsTheme.GetColor("ForegroundActive");
								var table = (TableLayoutPanel)Find(pad, nameof(PadControl.GeneralTableLayoutPanel));
								Func<Control, Rectangle> at = c => table.RectangleToClient(c.Parent.RectangleToScreen(c.Bounds));
								// The owner's 2 px at 100 %, which the page scales as it scales its other sizes.
								var inset = (int)Math.Round(2 * ControlsHelper.DpiScale, MidpointRounding.AwayFromZero);
								var valueGap = (int)Math.Round(4 * ControlsHelper.DpiScale, MidpointRounding.AwayFromZero);
								var edges = new Dictionary<int, HashSet<int>>();
								var gaps = new SortedSet<int>();
								var overlaps = new SortedSet<int>();
								foreach (var code in Codes())
								{
									var name = (Label)Find(pad, code + "Label");
									var value = (Label)Find(pad, code + "ValueLabel");
									var box = Find(pad, code + "ComboBox");
									Assert.AreSame(name.Parent, value.Parent, where + code + "'s number is not beside its name.");
									Assert.IsTrue(value.Visible, where + code + "'s number is hidden.");
									// The empty top of the number's line lies under its name, so its digits stand the owner's 4 px under the
									// name's baseline at 100 %, 6 at 150 %, counting every row either draws ink on; further only where a font
									// leaves fewer rows empty above its digits than that takes, as the name never lies over a digit.
									int capTop, baseline, digitsTop, digitsBottom;
									ControlOverlapTest.InkRows(name.Font, "H", out capTop, out baseline);
									ControlOverlapTest.InkRows(value.Font, ControlOverlapTest.Digits, out digitsTop, out digitsBottom);
									var overlap = name.Bottom - value.Top;
									var gap = value.Top + digitsTop - (name.Top + baseline) - 1;
									Assert.IsTrue(overlap <= digitsTop, string.Format("{0}{1}'s name lies {2} px over its number, whose digits start {3} px down.",
										where, code, overlap, digitsTop));
									Assert.IsTrue(gap == valueGap || gap > valueGap && overlap == digitsTop, string.Format(
										"{0}{1}'s digits stand {2} clear rows under its name's baseline, not {3}.", where, code, gap, valueGap));
									Assert.IsTrue(name.Parent.Controls.GetChildIndex(name) < name.Parent.Controls.GetChildIndex(value),
										where + code + "'s number is drawn over its name.");
									Assert.IsTrue(digitsBottom < value.Height, string.Format("{0}{1}'s digits draw on rows {2} to {3} of its number, {4} high.",
										where, code, digitsTop, digitsBottom, value.Height));
									gaps.Add(gap);
									overlaps.Add(overlap);
									// A name before its box, on the right of the page, ends next to the box; a name after its box starts next to it.
									var column = table.GetColumn(name.Parent);
									var right = column < table.GetColumn(box);
									if (right)
									{
										Assert.AreEqual(ContentAlignment.TopRight, name.TextAlign, where + code + "'s name is not drawn at its right edge.");
										Assert.AreEqual(name.Right - name.Padding.Right - inset, value.Right - value.Padding.Right,
											where + code + "'s number does not end " + inset + " px in from its name's end.");
										Assert.AreEqual(ContentAlignment.TopRight, value.TextAlign, where + code + "'s number is not drawn at its right edge.");
									}
									else
									{
										Assert.AreEqual(ContentAlignment.TopLeft, name.TextAlign, where + code + "'s name is not drawn at its left edge.");
										Assert.AreEqual(name.Left + name.Padding.Left + inset, value.Left + value.Padding.Left,
											where + code + "'s number does not start " + inset + " px in from its name's start.");
										Assert.AreEqual(ContentAlignment.TopLeft, value.TextAlign, where + code + "'s number is not drawn at its left edge.");
									}
									HashSet<int> edge;
									if (!edges.TryGetValue(column, out edge))
										edges[column] = edge = new HashSet<int>();
									edge.Add(right ? at(name).Right : at(name).Left);
									Assert.AreEqual(name.Font.FontFamily.Name, value.Font.FontFamily.Name, where + code + "'s number is in another family than its name.");
									Assert.IsTrue(value.Font.Size < name.Font.Size, where + code + "'s number is not smaller than its name.");
									Assert.AreEqual(green.ToArgb(), value.ForeColor.ToArgb(), where + code + "'s number is not the theme's active colour.");
									var contrast = Contrast(value.ForeColor, Behind(value));
									Assert.IsTrue(contrast >= 4.5, string.Format("{0}{1}'s number has a contrast of {2:0.00}, under 4.5.", where, code, contrast));
									var offCentre = (at(name).Top + at(value).Bottom) / 2.0 - (box.Top + box.Bottom) / 2.0;
									Assert.IsTrue(Math.Abs(offCentre) <= 1, string.Format("{0}{1}'s name and number are {2} px off the middle of its box's row.", where, code, offCentre));
									var height = table.ClientSize.Height;
									Assert.IsTrue(at(name).Top >= 0 && at(value).Bottom <= height && box.Bottom <= height, string.Format(
										"{0}{1}'s row runs past the bottom of its table, {2} high: box {3}, name {4}, number {5}.", where, code, height, box.Bounds, at(name), at(value)));
								}
								foreach (var pair in edges)
									Assert.AreEqual(1, pair.Value.Count, string.Format("{0}the names in column {1} do not line up at the edge next to their boxes: {2}.",
										where, pair.Key, string.Join(", ", pair.Value)));
								TestContext.WriteLine("{0}{1} clear row(s) from a name's baseline to its number's digits, the number's top {2} row(s) under the name, at {3:P0}.",
									where, string.Join("/", gaps), string.Join("/", overlaps), ControlsHelper.DpiScale);
								var covered = ControlOverlapTest.Covered(table).ToList();
								var rows = Codes().Select(x => string.Format("  {0}: box {1}, name {2}, number {3}",
									x, Find(pad, x + "ComboBox").Bounds, Find(pad, x + "Label").Bounds, Find(pad, x + "ValueLabel").Bounds));
								Assert.AreEqual(0, covered.Count, string.Format("{0}{1} control(s) are covered by another:{2}{3}{2}The rows:{2}{4}",
									where, covered.Count, Environment.NewLine, string.Join(Environment.NewLine, covered), string.Join(Environment.NewLine, rows)));
							}
						}
					}
					finally
					{
						if (programFonts)
							FormsTheme.SetFonts(null, null);
						FormsTheme.SetTheme(ThemeType.Light);
					}
				}
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("Every name on the General tab stands the same number of ink-free pixel rows above the digits under it, 4 at 100 % and 6 at 150 %, counted in the page's own drawing with any pixel unlike the background as ink, in both columns and both themes, in the program's font drawn as the program draws it, with ClearType; names and digits are drawn whole")]
		public void General_tab_names_stand_the_same_ink_free_rows_above_their_numbers()
		{
			AppFontsTest.LoadFonts();
			var faults = new List<string>();
			var gaps = new SortedSet<int>();
			var expected = PadControl.GapAt(ControlsHelper.DpiScale);
			Ui.OnUiThread(() =>
			{
				FormsTheme.SetFonts(AppFonts.InterfaceFamilyFor(AppFontsTest.Chosen), AppFonts.CodeFamily, AppFonts.SizeFor(0));
				try
				{
					using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
					using (var pad = new PadControl(MapTo.Controller1) { Dock = DockStyle.Fill })
					{
						// The program draws its labels with GDI (Application.SetCompatibleTextRenderingDefault(false)), which the
						// test host cannot ask for once a window exists.
						foreach (var label in ButtonIconTest.All(pad).OfType<Label>())
							label.UseCompatibleTextRendering = false;
						// Sized again, as the page sizes each pair, now that the names are measured as they are drawn.
						foreach (var code in Codes())
							PadControl.FitPair((Label)Find(pad, code + "Label"), (Label)Find(pad, code + "ValueLabel"), expected);
						form.ClientSize = pad.Size;
						form.Controls.Add(pad);
						foreach (var theme in new[] { ThemeType.Light, ThemeType.Dark })
						{
							FormsTheme.SetTheme(theme);
							FormsTheme.Apply(form);
							form.Show();
							Application.DoEvents();
							foreach (var code in Codes())
							{
								var at = theme + ", " + code + ": ";
								var rows = InkFreeRows((Label)Find(pad, code + "Label"), (Label)Find(pad, code + "ValueLabel"), at, faults);
								gaps.Add(rows);
								if (rows != expected)
									faults.Add(string.Format("{0}{1} ink-free rows between the name and its digits, not {2}.", at, rows, expected));
							}
						}
					}
				}
				finally
				{
					FormsTheme.SetFonts(null, null);
					FormsTheme.SetTheme(ThemeType.Light);
				}
			});
			TestContext.WriteLine("{0} ink-free row(s) between a name and its digits at {1:P0}, {2} expected.", string.Join("/", gaps), ControlsHelper.DpiScale, expected);
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s):" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("A name and the number under it, built and sized as the General tab builds them, in Segoe UI at the size each Windows scale gives its 9 points, stand 4, 5, 6, 7 and 8 ink-free pixel rows apart at 100, 125, 150, 175 and 200 %, for every name of the General tab, in both themes, names and digits drawn whole")]
		public void Names_stand_four_ink_free_rows_a_hundred_per_cent_above_their_numbers_at_every_scale()
		{
			var faults = new List<string>();
			var report = new List<string>();
			Ui.OnUiThread(() =>
			{
				List<Tuple<string, bool, ContentAlignment>> names;
				using (var pad = new PadControl(MapTo.Controller1))
					names = Codes().Select(x => (Label)Find(pad, x + "Label")).Select(x => Tuple.Create(x.Text, x.UseMnemonic, x.TextAlign)).ToList();
				try
				{
					foreach (var scale in new[] { 1f, 1.25f, 1.5f, 1.75f, 2f })
						foreach (var theme in new[] { ThemeType.Light, ThemeType.Dark })
						{
							FormsTheme.SetTheme(theme);
							// What the program's 9 point Segoe UI is at that scale, in pixels, and its numbers' four fifths of it.
							using (var nameFont = new Font("Segoe UI", 12f * scale, FontStyle.Regular, GraphicsUnit.Pixel))
							using (var valueFont = new Font(nameFont.FontFamily, nameFont.Size * 0.8f, nameFont.Style, nameFont.Unit))
							using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
							{
								var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true };
								form.Controls.Add(flow);
								var pairs = new List<Tuple<Label, Label>>();
								foreach (var named in names)
								{
									var panel = new Panel { Margin = new Padding(4) };
									var right = named.Item3 == ContentAlignment.TopRight;
									var name = new Label { Font = nameFont, AutoSize = false, Dock = DockStyle.Top, TextAlign = named.Item3, UseMnemonic = named.Item2, UseCompatibleTextRendering = false, Text = named.Item1 };
									var value = new Label
									{
										Font = valueFont,
										AutoSize = false,
										Dock = DockStyle.Bottom,
										TextAlign = named.Item3,
										Padding = right ? new Padding(0, 0, (int)Math.Round(2 * scale), 0) : new Padding((int)Math.Round(2 * scale), 0, 0, 0),
										UseMnemonic = false,
										UseCompatibleTextRendering = false,
										Text = "0",
									};
									panel.Controls.Add(name);
									panel.Controls.Add(value);
									flow.Controls.Add(panel);
									PadControl.FitPair(name, value, PadControl.GapAt(scale));
									pairs.Add(Tuple.Create(name, value));
								}
								FormsTheme.Apply(form);
								form.Show();
								Application.DoEvents();
								var expected = PadControl.GapAt(scale);
								var gaps = new SortedSet<int>();
								foreach (var pair in pairs)
								{
									var at = string.Format("{0:P0}, {1}, \"{2}\": ", scale, theme, pair.Item1.Text);
									var rows = InkFreeRows(pair.Item1, pair.Item2, at, faults);
									gaps.Add(rows);
									if (rows != expected)
										faults.Add(string.Format("{0}{1} ink-free rows between the name and its digits, not {2}.", at, rows, expected));
								}
								report.Add(string.Format("{0:P0} {1}: names {2} px, numbers {3} px, {4} ink-free row(s), {5} expected", scale, theme,
									nameFont.Size, valueFont.Size, string.Join("/", gaps), expected));
							}
						}
				}
				finally
				{
					FormsTheme.SetTheme(ThemeType.Light);
				}
			});
			foreach (var line in report)
				TestContext.WriteLine(line);
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s):" + Environment.NewLine + string.Join(Environment.NewLine, faults.Take(40)));
		}

		/// <summary>Letters whose tails hang below the baseline, over the empty top of the number's line.</summary>
		const string Descenders = "gjpqy,;Q";

		/// <summary>
		/// The pixel rows with no ink at all between a name's letters and the digits under it, as the pair draws them: any pixel
		/// unlike the background is ink, ClearType's and smoothing's included.
		/// </summary>
		/// <remarks>
		/// The name's letters are those standing on its baseline: a tail, as of g or p, hangs past them over the empty top of the
		/// number's line, beside the digits, and is not counted. The digits are every one a number can show. Each is drawn alone,
		/// by a label like the one on the page, and the pair as the page draws it must agree: no ink in those rows but the tails,
		/// and every pixel of the name's ink and of the number's on it, so nothing is cut.
		/// <para>
		/// The pair is put together as Windows puts the two windows on the screen: each label draws itself, the number first and
		/// the name, which is above it, over it. Drawn as one with DrawToBitmap, the panel draws its children in the opposite order.
		/// </para>
		/// </remarks>
		static int InkFreeRows(Label name, Label value, string at, List<string> faults)
		{
			var panel = name.Parent;
			Assert.IsTrue(panel.Controls.GetChildIndex(name) < panel.Controls.GetChildIndex(value), at + "the name is not above its number.");
			using (var pair = new Bitmap(panel.Width, panel.Height))
			{
				value.DrawToBitmap(pair, value.Bounds);
				name.DrawToBitmap(pair, name.Bounds);
				var back = MostCommon(pair);
				var letters = new string(name.Text.Where(x => Descenders.IndexOf(x) < 0).ToArray());
				int nameTop, nameBottom, digitsTop, digitsBottom;
				using (var drawn = DrawnAlone(name, letters, back))
					InkRows(drawn, back, out nameTop, out nameBottom);
				using (var drawn = DrawnAlone(value, ControlOverlapTest.Digits, back))
					InkRows(drawn, back, out digitsTop, out digitsBottom);
				var first = name.Top + nameBottom + 1;
				var last = value.Top + digitsTop - 1;
				// Every pixel of ink the name and its number draw alone is on the pair.
				using (var whole = DrawnAlone(name, name.Text, back))
				{
					int top, bottom;
					InkRows(whole, back, out top, out bottom);
					if (bottom >= name.Height || !AllInkOn(whole, name.Location, pair, back))
						faults.Add(at + "the name is cut: ink rows " + top + " to " + bottom + " of " + name.Height + ".");
					// In the rows between, nothing but the name's tails.
					for (var y = first; y <= last; y++)
						for (var x = 0; x < pair.Width; x++)
						{
							var inside = x - name.Left >= 0 && x - name.Left < whole.Width && y - name.Top >= 0 && y - name.Top < whole.Height;
							var tail = inside && whole.GetPixel(x - name.Left, y - name.Top).ToArgb() != back.ToArgb();
							if (!tail && pair.GetPixel(x, y).ToArgb() != back.ToArgb())
							{
								faults.Add(string.Format("{0}ink at {1},{2} between the name and its digits.", at, x, y));
								y = last;
								break;
							}
						}
				}
				using (var digits = DrawnAlone(value, value.Text, back))
				{
					int top, bottom;
					InkRows(digits, back, out top, out bottom);
					if (top < 0 || bottom >= value.Height || !AllInkOn(digits, value.Location, pair, back))
						faults.Add(at + "the number is cut: ink rows " + top + " to " + bottom + " of " + value.Height + ".");
				}
				return last - first + 1;
			}
		}

		/// <summary>A label like the one given, with the text given, drawn alone on the colour given, and as tall again, so ink the label would cut shows below it.</summary>
		static Bitmap DrawnAlone(Label like, string text, Color back)
		{
			using (var label = new Label
			{
				AutoSize = false,
				Size = new Size(like.Width, like.Height * 2),
				Font = like.Font,
				Padding = like.Padding,
				TextAlign = like.TextAlign,
				UseMnemonic = like.UseMnemonic,
				UseCompatibleTextRendering = like.UseCompatibleTextRendering,
				ForeColor = like.ForeColor,
				BackColor = back,
				Text = text,
			})
			{
				// Drawn as the theme draws a label: its text without the room Windows keeps at either side.
				JocysCom.ClassLibrary.Controls.Themes.FormsTheme.Apply(label);
				var bitmap = new Bitmap(label.Width, label.Height);
				label.DrawToBitmap(bitmap, new Rectangle(Point.Empty, label.Size));
				return bitmap;
			}
		}

		/// <summary>The first and last rows holding any pixel unlike the background, or -1 when none does.</summary>
		static void InkRows(Bitmap bitmap, Color back, out int top, out int bottom)
		{
			top = -1;
			bottom = -1;
			for (var y = 0; y < bitmap.Height; y++)
				for (var x = 0; x < bitmap.Width; x++)
					if (bitmap.GetPixel(x, y).ToArgb() != back.ToArgb())
					{
						if (top < 0)
							top = y;
						bottom = y;
						break;
					}
		}

		/// <summary>Whether every pixel of ink drawn alone, within the label's own height, is ink on the pair, where the label stands on it.</summary>
		static bool AllInkOn(Bitmap alone, Point at, Bitmap pair, Color back)
		{
			for (var y = 0; y < alone.Height / 2; y++)
				for (var x = 0; x < alone.Width; x++)
					if (alone.GetPixel(x, y).ToArgb() != back.ToArgb())
					{
						var px = x + at.X;
						var py = y + at.Y;
						if (px < 0 || py < 0 || px >= pair.Width || py >= pair.Height || pair.GetPixel(px, py).ToArgb() == back.ToArgb())
							return false;
					}
			return true;
		}

		/// <summary>The colour most of a picture is.</summary>
		static Color MostCommon(Bitmap bitmap)
		{
			var counts = new Dictionary<int, int>();
			for (var y = 0; y < bitmap.Height; y++)
				for (var x = 0; x < bitmap.Width; x++)
				{
					var c = bitmap.GetPixel(x, y).ToArgb();
					int n;
					counts[c] = counts.TryGetValue(c, out n) ? n + 1 : 1;
				}
			return Color.FromArgb(counts.OrderByDescending(x => x.Value).First().Key);
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("The numbers under the names follow the emulated controller's state the picture is drawn from, are written only when they change, and the AI door reads them")]
		public void General_tab_values_follow_the_virtual_state()
		{
			Ui.OnUiThread(() =>
			{
				if (Global.DHelper == null)
					Global.InitDHelperHelper();
				var helper = Global.DHelper;
				var states = (State[])helper.CombinedXiStates.Clone();
				var connected = (bool[])helper.CombinedXiConencted.Clone();
				var getStates = SettingsManager.Options.GetXInputStates;
				var update = typeof(PadControl).GetMethod(nameof(PadControl.UpdateControlFromXInput), BindingFlags.NonPublic | BindingFlags.Instance);
				try
				{
					// The state the engine works out for Controller 1, which the picture is drawn from while XInput is not read back.
					SettingsManager.Options.GetXInputStates = false;
					using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false, Width = 900, Height = 700 })
					using (var pad = new PadControl(MapTo.Controller1) { Dock = DockStyle.Fill })
					{
						form.Controls.Add(pad);
						pad.InitPadControl();
						pad.UpdateSettingsMap();
						pad.InitPadData();
						form.Show();
						UiText.Apply(pad);
						helper.CombinedXiConencted[0] = true;
						helper.CombinedXiStates[0].Gamepad = new Gamepad
						{
							LeftTrigger = 126,
							RightTrigger = 255,
							LeftThumbX = -3356,
							LeftThumbY = -1869,
							RightThumbX = 32767,
							RightThumbY = 1000,
							Buttons = GamepadButtonFlags.A | GamepadButtonFlags.Back | GamepadButtonFlags.Guide | GamepadButtonFlags.LeftShoulder
								| GamepadButtonFlags.RightThumb | GamepadButtonFlags.DPadUp | GamepadButtonFlags.DPadRight,
						};
						update.Invoke(pad, null);
						Application.DoEvents();
						var expected = new Dictionary<MapCode, int>
						{
							{ MapCode.LeftTrigger, 126 }, { MapCode.RightTrigger, 255 },
							{ MapCode.LeftThumbAxisX, -3356 }, { MapCode.LeftThumbAxisY, -1869 },
							{ MapCode.LeftThumbLeft, -3356 }, { MapCode.LeftThumbRight, 0 }, { MapCode.LeftThumbUp, 0 }, { MapCode.LeftThumbDown, -1869 },
							{ MapCode.RightThumbAxisX, 32767 }, { MapCode.RightThumbAxisY, 1000 },
							{ MapCode.RightThumbLeft, 0 }, { MapCode.RightThumbRight, 32767 }, { MapCode.RightThumbUp, 1000 }, { MapCode.RightThumbDown, 0 },
							{ MapCode.ButtonA, 1 }, { MapCode.ButtonB, 0 }, { MapCode.ButtonX, 0 }, { MapCode.ButtonY, 0 },
							{ MapCode.ButtonBack, 1 }, { MapCode.ButtonStart, 0 }, { MapCode.ButtonGuide, 1 },
							{ MapCode.LeftShoulder, 1 }, { MapCode.RightShoulder, 0 },
							{ MapCode.LeftThumbButton, 0 }, { MapCode.RightThumbButton, 1 },
							// Up 1 and right 8.
							{ MapCode.DPad, 9 }, { MapCode.DPadUp, 1 }, { MapCode.DPadRight, 1 }, { MapCode.DPadLeft, 0 }, { MapCode.DPadDown, 0 },
						};
						CollectionAssert.AreEquivalent(Codes().ToList(), expected.Keys.ToList(), "The test does not cover every part of the General tab.");
						foreach (var pair in expected)
							Assert.AreEqual(pair.Value.ToString(), Find(pad, pair.Key + "ValueLabel").Text, pair.Key + " shows another value than the game is given.");
						// The AI door reads the number by its name, as ui_read does.
						var node = NodeOf(UiTreeWalker.Read(pad, false, ""), "LeftTriggerValueLabel");
						Assert.IsNotNull(node, "The AI door does not see the left trigger's number.");
						Assert.AreEqual("Left Trigger value", node.Name);
						Assert.AreEqual("126", node.Value);
						// Written only when it changes.
						var writes = 0;
						Find(pad, "LeftTriggerValueLabel").TextChanged += (sender, e) => writes++;
						update.Invoke(pad, null);
						Assert.AreEqual(0, writes, "A number that did not change was written again.");
						helper.CombinedXiStates[0] = new State();
						update.Invoke(pad, null);
						Assert.AreEqual(1, writes, "A number that changed was not written once.");
						foreach (var code in Codes())
							Assert.AreEqual("0", Find(pad, code + "ValueLabel").Text, code + " does not go back to 0 at rest.");
					}
				}
				finally
				{
					SettingsManager.Options.GetXInputStates = getStates;
					states.CopyTo(helper.CombinedXiStates, 0);
					connected.CopyTo(helper.CombinedXiConencted, 0);
				}
			});
		}

		/// <summary>The interface fonts the General tab is checked in, as the Options page's Font list keeps them: the default, Segoe UI with code in Consolas, Selawik and four installed families.</summary>
		internal static readonly string[] FontChoices = { "", AppFonts.BuiltInName, AppFontsTest.Chosen, "Arial", "Tahoma", "Verdana" };

		/// <summary>The widest reading a number shows: an axis at its lowest.</summary>
		const string WidestReading = "-32768";

		[TestMethod, TestCategory("ui")]
		[Description("In the main window at its default size, in every interface font, at 100 % and 150 %, in both themes, nothing on the General tab covers another control or leaves the page, but a name the empty top of its own number, every name and number is whole, every box shows the longest mapping the menu writes into it whole, and the Input column's readings show -32768 whole")]
		public void General_tab_is_whole_in_every_font()
		{
			AppFontsTest.LoadFonts();
			var faults = new List<string>();
			var report = new List<string>();
			foreach (var choice in FontChoices)
				ButtonIconTest.OnEverySurface(new Func<Control>[] { () => OptionsLayoutTest.MainWindowAtDefaultSize(WithFormulaSwitches(), "Controller 1") }, (form, when) =>
				{
					var page = Find(form, nameof(PadControl.GeneralTabPage));
					when += " " + page.Font.Name;
					Func<Control, Rectangle> at = c => page.RectangleToClient(c.Parent.RectangleToScreen(c.Bounds));
					var shown = Leaves(page).ToList();
					for (var i = 0; i < shown.Count; i++)
					{
						if (!page.ClientRectangle.Contains(at(shown[i])))
							faults.Add(string.Format("{0}: {1} {2} leaves the page {3}.", when, shown[i].Name, at(shown[i]), page.ClientRectangle));
						for (var j = i + 1; j < shown.Count; j++)
						{
							var shared = Rectangle.Intersect(at(shown[i]), at(shown[j]));
							if (shared.Width > 0 && shared.Height > 0 && !ControlOverlapTest.IsNameOverItsNumber(shown[i], shown[j]))
								faults.Add(string.Format("{0}: {1} {2} and {3} {4} cover each other by {5}x{6} px.", when,
									shown[i].Name, at(shown[i]), shown[j].Name, at(shown[j]), shared.Width, shared.Height));
						}
					}
					var boxes = Codes().Select(x => (ComboBox)Find(form, x + "ComboBox")).ToList();
					var boxNeeds = ComboBoxWidthFor(boxes[0], ControlOverlapTest.MappingTexts());
					report.Add(string.Format("{0}: boxes {1} px wide, the longest mapping needs {2}", when,
						string.Join("/", boxes.Select(x => x.Width).Distinct()), boxNeeds));
					foreach (var code in Codes())
					{
						var name = (Label)Find(form, code + "Label");
						var value = (Label)Find(form, code + "ValueLabel");
						var box = (ComboBox)Find(form, code + "ComboBox");
						var nameNeeds = LabelWidthFor(name, name.Text);
						if (name.Width < nameNeeds)
							faults.Add(string.Format("{0}: {1} is {2} px wide and \"{3}\" needs {4}.", when, name.Name, name.Width, name.Text, nameNeeds));
						var valueNeeds = LabelWidthFor(value, WidestReading);
						if (value.Width < valueNeeds)
							faults.Add(string.Format("{0}: {1} is {2} px wide and \"{3}\" needs {4}.", when, value.Name, value.Width, WidestReading, valueNeeds));
						foreach (var label in new[] { name, value })
						{
							var line = TextRenderer.MeasureText(" ", label.Font, Size.Empty, TextFormatFlags.NoPadding).Height;
							if (label.Height < line)
								faults.Add(string.Format("{0}: {1} is {2} px high and its line needs {3}.", when, label.Name, label.Height, line));
						}
						if (box.Width < boxNeeds)
							faults.Add(string.Format("{0}: {1} is {2} px wide and the longest mapping needs {3}.", when, box.Name, box.Width, boxNeeds));
					}
					// The Input column given a device with eight axes and two sliders, as the page gives it the selected device,
					// each axis and slider showing its reading under its name.
					var input = (InputUserControl)Find(form, nameof(PadControl.InputPanel));
					input.UpdateFrom(new UserDevice { InstanceGuid = Guid.NewGuid(), DiAxeMask = 0xFF, DiSliderMask = 0x3 });
					Application.DoEvents();
					foreach (var group in new[] { input.AxisChips, input.SliderChips })
						using (var font = new Font(group.Font.FontFamily, group.Font.Size * 0.8f, group.Font.Style))
						{
							var needs = TextRenderer.MeasureText(WidestReading, font, Size.Empty, TextFormatFlags.NoPadding).Width;
							var widths = group.Chips.Select(x => group.BoundsOf(x).Width).ToList();
							report.Add(string.Format("{0}: {1} chips {2} px wide, \"{3}\" needs {4}", when, group.Name, string.Join("/", widths.Distinct()), WidestReading, needs));
							if (widths.Min() < needs)
								faults.Add(string.Format("{0}: {1}'s chips are {2} px wide and \"{3}\" needs {4}.", when, group.Name, widths.Min(), WidestReading, needs));
						}
				}, choice);
			foreach (var line in report)
				TestContext.WriteLine(line);
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s):" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		[TestMethod, TestCategory("ui")]
		[Description("A hundred changes of the numbers under the names lay out nothing on the General tab")]
		public void General_tab_values_change_without_laying_out_the_page()
		{
			Ui.OnUiThread(() =>
			{
				if (Global.DHelper == null)
					Global.InitDHelperHelper();
				var helper = Global.DHelper;
				var states = (State[])helper.CombinedXiStates.Clone();
				var connected = (bool[])helper.CombinedXiConencted.Clone();
				var getStates = SettingsManager.Options.GetXInputStates;
				var update = typeof(PadControl).GetMethod(nameof(PadControl.UpdateControlFromXInput), BindingFlags.NonPublic | BindingFlags.Instance);
				try
				{
					SettingsManager.Options.GetXInputStates = false;
					using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
					using (var pad = new PadControl(MapTo.Controller1) { Dock = DockStyle.Fill })
					{
						form.ClientSize = pad.Size;
						form.Controls.Add(pad);
						pad.InitPadControl();
						pad.UpdateSettingsMap();
						pad.InitPadData();
						form.Show();
						UiText.Apply(pad);
						helper.CombinedXiConencted[0] = true;
						helper.CombinedXiStates[0] = new State();
						update.Invoke(pad, null);
						Application.DoEvents();
						var page = Find(pad, nameof(PadControl.GeneralTabPage));
						var passes = new Dictionary<string, int>();
						foreach (var container in new[] { page }.Concat(Descendants(page)).Where(x => x.HasChildren))
							container.Layout += (sender, e) =>
							{
								var name = ((Control)sender).Name;
								int count;
								passes[name] = passes.TryGetValue(name, out count) ? count + 1 : 1;
							};
						// Every number changes on every update, between its widest and its narrowest.
						for (var i = 0; i < 100; i++)
						{
							var wide = i % 2 == 0;
							var axis = (short)(wide ? short.MinValue : i);
							helper.CombinedXiStates[0].Gamepad = new Gamepad
							{
								LeftTrigger = (byte)(wide ? 255 : i),
								RightTrigger = (byte)(wide ? 255 : i),
								LeftThumbX = axis,
								LeftThumbY = axis,
								RightThumbX = axis,
								RightThumbY = axis,
								Buttons = wide ? GamepadButtonFlags.A | GamepadButtonFlags.B | GamepadButtonFlags.X | GamepadButtonFlags.Y
									| GamepadButtonFlags.Back | GamepadButtonFlags.Start | GamepadButtonFlags.Guide | GamepadButtonFlags.LeftShoulder
									| GamepadButtonFlags.RightShoulder | GamepadButtonFlags.LeftThumb | GamepadButtonFlags.RightThumb
									| GamepadButtonFlags.DPadUp | GamepadButtonFlags.DPadRight : GamepadButtonFlags.None,
							};
							update.Invoke(pad, null);
						}
						var total = passes.Values.Sum();
						TestContext.WriteLine("{0} layout passes on the General tab over 100 updates{1}", total,
							passes.Count == 0 ? "" : ": " + string.Join(", ", passes.Select(x => x.Key + " " + x.Value)));
						Assert.AreEqual(0, total, "Changing the numbers laid out the General tab.");
					}
				}
				finally
				{
					SettingsManager.Options.GetXInputStates = getStates;
					states.CopyTo(helper.CombinedXiStates, 0);
					connected.CopyTo(helper.CombinedXiConencted, 0);
				}
			});
		}

		/// <summary>A controller page with a formula switch beside each mapping box, as the program's settings map gives it one.</summary>
		static Control WithFormulaSwitches()
		{
			var pad = new PadControl(MapTo.Controller1);
			foreach (var code in Codes())
				MapExpressionToggle.AttachTo((ComboBox)Find(pad, code + "ComboBox"));
			return pad;
		}

		/// <summary>What stands on a page in a place of its own: every shown control that is not only a table or a panel holding others.</summary>
		static IEnumerable<Control> Leaves(Control parent)
		{
			foreach (Control child in parent.Controls)
			{
				if (!child.Visible || child.Width <= 0 || child.Height <= 0)
					continue;
				if (child.GetType() == typeof(Panel) || child is TableLayoutPanel || child is FlowLayoutPanel)
				{
					foreach (var leaf in Leaves(child))
						yield return leaf;
				}
				else
				{
					yield return child;
				}
			}
		}

		static IEnumerable<Control> Descendants(Control parent)
		{
			foreach (Control child in parent.Controls)
			{
				yield return child;
				foreach (var deeper in Descendants(child))
					yield return deeper;
			}
		}

		/// <summary>
		/// The width a label like the one given needs to show a text whole, as it is drawn: text drawn with GDI, as the program
		/// draws all its text, by the theme, from the label's padding without the room Windows keeps at either side of it, which
		/// .NET Framework leaves out only when it measures on a device context; other text by Windows, with that room.
		/// </summary>
		static int LabelWidthFor(Label like, string text)
		{
			if (!like.UseCompatibleTextRendering)
				using (var bitmap = new Bitmap(1, 1))
				using (var g = Graphics.FromImage(bitmap))
					return TextRenderer.MeasureText(g, text, like.Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width
						+ like.Padding.Horizontal;
			using (var label = new Label { Font = like.Font, Padding = like.Padding, UseMnemonic = like.UseMnemonic, UseCompatibleTextRendering = like.UseCompatibleTextRendering, Text = text })
				return label.PreferredWidth;
		}

		/// <summary>The width a drop-down list, drawn as the one given is, needs to show the widest of the texts whole.</summary>
		static int ComboBoxWidthFor(ComboBox box, IEnumerable<string> texts)
		{
			var items = box.Items.Cast<object>().ToArray();
			box.Items.Clear();
			try
			{
				box.Items.AddRange(texts.Cast<object>().ToArray());
				return FormsTheme.GetComboBoxWidth(box);
			}
			finally
			{
				box.Items.Clear();
				box.Items.AddRange(items);
			}
		}

		/// <summary>Every part the General tab maps, each with a name and a number.</summary>
		static IEnumerable<MapCode> Codes()
		{
			return SettingsConverter.TriggerButtonCodes.Concat(SettingsConverter.ShoulderButtonCodes)
				.Concat(SettingsConverter.MenuButtonCodes).Concat(SettingsConverter.MainButtonCodes)
				.Concat(SettingsConverter.DPadCodes).Concat(SettingsConverter.LeftThumbCodes).Concat(SettingsConverter.RightThumbCodes);
		}

		static Control Find(Control root, string name)
		{
			var found = root.Controls.Find(name, true);
			Assert.AreEqual(1, found.Length, "Expected one " + name + ".");
			return found[0];
		}

		/// <summary>The node with the field name given, at any depth, or null.</summary>
		static UiNode NodeOf(UiNode node, string id)
		{
			if (node.Id == id)
				return node;
			if (node.Items == null)
				return null;
			foreach (var item in node.Items)
			{
				var found = NodeOf(item, id);
				if (found != null)
					return found;
			}
			return null;
		}

		/// <summary>The colour the text is drawn on: the first that is not see-through, the control's or a parent's.</summary>
		static Color Behind(Control control)
		{
			for (var c = control; c != null; c = c.Parent)
				if (c.BackColor.A == 255)
					return c.BackColor;
			return SystemColors.Control;
		}

		/// <summary>WCAG 2 contrast ratio of two colours, 1 to 21.</summary>
		static double Contrast(Color a, Color b)
		{
			var la = Luminance(a);
			var lb = Luminance(b);
			return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
		}

		static double Luminance(Color c)
		{
			Func<int, double> channel = v =>
			{
				var s = v / 255.0;
				return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
			};
			return 0.2126 * channel(c.R) + 0.7152 * channel(c.G) + 0.0722 * channel(c.B);
		}
	}
}
