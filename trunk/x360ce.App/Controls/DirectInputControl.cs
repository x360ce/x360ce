using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Microsoft.DirectX.DirectInput;

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
		}

		void ShowDeviceInfo(Device device)
		{
			if (device == null)
			{
				// clean everything here.
				DiCapFfLabel.Text = string.Empty;
				DiCapAxesLabel.Text = string.Empty;
				DiCapButtonsLabel.Text = string.Empty;
				DiCapDPadsLabel.Text = string.Empty;
				DiEffectsTable.Rows.Clear();
				return;
			}
			var di = device.DeviceInformation;
			// Update pid and vid always so they wont be overwritten by load settings.
			short vid = BitConverter.ToInt16(di.ProductGuid.ToByteArray(), 0);
			short pid = BitConverter.ToInt16(di.ProductGuid.ToByteArray(), 2);
			//bool attached = false;
			//try
			//{
			//    attached = Manager.GetDeviceAttached(di.ProductGuid);
			//}
			//catch (Exception)
			//{
			//}
			DeviceVidTextBox.Text = string.Format("0x{0}", vid.ToString("X4"));
			DevicePidTextBox.Text = string.Format("0x{0}", pid.ToString("X4"));
			DeviceProductNameTextBox.Text = di.ProductName;
			DeviceProductGuidTextBox.Text = di.ProductGuid.ToString();
			DeviceInstanceGuidTextBox.Text = di.InstanceGuid.ToString();
			var state = device.CurrentJoystickState;
			DeviceTypeTextBox.Text = di.DeviceType.ToString();
			DiCapFfLabel.Text = string.Format("Force Feedback: {0}", device.Caps.ForceFeedback ? "YES" : "NO");
			DiCapAxesLabel.Text = string.Format("Axes: {0}", device.Caps.NumberAxes);
			DiCapButtonsLabel.Text = string.Format("Buttons: {0}", device.Caps.NumberButtons);
			DiCapDPadsLabel.Text = string.Format("D-Pads: {0}", device.Caps.NumberPointOfViews);
			DiEffectsTable.Rows.Clear();
			EffectList effects = device.GetEffects(EffectType.All);
			foreach (EffectInformation eff in effects)
			{
				DiEffectsTable.Rows.Add(new object[]{
		            eff.Name,
		            ((EffectParameterFlags)eff.StaticParams).ToString(),
		            ((EffectParameterFlags)eff.DynamicParams).ToString()
		        });
			}
		}

		JoystickState emptyState {
            get
            {
                return default(JoystickState);
            }
        }
  
		List<string> ShowDirectInputState(Device device)
		{
			List<string> actions = new List<string>();
			JoystickState state = emptyState;
			if (device != null)
			{
				try { state = device.CurrentJoystickState; }
				catch (Exception) { }
			}
			// X-axis.
			DiAxisTable.Rows[0][1] = state.X;
			DiAxisTable.Rows[0][2] = state.Rx;
			DiAxisTable.Rows[0][3] = state.AX;
			DiAxisTable.Rows[0][4] = state.ARx;
			DiAxisTable.Rows[0][5] = state.FX;
			DiAxisTable.Rows[0][6] = state.FRx;
			DiAxisTable.Rows[0][7] = state.VX;
			DiAxisTable.Rows[0][8] = state.VRx;
			// Y-axis.
			DiAxisTable.Rows[1][1] = state.Y;
			DiAxisTable.Rows[1][2] = state.Ry;
			DiAxisTable.Rows[1][3] = state.AY;
			DiAxisTable.Rows[1][4] = state.ARy;
			DiAxisTable.Rows[1][5] = state.FY;
			DiAxisTable.Rows[1][6] = state.FRy;
			DiAxisTable.Rows[1][7] = state.VY;
			DiAxisTable.Rows[1][8] = state.VRy;
			// Z-axis.
			DiAxisTable.Rows[2][1] = state.Z;
			DiAxisTable.Rows[2][2] = state.Rz;
			DiAxisTable.Rows[2][3] = state.AZ;
			DiAxisTable.Rows[2][4] = state.ARz;
			DiAxisTable.Rows[2][5] = state.FZ;
			DiAxisTable.Rows[2][6] = state.FRz;
			DiAxisTable.Rows[2][7] = state.VZ;
			DiAxisTable.Rows[2][8] = state.VRz;

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

			byte[] buttons = state.GetButtons();
			DiButtonsTextBox.Text = "";
			if (buttons != null)
			{

				for (int i = 0; i < buttons.Length; i++)
				{
					if (DiButtonsTextBox.Text.Length > 0) DiButtonsTextBox.Text += " ";
					if (0 != (buttons[i] & 0x80))
					{
						actions.Add(string.Format("Button {0}", i + 1));
						DiButtonsTextBox.Text += (i + 1).ToString("00");
					}
				}
			}
			// Sliders
			var sNum = 1;
			ProcessSlider(actions, state.GetASlider(), DiASliderTextBox, ref sNum);
			ProcessSlider(actions, state.GetFSlider(), DiFSliderTextBox, ref sNum);
			ProcessSlider(actions, state.GetSlider(), DiUvSliderTextBox, ref sNum);
			ProcessSlider(actions, state.GetVSlider(), DiVSliderTextBox, ref sNum);

			// Poin of view buttons
			int[] dPad = state.GetPointOfView();
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
			control.Text = "";
			if (sliders == null) return;
			int v;
			for (int i = 0; i < sliders.Length; i++)
			{
				v = sliders[i];
				if (control.Text.Length > 0) control.Text += " ";
				control.Text += v.ToString("00000");
				addAction(actions, v, "Slider", num++);
			}

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
			if (v > n1 && v < n2) return "I";
			if (v > p3 && v < p4) return "";
			if (v > n3 && v < n4) return "I";
			return null;
		}

		Guid deviceInstanceGuid;

		public List<string> UpdateFrom(Device device)
		{
			if (!Helper.IsSameDevice(device, deviceInstanceGuid))
			{
				ShowDeviceInfo(device);
				deviceInstanceGuid = device == null ? Guid.Empty : device.DeviceInformation.InstanceGuid;
			}
			return ShowDirectInputState(device);
		}

	}

}
