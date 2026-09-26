// scratch probe, not part of the suite
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.DirectInput;
using System;
using System.Linq;
using System.Threading;

namespace x360ce.Tests
{
	/// <summary>
	/// Asks a two-motor gamepad what it accepts, the way ForceFeedbackState asks, and prints every answer.
	/// </summary>
	[TestClass]
	public class RumblePadProbeScratchTest
	{
		const int Infinite = -1;
		const int NoTrigger = -1;

		static Joystick Pad(DirectInput di, string namePart)
		{
			var instance = di.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly)
				.FirstOrDefault(x => x.ProductName.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0);
			if (instance == null)
				Assert.Inconclusive("No '" + namePart + "' attached.");
			Console.WriteLine("Device: {0}  instance {1}", instance.ProductName, instance.InstanceGuid);
			return new Joystick(di, instance.InstanceGuid);
		}

		static EffectParameters Parameters(int[] axes, int[] directions, int magnitude)
		{
			return new EffectParameters
			{
				Flags = EffectFlags.Cartesian | EffectFlags.ObjectIds,
				StartDelay = 0,
				Duration = Infinite,
				SamplePeriod = 0,
				TriggerButton = NoTrigger,
				TriggerRepeatInterval = Infinite,
				Gain = 10000,
				Axes = axes,
				Directions = directions,
				Envelope = null,
				Parameters = new ConstantForce { Magnitude = magnitude },
			};
		}

		static void Try(string what, Action action)
		{
			try
			{
				action();
				Console.WriteLine("  OK    {0}", what);
			}
			catch (SharpDX.SharpDXException ex)
			{
				Console.WriteLine("  FAIL  {0}: 0x{1:X8} {2}", what, ex.ResultCode.Code, ex.Descriptor?.ApiCode ?? ex.Message);
			}
			catch (Exception ex)
			{
				Console.WriteLine("  FAIL  {0}: {1}: {2}", what, ex.GetType().Name, ex.Message);
			}
		}

		/// <summary>Three bursts of three seconds, three seconds apart: constant with direction 0, sine with direction 0, constant with direction 1.</summary>
		[TestMethod, TestCategory("scratch")]
		public void DirectionZero()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = new System.Windows.Forms.Form())
				using (var di = new DirectInput())
				using (var pad = Pad(di, "RumblePad"))
				{
					form.CreateControl();
					var actuators = pad.GetObjects(DeviceObjectTypeFlags.All).Where(o => o.ObjectId.Flags.HasFlag(DeviceObjectTypeFlags.ForceFeedbackActuator)).ToList();
					var x = actuators.First(a => a.ObjectType == ObjectGuid.XAxis);
					var y = actuators.First(a => a.ObjectType == ObjectGuid.YAxis);
					pad.SetCooperativeLevel(form.Handle, CooperativeLevel.Background | CooperativeLevel.Exclusive);
					pad.Acquire();
					Action<string, Guid, int, Func<object>> burst = (label, guid, direction, typeSpecific) =>
					{
						Console.WriteLine("{0:HH:mm:ss.f} {1}", DateTime.Now, label);
						Effect l = null, r = null;
						var pl = Parameters(new[] { (int)x.ObjectId }, new[] { direction }, 8000); pl.Parameters = (TypeSpecificParameters)typeSpecific();
						var pr = Parameters(new[] { (int)y.ObjectId }, new[] { direction }, 8000); pr.Parameters = (TypeSpecificParameters)typeSpecific();
						Try("  create L", () => l = new Effect(pad, guid, pl));
						Try("  create R", () => r = new Effect(pad, guid, pr));
						if (l != null) Try("  start L", () => l.Start(1));
						if (r != null) Try("  start R", () => r.Start(1));
						Thread.Sleep(3000);
						if (l != null) Try("  stop L", () => { l.Stop(); l.Dispose(); });
						if (r != null) Try("  stop R", () => { r.Stop(); r.Dispose(); });
						Thread.Sleep(3000);
					};
					burst("1. constant, direction 0", EffectGuid.ConstantForce, 0, () => new ConstantForce { Magnitude = 8000 });
					burst("2. sine, direction 0, period 60 ms", EffectGuid.Sine, 0, () => new PeriodicForce { Magnitude = 8000, Period = 60000 });
					burst("3. constant, direction 1", EffectGuid.ConstantForce, 1, () => new ConstantForce { Magnitude = 8000 });
					pad.Unacquire();
				}
			});
		}

		[TestMethod, TestCategory("scratch")]
		public void Probe()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = new System.Windows.Forms.Form())
				using (var di = new DirectInput())
				using (var pad = Pad(di, "RumblePad"))
				{
					form.CreateControl();
					var caps = pad.Capabilities;
					Console.WriteLine("Flags: {0}; axes {1} buttons {2}", caps.Flags, caps.AxeCount, caps.ButtonCount);
					Console.WriteLine("Effects the device lists: {0}", string.Join(", ", pad.GetEffects().Select(e => e.Name + " [" + e.Guid + "]")));
					var actuators = pad.GetObjects(DeviceObjectTypeFlags.All).Where(o => o.ObjectId.Flags.HasFlag(DeviceObjectTypeFlags.ForceFeedbackActuator)).ToList();
					Console.WriteLine("Actuators ({0}):", actuators.Count);
					foreach (var a in actuators)
						Console.WriteLine("  {0}  type {1}  objectId 0x{2:X8}  offset {3}  flags {4}", a.Name, a.ObjectType == ObjectGuid.XAxis ? "X" : a.ObjectType == ObjectGuid.YAxis ? "Y" : a.ObjectType.ToString(), (int)a.ObjectId, a.Offset, a.ObjectId.Flags);
					if (actuators.Count == 0)
						Assert.Inconclusive("No actuators reported.");

					pad.SetCooperativeLevel(form.Handle, CooperativeLevel.Background | CooperativeLevel.Exclusive);
					Try("Acquire exclusive/background", () => pad.Acquire());
					Try("Reset FF", () => pad.SendForceFeedbackCommand(ForceFeedbackCommand.Reset));
					Try("Actuators on", () => pad.SendForceFeedbackCommand(ForceFeedbackCommand.SetActuatorsOn));
					Console.WriteLine("  FF state now: {0}", pad.GetForceFeedbackState());

					var x = actuators.FirstOrDefault(a => a.ObjectType == ObjectGuid.XAxis) ?? actuators[0];
					var y = actuators.FirstOrDefault(a => a.ObjectType == ObjectGuid.YAxis) ?? (actuators.Count > 1 ? actuators[1] : null);

					// A: the program's two-actuator path, one axis per effect.
					Console.WriteLine("A. one axis per effect (the program's default for two actuators)");
					Effect ea = null, eb = null;
					Try("  create L on X", () => ea = new Effect(pad, EffectGuid.ConstantForce, Parameters(new[] { (int)x.ObjectId }, new[] { 1 }, 5000)));
					if (y != null)
						Try("  create R on Y", () => eb = new Effect(pad, EffectGuid.ConstantForce, Parameters(new[] { (int)y.ObjectId }, new[] { 1 }, 5000)));
					if (ea != null) Try("  start L", () => ea.Start(1));
					if (eb != null) Try("  start R", () => eb.Start(1));
					Thread.Sleep(1200);
					Console.WriteLine("  FF state while playing: {0}", pad.GetForceFeedbackState());
					if (ea != null) Try("  stop/dispose L", () => { ea.Stop(); ea.Dispose(); });
					if (eb != null) Try("  stop/dispose R", () => { eb.Stop(); eb.Dispose(); });

					// B: the program's Type2 path, both axes per effect, second direction zero.
					if (y != null)
					{
						Console.WriteLine("B. both axes per effect (Type2)");
						Effect ec = null, ed = null;
						Try("  create L on X,Y", () => ec = new Effect(pad, EffectGuid.ConstantForce, Parameters(new[] { (int)x.ObjectId, (int)y.ObjectId }, new[] { 1, 0 }, 5000)));
						Try("  create R on Y,X", () => ed = new Effect(pad, EffectGuid.ConstantForce, Parameters(new[] { (int)y.ObjectId, (int)x.ObjectId }, new[] { 1, 0 }, 5000)));
						if (ec != null) Try("  start L", () => ec.Start(1));
						if (ed != null) Try("  start R", () => ed.Start(1));
						Thread.Sleep(1200);
						if (ec != null) Try("  stop/dispose L", () => { ec.Stop(); ec.Dispose(); });
						if (ed != null) Try("  stop/dispose R", () => { ed.Stop(); ed.Dispose(); });

						// C: one effect on both axes, both directions.
						Console.WriteLine("C. one effect, both axes, both directions");
						Effect ee = null;
						Try("  create", () => ee = new Effect(pad, EffectGuid.ConstantForce, Parameters(new[] { (int)x.ObjectId, (int)y.ObjectId }, new[] { 1, 1 }, 5000)));
						if (ee != null) { Try("  start", () => ee.Start(1)); Thread.Sleep(1200); Try("  stop/dispose", () => { ee.Stop(); ee.Dispose(); }); }
					}

					// D: SetParameters with Start, as SetParamaters does after creation.
					Console.WriteLine("D. create then SetParameters(TypeSpecific|Gain|Direction|Start)");
					Effect ef = null;
					Try("  create L on X", () => ef = new Effect(pad, EffectGuid.ConstantForce, Parameters(new[] { (int)x.ObjectId }, new[] { 1 }, 0)));
					if (ef != null)
					{
						var p = Parameters(new[] { (int)x.ObjectId }, new[] { 1 }, 8000);
						Try("  SetParameters+Start", () => ef.SetParameters(p, EffectParameterFlags.TypeSpecificParameters | EffectParameterFlags.Gain | EffectParameterFlags.Direction | EffectParameterFlags.Start));
						Thread.Sleep(1200);
						Console.WriteLine("  status: {0}", ef.Status);
						Try("  stop/dispose", () => { ef.Stop(); ef.Dispose(); });
					}
					Try("Unacquire", () => pad.Unacquire());
				}
			});
		}
	}
}
