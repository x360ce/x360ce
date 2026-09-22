// scratch probe, not part of the suite
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
	/// <summary>From the low stop, holds one constant force after another for a second each and reports how far and how fast the wheel went. Hands off.</summary>
	[TestClass]
	public class WheelStartProbeScratchTest
	{
		const int StopZone = SpringCalibration.AxisMax * 15 / 100;

		[TestMethod, TestCategory("scratch")]
		public void Movement_per_force_level()
		{
			using (var manager = new DirectInput())
			{
				var instance = manager.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly)
					.Where(x => x.ForceFeedbackDriverGuid != Guid.Empty)
					.OrderByDescending(x => x.Type == DeviceType.Driving)
					.First();
				using (var form = new Form())
				using (var device = new Joystick(manager, instance.InstanceGuid))
				{
					var actuator = device.GetObjects(DeviceObjectTypeFlags.All)
						.First(x => x.ObjectId.Flags.HasFlag(DeviceObjectTypeFlags.ForceFeedbackActuator));
					device.SetCooperativeLevel(form.Handle, CooperativeLevel.Background | CooperativeLevel.Exclusive);
					device.Properties.AutoCenter = false;
					device.Acquire();
					var parameters = WheelHardwareTest.PushParameters((int)actuator.ObjectId);
					var force = new ConstantForce();
					parameters.Parameters = force;
					using (var effect = new Effect(device, EffectGuid.ConstantForce, parameters))
					{
						try
						{
							Console.WriteLine("level  moved_in_1s  at_100ms  at_300ms  at_600ms  (axis units; centre is 32767 from the stop)");
							for (var level = 16; level <= 40; level += 2)
							{
								WheelHardwareTest.Drive(device, effect, parameters, force, -35, 6000, x => x <= StopZone);
								WheelHardwareTest.Drive(device, effect, parameters, force, 0, 700, x => false);
								device.Poll();
								var from = device.GetCurrentState().X;
								force.Magnitude = -level * 100;
								effect.SetParameters(parameters, EffectParameterFlags.TypeSpecificParameters | EffectParameterFlags.Start);
								var watch = Stopwatch.StartNew();
								int at100 = 0, at300 = 0, at600 = 0, last = from;
								while (watch.ElapsedMilliseconds < 1000)
								{
									device.Poll();
									last = device.GetCurrentState().X;
									var t = watch.ElapsedMilliseconds;
									if (t >= 100 && at100 == 0) at100 = last - from;
									if (t >= 300 && at300 == 0) at300 = last - from;
									if (t >= 600 && at600 == 0) at600 = last - from;
									Thread.Sleep(1);
								}
								effect.Stop();
								Console.WriteLine("{0,5}  {1,11}  {2,8}  {3,8}  {4,8}", level, last - from, at100, at300, at600);
							}
						}
						finally
						{
							effect.Stop();
							device.Unacquire();
						}
					}
				}
			}
		}
	}
}
