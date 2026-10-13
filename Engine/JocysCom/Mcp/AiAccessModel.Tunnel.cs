#nullable disable
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace JocysCom.ClassLibrary.Mcp
{
	public partial class AiAccessModel
	{
		#region Shared by every view

		/// <summary>The tunnel section's first line: why ChatGPT on the web needs a tunnel, and where its keys come from.</summary>
		public static string TunnelNote
		{
			get
			{
				return "ChatGPT runs on OpenAI's servers, so it cannot reach " + Subject + " on this computer. OpenAI's tunnel-client, started here, "
					+ "connects out to OpenAI and passes ChatGPT's calls to the door, with its token, while AI assistant access is on. The IDs and keys below come from the "
					+ "OpenAI Platform, where you sign in with your ChatGPT account. For ChatGPT Plus or Pro, first pick Personal in its organization menu.";
			}
		}

		/// <summary>What to do in ChatGPT once the tunnel is open, beside the caption "In ChatGPT".</summary>
		public const string TunnelChatGpt = "Plugins, the plus button, Add custom MCP server. Connection: Tunnel, with the tunnel ID above. "
			+ "Authentication: No authentication. Then Create as a plugin, Install it, and type @ in a chat to use it.";

		/// <summary>Where the tunnel's keys are kept, and how.</summary>
		public static string TunnelKeysNote
		{
			get
			{
				return "The API key is kept in this file encrypted for your Windows account with Windows DPAPI: other accounts and other computers "
					+ "cannot read it. The admin key is used only for Create and Delete and is never kept. tunnel-client is downloaded from "
					+ McpTunnel.Releases + " into " + McpTunnel.ClientFolder + ".";
			}
		}

		/// <summary>The question the view asks before Delete.</summary>
		public string TunnelDeleteQuestion
		{
			get { return "Delete " + TunnelId + " at OpenAI? ChatGPT can then no longer reach " + Subject + " through it, and the plugin made with it stops working."; }
		}

		#endregion

		#region State the views show

		/// <summary>The tunnel's settings, or null when the program has none and the page shows no tunnel section.</summary>
		public IAiTunnelSettings Tunnel { get { return Settings as IAiTunnelSettings; } }

		/// <summary>Whether Open the tunnel is ticked.</summary>
		public bool TunnelEnabled { get { return Tunnel != null && Tunnel.TunnelEnabled; } }

		/// <summary>The organization new tunnels are made in.</summary>
		public string TunnelOrganization { get { return Tunnel == null ? "" : Tunnel.TunnelOrganization ?? ""; } }

		/// <summary>The tunnel ChatGPT asks for.</summary>
		public string TunnelId { get { return Tunnel == null ? "" : Tunnel.TunnelId ?? ""; } }

		/// <summary>The API key, decrypted for the masked box; empty when none is kept or another account encrypted it.</summary>
		public string TunnelApiKey { get { return Tunnel == null ? "" : McpTunnel.Unprotect(Tunnel.TunnelApiKey); } }

		/// <summary>The file the tunnel's settings are kept in.</summary>
		public string TunnelFile { get { return Tunnel == null ? "" : Tunnel.SettingsFile ?? ""; } }

		/// <summary>Whether the tunnel is open, why not, or what the last press did.</summary>
		public string TunnelStatus { get; private set; }

		/// <summary>True while Create or Delete waits for OpenAI; both buttons are greyed out meanwhile.</summary>
		public bool TunnelBusy { get; private set; }

		/// <summary>Whether there is a tunnel to delete.</summary>
		public bool TunnelDeleteAllowed { get { return !TunnelBusy && McpTunnel.IsTunnelId(TunnelId); } }

		/// <summary>Whether tunnel-client's status page can be opened: only while it runs.</summary>
		public bool TunnelPageAllowed { get { return McpTunnel.StatusPage != null; } }

		#endregion

		#region What the person does

		/// <summary>Ticks or clears Open the tunnel.</summary>
		public void SetTunnelEnabled(bool value)
		{
			if (Tunnel == null || value == Tunnel.TunnelEnabled)
				return;
			Tunnel.TunnelEnabled = value;
			SaveTunnel();
		}

		/// <summary>Takes the organization box's text when editing ends. Text that is no organization ID is put back, and the person told why.</summary>
		public void SetTunnelOrganization(string text)
		{
			text = (text ?? "").Trim();
			if (Tunnel == null || text == TunnelOrganization)
				return;
			if (text.Length > 0 && !McpTunnel.IsOrganization(text))
			{
				ShowTunnel("An organization ID starts with org-. Find opens the page that shows it.");
				return;
			}
			Tunnel.TunnelOrganization = text;
			SaveTunnel();
		}

		/// <summary>Takes the tunnel ID box's text when editing ends, such as a tunnel made on the OpenAI Platform. Text that is no tunnel ID is put back.</summary>
		public void SetTunnelId(string text)
		{
			text = (text ?? "").Trim();
			if (Tunnel == null || text == TunnelId)
				return;
			if (text.Length > 0 && !McpTunnel.IsTunnelId(text))
			{
				ShowTunnel("A tunnel ID is tunnel_ followed by 32 digits and letters a to f.");
				return;
			}
			Tunnel.TunnelId = text;
			SaveTunnel();
		}

		/// <summary>Takes the API key box's text when editing ends: keeps it encrypted for this Windows account, or forgets it when the box is empty.</summary>
		public void SetTunnelApiKey(string text)
		{
			text = (text ?? "").Trim();
			if (Tunnel == null || text == TunnelApiKey)
				return;
			Tunnel.TunnelApiKey = McpTunnel.Protect(text);
			TunnelStatus = text.Length == 0 ? "API key forgotten." : "API key kept, encrypted for your Windows account.";
			SaveTunnel();
		}

		/// <summary>Makes a tunnel in the organization with the admin key, which is used for this call only, and keeps its ID.</summary>
		public void CreateTunnel(string adminKey)
		{
			var tunnel = Tunnel;
			if (tunnel == null || TunnelBusy)
				return;
			var organization = TunnelOrganization;
			Busy("Making a tunnel in " + organization + ".");
			Task.Run(() =>
			{
				string id = null;
				string error = null;
				try
				{
					id = McpTunnel.Create(adminKey, organization);
				}
				catch (Exception ex)
				{
					error = Message(ex);
				}
				Post(() =>
				{
					TunnelBusy = false;
					if (id == null)
					{
						ShowTunnel("Not made: " + error);
						return;
					}
					tunnel.TunnelId = id;
					TunnelStatus = "Made " + id + ". OpenAI makes it ready in about 30 seconds.";
					SaveTunnel();
				});
			});
		}

		/// <summary>Deletes the kept tunnel at OpenAI with the admin key, which is used for this call only, and forgets its ID.</summary>
		public void DeleteTunnel(string adminKey)
		{
			var tunnel = Tunnel;
			if (tunnel == null || !TunnelDeleteAllowed)
				return;
			var id = TunnelId;
			Busy("Deleting " + id + ".");
			Task.Run(() =>
			{
				string error = null;
				try
				{
					McpTunnel.Delete(adminKey, id);
				}
				catch (Exception ex)
				{
					error = Message(ex);
				}
				Post(() =>
				{
					TunnelBusy = false;
					if (error != null)
					{
						ShowTunnel("Not deleted: " + error);
						return;
					}
					tunnel.TunnelId = "";
					TunnelStatus = "Deleted " + id + " at OpenAI.";
					SaveTunnel();
				});
			});
		}

		/// <summary>Copies the tunnel ID, which ChatGPT asks for.</summary>
		public void CopyTunnelId() { ShowTunnel(CopyText(TunnelId)); }

		/// <summary>Opens the OpenAI Platform page that shows the organization ID.</summary>
		public void OpenOrganizationPage() { OpenTunnelPage(McpTunnel.OrganizationPage); }

		/// <summary>Opens the OpenAI Platform page that makes admin keys.</summary>
		public void OpenAdminKeysPage() { OpenTunnelPage(McpTunnel.AdminKeysPage); }

		/// <summary>Opens the OpenAI Platform page that makes API keys.</summary>
		public void OpenApiKeysPage() { OpenTunnelPage(McpTunnel.ApiKeysPage); }

		/// <summary>Opens ChatGPT where it adds a custom MCP server.</summary>
		public void OpenChatGpt() { OpenTunnelPage(McpTunnel.ChatGptPage); }

		/// <summary>Opens tunnel-client's own status page, which counts the calls ChatGPT made.</summary>
		public void OpenTunnelStatusPage()
		{
			var page = McpTunnel.StatusPage;
			if (page != null)
				OpenTunnelPage(page);
		}

		/// <summary>Shows the settings file in File Explorer.</summary>
		public void ShowTunnelFile()
		{
			try
			{
				Process.Start("explorer.exe", "/select,\"" + TunnelFile + "\"");
			}
			catch (Win32Exception ex)
			{
				ShowTunnel("Not shown: " + ex.Message);
			}
		}

		/// <summary>Stops following the tunnel's state. The view calls it when it is disposed.</summary>
		public void Close()
		{
			McpTunnel.Changed -= McpTunnel_Changed;
		}

		#endregion

		/// <summary>Follows the tunnel's state from now on, when the program's settings have a tunnel.</summary>
		void BindTunnel()
		{
			McpTunnel.Changed -= McpTunnel_Changed;
			if (Tunnel == null)
				return;
			McpTunnel.Changed += McpTunnel_Changed;
			TunnelStatus = McpTunnel.Status;
		}

		/// <summary>The tunnel changes state on another thread; the page shows it on its own.</summary>
		void McpTunnel_Changed(object sender, EventArgs e)
		{
			Post(() => ShowTunnel(McpTunnel.Status));
		}

		void SaveTunnel()
		{
			Settings.Save();
			Settings.Apply();
			OnChanged();
		}

		void ShowTunnel(string status)
		{
			TunnelStatus = status;
			OnChanged();
		}

		void Busy(string status)
		{
			TunnelBusy = true;
			ShowTunnel(status);
		}

		void OpenTunnelPage(string address)
		{
			try
			{
				Process.Start(new ProcessStartInfo(address) { UseShellExecute = true });
			}
			catch (Win32Exception ex)
			{
				ShowTunnel("Not opened: " + ex.Message);
			}
		}

		/// <summary>What went wrong, without the wrapper a task puts around it.</summary>
		static string Message(Exception ex)
		{
			while (ex is AggregateException && ex.InnerException != null)
				ex = ex.InnerException;
			return ex.Message;
		}

		/// <summary>Runs an action on the thread the page was made on, where its view may be changed.</summary>
		void Post(Action action)
		{
			if (_context == null)
				action();
			else
				_context.Post(s => action(), null);
		}
	}
}
