using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using x360ce.Engine;

namespace x360ce.App.Mcp
{
	/// <summary>
	/// Registers the program with the Windows on-device agent registry, so agents such as Copilot
	/// find it without being told where it is. The registry ships with newer Windows only; where
	/// its tool is absent nothing is done and the switch says so.
	/// </summary>
	public static class WindowsAgentRegistry
	{
		public const string ServerName = "x360ce";

		/// <summary>The registry's command-line tool, part of Windows where the registry exists.</summary>
		public static string OdrPath
		{
			get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "odr.exe"); }
		}

		/// <summary>Whether the registry tool is on this Windows. A test replaces it, since the tool is not on every machine.</summary>
		public static Func<bool> Available = () => File.Exists(OdrPath);

		public static bool IsAvailable { get { return Available(); } }

		/// <summary>Where the manifest is written: beside the settings unless a test points it elsewhere.</summary>
		public static string Folder;

		/// <summary>The bundle manifest the registry reads, written beside the settings.</summary>
		public static string ManifestPath
		{
			get { return Path.Combine(Folder ?? EngineHelper.AppDataPath, "x360ce.mcpb.json"); }
		}

		/// <summary>Why the last registration failed, for the Issues tab. Null after one that worked.</summary>
		public static string LastError;

		/// <summary>Runs the registry tool with the arguments and returns its exit code. A test replaces it, since the tool is not on every machine.</summary>
		public static Func<string, int> Run = RunOdr;

		/// <summary>
		/// The bundle manifest: who the server is, how to start it, and every tool it will ever
		/// answer with. Windows checks the tool list against the running server, so both come from
		/// the one catalogue and cannot disagree.
		/// </summary>
		public static string Manifest(string exePath)
		{
			var manifest = new Dictionary<string, object>
			{
				{ "manifest_version", "0.3" },
				{ "name", ServerName },
				{ "display_name", "Jocys.com X360 Controller Emulator" },
				{ "version", Application.ProductVersion },
				{ "description", "Inspect and operate the X360 Controller Emulator: read its interface, point at controls, map controllers, apply presets." },
				{ "author", new Dictionary<string, object> { { "name", "Jocys.com" } } },
				{ "server", new Dictionary<string, object>
					{
						{ "type", "binary" },
						{ "entry_point", Path.GetFileName(exePath) },
						{ "mcp_config", new Dictionary<string, object> { { "command", exePath }, { "args", new[] { "/" + Program.arg_Mcp } }, { "env", new Dictionary<string, object>() } } },
					}
				},
				{ "tools", McpCatalog.Tools.Select(t => (object)new Dictionary<string, object> { { "name", t.Name }, { "description", t.Description } }).ToArray() },
				{ "_meta", new Dictionary<string, object>
					{
						{ "com.microsoft.windows", new Dictionary<string, object>
							{
								{ "static_responses", new Dictionary<string, object> { { "initialize", McpServer.InitializeResult() }, { "tools/list", McpServer.ToolsList() } } },
							}
						},
					}
				},
			};
			return new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(manifest);
		}

		/// <summary>
		/// Registers or unregisters, to match the option. Nothing happens where Windows has no
		/// registry. Called at every start as well, so it does only what has changed: the manifest
		/// on disk is the record of what Windows was last told, and an unchanged one is left alone.
		/// </summary>
		public static void Apply(bool register)
		{
			LastError = null;
			if (!IsAvailable)
				return;
			try
			{
				if (register)
				{
					var manifest = Manifest(Application.ExecutablePath);
					if (File.Exists(ManifestPath) && File.ReadAllText(ManifestPath) == manifest)
						return;
					File.WriteAllText(ManifestPath, manifest, new UTF8Encoding(false));
					var code = Run("mcp add \"" + ManifestPath + "\"");
					if (code != 0)
						LastError = "Windows did not register the program for agents (odr.exe returned " + code + "). Try again, or register by hand: odr.exe mcp add \"" + ManifestPath + "\"";
					McpLog.Write("windows registration " + (code == 0 ? "added" : "failed with " + code));
				}
				else
				{
					if (!File.Exists(ManifestPath))
						return;
					var code = Run("mcp remove " + ServerName);
					if (code != 0)
						LastError = "Windows did not unregister the program for agents (odr.exe returned " + code + "). Try again, or unregister by hand: odr.exe mcp remove " + ServerName;
					else
						File.Delete(ManifestPath);
					McpLog.Write("windows registration " + (code == 0 ? "removed" : "not removed, odr.exe returned " + code));
				}
			}
			catch (IOException ex) { LastError = ex.Message; }
			catch (UnauthorizedAccessException ex) { LastError = ex.Message; }
			catch (System.ComponentModel.Win32Exception ex) { LastError = ex.Message; }
		}

		static int RunOdr(string arguments)
		{
			using (var process = Process.Start(new ProcessStartInfo(OdrPath, arguments) { UseShellExecute = false, CreateNoWindow = true }))
			{
				process.WaitForExit();
				return process.ExitCode;
			}
		}
	}
}
