#nullable disable
using System.Collections.Generic;
using JocysCom.ClassLibrary.Controls.UiTree;

namespace JocysCom.ClassLibrary.Mcp
{
	public partial class AiAccessModel
	{
		/// <summary>The WPF view's type name, which its catalog keys start with.</summary>
		public const string WpfView = "AiAccessControl";

		/// <summary>The Windows Forms view's type name, which its catalog keys start with.</summary>
		public const string FormsView = "AiAccessUserControl";

		/// <summary>
		/// Adds the page's names and purposes to a program's catalog, keyed "ViewType.FieldName" as
		/// <see cref="UiText.Catalog"/> keys them, and the page itself by its bare type name. Both views carry the same
		/// field names, so one list serves both: pass <see cref="WpfView"/> or <see cref="FormsView"/>. Call it while
		/// the catalog is built, after <see cref="Subject"/> is set.
		/// </summary>
		public static void AddCatalog(Dictionary<string, UiText.Text> catalog, string viewType)
		{
			foreach (var entry in Entries())
				catalog[entry.Key.Length == 0 ? viewType : viewType + "." + entry.Key] = entry.Value;
		}

		/// <summary>
		/// The page's names and purposes by field name, in the Windows Forms page's order; the empty name is the page itself.
		/// The tabs are the Windows Forms page's; the WPF page shows the sections in one column.
		/// </summary>
		static Dictionary<string, UiText.Text> Entries()
		{
			var s = Subject;
			var d = new Dictionary<string, UiText.Text>();
			d[""] = new UiText.Text("AI assistant access",
				"Lets an AI assistant or a script read or operate " + s + ", and installs the skill that teaches it how.");

			// The header: what the door does, and an example of what to ask.
			d["AiHeaderPanel"] = new UiText.Text("About AI assistant access",
				"What AI assistant access does, and an example of what to ask an assistant once it is on.");
			d["AiHeaderText"] = UiText.Live("What AI assistant access does",
				"Says that an AI assistant you run can read and use " + s + " only at the level you choose, and that nothing can reach it until AI assistant access is ticked.");
			d["AiExampleTextBox"] = new UiText.Text("Example request",
				"Something to ask an AI assistant that has " + s + "'s skill, once AI assistant access is on. It can be selected and copied.");
			d["AiExampleCopyButton"] = new UiText.Text("Copy example",
				"Copies the example request to the clipboard, to paste into an AI assistant.");
			d["AiTabControl"] = new UiText.Text("AI tabs",
				"The skill, the door, and the tunnel for ChatGPT on the web, each on a tab of its own.");

			// The skill.
			d["AiSkillTabPage"] = new UiText.Text("AI Skill",
				"Installs the skill that teaches an AI agent how to use " + s + ".");
			d["AiSkillPanel"] = new UiText.Text("AI skill",
				"Installs the skill that teaches an AI agent what " + s + " does and how to use it, where agents read skills.");
			d["AiSkillClaudeFolderTextBox"] = new UiText.Text("Claude Code skills folder",
				"Where Claude Code reads skills.");
			// The captions of both install buttons are Install, Reinstall or Update, so the page names each by its caption and the
			// agents it is for; the exported document names them by these words, the same on every computer.
			d["AiSkillClaudeButton"] = UiText.Live("Install for Claude Code",
				"Writes the skill and its reference files into the Claude Code folder, replacing only " + s + "'s skill.");
			d["AiSkillClaudeStatusText"] = UiText.Live("Claude Code skill",
				"Whether the skill is in that folder, and whether it is " + s + "'s version.");
			d["AiSkillAgentsFolderTextBox"] = new UiText.Text("Other agents' skills folder",
				"Where Codex, GitHub Copilot, Gemini CLI, Cursor, OpenCode and Windsurf read skills.");
			d["AiSkillAgentsButton"] = UiText.Live("Install for other agents",
				"Writes the skill and its reference files into the folder the other agents share, replacing only " + s + "'s skill.");
			d["AiSkillAgentsStatusText"] = UiText.Live("Other agents' skill",
				"Whether the skill is in that folder, and whether it is " + s + "'s version.");
			d["AiSkillZipNoteText"] = UiText.Live("Claude app",
				"What to do with the ZIP, or why it was not saved.");
			d["AiSkillZipButton"] = new UiText.Text("Save as ZIP",
				"Saves the skill as a ZIP, which the Claude app takes under Customize, Skills.");

			// The door.
			d["AiAccessTabPage"] = new UiText.Text("MCP Server - AI Assistant Access",
				"Opens the door an AI assistant or a script reaches " + s + " by, at the level chosen there.");
			d["AiAccessPanel"] = new UiText.Text("AI assistant access (MCP server)",
				"Lets an AI assistant or a script read or operate " + s + ", at the level chosen here.");
			d["AiAccessEnabledCheckBox"] = new UiText.Text("AI assistant access",
				"Lets an AI assistant or a script reach " + s + " at all. Off until a person ticks it.");
			d["AiAccessComboBox"] = new UiText.Text("AI assistant level",
				"Read, Configure or Administer: how much a connected assistant or script may do.");
			d["AiAccessAddressComboBox"] = new UiText.Text("AI assistant address",
				"127.0.0.1 keeps the door on this computer; 0.0.0.0 opens it to every network the computer is on, where calls and the token travel unencrypted.");
			d["AiAccessTrustLocalCheckBox"] = new UiText.Text("Trust local connections (no token)",
				"Lets programs on this computer reach the door without the token. Greyed out while the address is 0.0.0.0: on every network the token is always needed.");
			d["AiAccessPortTextBox"] = new UiText.Text("AI assistant port",
				"Local port an assistant connects to, from 1024 to 49151. Change it if another program holds it.");
			d["AiAccessUrlTextBox"] = new UiText.Text("AI assistant URL",
				"Address an agent that connects over HTTP is given, with the token as a bearer header unless local connections are trusted.");
			d["AiAccessUrlCopyButton"] = new UiText.Text("Copy URL",
				"Copies the URL to the clipboard.");
			d["AiAccessTokenTextBox"] = new UiText.Text("AI assistant token",
				"What a caller must present to be let in. Made by " + s + ".");
			d["AiAccessRegenerateButton"] = new UiText.Text("Regenerate token",
				"Makes a new token, so anything holding the old one is shut out.");
			d["AiAccessSnippetTextBox"] = new UiText.Text("Assistant snippet",
				"Settings to paste into an assistant so it can start " + s + " as an MCP server.");
			d["AiAccessCopyButton"] = new UiText.Text("Copy snippet",
				"Copies the assistant snippet to the clipboard. The snippet holds no token.");
			d["AiAccessPromptButton"] = new UiText.Text("Copy prompt",
				"Copies instructions for any AI: how to connect to " + s + ", both ways, and a first thing to ask. Unless local connections are trusted it holds the token, so paste it only to an assistant you trust.");
			d["AiAccessLogButton"] = new UiText.Text("Open log",
				"Opens the record of everything an assistant did through this door: each call, its arguments and what came of it.");
			d["AiAccessStatusText"] = UiText.Live("Door state",
				"Whether the door is open and at which level, or why it could not open; after a press, what it did.");

			// The tunnel for ChatGPT on the web. Shown only by a program whose settings keep one.
			d["AiTunnelTabPage"] = new UiText.Text("OpenAI Tunnel",
				"Lets ChatGPT on the web reach " + s + " through the door. Only the person can change it.");
			d["AiTunnelPanel"] = new UiText.Text("ChatGPT on the web (OpenAI tunnel)",
				"Opens an OpenAI Secure MCP Tunnel, so ChatGPT on the web reaches " + s + " through the door. Only the person can change it.");
			d["AiTunnelNoteText"] = new UiText.Text("About the tunnel",
				"Why ChatGPT on the web needs a tunnel, and that its IDs and keys come from the OpenAI Platform.");
			d["AiTunnelOrganizationTextBox"] = new UiText.Text("OpenAI organization ID",
				"The OpenAI organization new tunnels are made in, org- and letters and digits. Personal for ChatGPT Plus or Pro.");
			d["AiTunnelOrganizationButton"] = new UiText.Text("Find organization ID",
				"Opens the OpenAI Platform page that shows the organization ID.");
			d["AiTunnelAdminKeyTextBox"] = new UiText.Text("OpenAI admin key",
				"An admin key of the organization, used only to list, create and delete tunnels and never kept.");
			d["AiTunnelAdminKeyButton"] = new UiText.Text("Get admin key",
				"Opens the OpenAI Platform page that makes admin keys.");
			d["AiTunnelIdComboBox"] = new UiText.Text("Tunnel ID",
				"The tunnel ChatGPT's Add custom MCP server asks for, tunnel_ and 32 characters. Opening the list shows the organization's "
				+ "tunnels, listed with the admin key, so the one a ChatGPT plugin was made with can be picked; or made by Create, or pasted.");
			d["AiTunnelCreateButton"] = new UiText.Text("Create tunnel",
				"Keeps the ID of the tunnel made earlier for this computer, or makes one in the organization with the admin key when there is none.");
			d["AiTunnelCopyButton"] = new UiText.Text("Copy tunnel ID",
				"Copies the tunnel ID, to paste into ChatGPT.");
			d["AiTunnelDeleteButton"] = new UiText.Text("Delete tunnel",
				"Deletes the tunnel at OpenAI with the admin key, after asking, and forgets its ID.");
			d["AiTunnelApiKeyTextBox"] = new UiText.Text("OpenAI API key",
				"The restricted API key the tunnel runs with, made in the same organization with Tunnels: Read and Use. Kept encrypted for this Windows account.");
			d["AiTunnelApiKeyButton"] = new UiText.Text("Get API key",
				"Opens the OpenAI Platform page that makes the API key.");
			d["AiTunnelEnabledCheckBox"] = new UiText.Text("Open the tunnel",
				"Opens the tunnel while AI assistant access is on, now and each time " + s + " starts.");
			d["AiTunnelPageButton"] = new UiText.Text("Tunnel status page",
				"Opens tunnel-client's own page, which shows whether ChatGPT's calls arrive. Only while the tunnel runs.");
			d["AiTunnelChatGptText"] = new UiText.Text("In ChatGPT",
				"What to choose in ChatGPT's Add custom MCP server once the tunnel is open.");
			d["AiTunnelChatGptButton"] = new UiText.Text("Open ChatGPT",
				"Opens the ChatGPT page where custom MCP servers are added.");
			d["AiTunnelFileTextBox"] = new UiText.Text("Tunnel settings file",
				"The file the tunnel's ID, organization and encrypted API key are kept in.");
			d["AiTunnelFileButton"] = new UiText.Text("Show file",
				"Shows the settings file in File Explorer.");
			d["AiTunnelKeysNoteText"] = UiText.Live("Where the keys are",
				"How the API key is kept, that the admin key is never kept, and where tunnel-client is downloaded to.");
			d["AiTunnelStatusText"] = UiText.Live("Tunnel state",
				"Whether the tunnel is open, why not, or what the last press did.");
			return d;
		}
	}
}
