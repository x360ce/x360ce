using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

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

	/// <summary>
	/// The device's settings, which x360ce keeps for each device and shows on its tab's Properties page, and its statuses,
	/// which the page shows read only and AI assistants read with the device's read tool.
	/// </summary>
	public partial class FitnessMachine
	{
		#region Settings

		[Category("Machine"), DisplayName("Machine")]
		[Description("The machine this device connects to while ticked: its name and Bluetooth address. The Connection page sets it.")]
		public string Machine
		{
			get { return _machine; }
			set { _machine = value ?? ""; }
		}
		volatile string _machine = "";

		[Category("Ranges"), DisplayName("Max speed (km/h)")]
		[Description("The speed that moves axes 1, 2 and 3 and slider 1 all the way, to 65535.")]
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
		[Description("What the device's connection to its machine is doing: no machine chosen, waiting for the device to be ticked, "
			+ "connecting, connected, or why it cannot.")]
		public string Connection
		{
			get
			{
				var chosen = FitnessSession.Want.Parse(_machine);
				if (chosen == null)
					return _machine.Trim().Length == 0
						? "No machine chosen. Press Scan on the Connection page, then select the machine."
						: "Machine holds no Bluetooth address. Select the machine on the Connection page.";
				if (!Reading)
					return chosen.Name + " is chosen. x360ce connects to it while this device is ticked, on the Devices tab and in its controller's list.";
				return _session.StatusText ?? "Connecting to " + chosen.Name + "...";
			}
		}

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
	}
}
