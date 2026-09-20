// @under-test: App.v4/Forms/LoadPresetsForm.cs, App.v3/Controls/ControllerSettingsUserControl.cs
// @area: webservice   @layer: ui-wpf
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Automation;

namespace x360ce.Tests
{
	/// <summary>
	/// The released programs, not a test client, load presets from the service under test.
	/// </summary>
	/// <remarks>
	/// The SOAP tests prove the service answers what a program asks. These prove each program
	/// gets its list: x360ce is started, pointed at <c>X360CE_WEBSERVICE_URL</c> through its own
	/// Options page, and its "Most Popular" presets must become exactly what the service answers
	/// right now. Rows alone prove nothing, because both programs keep the last list between
	/// runs. It is the screen the owner's report of 2026-09-19 showed failing, so the window is
	/// captured as evidence. The program's web service address is put back afterwards.
	/// </remarks>
	[TestClass]
	public class LoadPresetUiTest
	{
		public TestContext TestContext { get; set; }

		Process _process;

		[TestCleanup]
		public void Cleanup() => Ui.CloseApp(_process);

		[TestMethod, TestCategory("webservice"), TestCategory("v4"), TestCategory("ui-interactive")]
		[Description("Version 4 fills its Most Popular presets list from the configured service")]
		public void V4_loads_popular_presets_from_the_configured_service()
		{
			var url = WebServiceTarget.Require();
			var window = Start("App.v4");
			// Options > Internet: the address the program will call, and the switch that lets it.
			SelectTab(window, "Options");
			SelectTab(window, "Internet");
			var original = AddressShown(window);
			try
			{
				TypeInto(Find(window, AutomationElement.AutomationIdProperty, "InternetDatabaseUrlComboBox"), url);
				var features = Find(window, AutomationElement.AutomationIdProperty, "InternetFeaturesCheckBox");
				var toggle = (TogglePattern)features.GetCurrentPattern(TogglePattern.Pattern);
				if (toggle.Current.ToggleState != ToggleState.On)
					toggle.Toggle();

				// Controller 1 > Load Preset... opens a dialog; the popular tab is inside it.
				SelectTab(window, "Controller 1");
				Invoke(Find(window, AutomationElement.AutomationIdProperty, "LoadPresetButton"));
				var dialog = Ui.WaitFor(() => TopLevelWindow(_process.Id, "Load Preset"),
					TimeSpan.FromSeconds(20), "the Load Preset window");
				SelectTab(dialog, "Default Settings for Most Popular Controllers");
				PopularPresetsComeFromTheService(dialog, url, "v4-load-preset-popular");
				Invoke(Find(dialog, AutomationElement.AutomationIdProperty, "CloseButton"));
			}
			finally
			{
				CloseDialogs();
				RestoreAddress(window, original, "Internet");
			}
		}

		[TestMethod, TestCategory("webservice"), TestCategory("v3"), TestCategory("ui-interactive")]
		[Description("Version 3 fills its Most Popular presets list from the configured service")]
		public void V3_loads_popular_presets_from_the_configured_service()
		{
			var url = WebServiceTarget.Require();
			var window = Start("App.v3");
			SelectTab(window, "Options");
			var original = AddressShown(window);
			try
			{
				TypeInto(Find(window, AutomationElement.AutomationIdProperty, "InternetDatabaseUrlComboBox"), url);
				// Controller Settings holds the three lists on its own tabs; no dialog in v3.
				SelectTab(window, "Controller Settings");
				SelectTab(window, "Default Settings for Most Popular Controllers");
				PopularPresetsComeFromTheService(window, url, "v3-controller-settings-popular");
			}
			finally
			{
				RestoreAddress(window, original, null);
			}
		}

		#region The shared walk

		AutomationElement Start(string appFolder)
		{
			var exe = Ui.FindApp(appFolder);
			if (exe == null)
				Assert.Inconclusive(appFolder + " is not built. Build the solution before running UI tests.");
			_process = Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = Path.GetDirectoryName(exe) });
			return Ui.WaitForMainWindow(_process, TimeSpan.FromSeconds(45));
		}

		/// <summary>
		/// Refresh, then wait until the presets grid shows exactly the SIDs the service answers.
		/// </summary>
		void PopularPresetsComeFromTheService(AutomationElement scope, string url, string captureName)
		{
			string[] served;
			using (var ws = WebServiceTarget.Client())
				served = ws.SearchSettings(new[] { new Engine.SearchParameter() }).Presets
					.Select(x => x.PadSettingChecksum.ToString("N").Substring(0, 8).ToUpperInvariant())
					.OrderBy(x => x).ToArray();
			Assert.IsTrue(served.Length > 0, "The service has no popular presets to show");

			// A toolbar item has no automation id, only its text; the other tabs' toolbars are off screen.
			Invoke(Find(scope, AutomationElement.NameProperty, "Refresh"));
			var grid = Find(scope, AutomationElement.AutomationIdProperty, "PresetsDataGridView");
			string[] shown = null;
			try
			{
				Ui.WaitFor(() =>
				{
					// A WinForms grid exposes its rows as custom elements named "Row 0", "Row 1", ...
					shown = grid.FindAll(TreeScope.Children, Condition.TrueCondition)
						.Cast<AutomationElement>()
						.Where(x => Regex.IsMatch(x.Current.Name ?? "", @"^Row \d+$"))
						.Select(SidOf).Where(x => x != null).OrderBy(x => x).ToArray();
					return shown.SequenceEqual(served) ? shown : null;
				}, TimeSpan.FromSeconds(60), "the window to show the " + served.Length + " presets the service answers");
			}
			catch (TimeoutException)
			{
				// The picture shows the program's own message; the lists show what it kept instead.
				Capture(scope, captureName + "-timeout.png");
				Console.WriteLine("served: " + string.Join(",", served));
				Console.WriteLine("shown : " + string.Join(",", shown ?? new string[0]));
				throw;
			}
			Capture(scope, captureName + ".png");
			Console.WriteLine("{0} preset rows from {1}", shown.Length, url);
		}

		static string AddressShown(AutomationElement window)
		{
			var box = Find(window, AutomationElement.AutomationIdProperty, "InternetDatabaseUrlComboBox");
			return ((ValuePattern)box.GetCurrentPattern(ValuePattern.Pattern)).Current.Value;
		}

		/// <summary>Leaves the program as it was found; a failure here must not hide the one that brought us here.</summary>
		void RestoreAddress(AutomationElement window, string original, string innerTab)
		{
			try
			{
				SelectTab(window, "Options");
				if (innerTab != null)
					SelectTab(window, innerTab);
				// Found again: the element seen before a dialog does not answer after it.
				TypeInto(Find(window, AutomationElement.AutomationIdProperty, "InternetDatabaseUrlComboBox"), original ?? "");
			}
			catch (Exception ex)
			{
				Console.WriteLine("Could not restore the web service address: " + ex.Message);
			}
		}

		/// <summary>A dialog still open because the test failed inside it blocks the main window.</summary>
		void CloseDialogs()
		{
			try
			{
				var open = TopLevelWindow(_process.Id, "Load Preset");
				if (open != null)
					((WindowPattern)open.GetCurrentPattern(WindowPattern.Pattern)).Close();
			}
			catch (Exception ex)
			{
				Console.WriteLine("Could not close the dialog: " + ex.Message);
			}
		}

		#endregion

		#region Automation helpers

		/// <summary>
		/// Types a value into a text box as a user would. Setting the value through the automation
		/// pattern writes the text without the control raising TextChanged, so the program's
		/// option behind the box never learns of it; keystrokes go through the same path a user's do.
		/// </summary>
		static void TypeInto(AutomationElement box, string text)
		{
			box.SetFocus();
			System.Windows.Forms.SendKeys.SendWait("^a");
			foreach (var c in text)
				System.Windows.Forms.SendKeys.SendWait("+^%~(){}[]".IndexOf(c) >= 0 ? "{" + c + "}" : c.ToString());
			System.Windows.Forms.SendKeys.SendWait("{TAB}");
			var shown = Ui.WaitFor(() =>
			{
				var value = ((ValuePattern)box.GetCurrentPattern(ValuePattern.Pattern)).Current.Value;
				return value == text ? value : null;
			}, TimeSpan.FromSeconds(10), "the box to show '" + text + "'");
			Console.WriteLine("Typed: " + shown);
		}

		/// <summary>The SID cell of a grid row, as the window shows it.</summary>
		static string SidOf(AutomationElement row)
		{
			var cell = row.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit));
			object pattern;
			if (cell == null || !cell.TryGetCurrentPattern(ValuePattern.Pattern, out pattern))
				return null;
			return ((ValuePattern)pattern).Current.Value;
		}

		/// <summary>
		/// A visible top-level window of the process whose title contains the text. The Load
		/// Preset dialog is owned by a hidden form, so the automation root does not list it;
		/// the window handle is found the way Windows finds it and wrapped from there.
		/// </summary>
		static AutomationElement TopLevelWindow(int processId, string titlePart)
		{
			IntPtr found = IntPtr.Zero;
			NativeMethods.EnumWindows((handle, _) =>
			{
				uint pid;
				NativeMethods.GetWindowThreadProcessId(handle, out pid);
				if (pid != processId || !NativeMethods.IsWindowVisible(handle))
					return true;
				var title = new System.Text.StringBuilder(256);
				NativeMethods.GetWindowText(handle, title, title.Capacity);
				if (title.ToString().IndexOf(titlePart, StringComparison.Ordinal) < 0)
					return true;
				found = handle;
				return false;
			}, IntPtr.Zero);
			return found == IntPtr.Zero ? null : AutomationElement.FromHandle(found);
		}

		static void SelectTab(AutomationElement root, string name)
		{
			var tab = Ui.WaitFor(() => root.FindFirst(TreeScope.Descendants, new AndCondition(
				new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem),
				new PropertyCondition(AutomationElement.NameProperty, name))),
				TimeSpan.FromSeconds(20), "tab '" + name + "'");
			((SelectionItemPattern)tab.GetCurrentPattern(SelectionItemPattern.Pattern)).Select();
		}

		static AutomationElement Find(AutomationElement root, AutomationProperty property, object value)
		{
			return Ui.WaitFor(() =>
			{
				var found = root.FindFirst(TreeScope.Descendants, new PropertyCondition(property, value));
				return found != null && !found.Current.IsOffscreen ? found : null;
			}, TimeSpan.FromSeconds(20), value + "");
		}

		static void Invoke(AutomationElement element)
		{
			((InvokePattern)element.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
		}

		/// <summary>
		/// Writes the window to the git-ignored captures folder (the deployment folder of a
		/// passing run is deleted with the run). PrintWindow renders the window itself, so the
		/// picture is right even when another window covers it, which a screen copy is not.
		/// </summary>
		void Capture(AutomationElement element, string fileName)
		{
			var r = element.Current.BoundingRectangle;
			if (r.IsEmpty)
				return;
			var folder = Path.Combine(Ui.RepoRoot.FullName, "scripts", "ui", "captures");
			Directory.CreateDirectory(folder);
			var path = Path.Combine(folder, fileName);
			using (var bitmap = new Bitmap((int)r.Width, (int)r.Height))
			{
				using (var g = Graphics.FromImage(bitmap))
				{
					var dc = g.GetHdc();
					NativeMethods.PrintWindow(new IntPtr(element.Current.NativeWindowHandle), dc, NativeMethods.PW_RENDERFULLCONTENT);
					g.ReleaseHdc(dc);
				}
				bitmap.Save(path, ImageFormat.Png);
			}
			TestContext.AddResultFile(path);
			Console.WriteLine("Captured " + path);
		}

		static class NativeMethods
		{
			public delegate bool EnumWindowsProc(IntPtr handle, IntPtr lParam);
			[System.Runtime.InteropServices.DllImport("user32.dll")]
			public static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);
			[System.Runtime.InteropServices.DllImport("user32.dll")]
			public static extern uint GetWindowThreadProcessId(IntPtr handle, out uint processId);
			[System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
			public static extern int GetWindowText(IntPtr handle, System.Text.StringBuilder text, int capacity);
			[System.Runtime.InteropServices.DllImport("user32.dll")]
			public static extern bool IsWindowVisible(IntPtr handle);
			public const uint PW_RENDERFULLCONTENT = 2;
			[System.Runtime.InteropServices.DllImport("user32.dll")]
			public static extern bool PrintWindow(IntPtr handle, IntPtr dc, uint flags);
		}

		#endregion
	}
}
