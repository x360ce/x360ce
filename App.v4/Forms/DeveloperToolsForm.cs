using JocysCom.ClassLibrary.Configuration;
using JocysCom.ClassLibrary.Controls;
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
			var di = new DirectoryInfo(WorkingFolderTextBox.Text);
			var fis = di.GetFiles("*.xml");
			foreach (var fi in fis)
			{
				var bytes = File.ReadAllBytes(fi.FullName);
				bytes = SettingsHelper.Compress(bytes);
				SettingsHelper.WriteIfDifferent(fi.FullName + ".gz", bytes);
				LogTextBox.AppendText(string.Format("Compress {0}\r\n", fi.Name));
			}
		}

		#region Test Windows

		// Each button shows a window the program shows only when something happens, with the same kind of text
		// and buttons, so its look can be checked in either theme and at any screen zoom. The answer goes to the log.

		void WriteAnswer(string window, DialogResult answer)
		{
			LogTextBox.AppendText(string.Format("{0}: {1}\r\n", window, answer));
		}

		private void InformationButton_Click(object sender, EventArgs e)
		{
			var answer = MessageBoxForm.Show(
				"Settings were saved.\r\n\r\n" +
				"This test shows an information message: one button, and text long enough to wrap beside the icon.",
				"Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
			WriteAnswer("Information", answer);
		}

		private void WarningButton_Click(object sender, EventArgs e)
		{
			using (var form = new MessageBoxForm { StartPosition = FormStartPosition.CenterParent })
			{
				var answer = form.ShowForm(
					"Settings could not be saved to:\r\nC:\\ProgramData\\X360CE\\Settings\r\n\r\n" +
					"This test shows a warning with three buttons named for what they do.",
					"Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1,
					new[] { "&Try again", "&Use my own folder", "&Not now" });
				WriteAnswer("Warning", answer);
			}
		}

		private void ErrorButton_Click(object sender, EventArgs e)
		{
			using (var form = new MessageBoxForm { StartPosition = FormStartPosition.CenterParent })
			{
				var answer = form.ShowForm(
					"Do you want to clear all errors?\r\n\r\n" +
					"This test shows an error message with the second button as the default.",
					"Error", MessageBoxButtons.YesNo, MessageBoxIcon.Error, MessageBoxDefaultButton.Button2);
				WriteAnswer("Error", answer);
			}
		}

		private void QuestionButton_Click(object sender, EventArgs e)
		{
			using (var form = new MessageBoxForm { StartPosition = FormStartPosition.CenterParent })
			{
				var answer = form.ShowForm(
					"The device is already on Controller 1.\r\n\r\n" +
					"This test shows a question with a heading and buttons named for what they do.",
					"Question", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1,
					new[] { "&Move", "&Copy" }, "Move the device to Controller 2, or copy it there?");
				WriteAnswer("Question", answer);
			}
		}

		private void ExceptionButton_Click(object sender, EventArgs e)
		{
			LogTextBox.AppendText("Exception: thrown; the program records it and counts it in the status bar.\r\n");
			// Left uncaught on the interface thread, so it takes the path every unexpected error there takes.
			throw new InvalidOperationException("Test exception thrown from Developer Tools.",
				new ArgumentException("Test inner exception."));
		}

		private void ErrorReportButton_Click(object sender, EventArgs e)
		{
			MainForm.Current.ShowErrorReport();
		}

		#endregion
	}
}
