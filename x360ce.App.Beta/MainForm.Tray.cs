using JocysCom.ClassLibrary.Controls;
using Microsoft.Win32;
using System;
using System.Linq;
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

		void InitMinimizeAndTopMost()
		{
			_hiddenForm.FormBorderStyle = FormBorderStyle.SizableToolWindow;
			_hiddenForm.ShowInTaskbar = false;
			Resize += MainForm_Resize;
			TrayNotifyIcon.Click += TrayNotifyIcon_Click;
			TrayNotifyIcon.DoubleClick += TrayNotifyIcon_DoubleClick;
			// Run event once to apply settings.
			MainForm_Resize(this, new EventArgs());
			var o = SettingsManager.Options;
			TopMost = o.AlwaysOnTop;
			InfoForm.MonitorEnabled = o.EnableShowFormInfo;
			// Start monitoring event.
			o.PropertyChanged += Options_PropertyChanged_Tray;
		}

		private void Options_PropertyChanged_Tray(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			var o = SettingsManager.Options;
			// Update controls by specific property.
			switch (e.PropertyName)
			{
				case nameof(Options.AlwaysOnTop):
					ControlsHelper.BeginInvoke(() =>
					{
						TopMost = o.AlwaysOnTop;
					});
					break;
				case nameof(Options.StartWithWindows):
				case nameof(Options.StartWithWindowsState):
					UpdateWindowsStartRegistry(o.StartWithWindows, o.StartWithWindowsState);
					break;
				case nameof(Options.EnableShowFormInfo):
					InfoForm.MonitorEnabled = o.EnableShowFormInfo;
					break;
				default:
					break;
			}
		}

		Form _hiddenForm = new Form();
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


		#region ■ Minimize / Restore

		/// <summary>
		/// Minimize the window and hide it from the TaskBar. 
		/// </summary>
		public void MinimizeToTray(bool showBalloonTip, bool minimizeToTray)
		{
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
			if (minimizeToTray)
			{
				// Set window style as ToolWindow to avoid its icon in ALT+TAB.
				if (Owner != _hiddenForm)
					Owner = _hiddenForm;
				// Hide form bar from the TarkBar.
				if (ShowInTaskbar)
					ShowInTaskbar = false;
			}
			if (WindowState != FormWindowState.Minimized)
				WindowState = FormWindowState.Minimized;
		}

		/// <summary>
		/// Restores the window.
		/// </summary>
		public void RestoreFromTray(bool activate = false)
		{
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
			// Show in task bar before restoring windows state in order to prevent flickering.
			if (!ShowInTaskbar)
				ShowInTaskbar = true;
			if (WindowState != FormWindowState.Normal)
			{
				WindowState = FormWindowState.Normal;
			}
			// Set window style as ToolWindow to show in ALT+TAB.
			if (Owner != null)
				Owner = null;
			// Bring form to the front.
			var tm = TopMost;
			TopMost = true;
			TopMost = tm;
			BringToFront();
		}

		#endregion

		#region ■ Operation 

		/// <summary>
		/// Requires no special permissions, because current used have full access to CurrentUser 'Run' registry key.
		/// </summary>
		/// <param name="enabled">Start with Windows after Sign-In.</param>
		/// <param name="startState">Start Mode.</param>
		public void UpdateWindowsStartRegistry(bool enabled, System.Windows.Forms.FormWindowState? startState = null)
		{
			var ai = new JocysCom.ClassLibrary.Configuration.AssemblyInfo();
			startState = startState ?? SettingsManager.Options.StartWithWindowsState;
			var runKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
			if (enabled)
			{
				// Add the value in the registry so that the application runs at start-up
				string command = string.Format("\"{0}\" /{1}={2}", ai.AssemblyPath, Program.arg_WindowState, startState.ToString());
				var value = (string)runKey.GetValue(ai.Product);
				if (value != command)
					runKey.SetValue(ai.Product, command);
			}
			else
			{
				// Remove the value from the registry so that the application doesn't start
				if (runKey.GetValueNames().Contains(ai.Product))
					runKey.DeleteValue(ai.Product, false);
			}
			runKey.Close();
		}

		#endregion


	}
}
