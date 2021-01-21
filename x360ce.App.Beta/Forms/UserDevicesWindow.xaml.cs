using JocysCom.ClassLibrary.Controls;
using System.Windows;

namespace x360ce.App.Forms
{
	/// <summary>
	/// Interaction logic for UserDevicesWindow.xaml
	/// </summary>
	public partial class UserDevicesWindow : Window
	{
		public UserDevicesWindow()
		{
			InitializeComponent();
			WinControl.MainBody.Content = WinControl.MainContent;
			ControlsHelper.CheckTopMost(this);
			WinControl.SetTitle("X360CE - Map Device To Controller");
			WinControl.SetHead("Map Device To Controller");
			WinControl.SetBodyInfo("Select Devices and press [Add Selected Devices] button.");
			WinControl.SetImage(Icons_Default.Icon_gamepad);
			WinControl.SetButton1("Add Selected Devices", Icons_Default.Icon_ok);
			WinControl.SetButton2("Cancel", Icons_Default.Icon_close);
			WinControl.SetButton3();
			MainControl.MapDeviceToControllerMode = true;
		}

		public Controls.UserDevicesControl MainControl
			=> (Controls.UserDevicesControl)WinControl.MainBody.Content;
	}
}
