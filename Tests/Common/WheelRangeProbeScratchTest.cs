// scratch probe, not part of the suite
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.DirectInput;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>Times the wheel from stop to stop at whatever range it is on now, without setting one. Hands off.</summary>
	[TestClass]
	public class WheelRangeProbeScratchTest
	{
		const int StopZone = SpringCalibration.AxisMax * 15 / 100;

		[TestMethod, TestCategory("scratch")]
		public void Travel_time_at_the_current_range()
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
					Console.WriteLine("InterfacePath: " + device.Properties.InterfacePath);
					var actuator = device.GetObjects(DeviceObjectTypeFlags.All)
						.First(x => x.ObjectId.Flags.HasFlag(DeviceObjectTypeFlags.ForceFeedbackActuator));
					device.SetCooperativeLevel(form.Handle, CooperativeLevel.Background | CooperativeLevel.Exclusive);
					device.Acquire();
					var parameters = WheelHardwareTest.PushParameters((int)actuator.ObjectId);
					var force = new ConstantForce();
					parameters.Parameters = force;
					using (var effect = new Effect(device, EffectGuid.ConstantForce, parameters))
					{
						try
						{
							WheelHardwareTest.Drive(device, effect, parameters, force, -35, 6000, x => x <= StopZone);
							WheelHardwareTest.Drive(device, effect, parameters, force, 0, 500, x => false);
							var watch = Stopwatch.StartNew();
							WheelHardwareTest.Drive(device, effect, parameters, force, 35, 10000, x => x >= SpringCalibration.AxisMax - StopZone);
							Console.WriteLine("Stop to stop now: {0} ms (about 1300 ms is 200 degrees, 5500 ms is 900 degrees)", watch.ElapsedMilliseconds);
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
