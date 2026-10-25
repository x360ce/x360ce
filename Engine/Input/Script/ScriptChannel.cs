using System;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using x360ce.Engine.Data;

namespace x360ce.Engine
{
	/// <summary>Shares the routed devices' states with an AutoHotkey script and applies what the script sends back.</summary>
	/// <remarks>
	/// <para>
	/// The script and the engine meet in a block of shared memory named <see cref="Name"/>, laid out by the constants
	/// below; <c>x360ce.ahk</c> reads and writes it with the same offsets. Every pass the engine writes each scripted
	/// device's state into its slot, lists the changes the script watches in a ring of events, and posts the script's
	/// window a message when there is something new. The script writes commands into a second ring: press or let go of a
	/// button, hold a hat, an axis or a slider at a value, or let go of it, each after a delay.
	/// </para>
	/// <para>
	/// The engine never waits for the script. Measured with AutoHotkey 2.0.18, an idle script answers a message in
	/// about 50 microseconds, so what it sends is applied on the next pass; but a script running other code reads its
	/// messages only every 15.6 ms, and one in a Critical section every 31 ms. A pass that waited would take that long.
	/// So commands are taken as they come, and the delays in them are played on the engine's own clock.
	/// </para>
	/// <para>
	/// The script beats every <see cref="HeartbeatMilliseconds"/>, which the engine writes for it. A script whose process
	/// ends, closed, killed or crashed, loses everything it held on the next pass; one that stops beating for
	/// <see cref="LeaseMilliseconds"/>, hung, loses it then. Each device then reaches its controller as it is.
	/// </para>
	/// <para>
	/// Made on the engine thread and used only there. A pass allocates nothing and takes no lock.
	/// </para>
	/// </remarks>
	public sealed unsafe class ScriptChannel : IDisposable
	{
		#region Layout shared with x360ce.ahk

		public const string Name = @"Local\X360CE.Script";
		public const int Magic = 0x43533358;
		public const int Version = 1;
		public const int Size = 16384;
		public const int MaxSlots = 16;
		public const int RingLength = 256;
		/// <summary>The message posted to the script's window when there is something new: WM_APP + 0x360.</summary>
		public const uint MessageId = 0x8360;
		/// <summary>A command value that lets go of a hat, an axis or a slider.</summary>
		public const int ReleaseValue = int.MinValue;
		/// <summary>How long x360ce waits for a heartbeat before it takes the script for hung, unless set: long enough for a script busy in a Critical section or a slow file read.</summary>
		public const int DefaultLeaseMilliseconds = 250;
		/// <summary>How often the script beats, unless set: AutoHotkey's timers step every 15.6 ms, so this is as often as they go.</summary>
		public const int DefaultHeartbeatMilliseconds = 15;

		public const int OffMagic = 0;
		public const int OffVersion = 4;
		public const int OffProcessId = 8;
		public const int OffTableVersion = 12;
		public const int OffScriptWindow = 16;
		public const int OffHeartbeat = 24;
		public const int OffPending = 28;
		public const int OffEventsWritten = 32;
		public const int OffEventsRead = 36;
		public const int OffCommandsWritten = 40;
		public const int OffCommandsRead = 44;
		public const int OffEventsDropped = 48;
		public const int OffCommandsDropped = 52;
		public const int OffConnected = 56;
		public const int OffHeartbeatInterval = 60;

		public const int OffSlots = 256;
		public const int SlotSize = 512;
		public const int OffEvents = 8448;
		public const int EventSize = 8;
		public const int OffCommands = 10496;
		public const int CommandSize = 16;

		public const int SlotInUse = 0;
		/// <summary>The device's number on each controller, a byte each, the lowest for Controller 1: c1.d2 is the device whose lowest byte is 2.</summary>
		public const int SlotNumbers = 4;
		public const int SlotGuid = 8;
		public const int SlotMoves = 24;
		public const int SlotWatchButtons = 32;
		public const int SlotWatchPovs = 48;
		public const int SlotWatchAxes = 52;
		public const int SlotWatchSliders = 56;
		public const int SlotOwnButtons = 64;
		public const int SlotButtons = 96;
		public const int SlotPovs = 112;
		public const int SlotAxes = 128;
		public const int SlotSliders = 224;
		public const int SlotName = 256;
		public const int NameLength = 64;

		public const int KindButton = 1;
		public const int KindPov = 2;
		public const int KindAxis = 3;
		public const int KindSlider = 4;
		public const int FlagFromNow = 1;

		/// <summary>How far a watched axis or slider must move, out of 65535, before the script is told: a 256th of its travel.</summary>
		public const int MoveStep = 256;

		#endregion

		readonly MemoryMappedFile _map;
		readonly MemoryMappedViewAccessor _view;
		readonly byte* _p;
		long _leaseTicks;
		int _leaseMilliseconds;
		readonly long _nudgeTicks;

		/// <summary>How long x360ce waits for a heartbeat before it takes the script for hung, in milliseconds.</summary>
		/// <remarks>Set on every pass from the option; only a change does any work.</remarks>
		public int LeaseMilliseconds
		{
			get { return _leaseMilliseconds; }
			set
			{
				if (value == _leaseMilliseconds)
					return;
				_leaseMilliseconds = value;
				_leaseTicks = Stopwatch.Frequency * value / 1000;
			}
		}

		/// <summary>How often the script beats, in milliseconds, written for the library, which times its heartbeat by it.</summary>
		/// <remarks>Set on every pass from the option; only a change is written.</remarks>
		public int HeartbeatMilliseconds
		{
			get { return Int(OffHeartbeatInterval); }
			set
			{
				if (value != Int(OffHeartbeatInterval))
					SetInt(OffHeartbeatInterval, value);
			}
		}

		/// <summary>Called with the script's window when it has something new to read. Posts <see cref="MessageId"/> to it unless replaced.</summary>
		public Action<IntPtr> Nudge;

		public ScriptChannel() : this(Name) { }

		/// <param name="name">The shared memory's name; tests give their own, so they never meet a running x360ce.</param>
		public ScriptChannel(string name)
		{
			_map = MemoryMappedFile.CreateOrOpen(name, Size);
			_view = _map.CreateViewAccessor(0, Size);
			byte* p = null;
			_view.SafeMemoryMappedViewHandle.AcquirePointer(ref p);
			_p = p + _view.PointerOffset;
			LeaseMilliseconds = DefaultLeaseMilliseconds;
			_nudgeTicks = Stopwatch.Frequency / 50;
			Nudge = PostToScript;
			for (var s = 0; s < MaxSlots; s++)
			{
				_press[s] = new bool[128];
				_pov[s] = new int[4];
				_axis[s] = new int[SourceState.MaxAxis];
				_slider[s] = new int[SourceState.MaxSliders];
				_buttonStamp[s] = new int[128];
				_povStamp[s] = new int[4];
				_axisStamp[s] = new int[SourceState.MaxAxis];
				_sliderStamp[s] = new int[SourceState.MaxSliders];
				_lastButtons[s] = new int[4];
				_lastPovs[s] = new int[4];
				_lastAxis[s] = new int[SourceState.MaxAxis];
				_lastSliders[s] = new int[SourceState.MaxSliders];
				_states[s] = new SourceState();
			}
			Begin();
		}

		/// <summary>Writes the engine's part of the header, keeping what a script that already holds the memory wrote.</summary>
		/// <remarks>Events the script never read and commands it queued while no engine ran are dropped: they belong to a run that ended.</remarks>
		void Begin()
		{
			if (Int(OffMagic) != Magic || Int(OffVersion) != Version)
			{
				for (var i = 0; i < Size; i += 4)
					*(int*)(_p + i) = 0;
				SetInt(OffMagic, Magic);
				SetInt(OffVersion, Version);
			}
			SetInt(OffProcessId, Process.GetCurrentProcess().Id);
			SetInt(OffHeartbeatInterval, DefaultHeartbeatMilliseconds);
			_eventsWritten = Int(OffEventsRead);
			SetInt(OffEventsWritten, _eventsWritten);
			_commandsRead = Int(OffCommandsWritten);
			SetInt(OffCommandsRead, _commandsRead);
			SetInt(OffPending, 0);
			SetInt(OffConnected, 0);
			for (var s = 0; s < MaxSlots; s++)
				ClearSlot(s);
			SetInt(OffTableVersion, Int(OffTableVersion) + 1);
		}

		#region Memory

		int Int(int offset) { return Volatile.Read(ref *(int*)(_p + offset)); }
		void SetInt(int offset, int value) { Volatile.Write(ref *(int*)(_p + offset), value); }
		int SlotInt(int slot, int offset) { return Int(OffSlots + slot * SlotSize + offset); }
		void SetSlotInt(int slot, int offset, int value) { SetInt(OffSlots + slot * SlotSize + offset, value); }

		#endregion

		#region Devices

		readonly Guid[] _slotGuids = new Guid[MaxSlots];
		readonly bool[] _slotUsed = new bool[MaxSlots];
		readonly bool[] _slotKept = new bool[MaxSlots];

		/// <summary>Gives each routed device a slot, keeping the slot of a device routed before, and frees the rest.</summary>
		/// <remarks>Called when the routing changes, not every pass. A device past the sixteenth gets no slot and is not scripted.</remarks>
		/// <param name="devices">The routed devices.</param>
		/// <param name="numbers">For each device, its number on each controller, a byte each, the lowest for Controller 1; 0 where it is not on one.</param>
		/// <param name="slots">Filled with each device's slot, or -1.</param>
		public void Route(UserDevice[] devices, int[] numbers, int[] slots)
		{
			Array.Clear(_slotKept, 0, MaxSlots);
			for (var i = 0; i < devices.Length; i++)
			{
				slots[i] = -1;
				for (var s = 0; s < MaxSlots; s++)
				{
					if (_slotUsed[s] && !_slotKept[s] && _slotGuids[s] == devices[i].InstanceGuid)
					{
						slots[i] = s;
						_slotKept[s] = true;
						break;
					}
				}
			}
			for (var s = 0; s < MaxSlots; s++)
			{
				if (_slotUsed[s] && !_slotKept[s])
					ClearSlot(s);
			}
			for (var i = 0; i < devices.Length; i++)
			{
				var s = slots[i];
				if (s < 0)
				{
					for (var f = 0; f < MaxSlots; f++)
					{
						if (!_slotUsed[f])
						{
							s = f;
							break;
						}
					}
					if (s < 0)
						continue;
					slots[i] = s;
					_slotUsed[s] = true;
					_slotGuids[s] = devices[i].InstanceGuid;
					var guid = devices[i].InstanceGuid.ToByteArray();
					Marshal.Copy(guid, 0, (IntPtr)(_p + OffSlots + s * SlotSize + SlotGuid), guid.Length);
					WriteName(s, devices[i].ProductName ?? devices[i].InstanceName);
					SetSlotInt(s, SlotInUse, 1);
				}
				SetSlotInt(s, SlotNumbers, numbers[i]);
			}
			SetInt(OffTableVersion, Int(OffTableVersion) + 1);
		}

		void WriteName(int slot, string name)
		{
			var at = (char*)(_p + OffSlots + slot * SlotSize + SlotName);
			var n = name == null ? 0 : Math.Min(name.Length, NameLength - 1);
			for (var i = 0; i < n; i++)
				at[i] = name[i];
			for (var i = n; i < NameLength; i++)
				at[i] = '\0';
		}

		/// <summary>Frees a slot: nothing the script held on it stays, and nothing it watched or owned there carries over to the next device.</summary>
		/// <remarks>
		/// Until the first pass writes the device's state, the slot reads as a device at rest: its hats at -1 and its axes
		/// centred. 0 is a hat pushed up and an axis at one end, which a script would read as a move nobody made.
		/// </remarks>
		void ClearSlot(int slot)
		{
			var at = _p + OffSlots + slot * SlotSize;
			for (var i = 0; i < SlotSize; i += 4)
				*(int*)(at + i) = 0;
			for (var i = 0; i < 4; i++)
			{
				*(int*)(at + SlotPovs + i * 4) = -1;
				_lastPovs[slot][i] = -1;
			}
			for (var i = 0; i < SourceState.MaxAxis; i++)
				*(int*)(at + SlotAxes + i * 4) = MapExpressionUnits.RawCentre;
			_slotUsed[slot] = false;
			_slotGuids[slot] = Guid.Empty;
			Array.Clear(_lastButtons[slot], 0, 4);
			Array.Clear(_lastAxis[slot], 0, SourceState.MaxAxis);
			Array.Clear(_lastSliders[slot], 0, SourceState.MaxSliders);
			ClearOverlay(slot);
		}

		#endregion

		#region Lease

		int _lastBeat;
		long _lastBeatAt;
		/// <summary>The heartbeat was read at least once, and changed at least once since: a value a script that ended left behind is no beat.</summary>
		bool _beatSeen;
		bool _beatChanged;

		/// <summary>True while a script beats: what it sends is applied, and it is told about what it watches.</summary>
		public bool Connected { get; private set; }

		/// <summary>The script's window, or zero while none has said which it is.</summary>
		IntPtr ScriptWindow { get { return (IntPtr)Volatile.Read(ref *(long*)(_p + OffScriptWindow)); } }

		/// <summary>The script window whose process is watched, the process, and whether it has ended.</summary>
		IntPtr _watchedWindow;
		Process _scriptProcess;
		volatile bool _scriptExited;

		void CheckLease(long now)
		{
			var beat = Int(OffHeartbeat);
			if (!_beatSeen)
			{
				_beatSeen = true;
				_lastBeat = beat;
			}
			else if (beat != _lastBeat)
			{
				_lastBeat = beat;
				_lastBeatAt = now;
				_beatChanged = true;
			}
			var window = ScriptWindow;
			if (window != _watchedWindow)
				WatchScript(window);
			var alive = _beatChanged && window != IntPtr.Zero && !_scriptExited && now - _lastBeatAt < _leaseTicks;
			if (alive == Connected)
				return;
			if (!alive)
				Disconnect();
			Connected = alive;
			SetInt(OffConnected, alive ? 1 : 0);
			// A script that comes back registers again: its watched and owned controls were cleared when it went.
			SetInt(OffTableVersion, Int(OffTableVersion) + 1);
		}

		/// <summary>Watches the process of a new script window, so a script that closes or crashes is let go of on the next pass, not when its heartbeat stops.</summary>
		/// <remarks>
		/// Called when the window changes, once per script, so what it makes is made once. A process x360ce may not watch,
		/// a script run as administrator by an x360ce that is not, is left to the heartbeat, which also catches a script
		/// that hangs.
		/// </remarks>
		void WatchScript(IntPtr window)
		{
			StopWatching();
			_watchedWindow = window;
			uint id;
			if (window == IntPtr.Zero || GetWindowThreadProcessId(window, out id) == 0)
				return;
			try
			{
				var process = Process.GetProcessById((int)id);
				process.EnableRaisingEvents = true;
				process.Exited += ScriptProcess_Exited;
				_scriptProcess = process;
				_scriptExited = process.HasExited;
			}
			catch (ArgumentException)
			{
				// Ended between giving its window and being looked up.
				_scriptExited = true;
			}
			catch (System.ComponentModel.Win32Exception)
			{
				// Not allowed to watch it: the heartbeat decides.
			}
		}

		void ScriptProcess_Exited(object sender, EventArgs e)
		{
			if (sender == _scriptProcess)
				_scriptExited = true;
		}

		void StopWatching()
		{
			var process = _scriptProcess;
			_scriptProcess = null;
			_scriptExited = false;
			if (process == null)
				return;
			process.Exited -= ScriptProcess_Exited;
			process.Dispose();
		}

		/// <summary>Drops everything a script that stopped beating held, so no button stays pressed and no axis stays put.</summary>
		void Disconnect()
		{
			for (var s = 0; s < MaxSlots; s++)
			{
				ClearOverlay(s);
				var at = _p + OffSlots + s * SlotSize;
				for (var i = SlotWatchButtons; i < SlotButtons; i += 4)
					*(int*)(at + i) = 0;
			}
			_queued = 0;
			_commandsRead = Int(OffCommandsWritten);
			SetInt(OffCommandsRead, _commandsRead);
			SetInt(OffPending, 0);
		}

		#endregion

		#region Commands

		int _commandsRead;
		int _commandsDropped;
		int _pass;
		long _lastDue;
		readonly int[] _queueCode = new int[RingLength];
		readonly int[] _queueValue = new int[RingLength];
		readonly long[] _queueDue = new long[RingLength];
		int _queued;

		/// <summary>What the script sends: buttons pressed, and hats, axes and sliders held, by slot. <see cref="ReleaseValue"/> where the device's own value goes through.</summary>
		readonly bool[][] _press = new bool[MaxSlots][];
		readonly int[][] _pov = new int[MaxSlots][];
		readonly int[][] _axis = new int[MaxSlots][];
		readonly int[][] _slider = new int[MaxSlots][];
		/// <summary>How many of a slot's controls the script presses or holds now.</summary>
		readonly int[] _held = new int[MaxSlots];
		/// <summary>The pass that last changed or held back each control, so one control changes at most once a pass.</summary>
		readonly int[][] _buttonStamp = new int[MaxSlots][];
		readonly int[][] _povStamp = new int[MaxSlots][];
		readonly int[][] _axisStamp = new int[MaxSlots][];
		readonly int[][] _sliderStamp = new int[MaxSlots][];

		void ClearOverlay(int slot)
		{
			Array.Clear(_press[slot], 0, 128);
			for (var i = 0; i < 4; i++)
				_pov[slot][i] = ReleaseValue;
			for (var i = 0; i < SourceState.MaxAxis; i++)
				_axis[slot][i] = ReleaseValue;
			for (var i = 0; i < SourceState.MaxSliders; i++)
				_slider[slot][i] = ReleaseValue;
			_held[slot] = 0;
		}

		/// <summary>Starts a pass: checks the lease, takes in the commands the script wrote, and applies those due.</summary>
		/// <param name="now">The engine's clock, <see cref="Stopwatch.GetTimestamp"/>.</param>
		/// <returns>True while a script is connected.</returns>
		public bool BeginPass(long now)
		{
			_pass++;
			CheckLease(now);
			if (!Connected)
				return false;
			TakeCommands(now);
			ApplyDue(now);
			return true;
		}

		void TakeCommands(long now)
		{
			var written = Int(OffCommandsWritten);
			var read = _commandsRead;
			while (read != written)
			{
				var c = _p + OffCommands + (read & (RingLength - 1)) * CommandSize;
				var code = *(int*)c;
				var value = *(int*)(c + 4);
				var delay = *(int*)(c + 8);
				var flags = *(int*)(c + 12);
				read++;
				if (_queued == RingLength || !IsValid(code) || delay < 0)
				{
					_commandsDropped++;
					continue;
				}
				var from = (flags & FlagFromNow) != 0 ? now : _lastDue;
				var due = from + delay * Stopwatch.Frequency / 1000;
				_lastDue = due;
				_queueCode[_queued] = code;
				_queueValue[_queued] = value;
				_queueDue[_queued] = due;
				_queued++;
			}
			_commandsRead = read;
			SetInt(OffCommandsRead, read);
			SetInt(OffCommandsDropped, _commandsDropped);
		}

		static bool IsValid(int code)
		{
			var slot = (code >> 24) & 0xFF;
			var kind = (code >> 16) & 0xFF;
			var index = code & 0xFFFF;
			if (slot >= MaxSlots)
				return false;
			switch (kind)
			{
				case KindButton: return index < 128;
				case KindPov: return index < 4;
				case KindAxis: return index < SourceState.MaxAxis;
				case KindSlider: return index < SourceState.MaxSliders;
				default: return false;
			}
		}

		/// <summary>Applies the commands that are due, in the order they came, one change per control per pass.</summary>
		/// <remarks>
		/// A command that is not due yet, or whose control already changed this pass, holds back every later command for
		/// that control: a press and a release sent together land on two passes, and a sequence plays in its order.
		/// </remarks>
		void ApplyDue(long now)
		{
			var kept = 0;
			for (var q = 0; q < _queued; q++)
			{
				var code = _queueCode[q];
				var slot = (code >> 24) & 0xFF;
				var kind = (code >> 16) & 0xFF;
				var index = code & 0xFFFF;
				var stamp = Stamps(kind, slot);
				var apply = _queueDue[q] <= now && stamp[index] != _pass;
				stamp[index] = _pass;
				if (apply && _slotUsed[slot])
				{
					Set(slot, kind, index, _queueValue[q]);
					continue;
				}
				if (!apply)
				{
					_queueCode[kept] = code;
					_queueValue[kept] = _queueValue[q];
					_queueDue[kept] = _queueDue[q];
					kept++;
				}
			}
			_queued = kept;
		}

		int[] Stamps(int kind, int slot)
		{
			switch (kind)
			{
				case KindButton: return _buttonStamp[slot];
				case KindPov: return _povStamp[slot];
				case KindAxis: return _axisStamp[slot];
				default: return _sliderStamp[slot];
			}
		}

		void Set(int slot, int kind, int index, int value)
		{
			if (kind == KindButton)
			{
				var press = value != 0;
				if (_press[slot][index] != press)
					_held[slot] += press ? 1 : -1;
				_press[slot][index] = press;
				return;
			}
			var values = kind == KindPov ? _pov[slot] : kind == KindAxis ? _axis[slot] : _slider[slot];
			if (kind != KindPov && value != ReleaseValue)
				value = Math.Max(0, Math.Min(65535, value));
			var was = values[index] != ReleaseValue;
			var now = value != ReleaseValue;
			if (was != now)
				_held[slot] += now ? 1 : -1;
			values[index] = value;
		}

		#endregion

		#region State and events

		int _eventsWritten;
		int _eventsDropped;
		bool _news;
		long _lastNudge;
		readonly int[][] _lastButtons = new int[MaxSlots][];
		readonly int[][] _lastPovs = new int[MaxSlots][];
		readonly int[][] _lastAxis = new int[MaxSlots][];
		readonly int[][] _lastSliders = new int[MaxSlots][];
		readonly SourceState[] _states = new SourceState[MaxSlots];

		/// <summary>Publishes a device's state to the script and returns what its controller's mapping reads instead, or null when the script changes nothing on it.</summary>
		/// <param name="slot">The device's slot from <see cref="Route"/>; -1 for none.</param>
		/// <param name="state">The state read from the device this pass. Not changed.</param>
		public SourceState Apply(int slot, SourceState state)
		{
			if (!Connected || slot < 0 || !_slotUsed[slot])
				return null;
			Publish(slot, state);
			var at = _p + OffSlots + slot * SlotSize + SlotOwnButtons;
			var own0 = *(int*)at;
			var own1 = *(int*)(at + 4);
			var own2 = *(int*)(at + 8);
			var own3 = *(int*)(at + 12);
			if (_held[slot] == 0 && (own0 | own1 | own2 | own3) == 0)
				return null;
			var result = _states[slot];
			state.CopyTo(result);
			var press = _press[slot];
			for (var b = 0; b < 128; b++)
			{
				var word = b < 32 ? own0 : b < 64 ? own1 : b < 96 ? own2 : own3;
				var owned = (word & (1 << (b & 31))) != 0;
				result.Buttons[b] = owned ? press[b] : result.Buttons[b] | press[b];
			}
			Hold(_pov[slot], result.Povs);
			Hold(_axis[slot], result.Axis);
			Hold(_slider[slot], result.Sliders);
			return result;
		}

		static void Hold(int[] values, int[] target)
		{
			for (var i = 0; i < values.Length; i++)
			{
				if (values[i] != ReleaseValue)
					target[i] = values[i];
			}
		}

		/// <summary>Writes a device's state into its slot and lists the changes the script watches.</summary>
		void Publish(int slot, SourceState state)
		{
			var at = _p + OffSlots + slot * SlotSize;
			var last = _lastButtons[slot];
			for (var w = 0; w < 4; w++)
			{
				var bits = 0;
				for (var b = 0; b < 32; b++)
				{
					if (state.Buttons[w * 32 + b])
						bits |= 1 << b;
				}
				*(int*)(at + SlotButtons + w * 4) = bits;
				var changed = (bits ^ last[w]) & *(int*)(at + SlotWatchButtons + w * 4);
				last[w] = bits;
				for (var b = 0; changed != 0; b++, changed = (int)((uint)changed >> 1))
				{
					if ((changed & 1) != 0)
						AddEvent(slot, KindButton, w * 32 + b, (bits >> b) & 1);
				}
			}
			var watchPovs = *(int*)(at + SlotWatchPovs);
			var lastPovs = _lastPovs[slot];
			for (var i = 0; i < 4; i++)
			{
				var value = state.Povs[i];
				*(int*)(at + SlotPovs + i * 4) = value;
				if (value != lastPovs[i] && (watchPovs & (1 << i)) != 0)
					AddEvent(slot, KindPov, i, value);
				lastPovs[i] = value;
			}
			var moved = PublishValues(state.Axis, at + SlotAxes, _lastAxis[slot], *(int*)(at + SlotWatchAxes));
			moved |= PublishValues(state.Sliders, at + SlotSliders, _lastSliders[slot], *(int*)(at + SlotWatchSliders));
			if (moved)
			{
				SetSlotInt(slot, SlotMoves, SlotInt(slot, SlotMoves) + 1);
				_news = true;
			}
		}

		static bool PublishValues(int[] values, byte* at, int[] last, int watched)
		{
			var moved = false;
			for (var i = 0; i < values.Length; i++)
			{
				var value = values[i];
				*(int*)(at + i * 4) = value;
				if ((watched & (1 << i)) != 0 && Math.Abs(value - last[i]) >= MoveStep)
				{
					last[i] = value;
					moved = true;
				}
			}
			return moved;
		}

		void AddEvent(int slot, int kind, int index, int value)
		{
			if (_eventsWritten - Int(OffEventsRead) >= RingLength)
			{
				_eventsDropped++;
				SetInt(OffEventsDropped, _eventsDropped);
				return;
			}
			var e = _p + OffEvents + (_eventsWritten & (RingLength - 1)) * EventSize;
			*(int*)e = slot << 24 | kind << 16 | index;
			*(int*)(e + 4) = value;
			_eventsWritten++;
			SetInt(OffEventsWritten, _eventsWritten);
			_news = true;
		}

		/// <summary>Ends a pass: tells the script when there is something new for it.</summary>
		/// <remarks>
		/// One message is in flight at a time: the script clears <see cref="OffPending"/> when it starts reading. News is kept
		/// until a message carries it: the script may clear the flag just as this pass reads it and still see it set, and a
		/// move told no one would wait for the next move. News while a message is in flight, and events left unread, are told
		/// again after 20 ms: AutoHotkey drops a message that comes while the callback before it still runs.
		/// </remarks>
		public void EndPass(long now)
		{
			if (!Connected)
				return;
			var quiet = now - _lastNudge > _nudgeTicks;
			var due = _news
				? Int(OffPending) == 0 || quiet
				: quiet && _eventsWritten != Int(OffEventsRead);
			if (!due)
				return;
			_news = false;
			SetInt(OffPending, 1);
			_lastNudge = now;
			Nudge(ScriptWindow);
		}

		#endregion

		[DllImport("user32.dll")]
		static extern bool PostMessageW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

		static void PostToScript(IntPtr window)
		{
			PostMessageW(window, MessageId, IntPtr.Zero, IntPtr.Zero);
		}

		public void Dispose()
		{
			StopWatching();
			SetInt(OffConnected, 0);
			_view.SafeMemoryMappedViewHandle.ReleasePointer();
			_view.Dispose();
			_map.Dispose();
		}
	}
}
