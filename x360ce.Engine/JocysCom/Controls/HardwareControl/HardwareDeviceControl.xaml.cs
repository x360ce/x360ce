using System.Windows.Controls;

namespace JocysCom.ClassLibrary.Controls
{
	/// <summary>
	/// Interaction logic for HardwareDeviceControl.xaml
	/// </summary>
	public partial class HardwareDeviceControl : UserControl
	{
		public HardwareDeviceControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
		}
	}
}
