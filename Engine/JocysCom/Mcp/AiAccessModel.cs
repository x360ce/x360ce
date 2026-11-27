#nullable disable
using JocysCom.ClassLibrary.Controls.UiTree;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace JocysCom.ClassLibrary.Mcp
{
	/// <summary>
	/// What the AI page shows and does, for both of its views: the WPF <c>AiAccessControl</c> and the Windows Forms
	/// <c>AiAccessUserControl</c>. A view names its controls as the other does, copies this model's state into them
	/// when <see cref="Changed"/> is raised, and passes every change and press to a method here.
	/// </summary>
	/// <remarks>
	/// Holds no interface types, so a Windows Forms program compiles it without WPF. What differs between programs is
	/// set once at start (<see cref="Example"/>, <see cref="PromptRequest"/>, <see cref="PromptFirstTool"/>,
	/// <see cref="Subject"/>) or passed in: the settings through <see cref="IAiAccessSettings"/>, and the clipboard by
	/// the view. Texts that depend only on the program are static; what depends on the settings is on the instance.
	/// </remarks>
	public partial class AiAccessModel
	{
		#region What each program sets

		/// <summary>A request the person can give an assistant that has the program's skill, shown in the page's header. Set by the program.</summary>
		public static string Example = "";

		/// <summary>The prompt's first line: what to connect to and a first thing to ask. Set by the program.</summary>
		public static string PromptRequest = "Connect to my program as an MCP server, then tell me what it can do.";

		/// <summary>The tool the prompt asks an assistant to call first, so it proves the connection with something simple. Set by the program.</summary>
		public static string PromptFirstTool = "help";

		/// <summary>How the page's texts name the program, such as "this client". Set by the program.</summary>
		public static string Subject = "this program";

		#endregion

		#region Shared by every view

		/// <summary>The page's own door controls, which no caller may touch at any level. A program adds them to <see cref="McpUiTools.DoorControls"/>.</summary>
		public static readonly string[] DoorControls =
		{
			"AiAccessEnabledCheckBox", "AiAccessComboBox", "AiAccessAddressComboBox", "AiAccessTrustLocalCheckBox", "AiAccessPortTextBox",
			"AiAccessRegenerateButton", "AiSkillClaudeButton", "AiSkillAgentsButton", "AiSkillZipButton",
			"AiTunnelEnabledCheckBox", "AiTunnelOrganizationTextBox", "AiTunnelAdminKeyTextBox", "AiTunnelIdComboBox", "AiTunnelCreateButton",
			"AiTunnelDeleteButton", "AiTunnelApiKeyTextBox",
		};

		/// <summary>The page's boxes whose value is a secret: the door's token and the tunnel's keys. A program adds them to <see cref="McpUiTools.SecretControls"/>.</summary>
		public static readonly string[] SecretControls = { "AiAccessTokenTextBox", "AiTunnelAdminKeyTextBox", "AiTunnelApiKeyTextBox" };

		/// <summary>
		/// The page's controls that need Administer. None: every control on the page that changes anything is a door
		/// control. A program adds them to <see cref="McpUiTools.AdminControls"/>, so one added here is guarded at once.
		/// </summary>
		public static readonly string[] AdminControls = new string[0];

		/// <summary>The levels the Level list offers, by name.</summary>
		public static readonly string[] Levels = Enum.GetNames(typeof(AiAccess));

		/// <summary>The addresses the Address list offers: this computer only, or every network.</summary>
		public static readonly string[] Addresses = { McpListener.LoopbackAddress, McpListener.AnyAddress };

		/// <summary>What the person is told when the port box holds no port the door can use.</summary>
		public static string PortRange => UiLanguage.T("AiAccessModel.PortRange", "The port must be a number from 1024 to 49151.");

		/// <summary>The Save as ZIP dialog's file type list.</summary>
		public static string ZipFilter => UiLanguage.T("AiAccessModel.ZipFileType", "ZIP files (*.zip)") + "|*.zip";

		/// <summary>The Save as ZIP dialog's default extension.</summary>
		public const string ZipExtension = "zip";

		/// <summary>The Save as ZIP dialog's file name.</summary>
		public static string ZipFileName { get { return AiSkill.Name + "-skill.zip"; } }

		/// <summary>The header: what the door does, and that it is off until the person ticks it.</summary>
		public static string Header
		{
			get
			{
				return string.Format(UiLanguage.T("AiAccessModel.Header",
					"An AI assistant you run, such as Claude Code or Codex, can read and use {0} for you, only at the level you choose. " +
					"Nothing can reach {0} until you tick AI assistant access."), SubjectText);
			}
		}

		/// <summary>The line under the AI skill heading.</summary>
		public static string SkillNote
		{
			get { return string.Format(UiLanguage.T("AiAccessModel.SkillNote", "Teaches an AI agent what {0} does and how to use it through AI assistant access."), SubjectText); }
		}

		/// <summary>What an assistant's MCP settings need to start this program as a server. Holds no token.</summary>
		public static string Snippet { get { return McpClient.ServerSettings(ExecutablePath); } }

		/// <summary>The running program's file.</summary>
		static string ExecutablePath
		{
			get
			{
				using (var process = Process.GetCurrentProcess())
					return process.MainModule.FileName;
			}
		}

		static string SubjectText => Subject == "this program" ? UiLanguage.T("AiAccessModel.DefaultSubject", "this program") : Subject;

		static string ZipHint => UiLanguage.T("AiAccessModel.ZipHint", "Save as a ZIP, then add it in Claude under Customize, Skills.");

		#endregion

		readonly Action<string> _setClipboard;
		readonly SynchronizationContext _context;

		/// <summary>A page's model. The view passes what puts text on its clipboard, which throws when another program holds it.</summary>
		public AiAccessModel(Action<string> setClipboard)
		{
			_setClipboard = setClipboard;
			// The tunnel and the tunnel buttons finish on other threads; their results are shown on the page's own.
			_context = SynchronizationContext.Current;
			Claude = new SkillRow(() => AiSkill.ClaudeFolder, " for Claude Code");
			Agents = new SkillRow(() => AiSkill.AgentsFolder, " for other agents");
		}

		/// <summary>Raised after anything the page shows has changed; the view copies the state into its controls.</summary>
		public event EventHandler Changed;

		#region State the views show

		/// <summary>The program's settings, or null before <see cref="Bind"/>.</summary>
		public IAiAccessSettings Settings { get; private set; }

		/// <summary>The settings of a door other than the program's own, or null when the page shows the program's.</summary>
		public IAiDoorSettings Door { get { return Settings as IAiDoorSettings; } }

		/// <summary>The door the page shows the state of.</summary>
		McpListener Listener { get { return Door == null ? McpListener.Program : Door.Listener; } }

		/// <summary>Whether AI assistant access is ticked.</summary>
		public bool Enabled { get { return Settings != null && Settings.Enabled; } }

		/// <summary>The chosen level, as the Level list names it.</summary>
		public string Level { get { return (Settings == null ? AiAccess.Read : Settings.Access).ToString(); } }

		/// <summary>The chosen address, as the Address list names it.</summary>
		public string Address
		{
			get { return Settings != null && Settings.Address == McpListener.AnyAddress ? McpListener.AnyAddress : McpListener.LoopbackAddress; }
		}

		/// <summary>Whether Trust local connections is ticked.</summary>
		public bool TrustLocal { get { return Settings != null && Settings.TrustLocal; } }

		/// <summary>False on every network, where the token is always needed, so the check box is greyed out.</summary>
		public bool TrustLocalAllowed { get { return Address != McpListener.AnyAddress; } }

		/// <summary>The port, as the port box shows it; empty while the door has none, as a device that is on no controller.</summary>
		public string Port { get { return Settings == null || Settings.Port == 0 ? "" : Settings.Port.ToString(CultureInfo.InvariantCulture); } }

		/// <summary>
		/// The address an agent that connects over HTTP is given. http.sys routes by the Host header: the loopback door
		/// answers to its own prefix's name, and the door on every network answers to the computer's name.
		/// </summary>
		public string Url
		{
			get
			{
				if (Settings == null || Settings.Port == 0)
					return "";
				return Address == McpListener.AnyAddress
					? "http://" + Environment.MachineName.ToLowerInvariant() + ":" + Port + "/mcp/"
					: McpListener.Prefix(McpListener.LoopbackAddress, Settings.Port);
			}
		}

		/// <summary>The door's token.</summary>
		public string Token { get { return Settings == null ? "" : Settings.Token ?? ""; } }

		/// <summary>Whether the door is open and at which level, why it could not open, or what the last press did.</summary>
		public string Status => _status?.Invoke() ?? "";
		Func<string> _status;

		/// <summary>The row for the folder Claude Code reads skills from.</summary>
		public SkillRow Claude { get; private set; }

		/// <summary>The row for the folder the other agents share.</summary>
		public SkillRow Agents { get; private set; }

		/// <summary>What to do with the ZIP, or why it was not saved.</summary>
		public string ZipNote => _zipNote?.Invoke() ?? ZipHint;
		Func<string> _zipNote;

		#endregion

		#region What the person does

		/// <summary>Shows the program's settings, and changes them from now on.</summary>
		public void Bind(IAiAccessSettings settings)
		{
			Settings = settings;
			ShowDoor();
			BindTunnel();
			Check(Claude);
			Check(Agents);
			OnChanged();
		}

		/// <summary>Reads the settings and the door again, after something other than this page changed them.</summary>
		public void Refresh()
		{
			if (Settings != null)
				ShowDoor();
			OnChanged();
		}

		/// <summary>Ticks or clears AI assistant access.</summary>
		public void SetEnabled(bool value)
		{
			if (Settings == null)
				return;
			Settings.Enabled = value;
			Change();
		}

		/// <summary>Chooses the level by its name in the Level list.</summary>
		public void SetLevel(string name)
		{
			AiAccess level;
			if (Settings == null || !Enum.TryParse(name, true, out level) || !Enum.IsDefined(typeof(AiAccess), level))
				return;
			Settings.Access = level;
			Change();
		}

		/// <summary>Chooses the address; anything but every network keeps the door on this computer.</summary>
		public void SetAddress(string address)
		{
			if (Settings == null)
				return;
			Settings.Address = address == McpListener.AnyAddress ? McpListener.AnyAddress : McpListener.LoopbackAddress;
			Change();
		}

		/// <summary>Ticks or clears Trust local connections.</summary>
		public void SetTrustLocal(bool value)
		{
			if (Settings == null)
				return;
			Settings.TrustLocal = value;
			Change();
		}

		/// <summary>Takes the port box's text when editing ends. Text that is no port the door can use is put back, and the person told why.</summary>
		public void SetPort(string text)
		{
			if (Settings == null)
				return;
			int port;
			if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out port) || port < 1024 || port > 49151)
			{
				_status = () => PortRange;
				OnChanged();
				return;
			}
			if (port == Settings.Port)
				return;
			Settings.Port = port;
			Change();
		}

		/// <summary>Makes a new token, so anything holding the old one is shut out.</summary>
		public void Regenerate()
		{
			if (Settings == null)
				return;
			Settings.Token = McpListener.NewToken();
			Change();
		}

		/// <summary>Copies the example request.</summary>
		public void CopyExample() { Copy(Example); }

		/// <summary>Copies the URL.</summary>
		public void CopyUrl() { Copy(Url); }

		/// <summary>Copies the assistant snippet.</summary>
		public void CopySnippet() { Copy(Snippet); }

		/// <summary>Copies the prompt.</summary>
		public void CopyPrompt() { Copy(Prompt()); }

		/// <summary>Opens the log in the program Windows uses for it, or says why it could not: there may be none for .log files.</summary>
		public void OpenLog()
		{
			try
			{
				McpLog.Open();
			}
			catch (Win32Exception ex)
			{
				_status = () => string.Format(UiLanguage.T("AiAccessModel.LogNotOpened", "Log not opened: {0}"), ex.Message);
				OnChanged();
			}
		}

		/// <summary>Looks at both skill folders again. Called each time the page is shown: another program can install or update the skill.</summary>
		public void CheckSkills()
		{
			Check(Claude);
			Check(Agents);
			OnChanged();
		}

		/// <summary>Writes the skill into the Claude Code folder.</summary>
		public void InstallClaude() { Install(Claude); }

		/// <summary>Writes the skill into the folder the other agents share.</summary>
		public void InstallAgents() { Install(Agents); }

		/// <summary>Saves the skill as a ZIP at the path the person chose in the view's save dialog.</summary>
		public void SaveZip(string path)
		{
			try
			{
				AiSkill.SaveZip(path);
				var fileName = Path.GetFileName(path);
				_zipNote = () => string.Format(UiLanguage.T("AiAccessModel.ZipSaved", "Saved. In Claude, add {0} under Customize, Skills."), fileName);
			}
			catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException)
			{
				_zipNote = () => string.Format(UiLanguage.T("AiAccessModel.ZipNotSaved", "Not saved: {0}"), ex.Message);
			}
			OnChanged();
		}

		/// <summary>
		/// What a person pastes into any AI: how to reach the program, both ways, and a first thing to ask, so the
		/// assistant proves the connection with something simple. A door that trusts this computer's own programs needs
		/// no token, so then the prompt does not hand it out.
		/// </summary>
		public string Prompt()
		{
			if (Door != null)
				return Door.Prompt(Url, McpListener.TrustsLocal(Settings.Address, Settings.TrustLocal) ? null : "Authorization: Bearer " + Token);
			var sb = new StringBuilder();
			sb.AppendLine(PromptRequest);
			sb.AppendLine();
			sb.AppendLine("If you can run commands on this computer, add this MCP server:");
			sb.AppendLine(Snippet);
			sb.AppendLine();
			sb.AppendLine("If you connect to MCP servers by URL, use this one (JSON-RPC over HTTP POST):");
			sb.AppendLine(Url);
			if (Settings == null || !McpListener.TrustsLocal(Settings.Address, Settings.TrustLocal))
				sb.AppendLine("with the header: Authorization: Bearer " + Token);
			sb.AppendLine();
			sb.AppendLine("Start by calling the tool " + PromptFirstTool + " and tell me what you found. "
				+ "Use ui_find with a word to locate any control, ui_show to point at one for me, and help for the manual.");
			sb.AppendLine("If you can run commands, \"" + ExecutablePath + "\" -Skill prints full instructions for using " + Subject + ".");
			if (!Enabled)
				sb.AppendLine("Note: AI assistant access is not switched on yet; I will tick it " + McpCatalog.SettingsPlace + " first.");
			return sb.ToString();
		}

		#endregion

		/// <summary>Keeps the settings, opens or closes the door to match them, and shows the result.</summary>
		void Change()
		{
			Settings.Save();
			Settings.Apply();
			ShowDoor();
			OnChanged();
		}

		/// <summary>Whether the door is open and at which level; when it could not open, why.</summary>
		void ShowDoor()
		{
			var level = Level;
			var door = Door;
			if (!Settings.Enabled)
				_status = () => string.Format(UiLanguage.T("AiAccessModel.DoorOff", "Off. Nothing can reach {0}."), door == null ? SubjectText : door.Subject);
			else if (!Listener.IsRunning)
			{
				var error = Listener.LastError;
				_status = () => string.Format(UiLanguage.T("AiAccessModel.DoorNotOpen", "Not open: {0}"), error);
			}
			else if (Address == McpListener.AnyAddress)
				_status = () => string.Format(UiLanguage.T("AiAccessModel.DoorOnEveryNetwork", "On, at the {0} level, on every network: calls and the token travel unencrypted."), LevelText(level));
			else if (TrustLocal)
				_status = () => string.Format(UiLanguage.T("AiAccessModel.DoorOnTrustingLocal", "On, at the {0} level. Programs on this computer need no token."), LevelText(level));
			else
				_status = () => string.Format(UiLanguage.T("AiAccessModel.DoorOn", "On, at the {0} level."), LevelText(level));
		}

		internal static string LevelText(string level)
		{
			switch (level)
			{
				case nameof(AiAccess.Read): return UiLanguage.T("AiAccessModel.LevelRead", "Read");
				case nameof(AiAccess.Configure): return UiLanguage.T("AiAccessModel.LevelConfigure", "Configure");
				case nameof(AiAccess.Administer): return UiLanguage.T("AiAccessModel.LevelAdminister", "Administer");
				default: return level;
			}
		}

		/// <summary>Puts text on the clipboard, and shows what came of it.</summary>
		void Copy(string text)
		{
			_status = CopyText(text);
			OnChanged();
		}

		/// <summary>Puts text on the clipboard. Says it did, or why it could not: another program can hold the clipboard for a moment.</summary>
		Func<string> CopyText(string text)
		{
			try
			{
				_setClipboard(text);
				return () => UiLanguage.T("AiAccessModel.Copied", "Copied.");
			}
			catch (ExternalException ex)
			{
				return () => string.Format(UiLanguage.T("AiAccessModel.NotCopied", "Not copied: {0}"), ex.Message);
			}
		}

		void OnChanged()
		{
			var changed = Changed;
			if (changed != null)
				changed(this, EventArgs.Empty);
		}

		#region AI skill

		/// <summary>One folder agents read skills from, as its row on the page shows it.</summary>
		public sealed class SkillRow
		{
			internal SkillRow(Func<string> folderOf, string forWhom)
			{
				FolderOf = folderOf;
				ForWhom = forWhom;
				Folder = folderOf();
				EnglishCaption = "Install";
				Enabled = true;
			}

			internal readonly Func<string> FolderOf;

			/// <summary>Whom the row is for, as its button's accessible name ends: " for Claude Code".</summary>
			internal readonly string ForWhom;

			/// <summary>The skills folder.</summary>
			public string Folder { get; internal set; }

			/// <summary>Whether the skill is there and current, or why that could not be read or written.</summary>
			public string Status => StatusText?.Invoke() ?? "";
			internal Func<string> StatusText { get; set; }

			/// <summary>The button's caption: what it would do.</summary>
			public string Caption
			{
				get
				{
					switch (EnglishCaption)
					{
						case "Install": return UiLanguage.T("AiAccessModel.SkillInstall", "Install");
						case "Reinstall": return UiLanguage.T("AiAccessModel.SkillReinstall", "Reinstall");
						case "Update": return UiLanguage.T("AiAccessModel.SkillUpdate", "Update");
						default: return EnglishCaption;
					}
				}
			}
			internal string EnglishCaption { get; set; }

			/// <summary>The button's accessible name: its caption and whom it is for, so two buttons that read alike are named apart.</summary>
			public string AccessibleName => EnglishCaption + ForWhom;

			/// <summary>False for a copy a newer program wrote, which is left alone: this program's copy would describe fewer tools.</summary>
			public bool Enabled { get; internal set; }
		}

		/// <summary>Says whether the skill is in the row's folder and current, and names the button for what it would do.</summary>
		static void Check(SkillRow row)
		{
			row.Folder = row.FolderOf();
			AiSkill.State state;
			Version installed;
			try
			{
				state = AiSkill.Check(row.Folder, AiSkill.ProgramVersion, out installed);
			}
			catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
			{
				row.StatusText = () => string.Format(UiLanguage.T("AiAccessModel.SkillCannotRead", "Cannot read the installed copy: {0}"), ex.Message);
				return;
			}
			row.Enabled = state != AiSkill.State.Newer;
			switch (state)
			{
				case AiSkill.State.NotInstalled:
					row.StatusText = () => UiLanguage.T("AiAccessModel.SkillNotInstalled", "Not installed.");
					row.EnglishCaption = "Install";
					break;
				case AiSkill.State.UpToDate:
					row.StatusText = () => UiLanguage.T("AiAccessModel.SkillInstalled", "Installed, this version.");
					row.EnglishCaption = "Reinstall";
					break;
				case AiSkill.State.Older:
					row.StatusText = () => installed == null ? UiLanguage.T("AiAccessModel.SkillOlderCopy", "An older copy.")
						: string.Format(UiLanguage.T("AiAccessModel.SkillOlderVersion", "Version {0}, older."), installed);
					row.EnglishCaption = "Update";
					break;
				default:
					row.StatusText = () => string.Format(UiLanguage.T("AiAccessModel.SkillNewerVersion", "Version {0}, from a newer program."), installed);
					row.EnglishCaption = "Install";
					break;
			}
		}

		void Install(SkillRow row)
		{
			try
			{
				AiSkill.Install(row.FolderOf());
			}
			catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException)
			{
				row.StatusText = () => string.Format(UiLanguage.T("AiAccessModel.SkillNotWritten", "Not written: {0}"), ex.Message);
				OnChanged();
				return;
			}
			CheckSkills();
		}

		#endregion
	}
}
