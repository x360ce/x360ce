using System;
using x360ce.App.vBox;
using x360ce.Engine;

namespace x360ce.App.DInput
{
	public partial class DInputHelper
	{

		void UpdateVirtualDevices()
		{
			var game = MainForm.Current.CurrentGame;
			for (uint i = 1; i <= 4; i++)
			{
				var mapTo = (MapTo)i;
				var flag = AppHelper.GetMapFlag(mapTo);
				var value = (MapToMask)game.VirtualMask;
				var virtualEnabled = value.HasFlag(flag);
				if (virtualEnabled)
				{
					EnableFeeding(i);
				}
				else
				{
					DisableFeeding(i);
				}
			}
		}

		public VirtualError EnableFeeding(uint userIndex)
		{
			bool success;
			if (userIndex < 1 || userIndex > 4)
				return VirtualError.Index;
			if (!vXboxInterface.isVBusExists())
				return VirtualError.Missing;
			if (vXboxInterface.isControllerExists(userIndex))
			{
				if (vXboxInterface.isControllerOwned(userIndex))
					return VirtualError.None;
				success = vXboxInterface.UnPlugForce(userIndex);
				if (!success)
					return VirtualError.Other;
			}
			success = vXboxInterface.PlugIn(userIndex);
			if (success)
				return VirtualError.None;
			return VirtualError.Other;
		}

		public VirtualError DisableFeeding(uint userIndex)
		{
			bool success;
			if (userIndex < 1 || userIndex > 4)
				return VirtualError.Index;
			if (!vXboxInterface.isVBusExists())
				return VirtualError.Missing;
			if (!vXboxInterface.isControllerExists(userIndex))
				return VirtualError.None;
			if (vXboxInterface.isControllerOwned(userIndex))
			{
				success = vXboxInterface.UnPlug(userIndex);
				if (success)
					return VirtualError.None;
			}
			else
			{
				success = vXboxInterface.UnPlugForce(userIndex);
				if (success)
					return VirtualError.None;
			}
			return VirtualError.Other;
		}

	}
}
