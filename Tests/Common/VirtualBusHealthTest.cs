// @under-test: App.v4/Common/DInput/DInputHelper.Step5.VirtualDevices.cs, App.v4/Common/DInput/DInputHelper.Step6.RetrieveXiStates.cs, App.v4/Issues/VirtualDriverNotWorkingIssue.cs, App.v4/Issues/VirtualDeviceDriverIssue.cs, App.v4/Issues/UnfinishedVirtualPadsIssue.cs, App.v4/Common/DInput/VirtualDriverInstaller.cs, App.v4/Controls/OptionsUserControl.cs, App.v4/ViGEm/Client/ViGEmClient.x360ce.cs
// @area: diagnostics   @layer: unit
using JocysCom.ClassLibrary.Controls.IssuesControl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nefarius.ViGEm.Client;
using System.IO;
using System.Linq;
using x360ce.App;
using x360ce.App.DInput;
using x360ce.App.Issues;
using x360ce.Engine;
using x360ce.Engine.Data;
using static x360ce.Tests.VirtualBusHealthFixtures;

namespace x360ce.Tests
{
	/// <summary>What the engine records of the bus's own answers: plug and feed-drop counts, recency windows, and the plug gate.</summary>
	/// <remarks>
	/// A bus that is installed but refuses its work leaves pads that games never detect. The engine
	/// records what the bus refuses: connections, plugs and reports. These tests judge the counting and
	/// timing rules on their own, without a bus, so every branch can be driven, and that the bus is
	/// repaired from one shared place and never dialled from an issue check.
	/// </remarks>
	[TestClass]
	public class VirtualBusHealthTest
	{
		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("Only plugs the bus took part in count, and one that arrives anywhere clears the count")]
		public void Plug_failures_count_only_what_the_bus_did()
		{
			Assert.AreEqual(1, DInputHelper.NextPlugFailures(0, VirtualError.Other));
			Assert.AreEqual(3, DInputHelper.NextPlugFailures(2, VirtualError.PlaceNotGiven),
				"A controller the bus accepted and Windows never built is the bus failing.");
			Assert.AreEqual(2, DInputHelper.NextPlugFailures(2, VirtualError.PlaceTaken),
				"A taken place never reached the bus, so it says nothing about it.");
			Assert.AreEqual(2, DInputHelper.NextPlugFailures(2, VirtualError.Missing),
				"No bus client is the connection's fault, which is judged on its own.");
			Assert.AreEqual(0, DInputHelper.NextPlugFailures(2, VirtualError.None));
			Assert.AreEqual(0, DInputHelper.NextPlugFailures(2, VirtualError.PlaceWrong),
				"A controller that arrived, even in the wrong place, shows the bus works.");
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("Refused reports close together make one run; one long after starts a new run")]
		public void Feed_drops_close_together_make_one_run()
		{
			Assert.AreEqual(1, DInputHelper.NextFeedDrops(0, 0, Now));
			Assert.AreEqual(2, DInputHelper.NextFeedDrops(1, Now - 5000, Now));
			Assert.AreEqual(1, DInputHelper.NextFeedDrops(4, Now - DInputHelper.FeedDropWindowMs, Now),
				"A drop long after the last - a driver update an hour later - is not the same fault.");
			Assert.AreEqual(3, DInputHelper.NextFeedDrops(2, int.MaxValue - 499, int.MinValue + 500),
				"The tick count wraps after 49 days; a drop just before the wrap is still recent.");
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("A moment is recent for its window and no longer, across the tick counter wrapping round")]
		public void A_moment_is_recent_for_its_window_only()
		{
			const int window = 5000;
			Assert.IsTrue(DInputHelper.IsRecent(Now, Now, window), "A moment is recent as it happens.");
			Assert.IsTrue(DInputHelper.IsRecent(Now - window + 1, Now, window));
			Assert.IsFalse(DInputHelper.IsRecent(Now - window, Now, window), "The window ends once its length has passed.");
			Assert.IsFalse(DInputHelper.IsRecent(Now + 1, Now, window), "A moment still to come is not recent.");
			// Recorded just before the counter turns negative at 24.9 days, and read just after.
			Assert.IsTrue(DInputHelper.IsRecent(int.MaxValue - 499, int.MinValue + 500, window));
			// Recorded just before the counter comes round to 0 at 49.7 days, and read just after.
			Assert.IsTrue(DInputHelper.IsRecent(-500, 500, window));
			// Recorded 25 days ago, when the difference of the two counts has turned negative.
			Assert.IsFalse(DInputHelper.IsRecent(LongAgo, Now, window), "A moment from 25 days ago is recent.");
			Assert.IsFalse(DInputHelper.IsRecent(0, int.MinValue + Now, window), "A moment never recorded is recent 25 days after Windows started.");
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("Every window after a recorded moment is judged by IsRecent, not written out again")]
		public void Every_window_is_judged_by_IsRecent()
		{
			var app = Path.Combine(Ui.RepoRoot.FullName, "App.v4");
			var uses = new[]
			{
				new[] { Path.Combine("ViGEm", "Client", "ViGEmClient.x360ce.cs"), "DInputHelper.IsRecent(_LastConnectTick, Environment.TickCount, ConnectRetryMs)" },
				new[] { Path.Combine("Common", "DInput", "DInputHelper.Step5.VirtualDevices.cs"), "IsRecent(lastDropTick, now, FeedDropWindowMs)" },
				new[] { Path.Combine("Issues", "VirtualDriverNotWorkingIssue.cs"), "DInputHelper.IsRecent(health.ConnectTick, health.Now, ConnectErrorFreshMs)" },
				new[] { Path.Combine("Issues", "VirtualDriverNotWorkingIssue.cs"), "DInputHelper.IsRecent(pad.LastFeedDropTick, health.Now, DInputHelper.FeedDropWindowMs)" },
			};
			foreach (var use in uses)
			{
				var source = File.ReadAllText(Path.Combine(app, use[0]));
				StringAssert.Contains(source, use[1], use[0] + " judges its window some other way.");
				Assert.IsFalse(source.Contains("elapsed >= 0"), use[0] + " writes the window rule out again.");
			}
		}

		/// <summary>Whether a controller waits before asking for a place, asked the way the engine's plug gate asks it.</summary>
		static bool Waiting(int until, int now)
		{
			return DInputHelper.IsWaiting(until, now, DInputHelper.PlugRetryMs);
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("A controller is plugged in at any uptime, and a refused one waits its time and no longer")]
		public void A_plug_is_tried_at_any_uptime()
		{
			// Each controller's next attempt starts at 0. Environment.TickCount is negative from 24.9 to
			// 49.7 days after Windows starts, and compared by sign alone a 0 lies ahead of it for all that
			// time, so no controller is plugged in.
			Assert.IsFalse(Waiting(0, Now), "A fresh start, 1000 seconds after Windows started.");
			Assert.IsFalse(Waiting(0, int.MinValue + Now), "A fresh start 25 days after Windows started.");
			Assert.IsFalse(Waiting(0, -1000000000), "A fresh start 37 days after Windows started.");
			// A refused controller waits its time, then asks again.
			var until = Now + DInputHelper.PlugRetryMs;
			Assert.IsTrue(Waiting(until, Now));
			Assert.IsTrue(Waiting(until, until - 1));
			Assert.IsFalse(Waiting(until, until));
			Assert.IsFalse(Waiting(until, until + 60000));
			// Set just before the counter turns negative at 24.9 days, and read just after.
			until = unchecked(int.MaxValue - 500 + DInputHelper.PlugRetryMs);
			Assert.IsTrue(Waiting(until, int.MinValue + 500), "A wait set half a second before the wrap ended at once.");
			Assert.IsFalse(Waiting(until, unchecked(until + 1)));
			// Set just before the counter comes round to 0 at 49.7 days, and read just after.
			Assert.IsTrue(Waiting(-500 + DInputHelper.PlugRetryMs, 500));
			Assert.IsFalse(Waiting(-500 + DInputHelper.PlugRetryMs, DInputHelper.PlugRetryMs));
			// A retry time left from a refusal 25 days ago, the last time the game used virtual controllers.
			Assert.IsFalse(Waiting(LongAgo, Now), "A retry time from 25 days ago still held the controller back.");
			// The gate needs a bus to run, so its source is read instead.
			var step5 = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName,
				"App.v4", "Common", "DInput", "DInputHelper.Step5.VirtualDevices.cs"));
			StringAssert.Contains(step5, "IsWaiting(_NextPlugAttempt[i - 1], Environment.TickCount, PlugRetryMs)",
				"The plug gate compares its retry time some other way.");
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("The bus is repaired from one place, so every issue and page that offers it lets go first")]
		public void Repair_is_run_from_one_place()
		{
			// Repairing while this program still holds its controllers open leaves Windows needing a
			// restart. The flow that lets go first lives once; an issue or a page that ran the repair
			// itself would skip it.
			var app = Path.Combine(Ui.RepoRoot.FullName, "App.v4");
			var direct = new[] { "Issues", "Controls" }
				.SelectMany(x => Directory.GetFiles(Path.Combine(app, x), "*.cs", SearchOption.AllDirectories))
				.Where(x => File.ReadAllText(x).Contains("AdminCommand.RepairViGEmBus"))
				.Select(Path.GetFileName)
				.ToArray();
			Assert.AreEqual(0, direct.Length,
				"These run the repair themselves instead of VirtualDriverInstaller.RepairViGEmBusElevated: "
				+ string.Join(", ", direct));
			var installer = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName,
				"App.v4", "Common", "DInput", "VirtualDriverInstaller.cs"));
			StringAssert.Contains(installer, "public static void RepairViGEmBusElevated()",
				"The shared repair flow is gone.");
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("Only a game set to virtual emulation needs the virtual driver")]
		public void Only_virtual_emulation_requires_the_driver()
		{
			// Asked the way the engine asks it (DInputHelper.WantsVirtual), so the two never disagree.
			Assert.IsTrue(VirtualDeviceDriverIssue.IsRequired(new[]
				{ new UserGame { EmulationType = (int)EmulationType.Virtual } }));
			Assert.IsFalse(VirtualDeviceDriverIssue.IsRequired(new[]
				{ new UserGame { EmulationType = (int)EmulationType.Library } }));
			Assert.IsFalse(VirtualDeviceDriverIssue.IsRequired(new[]
				{ new UserGame { EmulationType = (int)EmulationType.None } }));
			Assert.IsFalse(VirtualDeviceDriverIssue.IsRequired(new UserGame[0]));
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("No issue check connects to the bus or waits for the input thread's bus lock")]
		public void Issue_checks_never_connect_to_the_bus()
		{
			// Connecting from the issue thread made a client there and held the lock the input thread
			// takes on every pass. Installed or not is a question for the driver; whether connecting
			// failed is a question for what the input thread recorded.
			var issues = Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Issues");
			foreach (var file in Directory.GetFiles(issues, "*.cs"))
			{
				var text = File.ReadAllText(file);
				var name = Path.GetFileName(file);
				Assert.IsFalse(text.Contains("isVBusExists("), name + " connects to the bus to check it.");
				Assert.IsFalse(text.Contains("ClientLock"), name + " takes the bus client's lock.");
				Assert.IsFalse(text.Contains("new ViGEmClient("), name + " makes its own bus client.");
			}
		}
	}
}
