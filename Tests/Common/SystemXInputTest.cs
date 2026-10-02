// @under-test: App.v4/Common/DInput/SystemXInput.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Runtime.InteropServices;
using x360ce.App.DInput;

namespace x360ce.Tests
{
	/// <summary>
	/// Windows' own XInput is asked, on the device thread, which places it has given out. Asked
	/// by library name, Windows looked in the program's own folder first, and a copy of the
	/// program inside a 32-bit game's folder found that game's 32-bit library and died on it,
	/// taking the device thread with it. The library is now loaded by its full path in the
	/// system folder, and a library that cannot be loaded answers "no controller" instead.
	/// </summary>
	[TestClass]
	public class SystemXInputTest
	{
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		static extern IntPtr GetModuleHandle(string moduleName);

		/// <summary>The system folder holding libraries of the other bitness than this process.</summary>
		static string OtherBitnessSystemFolder()
		{
			if (!Environment.Is64BitOperatingSystem)
				return null;
			return Environment.Is64BitProcess
				? Environment.GetFolderPath(Environment.SpecialFolder.SystemX86)
				: Environment.GetFolderPath(Environment.SpecialFolder.System);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The XInput library comes from the system folder that matches the process")]
		public void Library_is_taken_from_the_matching_system_folder()
		{
			var connected = SystemXInput.IsConnected(0);
			var path = SystemXInput.LibraryPath;
			if (path == null)
				Assert.Inconclusive("This machine has no XInput library: " + SystemXInput.LoadError);
			var expected = !Environment.Is64BitProcess && Environment.Is64BitOperatingSystem
				? Environment.GetFolderPath(Environment.SpecialFolder.SystemX86)
				: Environment.GetFolderPath(Environment.SpecialFolder.System);
			StringAssert.StartsWith(path, expected, StringComparison.OrdinalIgnoreCase,
				"The library must come from the system folder, never from the program's own folder.");
			Assert.IsNull(SystemXInput.LoadError);
			// A question about a place is answered, not thrown, whatever is plugged in.
			Console.WriteLine("Place 1 connected: " + connected);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Letting go of XInput unloads it, so the controllers it had open are closed")]
		public void Release_unloads_the_library()
		{
			if (SharpDX.XInput.Controller.IsLoaded)
				Assert.Inconclusive("Another part of this process holds the XInput library.");
			SystemXInput.IsConnected(0);
			var path = SystemXInput.LibraryPath;
			if (path == null)
				Assert.Inconclusive("This machine has no XInput library: " + SystemXInput.LoadError);
			Assert.IsTrue(SystemXInput.Release(TimeSpan.FromSeconds(5)));
			Assert.AreEqual(IntPtr.Zero, GetModuleHandle(path),
				"XInput stayed loaded, and with it every controller it had answered about, so Windows could not give their places away.");
			// The next question loads it again.
			SystemXInput.IsConnected(0);
			Assert.IsNotNull(SystemXInput.LibraryPath, "The library did not load again after being let go of: " + SystemXInput.LoadError);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A library of the wrong bitness is refused and reported, and the probe keeps answering")]
		public void Wrong_bitness_library_is_refused_without_an_exception()
		{
			var folder = OtherBitnessSystemFolder();
			var wrong = folder == null ? null : Path.Combine(folder, "xinput1_4.dll");
			if (wrong == null || !File.Exists(wrong))
				Assert.Inconclusive("No library of the other bitness on this machine.");
			try
			{
				Assert.IsFalse(SystemXInput.Load(wrong), "A library of the wrong bitness must not load.");
				Assert.IsNotNull(SystemXInput.LoadError, "The refusal must say why.");
				Assert.IsNull(SystemXInput.LibraryPath);
				// Nothing to ask, so nothing is connected and nothing throws.
				Assert.IsFalse(SystemXInput.IsConnected(0));
				Assert.AreEqual(false, SystemXInput.SetVibration(0, 0, 0, TimeSpan.FromSeconds(1)));
			}
			finally
			{
				// Back to the real library for the tests that follow.
				SystemXInput.Load(null);
			}
			Assert.IsNotNull(SystemXInput.LibraryPath, "The system library did not come back: " + SystemXInput.LoadError);
		}
	}
}
