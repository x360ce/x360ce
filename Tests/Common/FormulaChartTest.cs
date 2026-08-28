// @under-test: Engine/Common/MapExpressionUnits.cs, App.v4/Controls/AxisMapUserControl.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// The line on a mapping page, when a formula decides the row.
	/// </summary>
	/// <remarks>
	/// The line used to be drawn from the dead zone, anti dead zone and sensitivity. A row driven by a
	/// formula ignores all three, so the line described something the row no longer did - and the live
	/// point stopped moving entirely, because a formula names no single control and the drawing was
	/// skipped for want of a source number.
	///
	/// Both now go through the same call the device loop makes, so the line, the point, and what a
	/// game receives cannot describe different things.
	/// </remarks>
	[TestClass]
	public class FormulaChartTest
	{

		static MapExpression Parse(string text)
		{
			MapExpression expression;
			string error;
			int position;
			Assert.IsTrue(MapExpression.TryParse(text, out expression, out error, out position),
				"The formula " + text + " did not parse: " + error);
			return expression;
		}

		/// <summary>What the chart plots for one reading of the swept control.</summary>
		static int TriggerAt(MapExpression expression, int raw)
		{
			var swept = MapExpressionUnits.GetSweptSource(expression);
			Assert.IsTrue(swept.HasValue, "The formula names no control to sweep.");
			var values = new float[MapExpression.MaxReferences];
			float result;
			Assert.IsTrue(MapExpressionUnits.TrySweep(expression, swept.Value, raw, false, values, out result));
			return MapExpressionUnits.ToTrigger(result);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Doubling a trigger formula doubles the line")]
		public void Doubling_a_trigger_formula_doubles_the_line()
		{
			// What a person sees: =a5*2 climbs twice as fast across the same travel.
			var plain = Parse("=a5");
			var doubled = Parse("=a5*2");
			// A quarter of the way along, where neither has run out of room yet.
			var raw = ushort.MaxValue / 4;
			var one = TriggerAt(plain, raw);
			var two = TriggerAt(doubled, raw);
			Assert.AreEqual(one * 2, two, 2,
				"At the same reading the doubled formula must be twice as high. Plain gave " + one
				+ " and doubled gave " + two + ".");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A doubled trigger formula reaches the top half way along")]
		public void A_doubled_trigger_formula_reaches_the_top_half_way_along()
		{
			// The other half of what a person sees: the line meets the top border earlier and stays
			// there, rather than climbing all the way to the far edge.
			var doubled = Parse("=a5*2");
			var half = ushort.MaxValue / 2;
			Assert.AreEqual(byte.MaxValue, TriggerAt(doubled, half),
				"Half way along, twice the travel is the whole of it, so the line has to be at the " +
				"top.");
			Assert.AreEqual(byte.MaxValue, TriggerAt(doubled, ushort.MaxValue),
				"Past that it must stay at the top rather than wrapping or falling back.");
			Assert.IsTrue(TriggerAt(doubled, ushort.MaxValue / 8) < byte.MaxValue,
				"An eighth of the way along it is not at the top yet, or the line is not a slope at " +
				"all.");
		}

		[TestMethod, TestCategory("mapping")]
		[Description("A formula of buttons alone offers no line to draw")]
		public void A_formula_of_buttons_alone_offers_no_line_to_draw()
		{
			// A button has two readings rather than a travel, so there is nothing to sweep. Saying so
			// is what keeps the chart from drawing a flat line and calling it a mapping.
			Assert.IsFalse(MapExpressionUnits.GetSweptSource(Parse("=b1")).HasValue);
			Assert.IsTrue(MapExpressionUnits.GetSweptSource(Parse("=b1+a5")).HasValue,
				"A formula naming an axis as well does have a travel to show.");
		}

	}
}
