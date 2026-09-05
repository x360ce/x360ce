using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using x360ce.App.UiTree;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Mcp
{
	/// <summary>
	/// What an assistant or a script can ask of the program. Each public static method marked
	/// McpTool is one tool; its name, arguments and description are read from the method, so a
	/// tool is defined here and nowhere else. The catalogue runs every body on the interface
	/// thread; a tool refuses by throwing, and the message is what the caller reads.
	/// </summary>
	public static class McpTools
	{
		/// <summary>Window the path tools start from. The main window unless a test says otherwise.</summary>
		public static Control Root;

		/// <summary>
		/// Controls whose press or setting installs or removes a driver, turns on debug mode, or
		/// sends the controller tabs' Add and Remove buttons through HID Guardian, which elevates.
		/// Below Administer these may not be touched. Field names as in the tree. The two other
		/// elevating actions, reorder Apply and Cleanup virtual pads, are toolbar items, which no
		/// path reaches.
		/// </summary>
		public static string[] AdminControls =
		{
			"ViGEmBusInstallButton", "ViGEmBusUninstallButton",
			"HidGuardianInstallButton", "HidGuardianUninstallButton",
			"HidGuardianConfigureAutomaticallyCheckBox",
			"DebugModeCheckBox",
		};

		/// <summary>
		/// The door's own controls on the Options page. No caller may touch them at any level: the
		/// level is a person's choice, the port is where the door is, and the token is what lets
		/// the caller in.
		/// </summary>
		public static string[] DoorControls =
		{
			"AiAccessEnabledCheckBox", "AiAccessComboBox", "AiAccessAddressComboBox", "AiAccessPortNumericUpDown", "AiAccessRegenerateButton", "AiAccessWindowsCheckBox",
		};

		static Control RootWindow { get { return Root ?? MainForm.Current; } }

		[McpTool(AiAccess.Read, "The interface as a tree: every element with its kind, name, purpose, path and current value, as JSON. Pass a path to read one branch. Sibling controls that read alike are listed once; setting the one shown sets both.")]
		public static string UiRead([Description("Element path from an earlier ui_read; omit for the whole window.")] string path = null)
		{
			path = path ?? "";
			var start = path.Length == 0 ? RootWindow : UiTreeWalker.Find(RootWindow, path);
			if (start == null)
				throw new InvalidOperationException("No element at " + path + ".");
			var node = UiTreeWalker.Read(start, false, path);
			// The contract serialiser writes '/' as '\/', which is valid JSON and useless as a path.
			return JocysCom.ClassLibrary.Runtime.Serializer.SerializeToJson(node).Replace("\\/", "/");
		}

		[McpTool(AiAccess.Configure, "Sets an element by path: true/false for a CheckBox or Choice, a number for a Slider or Number, an item's shown text for a List, a tab name for Tabs, a row index for a Grid, text for Text.")]
		public static string UiSet([Description("Element path from ui_read.")] string path, [Description("The value, as text.")] string value)
		{
			var refused = UiTreeWalker.SetValue(Resolve(path), value);
			if (refused != null)
				throw new InvalidOperationException(refused);
			return null;
		}

		[McpTool(AiAccess.Configure, "Presses a Button by path; the tabs above it are selected first. A button that opens a window answers when that window is closed. Buttons that install drivers need Administer access.")]
		public static string UiInvoke([Description("Element path from ui_read.")] string path)
		{
			var refused = UiTreeWalker.Invoke(Resolve(path));
			if (refused != null)
				throw new InvalidOperationException(refused);
			return null;
		}

		[McpTool(AiAccess.Read, "Points at an element for the person: brings its page to the front, restores the window from the tray if need be, frames the element and shows a balloon with your words beside it. Waits the seconds before answering, so several calls in a row make a paced walkthrough.", OnUiThread = false)]
		public static string UiShow([Description("Element path from ui_read.")] string path, [Description("What to say beside it, in the person's language.")] string text = null, [Description("How long to point, 1 to 60 seconds.")] int seconds = 5)
		{
			seconds = Math.Max(1, Math.Min(60, seconds));
			McpCatalog.OnUiThread(() =>
			{
				var control = UiTreeWalker.Find(RootWindow, path);
				if (control == null)
					throw new InvalidOperationException("No element at " + path + ".");
				// "Show me" means the window too: a person asking cannot see a tray icon's insides.
				var main = control.FindForm() as MainForm;
				if (main != null && (main.WindowState == FormWindowState.Minimized || !main.Visible))
					main.RestoreFromTray(true);
				UiTreeWalker.Reveal(control);
				var window = control.FindForm();
				if (!control.Visible || window == null || !window.Visible || window.WindowState == FormWindowState.Minimized)
					throw new InvalidOperationException("The element is hidden, so there is nothing to point at.");
				UiCallout.Show(control, text, seconds);
			});
			// The pause is the point: the person reads the balloon before the next step arrives.
			Thread.Sleep(seconds * 1000);
			return null;
		}

		[McpTool(AiAccess.Read, "Finds elements whose name, purpose, field name or path contains the words, as JSON: Path, Role, Name, Description, Value. Cheaper than reading the whole tree; use the Path with the other tools.")]
		public static object UiFind([Description("Words to look for, any case.")] string query)
		{
			if (string.IsNullOrWhiteSpace(query))
				throw new InvalidOperationException("Give a word to look for.");
			var found = new List<object>();
			Collect(UiTreeWalker.Read(RootWindow, false, ""), query.Trim(), found);
			return found.ToArray();
		}

		static void Collect(UiNode node, string query, List<object> found)
		{
			if (node.Path != null && (Has(node.Name, query) || Has(node.Description, query) || Has(node.Id, query) || Has(node.Path, query)))
				found.Add(new Dictionary<string, object>
				{
					{ "Path", node.Path }, { "Role", node.Role }, { "Name", node.Name }, { "Description", node.Description }, { "Value", node.Value },
				});
			if (node.Items != null)
				foreach (var child in node.Items)
					Collect(child, query, found);
		}

		static bool Has(string text, string query)
		{
			return text != null && text.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		[McpTool(AiAccess.Read, "Runs a small script, one step per line, in order: 'show <path> | <words> | <seconds>' points at an element; 'click <path>' presses a button; 'set <path> | <value>' sets an element; 'wait <seconds>' pauses. Lines starting with # are ignored. Stops at the first step that fails and says which. show and wait need Read access; click and set need Configure.", OnUiThread = false)]
		public static string UiScript([Description("The steps, one per line.")] string script)
		{
			var lines = (script ?? "").Replace("\r", "").Split('\n');
			var done = 0;
			for (var i = 0; i < lines.Length; i++)
			{
				var line = lines[i].Trim();
				if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
					continue;
				var space = line.IndexOf(' ');
				var verb = (space < 0 ? line : line.Substring(0, space)).ToLowerInvariant();
				// A value or a sentence may hold a '|' of its own: only as many parts are cut as the step has.
				var parts = (space < 0 ? "" : line.Substring(space + 1)).Split(new[] { '|' }, verb == "set" ? 2 : 3).Select(x => x.Trim()).ToArray();
				try
				{
					switch (verb)
					{
						case "show":
							int showFor;
							UiShow(parts[0], parts.Length > 1 ? parts[1] : null, parts.Length > 2 && int.TryParse(parts[2], out showFor) ? showFor : 5);
							break;
						case "wait":
							int waitFor;
							Thread.Sleep(Math.Max(1, Math.Min(60, int.TryParse(parts[0], out waitFor) ? waitFor : 1)) * 1000);
							break;
						case "click":
							RequireConfigure();
							McpCatalog.OnUiThread(() => UiInvoke(parts[0]));
							break;
						case "set":
							RequireConfigure();
							if (parts.Length < 2)
								throw new InvalidOperationException("set needs a path and a value: set <path> | <value>.");
							McpCatalog.OnUiThread(() => UiSet(parts[0], parts[1]));
							break;
						default:
							throw new InvalidOperationException("Unknown step '" + verb + "'. Steps are show, click, set and wait.");
					}
				}
				catch (Exception ex)
				{
					throw new InvalidOperationException("Line " + (i + 1) + " failed after " + done + " step(s): " + ex.Message);
				}
				done++;
			}
			return done + " step(s) done.";
		}

		static void RequireConfigure()
		{
			if (McpCatalog.Level() < AiAccess.Configure)
				throw new InvalidOperationException(McpCatalog.Refusal(AiAccess.Configure));
		}

		[McpTool(AiAccess.Read, "The help page the program shows, as Markdown.")]
		public static string Help()
		{
			return AppHelper.ReadHelp(AppHelper.HelpV4Resource);
		}

		[McpTool(AiAccess.Read, "Every element of the running program's interface with its purpose, as Markdown.")]
		public static string UiTree()
		{
			return UiTreeMarkdown.Write(UiTreeExporter.Read(MainForm.Current, MainForm.Current.TrayMenu));
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
			if (MainForm.Current.PadControls[controller - 1].GetSelectedSetting() == null)
				throw new InvalidOperationException("No device is selected on controller " + controller + ". Map one, or select its row.");
			MainForm.Current.UpdateTimer.Stop();
			SettingsManager.Current.LoadPadSettingsIntoSelectedDevice((MapTo)controller, ps);
			MainForm.Current.UpdateTimer.Start();
			return null;
		}

		[McpTool(AiAccess.Configure, "Saves every setting, as Save All does.")]
		public static string SettingsSave()
		{
			MainForm.Current.SaveAll();
			return null;
		}

		/// <summary>The control a path names, refused when it is the door's own or administers below Administer.</summary>
		static Control Resolve(string path)
		{
			var control = UiTreeWalker.Find(RootWindow, path);
			if (control == null)
				throw new InvalidOperationException("No element at " + path + ".");
			if (DoorControls.Contains(control.Name))
				throw new InvalidOperationException("AI assistant access is changed by a person on the Options page, not through this door.");
			if (McpCatalog.Level() < AiAccess.Administer && AdminControls.Contains(control.Name))
				throw new InvalidOperationException(McpCatalog.Refusal(AiAccess.Administer));
			return control;
		}
	}
}
