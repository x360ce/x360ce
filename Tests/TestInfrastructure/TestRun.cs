// @under-test: Engine/JocysCom/Runtime/LogHelper.Exception.cs
// @area: diagnostics   @layer: unit
using JocysCom.ClassLibrary.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace x360ce.Tests
{
	/// <summary>
	/// Assembly-wide setup.
	/// </summary>
	[TestClass]
	public static class TestRun
	{

		/// <summary>Folder every crash report written during this run goes to.</summary>
		public static string LogsFolder { get; private set; }

		/// <summary>
		/// Send crash reports somewhere harmless for the whole run.
		/// </summary>
		/// <remarks>
		/// Redirecting inside one test class is not enough. Any unhandled exception raised while
		/// no test owns the setting - a timeout in a UI test, for instance - lands in the real
		/// application data folder, and the product then counts those files and reports errors to
		/// the user that never happened. Setting this once for the assembly means no test run can
		/// write there. Individual tests may still point it at their own folder and restore it,
		/// because restoring returns it to this value rather than to the product default.
		/// </remarks>
		[AssemblyInitialize]
		public static void Initialize(TestContext context)
		{
			LogsFolder = Path.Combine(Path.GetTempPath(), "x360ce.Tests", "run-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(LogsFolder);
			LogHelper.Current.OverrideLogFolder = LogsFolder;
		}

		[AssemblyCleanup]
		public static void Cleanup()
		{
			try { Directory.Delete(LogsFolder, true); }
			catch (IOException) { }
			catch (UnauthorizedAccessException) { }
		}

	}
}
