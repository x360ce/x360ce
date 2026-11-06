// @under-test: App.v4/Controls/PadControl.cs, App.v4/Controls/PadControl.Designer.cs, App.v4/Common/ImageInfo.cs, App.v4/Common/UiTree/UiCatalog.Pad.cs, App.v4/Controls/PadTabPages/General/InputChipGroup.cs
// @area: ui   @layer: unit
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
		[Description("Every name on the General tab has a small green number under it, the pair in the middle of its box's row and clear of every other control, in both themes, in Windows' fonts and in the program's")]
		public void General_tab_shows_a_value_under_each_name()
		{
			AppFontsTest.LoadFonts();
			Ui.OnUiThread(() =>
			{
				foreach (var programFonts in new[] { false, true })
				{
					if (programFonts)
						FormsTheme.SetFonts(AppFonts.InterfaceFamilyFor(AppFontsTest.Chosen), AppFonts.CodeFamily, AppFonts.SizeFor(AppFontsTest.Chosen, 0));
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
								var green = FormsTheme.GetColor("ForegroundSuccess", Color.Green);
								var table = Find(pad, "GeneralTableLayoutPanel");
								Func<Control, Rectangle> at = c => table.RectangleToClient(c.Parent.RectangleToScreen(c.Bounds));
								foreach (var code in Codes())
								{
									var name = Find(pad, code + "Label");
									var value = Find(pad, code + "ValueLabel");
									var box = Find(pad, code + "ComboBox");
									Assert.AreSame(name.Parent, value.Parent, where + code + "'s number is not beside its name.");
									Assert.IsTrue(value.Visible, where + code + "'s number is hidden.");
									Assert.AreEqual(new Point(name.Left, name.Bottom), value.Location, where + code + "'s number is not under its name.");
									Assert.AreEqual(name.Font.FontFamily.Name, value.Font.FontFamily.Name, where + code + "'s number is in another family than its name.");
									Assert.IsTrue(value.Font.Size < name.Font.Size, where + code + "'s number is not smaller than its name.");
									Assert.AreEqual(green.ToArgb(), value.ForeColor.ToArgb(), where + code + "'s number is not the theme's success colour.");
									var contrast = Contrast(value.ForeColor, Behind(value));
									Assert.IsTrue(contrast >= 4.5, string.Format("{0}{1}'s number has a contrast of {2:0.00}, under 4.5.", where, code, contrast));
									var offCentre = (at(name).Top + at(value).Bottom) / 2.0 - (box.Top + box.Bottom) / 2.0;
									Assert.IsTrue(Math.Abs(offCentre) <= 1, string.Format("{0}{1}'s name and number are {2} px off the middle of its box's row.", where, code, offCentre));
									var height = table.ClientSize.Height;
									Assert.IsTrue(at(name).Top >= 0 && at(value).Bottom <= height && box.Bottom <= height, string.Format(
										"{0}{1}'s row runs past the bottom of its table, {2} high: box {3}, name {4}, number {5}.", where, code, height, box.Bounds, at(name), at(value)));
								}
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
				var update = typeof(PadControl).GetMethod("UpdateControlFromXInput", BindingFlags.NonPublic | BindingFlags.Instance);
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

		/// <summary>The interface fonts the General tab is checked in, as the Options page's Font list keeps them: the default, Windows' own, Selawik and four installed families.</summary>
		internal static readonly string[] FontChoices = { "", AppFonts.BuiltInName, AppFontsTest.Chosen, "Arial", "Tahoma", "Verdana" };

		/// <summary>The widest reading a number shows: an axis at its lowest.</summary>
		const string WidestReading = "-32768";

		[TestMethod, TestCategory("ui")]
		[Description("In the main window at its default size, in every interface font, at 100 % and 150 %, in both themes, nothing on the General tab covers another control or leaves the page, every name and number is whole, every box shows the longest mapping the menu writes into it whole, and the Input column's readings show -32768 whole")]
		public void General_tab_is_whole_in_every_font()
		{
			AppFontsTest.LoadFonts();
			var faults = new List<string>();
			var report = new List<string>();
			foreach (var choice in FontChoices)
				ButtonIconTest.OnEverySurface(new Func<Control>[] { () => OptionsLayoutTest.MainWindowAtDefaultSize(WithFormulaSwitches(), "Controller 1") }, (form, when) =>
				{
					var page = Find(form, "GeneralTabPage");
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
							if (shared.Width > 0 && shared.Height > 0)
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
						if (name.Width < name.PreferredWidth)
							faults.Add(string.Format("{0}: {1} is {2} px wide and \"{3}\" needs {4}.", when, name.Name, name.Width, name.Text, name.PreferredWidth));
						var valueNeeds = LabelWidthFor(value, WidestReading);
						if (value.Width < valueNeeds)
							faults.Add(string.Format("{0}: {1} is {2} px wide and \"{3}\" needs {4}.", when, value.Name, value.Width, WidestReading, valueNeeds));
						if (box.Width < boxNeeds)
							faults.Add(string.Format("{0}: {1} is {2} px wide and the longest mapping needs {3}.", when, box.Name, box.Width, boxNeeds));
					}
					// The Input column with eight axes and two sliders, each showing its reading under its name.
					var input = (InputUserControl)Find(form, "InputPanel");
					var chips = InputChips.Create(0, 0xFF, 0x3, 0);
					input.AxisChips.Chips = chips.Where(x => x.Kind == InputChipKind.Axis).ToList();
					input.SliderChips.Chips = chips.Where(x => x.Kind == InputChipKind.Slider).ToList();
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
				var update = typeof(PadControl).GetMethod("UpdateControlFromXInput", BindingFlags.NonPublic | BindingFlags.Instance);
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
						var page = Find(pad, "GeneralTabPage");
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

		/// <summary>The width a label like the one given needs to show a text whole.</summary>
		static int LabelWidthFor(Label like, string text)
		{
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
