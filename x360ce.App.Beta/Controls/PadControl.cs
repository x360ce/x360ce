using JocysCom.ClassLibrary;
using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Runtime;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	public partial class PadControl : UserControl, IPadControl
	{

		public PadControl(MapTo controllerIndex)
		{
			InitializeComponent();
			if (ControlsHelper.IsDesignMode(this))
				return;

			PadItemPanel = new PadItemControl();
			PadItemHost.Child = PadItemPanel;
			// Add controls which must be notified on setting selection change.
			MacrosPanel.PadControl = this;
			Global.UpdateControlFromStates += Global_UpdateControlFromStates;
			// Hide for this version.
			PadItemPanel.PadTabControl.Items.Remove(PadItemPanel.XInputTabPage);
			//PadTabControl.TabPages.Remove(MacrosTabPage);
			RemapName = GeneralPanel.RemapAllButton.Content as string;
			MappedTo = controllerIndex;
			_Imager = new PadControlImager();
			_Imager.Top = XboxImage.TopPictureImage;
			_Imager.Front = XboxImage.FrontPictureImage;
			_Imager.LeftThumbStatus = XboxImage.LeftThumbContentControl;
			_Imager.RightThumbStatus = XboxImage.RightThumbContentControl;
			_Imager.LeftTriggerStatus = XboxImage.LeftTriggerContentControl;
			_Imager.RightTriggerStatus = XboxImage.RightTriggerContentControl;
			_Imager.ImageControl = XboxImage;
			XboxImage.InitializeImages(imageInfos, _Imager, controllerIndex);
			XboxImage.StartRecording = StartRecording;
			XboxImage.StopRecording = StopRecording;
			// Make font more consistent with the rest of the interface.
			Controls.OfType<ToolStrip>().ToList().ForEach(x => x.Font = Font);
			// Hide left/right border.
			//MappedDevicesDataGridView.Width = this.Width + 2;
			//MappedDevicesDataGridView.Left = -1;
			JocysCom.ClassLibrary.Controls.ControlsHelper.ApplyBorderStyle(MappedDevicesDataGridView);
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
			// Load Settings and enable events.
			UpdateGetXInputStatesWithNoEvents();
			// Monitor option changes.
			SettingsManager.OptionsData.Items.ListChanged += Items_ListChanged;
			// Monitor setting changes.
			SettingsManager.Current.SettingChanged += Current_SettingChanged;

		}
		private PadItemControl PadItemPanel;
		private PadItem_GeneralControl GeneralPanel => PadItemPanel.GeneralPanel;
		private PadItem_AdvancedControl AdvancedPanel => PadItemPanel.AdvancedPanel;
		private AxisToButtonListControl ButtonsPanel => PadItemPanel.ButtonsPanel;
		private PadItem_DPadControl DPadPanel => PadItemPanel.DPadPanel;
		private AxisMapControl LeftTriggerPanel => PadItemPanel.LeftTriggerPanel;
		private AxisMapControl RightTriggerPanel => PadItemPanel.RightTriggerPanel;
		private AxisMapControl LeftThumbXPanel => PadItemPanel.LeftThumbXPanel;
		private AxisMapControl LeftThumbYPanel => PadItemPanel.LeftThumbYPanel;
		private AxisMapControl RightThumbXPanel => PadItemPanel.RightThumbXPanel;
		private AxisMapControl RightThumbYPanel => PadItemPanel.RightThumbYPanel;
		private PadItem_MacrosControl MacrosPanel => PadItemPanel.MacrosPanel;
		private PadItem_ForceFeedbackControl ForceFeedbackPanel => PadItemPanel.ForceFeedbackPanel;
		//private XInputUserControl XInputPanel => PadItemPanel.XInputPanel;
		private PadItem_DInputControl DInputPanel => PadItemPanel.DInputPanel;

		private PadItem_General_XboxImageControl XboxImage => GeneralPanel.XboxImage;




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
				ControlsHelper.SetEnabled(LoadPresetButton, enable);
				ControlsHelper.SetEnabled(AutoPresetButton, enable);
				ControlsHelper.SetEnabled(ClearPresetButton, enable);
				ControlsHelper.SetEnabled(ResetPresetButton, enable);
				ControlsHelper.SetEnabled(GeneralPanel.RemapAllButton, enable && ud.DiState != null);
				PadItemPanel.SetEnabled(enable);
				// If device instance changed then...
				if (!Equals(instanceGuid, _InstanceGuid))
				{
					_InstanceGuid = instanceGuid;
					GeneralPanel.ResetDiMenuStrip(enable ? ud : null);
				}
				// Update direct input form and return actions (pressed Buttons/DPads, turned Axis/Sliders).
				UpdateDirectInputTabPage(ud);
				DInputPanel.UpdateFrom(ud);
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
							GeneralPanel.RemapAllButton.Content = RemapName;
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
				_Imager.SetImages(false);
				GeneralPanel.RemapAllButton.IsEnabled = false;
			}
			// If device connected then show enabled images.
			if (newConnected && !oldConnected)
			{
				_Imager.SetImages(true);
				GeneralPanel.RemapAllButton.IsEnabled = true;
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
			if (ud != null && ud.DiState != null)
			{
				// Get current pad setting.
				var ps = CurrentPadSetting;
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
			GeneralPanel.RemapAllButton.Content = RemapName;
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
			GeneralPanel.RemapAllButton.Content = RemapStopName;
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

		private void Items_ListChanged(object sender, ListChangedEventArgs e)
		{
			var pd = e.PropertyDescriptor;
			if (pd != null)
			{
				var o = SettingsManager.Options;
				// Update values only if different.
				if (e.PropertyDescriptor.Name == nameof(Options.GetXInputStates))
				{
					UpdateGetXInputStatesWithNoEvents();
				}
			}
		}

		object GetXInputStatesCheckBoxLock = new object();

		public void UpdateGetXInputStatesWithNoEvents()
		{
			lock (GetXInputStatesCheckBoxLock)
			{
				// Disable events.
				GetXInputStatesCheckBox.Click -= GetXInputStatesCheckBox_Click;
				var o = SettingsManager.Options;
				ControlsHelper.SetChecked(GetXInputStatesCheckBox, o.GetXInputStates);
				GetXInputStatesCheckBox.Image = o.GetXInputStates
				   ? Properties.Resources.checkbox_16x16
				   : Properties.Resources.checkbox_unchecked_16x16;
				// Enable events.
				GetXInputStatesCheckBox.Click += GetXInputStatesCheckBox_Click;
			}
		}

		// Must trigger only by the user input.
		private void GetXInputStatesCheckBox_Click(object sender, EventArgs e)
		{
			SettingsManager.Options.GetXInputStates = !SettingsManager.Options.GetXInputStates;
		}

		public void InitPadData()
		{
			// WORKAROUND: Remove SelectionChanged event.
			MappedDevicesDataGridView.SelectionChanged -= MappedDevicesDataGridView_SelectionChanged;
			MappedDevicesDataGridView.DataSource = mappedItems;
			GeneralPanel.InitPadData();
			// WORKAROUND: Use BeginInvoke to prevent SelectionChanged firing multiple times.
			ControlsHelper.BeginInvoke(() =>
			{
				MappedDevicesDataGridView.SelectionChanged += MappedDevicesDataGridView_SelectionChanged;
				MappedDevicesDataGridView_SelectionChanged(MappedDevicesDataGridView, new EventArgs());
			});
			UserSettings_Items_ListChanged(null, null);
			SettingsManager.UserSettings.Items.ListChanged += UserSettings_Items_ListChanged;
		}

		public void InitPadControl()
		{
			var dv = new System.Data.DataView();
			var grid = MappedDevicesDataGridView;
			grid.AutoGenerateColumns = false;
			// Show disabled images by default.
			_Imager.SetImages(false);
			// Add player index to combo boxes
			var playerOptions = new List<KeyValuePair>();
			var playerTypes = (UserIndex[])Enum.GetValues(typeof(UserIndex));
			foreach (var item in playerTypes)
				playerOptions.Add(new KeyValuePair(item.ToString(), ((int)item).ToString()));
			UpdateFromCurrentGame();
		}

		public void UpdateFromCurrentGame()
		{
			var game = SettingsManager.CurrentGame;
			var flag = AppHelper.GetMapFlag(MappedTo);
			// Update Virtual.
			var virt = game != null && ((MapToMask)game.EnableMask).HasFlag(flag);
			EnableButton.Checked = virt;
			EnableButton.Image = virt
				? x360ce.App.Properties.Resources.checkbox_16x16
				: x360ce.App.Properties.Resources.checkbox_unchecked_16x16;
			// Update emulation type.
			ShowAdvancedTab(game != null && game.EmulationType == (int)EmulationType.Library);
			// Update AutoMap.
			var auto = game != null && ((MapToMask)game.AutoMapMask).HasFlag(flag);
			AutoMapButton.Checked = auto;
			AutoMapButton.Image = auto
				? x360ce.App.Properties.Resources.checkbox_16x16
				: x360ce.App.Properties.Resources.checkbox_unchecked_16x16;
			MappedDevicesDataGridView.Enabled = !auto;
			MappedDevicesDataGridView.BackgroundColor = auto
				? SystemColors.Control
				: SystemColors.Window;
			MappedDevicesDataGridView.DefaultCellStyle.BackColor = auto
				? SystemColors.Control
				: SystemColors.Window;
			if (auto)
			{
				// Remove mapping from all devices.	
				var grid = MappedDevicesDataGridView;
				var items = grid.Rows.Cast<DataGridViewRow>().Where(x => x.Visible).Select(x => (UserSetting)x.DataBoundItem).ToArray();
				foreach (var item in items)
				{
					item.MapTo = (int)MapTo.None;
				}
			}
			ShowHideAndSelectGridRows(null);
			UpdateGridButtons();
		}

		private void UserSettings_Items_ListChanged(object sender, ListChangedEventArgs e)
		{
			// Make sure there is no crash when function gets called from another thread.
			ControlsHelper.Invoke(() =>
			{
				ShowHideAndSelectGridRows(null);
			});
		}

		object DevicesToMapDataGridViewLock = new object();

		SortableBindingList<Engine.Data.UserSetting> mappedItems = new SortableBindingList<Engine.Data.UserSetting>();

		void ShowHideAndSelectGridRows(Guid? instanceGuid = null)
		{
			lock (DevicesToMapDataGridViewLock)
			{
				var grid = MappedDevicesDataGridView;
				var game = SettingsManager.CurrentGame;
				// Get rows which must be displayed on the list.
				var itemsToShow = SettingsManager.UserSettings.ItemsToArraySyncronized()
					// Filter devices by controller.	
					.Where(x => x.MapTo == (int)MappedTo)
					// Filter devices by selected game (no items will be shown if game is not selected).
					.Where(x => game != null && x.FileName == game.FileName && x.FileProductName == game.FileProductName)
					.ToList();
				var itemsToRemove = mappedItems.Except(itemsToShow).ToArray();
				var itemsToInsert = itemsToShow.Except(mappedItems).ToArray();

				// If columns will be hidden or shown then...
				if (itemsToRemove.Length > 0 || itemsToInsert.Length > 0)
				{
					var selection = instanceGuid.HasValue
						? new List<Guid>() { instanceGuid.Value }
						: JocysCom.ClassLibrary.Controls.ControlsHelper.GetSelection<Guid>(grid, nameof(UserSetting.InstanceGuid));
					grid.CurrentCell = null;
					// Suspend Layout.
					grid.SuspendLayout();
					var bound = grid.DataSource != null;
					CurrencyManager cm = null;
					if (bound)
					{
						// Suspend CurrencyManager to avoid exceptions.
						cm = (CurrencyManager)BindingContext[grid.DataSource];
						cm.SuspendBinding();
					}
					// Do removal.
					foreach (var item in itemsToRemove)
						mappedItems.Remove(item);
					// Do adding.
					foreach (var item in itemsToInsert)
						mappedItems.Add(item);
					if (bound)
						// Resume CurrencyManager and Layout.
						cm.ResumeBinding();
					grid.ResumeLayout();
					// Restore selection.
					JocysCom.ClassLibrary.Controls.ControlsHelper.RestoreSelection(grid, nameof(UserSetting.InstanceGuid), selection);
				}
				var visibleCount = mappedItems.Count();
				var title = string.Format("Enable {0} Mapped Device{1}", visibleCount, visibleCount == 1 ? "" : "s");
				if (mappedItems.Count(x => x.IsEnabled) > 1)
				{
					title += " (Combine)";
				}
				ControlsHelper.SetText(EnableButton, title);
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

		#region Images

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

		#region Settings Map

		public MapTo MappedTo;

		#endregion

		//XINPUT_GAMEPAD GamePad;
		Guid _InstanceGuid;

		/// <summary>
		/// Get PadSetting from currently selected device.
		/// </summary>
		public PadSetting CloneCurrentPadSetting()
		{
			// Get settings related to PAD.
			var maps = SettingsManager.Current.SettingsMap.Where(x => x.MapTo == MappedTo).ToArray();
			PropertyInfo[] properties;
			if (!SettingsManager.ValidatePropertyNames(maps, out properties))
				return null;
			var ps = new PadSetting();
			foreach (var p in properties)
			{
				var map = maps.FirstOrDefault(x => x.PropertyName == p.Name);
				if (map == null)
					continue;
				// Get setting value from the form.
				var v = SettingsManager.Current.GetSettingValue(map.Control);
				// Set value onto padSetting.
				p.SetValue(ps, v ?? "", null);
			}
			ps.PadSettingChecksum = ps.CleanAndGetCheckSum();
			return ps;
		}

		object updateFromDirectInputLock = new object();

		#region Update Controls

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

		void ClearPresetButton_Click(object sender, EventArgs e)
		{
			ClearAll();
		}

		bool ClearAll()
		{
			var description = Attributes.GetDescription(MappedTo);
			var text = string.Format("Do you want to clear all {0} settings?", description);
			var form = new MessageBoxWindow();
			var result = form.ShowDialog(text, "Clear Controller Settings", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
			if (result != System.Windows.MessageBoxResult.Yes)
				return false;
			SettingsManager.Current.LoadPadSettingsIntoSelectedDevice(MappedTo, null);
			return true;
		}

		void ResetPresetButton_Click(object sender, EventArgs e)
		{
			var description = Attributes.GetDescription(MappedTo);
			var text = string.Format("Do you really want to reset all {0} settings?", description);
			var form = new MessageBoxWindow();
			var result = form.ShowDialog(text, "Reset Controller Settings", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
			if (result == System.Windows.MessageBoxResult.Yes)
			{
				//MainForm.Current.ReloadXinputSettings();
			}
		}

		private void AutoPresetButton_Click(object sender, EventArgs e)
		{
			var ud = CurrentUserDevice;
			if (ud == null)
				return;
			var description = Attributes.GetDescription(MappedTo);
			var form = new MessageBoxWindow();
			var buttons = System.Windows.MessageBoxButton.YesNo;
			var text = string.Format("Do you want to fill all {0} settings automatically?", description);
			if (ud.Device == null && !TestDeviceHelper.ProductGuid.Equals(ud.ProductGuid))
			{
				text = string.Format("Device is off-line. Please connect device to fill all {0} settings automatically.", description);
				buttons = System.Windows.MessageBoxButton.OK;
			}
			var result = form.ShowDialog(text, "Auto Controller Settings", buttons, System.Windows.MessageBoxImage.Question);
			if (result != System.Windows.MessageBoxResult.Yes)
				return;
			var padSetting = AutoMapHelper.GetAutoPreset(ud);
			// Load created setting.
			SettingsManager.Current.LoadPadSettingsIntoSelectedDevice(MappedTo, padSetting);
		}


		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				_Imager.Dispose();
				MacrosPanel.Dispose();
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void GameControllersButton_Click(object sender, EventArgs e)
		{
			var path = System.Environment.GetFolderPath(Environment.SpecialFolder.System);
			path += "\\joy.cpl";
			ControlsHelper.OpenPath(path);
		}


		private void LoadPresetButton_Click(object sender, EventArgs e)
		{
			ShowPresetForm();
		}

		void ShowPresetForm()
		{
			var form = new Forms.LoadPresetsWindow();
			form.Width = 800;
			form.Height = 400;
			ControlsHelper.CheckTopMost(form);
			form.MainControl.InitForm();
			var result = form.ShowDialog();
			if (result == true)
			{
				var ps = form.MainControl.SelectedItem;
				if (ps != null)
				{
					MainForm.Current.UpdateTimer.Stop();
					SettingsManager.Current.LoadPadSettingsIntoSelectedDevice(MappedTo, ps);
					MainForm.Current.UpdateTimer.Start();
				}
			}
			form.MainControl.UnInitForm();
		}

		#region Mapped Devices

		private void AddMapButton_Click(object sender, EventArgs e)
		{
			var game = SettingsManager.CurrentGame;
			// Return if game is not selected.
			if (game == null)
				return;
			// Show form which allows to select device.
			var selectedUserDevices = MainForm.Current.ShowDeviceForm();
			// Return if no devices were selected.
			if (selectedUserDevices == null)
				return;
			// Check if device already have old settings before adding new ones.
			var noOldSettings = SettingsManager.GetSettings(game.FileName, MappedTo).Count == 0;
			SettingsManager.MapGamePadDevices(game, MappedTo, selectedUserDevices,
				SettingsManager.Options.HidGuardianConfigureAutomatically);
			var hasNewSettings = SettingsManager.GetSettings(game.FileName, MappedTo).Count > 0;
			// If new devices mapped and button is not enabled then...
			if (noOldSettings && hasNewSettings && !EnableButton.Checked)
			{
				// Enable mapping.
				EnableButton_Click(null, null);
			}
			SettingsManager.Current.RaiseSettingsChanged(null);
		}

		private void RemoveMapButton_Click(object sender, EventArgs e)
		{
			var win = new MessageBoxWindow();
			var text = "Do you really want to remove selected user setting?";
			var result = win.ShowDialog(text,
				"X360CE - Remove?", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question, System.Windows.MessageBoxResult.No);
			if (result != System.Windows.MessageBoxResult.Yes)
				return;
			var game = SettingsManager.CurrentGame;
			// Return if game is not selected.
			if (game == null)
				return;
			var settingsOld = SettingsManager.GetSettings(game.FileName, MappedTo);
			var setting = CurrentUserSetting;
			SettingsManager.UnMapGamePadDevices(game, setting,
				SettingsManager.Options.HidGuardianConfigureAutomatically);
			var settingsNew = SettingsManager.GetSettings(game.FileName, MappedTo);
			// if all devices unmapped and mapping is enabled then...
			if (settingsOld.Count > 0 && settingsNew.Count == 0 && EnableButton.Checked)
			{
				// Disable mapping.
				EnableButton_Click(null, null);
			}
		}

		void UpdateGridButtons()
		{
			var grid = MappedDevicesDataGridView;
			var game = SettingsManager.CurrentGame;
			var flag = AppHelper.GetMapFlag(MappedTo);
			var auto = game != null && ((MapToMask)game.AutoMapMask).HasFlag(flag);
			RemoveMapButton.Enabled = !auto && grid.SelectedRows.Count > 0;
			AddMapButton.Enabled = !auto;
		}

		private void MappedDevicesDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;
			var grid = (DataGridView)sender;
			var viewRow = grid.Rows[e.RowIndex];
			var column = grid.Columns[e.ColumnIndex];
			var item = (Engine.Data.UserSetting)viewRow.DataBoundItem;
			if (column == IsOnlineColumn)
			{
				e.Value = item.IsOnline
					? Properties.Resources.bullet_square_glass_green
					: Properties.Resources.bullet_square_glass_grey;
			}
			else if (column == ConnectionClassColumn)
			{
				var device = SettingsManager.GetDevice(item.InstanceGuid);
				e.Value = device.ConnectionClass == Guid.Empty
					? new Bitmap(16, 16)
					: JocysCom.ClassLibrary.IO.DeviceDetector.GetClassIcon(device.ConnectionClass, 16)?.ToBitmap();
			}
			else if (column == InstanceIdColumn)
			{
				// Hide device Instance GUID from public eyes. Show part of checksum.
				e.Value = EngineHelper.GetID(item.InstanceGuid);
			}
			else if (column == SettingIdColumn)
			{
				// Hide device Setting GUID from public eyes. Show part of checksum.
				e.Value = EngineHelper.GetID(item.PadSettingChecksum);
			}
			else if (column == VendorNameColumn)
			{
				var device = SettingsManager.GetDevice(item.InstanceGuid);
				e.Value = device == null
					? ""
					: device.DevManufacturer;
			}
		}

		public event EventHandler<EventArgs<UserSetting>> OnSettingChanged;

		public UserSetting CurrentUserSetting
			=> _CurrentUserSetting;
		private UserSetting _CurrentUserSetting;

		public UserDevice CurrentUserDevice
			=> _CurrentUserDevice;
		private UserDevice _CurrentUserDevice;

		public PadSetting CurrentPadSetting
			=> _CurrentPadSetting;
		private PadSetting _CurrentPadSetting;

		static object selectionLock = new object();

		private void MappedDevicesDataGridView_SelectionChanged(object sender, EventArgs e)
		{
			lock (selectionLock)
			{
				var grid = (DataGridView)sender;
				var row = grid.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault();
				var setting = (UserSetting)row?.DataBoundItem;
				_CurrentUserSetting = setting;
				// Get device attached to user setting.
				_CurrentUserDevice = setting == null
					? new UserDevice()
					: SettingsManager.GetDevice(setting.InstanceGuid);
				// Get mappings attached to user setting.
				_CurrentPadSetting = setting == null
					? new PadSetting()
					: SettingsManager.GetPadSetting(setting.PadSettingChecksum);
				DPadPanel.SetBinding(_CurrentPadSetting);
				GeneralPanel.SetBinding(MappedTo, _CurrentPadSetting);
				AdvancedPanel.SetBinding(_CurrentPadSetting);
				LeftTriggerPanel.SetBinding(_CurrentPadSetting);
				RightTriggerPanel.SetBinding(_CurrentPadSetting);
				LeftThumbXPanel.SetBinding(_CurrentPadSetting);
				LeftThumbYPanel.SetBinding(_CurrentPadSetting);
				RightThumbXPanel.SetBinding(_CurrentPadSetting);
				RightThumbYPanel.SetBinding(_CurrentPadSetting);
				ForceFeedbackPanel.SetBinding(MappedTo, _CurrentPadSetting);
				ForceFeedbackPanel.LeftForceFeedbackMotorPanel.SetBinding(_CurrentPadSetting, 0);
				ForceFeedbackPanel.RightForceFeedbackMotorPanel.SetBinding(_CurrentPadSetting, 1);
				SettingsManager.Current.LoadPadSettingsIntoSelectedDevice(MappedTo, _CurrentPadSetting);
				OnSettingChanged?.Invoke(this, new EventArgs<UserSetting>(setting));
				UpdateGridButtons();
			}
		}

		private void MappedDevicesDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;
			var grid = (DataGridView)sender;
			var column = grid.Columns[e.ColumnIndex];
			// If user clicked on the CheckBox column then...
			if (column == IsEnabledColumn)
			{
				var row = grid.Rows[e.RowIndex];
				var item = (Engine.Data.UserSetting)row.DataBoundItem;
				// Changed check (enabled state) of the current item.
				item.IsEnabled = !item.IsEnabled;
			}
		}

		#endregion

		private void AutoMapButton_Click(object sender, EventArgs e)
		{
			var game = SettingsManager.CurrentGame;
			// If no game selected then ignore click.
			if (game == null)
				return;
			var flag = AppHelper.GetMapFlag(MappedTo);
			var value = (MapToMask)game.AutoMapMask;
			var autoMap = value.HasFlag(flag);
			// If AUTO enabled then...
			if (autoMap)
			{
				// Remove AUTO.
				game.AutoMapMask = (int)(value & ~flag);
			}
			else
			{
				// Add AUTO.
				game.AutoMapMask = (int)(value | flag);
			}
		}

		private void EnableButton_Click(object sender, EventArgs e)
		{
			var game = SettingsManager.CurrentGame;
			// If no game selected then ignore click.
			if (game == null)
				return;
			var flag = AppHelper.GetMapFlag(MappedTo);
			var value = (MapToMask)game.EnableMask;
			var type = game.EmulationType;
			var autoMap = value.HasFlag(flag);
			// Invert flag value.
			var enableMask = autoMap
				// Remove AUTO.
				? (int)(value & ~flag)
				// Add AUTO.	
				: (int)(value | flag);
			// Update emulation type.
			EmulationType? newType = null;
			// If emulation enabled and game is not using virtual type then...
			if (enableMask > 0 && type != (int)EmulationType.Virtual)
				newType = EmulationType.Virtual;
			// If emulation disabled, but game use virtual emulation then...
			if (enableMask == 0 && type == (int)EmulationType.Virtual)
				newType = EmulationType.None;
			// Set values.
			game.EnableMask = enableMask;
			if (newType.HasValue)
				game.EmulationType = (int)newType.Value;
		}

		public void ShowAdvancedTab(bool show)
			=> PadItemPanel.ShowTab(show, PadItemPanel.AdvancedTabPage);

		private void CalibrateButton_Click(object sender, EventArgs e)
		{
			FileInfo fi;
			var error = EngineHelper.ExtractFile("DXTweak2.exe", out fi);
			if (error != null)
			{
				MessageBox.Show(error.Message);
				return;
			}
			ControlsHelper.OpenPath(fi.FullName);
		}

		private void CopyPresetButton_Click(object sender, EventArgs e)
		{
			var text = Serializer.SerializeToXmlString(CurrentPadSetting, null, true);
			Clipboard.SetText(text);
		}

		private void PastePresetButton_Click(object sender, EventArgs e)
		{
			try
			{
				var xml = Clipboard.GetText();
				var ps = JocysCom.ClassLibrary.Runtime.Serializer.DeserializeFromXmlString<PadSetting>(xml);
				SettingsManager.Current.LoadPadSettingsIntoSelectedDevice(MappedTo, ps);
			}
			catch (Exception ex)
			{
				var form = new MessageBoxWindow();
				ControlsHelper.CheckTopMost(form);
				form.ShowDialog(ex.Message);
				return;
			}
		}

		List<SettingsMapItem> RecordAllMaps = new List<SettingsMapItem>();

		public string RemapName = "Remap All";
		public string RemapStopName = "STOP";

		private void RemapAllButton_Click(object sender, EventArgs e)
		{
			// If stop mode then...
			if (GeneralPanel.RemapAllButton.Content as string != RemapName)
			{
				StopRecording();
				return;
			}
			if (!ClearAll())
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

	}
}
