using JocysCom.ClassLibrary.Mcp;

namespace x360ce.App.Mcp
{
	/// <summary>
	/// The program's door settings as the shared AI page reads and changes them: the options in
	/// <see cref="SettingsManager.Options"/>, which the options file keeps.
	/// </summary>
	/// <remarks>
	/// Every door option but the token announces its change, and <see cref="Global"/> opens or closes the door and writes
	/// the options file each time one does. So <see cref="Save"/> and <see cref="Apply"/> act only for a new token, which
	/// announces nothing, and a change applies the door once.
	/// </remarks>
	public class AiAccessSettings : IAiAccessSettings
	{
		bool _newToken;

		static Options Options { get { return SettingsManager.Options; } }

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

		/// <summary>Nothing to do: <see cref="Apply"/> writes the options file with a new token, and Global writes it for every other change.</summary>
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
