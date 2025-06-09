using JocysCom.ClassLibrary.Controls;
using System.Windows.Controls;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for XInputUserControl.xaml
	/// </summary>
	public partial class PadItem_XInputControl : UserControl
	{
		public PadItem_XInputControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
		}

		private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowLoad(this))
				return;
		}

		private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowUnload(this))
				return;
		}
	}
}
