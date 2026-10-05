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
				using (var picture = x360ce.Engine.EngineHelper.GetResourcePicture("Images.xboxController" + name + ".png"))
				{
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
		}

		[TestMethod, TestCategory("pad-images"), TestCategory("critical")]
		[Description("A paint GDI+ finds no memory for is skipped and written once, and nothing else is caught")]
		public void A_draw_without_memory_skips_one_paint()
		{
			var text = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Controls", "PadTabPages", "General", "XboxImageUserControl.cs"));
			var paint = Ui.Between(text, "protected override void OnPaint(PaintEventArgs e)", "void DrawGlyph(");
			var tryAt = paint.IndexOf("\ttry");
			var firstDraw = paint.IndexOf("e.Graphics.DrawImage(");
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
