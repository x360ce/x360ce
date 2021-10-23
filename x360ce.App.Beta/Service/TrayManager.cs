using JocysCom.ClassLibrary.Controls;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace x360ce.App.Service
{
	public partial class TrayManager : IDisposable
	{

		public event EventHandler OnExitClick;
		public event EventHandler OnWindowSizeChanged;

		public Window _Window;

		private System.Windows.Forms.NotifyIcon TrayNotifyIcon;
		private System.Windows.Forms.ContextMenuStrip TrayMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem OpenApplicationMenu;
		private System.Windows.Forms.ToolStripMenuItem ExitMenu;
		private System.Windows.Forms.ToolStripMenuItem ShowLoadedControls;

		public void InitMinimizeAndTopMost()
		{
			// Item: Open Application.
			OpenApplicationMenu = new System.Windows.Forms.ToolStripMenuItem();
			OpenApplicationMenu.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
			OpenApplicationMenu.Image = Properties.Resources.app_16x16;
			OpenApplicationMenu.Text = "Open Application";
			OpenApplicationMenu.Click += OpenApplicationToolStripMenuItem_Click;
			// Item: Exit Menu.
			ExitMenu = new System.Windows.Forms.ToolStripMenuItem();
			ExitMenu.Image = Properties.Resources.exit_16x16;
			ExitMenu.Text = "Exit";
			ExitMenu.Click += (sender, e) => OnExitClick?.Invoke(sender, e);
			// Item: Show Loaded controls.
			ShowLoadedControls = new System.Windows.Forms.ToolStripMenuItem();
			ShowLoadedControls.Image = Properties.Resources.test_16x16;
			ShowLoadedControls.Text = "Show Loaded Controls";
			ShowLoadedControls.Click += (sender, e) =>
			{
				var names = string.Join("\r\n", InitHelper.LoadedControlNames);
				Clipboard.SetText(names);
				MessageBox.Show(names);
			};
			// Tray menu.
			TrayMenuStrip = new System.Windows.Forms.ContextMenuStrip();
			TrayNotifyIcon = new System.Windows.Forms.NotifyIcon();
			TrayNotifyIcon.ContextMenuStrip = TrayMenuStrip;
			TrayMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
				OpenApplicationMenu,
				ShowLoadedControls,
				ExitMenu,
			});
			var iconBytes = JocysCom.ClassLibrary.Helper.FindResource<byte[]>("app.ico");
			var ms = new MemoryStream(iconBytes);
			TrayNotifyIcon.Icon = new System.Drawing.Icon(ms);
			TrayNotifyIcon.Visible = true;
			_hiddenForm.ShowInTaskbar = false;
			TrayNotifyIcon.Click += TrayNotifyIcon_Click;
			TrayNotifyIcon.DoubleClick += TrayNotifyIcon_DoubleClick;
		}

		public void SetWindow(Window window)
		{
			var o = SettingsManager.Options;
			if (_Window != null)
			{
				InfoForm.MonitorEnabled = false;
				_Window.SizeChanged -= MainWindow_SizeChanged;
				o.PropertyChanged -= Options_PropertyChanged_Tray;
			}
			_Window = window;
			if (window == null)
			{
				CollectGarbage();
				return;
			}
			_Window.SizeChanged += MainWindow_SizeChanged;
			// Run event once to apply settings.
			MainWindow_SizeChanged(this, null);
			_Window.Topmost = o.AlwaysOnTop;
			InfoForm.MonitorEnabled = o.EnableShowFormInfo;
			// Start monitoring event.
			o.PropertyChanged += Options_PropertyChanged_Tray;
		}

		void CollectGarbage()
		{
			for (int i = 0; i < 4; i++)
			{
				GC.Collect(GC.MaxGeneration);
				GC.WaitForPendingFinalizers();
			}
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
						_Window.Topmost = o.AlwaysOnTop;
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

		Window _hiddenForm = new Window();
		WindowState? oldWindowState;
		object windowStateLock = new object();

		private void MainWindow_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
		{
			// Track window state changes.
			lock (windowStateLock)
			{
				var newWindowState = _Window.WindowState;
				if (!oldWindowState.HasValue || oldWindowState.Value != newWindowState)
				{
					oldWindowState = newWindowState;
					// If window was minimized.
					if (newWindowState == WindowState.Minimized)
						MinimizeToTray(false, SettingsManager.Options.MinimizeToTray);
				}
				OnWindowSizeChanged?.Invoke(this, EventArgs.Empty);
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
			var asm = new JocysCom.ClassLibrary.Configuration.AssemblyInfo();
			// Show only first time.
			if (showBalloonTip)
			{
				TrayNotifyIcon.BalloonTipText = asm.Product;
				// Show balloon tip for 2 seconds.
				TrayNotifyIcon.ShowBalloonTip(2, "X360CE", asm.Product, System.Windows.Forms.ToolTipIcon.Info);
			}
			// hold - program.
			// NOTE: also it would be possible to track which direction mouse will move in or move out on TrayIcon.
			// For example: open program if mouse moves in from left and moves out from top.
			TrayNotifyIcon.Text = "X360CE: Double click - program, click - menu.";
			if (minimizeToTray)
			{
				// Set window style as ToolWindow to avoid its icon in ALT+TAB.
				if (_Window.Owner != _hiddenForm)
					_Window.Owner = _hiddenForm;
				// Hide form bar from the TarkBar.
				if (_Window.ShowInTaskbar)
					_Window.ShowInTaskbar = false;
			}
			if (_Window.WindowState != WindowState.Minimized)
				_Window.WindowState = WindowState.Minimized;
		}

		/// <summary>
		/// Restores the window.
		/// </summary>
		public void RestoreFromTray(bool activate = false, bool maximize = false)
		{
			// Initialize main window.
			var w = new MainWindow();
			Global._MainWindow = w;
			Application.Current.MainWindow = w;
			// Finally show window.
			SetWindow(w);
			w.Show();
			// Closed will be executed first.
			w.Closed += (sender, e) =>
			{
				SetWindow(null);
				Application.Current.MainWindow = null;
			};
			// Unloaded will be executed after 'Closed' event.
			w.Unloaded += (sender, e) =>
			{
				// Global._MainWindow will be used by other controls to detach events,
				// therefore destroy reference by setting to null inside unloaded event.
				Global._MainWindow = null;
			};
			//if (activate)
			//{
			//	// Note: FormWindowState.Minimized and FormWindowState.Normal was used to make sure that Activate() wont fail because of this:
			//	// Windows NT 5.0 and later: An application cannot force a window to the foreground while the user is working with another window.
			//	// Instead, SetForegroundWindow will activate the window (see SetActiveWindow) and call theFlashWindowEx function to notify the user.
			//	if (_Window.WindowState != WindowState.Minimized)
			//		_Window.WindowState = WindowState.Minimized;
			//	_Window.Activate();
			//}
			//// Show in task bar before restoring windows state in order to prevent flickering.
			//if (!_Window.ShowInTaskbar)
			//	_Window.ShowInTaskbar = true;
			//var tagetState = maximize ? WindowState.Maximized : WindowState.Normal;

			//if (_Window.WindowState != tagetState)
			//	_Window.WindowState = tagetState;
			//// Set window style as ToolWindow to show in ALT+TAB.
			//if (_Window.Owner != null)
			//	_Window.Owner = null;
			//// Bring form to the front.
			//var tm = _Window.Topmost;
			//_Window.Topmost = true;
			//_Window.Topmost = tm;
			//_Window.BringIntoView();
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

		public void Dispose()
		{
			TrayNotifyIcon.Dispose();
		}

	}
}
