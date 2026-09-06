using System;
using System.Windows.Forms;

namespace x360ce.App
{
	public partial class MainForm
	{
		/// <summary>Identifies the emulation hotkey among any this window registers.</summary>
		const int EmulationHotkeyId = 1;

		/// <summary>Registers the hotkey from the Options page, or lets go of it when the field is empty.</summary>
		/// <remarks>
		/// Called once the settings are loaded and again whenever the field changes.
		/// </remarks>
		/// <returns>True when Windows holds the hotkey for this window. False when there is none, or another program already holds the combination.</returns>
		public bool ApplyEmulationHotkey()
		{
			if (!IsHandleCreated || SettingsManager.OptionsData.Items.Count == 0)
				return false;
			HotkeyHelper.Unregister(Handle, EmulationHotkeyId);
			return HotkeyHelper.Register(Handle, EmulationHotkeyId, SettingsManager.Options.EmulationHotkey);
		}

		/// <summary>Turns the emulated controllers on or off, and says so from the notification area.</summary>
		/// <remarks>
		/// The window is usually minimised while a game runs, so the icon is the one place the person can see.
		/// </remarks>
		public void ToggleEmulation()
		{
			var o = SettingsManager.Options;
			o.XInputEnabled = !o.XInputEnabled;
			TrayNotifyIcon.ShowBalloonTip(2, "X360CE",
				o.XInputEnabled ? "Emulated controllers on" : "Emulated controllers off", ToolTipIcon.Info);
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			// Windows forgets a hotkey with the window handle it was registered on, and the handle is made
			// again whenever the taskbar button is hidden or shown, so it is registered again here.
			ApplyEmulationHotkey();
		}

		protected override void OnHandleDestroyed(EventArgs e)
		{
			HotkeyHelper.Unregister(Handle, EmulationHotkeyId);
			base.OnHandleDestroyed(e);
		}
	}
}
