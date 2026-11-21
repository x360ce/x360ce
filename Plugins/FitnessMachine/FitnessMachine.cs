using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using x360ce.Plugins;

namespace x360ce.Plugins.Samples
{
	/// <summary>What the game's force feedback does to a fitness machine.</summary>
	public enum FitnessForce
	{
		[Description("Off")]
		Off,
		[Description("Resistance")]
		Resistance,
		[Description("Road grade")]
		Grade,
	}

	#region The device

	/// <summary>An exercise bike, treadmill, rower or cross trainer, read over Bluetooth's standard Fitness Machine Service (FTMS), as a controller.</summary>
	/// <remarks>
	/// The machine is chosen and connected on the Fitness tab (<see cref="FitnessPage"/>), and held by the session every
	/// device of the kind shares (<see cref="FitnessSession"/>): a computer has one Bluetooth radio. <see cref="Read"/>
	/// copies the newest values the machine sent from the session's volatile fields, and the session's worker sends the
	/// machine what <see cref="NextCommand"/> works out from the game's force, four times a second at most.
	///
	/// The first read joins the device to the session, which connects again to the machine connected last when nothing is
	/// held. Disposing the device, as x360ce does when it is unticked, disabled, taken off every controller or x360ce
	/// closes, leaves the session; the last device to leave lets go of the machine, without waiting for it.
	///
	/// x360ce sets the settings on the thread that calls <see cref="Read"/>; those the worker reads too are volatile.
	/// </remarks>
	[Description("An exercise bike, treadmill, rower or cross trainer as a controller, read over Bluetooth: pedal, run or row to drive a game. "
		+ "Experimental: tested on a Tunturi E60R exercise bike only; use it with care and at your own risk. It reads any machine that uses "
		+ "Bluetooth's standard Fitness Machine Service (FTMS), as most smart bikes and trainers do, and needs Windows 10 or later. Find and "
		+ "connect the machine on the Fitness tab, which adds this device to the Devices list as the machine connects.\n\n"
		+ "Axis 1 is your speed going forward and axis 2 your speed in reverse, from standing still to Max speed. Axis 3 rests in the middle "
		+ "and moves up as you go forward and down in reverse, as a stick does for walking. Axis 4 is your cadence, axis 5 your power and "
		+ "axis 6 your heart rate; slider 1 is your speed either way, and slider 2 the machine's resistance. Button 1 is held while you move, "
		+ "button 2 while in reverse, button 3 for a moment at each heartbeat, and button 4 while your heart rate is at or above Heart rate "
		+ "limit. A machine that sends nothing for 3 seconds reads as standing still.\n\n"
		+ "Map to on the Fitness tab, or the device put on a controller, maps it to drive: Right Trigger to Axis 1 and Left Trigger to Axis 2, "
		+ "as Auto Preset does too. To walk or run on foot, map Left Thumb Axis Y to IAxis 3.\n\n"
		+ "Reverse: tick Reverse, or set Reverse gear, and turning the machine's own resistance down to that level drives backwards. A cross "
		+ "trainer pedalled backwards drives backwards by itself.\n\n"
		+ "Force feedback: tick Enable on the controller's Force Feedback page, and the game's rumble makes the machine harder "
		+ "to pedal, by up to Extra resistance levels, or makes the road climb, by up to Road grade, as Force feedback to says. When the rumble "
		+ "stops, the machine goes back to the level you set on it. While your heart rate is at or above Heart rate limit, the rumble adds "
		+ "nothing.\n\n"
		+ "Unticked, disabled or taken off every controller, the device lets go of the machine, which goes back to the level you set on it; "
		+ "read again, it connects to the machine again.\n\n"
		+ "A computer's Bluetooth cannot connect to itself, so a transmitter such as the Virtual Fitness Machine Transmitter must run on "
		+ "another device, not on this computer.")]
	public class FitnessMachine : IPluginDevice, IPluginForceFeedback, IPluginMapping, IDisposable
	{
		/// <summary>Names the kind of device: the Fitness tab adds and maps the device by it.</summary>
		internal static readonly Guid Product = new Guid("6e1793da-b362-49b0-ad25-08433feac3f8");

		/// <summary>A device of the process's session, as x360ce makes one.</summary>
		public FitnessMachine() : this(FitnessSession.Shared)
		{
		}

		/// <summary>A device of a session of its own, as a test makes one.</summary>
		internal FitnessMachine(FitnessSession session)
		{
			_session = session;
		}

		readonly FitnessSession _session;

		/// <summary>The session the device reads.</summary>
		internal FitnessSession Session { get { return _session; } }

		public string Name { get { return "Fitness Machine"; } }
		public Guid ProductGuid { get { return Product; } }
		public int AxisCount { get { return 6; } }
		public int SliderCount { get { return 2; } }
		public int PovCount { get { return 0; } }
		public int ButtonCount { get { return 4; } }

		/// <summary>Drives: forward, axis 1, on Right Trigger, and reverse, axis 2, on Left Trigger.</summary>
		/// <remarks>
		/// The mapping for driving a car or riding a bike. Whole axes, so a trigger is released standing still and pressed all
		/// the way at Max speed. Walking, axis 3 on the left stick, is left for the person to map, as the help says.
		/// </remarks>
		public string Mapping { get { return "righttrigger:a0,lefttrigger:a1"; } }

		#region Settings

		[Category("Ranges"), DisplayName("Max speed (km/h)")]
		[Description("The speed that moves axes 1, 2 and 3 and slider 1 all the way.")]
		[Range(1.0, 200.0)]
		public double MaxSpeed { get; set; } = 40;

		[Category("Ranges"), DisplayName("Max cadence (rpm)")]
		[Description("The cadence that moves axis 4 all the way.")]
		[Range(10, 250)]
		public int MaxCadence { get; set; } = 120;

		[Category("Ranges"), DisplayName("Max power (W)")]
		[Description("The power that moves axis 5 all the way.")]
		[Range(50, 3000)]
		public int MaxPower { get; set; } = 400;

		[Category("Ranges"), DisplayName("Min heart rate (bpm)")]
		[Description("The heart rate at which axis 6 starts to move.")]
		[Range(30, 220)]
		public int MinHeartRate { get; set; } = 60;

		[Category("Ranges"), DisplayName("Max heart rate (bpm)")]
		[Description("The heart rate that moves axis 6 all the way.")]
		[Range(60, 240)]
		public int MaxHeartRate { get; set; } = 190;

		[Category("Reverse"), DisplayName("Reverse")]
		[Description("Drives in reverse: your speed moves axis 2 in place of axis 1, and axis 3 down in place of up.")]
		public bool Reverse { get; set; }

		[Category("Reverse"), DisplayName("Reverse gear")]
		[Description("Drives in reverse while the resistance you set on the machine is at or below this level, so the machine's own gear or "
			+ "resistance lever puts it in reverse. 0 never does.")]
		[Range(0, 100)]
		public int ReverseGear { get; set; }

		[Category("Force feedback"), DisplayName("Force feedback to")]
		[Description("What the game's rumble does once Enable is ticked on the controller's Force Feedback page. Resistance: "
			+ "the machine gets harder to pedal. Road grade: the road climbs, on a machine that simulates a road. Off: nothing.")]
		public FitnessForce ForceTo
		{
			get { return _forceTo; }
			set { _forceTo = value; }
		}
		volatile FitnessForce _forceTo = FitnessForce.Resistance;

		[Category("Force feedback"), DisplayName("Extra resistance (levels)")]
		[Description("The resistance levels the game's strongest rumble adds to the level you set on the machine; a lighter rumble adds less.")]
		[Range(0.0, 100.0)]
		public double ForceResistance
		{
			get { return _forceResistance; }
			set
			{
				_forceResistance = value;
				_forceResistanceTenths = (int)Math.Round(Math.Max(0, Math.Min(100, value)) * Ftms.ResistanceScale);
			}
		}
		double _forceResistance = 10;
		volatile int _forceResistanceTenths = 100;

		[Category("Force feedback"), DisplayName("Road grade (%)")]
		[Description("The road grade, in percent, the game's strongest rumble climbs; a lighter rumble climbs less.")]
		[Range(0.0, 20.0)]
		public double ForceGrade
		{
			get { return _forceGrade; }
			set
			{
				_forceGrade = value;
				_forceGradeHundredths = (int)Math.Round(Math.Max(0, Math.Min(20, value)) * Ftms.GradeScale);
			}
		}
		double _forceGrade = 8;
		volatile int _forceGradeHundredths = 800;

		[Category("Heart rate"), DisplayName("Heart rate limit (bpm)")]
		[Description("While your heart rate is at or above it, the game's rumble adds nothing and button 4 is held. 0 is off.")]
		[Range(0, 240)]
		public int HeartRateLimit
		{
			get { return _heartRateLimit; }
			set { _heartRateLimit = value; }
		}
		volatile int _heartRateLimit;

		#endregion

		#region Status

		[DisplayName("Connection")]
		[Description("What the Fitness tab's connection is doing: looking for a machine, connecting, connected, or why it cannot.")]
		public string Connection { get { return _session.StatusText; } }

		[DisplayName("Features")]
		[Description("What the machine measures, and the targets it accepts, as it says itself.")]
		public string Features { get { return _session.FeaturesText; } }

		[DisplayName("Speed")]
		[Description("Your speed now.")]
		public string Speed { get { return _session.Shown(_session.Speed, Ftms.SpeedScale, "0.0", " km/h"); } }

		[DisplayName("Cadence")]
		[Description("Your cadence now: pedal turns, steps or strokes a minute.")]
		public string Cadence { get { return _session.Shown(_session.Cadence, Ftms.CadenceScale, "0", " rpm"); } }

		[DisplayName("Power")]
		[Description("Your power now.")]
		public string Power { get { return _session.Shown(_session.Power, 1, "0", " W"); } }

		[DisplayName("Heart rate")]
		[Description("Your heart rate now, from the machine's own heart rate sensor or a strap paired with it.")]
		public string HeartRate { get { return _session.HeartRate > 0 ? _session.Shown(_session.HeartRate, 1, "0", " bpm") : ""; } }

		[DisplayName("Resistance")]
		[Description("The machine's resistance level now, and the range it goes over.")]
		public string Resistance
		{
			get
			{
				var level = _session.Shown(_session.Level, 1, "0", "");
				var min = _session.RangeMin;
				var max = _session.RangeMax;
				return level.Length == 0 || min == FitnessSession.Unknown || max == FitnessSession.Unknown ? level
					: level + " (" + (min / Ftms.ResistanceScale).ToString("0.#", CultureInfo.InvariantCulture)
						+ " to " + (max / Ftms.ResistanceScale).ToString("0.#", CultureInfo.InvariantCulture) + ")";
			}
		}

		[DisplayName("Distance")]
		[Description("How far you have gone since the machine started counting.")]
		public string Distance { get { return _session.Shown(_session.Distance, 1000, "0.00", " km"); } }

		[DisplayName("Elapsed time")]
		[Description("How long you have trained since the machine started counting.")]
		public string ElapsedTime
		{
			get
			{
				var seconds = _session.Elapsed;
				return _session.Fresh(_session.Now) && seconds != FitnessSession.Unknown
					? TimeSpan.FromSeconds(seconds).ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture)
					: "";
			}
		}

		[DisplayName("Force")]
		[Description("How strong the game's rumble is, and the target it sets on the machine.")]
		public string Force { get { return DescribeForce(); } }

		[DisplayName("Updates per second")]
		[Description("How many sets of values the machine sent in the last second.")]
		public int Updates { get { return _session.Updates; } }

		#endregion

		#region Read

		/// <summary>True once the device was read, and so joined the session.</summary>
		bool _joined;

		/// <summary>True once the device was let go of: it never joins the session again.</summary>
		volatile bool _disposed;

		public void Read(PluginState state)
		{
			var session = _session;
			// The first read joins the session, which connects to the machine connected last when nothing is held.
			if (!_joined && !_disposed)
			{
				_joined = true;
				session.Join(this);
			}
			var now = session.Now;
			session.Count(now);
			// A value the machine has not sent, or not for 3 seconds, reads as rest.
			var fresh = session.Fresh(now);
			var speed = fresh ? Math.Max(0, session.Speed) : 0;
			var cadence = fresh ? Math.Max(0, session.Cadence) : 0;
			var power = fresh ? Math.Max(0, session.Power) : 0;
			var heartRate = fresh ? Math.Max(0, session.HeartRate) : 0;
			var level = fresh ? session.Level : FitnessSession.Unknown;
			var gear = session.Base;
			var reverse = Reverse || (fresh && (session.Backward || (ReverseGear > 0 && gear != FitnessSession.Unknown && gear <= ReverseGear)));
			// Held inside the ranges the settings give, in case a value from elsewhere is not.
			var pace = Part(speed, Math.Max(1, MaxSpeed) * Ftms.SpeedScale);
			var travel = Travel(pace);
			state.Axes[0] = reverse ? 0 : travel;
			state.Axes[1] = reverse ? travel : 0;
			state.Axes[2] = reverse
				? PluginState.Middle + (int)Math.Round(pace * (PluginState.Max - PluginState.Middle))
				: PluginState.Middle - (int)Math.Round(pace * PluginState.Middle);
			state.Axes[3] = Travel(Part(cadence, Math.Max(1, MaxCadence) * Ftms.CadenceScale));
			state.Axes[4] = Travel(Part(power, Math.Max(1, MaxPower)));
			state.Axes[5] = heartRate > 0 ? Travel(Part(heartRate - MinHeartRate, Math.Max(1, MaxHeartRate - MinHeartRate))) : 0;
			state.Sliders[0] = travel;
			state.Sliders[1] = level == FitnessSession.Unknown ? 0 : Travel(LevelPart(level));
			state.Buttons[0] = speed > 0 || cadence > 0;
			state.Buttons[1] = reverse;
			state.Buttons[2] = heartRate > 0 && Heartbeat(heartRate, session.Milliseconds);
			var limit = _heartRateLimit;
			state.Buttons[3] = limit > 0 && heartRate >= limit;
		}

		/// <summary>How far a value is along 0 to <paramref name="max"/>, held inside 0 to 1.</summary>
		static double Part(double value, double max)
		{
			return value <= 0 ? 0 : value >= max ? 1 : value / max;
		}

		/// <summary>A part of the way as an axis or a slider, 0 to 65535.</summary>
		static int Travel(double part)
		{
			return (int)Math.Round(part * PluginState.Max);
		}

		/// <summary>How far a resistance level is along the machine's range, or along 0 to 100 while the range is not known.</summary>
		double LevelPart(int level)
		{
			var min = _session.RangeMin;
			var max = _session.RangeMax;
			return min != FitnessSession.Unknown && max > min ? Part(level * Ftms.ResistanceScale - min, max - min) : Part(level, 100);
		}

		/// <summary>True for the first quarter of each beat.</summary>
		static bool Heartbeat(int heartRate, long milliseconds)
		{
			var beat = 60000 / heartRate;
			return milliseconds % beat < beat / 4;
		}

		/// <summary>Leaves the session without waiting: the last device to leave lets go of the machine, which gets back the level the person set.</summary>
		/// <remarks>Called on x360ce's input thread, which must not wait: the session's worker lets go of the machine.</remarks>
		public void Dispose()
		{
			if (_disposed)
				return;
			_disposed = true;
			if (_joined)
				_session.Leave(this);
		}

		#endregion

		#region Force feedback

		/// <summary>The game's force, 0 to 65535: the stronger of the two motors.</summary>
		volatile int _force;

		public void SetForce(int largeMotor, int smallMotor)
		{
			_force = Math.Max(0, Math.Min(PluginState.Max, Math.Max(largeMotor, smallMotor)));
		}

		/// <summary>
		/// The command the machine is due next, or null: the one way the game's force reaches the machine. The session's
		/// worker sends what this returns, four times a second at most and one at a time; a test reads it without Bluetooth.
		/// </summary>
		/// <remarks>
		/// The target follows the game's force, the Force feedback to setting and the heart rate limit. A new target is due
		/// only once it is a whole level, or half a percent of grade, from the one sent, or when it goes back to the level
		/// the person set. The machine is asked for control once before the first, and a target of a kind it does not take
		/// is never sent.
		/// </remarks>
		public byte[] NextCommand()
		{
			var session = _session;
			if (session.Features == FitnessSession.Unknown)
				return null;
			var wanted = _forceTo;
			if (!Accepts(wanted))
				wanted = FitnessForce.Off;
			var sent = session.SentKind;
			// A target of another kind than the one wanted is given back first.
			var kind = sent != FitnessForce.Off ? sent : wanted;
			if (kind == FitnessForce.Off)
				return null;
			var rest = session.Rest(kind);
			var target = kind == wanted ? TargetValue(kind) : rest;
			var from = sent == FitnessForce.Off ? rest : session.SentValue;
			if (target == from || (target != rest && Math.Abs(target - from) < Step(kind)))
				return null;
			if (!session.ControlRequested)
			{
				session.ControlRequested = true;
				session.CommandAt = session.Now;
				return Ftms.RequestControl();
			}
			session.SentValue = target;
			session.SentKind = target == rest ? FitnessForce.Off : kind;
			session.CommandAt = session.Now;
			return kind == FitnessForce.Resistance ? Ftms.Resistance(target) : Ftms.Grade(target);
		}

		/// <summary>True when the machine takes a target of this kind.</summary>
		bool Accepts(FitnessForce kind)
		{
			var bit = kind == FitnessForce.Resistance ? Ftms.ResistanceTargetBit : kind == FitnessForce.Grade ? Ftms.SimulationTargetBit : 0;
			var session = _session;
			return bit != 0 && session.Features != FitnessSession.Unknown && (session.Targets & bit) != 0 && (session.Refused & (1 << (int)kind)) == 0;
		}

		/// <summary>The target of this kind due now, in the units it is sent in: tenths of a level, or hundredths of a percent.</summary>
		int TargetValue(FitnessForce kind)
		{
			var rest = _session.Rest(kind);
			if (HeartRateHigh())
				return rest;
			var force = (double)_force;
			if (kind == FitnessForce.Grade)
				return (int)Math.Round(force * _forceGradeHundredths / PluginState.Max);
			var value = rest + (int)Math.Round(force * _forceResistanceTenths / PluginState.Max);
			var max = _session.RangeMax;
			return max != FitnessSession.Unknown && value > max ? Math.Max(rest, max) : value;
		}

		/// <summary>The smallest change of a target worth a command: a whole level, or half a percent of grade.</summary>
		static int Step(FitnessForce kind)
		{
			return kind == FitnessForce.Resistance ? (int)Ftms.ResistanceScale : (int)(Ftms.GradeScale / 2);
		}

		bool HeartRateHigh()
		{
			var limit = _heartRateLimit;
			return limit > 0 && _session.Fresh(_session.Now) && _session.HeartRate >= limit;
		}

		string DescribeForce()
		{
			var kind = _forceTo;
			if (kind == FitnessForce.Off)
				return "Off.";
			var percent = ((_force * 100 + PluginState.Max / 2) / PluginState.Max).ToString(CultureInfo.InvariantCulture) + " %";
			if (_session.Features == FitnessSession.Unknown)
				return percent + ".";
			var note = _session.ForceNote;
			if (note != null)
				return note;
			if (!Accepts(kind))
				return _session.MachineName + (kind == FitnessForce.Resistance ? " takes no resistance target." : " takes no road grade.");
			var value = TargetValue(kind);
			var target = kind == FitnessForce.Resistance
				? "resistance " + (value / Ftms.ResistanceScale).ToString("0.#", CultureInfo.InvariantCulture)
				: "grade " + (value / Ftms.GradeScale).ToString("0.##", CultureInfo.InvariantCulture) + " %";
			return HeartRateHigh() ? "Heart rate limit reached: " + target + "." : percent + " → " + target;
		}

		#endregion
	}

	#endregion

	#region The protocol

	/// <summary>
	/// The Bluetooth Fitness Machine Service (FTMS) identifiers, op codes, scales and names, from the Bluetooth SIG's
	/// specification, and the commands written to a machine's control point. Multi-byte fields are little-endian.
	/// </summary>
	static class Ftms
	{
		public const ushort ServiceId = 0x1826;
		public const ushort FeatureId = 0x2ACC;
		public const ushort TreadmillDataId = 0x2ACD;
		public const ushort CrossTrainerDataId = 0x2ACE;
		public const ushort RowerDataId = 0x2AD1;
		public const ushort IndoorBikeDataId = 0x2AD2;
		public const ushort TrainingStatusId = 0x2AD3;
		public const ushort ResistanceRangeId = 0x2AD6;
		public const ushort PowerRangeId = 0x2AD8;
		public const ushort ControlPointId = 0x2AD9;
		public const ushort StatusId = 0x2ADA;

		public const byte RequestControlOp = 0x00;
		public const byte ResetOp = 0x01;
		public const byte SetTargetResistanceOp = 0x04;
		public const byte SetTargetPowerOp = 0x05;
		public const byte StartOp = 0x07;
		public const byte StopOrPauseOp = 0x08;
		public const byte SetSimulationOp = 0x11;
		public const byte ResponseOp = 0x80;

		/// <summary>The parameter of Stop or Pause that stops.</summary>
		public const byte StopParameter = 0x01;

		/// <summary>The parameter of Stop or Pause that pauses.</summary>
		public const byte PauseParameter = 0x02;

		public const byte SuccessResult = 0x01;
		public const byte NotSupportedResult = 0x02;
		public const byte ControlNotPermittedResult = 0x05;
		public const byte ControlPermissionLostStatus = 0xFF;

		/// <summary>The Resistance bit of the Fitness Machine Feature value's targets.</summary>
		public const int ResistanceTargetBit = 1 << 2;

		/// <summary>The Power bit of the Fitness Machine Feature value's targets.</summary>
		public const int PowerTargetBit = 1 << 3;

		/// <summary>The Indoor Bike Simulation bit of the Fitness Machine Feature value's targets: a road grade.</summary>
		public const int SimulationTargetBit = 1 << 13;

		/// <summary>Speed fields count hundredths of a km/h.</summary>
		public const double SpeedScale = 100;

		/// <summary>Cadence fields count half turns a minute.</summary>
		public const double CadenceScale = 2;

		/// <summary>Resistance targets and ranges count tenths of a level; the data fields count whole levels.</summary>
		public const double ResistanceScale = 10;

		/// <summary>A simulated grade counts hundredths of a percent.</summary>
		public const double GradeScale = 100;

		/// <summary>The rolling resistance sent with a grade, in ten-thousandths: 0.004, a road's.</summary>
		const byte RollingResistance = 40;

		/// <summary>The wind resistance sent with a grade, in hundredths of a kg/m: 0.51, a rider's.</summary>
		const byte WindResistance = 51;

		/// <summary>What the machine names in its Feature value it measures, by bit.</summary>
		static readonly string[] MeasureNames =
		{
			"average speed", "cadence", "distance", "incline", "elevation gain", "pace", "steps", "resistance", "strides", "energy",
			"heart rate", "metabolic equivalent", "elapsed time", "remaining time", "power", "force on belt", "user data",
		};

		/// <summary>The targets the machine names in its Feature value it accepts, by bit.</summary>
		static readonly string[] TargetNames =
		{
			"speed", "incline", "resistance", "power", "heart rate", "energy", "steps", "strides", "distance", "training time",
			"time in two heart rate zones", "time in three heart rate zones", "time in five heart rate zones", "road grade",
			"wheel circumference", "spin down", "cadence",
		};

		/// <summary>The kinds of machine an advertisement's service data names, by bit.</summary>
		static readonly string[] MachineTypeNames = { "treadmill", "cross trainer", "step climber", "stair climber", "rower", "indoor bike" };

		static readonly string[] TrainingStatusNames =
		{
			"other", "idle", "warming up", "low intensity interval", "high intensity interval", "recovery interval", "isometric",
			"heart rate control", "fitness test", "speed too low", "speed too high", "cooling down", "watt control",
			"manual mode (quick start)", "pre-workout", "post-workout",
		};

		#region Commands

		public static byte[] RequestControl()
		{
			return new[] { RequestControlOp };
		}

		/// <summary>The command a button or a target of the Fitness tab sends.</summary>
		/// <param name="command">The command.</param>
		/// <param name="value">A target's value: watts, a resistance level, or a road grade in percent; nothing for the others.</param>
		public static byte[] Command(FitnessCommand command, double value)
		{
			switch (command)
			{
				case FitnessCommand.Start:
					return new[] { StartOp };
				case FitnessCommand.Pause:
					return new[] { StopOrPauseOp, PauseParameter };
				case FitnessCommand.Stop:
					return new[] { StopOrPauseOp, StopParameter };
				case FitnessCommand.Reset:
					return new[] { ResetOp };
				case FitnessCommand.Power:
					return Int16(SetTargetPowerOp, (int)Math.Round(value));
				case FitnessCommand.Resistance:
					return Resistance((int)Math.Round(value * ResistanceScale));
				case FitnessCommand.Grade:
					return Grade((int)Math.Round(value * GradeScale));
				default:
					throw new ArgumentOutOfRangeException(nameof(command), command, "Not a command.");
			}
		}

		/// <summary>
		/// A resistance target, as a signed 16-bit count of tenths of a level: the form the Bluetooth SIG's FTMS test suite
		/// writes. A machine that reads only the first byte, as the specification's table names, gets the same value up to 25.5.
		/// </summary>
		public static byte[] Resistance(int tenths)
		{
			return Int16(SetTargetResistanceOp, tenths);
		}

		/// <summary>An indoor bike simulation of a road with no wind, the grade given, and a road's rolling and a rider's wind resistance.</summary>
		public static byte[] Grade(int hundredths)
		{
			return new[] { SetSimulationOp, (byte)0, (byte)0, (byte)hundredths, (byte)(hundredths >> 8), RollingResistance, WindResistance };
		}

		static byte[] Int16(byte op, int value)
		{
			value = Math.Max(short.MinValue, Math.Min(short.MaxValue, value));
			return new[] { op, (byte)value, (byte)(value >> 8) };
		}

		#endregion

		#region Names

		/// <summary>What the machine measures and the targets it accepts, as the Fitness Machine Feature value names them: "Measures: cadence, power. Targets: resistance."</summary>
		public static string Describe(int features, int targets)
		{
			var measures = Names(features, MeasureNames);
			var accepts = Names(targets, TargetNames);
			return "Measures: " + (measures.Length > 0 ? measures : "nothing listed") + ". Targets: " + (accepts.Length > 0 ? accepts : "none") + ".";
		}

		/// <summary>The kinds of machine named in an advertisement's fitness service data, after its service ID: flags, then the kinds.</summary>
		public static string MachineTypes(byte[] serviceData)
		{
			return serviceData.Length < 3 ? "" : Names(serviceData[1] | serviceData[2] << 8, MachineTypeNames);
		}

		/// <summary>A Training Status value in words, with the text the machine adds.</summary>
		public static string TrainingStatus(byte[] value)
		{
			if (value.Length < 2)
				return "unknown";
			var name = value[1] < TrainingStatusNames.Length ? TrainingStatusNames[value[1]] : "0x" + value[1].ToString("X2", CultureInfo.InvariantCulture);
			return (value[0] & 0x01) != 0 && value.Length > 2 ? name + " (" + System.Text.Encoding.UTF8.GetString(value, 2, value.Length - 2) + ")" : name;
		}

		/// <summary>
		/// A Fitness Machine Status value in words. A new resistance target comes as one unsigned byte from FTMS 1.0 machines
		/// and as a signed 16-bit value from 1.0.1 machines; the length tells them apart.
		/// </summary>
		public static string MachineStatus(byte[] value)
		{
			if (value.Length == 0)
				return "empty";
			switch (value[0])
			{
				case 0x01:
					return "reset";
				case 0x02:
					return value.Length > 1 && value[1] == PauseParameter ? "paused" : "stopped";
				case 0x03:
					return "stopped by the safety key";
				case 0x04:
					return "started or resumed";
				case 0x07:
					return value.Length < 2 ? "target resistance changed"
						: "target resistance changed to " + ((value.Length > 2 ? BitConverter.ToInt16(value, 1) : value[1]) / ResistanceScale).ToString("0.#", CultureInfo.InvariantCulture);
				case 0x08:
					return value.Length < 3 ? "target power changed" : "target power changed to " + BitConverter.ToInt16(value, 1).ToString(CultureInfo.InvariantCulture) + " W";
				case 0x12:
					return value.Length < 5 ? "simulation changed" : "simulation changed: grade " + (BitConverter.ToInt16(value, 3) / GradeScale).ToString("0.00", CultureInfo.InvariantCulture) + " %";
				case ControlPermissionLostStatus:
					return "x360ce lost control";
				default:
					return "status " + BitConverter.ToString(value).Replace('-', ' ');
			}
		}

		/// <summary>A control point's answer in words, such as "Set target power: success".</summary>
		public static string Answer(byte[] value)
		{
			if (value == null || value.Length < 3 || value[0] != ResponseOp)
				return "an answer x360ce does not know: " + (value == null ? "none" : BitConverter.ToString(value).Replace('-', ' '));
			string request;
			switch (value[1])
			{
				case RequestControlOp: request = "Request control"; break;
				case ResetOp: request = "Reset"; break;
				case SetTargetResistanceOp: request = "Set target resistance"; break;
				case SetTargetPowerOp: request = "Set target power"; break;
				case StartOp: request = "Start or resume"; break;
				case StopOrPauseOp: request = "Stop or pause"; break;
				case SetSimulationOp: request = "Set simulation"; break;
				default: request = "Op code 0x" + value[1].ToString("X2", CultureInfo.InvariantCulture); break;
			}
			string result;
			switch (value[2])
			{
				case SuccessResult: result = "success"; break;
				case NotSupportedResult: result = "not supported by the machine"; break;
				case 0x03: result = "value refused as invalid"; break;
				case 0x04: result = "failed"; break;
				case ControlNotPermittedResult: result = "control not permitted"; break;
				default: result = "result 0x" + value[2].ToString("X2", CultureInfo.InvariantCulture); break;
			}
			return request + ": " + result;
		}

		static string Names(int bits, string[] names)
		{
			return string.Join(", ", names.Where((name, bit) => (bits & (1 << bit)) != 0));
		}

		#endregion
	}

	/// <summary>A command a button or a target of the Fitness tab sends.</summary>
	enum FitnessCommand
	{
		Start,
		Pause,
		Stop,
		Reset,
		Power,
		Resistance,
		Grade,
	}

	#endregion

	#region The session

	/// <summary>
	/// The one connection to a fitness machine the process holds, shared by the Fitness tab and every Fitness Machine
	/// device: a computer has one Bluetooth radio. The tab finds machines, connects and sends commands; the devices read
	/// what the machine sends, and send it the game's force.
	/// </summary>
	/// <remarks>
	/// Bluetooth is worked on a thread of the session's own, started when first needed. It scans, connects, sends one
	/// command at a time and lets go of the machine as the tab and the devices ask, through volatile fields, so neither
	/// waits for it. Every value the machine sends passes through <see cref="Take"/>, which keeps the newest in volatile
	/// fields for a device's Read, and the readings, the activity log and the machines heard for the tab under a lock the
	/// input thread never takes.
	///
	/// Who connects: the tab's Connect; the tab at start, when Reconnect is ticked, to the machine connected last; and a
	/// device's first read, to the machine connected last in this run, when nothing is held. Those two retry quietly, a
	/// little later after each failure, until they connect or the person presses Disconnect. Who lets go: the tab's
	/// Disconnect; the last device read being disposed; and x360ce closing, which waits a moment for it. Letting go gives
	/// the machine back the level the person set, asks it to stop sending, and disposes every Bluetooth object.
	///
	/// Windows' Bluetooth types are named only in <see cref="Scanner"/> and <see cref="Link"/>, which only the worker uses,
	/// so on a Windows without them the worker says so and nothing else fails.
	/// </remarks>
	sealed class FitnessSession
	{
		/// <summary>The session of the process.</summary>
		internal static readonly FitnessSession Shared = new FitnessSession();

		#region Timing

		/// <summary>How long a value counts after the last packet: then the machine reads as standing still.</summary>
		const int StaleMilliseconds = 3000;

		/// <summary>How often the worker looks for something to do.</summary>
		const int TickMilliseconds = 100;

		/// <summary>How often at most the game's force is sent: four times a second.</summary>
		const int CommandMilliseconds = 250;

		/// <summary>How long the machine gets to answer a command, or to stop sending its values.</summary>
		const int AnswerMilliseconds = 2000;

		/// <summary>How long after a command the resistance the machine reports is not taken for the level the person set: it can still show the level before.</summary>
		const int QuietMilliseconds = 2000;

		/// <summary>How long a closing x360ce waits for the machine to be let go of: .NET Framework gives the handlers of a closing process two seconds in all.</summary>
		const int ExitMilliseconds = 1500;

		/// <summary>How long a quiet connection waits before it tries again after a failure: longer after each, up to the last.</summary>
		static readonly int[] BackOffMilliseconds = { 2000, 5000, 10000 };

		/// <summary>How many lines the activity log keeps.</summary>
		const int LogLines = 200;

		readonly Stopwatch _watch = Stopwatch.StartNew();

		/// <summary>Milliseconds since the session was made, wrapping after 24 days; a difference of two stays right across the wrap.</summary>
		internal int Now { get { return unchecked((int)_watch.ElapsedMilliseconds); } }

		/// <summary>Milliseconds since the session was made.</summary>
		internal long Milliseconds { get { return _watch.ElapsedMilliseconds; } }

		#endregion

		#region What the machine sent

		/// <summary>A value the machine has not sent.</summary>
		internal const int Unknown = int.MinValue;

		/// <summary>Hundredths of a km/h.</summary>
		internal volatile int Speed = Unknown;

		/// <summary>Half turns, steps or half strokes a minute.</summary>
		internal volatile int Cadence = Unknown;

		/// <summary>Watts.</summary>
		internal volatile int Power = Unknown;

		/// <summary>Beats a minute; 0 or less when there is no reading.</summary>
		internal volatile int HeartRate = Unknown;

		/// <summary>The resistance level the machine reports, in whole levels.</summary>
		internal volatile int Level = Unknown;

		/// <summary>The level the person set on the machine, in whole levels: what it reported while no target of x360ce's held it.</summary>
		internal volatile int Base = Unknown;

		/// <summary>Metres.</summary>
		internal volatile int Distance = Unknown;

		/// <summary>Seconds.</summary>
		internal volatile int Elapsed = Unknown;

		/// <summary>True while a cross trainer says it moves backwards.</summary>
		internal volatile bool Backward;

		/// <summary>When the last packet of values came, by <see cref="Now"/>.</summary>
		volatile int _dataAt;
		volatile bool _hasData;

		/// <summary>Packets of values since <see cref="Count"/> last counted them.</summary>
		int _received;

		/// <summary>When <see cref="Count"/> last counted the packets, by <see cref="Now"/>.</summary>
		int _countedAt;
		volatile int _updates;

		/// <summary>How many sets of values the machine sent in the last second.</summary>
		internal int Updates { get { return _updates; } }

		/// <summary>What the machine measures, from its Feature value; Unknown until read.</summary>
		internal volatile int Features = Unknown;

		/// <summary>The targets the machine accepts, from its Feature value; none for a machine without a control point.</summary>
		internal volatile int Targets;

		/// <summary>The lowest and highest resistance target the machine accepts, in tenths of a level; Unknown until read.</summary>
		internal volatile int RangeMin = Unknown;
		internal volatile int RangeMax = Unknown;

		/// <summary>The lowest and highest power target the machine accepts, in watts; Unknown until read.</summary>
		internal volatile int PowerMin = Unknown;
		internal volatile int PowerMax = Unknown;

		/// <summary>True while the machine connected has a control point, which takes commands.</summary>
		internal volatile bool CanCommand;

		/// <summary>
		/// Takes one value the machine sent or answered, by the short ID of its characteristic, such as 0x2AD2 for Indoor
		/// Bike Data: the one way a value reaches the session.
		/// </summary>
		/// <remarks>
		/// The worker calls it for every notification and every value it reads, on whichever thread Windows raises them, and
		/// a test calls it with bytes of its own. A packet that ends before all the fields its flags announce keeps the
		/// fields it holds, and a field it does not hold keeps its value.
		/// </remarks>
		/// <param name="characteristicId">The characteristic's short ID.</param>
		/// <param name="value">The bytes, as Bluetooth carries them.</param>
		internal void Take(ushort characteristicId, byte[] value)
		{
			if (value == null)
				return;
			switch (characteristicId)
			{
				case Ftms.IndoorBikeDataId:
					TakeIndoorBike(value);
					break;
				case Ftms.TreadmillDataId:
					TakeTreadmill(value);
					break;
				case Ftms.CrossTrainerDataId:
					TakeCrossTrainer(value);
					break;
				case Ftms.RowerDataId:
					TakeRower(value);
					break;
				case Ftms.FeatureId:
					if (value.Length >= 8)
					{
						Targets = BitConverter.ToInt32(value, 4);
						Features = BitConverter.ToInt32(value, 0);
						Changed();
					}
					break;
				case Ftms.ResistanceRangeId:
					if (value.Length >= 4)
					{
						RangeMin = BitConverter.ToInt16(value, 0);
						RangeMax = BitConverter.ToInt16(value, 2);
						Changed();
					}
					break;
				case Ftms.PowerRangeId:
					if (value.Length >= 4)
					{
						PowerMin = BitConverter.ToInt16(value, 0);
						PowerMax = BitConverter.ToInt16(value, 2);
						Changed();
					}
					break;
				case Ftms.TrainingStatusId:
					lock (_lock)
						Reading("Training status", Ftms.TrainingStatus(value), Milliseconds);
					Changed();
					break;
				case Ftms.StatusId:
					Note("Machine status: " + Ftms.MachineStatus(value) + ".");
					// Another app took control: it is asked for again before the next command.
					if (value.Length > 0 && value[0] == Ftms.ControlPermissionLostStatus)
						ControlRequested = false;
					break;
				case Ftms.ControlPointId:
					var answer = _answer;
					if (answer != null && value.Length >= 3 && value[0] == Ftms.ResponseOp)
						answer.TrySetResult(value);
					break;
			}
		}

		void TakeIndoorBike(byte[] value)
		{
			var fields = new Fields(value);
			var read = fields.UInt16();
			if (!read.HasValue)
				return;
			var flags = read.Value;
			// More Data: a record split over several packets carries its speed in the last.
			var speed = (flags & 0x0001) == 0 ? fields.UInt16() : null;
			if ((flags & 0x0002) != 0)
				fields.Skip(2); // Average speed
			var cadence = (flags & 0x0004) != 0 ? fields.UInt16() : null;
			if ((flags & 0x0008) != 0)
				fields.Skip(2); // Average cadence
			var distance = (flags & 0x0010) != 0 ? fields.UInt24() : null;
			var level = (flags & 0x0020) != 0 ? fields.SInt16() : null;
			var power = (flags & 0x0040) != 0 ? fields.SInt16() : null;
			if ((flags & 0x0080) != 0)
				fields.Skip(2); // Average power
			if ((flags & 0x0100) != 0)
				fields.Skip(5); // Energy: total, per hour and per minute
			var heartRate = (flags & 0x0200) != 0 ? fields.UInt8() : null;
			if ((flags & 0x0400) != 0)
				fields.Skip(1); // Metabolic equivalent
			var elapsed = (flags & 0x0800) != 0 ? fields.UInt16() : null;
			Keep(speed, cadence, power, heartRate, level, distance, elapsed, false, (flags & 0x0001) == 0);
		}

		void TakeTreadmill(byte[] value)
		{
			var fields = new Fields(value);
			var read = fields.UInt16();
			if (!read.HasValue)
				return;
			var flags = read.Value;
			var speed = (flags & 0x0001) == 0 ? fields.UInt16() : null;
			if ((flags & 0x0002) != 0)
				fields.Skip(2); // Average speed
			var distance = (flags & 0x0004) != 0 ? fields.UInt24() : null;
			if ((flags & 0x0008) != 0)
				fields.Skip(4); // Incline and ramp angle
			if ((flags & 0x0010) != 0)
				fields.Skip(4); // Elevation gain and loss
			if ((flags & 0x0020) != 0)
				fields.Skip(1); // Pace
			if ((flags & 0x0040) != 0)
				fields.Skip(1); // Average pace
			if ((flags & 0x0080) != 0)
				fields.Skip(5); // Energy
			var heartRate = (flags & 0x0100) != 0 ? fields.UInt8() : null;
			if ((flags & 0x0200) != 0)
				fields.Skip(1); // Metabolic equivalent
			var elapsed = (flags & 0x0400) != 0 ? fields.UInt16() : null;
			if ((flags & 0x0800) != 0)
				fields.Skip(2); // Remaining time
			int? power = null;
			if ((flags & 0x1000) != 0)
			{
				fields.Skip(2); // Force on belt
				power = fields.SInt16();
				// 0x7FFF: not available.
				if (power == short.MaxValue)
					power = null;
			}
			Keep(speed, null, power, heartRate, null, distance, elapsed, false, (flags & 0x0001) == 0);
		}

		void TakeCrossTrainer(byte[] value)
		{
			var fields = new Fields(value);
			var read = fields.UInt24();
			if (!read.HasValue)
				return;
			var flags = read.Value;
			var speed = (flags & 0x0001) == 0 ? fields.UInt16() : null;
			if ((flags & 0x0002) != 0)
				fields.Skip(2); // Average speed
			var distance = (flags & 0x0004) != 0 ? fields.UInt24() : null;
			// Steps a minute, each half a turn, so they count as half turns a minute.
			int? cadence = null;
			if ((flags & 0x0008) != 0)
			{
				cadence = fields.UInt16();
				fields.Skip(2); // Average step rate
			}
			if ((flags & 0x0010) != 0)
				fields.Skip(2); // Strides
			if ((flags & 0x0020) != 0)
				fields.Skip(4); // Elevation gain and loss
			if ((flags & 0x0040) != 0)
				fields.Skip(4); // Incline and ramp angle
			var level = (flags & 0x0080) != 0 ? fields.SInt16() : null;
			var power = (flags & 0x0100) != 0 ? fields.SInt16() : null;
			if ((flags & 0x0200) != 0)
				fields.Skip(2); // Average power
			if ((flags & 0x0400) != 0)
				fields.Skip(5); // Energy
			var heartRate = (flags & 0x0800) != 0 ? fields.UInt8() : null;
			if ((flags & 0x1000) != 0)
				fields.Skip(1); // Metabolic equivalent
			var elapsed = (flags & 0x2000) != 0 ? fields.UInt16() : null;
			Keep(speed, cadence, power, heartRate, level, distance, elapsed, (flags & 0x8000) != 0, (flags & 0x0001) == 0);
		}

		void TakeRower(byte[] value)
		{
			var fields = new Fields(value);
			var read = fields.UInt16();
			if (!read.HasValue)
				return;
			var flags = read.Value;
			// Half strokes a minute, the same count as half turns.
			int? cadence = null;
			if ((flags & 0x0001) == 0)
			{
				cadence = fields.UInt8();
				fields.Skip(2); // Stroke count
			}
			if ((flags & 0x0002) != 0)
				fields.Skip(1); // Average stroke rate
			var distance = (flags & 0x0004) != 0 ? fields.UInt24() : null;
			// A rower sends its pace, the seconds each 500 m takes, in place of a speed: 1800 / pace km/h.
			int? speed = null;
			if ((flags & 0x0008) != 0)
			{
				var pace = fields.UInt16();
				if (pace.HasValue)
					speed = pace.Value > 0 ? (int)(1800 * Ftms.SpeedScale) / pace.Value : 0;
			}
			if ((flags & 0x0010) != 0)
				fields.Skip(2); // Average pace
			var power = (flags & 0x0020) != 0 ? fields.SInt16() : null;
			if ((flags & 0x0040) != 0)
				fields.Skip(2); // Average power
			var level = (flags & 0x0080) != 0 ? fields.SInt16() : null;
			if ((flags & 0x0100) != 0)
				fields.Skip(5); // Energy
			var heartRate = (flags & 0x0200) != 0 ? fields.UInt8() : null;
			if ((flags & 0x0400) != 0)
				fields.Skip(1); // Metabolic equivalent
			var elapsed = (flags & 0x0800) != 0 ? fields.UInt16() : null;
			Keep(speed, cadence, power, heartRate, level, distance, elapsed, false, (flags & 0x0001) == 0);
		}

		/// <summary>Keeps the values one packet held, a value it did not hold keeping what it was, and shows each on the tab.</summary>
		/// <param name="complete">True for the last packet of a set of values, which carries no More Data flag.</param>
		void Keep(int? speed, int? cadence, int? power, int? heartRate, int? level, int? distance, int? elapsed, bool backward, bool complete)
		{
			if (speed.HasValue)
				Speed = speed.Value;
			if (cadence.HasValue)
				Cadence = cadence.Value;
			if (power.HasValue)
				Power = power.Value;
			if (heartRate.HasValue)
				HeartRate = heartRate.Value;
			if (distance.HasValue)
				Distance = distance.Value;
			if (elapsed.HasValue)
				Elapsed = elapsed.Value;
			Backward = backward;
			var now = Now;
			if (level.HasValue)
			{
				Level = level.Value;
				// The level the person set: learned while no target of x360ce's holds the machine, and not from the packets
				// just after a command, which can still show the level before it.
				if (SentKind == FitnessForce.Off && unchecked(now - CommandAt) >= QuietMilliseconds)
					Base = level.Value;
			}
			_dataAt = now;
			_hasData = true;
			Interlocked.Increment(ref _received);
			var at = Milliseconds;
			lock (_lock)
			{
				// In the order the Indoor Bike Data value carries them.
				if (speed.HasValue)
					Reading("Speed", Text(speed.Value, Ftms.SpeedScale, "0.00", " km/h"), at);
				if (cadence.HasValue)
					Reading("Cadence", Text(cadence.Value, Ftms.CadenceScale, "0.#", " rpm"), at);
				if (distance.HasValue)
					Reading("Distance", Text(distance.Value, 1, "0", " m"), at);
				if (level.HasValue)
					Reading("Resistance", Text(level.Value, 1, "0", ""), at);
				if (power.HasValue)
					Reading("Power", Text(power.Value, 1, "0", " W"), at);
				if (heartRate.HasValue)
					Reading("Heart rate", Text(heartRate.Value, 1, "0", " bpm"), at);
				if (elapsed.HasValue)
					Reading("Elapsed time", TimeSpan.FromSeconds(elapsed.Value).ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture), at);
				if (complete)
					_sets.Add(at);
			}
			Changed();
		}

		static string Text(int value, double scale, string format, string unit)
		{
			return (value / scale).ToString(format, CultureInfo.InvariantCulture) + unit;
		}

		/// <summary>True while the last packet is newer than <see cref="StaleMilliseconds"/>.</summary>
		internal bool Fresh(int now)
		{
			return _hasData && unchecked(now - _dataAt) <= StaleMilliseconds;
		}

		/// <summary>Counts the packets of the last second, once a second: called by each read, on the input thread.</summary>
		internal void Count(int now)
		{
			if (unchecked(now - _countedAt) < 1000)
				return;
			_updates = Interlocked.Exchange(ref _received, 0);
			_countedAt = now;
		}

		/// <summary>A value as a device's status shows it, or empty while the machine has not sent it, or not for 3 seconds.</summary>
		internal string Shown(int value, double scale, string format, string unit)
		{
			return Fresh(Now) && value != Unknown ? Text(value, scale, format, unit) : "";
		}

		/// <summary>Reads the little-endian fields of a value one after another; a field past its end reads as null, and so does every field after it.</summary>
		sealed class Fields
		{
			public Fields(byte[] data)
			{
				_data = data;
			}

			readonly byte[] _data;
			int _offset;
			bool _ended;

			public int? UInt8() { return Next(1) ? _data[_offset - 1] : (int?)null; }

			public int? UInt16() { return Next(2) ? _data[_offset - 2] | _data[_offset - 1] << 8 : (int?)null; }

			public int? SInt16() { return Next(2) ? (short)(_data[_offset - 2] | _data[_offset - 1] << 8) : (int?)null; }

			public int? UInt24() { return Next(3) ? _data[_offset - 3] | _data[_offset - 2] << 8 | _data[_offset - 1] << 16 : (int?)null; }

			/// <summary>Passes over a field the session does not use.</summary>
			public void Skip(int count)
			{
				Next(count);
			}

			bool Next(int count)
			{
				if (_ended || _offset + count > _data.Length)
				{
					_ended = true;
					return false;
				}
				_offset += count;
				return true;
			}
		}

		#endregion

		#region Targets and commands

		/// <summary>The kind of target x360ce set on the machine and has not given back; Off when the machine holds the level the person set.</summary>
		internal volatile FitnessForce SentKind;

		/// <summary>The target last sent, in the units it was sent in: tenths of a level, or hundredths of a percent.</summary>
		internal volatile int SentValue;

		/// <summary>True once the machine was asked for control on this connection.</summary>
		internal volatile bool ControlRequested;

		/// <summary>When the last command was sent, by <see cref="Now"/>.</summary>
		internal volatile int CommandAt = -QuietMilliseconds;

		/// <summary>A bit for each kind of target the machine refused on this connection, by <see cref="FitnessForce"/>.</summary>
		internal volatile int Refused;

		/// <summary>Why the machine takes no target, when it said so; null while it has not.</summary>
		internal volatile string ForceNote;

		/// <summary>The answer the worker waits for, which the control point's indication brings; null while it waits for none.</summary>
		volatile TaskCompletionSource<byte[]> _answer;

		/// <summary>Where a target of this kind rests: the level the person set, or a flat road.</summary>
		internal int Rest(FitnessForce kind)
		{
			if (kind != FitnessForce.Resistance)
				return 0;
			var level = Base;
			if (level != Unknown)
				return level * (int)Ftms.ResistanceScale;
			var min = RangeMin;
			return min != Unknown ? min : 0;
		}

		/// <summary>The command that gives the machine back the level the person set, or a flat road, while x360ce holds a target on it; null while it holds none.</summary>
		/// <remarks>Control is asked for again first when the machine took it back.</remarks>
		internal byte[] RestoreCommand()
		{
			var kind = SentKind;
			if (kind == FitnessForce.Off)
				return null;
			CommandAt = Now;
			if (!ControlRequested)
			{
				ControlRequested = true;
				return Ftms.RequestControl();
			}
			SentKind = FitnessForce.Off;
			return kind == FitnessForce.Resistance ? Ftms.Resistance(Rest(kind)) : Ftms.Grade(0);
		}

		/// <summary>Takes the machine's answer to a command the worker sent; null when it did not answer.</summary>
		internal void Answer(byte[] command, byte[] answer)
		{
			if (answer == null || answer.Length < 3 || answer[1] != command[0])
				return;
			var op = command[0];
			if (answer[2] == Ftms.SuccessResult)
			{
				// A reset leaves the machine taking commands from nobody until control is asked for again.
				if (op == Ftms.ResetOp)
					ControlRequested = false;
				return;
			}
			if (op == Ftms.RequestControlOp)
			{
				Refused = (1 << (int)FitnessForce.Resistance) | (1 << (int)FitnessForce.Grade);
				ForceNote = MachineName + " gives no control to x360ce: another app may hold it.";
			}
			else if (answer[2] == Ftms.ControlNotPermittedResult)
				ControlRequested = false;
			else if (answer[2] == Ftms.NotSupportedResult && (op == Ftms.SetTargetResistanceOp || op == Ftms.SetSimulationOp))
			{
				var kind = op == Ftms.SetTargetResistanceOp ? FitnessForce.Resistance : FitnessForce.Grade;
				Refused |= 1 << (int)kind;
				if (SentKind == kind)
					SentKind = FitnessForce.Off;
			}
		}

		/// <summary>A command of the tab waiting for the worker: its name, for the log, and its bytes.</summary>
		sealed class TabCommand
		{
			public TabCommand(byte[] bytes)
			{
				Bytes = bytes;
			}

			public readonly byte[] Bytes;
		}

		/// <summary>The tab's command the worker sends next; null while none waits.</summary>
		volatile TabCommand _command;

		/// <summary>True while a command of the tab waits or is sent: the tab sends one at a time.</summary>
		internal bool Busy { get { return _command != null; } }

		/// <summary>Sends the machine a command of the tab, asking for control first when it must, and logs its answer.</summary>
		internal void Send(FitnessCommand command, double value)
		{
			_command = new TabCommand(Ftms.Command(command, value));
			Changed();
			Wake();
		}

		/// <summary>True when the machine connected takes a target of this bit of its Feature value.</summary>
		internal bool Supports(int targetBit)
		{
			return Features != Unknown && (Targets & targetBit) != 0;
		}

		#endregion

		#region What the tab shows

		readonly object _lock = new object();

		/// <summary>Raised by every change the tab shows, so it draws only when something changed.</summary>
		internal int Version { get { return Volatile.Read(ref _version); } }
		int _version;

		void Changed()
		{
			Interlocked.Increment(ref _version);
		}

		/// <summary>The newest value of every field the machine sent, in the order the fields first came.</summary>
		readonly List<FitnessReading> _readings = new List<FitnessReading>();

		/// <summary>How often the machine sends a whole set of values.</summary>
		UpdateInterval _sets = new UpdateInterval();

		/// <summary>What happened, newest first.</summary>
		readonly List<string> _log = new List<string>();
		string _lastNote;

		/// <summary>The devices heard while scanning, in the order heard.</summary>
		readonly List<FitnessSeen> _machines = new List<FitnessSeen>();

		/// <summary>The addresses that said they are fitness machines: a machine often says so in another advertisement than the one with its name.</summary>
		readonly HashSet<ulong> _fitnessMachines = new HashSet<ulong>();

		/// <summary>Called with the lock held.</summary>
		void Reading(string name, string value, long at)
		{
			var row = _readings.FirstOrDefault(x => x.Name == name);
			if (row == null)
				_readings.Add(new FitnessReading(name, value, at));
			else
				row.Update(value, at);
		}

		/// <summary>The newest value of every field the machine sent, as the tab shows them, in the order the fields first came.</summary>
		internal FitnessReading[] Readings()
		{
			lock (_lock)
				return _readings.Select(x => x.Copy()).ToArray();
		}

		/// <summary>The activity log, newest first, each line "HH:mm:ss  text".</summary>
		internal string[] Log()
		{
			lock (_lock)
				return _log.ToArray();
		}

		/// <summary>Adds a line to the log. A machine can repeat one status every second, so a line the same as the last is left out.</summary>
		internal void Note(string text)
		{
			lock (_lock)
			{
				if (text == _lastNote)
					return;
				_lastNote = text;
				_log.Insert(0, DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "  " + text);
				if (_log.Count > LogLines)
					_log.RemoveAt(_log.Count - 1);
			}
			Changed();
		}

		/// <summary>The devices heard while scanning, in the order heard.</summary>
		internal FitnessSeen[] Machines()
		{
			lock (_lock)
				return _machines.ToArray();
		}

		/// <summary>Notes a device heard while scanning: a fitness machine, or with Show all Bluetooth devices any device that says its name.</summary>
		/// <param name="address">The device's Bluetooth address.</param>
		/// <param name="name">Its name, or empty when this advertisement has none.</param>
		/// <param name="fitness">True when this advertisement says it is a fitness machine.</param>
		/// <param name="types">The kinds of machine it says it is, or empty.</param>
		/// <param name="signal">Its signal strength, in dBm.</param>
		internal void Hear(ulong address, string name, bool fitness, string types, short signal)
		{
			lock (_lock)
			{
				if (fitness)
					_fitnessMachines.Add(address);
				else
					fitness = _fitnessMachines.Contains(address);
				if (!fitness && (!_showAll || name.Length == 0))
					return;
				var machine = _machines.FirstOrDefault(x => x.Address == address);
				if (machine == null)
				{
					machine = new FitnessSeen(address);
					_machines.Add(machine);
				}
				if (name.Length > 0)
					machine.Name = name;
				if (types.Length > 0)
					machine.Types = types;
				if (fitness)
					machine.Fitness = true;
				machine.Signal = signal;
			}
			Changed();
		}

		/// <summary>The name of the machine connected or connecting last.</summary>
		internal string MachineName { get { return _name ?? "The machine"; } }
		volatile string _name;

		/// <summary>What the machine measures and accepts, as the Feature value says; empty until read.</summary>
		internal string FeaturesText
		{
			get
			{
				var features = Features;
				return features == Unknown ? "" : Ftms.Describe(features, Targets);
			}
		}

		/// <summary>Where the session stands and, when it is stuck, what to do next, in one sentence or two.</summary>
		internal string StatusText
		{
			get
			{
				if (_tooOld)
					return "Needs Windows 10 or later.";
				var held = _held;
				if (held != null)
				{
					if (!_hasData)
						return "Connected to " + held.Name + ". Waiting for data.";
					string every;
					lock (_lock)
						every = _sets.ToString();
					return "Connected to " + held.Name + ". Receiving data" + (every.Length == 0 ? "." : " every " + every + ".");
				}
				var problem = _problem;
				if (problem != null)
					return problem;
				var want = _want;
				if (want != null)
					return want.Quiet ? "Looking for " + want.Name + "." : "Connecting to " + want.Name + "...";
				if (_scanning)
				{
					var count = Machines().Length;
					return "Scanning. " + (_showAll ? "Devices" : "Fitness machines") + " found: " + count + "."
						+ (count > 0 ? " Select one and press Connect." : "");
				}
				return IdleText;
			}
		}

		/// <summary>What the status says while nothing is asked for.</summary>
		internal const string IdleText = "Press Scan to look for fitness machines.";

		/// <summary>The last problem, shown until the next scan or connection; null while there is none.</summary>
		volatile string _problem;

		/// <summary>True when Windows has no Bluetooth types: the worker stopped.</summary>
		volatile bool _tooOld;

		#endregion

		#region What the tab and the devices ask for

		/// <summary>A machine to hold: its address and name, and whether it is tried again quietly after a failure.</summary>
		internal sealed class Want
		{
			public Want(ulong address, string name, bool quiet)
			{
				Address = address;
				Name = name;
				Quiet = quiet;
			}

			public readonly ulong Address;
			public readonly string Name;
			public readonly bool Quiet;
		}

		/// <summary>The machine asked for; null to hold none.</summary>
		volatile Want _want;

		/// <summary>The machine connected now; null while none is.</summary>
		volatile Want _held;

		/// <summary>The machine connected last in this run, or the one the tab reconnects to at start; null before either.</summary>
		volatile Want _last;

		volatile bool _scanning;
		volatile bool _showAll;

		/// <summary>True while x360ce closes: the worker lets go of the machine and ends.</summary>
		volatile bool _exiting;

		/// <summary>The machine connected now; null while none is.</summary>
		internal Want Held { get { return _held; } }

		/// <summary>The machine connected or being connected; null while none is asked for.</summary>
		internal Want Wanted { get { return _want; } }

		/// <summary>The machine connected last in this run; null before one connected.</summary>
		internal Want Last { get { return _last; } }

		internal bool Scanning { get { return _scanning; } }

		internal bool ShowAll { get { return _showAll; } }

		/// <summary>Starts looking for machines, with a new list: fitness machines, and with <paramref name="showAll"/> every device that says its name.</summary>
		internal void Scan(bool showAll)
		{
			lock (_lock)
			{
				_machines.Clear();
				_showAll = showAll;
			}
			_problem = null;
			_scanning = true;
			Note(showAll ? "Scanning for all Bluetooth devices." : "Scanning for fitness machines.");
			Wake();
		}

		internal void StopScan()
		{
			_scanning = false;
			Changed();
		}

		/// <summary>Connects to a machine heard, as the person asked: once, saying why when it cannot.</summary>
		internal void Connect(ulong address, string name)
		{
			_scanning = false;
			Hold(new Want(address, name, false));
		}

		/// <summary>Connects to the machine connected last, as the tab does at start: quietly, trying again until it connects or the person presses Disconnect.</summary>
		internal void Reconnect(ulong address, string name)
		{
			var want = new Want(address, name, true);
			_last = want;
			Hold(want);
		}

		/// <summary>Lets go of the machine, and stops trying to connect.</summary>
		internal void Disconnect()
		{
			_want = null;
			Changed();
		}

		void Hold(Want want)
		{
			_problem = null;
			_want = want;
			Changed();
			Wake();
		}

		/// <summary>Devices being read.</summary>
		int _readers;

		/// <summary>The device read last, whose game's force drives the machine.</summary>
		volatile FitnessMachine _driver;

		/// <summary>A device starts being read: it reads what the machine sends, and drives it with its game's force. When nothing is held, connects again to the machine connected last.</summary>
		/// <remarks>Called on the input thread, once for each device: it takes no lock and waits for nothing.</remarks>
		internal void Join(FitnessMachine device)
		{
			_driver = device;
			if (Interlocked.Increment(ref _readers) != 1 || _want != null)
				return;
			var last = _last;
			if (last != null)
				Hold(new Want(last.Address, last.Name, true));
		}

		/// <summary>A device stops being read. The last one lets go of the machine, as Disconnect does.</summary>
		/// <remarks>Called on the input thread: it takes no lock and waits for nothing; the worker lets go of the machine.</remarks>
		internal void Leave(FitnessMachine device)
		{
			if (_driver == device)
				_driver = null;
			if (Interlocked.Decrement(ref _readers) == 0)
				Disconnect();
		}

		/// <summary>Raised on a thread of Windows' when a machine asks to be paired; the tab asks the person and answers. Declined when nobody answers.</summary>
		internal event Action<FitnessPairing> PairingRequested;

		void AskPairing(FitnessPairing pairing)
		{
			var ask = PairingRequested;
			if (ask == null)
				pairing.Accept(false);
			else
				ask(pairing);
		}

		#endregion

		#region The worker

		/// <summary>The thread that works Bluetooth, from when it is first needed until x360ce closes.</summary>
		Thread _worker;

		/// <summary>Set once the worker ended.</summary>
		readonly ManualResetEventSlim _ended = new ManualResetEventSlim();

		/// <summary>When the game's force was last looked at, by <see cref="Now"/>.</summary>
		int _forceAt;

		/// <summary>Starts the worker, the first time only, without a lock: the input thread may call it.</summary>
		void Wake()
		{
			if (Volatile.Read(ref _worker) != null)
				return;
			var worker = new Thread(Work) { IsBackground = true, Name = "Fitness Machine" };
			if (Interlocked.CompareExchange(ref _worker, worker, null) != null)
				return;
			AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
			worker.Start();
		}

		/// <summary>Lets go of the machine as x360ce closes, waiting for it a moment at most: x360ce ends its process without waiting for threads.</summary>
		void OnProcessExit(object sender, EventArgs e)
		{
			_exiting = true;
			_ended.Wait(ExitMilliseconds);
		}

		void Work()
		{
			// An exception that left this thread would close x360ce, so each one becomes the status.
			try
			{
				Run().Wait();
			}
			catch (Exception ex)
			{
				_problem = Problem(ex);
				Changed();
			}
			finally
			{
				_ended.Set();
			}
		}

		/// <summary>Scans, connects, sends commands and lets go, as asked, until x360ce closes.</summary>
		async Task Run()
		{
			Scanner scanner = null;
			Link link = null;
			Want trying = null;
			var failures = 0;
			var retryAt = Now;
			while (true)
			{
				try
				{
					var want = _exiting ? null : _want;
					// Scanning stops while a machine is connected or connecting.
					var scan = _scanning && want == null && link == null && !_exiting;
					if (scan && scanner == null)
					{
						scanner = new Scanner(this);
						scanner.Start();
					}
					else if (!scan && scanner != null)
					{
						scanner.Stop();
						scanner = null;
					}
					if (link != null)
					{
						if (want == link.Want && link.Up)
						{
							await Serve(link);
							continue;
						}
						// Let go of: Disconnect, the last device read let go of, another machine asked for, x360ce closing, or the link lost.
						var lost = want == link.Want;
						var name = link.Want.Name;
						_held = null;
						CanCommand = false;
						await link.Close();
						link = null;
						if (lost)
						{
							// Connected again quietly, until it connects or the person presses Disconnect.
							if (_want == want)
								_want = new Want(want.Address, want.Name, true);
							_problem = "Lost " + name + "; connecting again.";
							Note("Link to " + name + " was lost.");
							failures = 0;
							retryAt = unchecked(Now + BackOffMilliseconds[0]);
						}
						else if (!_exiting)
							Note("Disconnected.");
						Changed();
						continue;
					}
					if (_exiting)
					{
						if (scanner != null)
							scanner.Stop();
						return;
					}
					if (want != null && unchecked(Now - retryAt) >= 0)
					{
						Forget();
						_name = want.Name;
						Changed();
						trying = want;
						link = new Link(this, want);
						var problem = await link.Open();
						if (problem == null && want == _want)
						{
							_problem = null;
							_held = want;
							_last = want;
							failures = 0;
							Note("Connected to " + want.Name + ".");
							continue;
						}
						await link.Close();
						link = null;
						if (problem == null)
							continue;
						Note(problem);
						if (want.Quiet)
							retryAt = unchecked(Now + BackOffMilliseconds[Math.Min(failures++, BackOffMilliseconds.Length - 1)]);
						else if (_want == want)
							_want = null;
						_problem = problem;
						Changed();
						continue;
					}
					await Task.Delay(TickMilliseconds);
				}
				catch (Exception ex)
				{
					_problem = Problem(ex);
					if (TooOld(ex))
					{
						_tooOld = true;
						Changed();
						return;
					}
					Note(_problem);
					// A machine held is connected again quietly, as when its link is lost. A connection the person asked for
					// that failed is not tried again; one asked for quietly is, a little later.
					if (trying != null && _want == trying)
						_want = _held != null ? new Want(trying.Address, trying.Name, true) : trying.Quiet ? trying : null;
					_held = null;
					CanCommand = false;
					if (link != null)
					{
						link.Dispose();
						link = null;
					}
					if (scanner != null)
					{
						scanner.Stop();
						scanner = null;
					}
					retryAt = unchecked(Now + BackOffMilliseconds[Math.Min(failures++, BackOffMilliseconds.Length - 1)]);
					Changed();
				}
			}
		}

		/// <summary>Sends the machine one command: the tab's first, else the game's force at most four times a second; else waits a moment.</summary>
		async Task Serve(Link link)
		{
			var command = _command;
			if (command != null)
			{
				if (!ControlRequested)
				{
					ControlRequested = true;
					var request = Ftms.RequestControl();
					var granted = await link.Send(request);
					Answer(request, granted);
					Note(granted == null ? MachineName + " did not answer the request for control." : Ftms.Answer(granted) + ".");
					if (granted == null || granted[2] != Ftms.SuccessResult)
					{
						_command = null;
						Changed();
						return;
					}
				}
				var answer = await link.Send(command.Bytes);
				Answer(command.Bytes, answer);
				Note(answer == null ? MachineName + " did not answer the command." : Ftms.Answer(answer) + ".");
				_command = null;
				Changed();
				return;
			}
			var driver = _driver;
			if (driver != null && unchecked(Now - _forceAt) >= CommandMilliseconds)
			{
				_forceAt = Now;
				var next = driver.NextCommand();
				if (next != null)
				{
					Answer(next, await link.Send(next));
					return;
				}
			}
			await Task.Delay(TickMilliseconds);
		}

		/// <summary>Forgets what one connection learned, so the next machine starts from nothing.</summary>
		void Forget()
		{
			Speed = Unknown;
			Cadence = Unknown;
			Power = Unknown;
			HeartRate = Unknown;
			Level = Unknown;
			Distance = Unknown;
			Elapsed = Unknown;
			Backward = false;
			_hasData = false;
			Features = Unknown;
			Targets = 0;
			RangeMin = Unknown;
			RangeMax = Unknown;
			PowerMin = Unknown;
			PowerMax = Unknown;
			Base = Unknown;
			SentKind = FitnessForce.Off;
			ControlRequested = false;
			Refused = 0;
			ForceNote = null;
			_answer = null;
			_command = null;
			CommandAt = unchecked(Now - QuietMilliseconds);
			lock (_lock)
			{
				_readings.Clear();
				_sets = new UpdateInterval();
			}
		}

		static Exception Inner(Exception ex)
		{
			while ((ex is AggregateException || ex is TargetInvocationException || ex is TypeInitializationException) && ex.InnerException != null)
				ex = ex.InnerException;
			return ex;
		}

		/// <summary>True for the exception a Windows without the Bluetooth types throws when they are first used.</summary>
		static bool TooOld(Exception ex)
		{
			ex = Inner(ex);
			return ex is TypeLoadException || ex is FileNotFoundException;
		}

		static string Problem(Exception ex)
		{
			return TooOld(ex) ? "Needs Windows 10 or later." : "Bluetooth stopped with an error, so it is tried again: " + Inner(ex).Message;
		}

		#endregion

		#region Bluetooth

		/// <summary>Listens for advertisements while the tab scans, on the worker: one of the two places Windows' Bluetooth types are named.</summary>
		sealed class Scanner
		{
			public Scanner(FitnessSession owner)
			{
				_owner = owner;
			}

			readonly FitnessSession _owner;
			BluetoothLEAdvertisementWatcher _watcher;
			static readonly Guid ServiceUuid = BluetoothUuidHelper.FromShortId(Ftms.ServiceId);

			public void Start()
			{
				_watcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };
				_watcher.Received += OnReceived;
				_watcher.Stopped += OnStopped;
				_watcher.Start();
			}

			public void Stop()
			{
				_watcher.Received -= OnReceived;
				_watcher.Stopped -= OnStopped;
				_watcher.Stop();
			}

			void OnReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
			{
				var advertisement = args.Advertisement;
				var serviceData = FitnessServiceData(advertisement);
				var fitness = serviceData != null || advertisement.ServiceUuids.Contains(ServiceUuid);
				_owner.Hear(args.BluetoothAddress, advertisement.LocalName ?? "", fitness,
					serviceData == null ? "" : Ftms.MachineTypes(serviceData), args.RawSignalStrengthInDBm);
			}

			/// <summary>Windows stopped the scan by itself, as when Bluetooth is off; a scan stopped by the session says nothing.</summary>
			void OnStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
			{
				if (args.Error == BluetoothError.Success)
					return;
				var problem = args.Error == BluetoothError.RadioNotAvailable ? "Bluetooth is off. Turn it on in Windows Settings, then press Scan."
					: args.Error == BluetoothError.DisabledByPolicy ? "Scanning is blocked by a Bluetooth policy on this computer."
					: "Scanning stopped (" + args.Error + "). Press Scan to try again.";
				_owner._scanning = false;
				_owner._problem = problem;
				_owner.Note(problem);
			}

			/// <summary>The fitness service data of an advertisement after its service ID, from a section 0x16 that starts with 0x1826; null when it has none.</summary>
			static byte[] FitnessServiceData(BluetoothLEAdvertisement advertisement)
			{
				foreach (var section in advertisement.DataSections)
				{
					if (section.DataType != 0x16 || section.Data.Length < 2)
						continue;
					var bytes = Link.Bytes(section.Data);
					if ((bytes[0] | bytes[1] << 8) == Ftms.ServiceId)
						return bytes.Skip(2).ToArray();
				}
				return null;
			}
		}

		/// <summary>One connection to a machine, made and let go of on the worker: the other place Windows' Bluetooth types are named.</summary>
		sealed class Link
		{
			public Link(FitnessSession owner, Want want)
			{
				_owner = owner;
				Want = want;
			}

			/// <summary>The machine the link is for.</summary>
			public readonly Want Want;

			/// <summary>The values read once after connecting.</summary>
			static readonly ushort[] Described = { Ftms.FeatureId, Ftms.ResistanceRangeId, Ftms.PowerRangeId, Ftms.TrainingStatusId };

			/// <summary>The values the machine is asked to send: its data, its training status, its status, and the answers of its control point.</summary>
			static readonly ushort[] Notified =
			{
				Ftms.IndoorBikeDataId, Ftms.TreadmillDataId, Ftms.CrossTrainerDataId, Ftms.RowerDataId, Ftms.TrainingStatusId, Ftms.StatusId, Ftms.ControlPointId,
			};

			readonly FitnessSession _owner;
			readonly Dictionary<ushort, GattCharacteristic> _characteristics = new Dictionary<ushort, GattCharacteristic>();

			/// <summary>The characteristics listened to.</summary>
			readonly List<GattCharacteristic> _heard = new List<GattCharacteristic>();

			/// <summary>The characteristics that send their values, told to stop when the session lets go.</summary>
			readonly List<GattCharacteristic> _subscribed = new List<GattCharacteristic>();

			BluetoothLEDevice _device;
			GattSession _session;
			GattDeviceService _service;
			GattCharacteristic _controlPoint;
			volatile bool _lost;

			/// <summary>True while the machine is connected.</summary>
			public bool Up { get { return _device != null && !_lost; } }

			/// <summary>Connects, reads what the machine is and asks for its values, pairing when it asks for that; null, or why it cannot.</summary>
			public async Task<string> Open()
			{
				var name = Want.Name;
				_device = await Wait(BluetoothLEDevice.FromBluetoothAddressAsync(Want.Address));
				if (_device == null)
					return name + " could not be reached. Make sure it is on and nearby, then try again.";
				_device.ConnectionStatusChanged += OnConnectionStatusChanged;
				_session = await Wait(GattSession.FromDeviceIdAsync(_device.BluetoothDeviceId));
				_session.MaintainConnection = true;
				var services = await Wait(_device.GetGattServicesForUuidAsync(BluetoothUuidHelper.FromShortId(Ftms.ServiceId), BluetoothCacheMode.Uncached));
				if (services.Status == GattCommunicationStatus.AccessDenied)
				{
					var refused = await Pair(name);
					if (refused != null)
						return refused;
					services = await Wait(_device.GetGattServicesForUuidAsync(BluetoothUuidHelper.FromShortId(Ftms.ServiceId), BluetoothCacheMode.Uncached));
				}
				if (services.Status != GattCommunicationStatus.Success)
					return name + " did not answer (" + services.Status + "). Make sure no other app is connected to it, then try again.";
				if (services.Services.Count == 0)
					return name + " has no standard Fitness Machine Service, so x360ce cannot read it.";
				_service = services.Services[0];
				var found = await Wait(_service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached));
				if (found.Status != GattCommunicationStatus.Success)
					return name + " did not list its values (" + found.Status + ").";
				foreach (var characteristic in found.Characteristics)
				{
					var id = BluetoothUuidHelper.TryGetShortId(characteristic.Uuid);
					if (id.HasValue)
						_characteristics[(ushort)id.Value] = characteristic;
				}
				foreach (var id in Notified)
				{
					GattCharacteristic characteristic;
					if (!_characteristics.TryGetValue(id, out characteristic))
						continue;
					characteristic.ValueChanged += OnValueChanged;
					_heard.Add(characteristic);
				}
				_characteristics.TryGetValue(Ftms.ControlPointId, out _controlPoint);
				if (await NeedsPairingToSubscribe())
				{
					var refused = await Pair(name);
					if (refused != null)
						return refused;
					if (await NeedsPairingToSubscribe())
						return name + " still refused access after pairing.";
				}
				// A machine with no control point takes no command, whatever its Feature value says.
				if (_controlPoint == null)
					_owner.Targets = 0;
				_owner.CanCommand = _controlPoint != null;
				return null;
			}

			/// <summary>Reads what the machine is and asks it for its values; true when it asks to be paired first.</summary>
			async Task<bool> NeedsPairingToSubscribe()
			{
				_subscribed.Clear();
				foreach (var id in Described)
				{
					GattCharacteristic characteristic;
					if (!_characteristics.TryGetValue(id, out characteristic))
						continue;
					var read = await Wait(characteristic.ReadValueAsync(BluetoothCacheMode.Uncached));
					if (NeedsPairing(read.Status, read.ProtocolError))
						return true;
					if (read.Status == GattCommunicationStatus.Success)
						_owner.Take(id, Bytes(read.Value));
				}
				foreach (var characteristic in _heard)
				{
					var kind = characteristic == _controlPoint
						? GattClientCharacteristicConfigurationDescriptorValue.Indicate
						: GattClientCharacteristicConfigurationDescriptorValue.Notify;
					var written = await Wait(characteristic.WriteClientCharacteristicConfigurationDescriptorWithResultAsync(kind));
					if (NeedsPairing(written.Status, written.ProtocolError))
						return true;
					// A value the machine will not send is left out; the others still come.
					if (written.Status == GattCommunicationStatus.Success)
						_subscribed.Add(characteristic);
				}
				return false;
			}

			/// <summary>
			/// Pairs with the machine, at least with encryption, which FTMS 1.0.1 asks for commands. Windows pairs as strongly
			/// as the machine can, so the person compares a number on both whenever the machine can show one: the tab asks them.
			/// </summary>
			/// <returns>Null once paired; else why not.</returns>
			async Task<string> Pair(string name)
			{
				var pairing = _device.DeviceInformation.Pairing;
				if (pairing.IsPaired)
					return name + " refused access although it is paired. Remove it in Windows Settings under Bluetooth & devices, then connect again.";
				if (!pairing.CanPair)
					return name + " asks to be paired, but Windows cannot pair with it.";
				_owner.Note(name + " asks to be paired.");
				var custom = pairing.Custom;
				custom.PairingRequested += OnPairingRequested;
				try
				{
					var result = await Wait(custom.PairAsync(DevicePairingKinds.ConfirmOnly | DevicePairingKinds.ConfirmPinMatch | DevicePairingKinds.DisplayPin,
						DevicePairingProtectionLevel.Encryption));
					if (result.Status != DevicePairingResultStatus.Paired && result.Status != DevicePairingResultStatus.AlreadyPaired)
						return "Pairing with " + name + " did not complete (" + result.Status + ").";
					_owner.Note("Paired with " + name + ".");
					return null;
				}
				finally
				{
					custom.PairingRequested -= OnPairingRequested;
				}
			}

			/// <summary>Asks the person through the tab, and accepts once they say yes; Windows waits for the answer.</summary>
			void OnPairingRequested(DeviceInformationCustomPairing sender, DevicePairingRequestedEventArgs args)
			{
				var kind = args.PairingKind == DevicePairingKinds.ConfirmPinMatch ? FitnessPairingKind.ConfirmPinMatch
					: args.PairingKind == DevicePairingKinds.DisplayPin ? FitnessPairingKind.DisplayPin
					: FitnessPairingKind.ConfirmOnly;
				var pairing = new FitnessPairing(Want.Name, kind, args.Pin ?? "");
				var deferral = args.GetDeferral();
				pairing.Answer.ContinueWith(answer =>
				{
					if (answer.Result)
						args.Accept();
					deferral.Complete();
				});
				_owner.AskPairing(pairing);
			}

			static bool NeedsPairing(GattCommunicationStatus status, byte? error)
			{
				return status == GattCommunicationStatus.AccessDenied
					|| status == GattCommunicationStatus.ProtocolError && error.HasValue
					&& (error.Value == GattProtocolError.InsufficientAuthentication
						|| error.Value == GattProtocolError.InsufficientEncryption
						|| error.Value == GattProtocolError.InsufficientEncryptionKeySize
						|| error.Value == GattProtocolError.InsufficientAuthorization);
			}

			/// <summary>Writes one command, and waits two seconds at most for the machine's answer; null when there is none.</summary>
			public async Task<byte[]> Send(byte[] command)
			{
				if (_controlPoint == null)
					return null;
				var answer = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
				_owner._answer = answer;
				try
				{
					var written = await Wait(_controlPoint.WriteValueWithResultAsync(Buffer(command), GattWriteOption.WriteWithResponse));
					if (written.Status != GattCommunicationStatus.Success)
						return null;
					return await Task.WhenAny(answer.Task, Task.Delay(AnswerMilliseconds)) == answer.Task ? answer.Task.Result : null;
				}
				finally
				{
					_owner._answer = null;
				}
			}

			/// <summary>
			/// Lets go of the machine: gives it back the level the person set, tells it to stop sending, and lets go of every
			/// Bluetooth object. Windows closes the link once nothing holds it.
			/// </summary>
			public async Task Close()
			{
				try
				{
					if (Up)
					{
						// Control may have to be asked for again first.
						for (var i = 0; i < 2; i++)
						{
							var command = _owner.RestoreCommand();
							if (command == null)
								break;
							_owner.Answer(command, await Send(command));
						}
						if (_subscribed.Count > 0)
						{
							var stops = _subscribed
								.Select(x => Wait(x.WriteClientCharacteristicConfigurationDescriptorWithResultAsync(GattClientCharacteristicConfigurationDescriptorValue.None)))
								.ToArray();
							var all = Task.WhenAll(stops);
							var confirmed = await Task.WhenAny(all, Task.Delay(AnswerMilliseconds)) == all
								&& stops.All(x => x.Status == TaskStatus.RanToCompletion && x.Result.Status == GattCommunicationStatus.Success);
							_owner.Note(confirmed ? Want.Name + " stopped sending values to x360ce." : Want.Name + " did not confirm that it stopped sending values.");
						}
					}
				}
				finally
				{
					Dispose();
				}
			}

			/// <summary>Lets go of every Bluetooth object at once, as after a failure.</summary>
			public void Dispose()
			{
				foreach (var characteristic in _heard)
					characteristic.ValueChanged -= OnValueChanged;
				_heard.Clear();
				if (_service != null)
					_service.Dispose();
				_service = null;
				if (_session != null)
				{
					_session.MaintainConnection = false;
					_session.Dispose();
				}
				_session = null;
				if (_device != null)
				{
					_device.ConnectionStatusChanged -= OnConnectionStatusChanged;
					_device.Dispose();
				}
				_device = null;
			}

			void OnValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
			{
				var id = BluetoothUuidHelper.TryGetShortId(sender.Uuid);
				if (id.HasValue)
					_owner.Take((ushort)id.Value, Bytes(args.CharacteristicValue));
			}

			void OnConnectionStatusChanged(BluetoothLEDevice sender, object args)
			{
				if (sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
					_lost = true;
				else
					_owner.Note("Link to " + Want.Name + " is up.");
			}

			/// <summary>A Windows operation as a task, which continues on another thread than the one Windows completes it on.</summary>
			static Task<T> Wait<T>(Windows.Foundation.IAsyncOperation<T> operation)
			{
				var done = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
				operation.Completed = (o, status) =>
				{
					if (status == Windows.Foundation.AsyncStatus.Completed)
						done.TrySetResult(o.GetResults());
					else if (status == Windows.Foundation.AsyncStatus.Error)
						done.TrySetException(o.ErrorCode);
					else
						done.TrySetCanceled();
				};
				return done.Task;
			}

			public static byte[] Bytes(IBuffer buffer)
			{
				var bytes = new byte[buffer.Length];
				using (var reader = DataReader.FromBuffer(buffer))
					reader.ReadBytes(bytes);
				return bytes;
			}

			static IBuffer Buffer(byte[] bytes)
			{
				using (var writer = new DataWriter())
				{
					writer.WriteBytes(bytes);
					return writer.DetachBuffer();
				}
			}
		}

		#endregion

		/// <summary>A Bluetooth address as six bytes in hexadecimal, separated by colons.</summary>
		internal static string AddressText(ulong address)
		{
			var hex = address.ToString("X12", CultureInfo.InvariantCulture);
			return string.Join(":", Enumerable.Range(0, 6).Select(i => hex.Substring(i * 2, 2)));
		}

		/// <summary>Reads an address written by <see cref="AddressText"/>; 0 when the text holds none.</summary>
		internal static ulong ParseAddress(string text)
		{
			ulong address;
			return ulong.TryParse((text ?? "").Replace(":", ""), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out address) ? address : 0;
		}
	}

	/// <summary>A device heard while scanning, kept up to date by its later advertisements: a row of the Fitness tab's list.</summary>
	sealed class FitnessSeen
	{
		public FitnessSeen(ulong address)
		{
			Address = address;
		}

		public readonly ulong Address;
		public volatile string Name = "";
		public volatile string Types = "";
		public volatile bool Fitness;
		public volatile int Signal;

		/// <summary>The name, or the address while the device has not said its name.</summary>
		public string DisplayName { get { var name = Name; return name.Length > 0 ? name : FitnessSession.AddressText(Address); } }

		/// <summary>What kind of device it is, as far as its advertisements say.</summary>
		public string Description
		{
			get
			{
				var types = Types;
				return types.Length > 0 ? types : Fitness ? "fitness machine" : "not advertising the fitness service";
			}
		}
	}

	/// <summary>One line of the values on the Fitness tab: its name, its newest value, and how often it changes.</summary>
	/// <remarks>
	/// How often a value changes tells one the machine measures all the time from one it works out only every few seconds.
	/// The same value sent again is no change.
	/// </remarks>
	sealed class FitnessReading
	{
		public FitnessReading(string name, string value, long at)
		{
			Name = name;
			Value = value;
			_changes.Add(at);
		}

		FitnessReading(FitnessReading row)
		{
			Name = row.Name;
			Value = row.Value;
			Interval = row.Interval;
		}

		public readonly string Name;
		public string Value { get; private set; }

		/// <summary>How often the value changes, such as "every 1.0 s", or empty until it has changed three times.</summary>
		public string Interval { get; private set; } = "";

		readonly UpdateInterval _changes = new UpdateInterval();

		public void Update(string value, long at)
		{
			if (value == Value)
				return;
			Value = value;
			_changes.Add(at);
			Interval = _changes.Seconds.HasValue ? "every " + _changes : "";
		}

		/// <summary>A copy to show, which the session's later changes leave as it is.</summary>
		public FitnessReading Copy()
		{
			return new FitnessReading(this);
		}
	}

	/// <summary>How often something happens, such as a set of values arriving or a value changing.</summary>
	/// <remarks>
	/// The middle (median) of the last five gaps, once there are three: one long gap, from a pause in pedalling, or one
	/// short one, from packets late and then delivered together, does not move it, as it would move an average.
	/// </remarks>
	sealed class UpdateInterval
	{
		const int Kept = 5;
		const int Needed = 3;

		readonly Queue<double> _seconds = new Queue<double>();
		long _last = -1;

		/// <summary>The median seconds between the last times, or null until there have been three gaps.</summary>
		public double? Seconds
		{
			get
			{
				if (_seconds.Count < Needed)
					return null;
				var sorted = _seconds.OrderBy(x => x).ToArray();
				return sorted[(sorted.Length - 1) / 2];
			}
		}

		/// <summary>Notes that it happened at a time, in milliseconds.</summary>
		public void Add(long milliseconds)
		{
			if (_last >= 0)
			{
				_seconds.Enqueue((milliseconds - _last) / 1000.0);
				if (_seconds.Count > Kept)
					_seconds.Dequeue();
			}
			_last = milliseconds;
		}

		/// <summary>The interval, such as "1.0 s", or empty until it is known.</summary>
		public override string ToString()
		{
			var seconds = Seconds;
			return seconds.HasValue ? seconds.Value.ToString(seconds.Value < 10 ? "0.0" : "0", CultureInfo.InvariantCulture) + " s" : "";
		}
	}

	/// <summary>How a machine asks to be paired.</summary>
	enum FitnessPairingKind
	{
		/// <summary>Accept or decline; no number shows.</summary>
		ConfirmOnly,
		/// <summary>Both show the same number, which the person compares.</summary>
		ConfirmPinMatch,
		/// <summary>The person types the number shown into the machine.</summary>
		DisplayPin,
	}

	/// <summary>A machine asking to be paired, as the Fitness tab asks the person: the question, and the answer once given.</summary>
	sealed class FitnessPairing
	{
		public FitnessPairing(string name, FitnessPairingKind kind, string pin)
		{
			Name = name;
			Kind = kind;
			Pin = pin;
		}

		public readonly string Name;
		public readonly FitnessPairingKind Kind;
		public readonly string Pin;
		readonly TaskCompletionSource<bool> _answer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		/// <summary>True once the person accepts, false once they decline.</summary>
		public Task<bool> Answer { get { return _answer.Task; } }

		public void Accept(bool accept)
		{
			_answer.TrySetResult(accept);
		}

		/// <summary>What the person is asked. With a number on both, a match proves it is the right machine.</summary>
		public string Question
		{
			get
			{
				switch (Kind)
				{
					case FitnessPairingKind.ConfirmPinMatch:
						return "Pair with " + Name + "?\n\nAccept only if " + Name + " shows the same number:\n\n" + Pin;
					case FitnessPairingKind.DisplayPin:
						return "Enter this number on " + Name + ":\n\n" + Pin + "\n\nPress OK once you have entered it, or Cancel to stop pairing.";
					default:
						return "Pair with " + Name + "?\n\nNo number is shown, so this cannot confirm that it is the right machine.";
				}
			}
		}
	}

	#endregion

	#region The Fitness tab

	/// <summary>
	/// The Fitness tab: finds fitness machines over Bluetooth, connects to one, shows what it sends and sends it commands,
	/// as the Virtual Fitness Machine Receiver does, and puts it on a controller as a Fitness Machine device.
	/// </summary>
	/// <remarks>
	/// Machines, commands and help on the left; the status, the values and the activity on the right. The two columns
	/// share their rows, so the list of machines and the box of values start and end level, and the log is as tall as the
	/// commands and help beside it. Each column is as wide as what is in it, the help and the status wrapping at a width
	/// in the font's units, and a wide window leaves the room beside them empty. Drawn in the system colours, so x360ce's
	/// theme applies, and laid out by tables and automatic sizes, with the room between controls taken from the font, so it
	/// follows the font and the zoom.
	///
	/// Every control is named, and the AI door reaches each: it reads the machines as the grid's rows and selects one by
	/// setting the grid to its index, and it reads each value by its name in the box of values.
	///
	/// The tab drives the session (<see cref="FitnessSession"/>) and reads it back four times a second, drawing only what
	/// changed. As a machine connects, the tab adds a Fitness Machine device to the Devices list when there is none.
	/// </remarks>
	public class FitnessPage : UserControl, IPluginPage
	{
		/// <summary>The tab of the process's session, as x360ce makes it.</summary>
		public FitnessPage(IPluginHost host) : this(host, FitnessSession.Shared)
		{
		}

		/// <summary>The tab of a session of its own, as a test makes it.</summary>
		internal FitnessPage(IPluginHost host, FitnessSession session)
		{
			_host = host;
			_session = session;
			Name = "FitnessPage";
			AccessibleName = "Fitness";
			AccessibleDescription = "Finds fitness machines over Bluetooth, connects to one, shows what it sends, and puts it on a controller.";
			SuspendLayout();
			Build();
			ResumeLayout(false);
			PerformLayout();
			ReconnectCheckBox.Checked = _host.GetValue(ReconnectValue) == bool.TrueString;
			ReconnectCheckBox.CheckedChanged += ReconnectCheckBox_CheckedChanged;
			// A machine that asks to be paired is asked about on this thread.
			_ui = SynchronizationContext.Current;
			_session.PairingRequested += Session_PairingRequested;
			// At start, to the machine connected last, when the person asked for that.
			var last = FitnessSession.ParseAddress(_host.GetValue(LastMachineValue));
			if (ReconnectCheckBox.Checked && last != 0 && _session.Wanted == null)
				_session.Reconnect(last, _host.GetValue(LastMachineNameValue) ?? FitnessSession.AddressText(last));
			_timer = new System.Windows.Forms.Timer { Interval = 250 };
			_timer.Tick += (s, e) => ShowSession();
			_timer.Start();
			ShowSession();
		}

		public string PageName { get { return "Fitness"; } }

		/// <summary>The name the Reconnect choice is kept under.</summary>
		internal const string ReconnectValue = "Reconnect";

		/// <summary>The names the machine connected last is kept under: its address and its name.</summary>
		internal const string LastMachineValue = "LastMachine";
		internal const string LastMachineNameValue = "LastMachineName";

		readonly IPluginHost _host;
		readonly FitnessSession _session;
		readonly SynchronizationContext _ui;
		readonly System.Windows.Forms.Timer _timer;

		/// <summary>The session's version the tab shows; -1 before it showed any.</summary>
		int _shownVersion = -1;

		/// <summary>The machine connected when the tab last looked, so it adds a device once for each connection.</summary>
		FitnessSession.Want _shownHeld;

		/// <summary>The machine connected last, as kept for the next start.</summary>
		FitnessSession.Want _keptLast;

		#region Controls

		internal TableLayoutPanel MainTableLayoutPanel;
		internal Label NoteLabel;
		internal FlowLayoutPanel ScanFlowLayoutPanel;
		internal Button ScanButton;
		internal Button ConnectButton;
		internal CheckBox ShowAllCheckBox;
		internal DataGridView MachinesDataGridView;
		internal DataGridViewTextBoxColumn NameColumn;
		internal DataGridViewTextBoxColumn TypeColumn;
		internal DataGridViewTextBoxColumn SignalColumn;
		internal FlowLayoutPanel MapFlowLayoutPanel;
		internal Button MapButton;
		internal ComboBox ControllerComboBox;
		internal CheckBox ReconnectCheckBox;
		internal FlowLayoutPanel CommandsFlowLayoutPanel;
		internal Button StartButton;
		internal Button PauseButton;
		internal Button StopButton;
		internal Button ResetButton;
		internal TableLayoutPanel TargetsTableLayoutPanel;
		internal TrackBar PowerTrackBar;
		internal Label PowerValueLabel;
		internal Button SetPowerButton;
		internal TrackBar ResistanceTrackBar;
		internal Label ResistanceValueLabel;
		internal Button SetResistanceButton;
		internal TrackBar GradeTrackBar;
		internal Label GradeValueLabel;
		internal Button SetGradeButton;
		internal TableLayoutPanel HelpTableLayoutPanel;
		internal Label HelpHeadingLabel;
		internal TableLayoutPanel StatusTableLayoutPanel;
		internal Label StatusLabel;
		internal Label FeaturesLabel;
		internal System.Windows.Forms.Panel ReadingsPanel;
		internal TableLayoutPanel ReadingsTableLayoutPanel;
		internal TextBox LogTextBox;

		/// <summary>Gives each control its room from the font: run again when the font changes.</summary>
		readonly List<Action<int>> _spacing = new List<Action<int>>();

		/// <summary>The width of the left column, the machines, commands and help, in gaps (half a line): the help wraps at it.</summary>
		/// <remarks>
		/// The receiver's columns at its own window's width: the two keep it on a wide window, and the room beside them stays
		/// empty. A wider control, in a larger font or other words, widens its column.
		/// </remarks>
		const int LeftGaps = 70;

		/// <summary>The width of the right column, the status, values and activity, in gaps: the status wraps at it.</summary>
		const int RightGaps = 46;

		/// <summary>The bold font of the status and the help's heading, made from the tab's font.</summary>
		Font _boldFont;

		void Build()
		{
			MainTableLayoutPanel = new TableLayoutPanel
			{
				Name = nameof(MainTableLayoutPanel),
				Dock = DockStyle.Fill,
				ColumnCount = 3,
				RowCount = 7,
			};
			// The two columns as wide as what is in them; the third, empty, takes the rest of a wide window.
			MainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			MainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			MainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			for (var i = 0; i < 7; i++)
				MainTableLayoutPanel.RowStyles.Add(i == 2 ? new RowStyle(SizeType.Percent, 100F) : new RowStyle());
			_spacing.Add(g => MainTableLayoutPanel.Padding = new Padding(g, g, 0, 0));
			Controls.Add(MainTableLayoutPanel);

			// The note across both columns, in the colours of a tip.
			NoteLabel = new Label
			{
				Name = nameof(NoteLabel),
				Text = "Experimental: walk, drive or row in games with a fitness machine. Tested on a Tunturi E60R exercise bike only. "
					+ "Use it with care and at your own risk.",
				AutoSize = true,
				Anchor = AnchorStyles.Left | AnchorStyles.Right,
				BackColor = SystemColors.Info,
				ForeColor = SystemColors.InfoText,
				AccessibleName = "Experimental feature",
			};
			_spacing.Add(g =>
			{
				NoteLabel.Padding = new Padding(g / 2);
				// As wide as the two columns and the room between them, where a longer note wraps.
				NoteLabel.MaximumSize = new Size((LeftGaps + 2 + RightGaps) * g, 0);
			});
			Place(NoteLabel, 0, 0);
			MainTableLayoutPanel.SetColumnSpan(NoteLabel, 2);

			// Left: machines.
			ScanButton = NewButton(nameof(ScanButton), "Scan", "Looks for fitness machines nearby, or stops looking.", 14);
			ScanButton.Click += ScanButton_Click;
			ConnectButton = NewButton(nameof(ConnectButton), "Connect", "Connects to the machine selected, or lets go of the machine connected.", 14);
			ConnectButton.Click += ConnectButton_Click;
			ShowAllCheckBox = NewCheckBox(nameof(ShowAllCheckBox), "Show all Bluetooth devices",
				"Lists every device that says its name, for machines that show the fitness service only once connected.");
			ScanFlowLayoutPanel = NewFlow(nameof(ScanFlowLayoutPanel), ScanButton, ConnectButton, ShowAllCheckBox);
			Place(ScanFlowLayoutPanel, 0, 1);

			// A grid, as x360ce's other lists are: the theme draws it, and the AI door reads its rows and selects one by its index.
			MachinesDataGridView = new DataGridView
			{
				Name = nameof(MachinesDataGridView),
				Dock = DockStyle.Fill,
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = false,
				AllowUserToResizeRows = false,
				AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
				ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
				MultiSelect = false,
				ReadOnly = true,
				RowHeadersVisible = false,
				SelectionMode = DataGridViewSelectionMode.FullRowSelect,
				AccessibleName = "Fitness machines",
				AccessibleDescription = "The machines heard: each one's name, what kind it is, and its signal. Select one and press Connect, or double-click it.",
			};
			NameColumn = NewMachineColumn(nameof(NameColumn), "Name", DataGridViewAutoSizeColumnMode.Fill);
			TypeColumn = NewMachineColumn(nameof(TypeColumn), "Type", DataGridViewAutoSizeColumnMode.AllCells);
			SignalColumn = NewMachineColumn(nameof(SignalColumn), "Signal", DataGridViewAutoSizeColumnMode.AllCells);
			SignalColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
			MachinesDataGridView.Columns.AddRange(NameColumn, TypeColumn, SignalColumn);
			MachinesDataGridView.SelectionChanged += (s, e) => ShowSession(true);
			MachinesDataGridView.CellDoubleClick += (s, e) =>
			{
				if (e.RowIndex >= 0)
					Connect();
			};
			Place(MachinesDataGridView, 0, 2);

			// Left: the controller the machine drives, and the start.
			MapButton = NewButton(nameof(MapButton), "Map to", "Puts the Fitness Machine device on the controller chosen beside it, mapped to drive.", 10);
			MapButton.Click += MapButton_Click;
			ControllerComboBox = new ComboBox
			{
				Name = nameof(ControllerComboBox),
				DropDownStyle = ComboBoxStyle.DropDownList,
				AccessibleName = "Controller",
				AccessibleDescription = "The controller Map to puts the Fitness Machine device on.",
			};
			ControllerComboBox.Items.AddRange(new object[] { "Controller 1", "Controller 2", "Controller 3", "Controller 4" });
			ControllerComboBox.SelectedIndex = 0;
			ReconnectCheckBox = NewCheckBox(nameof(ReconnectCheckBox), "Reconnect to the last machine when x360ce starts",
				"When x360ce starts, connects to the machine connected last, trying again until it connects or Disconnect is pressed.");
			MapFlowLayoutPanel = NewFlow(nameof(MapFlowLayoutPanel), MapButton, ControllerComboBox, ReconnectCheckBox);
			Place(MapFlowLayoutPanel, 0, 3);

			// Left: commands.
			StartButton = NewButton(nameof(StartButton), "Start", "Starts or resumes the machine's training.", 10);
			StartButton.Click += (s, e) => _session.Send(FitnessCommand.Start, 0);
			PauseButton = NewButton(nameof(PauseButton), "Pause", "Pauses the machine's training.", 10);
			PauseButton.Click += (s, e) => _session.Send(FitnessCommand.Pause, 0);
			StopButton = NewButton(nameof(StopButton), "Stop", "Stops the machine's training.", 10);
			StopButton.Click += (s, e) => _session.Send(FitnessCommand.Stop, 0);
			ResetButton = NewButton(nameof(ResetButton), "Reset", "Resets the machine.", 10);
			ResetButton.Click += (s, e) => _session.Send(FitnessCommand.Reset, 0);
			CommandsFlowLayoutPanel = NewFlow(nameof(CommandsFlowLayoutPanel), StartButton, PauseButton, StopButton, ResetButton);
			Place(CommandsFlowLayoutPanel, 0, 4);

			// Left: targets, each greyed while the machine does not take it.
			TargetsTableLayoutPanel = new TableLayoutPanel
			{
				Name = nameof(TargetsTableLayoutPanel),
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Anchor = AnchorStyles.Left | AnchorStyles.Right,
				ColumnCount = 4,
				RowCount = 3,
			};
			TargetsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle());
			TargetsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			TargetsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle());
			TargetsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle());
			for (var i = 0; i < 3; i++)
				TargetsTableLayoutPanel.RowStyles.Add(new RowStyle());
			PowerTrackBar = AddTarget(0, "Power", "Target power", 0, 1500, 150, 5, 25);
			PowerValueLabel = (Label)TargetsTableLayoutPanel.GetControlFromPosition(2, 0);
			SetPowerButton = (Button)TargetsTableLayoutPanel.GetControlFromPosition(3, 0);
			SetPowerButton.Click += (s, e) => _session.Send(FitnessCommand.Power, PowerTrackBar.Value);
			ResistanceTrackBar = AddTarget(1, "Resistance", "Target resistance", 0, 20, 5, 1, 2);
			ResistanceValueLabel = (Label)TargetsTableLayoutPanel.GetControlFromPosition(2, 1);
			SetResistanceButton = (Button)TargetsTableLayoutPanel.GetControlFromPosition(3, 1);
			SetResistanceButton.Click += (s, e) => _session.Send(FitnessCommand.Resistance, ResistanceTrackBar.Value);
			// Half percents, so the slider steps by half a percent.
			GradeTrackBar = AddTarget(2, "Grade", "Road grade", -40, 40, 0, 1, 4);
			GradeValueLabel = (Label)TargetsTableLayoutPanel.GetControlFromPosition(2, 2);
			SetGradeButton = (Button)TargetsTableLayoutPanel.GetControlFromPosition(3, 2);
			SetGradeButton.Click += (s, e) => _session.Send(FitnessCommand.Grade, GradeTrackBar.Value / 2.0);
			Place(TargetsTableLayoutPanel, 0, 5);

			// Left: how to receive from a machine.
			HelpTableLayoutPanel = new TableLayoutPanel
			{
				Name = nameof(HelpTableLayoutPanel),
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Anchor = AnchorStyles.Left | AnchorStyles.Right,
				ColumnCount = 1,
				BackColor = SystemColors.ControlLight,
				AccessibleName = "How to receive from a fitness machine",
			};
			HelpTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			_spacing.Add(g =>
			{
				HelpTableLayoutPanel.Padding = new Padding(g, g, g, g / 2);
				HelpTableLayoutPanel.MinimumSize = new Size(LeftGaps * g, 0);
			});
			HelpHeadingLabel = AddHelp(nameof(HelpHeadingLabel), "To receive from a fitness machine:");
			AddHelp("HelpStepsLabel",
				"1. Turn on the machine, or press (Start) in the Jocys.com Virtual Fitness Machine Transmitter app on another device.\n"
				+ "2. Press (Scan) and select the machine.\n"
				+ "3. Press (Connect). If it asks to pair, accept only if both devices show the same number.\n"
				+ "4. The machine's values appear on the right. (Start), (Pause) and the target (Set) buttons send it standard commands.\n"
				+ "5. Press (Map to) to drive that controller with the machine: your speed on the triggers.");
			AddHelp("HelpNoteLabel",
				"If a machine does not appear, tick 'Show all Bluetooth devices': some only show the fitness service after connecting. "
				+ "A machine connected to another app may not appear at all. When the Bluetooth connection between the app and the console "
				+ "is established, the console display may be dimmed. A computer's Bluetooth cannot connect to itself, so a transmitter "
				+ "must run on another device.");
			Place(HelpTableLayoutPanel, 0, 6);

			// Right: where the session stands, and what the machine measures and accepts.
			StatusLabel = new Label
			{
				Name = nameof(StatusLabel),
				AutoSize = true,
				Anchor = AnchorStyles.Left | AnchorStyles.Right,
				AccessibleName = "Status",
				AccessibleDescription = "Where the connection stands and, when it is stuck, what to do next.",
			};
			FeaturesLabel = new Label
			{
				Name = nameof(FeaturesLabel),
				AutoSize = true,
				Anchor = AnchorStyles.Left | AnchorStyles.Right,
				ForeColor = SystemColors.GrayText,
				AccessibleName = "Features",
				AccessibleDescription = "What the machine connected measures, and the targets it accepts.",
			};
			StatusTableLayoutPanel = new TableLayoutPanel
			{
				Name = nameof(StatusTableLayoutPanel),
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Anchor = AnchorStyles.Left | AnchorStyles.Right,
				ColumnCount = 1,
				RowCount = 2,
			};
			StatusTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			StatusTableLayoutPanel.RowStyles.Add(new RowStyle());
			StatusTableLayoutPanel.RowStyles.Add(new RowStyle());
			StatusTableLayoutPanel.Controls.Add(StatusLabel, 0, 0);
			StatusTableLayoutPanel.Controls.Add(FeaturesLabel, 0, 1);
			_spacing.Add(g =>
			{
				StatusTableLayoutPanel.MinimumSize = new Size(RightGaps * g, 0);
				StatusLabel.MaximumSize = new Size(RightGaps * g, 0);
				StatusLabel.Margin = new Padding(0, 0, 0, g / 2);
				FeaturesLabel.MaximumSize = new Size(RightGaps * g, 0);
				FeaturesLabel.Margin = new Padding(0);
			});
			Place(StatusTableLayoutPanel, 1, 1);

			// Right: the values, beside the list of machines.
			// Unnamed, so the AI door lists its rows' values in the box of values, as it lists any panel's.
			ReadingsTableLayoutPanel = new TableLayoutPanel
			{
				Name = nameof(ReadingsTableLayoutPanel),
				Dock = DockStyle.Top,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				ColumnCount = 3,
			};
			ReadingsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle());
			ReadingsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			ReadingsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle());
			ReadingsPanel = new System.Windows.Forms.Panel
			{
				Name = nameof(ReadingsPanel),
				Dock = DockStyle.Fill,
				AutoScroll = true,
				BorderStyle = BorderStyle.FixedSingle,
				BackColor = SystemColors.ControlLight,
				AccessibleName = "Values",
				AccessibleDescription = "The newest value of every field the machine sent, and how often each changes.",
			};
			ReadingsPanel.Controls.Add(ReadingsTableLayoutPanel);
			_spacing.Add(g => ReadingsPanel.Padding = new Padding(g, g / 2, g, g / 2));
			Place(ReadingsPanel, 1, 2);

			// Right: what happened, as tall as the commands and help beside it.
			LogTextBox = new TextBox
			{
				Name = nameof(LogTextBox),
				Dock = DockStyle.Fill,
				Multiline = true,
				ReadOnly = true,
				WordWrap = true,
				BackColor = SystemColors.ControlLight,
				AccessibleName = "Activity",
				AccessibleDescription = "What happened, newest first.",
			};
			LogTextBox.SizeChanged += (s, e) => ShowLogScrollBar();
			// Once its window is made, after the call that makes it, which a change of scroll bars makes again.
			LogTextBox.HandleCreated += (s, e) => LogTextBox.BeginInvoke((Action)ShowLogScrollBar);
			Place(LogTextBox, 1, 3);
			MainTableLayoutPanel.SetRowSpan(LogTextBox, 4);
			ApplyFont();
		}

		/// <summary>Puts a control in a cell of the main table, with the room after it the font gives, and twice that between the columns.</summary>
		void Place(Control control, int column, int row)
		{
			_spacing.Add(g => control.Margin = new Padding(column == 1 ? g : 0, 0, g, g));
			MainTableLayoutPanel.Controls.Add(control, column, row);
		}

		/// <summary>A button named by its words, as the AI door reads it: Connect reads Disconnect while it disconnects.</summary>
		Button NewButton(string name, string text, string description, int widthInGaps)
		{
			var button = new Button
			{
				Name = name,
				Text = text,
				AutoSize = true,
				UseVisualStyleBackColor = true,
				AccessibleDescription = description,
			};
			_spacing.Add(g => button.MinimumSize = new Size(g * widthInGaps, 0));
			return button;
		}

		CheckBox NewCheckBox(string name, string text, string description)
		{
			return new CheckBox
			{
				Name = name,
				Text = text,
				AutoSize = true,
				Anchor = AnchorStyles.Left,
				AccessibleName = text,
				AccessibleDescription = description,
			};
		}

		/// <summary>A column of the list of machines, kept in the order the machines were heard rather than sorted.</summary>
		static DataGridViewTextBoxColumn NewMachineColumn(string name, string header, DataGridViewAutoSizeColumnMode size)
		{
			return new DataGridViewTextBoxColumn
			{
				Name = name,
				HeaderText = header,
				AutoSizeMode = size,
				SortMode = DataGridViewColumnSortMode.NotSortable,
			};
		}

		/// <summary>A row of controls, each the room the font gives after the one before it.</summary>
		FlowLayoutPanel NewFlow(string name, params Control[] controls)
		{
			var flow = new FlowLayoutPanel
			{
				Name = name,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Anchor = AnchorStyles.Left | AnchorStyles.Right,
				WrapContents = true,
			};
			flow.Controls.AddRange(controls);
			_spacing.Add(g =>
			{
				foreach (Control control in flow.Controls)
					control.Margin = new Padding(0, 0, g, 0);
			});
			return flow;
		}

		/// <summary>Adds a target's row: its label, its slider, its value and its Set button.</summary>
		TrackBar AddTarget(int row, string key, string text, int minimum, int maximum, int value, int smallChange, int largeChange)
		{
			var label = new Label { Name = key + "Label", Text = text, AutoSize = true, Anchor = AnchorStyles.Left };
			var slider = new TrackBar
			{
				Name = key + "TrackBar",
				Minimum = minimum,
				Maximum = maximum,
				Value = value,
				SmallChange = smallChange,
				LargeChange = largeChange,
				TickStyle = TickStyle.None,
				Anchor = AnchorStyles.Left | AnchorStyles.Right,
				AccessibleName = text,
				AccessibleDescription = "The " + text.ToLowerInvariant() + " Set sends to the machine.",
			};
			var shown = new Label { Name = key + "ValueLabel", AutoSize = true, Anchor = AnchorStyles.Right, TextAlign = ContentAlignment.MiddleRight, AccessibleName = text };
			var set = new Button
			{
				Name = "Set" + key + "Button",
				Text = "Set",
				AutoSize = true,
				Anchor = AnchorStyles.Left,
				UseVisualStyleBackColor = true,
				AccessibleName = "Set " + text.ToLowerInvariant(),
				AccessibleDescription = "Sends the " + text.ToLowerInvariant() + " to the machine.",
			};
			slider.ValueChanged += (s, e) => ShowTargets();
			_spacing.Add(g =>
			{
				label.Margin = new Padding(0, 0, g, 0);
				slider.Margin = new Padding(0, 0, g, 0);
				shown.Margin = new Padding(0, 0, g, 0);
				shown.MinimumSize = new Size(g * 8, 0);
				set.Margin = new Padding(0, 0, 0, g / 2);
				set.MinimumSize = new Size(g * 8, 0);
			});
			TargetsTableLayoutPanel.Controls.Add(label, 0, row);
			TargetsTableLayoutPanel.Controls.Add(slider, 1, row);
			TargetsTableLayoutPanel.Controls.Add(shown, 2, row);
			TargetsTableLayoutPanel.Controls.Add(set, 3, row);
			return slider;
		}

		Label AddHelp(string name, string text)
		{
			var label = new Label { Name = name, Text = text, AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Right };
			_spacing.Add(g =>
			{
				label.Margin = new Padding(0, 0, 0, g / 2);
				// Wraps inside the help's room: the left column's width less the help's padding.
				label.MaximumSize = new Size((LeftGaps - 2) * g, 0);
			});
			HelpTableLayoutPanel.RowStyles.Add(new RowStyle());
			HelpTableLayoutPanel.Controls.Add(label, 0, HelpTableLayoutPanel.RowCount++);
			return label;
		}

		/// <summary>Gives the controls their room and the bold font from the tab's font, which follows x360ce's font and zoom.</summary>
		void ApplyFont()
		{
			var old = _boldFont;
			_boldFont = new Font(Font, FontStyle.Bold);
			StatusLabel.Font = _boldFont;
			HelpHeadingLabel.Font = _boldFont;
			if (old != null)
				old.Dispose();
			// Half a line, 8 pixels at 100 % in x360ce's font.
			var gap = Math.Max(1, (Font.Height + 1) / 2);
			foreach (var space in _spacing)
				space(gap);
		}

		protected override void OnFontChanged(EventArgs e)
		{
			base.OnFontChanged(e);
			if (MainTableLayoutPanel != null)
				ApplyFont();
		}

		#endregion

		#region What the tab shows

		/// <summary>The machine selected in the list, by a click or by the AI door setting the list to its row; null while none is.</summary>
		/// <remarks>Read from the rows selected, which the grid changes before the current row it raises SelectionChanged for.</remarks>
		FitnessSeen SelectedMachine
		{
			get
			{
				var rows = MachinesDataGridView.SelectedRows;
				return rows.Count == 1 ? rows[0].Tag as FitnessSeen : null;
			}
		}

		/// <summary>Shows what the session holds, when it changed since the tab showed it, or at once.</summary>
		void ShowSession(bool now = false)
		{
			// Listing the machines changes the selection, which asks for this again.
			if (IsDisposed || _showing)
				return;
			var version = _session.Version;
			if (!now && version == _shownVersion)
				return;
			_shownVersion = version;
			_showing = true;
			try
			{
				Draw();
			}
			finally
			{
				_showing = false;
			}
		}

		/// <summary>True while the tab shows the session.</summary>
		bool _showing;

		void Draw()
		{
			var held = _session.Held;
			var wanted = _session.Wanted;
			var connected = held != null;
			var busy = wanted != null;
			ShowMachines();
			var selected = SelectedMachine;
			var status = _session.StatusText;
			if (selected != null && status == FitnessSession.IdleText)
				status = "Press Connect to connect to " + selected.DisplayName + ".";
			SetText(StatusLabel, status);
			SetText(FeaturesLabel, _session.FeaturesText);
			ScanButton.Text = _session.Scanning ? "Stop scanning" : "Scan";
			ScanButton.Enabled = !busy;
			ConnectButton.Text = busy ? "Disconnect" : "Connect";
			ConnectButton.Enabled = busy || selected != null;
			ShowAllCheckBox.Enabled = !_session.Scanning;
			MachinesDataGridView.Enabled = !busy;
			var commands = connected && _session.CanCommand && !_session.Busy;
			StartButton.Enabled = commands;
			PauseButton.Enabled = commands;
			StopButton.Enabled = commands;
			ResetButton.Enabled = commands;
			ShowRange(PowerTrackBar, _session.PowerMin, _session.PowerMax, 1);
			ShowRange(ResistanceTrackBar, _session.RangeMin, _session.RangeMax, (int)Ftms.ResistanceScale);
			EnableTarget(PowerTrackBar, SetPowerButton, commands && _session.Supports(Ftms.PowerTargetBit));
			EnableTarget(ResistanceTrackBar, SetResistanceButton, commands && _session.Supports(Ftms.ResistanceTargetBit));
			EnableTarget(GradeTrackBar, SetGradeButton, commands && _session.Supports(Ftms.SimulationTargetBit));
			ShowTargets();
			ShowReadings();
			var log = string.Join(Environment.NewLine, _session.Log());
			if (LogTextBox.Text != log)
			{
				LogTextBox.Text = log;
				ShowLogScrollBar();
			}
			// A machine just connected: put a device for it in the Devices list, and keep it for the next start.
			if (held != null && held != _shownHeld)
				AddDevice();
			_shownHeld = held;
			var last = _session.Last;
			if (last != null && last != _keptLast)
			{
				_keptLast = last;
				_host.SetValue(LastMachineValue, FitnessSession.AddressText(last.Address));
				_host.SetValue(LastMachineNameValue, last.Name);
			}
		}

		static void SetText(Control control, string text)
		{
			if (control.Text != text)
				control.Text = text;
		}

		static void EnableTarget(TrackBar slider, Button set, bool enabled)
		{
			slider.Enabled = enabled;
			set.Enabled = enabled;
		}

		/// <summary>Takes a slider's range from the one the machine reported, in its units, keeping its value inside.</summary>
		static void ShowRange(TrackBar slider, int min, int max, int scale)
		{
			if (min == FitnessSession.Unknown || max == FitnessSession.Unknown)
				return;
			var minimum = (int)Math.Ceiling(min / (double)scale);
			var maximum = (int)Math.Floor(max / (double)scale);
			if (maximum <= minimum || (slider.Minimum == minimum && slider.Maximum == maximum))
				return;
			var value = Math.Max(minimum, Math.Min(maximum, slider.Value));
			slider.Minimum = Math.Min(minimum, slider.Minimum);
			slider.Maximum = Math.Max(maximum, slider.Maximum);
			slider.Value = value;
			slider.Minimum = minimum;
			slider.Maximum = maximum;
		}

		/// <summary>Shows the log's scroll bar only while its lines, wrapped, are more than it shows.</summary>
		void ShowLogScrollBar()
		{
			// Its lines are counted by its window, which is not made before it is shown.
			if (!LogTextBox.IsHandleCreated)
				return;
			var lines = LogTextBox.TextLength == 0 ? 0 : LogTextBox.GetLineFromCharIndex(LogTextBox.TextLength) + 1;
			var bars = lines * LogTextBox.Font.Height > LogTextBox.ClientSize.Height ? ScrollBars.Vertical : ScrollBars.None;
			if (LogTextBox.ScrollBars != bars)
				LogTextBox.ScrollBars = bars;
		}

		void ShowTargets()
		{
			SetText(PowerValueLabel, PowerTrackBar.Value.ToString(CultureInfo.InvariantCulture) + " W");
			SetText(ResistanceValueLabel, ResistanceTrackBar.Value.ToString(CultureInfo.InvariantCulture));
			SetText(GradeValueLabel, (GradeTrackBar.Value / 2.0).ToString("0.0", CultureInfo.InvariantCulture) + " %");
		}

		/// <summary>Lists the machines heard, a row each in the order heard, keeping the one selected.</summary>
		void ShowMachines()
		{
			var machines = _session.Machines();
			var grid = MachinesDataGridView;
			var rows = grid.Rows;
			if (rows.Count != machines.Length || machines.Where((x, i) => !ReferenceEquals(rows[i].Tag, x)).Any())
			{
				var keep = Array.IndexOf(machines, SelectedMachine);
				rows.Clear();
				foreach (var machine in machines)
					rows[rows.Add()].Tag = machine;
				// Only a machine the person chose is selected: a grid with the focus makes the first row it is given current by itself.
				grid.CurrentCell = keep < 0 ? null : rows[keep].Cells[NameColumn.Index];
				if (keep < 0)
					grid.ClearSelection();
			}
			// Names and signals change in place.
			foreach (DataGridViewRow row in rows)
			{
				var machine = (FitnessSeen)row.Tag;
				SetCell(row.Cells[NameColumn.Index], machine.DisplayName);
				SetCell(row.Cells[TypeColumn.Index], machine.Description);
				SetCell(row.Cells[SignalColumn.Index], machine.Signal.ToString(CultureInfo.InvariantCulture) + " dBm");
			}
		}

		static void SetCell(DataGridViewCell cell, string text)
		{
			if (!Equals(cell.Value, text))
				cell.Value = text;
		}

		/// <summary>Shows the values, a row for each in the order they first came: its name, its value, and how often it changes.</summary>
		/// <remarks>
		/// The value and how often it changes are named and described, so the AI door lists them under the box of values,
		/// such as SpeedValueLabel, named Speed; the name before them only captions the value, which carries it.
		/// </remarks>
		void ShowReadings()
		{
			var readings = _session.Readings();
			var table = ReadingsTableLayoutPanel;
			// A new connection starts from no values.
			if (readings.Length < table.RowCount)
			{
				table.SuspendLayout();
				while (table.Controls.Count > 0)
					table.Controls[0].Dispose();
				table.RowStyles.Clear();
				table.RowCount = 0;
				table.ResumeLayout();
			}
			for (var i = 0; i < readings.Length; i++)
			{
				var reading = readings[i];
				if (i >= table.RowCount)
				{
					// "Heart rate" is HeartRate.
					var key = new string(CultureInfo.InvariantCulture.TextInfo.ToTitleCase(reading.Name).Where(char.IsLetterOrDigit).ToArray());
					var words = reading.Name.ToLowerInvariant();
					var gap = Math.Max(1, (Font.Height + 1) / 2);
					table.RowStyles.Add(new RowStyle());
					table.RowCount++;
					table.Controls.Add(new Label { Name = key + "NameLabel", Text = reading.Name, AutoSize = true, ForeColor = SystemColors.GrayText,
						Margin = new Padding(0, gap / 4, gap * 2, gap / 4) }, 0, i);
					table.Controls.Add(new Label { Name = key + "ValueLabel", AutoSize = true, AccessibleName = reading.Name,
						AccessibleDescription = "The newest " + words + " the machine sent.",
						Margin = new Padding(0, gap / 4, 0, gap / 4) }, 1, i);
					table.Controls.Add(new Label { Name = key + "IntervalLabel", AutoSize = true, ForeColor = SystemColors.GrayText,
						Margin = new Padding(gap * 2, gap / 4, 0, gap / 4), AccessibleName = reading.Name + " changes",
						AccessibleDescription = "How often the " + words + " changes, such as every 1.0 s; empty until it changed three times." }, 2, i);
				}
				SetText(table.GetControlFromPosition(1, i), reading.Value);
				SetText(table.GetControlFromPosition(2, i), reading.Interval);
			}
		}

		#endregion

		#region What the tab does

		void ScanButton_Click(object sender, EventArgs e)
		{
			if (_session.Scanning)
				_session.StopScan();
			else
				_session.Scan(ShowAllCheckBox.Checked);
			ShowSession(true);
		}

		void ConnectButton_Click(object sender, EventArgs e)
		{
			if (_session.Wanted != null)
				_session.Disconnect();
			else
				Connect();
			ShowSession(true);
		}

		/// <summary>Connects to the machine selected.</summary>
		void Connect()
		{
			var machine = SelectedMachine;
			if (machine == null || _session.Wanted != null)
				return;
			_session.Connect(machine.Address, machine.DisplayName);
			ShowSession(true);
		}

		/// <summary>Adds a Fitness Machine device to the Devices list when there is none, as a machine connects.</summary>
		void AddDevice()
		{
			try
			{
				_host.AddDevice(FitnessMachine.Product);
			}
			// The plugin's device was switched off after it failed: the Plugins page says why.
			catch (InvalidOperationException ex)
			{
				_session.Note(ex.Message);
			}
		}

		void MapButton_Click(object sender, EventArgs e)
		{
			try
			{
				_host.MapDevice(FitnessMachine.Product, ControllerComboBox.SelectedIndex + 1);
			}
			// No game is chosen, or the device was switched off: said in the log.
			catch (InvalidOperationException ex)
			{
				_session.Note(ex.Message);
			}
		}

		void ReconnectCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			_host.SetValue(ReconnectValue, ReconnectCheckBox.Checked ? bool.TrueString : bool.FalseString);
		}

		/// <summary>Asks the person whether to pair, on the interface thread, and gives the session the answer.</summary>
		void Session_PairingRequested(FitnessPairing pairing)
		{
			if (_ui == null)
			{
				pairing.Accept(false);
				return;
			}
			_ui.Post(_ =>
			{
				if (IsDisposed)
				{
					pairing.Accept(false);
					return;
				}
				using (var form = new FitnessQuestionForm("Pair with fitness machine", pairing.Question, pairing.Kind == FitnessPairingKind.DisplayPin))
				{
					var answer = form.ShowDialog(FindForm());
					pairing.Accept(answer == DialogResult.Yes || answer == DialogResult.OK);
				}
			}, null);
		}

		#endregion

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_timer != null)
					_timer.Dispose();
				_session.PairingRequested -= Session_PairingRequested;
				if (_boldFont != null)
					_boldFont.Dispose();
			}
			base.Dispose(disposing);
		}
	}

	/// <summary>A question the Fitness tab asks: Yes and No, or OK and Cancel. A window of x360ce's interface thread, so x360ce's theme draws it.</summary>
	sealed class FitnessQuestionForm : Form
	{
		public FitnessQuestionForm(string caption, string question, bool okCancel)
		{
			Name = "FitnessQuestionForm";
			Text = caption;
			FormBorderStyle = FormBorderStyle.FixedDialog;
			StartPosition = FormStartPosition.CenterParent;
			MaximizeBox = false;
			MinimizeBox = false;
			ShowInTaskbar = false;
			AutoSize = true;
			AutoSizeMode = AutoSizeMode.GrowAndShrink;
			var gap = Math.Max(1, (Font.Height + 1) / 2);
			var table = new TableLayoutPanel
			{
				Name = "QuestionTableLayoutPanel",
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				ColumnCount = 1,
				Padding = new Padding(gap),
			};
			var label = new Label
			{
				Name = "QuestionLabel",
				Text = question,
				AutoSize = true,
				MaximumSize = new Size(Font.Height * 28, 0),
				Margin = new Padding(0, 0, 0, gap * 2),
			};
			var yes = new Button
			{
				Name = "YesButton",
				Text = okCancel ? "OK" : "&Yes",
				DialogResult = okCancel ? DialogResult.OK : DialogResult.Yes,
				AutoSize = true,
				UseVisualStyleBackColor = true,
				MinimumSize = new Size(gap * 10, 0),
				Margin = new Padding(0, 0, gap, 0),
			};
			var no = new Button
			{
				Name = "NoButton",
				Text = okCancel ? "Cancel" : "&No",
				DialogResult = okCancel ? DialogResult.Cancel : DialogResult.No,
				AutoSize = true,
				UseVisualStyleBackColor = true,
				MinimumSize = new Size(gap * 10, 0),
				Margin = new Padding(0),
			};
			var buttons = new FlowLayoutPanel { Name = "QuestionButtonsFlowLayoutPanel", AutoSize = true, Anchor = AnchorStyles.Right };
			buttons.Controls.Add(yes);
			buttons.Controls.Add(no);
			table.Controls.Add(label, 0, 0);
			table.Controls.Add(buttons, 0, 1);
			Controls.Add(table);
			AcceptButton = yes;
			CancelButton = no;
		}
	}

	#endregion
}
