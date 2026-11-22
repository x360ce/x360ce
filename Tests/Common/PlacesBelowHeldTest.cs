// @under-test: App.v4/Common/DInput/DInputHelper.Step5.VirtualDevices.cs, App.v4/ViGEm/Client/ViGEmClient.x360ce.cs, App.v4/Common/DInput/VirtualError.cs
// @area: devices   @layer: unit
using JocysCom.ClassLibrary.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using x360ce.App;
using x360ce.App.DInput;
using static x360ce.Tests.BusRefusalFixtures;
using static x360ce.Tests.StandInXInputFixtures;

namespace x360ce.Tests
{
	/// <summary>A controller tab reaches its own place while the places below it are free: each is held by a temporary controller while its controller arrives.</summary>
	/// <remarks>
	/// Windows gives a new controller the lowest free place, so with Controller 1 unused, Controller 2's controller landed in
	/// XInput 1, was taken away again and waited for good. Here Windows' XInput is the stand-in one, and every controller made
	/// is put in the lowest free place, as measured with ViGEmBus on Windows 11, unless a test puts a temporary one elsewhere.
	/// The bus is a client with nothing native behind it.
	/// </remarks>
	[TestClass]
	public class PlacesBelowHeldTest
	{
		static bool[] Places(params int[] taken)
		{
			var places = new bool[4];
			foreach (var place in taken)
				places[place] = true;
			return places;
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Controller 2 with XInput 1 free needs XInput 1 held; with a real controller in XInput 1 it needs nothing held")]
		public void Pad_2_needs_place_1_held_only_while_it_is_free()
		{
			CollectionAssert.AreEqual(new[] { 0 }, DInputHelper.PlacesToHold(Places(), 1), "XInput 1 free is not held for Controller 2.");
			CollectionAssert.AreEqual(new int[0], DInputHelper.PlacesToHold(Places(0), 1), "XInput 1 held by a real controller is held again.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Only the free places below a tab's own are held, lowest first, and Controller 1 needs none")]
		public void Only_free_places_below_are_held_lowest_first()
		{
			CollectionAssert.AreEqual(new[] { 0, 1 }, DInputHelper.PlacesToHold(Places(), 2), "Controller 3 alone.");
			CollectionAssert.AreEqual(new[] { 0, 2 }, DInputHelper.PlacesToHold(Places(1), 3), "Controller 4 with Controller 2 in its place.");
			CollectionAssert.AreEqual(new int[0], DInputHelper.PlacesToHold(Places(0, 1, 2), 3), "Controller 4 with every place below taken.");
			CollectionAssert.AreEqual(new int[0], DInputHelper.PlacesToHold(Places(), 0), "Controller 1 has no place below it.");
			CollectionAssert.AreEqual(new int[0], DInputHelper.PlacesToHold(Places(1, 2, 3), 0), "Controller 1 with every other place taken.");
		}

		/// <summary>A controller with nothing native behind it, which Windows puts in the lowest free place, as it does a new controller.</summary>
		/// <remarks>The temporary ones a plug makes come from <see cref="MakeTemporary"/>, and <see cref="PutAt"/> and <see cref="Refusal"/> apply to them only.</remarks>
		internal class PlacedPad : Xbox360Controller
		{
			PlacedPad(ViGEmClient client) : base(client) { }

			/// <summary>Every temporary one made since the test began, in order.</summary>
			public static readonly List<PlacedPad> Made = new List<PlacedPad>();
			/// <summary>Where the next temporary one is put, or -1 for the lowest free place.</summary>
			public static int PutAt = -1;
			/// <summary>What the bus answers a new temporary one: VIGEM_ERROR_NONE when it takes it.</summary>
			public static VIGEM_ERROR Refusal = VIGEM_ERROR.VIGEM_ERROR_NONE;

			public bool Temporary;
			public int Place = -1;
			public bool Attached;
			public bool Freed;
			public int ConnectsAsked;

			/// <summary>A tab's own controller.</summary>
			public static PlacedPad Make()
			{
				var pad = (PlacedPad)FormatterServices.GetUninitializedObject(typeof(PlacedPad));
				GC.SuppressFinalize(pad);
				typeof(Xbox360Controller).GetProperty(nameof(Xbox360Controller.RumbleError)).SetValue(pad, VIGEM_ERROR.VIGEM_ERROR_NONE);
				return pad;
			}

			/// <summary>A temporary controller, as <see cref="ViGEmClient.NewTemporary"/> makes one.</summary>
			public static Xbox360Controller MakeTemporary(ViGEmClient client)
			{
				var pad = Make();
				pad.Temporary = true;
				lock (Made)
					Made.Add(pad);
				return pad;
			}

			public override void Connect()
			{
				ConnectsAsked++;
				if (Temporary && Refusal != VIGEM_ERROR.VIGEM_ERROR_NONE)
					throw new ViGEmException(Refusal);
				var taken = Volatile.Read(ref Taken);
				var place = Temporary ? PutAt : -1;
				for (var i = 0; place < 0 && i < 4; i++)
					if ((taken & (1 << i)) == 0)
						place = i;
				Place = place;
				Attached = true;
				Volatile.Write(ref Taken, taken | (1 << place));
			}

			public override void Disconnect()
			{
				if (!Attached)
					return;
				Attached = false;
				Volatile.Write(ref Taken, Volatile.Read(ref Taken) & ~(1 << Place));
			}

			protected override void Dispose(bool disposing)
			{
				// Nothing native to free.
				Freed = true;
			}
		}

		/// <summary>One plug of this pad's controller on a stand-in bus, with temporary controllers made by <see cref="PlacedPad.MakeTemporary"/>.</summary>
		static VirtualError Plug(DInputHelper helper, uint pad, PlacedPad own, out List<KeyValuePair<string, System.Diagnostics.TraceLevel>> log)
		{
			var result = VirtualError.Other;
			var client = RefusedRemovalTest.Client();
			var current = ViGEmClient.Current;
			var readMachine = XInputPlaces.ReadMachine;
			var newTemporary = ViGEmClient.NewTemporary;
			client.Targets[pad - 1] = own;
			ViGEmClient.Current = client;
			ViGEmClient.NewTemporary = PlacedPad.MakeTemporary;
			// No device tree to read: the controller that appears is not looked for by name.
			XInputPlaces.ReadMachine = () => new DeviceInfo[0];
			try
			{
				log = Logged(() => result = helper.EnableFeeding(pad));
			}
			finally
			{
				ViGEmClient.Current = current;
				ViGEmClient.NewTemporary = newTemporary;
				XInputPlaces.ReadMachine = readMachine;
			}
			return result;
		}

		/// <summary>Runs the action on the stand-in XInput with no temporary controller made yet, and every one put back as it was after.</summary>
		static void OnStandIn(Action action)
		{
			lock (PlacedPad.Made)
				PlacedPad.Made.Clear();
			PlacedPad.PutAt = -1;
			PlacedPad.Refusal = VIGEM_ERROR.VIGEM_ERROR_NONE;
			try
			{
				RefusedRemovalTest.Faults(() => WithStandInXInput(action));
			}
			finally
			{
				PlacedPad.PutAt = -1;
				PlacedPad.Refusal = VIGEM_ERROR.VIGEM_ERROR_NONE;
			}
		}

		static void AssertAllTakenAway(string what)
		{
			foreach (var filler in PlacedPad.Made)
			{
				Assert.IsFalse(filler.Attached, what + ": a temporary controller is left on the bus, in XInput " + (filler.Place + 1) + ".");
				Assert.IsTrue(filler.Freed, what + ": a temporary controller was not freed.");
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Controller 2 alone is made in XInput 2 while a temporary controller holds XInput 1, which is then taken away and named in the log")]
		public void Pad_2_alone_is_made_in_its_own_place()
		{
			var helper = new DInputHelper();
			var own = PlacedPad.Make();
			var result = VirtualError.Other;
			var log = new List<KeyValuePair<string, System.Diagnostics.TraceLevel>>();
			var takenAfter = -1;
			OnStandIn(() =>
			{
				result = Plug(helper, 2, own, out log);
				takenAfter = Volatile.Read(ref Taken);
			});
			Assert.AreEqual(VirtualError.None, result, "Controller 2 was not made in its own place.");
			Assert.AreEqual(1, own.Place, "Controller 2's controller is not in XInput 2.");
			Assert.AreEqual(1, helper.XiPlaceForPad[1], "Controller 2's place is not noted as XInput 2.");
			Assert.AreEqual(1, own.ConnectsAsked, "Controller 2's controller was not made once.");
			CollectionAssert.AreEqual(new[] { 0 }, PlacedPad.Made.Select(x => x.Place).ToArray(), "XInput 1 was not held by one temporary controller.");
			AssertAllTakenAway("Controller 2");
			Assert.AreEqual(1 << 1, takenAfter, "The places are not Controller 2's own alone: the temporary controller's is still taken.");
			Assert.AreEqual(-1, helper._misplacedWith[1], "Controller 2 is held back.");
			Assert.IsTrue(log.Any(x => x.Key.Contains("Controller 2") && x.Key.Contains("temporary controller")),
				"The log does not name the temporary controller: " + string.Join(" | ", log.Select(x => x.Key)));
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Controller 2 with a real controller in XInput 1 is made with no temporary controller")]
		public void Pad_2_after_a_real_controller_needs_no_temporary_controller()
		{
			var helper = new DInputHelper();
			var own = PlacedPad.Make();
			var result = VirtualError.Other;
			List<KeyValuePair<string, System.Diagnostics.TraceLevel>> log = null;
			OnStandIn(() =>
			{
				Volatile.Write(ref Taken, 1 << 0);
				result = Plug(helper, 2, own, out log);
			});
			Assert.AreEqual(VirtualError.None, result);
			Assert.AreEqual(0, PlacedPad.Made.Count, "A temporary controller was made though XInput 1 was taken.");
			Assert.IsFalse(log.Any(x => x.Key.Contains("temporary controller")), "A temporary controller is named in the log though none was made.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Controller 4 with only XInput 2 taken holds XInput 1 and then XInput 3, and is made in XInput 4")]
		public void Pad_4_holds_each_free_place_below_lowest_first()
		{
			var helper = new DInputHelper();
			var own = PlacedPad.Make();
			var result = VirtualError.Other;
			List<KeyValuePair<string, System.Diagnostics.TraceLevel>> log;
			var takenAfter = -1;
			OnStandIn(() =>
			{
				Volatile.Write(ref Taken, 1 << 1);
				result = Plug(helper, 4, own, out log);
				takenAfter = Volatile.Read(ref Taken);
			});
			Assert.AreEqual(VirtualError.None, result, "Controller 4 was not made in its own place.");
			Assert.AreEqual(3, own.Place, "Controller 4's controller is not in XInput 4.");
			CollectionAssert.AreEqual(new[] { 0, 2 }, PlacedPad.Made.Select(x => x.Place).ToArray(), "The free places were not held lowest first.");
			AssertAllTakenAway("Controller 4");
			Assert.AreEqual((1 << 1) | (1 << 3), takenAfter, "The places are not as they were, with Controller 4's own added.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A temporary controller Windows puts in the tab's own place holds the tab back, and its controller is not made")]
		public void A_temporary_controller_in_the_own_place_holds_the_pad_back()
		{
			var helper = new DInputHelper();
			var own = PlacedPad.Make();
			var result = VirtualError.None;
			List<KeyValuePair<string, System.Diagnostics.TraceLevel>> log;
			OnStandIn(() =>
			{
				PlacedPad.PutAt = 1;
				result = Plug(helper, 2, own, out log);
			});
			Assert.AreEqual(VirtualError.PlaceWrong, result, "A temporary controller in Controller 2's place was not said as its place not given.");
			Assert.AreEqual(0, own.ConnectsAsked, "Controller 2's controller was made though it could not reach its place.");
			AssertAllTakenAway("Controller 2");
			Assert.AreEqual(VirtualError.PlaceWrong, helper.HeldAs(1), "Controller 2 is not held back until the places change.");
			Assert.AreEqual(0, helper._misplacedWith[1], "Controller 2 waits for other places than the ones it found.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A temporary controller the bus refuses ends the plug as a refusal, with nothing made and the bus's answer kept")]
		public void A_refused_temporary_controller_makes_nothing()
		{
			var helper = new DInputHelper();
			var own = PlacedPad.Make();
			var result = VirtualError.None;
			List<KeyValuePair<string, System.Diagnostics.TraceLevel>> log;
			OnStandIn(() =>
			{
				PlacedPad.Refusal = VIGEM_ERROR.VIGEM_ERROR_BUS_ACCESS_FAILED;
				result = Plug(helper, 3, own, out log);
			});
			Assert.AreEqual(VirtualError.Other, result);
			Assert.AreEqual(VIGEM_ERROR.VIGEM_ERROR_BUS_ACCESS_FAILED, helper.BusErrors[2], "The bus's answer is not kept for the Issues tab.");
			Assert.AreEqual(0, own.ConnectsAsked, "Controller 3's controller was made with the places below it free.");
			Assert.IsTrue(PlacedPad.Made.All(x => x.Freed), "A refused temporary controller was not freed.");
			Assert.AreEqual(-1, helper._misplacedWith[2], "A refusal holds the pad back, so it is not tried again after the plug wait.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A tab held back because Windows did not give it its own place is told so, with no claim that it would push another controller out")]
		public void A_held_tab_is_told_its_own_place_was_not_given()
		{
			var text = string.Format(JocysCom.ClassLibrary.Runtime.Attributes.GetDescription(VirtualError.PlaceWrong), 2, "a real controller");
			StringAssert.Contains(text, "XInput 2");
			Assert.IsFalse(text.Contains("push"), "The reason claims a push-out: " + text);
		}
	}
}
