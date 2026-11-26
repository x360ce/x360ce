using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using x360ce.Plugins;

namespace x360ce.Plugins.Samples
{
	/// <summary>
	/// Dedicated "Game Telemetry" plugin tab on controller pages (wheels and telemetry-consuming devices):
	/// Connection &amp; Ports and Force Feel Tuning on the left; Live Telemetry and Activity on the right;
	/// plus a Help sub-tab matching x360ce's native plugin pages.
	/// Strictly follows x360ce's shared Light/Dark theme and 8 px (4 px scaled grid) one-gap spacing rule:
	/// every item carries Margin(g, g, 0, 0) and every container carries Padding(0, 0, g, g).
	/// </summary>
	public class WheelTelemetryPage : UserControl, IPluginWheelTelemetryControl
	{
		readonly IPluginWheelTelemetryHost _host;
		readonly ToolTip _toolTip;
		readonly List<string> _logEntries = new List<string>();
		readonly List<Action<int>> _spacing = new List<Action<int>>();
		Font _boldFont;
		bool _syncing;
		string _lastLoggedSource;
		string _lastLoggedPortStatus;
		bool? _lastLoggedEnabled;

		public readonly TabControl PageTabControl;
		public readonly CheckBox EnabledCheckBox;
		public readonly CheckBox StandardPortsCheckBox;
		public readonly NumericUpDown ListenPortUpDown;
		public readonly CheckBox ForwardEnabledCheckBox;
		public readonly TextBox ForwardIpTextBox;
		public readonly NumericUpDown ForwardPortUpDown;
		public readonly TrackBar AlignStrengthTrackBar;
		public readonly TextBox AlignStrengthValueTextBox;
		public readonly TrackBar UndersteerStrengthTrackBar;
		public readonly TextBox UndersteerStrengthValueTextBox;
		public readonly TrackBar CurbStrengthTrackBar;
		public readonly TextBox CurbStrengthValueTextBox;
		public readonly TrackBar DampingStrengthTrackBar;
		public readonly TextBox DampingStrengthValueTextBox;
		public readonly TrackBar RumbleUndersteerTrackBar;
		public readonly TextBox RumbleUndersteerValueTextBox;
		public readonly TrackBar SteeringLeadTrackBar;
		public readonly TextBox SteeringLeadValueTextBox;
		public readonly ComboBox RumbleSourceComboBox;
		public readonly TrackBar GearChangeRumbleTrackBar;
		public readonly TextBox GearChangeRumbleValueTextBox;
		public readonly Button DefaultsButton;

		public readonly Label StatusLabel;
		public readonly Label PortStatusLabel;
		public readonly GroupBox ReadingsGroupBox;
		public readonly TableLayoutPanel ReadingsPanel;
		public readonly Label SourceValueLabel;
		public readonly Label SpeedValueLabel;
		public readonly ProgressBar SpeedBar;
		public readonly Label AlignValueLabel;
		public readonly ProgressBar AlignBar;
		public readonly Label FrontGripValueLabel;
		public readonly ProgressBar FrontGripBar;
		public readonly Label LateralGValueLabel;
		public readonly ProgressBar LateralGBar;
		public readonly Label PitchValueLabel;
		public readonly ProgressBar PitchBar;
		public readonly Label RpmValueLabel;
		public readonly ProgressBar RpmBar;
		public readonly Label RateValueLabel;
		public readonly Label FreshnessValueLabel;
		public readonly TextBox LogTextBox;
		public readonly RichTextBox HelpRichTextBox;

		/// <summary>Markdown help rendered by x360ce's shared MarkdownRtf viewer on the Help sub-tab.</summary>
		public string HelpMarkdown { get { return BuildHelpMarkdown(); } }

		public WheelTelemetryPage(IPluginWheelTelemetryHost host)
		{
			_host = host ?? throw new ArgumentNullException(nameof(host));
			Name = nameof(WheelTelemetryPage);
			Text = "Game Telemetry";
			AccessibleName = "Game Telemetry";
			AccessibleDescription = "Live game telemetry connection, UDP port forwarding, force feel tuning and live vehicle readings.";
			Dock = DockStyle.Fill;

			_toolTip = new ToolTip();

			SuspendLayout();

			PageTabControl = new TabControl
			{
				Name = nameof(PageTabControl),
				Dock = DockStyle.Fill,
				AccessibleName = "Game Telemetry sections",
				AccessibleDescription = "Switches between live telemetry settings and protocol help.",
			};

			var telemetryTab = new TabPage
			{
				Name = "TelemetryTabPage",
				Text = "Telemetry",
				AccessibleName = "Telemetry",
				AccessibleDescription = "Telemetry ports, force feel tuning, live vehicle readings and activity log.",
				BackColor = SystemColors.Control,
				AutoScroll = true,
			};

			// Two-column grid matching x360ce's Options and Force Feedback pages:
			// container Padding(0, 0, g, g), child GroupBoxes Margin(g, g, 0, 0); FormsTheme owns GroupBox.Padding.
			var main = new TableLayoutPanel
			{
				Name = "WheelTelemetryMainTableLayoutPanel",
				Dock = DockStyle.Fill,
				ColumnCount = 2,
				RowCount = 1,
				Margin = Padding.Empty,
				Padding = Padding.Empty,
			};
			main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
			main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
			main.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

			var leftCol = new TableLayoutPanel
			{
				Name = "TelemetryLeftColumnPanel",
				Dock = DockStyle.Fill,
				ColumnCount = 1,
				RowCount = 2,
				Margin = Padding.Empty,
				Padding = Padding.Empty,
			};
			leftCol.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			leftCol.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			leftCol.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

			var rightCol = new TableLayoutPanel
			{
				Name = "TelemetryRightColumnPanel",
				Dock = DockStyle.Fill,
				ColumnCount = 1,
				RowCount = 2,
				Margin = Padding.Empty,
				Padding = Padding.Empty,
			};
			rightCol.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			rightCol.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			rightCol.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

			_spacing.Add(g =>
			{
				// Matches PluginDeviceUserControl: PageTabControl stands 1 gap inside TabPage's (0, 0, g, g) padding.
				Padding = new Padding(g, g, 0, 0);
				telemetryTab.Padding = new Padding(0, 0, g, g);
			});

			// Left top: Connection & Ports group.
			var portsGroup = new GroupBox
			{
				Name = "ConnectionGroupBox",
				Text = "Connection && Ports",
				AccessibleName = "Connection and Ports",
				AccessibleDescription = "Telemetry enable switch, dedicated UDP port, optional standard game ports and UDP IP/port forwarding.",
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Dock = DockStyle.Fill,
			};
			var portsTable = new TableLayoutPanel
			{
				Name = "ConnectionTableLayoutPanel",
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Dock = DockStyle.Fill,
				ColumnCount = 4,
				RowCount = 5,
				Margin = Padding.Empty,
				Padding = Padding.Empty,
			};
			portsTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			portsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			portsTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			portsTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			for (var i = 0; i < 5; i++)
				portsTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));

			EnabledCheckBox = new CheckBox
			{
				Name = nameof(EnabledCheckBox),
				Text = "Enable live game telemetry",
				AccessibleName = "Enable live game telemetry",
				AccessibleDescription = "Reads live vehicle telemetry from Shared Memory (Assetto Corsa, iRacing) and localhost UDP to drive wheels and telemetry-enabled devices.",
				AutoSize = true,
				Anchor = AnchorStyles.Left,
				UseVisualStyleBackColor = true,
			};
			EnabledCheckBox.CheckedChanged += (s, e) =>
			{
				if (_syncing) return;
				_host.Enabled = EnabledCheckBox.Checked;
				UpdateControlStates();
				RefreshStatus();
			};
			portsTable.Controls.Add(EnabledCheckBox, 0, 0);
			portsTable.SetColumnSpan(EnabledCheckBox, 4);

			StandardPortsCheckBox = new CheckBox
			{
				Name = nameof(StandardPortsCheckBox),
				Text = "Also listen on standard game ports",
				AccessibleName = "Listen on standard game ports",
				AccessibleDescription = "Also binds standard game UDP ports (5300, 20777, 4123, 5606). Off by default so SimHub, CrewChief and wheel dashboards keep their ports. Occupied ports are always skipped automatically.",
				AutoSize = true,
				Anchor = AnchorStyles.Left,
				UseVisualStyleBackColor = true,
			};
			StandardPortsCheckBox.CheckedChanged += (s, e) =>
			{
				if (_syncing) return;
				_host.ListenStandardPorts = StandardPortsCheckBox.Checked;
				RefreshStatus();
			};
			portsTable.Controls.Add(StandardPortsCheckBox, 0, 1);
			portsTable.SetColumnSpan(StandardPortsCheckBox, 4);

			var listenLabel = new Label
			{
				Name = "ListenPortLabel",
				Text = "Listen port",
				AutoSize = true,
				Anchor = AnchorStyles.Left,
			};
			ListenPortUpDown = new NumericUpDown
			{
				Name = nameof(ListenPortUpDown),
				AccessibleName = "Listen UDP port",
				AccessibleDescription = "Dedicated conflict-free loopback UDP port x360ce listens on (default 37150). Point games or SimHub forwarding here.",
				Minimum = 1024,
				Maximum = 65535,
				Value = WheelTelemetryProvider.DefaultPort,
				Anchor = AnchorStyles.Left,
			};
			ListenPortUpDown.ValueChanged += (s, e) =>
			{
				if (_syncing) return;
				_host.Port = (int)ListenPortUpDown.Value;
				RefreshStatus();
			};

			ForwardEnabledCheckBox = new CheckBox
			{
				Name = nameof(ForwardEnabledCheckBox),
				Text = "Enable forward telemetry",
				AccessibleName = "Enable forward telemetry",
				AccessibleDescription = "Relays received UDP telemetry datagrams to Forward IP and Port (for example, SimHub on 127.0.0.1 or a dashboard on another PC/tablet).",
				AutoSize = true,
				Anchor = AnchorStyles.Left,
				UseVisualStyleBackColor = true,
			};
			ForwardEnabledCheckBox.CheckedChanged += (s, e) =>
			{
				if (_syncing) return;
				_host.ForwardEnabled = ForwardEnabledCheckBox.Checked;
				UpdateControlStates();
				RefreshStatus();
			};

			var forwardIpLabel = new Label
			{
				Name = "ForwardIpLabel",
				Text = "Forward IP",
				AutoSize = true,
				Anchor = AnchorStyles.Left,
			};
			ForwardIpTextBox = new TextBox
			{
				Name = nameof(ForwardIpTextBox),
				AccessibleName = "Forward IP address",
				AccessibleDescription = "Destination IP address to relay received UDP telemetry to (127.0.0.1 for local apps like SimHub, or a LAN PC/tablet IP).",
				Anchor = AnchorStyles.Left | AnchorStyles.Right,
				Text = "127.0.0.1",
			};
			ForwardIpTextBox.TextChanged += (s, e) =>
			{
				if (_syncing) return;
				_host.ForwardIp = ForwardIpTextBox.Text;
				RefreshStatus();
			};

			var forwardPortLabel = new Label
			{
				Name = "ForwardPortLabel",
				Text = "Port",
				AutoSize = true,
				Anchor = AnchorStyles.Left,
			};
			ForwardPortUpDown = new NumericUpDown
			{
				Name = nameof(ForwardPortUpDown),
				AccessibleName = "Forward UDP port",
				AccessibleDescription = "Destination UDP port to relay received telemetry packets to (0 = forwarding off).",
				Minimum = 0,
				Maximum = 65535,
				Value = 0,
				Anchor = AnchorStyles.Left,
			};
			ForwardPortUpDown.ValueChanged += (s, e) =>
			{
				if (_syncing) return;
				_host.ForwardPort = (int)ForwardPortUpDown.Value;
				RefreshStatus();
			};

			DefaultsButton = new Button
			{
				Name = nameof(DefaultsButton),
				Text = "Defaults",
				AccessibleName = "Reset telemetry defaults",
				AccessibleDescription = "Restores telemetry ports and force tuning sliders to their default values.",
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Anchor = AnchorStyles.Right,
				UseVisualStyleBackColor = true,
			};
			DefaultsButton.Click += (s, e) =>
			{
				_host.ResetDefaults();
				SyncFromHost();
				RefreshStatus();
			};

			portsTable.Controls.Add(listenLabel, 0, 2);
			portsTable.Controls.Add(ListenPortUpDown, 1, 2);
			portsTable.Controls.Add(DefaultsButton, 3, 2);
			portsTable.Controls.Add(ForwardEnabledCheckBox, 0, 3);
			portsTable.SetColumnSpan(ForwardEnabledCheckBox, 4);
			portsTable.Controls.Add(forwardIpLabel, 0, 4);
			portsTable.Controls.Add(ForwardIpTextBox, 1, 4);
			portsTable.Controls.Add(forwardPortLabel, 2, 4);
			portsTable.Controls.Add(ForwardPortUpDown, 3, 4);
			portsGroup.Controls.Add(portsTable);

			_spacing.Add(g =>
			{
				var itemMargin = new Padding(g, g, 0, 0);
				portsGroup.Margin = itemMargin;
				EnabledCheckBox.Margin = itemMargin;
				StandardPortsCheckBox.Margin = itemMargin;
				listenLabel.Margin = itemMargin;
				ListenPortUpDown.Margin = itemMargin;
				ListenPortUpDown.Width = g * 10;
				DefaultsButton.Margin = itemMargin;
				ForwardEnabledCheckBox.Margin = itemMargin;
				forwardIpLabel.Margin = itemMargin;
				ForwardIpTextBox.Margin = itemMargin;
				ForwardIpTextBox.MinimumSize = new Size(g * 12, 0);
				forwardPortLabel.Margin = itemMargin;
				ForwardPortUpDown.Margin = itemMargin;
				ForwardPortUpDown.Width = g * 10;
			});

			// Left bottom: Force Feel Tuning group (matching Wheel & Force Feedback slider rows).
			var tuningGroup = new GroupBox
			{
				Name = "TuningGroupBox",
				Text = "Force Feel Tuning",
				AccessibleName = "Force Feel Tuning",
				AccessibleDescription = "Adjusts how strongly live telemetry drives counter-steer aligning torque, understeer lightening, curb bumps, damping, rumble understeer, steering lead, and where road and impact feel comes from.",
				Dock = DockStyle.Fill,
				MinimumSize = new Size(120, 64),
			};
			var tuningTable = new TableLayoutPanel
			{
				Name = "TuningTableLayoutPanel",
				Dock = DockStyle.Fill,
				ColumnCount = 3,
				RowCount = 9,
				Margin = Padding.Empty,
				Padding = Padding.Empty,
			};
			tuningTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			tuningTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			tuningTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			for (var i = 0; i < 8; i++)
				tuningTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			tuningTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

			AlignStrengthTrackBar = AddTuningRow(tuningTable, 0, "AlignStrength", "Aligning torque",
				"Scales self-aligning torque and counter-steer pull from live tire slip and lateral G (0 to 200 %).",
				0, 200, 100, 5, 25, " %", out AlignStrengthValueTextBox);
			AlignStrengthTrackBar.ValueChanged += (s, e) =>
			{
				AlignStrengthValueTextBox.Text = AlignStrengthTrackBar.Value + " %";
				if (!_syncing) _host.AlignStrength = AlignStrengthTrackBar.Value;
			};

			UndersteerStrengthTrackBar = AddTuningRow(tuningTable, 1, "UndersteerStrength", "Understeer",
				"Scales how much the wheel goes light when the front tires lose grip (0 to 100 %). Friction floor is always preserved.",
				0, 100, 100, 5, 20, " %", out UndersteerStrengthValueTextBox);
			UndersteerStrengthTrackBar.ValueChanged += (s, e) =>
			{
				UndersteerStrengthValueTextBox.Text = UndersteerStrengthTrackBar.Value + " %";
				if (!_syncing) _host.UndersteerStrength = UndersteerStrengthTrackBar.Value;
			};

			CurbStrengthTrackBar = AddTuningRow(tuningTable, 2, "CurbStrength", "Curb bumps",
				"Scales directional curb strikes and suspension bump kicks from live telemetry (0 to 100 %).",
				0, 100, 100, 5, 20, " %", out CurbStrengthValueTextBox);
			CurbStrengthTrackBar.ValueChanged += (s, e) =>
			{
				CurbStrengthValueTextBox.Text = CurbStrengthTrackBar.Value + " %";
				if (!_syncing) _host.CurbStrength = CurbStrengthTrackBar.Value;
			};

			DampingStrengthTrackBar = AddTuningRow(tuningTable, 3, "DampingStrength", "Dynamic damping",
				"Scales surface and speed-driven wheel damping changes from telemetry (0 to 200 %).",
				0, 200, 100, 5, 25, " %", out DampingStrengthValueTextBox);
			DampingStrengthTrackBar.ValueChanged += (s, e) =>
			{
				DampingStrengthValueTextBox.Text = DampingStrengthTrackBar.Value + " %";
				if (!_syncing) _host.DampingStrength = DampingStrengthTrackBar.Value;
			};

			RumbleUndersteerTrackBar = AddTuningRow(tuningTable, 4, "RumbleUndersteer", "Rumble understeer",
				"Fallback when a game sends no telemetry: lightens the centering spring during sustained rumble while turning (0 to 60 %).",
				0, 60, 0, 5, 15, " %", out RumbleUndersteerValueTextBox);
			RumbleUndersteerTrackBar.ValueChanged += (s, e) =>
			{
				RumbleUndersteerValueTextBox.Text = RumbleUndersteerTrackBar.Value + " %";
				if (!_syncing) _host.RumbleUndersteer = RumbleUndersteerTrackBar.Value;
			};

			SteeringLeadTrackBar = AddTuningRow(tuningTable, 5, "SteeringLead", "Steering lead",
				"Steering velocity feed-forward in milliseconds (0 to 80 ms) to counteract gamepad steering filter lag in console ports.",
				0, 80, 0, 5, 15, " ms", out SteeringLeadValueTextBox);
			SteeringLeadTrackBar.ValueChanged += (s, e) =>
			{
				SteeringLeadValueTextBox.Text = SteeringLeadTrackBar.Value + " ms";
				if (!_syncing) _host.SteeringLeadMs = SteeringLeadTrackBar.Value;
			};

			var rumbleSourceDescription = "Where the wheel's road and impact feel comes from while live telemetry drives the centering spring: "
				+ "the game's rumble as sent, the game's rumble without its gear change kicks, hits felt from telemetry alone, or both. "
				+ "Telemetry is recommended for a wheel; the game's rumble plays unchanged while no telemetry arrives.";
			var rumbleSourceLabel = new Label
			{
				Name = "RumbleSourceLabel",
				Text = "Road and impact feel from",
				AutoSize = true,
				Anchor = AnchorStyles.Left,
			};
			RumbleSourceComboBox = new ComboBox
			{
				Name = nameof(RumbleSourceComboBox),
				AccessibleName = "Road and impact feel from",
				AccessibleDescription = rumbleSourceDescription,
				DropDownStyle = ComboBoxStyle.DropDownList,
				Anchor = AnchorStyles.Left | AnchorStyles.Right,
			};
			// In the order of WheelRumbleSource's values.
			RumbleSourceComboBox.Items.AddRange(new object[] { "Game", "Game without gear changes", "Telemetry", "Both" });
			RumbleSourceComboBox.SelectedIndex = (int)WheelRumbleSource.Telemetry;
			RumbleSourceComboBox.SelectedIndexChanged += (s, e) =>
			{
				if (!_syncing && RumbleSourceComboBox.SelectedIndex >= 0)
					_host.RumbleSource = (WheelRumbleSource)RumbleSourceComboBox.SelectedIndex;
			};
			_spacing.Add(g =>
			{
				var itemMargin = new Padding(g, g, 0, 0);
				rumbleSourceLabel.Margin = itemMargin;
				RumbleSourceComboBox.Margin = itemMargin;
			});
			_toolTip.SetToolTip(rumbleSourceLabel, rumbleSourceDescription);
			_toolTip.SetToolTip(RumbleSourceComboBox, rumbleSourceDescription);
			tuningTable.Controls.Add(rumbleSourceLabel, 0, 6);
			tuningTable.Controls.Add(RumbleSourceComboBox, 1, 6);
			tuningTable.SetColumnSpan(RumbleSourceComboBox, 2);

			GearChangeRumbleTrackBar = AddTuningRow(tuningTable, 7, "GearChangeRumble", "Gear change",
				"How much of the game's rumble plays for a moment while the gear changes, with live telemetry on (0 to 100 %).",
				0, 100, 0, 5, 20, " %", out GearChangeRumbleValueTextBox);
			GearChangeRumbleTrackBar.ValueChanged += (s, e) =>
			{
				GearChangeRumbleValueTextBox.Text = GearChangeRumbleTrackBar.Value + " %";
				if (!_syncing) _host.GearChangeRumble = GearChangeRumbleTrackBar.Value;
			};

			tuningGroup.Controls.Add(tuningTable);

			_spacing.Add(g =>
			{
				var itemMargin = new Padding(g, g, 0, 0);
				tuningGroup.Margin = itemMargin;
			});

			// Right top: Live Telemetry GroupBox (replaces custom border/color panel with native themed GroupBox + ProgressBars).
			ReadingsGroupBox = new GroupBox
			{
				Name = nameof(ReadingsGroupBox),
				Text = "Live Telemetry",
				AccessibleName = "Live telemetry",
				AccessibleDescription = "Connection state and newest vehicle physics values received from the running game.",
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Dock = DockStyle.Fill,
			};

			ReadingsPanel = new TableLayoutPanel
			{
				Name = nameof(ReadingsPanel),
				Dock = DockStyle.Fill,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				ColumnCount = 3,
				RowCount = 8,
				Margin = Padding.Empty,
				Padding = Padding.Empty,
			};
			ReadingsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			ReadingsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			ReadingsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			for (var i = 0; i < 8; i++)
				ReadingsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

			StatusLabel = new Label
			{
				Name = nameof(StatusLabel),
				AccessibleName = "Telemetry connection status",
				AccessibleDescription = "Current telemetry state and detected game or protocol.",
				AutoSize = true,
				Anchor = AnchorStyles.Left | AnchorStyles.Right,
				Text = "Waiting for game telemetry...",
			};
			PortStatusLabel = new Label
			{
				Name = nameof(PortStatusLabel),
				AccessibleName = "Listener ports",
				AccessibleDescription = "Currently bound UDP ports and any skipped port conflicts.",
				AutoSize = true,
				Anchor = AnchorStyles.Left | AnchorStyles.Right,
				ForeColor = SystemColors.GrayText,
				Text = "Shared Memory + UDP 37150",
			};
			ReadingsPanel.Controls.Add(StatusLabel, 0, 0);
			ReadingsPanel.SetColumnSpan(StatusLabel, 3);
			ReadingsPanel.Controls.Add(PortStatusLabel, 0, 1);
			ReadingsPanel.SetColumnSpan(PortStatusLabel, 3);

			SourceValueLabel = StatusLabel;
			RateValueLabel = PortStatusLabel;
			FreshnessValueLabel = StatusLabel;
			SpeedValueLabel = AddReadingRow(ReadingsPanel, 2, "Speed", "Vehicle speed in km/h.", true, out SpeedBar);
			AlignValueLabel = AddReadingRow(ReadingsPanel, 3, "Aligning angle", "Self-aligning / counter-steer equilibrium shift in degrees.", true, out AlignBar);
			FrontGripValueLabel = AddReadingRow(ReadingsPanel, 4, "Front grip", "Estimated front-tire grip (100 % full grip, lower = understeer).", true, out FrontGripBar);
			LateralGValueLabel = AddReadingRow(ReadingsPanel, 5, "Lateral G", "Sideways cornering acceleration in G.", true, out LateralGBar);
			PitchValueLabel = AddReadingRow(ReadingsPanel, 6, "Road pitch", "Vehicle pitch angle in degrees and road grade in percent (nose up positive).", true, out PitchBar);
			RpmValueLabel = AddReadingRow(ReadingsPanel, 7, "Engine RPM", "Normalized engine RPM for steering wheel RGB/RPM shift LEDs.", true, out RpmBar);

			ReadingsGroupBox.Controls.Add(ReadingsPanel);
			_spacing.Add(g =>
			{
				var itemMargin = new Padding(g, g, 0, 0);
				ReadingsGroupBox.Margin = itemMargin;
				StatusLabel.Margin = itemMargin;
				PortStatusLabel.Margin = itemMargin;
			});

			// Right bottom: Activity Log GroupBox.
			var logGroup = new GroupBox
			{
				Name = "ActivityGroupBox",
				Text = "Activity",
				AccessibleName = "Telemetry activity",
				AccessibleDescription = "Recent telemetry port binds, protocol switches and setup hints, newest first.",
				Dock = DockStyle.Fill,
				MinimumSize = new Size(120, 64),
			};
			var logTable = new TableLayoutPanel
			{
				Name = "ActivityTableLayoutPanel",
				Dock = DockStyle.Fill,
				ColumnCount = 1,
				RowCount = 1,
				Margin = Padding.Empty,
				Padding = Padding.Empty,
			};
			logTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			logTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			LogTextBox = new TextBox
			{
				Name = nameof(LogTextBox),
				AccessibleName = "Telemetry activity log",
				AccessibleDescription = "Recent telemetry port binds, protocol switches and setup hints, newest first.",
				Dock = DockStyle.Fill,
				Multiline = true,
				ReadOnly = true,
				WordWrap = true,
				ScrollBars = ScrollBars.Vertical,
				BackColor = SystemColors.Window,
			};
			logTable.Controls.Add(LogTextBox, 0, 0);
			logGroup.Controls.Add(logTable);
			_spacing.Add(g =>
			{
				var itemMargin = new Padding(g, g, 0, 0);
				logGroup.Margin = itemMargin;
				LogTextBox.Margin = itemMargin;
			});

			leftCol.Controls.Add(portsGroup, 0, 0);
			leftCol.Controls.Add(tuningGroup, 0, 1);
			rightCol.Controls.Add(ReadingsGroupBox, 0, 0);
			rightCol.Controls.Add(logGroup, 0, 1);
			main.Controls.Add(leftCol, 0, 0);
			main.Controls.Add(rightCol, 1, 0);
			telemetryTab.Controls.Add(main);

			// Help sub-tab (identical structure to PluginDeviceUserControl's Help page).
			var helpTab = new TabPage
			{
				Name = "HelpTabPage",
				Text = "Help",
				AccessibleName = "Help",
				AccessibleDescription = "Supported games, standard telemetry protocols, port coexistence and testing instructions.",
				BackColor = SystemColors.Control,
			};
			HelpRichTextBox = new RichTextBox
			{
				Name = nameof(HelpRichTextBox),
				AccessibleName = "Game Telemetry help",
				AccessibleDescription = "Supported racing games, UDP ports, SimHub coexistence and Virtual Car testing guide.",
				Dock = DockStyle.Fill,
				ReadOnly = true,
				BorderStyle = BorderStyle.None,
				BackColor = SystemColors.Window,
				Text = BuildHelpMarkdown(),
			};
			helpTab.Controls.Add(HelpRichTextBox);

			PageTabControl.TabPages.Add(telemetryTab);
			PageTabControl.TabPages.Add(helpTab);
			Controls.Add(PageTabControl);

			_toolTip.SetToolTip(EnabledCheckBox, EnabledCheckBox.AccessibleDescription);
			_toolTip.SetToolTip(StandardPortsCheckBox, StandardPortsCheckBox.AccessibleDescription);
			_toolTip.SetToolTip(ListenPortUpDown, ListenPortUpDown.AccessibleDescription);
			_toolTip.SetToolTip(ForwardEnabledCheckBox, ForwardEnabledCheckBox.AccessibleDescription);
			_toolTip.SetToolTip(ForwardIpTextBox, ForwardIpTextBox.AccessibleDescription);
			_toolTip.SetToolTip(ForwardPortUpDown, ForwardPortUpDown.AccessibleDescription);

			ApplySpacing();
			_host.SettingsChanged += Host_SettingsChanged;

			ResumeLayout(false);
			PerformLayout();

			AppendLog("Ready. Send standard UDP telemetry to 127.0.0.1:" + _host.Port + ", launch Assetto Corsa / iRacing, or use x360ce Test Game -> Virtual Car.");
			SyncFromHost();
			RefreshStatus();
		}

		/// <summary>Computes x360ce's one gap (8 px at 100 % DPI, scaled cleanly on the 4 px grid) without forcing premature handle creation.</summary>
		static int ScaledGap()
		{
			var dpiScale = 1.0;
			try
			{
				using (var g = Graphics.FromHwnd(IntPtr.Zero))
					dpiScale = g.DpiX / 96.0;
			}
			catch
			{
				dpiScale = 1.0;
			}
			// 8 px at 100 %, 12 px at 150 %, 16 px at 200 % — strictly matching SpacingGridTest's Gap * DpiScale.
			return Math.Max(4, (int)Math.Round(8.0 * dpiScale));
		}

		bool _applyingSpacing;

		void ApplySpacing()
		{
			if (_applyingSpacing)
				return;
			_applyingSpacing = true;
			try
			{
				if (_boldFont == null || _boldFont.Name != Font.Name || Math.Abs(_boldFont.Size - Font.Size) > 0.01f)
				{
					var previous = _boldFont;
					_boldFont = new Font(Font, FontStyle.Bold);
					StatusLabel.Font = _boldFont;
					if (previous != null)
						previous.Dispose();
				}
				var gap = ScaledGap();
				for (var i = 0; i < _spacing.Count; i++)
					_spacing[i](gap);
			}
			finally
			{
				_applyingSpacing = false;
			}
		}

		protected override void OnFontChanged(EventArgs e)
		{
			base.OnFontChanged(e);
			if (StatusLabel != null)
				ApplySpacing();
		}

		TrackBar AddTuningRow(TableLayoutPanel table, int row, string key, string labelText, string description,
			int min, int max, int def, int small, int large, string unit, out TextBox valueBox)
		{
			var caption = new Label
			{
				Name = key + "Label",
				Text = labelText,
				AutoSize = true,
				Anchor = AnchorStyles.Left,
			};
			var bar = new TrackBar
			{
				Name = key + "TrackBar",
				AccessibleName = labelText,
				AccessibleDescription = description,
				Minimum = min,
				Maximum = max,
				Value = def,
				SmallChange = small,
				LargeChange = large,
				TickStyle = TickStyle.None,
				AutoSize = false,
				Anchor = AnchorStyles.Left | AnchorStyles.Right,
			};
			var val = new TextBox
			{
				Name = key + "TextBox",
				AccessibleName = labelText + " value",
				AccessibleDescription = description,
				ReadOnly = true,
				TabStop = false,
				TextAlign = HorizontalAlignment.Right,
				Anchor = AnchorStyles.Left,
				Text = def + unit,
			};
			valueBox = val;
			_spacing.Add(g =>
			{
				var itemMargin = new Padding(g, g, 0, 0);
				caption.Margin = itemMargin;
				bar.Margin = itemMargin;
				bar.Height = Math.Max(18, g * 2 + 4);
				val.Margin = itemMargin;
				val.Width = g * 7;
			});
			_toolTip.SetToolTip(caption, description);
			_toolTip.SetToolTip(bar, description);
			table.Controls.Add(caption, 0, row);
			table.Controls.Add(bar, 1, row);
			table.Controls.Add(val, 2, row);
			return bar;
		}

		Label AddReadingRow(TableLayoutPanel table, int row, string name, string description, bool withBar, out ProgressBar bar)
		{
			var key = name.Replace(" ", "");
			var caption = new Label
			{
				Name = key + "CaptionLabel",
				Text = name,
				AutoSize = true,
				Anchor = AnchorStyles.Left,
				ForeColor = SystemColors.GrayText,
			};
			var value = new Label
			{
				Name = key + "ValueLabel",
				AccessibleName = name,
				AccessibleDescription = description,
				AutoSize = true,
				Anchor = AnchorStyles.Left,
				Text = "-",
			};
			table.Controls.Add(caption, 0, row);
			table.Controls.Add(value, 1, row);
			ProgressBar meter = null;
			if (withBar)
			{
				meter = new ProgressBar
				{
					Name = key + "Bar",
					AccessibleName = name + " meter",
					AccessibleDescription = description,
					Minimum = 0,
					Maximum = 100,
					Value = 0,
					Style = ProgressBarStyle.Continuous,
					Anchor = AnchorStyles.Left | AnchorStyles.Right,
				};
				table.Controls.Add(meter, 2, row);
			}
			else
			{
				table.SetColumnSpan(value, 2);
			}
			bar = meter;
			_spacing.Add(g =>
			{
				var itemMargin = new Padding(g, g, 0, 0);
				caption.Margin = itemMargin;
				value.Margin = itemMargin;
				if (meter != null)
				{
					value.MinimumSize = new Size(g * 10, 0);
					meter.Height = g * 2;
					meter.Margin = itemMargin;
				}
			});
			return value;
		}

		void Host_SettingsChanged(object sender, EventArgs e)
		{
			if (IsDisposed)
				return;
			if (InvokeRequired)
			{
				if (IsHandleCreated)
					BeginInvoke((Action)SyncAndRefresh);
				return;
			}
			SyncAndRefresh();
		}

		void SyncAndRefresh()
		{
			SyncFromHost();
			RefreshStatus();
		}

		void SyncFromHost()
		{
			_syncing = true;
			try
			{
				EnabledCheckBox.Checked = _host.Enabled;
				StandardPortsCheckBox.Checked = _host.ListenStandardPorts;
				ListenPortUpDown.Value = ClampInt(_host.Port, 1024, 65535);
				ForwardEnabledCheckBox.Checked = _host.ForwardEnabled;
				var ipText = string.IsNullOrWhiteSpace(_host.ForwardIp) ? "127.0.0.1" : _host.ForwardIp;
				if (ForwardIpTextBox.Text != ipText)
					ForwardIpTextBox.Text = ipText;
				ForwardPortUpDown.Value = ClampInt(_host.ForwardPort, 0, 65535);
				AlignStrengthTrackBar.Value = ClampInt(_host.AlignStrength, 0, 200);
				AlignStrengthValueTextBox.Text = AlignStrengthTrackBar.Value + " %";
				UndersteerStrengthTrackBar.Value = ClampInt(_host.UndersteerStrength, 0, 100);
				UndersteerStrengthValueTextBox.Text = UndersteerStrengthTrackBar.Value + " %";
				CurbStrengthTrackBar.Value = ClampInt(_host.CurbStrength, 0, 100);
				CurbStrengthValueTextBox.Text = CurbStrengthTrackBar.Value + " %";
				DampingStrengthTrackBar.Value = ClampInt(_host.DampingStrength, 0, 200);
				DampingStrengthValueTextBox.Text = DampingStrengthTrackBar.Value + " %";
				RumbleUndersteerTrackBar.Value = ClampInt(_host.RumbleUndersteer, 0, 60);
				RumbleUndersteerValueTextBox.Text = RumbleUndersteerTrackBar.Value + " %";
				SteeringLeadTrackBar.Value = ClampInt(_host.SteeringLeadMs, 0, 80);
				SteeringLeadValueTextBox.Text = SteeringLeadTrackBar.Value + " ms";
				var rumbleSource = (int)_host.RumbleSource;
				RumbleSourceComboBox.SelectedIndex = rumbleSource >= 0 && rumbleSource < RumbleSourceComboBox.Items.Count
					? rumbleSource
					: (int)WheelRumbleSource.Telemetry;
				GearChangeRumbleTrackBar.Value = ClampInt(_host.GearChangeRumble, 0, 100);
				GearChangeRumbleValueTextBox.Text = GearChangeRumbleTrackBar.Value + " %";
				UpdateControlStates();
			}
			finally
			{
				_syncing = false;
			}
		}

		void UpdateControlStates()
		{
			var on = EnabledCheckBox.Checked;
			var fwd = on && ForwardEnabledCheckBox.Checked;
			StandardPortsCheckBox.Enabled = on;
			ListenPortUpDown.Enabled = on;
			ForwardEnabledCheckBox.Enabled = on;
			ForwardIpTextBox.Enabled = fwd;
			ForwardPortUpDown.Enabled = fwd;
		}

		public void RefreshStatus()
		{
			if (IsDisposed)
				return;
			var enabled = _host.Enabled;
			var live = _host.LiveTelemetry;
			var freshness = enabled ? _host.Freshness : 0.0;
			var source = enabled ? (_host.Source ?? live.Source) : null;
			var ports = _host.PortStatus;
			var hz = enabled ? _host.PacketRateHz : 0;

			if (_lastLoggedEnabled != enabled)
			{
				_lastLoggedEnabled = enabled;
				AppendLog(enabled ? "Telemetry enabled." : "Telemetry turned off - listeners closed.");
			}
			if (enabled && ports != _lastLoggedPortStatus && !string.IsNullOrEmpty(ports))
			{
				_lastLoggedPortStatus = ports;
				AppendLog("Listeners: " + ports);
			}
			if (enabled && freshness > 0.0 && !string.IsNullOrEmpty(source) && source != _lastLoggedSource)
			{
				_lastLoggedSource = source;
				AppendLog("Receiving live telemetry: " + source + ".");
			}
			else if (freshness <= 0.0 && _lastLoggedSource != null)
			{
				AppendLog("Telemetry stream idle (" + _lastLoggedSource + ").");
				_lastLoggedSource = null;
			}

			var header = !enabled
				? "Telemetry off"
				: (freshness > 0.0 && !string.IsNullOrEmpty(source)
					? string.Format(CultureInfo.InvariantCulture, "{0} - {1} km/h{2}",
						source,
						float.IsNaN(live.SpeedKmh) ? (_host.SpeedKmh ?? 0) : (int)Math.Round(live.SpeedKmh),
						hz > 0 ? " (" + hz + " Hz)" : "")
					: "Waiting for game telemetry...");
			if (StatusLabel.Text != header)
				StatusLabel.Text = header;

			var portLine = !enabled
				? "Sockets closed while telemetry is off."
				: (string.IsNullOrEmpty(ports) ? "Shared Memory + UDP " + _host.Port : ports);
			if (PortStatusLabel.Text != portLine)
				PortStatusLabel.Text = portLine;

			if (enabled && freshness > 0.0)
			{
				var speed = float.IsNaN(live.SpeedKmh) ? (_host.SpeedKmh ?? 0f) : live.SpeedKmh;
				SetText(SpeedValueLabel, string.Format(CultureInfo.InvariantCulture, "{0:0.0} km/h", speed));
				SetBar(SpeedBar, speed / 250f);

				SetText(AlignValueLabel, string.Format(CultureInfo.InvariantCulture, "{0:+0.0;-0.0;0.0}°", live.AlignAngleDeg));
				SetBar(AlignBar, Math.Abs(live.AlignAngleDeg) / 45f);

				var grip = ClampFloat(live.FrontGrip, 0f, 1f);
				SetText(FrontGripValueLabel, string.Format(CultureInfo.InvariantCulture, "{0:0} %", grip * 100f));
				SetBar(FrontGripBar, grip);

				SetText(LateralGValueLabel, string.Format(CultureInfo.InvariantCulture, "{0:+0.00;-0.00;0.00} G", live.LateralG));
				SetBar(LateralGBar, Math.Abs(live.LateralG) / 2.5f);

				if (!float.IsNaN(live.PitchDeg) && !float.IsInfinity(live.PitchDeg))
				{
					var grade = Math.Tan(live.PitchDeg * Math.PI / 180.0) * 100.0;
					SetText(PitchValueLabel, string.Format(CultureInfo.InvariantCulture, "{0:+0.0;-0.0;0.0}° ({1:+0.0;-0.0;0.0}%)", live.PitchDeg, grade));
					SetBar(PitchBar, (float)((grade + 20.0) / 40.0));
				}
				else
				{
					SetText(PitchValueLabel, "-");
					SetBar(PitchBar, 0f);
				}

				if (!float.IsNaN(live.RpmFraction) && !float.IsInfinity(live.RpmFraction))
				{
					var rpm = ClampFloat(live.RpmFraction, 0f, 1f);
					SetText(RpmValueLabel, string.Format(CultureInfo.InvariantCulture, "{0:0} %", rpm * 100f));
					SetBar(RpmBar, rpm);
				}
				else
				{
					SetText(RpmValueLabel, "-");
					SetBar(RpmBar, 0f);
				}
			}
			else
			{
				SetText(SpeedValueLabel, "-");
				SetBar(SpeedBar, 0f);
				SetText(AlignValueLabel, "-");
				SetBar(AlignBar, 0f);
				SetText(FrontGripValueLabel, "-");
				SetBar(FrontGripBar, 0f);
				SetText(LateralGValueLabel, "-");
				SetBar(LateralGBar, 0f);
				SetText(PitchValueLabel, "-");
				SetBar(PitchBar, 0f);
				SetText(RpmValueLabel, "-");
				SetBar(RpmBar, 0f);
			}
		}

		static void SetBar(ProgressBar bar, float fraction)
		{
			var v = ClampInt((int)Math.Round(ClampFloat(fraction, 0f, 1f) * 100f), 0, 100);
			if (bar.Value != v)
				bar.Value = v;
		}

		void AppendLog(string message)
		{
			var line = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "  " + message;
			_logEntries.Insert(0, line);
			if (_logEntries.Count > 40)
				_logEntries.RemoveAt(_logEntries.Count - 1);
			LogTextBox.Text = string.Join(Environment.NewLine, _logEntries);
		}

		static void SetText(Label label, string text)
		{
			if (label.Text != text)
				label.Text = text;
		}

		static int ClampInt(int v, int min, int max)
		{
			return v < min ? min : v > max ? max : v;
		}

		static float ClampFloat(float v, float min, float max)
		{
			if (float.IsNaN(v) || float.IsInfinity(v)) return 0f;
			return v < min ? min : v > max ? max : v;
		}

		static string BuildHelpMarkdown()
		{
			return "# Game Telemetry\n\n"
				+ "Reads live vehicle physics from running games using popular existing telemetry standards and feeds steering wheels (speed-sensitive centering, counter-steer, understeer, curb kicks, RGB shift LEDs) and telemetry-consuming devices (such as indoor bike road grade).\n\n"
				+ "## Shared Memory (automatic, zero port conflicts)\n\n"
				+ "- **Assetto Corsa, Assetto Corsa Competizione, Assetto Corsa EVO** (`Local\\acpmf_physics`)\n"
				+ "- **iRacing** (`Local\\IRSDKMemMapFileName`)\n\n"
				+ "## Standard UDP Formats (auto-detected on UDP port 37150)\n\n"
				+ "- **Forza Data Out** (Forza Motorsport / Horizon 4 & 5 - 232, 311, 323 or 331 bytes)\n"
				+ "- **EA F1 Series** (F1 2020 to F1 2026+ Motion & Car Telemetry packets)\n"
				+ "- **Codemasters UDP** (DiRT Rally 1/2, EA WRC, Grid - 264 bytes)\n"
				+ "- **OutSim** (Live for Speed, BeamNG.drive - 64 or 68 bytes)\n"
				+ "- **Project CARS 1 / 2 & Automobilista 2 UDP**\n\n"
				+ "## Coexisting with SimHub, CrewChief & Wheel Dashboards\n\n"
				+ "- By default x360ce listens **only** on `127.0.0.1:37150` so other telemetry apps keep standard ports (`5300`, `20777`, `4123`, `5606`).\n"
				+ "- Either forward telemetry from SimHub to `127.0.0.1:37150`, or point the game at `127.0.0.1:37150`, tick **Enable forward telemetry**, and set **Forward IP** and **Port** here to relay datagrams to SimHub or another PC/tablet.\n"
				+ "- Tick **Also listen on standard game ports** only when no other app uses them. Occupied ports are always skipped safely.\n\n"
				+ "## Testing with x360ce Test Game\n\n"
				+ "- Launch `x360ce.TestGame.exe` and switch to the **Virtual Car** tab.\n"
				+ "- Drive with your wheel, gamepad, keyboard or Remote MCP Controller (or tick **Auto-Drive**) and adjust **Road grade %** to test hills.\n"
				+ "- Tick **Send telemetry** (`127.0.0.1:37150`) to see live telemetry readings and feel the wheel respond immediately.\n\n"
				+ "## Disabling the Plugin\n\n"
				+ "- Untick **WheelTelemetry** on **Options > Plugins** to unload this tab and release all listeners.";
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_host.SettingsChanged -= Host_SettingsChanged;
				_toolTip.Dispose();
				if (_boldFont != null)
				{
					_boldFont.Dispose();
					_boldFont = null;
				}
			}
			base.Dispose(disposing);
		}
	}
}
