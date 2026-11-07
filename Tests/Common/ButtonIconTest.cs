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
	/// The pictures on the buttons and tool bar buttons of every page and window: centred on the height of the button's text,
	/// inside the button, and the version drawn for the screen's scale.
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
		[Description("Every picture on a button stands centred on the height of the button's text and inside the button, in both themes")]
		public void Button_pictures_are_centred_on_their_text_and_inside_the_button()
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
		/// The interface font, as the Options page's Font list keeps it: empty for the default, Windows' own, the built-in
		/// Selawik or an installed family. It is drawn at its default size.
		/// </param>
		internal static void OnEverySurface(IEnumerable<Func<Control>> surfaces, Action<Form, string> measure, string fontChoice = "")
		{
			Ui.OnUiThread(() =>
			{
				var field = typeof(App.Properties.Resources).GetField("resourceMan", BindingFlags.NonPublic | BindingFlags.Static);
				var before = field.GetValue(null);
				AppFontsTest.LoadFonts();
				FormsTheme.SetFonts(AppFonts.InterfaceFamilyFor(fontChoice), AppFonts.CodeFamily, AppFonts.SizeFor(fontChoice, 0));
				try
				{
					ThemeResourceManager.Install(typeof(App.Properties.Resources), "x360ce.App.Properties.Icons");
					foreach (var theme in new[] { ThemeType.Light, ThemeType.Dark })
					{
						FormsTheme.SetTheme(theme);
						foreach (var make in surfaces)
							Show(make, string.Format("{0:P0} {1}", ControlsHelper.DpiScale, theme), measure);
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
		static void Show(Func<Control> make, string when, Action<Form, string> measure)
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
				FormsTheme.Apply(form);
				form.Show();
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
			Rectangle text, image;
			using (var bitmap = new Bitmap(Math.Max(1, button.Width), Math.Max(1, button.Height)))
			using (var g = Graphics.FromImage(bitmap))
			using (var e = new PaintEventArgs(g, button.ClientRectangle))
				Assert.IsTrue(GetButtonLayout(button, e, out text, out image), "The runtime does not show where it puts a button's picture.");
			var name = when + " " + Path(button);
			var withText = !string.IsNullOrEmpty(button.Text);
			Check(name, button.ClientRectangle, image, withText ? text : Rectangle.Empty, button.Image, button.Image.Size, report, faults);
			if (!withText || button.Width <= 0 || button.Height <= 0)
				return;
			// The picture is drawn where the layout centres it, and drawn enabled and disabled, the picture and the text stand
			// in the same place.
			var enabled = button.Enabled;
			try
			{
				button.Enabled = true;
				var up = Ink(button, image, text, false);
				var alone = Ink(button, image, text, true);
				button.Enabled = false;
				var off = Ink(button, image, text, false);
				var offAlone = Ink(button, image, text, true);
				var line = string.Format("{0}: picture alone at {1:0.0},{2:0.0}; drawn enabled, picture at {3:0.0},{4:0.0}, text at {5:0.0},{6:0.0}; disabled, picture at {7:0.0},{8:0.0}, text at {9:0.0},{10:0.0}",
					name, alone[0].X, alone[0].Y, up[0].X, up[0].Y, up[1].X, up[1].Y, off[0].X, off[0].Y, off[1].X, off[1].Y);
				report.Add(line);
				// A button on a disabled panel cannot be drawn enabled.
				if (!button.Parent.Enabled)
					return;
				if (Apart(alone[0], up[0]) || Apart(offAlone[0], off[0]) || Apart(up[1], off[1]))
					faults.Add(line + string.Format("; disabled alone at {0:0.0},{1:0.0}", offAlone[0].X, offAlone[0].Y));
			}
			finally
			{
				button.Enabled = enabled;
			}
		}

		/// <summary>Whether two places are more than half a pixel apart either way.</summary>
		static bool Apart(PointF a, PointF b)
		{
			return Math.Abs(a.X - b.X) > 0.5 || Math.Abs(a.Y - b.Y) > 0.5;
		}

		/// <summary>
		/// Where a button's picture and its text are drawn: the middle of each one's pixels, in the columns its layout gives each;
		/// or, alone, where the button's picture would be by itself on the button's face in the place given.
		/// </summary>
		/// <remarks>
		/// The layout may start the text's columns inside the picture's, beside it, where the picture is drawn and the text is
		/// not; those columns are the picture's, whose fading as the button is disabled would move the text's middle.
		/// </remarks>
		static PointF[] Ink(ButtonBase button, Rectangle image, Rectangle text, bool alone)
		{
			using (var bitmap = new Bitmap(button.Width, button.Height))
			{
				button.DrawToBitmap(bitmap, new Rectangle(Point.Empty, button.Size));
				// The face's colour, from the middle of its top edge, where nothing is drawn.
				var face = bitmap.GetPixel(button.Width / 2, 2);
				if (alone)
					using (var g = Graphics.FromImage(bitmap))
					{
						g.Clear(face);
						using (var attributes = new System.Drawing.Imaging.ImageAttributes())
						{
							// As the theme draws a disabled button's picture.
							if (!button.Enabled)
								attributes.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix { Matrix33 = 0.5f });
							g.DrawImage(button.Image, image, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
						}
					}
				var textLeft = image.Left < text.Left && image.Right > text.Left ? image.Right : text.Left;
				var textRight = image.Right > text.Right && image.Left < text.Right ? image.Left : text.Right;
				return new[] { Ink(bitmap, face, image.Left, image.Right), Ink(bitmap, face, textLeft, textRight) };
			}
		}

		/// <summary>
		/// The middle of the pixels unlike the face between two columns, each weighed by how unlike it is. A picture with pixels
		/// both lighter and darker than the face has another middle on another face, so each state is compared with itself alone.
		/// </summary>
		static PointF Ink(Bitmap bitmap, Color face, int left, int right)
		{
			double sum = 0, x = 0, y = 0;
			for (var px = Math.Max(0, left); px < Math.Min(bitmap.Width, right); px++)
				for (var py = 0; py < bitmap.Height; py++)
				{
					var c = bitmap.GetPixel(px, py);
					var weight = Math.Abs(c.R - face.R) + Math.Abs(c.G - face.G) + Math.Abs(c.B - face.B);
					sum += weight;
					x += px * weight;
					y += py * weight;
				}
			return sum == 0 ? PointF.Empty : new PointF((float)(x / sum), (float)(y / sum));
		}

		static void MeasureItem(ToolStripItem item, string when, List<string> report, List<string> faults)
		{
			var layout = InternalLayoutProperty.GetValue(item, null);
			var image = (Rectangle)layout.GetType().GetProperty("ImageRectangle", Flags).GetValue(layout, null);
			var text = (Rectangle)layout.GetType().GetProperty("TextRectangle", Flags).GetValue(layout, null);
			var shown = item.ImageScaling == ToolStripItemImageScaling.SizeToFit && item.Owner != null ? item.Owner.ImageScalingSize : item.Image.Size;
			var name = when + " " + Path(item.Owner) + "." + (string.IsNullOrEmpty(item.Name) ? item.Text : item.Name);
			var withText = item.DisplayStyle == ToolStripItemDisplayStyle.ImageAndText && !string.IsNullOrEmpty(item.Text);
			Check(name, new Rectangle(Point.Empty, item.Size), image, withText ? text : Rectangle.Empty, item.Image, shown, report, faults);
		}

		static void Check(string name, Rectangle client, Rectangle image, Rectangle text, Image picture, Size shown, List<string> report, List<string> faults)
		{
			// In half pixels, so a centre between two pixels is a whole number.
			var offset = text.IsEmpty
				? (image.Top * 2 + image.Height) - (client.Top * 2 + client.Height)
				: (image.Top * 2 + image.Height) - (text.Top * 2 + text.Height);
			var inside = client.Contains(image);
			var original = ControlsHelper.GetOriginal(picture);
			var wanted = new Size((int)Math.Round(original.Width * ControlsHelper.DpiScale / 2), (int)Math.Round(original.Height * ControlsHelper.DpiScale / 2));
			var drawn = picture.Size == shown && picture.Size == wanted;
			var line = string.Format("{0}: client {1}, image {2}, text {3}, centre offset {4:0.0} px, inside {5}, picture {6} shown {7} drawn {8}",
				name, client, image, text, offset / 2.0, inside, picture.Size, shown, drawn);
			report.Add(line);
			if (Math.Abs(offset) > 1 || !inside || !drawn)
				faults.Add(line);
		}

		const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		static readonly PropertyInfo AdapterProperty = typeof(ButtonBase).GetProperty("Adapter", Flags);
		static readonly PropertyInfo InternalLayoutProperty = typeof(ToolStripItem).GetProperty("InternalLayout", Flags);

		/// <summary>
		/// Where Windows Forms puts a button's text when it paints the button raised, and the place its layout centres the
		/// button's picture in.
		/// </summary>
		/// <remarks>
		/// The adapter's Layout method lays the button out pressed, its text a pixel down and right; the layout a button is
		/// painted raised with is the one its Paint...Layout method makes with up set. Laid out as .NET Framework 1.1 laid a
		/// button out, the picture's place starts where the layout says it starts, and Windows Forms draws it a pixel right of
		/// and below that.
		/// </remarks>
		static bool GetButtonLayout(ButtonBase button, PaintEventArgs e, out Rectangle text, out Rectangle image)
		{
			text = image = Rectangle.Empty;
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
			var textField = type?.GetField("textBounds", Flags) ?? type?.GetField("TextBounds", Flags);
			var imageField = type?.GetField("imageBounds", Flags) ?? type?.GetField("ImageBounds", Flags);
			if (textField == null || imageField == null)
				return false;
			text = (Rectangle)textField.GetValue(data);
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
