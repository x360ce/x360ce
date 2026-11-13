using x360ce.App.DInput;
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
using x360ce.App.Forms;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	public partial class PadControl : UserControl, IPadControl
	{

		public PadControl(MapTo controllerIndex)
		{
			// Scaled from the size it is made at only once the numbers under the General page's names are on it, so Windows
			// Forms scales their room with the page's other sizes.
			SuspendLayout();
			InitializeComponent();
			var designMode = ControlsHelper.IsDesignMode(this);
			if (!designMode)
				AddValueLabels();
			ResumeLayout(false);
			if (designMode)
				return;
			// The room round the controls on the pages that scale themselves, given once the page is scaled: Windows Forms
			// scales such a control's own margin again each time a table round it resumes, so the designer's would be scaled
			// several times over.
			foreach (var axis in new[] { LeftTriggerUserControl, RightTriggerUserControl, LeftThumbXUserControl, LeftThumbYUserControl,
				RightThumbXUserControl, RightThumbYUserControl })
				axis.Margin = AppHelper.ScaledPadding(0, 0, 0, 4);
			LeftMouseStickPanel.Margin = AppHelper.ScaledPadding(0, 0, 12, 4);
			RightMouseStickPanel.Margin = AppHelper.ScaledPadding(0, 0, 0, 4);
			// The SDL page, after Direct Input: what SDL's controller database says about the selected device.
			SdlPanel = new SdlUserControl { Dock = DockStyle.Fill };
			SdlPanel.ApplyClicked += SdlPanel_ApplyClicked;
			SdlPanel.CopyClicked += SdlPanel_CopyClicked;
			SdlTabPage = new TabPage { Name = nameof(SdlTabPage), Text = "SDL", UseVisualStyleBackColor = true };
			SdlTabPage.Controls.Add(SdlPanel);
			// Added, with the pages after Direct Input added again behind it: Insert does nothing while the tab control
			// has no window yet, as here, so the page was in the list and never shown.
			var after = PadTabControl.TabPages.Cast<TabPage>().Skip(PadTabControl.TabPages.IndexOf(DirectInputTabPage) + 1).ToArray();
			foreach (var page in after)
				PadTabControl.TabPages.Remove(page);
			PadTabControl.TabPages.Add(SdlTabPage);
			PadTabControl.TabPages.AddRange(after);
			Global.UpdateControlFromStates += Global_UpdateControlFromStates;
			PadTabControl.Selected += (s, e) =>
			{
				if (e.TabPage == GeneralTabPage)
					HoldTables();
			};
			PadTabControl.SelectedIndexChanged += (s, e) => ReleaseTables();
			InputPanel.ChipClicked += InputPanel_ChipClicked;
			InputPanel.DraggingChanged += InputPanel_DraggingChanged;
			// Read only by the XInput library of x360ce 3, from the x360ce.ini file this version does not write. Shown and
			// greyed out, and kept with the settings, so what x360ce 3 keeps in the cloud stays as it is.
			AdvancedOptionsPanel.Enabled = false;
			DeviceGroupBox.Text = "Device - x360ce 3 only";
			CombineGroupBox.Text = "Combining - x360ce 3 only";
			AxisToDPadEnabledCheckBox.CheckedChanged += (s, e) => AxisToDPadChart.Enabled = AxisToDPadEnabledCheckBox.Checked;
			AxisToDPadChart.Enabled = AxisToDPadEnabledCheckBox.Checked;
			_AxisCharts = new[] { LeftTriggerUserControl, RightTriggerUserControl, LeftThumbXUserControl, LeftThumbYUserControl, RightThumbXUserControl, RightThumbYUserControl };
			_ChartReadings = _AxisCharts.Select(x => new ChartReading()).ToArray();

			RemapName = RemapAllButton.Text;
			// Remap All reads STOP while it runs, and stays as wide as the longer text so the list beside it stays put.
			RemapAllButton.Texts = new[] { RemapName, RemapStopName };
			MappedTo = controllerIndex;
			_Imager = new PadControlImager();
			_Imager.ImageControl = XboxImage;
			XboxImage.InitializeImages(imageInfos, _Imager, controllerIndex);
			XboxImage.StartRecording = StartRecording;
			XboxImage.StopRecording = StopRecording;
			FitValueLabels();
			// These three stay pressed to say that something is on, rather than doing something and
			// springing back. A bar cannot tell the two apart on its own, so it is said here: a screen
			// reader then announces the state, and the interface tree offers them to be set.
			EnableButton.AccessibleRole = AccessibleRole.CheckButton;
			AutoMapButton.AccessibleRole = AccessibleRole.CheckButton;
			GetXInputStatesCheckBox.AccessibleRole = AccessibleRole.CheckButton;
			// Hide left/right border.
			//MappedDevicesDataGridView.Width = this.Width + 2;
			//MappedDevicesDataGridView.Left = -1;
			JocysCom.ClassLibrary.Controls.ControlsHelper.ApplyBorderStyle(MappedDevicesDataGridView);
			// The icon columns show a short heading or none, as on the Devices page. A screen reader still hears the full name,
			// and the mouse finds it on the heading.
			IconColumnHeaderCell.Apply(IsOnlineColumn, "Online");
			IconColumnHeaderCell.Apply(ConnectionClassColumn, "Connection");
			UpdateScriptColumn();
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
			// A page comes up to date as it is shown, once the engine has states to show, rather than at the next update.
			VisibleChanged += (sender, e) =>
			{
				if (Visible && Global.DHelper?.PassCount > 0)
					Global_UpdateControlFromStates(this, EventArgs.Empty);
			};
		}

		/// <summary>Gives the page room to scroll when the window is shorter than its rows need.</summary>
		/// <remarks>
		/// The table gives the tabs all the height they ask for even when the window has less, and moves them up
		/// to find it: on a 1920 x 1080 screen at 150 % the tab strip slid under the tool bar and the device list,
		/// and no tab could be chosen. Scrolling keeps every row where it belongs.
		/// </remarks>
		protected override void OnLayout(LayoutEventArgs e)
		{
			var needed = NeededHeight(ClientSize.Width);
			if (AutoScrollMinSize.Height != needed)
				AutoScrollMinSize = new Size(0, needed);
			base.OnLayout(e);
		}

		/// <summary>The height the page's rows need to be shown whole: the tool bar, the device list, the tabs and the buttons.</summary>
		/// <remarks>
		/// What the page scrolls by when it is shorter, and what the main window first opens tall enough for
		/// (<see cref="MainForm.FitToPage"/>). The height comes from the rows themselves, so it follows the font, the theme and
		/// the screen's scale. The device list counts at its designed height, whatever devices it holds, and takes any height
		/// the page has to spare.
		/// </remarks>
		/// <param name="width">The width the page is laid out at.</param>
		public int NeededHeight(int width)
		{
			FitTabsToRows();
			return PadTableLayoutPanel.GetPreferredSize(new Size(width, 0)).Height;
		}

		/// <summary>
		/// Makes the controller pages as tall as the rows of the taller of the General and Buttons pages, and never shorter
		/// than they were drawn.
		/// </summary>
		/// <remarks>
		/// The tabs ask for the height they were drawn at, while the General page's table is as tall as its rows, whose boxes,
		/// names and numbers are as tall as the font makes them, and the Buttons page's fourteen rows as tall as their boxes.
		/// In a font taller than the one the pages were drawn in, the last row would run past the bottom of the page and be cut
		/// off. The height wanted is read from the rows, so it follows the font. The General page is given the room the tabs
		/// gain or lose, as the tabs give it once they lay their pages out: tabs not shown do not lay their pages out, and without
		/// it the next call would find the page as before and change the tabs again. The rows are measured again each time, as
		/// the font and the theme reach them: a measure taken before, in the font the page was made in, is taller than they
		/// need, and is not kept.
		/// </remarks>
		void FitTabsToRows()
		{
			if (_DrawnPageHeight == 0)
				_DrawnPageHeight = GeneralTabPage.ClientSize.Height;
			var general = GeneralTableLayoutPanel.GetPreferredSize(Size.Empty).Height + GeneralTableLayoutPanel.Margin.Vertical
				+ GeneralTabPage.Padding.Vertical;
			var buttons = AxisToButtonPanel.GetPreferredSize(Size.Empty).Height + ButtonsTabPage.Padding.Vertical;
			var change = Math.Max(_DrawnPageHeight, Math.Max(general, buttons)) - GeneralTabPage.ClientSize.Height;
			if (change == 0)
				return;
			PadTabControl.Height += change;
			GeneralTabPage.Height += change;
		}

		/// <summary>The height the controller pages were drawn at, at the screen's scale: they are never made shorter.</summary>
		int _DrawnPageHeight;

		/// <summary>How many updates a page that is not shown lets pass before it shows the states again.</summary>
		const int HiddenUpdateEvery = 10;
		int _HiddenUpdates;

		private void Global_UpdateControlFromStates(object sender, EventArgs e)
		{
			// Nobody sees a page that is not shown, so it keeps up at a tenth of the rate: what reads it without showing it,
			// an AI assistant among them, still gets values a second old at most.
			if (!Visible && ++_HiddenUpdates % HiddenUpdateEvery != 0)
				return;
			UpdateControlFromDInput();
			UpdateControlFromXInput();
			UpdateSpringNote();
			UpdateForceSource();
			UpdateWheelChart();
		}

		/// <summary>Whether the page's two largest tables hold their layout.</summary>
		bool _TablesHeld;

		/// <summary>Holds the layout of the page's two largest tables until <see cref="ReleaseTables"/>, which lays them out once.</summary>
		/// <remarks>
		/// Each auto-sized panel on them lays itself out as it comes into view and then asks its parent to, so one opening of
		/// the page laid these two out 67 times with nothing changing size, and building the page hundreds of times.
		/// </remarks>
		internal void HoldTables()
		{
			if (_TablesHeld)
				return;
			_TablesHeld = true;
			tableLayoutPanel1.SuspendLayout();
			GeneralTableLayoutPanel.SuspendLayout();
		}

		/// <summary>Lays out once what <see cref="HoldTables"/> held.</summary>
		internal void ReleaseTables()
		{
			if (!_TablesHeld)
				return;
			_TablesHeld = false;
			GeneralTableLayoutPanel.ResumeLayout();
			tableLayoutPanel1.ResumeLayout();
		}

		/// <summary>Enables what needs a device, when the selected row has one.</summary>
		/// <remarks>What needs one stays on its page, disabled, so the page looks the same with a device or without.</remarks>
		internal void UpdateEnabledFromDevice(UserDevice ud)
		{
			var enable = ud != null;
			ControlsHelper.SetEnabled(LoadPresetButton, enable);
			ControlsHelper.SetEnabled(SavePresetButton, enable);
			ControlsHelper.SetEnabled(AutoPresetButton, enable);
			ControlsHelper.SetEnabled(ClearPresetButton, enable);
			ControlsHelper.SetEnabled(ResetPresetButton, enable);
			ControlsHelper.SetEnabled(RemapAllButton, enable && ud.SourceState != null);
			var pages = PadTabControl.TabPages.Cast<TabPage>().ToArray();
			for (int p = 0; p < pages.Length; p++)
			{
				// Get first control to disable which must be Panel.
				var controls = pages[p].Controls.Cast<Control>().ToArray();
				for (int c = 0; c < controls.Length; c++)
					ControlsHelper.SetEnabled(controls[c], enable);
			}
		}

		private void UpdateControlFromDInput()
		{
			lock (updateFromDirectInputLock)
			{
				var ud = GetSelectedDevice();
				var instanceGuid = Guid.Empty;
				var enable = ud != null;
				if (enable)
					instanceGuid = ud.InstanceGuid;
				UpdateEnabledFromDevice(ud);
				// If device instance changed then...
				if (!Equals(instanceGuid, _InstanceGuid))
				{
					_InstanceGuid = instanceGuid;
					ResetDiMenuStrip(enable ? ud : null);
				}
				// Update direct input form and return actions (pressed Buttons/DPads, turned Axis/Sliders).
				UpdateDirectInputTabPage(ud);
				UpdatePluginTab(ud);
				DirectInputPanel.UpdateFrom(ud);
				// Drawn while it is shown, and again only when the device, its settings or the database changed.
				if (Visible && PadTabControl.SelectedTab == SdlTabPage)
					SdlPanel.UpdateFrom(ud, GetSelectedPadSetting());
				InputPanel.UpdateFrom(ud);
				if (enable && _Imager.Recorder.Recording)
				{
					// Stop recording if DInput value captured.
					var stopped = _Imager.Recorder.StopRecording(ud.SourceState);
					// If value was found and recording stopped then...
					if (stopped)
					{
						// Device not initialized yet.
						if (ud.SourceState == null)
							RecordAllMaps.Clear();
						if (RecordAllMaps.Count == 0)
						{
							if (ud.SourceState != null)
								XboxImage.SetHelpText(XboxImage.MappingDone);
							else
								XboxImage.SetHelpText("");
							RemapAllButton.Text = RemapName;
							return;
						}
						// The next control from the list after a pause, so the person sees what was recorded and which control is
						// next; it then waits for the device to come to rest before it compares. Stop meanwhile empties the list.
						ControlsHelper.BeginInvoke(() => StartRecording(), (int)RemapAllPause.TotalMilliseconds);
					}
				}
			}
		}

		/// <summary>Shows the controller's output on the General tab, and the row shown on its charts, from one engine pass.</summary>
		/// <remarks>
		/// The General tab shows what the controller is given, combined from all its rows, or Windows' readback of it while Show
		/// XInput State is on. Each trigger and stick chart shows the selected row's own pair: the reading the engine converted
		/// for the row and what the row made of it, so its dot stands on its curve. When no copy of one pass's states can be
		/// taken, everything shows what it showed.
		/// </remarks>
		internal void UpdateControlFromXInput()
		{
			var helper = Global.DHelper;
			// What the charts read, found before the engine's states are copied: the row shown, its place in the routing the
			// engine converts it by, and where each chart's reading comes from.
			var setting = GetSelectedSetting();
			var routing = DeviceRouting.Current;
			var row = setting == null ? -1 : Array.IndexOf(routing.Rows, setting);
			var ps = setting == null ? null : GetSelectedPadSetting();
			var maps = ps == null ? null : ps.Maps;
			for (var c = 0; c < _AxisCharts.Length; c++)
			{
				var target = _AxisCharts[c].TargetType;
				_ChartReadings[c].Find(maps == null ? null : maps.FirstOrDefault(x => x.Target == target), target, routing, row);
			}
			if (!CopyOnePass(helper, (int)MappedTo - 1, SettingsManager.Options.GetXInputStates, routing, row, setting))
				return;
			newState = _Copied.State;
			newConnected = _Copied.Connected;
			if (newConnected || oldConnected)
			{
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
						_Imager.DrawState(ii, newState.Gamepad, CurrentCbx);
				}
				// The numbers under the names, from the state the picture is drawn from.
				foreach (var ii in imageInfos)
					ii.ShowValue(newState.Gamepad);
				// Update Axis to Button Images.
				var AxisToButtonControls = AxisToButtonTableLayoutPanel.Controls.OfType<AxisToButtonUserControl>();
				foreach (var atbPanel in AxisToButtonControls)
					atbPanel.Refresh(newState, _Imager.markB);
				// Store old state.
				oldState = newState;
				oldConnected = newConnected;
			}
			if (setting == null)
			{
				AxisToDPadChart.ClearReading();
				return;
			}
			for (var c = 0; c < _AxisCharts.Length; c++)
				DrawChart(_AxisCharts[c], _ChartReadings[c], _Copied.Output);
			// Axis to D-Pad reads the X and Y axes the row was converted from, and lights what the engine pressed from them.
			if (_Copied.HasXY)
				AxisToDPadChart.SetReading(_Copied.X, _Copied.Y, _Copied.AxisButtons);
			else
				AxisToDPadChart.ClearReading();
		}

		/// <summary>Puts a row's pair on its chart, whichever way the row is driven: the reading the engine converted and what the row made of it.</summary>
		/// <remarks>
		/// A row driven by a formula names no single control, so it has no source number, and the
		/// chart used to be skipped entirely for one: the picture froze at whatever it last showed
		/// while the controller picture beside it kept moving. The formula is asked which control it
		/// reads, and that control's travel is the across of the chart, exactly as a plain mapping's
		/// own control is. A row the pass did not convert keeps the point it shows.
		/// </remarks>
		/// <param name="chart">Chart for this row.</param>
		/// <param name="reading">Where the chart's reading comes from, and what the pass copied.</param>
		/// <param name="output">What the row made, as the same pass converted it.</param>
		static void DrawChart(AxisMapUserControl chart, ChartReading reading, Gamepad output)
		{
			var map = reading.Map;
			chart.SetExpression(map == null ? null : map.Expression);
			chart.SetTuningEnabled(map != null && map.Expression == null && (map.IsAxis || map.IsSlider));
			if (!reading.HasInput)
				return;
			int produced;
			var trigger = false;
			switch (chart.TargetType)
			{
				case TargetType.LeftTrigger:
					produced = output.LeftTrigger;
					trigger = true;
					break;
				case TargetType.RightTrigger:
					produced = output.RightTrigger;
					trigger = true;
					break;
				case TargetType.LeftThumbX:
					produced = output.LeftThumbX;
					break;
				case TargetType.LeftThumbY:
					produced = output.LeftThumbY;
					break;
				case TargetType.RightThumbX:
					produced = output.RightThumbX;
					break;
				case TargetType.RightThumbY:
					produced = output.RightThumbY;
					break;
				default:
					return;
			}
			// A trigger on a control that reports movement reads the half past its middle, as the engine reads it.
			chart.DrawPoint(reading.Input, produced, map.IsInverted, map.IsHalf || trigger && reading.Moving);
		}

		/// <summary>The trigger and stick charts, each drawn for the target its <see cref="AxisMapUserControl.TargetType"/> names.</summary>
		AxisMapUserControl[] _AxisCharts;

		/// <summary>For each of <see cref="_AxisCharts"/>, where its reading comes from and what the last pass copied gave it.</summary>
		ChartReading[] _ChartReadings;

		/// <summary>Where a trigger or stick chart's reading comes from, worked out from the row's mapping, and the reading one pass gave it.</summary>
		sealed class ChartReading
		{
			/// <summary>The row's mapping for the chart's target, or null when it maps none.</summary>
			public Map Map;

			/// <summary>The axis or slider read, counted from one, or 0 when the row reads none for the chart: no mapping, a button, a POV, or a formula of buttons alone.</summary>
			public int Index;

			/// <summary>True when a slider is read, false for an axis.</summary>
			public bool Slider;

			/// <summary>True when a formula reads its control from another device, <c>c1.d2</c>, rather than the row's own.</summary>
			public bool Other;

			/// <summary>The device a formula names, or null when the routing reads none there.</summary>
			public UserDevice Named;

			/// <summary>The row's Mouse page stick that turns a control reporting movement into this stick's reading, or null.</summary>
			public MouseStick Stick;

			/// <summary>The stick's axis the chart shows: 0 across, 1 up and down.</summary>
			public int StickAxis;

			/// <summary>True when the pass copied converted the row and gave the chart a reading.</summary>
			public bool HasInput;

			/// <summary>The reading the engine converted, 0 to 65535.</summary>
			public int Input;

			/// <summary>True when the control reports movement rather than where it is.</summary>
			public bool Moving;

			/// <summary>Works out where the chart's reading comes from, as Step 3 reads it.</summary>
			/// <param name="map">The row's mapping for the chart's target, or null.</param>
			/// <param name="target">The chart's target.</param>
			/// <param name="routing">The routing the engine converts by.</param>
			/// <param name="row">The row's place in its <see cref="DeviceRouting.Rows"/>, or -1 when it is not routed.</param>
			public void Find(Map map, TargetType target, DeviceRouting routing, int row)
			{
				Map = map;
				Index = 0;
				Slider = false;
				Other = false;
				Named = null;
				Stick = null;
				StickAxis = 0;
				if (map == null)
					return;
				if (map.Expression != null)
				{
					var swept = MapExpressionUnits.GetSweptSource(map.Expression);
					// A formula of buttons alone has no travel to show, so there is nothing to place.
					if (!swept.HasValue)
						return;
					var source = swept.Value;
					Index = source.Index;
					Slider = source.Type == 's' || source.Type == 'h';
					Other = source.Pad != 0;
					Named = Other ? routing.GetDevice(source.Pad, source.Device) : null;
					return;
				}
				if (!map.IsAxis && !map.IsSlider)
					return;
				Index = map.Index;
				Slider = map.IsSlider;
				var left = target == TargetType.LeftThumbX || target == TargetType.LeftThumbY;
				var right = target == TargetType.RightThumbX || target == TargetType.RightThumbY;
				var mouse = row < 0 ? null : routing.RowMouse[row];
				if (mouse != null && (left || right))
				{
					Stick = mouse[left ? 0 : 1];
					StickAxis = target == TargetType.LeftThumbX || target == TargetType.RightThumbX ? 0 : 1;
				}
			}

			/// <summary>Copies the chart's reading from the state the row was converted from. Run while a pass's states are copied.</summary>
			/// <param name="input">The state the row was converted from, or null when the pass did not convert it.</param>
			/// <param name="device">The device that state was read from.</param>
			public void Copy(SourceState input, UserDevice device)
			{
				HasInput = false;
				if (input == null || Index <= 0)
					return;
				var state = Other ? DInputHelper.GetDeviceState(Named) : input;
				if (state == null)
					return;
				var values = Slider ? state.Sliders : state.Axis;
				if (Index > values.Length)
					return;
				var value = values[Index - 1];
				// A control that reports movement, read as Step 3 reads it: on a stick the Mouse page sets, the stick's answer.
				var moving = !Other && Map.Expression == null && ((Slider ? device.DiRelativeSliderMask : device.DiRelativeAxisMask) & (1 << (Index - 1))) != 0;
				if (moving && Stick != null && device.SourceStateRead != null)
					value = Stick.LastValue(StickAxis);
				Input = value;
				Moving = moving;
				HasInput = true;
			}
		}

		/// <summary>What the page shows of one engine pass, as <see cref="CopyOnePass"/> copied it last.</summary>
		sealed class PassCopy
		{
			/// <summary>The controller's output as the General tab shows it, and whether the controller is there.</summary>
			public State State;
			public bool Connected;

			/// <summary>What the row shown made, <see cref="UserSetting.XiState"/>, while it is routed.</summary>
			public Gamepad Output;

			/// <summary>The buttons the row's axes held down, <see cref="UserSetting.AxisButtons"/>, while it is routed.</summary>
			public GamepadButtonFlags AxisButtons;

			/// <summary>True when the row was converted from a state with an X and a Y, which Axis to D-Pad reads: <see cref="X"/> and <see cref="Y"/>.</summary>
			public bool HasXY;
			public int X;
			public int Y;
		}

		readonly PassCopy _Copied = new PassCopy();

		/// <summary>How many copies one update takes, at most, before the page keeps what it shows.</summary>
		const int PassCopyAttempts = 3;

		/// <summary>How long one update waits, at most, for passes under way to end, in <see cref="System.Diagnostics.Stopwatch"/> ticks: a millisecond, far longer than a pass takes.</summary>
		static readonly long PassWaitTicks = System.Diagnostics.Stopwatch.Frequency / 1000;

		/// <summary>Copies what the page shows from the states of one engine pass, never of two.</summary>
		/// <remarks>
		/// <para>
		/// The engine writes them on its own thread up to a thousand times a second, and refills a device's reading in place,
		/// while the page draws ten times a second. Read at two moments, a chart put one pass's reading beside another's
		/// output, or Windows' readback of the controller from up to a tenth of a second before, and its dot left its curve
		/// whenever the stick moved fast. A copy taken while <see cref="DInputHelper.PassStarted"/> equals
		/// <see cref="DInputHelper.PassCount"/>, and still does once it is taken, is all one pass's.
		/// </para>
		/// <para>
		/// The engine never waits: a pass that starts meanwhile spoils the copy, and it is taken again. The page waits for a
		/// pass under way to end, giving the processor up meanwhile, and copies as it ends, so an update that comes during a
		/// pass still shows something new. While passes run back to back, as they do when each takes longer than the polling
		/// interval, a copy seldom fits between two, and the page goes on showing the last pass it copied.
		/// </para>
		/// </remarks>
		/// <param name="helper">The engine.</param>
		/// <param name="pad">The controller's index, from 0.</param>
		/// <param name="readBack">True to copy Windows' readback of the controller, false for its combined output.</param>
		/// <param name="routing">The routing the engine converts by.</param>
		/// <param name="row">The place in it of the row shown, or -1 when the row is not routed or none is shown.</param>
		/// <param name="setting">The row shown, or null.</param>
		/// <returns>False when no copy was all one pass's.</returns>
		bool CopyOnePass(DInputHelper helper, int pad, bool readBack, DeviceRouting routing, int row, UserSetting setting)
		{
			var copy = _Copied;
			var copies = 0;
			long due = 0;
			while (true)
			{
				var started = helper.PassStarted;
				// A pass is under way, writing what would be copied: its end is waited for, the processor given up meanwhile.
				if (started != helper.PassCount)
				{
					var now = System.Diagnostics.Stopwatch.GetTimestamp();
					if (due == 0)
						due = now + PassWaitTicks;
					else if (now > due)
						return false;
					System.Threading.Thread.Yield();
					continue;
				}
				if (readBack)
				{
					// Windows decides which of the four places a new controller goes into and does not say,
					// so the place is looked up rather than assumed to match the controller number. On a
					// computer with all four places empty this program's first controller was put in the
					// third; reading the first place showed a controller that was never there.
					var place = helper.XiPlaceForPad[pad];
					// No place, or one not known: nothing XInput reads back is this tab's controller. Read by the tab's
					// number, a real controller holding that place was drawn as this tab's, idle while its device moved.
					copy.State = place < 0 ? new State() : helper.LiveXiStates[place];
					copy.Connected = place >= 0 && helper.LiveXiConnected[place];
				}
				else
				{
					copy.State = helper.CombinedXiStates[pad];
					copy.Connected = helper.CombinedXiConencted[pad];
				}
				// The state the row was converted from, from the device it was read through, as Step 3 chose them.
				SourceState input = null;
				var device = row < 0 ? null : routing.RowDevices[row];
				if (device != null)
					input = DInputHelper.GetConvertedState(ref device, routing, row);
				if (row >= 0)
				{
					copy.Output = setting.XiState;
					copy.AxisButtons = setting.AxisButtons;
				}
				for (var c = 0; c < _ChartReadings.Length; c++)
					_ChartReadings[c].Copy(input, device);
				copy.HasXY = input != null && input.Axis.Length >= 2;
				if (copy.HasXY)
				{
					copy.X = input.Axis[0];
					copy.Y = input.Axis[1];
				}
				// What was copied is read before the mark is read again.
				System.Threading.Interlocked.MemoryBarrier();
				if (helper.PassStarted == started)
					return true;
				if (++copies == PassCopyAttempts)
					return false;
			}
		}


		public bool StopRecording()
		{
			RecordAllMaps.Clear();
			RemapAllButton.Text = RemapName;
			return _Imager.Recorder.StopRecording();
		}

		/// <summary>Keeps a key pressed while recording from pressing a button on this page.</summary>
		/// <remarks>
		/// The buttons answer to their underlined letters on their own, without Alt, whenever
		/// nothing else takes the key. Recording takes keys from the keyboard as a device and
		/// nothing else, so L, pressed to map it, opened Load Preset over the recording instead.
		/// R, C and A would have reset, cleared or replaced the preset the same way.
		/// </remarks>
		protected override bool ProcessMnemonic(char charCode)
		{
			if (_Imager != null && _Imager.Recorder.Recording)
				return true;
			return base.ProcessMnemonic(charCode);
		}

		void StartRecording(SettingsMapItem map = null)
		{
			// A control of Remap All is compared once the device is back as it was when Remap All began.
			SourceState rest = null;
			if (map == null)
			{
				map = RecordAllMaps.FirstOrDefault();
				if (map == null)
					return;
				RecordAllMaps.Remove(map);
				rest = RemapRest;
			}
			var cbx = (ComboBox)map.Control;
			if (_CurrentCbx != cbx)
				_CurrentCbx = cbx;
			_Imager.Recorder.StartRecording(map, rest);
			var helpText =
				SettingsConverter.ThumbDirections.Contains(map.Code) ||
				SettingsConverter.TriggerButtonCodes.Contains(map.Code)
					? "Move Axis"
					: "Press Button";
			XboxImage.SetHelpText(helpText);
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
			if (e.Item.Control == ForceSpringEnableCheckBox || e.Item.Control == WheelRangeComboBox)
				KeepRangeForSpring();
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
					ForceSpringEnableCheckBox,
					ForceSpringStrengthTrackBar,
					ForceSpringCentreTrackBar,
					ForceSpringPeakTrackBar,
					ForceSpringLighteningTrackBar,
					ForceSpringDampingTrackBar,
					ForceSpringSpeedTrackBar,
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
				SendVibration(MappedTo, LeftMotorTestTrackBar.Value, RightMotorTestTrackBar.Value);
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
				else if (e.PropertyDescriptor.Name == nameof(Options.AllowAutoHotkeyScripts))
				{
					UpdateScriptColumn();
				}
			}
		}

		/// <summary>Shows the AHK column while AutoHotkey scripts are allowed, the only time a script can use the names in it.</summary>
		void UpdateScriptColumn()
		{
			ScriptNameColumn.Visible = SettingsManager.Options.AllowAutoHotkeyScripts;
		}

		/// <summary>A column heading that shows a short word or none, over a column of icons, and still names the column.</summary>
		/// <remarks>
		/// A grid gives a screen reader the heading's text as the column's name, so a blank heading left the column, and every
		/// cell under it, without one. This heading gives the full name instead, and shows it as the heading's tooltip.
		/// </remarks>
		sealed class IconColumnHeaderCell : DataGridViewColumnHeaderCell
		{
			/// <summary>The column's name as a screen reader says it.</summary>
			public string FullName { get; set; }

			/// <summary>Gives the column this heading, keeping the text the designer gave it.</summary>
			public static void Apply(DataGridViewColumn column, string fullName)
			{
				var text = column.HeaderText;
				column.HeaderCell = new IconColumnHeaderCell { FullName = fullName };
				column.HeaderText = text;
				column.ToolTipText = fullName;
			}

			public override object Clone()
			{
				var clone = (IconColumnHeaderCell)base.Clone();
				clone.FullName = FullName;
				return clone;
			}

			protected override AccessibleObject CreateAccessibilityInstance()
			{
				return new NamedAccessibleObject(this);
			}

			sealed class NamedAccessibleObject : DataGridViewColumnHeaderCellAccessibleObject
			{
				public NamedAccessibleObject(IconColumnHeaderCell owner) : base(owner) { }

				public override string Name
				{
					get { return ((IconColumnHeaderCell)Owner).FullName; }
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
				ControlsHelper.SetImage(GetXInputStatesCheckBox, o.GetXInputStates
				   ? Properties.Resources.checkbox_32x32
				   : Properties.Resources.checkbox_unchecked_32x32);
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
			// The main window builds its pages before it reads the settings and chooses the game, and a change of game
			// reaches a page only once it is given its settings, so the page takes the game's switches here.
			UpdateFromCurrentGame();
			// WORKAROUND: Remove SelectionChanged event.
			MappedDevicesDataGridView.SelectionChanged -= MappedDevicesDataGridView_SelectionChanged;
			MappedDevicesDataGridView.DataSource = mappedItems;
			// WORKAROUND: Use BeginInvoke to prevent SelectionChanged firing multiple times.
			ControlsHelper.BeginInvoke(() =>
			{
				MapNameComboBox.DataSource = SettingsManager.Layouts.Items;
				MapNameComboBox.DisplayMember = nameof(x360ce.Engine.Data.Layout.Name);
				// A list selects its first row by itself only once its tab has been shown. Selected here, the
				// page holds its device's settings from the start, so whatever reads a tab nobody has opened,
				// an AI assistant among them, gets the real values rather than blank defaults.
				ControlsHelper.RestoreSelection(MappedDevicesDataGridView, nameof(UserSetting.InstanceGuid), new List<Guid>());
				MappedDevicesDataGridView.SelectionChanged += MappedDevicesDataGridView_SelectionChanged;
				MappedDevicesDataGridView_SelectionChanged(MappedDevicesDataGridView, new EventArgs());
				// Before the page shows, and before its controls have windows, rather than on the first update after it has appeared.
				UpdateEnabledFromDevice(GetSelectedDevice());
			});
			UserSettings_Items_ListChanged(null, null);
			SettingsManager.UserSettings.Items.ListChanged += UserSettings_Items_ListChanged;
		}

		public void InitPadControl()
		{
			var dv = new System.Data.DataView();
			var grid = MappedDevicesDataGridView;
			grid.AutoGenerateColumns = false;
			// Without a context of its own, each window made above the list would rebuild its rows and load the selection again.
			grid.BindingContext = BindingContext;
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
			InitMotorPeriodPresets();

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
			// Where force feedback is sent when it is passed through. Nothing but the four places and
			// working it out, so it is written here rather than taken from the list of players beside
			// it - that one is built from an enum whose "any" member is 255, and this holds one digit.
			var forcePlaces = new List<KeyValuePair>();
			forcePlaces.Add(new KeyValuePair("Auto (Real)", "0"));
			for (var place = 1; place <= 4; place++)
				forcePlaces.Add(new KeyValuePair(string.Format("XInput {0}", place), place.ToString()));
			ForcePassThroughIndexComboBox.DataSource = new BindingSource(forcePlaces, null);
			ForcePassThroughIndexComboBox.DisplayMember = nameof(KeyValuePair.Key);
			ForcePassThroughIndexComboBox.ValueMember = nameof(KeyValuePair.Value);
			// The ranges a Logitech wheel takes, named by the cars each one suits. The wheel powers
			// up at 200 and stays there until told otherwise, which is what the first choice leaves.
			// The centering spring needs a range, so the first choice is for a wheel without it.
			WheelRangeComboBox.DisplayMember = nameof(KeyValuePair.Key);
			WheelRangeComboBox.ValueMember = nameof(KeyValuePair.Value);
			WheelRangeComboBox.Items.Add(new KeyValuePair("Leave as it is (spring off)", "0"));
			WheelRangeComboBox.Items.Add(new KeyValuePair("900° - Road and sports cars", "900"));
			WheelRangeComboBox.Items.Add(new KeyValuePair("720° - Drift and rally cars", "720"));
			WheelRangeComboBox.Items.Add(new KeyValuePair("540° - GT and WRC cars", "540"));
			WheelRangeComboBox.Items.Add(new KeyValuePair("360° - Formula cars", "360"));
			WheelRangeComboBox.Items.Add(new KeyValuePair("270° - Arcade", "270"));
			WheelRangeComboBox.Items.Add(new KeyValuePair("200° - As powered up", "200"));
			// The cars the Car list offers, then Custom for values that match none of them.
			ForceSpringCarComboBox.Items.AddRange(SpringCars);
			ForceSpringCarComboBox.Items.Add(CustomSpringCar);
			ShowSpringCar();
			UpdateSpringChart();
			PassThroughIndexComboBox.DisplayMember = nameof(KeyValuePair.Key);
			PassThroughIndexComboBox.ValueMember = nameof(KeyValuePair.Value);
			CombinedIndexComboBox.DataSource = new BindingSource(playerOptions, null);  // Otherwise changing one changes the other
			CombinedIndexComboBox.DisplayMember = nameof(KeyValuePair.Key);
			CombinedIndexComboBox.ValueMember = nameof(KeyValuePair.Value);
			// Attach drop down menu with record and map choices.
			var comboBoxes = new List<ComboBox>();
			GetAllControls(GeneralTabPage, ref comboBoxes);
			// Exclude map name combobox
			comboBoxes.Remove(MapNameComboBox);
			// Attach context strip with button names to every ComboBox on general tab, and let the
			// input panel's chips land in it, by click on the box last focused or by drop.
			foreach (var cb in comboBoxes)
			{
				cb.DropDown += ComboBox_DropDown;
				cb.Enter += MappingBox_Enter;
				cb.AllowDrop = true;
				cb.DragEnter += MappingBox_DragEnter;
				cb.DragDrop += MappingBox_DragDrop;
			}
			_MappingBoxes = comboBoxes.ToArray();
			if (_MappingBoxes.Length > 0)
				_MappingBoxColor = _MappingBoxes[0].BackColor;
		}

		/// <summary>The mapping box a clicked chip goes into: the one last focused on the General tab.</summary>
		ComboBox _ChipTargetCbx;

		/// <summary>Every mapping box on the General tab: where a dragged chip may land.</summary>
		ComboBox[] _MappingBoxes = new ComboBox[0];
		Color _MappingBoxColor;

		/// <summary>Tints the drop targets while a chip is in the air, and puts their colour back when it lands.</summary>
		void InputPanel_DraggingChanged(object sender, EventArgs<bool> e)
		{
			foreach (var cb in _MappingBoxes)
				cb.BackColor = e.Data ? DropTargetColor : _MappingBoxColor;
		}

		/// <summary>The colour of a box a chip may be dropped into: the lit green of the chips themselves.</summary>
		static readonly Color DropTargetColor = ColorTranslator.FromHtml(AppHelper.StatusGreen);

		void MappingBox_Enter(object sender, EventArgs e)
		{
			_ChipTargetCbx = (ComboBox)sender;
		}

		/// <summary>
		/// A chip clicked while a mapping box has focus fills that box, the way choosing the same
		/// item from its list does. While a mapping is being recorded the device itself is being
		/// listened to, so a click is ignored rather than raced against it.
		/// </summary>
		void InputPanel_ChipClicked(object sender, EventArgs<InputChip> e)
		{
			if (_Imager.Recorder.Recording)
				return;
			var target = _ChipTargetCbx;
			if (target == null || target.IsDisposed)
				return;
			SettingsManager.Current.SetComboBoxValue(target, e.Data.Payload);
		}

		void MappingBox_DragEnter(object sender, DragEventArgs e)
		{
			e.Effect = e.Data.GetDataPresent(DataFormats.Text) ? DragDropEffects.Copy : DragDropEffects.None;
		}

		void MappingBox_DragDrop(object sender, DragEventArgs e)
		{
			if (!e.Data.GetDataPresent(DataFormats.Text))
				return;
			var text = (string)e.Data.GetData(DataFormats.Text);
			MapType type;
			int index;
			// Only a mapping is accepted, so a stray drop cannot leave arbitrary text in a box.
			if (!SettingsConverter.TryParseTextValue(text, out type, out index))
				return;
			SettingsManager.Current.SetComboBoxValue((ComboBox)sender, text);
		}

		/// <summary>Shows the game's switch and rows for the controller on the page.</summary>
		/// <param name="select">The device row to select; null to keep the selection the list has.</param>
		public void UpdateFromCurrentGame(Guid? select = null)
		{
			var game = SettingsManager.CurrentGame;
			var flag = AppHelper.GetMapFlag(MappedTo);
			// Update Virtual.
			var virt = game != null && ((MapToMask)game.EnableMask).HasFlag(flag);
			EnableButton.Checked = virt;
			ControlsHelper.SetImage(EnableButton, virt
				? x360ce.App.Properties.Resources.checkbox_32x32
				: x360ce.App.Properties.Resources.checkbox_unchecked_32x32);
			// The theme's window colour: the designer records plain white, which a dark theme does not change.
			MappedDevicesDataGridView.BackgroundColor = SystemColors.Window;
			MappedDevicesDataGridView.DefaultCellStyle.BackColor = SystemColors.Window;
			ShowHideAndSelectGridRows(select);
			UpdateGridButtons();
			UpdateForceSource();
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

		/// <summary>Each row's device number on its controller, which the AHK column shows.</summary>
		Dictionary<Engine.Data.UserSetting, int> _deviceNumbers = new Dictionary<Engine.Data.UserSetting, int>();

		/// <summary>True while the rows are being rebuilt.</summary>
		bool _RowsUpdating;

		/// <summary>A rebuild was asked for during a rebuild; it runs when the current one ends.</summary>
		bool _RowsUpdatePending;

		/// <summary>
		/// Brings the mapped-devices list in line with the settings, one pass at a time.
		/// </summary>
		/// <remarks>
		/// The grid selects a row while it inserts it, selecting loads that device's settings into
		/// the form, the load writes to the setting, and the settings list reports that as a change
		/// back here. Re-entered from inside the grid's own insert, this added a row twice and left
		/// the grid measuring a row it had not finished building, which closed the program. A call
		/// that arrives during a pass is remembered and applied after it, and the selection is
		/// announced once the rows are in place rather than while they move.
		/// </remarks>
		void ShowHideAndSelectGridRows(Guid? instanceGuid = null)
		{
			lock (DevicesToMapDataGridViewLock)
			{
				if (_RowsUpdating)
				{
					_RowsUpdatePending = true;
					return;
				}
				_RowsUpdating = true;
				try
				{
					do
					{
						_RowsUpdatePending = false;
						UpdateGridRows(instanceGuid);
						instanceGuid = null;
					}
					while (_RowsUpdatePending);
				}
				finally
				{
					_RowsUpdating = false;
				}
			}
		}

		void UpdateGridRows(Guid? instanceGuid)
		{
			var grid = MappedDevicesDataGridView;
			var game = SettingsManager.CurrentGame;
			var settings = SettingsManager.UserSettings.ItemsToArraySynchronized();
			// The number a script names each device by, counted as the engine counts it.
			_deviceNumbers = DInput.DeviceRouting.GetDeviceNumbers(game, settings);
			// Get rows which must be displayed on the list.
			var itemsToShow = settings
				// Filter devices by controller.
				.Where(x => x.MapTo == (int)MappedTo)
				// Filter devices by selected game (no items will be shown if game is not selected).
				.Where(x => game != null && x.FileName == game.FileName && x.FileProductName == game.FileProductName)
				.ToList();
			var itemsToRemove = mappedItems.Except(itemsToShow).ToArray();
			var itemsToInsert = itemsToShow.Except(mappedItems).ToArray();

			// If rows come or go, or a device is asked to be selected, then... The rows of a device just
			// mapped are usually in place already, put there by the settings list's own change.
			if (itemsToRemove.Length > 0 || itemsToInsert.Length > 0 || instanceGuid.HasValue)
			{
				var selection = instanceGuid.HasValue
					? new List<Guid>() { instanceGuid.Value }
					: JocysCom.ClassLibrary.Controls.ControlsHelper.GetSelection<Guid>(grid, nameof(UserSetting.InstanceGuid));
				grid.CurrentCell = null;
				// Suspend Layout.
				grid.SuspendLayout();
				// BindingContext is null until the control belongs to a form, and the manager
				// is absent when the grid is not bound.
				var cm = grid.DataSource == null ? null : BindingContext?[grid.DataSource] as CurrencyManager;
				cm?.SuspendBinding();
				try
				{
					// Do removal.
					foreach (var item in itemsToRemove)
						mappedItems.Remove(item);
					// Do adding.
					foreach (var item in itemsToInsert)
						mappedItems.Add(item);
				}
				finally
				{
					// Resume CurrencyManager and Layout. This must run even when the update
					// above failed: a grid left suspended throws on every later operation, so
					// one error would turn into a permanently broken control.
					cm?.ResumeBinding();
					grid.ResumeLayout();
				}
				// Restore selection. Losing the selected row is cosmetic, so a failure here
				// must not reach the caller. The grid reports a reentrant call when selection
				// is set while it is still settling after rows were added or removed.
				try
				{
					JocysCom.ClassLibrary.Controls.ControlsHelper.RestoreSelection(grid, nameof(UserSetting.InstanceGuid), selection);
				}
				catch (InvalidOperationException)
				{
				}
				// The rows are in place, so the selection can be acted on. A list not bound yet does so once it is.
				if (grid.DataSource != null)
					AnnounceSelection();
			}
			var visibleCount = mappedItems.Count();
			var title = string.Format("Enable {0} Mapped Device{1}", visibleCount, visibleCount == 1 ? "" : "s");
			if (mappedItems.Count(x => x.IsEnabled) > 1)
			{
				title += " (Combine)";
			}
			ControlsHelper.SetText(EnableButton, title);
			// Another tab's list can change without this one's rows moving.
			UpdateForceFeedbackTitle();
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

		/// <summary>Box the mapping menu was last opened for.</summary>
		/// <remarks>
		/// One menu serves every box on the pad, and a click on one of its items is handled after
		/// the menu has closed - so after anything else that happened in between. CurrentCbx is
		/// cleared by opening the same list a second time, by recording, and by the previous click,
		/// any of which can happen first, leaving the click with nothing to write into. Keeping the
		/// target here means the menu and the box it belongs to cannot disagree.
		/// </remarks>
		internal ComboBox MenuTargetCbx;

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
				else
					ShowAdjustFor(cbx);
				MenuTargetCbx = cbx;
				menu.Show(cbx, new Point(0, cbx.Height), ToolStripDropDownDirection.Default);
			}
			if (cbx.Items.Count > 0)
				cbx.SelectedIndex = 0;
		}

		/// <summary>Offers the [Adjust] items in the mapping menu only where they can do something for the box.</summary>
		/// <remarks>
		/// [Invert] shows while the box holds a control that has another way round: a button, an axis and a slider each
		/// have one, a D-Pad, a formula and an empty box do not. [Round Dead Zone] shows on a stick's X and Y boxes.
		/// </remarks>
		/// <param name="cbx">The box the menu is about to open for.</param>
		public void ShowAdjustFor(ComboBox cbx)
		{
			var invertible = cbx != null && SettingsConverter.InvertTextValue(cbx.Text) != null;
			var stick = cbx != null && StickOf(cbx) != null;
			foreach (var adjust in DiMenuStrip.Items.OfType<ToolStripMenuItem>().Where(x => x.Text == cAdjust))
			{
				foreach (ToolStripItem item in adjust.DropDownItems)
				{
					if (item.Text == cInvert)
						item.Visible = invertible;
					else if (item.Text == cRoundDeadZone)
						item.Visible = stick;
				}
			}
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
					var triggerRight = new Point(XboxImageUserControl.CanvasWidth - triggerLeft.X - 1, triggerLeft.Y);
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
					_imageInfos.Add(2, MapCode.ButtonGuide, 127, 48, ButtonGuideLabel, ButtonGuideComboBox, GamepadButtonFlags.Guide);
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

		/// <summary>
		/// Puts a small green number under each control's name on the General page: the emulated controller's value for that
		/// part, the value the controller picture is drawn from, as x360ce 5 shows it.
		/// </summary>
		/// <remarks>
		/// One is made for each name in <see cref="imageInfos"/>, under it in the name's panel, which holds the pair in one cell
		/// of the General page's table, in the middle of its box's row. No second list is kept in step with that one. Each is
		/// named after its part, as LeftTriggerValueLabel, which is how the interface catalog and the AI door find it.
		/// <para>
		/// A number is drawn at the side its name is drawn at, the side next to the name's box (the right side on the right of
		/// the page), 2 px in from the name's edge at 100 %, the size the page is made at, where its digits line up with the
		/// name's letters (the owner's item 4), which Windows Forms scales with the page's other sizes: 3 px at 150 %.
		/// </para>
		/// <para>
		/// The name is docked to the top of the panel and its number to the bottom, under the name in the z-order, and the
		/// panel is <see cref="ValueOverlap"/> shorter than the two: the empty top of the number's line lies under the name's
		/// last rows, so its digits stand <see cref="ValueGap"/> clear rows under the name's letters.
		/// </para>
		/// <para>
		/// The numbers are made before Windows Forms scales the page from the size it is made at to the screen's, so their
		/// padding is scaled with it, and fitted to their fonts once it has been (<see cref="FitValueLabels"/>).
		/// </para>
		/// </remarks>
		void AddValueLabels()
		{
			foreach (var ii in imageInfos)
			{
				var name = (Label)ii.Label;
				var right = name.TextAlign == ContentAlignment.TopRight;
				var value = new Label
				{
					Name = ii.Code + "ValueLabel",
					AutoSize = false,
					Dock = DockStyle.Bottom,
					TextAlign = name.TextAlign,
					Padding = right ? new Padding(0, 0, 2, 0) : new Padding(2, 0, 0, 0),
					UseMnemonic = false,
					// Measured as it is drawn, with GDI, as the program draws all its text.
					UseCompatibleTextRendering = false,
					Text = "0",
				};
				ii.ValueLabel = value;
				// Added after its name, so the name is drawn over the number where the two meet.
				name.Parent.Controls.Add(value);
			}
		}

		/// <summary>
		/// Sizes each name and its number to their fonts, and again whenever the name's text or font changes, and colours the
		/// numbers in the theme's colour, in each theme.
		/// </summary>
		void FitValueLabels()
		{
			// Laid out once, as the page is shown: a panel that grows would lay out the table it is in, for every number.
			GeneralTableLayoutPanel.SuspendLayout();
			foreach (var ii in imageInfos)
			{
				var name = (Label)ii.Label;
				name.Parent.SuspendLayout();
				name.AutoSize = false;
				SetValueFont(ii);
				name.TextChanged += (sender, e) => FitPair(ii);
				name.FontChanged += (sender, e) => SetValueFont(ii);
				name.Parent.ResumeLayout(true);
			}
			GeneralTableLayoutPanel.ResumeLayout(false);
			SetValueColors();
			// The theme draws the names, numbers and captions once the page is on a themed window, which is before Windows makes it.
			HandleCreated += (sender, e) =>
			{
				FitPairs();
				ShareAdvancedCaptions();
			};
			ShareAdvancedCaptions();
			EventHandler themeChanged = (sender, e) => SetValueColors();
			JocysCom.ClassLibrary.Controls.Themes.FormsTheme.ThemeChanged += themeChanged;
			Disposed += (sender, e) => JocysCom.ClassLibrary.Controls.Themes.FormsTheme.ThemeChanged -= themeChanged;
		}

		/// <summary>
		/// The Advanced page's groups share one caption column, as the AI page's sections do, so every box on the page starts
		/// at one place.
		/// </summary>
		void ShareAdvancedCaptions()
		{
			var tables = new[] { DeviceTableLayoutPanel, CombineTableLayoutPanel };
			foreach (var table in tables)
				table.SuspendLayout();
			JocysCom.ClassLibrary.Controls.Themes.FormsTheme.ShareLabelWidth(tables.SelectMany(table => table.Controls.OfType<Label>()
				.Where(label => table.GetColumn(label) == 0 && table.GetColumnSpan(label) == 1)));
			foreach (var table in tables)
				table.ResumeLayout();
		}

		/// <summary>Sizes every name, its number and their panel again, as their labels are drawn now.</summary>
		void FitPairs()
		{
			GeneralTableLayoutPanel.SuspendLayout();
			foreach (var ii in imageInfos)
				FitPair(ii);
			GeneralTableLayoutPanel.ResumeLayout(true);
		}

		/// <summary>Sizes a number from its name's font: four fifths of it, the size the Input column's readings use.</summary>
		static void SetValueFont(ImageInfo ii)
		{
			var font = ii.Label.Font;
			ii.ValueLabel.Font = new Font(font.FontFamily, font.Size * 0.8f, font.Style, font.Unit);
			FitPair(ii);
		}

		/// <summary>How tall a label is: one line of its font.</summary>
		/// <remarks>
		/// A label left to size itself is about a third taller than its line, room under the text that holds nothing. A name
		/// and its number at that height would be taller than a row of boxes, and would push the rows apart.
		/// </remarks>
		static int LineHeight(Label label)
		{
			return TextRenderer.MeasureText(" ", label.Font, Size.Empty, TextFormatFlags.NoPadding).Height;
		}

		/// <summary>The clear rows between a name's baseline and the digits of the number under it, at the screen's scale.</summary>
		static int ValueGap
		{
			get { return GapAt(ControlsHelper.DpiScale); }
		}

		/// <summary>
		/// The clear rows between a name's baseline and the digits of the number under it at a scale: the owner's 4 px at
		/// 100 %, so 5 at 125 %, 6 at 150 %, 7 at 175 % and 8 at 200 %.
		/// </summary>
		internal static int GapAt(float scale)
		{
			return (int)Math.Round(4 * scale, MidpointRounding.AwayFromZero);
		}

		/// <summary>
		/// The rows at the top of a number's line that lie under its name: as many as leave <paramref name="gap"/> clear rows
		/// from the name's baseline to the digits, counted from where each font draws its ink, and never a row the digits are
		/// drawn on, in a font that leaves fewer empty above them. Less than none puts room between the two.
		/// </summary>
		static int ValueOverlap(Label name, Font valueFont, int gap)
		{
			var baseline = InkRows(name.Font, BaselineCharacters)[1];
			var digitsTop = InkRows(valueFont, ValueCharacters)[0];
			return Math.Min(name.Height - 1 - baseline + digitsTop - gap, digitsTop);
		}

		/// <summary>Every character a number draws.</summary>
		const string ValueCharacters = "-0123456789";

		/// <summary>A capital, which stands on a name's baseline and draws nothing under it.</summary>
		const string BaselineCharacters = "H";

		/// <summary>What <see cref="InkRows"/> found, by font and text. The names share one font and the numbers another.</summary>
		static readonly Dictionary<Tuple<Font, string>, int[]> _InkRows = new Dictionary<Tuple<Font, string>, int[]>();

		/// <summary>The first and last rows of its line a text draws ink on, drawn as a label draws it, with GDI.</summary>
		static int[] InkRows(Font font, string text)
		{
			var key = Tuple.Create(font, text);
			int[] rows;
			if (_InkRows.TryGetValue(key, out rows))
				return rows;
			var size = TextRenderer.MeasureText(text, font, Size.Empty, TextFormatFlags.NoPadding);
			rows = new[] { size.Height, -1 };
			using (var bitmap = new Bitmap(size.Width, size.Height))
			{
				using (var g = Graphics.FromImage(bitmap))
				{
					g.Clear(Color.White);
					TextRenderer.DrawText(g, text, font, Point.Empty, Color.Black, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
				}
				var white = Color.White.ToArgb();
				for (var y = 0; y < size.Height; y++)
					for (var x = 0; x < size.Width; x++)
						if (bitmap.GetPixel(x, y).ToArgb() != white)
						{
							rows[0] = Math.Min(rows[0], y);
							rows[1] = y;
							break;
						}
			}
			_InkRows[key] = rows;
			return rows;
		}

		/// <summary>The widest number a part shows: a stick's axis at its lowest.</summary>
		const string WidestValue = "-32768";

		/// <summary>
		/// Sizes a name and its number one line of their fonts tall, and their panel as wide as the name's text or the widest
		/// number, whichever is wider, and <see cref="ValueOverlap"/> shorter than the two.
		/// </summary>
		/// <remarks>
		/// The numbers change ten times a second on four controller pages. One that sized itself to each value would lay the
		/// page out each time its width changed; at one width, a new value is only drawn. A label whose text changes takes back
		/// the size it was last given, so each is given the size its dock gives it, and a new value lays out nothing.
		/// </remarks>
		static void FitPair(ImageInfo ii)
		{
			FitPair((Label)ii.Label, ii.ValueLabel, ValueGap);
		}

		/// <summary>Sizes a name, the number under it and their panel, with <paramref name="gap"/> clear rows from the name's baseline to the digits.</summary>
		internal static void FitPair(Label name, Label value, int gap)
		{
			var width = Math.Max(JocysCom.ClassLibrary.Controls.Themes.FormsTheme.GetLabelWidth(name),
				JocysCom.ClassLibrary.Controls.Themes.FormsTheme.GetLabelWidth(value, WidestValue));
			name.Size = new Size(width, LineHeight(name));
			value.Size = new Size(width, LineHeight(value));
			var size = new Size(width, name.Height + value.Height - ValueOverlap(name, value.Font, gap));
			if (name.Parent.Size != size)
				name.Parent.Size = size;
		}

		/// <summary>The numbers are the theme's active colour, which the lit names and the other readings are drawn in.</summary>
		void SetValueColors()
		{
			var color = JocysCom.ClassLibrary.Controls.Themes.FormsTheme.GetColor("ForegroundActive");
			foreach (var ii in imageInfos)
				ii.ValueLabel.ForeColor = color;
		}

		#endregion

		#region Settings Map

		public MapTo MappedTo;

		/// <summary>The page's boxes as the settings know them, which follow the page to the controller it shows.</summary>
		readonly List<SettingsMapItem> _MapItems = new List<SettingsMapItem>();

		/// <summary>The device list as the Mappings section knows it, which follows the page as well; null on a page without one.</summary>
		internal SettingsMapItem GridMap;

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
			AddMap(() => SettingName.ForcePassThrough, ForcePassThroughCheckBox);
			AddMap(() => SettingName.ForcePassThroughIndex, ForcePassThroughIndexComboBox);
			AddMap(() => SettingName.PassThroughIndex, PassThroughIndexComboBox);
			// Mapping
			// Left Trigger
			AddMap(() => SettingName.LeftTrigger, LeftTriggerComboBox, MapCode.LeftTrigger);
			AddMap(() => SettingName.LeftTriggerDeadZone, LeftTriggerUserControl.DeadZoneTrackBar);
			AddMap(() => SettingName.LeftTriggerAntiDeadZone, LeftTriggerUserControl.AntiDeadZoneNumericUpDown);
			AddMap(() => SettingName.LeftTriggerLinear, LeftTriggerUserControl.SensitivityNumericUpDown);
			// Right Trigger
			AddMap(() => SettingName.RightTrigger, RightTriggerComboBox, MapCode.RightTrigger);
			AddMap(() => SettingName.RightTriggerDeadZone, RightTriggerUserControl.DeadZoneTrackBar);
			AddMap(() => SettingName.RightTriggerAntiDeadZone, RightTriggerUserControl.AntiDeadZoneNumericUpDown);
			AddMap(() => SettingName.RightTriggerLinear, RightTriggerUserControl.SensitivityNumericUpDown);
			// Combining
			AddMap(() => SettingName.Combined, CombinedCheckBox);
			AddMap(() => SettingName.CombinedIndex, CombinedIndexComboBox);
			// D-Pad
			AddMap(() => SettingName.DPad, DPadComboBox, MapCode.DPad);
			AddMap(() => SettingName.DPadUp, DPadUpComboBox, MapCode.DPadUp);
			AddMap(() => SettingName.DPadDown, DPadDownComboBox, MapCode.DPadDown);
			AddMap(() => SettingName.DPadLeft, DPadLeftComboBox, MapCode.DPadLeft);
			AddMap(() => SettingName.DPadRight, DPadRightComboBox, MapCode.DPadRight);
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
			AddMap(() => SettingName.LeftThumbDeadZoneX, LeftThumbXUserControl.DeadZoneTrackBar);
			AddMap(() => SettingName.LeftThumbDeadZoneY, LeftThumbYUserControl.DeadZoneTrackBar);
			AddMap(() => SettingName.LeftThumbAntiDeadZoneX, LeftThumbXUserControl.AntiDeadZoneNumericUpDown);
			AddMap(() => SettingName.LeftThumbAntiDeadZoneY, LeftThumbYUserControl.AntiDeadZoneNumericUpDown);
			AddMap(() => SettingName.LeftThumbLinearX, LeftThumbXUserControl.SensitivityNumericUpDown);
			AddMap(() => SettingName.LeftThumbLinearY, LeftThumbYUserControl.SensitivityNumericUpDown);
			// Right Thumb
			AddMap(() => SettingName.RightThumbAxisX, RightThumbAxisXComboBox, MapCode.RightThumbAxisX);
			AddMap(() => SettingName.RightThumbAxisY, RightThumbAxisYComboBox, MapCode.RightThumbAxisY);
			AddMap(() => SettingName.RightThumbRight, RightThumbRightComboBox, MapCode.RightThumbRight);
			AddMap(() => SettingName.RightThumbLeft, RightThumbLeftComboBox, MapCode.RightThumbLeft);
			AddMap(() => SettingName.RightThumbUp, RightThumbUpComboBox, MapCode.RightThumbUp);
			AddMap(() => SettingName.RightThumbDown, RightThumbDownComboBox, MapCode.RightThumbDown);
			AddMap(() => SettingName.RightThumbButton, RightThumbButtonComboBox, MapCode.RightThumbButton);
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
			AddMap(() => SettingName.ForceSpringEnable, ForceSpringEnableCheckBox);
			AddMap(() => SettingName.ForceSpringStrength, ForceSpringStrengthTrackBar);
			AddMap(() => SettingName.ForceSpringCentre, ForceSpringCentreTrackBar);
			AddMap(() => SettingName.ForceSpringPeak, ForceSpringPeakTrackBar);
			AddMap(() => SettingName.ForceSpringLightening, ForceSpringLighteningTrackBar);
			AddMap(() => SettingName.ForceSpringDamping, ForceSpringDampingTrackBar);
			AddMap(() => SettingName.ForceSpringSpeed, ForceSpringSpeedTrackBar);
			AddMap(() => SettingName.WheelRange, WheelRangeComboBox);
			// Mouse to stick.
			AddMap(() => SettingName.MouseLeftThumbStyle, LeftMouseStickPanel.StyleComboBox);
			AddMap(() => SettingName.MouseLeftThumbSensitivityX, LeftMouseStickPanel.SensitivityXTrackBar);
			AddMap(() => SettingName.MouseLeftThumbSensitivityY, LeftMouseStickPanel.SensitivityYTrackBar);
			AddMap(() => SettingName.MouseLeftThumbSmoothing, LeftMouseStickPanel.SmoothingTrackBar);
			AddMap(() => SettingName.MouseLeftThumbReturn, LeftMouseStickPanel.ReturnTrackBar);
			AddMap(() => SettingName.MouseLeftThumbActive, LeftMouseStickPanel.ActiveComboBox);
			AddMap(() => SettingName.MouseLeftThumbButton, LeftMouseStickPanel.ButtonComboBox);
			AddMap(() => SettingName.MouseRightThumbStyle, RightMouseStickPanel.StyleComboBox);
			AddMap(() => SettingName.MouseRightThumbSensitivityX, RightMouseStickPanel.SensitivityXTrackBar);
			AddMap(() => SettingName.MouseRightThumbSensitivityY, RightMouseStickPanel.SensitivityYTrackBar);
			AddMap(() => SettingName.MouseRightThumbSmoothing, RightMouseStickPanel.SmoothingTrackBar);
			AddMap(() => SettingName.MouseRightThumbReturn, RightMouseStickPanel.ReturnTrackBar);
			AddMap(() => SettingName.MouseRightThumbActive, RightMouseStickPanel.ActiveComboBox);
			AddMap(() => SettingName.MouseRightThumbButton, RightMouseStickPanel.ButtonComboBox);
			AddMap(() => SettingName.LeftMotorDirection, LeftMotorDirectionComboBox);
			AddMap(() => SettingName.LeftMotorStrength, LeftMotorStrengthTrackBar);
			AddMap(() => SettingName.LeftMotorPeriod, LeftMotorPeriodTrackBar);
			AddMap(() => SettingName.RightMotorDirection, RightMotorDirectionComboBox);
			AddMap(() => SettingName.RightMotorStrength, RightMotorStrengthTrackBar);
			AddMap(() => SettingName.RightMotorPeriod, RightMotorPeriodTrackBar);
		}

		void AddMap<T>(Expression<Func<T>> setting, Control control, MapCode code = default)
		{
			var section = string.Format(@"PAD{0}", (int)MappedTo);
			var item = SettingsManager.AddMap(section, setting, control, MappedTo, code);
			item.Code = code;
			_MapItems.Add(item);
			// A box that names a control can hold an expression instead, so it gets the switch that
			// turns one into the other. Every mapping box carries a code and nothing else does, so
			// this needs no list of its own to fall out of step.
			if (control is ComboBox && code != default(MapCode))
				MapExpressionToggle.AttachTo((ComboBox)control);
		}

		#endregion

		#region Shared page

		/// <summary>What one controller's tab showed on the page, kept while the page shows another controller.</summary>
		class PadView
		{
			/// <summary>The page's selected tab, by name.</summary>
			public string Page;
			/// <summary>The Direct Input page's selected tab, by name.</summary>
			public string DetailsPage;
			/// <summary>The selected device row, by device instance; null when none was.</summary>
			public Guid? Row;
			/// <summary>How far the page was scrolled.</summary>
			public Point Scroll;
			/// <summary>The Test sliders, 0 to 100.</summary>
			public int LeftMotor;
			public int RightMotor;
		}

		readonly Dictionary<MapTo, PadView> _Views = new Dictionary<MapTo, PadView>();

		PadView ViewOf(MapTo mapTo)
		{
			PadView view;
			if (!_Views.TryGetValue(mapTo, out view))
				_Views[mapTo] = view = new PadView();
			return view;
		}

		void SaveView()
		{
			var view = ViewOf(MappedTo);
			view.Page = PadTabControl.SelectedTab?.Name;
			view.DetailsPage = DirectInputPanel.DeviceDetailsTabControl.SelectedTab?.Name;
			view.Row = GetSelectedSetting()?.InstanceGuid;
			view.Scroll = new Point(-AutoScrollPosition.X, -AutoScrollPosition.Y);
			view.LeftMotor = LeftMotorTestTrackBar.Value;
			view.RightMotor = RightMotorTestTrackBar.Value;
		}

		/// <summary>Selects a tab by name, or the first tab for a controller whose tab has not been shown, as a page of its own opened.</summary>
		static void SelectPage(TabControl tabs, string name)
		{
			var pages = tabs.TabPages.Cast<TabPage>().ToArray();
			var page = string.IsNullOrEmpty(name) ? pages.FirstOrDefault() : pages.FirstOrDefault(x => x.Name == name);
			if (page != null && tabs.SelectedTab != page)
				tabs.SelectedTab = page;
		}

		/// <summary>Shows another controller on the page, as that controller's own tab would show it.</summary>
		/// <remarks>
		/// One page serves the four Controller tabs, which made and kept about 3,000 windows when each had its own. The
		/// boxes keep their place in the settings and move to the controller shown: while they move and take its values,
		/// settings events are held, so the values shown for one controller are never saved as another's. Each controller's
		/// tab keeps what it showed: the page and the Direct Input page selected, the device row, the scroll position and the
		/// Test sliders, whose motors run on while another controller is shown. A mapping being recorded stops, as it does on
		/// Escape: its boxes are about to show another controller.
		/// </remarks>
		internal void SwitchTo(MapTo mapTo)
		{
			if (mapTo == MappedTo)
				return;
			var watch = System.Diagnostics.Stopwatch.StartNew();
			SaveView();
			StopRecording();
			var view = ViewOf(mapTo);
			SettingsManager.Current.SuspendEvents();
			try
			{
				MappedTo = mapTo;
				Name = string.Format("ControlPad{0}", (int)mapTo);
				var section = string.Format(@"PAD{0}", (int)mapTo);
				foreach (var item in _MapItems)
				{
					item.MapTo = mapTo;
					item.IniSection = section;
				}
				if (GridMap != null)
				{
					var key = string.Format(@"PAD{0}", (int)mapTo);
					GridMap.MapTo = mapTo;
					GridMap.PropertyName = key;
					GridMap.IniKey = key;
				}
				XboxImage.InitializeImages(imageInfos, _Imager, mapTo);
				// What the page knew of the controller shown before.
				_Announced = false;
				_AnnouncedSetting = null;
				_InstanceGuid = Guid.Empty;
				oldState = default(State);
				oldConnected = false;
				_Imager.SetImages(false);
				newState = default(State);
				newConnected = false;
				ClearReadings();
				springNoteShown = false;
				springNoteHid = null;
				forceSourceRouting = null;
				forceSourceSetting = null;
				forceSourceDevice = null;
				var row = view.Row ?? MappedItemsFor(mapTo).Select(x => (Guid?)x.InstanceGuid).FirstOrDefault();
				UpdateFromCurrentGame(row);
				// A controller with no device shows the defaults, as a tab of its own showed them, not the last one's values.
				if (GetSelectedSetting() == null)
					SettingsManager.Current.SyncFormFromPadSetting(mapTo, new PadSetting());
				UpdateEnabledFromDevice(GetSelectedDevice());
				UpdateWheelChart(true);
			}
			finally
			{
				SettingsManager.Current.ResumeEvents();
			}
			SelectPage(PadTabControl, view.Page);
			SelectPage(DirectInputPanel.DeviceDetailsTabControl, view.DetailsPage);
			LeftMotorTestTrackBar.Value = view.LeftMotor;
			RightMotorTestTrackBar.Value = view.RightMotor;
			AutoScrollPosition = view.Scroll;
			if (Global.DHelper?.PassCount > 0)
				Global_UpdateControlFromStates(this, EventArgs.Empty);
			Program.StartupTrace.Mark(string.Format("pad: controller {0} shown in {1} ms", (int)mapTo, watch.ElapsedMilliseconds));
		}

		/// <summary>
		/// Shows no reading, as a controller's tab of its own showed none before its controller's first state: the General
		/// page's names unlit, the numbers under them and each axis row's DInput and XInput at 0, the controller picture at
		/// rest, each axis chart drawn from its settings alone, no reading on the Axis to D-Pad chart and no button pressed
		/// on the Buttons page.
		/// </summary>
		/// <remarks>
		/// The next state update shows the controller's own readings only while it is connected and has a device, so a
		/// controller with no device would otherwise go on showing those of the controller shown before.
		/// </remarks>
		void ClearReadings()
		{
			var rest = default(State);
			foreach (var ii in imageInfos)
			{
				_Imager.DrawState(ii, rest.Gamepad, CurrentCbx);
				ii.ShowValue(rest.Gamepad);
			}
			foreach (var chart in _AxisCharts)
			{
				chart.SetExpression(null);
				chart.SetTuningEnabled(true);
				chart.DrawPoint(0, 0, false, false);
			}
			AxisToDPadChart.ClearReading();
			foreach (var button in AxisToButtonTableLayoutPanel.Controls.OfType<AxisToButtonUserControl>())
				button.Refresh(rest, _Imager.markB);
		}

		/// <summary>The rows a controller's tab lists for the game chosen.</summary>
		static IEnumerable<UserSetting> MappedItemsFor(MapTo mapTo)
		{
			var game = SettingsManager.CurrentGame;
			return SettingsManager.UserSettings.ItemsToArraySynchronized()
				.Where(x => x.MapTo == (int)mapTo && game != null && x.FileName == game.FileName && x.FileProductName == game.FileProductName);
		}

		/// <summary>The device row a controller's tab has selected, shown or not, for what reads a tab without opening it.</summary>
		internal UserSetting GetSelectedSettingFor(MapTo mapTo)
		{
			if (mapTo == MappedTo)
				return GetSelectedSetting();
			PadView view;
			var rows = MappedItemsFor(mapTo).ToArray();
			if (_Views.TryGetValue(mapTo, out view) && view.Row.HasValue)
				return rows.FirstOrDefault(x => x.InstanceGuid == view.Row.Value) ?? rows.FirstOrDefault();
			return rows.FirstOrDefault();
		}

		#endregion

		//XINPUT_GAMEPAD GamePad;
		Guid _InstanceGuid;

		private void UpdatePassThroughRelatedControls()
		{
			// Each pass-through says where it goes for itself, so each box turns its own destination on. Full pass-through
			// belongs to x360ce 3 and is greyed out with its page, so it leaves this version's force pass-through alone.
			PassThroughIndexComboBox.Enabled = PassThroughCheckBox.Checked;
			ForcePassThroughIndexComboBox.Enabled = ForcePassThroughCheckBox.Checked;
		}

		/// <summary>
		/// Get selected Setting. If device is not selected then return null.
		/// </summary>
		public UserSetting GetSelectedSetting()
		{
			var grid = MappedDevicesDataGridView;
			var row = grid.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault();
			if (row == null)
				return null;
			return (UserSetting)row.DataBoundItem;
		}

		/// <summary>
		/// Get selected device. If device is not connected then return null.
		/// </summary>
		public UserDevice GetSelectedDevice()
		{
			var setting = GetSelectedSetting();
			if (setting == null)
				return null;
			return SettingsManager.GetDevice(setting.InstanceGuid);
		}

		/// <summary>
		/// Get PadSetting from currently selected device.
		/// </summary>
		public PadSetting GetSelectedPadSetting()
		{
			var setting = GetSelectedSetting();
			if (setting == null)
				return new PadSetting();
			return SettingsManager.GetPadSetting(setting.PadSettingChecksum);
		}

		/// <summary>
		/// Get PadSetting from currently selected device.
		/// </summary>
		public PadSetting CloneCurrentPadSetting()
		{
			// Get settings related to PAD.
			var maps = SettingsManager.Current.GetPageMaps(MappedTo);
			PropertyInfo[] properties;
			if (!SettingsManager.ValidatePropertyNames(maps, out properties))
				return null;
			var ps = new PadSetting();
			foreach (var p in properties)
			{
				var map = maps[p.Name];
				// Get setting value from the form.
				var v = SettingsManager.Current.GetPresetValue(map);
				// Set value onto padSetting.
				p.SetValue(ps, v ?? "", null);
			}
			return SettingsManager.Current.KeepWhatWasNotChanged(MappedTo, ps);
		}

		object updateFromDirectInputLock = new object();

		#region Update Controls

		void UpdateDirectInputTabPage(UserDevice diDevice)
		{
			ControlsHelper.SetText(DirectInputTabPage, DirectInputTabTitle(diDevice));
		}

		/// <summary>The Direct Input tab's title: the source the device is read through, its instance, and whether it is offline or online with no state yet.</summary>
		/// <remarks>A Raw Input device has no DirectInput device; its state is the one the engine reads it into.</remarks>
		public static string DirectInputTabTitle(UserDevice diDevice)
		{
			var isOnline = diDevice != null && diDevice.IsOnline;
			var hasState = isOnline && (diDevice.IsDirectInput ? diDevice.Device != null : diDevice.SourceState != null);
			var source = diDevice != null && !diDevice.IsDirectInput ? AppHelper.GetInputSourceName(diDevice) : "Direct Input";
			var instance = diDevice == null ? "" : " - " + diDevice.InstanceId;
			return source + instance + (isOnline ? hasState ? "" : " - Online" : " - Offline");
		}

		#endregion

		// Old XInput state.
		State oldState;
		bool oldConnected;
		// Current XInput state.
		State newState;
		internal bool newConnected;


		// Check left thumbStick
		public float FloatToByte(float v)
		{
			// -1 to 1 int16.MinValue int16.MaxValue.
			return (Byte)Math.Round((double)v * (double)Byte.MaxValue);
		}

		string cRecord = "[Record]";
		string cAdjust = "[Adjust]";
		string cInvert = "[Invert]";
		string cRecentre = "[Recentre]";
		string cRoundDeadZone = "[Round Dead Zone]";
		string cTurbo = "[Turbo]";
		string cEmpty = "<empty>";
		string cPOVs = "POVs";

		// Function is recreated as soon as new DirectInput Device is available.
		public void ResetDiMenuStrip(UserDevice ud)
		{
			DiMenuStrip.Items.Clear();
			ToolStripMenuItem mi;
			mi = new ToolStripMenuItem(cEmpty);
			mi.ForeColor = SystemColors.GrayText;
			mi.Click += new EventHandler(DiMenuStrip_Click);
			DiMenuStrip.Items.Add(mi);
			// Return if direct input device is not available.
			if (ud == null)
				return;
			// Add [Record] button.
			mi = new ToolStripMenuItem(cRecord);
			mi.Click += new EventHandler(DiMenuStrip_Click);
			DiMenuStrip.Items.Add(mi);
			ControlsHelper.SetImage(mi, Properties.Resources.bullet_ball_glass_red_32x32);
			// Add [Adjust], which holds the changes made to what the box already reads.
			var adjust = new ToolStripMenuItem(cAdjust);
			DiMenuStrip.Items.Add(adjust);
			// [Invert] reads the box's control the other way round.
			mi = new ToolStripMenuItem(cInvert);
			mi.Click += new EventHandler(DiMenuStrip_Click);
			adjust.DropDownItems.Add(mi);
			// [Recentre] makes where the box's axis rests now read as its middle.
			mi = new ToolStripMenuItem(cRecentre);
			mi.ToolTipText = "Leave the stick at rest, then pick this: where it rests now reads as the middle.";
			mi.Click += new EventHandler(DiMenuStrip_Click);
			adjust.DropDownItems.Add(mi);
			// [Round Dead Zone] gives the stick one dead zone for both of its axes, a circle round its middle.
			mi = new ToolStripMenuItem(cRoundDeadZone);
			mi.ToolTipText = "On a stick's X or Y box: the stick reads nothing inside a circle round its middle, in every direction.";
			mi.Click += new EventHandler(DiMenuStrip_Click);
			adjust.DropDownItems.Add(mi);
			// [Turbo] makes the box's button press ten times a second while it is held.
			mi = new ToolStripMenuItem(cTurbo);
			mi.ToolTipText = "On a box mapped to a button: while the button is held, it presses ten times a second.";
			mi.Click += new EventHandler(DiMenuStrip_Click);
			adjust.DropDownItems.Add(mi);
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
					CreateItems(mi, "Inverted", "IAxis {0}", "a-{0}", SourceState.MaxAxis, ud.DiAxeMask);
					CreateItems(mi, "Inverted Half", "IHAxis {0}", "x-{0}", SourceState.MaxAxis, ud.DiAxeMask);
					CreateItems(mi, "Half", "HAxis {0}", "x{0}", SourceState.MaxAxis, ud.DiAxeMask);
					CreateItems(mi, "Axis {0}", "a{0}", SourceState.MaxAxis, ud.DiAxeMask);
				}
				if (ud.DiSliderMask > 0)
				{
					// Add Sliders.            
					mi = new ToolStripMenuItem("Sliders");
					DiMenuStrip.Items.Add(mi);
					// 2 x Sliders, 2 x AccelerationSliders, 2 x state.ForceSliders, 2 x VelocitySliders
					CreateItems(mi, "Inverted", "ISlider {0}", "s-{0}", SourceState.MaxSliders, ud.DiSliderMask);
					CreateItems(mi, "Inverted Half", "IHSlider {0}", "h-{0}", SourceState.MaxSliders, ud.DiSliderMask);
					CreateItems(mi, "Half", "HSlider {0}", "h{0}", SourceState.MaxSliders, ud.DiSliderMask);
					CreateItems(mi, "Slider {0}", "s{0}", SourceState.MaxSliders, ud.DiSliderMask);
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

		/// <summary>The most items one list of the mapping menu shows before they are grouped by tens.</summary>
		public const int MaxMenuItems = 20;

		/// <summary>
		/// Create menu items. More than <see cref="MaxMenuItems"/> are grouped by tens: "Button x" holds Buttons 1 to 9,
		/// "Button 1x" Buttons 10 to 19, and so on, so a device with 128 buttons does not fill the screen.
		/// </summary>
		/// <param name="mask">Mask contains information if item is present.</param>
		void CreateItems(ToolStripMenuItem parent, string text, string tag, int count, int? mask = null)
		{
			var items = new List<KeyValuePair<int, ToolStripMenuItem>>();
			for (int i = 0; i < count; i++)
			{
				// If mask specified and item is not present then...
				if (mask.HasValue && i < 32 && (((int)Math.Pow(2, i) & mask) == 0))
					continue;
				items.Add(new KeyValuePair<int, ToolStripMenuItem>(i + 1, CreateItem(text, tag, i + 1)));
			}
			if (items.Count <= MaxMenuItems)
			{
				foreach (var item in items)
					parent.DropDownItems.Add(item.Value);
				return;
			}
			foreach (var tens in items.GroupBy(x => x.Key / 10))
			{
				var group = new ToolStripMenuItem(string.Format(text, (tens.Key == 0 ? "" : tens.Key.ToString()) + "x"));
				group.DropDownItems.AddRange(tens.Select(x => (ToolStripItem)x.Value).ToArray());
				parent.DropDownItems.Add(group);
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
			// The box this menu was opened for. Nothing else can be asked: by now the menu has
			// closed and CurrentCbx may already have been cleared.
			var cbx = MenuTargetCbx;
			if (cbx == null || cbx.IsDisposed)
				return;
			// A POV's own item, clicked on the way to one of its directions. Only the D-Pad box takes a whole POV; anywhere
			// else the click only opens the directions.
			Regex rx = new Regex("^(POV [0-9]+)$");
			if (rx.IsMatch(item.Text))
			{
				if (cbx == DPadComboBox)
				{
					SettingsManager.Current.SetComboBoxValue(cbx, item.Text);
					CurrentCbx = null;
					//DiMenuStrip.Close();
				}
			}
			else
			{
				if (item.Text == cRecord)
				{
					// Swapping the device rebuilds the map while the menu is open, so the row this
					// box belongs to can be gone by the time [Record] is picked.
					var map = SettingsManager.Current.SettingsMap.FirstOrDefault(x => x.Control == cbx);
					if (map != null)
						StartRecording(map);
				}
				else if (item.Text == cInvert)
				{
					// The same control, read the other way round.
					var inverted = SettingsConverter.InvertTextValue(cbx.Text);
					if (inverted != null)
						SettingsManager.Current.SetComboBoxValue(cbx, inverted);
					CurrentCbx = null;
				}
				else if (item.Text == cRecentre)
				{
					var recentred = RecentredText(cbx.Text, GetSelectedDevice()?.SourceState?.Axis);
					if (recentred != null)
						SettingsManager.Current.SetComboBoxValue(cbx, recentred);
					MainForm.Current.StatusTimerLabel.Text = recentred == null
						? "[Recentre] works on a box mapped to an axis of a connected device."
						: "Recentred: where the stick rests now reads as the middle (" + recentred + ").";
					CurrentCbx = null;
				}
				else if (item.Text == cRoundDeadZone)
				{
					var stick = StickOf(cbx);
					var percent = stick == null ? 0 : stick.Item3.DeadZoneTrackBar.Value;
					var size = percent > 0 ? percent / 100.0 : DefaultRoundDeadZone;
					var texts = stick == null ? null : RoundDeadZoneTexts(stick.Item1.Text, stick.Item2.Text, size);
					if (texts != null)
					{
						SettingsManager.Current.SetComboBoxValue(stick.Item1, texts[0]);
						SettingsManager.Current.SetComboBoxValue(stick.Item2, texts[1]);
					}
					MainForm.Current.StatusTimerLabel.Text = texts == null
						? "[Round Dead Zone] works on a stick's X or Y box, with both boxes mapped to axes."
						: string.Format("Round dead zone: the stick reads nothing within {0:0} % of its middle, in every direction.", size * 100);
					CurrentCbx = null;
				}
				else if (item.Text == cTurbo)
				{
					var turbo = TurboText(cbx.Text);
					if (turbo != null)
						SettingsManager.Current.SetComboBoxValue(cbx, turbo);
					MainForm.Current.StatusTimerLabel.Text = turbo == null
						? "[Turbo] works on a box mapped to a button, such as Button 1."
						: "Turbo: while the button is held, it presses ten times a second (" + turbo + ").";
					CurrentCbx = null;
				}
				else if (item.Text == cEmpty)
				{
					SettingsManager.Current.SetComboBoxValue(cbx, string.Empty);
					CurrentCbx = null;
				}
				else
				{
					SettingsManager.Current.SetComboBoxValue(cbx, item.Text);
					CurrentCbx = null;
				}
			}
		}

		/// <summary>The mapping that reads a stick's axis from where it rests now, or null when the box maps no axis.</summary>
		/// <remarks>
		/// A formula subtracts the resting reading, in the units formulas use for a stick: -1 to 1 with the middle at
		/// nought. An axis read the other way round is turned round in the formula as well.
		/// </remarks>
		/// <param name="text">The box's mapping: "Axis N", "IAxis N", or a formula "=aN" or "=-aN" with or without an offset.</param>
		/// <param name="axes">The device's axes as they read now, 0 to 65535.</param>
		public static string RecentredText(string text, int[] axes)
		{
			if (string.IsNullOrEmpty(text) || axes == null)
				return null;
			var m = Regex.Match(text.Trim(), @"^(?:(?<inv>I?)Axis (?<n>\d+)|=(?<inv>-?)a(?<n>\d+)\s*(?:[+-]\s*[0-9.]+)?)$");
			if (!m.Success)
				return null;
			var index = int.Parse(m.Groups["n"].Value);
			if (index < 1 || index > axes.Length)
				return null;
			var inverted = m.Groups["inv"].Value.Length > 0;
			var rest = MapExpressionUnits.Centred(axes[index - 1]) * (inverted ? -1 : 1);
			var offset = Math.Round(rest, 3);
			if (offset == 0)
				return (inverted ? "IAxis " : "Axis ") + index;
			return "=" + (inverted ? "-a" : "a") + index + (offset > 0 ? "-" : "+")
				+ Math.Abs(offset).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
		}

		/// <summary>The mapping that presses a box's button ten times a second while it is held, or null when the box maps no button.</summary>
		/// <remarks>The formula Help gives for turbo, <see cref="MapExpressionHelp.TurboFormula"/>. A button read the other way round
		/// presses while it is released, so its turbo runs while it is released.</remarks>
		/// <param name="text">The box's mapping: "Button N" or "IButton N".</param>
		public static string TurboText(string text)
		{
			if (string.IsNullOrEmpty(text))
				return null;
			var m = Regex.Match(text.Trim(), @"^(?<inv>I?)Button (?<n>\d+)$");
			if (!m.Success)
				return null;
			var button = "b" + m.Groups["n"].Value;
			return string.Format(MapExpressionHelp.TurboFormula, m.Groups["inv"].Value.Length > 0 ? "(1-" + button + ")" : button);
		}

		/// <summary>The round dead zone's size, as a part of the stick's travel, while the stick's own Dead Zone slider is at nought.</summary>
		public const double DefaultRoundDeadZone = 0.2;

		/// <summary>The stick a box belongs to: its X box, its Y box and its X axis settings, or null for a box of no stick axis.</summary>
		Tuple<ComboBox, ComboBox, AxisMapUserControl> StickOf(ComboBox cbx)
		{
			if (cbx == LeftThumbAxisXComboBox || cbx == LeftThumbAxisYComboBox)
				return Tuple.Create(LeftThumbAxisXComboBox, LeftThumbAxisYComboBox, LeftThumbXUserControl);
			if (cbx == RightThumbAxisXComboBox || cbx == RightThumbAxisYComboBox)
				return Tuple.Create(RightThumbAxisXComboBox, RightThumbAxisYComboBox, RightThumbXUserControl);
			return null;
		}

		/// <summary>The axis a stick box reads, signed as a formula writes it ("a1", "-a2"), or null when the box reads something else.</summary>
		/// <remarks>Takes the box's axis back out of a round dead zone already there, so picking it again changes only its size.</remarks>
		static string StickAxisOf(string text)
		{
			if (string.IsNullOrEmpty(text))
				return null;
			var m = Regex.Match(text.Trim(),
				@"^(?:(?<inv>I?)Axis (?<n>\d+)|=(?<inv>-?)a(?<n>\d+)(?:\*\(1-[0-9.]+/max\(sqrt\(a\d+\^2\+a\d+\^2\),[0-9.]+\)\)/[0-9.]+)?)$");
			if (!m.Success)
				return null;
			return (m.Groups["inv"].Value.Length > 0 ? "-a" : "a") + m.Groups["n"].Value;
		}

		/// <summary>
		/// The mappings that give a stick one round dead zone for both axes, X first then Y, or null when either box maps no axis.
		/// </summary>
		/// <remarks>The formula Help gives, <see cref="MapExpressionHelp.RoundDeadZoneFormula"/>, keeping each box's axis and its way round.</remarks>
		/// <param name="xText">The stick's X box: "Axis N", "IAxis N", "=aN", "=-aN" or a round dead zone made here.</param>
		/// <param name="yText">The stick's Y box, read the same way.</param>
		/// <param name="size">The dead zone's radius, as a part of the stick's travel, more than 0 and less than 1.</param>
		public static string[] RoundDeadZoneTexts(string xText, string yText, double size)
		{
			var x = StickAxisOf(xText);
			var y = StickAxisOf(yText);
			if (x == null || y == null || size <= 0 || size >= 1)
				return null;
			var culture = System.Globalization.CultureInfo.InvariantCulture;
			var r = Math.Round(size, 2).ToString("0.##", culture);
			var rest = Math.Round(1 - size, 2).ToString("0.##", culture);
			var xn = x.TrimStart('-').Substring(1);
			var yn = y.TrimStart('-').Substring(1);
			return new[]
			{
				string.Format(MapExpressionHelp.RoundDeadZoneFormula, x, xn, yn, r, rest),
				string.Format(MapExpressionHelp.RoundDeadZoneFormula, y, xn, yn, r, rest),
			};
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
			ForceOverallTextBox.Text = string.Format("{0} %", control.Value);
		}

		void ForceSpringStrengthTrackBar_ValueChanged(object sender, EventArgs e)
		{
			TrackBar control = (TrackBar)sender;
			ForceSpringStrengthTextBox.Text = string.Format("{0} %", control.Value);
			UpdateSpringChart();
		}

		/// <summary>Shows the values of the centering spring's curve beside their sliders, the car they match, and the curve.</summary>
		void ForceSpringCurveTrackBar_ValueChanged(object sender, EventArgs e)
		{
			ForceSpringCentreTextBox.Text = string.Format("{0}° ", ForceSpringCentreTrackBar.Value);
			ForceSpringPeakTextBox.Text = ForceSpringPeakTrackBar.Value == 0 ? "Stop " : string.Format("{0}° ", ForceSpringPeakTrackBar.Value);
			ForceSpringLighteningTextBox.Text = string.Format("{0} %", ForceSpringLighteningTrackBar.Value);
			ForceSpringDampingTextBox.Text = string.Format("{0} %", ForceSpringDampingTrackBar.Value);
			ForceSpringSpeedTextBox.Text = string.Format("{0} km/h ", ForceSpringSpeedTrackBar.Value);
			ShowSpringCar();
			UpdateSpringChart();
		}

		void WheelRangeComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateSpringChart();
		}

		/// <summary>Picks the cars' range when the centering spring is ticked with the range left as it is.</summary>
		/// <remarks>
		/// The spring's degrees are counted against the range, and no wheel says what range it turns, so the spring needs
		/// one chosen: a wheel that takes it is sent it, and any other is counted against it. Left as it is, a Logitech
		/// wheel turns the 200 degrees it powers up at, and a spring counted against 900 swung it at its centre.
		/// </remarks>
		void KeepRangeForSpring()
		{
			if (!ForceSpringEnableCheckBox.Checked || SelectedWheelRange() != 0)
				return;
			for (var r = 0; r < WheelRangeComboBox.Items.Count; r++)
			{
				var item = WheelRangeComboBox.Items[r] as KeyValuePair;
				if (item != null && item.Value == RoadCarRange.ToString())
					WheelRangeComboBox.SelectedIndex = r;
			}
		}

		/// <summary>Shows the friction beside its slider, and keeps it with the wheel.</summary>
		/// <remarks>
		/// Kept with the device whose motors the engine drives, a Raw Input device's DirectInput twin, so it holds for
		/// every car and every tab the wheel is on.
		/// </remarks>
		void ForceSpringFrictionTrackBar_ValueChanged(object sender, EventArgs e)
		{
			ForceSpringFrictionTextBox.Text = string.Format("{0} %", ForceSpringFrictionTrackBar.Value);
			if (_showingFriction)
				return;
			var ud = GetSelectedDevice();
			var driven = ud == null ? null : DInput.DeviceRouting.Current.ForceDevice(ud);
			if (driven != null)
				driven.SetForceSpringFriction(ForceSpringFrictionTrackBar.Value);
		}

		/// <summary>Guards the friction slider against writing back the wheel's value while it shows it.</summary>
		bool _showingFriction;

		#region Wheel tab: cars and chart

		/// <summary>A car on the Car list: the values it puts on the centering spring's curve, the wheel range its kind of car turns, and what they are based on.</summary>
		/// <remarks>Neither Strength nor Friction is among them: Friction is the wheel's, which Auto measures, and Strength is left as it is.</remarks>
		public class SpringCar
		{
			public SpringCar(string name, int centre, int peak, int lightening, int damping, int speed, int range)
			{
				Name = name;
				Centre = centre;
				Peak = peak;
				Lightening = lightening;
				Damping = damping;
				Speed = speed;
				Range = range;
			}

			/// <summary>The kind of car, its speed, and what its values are based on.</summary>
			public readonly string Name;
			public readonly int Centre;
			public readonly int Peak;
			public readonly int Lightening;
			public readonly int Damping;
			public readonly int Speed;
			/// <summary>The wheel range, lock to lock, of the entry on the Wheel range list whose name is this kind of car.</summary>
			public readonly int Range;

			public override string ToString()
			{
				return Name;
			}
		}

		/// <summary>The cars on the Car list, in its order.</summary>
		/// <remarks>
		/// No standard sets how a steering wheel must feel, so each car names what it is based on. The family car at
		/// 100 km/h is worked out from measurements taken in the ISO 13674-1 weave test; in town and parking are the same
		/// car read at a lower speed; the sports car and the van are estimates within the range measured on many cars in
		/// the same test. The first is the settings' defaults. Each is a road car, so each turns the range the Wheel range
		/// list names for road and sports cars.
		/// </remarks>
		public static readonly SpringCar[] SpringCars =
		{
			new SpringCar("Family car, 100 km/h (ISO 13674-1 weave test, measured)", 12, 60, 40, 5, 100, RoadCarRange),
			new SpringCar("Family car in town, 40 km/h (same car, slower)", 12, 60, 40, 5, 40, RoadCarRange),
			new SpringCar("Parking (same car, 10 km/h)", 12, 60, 40, 0, 10, RoadCarRange),
			new SpringCar("Sports car, 100 km/h (estimate, firm end of the ISO 13674-1 range)", 8, 40, 30, 5, 100, RoadCarRange),
			new SpringCar("Heavy van, 80 km/h (estimate, slow end of the ISO 13674-1 range)", 30, 120, 50, 10, 80, RoadCarRange),
		};

		/// <summary>The range of "900° - Road and sports cars" on the Wheel range list.</summary>
		public const int RoadCarRange = 900;

		/// <summary>What the Car list shows when the values match no car.</summary>
		public const string CustomSpringCar = "Custom";

		/// <summary>The place on <see cref="SpringCars"/> of the car with these values, or -1 when none has them.</summary>
		/// <remarks>The wheel range is not compared: settings left empty keep the range as it is, and are still the family car.</remarks>
		public static int SpringCarIndex(int centre, int peak, int lightening, int damping, int speed)
		{
			for (var i = 0; i < SpringCars.Length; i++)
			{
				var car = SpringCars[i];
				if (car.Centre == centre && car.Peak == peak && car.Lightening == lightening && car.Damping == damping && car.Speed == speed)
					return i;
			}
			return -1;
		}

		/// <summary>Guards the Car list and the sliders against answering each other in a loop.</summary>
		bool _settingSpringCar;

		/// <summary>Shows on the Car list the car the sliders' values match, or Custom.</summary>
		void ShowSpringCar()
		{
			if (_settingSpringCar || ForceSpringCarComboBox.Items.Count == 0)
				return;
			_settingSpringCar = true;
			try
			{
				var i = SpringCarIndex(ForceSpringCentreTrackBar.Value, ForceSpringPeakTrackBar.Value, ForceSpringLighteningTrackBar.Value,
					ForceSpringDampingTrackBar.Value, ForceSpringSpeedTrackBar.Value);
				ForceSpringCarComboBox.SelectedIndex = i >= 0 ? i : SpringCars.Length;
			}
			finally
			{
				_settingSpringCar = false;
			}
		}

		/// <summary>A chosen car sets Centre, Peak, Lightening, Damping, Speed and the wheel range, which save as any other move does. Custom changes nothing.</summary>
		void ForceSpringCarComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_settingSpringCar)
				return;
			var i = ForceSpringCarComboBox.SelectedIndex;
			if (i < 0 || i >= SpringCars.Length)
			{
				// The list goes back to the car the values still match, once the choice is over.
				BeginInvoke((Action)ShowSpringCar);
				return;
			}
			var car = SpringCars[i];
			_settingSpringCar = true;
			try
			{
				ForceSpringCentreTrackBar.Value = car.Centre;
				ForceSpringPeakTrackBar.Value = car.Peak;
				ForceSpringLighteningTrackBar.Value = car.Lightening;
				ForceSpringDampingTrackBar.Value = car.Damping;
				ForceSpringSpeedTrackBar.Value = car.Speed;
				for (var r = 0; r < WheelRangeComboBox.Items.Count; r++)
				{
					var item = WheelRangeComboBox.Items[r] as KeyValuePair;
					if (item != null && item.Value == car.Range.ToString())
						WheelRangeComboBox.SelectedIndex = r;
				}
			}
			finally
			{
				_settingSpringCar = false;
			}
		}

		/// <summary>The steering range chosen on the Wheel range list, or nought when it leaves the wheel as it is.</summary>
		int SelectedWheelRange()
		{
			var item = WheelRangeComboBox.SelectedItem as KeyValuePair;
			int range;
			return item != null && int.TryParse(item.Value, out range) ? range : 0;
		}

		/// <summary>Gives the chart the curve the page's settings make, counted against the range the engine counts it against on the tab's wheel. The chart draws it again only when it changed.</summary>
		void UpdateSpringChart()
		{
			var ud = GetSelectedDevice();
			var driven = ud == null ? null : DInput.DeviceRouting.Current.ForceDevice(ud);
			WheelChartUserControl.SetCurve(ForceSpringStrengthTrackBar.Value, ForceSpringCentreTrackBar.Value, ForceSpringPeakTrackBar.Value,
				ForceSpringLighteningTrackBar.Value, ForceFeedbackState.SpringRange(SelectedWheelRange(), driven), ForceSpringSpeedTrackBar.Value);
		}

		/// <summary>Puts the wheel's angle, the live speed and the friction Auto measured on the chart, while the Wheel tab is shown.</summary>
		/// <remarks>
		/// Read from the device whose motors the engine drives, a Raw Input device's DirectInput twin, as the engine reads
		/// it for the spring: the axis the spring's motor turns, or the first axis before force feedback has found it.
		/// </remarks>
		/// <param name="hidden">True to put them there while the tab is not shown too, as when the page shows another controller.</param>
		void UpdateWheelChart(bool hidden = false)
		{
			if (!hidden && PadTabControl.SelectedTab != WheelTabPage)
				return;
			// The wheel the range is counted for can change with the selection or arrive after the page was drawn.
			UpdateSpringChart();
			var ud = GetSelectedDevice();
			var driven = ud == null ? null : DInput.DeviceRouting.Current.ForceDevice(ud);
			// The friction is the wheel's, so the slider shows the wheel's, and the default greyed without one.
			ControlsHelper.SetEnabled(ForceSpringFrictionTrackBar, driven != null);
			var friction = driven == null ? UserDevice.DefaultForceSpringFriction : Math.Min(100, driven.GetForceSpringFriction());
			if (ForceSpringFrictionTrackBar.Value != friction)
			{
				_showingFriction = true;
				ForceSpringFrictionTrackBar.Value = friction;
				_showingFriction = false;
			}
			int? position = null;
			var state = driven == null ? null : driven.SourceState;
			if (state != null && state.Axis != null)
			{
				var index = driven.FFState != null && driven.FFState.SpringAxisIndex >= 0 ? driven.FFState.SpringAxisIndex : 0;
				if (index < state.Axis.Length)
					position = state.Axis[index];
			}
			WheelChartUserControl.UpdateWheel(position,
				driven == null ? null : driven.ForceSpringSpeedOverride,
				friction);
		}

		#endregion

		/// <summary>The device the Auto button's run is on, held here because the tab can be switched to another while it runs.</summary>
		UserDevice springAutoDevice;

		/// <summary>Starts the run that measures the wheel's friction and sets Friction a tenth above it.</summary>
		/// <remarks>
		/// The engine drives the wheel and does the measuring; this side only starts it, watches it
		/// through a timer, and keeps the answer with the wheel, where the Friction slider shows it.
		/// The button waits, greyed, until the run ends on its own, which it does within a minute whatever happens.
		/// </remarks>
		void ForceSpringAutoButton_Click(object sender, EventArgs e)
		{
			var ud = GetSelectedDevice();
			// The device whose motors the engine drives, which runs the measuring: a Raw Input device's DirectInput twin.
			var driven = ud == null ? null : DInput.DeviceRouting.Current.ForceDevice(ud);
			if (driven == null || driven.DiActuatorCount == 0 || !ForceSpringEnableCheckBox.Checked)
			{
				WheelDescriptionLabel.Text = "Auto needs a connected wheel with force feedback, with Centering Spring ticked.";
				return;
			}
			// The engine drives the run only for a device whose spring the routing takes from this tab. A tab switched
			// off, or a device unticked in the tab's list or on the Devices page, drives nothing, and the run would
			// never end.
			if (!SpringFromThisTab(ud))
			{
				WheelDescriptionLabel.Text = "Auto needs this controller tab switched on, this device ticked in its list and on the Devices page, and no other controller tab's settings driving it. Otherwise nothing drives the wheel.";
				return;
			}
			WheelDescriptionLabel.Text = "Hands off the wheel.";
			ForceSpringAutoButton.Text = "Wait...";
			ForceSpringAutoButton.Enabled = false;
			springAutoDevice = driven;
			driven.SpringCalibration = new SpringCalibration(GetSelectedPadSetting(), driven.ForceSpringSpeedOverride, driven);
			SpringAutoTimer.Start();
		}

		/// <summary>Whether the engine holds the device's centering spring with this tab's settings.</summary>
		/// <remarks>
		/// Only for a device on this tab, with the tab switched on for the current game, the device ticked in its list and on
		/// the Devices page, and no other tab's settings driving it. Asked of the device whose motors the engine drives: a Raw
		/// Input device's DirectInput twin.
		/// </remarks>
		bool SpringFromThisTab(UserDevice ud)
		{
			var route = ForceRoute(DInput.DeviceRouting.Current, ud);
			return route != null && route.SettingsPad == (int)MappedTo - 1;
		}

		/// <summary>Where the force of the device whose motors the engine drives for this one comes from, or null when it is on no switched-on tab.</summary>
		/// <remarks>The device driven is a Raw Input device's DirectInput twin, or else the device itself.</remarks>
		static DInput.DeviceForce ForceRoute(DInput.DeviceRouting routing, UserDevice ud)
		{
			var driven = ud == null ? null : routing.ForceDevice(ud);
			DInput.DeviceForce route;
			return driven != null && routing.TryGetForce(driven.InstanceGuid, out route) ? route : null;
		}

		/// <summary>What the page says while the centering spring is ticked but does not reach the wheel, or null.</summary>
		/// <param name="springOn">Whether the centering spring is ticked.</param>
		/// <param name="pad">This tab's pad index, 0 to 3.</param>
		/// <param name="route">Where the wheel's force comes from, as <see cref="ForceRoute"/> gives it, or null when it is on no switched-on tab.</param>
		public static string SpringNote(bool springOn, int pad, DInput.DeviceForce route)
		{
			if (!springOn)
				return null;
			if (route == null)
				return "The centering spring does nothing now: this controller tab is switched off for the current game, or the wheel is unticked in its list or on the Devices page.";
			if (route.SettingsPad >= 0 && route.SettingsPad != pad)
				return string.Format("The centering spring here does nothing: Controller {0} is the lowest enabled controller tab this wheel is on, so its settings drive it. Change the spring there.", route.SettingsPad + 1);
			return null;
		}

		/// <summary>What the page says while a Wheel range is chosen for a wheel that x360ce cannot set the range of, or null.</summary>
		/// <remarks>
		/// Only the wheels <see cref="LogitechWheel.SupportsRange"/> names take a range from a program. Any other turns the range its own
		/// software sets, and Wheel range only tells the centering spring what range that is, so a player who chose 270° still steered
		/// the whole wheel (issue #1650).
		/// </remarks>
		/// <param name="wheelRange">The range chosen on the Wheel range list, or nought when it leaves the wheel as it is.</param>
		/// <param name="takesRange">Whether the wheel whose motors the engine drives takes a range from x360ce.</param>
		public static string RangeNote(int wheelRange, bool takesRange)
		{
			if (wheelRange <= 0 || takesRange)
				return null;
			return string.Format("x360ce cannot set this wheel's range: it turns what its own software sets. Set {0}° there, or shorten the steering with a formula in the steering box: =a1*900/270 steers as 270° on a wheel set to 900°.", wheelRange);
		}

		/// <summary>Whether the wheel whose motors the engine drives for this device takes a range from x360ce.</summary>
		static bool TakesRange(UserDevice ud)
		{
			var driven = DInput.DeviceRouting.Current.ForceDevice(ud);
			return LogitechWheel.SupportsRange(driven.DevVendorId, driven.DevProductId);
		}

		/// <summary>Whether the page shows <see cref="SpringNote"/> or <see cref="RangeNote"/>, and the text it showed before, put back when the note goes.</summary>
		bool springNoteShown;
		string springNoteHid;

		/// <summary>Shows or takes away the note that the spring or the wheel range reaches nothing, when that changes.</summary>
		/// <remarks>
		/// The spring's strength changes nothing while the tab is switched off for the current game, and the wheel range
		/// changes nothing on a wheel that takes no range, so the page says why. Left alone while Auto runs, which writes
		/// the same line.
		/// </remarks>
		void UpdateSpringNote()
		{
			if (springAutoDevice != null)
				return;
			var ud = GetSelectedDevice();
			var note = ud == null ? null : SpringNote(ForceSpringEnableCheckBox.Checked, (int)MappedTo - 1, ForceRoute(DInput.DeviceRouting.Current, ud))
				?? RangeNote(SelectedWheelRange(), TakesRange(ud));
			// A note already shown is written again when its reason changes, as when another tab comes to drive the wheel.
			if (note != null && springNoteShown)
			{
				ControlsHelper.SetText(WheelDescriptionLabel, "{0}", note);
				return;
			}
			if ((note != null) == springNoteShown)
				return;
			springNoteShown = note != null;
			if (springNoteShown)
			{
				springNoteHid = WheelDescriptionLabel.Text;
				WheelDescriptionLabel.Text = note;
			}
			else
			{
				WheelDescriptionLabel.Text = springNoteHid;
			}
		}

		void SpringAutoTimer_Tick(object sender, EventArgs e)
		{
			var ud = springAutoDevice;
			var run = ud?.SpringCalibration;
			if (run != null && !run.IsFinished)
			{
				// The run says what it is doing and what it is after, so the page is not silent for half a minute.
				WheelDescriptionLabel.Text = run.Status;
				return;
			}
			SpringAutoTimer.Stop();
			ForceSpringAutoButton.Text = "Auto";
			ForceSpringAutoButton.Enabled = true;
			springAutoDevice = null;
			if (run == null)
				return;
			ud.SpringCalibration = null;
			if (run.Result > 0)
			{
				// Kept with the wheel, for every car; the slider shows it on the next refresh.
				ud.SetForceSpringFriction(run.Result);
				WheelDescriptionLabel.Text = string.Format(
					"Friction set to {0} %: the wheel turns at {1} % and {2} % from the two stops, comes home within {3} s at {4} % and {5} %, and a tenth more starts it every time. "
					+ "Outside the centre the spring never pushes less, so the wheel comes home from any angle, and Strength adds the car's steering on top.",
					run.Result, run.LowTurnsAt, run.HighTurnsAt, SpringCalibration.ReturnWithinMs / 1000, run.LowLevel, run.HighLevel);
			}
			else
			{
				WheelDescriptionLabel.Text = run.Message;
			}
		}

		void MotorTrackBar_ValueChanged(object sender, EventArgs e)
		{
			//if (gamePadState == null) return;
			UpdateForceFeedBack();
		}

		void MotorPeriodTrackBar_ValueChanged(object sender, EventArgs e)
		{
			// Convert Direct Input Period force feedback effect parameter value.
			int leftMotorPeriod = (int)LeftMotorPeriodTrackBar.Value * PeriodMsPerStep;
			int rightMotorPeriod = (int)RightMotorPeriodTrackBar.Value * PeriodMsPerStep;
			LeftMotorPeriodTextBox.Text = string.Format("{0}", leftMotorPeriod);
			RightMotorPeriodTextBox.Text = string.Format("{0}", rightMotorPeriod);
			ShowMotorPeriodPreset(leftMotorPeriod, rightMotorPeriod);
		}

		/// <summary>Milliseconds per step of the period sliders: 0 to 400, so every preset lands on a step.</summary>
		const int PeriodMsPerStep = 4;

		/// <summary>Guards the preset box and the sliders against answering each other in a loop.</summary>
		bool _settingMotorPeriods;

		/// <summary>Shows in the preset box which multiplier the two sliders stand for, or Custom.</summary>
		void ShowMotorPeriodPreset(int leftMs, int rightMs)
		{
			if (_settingMotorPeriods)
				return;
			_settingMotorPeriods = true;
			try
			{
				var k = MotorModel.MultiplierOf(leftMs, rightMs);
				MotorPeriodPresetComboBox.SelectedIndex = k.HasValue ? Array.IndexOf(MotorModel.Multipliers, k.Value) + 1 : 0;
			}
			finally
			{
				_settingMotorPeriods = false;
			}
		}

		/// <summary>Fills the preset box: Custom, then every multiplier the model offers.</summary>
		void InitMotorPeriodPresets()
		{
			MotorPeriodPresetComboBox.Items.Clear();
			MotorPeriodPresetComboBox.Items.Add("Custom");
			foreach (var k in MotorModel.Multipliers)
				MotorPeriodPresetComboBox.Items.Add(MotorModel.Describe(k));
			MotorPeriodPresetComboBox.SelectedIndex = 0;
		}

		/// <summary>A chosen multiplier sets both period sliders; Custom changes nothing.</summary>
		private void MotorPeriodPresetComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_settingMotorPeriods || MotorPeriodPresetComboBox.SelectedIndex < 1)
				return;
			var k = MotorModel.Multipliers[MotorPeriodPresetComboBox.SelectedIndex - 1];
			_settingMotorPeriods = true;
			try
			{
				LeftMotorPeriodTrackBar.Value = MotorModel.PeriodAtFullMs(k, true) / PeriodMsPerStep;
				RightMotorPeriodTrackBar.Value = MotorModel.PeriodAtFullMs(k, false) / PeriodMsPerStep;
			}
			finally
			{
				_settingMotorPeriods = false;
			}
		}

		/// <summary>Shows what the motors do and what the presets mean: the Force Feedback help document.</summary>
		private void MotorInfoButton_Click(object sender, EventArgs e)
		{
			HelpForm.Show(this, "Force Feedback: motors and periods", AppHelper.HelpForceFeedbackResource);
		}

		/// <summary>Puts every setting on the Force Feedback page back to its default.</summary>
		/// <remarks>
		/// Only this page: the mappings, dead zones and the rest of the pad stay as they are. The
		/// defaults are the ones the settings declare, read the same way a new mapping reads them.
		/// </remarks>
		private void ForceDefaultsButton_Click(object sender, EventArgs e)
		{
			foreach (var map in SettingsManager.Current.SettingsMap.Where(x => x.MapTo == MappedTo && IsOnForceFeedbackPage(x.Control)))
				SettingsManager.Current.LoadSetting(map.Control, map.IniKey, string.Format("{0}", map.DefaultValue ?? ""));
		}

		bool IsOnForceFeedbackPage(Control control)
		{
			for (var c = control; c != null; c = c.Parent)
				if (c == ForceFeedbackTabPage)
					return true;
			return false;
		}

		public void UpdateForceFeedBack()
		{
			if (MainForm.Current.Main.ControllerIndex == -1)
				return;
			LeftMotorTestTextBox.Text = string.Format("{0} %", LeftMotorTestTrackBar.Value);
			RightMotorTestTextBox.Text = string.Format("{0} %", RightMotorTestTrackBar.Value);
			SendVibration(MappedTo, LeftMotorTestTrackBar.Value, RightMotorTestTrackBar.Value);
			//UnsafeNativeMethods.Enable(false);
			//UnsafeNativeMethods.Enable(true);
		}

		/// <summary>Sends what the Test sliders ask for, by every route this tab actually uses.</summary>
		/// <remarks>
		/// The test has to exercise the same path a game would, or it tests something nobody uses. That
		/// is two paths, and a tab can be using both: the force this program drives on the mapped device,
		/// and the force passed on to a real controller whose motors are reachable no other way.
		///
		/// It read a place by tab number through the emulator's own XInput library, which is neither the
		/// place the tab's controller is in nor a library that is always loaded, so it usually did nothing
		/// at all.
		/// </remarks>
		/// <param name="mapTo">The controller whose motors are tested.</param>
		/// <param name="left">The left slider, 0 to 100.</param>
		/// <param name="right">The right slider, 0 to 100.</param>
		void SendVibration(MapTo mapTo, int left, int right)
		{
			var game = SettingsManager.CurrentGame;
			if (game == null)
				return;
			var largeMotor = (byte)ConvertHelper.ConvertRange(0, 100, byte.MinValue, byte.MaxValue, left);
			var smallMotor = (byte)ConvertHelper.ConvertRange(0, 100, byte.MinValue, byte.MaxValue, right);
			// Into the same place a game's rumble arrives, so everything downstream of it is exercised:
			// the mapped device this program drives, and the pass-through to a real controller.
			if (((EmulationType)game.EmulationType).HasFlag(EmulationType.Virtual))
				Global.DHelper.SetVibration(mapTo, largeMotor, smallMotor, 0);
			// Straight to the real controller when nothing is being emulated, because then there is no
			// rumble arriving for anything to pass on. Asked of Windows' own XInput rather than the one
			// this program can put in a game's path: the force has to reach the device, not the emulation.
			PadSetting ps;
			var place = AppHelper.GetForcePassThroughPlace(mapTo, out ps);
			if (place >= 0 && !((EmulationType)game.EmulationType).HasFlag(EmulationType.Virtual))
				// Scaled by the strengths, the same as a game's rumble is, or the test would say the
				// motors do something they will not do once a game is running.
				SystemXInput.SetVibration(place,
					(ushort)(ps.ApplyForceStrength(largeMotor, true) * 257),
					(ushort)(ps.ApplyForceStrength(smallMotor, false) * 257),
					TimeSpan.FromMilliseconds(DInputHelper.XiAnswerMs));
		}

		/// <summary>Stops the motors the Test sliders set, on every controller the page has shown, through the path the sliders set them by.</summary>
		/// <remarks>
		/// The sliders are put at nought and the stop is sent by <see cref="SendVibration"/>. It is sent here as well as
		/// by the sliders' own handler, which sends only while a controller tab is shown. A controller shown before keeps
		/// its sliders in its view, and its motors run on until stopped here.
		/// </remarks>
		public bool StopTestVibration()
		{
			var stopped = false;
			foreach (var pair in _Views)
			{
				if (pair.Key == MappedTo || (pair.Value.LeftMotor == 0 && pair.Value.RightMotor == 0))
					continue;
				pair.Value.LeftMotor = 0;
				pair.Value.RightMotor = 0;
				SendVibration(pair.Key, 0, 0);
				stopped = true;
			}
			if (LeftMotorTestTrackBar.Value == 0 && RightMotorTestTrackBar.Value == 0)
				return stopped;
			LeftMotorTestTrackBar.Value = 0;
			RightMotorTestTrackBar.Value = 0;
			SendVibration(MappedTo, 0, 0);
			return true;
		}

		void AxisToDPadOffsetTrackBar_ValueChanged(object sender, EventArgs e)
		{
			TrackBar control = (TrackBar)sender;
			AxisToDPadOffsetTextBox.Text = string.Format("{0} %", control.Value);
			UpdateAxisToDPadChart();
		}

		void AxisToDPadDeadZoneTrackBar_ValueChanged(object sender, EventArgs e)
		{
			TrackBar control = (TrackBar)sender;
			AxisToDPadDeadZoneTextBox.Text = string.Format("{0} %", control.Value);
			UpdateAxisToDPadChart();
		}

		/// <summary>Draws the Axis to D-Pad chart for the sliders, as the values they store.</summary>
		void UpdateAxisToDPadChart()
		{
			Func<TrackBar, string, int> stored = (bar, key) => Convert.ToInt32(bar.Value / 100F * SettingsManager.TrackBarFullScale(key));
			AxisToDPadChart.SetSettings(stored(AxisToDPadDeadZoneTrackBar, SettingName.AxisToDPadDeadZone),
				stored(AxisToDPadOffsetTrackBar, SettingName.AxisToDPadOffset));
		}

		void ClearPresetButton_Click(object sender, EventArgs e)
		{
			ClearAll();
		}

		bool ClearAll()
		{
			var description = Attributes.GetDescription(MappedTo);
			var text = string.Format("Do you want to clear all {0} settings?", description);
			var form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			var result = form.ShowForm(text, "Clear Controller Settings", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			if (result != DialogResult.Yes)
				return false;
			SettingsManager.Current.LoadPadSettingsIntoSelectedDevice(MappedTo, GetSelectedSetting(), null);
			return true;
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
			var ud = GetSelectedDevice();
			if (ud == null)
				return;
			var description = Attributes.GetDescription(MappedTo);
			var form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			var buttons = MessageBoxButtons.YesNo;
			var text = string.Format("Do you want to fill all {0} settings automatically?", description);
			if (!AutoMapHelper.CanGetAutoPreset(ud))
			{
				text = string.Format("Device is offline. Please connect device to fill all {0} settings automatically.", description);
				buttons = MessageBoxButtons.OK;
			}
			var result = form.ShowForm(text, "Auto Controller Settings", buttons, MessageBoxIcon.Question);
			if (result != DialogResult.Yes)
				return;
			string note;
			var padSetting = AutoMapHelper.GetAutoPreset(ud, out note);
			// Load created setting.
			SettingsManager.Current.LoadPadSettingsIntoSelectedDevice(MappedTo, GetSelectedSetting(), padSetting);
			// Says whether SDL's controller database or x360ce's own guess filled it.
			MainForm.Current.StatusTimerLabel.Text = note;
		}

		#region SDL page

		/// <summary>The SDL page: what SDL's controller database says about the selected device.</summary>
		internal TabPage SdlTabPage;
		SdlUserControl SdlPanel;

		/// <summary>Fills every mapping box from the device's SDL row, as the SDL page shows it, and leaves the controller's other settings as they are.</summary>
		/// <remarks>A box the row leaves empty is emptied, so nothing mapped before stays beside SDL's mapping.</remarks>
		void SdlPanel_ApplyClicked(object sender, EventArgs e)
		{
			var sdl = SdlPanel.Translation;
			var setting = GetSelectedSetting();
			var ps = sdl == null || setting == null ? null : CloneCurrentPadSetting();
			if (ps == null)
				return;
			foreach (var code in SdlConverter.MappingBoxes)
				SdlConverter.SetValue(ps, code, SdlConverter.GetValue(sdl.Setting, code));
			ps.PadSettingChecksum = ps.CleanAndGetCheckSum();
			SettingsManager.Current.LoadPadSettingsIntoSelectedDevice(MappedTo, setting, ps);
			var skipped = sdl.SkippedText();
			MainForm.Current.StatusTimerLabel.Text = "Mapped as SDL's controller database maps " + sdl.Mapping.Name + "." + (skipped.Length == 0 ? "" : " " + skipped);
		}

		void SdlPanel_CopyClicked(object sender, EventArgs e)
		{
			SettingsManager.CopyPresetToClipboard(GetSelectedPadSetting(), SettingsManager.PresetFormat.Sdl, GetSelectedDevice());
			MainForm.Current.StatusTimerLabel.Text = "Copied this controller's mapping as a row of SDL's controller database.";
		}

		#endregion


		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				// What these belong to lives as long as the program. Left subscribed, a page made and
				// let go of went on handling every change afterwards, on whichever thread had made it.
				Global.UpdateControlFromStates -= Global_UpdateControlFromStates;
				SettingsManager.OptionsData.Items.ListChanged -= Items_ListChanged;
				SettingsManager.Current.SettingChanged -= Current_SettingChanged;
				SettingsManager.UserSettings.Items.ListChanged -= UserSettings_Items_ListChanged;
			}
			if (disposing && (components != null))
			{
				_Imager.Dispose();
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Show or hide every navigation glyph on this controller picture at once.
		/// </summary>
		/// <returns>How many glyphs were switched.</returns>
		/// <remarks>Used by the debug panel to check glyph placement in a single look.</remarks>
		public int ShowAllNavImages(bool show)
		{
			if (XboxImage == null)
				return 0;
			var count = 0;
			foreach (var info in imageInfos)
			{
				XboxImage.SetImage(info.Code, NavImageType.Normal, show);
				count++;
			}
			return count;
		}

		private void LeftMotorStrengthTrackBar_ValueChanged(object sender, EventArgs e)
		{
			var control = (TrackBar)sender;
			LeftMotorStrengthTextBox.Text = string.Format("{0} %", control.Value);
		}

		private void RightMotorStrengthTrackBar_ValueChanged(object sender, EventArgs e)
		{
			var control = (TrackBar)sender;
			RightMotorStrengthTextBox.Text = string.Format("{0} %", control.Value);
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
			presetForm.InitForm(GetSelectedDevice());
			ControlsHelper.CheckTopMost(presetForm);
			var result = presetForm.ShowDialog();
			if (result == DialogResult.OK)
			{
				var ps = presetForm.SelectedItem;
				if (ps != null)
					SettingsManager.Current.LoadPadSettingsIntoSelectedDevice(MappedTo, GetSelectedSetting(), ps);
			}
			presetForm.UnInitForm();
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
			if (selectedUserDevices == null || selectedUserDevices.Length == 0)
				return;
			MapDevices(game, selectedUserDevices);
		}

		/// <summary>Asks whether devices already on another tab are moved here or copied: Yes moves them, No copies them, anything else adds nothing.</summary>
		/// <remarks>Given the question, shown in bold, and what each answer does under it. A test replaces it, since the real one opens a window.</remarks>
		public static Func<string, string, DialogResult> AskMoveOrCopy = AskAboutSharedDevices;

		static DialogResult AskAboutSharedDevices(string heading, string text)
		{
			var form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			ControlsHelper.CheckTopMost(form);
			var answer = form.ShowForm(text, "Device already on a controller",
				MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question,
				MessageBoxDefaultButton.Button1,
				new[] { "&Move", "&Copy" }, heading);
			form.Dispose();
			return answer;
		}

		/// <summary>The devices already on another tab of the game, each with the tabs it is on.</summary>
		/// <remarks>A device already on this tab is left out: adding it again changes nothing.</remarks>
		static List<KeyValuePair<string, MapTo[]>> SharedDevices(UserGame game, MapTo mappedTo, UserDevice[] devices)
		{
			var shared = new List<KeyValuePair<string, MapTo[]>>();
			foreach (var ud in devices)
			{
				var tabs = SettingsManager.GetDeviceTabs(game.FileName, ud.InstanceGuid);
				if (tabs.Length == 0 || tabs.Contains(mappedTo))
					continue;
				var name = string.IsNullOrEmpty(ud.ProductName) ? ud.InstanceName : ud.ProductName;
				shared.Add(new KeyValuePair<string, MapTo[]>(name, tabs));
			}
			return shared;
		}

		/// <summary>The question Add asks, shown in bold, or null when no device is on another tab.</summary>
		public static string SharedDeviceHeading(UserGame game, MapTo mappedTo, UserDevice[] devices)
		{
			var shared = SharedDevices(game, mappedTo, devices);
			return shared.Count == 0 ? null : string.Format("Move or Copy {0}?", string.Join(", ", shared.Select(x => x.Key)));
		}

		/// <summary>What Add says under the question: what each answer does. Null when no device is on another tab.</summary>
		public static string SharedDeviceQuestion(UserGame game, MapTo mappedTo, UserDevice[] devices)
		{
			var shared = SharedDevices(game, mappedTo, devices);
			if (shared.Count == 0)
				return null;
			var from = string.Join(", ", shared.SelectMany(x => x.Value).Distinct().OrderBy(x => x)
				.Select(x => Attributes.GetDescription(x)));
			return string.Format("Move - add here and remove from {0}.", from) + Environment.NewLine
				+ "Copy - add here and leave others.";
		}

		/// <summary>Asks whether Xbox controllers picked through DirectInput are added through Raw Input instead: Yes uses Raw Input, No adds the rows picked, anything else adds nothing.</summary>
		/// <remarks>Given the question, shown in bold, and what each answer does under it. A test replaces it, since the real one opens a window.</remarks>
		public static Func<string, string, DialogResult> AskRawInput = AskAboutRawInput;

		static DialogResult AskAboutRawInput(string heading, string text)
		{
			var form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			ControlsHelper.CheckTopMost(form);
			var answer = form.ShowForm(text, "Xbox controller through DirectInput",
				MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning,
				MessageBoxDefaultButton.Button1,
				new[] { "Use &Raw Input", "Add &DirectInput" }, heading);
			form.Dispose();
			return answer;
		}

		/// <summary>The devices to add, with each Xbox controller picked through DirectInput swapped for its Raw Input row when the person says so; null when they cancel.</summary>
		/// <remarks>
		/// Windows feeds an Xbox controller's DirectInput row only to the program in front, so the controller works on this
		/// program's pages and does nothing in the game (<see cref="DInput.XInputPlaces.ReadsOnlyInFront"/>). Asked only when
		/// the controller has a Raw Input row to offer instead.
		/// </remarks>
		public static UserDevice[] PreferRawInput(UserDevice[] devices)
		{
			var all = SettingsManager.UserDevices.ItemsToArraySynchronized();
			var twins = devices.Select(x => DInput.XInputPlaces.ReadsOnlyInFront(x) ? x.RawInputTwin(all) : null).ToArray();
			if (twins.All(x => x == null))
				return devices;
			var names = string.Join(", ", devices.Where((x, i) => twins[i] != null)
				.Select(x => string.IsNullOrEmpty(x.ProductName) ? x.InstanceName : x.ProductName).Distinct());
			var answer = AskRawInput(string.Format("Add {0} through Raw Input instead?", names),
				"Windows lets only the program in front read an Xbox controller through DirectInput, so it stops working once a game is in front. Raw Input keeps working." + Environment.NewLine
				+ "Use Raw Input - add the controller's Raw Input row." + Environment.NewLine
				+ "Add DirectInput - add the row picked, which works only while x360ce is in front.");
			if (answer == DialogResult.Yes)
				return devices.Select((x, i) => twins[i] ?? x).Distinct().ToArray();
			if (answer == DialogResult.No)
				return devices;
			return null;
		}

		/// <summary>Maps the devices to this controller for the game and selects the first of them.</summary>
		/// <remarks>
		/// A device just added is the one the person means to set up next. The list kept whatever row
		/// was selected before, so with a device already mapped the new one arrived unselected and the
		/// page went on showing the old one's settings until it was picked by hand.
		///
		/// A device already on another tab of the game is asked about first: moved here, copied, or
		/// not added.
		/// </remarks>
		public void MapDevices(UserGame game, UserDevice[] devices)
		{
			devices = PreferRawInput(devices);
			if (devices == null)
				return;
			var copy = false;
			var question = SharedDeviceQuestion(game, MappedTo, devices);
			if (question != null)
			{
				var answer = AskMoveOrCopy(SharedDeviceHeading(game, MappedTo, devices), question);
				if (answer != DialogResult.Yes && answer != DialogResult.No)
					return;
				copy = answer == DialogResult.No;
			}
			SettingsManager.MapGamePadDevices(game, MappedTo, devices,
				SettingsManager.Options.HidHideConfigureAutomatically, copy);
			// Selecting the row loads its settings into the page and saves the page into it.
			ShowHideAndSelectGridRows(devices[0].InstanceGuid);
		}

		private void RemoveMapButton_Click(object sender, EventArgs e)
		{
			var game = SettingsManager.CurrentGame;
			// Return if game is not selected.
			if (game == null)
				return;
			var setting = GetSelectedSetting();
			SettingsManager.UnMapGamePadDevices(game, setting,
				SettingsManager.Options.HidHideConfigureAutomatically);
		}

		void UpdateGridButtons()
		{
			ControlsHelper.SetEnabled(RemoveMapButton, MappedDevicesDataGridView.SelectedRows.Count > 0);
		}

		/// <summary>Shows the XInput places again, sized to what they now say.</summary>
		/// <remarks>
		/// The place text is made while the table paints and stored nowhere, so the column that
		/// sizes itself to its cells never saw a change and kept the width of the first paint; a
		/// device reaching several places was cut off. Measured again whenever the places can have
		/// moved, which is whenever the device list has been read.
		/// </remarks>
		public void RefreshPlaces()
		{
			if (InvokeRequired)
			{
				BeginInvoke((Action)RefreshPlaces);
				return;
			}
			MappedDevicesDataGridView.AutoResizeColumn(XInputPlaceColumn.Index);
			MappedDevicesDataGridView.Invalidate();
		}

		/// <summary>The tooltip of a row's Enabled box when its device is unticked on the Devices page.</summary>
		const string SwitchedOffOnDevicesPage = "Switched off on the Devices page";

		private void MappedDevicesDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;
			var grid = (DataGridView)sender;
			var item = AppHelper.BoundItem<Engine.Data.UserSetting>(grid, e.RowIndex);
			if (item is null)
				return;
			var column = grid.Columns[e.ColumnIndex];
			// A setting can outlive the device it maps to, so the device is null for a mapping whose device is gone.
			var device = SettingsManager.GetDevice(item.InstanceGuid);
			// Tinted by the source the device is read through, as on the Devices page.
			e.CellStyle.BackColor = AppHelper.GetInputSourceTint(device);
			if (column == IsOnlineColumn)
			{
				e.Value = AppHelper.GetOnlineIcon(item.IsOnline);
			}
			else if (column == IsEnabledColumn)
			{
				// A device unticked on the Devices page is left out of every game, whatever its box here says.
				var off = device != null && !device.IsEnabled;
				grid.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText = off ? SwitchedOffOnDevicesPage : "";
				if (off)
				{
					e.CellStyle.BackColor = SystemColors.Control;
					// The darker grey, which the selected text reads on in both themes.
					e.CellStyle.SelectionBackColor = SystemColors.ControlDarkDark;
				}
			}
			else if (column == XInputPlaceColumn)
			{
				e.Value = AppHelper.GetXInputPlaces(device);
			}
			else if (column == ConnectionClassColumn)
			{
				// A mapping whose device is gone has an unknown connection class.
				e.Value = AppHelper.GetConnectionClassIcon(device?.ConnectionClass ?? Guid.Empty);
			}
			else if (column == InstanceIdColumn)
			{
				// Hide device Instance GUID from public eyes. Show part of checksum.
				e.Value = EngineHelper.GetID(item.InstanceGuid);
			}
			else if (column == SourceColumn)
			{
				e.Value = AppHelper.GetInputSourceName(device);
			}
			else if (column == ScriptNameColumn)
			{
				int number;
				e.Value = _deviceNumbers.TryGetValue(item, out number) ? "c" + item.MapTo + ".d" + number : "";
			}
			else if (column == SettingIdColumn)
			{
				// Hide device Setting GUID from public eyes. Show part of checksum.
				e.Value = EngineHelper.GetID(item.PadSettingChecksum);
			}
			else if (column == VendorNameColumn)
			{
				e.Value = device == null
					? ""
					: device.DevManufacturer;
			}
			else if (column == ProductNameColumn)
			{
				// The device's name as it is now: the setting keeps the one the device had when it was mapped.
				if (device != null && !string.IsNullOrEmpty(device.ProductName))
					e.Value = device.ProductName;
			}
		}

		public event EventHandler<EventArgs<UserSetting>> OnSettingChanged;

		private void MappedDevicesDataGridView_SelectionChanged(object sender, EventArgs e)
		{
			// While rows come and go the grid moves its selection several times; the one that
			// stands once they are in place is announced by the rebuild itself.
			if (_RowsUpdating)
				return;
			AnnounceSelection();
		}

		/// <summary>Whether a selection has been put on the page yet, and which row with which settings.</summary>
		bool _Announced;
		UserSetting _AnnouncedSetting;
		Guid _AnnouncedChecksum;

		/// <summary>Loads the selected device's settings into the form and tells the listeners, when they are not there already.</summary>
		/// <remarks>The grid raises its selection whenever it is bound, given its window or shown, with the same row selected.</remarks>
		void AnnounceSelection()
		{
			var setting = GetSelectedSetting();
			if (!_Announced || setting != _AnnouncedSetting || (setting != null && setting.PadSettingChecksum != _AnnouncedChecksum))
			{
				var padSetting = setting == null
					? null
					: SettingsManager.GetPadSetting(setting.PadSettingChecksum);
				SettingsManager.Current.LoadPadSettingsIntoSelectedDevice(MappedTo, setting, padSetting);
				_Announced = true;
				_AnnouncedSetting = setting;
				_AnnouncedChecksum = setting == null ? Guid.Empty : setting.PadSettingChecksum;
				OnSettingChanged?.Invoke(this, new EventArgs<UserSetting>(setting));
			}
			UpdateGridButtons();
			UpdateForceFeedbackTitle();
			UpdateEffectDescription();
			UpdateForceSource();
		}

		/// <summary>The Force Feedback page's title, naming the other tabs the selected device is on.</summary>
		/// <remarks>
		/// Each tab's Enable switch decides whether that tab's game rumbles the device, so a device on two
		/// tabs is felt from every switched-on tab where it is on. The title says where else it is, and the
		/// Force Source box says which of them it plays and whose settings shape it.
		/// </remarks>
		public static string ForceFeedbackTitle(MapTo[] otherTabs)
		{
			if (otherTabs == null || otherTabs.Length == 0)
				return "Force Feedback";
			return string.Format("Force Feedback - Shared with Controller{0} {1}",
				otherTabs.Length > 1 ? "s" : "",
				string.Join(", ", otherTabs.Select(x => ((int)x).ToString())));
		}

		/// <summary>Shows on the Force Feedback page which other tabs the selected device is on.</summary>
		void UpdateForceFeedbackTitle()
		{
			ControlsHelper.SetText(ForceFeedbackGroupBox, "{0}", ForceFeedbackTitle(OtherTabs(GetSelectedSetting())));
		}

		/// <summary>The other controller tabs of the current game the row's controller is on, lowest first.</summary>
		/// <remarks>
		/// The controller counts with its twin, the same controller read through another source (<see cref="UserDevice.IsTwinOf"/>):
		/// a Raw Input row on one tab and its DirectInput twin on another drive the same motors.
		/// </remarks>
		MapTo[] OtherTabs(UserSetting setting)
		{
			var game = SettingsManager.CurrentGame;
			if (game == null || setting == null)
				return new MapTo[0];
			var devices = new HashSet<Guid> { setting.InstanceGuid };
			var ud = SettingsManager.GetDevice(setting.InstanceGuid);
			if (ud != null)
				foreach (var device in SettingsManager.UserDevices.ItemsToArraySynchronized())
					if (ud.IsTwinOf(device))
						devices.Add(device.InstanceGuid);
			return devices
				.SelectMany(x => SettingsManager.GetDeviceTabs(game.FileName, x))
				.Where(x => x != MappedTo)
				.Distinct()
				.OrderBy(x => x)
				.ToArray();
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
				// An unticked device is not used by the game, so it is not hidden from it either.
				var game = SettingsManager.CurrentGame;
				if (game != null && SettingsManager.Options.HidHideConfigureAutomatically)
					SettingsManager.HideMappedDevices(game, item.InstanceGuid);
			}
		}

		#endregion

		private void EnableButton_Click(object sender, EventArgs e)
		{
			var game = SettingsManager.CurrentGame;
			// If no game selected then ignore click.
			if (game == null)
				return;
			var flag = AppHelper.GetMapFlag(MappedTo);
			// Invert flag value.
			SettingsManager.SetTabEnabled(game, MappedTo, ((MapToMask)game.EnableMask & flag) == 0);
		}

		#region Plugin device tab

		/// <summary>The tab of the plugin device selected on this page; null while it has none.</summary>
		TabPage PluginTabPage;

		/// <summary>What the plugin device's tab holds.</summary>
		PluginDeviceUserControl PluginPanel;

		/// <summary>When the plugin device's status was last read, by <see cref="Environment.TickCount"/>.</summary>
		int _pluginStatusTick;

		/// <summary>Shows the tab of the selected device when it is a plugin device with settings or a control, and reads its status once a second.</summary>
		void UpdatePluginTab(UserDevice ud)
		{
			var manager = Plugins.PluginManager.Current;
			// An export describes the program as every computer has it; a plugin device's tab is this computer's.
			var type = ud != null && ud.InputSourceType == (int)InputSourceType.Plugin && manager != null && Program.ExportUiFolder == null
				? manager.FindDeviceType(ud.ProductGuid)
				: null;
			var device = type == null || Global.DHelper == null ? null : Global.DHelper.GetPluginDevice(ud.InstanceGuid);
			ShowPluginTab(ud == null ? Guid.Empty : ud.InstanceGuid, type, device);
			if (PluginPanel != null && unchecked(Environment.TickCount - _pluginStatusTick) >= 1000)
			{
				_pluginStatusTick = Environment.TickCount;
				PluginPanel.RefreshStatus();
			}
		}

		/// <summary>Shows a plugin device's tab, last and named after the device, or takes it away.</summary>
		/// <remarks>
		/// The tab is made again when the device, its kind or the plugin instance the engine reads changes, so the plugin's
		/// own control always works the instance that is read. A device with neither settings nor a control, a kind
		/// switched off after it failed, and any other device have no tab.
		/// </remarks>
		/// <param name="instanceGuid">The device, as the device list knows it.</param>
		/// <param name="type">The device's kind, or null when it is not a plugin device.</param>
		/// <param name="device">The plugin instance the engine reads for it, or null while it reads none.</param>
		public void ShowPluginTab(Guid instanceGuid, Plugins.PluginDeviceType type, x360ce.Plugins.IPluginDevice device)
		{
			var show = type != null && !type.Faulted && type.HasTab;
			var panel = PluginPanel;
			if (show && panel != null && panel.InstanceGuid == instanceGuid && panel.Type == type && panel.Device == device)
				return;
			// A tab made again while it is open stays open.
			var selected = PluginTabPage != null && PadTabControl.SelectedTab == PluginTabPage;
			if (PluginTabPage != null)
			{
				PadTabControl.TabPages.Remove(PluginTabPage);
				PluginTabPage.Dispose();
				PluginTabPage = null;
				PluginPanel = null;
			}
			if (!show)
				return;
			PluginPanel = new PluginDeviceUserControl(instanceGuid, type, device, Plugins.PluginSettings.Current) { Dock = DockStyle.Fill };
			PluginTabPage = new TabPage { Name = nameof(PluginTabPage), Text = type.Name, AccessibleName = type.Name, UseVisualStyleBackColor = true };
			PluginTabPage.AccessibleDescription = "The settings of the plugin device mapped here, and the controls its plugin adds.";
			PluginTabPage.Controls.Add(PluginPanel);
			PadTabControl.TabPages.Add(PluginTabPage);
			if (selected)
				PadTabControl.SelectedTab = PluginTabPage;
		}

		#endregion

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
			UpdateEffectDescription();
			UpdateForceControls();
		}

		/// <summary>Whether another tab's settings shape the selected device's force, as <see cref="UpdateForceSource"/> last worked it out.</summary>
		bool otherTabShapesForce;

		/// <summary>Greys out the force settings that do nothing: every setting that shapes the force while another tab's settings shape it, and the motor periods while the effect type is Constant, which has no period.</summary>
		/// <remarks>
		/// Enable, Pass Through, Send To and the Test sliders stay live on every tab: they belong to this tab's virtual
		/// controller, not to the device. Auto stays live too, and says why it does not start.
		/// </remarks>
		void UpdateForceControls()
		{
			var shapes = !otherTabShapesForce;
			var periodic = !(ForceTypeComboBox.SelectedItem is ForceEffectType)
				|| ((ForceEffectType)ForceTypeComboBox.SelectedItem & ~ForceEffectType._Type2) != ForceEffectType.Constant;
			var shaping = new Control[]
			{
				label1, ForceTypeComboBox, ForceSwapMotorCheckBox,
				OveralEffectsStengthLabel, ForceOverallTrackBar, ForceOverallTextBox,
				LeftMotorDirectionLabel, LeftMotorDirectionComboBox, LeftMotorStrengthLabel, LeftMotorStrengthTrackBar, LeftMotorStrengthTextBox,
				RightMotorDirectionLabel, RightMotorDirectionComboBox, RightMotorStrengthLabel, RightMotorStrengthTrackBar, RightMotorStrengthTextBox,
				ForceSpringEnableCheckBox, ForceSpringCarLabel, ForceSpringCarComboBox,
				ForceSpringStrengthLabel, ForceSpringStrengthTrackBar, ForceSpringStrengthTextBox,
				ForceSpringCentreLabel, ForceSpringCentreTrackBar, ForceSpringCentreTextBox,
				ForceSpringPeakLabel, ForceSpringPeakTrackBar, ForceSpringPeakTextBox,
				ForceSpringLighteningLabel, ForceSpringLighteningTrackBar, ForceSpringLighteningTextBox,
				ForceSpringDampingLabel, ForceSpringDampingTrackBar, ForceSpringDampingTextBox,
				ForceSpringSpeedLabel, ForceSpringSpeedTrackBar, ForceSpringSpeedTextBox,
			};
			foreach (var control in shaping)
				ControlsHelper.SetEnabled(control, shapes);
			var periods = new Control[]
			{
				MotorPeriodPresetLabel, MotorPeriodPresetComboBox,
				LeftMotorPeriodLabel, LeftMotorPeriodTrackBar, LeftMotorPeriodTextBox,
				RightMotorPeriodLabel, RightMotorPeriodTrackBar, RightMotorPeriodTextBox,
			};
			foreach (var control in periods)
				ControlsHelper.SetEnabled(control, shapes && periodic);
		}

		/// <summary>The routing, row and device the Force Source box was last written for.</summary>
		DInput.DeviceRouting forceSourceRouting;
		UserSetting forceSourceSetting;
		UserDevice forceSourceDevice;

		/// <summary>Shows the Force Source box while the selected controller is on another tab too, and greys out the settings another tab's settings overrule, when the routing or the selection changes.</summary>
		/// <remarks>Asked on every state update, so it costs a reference compare unless something changed.</remarks>
		void UpdateForceSource()
		{
			var routing = DInput.DeviceRouting.Current;
			var setting = GetSelectedSetting();
			var ud = GetSelectedDevice();
			if (routing == forceSourceRouting && setting == forceSourceSetting && ud == forceSourceDevice)
				return;
			forceSourceRouting = routing;
			forceSourceSetting = setting;
			forceSourceDevice = ud;
			var pad = (int)MappedTo - 1;
			var route = ForceRoute(routing, ud);
			otherTabShapesForce = route != null && route.SettingsPad >= 0 && route.SettingsPad != pad;
			// Only for a controller on another tab too. A Raw Input device that no force reaches is not shown it either:
			// the effect description already says so.
			var shared = ud != null && OtherTabs(setting).Length > 0
				&& RawInputForceNote(ud, SettingsManager.UserDevices.ItemsToArraySynchronized()) == null;
			ControlsHelper.SetText(ForceSourceLabel, "{0}", shared ? ForceSourceNote(pad, route) : string.Empty);
			ForceSourceGroupBox.Visible = shared;
			UpdateForceControls();
		}

		/// <summary>What the Force Source box says: whose settings shape the device's force, and which controllers' force it plays.</summary>
		/// <param name="pad">This tab's pad index, 0 to 3.</param>
		/// <param name="route">Where the device's force comes from, as <see cref="ForceRoute"/> gives it, or null when it is on no switched-on tab.</param>
		public static string ForceSourceNote(int pad, DInput.DeviceForce route)
		{
			if (route == null)
				return "No force reaches this device: this controller tab is switched off for the current game, or the device is unticked in its list or on the Devices page.";
			var lines = new List<string>();
			if (route.SettingsPad == pad)
				lines.Add("This is the lowest enabled controller tab the device is on, so these settings shape its force.");
			else if (route.SettingsPad >= 0)
				lines.Add(string.Format("Controller {0} is the lowest enabled controller tab the device is on, so its settings shape the force. Change them there; here they are greyed out.", route.SettingsPad + 1));
			var pads = route.ForcePads.Select(x => (x + 1).ToString()).ToArray();
			if (pads.Length == 0)
				lines.Add(route.Spring
					? "No game force reaches it: Force Feedback Enable is off on every controller tab it is on. Only the centering spring holds it."
					: "No game force reaches it: Force Feedback Enable is off on every controller tab it is on.");
			else if (pads.Length == 1)
				lines.Add(string.Format("It plays Controller {0}'s force.", pads[0]));
			else
				lines.Add(string.Format("It plays the strongest force of Controllers {0} and {1}: each motor at the highest value any of them asks for.",
					string.Join(", ", pads, 0, pads.Length - 1), pads[pads.Length - 1]));
			return string.Join(Environment.NewLine + Environment.NewLine, lines);
		}

		/// <summary>What the Force Feedback page says for a device read through Raw Input that no force feedback reaches, or null.</summary>
		/// <remarks>
		/// Raw Input only reads, so a Raw Input device's force goes through its DirectInput twin (<see cref="UserDevice.ForceTwin"/>),
		/// and the note is for one with no twin that takes it, such as an Xbox controller. Pass Through goes to an XInput
		/// place, not through the device's source, so it reaches an Xbox controller from either row.
		/// </remarks>
		/// <param name="devices">The devices listed, among which the twin is looked for.</param>
		public static string RawInputForceNote(UserDevice ud, IEnumerable<UserDevice> devices)
		{
			return ud != null && ud.InputSource == InputSourceType.RawInput && ud.ForceTwin(devices) == null ? RawInputForceNoteText : null;
		}

		const string RawInputForceNoteText = "No vibration or wheel forces reach this controller: Raw Input only reads, and its DirectInput side takes none. Pass Through reaches an Xbox controller.";

		/// <summary>Shows what the chosen effect type does, or <see cref="RawInputForceNote"/> in its place for a Raw Input device.</summary>
		/// <remarks>Written when the effect type changes and when another device is selected, so neither leaves the other's text.</remarks>
		void UpdateEffectDescription()
		{
			var note = RawInputForceNote(GetSelectedDevice(), SettingsManager.UserDevices.ItemsToArraySynchronized());
			if (note != null)
			{
				EffectDescriptionLabel.Text = note;
				return;
			}
			if (ForceTypeComboBox.SelectedItem == null)
			{
				// No effect type to describe yet; the note of a Raw Input device selected before goes.
				if (EffectDescriptionLabel.Text == RawInputForceNoteText)
					EffectDescriptionLabel.Text = string.Empty;
				return;
			}
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
			SettingsManager.CopyPresetToClipboard(GetSelectedPadSetting());
		}

		private void CopyPresetFormatButton_Click(object sender, EventArgs e)
		{
			SettingsManager.ShowCopyPresetMenu(CopyPresetButton, GetSelectedPadSetting(), GetSelectedDevice());
		}

		private void SavePresetButton_Click(object sender, EventArgs e)
		{
			// Named after the device, so a folder of presets says which controller each one is for.
			var ud = GetSelectedDevice();
			var name = ud == null || string.IsNullOrEmpty(ud.DisplayName) ? "Preset" : ud.DisplayName;
			foreach (var c in System.IO.Path.GetInvalidFileNameChars())
				name = name.Replace(c, '_');
			using (var dialog = new SaveFileDialog())
			{
				dialog.Title = "Save Preset";
				dialog.Filter = SettingsManager.PresetFileFilter;
				dialog.DefaultExt = "xml";
				dialog.AddExtension = true;
				dialog.FileName = name + ".xml";
				// The dialog opened in the working folder, which is the game's folder when the program
				// is started from there, and a game under Program Files refuses the write.
				dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				if (dialog.ShowDialog(this) != DialogResult.OK)
					return;
				string refusal;
				if (SettingsManager.TrySavePadSetting(dialog.FileName, GetSelectedPadSetting(), out refusal))
					return;
				// Said where the file was chosen, and the program stays up.
				var form = new JocysCom.ClassLibrary.Controls.MessageBoxForm();
				form.StartPosition = FormStartPosition.CenterParent;
				ControlsHelper.CheckTopMost(form);
				form.ShowForm(refusal);
				form.Dispose();
			}
		}

		private void PastePresetButton_Click(object sender, EventArgs e)
		{
			try
			{
				// XML, JSON or YAML, whichever Copy Preset wrote or a person typed, or SDL rows, read for the device.
				string note;
				var ps = SettingsManager.PadSettingFromText(Clipboard.GetText(), GetSelectedDevice(), out note);
				SettingsManager.Current.LoadPadSettingsIntoSelectedDevice(MappedTo, GetSelectedSetting(), ps);
				if (note.Length > 0)
					MainForm.Current.StatusTimerLabel.Text = note;
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

		List<SettingsMapItem> RecordAllMaps = new List<SettingsMapItem>();

		/// <summary>The device as it was when Remap All began: at rest, which each next control waits for.</summary>
		SourceState RemapRest;

		/// <summary>How long Remap All shows a recorded control before it starts the next one.</summary>
		/// <remarks>Short, as the next control also waits for the device to come to rest; without it the boxes moved on faster than people could follow.</remarks>
		public static TimeSpan RemapAllPause = TimeSpan.FromMilliseconds(500);

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
			// A copy: the input thread fills its states again as it reads.
			RemapRest = GetSelectedDevice()?.SourceState?.Clone();
			StartRecording();
		}

	}
}
