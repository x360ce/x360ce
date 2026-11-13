// @under-test: App.v4/Controls/PadControl.cs, App.v4/Controls/AxisMapUserControl.cs, App.v4/Common/DInput/DInputHelper.cs, App.v4/Controls/PadTabPages/AxisToDPadChart.cs, App.v4/Common/DInput/DInputHelper.Step3.UpdateXiStates.cs
// @area: ui   @layer: unit
using JocysCom.ClassLibrary;
using JocysCom.ClassLibrary.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nefarius.ViGEm.Client;
using SharpDX.DirectInput;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.App.DInput;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// A stick or trigger chart shows one engine pass's pair for the row it shows: the reading the engine converted for the
	/// row and what the row made of it, so its dot stays on its curve however fast the control moves.
	/// </summary>
	/// <remarks>
	/// The owner moved a stick fast and the dot left the red line. The chart's across was the device's reading as the page
	/// drew it, and its height Windows' readback of the virtual controller, read up to a tenth of a second earlier, or the
	/// controller's output combined from all its rows. With every setting at 0 a stick's curve is the diagonal, so a pair
	/// from one pass lies on it.
	/// </remarks>
	[TestClass]
	public class AxisChartPairTest
	{
		public TestContext TestContext { get; set; }

		/// <summary>The stick mapped as in the owner's picture: X as it is, Y inverted, every setting at 0.</summary>
		static PadSetting StickSetting()
		{
			return new PadSetting { LeftThumbAxisX = "a1", LeftThumbAxisY = "a-2" };
		}

		/// <summary>What a stick row with every setting at 0 sends for a reading: the diagonal, its centre's -1 sent as 0, as the engine sends it.</summary>
		static int OnTheCurve(int dInput, bool inverted)
		{
			var value = (short)ConvertHelper.GetThumbValue(dInput, 0, 0, 0, inverted, false, true);
			return value == -1 ? 0 : value;
		}

		/// <summary>A device read as a DirectInput device is, with its X and Y where they are given.</summary>
		static UserDevice Stick(string name, int x, int y)
		{
			var ud = DeviceOnTwoTabsTest.NewDevice(name);
			ud.CapAxeCount = 2;
			ud.DiAxeMask = 0x3;
			ud.IsOnline = true;
			ud.JoState = new JoystickState();
			ud.SourceState = new SourceState(new JoystickState());
			ud.SourceState.Axis[0] = x;
			ud.SourceState.Axis[1] = y;
			return ud;
		}

		static readonly MethodInfo UpdateMethod = typeof(PadControl).GetMethod(nameof(PadControl.UpdateControlFromXInput), BindingFlags.NonPublic | BindingFlags.Instance);

		/// <summary>The page's state update, as the window's timer runs it.</summary>
		static void Update(PadControl pad)
		{
			UpdateMethod.Invoke(pad, null);
		}

		static Control Find(Control parent, string name)
		{
			return parent.Controls.Find(name, true).Single();
		}

		static readonly FieldInfo DotAcross = typeof(AxisMapUserControl).GetField("_dInput", BindingFlags.NonPublic | BindingFlags.Instance);
		static readonly FieldInfo DotHeight = typeof(AxisMapUserControl).GetField("_xInput", BindingFlags.NonPublic | BindingFlags.Instance);

		/// <summary>Checks that a stick chart shows the pair given, its numbers and its dot alike, and that the pair lies on the curve.</summary>
		/// <returns>The chart's DInput.</returns>
		static int CheckStick(string what, AxisMapUserControl chart, bool inverted, int? expectedDInput, List<string> faults)
		{
			var shownD = int.Parse(Find(chart, "DInputValueLabel").Text);
			var shownX = int.Parse(Find(chart, "XInputValueLabel").Text);
			var dotD = (int)DotAcross.GetValue(chart);
			var dotX = (int)DotHeight.GetValue(chart);
			if (dotD != shownD || dotX != shownX)
				faults.Add(string.Format("{0}: shows DInput {1} and XInput {2}, while its dot stands at {3} and {4}.", what, shownD, shownX, dotD, dotX));
			if (expectedDInput.HasValue && shownD != expectedDInput.Value)
				faults.Add(string.Format("{0}: shows DInput {1}, not the {2} the engine converted.", what, shownD, expectedDInput.Value));
			var curve = OnTheCurve(shownD, inverted);
			if (shownX != curve)
				faults.Add(string.Format("{0}: shows DInput {1} with XInput {2}, off its curve, which gives {3} for it.", what, shownD, shownX, curve));
			return shownD;
		}

		/// <summary>Runs the engine's conversion and combining over the current routing, as one pass does.</summary>
		static void Convert(DInputHelper helper)
		{
			var routing = DeviceRouting.Current;
			EngineSteps.UpdateXiStates(helper)(routing);
			helper.CombineXiStates(routing);
		}

		/// <summary>Maps the devices to Controller 1 with the settings given, converts them as a pass does, and shows Controller 1's page with the first row selected.</summary>
		static void OnPage(UserDevice[] devices, PadSetting ps, Action<PadControl, DInputHelper, UserSetting[]> test)
		{
			Ui.OnUiThread(() =>
			{
				if (Global.DHelper == null)
					Global.InitDHelperHelper();
				var helper = Global.DHelper;
				var combined = (State[])helper.CombinedXiStates.Clone();
				var combinedConnected = (bool[])helper.CombinedXiConencted.Clone();
				var live = (State[])helper.LiveXiStates.Clone();
				var liveConnected = (bool[])helper.LiveXiConnected.Clone();
				var places = (int[])helper.XiPlaceForPad.Clone();
				var getStates = SettingsManager.Options.GetXInputStates;
				var settings = SettingsManager.UserSettings.ItemsToArraySynchronized();
				var pads = SettingsManager.PadSettings.ItemsToArraySynchronized();
				var listed = SettingsManager.UserDevices.ItemsToArraySynchronized();
				var game = SettingsManager.CurrentGame;
				var status = SettingsManager.Current.NotifySettingsStatus;
				SettingsManager.Current.NotifySettingsStatus = count => { };
				// A page adds its boxes to the settings map and nothing takes them out, so the boxes of a page built earlier in
				// this process, disposed by now, would receive the settings instead of this page's own.
				var settingsMap = SettingsManager.Current.SettingsMap;
				Predicate<SettingsMapItem> controller1 = x => x.MapTo == MapTo.Controller1;
				var earlier = settingsMap.Where(x => controller1(x)).ToArray();
				settingsMap.RemoveAll(controller1);
				try
				{
					SettingsManager.Options.GetXInputStates = false;
					var played = DeviceOnTwoTabsTest.NewGame();
					SettingsManager.UserSettings.Items.Clear();
					ps.PadSettingChecksum = ps.CleanAndGetCheckSum();
					SettingsManager.PadSettings.Items.Add(ps);
					var rows = new List<UserSetting>();
					foreach (var device in devices)
					{
						SettingsManager.UserDevices.Items.Add(device);
						var row = DeviceOnTwoTabsTest.Map(played, device, MapTo.Controller1);
						row.PadSettingChecksum = ps.PadSettingChecksum;
						rows.Add(row);
					}
					SettingsManager.UpdateCurrentGame(played);
					DeviceRouting.Refresh();
					Assert.AreEqual(devices.Length, DeviceRouting.Current.Rows.Length, "The rows are not routed, so the engine converts nothing.");
					Convert(helper);
					using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false, Width = 1400, Height = 1000 })
					using (var pad = new PadControl(MapTo.Controller1) { Dock = DockStyle.Fill })
					{
						form.Controls.Add(pad);
						pad.InitPadControl();
						pad.UpdateSettingsMap();
						pad.InitPadData();
						form.Show();
						pad.UpdateFromCurrentGame(rows[0].InstanceGuid);
						Assert.AreSame(rows[0], pad.GetSelectedSetting(), "The first row is not selected, so the test shows nothing.");
						test(pad, helper, rows.ToArray());
					}
				}
				finally
				{
					settingsMap.RemoveAll(controller1);
					settingsMap.AddRange(earlier);
					SettingsManager.Current.NotifySettingsStatus = status;
					SettingsManager.Options.GetXInputStates = getStates;
					combined.CopyTo(helper.CombinedXiStates, 0);
					combinedConnected.CopyTo(helper.CombinedXiConencted, 0);
					live.CopyTo(helper.LiveXiStates, 0);
					liveConnected.CopyTo(helper.LiveXiConnected, 0);
					places.CopyTo(helper.XiPlaceForPad, 0);
					SettingsManager.UpdateCurrentGame(game);
					SettingsManager.UserSettings.Items.Clear();
					foreach (var item in settings)
						SettingsManager.UserSettings.Items.Add(item);
					SettingsManager.PadSettings.Items.Clear();
					foreach (var item in pads)
						SettingsManager.PadSettings.Items.Add(item);
					SettingsManager.UserDevices.Items.Clear();
					foreach (var item in listed)
						SettingsManager.UserDevices.Items.Add(item);
					DeviceRouting.Refresh();
				}
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("With Show XInput State on, the Left Stick charts show the reading the engine converted for the row and what the row made of it, on the curve, numbers and dot alike, while the General tab shows Windows' readback of the virtual controller")]
		public void A_stick_chart_shows_its_rows_pair_not_the_readback()
		{
			var faults = new List<string>();
			OnPage(new[] { Stick("Stick", 6144, 57213) }, StickSetting(), (pad, helper, rows) =>
			{
				// Windows' readback of the virtual controller, read before the stick moved, as in the owner's picture.
				SettingsManager.Options.GetXInputStates = true;
				helper.XiPlaceForPad[0] = 0;
				helper.LiveXiConnected[0] = true;
				helper.LiveXiStates[0].Gamepad.LeftThumbX = -1281;
				helper.LiveXiStates[0].Gamepad.LeftThumbY = -3902;
				Update(pad);
				CheckStick("Left Stick X", pad.LeftThumbXUserControl, false, 6144, faults);
				CheckStick("Left Stick Y", pad.LeftThumbYUserControl, true, 57213, faults);
				Assert.AreEqual(-26624, OnTheCurve(6144, false), "The diagonal is not where the owner's picture puts it.");
				Assert.AreEqual(-24446, OnTheCurve(57213, true), "The inverted diagonal is not where the owner's picture puts it.");
				var general = Find(pad, "LeftThumbAxisXValueLabel").Text;
				if (general != "-1281")
					faults.Add("The General tab shows " + general + " for the left stick's X, not Windows' readback, -1281.");
			});
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s):" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("With two devices combined on one controller, each row's Left Stick charts show that row's own reading and output, on the curve, while the General tab shows the combined output")]
		public void Combined_rows_each_show_their_own_pair()
		{
			var faults = new List<string>();
			var first = Stick("First", 6144, 57213);
			var second = Stick("Second", 60000, 30000);
			OnPage(new[] { first, second }, StickSetting(), (pad, helper, rows) =>
			{
				var combinedX = helper.CombinedXiStates[0].Gamepad.LeftThumbX;
				Assert.AreNotEqual(OnTheCurve(6144, false), (int)combinedX, "The combined output is the first row's own, so the test shows nothing.");
				Update(pad);
				CheckStick("First row, Left Stick X", pad.LeftThumbXUserControl, false, 6144, faults);
				CheckStick("First row, Left Stick Y", pad.LeftThumbYUserControl, true, 57213, faults);
				var general = Find(pad, "LeftThumbAxisXValueLabel").Text;
				if (general != combinedX.ToString())
					faults.Add("The General tab shows " + general + " for the left stick's X, not the combined output, " + combinedX + ".");
				pad.UpdateFromCurrentGame(second.InstanceGuid);
				Assert.AreSame(rows[1], pad.GetSelectedSetting(), "The second row is not selected, so the test shows nothing.");
				Update(pad);
				CheckStick("Second row, Left Stick X", pad.LeftThumbXUserControl, false, 60000, faults);
				CheckStick("Second row, Left Stick Y", pad.LeftThumbYUserControl, true, 30000, faults);
			});
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s):" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("The Axis to D-Pad chart lights the sides the engine pressed for the row, a side held on the way back included, at the reading the engine converted")]
		public void The_axis_to_dpad_chart_lights_what_the_engine_pressed()
		{
			// Half the dead zone's scale, so a reading a little below the press point still holds a side pressed before.
			const int pressPoint = 32768 + 15359;
			var ps = new PadSetting { AxisToDPadEnabled = "1", AxisToDPadDeadZone = "128", AxisToDPadOffset = "0" };
			var ud = Stick("Pad", ushort.MaxValue, short.MaxValue);
			OnPage(new[] { ud }, ps, (pad, helper, rows) =>
			{
				// Pushed right, then back to just below the press point: the engine still holds right.
				ud.SourceState.Axis[0] = pressPoint - 100;
				Convert(helper);
				var held = rows[0].AxisButtons & (GamepadButtonFlags.DPadUp | GamepadButtonFlags.DPadDown | GamepadButtonFlags.DPadLeft | GamepadButtonFlags.DPadRight);
				Assert.AreEqual(GamepadButtonFlags.DPadRight, held, "The engine does not hold right on the way back, so the test shows nothing.");
				Update(pad);
				var chart = pad.AxisToDPadChart;
				Func<string, object> field = name => typeof(AxisToDPadChart).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(chart);
				Assert.IsTrue((bool)field("_hasReading"), "The Axis to D-Pad chart shows no reading.");
				Assert.AreEqual(pressPoint - 100, (int)field("_x"), "The Axis to D-Pad chart's dot is not at the reading the engine converted.");
				Assert.AreEqual(GamepadButtonFlags.DPadRight, (GamepadButtonFlags)field("_pressed"), "The Axis to D-Pad chart does not light the side the engine pressed.");
			});
		}

		/// <summary>State updates of the page in each half of the run: while the engine paces its passes, then while it runs them back to back.</summary>
		const int Updates = 1000;

		/// <summary>The seed of the stick's moves, so a run that fails can be run again the same way.</summary>
		const int Seed = 19;

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("While the engine runs pass after pass on another thread, the Test Controller's stick moved to a new place before each, every one of 2,000 updates of the Left Stick charts shows a pair from one pass, on the curve, numbers and dot alike: 1,000 while the engine paces its passes at 1000 Hz, where the dot follows the stick, and 1,000 while it runs them back to back")]
		public void A_moving_stick_keeps_its_dot_on_the_curve()
		{
			var faults = new List<string>();
			var halves = new[] { "Paced at 1000 Hz", "Back to back" };
			var updates = new int[halves.Length];
			var moves = new int[halves.Length];
			var ran = new int[halves.Length];
			var passes = 0;
			var paced = 1;
			var oldHelper = Global.DHelper;
			var oldGame = SettingsManager.CurrentGame;
			var oldReadMachine = XInputPlaces.ReadMachine;
			var oldGetStates = SettingsManager.Options.GetXInputStates;
			var oldClient = ViGEmClient.Current;
			var helper = new DInputHelper { UpdateDevicesEnabled = false };
			var ud = TestControllerDevice.NewUserDevice();
			ud.IsOnline = true;
			var ps = StickSetting();
			ps.PadSettingChecksum = ps.CleanAndGetCheckSum();
			var game = EnginePassAllocationTest.Game;
			var row = AppHelper.GetNewSetting(ud, game, MapTo.Controller1);
			row.PadSettingChecksum = ps.PadSettingChecksum;
			Global.DHelper = helper;
			var settingsMap = SettingsManager.Current.SettingsMap;
			Predicate<SettingsMapItem> controller1 = x => x.MapTo == MapTo.Controller1;
			var earlier = settingsMap.Where(x => controller1(x)).ToArray();
			settingsMap.RemoveAll(controller1);
			var status = SettingsManager.Current.NotifySettingsStatus;
			SettingsManager.Current.NotifySettingsStatus = count => { };
			try
			{
				// The place table is read from a stand-in machine, and no bus is reached: the game emulates nothing.
				XInputPlaces.ReadMachine = () => new DeviceInfo[0];
				ViGEmClient.Current = null;
				XInputPlaces.Invalidate();
				XInputPlaces.ReadIfStale();
				SettingsManager.Options.GetXInputStates = false;
				DeviceRouting.Watch();
				SettingsManager.PadSettings.Items.Add(ps);
				SettingsManager.UserDevices.Items.Add(ud);
				SettingsManager.UserSettings.Items.Add(row);
				SettingsManager.UpdateCurrentGame(game);
				DeviceRouting.Refresh();
				var pass = EngineSteps.RefreshAll(helper);
				pass(null, null);
				Assert.IsNotNull(ud.SourceState, "The Test Controller was not read, so the test shows nothing.");
				var plugin = helper.GetPluginDevice(ud.InstanceGuid);
				var steer = (Action<int, int>)Delegate.CreateDelegate(typeof(Action<int, int>), plugin, "Steer");
				Ui.OnUiThread(() =>
				{
					using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false, Width = 1400, Height = 1000 })
					using (var pad = new PadControl(MapTo.Controller1) { Dock = DockStyle.Fill })
					{
						form.Controls.Add(pad);
						pad.InitPadControl();
						pad.UpdateSettingsMap();
						pad.InitPadData();
						form.Show();
						pad.UpdateFromCurrentGame(ud.InstanceGuid);
						Assert.AreSame(row, pad.GetSelectedSetting(), "The Test Controller's row is not selected, so the test shows nothing.");
						// The first update, while no pass runs.
						steer(6144, 57213);
						pass(null, null);
						Update(pad);
						var lastX = CheckStick("Before the engine runs, Left Stick X", pad.LeftThumbXUserControl, false, 6144, faults);
						CheckStick("Before the engine runs, Left Stick Y", pad.LeftThumbYUserControl, true, 57213, faults);
						var stop = 0;
						Exception engineError = null;
						var engine = new Thread(() =>
						{
							try
							{
								var random = new Random(Seed);
								// The engine's own wait until its next pass is due, while it is paced.
								using (var wake = new ManualResetEvent(false))
								using (var pacer = new HiResPacer(wake))
									while (Volatile.Read(ref stop) == 0)
									{
										steer(random.Next(ushort.MaxValue + 1), random.Next(ushort.MaxValue + 1));
										pass(null, null);
										Interlocked.Increment(ref passes);
										if (Volatile.Read(ref paced) != 0)
											pacer.Wait(1);
									}
							}
							catch (Exception ex)
							{
								engineError = ex;
							}
						});
						engine.IsBackground = true;
						engine.Name = "Engine";
						engine.Start();
						try
						{
							for (var half = 0; half < halves.Length; half++)
							{
								Volatile.Write(ref paced, half == 0 ? 1 : 0);
								var first = Volatile.Read(ref passes);
								for (; updates[half] < Updates && faults.Count < 20; updates[half]++)
								{
									Update(pad);
									var at = halves[half] + ", update " + updates[half];
									var x = CheckStick(at + ", Left Stick X", pad.LeftThumbXUserControl, false, null, faults);
									CheckStick(at + ", Left Stick Y", pad.LeftThumbYUserControl, true, null, faults);
									if (x != lastX)
										moves[half]++;
									lastX = x;
								}
								ran[half] = Volatile.Read(ref passes) - first;
							}
						}
						finally
						{
							Volatile.Write(ref stop, 1);
							engine.Join();
						}
						if (engineError != null)
							throw new AssertFailedException("The engine's pass failed: " + engineError, engineError);
					}
				});
			}
			finally
			{
				settingsMap.RemoveAll(controller1);
				settingsMap.AddRange(earlier);
				SettingsManager.Current.NotifySettingsStatus = status;
				SettingsManager.UpdateCurrentGame(oldGame);
				SettingsManager.UserSettings.Items.Remove(row);
				SettingsManager.UserDevices.Items.Remove(ud);
				SettingsManager.PadSettings.Items.Remove(ps);
				SettingsManager.Options.GetXInputStates = oldGetStates;
				XInputPlaces.ReadMachine = oldReadMachine;
				ViGEmClient.Current = oldClient;
				// No hub thread runs, so this lets go of what the passes gave the hub.
				helper.RawInput.Stop();
				Global.DHelper = oldHelper;
				DeviceRouting.Refresh();
			}
			for (var half = 0; half < halves.Length; half++)
				TestContext.WriteLine("{0}: {1} passes ran beside {2} updates; the dot moved on {3} of them.", halves[half], ran[half], updates[half], moves[half]);
			Assert.AreEqual(0, faults.Count, faults.Count + " fault(s) in " + updates.Sum() + " updates:" + Environment.NewLine + string.Join(Environment.NewLine, faults));
			// Paced, an update that comes after a pass the one before did not see shows that pass's move.
			var seen = Math.Min(updates[0], ran[0]);
			Assert.IsTrue(moves[0] >= seen * 9 / 10, string.Format("{0}: the dot moved on {1} of {2} updates, beside {3} passes, each moving the stick.",
				halves[0], moves[0], updates[0], ran[0]));
		}
	}
}
