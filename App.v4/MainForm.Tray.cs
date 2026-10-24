using System;
using System.Reflection;
using System.Windows.Forms;

namespace x360ce.App
{

	// Configuration lines inside *.*proj file will open this partial class in a code editor.
	//
	//		<Compile Include="MainForm.Tray.cs">
	//			<DependentUpon>MainForm.cs</DependentUpon>
	//			<SubType>Code</SubType>
	//		</Compile>

	public partial class MainForm
	{

		void InitMinimize()
		{
			OptionsPanel.MinimizeToTrayCheckBox.Checked = SettingsManager.Options.MinimizeToTray;
			OptionsPanel.MinimizeToTrayCheckBox.CheckedChanged += MinimizeToTrayCheckBox_CheckedChanged;
			Resize += MainForm_Resize;
			TrayNotifyIcon.Click += TrayNotifyIcon_Click;
			TrayNotifyIcon.DoubleClick += TrayNotifyIcon_DoubleClick;
			TrayContextMenuStrip.Opening += TrayContextMenuStrip_Opening;
			// Run event once to apply settings.
			MainForm_Resize(this, new EventArgs());
		}

		void UnInitMinimize()
		{
			OptionsPanel.MinimizeToTrayCheckBox.CheckedChanged -= MinimizeToTrayCheckBox_CheckedChanged;
		}

		FormWindowState? oldWindowState;
		object windowStateLock = new object();

		private void MainForm_Resize(object sender, EventArgs e)
		{
			// Track window state changes.
			lock (windowStateLock)
			{
				var newWindowState = WindowState;
				if (!oldWindowState.HasValue || oldWindowState.Value != newWindowState)
				{
					oldWindowState = newWindowState;
					// If window was minimized.
					if (newWindowState == FormWindowState.Minimized)
					{
						MinimizeToTray(false, SettingsManager.Options.MinimizeToTray);
					}
				}
				// Form GUI update is very heavy on CPU.
				// Enable form GUI update only if form is not minimized.
				EnableFormUpdates(WindowState != FormWindowState.Minimized && !Program.IsClosing);
			}
		}

		private void MinimizeToTrayCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			SettingsManager.Options.MinimizeToTray = OptionsPanel.MinimizeToTrayCheckBox.Checked;
		}

		private void TrayNotifyIcon_Click(object sender, EventArgs e)
		{
			var mi = TrayNotifyIcon.GetType().GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
			mi.Invoke(TrayNotifyIcon, null);
		}

		private void TrayNotifyIcon_DoubleClick(object sender, EventArgs e)
		{
			RestoreFromTray();
		}

		private void OpenApplicationToolStripMenuItem_Click(object sender, EventArgs e)
		{
			RestoreFromTray();
		}

		/// <summary>Reads the switch each time the menu opens, so the page, the hotkey and the menu never disagree.</summary>
		/// <remarks>
		/// The hotkey is shown beside the entry the way Windows menus show their shortcuts, and only
		/// while it is on, so the menu never promises keys that do nothing.
		/// </remarks>
		private void TrayContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
		{
			var o = SettingsManager.Options;
			EmulationToolStripMenuItem.Checked = o.XInputEnabled;
			EmulationToolStripMenuItem.ShortcutKeyDisplayString = o.EmulationHotkeyEnabled ? o.EmulationHotkey : null;
		}

		private void EmulationToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SettingsManager.Options.XInputEnabled = EmulationToolStripMenuItem.Checked;
		}


		#region Minimize / Restore

		/// <summary>
		/// Minimize the window and hide it from the TaskBar.
		/// </summary>
		/// <remarks>
		/// The window is hidden rather than taken off the task bar. Changing ShowInTaskbar makes Windows Forms
		/// build the window and every control on it again, at each minimise and each restore: lists read their
		/// rows afresh in that rebuild and failed there, and a process short of window handles failed making
		/// them. A hidden window has no task bar button and is not in ALT+TAB.
		/// </remarks>
		public void MinimizeToTray(bool showBalloonTip, bool minimizeToTray)
		{
			if (!CanChangeWindowState)
				return;
			// Show only first time.
			if (showBalloonTip)
			{
				TrayNotifyIcon.BalloonTipText = Application.ProductName;
				// Show balloon tip for 2 seconds.
				TrayNotifyIcon.ShowBalloonTip(2, "X360CE", Application.ProductName, ToolTipIcon.Info);
			}
			// hold - program.
			// NOTE: also it would be possible to track which direction mouse will move in or move out on TrayIcon.
			// For example: open program if mouse moves in from left and moves out from top.
			TrayNotifyIcon.Text = "X360CE: Double click - program, click - menu.";
			if (WindowState != FormWindowState.Minimized)
			{
				WindowState = FormWindowState.Minimized;
			}
			// Hidden once it has finished minimising. A window started minimised has no handle yet; it comes
			// here again from Resize when it is first shown.
			if (minimizeToTray && IsHandleCreated)
				BeginInvoke((Action)HideInTray);
		}

		/// <summary>Hides the window, if it is still open and minimised.</summary>
		void HideInTray()
		{
			if (CanChangeWindowState && WindowState == FormWindowState.Minimized)
				Hide();
		}

		/// <summary>Whether the window is still one that can be shown, hidden or resized.</summary>
		/// <remarks>
		/// A second copy of the program, the tray icon and the command line all ask this window to
		/// show or hide itself, and the request can arrive while the window is closing. Rebuilding a
		/// window whose pages are already gone fails inside the framework and ends the program on a
		/// fault while closing; the request is simply too late, and is dropped.
		/// </remarks>
		bool CanChangeWindowState
		{
			get { return !Program.IsClosing && !IsDisposed && !Disposing; }
		}

		/// <summary>
		/// Restores the window.
		/// </summary>
		public void RestoreFromTray(bool activate = false)
		{
			if (!CanChangeWindowState)
				return;
			if (!Visible)
				Show();
			if (activate)
			{
				// Note: FormWindowState.Minimized and FormWindowState.Normal was used to make sure that Activate() wont fail because of this:
				// Windows NT 5.0 and later: An application cannot force a window to the foreground while the user is working with another window.
				// Instead, SetForegroundWindow will activate the window (see SetActiveWindow) and call theFlashWindowEx function to notify the user.
				if (WindowState != FormWindowState.Minimized)
				{
					WindowState = FormWindowState.Minimized;
				}
				this.Activate();
			}
			if (WindowState != FormWindowState.Normal)
			{
				WindowState = FormWindowState.Normal;
			}
			// Bring form to the front.
			var tm = TopMost;
			TopMost = true;
			TopMost = tm;
			BringToFront();
		}

		#endregion

	}
}
