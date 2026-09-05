// @under-test: App.v4/Common/Options.cs, App.v4/Common/AiAccess.cs
// @area: settings   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using x360ce.App;

namespace x360ce.Tests
{
	/// <summary>The switch that lets an assistant into the program, and the token that proves it is the one invited.</summary>
	[TestClass]
	public class AiAccessOptionTest
	{
		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("Access is off until somebody switches it on")]
		public void Access_is_off_by_default()
		{
			var o = new Options();
			Assert.AreEqual(AiAccess.Off, o.AiAccess);
			Assert.IsTrue(string.IsNullOrEmpty(o.AiAccessToken), "A token exists before anyone asked for access.");
			Assert.AreEqual(37360, o.AiAccessPort);
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("A token is made once and kept, and regenerating makes a different one")]
		public void Token_is_made_once_and_can_be_regenerated()
		{
			var o = new Options();
			Assert.IsTrue(o.EnsureAiAccessToken(), "The first call makes the token.");
			var first = o.AiAccessToken;
			Assert.AreEqual(64, first.Length, "Token is 32 random bytes as hex.");
			Assert.IsFalse(o.EnsureAiAccessToken(), "Asking again must not change the token.");
			Assert.AreEqual(first, o.AiAccessToken);
			var second = o.RegenerateAiAccessToken();
			Assert.AreNotEqual(first, second);
			Assert.AreEqual(second, o.AiAccessToken);
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("Changing the level or the port tells listeners, so the server can restart")]
		public void Level_and_port_changes_are_announced()
		{
			var o = new Options();
			string changed = null;
			o.PropertyChanged += (s, e) => changed = e.PropertyName;
			o.AiAccess = AiAccess.Read;
			Assert.AreEqual(nameof(Options.AiAccess), changed);
			o.AiAccessPort = 37361;
			Assert.AreEqual(nameof(Options.AiAccessPort), changed);
		}
	}
}
