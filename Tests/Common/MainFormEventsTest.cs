// @under-test: App.v4/MainForm.cs
// @area: startup   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace x360ce.Tests
{
	/// <summary>
	/// The main window lets go of every static event it listens to.
	/// </summary>
	/// <remarks>
	/// A static event keeps the window alive and calls into it after its controls are disposed. The current
	/// game changing as the program closed did exactly that and was reported against 4.24.60.0.
	/// </remarks>
	[TestClass]
	public class MainFormEventsTest
	{
		[TestMethod, TestCategory("startup"), TestCategory("critical")]
		[Description("Every handler the main window adds to a static event is removed again")]
		public void Every_static_event_handler_is_removed()
		{
			var source = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "MainForm.cs"));
			var added = Regex.Matches(source, @"(?:SettingsManager|Global|FormsTheme)[\w.]*\s*\+=\s*(\w+);")
				.Cast<Match>().Select(m => m.Groups[1].Value).Distinct().ToArray();
			Assert.IsTrue(added.Contains("CurrentGame_PropertyChanged"), "The current game handler was not found, so nothing was checked.");
			var kept = added.Where(h => !Regex.IsMatch(source, @"-=\s*" + h + ";")).ToArray();
			Assert.AreEqual(0, kept.Length, "Never removed: " + string.Join(", ", kept));
		}

		[TestMethod, TestCategory("startup"), TestCategory("critical")]
		[Description("The current game handler does nothing once the window is disposed")]
		public void The_current_game_handler_stops_once_disposed()
		{
			var source = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "MainForm.cs"));
			var handler = Ui.Between(source, "private void CurrentGame_PropertyChanged", "SettingsManager.Current.RaiseSettingsChanged");
			StringAssert.Contains(handler, "IsDisposed", "The handler reads disposed controls.");
		}
	}
}
