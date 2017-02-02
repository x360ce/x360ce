using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace x360ce.App.Controls
{
	public partial class ControllersUserControl : UserControl
	{
		public ControllersUserControl()
		{
			InitializeComponent();
		}

		private void ControllersUserControl_Load(object sender, EventArgs e)
		{
			ControllersDataGridView.AutoGenerateColumns = false;
			ControllersDataGridView.DataSource = SettingManager.DiDevices;
		}

		private void ControllersDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			var grid = (DataGridView)sender;
			var item = ((DiDevice)grid.Rows[e.RowIndex].DataBoundItem);
			if (e.ColumnIndex == grid.Columns[MyIconColumn.Name].Index)
			{
				e.Value = item.IsOnline
					? Properties.Resources.bullet_square_glass_green
					: Properties.Resources.bullet_square_glass_grey;
			}
		}
	}
}
