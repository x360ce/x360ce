// @under-test: App.v4/Common/DInput/DInputHelper.Test.cs, App.v4/Common/DInput/DeviceTest.cs, Engine/Input/Processors/DeviceTestStatistics.cs
// @area: devices   @layer: integration
using JocysCom.ClassLibrary;
using JocysCom.ClassLibrary.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nefarius.ViGEm.Client;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using x360ce.App;
using x360ce.App.DInput;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>The engine while the Test Device window tests a Test Controller: only that device is read, the controllers are told nothing is pressed, the figures fill, and taking the test off resumes everything.</summary>
	[TestClass]
	public class DeviceTestEngineTest
	{
		UserGame _oldGame;
		Func<DeviceInfo[]> _oldReadMachine;
		bool _oldGetXInputStates;
		ViGEmClient _oldClient;
		readonly List<UserSetting> _rows = new List<UserSetting>();
		readonly List<PadSetting> _settings = new List<PadSetting>();
		readonly List<UserDevice> _devices = new List<UserDevice>();
		DInputHelper _helper;

		[TestInitialize]
		public void Setup()
		{
			if (Global.DHelper == null)
				Global.InitDHelperHelper();
			_oldGame = SettingsManager.CurrentGame;
			_oldReadMachine = XInputPlaces.ReadMachine;
			_oldGetXInputStates = SettingsManager.Options.GetXInputStates;
			_oldClient = ViGEmClient.Current;
			XInputPlaces.ReadMachine = () => new DeviceInfo[0];
			SettingsManager.Options.GetXInputStates = false;
			ViGEmClient.Current = null;
			XInputPlaces.Invalidate();
			XInputPlaces.ReadIfStale();
			DeviceRouting.Watch();
			_helper = new DInputHelper { UpdateDevicesEnabled = false };
			// The clock each read is timed by, which the engine's thread starts.
			_helper.watch.Start();
		}

		[TestCleanup]
		public void Restore()
		{
			_helper.Testing = null;
			SettingsManager.UpdateCurrentGame(_oldGame);
			foreach (var row in _rows)
				SettingsManager.UserSettings.Items.Remove(row);
			foreach (var ps in _settings)
				SettingsManager.PadSettings.Items.Remove(ps);
			foreach (var device in _devices)
				SettingsManager.UserDevices.Items.Remove(device);
			SettingsManager.Options.GetXInputStates = _oldGetXInputStates;
			XInputPlaces.ReadMachine = _oldReadMachine;
			ViGEmClient.Current = _oldClient;
			_helper.RawInput.Stop();
		}

		UserDevice Mapped(MapTo controller)
		{
			var ud = TestControllerDevice.NewUserDevice();
			ud.IsOnline = true;
			_devices.Add(ud);
			SettingsManager.UserDevices.Items.Add(ud);
			var ps = EnginePassAllocationTest.Typical();
			_settings.Add(ps);
			SettingsManager.PadSettings.Items.Add(ps);
			var row = new UserSetting { InstanceGuid = ud.InstanceGuid, FileName = EnginePassAllocationTest.Game.FileName, MapTo = (int)controller, IsEnabled = true, PadSettingChecksum = ps.PadSettingChecksum };
			_rows.Add(row);
			SettingsManager.UserSettings.Items.Add(row);
			return ud;
		}

		static void Run(EngineSteps.Pass pass, int milliseconds)
		{
			var watch = Stopwatch.StartNew();
			while (watch.ElapsedMilliseconds < milliseconds)
				pass(null, null);
		}

		[TestMethod, TestCategory("devices")]
		[Description("A test reads only its device, sends the controllers a neutral state once, records reads the statistics turn into figures, and taken off resumes every device")]
		public void A_test_pauses_the_engine_and_records_its_device()
		{
			var tested = Mapped(MapTo.Controller1);
			var other = Mapped(MapTo.Controller2);
			SettingsManager.UpdateCurrentGame(EnginePassAllocationTest.Game);
			var pass = EngineSteps.RefreshAll(_helper);
			Run(pass, 50);
			Assert.IsNotNull(other.SourceState, "The other device was not read before the test, so its pause would not show.");
			// Something held on a controller, which the test must let go of.
			_helper.CombinedXiStates[1].Gamepad.Buttons = GamepadButtonFlags.A;
			var test = new DeviceTest(tested);
			_helper.Testing = test;
			var otherRead = other.SourceStateTime;
			var steps = new DeviceTestSteps(App.Forms.TestDeviceForm.LayoutOf(tested), Stopwatch.Frequency);
			var statistics = steps.Statistics;
			pass(null, null);
			var latest = new int[DeviceTest.Channels];
			Assert.IsTrue(test.Latest(latest), "The first test pass recorded nothing.");
			statistics.Start(Stopwatch.GetTimestamp(), test.Reads, latest);
			Run(pass, 300);
			test.TakeInto(steps);
			statistics.StartMoving(Stopwatch.GetTimestamp(), test.Reads);
			Run(pass, 700);
			test.TakeInto(steps);
			statistics.Stop(Stopwatch.GetTimestamp(), test.Reads);
			Assert.IsTrue(test.VirtualNeutral, "The controllers were not sent a neutral state.");
			Assert.IsTrue(_helper.CombinedXiStates.All(x => x.Gamepad.Buttons == GamepadButtonFlags.None), "A button stayed pressed on a controller while testing.");
			Assert.AreEqual(otherRead, other.SourceStateTime, "Another device was read while one was tested.");
			var summary = statistics.Summary(Stopwatch.GetTimestamp());
			Console.WriteLine(App.Forms.TestDeviceForm.SummaryText(summary, test.Dropped));
			Assert.IsTrue(summary.Reads > 1000, "The test read its device " + summary.Reads + " times in a second, so it waited between reads.");
			Assert.IsTrue(statistics.Channels[0].Distinct > 1, "The moving Test Controller gave one value: the figures did not fill.");
			Assert.IsTrue(statistics.Channels[0].RangePercent > 0);
			_helper.Testing = null;
			Run(pass, 50);
			Assert.AreNotEqual(otherRead, other.SourceStateTime, "Taking the test off did not resume the other device.");
		}
	}
}
