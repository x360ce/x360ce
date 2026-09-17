// scratch probe, not part of the suite
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace x360ce.Tests
{
	[TestClass]
	public class WheelProbeScratchTest
	{
		static Joystick Wheel(DirectInput di)
		{
			var instance = di.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly)
				.FirstOrDefault(x => x.ProductName.Contains("G27"));
			if (instance == null)
				Assert.Inconclusive("No G27 attached.");
			return new Joystick(di, instance.InstanceGuid);
		}

		/// <summary>Pushes the wheel off centre for a moment, so a spring has something to bring home.</summary>
		[TestMethod, TestCategory("scratch")]
		public void Push()
		{
			Ui.OnUiThread(() =>
			{
			using (var form = new System.Windows.Forms.Form())
			using (var di = new DirectInput())
			using (var wheel = Wheel(di))
			{
				form.CreateControl();
				wheel.SetCooperativeLevel(form.Handle, CooperativeLevel.Background | CooperativeLevel.Exclusive);
				wheel.Properties.AutoCenter = false;
				wheel.Acquire();
				var axis = wheel.GetObjects().First(o => o.ObjectType == ObjectGuid.XAxis);
				var p = new EffectParameters
				{
					Flags = EffectFlags.Cartesian | EffectFlags.ObjectIds,
					Duration = 700000,
					SamplePeriod = 0,
					Gain = 10000,
					TriggerButton = unchecked((int)0xFFFFFFFF),
					TriggerRepeatInterval = unchecked((int)0xFFFFFFFF),
					StartDelay = 0,
					Axes = new[] { (int)axis.ObjectId },
					Directions = new[] { 1 },
					Envelope = null,
					Parameters = new ConstantForce { Magnitude = 6000 },
				};
				using (var effect = new Effect(wheel, EffectGuid.ConstantForce, p))
				{
					effect.Start(1);
					Thread.Sleep(800);
					effect.Stop();
				}
				wheel.Unacquire();
				Console.WriteLine("pushed");
			}
			});
		}

		/// <summary>Samples the steering axis for a while and reports how it moved.</summary>
		[TestMethod, TestCategory("scratch")]
		public void Sample()
		{
			var seconds = 15;
			var lines = new StringBuilder();
			using (var di = new DirectInput())
			using (var wheel = Wheel(di))
			{
				wheel.SetCooperativeLevel(IntPtr.Zero, CooperativeLevel.Background | CooperativeLevel.NonExclusive);
				wheel.Acquire();
				var watch = Stopwatch.StartNew();
				var samples = new List<int>();
				var lastPrinted = -100L;
				while (watch.ElapsedMilliseconds < seconds * 1000)
				{
					wheel.Poll();
					var x = wheel.GetCurrentState().X;
					samples.Add(x);
					if (watch.ElapsedMilliseconds - lastPrinted >= 250)
					{
						lines.AppendFormat("{0,6} ms  x={1,6}  ({2,6})", watch.ElapsedMilliseconds, x, x - 32767).AppendLine();
						lastPrinted = watch.ElapsedMilliseconds;
					}
					Thread.Sleep(10);
				}
				wheel.Unacquire();
				// Crossings of the centre with real travel on both sides, which is what a swing is.
				var crossings = 0;
				var side = 0;
				foreach (var x in samples)
				{
					var s = x > 32767 + 1500 ? 1 : x < 32767 - 1500 ? -1 : 0;
					if (s != 0 && side != 0 && s != side)
						crossings++;
					if (s != 0)
						side = s;
				}
				lines.AppendFormat("crossings of the centre with more than 1500 travel on both sides: {0}; min {1} max {2} last {3}",
					crossings, samples.Min(), samples.Max(), samples.Last()).AppendLine();
			}
			Assert.Fail(lines.ToString());
		}
	}
}
