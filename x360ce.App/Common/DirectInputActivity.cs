using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace x360ce.App
{
	public class DirectInputActivity
	{
		int[] Axis = new int[0];
		int[] Sliders = new int[0];
		int[] Pows = new int[0];
		bool[] Buttons = new bool[0];

		public DirectInputActivity(JoystickState state)
		{

			// Fill axis.
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
			// Fill Sliders.
			List<int> Sliders = new List<int>();
			Sliders.AddRange(state.Sliders);
			Sliders.AddRange(state.AccelerationSliders);
			Sliders.AddRange(state.ForceSliders);
			Sliders.AddRange(state.VelocitySliders);
			// Fill Pows.
			Pows = state.PointOfViewControllers.ToArray();
			// Fill buttons.
			Buttons = state.Buttons.ToArray();
		}

		/// <summary>
		/// Compare to another state.
		/// </summary>
		/// <param name="state"></param>
		/// <returns></returns>
		public string[] CompareTo(DirectInputActivity state)
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
						list.Add(string.Format("Button{0}", i + 1));
					}
				}
			};
			// Compare Pows.
			if (Pows.Length == state.Pows.Length)
			{
				for (int i = 0; i < Pows.Length; i++)
				{
					if (Pows[i] != state.Pows[i])
					{
						list.Add(string.Format("DPad{0} {1}", i + 1, ""));
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
					list.Add(string.Format("{0}{1}{2}", prefix, name, i + 1));
				}
			}
			return list.ToArray();
		}

		//public string DetectDirection(int v)
		//{
		//	// Threshold mark at which action on axis/slider is detected.
		//	// Value gets inbetween of specified range then action is recorded.
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
