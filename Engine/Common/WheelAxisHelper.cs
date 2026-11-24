using System;

namespace x360ce.Engine
{
	/// <summary>Per-device state for smoothed steering velocity feed-forward (lead compensation).</summary>
	public struct SteeringLeadState
	{
		/// <summary>Normalized steering angle (-1 to +1) on the previous pass.</summary>
		public double LastNorm;

		/// <summary>Low-pass filtered angular velocity in normalized units per second.</summary>
		public double FilteredVelocity;

		/// <summary>Stopwatch timestamp of the previous pass.</summary>
		public long LastTicks;

		/// <summary>True once at least one sample has been recorded.</summary>
		public bool Initialized;
	}

	/// <summary>
	/// Allocation-free steering axis processor providing universal software soft-lock scaling and
	/// optional steering-velocity feed-forward to counter gamepad thumbstick smoothing lag.
	/// </summary>
	public static class WheelAxisHelper
	{
		/// <summary>Time constant in seconds for smoothing raw encoder velocity steps before adding lead.</summary>
		const double VelocityFilterTauSeconds = 0.025;

		/// <summary>Maximum allowed lead time in milliseconds so rapid jerks cannot destabilize steering.</summary>
		public const int MaxLeadMs = 80;

		/// <summary>
		/// Scales a raw DirectInput steering axis (0..65535) for a narrower software soft-lock range and
		/// optionally adds smoothed steering-velocity lead compensation.
		/// </summary>
		/// <param name="raw">Raw steering axis value, 0 to 65535.</param>
		/// <param name="softLockScale">Multiplier &gt;= 1.0 (physicalRange / softLockRange), or 1.0 when soft-lock is off.</param>
		/// <param name="leadMs">Velocity feed-forward lookahead in milliseconds (0 = off, up to <see cref="MaxLeadMs"/>).</param>
		/// <param name="nowTicks">Current monotonic <c>Stopwatch.GetTimestamp()</c>.</param>
		/// <param name="ticksPerSecond"><c>Stopwatch.Frequency</c>.</param>
		/// <param name="state">Per-device filter state updated in place.</param>
		public static ushort ProcessSteeringAxis(ushort raw, double softLockScale, int leadMs, long nowTicks, long ticksPerSecond, ref SteeringLeadState state)
		{
			var hasScale = softLockScale > 1.0001;
			var clampedLead = leadMs < 0 ? 0 : leadMs > MaxLeadMs ? MaxLeadMs : leadMs;
			if (!hasScale && clampedLead == 0)
			{
				state.Initialized = false;
				state.FilteredVelocity = 0.0;
				return raw;
			}

			const double centre = SpringCalibration.Center;
			var norm = (raw - centre) / centre;
			if (hasScale)
			{
				norm *= softLockScale;
				if (norm < -1.0) norm = -1.0;
				else if (norm > 1.0) norm = 1.0;
			}

			var output = norm;
			if (clampedLead > 0 && ticksPerSecond > 0)
			{
				if (!state.Initialized)
				{
					state.LastNorm = norm;
					state.FilteredVelocity = 0.0;
					state.LastTicks = nowTicks;
					state.Initialized = true;
				}
				else
				{
					var dt = (nowTicks - state.LastTicks) / (double)ticksPerSecond;
					if (dt > 0.0001 && dt < 0.25)
					{
						var instantVel = (norm - state.LastNorm) / dt;
						var alpha = dt / (VelocityFilterTauSeconds + dt);
						state.FilteredVelocity += alpha * (instantVel - state.FilteredVelocity);
					}
					else if (dt >= 0.25)
					{
						state.FilteredVelocity = 0.0;
					}
					state.LastNorm = norm;
					state.LastTicks = nowTicks;
					output = norm + state.FilteredVelocity * (clampedLead / 1000.0);
					if (output < -1.0) output = -1.0;
					else if (output > 1.0) output = 1.0;
				}
			}

			var result = (int)Math.Round(output * 32767.5 + 32767.5);
			if (result < 0) result = 0;
			else if (result > ushort.MaxValue) result = ushort.MaxValue;
			return (ushort)result;
		}
	}
}
