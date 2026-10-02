// @under-test: App.v4/Issues/VirtualDriverNotWorkingIssue.cs, App.v4/ViGEm/Client/ViGEmClient.x360ce.cs
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
	/// <summary>A virtual driver that is installed and does not work is named in the Issues tab, with the reason.</summary>
	/// <remarks>
	/// <c>VirtualDriverNotWorkingIssue.Judge</c> turns the recorded bus health into a severity and a message:
	/// a controller that fails is named by number and by the bus's own answer, a controller that only lost
	/// its vibration is said once and softly, and an answer more than 24.9 days old, where the tick count's
	/// difference has turned negative, is read as old rather than as current.
	/// </remarks>
	[TestClass]
	public class VirtualBusHealthIssueTest
	{
		/// <summary>Everything wanted and nothing failed.</summary>
		static VirtualDriverNotWorkingIssue.Health Healthy()
		{
			var health = new VirtualDriverNotWorkingIssue.Health { BusInstalled = true, Now = Now };
			for (var i = 0; i < health.Pads.Length; i++)
				health.Pads[i] = new VirtualDriverNotWorkingIssue.PadHealth
				{
					Wanted = true,
					LastResult = VirtualError.None,
					LastError = VIGEM_ERROR.VIGEM_ERROR_NONE,
					RumbleError = VIGEM_ERROR.VIGEM_ERROR_NONE,
				};
			return health;
		}

		static IssueSeverity Judge(VirtualDriverNotWorkingIssue.Health health, out string message)
		{
			return VirtualDriverNotWorkingIssue.Judge(health, out message);
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("A bus that works raises nothing")]
		public void A_working_bus_raises_nothing()
		{
			string message;
			Assert.AreEqual(IssueSeverity.None, Judge(Healthy(), out message));
			Assert.IsNull(message);
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("A refused connection to an installed bus is reported with the driver's answer")]
		public void A_refused_connection_is_reported_with_the_answer()
		{
			var health = Healthy();
			health.ConnectError = VIGEM_ERROR.VIGEM_ERROR_BUS_ACCESS_FAILED;
			health.ConnectTick = Now - 1000;
			string message;
			Assert.AreEqual(IssueSeverity.Moderate, Judge(health, out message),
				"Moderate, so the checks after this one still run.");
			StringAssert.Contains(message, "BUS_ACCESS_FAILED");
			StringAssert.Contains(message, "Repair");
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("A refusal nothing has asked about since, and a driver that is not there, are not this issue")]
		public void Stale_or_uninstalled_connect_failures_are_not_reported()
		{
			var health = Healthy();
			health.ConnectError = VIGEM_ERROR.VIGEM_ERROR_BUS_NOT_FOUND;
			health.ConnectTick = Now - VirtualDriverNotWorkingIssue.ConnectErrorFreshMs;
			string message;
			Assert.AreEqual(IssueSeverity.None, Judge(health, out message),
				"Three retries without a new answer means nothing is asking; it no longer describes the bus.");
			health.ConnectTick = Now - 1000;
			health.BusInstalled = false;
			Assert.AreEqual(IssueSeverity.None, Judge(health, out message),
				"A driver that is not there is the Virtual Device Driver issue, which offers to install it.");
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("A controller that keeps failing to plug into a free place is reported by number")]
		public void Repeated_plug_failures_in_a_free_place_are_reported()
		{
			var health = Healthy();
			health.Pads[1].LastResult = VirtualError.Other;
			health.Pads[1].LastError = VIGEM_ERROR.VIGEM_ERROR_NO_FREE_SLOT;
			health.Pads[1].PlugFailures = VirtualDriverNotWorkingIssue.PlugFailuresToReport - 1;
			string message;
			Assert.AreEqual(IssueSeverity.None, Judge(health, out message),
				"Two failures can still be one passing moment straddling a retry.");
			health.Pads[1].PlugFailures = VirtualDriverNotWorkingIssue.PlugFailuresToReport;
			Assert.AreEqual(IssueSeverity.Moderate, Judge(health, out message));
			StringAssert.Contains(message, "Controller 2");
			StringAssert.Contains(message, "NO_FREE_SLOT");
			health.Pads[1].Wanted = false;
			Assert.AreEqual(IssueSeverity.None, Judge(health, out message),
				"A tab not set to have a virtual controller is not the bus's fault.");
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("A controller the bus accepted and Windows never built is said as such")]
		public void A_controller_Windows_never_builds_is_said_as_such()
		{
			var health = Healthy();
			health.Pads[0].LastResult = VirtualError.PlaceNotGiven;
			health.Pads[0].PlugFailures = VirtualDriverNotWorkingIssue.PlugFailuresToReport;
			string message;
			Assert.AreEqual(IssueSeverity.Moderate, Judge(health, out message));
			StringAssert.Contains(message, "Controller 1");
			StringAssert.Contains(message, "never finished building");
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("Reports the bus keeps refusing are reported while they keep coming")]
		public void Repeated_feed_drops_are_reported_while_they_keep_coming()
		{
			var health = Healthy();
			health.Pads[3].FeedDrops = VirtualDriverNotWorkingIssue.FeedDropsToReport;
			health.Pads[3].LastFeedDropTick = Now - 1000;
			health.Pads[3].LastError = VIGEM_ERROR.VIGEM_ERROR_INVALID_TARGET;
			string message;
			Assert.AreEqual(IssueSeverity.Moderate, Judge(health, out message));
			StringAssert.Contains(message, "Controller 4");
			StringAssert.Contains(message, "INVALID_TARGET");
			health.Pads[3].LastFeedDropTick = Now - DInputHelper.FeedDropWindowMs;
			Assert.AreEqual(IssueSeverity.None, Judge(health, out message),
				"The drops have stopped; the run is over and so is the fault.");
			health.Pads[3].LastFeedDropTick = Now - 1000;
			health.Pads[3].FeedDrops = VirtualDriverNotWorkingIssue.FeedDropsToReport - 1;
			Assert.AreEqual(IssueSeverity.None, Judge(health, out message),
				"One drop is a driver update; two can be one update straddling a replug.");
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("A fresh failure stays fresh across the tick count's wrap")]
		public void A_fresh_failure_stays_fresh_across_the_wrap()
		{
			var health = Healthy();
			health.Now = int.MinValue + 500;
			health.Pads[0].FeedDrops = VirtualDriverNotWorkingIssue.FeedDropsToReport;
			health.Pads[0].LastFeedDropTick = int.MaxValue - 499;
			string message;
			Assert.AreEqual(IssueSeverity.Moderate, Judge(health, out message),
				"A drop a second before the wrap was read as one from 49 days ago.");
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("An answer this program has no name for is quoted by number")]
		public void An_unnamed_answer_is_quoted_by_number()
		{
			var health = Healthy();
			health.ConnectError = (VIGEM_ERROR)0xE0000015;
			health.ConnectTick = Now - 1000;
			string message;
			Assert.AreEqual(IssueSeverity.Moderate, Judge(health, out message));
			StringAssert.Contains(message, "0xE0000015",
				"Newer bus libraries answer with codes this enum does not list; the number is what support can look up.");
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("Reports refused with an answer this program has no name for are reported with its number")]
		public void Reports_refused_with_an_unnamed_answer_are_quoted_by_number()
		{
			var health = Healthy();
			health.Pads[1].FeedDrops = VirtualDriverNotWorkingIssue.FeedDropsToReport;
			health.Pads[1].LastFeedDropTick = Now - 1000;
			health.Pads[1].LastError = (VIGEM_ERROR)0xE0000015;
			string message;
			Assert.AreEqual(IssueSeverity.Moderate, Judge(health, out message));
			StringAssert.Contains(message, "Controller 2");
			StringAssert.Contains(message, "0xE0000015",
				"A report refused with a code the enum does not name is recorded as it came; the number is what support can look up.");
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("A controller whose vibration the driver refused is said once and softly: it works, the driver refused its vibration, what the driver answered, and that Repair may restore it")]
		public void A_controller_without_vibration_is_said_once_and_softly()
		{
			var health = Healthy();
			health.Pads[1].RumbleError = VIGEM_ERROR.VIGEM_ERROR_CALLBACK_NOT_FOUND;
			string message;
			Assert.AreEqual(IssueSeverity.Low, Judge(health, out message),
				"The controller works and only its vibration is missing, so it is no driver that does not work.");
			StringAssert.Contains(message, "Controller 2 works, but the driver refused its vibration");
			StringAssert.Contains(message, "CALLBACK_NOT_FOUND");
			Assert.AreEqual(1, Ui.Count(message, "Controller 2"), "Controller 2 is named more than once: " + message);
			Assert.AreEqual(1, Ui.Count(message, "vibration"), "The missing vibration is said more than once: " + message);
			Assert.AreEqual(1, Ui.Count(message, "may restore"), "What Repair may restore is not said once: " + message);
			// Two controllers without vibration: each is named, and what Repair may restore is still said once.
			health.Pads[2].RumbleError = VIGEM_ERROR.VIGEM_ERROR_CALLBACK_NOT_FOUND;
			Assert.AreEqual(IssueSeverity.Low, Judge(health, out message));
			StringAssert.Contains(message, "Controller 3 works");
			Assert.AreEqual(1, Ui.Count(message, "may restore"), "What Repair may restore is said for each controller: " + message);
			health.Pads[1].RumbleError = (VIGEM_ERROR)0xE0000015;
			Judge(health, out message);
			StringAssert.Contains(message, "0xE0000015", "An answer the enum does not name is quoted otherwise than everywhere else.");
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("The row's title follows the worst it says: refused vibration alone is named as such, and a bus fault keeps the driver's title and weight")]
		public void The_title_follows_the_worst_it_says()
		{
			var health = Healthy();
			health.Pads[1].RumbleError = VIGEM_ERROR.VIGEM_ERROR_CALLBACK_NOT_FOUND;
			string message;
			var severity = Judge(health, out message);
			Assert.AreEqual(IssueSeverity.Low, severity);
			Assert.AreEqual("Virtual controller has no vibration", VirtualDriverNotWorkingIssue.Title(severity),
				"A row whose only line says the controller works is titled as a driver that does not work.");
			health.ConnectError = VIGEM_ERROR.VIGEM_ERROR_BUS_ACCESS_FAILED;
			health.ConnectTick = Now - 1000;
			severity = Judge(health, out message);
			Assert.AreEqual(IssueSeverity.Moderate, severity, "A bus fault beside refused vibration lost its weight.");
			Assert.AreEqual("Virtual driver is installed but not working", VirtualDriverNotWorkingIssue.Title(severity),
				"A bus fault beside refused vibration lost the driver's title.");
			StringAssert.Contains(message, "Controller 2 works", "The refused vibration is no longer said beside the bus fault.");
			// The check needs the program's settings, so the renaming is read from the source.
			var issue = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Issues", "VirtualDriverNotWorkingIssue.cs"));
			StringAssert.Contains(issue, "Name = Title(IssueSeverity.Moderate);", "The row starts with a title of its own.");
			StringAssert.Contains(issue, "var name = Title(severity);", "The check does not title the row by what it found.");
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("A controller is said to work without vibration only while it is made, wanted and not failing otherwise")]
		public void Vibration_is_said_only_of_a_controller_that_works()
		{
			var health = Healthy();
			health.Pads[1].RumbleError = VIGEM_ERROR.VIGEM_ERROR_CALLBACK_NOT_FOUND;
			string message;
			health.Pads[1].LastResult = VirtualError.PlaceTaken;
			Assert.AreEqual(IssueSeverity.None, Judge(health, out message), "A controller that was not made was said to work.");
			health.Pads[1].LastResult = VirtualError.None;
			health.Pads[1].Wanted = false;
			Assert.AreEqual(IssueSeverity.None, Judge(health, out message), "A tab not set to have a virtual controller was said to have one.");
			// The driver refusing its input is the fault to fix; the controller is not said to work beside it.
			health.Pads[1].Wanted = true;
			health.Pads[1].FeedDrops = VirtualDriverNotWorkingIssue.FeedDropsToReport;
			health.Pads[1].LastFeedDropTick = Now - 1000;
			Assert.AreEqual(IssueSeverity.Moderate, Judge(health, out message));
			Assert.AreEqual(1, Ui.Count(message, "Controller 2"), "Controller 2 is said to work and to fail: " + message);
			Assert.IsFalse(message.Contains("vibration"), "A controller whose input the driver keeps refusing was said to work.");
			// Another controller failing weighs the issue up, and this one's missing vibration is still said.
			health.Pads[1].FeedDrops = 0;
			health.Pads[3].LastResult = VirtualError.Other;
			health.Pads[3].PlugFailures = VirtualDriverNotWorkingIssue.PlugFailuresToReport;
			Assert.AreEqual(IssueSeverity.Moderate, Judge(health, out message));
			StringAssert.Contains(message, "Controller 2 works");
			StringAssert.Contains(message, "Controller 4");
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("The Issues tab reads a controller's missing vibration from the bus client's record, after the plug's result")]
		public void Missing_vibration_is_read_after_the_plug_result()
		{
			// Reading needs a running input thread and bus client, so the order is read from the source.
			var issue = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Issues", "VirtualDriverNotWorkingIssue.cs"));
			var result = issue.IndexOf("LastResult = helper.VirtualErrors[i],");
			var rumble = issue.IndexOf("RumbleError = rumble == null ? VIGEM_ERROR.VIGEM_ERROR_NONE : rumble[i],");
			Assert.IsTrue(result >= 0, "The plug's result is no longer read where this test looks for it.");
			Assert.IsTrue(rumble > result,
				"The vibration answer is read before the plug's result, so a controller just made can be read with the answer from before it.");
			StringAssert.Contains(issue, ".RumbleErrors;", "The Issues tab does not read the bus client's record of missing vibration.");
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("An answer more than 24.9 days old is old, not just now")]
		public void An_answer_older_than_the_tick_range_reads_as_old()
		{
			// The difference of two tick counts turns negative after 2^31 ms, and a negative age is below
			// every limit, so without a floor an answer from weeks ago read as one from this moment.
			Assert.AreEqual(1, DInputHelper.NextFeedDrops(4, LongAgo, Now),
				"A drop 25 days after the last one continued the old run.");
			var health = Healthy();
			health.ConnectError = VIGEM_ERROR.VIGEM_ERROR_BUS_ACCESS_FAILED;
			health.ConnectTick = LongAgo;
			string message;
			Assert.AreEqual(IssueSeverity.None, Judge(health, out message),
				"A refused connection 25 days old was reported as current.");
			health = Healthy();
			health.Pads[0].FeedDrops = VirtualDriverNotWorkingIssue.FeedDropsToReport;
			health.Pads[0].LastFeedDropTick = LongAgo;
			Assert.AreEqual(IssueSeverity.None, Judge(health, out message),
				"A run of refused reports that ended 25 days ago was reported as current.");
			// The bus client's retry gate needs a bus to run, so its source is read instead.
			var client = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName,
				"App.v4", "ViGEm", "Client", "ViGEmClient.x360ce.cs"));
			Assert.IsTrue(client.Contains("DInputHelper.IsRecent(_LastConnectTick, Environment.TickCount, ConnectRetryMs)"),
				"A refusal 25 days old stops the bus client connecting for the next 25 days.");
		}

		/// <summary>What the Issues tab reads from a helper whose Controller 1 Windows never built, with that many plugs counted.</summary>
		/// <param name="heldBack">Whether the pad is held back until a controller comes or goes.</param>
		static VirtualDriverNotWorkingIssue.Health ReadNeverBuilt(int plugFailures, bool heldBack)
		{
			var helper = new DInputHelper();
			helper.VirtualErrors[0] = VirtualError.PlaceNotGiven;
			helper.PlugFailures[0] = plugFailures;
			var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
			((int[])typeof(DInputHelper).GetField("_misplacedWith", flags).GetValue(helper))[0] = heldBack ? 0 : -1;
			((VirtualError[])typeof(DInputHelper).GetField("_heldAs", flags).GetValue(helper))[0] = VirtualError.PlaceNotGiven;
			return ReadHealth(helper);
		}

		/// <summary>What the Issues tab reads while <paramref name="helper"/> is the program's, with Controller 1 wanted and the driver installed.</summary>
		internal static VirtualDriverNotWorkingIssue.Health ReadHealth(DInputHelper helper)
		{
			var game = new UserGame
			{
				FileName = "never-built.exe",
				EmulationType = (int)EmulationType.Virtual,
				EnableMask = (int)MapToMask.Controller1,
			};
			var oldHelper = Global.DHelper;
			var oldGame = SettingsManager.CurrentGame;
			var xinputEnabled = SettingsManager.Options.XInputEnabled;
			try
			{
				Global.DHelper = helper;
				SettingsManager.UpdateCurrentGame(game);
				SettingsManager.Options.XInputEnabled = true;
				var health = (VirtualDriverNotWorkingIssue.Health)typeof(VirtualDriverNotWorkingIssue)
					.GetMethod("Read", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, null);
				// Whether this machine has the driver is not what is being tested.
				health.BusInstalled = true;
				health.ConnectError = VIGEM_ERROR.VIGEM_ERROR_NONE;
				return health;
			}
			finally
			{
				// The game first: changing it tells the helper in place.
				SettingsManager.UpdateCurrentGame(oldGame);
				Global.DHelper = oldHelper;
				SettingsManager.Options.XInputEnabled = xinputEnabled;
			}
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("A controller Windows never built is reported at once while it is held back, since it is not tried again; one that is tried again is reported after three")]
		public void A_controller_Windows_never_built_is_reported_once_while_held_back()
		{
			string message;
			Assert.AreEqual(IssueSeverity.Moderate, Judge(ReadNeverBuilt(1, true), out message),
				"A controller Windows never built is held back and never tried again, and the Issues tab says nothing.");
			StringAssert.Contains(message, "Controller 1: the driver accepted its virtual controller, but Windows never finished building it.");
			Assert.AreEqual(IssueSeverity.None, Judge(ReadNeverBuilt(1, false), out message),
				"One failure of a controller that is tried again is reported before it can be a passing moment.");
			Assert.AreEqual(IssueSeverity.Moderate, Judge(ReadNeverBuilt(3, true), out message));
			StringAssert.Contains(message, "accepted its virtual controller 3 times in a row");
		}
	}
}
