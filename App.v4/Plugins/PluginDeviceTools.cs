using JocysCom.ClassLibrary.Mcp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using x360ce.Plugins;

namespace x360ce.App.Plugins
{
	/// <summary>A plugin device's settings as tools a program or an AI assistant calls over MCP.</summary>
	/// <remarks>
	/// read returns every setting with its value, and a set_ tool sets each setting a person can change, such as set_state.
	/// One set of tools for each device, served two ways: alone, on the device's own MCP server, and through the program's
	/// door, which lists them in devices_list and calls them with device_call. A value is kept as the device's tab keeps
	/// it, so the engine gives it to the plugin before its next read; a status is read from the instance the engine reads.
	/// </remarks>
	public static class PluginDeviceTools
	{
		/// <summary>The tool that returns every setting with its value.</summary>
		public const string ReadTool = "read";

		/// <summary>The names of a kind of device's tools: read, then a set_ tool for each setting a person can change.</summary>
		public static string[] Names(PluginDeviceType type)
		{
			return new[] { ReadTool }.Concat(Editable(type).Select(NameOf)).ToArray();
		}

		/// <summary>A device's tools.</summary>
		/// <param name="instanceGuid">The device, as the device list knows it.</param>
		/// <param name="type">The device's kind.</param>
		/// <param name="store">Where the device's values are kept.</param>
		public static List<McpToolInfo> For(Guid instanceGuid, PluginDeviceType type, PluginSettings store)
		{
			var tools = new List<McpToolInfo>
			{
				new McpToolInfo
				{
					Name = ReadTool,
					Description = "Every setting of this " + type.Name + " with its value now, as JSON by name: "
						+ string.Join(", ", type.Settings.Select(x => x.Name)) + ". A status, which cannot be set, is null while "
						+ "x360ce reads no device for it: map the device to a controller.",
					Level = AiAccess.Read,
					OnUiThread = true,
					Method = ReadMethod,
					Parameters = ReadMethod.GetParameters(),
					Body = values => Read(instanceGuid, type, store),
				},
			};
			foreach (var setting in Editable(type))
			{
				var s = setting;
				tools.Add(new McpToolInfo
				{
					Name = NameOf(s),
					Description = "Sets " + s.DisplayName + (string.IsNullOrWhiteSpace(s.Description) ? "." : ": " + s.Description.Trim())
						+ " Takes " + s.Takes + ".",
					Level = AiAccess.Configure,
					OnUiThread = true,
					Method = ValueMethod,
					Parameters = ValueMethod.GetParameters(),
					Body = values => Set(instanceGuid, s, store, (string)values[0]),
				});
			}
			return tools;
		}

		/// <summary>A setting's status as the device the engine reads says it now, or null when it says nothing.</summary>
		/// <remarks>
		/// Read on the interface thread while the engine may be reading the device, which is what the plugin's help asks a
		/// status property to allow. One that throws switches the plugin off, as any of its members does.
		/// </remarks>
		public static string ReadStatus(PluginDeviceType type, PluginSetting setting, IPluginDevice device)
		{
			if (device == null || type.Faulted)
				return null;
			try
			{
				return setting.StatusText(device);
			}
			catch (Exception ex)
			{
				var manager = PluginManager.Current;
				if (manager != null)
					manager.Fault(type, ex);
				return null;
			}
		}

		static IEnumerable<PluginSetting> Editable(PluginDeviceType type)
		{
			return type.Settings.Where(x => x.Kind != PluginSettingKind.Status);
		}

		/// <summary>A setting's tool: State becomes set_state, MoveAxes set_move_axes.</summary>
		static string NameOf(PluginSetting setting)
		{
			return "set_" + McpCatalog.ToolName(setting.Name);
		}

		static Dictionary<string, object> Read(Guid instanceGuid, PluginDeviceType type, PluginSettings store)
		{
			var device = Global.DHelper == null ? null : Global.DHelper.GetPluginDevice(instanceGuid);
			var values = new Dictionary<string, object>();
			foreach (var setting in type.Settings)
				values[setting.Name] = setting.Kind == PluginSettingKind.Status
					? ReadStatus(type, setting, device)
					: store.Get(instanceGuid, setting.Name) ?? setting.DefaultText;
			return values;
		}

		static string Set(Guid instanceGuid, PluginSetting setting, PluginSettings store, string value)
		{
			store.Set(instanceGuid, setting.Name, setting.Parse(value));
			return setting.DisplayName + " is set; the device is given it before x360ce next reads it.";
		}

		/// <summary>Lends read its parameters: none.</summary>
		static void ReadParameters() { }

		/// <summary>Lends a set_ tool its one parameter.</summary>
		static void ValueParameters([Description("The new value, as text.")] string value) { }

		static readonly MethodInfo ReadMethod = typeof(PluginDeviceTools).GetMethod(nameof(ReadParameters), BindingFlags.NonPublic | BindingFlags.Static);

		static readonly MethodInfo ValueMethod = typeof(PluginDeviceTools).GetMethod(nameof(ValueParameters), BindingFlags.NonPublic | BindingFlags.Static);
	}
}
