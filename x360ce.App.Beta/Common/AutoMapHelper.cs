using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App
{
	public class AutoMapHelper
	{
		public static PadSetting GetAutoPreset(DeviceObjectItem[] objects)
		{
			var ps = new PadSetting();
			if (objects == null)
				return ps;
			// Get information about device.
			var o = objects.FirstOrDefault(x => x.Type == ObjectGuid.RxAxis);
			// If Right thumb triggers are missing then...
			if (o == null)
			{
				// Logitech RumblePad 2 USB
				ps.ButtonA = GetButtonValue(objects, 1);
				ps.ButtonB = GetButtonValue(objects, 2);
				ps.ButtonX = GetButtonValue(objects, 0);
				ps.ButtonY = GetButtonValue(objects, 3);
				ps.LeftShoulder = GetButtonValue(objects, 4);
				ps.RightShoulder = GetButtonValue(objects, 5);
				ps.ButtonBack = GetButtonValue(objects, 8);
				ps.ButtonStart = GetButtonValue(objects, 9);
				ps.LeftThumbButton = GetButtonValue(objects, 10);
				ps.RightThumbButton = GetButtonValue(objects, 11);
				// Triggers.
				ps.LeftTrigger = GetButtonValue(objects, 6);
				ps.RightTrigger = GetButtonValue(objects, 7);
				// Right Thumb.
				ps.RightThumbAxisX = GetAxisValue(objects, ObjectGuid.ZAxis);
				ps.RightThumbAxisY = GetAxisValue(objects, ObjectGuid.RzAxis);
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

				ps.ButtonA = GetButtonValue(objects, 0);
				ps.ButtonB = GetButtonValue(objects, 1);
				ps.ButtonX = GetButtonValue(objects, 2);
				ps.ButtonY = GetButtonValue(objects, 3);
				ps.LeftShoulder = GetButtonValue(objects, 4);
				ps.RightShoulder = GetButtonValue(objects, 5);
				ps.ButtonBack = GetButtonValue(objects, 6);
				ps.ButtonStart = GetButtonValue(objects, 7);
				ps.LeftThumbButton = GetButtonValue(objects, 8);
				ps.RightThumbButton = GetButtonValue(objects, 9);
				// Triggers.
				ps.LeftTrigger = GetAxisValue(objects, ObjectGuid.ZAxis);
				ps.RightTrigger = GetAxisValue(objects, ObjectGuid.RzAxis);
				// Right Thumb.
				ps.RightThumbAxisX = GetAxisValue(objects, ObjectGuid.RxAxis);
				ps.RightThumbAxisY = GetAxisValue(objects, ObjectGuid.RyAxis);
			}
			// Right Thumb.
			ps.LeftThumbAxisX = GetAxisValue(objects, ObjectGuid.XAxis);
			ps.LeftThumbAxisY = GetAxisValue(objects, ObjectGuid.YAxis);
			// D-Pad
			o = objects.FirstOrDefault(x => x.Type == ObjectGuid.PovController);
			ps.DPad = o == null ? "" : string.Format("{0}{1}", SettingName.SType.POV, o.Instance + 1);
			ps.PadSettingChecksum = ps.CleanAndGetCheckSum();
			return ps;
		}

		/// <summary>Return button setting value if button exists.</summary>
		static string GetButtonValue(DeviceObjectItem[] objects, int instance)
		{
			var o = objects.FirstOrDefault(x => x.Type == ObjectGuid.Button && x.Instance == instance);
			return o == null ? "" : string.Format("{0}{1}", SettingName.SType.Button, o.Instance + 1);
		}

		/// <summary>Return axis setting value if axis exists.</summary>
		static string GetAxisValue(DeviceObjectItem[] objects, Guid type)
		{
			// If axis found then...
			var o = objects.FirstOrDefault(x => x.Type == type);
			return o == null ? "" : string.Format("{0}{1}", SettingName.SType.Axis, o.Instance + 1);
		}


	}
}
