// @under-test: App.v4/Controls/PadTabPages/General/XboxImageUserControl.cs, Resources/Images/shared/xbox/xboxControllerTopDark.png, Resources/Images/shared/xbox/xboxControllerFrontDark.png, Engine/Common/EngineHelper.cs, scripts/art/build_controller.py
// @area: pad-images   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using x360ce.App.Controls;

namespace x360ce.Tests
{
	/// <summary>
	/// The controller pictures: the same controller in both themes and at every size they are drawn at, and the
	/// picture keeps painting when GDI+ runs short of memory.
	/// </summary>
	/// <remarks>
	/// An exception that leaves a paint makes Windows Forms draw a red cross in that control for the rest of the session.
	/// GDI+ reports a scaled draw it found no memory for as out of memory, and a person reported the red cross that
	/// followed. GDI+ cannot be made to run out of memory on demand, so the paint's catch is read from its source.
	/// </remarks>
	[TestClass]
	public class ControllerPicturePaintTest
	{
		[TestMethod, TestCategory("pad-images"), TestCategory("critical")]
		[Description("The controller picture paints onto a bitmap")]
		public void The_picture_paints()
		{
			Ui.OnUiThread(() =>
			{
				using (var picture = new XboxImageUserControl())
				using (var bitmap = new Bitmap(picture.Width, picture.Height))
				{
					picture.DrawToBitmap(bitmap, new Rectangle(Point.Empty, picture.Size));
					var back = picture.BackColor.ToArgb();
					var drawn = false;
					for (var x = 0; x < bitmap.Width && !drawn; x += 4)
						for (var y = 0; y < bitmap.Height && !drawn; y += 4)
							drawn = bitmap.GetPixel(x, y).ToArgb() != back;
					Assert.IsTrue(drawn, "Nothing of the controller was drawn.");
				}
			});
		}

		[TestMethod, TestCategory("pad-images")]
		[Description("A dark controller picture is its light picture in other colours: the same size and outline")]
		public void Dark_pictures_keep_the_light_outline()
		{
			// The program's own assembly carries the pictures; naming a type in it loads it.
			Assert.IsNotNull(typeof(XboxImageUserControl).Assembly);
			foreach (var view in new[] { "Top", "Front" })
			{
				var lightStream = x360ce.Engine.EngineHelper.GetResourceStream("Images.xboxController" + view + ".png");
				var darkStream = x360ce.Engine.EngineHelper.GetResourceStream("Images.xboxController" + view + "Dark.png");
				Assert.IsNotNull(darkStream, "Images.xboxController" + view + "Dark.png is not in the program.");
				using (var light = new Bitmap(lightStream))
				using (var dark = new Bitmap(darkStream))
				{
					Assert.AreEqual(light.Size, dark.Size, view + ": the dark picture is another size, so the marks would land elsewhere.");
					int both = 0, either = 0;
					for (var y = 0; y < light.Height; y++)
						for (var x = 0; x < light.Width; x++)
						{
							var inLight = light.GetPixel(x, y).A > 127;
							var inDark = dark.GetPixel(x, y).A > 127;
							if (inLight && inDark)
								both++;
							if (inLight || inDark)
								either++;
						}
					var overlap = both / (double)either;
					Assert.IsTrue(overlap > 0.97, view + ": the outlines overlap " + overlap.ToString("P1") +
						"; the dark picture must show the same controller in the same place.");
				}
			}
		}

		[TestMethod, TestCategory("pad-images")]
		[Description("Every controller picture comes with its versions drawn at 1.5 and 2 times its size, showing the controller in the same place")]
		public void Pictures_come_with_versions_drawn_at_larger_sizes()
		{
			// The program's own assembly carries the pictures; naming a type in it loads it.
			Assert.IsNotNull(typeof(XboxImageUserControl).Assembly);
			foreach (var name in new[] { "Top", "Front", "TopDark", "FrontDark" })
			{
				var picture = x360ce.Engine.EngineHelper.GetResourcePicture("Images.xboxController" + name + ".png");
				var drawn = JocysCom.ClassLibrary.Controls.ControlsHelper.GetDrawnSizes(picture);
				var expected = new[] { 1.5, 2.0 }
					.Select(m => new Size((int)Math.Round(picture.Width * m), (int)Math.Round(picture.Height * m))).ToArray();
				CollectionAssert.AreEqual(expected, drawn.Select(x => x.Size).ToArray(),
					name + ": a version drawn at a larger size is missing or of the wrong size.");
				// The largest version, reduced to the picture's size, must cover the same outline, or the marks land elsewhere.
				using (var reduced = new Bitmap(picture.Width, picture.Height))
				{
					using (var g = Graphics.FromImage(reduced))
					{
						g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
						g.DrawImage(drawn[drawn.Length - 1], new Rectangle(Point.Empty, reduced.Size));
					}
					int both = 0, either = 0;
					for (var y = 0; y < picture.Height; y++)
						for (var x = 0; x < picture.Width; x++)
						{
							var inPicture = picture.GetPixel(x, y).A > 127;
							var inReduced = reduced.GetPixel(x, y).A > 127;
							if (inPicture && inReduced)
								both++;
							if (inPicture || inReduced)
								either++;
						}
					var overlap = both / (double)either;
					Assert.IsTrue(overlap > 0.98, name + ": the largest version overlaps the picture " + overlap.ToString("P1") + " only.");
				}
			}
		}

		[TestMethod, TestCategory("pad-images"), TestCategory("memory")]
		[Description("Every controller page draws one shared copy of each picture, and closing a page leaves it to the others")]
		public void Pages_share_one_copy_of_each_picture()
		{
			// The program's own assembly carries the pictures; naming a type in it loads it.
			Assert.IsNotNull(typeof(XboxImageUserControl).Assembly);
			var name = "Images.xboxControllerTop.png";
			Assert.AreSame(x360ce.Engine.EngineHelper.GetResourcePicture(name), x360ce.Engine.EngineHelper.GetResourcePicture(name),
				"Each page loaded its own copy of the pictures and their versions.");
			Ui.OnUiThread(() =>
			{
				using (var closed = new XboxImageUserControl())
				using (var bitmap = new Bitmap(closed.Width, closed.Height))
					closed.DrawToBitmap(bitmap, new Rectangle(Point.Empty, closed.Size));
				using (var open = new XboxImageUserControl())
				using (var bitmap = new Bitmap(open.Width, open.Height))
				{
					open.DrawToBitmap(bitmap, new Rectangle(Point.Empty, open.Size));
					var back = open.BackColor.ToArgb();
					var drawn = false;
					for (var x = 0; x < bitmap.Width && !drawn; x += 4)
						for (var y = 0; y < bitmap.Height && !drawn; y += 4)
							drawn = bitmap.GetPixel(x, y).ToArgb() != back;
					Assert.IsTrue(drawn, "A page opened after another closed drew nothing of the controller.");
				}
			});
		}

		[TestMethod, TestCategory("pad-images"), TestCategory("critical")]
		[Description("A faded copy is the picture at half its opacity, pixel for pixel, at every size and in both themes")]
		public void Faded_pictures_are_half_as_opaque()
		{
			// The program's own assembly carries the pictures; naming a type in it loads it.
			Assert.IsNotNull(typeof(XboxImageUserControl).Assembly);
			foreach (var name in new[] { "Top", "Front", "TopDark", "FrontDark" })
			{
				var picture = x360ce.Engine.EngineHelper.GetResourcePicture("Images.xboxController" + name + ".png");
				foreach (Bitmap source in new Image[] { picture }.Concat(JocysCom.ClassLibrary.Controls.ControlsHelper.GetDrawnSizes(picture)))
				{
					using (var faded = x360ce.Engine.EngineHelper.Faded(source))
					{
						Assert.AreEqual(source.Size, faded.Size, name + ": the faded picture is another size.");
						int worstAlpha = 0, worstColour = 0;
						for (var y = 0; y < faded.Height; y++)
							for (var x = 0; x < faded.Width; x++)
							{
								var p = source.GetPixel(x, y);
								var f = faded.GetPixel(x, y);
								var alpha = p.A * x360ce.Engine.EngineHelper.DisabledOpacity;
								worstAlpha = Math.Max(worstAlpha, (int)Math.Round(Math.Abs(f.A - alpha)));
								// What a pixel adds to the screen is its colour times its opacity, so a faint pixel's
								// colour, which GDI+ keeps with less precision, is held to what it shows.
								worstColour = Math.Max(worstColour, (int)Math.Round(new[] { f.R * f.A - p.R * alpha, f.G * f.A - p.G * alpha, f.B * f.A - p.B * alpha }
									.Max(d => Math.Abs(d)) / 255.0));
							}
						Console.WriteLine("{0} {1}: alpha within {2}, colour on screen within {3}", name, source.Size, worstAlpha, worstColour);
						Assert.IsTrue(worstAlpha <= 1 && worstColour <= 1, name + " " + source.Size + ": the faded picture is off by "
							+ worstAlpha + " in opacity and " + worstColour + " in colour on screen from the picture at half its opacity.");
					}
				}
			}
		}

		[TestMethod, TestCategory("pad-images"), TestCategory("critical")]
		[Description("A switched-off controller picture paints halfway between the picture and the background, and costs about what a full one does")]
		public void A_switched_off_picture_paints_half_as_strong()
		{
			Ui.OnUiThread(() =>
			{
				using (var picture = new XboxImageUserControl())
				using (var on = new Bitmap(picture.Width, picture.Height))
				using (var off = new Bitmap(picture.Width, picture.Height))
				{
					var bounds = new Rectangle(Point.Empty, picture.Size);
					var onMs = PaintMs("on", picture, on, bounds);
					picture.SetEnabled(false);
					var offMs = PaintMs("off", picture, off, bounds);
					var back = picture.BackColor;
					int compared = 0, worst = 0;
					for (var y = 0; y < on.Height; y += 2)
						for (var x = 0; x < on.Width; x += 2)
						{
							var full = on.GetPixel(x, y);
							var faded = off.GetPixel(x, y);
							var part = new[] { full.R - back.R, full.G - back.G, full.B - back.B };
							if (part.Max(Math.Abs) < 40)
								continue;
							compared++;
							var half = new[] { back.R + part[0] / 2, back.G + part[1] / 2, back.B + part[2] / 2 };
							worst = Math.Max(worst, new[] { faded.R - half[0], faded.G - half[1], faded.B - half[2] }.Max(Math.Abs));
						}
					Console.WriteLine("{0} pixels of the controller compared; furthest from halfway: {1}", compared, worst);
					Assert.IsTrue(compared > 1000, "Only " + compared + " pixels of the controller differ from the background.");
					Assert.IsTrue(worst <= 3, "A switched-off pixel is " + worst + " away from halfway between the picture and the background.");
					// Both states are one plain draw of a picture made once at its size; a copy made pixel by pixel cost half a second.
					Assert.IsTrue(offMs <= onMs * 3 + 2, "A switched-off paint takes " + offMs.ToString("0.00") + " ms against "
						+ onMs.ToString("0.00") + " ms switched on.");
				}
			});
		}

		/// <summary>Paints the control into the bitmap twenty times and prints the cost of one paint with the machine's state.</summary>
		static double PaintMs(string state, System.Windows.Forms.Control control, Bitmap bitmap, Rectangle bounds)
		{
			control.DrawToBitmap(bitmap, bounds);
			using (var power = new MachinePower())
			{
				var watch = System.Diagnostics.Stopwatch.StartNew();
				for (var i = 0; i < 20; i++)
					control.DrawToBitmap(bitmap, bounds);
				var ms = watch.Elapsed.TotalMilliseconds / 20;
				Console.WriteLine("switched {0}: {1:0.00} ms a paint; {2}", state, ms, power);
				return ms;
			}
		}

		[TestMethod, TestCategory("pad-images"), TestCategory("critical")]
		[Description("A paint GDI+ finds no memory for is skipped and written once, and nothing else is caught")]
		public void A_draw_without_memory_skips_one_paint()
		{
			var text = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Controls", "PadTabPages", "General", "XboxImageUserControl.cs"));
			var paint = Ui.Between(text, "protected override void OnPaint(PaintEventArgs e)", "void DrawGlyph(");
			var tryAt = paint.IndexOf("\ttry");
			var firstDraw = paint.IndexOf("e.Graphics.DrawImage(Painted(");
			var lastDraw = paint.IndexOf("DrawHelpText(e.Graphics);");
			var catchAt = paint.IndexOf("catch (OutOfMemoryException ex)");
			Assert.IsTrue(tryAt > 0 && firstDraw > tryAt && lastDraw > firstDraw && catchAt > lastDraw,
				"A draw that runs out of memory leaves the paint, and the picture is a red cross until the program restarts.");
			Assert.AreEqual(1, Ui.Count(paint, "catch"), "The paint catches more than running out of memory.");
			StringAssert.Contains(paint, "if (!_PaintFaultWritten)", "Every failed paint is written, and the pictures paint ten times a second.");
			Assert.AreEqual(1, Ui.Count(paint, "WriteException(ex);"));
			StringAssert.Contains(text, "static bool _PaintFaultWritten;");
		}
	}
}
