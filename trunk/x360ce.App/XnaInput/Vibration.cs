using System;
using System.Runtime.InteropServices;

namespace x360ce.App.XnaInput
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct Vibration
	{
		public ushort LeftMotorSpeed;
		public ushort RightMotorSpeed;

		public Vibration(ushort left, ushort right)
		{
			this.LeftMotorSpeed = left;
			this.RightMotorSpeed = right;
		}
	}
}

