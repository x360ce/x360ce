using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace x360ce.App.Forms
{
	public partial class DeveloperToolsForm : Form
	{
		public DeveloperToolsForm()
		{
			InitializeComponent();
			if (IsDesignMode) return;
			FindResourcesFolder();
		}

		public void FindResourcesFolder()
		{
			var dir = new FileInfo(Application.ExecutablePath).Directory;
			DirectoryInfo pdir = dir;
			DirectoryInfo rdir = null;
			while (pdir.Parent != null)
			{
				rdir = pdir.GetDirectories().FirstOrDefault(x => string.Compare(x.Name, "resources", true) == 0);
				if (rdir != null)
					break;
				pdir = pdir.Parent;
			}
			WorkingFolderTextBox.Text = rdir == null ? dir.FullName : rdir.FullName;
		}

		public bool IsDesignMode { get { return JocysCom.ClassLibrary.Controls.ControlsHelper.IsDesignMode(this); } }

		private void DeveloperToolsForm_Load(object sender, EventArgs e)
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
					Show();
				}
				Left = MainForm.Current.Left + (MainForm.Current.Width - Width) / 2;
				Top = MainForm.Current.Top + (MainForm.Current.Height - Height) / 2;
				BringToFront();
			}
		}

		public void HidePanel()
		{
			lock (PanelLock)
			{
				if (!IsVisible)
					return;
				IsVisible = false;
				Hide();
			}
		}

		private void DeveloperToolsForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (!Program.IsClosing)
			{
				// Hide form instead.
				e.Cancel = true;
				HidePanel();
			}
		}

		#endregion

		private void CompressXmlResourcesButton_Click(object sender, EventArgs e)
		{

		}
	}
}
