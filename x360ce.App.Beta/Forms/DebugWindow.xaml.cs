using System;
using System.Windows;

namespace x360ce.App.Forms
{
	/// <summary>
	/// Interaction logic for DebugWindow.xaml
	/// </summary>
	public partial class DebugWindow : Window
	{
		public DebugWindow()
		{
			InitializeComponent();
		}

		private void DebugForm_Load(object sender, EventArgs e)
		{
		}

		#region Show/Hide Panel

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
				Left = MainForm.Current.Left + MainForm.Current.Width + 8;
				Top = MainForm.Current.Top;
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
