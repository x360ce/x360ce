// @under-test: Engine/Common/ConvertHelper.cs, App.v4/Common/DInput/DeviceRouting.cs, App.v4/Common/DInput/DInputHelper.Step3.UpdateXiStates.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.DirectInput;
using SharpDX.XInput;
using System;
using x360ce.App.DInput;
using x360ce.Engine;
using x360ce.Engine.Data;
using static x360ce.Tests.DeviceRoutingFixtures;

namespace x360ce.Tests
{
	/// <summary>
	/// Axis to D-Pad: a device's X and Y axes, its main stick, press the controller's D-Pad, for a pad whose direction
	/// buttons report as a stick.
	/// </summary>
	/// <remarks>
	/// Readings are DirectInput's, 0 to 65535, Y growing downwards. The dead zone and the offset are stored as x360ce 3
	/// stored them, 0 to 256; 256 is the dead zone's default. The dead zone's scale is half the travel less the release
	/// margin, the offset's the whole of half the travel.
	/// </remarks>
	[TestClass]
	public class AxisToDPadTest
	{
		const int Middle = short.MaxValue;
		const GamepadButtonFlags DPad = GamepadButtonFlags.DPadUp | GamepadButtonFlags.DPadDown | GamepadButtonFlags.DPadLeft | GamepadButtonFlags.DPadRight;

		static GamepadButtonFlags Pressed(int x, int y, int deadZone = 256, int offset = 0, GamepadButtonFlags was = GamepadButtonFlags.None)
		{
			return ConvertHelper.GetAxisToDPadButtons(x, y, ConvertHelper.GetAxisToDPadPressPoint(deadZone), ConvertHelper.GetAxisToDPadShift(offset), was);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Right and left come from X, down and up from Y, and a corner presses the two beside it")]
		public void Each_direction_comes_from_its_side_of_the_stick()
		{
			Assert.AreEqual(GamepadButtonFlags.None, Pressed(Middle, Middle));
			Assert.AreEqual(GamepadButtonFlags.DPadRight, Pressed(ushort.MaxValue, Middle));
			Assert.AreEqual(GamepadButtonFlags.DPadLeft, Pressed(0, Middle));
			Assert.AreEqual(GamepadButtonFlags.DPadDown, Pressed(Middle, ushort.MaxValue));
			Assert.AreEqual(GamepadButtonFlags.DPadUp, Pressed(Middle, 0));
			Assert.AreEqual(GamepadButtonFlags.DPadUp | GamepadButtonFlags.DPadRight, Pressed(ushort.MaxValue, 0));
			Assert.AreEqual(GamepadButtonFlags.DPadDown | GamepadButtonFlags.DPadLeft, Pressed(0, ushort.MaxValue));
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("The default dead zone presses close to the end, where a pad's direction buttons put the axis even when it falls a little short, and lets go before the axis is back at rest")]
		public void The_default_dead_zone_presses_close_to_the_end()
		{
			// 256 of 256: a release margin short of the end, 30719 past the middle.
			const int right = 32768 + 32767 - ConvertHelper.AxisButtonReleaseMargin;
			Assert.AreEqual(GamepadButtonFlags.None, Pressed(right, Middle));
			Assert.AreEqual(GamepadButtonFlags.DPadRight, Pressed(right + 1, Middle));
			Assert.AreEqual(GamepadButtonFlags.DPadRight, Pressed(ushort.MaxValue - 2, Middle), "A pad that falls two steps short of the end does not press.");
			Assert.AreEqual(GamepadButtonFlags.DPadLeft, Pressed(2, Middle));
			Assert.AreEqual(GamepadButtonFlags.None, Pressed(Middle + 16384, Middle, 256, 0, GamepadButtonFlags.DPadRight), "Half way back, the direction is still held.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A dead zone of half the scale presses half way along it and lets go a little before, as any button an axis drives")]
		public void A_smaller_dead_zone_presses_sooner_and_lets_go_a_little_before_it()
		{
			// 128 of 256: half of 30719 past the middle, so right presses above 32768 + 15359.
			const int pressPoint = 32768 + 15359;
			Assert.AreEqual(GamepadButtonFlags.None, Pressed(pressPoint, Middle, 128));
			Assert.AreEqual(GamepadButtonFlags.DPadRight, Pressed(pressPoint + 1, Middle, 128));
			Assert.AreEqual(GamepadButtonFlags.DPadRight, Pressed(pressPoint - 100, Middle, 128, 0, GamepadButtonFlags.DPadRight),
				"A reading just below the press point let go of a direction it held.");
			Assert.AreEqual(GamepadButtonFlags.None, Pressed(pressPoint - ConvertHelper.AxisButtonReleaseMargin, Middle, 128, 0, GamepadButtonFlags.DPadRight),
				"A reading at the release point still holds the direction.");
		}

		[TestMethod, TestCategory("mapping")]
		[Description("The offset moves the centre towards the right and the bottom")]
		public void The_offset_moves_the_centre()
		{
			// 64 of 256 is a quarter of half the travel: 8192 steps.
			Assert.AreEqual(GamepadButtonFlags.None, Pressed(Middle + 8192, Middle + 8192, 32, 64), "Resting at the moved centre presses a direction.");
			Assert.AreEqual(GamepadButtonFlags.DPadLeft | GamepadButtonFlags.DPadUp, Pressed(Middle, Middle, 32, 64),
				"The middle of the travel is left of and above the moved centre, past a dead zone of 3839.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Switched on, the device's X and Y axes press the controller's D-Pad as the engine converts the row; switched off, they press nothing")]
		public void The_engine_presses_the_D_Pad_from_the_X_and_Y_axes()
		{
			var ud = new UserDevice { InstanceGuid = Guid.NewGuid(), JoState = new JoystickState(), SourceState = new SourceState() };
			ud.IsOnline = true;
			// Pushed right and up.
			ud.SourceState.Axis[0] = ushort.MaxValue;
			ud.SourceState.Axis[1] = 0;
			var xi = EngineSteps.UpdateXiStates(new DInputHelper());
			foreach (var enabled in new[] { true, false })
			{
				var ps = new PadSetting { AxisToDPadEnabled = enabled ? "1" : "0", PadSettingChecksum = Guid.NewGuid() };
				var row = Row(ud.InstanceGuid, MapTo.Controller1, ps);
				xi(DeviceRouting.Build(Game, new[] { row }, new[] { ps }, new[] { ud }));
				Assert.AreEqual(enabled ? GamepadButtonFlags.DPadUp | GamepadButtonFlags.DPadRight : GamepadButtonFlags.None, row.XiState.Buttons & DPad,
					enabled ? "Axis to D-Pad on, the stick pushed right and up does not press Up and Right." : "Axis to D-Pad off, the stick presses the D-Pad.");
			}
		}
	}
}
