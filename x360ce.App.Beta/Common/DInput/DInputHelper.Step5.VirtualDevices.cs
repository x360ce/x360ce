using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using SharpDX.XInput;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.DInput
{
	public partial class DInputHelper
	{

		/// <summary>
		/// Enable or disable virtual controllers depending on game settings.
		/// </summary>
		/// <param name="game"></param>
		void UpdateVirtualDevices(UserGame game)
		{

			// Allow if not testing or testing with option enabled.
			var o = SettingsManager.Options;
			var allow = !o.TestEnabled || o.TestSetXInputStates;
			if (!allow)
				return;
			// If virtual driver is missing then return.
			if (!ViGEmClient.isVBusExists())
				return;
			var isVirtual = game != null && ((EmulationType)game.EmulationType).HasFlag(EmulationType.Virtual);
			// If game does not use virtual emulation then...
			if (!isVirtual)
			{
				ViGEmClient.DisposeCurrent();
				return;
			}
			var client = ViGEmClient.Current;
			if (client.Targets == null)
			{
				client.Targets = new Xbox360Controller[4];
				for (int i = 0; i < 4; i++)
				{
					var controller = new Xbox360Controller(client);
					client.Targets[i] = controller;
					controller.FeedbackReceived += Controller_FeedbackReceived;
				}
			}
			for (uint i = 1; i <= 4; i++)
			{
				var mapTo = (MapTo)i;
				var flag = AppHelper.GetMapFlag(mapTo);
				var value = (MapToMask)(game?.EnableMask ?? (int)MapToMask.None);
				var virtualEnabled = value.HasFlag(flag);
				var feedingState = FeedingState[i - 1];
				if (virtualEnabled)
				{
					// If feeding status unknown or not enabled then...
					if (!feedingState.HasValue || !feedingState.Value || !client.IsControllerConnected(i))
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
					// If feeding status unknown or enabled then...
					if (!feedingState.HasValue || feedingState.Value || client.IsControllerConnected(i))
					{
						var success = DisableFeeding(i) == VirtualError.None;
						if (!success)
							return;
						FeedingState[i - 1] = false;
					}
				}
			}
		}

		object FeedbackLock = new object();

		Xbox360FeedbackReceivedEventArgs[] CopyAndClearFeedbacks()
		{
			lock (FeedbackLock)
			{
				var client = ViGEmClient.Current;
				if (client == null)
					return new Xbox360FeedbackReceivedEventArgs[4];
				var list = client.Feedbacks.ToArray();
				for (int i = 0; i < 4; i++)
				{
					client.Feedbacks[i] = null;
				}
				return list;
			}
		}

		public void SetVibration(MapTo userIndex, byte largeMotor, byte smallMotor, byte ledNumber)
		{
			var client = ViGEmClient.Current;
			if (client == null)
				return;
			var e = new Xbox360FeedbackReceivedEventArgs(largeMotor, smallMotor, ledNumber);
			ViGEmClient.Current.Feedbacks[(int)userIndex - 1] = e;
		}

		private void Controller_FeedbackReceived(object sender, Xbox360FeedbackReceivedEventArgs e)
		{
			lock (FeedbackLock)
			{
				var controller = (Xbox360Controller)sender;
				for (int i = 0; i < 4; i++)
				{
					if (ViGEmClient.Current.Targets[i] == controller)
					{
						// Add force feedback value for processing.
						ViGEmClient.Current.Feedbacks[i] = e;
						break;
					}
				}
			}
		}

		bool?[] FeedingState = new bool?[4];

		Gamepad[] oldGamepadStates = new Gamepad[4];

		bool IsGuideDown;
		object guideLock = new object();

		public void FeedDevice(uint i)
		{
			// Get old and new game pad values.
			var n = CombinedXiStates[i - 1].Gamepad;
			var report = new Xbox360Report();
			// Update only when change.
			report.SetButtonState(Xbox360Buttons.A, n.Buttons.HasFlag(GamepadButtonFlags.A));
			report.SetButtonState(Xbox360Buttons.B, n.Buttons.HasFlag(GamepadButtonFlags.B));
			report.SetButtonState(Xbox360Buttons.X, n.Buttons.HasFlag(GamepadButtonFlags.X));
			report.SetButtonState(Xbox360Buttons.Y, n.Buttons.HasFlag(GamepadButtonFlags.Y));
			report.SetButtonState(Xbox360Buttons.Start, n.Buttons.HasFlag(GamepadButtonFlags.Start));
			report.SetButtonState(Xbox360Buttons.Back, n.Buttons.HasFlag(GamepadButtonFlags.Back));
			report.SetButtonState(Xbox360Buttons.Guide, n.Buttons.HasFlag(GamepadButtonFlags.Guide));
			report.SetButtonState(Xbox360Buttons.LeftThumb, n.Buttons.HasFlag(GamepadButtonFlags.LeftThumb));
			report.SetButtonState(Xbox360Buttons.RightThumb, n.Buttons.HasFlag(GamepadButtonFlags.RightThumb));
			report.SetButtonState(Xbox360Buttons.LeftShoulder, n.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder));
			report.SetButtonState(Xbox360Buttons.RightShoulder, n.Buttons.HasFlag(GamepadButtonFlags.RightShoulder));
			report.SetButtonState(Xbox360Buttons.Up, n.Buttons.HasFlag(GamepadButtonFlags.DPadUp));
			report.SetButtonState(Xbox360Buttons.Right, n.Buttons.HasFlag(GamepadButtonFlags.DPadRight));
			report.SetButtonState(Xbox360Buttons.Down, n.Buttons.HasFlag(GamepadButtonFlags.DPadDown));
			report.SetButtonState(Xbox360Buttons.Left, n.Buttons.HasFlag(GamepadButtonFlags.DPadLeft));
			report.SetAxis(Xbox360Axes.LeftTrigger, n.LeftTrigger);
			report.SetAxis(Xbox360Axes.RightTrigger, n.RightTrigger);
			report.SetAxis(Xbox360Axes.LeftThumbX, n.LeftThumbX);
			report.SetAxis(Xbox360Axes.LeftThumbY, n.LeftThumbY);
			report.SetAxis(Xbox360Axes.RightThumbX, n.RightThumbX);
			report.SetAxis(Xbox360Axes.RightThumbY, n.RightThumbY);
			// Compare with old state.
			var o = oldGamepadStates[i - 1];
			var changed =
				n.Buttons != o.Buttons ||
				n.LeftThumbX != o.LeftThumbX ||
				n.LeftThumbY != o.LeftThumbY ||
				n.LeftTrigger != o.LeftTrigger ||
				n.RightThumbX != o.RightThumbX ||
				n.RightThumbY != o.RightThumbY ||
				n.RightTrigger != o.RightTrigger;
			// If state changed then...
			if (changed)
			{
				// Update controller.
				ViGEmClient.Current.Targets[i - 1].SendReport(report);
				lock (guideLock)
				{
					var isGuidePressed = n.Buttons.HasFlag(GamepadButtonFlags.Guide);
					if (isGuidePressed && !IsGuideDown)
					{
						var keys = GetGuideKeys();
						if (keys.Count() > 0)
							JocysCom.ClassLibrary.Processes.KeyboardHelper.SendDown(keys);
						IsGuideDown = true;
					}
					if (!isGuidePressed && IsGuideDown)
					{
						var keys = GetGuideKeys();
						if (keys.Count() > 0)
							JocysCom.ClassLibrary.Processes.KeyboardHelper.SendUp(keys);
						IsGuideDown = false;
					}
				}
				// Update old state.
				oldGamepadStates[i - 1] = n;
			}
		}

		private static Key[] GetGuideKeys()
		{
			var list = new List<Key>();
			var keys = SettingsManager.Options.GuideButtonAction;
			var matches = rxKeys.Matches(keys);
			foreach (Match m in matches)
			{
				var s = m.Groups["key"].Value;
				byte keyCode;
				// Try parse as byte/number first.
				if (byte.TryParse(s, out keyCode))
				{
					list.Add((Key)keyCode);
					continue;
				}
				// Try parse as "Keys" enum (ignore case).
				Key keyValue;
				if (System.Enum.TryParse(s, true, out keyValue))
				{
					list.Add(keyValue);
					continue;
				}
			}
			return list.ToArray();
		}

		private static Regex rxKeys = new Regex("{(?<key>[0-9a-zA-Z]+)}");

		public static VirtualError CheckInstallVirtualDriver()
		{
			// If driver is installed already then return.
			if (ViGEmClient.isVBusExists())
				return VirtualError.None;
			Program.RunElevated(AdminCommand.InstallViGEmBus);
			return VirtualError.None;
		}

		public static VirtualError CheckUnInstallVirtualDriver()
		{
			// If driver is installed already then return.
			if (!ViGEmClient.isVBusExists())
				return VirtualError.None;
			Program.RunElevated(AdminCommand.UninstallViGEmBus);
			return VirtualError.None;
		}

		public VirtualError EnableFeeding(uint userIndex)
		{
			if (userIndex < 1 || userIndex > 4)
				return VirtualError.Index;
			if (!ViGEmClient.isVBusExists())
				return VirtualError.Missing;
			if (!ViGEmClient.Current.isControllerExists(userIndex))
				return VirtualError.Other;
			if (ViGEmClient.Current.IsControllerConnected(userIndex))
				return VirtualError.None;
			var success = ViGEmClient.Current.PlugIn(userIndex);
			return success
				? VirtualError.None
				: VirtualError.Other;
		}

		public VirtualError DisableFeeding(uint userIndex)
		{
			bool success;
			if (userIndex < 1 || userIndex > 4)
				return VirtualError.Index;
			if (!ViGEmClient.isVBusExists())
				return VirtualError.Missing;
			if (!ViGEmClient.Current.isControllerExists(userIndex))
				return VirtualError.None;
			if (!ViGEmClient.Current.IsControllerConnected(userIndex))
				return VirtualError.None;
			success = ViGEmClient.Current.UnPlug(userIndex);
			return success
				? VirtualError.None
				: VirtualError.Other;
		}

	}
}
