using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Mcp;

namespace x360ce.TestGame
{
	static class Program
	{
		/// <summary>The AI door's port, in x360ce's plan: 37101 is x360ce 4's door, 37102 x360ce 3's, 37103 this game's.</summary>
		public const int Port = 37103;

		/// <summary>Starts the game recording the keyboard and mouse while it is behind, as its box does.</summary>
		const string RecordBehindArgument = "RecordBehind";

		/// <summary>Where the game keeps the door's token, which its own -Ai switch reads to call it, and where its window was.</summary>
		static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "X360CE", "TestGame");

		[STAThread]
		static int Main(string[] args)
		{
			Register();
			var parameters = Parameters(args);
			if (McpClient.IsSwitch(parameters))
				return McpClient.RunSwitches(parameters, true, Port, Token(), Application.ExecutablePath);
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			var form = new MainForm(Folder, parameters.ContainsKey(RecordBehindArgument));
			TestGameTools.Window = form;
			ControlsHelper.InitInvokeContext();
			// The token always: the game can record what is typed in other programs, so no other program reads it without one.
			form.DoorText = McpListener.Program.Start(McpListener.LoopbackAddress, Port, Token(), false)
				? "AI door: http://localhost:" + Port + "/mcp/, or x360ce.TestGame.exe -Ai=input_last"
				: "AI door closed: " + McpListener.Program.LastError;
			Application.Run(form);
			McpListener.Program.Stop();
			return 0;
		}

		/// <summary>Points the shared door and its interface tools at this game.</summary>
		static void Register()
		{
			McpServer.ServerName = "x360ce-testgame";
			McpServer.ServerVersion = Application.ProductVersion;
			McpServer.Instructions = "The x360ce Test Game takes keyboard, mouse and controller input every way a game may, Raw Input, window "
				+ "messages, DirectInput and XInput, and records each event with the way it came and whether the window was in front, "
				+ "so x360ce's mappings and AutoHotkey scripts can be checked from what a game receives. "
				+ "Begin with input_clear, make the input, then read input_events from the Next it returned, or input_last for the newest "
				+ "event and action. input_state says what is held now and where the controllers' sticks are.";
			McpCatalog.SettingsPlace = "in the test game, which opens its door on this computer only";
			McpCatalog.Sources = new[] { typeof(McpUiTools), typeof(TestGameTools) };
			McpCatalog.Level = () => AiAccess.Configure;
			McpUiTools.MainWindow = () => TestGameTools.Window;
			// Recording behind hears what the person types anywhere: theirs alone to switch, at every level.
			McpUiTools.DoorControls = new[] { "RecordBehindCheckBox" };
			McpLog.Folder = Folder;
			McpLog.FileName = "x360ce.TestGame.AiAccess.log";
			McpCatalog.Load(McpCatalog.Sources);
		}

		/// <summary>The door's token, made once and kept in the game's folder, where its own -Ai switch finds it.</summary>
		static string Token()
		{
			var path = Path.Combine(Folder, "x360ce.TestGame.token");
			if (File.Exists(path))
			{
				var kept = File.ReadAllText(path).Trim();
				if (kept.Length > 0)
					return kept;
			}
			Directory.CreateDirectory(Folder);
			var token = McpListener.NewToken();
			File.WriteAllText(path, token);
			return token;
		}

		/// <summary>The command line as name and value, read without regard to case: -Ai=input_last -since=5.</summary>
		static Dictionary<string, string> Parameters(string[] args)
		{
			var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (var arg in args)
			{
				if (arg.Length < 2 || (arg[0] != '-' && arg[0] != '/'))
					continue;
				var text = arg.Substring(1);
				var equals = text.IndexOf('=');
				if (equals < 0)
					parameters[text] = "";
				else
					parameters[text.Substring(0, equals)] = text.Substring(equals + 1);
			}
			return parameters;
		}
	}
}
