// @under-test: App.v4/Controls/PadControl.cs, App.v4/Controls/PadTabPages/General/InputUserControl.cs, App.v4/Controls/PadTabPages/DirectInputControl.cs, App.v4/Controls/AxisMapUserControl.cs, App.v4/Controls/PadTabPages/WheelChartUserControl.cs, App.v4/Controls/PadTabPages/General/XboxImageUserControl.cs, App.v4/Common/AppHelper.cs
// @area: ui   @layer: unit
using JocysCom.ClassLibrary.Controls.Themes;
using JocysCom.ClassLibrary.Controls.UiTree;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
	/// A controller page with no device mapped keeps every chart and the controller picture on screen, where they stand with
	/// a device, disabled and drawn faded, rather than leaving an empty space.
	/// </summary>
	[TestClass]
	public class UnmappedPageTest
	{
		[TestMethod, TestCategory("ui")]
		[Description("Without a device, the charts on the Triggers, Left Stick, Right Stick and Wheel tabs and the General tab's controller picture stay where they are with one, disabled, their box in the colour of a box that cannot be changed and what they show at half strength, in both themes; the AI door still finds each and says it is disabled")]
		public void Unmapped_page_keeps_its_charts_and_picture_disabled()
		{
			var faults = new List<string>();
			Ui.OnUiThread(() =>
			{
				try
				{
					foreach (var theme in new[] { ThemeType.Light, ThemeType.Dark })
					{
						FormsTheme.SetTheme(theme);
						using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
						using (var pad = new PadControl(MapTo.Controller1))
						{
							pad.Dock = DockStyle.Fill;
							form.ClientSize = pad.Size;
							form.Controls.Add(pad);
							FormsTheme.Apply(form);
							form.Show();
							var mapped = Look(pad, new UserDevice());
							var unmapped = Look(pad, null);
							foreach (var pair in mapped)
								Compare(theme + ", " + pair.Key, pad, pair.Value, unmapped[pair.Key], faults);
							foreach (var look in mapped.Values.Concat(unmapped.Values))
								look.Picture.Dispose();
						}
					}
				}
				finally
				{
					FormsTheme.SetTheme(ThemeType.Light);
				}
			});
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s):" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("A controller tab with no device, shown after one with a device, shows none of that controller's readings: the numbers under the General tab's names, each axis row's DInput and XInput and heading, the wheel's angle and force, and the charts, the Buttons tab's pictures and the controller picture are as on a tab of its own; the input column shows no group and the Direct Input page lists none of the device's objects")]
		public void Unmapped_tab_shows_nothing_of_the_controller_shown_before()
		{
			var faults = new List<string>();
			Ui.OnUiThread(() =>
			{
				if (Global.DHelper == null)
					Global.InitDHelperHelper();
				var helper = Global.DHelper;
				var states = (SharpDX.XInput.State[])helper.CombinedXiStates.Clone();
				var connected = (bool[])helper.CombinedXiConencted.Clone();
				var getStates = SettingsManager.Options.GetXInputStates;
				var settings = SettingsManager.UserSettings.ItemsToArraySynchronized();
				var pads = SettingsManager.PadSettings.ItemsToArraySynchronized();
				var devices = SettingsManager.UserDevices.ItemsToArraySynchronized();
				var game = SettingsManager.CurrentGame;
				var status = SettingsManager.Current.NotifySettingsStatus;
				SettingsManager.Current.NotifySettingsStatus = count => { };
				// A page adds its boxes to the settings map and nothing takes them out, so the boxes of a page built earlier in
				// this process, disposed by now, would receive the settings instead of these pages' own.
				var settingsMap = SettingsManager.Current.SettingsMap;
				Predicate<SettingsMapItem> controllers = x => x.MapTo == MapTo.Controller1 || x.MapTo == MapTo.Controller2;
				var earlier = settingsMap.Where(x => controllers(x)).ToArray();
				settingsMap.RemoveAll(controllers);
				var update = typeof(PadControl).GetMethod("Global_UpdateControlFromStates", BindingFlags.NonPublic | BindingFlags.Instance);
				Action<PadControl> refresh = pad =>
				{
					update.Invoke(pad, new object[] { pad, EventArgs.Empty });
					Application.DoEvents();
				};
				try
				{
					SettingsManager.Options.GetXInputStates = false;
					// Controller 2: a wheel whose readings are all far from rest, mapped inverted, by a button and by a formula.
					var wheel = DeviceOnTwoTabsTest.NewDevice("Wheel");
					wheel.CapButtonCount = 8;
					wheel.CapAxeCount = 3;
					wheel.DiAxeMask = 0x7;
					wheel.CapPovCount = 1;
					// Online and read, so its row is one the engine converts and its charts show.
					wheel.IsOnline = true;
					wheel.JoState = new SharpDX.DirectInput.JoystickState();
					wheel.SourceState = new SourceState(new SharpDX.DirectInput.JoystickState());
					wheel.SourceState.Axis[0] = 54321;
					wheel.SourceState.Axis[1] = 12345;
					wheel.SourceState.Axis[2] = 40000;
					wheel.SourceState.Buttons[0] = true;
					wheel.SourceState.Povs[0] = 9000;
					wheel.DeviceObjects = new[] { new DeviceObjectItem(0, SharpDX.DirectInput.ObjectGuid.XAxis, SharpDX.DirectInput.ObjectAspect.Position,
						SharpDX.DirectInput.DeviceObjectTypeFlags.AbsoluteAxis, 0, "Wheel axis") };
					SettingsManager.UserDevices.Items.Add(wheel);
					var played = DeviceOnTwoTabsTest.NewGame();
					SettingsManager.UserSettings.Items.Clear();
					var row = DeviceOnTwoTabsTest.Map(played, wheel, MapTo.Controller2);
					var ps = new PadSetting { LeftTrigger = "a-1", RightTrigger = "b1", LeftThumbAxisX = "=a2*abs(a2)", LeftThumbAxisY = "a3", RightThumbAxisX = "a-2", ButtonA = "b1", DPad = "p1" };
					ps.PadSettingChecksum = ps.CleanAndGetCheckSum();
					SettingsManager.PadSettings.Items.Add(ps);
					row.PadSettingChecksum = ps.PadSettingChecksum;
					SettingsManager.CurrentGame = played;
					x360ce.App.DInput.DeviceRouting.Refresh();
					helper.CombinedXiConencted[0] = false;
					helper.CombinedXiStates[0] = new SharpDX.XInput.State();
					// What the row made, which its charts show, and what the controller is given, which the General tab shows.
					row.XiState = new SharpDX.XInput.Gamepad
					{
						LeftTrigger = 128,
						RightTrigger = 255,
						LeftThumbX = 32511,
						LeftThumbY = -12345,
						RightThumbX = 23456,
						RightThumbY = -23456,
						Buttons = SharpDX.XInput.GamepadButtonFlags.A | SharpDX.XInput.GamepadButtonFlags.DPadRight | SharpDX.XInput.GamepadButtonFlags.LeftShoulder,
					};
					helper.CombinedXiConencted[1] = true;
					helper.CombinedXiStates[1].Gamepad = row.XiState;
					Dictionary<string, string> shown;
					Dictionary<string, string> own;
					Dictionary<string, Bitmap> shownPictures;
					using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false, Width = 1400, Height = 1000 })
					using (var pad = new PadControl(MapTo.Controller2) { Dock = DockStyle.Fill })
					{
						form.Controls.Add(pad);
						pad.InitPadControl();
						pad.UpdateSettingsMap();
						pad.InitPadData();
						form.Show();
						UiText.Apply(pad);
						pad.UpdateFromCurrentGame();
						// The Wheel tab is shown once, so the wheel's angle is on it, then the General tab, as a person would leave it.
						pad.PadTabControl.SelectedTab = pad.WheelTabPage;
						refresh(pad);
						pad.PadTabControl.SelectedTab = pad.GeneralTabPage;
						refresh(pad);
						var before = Readings(pad);
						foreach (var pair in new Dictionary<string, string>
						{
							{ "LeftTriggerValueLabel", "128" },
							{ "LeftThumbAxisXValueLabel", "32511" },
						})
							Assert.AreEqual(pair.Value, before.Single(x => x.Key.EndsWith("/" + pair.Key)).Value, "Controller 2 does not show its own " + pair.Key + ", so the test shows nothing.");
						Assert.AreNotEqual("0", Find(pad, nameof(PadControl.LeftTriggerUserControl), "DInputValueLabel").Text, "Controller 2's left trigger row shows no DInput reading, so the test shows nothing.");
						Assert.AreEqual("128", Find(pad, nameof(PadControl.LeftTriggerUserControl), "XInputValueLabel").Text, "Controller 2's left trigger row does not show what its row made, so the test shows nothing.");
						Assert.AreNotEqual("-", pad.WheelChartUserControl.AngleValueLabel.Text, "Controller 2's wheel shows no angle, so the test shows nothing.");
						var objects = (DataGridView)Find(pad, "DirectInputPanel", nameof(DirectInputUserControl.DiObjectsDataGridView));
						Assert.AreEqual(1, (objects.DataSource as DeviceObjectItem[])?.Length, "Controller 2's Direct Input page does not list the wheel's axis, so the test shows nothing.");
						var groups = new[] { "ButtonsGroupBox", "AxesGroupBox", "SlidersGroupBox", "PovsGroupBox" };
						var mappedTree = UiTreeWalker.Read(pad, false, "");
						CollectionAssert.AreEqual(new[] { false, false, true, false }, groups.Select(x => NodeOf(mappedTree, x).Hidden).ToArray(),
							"Controller 2's input column does not show the wheel's buttons, axes and POV alone, so the test shows nothing.");
						pad.SwitchTo(MapTo.Controller1);
						refresh(pad);
						shown = Readings(pad);
						shownPictures = Pictures(pad);
						// What the AI door reads on the tab.
						var tree = UiTreeWalker.Read(pad, false, "");
						var node = NodeOf(tree, "LeftTriggerValueLabel");
						if (node == null || node.Value != "0")
							faults.Add("The AI door reads the left trigger as " + (node == null ? "nothing" : node.Value) + " on Controller 1, which has no device.");
						// The input column has nothing to show without a device, so it shows no group.
						foreach (var group in groups.Where(x => !NodeOf(tree, x).Hidden))
							faults.Add("The General tab's input column shows " + group + " on Controller 1, which has no device.");
						var listed = objects.DataSource as DeviceObjectItem[];
						if (listed != null && listed.Length > 0)
							faults.Add("The Direct Input page lists " + listed.Length + " object(s) of Controller 2's wheel on Controller 1, which has no device.");
					}
					// Controller 1 on a page of its own, as each controller had one before the four tabs shared a page.
					settingsMap.RemoveAll(controllers);
					using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false, Width = 1400, Height = 1000 })
					using (var pad = new PadControl(MapTo.Controller1) { Dock = DockStyle.Fill })
					{
						form.Controls.Add(pad);
						pad.InitPadControl();
						pad.UpdateSettingsMap();
						pad.InitPadData();
						form.Show();
						pad.UpdateFromCurrentGame();
						refresh(pad);
						own = Readings(pad);
						foreach (var pair in Pictures(pad))
						{
							Bitmap picture;
							if (shownPictures.TryGetValue(pair.Key, out picture) && !SamePicture(picture, pair.Value))
								faults.Add(pair.Key + " is not drawn as on a tab of its own (" + picture.Width + " x " + picture.Height
									+ " shared, " + pair.Value.Width + " x " + pair.Value.Height + " on its own).");
							pair.Value.Dispose();
						}
					}
					foreach (var picture in shownPictures.Values)
						picture.Dispose();
					foreach (var pair in own)
					{
						string text;
						shown.TryGetValue(pair.Key, out text);
						if (text != pair.Value)
							faults.Add(string.Format("{0} reads \"{1}\", where a tab of its own reads \"{2}\".", pair.Key, text, pair.Value));
					}
				}
				finally
				{
					settingsMap.RemoveAll(controllers);
					settingsMap.AddRange(earlier);
					SettingsManager.Current.NotifySettingsStatus = status;
					SettingsManager.Options.GetXInputStates = getStates;
					states.CopyTo(helper.CombinedXiStates, 0);
					connected.CopyTo(helper.CombinedXiConencted, 0);
					SettingsManager.CurrentGame = game;
					SettingsManager.UserSettings.Items.Clear();
					foreach (var item in settings)
						SettingsManager.UserSettings.Items.Add(item);
					SettingsManager.PadSettings.Items.Clear();
					foreach (var item in pads)
						SettingsManager.PadSettings.Items.Add(item);
					SettingsManager.UserDevices.Items.Clear();
					foreach (var item in devices)
						SettingsManager.UserDevices.Items.Add(item);
					x360ce.App.DInput.DeviceRouting.Refresh();
				}
			});
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s):" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		/// <summary>
		/// Every reading the page shows as text, by its path: each number named a value, the colour of the name it stands under,
		/// lit while its part is used, and each axis row's heading, which says when its settings do not apply.
		/// </summary>
		static Dictionary<string, string> Readings(PadControl pad)
		{
			var texts = new Dictionary<string, string>();
			foreach (var control in ButtonIconTest.All(pad))
			{
				if (control.Name.EndsWith("ValueLabel", StringComparison.Ordinal))
				{
					texts[PathOf(pad, control)] = control.Text;
					var name = control.Parent.Controls[control.Name.Replace("ValueLabel", "Label")];
					if (name != null)
						texts[PathOf(pad, name) + " colour"] = ColorTranslator.ToHtml(name.ForeColor);
				}
				var chart = control as AxisMapUserControl;
				if (chart != null)
					texts[PathOf(pad, chart.MainGroupBox)] = chart.MainGroupBox.Text;
			}
			return texts;
		}

		/// <summary>The pictures that show readings, by path, each drawn with its tab selected: the axis charts, the Axis to D-Pad chart, the Buttons tab's pictures and the controller picture.</summary>
		/// <remarks>
		/// An axis chart's curve is made off the interface thread, so the pictures are drawn once those made by now have
		/// arrived. A page shown for the first time makes its windows, which the theme draws in its own sizes, so an axis group
		/// takes the height of its rows then and its chart makes its curve again: each page is drawn once that has arrived too.
		/// </remarks>
		static Dictionary<string, Bitmap> Pictures(PadControl pad)
		{
			Action settle = () =>
			{
				for (var waited = 0; waited < 1000; waited += 50)
				{
					Application.DoEvents();
					System.Threading.Thread.Sleep(50);
				}
			};
			settle();
			var pictures = new Dictionary<string, Bitmap>();
			foreach (var page in new[] { pad.GeneralTabPage, pad.TriggersTabPage, pad.LeftThumbTabPage, pad.RightThumbTabPage, pad.ButtonsTabPage, pad.DPadTabPage })
			{
				pad.PadTabControl.SelectedTab = page;
				settle();
				var shown = new List<Control>();
				shown.AddRange(ButtonIconTest.All(page).OfType<AxisMapUserControl>().Select(x => (Control)x.MainPictureBox));
				shown.AddRange(ButtonIconTest.All(page).OfType<AxisToDPadChart>());
				shown.AddRange(ButtonIconTest.All(page).OfType<AxisToButtonUserControl>().Select(x => (Control)x.ButtonImagePictureBox));
				shown.AddRange(ButtonIconTest.All(page).OfType<XboxImageUserControl>());
				foreach (var control in shown)
				{
					var bitmap = new Bitmap(control.Width, control.Height);
					control.DrawToBitmap(bitmap, new Rectangle(Point.Empty, control.Size));
					pictures[PathOf(pad, control)] = bitmap;
				}
			}
			pad.PadTabControl.SelectedTab = pad.GeneralTabPage;
			return pictures;
		}

		static bool SamePicture(Bitmap a, Bitmap b)
		{
			if (a.Size != b.Size)
				return false;
			for (var x = 0; x < a.Width; x++)
				for (var y = 0; y < a.Height; y++)
					if (a.GetPixel(x, y).ToArgb() != b.GetPixel(x, y).ToArgb())
						return false;
			return true;
		}

		/// <summary>The control of this name inside the named control of the page.</summary>
		static Control Find(PadControl pad, string parent, string name)
		{
			return pad.Controls.Find(parent, true).Single().Controls.Find(name, true).Single();
		}

		static UiNode NodeOf(UiNode node, string id)
		{
			if (node.Id == id)
				return node;
			if (node.Items != null)
				foreach (var item in node.Items)
				{
					var found = NodeOf(item, id);
					if (found != null)
						return found;
				}
			return null;
		}

		/// <summary>A chart or the controller picture as one state of the page shows it.</summary>
		sealed class Seen
		{
			public Control Control;
			public Rectangle Bounds;
			public bool Visible;
			public bool Enabled;
			public Color Face;
			public Bitmap Picture;
		}

		/// <summary>Gives the page the device given, or none, and sees each chart and the controller picture on its tab.</summary>
		static Dictionary<string, Seen> Look(PadControl pad, UserDevice device)
		{
			pad.UpdateEnabledFromDevice(device);
			var seen = new Dictionary<string, Seen>();
			var pages = new[] { pad.GeneralTabPage, pad.TriggersTabPage, pad.LeftThumbTabPage, pad.RightThumbTabPage, pad.WheelTabPage };
			foreach (var page in pages)
			{
				pad.PadTabControl.SelectedTab = page;
				Application.DoEvents();
				var shown = new List<Control>();
				shown.AddRange(ButtonIconTest.All(page).OfType<AxisMapUserControl>().Select(x => (Control)x.MainPictureBox));
				shown.AddRange(ButtonIconTest.All(page).OfType<WheelChartUserControl>().Select(x => (Control)x.MainPictureBox));
				shown.AddRange(ButtonIconTest.All(page).OfType<XboxImageUserControl>());
				foreach (var control in shown)
				{
					var key = page.Text + " " + PathOf(pad, control);
					var bounds = pad.RectangleToClient(control.RectangleToScreen(control.ClientRectangle));
					seen[key] = new Seen
					{
						Control = control,
						Bounds = bounds,
						Visible = control.Visible,
						Enabled = control.Enabled,
						Face = FormsTheme.FrameFace(control),
						Picture = Draw(control),
					};
				}
			}
			return seen;
		}

		/// <summary>The control as it draws itself, once a stick's line, which is made off the interface thread, is on it.</summary>
		static Bitmap Draw(Control control)
		{
			var bitmap = new Bitmap(control.Width, control.Height);
			for (var waited = 0; waited < 3000; waited += 50)
			{
				control.DrawToBitmap(bitmap, new Rectangle(Point.Empty, control.Size));
				if (!(control is PictureBox) || Reddest(bitmap) > CurveRedness() * 60 / 255)
					break;
				Application.DoEvents();
				System.Threading.Thread.Sleep(50);
			}
			return bitmap;
		}

		static void Compare(string at, PadControl pad, Seen mapped, Seen unmapped, List<string> faults)
		{
			if (!unmapped.Visible || unmapped.Enabled)
				faults.Add(string.Format("{0}: without a device it is {1} and {2}.", at, unmapped.Visible ? "shown" : "hidden", unmapped.Enabled ? "enabled" : "disabled"));
			if (unmapped.Bounds != mapped.Bounds)
				faults.Add(string.Format("{0}: stands at {1} with a device and at {2} without.", at, mapped.Bounds, unmapped.Bounds));
			// The AI door still finds it and says it is disabled.
			if (UiTreeWalker.Find(pad, PathOf(pad, unmapped.Control)) != unmapped.Control)
				faults.Add(at + ": the AI door does not find it.");
			var answer = UiTreeWalker.SetValue(unmapped.Control, "x") ?? "";
			if (answer.IndexOf("disabled", StringComparison.OrdinalIgnoreCase) < 0)
				faults.Add(at + ": the AI door does not say it is disabled: " + answer);
			if (unmapped.Control is XboxImageUserControl)
			{
				// Faded: every part of the controller half as far from what is behind it.
				var back = unmapped.Control.BackColor;
				var full = Strength(mapped.Picture, back);
				var faded = Strength(unmapped.Picture, back);
				if (faded < full * 0.35 || faded > full * 0.65)
					faults.Add(string.Format("{0}: drawn at {1:0.##} of its strength with a device without one, not faded to a half.", at, faded / Math.Max(1, full)));
				return;
			}
			var area = AppHelper.ChartBounds(unmapped.Control);
			// The box's own colour, in the room round the chart.
			var face = unmapped.Picture.GetPixel(area.X / 2, unmapped.Picture.Height / 2);
			if (face.ToArgb() != unmapped.Face.ToArgb())
				faults.Add(string.Format("{0}: the disabled box is {1}, not {2}, the colour of a box that cannot be changed.", at, Hex(face), Hex(unmapped.Face)));
			var page = unmapped.Control.Parent.BackColor;
			if (unmapped.Face.ToArgb() == page.ToArgb())
				faults.Add(at + ": the disabled box is the page's colour, so it is not seen.");
			// The red line stays, at half strength.
			var full2 = Reddest(mapped.Picture);
			var faded2 = Reddest(unmapped.Picture);
			if (faded2 < CurveRedness() * 30 / 255)
				faults.Add(string.Format("{0}: without a device the chart shows no line (reddest {1}, {2} with one).", at, faded2, full2));
			else if (faded2 > full2 * 0.75)
				faults.Add(string.Format("{0}: without a device the line is as strong as with one ({1} and {2}).", at, faded2, full2));
		}

		/// <summary>How far the theme's curve colour is redder than it is green or blue: less for the dark theme's light red.</summary>
		static int CurveRedness()
		{
			var c = FormsTheme.GetColor("ColorCurve");
			return c.R - Math.Max(c.G, c.B);
		}

		/// <summary>How far the reddest pixel is redder than it is green or blue.</summary>
		static int Reddest(Bitmap bitmap)
		{
			var most = 0;
			for (var x = 0; x < bitmap.Width; x++)
				for (var y = 0; y < bitmap.Height; y++)
				{
					var c = bitmap.GetPixel(x, y);
					most = Math.Max(most, c.R - Math.Max(c.G, c.B));
				}
			return most;
		}

		/// <summary>The sum of how far every pixel is from the colour behind the picture.</summary>
		static double Strength(Bitmap bitmap, Color back)
		{
			double sum = 0;
			for (var x = 0; x < bitmap.Width; x++)
				for (var y = 0; y < bitmap.Height; y++)
				{
					var c = bitmap.GetPixel(x, y);
					sum += Math.Abs(c.R - back.R) + Math.Abs(c.G - back.G) + Math.Abs(c.B - back.B);
				}
			return sum;
		}

		/// <summary>A control's path below the page, as the AI door names it.</summary>
		static string PathOf(Control root, Control control)
		{
			var names = new List<string>();
			for (var c = control; c != null && c != root; c = c.Parent)
				names.Insert(0, c.Name);
			return string.Join("/", names);
		}

		static string Hex(Color c)
		{
			return string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
		}
	}
}
