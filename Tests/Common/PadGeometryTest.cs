// @under-test: App.v4/Controls/PadTabPages/General/XboxImageUserControl.cs, App.v4/Controls/PadControl.cs
// @area: pad-images   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
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

		public TestContext TestContext { get; set; }

		/// <summary>The table a controller page draws its marks from, copied without its controls.</summary>
		internal static ImageInfos RealMappings()
		{
			if (_RealMappings == null)
				Ui.OnUiThread(() =>
				{
					var infos = new ImageInfos();
					using (var pad = new PadControl(MapTo.Controller1))
						foreach (var info in pad.imageInfos)
							infos.Add(info.Image, info.Code, info.X, info.Y, null, null, info.Button);
					_RealMappings = infos;
				});
			return _RealMappings;
		}

		static ImageInfos _RealMappings;

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
				var expected = new PointF((float)info.X, (float)(105 + 8 + info.Y));
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

		static double Distance(PointF a, PointF b)
		{
			double dx = a.X - b.X, dy = a.Y - b.Y;
			return Math.Sqrt(dx * dx + dy * dy);
		}

		#region Marks centred on their buttons

		/// <summary>The buttons whose marks were seen off their middles: Back, Guide, Start and the four face buttons.</summary>
		internal static readonly MapCode[] CentredButtons =
		{
			MapCode.ButtonBack, MapCode.ButtonGuide, MapCode.ButtonStart,
			MapCode.ButtonA, MapCode.ButtonB, MapCode.ButtonX, MapCode.ButtonY,
		};

		/// <summary>The screen scales the picture is checked at: 100 % and 150 %.</summary>
		internal static readonly float[] CheckedScales = { 1f, 1.5f };

		[TestMethod, TestCategory("pad-images")]
		[Description("The mark of Back, Guide, Start, A, B, X and Y is drawn within a pixel of its button's middle, across and down, at 100 % and 150 %, each measured on the picture as drawn")]
		public void Marks_are_centred_on_their_buttons()
		{
			var infos = RealMappings();
			var faults = new List<string>();
			Ui.OnUiThread(() =>
			{
				foreach (var scale in CheckedScales)
					foreach (var pair in MeasureMarks(infos, scale))
					{
						var button = pair.Value[0];
						var mark = pair.Value[1];
						var line = string.Format("{0:P0} {1}: button ({2:0.00}, {3:0.00}), mark ({4:0.00}, {5:0.00}), mark off by ({6:+0.00;-0.00;0.00}, {7:+0.00;-0.00;0.00}) px",
							scale, pair.Key, button.X, button.Y, mark.X, mark.Y, mark.X - button.X, mark.Y - button.Y);
						TestContext.WriteLine(line);
						if (Math.Abs(mark.X - button.X) > 1 || Math.Abs(mark.Y - button.Y) > 1)
							faults.Add(line);
					}
			});
			Assert.AreEqual(0, faults.Count, faults.Count + " mark(s) more than a pixel off their buttons:" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		/// <summary>
		/// Draws the controller picture at a screen scale and measures, for each of <see cref="CentredButtons"/>, the middle of
		/// the button and the middle of its mark, in the picture's pixels. Runs on the interface thread.
		/// </summary>
		/// <returns>For each button: the button's middle, then its mark's.</returns>
		internal static Dictionary<MapCode, PointF[]> MeasureMarks(ImageInfos infos, float scale)
		{
			var result = new Dictionary<MapCode, PointF[]>();
			var size = new Size((int)Math.Round(XboxImageUserControl.CanvasWidth * scale), (int)Math.Round(XboxImageUserControl.CanvasHeight * scale));
			using (var picture = new XboxImageUserControl { Size = size })
			{
				picture.InitializeImages(infos, null, MapTo.Controller1);
				using (var bare = Draw(picture))
					foreach (var code in CentredButtons)
					{
						var info = infos.First(x => x.Code == code);
						var guess = new PointF((float)info.X * scale, (float)(105 + 8 + info.Y) * scale);
						var button = ButtonMiddle(bare, guess, scale, code);
						picture.SetImage(code, NavImageType.Normal, true);
						using (var blue = Draw(picture))
						{
							picture.SetImage(code, NavImageType.Record, true);
							using (var red = Draw(picture))
								result[code] = new[] { button, MarkMiddle(blue, red) };
						}
						picture.SetImage(code, NavImageType.Normal, false);
					}
			}
			return result;
		}

		internal static Bitmap Draw(Control control)
		{
			var bitmap = new Bitmap(control.Width, control.Height);
			control.DrawToBitmap(bitmap, new Rectangle(Point.Empty, control.Size));
			return bitmap;
		}

		/// <summary>
		/// The middle of a mark: the centre of the pixels that change when the same mark is drawn in two colours, each counted
		/// by how much it changes, so by how much of the mark covers it. The picture under the mark is the same in both and
		/// drops out.
		/// </summary>
		static PointF MarkMiddle(Bitmap one, Bitmap other)
		{
			var a = Pixels(one);
			var b = Pixels(other);
			double sum = 0, sx = 0, sy = 0;
			for (var y = 0; y < one.Height; y++)
				for (var x = 0; x < one.Width; x++)
				{
					var i = (y * one.Width + x) * 4;
					double w = Math.Abs(a[i] - b[i]) + Math.Abs(a[i + 1] - b[i + 1]) + Math.Abs(a[i + 2] - b[i + 2]);
					sum += w;
					sx += w * (x + 0.5);
					sy += w * (y + 0.5);
				}
			Assert.IsTrue(sum > 0, "The mark was drawn the same in both colours, or not at all.");
			return new PointF((float)(sx / sum), (float)(sy / sum));
		}

		/// <summary>
		/// The middle of a button as drawn: the centre of the circle that best fits its rim. The rim is the darkest point along
		/// each of 90 rays out from a first guess, or for the Guide the yellowest, its ring of light; the guess then moves to
		/// the centre found and the rays are cast again, four times. Each ray looks only as far out as the button's rim lies,
		/// so a letter inside the button or the words beside it are not taken for the rim.
		/// </summary>
		/// <param name="guess">A first guess at the middle, in the picture's pixels.</param>
		/// <param name="scale">Pixels to a canvas unit.</param>
		static PointF ButtonMiddle(Bitmap bitmap, PointF guess, float scale, MapCode code)
		{
			var guide = code == MapCode.ButtonGuide;
			var menu = code == MapCode.ButtonBack || code == MapCode.ButtonStart;
			// Where the rim lies from the button's middle, in canvas units: a face button's rim is about 7.6 out, the Guide's
			// ring 13 and Back's and Start's about 5.9.
			var from = (guide ? 8f : menu ? 3.5f : 5f) * scale;
			var to = (guide ? 14f : menu ? 7.5f : 11f) * scale;
			var rgb = Pixels(bitmap);
			var feature = new float[bitmap.Width * bitmap.Height];
			for (var i = 0; i < feature.Length; i++)
			{
				float b = rgb[i * 4], g = rgb[i * 4 + 1], r = rgb[i * 4 + 2];
				feature[i] = guide ? (r + g) / 2 - b : 255 - (0.299f * r + 0.587f * g + 0.114f * b);
			}
			double cx = guess.X, cy = guess.Y;
			const int rays = 90;
			for (var pass = 0; pass < 4; pass++)
			{
				var xs = new double[rays];
				var ys = new double[rays];
				for (var k = 0; k < rays; k++)
				{
					var angle = 2 * Math.PI * k / rays;
					double best = double.MinValue, at = from;
					for (var r = from; r <= to; r += 0.1f)
					{
						var v = Sample(feature, bitmap.Width, bitmap.Height, cx + r * Math.Cos(angle), cy + r * Math.Sin(angle));
						if (v > best)
						{
							best = v;
							at = r;
						}
					}
					xs[k] = cx + at * Math.Cos(angle);
					ys[k] = cy + at * Math.Sin(angle);
				}
				FitCircle(xs, ys, out cx, out cy);
			}
			return new PointF((float)cx, (float)cy);
		}

		/// <summary>A value between pixel centres, a pixel's own value standing at its middle.</summary>
		static double Sample(float[] values, int width, int height, double x, double y)
		{
			x -= 0.5;
			y -= 0.5;
			var x0 = Math.Max(0, Math.Min(width - 2, (int)Math.Floor(x)));
			var y0 = Math.Max(0, Math.Min(height - 2, (int)Math.Floor(y)));
			double fx = x - x0, fy = y - y0;
			return values[y0 * width + x0] * (1 - fx) * (1 - fy) + values[y0 * width + x0 + 1] * fx * (1 - fy)
				+ values[(y0 + 1) * width + x0] * (1 - fx) * fy + values[(y0 + 1) * width + x0 + 1] * fx * fy;
		}

		/// <summary>The centre of the circle nearest to the points, by least squares (Kasa's fit).</summary>
		static void FitCircle(double[] xs, double[] ys, out double cx, out double cy)
		{
			// x * x + y * y = 2ax + 2by + c, solved for a, b and c.
			double sxx = 0, sxy = 0, syy = 0, sx = 0, sy = 0, sz = 0, sxz = 0, syz = 0;
			var n = xs.Length;
			for (var i = 0; i < n; i++)
			{
				double x = xs[i], y = ys[i], z = x * x + y * y;
				sxx += x * x; sxy += x * y; syy += y * y; sx += x; sy += y; sz += z; sxz += x * z; syz += y * z;
			}
			var m = new[,] { { 2 * sxx, 2 * sxy, sx }, { 2 * sxy, 2 * syy, sy }, { 2 * sx, 2 * sy, n } };
			var v = new[] { sxz, syz, sz };
			var solved = Solve(m, v);
			cx = solved[0];
			cy = solved[1];
		}

		static double[] Solve(double[,] m, double[] v)
		{
			var n = v.Length;
			for (var col = 0; col < n; col++)
			{
				var pivot = col;
				for (var row = col + 1; row < n; row++)
					if (Math.Abs(m[row, col]) > Math.Abs(m[pivot, col]))
						pivot = row;
				for (var k = 0; k < n; k++)
				{
					var t = m[col, k]; m[col, k] = m[pivot, k]; m[pivot, k] = t;
				}
				var tv = v[col]; v[col] = v[pivot]; v[pivot] = tv;
				for (var row = 0; row < n; row++)
				{
					if (row == col)
						continue;
					var f = m[row, col] / m[col, col];
					for (var k = col; k < n; k++)
						m[row, k] -= f * m[col, k];
					v[row] -= f * v[col];
				}
			}
			var result = new double[n];
			for (var i = 0; i < n; i++)
				result[i] = v[i] / m[i, i];
			return result;
		}

		/// <summary>A bitmap's pixels as blue, green, red and alpha bytes, row after row.</summary>
		static byte[] Pixels(Bitmap bitmap)
		{
			var data = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), System.Drawing.Imaging.ImageLockMode.ReadOnly,
				System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			try
			{
				var bytes = new byte[bitmap.Width * bitmap.Height * 4];
				for (var y = 0; y < bitmap.Height; y++)
					System.Runtime.InteropServices.Marshal.Copy(data.Scan0 + y * data.Stride, bytes, y * bitmap.Width * 4, bitmap.Width * 4);
				return bytes;
			}
			finally
			{
				bitmap.UnlockBits(data);
			}
		}

		#endregion

	}
}
