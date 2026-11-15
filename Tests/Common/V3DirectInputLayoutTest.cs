// @under-test: App.v3/Controls/DirectInputControl.Designer.cs, App.v3/Controls/PadControl.cs, Engine/JocysCom/Controls/Themes/Default.Forms.cs
// @area: interface   @layer: ui
using JocysCom.ClassLibrary.Controls.Themes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace x360ce.Tests
{
	/// <summary>
	/// Version 3's Direct Input page, themed and shown as the program shows it for a mapped device: every label stands left
	/// of its own box, on the box's line, covers no box, and is seen whole, at 100 % and at 150 %.
	/// </summary>
	/// <remarks>
	/// The tests cannot reference version 3, because both programs build <c>x360ce.exe</c> in the namespace
	/// <c>x360ce.App</c>, so the page is made in an application domain of its own whose folder is a copy of version 3's
	/// build, and its controls are found by name. A scale is the font every window of that domain starts with, as Windows
	/// Forms scales a window by its font, whatever the scale of the screen the tests run on.
	/// </remarks>
	[TestClass]
	public class V3DirectInputLayoutTest
	{
		public TestContext TestContext { get; set; }

		/// <summary>Each label of the page, and the box it names.</summary>
		static readonly string[][] LabelBoxes =
		{
			new[] { "DeviceVendorNameLabel", "DeviceVendorNameTextBox" },
			new[] { "DiProductNameLabel", "DeviceProductNameTextBox" },
			new[] { "DiProductGuidLabel", "DeviceProductGuidTextBox" },
			new[] { "DiInstanceGuidLabel", "DeviceInstanceGuidTextBox" },
			new[] { "DiDeviceVidLabel", "DeviceVidTextBox" },
			new[] { "DiDevicePidLabel", "DevicePidTextBox" },
			new[] { "DiDeviceTypeLabel", "DeviceTypeTextBox" },
			new[] { "DiUvaLabel", "DiUvSliderTextBox" },
			new[] { "DiExtraALabel", "DiASliderTextBox" },
			new[] { "DiExtraFLabel", "DiFSliderTextBox" },
			new[] { "DiExtraVLabel", "DiVSliderTextBox" },
			new[] { "MapToControllerLabel", "MapToPadComboBox" },
			new[] { "DiDPadLabel", "DiDPadTextBox" },
			new[] { "DiButtonsLabel", "DiButtonsTextBox" },
			new[] { "AxeCountLabel", "DiCapAxesTextBox" },
			new[] { "SlidersLabel", "SlidersTextBox" },
			new[] { "label4", "DiCapDPadsTextBox" },
			new[] { "label3", "DiCapButtonsTextBox" },
			new[] { "ActuatorsLabel", "ActuatorsTextBox" },
			new[] { "DiCapFfLabel", "DiCapFfStateTextBox" },
		};

		[TestMethod, TestCategory("ui"), Timeout(180000)]
		[DataRow(1f), DataRow(1.5f)]
		[Description("On version 3's Direct Input page, at 100 % and 150 %, every label stands left of its own box, on its line, covers no box and is seen whole")]
		public void Every_label_stands_left_of_its_own_box(float scale)
		{
			var build = V3Build();
			if (build == null)
				Assert.Inconclusive("Version 3 is not built for a " + (Environment.Is64BitProcess ? "64" : "32") + "-bit process.");
			var copy = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "x360ce-tests", "v3-layout-" + Guid.NewGuid().ToString("N")));
			// A picture of the page only when a folder is named for them, as the other tests that draw a window keep theirs.
			var pictures = Environment.GetEnvironmentVariable("X360CE_TEST_PICTURES");
			var capture = string.IsNullOrEmpty(pictures) ? null : Path.Combine(pictures, string.Format("v3-direct-input-{0:0}.png", scale * 100));
			string[] faults;
			var domain = (AppDomain)null;
			try
			{
				foreach (var file in new[] { "x360ce.exe", "x360ce.exe.config", "x360ce.Engine.dll", "x360ce.Engine.dll.config" })
					if (File.Exists(Path.Combine(build, file)))
						File.Copy(Path.Combine(build, file), Path.Combine(copy.FullName, file));
				// The libraries the program carries inside itself and finds through its Main, beside it here.
				foreach (var file in Directory.GetFiles(Path.Combine(Ui.RepoRoot.FullName, "App.v3", "Resources", "SharpDX"), "*.dll"))
					File.Copy(file, Path.Combine(copy.FullName, Path.GetFileName(file)));
				domain = AppDomain.CreateDomain("V3DirectInputLayoutTest", null, new AppDomainSetup
				{
					ApplicationBase = copy.FullName,
					ConfigurationFile = Path.Combine(copy.FullName, "x360ce.exe.config"),
				});
				var page = (V3Page)domain.CreateInstanceFromAndUnwrap(typeof(V3Page).Assembly.Location, typeof(V3Page).FullName);
				faults = page.Measure(scale, LabelBoxes, capture);
			}
			finally
			{
				if (domain != null)
					AppDomain.Unload(domain);
				try { copy.Delete(true); }
				catch (IOException) { }
				catch (UnauthorizedAccessException) { }
			}
			foreach (var fault in faults)
				TestContext.WriteLine(fault);
			Assert.AreEqual(0, faults.Length, faults.Length + " fault(s) at " + scale.ToString("P0") + ":" + Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		/// <summary>The newest version 3 build this process can load, or null.</summary>
		static string V3Build()
		{
			var bin = new DirectoryInfo(Path.Combine(Ui.RepoRoot.FullName, "App.v3", "bin"));
			if (!bin.Exists)
				return null;
			var bits = Environment.Is64BitProcess ? "_x64" : "_x86";
			return bin.GetDirectories("*" + bits)
				.Select(x => new FileInfo(Path.Combine(x.FullName, "x360ce.exe")))
				.Where(x => x.Exists)
				.OrderByDescending(x => x.LastWriteTimeUtc)
				.Select(x => x.DirectoryName)
				.FirstOrDefault();
		}

		/// <summary>
		/// Runs in version 3's domain, as the program starts: its text drawing and theme set before the first window, the
		/// controller page made, and its Direct Input page added back as the program does once a device is mapped to it.
		/// </summary>
		public sealed class V3Page : MarshalByRefObject
		{
			public string[] Measure(float scale, string[][] labelBoxes, string capture)
			{
				string[] faults = null;
				Exception failure = null;
				var ui = new Thread(() =>
				{
					try { faults = Drive(scale, labelBoxes, capture); }
					catch (Exception ex) { failure = ex; }
				});
				ui.SetApartmentState(ApartmentState.STA);
				ui.Start();
				if (!ui.Join(TimeSpan.FromSeconds(120)))
					return new[] { "The page was not measured within two minutes." };
				return failure != null ? new[] { failure.ToString() } : faults;
			}

			static string[] Drive(float scale, string[][] labelBoxes, string capture)
			{
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				// The screen's scale is the size of the font every window starts with, so the scale asked for is that font,
				// made larger or smaller, before any window of the domain is made: each scales itself by it once, as made.
				float screen;
				using (var g = Graphics.FromHwnd(IntPtr.Zero))
					screen = g.DpiY / 96F;
				var font = Control.DefaultFont;
				typeof(Control).GetField("defaultFont", BindingFlags.Static | BindingFlags.NonPublic)
					.SetValue(null, new Font(font.FontFamily, font.SizeInPoints * scale / screen));
				FormsTheme.SetTheme(ThemeType.Light);
				var padType = Assembly.Load("x360ce").GetType("x360ce.App.Controls.PadControl", true);
				const BindingFlags field = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
				// Never disposed: the program loads the controller page's pictures after it makes the page, and disposes them
				// with it, so the window is left to the domain's unloading.
				var form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), AutoScaleMode = AutoScaleMode.Font };
				// The main window at its smallest, scaled by its font from the 6 by 13 its designer measures in, as version 3's
				// main window is.
				var factor = form.CurrentAutoScaleDimensions.Height / 13F;
				form.ClientSize = new Size((int)Math.Round(724 * factor), (int)Math.Round(669 * factor));
				var tabs = new TabControl { Dock = DockStyle.Fill };
				var controllerPage = new TabPage("Controller 1");
				tabs.TabPages.Add(controllerPage);
				form.Controls.Add(tabs);
				form.Show();
				try
				{
					// As the theme's hook does when the window first becomes active.
					FormsTheme.Apply(form);
					var pad = (Control)Activator.CreateInstance(padType, 0);
					pad.Dock = DockStyle.Fill;
					controllerPage.Controls.Add(pad);
					Application.DoEvents();
					var padTabs = (TabControl)padType.GetField("PadTabControl", field).GetValue(pad);
					var directInputPage = (TabPage)padType.GetField("DirectInputTabPage", field).GetValue(pad);
					padTabs.TabPages.Add(directInputPage);
					// The page's tab selected, as a person selects it, without the help header of a main window there is not.
					padTabs.SelectedIndexChanged -= (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), pad, "PadTabControl_SelectedIndexChanged");
					padTabs.SelectedTab = directInputPage;
					Application.DoEvents();
					var faults = Faults(directInputPage.Controls.Cast<Control>().Single(), labelBoxes);
					if (!string.IsNullOrEmpty(capture))
					{
						using (var picture = new Bitmap(form.Width, form.Height))
						{
							form.DrawToBitmap(picture, new Rectangle(Point.Empty, form.Size));
							picture.Save(capture, ImageFormat.Png);
						}
					}
					return faults;
				}
				finally
				{
					form.Hide();
				}
			}

			/// <summary>Each label not left of its own box, off its box's line, over a box, or not seen whole on the page.</summary>
			static string[] Faults(Control page, string[][] labelBoxes)
			{
				var faults = new List<string>();
				var all = All(page).ToList();
				var boxes = all.Where(x => x.Visible && (x is TextBoxBase || x is ComboBox || x is DataGridView))
					.ToDictionary(x => x, x => x.Parent.RectangleToScreen(x.Bounds));
				var seen = Rectangle.Intersect(page.RectangleToScreen(page.ClientRectangle), page.FindForm().RectangleToScreen(page.FindForm().ClientRectangle));
				foreach (var pair in labelBoxes)
				{
					var label = all.Single(x => x.Name == pair[0]);
					var box = all.Single(x => x.Name == pair[1]);
					var l = label.Parent.RectangleToScreen(label.Bounds);
					var b = box.Parent.RectangleToScreen(box.Bounds);
					var where = string.Format("{0} {1} and {2} {3}", label.Name, Relative(l, seen), box.Name, Relative(b, seen));
					if (l.Right > b.Left)
						faults.Add(where + ": the label does not end left of its box.");
					var middle = l.Top + l.Height / 2;
					if (middle < b.Top || middle > b.Bottom)
						faults.Add(where + ": the label is not on its box's line.");
					foreach (var other in boxes.Where(x => x.Value.IntersectsWith(l)))
						faults.Add(where + ": the label covers " + other.Key.Name + " " + Relative(other.Value, seen) + ".");
					if (!seen.Contains(l))
						faults.Add(where + ": the label is not seen whole on the page " + Relative(seen, seen) + ".");
				}
				return faults.ToArray();
			}

			/// <summary>A rectangle on the screen as it stands on the page.</summary>
			static string Relative(Rectangle r, Rectangle page)
			{
				return string.Format("({0},{1} {2}x{3})", r.X - page.X, r.Y - page.Y, r.Width, r.Height);
			}

			static IEnumerable<Control> All(Control parent)
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
}
