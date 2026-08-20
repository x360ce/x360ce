using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using x360ce.Engine;
using x360ce.Engine.Data;
using System.Text.RegularExpressions;

namespace x360ce.App
{
	public class AutoMapHelper
	{
		public static PadSetting GetAutoPreset(UserDevice ud)
		{
			var ps = new PadSetting();
			if (ud == null)
				return ps;
			var objects = ud.DeviceObjects;
			if (objects == null)
				return ps;
			var list = objects.ToList();
			// Get information about device.
			var deviceType = (DeviceType)ud.CapType;
			if (deviceType == DeviceType.Mouse)
			{
				// Offset  Type    Aspect    Flags         Instance  Name    
				// ------  ------  --------  ------------  --------  --------
				//      0  XAxis   Position  RelativeAxis         0  X-axis  
				//      4  YAxis   Position  RelativeAxis         1  Y-axis  
				//      8  ZAxis   Position  RelativeAxis         2  Wheel   
				//     12  Button            PushButton           3  Button 0
				//     13  Button            PushButton           4  Button 1
				//     14  Button            PushButton           5  Button 2
				//     15  Button            PushButton           6  Button 3
				//     16  Button            PushButton           7  Button 4
				//     17  Button            PushButton           8  Button 5
				//     18  Button            PushButton           9  Button 6
				//     19  Button            PushButton          10  Button 7
				//
				ps.ButtonA = GetButtonValue(list, 0, true, "Button 0");
				ps.ButtonB = GetButtonValue(list, 1, true, "Button 1");
				ps.ButtonX = GetButtonValue(list, 2, true, "Button 2");
				ps.ButtonY = GetButtonValue(list, 3, true, "Button 3");
				ps.LeftShoulder = GetButtonValue(list, 4, true, "Button 4");
				ps.RightShoulder = GetButtonValue(list, 5, true, "Button 5");
				ps.ButtonBack = GetButtonValue(list, 6, true, "Button 6");
				ps.ButtonStart = GetButtonValue(list, 7, true, "Button 7");
				ps.LeftThumbButton = GetButtonValue(list, 8, true, "Button 8");
				ps.RightThumbButton = GetButtonValue(list, 9, true, "Button 9");
				// Left Thumb (Look).
				ps.LeftThumbAxisX = GetAxisValue(list, false, false, ObjectGuid.XAxis, true, "X-Axis");
				ps.LeftThumbAxisY = GetAxisValue(list, true, false, ObjectGuid.YAxis, true, "Y-Axis");
				// Wheel.
				ps.RightThumbAxisY = GetAxisValue(list, true, false, ObjectGuid.ZAxis, true, "Wheel");
			}
			else if (deviceType == DeviceType.Keyboard)
			{
				ps.ButtonX = GetButtonValue(list, null, true, "^SPACE$"); // Jump/Kick
				ps.LeftThumbUp = GetButtonValue(list, null, true, "^W$"); // Move Forward
				ps.LeftThumbLeft = GetButtonValue(list, null, true, "^A$"); // Move Left
				ps.LeftThumbDown = GetButtonValue(list, null, true, "^S$"); // Move Backward
				ps.LeftThumbRight = GetButtonValue(list, null, true, "^D$"); // Move Right
																			 //ps.DPadUp = GetButtonValue(list, null, true, "^$"); // Phone Up
																			 //ps.DPadDown = GetButtonValue(list, null, true, "^]$"); // Phone Down
				ps.DPadLeft = GetButtonValue(list, null, true, "^\\[$"); // Previous Weapon
				ps.DPadRight = GetButtonValue(list, null, true, "^\\]$"); // Next Weapon
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
				//
				// If Sony then...
				if (ud.DevVendorId == 0x054C)
				{
					ps.ButtonA = GetButtonValue(list, 1, true, "Cross");
					ps.ButtonB = GetButtonValue(list, 2, true, "Circle");
					ps.ButtonX = GetButtonValue(list, 0, true, "Square"); // Jump/Kick
					ps.ButtonY = GetButtonValue(list, 3, true, "Triangle");
					ps.LeftShoulder = GetButtonValue(list, 4, true, "L1");
					ps.RightShoulder = GetButtonValue(list, 5, true, "R1");
					ps.ButtonBack = GetButtonValue(list, 8, true, "Select", "Back");
					ps.ButtonStart = GetButtonValue(list, 9, true, "StartDInputService");
					ps.LeftThumbButton = GetButtonValue(list, 10, true, "Left Paddle");
					ps.RightThumbButton = GetButtonValue(list, 11, true, "Right Paddle");
					// Map triggers from two different axis.
					ps.LeftTrigger = GetAxisValue(list, false, false, ObjectGuid.RxAxis, true, "L2");
					ps.RightTrigger = GetAxisValue(list, false, false, ObjectGuid.RyAxis, true, "R2");
					// Right Thumb.
					ps.RightThumbAxisX = GetAxisValue(list, false, false, ObjectGuid.ZAxis, true);
					// Y is inverted by default.
					ps.RightThumbAxisY = GetAxisValue(list, true, false, ObjectGuid.RzAxis, true);
					// Left Thumb.
					ps.LeftThumbAxisX = GetAxisValue(list, false, false, ObjectGuid.XAxis, true, "Wheel axis");
					// Y is inverted by default.
					ps.LeftThumbAxisY = GetAxisValue(list, true, false, ObjectGuid.YAxis, true);
				}
				// If DragonRise / Generic USB Gamepad / Twin USB Joystick (VID 0x0079) then...
				else if (ud.DevVendorId == 0x0079 || (ud.InstanceName != null && (ud.InstanceName.Contains("DragonRise") || ud.InstanceName.Contains("Generic USB Joystick") || ud.InstanceName.Contains("Twin USB"))))
				{
					ps.ButtonA = GetButtonValue(list, 2, true, "Button 3", "Cross");
					if (string.IsNullOrEmpty(ps.ButtonA)) ps.ButtonA = "b3";
					ps.ButtonB = GetButtonValue(list, 1, true, "Button 2", "Circle");
					if (string.IsNullOrEmpty(ps.ButtonB)) ps.ButtonB = "b2";
					ps.ButtonX = GetButtonValue(list, 3, true, "Button 4", "Square");
					if (string.IsNullOrEmpty(ps.ButtonX)) ps.ButtonX = "b4";
					ps.ButtonY = GetButtonValue(list, 0, true, "Button 1", "Triangle");
					if (string.IsNullOrEmpty(ps.ButtonY)) ps.ButtonY = "b1";
					ps.LeftShoulder = GetButtonValue(list, 4, true, "Button 5", "L1");
					if (string.IsNullOrEmpty(ps.LeftShoulder)) ps.LeftShoulder = "b5";
					ps.RightShoulder = GetButtonValue(list, 5, true, "Button 6", "R1");
					if (string.IsNullOrEmpty(ps.RightShoulder)) ps.RightShoulder = "b6";
					ps.LeftTrigger = GetButtonValue(list, 6, true, "Button 7", "L2");
					if (string.IsNullOrEmpty(ps.LeftTrigger)) ps.LeftTrigger = "b7";
					ps.RightTrigger = GetButtonValue(list, 7, true, "Button 8", "R2");
					if (string.IsNullOrEmpty(ps.RightTrigger)) ps.RightTrigger = "b8";
					ps.ButtonBack = GetButtonValue(list, 8, true, "Button 9", "Select", "Back");
					if (string.IsNullOrEmpty(ps.ButtonBack)) ps.ButtonBack = "b9";
					ps.ButtonStart = GetButtonValue(list, 9, true, "Button 10", "Start");
					if (string.IsNullOrEmpty(ps.ButtonStart)) ps.ButtonStart = "b10";
					ps.LeftThumbButton = GetButtonValue(list, 10, true, "Button 11");
					if (string.IsNullOrEmpty(ps.LeftThumbButton)) ps.LeftThumbButton = "b11";
					ps.RightThumbButton = GetButtonValue(list, 11, true, "Button 12");
					if (string.IsNullOrEmpty(ps.RightThumbButton)) ps.RightThumbButton = "b12";

					// Left Thumb (X = Axis 1, Y = Inverted Axis 2 [a-2]):
					ps.LeftThumbAxisX = GetAxisValue(list, false, false, ObjectGuid.XAxis, true, "X-Axis");
					if (string.IsNullOrEmpty(ps.LeftThumbAxisX)) ps.LeftThumbAxisX = "a1";
					ps.LeftThumbAxisY = GetAxisValue(list, true, false, ObjectGuid.YAxis, true, "Y-Axis");
					if (string.IsNullOrEmpty(ps.LeftThumbAxisY)) ps.LeftThumbAxisY = "a-2";

					// Right Thumb (X = Axis 3 [Z-Axis], Y = Inverted Axis 6 [a-6, Rz-Axis] or Slider):
					var rtx = GetAxisValue(list, false, false, ObjectGuid.ZAxis, true, "Z-Axis");
					if (string.IsNullOrEmpty(rtx)) rtx = GetAxisValue(list, false, false, ObjectGuid.RxAxis, true, "X Rotation");
					if (string.IsNullOrEmpty(rtx)) rtx = "a3";
					ps.RightThumbAxisX = rtx;

					var rty = GetAxisValue(list, true, false, ObjectGuid.RzAxis, true, "Z Rotation");
					if (string.IsNullOrEmpty(rty)) rty = GetAxisValue(list, true, false, ObjectGuid.RyAxis, true, "Y Rotation");
					if (string.IsNullOrEmpty(rty)) rty = GetAxisValue(list, true, false, ObjectGuid.Slider, true, "Slider");
					if (string.IsNullOrEmpty(rty)) rty = "a-6";
					ps.RightThumbAxisY = rty;
				}
				else
				{
					ps.ButtonA = GetButtonValue(list, 0, true, "Cross");
					ps.ButtonB = GetButtonValue(list, 1, true, "Circle");
					ps.ButtonX = GetButtonValue(list, 2, true, "Square"); // Jump/Kick
					ps.ButtonY = GetButtonValue(list, 3, true, "Triangle");
					ps.LeftShoulder = GetButtonValue(list, 4, true, "L1");
					ps.RightShoulder = GetButtonValue(list, 5, true, "R1");
					ps.ButtonBack = GetButtonValue(list, 6, true, "Select", "Back");
					ps.ButtonStart = GetButtonValue(list, 7, true, "StartDInputService");
					ps.LeftThumbButton = GetButtonValue(list, 8, true, "Left Paddle");
					ps.RightThumbButton = GetButtonValue(list, 9, true, "Right Paddle");
					// Triggers.
					var rightTrigger = GetAxisValue(list, false, false, ObjectGuid.RzAxis, true, "R2");
					// If RzAxis or "R2" name is missing then...
					if (string.IsNullOrEmpty(rightTrigger))
					{
						// Map triggers form single combined axis or buttons.
						ps.LeftTrigger = GetAxisValue(list, false, true, ObjectGuid.ZAxis, true, "L2");
						ps.RightTrigger = GetAxisValue(list, true, true, ObjectGuid.ZAxis, true, "L2");
						if (string.IsNullOrEmpty(ps.LeftTrigger)) ps.LeftTrigger = GetButtonValue(list, 6, true, "Button 7", "L2");
						if (string.IsNullOrEmpty(ps.RightTrigger)) ps.RightTrigger = GetButtonValue(list, 7, true, "Button 8", "R2");
					}
					else
					{
						// Map triggers from two different axis.
						ps.LeftTrigger = GetAxisValue(list, false, false, ObjectGuid.ZAxis, true, "L2");
						ps.RightTrigger = GetAxisValue(list, false, false, ObjectGuid.RzAxis, true, "R2");
					}
					// Left Thumb.
					ps.LeftThumbAxisX = GetAxisValue(list, false, false, ObjectGuid.XAxis, true, "Wheel axis", "X-Axis");
					// Y is inverted by default.
					ps.LeftThumbAxisY = GetAxisValue(list, true, false, ObjectGuid.YAxis, true, "Y-Axis");
					// Right Thumb.
					var rx = GetAxisValue(list, false, false, ObjectGuid.RxAxis, true, "X Rotation");
					if (string.IsNullOrEmpty(rx)) rx = GetAxisValue(list, false, false, ObjectGuid.ZAxis, true, "Z-Axis");
					ps.RightThumbAxisX = rx;

					var ry = GetAxisValue(list, true, false, ObjectGuid.RyAxis, true, "Y Rotation");
					if (string.IsNullOrEmpty(ry)) ry = GetAxisValue(list, true, false, ObjectGuid.RzAxis, true, "Z Rotation");
					if (string.IsNullOrEmpty(ry)) ry = GetAxisValue(list, true, false, ObjectGuid.Slider, true, "Slider");
					ps.RightThumbAxisY = ry;
				}
				// D-Pad
				var o = list.FirstOrDefault(x => x.Type == ObjectGuid.PovController);
				ps.DPad = o == null ? "" : string.Format("{0}{1}", SettingName.SType.POV, o.Instance + 1);
			}
			ps.PadSettingChecksum = ps.CleanAndGetCheckSum();
			return ps;
		}

		/// <summary>Return button setting value if button exists.</summary>
		static string GetButtonValue(List<DeviceObjectItem> objects, int? dIndex, bool removeIfFound, params string[] names)
		{
			DeviceObjectItem o = null;
			// Try to find by name.
			var rxs = names.Select(x => new Regex(x, RegexOptions.IgnoreCase));
			foreach (var rx in rxs)
			{
				// Try find a match.
				o = objects.FirstOrDefault(x => (x.Type == ObjectGuid.Button || x.Type == ObjectGuid.Key) && rx.IsMatch(x.Name));
				if (o != null)
				{
					if (removeIfFound)
						objects.Remove(o);
					break;
				}
			}
			// Try to find by Custom DIndex.
			if (o == null && dIndex.HasValue)
			{
				o = objects.FirstOrDefault(x => (x.Type == ObjectGuid.Button || x.Type == ObjectGuid.Key) && x.DiIndex == dIndex.Value);
				if (o != null && removeIfFound)
					objects.Remove(o);
			}
			// Use instance number which is same as X360CE button index.
			return o == null ? "" : string.Format($"{SettingName.SType.Button}{o.DiIndex}");
		}

		/// <summary>Return axis setting value if axis exists.</summary>
		static string GetAxisValue(List<DeviceObjectItem> objects, bool invert, bool half, Guid type, bool removeIfFound, params string[] names)
		{
			DeviceObjectItem o = null;
			// Try to find by name.
			foreach (var name in names)
			{
				// Try exact match first on axis/slider objects.
				o = objects.FirstOrDefault(x => (x.Type != ObjectGuid.Button && x.Type != ObjectGuid.Key && x.Type != ObjectGuid.PovController) && string.Compare(x.Name, name, true) == 0);
				if (o == null)
					o = objects.FirstOrDefault(x => (x.Type != ObjectGuid.Button && x.Type != ObjectGuid.Key && x.Type != ObjectGuid.PovController) && x.Name != null && x.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
				if (o != null)
				{
					if (removeIfFound)
						objects.Remove(o);
					break;
				}
			}
			// Try to find by type.
			if (o == null)
			{
				o = objects.FirstOrDefault(x => x.Type == type);
				if (o != null && removeIfFound)
					objects.Remove(o);
			}
			return o == null
				? ""
				: string.Format("{0}{1}{2}",
					// Set Half or Full axis
					half ? SettingName.SType.HAxis : SettingName.SType.Axis,
					// Set invert.
					invert ? "-" : "",
					// Use X360CE axis index.
					o.DiIndex
				);
		}

	}
}
