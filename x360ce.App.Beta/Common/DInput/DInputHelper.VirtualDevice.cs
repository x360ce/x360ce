using System;
using x360ce.App.vBox;

namespace x360ce.App.DInput
{
	public partial class DInputHelper
	{


		public Exception EnableFeeding(uint userIndex)
		{
			// Device ID can only be in the range 1-4
			if (userIndex < 1 || userIndex > 4)
			{
				return new Exception(string.Format("Illegal device ID {0}\nExit!", userIndex));
			}
			// If driver is not installed then...
			if (!vXboxInterface.isVBusExists())
			{
				return new Exception(string.Format("xBox driver not installed.\n"));
			}
			if (vXboxInterface.isControllerOwned(userIndex))
			{
				vXboxInterface.isControllerExists(userIndex);
			}

			var success = vXboxInterface.PlugIn(userIndex);
			if (!success)
			{
				return new Exception("Failed to PlugIn device");
			}
			return null;
		}

		public Exception DisableFeeding(uint userIndex)
		{
			// If disabled already then...
			if (!vBox.vXboxInterface.isControllerExists(userIndex))
				return null;
			bool success;
			if (vXboxInterface.isControllerOwned(userIndex))
			{
				success = vXboxInterface.UnPlug(userIndex);
				if (success)
					return null;
			}
			success = vXboxInterface.UnPlugForce(userIndex);
			if (!success)
			{
				return new Exception("Failed to UnPlug device"); ;
			}
			return null;
		}

	}
}
