using JocysCom.ClassLibrary.Configuration;
using JocysCom.ClassLibrary.Controls;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for AboutUserControl.xaml
	/// </summary>
	public partial class AboutUserControl : UserControl
	{
		public AboutUserControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
		}

		private void HyperLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
		{
			OpenUrl(e.Uri.AbsoluteUri);
		}

		public void OpenUrl(string url)
		{
			try
			{
				System.Diagnostics.Process.Start(url);
			}
			catch (System.ComponentModel.Win32Exception noBrowser)
			{
				if (noBrowser.ErrorCode == -2147467259)
					MessageBox.Show(noBrowser.Message);
			}
			catch (System.Exception other)
			{
				MessageBox.Show(other.Message);
			}
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (ControlsHelper.IsDesignMode(this))
				return;
			var stream = EngineHelper.GetResourceStream("ChangeLog.txt");
			if (stream == null)
				return;
			var sr = new StreamReader(stream);
			ChangeLogTextBox.Text = sr.ReadToEnd();
			var ai = new AssemblyInfo();
			AboutProductLabel.Content = string.Format((string)AboutProductLabel.Content, ai.Version);
			// Load license.
			stream = EngineHelper.GetResourceStream("Documents.License.txt");
			sr = new StreamReader(stream);
			LicenseTextBox.Text = sr.ReadToEnd();
			LicenseTabPage.Header = string.Format("{0} {1} License", ai.Product, ai.Version.ToString(2));
			// Load Xceed License.
			stream = EngineHelper.GetResourceStream("Xceed.Wpf.Toolkit.License.txt");
			sr = new StreamReader(stream);
			XceedLicenseTextBox.Text = sr.ReadToEnd();
		}
	}
}
