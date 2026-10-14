// @under-test: App.v4/Common/DInput/DInputHelper.Step5.VirtualDevices.cs, App.v4/Controls/PadControl.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.App.DInput;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>The place noted for each virtual controller is checked against the product number XInput reports there.</summary>
	/// <remarks>
	/// Where a controller went is noted as it arrives. With a real Xbox One controller on the machine, Controller 2's note
	/// said XInput 2 while XInput reported the Xbox One there and ours in XInput 1, so the picture drew the idle Xbox One
	/// while the mapped device moved.
	/// </remarks>
	[TestClass]
	public class OurPlaceCheckTest
	{
		const ushort XboxOne = 0x02FF;
		const ushort Ours = DInputHelper.VirtualProduct;
		const string OurPad = @"USB\VID_045E&PID_028E\01";

		/// <summary>The places and controllers <see cref="DInputHelper.CheckPlaces"/> leaves for the notes given.</summary>
		static int[] Checked(int[] noted, bool[] onBus, bool[] taken, ushort[] products, string[] ourHardware, out string[] hardware)
		{
			var places = (int[])noted.Clone();
			hardware = new string[4];
			DInputHelper.CheckPlaces(places, hardware, onBus, taken, products, ourHardware);
			return places;
		}

		static int[] Checked(int[] noted, bool[] onBus, bool[] taken, ushort[] products)
		{
			string[] hardware;
			return Checked(noted, onBus, taken, products, new string[0], out hardware);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A note on a place holding another kind of controller moves to the one place holding ours, and the pad is matched to its controller")]
		public void A_note_on_another_kind_of_controller_moves_to_ours()
		{
			string[] hardware;
			var places = Checked(new[] { -1, 1, -1, -1 }, new[] { false, true, false, false },
				new[] { true, true, false, false }, new ushort[] { Ours, XboxOne, 0, 0 }, new[] { OurPad }, out hardware);
			CollectionAssert.AreEqual(new[] { -1, 0, -1, -1 }, places, "Controller 2 is still noted in the Xbox One's place.");
			CollectionAssert.AreEqual(new[] { null, OurPad, null, null }, hardware,
				"The pad was not matched to the one controller of ours, so the Devices page cannot name its place.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A note on an empty place moves to the one place holding ours")]
		public void A_note_on_an_empty_place_moves_to_ours()
		{
			CollectionAssert.AreEqual(new[] { -1, 0, -1, -1 }, Checked(new[] { -1, 1, -1, -1 }, new[] { false, true, false, false },
				new[] { true, false, false, false }, new ushort[] { Ours, 0, 0, 0 }));
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A wrong note goes, and the place reads as not known, when which place is ours cannot be told")]
		public void A_wrong_note_goes_when_ours_cannot_be_told()
		{
			// A real Xbox 360 controller in XInput 3 reports the same product as ours.
			CollectionAssert.AreEqual(new[] { -1, -1, -1, -1 }, Checked(new[] { -1, 1, -1, -1 }, new[] { false, true, false, false },
				new[] { true, true, true, false }, new ushort[] { Ours, XboxOne, Ours, 0 }),
				"A place that may hold a real Xbox 360 controller was named as ours.");
			// Two of ours noted on Xbox One controllers: neither can be told from the other.
			CollectionAssert.AreEqual(new[] { -1, -1, -1, -1 }, Checked(new[] { 1, 0, -1, -1 }, new[] { true, true, false, false },
				new[] { true, true, true, true }, new ushort[] { XboxOne, XboxOne, Ours, Ours }),
				"Two controllers of ours were each given a place nobody can tell is theirs.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A note XInput agrees with, or says nothing against, stands; a second pad's wrong note finds the place left")]
		public void A_note_xinput_agrees_with_stands()
		{
			var onBus = new[] { false, true, false, false };
			CollectionAssert.AreEqual(new[] { -1, 1, -1, -1 }, Checked(new[] { -1, 1, -1, -1 }, onBus,
				new[] { true, true, false, false }, new ushort[] { XboxOne, Ours, 0, 0 }), "A note XInput agrees with was changed.");
			CollectionAssert.AreEqual(new[] { -1, 1, -1, -1 }, Checked(new[] { -1, 1, -1, -1 }, onBus,
				new[] { true, true, false, false }, new ushort[4]), "A note was changed by a library that does not say the product.");
			CollectionAssert.AreEqual(new[] { 0, 2, -1, -1 }, Checked(new[] { 0, 1, -1, -1 }, new[] { true, true, false, false },
				new[] { true, true, true, false }, new ushort[] { Ours, XboxOne, Ours, 0 }),
				"The pad noted on the Xbox One did not find the place of ours left over, or the other pad's note moved.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The place table's read checks the notes before it works the table out, and only while nothing was noted since the places were read")]
		public void The_place_table_read_checks_the_notes_first()
		{
			var source = System.IO.File.ReadAllText(System.IO.Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "XInputPlaces.cs"));
			var read = Ui.Between(source, "public static void Read()", "lock (SyncRoot)");
			Assert.IsTrue(read.IndexOf("CheckOurPlaces(") >= 0 && read.IndexOf("CheckOurPlaces(") < read.IndexOf("Resolve("),
				"The table is worked out from notes nobody checked.");
			StringAssert.Contains(read, "generation == System.Threading.Volatile.Read(ref _generation)",
				"A note written by a plug that finished during the read is checked against places read before it.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A tab whose controller has no place draws no controller, not the one holding the tab's own place")]
		public void A_tab_without_a_place_draws_no_other_controller()
		{
			Ui.OnUiThread(() =>
			{
				if (Global.DHelper == null)
					Global.InitDHelperHelper();
				var helper = Global.DHelper;
				var places = (int[])helper.XiPlaceForPad.Clone();
				var connected = (bool[])helper.LiveXiConnected.Clone();
				var states = (SharpDX.XInput.State[])helper.LiveXiStates.Clone();
				var getStates = SettingsManager.Options.GetXInputStates;
				var update = typeof(PadControl).GetMethod("UpdateControlFromXInput", BindingFlags.NonPublic | BindingFlags.Instance);
				var drawn = typeof(PadControl).GetField("newConnected", BindingFlags.NonPublic | BindingFlags.Instance);
				try
				{
					SettingsManager.Options.GetXInputStates = true;
					// Ours in XInput 1, with its left trigger part pressed; a real controller in XInput 2, Controller 2's own place.
					helper.LiveXiConnected[0] = true;
					helper.LiveXiStates[0].Gamepad.LeftTrigger = 126;
					helper.LiveXiConnected[1] = true;
					helper.LiveXiStates[1].Gamepad.LeftTrigger = 50;
					using (var form = new Form { Width = 900, Height = 700 })
					using (var pad = new PadControl(MapTo.Controller2) { Dock = DockStyle.Fill })
					{
						form.Controls.Add(pad);
						pad.InitPadControl();
						pad.UpdateSettingsMap();
						pad.InitPadData();
						form.Show();
						var trigger = pad.Controls.Find("LeftTriggerTextBox", true).Single();
						helper.XiPlaceForPad[1] = 0;
						update.Invoke(pad, null);
						Application.DoEvents();
						Assert.AreEqual("126", trigger.Text, "The picture does not read the place Controller 2's controller is in.");
						helper.XiPlaceForPad[1] = -1;
						update.Invoke(pad, null);
						Application.DoEvents();
						Assert.IsFalse((bool)drawn.GetValue(pad), "A tab whose controller has no place draws the controller in its own place.");
						Assert.AreEqual("0", trigger.Text, "A tab whose controller has no place shows the values of the controller in its own place.");
					}
				}
				finally
				{
					SettingsManager.Options.GetXInputStates = getStates;
					places.CopyTo(helper.XiPlaceForPad, 0);
					connected.CopyTo(helper.LiveXiConnected, 0);
					states.CopyTo(helper.LiveXiStates, 0);
				}
			});
		}
	}
}
