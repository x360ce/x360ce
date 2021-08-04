using SharpDX.DirectInput;
using System.Collections.Generic;

namespace x360ce.Engine
{
	public static class CustomDiHelper
	{

		/// <summary>
		/// Must have same order as in axis.
		/// Important: These values are not the same as on DeviceObjectInstance.Offset.
		/// MouseUpdate.Offset here points to a field in the RawMouseState structure.
		/// </summary>
		public static List<MouseOffset>MouseAxisOffsets = new List<MouseOffset>()
		{
			MouseOffset.X,
			MouseOffset.Y,
			MouseOffset.Z,
		};

		/// <summary>
		/// Must have same order as in axis.
		/// Important: These values are not the same as on DeviceObjectInstance.Offset.
		/// JoystickUpdate.Offset here points to a field in the RawJoystickState structure.
		/// </summary>
		public static List<JoystickOffset> AxisOffsets = new List<JoystickOffset>()
		{
			JoystickOffset.X,
			JoystickOffset.Y,
			JoystickOffset.Z,
			JoystickOffset.RotationX,
			JoystickOffset.RotationY,
			JoystickOffset.RotationZ,
			JoystickOffset.AccelerationX,
			JoystickOffset.AccelerationY,
			JoystickOffset.AccelerationZ,
			JoystickOffset.AngularAccelerationX,
			JoystickOffset.AngularAccelerationY,
			JoystickOffset.AngularAccelerationZ,
			JoystickOffset.ForceX,
			JoystickOffset.ForceY,
			JoystickOffset.ForceZ,
			JoystickOffset.TorqueX,
			JoystickOffset.TorqueY,
			JoystickOffset.TorqueZ,
			JoystickOffset.VelocityX,
			JoystickOffset.VelocityY,
			JoystickOffset.VelocityZ,
			JoystickOffset.AngularVelocityX,
			JoystickOffset.AngularVelocityY,
			JoystickOffset.AngularVelocityZ,
		};

		/// <summary>
		/// Must have same order as in Sliders[] property.
		/// Important: These values are not the same as on DeviceObjectInstance.Offset.
		/// </summary>
		public static List<JoystickOffset> SliderOffsets = new List<JoystickOffset>()
		{
			JoystickOffset.Sliders0,
			JoystickOffset.Sliders1,
			JoystickOffset.AccelerationSliders0,
			JoystickOffset.AccelerationSliders1,
			JoystickOffset.ForceSliders0,
			JoystickOffset.ForceSliders1,
			JoystickOffset.VelocitySliders0,
			JoystickOffset.VelocitySliders1,
		};

		/// <summary>
		/// Must have same order as in POVs[] property.
		/// Important: These values are not the same as on DeviceObjectInstance.Offset.
		/// </summary>
		public static List<JoystickOffset> POVOffsets = new List<JoystickOffset>()
		{
			JoystickOffset.PointOfViewControllers0,
			JoystickOffset.PointOfViewControllers1,
			JoystickOffset.PointOfViewControllers2,
			JoystickOffset.PointOfViewControllers3,
		};

		/// <summary>
		/// Must have same order as in POVs[] property.
		/// Important: These values are not the same as on DeviceObjectInstance.Offset.
		/// </summary>
		public static List<JoystickOffset> ButtonOffsets = new List<JoystickOffset>()
		{
			JoystickOffset.Buttons0,
			JoystickOffset.Buttons1,
			JoystickOffset.Buttons2,
			JoystickOffset.Buttons3,
			JoystickOffset.Buttons4,
			JoystickOffset.Buttons5,
			JoystickOffset.Buttons6,
			JoystickOffset.Buttons7,
			JoystickOffset.Buttons8,
			JoystickOffset.Buttons9,
			JoystickOffset.Buttons10,
			JoystickOffset.Buttons11,
			JoystickOffset.Buttons12,
			JoystickOffset.Buttons13,
			JoystickOffset.Buttons14,
			JoystickOffset.Buttons15,
			JoystickOffset.Buttons16,
			JoystickOffset.Buttons17,
			JoystickOffset.Buttons18,
			JoystickOffset.Buttons19,
			JoystickOffset.Buttons20,
			JoystickOffset.Buttons21,
			JoystickOffset.Buttons22,
			JoystickOffset.Buttons23,
			JoystickOffset.Buttons24,
			JoystickOffset.Buttons25,
			JoystickOffset.Buttons26,
			JoystickOffset.Buttons27,
			JoystickOffset.Buttons28,
			JoystickOffset.Buttons29,
			JoystickOffset.Buttons30,
			JoystickOffset.Buttons31,
			JoystickOffset.Buttons32,
			JoystickOffset.Buttons33,
			JoystickOffset.Buttons34,
			JoystickOffset.Buttons35,
			JoystickOffset.Buttons36,
			JoystickOffset.Buttons37,
			JoystickOffset.Buttons38,
			JoystickOffset.Buttons39,
			JoystickOffset.Buttons40,
			JoystickOffset.Buttons41,
			JoystickOffset.Buttons42,
			JoystickOffset.Buttons43,
			JoystickOffset.Buttons44,
			JoystickOffset.Buttons45,
			JoystickOffset.Buttons46,
			JoystickOffset.Buttons47,
			JoystickOffset.Buttons48,
			JoystickOffset.Buttons49,
			JoystickOffset.Buttons50,
			JoystickOffset.Buttons51,
			JoystickOffset.Buttons52,
			JoystickOffset.Buttons53,
			JoystickOffset.Buttons54,
			JoystickOffset.Buttons55,
			JoystickOffset.Buttons56,
			JoystickOffset.Buttons57,
			JoystickOffset.Buttons58,
			JoystickOffset.Buttons59,
			JoystickOffset.Buttons60,
			JoystickOffset.Buttons61,
			JoystickOffset.Buttons62,
			JoystickOffset.Buttons63,
			JoystickOffset.Buttons64,
			JoystickOffset.Buttons65,
			JoystickOffset.Buttons66,
			JoystickOffset.Buttons67,
			JoystickOffset.Buttons68,
			JoystickOffset.Buttons69,
			JoystickOffset.Buttons70,
			JoystickOffset.Buttons71,
			JoystickOffset.Buttons72,
			JoystickOffset.Buttons73,
			JoystickOffset.Buttons74,
			JoystickOffset.Buttons75,
			JoystickOffset.Buttons76,
			JoystickOffset.Buttons77,
			JoystickOffset.Buttons78,
			JoystickOffset.Buttons79,
			JoystickOffset.Buttons80,
			JoystickOffset.Buttons81,
			JoystickOffset.Buttons82,
			JoystickOffset.Buttons83,
			JoystickOffset.Buttons84,
			JoystickOffset.Buttons85,
			JoystickOffset.Buttons86,
			JoystickOffset.Buttons87,
			JoystickOffset.Buttons88,
			JoystickOffset.Buttons89,
			JoystickOffset.Buttons90,
			JoystickOffset.Buttons91,
			JoystickOffset.Buttons92,
			JoystickOffset.Buttons93,
			JoystickOffset.Buttons94,
			JoystickOffset.Buttons95,
			JoystickOffset.Buttons96,
			JoystickOffset.Buttons97,
			JoystickOffset.Buttons98,
			JoystickOffset.Buttons99,
			JoystickOffset.Buttons100,
			JoystickOffset.Buttons101,
			JoystickOffset.Buttons102,
			JoystickOffset.Buttons103,
			JoystickOffset.Buttons104,
			JoystickOffset.Buttons105,
			JoystickOffset.Buttons106,
			JoystickOffset.Buttons107,
			JoystickOffset.Buttons108,
			JoystickOffset.Buttons109,
			JoystickOffset.Buttons110,
			JoystickOffset.Buttons111,
			JoystickOffset.Buttons112,
			JoystickOffset.Buttons113,
			JoystickOffset.Buttons114,
			JoystickOffset.Buttons115,
			JoystickOffset.Buttons116,
			JoystickOffset.Buttons117,
			JoystickOffset.Buttons118,
			JoystickOffset.Buttons119,
			JoystickOffset.Buttons120,
			JoystickOffset.Buttons121,
			JoystickOffset.Buttons122,
			JoystickOffset.Buttons123,
			JoystickOffset.Buttons124,
			JoystickOffset.Buttons125,
			JoystickOffset.Buttons126,
			JoystickOffset.Buttons127,
		};


	}
}
