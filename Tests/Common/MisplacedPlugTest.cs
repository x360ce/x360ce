// @under-test: App.v4/Common/DInput/DInputHelper.Step5.VirtualDevices.cs, App.v4/Common/DInput/XInputReorderRunner.cs, App.v4/Issues/VirtualDriverNotWorkingIssue.cs
// @area: devices   @layer: unit
using JocysCom.ClassLibrary.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nefarius.ViGEm.Client;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using x360ce.App;
using x360ce.App.DInput;
using x360ce.Engine;
using x360ce.Engine.Data;
using static x360ce.Tests.BusRefusalFixtures;
using static x360ce.Tests.StandInXInputFixtures;

namespace x360ce.Tests
{
	/// <summary>A plug waits for XInput as long as its own wait lasts, and a pad Windows gave no place is made again only once a controller has come or gone.</summary>
	/// <remarks>
	/// XInput stops answering while Windows builds a controller. A plug that does not wait for it sees no place, takes its
	/// controller away, and without a hold makes it again every few seconds: each time a device change, and the whole device
	/// list read again. Here Windows' XInput is replaced, for the length of a test, by one that answers as the test sets, and the
	/// bus by a client with nothing native behind it, so nothing is made on the machine.
	/// </remarks>
	[TestClass]
	public class MisplacedPlugTest
	{
		/// <summary>The plug's wait for a place: the place a new controller filled, or -1.</summary>
		static int WaitForPlace(out bool answered)
		{
			var wait = typeof(DInputHelper).GetMethod("WaitForPlace", BindingFlags.NonPublic | BindingFlags.Static, null,
				new[] { typeof(bool[]), typeof(bool).MakeByRefType() }, null);
			var args = new object[] { new bool[4], false };
			var place = (int)wait.Invoke(null, args);
			answered = (bool)args[1];
			return place;
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A plug sees the place its controller was given after XInput was silent for longer than a second")]
		public void A_plug_sees_its_place_after_xinput_was_silent()
		{
			var place = -2;
			var answered = false;
			Logged(() => WithStandInXInput(() =>
			{
				// Windows builds the controller: nothing for a second and a half, then the controller in XInput 3.
				Volatile.Write(ref Taken, 1 << GivenPlace);
				Volatile.Write(ref SilentUntil, unchecked(Environment.TickCount + 1500));
				place = WaitForPlace(out answered);
			}));
			Assert.AreEqual(GivenPlace, place, "The place the controller was given was not seen after XInput was silent for 1.5 s.");
			Assert.IsTrue(answered, "XInput answering after 1.5 s was taken as never answering.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A plug hears XInput whose every read takes a little over a second, and sees the place its controller was given")]
		public void A_plug_hears_reads_that_each_take_over_a_second()
		{
			var place = -2;
			var answered = false;
			Logged(() => WithStandInXInput(() =>
			{
				Volatile.Write(ref Taken, 1 << GivenPlace);
				Volatile.Write(ref ReadTakes, 1025);
				place = WaitForPlace(out answered);
			}));
			Assert.AreEqual(GivenPlace, place, "Reads that each took just over a second were never heard, so the place was not seen.");
			Assert.IsTrue(answered, "XInput answering in just over a second was taken as never answering.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Putting controllers in order gives XInput as long to answer, before it starts and when it is done, as letting go of the controllers has")]
		public void Putting_controllers_in_order_waits_for_xinput_as_long_as_letting_go()
		{
			var runner = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "XInputReorderRunner.cs"));
			var run = Ui.Between(runner, "public bool Run(XInputReorderPlan plan)", "bool Fail(string why)");
			StringAssert.Contains(run, "var start = DInputHelper.OccupiedPlaces(StopLimit);",
				"The order gives XInput less time to answer before it starts than letting go of the controllers has.");
			StringAssert.Contains(Ui.Between(runner, "void Verify(", "static string Describe("), "var end = DInputHelper.OccupiedPlaces(StopLimit, products);",
				"The order gives XInput less time to say where everything ended up than letting go of the controllers has.");
			Assert.IsFalse(Ui.Between(runner, "bool RunStep(", "void Verify(").Contains("OccupiedPlaces"),
				"A step asks XInput which places are taken itself, rather than through the wait that does so only once controllers arrive.");
			// A real controller being switched off is not asked about: XInput would hold it open, and Windows cannot switch it off cleanly then.
			var off = Ui.Between(runner, "bool SwitchRealOff(", "bool SwitchRealOn(");
			Assert.IsFalse(off.Contains("Watch(") || off.Contains("Arriving("), "XInput is asked while a real controller is being switched off.");
			// A controller about to arrive starts the asking, before it is made.
			foreach (var arrival in new[] { new[] { "bool MakeVirtual(", "bool MakeDecoy(" }, new[] { "bool MakeDecoy(", "bool TakeAwayDecoy(" }, new[] { "bool SwitchRealOn(", "void Verify(" } })
				StringAssert.Contains(Ui.Between(runner, arrival[0], arrival[1]), "Arriving();", arrival[0] + " does not start asking XInput before the controller arrives.");
		}

		internal static T Field<T>(DInputHelper helper, string name)
		{
			var field = typeof(DInputHelper).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
			Assert.IsNotNull(field, "DInputHelper has no " + name + ".");
			return (T)field.GetValue(helper);
		}

		/// <summary>The places Controller 1 is held back for: -1 while it is not held back.</summary>
		static int Hold(DInputHelper helper)
		{
			return Field<int[]>(helper, "_misplacedWith")[0];
		}

		/// <summary>A helper whose Controller 1 was last taken away again as <paramref name="last"/>, with every place free.</summary>
		static DInputHelper HeldAfter(VirtualError last)
		{
			var helper = new DInputHelper();
			helper.VirtualErrors[0] = last;
			Field<int[]>(helper, "_misplacedWith")[0] = 0;
			Field<VirtualError[]>(helper, "_heldAs")[0] = last;
			helper.MisplacedIn[0] = last == VirtualError.PlaceNotGiven ? -1 : GivenPlace;
			return helper;
		}

		/// <summary>Runs the action with <paramref name="client"/> as the bus, holding <paramref name="pad"/> in Controller 1's slot.</summary>
		internal static void OnStandInBus(ViGEmClient client, RefusedRemovalTest.FakePad pad, Action action)
		{
			var current = ViGEmClient.Current;
			var readMachine = XInputPlaces.ReadMachine;
			client.Targets[0] = pad;
			Plugged = pad;
			ViGEmClient.Current = client;
			// No device tree to read: the controller that appears is not looked for by name.
			XInputPlaces.ReadMachine = () => new DeviceInfo[0];
			try
			{
				action();
			}
			finally
			{
				ViGEmClient.Current = current;
				XInputPlaces.ReadMachine = readMachine;
			}
		}

		/// <summary>One more plug of Controller 1 on a client with nothing native behind it, taken in as the pass takes it in.</summary>
		static VirtualError PlugAgain(DInputHelper helper, ViGEmClient client, RefusedRemovalTest.FakePad pad)
		{
			var result = VirtualError.None;
			OnStandInBus(client, pad, () =>
			{
				var plugging = helper.BeginPlug(1);
				Assert.IsTrue(plugging.Wait(20000), "The plug did not finish.");
				result = plugging.Result;
				helper.VirtualErrors[0] = result;
				if (!Field<bool[]>(helper, "_heldBack")[0])
					helper.PlugFailures[0] = DInputHelper.NextPlugFailures(helper.PlugFailures[0], result);
			});
			return result;
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A pad whose controller Windows gave no place is not made again while the places are as they were, says why as it did, and is asked again once they change")]
		public void A_pad_given_no_place_waits_for_a_change_in_the_places()
		{
			foreach (var last in new[] { VirtualError.PlaceNotGiven, VirtualError.PlaceWrong })
			{
				var helper = HeldAfter(last);
				var pad = RefusedRemovalTest.FakePad.Make(VIGEM_ERROR.VIGEM_ERROR_NONE);
				var result = VirtualError.None;
				// Every place free, as they were when the last controller was taken away again.
				RefusedRemovalTest.Faults(() => Logged(() => WithStandInXInput(() => result = PlugAgain(helper, RefusedRemovalTest.Client(), pad))));
				Assert.AreEqual(0, pad.ConnectsAsked, last + ": the controller was made again while the places were as they were.");
				Assert.AreEqual(last, result, last + ": the pad held back says it was " + result + ".");
				Assert.AreEqual(0, Hold(helper), last + ": the hold was let go of while the places were as they were.");
			}
			// A real controller has since arrived in Controller 1's own place: asked again, and told the place is taken.
			var moved = HeldAfter(VirtualError.PlaceNotGiven);
			var movedPad = RefusedRemovalTest.FakePad.Make(VIGEM_ERROR.VIGEM_ERROR_NONE);
			var answer = VirtualError.None;
			RefusedRemovalTest.Faults(() => Logged(() => WithStandInXInput(() =>
			{
				Volatile.Write(ref Taken, 1 << 0);
				answer = PlugAgain(moved, RefusedRemovalTest.Client(), movedPad);
			})));
			Assert.AreEqual(VirtualError.PlaceTaken, answer, "A pad held back was not asked again once the places changed.");
			Assert.AreEqual(0, movedPad.ConnectsAsked, "A controller was made while its place was taken.");
			Assert.AreEqual(-1, Hold(moved), "The hold outlasted a change in the places.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A pad held back stays held back through an attempt XInput did not answer, while the places are as they were")]
		public void A_hold_outlasts_an_attempt_xinput_did_not_answer()
		{
			var helper = HeldAfter(VirtualError.PlaceNotGiven);
			var pad = RefusedRemovalTest.FakePad.Make(VIGEM_ERROR.VIGEM_ERROR_NONE);
			var first = VirtualError.None;
			var second = VirtualError.None;
			RefusedRemovalTest.Faults(() => Logged(() => WithStandInXInput(() =>
			{
				// Silent for longer than a plug waits for XInput: five seconds.
				Volatile.Write(ref SilentUntil, unchecked(Environment.TickCount + 6000));
				first = PlugAgain(helper, RefusedRemovalTest.Client(), pad);
				Assert.IsNotNull(DInputHelper.OccupiedPlaces(TimeSpan.FromSeconds(10)), "XInput did not answer again.");
				second = PlugAgain(helper, RefusedRemovalTest.Client(), pad);
			})));
			Assert.AreEqual(VirtualError.NotAnswering, first, "The attempt XInput did not answer says " + first + ".");
			Assert.AreEqual(0, pad.ConnectsAsked, "The controller was made again after one attempt XInput did not answer, with the places as they were.");
			Assert.AreEqual(VirtualError.PlaceNotGiven, second, "The pad held back says " + second + ".");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A plug gives XInput the plug window to say which places are taken, held back or not, before it asks the bus")]
		public void A_plug_waits_for_xinput_through_the_plug_window_before_it_asks_the_bus()
		{
			var held = HeldAfter(VirtualError.PlaceNotGiven);
			var heldPad = RefusedRemovalTest.FakePad.Make(VIGEM_ERROR.VIGEM_ERROR_NONE);
			var heldResult = VirtualError.None;
			RefusedRemovalTest.Faults(() => Logged(() => WithStandInXInput(() =>
			{
				Volatile.Write(ref SilentUntil, unchecked(Environment.TickCount + 1500));
				heldResult = PlugAgain(held, RefusedRemovalTest.Client(), heldPad);
			})));
			Assert.AreEqual(VirtualError.PlaceNotGiven, heldResult, "The look at the places for a pad held back gave XInput less than the plug window.");
			// Not held back, with its own place taken: told so, once XInput answers.
			var free = new DInputHelper();
			var freePad = RefusedRemovalTest.FakePad.Make(VIGEM_ERROR.VIGEM_ERROR_NONE);
			var freeResult = VirtualError.None;
			RefusedRemovalTest.Faults(() => Logged(() => WithStandInXInput(() =>
			{
				Volatile.Write(ref Taken, 1 << 0);
				Volatile.Write(ref SilentUntil, unchecked(Environment.TickCount + 1500));
				freeResult = PlugAgain(free, RefusedRemovalTest.Client(), freePad);
			})));
			Assert.AreEqual(VirtualError.PlaceTaken, freeResult, "The look at the places before a plug gave XInput less than the plug window.");
			Assert.AreEqual(0, heldPad.ConnectsAsked + freePad.ConnectsAsked, "A controller was made.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A plug held back is not counted for or against the bus, and the pad goes on saying why it waits")]
		public void A_held_plug_is_not_counted_as_a_failed_one()
		{
			var update = typeof(DInputHelper).GetMethod("UpdateVirtualDevices", BindingFlags.NonPublic | BindingFlags.Instance);
			var game = new UserGame
			{
				FileName = "misplaced-plug.exe",
				EmulationType = (int)EmulationType.Virtual,
				EnableMask = (int)MapToMask.Controller1,
			};
			var o = SettingsManager.Options;
			var xinputEnabled = o.XInputEnabled;
			var testEnabled = o.TestEnabled;
			o.XInputEnabled = true;
			o.TestEnabled = false;
			try
			{
				foreach (var heldBack in new[] { true, false })
				{
					var helper = new DInputHelper();
					helper.PlugFailures[0] = 2;
					Field<bool[]>(helper, "_heldBack")[0] = heldBack;
					// Finished, and taken in by this pass.
					Field<Task<VirtualError>[]>(helper, "_plugging")[0] = Task.FromResult(VirtualError.PlaceNotGiven);
					OnStandInBus(RefusedRemovalTest.Client(), RefusedRemovalTest.FakePad.Make(VIGEM_ERROR.VIGEM_ERROR_NONE),
						() => update.Invoke(helper, new object[] { game }));
					Assert.AreEqual(VirtualError.PlaceNotGiven, helper.VirtualErrors[0], "The pad does not say why it waits.");
					Assert.AreEqual(heldBack ? 2 : 3, helper.PlugFailures[0], heldBack
						? "A plug held back was counted as one the bus failed, so the Issues tab counts attempts that never reached it."
						: "A plug Windows never built was not counted.");
				}
			}
			finally
			{
				o.XInputEnabled = xinputEnabled;
				o.TestEnabled = testEnabled;
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A hold ends when a controller is made in its own place, after a Repair, Remove or Auto-Order, and when the game leaves virtual emulation")]
		public void A_hold_ends_when_the_controller_is_made_or_after_a_repair()
		{
			// Made in its own place the way putting controllers in order makes it: straight away, whatever held it back.
			var made = HeldAfter(VirtualError.PlaceWrong);
			var pad = RefusedRemovalTest.FakePad.Make(VIGEM_ERROR.VIGEM_ERROR_NONE);
			var result = VirtualError.Other;
			RefusedRemovalTest.Faults(() => Logged(() => WithStandInXInput(() =>
			{
				Volatile.Write(ref GivenTo, 0);
				OnStandInBus(RefusedRemovalTest.Client(), pad, () => result = made.EnableFeeding(1));
			})));
			Assert.AreEqual(VirtualError.None, result, "The stand-in controller was not made in its own place.");
			Assert.AreEqual(-1, Hold(made), "A controller made in its own place is still held back.");
			// Repair, Remove and Auto-Order pick everything up again as it is now.
			var repaired = HeldAfter(VirtualError.PlaceNotGiven);
			OnStandInBus(RefusedRemovalTest.Client(), RefusedRemovalTest.FakePad.Make(VIGEM_ERROR.VIGEM_ERROR_NONE), repaired.ResumeAfterDeviceRemoval);
			Assert.AreEqual(-1, Hold(repaired), "A pad is still held back after a Repair, so the Repair the Issues tab offers changes nothing for it.");
			// Leaving virtual emulation forgets what the bus did, and the holds with it, so the Issues tab never reads a hold
			// from before the game came back.
			var left = HeldAfter(VirtualError.NotAnswering);
			var forget = typeof(DInputHelper).GetMethod("ForgetBusHealth", BindingFlags.NonPublic | BindingFlags.Instance);
			OnStandInBus(RefusedRemovalTest.Client(), RefusedRemovalTest.FakePad.Make(VIGEM_ERROR.VIGEM_ERROR_NONE), () => forget.Invoke(left, null));
			Assert.AreEqual(VirtualError.None, left.HeldAs(0), "A hold outlives leaving virtual emulation, so the Issues tab reports a pad nothing has tried since.");
			var step5 = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "DInputHelper.Step5.VirtualDevices.cs"));
			StringAssert.Contains(Ui.Between(step5, "if (!isVirtual)", "virtualModeActive = false;"), "ForgetBusHealth();",
				"Leaving virtual emulation does not forget what the bus did.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A pad whose last controller the bus would not let go of is not held back, so the bus's refusal is found and said")]
		public void A_pad_whose_controller_the_bus_kept_is_not_held_back()
		{
			var helper = HeldAfter(VirtualError.PlaceNotGiven);
			var client = RefusedRemovalTest.Client();
			var pad = RefusedRemovalTest.FakePad.Make(VIGEM_ERROR.VIGEM_ERROR_REMOVAL_FAILED);
			client.Targets[0] = pad;
			var result = VirtualError.None;
			RefusedRemovalTest.Faults(() =>
			{
				// Taken away again after Windows gave it no place, and the bus would not let it go.
				Assert.IsFalse(client.UnPlug(1), "The bus let go of a controller it keeps.");
				Logged(() => WithStandInXInput(() => result = PlugAgain(helper, client, pad)));
			});
			Assert.AreEqual(1, pad.ConnectsAsked, "A pad whose controller the bus would not let go of was held back, so its refusal is never said.");
			Assert.AreEqual(VirtualError.PlaceWrong, result, "Windows put the stand-in controller in XInput 3.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A controller XInput never showed during the whole wait is held back like one given no place, so it is not made again while the places are as they were")]
		public void A_controller_xinput_never_showed_is_held_back()
		{
			var helper = new DInputHelper();
			var pad = RefusedRemovalTest.FakePad.Make(VIGEM_ERROR.VIGEM_ERROR_NONE);
			var first = VirtualError.None;
			var second = VirtualError.None;
			RefusedRemovalTest.Faults(() => Logged(() => WithStandInXInput(() =>
			{
				SilentWhilePlugged = true;
				first = PlugAgain(helper, RefusedRemovalTest.Client(), pad);
				SilentWhilePlugged = false;
				second = PlugAgain(helper, RefusedRemovalTest.Client(), pad);
			})));
			Assert.AreEqual(VirtualError.NotAnswering, first, "A controller XInput never showed is said to be " + first + ".");
			Assert.AreEqual(1, pad.ConnectsAsked, "A controller XInput never showed was made again with the places as they were before it.");
			Assert.AreEqual(VirtualError.NotAnswering, second, "The pad held back after XInput did not answer says " + second + ".");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A plug that ends before it looks at the places is never taken as held back")]
		public void A_plug_that_ends_early_is_not_taken_as_held_back()
		{
			var helper = HeldAfter(VirtualError.PlaceNotGiven);
			var heldBack = Field<bool[]>(helper, "_heldBack");
			// Held back once, so the last plug taken in was held back.
			RefusedRemovalTest.Faults(() => Logged(() => WithStandInXInput(() =>
				PlugAgain(helper, RefusedRemovalTest.Client(), RefusedRemovalTest.FakePad.Make(VIGEM_ERROR.VIGEM_ERROR_NONE)))));
			Assert.IsTrue(heldBack[0], "The plug held back was not taken as held back.");
			// No controller in the slot: the plug ends at once, before it looks at anything.
			var result = VirtualError.None;
			RefusedRemovalTest.Faults(() => Logged(() => WithStandInXInput(() => result = PlugAgain(helper, RefusedRemovalTest.Client(), null))));
			Assert.AreEqual(VirtualError.Other, result, "A slot with no controller was not refused.");
			Assert.IsFalse(heldBack[0], "A plug that ended early is taken as held back, so it is not counted for or against the bus.");
		}

		/// <summary>Controller 1 made once and held back, the stand-in XInput answering as set up, then what the Issues tab says.</summary>
		static JocysCom.ClassLibrary.Controls.IssuesControl.IssueSeverity HeldThenJudged(Action setUp, out VirtualError held, out string message)
		{
			var helper = new DInputHelper();
			var pad = RefusedRemovalTest.FakePad.Make(VIGEM_ERROR.VIGEM_ERROR_NONE);
			var second = VirtualError.None;
			RefusedRemovalTest.Faults(() => Logged(() => WithStandInXInput(() =>
			{
				setUp();
				PlugAgain(helper, RefusedRemovalTest.Client(), pad);
				SilentWhilePlugged = false;
				second = PlugAgain(helper, RefusedRemovalTest.Client(), pad);
			})));
			Assert.AreEqual(1, pad.ConnectsAsked, "The controller was made again with the places as they were.");
			held = second;
			return x360ce.App.Issues.VirtualDriverNotWorkingIssue.Judge(VirtualBusHealthIssueTest.ReadHealth(helper), out message);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A pad held back because its controller was never seen is reported on the Issues tab at once, in the words of what held it back; one put in another place is not the driver's fault")]
		public void A_held_pad_is_reported_in_the_words_of_what_held_it_back()
		{
			VirtualError held;
			string message;
			// XInput said nothing while the controller was on the bus.
			var silent = HeldThenJudged(() => SilentWhilePlugged = true, out held, out message);
			Assert.AreEqual(JocysCom.ClassLibrary.Controls.IssuesControl.IssueSeverity.Moderate, silent,
				"A pad held back after XInput never showed its controller sits unmade, and the Issues tab says nothing.");
			StringAssert.Contains(message, "Controller 1: XInput did not answer while its virtual controller was being made");
			Assert.AreEqual(VirtualError.NotAnswering, held, "The pad held back does not say XInput did not answer.");
			// XInput answered, and Windows gave the controller no place.
			var notGiven = HeldThenJudged(() => Volatile.Write(ref GivenTo, -1), out held, out message);
			Assert.AreEqual(JocysCom.ClassLibrary.Controls.IssuesControl.IssueSeverity.Moderate, notGiven,
				"A pad held back after Windows gave its controller no place is not reported.");
			StringAssert.Contains(message, "Controller 1: the driver accepted its virtual controller, but Windows never finished building it.");
			Assert.AreEqual(VirtualError.PlaceNotGiven, held);
			// Windows put it in another place: the driver works.
			var wrong = HeldThenJudged(() => { }, out held, out message);
			Assert.AreEqual(JocysCom.ClassLibrary.Controls.IssuesControl.IssueSeverity.None, wrong,
				"A controller Windows put in another place is reported as a driver that does not work: " + message);
			Assert.AreEqual(VirtualError.PlaceWrong, held);
		}
	}
}
