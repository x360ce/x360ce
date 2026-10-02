// @under-test: App.v4/Common/Options.cs, App.v4/Common/SettingsManager.LoadAndSync.cs, App.v4/Controls/OptionsInternetUserControl.cs
// @area: options   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;

namespace x360ce.Tests
{
	/// <summary>
	/// The minimum instances box on the Internet page shows the saved number, saves the number set in
	/// it, and follows the option when the option changes.
	/// </summary>
	/// <remarks>
	/// The page binds each option to its box by the kind of box. A number box the binding does not
	/// handle looks like a working one, because it takes a number, yet it shows the number it was
	/// designed with and never gives the option the number set in it.
	/// </remarks>
	[TestClass]
	public class MinimumInstancesOptionTest
	{
		[TestMethod, TestCategory("ui"), TestCategory("options")]
		[Description("The box shows the number saved in the options, not the one it was designed with")]
		public void The_box_shows_the_saved_number()
		{
			OnPage(7, panel => Assert.AreEqual(7m, panel.GetProgramsMinInstancesUpDown.Value, "The box does not show the saved number."));
		}

		[TestMethod, TestCategory("ui"), TestCategory("options")]
		[Description("A number set in the box is the number the program asks the online database with")]
		public void A_number_set_in_the_box_is_saved()
		{
			OnPage(2, panel =>
			{
				panel.GetProgramsMinInstancesUpDown.Value = 9;
				Assert.AreEqual(9, SettingsManager.Options.GetProgramsMinInstances, "The number set in the box did not reach the option.");
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("options")]
		[Description("A number the option is given while the page is open shows in the box")]
		public void A_number_saved_later_shows_in_the_box()
		{
			OnPage(2, panel =>
			{
				SettingsManager.Options.GetProgramsMinInstances = 4;
				Assert.AreEqual(4m, panel.GetProgramsMinInstancesUpDown.Value, "The box does not follow the option.");
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("options")]
		[Description("A saved number outside the box's range shows as the nearest number in it, and showing it leaves the saved number alone")]
		public void A_saved_number_the_box_cannot_hold_shows_as_the_nearest_it_can()
		{
			OnPage(500, panel =>
			{
				var box = panel.GetProgramsMinInstancesUpDown;
				Assert.AreEqual(box.Maximum, box.Value, "A number above the range does not show as the largest one.");
				Assert.AreEqual(500, SettingsManager.Options.GetProgramsMinInstances, "Showing the page changed the saved number.");
			});
			OnPage(-3, panel =>
			{
				var box = panel.GetProgramsMinInstancesUpDown;
				Assert.AreEqual(box.Minimum, box.Value, "A number below the range does not show as the smallest one.");
			});
		}

		/// <summary>Builds the Internet page the way the main window does, with the option already at <paramref name="saved"/>, and hands it over.</summary>
		/// <remarks>The option and the settings map are put back afterwards.</remarks>
		static void OnPage(int saved, Action<OptionsInternetUserControl> test)
		{
			var options = SettingsManager.Options;
			var original = options.GetProgramsMinInstances;
			// A page adds its boxes to the settings map and nothing takes them out, so the boxes of a
			// page built earlier in this process would hear the option instead of this one.
			var settingsMap = SettingsManager.Current.SettingsMap;
			var earlier = settingsMap.Where(IsOptionBox).ToArray();
			settingsMap.RemoveAll(IsOptionBox);
			try
			{
				options.GetProgramsMinInstances = saved;
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
						test(panel);
					}
				});
			}
			finally
			{
				// Nothing is left bound, so putting the number back changes only the option.
				settingsMap.RemoveAll(IsOptionBox);
				options.GetProgramsMinInstances = original;
				settingsMap.AddRange(earlier);
			}
		}

		static bool IsOptionBox(SettingsMapItem item)
		{
			return item.Property != null && item.Property.DeclaringType == typeof(Options);
		}
	}
}
