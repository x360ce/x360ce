// @under-test: App.v4/ViGEm/Client/ViGEmClient.x360ce.cs, App.v4/Common/DInput/DInputHelper.Step5.VirtualDevices.cs
// @area: devices   @layer: unit
using JocysCom.ClassLibrary.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nefarius.ViGEm.Client;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using x360ce.App;
using x360ce.App.DInput;
using static x360ce.Tests.BusRefusalFixtures;

namespace x360ce.Tests
{
	/// <summary>A plug, an unplug or a refused vibration is news once per change of answer, and afresh after Repair.</summary>
	/// <remarks>
	/// Plugging in, letting go and asking for vibration each keep the bus's last answer for their own
	/// controller, so the same refusal does not write a warning on every pass, and Repair forgets every
	/// record so the next fault is judged afresh.
	/// </remarks>
	[TestClass]
	public class BusRefusalOncePerChangeTest
	{
		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A refused plug or unplug is news once per change of answer or kind, and afresh once the controller is let go of or plugged in")]
		public void A_refused_plug_or_unplug_is_news_once_per_change()
		{
			var types = new Type[ViGEmClient.PlaceCount];
			var codes = new VIGEM_ERROR[ViGEmClient.PlaceCount];
			var refused = new ViGEmException(VIGEM_ERROR.VIGEM_ERROR_NO_FREE_SLOT);
			var written = 0;
			for (var i = 0; i < 10000; i++)
				if (ViGEmClient.NoteFault(types, codes, 2, refused))
					written++;
			Assert.AreEqual(1, written, "10,000 identical refusals of one controller wrote " + written + " fault reports.");
			Assert.IsTrue(ViGEmClient.NoteFault(types, codes, 2, new ViGEmException(Unnamed)), "A different answer was not written.");
			Assert.IsFalse(ViGEmClient.NoteFault(types, codes, 2, new ViGEmException(Unnamed)), "The same answer was written twice.");
			Assert.IsTrue(ViGEmClient.NoteFault(types, codes, 2, new InvalidOperationException()), "A different kind of failure was not written.");
			Assert.IsTrue(ViGEmClient.NoteFault(types, codes, 1, refused), "One controller's record held back another's fault.");
			// Let go of, or plugged in: the next failure is news.
			Assert.IsFalse(ViGEmClient.NoteFault(types, codes, 2, null), "Success was read as a fault.");
			Assert.IsTrue(ViGEmClient.NoteFault(types, codes, 2, new InvalidOperationException()),
				"A failure after the controller was let go of or plugged in was not written.");
		}

		/// <summary>Checks, from its source, that a method writes its failure only when <c>NoteFault</c> says it is news.</summary>
		static void AssertWritesOnlyNews(string[] method, string name, string note)
		{
			var at = Array.IndexOf(method, note);
			Assert.IsTrue(at >= 0, name + " does not ask whether its failure is news.");
			Assert.AreEqual("JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);", method[at + 1],
				name + " writes its failure whether or not it is news.");
			Assert.AreEqual(1, method.Count(x => x.Contains("WriteException(")), name + " writes a failure some other way too.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Plugging in and letting go each write a failure once per change, by one rule, and clear it on success")]
		public void Plug_and_unplug_write_their_failures_once_per_change()
		{
			// Plugging in and letting go need a bus, so the wiring is read from the source.
			var text = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "ViGEm", "Client", "ViGEmClient.x360ce.cs"));
			var unplug = Ui.Between(text, "public bool UnPlug(", "public bool PlugIn(")
				.Split('\n').Select(x => x.Trim()).ToArray();
			var plugIn = Ui.Between(text, "public bool PlugIn(", "public void UnplugAllControllers")
				.Split('\n').Select(x => x.Trim()).ToArray();
			AssertWritesOnlyNews(unplug, "UnPlug", "if (NoteFault(_unplugFaultTypes, _unplugFaultCodes, i, ex))");
			AssertWritesOnlyNews(plugIn, "PlugIn", "if (NoteFault(_plugFaultTypes, _plugFaultCodes, userIndex, ex))");
			CollectionAssert.Contains(unplug, "NoteFault(_unplugFaultTypes, _unplugFaultCodes, i, null);",
				"A controller let go of keeps its old failure, so a later one is not written.");
			CollectionAssert.Contains(plugIn, "NoteFault(_plugFaultTypes, _plugFaultCodes, userIndex, null);",
				"A controller plugged in keeps its old plug failure, so a later one is not written.");
			CollectionAssert.Contains(plugIn, "NoteFault(_unplugFaultTypes, _unplugFaultCodes, userIndex, null);",
				"A controller plugged in again keeps its old unplug failure, so a later one is not written.");
			// One compare with the record, whether the failure was thrown or answered.
			Assert.AreEqual(1, Ui.Count(text, "if (types[i] == type && codes[i] == code)"),
				"The once-per-change rule is written out more than once.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Repair forgets each controller's last plug and unplug failure, so the same failure after it is written again")]
		public void Repair_forgets_plug_and_unplug_failures()
		{
			// The client's records need a bus, so the wiring is read from the source. NoteFault with null clears
			// a record, as A_refused_plug_or_unplug_is_news_once_per_change shows.
			var app = Path.Combine(Ui.RepoRoot.FullName, "App.v4");
			var helper = File.ReadAllText(Path.Combine(app, "Common", "DInput", "DInputHelper.Step5.VirtualDevices.cs"));
			var client = File.ReadAllText(Path.Combine(app, "ViGEm", "Client", "ViGEmClient.x360ce.cs"));
			StringAssert.Contains(Ui.Between(helper, "void ForgetBusHealth()", "\n\t\t}"), ".ForgetFaults();",
				"Repair leaves the client's plug and unplug failures standing, so the same failure after it is not written.");
			var forget = Ui.Between(client, "public void ForgetFaults()", "\n\t\t}")
				.Split('\n').Select(x => x.Trim()).ToArray();
			Assert.IsTrue(forget.Any(x => x.StartsWith("for (") && x.Contains("<= PlaceCount")),
				"Not every controller's failures are forgotten.");
			CollectionAssert.Contains(forget, "NoteFault(_plugFaultTypes, _plugFaultCodes, pad, null);",
				"The last plug failure outlives Repair.");
			CollectionAssert.Contains(forget, "NoteFault(_unplugFaultTypes, _unplugFaultCodes, pad, null);",
				"The last unplug failure outlives Repair.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A refused vibration is kept for the Issues tab, written to the log once per change of answer, and afresh after vibration works or a Repair")]
		public void A_refused_vibration_is_written_once_per_change()
		{
			const VIGEM_ERROR other = VIGEM_ERROR.VIGEM_ERROR_CALLBACK_NOT_FOUND;
			var current = NoErrors();
			var types = new Type[ViGEmClient.PlaceCount];
			var codes = new VIGEM_ERROR[ViGEmClient.PlaceCount];
			var lines = Logged(() =>
			{
				for (var i = 0; i < 10000; i++)
					ViGEmClient.NoteRumble(current, types, codes, 2, Unnamed);
			});
			Assert.AreEqual(1, lines.Count, "10,000 plugs refused vibration alike wrote " + lines.Count + " log lines.");
			StringAssert.Contains(lines[0].Key, "Virtual controller 2");
			StringAssert.Contains(lines[0].Key, "vibration");
			StringAssert.Contains(lines[0].Key, "0xE0000015", "The log quotes the code otherwise than the Issues tab does.");
			Assert.AreEqual(EventLogEntryType.Warning, lines[0].Value);
			Assert.AreEqual(Unnamed, current[1], "The refusal is not kept, so the Issues tab cannot say it.");
			Assert.AreEqual(VIGEM_ERROR.VIGEM_ERROR_NONE, current[0], "Another controller's value changed.");
			lines = Logged(() =>
			{
				ViGEmClient.NoteRumble(current, types, codes, 2, other);
				ViGEmClient.NoteRumble(current, types, codes, 2, other);
				ViGEmClient.NoteRumble(current, types, codes, 1, other);
			});
			Assert.AreEqual(2, lines.Count, "A changed answer, and another controller's answer, are each written once: " +
				string.Join(Environment.NewLine, lines.Select(x => x.Key)));
			// Vibration that works clears both, with nothing written; the same refusal after it is news.
			lines = Logged(() => ViGEmClient.NoteRumble(current, types, codes, 2, VIGEM_ERROR.VIGEM_ERROR_NONE));
			Assert.AreEqual(0, lines.Count, "Vibration that works was written as a refusal.");
			Assert.AreEqual(VIGEM_ERROR.VIGEM_ERROR_NONE, current[1], "A controller that takes vibration is still said to have none.");
			lines = Logged(() => ViGEmClient.NoteRumble(current, types, codes, 2, other));
			Assert.AreEqual(1, lines.Count, "A refusal after vibration worked was not written.");
			// Repair forgets the log's record through NoteFault, as ForgetFaults does, so the same refusal after it is news.
			ViGEmClient.NoteFault(types, codes, 2, null);
			lines = Logged(() => ViGEmClient.NoteRumble(current, types, codes, 2, other));
			Assert.AreEqual(1, lines.Count, "A refusal after Repair was not written.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A controller the program makes again and again by itself, refused vibration alike each time, writes one warning")]
		public void Automatic_replugs_refused_alike_write_one_warning()
		{
			// Letting go needs a bus, so what it does to the vibration records is read from the source: the
			// Issues tab's value only. The log's record follows the answer, not the controller.
			var text = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "ViGEm", "Client", "ViGEmClient.x360ce.cs"));
			var unplug = Ui.Between(text, "public bool UnPlug(", "\n\t\t}")
				.Split('\n').Select(x => x.Trim()).Where(x => !x.StartsWith("//")).ToArray();
			var plugIn = Ui.Between(text, "public bool PlugIn(", "\n\t\t}")
				.Split('\n').Select(x => x.Trim()).Where(x => !x.StartsWith("//")).ToArray();
			var failed = plugIn.Skip(Array.IndexOf(plugIn, "catch (Exception ex)")).ToArray();
			CollectionAssert.Contains(unplug, "RumbleErrors[i - 1] = VIGEM_ERROR.VIGEM_ERROR_NONE;",
				"A controller let go of is still said to have no vibration.");
			CollectionAssert.Contains(failed, "RumbleErrors[userIndex - 1] = VIGEM_ERROR.VIGEM_ERROR_NONE;",
				"A plug that failed leaves the last controller's missing vibration standing, for a controller that is not there.");
			Assert.IsFalse(unplug.Any(x => x.Contains("_rumbleFault")),
				"Letting go forgets the logged refusal, so each automatic re-plug writes it again.");
			Assert.IsFalse(failed.Any(x => x.Contains("_rumbleFault")),
				"A failed plug forgets the logged refusal, so the next plug writes it again.");
			// Windows never building the controller, or the bus refusing its reports, has it made again every few
			// seconds: each time a plug refused alike, then a let-go as above.
			var current = NoErrors();
			var types = new Type[ViGEmClient.PlaceCount];
			var codes = new VIGEM_ERROR[ViGEmClient.PlaceCount];
			var shown = 0;
			var lines = Logged(() =>
			{
				for (var i = 0; i < 1000; i++)
				{
					ViGEmClient.NoteRumble(current, types, codes, 2, Unnamed);
					if (current[1] == Unnamed)
						shown++;
					current[1] = VIGEM_ERROR.VIGEM_ERROR_NONE;
				}
			});
			Assert.AreEqual(1000, shown, "A controller made without vibration was not shown so in the Issues tab.");
			Assert.AreEqual(1, lines.Count, "1,000 automatic re-plugs refused alike wrote " + lines.Count + " warnings.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A controller is remembered as ours straight after it is made, its vibration answer is kept, and Repair forgets both vibration records")]
		public void The_vibration_record_follows_the_controller()
		{
			// Plugging in and Repair need a bus, so the wiring is read from the source.
			var text = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "ViGEm", "Client", "ViGEmClient.x360ce.cs"));
			var plugIn = Ui.Between(text, "public bool PlugIn(", "public void UnplugAllControllers")
				.Split('\n').Select(x => x.Trim()).ToArray();
			var forget = Ui.Between(text, "public void ForgetFaults()", "\n\t\t}")
				.Split('\n').Select(x => x.Trim()).ToArray();
			var connect = Array.IndexOf(plugIn, "t[userIndex - 1].Connect();");
			Assert.IsTrue(connect >= 0, "PlugIn no longer connects the pad asked for.");
			Assert.AreEqual("RememberSerial(t[userIndex - 1]);", plugIn[connect + 1],
				"A controller on the bus is not remembered as ours straight after it is made, so it can read as Leftover.");
			var made = Array.IndexOf(plugIn,
				"NoteRumble(RumbleErrors, _rumbleFaultTypes, _rumbleFaultCodes, userIndex, t[userIndex - 1].RumbleError);");
			var failed = Array.IndexOf(plugIn, "catch (Exception ex)");
			Assert.IsTrue(made > connect && failed > made,
				"A controller made without vibration is not recorded, so the Issues tab cannot say it.");
			CollectionAssert.Contains(forget, "NoteFault(_rumbleFaultTypes, _rumbleFaultCodes, pad, null);",
				"The logged refusal outlives Repair, and the same refusal after it is not written.");
			CollectionAssert.Contains(forget, "RumbleErrors[pad - 1] = VIGEM_ERROR.VIGEM_ERROR_NONE;",
				"The missing vibration outlives Repair in the Issues tab.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A controller the bus will not let go of is asked again after the plug wait, not on every pass")]
		public void A_controller_the_bus_keeps_waits_for_the_plug_gate()
		{
			// Letting go needs a bus, so the source of the update is read instead.
			var text = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName,
				"App.v4", "Common", "DInput", "DInputHelper.Step5.VirtualDevices.cs"));
			var start = text.IndexOf("if (!FeedDevice(i))");
			Assert.IsTrue(start >= 0, "The feed drop is no longer where this test looks for it.");
			var end = text.IndexOf("\n\t\t}", start);
			var split = text.IndexOf("\n\t\t\t\telse", start);
			Assert.IsTrue(start < split && split < end, "The branch for a pad switched off was not found.");
			var drop = text.Substring(start, split - start);
			var off = text.Substring(split, end - split);
			// A feed drop whose unplug fails: asked again at once, the controller is found attached and fed straight back.
			StringAssert.Contains(drop, "if (!client.UnPlug(i))", "A feed drop ignores whether the controller was let go of.");
			StringAssert.Contains(drop, "_NextPlugAttempt[i - 1] = unchecked(now + PlugRetryMs);",
				"A controller the bus kept after a feed drop is plugged and refused again every few passes.");
			// A pad switched off whose controller the bus keeps: tried on every pass, with an exception and a fault report each time.
			var wait = off.IndexOf("IsWaiting(_NextPlugAttempt[i - 1], Environment.TickCount, PlugRetryMs)");
			var disable = off.IndexOf("DisableFeeding(i)");
			Assert.IsTrue(disable > 0, "A pad switched off no longer lets go of its controller.");
			Assert.IsTrue(wait >= 0 && wait < disable, "A pad switched off lets go without waiting for the plug gate.");
			StringAssert.Contains(off.Substring(disable), "_NextPlugAttempt[i - 1] = unchecked(Environment.TickCount + PlugRetryMs);",
				"A controller the bus kept after its pad was switched off is asked again on every pass.");
		}
	}
}
