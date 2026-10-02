// @under-test: docs/.attachments/x360ce-v4-rc-transmitter.xml, App.v4/Common/SettingsManager.XML.cs
// @area: presets   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using x360ce.App;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>The preset Help offers for flying with a radio control transmitter.</summary>
	/// <remarks>
	/// Help links to this file and says to open it with Load Preset, Open File. A file that does not load,
	/// or loads into something other than Help describes, leaves somebody with a controller that does
	/// nothing and no idea why.
	/// </remarks>
	[TestClass]
	public class RcTransmitterPresetTest
	{
		static PadSetting Load()
		{
			return SettingsManager.LoadPadSetting(Path.Combine(Ui.RepoRoot.FullName, "docs", ".attachments", "x360ce-v4-rc-transmitter.xml"));
		}

		static Map Arm(List<Map> maps)
		{
			return maps.Single(m => m.Target == TargetType.Button && m.ButtonFlag == GamepadButtonFlags.LeftShoulder);
		}

		static void AssertAxis(List<Map> maps, TargetType target, int index)
		{
			var map = maps.Single(m => m.Target == target && !m.AxisValue.HasValue);
			Assert.AreEqual(MapType.Axis, map.Type, target + " is not read from a whole axis.");
			Assert.AreEqual(index, map.Index, target + " reads the wrong axis.");
		}

		[TestMethod, TestCategory("presets"), TestCategory("smoke")]
		[Description("The RC transmitter preset opens as a preset file")]
		public void The_preset_opens_as_a_preset_file()
		{
			var ps = Load();
			Assert.AreEqual("a4", ps.LeftThumbAxisX, "Yaw is Axis 4 on the left stick.");
			Assert.AreEqual("a3", ps.LeftThumbAxisY, "Throttle is Axis 3 on the left stick.");
			Assert.AreEqual("a1", ps.RightThumbAxisX, "Roll is Axis 1 on the right stick.");
			Assert.AreEqual("a2", ps.RightThumbAxisY, "Pitch is Axis 2 on the right stick.");
			Assert.AreEqual("h1", ps.LeftShoulder, "The arm switch is Slider 1 on the left bumper, read from the middle up.");
			Assert.AreEqual("16384", ps.LeftShoulderDeadZone);
		}

		[TestMethod, TestCategory("presets"), TestCategory("critical")]
		[Description("The preset maps the sticks and the arm switch the way Help says")]
		public void The_preset_maps_as_help_says()
		{
			var maps = Load().Maps;
			AssertAxis(maps, TargetType.LeftThumbX, 4);
			AssertAxis(maps, TargetType.LeftThumbY, 3);
			AssertAxis(maps, TargetType.RightThumbX, 1);
			AssertAxis(maps, TargetType.RightThumbY, 2);
			var arm = Arm(maps);
			Assert.AreEqual(MapType.HSlider, arm.Type, "The arm switch is not read as half a slider.");
			Assert.AreEqual(1, arm.Index, "The arm switch is not Slider 1.");
			Assert.AreEqual(16384, arm.DeadZone, "The arm switch's press point moved.");
		}

		[TestMethod, TestCategory("presets"), TestCategory("critical")]
		[Description("The arm switch presses the bumper at its far end only")]
		public void The_arm_switch_presses_at_its_far_end_only()
		{
			var arm = Arm(Load().Maps);
			Func<int, bool> pressedAt = reading => ConvertHelper.IsAxisButtonPressed(reading, arm.IsInverted, arm.IsHalf, arm.DeadZone, false);
			Assert.IsTrue(pressedAt(65535), "Armed, at the far end, the bumper is not pressed.");
			Assert.IsFalse(pressedAt(0), "Disarmed, at the near end, the bumper is pressed.");
			Assert.IsFalse(pressedAt(32767), "The middle of a three-position switch presses the bumper.");
			Assert.IsFalse(pressedAt(49152), "Three quarters of the way is the press point itself, not past it.");
		}

		[TestMethod, TestCategory("presets")]
		[Description("The preset carries the checksum its own settings give")]
		public void The_preset_carries_its_own_checksum()
		{
			var ps = Load();
			var stored = ps.PadSettingChecksum;
			Assert.AreEqual(ps.CleanAndGetCheckSum(), stored,
				"The file's PadSettingChecksum is not the one its settings give; write the expected value into the file.");
		}
	}
}
