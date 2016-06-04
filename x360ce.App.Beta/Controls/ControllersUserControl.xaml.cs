using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace x360ce.App
{
	/// <summary>
	/// Interaction logic for ControllersUserControl.xaml
	/// </summary>
	public partial class ControllersUserControl : UserControl
	{
		public ControllersUserControl()
		{
			InitializeComponent();
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			ControllersDataGrid.ItemsSource = SettingManager.DiDevices;
		}

	}
}
