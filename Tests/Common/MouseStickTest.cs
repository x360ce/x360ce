// @under-test: App.v4/Common/DInput/MouseStick.cs, App.v4/Common/DInput/DeviceRouting.cs, App.v4/Common/DInput/DInputHelper.Step3.UpdateXiStates.cs, Engine/Data/PadSetting.cs, Engine/Maps/SettingName.cs, App.v4/Controls/MouseStickUserControl.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.DirectInput;
using System;
using System.Diagnostics;
using System.Threading;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.App.DInput;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// The Mouse page moves a stick by how far a mouse moved, how fast it moves, or where the pointer is, always or while a
	/// button is held. At its defaults it is how a mouse moved a stick before, and saved presets keep their checksums.
	/// </summary>
	[TestClass]
	public class MouseStickTest
	{
		static readonly UserGame Game = new UserGame { FileName = "mouse-stick.exe", FileProductName = "Mouse stick", EnableMask = (int)MapToMask.Controller1 };

		const int Centre = 32768;

		/// <summary>A clock for a processor, in milliseconds.</summary>
		static long Ms(double ms)
		{
			return 1 + (long)(ms * Stopwatch.Frequency / 1000);
		}

		static SourceState Buttons(bool first)
		{
			var state = new SourceState();
			state.Buttons[0] = first;
			return state;
		}

		/// <summary>One pass of a processor on the across axis: the button, then the running total at a time.</summary>
		static int Pass(MouseStick stick, int total, double ms, bool button = false)
		{
			stick.BeginPass(Buttons(button));
			return stick.Value(0, total, 1, Ms(ms));
		}

		[TestMethod, TestCategory("mapping")]
		[Description("A preset whose Mouse page is at its defaults, empty or written out, makes no processor: the engine reads the mouse as before")]
		public void Defaults_make_no_processor()
		{
			Assert.IsNull(MouseStickSettings.From(new PadSetting(), false));
			var written = new PadSetting
			{
				MouseLeftThumbStyle = "0", MouseLeftThumbSensitivityX = "100", MouseLeftThumbSensitivityY = "100", MouseLeftThumbSmoothing = "30",
				MouseLeftThumbReturn = "0", MouseLeftThumbActive = "0", MouseLeftThumbButton = "0",
			};
			Assert.IsNull(MouseStickSettings.From(written, false), "Defaults written out made a processor.");
			var speed = new PadSetting { MouseRightThumbStyle = "1" };
			Assert.IsNull(MouseStickSettings.From(speed, false), "The right stick's setting made one for the left.");
			Assert.AreEqual(MouseStyle.Speed, MouseStickSettings.From(speed, true).Style);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("The Mouse page's settings at their defaults leave a preset's checksum as it was, and are stored empty; any other value changes it")]
		public void Defaults_leave_the_checksum_unchanged()
		{
			var before = new PadSetting { ButtonA = "1", LeftThumbAxisX = "a1", RightThumbAxisY = "a-2", ForceEnable = "1" };
			var checksum = before.CleanAndGetCheckSum();
			var after = new PadSetting { ButtonA = "1", LeftThumbAxisX = "a1", RightThumbAxisY = "a-2", ForceEnable = "1" };
			foreach (var stick in new[] { "Left", "Right" })
			{
				Set(after, "Mouse" + stick + "ThumbStyle", SettingName.DefaultMouseStyle);
				Set(after, "Mouse" + stick + "ThumbSensitivityX", SettingName.DefaultMouseSensitivity);
				Set(after, "Mouse" + stick + "ThumbSensitivityY", SettingName.DefaultMouseSensitivity);
				Set(after, "Mouse" + stick + "ThumbSmoothing", SettingName.DefaultMouseSmoothing);
				Set(after, "Mouse" + stick + "ThumbReturn", SettingName.DefaultMouseReturn);
				Set(after, "Mouse" + stick + "ThumbActive", SettingName.DefaultMouseActive);
				Set(after, "Mouse" + stick + "ThumbButton", SettingName.DefaultMouseButton);
			}
			Assert.AreEqual(checksum, after.CleanAndGetCheckSum(), "Defaults written out changed the checksum.");
			Assert.AreEqual("", after.MouseLeftThumbSmoothing, "A default was not stored empty.");
			Assert.AreEqual("", after.MouseRightThumbSensitivityX, "A default was not stored empty.");
			var changed = new PadSetting { ButtonA = "1", LeftThumbAxisX = "a1", RightThumbAxisY = "a-2", ForceEnable = "1", MouseRightThumbStyle = "1" };
			Assert.AreNotEqual(checksum, changed.CleanAndGetCheckSum(), "A setting the page changed did not change the checksum.");
			var nought = new PadSetting { ButtonA = "1", LeftThumbAxisX = "a1", RightThumbAxisY = "a-2", ForceEnable = "1", MouseLeftThumbSmoothing = "0" };
			Assert.AreNotEqual(checksum, nought.CleanAndGetCheckSum(), "No smoothing, which is not the default, was taken for the default.");
		}

		static void Set(PadSetting ps, string name, string value)
		{
			typeof(PadSetting).GetProperty(name).SetValue(ps, value, null);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Position at its defaults moves the stick as the engine's own reading of a mouse does, past the ends and back")]
		public void Position_is_the_engines_own_reading()
		{
			var stick = new MouseStick(new MouseStickSettings { Style = MouseStyle.Position });
			var ud = new UserDevice { DiRelativeAxisMask = 0x1 };
			var read = new SourceState();
			var into = new SourceState();
			var totals = new[] { 5000, 5000 + 1024, 5000 + 3000, 5000 + 9000, 5000 + 9000 - 1024, 5000 - 4000, 5000 - 3500 };
			for (var i = 0; i < totals.Length; i++)
			{
				read.Axis[0] = totals[i];
				DInputHelper.ToMouseState(ud, read, into, i);
				var value = Pass(stick, totals[i], i);
				Assert.IsTrue(Math.Abs(value - into.Axis[0]) <= 32, string.Format("At {0} counts the page reads {1}, the engine {2}.", totals[i], value, into.Axis[0]));
			}
		}

		[TestMethod, TestCategory("mapping")]
		[Description("Position with Back to centre returns to the centre when the mouse stops; with sensitivity it reaches the end sooner")]
		public void Position_returns_and_scales()
		{
			var stick = new MouseStick(new MouseStickSettings { Style = MouseStyle.Position, ReturnMs = 100, SensitivityX = 200 });
			Pass(stick, 0, 0);
			var moved = Pass(stick, 512, 1);
			Assert.IsTrue(moved > 48000, "512 counts at 200 % is not half the stick: " + moved);
			var later = 0;
			for (var ms = 2; ms <= 800; ms++)
				later = Pass(stick, 512, ms);
			Assert.IsTrue(Math.Abs(later - Centre) < 50, "The stick did not go back to the centre: " + later);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Speed follows how fast the mouse moves, from reports that arrive in bursts, and returns to the centre when it stops")]
		public void Speed_follows_the_speed_and_returns()
		{
			var stick = new MouseStick(new MouseStickSettings { Style = MouseStyle.Speed });
			var total = 0;
			var value = 0;
			// 4,000 counts a second, reported 32 counts every 8 ms, read every millisecond.
			for (var ms = 0; ms <= 400; ms++)
			{
				if (ms % 8 == 0)
					total += 32;
				value = Pass(stick, total, ms);
			}
			Assert.IsTrue(value > Centre + 0.85 * 32767, "4,000 counts a second is not nearly the whole stick at 100 %: " + value);
			for (var ms = 401; ms <= 650; ms++)
				value = Pass(stick, total, ms);
			Assert.IsTrue(Math.Abs(value - Centre) < 500, "The stick did not return to the centre when the mouse stopped: " + value);
			// Back the other way, at half the speed: half the stick, the other side.
			for (var ms = 651; ms <= 1100; ms++)
			{
				if (ms % 8 == 0)
					total -= 16;
				value = Pass(stick, total, ms);
			}
			Assert.IsTrue(Math.Abs(value - (Centre - 0.49 * 32767)) < 0.08 * 32767, "Half the speed back is not half the stick back: " + value);
		}

		[TestMethod, TestCategory("mapping")]
		[Description("Screen follows the pointer from the middle of its screen, half the screen's height being the whole stick")]
		public void Screen_follows_the_pointer()
		{
			var read = MouseStick.ReadScreen;
			try
			{
				var point = new ScreenPoint { X = 960 + 270, Y = 540 - 540, CentreX = 960, CentreY = 540, Half = 540 };
				MouseStick.ReadScreen = () => point;
				var stick = new MouseStick(new MouseStickSettings { Style = MouseStyle.Screen, SmoothingMs = 0 });
				stick.BeginPass(null);
				Assert.IsTrue(Math.Abs(stick.Value(0, 0, 1, Ms(1)) - (Centre + 16384)) < 20, "A quarter screen across is not half the stick.");
				Assert.AreEqual(0, stick.Value(1, 0, 1, Ms(1)), "The top of the screen is not the stick fully up.");
			}
			finally
			{
				MouseStick.ReadScreen = read;
			}
		}

		[TestMethod, TestCategory("mapping")]
		[Description("Screen turned on by a button takes the pointer's place then as its centre, as a stick on a phone starts where the finger lands")]
		public void Screen_held_starts_where_the_pointer_is()
		{
			var read = MouseStick.ReadScreen;
			try
			{
				var point = new ScreenPoint { X = 1500, Y = 300, CentreX = 960, CentreY = 540, Half = 540 };
				MouseStick.ReadScreen = () => point;
				var stick = new MouseStick(new MouseStickSettings { Style = MouseStyle.Screen, SmoothingMs = 0, Active = MouseActive.HeldCentre, Button = 1 });
				stick.BeginPass(Buttons(true));
				Assert.AreEqual(Centre, stick.Value(0, 0, 1, Ms(1)), "The pointer where the button was pressed is not the centre.");
				point.X = 1500 + 270;
				stick.BeginPass(Buttons(true));
				Assert.IsTrue(Math.Abs(stick.Value(0, 0, 1, Ms(2)) - (Centre + 16384)) < 20, "A quarter screen from where it started is not half the stick.");
				stick.BeginPass(Buttons(false));
				Assert.AreEqual(Centre, stick.Value(0, 0, 1, Ms(3)), "Let go, the stick did not return to the centre.");
			}
			finally
			{
				MouseStick.ReadScreen = read;
			}
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("While held, then centre: the mouse moves the stick only while the button is held, from the centre each time, and back to it after")]
		public void Held_then_centre_steers_only_while_held()
		{
			var stick = new MouseStick(new MouseStickSettings { Style = MouseStyle.Position, Active = MouseActive.HeldCentre, Button = 1 });
			Pass(stick, 0, 0);
			Assert.AreEqual(Centre, Pass(stick, 1024, 1), "The mouse moved the stick with the button let go.");
			Assert.AreEqual(Centre, Pass(stick, 1024, 2, true), "Pressed, the stick did not start from the centre.");
			Assert.IsTrue(Math.Abs(Pass(stick, 2048, 3, true) - 49151) < 2, "Held, the mouse did not move the stick.");
			Assert.AreEqual(Centre, Pass(stick, 2048, 4), "Let go, the stick did not return to the centre.");
			Assert.AreEqual(Centre, Pass(stick, 2048, 5, true), "Pressed again, the stick did not start from the centre.");
		}

		[TestMethod, TestCategory("mapping")]
		[Description("While held, then stay: let go, the stick stays where it was, and carries on from there when held again")]
		public void Held_then_stay_keeps_the_stick()
		{
			var stick = new MouseStick(new MouseStickSettings { Style = MouseStyle.Position, Active = MouseActive.HeldStay, Button = 1 });
			Pass(stick, 0, 0, true);
			var held = Pass(stick, 1024, 1, true);
			Assert.AreEqual(held, Pass(stick, 3000, 2), "Let go, the stick moved.");
			Assert.IsTrue(Pass(stick, 3000 + 1024, 3, true) > held, "Held again, it did not carry on from where it stayed.");
		}

		[TestMethod, TestCategory("mapping")]
		[Description("The button turns it on and off: one press on, the next off, and off is the centre")]
		public void Toggle_turns_on_and_off()
		{
			var stick = new MouseStick(new MouseStickSettings { Style = MouseStyle.Position, Active = MouseActive.Toggle, Button = 1 });
			Pass(stick, 0, 0);
			Assert.AreEqual(Centre, Pass(stick, 1024, 1), "Off before the first press, the mouse moved the stick.");
			Pass(stick, 1024, 2, true);
			Assert.IsTrue(stick.IsActive, "A press did not turn it on.");
			Assert.IsTrue(Pass(stick, 2048, 3) > Centre + 10000, "On after the press, the mouse did not move the stick.");
			Pass(stick, 2048, 4, true);
			Assert.IsFalse(stick.IsActive, "A second press did not turn it off.");
			Assert.AreEqual(Centre, Pass(stick, 3000, 5), "Off, the stick is not at the centre.");
		}

		[TestMethod, TestCategory("mapping")]
		[Description("A device counting again from somewhere else, as when it is taken again, moves nothing")]
		public void A_restart_of_the_count_moves_nothing()
		{
			var stick = new MouseStick(new MouseStickSettings { Style = MouseStyle.Position, SensitivityX = 200 });
			stick.BeginPass(null);
			stick.Value(0, 100, 1, Ms(0));
			stick.BeginPass(null);
			Assert.AreEqual(Centre, stick.Value(0, 900000, 2, Ms(1)), "A total from a new count was read as a movement.");
		}

		[TestMethod, TestCategory("mapping")]
		[Description("The routing makes a processor for each stick its Mouse page sets, and none for a page at its defaults")]
		public void The_routing_makes_one_for_each_stick_set()
		{
			var ud = new UserDevice { InstanceGuid = Guid.NewGuid(), CapType = (int)DeviceType.Mouse, IsOnline = true };
			var plain = new PadSetting { LeftThumbAxisX = "a1", PadSettingChecksum = Guid.NewGuid() };
			Assert.IsNull(Routing(ud, plain).RowMouse[0], "A page at its defaults made a processor.");
			var speed = new PadSetting { LeftThumbAxisX = "a1", MouseRightThumbStyle = "1", PadSettingChecksum = Guid.NewGuid() };
			var sticks = Routing(ud, speed).RowMouse[0];
			Assert.IsNotNull(sticks);
			Assert.IsNull(sticks[0], "The left stick, at its defaults, got a processor.");
			Assert.AreEqual(MouseStyle.Speed, sticks[1].Settings.Style);
		}

		static DeviceRouting Routing(UserDevice ud, PadSetting ps)
		{
			var row = new UserSetting { InstanceGuid = ud.InstanceGuid, FileName = Game.FileName, MapTo = (int)MapTo.Controller1, IsEnabled = true, PadSettingChecksum = ps.PadSettingChecksum };
			return DeviceRouting.Build(Game, new[] { row }, new[] { ps }, new[] { ud });
		}

		[TestMethod, TestCategory("mapping"), TestCategory("engine")]
		[Description("Through the engine: the mouse's X on both sticks, the left at its defaults reading the position as before, the right in Speed")]
		public void The_engine_reads_each_stick_in_its_style()
		{
			var convert = EngineSteps.UpdateXiStates(new DInputHelper());
			var ud = new UserDevice
			{
				InstanceGuid = Guid.NewGuid(), CapType = (int)DeviceType.Mouse, JoState = new JoystickState(),
				SourceState = new SourceState(), SourceStateRead = new SourceState(), OriginSourceStateTime = 7,
			};
			ud.IsOnline = true;
			ud.DiRelativeAxisMask = SourceState.TrustedRelativeMask(ud.CapType, 0x1);
			var ps = new PadSetting { LeftThumbAxisX = "a1", RightThumbAxisX = "a1", MouseRightThumbStyle = "1", PadSettingChecksum = Guid.NewGuid() };
			var routing = Routing(ud, ps);
			var row = routing.Rows[0];
			// The engine's own position reading, a quarter of the way right, which the left stick shows as it is.
			ud.SourceState.Axis[0] = 49152;
			var total = 0;
			var watch = Stopwatch.StartNew();
			// About 4,000 counts a second for 300 ms.
			while (watch.ElapsedMilliseconds < 300)
			{
				total += 20;
				ud.SourceStateRead.Axis[0] = total;
				convert(routing);
				Thread.Sleep(5);
			}
			var moving = row.XiState;
			Assert.IsTrue(moving.LeftThumbX > 12000 && moving.LeftThumbX < 20000, "The left stick, at its defaults, does not read the engine's position: " + moving.LeftThumbX);
			// What the counts came to, whatever the PC's sleeps took: the stick is that share of 4,096 counts a second.
			var expected = Math.Min(1, total / watch.Elapsed.TotalSeconds / MouseStick.SpeedCounts) * 32767;
			Assert.IsTrue(moving.RightThumbX > expected * 0.6 && moving.RightThumbX < expected * 1.4 + 2000,
				string.Format("The right stick in Speed reads {0} for a speed that is {1:0} of the stick.", moving.RightThumbX, expected));
			watch.Restart();
			while (watch.ElapsedMilliseconds < 300)
			{
				convert(routing);
				Thread.Sleep(5);
			}
			Assert.IsTrue(Math.Abs((int)row.XiState.RightThumbX) < 1500, "The right stick did not return when the mouse stopped: " + row.XiState.RightThumbX);
		}

		[TestMethod, TestCategory("mapping")]
		[Description("The page's lists load the value a preset stores, and are lists of choices, as the settings manager knows them")]
		public void The_page_lists_load_the_stored_choice()
		{
			Assert.IsTrue(SettingName.IsMouseChoice(SettingName.MouseLeftThumbStyle));
			Assert.IsTrue(SettingName.IsMouseChoice(SettingName.MouseRightThumbActive));
			Assert.IsTrue(SettingName.IsMouseChoice(SettingName.MouseRightThumbButton));
			Assert.IsFalse(SettingName.IsMouseChoice(SettingName.MouseLeftThumbSmoothing));
			Assert.IsFalse(SettingName.IsMouseChoice(SettingName.LeftThumbButton), "A mapping box was taken for a mouse list.");
			using (var page = new MouseStickUserControl())
			{
				SettingsManager.Current.LoadSetting(page.StyleComboBox, SettingName.MouseLeftThumbStyle, "2");
				Assert.AreEqual("2", ((KeyValuePair)page.StyleComboBox.SelectedItem).Value);
				SettingsManager.Current.LoadSetting(page.ButtonComboBox, SettingName.MouseLeftThumbButton, "3");
				StringAssert.StartsWith(((KeyValuePair)page.ButtonComboBox.SelectedItem).Key, "Button 3");
				Assert.IsFalse(page.ButtonComboBox.Enabled, "The button is not greyed out while Active is Always.");
				SettingsManager.Current.LoadSetting(page.ActiveComboBox, SettingName.MouseLeftThumbActive, "1");
				Assert.IsTrue(page.ButtonComboBox.Enabled, "The button is greyed out while a button turns the mouse on.");
				// The tuning values are sliders, as on the stick pages: a stored value loads, and one past the end stops there.
				SettingsManager.Current.LoadSetting(page.SensitivityXTrackBar, SettingName.MouseLeftThumbSensitivityX, "150");
				Assert.AreEqual(150, page.SensitivityXTrackBar.Value);
				SettingsManager.Current.LoadSetting(page.ReturnTrackBar, SettingName.MouseLeftThumbReturn, "5000");
				Assert.AreEqual(page.ReturnTrackBar.Maximum, page.ReturnTrackBar.Value, "A time past the slider's end did not stop at the end.");
				// The style loaded above is Screen, which smooths and has no way back to the centre of its own.
				Assert.IsTrue(page.SmoothingTrackBar.Enabled, "Smoothing is greyed out in Screen.");
				Assert.IsFalse(page.ReturnTrackBar.Enabled, "Back to centre is not greyed out in Screen.");
			}
		}
	}
}
