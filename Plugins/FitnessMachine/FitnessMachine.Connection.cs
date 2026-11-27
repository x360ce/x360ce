using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace x360ce.Plugins.Samples
{
	/// <summary>
	/// A Fitness Machine device's Connection page, the first page of its tab on the controller page: finds fitness machines
	/// over Bluetooth, chooses the one the device connects to, shows what it sends and sends it commands, as the Virtual
	/// Fitness Machine Receiver does.
	/// </summary>
	/// <remarks>
	/// x360ce makes it with the device it reads or, while the device is unticked, with an instance of the tab's own, which
	/// it never reads. Selecting a machine in the list chooses it for the device (<see cref="FitnessMachine.Choose"/>), and
	/// x360ce keeps it as the device's Machine setting. Selecting does not connect: the device connects while it is ticked.
	///
	/// Machines and commands on the left; the status, the values and the activity on the right. The two columns share their
	/// rows, so the list of machines and the box of values start and end level, and the log is as tall as the commands
	/// beside it. The left column is as wide as what is in it, at least a width in the font's units, and the right one
	/// takes the rest of the page, its status wrapping at its width. Drawn in the system colours, so x360ce's theme applies,
	/// and laid out by tables and automatic sizes, with the room between controls taken from the font, so it follows the
	/// font and the zoom.
	///
	/// Every control is named, and the AI door reaches each: it reads the machines as the grid's rows and chooses one by
	/// setting the grid to its index, and it reads each value by its name in the box of values. The page reads the device,
	/// its session and the list of machines heard four times a second, and draws only what changed.
	/// </remarks>
	public class FitnessConnection : UserControl
	{
		/// <summary>The page of a device, listing the machines the process hears, as x360ce makes it.</summary>
		public FitnessConnection(FitnessMachine device) : this(device, FitnessScan.Shared)
		{
		}

		/// <summary>The page of a device, listing the machines of the scan given, as a test makes it.</summary>
		internal FitnessConnection(FitnessMachine device, FitnessScan scan)
		{
			_device = device;
			_session = device.Session;
			_scan = scan;
			Name = "FitnessConnection";
			// The page's name on the device's tab.
			AccessibleName = "Connection";
			AccessibleDescription = "Finds fitness machines over Bluetooth, chooses the one this device connects to while it is ticked, and shows what it sends.";
			SuspendLayout();
			Build();
			ApplyTexts();
			ResumeLayout(false);
			PerformLayout();
			// A machine that asks to be paired is asked about on this thread.
			_ui = SynchronizationContext.Current;
			_ask = AskPairing;
			_session.AskPerson = _ask;
			_timer = new System.Windows.Forms.Timer { Interval = 250 };
			_timer.Tick += (s, e) => ShowSession();
			_timer.Start();
			ShowSession();
			PluginText.Changed += LanguageChanged;
		}

		readonly FitnessMachine _device;
		readonly FitnessSession _session;
		readonly FitnessScan _scan;
		readonly SynchronizationContext _ui;
		readonly Action<FitnessPairing> _ask;
		readonly System.Windows.Forms.Timer _timer;

		/// <summary>What the page showed last: the session's version, the scan's and the Machine setting; drawn again only when one changed.</summary>
		int _shownVersion = -1;
		int _shownScan = -1;
		string _shownMachine;
		bool _shownReading;

		/// <summary>The row of the machine chosen while the scan has not heard it, kept so the list is not made again on each look.</summary>
		FitnessSeen _chosenRow;

		#region Controls

		internal TableLayoutPanel MainTableLayoutPanel;
		internal Label NoteLabel;
		internal FlowLayoutPanel ScanFlowLayoutPanel;
		internal Button ScanButton;
		internal CheckBox ShowAllCheckBox;
		internal DataGridView MachinesDataGridView;
		internal DataGridViewTextBoxColumn NameColumn;
		internal DataGridViewTextBoxColumn TypeColumn;
		internal DataGridViewTextBoxColumn SignalColumn;
		internal FlowLayoutPanel CommandsFlowLayoutPanel;
		internal Button StartButton;
		internal Button PauseButton;
		internal Button StopButton;
		internal Button ResetButton;
		internal TableLayoutPanel TargetsTableLayoutPanel;
		internal TrackBar PowerTrackBar;
		internal Label PowerValueLabel;
		internal Button SetPowerButton;
		internal TrackBar ResistanceTrackBar;
		internal Label ResistanceValueLabel;
		internal Button SetResistanceButton;
		internal TrackBar GradeTrackBar;
		internal Label GradeValueLabel;
		internal Button SetGradeButton;
		internal TableLayoutPanel StatusTableLayoutPanel;
		internal Label StatusLabel;
		internal Label FeaturesLabel;
		internal Panel ReadingsPanel;
		internal TableLayoutPanel ReadingsTableLayoutPanel;
		internal TextBox LogTextBox;

		/// <summary>Gives each control its room from the font: run again when the font changes.</summary>
		readonly List<Action<int>> _spacing = new List<Action<int>>();

		/// <summary>Applies the translated captions of the target rows.</summary>
		readonly List<Action> _targetTexts = new List<Action>();

		/// <summary>The least width of the left column, the machines and commands, in gaps (half a line): the receiver's, at its own window's width.</summary>
		const int LeftGaps = 60;

		/// <summary>The bold font of the status, made from the page's font.</summary>
		Font _boldFont;

		void Build()
		{
			MainTableLayoutPanel = new TableLayoutPanel
			{
				Name = nameof(MainTableLayoutPanel),
				Dock = DockStyle.Fill,
				ColumnCount = 2,
				RowCount = 5,
			};
			// The left column as wide as what is in it; the right one takes the rest of the page.
			MainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			MainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			for (var i = 0; i < 5; i++)
				MainTableLayoutPanel.RowStyles.Add(i == 2 ? new RowStyle(SizeType.Percent, 100F) : new RowStyle());
			_spacing.Add(g => MainTableLayoutPanel.Padding = new Padding(g, g, 0, 0));
			Controls.Add(MainTableLayoutPanel);

			// The note across both columns, in the colours of a tip.
			NoteLabel = new Label
			{
				Name = nameof(NoteLabel),
				AutoSize = true,
				Anchor = AnchorStyles.Left | AnchorStyles.Right,
				BackColor = SystemColors.Info,
				ForeColor = SystemColors.InfoText,
				AccessibleName = "Experimental feature",
			};
			_spacing.Add(g => NoteLabel.Padding = new Padding(g / 2));
			Place(NoteLabel, 0, 0);
			MainTableLayoutPanel.SetColumnSpan(NoteLabel, 2);

			// Left: machines.
			ScanButton = NewButton(nameof(ScanButton), "Scan", "Looks for fitness machines nearby, or stops looking.", 14);
			ScanButton.Click += ScanButton_Click;
			ShowAllCheckBox = new CheckBox
			{
				Name = nameof(ShowAllCheckBox),
				AutoSize = true,
				Anchor = AnchorStyles.Left,
				AccessibleName = "Show all Bluetooth devices",
				AccessibleDescription = "Lists every device that says its name, for machines that show the fitness service only once connected.",
			};
			ScanFlowLayoutPanel = NewFlow(nameof(ScanFlowLayoutPanel), ScanButton, ShowAllCheckBox);
			Place(ScanFlowLayoutPanel, 0, 1);

			// A grid, as x360ce's other lists are: the theme draws it, and the AI door reads its rows and chooses one by its index.
			MachinesDataGridView = new DataGridView
			{
				Name = nameof(MachinesDataGridView),
				Dock = DockStyle.Fill,
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = false,
				AllowUserToResizeRows = false,
				AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
				ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
				MultiSelect = false,
				ReadOnly = true,
				RowHeadersVisible = false,
				SelectionMode = DataGridViewSelectionMode.FullRowSelect,
				AccessibleName = "Fitness machines",
				AccessibleDescription = "The machines heard: each one’s name, what kind it is, and its signal. Selecting one chooses it for this device.",
			};
			NameColumn = NewMachineColumn(nameof(NameColumn), DataGridViewAutoSizeColumnMode.Fill);
			TypeColumn = NewMachineColumn(nameof(TypeColumn), DataGridViewAutoSizeColumnMode.AllCells);
			SignalColumn = NewMachineColumn(nameof(SignalColumn), DataGridViewAutoSizeColumnMode.AllCells);
			SignalColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
			MachinesDataGridView.Columns.AddRange(NameColumn, TypeColumn, SignalColumn);
			MachinesDataGridView.SelectionChanged += MachinesDataGridView_SelectionChanged;
			Place(MachinesDataGridView, 0, 2);

			// Left: commands.
			StartButton = NewButton(nameof(StartButton), "Start", "Starts or resumes the machine’s training.", 10);
			StartButton.Click += (s, e) => _session.Send(FitnessCommand.Start, 0);
			PauseButton = NewButton(nameof(PauseButton), "Pause", "Pauses the machine’s training.", 10);
			PauseButton.Click += (s, e) => _session.Send(FitnessCommand.Pause, 0);
			StopButton = NewButton(nameof(StopButton), "Stop", "Stops the machine’s training.", 10);
			StopButton.Click += (s, e) => _session.Send(FitnessCommand.Stop, 0);
			ResetButton = NewButton(nameof(ResetButton), "Reset", "Resets the machine.", 10);
			ResetButton.Click += (s, e) => _session.Send(FitnessCommand.Reset, 0);
			CommandsFlowLayoutPanel = NewFlow(nameof(CommandsFlowLayoutPanel), StartButton, PauseButton, StopButton, ResetButton);
			Place(CommandsFlowLayoutPanel, 0, 3);

			// Left: targets, each greyed while the machine does not take it. At least the column's width, which its sliders fill.
			TargetsTableLayoutPanel = new TableLayoutPanel
			{
				Name = nameof(TargetsTableLayoutPanel),
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Anchor = AnchorStyles.Left | AnchorStyles.Right,
				ColumnCount = 4,
				RowCount = 3,
			};
			TargetsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle());
			TargetsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			TargetsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle());
			TargetsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle());
			for (var i = 0; i < 3; i++)
				TargetsTableLayoutPanel.RowStyles.Add(new RowStyle());
			_spacing.Add(g => TargetsTableLayoutPanel.MinimumSize = new Size(LeftGaps * g, 0));
			PowerTrackBar = AddTarget(0, "Power", "Target power",
				() => PluginText.T("FitnessMachine.TargetPower", "Target power"), 0, 1500, 150, 5, 25);
			PowerValueLabel = (Label)TargetsTableLayoutPanel.GetControlFromPosition(2, 0);
			SetPowerButton = (Button)TargetsTableLayoutPanel.GetControlFromPosition(3, 0);
			SetPowerButton.Click += (s, e) => _session.Send(FitnessCommand.Power, PowerTrackBar.Value);
			ResistanceTrackBar = AddTarget(1, "Resistance", "Target resistance",
				() => PluginText.T("FitnessMachine.TargetResistance", "Target resistance"), 0, 20, 5, 1, 2);
			ResistanceValueLabel = (Label)TargetsTableLayoutPanel.GetControlFromPosition(2, 1);
			SetResistanceButton = (Button)TargetsTableLayoutPanel.GetControlFromPosition(3, 1);
			SetResistanceButton.Click += (s, e) => _session.Send(FitnessCommand.Resistance, ResistanceTrackBar.Value);
			// Half percents, so the slider steps by half a percent.
			GradeTrackBar = AddTarget(2, "Grade", "Road grade",
				() => PluginText.T("FitnessMachine.RoadGrade", "Road grade"), -40, 40, 0, 1, 4);
			GradeValueLabel = (Label)TargetsTableLayoutPanel.GetControlFromPosition(2, 2);
			SetGradeButton = (Button)TargetsTableLayoutPanel.GetControlFromPosition(3, 2);
			SetGradeButton.Click += (s, e) => _session.Send(FitnessCommand.Grade, GradeTrackBar.Value / 2.0);
			Place(TargetsTableLayoutPanel, 0, 4);

			// Right: where the connection stands, and what the machine measures and accepts, wrapped at the column's width.
			StatusLabel = new Label
			{
				Name = nameof(StatusLabel),
				AutoSize = true,
				Anchor = AnchorStyles.Left | AnchorStyles.Right,
				AccessibleName = "Status",
				AccessibleDescription = "Where this device’s connection stands and, when it is stuck, what to do next.",
			};
			FeaturesLabel = new Label
			{
				Name = nameof(FeaturesLabel),
				AutoSize = true,
				Anchor = AnchorStyles.Left | AnchorStyles.Right,
				ForeColor = SystemColors.GrayText,
				AccessibleName = "Features",
				AccessibleDescription = "What the machine connected measures, and the targets it accepts.",
			};
			StatusTableLayoutPanel = new TableLayoutPanel
			{
				Name = nameof(StatusTableLayoutPanel),
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Anchor = AnchorStyles.Left | AnchorStyles.Right,
				ColumnCount = 1,
				RowCount = 2,
			};
			StatusTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			StatusTableLayoutPanel.RowStyles.Add(new RowStyle());
			StatusTableLayoutPanel.RowStyles.Add(new RowStyle());
			StatusTableLayoutPanel.Controls.Add(StatusLabel, 0, 0);
			StatusTableLayoutPanel.Controls.Add(FeaturesLabel, 0, 1);
			_spacing.Add(g =>
			{
				StatusLabel.Margin = new Padding(0, 0, 0, g / 2);
				FeaturesLabel.Margin = new Padding(0);
			});
			Place(StatusTableLayoutPanel, 1, 1);

			// Right: the values, beside the list of machines: each value's name, the value, and how often it changes, packed
			// on the left.
			ReadingsTableLayoutPanel = new TableLayoutPanel
			{
				Name = nameof(ReadingsTableLayoutPanel),
				Dock = DockStyle.Top,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				ColumnCount = 3,
			};
			ReadingsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle());
			ReadingsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle());
			ReadingsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			ReadingsPanel = new Panel
			{
				Name = nameof(ReadingsPanel),
				Dock = DockStyle.Fill,
				AutoScroll = true,
				BorderStyle = BorderStyle.FixedSingle,
				BackColor = SystemColors.ControlLight,
				AccessibleName = "Values",
				AccessibleDescription = "The newest value of every field the machine sent, and how often each changes.",
			};
			ReadingsPanel.Controls.Add(ReadingsTableLayoutPanel);
			_spacing.Add(g => ReadingsPanel.Padding = new Padding(g, g / 2, g, g / 2));
			Place(ReadingsPanel, 1, 2);

			// Right: what happened, as tall as the commands and targets beside it.
			LogTextBox = new TextBox
			{
				Name = nameof(LogTextBox),
				Dock = DockStyle.Fill,
				Multiline = true,
				ReadOnly = true,
				WordWrap = true,
				BackColor = SystemColors.ControlLight,
				AccessibleName = "Activity",
				AccessibleDescription = "What happened, newest first.",
			};
			LogTextBox.SizeChanged += (s, e) => PostLogScrollBar();
			LogTextBox.HandleCreated += (s, e) => PostLogScrollBar();
			Place(LogTextBox, 1, 3);
			MainTableLayoutPanel.SetRowSpan(LogTextBox, 2);
			ApplyFont();
		}

		void ApplyTexts()
		{
			Text = PluginText.T("FitnessMachine.Connection", "Connection");
			NoteLabel.Text = PluginText.T("FitnessMachine.ExperimentalNote",
				"Experimental: walk, drive or row in games with a fitness machine. Tested on a Tunturi E60R exercise bike only. Use it with care and at your own risk.");
			ShowAllCheckBox.Text = PluginText.T("FitnessMachine.ShowAllBluetoothDevices", "Show all Bluetooth devices");
			NameColumn.HeaderText = PluginText.T("FitnessMachine.Name", "Name");
			TypeColumn.HeaderText = PluginText.T("FitnessMachine.Type", "Type");
			SignalColumn.HeaderText = PluginText.T("FitnessMachine.Signal", "Signal");
			StartButton.Text = PluginText.T("FitnessMachine.Start", "Start");
			PauseButton.Text = PluginText.T("FitnessMachine.Pause", "Pause");
			StopButton.Text = PluginText.T("FitnessMachine.Stop", "Stop");
			ResetButton.Text = PluginText.T("FitnessMachine.Reset", "Reset");
			foreach (var text in _targetTexts)
				text();
		}

		void LanguageChanged(object sender, EventArgs e)
		{
			ApplyTexts();
			ShowSession(true);
		}

		/// <summary>Puts a control in a cell of the main table, with the room after it the font gives, and twice that between the columns.</summary>
		void Place(Control control, int column, int row)
		{
			_spacing.Add(g => control.Margin = new Padding(column == 1 ? g : 0, 0, g, g));
			MainTableLayoutPanel.Controls.Add(control, column, row);
		}

		/// <summary>A button with an English name for the AI door and screen readers.</summary>
		[System.ComponentModel.Localizable(false)]
		Button NewButton(string name, string accessibleName, string description, int widthInGaps)
		{
			var button = new Button
			{
				Name = name,
				AccessibleName = accessibleName,
				AutoSize = true,
				UseVisualStyleBackColor = true,
				AccessibleDescription = description,
			};
			_spacing.Add(g => button.MinimumSize = new Size(g * widthInGaps, 0));
			return button;
		}

		/// <summary>A column of the list of machines, kept in the order the machines were heard rather than sorted.</summary>
		static DataGridViewTextBoxColumn NewMachineColumn(string name, DataGridViewAutoSizeColumnMode size)
		{
			return new DataGridViewTextBoxColumn
			{
				Name = name,
				AutoSizeMode = size,
				SortMode = DataGridViewColumnSortMode.NotSortable,
			};
		}

		/// <summary>A row of controls, each the room the font gives after the one before it.</summary>
		FlowLayoutPanel NewFlow(string name, params Control[] controls)
		{
			var flow = new FlowLayoutPanel
			{
				Name = name,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Anchor = AnchorStyles.Left | AnchorStyles.Right,
				WrapContents = true,
			};
			flow.Controls.AddRange(controls);
			_spacing.Add(g =>
			{
				foreach (Control control in flow.Controls)
					control.Margin = new Padding(0, 0, g, 0);
			});
			return flow;
		}

		/// <summary>Adds a target's row: its label, its slider, its value and its Set button.</summary>
		[System.ComponentModel.Localizable(false)]
		TrackBar AddTarget(int row, string key, string text, Func<string> caption, int minimum, int maximum, int value, int smallChange, int largeChange)
		{
			var label = new Label { Name = key + "Label", AccessibleName = text, AutoSize = true, Anchor = AnchorStyles.Left };
			var slider = new TrackBar
			{
				Name = key + "TrackBar",
				Minimum = minimum,
				Maximum = maximum,
				Value = value,
				SmallChange = smallChange,
				LargeChange = largeChange,
				TickStyle = TickStyle.None,
				Anchor = AnchorStyles.Left | AnchorStyles.Right,
				AccessibleName = text,
				AccessibleDescription = "The " + text.ToLowerInvariant() + " Set sends to the machine.",
			};
			var shown = new Label { Name = key + "ValueLabel", AutoSize = true, Anchor = AnchorStyles.Right, TextAlign = ContentAlignment.MiddleRight, AccessibleName = text };
			var set = new Button
			{
				Name = "Set" + key + "Button",
				AutoSize = true,
				Anchor = AnchorStyles.Left,
				UseVisualStyleBackColor = true,
				AccessibleName = "Set " + text.ToLowerInvariant(),
				AccessibleDescription = "Sends the " + text.ToLowerInvariant() + " to the machine.",
			};
			_targetTexts.Add(() =>
			{
				label.Text = caption();
				set.Text = PluginText.T("FitnessMachine.Set", "Set");
			});
			slider.ValueChanged += (s, e) => ShowTargets();
			_spacing.Add(g =>
			{
				label.Margin = new Padding(0, 0, g, 0);
				slider.Margin = new Padding(0, 0, g, 0);
				shown.Margin = new Padding(0, 0, g, 0);
				shown.MinimumSize = new Size(g * 8, 0);
				set.Margin = new Padding(0, 0, 0, g / 2);
				set.MinimumSize = new Size(g * 8, 0);
			});
			TargetsTableLayoutPanel.Controls.Add(label, 0, row);
			TargetsTableLayoutPanel.Controls.Add(slider, 1, row);
			TargetsTableLayoutPanel.Controls.Add(shown, 2, row);
			TargetsTableLayoutPanel.Controls.Add(set, 3, row);
			return slider;
		}

		/// <summary>Gives the controls their room and the bold font from the page's font, which follows x360ce's font and zoom.</summary>
		void ApplyFont()
		{
			var old = _boldFont;
			_boldFont = new Font(Font, FontStyle.Bold);
			StatusLabel.Font = _boldFont;
			if (old != null)
				old.Dispose();
			// Half a line, 8 pixels at 100 % in x360ce's font.
			var gap = Math.Max(1, (Font.Height + 1) / 2);
			foreach (var space in _spacing)
				space(gap);
		}

		protected override void OnFontChanged(EventArgs e)
		{
			base.OnFontChanged(e);
			if (MainTableLayoutPanel != null)
				ApplyFont();
		}

		#endregion

		#region What the page shows

		/// <summary>The machine selected in the list, by a click or by the AI door setting the list to its row; null while none is.</summary>
		/// <remarks>Read from the rows selected, which the grid changes before the current row it raises SelectionChanged for.</remarks>
		FitnessSeen SelectedMachine
		{
			get
			{
				var rows = MachinesDataGridView.SelectedRows;
				return rows.Count == 1 ? rows[0].Tag as FitnessSeen : null;
			}
		}

		/// <summary>Shows what the device, its session and the scan hold, when one changed since the page showed it, or at once.</summary>
		void ShowSession(bool now = false)
		{
			// Listing the machines changes the selection, which asks for this again.
			if (IsDisposed || _showing)
				return;
			var version = _session.Version;
			var scan = _scan.Version;
			var machine = _device.Machine;
			var reading = _device.Reading;
			if (!now && version == _shownVersion && scan == _shownScan && machine == _shownMachine && reading == _shownReading)
				return;
			_shownVersion = version;
			_shownScan = scan;
			_shownMachine = machine;
			_shownReading = reading;
			_showing = true;
			try
			{
				Draw();
			}
			finally
			{
				_showing = false;
			}
		}

		/// <summary>True while the page shows the session: the list's selection then changes the device's machine.</summary>
		bool _showing;

		void Draw()
		{
			var connected = _session.Held != null;
			ShowMachines();
			SetText(StatusLabel, StatusText());
			SetText(FeaturesLabel, _session.FeaturesText);
			var scanning = _scan.Scanning;
			ScanButton.AccessibleName = scanning ? "Stop scanning" : "Scan";
			SetText(ScanButton, scanning
				? PluginText.T("FitnessMachine.StopScanning", "Stop scanning")
				: PluginText.T("FitnessMachine.Scan", "Scan"));
			ShowAllCheckBox.Enabled = !scanning;
			var commands = connected && _session.CanCommand && !_session.Busy;
			StartButton.Enabled = commands;
			PauseButton.Enabled = commands;
			StopButton.Enabled = commands;
			ResetButton.Enabled = commands;
			ShowRange(PowerTrackBar, _session.PowerMin, _session.PowerMax, 1);
			ShowRange(ResistanceTrackBar, _session.RangeMin, _session.RangeMax, (int)Ftms.ResistanceScale);
			EnableTarget(PowerTrackBar, SetPowerButton, commands && _session.Supports(Ftms.PowerTargetBit));
			EnableTarget(ResistanceTrackBar, SetResistanceButton, commands && _session.Supports(Ftms.ResistanceTargetBit));
			EnableTarget(GradeTrackBar, SetGradeButton, commands && _session.Supports(Ftms.SimulationTargetBit));
			ShowTargets();
			ShowReadings();
			var log = string.Join(Environment.NewLine, _session.Log());
			if (LogTextBox.Text != log)
			{
				LogTextBox.Text = log;
				PostLogScrollBar();
			}
		}

		/// <summary>The status: the scan while it runs and the device is not connected, why the scan stopped, or the device's connection.</summary>
		string StatusText()
		{
			if (_session.Held == null)
			{
				if (_scan.Scanning)
				{
					var count = _scan.Machines().Length;
					if (_scan.ShowAll)
						return count == 0 ? PluginText.T("FitnessMachine.ScanNoDevices", "Scanning. Devices found: 0.")
							: count == 1 ? PluginText.T("FitnessMachine.ScanOneDevice", "Scanning. Device found: 1. Select the one this device connects to.")
							: string.Format(PluginText.T("FitnessMachine.ScanMultipleDevices", "Scanning. Devices found: {0}. Select the one this device connects to."), count);
					return count == 0 ? PluginText.T("FitnessMachine.ScanNoMachines", "Scanning. Fitness machines found: 0.")
						: count == 1 ? PluginText.T("FitnessMachine.ScanOneMachine", "Scanning. Fitness machine found: 1. Select the one this device connects to.")
						: string.Format(PluginText.T("FitnessMachine.ScanMultipleMachines", "Scanning. Fitness machines found: {0}. Select the one this device connects to."), count);
				}
				var problem = _scan.Problem;
				if (problem != null)
					return problem;
			}
			return _device.Connection;
		}

		static void SetText(Control control, string text)
		{
			if (control.Text != text)
				control.Text = text;
		}

		static void EnableTarget(TrackBar slider, Button set, bool enabled)
		{
			slider.Enabled = enabled;
			set.Enabled = enabled;
		}

		/// <summary>Takes a slider's range from the one the machine reported, in its units, keeping its value inside.</summary>
		static void ShowRange(TrackBar slider, int min, int max, int scale)
		{
			if (min == FitnessSession.Unknown || max == FitnessSession.Unknown)
				return;
			var minimum = (int)Math.Ceiling(min / (double)scale);
			var maximum = (int)Math.Floor(max / (double)scale);
			if (maximum <= minimum || (slider.Minimum == minimum && slider.Maximum == maximum))
				return;
			var value = Math.Max(minimum, Math.Min(maximum, slider.Value));
			slider.Minimum = Math.Min(minimum, slider.Minimum);
			slider.Maximum = Math.Max(maximum, slider.Maximum);
			slider.Value = value;
			slider.Minimum = minimum;
			slider.Maximum = maximum;
		}

		/// <summary>True while a look at the log's scroll bar waits for the message loop.</summary>
		bool _scrollBarPosted;

		/// <summary>Looks at the log's scroll bar once the windows being made and laid out now are done.</summary>
		/// <remarks>
		/// A change of scroll bars makes the box's window again. Made again from inside the call that makes it, or that
		/// sizes it as its table is made, Windows refuses to make it ("Error creating window handle"), so the look waits for
		/// the message loop.
		/// </remarks>
		void PostLogScrollBar()
		{
			if (_scrollBarPosted || !LogTextBox.IsHandleCreated)
				return;
			_scrollBarPosted = true;
			LogTextBox.BeginInvoke((Action)ShowLogScrollBar);
		}

		/// <summary>Shows the log's scroll bar only while its lines, wrapped, are more than it shows.</summary>
		void ShowLogScrollBar()
		{
			_scrollBarPosted = false;
			// Its lines are counted by its window, which is not made before it is shown.
			if (IsDisposed || !LogTextBox.IsHandleCreated)
				return;
			var lines = LogTextBox.TextLength == 0 ? 0 : LogTextBox.GetLineFromCharIndex(LogTextBox.TextLength) + 1;
			var bars = lines * LogTextBox.Font.Height > LogTextBox.ClientSize.Height ? ScrollBars.Vertical : ScrollBars.None;
			if (LogTextBox.ScrollBars != bars)
				LogTextBox.ScrollBars = bars;
		}

		void ShowTargets()
		{
			SetText(PowerValueLabel, PowerTrackBar.Value.ToString(CultureInfo.InvariantCulture) + " W");
			SetText(ResistanceValueLabel, ResistanceTrackBar.Value.ToString(CultureInfo.InvariantCulture));
			SetText(GradeValueLabel, (GradeTrackBar.Value / 2.0).ToString("0.0", CultureInfo.InvariantCulture) + " %");
		}

		/// <summary>Lists the machines heard, a row each in the order heard, with the machine chosen first while the scan has not heard it, and selects the machine chosen.</summary>
		void ShowMachines()
		{
			var chosen = FitnessSession.Want.Parse(_device.Machine);
			var machines = _scan.Machines();
			if (chosen == null)
				_chosenRow = null;
			else if (!machines.Any(x => x.Address == chosen.Address))
			{
				if (_chosenRow == null || _chosenRow.Address != chosen.Address || _chosenRow.Name != chosen.Name)
					_chosenRow = FitnessSeen.Chosen(chosen);
				machines = new[] { _chosenRow }.Concat(machines).ToArray();
			}
			var grid = MachinesDataGridView;
			var rows = grid.Rows;
			if (rows.Count != machines.Length || machines.Where((x, i) => !ReferenceEquals(rows[i].Tag, x)).Any())
			{
				rows.Clear();
				foreach (var machine in machines)
					rows[rows.Add()].Tag = machine;
			}
			// Names and signals change in place.
			foreach (DataGridViewRow row in rows)
			{
				var machine = (FitnessSeen)row.Tag;
				SetCell(row.Cells[NameColumn.Index], machine.DisplayName);
				SetCell(row.Cells[TypeColumn.Index], machine.Description);
				SetCell(row.Cells[SignalColumn.Index], machine.SignalText);
			}
			// Only the machine chosen is selected: a grid with the focus makes the first row it is given current by itself.
			var index = chosen == null ? -1 : Array.FindIndex(machines, x => x.Address == chosen.Address);
			var selected = SelectedMachine;
			if (index < 0)
			{
				if (grid.SelectedRows.Count > 0 || grid.CurrentCell != null)
				{
					grid.CurrentCell = null;
					grid.ClearSelection();
				}
			}
			else if (selected != machines[index])
				grid.CurrentCell = rows[index].Cells[NameColumn.Index];
		}

		static void SetCell(DataGridViewCell cell, string text)
		{
			if (!Equals(cell.Value, text))
				cell.Value = text;
		}

		/// <summary>Shows the values, a row for each in the order they first came: its name, its value, and how often it changes.</summary>
		/// <remarks>
		/// The value and how often it changes are named and described, so the AI door lists them under the box of values,
		/// such as SpeedValueLabel, named Speed; the name before them only captions the value, which carries it.
		/// </remarks>
		void ShowReadings()
		{
			var readings = _session.Readings();
			var table = ReadingsTableLayoutPanel;
			// A new connection starts from no values.
			if (readings.Length < table.RowCount)
			{
				table.SuspendLayout();
				while (table.Controls.Count > 0)
					table.Controls[0].Dispose();
				table.RowStyles.Clear();
				table.RowCount = 0;
				table.ResumeLayout();
			}
			for (var i = 0; i < readings.Length; i++)
			{
				var reading = readings[i];
				if (i >= table.RowCount)
				{
					// "Heart rate" is HeartRate.
					var key = new string(CultureInfo.InvariantCulture.TextInfo.ToTitleCase(reading.Name).Where(char.IsLetterOrDigit).ToArray());
					var words = reading.Name.ToLowerInvariant();
					var gap = Math.Max(1, (Font.Height + 1) / 2);
					table.RowStyles.Add(new RowStyle());
					table.RowCount++;
					table.Controls.Add(new Label { Name = key + "NameLabel", Text = reading.Name, AutoSize = true, ForeColor = SystemColors.GrayText,
						Margin = new Padding(0, gap / 4, gap * 2, gap / 4) }, 0, i);
					table.Controls.Add(new Label { Name = key + "ValueLabel", AutoSize = true, AccessibleName = reading.Name,
						AccessibleDescription = "The newest " + words + " the machine sent.",
						Margin = new Padding(0, gap / 4, 0, gap / 4) }, 1, i);
					table.Controls.Add(new Label { Name = key + "IntervalLabel", AutoSize = true, ForeColor = SystemColors.GrayText,
						Margin = new Padding(gap * 2, gap / 4, 0, gap / 4), AccessibleName = reading.Name + " changes",
						AccessibleDescription = "How often the " + words + " changes, such as every 1.0 s; empty until it changed three times." }, 2, i);
				}
				SetText(table.GetControlFromPosition(1, i), reading.Value);
				SetText(table.GetControlFromPosition(2, i), reading.Interval);
			}
		}

		#endregion

		#region What the page does

		void ScanButton_Click(object sender, EventArgs e)
		{
			if (_scan.Scanning)
				_scan.Stop();
			else
				_scan.Start(ShowAllCheckBox.Checked);
			ShowSession(true);
		}

		/// <summary>Chooses the machine the person selected for the device, and stops the scan, which found it.</summary>
		void MachinesDataGridView_SelectionChanged(object sender, EventArgs e)
		{
			if (_showing)
				return;
			var machine = SelectedMachine;
			if (machine == null)
				return;
			_scan.Stop();
			_device.Choose(machine.Address, machine.DisplayName);
			ShowSession(true);
		}

		/// <summary>Asks the person whether to pair, on the interface thread, and gives the session the answer.</summary>
		void AskPairing(FitnessPairing pairing)
		{
			if (_ui == null)
			{
				pairing.Accept(false);
				return;
			}
			_ui.Post(_ =>
			{
				if (IsDisposed)
				{
					pairing.Accept(false);
					return;
				}
				using (var form = new FitnessQuestionForm(PluginText.T("FitnessMachine.PairWithMachine", "Pair with fitness machine"),
					pairing.Question, pairing.Kind == FitnessPairingKind.DisplayPin))
				{
					var answer = form.ShowDialog(FindForm());
					pairing.Accept(answer == DialogResult.Yes || answer == DialogResult.OK);
				}
			}, null);
		}

		#endregion

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				PluginText.Changed -= LanguageChanged;
				if (_timer != null)
					_timer.Dispose();
				// Another page of the device may ask from now on.
				if (Equals(_session.AskPerson, _ask))
					_session.AskPerson = null;
				if (_boldFont != null)
					_boldFont.Dispose();
			}
			base.Dispose(disposing);
		}
	}

	/// <summary>A question the Connection page asks: Yes and No, or OK and Cancel. A window of x360ce's interface thread, so x360ce's theme draws it.</summary>
	sealed class FitnessQuestionForm : Form
	{
		public FitnessQuestionForm(string caption, string question, bool okCancel)
		{
			Name = "FitnessQuestionForm";
			Text = caption;
			FormBorderStyle = FormBorderStyle.FixedDialog;
			StartPosition = FormStartPosition.CenterParent;
			MaximizeBox = false;
			MinimizeBox = false;
			ShowInTaskbar = false;
			AutoSize = true;
			AutoSizeMode = AutoSizeMode.GrowAndShrink;
			var gap = Math.Max(1, (Font.Height + 1) / 2);
			var table = new TableLayoutPanel
			{
				Name = "QuestionTableLayoutPanel",
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				ColumnCount = 1,
				Padding = new Padding(gap),
			};
			var label = new Label
			{
				Name = "QuestionLabel",
				Text = question,
				AutoSize = true,
				MaximumSize = new Size(Font.Height * 28, 0),
				Margin = new Padding(0, 0, 0, gap * 2),
			};
			var yes = new Button
			{
				Name = "YesButton",
				Text = okCancel ? PluginText.T("FitnessMachine.Ok", "OK") : PluginText.T("FitnessMachine.Yes", "&Yes"),
				DialogResult = okCancel ? DialogResult.OK : DialogResult.Yes,
				AutoSize = true,
				UseVisualStyleBackColor = true,
				MinimumSize = new Size(gap * 10, 0),
				Margin = new Padding(0, 0, gap, 0),
			};
			var no = new Button
			{
				Name = "NoButton",
				Text = okCancel ? PluginText.T("FitnessMachine.Cancel", "Cancel") : PluginText.T("FitnessMachine.No", "&No"),
				DialogResult = okCancel ? DialogResult.Cancel : DialogResult.No,
				AutoSize = true,
				UseVisualStyleBackColor = true,
				MinimumSize = new Size(gap * 10, 0),
				Margin = new Padding(0),
			};
			var buttons = new FlowLayoutPanel { Name = "QuestionButtonsFlowLayoutPanel", AutoSize = true, Anchor = AnchorStyles.Right };
			buttons.Controls.Add(yes);
			buttons.Controls.Add(no);
			table.Controls.Add(label, 0, 0);
			table.Controls.Add(buttons, 0, 1);
			Controls.Add(table);
			AcceptButton = yes;
			CancelButton = no;
		}
	}
}
