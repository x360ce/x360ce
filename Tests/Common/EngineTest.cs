// @under-test: Engine/Maps/SettingsConverter.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
			// A button carries no prefix at all - SettingName.SType.Button is "".
			AssertParses("3", MapType.Button, 3);
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
			// "b3" belongs here on purpose: 'b' is not a prefix the parser accepts, because a
			// button is written as a bare number. A value written with 'b' is malformed.
			foreach (var value in new[] { null, "", " ", "x", "a", "a0", "0", "b3", "axis 1", "!!", "a-" })
			{
				MapType type;
				int index;
				// The parser is called for user-edited settings, so bad input must not throw.
				var parsed = SettingsConverter.TryParseIniValue(value, out type, out index);
				Assert.IsFalse(parsed, $"'{value ?? "(null)"}' should not parse.");
			}
		}

		private static void AssertParses(string value, MapType expectedType, int expectedIndex)
		{
			MapType type;
			int index;
			Assert.IsTrue(SettingsConverter.TryParseIniValue(value, out type, out index), $"'{value}' should parse.");
			Assert.AreEqual(expectedType, type, $"'{value}' type");
			Assert.AreEqual(expectedIndex, index, $"'{value}' index");
		}

	}
}
