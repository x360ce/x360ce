using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using Microsoft.DirectX.DirectInput;
using Microsoft.Xna.Framework.Input;

namespace x360ce.App.Controls
{
	public partial class PadControl : UserControl
	{
		public PadControl(int controllerIndex)
		{
			InitializeComponent();
			ControllerIndex = controllerIndex;
			// Add direct input user control.
			this.SuspendLayout();
			diControl = new DirectInputControl();
			diControl.Dock = DockStyle.Fill;
			DirectInputTabPage.Controls.Add(diControl);
			PadTabControl.TabPages.Remove(DirectInputTabPage);
			// PadKeyboardControl
			this.PadKeyboardControl = new x360ce.App.Controls.KeyboardControl();
			this.PadKeyboardControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.PadKeyboardControl.Location = new System.Drawing.Point(3, 3);
			this.PadKeyboardControl.Name = "PadKeyboardControl";
			this.PadKeyboardControl.Size = new System.Drawing.Size(614, 405);
			this.PadKeyboardControl.TabIndex = 1;
			this.KeyboardTabPage.Controls.Add(this.PadKeyboardControl);
			this.ResumeLayout();
		}

		private KeyboardControl PadKeyboardControl;



		public void InitPadControl()
		{
			// Initialize images.
			this.TopPictureBox.Image = topDisabledImage;
			this.FrontPictureBox.Image = frontDisabledImage;
			this.markB = new Bitmap(Helper.GetResource("Images.MarkButton.png"));
			this.markA = new Bitmap(Helper.GetResource("Images.MarkAxis.png"));
			this.markC = new Bitmap(Helper.GetResource("Images.MarkController.png"));
			this.markR = new Bitmap(Helper.GetResource("Images.bullet_ball_glass_red_16x16.png"));
			float rH = topDisabledImage.HorizontalResolution;
			float rV = topDisabledImage.VerticalResolution;
			// Make sure resolution is same everywhere so images won't be resized.
			this.markB.SetResolution(rH, rV);
			this.markA.SetResolution(rH, rV);
			this.markC.SetResolution(rH, rV);
			this.markR.SetResolution(rH, rV);
			// Add gamepad typed to ComboBox.
			var types = (GamePadType[])Enum.GetValues(typeof(GamePadType));
			foreach (var item in types) GamePadTypeComboBox.Items.Add(item);
			// Add force feedback typed to ComboBox.
			var fTypes = (ForceFeedBackType[])Enum.GetValues(typeof(ForceFeedBackType));
			foreach (var item in fTypes) ForceTypeComboBox.Items.Add(item);
			// Attach context strip with button names to every ComboBox on general tab.
			foreach (var control in GeneralTabPage.Controls)
			{
				if (control is ComboBox)
				{
					((ComboBox)control).ContextMenuStrip = DiMenuStrip;
				}
			}

			// hide experimental option.
			if (!Properties.Settings.Default.EnableKeyboardControl)
			{
				PadTabControl.TabPages.Remove(KeyboardTabPage);
			}
		}

		#region Recording

		public bool Recording;

		int recordignFlashPause;

		public void drawMarkR(PaintEventArgs e, Point position)
		{
			int rW = -this.markR.Width / 2;
			int rH = -this.markR.Height / 2;
			e.Graphics.DrawImage(this.markR, position.X + rW, position.Y + rH);
		}

		void RecordingStart()
		{
			Recording = true;
			recordignFlashPause = 0;
			CurrentCbx.ForeColor = SystemColors.GrayText;
			if (CurrentCbx == DPadComboBox)
			{
				mainForm.toolStripStatusLabel1.Text = "Recording - press any D-Pad button on your direct input device. Press ESC to cancel...";
			}
			else
			{
				mainForm.toolStripStatusLabel1.Text = "Recording - press button, move axis or slider on your direct input device. Press ESC to cancel...";
			}
		}

		public void RecordingStop(List<string> actions)
		{
			// If null passed then recording must stop imediately.
			if (actions == null)
			{
				CurrentCbx.Items.Clear();
				CurrentCbx.ForeColor = SystemColors.WindowText;
				//mainForm.toolStripStatusLabel1.Text = "Recording Cancelled";
				CurrentCbx = null;
				Recording = false;
				return;
			}
			// If actions are not null then recording is still in progress....
			if (actions.Count > 0)
			{
				if (CurrentCbx == DPadComboBox)
				{
					Regex rx = new Regex("(DPad [0-9]+)");
					if (rx.IsMatch(actions[0]))
					{
						actions[0] = rx.Match(actions[0]).Groups[0].Value;
					}
				}
				SettingManager.Current.SetComboBoxValue(CurrentCbx, actions[0]);
				CurrentCbx.ForeColor = SystemColors.WindowText;
				// Save setting and notify if vaue changed.
				if (SettingManager.Current.SaveSetting(CurrentCbx)) mainForm.NotifySettingsChange();
				//mainForm.toolStripStatusLabel1.Text = "Recorded: " + CurrentCbx.Text;
				CurrentCbx = null;
				Recording = false;
			}
		}


		#endregion

		#region Control ComboBox'es

		ComboBox CurrentCbx;
		MainForm mainForm { get { return (MainForm)Parent.Parent.Parent; } }

		DirectInputControl diControl;

		private void PadControl_Load(object sender, EventArgs e)
		{
		}

		private void ComboBox_DropDown(object sender, EventArgs e)
		{
			var cbx = (ComboBox)sender;
			// Force the combo box to re-create its window handle. This seems
			// to be the only way to prevent it from dropping down. Toggle the
			// property twice, which leaves it unchanged but calls RecreateHandle.
			cbx.IntegralHeight = !cbx.IntegralHeight;
			// Don't try to close DropDown imediatly or it will fail.
			BeginInvoke(new ComboBoxDropDownDelegate(ComboBoxDropDown), new object[] { cbx });
		}

		private delegate void ComboBoxDropDownDelegate(ComboBox cbx);

		private void ComboBoxDropDown(ComboBox cbx)
		{
			mainForm.SuspendEvents();
			cbx.DroppedDown = false;
			if (CurrentCbx == cbx)
			{
				CurrentCbx = null;
				cbx.ContextMenuStrip.Hide();
			}
			else
			{
				if (cbx == DPadComboBox) EnableDPadMenu(true);
				cbx.ContextMenuStrip.Show(cbx, new Point(0, cbx.Height), ToolStripDropDownDirection.Default);
				CurrentCbx = cbx;
			}
			if (cbx.Items.Count > 0)
			{
				cbx.SelectedIndex = 0;
			}
			mainForm.ResumeEvents();
		}

		#endregion

		#region Images

		private Bitmap markB;
		private Bitmap markA;
		private Bitmap markC;
		private Bitmap markR;

		Bitmap _topImage;
		Bitmap topImage
		{
			get { return _topImage = _topImage ?? new Bitmap(Helper.GetResource("Images.xboxControllerTop.png")); }
		}

		Bitmap _frontImage;
		Bitmap frontImage
		{
			get { return _frontImage = _frontImage ?? new Bitmap(Helper.GetResource("Images.xboxControllerFront.png")); }
		}

		Bitmap _topDisabledImage;
		Bitmap topDisabledImage
		{
			get
			{
				if (_topDisabledImage == null)
				{
					_topDisabledImage = (Bitmap)topImage.Clone();
					Helper.GrayScale(_topDisabledImage);
					Helper.Transparent(_topDisabledImage, 50);
				}
				return _topDisabledImage;
			}
		}

		Bitmap _frontDisabledImage;
		Bitmap frontDisabledImage
		{
			get
			{
				if (_frontDisabledImage == null)
				{
					_frontDisabledImage = (Bitmap)frontImage.Clone();
					Helper.GrayScale(_frontDisabledImage);
					Helper.Transparent(_frontDisabledImage, 50);
				}
				return _frontDisabledImage;
			}
		}



		private void TopPictureBox_Paint(object sender, PaintEventArgs e)
		{
			// Display controller.
			bool on = gamePadState.IsConnected;
			if (!on) return;
			// Half mark position adjust.
			int mW = -this.markB.Width / 2;
			int mH = -this.markB.Height / 2;
			// Button coordinates.
			Point shoulderLeft = new Point(43, 66);
			Point shoulderRight = new Point(this.FrontPictureBox.Width - shoulderLeft.X, shoulderLeft.Y);
			Point triggerLeft = new Point(63, 27);
			Point triggerRight = new Point(this.FrontPictureBox.Width - triggerLeft.X - 1, triggerLeft.Y);
			if (!Recording)
			{
				var tl = FloatToByte(gamePadState.Triggers.Left);
				var tr = FloatToByte(gamePadState.Triggers.Right);
				// Temp workaround: when initialized triggers have default value of 127);
				if (tl == 110 && tr == 110)
				{
					this.LeftTriggerTextBox.Text = "0";
					this.RightTriggerTextBox.Text = "0";
				}
				else
				{
					UpdateControl(LeftTriggerTextBox, tl.ToString());
					UpdateControl(RightTriggerTextBox, tr.ToString());
					on = tl > 0;
					setLabelColor(on, LeftTriggerLabel);
					if (on) e.Graphics.DrawImage(this.markB, triggerLeft.X + mW, triggerLeft.Y + mH);
					on = tr > 0;
					setLabelColor(on, RightTriggerLabel);
					if (on) e.Graphics.DrawImage(this.markB, triggerRight.X + mW, triggerRight.Y + mH);
				}
				on = gamePadState.IsButtonDown(Buttons.LeftShoulder);
				setLabelColor(on, LeftShoulderLabel);
				if (on) e.Graphics.DrawImage(this.markB, shoulderLeft.X + mW, shoulderLeft.Y + mH);
				on = gamePadState.IsButtonDown(Buttons.RightShoulder);
				setLabelColor(on, RightShoulderLabel);
				if (on) e.Graphics.DrawImage(this.markB, shoulderRight.X + mW, shoulderRight.Y + mH);
			}
			// Recording LED.
			if (Recording && recordignFlashPause < 8)
			{
				if (CurrentCbx == LeftTriggerComboBox) drawMarkR(e, triggerLeft);
				if (CurrentCbx == LeftShoulderComboBox) drawMarkR(e, shoulderLeft);
				if (CurrentCbx == RightTriggerComboBox) drawMarkR(e, triggerRight);
				if (CurrentCbx == RightShoulderComboBox) drawMarkR(e, shoulderRight);
			}
		}

		private void FrontPictureBox_Paint(object sender, PaintEventArgs e)
		{
			// Button coordinates.
			Point buttonY = new Point(196, 29);
			Point buttonX = new Point(178, 48);
			Point buttonB = new Point(215, 48);
			Point buttonA = new Point(196, 66);
			Point thumbLeft = new Point(59, 47);
			Point thumbRight = new Point(160, 88);
			Point dPad = new Point(92, 88);
			Point dPadUp = new Point(dPad.X, dPad.Y - 13);
			Point dPadLeft = new Point(dPad.X - 13, dPad.Y);
			Point dPadRight = new Point(dPad.X + 13, dPad.Y);
			Point dPadDown = new Point(dPad.X, dPad.Y + 13);
			Point buttonBig = new Point(127, 48);
			Point buttonBack = new Point(103, 48);
			Point buttonStart = new Point(152, 48);
			Point[] pads = new Point[4];
			pads[0] = new Point(116, 35);
			pads[1] = new Point(139, 35);
			pads[2] = new Point(116, 62);
			pads[3] = new Point(139, 62);
			// Display controller.
			bool on = gamePadState.IsConnected;
			if (!on) return;
			// Display controler index light.
			int mW = -this.markC.Width / 2;
			int mH = -this.markC.Height / 2;
			e.Graphics.DrawImage(this.markC, pads[ControllerIndex].X + mW, pads[ControllerIndex].Y + mH);

			float padSize = 22F / (float)(ushort.MaxValue);

			mW = -this.markB.Width / 2;
			mH = -this.markB.Height / 2;

			if (!Recording)
			{
				setLabelColor(this.leftX > 2000, LeftThumbAxisXLabel);
				if (this.leftX < -2000) LeftThumbAxisXLabel.ForeColor = Color.DarkRed;
				setLabelColor(this.leftY > 2000, LeftThumbAxisYLabel);
				if (this.leftY < -2000) LeftThumbAxisYLabel.ForeColor = Color.DarkRed;
				setLabelColor(this.rightX > 2000, RightThumbAxisXLabel);
				if (this.rightX < -2000) RightThumbAxisXLabel.ForeColor = Color.DarkRed;
				setLabelColor(this.rightY > 2000, RightThumbAxisYLabel);
				if (this.rightY < -2000) RightThumbAxisYLabel.ForeColor = Color.DarkRed;
				// Draw button state green led image.
				DrawState(Buttons.Y, buttonY, ButtonYLabel, e);
				DrawState(Buttons.X, buttonX, ButtonXLabel, e);
				DrawState(Buttons.B, buttonB, ButtonBLabel, e);
				DrawState(Buttons.A, buttonA, ButtonALabel, e);
				DrawState(Buttons.BigButton, buttonBig, null, e);
				DrawState(Buttons.Start, buttonStart, StartButtonLabel, e);
				DrawState(Buttons.Back, buttonBack, BackButtonLabel, e);
				DrawState(Buttons.DPadUp, dPadUp, DPadUpLabel, e);
				DrawState(Buttons.DPadDown, dPadDown, DPadDownLabel, e);
				DrawState(Buttons.DPadLeft, dPadLeft, DPadLeftLabel, e);
				DrawState(Buttons.DPadRight, dPadRight, DPadRightLabel, e);
				DrawState(Buttons.RightStick, thumbRight, RightThumbButtonLabel, e);
				DrawState(Buttons.LeftStick, thumbLeft, LeftThumbButtonLabel, e);
				// Draw axis state green cross image.
				e.Graphics.DrawImage(this.markA, (float)((thumbRight.X + mW) + (this.rightX * padSize)), (float)((thumbRight.Y + mH) + (-this.rightY * padSize)));
				e.Graphics.DrawImage(this.markA, (float)((thumbLeft.X + mW) + (this.leftX * padSize)), (float)((thumbLeft.Y + mH) + (-this.leftY * padSize)));
			}
			// Recording LED.
			if (Recording && recordignFlashPause < 8)
			{
				if (CurrentCbx == ButtonBackComboBox) drawMarkR(e, buttonBack);
				if (CurrentCbx == ButtonStartComboBox) drawMarkR(e, buttonStart);
				if (CurrentCbx == ButtonYComboBox) drawMarkR(e, buttonY);
				if (CurrentCbx == ButtonXComboBox) drawMarkR(e, buttonX);
				if (CurrentCbx == ButtonBComboBox) drawMarkR(e, buttonB);
				if (CurrentCbx == ButtonAComboBox) drawMarkR(e, buttonA);
				if (CurrentCbx == DPadUpComboBox) drawMarkR(e, dPadUp);
				if (CurrentCbx == DPadRightComboBox) drawMarkR(e, dPadRight);
				if (CurrentCbx == DPadDownComboBox) drawMarkR(e, dPadDown);
				if (CurrentCbx == DPadLeftComboBox) drawMarkR(e, dPadLeft);
				if (CurrentCbx == LeftThumbAxisXComboBox) drawMarkR(e, new Point(thumbLeft.X + 10, thumbLeft.Y));
				if (CurrentCbx == LeftThumbAxisYComboBox) drawMarkR(e, new Point(thumbLeft.X, thumbLeft.Y - 10));
				if (CurrentCbx == LeftThumbButtonComboBox) drawMarkR(e, thumbLeft);
				if (CurrentCbx == RightThumbAxisXComboBox) drawMarkR(e, new Point(thumbRight.X + 10, thumbRight.Y));
				if (CurrentCbx == RightThumbAxisYComboBox) drawMarkR(e, new Point(thumbRight.X, thumbRight.Y - 10));
				if (CurrentCbx == RightThumbButtonComboBox) drawMarkR(e, thumbRight);
			}
			if (recordignFlashPause == 16) recordignFlashPause = 0;
			recordignFlashPause++;
		}

		void DrawState(Buttons button, Point location, Label label, PaintEventArgs e)
		{
			var mW = -this.markB.Width / 2;
			var mH = -this.markB.Height / 2;
			var on = gamePadState.IsButtonDown(button);
			if (on) e.Graphics.DrawImage(this.markB, location.X + mW, location.Y + mH);
			if (label != null) setLabelColor(on, label);
		}

		void setLabelColor(bool on, Label label)
		{
			Color c = on ? Color.Green : SystemColors.ControlText;
			if (label.ForeColor != c) label.ForeColor = c;
		}

		#endregion

		#region Settings Map

		Dictionary<string, Control> _SettingsMap;
		public Dictionary<string, Control> SettingsMap
		{
			get { return _SettingsMap = _SettingsMap ?? GetSettingsMap(); }
		}

		public int ControllerIndex;

		/// <summary>
		/// Link control with INI key. Value/Text of controll will be automatically tracked and INI file updated.
		/// </summary>
		Dictionary<string, Control> GetSettingsMap()
		{
			Dictionary<string, Control> sm = new Dictionary<string, Control>();
			string section = string.Format(@"PAD{0}\", ControllerIndex + 1);
			// FakeAPI
			sm.Add(section + SettingName.ProductName, diControl.DeviceProductNameTextBox);
			sm.Add(section + SettingName.ProductGuid, diControl.DeviceProductGuidTextBox);
			sm.Add(section + SettingName.InstanceGuid, diControl.DeviceInstanceGuidTextBox);
			sm.Add(section + SettingName.GamePadType, GamePadTypeComboBox);
			sm.Add(section + SettingName.PassThrough, PassThroughCheckBox);
			// Triggers
			sm.Add(section + SettingName.RightTrigger, RightTriggerComboBox);
			sm.Add(section + SettingName.RightTriggerDeadZone, RightTriggerDeadZoneTrackBar);
			sm.Add(section + SettingName.LeftTrigger, LeftTriggerComboBox);
			sm.Add(section + SettingName.LeftTriggerDeadZone, LeftTriggerDeadZoneTrackBar);
			// D-Pad
			sm.Add(section + SettingName.DPad, DPadComboBox);
			sm.Add(section + SettingName.DPadUp, DPadUpComboBox);
			sm.Add(section + SettingName.DPadDown, DPadDownComboBox);
			sm.Add(section + SettingName.DPadLeft, DPadLeftComboBox);
			sm.Add(section + SettingName.DPadRight, DPadRightComboBox);
			// Axis To D-Pad
			sm.Add(section + SettingName.AxisToDPadEnabled, AxisToDPadEnabledCheckBox);
			sm.Add(section + SettingName.AxisToDPadDeadZone, AxisToDPadDeadZoneTrackBar);
			sm.Add(section + SettingName.AxisToDPadOffset, AxisToDPadOffsetTrackBar);
			// Buttons
			sm.Add(section + SettingName.ButtonBack, ButtonBackComboBox);
			sm.Add(section + SettingName.ButtonStart, ButtonStartComboBox);
			sm.Add(section + SettingName.ButtonA, ButtonAComboBox);
			sm.Add(section + SettingName.ButtonB, ButtonBComboBox);
			sm.Add(section + SettingName.ButtonX, ButtonXComboBox);
			sm.Add(section + SettingName.ButtonY, ButtonYComboBox);
			// Shoulders.
			sm.Add(section + SettingName.LeftShoulder, LeftShoulderComboBox);
			sm.Add(section + SettingName.RightShoulder, RightShoulderComboBox);
			// Left Thumb
			sm.Add(section + SettingName.LeftThumbAxisX, LeftThumbAxisXComboBox);
			sm.Add(section + SettingName.LeftThumbAxisY, LeftThumbAxisYComboBox);
			sm.Add(section + SettingName.LeftThumbRight, LeftThumbRightComboBox);
			sm.Add(section + SettingName.LeftThumbLeft, LeftThumbLeftComboBox);
			sm.Add(section + SettingName.LeftThumbUp, LeftThumbUpComboBox);
			sm.Add(section + SettingName.LeftThumbDown, LeftThumbDownComboBox);
			sm.Add(section + SettingName.LeftThumbButton, LeftThumbButtonComboBox);
			sm.Add(section + SettingName.LeftThumbDeadZoneX, LeftThumbDeadZoneXTrackBar);
			sm.Add(section + SettingName.LeftThumbDeadZoneY, LeftThumbDeadZoneYTrackBar);
			sm.Add(section + SettingName.LeftThumbAntiDeadZoneX, LeftThumbXAntiDeadZoneNumericUpDown);
			sm.Add(section + SettingName.LeftThumbAntiDeadZoneY, LeftThumbYAntiDeadZoneNumericUpDown);
			// Right Thumb
			sm.Add(section + SettingName.RightThumbAxisX, RightThumbAxisXComboBox);
			sm.Add(section + SettingName.RightThumbAxisY, RightThumbAxisYComboBox);
			sm.Add(section + SettingName.RightThumbRight, RightThumbRightComboBox);
			sm.Add(section + SettingName.RightThumbLeft, RightThumbLeftComboBox);
			sm.Add(section + SettingName.RightThumbUp, RightThumbUpComboBox);
			sm.Add(section + SettingName.RightThumbDown, RightThumbDownComboBox);
			sm.Add(section + SettingName.RightThumbButton, RightThumbButtonComboBox);
			sm.Add(section + SettingName.RightThumbDeadZoneX, RightThumbDeadZoneXTrackBar);
			sm.Add(section + SettingName.RightThumbDeadZoneY, RightThumbDeadZoneYTrackBar);
			sm.Add(section + SettingName.RightThumbAntiDeadZoneX, RightThumbXAntiDeadZoneNumericUpDown);
			sm.Add(section + SettingName.RightThumbAntiDeadZoneY, RightThumbYAntiDeadZoneNumericUpDown);
			// Force Feedback
			sm.Add(section + SettingName.ForceEnable, ForceEnableCheckBox);
			sm.Add(section + SettingName.ForceType, ForceTypeComboBox);
			sm.Add(section + SettingName.ForceSwapMotor, ForceSwapMotorCheckBox);
			sm.Add(section + SettingName.ForceOverall, ForceOverallTrackBar);
			sm.Add(section + SettingName.LeftMotorPeriod, LeftMotorPeriodTrackBar);
			sm.Add(section + SettingName.RightMotorPeriod, RightMotorPeriodTrackBar);
			return sm;
		}

		#endregion

		private float leftX;
		private float leftY;
		private float rightX;
		private float rightY;

		GamePadState gamePadState;
		//XINPUT_GAMEPAD GamePad;
		Guid instanceGuid;

		public void UpdateFromDirectInput(Device device)
		{
			List<string> actions = diControl.UpdateFrom(device);
			if (Recording) RecordingStop(actions);

			var contains = PadTabControl.TabPages.Contains(DirectInputTabPage);
			if (device == null && contains)
			{
				PadTabControl.TabPages.Remove(DirectInputTabPage);
			}
			if (device != null && !contains)
			{
				PadTabControl.TabPages.Add(DirectInputTabPage);
			}
			if (device != null)
			{
				UpdateControl(DirectInputTabPage, device.DeviceInformation.InstanceName);
			}
			// if this is different device;
			if (!Helper.IsSameDevice(device, instanceGuid))
			{
				Guid iGuid = Guid.Empty;
				try { iGuid = device.DeviceInformation.InstanceGuid; }
				catch (Exception) { if (SettingManager.Current.IsDebugMode) throw; }
				instanceGuid = (device == null) ? Guid.Empty : iGuid;
				ResetDiMenuStrip(device);
			}
		}

		public void UpdateFromXInput(GamePadState state)
		{
			var wasConnected = gamePadState.IsConnected;
			var nowConnected = state.IsConnected;
			gamePadState = state;
			// If form was disabled and no data is comming then just return.
			if (!wasConnected && !nowConnected) return;
			// If device connection changed then...
			if (wasConnected != nowConnected)
			{
				if (nowConnected)
				{
					// Enable form.
					this.FrontPictureBox.Image = frontImage;
					this.TopPictureBox.Image = topImage;
				}
				else
				{
					// Disable form.
					this.FrontPictureBox.Image = frontDisabledImage;
					this.TopPictureBox.Image = topDisabledImage;
					
				}
			}
			if (nowConnected)
			{
				this.leftX = FloatToInt(state.ThumbSticks.Left.X);
				this.leftY = FloatToInt(state.ThumbSticks.Left.Y);
				this.rightX = FloatToInt(state.ThumbSticks.Right.X);
				this.rightY = FloatToInt(state.ThumbSticks.Right.Y);
			}
			else
			{
				this.leftX = 0;
				this.leftY = 0;
				this.rightX = 0;
				this.rightY = 0;
			}
			UpdateControl(LeftThumbTextBox, string.Format("{0};{1}", this.leftX, this.leftY));
			UpdateControl(RightThumbTextBox, string.Format("{0};{1}", this.rightX, this.rightY));
			this.TopPictureBox.Refresh();
			this.FrontPictureBox.Refresh();
		}

		// Check left thumbStick
		public float FloatToInt(float v)
		{
			// -1 to 1 int16.MinValue int16.MaxValue.
			return (UInt16)Math.Round((((double)v + 1) / 2) * (double)UInt16.MaxValue) + Int16.MinValue;
		}

		// Check left thumbStick
		public float FloatToByte(float v)
		{
			// -1 to 1 int16.MinValue int16.MaxValue.
			return (Byte)Math.Round((double)v * (double)Byte.MaxValue);
		}

		// Use this to reduce flicekring.
		public void UpdateControl(Control control, string text)
		{
			if (control.Text != text) control.Text = text;
		}

		string cRecord = "[Record]";
		string cEmpty = "<empty>";


		// Function is recreted as soon as new DirectInput Device is available.
		public void ResetDiMenuStrip(Device device)
		{
			DiMenuStrip.Items.Clear();
			ToolStripMenuItem mi;
			mi = new ToolStripMenuItem(cEmpty);
			mi.ForeColor = SystemColors.ControlDarkDark;
			mi.Click += new EventHandler(DiMenuStrip_Click);
			DiMenuStrip.Items.Add(mi);
			// Return if direct input device is not available.
			if (device == null) return;
			// Add [Record] button.
			mi = new ToolStripMenuItem(cRecord);
			mi.Image = new Bitmap(Helper.GetResource("Images.bullet_ball_glass_red_16x16.png"));
			mi.Click += new EventHandler(DiMenuStrip_Click);
			DiMenuStrip.Items.Add(mi);
			// Add Buttons.
			mi = new ToolStripMenuItem("Buttons");
			DiMenuStrip.Items.Add(mi);
			CreateItems(mi, "Button {0}", "b{0}", device.Caps.NumberButtons);
			// Add Axes.
			mi = new ToolStripMenuItem("Axes");
			DiMenuStrip.Items.Add(mi);
			var axisCount = device.Caps.NumberAxes * 2;
			CreateItems(mi, "Inverted", "IAxis {0}", "a-{0}", axisCount);
			CreateItems(mi, "Inverted Half", "IHAxis {0}", "x-{0}", axisCount);
			CreateItems(mi, "Half", "HAxis {0}", "x{0}", axisCount);
			CreateItems(mi, "Axis {0}", "a{0}", axisCount);
			// Add Sliders.            
			mi = new ToolStripMenuItem("Sliders");
			DiMenuStrip.Items.Add(mi);
			CreateItems(mi, "Inverted", "ISlider {0}", "s-{0}", 8);
			CreateItems(mi, "Inverted Half", "IHSlider {0}", "h-{0}", 8);
			CreateItems(mi, "Half", "HSlider {0}", "h{0}", 8);
			CreateItems(mi, "Slider {0}", "s{0}", 8);
			// Add D-Pads.
			mi = new ToolStripMenuItem("DPads");
			DiMenuStrip.Items.Add(mi);
			CreateItems(mi, "DPad {0}", "p{0}", device.Caps.NumberPointOfViews);
			// Add D-Pad Top, Right, Bottom, Left button.
			for (int i = 0; i < mi.DropDownItems.Count; i++)
			{
				var item = (ToolStripMenuItem)mi.DropDownItems[i];
				foreach (string p in Enum.GetNames(typeof(DPadEnum)))
				{
					var item2 = CreateItem("{0} {2}", "{1}{2}", item.Text, item.Tag, p);
					item.DropDownItems.Add(item2);
				}
			}
		}

		void CreateItems(ToolStripMenuItem parent, string subMenu, string text, string tag, int count)
		{
			var smi = new ToolStripMenuItem(subMenu);
			parent.DropDownItems.Add(smi);
			CreateItems(smi, text, tag, count);
		}

		void CreateItems(ToolStripMenuItem parent, string text, string tag, int count)
		{
			for (int i = 0; i < count; i++)
			{
				var item = CreateItem(text, tag, i + 1);
				parent.DropDownItems.Add(item);
			}
		}

		ToolStripMenuItem CreateItem(string text, string tag, params object[] args)
		{
			var item = new ToolStripMenuItem(string.Format(text, args));
			item.Tag = string.Format(tag, args);
			item.DisplayStyle = ToolStripItemDisplayStyle.Text;
			item.Padding = new Padding(0);
			item.Margin = new Padding(0);
			item.Click += new EventHandler(DiMenuStrip_Click);
			return item;
		}


		private void DiMenuStrip_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			EnableDPadMenu(false);
		}

		void DiMenuStrip_Click(object sender, EventArgs e)
		{
			ToolStripMenuItem item = (ToolStripMenuItem)sender;
			Regex rx = new Regex("^(DPad [0-9]+)$");
			// If this this DPad parent menu.
			if (rx.IsMatch(item.Text))
			{
				if (CurrentCbx == DPadComboBox)
				{
					SettingManager.Current.SetComboBoxValue(CurrentCbx, item.Text);
					CurrentCbx = null;
					DiMenuStrip.Close();
				}
			}
			else
			{
				if (item.Text == cRecord)
				{
					RecordingStart();
				}
				else if (item.Text == cEmpty)
				{
					SettingManager.Current.SetComboBoxValue(CurrentCbx, string.Empty);
					CurrentCbx = null;
				}
				else
				{
					SettingManager.Current.SetComboBoxValue(CurrentCbx, item.Text);
					CurrentCbx = null;
				}
			}
		}

		public void EnableDPadMenu(bool enable)
		{
			foreach (ToolStripMenuItem item in DiMenuStrip.Items)
			{
				if (!item.Text.StartsWith(cRecord)
					&& !item.Text.StartsWith(cEmpty)
					&& !item.Text.StartsWith("DPad"))
				{
					item.Visible = !enable;
				}
				if (item.Text.StartsWith("DPad"))
				{
					if (item.HasDropDownItems)
					{
						foreach (ToolStripMenuItem l1 in item.DropDownItems)
						{
							foreach (ToolStripMenuItem l2 in l1.DropDownItems) l2.Visible = !enable;
						}
					}
				}
			}
		}

		private void ForceOverallTrackBar_ValueChanged(object sender, EventArgs e)
		{
			TrackBar control = (TrackBar)sender;
			ForceOverallTextBox.Text = string.Format("{0} % ", control.Value);
		}


		private void LeftTriggerDeadZoneTrackBar_ValueChanged(object sender, EventArgs e)
		{
			TrackBar control = (TrackBar)sender;
			LeftTriggerDeadZoneTextBox.Text = string.Format("{0} % ", control.Value);
		}


		private void RightTriggerDeadZoneTrackBar_ValueChanged(object sender, EventArgs e)
		{
			TrackBar control = (TrackBar)sender;
			RightTriggerDeadZoneTextBox.Text = string.Format("{0} % ", control.Value);
		}

		private void LeftThumbDeadZoneXTrackBar_ValueChanged(object sender, EventArgs e)
		{
			TrackBar control = (TrackBar)sender;
			LeftThumbDeadZoneXTextBox.Text = string.Format("{0} % ", control.Value);
		}

		private void LeftThumbDeadZoneYTrackBar_ValueChanged(object sender, EventArgs e)
		{
			TrackBar control = (TrackBar)sender;
			LeftThumbDeadZoneYTextBox.Text = string.Format("{0} % ", control.Value);
		}


		private void RightThumbDeadZoneXTrackBar_ValueChanged(object sender, EventArgs e)
		{
			TrackBar control = (TrackBar)sender;
			RightThumbDeadZoneXTextBox.Text = string.Format("{0} % ", control.Value);
		}

		private void RightThumbDeadZoneYTrackBar_ValueChanged(object sender, EventArgs e)
		{
			TrackBar control = (TrackBar)sender;
			RightThumbDeadZoneYTextBox.Text = string.Format("{0} % ", control.Value);
		}

		private void MotorTrackBar_ValueChanged(object sender, EventArgs e)
		{
			if (gamePadState == null) return;
			UpdateForceFeedBack();
		}

		private void MotorPeriodTrackBar_ValueChanged(object sender, EventArgs e)
		{
			if (gamePadState == null) return;
			UpdateForceFeedBack2();
		}

		public void UpdateForceFeedBack2()
		{
			// Convert Direct Input Period force feedback effect parameter value.
			int leftMotorPeriod = (int)LeftMotorPeriodTrackBar.Value * 5;
			int rightMotorPeriod = (int)RightMotorPeriodTrackBar.Value * 5;
			LeftMotorPeriodTextBox.Text = string.Format("{0} ", leftMotorPeriod);
			RightMotorPeriodTextBox.Text = string.Format("{0} ", rightMotorPeriod);
		}

		public void UpdateForceFeedBack()
		{
			// Convert 100% trackbar to MotorSpeed's 0 - 1.0
			float leftMotor = (float)LeftMotorTestTrackBar.Value / 100F;
			float rightMotor = (float)RightMotorTestTrackBar.Value / 100F;
			LeftMotorTestTextBox.Text = string.Format("{0} % ", LeftMotorTestTrackBar.Value);
			RightMotorTestTextBox.Text = string.Format("{0} % ", RightMotorTestTrackBar.Value);
			GamePad.SetVibration((Microsoft.Xna.Framework.PlayerIndex)mainForm.ControllerIndex, leftMotor, rightMotor);
			//UnsafeNativeMethods.Enable(false);
			//UnsafeNativeMethods.Enable(true);
		}

		private void AxisToDPadOffsetTrackBar_ValueChanged(object sender, EventArgs e)
		{
			TrackBar control = (TrackBar)sender;
			AxisToDPadOffsetTextBox.Text = string.Format("{0} % ", control.Value);
		}

		private void AxisToDPadDeadZoneTrackBar_ValueChanged(object sender, EventArgs e)
		{
			TrackBar control = (TrackBar)sender;
			AxisToDPadDeadZoneTextBox.Text = string.Format("{0} % ", control.Value);
		}

		private void ThumbAntiDeadZoneComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			int XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE = 7849;
			int XINPUT_GAMEPAD_RIGHT_THUMB_DEADZONE = 8689;
			var n = ThumbAntiDeadZoneComboBox.Text == "Disabled"
				? 0
				: float.Parse(new Regex("[^0-9]").Replace(ThumbAntiDeadZoneComboBox.Text, "")) / 100;
			LeftThumbXAntiDeadZoneNumericUpDown.Value = (int)((float)XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE * n);
			LeftThumbYAntiDeadZoneNumericUpDown.Value = (int)((float)XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE * n);
			RightThumbXAntiDeadZoneNumericUpDown.Value = (int)((float)XINPUT_GAMEPAD_RIGHT_THUMB_DEADZONE * n);
			RightThumbYAntiDeadZoneNumericUpDown.Value = (int)((float)XINPUT_GAMEPAD_RIGHT_THUMB_DEADZONE * n);
		}

	}
}
