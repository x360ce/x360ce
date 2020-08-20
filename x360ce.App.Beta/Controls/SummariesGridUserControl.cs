using JocysCom.ClassLibrary.Web.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	public partial class SummariesGridUserControl : UserControl
	{
		public SummariesGridUserControl()
		{
			InitializeComponent();
			// Make font more consistent with the rest of the interface.
			Controls.OfType<ToolStrip>().ToList().ForEach(x => x.Font = Font);
			JocysCom.ClassLibrary.Controls.ControlsHelper.ApplyBorderStyle(SummariesDataGridView);
			EngineHelper.EnableDoubleBuffering(SummariesDataGridView);
			SummariesDataGridView.AutoGenerateColumns = false;
		}

		public BaseFormWithHeader _ParentForm;

		public void InitPanel()
		{
			SettingsManager.Summaries.Items.ListChanged += Summaries_ListChanged;
			SummariesDataGridView.DataSource = SettingsManager.Summaries.Items;
			UpdateControlsFromSummaries();
		}

		public void UnInitPanel()
		{
			SettingsManager.Summaries.Items.ListChanged -= Summaries_ListChanged;
			SummariesDataGridView.DataSource = null;
		}

		void UpdateControlsFromSummaries()
		{
			var count = SettingsManager.Summaries.Items.Count;
			// Allow refresh summaries.
			//ControlsHelper.SetEnabled(SummariesRefreshButton, count > 0);
		}

		void Summaries_ListChanged(object sender, ListChangedEventArgs e)
		{
			UpdateControlsFromSummaries();
		}

		private void SummariesRefreshButton_Click(object sender, EventArgs e)
		{
			RefreshSummariesGrid();
		}

		void SummariesDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;
			var grid = (DataGridView)sender;
			var item = (Summary)grid.Rows[e.RowIndex].DataBoundItem;
			var column = grid.Columns[e.ColumnIndex];
			if (column == SummariesSidColumn)
			{
				e.Value = EngineHelper.GetID(item.PadSettingChecksum);
			}
		}


		public void RefreshSummariesGrid()
		{
			//mainForm.LoadingCircle = true;

			_ParentForm.AddTask(TaskName.SearchSummaries);
			SummariesRefreshButton.Enabled = false;
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
			if (InvokeRequired)
			{
				var method = new EventHandler<SoapHttpClientEventArgs>(ws_SearchSummariesCompleted);
				BeginInvoke(method, new object[] { sender, e });
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
				_ParentForm.SetHeaderError(error);
			}
			else if (e.Result == null)
			{
				_ParentForm.SetHeaderInfo("No default settings received.");
			}
			else
			{
				var result = (SearchResult)e.Result;
				var summariesCount = result.Summaries?.Length ?? 0;
				var padSettingsCount = result.PadSettings?.Length ?? 0;
				if (summariesCount == 0)
				{
					_ParentForm.SetHeaderInfo("0 default settings received.");
				}
				else
				{
					if (padSettingsCount == 0)
					{
						_ParentForm.SetHeaderError("Error: {0} default settings received, but no PAD settings.", summariesCount);
					}
					else
					{
						// Reorder summaries.
						result.Summaries = result.Summaries.OrderBy(x => x.ProductName).ThenBy(x => x.FileName).ThenBy(x => x.FileProductName).ThenByDescending(x => x.Users).ToArray();
						AppHelper.UpdateList(result.Summaries, SettingsManager.Summaries.Items);
						// Update pad settings.
						SettingsManager.Current.UpsertPadSettings(result.PadSettings);
						SettingsManager.Current.CleanupPadSettings();
						_ParentForm.SetHeaderInfo("{0} default settings and {1} PAD settings received.", summariesCount, padSettingsCount);
					}
				}
			}
			_ParentForm.RemoveTask(TaskName.SearchSummaries);
			SummariesRefreshButton.Enabled = true;
		}

	}
}
