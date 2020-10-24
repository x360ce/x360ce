using JocysCom.ClassLibrary.Controls;
using System;
using System.IO;
using System.Windows.Forms;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	public partial class AboutControl : UserControl
	{
		public AboutControl()
		{
			InitializeComponent();
		}

		private void LinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			ControlsHelper.OpenUrl(((Control)sender).Text);
		}

		private void AboutControl_Load(object sender, EventArgs e)
		{
			var stream = EngineHelper.GetResourceStream("ChangeLog.txt");
			var sr = new StreamReader(stream);
			ChangeLogTextBox.Text = sr.ReadToEnd();
			AboutProductLabel.Text = string.Format(AboutProductLabel.Text, Application.ProductVersion);
			stream = EngineHelper.GetResourceStream("License.txt");
			sr = new StreamReader(stream);
			LicenseTextBox.Text = sr.ReadToEnd();
			LicenseTabPage.Text = string.Format("{0} {1} License", Application.ProductName, new Version(Application.ProductVersion).ToString(2));
		}

	}
}
