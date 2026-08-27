// @under-test: Engine/Common/MapExpression.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// Expressions arrive from a shared database, so their author is a stranger and may be hostile.
	/// These tests pin what the grammar accepts, and more importantly what it refuses.
	/// </summary>
	[TestClass]
	public class MapExpressionTest
	{

		#region Helpers

		private static MapExpression Parse(string text)
		{
			MapExpression result;
			string error;
			int position;
			Assert.IsTrue(MapExpression.TryParse(text, out result, out error, out position),
				string.Format("'{0}' should have parsed but was refused: {1}", text, error));
			return result;
		}

		private static void Refuses(string text)
		{
			MapExpression result;
			string error;
			int position;
			var parsed = MapExpression.TryParse(text, out result, out error, out position);
			Assert.IsFalse(parsed, string.Format("'{0}' should have been refused but was accepted.", text));
			Assert.IsNull(result, string.Format("'{0}' was refused but still produced an expression.", text));
			Assert.IsFalse(string.IsNullOrEmpty(error),
				string.Format("'{0}' was refused without saying why.", text));
		}

		private static float Value(string text, params float[] values)
		{
			return Parse(text).Evaluate(values);
		}

		#endregion

		#region Grammar

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Arithmetic evaluates with the precedence a person expects")]
		public void Arithmetic_follows_ordinary_precedence()
		{
			Assert.AreEqual(14f, Value("=2+3*4"));
			Assert.AreEqual(20f, Value("=(2+3)*4"));
			Assert.AreEqual(-6f, Value("=2*-3"));
			Assert.AreEqual(5f, Value("=3--2"));
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Subtraction and division group to the left, which is where they are silently wrong")]
		public void Subtraction_and_division_group_to_the_left()
		{
			// Recursing right instead of looping gives 9 and 20 here, and never raises an error.
			Assert.AreEqual(3f, Value("=10-4-3"));
			Assert.AreEqual(5f, Value("=100/10/2"));
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Raising to a power groups right and is negated after, both silently wrong if not")]
		public void Power_groups_right_and_binds_tighter_than_multiplying()
		{
			Assert.AreEqual(9f, Value("=3^2"));
			// Right, so 2^9 and not 8^2. Grouping left gives 64 and never raises an error.
			Assert.AreEqual(512f, Value("=2^3^2"));
			// Negation applies to the result. Reading it as (-2)^2 gives 4, also silently.
			Assert.AreEqual(-4f, Value("=-2^2"));
			// Tighter than multiplying, so 2*(3^2) and not (2*3)^2, which would be 36.
			Assert.AreEqual(18f, Value("=2*3^2"));
			// A negative exponent is a term of its own.
			Assert.AreEqual(0.5f, Value("=2^-1"));
			Assert.AreEqual(0.25f, Value("=a1^2", 0.5f));
		}

		[TestMethod, TestCategory("mapping")]
		[Description("Remainder keeps the sign of the value being divided")]
		public void Remainder_keeps_the_sign_of_the_left_value()
		{
			Assert.AreEqual(0.1f, Value("=0.7%0.3"), 0.0001f);
			Assert.AreEqual(-0.1f, Value("=-0.7%0.3"), 0.0001f);
			Assert.AreEqual(0f, Value("=1%0"));   // not a number, so it rests at zero
		}

		[TestMethod, TestCategory("mapping")]
		[Description("The added functions compute what their names say")]
		public void The_added_functions_compute_what_they_claim()
		{
			Assert.AreEqual(3f, Value("=ceil(2.1)"));
			Assert.AreEqual(1f, Value("=exp(0)"));
			Assert.AreEqual(3f, Value("=log(8,2)"), 0.0001f);
			// Inverse trigonometry answers in degrees, matching sin, cos and tan taking degrees.
			Assert.AreEqual(90f, Value("=asin(1)"), 0.0001f);
			Assert.AreEqual(0f, Value("=acos(1)"), 0.0001f);
			Assert.AreEqual(45f, Value("=atan(1)"), 0.0001f);
			// Out of range for the inverse functions, so no real answer exists.
			Assert.AreEqual(0f, Value("=asin(2)"));
		}

		[TestMethod, TestCategory("mapping")]
		[Description("Buttons are 0 or 1, so ordinary arithmetic already does and, or and not")]
		public void Button_logic_works_without_logical_operators()
		{
			// and
			Assert.AreEqual(1f, Value("=b1*b2", 1f, 1f));
			Assert.AreEqual(0f, Value("=b1*b2", 1f, 0f));
			// or
			Assert.AreEqual(1f, Value("=max(b1,b2)", 1f, 0f));
			Assert.AreEqual(0f, Value("=max(b1,b2)", 0f, 0f));
			// not
			Assert.AreEqual(0f, Value("=1-b1", 1f));
			Assert.AreEqual(1f, Value("=1-b1", 0f));
			// either but not both
			Assert.AreEqual(1f, Value("=abs(b1-b2)", 1f, 0f));
			Assert.AreEqual(0f, Value("=abs(b1-b2)", 1f, 1f));
			// gating an axis on a button
			Assert.AreEqual(0.5f, Value("=a1*b1", 0.5f, 1f));
			Assert.AreEqual(0f, Value("=a1*b1", 0.5f, 0f));
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Factorial stays absent, because its cost grows without bound from a short input")]
		public void Factorial_is_refused()
		{
			// The one arithmetic operation whose cost is unbounded from a few characters. Another
			// expression library carried it and earned a published advisory. Nothing here needs it:
			// no worked example uses it, and sources are fractions between -1 and 1 where it has no
			// ordinary meaning at all.
			Refuses("=3!");
			Refuses("=170!");
			Refuses("=99999999999999!");
			Refuses("=a1!");
			Refuses("=!a1");
		}

		[TestMethod, TestCategory("mapping")]
		[Description("A source is read by the letter and number that stored mappings use")]
		public void Sources_are_read_by_letter_and_number()
		{
			Assert.AreEqual(1.5f, Value("=a1*3", 0.5f));
			Assert.AreEqual(0.75f, Value("=a1+a2", 0.5f, 0.25f));
			var e = Parse("=a1+b2");
			Assert.AreEqual(2, e.References.Count);
			Assert.AreEqual("a1", e.References[0].ToString());
			Assert.AreEqual("b2", e.References[1].ToString());
		}

		[TestMethod, TestCategory("mapping")]
		[Description("The same source named twice occupies one slot, not two")]
		public void A_repeated_source_takes_one_slot()
		{
			var e = Parse("=a1*abs(a1)");
			Assert.AreEqual(1, e.References.Count);
			Assert.AreEqual(-0.25f, e.Evaluate(new[] { -0.5f }));
		}

		[TestMethod, TestCategory("mapping")]
		[Description("Every documented function computes what its name says")]
		public void Functions_compute_what_they_claim()
		{
			Assert.AreEqual(2f, Value("=abs(0-2)"));
			Assert.AreEqual(-1f, Value("=sign(0-7)"));
			Assert.AreEqual(3f, Value("=sqrt(9)"));
			Assert.AreEqual(8f, Value("=pow(2,3)"));
			Assert.AreEqual(2f, Value("=min(2,5)"));
			Assert.AreEqual(5f, Value("=max(2,5)"));
			Assert.AreEqual(5f, Value("=clamp(9,0,5)"));
			Assert.AreEqual(2f, Value("=floor(2.7)"));
			// Angles are degrees, so a quarter turn is 90. If this ever becomes radians, this test says so.
			Assert.AreEqual(1f, Value("=sin(90)"), 0.0001f);
			Assert.AreEqual(1f, Value("=cos(0)"), 0.0001f);
			Assert.AreEqual(1f, Value("=tan(45)"), 0.0001f);
			// Rounding sends a half to the even neighbour, so this is 2 and not 3. It is documented
			// because it is the result people report as a fault.
			Assert.AreEqual(2f, Value("=round(2.5)"));
			Assert.AreEqual(4f, Value("=round(3.5)"));
		}

		[TestMethod, TestCategory("mapping")]
		[Description("The worked examples in the documents actually produce what they promise")]
		public void Documented_examples_behave_as_documented()
		{
			// Fine control near centre, full travel at the edge, direction preserved.
			Assert.AreEqual(0.25f, Value("=a1*abs(a1)", 0.5f));
			Assert.AreEqual(-0.25f, Value("=a1*abs(a1)", -0.5f));
			Assert.AreEqual(1f, Value("=a1*abs(a1)", 1f));
			// Split one pedal axis into an accelerator and a brake.
			Assert.AreEqual(0.6f, Value("=max(a1,0)", 0.6f));
			Assert.AreEqual(0f, Value("=max(a1,0)", -0.6f));
			Assert.AreEqual(0.6f, Value("=0-min(a1,0)", -0.6f));
			// Walk slowly, run when the trigger is held.
			Assert.AreEqual(0.5f, Value("=a1*(0.5+a2*0.5)", 1f, 0f));
			Assert.AreEqual(1f, Value("=a1*(0.5+a2*0.5)", 1f, 1f));
		}

		#endregion

		#region Numbers that mislead

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A decimal point means the same thing in every language Windows runs in")]
		public void Decimal_point_does_not_depend_on_the_users_language()
		{
			var original = Thread.CurrentThread.CurrentCulture;
			try
			{
				foreach (var name in new[] { "en-US", "de-DE", "fr-FR", "lt-LT", "tr-TR", "ru-RU" })
				{
					Thread.CurrentThread.CurrentCulture = new CultureInfo(name);
					Assert.AreEqual(3.25f, Value("=1.5*2+0.25"),
						string.Format("Wrong result under {0}.", name));
					// A grouped number is refused rather than silently read as one thousand, which is
					// what the wider parse styles do without ever raising an error.
					// A grouped number has to be refused where a number is genuinely expected. Written
					// bare, the comma is read as an argument separator and never reaches the number
					// parser at all, so it would pin nothing.
					Refuses("=abs(1,000)");
					Assert.AreEqual(1000f, Value("=1000"),
						string.Format("A plain thousand stopped working under {0}.", name));
					// Function names are matched without the casing rules of the current language,
					// where an upper-case I does not fold back to the letter the table holds.
					// "min" is the case that matters: under Turkish an upper-case I does not fold back to
					// the letter the table holds, so MIN stops being found while ABS still works.
					Assert.AreEqual(2f, Value("=MIN(2,5)"),
						string.Format("MIN was not matched under {0}.", name));
					Assert.AreEqual(1f, Value("=sIn(90)"), 0.0001f,
						string.Format("sIn was not matched under {0}.", name));
				}
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = original;
			}
		}

		[TestMethod, TestCategory("mapping")]
		[Description("A result that is not a real number reaches the pad as a resting value")]
		public void Results_that_are_not_numbers_become_zero()
		{
			Assert.AreEqual(0f, Value("=1/0"));
			Assert.AreEqual(0f, Value("=0/0"));
			Assert.AreEqual(0f, Value("=sqrt(0-1)"));
			Assert.AreEqual(0f, Value("=pow(10,999)"));
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Nothing thrown by a function escapes onto the thread that feeds the pad")]
		public void No_function_throws_out_of_evaluate()
		{
			// Math.Sign refuses a value that is not a number by throwing, which left Evaluate entirely
			// and past the sanitiser. On the polling thread that ends the feed to the pad, so a player
			// loses their controller mid-game and nothing says why.
			Assert.AreEqual(0f, Value("=sign(0/0)"));
			Assert.AreEqual(0f, Value("=sign(a1)", float.NaN));
			Assert.AreEqual(1f, Value("=sign(a1)", 0.5f));
			Assert.AreEqual(-1f, Value("=sign(a1)", -0.5f));
			Assert.AreEqual(0f, Value("=sign(a1)", 0f));
			// Every function, fed the values most likely to upset it.
			foreach (var name in MapExpression.FunctionArity)
			{
				var args = string.Join(",", System.Linq.Enumerable.Repeat("a1", name.Value).ToArray());
				var text = "=" + name.Key + "(" + args + ")";
				// A few functions cannot be called at all inside sixteen characters. They are still
				// part of the language and are checked again as soon as the column is widened.
				if (text.Length > MapExpression.MaxLength)
					continue;
				foreach (var awkward in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, 0f, -1f, float.MaxValue })
				{
					var values = new float[1];
					values[0] = awkward;
					var e = Parse(text);
					var result = e.Evaluate(values);
					Assert.IsFalse(float.IsNaN(result) || float.IsInfinity(result),
						string.Format("'{0}' with {1} produced {2}.", text, awkward, result));
				}
			}
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Too few values, or none at all, gives a resting result rather than an exception")]
		public void A_short_or_missing_values_array_does_not_throw()
		{
			// The caller runs on the thread feeding the pad. A device that loses a control mid-session
			// would otherwise take the whole feed down with it.
			var e = Parse("=a1+a2");
			Assert.AreEqual(0f, e.Evaluate(null));
			Assert.AreEqual(0f, e.Evaluate(new float[0]));
			Assert.AreEqual(0f, e.Evaluate(new float[1]));
			Assert.AreEqual(0.75f, e.Evaluate(new[] { 0.5f, 0.25f }));
		}

		[TestMethod, TestCategory("mapping")]
		[Description("A caller cannot change the sources an expression was compiled against")]
		public void The_source_list_cannot_be_changed_from_outside()
		{
			// The compiled tree reads slots by position. Letting the list be edited would leave the two
			// disagreeing, and every value after that would come from the wrong control.
			var e = Parse("=a1+a2");
			try
			{
				e.References.Clear();
				Assert.Fail("The source list was changed from outside the expression.");
			}
			catch (NotSupportedException)
			{
			}
			Assert.AreEqual(2, e.References.Count);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Limits written the wrong way round hold a value, never pin it to one number")]
		public void Clamp_with_reversed_limits_still_holds_the_value()
		{
			// Written the wrong way round this used to return the same number for every input, so a
			// control appeared dead while the expression looked correct.
			Assert.AreEqual(0.5f, Value("=clamp(a1,1,0)", 0.5f));
			Assert.AreEqual(0.5f, Value("=clamp(a1,0,1)", 0.5f));
			Assert.AreEqual(1f, Value("=clamp(a1,1,0)", 5f));
			Assert.AreEqual(0f, Value("=clamp(a1,1,0)", -5f));
			// Two different inputs must not produce the same output.
			Assert.AreNotEqual(Value("=clamp(a1,1,0)", 0.2f), Value("=clamp(a1,1,0)", 0.8f));
		}

		[TestMethod, TestCategory("mapping")]
		[Description("A function name is followed by its bracket, with nothing in between")]
		public void A_space_before_the_bracket_is_not_a_function_call()
		{
			Refuses("=abs (1)");
			Refuses("=min (1,2)");
			Assert.AreEqual(1f, Value("=abs(1)"), "The ordinary spelling must still work.");
			// Space elsewhere is still fine, because it separates rather than joins.
			Assert.AreEqual(3f, Value("=1 + 2"));
			Assert.AreEqual(2f, Value("=abs( 0 - 2 )"));
		}

		#endregion

		#region Refusals

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Text that is not a complete expression is refused, with a reason")]
		public void Incomplete_expressions_are_refused()
		{
			Refuses("=");
			Refuses("=1+");
			Refuses("=*2");
			Refuses("=1++2");
			Refuses("=(1");
			Refuses("=1)");
			Refuses("=a1 a2");
			Refuses("=min(1)");
			Refuses("=min(1,2,3)");
			Refuses("=nope(1)");
			Refuses("=z1");
			Refuses("=a0");
			Refuses("=a");
			Refuses("=1..2");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A refusal says where the trouble is, and points at the right place")]
		public void A_refusal_points_at_the_place_that_is_wrong()
		{
			// The position is shown to a person editing the field. Pointing at the wrong character is
			// worse than pointing nowhere, because they will look there and find nothing wrong.
			At("=a1&a2", 3, "the character that has no meaning");
			At("=1+2)", 4, "the closing bracket that has no opening one");
			// An unfinished bracket points at the bracket still waiting, not at the end of the text.
			At("=abs(1+2", 4, "the bracket that was never closed");
			At("=((1)", 1, "the outer bracket that was never closed");
		}

		private static void At(string text, int expected, string what)
		{
			MapExpression result;
			string error;
			int position;
			Assert.IsFalse(MapExpression.TryParse(text, out result, out error, out position),
				string.Format("'{0}' was accepted.", text));
			Assert.AreEqual(expected, position,
				string.Format("'{0}' should point at {1} (position {2}), not {3}.", text, what, expected, position));
		}

		[TestMethod, TestCategory("mapping")]
		[Description("A plain mapping value is not treated as an expression")]
		public void A_plain_mapping_value_is_not_an_expression()
		{
			Assert.IsFalse(MapExpression.IsExpression("a1"));
			Assert.IsFalse(MapExpression.IsExpression("3"));
			Assert.IsFalse(MapExpression.IsExpression(""));
			Assert.IsFalse(MapExpression.IsExpression(null));
			Assert.IsTrue(MapExpression.IsExpression("=a1*2"));
		}

		#endregion

		#region Hostile input

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Nothing in the grammar can name a type, reach a member, or call anything of its own")]
		public void Attempts_to_reach_outside_arithmetic_are_refused()
		{
			// Every one of these was accepted by at least one published expression library.
			Refuses("=a.b");
			Refuses("=1.GetType()");
			Refuses("=\"a\".GetType()");
			Refuses("=a1.GetType().Assembly");
			Refuses("=System.Environment.Exit(0)");
			Refuses("=System.Diagnostics.Process.Start('calc')");
			Refuses("=System.IO.File.WriteAllText('x','y')");
			Refuses("=typeof(int)");
			Refuses("=new System.Object()");
			Refuses("=Activator.CreateInstance(1)");
			Refuses("=a1[0]");
			Refuses("=a1{0}");
			Refuses("=a1;a2");
			Refuses("=a1=2");
			Refuses("=a1=>a2");
			Refuses("=$a1");
			Refuses("=@a1");
			Refuses("=#a1");
			Refuses("=a1|a2");
			Refuses("=a1&a2");
			Refuses("=a1?a2:0");
			Refuses("=`a1`");
			Refuses("=a1\\a2");
			Refuses("='abs'(1)");
			Refuses("=${a1}");
			Refuses("=a1//comment");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A function name spelled with lookalike letters is not the function")]
		public void Lookalike_letters_do_not_match_a_function()
		{
			// Cyrillic a and e. A tokenizer that accepts any Unicode letter would match these.
			Refuses("=аbs(1)");
			Refuses("=а1*2");
			Refuses("=abs (1)");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("An oversized or deeply nested expression is refused, never left to end the process")]
		public void Oversized_expressions_are_refused_before_they_are_parsed()
		{
			// A stack overflow cannot be caught. If any of these were parsed instead of refused, this
			// test would not fail: the whole test run would disappear without a message.
			Refuses("=" + new string('(', 5000) + "1" + new string(')', 5000));
			Refuses("=" + new string('-', 5000) + "1");
			Refuses("=" + string.Join("+", new string[2000]).Replace("+", "1+") + "1");
			Refuses("=" + new string('a', 1000));
			var deep = "=1";
			for (int i = 0; i < 40; i++)
				deep = "=abs(" + deep.Substring(1) + ")";
			Refuses(deep);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("The depth cap counts every way of nesting, not only brackets")]
		public void The_depth_cap_covers_nesting_without_brackets()
		{
			// Brackets are not the only thing that recurses. A run of minus signs descends once per sign
			// and opens no bracket at all, and that shape is what was measured killing another parser.
			// Relying on the length cap to bound it would tie two limits together silently: raising the
			// length would reintroduce the crash with nothing to say so.
			Refuses("=" + new string('-', MapExpression.MaxDepth + 1) + "1");
			// While a mapping is stored in sixteen characters the length refuses a deep expression
			// before the depth can. Both caps are real and both are kept; the tighter one speaks
			// first. This says which, so that widening the column cannot quietly remove the guard.
			var deepest = "=" + new string('-', MapExpression.MaxDepth - 1) + "1";
			if (deepest.Length > MapExpression.MaxLength)
				Assert.IsTrue(MapExpression.MaxDepth + 2 > MapExpression.MaxLength,
					"Depth is reachable again; go back to checking one under the cap parses.");
			else
				Assert.AreEqual(-1f, Value(deepest),
					"One sign inside the cap should still work.");
			// Nesting through function calls descends the same way.
			var nested = "1";
			for (int i = 0; i < MapExpression.MaxDepth + 1; i++)
				nested = "abs(" + nested + ")";
			Refuses("=" + nested);
		}

		[TestMethod, TestCategory("mapping")]
		[Description("The limits are the documented ones, and sit just under what is refused")]
		public void The_limits_are_where_they_are_documented_to_be()
		{
			// Named outright. Deriving every case from the constants means the test passes for any
			// value of them, including a cap so large it protects nothing.
			Assert.AreEqual(16, MapExpression.MaxLength, "The length cap moved; say so deliberately.");
			Assert.AreEqual(16, MapExpression.MaxDepth, "The nesting cap moved; say so deliberately.");
			Assert.AreEqual(128, MapExpression.MaxNodes, "The node cap moved; say so deliberately.");
			Assert.AreEqual(8, MapExpression.MaxReferences, "The source cap moved; say so deliberately.");
			// Fixed text, so removing a cap fails here rather than quietly widening the test with it.
			Refuses("=" + new string('(', 17) + "1" + new string(')', 17));
			Refuses("=" + new string('1', 16));
			// Nine sources need more room than a mapping is stored in, so the length refuses this
			// before the source count can. Both caps are real; the tighter one simply speaks first.
			Refuses("=a1+a2+a3+a4+a5+a6+a7+a8+a9");
			Assert.IsTrue("=a1+a2+a3+a4+a5+a6+a7+a8+a9".Length > MapExpression.MaxLength,
				"If this ever fits, the source cap must be the one refusing it.");
			// Depth: one under the cap parses, one over does not. Both are only askable while the
			// deeper of the two fits in what a mapping is stored in; below that the length refuses
			// first. Which cap is doing the work is stated, so widening the column cannot quietly
			// leave the depth guard untested.
			var atCap = "=" + new string('(', MapExpression.MaxDepth) + "1" + new string(')', MapExpression.MaxDepth);
			if (atCap.Length <= MapExpression.MaxLength)
				Assert.IsNotNull(Parse(atCap));
			else
				Assert.IsTrue(MapExpression.MaxDepth * 2 + 2 > MapExpression.MaxLength,
					"Depth is reachable again; go back to checking one under the cap parses.");
			Refuses("=" + new string('(', MapExpression.MaxDepth + 1) + "1" + new string(')', MapExpression.MaxDepth + 1));
			// Length: exactly at the cap parses, one over does not.
			Refuses("=" + new string('1', MapExpression.MaxLength));
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Nothing is compiled until every check has passed")]
		public void Refused_text_never_reaches_the_compiler()
		{
			// Compilation is the only expensive and irreversible step. A refusal must cost far less
			// than an acceptance, which it cannot if the text was compiled before being judged.
			var hostile = new[]
			{
				"=System.Diagnostics.Process.Start('calc')",
				"=" + new string('(', 5000) + "1" + new string(')', 5000),
				"=a1[0]",
				"=nope(1)",
			};
			var refusing = Stopwatch.StartNew();
			for (int i = 0; i < 200; i++)
				foreach (var text in hostile)
				{
					MapExpression r; string e; int p;
					Assert.IsFalse(MapExpression.TryParse(text, out r, out e, out p));
				}
			refusing.Stop();
			var accepting = Stopwatch.StartNew();
			for (int i = 0; i < 200; i++)
			{
				MapExpression r; string e; int p;
				Assert.IsTrue(MapExpression.TryParse("=a1*abs(a1)", out r, out e, out p));
			}
			accepting.Stop();
			Assert.IsTrue(refusing.ElapsedMilliseconds < accepting.ElapsedMilliseconds,
				string.Format("Refusing {0} hostile values took {1} ms, longer than compiling {2} good ones ({3} ms), so text is reaching the compiler before it is judged.",
					hostile.Length * 200, refusing.ElapsedMilliseconds, 200, accepting.ElapsedMilliseconds));
		}

		#endregion

		#region Cost

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Evaluating allocates nothing, so polling adds no work for the collector")]
		public void Evaluating_allocates_nothing()
		{
			// Without optimisation the compiler keeps locals rooted and this measurement means nothing.
			var debuggable = (DebuggableAttribute)Attribute.GetCustomAttribute(
				typeof(MapExpressionTest).Assembly, typeof(DebuggableAttribute));
			Assert.IsFalse(debuggable != null && debuggable.IsJITOptimizerDisabled,
				"Allocation cannot be measured in an unoptimised build.");

			AppDomain.MonitoringIsEnabled = true;
			var e = Parse("=a1*abs(a1)");
			var values = new[] { 0.5f, 0.25f };
			for (int i = 0; i < 10000; i++) e.Evaluate(values);   // warm up and settle one-time costs

			var before = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;
			for (int i = 0; i < 200000; i++) e.Evaluate(values);
			var perEvaluation = (AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize - before) / 200000.0;

			// Proof the instrument can see allocation at all, so a zero above means zero and not a
			// measurement that never worked.
			var control = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;
			object sink = null;
			for (int i = 0; i < 200000; i++) sink = (object)(float)i;
			GC.KeepAlive(sink);
			var controlPer = (AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize - control) / 200000.0;

			Assert.IsTrue(controlPer > 1.0,
				string.Format("The allocation measurement is not working: boxing 200,000 values recorded {0:F3} bytes each.", controlPer));
			// A single small object per evaluation is the smallest fault worth catching, so prove the
			// measurement resolves one before trusting a zero.
			var smallest = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;
			object one = null;
			for (int i = 0; i < 200000; i++) one = new object();
			GC.KeepAlive(one);
			var smallestPer = (AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize - smallest) / 200000.0;
			Assert.IsTrue(smallestPer > 1.0,
				string.Format("The measurement cannot resolve one small object per call: {0:F3} bytes.", smallestPer));
			Assert.IsTrue(perEvaluation < 1.0,
				string.Format("Evaluation allocated {0:F3} bytes each, so it is doing work the polling loop should not.", perEvaluation));
		}

		[TestMethod, TestCategory("mapping"), TestCategory("stress")]
		[Description("A compiled expression is released once nothing refers to it")]
		public void A_compiled_expression_is_released_when_dropped()
		{
			var reference = MakeCollectable();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			Assert.IsFalse(reference.IsAlive,
				"A compiled expression stayed alive after nothing referred to it, so a device that reconnects repeatedly would grow.");
		}

		// Built in its own frame that returns only the weak reference, because a local in the calling
		// frame keeps the object alive for the whole method and the result would mean nothing.
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static WeakReference MakeCollectable()
		{
			return new WeakReference(Parse("=a1*abs(a1)"));
		}

		[TestMethod, TestCategory("mapping"), TestCategory("stress")]
		[Description("Reports the cost of parsing and of evaluating, without asserting a speed")]
		public void Reports_what_parsing_and_evaluating_cost()
		{
			// Timing is never asserted: it encodes the speed of whichever machine happens to run it.
			// These numbers are recorded so a later change that makes evaluation expensive is visible.
			var e = Parse("=a1*abs(a1)");
			var values = new[] { 0.5f };
			for (int i = 0; i < 10000; i++) e.Evaluate(values);

			const int Runs = 2000000;
			var evaluating = Stopwatch.StartNew();
			for (int i = 0; i < Runs; i++) e.Evaluate(values);
			evaluating.Stop();

			const int Parses = 200;
			var parsing = Stopwatch.StartNew();
			for (int i = 0; i < Parses; i++)
			{
				MapExpression r; string err; int p;
				MapExpression.TryParse("=a1*abs(a1)+clamp(a2,0,1)", out r, out err, out p);
			}
			parsing.Stop();

			Console.WriteLine("evaluate : {0:F2} ns each, {1:N0} per second",
				evaluating.Elapsed.TotalMilliseconds * 1000000.0 / Runs,
				Runs / evaluating.Elapsed.TotalSeconds);
			Console.WriteLine("parse    : {0:F3} ms each",
				parsing.Elapsed.TotalMilliseconds / Parses);
			Console.WriteLine("at 1000 Hz with 24 mappings: {0:F3} ms of each second",
				24000.0 * evaluating.Elapsed.TotalMilliseconds / Runs);

			// Timing is deliberately not asserted, but a test with no assertion at all cannot fail
			// and so reads as coverage it does not provide. What is asserted is that two million
			// repetitions still answer correctly, which a caching or state fault would break.
			Assert.AreEqual(0.25f, e.Evaluate(values), "The answer changed while being repeated.");
			Assert.AreEqual(0.25f, Parse("=a1*abs(a1)").Evaluate(values),
				"A freshly parsed copy disagreed with one that had been run two million times.");
		}

		#endregion

	}
}
