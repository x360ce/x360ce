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
			ChangeLogTextBox.Text = ControlsHelper.NormalizeLineBreaks(sr.ReadToEnd());
			// The program's own name and version: wherever it is hosted, as in a test, the process's product can be another's.
			var program = new JocysCom.ClassLibrary.Configuration.AssemblyInfo(typeof(AboutControl).Assembly);
			var version = program.Version;
			AboutProductLabel.Text = string.Format(AboutProductLabel.Text, version);
			stream = EngineHelper.GetResourceStream("Documents.License.txt");
			sr = new StreamReader(stream);
			// What x360ce carries from other projects is shown under its own licence, as each of those licences asks.
			LicenseTextBox.Text = ControlsHelper.NormalizeLineBreaks(sr.ReadToEnd() + ThirdPartyLicences.Text(program.Assembly));
			LicenseTabPage.Text = string.Format("{0} {1} License", program.Product, version.ToString(2));
		}

	}
}
