using JocysCom.ClassLibrary;
using JocysCom.ClassLibrary.Controls;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for PadControl.xaml
	/// </summary>
	public partial class PadControl : UserControl, IPadControl
	{
		public PadControl()
		{
			CurrentPadSetting = new PadSetting();
			InitHelper.InitTimer(this, InitializeComponent);
		}

		public void InitControls(MapTo mappedTo)
		{
			if (ControlsHelper.IsDesignMode(this))
				return;
			// Add controls which must be notified on setting selection change.
			Global.UpdateControlFromStates += Global_UpdateControlFromStates;
			// Hide for this version.
			//PadItemPanel.PadTabControl.Items.Remove(PadItemPanel.XInputTabPage);
			//PadItemPanel.XInputTabPage.Content = null;
			//PadTabControl.TabPages.Remove(MacrosTabPage);
			RemapName = PadFootPanel.RemapAllButton.Content as string;
			PadFootPanel.MapNameComboBox.SelectionChanged += GeneralPanel.MapNameComboBox_SelectionChanged;
			PadFootPanel.RemapAllButton.Click += RemapAllButton_Click;
			MappedTo = mappedTo;
			_Imager = new PadControlImager();
			//_Imager.Top = XboxImage.TopPictureImage;
			//_Imager.Front = XboxImage.FrontPictureImage;
			_Imager.LeftThumbAxisStatus = XboxImage.LeftThumbAxisBorder;
			_Imager.RightThumbAxisStatus = XboxImage.RightThumbAxisBorder;
			_Imager.LeftTriggerAxisStatus = XboxImage.LeftTriggerAxisBorder;
			_Imager.RightTriggerAxisStatus = XboxImage.RightTriggerAxisBorder;
			_Imager.ImageControl = XboxImage;
			XboxImage.InitializeImages(imageInfos, _Imager, mappedTo);
			XboxImage.StartRecording = StartRecording;
			XboxImage.StopRecording = StopRecording;
			// Axis to Button DeadZones
			ButtonsPanel.AxisToButtonADeadZonePanel.MonitorComboBoxWpf = GeneralPanel.ButtonATextBox;
			ButtonsPanel.AxisToButtonBDeadZonePanel.MonitorComboBoxWpf = GeneralPanel.ButtonBTextBox;
			ButtonsPanel.AxisToButtonXDeadZonePanel.MonitorComboBoxWpf = GeneralPanel.ButtonXTextBox;
			ButtonsPanel.AxisToButtonYDeadZonePanel.MonitorComboBoxWpf = GeneralPanel.ButtonYTextBox;
			ButtonsPanel.AxisToButtonStartDeadZonePanel.MonitorComboBoxWpf = GeneralPanel.ButtonStartTextBox;
			ButtonsPanel.AxisToButtonBackDeadZonePanel.MonitorComboBoxWpf = GeneralPanel.ButtonBackTextBox;
			ButtonsPanel.AxisToLeftShoulderDeadZonePanel.MonitorComboBoxWpf = GeneralPanel.LeftShoulderTextBox;
			ButtonsPanel.AxisToLeftThumbButtonDeadZonePanel.MonitorComboBoxWpf = GeneralPanel.LeftThumbButtonTextBox;
			ButtonsPanel.AxisToRightShoulderDeadZonePanel.MonitorComboBoxWpf = GeneralPanel.RightShoulderTextBox;
			ButtonsPanel.AxisToRightThumbButtonDeadZonePanel.MonitorComboBoxWpf = GeneralPanel.RightThumbButtonTextBox;
			ButtonsPanel.AxisToDPadDownDeadZonePanel.MonitorComboBoxWpf = GeneralPanel.DPadDownTextBox;
			ButtonsPanel.AxisToDPadLeftDeadZonePanel.MonitorComboBoxWpf = GeneralPanel.DPadLeftTextBox;
			ButtonsPanel.AxisToDPadRightDeadZonePanel.MonitorComboBoxWpf = GeneralPanel.DPadRightTextBox;
			ButtonsPanel.AxisToDPadUpDeadZonePanel.MonitorComboBoxWpf = GeneralPanel.DPadUpTextBox;
			// Monitor setting changes.
			SettingsManager.Current.SettingChanged += Current_SettingChanged;
			PadListPanel.SetBinding(MappedTo);
			PadListPanel.MainDataGrid.SelectionChanged += MainDataGrid_SelectionChanged;
		}

		private void CurrentPadSetting_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName != nameof(PadSetting.PadSettingChecksum))
			{
				var oldChecksum = CurrentPadSetting.PadSettingChecksum;
				var newChecksum = CurrentPadSetting.CleanAndGetCheckSum();
				// If checksum changed.
				if (oldChecksum != newChecksum)
				{
					CurrentPadSetting.PadSettingChecksum = newChecksum;
					System.Diagnostics.Debug.WriteLine($"{MappedTo} PadSettingChecksum: {oldChecksum} => {newChecksum}");
					// Time to save settings by notifying that it changed.
					SettingsManager.Current.SavePadSetting(CurrentUserSetting, CurrentPadSetting);
					//OnSettingChanged?.Invoke(this, new EventArgs<UserSetting>(CurrentUserSetting));
				}
			}
		}

		#region ■ Control Links

		private PadItem_GeneralControl GeneralPanel => PadItemPanel.GeneralPanel;
		private PadItem_AdvancedControl AdvancedPanel => PadItemPanel.AdvancedPanel;
		private PadItem_ButtonsControl ButtonsPanel => PadItemPanel.ButtonsPanel;
		private PadItem_DPadControl DPadPanel => PadItemPanel.DPadPanel;
		private AxisMapControl LeftTriggerPanel => PadItemPanel.LeftTriggerPanel;
		private AxisMapControl RightTriggerPanel => PadItemPanel.RightTriggerPanel;
		private AxisMapControl LeftThumbXPanel => PadItemPanel.LeftThumbXPanel;
		private AxisMapControl LeftThumbYPanel => PadItemPanel.LeftThumbYPanel;
		private AxisMapControl RightThumbXPanel => PadItemPanel.RightThumbXPanel;
		private AxisMapControl RightThumbYPanel => PadItemPanel.RightThumbYPanel;
		private PadItem_ForceFeedbackControl ForceFeedbackPanel => PadItemPanel.ForceFeedbackPanel;
		//private XInputUserControl XInputPanel => PadItemPanel.XInputPanel;

		private PadItem_General_XboxImageControl XboxImage => GeneralPanel.XboxImage;

		#endregion

		private void Global_UpdateControlFromStates(object sender, EventArgs e)
		{
			UpdateControlFromDInput();
			UpdateControlFromXInput();
		}

		private void UpdateControlFromDInput()
		{
			lock (updateFromDirectInputLock)
			{
				var ud = CurrentUserDevice;
				var instanceGuid = Guid.Empty;
				var enable = ud != null;
				if (enable)
					instanceGuid = ud.InstanceGuid;
				ControlsHelper.SetEnabled(PadFootPanel.RemapAllButton, enable && ud.DiState != null);
				PadItemPanel.SetEnabled(enable);
				// If device instance changed then...
				if (!Equals(instanceGuid, _InstanceGuid))
				{
					_InstanceGuid = instanceGuid;
					GeneralPanel.ResetDiMenuStrip(enable ? ud : null);
				}
				// Update direct input form and return actions (pressed Buttons/DPads, turned Axis/Sliders).
				UpdateDirectInputTabPage(ud);
				PadItemPanel.DInputPanel.UpdateFrom(ud);
				if (enable && _Imager.Recorder.Recording)
				{
					// Stop recording if DInput value captured.
					var stopped = _Imager.Recorder.StopRecording(ud.DiState);
					// If value was found and recording stopped then...
					if (stopped)
					{
						// Device not initialized yet.
						if (ud.DiState == null)
							RecordAllMaps.Clear();
						if (RecordAllMaps.Count == 0)
						{
							if (ud.DiState != null)
								XboxImage.SetHelpText(XboxImage.MappingDone);
							else
								XboxImage.HelpTextLabel.Content = "";
                            PadFootPanel.RemapAllButton.Content = RemapName;
							return;
						}
						else
						{
							XboxImage.HelpTextLabel.Content = "";
						}
						// Try to record next available control from the list.
						ControlsHelper.BeginInvoke(() => StartRecording(), 1000);
					}
				}
			}
		}

		void UpdateControlFromXInput()
		{
			var i = (int)MappedTo - 1;
			var useXiStates = SettingsManager.Options.GetXInputStates;
			newState = useXiStates
				? Global.DHelper.LiveXiStates[i]
				: Global.DHelper.CombinedXiStates[i];
			newConnected = useXiStates
				? Global.DHelper.LiveXiConnected[i]
				: Global.DHelper.CombinedXiConencted[i];
			// If device is not connected and was not connected then return.
			if (!newConnected && !oldConnected)
				return;
			// If device disconnected then show disabled images.
			if (!newConnected && oldConnected)
			{
                //_Imager.SetImages(false);
                PadFootPanel.RemapAllButton.IsEnabled = false;
			}
			// If device connected then show enabled images.
			if (newConnected && !oldConnected)
			{
                //_Imager.SetImages(true);
                PadFootPanel.RemapAllButton.IsEnabled = true;
			}
			// Return if controller is not connected.
			if (newConnected)
			{
				//_Imager.DrawController(e, MappedTo);
				// Process all buttons and axis.
				foreach (var ii in imageInfos)
					_Imager.DrawState(ii, newState.Gamepad);
			}
			// Set values.
			ControlsHelper.SetText(GeneralPanel.LeftTextBox, "{0}", newState.Gamepad.LeftTrigger);
			ControlsHelper.SetText(GeneralPanel.RightTextBox, "{0}", newState.Gamepad.RightTrigger);
			ControlsHelper.SetText(GeneralPanel.LeftThumbTextBox, "{0}:{1}", newState.Gamepad.LeftThumbX, newState.Gamepad.LeftThumbY);
			ControlsHelper.SetText(GeneralPanel.RightThumbTextBox, "{0}:{1}", newState.Gamepad.RightThumbX, newState.Gamepad.RightThumbY);
			// Process device.
			var ud = CurrentUserDevice;
			var ps = CurrentPadSetting;
			if (ud?.DiState != null && ps != null)
			{
				// Get current pad setting.
				Map map;
				// LeftThumbX
				var axis = ud.DiState.Axis;
				map = ps.Maps.FirstOrDefault(x => x.Target == TargetType.LeftThumbX);
				if (map != null && map.Index > 0 && map.Index <= axis.Length)
					LeftThumbXPanel.DrawPoint(axis[map.Index - 1], newState.Gamepad.LeftThumbX, map.IsInverted, map.IsHalf);
				// LeftThumbY
				map = ps.Maps.FirstOrDefault(x => x.Target == TargetType.LeftThumbY);
				if (map != null && map.Index > 0 && map.Index <= axis.Length)
					LeftThumbYPanel.DrawPoint(axis[map.Index - 1], newState.Gamepad.LeftThumbY, map.IsInverted, map.IsHalf);
				// RightThumbX
				map = ps.Maps.FirstOrDefault(x => x.Target == TargetType.RightThumbX);
				if (map != null && map.Index > 0 && map.Index <= axis.Length)
					RightThumbXPanel.DrawPoint(axis[map.Index - 1], newState.Gamepad.RightThumbX, map.IsInverted, map.IsHalf);
				// RightThumbY
				map = ps.Maps.FirstOrDefault(x => x.Target == TargetType.RightThumbY);
				if (map != null && map.Index > 0 && map.Index <= axis.Length)
					RightThumbYPanel.DrawPoint(axis[map.Index - 1], newState.Gamepad.RightThumbY, map.IsInverted, map.IsHalf);
				// LeftTrigger
				map = ps.Maps.FirstOrDefault(x => x.Target == TargetType.LeftTrigger);
				if (map != null && map.Index > 0 && map.Index <= axis.Length)
					LeftTriggerPanel.DrawPoint(axis[map.Index - 1], newState.Gamepad.LeftTrigger, map.IsInverted, map.IsHalf);
				// RightTrigger
				map = ps.Maps.FirstOrDefault(x => x.Target == TargetType.RightTrigger);
				if (map != null && map.Index > 0 && map.Index <= axis.Length)
					RightTriggerPanel.DrawPoint(axis[map.Index - 1], newState.Gamepad.RightTrigger, map.IsInverted, map.IsHalf);
			}
			// Update Axis to Button Images.
			if (_AxisToButtonControls == null)
				_AxisToButtonControls = ControlsHelper.GetAll<AxisToButtonControl>(ButtonsPanel.MainGroupBox);
			foreach (var atbPanel in _AxisToButtonControls)
				atbPanel.Refresh(newState);
			// Store old state.
			oldConnected = newConnected;
		}

		private AxisToButtonControl[] _AxisToButtonControls;

		public bool StopRecording()
		{
			RecordAllMaps.Clear();
            PadFootPanel.RemapAllButton.Content = RemapName;
			return _Imager.Recorder.StopRecording();
		}

		void StartRecording(SettingsMapItem map = null)
		{
			if (map == null)
			{
				map = RecordAllMaps.FirstOrDefault();
				if (map == null)
					return;
				RecordAllMaps.Remove(map);
			}
			var cbx = (ComboBox)map.Control;
			/*
				if (_CurrentCbx != cbx)
					_CurrentCbx = cbx;
			*/
			_Imager.Recorder.StartRecording(map);
			var helpText =
				SettingsConverter.ThumbDirections.Contains(map.Code) ||
				SettingsConverter.TriggerButtonCodes.Contains(map.Code)
					? "Move Axis"
					: "Press Button";
			XboxImage.HelpTextLabel.Content = helpText;
            PadFootPanel.RemapAllButton.Content = RemapStopName;
		}

		private void Current_SettingChanged(object sender, SettingChangedEventArgs e)
		{
			if (e.Item == null)
				return;
			// If control is linked to another controller then return.
			if (e.Item.MapTo != MappedTo)
				return;
			// If control is not specified then return.
			if (e.Item.Control == null)
				return;

			//// By default send vibration if force enabled/disabled changed.
			//var send = e.Item.Control == ForceEnableCheckBox;
			//// If force is enabled then...
			//if (ForceEnableCheckBox.Checked)
			//{
			//	// List controls which will affect force feedback test.
			//	var controls = new Control[]
			//	{
			//		ForceTypeComboBox,
			//		ForceOverallTrackBar,
			//		ForceSwapMotorCheckBox,
			//		LeftMotorDirectionComboBox,
			//		LeftMotorPeriodTrackBar,
			//		LeftMotorStrengthTrackBar,
			//		RightMotorDirectionComboBox,
			//		RightMotorPeriodTrackBar,
			//		RightMotorStrengthTrackBar,
			//	};
			//	if (controls.Contains(e.Item.Control))
			//		send = true;
			//}
			//if (send)
			//	SendVibration();
		}

		public void InitPadData()
		{
			PadFootPanel.InitPadData();
		}

		public void InitPadControl()
		{
			var dv = new System.Data.DataView();
			// Show disabled images by default.
			//_Imager.SetImages(false);
			// Add player index to combo boxes
			var playerOptions = new List<KeyValuePair>();
			var playerTypes = (UserIndex[])Enum.GetValues(typeof(UserIndex));
			foreach (var item in playerTypes)
				playerOptions.Add(new KeyValuePair(item.ToString(), ((int)item).ToString()));
			PadListPanel.UpdateFromCurrentGame();
			// Update emulation type.
			var game = SettingsManager.CurrentGame;
			var showAdvanced = game != null && game.EmulationType == (int)EmulationType.Library;
			PadItemPanel.ShowTab(showAdvanced, PadItemPanel.AdvancedTabPage);
		}

		#region ■ Images

		public PadControlImager _Imager;

		List<ImageInfo> imageInfos
		{
			get
			{
				if (_imageInfos == null)
				{
					_imageInfos = new List<ImageInfo>();
					// Configure Image 1.
					AddImageInfo(1, MapCode.LeftTrigger, 63, 27, GeneralPanel.LeftTriggerLabel, GeneralPanel.LeftTriggerTextBox);
					AddImageInfo(1, MapCode.RightTrigger, 193, 27, GeneralPanel.RightTriggerLabel, GeneralPanel.RightTriggerTextBox);
					AddImageInfo(1, MapCode.LeftShoulder, 43, 66, GeneralPanel.LeftShoulderLabel, GeneralPanel.LeftShoulderTextBox, GamepadButtonFlags.LeftShoulder);
					AddImageInfo(1, MapCode.RightShoulder, 213, 66, GeneralPanel.RightShoulderLabel, GeneralPanel.RightShoulderTextBox, GamepadButtonFlags.RightShoulder);
					// Configure Image 2.
					AddImageInfo(2, MapCode.ButtonY, 196, 29, GeneralPanel.ButtonYLabel, GeneralPanel.ButtonYTextBox, GamepadButtonFlags.Y);
					AddImageInfo(2, MapCode.ButtonX, 178, 48, GeneralPanel.ButtonXLabel, GeneralPanel.ButtonXTextBox, GamepadButtonFlags.X);
					AddImageInfo(2, MapCode.ButtonB, 215, 48, GeneralPanel.ButtonBLabel, GeneralPanel.ButtonBTextBox, GamepadButtonFlags.B);
					AddImageInfo(2, MapCode.ButtonA, 196, 66, GeneralPanel.ButtonALabel, GeneralPanel.ButtonATextBox, GamepadButtonFlags.A);
					AddImageInfo(2, MapCode.ButtonGuide, 127, 48, GeneralPanel.ButtonGuideLabel, GeneralPanel.ButtonGuideTextBox);
					AddImageInfo(2, MapCode.ButtonBack, 103, 48, GeneralPanel.ButtonBackLabel, GeneralPanel.ButtonBackTextBox, GamepadButtonFlags.Back);
					AddImageInfo(2, MapCode.ButtonStart, 152, 48, GeneralPanel.ButtonStartLabel, GeneralPanel.ButtonStartTextBox, GamepadButtonFlags.Start);
					// D-Pad
					AddImageInfo(2, MapCode.DPadUp, 92, 88 - 13, GeneralPanel.DPadUpLabel, GeneralPanel.DPadUpTextBox, GamepadButtonFlags.DPadUp);
					AddImageInfo(2, MapCode.DPadLeft, 92 - 13, 88, GeneralPanel.DPadLeftLabel, GeneralPanel.DPadLeftTextBox, GamepadButtonFlags.DPadLeft);
					AddImageInfo(2, MapCode.DPadRight, 92 + 13, 88, GeneralPanel.DPadRightLabel, GeneralPanel.DPadRightTextBox, GamepadButtonFlags.DPadRight);
					AddImageInfo(2, MapCode.DPadDown, 92, 88 + 13, GeneralPanel.DPadDownLabel, GeneralPanel.DPadDownTextBox, GamepadButtonFlags.DPadDown);
					// D-Pad (Extra Map)
					AddImageInfo(2, MapCode.DPad, 92, 88, GeneralPanel.DPadLabel, GeneralPanel.DPadTextBox);
					// Left Thumb.
					AddImageInfo(2, MapCode.LeftThumbButton, 59, 47, GeneralPanel.LeftThumbButtonLabel, GeneralPanel.LeftThumbButtonTextBox, GamepadButtonFlags.LeftThumb);
					AddImageInfo(2, MapCode.LeftThumbAxisX, 59 + 10, 47, GeneralPanel.LeftThumbAxisXLabel, GeneralPanel.LeftThumbAxisXTextBox);
					AddImageInfo(2, MapCode.LeftThumbAxisY, 59, 47 - 10, GeneralPanel.LeftThumbAxisYLabel, GeneralPanel.LeftThumbAxisYTextBox);
					// Left Thumb (Extra Map).
					AddImageInfo(2, MapCode.LeftThumbUp, 59, 47 - 10, GeneralPanel.LeftThumbUpLabel, GeneralPanel.LeftThumbUpTextBox);
					AddImageInfo(2, MapCode.LeftThumbLeft, 59 - 10, 47, GeneralPanel.LeftThumbLeftLabel, GeneralPanel.LeftThumbLeftTextBox);
					AddImageInfo(2, MapCode.LeftThumbRight, 59 + 10, 47, GeneralPanel.LeftThumbRightLabel, GeneralPanel.LeftThumbRightTextBox);
					AddImageInfo(2, MapCode.LeftThumbDown, 59, 47 + 10, GeneralPanel.LeftThumbDownLabel, GeneralPanel.LeftThumbDownTextBox);
					// Right Thumb.
					AddImageInfo(2, MapCode.RightThumbButton, 160, 88, GeneralPanel.RightThumbButtonLabel, GeneralPanel.RightThumbButtonTextBox, GamepadButtonFlags.RightThumb);
					AddImageInfo(2, MapCode.RightThumbAxisX, 160 + 10, 88, GeneralPanel.RightThumbAxisXLabel, GeneralPanel.RightThumbAxisXTextBox);
					AddImageInfo(2, MapCode.RightThumbAxisY, 160, 88 - 10, GeneralPanel.RightThumbAxisYLabel, GeneralPanel.RightThumbAxisYTextBox);
					// Right Thumb (Extra Map).
					AddImageInfo(2, MapCode.RightThumbUp, 160, 88 - 10, GeneralPanel.RightThumbUpLabel, GeneralPanel.RightThumbUpTextBox);
					AddImageInfo(2, MapCode.RightThumbLeft, 160 - 10, 88, GeneralPanel.RightThumbLeftLabel, GeneralPanel.RightThumbLeftTextBox);
					AddImageInfo(2, MapCode.RightThumbRight, 160 + 10, 88, GeneralPanel.RightThumbRightLabel, GeneralPanel.RightThumbRightTextBox);
					AddImageInfo(2, MapCode.RightThumbDown, 160, 88 + 10, GeneralPanel.RightThumbDownLabel, GeneralPanel.RightThumbDownTextBox);
				}
				return _imageInfos;
			}
		}
		List<ImageInfo> _imageInfos;

		public void AddImageInfo(int image, MapCode code, double x, double y, object label, object control, GamepadButtonFlags button = GamepadButtonFlags.None)
			=> _imageInfos.Add(new ImageInfo(image, code, x, y, label, control, button));

		#endregion

		#region ■ Settings Map

		public MapTo MappedTo;

		#endregion

		//XINPUT_GAMEPAD GamePad;
		Guid _InstanceGuid;

		///// <summary>
		///// Get PadSetting from currently selected device.
		///// </summary>
		//public PadSetting CloneCurrentPadSetting()
		//{
		//	// Get settings related to PAD.
		//	var maps = SettingsManager.Current.SettingsMap.Where(x => x.MapTo == MappedTo).ToArray();
		//	PropertyInfo[] properties;
		//	if (!SettingsManager.ValidatePropertyNames(maps, out properties))
		//		return null;
		//	var ps = new PadSetting();
		//	foreach (var p in properties)
		//	{
		//		var map = maps.FirstOrDefault(x => x.PropertyName == p.Name);
		//		if (map == null)
		//			continue;
		//		// Get setting value from the form.
		//		var v = SettingsManager.Current.GetSettingValue(map.Control);
		//		// Set value onto padSetting.
		//		p.SetValue(ps, v ?? "", null);
		//	}
		//	ps.PadSettingChecksum = ps.CleanAndGetCheckSum();
		//	return ps;
		//}

		object updateFromDirectInputLock = new object();

		#region ■ Update Controls

		void UpdateDirectInputTabPage(UserDevice diDevice)
		{
			var isOnline = diDevice != null && diDevice.IsOnline;
			var hasState = isOnline && diDevice.Device != null;
			var instance = diDevice == null ? "" : " - " + diDevice.InstanceId;
			var text = "Direct Input" + instance + (isOnline ? hasState ? "" : " - On-line" : " - Off-line");
			PadItemPanel.DInputTabPage.Header = text;
		}

		#endregion

		// Old XInput state.
		bool oldConnected;
		// Current XInput state.
		State newState;
		bool newConnected;


		// Check left thumbStick
		public float FloatToByte(float v)
		{
			// -1 to 1 int16.MinValue int16.MaxValue.
			return (byte)Math.Round(v * byte.MaxValue);
		}

		#region ■ Mapped Devices

		public event EventHandler<EventArgs<UserSetting>> OnSettingChanged;

		public UserSetting CurrentUserSetting
			=> _CurrentUserSetting;
		private UserSetting _CurrentUserSetting;

		public UserDevice CurrentUserDevice
			=> _CurrentUserDevice;
		private UserDevice _CurrentUserDevice;

		/// <summary>
		/// Load pad setting into control
		/// </summary>
		public void LoadPadSetting(Guid? padSettingChecksum)
		{
			// Load PadSetting object from configuration.
			PadSetting ps = null;
			if (padSettingChecksum.HasValue)
				ps = SettingsManager.GetPadSetting(padSettingChecksum.Value);
			if (ps == null)
				ps = new PadSetting();
			// Stop monitoring changes.
			CurrentPadSetting.PropertyChanged -= CurrentPadSetting_PropertyChanged;
			// Load values into current object which is attached to all controls.
			CurrentPadSetting.Load(ps);
			// Rebind pad setting to controls.
			DPadPanel.SetBinding(CurrentPadSetting);
			GeneralPanel.SetBinding(MappedTo, CurrentPadSetting);
			AdvancedPanel.SetBinding(CurrentPadSetting);
			LeftTriggerPanel.SetBinding(CurrentPadSetting);
			RightTriggerPanel.SetBinding(CurrentPadSetting);
			LeftThumbXPanel.SetBinding(CurrentPadSetting);
			LeftThumbYPanel.SetBinding(CurrentPadSetting);
			RightThumbXPanel.SetBinding(CurrentPadSetting);
			RightThumbYPanel.SetBinding(CurrentPadSetting);
			ForceFeedbackPanel.SetBinding(MappedTo, CurrentPadSetting);
			ForceFeedbackPanel.LeftForceFeedbackMotorPanel.SetBinding(CurrentPadSetting, 0);
			ForceFeedbackPanel.RightForceFeedbackMotorPanel.SetBinding(CurrentPadSetting, 1);
			PadFootPanel.SetBinding(MappedTo, _CurrentUserDevice, CurrentPadSetting);
			// Start monitoring changes.
			CurrentPadSetting.PropertyChanged += CurrentPadSetting_PropertyChanged;
			//SettingsManager.Current.LoadPadSettingsIntoSelectedDevice(MappedTo, CurrentPadSetting);
		}

		public void SavePadSetting(PadSetting ps)
		{

		}

		public readonly PadSetting CurrentPadSetting;

		static object selectionLock = new object();

		private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			lock (selectionLock)
			{
				var grid = (DataGrid)sender;
				var setting = grid.SelectedItems.Cast<UserSetting>().FirstOrDefault();
				_CurrentUserSetting = setting;
				// Get device attached to user setting.
				_CurrentUserDevice = setting == null
					? new UserDevice()
					: SettingsManager.GetDevice(setting.InstanceGuid);
				// Load pad settings.
				LoadPadSetting(setting?.PadSettingChecksum);
				OnSettingChanged?.Invoke(this, new EventArgs<UserSetting>(setting));
			}
		}

		#endregion

		List<SettingsMapItem> RecordAllMaps = new List<SettingsMapItem>();

		public string RemapName = "Remap All";
		public string RemapStopName = "STOP";

		private void RemapAllButton_Click(object sender, EventArgs e)
		{
			// If stop mode then...
			if (PadFootPanel.RemapAllButton.Content as string != RemapName)
			{
				StopRecording();
				return;
			}
			if (!SettingsManager.Current.ClearAll(MappedTo))
				return;
			StopRecording();
			// Buttons to record.
			var codes = new List<MapCode> {
				MapCode.LeftTrigger,
				MapCode.RightTrigger,
				MapCode.LeftShoulder,
				MapCode.RightShoulder,
				MapCode.ButtonBack,
				MapCode.ButtonStart,
				MapCode.DPad,
				MapCode.LeftThumbUp,
				MapCode.LeftThumbRight,
				MapCode.LeftThumbButton,
				MapCode.RightThumbUp,
				MapCode.RightThumbRight,
				MapCode.RightThumbButton,
				MapCode.ButtonY,
				MapCode.ButtonX,
				MapCode.ButtonB,
				MapCode.ButtonA,
			};
			RecordAllMaps = SettingsManager.Current.SettingsMap.Where(x => x.MapTo == MappedTo && codes.Contains(x.Code))
				// Order as same as in the list above.
				.OrderBy(x => codes.IndexOf(x.Code))
				.ToList();
			StartRecording();
		}

		private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowLoad(this))
				return;
		}

		private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowUnload(this))
				return;
			// Cleanup references which prevents disposal.
			Global.UpdateControlFromStates -= Global_UpdateControlFromStates;
			SettingsManager.Current.SettingChanged -= Current_SettingChanged;
			CurrentPadSetting.PropertyChanged -= CurrentPadSetting_PropertyChanged;
			PadListPanel.MainDataGrid.SelectionChanged -= MainDataGrid_SelectionChanged;
            PadFootPanel.MapNameComboBox.SelectionChanged -= GeneralPanel.MapNameComboBox_SelectionChanged;
            PadFootPanel.RemapAllButton.Click -= RemapAllButton_Click;
            XboxImage.StartRecording = null;
			XboxImage.StopRecording = null;
			RecordAllMaps.Clear();
			imageInfos.Clear();
			_Imager?.Dispose();
			_Imager = null;
			_CurrentUserSetting = null;
			_CurrentUserDevice = null;
		}

	}
}
