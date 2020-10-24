using JocysCom.ClassLibrary.Controls;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;

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
		}

		private void Hyperlink_Click(object sender, RoutedEventArgs e)
		{
			ControlsHelper.OpenUrl(((HyperLink)sender).NavigateUrl);
		}
	}
}
