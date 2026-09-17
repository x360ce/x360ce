// @under-test: App.v4/Common/AutoMapHelper.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using x360ce.App;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// Auto Preset used to know mice, keyboards and gamepads, so a steering wheel got a gamepad
	/// guess: pedals on the right stick, nothing on the triggers. The wheel branch is checked
	/// against the Logitech preset that ships with the program, because that preset is what
	/// wheel owners have been loading by hand.
	/// </summary>
	[TestClass]
	public class AutoPresetWheelTest
	{
		static DeviceObjectItem Axis(JoystickOffset offset, Guid type, int slot, string name)
		{
			return new DeviceObjectItem((int)offset, type, ObjectAspect.Position, DeviceObjectTypeFlags.AbsoluteAxis, slot, name) { DiIndex = slot };
		}

		static DeviceObjectItem Button(int index)
		{
			return new DeviceObjectItem((int)JoystickOffset.Buttons0 + index, ObjectGuid.Button, 0, DeviceObjectTypeFlags.PushButton, index, "Button " + index) { DiIndex = index };
		}

		static DeviceObjectItem Pov()
		{
			return new DeviceObjectItem((int)JoystickOffset.PointOfViewControllers0, ObjectGuid.PovController, 0, DeviceObjectTypeFlags.PointOfViewController, 0, "Hat Switch") { DiIndex = 0 };
		}

		/// <summary>A wheel as the test-device helper would report it, with the given controls.</summary>
		static UserDevice Wheel(params DeviceObjectItem[] objects)
		{
			var ud = TestDeviceHelper.NewUserDevice();
			ud.CapType = (int)DeviceType.Driving;
			ud.DeviceObjects = objects;
			ud.CapButtonCount = objects.Count(x => x.Type == ObjectGuid.Button);
			ud.CapPovCount = objects.Count(x => x.Type == ObjectGuid.PovController);
			return ud;
		}

		/// <summary>The G27 with its pedals separated: wheel on X, gas on Y, brake on RZ, clutch on a slider.</summary>
		static DeviceObjectItem[] SeparatedPedals()
		{
			var list = new List<DeviceObjectItem>
			{
				Axis(JoystickOffset.X, ObjectGuid.XAxis, 0, "X Axis"),
				Axis(JoystickOffset.Y, ObjectGuid.YAxis, 1, "Y Axis"),
				Axis(JoystickOffset.RotationZ, ObjectGuid.RzAxis, 5, "Z Rotation"),
				Axis(JoystickOffset.Sliders0, ObjectGuid.Slider, 0, "Slider"),
				Pov(),
			};
			for (var i = 0; i < 23; i++)
				list.Add(Button(i));
			return list.ToArray();
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A wheel with separate pedals gets the shipped Logitech G27 preset's mapping")]
		public void Separated_pedals_match_the_shipped_G27_preset()
		{
			var ps = AutoMapHelper.GetAutoPreset(Wheel(SeparatedPedals()));
			var preset = new Ini(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Presets", "x360ce.Logitech_G27_Racing_Wheel_USB.ini"));
			var expected = new Dictionary<string, string>
			{
				{ "Left Analog X", ps.LeftThumbAxisX },
				{ "Left Trigger", ps.LeftTrigger },
				{ "Right Trigger", ps.RightTrigger },
				{ "A", ps.ButtonA },
				{ "B", ps.ButtonB },
				{ "X", ps.ButtonX },
				{ "Y", ps.ButtonY },
				{ "Left Shoulder", ps.LeftShoulder },
				{ "Right Shoulder", ps.RightShoulder },
				{ "Back", ps.ButtonBack },
				{ "Start", ps.ButtonStart },
				{ "D-pad POV", ps.DPad },
			};
			var wrong = expected
				.Where(x => Shipped(preset, x.Key) != x.Value)
				.Select(x => string.Format("{0}: preset '{1}', auto '{2}'", x.Key, Shipped(preset, x.Key), x.Value))
				.ToList();
			Assert.AreEqual(0, wrong.Count, "Auto Preset differs from the shipped preset:" + Environment.NewLine + string.Join(Environment.NewLine, wrong));
			Assert.IsTrue(string.IsNullOrEmpty(ps.RightThumbAxisX), "Nothing is guessed onto the right stick; a wheel has none.");
		}

		/// <summary>
		/// A preset value in the setting's own notation. The old preset files write a plain axis
		/// and the POV as a bare number, which the reader has always taken as that axis or POV.
		/// </summary>
		static string Shipped(Ini preset, string key)
		{
			var value = preset.GetValue("PAD1", key);
			if (!value.All(char.IsDigit) || value.Length == 0)
				return value;
			if (key.Contains("Analog"))
				return SettingName.SType.Axis + value;
			if (key.Contains("POV"))
				return SettingName.SType.POV + value;
			return value;
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A wheel with both pedals on one axis gets one half of it per trigger")]
		public void Combined_pedals_split_one_axis_between_the_triggers()
		{
			var ps = AutoMapHelper.GetAutoPreset(Wheel(
				Axis(JoystickOffset.X, ObjectGuid.XAxis, 0, "X Axis"),
				Axis(JoystickOffset.Y, ObjectGuid.YAxis, 1, "Y Axis"),
				Button(0), Button(1), Button(2), Button(3)));
			Assert.AreEqual("a1", ps.LeftThumbAxisX);
			// The help's recipe for combined pedals: brake is the half above centre, gas the half below.
			Assert.AreEqual("x2", ps.LeftTrigger);
			Assert.AreEqual("x-2", ps.RightTrigger);
			Assert.AreEqual("", ps.DPad);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("An axis is found by the name the driver gives it, wherever it sits")]
		public void Pedals_are_found_by_name_before_slot()
		{
			// Brake on Z and gas on RY, named as some drivers name them: the slots say nothing.
			var ps = AutoMapHelper.GetAutoPreset(Wheel(
				Axis(JoystickOffset.X, ObjectGuid.XAxis, 0, "Steering"),
				Axis(JoystickOffset.Z, ObjectGuid.ZAxis, 2, "Brake"),
				Axis(JoystickOffset.RotationY, ObjectGuid.RyAxis, 4, "Accelerator"),
				Button(0)));
			Assert.AreEqual("a1", ps.LeftThumbAxisX);
			Assert.AreEqual("a-3", ps.LeftTrigger);
			Assert.AreEqual("a-5", ps.RightTrigger);
		}

		[TestMethod, TestCategory("mapping")]
		[Description("A name hint never takes a button: a gamepad named after its triggers still maps them to axes")]
		public void Name_hints_match_axes_only()
		{
			var ud = TestDeviceHelper.NewUserDevice();
			// A pad whose buttons carry the names the axis hints look for.
			var objects = ud.DeviceObjects.ToList();
			objects.First(x => x.Type == ObjectGuid.Button && x.DiIndex == 6).Name = "L2";
			objects.First(x => x.Type == ObjectGuid.Button && x.DiIndex == 7).Name = "R2";
			ud.DeviceObjects = objects.ToArray();
			var ps = AutoMapHelper.GetAutoPreset(ud);
			// The test pad has no RZ, so both triggers come from the halves of its Z axis.
			Assert.AreEqual("x3", ps.LeftTrigger, "The trigger comes from the axis, not the button named L2.");
			Assert.AreEqual("x-3", ps.RightTrigger, "The trigger comes from the axis, not the button named R2.");
			Assert.AreEqual("7", ps.ButtonBack, "The button named L2 stays a button.");
			Assert.AreEqual("8", ps.ButtonStart, "The button named R2 stays a button.");
		}
	}
}
