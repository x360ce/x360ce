// @under-test: App.v4/Global.cs, App.v4/Common/DInput/DInputHelper.cs
// @area: engine   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using x360ce.App;

namespace x360ce.Tests
{
	/// <summary>
	/// The polling rate chosen on the options page has to reach the device loop.
	///
	/// It did not. The only code that applied it sat in a handler nothing ever subscribed to,
	/// so the loop kept the rate it started with and the choice did nothing. Everything looked
	/// right from the outside: the box remembered the value and the settings file stored it.
	/// Four other settings were stranded in the same handler.
	///
	/// A property that is stored but never applied fails silently, which is why this is checked
	/// through the wiring rather than by calling the loop directly.
	/// </summary>
	[TestClass]
	public class PollingRateTest
	{
		[TestMethod, TestCategory("engine")]
		[Description("Choosing a polling rate changes the rate the device loop runs at")]
		public void Chosen_rate_reaches_the_device_loop()
		{
			var options = SettingsManager.Options;
			var original = options.PollingRate;
			try
			{
				// The real startup path, because the defect was that it never subscribed.
				Global.InitializeServices();
				Global.InitDHelperHelper();

				options.PollingRate = UpdateFrequency.ms2_500Hz;
				Assert.AreEqual(UpdateFrequency.ms2_500Hz, Global.DHelper.Frequency,
					"Choosing 500 Hz left the device loop at " + Global.DHelper.Frequency
					+ ". The rate is stored but never reaches the loop.");

				options.PollingRate = UpdateFrequency.ms8_125Hz;
				Assert.AreEqual(UpdateFrequency.ms8_125Hz, Global.DHelper.Frequency,
					"A second change did not reach the device loop.");
			}
			finally
			{
				options.PollingRate = original;
			}
		}

		[TestMethod, TestCategory("engine")]
		[Description("A saved polling rate is applied to a loop created afterwards")]
		public void Saved_rate_survives_a_restart()
		{
			// The loop does not exist when settings load, so a change raised then reaches
			// nothing. Without seeding at creation the choice is forgotten on every restart.
			var options = SettingsManager.Options;
			var original = options.PollingRate;
			try
			{
				options.PollingRate = UpdateFrequency.ms4_250Hz;
				Global.InitDHelperHelper();
				Assert.AreEqual(UpdateFrequency.ms4_250Hz, Global.DHelper.Frequency,
					"A loop created after the setting was loaded started at "
					+ Global.DHelper.Frequency + " instead of the saved rate.");
			}
			finally
			{
				options.PollingRate = original;
			}
		}
	}
}
