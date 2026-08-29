// @under-test: App.v4/Controls/OptionsUserControl.Designer.cs
// @area: options-layout   @layer: unit
using JocysCom.ClassLibrary.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using x360ce.App.Controls;

namespace x360ce.Tests
{
	/// <summary>
	/// Whether the controls on the Options page can be read and clicked.
	/// </summary>
	/// <remarks>
	/// A designer file records the size a control had while the form was being drawn, but an
	/// AutoSize label sizes itself to its text at run time. When the two disagree the label grows
	/// past where it was placed, and because these labels are anchored to the right they carry that
	/// error to every window size. That is what hid the Virtual Device <c>Refresh</c> button behind
	/// the ViGEm link: the link needs 137 px and the designer had recorded 89, so it painted 42 px
	/// into the button and left it reading "sh". None of that is visible in a diff, only in a
	/// rendered window — so it is measured here instead of being looked at.
	/// </remarks>
	[TestClass]
	public class OptionsLayoutTest
	{

		/// <summary>
		/// The page at the size it was designed at, and half as wide again to move the anchors.
		/// </summary>
		/// <remarks>
		/// Multiples of the page's own width rather than pixel counts: the page scales itself to the
		/// display, so a fixed number of pixels means a different amount of room on every machine.
		/// </remarks>
		static readonly double[] WidthFactors = { 1.0, 1.5 };

		[TestMethod, TestCategory("options-layout"), TestCategory("smoke")]
		[Description("Nothing a user reads or clicks on the Virtual Device panel is painted over")]
		public void Virtual_device_controls_never_overlap()
		{
			foreach (var factor in WidthFactors)
			{
				var boxes = MeasurePanel("VirtualDeviceGroupBox", factor);
				Assert.IsTrue(boxes.Count >= 7,
					"Expected the whole Virtual Device panel, measured " + boxes.Count + " controls.");
				AssertNoOverlap(boxes, factor);
			}
		}

		/// <summary>
		/// Bounds of every caption and command inside one group box on the Options page.
		/// </summary>
		/// <remarks>
		/// Labels, links, buttons and drop-downs are what a user reads and clicks, and every one of
		/// them has to be whole. A read-only status TextBox is not included: the panel is drawn at a
		/// design-time width of 186 px, which is narrower than the buttons on its own right-hand
		/// edge, so <c>ViGEmBusTextBox</c> stretches 63 px underneath <c>ViGEmBusInstallButton</c>
		/// and the button paints over the empty tail of it. Nothing is hidden by that today, and
		/// putting it right means laying the panel out again at a width it is really used at, which
		/// is a bigger change than the overlap this test was written for.
		/// </remarks>
		static Dictionary<string, Rectangle> MeasurePanel(string groupName, double widthFactor)
		{
			var boxes = new Dictionary<string, Rectangle>();
			WithOptionsPage(widthFactor, page =>
			{
				var group = Descendants(page).FirstOrDefault(x => x.Name == groupName);
				Assert.IsNotNull(group, groupName + " was not found on the Options page.");
				Show(group);
				foreach (var child in group.Controls.Cast<Control>().Where(x => x.Visible && ReadOrClicked(x)))
					boxes[child.Name] = child.Bounds;
			});
			return boxes;
		}

		static bool ReadOrClicked(Control control)
		{
			return control is Label || control is LinkLabel || control is ButtonBase || control is ComboBox;
		}

		static void AssertNoOverlap(Dictionary<string, Rectangle> boxes, double widthFactor)
		{
			var names = boxes.Keys.OrderBy(x => x, StringComparer.Ordinal).ToArray();
			for (var i = 0; i < names.Length; i++)
				for (var j = i + 1; j < names.Length; j++)
				{
					var a = boxes[names[i]];
					var b = boxes[names[j]];
					var shared = Rectangle.Intersect(a, b);
					Assert.IsTrue(shared.IsEmpty,
						names[i] + " " + a + " and " + names[j] + " " + b + " overlap by " +
						shared.Width + "x" + shared.Height + " px with the page " + widthFactor +
						" times its designed width, so one of them is painted over the other.");
				}
		}

		/// <summary>Bring a control on to the screen by selecting every tab page above it.</summary>
		/// <remarks>
		/// A tab page that is not the selected one is hidden, and a hidden parent makes every
		/// control below it report itself hidden too — which is how this test first measured an
		/// empty panel. Selecting the pages is also what a user does to reach the panel.
		/// </remarks>
		static void Show(Control control)
		{
			for (var parent = control.Parent; parent != null; parent = parent.Parent)
			{
				var page = parent as TabPage;
				var tabs = page == null ? null : page.Parent as TabControl;
				if (tabs != null)
					tabs.SelectedTab = page;
			}
			control.PerformLayout();
		}

		/// <summary>Every control below this one, at any depth.</summary>
		static IEnumerable<Control> Descendants(Control parent)
		{
			foreach (Control child in parent.Controls)
			{
				yield return child;
				foreach (var grandChild in Descendants(child))
					yield return grandChild;
			}
		}

		/// <summary>
		/// Build the Options page at a multiple of its designed width and hand it to the assertion.
		/// </summary>
		/// <remarks>
		/// The page is laid out but never shown: what is asserted is where the layout engine puts
		/// things, which it decides without a visible window, so this stays out of the interactive
		/// set. It is also why the page is sized directly rather than docked into a Form —
		/// <see cref="Control.Visible"/> reports the whole parent chain, and every control below a
		/// form that was never shown reads as hidden. With the page as the root, each control
		/// answers for itself. Windows Forms needs a single-threaded apartment, which MSTest does
		/// not provide.
		/// </remarks>
		static void WithOptionsPage(double widthFactor, Action<Control> assert)
		{
			Ui.OnUiThread(() =>
			{
				using (var page = new OptionsUserControl())
				{
					// A tab strip hands out its pages only once it has a window, and until then
					// every control below it reads as hidden.
					page.CreateControl();
					page.Width = (int)Math.Round(page.Width * widthFactor);
					page.PerformLayout();
					assert(page);
				}
			});
		}


	}
}
