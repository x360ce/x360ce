// @under-test: Engine/Common/MapExpressionHelp.cs, Engine/Common/MapExpressionUnits.cs, App.v4/Controls/PadControl.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using x360ce.App.Controls;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// Turbo and shift layers are formulas, not settings of their own: [Turbo] in a mapping box's menu writes
	/// the turbo formula Help gives, and a shift layer is two formulas Help gives.
	/// </summary>
	[TestClass]
	public class TurboTest
	{
		static MapExpression Parse(string text)
		{
			MapExpression result;
			string error;
			int position;
			Assert.IsTrue(MapExpression.TryParse(text, out result, out error, out position), text + ": " + error);
			return result;
		}

		/// <summary>Whether a formula presses a button with its buttons and the clock at the given readings.</summary>
		static bool Pressed(MapExpression e, IDictionary<int, float> buttons, float now)
		{
			var values = e.References.Select(r => r.Type == MapExpression.TimeType ? now : buttons[r.Index]).ToArray();
			return MapExpressionUnits.IsPressed(e.Evaluate(values));
		}

		/// <summary>How long each press and each release lasts over a stretch of the clock read every millisecond.</summary>
		static List<int> Runs(MapExpression e, float held, double from, int milliseconds)
		{
			var runs = new List<int>();
			bool? last = null;
			for (var t = 0; t < milliseconds; t++)
			{
				var pressed = Pressed(e, new Dictionary<int, float> { { 1, held } }, MapExpressionUnits.Milliseconds(from + t));
				if (pressed == last)
					runs[runs.Count - 1]++;
				else
					runs.Add(1);
				last = pressed;
			}
			return runs;
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Turbo presses ten times a second while the button is held, and never while it is released")]
		public void Turbo_presses_ten_times_a_second()
		{
			var turbo = Parse(string.Format(MapExpressionHelp.TurboFormula, "b1"));
			Assert.IsTrue(MapExpressionHelp.Examples.Any(x => x.Expression == string.Format(MapExpressionHelp.TurboFormula, "b1")), "Help does not show turbo.");
			var runs = Runs(turbo, 1f, 0, 1000);
			Assert.AreEqual(20, runs.Count, "A second held is not ten presses and ten releases.");
			Assert.IsTrue(runs.All(x => x == 50), "Presses and releases are not fifty milliseconds each: " + string.Join(", ", runs));
			Assert.AreEqual(1, Runs(turbo, 0f, 0, 1000).Count, "A released button pressed something.");
			Assert.IsFalse(Pressed(turbo, new Dictionary<int, float> { { 1, 0f } }, 60f), "A released button pressed something.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Turbo keeps its step after days of running and across the clock's four-hourly restart")]
		public void Turbo_keeps_its_step_after_days_of_running()
		{
			var turbo = Parse(string.Format(MapExpressionHelp.TurboFormula, "b1"));
			const double day = 24 * 60 * 60 * 1000.0;
			foreach (var from in new[] { 5 * day, MapExpressionUnits.ClockCycle - 500, 30 * day + MapExpressionUnits.ClockCycle - 500 })
			{
				var runs = Runs(turbo, 1f, from, 1000);
				Assert.IsTrue(runs.Skip(1).Take(runs.Count - 2).All(x => x == 50),
					string.Format("From {0} ms the presses are not fifty milliseconds each: {1}", from, string.Join(", ", runs)));
			}
			Assert.AreEqual(1000f, MapExpressionUnits.Milliseconds(MapExpressionUnits.ClockCycle * 30 + 1000), "The clock is not exact after thirty restarts.");
			Assert.AreEqual(0.0, MapExpressionUnits.ClockCycle % (60 * 60 * 1000), "The clock restarts part way through an hour.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("[Turbo] turns a box's button into the turbo formula, the right way round, and leaves other boxes alone")]
		public void Turbo_menu_writes_the_formula()
		{
			Assert.AreEqual("=b3*floor(now%100/50)", PadControl.TurboText("Button 3"));
			Assert.AreEqual("=(1-b3)*floor(now%100/50)", PadControl.TurboText("IButton 3"));
			foreach (var text in new[] { "Axis 1", "Slider 1", "POV 1 Up", "=b1*b2", "", null })
				Assert.IsNull(PadControl.TurboText(text), "'" + (text ?? "(null)") + "' maps no button to turbo.");
			var inverted = Parse(PadControl.TurboText("IButton 3"));
			Assert.IsFalse(Pressed(inverted, new Dictionary<int, float> { { 3, 1f } }, 60f), "An inverted button pressed while held.");
			Assert.IsTrue(Pressed(inverted, new Dictionary<int, float> { { 3, 0f } }, 60f), "An inverted button did not press while released.");
		}

		[TestMethod, TestCategory("mapping")]
		[Description("The shift layer presses exactly one of its two buttons while the button is held")]
		public void Shift_layer_presses_one_button_at_a_time()
		{
			var alone = Parse(MapExpressionHelp.Examples.Single(x => x.Goal.StartsWith("Shift layer: the button alone")).Expression);
			var shifted = Parse(MapExpressionHelp.Examples.Single(x => x.Goal.StartsWith("Shift layer: the same button")).Expression);
			foreach (var held in new[] { 0f, 1f })
			{
				foreach (var shift in new[] { 0f, 1f })
				{
					var buttons = new Dictionary<int, float> { { 1, held }, { 5, shift } };
					Assert.AreEqual(held == 1f && shift == 0f, Pressed(alone, buttons, 0f), string.Format("Unshifted is wrong for button {0}, shift {1}.", held, shift));
					Assert.AreEqual(held == 1f && shift == 1f, Pressed(shifted, buttons, 0f), string.Format("Shifted is wrong for button {0}, shift {1}.", held, shift));
				}
			}
		}
	}
}
