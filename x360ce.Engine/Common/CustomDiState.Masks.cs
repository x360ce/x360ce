using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;

namespace x360ce.Engine
{
	public partial class CustomDiState
	{

		/// <summary>
		/// Return bit-masked integer about present axis.
		/// bit 1 = 1 - Axis 1 is present
		/// bit 2 = 0 - Axis 2 is missing
		/// bit 3 = 1 - Axis 3 is present
		/// ...
		/// </summary>
		public static void GetMouseAxisMask(DeviceObjectItem[] items, Mouse device, out int axisMask)
		{
			// Must have same order as in Axis[] property.
			// Important: These values are not the same as on DeviceObjectInstance.Offset.
			var list = new List<MouseOffset>{
					MouseOffset.X,
					MouseOffset.Y,
					MouseOffset.Z,
				};
			axisMask = 0;
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
					}
				}
				catch { }
			}
		}

		/// <summary>
		/// Return bit-masked integer about present axis.
		/// bit 1 = 1 - Axis 1 is present
		/// bit 2 = 0 - Axis 2 is missing
		/// bit 3 = 1 - Axis 3 is present
		/// ...
		/// </summary>
		public static void GetJoystickAxisMask(DeviceObjectItem[] items, Joystick device, out int axisMask, out int actuatorMask, out int actuatorCount)
		{
			axisMask = 0;
			actuatorMask = 0;
			actuatorCount = 0;
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

		/// <summary>
		/// Return bit-masked integer about present sliders.
		/// bit 1 = 1 - Slider 1 is present
		/// bit 2 = 0 - Slider 2 is missing
		/// bit 3 = 1 - Slider 3 is present
		/// ...
		/// </summary>
		public static void GetJoystickSlidersMask(DeviceObjectItem[] items, Joystick device, out int slidersMask)
		{
			slidersMask = 0;
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
						slidersMask |= (int)Math.Pow(2, i);
					}
				}
				catch { }
			}
		}

	}
}
