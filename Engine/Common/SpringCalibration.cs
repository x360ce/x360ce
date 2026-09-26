using System;

namespace x360ce.Engine
{
	/// <summary>Finds the weakest centering force that reliably brings a wheel home, and checks that it holds it there.</summary>
	/// <remarks>
	/// The wheel is pushed to one stop and let go. The force towards the centre is then raised one
	/// percent at a time until the wheel moves at pace, held while it does, and raised again only
	/// if it stalls or slows to a creep: the level it is on when it reaches the centre is the force
	/// that overcomes the wheel's own friction. The same is done from the other stop, starting from
	/// that answer, because the two
	/// sides of a wheel are rarely the same, and the larger answer plus a little extra is the candidate.
	/// Then the spring itself runs at that strength, with its ramp and its damping, from a stop:
	/// the wheel must come home and stay, not swing through the centre, before the candidate is
	/// the answer. A run that ends in a swing says so instead of handing over a number.
	///
	/// This runs on the engine thread, one call per poll, and keeps no time of its own: the caller
	/// says what the time is. Nothing here allocates or waits.
	/// </remarks>
	public class SpringCalibration
	{
		public enum Phase
		{
			PushToLow,
			SettleLow,
			RampFromLow,
			PushToHigh,
			SettleHigh,
			RampFromHigh,
			PushForCheck,
			SettleCheck,
			Check,
			Done,
			Failed,
		}

		/// <summary>The axis as DirectInput reports it, with the centre halfway.</summary>
		public const int AxisMax = 65535;
		public const int Center = AxisMax / 2;

		/// <summary>How far from a stop still counts as being at it, as a share of the whole travel.</summary>
		const int StopZone = AxisMax * 15 / 100;
		/// <summary>How near the centre counts as home.</summary>
		const int CenterTolerance = AxisMax / 100;
		/// <summary>Movement smaller than this over the steady period means the wheel has stopped.</summary>
		const int SteadyMovement = AxisMax / 200;
		const int SteadyMs = 300;

		/// <summary>The push that takes the wheel to a stop, and the stronger one tried when the first is not enough.</summary>
		const int PushPercent = 35;
		const int StrongPushPercent = 70;
		const int PushTimeoutMs = 4000;
		const int SettleMs = 500;
		/// <summary>How long each level of the ramp is held before the next, when the wheel has not moved on it.</summary>
		const int StepMs = 300;
		/// <summary>How long the ramp may take in all, for a wheel that moves but never arrives.</summary>
		const int RampTimeoutMs = 20000;
		/// <summary>The slowest return that counts: from a stop to the centre in this long.</summary>
		/// <remarks>
		/// A floor, not a target. The least force that starts a G27 leaves it creeping: measured warm,
		/// it broke free at 22 percent and would have taken twelve seconds to the centre, while every
		/// two percent more roughly doubled the pace up to 30. A wheel under that force is turning,
		/// so the level is not raised for turning; it is raised for turning too slowly to be a
		/// centering spring at all. Set low, because the strength this finds is what the driver
		/// lives with, and a spring that straightens the car by itself in a few seconds is wanted
		/// over one that fights the hands.
		/// </remarks>
		public const int ReturnWithinMs = 6000;
		/// <summary>How often the pace is read. Short, so the level is held from the moment the wheel is at speed, not a step later.</summary>
		const int PaceWindowMs = 100;
		/// <summary>The least the wheel must move towards the centre in one window for it to count as at pace: the pace of <see cref="ReturnWithinMs"/>.</summary>
		const int PaceMovement = (int)((long)Center * PaceWindowMs / ReturnWithinMs);
		/// <summary>Added to the larger of the two answers, in percent of the device's force: the force that just started the wheel, plus enough over it to start it every time.</summary>
		const int ExtraPercent = 3;
		/// <summary>How long the spring is given, from a stop, to bring the wheel home and hold it: the slowest return that counts, and a second to settle.</summary>
		const int CheckMs = ReturnWithinMs + 1000;
		/// <summary>How many times the wheel may pass through the centre on its way to rest. A swing from side to side passes it every time.</summary>
		const int CrossingsAllowed = 3;

		volatile int phase = (int)Phase.PushToLow;
		volatile int result;
		volatile string message = "";
		volatile bool cancel;

		public Phase Step { get { return (Phase)phase; } }
		/// <summary>The strength found, in percent, or nought when nothing was found.</summary>
		public int Result { get { return result; } }
		/// <summary>Why it stopped, when it stopped without an answer.</summary>
		public string Message { get { return message; } }
		public bool IsFinished { get { return phase >= (int)Phase.Done; } }

		/// <summary>What the run is doing now and what it is after, for the page to show while it runs. Built when read, off the engine thread.</summary>
		public string Status
		{
			get
			{
				var seconds = ReturnWithinMs / 1000;
				switch (Step)
				{
					case Phase.PushToLow:
						return "Hands off the wheel. Pushing it to the left stop.";
					case Phase.SettleLow:
						return "At the left stop. Letting it rest.";
					case Phase.RampFromLow:
						return Ramping("left", LowTurnsAt, seconds);
					case Phase.PushToHigh:
						return string.Format("Left side: turns at {0} %, home in time at {1} %. Pushing to the right stop.", LowTurnsAt, LowLevel);
					case Phase.SettleHigh:
						return "At the right stop. Letting it rest.";
					case Phase.RampFromHigh:
						return Ramping("right", HighTurnsAt, seconds);
					case Phase.PushForCheck:
					case Phase.SettleCheck:
						return string.Format("Left {0} %, right {1} %, plus {2} % to start every time: {3} %. Pushing to a stop to check it.", LowLevel, HighLevel, ExtraPercent, result);
					case Phase.Check:
						return string.Format("Checking {0} %: the wheel must come home and stay, not swing. Crossed the centre {1} time(s).", result, Crossings);
					default:
						return message;
				}
			}
		}

		string Ramping(string side, int turnsAt, int seconds)
		{
			return turnsAt == 0
				? string.Format("From the {0} stop: raising the force, {1} %, until the wheel turns.", side, level)
				: string.Format("From the {0} stop: the wheel turns at {1} %; now {2} %, held while it comes home within {3} s, raised if it creeps.", side, turnsAt, level, seconds);
		}

		/// <summary>The forces that brought the wheel home at pace from each stop, in percent.</summary>
		public int LowLevel { get; private set; }
		public int HighLevel { get; private set; }
		/// <summary>The forces at which the wheel first turned from each stop, in percent: the friction alone, before the pace is asked for.</summary>
		public int LowTurnsAt { get; private set; }
		public int HighTurnsAt { get; private set; }
		/// <summary>How many times the wheel passed through the centre while the answer was checked.</summary>
		public int Crossings { get; private set; }
		/// <summary>The damping the caller applies this poll, in the device's units: none while the wheel is pushed to a stop, the level's own while it is ramped and checked.</summary>
		public int Damping { get; private set; }

		long phaseStart = -1;
		long steadySince = -1;
		int steadyFrom;
		int level;
		long levelStart;
		long paceAt;
		int paceFrom;
		int rampFrom;
		int side;

		/// <summary>Stops the run. The next call answers with no force and Failed.</summary>
		public void Cancel()
		{
			cancel = true;
		}

		/// <summary>One poll. Returns the force to apply, in percent, positive towards the high end of the axis.</summary>
		/// <param name="position">The steering axis, 0 to <see cref="AxisMax"/>.</param>
		/// <param name="nowMs">The time, in milliseconds from any fixed point.</param>
		public int Update(int position, long nowMs)
		{
			if (IsFinished)
				return 0;
			if (cancel)
			{
				Fail("Stopped.");
				return 0;
			}
			if (phaseStart < 0)
				phaseStart = nowMs;
			var elapsed = nowMs - phaseStart;
			switch (Step)
			{
				case Phase.PushToLow:
					return Push(position, nowMs, elapsed, -1, Phase.SettleLow);
				case Phase.SettleLow:
					return Settle(position, elapsed, nowMs, Phase.RampFromLow);
				case Phase.RampFromLow:
					return Ramp(position, nowMs, 1, Phase.PushToHigh);
				case Phase.PushToHigh:
					return Push(position, nowMs, elapsed, 1, Phase.SettleHigh);
				case Phase.SettleHigh:
					return Settle(position, elapsed, nowMs, Phase.RampFromHigh);
				case Phase.RampFromHigh:
					return Ramp(position, nowMs, -1, Phase.PushForCheck);
				case Phase.PushForCheck:
					return Push(position, nowMs, elapsed, -1, Phase.SettleCheck);
				case Phase.SettleCheck:
					return Settle(position, elapsed, nowMs, Phase.Check);
				case Phase.Check:
					return Check(position, nowMs, elapsed);
				default:
					return 0;
			}
		}

		/// <summary>Runs the spring at the candidate strength from a stop and counts the wheel's passes through the centre.</summary>
		int Check(int position, long nowMs, long elapsed)
		{
			var offset = position - Center;
			// A pass is a change of side beyond the home tolerance, so the encoder's own jitter at rest is not one.
			if (Math.Abs(offset) > CenterTolerance)
			{
				var now = Math.Sign(offset);
				if (side != 0 && now != side)
					Crossings++;
				side = now;
			}
			if (elapsed >= CheckMs)
			{
				Damping = 0;
				var home = Math.Abs(offset) <= CenterTolerance;
				if (Crossings <= CrossingsAllowed && home)
					phase = (int)Phase.Done;
				else if (Crossings > CrossingsAllowed)
					Fail(string.Format("{0} % brings the wheel home but it swings through the centre {1} times. Set the strength by hand, lower first.", result, Crossings));
				else
					Fail(string.Format("{0} % brought the wheel home once but not again. Take your hands off it and try again.", result));
				return 0;
			}
			Damping = ForceFeedbackState.DamperFor(result);
			return ForceFeedbackState.SpringForce(position, result);
		}

		/// <summary>Pushes towards a stop until the wheel sits there.</summary>
		int Push(int position, long nowMs, long elapsed, int direction, Phase next)
		{
			Damping = 0;
			var atStop = direction < 0 ? position <= StopZone : position >= AxisMax - StopZone;
			if (atStop && IsSteady(position, nowMs))
			{
				Enter(next, nowMs);
				return 0;
			}
			if (elapsed > PushTimeoutMs * 2)
			{
				Fail("The wheel did not reach its stop. Take your hands off it and try again.");
				return 0;
			}
			var percent = elapsed > PushTimeoutMs ? StrongPushPercent : PushPercent;
			return direction * percent;
		}

		/// <summary>Waits with no force, so the ramp starts from a wheel at rest.</summary>
		int Settle(int position, long elapsed, long nowMs, Phase next)
		{
			if (elapsed >= SettleMs)
			{
				Enter(next, nowMs);
				// The second side starts where the first side's answer was: the result takes the larger
				// of the two, so there is nothing to learn below it, only time to spend.
				level = next == Phase.RampFromHigh ? Math.Max(1, LowLevel) : 1;
				levelStart = nowMs;
				paceAt = nowMs;
				paceFrom = position;
				rampFrom = position;
			}
			return 0;
		}

		/// <summary>Raises the force towards the centre a percent at a time until the wheel moves at a fair pace, and holds it while it does.</summary>
		/// <remarks>
		/// The answer is the force that gets the wheel going against its friction and keeps it going.
		/// A wheel already on its way at pace needs no more, so a step in which it moved enough leaves
		/// the level where it is; it used to rise on the clock alone, and a wheel that took a second
		/// to cross came home under several levels more than it needed, which is what the spring was
		/// then set to. A wheel that merely creeps is given more, or the spring found would straighten
		/// a car in seconds.
		/// </remarks>
		int Ramp(int position, long nowMs, int direction, Phase next)
		{
			var home = direction > 0 ? position >= Center - CenterTolerance : position <= Center + CenterTolerance;
			if (home)
			{
				if (direction > 0)
					LowLevel = level;
				else
				{
					HighLevel = level;
					// Both sides answered: this is the strength the check runs at.
					result = Math.Min(100, Math.Max(LowLevel, HighLevel) + ExtraPercent);
				}
				Enter(next, nowMs);
				return 0;
			}
			if (nowMs - phaseStart > RampTimeoutMs)
			{
				Fail("The wheel moved but did not reach the centre. Take your hands off it and try again.");
				return 0;
			}
			// The level at which the wheel first turned is kept for the telling: friction alone.
			if (direction * (position - rampFrom) > SteadyMovement)
			{
				if (direction > 0 && LowTurnsAt == 0)
					LowTurnsAt = level;
				if (direction < 0 && HighTurnsAt == 0)
					HighTurnsAt = level;
			}
			// While the wheel moves towards the centre at pace, the level is held: each window at pace
			// starts the step over. Stalled or creeping, a step goes by and it gets one more percent.
			if (nowMs - paceAt >= PaceWindowMs)
			{
				if (direction * (position - paceFrom) >= PaceMovement)
					levelStart = nowMs;
				paceAt = nowMs;
				paceFrom = position;
			}
			if (nowMs - levelStart >= StepMs)
			{
				level++;
				levelStart = nowMs;
				if (level > 100)
				{
					Fail("The wheel did not move at full strength. Check that force feedback works on it.");
					return 0;
				}
			}
			// The spring runs with its damping, so the pace is measured with the damping this level brings.
			Damping = ForceFeedbackState.DamperFor(level);
			return direction * level;
		}

		/// <summary>Whether the wheel has hardly moved for the steady period.</summary>
		bool IsSteady(int position, long nowMs)
		{
			if (steadySince < 0 || Math.Abs(position - steadyFrom) > SteadyMovement)
			{
				steadySince = nowMs;
				steadyFrom = position;
				return false;
			}
			return nowMs - steadySince >= SteadyMs;
		}

		void Enter(Phase next, long nowMs)
		{
			phase = (int)next;
			phaseStart = nowMs;
			steadySince = -1;
			if (next == Phase.Check)
			{
				side = 0;
				Crossings = 0;
			}
		}

		void Fail(string why)
		{
			message = why;
			result = 0;
			Damping = 0;
			phase = (int)Phase.Failed;
		}
	}
}
