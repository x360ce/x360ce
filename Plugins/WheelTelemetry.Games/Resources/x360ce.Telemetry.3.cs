// x360ce GTA V telemetry script, version 1.
//
// Sends what the player does in GTA V's story mode as Forza Motorsport 7 "Data Out" packets, in the 311-byte "Dash"
// layout Turn 10 documents, to this computer (127.0.0.1). x360ce's Wheel Telemetry plugin reads them, and so do SimHub
// and most dashboards.
//
// x360ce's Wheel Telemetry Games plugin puts this file in the game's scripts folder, and ScriptHookVDotNet compiles it
// when the game starts. The ".3" in the file name makes ScriptHookVDotNet compile it against its version 3 scripting API, which
// ScriptHookVDotNet 3 (GTA V Legacy) and ScriptHookVDotNet Enhanced (GTA V Legacy and Enhanced) both have. It uses only
// the part of that API both have, and C# 5, the language ScriptHookVDotNet 3.6's compiler takes.
//
// Story mode only. While a network session is active, the game is paused or in a menu, loading, in a cutscene or
// switching characters, or the player is dead, it sends only "race off" packets: IsRaceOn 0 and nothing else of the
// game, one a second, so what reads them goes back to neutral.
//
// Settings, in x360ce.Telemetry.ini beside this file, which the plugin writes:
//   Port=37150         Where the packets go. x360ce's Wheel Telemetry plugin listens on 37150, SimHub on 5300.
//   Rate=60            Packets a second. The game runs the script at most once a frame.
// The game reads them when it starts, and when ScriptHookVDotNet loads its scripts again.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using GTA;
using GTA.Math;
using GTA.Native;

namespace X360ce.GtaV
{
	/// <summary>The script ScriptHookVDotNet runs: reads the game at each tick and sends a packet.</summary>
	public class TelemetryScript : Script
	{
		/// <summary>The settings file, beside this script.</summary>
		public const string SettingsFileName = "x360ce.Telemetry.ini";

		// The controls GTA V drives with, by their numbers in GTA's Control list, as GET_CONTROL_NORMAL takes them.
		const int ControlVehicleMoveLeftRight = 59;
		const int ControlVehicleAccelerate = 71;
		const int ControlVehicleBrake = 72;
		const int ControlVehicleHandbrake = 76;

		/// <summary>
		/// GTA keeps a vehicle's engine speed as a share of its red line, 0.2 at idle, and has no figure in revolutions a
		/// minute. The packets give every vehicle this red line.
		/// </summary>
		const float RedLineRpm = 7000f;

		/// <summary>GTA's engine speed at idle, as a share of the red line.</summary>
		const float IdleShare = 0.2f;

		/// <summary>How far ahead of and behind the player the ground is measured for its slope, in metres.</summary>
		const float SlopeReach = 3f;

		/// <summary>How far above the player the search for the ground starts, in metres: above a slope, below most roofs.</summary>
		const float GroundSearchHeight = 3f;

		/// <summary>Below this speed, in metres a second, the slope is measured the way the player faces, not the way it moves.</summary>
		const float SlowSpeed = 1f;

		/// <summary>A longer time between two ticks, in seconds, gives no acceleration or turn rate.</summary>
		const double LongestStep = 0.5;

		const float DegreesToRadians = (float)(Math.PI / 180);

		readonly byte[] _packet = new byte[ForzaDash.Length];
		readonly ForzaSample _sample = new ForzaSample();
		readonly UdpClient _udp = new UdpClient();
		readonly IPEndPoint _target;
		readonly OutputArgument _ground = new OutputArgument();
		readonly Stopwatch _clock = Stopwatch.StartNew();

		/// <summary>True while race off packets are sent, and when the last was sent, in milliseconds of the clock.</summary>
		bool _off;
		long _offSentMs;

		// The tick before, for acceleration and the rates of turn.
		bool _hasLast;
		bool _lastInVehicle;
		double _lastSeconds;
		float _lastVelocityX;
		float _lastVelocityY;
		float _lastVelocityZ;
		float _lastPitch;
		float _lastYaw;
		float _lastRoll;

		/// <summary>Metres driven since the script started, in any vehicle; time on foot adds nothing.</summary>
		double _distance;

		public TelemetryScript()
		{
			var settings = TelemetrySettings.Read(Path.Combine(BaseDirectory, SettingsFileName));
			_target = new IPEndPoint(IPAddress.Loopback, settings.Port);
			Interval = 1000 / settings.Rate;
			Tick += OnTick;
			Aborted += OnAborted;
		}

		void OnTick(object sender, EventArgs e)
		{
			var seconds = _clock.Elapsed.TotalSeconds;
			_sample.Clear();
			_sample.TimestampMs = unchecked((uint)Environment.TickCount);
			if (!InStoryMode())
			{
				_hasLast = false;
				var ms = _clock.ElapsedMilliseconds;
				if (_off && ms - _offSentMs < 1000)
					return;
				_off = true;
				_offSentMs = ms;
				Send();
				return;
			}
			_off = false;
			_sample.RaceOn = true;
			var ped = Game.Player.Character;
			var vehicle = ped.IsInVehicle() ? ped.CurrentVehicle : null;
			if (vehicle != null)
				ReadVehicle(vehicle, seconds);
			else
				ReadOnFoot(ped);
			Send();
		}

		/// <summary>False while anything of the game must not leave: online, paused, in a menu, loading, in a cutscene, switching characters or dead.</summary>
		static bool InStoryMode()
		{
			// Story mode only: nothing of the game leaves while a network session is active.
			if (Native(Hash.NETWORK_IS_SESSION_STARTED) || Native(Hash.NETWORK_IS_IN_SESSION))
				return false;
			if (Native(Hash.IS_PAUSE_MENU_ACTIVE) || Native(Hash.GET_IS_LOADING_SCREEN_ACTIVE)
				|| Native(Hash.IS_CUTSCENE_ACTIVE) || Native(Hash.IS_PLAYER_SWITCH_IN_PROGRESS))
				return false;
			return !Game.Player.IsDead;
		}

		static bool Native(Hash hash)
		{
			return Function.Call<bool>(hash);
		}

		void ReadVehicle(Vehicle vehicle, double seconds)
		{
			var velocity = vehicle.Velocity;
			var right = vehicle.RightVector;
			var up = vehicle.UpVector;
			var forward = vehicle.ForwardVector;
			_sample.VelocityX = Dot(velocity, right);
			_sample.VelocityY = Dot(velocity, up);
			_sample.VelocityZ = Dot(velocity, forward);
			// GTA's rotation, in degrees: X pitch, Y roll, Z the heading.
			var rotation = vehicle.Rotation;
			var bodyPitch = rotation.X * DegreesToRadians;
			_sample.Roll = rotation.Y * DegreesToRadians;
			_sample.Yaw = rotation.Z * DegreesToRadians;
			// The road's slope, measured ahead of and behind the vehicle, rather than the body's pitch, which dives under
			// braking and squats under power; the body's where the ground cannot be measured, as over water.
			float slope;
			_sample.Pitch = TrySlope(vehicle.Position, velocity, forward, out slope) ? slope : bodyPitch;
			if (_hasLast && _lastInVehicle)
			{
				var step = seconds - _lastSeconds;
				if (step > 0 && step <= LongestStep)
				{
					var dt = (float)step;
					// The change of the world velocity, in the vehicle's own axes, so a turn's pull to the side is in it.
					var change = new Vector3((velocity.X - _lastVelocityX) / dt, (velocity.Y - _lastVelocityY) / dt, (velocity.Z - _lastVelocityZ) / dt);
					_sample.AccelerationX = Dot(change, right);
					_sample.AccelerationY = Dot(change, up);
					_sample.AccelerationZ = Dot(change, forward);
					_sample.AngularVelocityX = Turn(_lastPitch, bodyPitch) / dt;
					_sample.AngularVelocityY = Turn(_lastYaw, _sample.Yaw) / dt;
					_sample.AngularVelocityZ = Turn(_lastRoll, _sample.Roll) / dt;
					_distance += vehicle.Speed * step;
				}
			}
			_hasLast = true;
			_lastInVehicle = true;
			_lastSeconds = seconds;
			_lastVelocityX = velocity.X;
			_lastVelocityY = velocity.Y;
			_lastVelocityZ = velocity.Z;
			_lastPitch = bodyPitch;
			_lastYaw = _sample.Yaw;
			_lastRoll = _sample.Roll;
			Place(vehicle.Position);
			_sample.Speed = vehicle.Speed;
			_sample.DistanceTraveled = (float)_distance;
			_sample.CurrentEngineRpm = vehicle.CurrentRPM * RedLineRpm;
			SetEngineRange();
			var gear = vehicle.CurrentGear;
			_sample.Gear = (byte)(gear < 0 ? 0 : gear > 255 ? 255 : gear);
			_sample.Accel = Pedal(Control(ControlVehicleAccelerate));
			_sample.Brake = Pedal(Control(ControlVehicleBrake));
			_sample.HandBrake = Pedal(Control(ControlVehicleHandbrake));
			var steer = Control(ControlVehicleMoveLeftRight);
			_sample.Steer = (sbyte)Math.Round((steer < -1f ? -1f : steer > 1f ? 1f : steer) * 127f);
		}

		/// <summary>On foot, as on an exercise bike: the speed, the place, and as pitch the slope of the ground the way the player goes.</summary>
		void ReadOnFoot(Ped ped)
		{
			_hasLast = false;
			var velocity = ped.Velocity;
			var forward = ped.ForwardVector;
			_sample.VelocityX = Dot(velocity, ped.RightVector);
			_sample.VelocityY = Dot(velocity, ped.UpVector);
			_sample.VelocityZ = Dot(velocity, forward);
			float slope;
			if (TrySlope(ped.Position, velocity, forward, out slope))
				_sample.Pitch = slope;
			Place(ped.Position);
			_sample.Speed = ped.Speed;
			// No engine turns, but the red line is still sent: a reader of Forza's packets, x360ce's among them, drops a race on
			// packet whose red line is below 500 as a broken one.
			SetEngineRange();
		}

		/// <summary>The red line and the idle speed every race on packet carries, in a vehicle and on foot.</summary>
		void SetEngineRange()
		{
			_sample.EngineIdleRpm = IdleShare * RedLineRpm;
			_sample.EngineMaxRpm = RedLineRpm;
		}

		/// <summary>Forza's axes have Y up: X east, Y up, Z north, from GTA's X east, Y north, Z up.</summary>
		void Place(Vector3 position)
		{
			_sample.PositionX = position.X;
			_sample.PositionY = position.Z;
			_sample.PositionZ = position.Y;
		}

		/// <summary>The slope of the ground through a place, the way it moves or, slower than walking, the way it faces.</summary>
		/// <returns>False where the ground ahead or behind cannot be measured.</returns>
		bool TrySlope(Vector3 position, Vector3 velocity, Vector3 forward, out float slope)
		{
			slope = 0f;
			var x = velocity.X;
			var y = velocity.Y;
			if (x * x + y * y < SlowSpeed * SlowSpeed)
			{
				x = forward.X;
				y = forward.Y;
			}
			var length = (float)Math.Sqrt(x * x + y * y);
			if (length < 0.001f)
				return false;
			x /= length;
			y /= length;
			float ahead;
			float behind;
			if (!TryGround(position.X + x * SlopeReach, position.Y + y * SlopeReach, position.Z, out ahead)
				|| !TryGround(position.X - x * SlopeReach, position.Y - y * SlopeReach, position.Z, out behind))
				return false;
			slope = Slope(behind, ahead, 2 * SlopeReach);
			return true;
		}

		/// <summary>The slope of the ground between two heights a run apart, in radians, positive uphill.</summary>
		public static float Slope(float behind, float ahead, float run)
		{
			return (float)Math.Atan2(ahead - behind, run);
		}

		bool TryGround(float x, float y, float z, out float height)
		{
			var found = Function.Call<bool>(Hash.GET_GROUND_Z_FOR_3D_COORD, x, y, z + GroundSearchHeight, _ground, false, false);
			height = found ? _ground.GetResult<float>() : 0f;
			return found;
		}

		static float Control(int control)
		{
			return Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, control);
		}

		static byte Pedal(float value)
		{
			return (byte)Math.Round((value < 0f ? 0f : value > 1f ? 1f : value) * 255f);
		}

		static float Dot(Vector3 a, Vector3 b)
		{
			return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
		}

		/// <summary>How far an angle turned from one tick to the next, in radians, the short way round.</summary>
		static float Turn(float from, float to)
		{
			var turn = to - from;
			while (turn > Math.PI)
				turn -= (float)(2 * Math.PI);
			while (turn < -Math.PI)
				turn += (float)(2 * Math.PI);
			return turn;
		}

		void Send()
		{
			ForzaDash.Write(_sample, _packet);
			try
			{
				_udp.Send(_packet, _packet.Length, _target);
			}
			catch (SocketException)
			{
				// Nothing listens, or Windows refused the packet. An error here would stop the script; the next tick sends again.
			}
		}

		void OnAborted(object sender, EventArgs e)
		{
			_udp.Close();
			_ground.Dispose();
		}
	}

	/// <summary>The settings x360ce writes beside the script.</summary>
	public sealed class TelemetrySettings
	{
		public const int DefaultPort = 37150;
		public const int DefaultRate = 60;

		public int Port = DefaultPort;
		public int Rate = DefaultRate;

		/// <summary>Reads the lines Port= and Rate= of a settings file; a value missing or out of range keeps its default.</summary>
		public static TelemetrySettings Read(string path)
		{
			var settings = new TelemetrySettings();
			if (!File.Exists(path))
				return settings;
			foreach (var line in File.ReadAllLines(path))
			{
				var equals = line.IndexOf('=');
				if (equals <= 0)
					continue;
				var key = line.Substring(0, equals).Trim();
				int value;
				if (!int.TryParse(line.Substring(equals + 1).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
					continue;
				if (Is(key, "Port") && value >= 1 && value <= 65535)
					settings.Port = value;
				else if (Is(key, "Rate") && value >= 1 && value <= 120)
					settings.Rate = value;
			}
			return settings;
		}

		static bool Is(string key, string name)
		{
			return string.Equals(key, name, StringComparison.OrdinalIgnoreCase);
		}
	}

	/// <summary>One moment of the game, in the fields and units of a Forza "Dash" packet. A field GTA cannot give stays 0.</summary>
	public sealed class ForzaSample
	{
		/// <summary>True while the player plays story mode; false sends IsRaceOn 0 and nothing else.</summary>
		public bool RaceOn;
		/// <summary>Milliseconds, Windows' tick count; it goes round to 0.</summary>
		public uint TimestampMs;
		/// <summary>Revolutions a minute: the red line, idle and now.</summary>
		public float EngineMaxRpm;
		public float EngineIdleRpm;
		public float CurrentEngineRpm;
		/// <summary>Metres a second squared, in the vehicle's own axes: X right, Y up, Z forward. Without gravity.</summary>
		public float AccelerationX;
		public float AccelerationY;
		public float AccelerationZ;
		/// <summary>Metres a second, in the vehicle's or the player's own axes: X right, Y up, Z forward.</summary>
		public float VelocityX;
		public float VelocityY;
		public float VelocityZ;
		/// <summary>Radians a second: how fast the pitch, the yaw and the roll change.</summary>
		public float AngularVelocityX;
		public float AngularVelocityY;
		public float AngularVelocityZ;
		/// <summary>Radians, as GTA gives them: yaw the heading, 0 facing north and growing turning left; roll GTA's.</summary>
		public float Yaw;
		/// <summary>Radians, positive uphill: the slope of the ground the way the player goes, or the body's pitch where it cannot be measured.</summary>
		public float Pitch;
		public float Roll;
		/// <summary>Metres, in the world, with Y up: X east, Y up, Z north.</summary>
		public float PositionX;
		public float PositionY;
		public float PositionZ;
		/// <summary>Metres a second.</summary>
		public float Speed;
		/// <summary>Metres driven since the script started.</summary>
		public float DistanceTraveled;
		/// <summary>The driver's pedals, 0 to 255.</summary>
		public byte Accel;
		public byte Brake;
		public byte HandBrake;
		/// <summary>0 reverse, 1 and up the gears forward, as GTA counts them.</summary>
		public byte Gear;
		/// <summary>The steering, -127 full left to 127 full right.</summary>
		public sbyte Steer;

		public void Clear()
		{
			RaceOn = false;
			TimestampMs = 0;
			EngineMaxRpm = 0f;
			EngineIdleRpm = 0f;
			CurrentEngineRpm = 0f;
			AccelerationX = 0f;
			AccelerationY = 0f;
			AccelerationZ = 0f;
			VelocityX = 0f;
			VelocityY = 0f;
			VelocityZ = 0f;
			AngularVelocityX = 0f;
			AngularVelocityY = 0f;
			AngularVelocityZ = 0f;
			Yaw = 0f;
			Pitch = 0f;
			Roll = 0f;
			PositionX = 0f;
			PositionY = 0f;
			PositionZ = 0f;
			Speed = 0f;
			DistanceTraveled = 0f;
			Accel = 0;
			Brake = 0;
			HandBrake = 0;
			Gear = 0;
			Steer = 0;
		}
	}

	/// <summary>
	/// Writes Forza Motorsport 7's "Data Out" packet in the "Dash" layout, as Turn 10 lists its fields: little-endian, each
	/// field after the one before. The byte each field starts at:
	/// </summary>
	/// <remarks>
	/// <code>
	///   0 S32 IsRaceOn                 4 U32 TimestampMS
	///   8 F32 EngineMaxRpm            12 F32 EngineIdleRpm          16 F32 CurrentEngineRpm
	///  20 F32 AccelerationX, Y, Z     32 F32 VelocityX, Y, Z        44 F32 AngularVelocityX, Y, Z
	///  56 F32 Yaw                     60 F32 Pitch                  64 F32 Roll
	///  68 to 211: suspension travel, tyre slip, wheel speed, rumble strip, puddle, surface rumble, tyre slip angle,
	///     combined slip and suspension travel in metres, front left, front right, rear left, rear right: 0, GTA gives none.
	/// 212 S32 CarOrdinal, CarClass, CarPerformanceIndex, DrivetrainType, NumCylinders: 0.
	/// 232 F32 PositionX, Y, Z        244 F32 Speed
	/// 248 to 279: power, torque, tyre temperatures, boost and fuel: 0.
	/// 280 F32 DistanceTraveled       284 to 299: lap times: 0.
	/// 300 U16 LapNumber, 302 U8 RacePosition: 0.
	/// 303 U8 Accel                   304 U8 Brake                  305 U8 Clutch: 0
	/// 306 U8 HandBrake               307 U8 Gear                   308 S8 Steer
	/// 309 S8 NormalizedDrivingLine, 310 S8 NormalizedAIBrakeDifference: 0.
	/// </code>
	/// </remarks>
	public static class ForzaDash
	{
		/// <summary>The length of a "Dash" packet, in bytes.</summary>
		public const int Length = 311;

		public static void Write(ForzaSample sample, byte[] packet)
		{
			Array.Clear(packet, 0, Length);
			PutBits(packet, 0, sample.RaceOn ? 1u : 0u);
			PutBits(packet, 4, sample.TimestampMs);
			if (!sample.RaceOn)
				return;
			PutFloat(packet, 8, sample.EngineMaxRpm);
			PutFloat(packet, 12, sample.EngineIdleRpm);
			PutFloat(packet, 16, sample.CurrentEngineRpm);
			PutFloat(packet, 20, sample.AccelerationX);
			PutFloat(packet, 24, sample.AccelerationY);
			PutFloat(packet, 28, sample.AccelerationZ);
			PutFloat(packet, 32, sample.VelocityX);
			PutFloat(packet, 36, sample.VelocityY);
			PutFloat(packet, 40, sample.VelocityZ);
			PutFloat(packet, 44, sample.AngularVelocityX);
			PutFloat(packet, 48, sample.AngularVelocityY);
			PutFloat(packet, 52, sample.AngularVelocityZ);
			PutFloat(packet, 56, sample.Yaw);
			PutFloat(packet, 60, sample.Pitch);
			PutFloat(packet, 64, sample.Roll);
			PutFloat(packet, 232, sample.PositionX);
			PutFloat(packet, 236, sample.PositionY);
			PutFloat(packet, 240, sample.PositionZ);
			PutFloat(packet, 244, sample.Speed);
			PutFloat(packet, 280, sample.DistanceTraveled);
			packet[303] = sample.Accel;
			packet[304] = sample.Brake;
			packet[306] = sample.HandBrake;
			packet[307] = sample.Gear;
			packet[308] = unchecked((byte)sample.Steer);
		}

		/// <summary>A float's bits, read without making anything the garbage collector must clean up.</summary>
		[StructLayout(LayoutKind.Explicit)]
		struct FloatBits
		{
			[FieldOffset(0)]
			public float Float;
			[FieldOffset(0)]
			public uint Bits;
		}

		static void PutFloat(byte[] packet, int offset, float value)
		{
			var bits = new FloatBits();
			bits.Float = value;
			PutBits(packet, offset, bits.Bits);
		}

		static void PutBits(byte[] packet, int offset, uint value)
		{
			packet[offset] = (byte)value;
			packet[offset + 1] = (byte)(value >> 8);
			packet[offset + 2] = (byte)(value >> 16);
			packet[offset + 3] = (byte)(value >> 24);
		}
	}
}
