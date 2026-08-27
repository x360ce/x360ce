// @under-test: App.v4/MainForm.cs
// @area: lifetime   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace x360ce.Tests
{
	/// <summary>
	/// A window which listens to a static event has to stop listening when it closes.
	/// </summary>
	/// <remarks>
	/// A static event keeps its handlers for as long as the program runs, so a form which
	/// subscribes and never unsubscribes stays reachable after its window is gone, and carries on
	/// being called. The handler then reads controls which have already been disposed. A disposed
	/// ToolStripComboBox answers null for its ComboBox, so that read fails outright rather than
	/// quietly doing nothing, and the failure is reported from a place which says nothing about
	/// the cause.
	///
	/// That is what happened. The main window listened for the game in front changing, which the
	/// foreground watch keeps announcing while the program shuts down. The subscription is one
	/// line and the release is one line, half a file apart, so it is checked here rather than left
	/// to be noticed in review.
	/// </remarks>
	[TestClass]
	public class StaticEventReleaseTest
	{

		[TestMethod, TestCategory("lifetime")]
		[Description("Every static event the main window listens to is released again")]
		public void The_main_window_releases_every_static_event_it_listens_to()
		{
			var path = Path.Combine(Ui.RepoRoot.FullName, "App.v4", "MainForm.cs");
			Assert.IsTrue(File.Exists(path), path + " was not found.");
			var text = File.ReadAllText(path);
			var subscriptions = Handlers(text, "+=");
			var releases = Handlers(text, "-=");
			// Without this the test would pass by reading nothing at all, which is the one way it
			// could go wrong without anybody noticing.
			Assert.AreNotEqual(0, subscriptions.Count,
				"No subscription to a static event was found in " + path +
				", so this test is no longer reading what it was written to read.");
			var unreleased = new List<string>();
			foreach (var one in subscriptions)
				if (!releases.Contains(one))
					unreleased.Add(one);
			Assert.AreEqual(0, unreleased.Count,
				"The main window subscribes to " + string.Join(", ", unreleased.ToArray()) +
				" and never lets go, so it is still called after its window has been disposed.");
		}

		/// <summary>Each "SettingsManager.{event} {sign} {handler}" written in the file.</summary>
		/// <remarks>
		/// Only events reached straight off the class are read. Anything behind an instance, such
		/// as the options object, is released with that object and is not what this is about.
		/// </remarks>
		static HashSet<string> Handlers(string text, string sign)
		{
			var found = new HashSet<string>();
			var rx = new Regex(@"SettingsManager\.(\w+)\s*" + Regex.Escape(sign) + @"\s*([\w.]+)\s*;");
			foreach (Match m in rx.Matches(text))
				found.Add(m.Groups[1].Value + " " + m.Groups[2].Value);
			return found;
		}

	}
}
