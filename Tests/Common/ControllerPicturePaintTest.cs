// @under-test: App.v4/Controls/PadTabPages/General/XboxImageUserControl.cs
// @area: pad-images   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;
using System.IO;
using x360ce.App.Controls;

namespace x360ce.Tests
{
	/// <summary>The controller picture keeps painting when GDI+ runs short of memory.</summary>
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
