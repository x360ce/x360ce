using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using SharpDX.DirectInput;
using SharpDX.XInput;
using System.Linq;

namespace x360ce.App.Controls
{
    public partial class DirectInputControl : UserControl
    {
        public DirectInputControl()
        {
            InitializeComponent();
            InitDirectInputTab();
        }

        DataTable DiAxisTable;
        DataTable DiEffectsTable;
        DataTable DiObjectsTable;

        void InitDirectInputTab()
        {
            System.Windows.Forms.ToolTip ToolTip1 = new System.Windows.Forms.ToolTip();
            DiAxisTable = new DataTable();
            DiAxisTable.Columns.Add("Axis", typeof(string));
            DiAxisTable.Columns.Add("M", typeof(int));
            DiAxisTable.Columns.Add("R", typeof(int));
            DiAxisTable.Columns.Add("A", typeof(int));
            DiAxisTable.Columns.Add("AR", typeof(int));
            DiAxisTable.Columns.Add("F", typeof(int));
            DiAxisTable.Columns.Add("FR", typeof(int));
            DiAxisTable.Columns.Add("V", typeof(int));
            DiAxisTable.Columns.Add("VR", typeof(int));
            DiAxisTable.Rows.Add(DiAxisTable.NewRow());
            DiAxisTable.Rows.Add(DiAxisTable.NewRow());
            DiAxisTable.Rows.Add(DiAxisTable.NewRow());
            DiAxisDataGridView.DataSource = DiAxisTable;
            DiAxisTable.Rows[0][0] = "X";
            DiAxisTable.Rows[1][0] = "Y";
            DiAxisTable.Rows[2][0] = "Z";
            DiEffectsTable = new DataTable();
            DiEffectsTable.Columns.Add("Effect", typeof(string));
            DiEffectsTable.Columns.Add("Parameters", typeof(string));
            DiEffectsTable.Columns.Add("DynamicParameters", typeof(string));
            DiEffectsDataGridView.DataSource = DiEffectsTable;
            DiObjectsTable = new DataTable();
            DiObjectsTable.Columns.Add("Instance", typeof(string));
            DiObjectsTable.Columns.Add("Usage", typeof(string));
            DiObjectsTable.Columns.Add("Name", typeof(string));
            DiObjectsTable.Columns.Add("Flags", typeof(string));
            DiObjectsDataGridView.DataSource = DiObjectsTable;
        }

        void ShowDeviceInfo(Joystick device)
        {
            if (device == null)
            {
                // clean everything here.
                SetValue(DeviceProductNameTextBox, "");
                SetValue(DeviceProductGuidTextBox, "");
                SetValue(DeviceInstanceGuidTextBox, "");
                DiCapFfStateTextBox.Text = string.Empty;
                DiCapAxesTextBox.Text = string.Empty;
                DiCapButtonsTextBox.Text = string.Empty;
                DiCapDPadsTextBox.Text = string.Empty;
                DiEffectsTable.Rows.Clear();
                return;
            }
            lock (MainForm.XInputLock)
            {
                var isLoaded = XInput.IsLoaded;
                if (isLoaded) XInput.FreeLibrary();
                device.Unacquire();
                device.SetCooperativeLevel(MainForm.Current, CooperativeLevel.Foreground | CooperativeLevel.Exclusive);
                effects = new List<EffectInfo>();
                try
                {
                    device.Acquire();
                    var forceFeedback = device.Capabilities.Flags.HasFlag(DeviceFlags.ForceFeedback);
                    forceFeedbackState = forceFeedback ? "YES" : "NO";
                    effects = device.GetEffects(EffectType.All);
                }
                catch (Exception)
                {
                    forceFeedbackState = "ERROR";
                }
                DiEffectsTable.Rows.Clear();
                foreach (var eff in effects)
                {
                    DiEffectsTable.Rows.Add(new object[]{
		                        eff.Name,
		                        ((EffectParameterFlags)eff.StaticParameters).ToString(),
		                        ((EffectParameterFlags)eff.DynamicParameters).ToString()
		                    });
                }
                device.Unacquire();
                device.SetCooperativeLevel(MainForm.Current, CooperativeLevel.Background | CooperativeLevel.NonExclusive);
                if (isLoaded)
                {
                    Exception error;
                    XInput.ReLoadLibrary(XInput.LibraryName, out error);
                }
            }
            DiCapFfStateTextBox.Text = forceFeedbackState;
            DiCapAxesTextBox.Text = device.Capabilities.AxeCount.ToString();
            DiCapButtonsTextBox.Text = device.Capabilities.ButtonCount.ToString();
            DiCapDPadsTextBox.Text = device.Capabilities.PovCount.ToString();
            var objects = device.GetObjects(DeviceObjectTypeFlags.All).OrderBy(x=>x.Usage).ToArray();
            DiObjectsTable.Rows.Clear();
            foreach (var o in objects)
            {
                DiObjectsTable.Rows.Add(new object[]{
                                o.ObjectId.InstanceNumber,  
                                o.Usage,
                                o.Name,
		                        o.ObjectId.Flags,
		                    });
            }
            var actuators = objects.Where(x => x.ObjectId.Flags.HasFlag(DeviceObjectTypeFlags.ForceFeedbackActuator));
            ActuatorsTextBox.Text = actuators.Count().ToString();
            var di = device.Information;
            // Update pid and vid always so they wont be overwritten by load settings.
            short vid = BitConverter.ToInt16(di.ProductGuid.ToByteArray(), 0);
            short pid = BitConverter.ToInt16(di.ProductGuid.ToByteArray(), 2);
            SetValue(DeviceVidTextBox, "0x{0}", vid.ToString("X4"));
            SetValue(DevicePidTextBox, "0x{0}", pid.ToString("X4"));
            SetValue(DeviceProductNameTextBox, di.ProductName);
            SetValue(DeviceProductGuidTextBox, di.ProductGuid.ToString());
            SetValue(DeviceInstanceGuidTextBox, di.InstanceGuid.ToString());
            SetValue(DeviceTypeTextBox, di.Type.ToString());
        }

        void SetValue(Control control, string value, params object[] args)
        {
            var s = string.Format(value, args);
            if (control.Text == s) return;
            control.Text = s;
        }

        JoystickState oldState;
        List<string> actions = new List<string>();
        IList<EffectInfo> effects;
        string forceFeedbackState;

        public int[] Axis = new int[0xf];

        List<string> ShowDirectInputState(Joystick device)
        {
            JoystickState state = null;
            if (device != null)
            {
                try
                {
                    device.Acquire();
                    state = device.GetCurrentState();
                }
                catch (Exception ex)
                {
                    var error = ex;
                }
            }

            if (state == null || state.Equals(oldState)) return actions;

            // Fill axis.
            Axis[0] = state.X;
            Axis[1] = state.Y;
            Axis[2] = state.Z;
            Axis[3] = state.RotationX;
            Axis[4] = state.RotationY;
            Axis[5] = state.RotationZ;

            oldState = state;
            actions.Clear();
            // X-axis.
            DiAxisTable.Rows[0][1] = state.X;
            DiAxisTable.Rows[0][2] = state.RotationX;
            DiAxisTable.Rows[0][3] = state.AccelerationX;
            DiAxisTable.Rows[0][4] = state.AngularAccelerationX;
            DiAxisTable.Rows[0][5] = state.ForceX;
            DiAxisTable.Rows[0][6] = state.TorqueX;
            DiAxisTable.Rows[0][7] = state.VelocityX;
            DiAxisTable.Rows[0][8] = state.AngularVelocityX;
            // Y-axis.
            DiAxisTable.Rows[1][1] = state.Y;
            DiAxisTable.Rows[1][2] = state.RotationY;
            DiAxisTable.Rows[1][3] = state.AccelerationY;
            DiAxisTable.Rows[1][4] = state.AngularAccelerationY;
            DiAxisTable.Rows[1][5] = state.ForceY;
            DiAxisTable.Rows[1][6] = state.TorqueY;
            DiAxisTable.Rows[1][7] = state.VelocityY;
            DiAxisTable.Rows[1][8] = state.AngularVelocityY;
            // Z-axis.
            DiAxisTable.Rows[2][1] = state.Z;
            DiAxisTable.Rows[2][2] = state.RotationZ;
            DiAxisTable.Rows[2][3] = state.AccelerationZ;
            DiAxisTable.Rows[2][4] = state.AngularAccelerationZ;
            DiAxisTable.Rows[2][5] = state.ForceZ;
            DiAxisTable.Rows[2][6] = state.TorqueZ;
            DiAxisTable.Rows[2][7] = state.VelocityZ;
            DiAxisTable.Rows[2][8] = state.AngularVelocityZ;

            var rows = DiAxisTable.Rows;
            var cols = DiAxisTable.Columns;
            int v;
            int axisNum;
            for (int r = 0; r < rows.Count; r++)
            {
                for (int c = 1; c < cols.Count; c++)
                {
                    if (System.DBNull.Value == rows[r][c]) continue;
                    v = (int)rows[r][c];
                    axisNum = (c - 1) * rows.Count + r + 1;
                    addAction(actions, v, "Axis", axisNum);
                }
            }

            bool[] buttons = state.Buttons;
            DiButtonsTextBox.Text = "";
            if (buttons != null)
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i])
                    {
                        actions.Add(string.Format("Button {0}", i + 1));
                        if (DiButtonsTextBox.Text.Length > 0) DiButtonsTextBox.Text += " ";
                        DiButtonsTextBox.Text += (i + 1).ToString("00");
                    }
                }
            }
            // Sliders
            var sNum = 1;
            ProcessSlider(actions, state.Sliders, DiUvSliderTextBox, ref sNum);
            ProcessSlider(actions, state.Sliders, DiASliderTextBox, ref sNum);
            ProcessSlider(actions, state.Sliders, DiFSliderTextBox, ref sNum);
            ProcessSlider(actions, state.Sliders, DiVSliderTextBox, ref sNum);

            // Poin of view buttons
            int[] dPad = state.PointOfViewControllers;
            DiDPadTextBox.Text = "";
            if (dPad != null)
            {
                for (int i = 0; i < dPad.Length; i++)
                {
                    v = dPad[i];
                    if (DiDPadTextBox.Text.Length > 0) DiDPadTextBox.Text += " ";
                    if (v != -1)
                    {
                        DiDPadTextBox.Text += "[" + i + "," + v.ToString() + "]";
                        if ((DPadEnum)v == DPadEnum.Up) actions.Add(string.Format("DPad {0} {1}", i + 1, DPadEnum.Up.ToString()));
                        if ((DPadEnum)v == DPadEnum.Right) actions.Add(string.Format("DPad {0} {1}", i + 1, DPadEnum.Right.ToString()));
                        if ((DPadEnum)v == DPadEnum.Down) actions.Add(string.Format("DPad {0} {1}", i + 1, DPadEnum.Down.ToString()));
                        if ((DPadEnum)v == DPadEnum.Left) actions.Add(string.Format("DPad {0} {1}", i + 1, DPadEnum.Left.ToString()));
                    }
                }
            }
            return actions;
        }

        void ProcessSlider(List<string> actions, int[] sliders, TextBox control, ref int num)
        {
            var s = "";
            if (sliders != null)
            {
                int v;
                for (int i = 0; i < sliders.Length; i++)
                {
                    v = sliders[i];
                    if (s.Length > 0) s += " ";
                    s += v.ToString("00000");
                    addAction(actions, v, "Slider", num++);
                }
            }
            SetValue(control, s);
        }

        void addAction(List<string> actions, int v, string type, int index)
        {
            string d = (DetectDirection(v));
            if (d == null) return;
            actions.Add(string.Format("{0}{1} {2:0}", d, type, index));
        }

        public string DetectDirection(int v)
        {
            // Threshold mark at which action on axis/slider is detected.
            // Value gets inbetween of specified range then action is recorded.
            // [--[p1]----[p2]--[n1]----[n2]--|--[p3]----[p4]--[n3]----[n4]--]
            int p1 = 2000;
            int space = (ushort.MaxValue - (p1 * 6)) / 4;
            int p2 = p1 + space;
            int n1 = p2 + p1;
            int n2 = n1 + space;
            int p3 = n2 + (p1 * 2);
            int p4 = p3 + space;
            int n3 = p4 + p1;
            int n4 = n3 + space;
            if (v > p1 && v < p2) return "";
            if (v > n1 && v < n2) return (isWheel) ? "IH" : "I";
            if (v > p3 && v < p4) return (isWheel) ? "H" : "";
            if (v > n3 && v < n4) return "I";
            return null;
        }

        Guid deviceInstanceGuid;
        bool isWheel = false;

        public List<string> UpdateFrom(Joystick device)
        {
            if (!Helper.IsSameDevice(device, deviceInstanceGuid))
            {
                ShowDeviceInfo(device);
                deviceInstanceGuid = Guid.Empty;
                if (device != null)
                {
                    deviceInstanceGuid = device.Information.InstanceGuid;
                    isWheel = device.Information.Type == SharpDX.DirectInput.DeviceType.Driving;
                }
            }
            return ShowDirectInputState(device);
        }

        private void DirectInputControl_Load(object sender, EventArgs e)
        {
            Helper.EnableDoubleBuffering(DiAxisDataGridView);
            Helper.EnableDoubleBuffering(DiEffectsDataGridView);
        }

    }

}
