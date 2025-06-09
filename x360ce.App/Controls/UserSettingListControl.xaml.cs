using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Web.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using x360ce.App.Converters;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for SettingsListControl.xaml
	/// </summary>
	public partial class UserSettingListControl : UserControl
	{
		public UserSettingListControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			MainDataGrid.AutoGenerateColumns = false;
			MainDataGrid.SelectionMode = System.Windows.Controls.DataGridSelectionMode.Single;
			if (ControlsHelper.IsDesignMode(this))
				return;
			_MainDataGridFormattingConverter = (ItemFormattingConverter)MainDataGrid.Resources[nameof(_MainDataGridFormattingConverter)];
			_MainDataGridFormattingConverter.ConvertFunction = _MainDataGridFormattingConverter_Convert;

		}

		ItemFormattingConverter _MainDataGridFormattingConverter;

		object _MainDataGridFormattingConverter_Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			var sender = (FrameworkElement)values[0];
			var template = (FrameworkElement)values[1];
			var cell = (DataGridCell)(template ?? sender).Parent;
			var value = values[2];
			var item = (UserSetting)cell.DataContext;
			// Format ConnectionClassColumn value.
			if (cell.Column == VendorNameColumn)
			{
				var ud = SettingsManager.UserDevices.Items.FirstOrDefault(x => x.InstanceGuid == item.InstanceGuid);
				return ud?.HidManufacturer;
			}
			return value;
		}

		public BaseWithHeaderControl _ParentControl;

		public void InitPanel()
		{
			SettingsManager.UserSettings.Items.ListChanged += Settings_ListChanged;
			// WORKAROUND: Remove SelectionChanged event.
			MainDataGrid.SelectionChanged -= MainDataGrid_SelectionChanged;
			var userSettingsView = new BindingListCollectionView(SettingsManager.UserSettings.Items);
			MainDataGrid.ItemsSource = userSettingsView;
			// WORKAROUND: Use BeginInvoke to prevent SelectionChanged firing multiple times.
			//BeginInvoke((Action)delegate ()
			//{
			MainDataGrid.SelectionChanged += MainDataGrid_SelectionChanged;
			MainDataGrid_SelectionChanged(MainDataGrid, null);
			//});
			UpdateControlsFromSettings();
		}

		public void UnInitPanel()
		{
			SettingsManager.UserSettings.Items.ListChanged -= Settings_ListChanged;
			((BindingListCollectionView)MainDataGrid.ItemsSource)?.DetachFromSourceCollection();
			//MainDataGrid.ItemsSource = null;
		}

		void UpdateControlsFromSettings()
		{
		}

		void Settings_ListChanged(object sender, ListChangedEventArgs e)
		{
			UpdateControlsFromSettings();
		}

		private void DeleteButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			var grid = MainDataGrid;
			var userSettings = grid.SelectedItems.Cast<UserSetting>().ToArray();
			var form = new MessageBoxWindow();
			var result = form.ShowDialog("Do you want to delete selected settings?", "X360CE - Delete Settings",
				MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
			if (result == MessageBoxResult.Yes)
			{
				// Remove from local settings.
				foreach (var item in userSettings)
					SettingsManager.UserSettings.Items.Remove(item);
				SettingsManager.Save();
				// Remove from cloud settings.
				Task.Run(new Action(() =>
				{
					foreach (var item in userSettings)
						Global.CloudClient.Add(CloudAction.Delete, new UserSetting[] { item });

				}));
			}
		}

		private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			UpdateNoteLabel();
		}

		void UpdateNoteLabel()
		{
			var item = MainDataGrid.SelectedItems
				.Cast<UserSetting>()
				.FirstOrDefault();
			var note = item == null ? string.Empty : item.Comment ?? "";
			ControlsHelper.SetText(CommentLabel, note);
			ControlsHelper.SetVisible(CommentPanel, !string.IsNullOrEmpty(note));
		}

		private void SettingsEditNoteButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			var item = MainDataGrid.SelectedItems
				.Cast<UserSetting>()
				.FirstOrDefault();
			if (item == null)
			{
				return;
			}
			var note = item.Comment ?? "";
			var form = new MessageBoxWindow();
			form.MessageTextBox.MaxLength = 1024;
			form.MessageTextBox.Text = note;
			var result = form.ShowPrompt(note, "X360CE - Edit Note");
			if (result == MessageBoxResult.OK)
			{
				item.Comment = form.MessageTextBox.Text.Trim();
				UpdateNoteLabel();
			}
		}

		public void RefreshData()
		{
			_ParentControl.InfoPanel.AddTask(TaskName.SearchSettings);
			RefreshButton.IsEnabled = false;
			var sp = new List<SearchParameter>();
			SettingsManager.Current.FillSearchParameterWithInstances(sp);
			SettingsManager.Current.FillSearchParameterWithFiles(sp);
			var o = SettingsManager.Options;
			var ws = new WebServiceClient();
			ws.Url = o.InternetDatabaseUrl;
			ws.SearchSettingsCompleted += ws_SearchSettingsCompleted;
			// Make sure it runs on another thread.
			System.Threading.ThreadPool.QueueUserWorkItem(delegate (object state)
			{
				ws.SearchSettingsAsync(sp.ToArray());
			});
		}

		void ws_SearchSettingsCompleted(object sender, SoapHttpClientEventArgs e)
		{
			// Make sure method is executed on the same thread as this control.
			if (ControlsHelper.InvokeRequired)
			{
				var method = new EventHandler<SoapHttpClientEventArgs>(ws_SearchSettingsCompleted);
				ControlsHelper.BeginInvoke(method, new object[] { sender, e });
				return;
			}
			// Detach event handler so resource could be released.
			var ws = (WebServiceClient)sender;
			ws.SearchSettingsCompleted -= ws_SearchSettingsCompleted;
			if (e.Error != null)
			{
				var error = e.Error.Message;
				if (e.Error.InnerException != null)
					error += "\r\n" + e.Error.InnerException.Message;
				_ParentControl.InfoPanel.SetBodyError(error);
			}
			else if (e.Result == null)
			{
				_ParentControl.InfoPanel.SetBodyInfo("No user settings received.");
			}
			else
			{
				// Suspend DInput Service.
				Global.DHelper.StopDInputService();
				MainDataGrid.ItemsSource = null;
				var result = (SearchResult)e.Result;
				// Reorder Settings.
				result.Settings = result.Settings.OrderBy(x => x.ProductName).ThenBy(x => x.FileName).ThenBy(x => x.FileProductName).ToArray();
				SettingsManager.Current.UpsertSettings(result.Settings);
				// Insert pad settings which are used by settings.
				SettingsManager.Current.UpsertPadSettings(result.PadSettings);
				// Remove unused pad settings.
				SettingsManager.Current.CleanupPadSettings();
				// Display results about operation.
				var settingsCount = (result.Settings == null) ? 0 : result.Settings.Length;
				var padSettingsCount = (result.PadSettings == null) ? 0 : result.PadSettings.Length;
				_ParentControl.InfoPanel.SetBodyInfo("{0} user settings and {1} PAD settings received.", settingsCount, padSettingsCount);

				MainDataGrid.ItemsSource = SettingsManager.UserSettings.Items;
				// Resume DInput Service.
				if (Global.AllowDHelperStart)
					Global.DHelper.StartDInputService();
			}
			_ParentControl.InfoPanel.RemoveTask(TaskName.SearchSettings);
			RefreshButton.IsEnabled = true;
		}

		private void RefreshButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			RefreshData();
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
			UnInitPanel();
		}
	}
}
