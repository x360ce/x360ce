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

		int GetValue(int v)
		{
			short absval = (short)((Math.Abs(v) + (((32767.0 / 2.0) - (((Math.Abs((Math.Abs(v)) - (32767.0 / 2.0)))))) * (v * 0.01))));
			v = v > 0 ? absval : -absval;
			return v;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="value">[-1.0;1.0]</param>
		/// <param name="strength">[-1.0;1.0]</param>
		/// <returns></returns>
		float GetValue(float value, float param)
		{
			var x = value;
			var invert = param < 0f;
			if (value > 0f) x = x * -1f;
			if (invert) x = 1f + x;
			var v = ((float)Math.Sqrt((float)1 - Math.Pow(x, 2f))) / 2f;
			v = invert ? 0.5f - v : v;
			if (value > 0) v = 1f - v;
			var value2 = value / 2f + 0.5f;
			return value2 + (v - value2) * Math.Abs(param);
		}

		Bitmap LastBackgroundImage = null;

		void RefreshBackgroundImage(object state)
		{
			var param = (float)state;
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
				float result = GetValue(value, param) * w;
				g.FillEllipse(transBrush, i - 1, w - result - 1, 2, 2);
			}
			LastBackgroundImage = bmp;
			Invoke(((MethodInvoker)delegate()
			{
				LinearPictureBox.BackgroundImage = Enabled ? LastBackgroundImage : null;
			}));
		}

		void RefreshBackgroundImageAsync()
		{
			var param = (float)LinearTrackBar.Value / (float)100;
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
