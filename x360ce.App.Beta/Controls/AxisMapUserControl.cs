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
using x360ce.App.DInput;

namespace x360ce.App.Controls
{
	public partial class AxisMapUserControl : UserControl
	{
		public AxisMapUserControl()
		{
			InitializeComponent();
			InitPaintObjects();
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

		TargetType _TargetType;

		[Category("Appearance"), DefaultValue(TargetType.LeftThumbX)]
		public TargetType TargetType
		{
			get { return _TargetType; }
			set
			{
				_TargetType = value;
			}
		}

		Bitmap LastBackgroundImage = null;

		void RefreshBackgroundImageAsync()
		{
			if (updateTimer == null)
				return;
			var param = (int)SensitivityTrackBar.Value;
			updateTimer.DoActionNow(param);
			SensitivityLabel.Text = SensitivityCheckBox.Checked
				? "Sensitivity - Make more sensitive in the center:"
				: "Sensitivity - Make less sensitive in the center:";
		}

		private void LinearUserControl_Load(object sender, EventArgs e)
		{
			updateTimer = new QueueTimer(500, 0);
			updateTimer.DoWork += updateTimer_DoWork;
			var maxValue = TargetType == TargetType.LeftTrigger || TargetType == TargetType.RightTrigger
				? byte.MaxValue : short.MaxValue;
			deadzoneLink = new DeadZoneControlsLink(DeadZoneTrackBar, DeadZoneNumericUpDown, DeadZoneTextBox, maxValue);
			deadzoneLink.ValueChanged += deadzoneLink_ValueChanged;
			antiDeadzoneLink = new DeadZoneControlsLink(AntiDeadZoneTrackBar, AntiDeadZoneNumericUpDown, AntiDeadZoneTextBox, maxValue);
			antiDeadzoneLink.ValueChanged += deadzoneLink_ValueChanged;
			RefreshBackgroundImageAsync();
		}

		void updateTimer_DoWork(object sender, QueueTimerEventArgs e)
		{
			CreateBacgroundPicture();
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (deadzoneLink != null)
					deadzoneLink.Dispose();
				if (antiDeadzoneLink != null)
					antiDeadzoneLink.Dispose();
				if (updateTimer != null)
					updateTimer.Dispose();
				if (components != null)
					components.Dispose();
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
			DInputValueLabel.Text = dInput.ToString();
			XInputValueLabel.Text = xInput.ToString();
			_invert = invert;
			_half = half;
			_dInput = dInput;
			_xInput = xInput;
			MainPictureBox.Refresh();
		}

		public void InitPaintObjects()
		{
			xInputPath = new SolidBrush(System.Drawing.Color.FromArgb(255, Color.Red));
			xInputPoint = new SolidBrush(System.Drawing.Color.FromArgb(255, Color.Blue));
			dInputPoint = new SolidBrush(System.Drawing.Color.FromArgb(255, Color.Green));
			// Create thin lines.
			var xInputLineBrush = new SolidBrush(System.Drawing.Color.FromArgb(32, Color.Blue));
			xInputLine = new Pen(xInputLineBrush);
			var dInputLineBrush = new SolidBrush(System.Drawing.Color.FromArgb(32, Color.Green));
			dInputLine = new Pen(dInputLineBrush);
			var nInputLineBrush = new SolidBrush(System.Drawing.Color.FromArgb(32, Color.Gray));
			nInputLine = new Pen(nInputLineBrush);
		}

		SolidBrush xInputPath;
		SolidBrush xInputPoint;
		SolidBrush dInputPoint;
		Pen xInputLine;
		Pen dInputLine;
		Pen nInputLine;

		private void CreateBacgroundPicture()
		{
			int deadZone = 0;
			int antiDeadZone = 0;
			int sensitivity = 0;
			Invoke(((MethodInvoker)delegate ()
			{
				deadZone = (int)DeadZoneNumericUpDown.Value;
				antiDeadZone = (int)AntiDeadZoneNumericUpDown.Value;
				sensitivity = (int)SensitivityNumericUpDown.Value;
			}));
			var borders = MainPictureBox.BorderStyle == System.Windows.Forms.BorderStyle.None ? 0 : 2;
			var w = MainPictureBox.Width - borders;
			var h = MainPictureBox.Height - borders;
			var bmp = new Bitmap(w, h);
			var g = Graphics.FromImage(bmp);
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
			g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
			var wF = (float)w;
			var hF = (float)h;
			// Draw grey line from bottom-left to top-right.
			g.DrawLine(nInputLine, 0f, (float)h, (float)w, 0f);
			for (float i = 0; i <= wF; i += 0.5f)
			{
				// Convert Image X position [0;m] to DInput position [0;65535].
				var dInputValue = ConvertHelper.ConvertRangeF(0, wF, ushort.MinValue, ushort.MaxValue, i);
				var result = ConvertHelper.GetThumbValue(dInputValue, deadZone, antiDeadZone, sensitivity, _invert, _half);
				var rounded = result >= -1f && result <= 1f;
				// Convert XInput Y position [-32768;32767] to image size [0;m].
				var y = ConvertHelper.ConvertRangeF(short.MinValue, short.MaxValue, 0, hF, result);
				var radius = 0.5f;
				var shift = -0.5f;
				// Round on zero so deadzone line will look nice.
				var ir = rounded ? (float)Math.Round(i) : i;
				var yr = rounded ? (float)Math.Round(y) - radius : y;
				// Put red dot where XInput dot must travel. Use radius to fix exlipse position.
				g.FillEllipse(xInputPath, ir - radius + shift, hF - yr - radius + shift, radius * 2f, radius * 2f);
			}
			Invoke(((MethodInvoker)delegate ()
			{
				LastBackgroundImage = bmp;
				MainPictureBox.BackgroundImage = Enabled ? LastBackgroundImage : null;
			}));
		}

		private void LinearPictureBox_Paint(object sender, PaintEventArgs e)
		{
			var image = LastBackgroundImage;
			if (image == null) return;
			var w = (float)image.Width;
			var h = (float)image.Width;
			// Convert DInput to image position.
			var di = ConvertHelper.ConvertRangeF(0, ushort.MaxValue, 0, w, _dInput);
			// Convert XInput to image position.
			var xi = ConvertHelper.ConvertRangeF(short.MinValue, short.MaxValue, 0, h, _xInput);
			if (_invert) di = w - di;
			var g = e.Graphics;
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
			g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
			// Round to make it snap.
			var dir = (float)Math.Round(di, 0);
			var xir = (float)Math.Round(xi, 0);
			// Draw lines.
			g.DrawLine(dInputLine, 0, h - dir, w, h - dir);
			g.DrawLine(xInputLine, 0, w - xir, w, w - xir);
			// Draw dots.
			var radius = 2f;
			var shift = -0.5f;
			// Use radius to fix exlipse position.
			g.FillEllipse(dInputPoint, di - radius + shift, h - di - radius + shift, radius * 2f, radius * 2f);
			g.FillEllipse(xInputPoint, di - radius + shift, w - xi - radius + shift, radius * 2f, radius * 2f);
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
			var xDeadZone = 0;
			switch (TargetType)
			{
				case TargetType.LeftTrigger:
				case TargetType.RightTrigger:
					xDeadZone = XInput.XINPUT_GAMEPAD_TRIGGER_THRESHOLD;
					break;
				case TargetType.LeftThumbX:
				case TargetType.LeftThumbY:
					xDeadZone = XInput.XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE;
					break;
				case TargetType.RightThumbX:
				case TargetType.RightThumbY:
					xDeadZone = XInput.XINPUT_GAMEPAD_RIGHT_THUMB_DEADZONE;
					break;
				default:
					break;
			}
			var deadZone = int.Parse(values[1]);
			var antiDeadZone = int.Parse(values[2]);
			var sensitivity = int.Parse(values[3]);
			// Move focus away from below controls, so that their value can be changed.
			ActiveControl = SensitivityCheckBox;
			DeadZoneTrackBar.Value = deadZone;
			AntiDeadZoneNumericUpDown.Value = (decimal)((float)xDeadZone * (float)antiDeadZone / 100f);
		}


	}
}

