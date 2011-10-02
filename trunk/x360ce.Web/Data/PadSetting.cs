using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace x360ce.Web.Data
{
	public partial class PadSetting
	{
		public Guid GetCheckSum()
		{
			// Make sure to update checksums in database if you are changing this method.
			var list = new List<string>();
			list.Add(this.AxisToDPadDeadZone);
			list.Add(this.AxisToDPadEnabled);
			list.Add(this.AxisToDPadOffset);
			list.Add(this.ButtonA);
			list.Add(this.ButtonB);
			list.Add(this.ButtonBack);
			list.Add(this.ButtonStart);
			list.Add(this.ButtonX);
			list.Add(this.ButtonY);
			list.Add(this.DPad);
			list.Add(this.DPadDown);
			list.Add(this.DPadLeft);
			list.Add(this.DPadRight);
			list.Add(this.DPadUp);
			list.Add(this.ForceEnable);
			list.Add(this.ForceOverall);
			list.Add(this.ForceSwapMotor);
			list.Add(this.ForceType);
			list.Add(this.GamePadType);
			list.Add(this.LeftMotorPeriod);
			list.Add(this.LeftShoulder);
			list.Add(this.LeftThumbAntiDeadZoneX);
			list.Add(this.LeftThumbAntiDeadZoneY);
			list.Add(this.LeftThumbAxisX);
			list.Add(this.LeftThumbAxisY);
			list.Add(this.LeftThumbButton);
			list.Add(this.LeftThumbDeadZoneX);
			list.Add(this.LeftThumbDeadZoneY);
			list.Add(this.LeftThumbDown);
			list.Add(this.LeftThumbLeft);
			list.Add(this.LeftThumbRight);
			list.Add(this.LeftThumbUp);
			list.Add(this.LeftTrigger);
			list.Add(this.LeftTriggerDeadZone);
			list.Add(this.PassThrough);
			list.Add(this.RightMotorPeriod);
			list.Add(this.RightShoulder);
			list.Add(this.RightThumbAntiDeadZoneX);
			list.Add(this.RightThumbAntiDeadZoneY);
			list.Add(this.RightThumbAxisX);
			list.Add(this.RightThumbAxisY);
			list.Add(this.RightThumbButton);
			list.Add(this.RightThumbDeadZoneX);
			list.Add(this.RightThumbDeadZoneY);
			list.Add(this.RightThumbDown);
			list.Add(this.RightThumbLeft);
			list.Add(this.RightThumbRight);
			list.Add(this.RightThumbUp);
			list.Add(this.RightTrigger);
			list.Add(this.RightTriggerDeadZone);
			var s = string.Join("\r\n", list.ToArray());
			var bytes = System.Text.Encoding.ASCII.GetBytes(s);
			var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
			return new Guid(md5.ComputeHash(bytes));
		}

	}
}