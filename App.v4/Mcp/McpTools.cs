using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using x360ce.App.UiTree;
using x360ce.Engine;
using x360ce.Engine.Data;
using x360ce.Engine.Mcp;
using x360ce.Engine.UiTree;

namespace x360ce.App.Mcp
{
	/// <summary>
	/// What an assistant or a script can ask of this program beyond its interface: its devices,
	/// presets and settings. Each public static method marked McpTool is one tool, catalogued with
	/// the shared interface tools in <see cref="McpUiTools"/>.
	/// </summary>
	public static class McpTools
	{
		/// <summary>
		/// Elements whose press or setting installs or removes a driver, turns on debug mode, removes
		/// controllers left behind by earlier runs, or sends the controller tabs' Add and Remove
		/// buttons through HID Guardian, which elevates. Below Administer these may not be touched.
		/// Field names as in the tree, so a bar entry is named the same way a control is. The
		/// remaining elevating action, the XInput reorder Apply, shares its field name with the
		/// Options page's own Apply, which administers nothing; it is left out rather than have one
		/// name refuse both, and it states what it will do and waits for the person's answer before
		/// it changes anything.
		/// </summary>
		public static readonly string[] AdminControls =
		{
			"ViGEmBusInstallButton", "ViGEmBusUninstallButton",
			"HidGuardianInstallButton", "HidGuardianUninstallButton",
			"HidGuardianConfigureAutomaticallyCheckBox",
			"DebugModeCheckBox",
			"CleanupVirtualPadsButton",
		};

		/// <summary>The door's own controls on the Options page, which no caller may touch at any level.</summary>
		public static readonly string[] DoorControls =
		{
			"AiAccessEnabledCheckBox", "AiAccessComboBox", "AiAccessAddressComboBox", "AiAccessPortNumericUpDown", "AiAccessRegenerateButton", "AiAccessWindowsCheckBox",
		};

		/// <summary>
		/// Points the shared interface description, the door and its tools at this program. Called
		/// first thing at start, before a switch is read or a window is built.
		/// </summary>
		public static void Register()
		{
			UiText.Catalog = UiCatalog.Build;
			UiTreeExporter.BaseName = "ui-tree-v4";
			McpServer.ServerName = "x360ce";
			McpLog.FileName = "x360ce.AiAccess.log";
			McpCatalog.Sources = new[] { typeof(McpUiTools), typeof(McpTools) };
			McpCatalog.Level = () => SettingsManager.Options.AiAccess;
			McpUiTools.MainWindow = () => MainForm.Current;
			McpUiTools.TrayMenu = () => MainForm.Current == null ? null : MainForm.Current.TrayMenu;
			McpUiTools.Restore = window => ((MainForm)window).RestoreFromTray(true);
			McpUiTools.HelpText = () => AppHelper.ReadHelp(AppHelper.HelpV4Resource);
			McpUiTools.AdminControls = AdminControls;
			McpUiTools.DoorControls = DoorControls;
			McpCatalog.Load(McpCatalog.Sources);
		}

		[McpTool(AiAccess.Read, "Every controller the program knows, as JSON: InstanceGuid, Product, Online, Controller (1 to 4 for the current game, 0 when unmapped), XInputPlaces.")]
		public static object DevicesList()
		{
			var game = SettingsManager.CurrentGame;
			return SettingsManager.UserDevices.ItemsToArraySyncronized().Select(d =>
			{
				var setting = SettingsManager.GetSetting(d.InstanceGuid, game == null ? null : game.FileName);
				return (object)new Dictionary<string, object>
				{
					{ "InstanceGuid", d.InstanceGuid.ToString() },
					{ "Product", d.ProductName },
					{ "Online", d.IsOnline },
					{ "Controller", setting == null || setting.MapTo < 1 ? 0 : setting.MapTo },
					{ "XInputPlaces", AppHelper.GetXInputPlaces(d) },
				};
			}).ToArray();
		}

		[McpTool(AiAccess.Configure, "Maps a device to a controller 1 to 4, or 0 to unmap it, for the current game. The same as the Add and Remove buttons on a controller tab. Below Administer, HID Guardian is left as it is.")]
		public static string DeviceMap([Description("InstanceGuid from devices_list.")] string instanceGuid, [Description("1 to 4, or 0 to unmap.")] int controller)
		{
			if (controller < 0 || controller > 4)
				throw new InvalidOperationException("Controller must be 1 to 4, or 0 to unmap.");
			Guid guid;
			var device = Guid.TryParse(instanceGuid, out guid) ? SettingsManager.GetDevice(guid) : null;
			if (device == null)
				throw new InvalidOperationException("No device with that InstanceGuid. Read devices_list for the ids.");
			var game = SettingsManager.CurrentGame;
			if (game == null)
				throw new InvalidOperationException("No game is selected. Choose one in the Game list first.");
			// Reconfiguring HID Guardian elevates, which only Administer may do through this door.
			var hidGuardian = McpCatalog.Level() >= AiAccess.Administer && SettingsManager.Options.HidGuardianConfigureAutomatically;
			if (controller == 0)
			{
				var setting = SettingsManager.GetSetting(guid, game.FileName);
				if (setting != null)
					SettingsManager.UnMapGamePadDevices(game, setting, hidGuardian);
				return null;
			}
			SettingsManager.MapGamePadDevices(game, (MapTo)controller, new[] { device }, hidGuardian);
			return null;
		}

		[McpTool(AiAccess.Configure, "Waits for the user to press a button or move an axis on any controller, and returns which device and which control moved, named as the Record button names them.", OnUiThread = false)]
		public static object InputWait([Description("How long to wait, 1 to 60 seconds.")] int seconds = 10)
		{
			seconds = Math.Max(1, Math.Min(60, seconds));
			// The engine publishes a fresh state per poll; this reads those states from a device list
			// taken once, and never touches the engine or the window, so the window keeps drawing
			// while a person reaches for a button.
			var devices = SettingsManager.UserDevices.ItemsToArraySyncronized();
			var before = devices.ToDictionary(d => d.InstanceGuid, d => d.DiState);
			var until = DateTime.Now.AddSeconds(seconds);
			while (DateTime.Now < until)
			{
				Thread.Sleep(50);
				foreach (var device in devices)
				{
					var old = before[device.InstanceGuid];
					var now = device.DiState;
					if (old == null || now == null)
						continue;
					var moved = Recorder.CompareTo(old, now, default(MapCode));
					if (moved.Length == 0)
						continue;
					return new Dictionary<string, object>
					{
						{ "InstanceGuid", device.InstanceGuid.ToString() },
						{ "Device", device.ProductName },
						{ "Control", moved[0] },
					};
				}
			}
			return "Nothing moved in " + seconds + " seconds.";
		}

		[McpTool(AiAccess.Configure, "Applies a preset to the device selected on controller 1 to 4's tab, as the Load Preset button does, by the product name the Load Preset window lists. Select a row with ui_set on the tab's grid first.")]
		public static string PresetApply([Description("1 to 4.")] int controller, [Description("Product name of the preset, as listed.")] string productName)
		{
			if (controller < 1 || controller > 4)
				throw new InvalidOperationException("Controller must be 1 to 4.");
			var preset = SettingsManager.Presets.ItemsToArraySyncronized()
				.FirstOrDefault(p => string.Equals(p.ProductName, productName, StringComparison.OrdinalIgnoreCase));
			var ps = preset == null ? null : SettingsManager.GetPadSetting(preset.PadSettingChecksum);
			if (ps == null)
				throw new InvalidOperationException("No preset for " + productName + ". The Load Preset window lists the names.");
			// The selected row is the device the tab's form shows; loading elsewhere would show one
			// device's values under another, and the button itself works on the selection.
			var selected = MainForm.Current.PadControls[controller - 1].GetSelectedSetting();
			if (selected == null)
				throw new InvalidOperationException("No device is selected on controller " + controller + ". Map one, or select its row.");
			MainForm.Current.UpdateTimer.Stop();
			SettingsManager.Current.LoadPadSettingsIntoSelectedDevice((MapTo)controller, selected, ps);
			MainForm.Current.UpdateTimer.Start();
			return null;
		}

		[McpTool(AiAccess.Configure, "Saves every setting, as Save All does.")]
		public static string SettingsSave()
		{
			MainForm.Current.SaveAll();
			return null;
		}
	}
}
