using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using x360ce.App.XnaInput;
using System.Text.RegularExpressions;
using Microsoft.DirectX.DirectInput;

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
            var types = (ControllerType[])Enum.GetValues(typeof(ControllerType));
            foreach (var item in types) GamePadTypeComboBox.Items.Add(item);
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
                mainForm.SetComboBoxValue(CurrentCbx, actions[0]);
                CurrentCbx.ForeColor = SystemColors.WindowText;
                mainForm.SaveSetting(CurrentCbx);
                mainForm.NotifySettingsChange();
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
            bool on = Controller.IsConnected;
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
                // Temp workaround: when initialized triggers have default value of 127);
                if (GamePad.LeftTrigger == 127 && GamePad.RightTrigger == 127)
                {
                    this.LeftTriggerTextBox.Text = "0";
                    this.RightTriggerTextBox.Text = "0";
                }
                else
                {
                    UpdateControl(LeftTriggerTextBox, GamePad.LeftTrigger.ToString());
                    UpdateControl(RightTriggerTextBox, GamePad.RightTrigger.ToString());
                    on = GamePad.LeftTrigger > 0;
                    setLabelColor(on, LeftTriggerLabel);
                    if (on) e.Graphics.DrawImage(this.markB, triggerLeft.X + mW, triggerLeft.Y + mH);
                    on = GamePad.RightTrigger > 0;
                    setLabelColor(on, RightTriggerLabel);
                    if (on) e.Graphics.DrawImage(this.markB, triggerRight.X + mW, triggerRight.Y + mH);
                }
                on = Controller.IsButtonDown(ButtonValues.LeftShoulder);
                setLabelColor(on, LeftShoulderLabel);
                if (on) e.Graphics.DrawImage(this.markB, shoulderLeft.X + mW, shoulderLeft.Y + mH);
                on = Controller.IsButtonDown(ButtonValues.RightShoulder);
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
            Point buttonBack = new Point(103, 48);
            Point buttonStart = new Point(152, 48);
            Point[] pads = new Point[4];
            pads[0] = new Point(116, 35);
            pads[1] = new Point(139, 35);
            pads[2] = new Point(116, 62);
            pads[3] = new Point(139, 62);
            // Display controller.
            bool on = Controller.IsConnected;
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

                on = Controller.IsButtonDown(ButtonValues.RightThumb);
                if (on) e.Graphics.DrawImage(this.markB, thumbRight.X + mW, thumbRight.Y + mH);
                e.Graphics.DrawImage(this.markA, (float)((thumbRight.X + mW) + (this.rightX * padSize)), (float)((thumbRight.Y + mH) + (-this.rightY * padSize)));
                setLabelColor(on, RightThumbButtonLabel);

                on = Controller.IsButtonDown(ButtonValues.LeftThumb);
                if (on) e.Graphics.DrawImage(this.markB, thumbLeft.X + mW, thumbLeft.Y + mH);
                e.Graphics.DrawImage(this.markA, (float)((thumbLeft.X + mW) + (this.leftX * padSize)), (float)((thumbLeft.Y + mH) + (-this.leftY * padSize)));
                setLabelColor(on, LeftThumbButtonLabel);

                on = Controller.IsButtonDown(ButtonValues.Y);
                if (on) e.Graphics.DrawImage(this.markB, buttonY.X + mW, buttonY.Y + mH);
                setLabelColor(on, ButtonYLabel);

                on = Controller.IsButtonDown(ButtonValues.X);
                if (on) e.Graphics.DrawImage(this.markB, buttonX.X + mW, buttonX.Y + mH);
                setLabelColor(on, ButtonXLabel);

                on = Controller.IsButtonDown(ButtonValues.B);
                if (on) e.Graphics.DrawImage(this.markB, buttonB.X + mW, buttonB.Y + mH);
                setLabelColor(on, ButtonBLabel);

                on = Controller.IsButtonDown(ButtonValues.A);
                if (on) e.Graphics.DrawImage(this.markB, buttonA.X + mW, buttonA.Y + mH);
                setLabelColor(on, ButtonALabel);

                on = Controller.IsButtonDown(ButtonValues.Start);
                if (on) e.Graphics.DrawImage(this.markB, buttonStart.X + mW, buttonStart.Y + mH);
                setLabelColor(on, StartButtonLabel);

                on = Controller.IsButtonDown(ButtonValues.Back);
                if (on) e.Graphics.DrawImage(this.markB, buttonBack.X + mW, buttonBack.Y + mH);
                setLabelColor(on, BackButtonLabel);

                on = Controller.IsButtonDown(ButtonValues.Up);
                if (on) e.Graphics.DrawImage(this.markB, dPadUp.X + mW, dPadUp.Y + mH);
                setLabelColor(on, DPadUpLabel);

                on = Controller.IsButtonDown(ButtonValues.Down);
                if (on) e.Graphics.DrawImage(this.markB, dPadDown.X + mW, dPadDown.Y + mH);
                setLabelColor(on, DPadDownLabel);

                on = Controller.IsButtonDown(ButtonValues.Left);
                if (on) e.Graphics.DrawImage(this.markB, dPadLeft.X + mW, dPadLeft.Y + mH);
                setLabelColor(on, DPadLeftLabel);

                on = Controller.IsButtonDown(ButtonValues.Right);
                if (on) e.Graphics.DrawImage(this.markB, dPadRight.X + mW, dPadRight.Y + mH);
                setLabelColor(on, DPadRightLabel);

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
            Dictionary<string, Control> map = new Dictionary<string, Control>();
            string section = string.Format(@"PAD{0}\", ControllerIndex + 1);
            // FakeAPI
            map.Add(section + SettingName.ProductName, diControl.DeviceProductNameTextBox);
            map.Add(section + SettingName.InstanceGuid, diControl.DeviceInstanceGuidTextBox);
            map.Add(section + SettingName.Vid, diControl.DeviceVidTextBox);
            map.Add(section + SettingName.Pid, diControl.DevicePidTextBox);
            map.Add(section + SettingName.GamePadType, GamePadTypeComboBox);
            map.Add(section + SettingName.NativeMode, NativeModeCheckBox);
            // Mappings
            map.Add(section + SettingName.LeftThumbAxisX, LeftThumbAxisXComboBox);
            map.Add(section + SettingName.LeftThumbAxisY, LeftThumbAxisYComboBox);
            map.Add(section + SettingName.RightThumbAxisX, RightThumbAxisXComboBox);
            map.Add(section + SettingName.RightThumbAxisY, RightThumbAxisYComboBox);
            map.Add(section + SettingName.LeftThumbRight, LeftThumbRightComboBox);
            map.Add(section + SettingName.LeftThumbLeft, LeftThumbLeftComboBox);
            map.Add(section + SettingName.LeftThumbUp, LeftThumbUpComboBox);
            map.Add(section + SettingName.LeftThumbDown, LeftThumbDownComboBox);
            map.Add(section + SettingName.RightThumbRight, RightThumbRightComboBox);
            map.Add(section + SettingName.RightThumbLeft, RightThumbLeftComboBox);
            map.Add(section + SettingName.RightThumbUp, RightThumbUpComboBox);
            map.Add(section + SettingName.RightThumbDown, RightThumbDownComboBox);
            map.Add(section + SettingName.DPad, DPadComboBox);
            map.Add(section + SettingName.DPadUp, DPadUpComboBox);
            map.Add(section + SettingName.DPadDown, DPadDownComboBox);
            map.Add(section + SettingName.DPadLeft, DPadLeftComboBox);
            map.Add(section + SettingName.DPadRight, DPadRightComboBox);
            map.Add(section + SettingName.ButtonBack, ButtonBackComboBox);
            map.Add(section + SettingName.ButtonStart, ButtonStartComboBox);
            map.Add(section + SettingName.ButtonA, ButtonAComboBox);
            map.Add(section + SettingName.ButtonB, ButtonBComboBox);
            map.Add(section + SettingName.ButtonX, ButtonXComboBox);
            map.Add(section + SettingName.ButtonY, ButtonYComboBox);
            map.Add(section + SettingName.LeftShoulder, LeftShoulderComboBox);
            map.Add(section + SettingName.RightShoulder, RightShoulderComboBox);
            map.Add(section + SettingName.LeftThumbButton, LeftThumbButtonComboBox);
            map.Add(section + SettingName.RightThumbButton, RightThumbButtonComboBox);
            map.Add(section + SettingName.LeftTrigger, LeftTriggerComboBox);
            map.Add(section + SettingName.LeftTriggerDeadZone, LeftTriggerDeadZoneTrackBar);
            map.Add(section + SettingName.RightTrigger, RightTriggerComboBox);
            map.Add(section + SettingName.RightTriggerDeadZone, RightTriggerDeadZoneTrackBar);
            // Force Feedback
            map.Add(section + SettingName.ForceEnable, FfEnableCheckBox);
            map.Add(section + SettingName.ForceSwapMotor, FfSwapMotorCheckBox);
            map.Add(section + SettingName.ForceLeftMotorInvert, FfLeftMotorInvertCheckBox);
            map.Add(section + SettingName.ForceRightMotorInvert, FfRightMotorInvertCheckBox);
            map.Add(section + SettingName.ForceOverall, FfOverallTrackBar);
            // Axis To D-Pad
            map.Add(section + SettingName.AxisToDPadEnabled, AxisToDPadEnabledCheckBox);
            map.Add(section + SettingName.AxisToDPadDeadZone, AxisToDPadDeadZoneTrackBar);
            map.Add(section + SettingName.AxisToDPadOffset, AxisToDPadOffsetTrackBar);
            return map;
        }

        #endregion

        private int leftX;
        private int leftY;
        private int rightX;
        private int rightY;

        Controller Controller;
        XINPUT_GAMEPAD GamePad;
        Guid instanceGuid;

        public void UpdateFrom(Controller controller, Device device)
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
                catch (Exception) { if (mainForm.IsDebugMode) throw; }
                instanceGuid = (device == null) ? Guid.Empty : iGuid;
                ResetDiMenuStrip(device);
            }
            Controller = controller;
            IsEnabled = controller.IsConnected;
            if (!IsEnabled) return;
            GamePad = controller.State.GamePad;
            this.leftX = GamePad.ThumbLX;
            this.leftY = GamePad.ThumbLY;
            this.rightX = GamePad.ThumbRX;
            this.rightY = GamePad.ThumbRY;
            UpdateControl(LeftThumbTextBox, string.Format("{0};{1}", this.leftX, this.leftY));
            UpdateControl(RightThumbTextBox, string.Format("{0};{1}", this.rightX, this.rightY));
            //ButtonValues buttons = CurrentPad.Buttons;
            this.TopPictureBox.Refresh();
            this.FrontPictureBox.Refresh();
            //this.lastButtonsPressed = buttons;
            //ShowDirectInputState();
        }

        // Use this to reduce flicekring.
        public void UpdateControl(Control control, string text)
        {
            if (control.Text != text) control.Text = text;
        }

        bool _IsEnabled;
        public bool IsEnabled
        {
            set
            {
                if (_IsEnabled == value) return;
                if (value)
                {
                    this.FrontPictureBox.Image = frontImage;
                    this.TopPictureBox.Image = topImage;
                    this.FrontPictureBox.Paint += new System.Windows.Forms.PaintEventHandler(this.FrontPictureBox_Paint);
                    this.TopPictureBox.Paint += new System.Windows.Forms.PaintEventHandler(this.TopPictureBox_Paint);
                }
                else
                {
                    this.FrontPictureBox.Image = frontDisabledImage;
                    this.TopPictureBox.Image = topDisabledImage;
                    this.FrontPictureBox.Paint -= new System.Windows.Forms.PaintEventHandler(this.FrontPictureBox_Paint);
                    this.TopPictureBox.Paint -= new System.Windows.Forms.PaintEventHandler(this.TopPictureBox_Paint);
                }
                _IsEnabled = value;
            }
            get { return _IsEnabled; }

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
                    mainForm.SetComboBoxValue(CurrentCbx, item.Text);
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
                    mainForm.SetComboBoxValue(CurrentCbx, string.Empty);
                    CurrentCbx = null;
                }
                else
                {
                    mainForm.SetComboBoxValue(CurrentCbx, item.Text);
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

        private void FfOverallTrackBar_ValueChanged(object sender, EventArgs e)
        {
            TrackBar control = (TrackBar)sender;
            FfOverallTextBox.Text = string.Format("{0} % ", control.Value);
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

        private void LeftMotorTestTrackBar_ValueChanged(object sender, EventArgs e)
        {
            if (Controller == null) return;
            float value = (float)LeftMotorTestTrackBar.Value / 100F * (float)ushort.MaxValue;
            short speed = (short)value;
            LeftMotorTestTextBox.Text = string.Format("{0} ", speed);
            Controller.LeftMotorSpeed = speed;
            Controller.LeftMotorSpeed = speed;
        }

        private void RightMotorTestTrackBar_ValueChanged(object sender, EventArgs e)
        {
            if (Controller == null) return;
            float value = (float)RightMotorTestTrackBar.Value / 100F * (float)ushort.MaxValue;
            short speed = (short)value;
            RightMotorTestTextBox.Text = string.Format("{0} ", speed);
            Controller.RightMotorSpeed = speed;
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

    }
}
