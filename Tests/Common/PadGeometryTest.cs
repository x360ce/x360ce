// @under-test: App.v4/Controls/PadTabPages/General/XboxImageUserControl.cs
// @area: pad-images   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// Glyph placement on the controller picture.
	/// </summary>
	/// <remarks>
	/// The mapping table was written for the small marks the older interface drew, and around the
	/// D-pad and thumbsticks its positions are closer together than a glyph is wide. Drawing there
	/// unchanged fused each cluster into a blob. The layout spreads those apart, and these tests
	/// hold that: no pair may sit closer than a glyph, and clusters that were already wide enough
	/// must not be nudged.
	/// </remarks>
	[TestClass]
	public class PadGeometryTest
	{

		/// <summary>The real table, built without needing a controller picture or a device.</summary>
		static ImageInfos RealMappings()
		{
			var infos = new ImageInfos();
			// Image 1 is the top view, image 2 the front, matching PadControl.
			infos.Add(1, MapCode.LeftTrigger, 63, 27, null, null);
			infos.Add(1, MapCode.RightTrigger, 193, 27, null, null);
			infos.Add(1, MapCode.LeftShoulder, 43, 66, null, null);
			infos.Add(1, MapCode.RightShoulder, 213, 66, null, null);
			infos.Add(2, MapCode.ButtonY, 196, 29, null, null);
			infos.Add(2, MapCode.ButtonX, 178, 48, null, null);
			infos.Add(2, MapCode.ButtonB, 215, 48, null, null);
			infos.Add(2, MapCode.ButtonA, 196, 66, null, null);
			infos.Add(2, MapCode.ButtonGuide, 127, 48, null, null);
			infos.Add(2, MapCode.ButtonBack, 103, 48, null, null);
			infos.Add(2, MapCode.ButtonStart, 152, 48, null, null);
			infos.Add(2, MapCode.DPadUp, 92, 88 - 13, null, null);
			infos.Add(2, MapCode.DPadLeft, 92 - 13, 88, null, null);
			infos.Add(2, MapCode.DPadRight, 92 + 13, 88, null, null);
			infos.Add(2, MapCode.DPadDown, 92, 88 + 13, null, null);
			infos.Add(2, MapCode.DPad, 92, 88, null, null);
			infos.Add(2, MapCode.LeftThumbButton, 59, 47, null, null);
			infos.Add(2, MapCode.LeftThumbUp, 59, 47 - 10, null, null);
			infos.Add(2, MapCode.LeftThumbLeft, 59 - 10, 47, null, null);
			infos.Add(2, MapCode.LeftThumbRight, 59 + 10, 47, null, null);
			infos.Add(2, MapCode.LeftThumbDown, 59, 47 + 10, null, null);
			infos.Add(2, MapCode.RightThumbButton, 160, 88, null, null);
			infos.Add(2, MapCode.RightThumbUp, 160, 88 - 10, null, null);
			infos.Add(2, MapCode.RightThumbLeft, 160 - 10, 88, null, null);
			infos.Add(2, MapCode.RightThumbRight, 160 + 10, 88, null, null);
			infos.Add(2, MapCode.RightThumbDown, 160, 88 + 10, null, null);
			return infos;
		}

		/// <summary>Codes that share a glyph with a direction, so they never draw separately.</summary>
		static bool DrawsItsOwnGlyph(MapCode code)
		{
			return NavImages.GetNameCode(code) == code;
		}

		[TestMethod, TestCategory("pad-images"), TestCategory("smoke")]
		[Description("No two glyphs are drawn close enough to overlap")]
		public void Glyphs_never_overlap_each_other()
		{
			var points = XboxImageUserControl.BuildGlyphPoints(RealMappings())
				.Where(x => DrawsItsOwnGlyph(x.Key)).ToArray();
			Assert.IsTrue(points.Length > 20, "Expected the whole mapping table, got " + points.Length + ".");

			var closest = double.MaxValue;
			string pair = null;
			for (var i = 0; i < points.Length; i++)
				for (var j = i + 1; j < points.Length; j++)
				{
					var distance = Distance(points[i].Value, points[j].Value);
					if (distance >= closest)
						continue;
					closest = distance;
					pair = points[i].Key + " and " + points[j].Key;
				}

			Assert.IsTrue(closest >= XboxImageUserControl.GlyphSize,
				"The closest glyphs are " + pair + " at " + closest.ToString("N1") +
				" apart, which is less than the " + XboxImageUserControl.GlyphSize +
				" a glyph is wide, so they overlap.");
		}

		[TestMethod, TestCategory("pad-images"), TestCategory("smoke")]
		[Description("Clusters that were already wide enough are left where they were")]
		public void Wide_enough_clusters_are_not_moved()
		{
			var infos = RealMappings();
			var points = XboxImageUserControl.BuildGlyphPoints(infos);
			// The face buttons sit 25 apart in the table, comfortably wider than a glyph.
			foreach (var code in new[] { MapCode.ButtonY, MapCode.ButtonX, MapCode.ButtonB, MapCode.ButtonA,
										 MapCode.ButtonBack, MapCode.ButtonGuide, MapCode.ButtonStart })
			{
				var info = infos.First(x => x.Code == code);
				var expected = new Point((int)info.X, (int)(105 + 8 + info.Y));
				Assert.AreEqual(expected, points[code],
					code + " was moved from its table position, but it was already far enough from its neighbours.");
			}
		}

		[TestMethod, TestCategory("pad-images"), TestCategory("smoke")]
		[Description("Spread glyphs keep the direction the table gave them")]
		public void Spreading_keeps_each_direction()
		{
			var points = XboxImageUserControl.BuildGlyphPoints(RealMappings());
			var centre = points[MapCode.DPad];
			Assert.IsTrue(points[MapCode.DPadUp].Y < centre.Y, "Up moved below the centre.");
			Assert.IsTrue(points[MapCode.DPadDown].Y > centre.Y, "Down moved above the centre.");
			Assert.IsTrue(points[MapCode.DPadLeft].X < centre.X, "Left moved right of the centre.");
			Assert.IsTrue(points[MapCode.DPadRight].X > centre.X, "Right moved left of the centre.");
			// Straight up must stay straight up rather than drifting sideways.
			Assert.AreEqual(centre.X, points[MapCode.DPadUp].X, "Up drifted off the vertical.");
			Assert.AreEqual(centre.Y, points[MapCode.DPadLeft].Y, "Left drifted off the horizontal.");
		}

		[TestMethod, TestCategory("pad-images"), TestCategory("smoke")]
		[Description("An axis and its direction share one glyph rather than stacking")]
		public void Axis_codes_share_a_glyph_with_their_direction()
		{
			Assert.AreEqual(MapCode.LeftThumbRight, NavImages.GetNameCode(MapCode.LeftThumbAxisX));
			Assert.AreEqual(MapCode.LeftThumbUp, NavImages.GetNameCode(MapCode.LeftThumbAxisY));
			Assert.AreEqual(MapCode.RightThumbRight, NavImages.GetNameCode(MapCode.RightThumbAxisX));
			Assert.AreEqual(MapCode.RightThumbUp, NavImages.GetNameCode(MapCode.RightThumbAxisY));
			// Everything else is drawn for itself.
			Assert.AreEqual(MapCode.DPadUp, NavImages.GetNameCode(MapCode.DPadUp));
			Assert.AreEqual(MapCode.ButtonA, NavImages.GetNameCode(MapCode.ButtonA));
		}

		[TestMethod, TestCategory("pad-images"), TestCategory("smoke")]
		[Description("A trigger bar travels a visible distance as the trigger is pressed")]
		public void Trigger_bar_travels_as_the_trigger_is_pressed()
		{
			var button = new Point(63, 27);
			var released = XboxImageUserControl.TriggerBarPoint(button, 0f);
			var pressed = XboxImageUserControl.TriggerBarPoint(button, 1f);

			// Pressing lifts the bar up the shoulder, so the screen coordinate falls.
			Assert.IsTrue(pressed.Y < released.Y,
				"The bar moved from " + released.Y + " to " + pressed.Y + ", which is not upwards.");
			Assert.AreEqual(XboxImageUserControl.TriggerTravel, released.Y - pressed.Y,
				"Full travel should move the bar by the whole trigger range.");
			Assert.AreEqual(released.X, pressed.X, "The bar drifted sideways.");

			// Every step in between must move it, or it reads as a blink rather than travel.
			var previous = released.Y;
			foreach (var level in new[] { 0.25f, 0.5f, 0.75f, 1f })
			{
				var y = XboxImageUserControl.TriggerBarPoint(button, level).Y;
				Assert.IsTrue(y < previous,
					"The bar stopped moving at " + level + ": still at " + y + ".");
				previous = y;
			}
		}

		[TestMethod, TestCategory("pad-images"), TestCategory("smoke")]
		[Description("A released trigger still shows its bar, at rest below the arrow")]
		public void Released_trigger_still_shows_its_bar()
		{
			var button = new Point(63, 27);
			var released = XboxImageUserControl.TriggerBarPoint(button, 0f);
			// Resting below the arrow is what makes the bar readable as a level rather than a light.
			Assert.AreEqual(button.Y + XboxImageUserControl.TriggerRestOffset, released.Y,
				"A released trigger must rest below its arrow, not sit on it or vanish.");
			Assert.IsTrue(released.Y > button.Y, "The resting bar overlaps the arrow.");
		}

		[TestMethod, TestCategory("pad-images"), TestCategory("smoke")]
		[Description("A thumbstick mark follows the stick in both axes")]
		public void Thumb_mark_follows_the_stick()
		{
			var centre = new Point(59, 160);
			Assert.AreEqual(centre, XboxImageUserControl.ThumbMarkPoint(centre, new PointF(0f, 0f)),
				"A centred stick should leave the mark in the middle.");

			var right = XboxImageUserControl.ThumbMarkPoint(centre, new PointF(1f, 0f));
			var left = XboxImageUserControl.ThumbMarkPoint(centre, new PointF(-1f, 0f));
			var up = XboxImageUserControl.ThumbMarkPoint(centre, new PointF(0f, 1f));
			var down = XboxImageUserControl.ThumbMarkPoint(centre, new PointF(0f, -1f));

			Assert.IsTrue(right.X > centre.X && left.X < centre.X, "Left and right are swapped.");
			// Up on the stick must raise the mark, which lowers the screen coordinate.
			Assert.IsTrue(up.Y < centre.Y && down.Y > centre.Y, "Up and down are swapped.");
			Assert.AreEqual(XboxImageUserControl.ThumbTravel, right.X - centre.X, "Sideways travel is wrong.");
			Assert.AreEqual(XboxImageUserControl.ThumbTravel, centre.Y - up.Y, "Vertical travel is wrong.");

			// Beyond the ends of the stick the mark must stop rather than run off the picture.
			Assert.AreEqual(right, XboxImageUserControl.ThumbMarkPoint(centre, new PointF(9f, 0f)),
				"An out of range value moved the mark past the edge of its travel.");
		}

		static double Distance(Point a, Point b)
		{
			double dx = a.X - b.X, dy = a.Y - b.Y;
			return Math.Sqrt(dx * dx + dy * dy);
		}

	}
}
