// @under-test: App.v4/Controls/PadControl.cs, Engine/Common/MapExpressionHelp.cs, Engine/Common/MapExpressionUnits.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using x360ce.App.Controls;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// [Round Dead Zone] in a stick box's menu writes one formula into the stick's X box and one into its Y box, so the
	/// stick reads nothing inside a circle round its middle and its full travel outside it.
	/// </summary>
	[TestClass]
	public class RoundDeadZoneTest
	{
		static MapExpression Parse(string text)
		{
			MapExpression result;
			string error;
			int position;
			Assert.IsTrue(MapExpression.TryParse(text, out result, out error, out position), text + ": " + error);
			return result;
		}

		/// <summary>What a stick formula gives the game, with the stick's axes at x and y in formula units, -1 to 1.</summary>
		static short Thumb(string formula, float x, float y)
		{
			var e = Parse(formula);
			var values = e.References.Select(r => r.Index == 1 ? x : y).ToArray();
			return MapExpressionUnits.ToThumb(e.Evaluate(values));
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("The formulas keep each box's axis and its way round, and Help shows them")]
		public void The_formulas_keep_each_axis_and_its_way_round()
		{
			var texts = PadControl.RoundDeadZoneTexts("Axis 1", "IAxis 2", 0.2);
			Assert.AreEqual("=a1*(1-0.2/max(sqrt(a1^2+a2^2),0.2))/0.8", texts[0]);
			Assert.AreEqual("=-a2*(1-0.2/max(sqrt(a1^2+a2^2),0.2))/0.8", texts[1]);
			Assert.IsTrue(MapExpressionHelp.Examples.Any(x => x.Expression == texts[0]), "Help does not show the X formula.");
			Assert.IsTrue(MapExpressionHelp.Examples.Any(x => x.Expression == texts[1]), "Help does not show the Y formula.");
			var again = PadControl.RoundDeadZoneTexts(texts[0], texts[1], 0.15);
			Assert.AreEqual("=a1*(1-0.15/max(sqrt(a1^2+a2^2),0.15))/0.85", again[0], "Picked again, only the size changes.");
			Assert.AreEqual("=-a2*(1-0.15/max(sqrt(a1^2+a2^2),0.15))/0.85", again[1], "Picked again, only the size changes.");
			var right = PadControl.RoundDeadZoneTexts("=-a4", "Axis 5", 0.2);
			Assert.AreEqual("=-a4*(1-0.2/max(sqrt(a4^2+a5^2),0.2))/0.8", right[0]);
			Assert.AreEqual("=a5*(1-0.2/max(sqrt(a4^2+a5^2),0.2))/0.8", right[1]);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Inside the circle the stick reads nothing in every direction; at the edge it reaches all the way")]
		public void Inside_the_circle_nothing_and_at_the_edge_all_the_way()
		{
			var texts = PadControl.RoundDeadZoneTexts("Axis 1", "Axis 2", 0.2);
			// A diagonal just inside the circle, where a square dead zone of 20 % per axis would already let it move.
			var d = 0.19f / (float)Math.Sqrt(2);
			Assert.AreEqual(0, Thumb(texts[0], d, d));
			Assert.AreEqual(0, Thumb(texts[1], d, d));
			// Straight along one axis, just past the circle: moving, and in the direction pushed.
			Assert.IsTrue(Thumb(texts[0], 0.25f, 0f) > 0, "Past the circle the stick does not move.");
			Assert.IsTrue(Thumb(texts[0], -0.25f, 0f) < 0, "Past the circle the stick moves the wrong way.");
			Assert.AreEqual(0, Thumb(texts[1], 0.25f, 0f), "Pushed along X, the Y box moves.");
			// At the edge, and in a corner, the stick reaches all the way.
			Assert.AreEqual(short.MaxValue, Thumb(texts[0], 1f, 0f));
			Assert.AreEqual(short.MinValue, Thumb(texts[0], -1f, 0f));
			Assert.AreEqual(short.MaxValue, Thumb(texts[0], 1f, 1f), "A corner is not all the way.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A box with no axis, or a size outside 0 to 1, gets nothing")]
		public void Only_two_axes_get_a_round_dead_zone()
		{
			foreach (var text in new[] { "Slider 1", "Button 3", "POV 1 Up", "=a1*2", "=a1-0.05", "HAxis 1", "", null })
				Assert.IsNull(PadControl.RoundDeadZoneTexts(text, "Axis 2", 0.2), "'" + (text ?? "(null)") + "' maps no stick axis.");
			Assert.IsNull(PadControl.RoundDeadZoneTexts("Axis 1", "Axis 2", 0));
			Assert.IsNull(PadControl.RoundDeadZoneTexts("Axis 1", "Axis 2", 1));
		}
	}
}
