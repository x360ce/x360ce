using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace x360ce.App.Forms
{
	public partial class DebugForm : Form
	{
		public DebugForm()
		{
			InitializeComponent();
			if (IsDesignMode) return;
		}

		public bool IsDesignMode { get { return JocysCom.ClassLibrary.Controls.ControlsHelper.IsDesignMode(this); } }

		private void DebugForm_Load(object sender, EventArgs e)
		{
			if (IsDesignMode) return;
		}

		#region Show/Hide Panel

		object PanelLock = new object();
		public bool IsVisible = false;

		public void ShowPanel()
		{
			lock (PanelLock)
			{
				if (!IsVisible)
				{
					IsVisible = true;
					SettingsManager.Options.ShowDebugPanel = true;
					var wa = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
					Show();
				}
				Left = MainForm.Current.Left + MainForm.Current.Width + 8;
				Top = MainForm.Current.Top;
			}
		}

		public void HidePanel()
		{
			lock (PanelLock)
			{
				if (!IsVisible)
					return;
				IsVisible = false;
				SettingsManager.Options.ShowDebugPanel = false;
				Properties.Settings.Default.Save();
				Hide();
			}
		}

		private void DebugForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (!Program.IsClosing)
			{
				// Hide debug form instead.
				e.Cancel = true;
				HidePanel();
			}
		}

		#endregion

	}
}
