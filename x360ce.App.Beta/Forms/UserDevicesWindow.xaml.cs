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
			InitHelper.InitTimer(this, InitializeComponent);
			WinControl.MainBody.Content = WinControl.MainContent;
			ControlsHelper.CheckTopMost(this);
			Title = "X360CE - Map Device To Controller";
			WinControl.InfoPanel.SetHead("Map Device To Controller");
			WinControl.InfoPanel.SetBodyInfo("Select Devices and press [Add Selected Devices] button.");
			WinControl.InfoPanel.SetImage(Application.Current.Resources[Icons_Default.Icon_gamepad]);
			WinControl.SetButton1("Add Selected Devices", Icons_Default.Icon_ok);
			WinControl.SetButton2("Cancel", Icons_Default.Icon_close);
			WinControl.SetButton3();
			MainControl.MapDeviceToControllerMode = true;
		}

		public Controls.UserDevicesControl MainControl
			=> (Controls.UserDevicesControl)WinControl.MainBody.Content;
	}
}
