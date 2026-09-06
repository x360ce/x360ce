using System;
using System.Windows.Forms;

namespace x360ce.App
{
	public partial class MainForm
	{
		/// <summary>Identifies the emulation hotkey among any this window registers.</summary>
		const int EmulationHotkeyId = 1;

		/// <summary>Registers the hotkey from the Options page, or lets go of it when the box is off or the field is empty.</summary>
		/// <remarks>
		/// Called once the settings are loaded and again whenever the box or the field changes.
		/// </remarks>
		/// <returns>True when Windows holds the hotkey for this window. False when it is off, there is none, or another program already holds the combination.</returns>
		public bool ApplyEmulationHotkey()
		{
			if (!IsHandleCreated || SettingsManager.OptionsData.Items.Count == 0)
				return false;
			HotkeyHelper.Unregister(Handle, EmulationHotkeyId);
			var o = SettingsManager.Options;
			if (!o.EmulationHotkeyEnabled)
				return false;
			return HotkeyHelper.Register(Handle, EmulationHotkeyId, o.EmulationHotkey);
		}

		/// <summary>Turns the emulated controllers on or off, and says so where the person is looking.</summary>
		/// <remarks>
		/// The window is usually minimised while a game runs. A note over the game is the one answer
		/// that reaches a person there: the tray balloon is a Windows notification, and Windows silences
		/// those by itself while a full-screen game runs. The balloon stays for whoever turns the note off.
		/// </remarks>
		public void ToggleEmulation()
		{
			var o = SettingsManager.Options;
			o.XInputEnabled = !o.XInputEnabled;
			var text = o.XInputEnabled ? "Emulated controllers on" : "Emulated controllers off";
			if (o.EmulationHotkeyOverlay)
				OverlayNote.Show(text, System.Drawing.ColorTranslator.FromHtml(o.XInputEnabled ? AppHelper.StatusGreen : AppHelper.StatusGrey));
			else
				TrayNotifyIcon.ShowBalloonTip(2, "X360CE", text, ToolTipIcon.Info);
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
