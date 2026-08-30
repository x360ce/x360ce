// @under-test: App.v4/ViGEm/Client/ViGEmClient.x360ce.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace x360ce.Tests
{
	/// <summary>
	/// What the program takes away afterwards is what it actually made, not what it meant to make.
	/// </summary>
	/// <remarks>
	/// To put a controller in the third place, the two places below it are filled with placeholders
	/// first, and those are taken away again afterwards. Each was marked as made before the attempt
	/// to make it, so a placeholder that failed was still taken away - and taking away what was never
	/// there fails in its own right. A person who could not get one controller was told about a
	/// second fault, in the tidying up, which is the report that arrived from 4.19.17.0.
	///
	/// This is checked by reading the source rather than by running it, because making a controller
	/// needs the bus driver and a machine with it installed. It is a weaker test than driving the
	/// real thing, and it is the one that would have caught this.
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
		[Description("A placeholder is recorded once it exists, not when it is wished for")]
		public void A_placeholder_is_recorded_after_it_is_made()
		{
			var body = PlugInMethod();
			var connect = body.IndexOf("t[i].Connect();");
			var marked = body.IndexOf("tempDevices[i] = true;");
			Assert.IsTrue(connect >= 0 && marked >= 0,
				"PlugIn no longer connects a placeholder and records it, so this test is looking at "
				+ "the wrong thing and should be rewritten rather than deleted.");
			Assert.IsTrue(connect < marked,
				"A placeholder is marked as made before the attempt to make it. One that fails is "
				+ "then taken away anyway, and taking away what was never there fails - so a "
				+ "controller that could not be made reports a second fault about the tidying up.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Being told a placeholder is already gone is not reported as a fault")]
		public void An_already_absent_placeholder_is_not_a_fault()
		{
			// The purpose of that loop is that the placeholders are gone. Hearing that one already
			// is, is the purpose met. The bus can drop a controller between making it and tidying it
			// away, and nobody needs a report about a wish already granted.
			var body = PlugInMethod();
			StringAssert.Contains(body, "VIGEM_ERROR_TARGET_NOT_PLUGGED_IN",
				"Taking away a placeholder that is already gone is reported as a failure, though it "
				+ "is the outcome the loop exists to reach.");
		}
	}
}
