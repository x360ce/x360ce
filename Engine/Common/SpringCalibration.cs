using System;

namespace x360ce.Engine
{
	/// <summary>Finds the weakest centering force that reliably brings a wheel home.</summary>
	/// <remarks>
	/// The wheel is pushed to one stop and let go. The force towards the centre is then raised one
	/// percent at a time until the wheel reaches the centre, and that is the force that overcomes
	/// the wheel's own friction from rest. The same is done from the other stop, because the two
	/// sides of a wheel are rarely the same, and the larger answer plus a margin is the result.
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
		/// <summary>How long each level of the ramp is held before the next.</summary>
		const int StepMs = 300;
		/// <summary>Added to the larger of the two answers, so the result is reliable rather than marginal.</summary>
		const int MarginPercent = 5;

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

		/// <summary>The forces that brought the wheel home from each stop, in percent.</summary>
		public int LowLevel { get; private set; }
		public int HighLevel { get; private set; }

		long phaseStart = -1;
		long steadySince = -1;
		int steadyFrom;
		int level;
		long levelStart;

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
					return Settle(elapsed, nowMs, Phase.RampFromLow);
				case Phase.RampFromLow:
					return Ramp(position, nowMs, 1, Phase.PushToHigh);
				case Phase.PushToHigh:
					return Push(position, nowMs, elapsed, 1, Phase.SettleHigh);
				case Phase.SettleHigh:
					return Settle(elapsed, nowMs, Phase.RampFromHigh);
				case Phase.RampFromHigh:
					return Ramp(position, nowMs, -1, Phase.Done);
				default:
					return 0;
			}
		}

		/// <summary>Pushes towards a stop until the wheel sits there.</summary>
		int Push(int position, long nowMs, long elapsed, int direction, Phase next)
		{
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
		int Settle(long elapsed, long nowMs, Phase next)
		{
			if (elapsed >= SettleMs)
			{
				Enter(next, nowMs);
				level = 1;
				levelStart = nowMs;
			}
			return 0;
		}

		/// <summary>Raises the force towards the centre a percent at a time until the wheel gets there.</summary>
		int Ramp(int position, long nowMs, int direction, Phase next)
		{
			var home = direction > 0 ? position >= Center - CenterTolerance : position <= Center + CenterTolerance;
			if (home)
			{
				if (direction > 0)
					LowLevel = level;
				else
					HighLevel = level;
				if (next == Phase.Done)
					Finish();
				else
					Enter(next, nowMs);
				return 0;
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
		}

		void Finish()
		{
			var larger = Math.Max(LowLevel, HighLevel);
			result = Math.Min(100, (int)Math.Ceiling(larger * (100 + MarginPercent) / 100.0));
			phase = (int)Phase.Done;
		}

		void Fail(string why)
		{
			message = why;
			phase = (int)Phase.Failed;
		}
	}
}
