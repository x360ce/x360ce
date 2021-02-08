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
using x360ce.Engine;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for OptionsRemoteControllerControl.xaml
	/// </summary>
	public partial class OptionsRemoteControllerControl : UserControl
	{
		public OptionsRemoteControllerControl()
		{
			InitializeComponent();
			LoadSettings();
			
		}

		void LoadSettings()
		{
			var o = SettingsManager.Options;
			SettingsManager.LoadAndMonitor(o, nameof(o.RemoteEnabled), RemoteEnabledCheckBox);
			AllowRemote1CheckBox.IsChecked = o.RemoteControllers.HasFlag(MapToMask.Controller1);
			AllowRemote2CheckBox.IsChecked = o.RemoteControllers.HasFlag(MapToMask.Controller2);
			AllowRemote3CheckBox.IsChecked = o.RemoteControllers.HasFlag(MapToMask.Controller3);
			AllowRemote4CheckBox.IsChecked = o.RemoteControllers.HasFlag(MapToMask.Controller4);
			RemotePasswordTextBox.Text = o.RemotePassword;
			if (o.RemotePort >= RemotePortNumericUpDown.Minimum && o.RemotePort <= RemotePortNumericUpDown.Maximum)
				RemotePortNumericUpDown.Value = o.RemotePort;
			// Attach event which will save form settings before Save().
			SettingsManager.OptionsData.Saving += OptionsData_Saving; ;
		}

		private void OptionsData_Saving(object sender, EventArgs e)
		{
			var o = SettingsManager.Options;
			var remoteControllers = MapToMask.None;
			remoteControllers |= AllowRemote1CheckBox.IsChecked ?? false ? MapToMask.Controller1 : MapToMask.None;
			remoteControllers |= AllowRemote2CheckBox.IsChecked ?? false ? MapToMask.Controller2 : MapToMask.None;
			remoteControllers |= AllowRemote3CheckBox.IsChecked ?? false ? MapToMask.Controller3 : MapToMask.None;
			remoteControllers |= AllowRemote4CheckBox.IsChecked ?? false ? MapToMask.Controller4 : MapToMask.None;
			o.RemoteControllers = remoteControllers;
			o.RemotePassword = RemotePasswordTextBox.Text;
			o.RemotePort = (int)RemotePortNumericUpDown.Value;
		}
	}
}
