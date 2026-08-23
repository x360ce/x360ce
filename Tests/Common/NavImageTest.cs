// @under-test: App.v4/Controls/PadTabPages/General/NavImages.cs, Engine/JocysCom/Controls/ControlsHelper.Windows.cs
// @area: pad-images   @layer: unit
using JocysCom.ClassLibrary.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace x360ce.Tests
{
	/// <summary>
	/// The navigation glyphs replaced vector artwork that used to scale to any size. A bitmap
	/// cannot do that, so the masters are kept at the size the artwork was drawn at and are only
	/// ever scaled down. The picture is 256 by 289 today, which puts a glyph at 18 points and
	/// about 27 device pixels at 150 percent scaling, but the control scales with its container
	/// and display scaling goes higher, so the headroom is deliberate.
	/// </summary>
	[TestClass]
	public class NavImageTest
	{

		/// <summary>Size the artwork was drawn at, and the floor for every master.</summary>
		const int AuthoredSize = 512;

		/// <summary>Directions times states, plus the two axis and trigger indicators.</summary>
		const int ExpectedCount = 17;

		static DirectoryInfo GlyphFolder()
		{
			return new DirectoryInfo(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Images", "Nav"));
		}

		[TestMethod, TestCategory("pad-images"), TestCategory("smoke")]
		[Description("Every glyph master is at least the size the artwork was drawn at")]
		public void Nav_glyph_masters_are_high_resolution()
		{
			var folder = GlyphFolder();
			Assert.IsTrue(folder.Exists, "Glyph folder is missing: " + folder.FullName);
			var files = folder.GetFiles("*.png");
			Assert.AreEqual(ExpectedCount, files.Length,
				"Expected " + ExpectedCount + " glyphs, found " + files.Length + ".");
			foreach (var file in files)
			{
				using (var image = Image.FromFile(file.FullName))
				{
					Assert.AreEqual(image.Width, image.Height, file.Name + " is not square.");
					Assert.IsTrue(image.Width >= AuthoredSize,
						file.Name + " is only " + image.Width + " pixels. Masters must be at least " +
						AuthoredSize + " so they are always scaled down, never up.");
				}
			}
		}

		[TestMethod, TestCategory("pad-images"), TestCategory("smoke")]
		[Description("Glyphs keep their transparency, so the controller shows through")]
		public void Nav_glyphs_have_an_alpha_channel()
		{
			foreach (var file in GlyphFolder().GetFiles("*.png"))
			{
				using (var image = Image.FromFile(file.FullName))
					Assert.IsTrue(Image.IsAlphaPixelFormat(image.PixelFormat),
						file.Name + " has no alpha channel, so it would draw a solid box over the controller.");
			}
		}

		[TestMethod, TestCategory("pad-images"), TestCategory("smoke")]
		[Description("Each state of each direction ships, so no mapping draws blank")]
		public void Every_direction_ships_in_every_state()
		{
			var names = GlyphFolder().GetFiles("*.png").Select(x => Path.GetFileNameWithoutExtension(x.Name)).ToArray();
			foreach (var direction in new[] { "", "Up", "Down", "Left", "Right" })
				foreach (var state in new[] { "Normal", "Active", "Record" })
				{
					var expected = "Nav" + direction + state;
					CollectionAssert.Contains(names, expected, expected + ".png is missing.");
				}
			CollectionAssert.Contains(names, "NavAxisActive", "The thumbstick indicator is missing.");
			CollectionAssert.Contains(names, "NavTriggerActive", "The trigger indicator is missing.");
		}

		[TestMethod, TestCategory("pad-images"), TestCategory("smoke")]
		[Description("Drawing at partial opacity scales the alpha channel")]
		public void Drawing_with_opacity_scales_the_alpha_channel()
		{
			using (var source = new Bitmap(8, 8, PixelFormat.Format32bppArgb))
			{
				using (var g = Graphics.FromImage(source))
					g.Clear(Color.FromArgb(255, 200, 30, 30));

				Assert.AreEqual(255, Alpha(source, 0.0f, draw: false), "control: the surface starts empty");
				Assert.AreEqual(255, Alpha(source, 1.0f), "fully opaque should stay opaque");

				var half = Alpha(source, 0.5f);
				Assert.IsTrue(half > 100 && half < 155,
					"Half opacity produced alpha " + half + ", which is not about half of 255.");

				var faint = Alpha(source, 0.2f);
				Assert.IsTrue(faint < half,
					"Lower opacity produced alpha " + faint + ", which is not fainter than " + half + ".");
			}
		}

		[TestMethod, TestCategory("pad-images"), TestCategory("smoke")]
		[Description("Zero opacity draws nothing at all")]
		public void Drawing_with_zero_opacity_draws_nothing()
		{
			using (var source = new Bitmap(8, 8, PixelFormat.Format32bppArgb))
			{
				using (var g = Graphics.FromImage(source))
					g.Clear(Color.FromArgb(255, 200, 30, 30));
				Assert.AreEqual(0, Alpha(source, 0f),
					"An invisible glyph still marked the surface.");
			}
		}

		/// <summary>Alpha at the centre of a transparent surface after drawing the source onto it.</summary>
		static int Alpha(Bitmap source, float opacity, bool draw = true)
		{
			using (var target = new Bitmap(8, 8, PixelFormat.Format32bppArgb))
			{
				using (var g = Graphics.FromImage(target))
				{
					g.Clear(Color.Transparent);
					if (draw)
						ControlsHelper.DrawImageWithOpacity(g, source, new Rectangle(0, 0, 8, 8), opacity);
					else
						g.Clear(Color.FromArgb(255, 0, 0, 0));
				}
				return target.GetPixel(4, 4).A;
			}
		}

	}
}
