// @under-test: App.v4/Controls/PadControl.cs, App.v4/Common/SettingsManager.cs
// @area: settings   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// A setting this version does not use, and whether it survives being loaded and saved by it.
	/// </summary>
	/// <remarks>
	/// Map To was version 3's way of choosing the order controllers were handed to a game. It could
	/// work there because version 3 was itself the XInput a game loaded, so it presented the four in
	/// whatever order it liked. This version emulates through the virtual bus, where Windows gives out
	/// the places and cannot be asked for one, so the setting had nothing to act on: the box was shown,
	/// bound to a name with nothing behind it, and did nothing at all.
	///
	/// The box is gone. The value is not. Somebody running both versions against one configuration
	/// file would otherwise find that opening it here quietly emptied a setting the other one obeys,
	/// and nothing about opening a file should change what it says.
	/// </remarks>
	[TestClass]
	public class MapToPadPreservedTest
	{

		static string Read(string relative)
		{
			return File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, relative));
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("The setting still has a name, because the older version writes it")]
		public void The_setting_still_has_a_name()
		{
			// Removing the name would take it out of version 3 as well, which does use it.
			Assert.AreEqual("MapToPad", SettingName.MapToPad,
				"The name of the setting has changed or gone, so the older version can no longer " +
				"read and write what it always did.");
			StringAssert.Contains(Read(Path.Combine("App.v3", "Controls", "PadControl.cs")), "SettingName.MapToPad",
				"Version 3 no longer uses the setting, so keeping it here protects nothing.");
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("This version binds nothing to it, so it cannot write over it")]
		public void This_version_binds_nothing_to_it()
		{
			// A control bound to the setting is a control that saves whatever it happens to be showing,
			// and what it showed was nothing.
			var pad = Read(Path.Combine("App.v4", "Controls", "PadControl.cs"));
			Assert.IsFalse(pad.Contains("SettingName.MapToPad"),
				"A control in this version is bound to Map To. It has nothing behind it to read from, " +
				"so what it saves is whatever the empty box was showing - which overwrites the value " +
				"the older version put there.");
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("This version writes nothing into the older version's configuration file")]
		public void Saving_leaves_the_older_versions_file_alone()
		{
			// This version keeps its settings in its own files, so a setting it does not use stays in
			// x360ce.ini exactly as the older version left it. Code here that opens that file could
			// rewrite or drop every setting belonging to the other version.
			var app = Path.Combine(Ui.RepoRoot.FullName, "App.v4");
			var writers = Directory.GetFiles(app, "*.cs", SearchOption.AllDirectories)
				.Where(x => !x.Contains(@"\bin\") && !x.Contains(@"\obj\"))
				.Where(x => Regex.IsMatch(File.ReadAllText(x), @"new\s+Ini\("))
				.Select(x => x.Substring(app.Length + 1))
				.ToArray();
			Assert.AreEqual(0, writers.Length,
				"These files open x360ce.ini, so this version can change the older version's settings: "
				+ string.Join(", ", writers));
		}

	}
}
