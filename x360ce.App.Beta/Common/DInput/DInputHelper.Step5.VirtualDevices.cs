using SharpDX.XInput;
using System;
using System.Windows.Forms;
using x360ce.App.vBox;
using x360ce.Engine;

namespace x360ce.App.DInput
{
	public partial class DInputHelper
	{

		void UpdateVirtualDevices()
		{
			var game = MainForm.Current.CurrentGame;
			// If game does not support emulation type.
			if (!((EmulationType)game.EmulationType).HasFlag(EmulationType.Virtual))
				return;
			var error = CheckInstallVirtualDriver();
			if (error != VirtualError.None)
				return;
			for (uint i = 1; i <= 4; i++)
			{
				var mapTo = (MapTo)i;
				var flag = AppHelper.GetMapFlag(mapTo);
				var value = (MapToMask)game.EnableMask;
				var virtualEnabled = value.HasFlag(flag);
				if (virtualEnabled)
				{
					var success = EnableFeeding(i);
					if (success != VirtualError.None)
						return;
					var gp = CombinedXInputStates[i - 1].Gamepad;
					vXboxInterface.SetBtnA(i, gp.Buttons.HasFlag(GamepadButtonFlags.A));
					vXboxInterface.SetBtnB(i, gp.Buttons.HasFlag(GamepadButtonFlags.B));
					vXboxInterface.SetBtnX(i, gp.Buttons.HasFlag(GamepadButtonFlags.X));
					vXboxInterface.SetBtnY(i, gp.Buttons.HasFlag(GamepadButtonFlags.Y));
					vXboxInterface.SetBtnStart(i, gp.Buttons.HasFlag(GamepadButtonFlags.Start));
					vXboxInterface.SetBtnBack(i, gp.Buttons.HasFlag(GamepadButtonFlags.Back));
					vXboxInterface.SetBtnLT(i, gp.Buttons.HasFlag(GamepadButtonFlags.LeftThumb));
					vXboxInterface.SetBtnRT(i, gp.Buttons.HasFlag(GamepadButtonFlags.RightThumb));
					vXboxInterface.SetBtnLB(i, gp.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder));
					vXboxInterface.SetBtnRB(i, gp.Buttons.HasFlag(GamepadButtonFlags.RightShoulder));
					vXboxInterface.SetTriggerL(i, gp.LeftTrigger);
					vXboxInterface.SetTriggerR(i, gp.RightTrigger);
					vXboxInterface.SetAxisX(i, gp.LeftThumbX);
					vXboxInterface.SetAxisY(i, gp.LeftThumbY);
					vXboxInterface.SetAxisRx(i, gp.RightThumbX);
					vXboxInterface.SetAxisRy(i, gp.RightThumbY);
					vXboxInterface.SetDpadOff(i);
					if (gp.Buttons.HasFlag(GamepadButtonFlags.DPadUp))
						vXboxInterface.SetDpadUp(i);
					if (gp.Buttons.HasFlag(GamepadButtonFlags.DPadRight))
						vXboxInterface.SetDpadRight(i);
					if (gp.Buttons.HasFlag(GamepadButtonFlags.DPadDown))
						vXboxInterface.SetDpadDown(i);
					if (gp.Buttons.HasFlag(GamepadButtonFlags.DPadLeft))
						vXboxInterface.SetDpadLeft(i);
				}
				else
				{
					DisableFeeding(i);
				}
			}
		}

		public VirtualError CheckInstallVirtualDriver()
		{
			// If driver is installed already then return.
			if (vXboxInterface.isVBusExists())
				return VirtualError.None;
			JocysCom.ClassLibrary.Win32.NativeMethods.RunElevated(Application.ExecutablePath, Program.InstallVirtualDriverParam, System.Diagnostics.ProcessWindowStyle.Hidden);
			return VirtualError.None;
		}

		public VirtualError CheckUnInstallVirtualDriver()
		{
			// If driver is installed already then return.
			if (vXboxInterface.isVBusExists())
				return VirtualError.None;
			JocysCom.ClassLibrary.Win32.NativeMethods.RunElevated(Application.ExecutablePath, Program.UnInstallVirtualDriverParam, System.Diagnostics.ProcessWindowStyle.Hidden);
			return VirtualError.None;
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
