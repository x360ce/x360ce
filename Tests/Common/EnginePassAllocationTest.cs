// @under-test: App.v4/Common/DInput/DInputHelper.cs, App.v4/Common/DInput/DInputHelper.Step1.UpdateDevices.cs, App.v4/Common/DInput/DInputHelper.Step2.UpdateDiStates.cs, App.v4/Common/DInput/DInputHelper.Step3.UpdateXiStates.cs, App.v4/Common/DInput/DInputHelper.Step4.CombineXiStates.cs, App.v4/Common/DInput/DInputHelper.Step5.VirtualDevices.cs, App.v4/Common/DInput/DInputHelper.Step6.RetrieveXiStates.cs, App.v4/Common/DInput/DInputHelper.XInputLibrarry.cs, App.v4/Common/DInput/DeviceRouting.cs, App.v4/Common/DInput/XInputPlaces.cs, Engine/JocysCom/Common/HiResTimer.cs
// @area: engine   @layer: unit
using JocysCom.ClassLibrary;
using JocysCom.ClassLibrary.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using x360ce.App;
using x360ce.App.DInput;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>A whole turn of the engine's loop, the pass and the wait after it, hands the collector nothing.</summary>
	/// <remarks>
	/// At a thousand passes a second, a pass that makes a kilobyte hands the collector a megabyte a second, and every
	/// collection stops the engine with every other managed thread. Everything a pass uses is reserved up front: the
	/// routing carries the rows, their devices, mappings and D-Pads, and each device reads into its own states. The
	/// wait is the loop's own pacer, with its wake handle set, so it goes through the same wait call and returns at once.
	///
	/// The games here use no emulation, so the pass never reaches the bus, apart from one in virtual mode while a
	/// controller is plugged in, which stops at Step 5's plug gate. Step 5's other parts that can run without a bus
	/// are measured by BusRefusalTest, GuideButtonTest and EngineWaitsForNoLockTest.
	/// </remarks>
	[TestClass]
	public class EnginePassAllocationTest
	{
		/// <summary>Turns in each measured window. The smallest of <see cref="Windows"/> windows is the turn's own count.</summary>
		const int Passes = 20000;
		const int Windows = 5;

		/// <summary>Stands in for the window's handlers, so an event raised on a pass makes its arguments here as it does in the program.</summary>
		static readonly EventHandler<DInputEventArgs> Nothing = (sender, e) => { };

		UserGame _oldGame;
		Func<DeviceInfo[]> _oldReadMachine;
		bool _oldGetXInputStates;
		bool _oldXInputEnabled;
		ViGEmClient _oldClient;
		readonly List<UserSetting> _rows = new List<UserSetting>();
		readonly List<PadSetting> _settings = new List<PadSetting>();
		readonly List<UserDevice> _devices = new List<UserDevice>();
		DInputHelper _helper;
		EngineSteps.Pass _pass;
		ManualResetEvent _wake;
		HiResPacer _pacer;

		internal static readonly UserGame Game = new UserGame
		{
			FileName = "pass-allocation.exe",
			FileProductName = "Pass allocation",
			EmulationType = (int)EmulationType.None,
			EnableMask = (int)(MapToMask.Controller1 | MapToMask.Controller2 | MapToMask.Controller3 | MapToMask.Controller4),
		};

		/// <summary>Settings a person would have: buttons, sticks, triggers and a D-Pad.</summary>
		internal static PadSetting Typical()
		{
			return new PadSetting
			{
				ButtonA = "b1", ButtonB = "b2", ButtonX = "b3", ButtonY = "b4",
				LeftShoulder = "b5", RightShoulder = "b6", ButtonBack = "b7", ButtonStart = "b8",
				LeftThumbAxisX = "a1", LeftThumbAxisY = "a-2", RightThumbAxisX = "a3", RightThumbAxisY = "a-4",
				LeftTrigger = "x5", RightTrigger = "x-5", DPad = "p1",
				PadSettingChecksum = Guid.NewGuid(),
			};
		}

		[TestInitialize]
		public void Setup()
		{
			if (Global.DHelper == null)
				Global.InitDHelperHelper();
			_oldGame = SettingsManager.CurrentGame;
			_oldReadMachine = XInputPlaces.ReadMachine;
			_oldGetXInputStates = SettingsManager.Options.GetXInputStates;
			_oldXInputEnabled = SettingsManager.Options.XInputEnabled;
			_oldClient = ViGEmClient.Current;
			XInputPlaces.ReadMachine = () => new DeviceInfo[0];
			// The window is not there to say whether it shows the XInput states, so the pass is told it does not.
			SettingsManager.Options.GetXInputStates = false;
			ViGEmClient.Current = null;
			XInputPlaces.Invalidate();
			XInputPlaces.ReadIfStale();
			DeviceRouting.Watch();
			_helper = new DInputHelper { UpdateDevicesEnabled = false };
			// The window's subscriptions.
			_helper.DevicesUpdated += Nothing;
			_helper.StatesRetrieved += Nothing;
			_helper.XInputReloaded += Nothing;
			_pass = EngineSteps.RefreshAll(_helper);
			// Set, so every wait ends at once, as a wait does when the loop is asked to stop.
			_wake = new ManualResetEvent(true);
			_pacer = new HiResPacer(_wake);
		}

		[TestCleanup]
		public void Restore()
		{
			SettingsManager.UpdateCurrentGame(_oldGame);
			foreach (var row in _rows)
				SettingsManager.UserSettings.Items.Remove(row);
			foreach (var ps in _settings)
				SettingsManager.PadSettings.Items.Remove(ps);
			foreach (var device in _devices)
				SettingsManager.UserDevices.Items.Remove(device);
			SettingsManager.Options.GetXInputStates = _oldGetXInputStates;
			SettingsManager.Options.XInputEnabled = _oldXInputEnabled;
			XInputPlaces.ReadMachine = _oldReadMachine;
			ViGEmClient.Current = _oldClient;
			_pacer.Dispose();
			_wake.Dispose();
		}

		/// <summary>The test controller, listed.</summary>
		UserDevice Device()
		{
			var ud = TestDeviceHelper.NewUserDevice();
			ud.IsOnline = true;
			ud.HidDeviceId = "HID\\VID_045E&PID_028E&IG_00\\pass-allocation";
			_devices.Add(ud);
			SettingsManager.UserDevices.Items.Add(ud);
			return ud;
		}

		PadSetting Stored(PadSetting ps)
		{
			_settings.Add(ps);
			SettingsManager.PadSettings.Items.Add(ps);
			return ps;
		}

		void Map(UserDevice ud, MapTo controller, PadSetting ps)
		{
			var row = new UserSetting { InstanceGuid = ud.InstanceGuid, FileName = Game.FileName, MapTo = (int)controller, IsEnabled = true, PadSettingChecksum = ps.PadSettingChecksum };
			_rows.Add(row);
			SettingsManager.UserSettings.Items.Add(row);
		}

		/// <summary>One turn of the engine's loop: the pass, then the wait until the next one is due.</summary>
		void Turn()
		{
			_pass(null, null);
			_pacer.Wait((int)_helper.Frequency);
		}

		/// <param name="before">What the scenario measured when the pass made objects, or null when it was not measured.</param>
		void AssertNothingPerPass(string scenario, string before)
		{
			for (var i = 0; i < 100; i++)
				Turn();
			var allocated = Allocations.FewestBytes(Windows, () =>
			{
				for (var i = 0; i < Passes; i++)
					Turn();
			});
			Console.WriteLine(scenario + ": " + allocated / (double)Passes + " bytes a pass.");
			Assert.IsTrue(allocated < Passes, scenario + ": " + Passes + " passes handed the collector " + allocated
				+ " bytes, " + allocated / (double)Passes + " a pass"
				+ (before == null ? "." : "; this scenario measured " + before + " when the pass made objects."));
		}

		/// <summary>The rows were routed and the controller read, so the passes measured did that work.</summary>
		static void AssertMeasured(UserDevice ud, int rows)
		{
			Assert.AreEqual(rows, DeviceRouting.Current.Rows.Length, "The rows were not routed, so converting them was not measured.");
			Assert.IsNotNull(ud.DiState, "The test controller was not read, so reading it was not measured.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("performance")]
		[Description("A pass with no game hands the collector nothing")]
		public void No_game()
		{
			SettingsManager.UpdateCurrentGame(null);
			AssertNothingPerPass("No game", "40 bytes with the window subscribed");
		}

		[TestMethod, TestCategory("engine"), TestCategory("performance")]
		[Description("A pass for a game with nothing mapped hands the collector nothing")]
		public void A_game_with_nothing_mapped()
		{
			SettingsManager.UpdateCurrentGame(Game);
			AssertNothingPerPass("A game with nothing mapped", "979 bytes");
		}

		[TestMethod, TestCategory("engine"), TestCategory("performance")]
		[Description("A pass with one controller on one tab hands the collector nothing")]
		public void One_controller_on_one_tab()
		{
			var ud = Device();
			Map(ud, MapTo.Controller1, Stored(Typical()));
			SettingsManager.UpdateCurrentGame(Game);
			AssertNothingPerPass("One controller on one tab", "6,130 bytes with the test controller");
			AssertMeasured(ud, 1);
		}

		[TestMethod, TestCategory("engine"), TestCategory("performance")]
		[Description("A pass with one controller on two tabs hands the collector nothing")]
		public void One_controller_on_two_tabs()
		{
			var ud = Device();
			var ps = Stored(Typical());
			Map(ud, MapTo.Controller1, ps);
			Map(ud, MapTo.Controller2, ps);
			SettingsManager.UpdateCurrentGame(Game);
			AssertNothingPerPass("One controller on two tabs", "7,197 bytes with the test controller");
			AssertMeasured(ud, 2);
		}

		[TestMethod, TestCategory("engine"), TestCategory("performance")]
		[Description("A pass that passes force on to the place a controller holds hands the collector nothing")]
		public void Pass_through_on()
		{
			var ud = Device();
			var ps = Typical();
			ps.ForcePassThrough = "1";
			ps.ForcePassThroughIndex = "0";
			Map(ud, MapTo.Controller1, Stored(ps));
			SettingsManager.UpdateCurrentGame(Game);
			Assert.IsTrue(DeviceRouting.Current.PassesForceThrough, "Pass-through is not routed, so it would not be measured.");
			AssertNothingPerPass("Pass-through on", "6,130 bytes with the test controller");
			AssertMeasured(ud, 1);
		}

		[TestMethod, TestCategory("engine"), TestCategory("performance")]
		[Description("A pass in an install with a thousand stored settings hands the collector nothing")]
		public void A_used_install()
		{
			for (var i = 0; i < 1000; i++)
				Stored(new PadSetting { PadSettingChecksum = Guid.NewGuid() });
			for (var i = 0; i < 300; i++)
			{
				var other = new UserSetting { InstanceGuid = Guid.NewGuid(), FileName = "other" + (i % 30) + ".exe", MapTo = 1 + i % 4, IsEnabled = true };
				_rows.Add(other);
				SettingsManager.UserSettings.Items.Add(other);
			}
			for (var i = 0; i < 30; i++)
			{
				var listed = new UserDevice { InstanceGuid = Guid.NewGuid() };
				_devices.Add(listed);
				SettingsManager.UserDevices.Items.Add(listed);
			}
			var ud = Device();
			Map(ud, MapTo.Controller1, Stored(Typical()));
			SettingsManager.UpdateCurrentGame(Game);
			AssertNothingPerPass("A used install", "17,043 bytes with the test controller");
			AssertMeasured(ud, 1);
		}

		[TestMethod, TestCategory("engine"), TestCategory("performance")]
		[Description("A pass with a row driven by a formula hands the collector nothing")]
		public void A_formula_row()
		{
			var ud = Device();
			var ps = Typical();
			ps.RightTrigger = "=a5*2";
			Map(ud, MapTo.Controller1, Stored(ps));
			SettingsManager.UpdateCurrentGame(Game);
			Assert.IsTrue(DeviceRouting.Current.RowMaps[0].Exists(x => x.Expression != null),
				"The formula did not compile, so working it out would not be measured.");
			AssertNothingPerPass("A formula row", null);
			AssertMeasured(ud, 1);
		}

		[TestMethod, TestCategory("engine"), TestCategory("performance")]
		[Description("A pass while the device list is being read hands the collector nothing")]
		public void While_the_device_list_is_read()
		{
			var ud = Device();
			Map(ud, MapTo.Controller1, Stored(Typical()));
			SettingsManager.UpdateCurrentGame(Game);
			// A list is wanted and a worker is reading it: the state of every pass until the read comes back.
			EngineSteps.DeviceListReadUnderWay(_helper);
			AssertNothingPerPass("While the device list is read", "32 bytes");
			AssertMeasured(ud, 1);
		}

		static T HelperField<T>(DInputHelper helper, string name)
		{
			return (T)typeof(DInputHelper).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(helper);
		}

		[TestMethod, TestCategory("engine"), TestCategory("performance")]
		[Description("While one controller is plugged in, a pass for the others, one of them left in the wrong place, hands the collector nothing and asks XInput nothing")]
		public void While_a_controller_is_plugged_in()
		{
			var ud = Device();
			Map(ud, MapTo.Controller1, Stored(Typical()));
			var game = new UserGame
			{
				FileName = Game.FileName,
				FileProductName = Game.FileProductName,
				EmulationType = (int)EmulationType.Virtual,
				EnableMask = Game.EnableMask,
			};
			// A bus client with nothing native behind it and no controller made, so the pass reaches the plug gate and never
			// the bus. It is never finalised, because there is nothing to let go of.
			var client = (ViGEmClient)FormatterServices.GetUninitializedObject(typeof(ViGEmClient));
			GC.SuppressFinalize(client);
			client.Targets = new Xbox360Controller[4];
			client.Feedbacks = new Xbox360FeedbackReceivedEventArgs[4];
			ViGEmClient.Current = client;
			SettingsManager.Options.XInputEnabled = true;
			// Controller 1 is being plugged in on a worker that has not finished. Controllers 2 and 4 want one too, and
			// Controller 3 was put in the wrong place last time, with other places taken than now.
			var plugging = HelperField<Task<VirtualError>[]>(_helper, "_plugging");
			var underWay = new TaskCompletionSource<VirtualError>().Task;
			plugging[0] = underWay;
			_helper.VirtualErrors[2] = VirtualError.PlaceWrong;
			HelperField<int[]>(_helper, "_misplacedWith")[2] = 0xF;
			SettingsManager.UpdateCurrentGame(game);
			// XInput answers no controller without a library, so a pass that looks at the places costs what it makes and
			// never reaches the system's XInput.
			var attempted = typeof(SystemXInput).GetField("_Attempted", BindingFlags.NonPublic | BindingFlags.Static);
			var getState = typeof(SystemXInput).GetField("_GetState", BindingFlags.NonPublic | BindingFlags.Static);
			var oldAttempted = attempted.GetValue(null);
			var oldGetState = getState.GetValue(null);
			attempted.SetValue(null, true);
			getState.SetValue(null, null);
			try
			{
				AssertNothingPerPass("While a controller is plugged in", null);
			}
			finally
			{
				attempted.SetValue(null, oldAttempted);
				getState.SetValue(null, oldGetState);
			}
			Assert.AreSame(underWay, plugging[0], "The plug under way was taken in, so the passes were not measured while it ran.");
			Assert.IsNull(plugging[1], "A second plug began while the first was under way.");
			AssertMeasured(ud, 1);
		}
	}
}
