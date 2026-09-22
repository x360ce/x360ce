// scratch probe, not part of the suite
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace x360ce.Tests
{
	/// <summary>
	/// Plays a sine force on a wheel at one frequency after another and reads how far the wheel swings.
	/// </summary>
	/// <remarks>
	/// The swing against frequency is the wheel's response: where it stops following, the imitation of
	/// a pad's motors must stop asking. Hands off the wheel while it runs; nothing else may hold it.
	/// </remarks>
	[TestClass]
	public class WheelSweepScratchTest
	{
		static readonly double[] Frequencies = { 2, 3, 4, 6, 8, 12, 16, 24, 32, 48, 64 };

		[TestMethod, TestCategory("scratch")]
		public void Sweep()
		{
			var magnitude = int.Parse(Environment.GetEnvironmentVariable("X360CE_WHEEL_MAG") ?? "3000");
			var rows = new List<string> { "hz,amplitude,measured_hz,samples" };
			Ui.OnUiThread(() =>
			{
				using (var form = new System.Windows.Forms.Form())
				using (var di = new DirectInput())
				{
					var instance = di.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly)
						.FirstOrDefault(x => x.ProductName.IndexOf("G27", StringComparison.OrdinalIgnoreCase) >= 0);
					if (instance == null)
						Assert.Inconclusive("No G27 attached. Attached: " + string.Join(", ",
							di.GetDevices(DeviceClass.All, DeviceEnumerationFlags.AttachedOnly).Select(d => d.ProductName + " [" + d.Type + "]")));
					using (var wheel = new Joystick(di, instance.InstanceGuid))
					{
						form.CreateControl();
						var x = wheel.GetObjects(DeviceObjectTypeFlags.All)
							.First(o => o.ObjectType == ObjectGuid.XAxis && o.ObjectId.Flags.HasFlag(DeviceObjectTypeFlags.ForceFeedbackActuator));
						wheel.SetCooperativeLevel(form.Handle, CooperativeLevel.Background | CooperativeLevel.Exclusive);
						// The centering spring would fight the sine; the swing must be the effect alone.
						wheel.Properties.AutoCenter = false;
						wheel.Acquire();
						Console.WriteLine("{0}, magnitude {1} of 10000", instance.ProductName, magnitude);
						foreach (var hz in Frequencies)
						{
							var p = new EffectParameters
							{
								Flags = EffectFlags.Cartesian | EffectFlags.ObjectIds,
								Duration = -1,
								SamplePeriod = 0,
								TriggerButton = -1,
								TriggerRepeatInterval = -1,
								Gain = 10000,
								Axes = new[] { (int)x.ObjectId },
								Directions = new[] { 1 },
								Envelope = null,
								Parameters = new PeriodicForce { Magnitude = magnitude, Period = (int)(1000000 / hz) },
							};
							using (var effect = new Effect(wheel, EffectGuid.Sine, p))
							{
								effect.Start(1);
								Thread.Sleep(1500); // settle
								var samples = new List<int>();
								var times = new List<double>();
								var watch = Stopwatch.StartNew();
								while (watch.Elapsed.TotalSeconds < 2.0)
								{
									wheel.Poll();
									samples.Add(wheel.GetCurrentState().X);
									times.Add(watch.Elapsed.TotalSeconds);
									Thread.Sleep(1);
								}
								effect.Stop();
								var amplitude = (samples.Max() - samples.Min()) / 2.0;
								var mean = samples.Average();
								var crossings = 0;
								for (var i = 1; i < samples.Count; i++)
									if ((samples[i - 1] < mean) != (samples[i] < mean))
										crossings++;
								var measured = crossings / 2.0 / (times.Last() - times.First());
								var row = string.Format("{0},{1:0},{2:0.0},{3}", hz, amplitude, measured, samples.Count);
								rows.Add(row);
								Console.WriteLine("{0,5} Hz  swing +/-{1,6:0}  seen {2,5:0.0} Hz  ({3} samples)", hz, amplitude, measured, samples.Count);
							}
							Thread.Sleep(700);
						}
						wheel.Unacquire();
					}
				}
			});
			var folder = Path.Combine(Ui.RepoRoot.FullName, ".tmp", "motors");
			Directory.CreateDirectory(folder);
			var path = Path.Combine(folder, "wheel-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv");
			File.WriteAllLines(path, rows);
			Console.WriteLine("written to " + path);
		}
	}
}
