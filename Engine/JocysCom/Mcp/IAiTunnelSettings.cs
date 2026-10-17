#nullable disable

namespace JocysCom.ClassLibrary.Mcp
{
	/// <summary>
	/// The tunnel's settings, for a program that lets ChatGPT on the web reach its door through <see cref="McpTunnel"/>.
	/// A program whose settings do not implement it shows no tunnel section on the AI page.
	/// </summary>
	public interface IAiTunnelSettings : IAiAccessSettings
	{
		/// <summary>Whether the tunnel is open while the door is.</summary>
		bool TunnelEnabled { get; set; }

		/// <summary>The tunnel ChatGPT's Add custom MCP server asks for: "tunnel_" and 32 hexadecimal digits.</summary>
		string TunnelId { get; set; }

		/// <summary>The OpenAI organization new tunnels are made in: "org-" and letters and digits. Not a secret.</summary>
		string TunnelOrganization { get; set; }

		/// <summary>The API key, as <see cref="McpTunnel.Protect"/> encrypted it for this Windows account; empty when none is kept.</summary>
		string TunnelApiKey { get; set; }

		/// <summary>The file these settings are kept in, which the page names so the person knows where their keys are.</summary>
		string SettingsFile { get; }
	}
}
