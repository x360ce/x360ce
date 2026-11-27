// @under-test: App.v4/Languages, App.v4/Controls/PadControl.Designer.cs, App.v4/Controls/PadTabPages/General/XboxImageUserControl.cs, Engine/JocysCom/Controls/UiTree/UiLanguage.Forms.cs
// @area: languages   @layer: ui
using JocysCom.ClassLibrary.Controls.UiTree;
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
	/// Every page shows every control whole in every language, at 100 % and 150 %, in the main window at its default size:
	/// no control covers another, no caption is cut or ends in an ellipsis that English does not, no button is narrower than
	/// its words, nothing lies past the edge of what holds it, no entry of a bar is pushed off it, and on the controller's
	/// General page no name lies over the controller picture. As the window first opens, the General page ends with its last
	/// row, neither cut off nor with an empty band below it. The smallest window is the designer's smallest size at the zoom.
	/// </summary>
	/// <remarks>
	/// Measured against English on the same page: a fault English has too is the page's own, listed as such, and fails only
	/// the English test. Ukrainian, German, Polish and Portuguese run longest; Chinese, Japanese and Korean need their fonts.
	/// </remarks>
	[TestClass]
	public class LanguageLayoutTest
	{
		public TestContext TestContext { get; set; }

		/// <summary>The pages, each as the main window shows it on its tab.</summary>
		static Func<Control>[] Surfaces()
		{
			return new Func<Control>[]
			{
				() => Registered(new PadControl(MapTo.Controller1), "Controller 1"),
				() => Registered(new OptionsUserControl(), "Options"),
				() => Registered(new GamesGridUserControl(), "Games"),
				() => Registered(new UserDevicesUserControl(), "Devices"),
				() => Registered(new JocysCom.ClassLibrary.Controls.IssuesControl.IssuesUserControl(), "Issues"),
				() => Registered(new AboutControl(), "About"),
			};
		}

		/// <summary>A page with its words recorded and shown in the language chosen, as the program shows it, in the main window.</summary>
		static Control Registered(Control page, string caption)
		{
			UiLanguage.Register(page);
			return OptionsLayoutTest.MainWindowAtDefaultSize(page, caption);
		}

		/// <summary>What does not show whole in one language at one zoom, each fault once, by the control it is about.</summary>
		static SortedSet<string> Faults(string culture, float zoom)
		{
			var faults = new SortedSet<string>(StringComparer.Ordinal);
			var own = UiTreeWalker.OwnNamespaces;
			UiTreeWalker.OwnNamespaces = new[] { "x360ce", "JocysCom" };
			try
			{
				var language = AppLanguages.All.Single(x => x.Culture == culture);
				ButtonIconTest.OnEverySurface(Surfaces().Select(make => (Func<Control>)(() => { AppLanguages.Use(language); return make(); })), (form, when) =>
				{
					// Light only: the theme changes colours, not sizes.
					if (!when.EndsWith("Light", StringComparison.Ordinal))
						return;
					// At the window's size when it first opens, and at its smallest, both as many pixels as the program makes them
					// at the zoom measured: the zoom is the font's, so the smallest size is the designer's at that zoom, not at
					// the screen's own scale.
					var designer = PadPageShortWindowTest.MinimumWindowSize();
					var scale = (zoom > 0 ? zoom : JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale) / JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale;
					var smallest = new Size((int)Math.Round(designer.Width * scale), (int)Math.Round(designer.Height * scale));
					var sizes = new[] { form.Size, smallest };
					foreach (var size in sizes)
					{
						form.Size = size;
						var at = size == sizes[0] ? Opening : Smallest;
						Application.DoEvents();
						foreach (var tabs in PadPageShortWindowTest.Descendants(form).OfType<TabControl>().ToList())
						{
							foreach (TabPage tab in tabs.TabPages)
							{
								tabs.SelectedTab = tab;
								Application.DoEvents();
								foreach (var fault in FaultsIn(tab))
									faults.Add(at + tab.Name + ": " + fault);
								// The controller's General page as the window first opens, as a whole, at the screen's own scale: at
								// another zoom only the words change size, while the pictures and the rows the program sizes from the
								// screen's scale do not, so heights there are not the program's.
								var whole = at == Opening && Math.Abs(scale - 1) < 0.01
									&& PadPageShortWindowTest.Descendants(form).OfType<PadControl>().Any(x => x.GeneralTabPage == tab)
									? BandOrCut(tab) : null;
								if (whole != null)
									faults.Add(at + tab.Name + ": " + whole);
							}
						}
						foreach (var strip in PadPageShortWindowTest.Descendants(form).OfType<ToolStrip>().Where(x => x.Visible))
							foreach (ToolStripItem item in strip.Items)
								if (item.Available && item.Placement != ToolStripItemPlacement.Main && !string.IsNullOrEmpty(item.Name))
									faults.Add(at + strip.Name + ": " + item.Name + " is pushed off the bar");
					}
				}, zoom: zoom);
			}
			finally
			{
				Ui.OnUiThread(() => AppLanguages.Use(AppLanguages.All[0]));
				UiTreeWalker.OwnNamespaces = own;
			}
			return faults;
		}

		/// <summary>The start of a fault seen in the window at the size it first opens at.</summary>
		const string Opening = "opening size, ";

		/// <summary>The start of a fault seen in the window at its smallest size.</summary>
		const string Smallest = "smallest size, ";

		/// <summary>What on one page does not show whole: covered, cut, past its holder's edge, or over the controller picture.</summary>
		static IEnumerable<string> FaultsIn(Control page)
		{
			foreach (var covered in ControlOverlapTest.Covered(page))
				yield return covered.Trim();
			foreach (var control in PadPageShortWindowTest.Descendants(page).Where(IsShown))
			{
				var cut = Cut(control);
				if (cut != null)
					yield return control.Name + " " + cut;
				var parent = control.Parent;
				var scrolls = parent is ScrollableControl && ((ScrollableControl)parent).AutoScroll;
				if (parent != null && parent != page && !scrolls && !(parent is TabControl) && !(control is TabPage)
					&& (control.Left < -1 || control.Top < -1 || control.Right > parent.ClientSize.Width + 1 || control.Bottom > parent.ClientSize.Height + 1))
					yield return control.Name + " lies past the edge of " + parent.Name;
			}
			var picture = PadPageShortWindowTest.Descendants(page).OfType<XboxImageUserControl>().FirstOrDefault(IsShown);
			if (picture != null)
			{
				var drawn = picture.RectangleToScreen(picture.ClientRectangle);
				foreach (var name in PadPageShortWindowTest.Descendants(picture.Parent).OfType<Label>().Where(x => IsShown(x) && x.Text.Length > 0))
					if (name.RectangleToScreen(name.ClientRectangle).IntersectsWith(Shrink(drawn, picture)))
						yield return name.Name + " lies over the controller picture";
			}
		}

		/// <summary>
		/// Why the controller's General page does not show as a whole, or null: its last row cut off at the bottom, or an empty
		/// band below it. The device list above the pages takes the height the page does not need, so the page ends where its
		/// last row does.
		/// </summary>
		static string BandOrCut(TabPage page)
		{
			var shown = PadPageShortWindowTest.Descendants(page).Where(x => x != page && IsShown(x) && !x.Controls.Cast<Control>().Any(IsShown)).ToList();
			if (shown.Count == 0)
				return null;
			var bottom = shown.Max(x => page.RectangleToClient(x.Parent.RectangleToScreen(x.Bounds)).Bottom);
			if (bottom > page.ClientSize.Height + 1)
				return "cuts its last row off at the bottom";
			// More than two lines of text below the last row is room the device list should have had.
			if (page.ClientSize.Height - bottom > page.Font.Height * 2)
				return "leaves an empty band below its last row";
			return null;
		}

		/// <summary>Where the picture draws: its middle, as wide as the artwork at its scale.</summary>
		static Rectangle Shrink(Rectangle bounds, XboxImageUserControl picture)
		{
			var scale = Math.Min(bounds.Width / (float)XboxImageUserControl.CanvasWidth, bounds.Height / (float)XboxImageUserControl.CanvasHeight);
			var width = (int)Math.Round(XboxImageUserControl.CanvasWidth * scale);
			return new Rectangle(bounds.X + (bounds.Width - width) / 2, bounds.Y, width, bounds.Height);
		}

		static bool IsShown(Control control)
		{
			return control.Visible && control.Width > 0 && control.Height > 0;
		}

		/// <summary>Why a control's words do not show whole, or null: a button narrower than its words, a caption cut or ending in an ellipsis.</summary>
		static string Cut(Control control)
		{
			var text = control.Text;
			if (string.IsNullOrEmpty(text) || control is TextBoxBase || control is ComboBox || control is UpDownBase || control is TabPage)
				return null;
			var button = control as ButtonBase;
			if (button != null && !button.AutoSize)
			{
				var needed = button.GetPreferredSize(Size.Empty).Width;
				return needed > button.Width + 1 ? "is narrower than its words" : null;
			}
			var label = control as Label;
			if (label != null && !label.AutoSize)
			{
				var prefix = label.UseMnemonic ? TextFormatFlags.Default : TextFormatFlags.NoPrefix;
				// Words on one line that fit are whole; longer ones wrap, and are whole when every line fits.
				// As wide as the theme draws it, as the program measures it.
				var line = JocysCom.ClassLibrary.Controls.Themes.FormsTheme.GetLabelWidth(label);
				var size = TextRenderer.MeasureText(text, label.Font, new Size(label.ClientSize.Width, int.MaxValue), prefix | TextFormatFlags.WordBreak);
				if (line > label.Width + 1 && size.Height > label.ClientSize.Height + 1)
					return "cuts its words";
				if (label.AutoEllipsis && TextRenderer.MeasureText(text, label.Font).Width > label.ClientSize.Width + 1)
					return "ends its words in an ellipsis";
			}
			return null;
		}

		/// <summary>English's own faults at each zoom, measured once for every language compared with it.</summary>
		static readonly Dictionary<float, SortedSet<string>> English = new Dictionary<float, SortedSet<string>>();

		/// <summary>The faults of each language that English does not have on the same page, at 100 % and 150 %, at the size the window opens at and at its smallest.</summary>
		void AssertWhole(params string[] cultures)
		{
			var problems = new List<string>();
			foreach (var zoom in new[] { 1f, 1.5f })
			{
				SortedSet<string> english;
				if (!English.TryGetValue(zoom, out english))
					English[zoom] = english = Faults("en", zoom);
				foreach (var fault in english)
					TestContext.WriteLine("{0:P0} English: {1}", zoom, fault);
				foreach (var culture in cultures)
					foreach (var fault in Faults(culture, zoom).Except(english))
						problems.Add(string.Format("{0:P0} {1}: {2}", zoom, culture, fault));
			}
			foreach (var problem in problems)
				TestContext.WriteLine(problem);
			Assert.AreEqual(0, problems.Count, problems.Count + " control(s) do not show whole:" + Environment.NewLine + string.Join(Environment.NewLine, problems));
		}

		[TestMethod, TestCategory("languages"), TestCategory("ui")]
		[Description("In Ukrainian, every page shows every control whole, and no name lies over the controller picture, at 100 % and 150 %")]
		public void Ukrainian_shows_every_control_whole() { AssertWhole("uk"); }

		[TestMethod, TestCategory("languages"), TestCategory("ui")]
		[Description("In German, every page shows every control whole, and no name lies over the controller picture, at 100 % and 150 %")]
		public void German_shows_every_control_whole() { AssertWhole("de"); }

		[TestMethod, TestCategory("languages"), TestCategory("ui")]
		[Description("In Polish, every page shows every control whole, and no name lies over the controller picture, at 100 % and 150 %")]
		public void Polish_shows_every_control_whole() { AssertWhole("pl"); }

		[TestMethod, TestCategory("languages"), TestCategory("ui")]
		[Description("In Portuguese, every page shows every control whole, and no name lies over the controller picture, at 100 % and 150 %")]
		public void Portuguese_shows_every_control_whole() { AssertWhole("pt-BR"); }

		[TestMethod, TestCategory("languages"), TestCategory("ui")]
		[Description("In French, Spanish and Turkish, every page shows every control whole, at 100 % and 150 %")]
		public void French_Spanish_and_Turkish_show_every_control_whole()
		{
			AssertWhole("fr", "es", "tr");
		}

		[TestMethod, TestCategory("languages"), TestCategory("ui")]
		[Description("In Chinese, Japanese and Korean, every page shows every control whole, at 100 % and 150 %")]
		public void Chinese_Japanese_and_Korean_show_every_control_whole()
		{
			AssertWhole("zh-Hans", "ja", "ko");
		}

		[TestMethod, TestCategory("languages"), TestCategory("ui")]
		[Description("In English, no name lies over the controller picture, at 100 % and 150 %, and the controller's General page ends with its last row, whole, at the screen's own scale")]
		public void English_shows_every_control_whole()
		{
			var faults = Faults("en", 1).Select(x => "100 % " + x).Concat(Faults("en", 1.5f).Select(x => "150 % " + x)).ToList();
			foreach (var fault in faults)
				TestContext.WriteLine(fault);
			Assert.AreEqual(0, faults.Count(x => x.Contains("controller picture")), string.Join(Environment.NewLine, faults));
			// Each language is measured against English, so the General page as a whole is held to it here.
			Assert.AreEqual(0, faults.Count(x => x.Contains("last row")), string.Join(Environment.NewLine, faults));
		}
	}
}
