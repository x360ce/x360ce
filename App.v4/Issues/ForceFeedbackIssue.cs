using JocysCom.ClassLibrary.Controls.IssuesControl;
using System;
using System.Collections.Generic;
using x360ce.Engine;

namespace x360ce.App.Issues
{
	/// <summary>Force feedback a mapped device does not play the way its settings ask.</summary>
	/// <remarks>
	/// Three cases, all otherwise silent. A device can refuse an effect; the program does not ask it
	/// again, since the answer does not change, and the motor simply stays still. A device's force
	/// feedback can fail on every attempt; the program tries it again twice a second, and the error is
	/// named here until an attempt succeeds. And a pad on the generic "USB Vibration" driver would end
	/// the program if asked for a sawtooth, so the program plays a sine there instead of what the
	/// setting says. In each case the person set one thing and feels another, and this is where they are
	/// told which and why.
	/// </remarks>
	public class ForceFeedbackIssue : IssueItem
	{
		public ForceFeedbackIssue() : base()
		{
			Name = "Force Feedback";
			FixName = "";
		}

		public override void CheckTask()
		{
			var lines = new List<string>();
			foreach (var ud in SettingsManager.UserDevices.ItemsToArraySynchronized())
			{
				var name = string.IsNullOrEmpty(ud.ProductName) ? ud.InstanceName : ud.ProductName;
				// Read once, since the engine sets and clears it.
				var fault = ud.ForceFault;
				var refused = ud.FFState?.Refused;
				// While it fails, an effect lost to the failure reads as refused as well, so only the failure is
				// said, and only while the device is connected, which is when it is tried again.
				if (fault != 0 && ud.IsOnline)
					lines.Add(string.Format(
						"Force feedback on {0} failed (error 0x{1:X8}), so it is tried again twice a second.",
						name, fault));
				else if (fault == 0 && refused != null && refused.Length > 0)
					lines.Add(string.Format(
						"{0} refused {1}, so it stays still. On a pad with two motors, try the '2' form of the effect type, such as Constant2.",
						name, string.Join(" and ", refused)));
				if (ForceFeedbackDriver.FailsOnSawtooth(ud.ForceFeedbackDriver) && AsksForSawtooth(ud))
					lines.Add(string.Format(
						"{0} uses the \"USB Vibration\" driver, which closes the program when asked for a sawtooth, so a sine is played instead.",
						name));
			}
			if (lines.Count == 0)
			{
				SetSeverity(IssueSeverity.None);
				return;
			}
			SetSeverity(IssueSeverity.Moderate, 0, string.Join(Environment.NewLine, lines.ToArray()));
		}

		static bool AsksForSawtooth(Engine.Data.UserDevice ud)
		{
			// The settings the engine plays the device's effects with, in the current game.
			DInput.DeviceForce route;
			var ps = DInput.DeviceRouting.Current.TryGetForce(ud.InstanceGuid, out route) ? route.PadSetting : null;
			if (ps == null || ps.ForceEnable != "1")
				return false;
			int type;
			return int.TryParse(ps.ForceType, out type) && ((ForceEffectType)type).HasFlag(ForceEffectType.PeriodicSawtooth);
		}
	}
}
