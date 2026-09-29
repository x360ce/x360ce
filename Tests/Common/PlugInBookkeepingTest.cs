// @under-test: App.v4/ViGEm/Client/ViGEmClient.x360ce.cs, App.v4/ViGEm/Client/ViGEmTarget.cs, App.v4/ViGEm/Client/Targets/Xbox360Controller.cs, App.v4/ViGEm/Client/Enums/BusAnswer.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;

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

		/// <summary>Checks that a Connect method, read from its source, refuses every answer from the bus but a plain yes.</summary>
		static void AssertConnectRefusesAllButNone(string file, string connect, string disconnect)
		{
			var path = Path.Combine(Ui.RepoRoot.FullName, "App.v4", "ViGEm", "Client", file);
			var text = File.ReadAllText(path);
			var start = text.IndexOf(connect);
			Assert.IsTrue(start >= 0, "Connect is no longer where this test looks for it in " + file + ".");
			var end = text.IndexOf(disconnect, start);
			Assert.IsTrue(end > start, "Disconnect no longer follows Connect in " + file + ".");
			var body = text.Substring(start, end - start);
			// BusAnswers.Of reads every answer but NONE as a refusal; BusRefusalTest holds it to that for every code.
			StringAssert.Contains(body, "BusAnswers.Of(error) != BusAnswer.Fine",
				"Connect in " + file + " no longer treats every answer but NONE as a refusal.");
			Assert.IsFalse(body.Contains("case VIGEM_ERROR."),
				"Connect in " + file + " lists the refusals it knows. Every answer the list does not name - a bus " +
				"that could not be reached among them - is then taken as a controller that was made.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Connecting a controller fails on every answer from the bus but a plain yes")]
		public void Connecting_fails_on_every_answer_but_none()
		{
			AssertConnectRefusesAllButNone("ViGEmTarget.cs", "public virtual void Connect()", "public virtual void Disconnect()");
		}

		/// <summary>The code of one method, read from its source, without its comments.</summary>
		static string CodeBetween(string text, string start, string end)
		{
			var lines = Ui.Between(text, start, end).Split('\n')
				.Where(x => !x.Trim().StartsWith("//"));
			return string.Join("\n", lines);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Only a refused add fails Connect; a refused rumble registration is kept, and the controller stays")]
		public void A_refused_rumble_registration_keeps_the_controller()
		{
			var path = Path.Combine(Ui.RepoRoot.FullName, "App.v4", "ViGEm", "Client", "Targets", "Xbox360Controller.cs");
			var text = File.ReadAllText(path);
			var connect = CodeBetween(text, "public override void Connect()", "public override void Disconnect()");
			var disconnect = CodeBetween(text, "public override void Disconnect()", "public event ");
			// base.Connect throws on a refused add, and then nothing is on the bus; Connecting_fails_on_every_answer_but_none holds it to that.
			var add = connect.IndexOf("base.Connect();");
			var register = connect.IndexOf("vigem_target_x360_register_notification(");
			Assert.IsTrue(add >= 0 && register > add, "The rumble registration no longer follows the add.");
			Assert.IsFalse(connect.Contains("throw"),
				"Connect throws after the add, so a controller on the bus reads as a failed plug: never remembered as " +
				"ours, plugged again on top of itself, and holding its place.");
			StringAssert.Contains(connect, "RumbleError = ViGEmClient.NativeMethods.vigem_target_x360_register_notification(",
				"The registration's answer is not kept, so nothing can say the controller has no vibration.");
			// Letting go unregisters whatever the registration answered, and nothing there stops the removal after it.
			var unregister = disconnect.IndexOf("vigem_target_x360_unregister_notification(NativeHandle);");
			var remove = disconnect.IndexOf("base.Disconnect();");
			Assert.IsTrue(unregister >= 0 && remove > unregister, "Disconnect no longer unregisters rumble before removing the controller.");
			Assert.IsFalse(disconnect.Contains("if (") || disconnect.Contains("throw"),
				"Disconnect unregisters only sometimes, or can stop before the removal.");
		}
	}
}
