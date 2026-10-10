// @under-test: App.v4/Controls/OptionsUserControl.cs, App.v4/Mcp/AiAccessSettings.cs, App.v4/Mcp/McpTools.cs, App.v4/Global.cs, Engine/JocysCom/Mcp/AiAccessUserControl.cs, Engine/JocysCom/Mcp/AiAccessModel.cs, Engine/JocysCom/Mcp/AiAccessModel.Catalog.cs, App.v4/MainForm.cs, README.MD
// @area: ui   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.App.Mcp;
using x360ce.Engine;
using JocysCom.ClassLibrary.Controls.UiTree;
using JocysCom.ClassLibrary.Mcp;

namespace x360ce.Tests
{
	/// <summary>Where a person switches an assistant on, what they are shown to register it, and where an AI reader is told to look.</summary>
	[TestClass]
	public class AiAccessOptionsPageTest
	{
		/// <summary>Settings that count what the page asks of them, and touch neither the options file nor the door.</summary>
		sealed class CountingSettings : IAiAccessSettings
		{
			public bool Enabled { get; set; }
			public AiAccess Access { get; set; } = AiAccess.Read;
			public string Address { get; set; } = McpListener.LoopbackAddress;
			public int Port { get; set; } = 40000;
			public string Token { get; set; } = McpListener.NewToken();
			public bool TrustLocal { get; set; }
			public int Saves;
			public int Applies;
			public void Save() { Saves++; }
			public void Apply() { Applies++; }
		}

		/// <summary>The one control of that name and kind at any depth below the root.</summary>
		static T Find<T>(Control root, string name) where T : Control
		{
			var found = root.Controls.Find(name, true);
			Assert.AreEqual(1, found.Length, name + " is not on the page exactly once.");
			Assert.IsInstanceOfType(found[0], typeof(T), name + " is not a " + typeof(T).Name + ".");
			return (T)found[0];
		}

		/// <summary>What Windows Forms raises when the person leaves a box they edited.</summary>
		static void Validated(Control control)
		{
			typeof(Control).GetMethod("OnValidated", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(control, new object[] { EventArgs.Empty });
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("The AI tab holds only the shared page, which shows the options: the level list, the port, the token, the URL the door answers and a snippet naming this program and the /Mcp switch")]
		public void Options_page_offers_the_controls_and_a_snippet()
		{
			Ui.OnUiThread(() =>
			{
				using (var page = new OptionsUserControl())
				{
					page.CreateControl();
					var ai = Find<AiAccessUserControl>(page, "AiControl");
					Assert.AreEqual("AiTabPage", ai.Parent.Name, "The shared page is not on the AI tab.");
					Assert.AreEqual(DockStyle.Fill, ai.Dock);
					// The AI tab holds only the shared page: the Options page has no AI control of its own.
					var left =typeof(OptionsUserControl).GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
						.Select(x => x.Name).Where(x => x.StartsWith("AiAccess") || x.StartsWith("AiSkill")).ToArray();
					Assert.AreEqual(0, left.Length, "The Options page still has AI controls of its own: " + string.Join(", ", left));
					ai.Bind(new AiAccessSettings());
					var o = SettingsManager.Options;
					// Typed text would leave the list's selection empty.
					var level = Find<ComboBox>(ai, "AiAccessComboBox");
					Assert.AreEqual(ComboBoxStyle.DropDownList, level.DropDownStyle);
					CollectionAssert.AreEqual(AiAccessModel.Levels, level.Items.Cast<string>().ToArray());
					Assert.AreEqual(o.AiAccess.ToString(), level.SelectedItem);
					var address = Find<ComboBox>(ai, "AiAccessAddressComboBox");
					Assert.AreEqual(ComboBoxStyle.DropDownList, address.DropDownStyle);
					CollectionAssert.AreEqual(new[] { Options.LoopbackAddress, Options.AnyAddress }, address.Items.Cast<string>().ToArray());
					Assert.AreEqual(o.AiAccessPort.ToString(), Find<TextBox>(ai, "AiAccessPortTextBox").Text);
					Assert.AreEqual(o.AiAccessToken ?? "", Find<TextBox>(ai, "AiAccessTokenTextBox").Text);
					// http.sys routes by the Host header: the door on this computer answers to localhost, and 127.0.0.1 gets 400.
					var url = Find<TextBox>(ai, "AiAccessUrlTextBox");
					Assert.IsTrue(url.ReadOnly);
					if (o.AiAccessAddress != Options.AnyAddress)
						Assert.AreEqual("http://localhost:" + o.AiAccessPort + "/mcp/", url.Text);
					var snippet = Find<TextBox>(ai, "AiAccessSnippetTextBox");
					StringAssert.Contains(snippet.Text, "\"/Mcp\"");
					StringAssert.Contains(snippet.Text, Application.ExecutablePath.Replace("\\", "\\\\"));
					Find<CheckBox>(ai, "AiAccessEnabledCheckBox");
					Find<CheckBox>(ai, "AiAccessTrustLocalCheckBox");
					foreach (var name in new[] { "AiAccessRegenerateButton", "AiAccessCopyButton", "AiAccessUrlCopyButton", "AiAccessPromptButton", "AiAccessLogButton", "AiSkillZipButton" })
						Find<Button>(ai, name);
					Assert.AreEqual(ai.Model.Status, Find<Label>(ai, "AiAccessStatusText").Text);
					// The door's own controls, which the door refuses at every level, are the shared page's, and the program adds its own to the others.
					CollectionAssert.IsSubsetOf(new[] { "AiAccessEnabledCheckBox", "AiAccessComboBox", "AiAccessAddressComboBox", "AiAccessPortTextBox",
						"AiAccessTrustLocalCheckBox", "AiAccessRegenerateButton", "AiSkillClaudeButton", "AiSkillAgentsButton", "AiSkillZipButton" }, McpTools.DoorControls);
					foreach (var name in McpTools.DoorControls)
						Assert.AreEqual(1, ai.Controls.Find(name, true).Length, "The door guards " + name + ", which is not a control on the page.");
					CollectionAssert.IsSubsetOf(AiAccessModel.SecretControls, McpTools.SecretControls);
					CollectionAssert.IsSubsetOf(AiAccessModel.AdminControls, McpTools.AdminControls);
					CollectionAssert.AreEqual(McpTools.DoorControls, McpUiTools.DoorControls);
					CollectionAssert.AreEqual(McpTools.SecretControls, McpUiTools.SecretControls);
				}
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("accessibility")]
		[Description("The page opens with what the door does and this program's example request, read-only and wrapping, and its Copy button is named apart from the page's other Copy buttons")]
		public void The_header_explains_the_door_with_the_programs_example()
		{
			Ui.OnUiThread(() =>
			{
				using (var ai = new AiAccessUserControl())
				{
					UiText.Apply(ai);
					var header = Find<Label>(ai, "AiHeaderText").Text;
					StringAssert.Contains(header, "An AI assistant you run, such as Claude Code or Codex, can read and use this program for you, only at the level you choose.");
					StringAssert.Contains(header, "Nothing can reach this program until you tick AI assistant access.");
					Assert.AreEqual("Example:", Find<Label>(ai, "AiExampleLabel").Text);
					var example = Find<TextBox>(ai, "AiExampleTextBox");
					Assert.AreEqual("In the running x360ce, map the Thrustmaster device to Controller 2, show me where Controller 2's left stick is mapped, "
						+ "then round the corners of its square range.", example.Text);
					Assert.IsTrue(example.ReadOnly, "The example is to read and copy, not to edit.");
					Assert.IsTrue(example.Multiline && example.WordWrap, "The example does not wrap.");
					var buttons = new[] { "AiExampleCopyButton", "AiAccessUrlCopyButton", "AiAccessCopyButton" }.Select(x => Find<Button>(ai, x)).ToArray();
					CollectionAssert.AreEqual(new[] { "Copy example", "Copy URL", "Copy snippet" }, buttons.Select(x => x.AccessibleName).ToArray());
					foreach (var button in buttons)
						StringAssert.StartsWith(button.AccessibleName, button.Text, "The name hides the caption a person reads.");
					var copied = new List<string>();
					var model = new AiAccessModel(copied.Add);
					model.CopyExample();
					CollectionAssert.AreEqual(new[] { AiAccessModel.Example }, copied);
					Assert.AreEqual("Copied.", model.Status);
				}
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("Trust local connections sits under Address, is kept when ticked, is greyed out on every network keeping the choice, and is the person's alone: the door refuses to set it at any level, by the path the WPF page gives it")]
		public void Trust_local_connections_is_greyed_on_every_network_and_is_the_persons_alone()
		{
			Ui.OnUiThread(() =>
			{
				var level = McpCatalog.Level;
				var onUiThread = McpCatalog.OnUiThread;
				using (var form = new Form { Name = "Main" })
				{
					var ai = new AiAccessUserControl { Name = "AiControl", Dock = DockStyle.Fill };
					form.Controls.Add(ai);
					var settings = new CountingSettings();
					ai.Bind(settings);
					var table = Find<TableLayoutPanel>(ai, "AiAccessPanel");
					var trust = Find<CheckBox>(ai, "AiAccessTrustLocalCheckBox");
					var address = Find<ComboBox>(ai, "AiAccessAddressComboBox");
					Assert.AreEqual(table.GetRow(address) + 1, table.GetRow(trust), "The check box is not under Address.");
					Assert.AreEqual(table.GetColumn(address), table.GetColumn(trust), "The check box is not in the value column.");
					Assert.AreEqual("Trust local connections (no token)", trust.Text);
					Assert.IsTrue(trust.Enabled);
					Assert.IsFalse(trust.Checked);
					trust.Checked = true;
					Assert.IsTrue(settings.TrustLocal, "Ticking it did not reach the settings.");
					Assert.AreEqual(1, settings.Saves);
					Assert.AreEqual(1, settings.Applies, "The change was kept but the door was not opened again to match.");
					address.SelectedItem = McpListener.AnyAddress;
					Assert.AreEqual(McpListener.AnyAddress, settings.Address);
					Assert.IsFalse(trust.Enabled, "On every network the token is always needed, so the check box is greyed out.");
					Assert.IsTrue(trust.Checked, "Greying it out changed the person's choice.");
					StringAssert.StartsWith(Find<TextBox>(ai, "AiAccessUrlTextBox").Text, "http://" + Environment.MachineName.ToLowerInvariant() + ":");
					address.SelectedItem = McpListener.LoopbackAddress;
					Assert.IsTrue(trust.Enabled);
					form.Show();
					try
					{
						McpCatalog.Level = () => AiAccess.Administer;
						McpCatalog.OnUiThread = a => a();
						McpUiTools.Root = form;
						var refused = Assert.ThrowsExactly<InvalidOperationException>(() =>
							McpUiTools.UiSet("AiControl/AiPanel/AiAccessPanel/AiAccessTrustLocalCheckBox", "false"));
						StringAssert.Contains(refused.Message, "the person's alone");
						StringAssert.Contains(refused.Message, "Options tab");
						Assert.IsTrue(trust.Checked, "The door changed a door control.");
					}
					finally
					{
						McpUiTools.Root = null;
						McpCatalog.Level = level;
						McpCatalog.OnUiThread = onUiThread;
					}
				}
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("The page passes each change and press to the shared model: a port the door cannot use is refused and put back, a good one is kept then applied, the level comes from the list, Regenerate makes a new token, and the prompt is this program's")]
		public void The_page_passes_every_change_to_the_shared_model()
		{
			Ui.OnUiThread(() =>
			{
				using (var ai = new AiAccessUserControl())
				{
					var settings = new CountingSettings();
					ai.Bind(settings);
					var status = Find<Label>(ai, "AiAccessStatusText");
					Assert.AreEqual("Off. Nothing can reach this program.", status.Text);
					var port = Find<TextBox>(ai, "AiAccessPortTextBox");
					port.Text = "80";
					Validated(port);
					Assert.AreEqual(AiAccessModel.PortRange, status.Text);
					Assert.AreEqual("40000", port.Text, "The box did not get the port back.");
					Assert.AreEqual(0, settings.Saves, "A port the door cannot use was kept.");
					port.Text = "41000";
					Validated(port);
					Assert.AreEqual(41000, settings.Port);
					Assert.AreEqual(1, settings.Saves);
					Assert.AreEqual(1, settings.Applies);
					Assert.AreEqual("http://localhost:41000/mcp/", Find<TextBox>(ai, "AiAccessUrlTextBox").Text);
					Find<ComboBox>(ai, "AiAccessComboBox").SelectedItem = "Configure";
					Assert.AreEqual(AiAccess.Configure, settings.Access);
					var token = settings.Token;
					Find<Button>(ai, "AiAccessRegenerateButton").PerformClick();
					Assert.AreNotEqual(token, settings.Token, "Regenerate made no new token.");
					Assert.AreEqual(settings.Token, Find<TextBox>(ai, "AiAccessTokenTextBox").Text);
					Find<CheckBox>(ai, "AiAccessEnabledCheckBox").Checked = true;
					Assert.IsTrue(settings.Enabled);
					var prompt = ai.Model.Prompt();
					StringAssert.StartsWith(prompt, "Connect to my Jocys.com X360 Controller Emulator (x360ce) as an MCP server, then list my controllers.");
					StringAssert.Contains(prompt, "Start by calling the tool devices_list");
					StringAssert.Contains(prompt, "http://localhost:41000/mcp/");
					StringAssert.Contains(prompt, "Authorization: Bearer " + settings.Token);
					settings.Enabled = false;
					StringAssert.Contains(ai.Model.Prompt(), "I will tick it under AI assistant access on the Options tab first.");
				}
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("accessibility")]
		[Description("The Windows Forms page and the shared catalog agree: every element a person reads or uses has an entry, every entry names an element, the elements follow the catalog's order, which is the WPF page's, and every guarded name is an element")]
		public void Forms_page_and_shared_catalog_agree()
		{
			Assert.AreEqual(AiAccessModel.FormsView, typeof(AiAccessUserControl).Name);
			var catalog = new Dictionary<string, UiText.Text>();
			AiAccessModel.AddCatalog(catalog, AiAccessModel.FormsView);
			Assert.IsTrue(catalog.ContainsKey(AiAccessModel.FormsView), "The page itself has no name and purpose.");
			var prefix = AiAccessModel.FormsView + ".";
			var entries = catalog.Keys.Where(x => x.StartsWith(prefix)).Select(x => x.Substring(prefix.Length)).ToList();
			var fields = typeof(AiAccessUserControl).GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
				.Where(x => typeof(Control).IsAssignableFrom(x.FieldType)).Select(x => x.Name).ToList();
			CollectionAssert.IsSubsetOf(entries, fields, "An entry names no element of the page.");
			// A caption or a title names what is beside or below it and stays out of the tree, as a field ending in Label;
			// the scroll panel only arranges; the skill note says what its section's purpose says.
			var uncatalogued = fields.Where(x => !x.EndsWith("Label")).Except(entries).ToArray();
			CollectionAssert.AreEquivalent(new[] { "AiPanel", "AiSkillNoteText" }, uncatalogued,
				"Elements without a name and purpose: " + string.Join(", ", uncatalogued));
			foreach (var name in AiAccessModel.DoorControls.Concat(AiAccessModel.SecretControls).Concat(AiAccessModel.AdminControls))
				CollectionAssert.Contains(fields, name, name + " is guarded, but it is not an element of the page.");
			Ui.OnUiThread(() =>
			{
				using (var ai = new AiAccessUserControl())
				{
					var order = new List<string>();
					foreach (var name in new[] { "AiHeaderPanel", "AiAccessPanel", "AiSkillPanel" })
					{
						var table = Find<TableLayoutPanel>(ai, name);
						Assert.AreSame(ai.Controls.Find("AiPanel", false).Single(), table.Parent, name + " is not straight inside AiPanel, so its path differs from the WPF page's.");
						order.Add(name);
						var inTabOrder = table.Controls.Cast<Control>().OrderBy(x => x.TabIndex).ToArray();
						// The keyboard and the tree go through a section the way it reads: row by row, left to right.
						var cells = inTabOrder.Select(x => table.GetRow(x) * 10 + table.GetColumn(x)).ToArray();
						CollectionAssert.AreEqual(cells.OrderBy(x => x).ToArray(), cells, name + " is not in reading order.");
						order.AddRange(inTabOrder.Select(x => x.Name).Where(entries.Contains));
					}
					CollectionAssert.AreEqual(entries, order, "The page's order differs from the catalog's.");
				}
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("The exported document names each skill button by its catalog name, the same on every computer, while a live read names it by the caption the person sees")]
		public void The_document_names_the_skill_buttons_the_same_on_every_computer()
		{
			Ui.OnUiThread(() =>
			{
				using (var ai = new AiAccessUserControl())
				{
					UiText.Apply(ai);
					var button = Find<Button>(ai, "AiSkillClaudeButton");
					// What the page writes on a computer that holds an older copy of the skill.
					button.Text = "Update";
					button.AccessibleName = "Update for Claude Code";
					Assert.AreEqual("Install for Claude Code", NodeOf(UiTreeWalker.Read(ai), "AiSkillClaudeButton").Name,
						"The exported document names the button after this computer's skill.");
					Assert.AreEqual("Update for Claude Code", NodeOf(UiTreeWalker.Read(ai, false, ""), "AiSkillClaudeButton").Name,
						"A live read must name the button by the caption the person sees.");
				}
			});
		}

		/// <summary>The node with the field name given, at any depth.</summary>
		static UiNode NodeOf(UiNode node, string id)
		{
			if (node.Id == id)
				return node;
			return node.Items == null ? null : node.Items.Select(x => NodeOf(x, id)).FirstOrDefault(x => x != null);
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical"), Timeout(60000)]
		[Description("Ticking AI assistant access and then Trust local connections on the page, bound to the program's own options, reaches the door through Global: a call without the token is answered only once Trust local is ticked")]
		public void Trust_local_reaches_the_door_through_the_programs_options()
		{
			var o = SettingsManager.Options;
			var saved = new object[] { o.AiAccessEnabled, o.AiAccess, o.AiAccessAddress, o.AiAccessTrustLocal, o.AiAccessPort, o.AiAccessToken };
			var appData = EngineHelper.AppDataPath;
			var logFolder = McpLog.Folder;
			// Every change writes the options file and the door's log, so both go into a folder of the test's own.
			var folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "x360ce.Tests", "trust-" + Guid.NewGuid().ToString("N"))).FullName;
			EngineHelper.AppDataPath = folder;
			SettingsManager.OptionsData.Rebase();
			McpLog.Folder = folder;
			McpListener.Stop();
			o.AiAccessEnabled = false;
			o.AiAccessTrustLocal = false;
			o.AiAccessAddress = Options.LoopbackAddress;
			o.AiAccessPort = McpListenerTest.Port;
			// The program's own start-up path, which subscribes Global to the options.
			Global.InitializeServices();
			try
			{
				Ui.OnUiThread(() =>
				{
					using (var ai = new AiAccessUserControl())
					{
						ai.Bind(new AiAccessSettings());
						Find<CheckBox>(ai, "AiAccessEnabledCheckBox").Checked = true;
						Assert.IsTrue(McpListener.IsRunning, "Ticking AI assistant access did not open the door.");
						Assert.AreEqual(HttpStatusCode.Unauthorized, McpListenerTest.Post(null, null), "A call without the token was answered before Trust local connections was ticked.");
						Find<CheckBox>(ai, "AiAccessTrustLocalCheckBox").Checked = true;
						Assert.IsTrue(o.AiAccessTrustLocal, "The page did not pass Trust local connections to the options.");
						Assert.AreEqual(HttpStatusCode.OK, McpListenerTest.Post(null, null), "Trust local connections was kept, but the door was not opened again with it.");
					}
				});
			}
			finally
			{
				Global.DisposeServices();
				o.AiAccessEnabled = (bool)saved[0];
				o.AiAccess = (AiAccess)saved[1];
				o.AiAccessAddress = (string)saved[2];
				o.AiAccessTrustLocal = (bool)saved[3];
				o.AiAccessPort = (int)saved[4];
				o.AiAccessToken = (string)saved[5];
				McpListener.Stop();
				EngineHelper.AppDataPath = appData;
				SettingsManager.OptionsData.Rebase();
				McpLog.Folder = logFolder;
				Directory.Delete(folder, true);
			}
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("The README tells an AI reader where the latest facts live")]
		public void Readme_points_an_ai_reader_at_the_sources()
		{
			var readme = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "README.MD"));
			foreach (var pointer in new[] { "skills/x360ce", "skills/x360ce/references/ui-tree-v4.md", "skills/x360ce/references/ui-tree-v3.md", "docs/Help.v4.md", "docs/Help.v3.md", "AGENTS.md", "/Mcp", "/Ai" })
				StringAssert.Contains(readme, pointer, "README does not point at " + pointer);
		}
	}
}
