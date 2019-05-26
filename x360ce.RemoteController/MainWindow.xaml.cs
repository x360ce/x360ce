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

namespace JocysCom.x360ce.RemoteController
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			Title = new JocysCom.ClassLibrary.Configuration.AssemblyInfo().GetTitle();
		}

		private void OptionsButton_Click(object sender, RoutedEventArgs e)
		{
			var win = new OptionsWindow();
			win.ShowDialog();
		}
	}
}
