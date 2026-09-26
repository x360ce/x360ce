// @under-test: App.v4/Forms/LoadPresetsForm.cs
// @area: ui   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// "Load Selected Preset" loads the selected preset.
	/// </summary>
	/// <remarks>
	/// The lists fill from the server after the form is open. The button used to follow the tab:
	/// enabled when the tab's list had rows at the moment the tab was chosen. A tab opened while
	/// its list was still empty left the button off however much was selected afterwards. And a
	/// preset whose settings the server did not send closed the form with nothing, which looked
	/// the same as the button doing nothing.
	/// </remarks>
	[TestClass]
	public class LoadPresetOkButtonTest
	{
		static void OnFormWithOneSummary(Action<LoadPresetsForm, Summary> test)
		{
			if (SettingsManager.Summaries.Items.Count > 0)
				Assert.Inconclusive("This process already holds summaries; the test needs an empty list to start from.");
			var summary = new Summary { PadSettingChecksum = Guid.NewGuid(), ProductName = "Test pad", FileName = "test.exe" };
			Ui.OnUiThread(() =>
			{
				using (var form = new LoadPresetsForm())
				{
					try
					{
						form.InitForm();
						form.Show();
						Application.DoEvents();
						// The tab is chosen while its list is still empty, as it is while the server answers.
						form.MainTabControl.SelectedTab = form.SummariesTabPage;
						Application.DoEvents();
						Assert.IsFalse(form.OkButton.Enabled, "Nothing to load, yet the button is on.");
						Assert.IsFalse(form.CopyPresetButton.Enabled, "Nothing to copy, yet Copy Preset is on.");
						Assert.IsFalse(form.CopyPresetFormatButton.Enabled, "Nothing to copy, yet the format arrow is on.");
						// The answer arrives: one row, which the list selects by itself.
						SettingsManager.Summaries.Items.Add(summary);
						Application.DoEvents();
						Assert.AreEqual(1, form.SummariesGridPanel.SummariesDataGridView.SelectedRows.Count, "The arriving row was not selected.");
						Assert.IsTrue(form.CopyPresetButton.Enabled, "A row is selected, yet Copy Preset stays off.");
						Assert.IsTrue(form.CopyPresetFormatButton.Enabled, "A row is selected, yet the format arrow stays off.");
						test(form, summary);
					}
					finally
					{
						form.UnInitForm();
						SettingsManager.Summaries.Items.Remove(summary);
					}
				}
			});
		}

		[TestMethod, TestCategory("ui")]
		public void The_button_turns_on_when_a_row_arrives_and_loads_it()
		{
			OnFormWithOneSummary((form, summary) =>
			{
				Assert.IsTrue(form.OkButton.Enabled, "A row is selected, yet the button stays off.");
				var padSetting = new PadSetting { PadSettingChecksum = summary.PadSettingChecksum };
				SettingsManager.PadSettings.Add(padSetting);
				try
				{
					form.OkButton.PerformClick();
					Assert.AreEqual(DialogResult.OK, form.DialogResult);
					Assert.AreSame(padSetting, form.SelectedItem, "The button did not hand over the selected preset's settings.");
				}
				finally
				{
					SettingsManager.PadSettings.Remove(new[] { padSetting });
				}
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("clipboard")]
		[Description("One click on Copy Preset copies the selected preset as YAML")]
		public void One_click_on_Copy_Preset_copies_the_selected_preset_as_yaml()
		{
			OnFormWithOneSummary((form, summary) =>
			{
				var padSetting = new PadSetting { PadSettingChecksum = summary.PadSettingChecksum, ButtonA = "3" };
				SettingsManager.PadSettings.Add(padSetting);
				try
				{
					// A busy clipboard makes the button warn in a modal window nobody here can close.
					if (!JocysCom.ClassLibrary.Controls.ControlsHelper.CopyToClipboard(""))
						Assert.Inconclusive("The clipboard was busy on this machine.");
					form.CopyPresetButton.PerformClick();
					// Other programs read the clipboard the moment it changes, clipboard history among them,
					// and a read made while one of them holds it can come back empty although the copy was
					// made. The copy is made before the click returns, and nothing here lets a later one
					// run, so waiting for the text only waits for those readers to let go.
					var expected = SettingsManager.PadSettingToText(padSetting, SettingsManager.PresetFormat.Yaml);
					var copied = Clipboard.GetText();
					for (var waited = 0; copied != expected && waited < 2000; waited += 50)
					{
						System.Threading.Thread.Sleep(50);
						copied = Clipboard.GetText();
					}
					Assert.AreEqual(expected, copied, "Copy Preset did not copy the selected preset as YAML.");
				}
				finally
				{
					SettingsManager.PadSettings.Remove(new[] { padSetting });
				}
			});
		}

		[TestMethod, TestCategory("ui")]
		public void A_preset_whose_settings_never_arrived_keeps_the_form_open_and_says_so()
		{
			OnFormWithOneSummary((form, summary) =>
			{
				form.SelectedItem = null;
				// Away and back, so the button is on whichever way it is enabled.
				form.MainTabControl.SelectedTab = form.PresetsTabPage;
				form.MainTabControl.SelectedTab = form.SummariesTabPage;
				Application.DoEvents();
				Assert.IsTrue(form.OkButton.Enabled, "The button is off, so this test cannot press it.");
				form.OkButton.PerformClick();
				Assert.AreEqual(DialogResult.None, form.DialogResult, "The form closed with nothing to load.");
				Assert.IsNull(form.SelectedItem);
			});
		}
	}
}
