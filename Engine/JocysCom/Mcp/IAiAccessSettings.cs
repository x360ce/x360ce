#nullable disable

namespace JocysCom.ClassLibrary.Mcp
{
	/// <summary>
	/// The door's settings as the AI page reads and changes them. Each program implements it over its own settings
	/// store, so the shared page and its model never name a program's settings type.
	/// </summary>
	public interface IAiAccessSettings
	{
		/// <summary>Whether an AI assistant or a script may reach the program at all.</summary>
		bool Enabled { get; set; }

		/// <summary>How much a connected assistant or script may do.</summary>
		AiAccess Access { get; set; }

		/// <summary>Where the door listens: <see cref="McpListener.LoopbackAddress"/> or <see cref="McpListener.AnyAddress"/>.</summary>
		string Address { get; set; }

		/// <summary>The port the door listens on, from 1024 to 49151.</summary>
		int Port { get; set; }

		/// <summary>What a caller must present to be let in.</summary>
		string Token { get; set; }

		/// <summary>Whether programs on this computer reach the door without the token while it listens on the loopback address.</summary>
		bool TrustLocal { get; set; }

		/// <summary>Keeps the settings, so the -Ai and -Mcp switches read what the page shows.</summary>
		void Save();

		/// <summary>Opens or closes the door to match the settings. A door opened without a token makes one and keeps it.</summary>
		void Apply();
	}
}
