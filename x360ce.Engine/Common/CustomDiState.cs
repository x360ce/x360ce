using SharpDX.DirectInput;
using System;
using System.Collections.Generic;

namespace x360ce.Engine
{
	/// <summary>
	///  Custom X360CE direct input state class used for configuration.
	/// </summary>
	public partial class CustomDiState
	{

		public CustomDiState()
		{
		}

		public CustomDiState(MouseState state)
		{
			Copy(state.Buttons, Buttons);
			CopyAxis(state, Axis);
		}

		public CustomDiState(KeyboardState state)
		{
			foreach (var key in state.PressedKeys)
				Buttons[(int)key] = true;
		}

		public CustomDiState(JoystickState state)
		{
			CopyAxis(state, Axis);
			CopySliders(state, Sliders);
			Copy(state.PointOfViewControllers, POVs);
			Copy(state.Buttons, Buttons);
		}

		public const int MaxAxis = 24; // (3 x 8)
		public const int MaxSliders = 8; // (2 x 4).
		public const int MaxPOVs = 4;
		public const int MaxButtons = 256;

		public int[] Axis = new int[MaxAxis];
		public int[] Sliders = new int[MaxSliders];
		public int[] POVs = new int[MaxPOVs];
		public bool[] Buttons = new bool[MaxButtons];

		static void Copy<T>(T[] source, T[] destination)
		{
			Array.Clear(destination, 0, destination.Length);
			var length = Math.Min(source.Length, destination.Length);
			Array.Copy(source, destination, length);
		}

		#region Mouse

		static void CopyAxis(MouseState state, int[] axis)
		{
			axis[0] = state.X;
			axis[1] = state.Y;
			axis[2] = state.Z;
		}

		public static void CopyAxis(int[] axis, MouseState state)
		{
			state.X = axis[0];
			state.Y = axis[1];
			state.Z = axis[2];
		}

		#endregion

		#region Joystick

		public static void CopyAxis(JoystickState state, int[] axis)
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

		public static void CopyAxis(int[] axis, JoystickState state)
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

		public static void CopySliders(JoystickState state, int[] sliders)
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

		public static void CopySliders(int[] sliders, JoystickState state)
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

		#endregion

		/// <summary>
		/// Compare states and return differences
		/// </summary>
		public static CustomDiUpdate[] CompareTo(CustomDiState oldState, CustomDiState newState)
		{
			if (oldState == null)
				throw new ArgumentNullException(nameof(oldState));
			if (newState == null)
				throw new ArgumentNullException(nameof(newState));
			var list = new List<CustomDiUpdate>();
			list.AddRange(CompareRange(oldState.Axis, newState.Axis, MapType.Axis));
			list.AddRange(CompareRange(oldState.Sliders, newState.Sliders, MapType.Slider));
			list.AddRange(CompareValue(oldState.POVs, newState.POVs, MapType.POV));
			list.AddRange(CompareValue(oldState.Buttons, newState.Buttons, MapType.Button));
			// Return results.
			return list.ToArray();
		}

		static CustomDiUpdate[] CompareValue(bool[] oldValues, bool[] newValues, MapType mapType)
		{
			var list = new List<CustomDiUpdate>();
			for (int i = 0; i < oldValues.Length; i++)
				// If differ then...
				if (newValues[i] != oldValues[i])
					list.Add(new CustomDiUpdate(mapType, i, newValues[i] ? 1 : 0));
			// Return results.
			return list.ToArray();
		}

		static CustomDiUpdate[] CompareValue(int[] oldValues, int[] newValues, MapType mapType)
		{
			var list = new List<CustomDiUpdate>();
			for (int i = 0; i < oldValues.Length; i++)
				// If differ then...
				if (newValues[i] != oldValues[i])
					list.Add(new CustomDiUpdate(mapType, i, newValues[i]));
			// Return results.
			return list.ToArray();
		}

		static CustomDiUpdate[] CompareRange(int[] oldValues, int[] newValues, MapType mapType)
		{
			var list = new List<CustomDiUpdate>();
			for (int i = 0; i < oldValues.Length; i++)
				// If differ by more than 20% then...
				if (Math.Abs(newValues[i] - oldValues[i]) > (ushort.MaxValue * 30 / 100))
					list.Add(new CustomDiUpdate(mapType, i, newValues[i]));
			// Return results.
			return list.ToArray();
		}

	}
}
