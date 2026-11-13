// @under-test: Engine/JocysCom/Controls/Themes/Default.Forms.cs, Engine/JocysCom/Controls/ControlsHelper.Windows.cs, App.v4/Controls, App.v4/Forms
// @area: ui   @layer: unit
using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.IssuesControl;
using JocysCom.ClassLibrary.Controls.Themes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// The pictures on the buttons and tool bar buttons of every page and window: in the middle of the button's height, inside
	/// the button, and the version drawn for the screen's scale.
	/// </summary>
	[TestClass]
	public class ButtonIconTest
	{
		public TestContext TestContext { get; set; }

		/// <summary>The pages and windows that show a button with a picture, each made as the program makes it.</summary>
		/// <remarks>The Cloud page and the main window's own tool bar are made only by the running program, and are looked at there.</remarks>
		internal static readonly Func<Control>[] Surfaces =
		{
			() => new PadControl(MapTo.Controller1),
			() => new OptionsUserControl(),
			() => new GamesGridUserControl(),
			() => new GameDetailsUserControl(),
			() => new ProgramsGridUserControl(),
			() => new UserDevicesUserControl(),
			() => new XInputDevicesUserControl(),
			() => new SettingsGridUserControl(),
			() => new SummariesGridUserControl(),
			() => new PresetsGridUserControl(),
			() => new IssuesUserControl(),
			() => new LoadPresetsForm(),
		};

		[TestMethod, TestCategory("ui")]
		[Description("Every picture on a button stands inside it, drawn at its own size, with as many rows of the button above it as below, enabled and disabled, in both themes")]
		public void Button_pictures_stand_in_the_middle_of_the_button()
		{
			var report = new List<string>();
			var faults = new List<string>();
			OnEverySurface(Surfaces, (form, when) =>
			{
				foreach (var control in All(form))
				{
					if (control is ButtonBase button && button.Image != null)
						MeasureButton(button, when, report, faults);
					if (control is ToolStrip strip)
						foreach (ToolStripItem item in strip.Items)
							if (item.Image != null && item.Available && item.DisplayStyle != ToolStripItemDisplayStyle.Text)
								MeasureItem(item, when, report, faults);
				}
			});
			foreach (var line in report)
				TestContext.WriteLine(line);
			Assert.AreEqual(0, faults.Count, faults.Count + " picture(s) are off:" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		/// <summary>
		/// Makes each page or window as the program makes it, in both themes, in the program's fonts and pictures, shows it,
		/// and hands its window to <paramref name="measure"/> with the scale and theme it was made in.
		/// </summary>
		/// <remarks>
		/// The test run draws at the scale of the main monitor, as the program does (<see cref="TestRun"/>). A window is shown
		/// off every screen, so nothing appears while it is measured.
		/// </remarks>
		/// <param name="fontChoice">
		/// The interface font, as the Options page's Font list keeps it: empty for the default, Segoe UI with code in Consolas,
		/// the built-in Selawik or an installed family. It is drawn at its default size.
		/// </param>
		/// <param name="themedOnceActive">
		/// True to give a window the theme as the program gives a dialog it opens, when the window first becomes active, after
		/// it has loaded; otherwise it is themed before it is shown, as the main window is.
		/// </param>
		internal static void OnEverySurface(IEnumerable<Func<Control>> surfaces, Action<Form, string> measure, string fontChoice = "", bool themedOnceActive = false)
		{
			Ui.OnUiThread(() =>
			{
				var field = typeof(App.Properties.Resources).GetField("resourceMan", BindingFlags.NonPublic | BindingFlags.Static);
				var before = field.GetValue(null);
				AppFontsTest.LoadFonts();
				FormsTheme.SetFonts(AppFonts.InterfaceFamilyFor(fontChoice), string.IsNullOrEmpty(fontChoice) ? null : AppFonts.CodeFamily, AppFonts.SizeFor(0));
				try
				{
					ThemeResourceManager.Install(typeof(App.Properties.Resources), "x360ce.App.Properties.Icons");
					foreach (var theme in new[] { ThemeType.Light, ThemeType.Dark })
					{
						FormsTheme.SetTheme(theme);
						foreach (var make in surfaces)
							Show(make, string.Format("{0:P0} {1}", ControlsHelper.DpiScale, theme), measure, themedOnceActive);
					}
				}
				finally
				{
					FormsTheme.SetTheme(ThemeType.Light);
					FormsTheme.SetFonts(null, null);
					field.SetValue(null, before);
				}
			});
		}

		/// <summary>Shows a page in a window of its own, or a window as it is, themed and with its pictures, off the screens while it is measured.</summary>
		static void Show(Func<Control> make, string when, Action<Form, string> measure, bool themedOnceActive)
		{
			var made = make();
			var form = made as Form ?? new Form { ClientSize = made.Size };
			form.StartPosition = FormStartPosition.Manual;
			form.Location = new Point(-5000, -5000);
			form.ShowInTaskbar = false;
			try
			{
				if (form != made)
				{
					made.Dock = DockStyle.Fill;
					form.Controls.Add(made);
				}
				// The program draws text with GDI (Application.SetCompatibleTextRenderingDefault(false)), which the test host
				// cannot ask for once a window exists.
				foreach (var button in All(form).OfType<ButtonBase>())
					button.UseCompatibleTextRendering = false;
				if (!themedOnceActive)
					FormsTheme.Apply(form);
				form.Show();
				// As the theme's hook does when the window first becomes active.
				if (themedOnceActive)
					FormsTheme.Apply(form);
				ControlsHelper.ScaleImages(form);
				Application.DoEvents();
				measure(form, when);
			}
			finally
			{
				form.Dispose();
				made.Dispose();
			}
		}

		static void MeasureButton(ButtonBase button, string when, List<string> report, List<string> faults)
		{
			Rectangle image;
			using (var bitmap = new Bitmap(Math.Max(1, button.Width), Math.Max(1, button.Height)))
			using (var g = Graphics.FromImage(bitmap))
			using (var e = new PaintEventArgs(g, button.ClientRectangle))
				Assert.IsTrue(GetButtonLayout(button, e, out image), "The runtime does not show where it puts a button's picture.");
			var name = when + " " + Path(button);
			Check(name, button.ClientRectangle, image, button.Image, button.Image.Size, report, faults);
			if (button.Width <= 0 || button.Height <= 0)
				return;
			var enabled = button.Enabled;
			try
			{
				// A button on a disabled panel cannot be drawn enabled.
				foreach (var on in button.Parent.Enabled ? new[] { true, false } : new[] { false })
				{
					button.Enabled = on;
					Centred(name + (on ? ", enabled" : ", disabled"), button.Image, x => button.Image = x,
						() => Draw(button, button.ClientRectangle), report, faults);
				}
			}
			finally
			{
				button.Enabled = enabled;
			}
		}

		static void MeasureItem(ToolStripItem item, string when, List<string> report, List<string> faults)
		{
			var layout = InternalLayoutProperty.GetValue(item, null);
			var image = (Rectangle)layout.GetType().GetProperty("ImageRectangle", Flags).GetValue(layout, null);
			var shown = item.ImageScaling == ToolStripItemImageScaling.SizeToFit && item.Owner != null ? item.Owner.ImageScalingSize : item.Image.Size;
			var name = when + " " + Path(item.Owner) + "." + (string.IsNullOrEmpty(item.Name) ? item.Text : item.Name);
			Check(name, new Rectangle(Point.Empty, item.Size), image, item.Image, shown, report, faults);
			var strip = item.Owner;
			// An item that does not fit on its tool bar is not drawn there.
			if (strip == null || item.Width <= 0 || item.Height <= 0 || !new Rectangle(Point.Empty, strip.Size).Contains(item.Bounds))
				return;
			var enabled = item.Enabled;
			try
			{
				foreach (var on in strip.Enabled ? new[] { true, false } : new[] { false })
				{
					item.Enabled = on;
					Centred(name + (on ? ", enabled" : ", disabled"), item.Image, x => item.Image = x,
						() => Draw(strip, item.Bounds), report, faults);
				}
			}
			finally
			{
				item.Enabled = enabled;
			}
		}

		/// <summary>The part of a control's drawing given, in a bitmap of its own.</summary>
		static Bitmap Draw(Control control, Rectangle part)
		{
			using (var whole = new Bitmap(control.Width, control.Height))
			{
				control.DrawToBitmap(whole, new Rectangle(Point.Empty, control.Size));
				return whole.Clone(part, whole.PixelFormat);
			}
		}

		/// <summary>
		/// Measures where a button draws its picture, and reports a fault unless the picture is drawn at its own size, as many
		/// rows of the button above it as below it.
		/// </summary>
		/// <remarks>
		/// The button is drawn with a picture of the same size in one colour, opaque all over, and with a clear one: the two
		/// drawings differ only where the picture stands, which gives its box, whatever shows of the picture itself.
		/// </remarks>
		/// <param name="name">What the button is, for the report.</param>
		/// <param name="picture">The button's picture, given back when measured.</param>
		/// <param name="give">Gives the button a picture.</param>
		/// <param name="draw">Draws the button, its face from the top of the bitmap to the bottom.</param>
		static void Centred(string name, Image picture, Action<Image> give, Func<Bitmap> draw, List<string> report, List<string> faults)
		{
			try
			{
				using (var clear = new Bitmap(picture.Width, picture.Height))
				{
					give(clear);
					using (var behind = draw())
					{
						// The face's colour, from the middle of its top edge, where nothing is drawn; the mark in the colour farthest from it.
						var face = behind.GetPixel(behind.Width / 2, 2);
						using (var mark = new Bitmap(picture.Width, picture.Height))
						{
							using (var g = Graphics.FromImage(mark))
								g.Clear(face.GetBrightness() > 0.5f ? Color.Black : Color.White);
							give(mark);
							using (var drawn = draw())
							{
								var rows = new bool[drawn.Height];
								for (var y = 0; y < drawn.Height; y++)
									for (var x = 0; x < drawn.Width && !rows[y]; x++)
										rows[y] = drawn.GetPixel(x, y).ToArgb() != behind.GetPixel(x, y).ToArgb();
								var top = Array.IndexOf(rows, true);
								var bottom = Array.LastIndexOf(rows, true);
								var last = drawn.Height - 1;
								var line = top < 0
									? string.Format("{0}: face {1} px, the picture is not drawn", name, drawn.Height)
									: string.Format("{0}: face {1} px, picture {2} px drawn on rows {3} to {4} ({5} px), {6} rows above, {7} below",
										name, drawn.Height, picture.Height, top, bottom, bottom - top + 1, top, last - bottom);
								report.Add(line);
								if (top >= 0 && (bottom - top + 1 != picture.Height || top != last - bottom))
									faults.Add(line);
							}
						}
					}
				}
			}
			finally
			{
				give(picture);
			}
		}

		static void Check(string name, Rectangle client, Rectangle image, Image picture, Size shown, List<string> report, List<string> faults)
		{
			var inside = client.Contains(image);
			var original = ControlsHelper.GetOriginal(picture);
			var wanted = new Size((int)Math.Round(original.Width * ControlsHelper.DpiScale / 2), (int)Math.Round(original.Height * ControlsHelper.DpiScale / 2));
			var drawn = picture.Size == shown && picture.Size == wanted;
			var line = string.Format("{0}: client {1}, image {2}, inside {3}, picture {4} shown {5} drawn {6}",
				name, client, image, inside, picture.Size, shown, drawn);
			report.Add(line);
			if (!inside || !drawn)
				faults.Add(line);
		}

		const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		static readonly PropertyInfo AdapterProperty = typeof(ButtonBase).GetProperty("Adapter", Flags);
		static readonly PropertyInfo InternalLayoutProperty = typeof(ToolStripItem).GetProperty("InternalLayout", Flags);

		/// <summary>
		/// The place Windows Forms' layout centres a button's picture in when it paints the button raised.
		/// </summary>
		/// <remarks>
		/// The adapter's Layout method lays the button out pressed, its text a pixel down and right; the layout a button is
		/// painted raised with is the one its Paint...Layout method makes with up set. Laid out as .NET Framework 1.1 laid a
		/// button out, the picture's place starts where the layout says it starts, and Windows Forms draws it a pixel right of
		/// and below that.
		/// </remarks>
		static bool GetButtonLayout(ButtonBase button, PaintEventArgs e, out Rectangle image)
		{
			image = Rectangle.Empty;
			var adapter = AdapterProperty?.GetValue(button, null);
			var method = adapter?.GetType().GetMethods(Flags)
				.FirstOrDefault(x => x.Name.StartsWith("Paint") && x.Name.EndsWith("Layout") && !x.IsStatic && x.GetParameters().Any(p => p.Name == "up"));
			Assert.IsNotNull(method, "The runtime's " + adapter?.GetType().Name + " has no layout to paint a raised button with: "
				+ string.Join(", ", adapter?.GetType().GetMethods(Flags).Where(x => x.Name.Contains("Layout")).Select(x => x.ToString()) ?? new string[0]));
			var arguments = method.GetParameters().Select(p =>
				p.ParameterType == typeof(PaintEventArgs) ? e
				: p.Name == "up" ? true
				: p.Name == "borderSize" ? button.FlatAppearance.BorderSize
				: p.ParameterType == typeof(bool) ? false
				: p.ParameterType == typeof(int) ? (object)0
				: null).ToArray();
			var options = method.Invoke(adapter, arguments);
			var data = options?.GetType().GetMethod("Layout", Flags, null, Type.EmptyTypes, null)?.Invoke(options, null);
			var type = data?.GetType();
			var imageField = type?.GetField("imageBounds", Flags) ?? type?.GetField("ImageBounds", Flags);
			if (imageField == null)
				return false;
			var place = (Rectangle)imageField.GetValue(data);
			var compat = options.GetType().GetField("everettButtonCompat", Flags) ?? options.GetType().GetField("DotNetOneButtonCompat", Flags);
			var start = type.GetField("imageStart", Flags) ?? type.GetField("ImageStart", Flags);
			image = compat != null && start != null && (bool)compat.GetValue(options)
				? new Rectangle((Point)start.GetValue(data), button.Image.Size)
				: Rectangle.Intersect(new Rectangle(place.Location, button.Image.Size), place);
			return true;
		}

		internal static string Path(Control control)
		{
			var names = new List<string>();
			for (var c = control; c != null && !(c is Form); c = c.Parent)
				if (!string.IsNullOrEmpty(c.Name))
					names.Insert(0, c.Name);
			return string.Join(".", names);
		}

		internal static IEnumerable<Control> All(Control parent)
		{
			foreach (Control child in parent.Controls)
			{
				yield return child;
				foreach (var grandchild in All(child))
					yield return grandchild;
			}
		}
	}
}
