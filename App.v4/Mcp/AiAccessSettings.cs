using JocysCom.ClassLibrary.Mcp;

namespace x360ce.App.Mcp
{
	/// <summary>
	/// The program's AI settings as the shared AI page reads and changes them: <see cref="SettingsManager.AiOptions"/>,
	/// which x360ce.AI.xml keeps.
	/// </summary>
	/// <remarks>
	/// Every setting but the token announces its change, and <see cref="Global"/> opens or closes the door or the tunnel
	/// and writes the file each time one does. So <see cref="Save"/> and <see cref="Apply"/> act only for a new token,
	/// which announces nothing, and a change applies once.
	/// </remarks>
	public class AiAccessSettings : IAiTunnelSettings
	{
		bool _newToken;

		static AiOptions Options { get { return SettingsManager.AiOptions; } }

		public bool Enabled { get { return Options.AiAccessEnabled; } set { Options.AiAccessEnabled = value; } }

		public AiAccess Access { get { return Options.AiAccess; } set { Options.AiAccess = value; } }

		public string Address { get { return Options.AiAccessAddress; } set { Options.AiAccessAddress = value; } }

		public int Port { get { return Options.AiAccessPort; } set { Options.AiAccessPort = value; } }

		public string Token
		{
			get { return Options.AiAccessToken; }
			set
			{
				Options.AiAccessToken = value;
				_newToken = true;
			}
		}

		public bool TrustLocal { get { return Options.AiAccessTrustLocal; } set { Options.AiAccessTrustLocal = value; } }

		public bool TunnelEnabled { get { return Options.TunnelEnabled; } set { Options.TunnelEnabled = value; } }

		public string TunnelId { get { return Options.TunnelId; } set { Options.TunnelId = value; } }

		public string TunnelOrganization { get { return Options.TunnelOrganization; } set { Options.TunnelOrganization = value; } }

		public string TunnelApiKey { get { return Options.TunnelApiKey; } set { Options.TunnelApiKey = value; } }

		public string SettingsFile { get { return SettingsManager.AiOptionsData.XmlFile.FullName; } }

		/// <summary>Nothing to do: <see cref="Apply"/> writes the file with a new token, and Global writes it for every other change.</summary>
		public void Save() { }

		/// <summary>Opens the door again with a new token. Global has applied every other change already.</summary>
		public void Apply()
		{
			if (!_newToken)
				return;
			_newToken = false;
			Global.ApplyAiAccess();
		}
	}
}
