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
using System.Linq;
using x360ce.Engine;
using System.Diagnostics;
using JocysCom.ClassLibrary.IO;

namespace x360ce.App.Controls
{
    public partial class PadControl : UserControl
    {
        public PadControl(int controllerIndex)
        {
            InitializeComponent();
            TestTimer = new System.Timers.Timer();
            TestTimer.AutoReset = false;
            TestTimer.Elapsed += TestTimer_Elapsed;
            ControllerIndex = controllerIndex;
            object[] rates = {
                1000/8, //  125
				1000/7, //  142
				1000/6, //  166
				1000/5, //  200
				1000/4, //  250
				1000/3, //  333
				1000/2, //  500
				1000/1, // 1000
			};
            PollingRateComboBox.Items.AddRange(rates);
            PollingRateComboBox.SelectedIndex = 0;
            // Add direct input user control.
            this.SuspendLayout();
            diControl = new DirectInputControl();
            diControl.Dock = DockStyle.Fill;
            DirectInputTabPage.Controls.Add(diControl);
            PadTabControl.TabPages.Remove(DirectInputTabPage);
            this.ResumeLayout();
            // Axis to Button DeadZones
            AxisToButtonADeadZonePanel.MonitorComboBox = ButtonAComboBox;
            AxisToButtonBDeadZonePanel.MonitorComboBox = ButtonBComboBox;
            AxisToButtonXDeadZonePanel.MonitorComboBox = ButtonXComboBox;
            AxisToButtonYDeadZonePanel.MonitorComboBox = ButtonYComboBox;
            AxisToButtonStartDeadZonePanel.MonitorComboBox = ButtonStartComboBox;
            AxisToButtonBackDeadZonePanel.MonitorComboBox = ButtonBackComboBox;
            AxisToLeftShoulderDeadZonePanel.MonitorComboBox = LeftShoulderComboBox;
            AxisToLeftThumbButtonDeadZonePanel.MonitorComboBox = LeftThumbButtonComboBox;
            AxisToRightShoulderDeadZonePanel.MonitorComboBox = RightShoulderComboBox;
            AxisToRightThumbButtonDeadZonePanel.MonitorComboBox = RightThumbButtonComboBox;
            AxisToDPadDownDeadZonePanel.MonitorComboBox = DPadDownComboBox;
            AxisToDPadLeftDeadZonePanel.MonitorComboBox = DPadLeftComboBox;
            AxisToDPadRightDeadZonePanel.MonitorComboBox = DPadRightComboBox;
            AxisToDPadUpDeadZonePanel.MonitorComboBox = DPadUpComboBox;
        }

        public void InitPadControl()
        {
            // Initialize images.
            this.TopPictureBox.Image = topDisabledImage;
            this.FrontPictureBox.Image = frontDisabledImage;
            this.markB = new Bitmap(EngineHelper.GetResource("Images.MarkButton.png"));
            this.markA = new Bitmap(EngineHelper.GetResource("Images.MarkAxis.png"));
            this.markC = new Bitmap(EngineHelper.GetResource("Images.MarkController.png"));
            this.markR = new Bitmap(EngineHelper.GetResource("Images.bullet_ball_glass_red_16x16.png"));
            float rH = topDisabledImage.HorizontalResolution;
            float rV = topDisabledImage.VerticalResolution;
            // Make sure resolution is same everywhere so images won't be resized.
            this.markB.SetResolution(rH, rV);
            this.markA.SetResolution(rH, rV);
            this.markC.SetResolution(rH, rV);
            this.markR.SetResolution(rH, rV);
            // Add GamePad typed to ComboBox.
            var types = (SharpDX.XInput.DeviceSubType[])Enum.GetValues(typeof(SharpDX.XInput.DeviceSubType));
            foreach (var item in types) DeviceSubTypeComboBox.Items.Add(item);
            // Add force feedback typed to ComboBox.
            var effectsTypes = (ForceEffectType[])Enum.GetValues(typeof(ForceEffectType));
            foreach (var item in effectsTypes) ForceTypeComboBox.Items.Add(item);

            var effectDirections = (ForceEffectDirection[])Enum.GetValues(typeof(ForceEffectDirection));
            foreach (var item in effectDirections) LeftMotorDirectionComboBox.Items.Add(item);
            foreach (var item in effectDirections) RightMotorDirectionComboBox.Items.Add(item);

            // Add player index to combo boxes
            var playerOptions = new List<KeyValuePair>();
            var playerTypes = (UserIndex[])Enum.GetValues(typeof(UserIndex));
            foreach (var item in playerTypes) playerOptions.Add(new KeyValuePair(item.ToString(), ((int)item).ToString()));
            PassThroughIndexComboBox.DataSource = new BindingSource(playerOptions, null); // Otherwise changing one changes the other
            PassThroughIndexComboBox.DisplayMember = "Key";
            PassThroughIndexComboBox.ValueMember = "Value";
            CombinedIndexComboBox.DataSource = new BindingSource(playerOptions, null);  // Otherwise changing one changes the other
            CombinedIndexComboBox.DisplayMember = "Key";
            CombinedIndexComboBox.ValueMember = "Value";
            var comboBoxes = new List<ComboBox>();
            GetAllControls(GeneralTabPage, ref comboBoxes);
            // Attach context strip with button names to every ComboBox on general tab.
            foreach (var cb in comboBoxes)
            {
                ((ComboBox)cb).ContextMenuStrip = DiMenuStrip;
            }
        }

        public void GetAllControls<T>(Control c, ref List<T> l) where T : Control
        {
            T[] boxes = c.Controls.OfType<T>().ToArray();
            Control[] bases = c.Controls.Cast<Control>().ToArray();
            l.AddRange(boxes);
            Control[] c2 = c.Controls.Cast<Control>().Except(boxes).ToArray();
            for (int i = 0; i <= c2.Length - 1; i++)
            {
                GetAllControls(c2[i], ref l);
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
                recordingSnapshot = null;
                drawRecordingImage = true;
                RecordingTimer.Start();
                CurrentCbx.ForeColor = SystemColors.GrayText;
                MainForm.Current.StatusTimerLabel.Text = (CurrentCbx == DPadComboBox)
                     ? "Recording - press any D-Pad button on your direct input device. Press ESC to cancel..."
                     : "Recording - press button, move axis or slider on your direct input device. Press ESC to cancel...";
            }
        }

        /// <summary>Initial Direct Input activity state</summary>
        DirectInputState recordingSnapshot;

        /// <summary>
        /// Called when recording is in progress.
        /// </summary>
        /// <param name="state">Current direct input activity.</param>
        /// <returns>True if recording stopped, otherwise false.</returns>
        public bool StopRecording(DirectInputState state = null)
        {
            lock (recordingLock)
            {
                // If recording is not in progress then return false.
                if (!Recording)
                {
                    recordingSnapshot = null;
                    return false;
                }
                // If recording snapshot was not created yet then...
                else if (recordingSnapshot == null)
                {
                    // Make snapshot out of the first state during recording.
                    recordingSnapshot = state;
                    return false;
                }
                // Get actions by comparing initial snapshot with current state.
                var actions = recordingSnapshot.CompareTo(state);
                string action = null;
                // Must stop recording if null passed.
                var stop = actions == null;
                // if at least one action was recorded then...
                if (!stop && actions.Length > 0)
                {
                    // If this is DPad ComboBox then...
                    if (CurrentCbx == DPadComboBox)
                    {
                        // Get first action suitable for DPad
                        var dPadAction = actions.FirstOrDefault(x => dPadRx.IsMatch(x));
                        if (dPadAction != null)
                        {
                            action = dPadRx.Match(dPadAction).Groups[0].Value;
                            stop = true;
                        }
                    }
                    else
                    {
                        // Get first recorded action.
                        action = actions[0];
                        stop = true;
                    }
                }
                // If recording must stop then...
                if (stop)
                {
                    Recording = false;
                    RecordingTimer.Stop();
                    // If stop was initiated before action was recorded then...                    
                    if (string.IsNullOrEmpty(action))
                    {
                        CurrentCbx.Items.Clear();
                    }
                    else
                    {
                        // If suitable action was recorded then...
                        SettingManager.Current.SetComboBoxValue(CurrentCbx, action);
                        // Save setting and notify if value changed.
                        if (SettingManager.Current.SaveSetting(CurrentCbx)) MainForm.Current.NotifySettingsChange();
                    }
                    CurrentCbx.ForeColor = SystemColors.WindowText;
                    CurrentCbx = null;
                }
                return stop;
            }
        }

        #endregion

        #region Control ComboBox'es

        ComboBox CurrentCbx;
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
            // Move default DropDown away from the screen.
            cbx.Left = -10000;
            var del = new ComboBoxDropDownDelegate(ComboBoxDropDown);
            BeginInvoke(del, new object[] { cbx, oldLeft });
        }

        delegate void ComboBoxDropDownDelegate(ComboBox cbx, int oldLeft);

        void ComboBoxDropDown(ComboBox cbx, int oldLeft)
        {
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
                var menuStrip = cbx.ContextMenuStrip;
                if (menuStrip != null)
                {
                    menuStrip.Show(cbx, new Point(0, cbx.Height), ToolStripDropDownDirection.Default);
                }
                CurrentCbx = cbx;
            }
            if (cbx.Items.Count > 0)
            {
                cbx.SelectedIndex = 0;
            }
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
            get { return _topImage = _topImage ?? new Bitmap(EngineHelper.GetResource("Images.xboxControllerTop.png")); }
        }

        Bitmap _frontImage;
        Bitmap frontImage
        {
            get { return _frontImage = _frontImage ?? new Bitmap(EngineHelper.GetResource("Images.xboxControllerFront.png")); }
        }

        Bitmap _topDisabledImage;
        Bitmap topDisabledImage
        {
            get
            {
                if (_topDisabledImage == null)
                {
                    _topDisabledImage = AppHelper.GetDisabledImage(topImage);
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
                    _frontDisabledImage = AppHelper.GetDisabledImage(frontImage);
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
                DrawState(GamepadButtonFlags.A, buttonA, ButtonALabel, e);
                DrawState(GamepadButtonFlags.B, buttonB, ButtonBLabel, e);
                DrawState(GamepadButtonFlags.X, buttonX, ButtonXLabel, e);
                DrawState(GamepadButtonFlags.Y, buttonY, ButtonYLabel, e);
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

        object settingsMapLock = new object();

        Dictionary<string, Control> _SettingsMap;
        public Dictionary<string, Control> SettingsMap
        {
            get
            {
                lock (settingsMapLock)
                {
                    if (_SettingsMap == null)
                    {
                        _SettingsMap = GetSettingsMap();
                    }
                    return _SettingsMap;
                }
            }
        }

        public int ControllerIndex;

        /// <summary>
        /// Link control with INI key. Value/Text of control will be automatically tracked and INI file updated.
        /// </summary>
        Dictionary<string, Control> GetSettingsMap()
        {
            Dictionary<string, Control> sm = new Dictionary<string, Control>();
            string section = string.Format(@"PAD{0}\", ControllerIndex + 1);
            // FakeAPI
            SettingManager.AddMap(section, () => SettingName.ProductName, diControl.DeviceProductNameTextBox, sm);
            SettingManager.AddMap(section, () => SettingName.ProductGuid, diControl.DeviceProductGuidTextBox, sm);
            SettingManager.AddMap(section, () => SettingName.InstanceGuid, diControl.DeviceInstanceGuidTextBox, sm);
            SettingManager.AddMap(section, () => SettingName.DeviceSubType, DeviceSubTypeComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.PassThrough, PassThroughCheckBox, sm);
            SettingManager.AddMap(section, () => SettingName.ForcesPassThrough, ForceFeedbackPassThroughCheckBox, sm);
            SettingManager.AddMap(section, () => SettingName.PassThroughIndex, PassThroughIndexComboBox, sm);
            // Mapping
            SettingManager.AddMap(section, () => SettingName.MapToPad, diControl.MapToPadComboBox, sm);
            // Triggers
            SettingManager.AddMap(section, () => SettingName.RightTrigger, RightTriggerComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.RightTriggerDeadZone, RightTriggerDeadZoneTrackBar, sm);
            SettingManager.AddMap(section, () => SettingName.LeftTrigger, LeftTriggerComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.LeftTriggerDeadZone, LeftTriggerDeadZoneTrackBar, sm);
            // Combining
            SettingManager.AddMap(section, () => SettingName.Combined, CombinedCheckBox, sm);
            SettingManager.AddMap(section, () => SettingName.CombinedIndex, CombinedIndexComboBox, sm);
            // D-Pad
            SettingManager.AddMap(section, () => SettingName.DPad, DPadComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.DPadUp, DPadUpComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.DPadDown, DPadDownComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.DPadLeft, DPadLeftComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.DPadRight, DPadRightComboBox, sm);
            // Axis To Button
            SettingManager.AddMap(section, () => SettingName.AxisToButtonADeadZone, AxisToButtonADeadZonePanel.DeadZoneNumericUpDown, sm);
            SettingManager.AddMap(section, () => SettingName.AxisToButtonBDeadZone, AxisToButtonBDeadZonePanel.DeadZoneNumericUpDown, sm);
            SettingManager.AddMap(section, () => SettingName.AxisToButtonXDeadZone, AxisToButtonXDeadZonePanel.DeadZoneNumericUpDown, sm);
            SettingManager.AddMap(section, () => SettingName.AxisToButtonYDeadZone, AxisToButtonYDeadZonePanel.DeadZoneNumericUpDown, sm);
            SettingManager.AddMap(section, () => SettingName.AxisToButtonStartDeadZone, AxisToButtonStartDeadZonePanel.DeadZoneNumericUpDown, sm);
            SettingManager.AddMap(section, () => SettingName.AxisToButtonBackDeadZone, AxisToButtonBackDeadZonePanel.DeadZoneNumericUpDown, sm);
            SettingManager.AddMap(section, () => SettingName.AxisToLeftShoulderDeadZone, AxisToLeftShoulderDeadZonePanel.DeadZoneNumericUpDown, sm);
            SettingManager.AddMap(section, () => SettingName.AxisToLeftThumbButtonDeadZone, AxisToLeftThumbButtonDeadZonePanel.DeadZoneNumericUpDown, sm);
            SettingManager.AddMap(section, () => SettingName.AxisToRightShoulderDeadZone, AxisToRightShoulderDeadZonePanel.DeadZoneNumericUpDown, sm);
            SettingManager.AddMap(section, () => SettingName.AxisToRightThumbButtonDeadZone, AxisToRightThumbButtonDeadZonePanel.DeadZoneNumericUpDown, sm);
            // Axis To D-Pad (separate directions).
            SettingManager.AddMap(section, () => SettingName.AxisToDPadDownDeadZone, AxisToDPadDownDeadZonePanel.DeadZoneNumericUpDown, sm);
            SettingManager.AddMap(section, () => SettingName.AxisToDPadLeftDeadZone, AxisToDPadLeftDeadZonePanel.DeadZoneNumericUpDown, sm);
            SettingManager.AddMap(section, () => SettingName.AxisToDPadRightDeadZone, AxisToDPadRightDeadZonePanel.DeadZoneNumericUpDown, sm);
            SettingManager.AddMap(section, () => SettingName.AxisToDPadUpDeadZone, AxisToDPadUpDeadZonePanel.DeadZoneNumericUpDown, sm);
            // Axis To D-Pad.
            SettingManager.AddMap(section, () => SettingName.AxisToDPadEnabled, AxisToDPadEnabledCheckBox, sm);
            SettingManager.AddMap(section, () => SettingName.AxisToDPadDeadZone, AxisToDPadDeadZoneTrackBar, sm);
            SettingManager.AddMap(section, () => SettingName.AxisToDPadOffset, AxisToDPadOffsetTrackBar, sm);
            // Buttons
            SettingManager.AddMap(section, () => SettingName.ButtonGuide, ButtonGuideComboBox, sm);
            //sm.Add(section + SettingName.ButtonBig, ButtonBigComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.ButtonBack, ButtonBackComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.ButtonStart, ButtonStartComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.ButtonA, ButtonAComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.ButtonB, ButtonBComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.ButtonX, ButtonXComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.ButtonY, ButtonYComboBox, sm);
            // Shoulders.
            SettingManager.AddMap(section, () => SettingName.LeftShoulder, LeftShoulderComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.RightShoulder, RightShoulderComboBox, sm);
            // Left Thumb
            SettingManager.AddMap(section, () => SettingName.LeftThumbAxisX, LeftThumbAxisXComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.LeftThumbAxisY, LeftThumbAxisYComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.LeftThumbRight, LeftThumbRightComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.LeftThumbLeft, LeftThumbLeftComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.LeftThumbUp, LeftThumbUpComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.LeftThumbDown, LeftThumbDownComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.LeftThumbButton, LeftThumbButtonComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.LeftThumbDeadZoneX, LeftThumbXUserControl.DeadZoneTrackBar, sm);
            SettingManager.AddMap(section, () => SettingName.LeftThumbDeadZoneY, LeftThumbYUserControl.DeadZoneTrackBar, sm);
            SettingManager.AddMap(section, () => SettingName.LeftThumbAntiDeadZoneX, LeftThumbXUserControl.AntiDeadZoneNumericUpDown, sm);
            SettingManager.AddMap(section, () => SettingName.LeftThumbAntiDeadZoneY, LeftThumbYUserControl.AntiDeadZoneNumericUpDown, sm);
            SettingManager.AddMap(section, () => SettingName.LeftThumbLinearX, LeftThumbXUserControl.SensitivityNumericUpDown, sm);
            SettingManager.AddMap(section, () => SettingName.LeftThumbLinearY, LeftThumbYUserControl.SensitivityNumericUpDown, sm);
            // Right Thumb
            SettingManager.AddMap(section, () => SettingName.RightThumbAxisX, RightThumbAxisXComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.RightThumbAxisY, RightThumbAxisYComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.RightThumbRight, RightThumbRightComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.RightThumbLeft, RightThumbLeftComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.RightThumbUp, RightThumbUpComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.RightThumbDown, RightThumbDownComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.RightThumbButton, RightThumbButtonComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.RightThumbDeadZoneX, RightThumbXUserControl.DeadZoneTrackBar, sm);
            SettingManager.AddMap(section, () => SettingName.RightThumbDeadZoneY, RightThumbYUserControl.DeadZoneTrackBar, sm);
            SettingManager.AddMap(section, () => SettingName.RightThumbAntiDeadZoneX, RightThumbXUserControl.AntiDeadZoneNumericUpDown, sm);
            SettingManager.AddMap(section, () => SettingName.RightThumbAntiDeadZoneY, RightThumbYUserControl.AntiDeadZoneNumericUpDown, sm);
            SettingManager.AddMap(section, () => SettingName.RightThumbLinearX, RightThumbXUserControl.SensitivityNumericUpDown, sm);
            SettingManager.AddMap(section, () => SettingName.RightThumbLinearY, RightThumbYUserControl.SensitivityNumericUpDown, sm);
            // Force Feedback
            SettingManager.AddMap(section, () => SettingName.ForceEnable, ForceEnableCheckBox, sm);
            SettingManager.AddMap(section, () => SettingName.ForceType, ForceTypeComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.ForceSwapMotor, ForceSwapMotorCheckBox, sm);
            SettingManager.AddMap(section, () => SettingName.ForceOverall, ForceOverallTrackBar, sm);
            SettingManager.AddMap(section, () => SettingName.LeftMotorDirection, LeftMotorDirectionComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.LeftMotorStrength, LeftMotorStrengthTrackBar, sm);
            SettingManager.AddMap(section, () => SettingName.LeftMotorPeriod, LeftMotorPeriodTrackBar, sm);
            SettingManager.AddMap(section, () => SettingName.RightMotorDirection, RightMotorDirectionComboBox, sm);
            SettingManager.AddMap(section, () => SettingName.RightMotorStrength, RightMotorStrengthTrackBar, sm);
            SettingManager.AddMap(section, () => SettingName.RightMotorPeriod, RightMotorPeriodTrackBar, sm);
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

        private void UpdatePassThroughRelatedControls()
        {
            // Is Pass Through enabled?
            bool fullPassThrough = PassThroughCheckBox.Checked;
            bool forcesPassThrough = ForceFeedbackPassThroughCheckBox.Checked;

            // If full pass-through mode is turned on, changing forces pass-through has no effect.
            ForceFeedbackPassThroughCheckBox.Enabled = !fullPassThrough;

            // Pass Through index is enabled if either pass through mode is enabled
            PassThroughIndexComboBox.Enabled = (fullPassThrough || forcesPassThrough);
        }

        Joystick _device;

        /// <summary>
        /// This function will be called from UpdateTimer on main form.
        /// </summary>
        /// <param name="device">Device responsible for activity.</param>
        public void UpdateFromDirectInput(Joystick device, DeviceInfo dInfo)
        {
            // Update direct input form and return actions (pressed buttons/dpads, turned axis/sliders).
            JoystickState state;
            //List<string> actions = 
            diControl.UpdateFrom(device, dInfo, out state);
            DirectInputState diState = null;
            if (state != null) diState = new DirectInputState(state);
            StopRecording(diState);
            var contains = PadTabControl.TabPages.Contains(DirectInputTabPage);
            var focusTab = false;
            _device = device;
            var enable = device != null;
            AutoPresetButton.Enabled = enable;
            if (!enable && contains)
            {
                PadTabControl.TabPages.Remove(DirectInputTabPage);
            }
            if (enable && !contains)
            {
                PadTabControl.TabPages.Add(DirectInputTabPage);
                var controllerInstance = MainForm.Current.AutoSelectControllerInstance;
                if (controllerInstance != Guid.Empty && controllerInstance == device.Information.InstanceGuid)
                {
                    MainForm.Current.AutoSelectControllerInstance = Guid.Empty;
                    focusTab = true;
                }
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
            if (!AppHelper.IsSameDevice(device, instanceGuid))
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
            if (focusTab)
            {
                PadTabControl.SelectedTab = DirectInputTabPage;
                Application.DoEvents();
                var padTabPage = (TabPage)this.Parent;
                ((TabControl)padTabPage.Parent).SelectedTab = padTabPage;
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
            // If form was disabled and no data is coming then just return.
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
            if (success)
                LeftThumbXUserControl.DrawPoint(axis[index - 1], _leftX, type == SettingType.IAxis);
            success = SettingsConverter.TryParseIndexAndType(LeftThumbAxisYComboBox.Text, out index, out type);
            if (success)
                LeftThumbYUserControl.DrawPoint(axis[index - 1], _leftY, type == SettingType.IAxis);
            success = SettingsConverter.TryParseIndexAndType(RightThumbAxisXComboBox.Text, out index, out type);
            if (success)
                RightThumbXUserControl.DrawPoint(axis[index - 1], _rightX, type == SettingType.IAxis);
            success = SettingsConverter.TryParseIndexAndType(RightThumbAxisYComboBox.Text, out index, out type);
            if (success)
                RightThumbYUserControl.DrawPoint(axis[index - 1], _rightY, type == SettingType.IAxis);
            // Update controller images.
            this.TopPictureBox.Refresh();
            this.FrontPictureBox.Refresh();
            // Update Axis to Button Images.
            var AxisToButtonControls = AxisToButtonGroupBox.Controls.OfType<AxisToButtonUserControl>();
            foreach (var atbPanel in AxisToButtonControls)
            {
                atbPanel.Refresh(gamePadState, markB);
            }
        }

        // Check left thumbStick
        public float FloatToByte(float v)
        {
            // -1 to 1 int16.MinValue int16.MaxValue.
            return (Byte)Math.Round((double)v * (double)Byte.MaxValue);
        }

        // Use this to reduce flickering.
        public void UpdateControl(Control control, string text)
        {
            if (control.Text != text) control.Text = text;
        }

        string cRecord = "[Record]";
        string cEmpty = "<empty>";


        // Function is recreated as soon as new DirectInput Device is available.
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
            mi.Image = new Bitmap(EngineHelper.GetResource("Images.bullet_ball_glass_red_16x16.png"));
            mi.Click += new EventHandler(DiMenuStrip_Click);
            DiMenuStrip.Items.Add(mi);
            // Add Buttons.
            mi = new ToolStripMenuItem("Buttons");
            DiMenuStrip.Items.Add(mi);
            CreateItems(mi, "Button {0}", "b{0}", device.Capabilities.ButtonCount);
            // Add Axes.
            mi = new ToolStripMenuItem("Axes");
            DiMenuStrip.Items.Add(mi);
            var axisCount = diControl.Axis.Length;
            CreateItems(mi, "Inverted", "IAxis {0}", "a-{0}", axisCount);
            CreateItems(mi, "Inverted Half", "IHAxis {0}", "x-{0}", axisCount);
            CreateItems(mi, "Half", "HAxis {0}", "x{0}", axisCount);
            CreateItems(mi, "Axis {0}", "a{0}", axisCount);
            // Add Sliders.            
            mi = new ToolStripMenuItem("Sliders");
            DiMenuStrip.Items.Add(mi);
            var slidersCount = 8;
            CreateItems(mi, "Inverted", "ISlider {0}", "s-{0}", slidersCount);
            CreateItems(mi, "Inverted Half", "IHSlider {0}", "h-{0}", slidersCount);
            CreateItems(mi, "Half", "HSlider {0}", "h{0}", slidersCount);
            CreateItems(mi, "Slider {0}", "s{0}", slidersCount);
            // Add D-Pads.
            mi = new ToolStripMenuItem("DPads");
            DiMenuStrip.Items.Add(mi);
            // Add D-Pad Top, Right, Bottom, Left button.
            var dPadNames = Enum.GetNames(typeof(DPadEnum));
            for (int p = 0; p < device.Capabilities.PovCount; p++)
            {
                var dPadItem = CreateItem("DPad {0}", "{1}{0}", p + 1, SettingName.SType.DPad);
                mi.DropDownItems.Add(dPadItem);
                for (int d = 0; d < dPadNames.Length; d++)
                {
                    var dPadButtonIndex = p * 4 + d + 1;
                    var dPadButtonItem = CreateItem("DPad {0} {1}", "{2}{3}", p + 1, dPadNames[d], SettingName.SType.DPadButton, dPadButtonIndex);
                    dPadItem.DropDownItems.Add(dPadButtonItem);
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
            // If this DPad parent menu.
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
            if (MainForm.Current.ControllerIndex == -1) return;
            // Convert 100% TrackBar to MotorSpeed's 0 - 65,535 (100%).
            var leftMotor = (short)(LeftMotorStrengthTrackBar.Value / 100F * ushort.MaxValue);
            var rightMotor = (short)(RightMotorStrengthTrackBar.Value / 100F * ushort.MaxValue);
            lock (MainForm.XInputLock)
            {
                var gPad = MainForm.Current.GamePads[ControllerIndex];
                if (XInput.IsLoaded && gPad.IsConnected)
                {
                    var vibration = new Vibration();
                    if (_TestEnabled)
                    {
                        vibration.LeftMotorSpeed = leftMotor;
                        vibration.RightMotorSpeed = rightMotor;
                        UpdateControl(TestButton, "Stop Test");
                    }
                    else
                    {
                        UpdateControl(TestButton, "Start Test");
                    }
                    gPad.SetVibration(vibration);
                }
            }
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
            var text = string.Format("Do you really want to clear all Controller {0} settings?", ControllerIndex + 1);
            var form = new MessageBoxForm();
            form.StartPosition = FormStartPosition.CenterParent;
            var result = form.ShowForm(text, "Clear Controller Settings", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                MainForm.Current.LoadPreset("Clear", ControllerIndex);
            }
        }

        void ResetPresetButton_Click(object sender, EventArgs e)
        {
            var text = string.Format("Do you really want to reset all Controller {0} settings?", ControllerIndex + 1);
            var form = new MessageBoxForm();
            form.StartPosition = FormStartPosition.CenterParent;
            var result = form.ShowForm(text, "Reset Controller Settings", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                MainForm.Current.ReloadXinputSettings();
            }
        }

        private void AutoPresetButton_Click(object sender, EventArgs e)
        {
            var d = _device;
            if (d == null) return;
            var text = string.Format("Do you want to fill all Controller {0} settings automatically?", ControllerIndex + 1);
            var form = new MessageBoxForm();
            form.StartPosition = FormStartPosition.CenterParent;
            var result = form.ShowForm(text, "Auto Controller Settings", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                MainForm.Current.LoadPreset("Clear", ControllerIndex);
                var objects = AppHelper.GetDeviceObjects(d);
                DeviceObjectItem o = null;
                o = objects.FirstOrDefault(x => x.GuidValue == ObjectGuid.RxAxis);
                // If Right thumb triggers are missing then...
                if (o == null)
                {
                    // Logitech RumblePad 2 USB
                    AutoPreset(objects, SettingName.ButtonA, 1);
                    AutoPreset(objects, SettingName.ButtonB, 2);
                    AutoPreset(objects, SettingName.ButtonX, 0);
                    AutoPreset(objects, SettingName.ButtonY, 3);
                    AutoPreset(objects, SettingName.LeftShoulder, 4);
                    AutoPreset(objects, SettingName.RightShoulder, 5);
                    AutoPreset(objects, SettingName.ButtonBack, 8);
                    AutoPreset(objects, SettingName.ButtonStart, 9);
                    AutoPreset(objects, SettingName.LeftThumbButton, 10);
                    AutoPreset(objects, SettingName.RightThumbButton, 11);
                    // Triggers.
                    AutoPreset(objects, SettingName.LeftTrigger, 6);
                    AutoPreset(objects, SettingName.RightTrigger, 7);
                    // Right Thumb.
                    o = objects.FirstOrDefault(x => x.GuidValue == ObjectGuid.ZAxis);
                    if (o != null) AutoPresetRead(SettingName.RightThumbAxisX, string.Format("{0}{1}", SettingName.SType.Axis, o.Instance + 1));
                    o = objects.FirstOrDefault(x => x.GuidValue == ObjectGuid.RzAxis);
                    if (o != null) AutoPresetRead(SettingName.RightThumbAxisY, string.Format("{0}-{1}", SettingName.SType.Axis, o.Instance + 1));
                }
                else
                {
                    // ----------------------------------------------------------------------------------------------
                    // Controller (Xbox One For Windows)
                    // ----------------------------------------------------------------------------------------------
                    // Offset   Usage  Instance  Guid           Name                            Flags                
                    // ------  ------  --------  -------------  ------------------------------  ---------------------
                    //      0      49         1  YAxis          Y Axis                          AbsoluteAxis         
                    //      0       5         0  Unknown        Collection 0 - Game Pad         Collection, NoData   
                    //      0       0         1  Unknown        Collection 1                    Collection, NoData   
                    //      0       0         2  Unknown        Collection 2                    Collection, NoData   
                    //      0       0         3  Unknown        Collection 3                    Collection, NoData   
                    //      0     128         4  Unknown        Collection 4 - System Controls  Collection, NoData   
                    //      4      48         0  XAxis          X Axis                          AbsoluteAxis         
                    //      8      52         4  RyAxis         Y Rotation                      AbsoluteAxis         
                    //     12      51         3  RxAxis         X Rotation                      AbsoluteAxis         
                    //     16      50         2  ZAxis          Z Axis                          AbsoluteAxis         
                    //     20      53         5  RzAxis         Z Rotation                      AbsoluteAxis         
                    //     24      57         0  PovController  Hat Switch                      PointOfViewController
                    //     32     151        19  Unknown        DC Enable Actuators             NoData, Output       
                    //     36       1        20  Unknown        Physical Interface Device       NoData, Output       
                    //     40     112        21  Unknown        Magnitude                       NoData, Output       
                    //     44      80        22  Unknown        Duration                        NoData, Output       
                    //     48     167        23  Unknown        Start Delay                     NoData, Output       
                    //     52     124        24  Unknown        Loop Count                      NoData, Output       
                    //     56       1         0  Button         Button 0                        PushButton           
                    //     57       2         1  Button         Button 1                        PushButton           
                    //     58       3         2  Button         Button 2                        PushButton           
                    //     59       4         3  Button         Button 3                        PushButton           
                    //     60       5         4  Button         Button 4                        PushButton           
                    //     61       6         5  Button         Button 5                        PushButton           
                    //     62       7         6  Button         Button 6                        PushButton           
                    //     63       8         7  Button         Button 7                        PushButton           
                    //     64       9         8  Button         Button 8                        PushButton           
                    //     65      10         9  Button         Button 9                        PushButton           
                    //     66     133        10  Button         System Main Menu                PushButton           
                    AutoPreset(objects, SettingName.ButtonA, 0);
                    AutoPreset(objects, SettingName.ButtonB, 1);
                    AutoPreset(objects, SettingName.ButtonX, 2);
                    AutoPreset(objects, SettingName.ButtonY, 3);
                    AutoPreset(objects, SettingName.LeftShoulder, 4);
                    AutoPreset(objects, SettingName.RightShoulder, 5);
                    AutoPreset(objects, SettingName.ButtonBack, 6);
                    AutoPreset(objects, SettingName.ButtonStart, 7);
                    AutoPreset(objects, SettingName.LeftThumbButton, 8);
                    AutoPreset(objects, SettingName.RightThumbButton, 9);
                    // Triggers.
                    o = objects.FirstOrDefault(x => x.GuidValue == ObjectGuid.ZAxis);
                    if (o != null) AutoPresetRead(SettingName.LeftTrigger, string.Format("{0}{1}", SettingName.SType.Axis, o.Instance + 1));
                    o = objects.FirstOrDefault(x => x.GuidValue == ObjectGuid.RzAxis);
                    if (o != null) AutoPresetRead(SettingName.RightTrigger, string.Format("{0}{1}", SettingName.SType.Axis, o.Instance + 1));
                    // Right Thumb.
                    o = objects.FirstOrDefault(x => x.GuidValue == ObjectGuid.RxAxis);
                    if (o != null) AutoPresetRead(SettingName.RightThumbAxisX, string.Format("{0}{1}", SettingName.SType.Axis, o.Instance + 1));
                    o = objects.FirstOrDefault(x => x.GuidValue == ObjectGuid.RyAxis);
                    if (o != null) AutoPresetRead(SettingName.RightThumbAxisY, string.Format("{0}-{1}", SettingName.SType.Axis, o.Instance + 1));
                }
                // Left Thumb.
                o = objects.FirstOrDefault(x => x.GuidValue == ObjectGuid.XAxis);
                if (o != null) AutoPresetRead(SettingName.LeftThumbAxisX, string.Format("{0}{1}", SettingName.SType.Axis, o.Instance + 1));
                o = objects.FirstOrDefault(x => x.GuidValue == ObjectGuid.YAxis);
                if (o != null) AutoPresetRead(SettingName.LeftThumbAxisY, string.Format("{0}-{1}", SettingName.SType.Axis, o.Instance + 1));
                // D-Pad
                o = objects.FirstOrDefault(x => x.GuidValue == ObjectGuid.PovController);
                if (o != null) AutoPresetRead(SettingName.DPad, string.Format("{0}{1}", SettingName.SType.DPad, o.Instance + 1));
            }
        }

        void AutoPreset(DeviceObjectItem[] objects, string settingName, int index)
        {
            var o = objects.FirstOrDefault(x => x.GuidValue == ObjectGuid.Button && x.Instance == index);
            if (o != null) AutoPresetRead(settingName, string.Format("{0}{1}", SettingName.SType.Button, o.Instance + 1));
        }

        void AutoPresetRead(string key, string value)
        {
            var pad = string.Format("PAD{0}", ControllerIndex + 1);
            var path = string.Format("{0}\\{1}", pad, key);
            var control = SettingsMap[path];
            //control.Text = value;
            SettingManager.Current.ReadSettingTo(control, key, value);
        }

        void SavePresetButton_Click(object sender, EventArgs e)
        {
            MainForm.Current.SaveSettings();
        }

        void PadTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            MainForm.Current.UpdateHelpHeader();
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
            LeftMotorStrengthTextBox.Text = string.Format("{0} % ", control.Value);
            UpdateForceFeedBack();
        }

        private void RightMotorStrengthTrackBar_ValueChanged(object sender, EventArgs e)
        {
            var control = (TrackBar)sender;
            RightMotorStrengthTextBox.Text = string.Format("{0} % ", control.Value);
            UpdateForceFeedBack();
        }

        private void PassThroughCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePassThroughRelatedControls();
        }

        private void ForcesPassThroughCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePassThroughRelatedControls();
        }

        private void GeneralTabPage_SizeChanged(object sender, EventArgs e)
        {
            GeneralCenterPanel.Left = (this.Width - GeneralCenterPanel.Width) / 2;
        }

        private void GameControllersButton_Click(object sender, EventArgs e)
        {
            var path = System.Environment.GetFolderPath(Environment.SpecialFolder.System);
            path += "\\joy.cpl";
            OpenPath(path, "");
        }

        void OpenPath(string path, string arguments = null)
        {
            try
            {
                var fi = new System.IO.FileInfo(path);
                //if (!fi.Exists) return;
                // Brings up the "Windows cannot open this file" dialog if association not found.
                var psi = new ProcessStartInfo(path);
                psi.UseShellExecute = true;
                psi.WorkingDirectory = fi.Directory.FullName;
                psi.ErrorDialog = true;
                if (arguments != null) psi.Arguments = arguments;
                Process.Start(psi);
            }
            catch (Exception) { }
        }


        private void FeedVirtualDeviceCeckBox_CheckedChanged(object sender, EventArgs e)
        {
            FeedingEnabled = EnableVirtualDeviceCeckBox.Checked;
            if (EnableVirtualDeviceCeckBox.Checked)
            {
                var resourceName = Program.GetResourceName("vJoy", "vJoyInterface");
                if (!System.IO.File.Exists("vJoyInterface.dll"))
                {
                    AppHelper.WriteFile(typeof(MainForm).Namespace + "." + resourceName + ".dll", "vJoyInterface.dll");
                }



                System.Threading.ThreadPool.QueueUserWorkItem(FeedWaitCallback, (uint)1);
            }
        }

        bool FeedingEnabled;

        void FeedWaitCallback(object state)
        {
            string message;
            var success = FeedDinputDevice((uint)state, out message);
            if (!string.IsNullOrEmpty(message) && !success)
            {
                MessageBox.Show(message);
            }
        }

        object TestTimerLock = new object();
        System.Timers.Timer TestTimer;

        bool _TestEnabled;
        public bool TestEnabled
        {
            get { lock (TestTimerLock) { return _TestEnabled; } }
            set
            {
                lock (TestTimerLock)
                {
                    _TestEnabled = value;
                    var list = SettingManager.Current.DisabledControls;
                    if (_TestEnabled)
                    {
                        if (!list.Contains(LeftMotorStrengthTrackBar)) list.Add(LeftMotorStrengthTrackBar);
                        if (!list.Contains(RightMotorStrengthTrackBar)) list.Add(RightMotorStrengthTrackBar);
                        TestTimer.Interval = (double)TestTimerNumericUpDown.Value;
                        TestTimer.Start();
                    }
                    else
                    {
                        TestTimer.Stop();
                        if (list.Contains(LeftMotorStrengthTrackBar)) list.Remove(LeftMotorStrengthTrackBar);
                        if (list.Contains(RightMotorStrengthTrackBar)) list.Remove(RightMotorStrengthTrackBar);
                    }
                    UpdateForceFeedBack();
                }
            }
        }

        private void TestButton_Click(object sender, EventArgs e)
        {
            var oldValue = TestEnabled;
            TestEnabled = !oldValue;
        }

        private void TestTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (TestTimerLock)
            {
                if (_TestEnabled)
                {
                    UpdateForceFeedBack();
                    TestTimer.Start();
                }
            }
        }

        private void TestTimerNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (TestEnabled)
            {
                TestEnabled = false;
                TestEnabled = true;
            }
        }
    }
}
