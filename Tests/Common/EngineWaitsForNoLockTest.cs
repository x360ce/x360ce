// @under-test: App.v4/Common/SettingsManager.cs, App.v4/Common/DInput/DInputHelper.cs, App.v4/Common/DInput/DInputHelper.Step2.UpdateDiStates.cs, App.v4/Common/DInput/DInputHelper.Step3.UpdateXiStates.cs, App.v4/Common/DInput/XInputPlaces.cs, App.v4/ViGEm/Client/ViGEmClient.x360ce.cs, App.v4/Common/DInput/DInputHelper.XInputLibrarry.cs, App.v4/Common/DInput/DInputHelper.Step6.RetrieveXiStates.cs, App.v4/MainForm.cs
// @area: engine   @layer: unit
using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nefarius.ViGEm.Client;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using x360ce.App;
using x360ce.App.DInput;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>The engine's pass waits for no lock the interface thread also takes.</summary>
	/// <remarks>
	/// Each test holds a lock on another thread, as the interface does while it draws or loads, and runs the
	/// engine's own code against it. Code that waits for the lock does not finish in time.
	/// </remarks>
	[TestClass]
	public class EngineWaitsForNoLockTest
	{
		/// <summary>Long enough for code that waits for nothing, far too short for code that waits for a held lock.</summary>
		const int Limit = 1000;

		static object StaticField(Type type, string name)
		{
			return type.GetField(name, BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
		}

		static string DInputSource(string file)
		{
			return File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", file));
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("The options are read without the lock that loads them, and once a pass")]
		public void The_options_are_read_without_the_lock_that_loads_them()
		{
			GC.KeepAlive(SettingsManager.Options);
			Assert.IsTrue(HeldLock.Finishes(() => GC.KeepAlive(SettingsManager.Options.XInputEnabled), Limit,
				StaticField(typeof(SettingsManager), "OptionsLock")),
				"Reading an option waits for the lock the interface takes, and the engine reads options several times a pass.");
			var settings = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "SettingsManager.cs"));
			var getter = settings.IndexOf("public static XSettingsData<Options> OptionsData");
			var loaded = settings.IndexOf(".InitDefaults();", getter);
			var published = settings.IndexOf("_OptionsData = data;", getter);
			Assert.IsTrue(getter > 0 && loaded > getter && published > loaded,
				"The options are published before they are loaded, so a read without the lock can find them half made.");
			var step2 = DInputSource("DInputHelper.Step2.UpdateDiStates.cs");
			Assert.IsTrue(step2.IndexOf("var o = SettingsManager.Options;") < step2.IndexOf("for (int i = 0; i < userDevices.Length; i++)"),
				"The options are read again for every device on every pass.");
			Assert.IsFalse(DInputSource("DInputHelper.Step3.UpdateXiStates.cs").Contains("SettingsManager.Options"),
				"The conversion reads the options for every row, and uses nothing it reads.");
		}

		/// <summary>The lock a settings list takes for each change it applies and for each copy of itself.</summary>
		static object ListLock<T>(BindingListInvoked<T> list)
		{
			return typeof(BindingListInvoked<T>).GetField("OneChangeAtTheTime", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(list);
		}

		static object MapsLock(PadSetting ps)
		{
			return typeof(PadSetting).GetField("MapsLock", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ps);
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("Reading the devices and converting the rows wait for no settings list")]
		public void Steps_2_and_3_wait_for_no_settings_list()
		{
			// The test controller, so a state is read and converted with no hardware.
			var ud = TestDeviceHelper.NewUserDevice();
			ud.IsOnline = true;
			var ps = new PadSetting { ButtonA = "b1", LeftThumbAxisX = "a1", DPad = "p1", PadSettingChecksum = Guid.NewGuid() };
			var game = new UserGame { FileName = "no-list-lock.exe", FileProductName = "No list lock", EnableMask = (int)MapToMask.Controller1 };
			var row = new UserSetting { InstanceGuid = ud.InstanceGuid, FileName = game.FileName, MapTo = (int)MapTo.Controller1, IsEnabled = true, PadSettingChecksum = ps.PadSettingChecksum };
			var routing = DeviceRouting.Build(game, new[] { row }, new[] { ps }, new[] { ud });
			var helper = new DInputHelper();
			var di = EngineSteps.UpdateDiStates(helper);
			var xi = EngineSteps.UpdateXiStates(helper);
			Assert.IsTrue(HeldLock.Finishes(() => { di(null, game, null, routing); xi(routing); }, Limit,
				ListLock(SettingsManager.UserSettings.Items), ListLock(SettingsManager.UserDevices.Items),
				ListLock(SettingsManager.PadSettings.Items), MapsLock(ps)),
				"Reading the devices or converting the rows waits for a settings list or a mapping the interface holds while it draws a change.");
			Assert.IsNotNull(ud.DiState, "The test controller was not read, so the conversion was not timed.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("The place table is asked about, and its staleness checked, without a lock")]
		public void The_place_table_is_read_without_a_lock()
		{
			var real = XInputPlaces.ReadMachine;
			XInputPlaces.ReadMachine = () => new DeviceInfo[0];
			try
			{
				XInputPlaces.Invalidate();
				Assert.IsTrue(HeldLock.Finishes(() =>
				{
					XInputPlaces.ReadWhenStale();
					XInputPlaces.PlaceFor("HID\\VID_045E&PID_028E&IG_00\\1");
					XInputPlaces.IsMadeNotPluggedIn("USB\\VID_045E&PID_028E\\1");
					XInputPlaces.IsOneOfOurs("USB\\VID_045E&PID_028E\\1");
					XInputPlaces.Invalidate();
					// The first call above already started a read that is blocked on the held lock, so
					// this one takes the failed-CAS branch a pass runs while a read is under way.
					XInputPlaces.ReadWhenStale();
					GC.KeepAlive(XInputPlaces.IsStale);
				}, Limit, StaticField(typeof(XInputPlaces), "SyncRoot")),
					"The engine's check, or a row's lookup, waits for the lock the reader publishes under.");
			}
			finally
			{
				// Let the reads the test started finish on the stand-in machine before the real one is put back.
				var deadline = DateTime.UtcNow.AddSeconds(5);
				while (DateTime.UtcNow < deadline && XInputPlaces.IsStale)
				{
					XInputPlaces.ReadWhenStale();
					System.Threading.Thread.Sleep(10);
				}
				XInputPlaces.ReadMachine = real;
			}
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A bus client in place is answered without the lock the interface takes")]
		public void The_bus_client_in_place_is_answered_without_a_lock()
		{
			var old = ViGEmClient.Current;
			// A client with nothing native behind it. With one in place the check never reaches the bus.
			var client = (ViGEmClient)FormatterServices.GetUninitializedObject(typeof(ViGEmClient));
			GC.SuppressFinalize(client);
			try
			{
				ViGEmClient.Current = client;
				var answer = false;
				Assert.IsTrue(HeldLock.Finishes(() => answer = ViGEmClient.isVBusExists(true), Limit, ViGEmClient.ClientLock),
					"Every pass in virtual mode waits for the lock the Uninstall button takes.");
				Assert.IsTrue(answer, "A client in place is not answered as there.");
				const int passes = 20000;
				var allocated = Allocations.FewestBytes(5, () =>
				{
					for (var i = 0; i < passes; i++)
						ViGEmClient.isVBusExists(true);
				});
				Assert.IsTrue(allocated < passes, passes + " checks handed the collector " + allocated + " bytes.");
			}
			finally
			{
				ViGEmClient.Current = old;
			}
			var source = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "ViGEm", "Client", "ViGEmClient.x360ce.cs"));
			var dispose = source.Substring(source.IndexOf("public static void DisposeCurrent()"));
			var nulled = dispose.IndexOf("Current = null;");
			Assert.IsTrue(nulled >= 0 && nulled < dispose.IndexOf(".Dispose();"),
				"The client is let go of while still published, so a check without the lock can hand it out mid-dispose.");
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A pass in virtual mode with no client takes no lock while a refusal is still recent")]
		public void The_refusal_gate_is_answered_without_a_lock()
		{
			var errorField = typeof(ViGEmClient).GetField("_LastConnectError", BindingFlags.NonPublic | BindingFlags.Static);
			var tickField = typeof(ViGEmClient).GetField("_LastConnectTick", BindingFlags.NonPublic | BindingFlags.Static);
			var oldClient = ViGEmClient.Current;
			var oldError = errorField.GetValue(null);
			var oldTick = tickField.GetValue(null);
			try
			{
				ViGEmClient.Current = null;
				errorField.SetValue(null, VIGEM_ERROR.VIGEM_ERROR_BUS_NOT_FOUND);
				tickField.SetValue(null, Environment.TickCount);
				var answer = true;
				Assert.IsTrue(HeldLock.Finishes(() => answer = ViGEmClient.isVBusExists(true), Limit, ViGEmClient.ClientLock),
					"A pass with no client waits for the lock the Install and Uninstall buttons take while the gate is closed.");
				Assert.IsFalse(answer, "A closed gate is not answered as open.");
			}
			finally
			{
				errorField.SetValue(null, oldError);
				tickField.SetValue(null, oldTick);
				ViGEmClient.Current = oldClient;
			}
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A pass in virtual mode with no client takes no lock while a library load failure is still recent")]
		public void The_load_failure_gate_is_answered_without_a_lock()
		{
			var errorField = typeof(ViGEmClient).GetField("_LastConnectError", BindingFlags.NonPublic | BindingFlags.Static);
			var connectTickField = typeof(ViGEmClient).GetField("_LastConnectTick", BindingFlags.NonPublic | BindingFlags.Static);
			var failedField = typeof(ViGEmClient).GetField("_LastLoadFailed", BindingFlags.NonPublic | BindingFlags.Static);
			var loadTickField = typeof(ViGEmClient).GetField("_LastLoadFailTick", BindingFlags.NonPublic | BindingFlags.Static);
			var oldClient = ViGEmClient.Current;
			var oldError = errorField.GetValue(null);
			var oldConnectTick = connectTickField.GetValue(null);
			var oldFailed = failedField.GetValue(null);
			var oldLoadTick = loadTickField.GetValue(null);
			try
			{
				ViGEmClient.Current = null;
				// The connect gate stays open, so only the load-failure gate can be answering this.
				errorField.SetValue(null, VIGEM_ERROR.VIGEM_ERROR_NONE);
				connectTickField.SetValue(null, oldConnectTick);
				failedField.SetValue(null, true);
				loadTickField.SetValue(null, Environment.TickCount);
				var answer = true;
				Assert.IsTrue(HeldLock.Finishes(() => answer = ViGEmClient.isVBusExists(true), Limit, ViGEmClient.ClientLock),
					"A pass with no client waits for the lock the Install and Uninstall buttons take while a load failure is recent.");
				Assert.IsFalse(answer, "A closed load gate is not answered as open.");
			}
			finally
			{
				errorField.SetValue(null, oldError);
				connectTickField.SetValue(null, oldConnectTick);
				failedField.SetValue(null, oldFailed);
				loadTickField.SetValue(null, oldLoadTick);
				ViGEmClient.Current = oldClient;
			}
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A pass in virtual mode with no client takes no lock while the Visual C++ runtime is known to be missing")]
		public void The_missing_runtime_is_answered_without_a_lock()
		{
			var errorField = typeof(ViGEmClient).GetField("_LastConnectError", BindingFlags.NonPublic | BindingFlags.Static);
			var failedField = typeof(ViGEmClient).GetField("_LastLoadFailed", BindingFlags.NonPublic | BindingFlags.Static);
			var runtimeField = typeof(ViGEmClient).GetField("_Runtime", BindingFlags.NonPublic | BindingFlags.Static);
			var oldClient = ViGEmClient.Current;
			var oldError = errorField.GetValue(null);
			var oldFailed = failedField.GetValue(null);
			var oldRuntime = runtimeField.GetValue(null);
			try
			{
				ViGEmClient.Current = null;
				// The connect and load gates stay open, so only the runtime's answer can be answering this.
				errorField.SetValue(null, VIGEM_ERROR.VIGEM_ERROR_NONE);
				failedField.SetValue(null, false);
				runtimeField.SetValue(null, StaticField(typeof(ViGEmClient), "RuntimeMissing"));
				var answer = true;
				Assert.IsTrue(HeldLock.Finishes(() => answer = ViGEmClient.isVBusExists(true), Limit, ViGEmClient.ClientLock),
					"A pass with no client waits for the lock the Install and Uninstall buttons take while the runtime is missing.");
				Assert.IsFalse(answer, "A missing runtime is answered as a bus.");
			}
			finally
			{
				errorField.SetValue(null, oldError);
				failedField.SetValue(null, oldFailed);
				runtimeField.SetValue(null, oldRuntime);
				ViGEmClient.Current = oldClient;
			}
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("Loading, letting go of and reading XInput never wait for the lock a read holds")]
		public void XInput_is_never_waited_for()
		{
			var helper = new DInputHelper { UpdateDevicesEnabled = false };
			var game = new UserGame { FileName = "no-xinput-lock.exe", FileProductName = "No XInput lock" };
			var retrieve = EngineSteps.RetrieveXiStates(helper);
			// The clock runs, so the read falls due and is asked of the reader, which then waits for the held lock.
			XInputDisplayReadTest.StartClock(helper);
			System.Threading.Thread.Sleep(20);
			var state = -1;
			try
			{
				XInputDisplayReadTest.WithSystemXInput(() =>
				{
					Assert.IsTrue(HeldLock.Finishes(() =>
					{
						// No emulation: the library is to be let go of, but a read holds it.
						helper.CheckAndUnloadXInputLibrarry(game, false);
						helper.CheckAndLoadXInputLibrary(game, false);
						retrieve(game, true);
						state = XInputDisplayReadTest.DisplayRead(helper);
					}, Limit, SharpDX.XInput.Controller.XInputLock), "The input thread waits for the lock the XInput reader holds.");
					Assert.AreEqual(XInputDisplayReadTest.Asked, state, "The display read was not asked for, so asking for it was not timed.");
					Assert.IsTrue(SharpDX.XInput.Controller.IsLoaded, "The library was let go of while a read held it.");
					// The reader reads once the lock is free, and the library is let go of once it has.
					XInputDisplayReadTest.WaitForAnswer(helper);
					helper.CheckAndUnloadXInputLibrarry(game, false);
					Assert.IsFalse(SharpDX.XInput.Controller.IsLoaded, "The library is never let go of once the read has finished.");
				});
			}
			finally
			{
				helper.Dispose();
			}
			var dir = Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput");
			Assert.IsFalse(File.ReadAllText(Path.Combine(dir, "DInputHelper.XInputLibrarry.cs")).Contains("lock (Controller.XInputLock)"),
				"Loading or letting go of XInput waits for a read.");
			StringAssert.Contains(File.ReadAllText(Path.Combine(dir, "DInputHelper.Step2.UpdateDiStates.cs")),
				"ud.DeviceEffects == null && System.Threading.Monitor.TryEnter(Controller.XInputLock)",
				"Reading a new device's effects waits for a read.");
			var form = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "MainForm.cs"));
			var close = form.Substring(form.IndexOf("private void OnCloseAction("));
			close = close.Substring(0, close.IndexOf("\n\t\t}"));
			Assert.IsFalse(close.Contains("XInputLock"), "Closing the window takes a lock the input thread takes.");
			StringAssert.Contains(close, "StopTestVibration()", "Closing does not stop the Test sliders' motors through the path they were started by.");
		}

		static object InstanceField(object target, string name)
		{
			return target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A whole pass finishes while every lock the interface takes is held")]
		public void A_whole_pass_waits_for_no_lock_the_interface_takes()
		{
			if (Global.DHelper == null)
				Global.InitDHelperHelper();
			var oldGame = SettingsManager.CurrentGame;
			var oldGetXInputStates = SettingsManager.Options.GetXInputStates;
			var oldReadMachine = XInputPlaces.ReadMachine;
			var ud = TestDeviceHelper.NewUserDevice();
			ud.IsOnline = true;
			var ps = EnginePassAllocationTest.Typical();
			var game = EnginePassAllocationTest.Game;
			var row = new UserSetting { InstanceGuid = ud.InstanceGuid, FileName = game.FileName, MapTo = (int)MapTo.Controller1, IsEnabled = true, PadSettingChecksum = ps.PadSettingChecksum };
			DeviceRouting.Watch();
			try
			{
				XInputPlaces.ReadMachine = () => new DeviceInfo[0];
				SettingsManager.Options.GetXInputStates = false;
				SettingsManager.PadSettings.Items.Add(ps);
				SettingsManager.UserDevices.Items.Add(ud);
				SettingsManager.UserSettings.Items.Add(row);
				SettingsManager.UpdateCurrentGame(game);
				XInputPlaces.Invalidate();
				XInputPlaces.ReadIfStale();
				var helper = new DInputHelper { UpdateDevicesEnabled = false };
				// A list is wanted and a worker is reading it, so the passes go the way every pass of a read goes.
				EngineSteps.DeviceListReadUnderWay(helper);
				var pass = EngineSteps.RefreshAll(helper);
				Assert.IsTrue(HeldLock.Finishes(() => { for (var i = 0; i < 1000; i++) pass(null, null); }, Limit,
					// The options, the game and its tab switches, the lists the interface holds while it delivers a change,
					// the devices list's own root, the mappings, and the place table.
					StaticField(typeof(SettingsManager), "OptionsLock"), ListLock(SettingsManager.OptionsData.Items),
					SettingsManager.CurrentGameLock, ListLock(SettingsManager.UserGames.Items),
					ListLock(SettingsManager.UserSettings.Items), ListLock(SettingsManager.UserDevices.Items), ListLock(SettingsManager.PadSettings.Items),
					SettingsManager.UserDevices.SyncRoot, MapsLock(ps), StaticField(typeof(XInputPlaces), "SyncRoot"),
					// XInput and the bus, which the interface and the workers read, load, let go of and connect under these.
					SharpDX.XInput.Controller.XInputLock, StaticField(typeof(SharpDX.XInput.Controller), "loadLock"),
					StaticField(typeof(SystemXInput), "LoadLock"), ViGEmClient.ClientLock,
					// The helper's own: the reorder worker takes the first to know the pass under way has finished, and the
					// window starts a device list read under the second.
					InstanceField(helper, "PassLock"), InstanceField(helper, "_deviceListStartLock")),
					"A thousand passes did not finish while the interface's locks were held: the pass waits for one of them.");
				Assert.IsNotNull(ud.DiState, "The test controller was not read, so the pass was not timed with a device.");
			}
			finally
			{
				SettingsManager.UpdateCurrentGame(oldGame);
				SettingsManager.UserSettings.Items.Remove(row);
				SettingsManager.UserDevices.Items.Remove(ud);
				SettingsManager.PadSettings.Items.Remove(ps);
				SettingsManager.Options.GetXInputStates = oldGetXInputStates;
				XInputPlaces.ReadMachine = oldReadMachine;
			}
			// The loop around the pass only tries the lock the reorder worker takes, with no wait, and skips the pass while
			// it is held.
			var main = DInputSource("DInputHelper.cs");
			Assert.IsFalse(main.Contains("lock (PassLock)") || main.Contains("Monitor.Enter(PassLock"),
				"The loop waits for the lock the reorder worker takes to know a pass has finished.");
			StringAssert.Contains(main, "&& Monitor.TryEnter(PassLock))",
				"The loop does not try the lock the reorder worker takes without waiting.");
		}
	}
}
