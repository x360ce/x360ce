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
			var app = Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = System.IO.Path.GetDirectoryName(exe) });
			try
			{
				var window = Ui.WaitForMainWindow(app, TimeSpan.FromSeconds(60));
				// The header is wired once the window has finished building itself, which is also
				// when the controller pages arrive.
				var target = Ui.WaitFor(() => ByName(window, "Load Preset..."), TimeSpan.FromSeconds(90),
					"the Load Preset button never appeared");
				var subject = Ui.WaitFor(() => ByAutomationId(window, "HelpSubjectLabel"), TimeSpan.FromSeconds(10),
					"the header has no subject label");
				var body = Ui.WaitFor(() => ByAutomationId(window, "HelpBodyLabel"), TimeSpan.FromSeconds(10),
					"the header has no body label");

				var restingSubject = subject.Current.Name;
				// The centre of where it is drawn, rather than a clickable point: another window
				// lying over this one makes the second unavailable and says nothing about the wiring.
				var box = target.Current.BoundingRectangle;
				Assert.IsFalse(box.IsEmpty, "Load Preset is not drawn anywhere.");
				// Windows sends the move to whichever window is under the pointer, so a window lying
				// over this one would swallow it and the header would rightly say nothing.
				SetForegroundWindow(app.MainWindowHandle);
				var x = (int)(box.Left + box.Width / 2);
				var y = (int)(box.Top + box.Height / 2);
				// Two moves: entering a control is a change of position, and a single jump from a
				// point the pointer already occupied is not one.
				SetCursorPos(x - 2, y - 2);
				SetCursorPos(x, y);

				// The header is filled by the window's own message loop, so this waits for it rather
				// than reading straight after moving, which reads what was there before.
				var shown = Ui.WaitFor(
					() => body.Current.Name != null && body.Current.Name.Contains("saved set")
						? body.Current.Name : null,
					TimeSpan.FromSeconds(10),
					"the header never reported what the mouse was over. It said: " + body.Current.Name);
				Assert.IsTrue(shown.Contains("saved set"),
					"The header body should describe the control under the mouse.");
				Assert.AreEqual("Load Preset...", subject.Current.Name,
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
				Ui.CloseApp(app);
			}
		}

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

		[DllImport("user32.dll")]
		static extern bool SetCursorPos(int x, int y);

		[DllImport("user32.dll")]
		static extern bool SetForegroundWindow(IntPtr window);
	}
}
