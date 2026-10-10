#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace JocysCom.ClassLibrary.Mcp
{
	/// <summary>A call the door refused: the HTTP status and what it said.</summary>
	public sealed class McpHttpException : HttpRequestException
	{
		public McpHttpException(int status, string body) : base("The door answered " + status + ": " + body)
		{
			Status = status;
			Body = body;
		}

		public readonly int Status;
		public readonly string Body;
	}

	/// <summary>The client side of the program's own door. Port and token come from the program's settings.</summary>
	public static class McpClient
	{
		/// <summary>Speak MCP over standard input and output, carried to the running program.</summary>
		public const string McpArgument = "Mcp";
		/// <summary>Call one tool of the running program and print the answer; alone, print the tools.</summary>
		public const string AiArgument = "Ai";

		/// <summary>Arguments that pick how the program runs rather than what a tool is given. None unless the program says.</summary>
		public static string[] IgnoredArguments = new string[0];

		/// <summary>What a switch starts with where the program writes one out, in its usage and in an assistant's settings: '-', or '/' where the program's own documents use that.</summary>
		public static string SwitchPrefix = "-";

		/// <summary>
		/// Straight to this machine: a system proxy that does not bypass local addresses would carry a
		/// loopback call out and back, or nowhere. A call lasts as long as the action, and some take a while.
		/// </summary>
		static readonly HttpClient Http = new HttpClient(new HttpClientHandler { UseProxy = false }) { Timeout = Timeout.InfiniteTimeSpan };

		/// <summary>Straight to this machine as well, for the ping that asks whether the program is there: a program that holds the port and never answers does not keep the caller waiting.</summary>
		static readonly HttpClient Ping = new HttpClient(new HttpClientHandler { UseProxy = false }) { Timeout = TimeSpan.FromSeconds(5) };

		[System.Runtime.InteropServices.DllImport("kernel32.dll")]
		static extern bool AttachConsole(int processId);

		[System.Runtime.InteropServices.DllImport("kernel32.dll")]
		static extern IntPtr GetStdHandle(int handle);

		/// <summary>True when the command line asks for one of the two switches that talk to the running program. Read without regard to case.</summary>
		public static bool IsSwitch(IDictionary<string, string> parameters)
		{
			return parameters.ContainsKey(McpArgument) || parameters.ContainsKey(AiArgument);
		}

		/// <summary>Standard output and error for a switch that answers the caller and stops.</summary>
		/// <remarks>
		/// The program is a windowed executable, so a person typing the command gets no console and no
		/// standard output handle. When there is no handle, the parent console is attached, so the
		/// answer is printed where it was typed; an assistant's pipe is a real handle and is used as it
		/// is. Both streams are UTF-8, so a device name reaches the caller as written.
		/// </remarks>
		public static void OpenConsole(out TextWriter output, out TextWriter error)
		{
			var stdout = GetStdHandle(-11);
			if (stdout == IntPtr.Zero || stdout == new IntPtr(-1))
				AttachConsole(-1);
			output = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true };
			error = new StreamWriter(Console.OpenStandardError(), new UTF8Encoding(false)) { AutoFlush = true };
		}

		/// <summary>The two switches that talk to the running program on a caller's behalf.</summary>
		/// <remarks>
		/// Failures are printed and become exit codes, because an exception here would be a crash
		/// report for what is a script's mistake.
		/// </remarks>
		/// <param name="parameters">The command line, read without regard to case.</param>
		/// <param name="enabled">Whether AI assistant access is switched on in the program's settings.</param>
		public static int RunSwitches(IDictionary<string, string> parameters, bool enabled, int port, string token, string exePath)
		{
			TextWriter output, error;
			OpenConsole(out output, out error);
			if (parameters.ContainsKey(AiArgument) && string.IsNullOrEmpty(parameters[AiArgument]))
			{
				output.Write(Usage());
				return 0;
			}
			if (!enabled)
			{
				error.WriteLine("AI assistant access is off. The person opens the door " + McpCatalog.SettingsPlace + ".");
				return 2;
			}
			try
			{
				EnsureRunning(port, token, exePath);
				Func<string, string> post = body => Post(port, token, body);
				if (parameters.ContainsKey(McpArgument))
					return RunStdio(new StreamReader(Console.OpenStandardInput(), Encoding.UTF8), output, post);
				// Arguments that pick how the program runs are not the tool's.
				var arguments = parameters
					.Where(p => !string.Equals(p.Key, AiArgument, StringComparison.OrdinalIgnoreCase)
						&& !IgnoredArguments.Contains(p.Key, StringComparer.OrdinalIgnoreCase))
					.ToDictionary(p => p.Key, p => p.Value);
				return RunCommand(parameters[AiArgument], arguments, output, post);
			}
			catch (Exception ex)
			{
				error.WriteLine(ex.Message);
				return 1;
			}
		}

		/// <summary>Posts one JSON-RPC message to the door and returns its answer. A refusal throws McpHttpException.</summary>
		public static string Post(int port, string token, string body)
		{
			return Post(Http, port, token, body);
		}

		static string Post(HttpClient http, int port, string token, string body)
		{
			// The same host the listener binds: http.sys routes by the Host header.
			using (var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:" + port + "/mcp/"))
			{
				request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);
				request.Content = new StringContent(body, Encoding.UTF8, "application/json");
				using (var response = http.SendAsync(request).GetAwaiter().GetResult())
				{
					var text = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
					if (!response.IsSuccessStatusCode)
						throw new McpHttpException((int)response.StatusCode, text);
					return text;
				}
			}
		}

		/// <summary>
		/// Starts the program when nothing answers on the port, and waits up to 60 seconds for it: the
		/// door opens only after start-up, which a slow machine stretches. A copy that is running and
		/// silent is reported, not doubled.
		/// </summary>
		public static void EnsureRunning(int port, string token, string exePath)
		{
			if (Answers(port, token))
				return;
			// The switch is the same executable, so its own process is not the copy being looked for.
			var name = Path.GetFileNameWithoutExtension(exePath);
			var self = Process.GetCurrentProcess().Id;
			// The person looks at the program, not the caller: its door may be off, or open on another port.
			var why = ": its AI assistant access may be off or use another port. The person checks it " + McpCatalog.SettingsPlace + ".";
			if (Process.GetProcessesByName(name).Any(p => p.Id != self))
				throw new InvalidOperationException(name + " is running but does not answer on port " + port + why);
			// Started by the shell, so the program does not inherit the caller's output: a caller that reads
			// the switch's output to its end would otherwise wait until the program exits.
			Process.Start(new ProcessStartInfo(exePath) { WorkingDirectory = Path.GetDirectoryName(exePath), UseShellExecute = true });
			var until = DateTime.Now.AddSeconds(60);
			while (DateTime.Now < until)
			{
				Thread.Sleep(500);
				if (Answers(port, token))
					return;
			}
			throw new InvalidOperationException(name + " did not answer on port " + port + " within 60 seconds" + why);
		}

		/// <summary>What an assistant's MCP settings need to start this program as a server: its name, the program and the switch.</summary>
		public static string ServerSettings(string exePath)
		{
			var server = new Dictionary<string, object> { { "command", exePath }, { "args", new[] { SwitchPrefix + McpArgument } } };
			return McpServer.ToJson(new Dictionary<string, object> { { "mcpServers", new Dictionary<string, object> { { McpServer.ServerName, server } } } });
		}

		static bool Answers(int port, string token)
		{
			try { return Post(Ping, port, token, "{\"jsonrpc\":\"2.0\",\"id\":0,\"method\":\"ping\"}").Contains("result"); }
			catch (HttpRequestException) { return false; }
			// The ping's time ran out: something holds the port and does not answer.
			catch (OperationCanceledException) { return false; }
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
				catch (HttpRequestException ex)
				{
					// One failed carry is one error line; the assistant decides what to do, and the
					// bridge stays up for the next message.
					object id = null;
					try { (McpServer.FromJson(line) as Dictionary<string, object>)?.TryGetValue("id", out id); } catch (Exception) { }
					answer = McpServer.ToJson(new Dictionary<string, object> { { "jsonrpc", "2.0" }, { "id", id }, { "error", new Dictionary<string, object> { { "code", -32603 }, { "message", "The program did not answer: " + ex.Message } } } });
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
			var request = McpServer.ToJson(new Dictionary<string, object>
			{
				{ "jsonrpc", "2.0" }, { "id", 1 }, { "method", "tools/call" },
				{ "params", new Dictionary<string, object> { { "name", tool }, { "arguments", arguments.ToDictionary(a => a.Key, a => (object)a.Value) } } },
			});
			var answer = (Dictionary<string, object>)McpServer.FromJson(post(request));
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
			var exe = Process.GetCurrentProcess().ProcessName + ".exe";
			var prefix = SwitchPrefix;
			var sb = new StringBuilder();
			sb.AppendLine(exe + " " + prefix + AiArgument + "=<tool> [" + prefix + "<argument>=<value> ...]   calls one tool of the running program");
			sb.AppendLine(exe + " " + prefix + McpArgument + "                                  speaks MCP over stdio for an assistant");
			sb.AppendLine("The level in brackets is the AI assistant access the tool needs, chosen " + McpCatalog.SettingsPlace + ".");
			sb.AppendLine("From a batch file that needs the exit code, run: start /wait " + exe + " " + prefix + AiArgument + "=...");
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
					sb.AppendLine("  " + prefix + property.Key + "=" + (required.Contains(property.Key) ? "" : "[optional] ") + (description ?? ""));
				}
			}
			return sb.ToString();
		}
	}
}
