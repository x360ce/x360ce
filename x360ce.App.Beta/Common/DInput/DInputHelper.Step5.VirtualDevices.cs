using SharpDX.XInput;
using System;
using System.Windows.Forms;
using x360ce.Engine;

namespace x360ce.App.DInput
{
	public partial class DInputHelper
	{

		void UpdateVirtualDevices()
		{

			// Allow if not testing or testing with option enabled.
			var o = SettingsManager.Options;
			var allow = !o.TestEnabled || o.TestSetXInputStates;
			if (!allow)
				return;
			var game = MainForm.Current.CurrentGame;
			// If game does not support emulation type.
			if (!((EmulationType)game.EmulationType).HasFlag(EmulationType.Virtual))
				return;
			// If virtual driver is missing then return.
			if (!vXboxInterface.isVBusExists())
				return;
			for (uint i = 1; i <= 4; i++)
			{
				var mapTo = (MapTo)i;
				var flag = AppHelper.GetMapFlag(mapTo);
				var value = (MapToMask)game.EnableMask;
				var virtualEnabled = value.HasFlag(flag);
				var feedingState = FeedingState[i - 1];
				if (virtualEnabled)
				{
					// If feeding status unknonw or not enabled then...
					if (!feedingState.HasValue || !feedingState.Value)
					{
						var success = EnableFeeding(i) == VirtualError.None;
						if (!success)
							return;
						FeedingState[i - 1] = true;
					}
					FeedDevice(i);
				}
				else
				{
					// If feeding status unknonw or enabled then...
					if (!feedingState.HasValue || feedingState.Value)
					{
						var success = DisableFeeding(i) == VirtualError.None;
						if (!success)
							return;
						FeedingState[i - 1] = false;
					}
				}
			}
		}

		bool?[] FeedingState = new bool?[4];

		Gamepad[] oldGamepadStates = new Gamepad[4];

		bool Changed(uint i, GamepadButtonFlags flag)
		{
			return oldGamepadStates[i - 1].Buttons.HasFlag(flag) != CombinedXInputStates[i - 1].Gamepad.Buttons.HasFlag(flag);
		}

		public void FeedDevice(uint i)
		{
			// Get old and new game pad values.
			var n = CombinedXInputStates[i - 1].Gamepad;
			var o = oldGamepadStates[i - 1];
			// Update only when change.
			if (Changed(i, GamepadButtonFlags.A))
				vXboxInterface.SetBtnA(i, n.Buttons.HasFlag(GamepadButtonFlags.A));
			if (Changed(i, GamepadButtonFlags.B))
				vXboxInterface.SetBtnB(i, n.Buttons.HasFlag(GamepadButtonFlags.B));
			if (Changed(i, GamepadButtonFlags.X))
				vXboxInterface.SetBtnX(i, n.Buttons.HasFlag(GamepadButtonFlags.X));
			if (Changed(i, GamepadButtonFlags.Y))
				vXboxInterface.SetBtnY(i, n.Buttons.HasFlag(GamepadButtonFlags.Y));
			if (Changed(i, GamepadButtonFlags.Start))
				vXboxInterface.SetBtnStart(i, n.Buttons.HasFlag(GamepadButtonFlags.Start));
			if (Changed(i, GamepadButtonFlags.Back))
				vXboxInterface.SetBtnBack(i, n.Buttons.HasFlag(GamepadButtonFlags.Back));
			if (Changed(i, GamepadButtonFlags.LeftThumb))
				vXboxInterface.SetBtnLT(i, n.Buttons.HasFlag(GamepadButtonFlags.LeftThumb));
			if (Changed(i, GamepadButtonFlags.RightThumb))
				vXboxInterface.SetBtnRT(i, n.Buttons.HasFlag(GamepadButtonFlags.RightThumb));
			if (Changed(i, GamepadButtonFlags.LeftShoulder))
				vXboxInterface.SetBtnLB(i, n.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder));
			if (Changed(i, GamepadButtonFlags.RightShoulder))
				vXboxInterface.SetBtnRB(i, n.Buttons.HasFlag(GamepadButtonFlags.RightShoulder));
			if (n.LeftTrigger != o.LeftTrigger)
				vXboxInterface.SetTriggerL(i, n.LeftTrigger);
			if (n.RightTrigger != o.RightTrigger)
				vXboxInterface.SetTriggerR(i, n.RightTrigger);
			if (n.LeftThumbX != o.LeftThumbX)
				vXboxInterface.SetAxisX(i, n.LeftThumbX);
			if (n.LeftThumbY != o.LeftThumbY)
				vXboxInterface.SetAxisY(i, n.LeftThumbY);
			if (n.RightThumbX != o.RightThumbX)
				vXboxInterface.SetAxisRx(i, n.RightThumbX);
			if (n.RightThumbY != o.RightThumbY)
				vXboxInterface.SetAxisRy(i, n.RightThumbY);
			var changed = Changed(i, GamepadButtonFlags.DPadUp) ||
				Changed(i, GamepadButtonFlags.DPadRight) ||
				Changed(i, GamepadButtonFlags.DPadDown) ||
				Changed(i, GamepadButtonFlags.DPadLeft);
			if (changed)
			{
				vXboxInterface.SetDpadOff(i);
				if (n.Buttons.HasFlag(GamepadButtonFlags.DPadUp))
					vXboxInterface.SetDpadUp(i);
				if (n.Buttons.HasFlag(GamepadButtonFlags.DPadRight))
					vXboxInterface.SetDpadRight(i);
				if (n.Buttons.HasFlag(GamepadButtonFlags.DPadDown))
					vXboxInterface.SetDpadDown(i);
				if (n.Buttons.HasFlag(GamepadButtonFlags.DPadLeft))
					vXboxInterface.SetDpadLeft(i);
			}
			// Update old state.
			oldGamepadStates[i - 1] = n;
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
				{
					return VirtualError.None;
				}
				success = vXboxInterface.UnPlugForce(userIndex);
				if (!success)
					return VirtualError.Other;
			}
			success = vXboxInterface.PlugIn(userIndex);
			if (success)
			{
				return VirtualError.None;
			}
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
