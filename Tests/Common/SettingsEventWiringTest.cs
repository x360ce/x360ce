// @under-test: App.v4/Common/SettingsManager.Events.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Forms;
using x360ce.App;

namespace x360ce.Tests
{
	/// <summary>
	/// Whether a mapping box that changes shape is still listened to.
	/// </summary>
	/// <remarks>
	/// A mapping box is a list until it is switched to a formula, and a box that is typed into
	/// afterwards. The two announce a change through different events, and which one the program
	/// listens to is decided when it attaches, which happens once at startup while every box is
	/// still a list.
	///
	/// So a box switched to a formula reported nothing at all. What the person typed stayed on
	/// screen, looking applied, while the controller carried on doing whatever it did before, and
	/// there was no way to adjust a formula and feel the difference.
	/// </remarks>
	[TestClass]
	public class SettingsEventWiringTest
	{

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A box that is typed into reports every keystroke")]
		public void A_box_that_is_typed_into_reports_every_keystroke()
		{
			WithEventsRunning(manager =>
			{
				var box = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown };
				var reported = 0;
				System.EventHandler<SettingChangedEventArgs> watch = (s, e) => reported++;
				manager.SettingChanged += watch;
				try
				{
					manager.RewireControl(box);
					box.Text = "=a";
					box.Text = "=a5";
					box.Text = "=a5*4";
					Assert.AreEqual(3, reported,
						"A formula being typed has to reach the controller as it is written. " +
						"Reported " + reported + " of 3 changes.");
				}
				finally
				{
					manager.SettingChanged -= watch;
					box.Dispose();
				}
			});
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A box that becomes a list again stops reporting its text")]
		public void A_box_that_becomes_a_list_again_stops_reporting_its_text()
		{
			// Switching a formula off has to undo the change too, or a list would report a change
			// every time the program wrote a value into it while loading.
			WithEventsRunning(manager =>
			{
				var box = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown };
				var reported = 0;
				System.EventHandler<SettingChangedEventArgs> watch = (s, e) => reported++;
				manager.SettingChanged += watch;
				try
				{
					manager.RewireControl(box);
					box.DropDownStyle = ComboBoxStyle.DropDownList;
					manager.RewireControl(box);
					reported = 0;
					box.Text = "";
					Assert.AreEqual(0, reported, "A list should not report its text being written.");
				}
				finally
				{
					manager.SettingChanged -= watch;
					box.Dispose();
				}
			});
		}

		[TestMethod, TestCategory("mapping")]
		[Description("Asking twice does not make one keystroke count as two")]
		public void Asking_twice_does_not_double_the_report()
		{
			// The switch can be pressed repeatedly, and loading asks as well. Attaching a second
			// handler beside the first would save every keystroke twice.
			WithEventsRunning(manager =>
			{
				var box = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown };
				var reported = 0;
				System.EventHandler<SettingChangedEventArgs> watch = (s, e) => reported++;
				manager.SettingChanged += watch;
				try
				{
					manager.RewireControl(box);
					manager.RewireControl(box);
					manager.RewireControl(box);
					box.Text = "=b1";
					Assert.AreEqual(1, reported, "One keystroke was reported " + reported + " times.");
				}
				finally
				{
					manager.SettingChanged -= watch;
					box.Dispose();
				}
			});
		}

		/// <summary>
		/// Runs the body with the manager listening, and leaves it as it was found.
		/// </summary>
		/// <remarks>
		/// The manager is a single shared object and starts with its events suspended, which is how
		/// the program loads settings without every write counting as a change. A test which left it
		/// listening would change what the next test sees.
		/// </remarks>
		private static void WithEventsRunning(System.Action<SettingsManager> body)
		{
			var manager = SettingsManager.Current;
			var status = manager.NotifySettingsStatus;
			manager.NotifySettingsStatus = count => { };
			manager.ResumeEvents();
			try
			{
				body(manager);
			}
			finally
			{
				manager.SuspendEvents();
				manager.NotifySettingsStatus = status;
			}
		}

	}
}
