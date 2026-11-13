using System;
using System.Collections.Generic;
using System.Linq;

namespace x360ce.Engine
{
	/// <summary>The steps of a device test, in the order the Test Device window takes them.</summary>
	public enum DeviceTestStep
	{
		Idle,
		GetReady,
		Rest,
		PickUp,
		Sticks,
		Buttons,
		Done,
	}

	/// <summary>What a device test needs to know of the device: how it is read, which values and buttons it has, and what kind it is.</summary>
	public sealed class DeviceTestLayout
	{
		/// <summary>How x360ce reads the device.</summary>
		public InputSourceType Method = InputSourceType.DirectInput;

		/// <summary>The values the device has, as indexes into a read's values: the 24 axes, then the 8 sliders.</summary>
		public int[] Channels = new int[0];

		/// <summary>How many buttons the device has.</summary>
		public int ButtonCount;

		/// <summary>An Xbox controller: DirectInput and Raw Input give its two triggers as one axis, Z.</summary>
		public bool XInputCapable;

		/// <summary>A wheel, whose X turns: none of its axes is taken for a stick unless two move as one.</summary>
		public bool Wheel;

		/// <summary>The axis DirectInput and Raw Input give an Xbox controller's two triggers on: Z.</summary>
		public const int MergedTriggers = 2;

		/// <summary>The input method, when it gives a control in a way no test can grade; otherwise null.</summary>
		/// <remarks>An Xbox controller's two triggers share one axis in DirectInput and Raw Input, so neither can be measured alone.</remarks>
		public string LimitsControl(int channel)
		{
			return XInputCapable && channel == MergedTriggers && (Method == InputSourceType.DirectInput || Method == InputSourceType.RawInput)
				? MethodName(Method) : null;
		}

		/// <summary>The input method, when it limits a value's step size: XInput gives a trigger 256 steps.</summary>
		public string LimitsStep(int channel)
		{
			return Method == InputSourceType.XInput && IsXInputTrigger(channel) ? MethodName(Method) : LimitsControl(channel);
		}

		/// <summary>The name a person knows an input method by.</summary>
		public static string MethodName(InputSourceType method)
		{
			switch (method)
			{
				case InputSourceType.DirectInput: return "DirectInput";
				case InputSourceType.XInput: return "XInput";
				case InputSourceType.RawInput: return "Raw Input";
				case InputSourceType.GamingInput: return "Gaming Input";
				case InputSourceType.Plugin: return "its plugin";
				default: return "its input method";
			}
		}

		/// <summary>The slots XInput's triggers are given in: Z and Z Rotation, beside the sticks' X, Y, X Rotation and Y Rotation.</summary>
		public static bool IsXInputTrigger(int channel)
		{
			return channel == 2 || channel == 5;
		}

		/// <summary>The lowest and highest value a method gives a value in.</summary>
		/// <remarks>
		/// DirectInput gives 0 to 65,535, and so do Raw Input and plugins, whose reports are read into DirectInput's ranges.
		/// XInput gives its sticks -32,768 to 32,767 and its triggers 0 to 255.
		/// </remarks>
		public static void Range(InputSourceType method, int channel, out int min, out int max)
		{
			if (method == InputSourceType.XInput)
			{
				min = IsXInputTrigger(channel) ? 0 : short.MinValue;
				max = IsXInputTrigger(channel) ? 255 : short.MaxValue;
				return;
			}
			min = 0;
			max = ushort.MaxValue;
		}

		/// <summary>A value as the method gives it, on the one scale every method is compared on: -32,768 to 32,767.</summary>
		public static int Normalise(InputSourceType method, int channel, int value)
		{
			int min, max;
			Range(method, channel, out min, out max);
			if (value <= min)
				return short.MinValue;
			if (value >= max)
				return short.MaxValue;
			return (int)Math.Round((value - (double)min) * ushort.MaxValue / (max - min)) + short.MinValue;
		}

		/// <summary>The device's own name for each value, as DirectInput gives its objects ("Throttle", "Rudder"); null or empty where it gives none.</summary>
		public string[] Names;

		/// <summary>
		/// The name of a value as a person reads it: XInput's triggers, an Xbox controller's combined triggers, then the
		/// device's own name, else DirectInput's: X Axis, Y Axis, Z Axis, their rotations, Axis 7 to 24, Slider 1 to 8.
		/// </summary>
		public string ChannelName(int channel)
		{
			if (Method == InputSourceType.XInput && IsXInputTrigger(channel))
				return channel == 2 ? "Left trigger" : "Right trigger";
			if (LimitsControl(channel) != null)
				return "Triggers (combined)";
			if (Names != null && channel < Names.Length && !string.IsNullOrEmpty(Names[channel]))
				return Names[channel];
			string[] names = { "X Axis", "Y Axis", "Z Axis", "X Rotation", "Y Rotation", "Z Rotation" };
			if (channel < names.Length)
				return names[channel];
			return channel < SourceState.MaxAxis ? "Axis " + (channel + 1) : "Slider " + (channel - SourceState.MaxAxis + 1);
		}
	}

	/// <summary>
	/// A device test, step by step: get ready, rest, pick up, sticks and triggers, buttons, then the results. The person
	/// presses only Start; each step moves on by itself, as the reads show it done or as its countdown ends.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Fed the reads the engine recorded (<see cref="AddReport"/>) and the time (<see cref="Tick"/>), and nothing else, so a
	/// scripted stream on a made-up clock runs it whole. Every value is put on one scale first,
	/// <see cref="DeviceTestLayout.Normalise"/>, and handed on to <see cref="Statistics"/>, which measures the rate, the rest
	/// and the steps.
	/// </para>
	/// <para>
	/// The controller must lie still through get ready and rest: a touch, a value moving 5 % of its range or a button, starts
	/// get ready again, so a rest figure is never one of a moving controller. Sticks and triggers move on once every control
	/// is full; a control half done holds the step for as long as it takes. Only when every control moved is full, or none
	/// was, do 15 seconds without movement start a countdown, at whose end the controls never moved count as absent. Buttons
	/// do the same for buttons never pressed.
	/// </para>
	/// </remarks>
	public sealed class DeviceTestSteps
	{
		/// <param name="layout">The device: how it is read, and its values and buttons.</param>
		/// <param name="frequency">Ticks a second of the clock the reads and <see cref="Tick"/> are timed by.</param>
		public DeviceTestSteps(DeviceTestLayout layout, long frequency)
		{
			Layout = layout;
			Frequency = frequency;
			Statistics = new DeviceTestStatistics(Values, frequency);
			Buttons = new DeviceTestButton[layout.ButtonCount];
			for (var i = 0; i < Buttons.Length; i++)
				Buttons[i] = new DeviceTestButton(i);
			_axes = layout.Channels.Where(x => x < SourceState.MaxAxis + SourceState.MaxSliders).Distinct().OrderBy(x => x)
				.Select(x => new DeviceTestAxis(x)).ToArray();
			var sticks = _axes.Where(x => x.Channel < SourceState.MaxAxis).ToArray();
			for (var a = 0; a < sticks.Length; a++)
				for (var b = a + 1; b < sticks.Length; b++)
					_pairs.Add(new DeviceTestPair(sticks[a], sticks[b]));
			Instruction = "Press Start to test the controller.";
		}

		#region Settings

		/// <summary>The steps a person is shown: get ready, rest, pick up, sticks and triggers, buttons.</summary>
		public const int StepCount = 5;

		/// <summary>The values a read carries: the 24 axes, then the 8 sliders.</summary>
		public const int Values = SourceState.MaxAxis + SourceState.MaxSliders;

		/// <summary>How long each timed step lasts, and how long to wait without movement before counting down, in seconds.</summary>
		public const int GetReadySeconds = 10, RestSeconds = 5, PickUpSeconds = 5, IdleSeconds = 15, ButtonsIdleSeconds = 10, CountdownSeconds = 10;

		/// <summary>A change this far from where a value was is a touch, less is noise: <see cref="DeviceTestStatistics.RestMovedPercent"/> of the range, 3,277.</summary>
		public static readonly int Moved = (int)Math.Round(ushort.MaxValue * DeviceTestStatistics.RestMovedPercent / 100);

		/// <summary>How near its end a value must come, before it turns back by <see cref="Moved"/>, for the end to count as reached: 5 % of the range, so a stick worn to 90 % of its travel still finishes, and is graded by the range it reached.</summary>
		public const int EndTolerance = 3277;

		/// <summary>An axis is full when it reached both ends and turned back, gave values in 90 of the 100 equal parts of its range, and changed 100 times while away from rest.</summary>
		public const int Bins = 100, BinsNeeded = 90, UpdatesNeeded = 100;

		/// <summary>A stick is full when it also reached the edge, 80 % of the way out, in each of its 36 ten-degree sectors.</summary>
		public const int Sectors = 36;
		public const double EdgeRadius = 0.8;

		/// <summary>A control that goes this long without getting fuller while it still changes is unstable, and holds its step no longer; a button pressed this many times since a new one was last ticked is flickering.</summary>
		public const int UnstableSeconds = 20, FlickerPresses = 20;

		/// <summary>A press this soon after the button was let go is the same push registered twice.</summary>
		public const double TwiceMilliseconds = 30;

		#endregion

		/// <summary>The device tested.</summary>
		public DeviceTestLayout Layout { get; }

		/// <summary>Ticks a second of the clock.</summary>
		public long Frequency { get; }

		/// <summary>The rate, rest and step figures, from every read of the test.</summary>
		public DeviceTestStatistics Statistics { get; }

		public DeviceTestStep Step { get; private set; }

		/// <summary>The step as shown: 1 to <see cref="StepCount"/>, 0 before and after.</summary>
		public int StepNumber
		{
			get { return Step >= DeviceTestStep.GetReady && Step <= DeviceTestStep.Buttons ? (int)Step : 0; }
		}

		/// <summary>The steps' names, and what each asks, in order.</summary>
		public static readonly string[] StepNames = { "Get ready", "Rest", "Pick up", "Sticks and triggers", "Buttons" };
		public static readonly string[] StepTexts =
		{
			"Put the controller on the desk and let go.",
			"It lies still for 5 seconds while its drift and centre are measured.",
			"Pick up the controller.",
			"Turn each stick slowly round its edge, then press each trigger slowly all the way, until every bar is full.",
			"Press each button once.",
		};

		/// <summary>The step's name: Get ready, Rest, Pick up, Sticks and triggers, Buttons; Ready before and Results or Stopped after.</summary>
		public string StepName
		{
			get
			{
				if (StepNumber > 0)
					return StepNames[StepNumber - 1];
				if (Step == DeviceTestStep.Done)
					return Stopped ? "Stopped" : "Results";
				return "Ready";
			}
		}

		/// <summary>What to do now, in a few words.</summary>
		public string Instruction { get; private set; }

		/// <summary>What the reads show, or what to do next: why a countdown started again, the control to keep moving.</summary>
		public string Hint { get; private set; } = "";

		/// <summary>The countdown as shown: the whole seconds left, empty without one; and what happens when it ends, "Moving on in" or "Finishing in", empty for a timed step.</summary>
		public string Countdown { get; private set; } = "";
		public string CountdownCaption { get; private set; } = "";

		/// <summary>Whether Stop ended the test before its steps did.</summary>
		public bool Stopped { get; private set; }

		/// <summary>The sticks and the single axes and sliders, as the reads paired them; made again when they pair otherwise, which <see cref="ControlsVersion"/> counts.</summary>
		public IList<DeviceTestControl> Controls { get { return _controls; } }
		public int ControlsVersion { get; private set; }

		/// <summary>The buttons, each with its tick and whether one push registered twice.</summary>
		public DeviceTestButton[] Buttons { get; }

		/// <summary>The graded results, once done; null before, or when the test stopped before the controls were measured.</summary>
		public DeviceTestResults Results { get; private set; }

		readonly DeviceTestAxis[] _axes;
		readonly List<DeviceTestPair> _pairs = new List<DeviceTestPair>();
		readonly List<DeviceTestControl> _controls = new List<DeviceTestControl>();
		string _pairing;

		/// <summary>The latest values on one scale, and as the statistics take them: 0 to 65,535.</summary>
		readonly int[] _values = new int[Values];
		readonly int[] _statisticsValues = new int[Values];
		readonly bool[] _buttons = new bool[128];
		bool _hasValues;

		/// <summary>Get ready and rest: the lowest and highest each value went, and the buttons, since the controller was last still.</summary>
		readonly int[] _stillMin = new int[Values];
		readonly int[] _stillMax = new int[Values];
		readonly bool[] _stillButtons = new bool[128];

		long _stepStart, _lastNewPress, _restarted, _reads;
		bool _measured;

		/// <summary>Begins the test with get ready.</summary>
		public void Start(long now)
		{
			Stopped = false;
			Results = null;
			_measured = false;
			BeginGetReady(now, "");
		}

		/// <summary>How many reads the device has had, changed or not.</summary>
		public void SetReads(long ticks, long reads)
		{
			_reads = reads;
			Statistics.SetReads(ticks, reads);
		}

		/// <summary>A read that changed something.</summary>
		/// <param name="values">The 32 values as the device's input method gives them.</param>
		/// <param name="buttons">Its buttons, pressed or not; null when not known.</param>
		public void AddReport(long ticks, int[] values, bool[] buttons)
		{
			for (var i = 0; i < Values; i++)
			{
				_values[i] = DeviceTestLayout.Normalise(Layout.Method, i, values[i]);
				_statisticsValues[i] = _values[i] - short.MinValue;
			}
			// The first read is where the controller lies when the test begins.
			if (!_hasValues)
			{
				_hasValues = true;
				Hold(buttons);
			}
			Statistics.AddReport(ticks, _statisticsValues);
			switch (Step)
			{
				case DeviceTestStep.GetReady:
					if (Touched(buttons))
					{
						_stepStart = ticks;
						_restarted = ticks;
						Hint = "Still moving: let go of the controller.";
						Hold(buttons);
					}
					break;
				case DeviceTestStep.Rest:
					if (Touched(buttons))
						BeginGetReady(ticks, "The controller moved: put it down and let go.");
					break;
				case DeviceTestStep.Sticks:
					Track(ticks);
					break;
				case DeviceTestStep.Buttons:
					Press(ticks, buttons);
					break;
			}
			if (buttons != null)
				Array.Copy(buttons, _buttons, Math.Min(buttons.Length, _buttons.Length));
		}

		/// <summary>Moves the steps on in time: counts down, and goes to the next step when one is done.</summary>
		public void Tick(long now)
		{
			var elapsed = now - _stepStart;
			switch (Step)
			{
				case DeviceTestStep.GetReady:
					// "Still moving" stays while the controller keeps moving, and goes once it lies still.
					if (_restarted != 0 && now - _restarted > Frequency * 2 && Hint.StartsWith("Still"))
						Hint = "";
					if (elapsed >= Frequency * GetReadySeconds)
						Begin(DeviceTestStep.Rest, now, "Measuring, don't touch.");
					else
						Countdown = SecondsLeft(Frequency * GetReadySeconds - elapsed);
					break;
				case DeviceTestStep.Rest:
					if (elapsed >= Frequency * RestSeconds)
					{
						Statistics.StartMoving(now, _reads);
						Begin(DeviceTestStep.PickUp, now, "Pick up the controller.");
					}
					else
						Countdown = SecondsLeft(Frequency * RestSeconds - elapsed);
					break;
				case DeviceTestStep.PickUp:
					if (elapsed >= Frequency * PickUpSeconds)
						BeginSticks(now);
					else
						Countdown = SecondsLeft(Frequency * PickUpSeconds - elapsed);
					break;
				case DeviceTestStep.Sticks:
					TickSticks(now);
					break;
				case DeviceTestStep.Buttons:
					TickButtons(now);
					break;
			}
		}

		/// <summary>Ends the test at once; what was measured is graded.</summary>
		public void Stop(long now)
		{
			if (Step == DeviceTestStep.Idle || Step == DeviceTestStep.Done)
				return;
			Stopped = true;
			Finish(now);
		}

		#region Get ready and rest

		void BeginGetReady(long now, string hint)
		{
			Begin(DeviceTestStep.GetReady, now, "Put the controller on the desk and let go.");
			Hint = hint;
			_restarted = 0;
		}

		void Begin(DeviceTestStep step, long now, string instruction)
		{
			Step = step;
			_stepStart = now;
			Instruction = instruction;
			if (step != DeviceTestStep.GetReady)
				Hint = "";
			Countdown = CountdownCaption = "";
			Hold(_buttons);
			if (step == DeviceTestStep.Rest)
				Statistics.Start(now, _reads, _hasValues ? _statisticsValues : null);
			Tick(now);
		}

		/// <summary>Takes the controller as still from now: where each value is, and the buttons.</summary>
		void Hold(bool[] buttons)
		{
			Array.Copy(_values, _stillMin, Values);
			Array.Copy(_values, _stillMax, Values);
			if (buttons != null)
				Array.Copy(buttons, _stillButtons, Math.Min(buttons.Length, _stillButtons.Length));
		}

		/// <summary>Whether the controller was touched since it was last still: a value moved more than <see cref="Moved"/>, or a button.</summary>
		bool Touched(bool[] buttons)
		{
			var touched = false;
			foreach (var axis in _axes)
			{
				var i = axis.Channel;
				var v = _values[i];
				if (v < _stillMin[i])
					_stillMin[i] = v;
				if (v > _stillMax[i])
					_stillMax[i] = v;
				if (_stillMax[i] - _stillMin[i] > Moved)
					touched = true;
			}
			if (buttons != null)
				for (var b = 0; b < Buttons.Length && b < buttons.Length; b++)
					if (buttons[b] != _stillButtons[b])
						touched = true;
			return touched;
		}

		#endregion

		#region Sticks and triggers

		void BeginSticks(long now)
		{
			_measured = true;
			if (_axes.Length == 0)
			{
				BeginButtons(now);
				return;
			}
			foreach (var axis in _axes)
			{
				var centre = Statistics.Channels[axis.Channel].Centre;
				axis.Begin(_values[axis.Channel], centre.HasValue ? (int)Math.Round(centre.Value) + short.MinValue : _values[axis.Channel]);
			}
			foreach (var pair in _pairs)
				pair.Clear();
			_pairing = null;
			Pair();
			Begin(DeviceTestStep.Sticks, now, "Turn each stick slowly round its edge, then press each trigger slowly all the way.");
		}

		/// <summary>Takes a read into each axis and each pair of axes.</summary>
		void Track(long ticks)
		{
			foreach (var axis in _axes)
				axis.Add(_values[axis.Channel], ticks);
			foreach (var pair in _pairs)
				pair.Add(_values[pair.X.Channel], _values[pair.Y.Channel]);
		}

		void TickSticks(long now)
		{
			Pair();
			foreach (var control in _controls)
			{
				if (control.Complete && control.CompletedAt == 0)
					control.CompletedAt = now;
				Watch(control, now);
			}
			// A control that keeps changing without getting fuller holds the step no longer.
			if (_controls.All(x => x.Complete || x.Unstable))
			{
				BeginButtons(now);
				return;
			}
			var current = _controls.Where(x => x.Started && !x.Complete && !x.Unstable).OrderByDescending(x => x.LastMoved).FirstOrDefault();
			Countdown = CountdownCaption = "";
			if (current != null)
			{
				Hint = current.IsStick
					? "Keep turning " + current.Name + " round its edge until the circle is full."
					: "Keep pressing " + current.Name + " slowly all the way in and out until its bar is full.";
				return;
			}
			// Every control moved is full: the ones never moved may be absent, and are let go after a while without movement.
			var next = _controls.First(x => !x.Complete && !x.Unstable);
			var done = _controls.Where(x => x.Complete).OrderByDescending(x => x.CompletedAt).FirstOrDefault();
			Hint = done == null ? "Start with " + next.Name + "." : done.Name + " \u2713. Next: " + next.Name + ".";
			// An unstable control's changes are no movement of the person's.
			var idle = now - Math.Max(_stepStart, _controls.Where(x => !x.Unstable).Select(x => x.LastMoved).DefaultIfEmpty(0).Max());
			if (idle < Frequency * IdleSeconds)
				return;
			var left = Frequency * (IdleSeconds + CountdownSeconds) - idle;
			if (left > 0)
			{
				Countdown = SecondsLeft(left);
				CountdownCaption = "Moving on in";
				return;
			}
			foreach (var control in _controls.Where(x => !x.Complete && !x.Unstable))
				control.NotMoved = true;
			BeginButtons(now);
		}

		/// <summary>Marks a control unstable when it went <see cref="UnstableSeconds"/> without getting fuller while it still changes, as a drifting axis does.</summary>
		void Watch(DeviceTestControl control, long now)
		{
			if (control.Complete || control.Unstable || !control.Started)
				return;
			var progress = control.Progress;
			if (control.ProgressAt == 0 || progress > control.BestProgress + 0.001)
			{
				control.BestProgress = progress;
				control.ProgressAt = now;
				return;
			}
			if (now - control.ProgressAt >= Frequency * UnstableSeconds && now - control.LastMoved <= Frequency * 2)
				control.Unstable = true;
		}

		/// <summary>Ends sticks and triggers, or buttons, at once: what is not done is marked skipped, and not graded.</summary>
		public void Skip(long now)
		{
			if (Step == DeviceTestStep.Sticks)
			{
				foreach (var control in _controls.Where(x => !x.Complete && !x.Unstable))
					control.Skipped = true;
				BeginButtons(now);
			}
			else if (Step == DeviceTestStep.Buttons)
			{
				foreach (var button in Buttons.Where(x => !x.Pressed))
					button.Skipped = true;
				Finish(now);
			}
		}

		/// <summary>Whether the step waits on the person's controls, so Skip can end it.</summary>
		public bool CanSkip { get { return Step == DeviceTestStep.Sticks || Step == DeviceTestStep.Buttons; } }

		/// <summary>Whether two axes are a stick in the usual layout: X and Y, or X and Y Rotation.</summary>
		static bool Usual(DeviceTestPair pair)
		{
			return (pair.X.Channel == 0 && pair.Y.Channel == 1) || (pair.X.Channel == 3 && pair.Y.Channel == 4);
		}

		/// <summary>Pairs the axes into sticks: two that move together round their rest, or else the usual X and Y, and X and Y Rotation, when both rest in the middle.</summary>
		void Pair()
		{
			var used = new HashSet<int>();
			var sticks = new List<DeviceTestPair>();
			// Of two pairings a stick turned round allows, the usual one first.
			foreach (var pair in _pairs.Where(x => x.MovedTogether).OrderByDescending(Usual).ThenByDescending(x => x.Together))
				if (!used.Contains(pair.X.Channel) && !used.Contains(pair.Y.Channel))
				{
					sticks.Add(pair);
					used.Add(pair.X.Channel);
					used.Add(pair.Y.Channel);
				}
			// Two axes resting at an end, such as pedals, are no stick whatever their slots.
			if (!Layout.Wheel)
				foreach (var pair in _pairs.Where(x => Usual(x) && x.X.RestsInMiddle && x.Y.RestsInMiddle))
					if (!used.Contains(pair.X.Channel) && !used.Contains(pair.Y.Channel))
					{
						sticks.Add(pair);
						used.Add(pair.X.Channel);
						used.Add(pair.Y.Channel);
					}
			sticks = sticks.OrderBy(x => x.X.Channel).ToList();
			var key = string.Join(",", sticks.Select(x => x.X.Channel + "-" + x.Y.Channel));
			if (key == _pairing)
				return;
			_pairing = key;
			_controls.Clear();
			for (var i = 0; i < sticks.Count; i++)
			{
				var pair = sticks[i];
				var name = sticks.Count == 1 ? "Stick" : i == 0 ? "Left stick" : i == 1 ? "Right stick" : "Stick " + (i + 1);
				_controls.Add(new DeviceTestControl(name + " (" + Layout.ChannelName(pair.X.Channel) + ", " + Layout.ChannelName(pair.Y.Channel) + ")", pair.X, pair.Y, pair, null));
			}
			foreach (var axis in _axes.Where(x => !used.Contains(x.Channel)))
				_controls.Add(new DeviceTestControl(Layout.ChannelName(axis.Channel), axis, null, null, Layout.LimitsControl(axis.Channel)));
			ControlsVersion++;
		}

		#endregion

		#region Buttons

		void BeginButtons(long now)
		{
			if (Buttons.Length == 0)
			{
				Finish(now);
				return;
			}
			foreach (var button in Buttons)
				button.Begin(_buttons[button.Index]);
			_lastNewPress = now;
			Begin(DeviceTestStep.Buttons, now, "Press each button once.");
		}

		void Press(long ticks, bool[] buttons)
		{
			if (buttons == null)
				return;
			foreach (var button in Buttons)
			{
				var wasPressed = button.Pressed;
				if (button.Index >= buttons.Length || !button.Set(buttons[button.Index], ticks, Frequency) || wasPressed)
					continue;
				// A button ticked for the first time: what went before it is no flicker.
				_lastNewPress = ticks;
				foreach (var other in Buttons)
					other.PressesSinceNew = 0;
			}
		}

		void TickButtons(long now)
		{
			var left = Buttons.Where(x => !x.Pressed).ToList();
			if (left.Count == 0)
			{
				Finish(now);
				return;
			}
			Hint = string.Format("{0} of {1} pressed. Next: button {2}.", Buttons.Length - left.Count, Buttons.Length, left[0].Number);
			Countdown = CountdownCaption = "";
			// A button pressed again and again by itself, with no new one ticked for a while, is flickering: it is
			// marked unstable, and its presses keep the step waiting no longer.
			if (now - _lastNewPress >= Frequency * UnstableSeconds)
				foreach (var button in Buttons.Where(x => !x.Unstable && x.PressesSinceNew >= FlickerPresses && now - x.LastPress <= Frequency * 2))
					button.Unstable = true;
			var idle = now - Math.Max(_stepStart, Buttons.Where(x => !x.Unstable).Select(x => x.LastPress).DefaultIfEmpty(0).Max());
			if (idle < Frequency * ButtonsIdleSeconds)
				return;
			var countdown = Frequency * (ButtonsIdleSeconds + CountdownSeconds) - idle;
			if (countdown > 0)
			{
				Countdown = SecondsLeft(countdown);
				CountdownCaption = "Finishing in";
				return;
			}
			foreach (var button in left)
				button.NotPressed = true;
			Finish(now);
		}

		#endregion

		void Finish(long now)
		{
			Statistics.Stop(now, _reads);
			Step = DeviceTestStep.Done;
			Countdown = CountdownCaption = "";
			Results = _measured ? DeviceTestGrades.Grade(this) : null;
			// The results say it all; with none, what to do.
			Instruction = Results == null ? "Press Start to test the controller again." : "";
			Hint = Results == null ? "It stopped before the controller was measured." : "";
		}

		/// <summary>The whole seconds left, as shown: 1 in the last second.</summary>
		string SecondsLeft(long ticks)
		{
			return Math.Max(1, (int)Math.Ceiling(ticks / (double)Frequency)).ToString();
		}
	}

	/// <summary>One axis or slider while sticks and triggers are moved: where it went, and how much of its range it covered.</summary>
	public sealed class DeviceTestAxis
	{
		internal DeviceTestAxis(int channel)
		{
			Channel = channel;
		}

		/// <summary>The value's index in a read.</summary>
		public int Channel { get; }

		/// <summary>Where the value is, where it rested, and the lowest and highest it went, on the scale of -32,768 to 32,767.</summary>
		public int Value { get; private set; }
		public int Centre { get; private set; }
		public int Min { get; private set; }
		public int Max { get; private set; }

		/// <summary>How many of the 100 equal parts of the range it gave a value in, and how often it changed away from rest.</summary>
		public int BinCount { get; private set; }
		public int Updates { get; private set; }

		/// <summary>Whether it moved away from rest, and when it last moved.</summary>
		public bool Started { get; private set; }
		public long LastMoved { get; private set; }

		readonly bool[] _bins = new bool[DeviceTestSteps.Bins];
		int _mark;

		internal void Begin(int value, int centre)
		{
			Value = Min = Max = _mark = value;
			Centre = centre;
			Array.Clear(_bins, 0, _bins.Length);
			BinCount = 0;
			Updates = 0;
			Started = false;
			LastMoved = 0;
			LowEnd = HighEnd = false;
			Bin(value);
		}

		/// <returns>True when the value moved more than <see cref="DeviceTestSteps.Moved"/> since it last did: movement, not noise.</returns>
		internal bool Add(int value, long ticks)
		{
			if (value == Value)
				return false;
			Value = value;
			if (value < Min)
				Min = value;
			if (value > Max)
				Max = value;
			Bin(value);
			// An end counts once the value came near it and turned back from it: the end measured is then the farthest it went.
			if (!LowEnd && Min <= short.MinValue + DeviceTestSteps.EndTolerance && value >= Min + DeviceTestSteps.Moved)
				LowEnd = true;
			if (!HighEnd && Max >= short.MaxValue - DeviceTestSteps.EndTolerance && value <= Max - DeviceTestSteps.Moved)
				HighEnd = true;
			if (Math.Abs(value - Centre) > DeviceTestSteps.Moved)
			{
				Started = true;
				Updates++;
			}
			if (Math.Abs(value - _mark) <= DeviceTestSteps.Moved)
				return false;
			_mark = value;
			LastMoved = ticks;
			return true;
		}

		void Bin(int value)
		{
			var bin = (int)((value - (long)short.MinValue) * DeviceTestSteps.Bins / (ushort.MaxValue + 1));
			if (_bins[bin])
				return;
			_bins[bin] = true;
			BinCount++;
		}

		/// <summary>Whether the value reached its lowest and its highest end, and turned back from it.</summary>
		public bool LowEnd { get; private set; }

		/// <summary>Whether it rests within an eighth of the range of the middle, as a stick does, rather than at an end, as a trigger or a pedal does.</summary>
		public bool RestsInMiddle { get { return Math.Abs(Centre) <= ushort.MaxValue / 8; } }
		public bool HighEnd { get; private set; }

		/// <summary>The share of the range between the lowest and the highest value it went to.</summary>
		public double RangePercent { get { return 100.0 * (Max - Min) / ushort.MaxValue; } }

		public bool Complete
		{
			get { return LowEnd && HighEnd && BinCount >= DeviceTestSteps.BinsNeeded && Updates >= DeviceTestSteps.UpdatesNeeded; }
		}

		/// <summary>How full it is, 0 to 1: its ends, its parts of the range and its changes, a third each.</summary>
		public double Progress
		{
			get
			{
				if (Complete)
					return 1;
				var ends = (LowEnd ? 0.5 : 0) + (HighEnd ? 0.5 : 0);
				var bins = Math.Min(1.0, BinCount / (double)DeviceTestSteps.BinsNeeded);
				var updates = Math.Min(1.0, Updates / (double)DeviceTestSteps.UpdatesNeeded);
				return Math.Min(0.99, (ends + bins + updates) / 3);
			}
		}
	}

	/// <summary>Two axes, which may be a stick: whether they move together round a centre, and how far out the stick reached in each sector.</summary>
	public sealed class DeviceTestPair
	{
		internal DeviceTestPair(DeviceTestAxis x, DeviceTestAxis y)
		{
			X = x;
			Y = y;
		}

		public DeviceTestAxis X { get; }
		public DeviceTestAxis Y { get; }

		/// <summary>The farthest it reached in each 10-degree sector, where 1 is the edge of the range; sector 0 points right and they go round anticlockwise.</summary>
		public double[] Radius { get; } = new double[DeviceTestSteps.Sectors];

		/// <summary>The sectors reached at the edge: <see cref="DeviceTestSteps.EdgeRadius"/> out or farther.</summary>
		public int SectorsReached { get; private set; }

		/// <summary>The reads both axes were far out together in, by quarter of the circle round their rest.</summary>
		readonly int[] _quarters = new int[4];

		/// <summary>How often both were far out together, and whether that was in three quarters or more, as a stick turned round goes and two triggers do not.</summary>
		internal int Together { get { return _quarters.Sum(); } }
		internal bool MovedTogether { get { return _quarters.Count(x => x >= 5) >= 3; } }

		internal void Clear()
		{
			Array.Clear(Radius, 0, Radius.Length);
			Array.Clear(_quarters, 0, _quarters.Length);
			SectorsReached = 0;
		}

		internal void Add(int x, int y)
		{
			const int far = 16384;
			var dx = x - X.Centre;
			var dy = y - Y.Centre;
			if (Math.Abs(dx) >= far && Math.Abs(dy) >= far)
				_quarters[(dx >= 0 ? 0 : 1) + (dy >= 0 ? 0 : 2)]++;
			var r = Math.Sqrt((double)x * x + (double)y * y) / short.MaxValue;
			// Near the middle the direction means nothing.
			if (r < DeviceTestSteps.EdgeRadius / 2)
				return;
			// Up is the lower value, as a stick reads pushed away.
			var angle = Math.Atan2(-y, x);
			var sector = (int)((angle < 0 ? angle + 2 * Math.PI : angle) * DeviceTestSteps.Sectors / (2 * Math.PI)) % DeviceTestSteps.Sectors;
			if (r <= Radius[sector])
				return;
			if (Radius[sector] < DeviceTestSteps.EdgeRadius && r >= DeviceTestSteps.EdgeRadius)
				SectorsReached++;
			Radius[sector] = r;
		}

		/// <summary>How far the stick's edge is from a true circle, in percent: the average distance of each sector's reach from their average, against that average.</summary>
		public double? CircleError
		{
			get
			{
				var reached = Radius.Where(x => x >= DeviceTestSteps.EdgeRadius).ToArray();
				if (reached.Length == 0)
					return null;
				var mean = reached.Average();
				return 100 * reached.Average(x => Math.Abs(x - mean)) / mean;
			}
		}
	}

	/// <summary>A stick, or a single axis or slider, as the test shows and grades it.</summary>
	public sealed class DeviceTestControl
	{
		internal DeviceTestControl(string name, DeviceTestAxis x, DeviceTestAxis y, DeviceTestPair pair, string limitedBy)
		{
			Name = name;
			X = x;
			Y = y;
			Pair = pair;
			LimitedBy = limitedBy;
		}

		public string Name { get; }
		public DeviceTestAxis X { get; }
		public DeviceTestAxis Y { get; }

		/// <summary>The stick's reach round its edge; null for a single axis.</summary>
		public DeviceTestPair Pair { get; }
		public bool IsStick { get { return Pair != null; } }

		/// <summary>The input method, when it gives this control in a way no test can grade.</summary>
		public string LimitedBy { get; }

		/// <summary>Never moved while the step waited: counted as absent, and not graded.</summary>
		public bool NotMoved { get; internal set; }

		/// <summary>Not full when Skip ended the step: not graded.</summary>
		public bool Skipped { get; internal set; }

		/// <summary>Kept changing for <see cref="DeviceTestSteps.UnstableSeconds"/> without getting fuller, as a drifting or flickering axis does: a finding of its own, and not graded.</summary>
		public bool Unstable { get; internal set; }

		/// <summary>When it was first seen full; and the fullest it was, and when it last got fuller.</summary>
		internal long CompletedAt;
		internal double BestProgress;
		internal long ProgressAt;

		public bool Started { get { return X.Started || (Y != null && Y.Started); } }
		public long LastMoved { get { return Math.Max(X.LastMoved, Y == null ? 0 : Y.LastMoved); } }

		/// <summary>Whether it rests in the middle of its range, as a stick does, rather than at an end, as a trigger does.</summary>
		public bool Centred { get { return IsStick || X.RestsInMiddle; } }

		public bool Complete
		{
			get { return IsStick ? X.Complete && Y.Complete && Pair.SectorsReached == DeviceTestSteps.Sectors : X.Complete; }
		}

		/// <summary>How full it is, 0 to 1; for a stick its edge counts as much as its two axes.</summary>
		public double Progress
		{
			get
			{
				if (Complete)
					return 1;
				if (!IsStick)
					return X.Progress;
				return Math.Min(0.99, (X.Progress + Y.Progress + 2.0 * Pair.SectorsReached / DeviceTestSteps.Sectors) / 4);
			}
		}
	}

	/// <summary>One button: pressed once, and whether one push registered twice.</summary>
	public sealed class DeviceTestButton
	{
		internal DeviceTestButton(int index)
		{
			Index = index;
		}

		public int Index { get; }
		public int Number { get { return Index + 1; } }

		/// <summary>Pressed during the step; never pressed while the step waited, and counted as absent; not pressed when Skip ended the step.</summary>
		public bool Pressed { get; private set; }
		public bool NotPressed { get; internal set; }
		public bool Skipped { get; internal set; }

		/// <summary>Pressed again and again by itself: a flickering button, a finding of its own.</summary>
		public bool Unstable { get; internal set; }

		/// <summary>When it was last pressed, and how often since a button was last ticked for the first time.</summary>
		public long LastPress { get; private set; }
		internal int PressesSinceNew;

		/// <summary>Pressed again within <see cref="DeviceTestSteps.TwiceMilliseconds"/> of being let go, and how soon.</summary>
		public bool Twice { get; private set; }
		public double TwiceAfter { get; private set; }

		bool _down;
		long _released;

		internal void Begin(bool down)
		{
			_down = down;
			_released = 0;
		}

		/// <returns>True on a press.</returns>
		internal bool Set(bool down, long ticks, long frequency)
		{
			if (down == _down)
				return false;
			_down = down;
			if (!down)
			{
				_released = ticks;
				return false;
			}
			var after = _released == 0 ? double.MaxValue : (ticks - _released) * 1000.0 / frequency;
			if (Pressed && after <= DeviceTestSteps.TwiceMilliseconds)
			{
				Twice = true;
				TwiceAfter = after;
			}
			Pressed = true;
			LastPress = ticks;
			PressesSinceNew++;
			return true;
		}
	}
}
