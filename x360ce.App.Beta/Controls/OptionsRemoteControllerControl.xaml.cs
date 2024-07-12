using JocysCom.ClassLibrary.Controls;
using System;
using System.Windows.Controls;
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
			InitHelper.InitTimer(this, InitializeComponent);
			if (ControlsHelper.IsDesignMode(this))
				return;
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
			SettingsManager.OptionsData.Saving += OptionsData_Saving;
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

		private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowLoad(this))
				return;
		}

		private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowUnload(this))
				return;
			// Moved to MainBodyControl_Unloaded().
		}

		public void ParentWindow_Unloaded()
		{
			SettingsManager.UnLoadMonitor(RemoteEnabledCheckBox);
			SettingsManager.OptionsData.Saving -= OptionsData_Saving;
		}

	}
}
