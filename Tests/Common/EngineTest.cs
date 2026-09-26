// @under-test: Engine/Maps/SettingsConverter.cs, Engine/Maps/Map.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// Mapping values are parsed on the controller thread for every pad on every update, and a
	/// failure there previously ended that thread. These tests pin the parser's contract.
	/// </summary>
	[TestClass]
	public class EngineTest
	{

		[TestMethod, TestCategory("mapping"), TestCategory("smoke")]
		[Description("Axis, button and slider values parse to the type and index they name")]
		public void Ini_values_parse_to_their_type_and_index()
		{
			// Outside any field a bare number is a button, and 'b' says so outright.
			AssertParses("3", MapType.Button, 3);
			AssertParses("b3", MapType.Button, 3);
			AssertParses("a1", MapType.Axis, 1);
			AssertParses("s2", MapType.Slider, 2);
			AssertParses("x4", MapType.HAxis, 4);
			AssertParses("h5", MapType.HSlider, 5);
			AssertParses("p1", MapType.POV, 1);
			AssertParses("d2", MapType.DPOVButton, 2);
		}

		[TestMethod, TestCategory("mapping")]
		[Description("A minus sign after the prefix selects the inverted form of the same type")]
		public void Minus_sign_selects_the_inverted_type()
		{
			AssertParses("a-2", MapType.IAxis, 2);
			AssertParses("s-3", MapType.ISlider, 3);
			AssertParses("x-1", MapType.IHAxis, 1);
		}

		[TestMethod, TestCategory("mapping")]
		[Description("An empty or malformed value is rejected rather than throwing")]
		public void Malformed_values_are_rejected_without_throwing()
		{
			foreach (var value in new[] { null, "", " ", "x", "a", "a0", "0", "b", "b0", "axis 1", "!!", "a-" })
			{
				MapType type;
				int index;
				// The parser is called for user-edited settings, so bad input must not throw.
				var parsed = SettingsConverter.TryParseIniValue(value, out type, out index);
				Assert.IsFalse(parsed, $"'{value ?? "(null)"}' should not parse.");
			}
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A bare number is a control of the field's own kind: an axis on a stick axis, a D-Pad on the D-Pad, a button elsewhere")]
		public void A_bare_number_is_a_control_of_the_fields_own_kind()
		{
			// As the native library reads them, and as the presets written for it store them: this is
			// a Logitech RumblePad 2, whose right stick is axes 3 and 6.
			AssertParses("3", MapCode.RightThumbAxisX, MapType.Axis, 3);
			AssertParses("-6", MapCode.RightThumbAxisY, MapType.IAxis, 6);
			AssertParses("1", MapCode.DPad, MapType.POV, 1);
			AssertParses("3", MapCode.ButtonB, MapType.Button, 3);
			AssertParses("-3", MapCode.ButtonB, MapType.IButton, 3);
			AssertParses("7", MapCode.LeftTrigger, MapType.Button, 7);
			AssertParses("9", MapCode.LeftThumbUp, MapType.Button, 9);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A prefix names the kind of control in any field, and 'b' names a button")]
		public void A_prefix_names_the_kind_of_control_in_any_field()
		{
			AssertParses("b2", MapCode.LeftThumbAxisX, MapType.Button, 2);
			AssertParses("b-2", MapCode.LeftThumbAxisY, MapType.IButton, 2);
			AssertParses("b1", MapCode.DPad, MapType.Button, 1);
			AssertParses("a3", MapCode.ButtonB, MapType.Axis, 3);
			AssertParses("p1", MapCode.ButtonA, MapType.POV, 1);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Every kind of control written into any field reads back as the same control")]
		public void Every_control_written_into_any_field_reads_back_the_same()
		{
			var fields = new[] { MapCode.ButtonA, MapCode.LeftTrigger, MapCode.LeftThumbAxisX, MapCode.LeftThumbUp, MapCode.DPad, MapCode.DPadUp };
			var types = Enum.GetValues(typeof(MapType)).Cast<MapType>().Where(x => x != MapType.None);
			foreach (var field in fields)
				foreach (var type in types)
					AssertParses(SettingsConverter.ToIniValue(type, 3, field), field, type, 3);
		}

		[TestMethod, TestCategory("mapping")]
		[Description("A control of the field's own kind keeps the form the native library and older versions read")]
		public void A_control_of_the_fields_own_kind_keeps_the_form_older_readers_know()
		{
			Assert.AreEqual("3", SettingsConverter.ToIniValue(MapType.Button, 3, MapCode.ButtonB));
			Assert.AreEqual("-3", SettingsConverter.ToIniValue(MapType.IButton, 3, MapCode.ButtonB));
			Assert.AreEqual("a3", SettingsConverter.ToIniValue(MapType.Axis, 3, MapCode.RightThumbAxisX));
			Assert.AreEqual("p1", SettingsConverter.ToIniValue(MapType.POV, 1, MapCode.DPad));
			// Only a button away from a button field needs the letter, or it would read back as an axis.
			Assert.AreEqual("b3", SettingsConverter.ToIniValue(MapType.Button, 3, MapCode.RightThumbAxisX));
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("The engine drives a stick from the axis a bare number names")]
		public void The_engine_drives_a_stick_from_the_axis_a_bare_number_names()
		{
			var map = new Map(MapCode.RightThumbAxisX, "3", TargetType.RightThumbX, "", "", "");
			Assert.IsTrue(map.IsAxis, "A bare number on a stick was taken for a " + map.Type + ".");
			Assert.AreEqual(3, map.Index);
		}

		private static void AssertParses(string value, MapType expectedType, int expectedIndex)
		{
			MapType type;
			int index;
			Assert.IsTrue(SettingsConverter.TryParseIniValue(value, out type, out index), $"'{value}' should parse.");
			Assert.AreEqual(expectedType, type, $"'{value}' type");
			Assert.AreEqual(expectedIndex, index, $"'{value}' index");
		}

		private static void AssertParses(string value, MapCode field, MapType expectedType, int expectedIndex)
		{
			MapType type;
			int index;
			Assert.IsTrue(SettingsConverter.TryParseIniValue(value, out type, out index, field), $"'{value}' in {field} should parse.");
			Assert.AreEqual(expectedType, type, $"'{value}' in {field}: type");
			Assert.AreEqual(expectedIndex, index, $"'{value}' in {field}: index");
		}

	}
}
