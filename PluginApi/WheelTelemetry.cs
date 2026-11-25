namespace x360ce.Plugins
{
	/// <summary>Live vehicle state a telemetry plugin feeds into x360ce's wheel centering spring.</summary>
	/// <remarks>
	/// A value type copied on the engine thread up to a thousand times a second, so a read makes nothing for the garbage
	/// collector. Leave any unknown field at its neutral value (<see cref="Neutral"/>): NaN for speed, distance, position
	/// and pitch, 0 for angle, the accelerations, impulse and jolt, -1 for gear, and 1 for front grip and damping scale.
	/// </remarks>
	public struct WheelTelemetry
	{
		/// <summary>Vehicle speed in km/h, or NaN when unknown. Replaces the static Speed setting on the Wheel tab.</summary>
		public float SpeedKmh;

		/// <summary>Self-aligning target angle in steering wheel degrees (positive pulls right), or 0 when unknown.</summary>
		public float AlignAngleDeg;

		/// <summary>Front tyre grip multiplier: 1 at full grip or unknown, falling towards 0 during understeer or while airborne.</summary>
		public float FrontGrip;

		/// <summary>Lateral acceleration in G (positive right), or 0 when unknown. Adds cornering weight when <see cref="AlignAngleDeg"/> is 0.</summary>
		public float LateralG;

		/// <summary>Short directional kick for curbs, bumps or hits: -1 left to +1 right, 0 at rest.</summary>
		public float Impulse;

		/// <summary>Short shake, without a direction, for a hit from the front or behind: 0 at rest to 1 for the hardest.</summary>
		public float Jolt;

		/// <summary>Forward and back acceleration in G (positive forward), or 0 when unknown.</summary>
		public float LongitudinalG;

		/// <summary>Up and down acceleration in G (positive up), or 0 when unknown.</summary>
		public float VerticalG;

		/// <summary>The gear the game reports (in Forza's count, 0 reverse and 1 up forward), or -1 when unknown.</summary>
		public int Gear;

		/// <summary>Distance driven in metres since the game started sending, or NaN when unknown.</summary>
		public float DistanceMeters;

		/// <summary>Position in the game's world in metres, or NaN when unknown. Axes as the game sends them (Forza: X east, Y up, Z north).</summary>
		public float PositionX;

		/// <summary>See <see cref="PositionX"/>.</summary>
		public float PositionY;

		/// <summary>See <see cref="PositionX"/>.</summary>
		public float PositionZ;

		/// <summary>Resistance multiplier applied to the wheel's damper: 1 normal, above 1 for heavy surfaces such as gravel or mud.</summary>
		public float DampingScale;

		/// <summary>Normalized engine RPM fraction (0.0 idle to 1.0 redline) for steering wheel RGB/RPM shift LEDs, or NaN when unknown.</summary>
		public float RpmFraction;

		/// <summary>Vehicle pitch angle in degrees (nose up positive), or NaN when unknown. Road grade in percent is <c>tan(pitch) * 100</c>.</summary>
		public float PitchDeg;

		/// <summary>Monotonic timestamp in milliseconds (<c>Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency</c>) when this sample arrived, or 0 when inactive.</summary>
		public long TimestampMs;

		/// <summary>Short name of the detected game or telemetry standard (for example, "Forza Data Out" or "Assetto Corsa"), or null.</summary>
		public string Source;

		/// <summary>A neutral sample that leaves the spring at its own settings.</summary>
		public static WheelTelemetry Neutral
		{
			get
			{
				return new WheelTelemetry
				{
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

	/// <summary>Where a wheel's road and impact feel comes from while live telemetry drives its centering spring.</summary>
	public enum WheelRumbleSource
	{
		/// <summary>The game's rumble plays as the game sends it.</summary>
		Game = 0,
		/// <summary>The game's rumble plays, but quieter for a moment while the gear changes.</summary>
		GameWithoutGearChanges = 1,
		/// <summary>The game's rumble is ignored; hits felt from telemetry shake the wheel instead.</summary>
		Telemetry = 2,
		/// <summary>The game's rumble, quieter while the gear changes, and hits felt from telemetry.</summary>
		Both = 3,
	}
}
