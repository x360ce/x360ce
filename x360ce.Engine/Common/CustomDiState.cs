using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;

namespace x360ce.Engine
{
	/// <summary>
	///  Custom X360CE direct input state class used for configuration.
	/// </summary>
	public class CustomDiState
	{
		public int[] Axis = new int[24];
		public int[] Sliders = new int[8];
		public int[] Povs = new int[4];
		public bool[] Buttons = new bool[128];

		#region Get/Set Axis Array and Existence Mask

		public static int[] GetAxisFromState(JoystickState state)
		{
			return new int[] {
				state.X,
				state.Y,
				state.Z,
				state.RotationX,
				state.RotationY,
				state.RotationZ,
				state.AccelerationX,
				state.AccelerationY,
				state.AccelerationZ,
				state.AngularAccelerationX,
				state.AngularAccelerationY,
				state.AngularAccelerationZ,
				state.ForceX,
				state.ForceY,
				state.ForceZ,
				state.TorqueX,
				state.TorqueY,
				state.TorqueZ,
				state.VelocityX,
				state.VelocityY,
				state.VelocityZ,
				state.AngularVelocityX,
				state.AngularVelocityY,
				state.AngularVelocityZ,
			};
		}

		public static void SetStateFromAxis(JoystickState state, int[] axis)
		{
			state.X = axis[0];
			state.Y = axis[1];
			state.Z = axis[2];
			state.RotationX = axis[3];
			state.RotationY = axis[4];
			state.RotationZ = axis[5];
			state.AccelerationX = axis[6];
			state.AccelerationY = axis[7];
			state.AccelerationZ = axis[8];
			state.AngularAccelerationX = axis[9];
			state.AngularAccelerationY = axis[10];
			state.AngularAccelerationZ = axis[11];
			state.ForceX = axis[12];
			state.ForceY = axis[13];
			state.ForceZ = axis[14];
			state.TorqueX = axis[15];
			state.TorqueY = axis[16];
			state.TorqueZ = axis[17];
			state.VelocityX = axis[18];
			state.VelocityY = axis[19];
			state.VelocityZ = axis[20];
			state.AngularVelocityX = axis[21];
			state.AngularVelocityY = axis[22];
			state.AngularVelocityZ = axis[23];
		}

		/// <summary>
		/// Return bitmasked integer about present axis.
		/// bit 1 = 1 - Axis 1 is present
		/// bit 2 = 0 - Axis 2 is missing
		/// bit 3 = 1 - Axis 3 is present
		/// ...
		/// </summary>
		public static int GetAxisMask(Joystick device)
		{
			int mask = 0;
			if (device.Information.Type == DeviceType.Joystick)
			{
				var items = device.GetObjects(DeviceObjectTypeFlags.All);
				// Must have same order as in axis.
				var list = new List<JoystickOffset>{
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
				foreach (var item in items)
				{
					var offset = (JoystickOffset)item.Offset;
					var index = list.IndexOf(offset);
					if (index > -1)
						mask |= (int)Math.Pow(2, index);
				}
			}
			else if (device.Information.Type == DeviceType.Mouse)
			{
				var items = device.GetObjects(DeviceObjectTypeFlags.All);
				// Must have same order as in Axis[] property.
				var list = new List<MouseOffset>{
					MouseOffset.X,
					MouseOffset.Y,
					MouseOffset.Z,
				};
				foreach (var item in items)
				{
					var offset = (MouseOffset)item.Offset;
					var index = list.IndexOf(offset);
					if (index > -1)
						mask |= (int)Math.Pow(2, index);
				}
			}
			return mask;
		}
		
		#endregion

		#region Get/Set Sliders Array and Existence Mask

		public static int[] GetSlidersFromState(JoystickState state)
		{
			List<int> sl = new List<int>();
			sl.AddRange(state.Sliders);
			sl.AddRange(state.AccelerationSliders);
			sl.AddRange(state.ForceSliders);
			sl.AddRange(state.VelocitySliders);
			return sl.ToArray();
		}

		public static void SetStateFromSliders(JoystickState state, int[] sliders)
		{
			state.Sliders[0] = sliders[0];
			state.Sliders[1] = sliders[1];
			state.AccelerationSliders[0] = sliders[2];
			state.AccelerationSliders[1] = sliders[3];
			state.ForceSliders[0] = sliders[4];
			state.ForceSliders[1] = sliders[5];
			state.VelocitySliders[0] = sliders[6];
			state.VelocitySliders[1] = sliders[7];
		}

		public static int GetSlidersMask(Joystick device)
		{
			int mask = 0;
			if (device.Information.Type == DeviceType.Joystick)
			{
				var items = device.GetObjects(DeviceObjectTypeFlags.All);
				// Must have same order as in Sliders[] property.
				var list = new List<JoystickOffset>{
						JoystickOffset.Sliders0,
						JoystickOffset.Sliders1,
						JoystickOffset.AccelerationSliders0,
						JoystickOffset.AccelerationSliders1,
						JoystickOffset.ForceSliders0,
						JoystickOffset.ForceSliders1,
						JoystickOffset.VelocitySliders0,
						JoystickOffset.VelocitySliders1,
				};
				foreach (var item in items)
				{
					var offset = (JoystickOffset)item.Offset;
					var index = list.IndexOf(offset);
					if (index > -1)
						mask |= (int)Math.Pow(2, index);
				}
			}
			return mask;
		}

		#endregion

		public CustomDiState(JoystickState state)
		{
			// Fill 24 axis (3 x 8).
			Axis = GetAxisFromState(state);
			// Fill 8 sliders (2 x 4).
			Sliders = GetSlidersFromState(state);
			// Fill 4 POVs.
			Povs = state.PointOfViewControllers.ToArray();
			// Fill 128 buttons.
			Buttons = state.Buttons.ToArray();
		}

	}
}
