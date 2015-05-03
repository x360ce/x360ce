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
                case GamepadButtonFlags.A: ButtonImagePictureBox.BackgroundImage = Properties.Resources.Button_A; break;
                case GamepadButtonFlags.B: ButtonImagePictureBox.BackgroundImage = Properties.Resources.Button_B; break;
                case GamepadButtonFlags.X: ButtonImagePictureBox.BackgroundImage = Properties.Resources.Button_X; break;
                case GamepadButtonFlags.Y: ButtonImagePictureBox.BackgroundImage = Properties.Resources.Button_Y; break;
                case GamepadButtonFlags.Start: ButtonImagePictureBox.BackgroundImage = Properties.Resources.Button_Start; break;
                case GamepadButtonFlags.Back: ButtonImagePictureBox.BackgroundImage = Properties.Resources.Button_Back; break;
                case GamepadButtonFlags.DPadDown: ButtonImagePictureBox.BackgroundImage = Properties.Resources.Button_DPadDown; break;
                case GamepadButtonFlags.DPadLeft: ButtonImagePictureBox.BackgroundImage = Properties.Resources.Button_DPadLeft; break;
                case GamepadButtonFlags.DPadRight: ButtonImagePictureBox.BackgroundImage = Properties.Resources.Button_DPadRight; break;
                case GamepadButtonFlags.DPadUp: ButtonImagePictureBox.BackgroundImage = Properties.Resources.Button_DPadUp; break;
                case GamepadButtonFlags.LeftShoulder: ButtonImagePictureBox.BackgroundImage = Properties.Resources.Button_LeftShoulder; break;
                case GamepadButtonFlags.LeftThumb: ButtonImagePictureBox.BackgroundImage = Properties.Resources.Button_LeftThumb; break;
                case GamepadButtonFlags.RightShoulder: ButtonImagePictureBox.BackgroundImage = Properties.Resources.Button_RightShoulder; break;
                case GamepadButtonFlags.RightThumb: ButtonImagePictureBox.BackgroundImage = Properties.Resources.Button_RightThumb; break;
                default: ButtonImagePictureBox.Image = null; break;
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

    }
}
