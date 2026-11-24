using System;
using System.Text;
using x360ce.Plugins;

namespace x360ce.Plugins.Samples
{
	public partial class WheelTelemetryProvider
	{
		public const string AcPhysicsMapName = @"Local\acpmf_physics";
		public const string IRacingMapName = @"Local\IRSDKMemMapFileName";

		const float Gravity = 9.80665f;
		const float RadToDeg = (float)(180.0 / Math.PI);

		/// <summary>How many consecutive valid packets a headerless UDP protocol needs before locking on.</summary>
		public const int ConfidencePacketsRequired = 3;

		enum UdpProtocol
		{
			None = 0,
			Forza = 1,
			EaF1 = 2,
			Codemasters = 3,
			OutSim = 4,
			ProjectCarsV1 = 5,
			ProjectCarsV2 = 6,
		}

		// Auto-detection state (touched only on worker thread or synchronously in unit tests).
		UdpProtocol _lockedProtocol;
		long _lastPacketMs;

		UdpProtocol _candidateProtocol;
		int _candidateStreak;
		uint _lastForzaTimestamp;
		float _lastCodemastersTime = -1f;
		uint _lastOutSimTime;

		// EA F1 splits speed (packet 6) and motion/G-force (packet 0) across two datagrams.
		float _f1SpeedKmh = float.NaN;
		float _f1RpmFraction = float.NaN;
		float _f1PitchDeg = float.NaN;
		float _f1LateralG;
		float _f1AlignDeg;

		// Shared Memory monotonic tick tracking (so a paused/menu map does not emit stale speed).
		int _lastAcPacketId = -1;
		int _lastIrTickCount = -1;
		float _acPeakRpm = 7500f;
		float _irPeakRpm = 7500f;

		/// <summary>Feeds one received UDP datagram through the 2-stage auto-detector and publishes telemetry when locked.</summary>
		/// <remarks>Public test seam so unit tests verify every standard decoder and false-positive rejection without sockets.</remarks>
		public bool TakeUdp(int port, byte[] data, int length, long nowMs)
		{
			if (data == null || length <= 0 || length > data.Length)
				return false;

			// Unlock if the previous game went silent so switching games auto-detects automatically.
			if (_lockedProtocol != UdpProtocol.None && _lastPacketMs > 0 && nowMs - _lastPacketMs > UnlockAfterMs)
			{
				_lockedProtocol = UdpProtocol.None;
				_candidateProtocol = UdpProtocol.None;
				_candidateStreak = 0;
				_f1SpeedKmh = float.NaN;
				_f1RpmFraction = float.NaN;
				_f1PitchDeg = float.NaN;
				_f1LateralG = 0f;
				_f1AlignDeg = 0f;
			}

			var sample = WheelTelemetry.Neutral;
			sample.TimestampMs = nowMs;

			if (_lockedProtocol != UdpProtocol.None)
			{
				if (TryDecodeLocked(_lockedProtocol, data, length, ref sample))
				{
					_lastPacketMs = nowMs;
					if (sample.TimestampMs > 0)
						Publish(ref sample);
					return true;
				}
				// If a different explicit-magic protocol arrives, let auto-detect re-evaluate.
				if (nowMs - _lastPacketMs <= UnlockAfterMs)
					return false;
				_lockedProtocol = UdpProtocol.None;
			}

			// Stage 1: Explicit-header protocols lock on packet #1.
			if (TryDecodeEaF1(data, length, ref sample))
			{
				Lock(UdpProtocol.EaF1, nowMs, ref sample);
				return true;
			}
			if (TryDecodeProjectCarsV1(data, length, ref sample))
			{
				Lock(UdpProtocol.ProjectCarsV1, nowMs, ref sample);
				return true;
			}
			if (TryDecodeProjectCarsV2(data, length, ref sample))
			{
				Lock(UdpProtocol.ProjectCarsV2, nowMs, ref sample);
				return true;
			}

			// Stage 2: Headerless fixed-size structs require consecutive valid packets with advancing timestamps.
			UdpProtocol matched = UdpProtocol.None;
			bool advancing = false;
			if (TryDecodeForza(data, length, ref sample, out advancing))
				matched = UdpProtocol.Forza;
			else if (TryDecodeCodemasters(data, length, ref sample, out advancing))
				matched = UdpProtocol.Codemasters;
			else if (TryDecodeOutSim(data, length, ref sample, out advancing))
				matched = UdpProtocol.OutSim;

			if (matched == UdpProtocol.None)
				return false;

			if (matched == _candidateProtocol)
			{
				if (advancing)
					_candidateStreak++;
			}
			else
			{
				_candidateProtocol = matched;
				_candidateStreak = 1;
			}

			if (_candidateStreak >= ConfidencePacketsRequired)
			{
				Lock(matched, nowMs, ref sample);
				return true;
			}
			return false;
		}

		void Lock(UdpProtocol protocol, long nowMs, ref WheelTelemetry sample)
		{
			_lockedProtocol = protocol;
			_candidateProtocol = UdpProtocol.None;
			_candidateStreak = 0;
			_lastPacketMs = nowMs;
			if (sample.TimestampMs > 0)
				Publish(ref sample);
		}

		bool TryDecodeLocked(UdpProtocol protocol, byte[] data, int length, ref WheelTelemetry sample)
		{
			bool advancing;
			switch (protocol)
			{
				case UdpProtocol.Forza:
					return TryDecodeForza(data, length, ref sample, out advancing);
				case UdpProtocol.EaF1:
					return TryDecodeEaF1(data, length, ref sample);
				case UdpProtocol.Codemasters:
					return TryDecodeCodemasters(data, length, ref sample, out advancing);
				case UdpProtocol.OutSim:
					return TryDecodeOutSim(data, length, ref sample, out advancing);
				case UdpProtocol.ProjectCarsV1:
					return TryDecodeProjectCarsV1(data, length, ref sample);
				case UdpProtocol.ProjectCarsV2:
					return TryDecodeProjectCarsV2(data, length, ref sample);
				default:
					return false;
			}
		}

		#region Forza Data Out (232 Sled / 311 FM7 / 323 FH4-5 / 331 FM23)

		bool TryDecodeForza(byte[] data, int length, ref WheelTelemetry sample, out bool advancing)
		{
			advancing = false;
			if (length != 232 && length != 311 && length != 323 && length != 331)
				return false;

			var isRaceOn = BitConverter.ToInt32(data, 0);
			if (isRaceOn != 0 && isRaceOn != 1)
				return false;
			var timestamp = BitConverter.ToUInt32(data, 4);
			var maxRpm = BitConverter.ToSingle(data, 8);
			var idleRpm = BitConverter.ToSingle(data, 12);
			var curRpm = BitConverter.ToSingle(data, 16);
			if (!IsFinite(maxRpm) || !IsFinite(idleRpm) || !IsFinite(curRpm))
				return false;
			var neutralRpm = maxRpm == 0f && idleRpm == 0f && curRpm == 0f;
			if (isRaceOn == 1 && !neutralRpm && (maxRpm < 500f || maxRpm > 30000f || curRpm < -100f || curRpm > maxRpm * 1.3f))
				return false;

			var ax = BitConverter.ToSingle(data, 20);
			var vx = BitConverter.ToSingle(data, 32);
			var vy = BitConverter.ToSingle(data, 36);
			var vz = BitConverter.ToSingle(data, 40);
			var pitchRad = BitConverter.ToSingle(data, 60);
			if (!IsFinite(ax) || !IsFinite(vx) || !IsFinite(vy) || !IsFinite(vz))
				return false;

			var speedMs = (float)Math.Sqrt(vx * vx + vy * vy + vz * vz);
			var speedKmh = speedMs * 3.6f;
			var latG = ax / Gravity;
			if (speedKmh > 650f || Math.Abs(latG) > 20f)
				return false;

			advancing = timestamp != _lastForzaTimestamp;
			_lastForzaTimestamp = timestamp;

			// Paused / menu: recognised as valid Forza traffic, but emit no active driving forces.
			if (isRaceOn == 0)
			{
				sample.TimestampMs = 0;
				return true;
			}

			var slipFl = BitConverter.ToSingle(data, 164);
			var slipFr = BitConverter.ToSingle(data, 168);
			var frontSlip = (IsFinite(slipFl) && IsFinite(slipFr))
				? (Math.Abs(slipFl) + Math.Abs(slipFr)) * 0.5f
				: 0f;
			var frontGrip = Clamp(1f - Math.Max(0f, frontSlip - 0.8f) * 0.7f, 0.15f, 1f);

			var rumbleFl = BitConverter.ToSingle(data, 132);
			var rumbleFr = BitConverter.ToSingle(data, 136);
			var impulse = (IsFinite(rumbleFl) && IsFinite(rumbleFr))
				? Clamp((rumbleFr - rumbleFl) * 0.6f, -1f, 1f)
				: 0f;

			// Body slip angle (when moving forward) creates genuine counter-steer aligning pull.
			var alignDeg = 0f;
			if (speedMs > 3f && vz > 1f)
			{
				var bodySlipDeg = (float)Math.Atan2(vx, vz) * RadToDeg;
				alignDeg = Clamp(bodySlipDeg * 1.4f + latG * 2.0f, -60f, 60f);
			}

			sample.SpeedKmh = speedKmh;
			sample.LateralG = latG;
			sample.AlignAngleDeg = alignDeg;
			sample.FrontGrip = frontGrip;
			sample.Impulse = impulse;
			sample.DampingScale = 1f;
			sample.RpmFraction = maxRpm > 500f ? Clamp(curRpm / maxRpm, 0f, 1f) : float.NaN;
			sample.PitchDeg = PitchFromRadians(pitchRad);
			sample.Source = "Forza Data Out";
			return true;
		}

		#endregion

		#region EA F1 (2020-2026+)

		bool TryDecodeEaF1(byte[] data, int length, ref WheelTelemetry sample)
		{
			if (length < 24 || length > 2048)
				return false;
			var format = BitConverter.ToUInt16(data, 0);
			if (format < 2020 || format > 2030)
				return false;

			var is2023Plus = format >= 2023;
			var headerSize = is2023Plus ? 29 : 24;
			if (length < headerSize)
				return false;

			var packetId = data[is2023Plus ? 6 : 5];
			var playerIndex = data[is2023Plus ? 27 : 21];
			if (packetId > 15 || playerIndex >= 22)
				return false;

			// Packet 6: Car Telemetry (speed in km/h at byte 0 of each car entry).
			if (packetId == 6)
			{
				var stride = format == 2020 ? 58 : 60;
				var offset = headerSize + playerIndex * stride;
				if (offset + 2 > length)
					return false;
				var speed = BitConverter.ToUInt16(data, offset);
				if (speed > 600)
					return false;
				_f1SpeedKmh = speed;
				// Official EA F1 CarTelemetry: engineRPM (ushort @ +16) and revLightsPercent (byte @ +22, 0..100).
				if (offset + 23 <= length)
				{
					var revPct = data[offset + 22];
					if (revPct > 0 && revPct <= 100)
						_f1RpmFraction = revPct / 100f;
					else
					{
						var rpm = BitConverter.ToUInt16(data, offset + 16);
						if (rpm > 0)
							_f1RpmFraction = Clamp(rpm / 12500f, 0f, 1f);
					}
				}
			}
			// Packet 0: Motion (world velocity + lateral G-force + m_pitch in radians at +52).
			else if (packetId == 0)
			{
				const int stride = 60;
				var offset = headerSize + playerIndex * stride;
				if (offset + 40 > length)
					return false;
				var latG = BitConverter.ToSingle(data, offset + 36);
				if (!IsFinite(latG) || Math.Abs(latG) > 15f)
					return false;
				_f1LateralG = latG;
				_f1AlignDeg = Clamp(latG * 3.5f, -45f, 45f);
				if (offset + 56 <= length)
					_f1PitchDeg = PitchFromRadians(BitConverter.ToSingle(data, offset + 52));
			}
			else
			{
				// Other F1 session/lap/event packets keep the lock alive without changing forces.
				sample.TimestampMs = 0;
				return true;
			}

			if (float.IsNaN(_f1SpeedKmh))
			{
				sample.TimestampMs = 0;
				return true;
			}

			sample.SpeedKmh = _f1SpeedKmh;
			sample.LateralG = _f1LateralG;
			sample.AlignAngleDeg = _f1AlignDeg;
			sample.FrontGrip = 1f;
			sample.Impulse = 0f;
			sample.DampingScale = 1f;
			sample.RpmFraction = _f1RpmFraction;
			sample.PitchDeg = _f1PitchDeg;
			sample.Source = "EA F1";
			return true;
		}

		#endregion

		#region Codemasters / EA Classic UDP (264 bytes — DiRT Rally, GRID, WRC)

		bool TryDecodeCodemasters(byte[] data, int length, ref WheelTelemetry sample, out bool advancing)
		{
			advancing = false;
			if (length != 264)
				return false;

			var time = BitConverter.ToSingle(data, 0);
			var speedMs = BitConverter.ToSingle(data, 28);
			var vx = BitConverter.ToSingle(data, 32);
			var vz = BitConverter.ToSingle(data, 40);
			var latG = BitConverter.ToSingle(data, 136);
			var rpm = BitConverter.ToSingle(data, 148);
			var maxRpm = BitConverter.ToSingle(data, 252);

			if (!IsFinite(time) || !IsFinite(speedMs) || !IsFinite(latG) || !IsFinite(rpm) || !IsFinite(maxRpm))
				return false;
			if (time < 0f || speedMs < 0f || speedMs > 180f || Math.Abs(latG) > 20f)
				return false;
			if (maxRpm < 50f || maxRpm > 3000f || rpm < -10f || rpm > maxRpm * 1.3f)
				return false;

			advancing = time != _lastCodemastersTime;
			_lastCodemastersTime = time;

			// Suspension velocity FL (23*4=92) and FR (24*4=96) give directional curb kicks.
			var suspVelFl = BitConverter.ToSingle(data, 92);
			var suspVelFr = BitConverter.ToSingle(data, 96);
			var impulse = (IsFinite(suspVelFl) && IsFinite(suspVelFr))
				? Clamp((suspVelFr - suspVelFl) * 0.15f, -1f, 1f)
				: 0f;

			// Compare front wheel speed against chassis speed to estimate front lockup / slip.
			var wFl = BitConverter.ToSingle(data, 108);
			var wFr = BitConverter.ToSingle(data, 112);
			var frontGrip = 1f;
			if (speedMs > 5f && IsFinite(wFl) && IsFinite(wFr))
			{
				var avgFront = (Math.Abs(wFl) + Math.Abs(wFr)) * 0.5f;
				var slip = Math.Abs(avgFront - speedMs) / speedMs;
				frontGrip = Clamp(1f - Math.Max(0f, slip - 0.15f) * 1.4f, 0.25f, 1f);
			}

			var alignDeg = 0f;
			if (speedMs > 3f && IsFinite(vx) && IsFinite(vz) && vz > 1f)
				alignDeg = Clamp((float)Math.Atan2(vx, vz) * RadToDeg * 1.3f + latG * 2.0f, -60f, 60f);

			sample.SpeedKmh = speedMs * 3.6f;
			sample.LateralG = latG;
			sample.AlignAngleDeg = alignDeg;
			sample.FrontGrip = frontGrip;
			sample.Impulse = impulse;
			sample.DampingScale = 1f;
			sample.RpmFraction = maxRpm > 50f ? Clamp(rpm / maxRpm, 0f, 1f) : float.NaN;
			sample.Source = "Codemasters UDP";
			return true;
		}

		#endregion

		#region OutSim (64 / 68 bytes — Live for Speed, BeamNG.drive)

		bool TryDecodeOutSim(byte[] data, int length, ref WheelTelemetry sample, out bool advancing)
		{
			advancing = false;
			if (length != 64 && length != 68)
				return false;

			var time = BitConverter.ToUInt32(data, 0);
			var pitchRad = BitConverter.ToSingle(data, 20);
			var ax = BitConverter.ToSingle(data, 28);
			var ay = BitConverter.ToSingle(data, 32);
			var az = BitConverter.ToSingle(data, 36);
			var vx = BitConverter.ToSingle(data, 40);
			var vy = BitConverter.ToSingle(data, 44);
			var vz = BitConverter.ToSingle(data, 48);

			if (!IsFinite(ax) || !IsFinite(ay) || !IsFinite(az) || !IsFinite(vx) || !IsFinite(vy) || !IsFinite(vz))
				return false;
			var speedMs = (float)Math.Sqrt(vx * vx + vy * vy + vz * vz);
			var latG = ax / Gravity;
			if (speedMs > 180f || Math.Abs(latG) > 20f || Math.Abs(ay / Gravity) > 20f)
				return false;

			advancing = time != _lastOutSimTime;
			_lastOutSimTime = time;

			sample.SpeedKmh = speedMs * 3.6f;
			sample.LateralG = latG;
			sample.AlignAngleDeg = Clamp(latG * 3.5f, -45f, 45f);
			sample.FrontGrip = 1f;
			sample.Impulse = 0f;
			sample.DampingScale = 1f;
			sample.PitchDeg = PitchFromRadians(pitchRad);
			sample.Source = "OutSim";
			return true;
		}

		#endregion

		#region Project CARS / AMS2 UDP (v1 1367 bytes, v2 559 bytes)

		bool TryDecodeProjectCarsV1(byte[] data, int length, ref WheelTelemetry sample)
		{
			if (length != 1367)
				return false;
			var packetType = data[2] & 0x03;
			if (packetType != 0)
				return false;
			var speedMs = BitConverter.ToSingle(data, 120);
			var localAx = BitConverter.ToSingle(data, 168);
			if (!IsFinite(speedMs) || !IsFinite(localAx) || speedMs < 0f || speedMs > 180f || Math.Abs(localAx / Gravity) > 20f)
				return false;
			var latG = localAx / Gravity;
			var rpm = BitConverter.ToUInt16(data, 124);
			var maxRpm = BitConverter.ToUInt16(data, 126);
			sample.SpeedKmh = speedMs * 3.6f;
			sample.LateralG = latG;
			sample.AlignAngleDeg = Clamp(latG * 3.5f, -45f, 45f);
			sample.FrontGrip = 1f;
			sample.Impulse = 0f;
			sample.DampingScale = 1f;
			sample.RpmFraction = maxRpm > 500 ? Clamp((float)rpm / maxRpm, 0f, 1f) : float.NaN;
			sample.PitchDeg = PitchFromRadians(BitConverter.ToSingle(data, 136));
			sample.Source = "Project CARS UDP";
			return true;
		}

		bool TryDecodeProjectCarsV2(byte[] data, int length, ref WheelTelemetry sample)
		{
			if (length != 559)
				return false;
			var packetType = data[10];
			if (packetType != 0)
				return false;
			var speedMs = BitConverter.ToSingle(data, 36);
			var localAx = BitConverter.ToSingle(data, 72);
			if (!IsFinite(speedMs) || !IsFinite(localAx) || speedMs < 0f || speedMs > 180f || Math.Abs(localAx / Gravity) > 20f)
				return false;
			var latG = localAx / Gravity;
			var rpm = BitConverter.ToUInt16(data, 40);
			var maxRpm = BitConverter.ToUInt16(data, 42);
			sample.SpeedKmh = speedMs * 3.6f;
			sample.LateralG = latG;
			sample.AlignAngleDeg = Clamp(latG * 3.5f, -45f, 45f);
			sample.FrontGrip = 1f;
			sample.Impulse = 0f;
			sample.DampingScale = 1f;
			sample.RpmFraction = maxRpm > 500 ? Clamp((float)rpm / maxRpm, 0f, 1f) : float.NaN;
			sample.PitchDeg = PitchFromRadians(BitConverter.ToSingle(data, 52));
			sample.Source = "Project CARS 2 UDP";
			return true;
		}

		#endregion

		#region Windows Shared Memory (Assetto Corsa / ACC / AC Evo, iRacing)

		static bool IsKnownSharedMemoryHeader(string mapName, byte[] data, int length)
		{
			if (mapName == AcPhysicsMapName)
				return length >= 88;
			if (mapName == IRacingMapName)
				return length >= 112 && BitConverter.ToInt32(data, 0) >= 2 && BitConverter.ToInt32(data, 16) > 0;
			return false;
		}

		/// <summary>Feeds a Shared Memory byte snapshot through the decoders. Public test seam for unit tests.</summary>
		public bool TakeSharedMemory(string mapName, byte[] data, int length, long nowMs)
		{
			if (data == null || length <= 0 || length > data.Length)
				return false;
			var sample = WheelTelemetry.Neutral;
			sample.TimestampMs = nowMs;

			if (string.Equals(mapName, AcPhysicsMapName, StringComparison.OrdinalIgnoreCase))
			{
				if (!TryDecodeAssettoCorsa(data, length, ref sample))
					return false;
				Publish(ref sample);
				return true;
			}
			if (string.Equals(mapName, IRacingMapName, StringComparison.OrdinalIgnoreCase))
			{
				if (!TryDecodeIRacing(data, length, ref sample))
					return false;
				Publish(ref sample);
				return true;
			}
			return false;
		}

		/// <summary>Assetto Corsa / ACC / AC Evo <c>Local\acpmf_physics</c> fixed ABI.</summary>
		bool TryDecodeAssettoCorsa(byte[] data, int length, ref WheelTelemetry sample)
		{
			if (length < 88)
				return false;
			var packetId = BitConverter.ToInt32(data, 0);
			if (packetId <= 0 || packetId == _lastAcPacketId)
				return false;
			var speedKmh = BitConverter.ToSingle(data, 28);
			var vx = BitConverter.ToSingle(data, 32);
			var vz = BitConverter.ToSingle(data, 40);
			var latG = BitConverter.ToSingle(data, 44);
			var slipFl = BitConverter.ToSingle(data, 56);
			var slipFr = BitConverter.ToSingle(data, 60);
			if (!IsFinite(speedKmh) || !IsFinite(latG) || speedKmh < 0f || speedKmh > 650f || Math.Abs(latG) > 20f)
				return false;
			_lastAcPacketId = packetId;

			var avgSlip = (IsFinite(slipFl) && IsFinite(slipFr))
				? (Math.Abs(slipFl) + Math.Abs(slipFr)) * 0.5f
				: 0f;
			var frontGrip = Clamp(1f - Math.Max(0f, avgSlip - 1.0f) * 0.25f, 0.2f, 1f);
			var alignDeg = 0f;
			if (speedKmh > 10f && IsFinite(vx) && IsFinite(vz) && vz > 1f)
				alignDeg = Clamp((float)Math.Atan2(vx, vz) * RadToDeg * 1.3f + latG * 2.5f, -60f, 60f);

			var rpms = BitConverter.ToInt32(data, 20);
			if (rpms > _acPeakRpm && rpms < 25000)
				_acPeakRpm = rpms;
			sample.SpeedKmh = speedKmh;
			sample.LateralG = latG;
			sample.AlignAngleDeg = alignDeg;
			sample.FrontGrip = frontGrip;
			sample.Impulse = 0f;
			sample.DampingScale = 1f;
			sample.RpmFraction = rpms > 0 ? Clamp(rpms / _acPeakRpm, 0f, 1f) : float.NaN;
			sample.PitchDeg = length >= 216 ? PitchFromRadians(BitConverter.ToSingle(data, 212)) : float.NaN;
			sample.Source = "Assetto Corsa";
			return true;
		}

		/// <summary>iRacing <c>Local\IRSDKMemMapFileName</c> self-describing header and variable table.</summary>
		bool TryDecodeIRacing(byte[] data, int length, ref WheelTelemetry sample)
		{
			if (length < 112)
				return false;
			var version = BitConverter.ToInt32(data, 0);
			var status = BitConverter.ToInt32(data, 4);
			var numVars = BitConverter.ToInt32(data, 24);
			var varHeaderOffset = BitConverter.ToInt32(data, 28);
			var numBuf = BitConverter.ToInt32(data, 32);
			var bufLen = BitConverter.ToInt32(data, 36);
			if (version < 2 || (status & 1) == 0 || numVars <= 0 || numVars > 2048 || numBuf <= 0 || numBuf > 4 || bufLen <= 0)
				return false;
			if (varHeaderOffset < 48 || varHeaderOffset + numVars * 144 > length)
				return false;

			// Pick the buffer with the newest tickCount (entries start at byte 48, 16 bytes each: tickCount, bufOffset, pad[2]).
			var bestTick = -1;
			var bestOffset = -1;
			for (var i = 0; i < numBuf; i++)
			{
				var entry = 48 + i * 16;
				var tick = BitConverter.ToInt32(data, entry);
				var off = BitConverter.ToInt32(data, entry + 4);
				if (tick > bestTick && off >= 0 && off + bufLen <= length)
				{
					bestTick = tick;
					bestOffset = off;
				}
			}
			if (bestOffset < 0 || bestTick == _lastIrTickCount)
				return false;

			// Scan self-describing irsdk_varHeader entries (144 bytes each: type@0, offset@4, count@8, name@16[32]).
			var speedOff = -1;
			var latAccelOff = -1;
			var rpmOff = -1;
			var pitchOff = -1;
			for (var i = 0; i < numVars; i++)
			{
				var h = varHeaderOffset + i * 144;
				var type = BitConverter.ToInt32(data, h);
				if (type != 4) // irsdk_float = 4
					continue;
				var off = BitConverter.ToInt32(data, h + 4);
				if (off < 0 || off + 4 > bufLen)
					continue;
				if (NameEquals(data, h + 16, "Speed"))
					speedOff = off;
				else if (NameEquals(data, h + 16, "LatAccel"))
					latAccelOff = off;
				else if (NameEquals(data, h + 16, "RPM"))
					rpmOff = off;
				else if (NameEquals(data, h + 16, "Pitch"))
					pitchOff = off;
				if (speedOff >= 0 && latAccelOff >= 0 && rpmOff >= 0 && pitchOff >= 0)
					break;
			}
			if (speedOff < 0)
				return false;

			var speedMs = BitConverter.ToSingle(data, bestOffset + speedOff);
			var latAccel = latAccelOff >= 0 ? BitConverter.ToSingle(data, bestOffset + latAccelOff) : 0f;
			var rpm = rpmOff >= 0 ? BitConverter.ToSingle(data, bestOffset + rpmOff) : float.NaN;
			var pitchRad = pitchOff >= 0 ? BitConverter.ToSingle(data, bestOffset + pitchOff) : float.NaN;
			if (!IsFinite(speedMs) || speedMs < 0f || speedMs > 180f)
				return false;
			_lastIrTickCount = bestTick;
			if (IsFinite(rpm) && rpm > _irPeakRpm && rpm < 25000f)
				_irPeakRpm = rpm;

			var latG = IsFinite(latAccel) ? Clamp(latAccel / Gravity, -15f, 15f) : 0f;
			sample.SpeedKmh = speedMs * 3.6f;
			sample.LateralG = latG;
			sample.AlignAngleDeg = Clamp(latG * 3.5f, -45f, 45f);
			sample.FrontGrip = 1f;
			sample.Impulse = 0f;
			sample.DampingScale = 1f;
			sample.RpmFraction = IsFinite(rpm) && rpm > 0f ? Clamp(rpm / _irPeakRpm, 0f, 1f) : float.NaN;
			sample.PitchDeg = PitchFromRadians(pitchRad);
			sample.Source = "iRacing";
			return true;
		}

		static bool NameEquals(byte[] data, int offset, string expected)
		{
			for (var i = 0; i < expected.Length; i++)
				if (data[offset + i] != (byte)expected[i])
					return false;
			return data[offset + expected.Length] == 0;
		}

		#endregion

		static float PitchFromRadians(float pitchRad)
		{
			return IsFinite(pitchRad) && Math.Abs(pitchRad) <= 1.57f
				? Clamp(pitchRad * RadToDeg, -85f, 85f)
				: float.NaN;
		}

		static bool IsFinite(float v)
		{
			return !float.IsNaN(v) && !float.IsInfinity(v);
		}

		static float Clamp(float value, float min, float max)
		{
			return value < min ? min : value > max ? max : value;
		}
	}
}
