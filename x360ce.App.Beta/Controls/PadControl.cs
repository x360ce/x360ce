using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using SharpDX.DirectInput;
using SharpDX.XInput;
using System.Linq;
using x360ce.Engine;
using System.Diagnostics;
using JocysCom.ClassLibrary.IO;
using System.Linq.Expressions;
using JocysCom.ClassLibrary.ComponentModel;
using x360ce.Engine.Data;
using System.Reflection;

namespace x360ce.App.Controls
{
	public partial class PadControl : UserControl
	{

		public PadControl(MapTo controllerIndex)
		{
			InitializeComponent();
			// Hide left/right border.
			//MappedDevicesDataGridView.Width = this.Width + 2;
			//MappedDevicesDataGridView.Left = -1;
			JocysCom.ClassLibrary.Controls.ControlsHelper.ApplyBorderStyle(MappedDevicesDataGridView);


			MappedTo = controllerIndex;
			object[] rates = {
				1000/8, //  125
				1000/7, //  142
				1000/6, //  166
				1000/5, //  200
				1000/4, //  250
				1000/3, //  333
				1000/2, //  500
				1000/1, // 1000
			};
			PollingRateComboBox.Items.AddRange(rates);
			PollingRateComboBox.SelectedIndex = 0;
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
		}


		public void InitPadData()
		{
			// WORKAROUND: Remove SelectionChanged event.
			MappedDevicesDataGridView.SelectionChanged -= MappedDevicesDataGridView_SelectionChanged;
			MappedDevicesDataGridView.DataSource = mappedItems;
			// WORKAROUND: Use BeginInvoke to prevent SelectionChanged firing multiple times.
			BeginInvoke((MethodInvoker)delegate ()
			{
				MappedDevicesDataGridView.SelectionChanged += MappedDevicesDataGridView_SelectionChanged;
				MappedDevicesDataGridView_SelectionChanged(MappedDevicesDataGridView, new EventArgs());
			});
			Settings_Items_ListChanged(null, null);
			SettingsManager.Settings.Items.ListChanged += Settings_Items_ListChanged;
		}

		public void InitPadControl()
		{
			var dv = new System.Data.DataView();
			var grid = MappedDevicesDataGridView;
			grid.AutoGenerateColumns = false;
			// Initialize images.
			this.TopPictureBox.Image = topDisabledImage;
			this.FrontPictureBox.Image = frontDisabledImage;
			this.markB = new Bitmap(EngineHelper.GetResource("Images.MarkButton.png"));
			this.markA = new Bitmap(EngineHelper.GetResource("Images.MarkAxis.png"));
			this.markC = new Bitmap(EngineHelper.GetResource("Images.MarkController.png"));
			this.markR = new Bitmap(EngineHelper.GetResource("Images.bullet_ball_glass_red_16x16.png"));
			float rH = topDisabledImage.HorizontalResolution;
			float rV = topDisabledImage.VerticalResolution;
			// Make sure resolution is same everywhere so images won't be resized.
			this.markB.SetResolution(rH, rV);
			this.markA.SetResolution(rH, rV);
			this.markC.SetResolution(rH, rV);
			this.markR.SetResolution(rH, rV);
			// Add GamePad typed to ComboBox.
			var types = (SharpDX.XInput.DeviceSubType[])Enum.GetValues(typeof(SharpDX.XInput.DeviceSubType));
			foreach (var item in types) DeviceSubTypeComboBox.Items.Add(item);
			// Add force feedback typed to ComboBox.
			var effectsTypes = (ForceEffectType[])Enum.GetValues(typeof(ForceEffectType));
			foreach (var item in effectsTypes) ForceTypeComboBox.Items.Add(item);

			var effectDirections = (ForceEffectDirection[])Enum.GetValues(typeof(ForceEffectDirection));
			foreach (var item in effectDirections) LeftMotorDirectionComboBox.Items.Add(item);
			foreach (var item in effectDirections) RightMotorDirectionComboBox.Items.Add(item);

			// Add player index to combo boxes
			var playerOptions = new List<KeyValuePair>();
			var playerTypes = (UserIndex[])Enum.GetValues(typeof(UserIndex));
			foreach (var item in playerTypes) playerOptions.Add(new KeyValuePair(item.ToString(), ((int)item).ToString()));
			PassThroughIndexComboBox.DataSource = new BindingSource(playerOptions, null); // Otherwise changing one changes the other
			PassThroughIndexComboBox.DisplayMember = "Key";
			PassThroughIndexComboBox.ValueMember = "Value";
			CombinedIndexComboBox.DataSource = new BindingSource(playerOptions, null);  // Otherwise changing one changes the other
			CombinedIndexComboBox.DisplayMember = "Key";
			CombinedIndexComboBox.ValueMember = "Value";
			var comboBoxes = new List<ComboBox>();
			GetAllControls(GeneralTabPage, ref comboBoxes);
			// Attach context strip with button names to every ComboBox on general tab.
			foreach (var cb in comboBoxes)
			{
				((ComboBox)cb).ContextMenuStrip = DiMenuStrip;
			}
			UpdateFromCurrentGame();
		}

		public void UpdateFromCurrentGame()
		{
			var game = MainForm.Current.CurrentGame;
			var flag = AppHelper.GetMapFlag(MappedTo);
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
				// Unmap all devices.	
				var grid = MappedDevicesDataGridView;
				var items = grid.Rows.Cast<DataGridViewRow>().Where(x => x.Visible).Select(x => (Setting)x.DataBoundItem).ToArray();
				foreach (var item in items)
				{
					item.MapTo = (int)MapTo.None;
				}
			}
			ShowHideAndSelectGridRows(null);
			UpdateGridButtons();
		}

		private void Settings_Items_ListChanged(object sender, ListChangedEventArgs e)
		{
			ShowHideAndSelectGridRows(null);
		}

		object DevicesToMapDataGridViewLock = new object();

		SortableBindingList<Engine.Data.Setting> mappedItems = new SortableBindingList<Engine.Data.Setting>();

		void ShowHideAndSelectGridRows(Guid? instanceGuid = null)
		{
			lock (DevicesToMapDataGridViewLock)
			{
				var grid = MappedDevicesDataGridView;
				var game = MainForm.Current.CurrentGame;
				// Get rows which must be displayed on the list.
				var itemsToShow = SettingsManager.Settings.Items
					// Filter devices by controller.	
					.Where(x => x.MapTo == (int)MappedTo)
					// Filter devices by selected game.
					.Where(x => x.FileName == game.FileName && x.FileProductName == game.FileProductName)
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
					{
						mappedItems.Remove(item);
					}
					// Do adding.
					foreach (var item in itemsToInsert)
					{
						mappedItems.Add(item);
					}
					if (bound)
					{
						// Resume CurrencyManager and Layout.
						cm.ResumeBinding();
					}
					grid.ResumeLayout();
					// Restore selection.
					JocysCom.ClassLibrary.Controls.ControlsHelper.RestoreSelection(grid, "InstanceGuid", selection);
				}
				var visibleCount = mappedItems.Count();
				var title = string.Format("{0} Mapped Device{1}", visibleCount, visibleCount == 1 ? "" : "s");
				if (mappedItems.Count(x => x.IsEnabled) > 1)
				{
					title += " (Combine)";
				}
				AppHelper.SetText(MappedDevicesLabel, title);
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



		#region Recording

		bool Recording;
		Regex dPadRx = new Regex("(DPad [0-9]+)");
		bool drawRecordingImage;
		object recordingLock = new object();

		private void RecordingTimer_Tick(object sender, EventArgs e)
		{
			drawRecordingImage = !drawRecordingImage;
		}

		void drawMarkR(PaintEventArgs e, Point position)
		{
			int rW = -this.markR.Width / 2;
			int rH = -this.markR.Height / 2;
			e.Graphics.DrawImage(this.markR, position.X + rW, position.Y + rH);
		}

		void StartRecording()
		{
			lock (recordingLock)
			{
				// If recording is not in progress then return.
				if (Recording) return;
				Recording = true;
				recordingSnapshot = null;
				drawRecordingImage = true;
				RecordingTimer.Start();
				CurrentCbx.ForeColor = SystemColors.GrayText;
				MainForm.Current.StatusTimerLabel.Text = (CurrentCbx == DPadComboBox)
					 ? "Recording - press any D-Pad button on your direct input device. Press ESC to cancel..."
					 : "Recording - press button, move axis or slider on your direct input device. Press ESC to cancel...";
			}
		}

		/// <summary>Initial Direct Input activity state</summary>
		CustomDiState recordingSnapshot;

		/// <summary>
		/// Called when recording is in progress.
		/// </summary>
		/// <param name="state">Current direct input activity.</param>
		/// <returns>True if recording stopped, otherwise false.</returns>
		public bool StopRecording(CustomDiState state = null)
		{
			lock (recordingLock)
			{
				// If recording is not in progress then return false.
				if (!Recording)
				{
					recordingSnapshot = null;
					return false;
				}
				// If recording snapshot was not created yet then...
				else if (recordingSnapshot == null)
				{
					// Make snapshot out of the first state during recording.
					recordingSnapshot = state;
					return false;
				}
				// Get actions by comparing initial snapshot with current state.
				var actions = recordingSnapshot.CompareTo(state);
				string action = null;
				// Must stop recording if null passed.
				var stop = actions == null;
				// if at least one action was recorded then...
				if (!stop && actions.Length > 0)
				{
					// If this is DPad ComboBox then...
					if (CurrentCbx == DPadComboBox)
					{
						// Get first action suitable for DPad
						var dPadAction = actions.FirstOrDefault(x => dPadRx.IsMatch(x));
						if (dPadAction != null)
						{
							action = dPadRx.Match(dPadAction).Groups[0].Value;
							stop = true;
						}
					}
					else
					{
						// Get first recorded action.
						action = actions[0];
						stop = true;
					}
				}
				// If recording must stop then...
				if (stop)
				{
					Recording = false;
					RecordingTimer.Stop();
					// If stop was initiated before action was recorded then...                    
					if (string.IsNullOrEmpty(action))
					{
						CurrentCbx.Items.Clear();
					}
					else
					{
						// If suitable action was recorded then...
						SettingsManager.Current.SetComboBoxValue(CurrentCbx, action);
						// Save setting and notify if value changed.
						MainForm.Current.NotifySettingsChange(CurrentCbx);
					}
					CurrentCbx.ForeColor = SystemColors.WindowText;
					CurrentCbx = null;
				}
				return stop;
			}
		}

		#endregion

		#region Control ComboBox'es

		ComboBox CurrentCbx;

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
			var oldLeft = cbx.Left;
			// Move default DropDown away from the screen.
			cbx.Left = -10000;
			var del = new ComboBoxDropDownDelegate(ComboBoxDropDown);
			BeginInvoke(del, new object[] { cbx, oldLeft });
		}

		delegate void ComboBoxDropDownDelegate(ComboBox cbx, int oldLeft);

		void ComboBoxDropDown(ComboBox cbx, int oldLeft)
		{
			cbx.IntegralHeight = !cbx.IntegralHeight;
			cbx.IntegralHeight = !cbx.IntegralHeight;
			cbx.Left = oldLeft;
			if (CurrentCbx == cbx)
			{
				CurrentCbx = null;
				cbx.ContextMenuStrip.Hide();
			}
			else
			{
				if (cbx == DPadComboBox) EnableDPadMenu(true);
				var menuStrip = cbx.ContextMenuStrip;
				if (menuStrip != null)
				{
					menuStrip.Show(cbx, new Point(0, cbx.Height), ToolStripDropDownDirection.Default);
				}
				CurrentCbx = cbx;
			}
			if (cbx.Items.Count > 0)
			{
				cbx.SelectedIndex = 0;
			}
		}

		#endregion

		#region Images

		Bitmap markB;
		Bitmap markA;
		Bitmap markC;
		Bitmap markR;

		Bitmap _topImage;
		Bitmap topImage
		{
			get { return _topImage = _topImage ?? new Bitmap(EngineHelper.GetResource("Images.xboxControllerTop.png")); }
		}

		Bitmap _frontImage;
		Bitmap frontImage
		{
			get { return _frontImage = _frontImage ?? new Bitmap(EngineHelper.GetResource("Images.xboxControllerFront.png")); }
		}

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
			bool on = gamePadStateIsConnected;
			if (!on) return;
			// Half mark position adjust.
			int mW = -this.markB.Width / 2;
			int mH = -this.markB.Height / 2;
			// Button coordinates.
			Point shoulderLeft = new Point(43, 66);
			Point shoulderRight = new Point(this.FrontPictureBox.Width - shoulderLeft.X, shoulderLeft.Y);
			Point triggerLeft = new Point(63, 27);
			Point triggerRight = new Point(this.FrontPictureBox.Width - triggerLeft.X - 1, triggerLeft.Y);
			if (!Recording)
			{
				var tl = gamePadState.Gamepad.LeftTrigger;
				var tr = gamePadState.Gamepad.RightTrigger;
				// Temp workaround: when initialized triggers have default value of 127);
				if (tl == 110 && tr == 110)
				{
					this.LeftTriggerTextBox.Text = "0";
					this.RightTriggerTextBox.Text = "0";
				}
				else
				{
					AppHelper.SetText(LeftTriggerTextBox, tl.ToString());
					AppHelper.SetText(RightTriggerTextBox, tr.ToString());
					on = tl > 0;
					setLabelColor(on, LeftTriggerLabel);
					if (on) e.Graphics.DrawImage(this.markB, triggerLeft.X + mW, triggerLeft.Y + mH);
					on = tr > 0;
					setLabelColor(on, RightTriggerLabel);
					if (on) e.Graphics.DrawImage(this.markB, triggerRight.X + mW, triggerRight.Y + mH);
				}
				on = gamePadState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder);
				setLabelColor(on, LeftShoulderLabel);
				if (on) e.Graphics.DrawImage(this.markB, shoulderLeft.X + mW, shoulderLeft.Y + mH);
				on = gamePadState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.RightShoulder);
				setLabelColor(on, RightShoulderLabel);
				if (on) e.Graphics.DrawImage(this.markB, shoulderRight.X + mW, shoulderRight.Y + mH);
			}
			// If recording is in progress and recording image must be drawn then...
			else if (drawRecordingImage)
			{
				// Draw recording mark on controller.
				if (CurrentCbx == LeftTriggerComboBox) drawMarkR(e, triggerLeft);
				if (CurrentCbx == LeftShoulderComboBox) drawMarkR(e, shoulderLeft);
				if (CurrentCbx == RightTriggerComboBox) drawMarkR(e, triggerRight);
				if (CurrentCbx == RightShoulderComboBox) drawMarkR(e, shoulderRight);
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
			bool on = gamePadStateIsConnected;
			if (!on) return;
			// Display controller index light.
			int mW = -this.markC.Width / 2;
			int mH = -this.markC.Height / 2;
			var index = (int)MappedTo - 1;
			e.Graphics.DrawImage(this.markC, pads[index].X + mW, pads[index].Y + mH);

			float padSize = 22F / (float)(ushort.MaxValue);

			mW = -this.markB.Width / 2;
			mH = -this.markB.Height / 2;

			if (!Recording)
			{
				setLabelColor(_leftX > 2000, LeftThumbAxisXLabel);
				if (_leftX < -2000) LeftThumbAxisXLabel.ForeColor = Color.DarkRed;
				setLabelColor(_leftY > 2000, LeftThumbAxisYLabel);
				if (_leftY < -2000) LeftThumbAxisYLabel.ForeColor = Color.DarkRed;
				setLabelColor(_rightX > 2000, RightThumbAxisXLabel);
				if (_rightX < -2000) RightThumbAxisXLabel.ForeColor = Color.DarkRed;
				setLabelColor(_rightY > 2000, RightThumbAxisYLabel);
				if (_rightY < -2000) RightThumbAxisYLabel.ForeColor = Color.DarkRed;
				// Draw button state green led image.
				DrawState(GamepadButtonFlags.A, buttonA, ButtonALabel, e);
				DrawState(GamepadButtonFlags.B, buttonB, ButtonBLabel, e);
				DrawState(GamepadButtonFlags.X, buttonX, ButtonXLabel, e);
				DrawState(GamepadButtonFlags.Y, buttonY, ButtonYLabel, e);
				//DrawState(GamepadButtonFlags.Guide, buttonGuide, ButtonGuideLabel, e);
				DrawState(GamepadButtonFlags.Start, buttonStart, StartButtonLabel, e);
				DrawState(GamepadButtonFlags.Back, buttonBack, BackButtonLabel, e);
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
			else if (drawRecordingImage)
			{
				if (CurrentCbx == ButtonBackComboBox) drawMarkR(e, buttonBack);
				if (CurrentCbx == ButtonStartComboBox) drawMarkR(e, buttonStart);
				if (CurrentCbx == ButtonYComboBox) drawMarkR(e, buttonY);
				if (CurrentCbx == ButtonXComboBox) drawMarkR(e, buttonX);
				if (CurrentCbx == ButtonBComboBox) drawMarkR(e, buttonB);
				if (CurrentCbx == ButtonAComboBox) drawMarkR(e, buttonA);
				if (CurrentCbx == DPadUpComboBox) drawMarkR(e, dPadUp);
				if (CurrentCbx == DPadRightComboBox) drawMarkR(e, dPadRight);
				if (CurrentCbx == DPadDownComboBox) drawMarkR(e, dPadDown);
				if (CurrentCbx == DPadLeftComboBox) drawMarkR(e, dPadLeft);
				if (CurrentCbx == LeftThumbButtonComboBox) drawMarkR(e, thumbLeft);
				if (CurrentCbx == RightThumbButtonComboBox) drawMarkR(e, thumbRight);
				if (CurrentCbx == LeftThumbAxisXComboBox) drawMarkR(e, new Point(thumbLeft.X + 10, thumbLeft.Y));
				if (CurrentCbx == LeftThumbAxisYComboBox) drawMarkR(e, new Point(thumbLeft.X, thumbLeft.Y - 10));
				if (CurrentCbx == RightThumbAxisXComboBox) drawMarkR(e, new Point(thumbRight.X + 10, thumbRight.Y));
				if (CurrentCbx == RightThumbAxisYComboBox) drawMarkR(e, new Point(thumbRight.X, thumbRight.Y - 10));
			}
		}

		void DrawState(GamepadButtonFlags button, Point location, Label label, PaintEventArgs e)
		{
			var mW = -this.markB.Width / 2;
			var mH = -this.markB.Height / 2;
			var on = gamePadState.Gamepad.Buttons.HasFlag(button);
			if (on) e.Graphics.DrawImage(this.markB, location.X + mW, location.Y + mH);
			if (label != null) setLabelColor(on, label);
		}

		void setLabelColor(bool on, Label label)
		{
			Color c = on ? Color.Green : SystemColors.ControlText;
			if (label.ForeColor != c) label.ForeColor = c;
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
			// Triggers
			AddMap(() => SettingName.RightTrigger, RightTriggerComboBox);
			AddMap(() => SettingName.RightTriggerDeadZone, RightTriggerDeadZoneTrackBar);
			AddMap(() => SettingName.LeftTrigger, LeftTriggerComboBox);
			AddMap(() => SettingName.LeftTriggerDeadZone, LeftTriggerDeadZoneTrackBar);
			// Combining
			AddMap(() => SettingName.Combined, CombinedCheckBox);
			AddMap(() => SettingName.CombinedIndex, CombinedIndexComboBox);
			// D-Pad
			AddMap(() => SettingName.DPad, DPadComboBox);
			AddMap(() => SettingName.DPadUp, DPadUpComboBox);
			AddMap(() => SettingName.DPadDown, DPadDownComboBox);
			AddMap(() => SettingName.DPadLeft, DPadLeftComboBox);
			AddMap(() => SettingName.DPadRight, DPadRightComboBox);
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
			AddMap(() => SettingName.ButtonGuide, ButtonGuideComboBox);
			AddMap(() => SettingName.ButtonBack, ButtonBackComboBox);
			AddMap(() => SettingName.ButtonStart, ButtonStartComboBox);
			AddMap(() => SettingName.ButtonA, ButtonAComboBox);
			AddMap(() => SettingName.ButtonB, ButtonBComboBox);
			AddMap(() => SettingName.ButtonX, ButtonXComboBox);
			AddMap(() => SettingName.ButtonY, ButtonYComboBox);
			// Shoulders.
			AddMap(() => SettingName.LeftShoulder, LeftShoulderComboBox);
			AddMap(() => SettingName.RightShoulder, RightShoulderComboBox);
			// Left Thumb
			AddMap(() => SettingName.LeftThumbAxisX, LeftThumbAxisXComboBox);
			AddMap(() => SettingName.LeftThumbAxisY, LeftThumbAxisYComboBox);
			AddMap(() => SettingName.LeftThumbRight, LeftThumbRightComboBox);
			AddMap(() => SettingName.LeftThumbLeft, LeftThumbLeftComboBox);
			AddMap(() => SettingName.LeftThumbUp, LeftThumbUpComboBox);
			AddMap(() => SettingName.LeftThumbDown, LeftThumbDownComboBox);
			AddMap(() => SettingName.LeftThumbButton, LeftThumbButtonComboBox);
			AddMap(() => SettingName.LeftThumbDeadZoneX, LeftThumbXUserControl.DeadZoneTrackBar);
			AddMap(() => SettingName.LeftThumbDeadZoneY, LeftThumbYUserControl.DeadZoneTrackBar);
			AddMap(() => SettingName.LeftThumbAntiDeadZoneX, LeftThumbXUserControl.AntiDeadZoneNumericUpDown);
			AddMap(() => SettingName.LeftThumbAntiDeadZoneY, LeftThumbYUserControl.AntiDeadZoneNumericUpDown);
			AddMap(() => SettingName.LeftThumbLinearX, LeftThumbXUserControl.SensitivityNumericUpDown);
			AddMap(() => SettingName.LeftThumbLinearY, LeftThumbYUserControl.SensitivityNumericUpDown);
			// Right Thumb
			AddMap(() => SettingName.RightThumbAxisX, RightThumbAxisXComboBox);
			AddMap(() => SettingName.RightThumbAxisY, RightThumbAxisYComboBox);
			AddMap(() => SettingName.RightThumbRight, RightThumbRightComboBox);
			AddMap(() => SettingName.RightThumbLeft, RightThumbLeftComboBox);
			AddMap(() => SettingName.RightThumbUp, RightThumbUpComboBox);
			AddMap(() => SettingName.RightThumbDown, RightThumbDownComboBox);
			AddMap(() => SettingName.RightThumbButton, RightThumbButtonComboBox);
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

		void AddMap<T>(Expression<Func<T>> setting, Control control)
		{
			var section = string.Format(@"PAD{0}", (int)MappedTo);
			SettingsManager.AddMap(section, setting, control, MappedTo);
		}

		#endregion

		short _leftX;
		short _leftY;
		short _rightX;
		short _rightY;

		State gamePadState;
		bool gamePadStateIsConnected;
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
		public Setting GetCurrentSetting()
		{
			var grid = MappedDevicesDataGridView;
			var row = grid.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault();
			var setting = (row == null)
				? null
				: (Engine.Data.Setting)row.DataBoundItem;
			return setting;
		}

		/// <summary>
		/// Get selected device. If device is not connected then return null.
		/// </summary>
		/// <returns></returns>
		UserDevice GetCurrentDevice()
		{
			var setting = GetCurrentSetting();
			var device = (setting == null)
				? null
				: SettingsManager.GetDevice(setting.InstanceGuid);
			return device;
		}

		/// <summary>
		/// Get PadSetting from currently selected device.
		/// </summary>
		/// <param name="padIndex">Source pad index.</param>
		public PadSetting GetCurrentPadSetting()
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

		object updateFromDirectInputLock = new object();

		/// <summary>
		/// This function will be called from UpdateTimer on main form.
		/// </summary>
		public void UpdateFromDInput()
		{
			lock (updateFromDirectInputLock)
			{
				var diDevice = GetCurrentDevice();
				Guid instanceGuid = Guid.Empty;
				var enable = diDevice != null;
				if (enable)
				{
					instanceGuid = diDevice.InstanceGuid;
				}
				AppHelper.SetEnabled(LoadPresetButton, enable);
				AppHelper.SetEnabled(AutoPresetButton, enable);
				AppHelper.SetEnabled(ClearPresetButton, enable);
				AppHelper.SetEnabled(ResetPresetButton, enable);
				var pages = PadTabControl.TabPages.Cast<TabPage>().ToArray();
				for (int p = 0; p < pages.Length; p++)
				{
					// Get first control to disable which must be Panel.
					var controls = pages[p].Controls.Cast<Control>().ToArray();
					for (int c = 0; c < controls.Length; c++)
					{
						AppHelper.SetEnabled(controls[c], enable);
					}
				}
				// If device instance changed then...
				if (!Equals(instanceGuid, _InstanceGuid))
				{
					_InstanceGuid = instanceGuid;
					ResetDiMenuStrip(enable ? diDevice : null);
				}
				// Update direct input form and return actions (pressed buttons/dpads, turned axis/sliders).
				UpdateDirectInputTabPage(diDevice);
				JoystickState state;
				DirectInputPanel.UpdateFrom(diDevice, out state);
				CustomDiState diState = null;
				if (state != null) diState = new CustomDiState(state);
				StopRecording(diState);
			}
		}

		#region Update Controls

		void UpdateDirectInputTabPage(UserDevice diDevice)
		{
			var isOnline = diDevice != null && diDevice.IsOnline;
			var hasState = isOnline && diDevice.Device != null;
			var instance = diDevice == null ? "" : " - " + diDevice.InstanceId;
			var text = "Direct Input" + instance + (isOnline ? hasState ? "" : " - Online" : " - Offline");
			AppHelper.SetText(DirectInputTabPage, text);
		}

		#endregion

		State oldState;

		public void UpdateFromXInput(State state, bool IsConnected)
		{
			// If nothing changed then return.
			if (state.Equals(oldState)) return;
			oldState = state;
			var wasConnected = gamePadStateIsConnected;
			var nowConnected = IsConnected;
			gamePadStateIsConnected = IsConnected;
			gamePadState = state;
			// If form was disabled and no data is coming then just return.
			if (!wasConnected && !nowConnected) return;
			// If device connection changed then...
			if (wasConnected != nowConnected)
			{
				if (nowConnected)
				{
					// Enable form.
					FrontPictureBox.Image = frontImage;
					TopPictureBox.Image = topImage;
				}
				else
				{
					// Disable form.
					FrontPictureBox.Image = frontDisabledImage;
					TopPictureBox.Image = topDisabledImage;

				}
			}
			if (nowConnected)
			{
				_leftX = state.Gamepad.LeftThumbX;
				_leftY = state.Gamepad.LeftThumbY;
				_rightX = state.Gamepad.RightThumbX;
				_rightY = state.Gamepad.RightThumbY;
			}
			else
			{
				_leftX = 0;
				_leftY = 0;
				_rightX = 0;
				_rightY = 0;
			}
			AppHelper.SetText(LeftThumbTextBox, "{0};{1}", _leftX, _leftY);
			AppHelper.SetText(RightThumbTextBox, "{0};{1}", _rightX, _rightY);

			var axis = DirectInputPanel.Axis;
			bool success;
			int index;
			SettingType type;
			success = SettingsConverter.TryParseIndexAndType(LeftThumbAxisXComboBox.Text, out index, out type);
			if (success)
				LeftThumbXUserControl.DrawPoint(axis[index - 1], _leftX, type == SettingType.IAxis);
			success = SettingsConverter.TryParseIndexAndType(LeftThumbAxisYComboBox.Text, out index, out type);
			if (success)
				LeftThumbYUserControl.DrawPoint(axis[index - 1], _leftY, type == SettingType.IAxis);
			success = SettingsConverter.TryParseIndexAndType(RightThumbAxisXComboBox.Text, out index, out type);
			if (success)
				RightThumbXUserControl.DrawPoint(axis[index - 1], _rightX, type == SettingType.IAxis);
			success = SettingsConverter.TryParseIndexAndType(RightThumbAxisYComboBox.Text, out index, out type);
			if (success)
				RightThumbYUserControl.DrawPoint(axis[index - 1], _rightY, type == SettingType.IAxis);
			// Update controller images.
			this.TopPictureBox.Refresh();
			this.FrontPictureBox.Refresh();
			// Update Axis to Button Images.
			var AxisToButtonControls = AxisToButtonGroupBox.Controls.OfType<AxisToButtonUserControl>();
			foreach (var atbPanel in AxisToButtonControls)
			{
				atbPanel.Refresh(gamePadState, markB);
			}
		}

		// Check left thumbStick
		public float FloatToByte(float v)
		{
			// -1 to 1 int16.MinValue int16.MaxValue.
			return (Byte)Math.Round((double)v * (double)Byte.MaxValue);
		}

		string cRecord = "[Record]";
		string cEmpty = "<empty>";


		// Function is recreated as soon as new DirectInput Device is available.
		public void ResetDiMenuStrip(UserDevice device)
		{
			DiMenuStrip.Items.Clear();
			ToolStripMenuItem mi;
			mi = new ToolStripMenuItem(cEmpty);
			mi.ForeColor = SystemColors.ControlDarkDark;
			mi.Click += new EventHandler(DiMenuStrip_Click);
			DiMenuStrip.Items.Add(mi);
			// Return if direct input device is not available.
			if (device == null) return;
			// Add [Record] button.
			mi = new ToolStripMenuItem(cRecord);
			mi.Image = new Bitmap(EngineHelper.GetResource("Images.bullet_ball_glass_red_16x16.png"));
			mi.Click += new EventHandler(DiMenuStrip_Click);
			DiMenuStrip.Items.Add(mi);
			// Add Buttons.
			mi = new ToolStripMenuItem("Buttons");
			DiMenuStrip.Items.Add(mi);
			CreateItems(mi, "Button {0}", "b{0}", device.CapButtonCount);
			// Add Axes.
			mi = new ToolStripMenuItem("Axes");
			DiMenuStrip.Items.Add(mi);
			CreateItems(mi, "Inverted", "IAxis {0}", "a-{0}", device.CapAxeCount);
			CreateItems(mi, "Inverted Half", "IHAxis {0}", "x-{0}", device.CapAxeCount);
			CreateItems(mi, "Half", "HAxis {0}", "x{0}", device.CapAxeCount);
			CreateItems(mi, "Axis {0}", "a{0}", device.CapAxeCount);
			// Add Sliders.            
			mi = new ToolStripMenuItem("Sliders");
			DiMenuStrip.Items.Add(mi);
			var slidersCount = 8;
			CreateItems(mi, "Inverted", "ISlider {0}", "s-{0}", slidersCount);
			CreateItems(mi, "Inverted Half", "IHSlider {0}", "h-{0}", slidersCount);
			CreateItems(mi, "Half", "HSlider {0}", "h{0}", slidersCount);
			CreateItems(mi, "Slider {0}", "s{0}", slidersCount);
			// Add D-Pads.
			mi = new ToolStripMenuItem("DPads");
			DiMenuStrip.Items.Add(mi);
			// Add D-Pad Top, Right, Bottom, Left button.
			var dPadNames = Enum.GetNames(typeof(DPadEnum));
			for (int p = 0; p < device.CapPovCount; p++)
			{
				var dPadItem = CreateItem("DPad {0}", "{1}{0}", p + 1, SettingName.SType.DPad);
				mi.DropDownItems.Add(dPadItem);
				for (int d = 0; d < dPadNames.Length; d++)
				{
					var dPadButtonIndex = p * 4 + d + 1;
					var dPadButtonItem = CreateItem("DPad {0} {1}", "{2}{3}", p + 1, dPadNames[d], SettingName.SType.DPadButton, dPadButtonIndex);
					dPadItem.DropDownItems.Add(dPadButtonItem);
				}
			}
		}

		void CreateItems(ToolStripMenuItem parent, string subMenu, string text, string tag, int count)
		{
			var smi = new ToolStripMenuItem(subMenu);
			parent.DropDownItems.Add(smi);
			CreateItems(smi, text, tag, count);
		}

		void CreateItems(ToolStripMenuItem parent, string text, string tag, int count)
		{
			for (int i = 0; i < count; i++)
			{
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
					DiMenuStrip.Close();
				}
			}
			else
			{
				if (item.Text == cRecord)
				{
					StartRecording();
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
					&& !item.Text.StartsWith("DPad"))
				{
					item.Visible = !enable;
				}
				if (item.Text.StartsWith("DPad"))
				{
					if (item.HasDropDownItems)
					{
						foreach (ToolStripMenuItem l1 in item.DropDownItems)
						{
							foreach (ToolStripMenuItem l2 in l1.DropDownItems) l2.Visible = !enable;
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

		void LeftTriggerDeadZoneTrackBar_ValueChanged(object sender, EventArgs e)
		{
			TrackBar control = (TrackBar)sender;
			LeftTriggerDeadZoneTextBox.Text = string.Format("{0} % ", control.Value);
		}


		void RightTriggerDeadZoneTrackBar_ValueChanged(object sender, EventArgs e)
		{
			TrackBar control = (TrackBar)sender;
			RightTriggerDeadZoneTextBox.Text = string.Format("{0} % ", control.Value);
		}

		void MotorTrackBar_ValueChanged(object sender, EventArgs e)
		{
			//if (gamePadState == null) return;
			UpdateForceFeedBack();
		}

		void MotorPeriodTrackBar_ValueChanged(object sender, EventArgs e)
		{
			//if (gamePadState == null) return;
			UpdateForceFeedBack2();
		}

		public void UpdateForceFeedBack2()
		{
			// Convert Direct Input Period force feedback effect parameter value.
			int leftMotorPeriod = (int)LeftMotorPeriodTrackBar.Value * 5;
			int rightMotorPeriod = (int)RightMotorPeriodTrackBar.Value * 5;
			LeftMotorPeriodTextBox.Text = string.Format("{0} ", leftMotorPeriod);
			RightMotorPeriodTextBox.Text = string.Format("{0} ", rightMotorPeriod);
		}

		public void UpdateForceFeedBack()
		{
			if (MainForm.Current.ControllerIndex == -1) return;
			// Convert 100% TrackBar to MotorSpeed's 0 - 65,535 (100%).
			var leftMotor = (short)(LeftMotorTestTrackBar.Value / 100F * ushort.MaxValue);
			var rightMotor = (short)(RightMotorTestTrackBar.Value / 100F * ushort.MaxValue);
			LeftMotorTestTextBox.Text = string.Format("{0} % ", LeftMotorTestTrackBar.Value);
			RightMotorTestTextBox.Text = string.Format("{0} % ", RightMotorTestTrackBar.Value);
			lock (MainForm.XInputLock)
			{
				var gamePad = MainForm.Current.XiControllers[(int)MappedTo - 1];
				if (XInput.IsLoaded && gamePad.IsConnected)
				{
					var vibration = new Vibration();
					vibration.LeftMotorSpeed = leftMotor;
					vibration.RightMotorSpeed = rightMotor;
					gamePad.SetVibration(vibration);
				}
			}
			//UnsafeNativeMethods.Enable(false);
			//UnsafeNativeMethods.Enable(true);
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
			var description = JocysCom.ClassLibrary.ClassTools.EnumTools.GetDescription(MappedTo);
			var text = string.Format("Do you really want to clear all {0} settings?", description);
			var form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			var result = form.ShowForm(text, "Clear Controller Settings", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			if (result == DialogResult.Yes)
			{
				SettingsManager.Current.ClearPadSettings(MappedTo);
			}
		}

		void ResetPresetButton_Click(object sender, EventArgs e)
		{
			var description = JocysCom.ClassLibrary.ClassTools.EnumTools.GetDescription(MappedTo);
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
			var d = GetCurrentDevice();
			if (d == null) return;
			var description = JocysCom.ClassLibrary.ClassTools.EnumTools.GetDescription(MappedTo);
			var form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			var buttons = MessageBoxButtons.YesNo;
			var text = string.Format("Do you want to fill all {0} settings automatically?", description);
			if (d.Device == null)
			{
				text = string.Format("Device is offline. Please connect device to fill all {0} settings automatically.", description);
				buttons = MessageBoxButtons.OK;
			}
			var result = form.ShowForm(text, "Auto Controller Settings", buttons, MessageBoxIcon.Question);
			if (result == DialogResult.Yes)
			{
				SettingsManager.Current.ClearPadSettings(MappedTo);
				var objects = AppHelper.GetDeviceObjects(d.Device);
				DeviceObjectItem o = null;
				o = objects.FirstOrDefault(x => x.GuidValue == ObjectGuid.RxAxis);
				// If Right thumb triggers are missing then...
				if (o == null)
				{
					// Logitech RumblePad 2 USB
					AutoPreset(objects, SettingName.ButtonA, 1);
					AutoPreset(objects, SettingName.ButtonB, 2);
					AutoPreset(objects, SettingName.ButtonX, 0);
					AutoPreset(objects, SettingName.ButtonY, 3);
					AutoPreset(objects, SettingName.LeftShoulder, 4);
					AutoPreset(objects, SettingName.RightShoulder, 5);
					AutoPreset(objects, SettingName.ButtonBack, 8);
					AutoPreset(objects, SettingName.ButtonStart, 9);
					AutoPreset(objects, SettingName.LeftThumbButton, 10);
					AutoPreset(objects, SettingName.RightThumbButton, 11);
					// Triggers.
					AutoPreset(objects, SettingName.LeftTrigger, 6);
					AutoPreset(objects, SettingName.RightTrigger, 7);
					// Right Thumb.
					o = objects.FirstOrDefault(x => x.GuidValue == ObjectGuid.ZAxis);
					if (o != null) AutoPresetRead(SettingName.RightThumbAxisX, string.Format("{0}{1}", SettingName.SType.Axis, o.Instance + 1));
					o = objects.FirstOrDefault(x => x.GuidValue == ObjectGuid.RzAxis);
					if (o != null) AutoPresetRead(SettingName.RightThumbAxisY, string.Format("{0}-{1}", SettingName.SType.Axis, o.Instance + 1));
				}
				else
				{
					// ----------------------------------------------------------------------------------------------
					// Controller (Xbox One For Windows)
					// ----------------------------------------------------------------------------------------------
					// Offset   Usage  Instance  Guid           Name                            Flags                
					// ------  ------  --------  -------------  ------------------------------  ---------------------
					//      0      49         1  YAxis          Y Axis                          AbsoluteAxis         
					//      0       5         0  Unknown        Collection 0 - Game Pad         Collection, NoData   
					//      0       0         1  Unknown        Collection 1                    Collection, NoData   
					//      0       0         2  Unknown        Collection 2                    Collection, NoData   
					//      0       0         3  Unknown        Collection 3                    Collection, NoData   
					//      0     128         4  Unknown        Collection 4 - System Controls  Collection, NoData   
					//      4      48         0  XAxis          X Axis                          AbsoluteAxis         
					//      8      52         4  RyAxis         Y Rotation                      AbsoluteAxis         
					//     12      51         3  RxAxis         X Rotation                      AbsoluteAxis         
					//     16      50         2  ZAxis          Z Axis                          AbsoluteAxis         
					//     20      53         5  RzAxis         Z Rotation                      AbsoluteAxis         
					//     24      57         0  PovController  Hat Switch                      PointOfViewController
					//     32     151        19  Unknown        DC Enable Actuators             NoData, Output       
					//     36       1        20  Unknown        Physical Interface Device       NoData, Output       
					//     40     112        21  Unknown        Magnitude                       NoData, Output       
					//     44      80        22  Unknown        Duration                        NoData, Output       
					//     48     167        23  Unknown        Start Delay                     NoData, Output       
					//     52     124        24  Unknown        Loop Count                      NoData, Output       
					//     56       1         0  Button         Button 0                        PushButton           
					//     57       2         1  Button         Button 1                        PushButton           
					//     58       3         2  Button         Button 2                        PushButton           
					//     59       4         3  Button         Button 3                        PushButton           
					//     60       5         4  Button         Button 4                        PushButton           
					//     61       6         5  Button         Button 5                        PushButton           
					//     62       7         6  Button         Button 6                        PushButton           
					//     63       8         7  Button         Button 7                        PushButton           
					//     64       9         8  Button         Button 8                        PushButton           
					//     65      10         9  Button         Button 9                        PushButton           
					//     66     133        10  Button         System Main Menu                PushButton           
					AutoPreset(objects, SettingName.ButtonA, 0);
					AutoPreset(objects, SettingName.ButtonB, 1);
					AutoPreset(objects, SettingName.ButtonX, 2);
					AutoPreset(objects, SettingName.ButtonY, 3);
					AutoPreset(objects, SettingName.LeftShoulder, 4);
					AutoPreset(objects, SettingName.RightShoulder, 5);
					AutoPreset(objects, SettingName.ButtonBack, 6);
					AutoPreset(objects, SettingName.ButtonStart, 7);
					AutoPreset(objects, SettingName.LeftThumbButton, 8);
					AutoPreset(objects, SettingName.RightThumbButton, 9);
					// Triggers.
					o = objects.FirstOrDefault(x => x.GuidValue == ObjectGuid.ZAxis);
					if (o != null) AutoPresetRead(SettingName.LeftTrigger, string.Format("{0}{1}", SettingName.SType.Axis, o.Instance + 1));
					o = objects.FirstOrDefault(x => x.GuidValue == ObjectGuid.RzAxis);
					if (o != null) AutoPresetRead(SettingName.RightTrigger, string.Format("{0}{1}", SettingName.SType.Axis, o.Instance + 1));
					// Right Thumb.
					o = objects.FirstOrDefault(x => x.GuidValue == ObjectGuid.RxAxis);
					if (o != null) AutoPresetRead(SettingName.RightThumbAxisX, string.Format("{0}{1}", SettingName.SType.Axis, o.Instance + 1));
					o = objects.FirstOrDefault(x => x.GuidValue == ObjectGuid.RyAxis);
					if (o != null) AutoPresetRead(SettingName.RightThumbAxisY, string.Format("{0}-{1}", SettingName.SType.Axis, o.Instance + 1));
				}
				// Left Thumb.
				o = objects.FirstOrDefault(x => x.GuidValue == ObjectGuid.XAxis);
				if (o != null) AutoPresetRead(SettingName.LeftThumbAxisX, string.Format("{0}{1}", SettingName.SType.Axis, o.Instance + 1));
				o = objects.FirstOrDefault(x => x.GuidValue == ObjectGuid.YAxis);
				if (o != null) AutoPresetRead(SettingName.LeftThumbAxisY, string.Format("{0}-{1}", SettingName.SType.Axis, o.Instance + 1));
				// D-Pad
				o = objects.FirstOrDefault(x => x.GuidValue == ObjectGuid.PovController);
				if (o != null) AutoPresetRead(SettingName.DPad, string.Format("{0}{1}", SettingName.SType.DPad, o.Instance + 1));
			}
		}

		void AutoPreset(DeviceObjectItem[] objects, string settingName, int index)
		{
			var o = objects.FirstOrDefault(x => x.GuidValue == ObjectGuid.Button && x.Instance == index);
			if (o != null) AutoPresetRead(settingName, string.Format("{0}{1}", SettingName.SType.Button, o.Instance + 1));
		}

		void AutoPresetRead(string key, string value)
		{
			var pad = string.Format("PAD{0}", (int)MappedTo);
			var path = string.Format("{0}\\{1}", pad, key);
			var map = SettingsManager.Current.SettingsMap.FirstOrDefault(x => x.IniPath == path);
			if (map == null)
			{
				MessageBox.Show(string.Format("SettingsMap[IniPath='{0}'] not found!", path));
				return;
			}
			SettingsManager.Current.LoadSetting(map.Control, key, value);
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
				markR.Dispose();
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

		private void GeneralTabPage_SizeChanged(object sender, EventArgs e)
		{
			GeneralCenterPanel.Left = (this.Width - GeneralCenterPanel.Width) / 2;
		}

		private void GameControllersButton_Click(object sender, EventArgs e)
		{
			var path = System.Environment.GetFolderPath(Environment.SpecialFolder.System);
			path += "\\joy.cpl";
			OpenPath(path, "");
		}

		void OpenPath(string path, string arguments = null)
		{
			try
			{
				var fi = new System.IO.FileInfo(path);
				//if (!fi.Exists) return;
				// Brings up the "Windows cannot open this file" dialog if association not found.
				var psi = new ProcessStartInfo(path);
				psi.UseShellExecute = true;
				psi.WorkingDirectory = fi.Directory.FullName;
				psi.ErrorDialog = true;
				if (arguments != null) psi.Arguments = arguments;
				Process.Start(psi);
			}
			catch (Exception) { }
		}


		private void FeedVirtualDeviceCeckBox_CheckedChanged(object sender, EventArgs e)
		{
			FeedingEnabled = EnableVirtualDeviceCeckBox.Checked;
			if (EnableVirtualDeviceCeckBox.Checked)
			{
				var resourceName = Program.GetResourceName("vJoy", "vJoyInterface");
				if (!System.IO.File.Exists("vJoyInterface.dll"))
				{
					AppHelper.WriteFile(typeof(MainForm).Namespace + "." + resourceName + ".dll", "vJoyInterface.dll");
				}
				System.Threading.ThreadPool.QueueUserWorkItem(FeedWaitCallback, (uint)1);
			}
		}

		bool FeedingEnabled;

		void FeedWaitCallback(object state)
		{
			string message;
			var success = FeedXInputDevice((uint)state, out message);
			//if (!string.IsNullOrEmpty(message) && !success)
			//{
			//	MessageBox.Show(message);
			//}
		}


		LoadPresetsForm presetForm;

		private void LoadPresetButton_Click(object sender, EventArgs e)
		{
			if (presetForm == null)
			{
				presetForm = new LoadPresetsForm();
				presetForm.Owner = MainForm.Current;
			}
			presetForm.StartPosition = FormStartPosition.CenterParent;
			presetForm.InitForm();
			var result = presetForm.ShowDialog();
			if (result == DialogResult.OK)
			{
				var ps = presetForm.SelectedItem;
				if (ps != null)
				{
					MainForm.Current.UpdateTimer.Stop();
					SettingsManager.Current.LoadPadSettings(MappedTo, ps);
					MainForm.Current.UpdateTimer.Start();
				}
			}
			presetForm.UnInitForm();
		}

		#region Mapped Devices

		private void AddMapButton_Click(object sender, EventArgs e)
		{
			var items = MainForm.Current.ShowDeviceForm();
			if (items == null)
				return;
			foreach (var item in items)
			{
				var game = MainForm.Current.CurrentGame;
				if (game != null)
				{
					var setting = SettingsManager.GetSetting(item.InstanceGuid, game.FileName);
					if (setting == null)
					{
						var newSetting = AppHelper.GetNewSetting(item, game, MappedTo);
						SettingsManager.Settings.Items.Add(newSetting);
					}
					else
					{
						// Enable if not enabled.
						if (!setting.IsEnabled)
							setting.IsEnabled = true;
						setting.MapTo = (int)MappedTo;
					}
				}
			}
		}

		private void RemoveMapButton_Click(object sender, EventArgs e)
		{
			var grid = MappedDevicesDataGridView;
			var setting = GetCurrentSetting();
			if (setting != null)
			{
				setting.MapTo = (int)MapTo.Disabled;
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
			var grid = (DataGridView)sender;
			var viewRow = grid.Rows[e.RowIndex];
			var item = (Engine.Data.Setting)viewRow.DataBoundItem;
			if (e.ColumnIndex == grid.Columns[IsOnlineColumn.Name].Index)
			{
				e.Value = item.IsOnline
					? Properties.Resources.bullet_square_glass_green
					: Properties.Resources.bullet_square_glass_grey;
			}
			if (e.ColumnIndex == grid.Columns[InstanceIdColumn.Name].Index)
			{
				// Hide device Instance GUID from public eyes. Show part of checksum.
				e.Value = EngineHelper.GetID(item.InstanceGuid);
			}
			else if (e.ColumnIndex == grid.Columns[SettingIdColumn.Name].Index)
			{
				// Hide device Setting GUID from public eyes. Show part of checksum.
				e.Value = EngineHelper.GetID(item.PadSettingChecksum);
			}
			else if (e.ColumnIndex == grid.Columns[VendorNameColumn.Name].Index)
			{
				var device = SettingsManager.GetDevice(item.InstanceGuid);
				e.Value = device == null
					? ""
					: device.DevManufacturer;
			}
		}

		private void MappedDevicesDataGridView_SelectionChanged(object sender, EventArgs e)
		{
			var setting = GetCurrentSetting();
			var padSetting = setting == null
				? null
				: SettingsManager.GetPadSetting(setting.PadSettingChecksum);
			SettingsManager.Current.LoadPadSettings(MappedTo, padSetting);
			UpdateGridButtons();
		}

		private void MappedDevicesDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0) return;
			var grid = (DataGridView)sender;
			// If user clicked on the checkbox column then...
			if (e.ColumnIndex == grid.Columns[IsEnabledColumn.Name].Index)
			{
				var row = grid.Rows[e.RowIndex];
				var item = (Engine.Data.Setting)row.DataBoundItem;
				// Changed check (enabled state) of the current item.
				item.IsEnabled = !item.IsEnabled;
			}
		}

		#endregion

		private void AutoMapButton_Click(object sender, EventArgs e)
		{
			var game = MainForm.Current.CurrentGame;
			var mapFlag = AppHelper.GetMapFlag(MappedTo);
			var value = (MapToMask)game.AutoMapMask;
			var autoMap = value.HasFlag(mapFlag);
			// If AUTO enabled then...
			if (autoMap)
			{
				// Remove AUTO.
				game.AutoMapMask = (int)(value & ~mapFlag);
			}
			else
			{
				// Add AUTO.
				game.AutoMapMask = (int)(value | mapFlag);
			}
		}

	}
}
