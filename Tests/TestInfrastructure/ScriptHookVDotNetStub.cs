namespace x360ce.Tests
{
	/// <summary>
	/// The part of ScriptHookVDotNet's version 3 scripting API (ScriptHookVDotNet3.dll) that GTA V's game script, which the
	/// Wheel Telemetry Games plugin installs, may use, as C# source, so a test compiles the script with no game: the members ScriptHookVDotNet 3
	/// (GTA V Legacy) and ScriptHookVDotNet Enhanced (GTA V Legacy and Enhanced) both have, with the same signatures.
	/// </summary>
	/// <remarks>
	/// Each member is declared as the three sources it was read from declare it, the line given for each:
	/// <list type="bullet">
	/// <item>v3.6.0: scripthookvdotnet/scripthookvdotnet, tag v3.6.0 (b5a25a8), the latest release.</item>
	/// <item>nightly: scripthookvdotnet/scripthookvdotnet, main (28b4dfd, 2026-11-15), the nightly builds Legacy players use for newer game versions.</item>
	/// <item>Enhanced: Chiheb-Bacha/ScriptHookVDotNetEnhanced, tag v1.1.0.6 (d756d47).</item>
	/// </list>
	/// A member the three do not all have is left out, so a script that uses one does not compile: World.GetGroundHeight
	/// (float GetGroundHeight(Vector3) in v3.6.0 and Enhanced, bool GetGroundHeight(Vector3, out float, ...) in nightly),
	/// Game.IsLoading (not in nightly) and Entity.RotationVelocity (obsolete in Enhanced and nightly) among them.
	///
	/// The source is C# 5, compiled as ScriptHookVDotNet 3.6.0 and Enhanced compile a script, with CodeDom. Its bodies are a
	/// small game the test sets through <c>ShvdnStub.Harness</c>, a namespace the real API does not have and the script
	/// never names: what the player is in, where it is and moves, the ground under it, the controls, and what each native
	/// function returns.
	/// </remarks>
	public static class ScriptHookVDotNetStub
	{
		/// <summary>The name the stub assembly is given, as the real API's file is named.</summary>
		public const string AssemblyName = "ScriptHookVDotNet3";

		public const string Source = @"
using System;
using System.Collections.Generic;

namespace GTA.Math
{
	// v3.6.0 GTA.Math/Vector3.cs:33, nightly :39, Enhanced :44. Fields X, Y, Z: v3.6.0 :40-54, nightly :46-60, Enhanced :51-65.
	public struct Vector3
	{
		public float X;
		public float Y;
		public float Z;

		// v3.6.0 :65, nightly :71, Enhanced :76.
		public Vector3(float x, float y, float z)
		{
			X = x;
			Y = y;
			Z = z;
		}
	}
}

namespace GTA.Native
{
	// v3.6.0 GTA.Native/NativeHashes.cs:7, nightly :7, Enhanced :7: enum Hash : ulong. Values as all three give them.
	public enum Hash : ulong
	{
		NETWORK_IS_SESSION_STARTED = 0x9DE624D2FC4B603F,
		NETWORK_IS_IN_SESSION = 0xCA97246103B63917,
		IS_PAUSE_MENU_ACTIVE = 0xB0034A223497FFCB,
		GET_IS_LOADING_SCREEN_ACTIVE = 0x10D0A8F259E93EC9,
		IS_CUTSCENE_ACTIVE = 0x991251AFC3981F84,
		IS_PLAYER_SWITCH_IN_PROGRESS = 0xD9D2CFFF49FAB35F,
		GET_CONTROL_NORMAL = 0xEC3C9B8D5327B563,
		GET_GROUND_Z_FOR_3D_COORD = 0xC906A7DAB05C8D2B,
	}

	// v3.6.0 GTA.Native/Native.cs:117 (class), nightly :119 and Enhanced :119 (sealed class). Only the conversions all
	// three have: bool v3.6.0 :162, nightly :188, Enhanced :164; int :183, :209, :185; float :199, :225, :201;
	// OutputArgument :279, :306, :282.
	public class InputArgument
	{
		internal readonly object Value;

		InputArgument(object value)
		{
			Value = value;
		}

		public static implicit operator InputArgument(bool value) { return new InputArgument(value); }
		public static implicit operator InputArgument(int value) { return new InputArgument(value); }
		public static implicit operator InputArgument(float value) { return new InputArgument(value); }
		public static implicit operator InputArgument(OutputArgument value) { return new InputArgument(value); }
	}

	// v3.6.0 Native.cs:289, nightly :316, Enhanced :292: class OutputArgument : IDisposable. Constructor v3.6.0 :299,
	// nightly :327, Enhanced :303; Dispose :323, :362, :338; GetResult<T> :342, :437, :416.
	public class OutputArgument : IDisposable
	{
		internal object Value;

		public OutputArgument()
		{
		}

		public void Dispose()
		{
		}

		public T GetResult<T>()
		{
			return (T)Value;
		}
	}

	// v3.6.0 Native.cs:361, nightly and Enhanced: static class Function. Call<T>(Hash, params InputArgument[]): v3.6.0
	// :371, nightly :486, Enhanced :465.
	public static class Function
	{
		public static T Call<T>(Hash hash, params InputArgument[] arguments)
		{
			return (T)ShvdnStub.Harness.Native(hash, arguments);
		}
	}
}

namespace GTA
{
	// v3.6.0 GTA/Script.cs:16, nightly and Enhanced GTA/Shvdn/Script/Script.cs:16: abstract class Script.
	public abstract class Script
	{
		public Script()
		{
			Filename = ShvdnStub.Harness.ScriptFile;
		}

		// v3.6.0 :43, nightly :49, Enhanced :49.
		public event EventHandler Tick;

		// v3.6.0 :66, nightly :72, Enhanced :72.
		public event EventHandler Aborted;

		// v3.6.0 :143, nightly :149, Enhanced :149.
		public string Filename { get; private set; }

		// v3.6.0 :151, nightly :157, Enhanced :157.
		public string BaseDirectory { get { return System.IO.Path.GetDirectoryName(Filename); } }

		// v3.6.0 :210, nightly :200, Enhanced :200.
		protected int Interval { get; set; }

		internal int IntervalValue { get { return Interval; } }

		internal void RaiseTick()
		{
			var handler = Tick;
			if (handler != null)
				handler(this, EventArgs.Empty);
		}

		internal void RaiseAborted()
		{
			var handler = Aborted;
			if (handler != null)
				handler(this, EventArgs.Empty);
		}
	}

	// GTA/Game.cs: static class Game. Player: v3.6.0 :72, nightly :74, Enhanced :83.
	public static class Game
	{
		public static Player Player { get { return ShvdnStub.Harness.Player; } }
	}

	// GTA/Player.cs. Character: v3.6.0 :38, nightly :50, Enhanced :50. IsDead: v3.6.0 :218, nightly :594, Enhanced :709.
	public sealed class Player
	{
		public Ped Character { get { return ShvdnStub.Harness.Ped; } }
		public bool IsDead { get { return ShvdnStub.Harness.PlayerDead; } }
	}

	// GTA/Entities/Entity.cs: abstract class Entity.
	public abstract class Entity
	{
		internal ShvdnStub.Body Body;

		// v3.6.0 :123, nightly :172, Enhanced :168.
		public bool IsDead { get { return false; } }
		// v3.6.0 :341, nightly :563, Enhanced :580 (virtual).
		public virtual Math.Vector3 Position { get { return Body.Position; } }
		// v3.6.0 :364, nightly :587, Enhanced :604 (virtual): GET_ENTITY_ROTATION order 2, degrees.
		public virtual Math.Vector3 Rotation { get { return Body.Rotation; } }
		// v3.6.0 :416, nightly :639, Enhanced :656.
		public Math.Vector3 UpVector { get { return Body.Up; } }
		// v3.6.0 :433, nightly :653, Enhanced :673.
		public Math.Vector3 RightVector { get { return Body.Right; } }
		// v3.6.0 :450, nightly :667, Enhanced :690.
		public Math.Vector3 ForwardVector { get { return Body.Forward; } }
		// v3.6.0 :560, nightly :774, Enhanced :800: GET_ENTITY_SPEED, metres a second.
		public float Speed { get { return Body.Speed; } }
		// v3.6.0 :577, nightly :800, Enhanced :826: GET_ENTITY_VELOCITY, world space.
		public Math.Vector3 Velocity { get { return Body.Velocity; } }
	}

	// GTA/Entities/Peds/Ped.cs. IsInVehicle(): v3.6.0 :588, nightly :1698, Enhanced :1732. CurrentVehicle: v3.6.0 :636,
	// nightly :1772, Enhanced :1809.
	public sealed class Ped : Entity
	{
		public bool IsInVehicle() { return ShvdnStub.Harness.InVehicle; }
		public Vehicle CurrentVehicle { get { return ShvdnStub.Harness.InVehicle ? ShvdnStub.Harness.Vehicle : null; } }
	}

	// GTA/Entities/Vehicles/Vehicle.cs. CurrentGear: v3.6.0 :856, nightly :1022, Enhanced :1268. CurrentRPM: v3.6.0 :1096,
	// nightly :1231, Enhanced :1507.
	public sealed class Vehicle : Entity
	{
		public int CurrentGear { get { return ShvdnStub.Harness.Gear; } }
		public float CurrentRPM { get { return ShvdnStub.Harness.Rpm; } }
	}
}

namespace ShvdnStub
{
	/// <summary>Where an entity is and how it moves.</summary>
	public sealed class Body
	{
		public GTA.Math.Vector3 Position;
		public GTA.Math.Vector3 Rotation;
		public GTA.Math.Vector3 Velocity;
		public GTA.Math.Vector3 Forward = new GTA.Math.Vector3(0f, 1f, 0f);
		public GTA.Math.Vector3 Right = new GTA.Math.Vector3(1f, 0f, 0f);
		public GTA.Math.Vector3 Up = new GTA.Math.Vector3(0f, 0f, 1f);
		public float Speed;
	}

	/// <summary>The game the stub's API reads, set by a test.</summary>
	public static class Harness
	{
		/// <summary>The path of the script, as the real API gives Script.Filename.</summary>
		public static string ScriptFile;

		public static readonly GTA.Player Player = new GTA.Player();
		public static readonly GTA.Ped Ped = new GTA.Ped();
		public static readonly GTA.Vehicle Vehicle = new GTA.Vehicle();
		public static bool InVehicle;
		public static bool PlayerDead;
		public static int Gear;
		public static float Rpm;

		/// <summary>What each native function that returns a bool returns; false when not set.</summary>
		public static readonly Dictionary<GTA.Native.Hash, bool> Flags = new Dictionary<GTA.Native.Hash, bool>();

		/// <summary>What GET_CONTROL_NORMAL returns for each control; 0 when not set.</summary>
		public static readonly Dictionary<int, float> Controls = new Dictionary<int, float>();

		/// <summary>The height of the ground at x, y, or NaN where GET_GROUND_Z_FOR_3D_COORD finds none.</summary>
		public static Func<float, float, float> Ground;

		/// <summary>The heights GET_GROUND_Z_FOR_3D_COORD was asked from, as x, y, z.</summary>
		public static readonly List<float[]> GroundAsked = new List<float[]>();

		public static void Reset()
		{
			ScriptFile = null;
			InVehicle = false;
			PlayerDead = false;
			Gear = 0;
			Rpm = 0f;
			Flags.Clear();
			Controls.Clear();
			Ground = null;
			GroundAsked.Clear();
			Ped.Body = new Body();
			Vehicle.Body = new Body();
		}

		public static void Tick(GTA.Script script) { script.RaiseTick(); }

		public static void Abort(GTA.Script script) { script.RaiseAborted(); }

		public static int Interval(GTA.Script script) { return script.IntervalValue; }

		internal static object Native(GTA.Native.Hash hash, GTA.Native.InputArgument[] arguments)
		{
			if (hash == GTA.Native.Hash.GET_CONTROL_NORMAL)
			{
				float value;
				return Controls.TryGetValue((int)arguments[1].Value, out value) ? value : 0f;
			}
			if (hash == GTA.Native.Hash.GET_GROUND_Z_FOR_3D_COORD)
			{
				var x = (float)arguments[0].Value;
				var y = (float)arguments[1].Value;
				var z = (float)arguments[2].Value;
				GroundAsked.Add(new[] { x, y, z });
				var height = Ground == null ? float.NaN : Ground(x, y);
				var found = !float.IsNaN(height) && height <= z;
				((GTA.Native.OutputArgument)arguments[3].Value).Value = found ? height : 0f;
				return found;
			}
			bool flag;
			return Flags.TryGetValue(hash, out flag) && flag;
		}
	}
}
";
	}
}
