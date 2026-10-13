// @under-test: Engine/JocysCom/Mcp/McpTunnel.cs, Engine/JocysCom/Mcp/IAiTunnelSettings.cs
// @area: mcp   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JocysCom.ClassLibrary.Mcp;

namespace x360ce.Tests
{
	/// <summary>The tunnel that lets ChatGPT on the web reach the door: what it accepts, how it keeps the API key, and what it waits for before it starts anything.</summary>
	[TestClass]
	public class McpTunnelTest
	{
		/// <summary>Settings held in memory, so a test touches no settings file and starts no tunnel-client.</summary>
		sealed class Settings : IAiTunnelSettings
		{
			public bool Enabled { get; set; }
			public AiAccess Access { get; set; }
			public string Address { get; set; }
			public int Port { get; set; }
			public string Token { get; set; }
			public bool TrustLocal { get; set; }
			public bool TunnelEnabled { get; set; }
			public string TunnelId { get; set; }
			public string TunnelOrganization { get; set; }
			public string TunnelApiKey { get; set; }
			public string SettingsFile { get { return ""; } }
			public void Save() { }
			public void Apply() { }
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("Only a tunnel ID and an organization ID as OpenAI writes them are taken")]
		public void Tunnel_and_organization_ids_are_checked()
		{
			Assert.IsTrue(McpTunnel.IsTunnelId("tunnel_0123456789abcdef0123456789abcdef"));
			Assert.IsFalse(McpTunnel.IsTunnelId("tunnel_0123456789ABCDEF0123456789ABCDEF"), "OpenAI writes tunnel IDs in small letters.");
			Assert.IsFalse(McpTunnel.IsTunnelId("tunnel_0123"));
			Assert.IsFalse(McpTunnel.IsTunnelId(" tunnel_0123456789abcdef0123456789abcdef"));
			Assert.IsFalse(McpTunnel.IsTunnelId(null));
			Assert.IsTrue(McpTunnel.IsOrganization("org-mu1Qg1TTxi396SaZsMQyex4r"));
			Assert.IsFalse(McpTunnel.IsOrganization("org_123"), "An organization ID starts with org- and a dash.");
			Assert.IsFalse(McpTunnel.IsOrganization("org-12 3"));
			Assert.IsFalse(McpTunnel.IsOrganization(null));
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("The API key is kept encrypted for this Windows account, and what cannot be decrypted reads as no key")]
		public void API_key_is_kept_encrypted()
		{
			const string key = "sk-test-api-key";
			var kept = McpTunnel.Protect(key);
			Assert.IsFalse(kept.Contains(key), "The key was kept as typed.");
			Assert.AreNotEqual(kept, McpTunnel.Protect(key), "DPAPI salts each encryption, so the same key never reads the same twice.");
			Assert.AreEqual(key, McpTunnel.Unprotect(kept));
			Assert.AreEqual("", McpTunnel.Protect(""));
			Assert.AreEqual("", McpTunnel.Unprotect(""));
			Assert.AreEqual("", McpTunnel.Unprotect("not base64"), "Text that is no encrypted key must read as no key.");
			Assert.AreEqual("", McpTunnel.Unprotect("AAAA"), "A key encrypted for another account must read as no key.");
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("The tunnel starts nothing until it is ticked, the door is on, and a tunnel ID and an API key are kept, and it says which is missing")]
		public void Tunnel_says_what_it_waits_for()
		{
			var s = new Settings { Port = 37360, Token = "secret" };
			try
			{
				McpTunnel.Apply(s);
				Assert.AreEqual(McpTunnel.Off, McpTunnel.Status);
				s.TunnelEnabled = true;
				McpTunnel.Apply(s);
				StringAssert.Contains(McpTunnel.Status, "AI assistant access");
				s.Enabled = true;
				McpTunnel.Apply(s);
				StringAssert.Contains(McpTunnel.Status, "tunnel ID");
				s.TunnelId = "tunnel_0123456789abcdef0123456789abcdef";
				McpTunnel.Apply(s);
				StringAssert.Contains(McpTunnel.Status, "API key");
				s.TunnelApiKey = "AAAA";
				McpTunnel.Apply(s);
				StringAssert.Contains(McpTunnel.Status, "another Windows account");
				Assert.IsNull(McpTunnel.StatusPage, "A tunnel that waits has no status page.");
			}
			finally
			{
				McpTunnel.Stop();
			}
		}
	}
}
