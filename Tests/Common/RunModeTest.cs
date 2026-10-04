// @under-test: Engine/JocysCom/Runtime/LogHelper.cs, Engine/JocysCom/Configuration/AssemblyInfo.cs, App.v4/MainForm.cs
// @area: diagnostics   @layer: unit
using JocysCom.ClassLibrary.Configuration;
using JocysCom.ClassLibrary.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace x360ce.Tests
{
	/// <summary>What a program with no run mode setting calls itself in its error reports.</summary>
	/// <remarks>
	/// The released program carries no configuration file, so it has no RunMode setting. Error reports
	/// and the window title both take that absence to mean a release.
	/// </remarks>
	[TestClass]
	public class RunModeTest
	{
		Func<string, string> _getValue;
		string _defaultRunMode;

		[TestInitialize]
		public void Before()
		{
			_getValue = SettingsParser._GetValue;
			_defaultRunMode = LogHelper.DefaultRunMode;
			// As the program sets it at start, before anything is logged.
			LogHelper.DefaultRunMode = "LIVE";
		}

		[TestCleanup]
		public void After()
		{
			SettingsParser._GetValue = _getValue;
			LogHelper.DefaultRunMode = _defaultRunMode;
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("The program starts by calling itself a release, for when it has no run mode setting")]
		public void Program_starts_as_a_release()
		{
			var mainForm = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "MainForm.cs"));
			StringAssert.Contains(mainForm, "LogHelper.DefaultRunMode = \"LIVE\";");
		}

		/// <summary>Settings as a configuration file would give them; null is a setting that is not there.</summary>
		static void Settings(string runMode, string environment)
		{
			SettingsParser._GetValue = name => name == "RunMode" ? runMode : name == "Environment" ? environment : null;
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("With no run mode setting the program is a release, and its reports add nothing to its name")]
		public void No_setting_means_a_release()
		{
			Settings(null, null);
			Assert.AreEqual("LIVE", LogHelper.RunMode);
			Assert.IsTrue(LogHelper.IsLive);
			var name = "Jocys.com x360ce 4.24.20.0";
			LogHelper.ApplyRunModeSuffix(ref name);
			Assert.AreEqual("Jocys.com x360ce 4.24.20.0", name, "A released program's report calls it a test build.");
		}

		[TestMethod, TestCategory("diagnostics")]
		[Description("A run mode that is set, under either name, is still used")]
		public void A_set_run_mode_is_used()
		{
			Settings("TEST", null);
			Assert.AreEqual("TEST", LogHelper.RunMode);
			var name = "x360ce";
			LogHelper.ApplyRunModeSuffix(ref name);
			Assert.AreEqual("x360ce (TEST)", name);
			Settings(null, "DEV");
			Assert.AreEqual("DEV", LogHelper.RunMode);
		}

		[TestMethod, TestCategory("diagnostics")]
		[Description("The window title and the reports agree about a program with no setting")]
		public void Title_and_reports_agree()
		{
			Settings(null, null);
			var title = new AssemblyInfo(typeof(LogHelper).Assembly).GetTitle(true, true, false, false, false);
			Assert.IsFalse(title.Contains("TEST"), title);
			Assert.IsTrue(LogHelper.IsLive, "The title says release and the reports say test.");
		}
	}
}
