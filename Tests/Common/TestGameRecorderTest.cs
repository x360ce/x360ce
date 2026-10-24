// @under-test: TestGame/InputRecorder.cs
// @area: test-game   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using x360ce.TestGame;

namespace x360ce.Tests
{
	/// <summary>
	/// The x360ce Test Game makes of its input what a game makes of it: a jump, and a double jump when the jump is pressed
	/// again soon enough. Each way of reading the keyboard records every key, so only one may count, and only what arrives
	/// while the game is in front.
	/// </summary>
	[TestClass]
	public class TestGameRecorderTest
	{
		static InputEvent Key(string kind, long ms, string api = "RawInput", bool inFront = true, string name = "Space")
		{
			return new InputEvent { Source = "Keyboard", Api = api, InFront = inFront, Kind = kind, Name = name, Ms = ms };
		}

		static string[] Actions(InputRecorder r)
		{
			return r.Since(0, 1000).Where(x => x.Source == "Action").Select(x => x.Kind + (x.Kind == "Double jump" ? " " + x.Value : "")).ToArray();
		}

		[TestMethod, TestCategory("test-game")]
		[Description("A second jump press within the window is a double jump with its gap; a third press, or one too late, jumps again")]
		public void A_second_press_soon_enough_is_a_double_jump()
		{
			var r = new InputRecorder { DoubleJumpMs = 250 };
			r.Add(Key("Down", 1000)); r.Add(Key("Up", 1030));
			r.Add(Key("Down", 1123)); r.Add(Key("Up", 1150));
			r.Add(Key("Down", 1200)); r.Add(Key("Up", 1230));
			r.Add(Key("Down", 2000)); r.Add(Key("Up", 2030));
			r.Add(Key("Down", 2300)); r.Add(Key("Up", 2330));
			CollectionAssert.AreEqual(new[] { "Jump", "Double jump 123", "Jump", "Jump", "Jump" }, Actions(r));
			Assert.AreEqual("Jump", r.LastAction.Kind);
		}

		[TestMethod, TestCategory("test-game")]
		[Description("A key the keyboard repeats while it is held is a Repeat, and makes no jump")]
		public void A_held_key_repeats_without_jumping()
		{
			var r = new InputRecorder();
			r.Add(Key("Down", 1000));
			var repeat = r.Add(Key("Down", 1050));
			r.Add(Key("Down", 1100));
			r.Add(Key("Up", 1150));
			Assert.AreEqual("Repeat", repeat.Kind);
			CollectionAssert.AreEqual(new[] { "Jump" }, Actions(r));
		}

		[TestMethod, TestCategory("test-game")]
		[Description("Only the chosen way of reading the keyboard jumps, so one press read three ways jumps once")]
		public void One_press_read_three_ways_jumps_once()
		{
			var r = new InputRecorder { JumpApi = "Messages" };
			r.Add(Key("Down", 1000, "RawInput"));
			r.Add(Key("Down", 1001, "Messages"));
			r.Add(Key("Down", 1002, "DirectInput"));
			CollectionAssert.AreEqual(new[] { "Jump" }, Actions(r));
			StringAssert.Contains(r.LastAction.Note, "Keyboard Space");
		}

		[TestMethod, TestCategory("test-game")]
		[Description("What arrives while the game is behind is recorded and makes no jump, as a game ignores it")]
		public void Input_behind_makes_no_jump()
		{
			var r = new InputRecorder();
			var behind = r.Add(Key("Down", 1000, inFront: false));
			r.Add(new InputEvent { Source = "XInput 1", Api = "XInput", InFront = false, Kind = "Down", Name = "A", Ms = 1100 });
			Assert.AreEqual(2, r.Since(0, 10).Count, "Input behind was not recorded.");
			Assert.IsFalse(behind.InFront);
			Assert.AreEqual(0, Actions(r).Length, "Input behind made a jump.");
		}

		[TestMethod, TestCategory("test-game")]
		[Description("Button A through XInput jumps and double-jumps as the jump key does")]
		public void Button_A_jumps_too()
		{
			var r = new InputRecorder();
			foreach (var ms in new[] { 1000, 1100 })
			{
				r.Add(new InputEvent { Source = "XInput 2", Api = "XInput", InFront = true, Kind = "Down", Name = "A", Ms = ms });
				r.Add(new InputEvent { Source = "XInput 2", Api = "XInput", InFront = true, Kind = "Up", Name = "A", Ms = ms + 20 });
			}
			CollectionAssert.AreEqual(new[] { "Jump", "Double jump 100" }, Actions(r));
		}

		[TestMethod, TestCategory("test-game")]
		[Description("Events are read on from a number, and clearing forgets them without reusing a number")]
		public void Events_are_read_on_and_numbers_are_never_reused()
		{
			var r = new InputRecorder();
			r.Add(Key("Down", 1000, name: "A")); r.Add(Key("Up", 1010, name: "A"));
			var first = r.Since(0, 1);
			Assert.AreEqual(1, first.Count);
			Assert.AreEqual(1, r.Since(first[0].Seq, 10).Count, "Reading on from the first event did not give the second.");
			var next = r.Next;
			r.Clear();
			Assert.IsNull(r.Last);
			Assert.AreEqual(0, r.Since(0, 10).Count);
			Assert.AreEqual(next, r.Add(Key("Down", 2000, name: "B")).Seq, "Clearing reused a number.");
		}
	}
}
