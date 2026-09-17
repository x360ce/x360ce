// @under-test: App.v4/Common/DInput/DInputHelper.Step6.RetrieveXiStates.cs, App.v4/MainForm.cs
// @area: engine   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text.RegularExpressions;
using x360ce.App.DInput;

namespace x360ce.Tests
{
	/// <summary>
	/// XInput stops answering while Windows takes controllers away and builds them again, which
	/// removing leftover controllers does. The reads must rest for a while and then go on by
	/// themselves; nothing may answer the timeout by switching the person's XInput view off.
	/// </summary>
	[TestClass]
	public class XInputReadPauseTest
	{
		[TestMethod, TestCategory("engine")]
		[Description("A pause runs until its end and not past it, across the tick counter wrapping round")]
		public void The_pause_ends_when_it_ends()
		{
			Assert.IsTrue(DInputHelper.IsPaused(1000, 999));
			Assert.IsFalse(DInputHelper.IsPaused(1000, 1000));
			Assert.IsFalse(DInputHelper.IsPaused(1000, 5000));
			// The tick counter wraps after 25 days; a pause set just before must still end just after.
			Assert.IsTrue(DInputHelper.IsPaused(int.MinValue + 100, int.MaxValue - 100));
			Assert.IsFalse(DInputHelper.IsPaused(int.MinValue + 100, int.MinValue + 200));
			Assert.IsTrue(DInputHelper.XiReadPauseMs >= 1000 && DInputHelper.XiReadPauseMs <= 30000,
				"The pause is seconds, not an instant and not for ever.");
		}

		[TestMethod, TestCategory("engine")]
		[Description("Nothing in the window answers a read failure by switching the XInput view off")]
		public void A_read_failure_never_switches_the_view_off()
		{
			var path = Path.Combine(Ui.RepoRoot.FullName, "App.v4", "MainForm.cs");
			var source = File.ReadAllText(path);
			var handler = Regex.Match(source, @"void DHelper_StatesRetrieved\(.*?\n\t\t\}", RegexOptions.Singleline);
			Assert.IsTrue(handler.Success, "The handler for retrieved states was not found.");
			Assert.IsFalse(handler.Value.Contains("GetXInputStates"),
				"The handler for a failed read changes the XInput view setting. A timeout while Windows " +
				"rebuilds controllers used to switch the view off for good, with its button still showing on.");
		}
	}
}
