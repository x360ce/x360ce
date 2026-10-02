// @under-test: App.v4/ViGEm/Client/ViGEmClient.x360ce.cs, App.v4/Common/DInput/DInputHelper.Step5.VirtualDevices.cs, App.v4/Common/DInput/XInputPlaces.cs, App.v4/Common/DInput/VirtualError.cs, App.v4/MainForm.cs, App.v4/Issues/VirtualDriverNotWorkingIssue.cs
// @area: devices   @layer: unit
using JocysCom.ClassLibrary.Controls.IssuesControl;
using JocysCom.ClassLibrary.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using x360ce.App;
using x360ce.App.DInput;
using x360ce.App.Issues;

namespace x360ce.Tests
{
	/// <summary>A virtual controller the bus would not let go of is made again, once per pad, not fed and refused for as long as the game runs.</summary>
	/// <remarks>
	/// The bus library keeps a controller attached until a removal works. Found attached after a refused removal, it was
	/// fed, its report refused and its removal refused again every two seconds until Repair. Plugging in needs a bus, so
	/// the plug worker's order is read from the source; the bus client runs on objects made without their constructors,
	/// with controllers that answer as each test sets and nothing native behind them.
	/// </remarks>
	[TestClass]
	public class RefusedRemovalTest
	{
		/// <summary>What the bus answers a removal it will not do.</summary>
		const VIGEM_ERROR Refused = VIGEM_ERROR.VIGEM_ERROR_REMOVAL_FAILED;

		/// <summary>A controller with nothing native behind it, whose removal the bus answers as the test sets.</summary>
		internal class FakePad : Xbox360Controller
		{
			FakePad(ViGEmClient client) : base(client) { }

			/// <summary>What the bus answers its removal: VIGEM_ERROR_NONE when it goes.</summary>
			public VIGEM_ERROR RemovalAnswer;
			/// <summary>Whether the bus holds it.</summary>
			public bool Attached;
			/// <summary>How many times it was put on the bus.</summary>
			public int ConnectsAsked;
			/// <summary>How many times it was asked to go.</summary>
			public int RemovalsAsked;

			public static FakePad Make(VIGEM_ERROR removalAnswer)
			{
				var pad = (FakePad)FormatterServices.GetUninitializedObject(typeof(FakePad));
				// Nothing native is behind it, so nothing is freed when it is collected.
				GC.SuppressFinalize(pad);
				typeof(Xbox360Controller).GetProperty("RumbleError").SetValue(pad, VIGEM_ERROR.VIGEM_ERROR_NONE);
				pad.RemovalAnswer = removalAnswer;
				return pad;
			}

			public override void Connect()
			{
				ConnectsAsked++;
				Attached = true;
			}

			public override void Disconnect()
			{
				RemovalsAsked++;
				if (RemovalAnswer != VIGEM_ERROR.VIGEM_ERROR_NONE)
					throw new ViGEmException(RemovalAnswer);
				Attached = false;
			}
		}

		/// <summary>A bus client with nothing native behind it, with the arrays its constructor reserves.</summary>
		internal static ViGEmClient Client()
		{
			var client = (ViGEmClient)FormatterServices.GetUninitializedObject(typeof(ViGEmClient));
			GC.SuppressFinalize(client);
			foreach (var field in typeof(ViGEmClient).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				if (field.FieldType.IsArray && field.GetValue(client) == null)
					field.SetValue(client, Array.CreateInstance(field.FieldType.GetElementType(), ViGEmClient.PlaceCount));
			return client;
		}

		/// <summary>The controller each pad keeps for the bus, by pad index.</summary>
		static Xbox360Controller[] Kept(ViGEmClient client)
		{
			return (Xbox360Controller[])typeof(ViGEmClient).GetField("_keptTargets", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(client);
		}

		/// <summary>Runs the action and returns every fault it wrote to the log, which goes nowhere else meanwhile.</summary>
		internal static List<Exception> Faults(Action action)
		{
			var faults = new List<Exception>();
			EventHandler<LogHelperEventArgs> keep = (sender, e) =>
			{
				faults.Add(e.Exception);
				e.Cancel = true;
			};
			var log = LogHelper.Current;
			log.WritingException += keep;
			try
			{
				action();
			}
			finally
			{
				log.WritingException -= keep;
			}
			return faults;
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A new controller takes the slot of one the bus would not let go of, and the old one is kept, not freed, one per pad")]
		public void A_new_controller_takes_the_slot_and_the_old_one_is_kept()
		{
			var client = Client();
			var old = FakePad.Make(Refused);
			var fresh = FakePad.Make(Refused);
			client.Targets[1] = old;
			Assert.IsFalse(client.HasKept(2), "A pad keeps a controller before the bus refused to let one go.");
			client.Replace(2, fresh);
			Assert.AreSame(fresh, client.Targets[1], "The new controller is not in the pad's slot.");
			Assert.IsTrue(client.HasKept(2), "The pad does not say it keeps a controller, so another would be made.");
			Assert.AreSame(old, Kept(client)[1], "The controller the bus holds was dropped, and its vibration handler with it.");
			Assert.IsNull(client.Targets[0], "Another pad's slot changed.");
			Assert.IsFalse(client.HasKept(1) || client.HasKept(3) || client.HasKept(4), "Another pad keeps a controller.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A controller found attached after a refused removal is replaced once per pad on the plug worker; one that is simply attached is kept")]
		public void A_controller_the_bus_would_not_let_go_of_is_made_again()
		{
			var app = Path.Combine(Ui.RepoRoot.FullName, "App.v4");
			var step5 = File.ReadAllText(Path.Combine(app, "Common", "DInput", "DInputHelper.Step5.VirtualDevices.cs"));
			var client = File.ReadAllText(Path.Combine(app, "ViGEm", "Client", "ViGEmClient.x360ce.cs"));
			var enable = Ui.Between(step5, "public VirtualError EnableFeeding(", "public VirtualError DisableFeeding(");
			var attached = enable.IndexOf("if (client.IsControllerConnected(userIndex))");
			var asked = enable.IndexOf("if (!client.RemovalRefused(userIndex))");
			var kept = enable.IndexOf("return VirtualError.None;");
			var once = enable.IndexOf("if (client.HasKept(userIndex))");
			var stopped = enable.IndexOf("return VirtualError.RemovalRefused;");
			var replaced = enable.IndexOf("client.Replace(userIndex, NewTarget(client));");
			var taken = enable.IndexOf("return client.HasKept(userIndex) ? VirtualError.RemovalRefused : VirtualError.PlaceTaken;");
			var plugged = enable.IndexOf("client.PlugIn(userIndex, out plugError)");
			Assert.IsTrue(attached > 0 && asked > attached && kept > asked && replaced > kept && plugged > replaced,
				"A controller the bus would not let go of is found attached and kept, so it is fed and refused until Repair.");
			Assert.IsTrue(once > kept && stopped > once && replaced > stopped,
				"A pad that already keeps a controller for the bus makes another each time the bus refuses to let one go.");
			Assert.IsTrue(taken > replaced && plugged > taken,
				"A pad whose place its kept controller holds is said to be blocked by another controller.");
			// One place makes a controller and hooks its vibration up: the first four and every one made again.
			Assert.AreEqual(1, Ui.Count(step5, "FeedbackReceived += Controller_FeedbackReceived"),
				"Controllers are made in more than one way, and one made again can miss the game's vibration.");
			StringAssert.Contains(step5, "client.Targets[i] = NewTarget(client);");
			Assert.AreEqual(1, Ui.Count(step5, "client.Replace("), "A controller is made again outside the plug worker.");
			Assert.IsFalse(Ui.Between(client, "public void Replace(", "\n\t\t}").Contains("Dispose("),
				"The controller the bus holds is freed while the bus library can still call its vibration handler.");
			StringAssert.Contains(Ui.Between(client, "public bool RemovalRefused(", "\n\t\t}"), "_unplugFaultTypes[userIndex - 1] != null");
		}

		/// <summary>One gated attempt at plugging in pad 2 on a bus that takes every controller and lets none go, in the plug worker's order.</summary>
		/// <param name="client">The bus client.</param>
		/// <param name="places">The XInput places taken. The place a controller is given is marked.</param>
		/// <param name="givesPlace">Whether Windows gives a new controller a place: another tab's, the first free one.</param>
		/// <param name="made">Every controller made in the pad's slot in place of one the bus would not let go of.</param>
		/// <remarks>
		/// The order is pinned from the source by <see cref="A_controller_the_bus_would_not_let_go_of_is_made_again"/>. A
		/// pad put in the wrong place or given none is held back only while the places are as they were and the bus let its
		/// controller go. Here the bus lets none go, so that hold-back never stops these attempts and is left out; see
		/// <see cref="MisplacedPlugTest.A_pad_whose_controller_the_bus_kept_is_not_held_back"/>.
		/// </remarks>
		static VirtualError Gate(ViGEmClient client, bool[] places, bool givesPlace, List<FakePad> made)
		{
			const uint pad = 2;
			const int own = 1;
			if (((FakePad)client.Targets[own]).Attached)
			{
				if (!client.RemovalRefused(pad))
					return VirtualError.None;
				if (client.HasKept(pad))
					return VirtualError.RemovalRefused;
				var fresh = FakePad.Make(Refused);
				made.Add(fresh);
				client.Replace(pad, fresh);
			}
			if (places[own])
				return client.HasKept(pad) ? VirtualError.RemovalRefused : VirtualError.PlaceTaken;
			VIGEM_ERROR error;
			Assert.IsTrue(client.PlugIn(pad, out error), "The bus refused a plug it takes.");
			var given = -1;
			for (var i = 0; givesPlace && given < 0 && i < places.Length; i++)
				if (i != own && !places[i])
					given = i;
			if (given >= 0)
				places[given] = true;
			// Not in its own place, so it is asked to go, and the bus refuses.
			Assert.IsFalse(client.UnPlug(pad), "The bus let go of a controller it keeps.");
			return given < 0 ? VirtualError.PlaceNotGiven : VirtualError.PlaceWrong;
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A bus that lets no controller go gets one more controller per pad, not one per attempt, and its refusal is written once")]
		public void A_bus_that_lets_nothing_go_gets_one_more_controller_per_pad()
		{
			// Windows never builds the new controller, or puts it in another tab's place. Either way it is asked to go at
			// once and the bus refuses, on every one of a hundred gated attempts: over three minutes of play.
			foreach (var givesPlace in new[] { false, true })
			{
				var client = Client();
				var first = FakePad.Make(Refused);
				client.Targets[1] = first;
				var places = new bool[4];
				var made = new List<FakePad>();
				var result = VirtualError.None;
				var faults = Faults(() =>
				{
					for (var gate = 0; gate < 100; gate++)
						result = Gate(client, places, givesPlace, made);
				});
				var name = givesPlace ? "Given another tab's place: " : "Never given a place: ";
				Assert.AreEqual(1, made.Count, name + "a pad made " + made.Count + " controllers the bus holds.");
				Assert.AreSame(first, Kept(client)[1], name + "the first controller the bus holds was not kept.");
				Assert.AreEqual(1, faults.Count, name + "the same refusal was written " + faults.Count + " times.");
				Assert.AreEqual(VirtualError.RemovalRefused, result, name + "the pad does not say the driver would not remove its controller.");
				Assert.AreEqual(givesPlace ? 2 : 0, places.Count(x => x), name + "other tabs' places taken by controllers nobody feeds.");
			}
			// Fed in its own place, and its report refused: the pass asks it to go, and the bus refuses.
			var held = Client();
			var own = FakePad.Make(Refused);
			held.Targets[1] = own;
			var taken = new bool[] { false, true, false, false };
			var replacements = new List<FakePad>();
			var last = VirtualError.None;
			var written = Faults(() =>
			{
				VIGEM_ERROR error;
				held.PlugIn(2, out error);
				held.UnPlug(2);
				for (var gate = 0; gate < 100; gate++)
					last = Gate(held, taken, true, replacements);
			});
			Assert.AreEqual(1, replacements.Count, "A pad whose place its kept controller holds made " + replacements.Count + " controllers.");
			Assert.IsFalse(replacements[0].Attached, "A controller was plugged in while the kept one holds its place.");
			Assert.AreEqual(1, written.Count, "The same refusal was written " + written.Count + " times.");
			Assert.AreEqual(VirtualError.RemovalRefused, last, "The pad is said to be blocked by another controller.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Repair, Auto-Order and closing ask each kept controller to go once, and forget each one the bus lets go of")]
		public void Letting_go_of_every_controller_asks_the_kept_ones_too()
		{
			var client = Client();
			for (var i = 0; i < 4; i++)
				client.Targets[i] = FakePad.Make(VIGEM_ERROR.VIGEM_ERROR_NONE);
			var goes = FakePad.Make(VIGEM_ERROR.VIGEM_ERROR_NONE);
			var gone = FakePad.Make(VIGEM_ERROR.VIGEM_ERROR_TARGET_NOT_PLUGGED_IN);
			var stays = FakePad.Make(Refused);
			var kept = Kept(client);
			kept[0] = goes;
			kept[1] = gone;
			kept[2] = stays;
			// Repair, Remove and Auto-Order let go of everything through the same step.
			var helper = new DInputHelper();
			var current = ViGEmClient.Current;
			ViGEmClient.Current = client;
			try
			{
				Faults(() => Assert.IsTrue(helper.ReleaseForDeviceRemoval(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5)), "XInput did not let go."));
				helper.ResumeAfterDeviceRemoval();
			}
			finally
			{
				ViGEmClient.Current = current;
			}
			Assert.AreEqual(1, goes.RemovalsAsked, "A kept controller was not asked to go.");
			Assert.AreEqual(1, gone.RemovalsAsked, "A kept controller already gone was not asked to go.");
			Assert.AreEqual(1, stays.RemovalsAsked, "A kept controller the bus holds was not asked again.");
			Assert.IsFalse(client.HasKept(1), "A controller the bus let go of is still kept.");
			Assert.IsFalse(client.HasKept(2), "A controller already gone is still kept.");
			Assert.IsTrue(client.HasKept(3), "A controller the bus still holds was dropped.");
			// Closing lets go of the slots, then asks again for each controller still kept.
			Faults(client.UnplugAllControllers);
			Assert.AreEqual(2, stays.RemovalsAsked, "Closing did not ask again for the controller the bus still holds.");
			Assert.AreEqual(1, goes.RemovalsAsked, "A controller already let go of was asked again.");
			stays.RemovalAnswer = VIGEM_ERROR.VIGEM_ERROR_NONE;
			Faults(client.UnplugAllControllers);
			Assert.IsFalse(client.HasKept(3), "A controller the bus let go of at last is still kept.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A kept controller in its own tab's place is named as the reason, with Repair, on the tab and once in the Issues tab")]
		public void A_controller_the_driver_would_not_remove_is_named_with_repair()
		{
			// The tab reads the place as held by one of this program's own, and briefly as its own while the plug worker has
			// not yet looked again. Neither may blame another controller or send the person to Auto-Order.
			var hints = new[]
			{
				MainForm.ControllerStateHint(2, true, true, false, true, -1, true, VirtualError.RemovalRefused, XInputPlaces.HolderVirtual),
				MainForm.ControllerStateHint(2, true, true, true, true, 1, true, VirtualError.RemovalRefused, null),
				MainForm.ControllerStateHint(2, false, true, false, true, -1, true, VirtualError.RemovalRefused, XInputPlaces.HolderVirtual),
			};
			foreach (var hint in hints)
			{
				StringAssert.Contains(hint, "The driver would not remove a virtual controller made for Controller 2", hint);
				StringAssert.Contains(hint, "Repair the driver on the Issues tab", hint);
				Assert.IsFalse(hint.Contains(XInputPlaces.HolderWords(XInputPlaces.HolderVirtual)), "Another controller is blamed: " + hint);
				Assert.IsFalse(hint.Contains("Auto-Order"), "Auto-Order cannot move it: " + hint);
				Assert.IsFalse(hint.Contains("nplug"), "A virtual controller cannot be unplugged: " + hint);
			}
			// Said in the Issues tab once, with Repair, and not counted as a plug the bus refused.
			var health = new VirtualDriverNotWorkingIssue.Health { BusInstalled = true, Now = Environment.TickCount };
			for (var i = 0; i < health.Pads.Length; i++)
				health.Pads[i] = new VirtualDriverNotWorkingIssue.PadHealth
				{
					Wanted = true,
					LastResult = VirtualError.None,
					LastError = VIGEM_ERROR.VIGEM_ERROR_NONE,
					RumbleError = VIGEM_ERROR.VIGEM_ERROR_NONE,
				};
			health.Pads[1].LastResult = VirtualError.RemovalRefused;
			string message;
			Assert.AreEqual(IssueSeverity.Moderate, VirtualDriverNotWorkingIssue.Judge(health, out message), "The Issues tab says nothing.");
			Assert.AreEqual(1, Ui.Count(message, "would not remove"), message);
			StringAssert.Contains(message, "Controller 2: the driver would not remove its virtual controller");
			StringAssert.Contains(message, "Repair installs the driver again");
			Assert.AreEqual(2, DInputHelper.NextPlugFailures(2, VirtualError.RemovalRefused),
				"A controller the driver would not remove is counted as a plug it refused.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A tab whose place a controller the driver would not remove holds names it and points to Repair, not Auto-Order or unplugging")]
		public void Another_tab_whose_place_a_kept_controller_holds_is_told_to_repair()
		{
			// Controller 2's kept controller sits in XInput 3: tab 3 wanting a virtual controller of its own, tab 3 with a
			// device and none wanted, and tab 3 with nothing mapped.
			var hints = new[]
			{
				MainForm.ControllerStateHint(3, true, true, false, true, -1, true, VirtualError.PlaceTaken, XInputPlaces.HolderVirtual, true),
				MainForm.ControllerStateHint(3, true, true, false, true, -1, true, VirtualError.None, XInputPlaces.HolderVirtual, true),
				MainForm.ControllerStateHint(3, false, true, false, true, -1, true, VirtualError.None, XInputPlaces.HolderVirtual, true),
			};
			foreach (var hint in hints)
			{
				StringAssert.Contains(hint, "a virtual controller the driver would not remove", hint);
				StringAssert.Contains(hint, "Repair the driver on the Issues tab, or restart x360ce.", hint);
				Assert.IsFalse(hint.Contains(XInputPlaces.HolderWords(XInputPlaces.HolderVirtual)), "It is not named: " + hint);
				Assert.IsFalse(hint.Contains("Auto-Order"), "Auto-Order cannot move it: " + hint);
				Assert.IsFalse(hint.Contains("nplug"), "A virtual controller cannot be unplugged: " + hint);
			}
			// One of this program's own that the driver has not refused to remove is still advised as before.
			StringAssert.Contains(MainForm.ControllerStateHint(3, true, true, false, true, -1, true, VirtualError.PlaceTaken, XInputPlaces.HolderVirtual),
				"Use Auto-Order on the Devices page to move it, or unplug it.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A pad the driver would not let go of gets one Issues line, which says so, whatever plugs failed before")]
		public void A_pad_the_driver_would_not_let_go_of_gets_one_issue_line()
		{
			// Three plugs Windows never built, counted before the removal was refused: the driver accepted each of them.
			var health = new VirtualDriverNotWorkingIssue.Health { BusInstalled = true, Now = Environment.TickCount };
			for (var i = 0; i < health.Pads.Length; i++)
				health.Pads[i] = new VirtualDriverNotWorkingIssue.PadHealth
				{
					Wanted = true,
					LastResult = VirtualError.None,
					LastError = VIGEM_ERROR.VIGEM_ERROR_NONE,
					RumbleError = VIGEM_ERROR.VIGEM_ERROR_NONE,
				};
			health.Pads[1].LastResult = VirtualError.RemovalRefused;
			health.Pads[1].PlugFailures = VirtualDriverNotWorkingIssue.PlugFailuresToReport;
			string message;
			Assert.AreEqual(IssueSeverity.Moderate, VirtualDriverNotWorkingIssue.Judge(health, out message), "The Issues tab says nothing.");
			Assert.AreEqual(1, Ui.Count(message, "Controller 2"), "Controller 2 is said to have failed in two ways: " + message);
			StringAssert.Contains(message, "Controller 2: the driver would not remove its virtual controller");
			Assert.IsFalse(message.Contains("refused to make"), "The driver is said to have refused plugs it accepted: " + message);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The place a new controller arrives in is not claimed as well by the note of the one it replaced")]
		public void The_old_controllers_note_does_not_claim_the_new_ones_place()
		{
			// The controller the bus kept is still ours and can still be listed. Its note pointing at the place the new one
			// arrived in made two claims on one place, and then neither controller was shown in it.
			var notes = (Dictionary<string, int>)typeof(XInputPlaces)
				.GetField("OursByHardware", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
			const string old = @"RefusedRemovalTest\Old";
			const string fresh = @"RefusedRemovalTest\New";
			const string other = @"RefusedRemovalTest\Other";
			try
			{
				XInputPlaces.Remember(other, 2);
				XInputPlaces.Remember(old, 1);
				XInputPlaces.Remember(fresh, 1);
				Assert.IsFalse(notes.ContainsKey(old), "The controller the bus kept still claims the place the new one arrived in.");
				Assert.AreEqual(1, notes[fresh], "The new controller's place is not noted.");
				Assert.AreEqual(2, notes[other], "Another controller's note was rubbed out.");
				// One whose name could not be told apart still arrived there, so the note before it is out of date too.
				XInputPlaces.Remember(null, 1);
				Assert.IsFalse(notes.ContainsKey(fresh), "A controller no longer in a place still claims it.");
				Assert.AreEqual(2, notes[other], "Another controller's note was rubbed out.");
			}
			finally
			{
				XInputPlaces.Forget(old);
				XInputPlaces.Forget(fresh);
				XInputPlaces.Forget(other);
			}
		}
	}
}
