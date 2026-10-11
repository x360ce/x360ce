using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JocysCom.ClassLibrary.Mcp;

namespace x360ce.App.Plugins
{
	/// <summary>Each plugin's documentation for AI agents: one Markdown file a plugin, written beside the skill's references and read by plugins_list.</summary>
	/// <remarks>
	/// Made from what the plugin manager knows, so it says what the program found on this computer: the plugin's devices,
	/// their controls, who signed it, whether it loaded, and the help its author wrote in the class's Description attribute.
	/// </remarks>
	public static class PluginDocs
	{
		/// <summary>The folder of the skill that holds one file for each plugin.</summary>
		public const string Folder = "references/plugins/";

		/// <summary>The start of the name a plugin's document is read by, in place of an embedded document's.</summary>
		const string ResourcePrefix = "Plugin:";

		/// <summary>The manager the documents are made from.</summary>
		static PluginManager _manager;

		/// <summary>A manager holding only the plugins embedded in this program, for a start that answers a switch and stops.</summary>
		public static PluginManager EmbeddedManager()
		{
			var manager = new PluginManager(null, null, () => false, typeof(PluginDocs).Assembly);
			manager.LoadEmbedded();
			return manager;
		}

		/// <summary>Lists the skill's references again: the program's own, and one for each plugin the manager knows.</summary>
		/// <remarks>Replaced whole, so an install under way on another thread reads the old list or the new one.</remarks>
		public static void UpdateReferences(PluginManager manager)
		{
			_manager = manager;
			var references = new Dictionary<string, string>(Mcp.McpTools.SkillReferences, StringComparer.OrdinalIgnoreCase);
			if (manager != null)
				foreach (var file in manager.Files)
					references[Folder + file.Name + ".md"] = ResourcePrefix + file.Name;
			AiSkill.References = references;
		}

		/// <summary>Reads a document the skill lists: a plugin's, made now, or one embedded in the program.</summary>
		public static string Read(string name)
		{
			if (!name.StartsWith(ResourcePrefix, StringComparison.Ordinal))
				return AppHelper.ReadHelp(name);
			var manager = _manager;
			var file = manager == null ? null : manager.Files.FirstOrDefault(x => x.Name == name.Substring(ResourcePrefix.Length));
			return file == null ? "" : Markdown(file);
		}

		/// <summary>A plugin's document: what it is, what became of it, and each kind of device it adds.</summary>
		public static string Markdown(PluginFile file)
		{
			var sb = new StringBuilder();
			sb.Append("# ").Append(file.Name).Append("\n\n");
			sb.Append("An x360ce plugin. How plugins work, and how to write one: ../plugins.md.\n\n");
			sb.Append("- Source: ").Append(file.Embedded ? "embedded in x360ce.exe" : file.Source).Append('\n');
			sb.Append("- Signed by: ").Append(file.Embedded ? "part of x360ce" : file.Signer ?? "not signed").Append('\n');
			sb.Append("- State: ").Append(StateText(file)).Append('\n');
			foreach (var type in file.DeviceTypes)
			{
				sb.Append("\n## ").Append(type.Name).Append("\n\n");
				sb.Append("- Product GUID: ").Append(type.ProductGuid).Append('\n');
				sb.Append("- Controls: axes ").Append(type.AxisCount).Append(", sliders ").Append(type.SliderCount)
					.Append(", hat switches ").Append(type.PovCount).Append(", buttons ").Append(type.ButtonCount).Append('\n');
				if (type.Faulted)
					sb.Append("- Switched off after it failed, until x360ce starts again: ").Append(type.FaultReason).Append('\n');
				sb.Append("- To use it: Add Plugin Device on the Devices tab adds one; map it to a controller like any device.\n");
				if (!string.IsNullOrWhiteSpace(type.Description))
					sb.Append('\n').Append(type.Description.Trim()).Append('\n');
			}
			return sb.ToString();
		}

		/// <summary>Whether the plugin loaded, and if not, why.</summary>
		public static string StateText(PluginFile file)
		{
			switch (file.State)
			{
				case PluginFileState.Loaded:
					return file.Reason == null ? "loaded" : "loaded. " + file.Reason;
				case PluginFileState.WaitingForPermission:
					return "waiting for permission. " + file.Reason;
				default:
					return "failed. " + file.Reason;
			}
		}
	}
}
