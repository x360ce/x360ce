using JocysCom.ClassLibrary;
using JocysCom.ClassLibrary.Controls;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
//using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
//using System.Windows.Documents;
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

			_Imager.LeftTriggerAxisStatus = XboxImage.LeftTriggerAxisBorder;
			_Imager.RightTriggerAxisStatus = XboxImage.RightTriggerAxisBorder;
			_Imager.LeftThumbAxisStatus = XboxImage.LeftThumbAxisBorder;
			_Imager.RightThumbAxisStatus = XboxImage.RightThumbAxisBorder;

			_Imager.ImageControl = XboxImage;
			XboxImage.InitializeImages(imageInfos, _Imager, mappedTo);
			XboxImage.StartRecording = StartRecording;
			XboxImage.StopRecording = StopRecording;
			// Axis to Button DeadZones
			ButtonsPanel.AxisToButtonADeadZonePanel.MonitorTextBox = GeneralPanel.ActionATextBox;
			ButtonsPanel.AxisToButtonBDeadZonePanel.MonitorTextBox = GeneralPanel.ActionBTextBox;
			ButtonsPanel.AxisToButtonXDeadZonePanel.MonitorTextBox = GeneralPanel.ActionXTextBox;
			ButtonsPanel.AxisToButtonYDeadZonePanel.MonitorTextBox = GeneralPanel.ActionYTextBox;
			ButtonsPanel.AxisToButtonStartDeadZonePanel.MonitorTextBox = GeneralPanel.MenuStartTextBox;
			ButtonsPanel.AxisToButtonBackDeadZonePanel.MonitorTextBox = GeneralPanel.MenuBackTextBox;
			ButtonsPanel.AxisToLeftShoulderDeadZonePanel.MonitorTextBox = GeneralPanel.BumperLTextBox;
			ButtonsPanel.AxisToLeftThumbButtonDeadZonePanel.MonitorTextBox = GeneralPanel.StickLButtonTextBox;
			ButtonsPanel.AxisToRightShoulderDeadZonePanel.MonitorTextBox = GeneralPanel.BumperRTextBox;
			ButtonsPanel.AxisToRightThumbButtonDeadZonePanel.MonitorTextBox = GeneralPanel.StickRButtonTextBox;
			ButtonsPanel.AxisToDPadDownDeadZonePanel.MonitorTextBox = GeneralPanel.DPadDownTextBox;
			ButtonsPanel.AxisToDPadLeftDeadZonePanel.MonitorTextBox = GeneralPanel.DPadLeftTextBox;
			ButtonsPanel.AxisToDPadRightDeadZonePanel.MonitorTextBox = GeneralPanel.DPadRightTextBox;
			ButtonsPanel.AxisToDPadUpDeadZonePanel.MonitorTextBox = GeneralPanel.DPadUpTextBox;
			// Monitor setting changes.
			SettingsManager.Current.SettingChanged += Current_SettingChanged;
			PadListPanel.SetBinding(MappedTo);
			PadListPanel.DevicesDataGrid.SelectionChanged += MainDataGrid_SelectionChanged;
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

		bool _isOnline = false;

		private void UpdateControlFromDInput()
		{
			lock (updateFromDirectInputLock)
			{
				var ud = CurrentUserDevice;
				var udNotNull = ud != null;
				var instanceGuid = udNotNull ? ud.InstanceGuid : Guid.Empty;
				var isOnline = udNotNull ? ud.IsOnline : false;

				ControlsHelper.SetEnabled(PadFootPanel.RemapAllButton, udNotNull && ud.DiState != null);
				PadItemPanel.SetEnabled(udNotNull);
				// If device instance changed then...
				if (!Equals(_InstanceGuid, instanceGuid))
				{
					//if (instanceGuid != Guid.Empty && ud?.DeviceState != null)
					//{
					_InstanceGuid = instanceGuid;
					GeneralPanel.ResetDiMenuStrip(udNotNull && ud.IsOnline ? ud : null);
					//}
				}

				if (!Equals(_isOnline, isOnline))
				{
					_isOnline = isOnline;
					GeneralPanel.ResetDiMenuStrip(udNotNull && ud.IsOnline ? ud : null);
				}
				// Update direct input form and return actions (pressed Buttons/DPads, turned Axis/Sliders).
				UpdateDirectInputTabPage(ud);

				PadItemPanel.DInputPanel.UpdateFrom(ud);
				// DragAndDrop menu update. ---------------------------------------------------------------------------------------------------------------------------
				PadItemPanel.GeneralPanel.DragAndDropMenuLabels_Update(ud);

				if (udNotNull && _Imager.Recorder.Recording)
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
			var getXInputStates = SettingsManager.Options.GetXInputStates;
			newState = getXInputStates
				? Global.DHelper.LiveXiStates[i]
				: Global.DHelper.CombinedXiStates[i];
			newConnected = getXInputStates
				? Global.DHelper.LiveXiConnected[i]
				: Global.DHelper.CombinedXiConnected[i];
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
				var customDiState = GeneralPanel.GetCustomDiState(CurrentUserDevice);
				
				// Process all buttons and axis. ------------------------------------------------------------------------------------------------------
				foreach (var ii in imageInfos)
				{
					//SetLabelDIContent(customDiState, ii.Type, (StackPanel)ii.ControlStackPanel);
					_Imager.DrawState(ii, newState.Gamepad, customDiState);
				}
			}

			// Process device.
			if (CurrentUserDevice?.DiState != null && CurrentPadSetting != null)
			{
				// Update graphs.
				var axis = CurrentUserDevice.DiState.Axis;
				foreach (var (target, panel, value) in new (TargetType Target, AxisMapControl Panel, short Value)[]
				{
					(TargetType.LeftThumbX, LeftThumbXPanel, newState.Gamepad.LeftThumbX),
					(TargetType.LeftThumbY, LeftThumbYPanel, newState.Gamepad.LeftThumbY),
					(TargetType.RightThumbX, RightThumbXPanel, newState.Gamepad.RightThumbX),
					(TargetType.RightThumbY, RightThumbYPanel, newState.Gamepad.RightThumbY),
					(TargetType.LeftTrigger, LeftTriggerPanel, newState.Gamepad.LeftTrigger),
					(TargetType.RightTrigger, RightTriggerPanel, newState.Gamepad.RightTrigger),
				})
				{
					// Get current pad setting.
					Map map = CurrentPadSetting.Maps.FirstOrDefault(x => x.Target == target);
					if (map != null && map.Index > 0 && map.Index <= axis.Length)
					{
						panel.UpdateGraph(axis[map.Index - 1], value, map.IsInverted, map.IsHalf);
					}
				}
			}
			// Update Axis to Button Images.
			if (_AxisToButtonControls == null)
				_AxisToButtonControls = ControlsHelper.GetAll<AxisToButtonControl>(ButtonsPanel.MainGroupBox);
			foreach (var atbPanel in _AxisToButtonControls)
				atbPanel.Refresh(newState);
			// Store old state.
			oldConnected = newConnected;
		}

		//private void SetLabelDIContent(CustomDiState customDiState, TargetType targetType, StackPanel sp)
		//{
			
		//	Map map = CurrentPadSetting.Maps.FirstOrDefault(x => x.Target == targetType);

		//	if (map?.Index <= 0/* || map.Index > axisLength*/)
		//		return;

		//	var i = map.Index - 1;
		//	if (map.IsAxis || map.IsHalf || map.IsInverted)
		//	{
		//		((Label)sp.Children[1]).Content = customDiState.Axis[i];
		//	}
		//	else if (map.IsButton)
		//	{
		//		((Label)sp.Children[1]).Content = customDiState.Buttons[i] ? 1 : 0;
		//	}
		//	else if (map.IsSlider)
		//	{
		//		((Label)sp.Children[1]).Content = customDiState.Sliders[i];
		//	}
		//}

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

		//LeftTrigger		> TriggerLeftAxis
		//RightTrigger		> TriggerRightAxis

		//LeftShoulder		> BumperLeftButton
		//RightShoulder		> BumperRightButton

		//ButtonBack		> MenuBackButton
		//ButtonStart		> MenuStartButton
		//ButtonGuide		> MenuGuideButton

		//ButtonY			> ActionYButton
		//ButtonX			> ActionXButton
		//ButtonB			> ActionBButton
		//ButtonA			> ActionAButton

		//DPad				> DPadButton
		//DPadUp			> DPadUpButton
		//DPadLeft			> DPadLeftButton
		//DPadRight			> DPadRightButton
		//DPadDown			> DPadDownButton

		//LeftThumbButton	> StickLeftButton
		//LeftThumbAxisX	> StickLeftXAxis
		//LeftThumbAxisY	> StickLeftYAxis
		//LeftThumbUp		> StickLeftUpAxis
		//LeftThumbLeft		> StickLeftLeftAxis
		//LeftThumbRight	> StickLeftRightAxis
		//LeftThumbDown		> StickLeftDownAxis

		//RightThumbButton	> StickRightButton
		//RightThumbAxisX	> StickRightXAxis
		//RightThumbAxisY	> StickRightYAxis
		//RightThumbUp		> StickRightUpAxis
		//RightThumbRight	> StickRightLeftAxis
		//RightThumbRight	> StickRightRightAxis
		//RightThumbDown	> StickRightDownAxis

		public List<ImageInfo> imageInfos
		{
			get
			{
				if (_imageInfos == null)
				{
					// Configure.
					_imageInfos = new List<ImageInfo>();
					// Triggers.
					AddImageInfo(1, TargetType.LeftTrigger, MapCode.LeftTrigger, 63, 27, GeneralPanel.TriggerLLabel, GeneralPanel.TriggerLXILabel, GeneralPanel.TriggerLTextBox);
					AddImageInfo(1, TargetType.RightTrigger, MapCode.RightTrigger, 193, 27, GeneralPanel.TriggerRLabel, GeneralPanel.TriggerRXILabel, GeneralPanel.TriggerRTextBox);
					// Bumpers.
					AddImageInfo(1, TargetType.Button, MapCode.LeftShoulder, 43, 66, GeneralPanel.BumperLLabel, GeneralPanel.BumperLXILabel, GeneralPanel.BumperLTextBox, GamepadButtonFlags.LeftShoulder);
					AddImageInfo(1, TargetType.Button, MapCode.RightShoulder, 213, 66, GeneralPanel.BumperRLabel, GeneralPanel.BumperRXILabel, GeneralPanel.BumperRTextBox, GamepadButtonFlags.RightShoulder);
					// Action.
					AddImageInfo(2, TargetType.Button, MapCode.ButtonY, 196, 29, GeneralPanel.ActionYLabel, GeneralPanel.ActionYXILabel, GeneralPanel.ActionYTextBox, GamepadButtonFlags.Y);
					AddImageInfo(2, TargetType.Button, MapCode.ButtonX, 178, 48, GeneralPanel.ActionXLabel, GeneralPanel.ActionXXILabel, GeneralPanel.ActionXTextBox, GamepadButtonFlags.X);
					AddImageInfo(2, TargetType.Button, MapCode.ButtonB, 215, 48, GeneralPanel.ActionBLabel, GeneralPanel.ActionBXILabel, GeneralPanel.ActionBTextBox, GamepadButtonFlags.B);
					AddImageInfo(2, TargetType.Button, MapCode.ButtonA, 196, 66, GeneralPanel.ActionALabel, GeneralPanel.ActionAXILabel, GeneralPanel.ActionATextBox, GamepadButtonFlags.A);
					// Menu.
					AddImageInfo(2, TargetType.Button, MapCode.ButtonGuide, 127, 48, GeneralPanel.MenuGuideLabel, GeneralPanel.MenuGuideXILabel, GeneralPanel.MenuGuideTextBox, GamepadButtonFlags.Guide);
					AddImageInfo(2, TargetType.Button, MapCode.ButtonBack, 103, 48, GeneralPanel.MenuBackLabel, GeneralPanel.MenuBackXILabel, GeneralPanel.MenuBackTextBox, GamepadButtonFlags.Back);
					AddImageInfo(2, TargetType.Button, MapCode.ButtonStart, 152, 48, GeneralPanel.MenuStartLabel, GeneralPanel.MenuStartXILabel, GeneralPanel.MenuStartTextBox, GamepadButtonFlags.Start);
					// D-Pad.
					AddImageInfo(2, TargetType.Button, MapCode.DPad, 92, 88, GeneralPanel.DPadLabel, GeneralPanel.DPadXILabel, GeneralPanel.DPadTextBox);
					AddImageInfo(2, TargetType.Button, MapCode.DPadUp, 92, 88 - 13, GeneralPanel.DPadUpLabel, GeneralPanel.DPadUpXILabel, GeneralPanel.DPadUpTextBox, GamepadButtonFlags.DPadUp);
					AddImageInfo(2, TargetType.Button, MapCode.DPadLeft, 92 - 13, 88, GeneralPanel.DPadLeftLabel, GeneralPanel.DPadLeftXILabel, GeneralPanel.DPadLeftTextBox, GamepadButtonFlags.DPadLeft);
					AddImageInfo(2, TargetType.Button, MapCode.DPadRight, 92 + 13, 88, GeneralPanel.DPadRightLabel, GeneralPanel.DPadRightXILabel, GeneralPanel.DPadRightTextBox, GamepadButtonFlags.DPadRight);
					AddImageInfo(2, TargetType.Button, MapCode.DPadDown, 92, 88 + 13, GeneralPanel.DPadDownLabel, GeneralPanel.DPadDownXILabel, GeneralPanel.DPadDownTextBox, GamepadButtonFlags.DPadDown);
					// Stick Left.
					AddImageInfo(2, TargetType.Button, MapCode.LeftThumbButton, 59, 47, GeneralPanel.StickLButtonLabel, GeneralPanel.StickLButtonXILabel, GeneralPanel.StickLButtonTextBox, GamepadButtonFlags.LeftThumb);
					AddImageInfo(2, TargetType.LeftThumbX, MapCode.LeftThumbAxisX, 59 + 10, 47, GeneralPanel.StickLAxisXLabel, GeneralPanel.StickLAxisXXILabel, GeneralPanel.StickLAxisXTextBox);
					AddImageInfo(2, TargetType.LeftThumbY, MapCode.LeftThumbAxisY, 59, 47 - 10, GeneralPanel.StickLAxisYLabel, GeneralPanel.StickLAxisYXILabel, GeneralPanel.StickLAxisYTextBox);
					AddImageInfo(2, TargetType.LeftThumbX, MapCode.LeftThumbUp, 59, 47 - 10, GeneralPanel.StickLUpLabel, GeneralPanel.StickLUpXILabel, GeneralPanel.StickLUpTextBox);
					AddImageInfo(2, TargetType.LeftThumbX, MapCode.LeftThumbLeft, 59 - 10, 47, GeneralPanel.StickLLeftLabel, GeneralPanel.StickLLeftXILabel, GeneralPanel.StickLLeftTextBox);
					AddImageInfo(2, TargetType.LeftThumbX, MapCode.LeftThumbRight, 59 + 10, 47, GeneralPanel.StickLRightLabel, GeneralPanel.StickLRightXILabel, GeneralPanel.StickLRightTextBox);
					AddImageInfo(2, TargetType.LeftThumbX, MapCode.LeftThumbDown, 59, 47 + 10, GeneralPanel.StickLDownLabel, GeneralPanel.StickLDownXILabel, GeneralPanel.StickLDownTextBox);
					// Stick Right.
					AddImageInfo(2, TargetType.Button, MapCode.RightThumbButton, 160, 88, GeneralPanel.StickRButtonLabel, GeneralPanel.StickRButtonXILabel, GeneralPanel.StickRButtonTextBox, GamepadButtonFlags.RightThumb);
					AddImageInfo(2, TargetType.RightThumbX, MapCode.RightThumbAxisX, 160 + 10, 88, GeneralPanel.StickRAxisXLabel, GeneralPanel.StickRAxisXXILabel, GeneralPanel.StickRAxisXTextBox);
					AddImageInfo(2, TargetType.RightThumbY, MapCode.RightThumbAxisY, 160, 88 - 10, GeneralPanel.StickRAxisYLabel, GeneralPanel.StickRAxisYXILabel, GeneralPanel.StickRAxisYTextBox);
					AddImageInfo(2, TargetType.RightThumbX, MapCode.RightThumbUp, 160, 88 - 10, GeneralPanel.StickRUpLabel, GeneralPanel.StickRUpXILabel, GeneralPanel.StickRUpTextBox);
					AddImageInfo(2, TargetType.RightThumbX, MapCode.RightThumbLeft, 160 - 10, 88, GeneralPanel.StickRLeftLabel, GeneralPanel.StickRLeftXILabel, GeneralPanel.StickRLeftTextBox);
					AddImageInfo(2, TargetType.RightThumbX, MapCode.RightThumbRight, 160 + 10, 88, GeneralPanel.StickRRightLabel, GeneralPanel.StickRRightXILabel, GeneralPanel.StickRRightTextBox);
					AddImageInfo(2, TargetType.RightThumbX, MapCode.RightThumbDown, 160, 88 + 10, GeneralPanel.StickRDownLabel, GeneralPanel.StickRDownXILabel, GeneralPanel.StickRDownTextBox);
				}
				return _imageInfos;
			}
		}
		List<ImageInfo> _imageInfos;

		public void AddImageInfo(int image, TargetType type, MapCode code, double x, double y, object controlName, object controlValue, object controlBindingName, GamepadButtonFlags button = GamepadButtonFlags.None)
			=> _imageInfos.Add(new ImageInfo(image, type, code, x, y, controlName, controlValue, controlBindingName, button));

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
		//	foreach (var dInputPolylineStepSize in properties)
		//	{
		//		var map = maps.FirstOrDefault(x => x.PropertyName == dInputPolylineStepSize.Name);
		//		if (map == null)
		//			continue;
		//		// Get setting value from the form.
		//		var v = SettingsManager.Current.GetSettingValue(map.Control);
		//		// Set value onto padSetting.
		//		dInputPolylineStepSize.SetValue(ps, v ?? "", null);
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
			GeneralPanel.SetBinding(MappedTo, CurrentPadSetting, imageInfos);
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
				MapCode.LeftThumbDown,
				MapCode.LeftThumbLeft,
				MapCode.LeftThumbRight,
				MapCode.LeftThumbButton,
				MapCode.RightThumbUp,
				MapCode.RightThumbDown,
				MapCode.RightThumbLeft,
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
			// Moved to MainBodyControl_Unloaded().
		}

		public void ParentWindow_Unloaded()
		{
			// Cleanup references which prevents disposal.
			Global.UpdateControlFromStates -= Global_UpdateControlFromStates;
			SettingsManager.Current.SettingChanged -= Current_SettingChanged;
			CurrentPadSetting.PropertyChanged -= CurrentPadSetting_PropertyChanged;
			PadListPanel.DevicesDataGrid.SelectionChanged -= MainDataGrid_SelectionChanged;
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
