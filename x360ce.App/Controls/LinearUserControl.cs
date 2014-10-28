using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using JocysCom.ClassLibrary.Threading;
using System.Text.RegularExpressions;

namespace x360ce.App.Controls
{
	public partial class LinearUserControl : UserControl
	{
		public LinearUserControl()
		{
			InitializeComponent();
			updateTimer = new QueueTimer(500, 0);
			updateTimer.DoAction = RefreshBackgroundImage;
		}

		QueueTimer updateTimer;

		[Category("Appearance"), DefaultValue(0)]
		public string HeaderText
		{
			get { return MainGroupBox.Text; }
			set
			{
				MainGroupBox.Text = value;
			}
		}

		Bitmap LastBackgroundImage = null;

		void RefreshBackgroundImage(object state)
		{
			var linear = (int)state;
			var bmp = new Bitmap(128, 128);
			var g = Graphics.FromImage(bmp);
			var nativeBrush = new SolidBrush(System.Drawing.Color.Gray);
			var transBrush = new SolidBrush(System.Drawing.Color.Red);
			var w = bmp.Width;
			for (int i = 0; i < w; i++)
			{
				g.FillEllipse(nativeBrush, i - 1, w - i - 1, 2, 2);
				// Get value range [-1;1].
				float value = (float)i / (float)(w - 1) * 2f - 1f;
				short dInputValue = SharpDX.XInput.XInput.ConvertToShort(value);
				short result = SharpDX.XInput.XInput.GetThumbValue(dInputValue, 0, 0, linear);
				var resultInt = (int)((SharpDX.XInput.XInput.ConvertToFloat(result) + 1f) / 2 * w);
				g.FillEllipse(transBrush, i - 1, w - resultInt - 1, 2, 2);
			}
			LastBackgroundImage = bmp;
			Invoke(((MethodInvoker)delegate()
			{
				LinearPictureBox.BackgroundImage = Enabled ? LastBackgroundImage : null;
			}));
		}

		void RefreshBackgroundImageAsync()
		{
			var param = (int)LinearTrackBar.Value;
			updateTimer.AddToQueue(param);
		}

		private void LinearUserControl_Load(object sender, EventArgs e)
		{
			RefreshBackgroundImageAsync();
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (updateTimer != null) updateTimer.Dispose();
				if (components != null) components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void LinearTrackBar_ValueChanged(object sender, EventArgs e)
		{
			TrackBar control = (TrackBar)sender;
			ValueTextBox.Text = string.Format("{0} % ", control.Value);
			RefreshBackgroundImageAsync();
		}

		private void ValueComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			var box = (ComboBox)sender;
			var n = box.Text == "Disabled" ? 0 : int.Parse(new Regex("[^0-9-]").Replace(box.Text, ""));
			LinearTrackBar.Value = n;
		}

		private void LinearUserControl_EnabledChanged(object sender, EventArgs e)
		{
			LinearPictureBox.BackgroundImage = Enabled ? LastBackgroundImage : null;
		}


	}
}
