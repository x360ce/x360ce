using System.Collections.Generic;
using System.IO;
using System.Linq;
using x360ce.App.UiTree;
using x360ce.Engine;
using x360ce.Engine.Mcp;
using x360ce.Engine.UiTree;

namespace x360ce.App.Mcp
{
	/// <summary>
	/// What an assistant or a script can ask of version 3 beyond its interface. Each public static
	/// method marked McpTool is one tool, catalogued with the shared interface tools in
	/// <see cref="McpUiTools"/>.
	/// </summary>
	public static class McpTools
	{
		/// <summary>Elements that change how the program itself runs rather than a setting of a game. Below Administer these may not be touched.</summary>
		public static readonly string[] AdminControls =
		{
			"DebugModeCheckBox",
		};

		/// <summary>The door's own controls on the Options page, which no caller may touch at any level.</summary>
		public static readonly string[] DoorControls =
		{
			"AiAccessEnabledCheckBox", "AiAccessComboBox", "AiAccessPortNumericUpDown", "AiAccessRegenerateButton", "AiAccessWindowsCheckBox",
		};

		/// <summary>
		/// Points the shared interface description, the door and its tools at this program. Called
		/// first thing at start, before a switch is read or a window is built. Version 3 answers as
		/// x360ce-v3, keeps its own log and listens on its own port, so both versions can be
		/// connected at once.
		/// </summary>
		public static void Register()
		{
			UiText.Catalog = UiCatalog.Build;
			UiTreeExporter.BaseName = "ui-tree-v3";
			McpServer.ServerName = "x360ce-v3";
			McpLog.FileName = "x360ce.v3.AiAccess.log";
			WindowsAgentRegistry.DisplayName = "Jocys.com X360 Controller Emulator 3";
			WindowsAgentRegistry.Description = "Inspect and operate the X360 Controller Emulator 3: read its interface, point at controls, answer its warnings, change its settings.";
			McpCatalog.Sources = new[] { typeof(McpUiTools), typeof(McpTools) };
			McpCatalog.Level = () => AiAccessSettings.Current.Level;
			McpUiTools.MainWindow = () => MainForm.Current;
			McpUiTools.TrayMenu = () => MainForm.Current == null ? null : MainForm.Current.TrayMenu;
			McpUiTools.Restore = window => ((MainForm)window).RestoreFromTray();
			McpUiTools.HelpText = () =>
			{
				using (var reader = new StreamReader(EngineHelper.GetResourceStream("Documents.Help.v3.md")))
					return reader.ReadToEnd();
			};
			McpUiTools.AdminControls = AdminControls;
			McpUiTools.DoorControls = DoorControls;
			McpCatalog.Load(McpCatalog.Sources);
		}

		[McpTool(AiAccess.Read, "The four controllers and the device each one reads, as JSON: Controller (1 to 4), Product, InstanceGuid; Product is empty where no device is connected.")]
		public static object DevicesList()
		{
			var devices = MainForm.Current.PadDevices;
			return Enumerable.Range(0, devices.Length).Select(i => (object)new Dictionary<string, object>
			{
				{ "Controller", i + 1 },
				{ "Product", devices[i] == null ? "" : devices[i].ProductName },
				{ "InstanceGuid", devices[i] == null ? "" : devices[i].InstanceGuid.ToString() },
			}).ToArray();
		}

		[McpTool(AiAccess.Configure, "Saves every setting to x360ce.ini, as the Save button on the Options page does.")]
		public static string SettingsSave()
		{
			MainForm.Current.SaveSettings();
			return null;
		}
	}
}
