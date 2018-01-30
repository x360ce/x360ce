using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets.Xbox360;
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
			if (!ViGEmClient.isVBusExists())
				return;
			if (ViGEmClient.Targets == null)
			{
				ViGEmClient.Targets = new Nefarius.ViGEm.Client.Targets.Xbox360Controller[4];
				ViGEmClient.Targets[0] = new Nefarius.ViGEm.Client.Targets.Xbox360Controller(ViGEmClient.Client);
				ViGEmClient.Targets[1] = new Nefarius.ViGEm.Client.Targets.Xbox360Controller(ViGEmClient.Client);
				ViGEmClient.Targets[2] = new Nefarius.ViGEm.Client.Targets.Xbox360Controller(ViGEmClient.Client);
				ViGEmClient.Targets[3] = new Nefarius.ViGEm.Client.Targets.Xbox360Controller(ViGEmClient.Client);
			}
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
			var report = new Nefarius.ViGEm.Client.Targets.Xbox360.Xbox360Report();
			// Update only when change.
			if (Changed(i, GamepadButtonFlags.A))
				report.SetButtonState(Xbox360Buttons.A, n.Buttons.HasFlag(GamepadButtonFlags.A));
			if (Changed(i, GamepadButtonFlags.B))
				report.SetButtonState(Xbox360Buttons.B, n.Buttons.HasFlag(GamepadButtonFlags.B));
			if (Changed(i, GamepadButtonFlags.X))
				report.SetButtonState(Xbox360Buttons.X, n.Buttons.HasFlag(GamepadButtonFlags.X));
			if (Changed(i, GamepadButtonFlags.Y))
				report.SetButtonState(Xbox360Buttons.Y, n.Buttons.HasFlag(GamepadButtonFlags.Y));
			if (Changed(i, GamepadButtonFlags.Start))
				report.SetButtonState(Xbox360Buttons.Start, n.Buttons.HasFlag(GamepadButtonFlags.Start));
			if (Changed(i, GamepadButtonFlags.Back))
				report.SetButtonState(Xbox360Buttons.Back, n.Buttons.HasFlag(GamepadButtonFlags.Back));
			if (Changed(i, GamepadButtonFlags.LeftThumb))
				report.SetButtonState(Xbox360Buttons.LeftThumb, n.Buttons.HasFlag(GamepadButtonFlags.LeftThumb));
			if (Changed(i, GamepadButtonFlags.RightThumb))
				report.SetButtonState(Xbox360Buttons.RightThumb, n.Buttons.HasFlag(GamepadButtonFlags.RightThumb));
			if (Changed(i, GamepadButtonFlags.LeftShoulder))
				report.SetButtonState(Xbox360Buttons.LeftShoulder, n.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder));
			if (Changed(i, GamepadButtonFlags.RightShoulder))
				report.SetButtonState(Xbox360Buttons.LeftShoulder, n.Buttons.HasFlag(GamepadButtonFlags.RightShoulder));
			if (n.LeftTrigger != o.LeftTrigger)
				report.SetAxis(Xbox360Axes.LeftTrigger, n.LeftTrigger);
			if (n.RightTrigger != o.RightTrigger)
				report.SetAxis(Xbox360Axes.RightTrigger, n.RightTrigger);
			if (n.LeftThumbX != o.LeftThumbX)
				report.SetAxis(Xbox360Axes.LeftThumbX, n.LeftThumbX);
			if (n.LeftThumbY != o.LeftThumbY)
				report.SetAxis(Xbox360Axes.LeftThumbY, n.LeftThumbY);
			if (n.RightThumbX != o.RightThumbX)
				report.SetAxis(Xbox360Axes.RightThumbX, n.RightThumbX);
			if (n.RightThumbY != o.RightThumbY)
				report.SetAxis(Xbox360Axes.RightThumbY, n.RightThumbY);
			// D-PAD
			if (Changed(i, GamepadButtonFlags.DPadUp))
				report.SetButtonState(Xbox360Buttons.Up, n.Buttons.HasFlag(GamepadButtonFlags.DPadUp));
			if (Changed(i, GamepadButtonFlags.DPadRight))
				report.SetButtonState(Xbox360Buttons.Right, n.Buttons.HasFlag(GamepadButtonFlags.DPadRight));
			if (Changed(i, GamepadButtonFlags.DPadDown))
				report.SetButtonState(Xbox360Buttons.Down, n.Buttons.HasFlag(GamepadButtonFlags.DPadDown));
			if (Changed(i, GamepadButtonFlags.DPadLeft))
				report.SetButtonState(Xbox360Buttons.Left, n.Buttons.HasFlag(GamepadButtonFlags.DPadLeft));
			// Update controller.
			ViGEmClient.Targets[i - 1].SendReport(report);
			// Update old state.
			oldGamepadStates[i - 1] = n;
		}

		public VirtualError CheckInstallVirtualDriver()
		{
			// If driver is installed already then return.
			if (ViGEmClient.isVBusExists())
				return VirtualError.None;
			JocysCom.ClassLibrary.Win32.NativeMethods.RunElevated(Application.ExecutablePath, Program.InstallVirtualDriverParam, System.Diagnostics.ProcessWindowStyle.Hidden);
			return VirtualError.None;
		}

		public VirtualError CheckUnInstallVirtualDriver()
		{
			// If driver is installed already then return.
			if (ViGEmClient.isVBusExists())
				return VirtualError.None;
			JocysCom.ClassLibrary.Win32.NativeMethods.RunElevated(Application.ExecutablePath, Program.UnInstallVirtualDriverParam, System.Diagnostics.ProcessWindowStyle.Hidden);
			return VirtualError.None;
		}

		public VirtualError EnableFeeding(uint userIndex)
		{
			bool success;
			if (userIndex < 1 || userIndex > 4)
				return VirtualError.Index;
			if (!ViGEmClient.isVBusExists())
				return VirtualError.Missing;
			if (ViGEmClient.isControllerExists(userIndex))
			{
				if (ViGEmClient.isControllerOwned(userIndex))
				{
					return VirtualError.None;
				}
				success = ViGEmClient.UnPlugForce(userIndex);
				if (!success)
					return VirtualError.Other;
			}
			success = ViGEmClient.PlugIn(userIndex);
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
			if (!ViGEmClient.isVBusExists())
				return VirtualError.Missing;
			if (!ViGEmClient.isControllerExists(userIndex))
				return VirtualError.None;
			if (ViGEmClient.isControllerOwned(userIndex))
			{
				success = ViGEmClient.UnPlug(userIndex);
				if (success)
					return VirtualError.None;
			}
			else
			{
				success = ViGEmClient.UnPlugForce(userIndex);
				if (success)
					return VirtualError.None;
			}
			return VirtualError.Other;
		}

	}
}
