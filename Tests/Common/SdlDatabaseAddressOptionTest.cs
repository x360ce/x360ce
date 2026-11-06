// @under-test: App.v4/Common/Options.cs, App.v4/Controls/OptionsInternetUserControl.cs, App.v4/Controls/PadTabPages/SdlUserControl.cs, App.v4/Controls/SdlPresetsUserControl.cs
// @area: options   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// Where SDL's controller database is downloaded from: an option with a list of addresses, shown on Options > Internet in
	/// an editable drop-down like the web service address, beside an [Update SDL Database] button.
	/// </summary>
	[TestClass]
	public class SdlDatabaseAddressOptionTest
	{
		const string Mirror = "https://example.org/sdl/gamecontrollerdb.txt";
		const string Fork = "https://raw.githubusercontent.com/someone/SDL_GameControllerDB/main/gamecontrollerdb.txt";

		[TestMethod, TestCategory("critical")]
		[Description("The address starts as SDL_GameControllerDB's own file on GitHub, the one address listed, and an empty one becomes it")]
		public void The_default_address_is_sdl_game_controller_db_on_github()
		{
			Assert.AreEqual("https://raw.githubusercontent.com/mdqinc/SDL_GameControllerDB/master/gamecontrollerdb.txt", Options.DefaultSdlDatabaseUrl);
			var options = new Options();
			options.InitDefaults();
			Assert.AreEqual(Options.DefaultSdlDatabaseUrl, options.SdlDatabaseUrl);
			CollectionAssert.AreEqual(new[] { Options.DefaultSdlDatabaseUrl }, options.SdlDatabaseUrls);
			options = new Options { SdlDatabaseUrl = "", SdlDatabaseUrls = new System.ComponentModel.BindingList<string>() };
			options.InitDefaults();
			Assert.AreEqual(Options.DefaultSdlDatabaseUrl, options.SdlDatabaseUrl, "An empty address was kept.");
			CollectionAssert.AreEqual(new[] { Options.DefaultSdlDatabaseUrl }, options.SdlDatabaseUrls, "An empty list was kept.");
		}

		[TestMethod, TestCategory("critical")]
		[Description("A saved address missing from the list is added to it once, so the box shows it")]
		public void A_saved_address_missing_from_the_list_is_added_to_it()
		{
			var options = new Options { SdlDatabaseUrl = Mirror, SdlDatabaseUrls = new System.ComponentModel.BindingList<string> { Options.DefaultSdlDatabaseUrl, Fork } };
			options.InitDefaults();
			options.InitDefaults();
			Assert.AreEqual(Mirror, options.SdlDatabaseUrl, "The saved address was changed.");
			CollectionAssert.AreEqual(new[] { Options.DefaultSdlDatabaseUrl, Fork, Mirror }, options.SdlDatabaseUrls,
				"The saved address is not listed once after the person's own, or a listed address was lost.");
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("Options > Internet shows the saved SDL database address in an editable drop-down under the web service's, an address typed into it is saved, and the button beside it updates from that address; the SDL tabs then say where the copy came from")]
		public void The_internet_page_row_is_bound_and_its_button_updates()
		{
			var options = SettingsManager.Options;
			var originalUrl = options.SdlDatabaseUrl;
			var originalList = options.SdlDatabaseUrls == null ? null : options.SdlDatabaseUrls.ToArray();
			var appData = EngineHelper.AppDataPath;
			var updater = SdlUserControl.Updater;
			var folder = Path.Combine(Path.GetTempPath(), "x360ce-sdl-" + Guid.NewGuid().ToString("N"));
			var asked = new List<string>();
			var rows = string.Join("\n", Enumerable.Range(1, 120).Select(i => string.Format("03000000{0:x4}00000a01000000000000,Pad {0},a:b0,platform:Windows,", i)));
			try
			{
				ForgetClosedPages();
				options.SdlDatabaseUrl = Fork;
				options.InitDefaults();
				EngineHelper.AppDataPath = folder;
				SdlDatabase.Reload();
				SdlUserControl.Updater = new SdlUpdateClient(r =>
				{
					asked.Add(r.Url);
					return r.Url == Mirror
						? new UpdateResponse { StatusCode = 200, Body = Encoding.UTF8.GetBytes(rows), LastModified = new DateTime(2026, 12, 3, 8, 30, 0, DateTimeKind.Utc) }
						: new UpdateResponse { StatusCode = 404 };
				});
				Ui.OnUiThread(() =>
				{
					using (var form = new Form())
					using (var panel = new OptionsInternetUserControl())
					{
						form.Controls.Add(panel);
						// Bound and shown the way the main window does it: the map first, the handle after.
						panel.UpdateSettingsMap();
						form.Show();
						Application.DoEvents();
						var label = panel.Controls.Find(nameof(OptionsInternetUserControl.SdlDatabaseUrlLabel), true).FirstOrDefault() as Label;
						var box = panel.Controls.Find(nameof(OptionsInternetUserControl.SdlDatabaseUrlComboBox), true).FirstOrDefault() as ComboBox;
						var button = panel.Controls.Find(nameof(OptionsInternetUserControl.UpdateSdlDatabaseButton), true).FirstOrDefault() as Button;
						Assert.IsNotNull(label, "Options > Internet has no SDL database label.");
						Assert.IsNotNull(box, "Options > Internet has no SDL database address box.");
						Assert.IsNotNull(button, "Options > Internet has no [Update SDL Database] button.");
						// The web service's row, one row lower: caption, box, button.
						var table = (TableLayoutPanel)panel.InternetDatabaseUrlComboBox.Parent;
						Assert.AreSame(table, box.Parent, "The box is not in the web service address's table.");
						var web = table.GetPositionFromControl(panel.InternetDatabaseUrlComboBox);
						var test = table.GetPositionFromControl(panel.TestServiceButton);
						Assert.AreEqual(new TableLayoutPanelCellPosition(0, web.Row + 1), table.GetPositionFromControl(label));
						Assert.AreEqual(new TableLayoutPanelCellPosition(web.Column, web.Row + 1), table.GetPositionFromControl(box));
						Assert.AreEqual(new TableLayoutPanelCellPosition(test.Column, web.Row + 1), table.GetPositionFromControl(button));
						Assert.AreEqual("Update SDL Database", button.Text);
						Assert.IsTrue(label.Text.IndexOf("SDL", StringComparison.Ordinal) >= 0, "The caption does not name SDL: " + label.Text);
						Assert.IsTrue(button.TabIndex > panel.TestServiceButton.TabIndex && box.TabIndex < button.TabIndex && label.TabIndex < box.TabIndex,
							"The tab order does not follow the eye: web service row, then caption, box and button.");
						// Editable, listing the addresses, showing the saved one.
						Assert.AreEqual(ComboBoxStyle.DropDown, box.DropDownStyle, "The address cannot be typed.");
						CollectionAssert.AreEqual(options.SdlDatabaseUrls.ToArray(), box.Items.Cast<object>().Select(x => x.ToString()).ToArray());
						Assert.AreEqual(Fork, box.Text, "The box does not show the saved address.");
						Assert.AreEqual(Fork, options.SdlDatabaseUrl, "Showing the page changed the saved address.");
						// An address typed is the option.
						box.Text = Mirror;
						Application.DoEvents();
						Assert.AreEqual(Mirror, options.SdlDatabaseUrl, "The address typed was not saved.");
						Assert.IsTrue(button.Enabled);
						button.PerformClick();
						var clock = Stopwatch.StartNew();
						while (!button.Enabled && clock.ElapsedMilliseconds < 10000)
						{
							Application.DoEvents();
							System.Threading.Thread.Sleep(10);
						}
						Assert.IsTrue(button.Enabled, "The update did not finish.");
						CollectionAssert.AreEqual(new[] { Mirror }, asked, "The button did not fetch from the address in the box.");
						var version = SdlDatabase.Current.Version;
						Assert.AreEqual(Mirror, version.Address, "The database was not read again from the copy saved.");
						Assert.AreEqual(120, version.Mappings.Count);
						// Both SDL tabs say where the copy came from.
						using (var tab = new SdlUserControl())
						{
							tab.UpdateFrom(null, null);
							StringAssert.Contains(tab.VersionLabel.Text, "downloaded from " + Mirror + ", 2026-12-03");
						}
						using (var presets = new SdlPresetsUserControl())
						{
							presets.InitPanel(null);
							StringAssert.Contains(presets.VersionLabel.Text, "downloaded from " + Mirror + ", 2026-12-03");
						}
					}
				});
			}
			finally
			{
				ForgetClosedPages();
				SdlUserControl.Updater = updater;
				EngineHelper.AppDataPath = appData;
				SdlDatabase.Reload();
				options.SdlDatabaseUrl = originalUrl;
				if (originalList == null)
					options.SdlDatabaseUrls = null;
				else
				{
					options.SdlDatabaseUrls.Clear();
					foreach (var url in originalList)
						options.SdlDatabaseUrls.Add(url);
				}
				if (Directory.Exists(folder))
					Directory.Delete(folder, true);
			}
		}

		/// <summary>
		/// Forgets the boxes of pages already closed. An option is written only into the first box mapped to it, which the
		/// program makes once; a page another test made and closed would take the value meant for this test's page.
		/// </summary>
		static void ForgetClosedPages()
		{
			SettingsManager.Current.SettingsMap.RemoveAll(x => x.Control != null && x.Control.IsDisposed);
		}
	}
}
