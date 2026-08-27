// @under-test: Engine/Common/MapExpressionUnits.cs, Engine/Maps/Map.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.XInput;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// A formula is written in plain units and has to reach the controller in the device's own.
	/// These tests cover both ends of that translation, and that a stored formula is picked up at all.
	/// </summary>
	/// <remarks>
	/// The gap these close is the one a person meets first: a formula typed into a row that then does
	/// nothing, because nothing between the box and the controller ever looked at it.
	/// </remarks>
	[TestClass]
	public class MapExpressionUnitsTest
	{

		/// <summary>A controller reporting nothing but one axis at the value given.</summary>
		private static CustomDiState StateWithAxis(int index, int raw)
		{
			var state = Resting();
			state.Axis[index - 1] = raw;
			return state;
		}

		/// <summary>A controller nobody is touching.</summary>
		private static CustomDiState Resting()
		{
			// Built from an empty DirectInput state, which is how the program itself makes one.
			return new CustomDiState(new SharpDX.DirectInput.JoystickState());
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A row holding a formula is recognised as one, not read as a control name")]
		public void A_row_holding_a_formula_is_recognised()
		{
			var map = new Map(MapCode.RightTrigger, "=a5*2", TargetType.RightTrigger, "", "", "");
			Assert.IsNotNull(map.Expression, "The formula was not picked up, so the row does nothing.");
			Assert.AreEqual(0, map.Index, "A formula names no single control, so there is no index.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A row mapped to one control is still read the old way")]
		public void A_row_mapped_to_one_control_is_unchanged()
		{
			var map = new Map(MapCode.RightTrigger, "a5", TargetType.RightTrigger, "", "", "");
			Assert.IsNull(map.Expression, "A plain mapping was mistaken for a formula.");
			Assert.AreEqual(5, map.Index);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("An axis is read as -1 to 1 with the resting position at nought")]
		public void An_axis_reads_as_minus_one_to_one()
		{
			var reference = new MapReference('a', 5);
			Assert.AreEqual(0f, MapExpressionUnits.Read(reference, StateWithAxis(5, 32767), true), 0.001f,
				"A stick nobody is touching has to read as nought.");
			Assert.AreEqual(1f, MapExpressionUnits.Read(reference, StateWithAxis(5, 65535), true), 0.001f);
			Assert.AreEqual(-1f, MapExpressionUnits.Read(reference, StateWithAxis(5, 0), true), 0.001f);
			Assert.AreEqual(0.5f, MapExpressionUnits.Read(reference, StateWithAxis(5, 49151), true), 0.01f);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A control the device does not have reads as nought instead of failing")]
		public void A_control_that_is_not_there_reads_as_nothing()
		{
			// A formula naming axis ninety on a device with four is describing something that is not
			// there, and something that is not there is not being moved.
			Assert.AreEqual(0f, MapExpressionUnits.Read(new MapReference('a', 90), Resting(), true));
			Assert.AreEqual(0f, MapExpressionUnits.Read(new MapReference('b', 999), Resting(), true));
			Assert.AreEqual(0f, MapExpressionUnits.Read(new MapReference('a', 1), null, true));
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("=a5*2 on a trigger drives it, and reaches full travel at half a press")]
		public void The_formula_that_was_reported_broken_now_drives_a_trigger()
		{
			// This is the case that was reported doing nothing at all.
			var map = new Map(MapCode.RightTrigger, "=a5*2", TargetType.RightTrigger, "", "", "");
			var values = new float[MapExpression.MaxReferences];

			// Resting. A trigger rests at one end, which is nought, not the middle.
			Assert.IsTrue(MapExpressionUnits.TryFill(map.Expression, StateWithAxis(5, 0), values, false));
			Assert.AreEqual(0, MapExpressionUnits.ToTrigger(map.Expression.Evaluate(values)),
				"A trigger nobody is touching has to stay at nought.");

			// A quarter of the way: 0.25 doubled is 0.5, which is half a trigger.
			Assert.IsTrue(MapExpressionUnits.TryFill(map.Expression, StateWithAxis(5, 16384), values, false));
			var half = MapExpressionUnits.ToTrigger(map.Expression.Evaluate(values));
			Assert.IsTrue(half > 120 && half < 136, "Expected about half travel, got " + half + ".");

			// Half way: 0.5 doubled is 1, so the trigger is already fully pressed.
			Assert.IsTrue(MapExpressionUnits.TryFill(map.Expression, StateWithAxis(5, 32767), values, false));
			Assert.AreEqual(byte.MaxValue, MapExpressionUnits.ToTrigger(map.Expression.Evaluate(values)),
				"Doubling means full travel is reached at half a press.");

			// All the way up: doubled is 2, which cannot go past the end.
			Assert.IsTrue(MapExpressionUnits.TryFill(map.Expression, StateWithAxis(5, 65535), values, false));
			Assert.AreEqual(byte.MaxValue, MapExpressionUnits.ToTrigger(map.Expression.Evaluate(values)));
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A formula's answer lands correctly on a stick, both ways from the middle")]
		public void An_answer_lands_correctly_on_a_stick()
		{
			Assert.AreEqual(0, MapExpressionUnits.ToThumb(0f));
			Assert.AreEqual(short.MaxValue, MapExpressionUnits.ToThumb(1f));
			Assert.AreEqual(short.MinValue, MapExpressionUnits.ToThumb(-1f));
			Assert.AreEqual(short.MaxValue, MapExpressionUnits.ToThumb(9f), "Cannot travel past the end.");
			Assert.AreEqual(short.MinValue, MapExpressionUnits.ToThumb(-9f));
			Assert.AreEqual(0, MapExpressionUnits.ToThumb(float.NaN), "Not a number has to rest, not jump.");
		}

		[TestMethod, TestCategory("mapping")]
		[Description("A negative answer still presses a trigger, which only travels one way")]
		public void A_negative_answer_still_presses_a_trigger()
		{
			// A formula written for a stick put on a trigger should do something sensible rather than
			// nothing at all.
			Assert.AreEqual(byte.MaxValue, MapExpressionUnits.ToTrigger(-1f));
			Assert.AreEqual(0, MapExpressionUnits.ToTrigger(0f));
			Assert.AreEqual(0, MapExpressionUnits.ToTrigger(float.NaN));
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Saving a row keeps the formula that was typed, rather than throwing it away")]
		public void Saving_keeps_the_formula_that_was_typed()
		{
			// The reported symptom was a formula that did nothing. Part of that was here: saving put
			// the text through the translation that turns the name of one control into how it is
			// stored. A formula is not the name of a control, so it came back as nothing and the row
			// quietly reverted to whatever it held before. The person is told nothing either way.
			var box = new System.Windows.Forms.ComboBox();
			var item = new x360ce.App.SettingsMapItem
			{
				IniSection = "PAD1",
				IniKey = SettingName.RightTrigger,
				Control = box,
				Code = MapCode.RightTrigger,
			};
			var settings = x360ce.App.SettingsManager.Current.SettingsMap;
			settings.Add(item);
			try
			{
				box.Text = "=a5*2";
				Assert.AreEqual("=a5*2", x360ce.App.SettingsManager.Current.GetSettingValue(box),
					"The formula was not stored as typed, so the row loses it on save.");
				// A row mapped to one control still stores the short form it always did.
				box.Text = "Axis 5";
				Assert.AreEqual("a5", x360ce.App.SettingsManager.Current.GetSettingValue(box),
					"An ordinary mapping must still be stored the way it always was.");
			}
			finally
			{
				settings.Remove(item);
				box.Dispose();
			}
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Loading a stored formula shows it, with or without the switch being ready")]
		public void Loading_a_stored_formula_shows_it()
		{
			// The switch is attached when a row is built, and settings are loaded into rows at a
			// different moment. Making the display depend on the switch already existing meant that on
			// the wrong ordering the formula was handed back to the code that reads a control name,
			// which turns it into nothing. What the person then sees is an empty row.
			var box = new System.Windows.Forms.ComboBox();
			try
			{
				Assert.IsTrue(x360ce.App.Controls.MapExpressionToggle.ShowExpression(box, "=a5*2"),
					"A stored formula was not shown, so the row appears empty and is lost on next save.");
				Assert.AreEqual("=a5*2", box.Text);
				Assert.AreEqual(System.Windows.Forms.ComboBoxStyle.DropDown, box.DropDownStyle,
					"A row holding a formula has to be typeable, not a fixed list.");
				// An ordinary mapping is left for the normal path to handle.
				Assert.IsFalse(x360ce.App.Controls.MapExpressionToggle.ShowExpression(box, "a5"));
			}
			finally { box.Dispose(); }
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Each fx switch is named after its own row, so it can be found and pressed")]
		public void Each_switch_is_named_after_its_row()
		{
			// Thirty switches sharing one name identify none of them, and a switch with no name of its
			// own is reported by its window handle, which is a different number every run. Either way
			// nothing can ask for the same switch twice: not a test, not a screen reader, not somebody
			// driving the window from the keyboard.
			var toggle = new x360ce.App.Controls.MapExpressionToggle();
			try
			{
				toggle.NameAfterRow("Right Trigger");
				Assert.AreEqual("RightTriggerExpressionToggle", toggle.Name,
					"Windows reports this as the automation identifier. Empty means the window handle.");
				Assert.AreEqual("Right Trigger formula", toggle.Text,
					"Windows reports a button of this kind by its text and by nothing else, so the " +
					"text is the only thing that can tell one switch from another.");
				Assert.AreEqual("Right Trigger formula", toggle.AccessibleName,
					"This is what a screen reader says.");
				Assert.AreEqual("fx", x360ce.App.Controls.MapExpressionToggle.Label,
					"The face stays the same two letters on every row.");
				Assert.IsFalse(string.IsNullOrEmpty(toggle.AccessibleDescription),
					"The name says which row; the description has to say what pressing it does.");
				Assert.AreEqual(System.Windows.Forms.AccessibleRole.CheckButton, toggle.AccessibleRole);
			}
			finally { toggle.Dispose(); }
		}

		[TestMethod, TestCategory("mapping")]
		[Description("A row's name is read as the words shown on screen")]
		public void A_rows_name_reads_as_the_words_on_screen()
		{
			var box = new System.Windows.Forms.ComboBox { Name = "RightTriggerComboBox" };
			try
			{
				Assert.AreEqual("Right Trigger", x360ce.App.Controls.MapExpressionToggle.RowNameFor(box));
				box.AccessibleName = "Left Thumb Axis Y";
				Assert.AreEqual("Left Thumb Axis Y", x360ce.App.Controls.MapExpressionToggle.RowNameFor(box),
					"A name already written for a person is used as it stands.");
			}
			finally { box.Dispose(); }
		}

		[TestMethod, TestCategory("mapping")]
		[Description("A formula that will not compile leaves the row doing nothing, not crashing")]
		public void A_formula_that_will_not_compile_is_simply_not_a_mapping()
		{
			var map = new Map(MapCode.RightTrigger, "=a5*", TargetType.RightTrigger, "", "", "");
			Assert.IsNull(map.Expression, "A broken formula must not become a live mapping.");
			Assert.AreEqual(0, map.Index);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("The clock is a source a formula can read, written as a word")]
		public void The_clock_can_be_read()
		{
			MapExpression parsed;
			string error;
			int position;
			Assert.IsTrue(MapExpression.TryParse("=now", out parsed, out error, out position), error);
			Assert.AreEqual(1, parsed.References.Count);
			Assert.AreEqual(MapExpression.TimeType, parsed.References[0].Type);
			// A formula that names it twice still reads one value, not two.
			Assert.IsTrue(MapExpression.TryParse("=now+now", out parsed, out error, out position), error);
			Assert.AreEqual(1, parsed.References.Count, "The clock is one source however often it is named.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("The clock is fed like any other source, so a formula stays a plain calculation")]
		public void The_clock_is_fed_like_any_other_source()
		{
			// Time arriving as a value rather than being read inside the formula is what keeps the
			// whole thing testable: the same formula given the same instant always answers the same.
			MapExpression parsed;
			string error;
			int position;
			Assert.IsTrue(MapExpression.TryParse("=now/60000", out parsed, out error, out position), error);
			var values = new float[MapExpression.MaxReferences];
			values[0] = 90000f;                      // a minute and a half
			Assert.AreEqual(1.5f, parsed.Evaluate(values), 0.0001f);
			values[0] = 0f;
			Assert.AreEqual(0f, parsed.Evaluate(values), 0.0001f);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("The clock counts minutes, which is what it is for")]
		public void The_clock_can_be_read_as_minutes()
		{
			// Turning it on and off once a minute needs "round" and "floor" around it, which does not
			// fit while a mapping is stored in sixteen characters. What does fit is the count itself,
			// and that is what is checked until the column is widened.
			// The fraction of the current minute, rounded, is on for the second half of every minute.
			MapExpression parsed;
			string error;
			int position;
			Assert.IsTrue(MapExpression.TryParse("=now/60000", out parsed, out error, out position), error);
			var values = new float[MapExpression.MaxReferences];
			values[0] = 30000f;                      // half a minute
			Assert.AreEqual(0.5f, parsed.Evaluate(values), 0.001f);
			values[0] = 60000f;                      // one minute
			Assert.AreEqual(1f, parsed.Evaluate(values), 0.001f);
			values[0] = 150000f;                     // two and a half minutes
			Assert.AreEqual(2.5f, parsed.Evaluate(values), 0.001f);
		}

		[TestMethod, TestCategory("mapping")]
		[Description("The clock counts up, in milliseconds")]
		public void The_clock_counts_up_in_milliseconds()
		{
			var first = MapExpressionUnits.Milliseconds();
			System.Threading.Thread.Sleep(50);
			var second = MapExpressionUnits.Milliseconds();
			Assert.IsTrue(second > first, "The clock did not move.");
			var moved = second - first;
			Assert.IsTrue(moved >= 40f && moved < 2000f,
				"Fifty milliseconds of waiting moved the clock by " + moved + ", which is not milliseconds.");
		}

		[TestMethod, TestCategory("mapping")]
		[Description("A source letter for the clock is still refused, so the word is the only spelling")]
		public void The_clock_has_one_spelling()
		{
			MapExpression parsed;
			string error;
			int position;
			Assert.IsFalse(MapExpression.TryParse("=t1", out parsed, out error, out position),
				"'t1' must not quietly become the clock; a person who wants it writes 'now'.");
		}

		/// <summary>Works a formula out for a destination, the way the program does when polling.</summary>
		private static float Drive(string formula, int axisIndex, int raw, bool isThumb)
		{
			MapExpression parsed;
			string error;
			int position;
			Assert.IsTrue(MapExpression.TryParse(formula, out parsed, out error, out position), error);
			var values = new float[MapExpression.MaxReferences];
			Assert.IsTrue(MapExpressionUnits.TryFill(parsed, StateWithAxis(axisIndex, raw), values, isThumb));
			return parsed.Evaluate(values);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("=a5*2 on a trigger: rests at nothing and reaches full at a half press")]
		public void The_reported_formula_gives_the_expected_trigger_values()
		{
			// A trigger rests at one end, not in the middle, and the whole of its travel is the
			// whole of a trigger's travel. Reading it as though it rested in the middle put it at
			// minus one while nobody was touching it, which doubled to minus two and came out as a
			// trigger held fully down. That is what "not working properly" looked like.
			Assert.AreEqual(0, MapExpressionUnits.ToTrigger(Drive("=a5*2", 5, 0, false)),
				"Resting. Nobody is touching it.");
			Assert.AreEqual(128, MapExpressionUnits.ToTrigger(Drive("=a5*2", 5, 16384, false)), 2,
				"A quarter pressed, doubled, is half a trigger.");
			Assert.AreEqual(255, MapExpressionUnits.ToTrigger(Drive("=a5*2", 5, 32767, false)),
				"Half pressed, doubled, is a full trigger.");
			Assert.AreEqual(255, MapExpressionUnits.ToTrigger(Drive("=a5*2", 5, 65535, false)),
				"Fully pressed cannot go past the end.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A plain trigger mapping written as a formula changes nothing")]
		public void A_plain_trigger_formula_matches_the_plain_mapping()
		{
			// Somebody switching a row to a formula and editing nothing must get exactly what they had.
			Assert.AreEqual(0, MapExpressionUnits.ToTrigger(Drive("=a5", 5, 0, false)));
			Assert.AreEqual(127, MapExpressionUnits.ToTrigger(Drive("=a5", 5, 32767, false)), 2);
			Assert.AreEqual(255, MapExpressionUnits.ToTrigger(Drive("=a5", 5, 65535, false)));
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("The same source on a stick still rests in the middle and goes both ways")]
		public void The_same_source_on_a_stick_still_goes_both_ways()
		{
			// A stick rests in the middle, so the same reading has to mean something different here.
			// This is the pair that stops one being fixed by breaking the other.
			Assert.AreEqual(short.MinValue, MapExpressionUnits.ToThumb(Drive("=a1", 1, 0, true)),
					"Pushed fully one way.");
			Assert.AreEqual(0, MapExpressionUnits.ToThumb(Drive("=a1", 1, 32767, true)),
					"Resting in the middle.");
			Assert.AreEqual(short.MaxValue, MapExpressionUnits.ToThumb(Drive("=a1", 1, 65535, true)),
					"Pushed fully the other way.");
		}

		[TestMethod, TestCategory("mapping")]
		[Description("A button reads the same whatever it drives")]
		public void A_button_reads_the_same_either_way()
		{
			// Only an axis is read differently by its destination, because only an axis has a resting
			// place that depends on what kind of control it is.
			var pressed = Resting();
			pressed.Buttons[0] = true;
			var values = new float[MapExpression.MaxReferences];
			MapExpression parsed;
			string error;
			int position;
			Assert.IsTrue(MapExpression.TryParse("=b1", out parsed, out error, out position), error);
			Assert.IsTrue(MapExpressionUnits.TryFill(parsed, pressed, values, false));
			Assert.AreEqual(1f, parsed.Evaluate(values), 0.001f);
			Assert.IsTrue(MapExpressionUnits.TryFill(parsed, pressed, values, true));
			Assert.AreEqual(1f, parsed.Evaluate(values), 0.001f);
		}

	}
}
