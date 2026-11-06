// @under-test: App.v4/Controls/AboutControl.cs, App.v4/Controls/AboutControl.Designer.cs, App.v4/Documents/Program.md
// @area: about   @layer: unit
using JocysCom.ClassLibrary.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// Credits and links on the About screen: the Program tab, first, shows App.v4/Documents/Program.md, which names the
	/// program, who made it and what it carries from other projects, each with a link; the licence tab shows the licences of
	/// what it carries after its own.
	/// </summary>
	/// <remarks>
	/// vigem.org stopped resolving when that project was archived, and the About screen went on
	/// pointing at it, so the shipping build carried a dead link nobody noticed. The credits are one document the program
	/// carries, so the document is what is read: every link in it is there, not only the ones a screen shows at once.
	/// </remarks>
	[TestClass]
	public class AboutTabTest
	{

		/// <summary>Addresses that must never appear again; their hosts are gone.</summary>
		static readonly string[] RetiredHosts = { "vigem.org" };

		/// <summary>Each project the Program tab credits, as its heading names it, and an address its section links to.</summary>
		static readonly string[][] Credited =
		{
			new[] { "x360ce", "https://github.com/x360ce/x360ce" },
			new[] { "ViGEm", "https://github.com/nefarius/ViGEmBus" },
			new[] { "HID Hide", "https://github.com/nefarius/HidHide" },
			new[] { "SDL_GameControllerDB", SdlDatabase.ProjectUrl },
			new[] { "Selawik", "https://github.com/microsoft/Selawik" },
			new[] { "Inconsolata", "https://github.com/googlefonts/Inconsolata" },
		};

		/// <summary>Who the page named above its tabs before it had a Program tab, each still named there with a link.</summary>
		static readonly string[] Credits = { "Jocys.com", "ToCA EDIT", "Nucleoprotein", "Nefarius" };

		/// <summary>An address written in angle brackets, which is how the document writes every link.</summary>
		static readonly Regex Address = new Regex(@"<(https?://[^>\s]+)>");

		[TestMethod, TestCategory("about"), TestCategory("ui")]
		[Description("The Program tab is the first tab, the page opens on it, and it shows the document the program carries, every address a link")]
		public void Program_tab_is_first_and_shows_the_program_document()
		{
			Ui.OnUiThread(() =>
			{
				using (var about = new AboutControl())
				{
					about.CreateControl();
					var tabs = Find<TabControl>(about, "AboutTabControl");
					Assert.AreEqual("ProgramTabPage", tabs.TabPages[0].Name, "The Program tab is not the first tab.");
					Assert.AreEqual("Program", tabs.TabPages[0].Text);
					Assert.AreEqual(0, tabs.SelectedIndex, "The About page does not open on the Program tab.");
					var box = Find<RichTextBox>(about, "ProgramRichTextBox");
					Assert.AreSame(tabs.TabPages[0], box.Parent, "The program's details are not on the Program tab.");
					Assert.AreEqual(DockStyle.Fill, box.Dock);
					Assert.IsTrue(box.ReadOnly);
					StringAssert.Contains(box.Text, "Xbox 360 Controller Emulator", "The Program tab is empty.");
					foreach (var name in Credits)
						StringAssert.Contains(box.Text, name, "The Program tab does not name " + name + ".");
					// The box raises LinkClicked, which opens the browser as in the help boxes, only over text Windows marks as a link.
					foreach (Match m in Address.Matches(AppHelper.ReadHelp(AppHelper.ProgramResource)))
					{
						var url = m.Groups[1].Value;
						var at = box.Text.IndexOf(url, StringComparison.Ordinal);
						Assert.IsTrue(at >= 0, "The Program tab does not show " + url + ".");
						Assert.IsTrue(IsLink(box, at) && IsLink(box, at + url.Length - 1), url + " is shown, but clicking it opens nothing.");
					}
					// The picture, the title and the subtitle stay; the names and links that sat beside them are on the Program tab.
					Assert.IsNotNull(Find<PictureBox>(about, "ProductPictureBox").Image);
					// The program's own version: in a test, the process's product is the test host.
					StringAssert.Contains(Find<Label>(about, "AboutProductLabel").Text, typeof(AboutControl).Assembly.GetName().Version.ToString());
					Find<Label>(about, "AboutDescriptionLabel");
					Assert.IsFalse(All(about).OfType<LinkLabel>().Any(), "A link is still shown above the tabs.");
				}
			});
		}

		[TestMethod, TestCategory("about"), TestCategory("ui")]
		[Description("The program's picture stands beside the title and the subtitle, as tall as both, made from a picture drawn at least that large, and the tabs stand right below them")]
		public void Header_picture_is_as_tall_as_the_title_and_subtitle()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), ShowInTaskbar = false })
				using (var about = new AboutControl { Dock = DockStyle.Fill })
				{
					form.ClientSize = about.Size;
					form.Controls.Add(about);
					form.Show();
					Application.DoEvents();
					var picture = Find<PictureBox>(about, "ProductPictureBox");
					var title = Find<Label>(about, "AboutProductLabel");
					var subtitle = Find<Label>(about, "AboutDescriptionLabel");
					var tabs = Find<TabControl>(about, "AboutTabControl");
					var where = string.Format(" Picture {0}, title {1}, subtitle {2}, tabs {3}.", picture.Bounds, title.Bounds, subtitle.Bounds, tabs.Bounds);
					Assert.AreEqual(title.Top, picture.Top, "The picture does not start level with the title." + where);
					Assert.AreEqual(subtitle.Bottom, picture.Bottom, "The picture does not end level with the subtitle." + where);
					Assert.IsTrue(title.Left >= picture.Right && subtitle.Left == title.Left, "The title and the subtitle do not stand beside the picture.");
					Assert.AreEqual(picture.Size, picture.Image.Size, "The picture is stretched to its place rather than made at its size.");
					Assert.IsTrue(ControlsHelper.GetOriginal(picture.Image).Height >= picture.Height, "The picture is enlarged from one drawn smaller.");
					Assert.AreEqual(subtitle.Bottom + subtitle.Margin.Bottom + tabs.Margin.Top, tabs.Top, "The tabs do not stand right below the subtitle." + where);
				}
			});
		}

		[TestMethod, TestCategory("about")]
		[Description("The Program tab's document credits x360ce, ViGEm, HID Hide, SDL's controller database and both fonts, each with a link, and links nowhere retired")]
		public void Program_document_credits_each_project_with_a_link()
		{
			var text = AppHelper.ReadHelp(AppHelper.ProgramResource).TrimStart('\uFEFF').Replace("\r\n", "\n");
			Assert.IsTrue(text.Length > 0, "The program does not carry App.v4/Documents/Program.md.");
			// One section to each project, from its heading to the next.
			var sections = Regex.Split(text, @"(?m)^(?=#{1,6}\s)").Where(x => x.StartsWith("#", StringComparison.Ordinal)).ToArray();
			foreach (var credit in Credited)
				StringAssert.Contains(Section(sections, credit[0]), "<" + credit[1] + ">", credit[0] + " is not linked to " + credit[1] + ".");
			StringAssert.Contains(Section(sections, "SDL_GameControllerDB"), "Licence: zlib, <http", "SDL's controller database is not said to be under the zlib licence, with a link.");
			foreach (var font in new[] { "Selawik", "Inconsolata" })
				StringAssert.Contains(Section(sections, font), "Licence: SIL Open Font License 1.1, <http", font + " is not said to be under the SIL Open Font License, with a link.");
			foreach (var name in Credits)
				StringAssert.Contains(text, "Developer: " + name + ", <http", "The Program tab does not name " + name + " with a link.");
			foreach (var host in RetiredHosts)
				Assert.IsFalse(text.IndexOf(host, StringComparison.OrdinalIgnoreCase) >= 0,
					"The Program tab still links to " + host + ", which no longer resolves.");
			// A link written as [text](address) is drawn as a link, but the box opens only an address it shows.
			Assert.IsFalse(Regex.IsMatch(text, @"\]\([^)]*\)"), "A link in the Program tab hides its address, so clicking it opens nothing.");
			var rtf = MarkdownRtf.ToRtf(text);
			Assert.AreEqual(rtf.Count(c => c == '{'), rtf.Count(c => c == '}'), "The Program tab's document renders with unbalanced braces, so the tab would be empty.");
		}

		[TestMethod, TestCategory("about"), TestCategory("ui")]
		[Description("The licence tab shows x360ce's licence, then SDL's controller database's and both fonts' licences in full")]
		public void Licence_tab_carries_the_licence_of_what_the_program_carries()
		{
			Ui.OnUiThread(() =>
			{
				using (var about = new AboutControl())
				{
					about.CreateControl();
					// The tab names the program, not the process that hosts it: in a test, that is the test host.
					var version = typeof(AboutControl).Assembly.GetName().Version;
					Assert.AreEqual("X360 Controller Emulator " + version.ToString(2) + " License", Find<TabPage>(about, "LicenseTabPage").Text);
					var licence = Find<TextBox>(about, "LicenseTextBox").Text.TrimStart('\uFEFF');
					var own = AppHelper.ReadHelp("Documents.License.txt").TrimStart('\uFEFF').Split('\n')[0].Trim();
					StringAssert.StartsWith(licence.TrimStart(), own, "x360ce's own licence is not shown first.");
					var at = new[] { "Sam Lantinga", "Reserved Font Name Selawik", "The Inconsolata Project Authors" }
						.Select(x => licence.IndexOf(x, StringComparison.Ordinal)).ToArray();
					Assert.IsTrue(at[0] > 0, "SDL's controller database's licence is not shown.");
					Assert.IsTrue(at[1] > at[0], "Selawik's licence is not shown, after SDL's.");
					Assert.IsTrue(at[2] > at[1], "Inconsolata's licence is not shown, after Selawik's.");
					Assert.AreEqual(2, Regex.Matches(licence, "SIL OPEN FONT LICENSE Version 1.1").Count, "The font licence is not shown once for each font.");
				}
			});
		}

		static string Section(string[] sections, string heading)
		{
			var section = sections.FirstOrDefault(x => Regex.IsMatch(x, @"^#{1,6}\s+" + Regex.Escape(heading) + @"\b"));
			Assert.IsNotNull(section, "The Program tab has no heading for " + heading + ".");
			return section;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		struct CHARFORMAT2
		{
			public int cbSize;
			public uint dwMask;
			public uint dwEffects;
			public int yHeight;
			public int yOffset;
			public int crTextColor;
			public byte bCharSet;
			public byte bPitchAndFamily;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
			public string szFaceName;
			public short wWeight;
			public short sSpacing;
			public int crBackColor;
			public int lcid;
			public int dwReserved;
			public short sStyle;
			public short wKerning;
			public byte bUnderlineType;
			public byte bAnimation;
			public byte bRevAuthor;
			public byte bReserved1;
		}

		const int EM_GETCHARFORMAT = 0x043A;
		const int SCF_SELECTION = 1;
		const uint CFM_LINK = 0x20;
		const uint CFE_LINK = 0x20;

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref CHARFORMAT2 lParam);

		/// <summary>Whether Windows marks the character at the position as part of a link, which is what a click on it opens.</summary>
		static bool IsLink(RichTextBox box, int position)
		{
			box.Select(position, 1);
			var format = new CHARFORMAT2 { dwMask = CFM_LINK };
			format.cbSize = Marshal.SizeOf(format);
			SendMessage(box.Handle, EM_GETCHARFORMAT, (IntPtr)SCF_SELECTION, ref format);
			return (format.dwEffects & CFE_LINK) != 0;
		}

		static IEnumerable<Control> All(Control parent)
		{
			foreach (Control child in parent.Controls)
			{
				yield return child;
				foreach (var grandChild in All(child))
					yield return grandChild;
			}
		}

		static T Find<T>(Control parent, string name) where T : Control
		{
			var found = parent.Controls.Find(name, true).OfType<T>().FirstOrDefault();
			Assert.IsNotNull(found, name + " is not on the About page.");
			return found;
		}

	}
}
