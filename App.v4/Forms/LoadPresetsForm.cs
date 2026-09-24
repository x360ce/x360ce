using JocysCom.ClassLibrary.Controls;
using System;
using System.Linq;
using System.Windows.Forms;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	public partial class LoadPresetsForm : BaseFormWithHeader
	{
		public LoadPresetsForm()
		{
			InitializeComponent();
			SetHeaderBody(MessageBoxIcon.None);
			SettingsGridPanel._ParentForm = this;
			SummariesGridPanel._ParentForm = this;
			PresetsGridPanel._ParentForm = this;
			// The lists fill from the server after the form is open, and a row is selected as it
			// arrives. The button followed the tab only, so a tab opened while its list was still
			// empty kept the button off however much was selected afterwards.
			SettingsGridPanel.SettingsDataGridView.SelectionChanged += Grid_SelectionChanged;
			SummariesGridPanel.SummariesDataGridView.SelectionChanged += Grid_SelectionChanged;
			PresetsGridPanel.PresetsDataGridView.SelectionChanged += Grid_SelectionChanged;
		}

		private void Grid_SelectionChanged(object sender, EventArgs e)
		{
			UpdateControls();
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

		/// <summary>The settings of the row selected on the open tab.</summary>
		/// <param name="rowSelected">Whether a row is selected at all.</param>
		/// <returns>Null when no row is selected, or when the server sent no settings for the one that is.</returns>
		PadSetting GetSelectedPadSetting(out bool rowSelected)
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
					var setting = (UserSetting)row.DataBoundItem;
					checksum = setting.PadSettingChecksum;
				}
			}
			rowSelected = checksum.HasValue;
			return checksum.HasValue
				? SettingsManager.PadSettings.Items.FirstOrDefault(x => x.PadSettingChecksum == checksum.Value)
				: null;
		}

		/// <summary>What the header says when the selected row came without its settings.</summary>
		const string NoSettingsMessage = "The server sent no settings for this preset. Choose another.";

		private void OkButton_Click(object sender, EventArgs e)
		{
			bool rowSelected;
			var padSetting = GetSelectedPadSetting(out rowSelected);
			if (rowSelected)
			{
				SelectedItem = padSetting;
				// The lists come with their settings, so a missing one is missing on the server too.
				// Closing with nothing looked like the button did not work; the form stays open and says.
				if (SelectedItem == null)
				{
					SetHeaderError(NoSettingsMessage);
					return;
				}
			}
			DialogResult = DialogResult.OK;
		}

		/// <summary>The selected preset to copy as text, so what it holds can be read before it is loaded.</summary>
		/// <returns>Null, having said so in the header, when the selected row came without its settings.</returns>
		PadSetting GetPadSettingToCopy()
		{
			bool rowSelected;
			var padSetting = GetSelectedPadSetting(out rowSelected);
			if (padSetting == null && rowSelected)
				SetHeaderError(NoSettingsMessage);
			return padSetting;
		}

		private void CopyPresetButton_Click(object sender, EventArgs e)
		{
			var padSetting = GetPadSettingToCopy();
			if (padSetting != null)
				SettingsManager.CopyPresetToClipboard(padSetting);
		}

		private void CopyPresetFormatButton_Click(object sender, EventArgs e)
		{
			var padSetting = GetPadSettingToCopy();
			if (padSetting != null)
				SettingsManager.ShowCopyPresetMenu(CopyPresetButton, padSetting);
		}

		private void OpenFileButton_Click(object sender, EventArgs e)
		{
			using (var dialog = new OpenFileDialog())
			{
				dialog.Title = "Open Preset";
				dialog.Filter = SettingsManager.PresetFileFilter;
				if (dialog.ShowDialog(this) != DialogResult.OK)
					return;
				try
				{
					SelectedItem = SettingsManager.LoadPadSetting(dialog.FileName);
				}
				catch (Exception ex)
				{
					// Not a preset, or not readable. Said here, where the file was chosen.
					var form = new MessageBoxForm();
					form.StartPosition = FormStartPosition.CenterParent;
					ControlsHelper.CheckTopMost(form);
					form.ShowForm(ex.Message);
					form.Dispose();
					return;
				}
			}
			DialogResult = DialogResult.OK;
		}

		private void MainTabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateControls();
		}

		private void UpdateControls()
		{
			var tab = MainTabControl.SelectedTab;
			if (tab != null)
				SetHeaderSubject(tab.Text);
			// What the buttons load and copy is the selected row, so the selected row is what enables them.
			var selected = false;
			if (MainTabControl.SelectedTab == PresetsTabPage)
				selected = PresetsGridPanel.PresetsDataGridView.SelectedRows.Count > 0;
			if (MainTabControl.SelectedTab == SummariesTabPage)
				selected = SummariesGridPanel.SummariesDataGridView.SelectedRows.Count > 0;
			if (MainTabControl.SelectedTab == SettingsTabPage)
				selected = SettingsGridPanel.SettingsDataGridView.SelectedRows.Count > 0;
			ControlsHelper.SetEnabled(OkButton, selected);
			ControlsHelper.SetEnabled(CopyPresetButton, selected);
			ControlsHelper.SetEnabled(CopyPresetFormatButton, selected);
		}

	}
}
