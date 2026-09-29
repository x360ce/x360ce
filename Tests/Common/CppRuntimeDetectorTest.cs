// @under-test: App.v4/Issues/CppRuntimeDetector.cs, App.v4/Issues/CppX64RuntimeInstallIssue.cs, App.v4/Issues/CppX86RuntimeInstallIssue.cs, App.v4/ViGEm/Client/ViGEmClient.x360ce.cs
// @area: diagnostics   @layer: unit
using JocysCom.ClassLibrary.Controls.IssuesControl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nefarius.ViGEm.Client;
using System.Reflection;
using x360ce.App.Issues;

namespace x360ce.Tests
{
	/// <summary>
	/// The Visual C++ fix runs Microsoft's installer, and the Issues tab says what the installer answered.
	/// Each answer is turned into what the person does next: for 0x80070666, a runtime registered but not
	/// found, that is removing every runtime, restarting and installing again.
	/// </summary>
	[TestClass]
	public class CppRuntimeDetectorTest
	{
		const string Program = "Microsoft Visual C++ v14 Redistributable (x64)";

		static IssueSeverity Explain(int? exitCode, bool installed, out string text, out bool troubleshoot)
		{
			return CppRuntimeDetector.ExplainInstall(exitCode, installed, Program, out text, out troubleshoot);
		}

		static void Check(int exitCode, IssueSeverity severity, string phrase, bool troubleshoot)
		{
			string text;
			bool linked;
			Assert.AreEqual(severity, Explain(exitCode, false, out text, out linked), "Severity for exit code " + exitCode);
			StringAssert.Contains(text, phrase, "Text for exit code " + exitCode);
			Assert.AreEqual(troubleshoot, linked, "Troubleshooting link for exit code " + exitCode);
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("A detected runtime is no issue, whatever the installer said last")]
		public void A_detected_runtime_is_no_issue()
		{
			foreach (var code in new int?[] { null, 0, 3010, 1638, 1603, -1 })
			{
				string text;
				bool troubleshoot;
				Assert.AreEqual(IssueSeverity.None, Explain(code, true, out text, out troubleshoot), "Exit code " + code);
				Assert.IsNull(text, "Exit code " + code);
				Assert.IsFalse(troubleshoot, "Exit code " + code);
			}
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("Before the installer has run, the issue asks for the install as it always did")]
		public void Before_the_installer_runs_the_issue_asks_for_the_install()
		{
			string text;
			bool troubleshoot;
			Assert.AreEqual(IssueSeverity.Critical, Explain(null, false, out text, out troubleshoot));
			Assert.AreEqual("Install " + Program, text);
			Assert.IsFalse(troubleshoot);
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("Each answer from the installer is explained, with Microsoft's guide where it helps")]
		public void Each_installer_answer_is_explained()
		{
			Check(3010, IssueSeverity.Critical, "Restart Windows to finish installing", false);
			Check(1602, IssueSeverity.Critical, "was cancelled", false);
			// A cancelled prompt wrapped as a Windows error result, 0x800704C7, is the same answer.
			Check(unchecked((int)0x800704C7), IssueSeverity.Critical, "was cancelled", false);
			Check(1618, IssueSeverity.Critical, "Another installation is running", false);
			Check(-1, IssueSeverity.Critical, "administrator permission was refused", false);
			Check(0, IssueSeverity.Critical, "Restart Windows, then check again", false);
			Check(1603, IssueSeverity.Critical, "failed with code 1603", true);
			Check(unchecked((int)0x80091007), IssueSeverity.Critical, "failed with code 0x80091007", true);
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("A runtime already registered but not detected is Moderate, so the checks after it still run")]
		public void Already_registered_but_missing_is_moderate()
		{
			Check(1638, IssueSeverity.Moderate, "already registered", true);
			// The form the installer's own error window shows, 0x80070666, is the same answer.
			Check(unchecked((int)0x80070666), IssueSeverity.Moderate, "already registered", true);
		}

		[TestMethod, TestCategory("diagnostics")]
		[Description("Only a finished install counts as success")]
		public void Only_a_finished_install_counts_as_success()
		{
			Assert.IsTrue(CppRuntimeDetector.Succeeded(0));
			Assert.IsTrue(CppRuntimeDetector.Succeeded(3010));
			foreach (var code in new int?[] { null, -1, 1602, 1603, 1618, 1638 })
				Assert.IsFalse(CppRuntimeDetector.Succeeded(code), "Exit code " + code);
		}

		[TestMethod, TestCategory("diagnostics")]
		[Description("The installer runs without questions, shows its progress, and does not restart Windows by itself")]
		public void The_installer_runs_passively_without_restarting()
		{
			Assert.AreEqual("/install /passive /norestart", CppRuntimeDetector.InstallArguments);
		}

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("After a successful install the virtual bus asks about the runtime again instead of keeping its first answer")]
		public void The_bus_forgets_its_runtime_answer()
		{
			var field = typeof(ViGEmClient).GetField("_Runtime", BindingFlags.NonPublic | BindingFlags.Static);
			Assert.IsNotNull(field, "The runtime answer is no longer where this test looks for it.");
			var missing = typeof(ViGEmClient).GetField("RuntimeMissing", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
			var unknown = typeof(ViGEmClient).GetField("RuntimeUnknown", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
			var before = field.GetValue(null);
			try
			{
				field.SetValue(null, missing);
				ViGEmClient.ForgetRuntimeInstalled();
				Assert.AreEqual(unknown, field.GetValue(null), "The bus kept its answer, so a runtime installed now stays unseen until the program restarts.");
			}
			finally
			{
				field.SetValue(null, before);
			}
		}

		[TestMethod, TestCategory("diagnostics")]
		[Description("Both runtime fixes carry the shield, because Microsoft's installer asks for Administrator")]
		public void Both_runtime_fixes_carry_the_shield()
		{
			Assert.IsTrue(new CppX64RuntimeInstallIssue().FixNeedsAdmin);
			Assert.IsTrue(new CppX86RuntimeInstallIssue().FixNeedsAdmin);
		}
	}
}
