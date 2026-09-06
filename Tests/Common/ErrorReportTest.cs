// @under-test: Engine/JocysCom/Controls/ErrorReportUserControl.cs
// @area: diagnostics   @layer: unit
using JocysCom.ClassLibrary.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
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
		[Description("Every control a person acts on says what it is")]
		public void Controls_say_what_they_are()
		{
			// This is the window a user has to operate before a report ever reaches us, so it has
			// to be usable without seeing it. A control that announces nothing is a dead end for
			// anyone using a screen reader, and a report that is never sent.
			WithControl((form, control) =>
			{
				var unnamed = new List<string>();
				foreach (var c in Interactive(control))
				{
					var name = !string.IsNullOrWhiteSpace(c.AccessibleName) ? c.AccessibleName : c.Text;
					if (string.IsNullOrWhiteSpace(name))
						unnamed.Add(c.GetType().Name + " " + c.Name);
				}
				Assert.AreEqual(0, unnamed.Count,
					"These controls announce nothing to a screen reader: " + string.Join(", ", unnamed));
			});
		}

		/// <summary>Every control a person can act on, anywhere under the given one.</summary>
		static List<Control> Interactive(Control parent)
		{
			var found = new List<Control>();
			foreach (Control c in parent.Controls)
			{
				if (c is Button || c is CheckBox || c is RadioButton || c is TextBox || c is ComboBox)
					found.Add(c);
				found.AddRange(Interactive(c));
			}
			return found;
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

		[TestMethod, TestCategory("diagnostics"), TestCategory("ui-interactive")]
		[Description("With no report to choose there is nothing to send and no greeting to send it with")]
		public void Nothing_is_sent_while_no_report_is_chosen()
		{
			// Three of the reports received about 4.20.43.0 were a greeting saying the details were
			// attached below, and nothing below it: the greeting had been written into the blank page
			// shown when there is no report, and Send was live.
			WithReports(0, (form, control) =>
			{
				var send = Descendants(control).OfType<Button>().First(x => x.Name == "SendErrorButton");
				var browser = Descendants(control).OfType<WebBrowser>().First();
				PumpUntil(() => browser.ReadyState == WebBrowserReadyState.Complete);
				Assert.IsFalse(send.Enabled, "Send is live with no report to choose.");
				Assert.IsTrue(string.IsNullOrWhiteSpace(control.GetBody()),
					"The blank page carries text, which is what was mailed as a report: " + control.GetBody());
			});
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("ui-interactive")]
		[Description("One click sends one report")]
		public void Send_goes_down_after_one_click()
		{
			// A second click while "Sending..." showed queued the same report again. Counted by the
			// time of the crash, a third of the reports received were copies of another.
			WithReports(1, (form, control) =>
			{
				// The hosting application sets the address; a bare control has none to send to.
				control.SupportEmail = "support@example.test";
				var send = Descendants(control).OfType<Button>().First(x => x.Name == "SendErrorButton");
				PumpUntil(() => !string.IsNullOrWhiteSpace(control.GetBody()));
				Assert.IsTrue(send.Enabled, "Send is not live with a report chosen.");
				var sent = 0;
				control.SendMessages += (s, e) => sent += e.Data.Count;
				send.PerformClick();
				send.PerformClick();
				Assert.AreEqual(1, sent, "Two clicks sent " + sent + " report(s).");
				Assert.IsFalse(send.Enabled, "Send is still live after sending.");
			});
		}

		/// <summary>Shows the control over a folder holding the given number of reports, in place of the real one.</summary>
		static void WithReports(int reports, Action<Form, ErrorReportUserControl> assert)
		{
			var folder = Path.Combine(Path.GetTempPath(), "x360ce-report-test-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(folder);
			for (var i = 0; i < reports; i++)
				File.WriteAllText(Path.Combine(folder, "FCE_TestException_00000000_20260904_00000" + i + ".000000.htm"),
					"<html><body><p>Report " + i + "</p></body></html>");
			var helper = JocysCom.ClassLibrary.Runtime.LogHelper.Current;
			var previous = helper.OverrideLogFolder;
			helper.OverrideLogFolder = folder;
			try
			{
				WithControl(assert);
			}
			finally
			{
				helper.OverrideLogFolder = previous;
				Directory.Delete(folder, true);
			}
		}

		/// <summary>Runs the message loop until the condition holds, or gives up after a while.</summary>
		static void PumpUntil(Func<bool> condition)
		{
			var until = DateTime.Now.AddSeconds(15);
			while (!condition() && DateTime.Now < until)
			{
				Application.DoEvents();
				Thread.Sleep(50);
			}
			Application.DoEvents();
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
