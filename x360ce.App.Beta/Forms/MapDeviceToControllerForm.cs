using System;
using System.Windows.Forms;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	public partial class MapDeviceToControllerForm : BaseFormWithHeader
	{
		public MapDeviceToControllerForm()
		{
			InitializeComponent();
			if (IsDesignMode)
				return;
			SetHeaderSubject(Text);
			SetHeaderBody(MessageBoxIcon.None);
			ControllersPanel.MapDeviceToControllerMode = true;
		}

		public UserDevice[] SelectedDevices;

		private void OkButton_Click(object sender, EventArgs e)
		{
			SelectedDevices = ControllersPanel.GetSelected();
			DialogResult = DialogResult.OK;
		}

		private void CloseButton_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

	}
}
