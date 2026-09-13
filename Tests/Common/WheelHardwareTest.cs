// @under-test: Engine/Common/ForceFeedbackState.cs, Engine/Common/LogitechWheel.cs
// @area: devices   @layer: hardware
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.DirectInput;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// Moves an attached Logitech wheel to check two things nothing but the wheel can answer:
	/// that a force "towards the high end of the axis" moves it that way, which is the sign the
	/// centering spring relies on, and that the range report widens its travel, seen as the time
	/// the same push takes from one stop to the other.
	/// </summary>
	/// <remarks>
	/// The wheel is pushed to its stops, so hands stay off it. It is left in its power-up range.
	/// Needs the wheel free: the program must not be running.
	/// </remarks>
	[TestClass]
	public class WheelHardwareTest
	{
		const int Push = 35;
		const int StopZone = SpringCalibration.AxisMax * 15 / 100;

		[TestMethod, TestCategory("devices"), TestCategory("ui-interactive"), TestCategory("requires-wheel")]
		[Description("A force towards the high end moves the wheel that way, and the range report widens its travel")]
		public void The_wheel_moves_the_way_the_spring_expects_and_takes_its_range()
		{
			using (var manager = new DirectInput())
			{
				var instance = manager.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly)
					.FirstOrDefault(x => x.ForceFeedbackDriverGuid != Guid.Empty);
				if (instance == null)
					Assert.Inconclusive("No force feedback controller is attached.");
				using (var form = new Form())
				using (var device = new Joystick(manager, instance.InstanceGuid))
				{
					var vendorId = device.Properties.VendorId;
					var productId = device.Properties.ProductId;
					if (!LogitechWheel.SupportsRange(vendorId, productId))
						Assert.Inconclusive(string.Format("{0} ({1:X4}:{2:X4}) is not a Logitech wheel that takes the range report.", instance.ProductName, vendorId, productId));
					var path = device.Properties.InterfacePath;
					var actuator = device.GetObjects(DeviceObjectTypeFlags.All)
						.First(x => x.ObjectId.Flags.HasFlag(DeviceObjectTypeFlags.ForceFeedbackActuator));
					device.SetCooperativeLevel(form.Handle, CooperativeLevel.Background | CooperativeLevel.Exclusive);
					device.Acquire();
					var parameters = PushParameters((int)actuator.ObjectId);
					var force = new ConstantForce();
					parameters.Parameters = force;
					using (var effect = new Effect(device, EffectGuid.ConstantForce, parameters))
					{
						try
						{
							// Towards the low end first, so the wheel starts every run from the same place.
							var atLow = Drive(device, effect, parameters, force, -Push, 4000, x => x <= StopZone);
							Assert.IsTrue(atLow, "Pushed towards the low end for four seconds, the wheel did not get there. "
								+ "Either the sign is the other way round from what the spring assumes, or the wheel is held.");
							var withPowerUpRange = TravelMs(device, effect, parameters, force);
							Console.WriteLine("Stop to stop at the power-up range: {0} ms", withPowerUpRange);

							Assert.IsTrue(LogitechWheel.SetRange(path, 900), "The wheel did not take the range report.");
							Thread.Sleep(500);
							Drive(device, effect, parameters, force, -Push, 6000, x => x <= StopZone);
							var withFullRange = TravelMs(device, effect, parameters, force);
							Console.WriteLine("Stop to stop at 900 degrees: {0} ms", withFullRange);
							Assert.IsTrue(withFullRange > withPowerUpRange * 2, string.Format(
								"The same push took {0} ms across the wheel before the range report and {1} ms after it. "
								+ "A wheel that went from 200 to 900 degrees takes several times longer.", withPowerUpRange, withFullRange));
						}
						finally
						{
							LogitechWheel.SetRange(path, 200);
							effect.Stop();
							device.Unacquire();
						}
					}
				}
			}
		}

		/// <summary>A constant force on one actuator that runs until stopped, the shape the spring uses.</summary>
		internal static EffectParameters PushParameters(int actuatorObjectId)
		{
			return new EffectParameters
			{
				Flags = EffectFlags.Cartesian | EffectFlags.ObjectIds,
				Duration = unchecked((int)0xFFFFFFFF),
				TriggerButton = unchecked((int)0xFFFFFFFF),
				Gain = 10000,
				Axes = new[] { actuatorObjectId },
				Directions = new[] { 1 },
			};
		}

		/// <summary>Pushes towards the low stop, then times the push from there to the high stop.</summary>
		static long TravelMs(Joystick device, Effect effect, EffectParameters parameters, ConstantForce force)
		{
			Drive(device, effect, parameters, force, -Push, 6000, x => x <= StopZone);
			Drive(device, effect, parameters, force, 0, 500, x => false);
			var watch = Stopwatch.StartNew();
			var arrived = Drive(device, effect, parameters, force, Push, 10000, x => x >= SpringCalibration.AxisMax - StopZone);
			Assert.IsTrue(arrived, "Pushed towards the high end for ten seconds, the wheel did not get there.");
			return watch.ElapsedMilliseconds;
		}

		/// <summary>Applies a force, in percent towards the high end of the axis, until the wheel satisfies the condition or the time is up.</summary>
		internal static bool Drive(Joystick device, Effect effect, EffectParameters parameters, ConstantForce force, int percent, int maxMs, Func<int, bool> done)
		{
			// The same sign as the spring: a DirectInput direction is where the force comes from.
			force.Magnitude = -percent * 100;
			effect.SetParameters(parameters, EffectParameterFlags.TypeSpecificParameters | EffectParameterFlags.Start);
			var watch = Stopwatch.StartNew();
			while (watch.ElapsedMilliseconds < maxMs)
			{
				device.Poll();
				var x = device.GetCurrentState().X;
				if (done(x))
					return true;
				Thread.Sleep(10);
			}
			return false;
		}
	}
}
