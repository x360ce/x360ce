// @under-test: App.v3/Common/DirectInputState.cs, App.v3/Controls/PadControl.cs, Engine/Maps/SettingsConverter.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text.RegularExpressions;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>The text v3 records for a D-Pad direction is text the mapping parser accepts as a POV.</summary>
	/// <remarks>
	/// v3's recorder (<c>DirectInputState.CompareStates</c>) wrote "DPad N Up", a name the menu items stopped
	/// having in 2017. <see cref="SettingsConverter"/> never accepted "DPad", so a recorded D-Pad direction never
	/// round-tripped into a mapping, and the menu's own check for it (<c>PadControl.dPadRx</c>) never matched an
	/// action the parser had already accepted. The test project does not reference App.v3, so both source files
	/// are read as text and checked against the real Engine parser and a live regex match.
	/// </remarks>
	[TestClass]
	public class V3PovRecordingTest
	{
		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("The text DirectInputState records for each D-Pad direction parses as a POV")]
		public void The_recorded_text_parses_as_a_pov()
		{
			var path = Path.Combine(Ui.RepoRoot.FullName, "App.v3", "Common", "DirectInputState.cs");
			var text = File.ReadAllText(path);
			var format = Regex.Match(text, "string\\.Format\\(\"([^\"]+)\", i \\+ 1, DPadEnum\\.Up").Groups[1].Value;
			Assert.IsTrue(format.Length > 0, "The D-Pad direction format was not found.");
			foreach (var direction in new[] { "Up", "Right", "Down", "Left" })
			{
				var recorded = string.Format(format, 1, direction);
				Assert.IsTrue(SettingsConverter.TryParseTextValue(recorded, out var type, out var index),
					"'" + recorded + "' does not parse: v3 would record a D-Pad direction the mapping never recognises.");
				Assert.AreEqual(MapType.DPOVButton, type, "'" + recorded + "' parsed as " + type + ", not a POV direction.");
			}
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("PadControl's own check for a recorded D-Pad action matches the text DirectInputState now writes")]
		public void The_v3_check_matches_the_recorded_text()
		{
			var path = Path.Combine(Ui.RepoRoot.FullName, "App.v3", "Controls", "PadControl.cs");
			var text = File.ReadAllText(path);
			var pattern = Regex.Match(text, "Regex dPadRx = new Regex\\(\"([^\"]+)\"\\)").Groups[1].Value;
			Assert.IsTrue(pattern.Length > 0, "dPadRx was not found.");
			Assert.IsTrue(Regex.IsMatch("POV 1 Up", pattern),
				"dPadRx ('" + pattern + "') does not match 'POV 1 Up', the text DirectInputState now records.");
		}
	}
}
