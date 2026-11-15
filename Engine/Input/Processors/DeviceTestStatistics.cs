using System;
using System.Collections.Generic;
using System.Linq;

namespace x360ce.Engine
{
	/// <summary>Where a device test is: not started, at rest, moving, or stopped.</summary>
	public enum DeviceTestPhase
	{
		Idle,
		Rest,
		Moving,
		Stopped,
	}

	/// <summary>
	/// What the Test Device window measures from the reads of one device: how often it reports, how many values a report
	/// carries, and each axis's range, centre, rest noise, resolution and steps.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Fed the reads the engine recorded and nothing else, so it can be tested on made-up streams. A test has two phases: at
	/// rest, with the device left alone, gives each axis's noise and centre; moving, one control at a time to its ends, gives
	/// the rest. Only the reads that changed something are given (<see cref="AddReport"/>); the others are counted
	/// (<see cref="SetReads"/>). A value holds from one report to the next, so the centre and the noise weigh each value by how
	/// long it was held.
	/// </para>
	/// <para>
	/// Where the device keeps a change buffer, DirectInput gives every change the sequence number of the report it came in, and
	/// the values a report carries are counted from those (<see cref="AddEntry"/>); otherwise from which values changed together
	/// between two reads. Used on one thread; it makes objects as it goes, and is never used by the engine.
	/// </para>
	/// </remarks>
	public sealed class DeviceTestStatistics
	{
		/// <param name="channels">How many values a report carries: the axes and the sliders.</param>
		/// <param name="frequency">Ticks a second of the clock the reads are timed by.</param>
		/// <param name="range">The highest value an axis reports; the lowest is 0.</param>
		public DeviceTestStatistics(int channels, long frequency, int range = 65535)
		{
			Frequency = frequency;
			Range = range;
			Channels = new DeviceTestChannel[channels];
			for (var i = 0; i < channels; i++)
				Channels[i] = new DeviceTestChannel(this);
			MovingWindowTicks = frequency / 10;
			RunGapTicks = frequency / 10;
		}

		/// <summary>Ticks a second of the clock the reads are timed by.</summary>
		public long Frequency { get; }

		/// <summary>The highest value an axis reports.</summary>
		public int Range { get; }

		/// <summary>The middle of the range, which a dead zone is centred on.</summary>
		public double Middle { get { return Range / 2.0; } }

		/// <summary>One for each axis and slider, in the order of the values a report carries.</summary>
		public DeviceTestChannel[] Channels { get; }

		public DeviceTestPhase Phase { get; private set; }

		/// <summary>How recently another value must have changed for two values to count as moving at once: 100 ms.</summary>
		internal readonly long MovingWindowTicks;

		/// <summary>A rest spread over this share of the range, in percent, means the control moved while it was to be left alone.</summary>
		/// <remarks>Measured at rest: a Thrustmaster T.16000M and a Logitech Cordless RumblePad 2 held every axis at one value, a spread of 0 of 65,535, and the Test Controller, which never rests, spread 99 %. Five percent, 3,277, leaves a noisy or worn stick room to wander and is still far short of a touch.</remarks>
		public const double RestMovedPercent = 5;

		/// <summary>The longest pause within one movement of a control: 100 ms.</summary>
		internal readonly long RunGapTicks;

		long _startTicks;
		long _startReads, _reads, _readsTicks;

		/// <summary>Reports while moving, and the time between them, in microseconds, counted by length.</summary>
		int _reports;
		long _lastReport;
		readonly SortedDictionary<long, int> _intervals = new SortedDictionary<long, int>();

		/// <summary>Values changed per report, from the reads: the reports that changed a value, and the values they changed.</summary>
		int _stateReports;
		long _stateValues;
		int _stateConsidered, _stateTogether;

		/// <summary>The same from the change buffer's sequence numbers: the group of entries being gathered and the totals.</summary>
		bool _inGroup;
		int _groupSequence, _groupEntries;
		long _groupTicks;
		readonly List<int> _groupChannels = new List<int>();
		int _groups;
		long _groupValues;
		int _sequenceConsidered, _sequenceTogether;
		long[] _sequenceChanged;

		/// <summary>Begins the rest: everything measured so far is forgotten, and each value starts held from now.</summary>
		/// <param name="values">Each value as the device reports it now, from <paramref name="offset"/>; null when not yet read.</param>
		public void Start(long ticks, long reads, int[] values = null, int offset = 0)
		{
			Phase = DeviceTestPhase.Rest;
			_startTicks = ticks;
			_startReads = reads;
			_reads = reads;
			_readsTicks = ticks;
			_reports = 0;
			_lastReport = 0;
			_intervals.Clear();
			_stateReports = 0;
			_stateValues = 0;
			_stateConsidered = 0;
			_stateTogether = 0;
			_inGroup = false;
			_groupChannels.Clear();
			_groups = 0;
			_groupValues = 0;
			_sequenceConsidered = 0;
			_sequenceTogether = 0;
			_sequenceChanged = new long[Channels.Length];
			for (var i = 0; i < Channels.Length; i++)
			{
				Channels[i].Reset();
				if (values != null)
					Channels[i].Seed(values[offset + i], ticks);
			}
		}

		/// <summary>Ends the rest and begins the moving: the noise and the centre are what the rest held.</summary>
		public void StartMoving(long ticks, long reads)
		{
			if (Phase != DeviceTestPhase.Rest)
				return;
			foreach (var channel in Channels)
				channel.EndRest(ticks);
			SetReads(ticks, reads);
			Phase = DeviceTestPhase.Moving;
		}

		/// <summary>Ends the test; what was measured stays.</summary>
		public void Stop(long ticks, long reads)
		{
			if (Phase == DeviceTestPhase.Rest)
				foreach (var channel in Channels)
					channel.EndRest(ticks);
			if (Phase == DeviceTestPhase.Rest || Phase == DeviceTestPhase.Moving)
			{
				FinishGroup();
				SetReads(ticks, reads);
				Phase = DeviceTestPhase.Stopped;
			}
		}

		/// <summary>How many reads the device has had, changed or not.</summary>
		public void SetReads(long ticks, long reads)
		{
			if (Phase != DeviceTestPhase.Rest && Phase != DeviceTestPhase.Moving)
				return;
			_reads = reads;
			_readsTicks = ticks;
		}

		/// <summary>A read that changed something: a value, a button or a hat.</summary>
		/// <param name="values">Each value the read gave, from <paramref name="offset"/>, one for each of <see cref="Channels"/>.</param>
		public void AddReport(long ticks, int[] values, int offset = 0)
		{
			if (Phase != DeviceTestPhase.Rest && Phase != DeviceTestPhase.Moving)
				return;
			var moving = Phase == DeviceTestPhase.Moving;
			var changed = 0;
			for (var i = 0; i < Channels.Length; i++)
				if (Channels[i].Add(values[offset + i], ticks, moving))
					changed++;
			if (!moving)
				return;
			// The time between two reports within a movement; a longer pause is the device left alone, not its rate.
			if (_reports > 0 && ticks - _lastReport <= RunGapTicks)
			{
				var us = (ticks - _lastReport) * 1000000L / Frequency;
				int count;
				_intervals.TryGetValue(us, out count);
				_intervals[us] = count + 1;
			}
			_reports++;
			_lastReport = ticks;
			if (changed == 0)
				return;
			_stateReports++;
			_stateValues += changed;
			if (Channels.Count(x => x.HasChanged && ticks - x.LastChange <= MovingWindowTicks) >= 2)
			{
				_stateConsidered++;
				if (changed >= 2)
					_stateTogether++;
			}
		}

		/// <summary>An entry of the device's change buffer: one value of one report, with the report's sequence number.</summary>
		/// <param name="channel">The axis or slider the entry changed, as an index into <see cref="Channels"/>; -1 for a button or a hat.</param>
		public void AddEntry(long ticks, int sequence, int channel)
		{
			if (Phase != DeviceTestPhase.Moving)
				return;
			if (_inGroup && sequence != _groupSequence)
				FinishGroup();
			if (!_inGroup)
			{
				_inGroup = true;
				_groupSequence = sequence;
				_groupEntries = 0;
				_groupTicks = ticks;
				_groupChannels.Clear();
			}
			_groupEntries++;
			if (channel >= 0 && channel < Channels.Length && !_groupChannels.Contains(channel))
				_groupChannels.Add(channel);
		}

		/// <summary>Counts the report whose entries were gathered.</summary>
		void FinishGroup()
		{
			if (!_inGroup)
				return;
			_inGroup = false;
			_groups++;
			_groupValues += _groupEntries;
			if (_groupChannels.Count == 0)
				return;
			foreach (var channel in _groupChannels)
				_sequenceChanged[channel] = _groupTicks;
			var movingNow = 0;
			for (var i = 0; i < _sequenceChanged.Length; i++)
				if (_sequenceChanged[i] != 0 && _groupTicks - _sequenceChanged[i] <= MovingWindowTicks)
					movingNow++;
			if (movingNow >= 2)
			{
				_sequenceConsidered++;
				if (_groupChannels.Count >= 2)
					_sequenceTogether++;
			}
		}

		/// <summary>The test's figures for the whole device, as of <paramref name="now"/> while it runs.</summary>
		public DeviceTestSummary Summary(long now)
		{
			var s = new DeviceTestSummary { Phase = Phase, Reports = _reports };
			if (Phase == DeviceTestPhase.Idle)
				return s;
			var seconds = (double)(_readsTicks - _startTicks) / Frequency;
			s.Reads = _reads - _startReads;
			if (seconds > 0)
				s.ReadRate = s.Reads / seconds;
			if (_intervals.Count > 0)
			{
				s.ShortestInterval = _intervals.Keys.First() / 1000.0;
				var median = Median(_intervals);
				s.MedianInterval = median / 1000.0;
				if (median > 0)
				{
					s.ReportRate = 1000000.0 / median;
					s.OnTimePercent = OnTime(_intervals, median);
				}
			}
			// A group still being gathered is counted as it stands.
			var groups = _groups + (_inGroup ? 1 : 0);
			var groupValues = _groupValues + (_inGroup ? _groupEntries : 0);
			s.FromSequence = groups > 0;
			if (s.FromSequence)
			{
				s.ValuesPerReport = (double)groupValues / groups;
				if (_sequenceConsidered > 0)
					s.TogetherPercent = 100.0 * _sequenceTogether / _sequenceConsidered;
			}
			else if (_stateReports > 0)
			{
				s.ValuesPerReport = (double)_stateValues / _stateReports;
				if (_stateConsidered > 0)
					s.TogetherPercent = 100.0 * _stateTogether / _stateConsidered;
			}
			s.TooSlow = s.ReportRate.HasValue && s.ReadRate.HasValue && s.ReportRate.Value > 0 && s.ReadRate.Value < 2 * s.ReportRate.Value;
			return s;
		}

		/// <summary>Of times given with how often each came, the share in percent within a tenth of <paramref name="median"/>.</summary>
		internal static double OnTime(SortedDictionary<long, int> counts, long median)
		{
			long total = 0, near = 0;
			foreach (var pair in counts)
			{
				total += pair.Value;
				if (Math.Abs(pair.Key - median) * 10 <= median)
					near += pair.Value;
			}
			return total > 0 ? 100.0 * near / total : 0;
		}

		/// <summary>The middle value of numbers given with how often each came, in increasing order.</summary>
		internal static long Median(SortedDictionary<long, int> counts)
		{
			var total = counts.Values.Sum(x => (long)x);
			var half = (total + 1) / 2;
			long seen = 0;
			foreach (var pair in counts)
			{
				seen += pair.Value;
				if (seen >= half)
					return pair.Key;
			}
			return 0;
		}
	}

	/// <summary>The test's figures for the whole device.</summary>
	public sealed class DeviceTestSummary
	{
		public DeviceTestPhase Phase;

		/// <summary>Reports a second while the device moves: one over the median time between two reads that changed anything, within a movement.</summary>
		public double? ReportRate;

		/// <summary>The shortest and the median time between two reports while moving, in milliseconds.</summary>
		public double? ShortestInterval;
		public double? MedianInterval;

		/// <summary>Of the times between two reports while moving, the share within a tenth of the median: how evenly the device reports.</summary>
		public double? OnTimePercent;

		/// <summary>The values a report changes, on average; from the change buffer when <see cref="FromSequence"/>.</summary>
		public double? ValuesPerReport;

		/// <summary>Of the reports made while two or more values moved, the share that changed two or more of them together.</summary>
		public double? TogetherPercent;

		/// <summary>Whether <see cref="ValuesPerReport"/> and <see cref="TogetherPercent"/> come from the change buffer's sequence numbers.</summary>
		public bool FromSequence;

		/// <summary>Reads a second, changed or not, and how many.</summary>
		public double? ReadRate;
		public long Reads;

		/// <summary>Reports while moving.</summary>
		public int Reports;

		/// <summary>True when reads come less than twice as often as reports, so a report can be missed.</summary>
		public bool TooSlow;
	}

	/// <summary>One axis or slider: what the test found, and what it keeps to find it.</summary>
	public sealed class DeviceTestChannel
	{
		internal DeviceTestChannel(DeviceTestStatistics owner)
		{
			_owner = owner;
		}

		readonly DeviceTestStatistics _owner;
		readonly HashSet<int> _distinct = new HashSet<int>();
		readonly SortedDictionary<long, int> _steps = new SortedDictionary<long, int>();
		bool _seeded;
		int _value;
		long _since;
		double _restTicks, _restSum, _restSquares;
		bool _restDone;
		/// <summary>The time between two changes within a movement, in microseconds, counted by length.</summary>
		readonly SortedDictionary<long, int> _intervals = new SortedDictionary<long, int>();

		/// <summary>The lowest and highest value seen.</summary>
		public int? Min { get; private set; }
		public int? Max { get; private set; }

		/// <summary>The lowest and highest value held at rest.</summary>
		public int? RestMin { get; private set; }
		public int? RestMax { get; private set; }

		/// <summary>The smallest and largest change from one report to the next, moving or not.</summary>
		public int? SmallestStep { get; private set; }
		public int? LargestStep { get; private set; }

		/// <summary>The smallest distance from the centre the value first went to, each time it left its rest band while moving.</summary>
		public double? FirstStep { get; private set; }

		/// <summary>Whether the value changed since the test began, and when it last did.</summary>
		public bool HasChanged { get; private set; }
		public long LastChange { get; private set; }

		/// <summary>When the value was last outside its rest band while moving: the control being moved.</summary>
		public long LastMoved { get; private set; }

		/// <summary>How many different values were seen.</summary>
		public int Distinct { get { return _distinct.Count; } }

		internal void Reset()
		{
			_distinct.Clear();
			_steps.Clear();
			_seeded = false;
			_restTicks = 0;
			_restSum = 0;
			_restSquares = 0;
			_restDone = false;
			_intervals.Clear();
			Min = Max = RestMin = RestMax = SmallestStep = LargestStep = null;
			FirstStep = null;
			HasChanged = false;
			LastChange = 0;
			LastMoved = 0;
		}

		/// <summary>Takes the value the channel holds from <paramref name="ticks"/> on, before it changes.</summary>
		internal void Seed(int value, long ticks, bool rest = true)
		{
			_seeded = true;
			_value = value;
			_since = ticks;
			Seen(value, rest);
		}

		void Seen(int value, bool rest)
		{
			_distinct.Add(value);
			if (!Min.HasValue || value < Min)
				Min = value;
			if (!Max.HasValue || value > Max)
				Max = value;
			if (!rest)
				return;
			if (!RestMin.HasValue || value < RestMin)
				RestMin = value;
			if (!RestMax.HasValue || value > RestMax)
				RestMax = value;
		}

		/// <summary>Adds the time the current value was held at rest, up to <paramref name="ticks"/>.</summary>
		void HoldAtRest(long ticks)
		{
			var held = (double)(ticks - _since);
			if (held <= 0)
				return;
			_restTicks += held;
			_restSum += held * _value;
			_restSquares += held * _value * (double)_value;
		}

		internal void EndRest(long ticks)
		{
			if (_seeded && !_restDone)
				HoldAtRest(ticks);
			_since = ticks;
			_restDone = true;
		}

		/// <returns>True when the value changed.</returns>
		internal bool Add(int value, long ticks, bool moving)
		{
			if (!_seeded)
			{
				Seed(value, ticks, !moving);
				return false;
			}
			if (value == _value)
				return false;
			if (!moving)
				HoldAtRest(ticks);
			var step = Math.Abs((long)value - _value);
			if (!SmallestStep.HasValue || step < SmallestStep)
				SmallestStep = (int)step;
			if (!LargestStep.HasValue || step > LargestStep)
				LargestStep = (int)step;
			if (moving)
			{
				int count;
				_steps.TryGetValue(step, out count);
				_steps[step] = count + 1;
				// A movement is the changes no further apart than a pause within one; its own rate is counted from them.
				if (HasChanged && LastChange != 0 && ticks - LastChange <= _owner.RunGapTicks)
				{
					var us = (ticks - LastChange) * 1000000L / _owner.Frequency;
					int times;
					_intervals.TryGetValue(us, out times);
					_intervals[us] = times + 1;
				}
				if (RestMin.HasValue && (value < RestMin || value > RestMax))
				{
					LastMoved = ticks;
					if (_value >= RestMin && _value <= RestMax && Centre.HasValue)
					{
						var away = Math.Abs(value - Centre.Value);
						if (!FirstStep.HasValue || away < FirstStep)
							FirstStep = away;
					}
				}
			}
			_value = value;
			_since = ticks;
			HasChanged = true;
			LastChange = ticks;
			Seen(value, !moving);
			return true;
		}

		/// <summary>The value the axis rests at: the average of what it held at rest, by time.</summary>
		public double? Centre { get { return _restTicks > 0 ? _restSum / _restTicks : (_seeded && !_restDone ? _value : (double?)null); } }

		/// <summary>How far the centre is from the middle of the range.</summary>
		public double? CentreOffset { get { return Centre.HasValue ? Centre - _owner.Middle : null; } }

		/// <summary><see cref="CentreOffset"/> as a share of the range: below the middle negative, above it positive.</summary>
		public double? CentreOffsetPercent { get { return CentreOffset.HasValue ? 100 * CentreOffset / _owner.Range : null; } }

		/// <summary>The rest noise: the spread of the values held at rest, and their standard deviation by time.</summary>
		public int? NoiseSpread { get { return RestMin.HasValue ? RestMax - RestMin : null; } }
		public double? NoiseDeviation
		{
			get
			{
				if (_restTicks <= 0)
					return RestMin.HasValue ? 0 : (double?)null;
				var mean = _restSum / _restTicks;
				return Math.Sqrt(Math.Max(0, _restSquares / _restTicks - mean * mean));
			}
		}

		/// <summary>The dead zone, in percent of half the range, that hides the rest noise and the centre's offset: the farthest the value went from the middle at rest.</summary>
		public double? DeadZonePercent
		{
			get
			{
				if (!RestMin.HasValue)
					return null;
				var far = Math.Max(Math.Abs(RestMin.Value - _owner.Middle), Math.Abs(RestMax.Value - _owner.Middle));
				return 100 * far / _owner.Middle;
			}
		}

		/// <summary>The share of the range between the lowest and the highest value seen.</summary>
		public double? RangePercent { get { return Min.HasValue ? 100.0 * (Max - Min) / _owner.Range : null; } }

		/// <summary>The bits the different values seen take: log2 of their number.</summary>
		public double? EffectiveBits { get { return Distinct > 1 ? Math.Log(Distinct, 2) : (double?)null; } }

		/// <summary><see cref="FirstStep"/> as a share of the range: the first jump away from the centre.</summary>
		public double? FirstStepPercent { get { return FirstStep.HasValue ? 100 * FirstStep / _owner.Range : null; } }

		/// <summary>The median change from one report to the next while moving.</summary>
		public long? MedianStep { get { return _steps.Count > 0 ? DeviceTestStatistics.Median(_steps) : (long?)null; } }

		/// <summary>The finest change from one report to the next while moving that came more than once: the axis's resolution, a single stray step left out.</summary>
		public long? FineStep
		{
			get
			{
				foreach (var pair in _steps)
					if (pair.Value >= 2)
						return pair.Key;
				return _steps.Count > 0 ? _steps.Keys.First() : (long?)null;
			}
		}

		/// <summary>Changes while moving more than four times the median change: jumps.</summary>
		public int Jumps
		{
			get
			{
				var median = MedianStep;
				return median.HasValue ? _steps.Where(x => x.Key > 4 * median.Value).Sum(x => x.Value) : 0;
			}
		}

		/// <summary>Changes a second while the control moves: one over the median time between two of its changes within a movement, as the report rate is worked out.</summary>
		public double? UpdateRate
		{
			get
			{
				var median = _intervals.Count > 0 ? DeviceTestStatistics.Median(_intervals) : 0;
				return median > 0 ? 1000000.0 / median : (double?)null;
			}
		}

		/// <summary>Whether the control moved while the controller was to be left alone: its rest spread is over <see cref="DeviceTestStatistics.RestMovedPercent"/> of the range, and its rest figures mean nothing.</summary>
		public bool MovedAtRest { get { return NoiseSpread.HasValue && 100.0 * NoiseSpread.Value / _owner.Range > DeviceTestStatistics.RestMovedPercent; } }
	}
}
