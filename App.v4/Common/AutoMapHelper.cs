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
			var deviceType = (SharpDX.DirectInput.DeviceType)ud.CapType;
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
				// Twin USB Gamepad / Generic USB Joystick (VID_0810, PID_0001, VID_0079, DragonRise, ShanWan, Betop, etc.)
				var isTwinUsb = ud.DevVendorId == 0x0810 || ud.DevVendorId == 0x0079 || ud.DevVendorId == 0x11FF || ud.DevVendorId == 0x2563 ||
					(!string.IsNullOrEmpty(ud.InstanceName) && (
						ud.InstanceName.IndexOf("Twin USB", StringComparison.OrdinalIgnoreCase) >= 0 ||
						ud.InstanceName.IndexOf("USB Gamepad", StringComparison.OrdinalIgnoreCase) >= 0 ||
						ud.InstanceName.IndexOf("DragonRise", StringComparison.OrdinalIgnoreCase) >= 0 ||
						ud.InstanceName.IndexOf("ShanWan", StringComparison.OrdinalIgnoreCase) >= 0 ||
						ud.InstanceName.IndexOf("Generic USB", StringComparison.OrdinalIgnoreCase) >= 0)) ||
					(!string.IsNullOrEmpty(ud.ProductName) && (
						ud.ProductName.IndexOf("Twin USB", StringComparison.OrdinalIgnoreCase) >= 0 ||
						ud.ProductName.IndexOf("USB Gamepad", StringComparison.OrdinalIgnoreCase) >= 0 ||
						ud.ProductName.IndexOf("DragonRise", StringComparison.OrdinalIgnoreCase) >= 0 ||
						ud.ProductName.IndexOf("ShanWan", StringComparison.OrdinalIgnoreCase) >= 0 ||
						ud.ProductName.IndexOf("Generic USB", StringComparison.OrdinalIgnoreCase) >= 0));

				// Nintendo Switch Pro / Joy-Con / Wii U Pro
				var isNintendo = ud.DevVendorId == 0x057E ||
					(!string.IsNullOrEmpty(ud.ProductName) && ud.ProductName.IndexOf("Joy-Con", StringComparison.OrdinalIgnoreCase) >= 0);

				// Sony PlayStation (DualSense, DualSense Edge, DualShock 4, DualShock 3)
				var isSony = ud.DevVendorId == 0x054C ||
					(!string.IsNullOrEmpty(ud.ProductName) && (
						ud.ProductName.IndexOf("Wireless Controller", StringComparison.OrdinalIgnoreCase) >= 0 ||
						ud.ProductName.IndexOf("DualSense", StringComparison.OrdinalIgnoreCase) >= 0 ||
						ud.ProductName.IndexOf("DualShock", StringComparison.OrdinalIgnoreCase) >= 0));

				// Logitech Gamepads (F310, F510, F710, Dual Action, RumblePad 2)
				var isLogitechPad = ud.DevVendorId == 0x046D && (
					ud.DevProductId == 0xC216 || ud.DevProductId == 0xC218 || ud.DevProductId == 0xC219 || // F310, F510, F710 in DInput mode
					ud.DevProductId == 0xC214 || // Dual Action
					(!string.IsNullOrEmpty(ud.ProductName) && (
						ud.ProductName.IndexOf("Logitech Dual Action", StringComparison.OrdinalIgnoreCase) >= 0 ||
						ud.ProductName.IndexOf("Logitech RumblePad", StringComparison.OrdinalIgnoreCase) >= 0 ||
						ud.ProductName.IndexOf("Logitech Cordless", StringComparison.OrdinalIgnoreCase) >= 0)));

				// 8BitDo Gamepads (Pro 2, Ultimate, SN30 Pro)
				var is8BitDo = ud.DevVendorId == 0x2DC8 ||
					(!string.IsNullOrEmpty(ud.ProductName) && ud.ProductName.IndexOf("8BitDo", StringComparison.OrdinalIgnoreCase) >= 0);

				// DirectInput Racing Wheel (Logitech G25/G27/G29/G920/G923, Thrustmaster, Fanatec)
				var isWheel = ud.CapType == (int)DeviceType.Driving || ud.CapType == (int)DeviceType.Flight ||
					(!string.IsNullOrEmpty(ud.InstanceName) && (
						ud.InstanceName.IndexOf("Wheel", StringComparison.OrdinalIgnoreCase) >= 0 ||
						ud.InstanceName.IndexOf("Logitech G", StringComparison.OrdinalIgnoreCase) >= 0 ||
						ud.InstanceName.IndexOf("Driving Force", StringComparison.OrdinalIgnoreCase) >= 0 ||
						ud.InstanceName.IndexOf("Thrustmaster", StringComparison.OrdinalIgnoreCase) >= 0 ||
						ud.InstanceName.IndexOf("Fanatec", StringComparison.OrdinalIgnoreCase) >= 0)) ||
					(!string.IsNullOrEmpty(ud.ProductName) && (
						ud.ProductName.IndexOf("Wheel", StringComparison.OrdinalIgnoreCase) >= 0 ||
						ud.ProductName.IndexOf("Logitech G", StringComparison.OrdinalIgnoreCase) >= 0 ||
						ud.ProductName.IndexOf("Driving Force", StringComparison.OrdinalIgnoreCase) >= 0 ||
						ud.ProductName.IndexOf("Thrustmaster", StringComparison.OrdinalIgnoreCase) >= 0 ||
						ud.ProductName.IndexOf("Fanatec", StringComparison.OrdinalIgnoreCase) >= 0));

				if (isTwinUsb)
				{
					// Twin USB & Generic 12-Button USB physical button mapping:
					// Button 3 = Cross (A)
					// Button 2 = Circle (B)
					// Button 4 = Square (X)
					// Button 1 = Triangle (Y)
					// Button 5 = L1 (Left Shoulder)
					// Button 6 = R1 (Right Shoulder)
					// Button 7 = L2 (Left Trigger)
					// Button 8 = R2 (Right Trigger)
					// Button 9 = Select (Back)
					// Button 10 = Start (Start)
					// Button 11 = L3 (Left Thumb)
					// Button 12 = R3 (Right Thumb)
					ps.ButtonA = GetButtonValue(list, 2, true, "Button 2", "3");
					if (string.IsNullOrEmpty(ps.ButtonA)) ps.ButtonA = "3";
					ps.ButtonB = GetButtonValue(list, 1, true, "Button 1", "2");
					if (string.IsNullOrEmpty(ps.ButtonB)) ps.ButtonB = "2";
					ps.ButtonX = GetButtonValue(list, 3, true, "Button 3", "4");
					if (string.IsNullOrEmpty(ps.ButtonX)) ps.ButtonX = "4";
					ps.ButtonY = GetButtonValue(list, 0, true, "Button 0", "1");
					if (string.IsNullOrEmpty(ps.ButtonY)) ps.ButtonY = "1";
					ps.LeftShoulder = GetButtonValue(list, 4, true, "Button 4", "5");
					if (string.IsNullOrEmpty(ps.LeftShoulder)) ps.LeftShoulder = "5";
					ps.RightShoulder = GetButtonValue(list, 5, true, "Button 5", "6");
					if (string.IsNullOrEmpty(ps.RightShoulder)) ps.RightShoulder = "6";
					ps.LeftTrigger = GetButtonValue(list, 6, true, "Button 6", "7");
					if (string.IsNullOrEmpty(ps.LeftTrigger)) ps.LeftTrigger = "7";
					ps.RightTrigger = GetButtonValue(list, 7, true, "Button 7", "8");
					if (string.IsNullOrEmpty(ps.RightTrigger)) ps.RightTrigger = "8";
					ps.ButtonBack = GetButtonValue(list, 8, true, "Button 8", "9");
					if (string.IsNullOrEmpty(ps.ButtonBack)) ps.ButtonBack = "9";
					ps.ButtonStart = GetButtonValue(list, 9, true, "Button 9", "10");
					if (string.IsNullOrEmpty(ps.ButtonStart)) ps.ButtonStart = "10";
					ps.LeftThumbButton = GetButtonValue(list, 10, true, "Button 10", "11");
					if (string.IsNullOrEmpty(ps.LeftThumbButton)) ps.LeftThumbButton = "11";
					ps.RightThumbButton = GetButtonValue(list, 11, true, "Button 11", "12");
					if (string.IsNullOrEmpty(ps.RightThumbButton)) ps.RightThumbButton = "12";

					// Analog Sticks
					ps.LeftThumbAxisX = GetAxisValue(list, false, false, ObjectGuid.XAxis, true, "X-Axis");
					if (string.IsNullOrEmpty(ps.LeftThumbAxisX)) ps.LeftThumbAxisX = "a1";
					ps.LeftThumbAxisY = GetAxisValue(list, true, false, ObjectGuid.YAxis, true, "Y-Axis");
					if (string.IsNullOrEmpty(ps.LeftThumbAxisY)) ps.LeftThumbAxisY = "a-2";

					var rX = GetAxisValue(list, false, false, ObjectGuid.ZAxis, true, "Z-Axis");
					if (string.IsNullOrEmpty(rX))
						rX = GetAxisValue(list, false, false, ObjectGuid.RxAxis, true, "X-Rotation");
					if (string.IsNullOrEmpty(rX))
						rX = "a3";
					ps.RightThumbAxisX = rX;

					var rY = GetAxisValue(list, true, false, ObjectGuid.RzAxis, true, "Z-Rotation");
					if (string.IsNullOrEmpty(rY))
						rY = GetAxisValue(list, true, false, ObjectGuid.RyAxis, true, "Y-Rotation");
					if (string.IsNullOrEmpty(rY))
						rY = "a-6";
					ps.RightThumbAxisY = rY;
				}
				else if (isNintendo)
				{
					// Nintendo Switch Pro & Joy-Con (remap A/B and X/Y to match physical Xbox layout)
					ps.ButtonA = GetButtonValue(list, 0, true, "B");
					ps.ButtonB = GetButtonValue(list, 1, true, "A");
					ps.ButtonX = GetButtonValue(list, 2, true, "Y");
					ps.ButtonY = GetButtonValue(list, 3, true, "X");
					ps.LeftShoulder = GetButtonValue(list, 4, true, "L");
					ps.RightShoulder = GetButtonValue(list, 5, true, "R");
					ps.LeftTrigger = GetButtonValue(list, 6, true, "ZL");
					ps.RightTrigger = GetButtonValue(list, 7, true, "ZR");
					ps.ButtonBack = GetButtonValue(list, 8, true, "-");
					ps.ButtonStart = GetButtonValue(list, 9, true, "+");
					ps.LeftThumbButton = GetButtonValue(list, 10, true, "LStick");
					ps.RightThumbButton = GetButtonValue(list, 11, true, "RStick");
					ps.LeftThumbAxisX = GetAxisValue(list, false, false, ObjectGuid.XAxis, true);
					ps.LeftThumbAxisY = GetAxisValue(list, true, false, ObjectGuid.YAxis, true);
					ps.RightThumbAxisX = GetAxisValue(list, false, false, ObjectGuid.RxAxis, true);
					ps.RightThumbAxisY = GetAxisValue(list, true, false, ObjectGuid.RyAxis, true);
				}
				else if (isLogitechPad)
				{
					// Logitech F-Series in DirectInput mode, Dual Action & RumblePad 2
					ps.ButtonA = GetButtonValue(list, 1, true, "Button 1", "2");
					if (string.IsNullOrEmpty(ps.ButtonA)) ps.ButtonA = "2";
					ps.ButtonB = GetButtonValue(list, 2, true, "Button 2", "3");
					if (string.IsNullOrEmpty(ps.ButtonB)) ps.ButtonB = "3";
					ps.ButtonX = GetButtonValue(list, 0, true, "Button 0", "1");
					if (string.IsNullOrEmpty(ps.ButtonX)) ps.ButtonX = "1";
					ps.ButtonY = GetButtonValue(list, 3, true, "Button 3", "4");
					if (string.IsNullOrEmpty(ps.ButtonY)) ps.ButtonY = "4";
					ps.LeftShoulder = GetButtonValue(list, 4, true, "Button 4", "5");
					if (string.IsNullOrEmpty(ps.LeftShoulder)) ps.LeftShoulder = "5";
					ps.RightShoulder = GetButtonValue(list, 5, true, "Button 5", "6");
					if (string.IsNullOrEmpty(ps.RightShoulder)) ps.RightShoulder = "6";
					ps.LeftTrigger = GetButtonValue(list, 6, true, "Button 6", "7");
					if (string.IsNullOrEmpty(ps.LeftTrigger)) ps.LeftTrigger = "7";
					ps.RightTrigger = GetButtonValue(list, 7, true, "Button 7", "8");
					if (string.IsNullOrEmpty(ps.RightTrigger)) ps.RightTrigger = "8";
					ps.ButtonBack = GetButtonValue(list, 8, true, "Button 8", "9");
					if (string.IsNullOrEmpty(ps.ButtonBack)) ps.ButtonBack = "9";
					ps.ButtonStart = GetButtonValue(list, 9, true, "Button 9", "10");
					if (string.IsNullOrEmpty(ps.ButtonStart)) ps.ButtonStart = "10";
					ps.LeftThumbButton = GetButtonValue(list, 10, true, "Button 10", "11");
					if (string.IsNullOrEmpty(ps.LeftThumbButton)) ps.LeftThumbButton = "11";
					ps.RightThumbButton = GetButtonValue(list, 11, true, "Button 11", "12");
					if (string.IsNullOrEmpty(ps.RightThumbButton)) ps.RightThumbButton = "12";

					ps.LeftThumbAxisX = GetAxisValue(list, false, false, ObjectGuid.XAxis, true, "X-Axis");
					if (string.IsNullOrEmpty(ps.LeftThumbAxisX)) ps.LeftThumbAxisX = "a1";
					ps.LeftThumbAxisY = GetAxisValue(list, true, false, ObjectGuid.YAxis, true, "Y-Axis");
					if (string.IsNullOrEmpty(ps.LeftThumbAxisY)) ps.LeftThumbAxisY = "a-2";
					ps.RightThumbAxisX = GetAxisValue(list, false, false, ObjectGuid.ZAxis, true, "Z-Axis");
					if (string.IsNullOrEmpty(ps.RightThumbAxisX)) ps.RightThumbAxisX = "a3";
					ps.RightThumbAxisY = GetAxisValue(list, true, false, ObjectGuid.RzAxis, true, "Z-Rotation");
					if (string.IsNullOrEmpty(ps.RightThumbAxisY)) ps.RightThumbAxisY = "a-4";
				}
				else if (is8BitDo)
				{
					// 8BitDo Controllers in DirectInput Mode
					ps.ButtonA = GetButtonValue(list, 0, true, "Button 0", "1");
					if (string.IsNullOrEmpty(ps.ButtonA)) ps.ButtonA = "1";
					ps.ButtonB = GetButtonValue(list, 1, true, "Button 1", "2");
					if (string.IsNullOrEmpty(ps.ButtonB)) ps.ButtonB = "2";
					ps.ButtonX = GetButtonValue(list, 3, true, "Button 3", "4");
					if (string.IsNullOrEmpty(ps.ButtonX)) ps.ButtonX = "4";
					ps.ButtonY = GetButtonValue(list, 4, true, "Button 4", "5");
					if (string.IsNullOrEmpty(ps.ButtonY)) ps.ButtonY = "5";
					ps.LeftShoulder = GetButtonValue(list, 6, true, "Button 6", "7");
					ps.RightShoulder = GetButtonValue(list, 7, true, "Button 7", "8");
					ps.LeftTrigger = GetButtonValue(list, 8, true, "Button 8", "9");
					ps.RightTrigger = GetButtonValue(list, 9, true, "Button 9", "10");
					ps.ButtonBack = GetButtonValue(list, 10, true, "Button 10", "11");
					ps.ButtonStart = GetButtonValue(list, 11, true, "Button 11", "12");
					ps.LeftThumbButton = GetButtonValue(list, 13, true, "Button 13", "14");
					ps.RightThumbButton = GetButtonValue(list, 14, true, "Button 14", "15");

					ps.LeftThumbAxisX = GetAxisValue(list, false, false, ObjectGuid.XAxis, true);
					if (string.IsNullOrEmpty(ps.LeftThumbAxisX)) ps.LeftThumbAxisX = "a1";
					ps.LeftThumbAxisY = GetAxisValue(list, true, false, ObjectGuid.YAxis, true);
					if (string.IsNullOrEmpty(ps.LeftThumbAxisY)) ps.LeftThumbAxisY = "a-2";
					ps.RightThumbAxisX = GetAxisValue(list, false, false, ObjectGuid.ZAxis, true);
					if (string.IsNullOrEmpty(ps.RightThumbAxisX)) ps.RightThumbAxisX = "a3";
					ps.RightThumbAxisY = GetAxisValue(list, true, false, ObjectGuid.RzAxis, true);
					if (string.IsNullOrEmpty(ps.RightThumbAxisY)) ps.RightThumbAxisY = "a-4";
				}
				else if (isSony)
				{
					// Sony PlayStation: DualSense (PS5), DualShock 4 (PS4), DualShock 3 (PS3)
					ps.ButtonA = GetButtonValue(list, 1, true, "Cross", "Button 1");
					if (string.IsNullOrEmpty(ps.ButtonA)) ps.ButtonA = "2";
					ps.ButtonB = GetButtonValue(list, 2, true, "Circle", "Button 2");
					if (string.IsNullOrEmpty(ps.ButtonB)) ps.ButtonB = "3";
					ps.ButtonX = GetButtonValue(list, 0, true, "Square", "Button 0");
					if (string.IsNullOrEmpty(ps.ButtonX)) ps.ButtonX = "1";
					ps.ButtonY = GetButtonValue(list, 3, true, "Triangle", "Button 3");
					if (string.IsNullOrEmpty(ps.ButtonY)) ps.ButtonY = "4";
					ps.LeftShoulder = GetButtonValue(list, 4, true, "L1", "Button 4");
					if (string.IsNullOrEmpty(ps.LeftShoulder)) ps.LeftShoulder = "5";
					ps.RightShoulder = GetButtonValue(list, 5, true, "R1", "Button 5");
					if (string.IsNullOrEmpty(ps.RightShoulder)) ps.RightShoulder = "6";
					ps.ButtonBack = GetButtonValue(list, 8, true, "Share", "Create", "Select", "Button 8");
					if (string.IsNullOrEmpty(ps.ButtonBack)) ps.ButtonBack = "9";
					ps.ButtonStart = GetButtonValue(list, 9, true, "Options", "Start", "Button 9");
					if (string.IsNullOrEmpty(ps.ButtonStart)) ps.ButtonStart = "10";
					ps.LeftThumbButton = GetButtonValue(list, 10, true, "L3", "Left Paddle", "Button 10");
					if (string.IsNullOrEmpty(ps.LeftThumbButton)) ps.LeftThumbButton = "11";
					ps.RightThumbButton = GetButtonValue(list, 11, true, "R3", "Right Paddle", "Button 11");
					if (string.IsNullOrEmpty(ps.RightThumbButton)) ps.RightThumbButton = "12";

					// Map triggers from separate axes or buttons
					var lTrig = GetAxisValue(list, false, false, ObjectGuid.RxAxis, true, "L2");
					if (string.IsNullOrEmpty(lTrig)) lTrig = GetButtonValue(list, 6, true, "L2", "Button 6");
					if (string.IsNullOrEmpty(lTrig)) lTrig = "a4";
					ps.LeftTrigger = lTrig;

					var rTrig = GetAxisValue(list, false, false, ObjectGuid.RyAxis, true, "R2");
					if (string.IsNullOrEmpty(rTrig)) rTrig = GetButtonValue(list, 7, true, "R2", "Button 7");
					if (string.IsNullOrEmpty(rTrig)) rTrig = "a5";
					ps.RightTrigger = rTrig;

					// Analog Sticks
					ps.LeftThumbAxisX = GetAxisValue(list, false, false, ObjectGuid.XAxis, true, "X-Axis");
					if (string.IsNullOrEmpty(ps.LeftThumbAxisX)) ps.LeftThumbAxisX = "a1";
					ps.LeftThumbAxisY = GetAxisValue(list, true, false, ObjectGuid.YAxis, true, "Y-Axis");
					if (string.IsNullOrEmpty(ps.LeftThumbAxisY)) ps.LeftThumbAxisY = "a-2";

					ps.RightThumbAxisX = GetAxisValue(list, false, false, ObjectGuid.ZAxis, true, "Z-Axis");
					if (string.IsNullOrEmpty(ps.RightThumbAxisX)) ps.RightThumbAxisX = "a3";
					ps.RightThumbAxisY = GetAxisValue(list, true, false, ObjectGuid.RzAxis, true, "Z-Rotation");
					if (string.IsNullOrEmpty(ps.RightThumbAxisY)) ps.RightThumbAxisY = "a-6";
				}
				else if (isWheel)
				{
					// Steering Wheels (Logitech G25/G27/G29/G920/G923, Thrustmaster, Fanatec)
					ps.LeftThumbAxisX = GetAxisValue(list, false, false, ObjectGuid.XAxis, true, "Wheel", "X-Axis");
					if (string.IsNullOrEmpty(ps.LeftThumbAxisX)) ps.LeftThumbAxisX = "a1";

					// Accelerator & Brake Pedals
					ps.RightTrigger = GetAxisValue(list, false, false, ObjectGuid.YAxis, true, "Accelerator", "Throttle");
					if (string.IsNullOrEmpty(ps.RightTrigger)) ps.RightTrigger = "a-2";
					ps.LeftTrigger = GetAxisValue(list, false, false, ObjectGuid.RzAxis, true, "Brake");
					if (string.IsNullOrEmpty(ps.LeftTrigger)) ps.LeftTrigger = "a3";

					ps.ButtonA = GetButtonValue(list, 0, true, "Button 0", "1");
					ps.ButtonB = GetButtonValue(list, 1, true, "Button 1", "2");
					ps.ButtonX = GetButtonValue(list, 2, true, "Button 2", "3");
					ps.ButtonY = GetButtonValue(list, 3, true, "Button 3", "4");
					ps.LeftShoulder = GetButtonValue(list, 4, true, "Paddle L", "Button 4");
					ps.RightShoulder = GetButtonValue(list, 5, true, "Paddle R", "Button 5");
					ps.ButtonBack = GetButtonValue(list, 8, true, "Button 8", "9");
					ps.ButtonStart = GetButtonValue(list, 9, true, "Button 9", "10");
				}
				else
				{
					// Standard Generic DirectInput Gamepad Fallback
					ps.ButtonA = GetButtonValue(list, 0, true, "Cross", "Button 0", "1");
					if (string.IsNullOrEmpty(ps.ButtonA)) ps.ButtonA = "1";
					ps.ButtonB = GetButtonValue(list, 1, true, "Circle", "Button 1", "2");
					if (string.IsNullOrEmpty(ps.ButtonB)) ps.ButtonB = "2";
					ps.ButtonX = GetButtonValue(list, 2, true, "Square", "Button 2", "3");
					if (string.IsNullOrEmpty(ps.ButtonX)) ps.ButtonX = "3";
					ps.ButtonY = GetButtonValue(list, 3, true, "Triangle", "Button 3", "4");
					if (string.IsNullOrEmpty(ps.ButtonY)) ps.ButtonY = "4";
					ps.LeftShoulder = GetButtonValue(list, 4, true, "L1", "Button 4", "5");
					if (string.IsNullOrEmpty(ps.LeftShoulder)) ps.LeftShoulder = "5";
					ps.RightShoulder = GetButtonValue(list, 5, true, "R1", "Button 5", "6");
					if (string.IsNullOrEmpty(ps.RightShoulder)) ps.RightShoulder = "6";
					ps.ButtonBack = GetButtonValue(list, 6, true, "Select", "Back", "Button 6", "7");
					if (string.IsNullOrEmpty(ps.ButtonBack)) ps.ButtonBack = "7";
					ps.ButtonStart = GetButtonValue(list, 7, true, "Start", "Button 7", "8");
					if (string.IsNullOrEmpty(ps.ButtonStart)) ps.ButtonStart = "8";
					ps.LeftThumbButton = GetButtonValue(list, 8, true, "Button 8", "9");
					if (string.IsNullOrEmpty(ps.LeftThumbButton)) ps.LeftThumbButton = "9";
					ps.RightThumbButton = GetButtonValue(list, 9, true, "Button 9", "10");
					if (string.IsNullOrEmpty(ps.RightThumbButton)) ps.RightThumbButton = "10";

					// Triggers
					var rightTrigger = GetAxisValue(list, false, false, ObjectGuid.RzAxis, true, "R2");
					if (string.IsNullOrEmpty(rightTrigger))
					{
						ps.LeftTrigger = GetAxisValue(list, false, true, ObjectGuid.ZAxis, true, "L2");
						ps.RightTrigger = GetAxisValue(list, true, true, ObjectGuid.ZAxis, true, "L2");
					}
					else
					{
						ps.LeftTrigger = GetAxisValue(list, false, false, ObjectGuid.ZAxis, true, "L2");
						ps.RightTrigger = GetAxisValue(list, false, false, ObjectGuid.RzAxis, true, "R2");
					}

					// Analog Sticks
					ps.RightThumbAxisX = GetAxisValue(list, false, false, ObjectGuid.RxAxis, true);
					ps.RightThumbAxisY = GetAxisValue(list, true, false, ObjectGuid.RyAxis, true);
					ps.LeftThumbAxisX = GetAxisValue(list, false, false, ObjectGuid.XAxis, true, "Wheel axis");
					ps.LeftThumbAxisY = GetAxisValue(list, true, false, ObjectGuid.YAxis, true);
				}

				// Enable Force Feedback and Centering Spring for wheels and capable gamepads
				if (isWheel)
				{
					ps.ForceEnable = "1";
					ps.ForceType = "1";
					ps.ForceSpringStrength = "100";
				}
				{
					ps.ForceEnable = "1";
					ps.ForceType = "1";
					ps.ForceSpringStrength = "100";
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
				o = objects.FirstOrDefault(x => (x.Type == ObjectGuid.Button || x.Type == ObjectGuid.Key) && x.DiIndex == dIndex.Value);
			// Use instance number which is same as X360CE button index.
			return o == null ? "" : string.Format("{0}{1}", SettingName.SType.Button, o.DiIndex + 1);
		}

		/// <summary>Return axis setting value if axis exists.</summary>
		static string GetAxisValue(List<DeviceObjectItem> objects, bool invert, bool half, Guid type, bool removeIfFound, params string[] names)
		{
			DeviceObjectItem o = null;
			// Try to find by name.
			foreach (var name in names)
			{
				// Try exact match first.
				o = objects.FirstOrDefault(x => (x.Type == ObjectGuid.Button || x.Type == ObjectGuid.Key) && string.Compare(x.Name, name, true) == 0);
				if (o == null)
					o = objects.FirstOrDefault(x => (x.Type == ObjectGuid.Button || x.Type == ObjectGuid.Key) && x.Name.Contains(name));
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
			return o == null
				? ""
				: string.Format("{0}{1}{2}",
					// Set Half or Full axis
					half ? SettingName.SType.HAxis : SettingName.SType.Axis,
					// Set invert.
					invert ? "-" : "",
					// Use X360CE axis index.
					o.DiIndex + 1
				);
		}

	}
}
