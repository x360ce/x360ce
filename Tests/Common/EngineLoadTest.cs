// @under-test: App.v4/Common/DInput/DInputHelper.cs, Engine/JocysCom/Common/HiResTimer.cs
// @area: engine   @layer: perf
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using x360ce.App;
using x360ce.App.DInput;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>The engine's own thread, reading the controllers connected to this machine, for a fixed time: how fast it can go and what it costs.</summary>
	/// <remarks>
	/// <para>
	/// Opt-in: tagged <c>load</c>, which <c>Run-Tests.ps1</c> leaves out. Run it on a quiet machine with the controllers
	/// that matter connected: <c>vstest.console.exe x360ce.Tests.dll /TestCaseFilter:TestCategory=load</c>.
	/// </para>
	/// <para>
	/// The engine starts as the program starts it: its thread, its DirectInput, its window and the Raw Input hub. It reads
	/// the device list, and every connected DirectInput controller is mapped to a controller tab of a game that uses no
	/// emulation, so the passes read and convert every device and never reach the virtual bus. An interval of 0 is the
	/// test's own setting, not a user's: the pacer then does not wait, and the passes run back to back.
	/// </para>
	/// <para>
	/// Reports the rate of each second (median, lowest, highest), the process's processor time, what the domain handed the
	/// collector and the collections it made, the working set, and the time of each step of a pass. Nothing is asserted
	/// about the numbers: they depend on the machine and on what is connected.
	/// </para>
	/// </remarks>
	[TestClass]
	public class EngineLoadTest
	{
		/// <summary>Seconds measured, after the devices are read.</summary>
		const int MeasureSeconds = 30;

		/// <summary>Seconds run before measuring, so the first passes' one-off costs are not counted.</summary>
		const int WarmUpSeconds = 5;

		static readonly UserGame Game = new UserGame
		{
			FileName = "engine-load.exe",
			FileProductName = "Engine load",
			EmulationType = (int)EmulationType.None,
			EnableMask = (int)(MapToMask.Controller1 | MapToMask.Controller2 | MapToMask.Controller3 | MapToMask.Controller4),
		};

		static readonly FieldInfo TimeStepsField = typeof(DInputHelper).GetField("_timeSteps", BindingFlags.NonPublic | BindingFlags.Static);
		static readonly long[] StepTicks = (long[])typeof(DInputHelper).GetField("StepTicks", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
		static readonly string[] StepNames = (string[])typeof(DInputHelper).GetField("StepNames", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

		UserGame _oldGame;
		bool _oldGetXInputStates;
		UserDevice[] _oldDevices;
		readonly List<UserSetting> _rows = new List<UserSetting>();
		readonly List<PadSetting> _settings = new List<PadSetting>();
		DInputHelper _helper;

		[TestInitialize]
		public void Setup()
		{
			if (Global.DHelper == null)
				Global.InitDHelperHelper();
			_oldGame = SettingsManager.CurrentGame;
			_oldGetXInputStates = SettingsManager.Options.GetXInputStates;
			_oldDevices = SettingsManager.UserDevices.ItemsToArraySynchronized();
			// Switched on before the engine starts, so the first count is not the one that switches it on.
			AppDomain.MonitoringIsEnabled = true;
			// No window shows the XInput states.
			SettingsManager.Options.GetXInputStates = false;
			DeviceRouting.Watch();
		}

		[TestCleanup]
		public void Restore()
		{
			if (_helper != null)
			{
				_helper.Stop();
				_helper.RawInput.Stop();
			}
			TimeStepsField.SetValue(null, false);
			SettingsManager.UpdateCurrentGame(_oldGame);
			foreach (var row in _rows)
				SettingsManager.UserSettings.Items.Remove(row);
			foreach (var ps in _settings)
				SettingsManager.PadSettings.Items.Remove(ps);
			// The engine has stopped, so the devices it opened can be let go of.
			foreach (var ud in SettingsManager.UserDevices.ItemsToArraySynchronized().Except(_oldDevices).ToArray())
			{
				SettingsManager.UserDevices.Items.Remove(ud);
				if (ud.Device != null)
					ud.Device.Dispose();
			}
			SettingsManager.Options.GetXInputStates = _oldGetXInputStates;
		}

		/// <param name="intervalUs">Microseconds between passes: 0 runs them back to back, the others are the polling rates a person can choose.</param>
		[TestMethod, TestCategory("engine"), TestCategory("load"), Timeout(180000)]
		[Description("The engine's thread reads every connected controller for a fixed time; reports its rate, processor time, allocation, collections and working set")]
		[DataRow(0), DataRow((int)UpdateFrequency.us250_4000Hz), DataRow((int)UpdateFrequency.ms1_1000Hz),
			DataRow((int)UpdateFrequency.ms2_500Hz), DataRow((int)UpdateFrequency.ms4_250Hz), DataRow((int)UpdateFrequency.ms8_125Hz)]
		public void Engine_polls_connected_controllers(int intervalUs)
		{
			SettingsManager.UpdateCurrentGame(Game);
			_helper = new DInputHelper { Frequency = (UpdateFrequency)intervalUs };
			_helper.Start();
			// The first pass asks for the device list, which a worker reads.
			Ui.WaitFor(() => _helper.RefreshDevicesCount > 0 ? "" : null, TimeSpan.FromSeconds(30), "the device list");
			var controllers = SettingsManager.UserDevices.ItemsToArraySynchronized()
				.Where(x => x.IsOnline && x.IsDirectInput && !x.IsKeyboard && !x.IsMouse)
				.ToArray();
			if (controllers.Length == 0)
				Assert.Inconclusive("No DirectInput controller is connected, so nothing would be read.");
			for (var i = 0; i < controllers.Length; i++)
				Map(controllers[i], (MapTo)(1 + i % 4));
			Ui.WaitFor(() => controllers.All(x => x.SourceState != null) ? "" : null, TimeSpan.FromSeconds(15), "a reading of every controller");
			Thread.Sleep(WarmUpSeconds * 1000);
			Measure(intervalUs, controllers);
		}

		void Map(UserDevice ud, MapTo controller)
		{
			var ps = EnginePassAllocationTest.Typical();
			_settings.Add(ps);
			SettingsManager.PadSettings.Items.Add(ps);
			var row = new UserSetting { InstanceGuid = ud.InstanceGuid, FileName = Game.FileName, MapTo = (int)controller, IsEnabled = true, PadSettingChecksum = ps.PadSettingChecksum };
			_rows.Add(row);
			SettingsManager.UserSettings.Items.Add(row);
		}

		void Measure(int intervalUs, UserDevice[] controllers)
		{
			// The process is read outside the counted allocation: a refresh makes a list of every process on the machine.
			var process = Process.GetCurrentProcess();
			TimeStepsField.SetValue(null, true);
			Array.Clear(StepTicks, 0, StepTicks.Length);
			process.Refresh();
			var cpu = process.TotalProcessorTime;
			var workingSet = process.WorkingSet64;
			var gc = new[] { GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2) };
			var allocated = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;
			var passes = _helper.PassCount;
			var watch = Stopwatch.StartNew();
			var rates = new List<int>();
			var last = passes;
			for (var second = 1; second <= MeasureSeconds; second++)
			{
				var sleep = second * 1000 - watch.ElapsedMilliseconds;
				if (sleep > 0)
					Thread.Sleep((int)sleep);
				var now = _helper.PassCount;
				rates.Add(now - last);
				last = now;
			}
			var seconds = watch.Elapsed.TotalSeconds;
			var total = _helper.PassCount - passes;
			var bytes = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize - allocated;
			var collections = new[] { GC.CollectionCount(0) - gc[0], GC.CollectionCount(1) - gc[1], GC.CollectionCount(2) - gc[2] };
			process.Refresh();
			var cpuMs = (process.TotalProcessorTime - cpu).TotalMilliseconds;
			var sorted = rates.OrderBy(x => x).ToArray();
			Console.WriteLine("Interval: {0}", intervalUs == 0 ? "none, passes back to back" : intervalUs + " µs, " + 1000000 / intervalUs + " Hz asked");
			Console.WriteLine("Controllers read: {0}", string.Join("; ", controllers.Select(x => x.ProductName + (x.SourceState == null ? " (not read)" : ""))));
			Console.WriteLine("Passes a second over {0} s: median {1}, lowest {2}, highest {3}; {4:0} on average",
				MeasureSeconds, sorted[sorted.Length / 2], sorted[0], sorted[sorted.Length - 1], total / seconds);
			Console.WriteLine("Each second: {0}", string.Join(" ", rates));
			Console.WriteLine("Process CPU: {0:0.0} % of one core, {1:0.00} % of all {2}; {3:0.0} µs a pass",
				cpuMs / 10 / seconds, cpuMs / 10 / seconds / Environment.ProcessorCount, Environment.ProcessorCount,
				total == 0 ? 0 : cpuMs * 1000 / total);
			Console.WriteLine("Allocated: {0:0.0} KB/s, {1:0.0} bytes a pass", bytes / 1024.0 / seconds, total == 0 ? 0 : (double)bytes / total);
			Console.WriteLine("Collections: gen0 {0}, gen1 {1}, gen2 {2}", collections[0], collections[1], collections[2]);
			Console.WriteLine("Working set: {0:0.0} MB at start, {1:0.0} MB at end, {2:0.0} MB peak; private {3:0.0} MB",
				workingSet / 1048576.0, process.WorkingSet64 / 1048576.0, process.PeakWorkingSet64 / 1048576.0, process.PrivateMemorySize64 / 1048576.0);
			for (var i = 0; i < StepNames.Length; i++)
				Console.WriteLine("Step {0,-20} {1,8:0.00} µs a pass, {2,5:0.0} % of the time",
					StepNames[i], total == 0 ? 0 : StepTicks[i] * 1000000.0 / Stopwatch.Frequency / total,
					StepTicks[i] * 100.0 / Stopwatch.Frequency / seconds);
			Assert.IsTrue(total > 0, "The engine ran no pass.");
		}
	}
}
