#:include FitnessMachine.Properties.cs
#:include FitnessMachine.Connection.cs
#:include FitnessMachine.Help.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using x360ce.Plugins;

namespace x360ce.Plugins.Samples
{
	#region The device

	/// <summary>An exercise bike, treadmill, rower or cross trainer, read over Bluetooth's standard Fitness Machine Service (FTMS), as a controller.</summary>
	/// <remarks>
	/// The plugin is four files, which the #:include lines above build as one: this one, the device and its connection to
	/// the machine; FitnessMachine.Properties.cs, its settings and statuses; FitnessMachine.Connection.cs, its Connection
	/// page; and FitnessMachine.Help.cs, its help.
	///
	/// Each device holds its own connection (<see cref="FitnessSession"/>) to the one machine its Machine setting names,
	/// which its Connection page sets when the person selects a machine. x360ce reads a device only while it is ticked, on
	/// the Devices tab and in its controller's list, and disposes it when it is unticked, removed or x360ce closes. The
	/// first read connects to the machine, trying again quietly until it connects; a machine chosen later replaces it; and
	/// Dispose lets go of it, without waiting. A device x360ce never reads, such as the one it makes to learn the plugin's
	/// name or the one its tab shows while it is unticked, never connects.
	///
	/// <see cref="Read"/> copies the newest values the machine sent from the session's volatile fields, and the session's
	/// worker sends the machine what <see cref="NextCommand"/> works out from the game's force, four times a second at most.
	/// x360ce sets the settings on the thread that calls Read; those the worker or the Connection page read too are volatile.
	/// </remarks>
	public partial class FitnessMachine : IPluginDevice, IPluginForceFeedback, IPluginMapping, IPluginDeviceIdentity, INotifyPropertyChanged, IDisposable
	{
		/// <summary>Names the kind of device: saved mappings find the device by it.</summary>
		internal static readonly Guid Product = new Guid("6e1793da-b362-49b0-ad25-08433feac3f8");

		/// <summary>A device with a connection of its own, as x360ce makes one.</summary>
		public FitnessMachine() : this(new FitnessSession())
		{
		}

		/// <summary>A device with the connection given, as a test makes one.</summary>
		internal FitnessMachine(FitnessSession session)
		{
			_session = session;
			session.Driver = this;
		}

		readonly FitnessSession _session;

		/// <summary>The device's connection to its machine.</summary>
		internal FitnessSession Session { get { return _session; } }

		public string Name { get { return "Fitness Machine"; } }
		public Guid ProductGuid { get { return Product; } }
		public PluginDeviceIdentity HardwareIdentity { get { return _session.HardwareIdentity; } }
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

		#region Choosing the machine

		/// <summary>Raised on the interface thread when the Connection page chooses a machine, so x360ce keeps the Machine setting for the device.</summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>Raised on the interface thread when a different Bluetooth machine is selected so x360ce unchecks and disconnects the controller instead of auto-connecting to another person's machine.</summary>
		public event EventHandler DisconnectRequested;

		/// <summary>Chooses the machine the device connects to, as the Connection page does when the person selects one.</summary>
		/// <remarks>
		/// Called on the interface thread. Immediately releases any current Bluetooth link and asks x360ce to uncheck the
		/// controller so selecting a nearby machine never auto-connects without explicit confirmation.
		/// </remarks>
		/// <param name="address">The machine's Bluetooth address.</param>
		/// <param name="name">Its name, as the list shows it.</param>
		internal void Choose(ulong address, string name)
		{
			var text = FitnessSession.Want.Text(address, name);
			if (text == _machine)
				return;
			_machine = text;
			_session.Hold(null);
			var changed = PropertyChanged;
			if (changed != null)
				changed(this, new PropertyChangedEventArgs(nameof(Machine)));
			var disconnect = DisconnectRequested;
			if (disconnect != null)
				disconnect(this, EventArgs.Empty);
		}

		#endregion

		#region Read

		/// <summary>The Machine setting the session was last asked to hold; null before the first read.</summary>
		string _held;

		/// <summary>True once x360ce read the device.</summary>
		volatile bool _read;

		/// <summary>True once the device was let go of: it never holds a machine again.</summary>
		volatile bool _disposed;

		/// <summary>True while x360ce reads the device, which it does while the device is ticked: from the first read until the device is let go of.</summary>
		internal bool Reading { get { return _read && !_disposed; } }

		public void Read(PluginState state)
		{
			var session = _session;
			// Read, the device is ticked: the first read holds the machine chosen, and a machine chosen later replaces it.
			var machine = _machine;
			if (!string.Equals(machine, _held, StringComparison.Ordinal) && !_disposed)
			{
				_held = machine;
				_read = true;
				session.Hold(FitnessSession.Want.Parse(machine));
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

		/// <summary>Lets go of the machine without waiting: the session's worker gives it back the level the person set and closes the link.</summary>
		/// <remarks>
		/// Called on x360ce's input thread when the device is unticked, removed or x360ce closes, which must not wait. The
		/// device is not read again: ticked again, x360ce makes a new one.
		/// </remarks>
		public void Dispose()
		{
			if (_disposed)
				return;
			_disposed = true;
			_session.Close();
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

		/// <summary>The Force status: how strong the game's rumble is, and the target it sets on the machine.</summary>
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
		// Standard Bluetooth SIG Device Information Service (0x180A) for real hardware manufacturer, model and VID/PID.
		public const ushort DeviceInformationServiceId = 0x180A;
		public const ushort ManufacturerNameId = 0x2A29;
		public const ushort ModelNumberId = 0x2A24;
		public const ushort PnpId = 0x2A50;

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

		/// <summary>The command a button or a target of the Connection page sends.</summary>
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

	/// <summary>A command a button or a target of the Connection page sends.</summary>
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

	#region The connection

	/// <summary>One connection to a machine, which the session's worker opens, works and closes: Bluetooth's (<see cref="FitnessSession.Link"/>), or a test's.</summary>
	public interface IFitnessLink : IDisposable
	{
		/// <summary>True while the machine is connected.</summary>
		bool Up { get; }

		/// <summary>Connects, reads what the machine is and asks for its values; null once connected, or why it cannot.</summary>
		Task<string> Open();

		/// <summary>Writes one command, and waits a moment for the machine's answer; null when there is none.</summary>
		Task<byte[]> Send(byte[] command);

		/// <summary>Lets go of the machine: gives it back the level the person set, tells it to stop sending, and disposes the link.</summary>
		Task Close();
	}

	/// <summary>A device's connection to its machine: what the machine sent, what x360ce sends it, and the worker that holds the link.</summary>
	/// <remarks>
	/// One for each device, as each connects to a machine of its own. Bluetooth is worked on a thread of the session's own,
	/// started when the device first holds a machine and ended once the device let go: it connects, sends one command at a
	/// time and lets go as the device asks, through volatile fields, so neither the device's Read nor the Connection page
	/// waits for it. Every value the machine sends passes through <see cref="Take"/>, which keeps the newest in volatile
	/// fields for Read, and the readings and the activity log for the Connection page under a lock the input thread never
	/// takes.
	///
	/// The worker connects to the machine the device holds (<see cref="Hold"/>), trying again quietly, a little later after
	/// each failure, until it connects or the device lets go (<see cref="Close"/>), and connects again when the link is lost.
	/// Letting go gives the machine back the level the person set, asks it to stop sending, and disposes every Bluetooth
	/// object; a closing x360ce waits a moment for it.
	///
	/// Windows' Bluetooth types are named only in <see cref="Link"/> and <see cref="Scanner"/>, which only threads other than
	/// the interface's use, so on a Windows without them the worker says so and nothing else fails.
	/// </remarks>
	sealed class FitnessSession
	{
		/// <summary>A connection that makes Bluetooth links.</summary>
		internal FitnessSession()
		{
			NewLink = (address, name) => new Link(this, address, name);
		}

		/// <summary>The device whose game's force drives the machine.</summary>
		internal volatile FitnessMachine Driver;

		/// <summary>Makes the link to a machine, by its address and its name: Bluetooth's; a test gives one of its own.</summary>
		internal Func<ulong, string, IFitnessLink> NewLink;

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

		/// <summary>How long a closing x360ce waits for every machine to be let go of: .NET Framework gives the handlers of a closing process two seconds in all.</summary>
		const int ExitMilliseconds = 1500;

		/// <summary>How long the worker waits before it tries again after a failure: longer after each, up to the last.</summary>
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
		/// The link calls it for every notification and every value it reads, on whichever thread Windows raises them, and
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

		/// <summary>Keeps the values one packet held, a value it did not hold keeping what it was, and shows each on the Connection page.</summary>
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

		/// <summary>A command of the Connection page waiting for the worker.</summary>
		sealed class PageCommand
		{
			public PageCommand(byte[] bytes)
			{
				Bytes = bytes;
			}

			public readonly byte[] Bytes;
		}

		/// <summary>The Connection page's command the worker sends next; null while none waits.</summary>
		volatile PageCommand _command;

		/// <summary>True while a command of the Connection page waits or is sent: the page sends one at a time.</summary>
		internal bool Busy { get { return _command != null; } }

		/// <summary>Sends the machine a command of the Connection page, asking for control first when it must, and logs its answer.</summary>
		internal void Send(FitnessCommand command, double value)
		{
			_command = new PageCommand(Ftms.Command(command, value));
			Changed();
			Wake();
		}

		/// <summary>True when the machine connected takes a target of this bit of its Feature value.</summary>
		internal bool Supports(int targetBit)
		{
			return Features != Unknown && (Targets & targetBit) != 0;
		}

		#endregion

		#region What the Connection page shows

		readonly object _lock = new object();

		/// <summary>Raised by every change the Connection page shows, so it draws only when something changed.</summary>
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

		/// <summary>Called with the lock held.</summary>
		void Reading(string name, string value, long at)
		{
			var row = _readings.FirstOrDefault(x => x.Name == name);
			if (row == null)
				_readings.Add(new FitnessReading(name, value, at));
			else
				row.Update(value, at);
		}

		/// <summary>The newest value of every field the machine sent, as the Connection page shows them, in the order the fields first came.</summary>
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

		/// <summary>Where the connection stands while the device holds a machine, in a sentence or two; null while it holds none.</summary>
		internal string StatusText
		{
			get
			{
				if (_tooOld)
					return NeedsWindows10;
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
				var want = _want;
				if (want == null)
					return null;
				if (_declined)
					return "Pairing with " + want.Name + " was declined. Untick and tick this device to try again.";
				var problem = _problem;
				return problem == null ? "Connecting to " + want.Name + "..." : problem + " Trying again.";
			}
		}

		/// <summary>What a Windows without Bluetooth's WinRT types says.</summary>
		internal const string NeedsWindows10 = "Needs Windows 10 or later.";

		/// <summary>The last problem, shown until the next connection; null while there is none.</summary>
		volatile string _problem;

		/// <summary>True when Windows has no Bluetooth types: the worker stopped.</summary>
		volatile bool _tooOld;

		#endregion

		#region What the device asks for

		/// <summary>A machine to hold: its address and its name.</summary>
		internal sealed class Want
		{
			public Want(ulong address, string name)
			{
				Address = address;
				Name = name;
			}

			public readonly ulong Address;
			public readonly string Name;

			/// <summary>True for the same machine under the same name.</summary>
			public bool Same(Want other)
			{
				return other != null && other.Address == Address && other.Name == Name;
			}

			/// <summary>A machine as its Machine setting keeps it: "EJT [A1:B2:C3:D4:E5:F6]", or the address alone for a machine without a name.</summary>
			public static string Text(ulong address, string name)
			{
				var shown = AddressText(address);
				name = (name ?? "").Trim();
				return name.Length == 0 || name == shown ? shown : name + " [" + shown + "]";
			}

			/// <summary>The machine a Machine setting names by the Bluetooth address it holds, alone or in brackets after a name; null when it holds none.</summary>
			public static Want Parse(string text)
			{
				text = (text ?? "").Trim();
				var open = text.LastIndexOf('[');
				var bracketed = open >= 0 && text.EndsWith("]", StringComparison.Ordinal);
				var address = ParseAddress(bracketed ? text.Substring(open + 1, text.Length - open - 2) : text);
				if (address == 0)
					return null;
				var name = bracketed ? text.Substring(0, open).Trim() : "";
				return new Want(address, name.Length > 0 ? name : AddressText(address));
			}
		}

		/// <summary>The machine the device holds; null to hold none.</summary>
		volatile Want _want;

		/// <summary>The machine connected now; null while none is.</summary>
		volatile Want _held;

		/// <summary>Real hardware identity of the connected fitness machine, or null while unconnected.</summary>
		volatile PluginDeviceIdentity _hardwareIdentity;

		internal PluginDeviceIdentity HardwareIdentity { get { return _hardwareIdentity; } }

		internal void SetHardwareIdentity(PluginDeviceIdentity identity)
		{
			_hardwareIdentity = identity;
			Changed();
		}

		/// <summary>
		/// Builds a deterministic real-hardware <see cref="PluginDeviceIdentity"/> from Bluetooth Device Information Service
		/// characteristics (Manufacturer Name 0x2A29, Model Number 0x2A24, PnP ID 0x2A50) and the advertised BLE name.
		/// </summary>
		public static PluginDeviceIdentity BuildHardwareIdentity(string advertisedName, string manufacturer, string modelNumber, byte[] pnpId)
		{
			var cleanAdv = CleanText(advertisedName);
			var cleanMfg = CleanText(manufacturer);
			var cleanModel = CleanText(modelNumber);

			int vid = 0;
			int pid = 0;
			if (pnpId != null && pnpId.Length >= 5)
			{
				// BLE PnP ID (0x2A50): byte 0 = Vendor ID Source (1=Bluetooth SIG, 2=USB-IF), 1..2 = VID (LE), 3..4 = PID (LE).
				vid = pnpId[1] | (pnpId[2] << 8);
				pid = pnpId[3] | (pnpId[4] << 8);
			}

			var productName = CombineProductName(cleanMfg, cleanModel, cleanAdv);
			if (string.IsNullOrEmpty(productName))
				return null;
			var finalMfg = !string.IsNullOrEmpty(cleanMfg) ? cleanMfg : InferManufacturer(productName);

			Guid productGuid;
			if (vid > 0 && pid > 0)
			{
				// Standard Windows DirectInput/HID VID+PID GUID format: {PID:X4}{VID:X4}-0000-0000-0000-504944564944 ("PIDVID").
				var d1 = (uint)(((pid & 0xFFFF) << 16) | (vid & 0xFFFF));
				productGuid = new Guid(d1, 0, 0, 0x00, 0x00, 0x50, 0x49, 0x44, 0x56, 0x49, 0x44);
			}
			else
			{
				// Deterministic hash of manufacturer + model so every user with the same machine shares one cloud ProductGuid.
				using (var md5 = System.Security.Cryptography.MD5.Create())
				{
					var key = "x360ce.FTMS|" + finalMfg.ToUpperInvariant() + "|" + productName.ToUpperInvariant();
					productGuid = new Guid(md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(key)));
				}
			}

			return new PluginDeviceIdentity
			{
				IsConnected = true,
				Manufacturer = finalMfg,
				ProductName = productName,
				VendorId = vid,
				ProductId = pid,
				ProductGuid = productGuid,
			};
		}

		static string CleanText(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return "";
			return value.Replace("\0", "").Trim();
		}

		static string CombineProductName(string manufacturer, string model, string advertised)
		{
			if (!string.IsNullOrEmpty(model))
			{
				if (!string.IsNullOrEmpty(manufacturer) && !model.StartsWith(manufacturer, StringComparison.OrdinalIgnoreCase))
					return manufacturer + " " + model;
				return model;
			}
			if (!string.IsNullOrEmpty(advertised))
			{
				if (!string.IsNullOrEmpty(manufacturer) && !advertised.StartsWith(manufacturer, StringComparison.OrdinalIgnoreCase))
					return manufacturer + " " + advertised;
				return advertised;
			}
			return manufacturer;
		}

		static string InferManufacturer(string productName)
		{
			var space = productName.IndexOf(' ');
			return space > 0 ? productName.Substring(0, space) : "Bluetooth Fitness";
		}

		/// <summary>True once the device let go: the worker lets go of the machine and ends.</summary>
		volatile bool _closing;

		/// <summary>True once the person declined to pair with the machine held: it is not tried again until another is held.</summary>
		volatile bool _declined;

		/// <summary>The machine connected now; null while none is.</summary>
		internal Want Held { get { return _held; } }

		/// <summary>The machine the device holds, connected or not; null while it holds none.</summary>
		internal Want Wanted { get { return _want; } }

		/// <summary>True once the device let go of its connection for good.</summary>
		internal bool Closed { get { return _closing; } }

		/// <summary>Connects to a machine, or to none with null, trying again quietly until it connects; the machine held before is let go of.</summary>
		/// <remarks>Called on the input thread, by the device's read: it takes no lock and waits for nothing.</remarks>
		internal void Hold(Want want)
		{
			if (_closing || (want == null ? _want == null : want.Same(_want)))
				return;
			_problem = null;
			_declined = false;
			_want = want;
			Changed();
			if (want != null)
				Wake();
		}

		/// <summary>Lets go of the machine for good, as the device is let go of: the worker lets go and ends.</summary>
		/// <remarks>Called on the input thread: it takes no lock and waits for nothing.</remarks>
		internal void Close()
		{
			_closing = true;
			_want = null;
			Changed();
		}

		/// <summary>Asks the person whether to pair with a machine, on the interface thread: set by the Connection page shown last, null while none is.</summary>
		internal volatile Action<FitnessPairing> AskPerson;

		/// <summary>Asks the person, from a thread of Windows'; a person who declines is not asked again until another machine is held.</summary>
		void AskPairing(FitnessPairing pairing)
		{
			var ask = AskPerson;
			if (ask == null)
			{
				pairing.Accept(false);
				return;
			}
			pairing.Answer.ContinueWith(answer =>
			{
				if (!answer.Result)
				{
					_declined = true;
					Changed();
				}
			});
			ask(pairing);
		}

		#endregion

		#region The worker

		/// <summary>The thread that works Bluetooth, from when the device first holds a machine until it let go.</summary>
		Thread _worker;

		/// <summary>Set once the worker ended.</summary>
		readonly ManualResetEventSlim _ended = new ManualResetEventSlim();

		/// <summary>When the game's force was last looked at, by <see cref="Now"/>.</summary>
		int _forceAt;

		/// <summary>When a closing x360ce stops waiting for every machine to be let go of, in <see cref="Stopwatch"/> ticks; 0 before it closes.</summary>
		static long _exitDeadline;

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
		/// <remarks>Every session waits until one deadline, the first handler's, so several devices together wait no longer than one.</remarks>
		void OnProcessExit(object sender, EventArgs e)
		{
			_closing = true;
			var now = Stopwatch.GetTimestamp();
			Interlocked.CompareExchange(ref _exitDeadline, now + ExitMilliseconds * Stopwatch.Frequency / 1000, 0);
			var left = (Interlocked.Read(ref _exitDeadline) - now) * 1000 / Stopwatch.Frequency;
			if (left > 0)
				_ended.Wait((int)left);
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
				_held = null;
				CanCommand = false;
				AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
				_ended.Set();
			}
		}

		/// <summary>Connects to the machine held, sends commands and lets go, as the device asks, until it let go for good.</summary>
		async Task Run()
		{
			IFitnessLink link = null;
			Want linked = null;
			var failures = 0;
			var retryAt = Now;
			while (true)
			{
				try
				{
					var want = _closing ? null : _want;
					if (link != null)
					{
						if (want == linked && link.Up)
						{
							await Serve(link);
							continue;
						}
						// Let go of: the device let go, another machine chosen, x360ce closing, or the link lost.
						var lost = want == linked;
						var name = linked.Name;
						_held = null;
						CanCommand = false;
						await link.Close();
						link = null;
						linked = null;
						_hardwareIdentity = null;
						if (lost)
						{
							_problem = "Lost the link to " + name + ".";
							Note("Link to " + name + " was lost.");
							failures = 0;
							retryAt = unchecked(Now + BackOffMilliseconds[0]);
						}
						else
							Note("Disconnected from " + name + ".");
						Changed();
						continue;
					}
					if (_closing)
						return;
					if (want != null && !_declined && unchecked(Now - retryAt) >= 0)
					{
						Forget();
						_name = want.Name;
						Changed();
						linked = want;
						link = NewLink(want.Address, want.Name);
						var problem = await link.Open();
						if (problem == null && want == _want && !_closing)
						{
							_problem = null;
							_held = want;
							failures = 0;
							Note("Connected to " + want.Name + ".");
							Changed();
							continue;
						}
						await link.Close();
						link = null;
						linked = null;
						if (problem == null)
							continue;
						Note(problem);
						_problem = problem;
						retryAt = unchecked(Now + BackOffMilliseconds[Math.Min(failures++, BackOffMilliseconds.Length - 1)]);
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
					_held = null;
					CanCommand = false;
					if (link != null)
					{
						link.Dispose();
						link = null;
						linked = null;
					}
					retryAt = unchecked(Now + BackOffMilliseconds[Math.Min(failures++, BackOffMilliseconds.Length - 1)]);
					Changed();
				}
			}
		}

		/// <summary>Sends the machine one command: the Connection page's first, else the game's force at most four times a second; else waits a moment.</summary>
		async Task Serve(IFitnessLink link)
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
			var driver = Driver;
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

		/// <summary>Forgets what one connection learned, so the next starts from nothing.</summary>
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

		/// <summary>An exception from Bluetooth, in words.</summary>
		internal static string Problem(Exception ex)
		{
			return TooOld(ex) ? NeedsWindows10 : "Bluetooth stopped with an error: " + Inner(ex).Message;
		}

		#endregion

		#region Bluetooth

		/// <summary>One connection to a machine, made and let go of on the worker: one of the two places Windows' Bluetooth types are named.</summary>
		sealed class Link : IFitnessLink
		{
			public Link(FitnessSession owner, ulong address, string name)
			{
				_owner = owner;
				_address = address;
				_name = name;
			}

			readonly ulong _address;
			readonly string _name;

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

			public bool Up { get { return _device != null && !_lost; } }

			public async Task<string> Open()
			{
				var name = _name;
				_device = await Wait(BluetoothLEDevice.FromBluetoothAddressAsync(_address));
				if (_device == null)
					return name + " could not be reached: make sure it is on and nearby.";
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
					return name + " did not answer (" + services.Status + "): make sure no other app is connected to it.";
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
				await ReadDeviceInformation(name);
				return null;
			}

			/// <summary>Reads standard Bluetooth Device Information Service (0x180A) so x360ce records the real machine manufacturer, model and VID/PID.</summary>
			async Task ReadDeviceInformation(string advertisedName)
			{
				string manufacturer = null;
				string modelNumber = null;
				byte[] pnpId = null;
				try
				{
					var dis = await Wait(_device.GetGattServicesForUuidAsync(BluetoothUuidHelper.FromShortId(Ftms.DeviceInformationServiceId), BluetoothCacheMode.Uncached));
					if (dis.Status == GattCommunicationStatus.Success && dis.Services.Count > 0)
					{
						var chars = await Wait(dis.Services[0].GetCharacteristicsAsync(BluetoothCacheMode.Uncached));
						if (chars.Status == GattCommunicationStatus.Success)
						{
							foreach (var c in chars.Characteristics)
							{
								var id = BluetoothUuidHelper.TryGetShortId(c.Uuid);
								if (!id.HasValue)
									continue;
								var sid = (ushort)id.Value;
								if (sid != Ftms.ManufacturerNameId && sid != Ftms.ModelNumberId && sid != Ftms.PnpId)
									continue;
								var read = await Wait(c.ReadValueAsync(BluetoothCacheMode.Uncached));
								if (read.Status != GattCommunicationStatus.Success || read.Value == null)
									continue;
								var bytes = Bytes(read.Value);
								if (sid == Ftms.ManufacturerNameId)
									manufacturer = System.Text.Encoding.UTF8.GetString(bytes);
								else if (sid == Ftms.ModelNumberId)
									modelNumber = System.Text.Encoding.UTF8.GetString(bytes);
								else if (sid == Ftms.PnpId)
									pnpId = bytes;
							}
						}
					}
				}
				catch (Exception)
				{
					// Device Information Service is optional; fall back to the BLE device's advertised name.
				}
				var realName = !string.IsNullOrWhiteSpace(_device.Name) ? _device.Name : advertisedName;
				_owner.SetHardwareIdentity(FitnessSession.BuildHardwareIdentity(realName, manufacturer, modelNumber, pnpId));
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
			/// as the machine can, so the person compares a number on both whenever the machine can show one: the device's
			/// Connection page asks them, and with no page open the machine waits until the next try.
			/// </summary>
			/// <returns>Null once paired; else why not.</returns>
			async Task<string> Pair(string name)
			{
				var pairing = _device.DeviceInformation.Pairing;
				if (pairing.IsPaired)
					return name + " refused access although it is paired: remove it in Windows Settings under Bluetooth & devices.";
				if (!pairing.CanPair)
					return name + " asks to be paired, but Windows cannot pair with it.";
				if (_owner.AskPerson == null)
					return name + " asks to be paired: open this device's Connection page, which asks you.";
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

			/// <summary>Asks the person through the Connection page, and accepts once they say yes; Windows waits for the answer.</summary>
			void OnPairingRequested(DeviceInformationCustomPairing sender, DevicePairingRequestedEventArgs args)
			{
				var kind = args.PairingKind == DevicePairingKinds.ConfirmPinMatch ? FitnessPairingKind.ConfirmPinMatch
					: args.PairingKind == DevicePairingKinds.DisplayPin ? FitnessPairingKind.DisplayPin
					: FitnessPairingKind.ConfirmOnly;
				var pairing = new FitnessPairing(_name, kind, args.Pin ?? "");
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

			/// <remarks>Windows closes the link once nothing holds it.</remarks>
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
							_owner.Note(confirmed ? _name + " stopped sending values to x360ce." : _name + " did not confirm that it stopped sending values.");
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
					_owner.Note("Link to " + _name + " is up.");
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

		/// <summary>Listens for advertisements while a Connection page scans, on a pool thread: the other place Windows' Bluetooth types are named.</summary>
		internal sealed class Scanner
		{
			public Scanner(FitnessScan owner)
			{
				_owner = owner;
			}

			readonly FitnessScan _owner;
			BluetoothLEAdvertisementWatcher _watcher;
			static readonly Guid ServiceUuid = BluetoothUuidHelper.FromShortId(Ftms.ServiceId);

			/// <summary>True from Start until Stop, or until Windows stopped the scan by itself.</summary>
			public bool Running { get { return _running; } }
			volatile bool _running;

			public void Start()
			{
				_watcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };
				_watcher.Received += OnReceived;
				_watcher.Stopped += OnStopped;
				_running = true;
				_watcher.Start();
			}

			public void Stop()
			{
				_running = false;
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

			/// <summary>Windows stopped the scan by itself, as when Bluetooth is off; a scan stopped by x360ce says nothing.</summary>
			void OnStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
			{
				_running = false;
				if (args.Error == BluetoothError.Success)
					return;
				_owner.Stopped(args.Error == BluetoothError.RadioNotAvailable ? "Bluetooth is off. Turn it on in Windows Settings, then press Scan."
					: args.Error == BluetoothError.DisabledByPolicy ? "Scanning is blocked by a Bluetooth policy on this computer."
					: "Scanning stopped (" + args.Error + "). Press Scan to try again.");
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

		#endregion

		/// <summary>A Bluetooth address as six bytes in hexadecimal, separated by colons.</summary>
		internal static string AddressText(ulong address)
		{
			var hex = address.ToString("X12", CultureInfo.InvariantCulture);
			return string.Join(":", Enumerable.Range(0, 6).Select(i => hex.Substring(i * 2, 2)));
		}

		/// <summary>Reads an address as <see cref="AddressText"/> writes it, with or without its colons; 0 when the text is not one.</summary>
		internal static ulong ParseAddress(string text)
		{
			var hex = (text ?? "").Trim().Replace(":", "");
			ulong address;
			return hex.Length == 12 && ulong.TryParse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out address) ? address : 0;
		}
	}

	/// <summary>The machines heard over Bluetooth, which every Fitness Machine device's Connection page lists: a computer has one radio, so one list.</summary>
	/// <remarks>
	/// A page starts and stops the scan. Windows' watcher is started and stopped on a pool thread, never on the interface
	/// thread, and its advertisements come on threads of Windows' (<see cref="Hear"/>); the list is kept under a lock the
	/// input thread never takes.
	/// </remarks>
	sealed class FitnessScan
	{
		/// <summary>The list of the process.</summary>
		internal static readonly FitnessScan Shared = new FitnessScan();

		readonly object _lock = new object();

		/// <summary>Held while the watcher is started or stopped, so the two never overlap.</summary>
		readonly object _gate = new object();

		/// <summary>The devices heard, in the order heard.</summary>
		readonly List<FitnessSeen> _machines = new List<FitnessSeen>();

		/// <summary>The addresses that said they are fitness machines: a machine often says so in another advertisement than the one with its name.</summary>
		readonly HashSet<ulong> _fitnessMachines = new HashSet<ulong>();

		/// <summary>Windows' watcher, touched only on a pool thread with the gate held; null while none runs.</summary>
		FitnessSession.Scanner _scanner;

		volatile bool _scanning;
		volatile bool _showAll;
		volatile string _problem;
		int _version;

		/// <summary>Raised by every change the Connection page shows, so it draws only when something changed.</summary>
		internal int Version { get { return Volatile.Read(ref _version); } }

		void Changed()
		{
			Interlocked.Increment(ref _version);
		}

		internal bool Scanning { get { return _scanning; } }

		/// <summary>True while every device that says its name is listed, not only fitness machines.</summary>
		internal bool ShowAll { get { return _showAll; } }

		/// <summary>Why the last scan stopped or could not start; null while nothing went wrong.</summary>
		internal string Problem { get { return _problem; } }

		/// <summary>Starts looking for machines, with a new list: fitness machines, and with <paramref name="showAll"/> every device that says its name.</summary>
		internal void Start(bool showAll)
		{
			lock (_lock)
			{
				_machines.Clear();
				_showAll = showAll;
			}
			_problem = null;
			_scanning = true;
			Changed();
			Follow();
		}

		/// <summary>Stops looking; the machines heard stay listed.</summary>
		internal void Stop()
		{
			if (!_scanning)
				return;
			_scanning = false;
			Changed();
			Follow();
		}

		/// <summary>Windows stopped the scan by itself: says why.</summary>
		internal void Stopped(string problem)
		{
			_problem = problem;
			_scanning = false;
			Changed();
			Follow();
		}

		/// <summary>Starts or stops Windows' watcher, on a pool thread, as <see cref="Scanning"/> says: the last call wins, whatever order the threads run in.</summary>
		void Follow()
		{
			Task.Run(() =>
			{
				lock (_gate)
				{
					try
					{
						Apply();
					}
					catch (Exception ex)
					{
						_scanner = null;
						_scanning = false;
						_problem = FitnessSession.Problem(ex);
						Changed();
					}
				}
			});
		}

		/// <summary>Makes the watcher run while scanning and stop otherwise. Called with the gate held, on a pool thread, where a Windows without Bluetooth's types throws.</summary>
		void Apply()
		{
			var scanner = _scanner;
			if (scanner != null && (!_scanning || !scanner.Running))
			{
				_scanner = null;
				scanner.Stop();
			}
			if (_scanning && _scanner == null)
			{
				scanner = new FitnessSession.Scanner(this);
				_scanner = scanner;
				scanner.Start();
			}
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
	}

	/// <summary>A device heard while scanning, kept up to date by its later advertisements, or the machine chosen: a row of the Connection page's list.</summary>
	sealed class FitnessSeen
	{
		public FitnessSeen(ulong address)
		{
			Address = address;
		}

		/// <summary>The machine a device's Machine setting names, listed while the scan has not heard it.</summary>
		public static FitnessSeen Chosen(FitnessSession.Want want)
		{
			return new FitnessSeen(want.Address) { Name = want.Name, Types = "chosen, not heard yet", Signal = NoSignal };
		}

		/// <summary>The signal of a machine not heard.</summary>
		public const int NoSignal = int.MinValue;

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

		/// <summary>The signal, such as "-78 dBm", or empty while the device was not heard.</summary>
		public string SignalText
		{
			get
			{
				var signal = Signal;
				return signal == NoSignal ? "" : signal.ToString(CultureInfo.InvariantCulture) + " dBm";
			}
		}
	}

	/// <summary>One line of the values on the Connection page: its name, its newest value, and how often it changes.</summary>
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

	/// <summary>A machine asking to be paired, as the Connection page asks the person: the question, and the answer once given.</summary>
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
}
