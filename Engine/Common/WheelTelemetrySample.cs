using System;

namespace x360ce.Engine
{
	/// <summary>Live vehicle telemetry sample consumed by <see cref="ForceFeedbackState.UpdateSpring"/> on the 1000 Hz engine path.</summary>
	/// <remarks>
	/// Value type so the engine copies it with zero allocations. Populated from an <c>IPluginWheelTelemetry</c> provider
	/// or a test harness; <see cref="Active"/> is false when no game is sending telemetry.
	/// </remarks>
	public struct WheelTelemetrySample
	{
		/// <summary>True when a telemetry source produced this sample.</summary>
		public bool Active;

		/// <summary>Vehicle speed in km/h, or NaN when unknown.</summary>
		public float SpeedKmh;

		/// <summary>Self-aligning target angle in steering wheel degrees (positive pulls right).</summary>
		public float AlignAngleDeg;

		/// <summary>Front tyre grip multiplier, 0 (full understeer / airborne) to 1 (full grip).</summary>
		public float FrontGrip;

		/// <summary>Lateral acceleration in G (positive right).</summary>
		public float LateralG;

		/// <summary>Short directional kick for curbs/bumps (-1 left to +1 right).</summary>
		public float Impulse;

		/// <summary>Short shake without a direction for a hit from the front or behind (0 at rest to 1).</summary>
		public float Jolt;

		/// <summary>Forward and back acceleration in G (positive forward), or 0 when unknown.</summary>
		public float LongitudinalG;

		/// <summary>Up and down acceleration in G (positive up), or 0 when unknown.</summary>
		public float VerticalG;

		/// <summary>The gear the game reports, or -1 when unknown.</summary>
		public int Gear;

		/// <summary>Distance driven in metres, or NaN when unknown.</summary>
		public float DistanceMeters;

		/// <summary>Position in the game's world in metres, or NaN when unknown.</summary>
		public float PositionX;

		/// <summary>See <see cref="PositionX"/>.</summary>
		public float PositionY;

		/// <summary>See <see cref="PositionX"/>.</summary>
		public float PositionZ;

		/// <summary>Damper resistance multiplier (1 normal, &gt; 1 heavy surface).</summary>
		public float DampingScale;

		/// <summary>Normalized engine RPM fraction (0.0 idle to 1.0 redline) for wheel RGB/RPM shift LEDs, or NaN when unknown.</summary>
		public float RpmFraction;

		/// <summary>Vehicle pitch angle in degrees (nose up positive), or NaN when unknown.</summary>
		public float PitchDeg;

		/// <summary>Monotonic timestamp in milliseconds when the sample arrived.</summary>
		public long TimestampMs;

		/// <summary>Short source label (interned/constant string), or null.</summary>
		public string Source;

		/// <summary>Inactive neutral sample.</summary>
		public static WheelTelemetrySample Inactive
		{
			get
			{
				return new WheelTelemetrySample
				{
					Active = false,
					SpeedKmh = float.NaN,
					AlignAngleDeg = 0f,
					FrontGrip = 1f,
					LateralG = 0f,
					Impulse = 0f,
					Jolt = 0f,
					LongitudinalG = 0f,
					VerticalG = 0f,
					Gear = -1,
					DistanceMeters = float.NaN,
					PositionX = float.NaN,
					PositionY = float.NaN,
					PositionZ = float.NaN,
					DampingScale = 1f,
					RpmFraction = float.NaN,
					PitchDeg = float.NaN,
					TimestampMs = 0,
					Source = null,
				};
			}
		}
	}

	/// <summary>User-tunable multipliers for how live telemetry shapes the wheel centering spring (zero-allocation value type).</summary>
	public struct WheelTelemetryTuning
	{
		/// <summary>Aligning torque / counter-steer scale in percent (0 to 200, default 100).</summary>
		public int AlignStrengthPercent;

		/// <summary>Front-tyre understeer lightening scale in percent (0 to 100, default 100).</summary>
		public int UndersteerStrengthPercent;

		/// <summary>Road / curb impulse scale in percent (0 to 100, default 100).</summary>
		public int CurbStrengthPercent;

		/// <summary>Telemetry surface damping scale in percent (0 to 200, default 100).</summary>
		public int DampingStrengthPercent;

		/// <summary>Whether a hit felt from telemetry (<see cref="WheelTelemetrySample.Jolt"/>) shakes the wheel; false leaves the shake to the game's rumble.</summary>
		public bool JoltEnabled;

		/// <summary>Default 1:1 tuning (100 % across all channels), with the shake left to the game's rumble.</summary>
		public static WheelTelemetryTuning Default
		{
			get
			{
				return new WheelTelemetryTuning
				{
					AlignStrengthPercent = 100,
					UndersteerStrengthPercent = 100,
					CurbStrengthPercent = 100,
					DampingStrengthPercent = 100,
				};
			}
		}
	}

	/// <summary>The parts the centering spring's force was made of on the last poll, for a recording to show how telemetry turned into force.</summary>
	/// <remarks>A value type filled in place by <see cref="WheelTelemetryMath.ComputeSpringForce(SpringCurve, int, int, int, int, WheelTelemetrySample, long, double, int, WheelTelemetryTuning, ref SpringTerms, out double)"/>, so the engine makes nothing.</remarks>
	public struct SpringTerms
	{
		/// <summary>The steering axis, 0 to <see cref="SpringCalibration.AxisMax"/>.</summary>
		public int Position;
		/// <summary>The range, lock to lock, the degrees are counted against.</summary>
		public int RangeDegrees;
		/// <summary>The speed the curve was set for, in km/h.</summary>
		public int SpeedKmh;
		/// <summary>How fresh the telemetry was, 0 to 1.</summary>
		public double Weight;
		/// <summary>How far telemetry moved the spring's centre, in wheel degrees (positive right).</summary>
		public double AlignDeg;
		/// <summary>The curve's force at the moved centre, in percent.</summary>
		public int CurveForce;
		/// <summary>The front grip the car's part of the force was scaled by, 0 to 1.</summary>
		public double Grip;
		/// <summary>The curb or bump kick added, in percent.</summary>
		public double ImpulsePercent;
		/// <summary>The shake added for a hit from the front or behind, in percent, its sign alternating.</summary>
		public double JoltPercent;
		/// <summary>The soft lock's bump stop took over.</summary>
		public bool SoftLock;
		/// <summary>The force sent, in percent (-100 to 100).</summary>
		public int Percent;
	}

	/// <summary>Pure, allocation-free wheel physics calculations combining <see cref="SpringCurve"/>, live telemetry, rumble understeer and software soft-lock.</summary>
	public static class WheelTelemetryMath
	{
		/// <summary>How long a telemetry sample stays at full weight before decaying back to static settings, in milliseconds.</summary>
		public const long StaleMs = 500;

		/// <summary>How long stale telemetry fades smoothly back to neutral so the wheel never snaps when a game pauses or exits.</summary>
		public const long DecayMs = 250;

		/// <summary>Maximum self-aligning equilibrium shift allowed from telemetry, in degrees.</summary>
		public const double MaxAlignAngleDeg = 90.0;

		/// <summary>Maximum directional impulse contribution on top of the curve, in percent of device force.</summary>
		public const double MaxImpulsePercent = 40.0;

		/// <summary>Half a period of the jolt's shake in milliseconds: the push changes side every 50 ms, a 100 ms period.</summary>
		public const long JoltHalfPeriodMs = 50;

		/// <summary>Degrees past the soft-lock stop over which the virtual bump stop ramps to full opposing force.</summary>
		public const double SoftLockRampDegrees = 4.0;

		static readonly double TicksToMs = 1000.0 / System.Diagnostics.Stopwatch.Frequency;

		/// <summary>Current monotonic clock in milliseconds, matching <c>WheelTelemetry.TimestampMs</c>.</summary>
		public static long MonotonicNowMs()
		{
			return (long)(System.Diagnostics.Stopwatch.GetTimestamp() * TicksToMs);
		}

		/// <summary>Weight of a telemetry sample from 1.0 (fresh) decaying smoothly to 0.0 once stale.</summary>
		public static double Freshness(long timestampMs, long nowMs)
		{
			if (timestampMs <= 0)
				return 0.0;
			var age = nowMs - timestampMs;
			if (age < 0)
				return 1.0;
			if (age <= StaleMs)
				return 1.0;
			var over = age - StaleMs;
			if (over >= DecayMs)
				return 0.0;
			return 1.0 - (double)over / DecayMs;
		}

		/// <summary>The speed in km/h fed into <see cref="SpringCurve"/>, blending live telemetry smoothly on staleness.</summary>
		public static int EffectiveSpeed(int settingSpeed, int? speedOverride, WheelTelemetrySample telemetry, long nowMs)
		{
			var baseSpeed = speedOverride ?? settingSpeed;
			if (!telemetry.Active || float.IsNaN(telemetry.SpeedKmh) || float.IsInfinity(telemetry.SpeedKmh) || telemetry.SpeedKmh < 0f)
				return baseSpeed;
			var weight = Freshness(telemetry.TimestampMs, nowMs);
			if (weight <= 0.0)
				return baseSpeed;
			var target = Math.Min(500.0, (double)telemetry.SpeedKmh);
			return (int)Math.Round(baseSpeed + (target - baseSpeed) * weight);
		}

		/// <summary>
		/// Computes the signed steering force in percent (-100 to +100, positive towards high end of axis)
		/// and the effective damper multiplier from the curve, live telemetry, optional rumble understeer,
		/// and optional universal software soft-lock.
		/// </summary>
		/// <param name="curve">Spring curve already built for the current settings and effective speed.</param>
		/// <param name="position">Steering axis reading, 0 to <see cref="SpringCalibration.AxisMax"/>.</param>
		/// <param name="rangeDegrees">Physical wheel range lock to lock that degrees are counted against.</param>
		/// <param name="frictionPercent">Wheel friction floor in percent, preserved while scaling the car's portion.</param>
		/// <param name="springStrength">Configured spring strength in percent; 0 disables the spring.</param>
		/// <param name="telemetry">Latest vehicle telemetry snapshot (inactive when none).</param>
		/// <param name="nowMs">Monotonic clock in milliseconds.</param>
		/// <param name="rumbleUndersteerAttenuation">Zero-telemetry grip attenuation (0 = none, up to 0.6) driven by sustained rumble while turning.</param>
		/// <param name="softLockRangeDegrees">Optional virtual soft-lock range lock to lock (&gt; 0 and &lt; <paramref name="rangeDegrees"/> to activate; 0 = off).</param>
		/// <param name="dampingMultiplier">Effective damper multiplier (1.0 when neutral).</param>
		public static int ComputeSpringForce(
			SpringCurve curve,
			int position,
			int rangeDegrees,
			int frictionPercent,
			int springStrength,
			WheelTelemetrySample telemetry,
			long nowMs,
			double rumbleUndersteerAttenuation,
			int softLockRangeDegrees,
			out double dampingMultiplier)
		{
			return ComputeSpringForce(curve, position, rangeDegrees, frictionPercent, springStrength,
				telemetry, nowMs, rumbleUndersteerAttenuation, softLockRangeDegrees, WheelTelemetryTuning.Default, out dampingMultiplier);
		}

		/// <summary>
		/// Computes the signed steering force in percent (-100 to +100, positive towards high end of axis)
		/// and the effective damper multiplier from the curve, live telemetry, user tuning scales, optional rumble understeer,
		/// and optional universal software soft-lock.
		/// </summary>
		public static int ComputeSpringForce(
			SpringCurve curve,
			int position,
			int rangeDegrees,
			int frictionPercent,
			int springStrength,
			WheelTelemetrySample telemetry,
			long nowMs,
			double rumbleUndersteerAttenuation,
			int softLockRangeDegrees,
			WheelTelemetryTuning tuning,
			out double dampingMultiplier)
		{
			var terms = new SpringTerms();
			return ComputeSpringForce(curve, position, rangeDegrees, frictionPercent, springStrength, telemetry, nowMs,
				rumbleUndersteerAttenuation, softLockRangeDegrees, tuning, ref terms, out dampingMultiplier);
		}

		/// <summary>
		/// Computes the signed steering force as above, and fills <paramref name="terms"/> with the parts it was made of.
		/// </summary>
		public static int ComputeSpringForce(
			SpringCurve curve,
			int position,
			int rangeDegrees,
			int frictionPercent,
			int springStrength,
			WheelTelemetrySample telemetry,
			long nowMs,
			double rumbleUndersteerAttenuation,
			int softLockRangeDegrees,
			WheelTelemetryTuning tuning,
			ref SpringTerms terms,
			out double dampingMultiplier)
		{
			dampingMultiplier = 1.0;
			terms.Position = position;
			terms.RangeDegrees = rangeDegrees;
			terms.Weight = 0.0;
			terms.AlignDeg = 0.0;
			terms.CurveForce = 0;
			terms.Grip = 1.0;
			terms.ImpulsePercent = 0.0;
			terms.JoltPercent = 0.0;
			terms.SoftLock = false;
			terms.Percent = 0;
			if (springStrength <= 0 || curve == null)
				return 0;

			var physicalRange = rangeDegrees > 0 ? rangeDegrees : ForceFeedbackState.DefaultRangeDegrees;
			var weight = telemetry.Active ? Freshness(telemetry.TimestampMs, nowMs) : 0.0;
			var alignScale = Clamp(tuning.AlignStrengthPercent / 100.0, 0.0, 2.0);
			var understeerScale = Clamp(tuning.UndersteerStrengthPercent / 100.0, 0.0, 1.0);
			var curbScale = Clamp(tuning.CurbStrengthPercent / 100.0, 0.0, 1.0);
			var dampingScale = Clamp(tuning.DampingStrengthPercent / 100.0, 0.0, 2.0);

			// 1. Self-aligning equilibrium offset (counter-steer pull during oversteer / cornering).
			var alignDeg = 0.0;
			if (weight > 0.0 && alignScale > 0.0)
			{
				if (IsFinite(telemetry.AlignAngleDeg) && Math.Abs(telemetry.AlignAngleDeg) > 0.001f)
					alignDeg = Clamp(telemetry.AlignAngleDeg * alignScale, -MaxAlignAngleDeg, MaxAlignAngleDeg) * weight;
				else if (IsFinite(telemetry.LateralG) && Math.Abs(telemetry.LateralG) > 0.001f)
					alignDeg = Clamp(telemetry.LateralG * 4.0 * alignScale, -25.0, 25.0) * weight;
			}
			var alignUnits = (int)Math.Round(alignDeg * SpringCalibration.AxisMax / physicalRange);
			var shiftedPosition = ClampInt(position - alignUnits, 0, SpringCalibration.AxisMax);

			// 2. Base curve force with front-grip lightening (scales only the car's torque above the wheel's friction floor).
			var rawForce = curve.Force(shiftedPosition);
			var effectiveGrip = 1.0;
			var hasTelemetryGrip = weight > 0.0 && IsFinite(telemetry.FrontGrip) && telemetry.FrontGrip >= 0f && telemetry.FrontGrip < 0.999f;
			if (hasTelemetryGrip)
			{
				var clampedGrip = Clamp(telemetry.FrontGrip, 0.0, 1.0);
				effectiveGrip = 1.0 - (1.0 - clampedGrip) * weight * understeerScale;
			}
			else if (rumbleUndersteerAttenuation > 0.0 && Math.Abs(position - SpringCalibration.Center) > curve.CentreUnits)
			{
				effectiveGrip = Clamp(1.0 - rumbleUndersteerAttenuation, 0.25, 1.0);
			}

			double force = rawForce;
			if (effectiveGrip < 0.999 && rawForce != 0)
			{
				var mag = Math.Abs(rawForce);
				var floor = Math.Min(mag, Math.Max(0, frictionPercent));
				var carPart = mag - floor;
				force = Math.Sign(rawForce) * (floor + carPart * effectiveGrip);
			}

			terms.Weight = weight;
			terms.AlignDeg = alignDeg;
			terms.CurveForce = rawForce;
			terms.Grip = effectiveGrip;

			// 3. Directional curb / suspension impulse from telemetry.
			if (weight > 0.0 && curbScale > 0.0 && IsFinite(telemetry.Impulse) && Math.Abs(telemetry.Impulse) > 0.001f)
			{
				terms.ImpulsePercent = Clamp(telemetry.Impulse, -1.0, 1.0) * MaxImpulsePercent * curbScale * weight;
				force += terms.ImpulsePercent;
			}

			// 3b. Shake for a hit from the front or behind: the same push as a kick, changing side every half period.
			if (tuning.JoltEnabled && weight > 0.0 && curbScale > 0.0 && IsFinite(telemetry.Jolt) && telemetry.Jolt > 0.001f)
			{
				var side = (nowMs / JoltHalfPeriodMs) % 2 == 0 ? 1.0 : -1.0;
				terms.JoltPercent = side * Clamp(telemetry.Jolt, 0.0, 1.0) * MaxImpulsePercent * curbScale * weight;
				force += terms.JoltPercent;
			}

			// 4. Surface damping multiplier from telemetry.
			if (weight > 0.0 && IsFinite(telemetry.DampingScale) && telemetry.DampingScale > 0f)
			{
				var targetDamper = Clamp(telemetry.DampingScale, 0.2, 3.0);
				dampingMultiplier = Clamp(1.0 + (targetDamper - 1.0) * weight * dampingScale, 0.2, 3.0);
			}

			// 5. Universal software soft-lock bump stop for wheels without hardware range control.
			if (softLockRangeDegrees > 0 && softLockRangeDegrees < physicalRange)
			{
				var halfLockUnits = (int)Math.Round(SpringCalibration.Center * (softLockRangeDegrees / (double)physicalRange));
				var offset = position - SpringCalibration.Center;
				var absOffset = Math.Abs(offset);
				if (absOffset > halfLockUnits)
				{
					var rampUnits = Math.Max(1.0, SoftLockRampDegrees * SpringCalibration.AxisMax / physicalRange);
					var penetration = Clamp((absOffset - halfLockUnits) / rampUnits, 0.0, 1.0);
					// Push strictly back toward centre: positive offset pushes negative (left), negative pushes positive (right).
					var stopDirection = offset > 0 ? -1.0 : 1.0;
					var stopForce = stopDirection * (35.0 + 65.0 * penetration);
					force = stopDirection < 0 ? Math.Min(force, stopForce) : Math.Max(force, stopForce);
					terms.SoftLock = true;
				}
			}

			terms.Percent = ClampInt((int)Math.Round(force), -100, 100);
			return terms.Percent;
		}

		static bool IsFinite(float v)
		{
			return !float.IsNaN(v) && !float.IsInfinity(v);
		}

		static double Clamp(double value, double min, double max)
		{
			return value < min ? min : value > max ? max : value;
		}

		static int ClampInt(int value, int min, int max)
		{
			return value < min ? min : value > max ? max : value;
		}
	}
}
