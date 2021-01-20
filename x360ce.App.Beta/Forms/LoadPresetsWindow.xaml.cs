using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
			WinControl.MainBody.Content = WinControl.MainContent;
			WinControl.SetTitle("X360CE - Load Preset");
			WinControl.SetHead("Load Preset");
			WinControl.SetBodyInfo("Select Preset and press [Load Selected] button.");
			WinControl.SetImage(Icons_Default.Icon_gamepad);
			WinControl.SetButton1("Load Selected", Icons_Default.Icon_ok);
			WinControl.SetButton2("Cancel", Icons_Default.Icon_close);
			WinControl.SetButton3();
		}
	}
}
