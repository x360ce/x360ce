using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Runtime;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for PadFootControl.xaml
	/// </summary>
	public partial class PadFootControl : UserControl
	{
		public PadFootControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
		}

		UserDevice _UserDevice;
		PadSetting _PadSetting;
		MapTo _MappedTo;

        public void InitPadData()
        {
            ControlsHelper.SetItemsSource(MapNameComboBox, SettingsManager.Layouts.Items);
            //MapNameComboBox.ItemsSource = LayoutsItems;
            //MapNameComboBox.DisplayMemberPath = "Name";
            if (SettingsManager.Layouts.Items.Count > 0)
                MapNameComboBox.SelectedIndex = 0;
        }

        public void SetBinding(MapTo mappedTo, UserDevice ud, PadSetting ps)
		{
			_MappedTo = mappedTo;
			_UserDevice = ud;
			_PadSetting = ps;
			var en = ps != null;
			ControlsHelper.SetEnabled(CopyButton, en);
			ControlsHelper.SetEnabled(PasteButton, en);
			ControlsHelper.SetEnabled(LoadButton, en);
			ControlsHelper.SetEnabled(AutoButton, en);
			ControlsHelper.SetEnabled(ClearButton, en);
			ControlsHelper.SetEnabled(ResetButton, en);
		}

		private void GameControllersButton_Click(object sender, RoutedEventArgs e)
		{
			var path = System.Environment.GetFolderPath(Environment.SpecialFolder.System);
			path += "\\joy.cpl";
			ControlsHelper.OpenPath(path);
		}

		private void DxTweakButton_Click(object sender, RoutedEventArgs e)
		{
			FileInfo fi;
			var error = EngineHelper.ExtractFile("DXTweak2.exe", out fi);
			if (error != null)
			{
				MessageBox.Show(error.Message);
				return;
			}
			ControlsHelper.OpenPath(fi.FullName);
		}

		private void CopyButton_Click(object sender, RoutedEventArgs e)
		{
			var text = Serializer.SerializeToXmlString(_PadSetting, null, true);
			Clipboard.SetText(text);
		}

		private void PasteButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var xml = Clipboard.GetText();
				var ps = JocysCom.ClassLibrary.Runtime.Serializer.DeserializeFromXmlString<PadSetting>(xml);
				_PadSetting.Load(ps);
			}
			catch (Exception ex)
			{
				var form = new MessageBoxWindow();
				ControlsHelper.CheckTopMost(form);
				form.ShowDialog(ex.Message);
				return;
			}
		}

		private void LoadButton_Click(object sender, RoutedEventArgs e)
		{
			var form = new Forms.LoadPresetsWindow();
			form.Width = 800;
			form.Height = 400;
			ControlsHelper.CheckTopMost(form);
			form.MainControl.InitForm();
			var result = form.ShowDialog();
			if (result == true)
			{
				var ps = form.MainControl.SelectedItem;
				if (ps != null)
				{
					Global._MainWindow.UpdateTimer.Stop();
					_PadSetting.Load(ps);
					Global._MainWindow.UpdateTimer.Start();
				}
			}
			form.MainControl.UnInitForm();
		}

		private void AutoButton_Click(object sender, RoutedEventArgs e)
		{
			var ud = _UserDevice;
			if (ud == null)
				return;
			var description = Attributes.GetDescription(_MappedTo);
			var form = new MessageBoxWindow();
			var buttons = MessageBoxButton.YesNo;
			var text = string.Format("Do you want to Fill {0} settings automatically?", description);
			if (ud.Device == null && !TestDeviceHelper.ProductGuid.Equals(ud.ProductGuid))
			{
				text = string.Format("Device is off-line. Please connect device to Fill {0} settings automatically.", description);
				buttons = MessageBoxButton.OK;
			}
			var result = form.ShowDialog(text, "Fill Controller Settings", buttons, MessageBoxImage.Question);
			if (result != MessageBoxResult.Yes)
				return;
			var ps = AutoMapHelper.GetAutoPreset(ud);
			_PadSetting.Load(ps);
		}

		private void ClearButton_Click(object sender, RoutedEventArgs e)
		{
			var description = Attributes.GetDescription(_MappedTo);
			var text = string.Format("Do you want to clear all {0} settings?", description);
			var form = new MessageBoxWindow();
			var result = form.ShowDialog(text, "Clear Controller Settings", MessageBoxButton.YesNo, MessageBoxImage.Question);
			if (result != MessageBoxResult.Yes)
				return;
			var ps = new PadSetting();
			_PadSetting.Load(ps);
		}

		private void ResetButton_Click(object sender, RoutedEventArgs e)
		{
			var description = Attributes.GetDescription(_MappedTo);
			var text = string.Format("Do you want to reset all {0} settings?", description);
			var form = new MessageBoxWindow();
			var result = form.ShowDialog(text, "Reset Controller Settings", MessageBoxButton.YesNo, MessageBoxImage.Question);
			if (result != MessageBoxResult.Yes)
				return;
			//MainForm.Current.ReloadXinputSettings();
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
			_UserDevice = null;
			_PadSetting = null;
			MapNameComboBox.ItemsSource = null;
			MapNameComboBox.SelectedItem = null;
			ControlsHelper.SetItemsSource(MapNameComboBox, null);
		}
	}
}
