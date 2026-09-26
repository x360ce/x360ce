using System;
using x360ce.Engine;
using x360ce.Engine.Mcp;

namespace x360ce.App
{
	/// <summary>
	/// AI assistant access, kept where every other version 3 option is: the [Options] section of
	/// x360ce.ini beside the program. Off unless a person switches it on, and changed only on the
	/// Options page, never through the door itself.
	/// </summary>
	public class AiAccessSettings
	{
		const string Section = "Options";

		/// <summary>One above version 4's port, so both programs can be reached at once.</summary>
		public const int DefaultPort = 37361;

		/// <summary>The settings the running program uses. Read at start and replaced by the Options page.</summary>
		public static AiAccessSettings Current = new AiAccessSettings();

		/// <summary>Whether an AI assistant or a script may reach the program at all.</summary>
		public bool Enabled;

		/// <summary>How much a connected assistant may do.</summary>
		public AiAccess Level = AiAccess.Read;

		/// <summary>Local port the assistant connects to.</summary>
		public int Port = DefaultPort;

		/// <summary>Token a caller must present. Made by the program; regenerate to revoke.</summary>
		public string Token;

		/// <summary>Registered with the Windows agent registry, so agents such as Copilot find the program by themselves.</summary>
		public bool Windows;

		/// <summary>The settings as the file holds them, with the defaults for what it does not say.</summary>
		public static AiAccessSettings Load()
		{
			var ini = new Ini(SettingManager.IniFileName);
			var s = new AiAccessSettings();
			s.Enabled = ini.GetValue(Section, "AiAccessEnabled") == "1";
			AiAccess level;
			if (Enum.TryParse(ini.GetValue(Section, "AiAccess"), true, out level) && Enum.IsDefined(typeof(AiAccess), level))
				s.Level = level;
			int port;
			if (int.TryParse(ini.GetValue(Section, "AiAccessPort"), out port))
				s.Port = port;
			var token = ini.GetValue(Section, "AiAccessToken");
			s.Token = string.IsNullOrEmpty(token) ? null : token;
			s.Windows = ini.GetValue(Section, "AiAccessWindows") == "1";
			return s;
		}

		/// <summary>Writes every value. False when the file could not be written, which the Options page reports.</summary>
		public bool Save()
		{
			var ini = new Ini(SettingManager.IniFileName);
			return ini.SetValue(Section, "AiAccessEnabled", Enabled ? "1" : "0") != 0
				&& ini.SetValue(Section, "AiAccess", Level.ToString()) != 0
				&& ini.SetValue(Section, "AiAccessPort", Port.ToString()) != 0
				&& ini.SetValue(Section, "AiAccessToken", Token ?? "") != 0
				&& ini.SetValue(Section, "AiAccessWindows", Windows ? "1" : "0") != 0;
		}

		/// <summary>
		/// Makes and stores a token the first time the door is on without one, so the program and a
		/// caller started from the command line read the same token from the file.
		/// </summary>
		public void EnsureToken()
		{
			if (!Enabled || !string.IsNullOrEmpty(Token))
				return;
			Token = McpListener.NewToken();
			Save();
		}

		/// <summary>Opens or closes the door and the Windows registration to match. The door listens on this computer only.</summary>
		public void Apply()
		{
			McpListener.Stop();
			EnsureToken();
			if (Enabled)
				McpListener.Start(McpListener.LoopbackAddress, Port, Token);
			WindowsAgentRegistry.Apply(Enabled && Windows);
		}
	}
}
