using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using vJoyInterfaceWrap;

namespace x360ce.App.Controls
{
	partial class PadControl
	{

		// Declaring one joystick (Device id 1) and a position structure. 
		public vJoy joystick;
		public vJoy.JoystickState iReport;
		public uint id = 1;

		bool FeedXinputDevice(uint id, out string message)
		{
			message = "Not implemented";
			return false;
		}

		bool FeedDinputDevice(uint id, out string message)
		{
			message = "";
			// Create one joystick object and a position structure.
			joystick = new vJoy();
			iReport = new vJoy.JoystickState();
			// Device ID can only be in the range 1-16
			if (id <= 0 || id > 16)
			{
				message += string.Format("Illegal device ID {0}\nExit!", id);
				return false;
			}

			// Get the driver attributes (Vendor ID, Product ID, Version Number)
			if (!joystick.vJoyEnabled())
			{
				message += string.Format("vJoy driver not enabled: Failed Getting vJoy attributes.\n");
				return false;
			}
			else
				message += string.Format("Vendor: {0}\nProduct :{1}\nVersion Number:{2}\n", joystick.GetvJoyManufacturerString(), joystick.GetvJoyProductString(), joystick.GetvJoySerialNumberString());

			// Get the state of the requested device
			VjdStat status = joystick.GetVJDStatus(id);
			switch (status)
			{
				case VjdStat.VJD_STAT_OWN:
					message += string.Format("vJoy Device {0} is already owned by this feeder\n", id);
					break;
				case VjdStat.VJD_STAT_FREE:
					message += string.Format("vJoy Device {0} is free\n", id);
					break;
				case VjdStat.VJD_STAT_BUSY:
					message += string.Format("vJoy Device {0} is already owned by another feeder\nCannot continue\n", id);
					return false;
				case VjdStat.VJD_STAT_MISS:
					message += string.Format("vJoy Device {0} is not installed or disabled\nCannot continue\n", id);
					return false;
				default:
					message += string.Format("vJoy Device {0} general error\nCannot continue\n", id);
					return false;
			};

			// Check which axes are supported
			bool AxisX = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_X);
			bool AxisY = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_Y);
			bool AxisZ = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_Z);
			bool AxisRX = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_RX);
			bool AxisRZ = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_RZ);
			// Get the number of buttons and POV Hat switches supported by this vJoy device
			int nButtons = joystick.GetVJDButtonNumber(id);
			int ContPovNumber = joystick.GetVJDContPovNumber(id);
			int DiscPovNumber = joystick.GetVJDDiscPovNumber(id);

			// Print results
			message += string.Format("\nvJoy Device {0} capabilities:\n", id);
			message += string.Format("Number of buttons\t\t{0}\n", nButtons);
			message += string.Format("Number of Continuous POVs\t{0}\n", ContPovNumber);
			message += string.Format("Number of Discrete POVs\t\t{0}\n", DiscPovNumber);
			message += string.Format("Axis X\t\t{0}\n", AxisX ? "Yes" : "No");
			message += string.Format("Axis Y\t\t{0}\n", AxisX ? "Yes" : "No");
			message += string.Format("Axis Z\t\t{0}\n", AxisX ? "Yes" : "No");
			message += string.Format("Axis Rx\t\t{0}\n", AxisRX ? "Yes" : "No");
			message += string.Format("Axis Rz\t\t{0}\n", AxisRZ ? "Yes" : "No");

			// Test if DLL matches the driver
			UInt32 DllVer = 0, DrvVer = 0;
			bool match = joystick.DriverMatch(ref DllVer, ref DrvVer);
			if (match)
			{
				message += string.Format("Version of Driver Matches DLL Version ({0:X})\n", DllVer);
			}
			else
			{
				message += string.Format("Version of Driver ({0:X}) does NOT match DLL Version ({1:X})\n", DrvVer, DllVer);
			}

			// Acquire the target
			if ((status == VjdStat.VJD_STAT_OWN) || ((status == VjdStat.VJD_STAT_FREE) && (!joystick.AcquireVJD(id))))
			{
				message += string.Format("Failed to acquire vJoy device number {0}.\n", id);
				return false;
			}
			else
			{
				message += string.Format("Acquired: vJoy device number {0}.\n", id);
			}


			int X, Y, Z, ZR, XR;
			uint count = 0;
			long maxval = 0;

			X = 20;
			Y = 30;
			Z = 40;
			XR = 60;
			ZR = 80;

			joystick.GetVJDAxisMax(id, HID_USAGES.HID_USAGE_X, ref maxval);

			bool res;
			// Reset this device to default values
			joystick.ResetVJD(id);

			// Feed the device in endless loop
			while (!Program.IsClosing && FeedingEnabled)
			{
				// Set position of 4 axes
				res = joystick.SetAxis(X, id, HID_USAGES.HID_USAGE_X);
				res = joystick.SetAxis(Y, id, HID_USAGES.HID_USAGE_Y);
				res = joystick.SetAxis(Z, id, HID_USAGES.HID_USAGE_Z);
				res = joystick.SetAxis(XR, id, HID_USAGES.HID_USAGE_RX);
				res = joystick.SetAxis(ZR, id, HID_USAGES.HID_USAGE_RZ);

				// Press/Release Buttons
				res = joystick.SetBtn(true, id, count / 50);
				res = joystick.SetBtn(false, id, 1 + count / 50);

				// If Continuous POV hat switches installed - make them go round
				// For high values - put the switches in neutral state
				if (ContPovNumber > 0)
				{
					if ((count * 70) < 30000)
					{
						res = joystick.SetContPov(((int)count * 70), id, 1);
						res = joystick.SetContPov(((int)count * 70) + 2000, id, 2);
						res = joystick.SetContPov(((int)count * 70) + 4000, id, 3);
						res = joystick.SetContPov(((int)count * 70) + 6000, id, 4);
					}
					else
					{
						res = joystick.SetContPov(-1, id, 1);
						res = joystick.SetContPov(-1, id, 2);
						res = joystick.SetContPov(-1, id, 3);
						res = joystick.SetContPov(-1, id, 4);
					};
				};

				// If Discrete POV hat switches installed - make them go round
				// From time to time - put the switches in neutral state
				if (DiscPovNumber > 0)
				{
					if (count < 550)
					{
						joystick.SetDiscPov((((int)count / 20) + 0) % 4, id, 1);
						joystick.SetDiscPov((((int)count / 20) + 1) % 4, id, 2);
						joystick.SetDiscPov((((int)count / 20) + 2) % 4, id, 3);
						joystick.SetDiscPov((((int)count / 20) + 3) % 4, id, 4);
					}
					else
					{
						joystick.SetDiscPov(-1, id, 1);
						joystick.SetDiscPov(-1, id, 2);
						joystick.SetDiscPov(-1, id, 3);
						joystick.SetDiscPov(-1, id, 4);
					};
				};

				System.Threading.Thread.Sleep(20);
				X += 150; if (X > maxval) X = 0;
				Y += 250; if (Y > maxval) Y = 0;
				Z += 350; if (Z > maxval) Z = 0;
				XR += 220; if (XR > maxval) XR = 0;
				ZR += 200; if (ZR > maxval) ZR = 0;
				count++;

				if (count > 640)
					count = 0;

			} // While (Robust)
			return true;
		} // Main


	}
}
