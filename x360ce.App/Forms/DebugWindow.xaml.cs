using JocysCom.ClassLibrary.Controls;
using System;
using System.Windows;

namespace x360ce.App.Forms
{
	/// <summary>
	/// Interaction logic for DebugWindow.xaml
	/// </summary>
	/// <remarks>Make sure to set the Owner property to be disposed properly after closing.</remarks>
	public partial class DebugWindow : Window
	{
		public DebugWindow()
		{
			InitHelper.InitTimer(this, InitializeComponent);
		}

		private void DebugForm_Load(object sender, EventArgs e)
		{
		}

		#region ■ Show/Hide Panel

		object PanelLock = new object();
		public bool IsFormVisible = false;

		public void ShowPanel()
		{
			lock (PanelLock)
			{
				if (!IsFormVisible)
				{
					IsFormVisible = true;
					SettingsManager.Options.ShowDebugPanel = true;
					var wa = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
					Show();
				}
				Left = Global._MainWindow.Left + Global._MainWindow.Width + 8;
				Top = Global._MainWindow.Top;
				// Bring to front.
				Activate();
			}
		}

		public void HidePanel()
		{
			lock (PanelLock)
			{
				if (!IsFormVisible)
					return;
				IsFormVisible = false;
				SettingsManager.Options.ShowDebugPanel = false;
				Properties.Settings.Default.Save();
				Hide();
			}
		}

		#endregion

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (!Program.IsClosing)
			{
				// Hide debug form instead.
				e.Cancel = true;
				HidePanel();
			}
		}
	}
}
