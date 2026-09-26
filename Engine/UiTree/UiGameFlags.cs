using System.Collections.Generic;
using static x360ce.Engine.UiTree.UiText;

namespace x360ce.Engine.UiTree
{
	/// <summary>
	/// The tick boxes that choose a game's hook mask and the libraries written for it. Every version
	/// shows them on its own game details control, each box named after the <see cref="HookMask"/>,
	/// <see cref="XInputMask"/> or <see cref="DInputMask"/> flag it sets, so one description serves all.
	/// </summary>
	public static class UiGameFlags
	{
		/// <summary>Adds an entry for every flag box, keyed under the control that shows them.</summary>
		public static void Add(Dictionary<string, Text> d, string owner)
		{
			// Each flag makes the program answer one question a game asks about the controller. Named
			// after the Windows call each one intercepts, which is what the short captions abbreviate.
			d[owner + ".HookLLCheckBox"] = new Text("Hook Load Library",
				"Answers when the game loads a library, so the replacement is loaded instead.");
			d[owner + ".HookCOMCheckBox"] = new Text("Hook COM",
				"Answers when the game asks Windows for a controller through COM.");
			d[owner + ".HookDICheckBox"] = new Text("Hook Direct Input",
				"Answers when the game asks for a controller through Direct Input.");
			d[owner + ".HookPIDVIDCheckBox"] = new Text("Hook product and vendor codes",
				"Reports the fake product and vendor codes instead of the real ones.");
			d[owner + ".HookNAMECheckBox"] = new Text("Hook name",
				"Reports a different controller name to the game.");
			d[owner + ".HookSACheckBox"] = new Text("Hook SetupAPI",
				"Answers when the game asks Windows to list devices.");
			d[owner + ".HookWTCheckBox"] = new Text("Hook WinVerifyTrust",
				"Answers when the game checks a file's signature.");
			d[owner + ".HookSTOPCheckBox"] = new Text("Stop",
				"Stops answering once the game has started.");
			d[owner + ".HookDISABLECheckBox"] = new Text("Disable",
				"Turns every answer off, leaving the game with the real controllers.");
			AddXInput(d, owner, "91", "9.1");
			AddXInput(d, owner, "11", "1.1");
			AddXInput(d, owner, "12", "1.2");
			AddXInput(d, owner, "13", "1.3");
			AddXInput(d, owner, "14", "1.4");
			d[owner + ".DInput8_x86CheckBox"] = new Text("DInput 8, 32-bit",
				"Supplies the 32-bit Direct Input library to the game.");
			d[owner + ".DInput8_x64CheckBox"] = new Text("DInput 8, 64-bit",
				"Supplies the 64-bit Direct Input library to the game.");
		}

		/// <summary>
		/// Ten identical tick boxes, captioned only "32-bit" or "64-bit", whose meaning comes from
		/// the version label beside them. Each needs its version in its own name to be told apart.
		/// </summary>
		static void AddXInput(Dictionary<string, Text> d, string owner, string field, string version)
		{
			d[owner + ".XInput" + field + "_x86CheckBox"] = new Text(
				"XInput " + version + ", 32-bit",
				"Supplies the 32-bit XInput " + version + " library to the game.");
			d[owner + ".XInput" + field + "_x64CheckBox"] = new Text(
				"XInput " + version + ", 64-bit",
				"Supplies the 64-bit XInput " + version + " library to the game.");
		}
	}
}
