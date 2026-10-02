// @under-test: Engine/JocysCom/Controls/IssuesControl/IssueHelper.cs
// @area: diagnostics   @layer: unit
using JocysCom.ClassLibrary.Controls.IssuesControl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace x360ce.Tests
{
	/// <summary>
	/// A fix that installs something runs the installer, waits for it and keeps what it answered, so the
	/// Issues tab can say what happened instead of starting a program and looking away.
	/// </summary>
	[TestClass]
	public class IssueHelperTest
	{
		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("The installer gets its arguments, is waited for, and its exit code comes back")]
		public void Download_and_install_waits_and_returns_the_exit_code()
		{
			// Windows' own command interpreter stands in for an installer: it is always there, it is
			// "downloaded" through a file address with no network, and "/c exit N" ends it with code N.
			// Getting the code back proves the arguments arrived and the call waited for the end.
			var source = new Uri(Path.Combine(Environment.SystemDirectory, "cmd.exe"));
			var folder = Path.Combine(Path.GetTempPath(), "x360ce.Tests." + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(folder);
			try
			{
				var localPath = Path.Combine(folder, "cmd.exe");
				var exitCode = IssueHelper.DownloadAndInstall(source, localPath, null, false, "/c exit 1638");
				Assert.AreEqual((int?)1638, exitCode, "The installer's exit code did not come back.");
			}
			finally
			{
				Directory.Delete(folder, true);
			}
		}
	}
}
