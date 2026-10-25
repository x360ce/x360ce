#nullable disable
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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
					+ "cannot read it. The admin key is used only to list, create and delete tunnels and is never kept. tunnel-client is downloaded from "
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

		/// <summary>True while Create, Delete or a list of the tunnels waits for OpenAI; both buttons are greyed out meanwhile.</summary>
		public bool TunnelBusy { get; private set; }

		/// <summary>The organization's tunnels, newest first, as OpenAI last listed them; empty until they are listed.</summary>
		public McpTunnel.Info[] TunnelChoices { get; private set; } = new McpTunnel.Info[0];

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
			// A line of the list of tunnels, picked or pasted, starts with the ID.
			var first = text.Split(' ')[0];
			if (McpTunnel.IsTunnelId(first))
				text = first;
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

		/// <summary>
		/// Lists the organization's tunnels with the admin key, which is used for this call only, so one made earlier, such as
		/// the one a ChatGPT plugin was made with, can be picked. OpenAI lets no other key list them.
		/// </summary>
		public void ListTunnels(string adminKey)
		{
			if (Tunnel == null || TunnelBusy)
				return;
			if (string.IsNullOrEmpty(adminKey))
			{
				ShowTunnel("Paste the admin key to list the organization's tunnels: OpenAI lets no other key list them.");
				return;
			}
			var organization = TunnelOrganization;
			Busy("Listing the tunnels in " + organization + ".");
			Task.Run(() =>
			{
				McpTunnel.Info[] tunnels = null;
				string error = null;
				try
				{
					tunnels = McpTunnel.List(adminKey, organization);
				}
				catch (Exception ex)
				{
					error = Message(ex);
				}
				Post(() =>
				{
					TunnelBusy = false;
					if (tunnels == null)
					{
						ShowTunnel("Not listed: " + error);
						return;
					}
					TunnelChoices = tunnels;
					ShowTunnel(tunnels.Length == 0
						? organization + " has no tunnels yet. Create makes one."
						: organization + " has " + tunnels.Length + " tunnel(s). Pick the one your ChatGPT plugin was made with.");
				});
			});
		}

		/// <summary>Keeps the ID of a tunnel from the list.</summary>
		public void PickTunnel(int index)
		{
			if (index >= 0 && index < TunnelChoices.Length)
				SetTunnelId(TunnelChoices[index].Id);
		}

		/// <summary>
		/// Keeps the ID of the tunnel made for this computer, made now when the organization has none, with the admin key,
		/// which is used for this call only. A tunnel made earlier is used again, so a ChatGPT plugin made with it keeps working.
		/// </summary>
		public void CreateTunnel(string adminKey)
		{
			var tunnel = Tunnel;
			if (tunnel == null || TunnelBusy)
				return;
			var organization = TunnelOrganization;
			Busy("Looking for this computer's tunnel in " + organization + ".");
			Task.Run(() =>
			{
				McpTunnel.Info[] tunnels = null;
				McpTunnel.Info mine = null;
				string id = null;
				string error = null;
				try
				{
					tunnels = McpTunnel.List(adminKey, organization);
					mine = McpTunnel.Mine(tunnels);
					id = mine != null ? mine.Id : McpTunnel.Create(adminKey, organization);
				}
				catch (Exception ex)
				{
					error = Message(ex);
				}
				Post(() =>
				{
					TunnelBusy = false;
					if (tunnels != null)
						TunnelChoices = tunnels;
					if (id == null)
					{
						ShowTunnel("Not made: " + error);
						return;
					}
					tunnel.TunnelId = id;
					TunnelStatus = mine != null
						? "Using " + id + ", made earlier for this computer. Delete it first to make a new one."
						: "Made " + id + ". OpenAI makes it ready in about 30 seconds.";
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
					TunnelChoices = TunnelChoices.Where(x => x.Id != id).ToArray();
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
