using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SharpDX.XInput;

namespace x360ce.App.Controls
{
	public partial class AxisToButtonUserControl : UserControl
	{

		public AxisToButtonUserControl()
		{
			InitializeComponent();
			controlsLink = new DeadZoneControlsLink(DeadZoneTrackBar, DeadZoneNumericUpDown, DeadZoneTextBox);
			controlsLink.ValueChanged += controlsLink_ValueChanged;
			arrowEnabledImage = ArrowPictureBox.Image;
			if (arrowEnabledImage != null)
			{
				arrowDisabledImage = AppHelper.GetDisabledImage((Bitmap)arrowEnabledImage);
			}
		}

		void controlsLink_ValueChanged(object sender, EventArgs e)
		{
		}

		DeadZoneControlsLink controlsLink;

		GamepadButtonFlags _GamepadButton;

		[DefaultValue(GamepadButtonFlags.None)] // Category("Appearance")
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		public GamepadButtonFlags GamepadButton
		{
			get { return _GamepadButton; }
			set
			{
				_GamepadButton = value;
				UpdateImage();
			}
		}

		Image arrowEnabledImage;
		Image arrowDisabledImage;

		Bitmap enabledImage;
		Bitmap disabledImage;

		void UpdateImage()
		{
			var name = GamepadButton.ToString();
			name = name.Replace("DPad", "D-Pad ");
			name = name.Replace("Shoulder", " Bumper");
			name = name.Replace("Thumb", " Stick Button");
			if (name.Length == 1) name += " Button";
			ButtonNameLabel.Text = name + ":";
			switch (GamepadButton)
			{
				case GamepadButtonFlags.A: enabledImage = Properties.Resources.Button_A; break;
				case GamepadButtonFlags.B: enabledImage = Properties.Resources.Button_B; break;
				case GamepadButtonFlags.X: enabledImage = Properties.Resources.Button_X; break;
				case GamepadButtonFlags.Y: enabledImage = Properties.Resources.Button_Y; break;
				case GamepadButtonFlags.Start: enabledImage = Properties.Resources.Button_Start; break;
				case GamepadButtonFlags.Back: enabledImage = Properties.Resources.Button_Back; break;
				case GamepadButtonFlags.DPadDown: enabledImage = Properties.Resources.Button_DPadDown; break;
				case GamepadButtonFlags.DPadLeft: enabledImage = Properties.Resources.Button_DPadLeft; break;
				case GamepadButtonFlags.DPadRight: enabledImage = Properties.Resources.Button_DPadRight; break;
				case GamepadButtonFlags.DPadUp: enabledImage = Properties.Resources.Button_DPadUp; break;
				case GamepadButtonFlags.LeftShoulder: enabledImage = Properties.Resources.Button_LeftShoulder; break;
				case GamepadButtonFlags.LeftThumb: enabledImage = Properties.Resources.Button_LeftThumb; break;
				case GamepadButtonFlags.RightShoulder: enabledImage = Properties.Resources.Button_RightShoulder; break;
				case GamepadButtonFlags.RightThumb: enabledImage = Properties.Resources.Button_RightThumb; break;
				default: enabledImage = null; break;
			}
			if (enabledImage != null)
			{
				disabledImage = AppHelper.GetDisabledImage(enabledImage);
			}
			ButtonImagePictureBox.BackgroundImage = ButtonImagePictureBox.Enabled ? enabledImage : disabledImage;
		}

		object monitorComboBoxLock = new object();
		ComboBox _MonitorComboBox;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ComboBox MonitorComboBox
		{
			get
			{
				return _MonitorComboBox;
			}
			set
			{
				lock (monitorComboBoxLock)
				{
					if (_MonitorComboBox != null)
					{
						_MonitorComboBox.SelectedIndexChanged -= _MonitorComboBox_TextChanged;
						_MonitorComboBox.TextChanged -= _MonitorComboBox_TextChanged;
					}
					_MonitorComboBox = value;
					if (_MonitorComboBox != null)
					{
						_MonitorComboBox.SelectedIndexChanged += _MonitorComboBox_TextChanged;
						_MonitorComboBox.TextChanged += _MonitorComboBox_TextChanged;
					}
				}
			}
		}

		void _MonitorComboBox_TextChanged(object sender, EventArgs e)
		{
			lock (monitorComboBoxLock)
			{
				var en = (_MonitorComboBox != null && (_MonitorComboBox.Text.Contains("Axis") || _MonitorComboBox.Text.Contains("Slider")));
				MappedAxisTextBox.Text = en ? _MonitorComboBox.Text : "";
				foreach (Control c in Controls)
				{
					c.Enabled = en;
				}
				// If enabled and value is 0.
				if (en && DeadZoneNumericUpDown.Value == 0)
				{
					DeadZoneNumericUpDown.Value = 8192;
				}
				else if (!en && DeadZoneNumericUpDown.Value > 0)
				{
					DeadZoneNumericUpDown.Value = 0;
				}
			}
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				controlsLink.Dispose();
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void ButtonImagePictureBox_EnabledChanged(object sender, EventArgs e)
		{
			ButtonImagePictureBox.BackgroundImage = ButtonImagePictureBox.Enabled ? enabledImage : disabledImage;
		}

		private void ArrowPictureBox_EnabledChanged(object sender, EventArgs e)
		{
			ArrowPictureBox.BackgroundImage = ArrowPictureBox.Enabled ? arrowEnabledImage : arrowDisabledImage;
		}

		State _gamepadState;
		Bitmap _markB;

		public void Refresh(State gamePadState, Bitmap markB)
		{
			_gamepadState = gamePadState;
			_markB = markB;
			ButtonImagePictureBox.Refresh();
		}

		private void ButtonImagePictureBox_Paint(object sender, PaintEventArgs e)
		{
			if (_markB != null)
			{
				var mW = -_markB.Width / 2;
				var mH = -_markB.Height / 2;
				var x = ButtonImagePictureBox.Width / 2;
				var y = ButtonImagePictureBox.Height / 2;
				var on = _gamepadState.Gamepad.Buttons.HasFlag(_GamepadButton);
				if (on) e.Graphics.DrawImage(_markB, x + mW, y + mH);
				Color c = on ? Color.Green : SystemColors.ControlText;
				if (ButtonNameLabel.ForeColor != c) ButtonNameLabel.ForeColor = c;
			}
		}

	}
}
