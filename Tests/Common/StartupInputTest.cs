// @under-test: App.v3/MainForm.cs, App.v4/MainForm.cs
// @area: startup   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Automation;

namespace x360ce.Tests
{
	/// <summary>
	/// Keyboard input that arrives before the window has finished building itself.
	/// </summary>
	/// <remarks>
	/// Both applications show their window first and create the four pad controls afterwards, and
	/// both raise a start-up dialog in between — so the first thing a user does is press a key while
	/// that array is still null. App.v4 returns early in that case; App.v3 did not, and read
	/// <c>ControlPads.Length</c> straight into an unhandled NullReferenceException on the very
	/// first launch. The guard is one line in each application, which is exactly the kind of thing
	/// that gets fixed in one copy and forgotten in the other, so the symmetry is asserted rather
	/// than trusted.
	/// </remarks>
	[TestClass]
	public class StartupInputTest
	{

		private Process _process;

		[TestCleanup]
		public void Cleanup() => Ui.CloseApp(_process);

		/// <summary>The applications, and the field each one keeps its four pad controls in.</summary>
		static readonly string[] MainForms = { "App.v3/MainForm.cs", "App.v4/MainForm.cs" };

		[TestMethod, TestCategory("startup"), TestCategory("smoke")]
		[Description("Every main form ignores a key press until its pad controls exist")]
		public void Key_handlers_return_before_touching_pad_controls()
		{
			foreach (var relative in MainForms)
			{
				var path = Path.Combine(Ui.RepoRoot.FullName, relative.Replace('/', Path.DirectorySeparatorChar));
				Assert.IsTrue(File.Exists(path), relative + " not found; the test no longer covers it.");
				var body = KeyDownBody(File.ReadAllText(path), relative);

				// The field is read from the loop the handler runs, so renaming it cannot silently
				// take this test out of service.
				var field = Regex.Match(body, @"<\s*(\w+)\.Length");
				Assert.IsTrue(field.Success,
					relative + " no longer walks a pad-control array in MainForm_KeyDown; " +
					"check whether this test still describes it.");
				var name = field.Groups[1].Value;

				var guard = Regex.Match(body, @"if\s*\(\s*" + name + @"\s*==\s*null\s*\)\s*(?:\r?\n\s*)?return\s*;");
				Assert.IsTrue(guard.Success,
					relative + " reads " + name + " in MainForm_KeyDown without checking it for null first. " +
					"A key pressed before the pad controls are created crashes the application.");
				Assert.IsTrue(guard.Index < field.Index,
					relative + " checks " + name + " for null only after it has already been used.");
			}
		}

		[TestMethod, TestCategory("startup"), TestCategory("ui-interactive")]
		[Description("Version 3 survives a key press sent while it is still starting up")]
		public void V3_survives_a_key_press_during_start_up()
			=> App_survives_a_key_press_during_start_up("App.v3");

		[TestMethod, TestCategory("startup"), TestCategory("ui-interactive")]
		[Description("Version 4 survives a key press sent while it is still starting up")]
		public void V4_survives_a_key_press_during_start_up()
			=> App_survives_a_key_press_during_start_up("App.v4");

		/// <summary>
		/// Post keys to the main window from the moment it exists, which is what the reported crash
		/// needed: a key that arrives after the window is up and before the pad controls are built.
		/// </summary>
		private void App_survives_a_key_press_during_start_up(string appFolder)
		{
			var exe = Ui.FindApp(appFolder);
			if (exe == null)
				Assert.Inconclusive(appFolder + " is not built. Build the solution before running UI tests.");

			_process = Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = Path.GetDirectoryName(exe) });

			// Not WaitForMainWindow: that waits for a title, and the window takes keys before then.
			var handle = Ui.WaitFor(() =>
			{
				_process.Refresh();
				if (_process.HasExited)
					throw new InvalidOperationException(
						"Process exited with code " + _process.ExitCode + " before a window appeared.");
				return _process.MainWindowHandle == IntPtr.Zero ? null : (object)_process.MainWindowHandle;
			}, TimeSpan.FromSeconds(45), "the main window handle");

			// Press Escape — the key the handler acts on — over and over for the whole of start-up.
			// The reported crash needed one key about half a second in; App.v3 also raises a modal
			// warnings dialog on the way, and its pad controls are created only once that closes,
			// so the window being covered is bounded by time rather than by the application
			// reaching a state it does not reach unattended.
			string crash = null;
			try
			{
				crash = Ui.WaitFor(() =>
				{
					NativeMethods.PostMessage(
						(IntPtr)handle, NativeMethods.WM_KEYDOWN, (IntPtr)NativeMethods.VK_ESCAPE, IntPtr.Zero);
					return UnhandledExceptionDialog(_process);
				}, TimeSpan.FromSeconds(15), "an unhandled exception that must not happen");
			}
			catch (TimeoutException)
			{
				// Fifteen seconds of key presses and no crash dialog: this is the pass.
			}

			Assert.IsNull(crash,
				appFolder + " raised an unhandled exception while it was starting up: " + crash);

			_process.Refresh();
			Assert.IsFalse(_process.HasExited, appFolder + " exited while it was starting up.");
		}

		/// <summary>Text of the Windows Forms unhandled-exception dialog, or null when there is none.</summary>
		static string UnhandledExceptionDialog(Process process)
		{
			var root = AutomationElement.RootElement.FindAll(TreeScope.Children, new PropertyCondition(
				AutomationElement.ProcessIdProperty, process.Id));
			foreach (AutomationElement window in root)
			{
				foreach (AutomationElement text in window.FindAll(TreeScope.Descendants, new PropertyCondition(
					AutomationElement.ControlTypeProperty, ControlType.Text)))
				{
					var value = text.Current.Name ?? "";
					if (value.IndexOf("Unhandled exception", StringComparison.OrdinalIgnoreCase) >= 0 ||
						value.IndexOf("Object reference not set", StringComparison.OrdinalIgnoreCase) >= 0)
						return window.Current.Name + ": " + value;
				}
			}
			return null;
		}

		/// <summary>The body of MainForm_KeyDown, from its opening brace to its matching close.</summary>
		static string KeyDownBody(string source, string relative)
		{
			var signature = source.IndexOf("MainForm_KeyDown(object sender, KeyEventArgs e)", StringComparison.Ordinal);
			Assert.IsTrue(signature >= 0, relative + " has no MainForm_KeyDown handler.");
			var open = source.IndexOf('{', signature);
			Assert.IsTrue(open >= 0, relative + " has no body for MainForm_KeyDown.");
			var depth = 0;
			for (var i = open; i < source.Length; i++)
			{
				if (source[i] == '{')
					depth++;
				else if (source[i] == '}' && --depth == 0)
					return source.Substring(open, i - open + 1);
			}
			Assert.Fail(relative + " has an unbalanced MainForm_KeyDown body.");
			return null;
		}

		private static class NativeMethods
		{
			public const uint WM_KEYDOWN = 0x0100;
			public const int VK_ESCAPE = 0x1B;

			[DllImport("user32.dll")]
			public static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
		}

	}
}
