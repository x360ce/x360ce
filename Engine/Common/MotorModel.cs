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
	///
	/// The method is scripts/motors/measure_motors.py with --steps 20 --hold 3.0: the two grip
	/// motors of the controller (VID_045E&amp;PID_02FF) driven through XInput in 5 % steps, 3 s per
	/// level, and an FFT of the middle of each step from a microphone. The strongest tone in the
	/// motor's band is the rotation frequency (RPM is Hz x 60). The motors are the same
	/// eccentric-mass type the Xbox 360 pad uses. The bands are left 8-35 Hz and right 25-120 Hz.
	/// On a hard surface the shell rattles at 15-19 Hz, which spoilt two runs; a softer placement
	/// removes it. The frequencies by drive level:
	/// <code>
	/// Drive %   Left Hz       Right Hz
	///     5     not spinning  not spinning
	///    10     13.5          weak
	///    15     13.9          41.7
	///    20     14.3          45.2
	///    25     14.8          47.4
	///    30     15.2          48.2
	///    40     16.1          50.0
	///    50     18.7          54.3
	///    60     19.1          56.5
	///    70     20.9          58.2
	///    80     22.2          60.0
	///    90     22.6          61.3
	///   100     25.2          62.2   (1513 and 3730 RPM)
	/// </code>
	/// The fits, with the drive p in percent: left f = 12.2 + 0.13 p for p of 10 or more (it
	/// starts between 5 and 10 %); right f = 38.1 + 0.24 p for p of 15 or more (it starts between
	/// 10 and 15 %). At full drive the right motor spins 2.5 times the left; each motor's speed
	/// changes only 1.5 to 1.9 times across its range.
	///
	/// The constants below round these lines to multiples of 8 ms, which the 4 ms step of the
	/// period sliders reaches: the left motor's 74 -> 40 ms (13.9 -> 25 Hz) becomes 72 -> 40 ms,
	/// starting at 8 % drive; the right motor's 24 -> 16 ms (41.7 -> 62.5 Hz) stays, starting at
	/// 16 %. The largest error is the left start, 72 against 74 ms (3 %); the rest is within 1 %.
	///
	/// A G27 driven by a sine at 30 % on the X actuator (Tests/Common/WheelSweepScratchTest.cs: 11
	/// frequencies, 2 s each), with the spring off and the steering read
	/// at about 500 Hz (328 raw units are 1 degree), swings +/-11.7 degrees at 2 Hz, 3.7 at 4,
	/// 1.3 at 8, 0.7 at 12 and 0.5 at 16 (it barely follows, 13.5 Hz measured), and 0.3-0.6 at 24-64 Hz (gear
	/// rattle only); the swing falls about with the square of the frequency. Multiplier 4 gives
	/// the right motor 10.5-15.5 Hz (+/-0.5-0.7 degrees, a fine buzz: engine) and the left motor
	/// 3.5-6 Hz (+/-2-4 degrees, a shake: curbs). Multiplier 2 puts the right motor at 21-31 Hz,
	/// where nothing is felt; multiplier 8 puts the left motor at 1.7-3 Hz with swings of +/-7-12
	/// degrees, too wide. On a rumble pad a periodic force only modulates the motor's power and
	/// the pad's motor cannot follow 25-60 Hz, so the faithful imitation there is a Constant
	/// force whose magnitude is the XInput speed.
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
