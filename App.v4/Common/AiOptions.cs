using JocysCom.ClassLibrary.Mcp;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace x360ce.App
{
	/// <summary>
	/// The AI settings, kept apart from the other options in x360ce.AI.xml: AI assistant access, the door an AI assistant
	/// or a script reaches the program by, and the tunnel that lets ChatGPT on the web reach that door.
	/// </summary>
	/// <remarks>
	/// The file sits beside the other settings, where every account on this computer can read it. So the tunnel's runtime
	/// key is kept encrypted for the Windows account that saved it (<see cref="McpTunnel.Protect"/>). The door's token is
	/// kept as it is: every copy of the program the person starts reads it, the -Ai and -Mcp switches included.
	/// </remarks>
	public class AiOptions : INotifyPropertyChanged
	{
		// AI assistant access

		[DefaultValue(false), Description("Whether an AI assistant or a script may reach the program at all.")]
		public bool AiAccessEnabled { get { return _AiAccessEnabled; } set { _AiAccessEnabled = value; OnPropertyChanged(); } }
		bool _AiAccessEnabled;

		[DefaultValue(AiAccess.Read), Description("How much a connected assistant may do: Read, Configure or Administer.")]
		public AiAccess AiAccess { get { return _AiAccess; } set { _AiAccess = value; OnPropertyChanged(); } }
		AiAccess _AiAccess = AiAccess.Read;

		[DefaultValue(LoopbackAddress), Description("Where the door listens: 127.0.0.1 for this computer only, 0.0.0.0 for every network.")]
		public string AiAccessAddress { get { return _AiAccessAddress; } set { _AiAccessAddress = value; OnPropertyChanged(); } }
		string _AiAccessAddress = LoopbackAddress;

		/// <summary>The address that keeps the door on this computer. The default.</summary>
		public const string LoopbackAddress = McpListener.LoopbackAddress;
		/// <summary>The address that opens the door to every network the computer is on.</summary>
		public const string AnyAddress = McpListener.AnyAddress;

		[DefaultValue(false), Description("Whether programs on this computer reach the door without the token while it listens on 127.0.0.1 only.")]
		public bool AiAccessTrustLocal { get { return _AiAccessTrustLocal; } set { _AiAccessTrustLocal = value; OnPropertyChanged(); } }
		bool _AiAccessTrustLocal;

		[DefaultValue(Mcp.PortPlan.AiAccess), Description("Local port the assistant connects to.")]
		public int AiAccessPort { get { return _AiAccessPort; } set { _AiAccessPort = value; OnPropertyChanged(); } }
		int _AiAccessPort = Mcp.PortPlan.AiAccess;

		[Description("Token a caller must present. Made by the program; regenerate to revoke.")]
		public string AiAccessToken { get; set; }

		/// <summary>Makes the token when there is none. True when it did.</summary>
		public bool EnsureAiAccessToken()
		{
			if (!string.IsNullOrEmpty(AiAccessToken))
				return false;
			RegenerateAiAccessToken();
			return true;
		}

		public string RegenerateAiAccessToken()
		{
			AiAccessToken = McpListener.NewToken();
			return AiAccessToken;
		}

		// ChatGPT on the web

		[DefaultValue(false), Description("Whether the OpenAI tunnel for ChatGPT on the web is open while AI assistant access is on.")]
		public bool TunnelEnabled { get { return _TunnelEnabled; } set { _TunnelEnabled = value; OnPropertyChanged(); } }
		bool _TunnelEnabled;

		[Description("The tunnel ChatGPT's Add custom MCP server asks for: tunnel_ and 32 characters.")]
		public string TunnelId { get { return _TunnelId; } set { _TunnelId = value; OnPropertyChanged(); } }
		string _TunnelId;

		[Description("The OpenAI organization new tunnels are made in. Not a secret.")]
		public string TunnelOrganization { get { return _TunnelOrganization; } set { _TunnelOrganization = value; OnPropertyChanged(); } }
		string _TunnelOrganization;

		[Description("The OpenAI API key the tunnel runs with, encrypted with Windows DPAPI for the Windows account that saved it. Other accounts and other computers cannot read it.")]
		public string TunnelApiKey { get { return _TunnelApiKey; } set { _TunnelApiKey = value; OnPropertyChanged(); } }
		string _TunnelApiKey;

		public event PropertyChangedEventHandler PropertyChanged;

		public void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
