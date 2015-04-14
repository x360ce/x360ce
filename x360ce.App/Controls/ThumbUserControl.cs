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

namespace x360ce.App.Controls
{
    public partial class ThumbUserControl : UserControl
    {
        public ThumbUserControl()
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

        void RefreshBackgroundImage(object state)
        {
            int deadZone = 0;
            int antiDeadZone = 0;
            int sensitivity = 0;
            Invoke(((MethodInvoker)delegate()
            {
                deadZone = (int)DeadZoneNumericUpDown.Value;
                antiDeadZone = (int)AntiDeadZoneNumericUpDown.Value;
                sensitivity = (int)SensitivityNumericUpDown.Value;
            }));
            var borders = MainPictureBox.BorderStyle == System.Windows.Forms.BorderStyle.None ? 0 : 2;
            var w =  MainPictureBox.Width - borders;
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
            var radius = 0.5f;
            g.DrawLine(nInputPen, 0, h, w, 0);
            for (float i = 0; i < w; i += 0.5f)
            {
                var m = (float)w;
                // Get value range [-1;1].
                float value = i / (m - 1f) * 2f - 1f;
                short dInputValue = SharpDX.XInput.XInput.ConvertToShort(value);
                short result = SharpDX.XInput.XInput.GetThumbValue(dInputValue, deadZone, antiDeadZone, sensitivity);
                var resultInt = ((SharpDX.XInput.XInput.ConvertToFloat(result) + 1f) / 2f * m);
                var x1 = i;
                var y1 = m - resultInt - 1f;
                g.FillEllipse(xInputBrush, x1, y1, radius * 2f, radius * 2f);
            }
            Invoke(((MethodInvoker)delegate()
            {
                LastBackgroundImage = bmp;
                MainPictureBox.BackgroundImage = Enabled ? LastBackgroundImage : null;
            }));
        }

        void RefreshBackgroundImageAsync()
        {
            var param = (int)SensitivityTrackBar.Value;
            updateTimer.AddToQueue(param);
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
        int _dInput;
        int _xInput;

        public void DrawPoint(int dInput, int xInput, bool invert)
        {
            DInputValueLabel.Text = (dInput + short.MinValue).ToString();
            XInputValueLabel.Text = xInput.ToString();
            _invert = invert;
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
            var radius = 2f;
            var di = ((float)_dInput / (float)ushort.MaxValue * (w - 1f));
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
            g.FillEllipse(dInputPoint, x1 - radius, (h - x1 - 1f) - radius, radius * 2f, radius * 2f);
            g.FillEllipse(xInputPoint, x1 - radius, y1 - radius, radius * 2f, radius * 2f);
        }

        #region Dead Zone

        object DeadZoneLock = new object();

        private void DeadZoneTrackBar_ValueChanged(object sender, EventArgs e)
        {
            var control = (TrackBar)sender;
            lock (DeadZoneLock)
            {
                DeadZoneNumericUpDown.ValueChanged -= new System.EventHandler(DeadZoneNumericUpDown_ValueChanged);
                var percent = control.Value;
                var percentString = string.Format("{0} % ", percent);
                // Update percent TextBox.
                if (DeadZoneTextBox.Text != percentString) DeadZoneTextBox.Text = percentString;
                // Update NumericUpDown.
                var value = (decimal)Math.Round((float)percent / 100f * short.MaxValue);
                if (DeadZoneNumericUpDown.Value != value) DeadZoneNumericUpDown.Value = value;
                DeadZoneNumericUpDown.ValueChanged += new System.EventHandler(DeadZoneNumericUpDown_ValueChanged);
            }
            RefreshBackgroundImageAsync();
        }


        private void DeadZoneNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            var control = (NumericUpDown)sender;
            lock (DeadZoneLock)
            {
                DeadZoneTrackBar.ValueChanged -= new System.EventHandler(DeadZoneTrackBar_ValueChanged);
                var percent = (int)Math.Round(((float)control.Value / (float)short.MaxValue) * 100f);
                var percentString = string.Format("{0} % ", percent);
                // Update percent TextBox.
                if (DeadZoneTextBox.Text != percentString) DeadZoneTextBox.Text = percentString;
                // Update TrackBar;
                if (DeadZoneTrackBar.Value != percent) DeadZoneTrackBar.Value = percent;
                DeadZoneTrackBar.ValueChanged += new System.EventHandler(DeadZoneTrackBar_ValueChanged);
            }
            RefreshBackgroundImageAsync();
        }

        #endregion

        #region Anti Dead Zone

        object AntiDeadZoneLock = new object();

        private void AntiDeadZoneTrackBar_ValueChanged(object sender, EventArgs e)
        {
            var control = (TrackBar)sender;
            lock (AntiDeadZoneLock)
            {
                AntiDeadZoneNumericUpDown.ValueChanged -= new System.EventHandler(AntiDeadZoneNumericUpDown_ValueChanged);
                var percent = control.Value;
                var percentString = string.Format("{0} % ", percent);
                // Update percent TextBox.
                if (AntiDeadZoneTextBox.Text != percentString) AntiDeadZoneTextBox.Text = percentString;
                // Update NumericUpDown.
                var value = (decimal)Math.Round((float)percent / 100f * short.MaxValue);
                if (AntiDeadZoneNumericUpDown.Value != value) AntiDeadZoneNumericUpDown.Value = value;
                AntiDeadZoneNumericUpDown.ValueChanged += new System.EventHandler(AntiDeadZoneNumericUpDown_ValueChanged);
            }
            RefreshBackgroundImageAsync();
        }


        private void AntiDeadZoneNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            var control = (NumericUpDown)sender;
            lock (AntiDeadZoneLock)
            {
                AntiDeadZoneTrackBar.ValueChanged -= new System.EventHandler(AntiDeadZoneTrackBar_ValueChanged);
                var percent = (int)Math.Round(((float)control.Value / (float)short.MaxValue) * 100f);
                var percentString = string.Format("{0} % ", percent);
                // Update percent TextBox.
                if (AntiDeadZoneTextBox.Text != percentString) AntiDeadZoneTextBox.Text = percentString;
                // Update TrackBar;
                if (AntiDeadZoneTrackBar.Value != percent) AntiDeadZoneTrackBar.Value = percent;
                AntiDeadZoneTrackBar.ValueChanged += new System.EventHandler(AntiDeadZoneTrackBar_ValueChanged);
            }
            RefreshBackgroundImageAsync();
        }

        #endregion

        #region Anti Dead Zone

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


        const int XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE = 7849;
        const int XINPUT_GAMEPAD_RIGHT_THUMB_DEADZONE = 8689;

        private void P_X_Y_Z_MenuItem_Click(object sender, EventArgs e)
        {
            var c = (ToolStripMenuItem)sender;
            var values = c.Name.Split('_');
                var xDeadZone = ThumbIndex == SharpDX.XInput.ThumbIndex.LeftX || ThumbIndex == SharpDX.XInput.ThumbIndex.LeftX
                    ? XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE
                    : XINPUT_GAMEPAD_RIGHT_THUMB_DEADZONE;
                var deadZone = int.Parse(values[1]);
                var antiDeadZone = int.Parse(values[2]);
                var sensitivity = int.Parse(values[3]);
                DeadZoneTrackBar.Value = deadZone;
                AntiDeadZoneNumericUpDown.Value = (decimal)((float)xDeadZone * (float)antiDeadZone / 100f);
        }

    
    }
}

