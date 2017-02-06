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
	public partial class MapDeviceToControllerForm : BaseForm
	{
		public MapDeviceToControllerForm()
		{
			InitializeComponent();
			if (IsDesignMode) return;
			SetHeaderSubject(Text);
			SetHeaderBody(MessageBoxIcon.Information, "");
		}

		private void OkButton_Click(object sender, EventArgs e)
		{
			var grid = AvailableDInputDevicesDataGridView;
			var row = grid.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault();
			SelectedDevice = row == null
				? null
				: (DiDevice)row.DataBoundItem;
			DialogResult = DialogResult.OK;
		}

		private void CloseButton_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
		}

		private void MapDeviceToControllerForm_Load(object sender, EventArgs e)
		{
			AvailableDInputDevicesDataGridView.AutoGenerateColumns = false;
			// Show available devices.
			AvailableDInputDevicesDataGridView.DataSource = SettingManager.UserDevices;
		}

		private void AvailableDInputDevicesDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			var grid = (DataGridView)sender;
			var did = ((DiDevice)grid.Rows[e.RowIndex].DataBoundItem);
			if (e.ColumnIndex == grid.Columns[InstanceIdColumn.Name].Index)
			{
				// Hide device Instance GUID from public eyes. Show part of checksum.
				e.Value = EngineHelper.GetID(did.InstanceGuid);
			}
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				AvailableDInputDevicesDataGridView.DataSource = null;
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		public DiDevice SelectedDevice;

	}
}
