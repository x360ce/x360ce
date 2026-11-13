using JocysCom.ClassLibrary.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	/// <summary>One row of SDL's controller database as the Load Preset window lists it.</summary>
	public sealed class SdlPresetRow
	{
		public SdlMapping Mapping { get; set; }
		public string Name { get; set; }
		public string Guid { get; set; }
		/// <summary>The vendor and product as hex, "054C:05C4", or empty when the GUID names none.</summary>
		public string Ids { get; set; }
		public string Source { get; set; }
		/// <summary>"This device" for a row of the selected device, which come first.</summary>
		public string Device { get; set; }
	}

	/// <summary>
	/// The SDL tab of the Load Preset window: the rows of SDL's controller database, the selected device's first, with a
	/// filter, the database's version and the update button. The row chosen is loaded or copied translated for the device.
	/// </summary>
	public class SdlPresetsUserControl : UserControl
	{
		public SdlPresetsUserControl()
		{
			Name = "SdlPresetsPanel";
			SuspendLayout();
			MainTableLayoutPanel = new TableLayoutPanel
			{
				Name = nameof(MainTableLayoutPanel),
				Dock = DockStyle.Fill,
				// None at the sides, so the list meets the tab's edges; the rows above and below it keep that room as margin.
				Padding = AppHelper.ScaledPadding(0, 8, 0, 8),
				ColumnCount = 2,
			};
			MainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle());
			MainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			VersionLabel = new Label { Name = nameof(VersionLabel), AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = AppHelper.ScaledPadding(12, 4, 12, 4) };
			FilterLabel = new Label { Name = nameof(FilterLabel), Text = "Filter", AutoSize = true, Anchor = AnchorStyles.Left, Margin = AppHelper.ScaledPadding(12, 4, 8, 4) };
			FilterTextBox = new TextBox { Name = nameof(FilterTextBox), Dock = DockStyle.Fill, Margin = AppHelper.ScaledPadding(4, 4, 12, 4) };
			FilterTextBox.TextChanged += (s, e) => Fill();
			RowsDataGridView = new DataGridView
			{
				Name = nameof(RowsDataGridView),
				Dock = DockStyle.Fill,
				Margin = AppHelper.ScaledPadding(0, 4, 0, 4),
				AutoGenerateColumns = false,
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = false,
				AllowUserToResizeRows = false,
				ReadOnly = true,
				RowHeadersVisible = false,
				SelectionMode = DataGridViewSelectionMode.FullRowSelect,
				MultiSelect = false,
				ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
			};
			AddColumn("DeviceColumn", "Device", "Device", DataGridViewAutoSizeColumnMode.AllCells);
			AddColumn("NameColumn", "Name", "Name", DataGridViewAutoSizeColumnMode.Fill);
			AddColumn("IdsColumn", "VID:PID", "Ids", DataGridViewAutoSizeColumnMode.AllCells);
			AddColumn("SourceColumn", "From", "Source", DataGridViewAutoSizeColumnMode.AllCells);
			AddColumn("GuidColumn", "SDL GUID", "Guid", DataGridViewAutoSizeColumnMode.AllCells);
			RowsDataGridView.SelectionChanged += (s, e) => SelectionChanged?.Invoke(this, EventArgs.Empty);
			ControlsHelper.ApplyBorderStyle(RowsDataGridView);
			UpdateButton = new Button { Name = nameof(UpdateButton), Text = "Update SDL Database", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, UseVisualStyleBackColor = true, Margin = AppHelper.ScaledPadding(12, 4, 12, 4) };
			UpdateButton.Click += (s, e) => SdlUserControl.StartUpdate(UpdateButton, (text, failed) => StatusLabel.Text = text, Fill);
			// Across the column, so a long answer wraps at the tab's width.
			StatusLabel = new Label { Name = nameof(StatusLabel), AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = AppHelper.ScaledPadding(12, 4, 12, 4) };
			AddRow(VersionLabel);
			MainTableLayoutPanel.SetColumnSpan(VersionLabel, 2);
			MainTableLayoutPanel.RowStyles.Add(new RowStyle());
			MainTableLayoutPanel.Controls.Add(FilterLabel, 0, 1);
			MainTableLayoutPanel.Controls.Add(FilterTextBox, 1, 1);
			AddRow(RowsDataGridView, 100F);
			MainTableLayoutPanel.SetColumnSpan(RowsDataGridView, 2);
			AddRow(UpdateButton);
			MainTableLayoutPanel.SetColumnSpan(UpdateButton, 2);
			AddRow(StatusLabel);
			MainTableLayoutPanel.SetColumnSpan(StatusLabel, 2);
			MainTableLayoutPanel.RowCount = MainTableLayoutPanel.RowStyles.Count;
			Controls.Add(MainTableLayoutPanel);
			ResumeLayout(false);
			PerformLayout();
			// The window it sits in is made after the main window has named and described everything.
			JocysCom.ClassLibrary.Controls.UiTree.UiText.Apply(this);
			x360ce.App.UiTree.UiHelp.Attach(this);
		}

		readonly TableLayoutPanel MainTableLayoutPanel;
		public readonly Label VersionLabel;
		public readonly Label FilterLabel;
		public readonly TextBox FilterTextBox;
		public readonly DataGridView RowsDataGridView;
		public readonly Button UpdateButton;
		public readonly Label StatusLabel;

		/// <summary>The row selected changed.</summary>
		public event EventHandler SelectionChanged;

		/// <summary>The device the rows are for: its own come first, and the row chosen is translated for it.</summary>
		UserDevice _device;

		void AddColumn(string name, string header, string property, DataGridViewAutoSizeColumnMode mode)
		{
			RowsDataGridView.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = name,
				HeaderText = header,
				DataPropertyName = property,
				AutoSizeMode = mode,
			});
		}

		void AddRow(Control control, float percent = 0)
		{
			MainTableLayoutPanel.RowStyles.Add(percent > 0 ? new RowStyle(SizeType.Percent, percent) : new RowStyle());
			MainTableLayoutPanel.Controls.Add(control, 0, MainTableLayoutPanel.RowStyles.Count - 1);
		}

		/// <summary>Lists the rows for a device, its own first and selected.</summary>
		/// <param name="device">The device selected on the controller, or null.</param>
		public void InitPanel(UserDevice device)
		{
			_device = device;
			Fill();
		}

		/// <summary>Lists the rows the filter lets through, the device's own first, and selects the first of those.</summary>
		void Fill()
		{
			var database = SdlDatabase.Current;
			ControlsHelper.SetText(VersionLabel, "SDL controller database: " + database.Describe() + ".");
			var device = SdlDevice.From(_device);
			var own = new HashSet<SdlMapping>(database.FindAll(device));
			var filter = FilterTextBox.Text.Trim();
			var rows = database.Mappings
				.Select(x => new SdlPresetRow
				{
					Mapping = x,
					Name = x.Name,
					Guid = x.Guid.Text,
					Ids = x.Guid.HasIds ? string.Format(CultureInfo.InvariantCulture, "{0:X4}:{1:X4}", x.Guid.VendorId, x.Guid.ProductId) : "",
					Source = Source(database.SourceOf(x)),
					Device = own.Contains(x) ? "This device" : "",
				})
				.Where(x => filter.Length == 0
					|| x.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
					|| x.Guid.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
					|| x.Ids.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
				.OrderByDescending(x => own.Contains(x.Mapping))
				.ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
				.ToList();
			RowsDataGridView.DataSource = rows;
			RowsDataGridView.ClearSelection();
			if (rows.Count > 0 && own.Contains(rows[0].Mapping))
				RowsDataGridView.Rows[0].Selected = true;
		}

		static string Source(SdlSource source)
		{
			if (source == null)
				return "";
			switch (source.Kind)
			{
				case SdlSourceKind.Downloaded: return "Downloaded";
				case SdlSourceKind.File: return SdlDatabase.FileName;
				case SdlSourceKind.Environment: return SdlDatabase.EnvironmentVariable;
				default: return "Built in";
			}
		}

		/// <summary>The row selected, or null.</summary>
		public SdlMapping SelectedMapping
		{
			get
			{
				var row = RowsDataGridView.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault();
				var item = row == null ? null : row.DataBoundItem as SdlPresetRow;
				return item == null ? null : item.Mapping;
			}
		}

		/// <summary>The row selected as a preset for the device: its mapping boxes, nothing else; null when no row is selected.</summary>
		public PadSetting GetSelectedPreset()
		{
			var mapping = SelectedMapping;
			if (mapping == null)
				return null;
			var setting = SdlConverter.ToPadSetting(mapping, SdlDevice.From(_device)).Setting;
			setting.PadSettingChecksum = setting.CleanAndGetCheckSum();
			return setting;
		}
	}
}
