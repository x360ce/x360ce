using JocysCom.ClassLibrary.Controls;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using x360ce.App.DInput;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for HidGuardianWarningUserControl.xaml
	/// </summary>
	public partial class HidGuardianWarningUserControl : UserControl
	{
		public HidGuardianWarningUserControl()
		{
			InitializeComponent();
			ProgramFolder.Content = VirtualDriverInstaller.GetHidGuardianPath();
		}

		private void Hyperlink_Click(object sender, RoutedEventArgs e)
		{
			ControlsHelper.OpenUrl(((HyperLink)sender).NavigateUrl);
		}

		private void ProgramFolderTextBox_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			ControlsHelper.OpenPath((string)ProgramFolder.Content);
		}
	}
}
