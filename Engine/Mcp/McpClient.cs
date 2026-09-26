using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace x360ce.Engine.Mcp
{
	/// <summary>The client side of the program's own door. Port and token come from the program's options.</summary>
	public static class McpClient
	{
		/// <summary>Speak MCP over standard input and output, carried to the running program.</summary>
		public const string McpArgument = "Mcp";
		/// <summary>Call one tool of the running program and print the answer; alone, print the tools.</summary>
		public const string AiArgument = "Ai";

		static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

		[System.Runtime.InteropServices.DllImport("kernel32.dll")]
		static extern bool AttachConsole(int processId);

		[System.Runtime.InteropServices.DllImport("kernel32.dll")]
		static extern IntPtr GetStdHandle(int handle);

		/// <summary>True when the command line asks for one of the two switches that talk to the running program.</summary>
		public static bool IsSwitch(System.Collections.Specialized.StringDictionary parameters)
		{
			return parameters.ContainsKey(McpArgument) || parameters.ContainsKey(AiArgument);
		}

		/// <summary>The two switches that talk to the running program on a caller's behalf.</summary>
		/// <remarks>
		/// The program is a windowed executable, so a person typing the command gets no console and no
		/// standard output handle. When there is no handle, the parent console is attached, so /Ai
		/// prints where it was typed; an assistant's pipe is a real handle and is used as it is.
		/// Both streams are UTF-8, so a device name reaches the caller as written. Failures are
		/// printed and become exit codes, because an exception here would open the crash reporter
		/// for what is a script's mistake.
		/// </remarks>
		/// <param name="parameters">The command line, as the install context reads it.</param>
		/// <param name="enabled">Whether AI assistant access is switched on in the program's options.</param>
		public static int RunSwitches(System.Collections.Specialized.StringDictionary parameters, bool enabled, int port, string token, string exePath)
		{
			var stdout = GetStdHandle(-11);
			if (stdout == IntPtr.Zero || stdout == new IntPtr(-1))
				AttachConsole(-1);
			var output = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true };
			var error = new StreamWriter(Console.OpenStandardError(), new UTF8Encoding(false)) { AutoFlush = true };
			if (parameters.ContainsKey(AiArgument) && string.IsNullOrEmpty(parameters[AiArgument]))
			{
				output.Write(Usage());
				return 0;
			}
			if (!enabled)
			{
				error.WriteLine("AI assistant access is off. Switch it on in x360ce Options.");
				return 2;
			}
			try
			{
				EnsureRunning(port, token, exePath);
				Func<string, string> post = body => Post(port, token, body);
				if (parameters.ContainsKey(McpArgument))
					return RunStdio(new StreamReader(Console.OpenStandardInput(), Encoding.UTF8), output, post);
				var arguments = parameters.Keys.Cast<string>()
					.Where(k => k != AiArgument.ToLowerInvariant())
					.ToDictionary(k => k, k => parameters[k]);
				return RunCommand(parameters[AiArgument], arguments, output, post);
			}
			catch (Exception ex)
			{
				error.WriteLine(ex.Message);
				return 1;
			}
		}

		public static string Post(int port, string token, string body)
		{
			// The same host the listener binds: http.sys routes by the Host header.
			var request = (HttpWebRequest)WebRequest.Create("http://localhost:" + port + "/mcp/");
			// Straight to this machine. A system proxy that does not bypass local addresses would
			// otherwise carry a loopback call out and back, or nowhere.
			request.Proxy = null;
			// A call lasts as long as the action: a button that opens a window answers when it closes.
			request.Timeout = System.Threading.Timeout.Infinite;
			request.ReadWriteTimeout = System.Threading.Timeout.Infinite;
			request.Method = "POST";
			request.ContentType = "application/json; charset=utf-8";
			request.Headers["Authorization"] = "Bearer " + token;
			var bytes = Encoding.UTF8.GetBytes(body);
			request.ContentLength = bytes.Length;
			using (var stream = request.GetRequestStream())
				stream.Write(bytes, 0, bytes.Length);
			using (var response = (HttpWebResponse)request.GetResponse())
			using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
				return reader.ReadToEnd();
		}
		/// <summary>
		/// Starts the program when nothing answers on the port, and waits up to 60 seconds for it:
		/// the door opens only after the start-up checks have run, which a slow machine stretches.
		/// A copy that is running and silent is reported, not doubled: the program allows one copy,
		/// so a second start would only bring the first to the front and exit.
		/// </summary>
		public static void EnsureRunning(int port, string token, string exePath)
		{
			if (Answers(port, token))
				return;
			// The switch is the same executable, so its own process is not the copy being looked for.
			var name = Path.GetFileNameWithoutExtension(exePath);
			var self = Process.GetCurrentProcess().Id;
			if (Process.GetProcessesByName(name).Any(p => p.Id != self))
				throw new InvalidOperationException("x360ce is running but does not answer on port " + port + ". Open its window to see why.");
			Process.Start(new ProcessStartInfo(exePath) { WorkingDirectory = Path.GetDirectoryName(exePath) });
			var until = DateTime.Now.AddSeconds(60);
			while (DateTime.Now < until)
			{
				Thread.Sleep(500);
				if (Answers(port, token))
					return;
			}
			throw new InvalidOperationException("x360ce did not answer on port " + port + " within 60 seconds. Open its window to see why.");
		}

		/// <summary>What an assistant's MCP settings need to start this program as a server: its name, the program and the switch.</summary>
		public static string ServerSettings(string exePath)
		{
			var server = new Dictionary<string, object> { { "command", exePath }, { "args", new[] { "/" + McpArgument } } };
			return Json.Serialize(new Dictionary<string, object> { { "mcpServers", new Dictionary<string, object> { { McpServer.ServerName, server } } } });
		}

		static bool Answers(int port, string token)
		{
			try { return Post(port, token, "{\"jsonrpc\":\"2.0\",\"id\":0,\"method\":\"ping\"}").Contains("result"); }
			catch (WebException) { return false; }
		}

		/// <summary>MCP over standard streams: one message per line in, one answer per line out, each carried to the running program.</summary>
		public static int RunStdio(TextReader input, TextWriter output, Func<string, string> post)
		{
			string line;
			while ((line = input.ReadLine()) != null)
			{
				if (line.Trim().Length == 0)
					continue;
				string answer;
				try { answer = post(line); }
				catch (WebException ex)
				{
					// One failed carry is one error line; the assistant decides what to do, and the
					// bridge stays up for the next message.
					object id = null;
					try { (Json.DeserializeObject(line) as Dictionary<string, object>)?.TryGetValue("id", out id); } catch (Exception) { }
					answer = Json.Serialize(new Dictionary<string, object> { { "jsonrpc", "2.0" }, { "id", id }, { "error", new Dictionary<string, object> { { "code", -32603 }, { "message", "x360ce did not answer: " + ex.Message } } } });
				}
				if (!string.IsNullOrEmpty(answer))
				{
					output.WriteLine(answer);
					output.Flush();
				}
			}
			return 0;
		}

		/// <summary>One tool call for a script: prints the answer, returns 0, or prints the failure and returns 1.</summary>
		public static int RunCommand(string tool, IDictionary<string, string> arguments, TextWriter output, Func<string, string> post)
		{
			var request = Json.Serialize(new Dictionary<string, object>
			{
				{ "jsonrpc", "2.0" }, { "id", 1 }, { "method", "tools/call" },
				{ "params", new Dictionary<string, object> { { "name", tool }, { "arguments", arguments.ToDictionary(a => a.Key, a => (object)a.Value) } } },
			});
			var answer = (Dictionary<string, object>)Json.DeserializeObject(post(request));
			object error;
			if (answer.TryGetValue("error", out error) && error != null)
			{
				output.WriteLine(((Dictionary<string, object>)error)["message"]);
				return 1;
			}
			var result = (Dictionary<string, object>)answer["result"];
			var content = (object[])result["content"];
			output.WriteLine(((Dictionary<string, object>)content[0])["text"]);
			object isError;
			return result.TryGetValue("isError", out isError) && true.Equals(isError) ? 1 : 0;
		}

		/// <summary>Every catalogued tool with its level and arguments, from the same schema the assistant is given.</summary>
		public static string Usage()
		{
			var sb = new StringBuilder();
			sb.AppendLine("x360ce.exe /Ai=<tool> [/<argument>=<value> ...]   calls one tool of the running program");
			sb.AppendLine("x360ce.exe /Mcp                                  speaks MCP over stdio for an assistant");
			sb.AppendLine("The level in brackets is the AI assistant access the tool needs, chosen on the Options page.");
			sb.AppendLine("From a batch file that needs the exit code, run: start /wait x360ce.exe /Ai=...");
			sb.AppendLine();
			foreach (var tool in McpCatalog.Tools)
			{
				sb.AppendLine(tool.Name + "  (" + tool.Level + ")  " + tool.Description);
				var schema = tool.InputSchema();
				object requiredValue;
				var required = schema.TryGetValue("required", out requiredValue) ? (string[])requiredValue : new string[0];
				foreach (var property in (Dictionary<string, object>)schema["properties"])
				{
					object description;
					((Dictionary<string, object>)property.Value).TryGetValue("description", out description);
					sb.AppendLine("  /" + property.Key + "=" + (required.Contains(property.Key) ? "" : "[optional] ") + (description ?? ""));
				}
			}
			return sb.ToString();
		}
	}
}
