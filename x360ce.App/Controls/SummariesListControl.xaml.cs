using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Web.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for SummariesListUserControl.xaml
	/// </summary>
	public partial class SummariesListControl : UserControl
	{
		public SummariesListControl()
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
			SettingsManager.Summaries.Items.ListChanged += Summaries_ListChanged;
			MainDataGrid.ItemsSource = SettingsManager.Summaries.Items;
			UpdateControlsFromSummaries();
		}

		public void UnInitPanel()
		{
			SettingsManager.Summaries.Items.ListChanged -= Summaries_ListChanged;
			MainDataGrid.ItemsSource = null;
		}

		void UpdateControlsFromSummaries()
		{
		}

		void Summaries_ListChanged(object sender, ListChangedEventArgs e)
		{
			UpdateControlsFromSummaries();
		}

		public void RefreshData()
		{
			_ParentControl.InfoPanel.AddTask(TaskName.SearchSummaries);
			RefreshButton.IsEnabled = false;
			var sp = new List<SearchParameter>();
			FillSearchParameterWithProducts(sp);
			SettingsManager.Current.FillSearchParameterWithFiles(sp);
			var o = SettingsManager.Options;
			var ws = new WebServiceClient();
			ws.Url = o.InternetDatabaseUrl;
			ws.SearchSettingsCompleted += ws_SearchSummariesCompleted;
			System.Threading.ThreadPool.QueueUserWorkItem(delegate (object state)
			{
				ws.SearchSettingsAsync(sp.ToArray());
			});
		}


		public void FillSearchParameterWithProducts(List<SearchParameter> sp)
		{
			// Select user devices as parameters to search.
			var userDevices = SettingsManager.UserSettings.Items
				.Select(x => x.ProductGuid).Distinct()
				// Do not add empty records.
				.Where(x => x != Guid.Empty)
				.Select(x => new SearchParameter() { ProductGuid = x })
				.ToArray();
			sp.AddRange(userDevices);
		}

		void ws_SearchSummariesCompleted(object sender, SoapHttpClientEventArgs e)
		{
			// Make sure method is executed on the same thread as this control.
			if (ControlsHelper.InvokeRequired)
			{
				var method = new EventHandler<SoapHttpClientEventArgs>(ws_SearchSummariesCompleted);
				ControlsHelper.BeginInvoke(method, new object[] { sender, e });
				return;
			}
			// Detach event handler so resource could be released.
			var ws = (WebServiceClient)sender;
			ws.SearchSettingsCompleted -= ws_SearchSummariesCompleted;
			if (e.Error != null)
			{
				var error = e.Error.Message;
				if (e.Error.InnerException != null)
					error += "\r\n" + e.Error.InnerException.Message;
				_ParentControl.InfoPanel.SetBodyError(error);
			}
			else if (e.Result == null)
			{
				_ParentControl.InfoPanel.SetBodyInfo("No default settings received.");
			}
			else
			{
				var result = (SearchResult)e.Result;
				var summariesCount = result.Summaries?.Length ?? 0;
				var padSettingsCount = result.PadSettings?.Length ?? 0;
				if (summariesCount == 0)
				{
					_ParentControl.InfoPanel.SetBodyInfo("0 default settings received.");
				}
				else
				{
					if (padSettingsCount == 0)
					{
						_ParentControl.InfoPanel.SetBodyError("Error: {0} default settings received, but no PAD settings.", summariesCount);
					}
					else
					{
						// Reorder summaries.
						result.Summaries = result.Summaries.OrderBy(x => x.ProductName).ThenBy(x => x.FileName).ThenBy(x => x.FileProductName).ThenByDescending(x => x.Users).ToArray();
						AppHelper.UpdateList(result.Summaries, SettingsManager.Summaries.Items);
						// Update pad settings.
						SettingsManager.Current.UpsertPadSettings(result.PadSettings);
						SettingsManager.Current.CleanupPadSettings();
						_ParentControl.InfoPanel.SetBodyInfo("{0} default settings and {1} PAD settings received.", summariesCount, padSettingsCount);
					}
				}
			}
			_ParentControl.InfoPanel.RemoveTask(TaskName.SearchSummaries);
			RefreshButton.IsEnabled = true;
		}

		private void RefreshButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			RefreshData();
		}

		private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{

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
