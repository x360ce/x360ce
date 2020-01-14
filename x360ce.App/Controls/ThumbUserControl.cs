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
using SharpDX.XInput;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	public partial class ThumbUserControl : UserControl
	{
		public ThumbUserControl()
		{
			InitializeComponent();
			updateTimer = new QueueTimer(500, 0);
			updateTimer.DoWork = RefreshBackgroundImage;
			deadzoneLink = new DeadZoneControlsLink(DeadZoneTrackBar, DeadZoneNumericUpDown, DeadZoneTextBox);
			deadzoneLink.ValueChanged += deadzoneLink_ValueChanged;
			antiDeadzoneLink = new DeadZoneControlsLink(AntiDeadZoneTrackBar, AntiDeadZoneNumericUpDown, AntiDeadZoneTextBox);
			antiDeadzoneLink.ValueChanged += deadzoneLink_ValueChanged;
		}

		void deadzoneLink_ValueChanged(object sender, EventArgs e)
		{
			RefreshBackgroundImageAsync();
		}

		DeadZoneControlsLink deadzoneLink;
		DeadZoneControlsLink antiDeadzoneLink;

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

		ThumbIndex _ThumbIndex;

		[Category("Appearance"), DefaultValue(ThumbIndex.LeftX)]
		public ThumbIndex ThumbIndex
		{
			get { return _ThumbIndex; }
			set
			{
				_ThumbIndex = value;
			}
		}

		Bitmap LastBackgroundImage = null;

		void RefreshBackgroundImage(object sender, object state)
		{
			int deadZone = 0;
			int antiDeadZone = 0;
			int sensitivity = 0;
			Invoke((Action)delegate ()
			{
				deadZone = (int)DeadZoneNumericUpDown.Value;
				antiDeadZone = (int)AntiDeadZoneNumericUpDown.Value;
				sensitivity = (int)SensitivityNumericUpDown.Value;
			});
			var borders = MainPictureBox.BorderStyle == System.Windows.Forms.BorderStyle.None ? 0 : 2;
			var w = MainPictureBox.Width - borders;
			var h = MainPictureBox.Height - borders;
			var bmp = new Bitmap(w, h);
			var g = Graphics.FromImage(bmp);
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
			g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
			var dInputBrush = new SolidBrush(System.Drawing.Color.Gray);
			var dInputPen = new Pen(dInputBrush);
			var xInputBrush = new SolidBrush(System.Drawing.Color.Red);
			var nInputBrush = new SolidBrush(System.Drawing.Color.FromArgb(32, 128, 128, 128));
			var nInputPen = new Pen(nInputBrush);
			var radius = 1f;
			g.DrawLine(nInputPen, 0, h, w, 0);
			var m = (float)w;
			for (float i = 0; i <= m; i += 0.5f)
			{
				// Convert Image X position [0;m] to DInput position [0;65535].
				var dInputValue = ConvertHelper.ConvertRangeF(0, w, ushort.MinValue, ushort.MaxValue, i);
				var result = ConvertHelper.GetThumbValue(dInputValue, deadZone, antiDeadZone, sensitivity, _invert, _half);
				// Convert XInput Y position [-32768;32767] to image size [0;m].
				var y = ConvertHelper.ConvertRangeF(short.MinValue, short.MaxValue, 0, h, result);
				g.FillEllipse(xInputBrush, i - 1f, h - y - 1f, radius, radius);
			}
			Invoke((Action)delegate ()
			{
				LastBackgroundImage = bmp;
				MainPictureBox.BackgroundImage = Enabled ? LastBackgroundImage : null;
			});
		}

		void RefreshBackgroundImageAsync()
		{
			var param = (int)SensitivityTrackBar.Value;
			updateTimer.DoActionNow(param);
			SensitivityLabel.Text = SensitivityCheckBox.Checked
				? "Sensitivity - Make more sensitive in the center:"
				: "Sensitivity - Make less sensitive in the center:";
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
				deadzoneLink.Dispose();
				antiDeadzoneLink.Dispose();
				if (updateTimer != null) updateTimer.Dispose();
				if (components != null) components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void ThumbUserControl_EnabledChanged(object sender, EventArgs e)
		{
			MainPictureBox.BackgroundImage = Enabled ? LastBackgroundImage : null;
			MainPictureBox.BackColor = Enabled ? System.Drawing.Color.White : System.Drawing.SystemColors.Control;
		}

		bool _invert;
		bool _half;
		int _dInput;
		int _xInput;

		public void DrawPoint(int dInput, int xInput, bool invert, bool half)
		{
			DInputValueLabel.Text = (dInput + short.MinValue).ToString();
			XInputValueLabel.Text = xInput.ToString();
			_invert = invert;
			_half = half;
			_dInput = dInput;
			_xInput = xInput;
			MainPictureBox.Refresh();
		}

		private void LinearPictureBox_Paint(object sender, PaintEventArgs e)
		{
			var image = LastBackgroundImage;
			if (image == null) return;
			var w = (float)image.Width;
			var h = (float)image.Width;
			// Convert DInput to image position.
			var di = ConvertHelper.ConvertRangeF(0, ushort.MaxValue, 0, w, _dInput);
			// Convert DInput to image position.
			var xi = ((float)(_xInput - short.MinValue) / (float)ushort.MaxValue * (w - 1f));
			var xInputPoint = new SolidBrush(System.Drawing.Color.FromArgb(255, 0, 0, 255));
			var xInputBrush = new SolidBrush(System.Drawing.Color.FromArgb(32, 0, 0, 255));
			var xInputPen = new Pen(xInputBrush);
			var dInputPoint = new SolidBrush(System.Drawing.Color.FromArgb(255, 0, 128, 0));
			var dInputBrush = new SolidBrush(System.Drawing.Color.FromArgb(32, 0, 128, 0));
			var dInputPen = new Pen(dInputBrush);
			var nInputBrush = new SolidBrush(System.Drawing.Color.FromArgb(32, 128, 128, 128));
			var nInputPen = new Pen(nInputBrush);
			if (_invert) di = w - di - 1f;
			var g = e.Graphics;
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
			g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
			var x1 = (float)Math.Round(di, 0);
			var y1 = (float)Math.Round(w - xi - 1f, 0);
			g.DrawLine(nInputPen, x1, 0, x1, h);
			g.DrawLine(dInputPen, 0, h - x1 - 1f, w, h - x1 - 1f);
			g.DrawLine(xInputPen, 0, y1, w, y1);
			var radius = 2f;
			g.FillEllipse(dInputPoint, x1 - radius, (h - x1 - 1f) - radius, radius * 2f, radius * 2f);
			g.FillEllipse(xInputPoint, x1 - radius, y1 - radius, radius * 2f, radius * 2f);
		}

		#region Sensitivity Controls

		object SensitivityLock = new object();

		private void SensitivityTrackBar_ValueChanged(object sender, EventArgs e)
		{
			var control = (TrackBar)sender;
			lock (SensitivityLock)
			{
				SensitivityNumericUpDown.ValueChanged -= new System.EventHandler(SensitivityNumericUpDown_ValueChanged);
				SensitivityCheckBox.CheckedChanged -= new System.EventHandler(SensitivityCheckBox_CheckedChanged);
				var percent = (int)control.Value;
				var invert = SensitivityCheckBox.Checked;
				var value = invert ? -percent : percent;
				var percentString = string.Format("{0} % ", percent);
				// Update percent TextBox.
				if (SensitivityTextBox.Text != percentString) SensitivityTextBox.Text = percentString;
				// Update NumericUpDown.
				if (SensitivityNumericUpDown.Value != percent) SensitivityNumericUpDown.Value = value;
				// Update BheckBox.
				if (SensitivityCheckBox.Checked != invert) SensitivityCheckBox.Checked = invert;
				SensitivityCheckBox.CheckedChanged += new System.EventHandler(SensitivityCheckBox_CheckedChanged);
				SensitivityNumericUpDown.ValueChanged += new System.EventHandler(SensitivityNumericUpDown_ValueChanged);
			}
			RefreshBackgroundImageAsync();
		}

		private void SensitivityNumericUpDown_ValueChanged(object sender, EventArgs e)
		{
			var control = (NumericUpDown)sender;
			lock (SensitivityLock)
			{
				SensitivityTrackBar.ValueChanged -= new System.EventHandler(SensitivityTrackBar_ValueChanged);
				SensitivityCheckBox.CheckedChanged -= new System.EventHandler(SensitivityCheckBox_CheckedChanged);
				var value = (int)control.Value;
				var invert = value < 0;
				var percent = invert ? -value : value;
				var percentString = string.Format("{0} % ", percent);
				// Update percent TextBox.
				if (SensitivityTextBox.Text != percentString) SensitivityTextBox.Text = percentString;
				// Update TrackBar.
				if (SensitivityTrackBar.Value != value) SensitivityTrackBar.Value = percent;
				// Update CheckBox.
				if (SensitivityCheckBox.Checked != invert) SensitivityCheckBox.Checked = invert;
				SensitivityCheckBox.CheckedChanged += new System.EventHandler(SensitivityCheckBox_CheckedChanged);
				SensitivityTrackBar.ValueChanged += new System.EventHandler(SensitivityTrackBar_ValueChanged);
			}
			RefreshBackgroundImageAsync();
		}

		private void SensitivityCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			var control = (CheckBox)sender;
			lock (SensitivityLock)
			{
				SensitivityNumericUpDown.ValueChanged -= new System.EventHandler(SensitivityNumericUpDown_ValueChanged);
				SensitivityTrackBar.ValueChanged -= new System.EventHandler(SensitivityTrackBar_ValueChanged);
				SensitivityNumericUpDown.Value = -SensitivityNumericUpDown.Value;
				SensitivityTrackBar.ValueChanged += new System.EventHandler(SensitivityTrackBar_ValueChanged);
				SensitivityNumericUpDown.ValueChanged += new System.EventHandler(SensitivityNumericUpDown_ValueChanged);
			}
			RefreshBackgroundImageAsync();
		}

		#endregion

		private void P_X_Y_Z_MenuItem_Click(object sender, EventArgs e)
		{
			var c = (ToolStripMenuItem)sender;
			var values = c.Name.Split('_');
			var xDeadZone = ThumbIndex == SharpDX.XInput.ThumbIndex.LeftX || ThumbIndex == SharpDX.XInput.ThumbIndex.LeftX
				? Controller.XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE
				: Controller.XINPUT_GAMEPAD_RIGHT_THUMB_DEADZONE;
			var deadZone = int.Parse(values[1]);
			var antiDeadZone = int.Parse(values[2]);
			var sensitivity = int.Parse(values[3]);
			DeadZoneTrackBar.Value = deadZone;
			AntiDeadZoneNumericUpDown.Value = (decimal)((float)xDeadZone * (float)antiDeadZone / 100f);
		}


	}
}

