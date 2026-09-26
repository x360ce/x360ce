// @under-test: Engine/Common/ForceFeedbackState.cs
// @area: force-feedback   @layer: hardware
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.DirectInput;
using SharpDX.XInput;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using x360ce.Engine;
using x360ce.Engine.Data;
using ForceFeedbackState = x360ce.Engine.ForceFeedbackState;

namespace x360ce.Tests
{
	/// <summary>
	/// Both motors of a two-motor pad keep playing through what a session does to them: speeds that
	/// change and stop, the device let go of and held again, its effects lost and made again, and the
	/// effect type and directions changed.
	/// </summary>
	/// <remarks>
	/// Issue #1633: since 4.20.43 only one motor vibrated. Two causes, each found on a Logitech
	/// Cordless RumblePad 2. Held again, the device forgot what was playing and nothing restarted it,
	/// so a steady rumble stopped. And effects made again took the plain layout, one axis each, even
	/// for the '2' types that put both axes in one effect for the pads that need it.
	/// The pad vibrates while this runs. The program must not be running.
	/// </remarks>
	[TestClass]
	public class TwoMotorPadHardwareTest
	{
		static readonly Vibration Full = new Vibration { LeftMotorSpeed = short.MaxValue, RightMotorSpeed = short.MaxValue };
		static readonly Vibration Off = new Vibration { LeftMotorSpeed = short.MinValue, RightMotorSpeed = short.MinValue };

		static T Field<T>(ForceFeedbackState ff, string name)
		{
			return (T)typeof(ForceFeedbackState).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ff);
		}

		static void Clear(ForceFeedbackState ff, string name)
		{
			typeof(ForceFeedbackState).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(ff, null);
		}

		static void Poll(ForceFeedbackState ff, UserDevice ud, Joystick device, PadSetting ps, Vibration v, int polls)
		{
			for (var i = 0; i < polls; i++)
			{
				ff.SetDeviceForces(ud, device, ps, v);
				Thread.Sleep(1);
			}
		}

		/// <summary>Both effects exist, play, and have the layout the type asks for.</summary>
		static void BothPlay(ForceFeedbackState ff, ForceEffectType type, string when)
		{
			var left = Field<Effect>(ff, "effectL");
			var right = Field<Effect>(ff, "effectR");
			Assert.AreEqual(0, ff.Refused.Length, when + ": the pad refused " + string.Join(" and ", ff.Refused) + ".");
			Assert.AreEqual(EffectStatus.Playing, left.Status, when + ": the left motor is not playing.");
			Assert.AreEqual(EffectStatus.Playing, right.Status, when + ": the right motor is not playing.");
			var axes = type.HasFlag(ForceEffectType._Type2) ? 2 : 1;
			Assert.AreEqual(axes, Field<EffectParameters>(ff, "paramsL").Axes.Length, when + ": " + type + " made with the wrong layout.");
			Assert.AreEqual(axes, Field<EffectParameters>(ff, "paramsR").Axes.Length, when + ": " + type + " made with the wrong layout.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("ui-interactive"), TestCategory("requires-pad")]
		[Description("Both motors keep playing through a session: speed changes, the device held again, effects lost, type and direction changed")]
		public void Both_motors_keep_playing_through_a_session()
		{
			Ui.OnUiThread(() =>
			{
				using (var manager = new DirectInput())
				using (var form = new Form())
				{
					// A pad with two actuators; a wheel has one.
					var name = Environment.GetEnvironmentVariable("X360CE_PAD");
					DeviceInstance instance = null;
					Joystick device = null;
					foreach (var candidate in manager.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly)
						.Where(x => x.ForceFeedbackDriverGuid != Guid.Empty && (name == null || x.ProductName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)))
					{
						var joystick = new Joystick(manager, candidate.InstanceGuid);
						if (joystick.GetObjects(DeviceObjectTypeFlags.All).Count(o => o.ObjectId.Flags.HasFlag(DeviceObjectTypeFlags.ForceFeedbackActuator)) >= 2)
						{
							instance = candidate;
							device = joystick;
							break;
						}
						joystick.Dispose();
					}
					if (device == null)
						Assert.Inconclusive("No pad with two force feedback motors is attached.");
					using (device)
					{
						form.CreateControl();
						Action hold = () =>
						{
							device.Unacquire();
							device.SetCooperativeLevel(form.Handle, CooperativeLevel.Background | CooperativeLevel.Exclusive);
							device.Acquire();
						};
						hold();
						var ud = new UserDevice();
						ud.DeviceObjects = x360ce.App.AppHelper.GetDeviceObjects(device);
						int axisMask, actuatorMask, actuatorCount;
						CustomDiState.GetJoystickAxisMask(ud.DeviceObjects, device, out axisMask, out actuatorMask, out actuatorCount);
						// The reporter's setting: Constant2, the motors pointed opposite ways.
						var ps = new PadSetting
						{
							ForceEnable = "1", ForceType = ((int)ForceEffectType.Constant2).ToString(), ForceOverall = "100",
							LeftMotorStrength = "100", RightMotorStrength = "100",
							LeftMotorDirection = "1", RightMotorDirection = "-1",
						};
						var ff = new ForceFeedbackState();
						try
						{
							Poll(ff, ud, device, ps, Full, 300);
							BothPlay(ff, ForceEffectType.Constant2, instance.ProductName + " at full");
							Poll(ff, ud, device, ps, Off, 200);
							Poll(ff, ud, device, ps, Full, 200);
							BothPlay(ff, ForceEffectType.Constant2, "Stopped and started again");
							hold();
							Poll(ff, ud, device, ps, Full, 300);
							BothPlay(ff, ForceEffectType.Constant2, "After the device was let go and held again, with the speed unchanged");
							foreach (var effect in device.CreatedEffects.ToArray())
								effect.Dispose();
							Clear(ff, "effectL");
							Clear(ff, "effectR");
							Poll(ff, ud, device, ps, Full, 300);
							BothPlay(ff, ForceEffectType.Constant2, "After its effects were lost and made again");
							ps.RightMotorDirection = "1";
							Poll(ff, ud, device, ps, Full, 300);
							BothPlay(ff, ForceEffectType.Constant2, "After the right direction changed");
							ps.ForceType = ((int)ForceEffectType.Constant).ToString();
							Poll(ff, ud, device, ps, Full, 300);
							BothPlay(ff, ForceEffectType.Constant, "After the type changed to Constant");
						}
						finally
						{
							Poll(ff, ud, device, ps, Off, 50);
							ff.StopDeviceForces(device);
							device.Unacquire();
						}
					}
				}
			});
		}
	}
}
