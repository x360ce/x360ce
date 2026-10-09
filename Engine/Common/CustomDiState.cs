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

		/// <summary>A state at rest, its arrays made once, for the input thread to fill again and again with <see cref="Load"/>.</summary>
		public CustomDiState() { }

		public CustomDiState(JoystickState state)
		{
			Load(state);
		}

		/// <summary>Fills this state from a DirectInput state, into the arrays it already has.</summary>
		/// <remarks>The input thread fills one on every poll of every device, so this makes nothing.</remarks>
		public void Load(JoystickState state)
		{
			// Fill 24 axis (3 x 8).
			FillAxis(state, Axis);
			// Fill 8 sliders (2 x 4).
			FillSliders(state, Sliders);
			// Fill 4 POVs.
			Array.Copy(state.PointOfViewControllers, Povs, Povs.Length);
			// Fill 128 buttons.
			Array.Copy(state.Buttons, Buttons, Buttons.Length);
		}

		/// <summary>A copy that nothing else writes, for whoever keeps a state while the input thread goes on filling its own.</summary>
		public CustomDiState Clone()
		{
			var copy = new CustomDiState();
			Array.Copy(Axis, copy.Axis, Axis.Length);
			Array.Copy(Sliders, copy.Sliders, Sliders.Length);
			Array.Copy(Povs, copy.Povs, Povs.Length);
			Array.Copy(Buttons, copy.Buttons, Buttons.Length);
			return copy;
		}

		public const int MaxAxis = 24;
		public const int MaxSliders = 8;

		public int[] Axis = new int[MaxAxis];
		public int[] Sliders = new int[MaxSliders];
		public int[] Povs = new int[4];
		public bool[] Buttons = new bool[128];

		#region Get/Set Axis Array and Existence Mask

		/// <summary>Writes a DirectInput state's 24 axes into <paramref name="axis"/>, in the order <see cref="SetStateFromAxis"/> reads them back.</summary>
		public static void FillAxis(JoystickState state, int[] axis)
		{
			axis[0] = state.X;
			axis[1] = state.Y;
			axis[2] = state.Z;
			axis[3] = state.RotationX;
			axis[4] = state.RotationY;
			axis[5] = state.RotationZ;
			axis[6] = state.AccelerationX;
			axis[7] = state.AccelerationY;
			axis[8] = state.AccelerationZ;
			axis[9] = state.AngularAccelerationX;
			axis[10] = state.AngularAccelerationY;
			axis[11] = state.AngularAccelerationZ;
			axis[12] = state.ForceX;
			axis[13] = state.ForceY;
			axis[14] = state.ForceZ;
			axis[15] = state.TorqueX;
			axis[16] = state.TorqueY;
			axis[17] = state.TorqueZ;
			axis[18] = state.VelocityX;
			axis[19] = state.VelocityY;
			axis[20] = state.VelocityZ;
			axis[21] = state.AngularVelocityX;
			axis[22] = state.AngularVelocityY;
			axis[23] = state.AngularVelocityZ;
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
		/// Return bit-masked integer about present axis.
		/// bit 1 = 1 - Axis 1 is present
		/// bit 2 = 0 - Axis 2 is missing
		/// bit 3 = 1 - Axis 3 is present
		/// ...
		/// </summary>
		public static void GetJoystickAxisMask(DeviceObjectItem[] items, Joystick device, out int axisMask, out int actuatorMask, out int actuatorCount, out int relativeMask)
		{
			axisMask = 0;
			actuatorMask = 0;
			actuatorCount = 0;
			relativeMask = 0;
			for (int i = 0; i < CustomDiHelper.AxisOffsets.Count; i++)
			{
				try
				{
					// This function accepts JoystickOffset enumeration values.
					// Important: These values are not the same as on DeviceObjectInstance.Offset.
					var o = device.GetObjectInfoByOffset((int)CustomDiHelper.AxisOffsets[i]);
					if (o != null)
					{
						// Now we can find same object by raw offset (DeviceObjectInstance.Offset).
						var item = items.First(x => x.Offset == o.Offset);
						item.DiIndex = i;
						axisMask |= (int)Math.Pow(2, i);
						// An axis that reports how far it moved rather than where it is: a trackball's or a spinner's.
						if (item.Flags.HasFlag(DeviceObjectTypeFlags.RelativeAxis))
							relativeMask |= 1 << i;
						// Create mask to know which axis have force feedback motor.
						if (item.Flags.HasFlag(DeviceObjectTypeFlags.ForceFeedbackActuator))
						{
							actuatorMask |= (int)Math.Pow(2, i);
							actuatorCount += 1;
						}
					}
				}
				catch (Exception ex)
				{
					_ = ex.Message;
					// Ignore exceptions from GetObjectInfoByOffset(int offset) method.
				}
			}
		}

		public static void GetMouseAxisMask(DeviceObjectItem[] items, Joystick device, out int axisMask, out int relativeMask)
		{
			// Must have same order as in Axis[] property.
			// Important: These values are not the same as on DeviceObjectInstance.Offset.
			var list = new List<MouseOffset>{
					MouseOffset.X,
					MouseOffset.Y,
					MouseOffset.Z,
				};
			axisMask = 0;
			relativeMask = 0;
			for (int i = 0; i < list.Count; i++)
			{
				try
				{
					// This function accepts JoystickOffset enumeration values.
					// Important: These values are not the same as on DeviceObjectInstance.Offset.
					var o = device.GetObjectInfoByOffset((int)list[i]);
					if (o != null)
					{
						// Now we can find same object by raw offset (DeviceObjectInstance.Offset).
						var item = items.First(x => x.Offset == o.Offset);
						item.DiIndex = i;
						axisMask |= (int)Math.Pow(2, i);
						// An axis that reports how far it moved rather than where it is: a mouse's.
						if (item.Flags.HasFlag(DeviceObjectTypeFlags.RelativeAxis))
							relativeMask |= 1 << i;
					}
				}
				catch { }
			}
		}

		#endregion

		#region Get/Set Sliders Array and Existence Mask

		/// <summary>Writes a DirectInput state's 8 sliders into <paramref name="sliders"/>, in the order <see cref="SetStateFromSliders"/> reads them back.</summary>
		public static void FillSliders(JoystickState state, int[] sliders)
		{
			sliders[0] = state.Sliders[0];
			sliders[1] = state.Sliders[1];
			sliders[2] = state.AccelerationSliders[0];
			sliders[3] = state.AccelerationSliders[1];
			sliders[4] = state.ForceSliders[0];
			sliders[5] = state.ForceSliders[1];
			sliders[6] = state.VelocitySliders[0];
			sliders[7] = state.VelocitySliders[1];
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

		public static int GetJoystickSlidersMask(DeviceObjectItem[] items, Joystick device, out int relativeMask)
		{
			int mask = 0;
			relativeMask = 0;
			for (int i = 0; i < CustomDiHelper.SliderOffsets.Count; i++)
			{
				try
				{
					// This function accepts JoystickOffset enumeration values.
					// Important: These values are not the same as on DeviceObjectInstance.Offset.
					var o = device.GetObjectInfoByOffset((int)CustomDiHelper.SliderOffsets[i]);
					if (o != null)
					{
						// Now we can find same object by raw offset (DeviceObjectInstance.Offset).
						var item = items.First(x => x.Offset == o.Offset);
						item.DiIndex = i;
						mask |= (int)Math.Pow(2, i);
						// A slider that reports how far it moved rather than where it is: a spinner's dial.
						if (item.Flags.HasFlag(DeviceObjectTypeFlags.RelativeAxis))
							relativeMask |= 1 << i;
					}
				}
				catch { }
			}
			return mask;
		}

		#endregion

		#region Relative Axes and Sliders

		/// <summary>The relative axes or sliders of a device that the engine believes: those its objects declare, unless the device is read as a gamepad.</summary>
		/// <remarks>
		/// The relative flag is the device's own claim, and a cheap gamepad's driver makes it for sticks that report where
		/// they are. A control worked out as movement stays at one end until it is moved back, which is right for a mouse
		/// and pins a stick. So a joystick, gamepad, wheel, flight stick or first-person controller is read as it reports,
		/// and any other device, such as a mouse, a trackball, a spinner, a screen pointer or a remote, keeps what it
		/// declares. Decided once, when the device's objects are first read.
		/// </remarks>
		/// <param name="capType">The device's DirectInput type, <see cref="x360ce.Engine.Data.UserDevice.CapType"/>.</param>
		/// <param name="declared">The axes or sliders its objects declare relative, one bit each.</param>
		public static int TrustedRelativeMask(int capType, int declared)
		{
			switch ((SharpDX.DirectInput.DeviceType)capType)
			{
				case SharpDX.DirectInput.DeviceType.Joystick:
				case SharpDX.DirectInput.DeviceType.Gamepad:
				case SharpDX.DirectInput.DeviceType.Driving:
				case SharpDX.DirectInput.DeviceType.Flight:
				case SharpDX.DirectInput.DeviceType.FirstPerson:
					return 0;
				default:
					return declared;
			}
		}

		#endregion

	}
}
