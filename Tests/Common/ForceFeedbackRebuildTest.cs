// @under-test: Engine/Common/ForceFeedbackState.cs
// @area: force-feedback   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using x360ce.Engine;
using MotorEffects = x360ce.Engine.ForceFeedbackState.MotorEffects;

namespace x360ce.Tests
{
	/// <summary>
	/// Which of its two motor effects a force feedback state stops and disposes on a force update, and which it makes.
	/// </summary>
	/// <remarks>
	/// A pad can refuse to make one of the effects, and asked again it gives the same answer. The state remembers the
	/// refusal and asks again only when the effect type, the motors or a direction change, or when the state is new.
	/// Before it makes any effect it stops and disposes every one it still holds, so the pad never holds two for one
	/// motor, each left playing at the last strength it was given. The decision is a static method, so it is tested
	/// here without a pad; <see cref="TwoMotorPadHardwareTest"/> drives a real one.
	/// </remarks>
	[TestClass]
	public class ForceFeedbackRebuildTest
	{
		const MotorEffects None = MotorEffects.None;
		const MotorEffects Left = MotorEffects.Left;
		const MotorEffects Right = MotorEffects.Right;
		const MotorEffects Both = MotorEffects.Left | MotorEffects.Right;

		/// <summary>The decision for a pad with two motors.</summary>
		static void Plan(MotorEffects held, MotorEffects refused, bool changed, out MotorEffects drop, out MotorEffects make)
		{
			ForceFeedbackState.PlanEffects(Both, held, refused, changed, out drop, out make);
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("After the pad refused the right effect, the next force update makes no new left effect and keeps the one the pad holds")]
		public void Right_refused_the_next_force_update_leaves_the_left_effect_alone()
		{
			MotorEffects drop, make;
			// The first force update made the left effect, and the pad refused the right one.
			Plan(Left, Right, false, out drop, out make);
			Assert.AreEqual(None, make, "A force update after a refusal made the effects again, a new left one among them.");
			Assert.AreEqual(None, drop, "A force update after a refusal replaced the left effect the pad holds.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("After the pad refused the left effect, the next force update makes no new right effect and keeps the one the pad holds")]
		public void Left_refused_the_next_force_update_leaves_the_right_effect_alone()
		{
			MotorEffects drop, make;
			// The first force update made the right effect, and the pad refused the left one.
			Plan(Right, Left, false, out drop, out make);
			Assert.AreEqual(None, make, "A force update after a refusal made the effects again, a new right one among them.");
			Assert.AreEqual(None, drop, "A force update after a refusal replaced the right effect the pad holds.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("An effect the pad lost is made again, and every effect still held is stopped and disposed first")]
		public void A_lost_effect_is_made_again_after_the_held_ones_are_stopped()
		{
			MotorEffects drop, make;
			// The right effect is gone, but the pad did not refuse it: making it failed, or the pad lost it.
			Plan(Left, None, false, out drop, out make);
			Assert.AreEqual(Both, make, "An effect the pad lost is not made again.");
			Assert.AreEqual(Left, drop, "A new left effect is made while the old one is left on the pad.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("A new effect type stops and disposes the old effects and makes both again, a refused one included")]
		public void Type_changed_old_effects_are_stopped_and_made_again()
		{
			MotorEffects drop, make;
			Plan(Both, None, true, out drop, out make);
			Assert.AreEqual(Both, drop, "The old effects stay on the pad after the type changed.");
			Assert.AreEqual(Both, make, "The effects are not made again for the new type.");
			// The refusal was for the old type.
			Plan(Left, Right, true, out drop, out make);
			Assert.AreEqual(Left, drop, "The old left effect stays on the pad after the type changed.");
			Assert.AreEqual(Both, make, "An effect refused for the old type is not asked for with the new one.");
		}

		[TestMethod, TestCategory("force-feedback")]
		[Description("A pad taken back for force feedback gets a new state, which asks for both effects, the one refused before included")]
		public void Held_again_the_refused_effect_is_asked_for_again()
		{
			// The program makes a new state for a pad it takes back for force feedback: after the force feedback or the tabs
			// it drives were switched off, or the device was unticked or taken off its tabs. A new state holds nothing and
			// remembers no refusal, and on its first force update every setting reads as changed.
			MotorEffects drop, make;
			Plan(None, None, true, out drop, out make);
			Assert.AreEqual(Both, make, "A pad taken back for force feedback is not asked for its effects.");
			Assert.AreEqual(None, drop, "A new state stopped effects it never made.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("A force update that changes nothing about the effects makes and stops nothing")]
		public void Nothing_changed_nothing_is_made()
		{
			MotorEffects drop, make;
			Plan(Both, None, false, out drop, out make);
			Assert.AreEqual(None, make, "A force update made effects the pad already holds.");
			Assert.AreEqual(None, drop, "A force update stopped effects the pad still plays.");
			// A wheel has one motor.
			ForceFeedbackState.PlanEffects(Left, Left, None, false, out drop, out make);
			Assert.AreEqual(None, make, "A force update made the effect a wheel already holds.");
			Assert.AreEqual(None, drop);
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("A device that refused every effect is not asked again on every force update")]
		public void Every_effect_refused_nothing_is_asked_again()
		{
			MotorEffects drop, make;
			Plan(None, Both, false, out drop, out make);
			Assert.AreEqual(None, make, "A pad that refused both effects is asked for them on every force update.");
			Assert.AreEqual(None, drop);
			// A wheel that refused its one effect.
			ForceFeedbackState.PlanEffects(Left, None, Left, false, out drop, out make);
			Assert.AreEqual(None, make, "A wheel that refused its effect is asked for it on every force update.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("For every input, nothing held is left beside a new effect, and a refusal is asked again only after a change")]
		public void No_input_leaves_an_effect_behind_or_asks_a_refusal_again()
		{
			var sets = new[] { None, Left, Right, Both };
			foreach (var motors in new[] { Left, Both })
				foreach (var held in sets)
					foreach (var refused in sets)
						foreach (var changed in new[] { false, true })
						{
							// The state never holds an effect it remembers as refused.
							if ((held & refused) != None)
								continue;
							MotorEffects drop, make;
							ForceFeedbackState.PlanEffects(motors, held, refused, changed, out drop, out make);
							var at = "motors " + motors + ", held " + held + ", refused " + refused + (changed ? ", changed" : "");
							Assert.AreEqual(make == None ? None : held, drop, at + ": an effect is left on the pad beside a new one.");
							Assert.AreEqual(None, make & ~motors, at + ": an effect is made for a motor the device does not have.");
							Assert.AreEqual(None, drop & motors & ~make, at + ": a motor's effect is stopped and not made again.");
							if (changed)
								Assert.AreEqual(motors, make, at + ": a change does not ask for every effect again.");
							else
								Assert.AreEqual(None, make & refused, at + ": a refusal is asked for again with nothing changed.");
						}
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("performance")]
		[Description("Deciding that nothing changed hands nothing to the collector")]
		public void Deciding_hands_nothing_to_the_collector()
		{
			const int calls = 20000;
			var allocated = Allocations.FewestBytes(5, () =>
			{
				MotorEffects drop, make;
				for (var i = 0; i < calls; i++)
				{
					if ((i & 1) == 0)
						ForceFeedbackState.PlanEffects(Both, Both, None, false, out drop, out make);
					else
						ForceFeedbackState.PlanEffects(Both, Left, Right, false, out drop, out make);
				}
			});
			Assert.IsTrue(allocated < calls,
				"Deciding " + calls + " times handed the collector " + allocated + " bytes; it runs on every force update.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("The state decides through the rule, remembers a refusal where it makes an effect, and forgets it on a change")]
		public void The_state_decides_through_the_rule()
		{
			var source = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "Engine", "Common", "ForceFeedbackState.cs"));
			var plan = source.IndexOf("PlanEffects(motors, held, motorsRefused, motorsChanged || forceChanged || resend, out drop, out make);");
			Assert.IsTrue(plan > 0, "The state does not decide through PlanEffects with the refusals it remembers.");
			Assert.IsFalse(source.Contains("if (paramsL != null && effectL == null)") || source.Contains("if (paramsR != null && effectR == null)"),
				"A missing effect still makes the pair again whatever the pad answered before.");
			var dispose = source.IndexOf("effectL.Dispose();");
			var makeLeft = source.IndexOf("effectL = CreateEffect(device, GUID_Force, paramsL);");
			var makeRight = source.IndexOf("effectR = CreateEffect(device, GUID_Force, paramsR);");
			Assert.IsTrue(plan < dispose && dispose < makeLeft && makeLeft < makeRight,
				"The held effects are not stopped and disposed, as planned, before new ones are made.");
			var rememberLeft = source.IndexOf("motorsRefused |= MotorEffects.Left;");
			var rememberRight = source.IndexOf("motorsRefused |= MotorEffects.Right;");
			Assert.IsTrue(rememberLeft > makeLeft && rememberLeft < makeRight && rememberRight > makeRight,
				"A refused effect is not remembered where it is made, so it is asked for on every force update.");
			var forgetAll = source.IndexOf("motorsRefused = MotorEffects.None;");
			var forgetLeft = source.IndexOf("motorsRefused &= ~MotorEffects.Left;");
			var forgetRight = source.IndexOf("motorsRefused &= ~MotorEffects.Right;");
			Assert.IsTrue(forgetAll > 0 && forgetAll < plan && source.IndexOf("GUID_Force = ForceFeedbackDriver.EffectFor(") < forgetAll,
				"A new effect type or new motors do not ask again for an effect refused before.");
			var changeBlock = source.IndexOf("GUID_Force = ForceFeedbackDriver.EffectFor(");
			Assert.IsTrue(forgetAll < source.IndexOf('}', changeBlock),
				"A refusal is forgotten on every force update, not only after a new type or new motors.");
			foreach (var step in new[] { "effectL.Stop();", "effectL.Dispose();", "effectR.Stop();", "effectR.Dispose();" })
			{
				var at = source.IndexOf(step, plan);
				Assert.IsTrue(at > plan && at < makeLeft, "The plan's " + step + " is not done before the new effects are made.");
			}
			Assert.IsTrue(source.IndexOf("var directionLChanged = Changed(") < forgetLeft && forgetLeft < plan
				&& source.IndexOf("var directionRChanged = Changed(") < forgetRight && forgetRight < plan,
				"A direction turned does not ask again for the effect refused before.");
			StringAssert.Contains(source, "effectD == null && motorsRefused == MotorEffects.None)",
				"A state that remembers a refusal starts clean on every force update, listing and disposing the device's effects.");
		}
	}
}
