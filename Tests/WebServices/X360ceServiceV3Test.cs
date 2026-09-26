// @under-test: Web/WebServices/x360ce.asmx.cs, Web/WebServices/x360ce.asmx.v3.cs
// @area: webservice   @layer: integration-api
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// What a v3 program depends on when it talks to the settings database.
	/// </summary>
	/// <remarks>
	/// v3 (<c>App.v3</c>) calls five operations: <c>SearchSettings</c> for its three lists,
	/// <c>LoadSetting</c> when a row is chosen, <c>SaveSetting</c> and <c>DeleteSetting</c> from
	/// its own settings grid, and <c>GetPrograms</c> for the game list. Each test here is one of
	/// those dependencies, sent through the same <see cref="WebServiceClient"/> the program uses.
	/// Set <c>X360CE_WEBSERVICE_URL</c> to choose the service; tests tagged <c>writes</c> create
	/// and remove a setting of their own and are meant for a local site.
	/// </remarks>
	[TestClass]
	public class X360ceServiceV3Test
	{
		/// <summary>
		/// The crash the owner reported on 2026-09-19: the summaries arrived and the pad settings
		/// did not, because the hosted site's data helper dropped the second result set of
		/// <c>x360ce_GetPresets</c>. A v3 program shows the row and then cannot load it.
		/// </summary>
		public static void SearchByProductReturnsAPadSettingForEverySummary(WebServiceClient ws, Guid productGuid)
		{
			var args = new[]
			{
				new SearchParameter { ProductGuid = productGuid },
				new SearchParameter { FileName = "x360ce.exe", FileProductName = "TocaEdit Xbox 360 Controller Emulator" },
			};
			var result = ws.SearchSettings(args);
			Assert.IsNotNull(result.Summaries, "Summaries");
			Assert.IsNotNull(result.PadSettings, "PadSettings");
			Assert.IsTrue(result.Summaries.Length > 0, "No presets for product " + productGuid + "; pick a product both databases have.");
			AssertPadSettingsAccompany(ws, result.Summaries.Select(x => x.PadSettingChecksum), result.PadSettings, "summaries");
		}

		/// <summary>
		/// Every row of the first result set has its pad setting in the second, unless the pad
		/// setting row itself is gone from the database (a cleanup can remove it; LoadSetting then
		/// has nothing either). The defect this guards against loses the whole second result set.
		/// </summary>
		static void AssertPadSettingsAccompany(WebServiceClient ws, System.Collections.Generic.IEnumerable<Guid> wanted, PadSetting[] padSettings, string what)
		{
			var keys = wanted.Distinct().ToArray();
			var present = padSettings.Select(x => x.PadSettingChecksum).ToArray();
			var missing = keys.Where(x => !present.Contains(x)).ToArray();
			if (missing.Length == 0)
				return;
			var loadable = (ws.LoadSetting(missing).PadSettings ?? new PadSetting[0]).Select(x => x.PadSettingChecksum).ToArray();
			Assert.AreEqual(0, loadable.Length, string.Format(
				"{0} of {1} {2} have a pad setting in the database that did not come back with them (first: {3}). The program lists them and cannot load them.",
				loadable.Length, keys.Length, what, loadable.FirstOrDefault()));
			Console.WriteLine("{0} of {1} {2} have no pad setting row at all: {3}", missing.Length, keys.Length, what, string.Join(", ", missing));
		}

		[TestMethod, TestCategory("webservice"), TestCategory("v3")]
		[Description("Default Settings for My Controllers: a pad setting comes back for every summary")]
		public void Search_by_product_returns_a_pad_setting_for_every_summary()
		{
			using (var ws = WebServiceTarget.Client())
				SearchByProductReturnsAPadSettingForEverySummary(ws, WebServiceTarget.LogitechG27ProductGuid);
		}

		[TestMethod, TestCategory("webservice"), TestCategory("v3")]
		[Description("Default Settings for Most Popular Controllers: one empty parameter returns presets with their pad settings")]
		public void Search_with_one_empty_parameter_returns_popular_presets_with_pad_settings()
		{
			using (var ws = WebServiceTarget.Client())
			{
				// This is literally what both programs send for that tab.
				var result = ws.SearchSettings(new[] { new SearchParameter() });
				Assert.IsNotNull(result.Presets, "Presets");
				Assert.IsTrue(result.Presets.Length > 0, "No popular presets");
				AssertPadSettingsAccompany(ws, result.Presets.Select(x => x.PadSettingChecksum), result.PadSettings, "presets");
				Assert.IsTrue(result.Presets.All(x => !string.IsNullOrEmpty(x.ProductName)), "A preset without a product name");
			}
		}

		[TestMethod, TestCategory("webservice"), TestCategory("v3")]
		[Description("My Settings for an instance nobody has: four empty lists, no fault")]
		public void Search_by_unknown_instance_returns_empty_lists_without_a_fault()
		{
			using (var ws = WebServiceTarget.Client())
			{
				var result = ws.SearchSettings(new[] { new SearchParameter { InstanceGuid = Guid.NewGuid() } });
				Assert.IsNotNull(result.Settings, "Settings");
				Assert.AreEqual(0, result.Settings.Length, "Settings");
				Assert.IsNotNull(result.PadSettings, "PadSettings");
				Assert.AreEqual(0, result.PadSettings.Length, "PadSettings");
				Assert.IsNotNull(result.Summaries, "Summaries");
				Assert.IsNotNull(result.Presets, "Presets");
			}
		}

		[TestMethod, TestCategory("webservice"), TestCategory("v3"), TestCategory("fixed-4-23")]
		[Description("No parameters at all: the service answers, it does not fault")]
		public void Search_with_no_parameters_answers_empty()
		{
			// A v4 grid with no devices sends an empty list, which reaches the service as null. The
			// live service of 2026-09-19 faults on it ("Value cannot be null"); 4.23 answers empty.
			using (var ws = WebServiceTarget.Client())
			{
				var result = ws.SearchSettings(new SearchParameter[0]);
				Assert.IsNotNull(result);
				Assert.AreEqual(0, (result.Presets ?? new Preset[0]).Length);
				Assert.AreEqual(0, (result.PadSettings ?? new PadSetting[0]).Length);
			}
		}

		[TestMethod, TestCategory("webservice"), TestCategory("v3")]
		[Description("Load Selected Preset: the checksums from a search load the same pad settings")]
		public void Load_setting_returns_the_pad_settings_the_search_named()
		{
			using (var ws = WebServiceTarget.Client())
			{
				var search = ws.SearchSettings(new[] { new SearchParameter { ProductGuid = WebServiceTarget.LogitechG27ProductGuid } });
				var wanted = search.Summaries.Select(x => x.PadSettingChecksum).Distinct().ToArray();
				Assert.IsTrue(wanted.Length > 0, "Nothing to load");
				var loaded = ws.LoadSetting(wanted);
				Assert.IsNotNull(loaded.PadSettings, "PadSettings");
				var got = loaded.PadSettings.Select(x => x.PadSettingChecksum).ToArray();
				CollectionAssert.AreEquivalent(wanted, got);
				// The row the program keeps is the whole mapping, not just the key.
				Assert.IsTrue(loaded.PadSettings.All(x => x.ButtonA != null || x.LeftThumbAxisX != null || x.DPad != null), "A loaded pad setting has no mapping at all");
			}
		}

		[TestMethod, TestCategory("webservice"), TestCategory("v3")]
		[Description("Load Selected Preset for a checksum nobody has: empty list, no fault")]
		public void Load_setting_for_an_unknown_checksum_returns_an_empty_list()
		{
			using (var ws = WebServiceTarget.Client())
			{
				var loaded = ws.LoadSetting(new[] { Guid.NewGuid() });
				Assert.AreEqual(0, (loaded.PadSettings ?? new PadSetting[0]).Length);
			}
		}

		[TestMethod, TestCategory("webservice"), TestCategory("v3")]
		[Description("The game list has entries with file names")]
		public void Get_programs_returns_enabled_programs_with_file_names()
		{
			using (var ws = WebServiceTarget.Client())
			{
				var programs = ws.GetPrograms(EnabledState.Enabled, 2);
				Assert.IsNotNull(programs);
				Assert.IsTrue(programs.Count > 0, "No programs");
				Assert.IsTrue(programs.All(x => !string.IsNullOrEmpty(x.FileName)), "A program without a file name");
			}
		}

		[TestMethod, TestCategory("webservice"), TestCategory("v3"), TestCategory("writes")]
		[Description("Save, find, delete: the program's own settings round-trip and 'success' is an empty string")]
		public void Save_setting_is_found_by_instance_and_delete_removes_it()
		{
			if (WebServiceTarget.IsLive)
				Assert.Inconclusive("Writes are not run against the public site.");
			using (var ws = WebServiceTarget.Client())
			{
				// The same mapping every run, so the server ever creates one pad setting row for
				// this test; only the user setting that links this run's instance to it is new.
				var padSetting = new PadSetting
				{
					ButtonA = "1", ButtonB = "2", ButtonX = "3", ButtonY = "4",
					LeftThumbAxisX = "1", LeftThumbAxisY = "2", DPad = "1",
					LeftTrigger = "a-6", RightTrigger = "a-2",
				};
				// What the program computes before it asks, with the 4.22 wheel settings blank,
				// must be what the server links the setting to: same algorithm on both ends.
				var expectedChecksum = padSetting.CleanAndGetCheckSum();
				var instanceGuid = Guid.NewGuid();
				var setting = new UserSetting
				{
					InstanceGuid = instanceGuid,
					InstanceName = "x360ce.Tests instance",
					ProductGuid = WebServiceTarget.LogitechG27ProductGuid,
					ProductName = "Logitech G27 Racing Wheel USB",
					FileName = "x360ce.Tests.exe",
					FileProductName = "x360ce test run",
					Comment = "Created by X360ceServiceV3Test; safe to delete",
					DeviceType = 0,
					MapTo = 1,
					IsEnabled = true,
					Completion = 0,
				};
				string saved = null;
				try
				{
					saved = ws.SaveSetting(setting, padSetting);
					Assert.AreEqual("", saved, "SaveSetting reports success as an empty string; got: " + saved);

					var mine = ws.SearchSettings(new[] { new SearchParameter { InstanceGuid = instanceGuid } });
					Assert.AreEqual(1, mine.Settings.Length, "The saved setting is found by its instance");
					Assert.AreEqual(expectedChecksum, mine.Settings[0].PadSettingChecksum, "Linked to the checksum the program computed for the pad setting it sent");
					Assert.IsTrue(mine.PadSettings.Any(x => x.PadSettingChecksum == expectedChecksum), "Its pad setting comes back with it (the second result set)");
					var loaded = ws.LoadSetting(new[] { expectedChecksum }).PadSettings;
					Assert.AreEqual(1, loaded.Length, "LoadSetting finds it by that checksum");
					Assert.AreEqual("a-6", loaded[0].LeftTrigger, "The mapping round-trips");
				}
				finally
				{
					if (saved != null)
					{
						Assert.AreEqual("", ws.DeleteSetting(setting), "DeleteSetting reports success as an empty string");
						Assert.AreEqual("Setting not found", ws.DeleteSetting(setting), "A second delete says so");
					}
				}
			}
		}

		[TestMethod, TestCategory("webservice"), TestCategory("v3"), TestCategory("writes")]
		[Description("Saving again for the same controller replaces its setting with the one just saved")]
		public void Saving_again_for_the_same_controller_replaces_its_setting()
		{
			if (WebServiceTarget.IsLive)
				Assert.Inconclusive("Writes are not run against the public site.");
			using (var ws = WebServiceTarget.Client())
			{
				// Two fixed mappings, so the server only ever creates these two pad setting rows. The second
				// adds the right stick axis the first leaves out: a controller saved without it, then saved
				// again with it, was still loaded without it, because the first save was the one kept.
				var withoutAxis = new PadSetting { ButtonA = "2", ButtonB = "3", ButtonX = "1", ButtonY = "4", LeftThumbAxisX = "1", DPad = "1" };
				var withAxis = new PadSetting { ButtonA = "2", ButtonB = "3", ButtonX = "1", ButtonY = "4", LeftThumbAxisX = "1", DPad = "1", RightThumbAxisX = "3" };
				var expectedChecksum = withAxis.CleanAndGetCheckSum();
				var setting = new UserSetting
				{
					InstanceGuid = Guid.NewGuid(),
					InstanceName = "x360ce.Tests instance",
					ProductGuid = WebServiceTarget.LogitechG27ProductGuid,
					ProductName = "Logitech G27 Racing Wheel USB",
					FileName = "x360ce.Tests.exe",
					FileProductName = "x360ce test run",
					Comment = "Created by X360ceServiceV3Test; safe to delete",
					IsEnabled = true,
				};
				try
				{
					Assert.AreEqual("", ws.SaveSetting(setting, withoutAxis), "The first save");
					Assert.AreEqual("", ws.SaveSetting(setting, withAxis), "The second save");
					var mine = ws.SearchSettings(new[] { new SearchParameter { InstanceGuid = setting.InstanceGuid } });
					Assert.AreEqual(1, mine.Settings.Length, "Saving again keeps one setting for the controller");
					Assert.AreEqual(expectedChecksum, mine.Settings[0].PadSettingChecksum,
						"The controller's setting still links to the first pad setting saved for it, not the one just saved.");
					Assert.AreEqual("3", ws.LoadSetting(new[] { expectedChecksum }).PadSettings.Single().RightThumbAxisX,
						"The right stick axis saved the second time is not what loads.");
				}
				finally
				{
					ws.DeleteSetting(setting);
				}
			}
		}
	}
}
