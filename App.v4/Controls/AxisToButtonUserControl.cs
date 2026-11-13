using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.Themes;
using SharpDX.XInput;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace x360ce.App.Controls
{
	public partial class AxisToButtonUserControl : UserControl
	{

		public AxisToButtonUserControl()
		{
			InitializeComponent();
			// Scaled by the font once, after the designer's tables have resumed their layout: scaled in the designer, Windows
			// Forms scales a table as it resumes and then again with the control. The mode first, as changing it clears the
			// dimensions.
			SuspendLayout();
			AutoScaleMode = AutoScaleMode.Font;
			AutoScaleDimensions = new SizeF(6F, 13F);
			ResumeLayout(false);
			// The pictures stay square at the screen's scale: scaled by the font, a row is scaled more down than across.
			var side = (int)Math.Round(24 * ControlsHelper.DpiScale);
			ButtonImagePictureBox.Size = new Size(side, side);
			ArrowPictureBox.Size = new Size(side, side);
			LayoutPanel.Layout += LayoutPanel_Layout;
			FitNameColumn();
			UpdateArrow();
			if (ControlsHelper.IsDesignMode(this))
				return;
			controlsLink = new DeadZoneControlsLink(DeadZoneTrackBar, DeadZoneNumericUpDown, DeadZoneTextBox, short.MaxValue);
			controlsLink.ValueChanged += controlsLink_ValueChanged;
			// The button's picture and the arrow are the theme's, so they are taken again in a new one.
			EventHandler themeChanged = (s, e) =>
			{
				UpdateImage();
				UpdateArrow();
			};
			FormsTheme.ThemeChanged += themeChanged;
			Disposed += (s, e) => FormsTheme.ThemeChanged -= themeChanged;
		}

		private void controlsLink_ValueChanged(object sender, EventArgs e)
		{
		}

		private readonly DeadZoneControlsLink controlsLink;
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

		private Bitmap enabledImage;
		private Bitmap disabledImage;

		/// <summary>The name shown for a button, as the label and the accessible name spell it.</summary>
		static string ButtonName(GamepadButtonFlags button)
		{
			var name = button.ToString();
			name = name.Replace("DPad", "D-Pad ");
			name = name.Replace("Shoulder", " Bumper");
			name = name.Replace("Thumb", " Stick Button");
			if (name.Length == 1) name += " Button";
			return name;
		}

		/// <summary>
		/// Fifteen of these sit one under another, each laying itself out, so the name is as wide as the longest name any row
		/// shows, as it is drawn, and every slider starts on the same line; the name's column is sized by the name, so the
		/// table never measures it with Windows' room round its text and wraps it.
		/// </summary>
		void FitNameColumn()
		{
			var widest = 0;
			foreach (GamepadButtonFlags button in Enum.GetValues(typeof(GamepadButtonFlags)))
				widest = Math.Max(widest, FormsTheme.GetLabelWidth(ButtonNameLabel, ButtonName(button)));
			ButtonNameLabel.MinimumSize = new Size(widest, 0);
			// A name the theme sized keeps the width it was given until it is sized again, so it is given this one.
			if (!ButtonNameLabel.AutoSize)
				ButtonNameLabel.Width = widest;
		}

		/// <summary>
		/// Makes the row as tall as its tallest content with a control's margin above and below, so the rows stand a row's gap
		/// apart between their tallest content: the boxes, as tall as the font makes them, or the button's picture, which is
		/// drawn at its own size.
		/// </summary>
		/// <remarks>
		/// The room round the table is read from the control, not from the table's height, which waits for the table to be laid
		/// out: each pass before then would add that room again.
		/// </remarks>
		private void LayoutPanel_Layout(object sender, LayoutEventArgs e)
		{
			var height = Height - DisplayRectangle.Height + AppHelper.RowsHeight(LayoutPanel);
			if (Height != height)
				Height = height;
		}

		/// <summary>The theme draws the name once the row is on a themed window, which is before Windows makes the row.</summary>
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			FitNameColumn();
		}

		protected override void OnFontChanged(EventArgs e)
		{
			base.OnFontChanged(e);
			FitNameColumn();
		}

		private void UpdateImage()
		{
			var name = ButtonName(GamepadButton);
			ButtonNameLabel.Text = name;
			// The panel is one of fifteen alike, and only the button it belongs to tells them
			// apart. Without this a screen reader announces fifteen identical panels, and the
			// exported tree lists fifteen identical rows.
			AccessibleName = name + " press point";
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

		/// <summary>The arrow's picture in the theme in use, at twice the size it shows at 100%.</summary>
		internal Image ArrowPicture { get; private set; }

		/// <summary>Takes the arrow of the theme in use.</summary>
		void UpdateArrow()
		{
			ArrowPicture = ControlsHelper.GetOriginal(Properties.Resources.Axis_Arrow);
			ArrowPictureBox.Invalidate();
		}

		/// <summary>
		/// Draws the arrow in the middle of its box, at the screen's scale, brought down from its picture by averaging.
		/// </summary>
		/// <remarks>
		/// The program brings its other pictures down with a filter that sharpens (<see cref="ControlsHelper.ScaleImage(Image, Size)"/>).
		/// On the arrow, a flat grey close to the page's colour, that leaves a lighter ring just inside its edge, which
		/// reads as a border; averaging keeps the edge as the picture has it.
		/// </remarks>
		private void ArrowPictureBox_Paint(object sender, PaintEventArgs e)
		{
			var arrow = ArrowPicture;
			if (arrow == null)
				return;
			var scale = ControlsHelper.DpiScale / 2f;
			var w = (int)Math.Round(arrow.Width * scale);
			var h = (int)Math.Round(arrow.Height * scale);
			var place = new Rectangle((ArrowPictureBox.Width - w) / 2, (ArrowPictureBox.Height - h) / 2, w, h);
			var g = e.Graphics;
			g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
			g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
			using (var attributes = new System.Drawing.Imaging.ImageAttributes())
			{
				// The filter reads past the edge; mirrored there, the edge pixels are not mixed with nothing.
				attributes.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
				g.DrawImage(arrow, place, 0, 0, arrow.Width, arrow.Height, GraphicsUnit.Pixel, attributes);
			}
		}

		private readonly object monitorComboBoxLock = new object();
		private ComboBox _MonitorComboBox;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ComboBox MonitorComboBox
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

		private void _MonitorComboBox_TextChanged(object sender, EventArgs e)
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
			// Taken from the resources each time rather than kept, so it is the theme's.
			UpdateImage();
		}

		private State _gamepadState;
		private Bitmap _markB;

		public void Refresh(State gamePadState, Bitmap markB)
		{
			// Painted from whether this button is on, so only that needs a new picture. This runs on every state update.
			var on = (gamePadState.Gamepad.Buttons & _GamepadButton) == _GamepadButton;
			var changed = on != _shownOn || markB != _markB;
			_gamepadState = gamePadState;
			_markB = markB;
			_shownOn = on;
			if (changed)
				ButtonImagePictureBox.Invalidate();
		}

		bool _shownOn;

		private void ButtonImagePictureBox_Paint(object sender, PaintEventArgs e)
		{
			if (_markB != null)
			{
				// The mark at the screen's scale, in the middle of the picture.
				var scale = ControlsHelper.DpiScale;
				var w = _markB.Width * scale;
				var h = _markB.Height * scale;
				var x = ButtonImagePictureBox.Width / 2f;
				var y = ButtonImagePictureBox.Height / 2f;
				var on = _gamepadState.Gamepad.Buttons.HasFlag(_GamepadButton);
				if (on) e.Graphics.DrawImage(_markB, x - w / 2, y - h / 2, w, h);
				var c = on ? FormsTheme.GetColor("ForegroundActive") : SystemColors.ControlText;
				if (ButtonNameLabel.ForeColor != c) ButtonNameLabel.ForeColor = c;
			}
		}

	}
}
