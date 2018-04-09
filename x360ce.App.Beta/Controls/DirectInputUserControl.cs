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
using JocysCom.ClassLibrary.Controls;

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

		DataTable DiButtonsTable;
		DataTable DiAxisTable;
		DataTable DiSlidersTable;
		DataTable DiPovsTable;
		DataTable DiEffectsTable;

		void InitDirectInputTab()
		{
			var ToolTip1 = new ToolTip();
			CreateButtonsTable();
			CreateAxisTable();
			CreateSlidersTable();
			CreatePovsTable();
			CreateEffectsTable();
		}

		/// <summary>
		/// Use this event handler to remove selection from DataGridView.
		/// </summary>
		private void DataGridView_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
		{
			((DataGridView)sender).ClearSelection();
		}

		#region Effects Table

		void CreateEffectsTable()
		{
			DiEffectsTable = new DataTable();
			DiEffectsTable.Columns.Add("Effect", typeof(string));
			DiEffectsTable.Columns.Add("Parameters", typeof(string));
			DiEffectsTable.Columns.Add("DynamicParameters", typeof(string));
			DiEffectsDataGridView.DataBindingComplete += DataGridView_DataBindingComplete;
			DiEffectsDataGridView.DataSource = DiEffectsTable;
		}

		#endregion

		#region Buttons Table

		void CreateButtonsTable()
		{
			DiButtonsTable = new DataTable();
			// http://msdn.microsoft.com/en-us/library/windows/desktop/bb151904%28v=vs.85%29.aspx
			DiButtonsTable.Columns.Add("M", typeof(string));
			DiButtonsTable.Rows.Add(DiButtonsTable.NewRow());
			DiButtonsDataGridView.DataBindingComplete += DataGridView_DataBindingComplete;
			DiButtonsDataGridView.DataSource = DiButtonsTable;
		}

		string oldButtons;

		void UpdateButtonsTable(JoystickState state)
		{
			bool[] buttons = state.Buttons;
			var buttonsText = "";
			if (buttons != null)
			{
				var ids = new List<string>();
				for (int i = 0; i < buttons.Length; i++)
					if (buttons[i])
						ids.Add(i.ToString("00"));
				buttonsText = string.Join(" ", ids);
			}
			if (oldButtons != buttonsText)
			{
				oldButtons = buttonsText;
				DiButtonsTable.Rows[0][0] = buttonsText;
			}
		}

		#endregion

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
			DiAxisDataGridView.DataBindingComplete += DataGridView_DataBindingComplete;
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
				DiAxisTable.Rows[1][1] = state.Y;
			if (oldState.RotationY != state.RotationY)
				DiAxisTable.Rows[1][2] = state.RotationY;
			if (oldState.AccelerationY != state.AccelerationY)
				DiAxisTable.Rows[1][3] = state.AccelerationY;
			if (oldState.AngularAccelerationY != state.AngularAccelerationY)
				DiAxisTable.Rows[1][4] = state.AngularAccelerationY;
			if (oldState.ForceY != state.ForceY)
				DiAxisTable.Rows[1][5] = state.ForceY;
			if (oldState.TorqueY != state.TorqueY)
				DiAxisTable.Rows[1][6] = state.TorqueY;
			if (oldState.VelocityY != state.VelocityY)
				DiAxisTable.Rows[1][7] = state.VelocityY;
			if (oldState.AngularVelocityY != state.AngularVelocityY)
				DiAxisTable.Rows[1][8] = state.AngularVelocityY;
			// Z-axis.
			if (oldState.Z != state.Z)
				DiAxisTable.Rows[2][1] = state.Z;
			if (oldState.RotationZ != state.RotationZ)
				DiAxisTable.Rows[2][2] = state.RotationZ;
			if (oldState.AccelerationZ != state.AccelerationZ)
				DiAxisTable.Rows[2][3] = state.AccelerationZ;
			if (oldState.AngularAccelerationZ != state.AngularAccelerationZ)
				DiAxisTable.Rows[2][4] = state.AngularAccelerationZ;
			if (oldState.ForceZ != state.ForceZ)
				DiAxisTable.Rows[2][5] = state.ForceZ;
			if (oldState.TorqueZ != state.TorqueZ)
				DiAxisTable.Rows[2][6] = state.TorqueZ;
			if (oldState.VelocityZ != state.VelocityZ)
				DiAxisTable.Rows[2][7] = state.VelocityZ;
			if (oldState.AngularVelocityZ != state.AngularVelocityZ)
				DiAxisTable.Rows[2][8] = state.AngularVelocityZ;
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
			DiSlidersDataGridView.DataBindingComplete += DataGridView_DataBindingComplete;
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
			DiPovsDataGridView.DataBindingComplete += DataGridView_DataBindingComplete;
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
				ControlsHelper.SetText(DiCapFfStateTextBox, "");
				ControlsHelper.SetText(DiCapButtonsTextBox, "");
				ControlsHelper.SetText(DiCapPovsTextBox, "");
				ControlsHelper.SetText(ActuatorsTextBox, "");
				ControlsHelper.SetText(DiCapAxesTextBox, "");
				ControlsHelper.SetText(DiSlidersTextBox, "");
				ControlsHelper.SetText(DeviceVidTextBox, "");
				ControlsHelper.SetText(DevicePidTextBox, "");
				ControlsHelper.SetText(DeviceRevTextBox, "");
				ControlsHelper.SetText(DeviceProductNameTextBox, "");
				ControlsHelper.SetText(DeviceVendorNameTextBox, "");
				ControlsHelper.SetText(DeviceProductGuidTextBox, "");
				ControlsHelper.SetText(DeviceInstanceGuidTextBox, "");
				ControlsHelper.SetText(DeviceTypeTextBox, "");
				if (DiEffectsTable.Rows.Count > 0) DiEffectsTable.Rows.Clear();
				return;
			}
			// This must be done for the first time device is connected in order to retrieve 
			// Force feedback information.
			// XInput must be unloaded in case it tries to lock the device exclusively.
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
			ControlsHelper.SetText(DiCapFfStateTextBox, forceFeedbackState);
			ControlsHelper.SetText(DiCapButtonsTextBox, ud.CapButtonCount.ToString());
			ControlsHelper.SetText(DiCapPovsTextBox, ud.CapPovCount.ToString());
			var objects = ud.DeviceObjects;

			DiObjectsDataGridView.DataBindingComplete += DataGridView_DataBindingComplete;
			DiObjectsDataGridView.DataSource = objects;
			if (objects != null)
			{
				var actuators = objects.Where(x => x.Flags.HasFlag(DeviceObjectTypeFlags.ForceFeedbackActuator));
				ControlsHelper.SetText(ActuatorsTextBox, actuators.Count().ToString());
				var slidersCount = objects.Where(x => x.Type.Equals(SharpDX.DirectInput.ObjectGuid.Slider)).Count();
				// https://msdn.microsoft.com/en-us/library/windows/desktop/microsoft.directx_sdk.reference.dijoystate2(v=vs.85).aspx
				ControlsHelper.SetText(DiCapAxesTextBox, (ud.CapAxeCount - slidersCount).ToString());
				ControlsHelper.SetText(DiSlidersTextBox, slidersCount.ToString());
			}
			// Update PID and VID always so they wont be overwritten by load settings.
			short vid = BitConverter.ToInt16(ud.ProductGuid.ToByteArray(), 0);
			short pid = BitConverter.ToInt16(ud.ProductGuid.ToByteArray(), 2);
			ControlsHelper.SetText(DeviceVidTextBox, "0x{0:X4}", vid);
			ControlsHelper.SetText(DevicePidTextBox, "0x{0:X4}", pid);
			ControlsHelper.SetText(DeviceProductNameTextBox, ud.ProductName);
			ControlsHelper.SetText(DeviceVendorNameTextBox, "{0}", ud.HidManufacturer);
			ControlsHelper.SetText(DeviceRevTextBox, "0x{0:X4}", ud.DevRevision);
			ControlsHelper.SetText(DeviceProductGuidTextBox, ud.ProductGuid.ToString());
			ControlsHelper.SetText(DeviceInstanceGuidTextBox, ud.InstanceGuid.ToString());
			ControlsHelper.SetText(DeviceTypeTextBox, ((SharpDX.DirectInput.DeviceType)ud.CapType).ToString());
		}

		JoystickState oldState;
		JoystickState emptyState;

		/// <summary>
		/// Update DirectInput control from DirectInput device.
		/// </summary>
		/// <param name="device">DirectInput device.</param>
		/// <returns>List of buttons/DPad pressed, axis/sliders turned.</returns>
		void ShowDirectInputState(JoystickState state)
		{
			var newState = state ?? emptyState;
			if (newState.Equals(oldState)) return;

			UpdateButtonsTable(newState);
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

			//bool[] buttons = newState.Buttons;
			//var buttonsText = "";
			//if (buttons != null)
			//{
			//	var ids = new List<string>();
			//	for (int i = 0; i < buttons.Length; i++)
			//		if (buttons[i])
			//			ids.Add(i.ToString("00"));
			//	buttonsText = string.Join(" ", ids);
			//}
			//ControlsHelper.SetText(DiButtonsTextBox, buttonsText);

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
