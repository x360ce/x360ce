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
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
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
			AxisToButtonsListPanel = new x360ce.App.Controls.AxisToButtonListControl();
			AxistToButtonsListHost.Child = AxisToButtonsListPanel;
			DirectInputPanel = new PadTabPages.DirectInputUserControl();
			DirectInputHost.Child = DirectInputPanel;
			TriggersWpfPanel = new TriggersControl();
			TriggersHost.Child = TriggersWpfPanel;
			LeftThumbWpfPanel = new LeftThumbControl();
			LeftThumbHost.Child = LeftThumbWpfPanel;
			RightThumbWpfPanel = new RightThumbControl();
			RightThumbHost.Child = RightThumbWpfPanel;
			// Add controls which must be notified on setting selection change.
			UserMacrosPanel.PadControl = this;
			Global.UpdateControlFromStates += Global_UpdateControlFromStates;
			// Hide for this version.
			PadTabControl.TabPages.Remove(XInputTabPage);
			//PadTabControl.TabPages.Remove(MacrosTabPage);

			RemapName = RemapAllButton.Text;
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
			AxisToButtonsListPanel.AxisToButtonADeadZonePanel.MonitorComboBox = ButtonAComboBox;
			AxisToButtonsListPanel.AxisToButtonBDeadZonePanel.MonitorComboBox = ButtonBComboBox;
			AxisToButtonsListPanel.AxisToButtonXDeadZonePanel.MonitorComboBox = ButtonXComboBox;
			AxisToButtonsListPanel.AxisToButtonYDeadZonePanel.MonitorComboBox = ButtonYComboBox;
			AxisToButtonsListPanel.AxisToButtonStartDeadZonePanel.MonitorComboBox = ButtonStartComboBox;
			AxisToButtonsListPanel.AxisToButtonBackDeadZonePanel.MonitorComboBox = ButtonBackComboBox;
			AxisToButtonsListPanel.AxisToLeftShoulderDeadZonePanel.MonitorComboBox = LeftShoulderComboBox;
			AxisToButtonsListPanel.AxisToLeftThumbButtonDeadZonePanel.MonitorComboBox = LeftThumbButtonComboBox;
			AxisToButtonsListPanel.AxisToRightShoulderDeadZonePanel.MonitorComboBox = RightShoulderComboBox;
			AxisToButtonsListPanel.AxisToRightThumbButtonDeadZonePanel.MonitorComboBox = RightThumbButtonComboBox;
			AxisToButtonsListPanel.AxisToDPadDownDeadZonePanel.MonitorComboBox = DPadDownComboBox;
			AxisToButtonsListPanel.AxisToDPadLeftDeadZonePanel.MonitorComboBox = DPadLeftComboBox;
			AxisToButtonsListPanel.AxisToDPadRightDeadZonePanel.MonitorComboBox = DPadRightComboBox;
			AxisToButtonsListPanel.AxisToDPadUpDeadZonePanel.MonitorComboBox = DPadUpComboBox;
			// Load Settings and enable events.
			UpdateGetXInputStatesWithNoEvents();
			// Monitor option changes.
			SettingsManager.OptionsData.Items.ListChanged += Items_ListChanged;
			// Monitor setting changes.
			SettingsManager.Current.SettingChanged += Current_SettingChanged;

		}


		public AxisToButtonListControl AxisToButtonsListPanel;
		public PadTabPages.DirectInputUserControl DirectInputPanel;

		private TriggersControl TriggersWpfPanel;
		private LeftThumbControl LeftThumbWpfPanel;
		private RightThumbControl RightThumbWpfPanel;

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
				ControlsHelper.SetEnabled(RemapAllButton, enable && ud.DiState != null);
				var pages = PadTabControl.TabPages.Cast<TabPage>().ToArray();
				for (int p = 0; p < pages.Length; p++)
				{
					// Get first control to disable which must be Panel.
					var controls = pages[p].Controls.Cast<Control>().ToArray();
					for (int c = 0; c < controls.Length; c++)
						ControlsHelper.SetEnabled(controls[c], enable);
				}
				// If device instance changed then...
				if (!Equals(instanceGuid, _InstanceGuid))
				{
					_InstanceGuid = instanceGuid;
					ResetDiMenuStrip(enable ? ud : null);
				}
				// Update direct input form and return actions (pressed Buttons/DPads, turned Axis/Sliders).
				UpdateDirectInputTabPage(ud);
				DirectInputPanel.UpdateFrom(ud);
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
							RemapAllButton.Text = RemapName;
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
				RemapAllButton.Enabled = false;
			}
			// If device connected then show enabled images.
			if (newConnected && !oldConnected)
			{
				_Imager.SetImages(true);
				RemapAllButton.Enabled = true;
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
			ControlsHelper.SetText(LeftTriggerTextBox, "{0}", newState.Gamepad.LeftTrigger);
			ControlsHelper.SetText(RightTriggerTextBox, "{0}", newState.Gamepad.RightTrigger);
			ControlsHelper.SetText(LeftThumbTextBox, "{0}:{1}", newState.Gamepad.LeftThumbX, newState.Gamepad.LeftThumbY);
			ControlsHelper.SetText(RightThumbTextBox, "{0}:{1}", newState.Gamepad.RightThumbX, newState.Gamepad.RightThumbY);
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
					LeftThumbWpfPanel.LeftThumbXPanel.DrawPoint(axis[map.Index - 1], newState.Gamepad.LeftThumbX, map.IsInverted, map.IsHalf);
				// LeftThumbY
				map = ps.Maps.FirstOrDefault(x => x.Target == TargetType.LeftThumbY);
				if (map != null && map.Index > 0 && map.Index <= axis.Length)
					LeftThumbWpfPanel.LeftThumbYPanel.DrawPoint(axis[map.Index - 1], newState.Gamepad.LeftThumbY, map.IsInverted, map.IsHalf);
				// RightThumbX
				map = ps.Maps.FirstOrDefault(x => x.Target == TargetType.RightThumbX);
				if (map != null && map.Index > 0 && map.Index <= axis.Length)
				 RightThumbWpfPanel.RightThumbXPanel.DrawPoint(axis[map.Index - 1], newState.Gamepad.RightThumbX, map.IsInverted, map.IsHalf);
				// RightThumbY
				map = ps.Maps.FirstOrDefault(x => x.Target == TargetType.RightThumbY);
				if (map != null && map.Index > 0 && map.Index <= axis.Length)
					RightThumbWpfPanel.RightThumbYPanel.DrawPoint(axis[map.Index - 1], newState.Gamepad.RightThumbY, map.IsInverted, map.IsHalf);
				// LeftTrigger
				map = ps.Maps.FirstOrDefault(x => x.Target == TargetType.LeftTrigger);
				if (map != null && map.Index > 0 && map.Index <= axis.Length)
					TriggersWpfPanel.LeftTriggerPanel.DrawPoint(axis[map.Index - 1], newState.Gamepad.LeftTrigger, map.IsInverted, map.IsHalf);
				// RightTrigger
				map = ps.Maps.FirstOrDefault(x => x.Target == TargetType.RightTrigger);
				if (map != null && map.Index > 0 && map.Index <= axis.Length)
					TriggersWpfPanel.RightTriggerPanel.DrawPoint(axis[map.Index - 1], newState.Gamepad.RightTrigger, map.IsInverted, map.IsHalf);
			}
			// Update Axis to Button Images.
			var AxisToButtonControls = ControlsHelper.GetAll<AxisToButtonControl>(AxisToButtonsListPanel.MainGroupBox);
			foreach (var atbPanel in AxisToButtonControls)
				atbPanel.Refresh(newState);
			// Store old state.
			oldConnected = newConnected;
		}

		public bool StopRecording()
		{
			RecordAllMaps.Clear();
			RemapAllButton.Text = RemapName;
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
			if (_CurrentCbx != cbx)
				_CurrentCbx = cbx;
			_Imager.Recorder.StartRecording(map);
			var helpText =
				SettingsConverter.ThumbDirections.Contains(map.Code) ||
				SettingsConverter.TriggerButtonCodes.Contains(map.Code)
					? "Move Axis"
					: "Press Button";
			XboxImage.HelpTextLabel.Content = helpText;
			RemapAllButton.Text = RemapStopName;
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
			// By default send vibration if force enabled/disabled changed.
			var send = e.Item.Control == ForceEnableCheckBox;
			// If force is enabled then...
			if (ForceEnableCheckBox.Checked)
			{
				// List controls which will affect force feedback test.
				var controls = new Control[]
				{
					ForceTypeComboBox,
					ForceOverallTrackBar,
					ForceSwapMotorCheckBox,
					LeftMotorDirectionComboBox,
					LeftMotorPeriodTrackBar,
					LeftMotorStrengthTrackBar,
					RightMotorDirectionComboBox,
					RightMotorPeriodTrackBar,
					RightMotorStrengthTrackBar,
				};
				if (controls.Contains(e.Item.Control))
					send = true;
			}
			if (send)
				SendVibration();
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
			// WORKAROUND: Use BeginInvoke to prevent SelectionChanged firing multiple times.
			ControlsHelper.BeginInvoke(() =>
			{
				MapNameComboBox.DataSource = SettingsManager.Layouts.Items;
				MapNameComboBox.DisplayMember = "Name";
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
			// Add GamePad typed to ComboBox.
			var types = (SharpDX.XInput.DeviceSubType[])Enum.GetValues(typeof(SharpDX.XInput.DeviceSubType));
			foreach (var item in types)
				DeviceSubTypeComboBox.Items.Add(item);
			// Add force feedback typed to ComboBox.
			var effectsTypes = Enum.GetValues(typeof(ForceEffectType)).Cast<ForceEffectType>().Distinct().ToArray();
			foreach (var item in effectsTypes)
				ForceTypeComboBox.Items.Add(item);

			var effectDirections = (ForceEffectDirection[])Enum.GetValues(typeof(ForceEffectDirection));
			foreach (var item in effectDirections)
				LeftMotorDirectionComboBox.Items.Add(item);
			foreach (var item in effectDirections)
				RightMotorDirectionComboBox.Items.Add(item);

			// Add player index to combo boxes
			var playerOptions = new List<KeyValuePair>();
			var playerTypes = (UserIndex[])Enum.GetValues(typeof(UserIndex));
			foreach (var item in playerTypes)
				playerOptions.Add(new KeyValuePair(item.ToString(), ((int)item).ToString()));
			// Attach drop down menu with record and map choices.
			var comboBoxes = new List<ComboBox>();
			GetAllControls(GeneralTabPage, ref comboBoxes);
			// Exclude map name ComboBox
			comboBoxes.Remove(MapNameComboBox);
			// Attach context strip with button names to every ComboBox on general tab.
			foreach (var cb in comboBoxes)
				cb.DropDown += ComboBox_DropDown;
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

		#region Control ComboBox'es

		ComboBox CurrentCbx
		{
			get { return _CurrentCbx; }
			set
			{
				// If changed then...
				if (_CurrentCbx != value)
				{
					// If current exist then remove context menu.
					if (_CurrentCbx != null)
						_CurrentCbx.ContextMenuStrip = null;
					// if new exist then add context menu.
					if (value != null)
						value.ContextMenuStrip = DiMenuStrip;
				}
				_CurrentCbx = value;
			}
		}
		ComboBox _CurrentCbx;

		void ComboBox_DropDown(object sender, EventArgs e)
		{
			var cbx = (ComboBox)sender;
			// Move default DropDown away from the screen.
			var oldLeft = cbx.Left;
			cbx.Left = -10000;
			// If same DropDown clicked then contract.
			if (CurrentCbx == cbx)
			{
				CurrentCbx = null;
			}
			else
			{
				CurrentCbx = cbx;
			}
			ControlsHelper.BeginInvoke(() =>
			{
				ComboBoxDropDown(cbx, oldLeft);
			});
		}

		void ComboBoxDropDown(ComboBox cbx, int oldLeft)
		{
			// Move default DropDown back to the screen.
			cbx.IntegralHeight = !cbx.IntegralHeight;
			cbx.IntegralHeight = !cbx.IntegralHeight;
			cbx.Left = oldLeft;
			var menu = cbx.ContextMenuStrip;
			if (menu != null)
			{
				if (cbx == DPadComboBox)
					EnableDPadMenu(true);
				cbx.ContextMenuStrip.Show(cbx, new Point(0, cbx.Height), ToolStripDropDownDirection.Default);
			}
			if (cbx.Items.Count > 0)
				cbx.SelectedIndex = 0;
		}

		#endregion

		#region Images

		public PadControlImager _Imager;

		ImageInfos imageInfos
		{
			get
			{
				if (_imageInfos == null)
				{

					var triggerLeft = new Point(63, 27);
					var triggerRight = new Point((int)XboxImage.Width - triggerLeft.X - 1, triggerLeft.Y);
					_imageInfos = new ImageInfos();
					// Configure Image 1.
					_imageInfos.Add(1, MapCode.LeftTrigger, 63, 27, LeftTriggerLabel, LeftTriggerComboBox);
					_imageInfos.Add(1, MapCode.RightTrigger, 193, 27, RightTriggerLabel, RightTriggerComboBox);
					_imageInfos.Add(1, MapCode.LeftShoulder, 43, 66, LeftShoulderLabel, LeftShoulderComboBox, GamepadButtonFlags.LeftShoulder);
					_imageInfos.Add(1, MapCode.RightShoulder, 213, 66, RightShoulderLabel, RightShoulderComboBox, GamepadButtonFlags.RightShoulder);
					// Configure Image 2.
					_imageInfos.Add(2, MapCode.ButtonY, 196, 29, ButtonYLabel, ButtonYComboBox, GamepadButtonFlags.Y);
					_imageInfos.Add(2, MapCode.ButtonX, 178, 48, ButtonXLabel, ButtonXComboBox, GamepadButtonFlags.X);
					_imageInfos.Add(2, MapCode.ButtonB, 215, 48, ButtonBLabel, ButtonBComboBox, GamepadButtonFlags.B);
					_imageInfos.Add(2, MapCode.ButtonA, 196, 66, ButtonALabel, ButtonAComboBox, GamepadButtonFlags.A);
					_imageInfos.Add(2, MapCode.ButtonGuide, 127, 48, ButtonGuideLabel, ButtonGuideComboBox);
					_imageInfos.Add(2, MapCode.ButtonBack, 103, 48, ButtonBackLabel, ButtonBackComboBox, GamepadButtonFlags.Back);
					_imageInfos.Add(2, MapCode.ButtonStart, 152, 48, ButtonStartLabel, ButtonStartComboBox, GamepadButtonFlags.Start);
					// D-Pad
					_imageInfos.Add(2, MapCode.DPadUp, 92, 88 - 13, DPadUpLabel, DPadUpComboBox, GamepadButtonFlags.DPadUp);
					_imageInfos.Add(2, MapCode.DPadLeft, 92 - 13, 88, DPadLeftLabel, DPadLeftComboBox, GamepadButtonFlags.DPadLeft);
					_imageInfos.Add(2, MapCode.DPadRight, 92 + 13, 88, DPadRightLabel, DPadRightComboBox, GamepadButtonFlags.DPadRight);
					_imageInfos.Add(2, MapCode.DPadDown, 92, 88 + 13, DPadDownLabel, DPadDownComboBox, GamepadButtonFlags.DPadDown);
					// D-Pad (Extra Map)
					_imageInfos.Add(2, MapCode.DPad, 92, 88, DPadLabel, DPadComboBox);
					// Left Thumb.
					_imageInfos.Add(2, MapCode.LeftThumbButton, 59, 47, LeftThumbButtonLabel, LeftThumbButtonComboBox, GamepadButtonFlags.LeftThumb);
					_imageInfos.Add(2, MapCode.LeftThumbAxisX, 59 + 10, 47, LeftThumbAxisXLabel, LeftThumbAxisXComboBox);
					_imageInfos.Add(2, MapCode.LeftThumbAxisY, 59, 47 - 10, LeftThumbAxisYLabel, LeftThumbAxisYComboBox);
					// Left Thumb (Extra Map).
					_imageInfos.Add(2, MapCode.LeftThumbUp, 59, 47 - 10, LeftThumbUpLabel, LeftThumbUpComboBox);
					_imageInfos.Add(2, MapCode.LeftThumbLeft, 59 - 10, 47, LeftThumbLeftLabel, LeftThumbLeftComboBox);
					_imageInfos.Add(2, MapCode.LeftThumbRight, 59 + 10, 47, LeftThumbRightLabel, LeftThumbRightComboBox);
					_imageInfos.Add(2, MapCode.LeftThumbDown, 59, 47 + 10, LeftThumbDownLabel, LeftThumbDownComboBox);
					// Right Thumb.
					_imageInfos.Add(2, MapCode.RightThumbButton, 160, 88, RightThumbButtonLabel, RightThumbButtonComboBox, GamepadButtonFlags.RightThumb);
					_imageInfos.Add(2, MapCode.RightThumbAxisX, 160 + 10, 88, RightThumbAxisXLabel, RightThumbAxisXComboBox);
					_imageInfos.Add(2, MapCode.RightThumbAxisY, 160, 88 - 10, RightThumbAxisYLabel, RightThumbAxisYComboBox);
					// Right Thumb (Extra Map).
					_imageInfos.Add(2, MapCode.RightThumbUp, 160, 88 - 10, RightThumbUpLabel, RightThumbUpComboBox);
					_imageInfos.Add(2, MapCode.RightThumbLeft, 160 - 10, 88, RightThumbLeftLabel, RightThumbLeftComboBox);
					_imageInfos.Add(2, MapCode.RightThumbRight, 160 + 10, 88, RightThumbRightLabel, RightThumbRightComboBox);
					_imageInfos.Add(2, MapCode.RightThumbDown, 160, 88 + 10, RightThumbDownLabel, RightThumbDownComboBox);
				}
				return _imageInfos;
			}
		}
		ImageInfos _imageInfos;

		#endregion

		#region Settings Map

		public MapTo MappedTo;

		/// <summary>
		/// Link control with INI key. Value/Text of control will be automatically tracked and INI file updated.
		/// </summary>
		public void UpdateSettingsMap()
		{
			//// FakeAPI
			//AddMap(() => SettingName.ProductName, DirectInputPanel.DeviceProductNameTextBox);
			//AddMap(() => SettingName.ProductGuid, DirectInputPanel.DeviceProductGuidTextBox);
			//AddMap(() => SettingName.InstanceGuid, DirectInputPanel.DeviceInstanceGuidTextBox);
			// Mapping
			//AddMap(() => SettingName.MapToPad, DirectInputPanel.MapToPadComboBox);

			AddMap(() => SettingName.GamePadType, DeviceSubTypeComboBox);
			AddMap(() => SettingName.PassThrough, PassThroughCheckBox);
			AddMap(() => SettingName.ForcesPassThrough, ForceFeedbackPassThroughCheckBox);

			// Left Trigger
			AddMap(() => SettingName.LeftTrigger, LeftTriggerComboBox, MapCode.LeftTrigger);

			// Right Trigger
			AddMap(() => SettingName.RightTrigger, RightTriggerComboBox, MapCode.RightTrigger);
			//AddMap(() => SettingName.RightTriggerDeadZone, RightTriggerUserControl.DeadZoneTrackBar);
			//AddMap(() => SettingName.RightTriggerAntiDeadZone, RightTriggerUserControl.AntiDeadZoneNumericUpDown);
			//AddMap(() => SettingName.RightTriggerLinear, RightTriggerUserControl.SensitivityNumericUpDown);

			// D-Pad
			AddMap(() => SettingName.DPad, DPadComboBox, MapCode.DPad);
			AddMap(() => SettingName.DPadUp, DPadUpComboBox, MapCode.DPadUp);
			AddMap(() => SettingName.DPadDown, DPadDownComboBox, MapCode.DPadDown);
			AddMap(() => SettingName.DPadLeft, DPadLeftComboBox, MapCode.DPadLeft);
			AddMap(() => SettingName.DPadRight, DPadRightComboBox, MapCode.DPadRight);

			// Axis To Button
			AddMap(() => SettingName.ButtonADeadZone, new NumericUpDown());
			AddMap(() => SettingName.ButtonBDeadZone, new NumericUpDown());
			AddMap(() => SettingName.ButtonXDeadZone, new NumericUpDown());
			AddMap(() => SettingName.ButtonYDeadZone, new NumericUpDown());
			AddMap(() => SettingName.ButtonStartDeadZone, new NumericUpDown());
			AddMap(() => SettingName.ButtonBackDeadZone, new NumericUpDown());
			AddMap(() => SettingName.LeftShoulderDeadZone, new NumericUpDown());
			AddMap(() => SettingName.LeftThumbButtonDeadZone, new NumericUpDown());
			AddMap(() => SettingName.RightShoulderDeadZone, new NumericUpDown());
			AddMap(() => SettingName.RightThumbButtonDeadZone, new NumericUpDown());
			// Axis To D-Pad (separate directions).
			AddMap(() => SettingName.DPadDownDeadZone, new NumericUpDown());
			AddMap(() => SettingName.DPadLeftDeadZone, new NumericUpDown());
			AddMap(() => SettingName.DPadRightDeadZone, new NumericUpDown());
			AddMap(() => SettingName.DPadUpDeadZone, new NumericUpDown());



			//// Axis To Button
			//AddMap(() => SettingName.ButtonADeadZone, AxisToButtonsListPanel.AxisToButtonADeadZonePanel.DeadZoneNumericUpDown);
			//AddMap(() => SettingName.ButtonBDeadZone, AxisToButtonsListPanel.AxisToButtonBDeadZonePanel.DeadZoneNumericUpDown);
			//AddMap(() => SettingName.ButtonXDeadZone, AxisToButtonsListPanel.AxisToButtonXDeadZonePanel.DeadZoneNumericUpDown);
			//AddMap(() => SettingName.ButtonYDeadZone, AxisToButtonsListPanel.AxisToButtonYDeadZonePanel.DeadZoneNumericUpDown);
			//AddMap(() => SettingName.ButtonStartDeadZone, AxisToButtonsListPanel.AxisToButtonStartDeadZonePanel.DeadZoneNumericUpDown);
			//AddMap(() => SettingName.ButtonBackDeadZone, AxisToButtonsListPanel.AxisToButtonBackDeadZonePanel.DeadZoneNumericUpDown);
			//AddMap(() => SettingName.LeftShoulderDeadZone, AxisToButtonsListPanel.AxisToLeftShoulderDeadZonePanel.DeadZoneNumericUpDown);
			//AddMap(() => SettingName.LeftThumbButtonDeadZone, AxisToButtonsListPanel.AxisToLeftThumbButtonDeadZonePanel.DeadZoneNumericUpDown);
			//AddMap(() => SettingName.RightShoulderDeadZone, AxisToButtonsListPanel.AxisToRightShoulderDeadZonePanel.DeadZoneNumericUpDown);
			//AddMap(() => SettingName.RightThumbButtonDeadZone, AxisToButtonsListPanel.AxisToRightThumbButtonDeadZonePanel.DeadZoneNumericUpDown);
			//// Axis To D-Pad (separate directions).
			//AddMap(() => SettingName.DPadDownDeadZone, AxisToButtonsListPanel.AxisToDPadDownDeadZonePanel.DeadZoneNumericUpDown);
			//AddMap(() => SettingName.DPadLeftDeadZone, AxisToButtonsListPanel.AxisToDPadLeftDeadZonePanel.DeadZoneNumericUpDown);
			//AddMap(() => SettingName.DPadRightDeadZone, AxisToButtonsListPanel.AxisToDPadRightDeadZonePanel.DeadZoneNumericUpDown);
			//AddMap(() => SettingName.DPadUpDeadZone, AxisToButtonsListPanel.AxisToDPadUpDeadZonePanel.DeadZoneNumericUpDown);
			// Axis To D-Pad.
			AddMap(() => SettingName.AxisToDPadEnabled, AxisToDPadEnabledCheckBox);
			AddMap(() => SettingName.AxisToDPadDeadZone, AxisToDPadDeadZoneTrackBar);
			AddMap(() => SettingName.AxisToDPadOffset, AxisToDPadOffsetTrackBar);
			// Buttons
			AddMap(() => SettingName.ButtonGuide, ButtonGuideComboBox, MapCode.ButtonGuide);
			AddMap(() => SettingName.ButtonBack, ButtonBackComboBox, MapCode.ButtonBack);
			AddMap(() => SettingName.ButtonStart, ButtonStartComboBox, MapCode.ButtonStart);
			AddMap(() => SettingName.ButtonA, ButtonAComboBox, MapCode.ButtonA);
			AddMap(() => SettingName.ButtonB, ButtonBComboBox, MapCode.ButtonB);
			AddMap(() => SettingName.ButtonX, ButtonXComboBox, MapCode.ButtonX);
			AddMap(() => SettingName.ButtonY, ButtonYComboBox, MapCode.ButtonY);
			// Shoulders.
			AddMap(() => SettingName.LeftShoulder, LeftShoulderComboBox, MapCode.LeftShoulder);
			AddMap(() => SettingName.RightShoulder, RightShoulderComboBox, MapCode.RightShoulder);
			// Left Thumb
			AddMap(() => SettingName.LeftThumbAxisX, LeftThumbAxisXComboBox, MapCode.LeftThumbAxisX);
			AddMap(() => SettingName.LeftThumbAxisY, LeftThumbAxisYComboBox, MapCode.LeftThumbAxisY);
			AddMap(() => SettingName.LeftThumbRight, LeftThumbRightComboBox, MapCode.LeftThumbRight);
			AddMap(() => SettingName.LeftThumbLeft, LeftThumbLeftComboBox, MapCode.LeftThumbLeft);
			AddMap(() => SettingName.LeftThumbUp, LeftThumbUpComboBox, MapCode.LeftThumbUp);
			AddMap(() => SettingName.LeftThumbDown, LeftThumbDownComboBox, MapCode.LeftThumbDown);
			AddMap(() => SettingName.LeftThumbButton, LeftThumbButtonComboBox, MapCode.LeftThumbButton);
			//AddMap(() => SettingName.LeftThumbDeadZoneX, LeftThumbXUserControl.DeadZoneTrackBar);
			//AddMap(() => SettingName.LeftThumbDeadZoneY, LeftThumbYUserControl.DeadZoneTrackBar);
			//AddMap(() => SettingName.LeftThumbAntiDeadZoneX, LeftThumbXUserControl.AntiDeadZoneNumericUpDown);
			//AddMap(() => SettingName.LeftThumbAntiDeadZoneY, LeftThumbYUserControl.AntiDeadZoneNumericUpDown);
			//AddMap(() => SettingName.LeftThumbLinearX, LeftThumbXUserControl.SensitivityNumericUpDown);
			//AddMap(() => SettingName.LeftThumbLinearY, LeftThumbYUserControl.SensitivityNumericUpDown);
			// Right Thumb
			AddMap(() => SettingName.RightThumbAxisX, RightThumbAxisXComboBox, MapCode.RightThumbAxisX);
			AddMap(() => SettingName.RightThumbAxisY, RightThumbAxisYComboBox, MapCode.RightThumbAxisY);
			AddMap(() => SettingName.RightThumbRight, RightThumbRightComboBox, MapCode.RightThumbRight);
			AddMap(() => SettingName.RightThumbLeft, RightThumbLeftComboBox, MapCode.RightThumbLeft);
			AddMap(() => SettingName.RightThumbUp, RightThumbUpComboBox, MapCode.RightThumbUp);
			AddMap(() => SettingName.RightThumbDown, RightThumbDownComboBox, MapCode.RightThumbDown);
			AddMap(() => SettingName.RightThumbButton, RightThumbButtonComboBox, MapCode.RightThumbButton);
			//AddMap(() => SettingName.RightThumbDeadZoneX, RightThumbXUserControl.DeadZoneTrackBar);
			//AddMap(() => SettingName.RightThumbDeadZoneY, RightThumbYUserControl.DeadZoneTrackBar);
			//AddMap(() => SettingName.RightThumbAntiDeadZoneX, RightThumbXUserControl.AntiDeadZoneNumericUpDown);
			//AddMap(() => SettingName.RightThumbAntiDeadZoneY, RightThumbYUserControl.AntiDeadZoneNumericUpDown);
			//AddMap(() => SettingName.RightThumbLinearX, RightThumbXUserControl.SensitivityNumericUpDown);
			//AddMap(() => SettingName.RightThumbLinearY, RightThumbYUserControl.SensitivityNumericUpDown);
			// Force Feedback
			AddMap(() => SettingName.ForceEnable, ForceEnableCheckBox);
			AddMap(() => SettingName.ForceType, ForceTypeComboBox);
			AddMap(() => SettingName.ForceSwapMotor, ForceSwapMotorCheckBox);
			AddMap(() => SettingName.ForceOverall, ForceOverallTrackBar);
			AddMap(() => SettingName.LeftMotorDirection, LeftMotorDirectionComboBox);
			AddMap(() => SettingName.LeftMotorStrength, LeftMotorStrengthTrackBar);
			AddMap(() => SettingName.LeftMotorPeriod, LeftMotorPeriodTrackBar);
			AddMap(() => SettingName.RightMotorDirection, RightMotorDirectionComboBox);
			AddMap(() => SettingName.RightMotorStrength, RightMotorStrengthTrackBar);
			AddMap(() => SettingName.RightMotorPeriod, RightMotorPeriodTrackBar);
		}

		void AddMap<T>(Expression<Func<T>> setting, Control control, MapCode code = default)
			=> SettingsManager.AddMap(setting, control, MappedTo, code);

		#endregion

		//XINPUT_GAMEPAD GamePad;
		Guid _InstanceGuid;

		private void UpdatePassThroughRelatedControls()
		{
			// Is Pass Through enabled?
			bool fullPassThrough = PassThroughCheckBox.Checked;
			bool forcesPassThrough = ForceFeedbackPassThroughCheckBox.Checked;
			// If full pass-through mode is turned on, changing forces pass-through has no effect.
			ForceFeedbackPassThroughCheckBox.Enabled = !fullPassThrough;
		}

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
			ControlsHelper.SetText(DirectInputTabPage, text);
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

		string cRecord = "[Record]";
		string cEmpty = "<empty>";
		string cPOVs = "POVs";

		// Function is recreated as soon as new DirectInput Device is available.
		public void ResetDiMenuStrip(UserDevice ud)
		{
			DiMenuStrip.Items.Clear();
			ToolStripMenuItem mi;
			mi = new ToolStripMenuItem(cEmpty);
			mi.ForeColor = SystemColors.ControlDarkDark;
			mi.Click += new EventHandler(DiMenuStrip_Click);
			DiMenuStrip.Items.Add(mi);
			// Return if direct input device is not available.
			if (ud == null)
				return;
			// Add [Record] button.
			mi = new ToolStripMenuItem(cRecord);
			mi.Image = new Bitmap(EngineHelper.GetResourceStream("Images.bullet_ball_glass_red_16x16.png"));
			mi.Click += new EventHandler(DiMenuStrip_Click);
			DiMenuStrip.Items.Add(mi);
			// Do not add menu items for keyboard, because user interface will become too sluggish.
			// Recording feature is preferred way to map keyboard button.
			if (!ud.IsKeyboard)
			{
				// Add Buttons.
				mi = new ToolStripMenuItem("Buttons");
				DiMenuStrip.Items.Add(mi);
				CreateItems(mi, "Inverted", "IButton {0}", "-{0}", ud.CapButtonCount);
				CreateItems(mi, "Button {0}", "{0}", ud.CapButtonCount);
				if (ud.DiAxeMask > 0)
				{
					// Add Axes.
					mi = new ToolStripMenuItem("Axes");
					DiMenuStrip.Items.Add(mi);
					CreateItems(mi, "Inverted", "IAxis {0}", "a-{0}", CustomDiState.MaxAxis, ud.DiAxeMask);
					CreateItems(mi, "Inverted Half", "IHAxis {0}", "x-{0}", CustomDiState.MaxAxis, ud.DiAxeMask);
					CreateItems(mi, "Half", "HAxis {0}", "x{0}", CustomDiState.MaxAxis, ud.DiAxeMask);
					CreateItems(mi, "Axis {0}", "a{0}", CustomDiState.MaxAxis, ud.DiAxeMask);
				}
				if (ud.DiSliderMask > 0)
				{
					// Add Sliders.            
					mi = new ToolStripMenuItem("Sliders");
					DiMenuStrip.Items.Add(mi);
					// 2 x Sliders, 2 x AccelerationSliders, 2 x state.ForceSliders, 2 x VelocitySliders
					CreateItems(mi, "Inverted", "ISlider {0}", "s-{0}", CustomDiState.MaxSliders, ud.DiSliderMask);
					CreateItems(mi, "Inverted Half", "IHSlider {0}", "h-{0}", CustomDiState.MaxSliders, ud.DiSliderMask);
					CreateItems(mi, "Half", "HSlider {0}", "h{0}", CustomDiState.MaxSliders, ud.DiSliderMask);
					CreateItems(mi, "Slider {0}", "s{0}", CustomDiState.MaxSliders, ud.DiSliderMask);
				}
				// Add D-Pads.
				if (ud.CapPovCount > 0)
				{
					mi = new ToolStripMenuItem(cPOVs);
					DiMenuStrip.Items.Add(mi);
					// Add D-Pad Top, Right, Bottom, Left button.
					var dPadNames = Enum.GetNames(typeof(DPadEnum));
					for (int p = 0; p < ud.CapPovCount; p++)
					{
						var dPadItem = CreateItem("POV {0}", "{1}{0}", p + 1, SettingName.SType.POV);
						mi.DropDownItems.Add(dPadItem);
						for (int d = 0; d < dPadNames.Length; d++)
						{
							var dPadButtonIndex = p * 4 + d + 1;
							var dPadButtonItem = CreateItem("POV {0} {1}", "{2}{3}", p + 1, dPadNames[d], SettingName.SType.POVButton, dPadButtonIndex);
							dPadItem.DropDownItems.Add(dPadButtonItem);
						}
					}
				}
			}
		}

		void CreateItems(ToolStripMenuItem parent, string subMenu, string text, string tag, int count, int? mask = null)
		{
			var smi = new ToolStripMenuItem(subMenu);
			parent.DropDownItems.Add(smi);
			CreateItems(smi, text, tag, count, mask);
		}

		/// <summary>Create menu item.</summary>
		/// <param name="mask">Mask contains information if item is present.</param>
		void CreateItems(ToolStripMenuItem parent, string text, string tag, int count, int? mask = null)
		{
			var items = new List<ToolStripMenuItem>();
			for (int i = 0; i < count; i++)
			{
				// If mask specified and item is not present then...
				if (mask.HasValue && i < 32 && (((int)Math.Pow(2, i) & mask) == 0))
					continue;
				var item = CreateItem(text, tag, i + 1);
				items.Add(item);
			}
			parent.DropDownItems.AddRange(items.ToArray());
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


		void DiMenuStrip_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			EnableDPadMenu(false);
		}

		void DiMenuStrip_Click(object sender, EventArgs e)
		{
			ToolStripMenuItem item = (ToolStripMenuItem)sender;
			Regex rx = new Regex("^(DPad [0-9]+)$");
			// If this DPad parent menu.
			if (rx.IsMatch(item.Text))
			{
				if (CurrentCbx == DPadComboBox)
				{
					SettingsManager.Current.SetComboBoxValue(CurrentCbx, item.Text);
					CurrentCbx = null;
					//DiMenuStrip.Close();
				}
			}
			else
			{
				if (item.Text == cRecord)
				{
					var map = SettingsManager.Current.SettingsMap.First(x => x.Control == CurrentCbx);
					StartRecording(map);
				}
				else if (item.Text == cEmpty)
				{
					SettingsManager.Current.SetComboBoxValue(CurrentCbx, string.Empty);
					CurrentCbx = null;
				}
				else
				{
					SettingsManager.Current.SetComboBoxValue(CurrentCbx, item.Text);
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
					&& !item.Text.StartsWith(cPOVs))
				{
					item.Visible = !enable;
				}
				if (item.Text.StartsWith(cPOVs))
				{
					if (item.HasDropDownItems)
					{
						foreach (ToolStripMenuItem l1 in item.DropDownItems)
						{
							foreach (ToolStripMenuItem l2 in l1.DropDownItems)
								l2.Visible = !enable;
						}
					}
				}
			}
		}

		void ForceOverallTrackBar_ValueChanged(object sender, EventArgs e)
		{
			TrackBar control = (TrackBar)sender;
			ForceOverallTextBox.Text = string.Format("{0} % ", control.Value);
		}

		void MotorTrackBar_ValueChanged(object sender, EventArgs e)
		{
			//if (gamePadState == null) return;
			UpdateForceFeedBack();
		}

		void MotorPeriodTrackBar_ValueChanged(object sender, EventArgs e)
		{
			// Convert Direct Input Period force feedback effect parameter value.
			int leftMotorPeriod = LeftMotorPeriodTrackBar.Value * 5;
			int rightMotorPeriod = RightMotorPeriodTrackBar.Value * 5;
			LeftMotorPeriodTextBox.Text = string.Format("{0} ", leftMotorPeriod);
			RightMotorPeriodTextBox.Text = string.Format("{0} ", rightMotorPeriod);
		}

		public void UpdateForceFeedBack()
		{
			if (MainForm.Current.ControllerIndex == -1)
				return;
			LeftMotorTestTextBox.Text = string.Format("{0} % ", LeftMotorTestTrackBar.Value);
			RightMotorTestTextBox.Text = string.Format("{0} % ", RightMotorTestTrackBar.Value);
			SendVibration();
			//UnsafeNativeMethods.Enable(false);
			//UnsafeNativeMethods.Enable(true);
		}

		void SendVibration()
		{
			var index = (int)MappedTo - 1;
			var game = SettingsManager.CurrentGame;
			var isVirtual = ((EmulationType)game.EmulationType).HasFlag(EmulationType.Virtual);
			if (isVirtual)
			{
				var largeMotor = (byte)ConvertHelper.ConvertRange(0, 100, byte.MinValue, byte.MaxValue, LeftMotorTestTrackBar.Value);
				var smallMotor = (byte)ConvertHelper.ConvertRange(0, 100, byte.MinValue, byte.MaxValue, RightMotorTestTrackBar.Value);
				Global.DHelper.SetVibration(MappedTo, largeMotor, smallMotor, 0);
			}
			else
			{
				lock (Controller.XInputLock)
				{
					// Convert 100% TrackBar to MotorSpeed's 0 - 65,535 (100%).
					var leftMotor = (short)ConvertHelper.ConvertRange(0, 100, short.MinValue, short.MaxValue, LeftMotorTestTrackBar.Value);
					var rightMotor = (short)ConvertHelper.ConvertRange(0, 100, short.MinValue, short.MaxValue, RightMotorTestTrackBar.Value);
					var gamePad = Global.DHelper.LiveXiControllers[index];
					var isConnected = Global.DHelper.LiveXiConnected[index];
					if (Controller.IsLoaded && isConnected)
					{
						var vibration = new Vibration();
						vibration.LeftMotorSpeed = leftMotor;
						vibration.RightMotorSpeed = rightMotor;
						gamePad.SetVibration(vibration);
					}
				}
			}
		}

		void AxisToDPadOffsetTrackBar_ValueChanged(object sender, EventArgs e)
		{
			TrackBar control = (TrackBar)sender;
			AxisToDPadOffsetTextBox.Text = string.Format("{0} % ", control.Value);
		}

		void AxisToDPadDeadZoneTrackBar_ValueChanged(object sender, EventArgs e)
		{
			TrackBar control = (TrackBar)sender;
			AxisToDPadDeadZoneTextBox.Text = string.Format("{0} % ", control.Value);
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
				UserMacrosPanel.Dispose();
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void LeftMotorStrengthTrackBar_ValueChanged(object sender, EventArgs e)
		{
			var control = (TrackBar)sender;
			LeftMotorStrengthTextBox.Text = string.Format("{0} % ", control.Value);
		}

		private void RightMotorStrengthTrackBar_ValueChanged(object sender, EventArgs e)
		{
			var control = (TrackBar)sender;
			RightMotorStrengthTextBox.Text = string.Format("{0} % ", control.Value);
		}

		private void PassThroughCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			UpdatePassThroughRelatedControls();
		}

		private void ForcesPassThroughCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			UpdatePassThroughRelatedControls();
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
				TriggersWpfPanel.LeftTriggerPanel.SetBinding(_CurrentPadSetting);
				TriggersWpfPanel.RightTriggerPanel.SetBinding(_CurrentPadSetting);
				LeftThumbWpfPanel.LeftThumbXPanel.SetBinding(_CurrentPadSetting);
				LeftThumbWpfPanel.LeftThumbYPanel.SetBinding(_CurrentPadSetting);
				RightThumbWpfPanel.RightThumbXPanel.SetBinding(_CurrentPadSetting);
				RightThumbWpfPanel.RightThumbYPanel.SetBinding(_CurrentPadSetting);
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
		{
			ShowTab(show, AdvancedTabPage);
		}

		void ShowTab(bool show, TabPage page)
		{
			var tc = PadTabControl;
			// If must hide then...
			if (!show && tc.TabPages.Contains(page))
			{
				// Hide and return.
				tc.TabPages.Remove(page);
				return;
			}
			// If must show then..
			if (show && !tc.TabPages.Contains(page))
			{
				// Create list of tabs to maintain same order when hiding and showing tabs.
				var tabs = new List<TabPage>() { AdvancedTabPage };
				// Get index of always displayed tab.
				var index = tc.TabPages.IndexOf(GeneralTabPage);
				// Get tabs in front of tab which must be inserted.
				var tabsBefore = tabs.Where(x => tabs.IndexOf(x) < tabs.IndexOf(page));
				// Count visible tabs.
				var countBefore = tabsBefore.Count(x => tc.TabPages.Contains(x));
				tc.TabPages.Insert(index + countBefore + 1, page);
			}
		}

		private void MapNameComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			var item = (Layout)MapNameComboBox.SelectedItem;
			ButtonALabel.Text = item.ButtonA;
			ButtonBLabel.Text = item.ButtonB;
			ButtonBackLabel.Text = item.ButtonBack;
			ButtonGuideLabel.Text = item.ButtonGuide;
			ButtonStartLabel.Text = item.ButtonStart;
			ButtonXLabel.Text = item.ButtonX;
			ButtonYLabel.Text = item.ButtonY;
			DPadLabel.Text = item.DPad;
			DPadDownLabel.Text = item.DPadDown;
			DPadLeftLabel.Text = item.DPadLeft;
			DPadRightLabel.Text = item.DPadRight;
			DPadUpLabel.Text = item.DPadUp;
			LeftShoulderLabel.Text = item.LeftShoulder;
			LeftThumbAxisXLabel.Text = item.LeftThumbAxisX;
			LeftThumbAxisYLabel.Text = item.LeftThumbAxisY;
			LeftThumbButtonLabel.Text = item.LeftThumbButton;
			LeftThumbDownLabel.Text = item.LeftThumbDown;
			LeftThumbLeftLabel.Text = item.LeftThumbLeft;
			LeftThumbRightLabel.Text = item.LeftThumbRight;
			LeftThumbUpLabel.Text = item.LeftThumbUp;
			LeftTriggerLabel.Text = item.LeftTrigger;
			RightShoulderLabel.Text = item.RightShoulder;
			RightThumbAxisXLabel.Text = item.RightThumbAxisX;
			RightThumbAxisYLabel.Text = item.RightThumbAxisY;
			RightThumbButtonLabel.Text = item.RightThumbButton;
			RightThumbDownLabel.Text = item.RightThumbDown;
			RightThumbLeftLabel.Text = item.RightThumbLeft;
			RightThumbRightLabel.Text = item.RightThumbRight;
			RightThumbUpLabel.Text = item.RightThumbUp;
			RightTriggerLabel.Text = item.RightTrigger;
		}

		private void ForceTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			var type = (ForceEffectType)ForceTypeComboBox.SelectedItem;
			var list = new List<string>();
			if (type == ForceEffectType.Constant || type == ForceEffectType._Type2)
				list.Add("Constant force type. Good for vibrating motors on game pads.");
			if (type.HasFlag(ForceEffectType.PeriodicSine))
				list.Add("Periodic 'Sine Wave' force type. Good for car/plane engine vibration. Good for torque motors on wheels.");
			if (type.HasFlag(ForceEffectType.PeriodicSawtooth))
				list.Add("Periodic 'Sawtooth Down Wave' force type. Good for gun recoil. Good for torque motors on wheels.");
			if (type.HasFlag(ForceEffectType._Type2))
				list.Add("Alternative implementation - two motors / actuators per effect.");
			EffectDescriptionLabel.Text = string.Format("{0} ({1}) - {2}", type, (int)type, string.Join(" ", list));
		}

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
			if (RemapAllButton.Text != RemapName)
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

		private void AxisToButtonYDeadZonePanel_Load(object sender, EventArgs e)
		{

		}

	}
}
