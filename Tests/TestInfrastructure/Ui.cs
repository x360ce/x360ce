using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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
		/// Locate a built application, preferring Release so the tests exercise what ships.
		/// </summary>
		/// <param name="appFolder">App.v3 or App.v4.</param>
		/// <returns>Path of the newest matching x360ce.exe, or null when none is built.</returns>
		public static string FindApp(string appFolder)
		{
			var bin = new DirectoryInfo(Path.Combine(RepoRoot.FullName, appFolder, "bin"));
			if (!bin.Exists)
				return null;
			var exe = bin.GetDirectories()
				.SelectMany(d => d.GetFiles("x360ce.exe"))
				.OrderByDescending(f => f.Directory.Name.StartsWith("Release", StringComparison.OrdinalIgnoreCase))
				.ThenByDescending(f => f.LastWriteTimeUtc)
				.FirstOrDefault();
			return exe?.FullName;
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

		/// <summary>Close the window if open, then make sure the process is gone.</summary>
		public static void CloseApp(Process p)
		{
			if (p == null)
				return;
			try
			{
				if (!p.HasExited)
				{
					p.CloseMainWindow();
					p.WaitForExit(5000);
				}
				if (!p.HasExited)
					p.Kill();
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

		/// <summary>Minimise the main window, the way a user sends the application to the tray.</summary>
		public static void Minimize(Process p)
		{
			NativeMethods.ShowWindow(p.MainWindowHandle, NativeMethods.SW_MINIMIZE);
		}

		/// <summary>Restore the main window from minimised.</summary>
		public static void Restore(Process p)
		{
			NativeMethods.ShowWindow(p.MainWindowHandle, NativeMethods.SW_RESTORE);
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
