// @under-test: App.v4/Common/UiTree/UiHelp.cs, App.v4/Forms/BaseFormWithHeader.cs
// @area: accessibility   @layer: ui-winforms
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;

namespace x360ce.Tests
{
	/// <summary>
	/// The header says what the mouse is over. It reads the same two properties a screen reader
	/// announces, so this also proves those two are actually set on the control the mouse is on -
	/// which no amount of reading the source can show.
	/// </summary>
	[TestClass]
	public class HeaderHelpTest
	{
		[TestMethod, TestCategory("accessibility"), TestCategory("ui-interactive")]
		[Description("Pointing at a control puts its name and purpose in the header")]
		public void Header_reports_whatever_the_mouse_is_over()
		{
			var exe = Ui.FindApp("App.v4");
			// The pointer goes back where it was, so a failed wait does not leave it over the place
			// the next test's window opens, whose header would then name the control under it.
			POINT cursor;
			GetCursorPos(out cursor);
			var app = Process.Start(new ProcessStartInfo(exe, TestRun.ProfileArgument) { WorkingDirectory = System.IO.Path.GetDirectoryName(exe) });
			// This program's window, once the test has put it on top of the others.
			var onTop = IntPtr.Zero;
			try
			{
				var window = Ui.WaitForMainWindow(app, TimeSpan.FromSeconds(60));
				// The header is wired once the window has finished building itself, which is also
				// when the controller pages arrive and the words that say it is loading go.
				Ui.WaitFor(() => ByAutomationId(window, nameof(x360ce.App.MainForm.LoadingLabel)) == null ? "loaded" : null, TimeSpan.FromSeconds(90),
					"the window never finished loading");
				// A button that works with no device mapped: a disabled one hears nothing of the mouse.
				var target = Ui.WaitFor(() => ByName(window, "Copy Preset"), TimeSpan.FromSeconds(90),
					"the Copy Preset button never appeared");
				var subject = Ui.WaitFor(() => ByAutomationId(window, "HelpSubjectLabel"), TimeSpan.FromSeconds(10),
					"the header has no subject label");
				var body = Ui.WaitFor(() => ByAutomationId(window, "HelpBodyLabel"), TimeSpan.FromSeconds(10),
					"the header has no body label");

				var restingSubject = subject.Current.Name;
				// The centre of where it is drawn, rather than a clickable point: another window
				// lying over this one makes the second unavailable and says nothing about the wiring.
				var box = target.Current.BoundingRectangle;
				Assert.IsFalse(box.IsEmpty, "Copy Preset is not drawn anywhere.");
				var x = (int)(box.Left + box.Width / 2);
				var y = (int)(box.Top + box.Height / 2);
				// Windows sends the move to whichever window is under the pointer, so a window lying
				// over this one would swallow it and the header would rightly say nothing. This
				// program's window is put in front of the others and kept there until the test ends,
				// pass or fail; no other program's window is touched.
				var handle = new IntPtr(window.Current.NativeWindowHandle);
				SetForegroundWindow(handle);
				if (SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE))
					onTop = handle;
				var refused = onTop == IntPtr.Zero ? Marshal.GetLastWin32Error() : 0;
				var topAtOnce = (GetWindowLong(handle, GWL_EXSTYLE) & WS_EX_TOPMOST) != 0;
				// The point is checked before it is used: a window that stays on top of all others
				// can still lie over it, and then the header's answer says nothing about the program.
				var under = AutomationElement.FromPoint(new System.Windows.Point(x, y));
				for (var i = 0; i < 20 && under.Current.ProcessId != app.Id; i++)
				{
					System.Threading.Thread.Sleep(100);
					under = AutomationElement.FromPoint(new System.Windows.Point(x, y));
				}
				if (under.Current.ProcessId != app.Id)
					Assert.Inconclusive(Covered(under, x, y, app, box, handle) +
						string.Format(" Putting it on top: Windows error {0}, on top straight after: {1}.", refused, topAtOnce));
				// Two moves: entering a control is a change of position, and a single jump from a
				// point the pointer already occupied is not one.
				SetCursorPos(x - 2, y - 2);
				SetCursorPos(x, y);

				// The header is filled by the window's own message loop, so this waits for it rather
				// than reading straight after moving, which reads what was there before.
				string shown = null;
				var until = DateTime.UtcNow.AddSeconds(10);
				while (shown == null && DateTime.UtcNow < until)
				{
					var name = body.Current.Name;
					if (name != null && name.Contains("to the clipboard"))
						shown = name;
					else
						System.Threading.Thread.Sleep(100);
				}
				if (shown == null)
				{
					// What the pointer is over tells a header that heard nothing from one that was told and put back,
					// and both from a window of another program that came over this one while the test waited.
					under = AutomationElement.FromPoint(new System.Windows.Point(x, y));
					if (under.Current.ProcessId != app.Id)
						Assert.Inconclusive(Covered(under, x, y, app, box, handle));
					Assert.Fail(string.Format("Timed out after 10s waiting for the header to report what the mouse was over. It says \"{0}: {1}\". " +
						"Under the pointer at {2},{3} is \"{4}\" ({5}) of this program, {6}; Copy Preset is at {7}.",
						subject.Current.Name, body.Current.Name, x, y, under.Current.Name, under.Current.AutomationId, app.Id, box));
				}
				Assert.IsTrue(shown.Contains("to the clipboard"),
					"The header body should describe the control under the mouse.");
				Assert.AreEqual("Copy Preset", subject.Current.Name,
					"The header subject should name the control under the mouse.");
				// A description of a control is not an event, so it must not be stamped with a time.
				Assert.IsFalse(shown.Contains(DateTime.Now.Year.ToString()),
					"The header stamped the time onto a description: " + shown);

				// Moving away puts the header back rather than leaving the last thing touched.
				SetCursorPos(1, 1);
				Ui.WaitFor(
					() => subject.Current.Name == restingSubject ? "back" : null,
					TimeSpan.FromSeconds(10),
					"the header kept describing a control the mouse had left");
			}
			finally
			{
				SetCursorPos(cursor.X, cursor.Y);
				// Off the top again before closing: a window left always on top would lie over the
				// owner's own windows if the program did not close.
				if (onTop != IntPtr.Zero)
					SetWindowPos(onTop, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
				Ui.CloseApp(app);
			}
		}

		/// <summary>Says which program's window lies over the point the test must point at, so the test ends without an answer.</summary>
		static string Covered(AutomationElement under, int x, int y, Process app, Rect box, IntPtr raised)
		{
			app.Refresh();
			var state = string.Format(" The window put on top, {0}, {1}; the program's main window is now {2}.", raised,
				!IsWindow(raised) ? "no longer exists" : (GetWindowLong(raised, GWL_EXSTYLE) & WS_EX_TOPMOST) != 0 ? "is still on top" : "is no longer on top",
				app.MainWindowHandle);
			var owner = under.Current.ProcessId;
			string name;
			try
			{
				using (var process = Process.GetProcessById(owner))
					name = process.ProcessName;
			}
			catch (ArgumentException)
			{
				name = "a program that has since closed";
			}
			return string.Format("A window of another program, {0} (process {1}), lies over Copy Preset at {2},{3} in front of this program ({4}), " +
				"so the pointer cannot reach it; Copy Preset is at {5}. That window was left as it was.", name, owner, x, y, app.Id, box) + state;
		}

		[DllImport("user32.dll")]
		static extern bool IsWindow(IntPtr window);

		[DllImport("user32.dll")]
		static extern int GetWindowLong(IntPtr window, int index);

		const int GWL_EXSTYLE = -20;
		const int WS_EX_TOPMOST = 0x8;

		static AutomationElement ByName(AutomationElement window, string name)
		{
			return window.FindFirst(TreeScope.Descendants,
				new PropertyCondition(AutomationElement.NameProperty, name));
		}

		static AutomationElement ByAutomationId(AutomationElement window, string id)
		{
			return window.FindFirst(TreeScope.Descendants,
				new PropertyCondition(AutomationElement.AutomationIdProperty, id));
		}

		[StructLayout(LayoutKind.Sequential)]
		struct POINT { public int X; public int Y; }

		[DllImport("user32.dll")]
		static extern bool GetCursorPos(out POINT point);

		[DllImport("user32.dll")]
		static extern bool SetCursorPos(int x, int y);

		[DllImport("user32.dll")]
		static extern bool SetForegroundWindow(IntPtr window);

		static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
		static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
		const uint SWP_NOSIZE = 0x0001;
		const uint SWP_NOMOVE = 0x0002;
		const uint SWP_NOACTIVATE = 0x0010;

		[DllImport("user32.dll", SetLastError = true)]
		static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
	}
}
