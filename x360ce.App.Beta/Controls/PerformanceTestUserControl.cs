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
	public partial class PerformanceTestUserControl : UserControl
	{
		public PerformanceTestUserControl()
		{
			InitializeComponent();
		}

		public void LoadSettings()
		{
			var o = SettingsManager.Options;
			EnableCheckBox.Checked = o.TestEnabled;
			GetDInputStatesCheckBox.Checked = o.TestGetDInputStates;
			SetXInputStatesCheckBox.Checked = o.TestSetXInputStates;
			GetXInputStatesCheckBox.Checked = o.TestGetXInputStates;
			UpdateInterfaceCheckBox.Checked = o.TestUpdateInterface;
			// Attach events.
			EnableCheckBox.CheckedChanged += EnableCheckBox_CheckedChanged;
			GetDInputStatesCheckBox.CheckedChanged += GetDInputStatesCheckBox_CheckedChanged;
			SetXInputStatesCheckBox.CheckedChanged += SetXInputStatesCheckBox_CheckedChanged;
			GetXInputStatesCheckBox.CheckedChanged += GetXInputStatesCheckBox_CheckedChanged;
			UpdateInterfaceCheckBox.CheckedChanged += UpdateInterfaceCheckBox_CheckedChanged;
		}

		private void UpdateInterfaceCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			throw new NotImplementedException();
		}

		private void GetXInputStatesCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			throw new NotImplementedException();
		}

		private void SetXInputStatesCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			throw new NotImplementedException();
		}

		private void GetDInputStatesCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			throw new NotImplementedException();
		}

		private void EnableCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			throw new NotImplementedException();
		}

		public void SaveSettings()
		{
			var o = SettingsManager.Options;
			EnableCheckBox.Checked = o.TestEnabled;
			o.TestGetDInputStates = GetDInputStatesCheckBox.Checked;
			o.TestSetXInputStates = SetXInputStatesCheckBox.Checked;
			o.TestGetXInputStates = GetXInputStatesCheckBox.Checked;
			o.TestUpdateInterface = UpdateInterfaceCheckBox.Checked;
		}

	}
}
