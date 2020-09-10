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
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	public partial class PadControl : UserControl
	{

		public PadControl(MapTo controllerIndex)
		{
			InitializeComponent();
			if (ControlsHelper.IsDesignMode(this))
				return;
			// Make font more consistent with the rest of the interface.
			Controls.OfType<ToolStrip>().ToList().ForEach(x => x.Font = Font);
			// Hide left/right border.
			//MappedDevicesDataGridView.Width = this.Width + 2;
			//MappedDevicesDataGridView.Left = -1;
			JocysCom.ClassLibrary.Controls.ControlsHelper.ApplyBorderStyle(MappedDevicesDataGridView);
			MappedTo = controllerIndex;
			// Axis to Button DeadZones
			AxisToButtonADeadZonePanel.MonitorComboBox = ButtonAComboBox;
			AxisToButtonBDeadZonePanel.MonitorComboBox = ButtonBComboBox;
			AxisToButtonXDeadZonePanel.MonitorComboBox = ButtonXComboBox;
			AxisToButtonYDeadZonePanel.MonitorComboBox = ButtonYComboBox;
			AxisToButtonStartDeadZonePanel.MonitorComboBox = ButtonStartComboBox;
			AxisToButtonBackDeadZonePanel.MonitorComboBox = ButtonBackComboBox;
			AxisToLeftShoulderDeadZonePanel.MonitorComboBox = LeftShoulderComboBox;
			AxisToLeftThumbButtonDeadZonePanel.MonitorComboBox = LeftThumbButtonComboBox;
			AxisToRightShoulderDeadZonePanel.MonitorComboBox = RightShoulderComboBox;
			AxisToRightThumbButtonDeadZonePanel.MonitorComboBox = RightThumbButtonComboBox;
			AxisToDPadDownDeadZonePanel.MonitorComboBox = DPadDownComboBox;
			AxisToDPadLeftDeadZonePanel.MonitorComboBox = DPadLeftComboBox;
			AxisToDPadRightDeadZonePanel.MonitorComboBox = DPadRightComboBox;
			AxisToDPadUpDeadZonePanel.MonitorComboBox = DPadUpComboBox;
			// Load Settings and enable events.
			UpdateGetXInputStatesWithNoEvents();
			// Monitor option changes.
			SettingsManager.OptionsData.Items.ListChanged += Items_ListChanged;
			// Monitor setting changes.
			SettingsManager.Current.SettingChanged += Current_SettingChanged;

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

		public Recorder _recorder;

		public void InitPadControl()
		{
			var dv = new System.Data.DataView();
			var grid = MappedDevicesDataGridView;
			grid.AutoGenerateColumns = false;
			// Initialize images.
			this.TopPictureBox.Image = topDisabledImage;
			this.FrontPictureBox.Image = frontDisabledImage;
			this.markB = new Bitmap(EngineHelper.GetResourceStream("Images.MarkButton.png"));
			this.markA = new Bitmap(EngineHelper.GetResourceStream("Images.MarkAxis.png"));
			this.markC = new Bitmap(EngineHelper.GetResourceStream("Images.MarkController.png"));
			float rH = topDisabledImage.HorizontalResolution;
			float rV = topDisabledImage.VerticalResolution;
			// Make sure resolution is same everywhere so images won't be resized.
			this.markB.SetResolution(rH, rV);
			this.markA.SetResolution(rH, rV);
			this.markC.SetResolution(rH, rV);
			_recorder = new Recorder(this.components, rH, rV);


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
			PassThroughIndexComboBox.DataSource = new BindingSource(playerOptions, null); // Otherwise changing one changes the other
			PassThroughIndexComboBox.DisplayMember = "Key";
			PassThroughIndexComboBox.ValueMember = "Value";
			CombinedIndexComboBox.DataSource = new BindingSource(playerOptions, null);  // Otherwise changing one changes the other
			CombinedIndexComboBox.DisplayMember = "Key";
			CombinedIndexComboBox.ValueMember = "Value";
			// Attach drop down menu with record and map choices.
			var comboBoxes = new List<ComboBox>();
			GetAllControls(GeneralTabPage, ref comboBoxes);
			// Exclude map name combobox
			comboBoxes.Remove(MapNameComboBox);
			// Attach context strip with button names to every ComboBox on general tab.
			foreach (var cb in comboBoxes)
				cb.DropDown += ComboBox_DropDown;
			UpdateFromCurrentGame();
		}

		public void UpdateFromCurrentGame()
		{
			var game = MainForm.Current.CurrentGame;
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
				var game = MainForm.Current.CurrentGame;
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
						: JocysCom.ClassLibrary.Controls.ControlsHelper.GetSelection<Guid>(grid, "InstanceGuid");
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
					JocysCom.ClassLibrary.Controls.ControlsHelper.RestoreSelection(grid, "InstanceGuid", selection);
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

		void PadControl_Load(object sender, EventArgs e)
		{
			//LeftThumbXAntiDeadZoneComboBox.SelectedIndex = 0;
			//LeftThumbYAntiDeadZoneComboBox.SelectedIndex = 0;
			//RightThumbXAntiDeadZoneComboBox.SelectedIndex = 0;
			//RightThumbYAntiDeadZoneComboBox.SelectedIndex = 0;
		}

		void ComboBox_DropDown(object sender, EventArgs e)
		{
			var cbx = (ComboBox)sender;
			// Move default DropDown away from the screen.
			var oldLeft = cbx.Left;
			cbx.Left = -10000;
			// If same dropwon clicked then contract.
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

		Bitmap markB;
		Bitmap markA;
		Bitmap markC;

		Bitmap topImage
		{
			get { return _topImage = _topImage ?? new Bitmap(EngineHelper.GetResourceStream("Images.xboxControllerTop.png")); }
		}
		Bitmap _topImage;

		Bitmap frontImage
		{
			get { return _frontImage = _frontImage ?? new Bitmap(EngineHelper.GetResourceStream("Images.xboxControllerFront.png")); }
		}
		Bitmap _frontImage;

		Bitmap _topDisabledImage;
		Bitmap topDisabledImage
		{
			get
			{
				if (_topDisabledImage == null)
				{
					_topDisabledImage = AppHelper.GetDisabledImage(topImage);
				}
				return _topDisabledImage;
			}
		}

		Bitmap _frontDisabledImage;
		Bitmap frontDisabledImage
		{
			get
			{
				if (_frontDisabledImage == null)
				{
					_frontDisabledImage = AppHelper.GetDisabledImage(frontImage);
				}
				return _frontDisabledImage;
			}
		}

		void TopPictureBox_Paint(object sender, PaintEventArgs e)
		{
			// Display controller.
			bool on = newConnected;
			if (!on)
				return;
			// Half mark position adjust.
			int mW = -this.markB.Width / 2;
			int mH = -this.markB.Height / 2;
			// Button coordinates.
			var shoulderLeft = new Point(43, 66);
			var shoulderRight = new Point(this.FrontPictureBox.Width - shoulderLeft.X, shoulderLeft.Y);
			var triggerLeft = new Point(63, 27);
			var triggerRight = new Point(this.FrontPictureBox.Width - triggerLeft.X - 1, triggerLeft.Y);
			if (!_recorder.Recording)
			{
				var tl = newState.Gamepad.LeftTrigger;
				var tr = newState.Gamepad.RightTrigger;
				// Temp workaround: when initialized triggers have default value of 127);
				if (tl == 110 && tr == 110)
				{
					this.LeftTriggerTextBox.Text = "0";
					this.RightTriggerTextBox.Text = "0";
				}
				else
				{
					ControlsHelper.SetText(LeftTriggerTextBox, tl.ToString());
					ControlsHelper.SetText(RightTriggerTextBox, tr.ToString());
					on = tl > 0;
					setLabelColor(on, LeftTriggerLabel);
					if (on)
						e.Graphics.DrawImage(this.markB, triggerLeft.X + mW, triggerLeft.Y + mH);
					on = tr > 0;
					setLabelColor(on, RightTriggerLabel);
					if (on)
						e.Graphics.DrawImage(this.markB, triggerRight.X + mW, triggerRight.Y + mH);
				}
				on = newState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder);
				setLabelColor(on, LeftShoulderLabel);
				if (on)
					e.Graphics.DrawImage(this.markB, shoulderLeft.X + mW, shoulderLeft.Y + mH);
				on = newState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.RightShoulder);
				setLabelColor(on, RightShoulderLabel);
				if (on)
					e.Graphics.DrawImage(this.markB, shoulderRight.X + mW, shoulderRight.Y + mH);
			}
			// If recording is in progress and recording image must be drawn then...
			else if (_recorder.drawRecordingImage)
			{
				// Draw recording mark on controller.
				if (CurrentCbx == LeftTriggerComboBox)
					_recorder.drawMarkR(e, triggerLeft);
				if (CurrentCbx == LeftShoulderComboBox)
					_recorder.drawMarkR(e, shoulderLeft);
				if (CurrentCbx == RightTriggerComboBox)
					_recorder.drawMarkR(e, triggerRight);
				if (CurrentCbx == RightShoulderComboBox)
					_recorder.drawMarkR(e, shoulderRight);
			}
		}

		void FrontPictureBox_Paint(object sender, PaintEventArgs e)
		{
			// Button coordinates.
			Point buttonY = new Point(196, 29);
			Point buttonX = new Point(178, 48);
			Point buttonB = new Point(215, 48);
			Point buttonA = new Point(196, 66);
			Point thumbLeft = new Point(59, 47);
			Point thumbRight = new Point(160, 88);
			Point dPad = new Point(92, 88);
			Point dPadUp = new Point(dPad.X, dPad.Y - 13);
			Point dPadLeft = new Point(dPad.X - 13, dPad.Y);
			Point dPadRight = new Point(dPad.X + 13, dPad.Y);
			Point dPadDown = new Point(dPad.X, dPad.Y + 13);
			Point buttonGuide = new Point(127, 48);
			Point buttonBack = new Point(103, 48);
			Point buttonStart = new Point(152, 48);
			Point[] pads = new Point[4];
			pads[0] = new Point(116, 35);
			pads[1] = new Point(139, 35);
			pads[2] = new Point(116, 62);
			pads[3] = new Point(139, 62);
			// Display controller.
			bool on = newConnected;
			if (!on)
				return;
			// Display controller index light.
			int mW = -this.markC.Width / 2;
			int mH = -this.markC.Height / 2;
			var index = (int)MappedTo - 1;
			e.Graphics.DrawImage(this.markC, pads[index].X + mW, pads[index].Y + mH);

			float padSize = 22F / (float)(ushort.MaxValue);

			mW = -this.markB.Width / 2;
			mH = -this.markB.Height / 2;

			if (!_recorder.Recording)
			{
				setLabelColor(_leftX > 2000, LeftThumbAxisXLabel);
				if (_leftX < -2000)
					LeftThumbAxisXLabel.ForeColor = Color.DarkRed;
				setLabelColor(_leftY > 2000, LeftThumbAxisYLabel);
				if (_leftY < -2000)
					LeftThumbAxisYLabel.ForeColor = Color.DarkRed;
				setLabelColor(_rightX > 2000, RightThumbAxisXLabel);
				if (_rightX < -2000)
					RightThumbAxisXLabel.ForeColor = Color.DarkRed;
				setLabelColor(_rightY > 2000, RightThumbAxisYLabel);
				if (_rightY < -2000)
					RightThumbAxisYLabel.ForeColor = Color.DarkRed;
				// Draw button state green led image.
				DrawState(GamepadButtonFlags.A, buttonA, ButtonALabel, e);
				DrawState(GamepadButtonFlags.B, buttonB, ButtonBLabel, e);
				DrawState(GamepadButtonFlags.X, buttonX, ButtonXLabel, e);
				DrawState(GamepadButtonFlags.Y, buttonY, ButtonYLabel, e);
				//DrawState(GamepadButtonFlags.Guide, buttonGuide, ButtonGuideLabel, e);
				DrawState(GamepadButtonFlags.Start, buttonStart, ButtonStartLabel, e);
				DrawState(GamepadButtonFlags.Back, buttonBack, ButtonBackLabel, e);
				DrawState(GamepadButtonFlags.DPadUp, dPadUp, DPadUpLabel, e);
				DrawState(GamepadButtonFlags.DPadDown, dPadDown, DPadDownLabel, e);
				DrawState(GamepadButtonFlags.DPadLeft, dPadLeft, DPadLeftLabel, e);
				DrawState(GamepadButtonFlags.DPadRight, dPadRight, DPadRightLabel, e);
				DrawState(GamepadButtonFlags.RightThumb, thumbRight, RightThumbButtonLabel, e);
				DrawState(GamepadButtonFlags.LeftThumb, thumbLeft, LeftThumbButtonLabel, e);
				// Draw axis state green cross image.
				e.Graphics.DrawImage(this.markA, (float)((thumbRight.X + mW) + (_rightX * padSize)), (float)((thumbRight.Y + mH) + (-_rightY * padSize)));
				e.Graphics.DrawImage(this.markA, (float)((thumbLeft.X + mW) + (_leftX * padSize)), (float)((thumbLeft.Y + mH) + (-_leftY * padSize)));
			}
			// If recording is in progress and recording image must be drawn then...
			else if (_recorder.drawRecordingImage)
			{
				Point? p = null;
				if (CurrentCbx == ButtonBackComboBox)
					p = buttonBack;
				if (CurrentCbx == ButtonStartComboBox)
					p = buttonStart;
				if (CurrentCbx == ButtonYComboBox)
					p = buttonY;
				if (CurrentCbx == ButtonXComboBox)
					p = buttonX;
				if (CurrentCbx == ButtonBComboBox)
					p = buttonB;
				if (CurrentCbx == ButtonAComboBox)
					p = buttonA;
				if (CurrentCbx == DPadUpComboBox)
					p = dPadUp;
				if (CurrentCbx == DPadRightComboBox)
					p = dPadRight;
				if (CurrentCbx == DPadDownComboBox)
					p = dPadDown;
				if (CurrentCbx == DPadLeftComboBox)
					p = dPadLeft;
				if (CurrentCbx == LeftThumbButtonComboBox)
					p = thumbLeft;
				if (CurrentCbx == RightThumbButtonComboBox)
					p = thumbRight;
				if (CurrentCbx == LeftThumbAxisXComboBox)
					p = new Point(thumbLeft.X + 10, thumbLeft.Y);
				if (CurrentCbx == LeftThumbAxisYComboBox)
					p = new Point(thumbLeft.X, thumbLeft.Y - 10);
				if (CurrentCbx == RightThumbAxisXComboBox)
					p = new Point(thumbRight.X + 10, thumbRight.Y);
				if (CurrentCbx == RightThumbAxisYComboBox)
					p = new Point(thumbRight.X, thumbRight.Y - 10);
				if (p.HasValue)
					_recorder.drawMarkR(e, p.Value);

			}
		}

		void DrawState(GamepadButtonFlags button, Point location, Label label, PaintEventArgs e)
		{
			var mW = -this.markB.Width / 2;
			var mH = -this.markB.Height / 2;
			var on = newState.Gamepad.Buttons.HasFlag(button);
			if (on)
				e.Graphics.DrawImage(this.markB, location.X + mW, location.Y + mH);
			if (label != null)
				setLabelColor(on, label);
		}

		void setLabelColor(bool on, Label label)
		{
			Color c = on ? Color.Green : SystemColors.ControlText;
			if (label.ForeColor != c)
				label.ForeColor = c;
		}

		#endregion

		#region Settings Map


		public MapTo MappedTo;

		/// <summary>
		/// Link control with INI key. Value/Text of control will be automatically tracked and INI file updated.
		/// </summary>
		public void UpdateSettingsMap()
		{
			// FakeAPI
			AddMap(() => SettingName.ProductName, DirectInputPanel.DeviceProductNameTextBox);
			AddMap(() => SettingName.ProductGuid, DirectInputPanel.DeviceProductGuidTextBox);
			AddMap(() => SettingName.InstanceGuid, DirectInputPanel.DeviceInstanceGuidTextBox);
			AddMap(() => SettingName.GamePadType, DeviceSubTypeComboBox);
			AddMap(() => SettingName.PassThrough, PassThroughCheckBox);
			AddMap(() => SettingName.ForcesPassThrough, ForceFeedbackPassThroughCheckBox);
			AddMap(() => SettingName.PassThroughIndex, PassThroughIndexComboBox);
			// Mapping
			AddMap(() => SettingName.MapToPad, DirectInputPanel.MapToPadComboBox);
			// Left Trigger
			AddMap(() => SettingName.LeftTrigger, LeftTriggerComboBox, true);
			AddMap(() => SettingName.LeftTriggerDeadZone, LeftTriggerUserControl.DeadZoneTrackBar);
			AddMap(() => SettingName.LeftTriggerAntiDeadZone, LeftTriggerUserControl.AntiDeadZoneNumericUpDown);
			AddMap(() => SettingName.LeftTriggerLinear, LeftTriggerUserControl.SensitivityNumericUpDown);
			// Right Trigger
			AddMap(() => SettingName.RightTrigger, RightTriggerComboBox, true);
			AddMap(() => SettingName.RightTriggerDeadZone, RightTriggerUserControl.DeadZoneTrackBar);
			AddMap(() => SettingName.RightTriggerAntiDeadZone, RightTriggerUserControl.AntiDeadZoneNumericUpDown);
			AddMap(() => SettingName.RightTriggerLinear, RightTriggerUserControl.SensitivityNumericUpDown);
			// Combining
			AddMap(() => SettingName.Combined, CombinedCheckBox);
			AddMap(() => SettingName.CombinedIndex, CombinedIndexComboBox);
			// D-Pad
			AddMap(() => SettingName.DPad, DPadComboBox, true);
			AddMap(() => SettingName.DPadUp, DPadUpComboBox, true);
			AddMap(() => SettingName.DPadDown, DPadDownComboBox, true);
			AddMap(() => SettingName.DPadLeft, DPadLeftComboBox, true);
			AddMap(() => SettingName.DPadRight, DPadRightComboBox, true);
			// Axis To Button
			AddMap(() => SettingName.ButtonADeadZone, AxisToButtonADeadZonePanel.DeadZoneNumericUpDown);
			AddMap(() => SettingName.ButtonBDeadZone, AxisToButtonBDeadZonePanel.DeadZoneNumericUpDown);
			AddMap(() => SettingName.ButtonXDeadZone, AxisToButtonXDeadZonePanel.DeadZoneNumericUpDown);
			AddMap(() => SettingName.ButtonYDeadZone, AxisToButtonYDeadZonePanel.DeadZoneNumericUpDown);
			AddMap(() => SettingName.ButtonStartDeadZone, AxisToButtonStartDeadZonePanel.DeadZoneNumericUpDown);
			AddMap(() => SettingName.ButtonBackDeadZone, AxisToButtonBackDeadZonePanel.DeadZoneNumericUpDown);
			AddMap(() => SettingName.LeftShoulderDeadZone, AxisToLeftShoulderDeadZonePanel.DeadZoneNumericUpDown);
			AddMap(() => SettingName.LeftThumbButtonDeadZone, AxisToLeftThumbButtonDeadZonePanel.DeadZoneNumericUpDown);
			AddMap(() => SettingName.RightShoulderDeadZone, AxisToRightShoulderDeadZonePanel.DeadZoneNumericUpDown);
			AddMap(() => SettingName.RightThumbButtonDeadZone, AxisToRightThumbButtonDeadZonePanel.DeadZoneNumericUpDown);
			// Axis To D-Pad (separate directions).
			AddMap(() => SettingName.DPadDownDeadZone, AxisToDPadDownDeadZonePanel.DeadZoneNumericUpDown);
			AddMap(() => SettingName.DPadLeftDeadZone, AxisToDPadLeftDeadZonePanel.DeadZoneNumericUpDown);
			AddMap(() => SettingName.DPadRightDeadZone, AxisToDPadRightDeadZonePanel.DeadZoneNumericUpDown);
			AddMap(() => SettingName.DPadUpDeadZone, AxisToDPadUpDeadZonePanel.DeadZoneNumericUpDown);
			// Axis To D-Pad.
			AddMap(() => SettingName.AxisToDPadEnabled, AxisToDPadEnabledCheckBox);
			AddMap(() => SettingName.AxisToDPadDeadZone, AxisToDPadDeadZoneTrackBar);
			AddMap(() => SettingName.AxisToDPadOffset, AxisToDPadOffsetTrackBar);
			// Buttons
			AddMap(() => SettingName.ButtonGuide, ButtonGuideComboBox, true);
			AddMap(() => SettingName.ButtonBack, ButtonBackComboBox, true);
			AddMap(() => SettingName.ButtonStart, ButtonStartComboBox, true);
			AddMap(() => SettingName.ButtonA, ButtonAComboBox, true);
			AddMap(() => SettingName.ButtonB, ButtonBComboBox, true);
			AddMap(() => SettingName.ButtonX, ButtonXComboBox, true);
			AddMap(() => SettingName.ButtonY, ButtonYComboBox, true);
			// Shoulders.
			AddMap(() => SettingName.LeftShoulder, LeftShoulderComboBox, true);
			AddMap(() => SettingName.RightShoulder, RightShoulderComboBox, true);
			// Left Thumb
			AddMap(() => SettingName.LeftThumbAxisX, LeftThumbAxisXComboBox, true);
			AddMap(() => SettingName.LeftThumbAxisY, LeftThumbAxisYComboBox, true);
			AddMap(() => SettingName.LeftThumbRight, LeftThumbRightComboBox, true);
			AddMap(() => SettingName.LeftThumbLeft, LeftThumbLeftComboBox, true);
			AddMap(() => SettingName.LeftThumbUp, LeftThumbUpComboBox, true);
			AddMap(() => SettingName.LeftThumbDown, LeftThumbDownComboBox, true);
			AddMap(() => SettingName.LeftThumbButton, LeftThumbButtonComboBox, true);
			AddMap(() => SettingName.LeftThumbDeadZoneX, LeftThumbXUserControl.DeadZoneTrackBar);
			AddMap(() => SettingName.LeftThumbDeadZoneY, LeftThumbYUserControl.DeadZoneTrackBar);
			AddMap(() => SettingName.LeftThumbAntiDeadZoneX, LeftThumbXUserControl.AntiDeadZoneNumericUpDown);
			AddMap(() => SettingName.LeftThumbAntiDeadZoneY, LeftThumbYUserControl.AntiDeadZoneNumericUpDown);
			AddMap(() => SettingName.LeftThumbLinearX, LeftThumbXUserControl.SensitivityNumericUpDown);
			AddMap(() => SettingName.LeftThumbLinearY, LeftThumbYUserControl.SensitivityNumericUpDown);
			// Right Thumb
			AddMap(() => SettingName.RightThumbAxisX, RightThumbAxisXComboBox, true);
			AddMap(() => SettingName.RightThumbAxisY, RightThumbAxisYComboBox, true);
			AddMap(() => SettingName.RightThumbRight, RightThumbRightComboBox, true);
			AddMap(() => SettingName.RightThumbLeft, RightThumbLeftComboBox, true);
			AddMap(() => SettingName.RightThumbUp, RightThumbUpComboBox, true);
			AddMap(() => SettingName.RightThumbDown, RightThumbDownComboBox, true);
			AddMap(() => SettingName.RightThumbButton, RightThumbButtonComboBox, true);
			AddMap(() => SettingName.RightThumbDeadZoneX, RightThumbXUserControl.DeadZoneTrackBar);
			AddMap(() => SettingName.RightThumbDeadZoneY, RightThumbYUserControl.DeadZoneTrackBar);
			AddMap(() => SettingName.RightThumbAntiDeadZoneX, RightThumbXUserControl.AntiDeadZoneNumericUpDown);
			AddMap(() => SettingName.RightThumbAntiDeadZoneY, RightThumbYUserControl.AntiDeadZoneNumericUpDown);
			AddMap(() => SettingName.RightThumbLinearX, RightThumbXUserControl.SensitivityNumericUpDown);
			AddMap(() => SettingName.RightThumbLinearY, RightThumbYUserControl.SensitivityNumericUpDown);
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

		void AddMap<T>(Expression<Func<T>> setting, Control control, bool iniConverter = false)
		{
			var section = string.Format(@"PAD{0}", (int)MappedTo);
			SettingsManager.AddMap(section, setting, control, MappedTo, iniConverter);
		}

		#endregion

		short _leftX;
		short _leftY;
		byte _leftTrigger;
		short _rightX;
		short _rightY;
		byte _rightTrigger;

		//XINPUT_GAMEPAD GamePad;
		Guid _InstanceGuid;

		private void UpdatePassThroughRelatedControls()
		{
			// Is Pass Through enabled?
			bool fullPassThrough = PassThroughCheckBox.Checked;
			bool forcesPassThrough = ForceFeedbackPassThroughCheckBox.Checked;

			// If full pass-through mode is turned on, changing forces pass-through has no effect.
			ForceFeedbackPassThroughCheckBox.Enabled = !fullPassThrough;

			// Pass Through index is enabled if either pass through mode is enabled
			PassThroughIndexComboBox.Enabled = (fullPassThrough || forcesPassThrough);
		}

		/// <summary>
		/// Get selected Setting. If device is not selected then return null.
		/// </summary>
		/// <returns></returns>
		public UserSetting GetSelectedSetting()
		{
			var grid = MappedDevicesDataGridView;
			var row = grid.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault();
			var setting = (row == null)
				? null
				: (Engine.Data.UserSetting)row.DataBoundItem;
			return setting;
		}

		/// <summary>
		/// Get selected device. If device is not connected then return null.
		/// </summary>
		/// <returns></returns>
		UserDevice GetCurrentDevice()
		{
			var setting = GetSelectedSetting();
			var device = (setting == null)
				? null
				: SettingsManager.GetDevice(setting.InstanceGuid);
			return device;
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
				var map = maps.First(x => x.PropertyName == p.Name);
				var key = map.IniPath.Split('\\')[1];
				// Get setting value from the form.
				var v = SettingsManager.Current.GetSettingValue(map.Control);
				// Set value onto padSetting.
				p.SetValue(ps, v ?? "", null);
			}
			ps.PadSettingChecksum = ps.CleanAndGetCheckSum();
			return ps;
		}

		/// <summary>
		/// Get PadSetting from currently selected device.
		/// </summary>
		public PadSetting GetCurrentPadSetting()
		{
			PadSetting ps = null;
			var setting = GetSelectedSetting();
			if (setting != null)
				ps = SettingsManager.GetPadSetting(setting.PadSettingChecksum);
			if (ps == null)
				ps = new PadSetting();
			return ps;
		}

		object updateFromDirectInputLock = new object();

		/// <summary>
		/// This function will be called from UpdateTimer on main form.
		/// </summary>
		public void UpdateFromDInput()
		{
			lock (updateFromDirectInputLock)
			{
				var ud = GetCurrentDevice();
				Guid instanceGuid = Guid.Empty;
				var enable = ud != null;
				if (enable)
				{
					instanceGuid = ud.InstanceGuid;
				}
				ControlsHelper.SetEnabled(LoadPresetButton, enable);
				ControlsHelper.SetEnabled(AutoPresetButton, enable);
				ControlsHelper.SetEnabled(ClearPresetButton, enable);
				ControlsHelper.SetEnabled(ResetPresetButton, enable);
				var pages = PadTabControl.TabPages.Cast<TabPage>().ToArray();
				for (int p = 0; p < pages.Length; p++)
				{
					// Get first control to disable which must be Panel.
					var controls = pages[p].Controls.Cast<Control>().ToArray();
					for (int c = 0; c < controls.Length; c++)
					{
						ControlsHelper.SetEnabled(controls[c], enable);
					}
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
				if (enable)
				{
					_recorder.StopRecording(ud.DiState);
				}
			}
		}

		#region Update Controls

		void UpdateDirectInputTabPage(UserDevice diDevice)
		{
			var isOnline = diDevice != null && diDevice.IsOnline;
			var hasState = isOnline && diDevice.Device != null;
			var instance = diDevice == null ? "" : " - " + diDevice.InstanceId;
			var text = "Direct Input" + instance + (isOnline ? hasState ? "" : " - Online" : " - Offline");
			ControlsHelper.SetText(DirectInputTabPage, text);
		}

		#endregion

		// Old XInput state.
		State oldState;
		bool oldConnected;
		// Current XInput state.
		State newState;
		bool newConnected;

		public void UpdateFromXInput()
		{
			var i = (int)MappedTo - 1;
			var useXiStates = SettingsManager.Options.GetXInputStates;
			newState = useXiStates
				? MainForm.Current.DHelper.LiveXiStates[i]
				: MainForm.Current.DHelper.CombinedXiStates[i];
			newConnected = useXiStates
				? MainForm.Current.DHelper.LiveXiConnected[i]
				: MainForm.Current.DHelper.CombinedXiConencted[i];
			// If device is not connected and was not connected then return.
			if (!newConnected && !oldConnected)
				return;
			// If device disconnected then...
			if (!newConnected && oldConnected)
			{
				// Disable form.
				FrontPictureBox.Image = frontDisabledImage;
				TopPictureBox.Image = topDisabledImage;
			}
			// If device connected then...
			if (newConnected && !oldConnected)
			{
				// Enable form.
				FrontPictureBox.Image = frontImage;
				TopPictureBox.Image = topImage;
			}
			_leftX = newState.Gamepad.LeftThumbX;
			_leftY = newState.Gamepad.LeftThumbY;
			_rightX = newState.Gamepad.RightThumbX;
			_rightY = newState.Gamepad.RightThumbY;
			_rightTrigger = newState.Gamepad.RightTrigger;
			_leftTrigger = newState.Gamepad.LeftTrigger;

			ControlsHelper.SetText(LeftThumbTextBox, "{0};{1}", _leftX, _leftY);
			ControlsHelper.SetText(RightThumbTextBox, "{0};{1}", _rightX, _rightY);

			var ud = GetCurrentDevice();
			if (ud != null && ud.DiState != null)
			{
				// Get current pad setting.
				var ps = GetCurrentPadSetting();
				Map map;
				// LeftThumbX
				var axis = ud.DiState.Axis;
				map = ps.Maps.FirstOrDefault(x => x.Target == TargetType.LeftThumbX);
				if (map != null && map.Index > 0 && map.Index <= axis.Length)
					LeftThumbXUserControl.DrawPoint(axis[map.Index - 1], _leftX, map.IsInverted, map.IsHalf);
				// LeftThumbY
				map = ps.Maps.FirstOrDefault(x => x.Target == TargetType.LeftThumbY);
				if (map != null && map.Index > 0 && map.Index <= axis.Length)
					LeftThumbYUserControl.DrawPoint(axis[map.Index - 1], _leftY, map.IsInverted, map.IsHalf);
				// RightThumbX
				map = ps.Maps.FirstOrDefault(x => x.Target == TargetType.RightThumbX);
				if (map != null && map.Index > 0 && map.Index <= axis.Length)
					RightThumbXUserControl.DrawPoint(axis[map.Index - 1], _rightX, map.IsInverted, map.IsHalf);
				// RightThumbY
				map = ps.Maps.FirstOrDefault(x => x.Target == TargetType.RightThumbY);
				if (map != null && map.Index > 0 && map.Index <= axis.Length)
					RightThumbYUserControl.DrawPoint(axis[map.Index - 1], _rightY, map.IsInverted, map.IsHalf);
				// LeftTrigger
				map = ps.Maps.FirstOrDefault(x => x.Target == TargetType.LeftTrigger);
				if (map != null && map.Index > 0 && map.Index <= axis.Length)
					LeftTriggerUserControl.DrawPoint(axis[map.Index - 1], _leftTrigger, map.IsInverted, map.IsHalf);
				// RightTrigger
				map = ps.Maps.FirstOrDefault(x => x.Target == TargetType.RightTrigger);
				if (map != null && map.Index > 0 && map.Index <= axis.Length)
					RightTriggerUserControl.DrawPoint(axis[map.Index - 1], _rightTrigger, map.IsInverted, map.IsHalf);
			}
			// Update controller images.
			TopPictureBox.Refresh();
			FrontPictureBox.Refresh();
			// Update Axis to Button Images.
			var AxisToButtonControls = AxisToButtonGroupBox.Controls.OfType<AxisToButtonUserControl>();
			foreach (var atbPanel in AxisToButtonControls)
			{
				atbPanel.Refresh(newState, markB);
			}
			// Store old state.
			oldState = newState;
			oldConnected = newConnected;
		}

		// Check left thumbStick
		public float FloatToByte(float v)
		{
			// -1 to 1 int16.MinValue int16.MaxValue.
			return (Byte)Math.Round((double)v * (double)Byte.MaxValue);
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
			for (int i = 0; i < count; i++)
			{
				// If mask specified and item is not present then...
				if (mask.HasValue && i < 32 && (((int)Math.Pow(2, i) & mask) == 0))
					continue;
				var item = CreateItem(text, tag, i + 1);
				parent.DropDownItems.Add(item);
			}
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
					_recorder.StartRecording(map);
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
			int leftMotorPeriod = (int)LeftMotorPeriodTrackBar.Value * 5;
			int rightMotorPeriod = (int)RightMotorPeriodTrackBar.Value * 5;
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
			var game = MainForm.Current.CurrentGame;
			var isVirtual = ((EmulationType)game.EmulationType).HasFlag(EmulationType.Virtual);
			if (isVirtual)
			{
				var largeMotor = (byte)ConvertHelper.ConvertRange(0, 100, byte.MinValue, byte.MaxValue, LeftMotorTestTrackBar.Value);
				var smallMotor = (byte)ConvertHelper.ConvertRange(0, 100, byte.MinValue, byte.MaxValue, RightMotorTestTrackBar.Value);
				MainForm.Current.DHelper.SetVibration(MappedTo, largeMotor, smallMotor, 0);
			}
			else
			{
				lock (Controller.XInputLock)
				{
					// Convert 100% TrackBar to MotorSpeed's 0 - 65,535 (100%).
					var leftMotor = (short)ConvertHelper.ConvertRange(0, 100, short.MinValue, short.MaxValue, LeftMotorTestTrackBar.Value);
					var rightMotor = (short)ConvertHelper.ConvertRange(0, 100, short.MinValue, short.MaxValue, RightMotorTestTrackBar.Value);
					var gamePad = MainForm.Current.DHelper.LiveXiControllers[index];
					var isConnected = MainForm.Current.DHelper.LiveXiConnected[index];
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
			var description = Attributes.GetDescription(MappedTo);
			var text = string.Format("Do you really want to clear all {0} settings?", description);
			var form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			var result = form.ShowForm(text, "Clear Controller Settings", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			if (result != DialogResult.Yes)
				return;
			SettingsManager.Current.LoadPadSettingsIntoSelectedDevice(MappedTo, null);
		}

		void ResetPresetButton_Click(object sender, EventArgs e)
		{
			var description = Attributes.GetDescription(MappedTo);
			var text = string.Format("Do you really want to reset all {0} settings?", description);
			var form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			var result = form.ShowForm(text, "Reset Controller Settings", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			if (result == DialogResult.Yes)
			{
				//MainForm.Current.ReloadXinputSettings();
			}
		}

		private void AutoPresetButton_Click(object sender, EventArgs e)
		{
			var ud = GetCurrentDevice();
			if (ud == null)
				return;
			var description = Attributes.GetDescription(MappedTo);
			var form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			var buttons = MessageBoxButtons.YesNo;
			var text = string.Format("Do you want to fill all {0} settings automatically?", description);
			if (ud.Device == null && !TestDeviceHelper.ProductGuid.Equals(ud.ProductGuid))
			{
				text = string.Format("Device is offline. Please connect device to fill all {0} settings automatically.", description);
				buttons = MessageBoxButtons.OK;
			}
			var result = form.ShowForm(text, "Auto Controller Settings", buttons, MessageBoxIcon.Question);
			if (result != DialogResult.Yes)
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
				markA.Dispose();
				markB.Dispose();
				markC.Dispose();
				components.Dispose();
				_recorder.Dispose();
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


		LoadPresetsForm presetForm;

		private void LoadPresetButton_Click(object sender, EventArgs e)
		{
			ShowPresetForm();
		}

		void ShowPresetForm()
		{
			if (presetForm == null)
			{
				presetForm = new LoadPresetsForm();
				presetForm.Owner = MainForm.Current;
			}
			presetForm.StartPosition = FormStartPosition.CenterParent;
			presetForm.InitForm();
			ControlsHelper.CheckTopMost(presetForm);
			var result = presetForm.ShowDialog();
			if (result == DialogResult.OK)
			{
				var ps = presetForm.SelectedItem;
				if (ps != null)
				{
					MainForm.Current.UpdateTimer.Stop();
					SettingsManager.Current.LoadPadSettingsIntoSelectedDevice(MappedTo, ps);
					MainForm.Current.UpdateTimer.Start();
				}
			}
			presetForm.UnInitForm();
		}

		#region Mapped Devices

		private void AddMapButton_Click(object sender, EventArgs e)
		{
			var game = MainForm.Current.CurrentGame;
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
			// Loop trough selected devices.
			foreach (var ud in selectedUserDevices)
			{
				// Try to get existing setting by instance guid and file name.
				var setting = SettingsManager.GetSetting(ud.InstanceGuid, game.FileName);
				// If device setting for the game was not found then.
				if (setting == null)
				{
					// Create new setting.
					setting = AppHelper.GetNewSetting(ud, game, MappedTo);
					// Get auto-configured pad setting.
					var ps = AutoMapHelper.GetAutoPreset(ud);
					SettingsManager.Current.LoadPadSettingAndCleanup(setting, ps, true);
					SettingsManager.Current.SyncFormFromPadSetting(MappedTo, ps);
					// Refresh online status
					SettingsManager.RefreshDeviceIsOnlineValueOnSettings(setting);
					// Load created setting.
					//SettingsManager.Current.LoadPadSettings(MappedTo, ps);
				}
				else
				{
					// Enable if not enabled.
					if (!setting.IsEnabled)
						setting.IsEnabled = true;
					// Map setting to current pad.
					setting.MapTo = (int)MappedTo;
				}
			}
			var hasNewSettings = SettingsManager.GetSettings(game.FileName, MappedTo).Count > 0;
			// if new devices mapped and button is not enabled then...
			if (noOldSettings && hasNewSettings && !EnableButton.Checked)
			{
				// Enable mapping.
				EnableButton_Click(null, null);
			}
			SettingsManager.Current.RaiseSettingsChanged(null);
		}

		private void RemoveMapButton_Click(object sender, EventArgs e)
		{
			var game = MainForm.Current.CurrentGame;
			// Return if game is not selected.
			if (game == null)
				return;
			var settingsOld = SettingsManager.GetSettings(game.FileName, MappedTo);
			var setting = GetSelectedSetting();
			if (setting != null)
			{
				setting.MapTo = (int)MapTo.Disabled;
			}
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
			var game = MainForm.Current.CurrentGame;
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

		private void MappedDevicesDataGridView_SelectionChanged(object sender, EventArgs e)
		{
			var setting = GetSelectedSetting();
			var padSetting = setting == null
				? null
				: SettingsManager.GetPadSetting(setting.PadSettingChecksum);
			SettingsManager.Current.LoadPadSettingsIntoSelectedDevice(MappedTo, padSetting);
			UpdateGridButtons();
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
			var game = MainForm.Current.CurrentGame;
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
			var game = MainForm.Current.CurrentGame;
			// If no game selected then ignore click.
			if (game == null)
				return;
			var flag = AppHelper.GetMapFlag(MappedTo);
			var value = (MapToMask)game.EnableMask;
			var autoMap = value.HasFlag(flag);
			// If AUTO enabled then...
			if (autoMap)
			{
				// Remove AUTO.
				game.EnableMask = (int)(value & ~flag);
			}
			else
			{
				// Add AUTO.
				game.EnableMask = (int)(value | flag);
			}
			if (game.EnableMask > 0 && game.EmulationType != (int)EmulationType.Virtual)
				game.EmulationType = (int)EmulationType.Virtual;
			if (game.EnableMask == 0 && game.EmulationType == (int)EmulationType.Virtual)
				game.EmulationType = (int)EmulationType.None;
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
			var ps = GetCurrentPadSetting();
			var text = JocysCom.ClassLibrary.Runtime.Serializer.SerializeToXmlString(ps, null, true);
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
				var form = new JocysCom.ClassLibrary.Controls.MessageBoxForm();
				form.StartPosition = FormStartPosition.CenterParent;
				ControlsHelper.CheckTopMost(form);
				form.ShowForm(ex.Message);
				form.Dispose();
				return;
			}
		}

	}
}
