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
		public int[] Axis = new int[0];
		public int[] Sliders = new int[0];
		public int[] Povs = new int[0];
		public bool[] Buttons = new bool[0];

		// Return bitmasked integer about present axis.
		// bit 1 = 1 - Axis 1 is present
		// bit 2 = 0 - Axis 2 is missing
		// bit 3 = 1 - Axis 3 is present
		// ...
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

		public CustomDiState(JoystickState state)
		{
			// Fill 24 axis.
			Axis = new int[] {
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
			// Fill 8 sliders.
			List<int> sl = new List<int>();
			sl.AddRange(state.Sliders);
			sl.AddRange(state.AccelerationSliders);
			sl.AddRange(state.ForceSliders);
			sl.AddRange(state.VelocitySliders);
			Sliders = sl.ToArray();
			// Fill POVs.
			Povs = state.PointOfViewControllers.ToArray();
			// Fill buttons.
			Buttons = state.Buttons.ToArray();
		}

		/// <summary>
		/// Compare to another state.
		/// </summary>
		/// <param name="state"></param>
		/// <returns></returns>
		public string[] CompareTo(CustomDiState state)
		{
			var list = new List<string>();
			list.AddRange(CompareAxisAndSliders(Axis, state.Axis, "Axis"));
			list.AddRange(CompareAxisAndSliders(Sliders, state.Sliders, "Slider"));
			// Compare Buttons
			if (Buttons.Length == state.Buttons.Length)
			{
				for (int i = 0; i < Buttons.Length; i++)
				{
					if (Buttons[i] != state.Buttons[i])
					{
						list.Add(string.Format("Button {0}", i + 1));
					}
				}
			};
			// Compare POVs.
			if (Povs.Length == state.Povs.Length)
			{
				for (int i = 0; i < Povs.Length; i++)
				{
					if (Povs[i] != state.Povs[i])
					{
						//list.Add(string.Format("DPad {0}", i + 1));
						var v = state.Povs[0];
						if ((DPadEnum)v == DPadEnum.Up) list.Add(string.Format("DPad {0} {1}", i + 1, DPadEnum.Up.ToString()));
						if ((DPadEnum)v == DPadEnum.Right) list.Add(string.Format("DPad {0} {1}", i + 1, DPadEnum.Right.ToString()));
						if ((DPadEnum)v == DPadEnum.Down) list.Add(string.Format("DPad {0} {1}", i + 1, DPadEnum.Down.ToString()));
						if ((DPadEnum)v == DPadEnum.Left) list.Add(string.Format("DPad {0} {1}", i + 1, DPadEnum.Left.ToString()));
					}
				}
			};
			return list.ToArray();
		}

		string[] CompareAxisAndSliders(int[] oldValues, int[] newValues, string name)
		{
			var list = new List<string>();
			if (oldValues.Length != newValues.Length) return list.ToArray();
			for (int i = 0; i < oldValues.Length; i++)
			{
				// Get difference between states (this object represents old value).
				var oldValue = oldValues[i];
				var diff = newValues[i] - oldValue;
				var prefix = "";
				// If moved more than 10%.
				if (Math.Abs(diff) > (ushort.MaxValue / 10))
				{
					// If value is negative then add "I" prefix.
					if (diff < 0) prefix += "I";
					// if starting point is located in the middle then...
					if ((oldValue > (ushort.MaxValue / 4)) && oldValue < (ushort.MaxValue * 3 / 4))
					{
						// Add half prefix.
						prefix += "H";
					}
					list.Add(string.Format("{0}{1} {2}", prefix, name, i + 1));
				}
			}
			return list.ToArray();
		}

		//public string DetectDirection(int v)
		//{
		//	// Threshold mark at which action on axis/slider is detected.
		//	// Value gets in-between of specified range then action is recorded.
		//	// [--[p1]----[p2]--[n1]----[n2]--|--[p3]----[p4]--[n3]----[n4]--]
		//	int p1 = 2000;
		//	int space = (ushort.MaxValue - (p1 * 6)) / 4;
		//	int p2 = p1 + space;
		//	int n1 = p2 + p1;
		//	int n2 = n1 + space;
		//	int p3 = n2 + (p1 * 2);
		//	int p4 = p3 + space;
		//	int n3 = p4 + p1;
		//	int n4 = n3 + space;
		//	if (v > p1 && v < p2) return "";
		//	if (v > n1 && v < n2) return (isWheel) ? "IH" : "I";
		//	if (v > p3 && v < p4) return (isWheel) ? "H" : "";
		//	if (v > n3 && v < n4) return "I";
		//	return null;
		//}

	}
}
