using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	public partial class PresetsGridUserControl : UserControl
	{

		public PresetsGridUserControl()
		{
			InitializeComponent();
			JocysCom.ClassLibrary.Controls.ControlsHelper.ApplyBorderStyle(PresetsDataGridView);
			PresetsDataGridView.AutoGenerateColumns = false;
		}

		public BaseFormWithHeader _ParentForm;

		public void InitPanel()
		{
			SettingsManager.Presets.Items.ListChanged += Presets_ListChanged;
			PresetsDataGridView.DataSource = SettingsManager.Presets.Items;
			ShowPresets(null);
			UpdateControlsFromPresets();
		}

		public void UnInitPanel()
		{
			SettingsManager.Presets.Items.ListChanged -= Presets_ListChanged;
			PresetsDataGridView.DataSource = null;
		}


		void UpdateControlsFromPresets()
		{
		}

		void Presets_ListChanged(object sender, ListChangedEventArgs e)
		{
			UpdateControlsFromPresets();
		}

		/// <summary>Fetches x360ce's controller database, the one file x360ce's server keeps, only when it changed (ETag), then lists it again; off the window's thread.</summary>
		private void PresetRefreshButton_Click(object sender, EventArgs e)
		{
			PresetRefreshButton.Enabled = false;
			_ParentForm.AddTask(TaskName.SearchPresets);
			var version = typeof(PresetsGridUserControl).Assembly.GetName().Version;
			X360ceUpdateClient.UpdateSettingsCopy("x360ce/" + version).ContinueWith(t =>
			{
				_ParentForm.RemoveTask(TaskName.SearchPresets);
				PresetRefreshButton.Enabled = true;
				if (t.Result.Value == DatabaseUpdateOutcome.Failed)
					_ParentForm.SetHeaderError("{0}", t.Result.Key);
				else
					ShowPresets(t.Result.Key);
			}, TaskScheduler.FromCurrentSynchronizationContext());
		}

		/// <summary>
		/// Lists the setting most people use for each controller, from x360ce's controller database: the built-in copy, or
		/// the one [Update x360ce Database] saved. Offline; read off the window's thread, as the first read takes a few
		/// hundred milliseconds, and listed at once rather than row by row.
		/// </summary>
		/// <param name="note">What the header says when the list is shown; how many controllers it has when null.</param>
		void ShowPresets(string note)
		{
			Task.Run(() => X360ceDatabase.Presets()).ContinueWith(t =>
			{
				if (IsDisposed || t.IsFaulted)
					return;
				var items = SettingsManager.Presets.Items;
				items.RaiseListChangedEvents = false;
				try
				{
					AppHelper.UpdateList(t.Result, items);
				}
				finally
				{
					items.RaiseListChangedEvents = true;
				}
				items.ResetBindings();
				_ParentForm.SetHeaderInfo("{0}", note ?? string.Format("The setting most people use for each of {0} controllers, from x360ce's controller database.", t.Result.Length));
			}, TaskScheduler.FromCurrentSynchronizationContext());
		}

		private void PresetsDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;
			var grid = (DataGridView)sender;
			var item = AppHelper.BoundItem<Preset>(grid, e.RowIndex);
			if (item is null)
				return;
			var column = grid.Columns[e.ColumnIndex];
			if (column == PresetSidColumn)
			{
				e.Value = EngineHelper.GetID(item.PadSettingChecksum);
			}
		}

	}
}
