// @under-test: App.v4/Common/AppHelper.cs
// @area: ui   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.Linq;
using x360ce.App;

namespace x360ce.Tests
{
	/// <summary>
	/// The lights that say what state a controller tab is in, and whether they can be told apart.
	/// </summary>
	/// <remarks>
	/// Colour is the entire message: the light says which of five states a tab is in and nothing else.
	/// The glass ones spent most of their height on a near-black rim and a white highlight, so about a
	/// third of the icon actually showed the colour, and at any distance green and grey were the same
	/// dark smudge - which is exactly the state pair somebody most needs to tell apart, because one
	/// means working and the other means nothing is set up.
	///
	/// So the icons are drawn instead of loaded: they fill the space they are given, the colour is
	/// what they are mostly made of, and adding a state costs a colour rather than a picture file.
	/// </remarks>
	[TestClass]
	public class StatusIconTest
	{
		static readonly string[] All =
		{
			AppHelper.StatusGreen, AppHelper.StatusRed, AppHelper.StatusAmber, AppHelper.StatusOrange,
			AppHelper.StatusBlue, AppHelper.StatusGrey,
		};

		/// <summary>How much of the icon is drawn on at all.</summary>
		static int Covered(Bitmap image)
		{
			var count = 0;
			for (var y = 0; y < image.Height; y++)
				for (var x = 0; x < image.Width; x++)
					if (image.GetPixel(x, y).A > 200)
						count++;
			return count;
		}

		/// <summary>How light a colour reads, on the measure eyes actually use.</summary>
		static double Lightness(Color c)
		{
			return (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255.0;
		}

		[TestMethod, TestCategory("ui")]
		[Description("A light is the size and shape it has always been")]
		public void A_light_is_the_size_and_shape_it_has_always_been()
		{
			// Ten pixels across in the middle of sixteen, with square corners, which is what the pictures
			// these replaced were. Drawn any bigger it sits taller than everything beside it in a row of
			// icons; rounded, it spends the few pixels it has on a curve nobody can see at this size.
			foreach (var hex in All)
			{
				var image = AppHelper.GetStatusIcon(hex);
				Assert.AreEqual(16, image.Width);
				Assert.AreEqual(16, image.Height);
				var covered = Covered(image);
				Assert.AreEqual(100, covered,
					"A light in " + hex + " covers " + covered + " of 256 pixels. The lights it replaced " +
					"covered 100, and changing that makes it the odd one out wherever icons sit in a row.");
				foreach (var corner in new[] { new Point(3, 3), new Point(12, 3), new Point(3, 12), new Point(12, 12) })
					Assert.AreEqual(255, image.GetPixel(corner.X, corner.Y).A,
						"The " + hex + " light is missing its corner at " + corner + ", so it has been rounded.");
			}
		}

		[TestMethod, TestCategory("ui")]
		[Description("A light is mostly the colour it stands for")]
		public void A_light_is_mostly_the_colour_it_stands_for()
		{
			// Not a rim and a highlight with a little colour between them. The middle of the icon has
			// to be the colour asked for, near enough that somebody would name it correctly.
			foreach (var hex in All)
			{
				var wanted = ColorTranslator.FromHtml(hex);
				var middle = AppHelper.GetStatusIcon(hex).GetPixel(8, 9);
				var apart = Math.Abs(middle.R - wanted.R) + Math.Abs(middle.G - wanted.G) + Math.Abs(middle.B - wanted.B);
				Assert.IsTrue(apart < 60,
					"The middle of the " + hex + " light is " + ColorTranslator.ToHtml(middle) +
					", which is not the colour it is meant to be showing.");
			}
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("Working and nothing-set-up do not look alike")]
		public void Working_and_nothing_set_up_do_not_look_alike()
		{
			// The pair that matters most and read the same before: green means the game is receiving
			// what the person presses, grey means nothing is set up at all. Told apart by lightness as
			// well as by hue, so they still differ for somebody who cannot separate the two colours.
			var green = AppHelper.GetStatusIcon(AppHelper.StatusGreen).GetPixel(8, 9);
			var grey = AppHelper.GetStatusIcon(AppHelper.StatusGrey).GetPixel(8, 9);
			var apart = Math.Abs(Lightness(green) - Lightness(grey));
			Assert.IsTrue(apart > 0.15,
				"Working and nothing-set-up are within " + apart.ToString("0.00") + " of each other in " +
				"lightness, so they are one smudge at a distance and identical to somebody who does " +
				"not separate green from grey.");
		}

		[TestMethod, TestCategory("ui")]
		[Description("Every state looks different from every other")]
		public void Every_state_looks_different_from_every_other()
		{
			// Five lights, five meanings. Two that look alike leave the person guessing which one they
			// are looking at, which is the position no colour at all would put them in.
			for (var i = 0; i < All.Length; i++)
				for (var j = i + 1; j < All.Length; j++)
				{
					var a = AppHelper.GetStatusIcon(All[i]).GetPixel(8, 9);
					var b = AppHelper.GetStatusIcon(All[j]).GetPixel(8, 9);
					var apart = Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);
					Assert.IsTrue(apart > 40,
						All[i] + " and " + All[j] + " differ by only " + apart + " across all three " +
						"channels, which is not enough to tell one from the other.");
				}
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("The warnings run from mild to serious, and look it")]
		public void The_warnings_run_from_mild_to_serious()
		{
			// Three warm lights, and which is worse than which has to be visible without reading anything.
			// Amber: something is there and nothing of ours drives it. Orange: it works, but a game reads
			// it as a different player - a thing that can be put right by reordering. Red: nothing reaches
			// the game at all. Less green in each, so it reddens as it worsens.
			var amber = AppHelper.GetStatusIcon(AppHelper.StatusAmber).GetPixel(8, 9);
			var orange = AppHelper.GetStatusIcon(AppHelper.StatusOrange).GetPixel(8, 9);
			var red = AppHelper.GetStatusIcon(AppHelper.StatusRed).GetPixel(8, 9);
			Assert.IsTrue(amber.G > orange.G, "Orange is not redder than amber, so the milder warning " +
				"looks like the worse one.");
			Assert.IsTrue(orange.G > red.G, "Red is not redder than orange, so the worst state does not " +
				"look like the worst state.");
		}

		[TestMethod, TestCategory("ui")]
		[Description("The same light is drawn once and kept")]
		public void The_same_light_is_drawn_once_and_kept()
		{
			// A tab and every row of a list ask for these many times a second. A bitmap made for a
			// single paint is never given back.
			Assert.AreSame(AppHelper.GetStatusIcon(AppHelper.StatusGreen),
				AppHelper.GetStatusIcon(AppHelper.StatusGreen),
				"A new picture is made every time one is asked for.");
		}

		[TestMethod, TestCategory("ui")]
		[Description("A colour can be given with or without its hash")]
		public void A_colour_can_be_given_with_or_without_its_hash()
		{
			var withHash = AppHelper.GetStatusIcon("#5FBF60").GetPixel(8, 9);
			var without = AppHelper.GetStatusIcon("5FBF60").GetPixel(8, 9);
			Assert.AreEqual(withHash, without);
		}

	}
}
