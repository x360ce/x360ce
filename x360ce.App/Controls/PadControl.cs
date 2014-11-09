using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using SharpDX.DirectInput;
using SharpDX.XInput;

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
            this.ResumeLayout();
        }

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
            var types = (SharpDX.DirectInput.DeviceType[])Enum.GetValues(typeof(SharpDX.DirectInput.DeviceType));
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
        }

        #region Recording

        bool Recording;
        Regex dPadRx = new Regex("(DPad [0-9]+)");
        bool drawRecordingImage;
        object recordingLock = new object();

        private void RecordingTimer_Tick(object sender, EventArgs e)
        {
            drawRecordingImage = !drawRecordingImage;
        }

        void drawMarkR(PaintEventArgs e, Point position)
        {
            int rW = -this.markR.Width / 2;
            int rH = -this.markR.Height / 2;
            e.Graphics.DrawImage(this.markR, position.X + rW, position.Y + rH);
        }

        void StartRecording()
        {
            lock (recordingLock)
            {
                // If recording is not in progress then return.
                if (Recording) return;
                Recording = true;
                drawRecordingImage = true;
                RecordingTimer.Start();
                CurrentCbx.ForeColor = SystemColors.GrayText;
                mainForm.StatusTimerLabel.Text = (CurrentCbx == DPadComboBox)
                     ? "Recording - press any D-Pad button on your direct input device. Press ESC to cancel..."
                     : "Recording - press button, move axis or slider on your direct input device. Press ESC to cancel...";
            }
        }

        public bool StopRecording(List<string> actions = null)
        {
            lock (recordingLock)
            {

                // If recording is not in progress then return false.
                if (!Recording) return false;
                // Must stop recording if null passed or at least one action was recorded.
                var stop = (actions == null || actions.Count > 0);
                // If recording must stop then...
                if (stop)
                {
                    Recording = false;
                    RecordingTimer.Stop();
                    // If stop was initiaded before action was recorded then...
                    if (actions == null)
                    {
                        CurrentCbx.Items.Clear();
                    }
                    // If action was recorded then...
                    else
                    {
                        // Get first recorded action.
                        var name = actions[0];
                        // If this is DPad ComboBox and recorded action is DPad then...
                        if (CurrentCbx == DPadComboBox && dPadRx.IsMatch(name))
                        {
                            name = dPadRx.Match(name).Groups[0].Value;
                        }
                        SettingManager.Current.SetComboBoxValue(CurrentCbx, name);
                        // Save setting and notify if vaue changed.
                        if (SettingManager.Current.SaveSetting(CurrentCbx)) mainForm.NotifySettingsChange();
                    }
                    CurrentCbx.ForeColor = SystemColors.WindowText;
                    CurrentCbx = null;
                }
                return true;
            }
        }

        #endregion

        #region Control ComboBox'es

        ComboBox CurrentCbx;
        MainForm mainForm { get { return (MainForm)Parent.Parent.Parent; } }

        DirectInputControl diControl;

        void PadControl_Load(object sender, EventArgs e)
        {
            //LeftThumbXAntiDeadZoneComboBox.SelectedIndex = 0;
            //LeftThumbYAntiDeadZoneComboBox.SelectedIndex = 0;
            //RightThumbXAntiDeadZoneComboBox.SelectedIndex = 0;
            //RightThumbYAntiDeadZoneComboBox.SelectedIndex = 0;
        }

        void ComboBox_DropDown(object sender, EventArgs e)
        {
            var cbx = (ComboBox)sender;
            var oldLeft = cbx.Left;
            // Move default dropdown away from the screen.
            cbx.Left = -10000;
            var del = new ComboBoxDropDownDelegate(ComboBoxDropDown);
            BeginInvoke(del, new object[] { cbx, oldLeft });
        }

        delegate void ComboBoxDropDownDelegate(ComboBox cbx, int oldLeft);

        void ComboBoxDropDown(ComboBox cbx, int oldLeft)
        {
            //mainForm.SuspendEvents();
            //if (cbx.Items.Count > 0) cbx.DroppedDown = false;
            cbx.IntegralHeight = !cbx.IntegralHeight;
            cbx.IntegralHeight = !cbx.IntegralHeight;
            cbx.Left = oldLeft;
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
            //mainForm.ResumeEvents();
        }

        #endregion

        #region Images

        Bitmap markB;
        Bitmap markA;
        Bitmap markC;
        Bitmap markR;

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

        void TopPictureBox_Paint(object sender, PaintEventArgs e)
        {
            // Display controller.
            bool on = gamePadStateIsConnected;
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
                var tl = gamePadState.Gamepad.LeftTrigger;
                var tr = gamePadState.Gamepad.RightTrigger;
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
                on = gamePadState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder);
                setLabelColor(on, LeftShoulderLabel);
                if (on) e.Graphics.DrawImage(this.markB, shoulderLeft.X + mW, shoulderLeft.Y + mH);
                on = gamePadState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.RightShoulder);
                setLabelColor(on, RightShoulderLabel);
                if (on) e.Graphics.DrawImage(this.markB, shoulderRight.X + mW, shoulderRight.Y + mH);
            }
            // If recording is in progress and recording image must be drawn then...
            else if (drawRecordingImage)
            {
                // Draw recording mark on controller.
                if (CurrentCbx == LeftTriggerComboBox) drawMarkR(e, triggerLeft);
                if (CurrentCbx == LeftShoulderComboBox) drawMarkR(e, shoulderLeft);
                if (CurrentCbx == RightTriggerComboBox) drawMarkR(e, triggerRight);
                if (CurrentCbx == RightShoulderComboBox) drawMarkR(e, shoulderRight);
            }
        }

        void FrontPictureBox_Paint(object sender, PaintEventArgs e)
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
            Point buttonGuide = new Point(127, 48);
            Point buttonBack = new Point(103, 48);
            Point buttonStart = new Point(152, 48);
            Point[] pads = new Point[4];
            pads[0] = new Point(116, 35);
            pads[1] = new Point(139, 35);
            pads[2] = new Point(116, 62);
            pads[3] = new Point(139, 62);
            // Display controller.
            bool on = gamePadStateIsConnected;
            if (!on) return;
            // Display controller index light.
            int mW = -this.markC.Width / 2;
            int mH = -this.markC.Height / 2;
            e.Graphics.DrawImage(this.markC, pads[ControllerIndex].X + mW, pads[ControllerIndex].Y + mH);

            float padSize = 22F / (float)(ushort.MaxValue);

            mW = -this.markB.Width / 2;
            mH = -this.markB.Height / 2;

            if (!Recording)
            {
                setLabelColor(_leftX > 2000, LeftThumbAxisXLabel);
                if (_leftX < -2000) LeftThumbAxisXLabel.ForeColor = Color.DarkRed;
                setLabelColor(_leftY > 2000, LeftThumbAxisYLabel);
                if (_leftY < -2000) LeftThumbAxisYLabel.ForeColor = Color.DarkRed;
                setLabelColor(_rightX > 2000, RightThumbAxisXLabel);
                if (_rightX < -2000) RightThumbAxisXLabel.ForeColor = Color.DarkRed;
                setLabelColor(_rightY > 2000, RightThumbAxisYLabel);
                if (_rightY < -2000) RightThumbAxisYLabel.ForeColor = Color.DarkRed;
                // Draw button state green led image.
                DrawState(GamepadButtonFlags.Y, buttonY, ButtonYLabel, e);
                DrawState(GamepadButtonFlags.X, buttonX, ButtonXLabel, e);
                DrawState(GamepadButtonFlags.B, buttonB, ButtonBLabel, e);
                DrawState(GamepadButtonFlags.A, buttonA, ButtonALabel, e);
                //DrawState(GamepadButtonFlags.Guide, buttonGuide, ButtonGuideLabel, e);
                DrawState(GamepadButtonFlags.Start, buttonStart, StartButtonLabel, e);
                DrawState(GamepadButtonFlags.Back, buttonBack, BackButtonLabel, e);
                DrawState(GamepadButtonFlags.DPadUp, dPadUp, DPadUpLabel, e);
                DrawState(GamepadButtonFlags.DPadDown, dPadDown, DPadDownLabel, e);
                DrawState(GamepadButtonFlags.DPadLeft, dPadLeft, DPadLeftLabel, e);
                DrawState(GamepadButtonFlags.DPadRight, dPadRight, DPadRightLabel, e);
                DrawState(GamepadButtonFlags.RightThumb, thumbRight, RightThumbButtonLabel, e);
                DrawState(GamepadButtonFlags.LeftThumb, thumbLeft, LeftThumbButtonLabel, e);
                // Draw axis state green cross image.
                e.Graphics.DrawImage(this.markA, (float)((thumbRight.X + mW) + (_rightX * padSize)), (float)((thumbRight.Y + mH) + (-_rightY * padSize)));
                e.Graphics.DrawImage(this.markA, (float)((thumbLeft.X + mW) + (_leftX * padSize)), (float)((thumbLeft.Y + mH) + (-_leftY * padSize)));
            }
            // If recording is in progress and recording image must be drawn then...
            else if (drawRecordingImage)
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
                if (CurrentCbx == LeftThumbButtonComboBox) drawMarkR(e, thumbLeft);
                if (CurrentCbx == RightThumbButtonComboBox) drawMarkR(e, thumbRight);
                if (CurrentCbx == LeftThumbAxisXComboBox) drawMarkR(e, new Point(thumbLeft.X + 10, thumbLeft.Y));
                if (CurrentCbx == LeftThumbAxisYComboBox) drawMarkR(e, new Point(thumbLeft.X, thumbLeft.Y - 10));
                if (CurrentCbx == RightThumbAxisXComboBox) drawMarkR(e, new Point(thumbRight.X + 10, thumbRight.Y));
                if (CurrentCbx == RightThumbAxisYComboBox) drawMarkR(e, new Point(thumbRight.X, thumbRight.Y - 10));
            }
        }

        void DrawState(GamepadButtonFlags button, Point location, Label label, PaintEventArgs e)
        {
            var mW = -this.markB.Width / 2;
            var mH = -this.markB.Height / 2;
            var on = gamePadState.Gamepad.Buttons.HasFlag(button);
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
            // Mapping
            sm.Add(section + SettingName.MapToPad, diControl.MapToPadComboBox);
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
            sm.Add(section + SettingName.ButtonGuide, ButtonGuideComboBox);
            //sm.Add(section + SettingName.ButtonBig, ButtonBigComboBox);
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
			sm.Add(section + SettingName.LeftThumbDeadZoneX, LeftThumbXUserControl.DeadZoneTrackBar);
			sm.Add(section + SettingName.LeftThumbDeadZoneY, LeftThumbYUserControl.DeadZoneTrackBar);
			sm.Add(section + SettingName.LeftThumbAntiDeadZoneX, LeftThumbXUserControl.AntiDeadZoneNumericUpDown);
			sm.Add(section + SettingName.LeftThumbAntiDeadZoneY, LeftThumbYUserControl.AntiDeadZoneNumericUpDown);
            sm.Add(section + SettingName.LeftThumbLinearX, LeftThumbXUserControl.SensitivityNumericUpDown);
            sm.Add(section + SettingName.LeftThumbLinearY, LeftThumbYUserControl.SensitivityNumericUpDown);
            // Right Thumb
            sm.Add(section + SettingName.RightThumbAxisX, RightThumbAxisXComboBox);
            sm.Add(section + SettingName.RightThumbAxisY, RightThumbAxisYComboBox);
            sm.Add(section + SettingName.RightThumbRight, RightThumbRightComboBox);
            sm.Add(section + SettingName.RightThumbLeft, RightThumbLeftComboBox);
            sm.Add(section + SettingName.RightThumbUp, RightThumbUpComboBox);
            sm.Add(section + SettingName.RightThumbDown, RightThumbDownComboBox);
            sm.Add(section + SettingName.RightThumbButton, RightThumbButtonComboBox);
            sm.Add(section + SettingName.RightThumbDeadZoneX, RightThumbXUserControl.DeadZoneTrackBar);
			sm.Add(section + SettingName.RightThumbDeadZoneY, RightThumbYUserControl.DeadZoneTrackBar);
			sm.Add(section + SettingName.RightThumbAntiDeadZoneX, RightThumbXUserControl.AntiDeadZoneNumericUpDown);
			sm.Add(section + SettingName.RightThumbAntiDeadZoneY, RightThumbYUserControl.AntiDeadZoneNumericUpDown);
            sm.Add(section + SettingName.RightThumbLinearX, RightThumbXUserControl.SensitivityNumericUpDown);
            sm.Add(section + SettingName.RightThumbLinearY, RightThumbYUserControl.SensitivityNumericUpDown);
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

        short _leftX;
        short _leftY;
        short _rightX;
        short _rightY;

        State gamePadState;
        bool gamePadStateIsConnected;
        //XINPUT_GAMEPAD GamePad;
        Guid instanceGuid;

        /// <summary>
        /// This function will be called when DirectInput activity is detected.
        /// </summary>
        /// <param name="device">Device responsible for activity.</param>
        public void UpdateFromDirectInput(Joystick device)
        {
            List<string> actions = diControl.UpdateFrom(device);
            StopRecording(actions);
            var contains = PadTabControl.TabPages.Contains(DirectInputTabPage);
            var enable = device != null;
            if (!enable && contains)
            {
                PadTabControl.TabPages.Remove(DirectInputTabPage);
            }
            if (enable && !contains)
            {
                PadTabControl.TabPages.Add(DirectInputTabPage);
            }
            ForceFeedbackGroupBox.Enabled = enable;
            TriggersGroupBox.Enabled = enable;
            AxisToDPadGroupBox.Enabled = enable;
            DeviceGroupBox.Enabled = enable;
            LeftThumbXUserControl.Enabled = enable;
            LeftThumbYUserControl.Enabled = enable;
            RightThumbXUserControl.Enabled = enable;
            RightThumbYUserControl.Enabled = enable;
            if (enable)
            {
                UpdateControl(DirectInputTabPage, device.Information.InstanceName);
            }
            // If this is different device.
            if (!Helper.IsSameDevice(device, instanceGuid))
            {
                Guid iGuid = Guid.Empty;
                if (enable)
                {
                    try { iGuid = device.Information.InstanceGuid; }
                    catch (Exception) { if (SettingManager.Current.IsDebugMode) throw; }
                }
                instanceGuid = !enable ? Guid.Empty : iGuid;
                ResetDiMenuStrip(device);
            }
        }

        State oldState;

        public void UpdateFromXInput(State state, bool IsConnected)
        {
            // If nothing changed then return.
            if (state.Equals(oldState)) return;
            oldState = state;
            var wasConnected = gamePadStateIsConnected;
            var nowConnected = IsConnected;
            gamePadStateIsConnected = IsConnected;
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
                _leftX = state.Gamepad.LeftThumbX;
                _leftY = state.Gamepad.LeftThumbY;
                _rightX = state.Gamepad.RightThumbX;
                _rightY = state.Gamepad.RightThumbY;
            }
            else
            {
                _leftX = 0;
                _leftY = 0;
                _rightX = 0;
                _rightY = 0;
            }
            UpdateControl(LeftThumbTextBox, string.Format("{0};{1}", _leftX, _leftY));
            UpdateControl(RightThumbTextBox, string.Format("{0};{1}", _rightX, _rightY));

            var axis = diControl.Axis;
            bool success;
            int index;
            SettingType type;
            success = SettingsConverter.TryParseIndexAndType(LeftThumbAxisXComboBox.Text, out index, out type);
            if (success) LeftThumbXUserControl.DrawPoint(axis[index - 1], _leftX, type == SettingType.IAxis);
            success = SettingsConverter.TryParseIndexAndType(LeftThumbAxisYComboBox.Text, out index, out type);
            if (success) LeftThumbYUserControl.DrawPoint(axis[index - 1], _leftY, type == SettingType.IAxis);
            success = SettingsConverter.TryParseIndexAndType(RightThumbAxisXComboBox.Text, out index, out type);
            if (success) RightThumbXUserControl.DrawPoint(axis[index - 1], _rightX, type == SettingType.IAxis);
            success = SettingsConverter.TryParseIndexAndType(RightThumbAxisYComboBox.Text, out index, out type);
            if (success) RightThumbYUserControl.DrawPoint(axis[index - 1], _rightY, type == SettingType.IAxis);

            this.TopPictureBox.Refresh();
            this.FrontPictureBox.Refresh();
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
            CreateItems(mi, "Button {0}", "b{0}", device.Capabilities.ButtonCount);
            // Add Axes.
            mi = new ToolStripMenuItem("Axes");
            DiMenuStrip.Items.Add(mi);
            var axisCount = device.Capabilities.AxeCount * 2;
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
            CreateItems(mi, "DPad {0}", "p{0}", device.Capabilities.PovCount);
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


        void DiMenuStrip_Closed(object sender, ToolStripDropDownClosedEventArgs e)
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
                    StartRecording();
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

        void ForceOverallTrackBar_ValueChanged(object sender, EventArgs e)
        {
            TrackBar control = (TrackBar)sender;
            ForceOverallTextBox.Text = string.Format("{0} % ", control.Value);
        }


        void LeftTriggerDeadZoneTrackBar_ValueChanged(object sender, EventArgs e)
        {
            TrackBar control = (TrackBar)sender;
            LeftTriggerDeadZoneTextBox.Text = string.Format("{0} % ", control.Value);
        }


        void RightTriggerDeadZoneTrackBar_ValueChanged(object sender, EventArgs e)
        {
            TrackBar control = (TrackBar)sender;
            RightTriggerDeadZoneTextBox.Text = string.Format("{0} % ", control.Value);
        }



        void MotorTrackBar_ValueChanged(object sender, EventArgs e)
        {
            //if (gamePadState == null) return;
            UpdateForceFeedBack();
        }

        void MotorPeriodTrackBar_ValueChanged(object sender, EventArgs e)
        {
            //if (gamePadState == null) return;
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
            if (mainForm.ControllerIndex == -1) return;
            // Convert 100% trackbar to MotorSpeed's 0 - 65,535 (100%).
            var leftMotor = (short)(LeftMotorTestTrackBar.Value / 100F * ushort.MaxValue);
            var rightMotor = (short)(RightMotorTestTrackBar.Value / 100F * ushort.MaxValue);
            LeftMotorTestTextBox.Text = string.Format("{0} % ", LeftMotorTestTrackBar.Value);
            RightMotorTestTextBox.Text = string.Format("{0} % ", RightMotorTestTrackBar.Value);
            lock (MainForm.XInputLock)
            {
                var gPad = mainForm.GamePads[ControllerIndex];
                if (XInput.IsLoaded && gPad.IsConnected)
                {
                    var vibration = new Vibration();
                    vibration.LeftMotorSpeed = leftMotor;
                    vibration.RightMotorSpeed = rightMotor;
                    gPad.SetVibration(vibration);
                }
            }
            //UnsafeNativeMethods.Enable(false);
            //UnsafeNativeMethods.Enable(true);
        }

        void AxisToDPadOffsetTrackBar_ValueChanged(object sender, EventArgs e)
        {
            TrackBar control = (TrackBar)sender;
            AxisToDPadOffsetTextBox.Text = string.Format("{0} % ", control.Value);
        }

        void AxisToDPadDeadZoneTrackBar_ValueChanged(object sender, EventArgs e)
        {
            TrackBar control = (TrackBar)sender;
            AxisToDPadDeadZoneTextBox.Text = string.Format("{0} % ", control.Value);
        }

		void ClearPresetButton_Click(object sender, EventArgs e)
        {
            mainForm.LoadPreset("Clear", ControllerIndex);
        }

        void ResetPresetButton_Click(object sender, EventArgs e)
        {
            mainForm.ReloadXinputSettings();
        }

        void SavePresetButton_Click(object sender, EventArgs e)
        {
            mainForm.UpdateTimer.Stop();
            // Save settigns to INI file.
            SettingManager.Current.SaveSettings();
            // Owerwrite Temp file.
            var ini = new System.IO.FileInfo(SettingManager.IniFileName);
            ini.CopyTo(SettingManager.TmpFileName, true);
            mainForm.StatusTimerLabel.Text = "Settings saved";
            mainForm.UpdateTimer.Start();
        }

        void PadTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            mainForm.UpdateHelpHeader();
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                markA.Dispose();
                markB.Dispose();
                markC.Dispose();
                markR.Dispose();
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void LeftMotorStrengthTrackBar_ValueChanged(object sender, EventArgs e)
        {
            var control = (TrackBar)sender;
            LeftMotorStrengthTrackBar.Text = string.Format("{0} % ", control.Value);
        }

        private void RightMotorStrengthTrackBar_ValueChanged(object sender, EventArgs e)
        {
            var control = (TrackBar)sender;
            RightMotorStrengthTrackBar.Text = string.Format("{0} % ", control.Value);
        }

    }
}
