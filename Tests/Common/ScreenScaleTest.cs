// @under-test: App.v4/MainForm.cs, App.v4/Forms, App.v4/Program.cs, Engine/JocysCom/Controls/ControlsHelper.Windows.cs
// @area: ui   @layer: unit
using JocysCom.ClassLibrary.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace x360ce.Tests
{
	/// <summary>
	/// The program on a screen set above 100 %: every window drawn at the screen's scale, and sharp.
	/// </summary>
	/// <remarks>
	/// The program tells Windows it knows the screen's scale, so Windows no longer stretches its windows,
	/// which blurred them, and leaves each window to enlarge itself. A window enlarges what its designer
	/// recorded only when it is told how to, and it leaves images at the size they were made. These are
	/// the parts of that which can be checked without a screen at another scale.
	/// </remarks>
	[TestClass]
	public class ScreenScaleTest
	{
		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("Every window the designer draws is told to enlarge itself with the screen")]
		public void Every_window_scales_with_the_screen()
		{
			var app = Path.Combine(Ui.RepoRoot.FullName, "App.v4");
			var unscaled = Directory.GetFiles(app, "*.Designer.cs", SearchOption.AllDirectories)
				.Where(x => !x.Contains(@"\bin\") && !x.Contains(@"\obj\"))
				.Where(IsWindow)
				// The base only carries the header; the windows made from it say how they scale.
				.Where(x => Path.GetFileName(x) != "BaseFormWithHeader.Designer.cs")
				.Where(x => !Regex.IsMatch(File.ReadAllText(x), @"AutoScaleMode = System\.Windows\.Forms\.AutoScaleMode\.(Font|Dpi)"))
				.Select(x => x.Substring(app.Length + 1))
				.ToList();
			Assert.AreEqual(0, unscaled.Count,
				"Drawn at 100 % and shown at the screen's scale, these windows cut their text off: " +
				string.Join(", ", unscaled));
		}

		/// <summary>True when the designer file belongs to a window rather than to a control placed on one.</summary>
		static bool IsWindow(string designer)
		{
			var code = designer.Substring(0, designer.Length - ".Designer.cs".Length) + ".cs";
			return File.Exists(code) && Regex.IsMatch(File.ReadAllText(code),
				@"class \w+\s*:\s*(System\.Windows\.Forms\.)?(Form|BaseFormWithHeader)\b");
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("No window says how it scales in its own code, where it would undo what its designer code says")]
		public void No_window_sets_its_scaling_outside_the_designer()
		{
			// The main window once set its scaling before its designer code ran. The designer's setting
			// then threw away the size the window was drawn at, so the window was never enlarged at all
			// and could be shrunk to two thirds of its smallest size. A window built in code has no
			// designer, and says how it scales in its constructor.
			var app = Path.Combine(Ui.RepoRoot.FullName, "App.v4");
			var builtInCode = new[] { "HelpForm.cs" };
			var offenders = Directory.GetFiles(app, "*.cs", SearchOption.AllDirectories)
				.Where(x => !x.Contains(@"\bin\") && !x.Contains(@"\obj\") && !x.EndsWith(".Designer.cs"))
				.Where(x => !builtInCode.Contains(Path.GetFileName(x)))
				.Where(x => Regex.IsMatch(File.ReadAllText(x), @"^\s*(this\.)?AutoScale(Mode|Dimensions)\s*=", RegexOptions.Multiline))
				.Select(x => x.Substring(app.Length + 1))
				.ToList();
			Assert.AreEqual(0, offenders.Count, "Scaling set outside the designer in: " + string.Join(", ", offenders));
		}

		[TestMethod, TestCategory("ui")]
		[Description("The program's image file keeps its manager where the program puts the one that enlarges images")]
		public void Image_manager_can_be_replaced()
		{
			var field = typeof(App.Properties.Resources).GetField("resourceMan", BindingFlags.NonPublic | BindingFlags.Static);
			Assert.IsNotNull(field, "Properties.Resources has no resourceMan field, so its images are drawn at 100 % on every screen.");
			Assert.AreEqual(typeof(System.Resources.ResourceManager), field.FieldType);
		}

		[TestMethod, TestCategory("ui")]
		[Description("An image is made at each size once, always from its original, with hard edges and no faded border")]
		public void Images_are_enlarged_once_from_the_original()
		{
			var original = new Bitmap(16, 16);
			using (var g = Graphics.FromImage(original))
			{
				g.Clear(Color.White);
				g.FillRectangle(Brushes.Black, 0, 0, 8, 16);
			}
			var copy = (Bitmap)ControlsHelper.ScaleImage(original, new Size(24, 24));
			Assert.AreEqual(new Size(24, 24), copy.Size);
			Assert.AreSame(copy, ControlsHelper.ScaleImage(original, new Size(24, 24)), "The same size was made twice.");
			Assert.AreSame(original, ControlsHelper.GetOriginal(copy));
			Assert.AreSame(original, ControlsHelper.ScaleImage(copy, new Size(16, 16)), "A copy asked for at the original's size is the original.");
			Assert.AreSame(original, ControlsHelper.GetOriginal(ControlsHelper.ScaleImage(copy, new Size(32, 32))),
				"A copy was made from another copy, which blurs it twice.");
			// The edge between the two halves stays within a pixel or two, where enlarging straight
			// to 150 % spreads it over three; the outermost pixels are not mixed with the outside.
			var grey = Enumerable.Range(0, 24).Count(x => { var c = copy.GetPixel(x, 12); return c.R > 25 && c.R < 230; });
			Assert.IsTrue(grey <= 2, grey + " pixels across the edge are neither black nor white.");
			foreach (var corner in new[] { copy.GetPixel(0, 0), copy.GetPixel(23, 0), copy.GetPixel(0, 23), copy.GetPixel(23, 23) })
				Assert.AreEqual(255, corner.A, "A corner faded into the outside of the picture.");
		}

		[TestMethod, TestCategory("ui")]
		[Description("An image drawn at larger sizes is handed out as drawn, and reduced from the nearest larger rather than enlarged")]
		public void Drawn_sizes_are_used_before_enlarging()
		{
			// Each size is one colour, so a copy shows which one it was made from.
			var original = Filled(16, Color.Red);
			var at24 = Filled(24, Color.Lime);
			var at32 = Filled(32, Color.Blue);
			ControlsHelper.SetDrawnSizes(original, at32, at24);
			Assert.AreSame(at24, ControlsHelper.ScaleImage(original, new Size(24, 24)), "The version drawn at 24 pixels was not used.");
			Assert.AreSame(at32, ControlsHelper.ScaleImage(original, new Size(32, 32)), "The version drawn at 32 pixels was not used.");
			Assert.AreSame(original, ControlsHelper.GetOriginal(at24), "A drawn version is not known as a copy, so it could be enlarged again.");
			CollectionAssert.AreEqual(new Image[] { at24, at32 }, ControlsHelper.GetDrawnSizes(at24));
			var at20 = (Bitmap)ControlsHelper.ScaleImage(original, new Size(20, 20));
			Assert.AreSame(original, ControlsHelper.GetOriginal(at20));
			Assert.AreEqual(Color.Lime.ToArgb(), at20.GetPixel(10, 10).ToArgb(), "20 pixels was not reduced from the version drawn at 24.");
			var at48 = (Bitmap)ControlsHelper.ScaleImage(original, new Size(48, 48));
			Assert.AreEqual(Color.Blue.ToArgb(), at48.GetPixel(24, 24).ToArgb(), "48 pixels was not enlarged from the largest version.");
		}

		static Bitmap Filled(int size, Color color)
		{
			var bitmap = new Bitmap(size, size);
			using (var g = Graphics.FromImage(bitmap))
				g.Clear(color);
			return bitmap;
		}
	}
}
