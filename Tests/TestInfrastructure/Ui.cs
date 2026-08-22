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
		public static DirectoryInfo RepoRoot
		{
			get
			{
				var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
				while (dir != null && !File.Exists(Path.Combine(dir.FullName, "x360ce.slnx")))
					dir = dir.Parent;
				if (dir == null)
					throw new InvalidOperationException("Repository root not found above " + AppDomain.CurrentDomain.BaseDirectory);
				return dir;
			}
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
					throw new InvalidOperationException($"Process exited with code {p.ExitCode} before a window appeared.");
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

	}
}
