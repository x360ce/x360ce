using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App
{
	public class AutoMapHelper
	{
		public static PadSetting GetAutoPreset(DeviceObjectItem[] objects)
		{

			var ps = new PadSetting();
			var list = objects.ToList();
			if (list == null)
				return ps;
			// Get information about device.
			var o = list.FirstOrDefault(x => x.Type == ObjectGuid.RxAxis);
			// If Right thumb triggers are missing then...
			if (o == null)
			{
				// Logitech RumblePad 2 USB
				ps.ButtonA = GetButtonValue(list, 1, true, "Cross");
				ps.ButtonB = GetButtonValue(list, 2, true, "Circle");
				ps.ButtonX = GetButtonValue(list, 0, true, "Square");
				ps.ButtonY = GetButtonValue(list, 3, true, "Triangle");
				ps.LeftShoulder = GetButtonValue(list, 4, true, "L1");
				ps.RightShoulder = GetButtonValue(list, 5, true, "R1");
				ps.ButtonBack = GetButtonValue(list, 8, true, "Select", "Back");
				ps.ButtonStart = GetButtonValue(list, 9, true, "Start");
				ps.LeftThumbButton = GetButtonValue(list, 10, true, "Left Paddle");
				ps.RightThumbButton = GetButtonValue(list, 11, true, "Right Paddle");
				// Triggers.
				ps.LeftTrigger = GetButtonValue(list, 6, true, "L2");
				ps.RightTrigger = GetButtonValue(list, 7, true, "R2");
				// Right Thumb.
				ps.RightThumbAxisX = GetAxisValue(list, false, ObjectGuid.ZAxis, true);
				ps.RightThumbAxisY = GetAxisValue(list, true, ObjectGuid.RzAxis, true);
			}
			else
			{
				// ----------------------------------------------------------------------------------------------
				// Controller (Xbox One For Windows)
				// ----------------------------------------------------------------------------------------------
				// Offset   Usage  Instance  Guid           Name                            Flags                
				// ------  ------  --------  -------------  ------------------------------  ---------------------
				//      0      49         1  YAxis          Y Axis                          AbsoluteAxis         
				//      0       5         0  Unknown        Collection 0 - Game Pad         Collection, NoData   
				//      0       0         1  Unknown        Collection 1                    Collection, NoData   
				//      0       0         2  Unknown        Collection 2                    Collection, NoData   
				//      0       0         3  Unknown        Collection 3                    Collection, NoData   
				//      0     128         4  Unknown        Collection 4 - System Controls  Collection, NoData   
				//      4      48         0  XAxis          X Axis                          AbsoluteAxis         
				//      8      52         4  RyAxis         Y Rotation                      AbsoluteAxis         
				//     12      51         3  RxAxis         X Rotation                      AbsoluteAxis         
				//     16      50         2  ZAxis          Z Axis                          AbsoluteAxis         
				//     20      53         5  RzAxis         Z Rotation                      AbsoluteAxis         
				//     24      57         0  PovController  Hat Switch                      PointOfViewController
				//     32     151        19  Unknown        DC Enable Actuators             NoData, Output       
				//     36       1        20  Unknown        Physical Interface Device       NoData, Output       
				//     40     112        21  Unknown        Magnitude                       NoData, Output       
				//     44      80        22  Unknown        Duration                        NoData, Output       
				//     48     167        23  Unknown        Start Delay                     NoData, Output       
				//     52     124        24  Unknown        Loop Count                      NoData, Output       
				//     56       1         0  Button         Button 0                        PushButton           
				//     57       2         1  Button         Button 1                        PushButton           
				//     58       3         2  Button         Button 2                        PushButton           
				//     59       4         3  Button         Button 3                        PushButton           
				//     60       5         4  Button         Button 4                        PushButton           
				//     61       6         5  Button         Button 5                        PushButton           
				//     62       7         6  Button         Button 6                        PushButton           
				//     63       8         7  Button         Button 7                        PushButton           
				//     64       9         8  Button         Button 8                        PushButton           
				//     65      10         9  Button         Button 9                        PushButton           
				//     66     133        10  Button         System Main Menu                PushButton           

				ps.ButtonA = GetButtonValue(list, 0, true, "Cross");
				ps.ButtonB = GetButtonValue(list, 1, true, "Circle");
				ps.ButtonX = GetButtonValue(list, 2, true, "Square");
				ps.ButtonY = GetButtonValue(list, 3, true, "Triangle");
				ps.LeftShoulder = GetButtonValue(list, 4, true, "L1");
				ps.RightShoulder = GetButtonValue(list, 5, true, "R1");
				ps.ButtonBack = GetButtonValue(list, 6, true, "Select", "Back");
				ps.ButtonStart = GetButtonValue(list, 7, true, "Start");
				ps.LeftThumbButton = GetButtonValue(list, 8, true, "Left Paddle");
				ps.RightThumbButton = GetButtonValue(list, 9, true, "Right Paddle");
				//Combined pedals
				// Triggers.
				ps.LeftTrigger = GetAxisValue(list, false, ObjectGuid.ZAxis, true, "L2");
				ps.RightTrigger = GetAxisValue(list, false, ObjectGuid.RzAxis, true, "R2");
				// Right Thumb.
				ps.RightThumbAxisX = GetAxisValue(list, false, ObjectGuid.RxAxis, true);
				// Y is inverted by default.
				ps.RightThumbAxisY = GetAxisValue(list, true, ObjectGuid.RyAxis, true);
			}
			// Right Thumb.
			ps.LeftThumbAxisX = GetAxisValue(list, false, ObjectGuid.XAxis, true, "Wheel axis");
			// Y is inverted by default.
			ps.LeftThumbAxisY = GetAxisValue(list, true, ObjectGuid.YAxis, true);
			// D-Pad
			o = list.FirstOrDefault(x => x.Type == ObjectGuid.PovController);
			ps.DPad = o == null ? "" : string.Format("{0}{1}", SettingName.SType.POV, o.Instance + 1);
			ps.PadSettingChecksum = ps.CleanAndGetCheckSum();
			return ps;
		}

		/// <summary>Return button setting value if button exists.</summary>
		static string GetButtonValue(List<DeviceObjectItem> objects, int instance, bool removeIfFound, params string[] names)
		{
			DeviceObjectItem o = null;
			// Try to find by name.
			foreach (var name in names)
			{
				o = objects.FirstOrDefault(x => x.Type == ObjectGuid.Button && x.Name.Contains(name));
				if (o != null)
				{
					if (removeIfFound)
						objects.Remove(o);
					break;
				}
			}
			// Try to find by instance.
			if (o == null)
				o = objects.FirstOrDefault(x => x.Type == ObjectGuid.Button && x.Instance == instance);
			// Use instance number which is same as X360CE button index.
			return o == null ? "" : string.Format("{0}{1}", SettingName.SType.Button, o.Instance + 1);
		}

		/// <summary>Return axis setting value if axis exists.</summary>
		static string GetAxisValue(List<DeviceObjectItem> objects, bool invert, Guid type, bool removeIfFound, params string[] names)
		{
			DeviceObjectItem o = null;
			// Try to find by name.
			foreach (var name in names)
			{
				o = objects.FirstOrDefault(x => x.Type == ObjectGuid.Button && x.Name.Contains(name));
				if (o != null)
				{
					if (removeIfFound)
						objects.Remove(o);
					break;
				}
			}
			// Try to find by type.
			if (o == null)
				o = objects.FirstOrDefault(x => x.Type == type);
			return o == null ? "" : string.Format("{0}{1}",
				(invert ? "-" : "") + SettingName.SType.Axis
				// Use X360CE axis index.
				, o.DiIndex + 1);
		}

	}
}
