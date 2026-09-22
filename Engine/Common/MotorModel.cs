using System;

namespace x360ce.Engine
{
	/// <summary>What an Xbox controller's two rumble motors do, measured, and how a wheel imitates them.</summary>
	/// <remarks>
	/// Measured 2026-09-20 on an Xbox One controller with a microphone: each motor's rotation
	/// frequency at each drive level (one revolution is one vibration cycle). Both motors are the same
	/// kind; the left carries the heavier weight, so it tops out slower. Speed rises with drive above
	/// a start-up threshold, close to a straight line.
	///
	/// Games follow XInput's naming: the left, low-frequency motor carries the car and what hits it -
	/// collisions, curbs, explosions, gunfire; the right, high-frequency motor carries the fine and
	/// the ambient - engine, road texture, and also phone rings and interface buzzes.
	///
	/// A wheel's motor has to move the rim, whose inertia lets the swing fall with the square of
	/// frequency; a G27 stops following at 12-16 Hz. So a wheel plays both motors slower by one
	/// factor, which keeps them apart and inside what the wheel can move. Everything on the Force
	/// Feedback page that speaks of periods reads its numbers from here; docs/Help.ForceFeedback.md
	/// tells the person the same, and a test keeps the two in step.
	/// </remarks>
	public static class MotorModel
	{
		/// <summary>Rotation frequency of the left motor: at spin start, and the rise to full drive (Hz).</summary>
		public const double LeftHzAtStart = 12.2;
		public const double LeftHzRise = 13.0;
		/// <summary>Rotation frequency of the right motor: at spin start, and the rise to full drive (Hz).</summary>
		public const double RightHzAtStart = 38.1;
		public const double RightHzRise = 24.0;
		/// <summary>Drive below which the motor does not spin at all.</summary>
		public const double LeftStartsAt = 0.08;
		public const double RightStartsAt = 0.16;
		/// <summary>The motors' own periods at full drive, in milliseconds: 1000 / (start + rise).</summary>
		public const int LeftPeriodAtFullMs = 40;
		public const int RightPeriodAtFullMs = 16;

		/// <summary>How many times slower a device plays the motors. 1 is the motors themselves.</summary>
		public static readonly int[] Multipliers = { 1, 2, 3, 4, 5, 8 };
		/// <summary>The multiplier a new mapping gets: the smallest that keeps the right motor inside a G27-class wheel's band.</summary>
		public const int DefaultMultiplier = 4;

		/// <summary>The period a motor is given at full drive on a device that plays it this many times slower.</summary>
		public static int PeriodAtFullMs(int multiplier, bool leftMotor)
		{
			return multiplier * (leftMotor ? LeftPeriodAtFullMs : RightPeriodAtFullMs);
		}

		/// <summary>The multiplier both periods stand for, or null when they are not a pair from <see cref="Multipliers"/>.</summary>
		public static int? MultiplierOf(int leftPeriodMs, int rightPeriodMs)
		{
			foreach (var k in Multipliers)
				if (PeriodAtFullMs(k, true) == leftPeriodMs && PeriodAtFullMs(k, false) == rightPeriodMs)
					return k;
			return null;
		}

		/// <summary>The name a multiplier is offered under: the kind of wheel it suits, by how the motor drives the rim.</summary>
		public static string Describe(int multiplier)
		{
			switch (multiplier)
			{
				case 1: return "1x - Direct drive";
				case 2: return "2x - Direct drive, heavy rim";
				case 3: return "3x - Belt drive";
				case 4: return "4x - Gear drive";
				case 5: return "5x - Gear drive, heavy rim";
				case 8: return "8x - Slow wheel";
				default: return multiplier + "x";
			}
		}

		/// <summary>
		/// The period to play at a drive level, given the period at full drive. A real motor slows as
		/// the drive falls, so the period stretches along the measured line; the stored setting is the
		/// period at full drive, which is what it always played at full drive.
		/// </summary>
		/// <param name="periodAtFullMs">The period the setting holds.</param>
		/// <param name="leftMotor">Which motor's line to follow.</param>
		/// <param name="drive">The game's speed for the motor, 0 to 1.</param>
		public static int PeriodMs(int periodAtFullMs, bool leftMotor, double drive)
		{
			if (periodAtFullMs <= 0)
				return 0;
			var start = leftMotor ? LeftHzAtStart : RightHzAtStart;
			var rise = leftMotor ? LeftHzRise : RightHzRise;
			var d = Math.Max(0.0, Math.Min(1.0, drive));
			var full = start + rise;
			var now = start + rise * d;
			return (int)Math.Round(periodAtFullMs * full / now);
		}
	}
}
