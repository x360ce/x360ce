using JocysCom.ClassLibrary.Controls;
using SharpDX.XInput;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for AxisToButtonControl.xaml
	/// </summary>
	public partial class AxisToButtonControl : UserControl
	{
		public AxisToButtonControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			if (ControlsHelper.IsDesignMode(this))
				return;
		}

		private void controlsLink_ValueChanged(object sender, EventArgs e)
		{
		}

		private TrackBarUpDownTextBoxLink controlsLink;
		private GamepadButtonFlags _GamepadButton;

		[DefaultValue(GamepadButtonFlags.None)] // Category("Appearance")
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		public GamepadButtonFlags GamepadButton
		{
			get => _GamepadButton;
			set
			{
				_GamepadButton = value;
				UpdateImage();
			}
		}

		private ImageSource arrowEnabledImage;
		private ImageSource arrowDisabledImage;
		private ImageSource enabledImage;
		private ImageSource disabledImage;

		private void UpdateImage()
		{
			var name = GamepadButton.ToString();
			name = name.Replace("DPad", "D-Pad ");
			name = name.Replace("Shoulder", " Bumper");
			name = name.Replace("Thumb", " Stick Button");
			if (name.Length == 1) name += " Button";
			ButtonNameLabel.Content = name + ":";
			System.Drawing.Bitmap image;
			switch (GamepadButton)
			{
				case GamepadButtonFlags.A: image = Properties.Resources.Button_A; break;
				case GamepadButtonFlags.B: image = Properties.Resources.Button_B; break;
				case GamepadButtonFlags.X: image = Properties.Resources.Button_X; break;
				case GamepadButtonFlags.Y: image = Properties.Resources.Button_Y; break;
				case GamepadButtonFlags.Start: image = Properties.Resources.Button_Start; break;
				case GamepadButtonFlags.Back: image = Properties.Resources.Button_Back; break;
				case GamepadButtonFlags.DPadDown: image = Properties.Resources.Button_DPadDown; break;
				case GamepadButtonFlags.DPadLeft: image = Properties.Resources.Button_DPadLeft; break;
				case GamepadButtonFlags.DPadRight: image = Properties.Resources.Button_DPadRight; break;
				case GamepadButtonFlags.DPadUp: image = Properties.Resources.Button_DPadUp; break;
				case GamepadButtonFlags.LeftShoulder: image = Properties.Resources.Button_LeftShoulder; break;
				case GamepadButtonFlags.LeftThumb: image = Properties.Resources.Button_LeftThumb; break;
				case GamepadButtonFlags.RightShoulder: image = Properties.Resources.Button_RightShoulder; break;
				case GamepadButtonFlags.RightThumb: image = Properties.Resources.Button_RightThumb; break;
				default: image = null; break;
			}
			if (image == null)
			{
				enabledImage = null;
				disabledImage = null;
			}
			else
			{
				enabledImage = ControlsHelper.GetImageSource(image);
				disabledImage = ControlsHelper.GetImageSource(AppHelper.GetDisabledImage(image));
			}
			// Set image.
			ButtonImagePictureBox.Source = ButtonImagePictureBox.IsEnabled
				? enabledImage
				: disabledImage;
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public System.Windows.Forms.ComboBox MonitorComboBox
		{
			get => _MonitorComboBox;
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
		private readonly object monitorComboBoxLock = new object();
		private System.Windows.Forms.ComboBox _MonitorComboBox;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public TextBox MonitorComboBoxWpf
		{
			get => _MonitorComboBoxWpf;
			set
			{
				lock (monitorComboBoxLock)
				{
					if (_MonitorComboBox != null)
						_MonitorComboBox.TextChanged -= _MonitorComboBox_TextChanged;
					_MonitorComboBoxWpf = value;
					if (_MonitorComboBox != null)
						_MonitorComboBox.TextChanged += _MonitorComboBox_TextChanged;
				}
			}
		}
		TextBox _MonitorComboBoxWpf;


		private void _MonitorComboBox_TextChanged(object sender, EventArgs e)
		{
			lock (monitorComboBoxLock)
			{
				var en = false;
				var text = "";
				if (_MonitorComboBox != null)
				{
					en = _MonitorComboBox.Text.Contains("Axis") || _MonitorComboBox.Text.Contains("Slider");
					text = en ? _MonitorComboBox.Text : "";
				}
				if (_MonitorComboBoxWpf != null)
				{
					en = _MonitorComboBoxWpf.Text.Contains("Axis") || _MonitorComboBoxWpf.Text.Contains("Slider");
					text = en ? _MonitorComboBoxWpf.Text : "";
				}
				MappedAxisTextBox.Text = text;
				IsEnabled = en;
				// If enabled and value is 0.
				if (en && DeadZoneNumericUpDown.Value == 0)
					DeadZoneNumericUpDown.Value = 8192;
				else if (!en && DeadZoneNumericUpDown.Value > 0)
					DeadZoneNumericUpDown.Value = 0;
				ButtonImagePictureBox.IsEnabled = en;
				ArrowPictureBox.IsEnabled = en;
			}
		}

		private void ButtonImagePictureBox_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			ButtonImagePictureBox.Source = ButtonImagePictureBox.IsEnabled
				? enabledImage
				: disabledImage;
		}

		private void ArrowPictureBox_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			ArrowPictureBox.Source = ArrowPictureBox.IsEnabled
				? arrowEnabledImage
				: arrowDisabledImage;
		}

		public void Refresh(State gamePadState)
		{
			var on = gamePadState.Gamepad.Buttons.HasFlag(_GamepadButton);
			ControlsHelper.SetVisible(ButtonStatusImage, on);
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowLoad(this))
				return;
			controlsLink = new TrackBarUpDownTextBoxLink(DeadZoneTrackBar, DeadZoneNumericUpDown, DeadZoneTextBox, 0, short.MaxValue);
			controlsLink.ValueChanged += controlsLink_ValueChanged;
			arrowEnabledImage = ControlsHelper.GetImageSource(Properties.Resources.arrow_right_16x16);
			arrowDisabledImage = ControlsHelper.GetImageSource(AppHelper.GetDisabledImage(Properties.Resources.arrow_right_16x16));
		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowUnload(this))
				return;
			controlsLink.ValueChanged -= controlsLink_ValueChanged;
			controlsLink.Dispose();
			controlsLink = null;
			arrowEnabledImage = null;
			arrowDisabledImage = null;
		}

	}
}
