// @under-test: Plugins/FitnessMachine/FitnessMachine.cs, Plugins/FitnessMachine/FitnessMachine.Connection.cs, Plugins/FitnessMachine/FitnessMachine.Properties.cs, Plugins/FitnessMachine/FitnessMachine.Help.cs, PluginApi/IPluginForceFeedback.cs, App.v4/Plugins/PluginDevices.cs, App.v4/Common/DInput/DInputHelper.Plugins.cs, App.v4/Controls/PadTabPages/PluginDeviceUserControl.cs
// @area: plugins   @layer: unit
using JocysCom.ClassLibrary.Controls.UiTree;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.App.DInput;
using x360ce.App.Plugins;
using x360ce.Engine;
using x360ce.Engine.Data;
using x360ce.Plugins;
using static x360ce.Tests.DeviceRoutingFixtures;

namespace x360ce.Tests
{
	/// <summary>
	/// The Fitness Machine, a plugin embedded in x360ce, makes an exercise bike read over Bluetooth a controller, and the
	/// game's force the machine's resistance or road grade; its device's Connection page finds the machine and chooses it.
	/// Every value reaches a device's session through Take, and every force command leaves a device through NextCommand,
	/// so these tests give a session of their own the bytes a machine sends and read the bytes it would send, with no
	/// Bluetooth: either its worker is taken, so nothing it is asked reaches the radio, or its links are fakes that count
	/// what the worker asks of them. And x360ce gives a plugin that takes force the game's force, only when it changes.
	/// </summary>
	[TestClass]
	public class FitnessMachineTest
	{
		static readonly Guid Product = new Guid("6e1793da-b362-49b0-ad25-08433feac3f8");

		const ushort FeatureId = 0x2ACC;
		const ushort CrossTrainerDataId = 0x2ACE;
		const ushort IndoorBikeDataId = 0x2AD2;
		const ushort ResistanceRangeId = 0x2AD6;

		/// <summary>
		/// Indoor Bike Data as the Virtual Fitness Machine Transmitter sends it: 25.00 km/h, 80 rpm, 1234 m, resistance 5,
		/// 150 W, 120 bpm and 10 minutes.
		/// </summary>
		static readonly byte[] BikeData = { 0x74, 0x0A, 0xC4, 0x09, 0xA0, 0x00, 0xD2, 0x04, 0x00, 0x05, 0x00, 0x96, 0x00, 0x78, 0x58, 0x02 };

		/// <summary>The Feature value of a bike that measures cadence, distance, resistance, heart rate, time and power, and takes resistance, power and road grade targets.</summary>
		static readonly byte[] BikeFeature = { 0x86, 0x54, 0x00, 0x00, 0x0C, 0x20, 0x00, 0x00 };

		/// <summary>A resistance range of 0 to 20 levels, a level at a time, in tenths.</summary>
		static readonly byte[] BikeRange = { 0x00, 0x00, 0xC8, 0x00, 0x0A, 0x00 };

		static PluginDeviceType Type()
		{
			var type = TestControllerDevice.Manager.FindDeviceType(Product);
			Assert.IsNotNull(type, "The Fitness Machine plugin is not embedded in x360ce.");
			return type;
		}

		const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

		/// <summary>A type of the plugin that x360ce does not see, by its name.</summary>
		static System.Type Internal(string name)
		{
			return Type().Type.Assembly.GetType("x360ce.Plugins.Samples." + name, true);
		}

		/// <summary>A session of its own, whose worker is taken by a thread never started: what it is asked is kept, and nothing reaches Bluetooth.</summary>
		static object NewSession()
		{
			var session = Activator.CreateInstance(Internal("FitnessSession"), true);
			Internal("FitnessSession").GetField("_worker", Any).SetValue(session, new Thread(() => { }));
			return session;
		}

		/// <summary>A new device, as the engine makes one, of a session of its own unless one is given.</summary>
		static IPluginDevice NewDevice(object session = null)
		{
			return (IPluginDevice)Activator.CreateInstance(Type().Type, Any, null, new[] { session ?? NewSession() }, null);
		}

		static object Session(IPluginDevice device)
		{
			return device.GetType().GetProperty("Session", Any).GetValue(device);
		}

		static object Call(object target, string name, params object[] arguments)
		{
			return target.GetType().GetMethod(name, Any).Invoke(target, arguments);
		}

		/// <summary>A field's or a property's value, public or not.</summary>
		static object Get(object target, string name)
		{
			var field = target.GetType().GetField(name, Any);
			return field != null ? field.GetValue(target) : target.GetType().GetProperty(name, Any).GetValue(target);
		}

		static void Take(IPluginDevice device, ushort characteristicId, params byte[] value)
		{
			Call(Session(device), "Take", characteristicId, value);
		}

		/// <summary>Indoor Bike Data with the transmitter's fields, the values given.</summary>
		static byte[] Bike(int hundredthsKmh, int halfRpm, int level, int heartRate)
		{
			return new byte[]
			{
				0x74, 0x0A, (byte)hundredthsKmh, (byte)(hundredthsKmh >> 8), (byte)halfRpm, (byte)(halfRpm >> 8), 0xD2, 0x04, 0x00,
				(byte)level, (byte)(level >> 8), 0x96, 0x00, (byte)heartRate, 0x58, 0x02,
			};
		}

		static byte[] NextCommand(IPluginDevice device)
		{
			return (byte[])device.GetType().GetMethod("NextCommand").Invoke(device, null);
		}

		static void SetForce(IPluginDevice device, int largeMotor)
		{
			((IPluginForceFeedback)device).SetForce(largeMotor, 0);
		}

		/// <summary>Gives the device a setting's value, as the engine gives it the value kept for the device.</summary>
		static void Set(IPluginDevice device, string name, string value)
		{
			Type().Settings.Single(x => x.Name == name).Write(device, value);
		}

		static string Status(IPluginDevice device, string name)
		{
			return (string)device.GetType().GetProperty(name).GetValue(device);
		}

		static PluginState Read(IPluginDevice device)
		{
			var state = new PluginState();
			device.Read(state);
			return state;
		}

		static void AreEqual(byte[] expected, byte[] actual, string message)
		{
			Assert.IsNotNull(actual, message + " No command was due.");
			CollectionAssert.AreEqual(expected, actual, message + " Sent: " + BitConverter.ToString(actual));
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("It has the controls and the product it claims, takes force feedback, and its settings in the order of its tab")]
		public void It_has_the_controls_and_settings_it_claims()
		{
			var device = Type().NewInstance();
			((IDisposable)device).Dispose();
			Assert.AreEqual("Fitness Machine", device.Name);
			Assert.AreEqual(Product, device.ProductGuid, "Saved mappings find the device by its product.");
			Assert.AreEqual(6, device.AxisCount);
			Assert.AreEqual(2, device.SliderCount);
			Assert.AreEqual(0, device.PovCount);
			Assert.AreEqual(4, device.ButtonCount);
			Assert.IsTrue(device is IPluginForceFeedback, "It does not take the game's force.");
			Assert.IsTrue(device is IPluginMapping, "It recommends no mapping.");
			Assert.AreEqual("righttrigger:a0,lefttrigger:a1", Type().Mapping, "Its recommended mapping was not read as it loaded.");
			Assert.IsFalse(string.IsNullOrWhiteSpace(Type().Description), "It does not explain itself.");
			// The machine the Connection page chooses is a setting, kept for each device.
			CollectionAssert.AreEqual(new[]
			{
				"Machine", "MaxSpeed", "MaxCadence", "MaxPower", "MinHeartRate", "MaxHeartRate", "Reverse", "ReverseGear",
				"ForceTo", "ForceResistance", "ForceGrade", "HeartRateLimit",
				"Connection", "Features", "Speed", "Cadence", "Power", "HeartRate", "Resistance", "Distance", "ElapsedTime", "Force", "Updates",
			}, Type().Settings.Select(x => x.Name).ToArray());
			Assert.AreEqual("FitnessConnection", Type().ControlType?.Name, "The device's Connection page is not its own control.");
			Assert.AreEqual(1, Type().File.Assembly.GetExportedTypes().Count(x => typeof(Control).IsAssignableFrom(x)),
				"The plugin adds a control other than the Connection page.");
			var help = Type().Description;
			foreach (var part in new[]
			{
				"Experimental: walk, drive or row in games with a fitness machine. Tested on a Tunturi E60R exercise bike only. Use it with care and at your own risk.",
				"`[Connection]`", "`[Scan]`", "`[Show all Bluetooth devices]`", "`[Enabled]`",
				"| Axis 1 | Forward |", "| Axis 2 | Reverse |", "| Axis 3 | Walk |", "| Axis 4 | Cadence |", "| Axis 5 | Power |", "| Axis 6 | HeartRate |",
				"| Slider 1 | Speed |", "| Slider 2 | Resistance |", "| Button 1 | Moving |", "| Button 2 | Reverse |", "| Button 3 | Heartbeat |", "| Button 4 | HR high |",
				"65535, all the way, at `[Max speed (km/h)]`", "`[Reverse]`", "`[Reverse gear]`", "`=s1*(1-c1.d2.b1)`",
				"`[Force Feedback]`", "`[Heart rate limit (bpm)]`", "`[Right Trigger]` takes `Axis 1`", "`IAxis 3`",
				"`[Start]`", "`[Target power]`", "`[Set]`", "Bluetooth cannot connect to itself",
			})
				StringAssert.Contains(help, part, "The help does not explain it.");
			Assert.IsFalse(help.Contains("Fitness tab"), "The help sends the person to a Fitness tab x360ce no longer has.");
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("Indoor Bike Data as a machine sends it moves each control, and the status shows each value with its unit")]
		public void Indoor_bike_data_moves_the_controls()
		{
			var device = NewDevice();
			Take(device, IndoorBikeDataId, BikeData);
			var state = Read(device);
			// 25 km/h of 40, 80 rpm of 120, 150 W of 400, 120 bpm from 60 to 190, resistance 5 of 100.
			Assert.AreEqual(40959, state.Axes[0], "Forward");
			Assert.AreEqual(0, state.Axes[1], "Reverse");
			Assert.AreEqual(12288, state.Axes[2], "Walk: up from the middle by the speed.");
			Assert.AreEqual(43690, state.Axes[3], "Cadence");
			Assert.AreEqual(24576, state.Axes[4], "Power");
			Assert.AreEqual(30247, state.Axes[5], "Heart rate");
			Assert.AreEqual(40959, state.Sliders[0], "Speed");
			Assert.AreEqual(3277, state.Sliders[1], "Resistance, of 0 to 100 while the machine's range is not known.");
			Assert.IsTrue(state.Buttons[0], "Moving");
			Assert.IsFalse(state.Buttons[1], "Reverse");
			Assert.IsFalse(state.Buttons[3], "Heart rate high, with no limit set.");
			Assert.AreEqual("25.0 km/h", Status(device, "Speed"));
			Assert.AreEqual("80 rpm", Status(device, "Cadence"));
			Assert.AreEqual("150 W", Status(device, "Power"));
			Assert.AreEqual("120 bpm", Status(device, "HeartRate"));
			Assert.AreEqual("5", Status(device, "Resistance"));
			Assert.AreEqual("1.23 km", Status(device, "Distance"));
			Assert.AreEqual("0:10:00", Status(device, "ElapsedTime"));

			Take(device, ResistanceRangeId, BikeRange);
			Assert.AreEqual(16384, Read(device).Sliders[1], "Resistance 5 is not a quarter of the machine's range of 0 to 20.");
			Assert.AreEqual("5 (0 to 20)", Status(device, "Resistance"));
			Take(device, FeatureId, BikeFeature);
			Assert.AreEqual("Measures: cadence, distance, resistance, heart rate, elapsed time, power. Targets: resistance, power, road grade.",
				Status(device, "Features"));
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("A record split over two packets keeps the speed the first packet without More Data gave")]
		public void A_packet_with_more_data_keeps_the_speed()
		{
			var device = NewDevice();
			Take(device, IndoorBikeDataId, BikeData);
			// More Data and the cadence alone: 90 rpm, no speed.
			Take(device, IndoorBikeDataId, 0x05, 0x00, 0xB4, 0x00);
			var state = Read(device);
			Assert.AreEqual(40959, state.Axes[0], "The speed was lost with a packet that carries none.");
			Assert.AreEqual(49151, state.Axes[3], "The cadence the second packet carries was not taken.");
			// A packet that ends before its fields keeps the fields it holds.
			Take(device, IndoorBikeDataId, 0x74, 0x0A, 0xE8, 0x03);
			Assert.AreEqual(16384, Read(device).Axes[0], "The speed of a short packet was not taken.");
			Assert.AreEqual(49151, Read(device).Axes[3], "A field a short packet does not hold lost its value.");
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("Reverse comes from the Reverse setting, from the machine's resistance at Reverse gear or below, or from a cross trainer going backwards")]
		public void Reverse_comes_from_the_setting_the_gear_or_the_cross_trainer()
		{
			var device = NewDevice();
			Take(device, IndoorBikeDataId, BikeData);
			Set(device, "Reverse", "True");
			var state = Read(device);
			Assert.AreEqual(0, state.Axes[0], "Forward moves in reverse.");
			Assert.AreEqual(40959, state.Axes[1], "Reverse does not move with the speed.");
			Assert.AreEqual(40959, state.Sliders[0], "The speed slider does not move both ways.");
			Assert.IsTrue(state.Buttons[1], "The Reverse button is not held.");

			device = NewDevice();
			Set(device, "ReverseGear", "5");
			Take(device, IndoorBikeDataId, Bike(2500, 160, 5, 120));
			Assert.IsTrue(Read(device).Buttons[1], "Resistance 5 with Reverse gear 5 does not drive in reverse.");
			Assert.AreEqual(40959, Read(device).Axes[1]);
			Take(device, IndoorBikeDataId, Bike(2500, 160, 6, 120));
			Assert.IsFalse(Read(device).Buttons[1], "Resistance 6 above Reverse gear 5 drives in reverse.");
			Assert.AreEqual(40959, Read(device).Axes[0]);
			Set(device, "ReverseGear", "0");
			Take(device, IndoorBikeDataId, Bike(2500, 160, 0, 120));
			Assert.IsFalse(Read(device).Buttons[1], "Reverse gear 0 drives in reverse.");

			device = NewDevice();
			// Cross Trainer Data: Movement Direction backward, then forward, each at 25 km/h.
			Take(device, CrossTrainerDataId, 0x00, 0x80, 0x00, 0xC4, 0x09);
			state = Read(device);
			Assert.IsTrue(state.Buttons[1], "A cross trainer going backwards does not drive in reverse.");
			Assert.AreEqual(40959, state.Axes[1]);
			Take(device, CrossTrainerDataId, 0x00, 0x00, 0x00, 0xC4, 0x09);
			Assert.IsFalse(Read(device).Buttons[1], "A cross trainer going forwards drives in reverse.");
		}

		[TestMethod, TestCategory("plugins")]
		[Description("The walk axis rests in the middle, moves up going forward and down in reverse")]
		public void The_walk_axis_moves_up_forward_and_down_in_reverse()
		{
			var device = NewDevice();
			Assert.AreEqual(PluginState.Middle, Read(device).Axes[2], "A machine that sent nothing does not rest in the middle.");
			Take(device, IndoorBikeDataId, Bike(1000, 0, 5, 0));
			Assert.AreEqual(24575, Read(device).Axes[2], "10 km/h of 40 does not move a quarter of the way up.");
			Set(device, "Reverse", "True");
			Assert.AreEqual(40959, Read(device).Axes[2], "10 km/h of 40 in reverse does not move a quarter of the way down.");
		}

		[TestMethod, TestCategory("plugins")]
		[Description("A speed past Max speed holds the speed axes at their ends")]
		public void Speed_past_max_speed_holds_the_axes_at_their_ends()
		{
			var device = NewDevice();
			Take(device, IndoorBikeDataId, Bike(5000, 160, 5, 120));
			var state = Read(device);
			Assert.AreEqual(PluginState.Max, state.Axes[0]);
			Assert.AreEqual(0, state.Axes[2], "Walk is not all the way up.");
			Assert.AreEqual(PluginState.Max, state.Sliders[0]);
			Set(device, "Reverse", "True");
			state = Read(device);
			Assert.AreEqual(PluginState.Max, state.Axes[1]);
			Assert.AreEqual(PluginState.Max, state.Axes[2], "Walk is not all the way down.");
			Set(device, "MaxSpeed", "80");
			Assert.AreEqual(40959, Read(device).Axes[1], "50 km/h of 80 is not five eighths of the way.");
		}

		[TestMethod, TestCategory("plugins")]
		[Description("Button 3 is held for the first quarter of each heartbeat, and never without a heart rate")]
		public void Button_3_beats_with_the_heart()
		{
			var device = NewDevice();
			// 120 bpm: a beat every 500 ms, held for 125.
			Take(device, IndoorBikeDataId, Bike(2500, 160, 5, 120));
			var state = new PluginState();
			var watch = Stopwatch.StartNew();
			long held = 0;
			long last = 0;
			var presses = 0;
			var wasHeld = false;
			while (watch.ElapsedMilliseconds < 1500)
			{
				device.Read(state);
				var now = watch.ElapsedMilliseconds;
				if (state.Buttons[2])
				{
					held += now - last;
					if (!wasHeld)
						presses++;
				}
				wasHeld = state.Buttons[2];
				last = now;
				Thread.Sleep(1);
			}
			Assert.IsTrue(presses >= 2 && presses <= 4, presses + " beats in 1.5 seconds at 120 bpm.");
			Assert.IsTrue(held > 1500 * 0.12 && held < 1500 * 0.38, "Held " + held + " ms of 1500, not a quarter.");

			Take(device, IndoorBikeDataId, Bike(2500, 160, 5, 0));
			watch.Restart();
			while (watch.ElapsedMilliseconds < 300)
			{
				device.Read(state);
				Assert.IsFalse(state.Buttons[2], "The heartbeat button is held with no heart rate.");
				Thread.Sleep(1);
			}
		}

		[TestMethod, TestCategory("plugins")]
		[Description("A machine that sends nothing for more than 3 seconds reads as standing still")]
		public void A_machine_gone_quiet_reads_as_standing_still()
		{
			var device = NewDevice();
			Take(device, IndoorBikeDataId, BikeData);
			Assert.AreEqual(40959, Read(device).Axes[0]);
			Thread.Sleep(3200);
			var state = Read(device);
			Assert.AreEqual(0, state.Axes[0], "A machine gone quiet keeps the car going.");
			Assert.AreEqual(PluginState.Middle, state.Axes[2], "A machine gone quiet keeps walking.");
			Assert.AreEqual(0, state.Axes[5]);
			Assert.AreEqual(0, state.Sliders[1]);
			Assert.AreEqual(0, state.Buttons.Count(x => x), "A button is held by a machine gone quiet.");
			Assert.AreEqual("", Status(device, "Speed"), "The status shows a speed the machine stopped sending.");
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("The game's force adds resistance to the level the person set, asks for control once first, sends a new target only a whole level away, and gives the level back")]
		public void Force_sets_the_resistance_a_level_at_a_time()
		{
			var device = NewDevice();
			SetForce(device, 22937);
			Assert.IsNull(NextCommand(device), "A target was due before the machine said what it takes.");
			Take(device, FeatureId, BikeFeature);
			Take(device, ResistanceRangeId, BikeRange);
			SetForce(device, 0);
			Take(device, IndoorBikeDataId, BikeData);
			Assert.IsNull(NextCommand(device), "A target was due with no force.");
			// 35 %, of 10 extra levels, on level 5: 8.5, sent in tenths.
			SetForce(device, 22937);
			AreEqual(new byte[] { 0x00 }, NextCommand(device), "Control was not asked for before the first target.");
			AreEqual(new byte[] { 0x04, 85, 0 }, NextCommand(device), "The force did not add 3.5 levels to 5.");
			Assert.AreEqual("35 % → resistance 8.5", Status(device, "Force"));
			Assert.IsNull(NextCommand(device), "The same target was due twice.");
			// 40 %: 9, half a level from 8.5.
			SetForce(device, 26214);
			Assert.IsNull(NextCommand(device), "A target less than a level from the last one was due.");
			// The machine reports the level x360ce set: that is not the person's level.
			Take(device, IndoorBikeDataId, Bike(2500, 160, 9, 120));
			// 50 %: 10.
			SetForce(device, 32768);
			AreEqual(new byte[] { 0x04, 100, 0 }, NextCommand(device), "A target a level and a half away was not due.");
			SetForce(device, 0);
			AreEqual(new byte[] { 0x04, 50, 0 }, NextCommand(device), "The level the person set was not given back.");
			Assert.IsNull(NextCommand(device));
			// Held inside the machine's range: 5 and 100 extra levels is past its 20.
			Set(device, "ForceResistance", "100");
			SetForce(device, PluginState.Max);
			AreEqual(new byte[] { 0x04, 200, 0 }, NextCommand(device), "The target went past the machine's range.");
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("The game's force climbs a road grade, half a percent at a time, and a target of the other kind is given back first")]
		public void Force_sets_the_road_grade_half_a_percent_at_a_time()
		{
			var device = NewDevice();
			Set(device, "ForceTo", "Grade");
			Take(device, FeatureId, BikeFeature);
			Take(device, IndoorBikeDataId, BikeData);
			// 35 % of 8 %: 2.8 %, sent in hundredths with a road's rolling and wind resistance.
			SetForce(device, 22937);
			AreEqual(new byte[] { 0x00 }, NextCommand(device), "Control was not asked for before the first target.");
			AreEqual(new byte[] { 0x11, 0, 0, 0x18, 0x01, 40, 51 }, NextCommand(device), "The force did not climb 2.8 %.");
			// 37 %: 2.96 %, and 40 %: 3.2 %, each less than half a percent from 2.8.
			SetForce(device, 24248);
			Assert.IsNull(NextCommand(device), "A grade less than half a percent from the last one was due.");
			SetForce(device, 26214);
			Assert.IsNull(NextCommand(device), "A grade less than half a percent from the last one was due.");
			// 45 %: 3.6 %.
			SetForce(device, 29491);
			AreEqual(new byte[] { 0x11, 0, 0, 0x68, 0x01, 40, 51 }, NextCommand(device), "A grade 0.8 % away was not due.");
			Assert.AreEqual("45 % → grade 3.6 %", Status(device, "Force"));
			Set(device, "ForceTo", "Resistance");
			AreEqual(new byte[] { 0x11, 0, 0, 0, 0, 40, 51 }, NextCommand(device), "The road was not made flat before the resistance was set.");
			AreEqual(new byte[] { 0x04, 95, 0 }, NextCommand(device), "45 % of 10 extra levels on level 5 was not set.");
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("At or above the heart rate limit the game's force adds nothing and button 4 is held")]
		public void At_the_heart_rate_limit_the_force_adds_nothing()
		{
			var device = NewDevice();
			Set(device, "HeartRateLimit", "100");
			Take(device, FeatureId, BikeFeature);
			Take(device, IndoorBikeDataId, Bike(2500, 160, 5, 120));
			SetForce(device, 22937);
			Assert.IsNull(NextCommand(device), "The force added resistance above the heart rate limit.");
			Assert.IsTrue(Read(device).Buttons[3], "Button 4 is not held above the heart rate limit.");
			Assert.AreEqual("Heart rate limit reached: resistance 5.", Status(device, "Force"));
			Take(device, IndoorBikeDataId, Bike(2500, 160, 5, 90));
			Assert.IsFalse(Read(device).Buttons[3], "Button 4 is held below the heart rate limit.");
			AreEqual(new byte[] { 0x00 }, NextCommand(device), "Control was not asked for before the first target.");
			AreEqual(new byte[] { 0x04, 85, 0 }, NextCommand(device), "The force added nothing below the heart rate limit.");
			Take(device, IndoorBikeDataId, Bike(2500, 160, 8, 100));
			AreEqual(new byte[] { 0x04, 50, 0 }, NextCommand(device), "The level the person set was not given back at the heart rate limit.");
		}

		[TestMethod, TestCategory("plugins")]
		[Description("A machine that takes no target of the kind chosen is sent none, and the status says so")]
		public void A_machine_that_takes_no_target_is_sent_none()
		{
			var device = NewDevice();
			SetForce(device, PluginState.Max);
			Assert.AreEqual("100 %.", Status(device, "Force"));
			Take(device, FeatureId, 0x86, 0x54, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);
			Take(device, IndoorBikeDataId, BikeData);
			Assert.IsNull(NextCommand(device), "A machine that takes no target was sent one.");
			Assert.AreEqual("The machine takes no resistance target.", Status(device, "Force"));
			Set(device, "ForceTo", "Grade");
			Assert.IsNull(NextCommand(device));
			Assert.AreEqual("The machine takes no road grade.", Status(device, "Force"));
			Set(device, "ForceTo", "Off");
			Assert.AreEqual("Off.", Status(device, "Force"));
			StringAssert.EndsWith(Status(device, "Features"), "Targets: none.");
		}

		[TestMethod, TestCategory("plugins")]
		[Description("Its Bluetooth code binds to the Bluetooth types of this Windows, so the worker can run: every method compiles, and none is called")]
		public void Its_bluetooth_code_binds_to_windows()
		{
			var metadata = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "WinMetadata", "Windows.Devices.winmd");
			if (!File.Exists(metadata))
				Assert.Inconclusive("This Windows has no WinRT metadata, so the plugin says it needs Windows 10 or later.");
			var prepared = 0;
			foreach (var type in Type().Type.Assembly.GetTypes().Where(x => !x.ContainsGenericParameters))
			{
				foreach (var method in type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
				{
					if (method.IsAbstract || method.ContainsGenericParameters)
						continue;
					RuntimeHelpers.PrepareMethod(method.MethodHandle);
					prepared++;
				}
			}
			Assert.IsTrue(prepared > 50, "Only " + prepared + " methods were compiled.");
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("The plugin's document names it, its controls and its product, and lists every setting on its tab")]
		public void Its_document_lists_its_settings()
		{
			var text = PluginDocs.Markdown(Type().File);
			StringAssert.StartsWith(text, "# FitnessMachine\n");
			StringAssert.Contains(text, "- Source: embedded in x360ce.exe");
			StringAssert.Contains(text, "## Fitness Machine");
			StringAssert.Contains(text, "- Product GUID: " + Product);
			StringAssert.Contains(text, "axes 6, sliders 2, hat switches 0, buttons 4");
			StringAssert.Contains(text, "- Machine (Machine, text, starts empty): ");
			Assert.IsFalse(text.Contains("Adds a page to the main window"), "The plugin claims a page of the main window.");
			StringAssert.Contains(text, "- The plugin’s own control, FitnessConnection, on the first page, before Properties.");
			StringAssert.Contains(text, "- Max speed (km/h) (Ranges, a number from 1 to 200, starts at 40): ");
			StringAssert.Contains(text, "- Reverse gear (Reverse, a number from 0 to 100, starts at 0): ");
			StringAssert.Contains(text, "- Force feedback to (Force feedback, one of: Off, Resistance, Road grade, starts at Resistance): ");
			StringAssert.Contains(text, "- Heart rate limit (bpm) (Heart rate, a number from 0 to 240, starts at 0): ");
			StringAssert.Contains(text, "- Connection (Status, read only): ");
			StringAssert.Contains(text, "- Updates per second (Status, read only): ");
			StringAssert.Contains(text, "in SDL’s form: righttrigger:a0,lefttrigger:a1\n");
			Assert.IsFalse(text.Contains("- Mapping ("), "Its recommended mapping is listed as a setting.");
		}

		/// <summary>A trigger's value, 0 to 255, from an axis's, as the engine converts it.</summary>
		static int Trigger(Map map, int axis)
		{
			return (byte)ConvertHelper.GetThumbValue(axis, map.DeadZone, map.AntiDeadZone, map.Linear, map.IsInverted, map.IsHalf, false);
		}

		[TestMethod, TestCategory("plugins"), TestCategory("mapping"), TestCategory("critical")]
		[Description("Auto Preset maps it as it recommends: forward, Axis 1, on Right Trigger and reverse, Axis 2, on Left Trigger, each from released standing still to pressed all the way at Max speed, and nothing else")]
		public void Auto_preset_puts_forward_and_reverse_on_the_triggers()
		{
			var ud = PluginDevices.NewUserDevice(Type());
			string note;
			var ps = x360ce.App.AutoMapHelper.GetAutoPreset(ud, out note);
			Assert.AreEqual("Auto Preset filled from the plugin’s recommended mapping.", note);
			Assert.AreEqual("a1", ps.RightTrigger, "Right Trigger is not the whole of Axis 1.");
			Assert.AreEqual("a2", ps.LeftTrigger, "Left Trigger is not the whole of Axis 2.");
			foreach (var code in SdlConverter.MappingBoxes.Where(x => x != MapCode.RightTrigger && x != MapCode.LeftTrigger))
				Assert.AreEqual("", SdlConverter.GetValue(ps, code), code + " was mapped; the walk axis and the rest are the person's to map.");
			Assert.AreEqual(ps.CleanAndGetCheckSum(), ps.PadSettingChecksum);
			var maps = new[]
			{
				new Map(MapCode.RightTrigger, ps.RightTrigger, TargetType.RightTrigger, ps.RightTriggerDeadZone, ps.RightTriggerAntiDeadZone, ps.RightTriggerLinear),
				new Map(MapCode.LeftTrigger, ps.LeftTrigger, TargetType.LeftTrigger, ps.LeftTriggerDeadZone, ps.LeftTriggerAntiDeadZone, ps.LeftTriggerLinear),
			};
			foreach (var map in maps)
			{
				Assert.IsFalse(map.IsHalf, map.Target + " reads half an axis, which leaves the first half of Max speed doing nothing.");
				Assert.AreEqual(0, Trigger(map, 0), map.Target + " is pressed standing still.");
				Assert.AreEqual(255, Trigger(map, PluginState.Max), map.Target + " is not pressed all the way at Max speed.");
				var half = Trigger(map, PluginState.Middle);
				Assert.IsTrue(half >= 127 && half <= 128, map.Target + " is at " + half + " of 255 at half of Max speed.");
			}
			// 25 km/h of 40 presses Right Trigger 25/40 of the way.
			var device = NewDevice();
			Take(device, IndoorBikeDataId, BikeData);
			Assert.AreEqual(159, Trigger(maps[0], Read(device).Axes[0]));
			Assert.AreEqual(0, Trigger(maps[1], Read(device).Axes[1]));
		}

		[TestMethod, TestCategory("plugins"), TestCategory("ui")]
		[Description("Its SDL page shows the row its plugin recommends, place by place, from the plugin, and offers to apply it")]
		public void Its_sdl_page_shows_the_row_it_recommends()
		{
			var ud = PluginDevices.NewUserDevice(Type());
			Ui.OnUiThread(() =>
			{
				using (var page = new x360ce.App.Controls.SdlUserControl())
				{
					var none = SdlDatabase.Merge();
					page.UpdateFrom(ud, new PadSetting(), none, none);
					Assert.IsNotNull(page.Translation, "The page shows no row: " + page.DeviceLabel.Text);
					StringAssert.Contains(page.DeviceLabel.Text, "Its plugin recommends a mapping");
					StringAssert.Contains(page.DeviceLabel.Text, SdlDevice.From(ud).Guid + ",Fitness Machine,righttrigger:a0,lefttrigger:a1,platform:Windows,");
					var places = (IEnumerable<SdlPositionRow>)page.PositionsDataGridView.DataSource;
					var right = places.First(x => x.Code == MapCode.RightTrigger);
					Assert.AreEqual("a0", right.Binding);
					Assert.AreEqual("Axis 1", right.Control);
					Assert.AreEqual(SdlTranslation.FromPlugin, right.From);
					Assert.IsTrue(right.Differs, "An empty box reads as the plugin's.");
					Assert.AreEqual("Axis 2", places.First(x => x.Code == MapCode.LeftTrigger).Control);
					Assert.IsTrue(page.ApplyButton.Enabled, "[Apply SDL Mapping] is off while the page shows the plugin's row.");
				}
			});
		}

		#region Connecting, and the Connection page

		const ulong Address = 0xA1B2C3D4E5F6;
		const ulong RowerAddress = 0xB1B2C3D4E5F6;

		/// <summary>The machine a session is asked to hold, as its address and name; null for none.</summary>
		static string Wanted(object session)
		{
			var want = Get(session, "Wanted");
			return want == null ? null : ((ulong)Get(want, "Address")).ToString("X12") + " " + Get(want, "Name");
		}

		/// <summary>True once the session's worker ended, as it does once its device let go.</summary>
		static bool Ended(object session)
		{
			return ((ManualResetEventSlim)Get(session, "_ended")).IsSet;
		}

		/// <summary>Waits a few seconds at most for a condition the session's worker brings about on its own thread.</summary>
		static void WaitUntil(Func<bool> condition, string message, int milliseconds = 5000)
		{
			var watch = Stopwatch.StartNew();
			while (!condition())
			{
				if (watch.ElapsedMilliseconds > milliseconds)
					Assert.Fail(message);
				Thread.Sleep(20);
			}
		}

		/// <summary>
		/// Links to machines with no Bluetooth: each link the worker makes is a fake that connects when <see cref="Problems"/>
		/// has nothing left to say, answers every command with success, and notes what the worker asked of it.
		/// </summary>
		sealed class FakeRadio
		{
			/// <summary>What the next links say when they cannot connect, one each, before one connects.</summary>
			public readonly Queue<string> Problems = new Queue<string>();

			readonly List<string> _calls = new List<string>();

			/// <summary>What the worker asked, in order, such as "Open EJT" and "Close EJT".</summary>
			public string[] Calls { get { lock (_calls) return _calls.ToArray(); } }

			public void Add(string call)
			{
				lock (_calls)
					_calls.Add(call);
			}

			/// <summary>A new link to a machine, as the session's NewLink makes one.</summary>
			public object Make(ulong address, string name)
			{
				string problem;
				lock (Problems)
					problem = Problems.Count > 0 ? Problems.Dequeue() : null;
				return new FakeLink(Internal("IFitnessLink"), this, name, problem).GetTransparentProxy();
			}
		}

		/// <summary>One fake link, as the plugin's interface for a link sees it.</summary>
		sealed class FakeLink : RealProxy
		{
			public FakeLink(System.Type type, FakeRadio radio, string name, string problem) : base(type)
			{
				_radio = radio;
				_name = name;
				_problem = problem;
			}

			readonly FakeRadio _radio;
			readonly string _name;
			readonly string _problem;
			volatile bool _up;

			public override IMessage Invoke(IMessage message)
			{
				var call = (IMethodCallMessage)message;
				object result = null;
				switch (call.MethodName)
				{
					case "get_Up":
						result = _up;
						break;
					case "Open":
						_radio.Add("Open " + _name);
						_up = _problem == null;
						result = Task.FromResult(_problem);
						break;
					case "Send":
						var command = (byte[])call.Args[0];
						_radio.Add("Send " + _name + " " + BitConverter.ToString(command));
						result = Task.FromResult(new byte[] { 0x80, command[0], 0x01 });
						break;
					case "Close":
						_radio.Add("Close " + _name);
						_up = false;
						result = Task.FromResult(0);
						break;
					case "Dispose":
						_up = false;
						break;
				}
				return new ReturnMessage(result, null, 0, call.LogicalCallContext, call);
			}
		}

		/// <summary>A session whose links are the radio's fakes, with a worker of its own that runs as x360ce's does.</summary>
		static object NewLinkedSession(FakeRadio radio)
		{
			var session = Activator.CreateInstance(Internal("FitnessSession"), true);
			var linkType = Internal("IFitnessLink");
			var address = Expression.Parameter(typeof(ulong), "address");
			var name = Expression.Parameter(typeof(string), "name");
			var make = Expression.Lambda(typeof(Func<,,>).MakeGenericType(typeof(ulong), typeof(string), linkType),
				Expression.Convert(Expression.Call(Expression.Constant(radio), typeof(FakeRadio).GetMethod(nameof(FakeRadio.Make)), address, name), linkType),
				address, name).Compile();
			session.GetType().GetField("NewLink", Any).SetValue(session, make);
			return session;
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("A device x360ce reads, as it does while the device is ticked, connects to its machine; a machine chosen later replaces it; and the device let go of, as when it is unticked or removed, lets go of the machine and its worker ends")]
		public void Reading_the_device_connects_and_letting_go_disconnects()
		{
			var radio = new FakeRadio();
			var session = NewLinkedSession(radio);
			var device = NewDevice(session);
			Set(device, "Machine", "EJT [A1:B2:C3:D4:E5:F6]");
			Assert.IsNull(Wanted(session), "The machine was connected to before the device was read.");
			StringAssert.StartsWith(Status(device, "Connection"), "EJT is chosen. x360ce connects to it while this device is ticked");

			Read(device);
			Assert.AreEqual("A1B2C3D4E5F6 EJT", Wanted(session), "The first read did not connect to the machine chosen.");
			WaitUntil(() => Get(session, "Held") != null, "The worker did not connect: " + string.Join(", ", radio.Calls));
			Assert.AreEqual("Connected to EJT. Waiting for data.", Status(device, "Connection"));
			Read(device);
			CollectionAssert.AreEqual(new[] { "Open EJT" }, radio.Calls, "Each read connected again.");

			// A machine chosen while the device is read: the first is let go of, and the new one connected to.
			Set(device, "Machine", "Rower [B1:B2:C3:D4:E5:F6]");
			Read(device);
			Assert.AreEqual("B1B2C3D4E5F6 Rower", Wanted(session));
			WaitUntil(() => radio.Calls.Contains("Open Rower") && Get(session, "Held") != null, "The machine chosen later was not connected to: " + string.Join(", ", radio.Calls));
			CollectionAssert.AreEqual(new[] { "Open EJT", "Close EJT", "Open Rower" }, radio.Calls);

			// Let go of: unticked, removed, or x360ce closing.
			((IDisposable)device).Dispose();
			Assert.IsNull(Wanted(session), "The device let go of still holds its machine.");
			WaitUntil(() => Ended(session), "The worker did not end once the device let go.");
			CollectionAssert.AreEqual(new[] { "Open EJT", "Close EJT", "Open Rower", "Close Rower" }, radio.Calls, "The machine was not let go of.");
			Assert.IsNull(Get(session, "Held"));
			Read(device);
			Assert.IsNull(Wanted(session), "A device let go of connected again.");
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("A device ticked when x360ce starts connects to the machine kept for it, trying again quietly until it connects, and says why meanwhile")]
		public void A_device_ticked_at_start_connects_to_its_machine_trying_again()
		{
			var radio = new FakeRadio();
			radio.Problems.Enqueue("EJT could not be reached: make sure it is on and nearby.");
			var session = NewLinkedSession(radio);
			var device = NewDevice(session);
			// As the engine gives a new device the values kept for it, before its first read.
			var store = new PluginSettings();
			var guid = Guid.NewGuid();
			store.Set(guid, "Machine", "EJT [A1:B2:C3:D4:E5:F6]");
			new PluginDeviceReader(Type(), device).ApplySettings(store, guid);
			Read(device);
			WaitUntil(() => Status(device, "Connection").EndsWith("Trying again.", StringComparison.Ordinal),
				"A failed connection does not say so: " + Status(device, "Connection"));
			Assert.AreEqual("EJT could not be reached: make sure it is on and nearby. Trying again.", Status(device, "Connection"));
			CollectionAssert.AreEqual(new[] { "Open EJT", "Close EJT" }, radio.Calls, "The link that failed was not let go of.");
			WaitUntil(() => Get(session, "Held") != null, "The worker did not try again: " + string.Join(", ", radio.Calls), 8000);
			CollectionAssert.AreEqual(new[] { "Open EJT", "Close EJT", "Open EJT" }, radio.Calls);
			((IDisposable)device).Dispose();
			WaitUntil(() => Ended(session), "The worker did not end once the device let go.");
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("A device x360ce never reads, as the one it makes to learn the plugin's name or the one a tab shows while the device is unticked, never connects")]
		public void A_device_never_read_never_connects()
		{
			var radio = new FakeRadio();
			var session = NewLinkedSession(radio);
			var device = NewDevice(session);
			Set(device, "Machine", "EJT [A1:B2:C3:D4:E5:F6]");
			Assert.AreEqual("No machine chosen. Press Scan on the Connection page, then select the machine.", Status(NewDevice(NewLinkedSession(radio)), "Connection"));
			((IDisposable)device).Dispose();
			Assert.IsNull(Get(session, "_worker"), "A device never read started a worker.");
			Assert.AreEqual(0, radio.Calls.Length, "A device never read connected.");
		}

		[TestMethod, TestCategory("plugins"), TestCategory("engine"), TestCategory("critical")]
		[Description("The engine reads a Fitness Machine ticked in its controller's list and on the Devices tab, which connects it; unticked in either, removed, or x360ce closing, the engine lets go of it, which disconnects it")]
		public void Ticking_the_device_connects_and_unticking_disconnects()
		{
			var radio = new FakeRadio();
			var ud = new UserDevice { InstanceGuid = Guid.NewGuid(), ProductGuid = Product, IsEnabled = true };
			ud.InputSourceType = (int)InputSourceType.Plugin;
			ud.IsOnline = true;
			var helper = new DInputHelper();
			var readers = (ConcurrentDictionary<Guid, PluginDeviceReader>)typeof(DInputHelper)
				.GetField("_pluginReaders", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(helper);
			var di = EngineSteps.UpdateDiStates(helper);
			var ticked = DeviceRouting.Build(Game, new[] { Row(ud.InstanceGuid, MapTo.Controller1) }, new PadSetting[0], new[] { ud });
			// Each time the device is ticked the engine makes a new one; here, one whose links are fakes.
			Func<object> tick = () =>
			{
				var session = NewLinkedSession(radio);
				var device = NewDevice(session);
				Set(device, "Machine", "EJT [A1:B2:C3:D4:E5:F6]");
				readers[ud.InstanceGuid] = new PluginDeviceReader(Type(), device);
				di(null, Game, null, ticked);
				WaitUntil(() => Get(session, "Held") != null, "The device ticked did not connect: " + string.Join(", ", radio.Calls));
				return session;
			};
			var first = tick();
			// Unticked in the controller's list.
			var unticked = Row(ud.InstanceGuid, MapTo.Controller1);
			unticked.IsEnabled = false;
			di(null, Game, null, DeviceRouting.Build(Game, new[] { unticked }, new PadSetting[0], new[] { ud }));
			Assert.IsFalse(readers.ContainsKey(ud.InstanceGuid), "The engine did not let go of the device unticked.");
			WaitUntil(() => Ended(first), "The device unticked did not let go of the machine.");
			CollectionAssert.AreEqual(new[] { "Open EJT", "Close EJT" }, radio.Calls);

			// Ticked again, then unticked on the Devices tab.
			var second = tick();
			ud.IsEnabled = false;
			di(null, Game, null, DeviceRouting.Build(Game, new[] { Row(ud.InstanceGuid, MapTo.Controller1) }, new PadSetting[0], new[] { ud }));
			WaitUntil(() => Ended(second), "The device unticked on the Devices tab did not let go of the machine.");
			ud.IsEnabled = true;

			// Ticked again, then removed from the device list.
			var third = tick();
			di(null, Game, null, DeviceRouting.Build(Game, new UserSetting[0], new PadSetting[0], new UserDevice[0]));
			WaitUntil(() => Ended(third), "The device removed did not let go of the machine.");

			// Ticked again, then x360ce closes: the engine lets go of every device as it ends.
			var fourth = tick();
			typeof(DInputHelper).GetMethod("ReleasePluginReaders", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(helper, null);
			WaitUntil(() => Ended(fourth), "Closing x360ce did not let go of the machine.");
			CollectionAssert.AreEqual(Enumerable.Repeat(new[] { "Open EJT", "Close EJT" }, 4).SelectMany(x => x).ToArray(), radio.Calls);
		}

		[TestMethod, TestCategory("plugins")]
		[Description("The machines heard are listed as the receiver lists them: fitness machines, a name from any advertisement, and every named device with Show all")]
		public void Machines_heard_are_listed_as_the_receiver_lists_them()
		{
			var scan = Activator.CreateInstance(Internal("FitnessScan"), true);
			Call(scan, "Hear", 1UL, "", true, "indoor bike", (short)-78);
			// The name comes in another advertisement, which does not say it is a fitness machine.
			Call(scan, "Hear", 1UL, "EJT", false, "", (short)-70);
			Call(scan, "Hear", 2UL, "Phone", false, "", (short)-60);
			Call(scan, "Hear", 4UL, "", true, "", (short)-90);
			var rows = ((Array)Call(scan, "Machines")).Cast<object>().ToArray();
			Assert.AreEqual(2, rows.Length, "A device that is not a fitness machine was listed.");
			Assert.AreEqual("EJT", Get(rows[0], "DisplayName"));
			Assert.AreEqual("indoor bike", Get(rows[0], "Description"));
			Assert.AreEqual("-70 dBm", Get(rows[0], "SignalText"));
			Assert.AreEqual("00:00:00:00:00:04", Get(rows[1], "DisplayName"), "A machine without a name is not shown by its address.");
			Assert.AreEqual("fitness machine", Get(rows[1], "Description"));
			Internal("FitnessScan").GetField("_showAll", Any).SetValue(scan, true);
			Call(scan, "Hear", 2UL, "Phone", false, "", (short)-60);
			Call(scan, "Hear", 3UL, "", false, "", (short)-60);
			rows = ((Array)Call(scan, "Machines")).Cast<object>().ToArray();
			Assert.AreEqual(3, rows.Length, "Show all did not add the named device alone.");
			Assert.AreEqual("not advertising the fitness service", Get(rows[2], "Description"));
		}

		[TestMethod, TestCategory("plugins")]
		[Description("A machine is kept as its name and Bluetooth address, and read back from them, from the address alone, and from nothing else")]
		public void A_machine_is_kept_by_its_name_and_address()
		{
			var want = Internal("FitnessSession").GetNestedType("Want", Any);
			Func<ulong, string, string> text = (address, name) => (string)want.GetMethod("Text", Any).Invoke(null, new object[] { address, name });
			Func<string, string> parse = value =>
			{
				var read = want.GetMethod("Parse", Any).Invoke(null, new object[] { value });
				return read == null ? null : ((ulong)Get(read, "Address")).ToString("X12") + " " + Get(read, "Name");
			};
			Assert.AreEqual("EJT [A1:B2:C3:D4:E5:F6]", text(Address, "EJT"));
			Assert.AreEqual("A1:B2:C3:D4:E5:F6", text(Address, "A1:B2:C3:D4:E5:F6"), "A machine without a name repeats its address.");
			Assert.AreEqual("A1B2C3D4E5F6 EJT", parse("EJT [A1:B2:C3:D4:E5:F6]"));
			Assert.AreEqual("A1B2C3D4E5F6 Bike [2]", parse(" Bike [2] [a1b2c3d4e5f6] "), "The last brackets do not hold the address.");
			Assert.AreEqual("A1B2C3D4E5F6 A1:B2:C3:D4:E5:F6", parse("A1:B2:C3:D4:E5:F6"));
			Assert.IsNull(parse(""));
			Assert.IsNull(parse("EJT"), "A name alone was read as an address.");
			Assert.IsNull(parse("ABC"), "A name of hexadecimal letters was read as an address.");
		}

		[TestMethod, TestCategory("plugins"), TestCategory("ui")]
		[Description("How often a value changes is the middle of its last gaps once there are three, and a pause does not move it; a value sent again unchanged is no change")]
		public void An_interval_is_the_middle_of_the_last_gaps()
		{
			var interval = Activator.CreateInstance(Internal("UpdateInterval"), true);
			foreach (var at in new long[] { 0, 1000, 2000 })
				Call(interval, "Add", at);
			Assert.AreEqual("", interval.ToString(), "Two gaps gave an interval.");
			Call(interval, "Add", 3000L);
			Assert.AreEqual("1.0 s", interval.ToString());
			Call(interval, "Add", 13000L);
			Assert.AreEqual("1.0 s", interval.ToString(), "A pause moved the interval.");
			var row = Activator.CreateInstance(Internal("FitnessReading"), Any, null, new object[] { "Speed", "25.00 km/h", 0L }, null);
			Call(row, "Update", "25.00 km/h", 500L);
			foreach (var at in new long[] { 1000, 2000, 3000 })
				Call(row, "Update", at.ToString(), at);
			Assert.AreEqual("every 1.0 s", Get(row, "Interval"), "The same value sent again counted as a change.");
		}

		[TestMethod, TestCategory("plugins")]
		[Description("The values are listed in the order they first came, each in the receiver's words and units, and the log is newest first without repeats")]
		public void Values_and_activity_read_as_the_receiver_shows_them()
		{
			var device = NewDevice();
			var session = Session(device);
			Assert.AreEqual("No machine chosen. Press Scan on the Connection page, then select the machine.", Status(device, "Connection"));
			Take(device, 0x2AD3, 0x00, 0x0D);
			Take(device, IndoorBikeDataId, BikeData);
			var readings = ((Array)Call(session, "Readings")).Cast<object>().Select(x => Get(x, "Name") + " = " + Get(x, "Value")).ToArray();
			CollectionAssert.AreEqual(new[]
			{
				"Training status = manual mode (quick start)", "Speed = 25.00 km/h", "Cadence = 80 rpm", "Distance = 1234 m", "Resistance = 5",
				"Power = 150 W", "Heart rate = 120 bpm", "Elapsed time = 0:10:00",
			}, readings);
			Call(session, "Note", "Connected to EJT.");
			Take(device, 0x2ADA, 0x02, 0x02);
			Take(device, 0x2ADA, 0x02, 0x02);
			var log = (string[])Call(session, "Log");
			Assert.AreEqual(2, log.Length, "A line the same as the last was added again.");
			StringAssert.Matches(log[0], new Regex(@"^\d\d:\d\d:\d\d  Machine status: paused\.$"));
			StringAssert.EndsWith(log[1], "  Connected to EJT.");
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("Each button and target of the Connection page sends the standard FTMS command, one at a time, and its answer reads in words")]
		public void The_connection_page_sends_the_standard_commands()
		{
			Func<string, double, byte[]> command = (name, value) => (byte[])Internal("Ftms").GetMethod("Command", Any)
				.Invoke(null, new object[] { Enum.Parse(Internal("FitnessCommand"), name), value });
			CollectionAssert.AreEqual(new byte[] { 0x07 }, command("Start", 0));
			CollectionAssert.AreEqual(new byte[] { 0x08, 0x02 }, command("Pause", 0));
			CollectionAssert.AreEqual(new byte[] { 0x08, 0x01 }, command("Stop", 0));
			CollectionAssert.AreEqual(new byte[] { 0x01 }, command("Reset", 0));
			CollectionAssert.AreEqual(new byte[] { 0x05, 150, 0 }, command("Power", 150), "Target power is not watts.");
			CollectionAssert.AreEqual(new byte[] { 0x04, 50, 0 }, command("Resistance", 5), "Target resistance is not tenths of a level.");
			CollectionAssert.AreEqual(new byte[] { 0x11, 0, 0, 0xFA, 0x00, 40, 51 }, command("Grade", 2.5), "Road grade is not hundredths of a percent.");
			CollectionAssert.AreEqual(new byte[] { 0x11, 0, 0, 0x6A, 0xFF, 40, 51 }, command("Grade", -1.5), "A road going down is not a negative grade.");
			Assert.AreEqual("Set target power: success", Internal("Ftms").GetMethod("Answer", Any).Invoke(null, new object[] { new byte[] { 0x80, 0x05, 0x01 } }));
			var session = NewSession();
			Assert.IsFalse((bool)Get(session, "Busy"));
			Call(session, "Send", Enum.Parse(Internal("FitnessCommand"), "Power"), 150.0);
			Assert.IsTrue((bool)Get(session, "Busy"), "The page can send another command before the first is answered.");
			CollectionAssert.AreEqual(new byte[] { 0x05, 150, 0 }, (byte[])Get(Get(session, "_command"), "Bytes"));
		}

		/// <summary>Every control inside one, as the AI door walks them: a grid is one control, read by its rows, not by its scroll bars.</summary>
		static IEnumerable<Control> All(Control control)
		{
			foreach (Control child in control.Controls)
			{
				yield return child;
				if (child is DataGridView)
					continue;
				foreach (var inner in All(child))
					yield return inner;
			}
		}

		/// <summary>A list of machines heard of its own, which reaches no radio while it is not started.</summary>
		static object NewScan()
		{
			return Activator.CreateInstance(Internal("FitnessScan"), true);
		}

		/// <summary>The Connection page of a device, listing the machines of a scan of its own, as a test makes it.</summary>
		static UserControl NewPage(IPluginDevice device, object scan)
		{
			return (UserControl)Activator.CreateInstance(Internal("FitnessConnection"), Any, null, new[] { device, scan }, null);
		}

		/// <summary>The element of a tree read by the AI door at a path, or null.</summary>
		static UiNode Find(UiNode node, string path)
		{
			if (node.Path == path)
				return node;
			return node.Items == null ? null : node.Items.Select(x => Find(x, path)).FirstOrDefault(x => x != null);
		}

		[TestMethod, TestCategory("plugins"), TestCategory("ui"), TestCategory("critical")]
		[Description("The Connection page names every control, lists the machines and values, greys what cannot be used, and selecting a machine chooses it for the device, kept as its Machine setting, without connecting by itself")]
		public void Selecting_a_machine_chooses_it_without_connecting()
		{
			Ui.OnUiThread(() =>
			{
				var session = NewSession();
				var device = NewDevice(session);
				var changed = new List<string>();
				((System.ComponentModel.INotifyPropertyChanged)device).PropertyChanged += (s, e) => changed.Add(e.PropertyName);
				var scan = NewScan();
				Call(scan, "Hear", Address, "EJT", true, "indoor bike", (short)-78);
				Call(scan, "Hear", RowerAddress, "Rower", true, "rower", (short)-60);
				using (var page = NewPage(device, scan))
				{
					Assert.AreEqual("Connection", page.Text, "The page is not named Connection.");
					Call(session, "Take", (ushort)IndoorBikeDataId, BikeData);
					Call(page, "ShowSession", true);
					var list = (DataGridView)Get(page, "MachinesDataGridView");
					CollectionAssert.AreEqual(new[] { "EJT|indoor bike|-78 dBm", "Rower|rower|-60 dBm" },
						list.Rows.Cast<DataGridViewRow>().Select(x => string.Join("|", x.Cells.Cast<DataGridViewCell>().Select(c => c.Value))).ToArray());
					Assert.AreEqual(0, list.SelectedRows.Count, "A machine is selected that nobody chose.");
					var values = (TableLayoutPanel)Get(page, "ReadingsTableLayoutPanel");
					Assert.AreEqual("Speed", values.GetControlFromPosition(0, 0).Text);
					Assert.AreEqual("25.00 km/h", values.GetControlFromPosition(1, 0).Text);
					Assert.AreEqual("No machine chosen. Press Scan on the Connection page, then select the machine.", ((Label)Get(page, "StatusLabel")).Text);
					// Greyed, never hidden, while nothing is connected.
					foreach (var name in new[] { "StartButton", "PauseButton", "StopButton", "ResetButton", "SetPowerButton", "SetResistanceButton", "SetGradeButton", "PowerTrackBar" })
					{
						var control = (Control)Get(page, name);
						Assert.IsFalse(control.Enabled, name + " is on with no machine connected.");
						Assert.IsTrue(JocysCom.ClassLibrary.Controls.ControlsHelper.IsVisible(control), name + " is hidden, not greyed.");
					}

					// Selected, the machine is chosen and kept; the device does not connect by itself.
					list.CurrentCell = list.Rows[0].Cells[0];
					Assert.AreEqual("EJT [A1:B2:C3:D4:E5:F6]", Get(device, "Machine"), "Selecting the machine did not choose it.");
					CollectionAssert.AreEqual(new[] { "Machine" }, changed, "x360ce is not told to keep the machine chosen.");
					Assert.IsNull(Wanted(session), "Selecting a machine connected the device by itself.");
					StringAssert.StartsWith(((Label)Get(page, "StatusLabel")).Text, "EJT is chosen. x360ce connects to it while this device is ticked");
					// Read, as while the device is ticked, it connects; another machine chosen replaces it.
					Read(device);
					Assert.AreEqual("A1B2C3D4E5F6 EJT", Wanted(session));
					list.CurrentCell = list.Rows[1].Cells[0];
					Read(device);
					Assert.AreEqual("B1B2C3D4E5F6 Rower", Wanted(session), "The machine chosen later was not connected to.");

					// A machine chosen that no scan heard is listed first, selected, so the page always shows it.
					var fresh = NewScan();
					using (var other = NewPage(device, fresh))
					{
						var rows = ((DataGridView)Get(other, "MachinesDataGridView")).Rows;
						Assert.AreEqual(1, rows.Count);
						Assert.AreEqual("Rower|chosen, not heard yet|", string.Join("|", rows[0].Cells.Cast<DataGridViewCell>().Select(c => c.Value)));
						Assert.IsTrue(rows[0].Selected, "The machine chosen is not selected.");
					}
					var unnamed = All(page).Where(x => string.IsNullOrEmpty(x.Name)).Select(x => x.GetType().Name + " in " + x.Parent.Name).ToArray();
					Assert.AreEqual(0, unnamed.Length, "The AI door cannot reach: " + string.Join(", ", unnamed));
					foreach (var gone in new[] { "ConnectButton", "MapButton", "ControllerComboBox", "ReconnectCheckBox" })
						Assert.AreEqual(0, page.Controls.Find(gone, true).Length, gone + " is still on the page: the Enabled box connects, and the controller page maps.");
				}
			});
		}

		[TestMethod, TestCategory("plugins"), TestCategory("ui"), TestCategory("accessibility"), TestCategory("critical")]
		[Description("The AI door reads the Connection page's machines as rows, chooses one by its index, and reads each value by its name")]
		public void The_ai_door_lists_and_chooses_a_machine_and_reads_its_values()
		{
			const string table = "FitnessConnection/MainTableLayoutPanel/";
			const string list = table + "MachinesDataGridView";
			const string values = table + "ReadingsPanel";
			Ui.OnUiThread(() =>
			{
				var session = NewSession();
				var device = NewDevice(session);
				var scan = NewScan();
				Call(scan, "Hear", Address, "EJT", true, "indoor bike", (short)-78);
				Call(scan, "Hear", RowerAddress, "Rower", true, "rower", (short)-60);
				// Training status: manual mode, then Indoor Bike Data.
				Call(session, "Take", (ushort)0x2AD3, new byte[] { 0x00, 0x0D });
				Call(session, "Take", (ushort)IndoorBikeDataId, BikeData);
				using (var form = new Form { Name = "Main" })
				using (var page = NewPage(device, scan))
				{
					page.Dock = DockStyle.Fill;
					form.Controls.Add(page);
					form.Show();
					Call(page, "ShowSession", true);

					// ui_read of the list: a row for each machine, named by its name and read as the list shows it.
					var grid = UiTreeWalker.Find(form, list);
					var read = UiTreeWalker.Read(grid, false, list);
					Assert.AreEqual("Grid", read.Role, "The list of machines is not a control the door reads.");
					Assert.AreEqual("", read.Value, "A machine is selected that nobody chose.");
					var rows = read.Items.Where(x => x.Role == "Row").ToArray();
					CollectionAssert.AreEqual(new[] { list + "/rows/0", list + "/rows/1" }, rows.Select(x => x.Path).ToArray());
					Assert.AreEqual("EJT", rows[0].Name, "A machine's row is not named by the machine's name.");
					Assert.AreEqual("Name: EJT; Type: indoor bike; Signal: -78 dBm", rows[0].Value);
					Assert.AreEqual("Name: Rower; Type: rower; Signal: -60 dBm", rows[1].Value);

					// ui_set of the list to a row's index chooses that machine.
					Assert.IsNull(UiTreeWalker.SetValue(grid, "1"), "The door cannot choose a machine.");
					Assert.AreEqual("1", UiTreeWalker.GetValue(grid));
					Assert.AreEqual("Rower [B1:B2:C3:D4:E5:F6]", Get(device, "Machine"), "The door's selection did not choose the machine.");
					StringAssert.StartsWith(UiTreeWalker.GetValue(UiTreeWalker.Find(form, table + "StatusTableLayoutPanel/StatusLabel")), "Rower is chosen.");

					// ui_read of the box of values: each value by its name, and how often it changes.
					var box = UiTreeWalker.Read(UiTreeWalker.Find(form, values), false, values);
					var shown = box.Items.Where(x => x.Id.EndsWith("ValueLabel", StringComparison.Ordinal)).Select(x => x.Name + " = " + x.Value).ToArray();
					CollectionAssert.AreEqual(new[]
					{
						"Training status = manual mode (quick start)", "Speed = 25.00 km/h", "Cadence = 80 rpm", "Distance = 1234 m", "Resistance = 5",
						"Power = 150 W", "Heart rate = 120 bpm", "Elapsed time = 0:10:00",
					}, shown, "The door does not read the values the page shows.");
					var speed = Find(box, values + "/ReadingsTableLayoutPanel/SpeedValueLabel");
					Assert.IsNotNull(speed, "The speed has no path of its own.");
					Assert.AreEqual("25.00 km/h", UiTreeWalker.GetValue(UiTreeWalker.Find(form, speed.Path)));
					Assert.IsNotNull(Find(box, values + "/ReadingsTableLayoutPanel/HeartRateIntervalLabel"), "How often a value changes is out of the door's reach.");
					Assert.IsFalse(box.Items.Any(x => x.Id.EndsWith("NameLabel", StringComparison.Ordinal)), "A value's caption is listed beside the value that carries its name.");
				}
			});
		}

		[TestMethod, TestCategory("plugins"), TestCategory("ui"), TestCategory("critical")]
		[Description("The device's tab has the Connection page, Properties and Help, in that order; a machine the page chooses is kept for the device; and while the engine reads no device the page shows one of the tab's own, given the kept values, which never connects")]
		public void Its_tab_has_connection_properties_and_help()
		{
			Ui.OnUiThread(() =>
			{
				var store = new PluginSettings();
				var guid = Guid.NewGuid();
				store.Set(guid, "MaxSpeed", "60");
				var radio = new FakeRadio();
				var device = NewDevice(NewLinkedSession(radio));
				using (var form = new Form { Width = 1100, Height = 700 })
				using (var panel = new PluginDeviceUserControl(guid, Type(), device, store) { Dock = DockStyle.Fill })
				{
					form.Controls.Add(panel);
					form.Show();
					var tabs = (TabControl)panel.Controls.Find("PluginTabControl", true).Single();
					CollectionAssert.AreEqual(new[] { "Connection", "Properties", "Help" }, tabs.TabPages.Cast<TabPage>().Select(x => x.Text).ToArray());
					Assert.AreSame(device, Get(tabs.TabPages[0].Controls[0], "_device"), "The Connection page shows another device than the one the engine reads.");
					var help = (RichTextBox)panel.Controls.Find("HelpRichTextBox", true).Single();
					StringAssert.Contains(help.Text, "Experimental: walk, drive or row in games with a fitness machine.");
					Assert.AreEqual(0, panel.Controls.Find("DescriptionLabel", true).Length, "The help heads the tab as well as having a page.");
					// Chosen on the page, kept for the device, as a row of the grid would keep it.
					Call(device, "Choose", Address, "EJT");
					Assert.AreEqual("EJT [A1:B2:C3:D4:E5:F6]", store.Get(guid, "Machine"), "The machine chosen is not kept for the device.");
				}
				// Unticked, the engine reads none: the page shows an instance of the tab's own, never read.
				using (var form = new Form { Width = 1100, Height = 700 })
				using (var panel = new PluginDeviceUserControl(guid, Type(), null, store) { Dock = DockStyle.Fill })
				{
					form.Controls.Add(panel);
					form.Show();
					var own = (IPluginDevice)panel.Shown;
					Assert.IsNotNull(own, "The tab shows no device while the engine reads none.");
					var page = panel.Controls.Find("FitnessConnection", true).Single();
					Assert.AreSame(own, Get(page, "_device"));
					Assert.AreEqual("EJT [A1:B2:C3:D4:E5:F6]", Get(own, "Machine"), "The tab's own device was not given the machine kept.");
					Assert.AreEqual(60.0, Get(own, "MaxSpeed"), "The tab's own device was not given the values kept.");
					StringAssert.StartsWith(((Label)Get(page, "StatusLabel")).Text, "EJT is chosen. x360ce connects to it while this device is ticked");
					Assert.IsNull(Get(Get(own, "Session"), "_worker"), "The tab's own device connected.");
					Call(own, "Choose", RowerAddress, "Rower");
					Assert.AreEqual("Rower [B1:B2:C3:D4:E5:F6]", store.Get(guid, "Machine"), "A machine chosen while the device is unticked is not kept.");
				}
			});
		}

		[TestMethod, TestCategory("plugins"), TestCategory("ui"), TestCategory("critical")]
		[Description("The device's tab made again on a window already shown, with activity in the log, as when the device is ticked while its Connection page is in view, makes its windows without an error, and the log's scroll bar shows")]
		public void Its_tab_is_made_on_a_shown_window_without_error()
		{
			Ui.OnUiThread(() =>
			{
				var session = NewSession();
				var device = NewDevice(session);
				for (var i = 0; i < 40; i++)
					Call(session, "Note", "Line " + i + ": Nobody could not be reached: make sure it is on and nearby.");
				using (var form = new Form { Width = 1100, Height = 700 })
				{
					var tabs = new TabControl { Name = "PadTabControl", Dock = DockStyle.Fill };
					tabs.TabPages.Add(new TabPage("General"));
					form.Controls.Add(tabs);
					form.Show();
					Application.DoEvents();
					var panel = new PluginDeviceUserControl(Guid.NewGuid(), Type(), device, new PluginSettings()) { Dock = DockStyle.Fill };
					var tab = new TabPage("Fitness Machine");
					tab.Controls.Add(panel);
					tabs.TabPages.Add(tab);
					tabs.SelectedTab = tab;
					Application.DoEvents();
					var log = (TextBox)panel.Controls.Find("LogTextBox", true).Single();
					Assert.IsTrue(log.IsHandleCreated, "The log has no window.");
					Assert.AreEqual(ScrollBars.Vertical, log.ScrollBars, "The log's scroll bar does not show while its lines are more than it shows.");
				}
			});
		}

		[TestMethod, TestCategory("plugins"), TestCategory("ui"), TestCategory("critical")]
		[Description("Selecting another Bluetooth machine disconnects immediately and unchecks Enabled; toggling Enabled keeps the Fitness Machine tab in place without disappearing")]
		public void Switching_machine_disconnects_and_unchecks_and_toggling_keeps_tab_in_place()
		{
			Ui.OnUiThread(() =>
			{
				var session = NewSession();
				var device = NewDevice(session);
				var guid = Guid.NewGuid();
				var setting = new UserSetting { InstanceGuid = guid, ProductGuid = Product, IsEnabled = true };
				x360ce.App.SettingsManager.UserSettings.Items.Add(setting);
				try
				{
					// Start connected to EJT.
					Call(device, "Choose", Address, "EJT");
					Read(device);
					Assert.AreEqual("A1B2C3D4E5F6 EJT", Wanted(session));

					using (var form = new Form { Width = 1100, Height = 700 })
					using (var pad = new PadControl(MapTo.Controller1) { Dock = DockStyle.Fill })
					{
						form.Controls.Add(pad);
						form.Show();
						pad.ShowPluginTab(guid, Type(), device);
						var tabBefore = pad.PadTabControl.TabPages.Cast<TabPage>().Single(p => p.Text == "Fitness Machine");

						// Selecting a different Bluetooth machine immediately drops the link AND unchecks Enabled.
						setting.IsEnabled = true;
						Call(device, "Choose", RowerAddress, "Rower");
						Assert.IsNull(Wanted(session), "Selecting another Bluetooth machine must immediately disconnect the current one.");
						Assert.IsFalse(setting.IsEnabled, "Selecting another Bluetooth machine must uncheck Enabled so x360ce never auto-connects to another person's machine.");

						// Toggling Enabled off (device -> null) and back on (null -> device) must keep the exact same TabPage instance without flicker.
						pad.ShowPluginTab(guid, Type(), null);
						var tabWhileOff = pad.PadTabControl.TabPages.Cast<TabPage>().Single(p => p.Text == "Fitness Machine");
						Assert.AreSame(tabBefore, tabWhileOff, "Unchecking Fitness Machine must not remove and re-add its TabPage.");

						pad.ShowPluginTab(guid, Type(), device);
						var tabWhileOn = pad.PadTabControl.TabPages.Cast<TabPage>().Single(p => p.Text == "Fitness Machine");
						Assert.AreSame(tabBefore, tabWhileOn, "Checking Fitness Machine must not remove and re-add its TabPage.");
					}
				}
				finally
				{
					x360ce.App.SettingsManager.UserSettings.Items.Remove(setting);
				}
			});
		}

		#endregion

		/// <summary>A plugin device that takes force, and counts what x360ce asks of it, making nothing while it does.</summary>
		sealed class ForceDevice : IPluginDevice, IPluginForceFeedback
		{
			public readonly List<string> Given = new List<string>();
			public int Reads;

			public string Name { get { return "Force Device"; } }
			public Guid ProductGuid { get { return Guid.Empty; } }
			public int AxisCount { get { return 1; } }
			public int SliderCount { get { return 0; } }
			public int PovCount { get { return 0; } }
			public int ButtonCount { get { return 1; } }

			public void Read(PluginState state)
			{
				Reads++;
			}

			public void SetForce(int largeMotor, int smallMotor)
			{
				Given.Add(largeMotor + "," + smallMotor + " before read " + (Reads + 1));
			}
		}

		[TestMethod, TestCategory("plugins"), TestCategory("engine"), TestCategory("critical")]
		[Description("The engine gives a plugin that takes force the strongest of each motor of its tabs with force on, scaled to 0 to 65535, just before it reads it and only when it changes, and makes nothing while it stands")]
		public void The_engine_gives_a_plugin_its_force_only_when_it_changes()
		{
			var file = new PluginFile("Tests.dll", false);
			Assert.IsNull(new PluginDeviceReader(TestControllerDevice.Type, TestControllerDevice.Type.NewInstance()).Force,
				"A plugin that takes no force is asked to.");
			var device = new ForceDevice();
			var reader = new PluginDeviceReader(new PluginDeviceType(typeof(ForceDevice), file), device);
			Assert.AreSame(device, reader.Force);
			var ud = new UserDevice { InstanceGuid = Guid.NewGuid() };
			ud.InputSourceType = (int)InputSourceType.Plugin;
			ud.IsOnline = true;
			var helper = new DInputHelper();
			var readers = (ConcurrentDictionary<Guid, PluginDeviceReader>)typeof(DInputHelper)
				.GetField("_pluginReaders", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(helper);
			readers[ud.InstanceGuid] = reader;
			var on = Force(true);
			var forced = DeviceRouting.Build(Game, new[] { Row(ud.InstanceGuid, MapTo.Controller1, on) }, new[] { on }, new[] { ud });
			var di = EngineSteps.UpdateDiStates(helper);
			helper._lastLargeMotor[0] = 255;
			helper._lastSmallMotor[0] = 10;
			di(null, Game, null, forced);
			di(null, Game, null, forced);
			helper._lastLargeMotor[0] = 0;
			di(null, Game, null, forced);
			var off = Force(false);
			di(null, Game, null, DeviceRouting.Build(Game, new[] { Row(ud.InstanceGuid, MapTo.Controller1, off) }, new[] { off }, new[] { ud }));
			CollectionAssert.AreEqual(new[] { "65535,2570 before read 1", "0,2570 before read 3", "0,0 before read 4" }, device.Given,
				"The force was not given before the read, once each time it changed, and nought once the tab's force feedback was off.");
			Assert.AreEqual(4, device.Reads);

			const int passes = 20000;
			var bytes = Allocations.FewestBytes(5, () =>
			{
				for (var i = 0; i < passes; i++)
					di(null, Game, null, forced);
			});
			Assert.IsTrue(bytes < passes, passes + " passes reading a plugin that takes force handed the collector " + bytes + " bytes.");
		}
	}
}
