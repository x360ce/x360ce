// @under-test: PluginApi/WheelTelemetry.cs, PluginApi/IPluginWheelTelemetry.cs, Engine/Common/WheelTelemetrySample.cs, Engine/Common/WheelAxisHelper.cs, Engine/Common/ForceFeedbackState.cs, Plugins/WheelTelemetry/WheelTelemetry.cs, Plugins/WheelTelemetry/WheelTelemetry.Decoders.cs, App.v4/Plugins/PluginManager.cs
// @area: plugins   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using x360ce.App.Plugins;
using x360ce.Engine;
using x360ce.Plugins;

namespace x360ce.Tests
{
	/// <summary>
	/// Verifies wheel telemetry auto-detection (Forza Data Out, EA F1, Codemasters, OutSim, Project CARS UDP,
	/// Assetto Corsa and iRacing Shared Memory), conflict-safe UDP port handling, physics-reactive spring math,
	/// software soft-lock, and steering velocity lead compensation.
	/// </summary>
	[TestClass]
	public class WheelTelemetryTest
	{
		const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

		static PluginTelemetryType TelemetryType()
		{
			var type = TestControllerDevice.Manager.TelemetryTypes.SingleOrDefault(x => x.Name == "Wheel Telemetry");
			Assert.IsNotNull(type, "The WheelTelemetry plugin is not embedded in x360ce.");
			return type;
		}

		/// <summary>Creates an isolated provider instance without background sockets/threads for deterministic decoder tests.</summary>
		static IPluginWheelTelemetry NewOfflineProvider()
		{
			return (IPluginWheelTelemetry)Activator.CreateInstance(TelemetryType().Type, Any, null, new object[] { false }, null);
		}

		static bool TakeUdp(IPluginWheelTelemetry provider, int port, byte[] data, long nowMs)
		{
			return (bool)provider.GetType().GetMethod("TakeUdp").Invoke(provider, new object[] { port, data, data.Length, nowMs });
		}

		static bool TakeSharedMemory(IPluginWheelTelemetry provider, string mapName, byte[] data, long nowMs)
		{
			return (bool)provider.GetType().GetMethod("TakeSharedMemory").Invoke(provider, new object[] { mapName, data, data.Length, nowMs });
		}

		static int[] BoundPorts(IPluginWheelTelemetry provider)
		{
			return (int[])provider.GetType().GetProperty("BoundPorts").GetValue(provider);
		}

		static void WriteInt32(byte[] buf, int offset, int value)
		{
			Array.Copy(BitConverter.GetBytes(value), 0, buf, offset, 4);
		}

		static void WriteUInt32(byte[] buf, int offset, uint value)
		{
			Array.Copy(BitConverter.GetBytes(value), 0, buf, offset, 4);
		}

		static void WriteUInt16(byte[] buf, int offset, ushort value)
		{
			Array.Copy(BitConverter.GetBytes(value), 0, buf, offset, 2);
		}

		static void WriteSingle(byte[] buf, int offset, float value)
		{
			Array.Copy(BitConverter.GetBytes(value), 0, buf, offset, 4);
		}

		static byte[] ForzaPacket(int length, int raceOn, uint timestampMs, float speedMs, float vx = 0f, float ax = 0f, float frontSlip = 0f, float rumbleFl = 0f, float rumbleFr = 0f,
			float ay = 0f, float az = 0f, int gear = -1, float rpm = 4500f, int stripFl = 0, int stripFr = 0, float distance = 0f)
		{
			var buf = new byte[length];
			WriteInt32(buf, 0, raceOn);
			WriteUInt32(buf, 4, timestampMs);
			WriteSingle(buf, 8, 8000f);   // EngineMaxRpm
			WriteSingle(buf, 12, 900f);   // EngineIdleRpm
			WriteSingle(buf, 16, rpm);    // CurrentEngineRpm
			WriteSingle(buf, 20, ax);     // AccelerationX
			WriteSingle(buf, 24, ay);     // AccelerationY
			WriteSingle(buf, 28, az);     // AccelerationZ
			WriteSingle(buf, 32, vx);     // VelocityX
			WriteSingle(buf, 40, speedMs); // VelocityZ
			WriteInt32(buf, 116, stripFl); // WheelOnRumbleStripFrontLeft
			WriteInt32(buf, 120, stripFr); // WheelOnRumbleStripFrontRight
			WriteSingle(buf, 148, rumbleFl); // SurfaceRumbleFrontLeft
			WriteSingle(buf, 152, rumbleFr); // SurfaceRumbleFrontRight
			WriteSingle(buf, 164, frontSlip);
			WriteSingle(buf, 168, frontSlip);
			if (length >= 311)
			{
				// The Dash part: at 232, or 12 bytes later in Forza Horizon's 323 and 324 byte packets.
				var dash = length == 323 || length == 324 ? 244 : 232;
				WriteSingle(buf, dash + 48, distance); // DistanceTraveled
				if (gear >= 0)
					buf[dash + 75] = (byte)gear;   // Gear
			}
			return buf;
		}

		/// <summary>A Forza provider locked on 311-byte packets in <paramref name="gear"/>, its last packet taken at <paramref name="nowMs"/>.</summary>
		static IPluginWheelTelemetry LockedForza(long nowMs, int gear = 3)
		{
			var telemetry = NewOfflineProvider();
			for (var i = 2; i >= 0; i--)
				TakeUdp(telemetry, 37150, ForzaPacket(311, 1, (uint)(nowMs - i * 16), 20f, gear: gear), nowMs - i * 16);
			return telemetry;
		}

		/// <summary>Feeds one packet to a locked provider and returns the sample it published.</summary>
		static WheelTelemetry Take(IPluginWheelTelemetry telemetry, long nowMs, byte[] packet)
		{
			Assert.IsTrue(TakeUdp(telemetry, 37150, packet, nowMs), "A locked Forza packet was not taken.");
			var sample = WheelTelemetry.Neutral;
			Assert.IsTrue(telemetry.TryReadTelemetry(ref sample));
			return sample;
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("The embedded WheelTelemetry plugin loads cleanly as a telemetry provider and appears in plugin documentation")]
		public void Embedded_wheel_telemetry_plugin_loads_and_documents_itself()
		{
			var type = TelemetryType();
			Assert.IsFalse(type.Faulted, type.FaultReason);
			Assert.AreEqual(PluginFileState.Loaded, type.File.State);
			Assert.IsNull(type.File.Reason, "A telemetry-only plugin should load without a missing-IPluginDevice warning.");
			var md = PluginDocs.Markdown(type.File);
			StringAssert.Contains(md, "# WheelTelemetry");
			StringAssert.Contains(md, "## Wheel Telemetry");
			StringAssert.Contains(md, "IPluginWheelTelemetry");
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("Forza Data Out (232/323 bytes) locks after 3 advancing packets, extracts speed, slip, grip and curb impulse, and stays quiet while paused")]
		public void Forza_data_out_auto_detects_and_decodes_physics()
		{
			using (var provider = (IDisposable)NewOfflineProvider())
			{
				var telemetry = (IPluginWheelTelemetry)provider;
				var sample = WheelTelemetry.Neutral;
				// First 2 packets build confidence; 3rd advancing packet locks on.
				Assert.IsFalse(TakeUdp(telemetry, 37150, ForzaPacket(323, 1, 100, 30f), 1000));
				Assert.IsFalse(TakeUdp(telemetry, 37150, ForzaPacket(323, 1, 116, 30f), 1016));
				Assert.IsTrue(TakeUdp(telemetry, 37150, ForzaPacket(323, 1, 132, 30f, vx: 4f, ax: 9.80665f, frontSlip: 1.5f, rumbleFl: 0f, rumbleFr: 1f), 1032));
				Assert.IsTrue(telemetry.TryReadTelemetry(ref sample));
				Assert.AreEqual("Forza Data Out", sample.Source);
				Assert.AreEqual(Math.Sqrt(30 * 30 + 4 * 4) * 3.6, sample.SpeedKmh, 0.5);
				Assert.AreEqual(1.0f, sample.LateralG, 0.05f);
				Assert.IsTrue(sample.AlignAngleDeg > 5f, "Oversteer slip angle should pull wheel into counter-steer.");
				Assert.IsTrue(sample.FrontGrip < 0.7f, "High front tire slip should reduce front grip.");
				Assert.IsTrue(sample.Impulse > 0.3f, "Right curb rumble should produce positive right impulse.");

				// Paused packet (IsRaceOn = 0) stays recognised but does not refresh TimestampMs.
				var prevTs = sample.TimestampMs;
				Assert.IsTrue(TakeUdp(telemetry, 37150, ForzaPacket(323, 0, 148, 30f), 1048));
				Assert.IsTrue(telemetry.TryReadTelemetry(ref sample));
				Assert.AreEqual(prevTs, sample.TimestampMs, "Paused Forza packet must not refresh active driving timestamp.");
			}
		}

		const float G = 9.80665f;

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("Forza Data Out carries the gear, forward-back and up-down G, the distance and the position, from the Dash part at 232 or 12 bytes later in Forza Horizon's 324-byte packet")]
		public void Forza_data_out_decodes_gear_accelerations_distance_and_position()
		{
			foreach (var length in new[] { 311, 324 })
			{
				var telemetry = LockedForza(1000);
				using ((IDisposable)telemetry)
				{
					var packet = ForzaPacket(length, 1, 1016, 20f, ay: 0.5f * G, az: -0.8f * G, gear: 4, distance: 1234.5f);
					var dash = length == 324 ? 244 : 232;
					WriteSingle(packet, dash, 100f);
					WriteSingle(packet, dash + 4, 5f);
					WriteSingle(packet, dash + 8, -200f);
					var s = Take(telemetry, 1016, packet);
					Assert.AreEqual(4, s.Gear, length + " bytes: the gear byte is not read.");
					Assert.AreEqual(-0.8f, s.LongitudinalG, 0.01f, length + " bytes: braking is not forward-back G.");
					Assert.AreEqual(0.5f, s.VerticalG, 0.01f, length + " bytes: up-down G is not read.");
					Assert.AreEqual(1234.5f, s.DistanceMeters, 0.01f, length + " bytes: the game's distance is not read.");
					Assert.AreEqual(100f, s.PositionX, 0.01f, length + " bytes: position X");
					Assert.AreEqual(5f, s.PositionY, 0.01f, length + " bytes: position Y");
					Assert.AreEqual(-200f, s.PositionZ, 0.01f, length + " bytes: position Z");
				}
			}
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("A Forza Sled packet sends no gear or position, so the distance is the speed added up over the game's clock, a gap of over half a second left out, and starts again after a pause")]
		public void Forza_sled_distance_is_added_up_from_speed()
		{
			using (var provider = (IDisposable)NewOfflineProvider())
			{
				var telemetry = (IPluginWheelTelemetry)provider;
				for (uint t = 0; t <= 1000; t += 100)
					TakeUdp(telemetry, 37150, ForzaPacket(232, 1, t, 20f), 5000 + t);
				var s = WheelTelemetry.Neutral;
				Assert.IsTrue(telemetry.TryReadTelemetry(ref s));
				Assert.AreEqual(-1, s.Gear, "A Sled packet has no gear.");
				Assert.IsTrue(float.IsNaN(s.PositionX), "A Sled packet has no position.");
				Assert.AreEqual(20f, s.DistanceMeters, 0.01f, "One second at 20 m/s is 20 m.");
				// The game's clock jumps two seconds while the packets keep arriving, so the lock holds.
				s = Take(telemetry, 6100, ForzaPacket(232, 1, 3000, 20f));
				Assert.AreEqual(20f, s.DistanceMeters, 0.01f, "A gap of two seconds must add nothing.");
				TakeUdp(telemetry, 37150, ForzaPacket(232, 0, 3100, 0f), 6200);
				s = Take(telemetry, 6300, ForzaPacket(232, 1, 3200, 20f));
				Assert.AreEqual(0f, s.DistanceMeters, 0.01f, "A pause must start the distance again.");
			}
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("Hard braking at 1 G is no hit: no kick and no jolt")]
		public void Forza_braking_is_no_impact()
		{
			var telemetry = LockedForza(1000);
			using ((IDisposable)telemetry)
			{
				var s = Take(telemetry, 1016, ForzaPacket(311, 1, 1016, 20f, az: -1f * G, gear: 3));
				Assert.AreEqual(0f, s.Impulse, 0.001f, "Braking must not kick the wheel.");
				Assert.AreEqual(0f, s.Jolt, 0.001f, "Braking must not jolt the wheel.");
			}
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("A hit from the right, the -9.9 G sideways spike of the owner's truck hit, kicks the wheel left harder than a 5.1 G hit, for about 200 ms, and does not pull the spring's centre aside")]
		public void Forza_side_hit_kicks_the_wheel_away_from_the_hit()
		{
			float truck, smaller;
			var telemetry = LockedForza(1000);
			using ((IDisposable)telemetry)
			{
				var s = Take(telemetry, 1016, ForzaPacket(311, 1, 1016, 12f, ax: -9.9f * G, gear: 3));
				truck = s.Impulse;
				Assert.IsTrue(truck < -0.5f, "A hit from the right must kick the wheel left, was " + truck + ".");
				Assert.AreEqual(0f, s.Jolt, 0.001f, "A hit from the side is no jolt.");
				Assert.IsTrue(s.AlignAngleDeg >= -3.01f, "A hit's spike must not pull the spring's centre aside, was " + s.AlignAngleDeg + "°.");
				s = Take(telemetry, 1116, ForzaPacket(311, 1, 1116, 12f, gear: 3));
				Assert.IsTrue(s.Impulse < 0f && s.Impulse > truck, "The kick must fade over the next samples, was " + s.Impulse + ".");
				s = Take(telemetry, 1226, ForzaPacket(311, 1, 1226, 12f, gear: 3));
				Assert.AreEqual(0f, s.Impulse, 0.001f, "The kick must be over after 200 ms.");
			}
			telemetry = LockedForza(1000);
			using ((IDisposable)telemetry)
				smaller = Take(telemetry, 1016, ForzaPacket(311, 1, 1016, 12f, ax: 5.1f * G, gear: 3)).Impulse;
			Assert.IsTrue(smaller > 0f, "A hit from the left must kick the wheel right.");
			Assert.IsTrue(Math.Abs(truck) > smaller, "The harder hit must kick harder.");
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("A hit from the front is a jolt without a direction, and a jump of acceleration while the gear changes is no hit")]
		public void Forza_front_hit_jolts_unless_the_gear_changes()
		{
			var telemetry = LockedForza(1000);
			using ((IDisposable)telemetry)
			{
				var s = Take(telemetry, 1016, ForzaPacket(311, 1, 1016, 12f, az: -6f * G, gear: 3));
				Assert.IsTrue(s.Jolt > 0.5f, "A hit from the front must jolt the wheel, was " + s.Jolt + ".");
				Assert.AreEqual(0f, s.Impulse, 0.001f, "A hit from the front has no side.");
			}
			telemetry = LockedForza(1000);
			using ((IDisposable)telemetry)
			{
				var s = Take(telemetry, 1016, ForzaPacket(311, 1, 1016, 12f, az: 5f * G, gear: 4));
				Assert.AreEqual(0f, s.Jolt, 0.001f, "The lurch of a gear change is no hit.");
			}
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("A front wheel on a rumble strip kicks the wheel towards its side")]
		public void Forza_rumble_strip_kicks_towards_its_side()
		{
			var telemetry = LockedForza(1000);
			using ((IDisposable)telemetry)
			{
				Assert.IsTrue(Take(telemetry, 1016, ForzaPacket(311, 1, 1016, 20f, gear: 3, stripFr: 1)).Impulse > 0.3f, "Right strip");
				Assert.IsTrue(Take(telemetry, 1032, ForzaPacket(311, 1, 1032, 20f, gear: 3, stripFl: 1)).Impulse < -0.3f, "Left strip");
				Assert.AreEqual(0f, Take(telemetry, 1048, ForzaPacket(311, 1, 1048, 20f, gear: 3, stripFl: 1, stripFr: 1)).Impulse, 0.001f, "Both strips");
			}
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("While telemetry drives the spring the game's rumble plays at the Gear change share for 150 ms after the gear changes and whole after it, not at all from Telemetry alone, and whole without fresh telemetry")]
		public void Game_rumble_is_scaled_while_the_gear_changes()
		{
			var ff = new ForceFeedbackState();
			var s = new WheelTelemetrySample { Active = true, SpeedKmh = 40f, RpmFraction = 0.9f, Gear = 2, TimestampMs = 1000 };
			Assert.AreEqual(100, ff.GameRumblePercent(s, 1000, false, 0), "No gear change yet.");
			s.Gear = 3;
			s.TimestampMs = 1016;
			Assert.AreEqual(0, ff.GameRumblePercent(s, 1016 + 100, false, 0), "100 ms after the gear changed.");
			Assert.AreEqual(25, ff.GameRumblePercent(s, 1016 + 120, false, 25), "The share the slider sets.");
			Assert.AreEqual(100, ff.GameRumblePercent(s, 1016 + 200, false, 0), "200 ms after the gear changed.");
			Assert.AreEqual(1, ff.Counts.GearChanges);
			Assert.AreEqual(3, ff.LastGear);
			Assert.AreEqual(0, ff.GameRumblePercent(s, 1016 + 200, true, 100), "From telemetry alone the game's rumble is ignored.");
			Assert.AreEqual(100, ff.GameRumblePercent(s, 1016 + 2000, true, 0), "Stale telemetry leaves the game's rumble whole.");
			Assert.AreEqual(100, ff.GameRumblePercent(WheelTelemetrySample.Inactive, 1016, true, 0), "No telemetry leaves the game's rumble whole.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("From the owner's GTA V drive: an upshift, the engine falling from 95 % to 70 % while the car gains speed, silences the game's 110 rumble at Gear change 0 %, and a downshift while braking is no upshift")]
		public void Gta_upshift_rumble_is_silenced_at_gear_change_zero()
		{
			var ff = new ForceFeedbackState();
			var s = new WheelTelemetrySample { Active = true, SpeedKmh = 30f, RpmFraction = 0.95f, Gear = -1, TimestampMs = 4600 };
			ff.GameRumblePercent(s, 4600, false, 0);
			s.SpeedKmh = 30.5f;
			s.RpmFraction = 0.70f;
			s.TimestampMs = 4620;
			var percent = ff.GameRumblePercent(s, 4630, false, 0);
			Assert.AreEqual(0, 110 * percent / 100, "The upshift's rumble must not play at Gear change 0 %.");
			s.SpeedKmh = 20f;
			s.RpmFraction = 0.95f;
			s.TimestampMs = 13200;
			ff.GameRumblePercent(s, 13200, false, 0);
			s.SpeedKmh = 18f;
			s.RpmFraction = 0.75f;
			s.TimestampMs = 13220;
			Assert.AreEqual(100, ff.GameRumblePercent(s, 13230, false, 0), "Slowing down is no upshift.");
		}

		/// <summary>
		/// Plays the game's large motor at <paramref name="pulse"/> from <paramref name="startMs"/> for <paramref name="lengthMs"/>
		/// through <see cref="ForceFeedbackState.ShapeGameRumble"/>, one call a millisecond with a telemetry sample every 20 ms,
		/// the gear going from 2 to 3 on the sample at <paramref name="gearMs"/> (or never when it is 0), and returns what the
		/// large motor played each millisecond from <paramref name="fromMs"/> to <paramref name="toMs"/>.
		/// </summary>
		static byte[] PlayPulse(ForceFeedbackState ff, int gearChangePercent, long startMs, long lengthMs, byte pulse,
			long gearMs, long fromMs, long toMs)
		{
			var played = new byte[toMs - fromMs];
			var s = new WheelTelemetrySample { Active = true, SpeedKmh = 40f, RpmFraction = 0.9f, Gear = 2 };
			for (var now = fromMs; now < toMs; now++)
			{
				var sampleMs = now - now % 20;
				s.TimestampMs = sampleMs;
				s.Gear = gearMs > 0 && sampleMs >= gearMs ? 3 : 2;
				var large = now >= startMs && now < startMs + lengthMs ? pulse : (byte)0;
				byte small = 0;
				ff.ShapeGameRumble(s, now, false, gearChangePercent, ref large, ref small);
				played[now - fromMs] = large;
			}
			return played;
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("From the owner's GTA V drive: a gear change kick that starts 40 ms before the telemetry sample showing the new gear is held, then silenced from its start at Gear change 0 %")]
		public void Gear_change_kick_before_its_sample_is_silenced_at_gear_change_zero()
		{
			var ff = new ForceFeedbackState();
			// The kick, 110 for 80 ms from 1020; the sample with the new gear arrives at 1060.
			var played = PlayPulse(ff, 0, 1020, 80, 110, 1060, 1000, 1300);
			Assert.AreEqual(1, ff.Counts.GearChanges, "The gear change is found.");
			for (var i = 0; i < played.Length; i++)
				Assert.AreEqual(0, played[i], "The kick played at " + (1000 + i) + " ms.");
			// The slider's share plays the kick, from the end of the wait to its own end.
			ff = new ForceFeedbackState();
			played = PlayPulse(ff, 50, 1020, 80, 110, 1060, 1000, 1300);
			Assert.AreEqual(0, played[1020 + ForceFeedbackState.GearChangeHoldMs - 1 - 1000], "Still held.");
			Assert.AreEqual(55, played[1020 + ForceFeedbackState.GearChangeHoldMs - 1000], "Half the kick once the wait is over.");
			Assert.AreEqual(0, played[1100 - 1000], "The kick's end plays at once.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("Without a gear change the game's rumble plays whole after the wait, falls at once, and is not held while gear changes play whole or the telemetry is stale")]
		public void Game_rumble_without_a_gear_change_plays_after_the_wait()
		{
			var hold = ForceFeedbackState.GearChangeHoldMs;
			var ff = new ForceFeedbackState();
			var played = PlayPulse(ff, 0, 1020, 200, 110, 0, 1000, 1300);
			Assert.AreEqual(0, ff.Counts.GearChanges);
			Assert.AreEqual(0, played[1020 + hold - 1 - 1000], "Held for the wait.");
			Assert.AreEqual(110, played[1020 + hold - 1000], "Whole once the wait is over.");
			Assert.AreEqual(110, played[1219 - 1000], "Whole to its end.");
			Assert.AreEqual(0, played[1220 - 1000], "Its fall plays at once.");
			// What the motors play changes when the wait ends, though the game sent nothing new then.
			ff = new ForceFeedbackState();
			var s = new WheelTelemetrySample { Active = true, Gear = 2, TimestampMs = 1000 };
			byte large = 110, small = 0;
			Assert.IsFalse(ff.ShapeGameRumble(s, 1000, false, 0, ref large, ref small), "Held: nothing new plays.");
			large = 110;
			Assert.IsTrue(ff.ShapeGameRumble(s, 1000 + hold, false, 0, ref large, ref small), "The wait is over: the rise is new.");
			Assert.AreEqual(110, large);
			// Gear change at 100 % holds nothing.
			played = PlayPulse(new ForceFeedbackState(), 100, 1020, 200, 110, 0, 1000, 1300);
			Assert.AreEqual(110, played[1020 - 1000], "Gear changes whole: no wait.");
			// Stale or no telemetry holds nothing.
			ff = new ForceFeedbackState();
			large = 110;
			ff.ShapeGameRumble(s, 1000 + 2000, false, 0, ref large, ref small);
			Assert.AreEqual(110, large, "Stale telemetry: no wait.");
			large = 110;
			ff.ShapeGameRumble(WheelTelemetrySample.Inactive, 0, false, 0, ref large, ref small);
			Assert.AreEqual(110, large, "No telemetry: no wait.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("A jolt shakes the wheel, its push changing side every 50 ms, scaled by Curb bumps, only where the jolt is switched on")]
		public void Jolt_shakes_the_wheel_both_ways()
		{
			var curve = new SpringCurve();
			curve.Set(60, 12, 60, 40, 900, 100, 20);
			var s = new WheelTelemetrySample { Active = true, SpeedKmh = 100f, FrontGrip = 1f, DampingScale = 1f, Jolt = 0.5f, TimestampMs = 1000 };
			var on = WheelTelemetryTuning.Default;
			on.JoltEnabled = true;
			var terms = new SpringTerms();
			double damping;
			WheelTelemetryMath.ComputeSpringForce(curve, SpringCalibration.Center, 900, 20, 60, s, 1000, 0, 0, on, ref terms, out damping);
			Assert.AreEqual(20.0, terms.JoltPercent, 0.01, "Half a jolt is half the kick's most.");
			WheelTelemetryMath.ComputeSpringForce(curve, SpringCalibration.Center, 900, 20, 60, s, 1050, 0, 0, on, ref terms, out damping);
			Assert.AreEqual(-20.0, terms.JoltPercent, 0.01, "50 ms later the push changes side.");
			on.CurbStrengthPercent = 50;
			WheelTelemetryMath.ComputeSpringForce(curve, SpringCalibration.Center, 900, 20, 60, s, 1000, 0, 0, on, ref terms, out damping);
			Assert.AreEqual(10.0, terms.JoltPercent, 0.01, "Curb bumps scales the jolt.");
			WheelTelemetryMath.ComputeSpringForce(curve, SpringCalibration.Center, 900, 20, 60, s, 1000, 0, 0, WheelTelemetryTuning.Default, ref terms, out damping);
			Assert.AreEqual(0.0, terms.JoltPercent, 0.001, "The game's rumble keeps the shake while the jolt is off.");
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("EA F1 2023+ locks immediately on header and combines speed (packet 6) with lateral G (packet 0)")]
		public void Ea_f1_locks_immediately_and_combines_telemetry_and_motion()
		{
			using (var provider = (IDisposable)NewOfflineProvider())
			{
				var telemetry = (IPluginWheelTelemetry)provider;
				// Packet 0 (Motion): header 29 bytes + 22 cars * 60 bytes.
				var motion = new byte[29 + 22 * 60];
				WriteUInt16(motion, 0, 2024);
				motion[6] = 0;   // PacketId = Motion
				motion[27] = 2;  // PlayerCarIndex = 2
				WriteSingle(motion, 29 + 2 * 60 + 36, 2.0f); // 2G lateral
				Assert.IsTrue(TakeUdp(telemetry, 37150, motion, 2000));

				// Packet 6 (Car Telemetry): header 29 bytes + 22 cars * 60 bytes.
				var carTel = new byte[29 + 22 * 60];
				WriteUInt16(carTel, 0, 2024);
				carTel[6] = 6;   // PacketId = CarTelemetry
				carTel[27] = 2;  // PlayerCarIndex = 2
				WriteUInt16(carTel, 29 + 2 * 60, 240); // 240 km/h
				Assert.IsTrue(TakeUdp(telemetry, 37150, carTel, 2016));

				var sample = WheelTelemetry.Neutral;
				Assert.IsTrue(telemetry.TryReadTelemetry(ref sample));
				Assert.AreEqual("EA F1", sample.Source);
				Assert.AreEqual(240f, sample.SpeedKmh, 0.1f);
				Assert.AreEqual(2.0f, sample.LateralG, 0.05f);
			}
		}

		[TestMethod, TestCategory("plugins")]
		[Description("Codemasters 264-byte UDP, OutSim 64-byte UDP, and Project CARS UDP decode accurately and garbage packets are rejected")]
		public void Codemasters_outsim_pcars_and_garbage_rejection()
		{
			// 1. Garbage / NaN packets must never lock.
			using (var provider = (IDisposable)NewOfflineProvider())
			{
				var telemetry = (IPluginWheelTelemetry)provider;
				var junk = new byte[323];
				WriteInt32(junk, 0, 1);
				WriteSingle(junk, 8, float.NaN);
				for (var i = 0; i < 5; i++)
					Assert.IsFalse(TakeUdp(telemetry, 37150, junk, 1000 + i * 20));
				var s = WheelTelemetry.Neutral;
				Assert.IsFalse(telemetry.TryReadTelemetry(ref s));
			}

			// 2. Codemasters 264-byte UDP (DiRT Rally / WRC).
			using (var provider = (IDisposable)NewOfflineProvider())
			{
				var telemetry = (IPluginWheelTelemetry)provider;
				for (var i = 0; i < 3; i++)
				{
					var cm = new byte[264];
					WriteSingle(cm, 0, 10f + i * 0.02f); // Time
					WriteSingle(cm, 28, 25f);             // Speed 25 m/s = 90 km/h
					WriteSingle(cm, 136, 1.2f);           // Lateral G
					WriteSingle(cm, 148, 400f);           // RPM/10
					WriteSingle(cm, 252, 700f);           // MaxRPM/10
					WriteSingle(cm, 92, 0f);              // SuspVel FL
					WriteSingle(cm, 96, 3.0f);            // SuspVel FR (curb hit)
					TakeUdp(telemetry, 37150, cm, 3000 + i * 20);
				}
				var sample = WheelTelemetry.Neutral;
				Assert.IsTrue(telemetry.TryReadTelemetry(ref sample));
				Assert.AreEqual("Codemasters UDP", sample.Source);
				Assert.AreEqual(90f, sample.SpeedKmh, 0.2f);
				Assert.IsTrue(sample.Impulse > 0.2f);
			}

			// 3. OutSim 64-byte UDP (LFS / BeamNG).
			using (var provider = (IDisposable)NewOfflineProvider())
			{
				var telemetry = (IPluginWheelTelemetry)provider;
				for (var i = 0; i < 3; i++)
				{
					var os = new byte[64];
					WriteUInt32(os, 0, (uint)(1000 + i * 16));
					WriteSingle(os, 28, -9.80665f); // -1 G lateral
					WriteSingle(os, 40, 20f);       // 20 m/s = 72 km/h
					TakeUdp(telemetry, 37150, os, 4000 + i * 16);
				}
				var sample = WheelTelemetry.Neutral;
				Assert.IsTrue(telemetry.TryReadTelemetry(ref sample));
				Assert.AreEqual("OutSim", sample.Source);
				Assert.AreEqual(72f, sample.SpeedKmh, 0.2f);
				Assert.AreEqual(-1f, sample.LateralG, 0.05f);
			}

			// 4. Project CARS 2 UDP (559 bytes) + automatic switch after silence.
			using (var provider = (IDisposable)NewOfflineProvider())
			{
				var telemetry = (IPluginWheelTelemetry)provider;
				var pc2 = new byte[559];
				pc2[10] = 0; // CarPhysics
				WriteSingle(pc2, 36, 40f); // 144 km/h
				WriteSingle(pc2, 72, 9.80665f);
				Assert.IsTrue(TakeUdp(telemetry, 5606, pc2, 5000));
				var sample = WheelTelemetry.Neutral;
				Assert.IsTrue(telemetry.TryReadTelemetry(ref sample));
				Assert.AreEqual("Project CARS 2 UDP", sample.Source);
				Assert.AreEqual(144f, sample.SpeedKmh, 0.2f);
			}
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("Assetto Corsa and iRacing Shared Memory snapshots decode accurately and ignore stalled ticks")]
		public void Shared_memory_decoders_read_assetto_corsa_and_iracing()
		{
			using (var provider = (IDisposable)NewOfflineProvider())
			{
				var telemetry = (IPluginWheelTelemetry)provider;
				var ac = new byte[256];
				WriteInt32(ac, 0, 42);      // packetId
				WriteSingle(ac, 28, 165f);  // speedKmh
				WriteSingle(ac, 44, 1.5f);  // accG[0] lateral
				WriteSingle(ac, 56, 2.2f);  // wheelSlip FL
				WriteSingle(ac, 60, 2.2f);  // wheelSlip FR
				Assert.IsTrue(TakeSharedMemory(telemetry, @"Local\acpmf_physics", ac, 1000));
				// Same packetId again (paused/stalled) must be ignored.
				Assert.IsFalse(TakeSharedMemory(telemetry, @"Local\acpmf_physics", ac, 1020));

				var sample = WheelTelemetry.Neutral;
				Assert.IsTrue(telemetry.TryReadTelemetry(ref sample));
				Assert.AreEqual("Assetto Corsa", sample.Source);
				Assert.AreEqual(165f, sample.SpeedKmh, 0.1f);
				Assert.IsTrue(sample.FrontGrip < 0.8f);
			}

			using (var provider = (IDisposable)NewOfflineProvider())
			{
				var telemetry = (IPluginWheelTelemetry)provider;
				// Self-describing iRacing header: 2 float vars ("Speed" @ 0, "LatAccel" @ 4) in buffer @ 512.
				var ir = new byte[1024];
				WriteInt32(ir, 0, 2);     // version
				WriteInt32(ir, 4, 1);     // status = connected
				WriteInt32(ir, 24, 2);    // numVars
				WriteInt32(ir, 28, 128);  // varHeaderOffset
				WriteInt32(ir, 32, 1);    // numBuf
				WriteInt32(ir, 36, 64);   // bufLen
				WriteInt32(ir, 48, 77);   // tickCount
				WriteInt32(ir, 52, 512);  // bufOffset
				// Var 0: Speed (float @ 0)
				WriteInt32(ir, 128 + 0, 4);
				WriteInt32(ir, 128 + 4, 0);
				Encoding.ASCII.GetBytes("Speed").CopyTo(ir, 128 + 16);
				// Var 1: LatAccel (float @ 4)
				WriteInt32(ir, 128 + 144 + 0, 4);
				WriteInt32(ir, 128 + 144 + 4, 4);
				Encoding.ASCII.GetBytes("LatAccel").CopyTo(ir, 128 + 144 + 16);
				// Data values
				WriteSingle(ir, 512 + 0, 50f);          // 50 m/s = 180 km/h
				WriteSingle(ir, 512 + 4, 19.6133f);     // 2 G

				Assert.IsTrue(TakeSharedMemory(telemetry, @"Local\IRSDKMemMapFileName", ir, 2000));
				var sample = WheelTelemetry.Neutral;
				Assert.IsTrue(telemetry.TryReadTelemetry(ref sample));
				Assert.AreEqual("iRacing", sample.Source);
				Assert.AreEqual(180f, sample.SpeedKmh, 0.2f);
				Assert.AreEqual(2.0f, sample.LateralG, 0.05f);
			}
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("Default UDP settings never bind standard game ports, skip occupied ports without error, and relay packets when forwarding is enabled")]
		public void Udp_listener_never_overtakes_standard_ports_and_handles_conflicts_cleanly()
		{
			// Pick two ephemeral free ports for testing conflict skipping and forwarding without touching real app ports.
			int freeDedicated;
			int forwardTarget;
			using (var probe1 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
			using (var probe2 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
			{
				probe1.Bind(new IPEndPoint(IPAddress.Loopback, 0));
				probe2.Bind(new IPEndPoint(IPAddress.Loopback, 0));
				freeDedicated = ((IPEndPoint)probe1.LocalEndPoint).Port;
				forwardTarget = ((IPEndPoint)probe2.LocalEndPoint).Port;
			}

			// 1. Default config (listenStandardPorts = false): binds ONLY the dedicated port, never 5300/20777/4123/5606.
			using (var provider = (IDisposable)TelemetryType().NewInstance())
			{
				var telemetry = (IPluginWheelTelemetry)provider;
				var settings = (IPluginWheelTelemetrySettings)provider;
				settings.Configure(freeDedicated, false, forwardTarget);
				var dummy = WheelTelemetry.Neutral;
				telemetry.TryReadTelemetry(ref dummy); // starts lazy worker

				Assert.IsTrue(WaitUntil(() => BoundPorts(telemetry).Contains(freeDedicated), 2000),
					"Dedicated port was not bound. Status: " + settings.PortStatus);
				var ports = BoundPorts(telemetry);
				Assert.IsFalse(ports.Contains(5300), "Must never bind standard port 5300 by default.");
				Assert.IsFalse(ports.Contains(20777), "Must never bind standard port 20777 by default.");
				Assert.IsFalse(ports.Contains(4123), "Must never bind standard port 4123 by default.");
				Assert.IsFalse(ports.Contains(5606), "Must never bind port 5606 by default.");

				// Verify UDP forwarding relays incoming Forza packets intact to forwardTarget while decoding them.
				using (var sink = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
				using (var sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
				{
					sink.ReceiveTimeout = 1000;
					sink.Bind(new IPEndPoint(IPAddress.Loopback, forwardTarget));
					var dest = new IPEndPoint(IPAddress.Loopback, freeDedicated);
					for (uint i = 1; i <= 3; i++)
					{
						var pkt = ForzaPacket(323, 1, i * 16, 35f);
						sender.SendTo(pkt, dest);
						var relayed = new byte[512];
						var n = sink.Receive(relayed);
						Assert.AreEqual(323, n, "Forwarded packet length did not match original datagram.");
					}
					var received = WheelTelemetry.Neutral;
					Assert.IsTrue(WaitUntil(() => telemetry.TryReadTelemetry(ref received) && received.Source == "Forza Data Out", 1500),
						"Live Forza UDP packet was not decoded.");
					Assert.AreEqual(126f, received.SpeedKmh, 0.5f);
				}
			}

			// 2. When a port is already held by another app, provider skips it cleanly without throwing and reports the conflict.
			using (var otherApp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
			{
				otherApp.ExclusiveAddressUse = true;
				otherApp.Bind(new IPEndPoint(IPAddress.Loopback, 0));
				var heldPort = ((IPEndPoint)otherApp.LocalEndPoint).Port;

				using (var provider = (IDisposable)TelemetryType().NewInstance())
				{
					var telemetry = (IPluginWheelTelemetry)provider;
					var settings = (IPluginWheelTelemetrySettings)provider;
					settings.Configure(heldPort, false, 0);
					var dummy = WheelTelemetry.Neutral;
					telemetry.TryReadTelemetry(ref dummy);

					Assert.IsTrue(WaitUntil(() => settings.PortStatus != null && settings.PortStatus.Contains("in use"), 2000),
						"Expected conflict status note, got: " + settings.PortStatus);
					Assert.IsFalse(BoundPorts(telemetry).Contains(heldPort), "Provider must not steal an already-held port.");
				}
			}
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("WheelTelemetryMath applies staleness decay, counter-steer equilibrium shift, front-grip lightening above friction, curb impulses and software soft-lock")]
		public void Wheel_telemetry_math_shapes_spring_and_soft_lock()
		{
			var curve = new SpringCurve();
			curve.Set(60, 12, 60, 40, 900, 100, 20);
			var center = SpringCalibration.Center;

			// 1. Staleness decay: fresh <= 500 ms, mid-decay 625 ms, expired >= 750 ms.
			Assert.AreEqual(1.0, WheelTelemetryMath.Freshness(1000, 1400), 0.001);
			Assert.AreEqual(0.5, WheelTelemetryMath.Freshness(1000, 1625), 0.01);
			Assert.AreEqual(0.0, WheelTelemetryMath.Freshness(1000, 1800), 0.001);

			// 2. Counter-steer equilibrium shift: AlignAngleDeg = +30 pulls right even when wheel sits at centre.
			var oversteer = new WheelTelemetrySample
			{
				Active = true,
				SpeedKmh = 100f,
				AlignAngleDeg = 30f,
				FrontGrip = 1f,
				DampingScale = 1f,
				TimestampMs = 1000,
			};
			double damp;
			var forceAtCenter = WheelTelemetryMath.ComputeSpringForce(curve, center, 900, 20, 60, oversteer, 1010, 0.0, 0, out damp);
			Assert.IsTrue(forceAtCenter > 20, "Positive AlignAngleDeg must pull wheel right at centre; got " + forceAtCenter);

			// 3. Front-grip lightening: drops car torque above 20 % friction floor without removing friction floor.
			var peakPos = center + (int)Math.Round(60.0 * SpringCalibration.AxisMax / 900.0);
			var fullGrip = new WheelTelemetrySample { Active = true, FrontGrip = 1f, DampingScale = 1f, TimestampMs = 1000 };
			var understeer = new WheelTelemetrySample { Active = true, FrontGrip = 0f, DampingScale = 1.5f, TimestampMs = 1000 };
			var fFull = WheelTelemetryMath.ComputeSpringForce(curve, peakPos, 900, 20, 60, fullGrip, 1010, 0.0, 0, out damp);
			var fUnder = WheelTelemetryMath.ComputeSpringForce(curve, peakPos, 900, 20, 60, understeer, 1010, 0.0, 0, out damp);
			Assert.AreEqual(-80, fFull, 2, "Full grip at Peak = 20 friction + 60 strength.");
			Assert.AreEqual(-20, fUnder, 2, "Zero front grip must leave only the 20 % wheel friction floor.");
			Assert.AreEqual(1.5, damp, 0.01, "DampingScale must propagate to damper multiplier.");

			// 4. Universal software soft-lock: 270° lock on a 900° wheel ramps strongly to -100 % past +135°.
			var pastStopPos = center + (int)Math.Round(145.0 * SpringCalibration.AxisMax / 900.0);
			var softStopForce = WheelTelemetryMath.ComputeSpringForce(curve, pastStopPos, 900, 20, 60, WheelTelemetrySample.Inactive, 1010, 0.0, 270, out damp);
			Assert.AreEqual(-100, softStopForce, "Past the 270° soft-lock stop the wheel must push back at full force.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("WheelAxisHelper scales software soft-lock range cleanly and boosts fast counter-steer turns with velocity lead")]
		public void Wheel_axis_helper_scales_soft_lock_and_applies_lead()
		{
			var state = new SteeringLeadState();
			var freq = Stopwatch.Frequency;

			// Soft-lock scaling: 900° wheel narrowed to 270° (3.333x) reaches full right (65535) at +135°.
			var at135 = (ushort)(SpringCalibration.Center + (int)Math.Round(135.0 * SpringCalibration.AxisMax / 900.0));
			var scaled = WheelAxisHelper.ProcessSteeringAxis(at135, 900.0 / 270.0, 0, freq, freq, ref state);
			Assert.AreEqual(ushort.MaxValue, scaled, "135° on a 900° wheel with 270° soft-lock must reach full stick deflection.");

			// Lead compensation: turning rapidly right produces output ahead of raw position, then settles back when steady.
			state = new SteeringLeadState();
			var t = freq;
			var stepTicks = freq / 100; // 10 ms steps
			WheelAxisHelper.ProcessSteeringAxis(32768, 1.0, 40, t, freq, ref state);
			t += stepTicks;
			var rawTurn = (ushort)(32768 + 3000);
			var boosted = WheelAxisHelper.ProcessSteeringAxis(rawTurn, 1.0, 40, t, freq, ref state);
			Assert.IsTrue(boosted > rawTurn, "Fast rightward turn must receive positive lead boost.");
			// Holding steady decays velocity back to raw position.
			for (var i = 0; i < 40; i++)
			{
				t += stepTicks;
				boosted = WheelAxisHelper.ProcessSteeringAxis(rawTurn, 1.0, 40, t, freq, ref state);
			}
			Assert.IsTrue(Math.Abs(boosted - rawTurn) < 15, "Lead boost must decay cleanly to raw position once steady; diff=" + Math.Abs(boosted - rawTurn));
		}

		[TestMethod, TestCategory("performance")]
		[Description("WheelTelemetryMath and WheelAxisHelper allocate zero bytes on the 1000 Hz hot path")]
		public void Wheel_telemetry_and_axis_hot_paths_allocate_nothing()
		{
			var curve = new SpringCurve();
			curve.Set(60, 12, 60, 40, 900, 100, 20);
			var sample = new WheelTelemetrySample { Active = true, SpeedKmh = 120f, AlignAngleDeg = 10f, FrontGrip = 0.8f, Impulse = 0.1f, DampingScale = 1.1f, TimestampMs = 1000 };
			var lead = new SteeringLeadState();
			var ff = new ForceFeedbackState();
			var freq = Stopwatch.Frequency;
			const int calls = 20000;
			var allocated = Allocations.FewestBytes(5, () =>
			{
				double damp;
				for (var i = 0; i < calls; i++)
				{
					WheelTelemetryMath.ComputeSpringForce(curve, SpringCalibration.Center + (i & 1023), 900, 20, 60, sample, 1010, 0.1, 270, out damp);
					WheelAxisHelper.ProcessSteeringAxis((ushort)(32768 + (i & 1023)), 900.0 / 270.0, 20, freq + i * 1000L, freq, ref lead);
					var large = (byte)(i & 255);
					var small = (byte)(i >> 3 & 255);
					ff.ShapeGameRumble(sample, 1010 + (i & 255), false, 30, ref large, ref small);
				}
			});
			Assert.IsTrue(allocated < calls, "Hot path allocated " + allocated + " bytes over " + calls + " calls.");
		}

		[TestMethod, TestCategory("ui"), TestCategory("plugins"), TestCategory("critical")]
		[Description("Wheel Telemetry plugin loads ONLY its dedicated Wheel Telemetry tab (no duplicate group on Wheel), shows Wheel/Telemetry tabs only for wheel devices, exposes explicit Forward IP and Forward Port fields, keeps controls clear, and unloads when disabled")]
		public void Wheel_telemetry_ui_loads_into_dedicated_tab_only_for_wheels_and_unloads_when_disabled()
		{
			var before = PluginManager.Current;
			try
			{
				using (var manager = new PluginManager(null, null, () => false, typeof(PluginManager).Assembly))
				{
					manager.LoadEmbedded();
					PluginManager.Current = manager;
					var telemetryType = manager.TelemetryTypes.Single(t => t.File.Name == "WheelTelemetry");
					Assert.IsNotNull(telemetryType.ControlType, "WheelTelemetry plugin must supply its dedicated controller tab control.");

					Ui.OnUiThread(() =>
					{
						using (var form = new System.Windows.Forms.Form())
						using (var pad = new x360ce.App.Controls.PadControl(MapTo.Controller1))
						{
							pad.Dock = System.Windows.Forms.DockStyle.Fill;
							form.ClientSize = pad.Size;
							form.Controls.Add(pad);
							// No device loaded (ud == null): Wheel and Game Telemetry tabs must BOTH be hidden.
							Assert.IsFalse(pad.PadTabControl.TabPages.Contains(pad.WheelTabPage), "Wheel tab must stay hidden when no device is loaded.");
							Assert.IsNotNull(pad.WheelTelemetryTabPage, "Dedicated Game Telemetry tab page should be prepared when the plugin is enabled.");
							Assert.IsFalse(pad.PadTabControl.TabPages.Contains(pad.WheelTelemetryTabPage), "Game Telemetry tab must stay hidden when no device is loaded.");

							// Gamepad selected: Wheel and Game Telemetry tabs must BOTH be hidden.
							var gamepad = new x360ce.Engine.Data.UserDevice { ProductName = "Xbox Controller", CapType = (int)SharpDX.DirectInput.DeviceType.Gamepad };
							var wheel = new x360ce.Engine.Data.UserDevice { ProductName = "Logitech G29 Driving Force", CapType = (int)SharpDX.DirectInput.DeviceType.Driving };
							Assert.IsFalse(x360ce.App.Controls.PadControl.IsWheelDevice(null));
							Assert.IsFalse(x360ce.App.Controls.PadControl.IsWheelDevice(gamepad));
							Assert.IsTrue(x360ce.App.Controls.PadControl.IsWheelDevice(wheel));

							pad.UpdateWheelTabs(gamepad);
							Assert.IsFalse(pad.PadTabControl.TabPages.Contains(pad.WheelTabPage), "Wheel tab must hide for a standard gamepad.");
							Assert.IsFalse(pad.PadTabControl.TabPages.Contains(pad.WheelTelemetryTabPage), "Game Telemetry tab must hide for a standard gamepad.");

							// Driving wheel selected: Wheel and Game Telemetry tabs BOTH appear.
							pad.UpdateWheelTabs(wheel);
							var page = pad.WheelTelemetryTabPage;
							Assert.IsTrue(pad.PadTabControl.TabPages.Contains(pad.WheelTabPage), "Wheel tab must show for a driving wheel.");
							Assert.IsTrue(pad.PadTabControl.TabPages.Contains(page), "Game Telemetry tab must show for a driving wheel.");
							Assert.AreEqual("Game Telemetry", page.Text);
							Assert.AreEqual(pad.PadTabControl.TabPages.IndexOf(pad.WheelTabPage) + 1, pad.PadTabControl.TabPages.IndexOf(page),
								"Game Telemetry tab should sit right after the Wheel tab.");

							pad.PadTabControl.SelectedTab = pad.WheelTabPage;
							form.Show();
							System.Windows.Forms.Application.DoEvents();

							// Single source of truth: no duplicated telemetry group on WheelTabPage; RGB LED strip row is present on WheelTabPage.
							Assert.AreEqual(0, pad.WheelTabPage.Controls.Find("WheelTelemetryGroupBox", true).Length,
								"Wheel tab must not duplicate Game Telemetry plugin controls.");
							Assert.AreEqual(0, pad.WheelTabPage.Controls.Find("WheelTelemetryCheckBox", true).Length,
								"Wheel tab must not host a second telemetry checkbox.");
							Assert.AreEqual(1, pad.WheelTabPage.Controls.Find("WheelRgbStripCheckBox", true).Length,
								"Wheel tab must include the RGB strip toggle.");
							Assert.AreEqual(1, pad.WheelTabPage.Controls.Find("WheelRgbStripBar", true).Length,
								"Wheel tab must include the visual RGB LED strip preview.");
							Assert.AreEqual(1, pad.WheelTabPage.Controls.Find("WheelRgbTestButton", true).Length,
								"Wheel tab must include the RGB LED strip Test button.");

							Assert.AreEqual(1, page.Controls.Find("StandardPortsCheckBox", true).Length);
							Assert.AreEqual(1, page.Controls.Find("ListenPortUpDown", true).Length);
							var fwdCheck = page.Controls.Find("ForwardEnabledCheckBox", true).OfType<System.Windows.Forms.CheckBox>().Single();
							var fwdIpBox = page.Controls.Find("ForwardIpTextBox", true).OfType<System.Windows.Forms.TextBox>().Single();
							var fwdPortBox = page.Controls.Find("ForwardPortUpDown", true).OfType<System.Windows.Forms.NumericUpDown>().Single();
							Assert.IsFalse(fwdCheck.Checked, "Forward telemetry should be off by default.");
							Assert.IsFalse(fwdIpBox.Enabled, "Forward IP should be disabled while forward telemetry is unchecked.");
							Assert.IsFalse(fwdPortBox.Enabled, "Forward Port should be disabled while forward telemetry is unchecked.");
							fwdCheck.Checked = true;
							Assert.IsTrue(x360ce.App.SettingsManager.Options.WheelTelemetryForwardEnabled, "Ticking Enable forward telemetry must update Options.");
							Assert.IsTrue(fwdIpBox.Enabled, "Forward IP should enable when forward telemetry is checked.");
							Assert.IsTrue(fwdPortBox.Enabled, "Forward Port should enable when forward telemetry is checked.");
							fwdCheck.Checked = false;
							Assert.IsFalse(x360ce.App.SettingsManager.Options.WheelTelemetryForwardEnabled, "Unticking Enable forward telemetry must update Options.");
							Assert.AreEqual(1, page.Controls.Find("AlignStrengthTrackBar", true).Length);
							Assert.AreEqual(1, page.Controls.Find("UndersteerStrengthTrackBar", true).Length);
							Assert.AreEqual(1, page.Controls.Find("CurbStrengthTrackBar", true).Length);
							Assert.AreEqual(1, page.Controls.Find("DampingStrengthTrackBar", true).Length);
							Assert.AreEqual(1, page.Controls.Find("RumbleUndersteerTrackBar", true).Length);
							Assert.AreEqual(1, page.Controls.Find("SteeringLeadTrackBar", true).Length);
							Assert.AreEqual(1, page.Controls.Find("ReadingsPanel", true).Length);
							Assert.AreEqual(1, page.Controls.Find("LogTextBox", true).Length);

							// Verify zero control overlap on both WheelTabPage and WheelTelemetryTabPage at every tested width.
							var drawn = form.ClientSize.Width;
							foreach (var tab in new[] { pad.WheelTabPage, page })
							{
								pad.PadTabControl.SelectedTab = tab;
								foreach (var width in new[] { drawn, (int)(drawn * 0.75), (int)(drawn * 1.5), drawn * 2 })
								{
									form.ClientSize = new System.Drawing.Size(width, form.ClientSize.Height);
									System.Windows.Forms.Application.DoEvents();
									var covered = ControlOverlapTest.Covered(tab).ToList();
									Assert.AreEqual(0, covered.Count, tab.Text + " overlap at width " + width + ": " + string.Join("; ", covered));
									if (tab == pad.WheelTabPage)
									{
										var outside = pad.WheelSettingsTableLayoutPanel.Controls.Cast<System.Windows.Forms.Control>()
											.Where(x => x.Visible && x.Right > x.Parent.ClientSize.Width).Select(x => x.Name).ToList();
										Assert.AreEqual(0, outside.Count, "Wheel overflow at width " + width + ": " + string.Join(", ", outside));
									}
								}
							}

							// Disabling the plugin must immediately unload and remove the dedicated tab.
							manager.SetEnabled("WheelTelemetry", false);
							System.Windows.Forms.Application.DoEvents();
							Assert.IsNull(pad.WheelTelemetryTabPage, "Disabling WheelTelemetry must remove the Game Telemetry tab.");

							// Re-enabling must restore the dedicated tab immediately.
							manager.SetEnabled("WheelTelemetry", true);
							System.Windows.Forms.Application.DoEvents();
							Assert.IsNotNull(pad.WheelTelemetryTabPage, "Re-enabling WheelTelemetry must restore the Game Telemetry tab.");
							pad.UpdateWheelTabs(wheel);
							Assert.IsTrue(pad.PadTabControl.TabPages.Contains(pad.WheelTelemetryTabPage));

							// Clearing selection (ud == null) hides both tabs again.
							pad.UpdateWheelTabs(null);
							Assert.IsFalse(pad.PadTabControl.TabPages.Contains(pad.WheelTabPage));
							Assert.IsFalse(pad.PadTabControl.TabPages.Contains(pad.WheelTelemetryTabPage));
						}
					});
				}
			}
			finally
			{
				PluginManager.Current = before;
			}
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("WheelTelemetryTuning scales aligning torque, understeer lightening, curb impulses and dynamic damping cleanly")]
		public void Wheel_telemetry_tuning_scales_each_physics_channel()
		{
			var curve = new SpringCurve();
			curve.Set(60, 12, 60, 40, 900, 100, 20);
			var center = SpringCalibration.Center;
			var sample = new WheelTelemetrySample
			{
				Active = true,
				SpeedKmh = 100f,
				AlignAngleDeg = 25f,
				FrontGrip = 0.25f,
				Impulse = 0.8f,
				DampingScale = 1.6f,
				TimestampMs = 1000,
			};

			double dampDefault, dampZero, dampBoost;
			var fDefault = WheelTelemetryMath.ComputeSpringForce(curve, center, 900, 20, 60, sample, 1010, 0, 0, WheelTelemetryTuning.Default, out dampDefault);
			var fZero = WheelTelemetryMath.ComputeSpringForce(curve, center, 900, 20, 60, sample, 1010, 0, 0,
				new WheelTelemetryTuning { AlignStrengthPercent = 0, UndersteerStrengthPercent = 0, CurbStrengthPercent = 0, DampingStrengthPercent = 0 }, out dampZero);
			var fBoost = WheelTelemetryMath.ComputeSpringForce(curve, center, 900, 20, 60, sample, 1010, 0, 0,
				new WheelTelemetryTuning { AlignStrengthPercent = 180, UndersteerStrengthPercent = 100, CurbStrengthPercent = 100, DampingStrengthPercent = 200 }, out dampBoost);

			Assert.AreEqual(0, fZero, "Zeroed aligning and curb scales at wheel centre must yield 0 force.");
			Assert.AreEqual(1.0, dampZero, 0.001, "Zeroed damping scale must keep neutral 1.0x damping.");
			Assert.IsTrue(fDefault > 20, "Default tuning should produce strong counter-steer + curb pull.");
			Assert.IsTrue(dampBoost > dampDefault && dampDefault > 1.0, "Higher damping scale must increase telemetry damping.");
			Assert.IsTrue(fBoost >= fDefault, "Higher aligning scale must strengthen counter-steer pull.");
		}

		[TestMethod, TestCategory("test-game"), TestCategory("plugins"), TestCategory("critical")]
		[Description("x360ce Test Game Virtual Car accelerates, turns, hits curbs and emits valid packets decoded by WheelTelemetry for every standard protocol")]
		public void Virtual_car_drives_and_streams_every_standard_protocol_to_wheel_telemetry()
		{
			var car = new x360ce.TestGame.VirtualCarSimulation();
			car.ThrottleInput = 1f;
			car.SteeringInput = -0.35f;
			for (var i = 0; i < 180; i++)
				car.Step(1.0 / 60.0);
			car.TriggerCurb(1f);
			car.Step(1.0 / 60.0);

			Assert.IsTrue(car.SpeedKmh > 60f, "Virtual car did not accelerate: " + car.SpeedKmh);
			Assert.IsTrue(Math.Abs(car.LateralG) > 0.2f, "Virtual car generated no lateral G while turning.");
			Assert.IsTrue(car.CurbImpulse > 0.3f, "Manual right curb strike did not produce positive impulse.");

			var expectedSources = new[]
			{
				new { Protocol = x360ce.TestGame.VirtualCarProtocol.ForzaDataOut, Source = "Forza Data Out" },
				new { Protocol = x360ce.TestGame.VirtualCarProtocol.EaF1Udp, Source = "EA F1" },
				new { Protocol = x360ce.TestGame.VirtualCarProtocol.CodemastersUdp, Source = "Codemasters UDP" },
				new { Protocol = x360ce.TestGame.VirtualCarProtocol.OutSimUdp, Source = "OutSim" },
				new { Protocol = x360ce.TestGame.VirtualCarProtocol.ProjectCars2Udp, Source = "Project CARS 2 UDP" },
			};
			foreach (var item in expectedSources)
			{
				using (var provider = (IDisposable)NewOfflineProvider())
				{
					var telemetry = (IPluginWheelTelemetry)provider;
					for (uint step = 1; step <= 4; step++)
					{
						car.Step(1.0 / 60.0);
						var packets = car.BuildPackets(item.Protocol, step * 16);
						foreach (var pkt in packets)
							TakeUdp(telemetry, 37150, pkt, 1000 + step * 16);
					}
					var decoded = WheelTelemetry.Neutral;
					Assert.IsTrue(telemetry.TryReadTelemetry(ref decoded), "Failed to decode Virtual Car packets for " + item.Protocol);
					Assert.AreEqual(item.Source, decoded.Source, "Wrong auto-detected protocol for " + item.Protocol);
					Assert.IsTrue(Math.Abs(decoded.SpeedKmh - car.SpeedKmh) < 5f,
						string.Format("{0}: expected speed ~{1:0.0} km/h, got {2:0.0}", item.Protocol, car.SpeedKmh, decoded.SpeedKmh));
				}
			}
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("UDP forwarding supports multiple local and LAN IP:port targets simultaneously while blocking loopback self-loops")]
		public void Forward_targets_support_multiple_local_and_lan_endpoints_without_self_loops()
		{
			var type = TelemetryType().Type;
			var parse = type.GetMethod("ParseForwardTargets", BindingFlags.Public | BindingFlags.Static);
			Assert.IsNotNull(parse);
			var endpoints = (IPEndPoint[])parse.Invoke(null, new object[]
			{
				"20778, 192.168.1.50:20777, 127.0.0.1:37150, localhost:5301, 20778",
				0,
				new[] { 37150 },
			});
			Assert.AreEqual(3, endpoints.Length, "Expected 3 unique endpoints with self-loop 127.0.0.1:37150 excluded.");
			CollectionAssert.AreEquivalent(
				new[] { "127.0.0.1:20778", "192.168.1.50:20777", "127.0.0.1:5301" },
				endpoints.Select(ep => ep.ToString()).ToArray());

			// Live fan-out test: relay one Forza datagram to two local receivers simultaneously.
			int listenPort, sinkPortA, sinkPortB;
			using (var p1 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
			using (var p2 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
			using (var p3 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
			{
				p1.Bind(new IPEndPoint(IPAddress.Loopback, 0));
				p2.Bind(new IPEndPoint(IPAddress.Loopback, 0));
				p3.Bind(new IPEndPoint(IPAddress.Loopback, 0));
				listenPort = ((IPEndPoint)p1.LocalEndPoint).Port;
				sinkPortA = ((IPEndPoint)p2.LocalEndPoint).Port;
				sinkPortB = ((IPEndPoint)p3.LocalEndPoint).Port;
			}
			using (var provider = (IDisposable)TelemetryType().NewInstance())
			using (var sinkA = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
			using (var sinkB = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
			using (var sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
			{
				sinkA.ReceiveTimeout = 1000;
				sinkB.ReceiveTimeout = 1000;
				sinkA.Bind(new IPEndPoint(IPAddress.Loopback, sinkPortA));
				sinkB.Bind(new IPEndPoint(IPAddress.Loopback, sinkPortB));
				var telemetry = (IPluginWheelTelemetry)provider;
				var settings = (IPluginWheelTelemetrySettings)provider;
				settings.Configure(listenPort, false, 0);
				settings.ConfigureForwardTargets(string.Format("127.0.0.1:{0}, 127.0.0.1:{1}", sinkPortA, sinkPortB));
				var dummy = WheelTelemetry.Neutral;
				telemetry.TryReadTelemetry(ref dummy);
				Assert.IsTrue(WaitUntil(() => BoundPorts(telemetry).Contains(listenPort), 2000));
				sender.SendTo(ForzaPacket(323, 1, 100, 30f), new IPEndPoint(IPAddress.Loopback, listenPort));
				var bufA = new byte[512];
				var bufB = new byte[512];
				Assert.AreEqual(323, sinkA.Receive(bufA), "First forward target did not receive relayed datagram.");
				Assert.AreEqual(323, sinkB.Receive(bufB), "Second forward target did not receive relayed datagram.");
			}
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("PitchDeg decodes nose-up-positive degrees across Forza Data Out (including zero-RPM on-foot), EA F1, Assetto Corsa, OutSim, Project CARS v1/v2 and iRacing, with NaN as Neutral")]
		public void Pitch_deg_decodes_in_every_pitch_capable_decoder()
		{
			Assert.IsTrue(float.IsNaN(WheelTelemetry.Neutral.PitchDeg), "WheelTelemetry.Neutral.PitchDeg must be NaN.");
			Assert.IsTrue(float.IsNaN(WheelTelemetrySample.Inactive.PitchDeg), "WheelTelemetrySample.Inactive.PitchDeg must be NaN.");

			const float upRad = (float)(6.0 * Math.PI / 180.0);    // +6 deg uphill (nose up) -> ~10.51 % grade
			const float downRad = (float)(-4.5 * Math.PI / 180.0); // -4.5 deg downhill (nose down)

			// 1. Forza Data Out (311-byte FM7 Dash): driving + downhill + zero-RPM on-foot packet (GTA V compatible).
			using (var provider = (IDisposable)NewOfflineProvider())
			{
				var telemetry = (IPluginWheelTelemetry)provider;
				var sample = WheelTelemetry.Neutral;
				for (uint i = 1; i <= 3; i++)
				{
					var pkt = ForzaPacket(311, 1, i * 16, 20f);
					WriteSingle(pkt, 60, upRad);
					TakeUdp(telemetry, 37150, pkt, 1000 + i * 16);
				}
				Assert.IsTrue(telemetry.TryReadTelemetry(ref sample));
				Assert.AreEqual("Forza Data Out", sample.Source);
				Assert.AreEqual(6.0f, sample.PitchDeg, 0.05f, "Forza uphill pitch");
				Assert.AreEqual(Math.Tan(upRad) * 100.0, Math.Tan(sample.PitchDeg * Math.PI / 180.0) * 100.0, 0.1);

				// Zero-RPM on-foot FM7 Dash packet (speed + ground slope pitch, RPM fields all 0).
				var onFoot = new byte[311];
				WriteInt32(onFoot, 0, 1);
				WriteUInt32(onFoot, 4, 80);
				WriteSingle(onFoot, 40, 3.5f); // walking/running speed
				WriteSingle(onFoot, 60, downRad);
				Assert.IsTrue(TakeUdp(telemetry, 37150, onFoot, 1080));
				Assert.IsTrue(telemetry.TryReadTelemetry(ref sample));
				Assert.AreEqual(-4.5f, sample.PitchDeg, 0.05f, "Forza zero-RPM on-foot downhill pitch");
				Assert.IsTrue(float.IsNaN(sample.RpmFraction), "Zero-RPM packet should leave RpmFraction NaN.");
			}

			// 2. EA F1 Motion packet (Packet 0, m_pitch @ +52).
			using (var provider = (IDisposable)NewOfflineProvider())
			{
				var telemetry = (IPluginWheelTelemetry)provider;
				var motion = new byte[29 + 22 * 60];
				WriteUInt16(motion, 0, 2024);
				motion[6] = 0;  // Packet 0 = Motion
				motion[27] = 1; // PlayerCarIndex = 1
				WriteSingle(motion, 29 + 1 * 60 + 52, upRad);
				Assert.IsTrue(TakeUdp(telemetry, 37150, motion, 2000));

				var carTel = new byte[29 + 22 * 60];
				WriteUInt16(carTel, 0, 2024);
				carTel[6] = 6;  // Packet 6 = CarTelemetry
				carTel[27] = 1;
				WriteUInt16(carTel, 29 + 1 * 60, 180);
				Assert.IsTrue(TakeUdp(telemetry, 37150, carTel, 2016));

				var sample = WheelTelemetry.Neutral;
				Assert.IsTrue(telemetry.TryReadTelemetry(ref sample));
				Assert.AreEqual("EA F1", sample.Source);
				Assert.AreEqual(6.0f, sample.PitchDeg, 0.05f, "EA F1 Motion m_pitch");

				WriteSingle(motion, 29 + 1 * 60 + 52, downRad);
				Assert.IsTrue(TakeUdp(telemetry, 37150, motion, 2032));
				Assert.IsTrue(telemetry.TryReadTelemetry(ref sample));
				Assert.AreEqual(-4.5f, sample.PitchDeg, 0.05f, "EA F1 negative pitch");
			}

			// 3. Assetto Corsa acpmf_physics (pitch @ 212).
			using (var provider = (IDisposable)NewOfflineProvider())
			{
				var telemetry = (IPluginWheelTelemetry)provider;
				var ac = new byte[256];
				WriteInt32(ac, 0, 10);
				WriteSingle(ac, 28, 95f);
				WriteSingle(ac, 212, upRad);
				Assert.IsTrue(TakeSharedMemory(telemetry, @"Local\acpmf_physics", ac, 3000));
				var sample = WheelTelemetry.Neutral;
				Assert.IsTrue(telemetry.TryReadTelemetry(ref sample));
				Assert.AreEqual("Assetto Corsa", sample.Source);
				Assert.AreEqual(6.0f, sample.PitchDeg, 0.05f, "Assetto Corsa pitch");

				WriteInt32(ac, 0, 11);
				WriteSingle(ac, 212, downRad);
				Assert.IsTrue(TakeSharedMemory(telemetry, @"Local\acpmf_physics", ac, 3020));
				Assert.IsTrue(telemetry.TryReadTelemetry(ref sample));
				Assert.AreEqual(-4.5f, sample.PitchDeg, 0.05f, "Assetto Corsa negative pitch");
			}

			// 4. OutSim (64 bytes, Pitch @ 20).
			using (var provider = (IDisposable)NewOfflineProvider())
			{
				var telemetry = (IPluginWheelTelemetry)provider;
				for (uint i = 1; i <= 3; i++)
				{
					var os = new byte[64];
					WriteUInt32(os, 0, i * 16);
					WriteSingle(os, 20, upRad);
					WriteSingle(os, 40, 22f);
					TakeUdp(telemetry, 37150, os, 4000 + i * 16);
				}
				var sample = WheelTelemetry.Neutral;
				Assert.IsTrue(telemetry.TryReadTelemetry(ref sample));
				Assert.AreEqual("OutSim", sample.Source);
				Assert.AreEqual(6.0f, sample.PitchDeg, 0.05f, "OutSim pitch");
			}

			// 5a. Project CARS v1 (1367 bytes, sOrientation[0] @ 136).
			using (var provider = (IDisposable)NewOfflineProvider())
			{
				var telemetry = (IPluginWheelTelemetry)provider;
				var pc1 = new byte[1367];
				pc1[2] = 0; // PacketType = Telemetry
				WriteSingle(pc1, 120, 30f);
				WriteSingle(pc1, 136, upRad);
				Assert.IsTrue(TakeUdp(telemetry, 5606, pc1, 5000));
				var sample = WheelTelemetry.Neutral;
				Assert.IsTrue(telemetry.TryReadTelemetry(ref sample));
				Assert.AreEqual("Project CARS UDP", sample.Source);
				Assert.AreEqual(6.0f, sample.PitchDeg, 0.05f, "Project CARS v1 pitch");
			}

			// 5b. Project CARS v2 (559 bytes, sOrientation[0] @ 52).
			using (var provider = (IDisposable)NewOfflineProvider())
			{
				var telemetry = (IPluginWheelTelemetry)provider;
				var pc2 = new byte[559];
				pc2[10] = 0; // PacketType = CarPhysics
				WriteSingle(pc2, 36, 35f);
				WriteSingle(pc2, 52, downRad);
				Assert.IsTrue(TakeUdp(telemetry, 5606, pc2, 6000));
				var sample = WheelTelemetry.Neutral;
				Assert.IsTrue(telemetry.TryReadTelemetry(ref sample));
				Assert.AreEqual("Project CARS 2 UDP", sample.Source);
				Assert.AreEqual(-4.5f, sample.PitchDeg, 0.05f, "Project CARS v2 pitch");
			}

			// 6. iRacing shared memory (self-describing "Pitch" variable).
			using (var provider = (IDisposable)NewOfflineProvider())
			{
				var telemetry = (IPluginWheelTelemetry)provider;
				var ir = new byte[1024];
				WriteInt32(ir, 0, 2);
				WriteInt32(ir, 4, 1);
				WriteInt32(ir, 24, 2);
				WriteInt32(ir, 28, 128);
				WriteInt32(ir, 32, 1);
				WriteInt32(ir, 36, 64);
				WriteInt32(ir, 48, 99);
				WriteInt32(ir, 52, 512);
				WriteInt32(ir, 128 + 0, 4);
				WriteInt32(ir, 128 + 4, 0);
				Encoding.ASCII.GetBytes("Speed").CopyTo(ir, 128 + 16);
				WriteInt32(ir, 128 + 144 + 0, 4);
				WriteInt32(ir, 128 + 144 + 4, 4);
				Encoding.ASCII.GetBytes("Pitch").CopyTo(ir, 128 + 144 + 16);
				WriteSingle(ir, 512 + 0, 40f);
				WriteSingle(ir, 512 + 4, upRad);
				Assert.IsTrue(TakeSharedMemory(telemetry, @"Local\IRSDKMemMapFileName", ir, 7000));
				var sample = WheelTelemetry.Neutral;
				Assert.IsTrue(telemetry.TryReadTelemetry(ref sample));
				Assert.AreEqual("iRacing", sample.Source);
				Assert.AreEqual(6.0f, sample.PitchDeg, 0.05f, "iRacing pitch");
			}
		}

		sealed class TelemetryConsumerStub : IPluginDevice, IPluginTelemetryConsumer
		{
			public static readonly Guid Product = new Guid("9c4d7a81-2e5b-4a9c-81f0-5d6e7f8a9b0c");
			public static TelemetryConsumerStub LastCreated;
			public int TakeCount;
			public int ReadCount;
			public bool SawTelemetryBeforeRead;
			public WheelTelemetry LastTelemetry = WheelTelemetry.Neutral;

			public TelemetryConsumerStub() { LastCreated = this; }
			public string Name { get { return "Telemetry Consumer Stub"; } }
			public Guid ProductGuid { get { return Product; } }
			public int AxisCount { get { return 1; } }
			public int SliderCount { get { return 0; } }
			public int PovCount { get { return 0; } }
			public int ButtonCount { get { return 1; } }

			public void TakeTelemetry(ref WheelTelemetry telemetry)
			{
				TakeCount++;
				LastTelemetry = telemetry;
			}

			public void Read(PluginState state)
			{
				ReadCount++;
				SawTelemetryBeforeRead = TakeCount >= ReadCount;
				state.Axes[0] = PluginState.Middle;
			}
		}

		sealed class TelemetryProviderStub : IPluginWheelTelemetry, IDisposable
		{
			public static int ActiveInstances;
			public static bool EmitFresh;
			public static WheelTelemetry NextSample = WheelTelemetry.Neutral;
			public static int ReadCalls;

			public TelemetryProviderStub() { Interlocked.Increment(ref ActiveInstances); }
			public string Name { get { return "Telemetry Provider Stub"; } }

			public bool TryReadTelemetry(ref WheelTelemetry telemetry)
			{
				ReadCalls++;
				if (!EmitFresh)
					return false;
				telemetry = NextSample;
				return true;
			}

			public void Dispose() { Interlocked.Decrement(ref ActiveInstances); }
		}

		[TestMethod, TestCategory("plugins"), TestCategory("performance"), TestCategory("critical")]
		[Description("Host runs telemetry providers for ticked IPluginTelemetryConsumer devices without a wheel, delivers fresh samples or Neutral, releases providers when unticked, and allocates zero bytes on the 1000 Hz path")]
		public void Telemetry_consumer_plugin_keeps_provider_alive_without_wheel_and_allocates_nothing()
		{
			var prevManager = PluginManager.Current;
			var prevEnabled = x360ce.App.SettingsManager.Options.WheelTelemetryEnabled;
			TelemetryConsumerStub.LastCreated = null;
			TelemetryProviderStub.ActiveInstances = 0;
			TelemetryProviderStub.ReadCalls = 0;
			TelemetryProviderStub.EmitFresh = true;

			try
			{
				using (var manager = new PluginManager(null, null, () => false, null))
				using (var helper = new x360ce.App.DInput.DInputHelper())
				{
					var file = new PluginFile("ConsumerHostTest", false) { State = PluginFileState.Loaded, Enabled = true };
					var deviceType = (PluginDeviceType)Activator.CreateInstance(
						typeof(PluginDeviceType), Any, null, new object[] { typeof(TelemetryConsumerStub), file }, null);
					var telemetryType = (PluginTelemetryType)Activator.CreateInstance(
						typeof(PluginTelemetryType), Any, null, new object[] { typeof(TelemetryProviderStub), file }, null);
					typeof(PluginManager).GetField("_deviceTypes", Any).SetValue(manager, new[] { deviceType });
					typeof(PluginManager).GetField("_telemetryTypes", Any).SetValue(manager, new[] { telemetryType });
					PluginManager.Current = manager;

					var ud = PluginDevices.NewUserDevice(deviceType);
					ud.IsOnline = true;
					var game = new x360ce.Engine.Data.UserGame { FileName = "test.exe" };
					var setting = new x360ce.Engine.Data.UserSetting
					{
						FileName = "test.exe",
						InstanceGuid = ud.InstanceGuid,
						ProductGuid = ud.ProductGuid,
						MapTo = (int)MapTo.Controller1,
						IsEnabled = true,
					};
					var withConsumer = x360ce.App.DInput.DeviceRouting.Build(game, new[] { setting }, new x360ce.Engine.Data.PadSetting[0], new[] { ud });
					var withoutConsumer = x360ce.App.DInput.DeviceRouting.Build(game, new x360ce.Engine.Data.UserSetting[0], new x360ce.Engine.Data.PadSetting[0], new[] { ud });
					var hasActiveProp = typeof(x360ce.App.DInput.DInputHelper).GetProperty("HasActiveWheelTelemetry", Any);

					// 1. Fresh telemetry delivered to ticked consumer with NO steering wheel present.
					x360ce.App.SettingsManager.Options.WheelTelemetryEnabled = true;
					var live = WheelTelemetry.Neutral;
					live.SpeedKmh = 42.5f;
					live.PitchDeg = 7.25f;
					live.TimestampMs = WheelTelemetryMath.MonotonicNowMs();
					live.Source = "Forza Data Out";
					TelemetryProviderStub.NextSample = live;

					helper.UpdateDiStates(null, game, null, withConsumer);
					Assert.AreEqual(1, TelemetryProviderStub.ActiveInstances, "Provider must open lazily when a ticked consumer is active.");
					Assert.IsTrue((bool)hasActiveProp.GetValue(helper));
					Assert.IsNotNull(TelemetryConsumerStub.LastCreated);
					Assert.IsTrue(TelemetryConsumerStub.LastCreated.SawTelemetryBeforeRead);
					Assert.AreEqual(42.5f, TelemetryConsumerStub.LastCreated.LastTelemetry.SpeedKmh, 0.01f);
					Assert.AreEqual(7.25f, TelemetryConsumerStub.LastCreated.LastTelemetry.PitchDeg, 0.01f);

					// 2. Zero allocations on the 1000 Hz engine path while consumer receives live telemetry.
					helper.UpdateDiStates(null, game, null, withConsumer); // prime second SourceState swap
					const int calls = 5000;
					var allocated = Allocations.FewestBytes(5, () =>
					{
						for (var i = 0; i < calls; i++)
							helper.UpdateDiStates(null, game, null, withConsumer);
					});
					Assert.IsTrue(allocated < calls, "Consumer engine path allocated " + allocated + " bytes over " + calls + " passes.");

					// 3. No fresh sample -> consumer receives WheelTelemetry.Neutral.
					TelemetryProviderStub.EmitFresh = false;
					helper.UpdateDiStates(null, game, null, withConsumer);
					Assert.IsTrue(float.IsNaN(TelemetryConsumerStub.LastCreated.LastTelemetry.SpeedKmh), "Idle telemetry must deliver Neutral speed (NaN).");
					Assert.IsTrue(float.IsNaN(TelemetryConsumerStub.LastCreated.LastTelemetry.PitchDeg), "Idle telemetry must deliver Neutral pitch (NaN).");

					// Stale timestamp -> consumer also receives WheelTelemetry.Neutral.
					TelemetryProviderStub.EmitFresh = true;
					live.TimestampMs = Math.Max(1, WheelTelemetryMath.MonotonicNowMs() - WheelTelemetryMath.StaleMs - WheelTelemetryMath.DecayMs - 500);
					TelemetryProviderStub.NextSample = live;
					helper.UpdateDiStates(null, game, null, withConsumer);
					Assert.IsTrue(float.IsNaN(TelemetryConsumerStub.LastCreated.LastTelemetry.SpeedKmh), "Stale telemetry must deliver Neutral speed (NaN).");
					Assert.IsTrue(float.IsNaN(TelemetryConsumerStub.LastCreated.LastTelemetry.PitchDeg), "Stale telemetry must deliver Neutral pitch (NaN).");

					// 4. Unticking the consumer device releases the provider so sockets close immediately.
					helper.UpdateDiStates(null, game, null, withoutConsumer);
					Assert.AreEqual(0, TelemetryProviderStub.ActiveInstances, "Provider must be disposed once no wheel or consumer is active.");
					Assert.IsFalse((bool)hasActiveProp.GetValue(helper));

					// 5. Telemetry disabled -> consumer receives Neutral and provider never opens.
					x360ce.App.SettingsManager.Options.WheelTelemetryEnabled = false;
					helper.UpdateDiStates(null, game, null, withConsumer);
					Assert.AreEqual(0, TelemetryProviderStub.ActiveInstances, "Provider must stay closed while telemetry is disabled.");
					Assert.IsTrue(float.IsNaN(TelemetryConsumerStub.LastCreated.LastTelemetry.PitchDeg));
				}
			}
			finally
			{
				PluginManager.Current = prevManager;
				x360ce.App.SettingsManager.Options.WheelTelemetryEnabled = prevEnabled;
			}
		}

		static bool WaitUntil(Func<bool> condition, int timeoutMs)
		{
			var sw = Stopwatch.StartNew();
			while (sw.ElapsedMilliseconds < timeoutMs)
			{
				if (condition())
					return true;
				Thread.Sleep(25);
			}
			return condition();
		}
	}
}
