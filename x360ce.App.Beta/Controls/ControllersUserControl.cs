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
	}
}
