using JocysCom.ClassLibrary.Controls;
using System.Windows;

namespace x360ce.App.Forms
{
	/// <summary>
	/// Interaction logic for LoadPresetsWindow.xaml
	/// </summary>
	public partial class LoadPresetsWindow : Window
	{
		public LoadPresetsWindow()
		{
			InitializeComponent();
			ControlsHelper.CheckTopMost(this);
			WinControl.MainBody.Content = WinControl.MainContent;
			WinControl.SetTitle("X360CE - Load Preset");
			WinControl.SetHead("Load Preset");
			WinControl.SetBodyInfo("Select Preset and press [Load Selected] button.");
			WinControl.SetImage(Icons_Default.Icon_gamepad);
			WinControl.SetButton1("Load Selected", Icons_Default.Icon_ok);
			WinControl.SetButton2("Cancel", Icons_Default.Icon_close);
			WinControl.SetButton3();
			MainControl._ParentControl = WinControl;
		}

		public Controls.PresetsControl MainControl
		=> (Controls.PresetsControl)WinControl.MainBody.Content;

	}
}
