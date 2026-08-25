// @under-test: Engine/JocysCom/Controls/ErrorReportUserControl.cs
// @area: diagnostics   @layer: unit
using JocysCom.ClassLibrary.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace x360ce.Tests
{
	/// <summary>
	/// The error report dialog is the last thing a user sees before deciding whether to send a
	/// crash report, so a control clipped at the window edge costs real reports. The WPF version
	/// this replaced had a fixed size that clipped its own buttons and shipped with a group
	/// caption still reading "GroupBox"; these tests keep both from coming back.
	/// </summary>
	[TestClass]
	public class ErrorReportTest
	{

		/// <summary>
		/// Windows Forms controls need a single-threaded apartment, which MSTest does not provide.
		/// </summary>
		static void OnUiThread(Action action)
		{
			Exception failure = null;
			var thread = new Thread(() =>
			{
				try { action(); }
				catch (Exception ex) { failure = ex; }
			});
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
			// Generous: the control hosts a browser, which is slow to create on a cold run.
			Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(60)), "The interface thread did not finish.");
			if (failure != null)
				throw new AssertFailedException(failure.Message, failure);
		}

		static void WithControl(Action<Form, ErrorReportUserControl> assert)
		{
			OnUiThread(() =>
			{
				using (var form = new Form { ClientSize = new System.Drawing.Size(600, 480) })
				using (var control = new ErrorReportUserControl { Dock = DockStyle.Fill })
				{
					form.Controls.Add(control);
					form.Show();
					Application.DoEvents();
					assert(form, control);
					form.Close();
				}
			});
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("ui-interactive")]
		[Description("Every button is fully inside the window at the size the dialog opens at")]
		public void Error_report_fits_its_window_without_clipping()
		{
			WithControl((form, control) =>
			{
				var buttons = Descendants(control).OfType<Button>().ToArray();
				Assert.IsTrue(buttons.Length >= 5,
					"Expected the four action buttons and both browse buttons, found " + buttons.Length + ".");
				foreach (var button in buttons)
				{
					var topLeft = form.PointToClient(button.PointToScreen(System.Drawing.Point.Empty));
					var right = topLeft.X + button.Width;
					var bottom = topLeft.Y + button.Height;
					// All four edges: a narrow window pushes a right-aligned button off the LEFT,
					// which a right-edge check alone never sees.
					Assert.IsTrue(topLeft.X >= 0,
						"Button '" + button.Text + "' is clipped: left edge " + topLeft.X + " is off-window.");
					Assert.IsTrue(topLeft.Y >= 0,
						"Button '" + button.Text + "' is clipped: top edge " + topLeft.Y + " is off-window.");
					Assert.IsTrue(right <= form.ClientSize.Width,
						"Button '" + button.Text + "' is clipped: right edge " + right +
						" exceeds the client width " + form.ClientSize.Width + ".");
					Assert.IsTrue(bottom <= form.ClientSize.Height,
						"Button '" + button.Text + "' is clipped: bottom edge " + bottom +
						" exceeds the client height " + form.ClientSize.Height + ".");
					Assert.IsTrue(button.Width > 0 && button.Height > 0,
						"Button '" + button.Text + "' has no size.");
				}
			});
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("ui-interactive")]
		[Description("The report group is captioned, not left as the designer placeholder")]
		public void Error_report_group_is_captioned()
		{
			WithControl((form, control) =>
			{
				var group = Descendants(control).OfType<GroupBox>().FirstOrDefault();
				Assert.IsNotNull(group, "The report fields are expected to sit in a group box.");
				Assert.AreNotEqual("GroupBox", group.Text,
					"The group caption is still the designer placeholder.");
				Assert.IsFalse(string.IsNullOrWhiteSpace(group.Text), "The group has no caption.");
			});
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("ui-interactive")]
		[Description("The report area grows with the window rather than staying a fixed size")]
		public void Error_report_details_area_follows_the_window()
		{
			WithControl((form, control) =>
			{
				var browser = Descendants(control).OfType<WebBrowser>().FirstOrDefault();
				Assert.IsNotNull(browser, "The report is displayed in a browser.");
				var before = browser.Height;
				form.ClientSize = new System.Drawing.Size(form.ClientSize.Width, form.ClientSize.Height + 200);
				Application.DoEvents();
				Assert.IsTrue(browser.Height > before,
					"The details area stayed " + before + " pixels tall when the window grew, " +
					"which is the fixed-size fault the Windows Forms rewrite removed.");
			});
		}

		static System.Collections.Generic.IEnumerable<Control> Descendants(Control root)
		{
			foreach (Control child in root.Controls)
			{
				yield return child;
				foreach (var nested in Descendants(child))
					yield return nested;
			}
		}

	}
}
