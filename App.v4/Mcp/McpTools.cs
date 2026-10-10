using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using JocysCom.ClassLibrary.Controls.UiTree;
using JocysCom.ClassLibrary.Mcp;
using x360ce.App.UiTree;
using x360ce.Engine;
using x360ce.Engine.Data;

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
		public static readonly string[] AdminControls = new[]
		{
			"ViGEmBusInstallButton", "ViGEmBusUninstallButton",
			"HidGuardianInstallButton", "HidGuardianUninstallButton",
			"HidGuardianConfigureAutomaticallyCheckBox",
			"DebugModeCheckBox",
			"CleanupVirtualPadsButton",
		}.Concat(AiAccessModel.AdminControls).ToArray();

		/// <summary>The door's own controls on the Options tab's AI page, which no caller may touch at any level: the shared page's.</summary>
		public static readonly string[] DoorControls = AiAccessModel.DoorControls;

		/// <summary>The elements whose value is a secret, never read out through the door nor written to its log: the shared page's token.</summary>
		public static readonly string[] SecretControls = AiAccessModel.SecretControls;

		/// <summary>Where the person chooses the access level, as each refusal names it.</summary>
		public const string SettingsPlace = "under AI assistant access on the Options tab";

		/// <summary>What a caller without the token is told: the command line needs none, and the skill says how to use the program.</summary>
		public const string Unauthorised = "Send the token from the program's AI assistant access settings as Authorization: Bearer <token>. "
			+ "A program on this computer can call x360ce.exe -Ai instead, which needs no token. x360ce.exe -Skill explains how to use the program.";

		/// <summary>
		/// The references beside SKILL.md, by their path in the skill's folder, and the embedded document each one is.
		/// Both programs, each named for its version: the skill teaches version 4, and an agent may meet version 3.
		/// </summary>
		public static readonly Dictionary<string, string> SkillReferences = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "references/help-v4.md", AppHelper.HelpV4Resource },
			{ "references/ui-tree-v4.md", "Documents.x360ce.ui-tree-v4.md" },
			{ "references/ui-tree-v4.json", "Documents.x360ce.ui-tree-v4.json" },
			{ "references/help-v3.md", AppHelper.HelpV3Resource },
			{ "references/ui-tree-v3.md", "Documents.x360ce.ui-tree-v3.md" },
			{ "references/ui-tree-v3.json", "Documents.x360ce.ui-tree-v3.json" },
		};

		/// <summary>
		/// Points the shared interface description, the door, its tools and the skill at this program. Called
		/// first thing at start, before a switch is read or a window is built.
		/// </summary>
		public static void Register()
		{
			UiText.Catalog = UiCatalog.Build;
			UiTreeWalker.OwnNamespaces = new[] { "x360ce", "JocysCom" };
			UiTreeExporter.BaseName = "ui-tree-v4";
			UiTreeMarkdown.ExportCommand = "x360ce.exe /ExportUi=<folder>";
			McpServer.ServerName = "x360ce";
			McpServer.ServerVersion = System.Windows.Forms.Application.ProductVersion;
			McpServer.Instructions = "X360CE (Xbox 360 Controller Emulator) maps real controllers, wheels and pedals to virtual Xbox 360 controllers that games read. "
				+ "Begin with devices_list and ui_current; find a setting with ui_find, point the person at it with ui_show, change it with ui_set, and keep changes with settings_save. "
				+ "When ui_find finds nothing, search the help before saying the program cannot do it: a formula in a mapping box does what no single setting does. "
				+ "A refusal names the access level the person must choose on the Options tab's AI page; the AI access controls themselves are the person's alone to change.";
			McpCatalog.SettingsPlace = SettingsPlace;
			McpListener.Unauthorised = Unauthorised;
			// The Fix runs the same reservation elevated, for every user: Program.AdminCommands.cs.
			McpListener.UrlReservationRemedy = prefix => "Press Fix on the Issues tab, or run as Administrator: netsh http add urlacl url=" + prefix + " sddl=D:(A;;GX;;;WD)";
			McpClient.SwitchPrefix = "/";
			// Profile picks whose settings, so whose port and token, the call uses; it is not the tool's.
			McpClient.IgnoredArguments = new[] { "profile" };
			McpLog.Folder = EngineHelper.AppDataPath;
			McpLog.FileName = "x360ce.AiAccess.log";
			McpCatalog.Sources = new[] { typeof(McpUiTools), typeof(McpTools) };
			McpCatalog.Level = () => SettingsManager.Options.AiAccess;
			McpUiTools.MainWindow = () => MainForm.Current;
			McpUiTools.TrayMenu = () => MainForm.Current == null ? null : MainForm.Current.TrayMenu;
			McpUiTools.Restore = window => ((MainForm)window).RestoreFromTray(true);
			McpUiTools.HelpText = () => AppHelper.ReadHelp(AppHelper.HelpV4Resource);
			// The help links to the docs folder's other pages and files, which a caller does not have.
			McpUiTools.ResolveLinks = MarkdownRtf.ResolveLinks;
			McpUiTools.AdminControls = AdminControls;
			McpUiTools.DoorControls = DoorControls;
			McpUiTools.SecretControls = SecretControls;
			AiSkill.Name = "x360ce";
			AiSkill.Resource = "Documents.x360ce.SKILL.md";
			AiSkill.References = SkillReferences;
			AiSkill.ReadResource = AppHelper.ReadHelp;
			AiSkill.ReferenceSuffix = "-v4";
			AiSkill.OwnTree = "references/ui-tree-v4";
			AiSkill.HelpPrefix = "references/help-";
			AiSkill.ProgramAssembly = typeof(McpTools).Assembly;
			AiAccessModel.Subject = "this program";
			AiAccessModel.Example = "In the running x360ce, map the Thrustmaster device to Controller 2, show me where Controller 2's left stick is mapped, "
				+ "then round the corners of its square range.";
			AiAccessModel.PromptRequest = "Connect to my Jocys.com X360 Controller Emulator (x360ce) as an MCP server, then list my controllers.";
			AiAccessModel.PromptFirstTool = "devices_list";
			McpCatalog.Load(McpCatalog.Sources);
		}

		[McpTool(AiAccess.Read, "Every controller the program knows, as JSON: InstanceGuid, Product, Vendor (its maker, as the controller tab's Vendor Name column shows it), Online, Controllers (the controllers 1 to 4 it is on for the current game, empty when none), XInputPlaces (the places games read it in: Real N for the device itself, Virtual N for a controller this program makes from it, Virtual N (waiting) while another controller holds that controller's place), Source (how the program reads it: DirectInput, or RawInput, which reads an Xbox One controller while a game has the focus; a controller read both ways is listed once for each, so map one of the two, and force feedback reaches it from either).")]
		public static object DevicesList()
		{
			var game = SettingsManager.CurrentGame;
			return SettingsManager.UserDevices.ItemsToArraySynchronized().Select(d => (object)new Dictionary<string, object>
			{
				{ "InstanceGuid", d.InstanceGuid.ToString() },
				{ "Product", d.ProductName },
				{ "Vendor", d.DevManufacturer },
				{ "Online", d.IsOnline },
				{ "Controllers", game == null
					? new int[0]
					: SettingsManager.GetDeviceTabs(game.FileName, d.InstanceGuid).Select(x => (int)x).ToArray() },
				{ "XInputPlaces", AppHelper.GetXInputPlaces(d) },
				{ "Source", d.InputSource.ToString() },
			}).ToArray();
		}

		[McpTool(AiAccess.Configure, "Maps a device to a controller 1 to 4 for the current game, or with 0 takes it off every controller. A device already on another controller moves off every other one unless keep is true, which leaves it there as well, the new one starting with the settings of the lowest-numbered controller it is on. A device can be on up to four controllers. The same as the Add and Remove buttons on a controller tab. Below Administer, HID Guardian is left as it is.")]
		public static string DeviceMap([Description("InstanceGuid from devices_list.")] string instanceGuid, [Description("1 to 4, or 0 to unmap.")] int controller, [Description("True keeps the device on the controllers it is already on; false, the default, moves it.")] bool keep = false)
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
				// Off every controller it is on; UnMapGamePadDevices switches off a tab it leaves empty, as Remove does.
				foreach (var setting in SettingsManager.GetSettings(game.FileName).Where(x => x.InstanceGuid == guid && x.MapTo > (int)MapTo.None).ToArray())
					SettingsManager.UnMapGamePadDevices(game, setting, hidGuardian);
				return null;
			}
			SettingsManager.MapGamePadDevices(game, (MapTo)controller, new[] { device }, hidGuardian, keep);
			return null;
		}

		[McpTool(AiAccess.Configure, "Waits for the user to press a button or move an axis on any controller, and returns which device and which control moved, named as the Record button names them.", OnUiThread = false)]
		public static object InputWait([Description("How long to wait, 1 to 60 seconds.")] int seconds = 10)
		{
			seconds = Math.Max(1, Math.Min(60, seconds));
			// The engine fills each device's states again two polls after it replaces them, so the states to compare
			// with are copied once here. The copies are read from a device list taken once, and nothing waits for the
			// engine or the window, so the window keeps drawing while a person reaches for a button.
			var devices = SettingsManager.UserDevices.ItemsToArraySynchronized();
			var before = devices.ToDictionary(d => d.InstanceGuid, d => d.SourceState == null ? null : d.SourceState.Clone());
			var until = DateTime.Now.AddSeconds(seconds);
			while (DateTime.Now < until)
			{
				Thread.Sleep(50);
				foreach (var device in devices)
				{
					var old = before[device.InstanceGuid];
					var now = device.SourceState;
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

		/// <summary>The engine whose passes <see cref="InputLog"/> logs. The program's own unless a test says otherwise.</summary>
		public static Func<DInput.DInputHelper> Engine = () => Global.DHelper;

		[McpTool(AiAccess.Configure, "Logs every change the engine reads from the controllers on the tabs for the seconds given, in order, as JSON: Changes, one line each, with the milliseconds since the log began, the device and the control, named as the Record button names them: each button pressed and let go, each hat direction, and each axis or slider as it reaches another eighth of its range. Logged inside each engine pass, so even a press shorter than a pass is there, as the controller got it. ShownBetweenPasses counts, by source, the changes that came and went between two passes, which reading each pass's state alone would have missed. Dropped counts changes that did not fit.", OnUiThread = false)]
		public static object InputLog([Description("How long to log, 1 to 60 seconds.")] int seconds = 10, [Description("InstanceGuid from devices_list to log one device; omit for all.")] string instanceGuid = null)
		{
			seconds = Math.Max(1, Math.Min(60, seconds));
			Guid only = Guid.Empty;
			if (!string.IsNullOrEmpty(instanceGuid) && !Guid.TryParse(instanceGuid, out only))
				throw new InvalidOperationException("instanceGuid is not a GUID. Take it from devices_list.");
			var engine = Engine();
			if (engine == null)
				throw new InvalidOperationException("The engine is not running.");
			var log = engine.StartInputLog(100000);
			try
			{
				Thread.Sleep(seconds * 1000);
			}
			finally
			{
				engine.StopInputLog();
			}
			return new Dictionary<string, object>
			{
				{ "Changes", DescribeChanges(log, only == Guid.Empty ? (Guid?)null : only) },
				{ "ShownBetweenPasses", new Dictionary<string, object> { { "DirectInput", log.ShownDirectInput }, { "RawInput", log.ShownRawInput } } },
				{ "Dropped", log.Dropped },
			};
		}

		/// <summary>The log's changes as lines: milliseconds since it began, the device, the control and what it did.</summary>
		public static string[] DescribeChanges(DInput.DInputHelper.InputLog log, Guid? onlyDevice = null)
		{
			var count = Volatile.Read(ref log.Count);
			var lines = new List<string>(count);
			for (var i = 0; i < count; i++)
			{
				var c = log.Changes[i];
				if (onlyDevice.HasValue && c.Device.InstanceGuid != onlyDevice.Value)
					continue;
				var ms = (c.Ticks - log.StartTicks) * 1000 / System.Diagnostics.Stopwatch.Frequency;
				string what;
				switch (c.Type)
				{
					case MapType.Button:
						what = "Button " + (c.Index + 1) + (c.Value != 0 ? " down" : " up");
						break;
					case MapType.POV:
						what = "POV " + (c.Index + 1) + (c.Value < 0 ? " centred" : " " + (c.Value / 100) + "°");
						break;
					case MapType.Slider:
						what = "Slider " + (c.Index + 1) + " " + c.Value;
						break;
					default:
						what = "Axis " + (c.Index + 1) + " " + c.Value;
						break;
				}
				lines.Add(ms + " ms " + c.Device.ProductName + " (" + c.Device.InputSource + "): " + what);
			}
			return lines.ToArray();
		}

		[McpTool(AiAccess.Configure, "Applies a preset to the device selected on controller 1 to 4's tab, as the Load Preset button does, by the product name the Load Preset window lists. Select a row with ui_set on the tab's grid first.")]
		public static string PresetApply([Description("1 to 4.")] int controller, [Description("Product name of the preset, as listed.")] string productName)
		{
			if (controller < 1 || controller > 4)
				throw new InvalidOperationException("Controller must be 1 to 4.");
			var preset = SettingsManager.Presets.ItemsToArraySynchronized()
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
