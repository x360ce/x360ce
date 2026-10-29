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

		/// <summary>Looks at the emulated controllers for the emulation buttons, on the interface thread, while they are on or being recorded.</summary>
		/// <remarks>
		/// A timer of its own, because the window stops drawing while it is minimised, which is when a game runs. It reads the
		/// states the engine has already combined, whether the emulated controllers are on or off, so the engine does nothing
		/// more for it and never waits for it.
		/// </remarks>
		Timer EmulationButtonsTimer;

		/// <summary>The emulation buttons, read from the options when they change.</summary>
		SharpDX.XInput.GamepadButtonFlags _emulationButtons;

		/// <summary>Whether the emulation buttons were held on the last look, so holding them flips emulation once.</summary>
		bool _emulationButtonsHeld;

		/// <summary>Starts looking for the emulation buttons while they are on or the Options page records them, and stops otherwise.</summary>
		/// <remarks>Called once the settings are loaded, and again whenever the box, the field or the recording changes.</remarks>
		public void ApplyEmulationButtons()
		{
			if (SettingsManager.OptionsData.Items.Count == 0)
				return;
			var o = SettingsManager.Options;
			_emulationButtons = o.EmulationButtonsEnabled ? HotkeyHelper.ParseButtons(o.EmulationButtons) : SharpDX.XInput.GamepadButtonFlags.None;
			var look = HotkeyHelper.IsCompleteButtons(_emulationButtons) || OptionsPanel.RecordingButtons;
			if (look && EmulationButtonsTimer == null)
			{
				EmulationButtonsTimer = new Timer { Interval = 50 };
				EmulationButtonsTimer.Tick += EmulationButtonsTimer_Tick;
				EmulationButtonsTimer.Start();
			}
			else if (!look && EmulationButtonsTimer != null)
			{
				EmulationButtonsTimer.Stop();
				EmulationButtonsTimer.Tick -= EmulationButtonsTimer_Tick;
				EmulationButtonsTimer.Dispose();
				EmulationButtonsTimer = null;
			}
		}

		void EmulationButtonsTimer_Tick(object sender, EventArgs e)
		{
			var states = Global.DHelper?.CombinedXiStates;
			if (states == null || Program.IsClosing)
				return;
			if (OptionsPanel.RecordingButtons)
			{
				OptionsPanel.RecordButtons(HotkeyHelper.HeldButtons(states));
				// Still held when the recording ends, the buttons are let go of before they flip anything.
				_emulationButtonsHeld = true;
				return;
			}
			if (HotkeyHelper.ButtonsPressed(_emulationButtons, states, ref _emulationButtonsHeld))
				ToggleEmulation();
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
