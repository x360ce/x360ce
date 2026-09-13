// @under-test: Engine/Common/ForceFeedbackState.cs, Engine/Common/SpringCalibration.cs
// @area: devices   @layer: hardware
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.DirectInput;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using x360ce.Engine;
using x360ce.Engine.Data;
using ForceFeedbackState = x360ce.Engine.ForceFeedbackState;

namespace x360ce.Tests
{
	/// <summary>
	/// Runs the centering spring itself on an attached wheel, through the same state the program
	/// uses: the Auto run first, then the spring at the strength it found and at full strength,
	/// each from a stop. The wheel must come home and stay, not swing from one end to the other,
	/// which is what a constant force does on its own.
	/// </summary>
	/// <remarks>
	/// The wheel is pushed to its stops, so hands stay off it. Needs the wheel free: the program
	/// must not be running.
	/// </remarks>
	[TestClass]
	public class CenteringSpringHardwareTest
	{
		const int StopZone = SpringCalibration.AxisMax * 15 / 100;
		/// <summary>How long the spring is given to bring the wheel home and hold it.</summary>
		const int SettleMs = 4000;
		/// <summary>How long the wheel must have stayed near the centre at the end to count as at rest.</summary>
		const int RestMs = 1000;
		/// <summary>How many times the wheel may pass the centre on its way to rest. A swing from end to end passes it every time.</summary>
		const int CrossingsAllowed = 3;

		[TestMethod, TestCategory("devices"), TestCategory("ui-interactive"), TestCategory("requires-wheel")]
		[Description("The Auto run finds a strength, and the spring brings the wheel home from a stop without swinging, at that strength and at full strength")]
		public void The_spring_brings_the_wheel_home_and_holds_it()
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
					device.SetCooperativeLevel(form.Handle, CooperativeLevel.Background | CooperativeLevel.Exclusive);
					device.Acquire();
					// The device as the program sees it: its objects, with the axis index each one moves.
					var ud = new UserDevice();
					ud.DeviceObjects = x360ce.App.AppHelper.GetDeviceObjects(device);
					int axisMask, actuatorMask, actuatorCount;
					CustomDiState.GetJoystickAxisMask(ud.DeviceObjects, device, out axisMask, out actuatorMask, out actuatorCount);
					if (actuatorCount == 0)
						Assert.Inconclusive(instance.ProductName + " has no actuator on an axis.");
					ud.DevVendorId = device.Properties.VendorId;
					ud.DevProductId = device.Properties.ProductId;
					var ps = new PadSetting { ForceEnable = "1", ForceType = "0", ForceSpringEnable = "1", ForceSpringStrength = "0" };
					// No rumble, said the way the program says it.
					var silence = new Vibration { LeftMotorSpeed = short.MinValue, RightMotorSpeed = short.MinValue };
					var ff = new ForceFeedbackState();
					Assert.IsTrue(ff.SetDeviceForces(ud, device, ps, silence), "The device offered no actuator.");
					Assert.IsTrue(ff.SpringAxisIndex >= 0, "The spring found no axis to read.");
					var actuator = ud.DeviceObjects.First(x => x.Flags.HasFlag(DeviceObjectTypeFlags.ForceFeedbackActuator));
					var pushParameters = WheelHardwareTest.PushParameters(actuator.ObjectId);
					var pushForce = new ConstantForce();
					pushParameters.Parameters = pushForce;
					using (var push = new Effect(device, EffectGuid.ConstantForce, pushParameters))
					{
						try
						{
							// The Auto run, exactly as the button starts it.
							var run = new SpringCalibration();
							var watch = Stopwatch.StartNew();
							while (!run.IsFinished && watch.ElapsedMilliseconds < 60000)
							{
								ff.UpdateSpring(device, Position(device), run, watch.ElapsedMilliseconds);
								Thread.Sleep(1);
							}
							Assert.AreEqual(SpringCalibration.Phase.Done, run.Step, run.Message);
							Console.WriteLine("Auto found {0} % (low side {1} %, high side {2} %)", run.Result, run.LowLevel, run.HighLevel);

							Settles(ff, ud, device, ps, silence, push, pushParameters, pushForce, run.Result);
							Settles(ff, ud, device, ps, silence, push, pushParameters, pushForce, 100);
						}
						finally
						{
							push.Stop();
							ff.StopDeviceForces(device);
							device.Unacquire();
						}
					}
				}
			}
		}

		/// <summary>Puts the wheel at a stop with the spring off, turns the spring on at the strength, and watches it come home.</summary>
		static void Settles(ForceFeedbackState ff, UserDevice ud, Joystick device, PadSetting ps, Vibration silence,
			Effect push, EffectParameters pushParameters, ConstantForce pushForce, int strength)
		{
			ps.ForceSpringStrength = "0";
			ff.SetDeviceForces(ud, device, ps, silence);
			ff.UpdateSpring(device, Position(device), null, 0);
			Assert.IsTrue(WheelHardwareTest.Drive(device, push, pushParameters, pushForce, -35, 6000, x => x <= StopZone),
				"The wheel could not be pushed to its stop for the run at " + strength + " %.");
			WheelHardwareTest.Drive(device, push, pushParameters, pushForce, 0, 500, x => false);

			ps.ForceSpringStrength = strength.ToString();
			ff.SetDeviceForces(ud, device, ps, silence);
			var positions = new List<int>();
			var watch = Stopwatch.StartNew();
			while (watch.ElapsedMilliseconds < SettleMs)
			{
				var position = Position(device);
				positions.Add(position);
				ff.UpdateSpring(device, position, null, watch.ElapsedMilliseconds);
				Thread.Sleep(1);
			}
			var crossings = 0;
			for (var i = 1; i < positions.Count; i++)
				if ((positions[i - 1] < SpringCalibration.Center) != (positions[i] < SpringCalibration.Center))
					crossings++;
			var perMs = positions.Count / (double)SettleMs;
			var tail = positions.Skip(positions.Count - (int)(RestMs * perMs)).ToArray();
			var farthest = tail.Max(x => Math.Abs(x - SpringCalibration.Center));
			var arrivedAt = positions.FindIndex(x => Math.Abs(x - SpringCalibration.Center) <= ForceFeedbackState.SpringDeadBand * 2);
			Console.WriteLine("At {0} %: home after about {1} ms, crossed the centre {2} time(s), at most {3} from it over the last second.",
				strength, arrivedAt < 0 ? -1 : (int)(arrivedAt / perMs), crossings, farthest);
			Assert.IsTrue(crossings <= CrossingsAllowed, string.Format(
				"At {0} % the wheel crossed the centre {1} times in {2} ms: it is swinging from one end to the other instead of coming to rest.",
				strength, crossings, SettleMs));
			Assert.IsTrue(farthest <= ForceFeedbackState.SpringDeadBand * 2, string.Format(
				"At {0} % the wheel was still {1} from the centre in the last second, which is outside the band the spring lets go in.",
				strength, farthest));
		}

		static int Position(Joystick device)
		{
			device.Poll();
			return device.GetCurrentState().X;
		}
	}
}
