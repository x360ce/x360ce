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
	/// which is what a constant force does on its own. And under a light steady push it must
	/// settle where the push and the spring balance, not pulse between pushed out and pushed back.
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
		/// <summary>How much more than the force that just moves the wheel a finger or a small weight pushes with, in percent of the device's force.</summary>
		const int FingerAboveFriction = 10;
		/// <summary>The most a finger pushes with, so it stays under the strengths it is tried against.</summary>
		const int FingerAtMost = 45;
		/// <summary>How long the finger stays on.</summary>
		const int HoldMs = 4000;
		/// <summary>The part of the hold that is judged, after the wheel has had time to find its place.</summary>
		const int JudgedMs = 3000;
		/// <summary>Movement smaller than this between two turns of direction is the encoder, not the wheel.</summary>
		const int Twitch = 60;
		/// <summary>How many times the wheel may turn round under the finger. A pulsing wheel turns round on every pulse.</summary>
		const int ReversalsAllowed = 2;

		/// <summary>What the spring runs against: the device as the program sees it, and a push effect of our own.</summary>
		sealed class Bench
		{
			public Joystick Device;
			public UserDevice Ud;
			public PadSetting Ps;
			public Vibration Silence;
			public ForceFeedbackState Ff;
			public Effect Push;
			public EffectParameters PushParameters;
			public ConstantForce PushForce;
		}

		[TestMethod, TestCategory("devices"), TestCategory("ui-interactive"), TestCategory("requires-wheel")]
		[Description("The Auto run finds a strength, and the spring brings the wheel home from a stop without swinging, at that strength and at full strength")]
		public void The_spring_brings_the_wheel_home_and_holds_it()
		{
			OnTheWheel(b =>
			{
				var found = Auto(b);
				Settles(b, found);
				Settles(b, 100);
			});
		}

		[TestMethod, TestCategory("devices"), TestCategory("ui-interactive"), TestCategory("requires-wheel")]
		[Description("Under a light steady push the wheel settles where the push and the spring balance, instead of pulsing")]
		public void A_light_push_holds_the_wheel_off_centre_without_pulsing()
		{
			OnTheWheel(b =>
			{
				// A push that just moves the wheel against the friction in its gears, as a finger does.
				var finger = Math.Min(Auto(b) + FingerAboveFriction, FingerAtMost);
				Holds(b, finger, 50);
				Holds(b, finger, 100);
			});
		}

		/// <summary>The Auto run, exactly as the button starts it, and the strength it found.</summary>
		static int Auto(Bench b)
		{
			var run = new SpringCalibration();
			var watch = Stopwatch.StartNew();
			while (!run.IsFinished && watch.ElapsedMilliseconds < 60000)
			{
				b.Ff.UpdateSpring(b.Device, Position(b.Device), run, watch.ElapsedMilliseconds);
				Thread.Sleep(1);
			}
			Assert.AreEqual(SpringCalibration.Phase.Done, run.Step, run.Message);
			Console.WriteLine("Auto found {0} % (low side {1} %, high side {2} %)", run.Result, run.LowLevel, run.HighLevel);
			return run.Result;
		}

		/// <summary>Acquires the attached force feedback wheel the way the program does and runs the check on it.</summary>
		static void OnTheWheel(Action<Bench> check)
		{
			using (var manager = new DirectInput())
			{
				// A wheel before anything else with force feedback: a rumble pad plugged in beside
				// the wheel answers to the same question, and its motor cannot be pushed to a stop.
				var instance = manager.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly)
					.Where(x => x.ForceFeedbackDriverGuid != Guid.Empty)
					.OrderByDescending(x => x.Type == SharpDX.DirectInput.DeviceType.Driving)
					.FirstOrDefault();
				if (instance == null)
					Assert.Inconclusive("No force feedback controller is attached.");
				using (var form = new Form())
				using (var device = new Joystick(manager, instance.InstanceGuid))
				{
					device.SetCooperativeLevel(form.Handle, CooperativeLevel.Background | CooperativeLevel.Exclusive);
					// The device's own centring is switched off, as the program does while its spring is on;
					// left on, it holds the wheel against every push made here.
					device.Properties.AutoCenter = false;
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
						var bench = new Bench
						{
							Device = device, Ud = ud, Ps = ps, Silence = silence, Ff = ff,
							Push = push, PushParameters = pushParameters, PushForce = pushForce,
						};
						try
						{
							check(bench);
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
		static void Settles(Bench b, int strength)
		{
			SetStrength(b, 0);
			Assert.IsTrue(WheelHardwareTest.Drive(b.Device, b.Push, b.PushParameters, b.PushForce, -35, 6000, x => x <= StopZone),
				"The wheel could not be pushed to its stop for the run at " + strength + " %.");
			WheelHardwareTest.Drive(b.Device, b.Push, b.PushParameters, b.PushForce, 0, 500, x => false);

			SetStrength(b, strength);
			var positions = Run(b, SettleMs);
			var crossings = 0;
			for (var i = 1; i < positions.Count; i++)
				if ((positions[i - 1] < SpringCalibration.Center) != (positions[i] < SpringCalibration.Center))
					crossings++;
			var perMs = positions.Count / (double)SettleMs;
			var tail = positions.Skip(positions.Count - (int)(RestMs * perMs)).ToArray();
			var farthest = tail.Max(x => Math.Abs(x - SpringCalibration.Center));
			var arrivedAt = positions.FindIndex(x => Math.Abs(x - SpringCalibration.Center) <= ForceFeedbackState.SpringRamp);
			Console.WriteLine("At {0} %: home after about {1} ms, crossed the centre {2} time(s), at most {3} from it over the last second ({4} degrees of 900).",
				strength, arrivedAt < 0 ? -1 : (int)(arrivedAt / perMs), crossings, farthest, Degrees(farthest));
			Assert.IsTrue(crossings <= CrossingsAllowed, string.Format(
				"At {0} % the wheel crossed the centre {1} times in {2} ms: it is swinging from one end to the other instead of coming to rest.",
				strength, crossings, SettleMs));
			// A spring whose force grows with distance rests where that force equals the friction in
			// the gears, which is inside the ramp; a wheel still outside it is not being held at all.
			Assert.IsTrue(farthest <= ForceFeedbackState.SpringRamp, string.Format(
				"At {0} % the wheel was still {1} from the centre in the last second, outside the ramp the spring rises over.",
				strength, farthest));
		}

		/// <summary>Lets the spring settle the wheel, then leans on it with a light steady push and watches what it does.</summary>
		static void Holds(Bench b, int finger, int strength)
		{
			SetStrength(b, strength);
			Run(b, 2000);
			// The finger: a small constant force towards the high end, on top of the spring.
			b.PushForce.Magnitude = -finger * 100;
			b.Push.SetParameters(b.PushParameters, EffectParameterFlags.TypeSpecificParameters | EffectParameterFlags.Start);
			List<int> positions;
			try
			{
				positions = Run(b, HoldMs);
			}
			finally
			{
				b.Push.Stop();
			}
			var perMs = positions.Count / (double)HoldMs;
			var judged = positions.Skip(positions.Count - (int)(JudgedMs * perMs)).ToList();
			// Turns of direction with real movement between them. A wheel pushed out, pushed back
			// and pushed out again turns round every time; one that has settled does not turn at all.
			var reversals = 0;
			var direction = 0;
			var extreme = judged[0];
			foreach (var x in judged)
			{
				if (direction >= 0 && x >= extreme)
				{
					extreme = x;
					direction = 1;
				}
				else if (direction <= 0 && x <= extreme)
				{
					extreme = x;
					direction = -1;
				}
				else if (Math.Abs(x - extreme) > Twitch)
				{
					reversals++;
					direction = -direction;
					extreme = x;
				}
			}
			var span = judged.Max() - judged.Min();
			var offset = judged.Last() - SpringCalibration.Center;
			Console.WriteLine("At {0} % under a {1} % push: turned round {2} time(s) in the last {3} ms, moved within {4} ({5} degrees of 900), ended {6} from the centre ({7} degrees).",
				strength, finger, reversals, JudgedMs, span, Degrees(span), offset, Degrees(offset));
			Assert.IsTrue(reversals <= ReversalsAllowed, string.Format(
				"At {0} % the wheel turned round {1} times under a steady {2} % push: it is pulsing between pushed out and pushed back instead of settling.",
				strength, reversals, finger));
			Assert.IsTrue(span <= ForceFeedbackState.SpringRamp, string.Format(
				"At {0} % the wheel moved over {1} under a steady {2} % push, more than the ramp the spring rises over.",
				strength, span, finger));
		}

		/// <summary>Runs the spring for a while, the way the device thread does, and returns the positions seen.</summary>
		static List<int> Run(Bench b, int ms)
		{
			var positions = new List<int>();
			var watch = Stopwatch.StartNew();
			while (watch.ElapsedMilliseconds < ms)
			{
				var position = Position(b.Device);
				positions.Add(position);
				b.Ff.UpdateSpring(b.Device, position, null, watch.ElapsedMilliseconds);
				Thread.Sleep(1);
			}
			return positions;
		}

		static void SetStrength(Bench b, int strength)
		{
			b.Ps.ForceSpringStrength = strength.ToString();
			b.Ff.SetDeviceForces(b.Ud, b.Device, b.Ps, b.Silence);
			b.Ff.UpdateSpring(b.Device, Position(b.Device), null, 0);
		}

		static int Degrees(int axisUnits)
		{
			return (int)Math.Round(Math.Abs(axisUnits) * 900.0 / SpringCalibration.AxisMax);
		}

		static int Position(Joystick device)
		{
			device.Poll();
			return device.GetCurrentState().X;
		}
	}
}
