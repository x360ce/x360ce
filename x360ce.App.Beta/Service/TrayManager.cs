using JocysCom.ClassLibrary.Controls;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using x360ce.App.Diagnostics;
using x360ce.App.Properties;

namespace x360ce.App.Service
{
	public partial class TrayManager : IDisposable
	{

		public static string TrayNotifyIconText = "x360ceTrayNotifyIcon";
		public static string TrayNotifyMenuText = "x360ceTrayNotifyMenu";

		public event EventHandler OnExitClick;
		public event EventHandler OnWindowSizeChanged;

		/// <summary>
		/// The secondary window must have the main window as owner in order to be disposed out correctly.
		/// </summary>
		public Window _Window;
		private WeakReference _WindowReference;
		private WeakReference _ContentReference;

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
				CollectGarbage();
				names += $"Window: {_WindowReference.IsAlive}, Control: {_ContentReference.IsAlive}";
				Clipboard.SetText(names);
				MessageBox.Show(names);
			};
			// Tray menu.
			TrayMenuStrip = new System.Windows.Forms.ContextMenuStrip();
			TrayMenuStrip.Text = TrayNotifyMenuText;
			TrayNotifyIcon = new System.Windows.Forms.NotifyIcon();
			TrayNotifyIcon.Text = TrayNotifyIconText;
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
			TrayNotifyIcon.Click += TrayNotifyIcon_Click;
			TrayNotifyIcon.DoubleClick += TrayNotifyIcon_DoubleClick;
		}

		public void SetWindow(Window window)
		{
			var o = SettingsManager.Options;
			// If old window exists then...
			if (_Window != null)
			{
				InfoForm.MonitorEnabled = false;
				_Window.SizeChanged -= MainWindow_SizeChanged;
				_Window.Closing -= _Window_Closing;
				o.PropertyChanged -= Options_PropertyChanged_Tray;
			}
			_Window = window;
			oldWindowState = null;
			if (window == null)
				return;
			_Window.SizeChanged += MainWindow_SizeChanged;
			_Window.Closing += _Window_Closing;
			_Window.Topmost = o.AlwaysOnTop;
			InfoForm.MonitorEnabled = o.EnableShowFormInfo;
			// Start monitoring event.
			o.PropertyChanged += Options_PropertyChanged_Tray;
		}

		private void _Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			var action = WindowLifecyclePolicy.DecideClose(
				Program.IsClosing, SettingsManager.Options.MinimizeOnClose);
			if (action == WindowCloseAction.MinimizeToTray)
			{
				e.Cancel = true;
				MinimizeToTray(false, SettingsManager.Options.MinimizeToTray);
				OperationalLog.Current?.Write("main_window_close_minimized");
				return;
			}

			var exitAlreadyRequested = Program.IsClosing;
			Program.IsClosing = true;
			if (TrayNotifyIcon != null)
				TrayNotifyIcon.Visible = false;
			OperationalLog.Current?.Write("main_window_close_exit");
			// The bootstrap window remains Application.MainWindow, so closing the
			// mapping window must explicitly stop the application. Queue shutdown to
			// avoid re-entering WPF's current Closing event.
			if (!exitAlreadyRequested)
				Application.Current.Dispatcher.BeginInvoke(
					new Action(() => Application.Current.Shutdown()));
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
					UpdateWindowsStart(o.StartWithWindows, o.StartWithWindowsState);
					break;
				case nameof(Options.EnableShowFormInfo):
					InfoForm.MonitorEnabled = o.EnableShowFormInfo;
					break;
				default:
					break;
			}
		}

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
			if (_Window != null)
			{
				if (_Window.WindowState != WindowState.Minimized)
					_Window.WindowState = WindowState.Minimized;
				if (minimizeToTray)
				{
					// Hide() reliably removes the WPF window from Alt-Tab and the
					// taskbar. ShowInTaskbar alone can leave a stale taskbar button.
					_Window.ShowInTaskbar = false;
					_Window.Hide();
				}
				else
				{
					_Window.ShowInTaskbar = true;
				}
				OperationalLog.Current?.Write("main_window_minimized", fields:
					new System.Collections.Generic.Dictionary<string, object>
					{
						["hiddenFromTaskbar"] = minimizeToTray,
					});
			}
		}

		public void RestoreFromTray(bool activate = false, bool maximize = false)
		{
			_RestoreFromTray(activate, maximize);
		}

		public event EventHandler MainWindowShown;

		/// <summary>
		/// Restores the window.
		/// </summary>
		public void _RestoreFromTray(bool activate = false, bool maximize = false)
		{
			// Need isolator or app freeze.
			Action isolator = () =>
			{
				if (_Window == null)
				{
					_WindowReference = new WeakReference(null);
					_ContentReference = new WeakReference(null);
					MainWindow mw;
					using (OperationalLog.Current?.Measure("main_window_construct"))
						mw = new MainWindow();
					Global._MainWindow = mw;
					mw.Owner = Application.Current.MainWindow;
					_WindowReference.Target = mw;
					_ContentReference.Target = mw.Content;
					mw.Loaded += (sender, e) => OperationalLog.Current?.Write("main_window_loaded");
					mw.Closed += (sender, e) => SetWindow(null);
					mw.Unloaded += (sender, e) => Global._MainWindow = null;
					SetWindow(mw);
				}

				using (OperationalLog.Current?.Measure("main_window_show"))
					if (!_Window.IsVisible)
						_Window.Show();
				// Detach the bootstrap owner after the first Show so hiding the
				// bootstrap cannot hide the mapping window as a side effect.
				_Window.Owner = null;
				if (activate)
				{
					// Note: FormWindowState.Minimized and FormWindowState.Normal was used to make sure that Activate() wont fail because of this:
					// Windows NT 5.0 and later: An application cannot force a window to the foreground while the user is working with another window.
					// Instead, SetForegroundWindow will activate the window (see SetActiveWindow) and call theFlashWindowEx function to notify the user.
					if (_Window.WindowState != WindowState.Minimized)
						_Window.WindowState = WindowState.Minimized;
					_Window.Activate();
				}
				// Show in task bar before restoring windows state in order to prevent flickering.
				if (!_Window.ShowInTaskbar)
					_Window.ShowInTaskbar = true;
				// Update window state.
				var tagetState = maximize ? WindowState.Maximized : WindowState.Normal;
				if (_Window.WindowState != tagetState)
					_Window.WindowState = tagetState;
				// Apply visibility-dependent update policy after Show(), including the
				// first window where no SizeChanged event is otherwise guaranteed.
				MainWindow_SizeChanged(this, null);
				// Bring form to the front.
				var tm = _Window.Topmost;
				_Window.Topmost = true;
				_Window.Topmost = tm;
				_Window.BringIntoView();
				OperationalLog.Current?.Write("main_window_shown");
				MainWindowShown?.Invoke(this, EventArgs.Empty);
			};
			Application.Current.Dispatcher.BeginInvoke(isolator);
		}

		private void Tw_Unloaded(object sender, RoutedEventArgs e)
		{
			throw new NotImplementedException();
		}

		private static void _CollectGarbage()
		{
			// Try to remove object from the memory.
			GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
			//GC.Collect();
			GC.WaitForFullGCComplete();
			GC.WaitForPendingFinalizers();
		}

		public static void CollectGarbage(Func<bool> whileCondition = null)
		{
			if (whileCondition == null)
			{
				_CollectGarbage();
				return;
			}
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			// loop untill object allive, but no longer than  seconds.
			while (whileCondition() && stopwatch.ElapsedMilliseconds < 4000)
			{
				Task.Delay(200).Wait();
				_CollectGarbage();
			}
		}

		#endregion

		#region ■ Operation 

		public void UpdateWindowsStart(bool enabled, System.Windows.Forms.FormWindowState? startState = null)
		{
			if (enabled)
			{
				// Pick one only.
				UpdateWindowsStartRegistry(enabled, startState);
				//UpdateWindowsStartFolder(enabled, startState);
			}
			else
			{
				UpdateWindowsStartRegistry(enabled, startState);
				UpdateWindowsStartFolder(enabled, startState);
			}
		}

		/// <summary>
		/// Enable or disable application start with Windows after sign-in.
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
				// Fix possible issues, dot notations and invalid path separator.
				var fullPath = Path.GetFullPath(ai.AssemblyPath);
				// Add the value in the registry so that the application runs at start-up
				var command = string.Format("\"{0}\" /{1}={2}", fullPath, Program.arg_WindowState, startState.ToString());
				var value = (string)runKey.GetValue(ai.Product);
				if (value != command)
					runKey.SetValue(ai.Product, command);
			}
			else
			{
				// Remove the value from the registry so that the application doesn't start automatically.
				if (runKey.GetValueNames().Contains(ai.Product))
					runKey.DeleteValue(ai.Product, false);
			}
			runKey.Close();
		}

		/// <summary>
		/// Enable or disable application start with Windows after sign-in
		/// Requires no special permissions, because current used have full access to CurrentUser 'Startup' folder.
		/// </summary>
		/// <param name="enabled">Start with Windows after sign-in.</param>
		/// <param name="startState">Start Mode.</param>
		public void UpdateWindowsStartFolder(bool enabled, System.Windows.Forms.FormWindowState? startState = null)
		{
			var ai = new JocysCom.ClassLibrary.Configuration.AssemblyInfo();
			startState = startState ?? SettingsManager.Options.StartWithWindowsState;
			var startupFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Startup);
			var shortcutPath = $"{startupFolder}\\{ai.Product}.lnk";
			if (enabled)
			{
				// Fix possible issues, dot notations and invalid path separator.
				var targetPath = Path.GetFullPath(ai.AssemblyPath);
				// Add the value in the registry so that the application runs at start-up
				//var arguments = $"/{Program.arg_WindowState}={startState}";
				var windowsStyle = 1; // Normal
				if (startState == System.Windows.Forms.FormWindowState.Maximized)
					windowsStyle = 3;
				if (startState == System.Windows.Forms.FormWindowState.Minimized)
					windowsStyle = 7;
				string powershellCommand = "-NoProfile -Command " +
					$"$wShell = New-Object -ComObject WScript.Shell; " +
					$"$shortcut = $wShell.CreateShortcut('{shortcutPath}'); " +
					$"$shortcut.TargetPath = '\"{targetPath}\"'; " +
					$"$shortcut.WindowStyle = '{windowsStyle}'; " +
					//$"$shortcut.Arguments = '{arguments}'; " +
					$"$shortcut.Save();";
				using (var process = new Process())
				{
					process.StartInfo.UseShellExecute = true;
					process.StartInfo.FileName = "powershell";
					process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
					process.StartInfo.Arguments = powershellCommand;
					process.Start();
				}
			}
			else
			{
				// Remove shortcut so that the application doesn't start automatically.
				if (File.Exists(shortcutPath))
					File.Delete(shortcutPath);
			}
		}

		#endregion

		#region ■ IDisposable

		public bool IsDisposing;
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		// The bulk of the clean-up code is implemented in Dispose(bool)
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				IsDisposing = true;
                if (TrayNotifyIcon != null) TrayNotifyIcon.Visible = false;
                TrayNotifyIcon?.Dispose();
				TrayMenuStrip?.Dispose();
				OpenApplicationMenu?.Dispose();
				ExitMenu?.Dispose();
				ShowLoadedControls?.Dispose();
			}
		}

		#endregion


	}
}
