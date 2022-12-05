using System.Windows;
using x360ce.Engine;

namespace JocysCom.x360ce.RemoteController
{
	/// <summary>
	/// Interaction logic for OptionsWindow.xaml
	/// </summary>
	/// <remarks>Make sure to set the Owner property to be disposed properly after closing.</remarks>
	public partial class OptionsWindow : Window
	{
		public OptionsWindow()
		{
			InitializeComponent();
			Title = new JocysCom.ClassLibrary.Configuration.AssemblyInfo().GetTitle(false, false, false, false, false, 0) + "- Options";
			LoadSettings();
		}

		private void OkButton_Click(object sender, RoutedEventArgs e)
		{
			SaveSettings();
			DialogResult = true;
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
		}

		void LoadSettings()
		{
			RemoteHostTextBox.Text = Properties.Settings.Default.ComputerHost;
			RemotePortTextBox.Text = Properties.Settings.Default.ComputerPort.ToString();
			LoginUsernameTextBox.Text = Properties.Settings.Default.LoginUsername;
			LoginPasswordBox.Password = Properties.Settings.Default.LoginPassword;
			AutoConnectCheckBox.IsChecked = Properties.Settings.Default.AutoConnect;
			ConnectCheckBox.IsChecked = Properties.Settings.Default.Connect;
			var index = (MapToMask)Properties.Settings.Default.ControllerIndex;
			Controller1CheckBox.IsChecked = index.HasFlag(MapToMask.Controller1);
			Controller2CheckBox.IsChecked = index.HasFlag(MapToMask.Controller2);
			Controller3CheckBox.IsChecked = index.HasFlag(MapToMask.Controller3);
			Controller4CheckBox.IsChecked = index.HasFlag(MapToMask.Controller4);
		}

		void SaveSettings()
		{
			Properties.Settings.Default.ComputerHost = RemoteHostTextBox.Text;
			int port;
			Properties.Settings.Default.ComputerPort = int.TryParse(RemotePortTextBox.Text, out port)
				? port : 26010;
			Properties.Settings.Default.LoginUsername = LoginUsernameTextBox.Text;
			Properties.Settings.Default.LoginPassword = LoginPasswordBox.Password.ToString();
			Properties.Settings.Default.AutoConnect = AutoConnectCheckBox.IsChecked == true;
			Properties.Settings.Default.Connect = ConnectCheckBox.IsChecked == true;
			var index = MapToMask.None;
			if (Controller1CheckBox.IsChecked == true)
				index |= MapToMask.Controller1;
			if (Controller2CheckBox.IsChecked == true)
				index |= MapToMask.Controller2;
			if (Controller3CheckBox.IsChecked == true)
				index |= MapToMask.Controller2;
			if (Controller4CheckBox.IsChecked == true)
				index |= MapToMask.Controller4;
			Properties.Settings.Default.ControllerIndex = (int)index;
			Properties.Settings.Default.Save();
		}

	}
}
