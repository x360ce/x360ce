// @under-test: App.v4/Common/SettingsManager.XML.cs, App.v4/Common/DInput/DeviceRouting.cs, App.v4/MainForm.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.App.DInput;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>A change on one controller page is saved into that page's row alone, and the engine's routing is built once for it.</summary>
	/// <remarks>
	/// Every change saved all four pages, a change on the Options page among them, and every list notice of the save built
	/// the routing again: up to four builds for one box, on every step of a slider's drag. A page left alone was saved as
	/// well, which wrote what it showed over a row changed elsewhere, as a download from the settings database changes one.
	/// The pages here are saved as the main window saves them, through <see cref="SettingsManager.ApplySettingsToXML"/>.
	/// </remarks>
	[TestClass]
	public class PageApplyTest
	{
		/// <summary>Four controller pages, each showing its own row of one game, and the second row changed elsewhere since its page showed it.</summary>
		class Fixture
		{
			public PadControl[] Pages;
			public UserSetting[] Rows;
			/// <summary>Each row's settings checksum before the change.</summary>
			public Guid[] Checksums;
			/// <summary>The stored settings each row pointed at before the change.</summary>
			public PadSetting[] Settings;
			/// <summary>The routing the engine was given before the change, then the one it read at each list notice.</summary>
			public readonly List<DeviceRouting> Routings = new List<DeviceRouting>();

			/// <summary>How many routings were built since the fixture was made, counted after the change.</summary>
			public int Builds()
			{
				return Routings.Concat(new[] { DeviceRouting.Current }).Distinct().Count() - 1;
			}

			/// <summary>Fails when a row other than the first points at other settings, or at another object holding them.</summary>
			public void AssertOtherRowsUntouched(string what)
			{
				for (var i = 1; i < Rows.Length; i++)
				{
					Assert.AreEqual(Checksums[i], Rows[i].PadSettingChecksum,
						what + " changed the row of Controller " + (i + 1) + (i == 1 ? ", writing what its page showed over the row changed elsewhere." : "."));
					Assert.AreSame(Settings[i], SettingsManager.GetPadSetting(Rows[i].PadSettingChecksum),
						what + " replaced the settings the row of Controller " + (i + 1) + " points at.");
				}
			}
		}

		/// <summary>A dead zone the slider can show exactly: the value a slider at this per cent stores.</summary>
		static string DeadZone(int percent)
		{
			return Convert.ToInt32(percent / 100F * short.MaxValue).ToString();
		}

		static PadSetting NewSettings(int percent)
		{
			var ps = new PadSetting { LeftThumbDeadZoneX = DeadZone(percent), ForceOverall = "80" };
			ps.PadSettingChecksum = ps.CleanAndGetCheckSum();
			return ps;
		}

		static bool IsPageBox(SettingsMapItem item)
		{
			return item.MapTo >= MapTo.Controller1 && item.MapTo <= MapTo.Controller4;
		}

		/// <summary>The slider of the left stick's X dead zone on a page.</summary>
		static TrackBar DeadZoneSlider(PadControl page)
		{
			var axis = page.Controls.Find("LeftThumbXUserControl", true).Single();
			return (TrackBar)axis.Controls.Find("DeadZoneTrackBar", true).Single();
		}

		/// <summary>Builds the four pages in a window, with the settings' events running, and runs the body on them.</summary>
		static void OnFourPages(Action<Fixture> body)
		{
			Ui.OnUiThread(() =>
			{
				var existing = SettingsManager.UserSettings.ItemsToArraySynchronized();
				var existingPads = SettingsManager.PadSettings.ItemsToArraySynchronized();
				var oldGame = SettingsManager.CurrentGame;
				var oldStatus = SettingsManager.Current.NotifySettingsStatus;
				SettingsManager.Current.NotifySettingsStatus = count => { };
				// A page adds its boxes to the settings map and nothing takes them out, so the boxes of pages built earlier
				// in this process would be read and loaded instead of these.
				var settingsMap = SettingsManager.Current.SettingsMap;
				var earlier = settingsMap.Where(IsPageBox).ToArray();
				settingsMap.RemoveAll(IsPageBox);
				// The routing follows the lists from the moment the settings are loaded, as in the program.
				DeviceRouting.Watch();
				var fixture = new Fixture();
				EventHandler<SettingChangedEventArgs> save = (s, e) => SettingsManager.Current.ApplySettingsToXML(fixture.Pages, e.Page);
				System.ComponentModel.ListChangedEventHandler noted = (s, e) => fixture.Routings.Add(DeviceRouting.Current);
				var resumed = false;
				var game = new UserGame
				{
					FileName = "four-pages.exe",
					FileProductName = "Four pages",
					EnableMask = 0,
					EmulationType = (int)EmulationType.None,
				};
				SettingsManager.UserSettings.Items.Clear();
				try
				{
					fixture.Rows = new UserSetting[4];
					for (var i = 0; i < fixture.Rows.Length; i++)
					{
						var ps = NewSettings(10 * (i + 1));
						SettingsManager.PadSettings.Items.Add(ps);
						fixture.Rows[i] = new UserSetting
						{
							SettingId = Guid.NewGuid(),
							MapTo = i + 1,
							FileName = game.FileName,
							FileProductName = game.FileProductName,
							InstanceGuid = Guid.NewGuid(),
							IsEnabled = true,
							PadSettingChecksum = ps.PadSettingChecksum,
							DateUpdated = DateTime.Now.AddDays(-1),
						};
						SettingsManager.UserSettings.Items.Add(fixture.Rows[i]);
					}
					SettingsManager.CurrentGame = game;
					using (var form = new Form { Width = 900, Height = 700 })
					using (var tabs = new TabControl { Dock = DockStyle.Fill })
					{
						form.Controls.Add(tabs);
						fixture.Pages = new PadControl[4];
						for (var i = 0; i < fixture.Pages.Length; i++)
						{
							var tab = new TabPage("Controller " + (i + 1));
							tabs.TabPages.Add(tab);
							var page = new PadControl((MapTo)(i + 1)) { Dock = DockStyle.Fill };
							tab.Controls.Add(page);
							// The same three steps the main window takes for each page.
							page.InitPadControl();
							page.UpdateSettingsMap();
							page.InitPadData();
							fixture.Pages[i] = page;
						}
						form.Show();
						Application.DoEvents();
						for (var i = 0; i < fixture.Pages.Length; i++)
						{
							Assert.AreSame(fixture.Rows[i], fixture.Pages[i].GetSelectedSetting(),
								"Controller " + (i + 1) + " does not select its row, so this proves nothing.");
							Assert.AreEqual(fixture.Rows[i].PadSettingChecksum, fixture.Pages[i].CloneCurrentPadSetting().PadSettingChecksum,
								"Controller " + (i + 1) + " does not show its row's settings, so this proves nothing.");
						}
						// Changed elsewhere, as a download from the settings database changes a row in place, while its page
						// goes on showing the settings it was loaded with.
						var downloaded = NewSettings(70);
						SettingsManager.Current.UpsertPadSettings(downloaded);
						var newer = new UserSetting();
						JocysCom.ClassLibrary.Runtime.RuntimeHelper.CopyDataMembers(fixture.Rows[1], newer);
						newer.PadSettingChecksum = downloaded.PadSettingChecksum;
						newer.DateUpdated = DateTime.Now;
						SettingsManager.Current.UpsertSettings(newer);
						Assert.AreEqual(downloaded.PadSettingChecksum, fixture.Rows[1].PadSettingChecksum,
							"The row of Controller 2 was not changed elsewhere, so this proves nothing.");
						fixture.Checksums = fixture.Rows.Select(x => x.PadSettingChecksum).ToArray();
						fixture.Settings = fixture.Rows.Select(x => SettingsManager.GetPadSetting(x.PadSettingChecksum)).ToArray();
						SettingsManager.Current.SettingChanged += save;
						SettingsManager.UserSettings.Items.ListChanged += noted;
						SettingsManager.PadSettings.Items.ListChanged += noted;
						// The settings' events start suspended and the main window resumes them once its pages are loaded.
						SettingsManager.Current.ResumeEvents();
						resumed = true;
						fixture.Routings.Add(DeviceRouting.Current);
						body(fixture);
					}
				}
				finally
				{
					if (resumed)
						SettingsManager.Current.SuspendEvents();
					SettingsManager.Current.SettingChanged -= save;
					SettingsManager.UserSettings.Items.ListChanged -= noted;
					SettingsManager.PadSettings.Items.ListChanged -= noted;
					SettingsManager.Current.NotifySettingsStatus = oldStatus;
					SettingsManager.CurrentGame = oldGame;
					SettingsManager.UserSettings.Items.Clear();
					foreach (var item in existing)
						SettingsManager.UserSettings.Items.Add(item);
					SettingsManager.PadSettings.Items.Clear();
					foreach (var item in existingPads)
						SettingsManager.PadSettings.Items.Add(item);
					settingsMap.RemoveAll(IsPageBox);
					settingsMap.AddRange(earlier);
				}
			}, "Saving the controller pages");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A box changed on one controller page is saved into that page's row, and the other three rows keep their settings")]
		public void A_box_change_saves_its_own_page_alone()
		{
			OnFourPages(fixture =>
			{
				DeadZoneSlider(fixture.Pages[0]).Value = 15;
				var saved = SettingsManager.GetPadSetting(fixture.Rows[0].PadSettingChecksum);
				Assert.IsNotNull(saved, "The row of Controller 1 points at settings that are not stored.");
				Assert.AreEqual(DeadZone(15), saved.LeftThumbDeadZoneX, "The change was not saved into the row of its page.");
				fixture.AssertOtherRowsUntouched("A change on Controller 1");
			});
		}

		[TestMethod, TestCategory("mapping"), TestCategory("engine"), TestCategory("critical")]
		[Description("One box change builds the engine's routing once")]
		public void A_box_change_builds_the_routing_once()
		{
			OnFourPages(fixture =>
			{
				DeadZoneSlider(fixture.Pages[0]).Value = 15;
				Assert.AreEqual(1, fixture.Builds(), "One box change built the engine's routing " + fixture.Builds() + " times.");
			});
		}

		[TestMethod, TestCategory("mapping"), TestCategory("options")]
		[Description("A box on the Options page saves no controller page and builds no routing")]
		public void An_options_box_saves_no_controller_page()
		{
			OnFourPages(fixture =>
			{
				using (var box = new CheckBox())
				{
					// An Options box is in the settings map as the Options page adds it, and reports its changes the same way.
					SettingsManager.AddMap<Options>(x => x.ShowTestButton, box);
					var map = SettingsManager.Current.SettingsMap.Last();
					try
					{
						SettingsManager.Current.RewireControl(box);
						box.Checked = !box.Checked;
						Assert.AreEqual(0, fixture.Builds(), "An Options box built the engine's routing " + fixture.Builds() + " times.");
						Assert.AreEqual(fixture.Checksums[0], fixture.Rows[0].PadSettingChecksum, "An Options box changed the row of Controller 1.");
						fixture.AssertOtherRowsUntouched("An Options box");
					}
					finally
					{
						SettingsManager.Current.SettingsMap.Remove(map);
					}
				}
			});
		}

		[TestMethod, TestCategory("mapping"), TestCategory("performance")]
		[Description("A slider dragged through ten steps saves ten times, its own page each time, and builds the routing once a step")]
		public void A_slider_drag_saves_its_own_page_once_a_step()
		{
			OnFourPages(fixture =>
			{
				var slider = DeadZoneSlider(fixture.Pages[0]);
				const int steps = 10;
				var watch = new Stopwatch();
				for (var step = 1; step <= steps; step++)
				{
					watch.Start();
					slider.Value = 10 + step;
					watch.Stop();
					var saved = SettingsManager.GetPadSetting(fixture.Rows[0].PadSettingChecksum);
					Assert.AreEqual(DeadZone(10 + step), saved == null ? null : saved.LeftThumbDeadZoneX, "Step " + step + " of the drag was not saved.");
				}
				Console.WriteLine("A step of the drag took " + (watch.Elapsed.TotalMilliseconds / steps).ToString("0.00") + " ms on the interface thread.");
				Assert.AreEqual(steps, fixture.Builds(), steps + " steps of a drag built the engine's routing " + fixture.Builds() + " times.");
				fixture.AssertOtherRowsUntouched("A drag on Controller 1");
			});
		}
	}
}
