using JocysCom.ClassLibrary.Controls;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for PresetsControl.xaml
	/// </summary>
	public partial class PresetsControl : UserControl
	{
		public PresetsControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
		}

		public BaseWithHeaderControl _ParentControl;

		public void InitForm()
		{
			//_ParentForm.SetImage(SetHeaderBody(MessageBoxIcon.None);
			SettingsGridPanel._ParentControl = _ParentControl;
			SummariesGridPanel._ParentControl = _ParentControl;
			PresetsGridPanel._ParentControl = _ParentControl;
			SettingsGridPanel.InitPanel();
			SummariesGridPanel.InitPanel();
			PresetsGridPanel.InitPanel();
			UpdateControls();
			_ParentControl.Button1.Click += OkButton_Click;
		}

		private void OkButton_Click(object sender, RoutedEventArgs e)
		{
			Guid? checksum = null;
			if (MainTabControl.SelectedItem == PresetsTabPage)
			{
				var preset = PresetsGridPanel.MainDataGrid.SelectedItems.Cast<Preset>().FirstOrDefault();
				if (preset != null)
					checksum = preset.PadSettingChecksum;
			}
			if (MainTabControl.SelectedItem == SummariesTabPage)
			{
				var summary = SummariesGridPanel.MainDataGrid.SelectedItems.Cast<Summary>().FirstOrDefault();
				if (summary != null)
					checksum = summary.PadSettingChecksum;
			}
			if (MainTabControl.SelectedItem == SettingsTabPage)
			{
				var setting = SettingsGridPanel.MainDataGrid.SelectedItems.Cast<UserSetting>().FirstOrDefault();
				if (setting != null)
					checksum = setting.PadSettingChecksum;
			}
			if (checksum.HasValue)
			{
				SelectedItem = SettingsManager.PadSettings.Items.FirstOrDefault(x => x.PadSettingChecksum == checksum.Value);
			}
		}

		public void UnInitForm()
		{
			SettingsGridPanel.UnInitPanel();
			SummariesGridPanel.UnInitPanel();
			PresetsGridPanel.UnInitPanel();
		}

		public PadSetting SelectedItem;


		private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			UpdateControls();
		}

		private void UpdateControls()
		{
			var selected = false;
			if (MainTabControl.SelectedItem == SummariesTabPage)
				selected = SummariesGridPanel.MainDataGrid.SelectedItems.Count > 0;
			if (MainTabControl.SelectedItem == SettingsTabPage)
				selected = SettingsGridPanel.MainDataGrid.SelectedItems.Count > 0;
			if (MainTabControl.SelectedItem == PresetsTabPage)
				selected = PresetsGridPanel.MainDataGrid.SelectedItems.Count > 0;
			if (_ParentControl != null)
			{
				var tab = MainTabControl.SelectedItem as TabItem;
				if (tab != null)
					_ParentControl.InfoPanel.SetHead(tab.Header as string);

				ControlsHelper.SetEnabled(_ParentControl.Button1, selected);
			}
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowLoad(this))
				return;
		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowUnload(this))
				return;
			// Moved to MainBodyControl_Unloaded().
		}

		public void ParentWindow_Unloaded()
		{
			SettingsGridPanel._ParentControl = null;
			SummariesGridPanel._ParentControl = null;
			PresetsGridPanel._ParentControl = null;
			SelectedItem = null;
		}
	}
}
