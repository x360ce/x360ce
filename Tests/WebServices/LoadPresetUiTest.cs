// @under-test: App.v4/Controls/PresetsGridUserControl.cs, App.v3/Controls/ControllerSettingsUserControl.cs, App.v3/Controls/OptionsControl.cs, Engine/Common/Sdl/X360ceDatabase.cs
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
using x360ce.App.Controls;

namespace x360ce.Tests
{
	/// <summary>
	/// The released programs list their Most Popular presets from x360ce's controller database, and fetch the copy
	/// x360ce's server keeps when a person asks.
	/// </summary>
	/// <remarks>
	/// Both programs carry the database built in and list its rows at once and offline: for each controller, the setting
	/// most people use with it. Version 4's [Refresh] beside the list and version 3's [Update Controller Database] on its
	/// Options tab fetch the server's copy into the settings folder, from the site of the web service address
	/// (<see cref="Engine.X360ceUpdateClient.DatabaseUrl"/>), and that copy wins row by row from then on. Each program
	/// starts on a settings folder of its own, so it asks x360ce's own site, its first list is the built-in database alone,
	/// and the copy it fetches reaches no other test. The list is the screen the owner's report of 2026-09-19 showed
	/// failing, so the window is captured as evidence.
	/// </remarks>
	[TestClass]
	public class LoadPresetUiTest
	{
		public TestContext TestContext { get; set; }

		Process _process;

		[TestCleanup]
		public void Cleanup() => Ui.CloseApp(_process);

		[TestMethod, TestCategory("webservice"), TestCategory("v4"), TestCategory("ui-interactive")]
		[Description("Version 4 lists its Most Popular presets from the built-in controller database, and Refresh fetches the server's copy")]
		public void V4_lists_popular_presets_from_the_controller_database()
		{
			// A folder of its own: nothing is saved in it yet, and the device added below stays on the controller in it.
			var profile = Path.Combine(TestRun.LogsFolder, "load-preset-profile");
			var window = Start("App.v4", profile);
			try
			{
				// Controller 1 > Load Preset... opens a dialog; the popular tab is inside it.
				SelectTab(window, "Controller 1");
				AddDevice(window);
				Invoke(Find(window, AutomationElement.AutomationIdProperty, nameof(PadControl.LoadPresetButton)));
				var dialog = Ui.WaitFor(() => TopLevelWindow(_process.Id, "Load Preset"),
					TimeSpan.FromSeconds(20), "the Load Preset window");
				SelectTab(dialog, "Default Settings for Most Popular Controllers");
				var grid = Find(dialog, AutomationElement.AutomationIdProperty, nameof(PresetsGridUserControl.PresetsDataGridView));
				ListsTheDatabase(dialog, grid, profile, "v4-load-preset-popular-built-in");
				// A toolbar item has no automation id, only its text; the other tabs' toolbars are off screen.
				Invoke(Find(dialog, AutomationElement.NameProperty, "Refresh"));
				WaitForSavedCopy(dialog, profile, "v4-load-preset-refresh-timeout.png");
				ListsTheDatabase(dialog, grid, profile, "v4-load-preset-popular");
				Invoke(Find(dialog, AutomationElement.AutomationIdProperty, "CloseButton"));
			}
			finally
			{
				CloseDialogs();
			}
		}

		[TestMethod, TestCategory("webservice"), TestCategory("v3"), TestCategory("ui-interactive")]
		[Description("Version 3 lists its Most Popular presets from the built-in controller database, and Update Controller Database fetches the server's copy")]
		public void V3_lists_popular_presets_from_the_controller_database()
		{
			// A folder of its own: nothing is saved in it yet, and the copy fetched below reaches no other test.
			var profile = Path.Combine(TestRun.LogsFolder, "load-preset-profile-v3");
			var window = Start("App.v3", profile);
			// On a clean folder version 3 first asks about its warnings and each new controller, in front of the window.
			Ui.WaitFor(() =>
			{
				Ui.FixWarnings(_process, window);
				Ui.CloseDialogs(_process, window);
				var rate = Ui.FindByName(window, V3NewDeviceTest.RateText);
				return rate != null && Ui.ReadNumber(rate, V3NewDeviceTest.RateText) >= 7 ? rate : null;
			}, TimeSpan.FromSeconds(60), "the program to read the controllers after its questions were answered");
			TabCountsTheDatabase(window, profile);
			SelectTab(window, "Options");
			Invoke(Find(window, AutomationElement.NameProperty, "Update Controller Database"));
			WaitForSavedCopy(window, profile, "v3-update-controller-database-timeout.png");
			// The program lists the copy again as soon as it is saved.
			TabCountsTheDatabase(window, profile);
			Capture(window, "v3-controller-settings-popular.png");
		}

		#region The shared walk

		/// <summary>Adds an attached device to the controller shown, as a person does: Load Preset needs one, and a clean folder has none.</summary>
		void AddDevice(AutomationElement window)
		{
			// UI Automation does not list this tool bar's Add button, so it is clicked, right beside the Auto Map button it lists.
			var box = Find(window, AutomationElement.NameProperty, "Auto Map").Current.BoundingRectangle;
			window.SetFocus();
			NativeMethods.SetCursorPos((int)(box.Right + box.Height), (int)(box.Top + box.Height / 2));
			NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
			NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
			var add = Ui.WaitFor(() => TopLevelWindow(_process.Id, "Map Device To Controller"), TimeSpan.FromSeconds(20), "the window that adds a device");
			var grid = Find(add, AutomationElement.AutomationIdProperty, nameof(UserDevicesUserControl.DevicesDataGridView));
			if (grid.FindAll(TreeScope.Children, Condition.TrueCondition).Cast<AutomationElement>().All(x => !Regex.IsMatch(x.Current.Name ?? "", @"^Row \d+$")))
				Assert.Inconclusive("No device is attached to add.");
			Invoke(Find(add, AutomationElement.AutomationIdProperty, "OkButton"));
			Ui.WaitFor(() => Find(window, AutomationElement.AutomationIdProperty, nameof(PadControl.LoadPresetButton)).Current.IsEnabled ? "enabled" : null,
				TimeSpan.FromSeconds(20), "Load Preset to be enabled once a device is on the controller");
		}

		AutomationElement Start(string appFolder, string profile = null)
		{
			var exe = Ui.FindApp(appFolder);
			if (exe == null)
				Assert.Inconclusive(appFolder + " is not built. Build the solution before running UI tests.");
			var argument = profile == null ? TestRun.ProfileArgument : "\"/Profile=" + profile + "\"";
			_process = Process.Start(new ProcessStartInfo(exe, argument) { WorkingDirectory = Path.GetDirectoryName(exe) });
			return Ui.WaitForMainWindow(_process, TimeSpan.FromSeconds(45));
		}

		/// <summary>Where a program on this settings folder saves the server's copy of the database.</summary>
		static string SavedCopy(string profile)
		{
			return Path.Combine(profile, "Settings", Engine.X360ceDatabase.FileName);
		}

		/// <summary>
		/// The SIDs of the presets a program on this settings folder lists, read as the program reads them: the built-in
		/// database, and the copy fetched into the folder, which wins row by row.
		/// </summary>
		static string[] DatabaseSids(string profile)
		{
			var sids = Engine.X360ceDatabase.Load(SavedCopy(profile)).Mappings
				.Select(x => Engine.EngineHelper.GetID(Engine.X360ceDatabase.ToPadSetting(x).PadSettingChecksum))
				.OrderBy(x => x).ToArray();
			Assert.IsTrue(sids.Length > 0, "x360ce's controller database has no rows to list.");
			return sids;
		}

		/// <summary>Waits until the program has saved the server's copy of the database into its settings folder.</summary>
		void WaitForSavedCopy(AutomationElement scope, string profile, string captureName)
		{
			var path = SavedCopy(profile);
			try
			{
				Ui.WaitFor(() => File.Exists(path) ? path : null, TimeSpan.FromSeconds(60),
					"the program to save the server's copy of x360ce's controller database as " + path);
			}
			catch (TimeoutException)
			{
				// The picture shows the program's own message: the address, the answer or the file that was not right.
				Capture(scope, captureName);
				throw;
			}
		}

		/// <summary>Waits until version 4's grid shows exactly the SIDs of the database the program reads from its settings folder.</summary>
		void ListsTheDatabase(AutomationElement scope, AutomationElement grid, string profile, string captureName)
		{
			var expected = DatabaseSids(profile);
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
					return shown.SequenceEqual(expected) ? shown : null;
				}, TimeSpan.FromSeconds(60), "the window to list the " + expected.Length + " presets of x360ce's controller database");
			}
			catch (TimeoutException)
			{
				// The picture shows the program's own message; the lists show what it kept instead.
				Capture(scope, captureName + "-timeout.png");
				Console.WriteLine("database: " + string.Join(",", expected));
				Console.WriteLine("shown   : " + string.Join(",", shown ?? new string[0]));
				throw;
			}
			Capture(scope, captureName + ".png");
			Console.WriteLine("{0} preset rows from x360ce's controller database{1}", shown.Length,
				File.Exists(SavedCopy(profile)) ? " and the copy saved as " + SavedCopy(profile) : ", built in");
		}

		/// <summary>Version 3's list has no SID column, so its tab says how many rows it holds: one for each row of the database.</summary>
		void TabCountsTheDatabase(AutomationElement window, string profile)
		{
			// Controller Settings holds the three lists on its own tabs; no dialog in v3.
			SelectTab(window, "Controller Settings");
			SelectTab(window, "Default Settings for Most Popular Controllers");
			var title = "Default Settings for Most Popular Controllers [" + DatabaseSids(profile).Length + "]";
			Ui.WaitFor(() => window.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, title)),
				TimeSpan.FromSeconds(60), "the tab to say '" + title + "'");
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
			// A tab that holds a list says how many rows it has after its name.
			var tab = Ui.WaitFor(() => root.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem))
				.Cast<AutomationElement>().FirstOrDefault(x => x.Current.Name == name || (x.Current.Name ?? "").StartsWith(name + " [", StringComparison.Ordinal)),
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
			public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
			public const uint MOUSEEVENTF_LEFTUP = 0x0004;
			[System.Runtime.InteropServices.DllImport("user32.dll")]
			public static extern bool SetCursorPos(int x, int y);
			[System.Runtime.InteropServices.DllImport("user32.dll")]
			public static extern void mouse_event(uint flags, uint x, uint y, uint data, UIntPtr extra);
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
