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
			// The form is shown again after it closes, and a fetch it closed during must not leave the buttons off.
			_fetching = false;
			UpdateControls();
		}

		public void UnInitForm()
		{
			SettingsGridPanel.UnInitPanel();
			SummariesGridPanel.UnInitPanel();
			PresetsGridPanel.UnInitPanel();
		}

		public PadSetting SelectedItem;

		/// <summary>The checksum of the settings of the row selected on the open tab.</summary>
		/// <returns>Null when no row is selected.</returns>
		Guid? GetSelectedChecksum()
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
			return checksum;
		}

		/// <summary>What a button does with the selected preset's settings.</summary>
		enum PresetAction { Load, Copy, CopyFormat }

		/// <summary>Gets a preset's settings from the server by their checksum, or null when the server has none.</summary>
		/// <remarks>A test replaces it, since the real one asks the server.</remarks>
		public static Func<Guid, PadSetting> LoadFromServer = checksum =>
		{
			var ws = new WebServiceClient();
			ws.Url = SettingsManager.Options.InternetDatabaseUrl;
			var result = ws.LoadSetting(new[] { checksum });
			return result?.PadSettings?.FirstOrDefault(x => x.PadSettingChecksum == checksum);
		};

		/// <summary>Whether the selected preset's settings are being fetched from the server.</summary>
		bool _fetching;

		/// <summary>Does what the button asks with the selected preset's settings.</summary>
		/// <remarks>
		/// Every row of the lists is a preset on the server. Its settings are used from this computer when
		/// they are stored here, which is the same thing, as the checksum names them; otherwise they are
		/// fetched from the server, as x360ce 3 does, and kept.
		/// </remarks>
		void UseSelectedPreset(PresetAction action)
		{
			var checksum = GetSelectedChecksum();
			if (!checksum.HasValue)
				return;
			var padSetting = SettingsManager.GetPadSetting(checksum.Value);
			if (padSetting != null)
			{
				UsePreset(action, padSetting);
				return;
			}
			_fetching = true;
			AddTask(TaskName.LoadSetting);
			UpdateControls();
			System.Threading.ThreadPool.QueueUserWorkItem(delegate (object state)
			{
				PadSetting fetched = null;
				Exception error = null;
				// Handed to the window, which says what went wrong; thrown here, it would stop the program.
				try { fetched = LoadFromServer(checksum.Value); }
				catch (Exception ex) { error = ex; }
				if (!IsDisposed && IsHandleCreated)
					BeginInvoke((Action)(() => PresetFetched(action, checksum.Value, fetched, error)));
			});
		}

		void PresetFetched(PresetAction action, Guid checksum, PadSetting padSetting, Exception error)
		{
			_fetching = false;
			RemoveTask(TaskName.LoadSetting);
			UpdateControls();
			if (error != null)
			{
				var message = error.Message;
				if (error.InnerException != null)
					message += "\r\n" + error.InnerException.Message;
				SetHeaderError(message);
				return;
			}
			if (padSetting == null)
			{
				// Listed by the server without settings it has, so it can never be loaded.
				SettingsManager.Summaries.Remove(SettingsManager.Summaries.Items.Where(x => x.PadSettingChecksum == checksum).ToArray());
				SettingsManager.Presets.Remove(SettingsManager.Presets.Items.Where(x => x.PadSettingChecksum == checksum).ToArray());
				SetHeaderInfo("This preset is no longer on the server, so it was taken off the list.");
				return;
			}
			SettingsManager.Current.UpsertPadSettings(padSetting);
			// Closed while the server answered: kept for next time, and nothing more.
			if (Visible)
				UsePreset(action, padSetting);
		}

		void UsePreset(PresetAction action, PadSetting padSetting)
		{
			switch (action)
			{
				case PresetAction.Load:
					SelectedItem = padSetting;
					DialogResult = DialogResult.OK;
					break;
				case PresetAction.Copy:
					SettingsManager.CopyPresetToClipboard(padSetting);
					break;
				case PresetAction.CopyFormat:
					SettingsManager.ShowCopyPresetMenu(CopyPresetButton, padSetting);
					break;
			}
		}

		private void OkButton_Click(object sender, EventArgs e)
		{
			UseSelectedPreset(PresetAction.Load);
		}

		private void CopyPresetButton_Click(object sender, EventArgs e)
		{
			UseSelectedPreset(PresetAction.Copy);
		}

		private void CopyPresetFormatButton_Click(object sender, EventArgs e)
		{
			UseSelectedPreset(PresetAction.CopyFormat);
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
			// Off while the server answers, so one press fetches once.
			var enabled = selected && !_fetching;
			ControlsHelper.SetEnabled(OkButton, enabled);
			ControlsHelper.SetEnabled(CopyPresetButton, enabled);
			ControlsHelper.SetEnabled(CopyPresetFormatButton, enabled);
		}

	}
}
