using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	public partial class LoadPresetsForm : BaseForm
	{
		public LoadPresetsForm()
		{
			InitializeComponent();
			ControlHelper.ApplyBorderStyle(MyDevicesDataGridView);
			ControlHelper.ApplyBorderStyle(SummariesDataGridView);
			ControlHelper.ApplyBorderStyle(PresetsDataGridView);
		}

		#region Presets.

		string _defaultPresetsTitle;

		public void InitPresets()
		{
			_defaultPresetsTitle = PresetsTabPage.Text;
			EngineHelper.EnableDoubleBuffering(PresetsDataGridView);
			PresetsDataGridView.AutoGenerateColumns = false;
			// Configure Presets.
			SettingsManager.Presets.Items.ListChanged += Presets_ListChanged;
			PresetsDataGridView.DataSource = SettingsManager.Presets.Items;
			UpdateControlsFromPresets();
		}

		public void UnInitPresets()
		{
			SettingsManager.Presets.Items.ListChanged -= Presets_ListChanged;
			PresetsDataGridView.DataSource = SettingsManager.Presets.Items;
		}

		void UpdateControlsFromPresets()
		{
			PresetsTabPage.Text = SettingsManager.Presets.Items.Count == 0
				? _defaultPresetsTitle
				: string.Format("{0} [{1}]", _defaultPresetsTitle, SettingsManager.Presets.Items.Count);
		}

		void Presets_ListChanged(object sender, ListChangedEventArgs e)
		{
			UpdateControlsFromPresets();
		}

		private void PresetRefreshButton_Click(object sender, EventArgs e)
		{
			RefreshPresetsGrid(true);
		}

		public void RefreshPresetsGrid(bool showResult)
		{
			var sp = new List<SearchParameter>();
			sp.Add(new SearchParameter());
			var ws = new WebServiceClient();
			ws.Url = SettingsManager.Options.InternetDatabaseUrl;
			ws.SearchSettingsCompleted += wsPresets_SearchSettingsCompleted;
			//System.Threading.ThreadPool.QueueUserWorkItem(delegate (object state)
			//{
			ws.SearchSettingsAsync(sp.ToArray(), showResult);
			//});
		}

		void wsPresets_SearchSettingsCompleted(object sender, ResultEventArgs e)
		{
			if (InvokeRequired)
			{
				var method = new EventHandler<ResultEventArgs>(wsPresets_SearchSettingsCompleted);
				BeginInvoke(method, new object[] { sender, e });
				return;
			}
			var ws = (WebServiceClient)sender;
			ws.SearchSettingsCompleted -= wsPresets_SearchSettingsCompleted;
			// Make sure method is executed on the same thread as this control.
			AddTask(TaskName.SearchPresets);
			if (e.Error != null)
			{
				var error = e.Error.Message;
				if (e.Error.InnerException != null) error += "\r\n" + e.Error.InnerException.Message;
				SetHeaderBody(MessageBoxIcon.Error,
						"{0: yyyy-MM-dd HH:mm:ss}: {1}",
						DateTime.Now, error);
			}
			else if (e.Result == null)
			{
				var showResult = (bool)e.UserState;
				if (showResult)
				{
					SetHeaderBody(
						MessageBoxIcon.Information,
						"{0: yyyy-MM-dd HH:mm:ss}: No Presets received.",
						DateTime.Now
					);
				}
			}
			else
			{
				var result = (SearchResult)e.Result;
				AppHelper.UpdateList(result.Presets, SettingsManager.Presets.Items);
				AppHelper.UpdateList(result.PadSettings, SettingsManager.PadSettings.Items);
				if ((bool)e.UserState)
				{
					var presetsCount = (result.Presets == null) ? 0 : result.Presets.Length;
					var padSettingsCount = (result.PadSettings == null) ? 0 : result.PadSettings.Length;
					SetHeaderBody(
						MessageBoxIcon.Information,
						"{0: yyyy-MM-dd HH:mm:ss}: {1} Presets and {2} PAD Settings received.",
						DateTime.Now, presetsCount, padSettingsCount
					);
				}
			}
			RemoveTask(TaskName.SearchPresets);
		}

		#endregion

		private void OkButton_Click(object sender, EventArgs e)
		{

		}

		void LoadPreset()
		{
			//MainForm.Current.UpdateTimer.Stop();
			//if (ControllerComboBox.SelectedItem == null) return;
			//var name = ((KeyValuePair)ControllerComboBox.SelectedItem).Key;
			//if (PresetsDataGridView.SelectedRows.Count == 0) return;
			//var title = "Load Preset Setting?";
			//var preset = (Preset)PresetsDataGridView.SelectedRows[0].DataBoundItem;
			//var message = "Do you want to load Preset Setting:";
			//message += "\r\n\r\n    " + preset.ProductName;
			//message += "\r\n\r\nfor \"" + name + "\" controller?";
			//MessageBoxForm form = new MessageBoxForm();
			//form.StartPosition = FormStartPosition.CenterParent;
			//var result = form.ShowForm(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
			//if (result == DialogResult.Yes) LoadPreset(preset.ProductName);
			//else MainForm.Current.UpdateTimer.Start();
		}

		private void MainTabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			var tab = MainTabControl.SelectedTab;
			if (tab != null) SetHeaderSubject(tab.Text);
		}
	}


}
