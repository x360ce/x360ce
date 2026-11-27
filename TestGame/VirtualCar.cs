using System;
using System.Collections.Generic;
using System.Drawing;

namespace x360ce.TestGame
{
	/// <summary>Standard racing telemetry UDP format emitted by the Virtual Car.</summary>
	public enum VirtualCarProtocol
	{
		ForzaDataOut,
		EaF1Udp,
		CodemastersUdp,
		OutSimUdp,
		ProjectCars2Udp,
	}

	/// <summary>Track surface preset controlling peak lateral grip and road bumpiness.</summary>
	public enum VirtualCarSurface
	{
		DryTarmac,
		WetTarmac,
		Gravel,
		Ice,
	}

	/// <summary>
	/// Lightweight 2D bicycle-model virtual car on a closed oval/circuit track with curbs, surface grip,
	/// drift/understeer dynamics, optional auto-drive cruise, and standard UDP telemetry encoders.
	/// </summary>
	/// <remarks>
	/// Designed so anyone (or an AI assistant driving x360ce's Remote MCP Controller / Test Controller)
	/// can test live wheel telemetry, centering spring physics, counter-steer, understeer lightening,
	/// curb impulses and port coexistence without launching a full racing game.
	/// </remarks>
	public sealed class VirtualCarSimulation
	{
		const double Gravity = 9.80665;
		const double RadToDeg = 180.0 / Math.PI;
		const double DegToRad = Math.PI / 180.0;

		// Track centreline is a rounded stadium oval in metres (X: -120..+120, Y: -65..+65).
		public const double TrackStraightHalf = 55.0;
		public const double TrackCornerRadius = 55.0;
		public const double TrackHalfWidth = 11.0;
		public const double CurbOuterWidth = 2.6;

		// Car state in track coordinates (metres, radians; heading 0 = +X right, +PI/2 = +Y down).
		public double X { get; private set; }
		public double Y { get; private set; }
		public double HeadingRad { get; private set; }

		/// <summary>Forward speed along car heading in m/s.</summary>
		public double SpeedMs { get; private set; }
		/// <summary>Sideways body velocity in m/s (positive = sliding right).</summary>
		public double LateralVelocityMs { get; private set; }
		/// <summary>Yaw angular velocity in rad/s.</summary>
		public double YawRateRad { get; private set; }

		// Inputs (-1..+1 steering, 0..1 throttle & brake).
		public float SteeringInput { get; set; }
		public float ThrottleInput { get; set; }
		public float BrakeInput { get; set; }
		public bool Handbrake { get; set; }
		public bool AutoDrive { get; set; }
		public VirtualCarSurface Surface { get; set; } = VirtualCarSurface.DryTarmac;

		/// <summary>Simulated road grade in percent (-20 % downhill to +20 % uphill), encoded as Pitch radians in telemetry packets.</summary>
		public float RoadGradePercent { get; set; }

		/// <summary>Vehicle pitch angle in radians derived from <see cref="RoadGradePercent"/> (nose up positive).</summary>
		public float PitchRad { get { return (float)Math.Atan(RoadGradePercent / 100.0); } }

		// Computed live telemetry outputs.
		public float SpeedKmh { get { return (float)(Math.Sqrt(SpeedMs * SpeedMs + LateralVelocityMs * LateralVelocityMs) * 3.6); } }
		public int Gear { get; private set; } = 1;
		public float Rpm { get; private set; } = 1000f;
		public float MaxRpm { get { return 7800f; } }
		public float LateralG { get; private set; }
		public float FrontGrip { get; private set; } = 1f;
		public float FrontSlip { get; private set; }
		public float BodySlipDeg { get; private set; }
		public float AlignAngleDeg { get; private set; }
		public float CurbImpulse { get; private set; }
		public float RumbleLeft { get; private set; }
		public float RumbleRight { get; private set; }
		public bool OnCurb { get; private set; }
		public double ElapsedSeconds { get; private set; }

		// Recent trail points for drawing skid/path marks on the track canvas.
		public readonly List<PointF> Trail = new List<PointF>(240);
		public readonly List<bool> TrailSlipping = new List<bool>(240);

		float _manualCurbTimer;
		float _manualCurbDir;
		double _trailAccum;
		double _autoDrivePhase;

		public VirtualCarSimulation()
		{
			Reset();
		}

		/// <summary>Places the car on the bottom straight pointing right at standstill.</summary>
		public void Reset()
		{
			X = 0.0;
			Y = TrackCornerRadius;
			HeadingRad = 0.0;
			SpeedMs = 0.0;
			LateralVelocityMs = 0.0;
			YawRateRad = 0.0;
			Gear = 1;
			Rpm = 1000f;
			LateralG = 0f;
			FrontGrip = 1f;
			FrontSlip = 0f;
			BodySlipDeg = 0f;
			AlignAngleDeg = 0f;
			CurbImpulse = 0f;
			RumbleLeft = 0f;
			RumbleRight = 0f;
			OnCurb = false;
			_manualCurbTimer = 0f;
			_manualCurbDir = 0f;
			Trail.Clear();
			TrailSlipping.Clear();
		}

		/// <summary>Triggers a short left (-1) or right (+1) curb strike impulse for immediate FFB testing.</summary>
		public void TriggerCurb(float direction)
		{
			_manualCurbDir = direction < 0f ? -1f : 1f;
			_manualCurbTimer = 0.35f;
		}

		/// <summary>Advances the car physics by <paramref name="dt"/> seconds.</summary>
		public void Step(double dt)
		{
			if (dt <= 0.0 || dt > 0.25)
				dt = 1.0 / 60.0;
			ElapsedSeconds += dt;

			var steer = Clamp(SteeringInput, -1.0, 1.0);
			var throttle = Clamp(ThrottleInput, 0.0, 1.0);
			var brake = Clamp(BrakeInput, 0.0, 1.0);
			var handbrake = Handbrake;

			// Optional Auto-Drive follows the oval centreline smoothly at ~115 km/h with occasional apex curb brushes.
			if (AutoDrive)
			{
				_autoDrivePhase += dt;
				double targetSteer;
				double targetSpeedMs;
				ComputeAutoDrive(out targetSteer, out targetSpeedMs);
				// Manual input overrides auto-drive immediately when moved.
				if (Math.Abs(steer) < 0.05)
					steer = targetSteer;
				if (throttle < 0.05 && brake < 0.05)
				{
					if (SpeedMs < targetSpeedMs)
						throttle = Clamp((targetSpeedMs - SpeedMs) * 0.14, 0.15, 0.85);
					else if (SpeedMs > targetSpeedMs + 2.5)
						brake = Clamp((SpeedMs - targetSpeedMs) * 0.12, 0.0, 0.7);
				}
			}

			double surfaceGrip;
			double surfaceRoughness;
			SurfaceParameters(Surface, out surfaceGrip, out surfaceRoughness);

			// Longitudinal acceleration, aerodynamic drag and braking.
			const double maxAccel = 11.5;   // ~0-100 km/h in ~4.5 s
			const double maxBrake = 17.5;
			var accel = throttle * maxAccel * (1.0 - SpeedMs / 75.0) - brake * maxBrake - (handbrake ? 7.0 : 0.0);
			var drag = 0.0022 * SpeedMs * Math.Abs(SpeedMs) + 0.35 * SpeedMs / (Math.Abs(SpeedMs) + 1.0);
			SpeedMs = Clamp(SpeedMs + (accel - drag) * dt, 0.0, 68.0); // up to ~245 km/h

			// Steering angle tapers gently at very high speed for stability.
			var maxSteerAngleRad = (32.0 - 16.0 * Clamp(SpeedMs / 60.0, 0.0, 1.0)) * DegToRad;
			var wheelAngle = steer * maxSteerAngleRad;

			// Target yaw rate from bicycle kinematics (wheelbase = 2.6 m).
			const double wheelbase = 2.6;
			var idealYaw = SpeedMs > 0.5 ? (SpeedMs / wheelbase) * Math.Tan(wheelAngle) : 0.0;
			var maxLatAccel = surfaceGrip * Gravity;
			var maxYaw = SpeedMs > 1.0 ? maxLatAccel / SpeedMs : 0.0;

			// Front slip ratio (> 1 means pushing past grip limit -> understeer).
			var demandedLatAccel = Math.Abs(idealYaw * SpeedMs);
			var demandRatio = maxLatAccel > 0.1 ? demandedLatAccel / maxLatAccel : 0.0;
			var frontSlip = demandRatio <= 0.75 ? demandRatio * 0.8 : 0.6 + (demandRatio - 0.75) * 1.9;
			if (brake > 0.75 && SpeedMs > 8.0)
				frontSlip += (brake - 0.75) * 2.4; // lockup under heavy braking
			FrontSlip = (float)Clamp(frontSlip, 0.0, 3.5);
			FrontGrip = (float)Clamp(1.0 - Math.Max(0.0, frontSlip - 0.8) * 0.7, 0.18, 1.0);

			// Achieved yaw rate saturates with front grip; handbrake kicks the rear out (oversteer drift).
			var clampedYaw = Clamp(idealYaw, -maxYaw, maxYaw);
			var driftBoost = handbrake && SpeedMs > 5.0 ? Math.Sign(steer == 0 ? YawRateRad : steer) * 0.85 : 0.0;
			var targetYaw = clampedYaw + driftBoost;
			YawRateRad += (targetYaw - YawRateRad) * Clamp(dt * 9.0, 0.0, 1.0);
			HeadingRad += YawRateRad * dt;

			// Lateral slip velocity builds during hard cornering / handbrake drifts and decays with grip.
			var targetLatVel = handbrake && SpeedMs > 5.0
				? -Math.Sign(YawRateRad == 0 ? steer : YawRateRad) * SpeedMs * 0.34
				: -YawRateRad * SpeedMs * (1.0 - surfaceGrip * 0.55) * 0.12;
			LateralVelocityMs += (targetLatVel - LateralVelocityMs) * Clamp(dt * 6.0, 0.0, 1.0);

			// Advance world position.
			var cos = Math.Cos(HeadingRad);
			var sin = Math.Sin(HeadingRad);
			var worldVx = SpeedMs * cos - LateralVelocityMs * sin;
			var worldVy = SpeedMs * sin + LateralVelocityMs * cos;
			X += worldVx * dt;
			Y += worldVy * dt;

			// Keep car inside the arena bounds with a gentle wall slide so it never gets trapped.
			var maxX = TrackStraightHalf + TrackCornerRadius + TrackHalfWidth + 14.0;
			var maxY = TrackCornerRadius + TrackHalfWidth + 14.0;
			if (X < -maxX) { X = -maxX; if (Math.Cos(HeadingRad) < 0) HeadingRad = Math.PI - HeadingRad; SpeedMs *= 0.92; }
			if (X > maxX) { X = maxX; if (Math.Cos(HeadingRad) > 0) HeadingRad = Math.PI - HeadingRad; SpeedMs *= 0.92; }
			if (Y < -maxY) { Y = -maxY; if (Math.Sin(HeadingRad) < 0) HeadingRad = -HeadingRad; SpeedMs *= 0.92; }
			if (Y > maxY) { Y = maxY; if (Math.Sin(HeadingRad) > 0) HeadingRad = -HeadingRad; SpeedMs *= 0.92; }

			// Lateral G and body slip angle.
			var latAccel = SpeedMs * YawRateRad;
			LateralG = (float)Clamp(latAccel / Gravity, -4.0, 4.0);
			BodySlipDeg = SpeedMs > 2.0 ? (float)(Math.Atan2(LateralVelocityMs, SpeedMs) * RadToDeg) : 0f;
			AlignAngleDeg = SpeedMs > 2.0
				? (float)Clamp(BodySlipDeg * 1.4 + LateralG * 2.5, -55.0, 55.0)
				: 0f;

			// Track curb detection (inner or outer edge of the oval) + manual curb trigger.
			var trackDist = SignedDistanceFromTrackCentreline(X, Y);
			var absTrackDist = Math.Abs(trackDist);
			OnCurb = absTrackDist >= TrackHalfWidth - 1.2 && absTrackDist <= TrackHalfWidth + CurbOuterWidth + 1.5;
			var offTrack = absTrackDist > TrackHalfWidth + CurbOuterWidth + 1.5;

			float curb = 0f;
			if (_manualCurbTimer > 0f)
			{
				_manualCurbTimer = Math.Max(0f, _manualCurbTimer - (float)dt);
				var pulse = (float)Math.Sin(ElapsedSeconds * 55.0);
				curb = _manualCurbDir * (0.65f + 0.35f * Math.Abs(pulse));
			}
			else if (OnCurb && SpeedMs > 2.5)
			{
				var curbDir = trackDist > 0 ? 1f : -1f;
				var ripple = (float)Math.Sin(ElapsedSeconds * Math.Min(65.0, SpeedMs * 2.6));
				curb = curbDir * (0.45f + 0.45f * ripple) * (float)Clamp(SpeedMs / 25.0, 0.25, 1.0);
			}
			else if ((offTrack || surfaceRoughness > 0.1) && SpeedMs > 3.0)
			{
				var rough = offTrack ? 0.45 : surfaceRoughness;
				curb = (float)(Math.Sin(ElapsedSeconds * 37.0) * rough * Clamp(SpeedMs / 30.0, 0.2, 1.0));
			}
			CurbImpulse = (float)Clamp(curb, -1.0, 1.0);

			// Engine RPM & Gear (6-speed gearbox).
			var kmh = SpeedKmh;
			Gear = kmh < 42f ? 1 : kmh < 80f ? 2 : kmh < 120f ? 3 : kmh < 162f ? 4 : kmh < 205f ? 5 : 6;
			var gearTop = Gear == 1 ? 48f : Gear == 2 ? 88f : Gear == 3 ? 130f : Gear == 4 ? 172f : Gear == 5 ? 215f : 260f;
			var gearBase = Gear == 1 ? 0f : Gear == 2 ? 32f : Gear == 3 ? 65f : Gear == 4 ? 105f : Gear == 5 ? 145f : 185f;
			var ratio = (float)Clamp((kmh - gearBase) / Math.Max(1f, gearTop - gearBase), 0.0, 1.0);
			Rpm = 1050f + ratio * (MaxRpm - 1400f);

			// XInput rumble simulation (heavy motor for curbs/off-track, light motor for tire scrub).
			var heavy = Math.Abs(CurbImpulse) * 0.85f + (offTrack && SpeedMs > 3.0 ? 0.35f : 0f);
			var light = (1f - FrontGrip) * 0.9f + (handbrake && SpeedMs > 5.0 ? 0.45f : 0f);
			RumbleLeft = (float)Clamp(heavy, 0.0, 1.0);
			RumbleRight = (float)Clamp(light, 0.0, 1.0);

			// Record trail point every 40 ms while moving.
			_trailAccum += dt;
			if (_trailAccum >= 0.04 && SpeedMs > 1.5)
			{
				_trailAccum = 0.0;
				Trail.Add(new PointF((float)X, (float)Y));
				TrailSlipping.Add(FrontGrip < 0.78f || Math.Abs(BodySlipDeg) > 6f || handbrake);
				if (Trail.Count > 220)
				{
					Trail.RemoveAt(0);
					TrailSlipping.RemoveAt(0);
				}
			}
		}

		void ComputeAutoDrive(out double steer, out double targetSpeedMs)
		{
			// Follow the stadium oval counter-clockwise at a lively ~110-140 km/h, gently clipping apex curbs.
			double cx = Clamp(X, -TrackStraightHalf, TrackStraightHalf);
			double dx = X - cx;
			double dy = Y;
			double dist = Math.Sqrt(dx * dx + dy * dy);
			if (dist < 0.001)
			{
				dx = 0.0;
				dy = 1.0;
				dist = 1.0;
			}
			// Tangent along counter-clockwise stadium oval (bottom goes +X, right curve goes -Y, top goes -X, left curve goes +Y).
			double nx = dx / dist;
			double ny = dy / dist;
			double tx = ny;
			double ty = -nx;
			// Slightly weave towards the inner curb in corners so users feel curb kicks and grip changes automatically.
			var targetRadius = TrackCornerRadius - 4.0 * Math.Sin(_autoDrivePhase * 0.75);
			var radialError = dist - targetRadius;
			double aimX = tx - nx * Clamp(radialError * 0.09, -0.8, 0.8);
			double aimY = ty - ny * Clamp(radialError * 0.09, -0.8, 0.8);
			double desiredHeading = Math.Atan2(aimY, aimX);
			double angleErr = NormalizeAngle(desiredHeading - HeadingRad);
			steer = Clamp(angleErr * 1.4, -1.0, 1.0);
			// Faster on straights (~145 km/h), slower in corners (~95 km/h).
			var inCorner = Math.Abs(X) > TrackStraightHalf - 5.0;
			targetSpeedMs = inCorner ? 27.0 : 40.0;
		}

		/// <summary>Signed distance from the stadium oval centreline in metres (positive = outside, negative = inside).</summary>
		public static double SignedDistanceFromTrackCentreline(double x, double y)
		{
			var cx = Clamp(x, -TrackStraightHalf, TrackStraightHalf);
			var dx = x - cx;
			var dist = Math.Sqrt(dx * dx + y * y);
			return dist - TrackCornerRadius;
		}

		static void SurfaceParameters(VirtualCarSurface surface, out double grip, out double roughness)
		{
			switch (surface)
			{
				case VirtualCarSurface.WetTarmac:
					grip = 0.78;
					roughness = 0.03;
					return;
				case VirtualCarSurface.Gravel:
					grip = 0.62;
					roughness = 0.28;
					return;
				case VirtualCarSurface.Ice:
					grip = 0.32;
					roughness = 0.06;
					return;
				default:
					grip = 1.18;
					roughness = 0.0;
					return;
			}
		}

		/// <summary>Encodes the current car state into one or more standard UDP datagrams for the chosen protocol.</summary>
		public byte[][] BuildPackets(VirtualCarProtocol protocol, uint timestampMs)
		{
			switch (protocol)
			{
				case VirtualCarProtocol.EaF1Udp:
					return new[] { BuildEaF1TelemetryPacket(timestampMs), BuildEaF1MotionPacket(timestampMs) };
				case VirtualCarProtocol.CodemastersUdp:
					return new[] { BuildCodemastersPacket() };
				case VirtualCarProtocol.OutSimUdp:
					return new[] { BuildOutSimPacket(timestampMs) };
				case VirtualCarProtocol.ProjectCars2Udp:
					return new[] { BuildProjectCars2Packet() };
				default:
					return new[] { BuildForzaPacket(timestampMs) };
			}
		}

		byte[] BuildForzaPacket(uint timestampMs)
		{
			var buf = new byte[323];
			WriteInt32(buf, 0, 1);                          // IsRaceOn = 1
			WriteUInt32(buf, 4, timestampMs);               // TimestampMS
			WriteSingle(buf, 8, MaxRpm);                    // EngineMaxRpm
			WriteSingle(buf, 12, 900f);                     // EngineIdleRpm
			WriteSingle(buf, 16, Rpm);                      // CurrentEngineRpm
			WriteSingle(buf, 20, (float)(LateralG * Gravity)); // AccelerationX (m/s^2)
			WriteSingle(buf, 32, (float)LateralVelocityMs); // VelocityX (local sideways)
			WriteSingle(buf, 36, 0f);                       // VelocityY
			WriteSingle(buf, 40, (float)SpeedMs);           // VelocityZ (forward)
			WriteSingle(buf, 60, PitchRad);                 // Pitch (radians, nose up positive)
			// SurfaceRumble FL (132) & FR (136) encode left/right curb kicks cleanly.
			var leftRumble = CurbImpulse < 0f ? Math.Min(1.6f, -CurbImpulse / 0.6f) : 0f;
			var rightRumble = CurbImpulse > 0f ? Math.Min(1.6f, CurbImpulse / 0.6f) : 0f;
			WriteSingle(buf, 132, leftRumble);
			WriteSingle(buf, 136, rightRumble);
			// TireCombinedSlip FL (164) & FR (168) drive front grip lightening.
			WriteSingle(buf, 164, FrontSlip);
			WriteSingle(buf, 168, FrontSlip);
			return buf;
		}

		byte[] BuildEaF1TelemetryPacket(uint timestampMs)
		{
			// EA F1 2024 Packet 6 (Car Telemetry): 29-byte header + 22 cars * 60 bytes = 1349 bytes.
			var buf = new byte[1349];
			WriteUInt16(buf, 0, 2024);
			buf[6] = 6;   // packetId = Car Telemetry
			buf[27] = 0;  // playerCarIndex = 0
			WriteUInt32(buf, 19, timestampMs);
			WriteUInt16(buf, 29, (ushort)Math.Max(0, Math.Min(600, (int)Math.Round(SpeedKmh))));
			WriteUInt16(buf, 29 + 16, (ushort)Math.Max(0, Math.Min(25000, (int)Math.Round(Rpm))));
			buf[29 + 22] = (byte)Math.Max(0, Math.Min(100, (int)Math.Round(Rpm / MaxRpm * 100f)));
			return buf;
		}

		byte[] BuildEaF1MotionPacket(uint timestampMs)
		{
			// EA F1 2024 Packet 0 (Motion): 29-byte header + 22 cars * 60 bytes = 1349 bytes.
			var buf = new byte[1349];
			WriteUInt16(buf, 0, 2024);
			buf[6] = 0;   // packetId = Motion
			buf[27] = 0;  // playerCarIndex = 0
			WriteUInt32(buf, 19, timestampMs);
			WriteSingle(buf, 29 + 36, LateralG);
			WriteSingle(buf, 29 + 52, PitchRad);
			return buf;
		}

		byte[] BuildCodemastersPacket()
		{
			var buf = new byte[264];
			WriteSingle(buf, 0, (float)ElapsedSeconds);
			WriteSingle(buf, 28, (float)SpeedMs);
			WriteSingle(buf, 32, (float)LateralVelocityMs);
			WriteSingle(buf, 40, (float)SpeedMs);
			// Suspension velocity FL (92) & FR (96).
			WriteSingle(buf, 92, CurbImpulse < 0f ? -CurbImpulse * 6.5f : 0f);
			WriteSingle(buf, 96, CurbImpulse > 0f ? CurbImpulse * 6.5f : 0f);
			// Front wheel speeds FL (108) & FR (112) reflecting front slip.
			var wheelSpeed = (float)(SpeedMs * (1.0 - Math.Max(0.0, 1.0 - FrontGrip) * 0.75));
			WriteSingle(buf, 108, wheelSpeed);
			WriteSingle(buf, 112, wheelSpeed);
			WriteSingle(buf, 136, LateralG);
			WriteSingle(buf, 148, Rpm / 10f);    // Codemasters RPM is in RPM/10
			WriteSingle(buf, 252, MaxRpm / 10f);
			return buf;
		}

		byte[] BuildOutSimPacket(uint timestampMs)
		{
			var buf = new byte[64];
			WriteUInt32(buf, 0, timestampMs);
			WriteSingle(buf, 20, PitchRad);
			WriteSingle(buf, 28, (float)(LateralG * Gravity));
			WriteSingle(buf, 40, (float)SpeedMs);
			WriteSingle(buf, 48, (float)LateralVelocityMs);
			return buf;
		}

		byte[] BuildProjectCars2Packet()
		{
			var buf = new byte[559];
			buf[10] = 0; // CarPhysics packet
			WriteSingle(buf, 36, (float)SpeedMs);
			WriteUInt16(buf, 40, (ushort)Math.Max(0, Math.Min(25000, (int)Math.Round(Rpm))));
			WriteUInt16(buf, 42, (ushort)Math.Max(1000, Math.Min(25000, (int)Math.Round(MaxRpm))));
			WriteSingle(buf, 52, PitchRad);
			WriteSingle(buf, 72, (float)(LateralG * Gravity));
			return buf;
		}

		static void WriteInt32(byte[] b, int o, int v) { Buffer.BlockCopy(BitConverter.GetBytes(v), 0, b, o, 4); }
		static void WriteUInt32(byte[] b, int o, uint v) { Buffer.BlockCopy(BitConverter.GetBytes(v), 0, b, o, 4); }
		static void WriteUInt16(byte[] b, int o, ushort v) { Buffer.BlockCopy(BitConverter.GetBytes(v), 0, b, o, 2); }
		static void WriteSingle(byte[] b, int o, float v) { Buffer.BlockCopy(BitConverter.GetBytes(v), 0, b, o, 4); }

		static double Clamp(double v, double min, double max)
		{
			return v < min ? min : v > max ? max : v;
		}

		static double NormalizeAngle(double a)
		{
			while (a > Math.PI) a -= 2.0 * Math.PI;
			while (a < -Math.PI) a += 2.0 * Math.PI;
			return a;
		}
	}
}
