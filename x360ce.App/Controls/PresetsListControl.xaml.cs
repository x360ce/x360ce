using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Web.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for PresetsListUserControl.xaml
	/// </summary>
	public partial class PresetsListControl : UserControl
	{
		public PresetsListControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			MainDataGrid.AutoGenerateColumns = false;
			MainDataGrid.SelectionMode = System.Windows.Controls.DataGridSelectionMode.Single;
			if (ControlsHelper.IsDesignMode(this))
				return;
		}

		public BaseWithHeaderControl _ParentControl;

		public void InitPanel()
		{
			SettingsManager.Presets.Items.ListChanged += Presets_ListChanged;
			MainDataGrid.ItemsSource = SettingsManager.Presets.Items;
			UpdateControlsFromPresets();
		}

		public void UnInitPanel()
		{
			SettingsManager.Presets.Items.ListChanged -= Presets_ListChanged;
			MainDataGrid.ItemsSource = null;
		}

		void UpdateControlsFromPresets()
		{
		}

		void Presets_ListChanged(object sender, ListChangedEventArgs e)
		{
			UpdateControlsFromPresets();
		}

		public void RefreshData()
		{
			_ParentControl.InfoPanel.AddTask(TaskName.SearchPresets);
			RefreshButton.IsEnabled = false;
			var sp = new List<SearchParameter>();
			sp.Add(new SearchParameter());
			var ws = new WebServiceClient();
			ws.Url = SettingsManager.Options.InternetDatabaseUrl;
			ws.SearchSettingsCompleted += wsPresets_SearchSettingsCompleted;
			// Make sure it starts completely on a separate thread.
			System.Threading.ThreadPool.QueueUserWorkItem(delegate (object state)
			{
				ws.SearchSettingsAsync(sp.ToArray());
			});
		}

		void wsPresets_SearchSettingsCompleted(object sender, SoapHttpClientEventArgs e)
		{
			// Make sure method is executed on the same thread as this control.
			if (ControlsHelper.InvokeRequired)
			{
				var method = new EventHandler<SoapHttpClientEventArgs>(wsPresets_SearchSettingsCompleted);
				ControlsHelper.BeginInvoke(method, new object[] { sender, e });
				return;
			}
			// Detach event handler so resource could be released.
			var ws = (WebServiceClient)sender;
			ws.SearchSettingsCompleted -= wsPresets_SearchSettingsCompleted;
			if (e.Error != null)
			{
				var error = e.Error.Message;
				if (e.Error.InnerException != null) error += "\r\n" + e.Error.InnerException.Message;
				_ParentControl.InfoPanel.SetBodyError(error);
			}
			else if (e.Result == null)
			{
				_ParentControl.InfoPanel.SetBodyInfo("No default settings received.");
			}
			else
			{
				var result = (SearchResult)e.Result;
				AppHelper.UpdateList(result.Presets, SettingsManager.Presets.Items);
				SettingsManager.Current.UpsertPadSettings(result.PadSettings);
				SettingsManager.Current.CleanupPadSettings();
				var presetsCount = (result.Presets == null) ? 0 : result.Presets.Length;
				var padSettingsCount = (result.PadSettings == null) ? 0 : result.PadSettings.Length;
				_ParentControl.InfoPanel.SetBodyInfo("{0} default settings and {1} PAD settings received.", presetsCount, padSettingsCount);
			}
			_ParentControl.InfoPanel.RemoveTask(TaskName.SearchPresets);
			RefreshButton.IsEnabled = true;
		}

		private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{

		}

		private void RefreshButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			RefreshData();
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
			UnInitPanel();
		}

	}
}
