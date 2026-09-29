// @under-test: App.v4/Common/DInput/DInputHelper.cs, App.v4/Common/DInput/DInputHelper.Step3.UpdateXiStates.cs, App.v4/MainForm.cs
// @area: engine   @layer: unit
using JocysCom.ClassLibrary.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using x360ce.App;
using x360ce.App.DInput;

namespace x360ce.Tests
{
	/// <summary>The engine publishes what the window draws, and hands the window nothing on a pass.</summary>
	/// <remarks>
	/// A handler told after every pass would run on the engine thread, take a lock the window holds while it refreshes
	/// its lists, read the options under their lock and post a task ten times a second. The window draws on its own
	/// timer, when the engine's pass count has moved.
	/// </remarks>
	[TestClass]
	public class EngineEventsTest
	{
		static string Source(params string[] path)
		{
			var parts = new string[path.Length + 1];
			parts[0] = Ui.RepoRoot.FullName;
			Array.Copy(path, 0, parts, 1, path.Length);
			return File.ReadAllText(Path.Combine(parts));
		}

		[TestMethod, TestCategory("engine"), TestCategory("critical")]
		[Description("A pass counts itself and hands the window nothing")]
		public void A_pass_counts_itself_and_hands_the_window_nothing()
		{
			var oldGame = SettingsManager.CurrentGame;
			var oldReadMachine = XInputPlaces.ReadMachine;
			if (Global.DHelper == null)
				Global.InitDHelperHelper();
			SettingsManager.UpdateCurrentGame(null);
			// The place table is read from a stand-in machine, so a pass never walks the real device tree.
			XInputPlaces.ReadMachine = () => new DeviceInfo[0];
			XInputPlaces.Invalidate();
			XInputPlaces.ReadIfStale();
			try
			{
				var helper = new DInputHelper();
				var pass = EngineSteps.RefreshAll(helper);
				var count = helper.PassCount;
				pass(null, null);
				pass(null, null);
				Assert.AreEqual(count + 2, helper.PassCount, "The window cannot tell a pass has run.");
				const int passes = 20000;
				var allocated = Allocations.FewestBytes(5, () =>
				{
					for (var i = 0; i < passes; i++)
						pass(null, null);
				});
				Assert.IsTrue(allocated < passes, passes + " passes handed the collector " + allocated + " bytes.");
			}
			finally
			{
				SettingsManager.UpdateCurrentGame(oldGame);
				XInputPlaces.ReadMachine = oldReadMachine;
			}
			// Only what changes rarely is pushed. An event a pass raised would run its handlers on the engine thread.
			CollectionAssert.AreEquivalent(new[] { "DevicesUpdated", "StatesRetrieved", "XInputReloaded" },
				typeof(DInputHelper).GetEvents().Select(x => x.Name).ToArray(),
				"The engine offers the window an event other than a device change, a library reload or an XInput fault.");
			var main = Source("App.v4", "Common", "DInput", "DInputHelper.cs");
			foreach (var gone in new[] { "UpdateCompleted", "FrequencyUpdated", "DiUpdatesLock" })
				Assert.IsFalse(main.Contains(gone), "The pass still has " + gone + ".");
			var loop = main.Substring(main.IndexOf("void ThreadAction()"));
			StringAssert.Contains(loop, "if (_passFault != ex.GetType())", "A pass that fails the same way is written on every pass.");
			StringAssert.Contains(loop, "_passFault = null;", "A pass that goes through does not make the next failure news.");
			Assert.IsFalse(Source("App.v4", "Common", "DInput", "DInputHelper.Step3.UpdateXiStates.cs").Contains("StatesUpdated"),
				"The conversion raises an event nothing listens to.");
			var form = Source("App.v4", "MainForm.cs");
			Assert.IsFalse(form.Contains("DHelper.UpdateCompleted +="), "The window is told after every pass, on the engine thread.");
			Assert.IsFalse(form.Contains("DHelper.FrequencyUpdated +="), "The window is told the rate on the engine thread.");
			StringAssert.Contains(form, ".PassCount", "The window does not tell whether the engine has run since it last drew.");
			var enable = form.Substring(form.IndexOf("private void EnableFormUpdates(bool enable)"));
			enable = enable.Substring(0, enable.IndexOf("\n\t\t}"));
			var lockEnd = enable.IndexOf("\n\t\t\t}", enable.IndexOf("lock (LockFormEvents)"));
			Assert.IsTrue(enable.IndexOf("DHelper_DevicesUpdated(null, null)") > lockEnd,
				"The device lists are refreshed while the window holds the lock the device thread takes.");
		}
	}
}
