// @under-test: App.v4/Controls/PadControl.cs, Engine/Data/PadSetting.cs
// @area: settings   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// Whether every setting that can be stored has somewhere to be changed.
	/// </summary>
	/// <remarks>
	/// A setting is stored because a control writes it. Add the storage and forget the control and
	/// the setting exists, is saved, is part of the name a set of settings is stored under, and can
	/// never be set to anything.
	///
	/// The program already refuses to start in that state, which is right, but it finds out while
	/// opening a window - one complaint per controller, four modal boxes stacked over each other with
	/// the buttons of the ones behind out of reach, and nothing to do but stop it from Task Manager.
	/// That is a bad place to learn it. Here it is a failing test with the names in it.
	/// </remarks>
	[TestClass]
	public class PadSettingMappedTest
	{
		/// <summary>
		/// The one setting deliberately not offered, matching the check the program makes at startup.
		/// </summary>
		static readonly string[] NotOffered = { "ButtonBig" };

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("Every setting that can be stored is bound to a control")]
		public void Every_setting_that_can_be_stored_is_bound_to_a_control()
		{
			var source = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName,
				"App.v4", "Controls", "PadControl.cs"));
			// The names the controller page binds. Read from the source rather than by building the
			// page, because building it needs a window, and this has to answer without one.
			var bound = new HashSet<string>(
				Regex.Matches(source, @"SettingName\.(?<name>\w+)").Cast<Match>()
					.Select(x => x.Groups["name"].Value),
				StringComparer.Ordinal);
			Assert.IsTrue(bound.Count > 50,
				"Only " + bound.Count + " binding(s) were read from the controller page, so this " +
				"measured almost nothing.");

			var storable = typeof(PadSetting).GetProperties()
				.Where(x => x.PropertyType == typeof(string))
				.Select(x => x.Name)
				.Where(x => !NotOffered.Contains(x))
				.ToArray();
			var unbound = storable.Where(x => !bound.Contains(x)).OrderBy(x => x).ToArray();

			Assert.AreEqual(0, unbound.Length,
				"These settings can be stored and cannot be changed, because nothing on the controller "
				+ "page writes them. The program will refuse to start and say so four times over:"
				+ Environment.NewLine + string.Join(", ", unbound));
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("The two pass-through settings are bound, each to its own control")]
		public void The_two_pass_through_settings_are_bound_to_their_own_controls()
		{
			// Named rather than left to the sweep above, because they are the ones that arrived with
			// storage first and a control afterwards, and because they must not share a control with
			// the pass-through that sends everything - the two send different things to different
			// places, and one box cannot mean both.
			var source = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName,
				"App.v4", "Controls", "PadControl.cs"));
			var pairs = new Dictionary<string, string>
			{
				{ "SettingName.ForcePassThrough", "ForcePassThroughCheckBox" },
				{ "SettingName.ForcePassThroughIndex", "ForcePassThroughIndexComboBox" },
			};
			foreach (var pair in pairs)
			{
				var call = Regex.Match(source,
					@"AddMap\(\(\) => " + Regex.Escape(pair.Key) + @",\s*(?<control>\w+)");
				Assert.IsTrue(call.Success, pair.Key + " is not bound to any control.");
				Assert.AreEqual(pair.Value, call.Groups["control"].Value,
					pair.Key + " is bound to the wrong control.");
			}
		}

	}
}
