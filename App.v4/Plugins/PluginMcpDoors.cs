using JocysCom.ClassLibrary.Mcp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace x360ce.App.Plugins
{
	/// <summary>The MCP server of one plugin device whose kind declares one: its settings, kept with the device's, and its door.</summary>
	/// <remarks>
	/// The settings are those of the program's own door, so the device's tab shows them with the AI page's rows, and the
	/// program's door, which refuses those rows wherever they are, can neither open this door nor read its token. They are
	/// kept in the plugin settings store under McpServer.*, beside the device's settings, and never given to the plugin.
	/// The door serves the device's tools alone (<see cref="PluginDeviceTools"/>), at the level chosen here.
	/// </remarks>
	public sealed class PluginDeviceDoor : IAiDoorSettings
	{
		/// <summary>What the door's settings are kept under, before the setting's name.</summary>
		public const string Key = "McpServer.";

		public PluginDeviceDoor(Guid instanceGuid, PluginDeviceType type, PluginSettings store)
		{
			InstanceGuid = instanceGuid;
			Type = type;
			Store = store;
			var tools = new McpToolSet
			{
				Name = "x360ce-" + string.Join("-", Subject.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)),
				Instructions = Subject + " is a " + type.Name + " in X360CE (Xbox 360 Controller Emulator), which maps it to a "
					+ "virtual Xbox 360 controller that games read. This server reaches this device alone. Call " + PluginDeviceTools.ReadTool
					+ " for its settings and their values, and a set_ tool to change one; a set_ tool needs the Configure level, which "
					+ "the person chooses " + SettingsPlace + "." + (string.IsNullOrWhiteSpace(type.Description) ? "" : " " + type.Description.Trim()),
				Tools = () => PluginDeviceTools.For(InstanceGuid, Type, Store),
				Level = () => Access,
				SettingsPlace = SettingsPlace,
			};
			Listener = new McpListener(tools, Subject + " door")
			{
				Unauthorised = "Send the token from " + Subject + "'s MCP server settings, " + SettingsPlace + ", as Authorization: Bearer <token>.",
			};
		}

		/// <summary>Where the device's values are kept.</summary>
		public PluginSettings Store { get; }

		/// <summary>The device, as the device list knows it.</summary>
		public Guid InstanceGuid { get; }

		public PluginDeviceType Type { get; }

		public McpListener Listener { get; }

		/// <summary>The device's name in the device list, such as "Remote MCP Controller 1".</summary>
		public string Subject
		{
			get
			{
				var ud = SettingsManager.GetDevice(InstanceGuid);
				return ud == null || string.IsNullOrEmpty(ud.InstanceName) ? Type.Name : ud.InstanceName;
			}
		}

		/// <summary>Where the person changes the door's settings, as the end of a sentence.</summary>
		string SettingsPlace { get { return "on the " + Type.Name + " tab of the x360ce controller page the device is mapped to"; } }

		string Get(string name)
		{
			return Store.Get(InstanceGuid, Key + name);
		}

		void Set(string name, string value)
		{
			Store.Set(InstanceGuid, Key + name, value);
		}

		public bool Enabled
		{
			get { return Get(nameof(Enabled)) == bool.TrueString; }
			set { Set(nameof(Enabled), value.ToString()); }
		}

		public AiAccess Access
		{
			get
			{
				AiAccess access;
				return Enum.TryParse(Get(nameof(Access)), out access) && Enum.IsDefined(typeof(AiAccess), access) ? access : AiAccess.Read;
			}
			set { Set(nameof(Access), value.ToString()); }
		}

		public string Address
		{
			get { return Get(nameof(Address)) == McpListener.AnyAddress ? McpListener.AnyAddress : McpListener.LoopbackAddress; }
			set { Set(nameof(Address), value); }
		}

		/// <summary>The port the person typed, or the plan's port for the device; 0 while it has neither.</summary>
		public int Port
		{
			get
			{
				int port;
				return int.TryParse(Get(nameof(Port)), NumberStyles.Integer, CultureInfo.InvariantCulture, out port) ? port : PlanPort;
			}
			set { Set(nameof(Port), value.ToString(CultureInfo.InvariantCulture)); }
		}

		/// <summary>
		/// The port x360ce's plan gives the device: the first controller it is on in the current game, and its number there,
		/// as the controller tab's AHK column shows it; 0 while it is on none.
		/// </summary>
		int PlanPort
		{
			get
			{
				var game = SettingsManager.CurrentGame;
				if (game == null)
					return 0;
				var place = DInput.DeviceRouting.GetDeviceNumbers(game, SettingsManager.UserSettings.ItemsToArraySynchronized())
					.Where(x => x.Key.InstanceGuid == InstanceGuid).OrderBy(x => x.Key.MapTo).FirstOrDefault();
				return place.Key == null ? 0 : Mcp.PortPlan.Port(place.Key.MapTo, place.Value);
			}
		}

		/// <summary>The port the door was opened on, so a change of mapping that moves the plan's port opens it again.</summary>
		public int OpenPort { get; private set; }

		public string Token
		{
			get { return Get(nameof(Token)) ?? ""; }
			set { Set(nameof(Token), value); }
		}

		public bool TrustLocal
		{
			get { return Get(nameof(TrustLocal)) == bool.TrueString; }
			set { Set(nameof(TrustLocal), value.ToString()); }
		}

		/// <summary>Nothing to do: the values are kept with the device's settings, which are saved with the program's.</summary>
		public void Save() { }

		/// <summary>Opens or closes the door to match the settings. A door opened without a token makes one and keeps it.</summary>
		public void Apply()
		{
			Listener.Stop();
			OpenPort = 0;
			if (!Enabled)
				return;
			var port = Port;
			if (port == 0)
			{
				Listener.LastError = "the device is on no controller, so the port plan gives it no port. Map it to a controller, or type a port " + SettingsPlace + ".";
				return;
			}
			if (string.IsNullOrEmpty(Token))
				Token = McpListener.NewToken();
			if (Listener.Start(Address, port, Token, TrustLocal))
				OpenPort = port;
		}

		public string Prompt(string url, string authorization)
		{
			var sb = new StringBuilder();
			sb.AppendLine("Connect to " + Subject + ", a " + Type.Name + " in my X360CE (Xbox 360 Controller Emulator), as an MCP server, "
				+ "then tell me what it can do.");
			sb.AppendLine();
			sb.AppendLine("Its URL (JSON-RPC over HTTP POST):");
			sb.AppendLine(url);
			if (authorization != null)
				sb.AppendLine("with the header: " + authorization);
			sb.AppendLine();
			sb.AppendLine("Its tools: " + string.Join(", ", PluginDeviceTools.Names(Type)) + ". Start by calling "
				+ PluginDeviceTools.ReadTool + " and tell me what you found.");
			if (!Enabled)
				sb.AppendLine("Note: its MCP server is not open yet; I will open it " + SettingsPlace + " first.");
			return sb.ToString();
		}
	}

	/// <summary>The doors of the plugin devices whose kind declares an MCP server: one for each device, open while it is ticked open.</summary>
	public static class PluginMcpDoors
	{
		static readonly object _lock = new object();
		static readonly Dictionary<Guid, PluginDeviceDoor> _doors = new Dictionary<Guid, PluginDeviceDoor>();

		/// <summary>A device's door, made the first time it is asked for; null for a device whose kind declares no MCP server.</summary>
		/// <param name="instanceGuid">The device, as the device list knows it.</param>
		/// <param name="type">The device's kind.</param>
		/// <param name="store">Where the device's values are kept: the program's own, <see cref="PluginSettings.Current"/>.</param>
		public static PluginDeviceDoor Get(Guid instanceGuid, PluginDeviceType type, PluginSettings store)
		{
			if (type == null || type.McpServer == null)
				return null;
			lock (_lock)
			{
				PluginDeviceDoor door;
				if (_doors.TryGetValue(instanceGuid, out door) && door.Type == type && door.Store == store)
					return door;
				// The plugin was loaded again: the old kind's door closes, and the new one opens as its settings say.
				if (door != null)
					door.Listener.Stop();
				door = new PluginDeviceDoor(instanceGuid, type, store);
				_doors[instanceGuid] = door;
				return door;
			}
		}

		/// <summary>
		/// Opens the door of each listed device whose MCP server is ticked open and not open on its port yet, and closes the
		/// doors of devices no longer listed. Called as the plugins load, and with each new routing, after the mappings or the
		/// device list changed, so a door follows its device when the port plan gives it another port.
		/// </summary>
		public static void Apply()
		{
			var devices = SettingsManager.UserDevices.ItemsToArraySynchronized();
			lock (_lock)
			{
				foreach (var gone in _doors.Values.Where(x => !devices.Any(d => d.InstanceGuid == x.InstanceGuid)).ToArray())
				{
					gone.Listener.Stop();
					_doors.Remove(gone.InstanceGuid);
				}
			}
			var manager = PluginManager.Current;
			if (manager == null)
				return;
			foreach (var ud in devices)
			{
				var door = Get(ud.InstanceGuid, manager.FindDeviceType(ud.ProductGuid), PluginSettings.Current);
				if (door != null && door.Enabled && door.OpenPort != door.Port)
					door.Apply();
			}
		}

		/// <summary>Closes every door, as the program closes.</summary>
		public static void CloseAll()
		{
			lock (_lock)
			{
				foreach (var door in _doors.Values)
					door.Listener.Stop();
				_doors.Clear();
			}
		}
	}
}
