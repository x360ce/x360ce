using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using x360ce.App.vBox;

namespace x360ce.App.Controls
{
	partial class PadControl
	{

		bool FeedXInputDevice(uint userIndex, out string message)
		{
			message = "";
			// Device ID can only be in the range 1-4
			if (userIndex < 1 || userIndex > 4)
			{
				message += string.Format("Illegal device ID {0}\nExit!", userIndex);
				return false;
			}
			// If driver is not installed then...
			if (!vXboxInterface.isVBusExists())
			{
				message += string.Format("xBox driver not installed.\n");
				return false;
			}
			var success = vXboxInterface.PlugIn(userIndex);
			if (!success)
			{
				return false;
			}
			//// Get the state of the requested device
			//VjdStat status = joystick.GetVJDStatus(userIndex);
			//switch (status)
			//{
			//	case VjdStat.VJD_STAT_OWN:
			//		message += string.Format("vJoy Device {0} is already owned by this feeder\n", userIndex);
			//		break;
			//	case VjdStat.VJD_STAT_FREE:
			//		message += string.Format("vJoy Device {0} is free\n", userIndex);
			//		break;
			//	case VjdStat.VJD_STAT_BUSY:
			//		message += string.Format("vJoy Device {0} is already owned by another feeder\nCannot continue\n", userIndex);
			//		return false;
			//	case VjdStat.VJD_STAT_MISS:
			//		message += string.Format("vJoy Device {0} is not installed or disabled\nCannot continue\n", userIndex);
			//		return false;
			//	default:
			//		message += string.Format("vJoy Device {0} general error\nCannot continue\n", userIndex);
			//		return false;
			//};

			// Feed the device in endless loop
			while (!Program.IsClosing && FeedingEnabled)
			{



			}
			success = vXboxInterface.UnPlugForce(userIndex);
			if (!success)
			{
				return false;
			}
			return true;
		}


	}
}
