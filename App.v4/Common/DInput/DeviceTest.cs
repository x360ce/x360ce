using System;
using System.Threading;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.DInput
{
	/// <summary>A test of one device, which the Test Device window starts: the engine reads only that device, as fast as it can, and records each read that changed something here, with its values and buttons.</summary>
	/// <remarks>
	/// <para>
	/// Set on <see cref="DInputHelper.Testing"/>, it pauses the engine's normal work: no other device is read, nothing is
	/// converted, and the virtual controllers are sent a neutral state once and nothing after. Taking it off resumes everything.
	/// </para>
	/// <para>
	/// The engine writes and the window reads, each from its own end of two rings reserved here once: the reads that changed,
	/// with their time and every value, and the change buffer's entries with their sequence numbers. A write makes nothing and
	/// waits for nothing: when the window has not taken enough out, the read is dropped and counted.
	/// </para>
	/// </remarks>
	public sealed class DeviceTest
	{
		/// <summary>Reads that changed, kept until the window takes them: a second and a half at 10,000 reports a second.</summary>
		public const int Capacity = 16384;

		/// <summary>Change buffer entries, kept until the window takes them.</summary>
		public const int EntryCapacity = 65536;

		/// <summary>The values each read keeps: the axes, then the sliders.</summary>
		public const int Channels = SourceState.MaxAxis + SourceState.MaxSliders;

		/// <param name="device">The device to read.</param>
		public DeviceTest(UserDevice device)
		{
			Device = device;
			// A game of its own, with no emulation, mapping only this device: the engine reads it as it reads a mapped one.
			Game = new UserGame { FileName = "x360ce-device-test", EmulationType = (int)EmulationType.None, EnableMask = (int)MapToMask.Controller1 };
			var row = new UserSetting { InstanceGuid = device.InstanceGuid, FileName = Game.FileName, MapTo = (int)MapTo.Controller1, IsEnabled = true };
			Routing = DeviceRouting.Build(Game, new[] { row }, new PadSetting[0], new[] { device });
		}

		/// <summary>The device read.</summary>
		public UserDevice Device { get; }

		/// <summary>The game and routing the engine reads the device with.</summary>
		internal UserGame Game { get; }
		internal DeviceRouting Routing { get; }

		#region Written by the engine

		/// <summary>The buttons a read keeps, 64 to a word.</summary>
		const int ButtonWords = 2;

		readonly long[] _ticks = new long[Capacity];
		readonly int[] _values = new int[Capacity * Channels];
		readonly ulong[] _buttons = new ulong[Capacity * ButtonWords];
		long _written;
		long _taken;

		readonly long[] _entryTicks = new long[EntryCapacity];
		readonly int[] _entryOffsets = new int[EntryCapacity];
		readonly int[] _entrySequences = new int[EntryCapacity];
		long _entriesWritten;
		long _entriesTaken;

		/// <summary>The last state recorded, to tell a read that changed from one that did not. Engine only.</summary>
		readonly SourceState _last = new SourceState();
		bool _recorded;

		long _reads;
		long _dropped;
		long _droppedEntries;
		int _virtualNeutral;
		int _offline;
		int _hasChangeBuffer;

		/// <summary>Reads of the device, changed or not.</summary>
		public long Reads { get { return Interlocked.Read(ref _reads); } }

		/// <summary>Reads that changed but found no room, because the window took too few out.</summary>
		public long Dropped { get { return Interlocked.Read(ref _dropped); } }

		/// <summary>Change buffer entries that found no room.</summary>
		public long DroppedEntries { get { return Interlocked.Read(ref _droppedEntries); } }

		/// <summary>Whether the virtual controllers were sent a neutral state.</summary>
		public bool VirtualNeutral { get { return Volatile.Read(ref _virtualNeutral) != 0; } }

		/// <summary>Whether the device was found unplugged.</summary>
		public bool Offline { get { return Volatile.Read(ref _offline) != 0; } }

		/// <summary>Whether the device keeps a change buffer, whose entries carry sequence numbers.</summary>
		public bool HasChangeBuffer { get { return Volatile.Read(ref _hasChangeBuffer) != 0; } }

		/// <summary>Whether the device has been read since the test began.</summary>
		public bool HasRead { get { return Volatile.Read(ref _recordedFlag) != 0; } }
		int _recordedFlag;

		internal void NoteVirtualNeutral()
		{
			Volatile.Write(ref _virtualNeutral, 1);
		}

		internal void NoteOffline()
		{
			Volatile.Write(ref _offline, 1);
		}

		/// <summary>Records a read of the device: the time, and every value when anything changed. Engine only; makes nothing.</summary>
		internal void Record(SourceState state, long ticks)
		{
			Interlocked.Increment(ref _reads);
			if (state == null)
				return;
			if (_recorded && !Changed(state))
				return;
			Copy(state, _last);
			_recorded = true;
			Volatile.Write(ref _recordedFlag, 1);
			var written = _written;
			if (written - Volatile.Read(ref _taken) >= Capacity)
			{
				Interlocked.Increment(ref _dropped);
				return;
			}
			var slot = (int)(written % Capacity);
			_ticks[slot] = ticks;
			Array.Copy(state.Axis, 0, _values, slot * Channels, SourceState.MaxAxis);
			Array.Copy(state.Sliders, 0, _values, slot * Channels + SourceState.MaxAxis, SourceState.MaxSliders);
			for (var w = 0; w < ButtonWords; w++)
			{
				ulong bits = 0;
				for (var b = 0; b < 64 && w * 64 + b < state.Buttons.Length; b++)
					if (state.Buttons[w * 64 + b])
						bits |= 1UL << b;
				_buttons[slot * ButtonWords + w] = bits;
			}
			Volatile.Write(ref _written, written + 1);
		}

		/// <summary>Records the change buffer's entries of a read. Engine only; makes nothing.</summary>
		internal void Record(DirectInputChanges changes, long ticks)
		{
			Volatile.Write(ref _hasChangeBuffer, 1);
			var written = _entriesWritten;
			for (var i = 0; i < changes.EntryCount; i++)
			{
				if (written - Volatile.Read(ref _entriesTaken) >= EntryCapacity)
				{
					Interlocked.Increment(ref _droppedEntries);
					continue;
				}
				var slot = (int)(written % EntryCapacity);
				_entryTicks[slot] = ticks;
				_entryOffsets[slot] = changes.EntryOffsets[i];
				_entrySequences[slot] = changes.EntrySequences[i];
				written++;
			}
			Volatile.Write(ref _entriesWritten, written);
		}

		bool Changed(SourceState state)
		{
			for (var i = 0; i < state.Axis.Length; i++)
				if (state.Axis[i] != _last.Axis[i])
					return true;
			for (var i = 0; i < state.Sliders.Length; i++)
				if (state.Sliders[i] != _last.Sliders[i])
					return true;
			for (var i = 0; i < state.Povs.Length; i++)
				if (state.Povs[i] != _last.Povs[i])
					return true;
			for (var i = 0; i < state.Buttons.Length; i++)
				if (state.Buttons[i] != _last.Buttons[i])
					return true;
			return false;
		}

		static void Copy(SourceState from, SourceState to)
		{
			Array.Copy(from.Axis, to.Axis, to.Axis.Length);
			Array.Copy(from.Sliders, to.Sliders, to.Sliders.Length);
			Array.Copy(from.Povs, to.Povs, to.Povs.Length);
			Array.Copy(from.Buttons, to.Buttons, to.Buttons.Length);
		}

		#endregion

		#region Taken by the window

		readonly int[] _taking = new int[Channels];
		readonly bool[] _takingButtons = new bool[ButtonWords * 64];

		/// <summary>Hands the reads and entries recorded since the last call to <paramref name="steps"/> and its statistics. Window only.</summary>
		/// <remarks>An entry's offset is the control's place in DirectInput's joystick state: the six axes, then the two sliders, are values; the rest are -1.</remarks>
		public void TakeInto(DeviceTestSteps steps)
		{
			var statistics = steps.Statistics;
			var written = Volatile.Read(ref _written);
			for (var taken = _taken; taken < written; taken++)
			{
				var slot = (int)(taken % Capacity);
				Array.Copy(_values, slot * Channels, _taking, 0, Channels);
				for (var b = 0; b < _takingButtons.Length; b++)
					_takingButtons[b] = (_buttons[slot * ButtonWords + b / 64] & (1UL << (b % 64))) != 0;
				steps.AddReport(_ticks[slot], _taking, _takingButtons);
			}
			Volatile.Write(ref _taken, written);
			var entries = Volatile.Read(ref _entriesWritten);
			for (var taken = _entriesTaken; taken < entries; taken++)
			{
				var slot = (int)(taken % EntryCapacity);
				statistics.AddEntry(_entryTicks[slot], _entrySequences[slot], Channel(_entryOffsets[slot]));
			}
			Volatile.Write(ref _entriesTaken, entries);
		}

		/// <summary>Takes out what was recorded and forgets it, so a new test starts from now.</summary>
		public void Skip()
		{
			Volatile.Write(ref _taken, Volatile.Read(ref _written));
			Volatile.Write(ref _entriesTaken, Volatile.Read(ref _entriesWritten));
		}

		/// <summary>The latest values recorded, for a new test to start from; false before the first read.</summary>
		public bool Latest(int[] values)
		{
			var written = Volatile.Read(ref _written);
			if (written == 0)
				return false;
			var slot = (int)((written - 1) % Capacity);
			Array.Copy(_values, slot * Channels, values, 0, Channels);
			return true;
		}

		/// <summary>The value a change buffer entry changed, as an index of a read's values: X, Y, Z, their rotations and the two sliders; -1 for a button or a hat.</summary>
		public static int Channel(int offset)
		{
			if (offset < 0 || offset >= 32 || offset % 4 != 0)
				return -1;
			var slot = offset / 4;
			return slot < 6 ? slot : SourceState.MaxAxis + slot - 6;
		}

		#endregion
	}
}
