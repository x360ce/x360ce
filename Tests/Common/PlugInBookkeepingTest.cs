// @under-test: App.v4/ViGEm/Client/ViGEmClient.x360ce.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace x360ce.Tests
{
	/// <summary>
	/// Plugging in the controller for a pad connects that controller and nothing else.
	/// </summary>
	/// <remarks>
	/// To put a controller in the third place, the two places below it were filled with brief
	/// controllers first and taken away again afterwards. With a real controller in the first place
	/// the brief one took the second, the one wanted landed in the third, and the second was left
	/// empty - so a pad that belongs in XInput 2 could never get there. Some brief ones were left
	/// behind as well, holding a place until the program ended.
	///
	/// This is checked by reading the source rather than by running it, because making a controller
	/// needs the bus driver and a machine with it installed.
	/// </remarks>
	[TestClass]
	public class PlugInBookkeepingTest
	{
		static string PlugInMethod()
		{
			var path = Path.Combine(Ui.RepoRoot.FullName, "App.v4", "ViGEm", "Client", "ViGEmClient.x360ce.cs");
			var text = File.ReadAllText(path);
			var start = text.IndexOf("public bool PlugIn(");
			Assert.IsTrue(start >= 0, "PlugIn is no longer where this test looks for it.");
			var end = text.IndexOf("public void UnplugAllControllers", start);
			return end > start ? text.Substring(start, end - start) : text.Substring(start);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Plugging in a pad connects that pad only, never brief ones in the places below it")]
		public void Plugging_in_connects_only_the_pad_asked_for()
		{
			var body = PlugInMethod();
			StringAssert.Contains(body, "t[userIndex - 1].Connect();", "PlugIn no longer connects the pad asked for.");
			var connects = body.Split(new[] { ".Connect();" }, System.StringSplitOptions.None).Length - 1;
			Assert.AreEqual(1, connects,
				"PlugIn connects more than the pad asked for. A controller connected in a place below it takes "
				+ "another tab's place, and pushes the one asked for out of its own.");
		}
	}
}
