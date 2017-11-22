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
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	public partial class DirectInputUserControl : UserControl
	{
		public DirectInputUserControl()
		{
			oldState = new JoystickState();
			emptyState = oldState;
			InitializeComponent();
			InitDirectInputTab();
			DiObjectsDataGridView.AutoGenerateColumns = false;
		}

		DataTable DiAxisTable;
		DataTable DiSlidersTable;
		DataTable DiPovsTable;
		DataTable DiEffectsTable;

		void InitDirectInputTab()
		{
			var ToolTip1 = new ToolTip();
			CreateAxisTable();
			CreateSlidersTable();
			CreatePovsTable();
			// Create effects table.
			DiEffectsTable = new DataTable();
			DiEffectsTable.Columns.Add("Effect", typeof(string));
			DiEffectsTable.Columns.Add("Parameters", typeof(string));
			DiEffectsTable.Columns.Add("DynamicParameters", typeof(string));
			DiEffectsDataGridView.DataSource = DiEffectsTable;
		}

		#region Axis Table

		void CreateAxisTable()
		{
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
			DiAxisTable.Rows.Add(DiAxisTable.NewRow()); // X
			DiAxisTable.Rows.Add(DiAxisTable.NewRow()); // Y
			DiAxisTable.Rows.Add(DiAxisTable.NewRow()); // Z
			DiAxisTable.Rows[0][0] = "X";
			DiAxisTable.Rows[1][0] = "Y";
			DiAxisTable.Rows[2][0] = "Z";
			DiAxisDataGridView.DataSource = DiAxisTable;
		}

		void UpdateAxisTable(JoystickState state)
		{
			// X-axis.
			if (oldState.X != state.X)
				DiAxisTable.Rows[0][1] = state.X;
			if (oldState.RotationX != state.RotationX)
				DiAxisTable.Rows[0][2] = state.RotationX;
			if (oldState.AccelerationX != state.AccelerationX)
				DiAxisTable.Rows[0][3] = state.AccelerationX;
			if (oldState.AngularAccelerationX != state.AngularAccelerationX)
				DiAxisTable.Rows[0][4] = state.AngularAccelerationX;
			if (oldState.ForceX != state.ForceX)
				DiAxisTable.Rows[0][5] = state.ForceX;
			if (oldState.TorqueX != state.TorqueX)
				DiAxisTable.Rows[0][6] = state.TorqueX;
			if (oldState.VelocityX != state.VelocityX)
				DiAxisTable.Rows[0][7] = state.VelocityX;
			if (oldState.AngularVelocityX != state.AngularVelocityX)
				DiAxisTable.Rows[0][8] = state.AngularVelocityX;
			// Y-axis.
			if (oldState.Y != state.Y)
				DiAxisTable.Rows[0][1] = state.Y;
			if (oldState.RotationY != state.RotationY)
				DiAxisTable.Rows[0][2] = state.RotationY;
			if (oldState.AccelerationY != state.AccelerationY)
				DiAxisTable.Rows[0][3] = state.AccelerationY;
			if (oldState.AngularAccelerationY != state.AngularAccelerationY)
				DiAxisTable.Rows[0][4] = state.AngularAccelerationY;
			if (oldState.ForceY != state.ForceY)
				DiAxisTable.Rows[0][5] = state.ForceY;
			if (oldState.TorqueY != state.TorqueY)
				DiAxisTable.Rows[0][6] = state.TorqueY;
			if (oldState.VelocityY != state.VelocityY)
				DiAxisTable.Rows[0][7] = state.VelocityY;
			if (oldState.AngularVelocityY != state.AngularVelocityY)
				DiAxisTable.Rows[0][8] = state.AngularVelocityY;
			// Z-axis.
			if (oldState.Z != state.Z)
				DiAxisTable.Rows[0][1] = state.Z;
			if (oldState.RotationZ != state.RotationZ)
				DiAxisTable.Rows[0][2] = state.RotationZ;
			if (oldState.AccelerationZ != state.AccelerationZ)
				DiAxisTable.Rows[0][3] = state.AccelerationZ;
			if (oldState.AngularAccelerationZ != state.AngularAccelerationZ)
				DiAxisTable.Rows[0][4] = state.AngularAccelerationZ;
			if (oldState.ForceZ != state.ForceZ)
				DiAxisTable.Rows[0][5] = state.ForceZ;
			if (oldState.TorqueZ != state.TorqueZ)
				DiAxisTable.Rows[0][6] = state.TorqueZ;
			if (oldState.VelocityZ != state.VelocityZ)
				DiAxisTable.Rows[0][7] = state.VelocityZ;
			if (oldState.AngularVelocityZ != state.AngularVelocityZ)
				DiAxisTable.Rows[0][8] = state.AngularVelocityZ;
		}

		#endregion

		#region Sliders Table

		void CreateSlidersTable()
		{
			DiSlidersTable = new DataTable();
			// http://msdn.microsoft.com/en-us/library/windows/desktop/bb151904%28v=vs.85%29.aspx
			DiSlidersTable.Columns.Add("Slider", typeof(string));
			DiSlidersTable.Columns.Add("M", typeof(int));
			DiSlidersTable.Columns.Add("A", typeof(int)); // Acceleration
			DiSlidersTable.Columns.Add("F", typeof(int)); // Force
			DiSlidersTable.Columns.Add("V", typeof(int)); // Velocity
			DiSlidersTable.Rows.Add(DiSlidersTable.NewRow()); // X
			DiSlidersTable.Rows.Add(DiSlidersTable.NewRow()); // Y
			DiSlidersTable.Rows[0][0] = "0";
			DiSlidersTable.Rows[1][0] = "1";
			DiSlidersDataGridView.DataSource = DiSlidersTable;
		}


		void UpdateSlidersTable(JoystickState state)
		{
			for (int i = 0; i <= 1; i++)
			{
				if (oldState.Sliders[i] != state.Sliders[i])
					DiSlidersTable.Rows[i][1] = state.Sliders[i];
				if (oldState.AccelerationSliders[i] != state.AccelerationSliders[i])
					DiSlidersTable.Rows[i][2] = state.AccelerationSliders[i];
				if (oldState.ForceSliders[i] != state.ForceSliders[i])
					DiSlidersTable.Rows[i][3] = state.ForceSliders[i];
				if (oldState.VelocitySliders[i] != state.VelocitySliders[i])
					DiSlidersTable.Rows[i][4] = state.VelocitySliders[i];
			}
		}

		#endregion

		#region POVs Table

		void CreatePovsTable()
		{
			DiPovsTable = new DataTable();
			// http://msdn.microsoft.com/en-us/library/windows/desktop/bb151904%28v=vs.85%29.aspx
			DiPovsTable.Columns.Add("POV", typeof(string));
			DiPovsTable.Columns.Add("M", typeof(int));
			DiPovsTable.Columns.Add("A", typeof(int));
			DiPovsTable.Rows.Add(DiPovsTable.NewRow());
			DiPovsTable.Rows.Add(DiPovsTable.NewRow());
			DiPovsTable.Rows[0][0] = "0";
			DiPovsTable.Rows[1][0] = "1";
			DiPovsDataGridView.DataSource = DiPovsTable;
		}


		void UpdatePovsTable(JoystickState state)
		{
			if (oldState.PointOfViewControllers[0] != state.PointOfViewControllers[0])
				DiPovsTable.Rows[0][1] = state.PointOfViewControllers[0];
			if (oldState.PointOfViewControllers[1] != state.PointOfViewControllers[1])
				DiPovsTable.Rows[0][2] = state.PointOfViewControllers[1];
			if (oldState.PointOfViewControllers[2] != state.PointOfViewControllers[2])
				DiPovsTable.Rows[1][1] = state.PointOfViewControllers[2];
			if (oldState.PointOfViewControllers[3] != state.PointOfViewControllers[3])
				DiPovsTable.Rows[1][2] = state.PointOfViewControllers[3];
		}

		#endregion


		void ShowDeviceInfo(UserDevice ud)
		{
			if (ud == null)
			{
				// clean everything here.
				AppHelper.SetText(DiCapFfStateTextBox, "");
				AppHelper.SetText(DiCapButtonsTextBox, "");
				AppHelper.SetText(DiCapPovsTextBox, "");
				AppHelper.SetText(ActuatorsTextBox, "");
				AppHelper.SetText(DiCapAxesTextBox, "");
				AppHelper.SetText(DiSlidersTextBox, "");
				AppHelper.SetText(DeviceVidTextBox, "");
				AppHelper.SetText(DevicePidTextBox, "");
				AppHelper.SetText(DeviceRevTextBox, "");
				AppHelper.SetText(DeviceProductNameTextBox, "");
				AppHelper.SetText(DeviceVendorNameTextBox, "");
				AppHelper.SetText(DeviceProductGuidTextBox, "");
				AppHelper.SetText(DeviceInstanceGuidTextBox, "");
				AppHelper.SetText(DeviceTypeTextBox, "");
				if (DiEffectsTable.Rows.Count > 0) DiEffectsTable.Rows.Clear();
				return;
			}
			// This must be done for the first time device is connected in order to retrieve 
			// Force feedback information.
			// XInput must be unloaded in case it tries to lock the device exclusivly.
			if (DiEffectsTable.Rows.Count > 0)
				DiEffectsTable.Rows.Clear();
			var effects = ud.DeviceEffects;
			if (effects != null)
			{
				foreach (var eff in ud.DeviceEffects)
				{
					DiEffectsTable.Rows.Add(
						eff.Name,
						eff.StaticParameters.ToString(),
						eff.DynamicParameters.ToString()
					);
				}
			}
			var forceFeedbackState = ((DeviceFlags)ud.CapFlags).HasFlag(DeviceFlags.ForceFeedback) ? "YES" : "NO";
			AppHelper.SetText(DiCapFfStateTextBox, forceFeedbackState);
			AppHelper.SetText(DiCapButtonsTextBox, ud.CapButtonCount.ToString());
			AppHelper.SetText(DiCapPovsTextBox, ud.CapPovCount.ToString());
			var objects = ud.DeviceObjects;
			DiObjectsDataGridView.DataSource = objects;
			if (objects != null)
			{
				var actuators = objects.Where(x => x.Flags.HasFlag(DeviceObjectTypeFlags.ForceFeedbackActuator));
				AppHelper.SetText(ActuatorsTextBox, actuators.Count().ToString());
				var slidersCount = objects.Where(x => x.Type.Equals(SharpDX.DirectInput.ObjectGuid.Slider)).Count();
				// https://msdn.microsoft.com/en-us/library/windows/desktop/microsoft.directx_sdk.reference.dijoystate2(v=vs.85).aspx
				AppHelper.SetText(DiCapAxesTextBox, (ud.CapAxeCount - slidersCount).ToString());
				AppHelper.SetText(DiSlidersTextBox, slidersCount.ToString());
			}
			// Update PID and VID always so they wont be overwritten by load settings.
			short vid = BitConverter.ToInt16(ud.ProductGuid.ToByteArray(), 0);
			short pid = BitConverter.ToInt16(ud.ProductGuid.ToByteArray(), 2);
			AppHelper.SetText(DeviceVidTextBox, "0x{0:X4}", vid);
			AppHelper.SetText(DevicePidTextBox, "0x{0:X4}", pid);
			AppHelper.SetText(DeviceProductNameTextBox, ud.ProductName);
			AppHelper.SetText(DeviceVendorNameTextBox, "{0}", ud.DevManufacturer);
			AppHelper.SetText(DeviceRevTextBox, "0x{0:X4}", ud.DevRevision);
			AppHelper.SetText(DeviceProductGuidTextBox, ud.ProductGuid.ToString());
			AppHelper.SetText(DeviceInstanceGuidTextBox, ud.InstanceGuid.ToString());
			AppHelper.SetText(DeviceTypeTextBox, ((SharpDX.DirectInput.DeviceType)ud.CapType).ToString());
		}

		JoystickState oldState;
		JoystickState emptyState;

		public int[] Axis = new int[6];

		/// <summary>
		/// Update DirectInput control from DirectInput device.
		/// </summary>
		/// <param name="device">DirectInput device.</param>
		/// <returns>List of buttons/DPad pressed, axis/sliders turned.</returns>
		void ShowDirectInputState(JoystickState state)
		{
			var newState = state ?? emptyState;
			if (newState.Equals(oldState)) return;

			// Fill axis.
			Axis[0] = newState.X;
			Axis[1] = newState.Y;
			Axis[2] = newState.Z;
			Axis[3] = newState.RotationX;
			Axis[4] = newState.RotationY;
			Axis[5] = newState.RotationZ;

			UpdateAxisTable(newState);
			UpdateSlidersTable(newState);
			UpdatePovsTable(newState);

			oldState = newState;

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

			bool[] buttons = newState.Buttons;
			var buttonsText = "";
			if (buttons != null)
			{
				var ids = new List<string>();
				for (int i = 0; i < buttons.Length; i++)
					if (buttons[i])
						ids.Add(i.ToString("00"));
				buttonsText = string.Join(" ", ids);
			}
			AppHelper.SetText(DiButtonsTextBox, buttonsText);

			// Point of view buttons
			int[] dPad = newState.PointOfViewControllers;
			//DiPovTextBox.Text = "";
			//if (dPad != null)
			//{
			//	for (int i = 0; i < dPad.Length; i++)
			//	{
			//		v = dPad[i];
			//		if (DiPovTextBox.Text.Length > 0) DiPovTextBox.Text += " ";
			//		if (v != -1)
			//		{
			//			DiPovTextBox.Text += "[" + i + "," + v.ToString() + "]";
			//			//if ((DPadEnum)v == DPadEnum.Up) actions.Add(string.Format("DPad {0} {1}", i + 1, DPadEnum.Up.ToString()));
			//			//if ((DPadEnum)v == DPadEnum.Right) actions.Add(string.Format("DPad {0} {1}", i + 1, DPadEnum.Right.ToString()));
			//			//if ((DPadEnum)v == DPadEnum.Down) actions.Add(string.Format("DPad {0} {1}", i + 1, DPadEnum.Down.ToString()));
			//			//if ((DPadEnum)v == DPadEnum.Left) actions.Add(string.Format("DPad {0} {1}", i + 1, DPadEnum.Left.ToString()));
			//		}
			//	}
			//}
			//return actions;
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

		Guid _DeviceInstanceGuid;
		bool isWheel = false;

		public void UpdateFrom(UserDevice ud)
		{
			var instanceGuid = ud == null ? Guid.Empty : ud.InstanceGuid;
			bool deviceChanged = false;
			if (ud != null)
			{
				deviceChanged = ud.DeviceChanged;
				ud.DeviceChanged = false;
			}
			// If this is different device then...
			if (!instanceGuid.Equals(_DeviceInstanceGuid))
				deviceChanged = true;
			// If device information changed.
			if (deviceChanged)
			{
				ShowDeviceInfo(ud);
				_DeviceInstanceGuid = instanceGuid;
				isWheel = ud == null
					? false : ud.CapType == (int)SharpDX.DirectInput.DeviceType.Driving;
			}
			var state = ud == null ? null : ud.JoState;
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
			var maxTypeName = objects.Max(x => x.TypeName.Length);
			var maxName = objects.Max(x => x.Name.Length);
			var maxFlags = objects.Max(x => x.Flags.ToString().Length);
			var maxAspectName = objects.Max(x => x.AspectName.Length);
			var maxOffsetName = objects.Max(x => x.OffsetName.ToString().Length);

			var names = new string[] { "Offset", "Type", "Aspect", "Flags", "Instance", "Name" };
			var sizes = new int[] { "Offset".Length, -maxTypeName, -maxAspectName, -maxFlags, "Instance".Length, -maxName };
			// Create format line.
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
				sb.AppendFormat(format, o.Offset, o.TypeName, o.AspectName, o.Flags, o.Instance, o.Name);
				sb.AppendLine();
			}
			Clipboard.SetDataObject(sb.ToString());
		}
	}

}
