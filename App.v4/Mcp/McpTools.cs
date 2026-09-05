using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using x360ce.App.UiTree;

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
			"AiAccessComboBox", "AiAccessPortNumericUpDown", "AiAccessRegenerateButton",
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
