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
using x360ce.Engine;
using JocysCom.ClassLibrary.IO;

namespace x360ce.App.Controls
{
    public partial class DirectInputControl : UserControl
    {
        public DirectInputControl()
        {
            InitializeComponent();
            InitDirectInputTab();
            DiObjectsDataGridView.AutoGenerateColumns = false;
        }

        DataTable DiAxisTable;
        DataTable DiEffectsTable;

        void InitDirectInputTab()
        {
            var ToolTip1 = new ToolTip();
            DiAxisTable = new DataTable();
            // http://msdn.microsoft.com/en-us/library/windows/desktop/bb151904%28v=vs.85%29.aspx
            DiAxisTable.Columns.Add("Axis", typeof(string));
            DiAxisTable.Columns.Add("M", typeof(int));
            DiAxisTable.Columns.Add("R", typeof(int)); // Rotation
            DiAxisTable.Columns.Add("A", typeof(int)); // Acceleration
            DiAxisTable.Columns.Add("AR", typeof(int)); // AngularAcceleration
            DiAxisTable.Columns.Add("F", typeof(int)); // Force
            DiAxisTable.Columns.Add("FR", typeof(int)); // Torque
            DiAxisTable.Columns.Add("V", typeof(int)); // Velocity
            DiAxisTable.Columns.Add("VR", typeof(int)); // AngularVelocity
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
        }

        void ShowDeviceInfo(Joystick device, DeviceInfo dInfo)
        {
            if (device == null)
            {
                // clean everything here.
                AppHelper.SetText(DeviceProductNameTextBox, "");
                AppHelper.SetText(DeviceVendorNameTextBox, "");
                AppHelper.SetText(DeviceProductGuidTextBox, "");
                AppHelper.SetText(DeviceInstanceGuidTextBox, "");
                AppHelper.SetText(DiCapFfStateTextBox, "");
                AppHelper.SetText(DiCapAxesTextBox, "");
                AppHelper.SetText(DiCapButtonsTextBox, "");
                AppHelper.SetText(DiCapDPadsTextBox, "");
                if (DiEffectsTable.Rows.Count > 0) DiEffectsTable.Rows.Clear();
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
            AppHelper.SetText(DiCapFfStateTextBox, forceFeedbackState);
            AppHelper.SetText(DiCapButtonsTextBox, device.Capabilities.ButtonCount.ToString());
            AppHelper.SetText(DiCapDPadsTextBox, device.Capabilities.PovCount.ToString());
            var objects = AppHelper.GetDeviceObjects(device);
            DiObjectsDataGridView.DataSource = objects;
            var actuators = objects.Where(x => x.Flags.HasFlag(DeviceObjectTypeFlags.ForceFeedbackActuator));
            AppHelper.SetText(ActuatorsTextBox, actuators.Count().ToString());
            var di = device.Information;
            var slidersCount = objects.Where(x => x.GuidValue.Equals(SharpDX.DirectInput.ObjectGuid.Slider)).Count();
            AppHelper.SetText(DiCapAxesTextBox, (device.Capabilities.AxeCount - slidersCount).ToString());
            AppHelper.SetText(SlidersTextBox, slidersCount.ToString());
            // Update PID and VID always so they wont be overwritten by load settings.
            short vid = BitConverter.ToInt16(di.ProductGuid.ToByteArray(), 0);
            short pid = BitConverter.ToInt16(di.ProductGuid.ToByteArray(), 2);
			AppHelper.SetText(DeviceVidTextBox, "0x{0:X4}", vid);
            AppHelper.SetText(DevicePidTextBox, "0x{0:X4}", pid);
			AppHelper.SetText(DeviceProductNameTextBox, di.ProductName);
            AppHelper.SetText(DeviceVendorNameTextBox, dInfo == null ? "" : dInfo.Manufacturer);
			AppHelper.SetText(DeviceRevTextBox, "0x{0:X4}", dInfo == null ? 0 : dInfo.Revision);
			AppHelper.SetText(DeviceProductGuidTextBox, di.ProductGuid.ToString());
            AppHelper.SetText(DeviceInstanceGuidTextBox, di.InstanceGuid.ToString());
            AppHelper.SetText(DeviceTypeTextBox, di.Type.ToString());
        }

        JoystickState oldState;
        // List<string> actions = new List<string>();
        IList<EffectInfo> effects;
        string forceFeedbackState;

        public int[] Axis = new int[6];

        /// <summary>
        /// Update DirectInput control from DirectInput device.
        /// </summary>
        /// <param name="device">DirectInput device.</param>
        /// <returns>List of buttons/DPad pressed, axis/sliders turned.</returns>
        void ShowDirectInputState(JoystickState state)
        {
            if (state == null || state.Equals(oldState)) return;

            // Fill axis.
            Axis[0] = state.X;
            Axis[1] = state.Y;
            Axis[2] = state.Z;
            Axis[3] = state.RotationX;
            Axis[4] = state.RotationY;
            Axis[5] = state.RotationZ;

            oldState = state;
            //actions.Clear();
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
                    //addAction(actions, v, "Axis", axisNum);
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
                        //actions.Add(string.Format("Button {0}", i + 1));
                        if (DiButtonsTextBox.Text.Length > 0) DiButtonsTextBox.Text += " ";
                        DiButtonsTextBox.Text += (i + 1).ToString("00");
                    }
                }
            }
            // Sliders
            ProcessSlider(state.Sliders, DiUvSliderTextBox);
            ProcessSlider(state.AccelerationSliders, DiASliderTextBox);
            ProcessSlider(state.ForceSliders, DiFSliderTextBox);
            ProcessSlider(state.VelocitySliders, DiVSliderTextBox);

            // Point of view buttons
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
                        //if ((DPadEnum)v == DPadEnum.Up) actions.Add(string.Format("DPad {0} {1}", i + 1, DPadEnum.Up.ToString()));
                        //if ((DPadEnum)v == DPadEnum.Right) actions.Add(string.Format("DPad {0} {1}", i + 1, DPadEnum.Right.ToString()));
                        //if ((DPadEnum)v == DPadEnum.Down) actions.Add(string.Format("DPad {0} {1}", i + 1, DPadEnum.Down.ToString()));
                        //if ((DPadEnum)v == DPadEnum.Left) actions.Add(string.Format("DPad {0} {1}", i + 1, DPadEnum.Left.ToString()));
                    }
                }
            }
            //return actions;
        }

        void ProcessSlider(int[] sliders, TextBox control)
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
                }
            }
            AppHelper.SetText(control, s);
        }

        void addAction(List<string> actions, int v, string type, int index)
        {
            string d = DetectDirection(v);


            if (d == null) return;
            actions.Add(string.Format("{0}{1} {2:0}", d, type, index));
        }

        public string DetectDirection(int v)
        {
            // Threshold mark at which action on axis/slider is detected.
            // If value gets in-between of specified range then action is recorded.
            // [       ""           "IH"              "H"           "I"      ]
            // [--[p1]----[p2]--[n1]----[n2]--|--[p3]----[p4]--[n3]----[n4]--]
            // [--    --->          <---             --->          <---      ]
            // Point width.
            int p1 = 2000;
            // Calculate space between points (~13384).
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

        public void UpdateFrom(DiDevice diDevice, out JoystickState state)
        {
            state = null;
            if (diDevice != null)
            {
                var device = diDevice.Device;
                var info = diDevice.HidInfo;
                if (!AppHelper.IsSameDevice(device, deviceInstanceGuid))
                {
                    ShowDeviceInfo(device, info);
                    deviceInstanceGuid = Guid.Empty;
                    if (device != null)
                    {
                        deviceInstanceGuid = device.Information.InstanceGuid;
                        isWheel = device.Information.Type == SharpDX.DirectInput.DeviceType.Driving;
                    }
                }
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
            ShowDirectInputState(state);
        }

        private void DirectInputControl_Load(object sender, EventArgs e)
        {
            EngineHelper.EnableDoubleBuffering(DiAxisDataGridView);
            EngineHelper.EnableDoubleBuffering(DiEffectsDataGridView);
        }

        private void CopyWithHeadersMenuItem_Click(object sender, EventArgs e)
        {
            var objects = DiObjectsDataGridView.DataSource as DeviceObjectItem[];
            var sb = new StringBuilder();
            var maxGuidName = objects.Max(x => x.GuidName.Length);
            var maxName = objects.Max(x => x.Name.Length);
            var maxFlags = objects.Max(x => x.Flags.ToString().Length);
            var names = new string[] { "Offset", "Usage", "Instance", "Guid", "Name", "Flags" };
            var sizes = new int[] { 6, 6, 8, -maxGuidName, -maxName, -maxFlags };
            var format = "// ";
            for (int i = 0; i < sizes.Length; i++)
            {
                if (i > 0) format += "  ";
                format += "{" + i.ToString() + "," + sizes[i].ToString() + "}";
            }
            sb.AppendFormat(format, names).AppendLine();
            sb.Append("// ");
            for (int i = 0; i < sizes.Length; i++)
            {
                if (i > 0) sb.Append("  ");
                sb.Append('-', Math.Abs(sizes[i]));
            }
            sb.AppendLine();
            for (int i = 0; i < objects.Length; i++)
            {
                var o = objects[i];
                sb.AppendFormat(format, o.Offset, o.Usage, o.Instance, o.GuidName, o.Name, o.Flags);
                sb.AppendLine();
            }
            Clipboard.SetDataObject(sb.ToString());
        }
    }

}
