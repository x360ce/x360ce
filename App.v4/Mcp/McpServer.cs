using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Web.Script.Serialization;

namespace x360ce.App.Mcp
{
	/// <summary>Marks a public static method as a tool an assistant or a script may call, at the given level or above.</summary>
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class McpToolAttribute : Attribute
	{
		public McpToolAttribute(AiAccess level, string description) { Level = level; Description = description; }
		public readonly AiAccess Level;
		public readonly string Description;
		/// <summary>False for a tool that waits, so the window keeps drawing while it does.</summary>
		public bool OnUiThread = true;
	}

	/// <summary>One tool as the catalogue knows it: read once from the method, used by every door.</summary>
	public sealed class McpToolInfo
	{
		public string Name;
		public string Description;
		public AiAccess Level;
		public bool OnUiThread;
		public MethodInfo Method;
		public ParameterInfo[] Parameters;

		/// <summary>JSON Schema for the arguments, from the parameters: string, integer or boolean; required unless defaulted. The one reading of the parameters, which the command-line usage walks too.</summary>
		public Dictionary<string, object> InputSchema()
		{
			var properties = new Dictionary<string, object>();
			var required = new List<string>();
			foreach (var p in Parameters)
			{
				var property = new Dictionary<string, object> { { "type", p.ParameterType == typeof(int) ? "integer" : p.ParameterType == typeof(bool) ? "boolean" : "string" } };
				var description = p.GetCustomAttribute<DescriptionAttribute>();
				if (description != null)
					property["description"] = description.Description;
				properties[p.Name] = property;
				if (!p.HasDefaultValue)
					required.Add(p.Name);
			}
			var schema = new Dictionary<string, object> { { "type", "object" }, { "properties", properties } };
			if (required.Count > 0)
				schema["required"] = required.ToArray();
			return schema;
		}

		/// <summary>Turns arguments into parameter values by name, whatever their case. What does not bind is an ArgumentException.</summary>
		public object[] Bind(Dictionary<string, object> arguments)
		{
			var byName = new Dictionary<string, object>(arguments ?? new Dictionary<string, object>(), StringComparer.OrdinalIgnoreCase);
			var values = new object[Parameters.Length];
			for (var i = 0; i < Parameters.Length; i++)
			{
				var p = Parameters[i];
				object raw;
				if (byName.TryGetValue(p.Name, out raw) && raw != null)
				{
					try { values[i] = Convert.ChangeType(raw, p.ParameterType, System.Globalization.CultureInfo.InvariantCulture); }
					catch (Exception ex) { throw new ArgumentException("Argument " + p.Name + ": " + ex.Message); }
				}
				else if (p.HasDefaultValue)
					values[i] = p.DefaultValue;
				else
					throw new ArgumentException("Missing argument: " + p.Name + ".");
			}
			return values;
		}

		/// <summary>Calls the method, on the interface thread unless the tool said otherwise. Whatever the method throws comes out as itself.</summary>
		public object Call(object[] values)
		{
			object result = null;
			Action run = () =>
			{
				try { result = Method.Invoke(null, values); }
				catch (TargetInvocationException ex) { ExceptionDispatchInfo.Capture(ex.InnerException).Throw(); }
			};
			if (OnUiThread)
				McpCatalog.OnUiThread(run);
			else
				run();
			return result;
		}
	}

	/// <summary>
	/// The tools, read once from the attributed methods of one class, and the two things every
	/// tool needs: which level the program is set to, and a way onto the interface thread.
	/// </summary>
	public static class McpCatalog
	{
		static List<McpToolInfo> _tools;

		public static List<McpToolInfo> Tools { get { if (_tools == null) Load(typeof(McpTools)); return _tools; } }

		/// <summary>The level the program is set to. Read from the option; a test may point it elsewhere.</summary>
		public static Func<AiAccess> Level = () => SettingsManager.Options.AiAccess;

		/// <summary>Runs an action on the interface thread. A test running there already may make it a plain call.</summary>
		public static Action<Action> OnUiThread = Marshal;

		/// <summary>
		/// ControlsHelper.Invoke runs the action elsewhere and never looks at what became of it,
		/// so a failure inside would be lost and the caller told nothing. Caught inside, thrown outside.
		/// </summary>
		public static void Marshal(Action action)
		{
			ExceptionDispatchInfo failure = null;
			JocysCom.ClassLibrary.Controls.ControlsHelper.Invoke(() =>
			{
				try { action(); }
				catch (Exception ex) { failure = ExceptionDispatchInfo.Capture(ex); }
			});
			if (failure != null)
				failure.Throw();
		}

		public static void Load(Type source)
		{
			_tools = source.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.Select(m => new { Method = m, Tool = m.GetCustomAttribute<McpToolAttribute>() })
				.Where(x => x.Tool != null)
				.Select(x => new McpToolInfo { Name = ToolName(x.Method.Name), Description = x.Tool.Description, Level = x.Tool.Level, OnUiThread = x.Tool.OnUiThread, Method = x.Method, Parameters = x.Method.GetParameters() })
				.OrderBy(t => t.Level).ThenBy(t => t.Name)
				.ToList();
		}

		/// <summary>UiSet becomes ui_set: the wire name from the method name, so there is one name to keep.</summary>
		public static string ToolName(string methodName)
		{
			var sb = new StringBuilder();
			for (var i = 0; i < methodName.Length; i++)
			{
				if (char.IsUpper(methodName[i]) && i > 0)
					sb.Append('_');
				sb.Append(char.ToLowerInvariant(methodName[i]));
			}
			return sb.ToString();
		}

		/// <summary>The one sentence a caller reads when the level is too low.</summary>
		public static string Refusal(AiAccess needed)
		{
			return "This needs " + needed + " access; the program is set to " + Level() + ". Change it on the Options page.";
		}
	}

	/// <summary>
	/// Answers Model Context Protocol requests. A request in, a response out; the transport and
	/// the two switches wrap this and add nothing.
	/// </summary>
	public static class McpServer
	{
		public const string ProtocolVersion = "2025-06-18";

		static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

		/// <summary>Handles one JSON-RPC message. Returns the response, or an empty string for a notification.</summary>
		public static string Handle(string requestJson)
		{
			Dictionary<string, object> request;
			try { request = Json.DeserializeObject(requestJson) as Dictionary<string, object>; }
			catch (Exception ex) { return Error(null, -32700, "Parse error: " + ex.Message); }
			if (request == null)
				return Error(null, -32600, "Not a request object.");
			object id;
			request.TryGetValue("id", out id);
			var method = request.ContainsKey("method") ? request["method"] as string : null;
			var p = request.ContainsKey("params") ? request["params"] as Dictionary<string, object> : null;
			if (method == null)
				return Error(id, -32600, "No method.");
			if (method.StartsWith("notifications/", StringComparison.Ordinal))
				return "";
			switch (method)
			{
				case "initialize":
					return Result(id, InitializeResult());
				case "ping":
					return Result(id, new Dictionary<string, object>());
				case "tools/list":
					return Result(id, ToolsList());
				case "tools/call":
					return CallTool(id, p, McpCatalog.Level());
				default:
					return Error(id, -32601, "Unknown method: " + method);
			}
		}

		/// <summary>What initialize answers. Also written into the Windows registration, which checks the two agree.</summary>
		public static Dictionary<string, object> InitializeResult()
		{
			return new Dictionary<string, object>
			{
				{ "protocolVersion", ProtocolVersion },
				{ "capabilities", new Dictionary<string, object> { { "tools", new Dictionary<string, object>() } } },
				{ "serverInfo", new Dictionary<string, object> { { "name", "x360ce" }, { "version", System.Windows.Forms.Application.ProductVersion } } },
			};
		}

		/// <summary>
		/// Every tool, whatever the level: the list never changes, which the Windows registration
		/// requires, and a tool above the level says so when called.
		/// </summary>
		public static Dictionary<string, object> ToolsList()
		{
			return new Dictionary<string, object> { { "tools", McpCatalog.Tools.Select(t => (object)new Dictionary<string, object> { { "name", t.Name }, { "description", t.Description }, { "inputSchema", t.InputSchema() } }).ToArray() } };
		}

		static string CallTool(object id, Dictionary<string, object> p, AiAccess level)
		{
			var name = p != null && p.ContainsKey("name") ? p["name"] as string : null;
			var arguments = p != null && p.ContainsKey("arguments") ? p["arguments"] as Dictionary<string, object> : null;
			var shown = name + " " + McpLog.Clip(Redact(name, arguments == null ? "" : Json.Serialize(arguments)), 300);
			var tool = McpCatalog.Tools.FirstOrDefault(t => t.Name == name);
			if (tool == null)
			{
				McpLog.Write(shown + " -> no such tool");
				return Error(id, -32602, "No such tool: " + name);
			}
			if (tool.Level > level)
			{
				McpLog.Write(shown + " -> refused, needs " + tool.Level + " and the level is " + level);
				return Error(id, -32001, McpCatalog.Refusal(tool.Level));
			}
			object[] values;
			try { values = tool.Bind(arguments); }
			catch (ArgumentException ex)
			{
				McpLog.Write(shown + " -> " + ex.Message);
				return Error(id, -32602, ex.Message);
			}
			var watch = System.Diagnostics.Stopwatch.StartNew();
			object result;
			try { result = tool.Call(values); }
			catch (Exception ex)
			{
				McpLog.Write(shown + " -> failed after " + watch.ElapsedMilliseconds + " ms: " + McpLog.Clip(ex.Message, 300));
				return Result(id, Content(ex.Message, true));
			}
			var text = result == null ? "Done." : result as string ?? Json.Serialize(result);
			McpLog.Write(shown + " -> " + McpLog.Clip(text, 200) + " in " + watch.ElapsedMilliseconds + " ms");
			return Result(id, Content(text, false));
		}

		/// <summary>
		/// A password typed through the door must not be readable in the log afterwards. The reader
		/// already refuses to read a password box out; the writer's argument is masked the same way.
		/// </summary>
		static string Redact(string tool, string argumentsJson)
		{
			if (argumentsJson.IndexOf("password", StringComparison.OrdinalIgnoreCase) < 0)
				return argumentsJson;
			if (tool == "ui_set")
				return System.Text.RegularExpressions.Regex.Replace(argumentsJson, "\"value\":\"[^\"]*\"", "\"value\":\"***\"");
			if (tool == "ui_script")
				return System.Text.RegularExpressions.Regex.Replace(argumentsJson, "(set [^|\\]*password[^|\\]*\\|)[^\\]*", "$1 ***", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
			return argumentsJson;
		}

		static Dictionary<string, object> Content(string text, bool isError)
		{
			var content = new Dictionary<string, object> { { "content", new object[] { new Dictionary<string, object> { { "type", "text" }, { "text", text } } } } };
			if (isError)
				content["isError"] = true;
			return content;
		}

		static string Result(object id, object result)
		{
			return Json.Serialize(new Dictionary<string, object> { { "jsonrpc", "2.0" }, { "id", id }, { "result", result } });
		}

		/// <summary>A JSON-RPC error envelope. Internal so the transport answers a fault in the same shape.</summary>
		internal static string Error(object id, int code, string message)
		{
			return Json.Serialize(new Dictionary<string, object> { { "jsonrpc", "2.0" }, { "id", id }, { "error", new Dictionary<string, object> { { "code", code }, { "message", message } } } });
		}
	}

	/// <summary>Serves McpServer over HTTP on the loopback address, to callers that present the token.</summary>
	public static class McpListener
	{
		static System.Net.HttpListener _listener;
		static string _token;

		public static bool IsRunning { get { return _listener != null; } }

		/// <summary>Why the door is not usable, as one sentence with its own remedy, for the Issues tab. Null after a start that worked.</summary>
		public static string LastError;

		/// <summary>True when the last start failed only because Windows has no URL reservation for every network; the Issues tab can make one.</summary>
		public static bool NeedsUrlReservation;

		/// <summary>The prefix http.sys is asked for: the loopback name a standard user may bind, or every address, which needs a reservation.</summary>
		public static string Prefix(string address, int port)
		{
			return (address == Options.AnyAddress ? "http://+:" : "http://localhost:") + port + "/mcp/";
		}

		/// <summary>Opens the door on the address and port. False, with the reason in LastError, when it cannot.</summary>
		public static bool Start(string address, int port, string token)
		{
			Stop();
			NeedsUrlReservation = false;
			if (port < 1024 || port > 49151)
			{
				LastError = "port " + port + " is outside 1024 to 49151. Choose a port in that range on the Options page.";
				return false;
			}
			var listener = new System.Net.HttpListener();
			// localhost is the host a standard user may bind without a URL reservation, where
			// 127.0.0.1 is refused on some Windows versions, and it stays on this machine. Every
			// network is the + wildcard, the only form http.sys takes for that, and it needs a
			// reservation made once as Administrator.
			listener.Prefixes.Add(Prefix(address, port));
			try
			{
				listener.Start();
			}
			catch (System.Net.HttpListenerException ex)
			{
				// Another program holds the port, or Windows has no reservation. The Issues tab says
				// which and what to do; a crash report would say neither.
				NeedsUrlReservation = address == Options.AnyAddress && ex.ErrorCode == 5;
				LastError = NeedsUrlReservation
					? "listening on every network needs a one-time permission from Windows. Press Fix on the Issues tab, or run as Administrator: netsh http add urlacl url=" + Prefix(address, port) + " sddl=D:(A;;GX;;;WD)"
					: "port " + port + " could not be opened (" + ex.Message + "). Choose another port on the Options page.";
				return false;
			}
			_token = token;
			_listener = listener;
			LastError = null;
			listener.BeginGetContext(OnRequest, listener);
			McpLog.Write("door opened at " + Prefix(address, port) + " with " + McpCatalog.Level() + " access");
			return true;
		}

		public static void Stop()
		{
			var listener = _listener;
			_listener = null;
			if (listener != null)
			{
				listener.Close();
				McpLog.Write("door closed");
			}
		}

		/// <summary>
		/// Runs on a pool thread, which the crash reporter does not watch. Everything is caught and
		/// answered, so a broken client, a fault in a tool, or a stop that lands mid-request ends in
		/// a response or in silence rather than in the program.
		/// </summary>
		static void OnRequest(IAsyncResult ar)
		{
			var listener = (System.Net.HttpListener)ar.AsyncState;
			System.Net.HttpListenerContext context;
			try
			{
				context = listener.EndGetContext(ar);
				listener.BeginGetContext(OnRequest, listener);
			}
			catch (ObjectDisposedException) { return; }
			catch (System.Net.HttpListenerException) { return; }
			try
			{
				Answer(context);
			}
			catch (Exception ex)
			{
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteLog("AI assistant access request failed: " + ex.Message, System.Diagnostics.EventLogEntryType.Warning);
				try { Write(context.Response, 500, McpServer.Error(null, -32603, "Internal error.")); }
				catch (Exception) { }
			}
		}

		static void Answer(System.Net.HttpListenerContext context)
		{
			var authorised = (context.Request.Headers["Authorization"] ?? "") == "Bearer " + _token;
			if (!authorised || context.Request.HttpMethod != "POST")
			{
				if (!authorised)
					McpLog.Write("refused: no valid token, from " + context.Request.RemoteEndPoint);
				Write(context.Response, authorised ? 405 : 401, "");
				return;
			}
			string body;
			// JSON is UTF-8 whatever the request says; a missing charset would otherwise mean the system code page.
			using (var reader = new System.IO.StreamReader(context.Request.InputStream, Encoding.UTF8))
				body = reader.ReadToEnd();
			var answer = McpServer.Handle(body);
			Write(context.Response, answer.Length == 0 ? 202 : 200, answer);
		}

		static void Write(System.Net.HttpListenerResponse response, int status, string body)
		{
			var bytes = Encoding.UTF8.GetBytes(body);
			response.StatusCode = status;
			response.ContentType = "application/json; charset=utf-8";
			response.ContentLength64 = bytes.Length;
			response.OutputStream.Write(bytes, 0, bytes.Length);
			response.Close();
		}
	}
}
