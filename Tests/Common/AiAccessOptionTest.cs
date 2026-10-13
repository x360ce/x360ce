// @under-test: App.v4/Common/AiOptions.cs, Engine/JocysCom/Mcp/AiAccess.cs
// @area: settings   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using x360ce.App;
using x360ce.Engine;
using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.Mcp;
using JocysCom.ClassLibrary.Runtime;

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
			var o = new AiOptions();
			Assert.IsFalse(o.AiAccessEnabled, "The door must be shut until a person opens it.");
			Assert.AreEqual(AiAccess.Read, o.AiAccess, "Switching on lands on the level that changes nothing.");
			Assert.IsTrue(string.IsNullOrEmpty(o.AiAccessToken), "A token exists before anyone asked for access.");
			Assert.AreEqual(37360, o.AiAccessPort);
			Assert.AreEqual(AiOptions.LoopbackAddress, o.AiAccessAddress, "The door must stay on this computer until somebody opens it wider.");
			Assert.IsFalse(o.AiAccessTrustLocal, "Local connections must need the token until a person says otherwise.");
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("The access settings, Trust local connections included, are written to the options file and read back")]
		public void Access_settings_round_trip_through_the_options_file()
		{
			var o = new AiOptions
			{
				AiAccessEnabled = true,
				AiAccess = AiAccess.Configure,
				AiAccessTrustLocal = true,
				AiAccessPort = 37365,
			};
			o.EnsureAiAccessToken();
			var data = new XSettingsData<AiOptions> { Items = new SortableBindingList<AiOptions> { o } };
			// Written and read the way the options file is, without touching the person's own file.
			var loaded = data.DeserializeData(Serializer.SerializeToXmlBytes(data, Encoding.UTF8, true), false).Items[0];
			Assert.IsTrue(loaded.AiAccessTrustLocal, "Trust local connections was not kept.");
			Assert.IsTrue(loaded.AiAccessEnabled);
			Assert.AreEqual(AiAccess.Configure, loaded.AiAccess);
			Assert.AreEqual(37365, loaded.AiAccessPort);
			Assert.AreEqual(o.AiAccessToken, loaded.AiAccessToken);
			o.AiAccessTrustLocal = false;
			loaded = data.DeserializeData(Serializer.SerializeToXmlBytes(data, Encoding.UTF8, true), false).Items[0];
			Assert.IsFalse(loaded.AiAccessTrustLocal, "Trust local connections, once switched off, came back on.");
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("A token is made once and kept, and regenerating makes a different one")]
		public void Token_is_made_once_and_can_be_regenerated()
		{
			var o = new AiOptions();
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
			var o = new AiOptions();
			string changed = null;
			o.PropertyChanged += (s, e) => changed = e.PropertyName;
			o.AiAccessEnabled = true;
			Assert.AreEqual(nameof(AiOptions.AiAccessEnabled), changed);
			o.AiAccess = AiAccess.Configure;
			Assert.AreEqual(nameof(AiOptions.AiAccess), changed);
			o.AiAccessPort = 37361;
			Assert.AreEqual(nameof(AiOptions.AiAccessPort), changed);
			o.AiAccessAddress = AiOptions.AnyAddress;
			Assert.AreEqual(nameof(AiOptions.AiAccessAddress), changed);
			o.AiAccessTrustLocal = true;
			Assert.AreEqual(nameof(AiOptions.AiAccessTrustLocal), changed);
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("The tunnel's settings are written to the AI settings file and read back, the API key only encrypted, and each change is announced so the tunnel can follow it")]
		public void Tunnel_settings_round_trip_with_the_key_encrypted()
		{
			const string key = "sk-test-api-key";
			var o = new AiOptions();
			string changed = null;
			o.PropertyChanged += (s, e) => changed = e.PropertyName;
			o.TunnelEnabled = true;
			Assert.AreEqual(nameof(AiOptions.TunnelEnabled), changed);
			o.TunnelId = "tunnel_0123456789abcdef0123456789abcdef";
			Assert.AreEqual(nameof(AiOptions.TunnelId), changed);
			o.TunnelOrganization = "org-test";
			Assert.AreEqual(nameof(AiOptions.TunnelOrganization), changed);
			o.TunnelApiKey = McpTunnel.Protect(key);
			Assert.AreEqual(nameof(AiOptions.TunnelApiKey), changed);
			var data = new XSettingsData<AiOptions> { Items = new SortableBindingList<AiOptions> { o } };
			var xml = Serializer.SerializeToXmlBytes(data, Encoding.UTF8, true);
			Assert.IsFalse(Encoding.UTF8.GetString(xml).Contains(key), "The file holds the API key as typed.");
			var loaded = data.DeserializeData(xml, false).Items[0];
			Assert.IsTrue(loaded.TunnelEnabled);
			Assert.AreEqual(o.TunnelId, loaded.TunnelId);
			Assert.AreEqual("org-test", loaded.TunnelOrganization);
			Assert.AreEqual(key, McpTunnel.Unprotect(loaded.TunnelApiKey));
		}
	}
}
