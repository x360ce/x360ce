using JocysCom.ClassLibrary.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace x360ce.App.Controls
{
	public partial class PromptForm : Form
	{
		public PromptForm()
		{
			InitializeComponent();
			if (IsDesignMode)
				return;
			SizeLabel.Visible = false;
		}

		internal bool IsDesignMode { get { return JocysCom.ClassLibrary.Controls.ControlsHelper.IsDesignMode(this); } }


		private void OkButton_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
		}

		private void CopyButton_Click(object sender, EventArgs e)
		{
			Clipboard.SetText(EditTextBox.Text);
		}

		private void EditTextBox_TextChanged(object sender, EventArgs e)
		{
			UpdateSizeLabel();
		}

		void UpdateSizeLabel()
		{
			var text = (EditTextBox.MaxLength - EditTextBox.Text.Length).ToString();
			ControlsHelper.SetText(SizeLabel, text);
			ControlsHelper.SetVisible(SizeLabel, EditTextBox.MaxLength > 0);
		}

		private void PromptForm_Load(object sender, EventArgs e)
		{
			UpdateSizeLabel();
		}

		private void EditTextBox_KeyUp(object sender, KeyEventArgs e)
		{
			UpdateSizeLabel();
		}
	}
}
