// @under-test: Engine/Input/States/SourceState.cs, App.v4/Common/DInput/DInputHelper.Step2.UpdateDiStates.cs, App.v4/Common/DInput/DInputHelper.Step3.UpdateXiStates.cs
// @area: engine   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.DirectInput;
using System;
using System.IO;
using x360ce.App.DInput;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>A device read as a gamepad is never summed as relative axes, whatever its objects declare.</summary>
	/// <remarks>
	/// The relative flag is the device's own claim, and a cheap gamepad's driver makes it for sticks that report where
	/// they are. A control worked out as movement stays at one end until it is moved back, which is right for a mouse
	/// and pins a stick. So the engine believes the flag only on a device that is not a joystick, gamepad, wheel, flight
	/// stick or first-person controller. The masks are worked out from a DirectInput device's objects, which needs a
	/// device in hand, so the decision is tested on its own and its use is pinned in the engine's reading step.
	/// </remarks>
	[TestClass]
	public class RelativeAxisGuardTest
	{
		/// <summary>The DirectInput types read as gamepads: their sticks report where they are.</summary>
		static readonly DeviceType[] GamepadLike = { DeviceType.Joystick, DeviceType.Gamepad, DeviceType.Driving, DeviceType.Flight, DeviceType.FirstPerson };

		/// <summary>The other DirectInput types: what their objects declare is believed.</summary>
		static readonly DeviceType[] Believed = { DeviceType.Device, DeviceType.Mouse, DeviceType.Keyboard, DeviceType.ControlDevice, DeviceType.ScreenPointer, DeviceType.Remote, DeviceType.Supplemental };

		/// <summary>Every axis, and every slider, declared relative.</summary>
		const int AllAxes = (1 << SourceState.MaxAxis) - 1;
		const int AllSliders = (1 << SourceState.MaxSliders) - 1;

		static readonly UserGame Game = new UserGame { FileName = "relative-guard.exe", FileProductName = "Relative guard", EnableMask = (int)MapToMask.Controller1 };

		/// <summary>The row Controller 1 converts for a device of <paramref name="type"/> whose objects declare axis 1 and slider 1 relative, every control at rest in the middle.</summary>
		static UserSetting Converted(EngineSteps.XiStates convert, DeviceType type)
		{
			var ud = new UserDevice { InstanceGuid = Guid.NewGuid(), CapType = (int)type, JoState = new JoystickState(), SourceState = new SourceState() };
			ud.IsOnline = true;
			// As the engine sets them once, when the device's objects are first read.
			ud.DiRelativeAxisMask = SourceState.TrustedRelativeMask(ud.CapType, 0x1);
			ud.DiRelativeSliderMask = SourceState.TrustedRelativeMask(ud.CapType, 0x1);
			var ps = new PadSetting { LeftTrigger = "a1", ButtonA = "a1", ButtonX = "s1", RightTrigger = "a2", PadSettingChecksum = Guid.NewGuid() };
			var row = new UserSetting { InstanceGuid = ud.InstanceGuid, FileName = Game.FileName, MapTo = (int)MapTo.Controller1, IsEnabled = true, PadSettingChecksum = ps.PadSettingChecksum };
			var routing = DeviceRouting.Build(Game, new[] { row }, new[] { ps }, new[] { ud });
			ud.SourceState.Axis[0] = -short.MinValue;
			ud.SourceState.Axis[1] = -short.MinValue;
			ud.SourceState.Sliders[0] = -short.MinValue;
			convert(routing);
			return row;
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A joystick, gamepad, wheel, flight stick or first-person controller keeps no relative axis or slider, however many its objects declare")]
		public void A_device_read_as_a_gamepad_keeps_no_relative_control()
		{
			foreach (var type in GamepadLike)
			{
				Assert.AreEqual(0, SourceState.TrustedRelativeMask((int)type, 0x1 | 0x2), type + " keeps an axis its objects declare relative.");
				Assert.AreEqual(0, SourceState.TrustedRelativeMask((int)type, AllAxes), type + " keeps an axis when its objects declare every one relative.");
				Assert.AreEqual(0, SourceState.TrustedRelativeMask((int)type, AllSliders), type + " keeps a slider when its objects declare every one relative.");
			}
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("Every other device, a mouse, a keyboard, a screen pointer, a remote, keeps the relative axes and sliders its objects declare, and no more")]
		public void Every_other_device_keeps_what_its_objects_declare()
		{
			Assert.AreEqual(GamepadLike.Length + Believed.Length, Enum.GetValues(typeof(DeviceType)).Length,
				"A DirectInput type is neither read as a gamepad nor believed here: decide which.");
			foreach (var type in Believed)
			{
				Assert.AreEqual(0x1 | 0x4, SourceState.TrustedRelativeMask((int)type, 0x1 | 0x4), type + " loses an axis its objects declare relative.");
				Assert.AreEqual(AllAxes, SourceState.TrustedRelativeMask((int)type, AllAxes), type + " loses an axis when its objects declare every one relative.");
				Assert.AreEqual(AllSliders, SourceState.TrustedRelativeMask((int)type, AllSliders), type + " loses a slider when its objects declare every one relative.");
				Assert.AreEqual(0, SourceState.TrustedRelativeMask((int)type, 0), type + " gains an axis its objects do not declare relative.");
			}
			Assert.AreEqual(0x5, SourceState.TrustedRelativeMask(0, 0x5), "A type not named is not read as a gamepad.");
			Assert.AreEqual(0x5, SourceState.TrustedRelativeMask(99, 0x5), "A type not named is not read as a gamepad.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("Axis 1 and slider 1 declared relative: a gamepad's are read as they report, a mouse's are worked out as movement and at rest in the middle")]
		public void A_gamepad_that_declares_relative_axes_is_read_as_it_reports()
		{
			var convert = EngineSteps.UpdateXiStates(new DInputHelper());
			var pressed = SharpDX.XInput.GamepadButtonFlags.A | SharpDX.XInput.GamepadButtonFlags.X;
			foreach (var type in GamepadLike)
			{
				var pad = Converted(convert, type).XiState;
				Assert.AreEqual(127, pad.LeftTrigger, type + ": a trigger on a control the driver called relative reads only the half past the middle.");
				Assert.AreEqual(pressed, pad.Buttons, type + ": a button on a control the driver called relative is held released in the middle.");
				Assert.AreEqual(127, pad.RightTrigger, type + ": a trigger on an axis that reports where it is was changed.");
			}
			foreach (var type in Believed)
			{
				var pad = Converted(convert, type).XiState;
				Assert.AreEqual(0, pad.LeftTrigger, type + ": a trigger on a moving axis at rest is pressed.");
				Assert.AreEqual(SharpDX.XInput.GamepadButtonFlags.None, pad.Buttons, type + ": a button on a moving axis or slider at rest is pressed.");
				Assert.AreEqual(127, pad.RightTrigger, type + ": a trigger on an axis that reports where it is was changed.");
			}
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("The masks are decided once, when the device's objects are first read, through the guard, and nothing sets them again")]
		public void The_masks_are_decided_once_when_the_objects_are_read()
		{
			var step2 = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "DInputHelper.Step2.UpdateDiStates.cs"));
			var objects = Ui.Between(step2, "var dos = AppHelper.GetDeviceObjects(device);", "// Reading the effects lets go of the XInput library");
			StringAssert.Contains(objects, "ud.DiRelativeAxisMask = SourceState.TrustedRelativeMask(ud.CapType, relativeMask);");
			StringAssert.Contains(objects, "ud.DiRelativeSliderMask = SourceState.TrustedRelativeMask(ud.CapType, relativeSliderMask);");
			Assert.AreEqual(1, Ui.Count(step2, "ud.DiRelativeAxisMask ="), "The relative axes are set somewhere else than where the objects are read.");
			Assert.AreEqual(1, Ui.Count(step2, "ud.DiRelativeSliderMask ="), "The relative sliders are set somewhere else than where the objects are read.");
			StringAssert.Contains(step2, "var moving = ud.Device != null && (ud.DiRelativeAxisMask | ud.DiRelativeSliderMask) != 0;",
				"A device with no relative control is read as it reports.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("performance")]
		[Description("Deciding a device's relative controls hands nothing to the collector")]
		public void Deciding_hands_nothing_to_the_collector()
		{
			const int calls = 20000;
			var kept = 0;
			var allocated = Allocations.FewestBytes(5, () =>
			{
				for (var i = 0; i < calls; i++)
					kept |= SourceState.TrustedRelativeMask(20 + i % 3, 0x7) | SourceState.TrustedRelativeMask(18 + i % 2, 0x7);
			});
			Assert.AreEqual(0x7, kept);
			Assert.IsTrue(allocated < calls, calls + " decisions handed the collector " + allocated + " bytes.");
		}
	}
}
