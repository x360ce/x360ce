using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Automation;

namespace x360ce.Tests
{
	/// <summary>
	/// The only place polling lives. Test bodies never sleep.
	/// </summary>
	public static class Ui
	{

		/// <summary>Repository root, found by walking up from the test assembly.</summary>
		/// <remarks>
		/// Several starting points are tried because the base directory of the application domain
		/// is not dependable: it becomes the test platform's own folder as soon as a referenced
		/// project brings a configuration file along, which has nothing to do with where the tests
		/// were built. The assembly's own path does not move.
		/// </remarks>
		public static DirectoryInfo RepoRoot
		{
			get
			{
				foreach (var start in StartingPoints())
				{
					var dir = string.IsNullOrEmpty(start) ? null : new DirectoryInfo(start);
					while (dir != null && !File.Exists(Path.Combine(dir.FullName, "x360ce.slnx")))
						dir = dir.Parent;
					if (dir != null)
						return dir;
				}
				throw new InvalidOperationException(
					"Repository root not found above any of: " + string.Join(", ", StartingPoints()));
			}
		}

		static string[] StartingPoints()
		{
			var assembly = typeof(Ui).Assembly;
			string fromCodeBase = null;
			try { fromCodeBase = Path.GetDirectoryName(new Uri(assembly.CodeBase).LocalPath); }
			catch (UriFormatException) { }
			return new[]
			{
				string.IsNullOrEmpty(assembly.Location) ? null : Path.GetDirectoryName(assembly.Location),
				fromCodeBase,
				AppDomain.CurrentDomain.BaseDirectory,
				Directory.GetCurrentDirectory(),
			}.Where(x => !string.IsNullOrEmpty(x)).ToArray();
		}

		/// <summary>
		/// Locate a built application to drive.
		/// </summary>
		/// <param name="appFolder">App.v3 or App.v4.</param>
		/// <returns>Path of the matching x360ce.exe, or null when none is built.</returns>
		/// <remarks>
		/// The build matching this test assembly's own configuration wins, and the most recently
		/// written build after that. Preferring Release unconditionally is what this used to do,
		/// and it meant a test run could drive a binary built days earlier while reporting success
		/// on source that had never been compiled.
		/// </remarks>
		public static string FindApp(string appFolder)
		{
			var bin = new DirectoryInfo(Path.Combine(RepoRoot.FullName, appFolder, "bin"));
			if (!bin.Exists)
				return null;
			var configuration = TestConfiguration;
			var exe = bin.GetDirectories()
				.SelectMany(d => d.GetFiles("x360ce.exe"))
				.OrderByDescending(f => f.Directory.Name.StartsWith(configuration, StringComparison.OrdinalIgnoreCase))
				.ThenByDescending(f => f.LastWriteTimeUtc)
				.FirstOrDefault();
			if (exe != null)
				Console.WriteLine("Driving " + exe.FullName + " (built " + exe.LastWriteTime + ")");
			return exe?.FullName;
		}

		/// <summary>Configuration this test assembly was built in, read from its own path.</summary>
		static string TestConfiguration
		{
			get
			{
				var location = typeof(Ui).Assembly.Location ?? "";
				return location.IndexOf(@"\bin\Release\", StringComparison.OrdinalIgnoreCase) >= 0
					? "Release" : "Debug";
			}
		}

		/// <summary>Poll until the probe returns a value, or fail with a useful message.</summary>
		public static T WaitFor<T>(Func<T> probe, TimeSpan timeout, string what) where T : class
		{
			var deadline = DateTime.UtcNow + timeout;
			while (DateTime.UtcNow < deadline)
			{
				T value = null;
				// A window can be mid-creation, which surfaces as a transient automation error.
				try { value = probe(); }
				catch (ElementNotAvailableException) { }
				if (value != null)
					return value;
				Thread.Sleep(100);
			}
			throw new TimeoutException($"Timed out after {timeout.TotalSeconds:N0}s waiting for {what}.");
		}

		/// <summary>Main window of the process, once it is present and has a title.</summary>
		public static AutomationElement WaitForMainWindow(Process p, TimeSpan timeout)
		{
			return WaitFor(() =>
			{
				p.Refresh();
				if (p.HasExited)
				{
					// x360ce allows one instance. A second launch hands off to the running one and
					// exits at once, which otherwise surfaces as an unexplained one-second failure.
					if (AnotherInstanceIsRunning(p))
						Assert.Inconclusive(
							"Another x360ce instance is already running, so this test could not drive its own. " +
							"Close it and run again.");
					throw new InvalidOperationException($"Process exited with code {p.ExitCode} before a window appeared.");
				}
				if (p.MainWindowHandle == IntPtr.Zero)
					return null;
				var element = AutomationElement.FromHandle(p.MainWindowHandle);
				return string.IsNullOrEmpty(element?.Current.Name) ? null : element;
			}, timeout, "the main window");
		}

		/// <summary>How long the application is given to shut itself down.</summary>
		/// <remarks>
		/// Closing is not instant: the program unplugs the controllers it created before it goes.
		/// Killing it instead leaves them plugged in, and Windows holds them until it restarts. Only
		/// four places exist for controllers of that kind, so what one test leaves behind takes the
		/// places the next test needs - and that test sees a controller which never moves, or one that
		/// moves on its own, with nothing saying why. Five seconds was not enough to shut down, so the
		/// suite was killing the program on every run and poisoning the run after it.
		/// </remarks>
		const int ShutdownMs = 20000;

		/// <summary>Close the window if open, then make sure the process is gone.</summary>
		public static void CloseApp(Process p)
		{
			var killed = false;
			if (p != null)
			{
				try
				{
					// Asked several times, with the window brought back into view and its handle read
					// again each time. This program hides itself in the tray, and a close sent to a window
					// that is no longer there does nothing and reports nothing. The test then killed it,
					// and the controllers it had plugged in stayed plugged in for every test after it.
					for (var attempt = 0; attempt < 4 && !p.HasExited; attempt++)
					{
						Restore(p);
						p.Refresh();
						p.CloseMainWindow();
						p.WaitForExit(ShutdownMs / 4);
					}
					if (!p.HasExited)
					{
						killed = true;
						p.Kill();
						p.WaitForExit(5000);
					}
				}
				catch (InvalidOperationException)
				{
					// Process already gone.
				}
				finally
				{
					p.Dispose();
				}
			}
			AssertNoLeftoverVirtualPads(killed);
		}

		/// <summary>Fails when controllers the run just ended created are still plugged in.</summary>
		/// <remarks>
		/// Asked here, as each test that starts the program finishes, so a leak is reported against the
		/// test which caused it rather than against whichever test happened to run next and behaved
		/// strangely for reasons of its own.
		/// </remarks>
		/// <param name="killed">Whether the program had to be killed, which is how they are left.</param>
		public static void AssertNoLeftoverVirtualPads(bool killed)
		{
			// Windows takes a moment to take the controllers away after the program lets go of them,
			// so the question is asked again for a while. Asking once reports a leak that is merely
			// half a second of ordinary tidying up.
			var deadline = DateTime.UtcNow.AddSeconds(15);
			var pads = x360ce.App.DInput.VirtualDriverInstaller.GetLeftoverVirtualPads();
			while (pads.Length > 0 && DateTime.UtcNow < deadline)
			{
				Thread.Sleep(500);
				pads = x360ce.App.DInput.VirtualDriverInstaller.GetLeftoverVirtualPads();
			}
			if (pads.Length == 0)
				return;
			Assert.Fail(pads.Length + " virtual controllers are still plugged in after the "
				+ "application closed" + (killed ? ", which had to be killed" : string.Empty)
				+ ". They hold the four places XInput has, so the next run reads somebody else's "
				+ "controller or none at all. Remove them from the Issues page, then run again. "
				+ string.Join(", ", pads.Take(8).Select(x => x.DeviceId).ToArray()));
		}

		/// <summary>The text every injected fault carries, and nothing a real failure carries.</summary>
		public const string InjectedFaultMarker = "Injected fault:";

		/// <summary>Removes the reports left behind by faults a test raised on purpose.</summary>
		/// <remarks>
		/// An injected fault is written to the same folder as a real one, so a suite that leaves them
		/// behind has the status bar counting the tests' own faults as the person's, and offers them to
		/// be sent to support. Only files naming an injected fault are removed, so a real report is
		/// never thrown away.
		/// </remarks>
		/// <param name="exePath">The application the test started, which decides where it writes.</param>
		public static void RemoveInjectedFaultReports(string exePath)
		{
			foreach (var folder in ErrorFolders(exePath))
			{
				if (!Directory.Exists(folder))
					continue;
				foreach (var file in Directory.GetFiles(folder, "*.htm"))
					if (File.ReadAllText(file).Contains(InjectedFaultMarker))
						File.Delete(file);
			}
		}

		/// <summary>Beside the application when it is carried around, otherwise the shared folder.</summary>
		static IEnumerable<string> ErrorFolders(string exePath)
		{
			yield return Path.Combine(Path.GetDirectoryName(exePath), "x360ce", "Errors");
			yield return Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "X360CE", "Errors");
		}

		/// <summary>Minimise the main window, the way a user sends the application to the tray.</summary>
		public static void Minimize(Process p)
		{
			p.Refresh();
			NativeMethods.ShowWindow(p.MainWindowHandle, NativeMethods.SW_MINIMIZE);
		}

		/// <summary>Restore the main window from minimised.</summary>
		/// <summary>Bring the window back into view, from the task bar or from the tray.</summary>
		/// <remarks>
		/// Minimising this program puts it in the tray and takes its window away: the handle becomes
		/// nothing, so showing it does nothing, closing it returns false, and the only way left is to
		/// kill it - which strands every controller it had plugged in.
		///
		/// The way back is the program's own. Only one copy of it runs, so starting a second tells the
		/// first to show itself, and the second ends. That is the same route a person takes by starting
		/// it again from the desktop, rather than something invented for a test.
		/// </remarks>
		public static void Restore(Process p)
		{
			p.Refresh();
			if (p.MainWindowHandle != IntPtr.Zero)
			{
				NativeMethods.ShowWindow(p.MainWindowHandle, NativeMethods.SW_RESTORE);
				return;
			}
			string exe;
			try { exe = p.MainModule.FileName; }
			catch (InvalidOperationException) { return; }
			using (var asker = Process.Start(new ProcessStartInfo(exe)
			{
				WorkingDirectory = Path.GetDirectoryName(exe),
				UseShellExecute = false,
			}))
			{
				if (asker != null)
					asker.WaitForExit(15000);
			}
			WaitFor(() =>
			{
				p.Refresh();
				return p.HasExited || p.MainWindowHandle != IntPtr.Zero ? "shown" : null;
			}, TimeSpan.FromSeconds(15), "the window to come back from the tray");
		}

		private static class NativeMethods
		{
			public const int SW_MINIMIZE = 6;
			public const int SW_RESTORE = 9;

			[System.Runtime.InteropServices.DllImport("user32.dll")]
			public static extern bool ShowWindow(IntPtr window, int command);
		}

		/// <summary>True when an x360ce process other than this one is alive.</summary>
		private static bool AnotherInstanceIsRunning(Process launched)
		{
			return Process.GetProcessesByName("x360ce").Any(x => x.Id != launched.Id);
		}

	}
}
