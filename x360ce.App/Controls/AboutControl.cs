using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	public partial class AboutControl : UserControl
	{
		public AboutControl()
		{
			InitializeComponent();
		}

		void LinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			EngineHelper.OpenUrl(((Control)sender).Text);
		}

		void AboutControl_Load(object sender, EventArgs e)
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
