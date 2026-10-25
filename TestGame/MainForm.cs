using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace x360ce.TestGame
{
	/// <summary>
	/// The game's window: the newest action large at the top, the newest event under it, the events as they arrive, and what
	/// is held now. It reads the keyboard, the mouse and the controllers every way a game may, Raw Input, window messages,
	/// DirectInput and XInput, all at once, so each way's limits show side by side, in front and behind. Keys reach the
	/// list, never a button or a box, so a key a script sends cannot press Clear.
	/// </summary>
	public sealed class MainForm : Form
	{
		/// <summary>How many rows the list shows; the recorder keeps more for the AI door.</summary>
		const int ShownRows = 500;

		/// <summary>The ways of reading the keyboard a jump may follow, as events name them.</summary>
		static readonly string[] KeyboardApis = { "RawInput", "Messages", "DirectInput" };

		public readonly InputRecorder Recorder = new InputRecorder();
		/// <summary>XInput and DirectInput, read on a thread of their own; made with the window's handle.</summary>
		public DevicePoller Devices { get; private set; }
		readonly MessageReader Messages;

		readonly Label LastActionLabel;
		readonly Label LastEventLabel;
		readonly ComboBox JumpKeyComboBox;
		readonly ComboBox JumpReadsComboBox;
		readonly NumericUpDown DoubleJumpNumericUpDown;
		readonly CheckBox RecordBehindCheckBox;
		readonly CheckBox DirectInputKeyboardCheckBox;
		readonly Button ClearButton;
		readonly Label DoorLabel;
		readonly DataGridView EventsDataGridView;
		readonly TextBox StateTextBox;
		readonly Timer PollTimer;
		readonly string _placeFile;
		string _shownState;

		/// <summary>True while the window is the one in front, the only time window messages reach it.</summary>
		public bool InFront { get; private set; }

		/// <param name="folder">Where the window keeps where it was, so it opens there again.</param>
		/// <param name="recordBehind">Also record the keyboard and mouse while the window is behind.</param>
		public MainForm(string folder, bool recordBehind)
		{
			_placeFile = Path.Combine(folder, "x360ce.TestGame.window.txt");
			Name = "TestGame";
			Text = "x360ce Test Game";
			AccessibleName = "x360ce Test Game";
			AutoScaleDimensions = new SizeF(6F, 13F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(1000, 640);
			StartPosition = FormStartPosition.CenterScreen;
			Messages = new MessageReader(Accept, () => Recorder.Now);

			var root = new TableLayoutPanel { Name = "RootPanel", Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(3) };
			root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			root.RowStyles.Add(new RowStyle());
			root.RowStyles.Add(new RowStyle());
			root.RowStyles.Add(new RowStyle());
			root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

			LastActionLabel = new Label
			{
				Name = "LastActionLabel",
				AccessibleName = "Last action",
				AccessibleDescription = "The newest action the game made of its input: a jump, or a double jump with its gap.",
				AutoSize = true,
				Margin = new Padding(3),
				Font = new Font(Font.FontFamily, Font.Size * 2, FontStyle.Bold),
				Text = "No action yet.",
			};
			LastEventLabel = new Label
			{
				Name = "LastEventLabel",
				AccessibleName = "Last event",
				AccessibleDescription = "The newest key, mouse or controller event the game received, how it read it, and whether it was in front.",
				AutoSize = true,
				Margin = new Padding(3),
				Text = "Bring this window to the front, then press keys, move the mouse or use a controller.",
			};

			var options = new FlowLayoutPanel { Name = "OptionsPanel", AutoSize = true, Dock = DockStyle.Fill, WrapContents = true, Margin = new Padding(0) };
			JumpKeyComboBox = new ComboBox
			{
				Name = "JumpKeyComboBox",
				AccessibleName = "Jump key",
				AccessibleDescription = "The key that jumps. Button A on a controller, through XInput, jumps too.",
				DropDownStyle = ComboBoxStyle.DropDownList,
				TabStop = false,
				Margin = new Padding(3),
			};
			JumpKeyComboBox.Items.AddRange(new object[] { "Space", "Up", "W", "Return", "ControlKey", "ShiftKey" });
			JumpKeyComboBox.SelectedItem = Recorder.JumpKey;
			JumpKeyComboBox.SelectedIndexChanged += (s, e) => { Recorder.JumpKey = (string)JumpKeyComboBox.SelectedItem; FocusList(); };
			JumpReadsComboBox = new ComboBox
			{
				Name = "JumpReadsComboBox",
				AccessibleName = "Jump reads",
				AccessibleDescription = "Which way of reading the keyboard the jump follows. Every way records every key, so only one counts.",
				DropDownStyle = ComboBoxStyle.DropDownList,
				TabStop = false,
				Margin = new Padding(3),
			};
			JumpReadsComboBox.Items.AddRange(KeyboardApis);
			JumpReadsComboBox.SelectedItem = Recorder.JumpApi;
			JumpReadsComboBox.SelectedIndexChanged += (s, e) => { Recorder.JumpApi = (string)JumpReadsComboBox.SelectedItem; FocusList(); };
			DoubleJumpNumericUpDown = new NumericUpDown
			{
				Name = "DoubleJumpNumericUpDown",
				AccessibleName = "Double jump within",
				AccessibleDescription = "How soon after a jump a second press makes a double jump, in milliseconds.",
				Minimum = 20,
				Maximum = 2000,
				Value = Recorder.DoubleJumpMs,
				TabStop = false,
				Margin = new Padding(3),
			};
			DoubleJumpNumericUpDown.ValueChanged += (s, e) => Recorder.DoubleJumpMs = (int)DoubleJumpNumericUpDown.Value;
			RecordBehindCheckBox = new CheckBox
			{
				Name = "RecordBehindCheckBox",
				AccessibleName = "Record keyboard and mouse while behind",
				AccessibleDescription = "Also records the keyboard and mouse while another window is in front: everything typed in any program, "
					+ "passwords too. Off unless the person ticks it or starts the game with /RecordBehind; the AI door cannot tick it.",
				Text = "Record keyboard and mouse while behind (every program's keys)",
				AutoSize = true,
				Checked = recordBehind,
				TabStop = false,
				Margin = new Padding(9, 6, 3, 3),
			};
			RecordBehindCheckBox.CheckedChanged += (s, e) => { RegisterRawInput(); FocusList(); };
			DirectInputKeyboardCheckBox = new CheckBox
			{
				Name = "DirectInputKeyboardCheckBox",
				AccessibleName = "Keyboard and mouse through DirectInput",
				AccessibleDescription = "Reads the keyboard and mouse through DirectInput too. DirectInput reads them through Raw Input under the hood, "
					+ "and Windows lets a program register one window for each, so while this is ticked Raw Input reports no keyboard or mouse.",
				Text = "Keyboard and mouse through DirectInput (Raw Input then hears none)",
				AutoSize = true,
				TabStop = false,
				Margin = new Padding(9, 6, 3, 3),
			};
			DirectInputKeyboardCheckBox.CheckedChanged += (s, e) =>
			{
				if (Devices != null)
					Devices.DirectInput.KeyboardAndMouse = DirectInputKeyboardCheckBox.Checked;
				FocusList();
			};
			ClearButton = new Button
			{
				Name = "ClearButton",
				AccessibleName = "Clear",
				AccessibleDescription = "Forgets the events and the last action.",
				Text = "Clear",
				AutoSize = true,
				TabStop = false,
				Margin = new Padding(9, 3, 3, 3),
			};
			ClearButton.Click += (s, e) => { ClearEvents(); FocusList(); };
			DoorLabel = new Label
			{
				Name = "DoorLabel",
				AccessibleName = "AI door",
				AccessibleDescription = "Where tests and AI assistants read what the game received.",
				AutoSize = true,
				Margin = new Padding(9, 8, 3, 3),
			};
			options.Controls.AddRange(new Control[]
			{
				Caption("JumpKeyLabel", "Jump key:"), JumpKeyComboBox,
				Caption("JumpReadsLabel", "read through:"), JumpReadsComboBox,
				Caption("DoubleJumpLabel", "Double jump within (ms):"), DoubleJumpNumericUpDown,
				RecordBehindCheckBox, DirectInputKeyboardCheckBox, ClearButton, DoorLabel,
			});

			var content = new TableLayoutPanel { Name = "ContentPanel", Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
			content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
			content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
			content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			EventsDataGridView = new DataGridView
			{
				Name = "EventsDataGridView",
				AccessibleName = "Events",
				AccessibleDescription = "What the game received, newest first, with the way it read each one and whether it was in front.",
				Dock = DockStyle.Fill,
				ReadOnly = true,
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = false,
				AllowUserToResizeRows = false,
				RowHeadersVisible = false,
				SelectionMode = DataGridViewSelectionMode.FullRowSelect,
				AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
				Margin = new Padding(3),
			};
			foreach (var name in new[] { "Seq", "Time", "Api", "Where", "Source", "Kind", "Name", "Text", "Value", "From", "Note" })
				EventsDataGridView.Columns.Add(name + "Column", name);
			// The widest column last, filling what is left, as x360ce's lists have it.
			EventsDataGridView.Columns["NoteColumn"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
			EventsDataGridView.Columns["NoteColumn"].MinimumWidth = 120;
			// The window's colour below the rows, as x360ce's lists have it, rather than the grey of an empty control.
			EventsDataGridView.BackgroundColor = SystemColors.Window;
			EventsDataGridView.BorderStyle = BorderStyle.None;
			StateTextBox = new TextBox
			{
				Name = "StateTextBox",
				AccessibleName = "Held now",
				AccessibleDescription = "What is held now, which devices DirectInput reads, and where the XInput controllers' sticks and triggers are.",
				Dock = DockStyle.Fill,
				Multiline = true,
				ReadOnly = true,
				TabStop = false,
				ScrollBars = ScrollBars.Vertical,
				Font = new Font(FontFamily.GenericMonospace, Font.Size),
				Margin = new Padding(3),
			};
			content.Controls.Add(EventsDataGridView, 0, 0);
			content.Controls.Add(StateTextBox, 1, 0);

			root.Controls.Add(LastActionLabel, 0, 0);
			root.Controls.Add(LastEventLabel, 0, 1);
			root.Controls.Add(options, 0, 2);
			root.Controls.Add(content, 0, 3);
			Controls.Add(root);

			Recorder.Added += ShowEvent;
			PollTimer = new Timer { Interval = 10 };
			PollTimer.Tick += PollTimer_Tick;
			LoadPlace();
		}

		static Label Caption(string name, string text)
		{
			return new Label { Name = name, Text = text, AutoSize = true, Margin = new Padding(9, 8, 3, 3) };
		}

		/// <summary>Where the AI door opened, or why it did not, for the line in the window.</summary>
		public string DoorText
		{
			get { return DoorLabel.Text; }
			set { DoorLabel.Text = value; }
		}

		/// <summary>Whether the keyboard and mouse are recorded while the window is behind, which only the person may switch on.</summary>
		public bool RecordBehind { get { return RecordBehindCheckBox.Checked; } }

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			RegisterRawInput();
			Devices = new DevicePoller(Handle, () => Recorder.Now) { InFront = InFront };
			// Let go of by DirectInput, the keyboard and mouse are Raw Input's again once registered again.
			Devices.DirectInput.KeyboardAndMouseChanged = on => BeginInvoke((Action)(() => { if (!on) RegisterRawInput(); }));
			Application.AddMessageFilter(Messages);
			PollTimer.Start();
		}

		/// <summary>Raw Input for the keyboard and mouse, unless DirectInput has them; behind too when the person allows it.</summary>
		void RegisterRawInput()
		{
			if (!DirectInputKeyboardCheckBox.Checked)
				RawInput.Register(Handle, RecordBehind);
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			FocusList();
		}

		protected override void OnFormClosed(FormClosedEventArgs e)
		{
			PollTimer.Stop();
			Application.RemoveMessageFilter(Messages);
			Devices?.Dispose();
			SavePlace();
			base.OnFormClosed(e);
		}

		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);
			InFront = true;
			if (Devices != null)
				Devices.InFront = true;
			Recorder.Add(new InputEvent { Source = "Window", Kind = "InFront", Name = "Window", InFront = true, Note = "The window came to the front." });
		}

		protected override void OnDeactivate(EventArgs e)
		{
			base.OnDeactivate(e);
			InFront = false;
			if (Devices != null)
				Devices.InFront = false;
			// Keys held when the window went behind may be let go of where a way of reading that stops behind cannot see it.
			Recorder.ReleaseAll();
			Recorder.Add(new InputEvent { Source = "Window", Kind = "Behind", Name = "Window", Note = "The window went behind another." });
		}

		protected override void WndProc(ref Message m)
		{
			if (m.Msg == RawInput.WM_INPUT)
			{
				var at = MessageClock.Of(Recorder.Now);
				foreach (var e in RawInput.Read(m.WParam, m.LParam))
				{
					e.Ms = at;
					if (e.Kind == "Move" && !e.FromProgram)
						AddDeviceMove(e);
					else
					{
						FlushDeviceMove();
						Accept(e);
					}
				}
			}
			base.WndProc(ref m);
		}

		/// <summary>
		/// Keeps an event unless it is the keyboard or the mouse while the window is behind and the person has not asked
		/// for that: behind, those are what the person types and does in other programs.
		/// </summary>
		void Accept(InputEvent e)
		{
			if (!e.InFront && !RecordBehind && (e.Source == "Keyboard" || e.Source == "Mouse"))
				return;
			Recorder.Add(e);
		}

		/// <summary>How long a device's Raw Input moves are added together before they are one event.</summary>
		const int DeviceMoveMs = 100;
		InputEvent _deviceMove;
		long _deviceMoveSince;

		/// <summary>
		/// A mouse sends hundreds of moves a second, so a device's moves are added together and kept as one event every
		/// tenth of a second. A program's moves are each kept, since each is one call of a script.
		/// </summary>
		void AddDeviceMove(InputEvent e)
		{
			if (_deviceMove == null || _deviceMove.Absolute != e.Absolute || _deviceMove.InFront != e.InFront)
			{
				FlushDeviceMove();
				_deviceMove = e;
				_deviceMoveSince = Recorder.Now;
				return;
			}
			if (e.Absolute)
			{
				_deviceMove.X = e.X;
				_deviceMove.Y = e.Y;
			}
			else
			{
				_deviceMove.X += e.X;
				_deviceMove.Y += e.Y;
			}
		}

		void FlushDeviceMove()
		{
			if (_deviceMove == null)
				return;
			var move = _deviceMove;
			_deviceMove = null;
			Accept(move);
		}

		void PollTimer_Tick(object sender, EventArgs e)
		{
			var now = Recorder.Now;
			if (_deviceMove != null && now - _deviceMoveSince >= DeviceMoveMs)
				FlushDeviceMove();
			Messages.Flush();
			foreach (var x in Devices.Take())
				Accept(x);
			var state = StateText();
			if (state != _shownState)
			{
				_shownState = state;
				StateTextBox.Text = state;
			}
		}

		void ShowEvent(InputEvent e)
		{
			var from = e.Api == "RawInput" ? (e.FromProgram ? "program" : "device") : "";
			var where = string.IsNullOrEmpty(e.Api) ? "" : e.InFront ? "front" : "behind";
			var value = e.Kind == "Move" ? (e.Absolute ? "to " : "") + e.X + ", " + e.Y
				: e.Kind == "Wheel" || e.Kind == "Axis" || e.Kind == "Hat" || e.Kind == "Double jump" ? e.Value.ToString(CultureInfo.InvariantCulture) : "";
			EventsDataGridView.Rows.Insert(0, e.Seq, e.Time.ToString("HH:mm:ss.fff"), e.Api, where, e.Source, e.Kind, e.Name, e.Text, value, from, e.Note);
			if (EventsDataGridView.Rows.Count > ShownRows)
				EventsDataGridView.Rows.RemoveAt(EventsDataGridView.Rows.Count - 1);
			if (e.Source == "Action")
				LastActionLabel.Text = e.Note;
			else if (!string.IsNullOrEmpty(e.Api))
				LastEventLabel.Text = "Last event: " + e.Source + " " + e.Kind + " " + e.Name + (value.Length == 0 ? "" : " " + value)
					+ ", through " + e.Api + (from.Length > 0 ? " from a " + from : "") + ", " + (e.InFront ? "in front" : "behind")
					+ ", at " + e.Time.ToString("HH:mm:ss.fff") + ".";
		}

		/// <summary>Forgets the events and the last action, on the screen and in the recorder.</summary>
		public int ClearEvents()
		{
			Recorder.Clear();
			EventsDataGridView.Rows.Clear();
			LastActionLabel.Text = "No action yet.";
			return Recorder.Next;
		}

		void FocusList()
		{
			EventsDataGridView.Focus();
		}

		string StateText()
		{
			var s = new StringBuilder();
			s.AppendLine("In front: " + (InFront ? "yes" : "no"));
			s.AppendLine("Keyboard and mouse behind: " + (RecordBehind ? "recorded" : "not recorded"));
			s.AppendLine("Held: " + string.Join(", ", Recorder.Held.OrderBy(x => x)));
			s.AppendLine("Pointer: " + Cursor.Position.X + ", " + Cursor.Position.Y);
			s.AppendLine();
			lock (Devices.Sync)
				StateText(s, Devices.XInput, Devices.DirectInput);
			return s.ToString();
		}

		static void StateText(StringBuilder s, XInputReader xinput, DirectInputReader directInput)
		{
			s.AppendLine("DirectInput reads:");
			foreach (var name in directInput.Devices)
				s.AppendLine("  " + name);
			if (directInput.LastError != null)
				s.AppendLine("  " + directInput.LastError);
			for (var i = 0; i < 4; i++)
			{
				var g = xinput.States[i].Gamepad;
				s.AppendLine();
				if (!xinput.Connected[i])
				{
					s.AppendLine("XInput " + (i + 1) + ": no controller");
					continue;
				}
				var buttons = XInputReader.ButtonNames.Where(b => (g.Buttons & b.Key) != 0).Select(b => b.Value);
				s.AppendLine("XInput " + (i + 1) + ": " + string.Join(" ", buttons));
				s.AppendLine(string.Format("  LX {0,6}  LY {1,6}  LT {2,3}", g.ThumbLX, g.ThumbLY, g.LeftTrigger));
				s.AppendLine(string.Format("  RX {0,6}  RY {1,6}  RT {2,3}", g.ThumbRX, g.ThumbRY, g.RightTrigger));
			}
		}

		/// <summary>Opens where the window was last closed, while that place is still on a screen; otherwise in the middle of the main one.</summary>
		void LoadPlace()
		{
			try
			{
				if (!File.Exists(_placeFile))
					return;
				var parts = File.ReadAllText(_placeFile).Split(' ').Select(x => int.Parse(x, CultureInfo.InvariantCulture)).ToArray();
				var place = new Rectangle(parts[0], parts[1], parts[2], parts[3]);
				// Enough of the title bar on some screen to take hold of: a screen unplugged since leaves nothing to drag.
				var grip = new Rectangle(place.X, place.Y, place.Width, SystemInformation.CaptionHeight);
				if (place.Width < 200 || place.Height < 150 || !Screen.AllScreens.Any(x => x.WorkingArea.IntersectsWith(grip)))
					return;
				StartPosition = FormStartPosition.Manual;
				Bounds = place;
				if (parts.Length > 4 && parts[4] == 1)
					WindowState = FormWindowState.Maximized;
			}
			catch (Exception ex) when (ex is IOException || ex is FormatException || ex is IndexOutOfRangeException || ex is UnauthorizedAccessException)
			{
				// A place that cannot be read is no place: the window opens in the middle.
			}
		}

		void SavePlace()
		{
			var place = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
			var text = string.Join(" ", new[] { place.X, place.Y, place.Width, place.Height, WindowState == FormWindowState.Maximized ? 1 : 0 }
				.Select(x => x.ToString(CultureInfo.InvariantCulture)));
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(_placeFile));
				File.WriteAllText(_placeFile, text);
			}
			catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
			{
				// The next start opens in the middle instead.
			}
		}
	}
}
