// @under-test: Engine/JocysCom/Mcp/McpTunnel.cs, Engine/JocysCom/Mcp/IAiTunnelSettings.cs, Engine/JocysCom/Mcp/AiAccessModel.Tunnel.cs, Engine/JocysCom/Mcp/AiAccessUserControl.cs
// @area: mcp   @layer: unit
using Microsoft.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JocysCom.ClassLibrary.Mcp;
using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Windows.Forms;

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

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("The organization's tunnels are listed with the admin key, newest first, whatever OpenAI wraps them in, and never without one")]
		public void Tunnels_are_listed_newest_first()
		{
			var folder = StandIn();
			var kept = McpTunnel.Folder;
			McpTunnel.Folder = folder;
			try
			{
				var older = Id('a');
				var newer = Id('b');
				var other = Id('c');
				Answer(folder, "list", "{\"data\":[" + Tunnel(older, "x360ce", "x360ce on EJN2", "1791600000") + ","
					+ Tunnel(newer, "x360ce", "x360ce on EJN2", "\"2026-10-17T13:00:00Z\"") + ","
					+ Tunnel(other, "other", "another program", "1791000000") + "],\"has_more\":false}");
				var tunnels = McpTunnel.List("admin-key", "org-test");
				CollectionAssert.AreEqual(new[] { newer, older, other }, tunnels.Select(x => x.Id).ToArray(), "Not every tunnel was read, or not newest first.");
				Assert.AreEqual("x360ce on EJN2", tunnels[0].Description);
				Assert.AreEqual("x360ce", tunnels[0].Name, "A name inside a tunnel's own field was read instead of the tunnel's.");
				Assert.AreEqual(new DateTime(2026, 10, 17, 13, 0, 0, DateTimeKind.Utc), tunnels[0].Created);
				StringAssert.StartsWith(tunnels[0].ToString(), newer + "  x360ce on EJN2, made ");
				Answer(folder, "list", "{\"tunnels\":[" + Tunnel(older, "x360ce", "x360ce on EJN2", "1791600000") + "]}");
				Assert.AreEqual(older, McpTunnel.List("admin-key", "org-test").Single().Id, "A list wrapped differently was not read.");
				Assert.AreEqual("admin tunnels list --organization-id org-test --json | admin key", Calls(folder)[0]);
				var refused = Assert.ThrowsExactly<InvalidOperationException>(() => McpTunnel.List("", "org-test"));
				StringAssert.Contains(refused.Message, "admin key");
				Assert.AreEqual(2, Calls(folder).Length, "tunnel-client was started without an admin key, which OpenAI refuses.");
			}
			finally
			{
				McpTunnel.Folder = kept;
				Directory.Delete(folder, true);
			}
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("Create keeps the tunnel made earlier for this computer, so a ChatGPT plugin made with it keeps working, and makes one only when there is none")]
		public void Create_uses_the_tunnel_made_for_this_computer()
		{
			var folder = StandIn();
			var kept = McpTunnel.Folder;
			McpTunnel.Folder = folder;
			try
			{
				var mine = Id('a');
				var other = Id('b');
				var made = Id('d');
				var here = McpTunnel.Name + " on " + Environment.MachineName;
				Answer(folder, "list", "{\"data\":[" + Tunnel(other, "other", "another program", "1791700000") + ","
					+ Tunnel(mine, McpTunnel.Name, here, "1791600000") + "]}");
				Answer(folder, "create", "{\"id\":\"" + made + "\"}");
				var s = new Settings { TunnelOrganization = "org-test" };
				var model = new AiAccessModel(x => { });
				model.Bind(s);
				model.CreateTunnel("admin-key");
				Wait(model);
				Assert.AreEqual(mine, s.TunnelId, "Create did not keep the tunnel made earlier for this computer.");
				StringAssert.Contains(model.TunnelStatus, "made earlier for this computer");
				Assert.IsFalse(Calls(folder).Any(x => x.Contains("tunnels create")), "Create made a second tunnel beside the one this computer has.");
				Assert.AreEqual(2, model.TunnelChoices.Length, "Create did not show the tunnels it found.");
				Answer(folder, "list", "{\"data\":[" + Tunnel(other, "other", "another program", "1791700000") + "]}");
				model.CreateTunnel("admin-key");
				Wait(model);
				Assert.AreEqual(made, s.TunnelId, "With no tunnel for this computer, Create made none.");
				StringAssert.StartsWith(Calls(folder).Last(), "admin tunnels create --name " + McpTunnel.Name + " --description " + here);
			}
			finally
			{
				McpTunnel.Folder = kept;
				Directory.Delete(folder, true);
			}
		}

		[TestMethod, TestCategory("mcp"), TestCategory("ui"), TestCategory("critical")]
		[Description("Opening the Tunnel ID list asks for the organization's tunnels with the admin key, and a tunnel picked from it keeps its ID")]
		public void Tunnel_ID_lists_the_organizations_tunnels()
		{
			var folder = StandIn();
			var kept = McpTunnel.Folder;
			McpTunnel.Folder = folder;
			try
			{
				var older = Id('a');
				var newer = Id('b');
				Answer(folder, "list", "{\"data\":[" + Tunnel(older, "x360ce", "x360ce on EJN2", "1791600000") + ","
					+ Tunnel(newer, "x360ce", "x360ce on EJN2", "1791700000") + "]}");
				var s = new Settings { TunnelOrganization = "org-test", TunnelId = newer };
				Ui.OnUiThread(() =>
				{
					using (var form = new Form { ShowInTaskbar = false })
					using (var ai = new AiAccessUserControl { Dock = DockStyle.Fill })
					{
						form.Controls.Add(ai);
						ai.Bind(s);
						form.Show();
						var box = (ComboBox)ai.Controls.Find("AiTunnelIdComboBox", true).Single();
						Assert.AreEqual(ComboBoxStyle.DropDown, box.DropDownStyle, "A tunnel ID can no longer be typed or pasted.");
						box.DroppedDown = true;
						StringAssert.Contains(ai.Model.TunnelStatus, "admin key", "Without the admin key the list did not say what it needs.");
						Assert.AreEqual(0, Calls(folder).Length, "tunnel-client was started without an admin key.");
						box.DroppedDown = false;
						((TextBox)ai.Controls.Find("AiTunnelAdminKeyTextBox", true).Single()).Text = "admin-key";
						box.DroppedDown = true;
						Ui.WaitFor(() =>
						{
							Application.DoEvents();
							return ai.Model.TunnelBusy ? null : "listed";
						}, TimeSpan.FromSeconds(30), "the stand-in's list of tunnels");
						Application.DoEvents();
						box.DroppedDown = false;
						CollectionAssert.AreEqual(ai.Model.TunnelChoices.Select(x => x.ToString()).ToArray(), box.Items.Cast<string>().ToArray(),
							"The list does not show the organization's tunnels.");
						Assert.AreEqual(2, box.Items.Count);
						ai.Model.PickTunnel(1);
						Assert.AreEqual(older, s.TunnelId, "The picked tunnel's ID was not kept.");
						ai.Model.SetTunnelId(box.Items[0].ToString());
						Assert.AreEqual(newer, s.TunnelId, "A line of the list, as the box shows it once picked, did not keep its ID.");
					}
				});
			}
			finally
			{
				McpTunnel.Folder = kept;
				Directory.Delete(folder, true);
			}
		}

		static string Id(char digit) { return "tunnel_" + new string(digit, 32); }

		/// <summary>A tunnel as OpenAI lists it, with an object of its own inside that names no tunnel.</summary>
		static string Tunnel(string id, string name, string description, string created)
		{
			return "{\"id\":\"" + id + "\",\"object\":\"tunnel\",\"name\":\"" + name + "\",\"description\":\"" + description
				+ "\",\"created_at\":" + created + ",\"organization_ids\":[\"org-test\"],\"metadata\":{\"id\":\"meta\",\"name\":\"inner\"}}";
		}

		/// <summary>What the stand-in answers to an admin tunnels command, such as list or create.</summary>
		static void Answer(string folder, string command, string json) { File.WriteAllText(Path.Combine(folder, command + ".json"), json); }

		/// <summary>The commands the stand-in was given, each with whether an admin key came with it.</summary>
		static string[] Calls(string folder)
		{
			var file = Path.Combine(folder, "calls.txt");
			return File.Exists(file) ? File.ReadAllLines(file) : new string[0];
		}

		static void Wait(AiAccessModel model)
		{
			Ui.WaitFor(() => model.TunnelBusy ? null : "answered", TimeSpan.FromSeconds(30), "the stand-in's answer");
		}

		/// <summary>
		/// A stand-in for tunnel-client, in a folder of its own: it writes each command it is given, and whether an admin key
		/// came with it, to calls.txt, and answers an admin tunnels command with the JSON file named after it. Nothing reaches OpenAI.
		/// </summary>
		static string StandIn()
		{
			var folder = Path.Combine(Path.GetTempPath(), "x360ce-tunnel-" + Guid.NewGuid().ToString("N"));
			var exe = Path.Combine(folder, "v1.0.0", "tunnel-client.exe");
			Directory.CreateDirectory(Path.GetDirectoryName(exe));
			const string source = @"using System; using System.IO;
class StandIn
{
	static int Main(string[] args)
	{
		var folder = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\'));
		var key = string.IsNullOrEmpty(Environment.GetEnvironmentVariable(""OPENAI_ADMIN_KEY"")) ? ""no admin key"" : ""admin key"";
		File.AppendAllText(Path.Combine(folder, ""calls.txt""), string.Join("" "", args) + "" | "" + key + Environment.NewLine);
		var answer = Path.Combine(folder, (args.Length > 2 ? args[2] : """") + "".json"");
		if (!File.Exists(answer))
		{
			Console.Write(""{\""message\"":\""The stand-in has no answer.\""}"");
			return 1;
		}
		Console.Write(File.ReadAllText(answer));
		return 0;
	}
}";
			using (var csharp = new CSharpCodeProvider())
			{
				var built = csharp.CompileAssemblyFromSource(new CompilerParameters { GenerateExecutable = true, OutputAssembly = exe }, source);
				Assert.IsFalse(built.Errors.HasErrors, string.Join(Environment.NewLine, built.Errors.Cast<CompilerError>()));
			}
			return folder;
		}
	}
}
