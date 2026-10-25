// @under-test: Engine/Common/MapExpression.cs, Engine/Common/MapExpressionUnits.cs, App.v4/Common/DInput/DeviceRouting.cs, App.v4/Common/DInput/DInputHelper.Step3.UpdateXiStates.cs
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
	/// A formula can name a control of any device, as an AutoHotkey script does: <c>=c1.d1.b1</c> is button 1 of device 1
	/// on Controller 1, from whichever mapping it is written in. On that device's own mapping it reads what <c>=b1</c> reads.
	/// </summary>
	[TestClass]
	public class FormulaDevicePathTest
	{
		static MapExpression Parse(string text)
		{
			MapExpression result;
			string error;
			int position;
			Assert.IsTrue(MapExpression.TryParse(text, out result, out error, out position), "'" + text + "' was refused: " + error);
			return result;
		}

		static string Refusal(string text)
		{
			MapExpression result;
			string error;
			int position;
			Assert.IsFalse(MapExpression.TryParse(text, out result, out error, out position), "'" + text + "' was accepted.");
			return error;
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A source may name its controller and device in front of it, both or neither")]
		public void A_source_names_its_controller_and_device_in_front()
		{
			var expression = Parse("=c1.d2.b1+b1+C4.D16.a3*0.5");
			Assert.AreEqual(3, expression.References.Count, "c1.d2.b1 and b1 were taken for one source.");
			Assert.AreEqual(new MapReference('b', 1, 1, 2), expression.References[0]);
			Assert.AreEqual(new MapReference('b', 1), expression.References[1]);
			Assert.AreEqual(new MapReference('a', 3, 4, 16), expression.References[2], "Capital letters were not read as the small ones.");
			Assert.AreEqual("c1.d2.b1", expression.References[0].ToString());
			StringAssert.Contains(Refusal("=c5.d1.b1"), "c1 to c4");
			StringAssert.Contains(Refusal("=c1.b1"), "device");
			StringAssert.Contains(Refusal("=c1.d1"), "control");
			StringAssert.Contains(Refusal("=c1.d0.b1"), "device");
			Refusal("=c1.d1.q1");
			Assert.AreEqual(0.5f, Parse("=d1*0.5").Evaluate(new[] { 1f }), "A hat direction followed by a number is no longer read.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A source with a device in front reads that device's state; one without reads the row's own")]
		public void A_named_device_is_read_through_the_engine()
		{
			var own = new SourceState();
			var named = new SourceState();
			named.Buttons[0] = true;
			var expression = Parse("=c2.d1.b1*10+b1");
			var values = new float[MapExpression.MaxReferences];
			float value;
			Assert.IsTrue(MapExpressionUnits.TryEvaluate(expression, own, false, values, out value,
				(pad, device) => pad == 2 && device == 1 ? named : null));
			Assert.AreEqual(10f, value, "The named device's button was not read, or the row's own was read in its place.");
			Assert.IsTrue(MapExpressionUnits.TryEvaluate(expression, own, false, values, out value));
			Assert.AreEqual(0f, value, "With no other device to read, a named device did not read as at rest.");
		}

		static UserDevice OnlineDevice()
		{
			var device = new UserDevice { InstanceGuid = Guid.NewGuid(), JoState = new JoystickState(), SourceState = new SourceState(new JoystickState()) };
			device.IsOnline = true;
			return device;
		}

		[TestMethod, TestCategory("engine"), TestCategory("mapping"), TestCategory("critical")]
		[Description("=c1.d1.b1 reads Controller 1's device 1 from any mapping, and on that device's own mapping reads as =b1 does")]
		public void A_formula_reads_a_named_device_from_any_mapping()
		{
			var first = OnlineDevice();
			var second = OnlineDevice();
			// A reads the row's own button 1, B button 1 of device 1 on Controller 1, on both controllers.
			var ownSettings = new PadSetting { ButtonA = "=b1", ButtonB = "=c1.d1.b1", PadSettingChecksum = Guid.NewGuid() };
			var otherSettings = new PadSetting { ButtonA = "=b1", ButtonB = "=c1.d1.b1", PadSettingChecksum = Guid.NewGuid() };
			var ownRow = Row(first.InstanceGuid, MapTo.Controller1, ownSettings);
			var otherRow = Row(second.InstanceGuid, MapTo.Controller2, otherSettings);
			var routing = DeviceRouting.Build(Game, new[] { ownRow, otherRow }, new[] { ownSettings, otherSettings }, new[] { first, second });
			Assert.AreSame(first, routing.GetDevice(1, 1));
			Assert.AreSame(second, routing.GetDevice(2, 1));
			Assert.IsNull(routing.GetDevice(1, 2), "A device number no row has names a device.");
			var convert = EngineSteps.UpdateXiStates(new DInputHelper());

			first.SourceState.Buttons[0] = true;
			convert(routing);
			Assert.IsTrue(ownRow.XiState.Buttons.HasFlag(GamepadButtonFlags.A), "=b1 did not read the device's own button.");
			Assert.IsTrue(ownRow.XiState.Buttons.HasFlag(GamepadButtonFlags.B), "=c1.d1.b1 on Controller 1's device 1 did not read as =b1 does.");
			Assert.IsFalse(otherRow.XiState.Buttons.HasFlag(GamepadButtonFlags.A), "=b1 on Controller 2 read another device.");
			Assert.IsTrue(otherRow.XiState.Buttons.HasFlag(GamepadButtonFlags.B), "=c1.d1.b1 on Controller 2 did not read Controller 1's device 1.");

			first.SourceState.Buttons[0] = false;
			second.SourceState.Buttons[0] = true;
			convert(routing);
			Assert.IsFalse(ownRow.XiState.Buttons.HasFlag(GamepadButtonFlags.B));
			Assert.IsTrue(otherRow.XiState.Buttons.HasFlag(GamepadButtonFlags.A), "=b1 on Controller 2 did not read its own device.");
			Assert.IsFalse(otherRow.XiState.Buttons.HasFlag(GamepadButtonFlags.B), "=c1.d1.b1 on Controller 2 read its own device.");

			first.IsOnline = false;
			first.SourceState.Buttons[0] = true;
			convert(routing);
			Assert.IsFalse(otherRow.XiState.Buttons.HasFlag(GamepadButtonFlags.B), "A named device that is offline still pressed a button.");
		}
	}
}
