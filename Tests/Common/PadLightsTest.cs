// @under-test: App.v4/Controls/PadTabPages/General/PadControlImager.cs, App.v4/Controls/PadTabPages/General/XboxImageUserControl.cs, App.v4/Controls/PadTabPages/General/NavImages.cs, App.v4/Controls/PadTabPages/General/InputUserControl.cs, App.v4/Controls/PadControl.cs, App.v4/Common/NavImageType.cs
// @area: pad-images   @layer: unit
using JocysCom.ClassLibrary.Controls.Themes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// A mapping lights its box beside the controller picture and its mark on the picture together, in one colour, by one
	/// rule: red while it is recorded, yellow while the pointer is on either, green while the control its box names moves.
	/// </summary>
	[TestClass]
	public class PadLightsTest
	{
		public TestContext TestContext { get; set; }

		static readonly bool[] Both = { false, true };

		[TestMethod, TestCategory("pad-images"), TestCategory("smoke")]
		[Description("Recording outranks the pointer, the pointer outranks movement and movement outranks a mark only shown, in every combination; a box takes its state's colour, and none for a mark only shown")]
		public void Recording_outranks_the_pointer_and_the_pointer_outranks_movement()
		{
			foreach (var recording in Both)
				foreach (var pointed in Both)
					foreach (var moving in Both)
						foreach (var shown in Both)
						{
							NavImageType? expected = null;
							if (shown)
								expected = NavImageType.Normal;
							if (moving)
								expected = NavImageType.Active;
							if (pointed)
								expected = NavImageType.Over;
							if (recording)
								expected = NavImageType.Record;
							var light = PadControlImager.Light(recording, pointed, moving, shown);
							Assert.AreEqual(expected, light, string.Format("Recording {0}, pointed {1}, moving {2}, shown {3}.", recording, pointed, moving, shown));
							var box = PadControlImager.BoxColor(light);
							if (light == null || light == NavImageType.Normal)
								Assert.IsTrue(box.IsEmpty, "A box is lit for " + (light == null ? "nothing" : "a mark only shown") + ".");
							else
								Assert.AreEqual(PadControlImager.LightColor(light.Value).ToArgb(), box.ToArgb(), "A box is lit in another colour than its mark, " + light + ".");
						}
		}

		[TestMethod, TestCategory("pad-images")]
		[Description("The lights are the theme's: red #FF6B66, yellow #FFCC66 and green #42C765 in the light theme and Default_DarkTheme.xaml's in the dark, each a colour of its own, with black text on each readable")]
		public void Lights_take_the_theme_colours()
		{
			var light = new Dictionary<NavImageType, string>
			{
				{ NavImageType.Record, "#FF6B66" }, { NavImageType.Over, "#FFCC66" }, { NavImageType.Active, "#42C765" }, { NavImageType.Normal, "#6699FF" },
			};
			var keys = new Dictionary<NavImageType, string>
			{
				{ NavImageType.Record, "ColorRecord" }, { NavImageType.Over, "ColorOver" }, { NavImageType.Active, "ColorActive" }, { NavImageType.Normal, "ColorNormal" },
			};
			Ui.OnUiThread(() =>
			{
				try
				{
					foreach (var theme in new[] { ThemeType.Light, ThemeType.Dark })
					{
						FormsTheme.SetTheme(theme);
						var colours = keys.Keys.ToDictionary(x => x, PadControlImager.LightColor);
						foreach (var pair in colours)
						{
							var expected = theme == ThemeType.Light ? ColorTranslator.FromHtml(light[pair.Key]) : FormsTheme.Palette[keys[pair.Key]];
							Assert.AreEqual(expected.ToArgb(), pair.Value.ToArgb(), theme + ": " + pair.Key + " is not the theme's " + keys[pair.Key] + ".");
							if (pair.Key == NavImageType.Normal)
								continue;
							var contrast = TextColourTest.Contrast(FormsTheme.LightPalette["ForegroundBlack"], pair.Value);
							TestContext.WriteLine("{0} {1}: {2}, black text {3:0.0}:1", theme, pair.Key, ColorTranslator.ToHtml(pair.Value), contrast);
							Assert.IsTrue(contrast >= 4.5, theme + ": the text of a box lit " + pair.Key + " has a contrast of " + contrast.ToString("0.0") + ".");
						}
						Assert.AreEqual(colours.Count, colours.Values.Select(x => x.ToArgb()).Distinct().Count(), theme + ": two states share a colour.");
					}
				}
				finally
				{
					FormsTheme.SetTheme(ThemeType.Light);
				}
			});
		}

		[TestMethod, TestCategory("pad-images"), TestCategory("ui")]
		[Description("The pointer on a mapping box lights the box and its mark on the controller picture yellow, the pointer on the mark lights the mark and its box yellow, and the pointer gone puts both back, in both themes")]
		public void The_pointer_on_a_box_or_its_mark_lights_both()
		{
			OnGeneralPage((pad, picture, where) =>
			{
				var a = Box(pad, MapCode.ButtonA);
				var b = Box(pad, MapCode.ButtonB);
				using (var bare = PadGeometryTest.Draw(picture))
				{
					Ui.Hover(a, new Point(a.Width / 2, a.Height / 2));
					AssertLit(where + "The pointer on the A box", pad, picture, bare, MapCode.ButtonA, NavImageType.Over);
					AssertUnlit(where + "The pointer on the A box", pad, picture, bare, MapCode.ButtonB);
					// The pointer is not on the box: Windows tells it so once its messages are read.
					Application.DoEvents();
					AssertUnlit(where + "The pointer gone from the A box", pad, picture, bare, MapCode.ButtonA);
					Ui.Hover(picture, Point.Round(MarkMiddle(picture, MapCode.ButtonA)));
					AssertLit(where + "The pointer on the A mark", pad, picture, bare, MapCode.ButtonA, NavImageType.Over);
					// The marks near the pointer are shown, and their boxes stay as they are.
					Assert.IsTrue(pad._Imager.GetBoxColor(b).IsEmpty, where + "The B box is lit while only its mark is shown.");
					AssertMark(where + "The pointer on the A mark", picture, bare, MapCode.ButtonB, PadControlImager.LightColor(NavImageType.Normal));
					Application.DoEvents();
					AssertUnlit(where + "The pointer gone from the picture", pad, picture, bare, MapCode.ButtonA);
				}
			});
		}

		[TestMethod, TestCategory("pad-images"), TestCategory("ui")]
		[Description("A mapping box and its mark on the controller picture light in one state: green while the control the box names is lit in the Source Input panel, yellow when the pointer is on it too, red when it is recorded too, in both themes")]
		public void A_box_and_its_mark_light_in_the_same_state()
		{
			OnGeneralPage((pad, picture, where) =>
			{
				var a = Box(pad, MapCode.ButtonA);
				a.Items.Add("Button 1");
				a.SelectedIndex = 0;
				var state = new SourceState();
				var device = new UserDevice { InstanceGuid = Guid.NewGuid(), CapButtonCount = 2, SourceState = state };
				using (var bare = PadGeometryTest.Draw(picture))
				{
					pad.InputPanel.UpdateFrom(device);
					pad._Imager.UpdateLights();
					AssertUnlit(where + "Button 1 at rest", pad, picture, bare, MapCode.ButtonA);
					state.Buttons[0] = true;
					pad.InputPanel.UpdateFrom(device);
					pad._Imager.UpdateLights();
					AssertLit(where + "Button 1 pressed", pad, picture, bare, MapCode.ButtonA, NavImageType.Active);
					Ui.Hover(a, new Point(a.Width / 2, a.Height / 2));
					AssertLit(where + "Button 1 pressed, the pointer on the A box", pad, picture, bare, MapCode.ButtonA, NavImageType.Over);
					pad._Imager.Recorder.StartRecording(new SettingsMapItem { Code = MapCode.ButtonA, Control = a, MapTo = MapTo.Controller1 });
					try
					{
						pad._Imager.UpdateLights();
						AssertLit(where + "Button 1 pressed, the pointer on the A box, A recorded", pad, picture, bare, MapCode.ButtonA, NavImageType.Record);
					}
					finally
					{
						pad._Imager.Recorder.Recording = false;
					}
				}
			});
		}

		[TestMethod, TestCategory("pad-images"), TestCategory("ui")]
		[Description("While a chip of the Source Input panel is dragged, the mapping boxes it may land in are drawn green and their marks stay off; when it lands they are drawn as before, in both themes")]
		public void The_boxes_a_dragged_chip_may_land_in_turn_green()
		{
			OnGeneralPage((pad, picture, where) =>
			{
				var green = PadControlImager.LightColor(NavImageType.Active);
				using (var bare = PadGeometryTest.Draw(picture))
				{
					pad._Imager.Dropping = true;
					pad._Imager.UpdateLights();
					foreach (var code in new[] { MapCode.ButtonA, MapCode.LeftTrigger })
					{
						var box = Box(pad, code);
						Assert.AreEqual(green.ToArgb(), pad._Imager.GetBoxColor(box).ToArgb(), where + "the " + code + " box is not lit while a chip is dragged.");
						using (var drawn = PadGeometryTest.Draw(box))
						{
							var face = drawn.GetPixel(box.Width - box.Height * 3 / 2, box.Height / 2);
							Assert.AreEqual(green.ToArgb(), face.ToArgb(), where + "the " + code + " box is drawn " + ColorTranslator.ToHtml(face) + " while a chip is dragged.");
						}
						var middle = Point.Round(MarkMiddle(picture, code));
						using (var drawn = PadGeometryTest.Draw(picture))
							Assert.AreEqual(bare.GetPixel(middle.X, middle.Y).ToArgb(), drawn.GetPixel(middle.X, middle.Y).ToArgb(), where + "the " + code + " mark is drawn while a chip is dragged.");
					}
					pad._Imager.Dropping = false;
					pad._Imager.UpdateLights();
					AssertUnlit(where + "the chip landed", pad, picture, bare, MapCode.ButtonA);
				}
			});
		}

		/// <summary>Shows a controller page in each theme, off the screens, and hands over the page and its picture.</summary>
		static void OnGeneralPage(Action<PadControl, XboxImageUserControl, string> check)
		{
			Ui.OnUiThread(() =>
			{
				try
				{
					foreach (var theme in new[] { ThemeType.Light, ThemeType.Dark })
					{
						FormsTheme.SetTheme(theme);
						using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
						using (var pad = new PadControl(MapTo.Controller1) { Dock = DockStyle.Fill })
						{
							form.ClientSize = pad.Size;
							form.Controls.Add(pad);
							FormsTheme.Apply(form);
							form.Show();
							Application.DoEvents();
							var picture = (XboxImageUserControl)pad.Controls.Find("XboxImage", true).Single();
							check(pad, picture, theme + ": ");
						}
					}
				}
				finally
				{
					FormsTheme.SetTheme(ThemeType.Light);
				}
			});
		}

		static ComboBox Box(PadControl pad, MapCode code)
		{
			return (ComboBox)pad.imageInfos.First(x => x.Code == code).Control;
		}

		/// <summary>Checks a mapping's box and its mark are both lit in a state's colour, as drawn.</summary>
		static void AssertLit(string when, PadControl pad, XboxImageUserControl picture, Bitmap bare, MapCode code, NavImageType state)
		{
			var colour = PadControlImager.LightColor(state);
			var box = Box(pad, code);
			Assert.AreEqual(colour.ToArgb(), pad._Imager.GetBoxColor(box).ToArgb(), when + ": the " + code + " box is not lit " + state + ".");
			using (var drawn = PadGeometryTest.Draw(box))
			{
				// The face beside the text, which is empty or short, and before the triangle.
				var face = drawn.GetPixel(box.Width - box.Height * 3 / 2, box.Height / 2);
				Assert.AreEqual(colour.ToArgb(), face.ToArgb(), when + ": the " + code + " box is drawn " + ColorTranslator.ToHtml(face)
					+ ", not " + state + " " + ColorTranslator.ToHtml(colour) + ".");
			}
			AssertMark(when, picture, bare, code, colour);
		}

		/// <summary>Checks a mapping's box is drawn as the theme draws it and its mark is not drawn.</summary>
		static void AssertUnlit(string when, PadControl pad, XboxImageUserControl picture, Bitmap bare, MapCode code)
		{
			Assert.IsTrue(pad._Imager.GetBoxColor(Box(pad, code)).IsEmpty, when + ": the " + code + " box is lit.");
			var middle = Point.Round(MarkMiddle(picture, code));
			using (var drawn = PadGeometryTest.Draw(picture))
				Assert.AreEqual(bare.GetPixel(middle.X, middle.Y).ToArgb(), drawn.GetPixel(middle.X, middle.Y).ToArgb(), when + ": the " + code + " mark is drawn.");
		}

		/// <summary>Checks a mark is drawn in a colour: the picture under its middle, seen through it, gives that colour back.</summary>
		static void AssertMark(string when, XboxImageUserControl picture, Bitmap bare, MapCode code, Color colour)
		{
			var middle = Point.Round(MarkMiddle(picture, code));
			using (var drawn = PadGeometryTest.Draw(picture))
			{
				var over = drawn.GetPixel(middle.X, middle.Y);
				var under = bare.GetPixel(middle.X, middle.Y);
				// A shown mark is drawn at 80 % over the picture.
				Func<int, int, int> mark = (o, u) => Math.Max(0, Math.Min(255, (int)Math.Round((o - 0.2 * u) / 0.8)));
				var seen = Color.FromArgb(mark(over.R, under.R), mark(over.G, under.G), mark(over.B, under.B));
				var off = new[] { seen.R - colour.R, seen.G - colour.G, seen.B - colour.B }.Max(x => Math.Abs(x));
				Assert.IsTrue(off <= 6, when + ": the " + code + " mark is drawn " + ColorTranslator.ToHtml(seen) + ", not " + ColorTranslator.ToHtml(colour) + ".");
			}
		}

		/// <summary>The middle of the place on the picture where the pointer is on a mapping's mark.</summary>
		static PointF MarkMiddle(XboxImageUserControl picture, MapCode code)
		{
			var key = picture.Size + " " + code;
			PointF middle;
			if (!Middles.TryGetValue(key, out middle))
				Middles[key] = middle = FindMarkMiddle(picture, code);
			return middle;
		}

		/// <summary>Each mark's middle, by the size of the picture and the mapping: the same picture puts it in the same place.</summary>
		static readonly Dictionary<string, PointF> Middles = new Dictionary<string, PointF>();

		static PointF FindMarkMiddle(XboxImageUserControl picture, MapCode code)
		{
			var points = new List<Point>();
			for (var y = 0; y < picture.Height; y++)
				for (var x = 0; x < picture.Width; x++)
				{
					var info = picture.HitTest(new Point(x, y));
					if (info != null && info.Code == code)
						points.Add(new Point(x, y));
				}
			Assert.IsTrue(points.Count > 0, "No place on the picture is the " + code + " mark.");
			return new PointF((float)points.Average(p => p.X), (float)points.Average(p => p.Y));
		}
	}
}
