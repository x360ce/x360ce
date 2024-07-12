using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Data;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for DirectInputUserControl.xaml
	/// </summary>
	public partial class PadItem_DInputControl : UserControl
	{
		public PadItem_DInputControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			if (ControlsHelper.IsDesignMode(this))
				return;
			InitDirectInputTab();
		}

		ObservableCollection<EffectsRow> DiEffectsTable { get; set; } = new ObservableCollection<EffectsRow>();
		ObservableCollection<ButtonsRow> DiButtonsTable { get; set; } = new ObservableCollection<ButtonsRow>();
		ObservableCollection<AxisRow> DiAxisTable { get; set; } = new ObservableCollection<AxisRow>();
		ObservableCollection<SliderRow> DiSlidersTable { get; set; } = new ObservableCollection<SliderRow>();
		ObservableCollection<POVsRow> DiPOVsTable { get; set; } = new ObservableCollection<POVsRow>();

		void InitDirectInputTab()
		{
			// http://msdn.microsoft.com/en-us/library/windows/desktop/bb151904%28v=vs.85%29.aspx
			// Create Effects Table.
			DiEffectsDataGridView.AutoGenerateColumns = false;
			DiEffectsDataGridView.ItemsSource = DiEffectsTable;
			// Create Buttons Table.
			DiButtonsTable.Add(new ButtonsRow());
			DiButtonsDataGridView.AutoGenerateColumns = false;
			DiButtonsDataGridView.ItemsSource = DiButtonsTable;
			// Create Axis Table.
			DiAxisTable.Add(new AxisRow() { Axis = "X" });
			DiAxisTable.Add(new AxisRow() { Axis = "Y" });
			DiAxisTable.Add(new AxisRow() { Axis = "Z" });
			DiAxisDataGridView.AutoGenerateColumns = false;
			DiAxisDataGridView.ItemsSource = DiAxisTable;
			// Create Sliders Table.
			DiSlidersTable.Add(new SliderRow() { Slider = "0" });
			DiSlidersTable.Add(new SliderRow() { Slider = "1" });
			DiSlidersDataGridView.AutoGenerateColumns = false;
			DiSlidersDataGridView.ItemsSource = DiSlidersTable;
			// Create POVs Table.
			DiPOVsTable.Add(new POVsRow() { POV = "0" });
			DiPOVsTable.Add(new POVsRow() { POV = "1" });
			DiPOVsDataGridView.AutoGenerateColumns = false;
			DiPOVsDataGridView.ItemsSource = DiPOVsTable;
		}

		#region ■ Effects Table

		public class EffectsRow : BindableItem
		{
			public string Effect { get => _Effect; set => SetValue(ref _Effect, value); }
			private string _Effect;
			public string StaticParameters { get => _Parameters; set => SetValue(ref _Parameters, value); }
			private string _Parameters;
			public string DynamicParameters { get => _DynamicParameters; set => SetValue(ref _DynamicParameters, value); }
			private string _DynamicParameters;
		}

		#endregion

		#region ■ Buttons Table

		public class ButtonsRow : BindableItem
		{
			public string M { get => _M; set => SetValue(ref _M, value); }
			private string _M;
		}

		void UpdateButtonsTable(bool[] buttons)
		{
			var buttonsText = "";
			var id = new List<int>();
			if (buttons != null)
			{
				var ids = new List<string>();
				for (int i = 0; i < buttons.Length; i++)
					if (buttons[i])
					{
						ids.Add(i.ToString("00"));
						id.Add(i);
					}
				buttonsText = string.Join(" ", ids);
			}
			DiButtonsTable[0].M = buttonsText;
		}

		#endregion

		#region ■ Axis Table

		public class AxisRow : BindableItem
		{
			/// <summary>Axis name.</summary>
			public string Axis { get => _Axis; set => SetValue(ref _Axis, value); }
			private string _Axis;

			/// <summary></summary>
			public int M { get => _M; set => SetValue(ref _M, value); }
			private int _M;

			/// <summary>Rotation</summary>
			public int R { get => _R; set => SetValue(ref _R, value); }
			private int _R;

			/// <summary>Acceleration</summary>
			public int A { get => _A; set => SetValue(ref _A, value); }
			private int _A;

			/// <summary>Angular Acceleration</summary>
			public int AR { get => _AR; set => SetValue(ref _AR, value); }
			private int _AR;

			/// <summary>Force</summary>
			public int F { get => _F; set => SetValue(ref _F, value); }
			private int _F;

			/// <summary>Torque</summary>
			public int FR { get => _FR; set => SetValue(ref _FR, value); }
			private int _FR;

			/// <summary>Velocity</summary>
			public int V { get => _V; set => SetValue(ref _V, value); }
			private int _V;

			/// <summary>AngularVelocity</summary>
			public int VR { get => _VR; set => SetValue(ref _VR, value); }
			private int _VR;
		}

		void UpdateAxisTable(MouseState state)
		{
			// X-axis.
			var x = DiAxisTable[0];
			x.M = state.X;
			// Y-axis.
			var y = DiAxisTable[1];
			y.M = state.Y;
			// Z-axis.
			var z = DiAxisTable[2];
			z.M = state.Z;
		}

		void UpdateAxisTable(JoystickState state)
		{
			// X-axis.
			var x = DiAxisTable[0];
			x.M = state.X;
			x.R = state.RotationX;
			x.A = state.AccelerationX;
			x.AR = state.AngularAccelerationX;
			x.F = state.ForceX;
			x.FR = state.TorqueX;
			x.V = state.VelocityX;
			x.VR = state.AngularVelocityX;
			// Y-axis.
			var y = DiAxisTable[1];
			y.M = state.Y;
			y.R = state.RotationY;
			y.A = state.AccelerationY;
			y.AR = state.AngularAccelerationY;
			y.F = state.ForceY;
			y.FR = state.TorqueY;
			y.V = state.VelocityY;
			y.VR = state.AngularVelocityY;
			// Z-axis.
			var z = DiAxisTable[2];
			z.M = state.Z;
			z.R = state.RotationZ;
			z.A = state.AccelerationZ;
			z.AR = state.AngularAccelerationZ;
			z.F = state.ForceZ;
			z.FR = state.TorqueZ;
			z.V = state.VelocityZ;
			z.VR = state.AngularVelocityZ;
		}

		#endregion

		#region ■ Sliders Table

		public class SliderRow : BindableItem
		{
			/// <summary>Name.</summary>
			public string Slider { get => _Slider; set => SetValue(ref _Slider, value); }
			private string _Slider;

			/// <summary></summary>
			public int M { get => _M; set => SetValue(ref _M, value); }
			private int _M;

			/// <summary>Acceleration</summary>
			public int A { get => _A; set => SetValue(ref _A, value); }
			private int _A;

			/// <summary>Force</summary>
			public int F { get => _F; set => SetValue(ref _F, value); }
			private int _F;

			/// <summary>Velocity</summary>
			public int V { get => _V; set => SetValue(ref _V, value); }
			private int _V;
		}

		void UpdateSlidersTable(JoystickState state)
		{
			for (int i = 0; i <= 1; i++)
			{
				var s = DiSlidersTable[i];
				s.M = state.Sliders[i];
				s.A = state.AccelerationSliders[i];
				s.F = state.ForceSliders[i];
				s.V = state.VelocitySliders[i];
			}
		}

		#endregion

		#region ■ POVs Table

		public class POVsRow : BindableItem
		{
			/// <summary>Name.</summary>
			public string POV { get => _POV; set => SetValue(ref _POV, value); }
			private string _POV;

			/// <summary></summary>
			public int M { get => _M; set => SetValue(ref _M, value); }
			private int _M;

			/// <summary>Acceleration</summary>
			public int A { get => _A; set => SetValue(ref _A, value); }
			private int _A;
		}

		void UpdatePovsTable(JoystickState state)
		{
			var r0 = DiPOVsTable[0];
			r0.M = state.PointOfViewControllers[0];
			r0.A = state.PointOfViewControllers[1];
			var r1 = DiPOVsTable[1];
			r1.M = state.PointOfViewControllers[2];
			r1.A = state.PointOfViewControllers[3];
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
				if (DiEffectsTable.Count > 0)
					DiEffectsTable.Clear();
				return;
			}
			// This must be done for the first time device is connected in order to retrieve 
			// Force feedback information.
			// XInput must be unloaded in case it tries to lock the device exclusively.
			if (DiEffectsTable.Count > 0)
				DiEffectsTable.Clear();
			var effects = ud.DeviceEffects;
			if (effects != null)
			{
				foreach (var eff in ud.DeviceEffects)
				{
					var er = new EffectsRow()
					{
						Effect = eff.Name,
						StaticParameters = eff.StaticParameters.ToString(),
						DynamicParameters = eff.DynamicParameters.ToString()
					};
					DiEffectsTable.Add(er);
				}
			}
			var forceFeedbackState = ((DeviceFlags)ud.CapFlags).HasFlag(DeviceFlags.ForceFeedback) ? "YES" : "NO";
			ControlsHelper.SetText(DiCapFfStateTextBox, forceFeedbackState);
			ControlsHelper.SetText(DiCapButtonsTextBox, ud.CapButtonCount.ToString());
			ControlsHelper.SetText(DiCapPovsTextBox, ud.CapPovCount.ToString());
			var objects = ud.DeviceObjects;
			ObjectsInformationAsText = GetDiObjectsInformationAsText(objects);
			DiObjectsDataGridView.AutoGenerateColumns = false;
			DiObjectsDataGridView.ItemsSource = objects;
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
			var maxIndex = DiAxisDataGridView.Columns.Count - 1;
			// Hide last 4 columns if device is mouse, in order to make more space for axis values.
			for (int i = maxIndex - 3; i <= maxIndex; i++)
				DiAxisDataGridView.Columns[i].Visibility = ud.IsMouse
					? System.Windows.Visibility.Collapsed
					: System.Windows.Visibility.Visible;
		}

		MouseState oldMState;
		KeyboardState oldKState;
		JoystickState oldJState;

		object updateLock = new object();

		void ShowDirectInputState(object state)
		{
			lock (updateLock)
			{
				if (state is MouseState mState)
					ShowDirectInputState(mState);
				if (state is KeyboardState kState)
					ShowDirectInputState(kState);
				if (state is JoystickState jState)
					ShowDirectInputState(jState);
			}
		}

		/// <summary>
		/// Update UI controls from DirectInput state.
		/// </summary>
		void ShowDirectInputState(MouseState state)
		{
			if (state == null)
				return;
			if (state == oldMState)
				return;
			UpdateButtonsTable(state.Buttons);
			UpdateAxisTable(state);
			ControlsHelper.SetVisible(DiAxisDataGridView, true);
			ControlsHelper.SetVisible(DiSlidersDataGridView, false);
			ControlsHelper.SetVisible(DiPOVsDataGridView, false);
			oldMState = state;
		}

		/// <summary>
		/// Update UI controls from DirectInput state.
		/// </summary>
		void ShowDirectInputState(KeyboardState state)
		{
			if (state == null)
				return;
			if (state == oldKState)
				return;
			var cds = new CustomDiState(state);
			UpdateButtonsTable(cds.Buttons);
			ControlsHelper.SetVisible(DiAxisDataGridView, false);
			ControlsHelper.SetVisible(DiSlidersDataGridView, false);
			ControlsHelper.SetVisible(DiPOVsDataGridView, false);
			oldKState = state;
		}

		/// <summary>
		/// Update UI controls from DirectInput state.
		/// </summary>
		void ShowDirectInputState(JoystickState state)
		{
			if (state == null)
				return;
			if (state == oldJState)
				return;
			UpdateButtonsTable(state.Buttons);
			UpdateAxisTable(state);
			UpdateSlidersTable(state);
			UpdatePovsTable(state);
			ControlsHelper.SetVisible(DiAxisDataGridView, true);
			ControlsHelper.SetVisible(DiSlidersDataGridView, true);
			ControlsHelper.SetVisible(DiPOVsDataGridView, true);
			oldJState = state;
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
			ShowDirectInputState(ud?.DeviceState);
		}

		string ObjectsInformationAsText;

		string GetDiObjectsInformationAsText(DeviceObjectItem[] objects)
		{
			if (objects == null)
				return null;
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
			//Clipboard.SetDataObject(sb.ToString());
			return sb.ToString();
		}
		/*

		#region ■ Copy Data

		private void CopyWithHeadersMenuItem_Click(object sender, EventArgs e)
		{
			var menuItem = sender as MenuItem;
			var toolItem = sender as ToolStripItem;
			var sourceControl = menuItem == null
				? (toolItem.Owner as ContextMenuStrip).SourceControl
				: menuItem.GetContextMenu().SourceControl;
			// Get the control that is displaying this context menu
			if (sourceControl == DiObjectsDataGridView)
				CopyDiObjectsInformation();
			if (sourceControl == DiEffectsDataGridView)
				CopyDiEffectsInformation();
		}
		#endregion
		*/

		private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowLoad(this))
				return;
		}

		private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowUnload(this))
				return;
			// Moved to MainBodyControl_Unloaded().
		}

		public void ParentWindow_Unloaded()
		{
			DiEffectsDataGridView.ItemsSource = null;
			DiButtonsDataGridView.ItemsSource = null;
			DiAxisDataGridView.ItemsSource = null;
			DiSlidersDataGridView.ItemsSource = null;
			DiPOVsDataGridView.ItemsSource = null;
			DiEffectsTable?.Clear();
			DiButtonsTable?.Clear();
			DiAxisTable?.Clear();
			DiSlidersTable?.Clear();
			DiPOVsTable?.Clear();
			DiEffectsTable = null;
			DiButtonsTable = null;
			DiAxisTable = null;
			DiSlidersTable = null;
			DiPOVsTable = null;
			oldMState = null;
			oldKState = null;
			oldJState = null;
		}
	}

}
