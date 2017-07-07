using JocysCom.ClassLibrary.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	public partial class LoadPresetsForm : BaseForm
	{
		public LoadPresetsForm()
		{
			InitializeComponent();
			SetHeaderBody(MessageBoxIcon.None, "", "");
			SettingsGridPanel._ParentForm = this;
			SummariesGridPanel._ParentForm = this;
			PresetsGridPanel._ParentForm = this;
		}

		public void InitForm()
		{
			SettingsGridPanel.InitPanel();
			SummariesGridPanel.InitPanel();
			PresetsGridPanel.InitPanel();
			UpdateControls();
		}

		public void UnInitForm()
		{
			SettingsGridPanel.UnInitPanel();
			SummariesGridPanel.UnInitPanel();
			PresetsGridPanel.UnInitPanel();
		}

		public PadSetting SelectedItem;

		private void OkButton_Click(object sender, EventArgs e)
		{
			Guid? checksum = null;
			if (MainTabControl.SelectedTab == PresetsTabPage)
			{
				var row = PresetsGridPanel.PresetsDataGridView.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault();
				if (row != null)
				{
					var preset = (Preset)row.DataBoundItem;
					checksum = preset.PadSettingChecksum;
				}
			}
			if (MainTabControl.SelectedTab == SummariesTabPage)
			{
				var row = SummariesGridPanel.SummariesDataGridView.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault();
				if (row != null)
				{
					var summary = (Summary)row.DataBoundItem;
					checksum = summary.PadSettingChecksum;
				}
			}
			if (MainTabControl.SelectedTab == SettingsTabPage)
			{
				var row = SettingsGridPanel.SettingsDataGridView.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault();
				if (row != null)
				{
					var setting = (Setting)row.DataBoundItem;
					checksum = setting.PadSettingChecksum;
				}
			}
			if (checksum.HasValue)
			{
				SelectedItem = SettingsManager.PadSettings.Items.FirstOrDefault(x => x.PadSettingChecksum == checksum.Value);
			}
			DialogResult = DialogResult.OK;
		}

		private void MainTabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateControls();
		}

		void UpdateControls()
		{
			var tab = MainTabControl.SelectedTab;
			if (tab != null) SetHeaderSubject(tab.Text);
			bool selected = false;
			if (MainTabControl.SelectedTab == PresetsTabPage)
			{
				selected = PresetsGridPanel.PresetsDataGridView.Rows.Count > 0;
			}
			if (MainTabControl.SelectedTab == SummariesTabPage)
			{
				selected = SummariesGridPanel.SummariesDataGridView.Rows.Count > 0;
			}
			if (MainTabControl.SelectedTab == SettingsTabPage)
			{
				selected = SettingsGridPanel.SettingsDataGridView.Rows.Count > 0;
			}
			ControlsHelper.SetEnabled(OkButton, selected);
		}

	}
}
