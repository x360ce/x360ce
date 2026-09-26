// scratch probe, not part of the suite
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.DirectInput;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace x360ce.Tests
{
	/// <summary>
	/// Does the wheel's damper effect slow the wheel? A constant push swings the wheel from one side
	/// to the other; the crossing time with the damper on, against the time with it off, says whether
	/// the device applies it at all. Hands off the wheel while it runs.
	/// </summary>
	[TestClass]
	public class WheelDamperScratchTest
	{
		[TestMethod, TestCategory("scratch")]
		public void Damper_slows_the_swing()
		{
			var push = int.Parse(Environment.GetEnvironmentVariable("X360CE_WHEEL_PUSH") ?? "3000");
			Ui.OnUiThread(() =>
			{
				using (var form = new System.Windows.Forms.Form())
				using (var di = new DirectInput())
				{
					var instance = di.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly)
						.FirstOrDefault(x => x.ProductName.IndexOf("G27", StringComparison.OrdinalIgnoreCase) >= 0);
					if (instance == null)
						Assert.Inconclusive("No G27 attached.");
					using (var wheel = new Joystick(di, instance.InstanceGuid))
					{
						form.CreateControl();
						var x = wheel.GetObjects(DeviceObjectTypeFlags.All)
							.First(o => o.ObjectType == ObjectGuid.XAxis && o.ObjectId.Flags.HasFlag(DeviceObjectTypeFlags.ForceFeedbackActuator));
						wheel.SetCooperativeLevel(form.Handle, CooperativeLevel.Background | CooperativeLevel.Exclusive);
						wheel.Properties.AutoCenter = false;
						wheel.Acquire();
						Console.WriteLine("{0}, push {1} of 10000", instance.ProductName, push);
						Func<EffectParameters> parameters = () => new EffectParameters
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
						};
						var pushForce = new ConstantForce { Magnitude = push };
						var pushParameters = parameters();
						pushParameters.Parameters = pushForce;
						using (var pushEffect = new Effect(wheel, EffectGuid.ConstantForce, pushParameters))
						{
							foreach (var coefficient in new[] { 0, 2500, 5000, 10000, 0 })
							{
								Effect damper = null;
								if (coefficient > 0)
								{
									var dp = parameters();
									dp.Directions = new[] { 0 };
									dp.Parameters = new ConditionSet
									{
										Conditions = new[]
										{
											new Condition
											{
												Offset = 0,
												PositiveCoefficient = coefficient,
												NegativeCoefficient = coefficient,
												PositiveSaturation = 10000,
												NegativeSaturation = 10000,
												DeadBand = 0,
											}
										}
									};
									damper = new Effect(wheel, EffectGuid.Damper, dp);
									damper.Start(1);
								}
								// To the low stop.
								pushForce.Magnitude = push;
								pushEffect.SetParameters(pushParameters, EffectParameterFlags.TypeSpecificParameters | EffectParameterFlags.Start);
								Thread.Sleep(1500);
								wheel.Poll();
								var from = wheel.GetCurrentState().X;
								// Reverse and time the crossing to the far quarter.
								pushForce.Magnitude = -push;
								var watch = Stopwatch.StartNew();
								pushEffect.SetParameters(pushParameters, EffectParameterFlags.TypeSpecificParameters | EffectParameterFlags.NoRestart);
								var centreAt = -1L;
								var farAt = -1L;
								while (watch.ElapsedMilliseconds < 3000)
								{
									wheel.Poll();
									var p = wheel.GetCurrentState().X;
									if (centreAt < 0 && p > 32767)
										centreAt = watch.ElapsedMilliseconds;
									if (farAt < 0 && p > 32767 + 16000)
										farAt = watch.ElapsedMilliseconds;
									Thread.Sleep(1);
								}
								pushEffect.Stop();
								Console.WriteLine("damper {0,5}: from {1,5}, centre after {2,5} ms, far side after {3,5} ms", coefficient, from, centreAt, farAt);
								if (damper != null)
								{
									damper.Stop();
									damper.Dispose();
								}
								Thread.Sleep(800);
							}
						}
						wheel.Unacquire();
					}
				}
			});
		}
	}
}
