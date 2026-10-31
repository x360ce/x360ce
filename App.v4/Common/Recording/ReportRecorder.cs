using JocysCom.ClassLibrary.Runtime;
using SharpDX.XInput;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using x360ce.App.DInput;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Recording
{
	/// <summary>What a recording holds, as the check boxes on the Issues page choose it.</summary>
	[Flags]
	public enum RecordParts
	{
		None = 0,
		/// <summary>The program's options, the game's settings, each controller's mapping, and every device.</summary>
		Settings = 1,
		/// <summary>What each device reports, buttons, axes, sliders and hats, as it changes, and each failed read.</summary>
		Input = 2,
		/// <summary>What each controller sends to the game, the virtual controller it is fed to, and what XInput reads back.</summary>
		Output = 4,
		/// <summary>The vibration each game asks for, the force sent to each device, and how each device is held.</summary>
		Forces = 8,
		/// <summary>How fast the engine runs, and each device call that takes long or does not return.</summary>
		Engine = 16,
		/// <summary>Whether the window answers, the program in front, the handles and memory x360ce holds, and its errors.</summary>
		Window = 32,
		All = Settings | Input | Output | Forces | Engine | Window,
	}

	/// <summary>
	/// Records what x360ce reads from devices and sends to games and devices, and how its engine and window run, into a
	/// text file made to be posted with a bug report.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Each layer is recorded where its value is made, so a value that goes wrong shows where it first went wrong: in what
	/// the device reported, in what x360ce made of it, in what the virtual controller passed on, or in a device call that
	/// held everything up.
	/// </para>
	/// <para>
	/// The engine hands each change to a ring that only it writes, with no lock and nothing made (<see cref="Add"/>), and
	/// compares fields to find what changed. The recorder's own thread turns the ring into text every 100 ms and writes it
	/// out, so the file holds everything up to the moment the program froze or was ended. Text from outside the program goes
	/// through <see cref="ShareSafe"/>; devices are D1, D2, and so on, and no key or button of a keyboard or a mouse is
	/// recorded.
	/// </para>
	/// </remarks>
	public sealed partial class ReportRecorder : IDisposable
	{
		/// <summary>Where recordings are saved.</summary>
		public static string Folder { get { return Path.Combine(EngineHelper.AppDataPath, "Recordings"); } }

		/// <summary>The longest a recording runs, so one left running cannot fill the disk.</summary>
		public static readonly TimeSpan MaxDuration = TimeSpan.FromMinutes(30);

		/// <summary>The largest a recording grows: below the 25 MB GitHub takes as an attachment.</summary>
		public const long MaxBytes = 20L * 1024 * 1024;

		/// <summary>The least an axis, slider or thumb moves before it is recorded again, of 65536: a resting stick records nothing.</summary>
		public const int AxisStep = 256;

		/// <summary>The least a trigger moves before it is recorded again, of 256.</summary>
		public const int TriggerStep = 2;

		/// <summary>A device call that takes longer is recorded with its time.</summary>
		public static readonly long SlowCallTicks = Stopwatch.Frequency / 50;

		/// <summary>How long the window or a device call may not answer before the recording says so.</summary>
		public static readonly long StallTicks = Stopwatch.Frequency;

		/// <param name="parts">What to record.</param>
		/// <param name="path">The file to write; made new.</param>
		/// <param name="displayScale">The window's display scale in percent, for the header.</param>
		public ReportRecorder(RecordParts parts, string path, int displayScale)
		{
			Parts = parts;
			FilePath = path;
			_displayScale = displayScale;
		}

		/// <summary>What this recording holds.</summary>
		public readonly RecordParts Parts;

		/// <summary>The file it is written to.</summary>
		public readonly string FilePath;

		readonly int _displayScale;

		/// <summary>True when the recording holds <paramref name="part"/>.</summary>
		public bool Has(RecordParts part)
		{
			return (Parts & part) != 0;
		}

		#region Engine side

		enum Kind : byte
		{
			Online,
			Input,
			InputAtStart,
			ReadFailures,
			RawInputMissing,
			Held,
			ForceState,
			ForceFailed,
			ForceSent,
			SlowCall,
			PadOutput,
			PadFeed,
			PadLive,
			GameForce,
			Text,
		}

		/// <summary>One change the engine handed over. Plain fields, so a write makes nothing.</summary>
		struct Entry
		{
			public long Ticks;
			public Kind Kind;
			public UserDevice Device;
			public int Index;
			public int A, B, C, D, E, F, G;
			public string Text;
		}

		/// <summary>Written by the engine only, read by the writer only: <see cref="_head"/> is the engine's, <see cref="_tail"/> the writer's.</summary>
		readonly Entry[] _ring = new Entry[1 << 16];
		int _head;
		int _tail;

		/// <summary>Changes that did not fit, because the writer fell behind. Written by the engine.</summary>
		int _dropped;

		void Add(ref Entry e)
		{
			var head = _head;
			var next = (head + 1) & (_ring.Length - 1);
			if (next == Volatile.Read(ref _tail))
			{
				_dropped++;
				return;
			}
			_ring[head] = e;
			Volatile.Write(ref _head, next);
		}

		void Add(long ticks, Kind kind, UserDevice device, int index, int a, int b = 0, int c = 0, int d = 0, int e = 0, int f = 0, int g = 0, string text = null)
		{
			var entry = new Entry { Ticks = ticks, Kind = kind, Device = device, Index = index, A = a, B = b, C = c, D = d, E = e, F = f, G = g, Text = text };
			Add(ref entry);
		}

		/// <summary>What was last recorded of a device, to tell what changed. Engine thread only.</summary>
		sealed class DeviceTrack
		{
			public readonly int[] Axis = new int[SourceState.MaxAxis];
			public readonly int[] Sliders = new int[SourceState.MaxSliders];
			public readonly int[] Povs = new int[4];
			public readonly bool[] Buttons = new bool[128];
			public bool StateSeen;
			public int Online = -1;
			public int ReadFailures;
			public bool RawInputMissing;
			public int Held = int.MinValue;
			public bool HasForce;
			public int ForceFault;
		}

		readonly Dictionary<UserDevice, DeviceTrack> _tracks = new Dictionary<UserDevice, DeviceTrack>();

		/// <summary>Records what changed on a device this pass. Engine thread only.</summary>
		public void Device(UserDevice ud, long now)
		{
			// Their keys and buttons are what a person types: never recorded.
			if (ud.IsKeyboard || ud.IsMouse)
				return;
			DeviceTrack t;
			if (!_tracks.TryGetValue(ud, out t))
			{
				t = new DeviceTrack();
				_tracks.Add(ud, t);
			}
			if (Has(RecordParts.Input))
			{
				// Caught here, at the engine's rate, so a device gone for a moment shows.
				var online = ud.IsOnline ? 1 : 0;
				if (online != t.Online)
				{
					if (t.Online >= 0)
						Add(now, Kind.Online, ud, 0, online);
					t.Online = online;
				}
				if (ud.DiReadFailures != t.ReadFailures)
				{
					t.ReadFailures = ud.DiReadFailures;
					Add(now, Kind.ReadFailures, ud, 0, t.ReadFailures);
				}
				if (ud.RawInputMissing != t.RawInputMissing)
				{
					t.RawInputMissing = ud.RawInputMissing;
					Add(now, Kind.RawInputMissing, ud, 0, t.RawInputMissing ? 1 : 0);
				}
				var state = ud.SourceState;
				if (state != null)
					DeviceState(ud, t, state, now);
			}
			if (Has(RecordParts.Forces))
			{
				var held = ud.IsExclusiveMode.HasValue ? (ud.IsExclusiveMode.Value ? 1 : 0) : -1;
				var heldKey = held * 2 + (ud.AutoCenter ? 1 : 0);
				if (heldKey != t.Held)
				{
					// A device never held, such as one with no force feedback, says nothing.
					var first = t.Held == int.MinValue;
					t.Held = heldKey;
					if (!first || held >= 0)
						Add(now, Kind.Held, ud, 0, held, ud.AutoCenter ? 1 : 0);
				}
				var hasForce = ud.FFState != null;
				if (hasForce != t.HasForce)
				{
					t.HasForce = hasForce;
					Add(now, Kind.ForceState, ud, 0, hasForce ? 1 : 0);
				}
				if (ud.ForceFault != t.ForceFault)
				{
					t.ForceFault = ud.ForceFault;
					Add(now, Kind.ForceFailed, ud, 0, ud.ForceFault, ud.ForceFailures);
				}
			}
		}

		void DeviceState(UserDevice ud, DeviceTrack t, SourceState state, long now)
		{
			var first = !t.StateSeen;
			t.StateSeen = true;
			var kind = first ? Kind.InputAtStart : Kind.Input;
			for (var i = 0; i < state.Buttons.Length && i < t.Buttons.Length; i++)
			{
				if (state.Buttons[i] == t.Buttons[i] && !(first && state.Buttons[i]))
					continue;
				t.Buttons[i] = state.Buttons[i];
				Add(now, kind, ud, i, (int)MapType.Button, state.Buttons[i] ? 1 : 0);
			}
			for (var i = 0; i < state.Povs.Length; i++)
			{
				if (state.Povs[i] == t.Povs[i] && !first)
					continue;
				t.Povs[i] = state.Povs[i];
				if (first && state.Povs[i] < 0 && i >= ud.CapPovCount)
					continue;
				Add(now, kind, ud, i, (int)MapType.POV, state.Povs[i]);
			}
			for (var i = 0; i < state.Axis.Length; i++)
			{
				if (!first && Math.Abs(state.Axis[i] - t.Axis[i]) < AxisStep)
					continue;
				t.Axis[i] = state.Axis[i];
				// At the start, only the axes the device has.
				if (first && (ud.DiAxeMask & (1 << i)) == 0 && state.Axis[i] == 0)
					continue;
				Add(now, kind, ud, i, (int)MapType.Axis, state.Axis[i]);
			}
			for (var i = 0; i < state.Sliders.Length; i++)
			{
				if (!first && Math.Abs(state.Sliders[i] - t.Sliders[i]) < AxisStep)
					continue;
				t.Sliders[i] = state.Sliders[i];
				if (first && (ud.DiSliderMask & (1 << i)) == 0 && state.Sliders[i] == 0)
					continue;
				Add(now, kind, ud, i, (int)MapType.Slider, state.Sliders[i]);
			}
		}

		/// <summary>Records the force sent to a device: each motor 0 to 255, as merged from its controllers. Engine thread only.</summary>
		public void Force(UserDevice ud, byte large, byte small)
		{
			if (Has(RecordParts.Forces))
				Add(Stopwatch.GetTimestamp(), Kind.ForceSent, ud, 0, large, small);
		}

		readonly Gamepad[] _padOutput = new Gamepad[4];
		readonly int[] _padConnected = { -1, -1, -1, -1 };
		readonly int[] _padFeed = { int.MinValue, int.MinValue, int.MinValue, int.MinValue };
		readonly Gamepad[] _padLive = new Gamepad[4];
		readonly int[] _padLiveConnected = { -1, -1, -1, -1 };
		readonly int[] _gameForce = { -1, -1, -1, -1 };

		/// <summary>Records what a controller sends to the game, when it changed. Engine thread only.</summary>
		/// <param name="pad">0 to 3.</param>
		/// <param name="connected">Whether any device is on the controller.</param>
		public void PadOutput(int pad, bool connected, Gamepad g, long now)
		{
			if (Moved(ref _padOutput[pad], ref _padConnected[pad], connected, g))
				AddPad(now, Kind.PadOutput, pad, connected, g);
		}

		/// <summary>Records what XInput reads back from the controller's place, when it changed. Engine thread only.</summary>
		public void PadLive(int pad, bool connected, Gamepad g, long now)
		{
			// An empty place at the start says nothing the controller's own line does not.
			if (_padLiveConnected[pad] < 0 && !connected)
			{
				_padLiveConnected[pad] = 0;
				return;
			}
			if (Moved(ref _padLive[pad], ref _padLiveConnected[pad], connected, g))
				AddPad(now, Kind.PadLive, pad, connected, g);
		}

		static bool Moved(ref Gamepad last, ref int lastConnected, bool connected, Gamepad g)
		{
			var c = connected ? 1 : 0;
			if (c == lastConnected && g.Buttons == last.Buttons
				&& Math.Abs(g.LeftTrigger - last.LeftTrigger) < TriggerStep && Math.Abs(g.RightTrigger - last.RightTrigger) < TriggerStep
				&& Math.Abs(g.LeftThumbX - last.LeftThumbX) < AxisStep && Math.Abs(g.LeftThumbY - last.LeftThumbY) < AxisStep
				&& Math.Abs(g.RightThumbX - last.RightThumbX) < AxisStep && Math.Abs(g.RightThumbY - last.RightThumbY) < AxisStep)
				return false;
			last = g;
			lastConnected = c;
			return true;
		}

		void AddPad(long now, Kind kind, int pad, bool connected, Gamepad g)
		{
			Add(now, kind, null, pad, connected ? 1 : 0, (int)g.Buttons, g.LeftTrigger, g.RightTrigger, g.LeftThumbX, g.LeftThumbY, (g.RightThumbX & 0xFFFF) | (g.RightThumbY << 16));
		}

		/// <summary>Records whether a controller is fed to its virtual controller, the error that stops it, and its XInput place. Engine thread only.</summary>
		/// <param name="feeding">1 fed, 0 not, -1 not known yet.</param>
		/// <param name="place">The XInput place, 0 to 3, or -1 for none.</param>
		public void PadFeed(int pad, int feeding, VirtualError error, int place, long now)
		{
			var key = (feeding + 1) | ((int)error << 2) | ((place + 1) << 10);
			if (key == _padFeed[pad])
				return;
			_padFeed[pad] = key;
			Add(now, Kind.PadFeed, null, pad, feeding, (int)error, place);
		}

		/// <summary>Records the vibration a controller's game asks for, each motor 0 to 255, when it changed. Engine thread only.</summary>
		public void GameForce(int pad, byte large, byte small, long now)
		{
			var key = large << 8 | small;
			if (key == _gameForce[pad])
				return;
			// No vibration at the start is the usual state, not news.
			if (_gameForce[pad] < 0 && key == 0)
			{
				_gameForce[pad] = 0;
				return;
			}
			_gameForce[pad] = key;
			Add(now, Kind.GameForce, null, pad, large, small);
		}

		/// <summary>Records the engine's line for the second just measured. Engine thread only.</summary>
		public void EngineLine(string line)
		{
			Add(Stopwatch.GetTimestamp(), Kind.Text, null, 0, 0, text: "engine " + line);
		}

		// The device call under way, for the writer to see one that does not return. Written by the engine only.
		UserDevice _callDevice;
		string _callName;
		long _callStart;

		/// <summary>
		/// Notes the device call the engine is about to make, by the name its error reports give it, and records the one before
		/// when it took long. Null ends the last one. Engine thread only.
		/// </summary>
		public void Calling(UserDevice ud, string call)
		{
			var now = Stopwatch.GetTimestamp();
			var was = _callName;
			if (was != null)
			{
				var took = now - _callStart;
				if (took > SlowCallTicks && Has(RecordParts.Engine))
					Add(now, Kind.SlowCall, _callDevice, 0, (int)Math.Min(int.MaxValue, took * 1000L / Stopwatch.Frequency), text: was);
			}
			_callDevice = ud;
			Volatile.Write(ref _callStart, now);
			Volatile.Write(ref _callName, call);
		}

		#endregion

		#region Other threads

		/// <summary>Lines from threads other than the engine's, a few a second at most: a worded line, or an error the writer words.</summary>
		readonly ConcurrentQueue<KeyValuePair<long, object>> _notes = new ConcurrentQueue<KeyValuePair<long, object>>();

		/// <summary>Adds a line from any thread. Text from outside the program goes through <see cref="ShareSafe"/> when written.</summary>
		public void Note(string text)
		{
			_notes.Enqueue(new KeyValuePair<long, object>(Stopwatch.GetTimestamp(), text));
		}

		/// <summary>The last time the interface thread answered, in timestamp ticks.</summary>
		long _uiBeat;
		string _windowState;

		/// <summary>Called by the interface thread a few times a second: says it answers, and how the window is shown.</summary>
		public void Beat(string windowState)
		{
			Volatile.Write(ref _uiBeat, Stopwatch.GetTimestamp());
			if (Has(RecordParts.Window) && windowState != _windowState)
			{
				if (_windowState != null)
					Note("window " + windowState);
				_windowState = windowState;
			}
		}

		#endregion

		#region Start and stop

		DInputHelper _engine;
		Thread _writer;
		volatile bool _stopAsked;
		readonly ManualResetEvent _wake = new ManualResetEvent(false);
		long _startTicks;
		DateTime _startTime;

		/// <summary>True once the recording has ended, by Stop, its time or size limit, or an error writing the file.</summary>
		public bool IsStopped { get { return _stopped; } }
		volatile bool _stopped;

		/// <summary>Why the recording ended early, or null.</summary>
		public string StopReason { get { return _stopReason; } }
		volatile string _stopReason;

		/// <summary>How long it has run.</summary>
		public TimeSpan Elapsed { get { return TimeSpan.FromSeconds((double)(Stopwatch.GetTimestamp() - _startTicks) / Stopwatch.Frequency); } }

		/// <summary>The bytes written so far.</summary>
		public long Bytes { get { return Interlocked.Read(ref _bytes); } }
		long _bytes;

		/// <summary>Starts recording from <paramref name="engine"/>, which may be null when no engine runs. Interface thread.</summary>
		public void Start(DInputHelper engine)
		{
			_startTicks = Stopwatch.GetTimestamp();
			_startTime = DateTime.UtcNow;
			_uiBeat = _startTicks;
			_engine = engine;
			Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
			_file = new StreamWriter(new FileStream(FilePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read), new UTF8Encoding(false));
			if (Has(RecordParts.Window))
				LogHelper.Current.WritingException += LogHelper_WritingException;
			if (Has(RecordParts.Settings))
				SettingsManager.Options.PropertyChanged += Options_PropertyChanged;
			_writer = new Thread(WriterAction) { IsBackground = true, Name = "ReportRecorder" };
			_writer.Start();
			if (engine != null)
				engine.Recording = this;
		}

		/// <summary>Ends the recording and closes the file. Waits a moment for the writer, never longer. Any thread.</summary>
		public void Stop()
		{
			Detach();
			_stopAsked = true;
			_wake.Set();
			if (_writer != null && _writer.IsAlive && Thread.CurrentThread != _writer)
				_writer.Join(3000);
		}

		/// <summary>Takes the recording off the engine and the events it listens to.</summary>
		void Detach()
		{
			var engine = _engine;
			if (engine != null && engine.Recording == this)
				engine.Recording = null;
			LogHelper.Current.WritingException -= LogHelper_WritingException;
			SettingsManager.Options.PropertyChanged -= Options_PropertyChanged;
		}

		public void Dispose()
		{
			Stop();
			_wake.Dispose();
		}

		void LogHelper_WritingException(object sender, LogHelperEventArgs e)
		{
			// Worded by the writer: the engine thread logs errors too, and must not wait while one is.
			if (e.Exception != null)
				_notes.Enqueue(new KeyValuePair<long, object>(Stopwatch.GetTimestamp(), e.Exception));
		}

		void Options_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			var value = OptionValue(e.PropertyName);
			if (value != null)
				Note("option " + e.PropertyName + " = " + value);
		}

		#endregion

		#region Writer

		StreamWriter _file;
		int _lines;

		void WriterAction()
		{
			try
			{
				WriteHeader();
				var nextSecond = 0L;
				var nextSample = 0L;
				while (true)
				{
					_wake.WaitOne(100);
					var now = Stopwatch.GetTimestamp();
					WriteDue();
					if (now >= nextSecond)
					{
						nextSecond = now + Stopwatch.Frequency;
						EverySecond(now);
					}
					if (now >= nextSample)
					{
						nextSample = now + Stopwatch.Frequency * 10;
						EveryTenSeconds();
					}
					_file.Flush();
					if (_stopAsked)
						break;
					if (Elapsed > MaxDuration)
					{
						_stopReason = "it reached " + (int)MaxDuration.TotalMinutes + " minutes";
						break;
					}
					if (Bytes > MaxBytes)
					{
						_stopReason = "it reached " + MaxBytes / 1024 / 1024 + " MB";
						break;
					}
				}
				Detach();
				// What a pass under way still handed over.
				Thread.Sleep(50);
				WriteDue();
				WriteLine(Stamp(Stopwatch.GetTimestamp()) + " recording ended" + (_stopReason == null ? "" : " because " + _stopReason)
					+ ": " + _lines.ToString("N0", CultureInfo.InvariantCulture) + " lines"
					+ (_dropped > 0 ? ", " + _dropped.ToString("N0", CultureInfo.InvariantCulture) + " changes lost because the file fell behind" : ""));
			}
			catch (Exception ex)
			{
				_stopReason = "the file could not be written: " + ShareSafe.Clean(ex.Message);
				Detach();
			}
			finally
			{
				try
				{
					if (_file != null)
						_file.Dispose();
				}
				catch (IOException) { }
				_stopped = true;
			}
		}

		/// <summary>Writes what the engine and the other threads handed over, oldest first.</summary>
		/// <remarks>The engine's ring and the notes are each in order; a note lands among the engine's lines by its time.</remarks>
		void WriteDue()
		{
			var head = Volatile.Read(ref _head);
			KeyValuePair<long, object> note;
			var hasNote = _notes.TryPeek(out note);
			while (_tail != head || hasNote)
			{
				if (hasNote && (_tail == head || note.Key <= _ring[_tail].Ticks))
				{
					_notes.TryDequeue(out note);
					var error = note.Value as Exception;
					var text = error == null ? (string)note.Value : "error " + DescribeException(error);
					WriteLine(Stamp(note.Key) + " " + ShareSafe.Clean(text, LabelOf));
					hasNote = _notes.TryPeek(out note);
					continue;
				}
				var e = _ring[_tail];
				_ring[_tail] = default(Entry);
				Volatile.Write(ref _tail, (_tail + 1) & (_ring.Length - 1));
				var line = Describe(ref e);
				if (line != null)
					WriteLine(Stamp(e.Ticks) + " " + line);
			}
		}

		void WriteLine(string line)
		{
			_file.WriteLine(line);
			_lines++;
			Interlocked.Add(ref _bytes, Encoding.UTF8.GetByteCount(line) + 2);
		}

		/// <summary>Seconds since the start, to the millisecond.</summary>
		string Stamp(long ticks)
		{
			var ms = (ticks - _startTicks) * 1000L / Stopwatch.Frequency;
			return (ms / 1000.0).ToString("0.000", CultureInfo.InvariantCulture);
		}

		static string ControlName(MapType type, int index)
		{
			return (type == MapType.POV ? "POV" : type.ToString()) + " " + (index + 1).ToString(CultureInfo.InvariantCulture);
		}

		static string Hat(int value)
		{
			return value < 0 ? "centred" : (value / 100).ToString(CultureInfo.InvariantCulture) + "°";
		}

		static string Pad(int index)
		{
			return "C" + (index + 1).ToString(CultureInfo.InvariantCulture);
		}

		string Describe(ref Entry e)
		{
			var d = e.Device == null ? null : Label(e.Device);
			switch (e.Kind)
			{
				case Kind.Online:
					return d + (e.A != 0 ? " online again" : " went offline");
				case Kind.Input:
				case Kind.InputAtStart:
					var type = (MapType)e.A;
					var value = type == MapType.Button ? (e.B != 0 ? "down" : "up") : type == MapType.POV ? Hat(e.B) : e.B.ToString(CultureInfo.InvariantCulture);
					return d + " " + ControlName(type, e.Index) + " " + value + (e.Kind == Kind.InputAtStart ? " (at start)" : "");
				case Kind.ReadFailures:
					return e.A == 0 ? d + " reads again" : d + " read failed " + e.A + " time" + (e.A == 1 ? "" : "s") + " in a row";
				case Kind.RawInputMissing:
					return e.A != 0 ? d + " is missing from Raw Input" : d + " is back in Raw Input";
				case Kind.Held:
					var held = e.A < 0 ? "let go" : e.A > 0 ? "held exclusively" : "held shared";
					return d + " " + held + (e.A > 0 ? ", its own centring " + (e.B != 0 ? "on" : "off") : "");
				case Kind.ForceState:
					return d + (e.A != 0 ? " force effects made" : " force effects stopped");
				case Kind.ForceFailed:
					return e.A == 0 ? d + " force works again" : d + " force failed with 0x" + e.A.ToString("X8", CultureInfo.InvariantCulture) + ", " + e.B + " time" + (e.B == 1 ? "" : "s");
				case Kind.ForceSent:
					return d + " force sent: large " + e.A + ", small " + e.B;
				case Kind.SlowCall:
					return (d ?? "engine") + " " + e.Text + " took " + e.A.ToString("N0", CultureInfo.InvariantCulture) + " ms";
				case Kind.PadOutput:
					return Pad(e.Index) + " out: " + DescribePad(ref e);
				case Kind.PadLive:
					return Pad(e.Index) + " XInput reads: " + DescribePad(ref e);
				case Kind.PadFeed:
					var feeding = e.A < 0 ? "not known yet" : e.A > 0 ? "fed" : "not fed";
					var error = (VirtualError)e.B == VirtualError.None ? "" : ", " + (VirtualError)e.B;
					var place = e.C < 0 ? ", no XInput place" : ", XInput place " + (e.C + 1);
					return Pad(e.Index) + " virtual controller " + feeding + error + place;
				case Kind.GameForce:
					return Pad(e.Index) + " game asks for vibration: large " + e.A + ", small " + e.B;
				case Kind.Text:
					return e.Text;
			}
			return null;
		}

		static string DescribePad(ref Entry e)
		{
			if (e.A == 0)
				return "no device";
			var buttons = (GamepadButtonFlags)e.B;
			var rx = (short)(e.G & 0xFFFF);
			var ry = (short)(e.G >> 16);
			return (buttons == GamepadButtonFlags.None ? "no buttons" : buttons.ToString().Replace(", ", "+"))
				+ ", LT " + e.C + ", RT " + e.D
				+ ", LX " + (short)e.E + ", LY " + (short)e.F + ", RX " + rx + ", RY " + ry;
		}

		/// <summary>The engine's call under way when it ran long, and whether the window answers. Writer thread.</summary>
		long _reportedCallStart;
		long _windowStalledSince;
		int _lastPassCount = -1;
		long _passStalledSince;

		void EverySecond(long now)
		{
			if (Has(RecordParts.Engine))
			{
				var name = Volatile.Read(ref _callName);
				var started = Volatile.Read(ref _callStart);
				if (name != null && started != _reportedCallStart && now - started > StallTicks && Volatile.Read(ref _callName) == name)
				{
					_reportedCallStart = started;
					var device = _callDevice;
					Note((device == null ? "engine" : Label(device)) + " has been in " + name + " for " + Seconds(now - started) + " s");
				}
				var engine = _engine;
				if (engine != null)
				{
					var passes = engine.PassCount;
					if (passes != _lastPassCount)
					{
						if (_passStalledSince != 0)
							Note("engine runs again after " + Seconds(now - _passStalledSince) + " s without a pass");
						_passStalledSince = 0;
						_lastPassCount = passes;
					}
					else if (_passStalledSince == 0 && !engine.Suspended)
					{
						_passStalledSince = now - Stopwatch.Frequency;
						Note("engine has run no pass for 1 s");
					}
				}
			}
			if (Has(RecordParts.Window))
			{
				var beat = Volatile.Read(ref _uiBeat);
				if (now - beat > StallTicks)
				{
					if (_windowStalledSince == 0)
					{
						_windowStalledSince = beat;
						Note("window stopped answering " + Seconds(now - beat) + " s ago");
					}
				}
				else if (_windowStalledSince != 0)
				{
					Note("window answers again after " + Seconds(beat - _windowStalledSince) + " s");
					_windowStalledSince = 0;
				}
				SampleForeground();
			}
			if (Has(RecordParts.Settings) || Has(RecordParts.Input))
				SampleDevices();
			if (Has(RecordParts.Settings))
				SampleSettings();
			WriteDue();
		}

		static string Seconds(long ticks)
		{
			return ((double)ticks / Stopwatch.Frequency).ToString("0.0", CultureInfo.InvariantCulture);
		}

		#endregion
	}
}
