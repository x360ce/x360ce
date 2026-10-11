#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
#if NETFRAMEWORK
using System.Web.Script.Serialization;
#else
using System.Text.Encodings.Web;
using System.Text.Json;
#endif
using System.Threading;
using JocysCom.ClassLibrary.Controls;

namespace JocysCom.ClassLibrary.Mcp
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
		/// <summary>True for a tool of the Read level that can still change things when the level allows, such as a script.</summary>
		public bool Changes;
	}

	/// <summary>One tool as the catalogue knows it: read once from the method, used by every door.</summary>
	public sealed class McpToolInfo
	{
		public string Name;
		public string Description;
		public AiAccess Level;
		public bool OnUiThread;
		public bool Changes;
		public MethodInfo Method;
		public ParameterInfo[] Parameters;

		/// <summary>True for a tool that changes nothing: one of the Read level that cannot change things at any level.</summary>
		public bool ReadOnly { get { return Level == AiAccess.Read && !Changes; } }

		/// <summary>
		/// The hints a client shows the person before a call: whether the tool changes anything, and whether a change
		/// replaces what was set. Neither leaves the computer.
		/// </summary>
		public Dictionary<string, object> Annotations()
		{
			return new Dictionary<string, object>
			{
				{ "readOnlyHint", ReadOnly },
				{ "destructiveHint", !ReadOnly },
				{ "openWorldHint", false },
			};
		}

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
			// An argument the tool does not take is refused rather than dropped: dropped, a misspelt or invented
			// one looks as if it worked, and the caller acts on an answer to a question it did not ask.
			var unknown = byName.Keys.Where(k => !Parameters.Any(p => string.Equals(p.Name, k, StringComparison.OrdinalIgnoreCase))).ToArray();
			if (unknown.Length > 0)
				throw new ArgumentException("Unknown argument: " + string.Join(", ", unknown) + ". " + Name + " takes " +
					(Parameters.Length == 0 ? "no arguments" : string.Join(", ", Parameters.Select(p => p.Name))) + ".");
			var values = new object[Parameters.Length];
			for (var i = 0; i < Parameters.Length; i++)
			{
				var p = Parameters[i];
				object raw;
				if (byName.TryGetValue(p.Name, out raw) && raw != null)
				{
					try { values[i] = System.Convert.ChangeType(raw, p.ParameterType, System.Globalization.CultureInfo.InvariantCulture); }
					catch (Exception ex) { throw new ArgumentException("Argument " + p.Name + ": " + ex.Message); }
				}
				else if (p.HasDefaultValue)
					values[i] = p.DefaultValue;
				else
					throw new ArgumentException("Missing argument: " + p.Name + ".");
			}
			return values;
		}

		/// <summary>
		/// Calls the method, on the interface thread unless the tool said otherwise. Whatever the method throws comes out as
		/// itself, except a window left waiting for an answer, which is the answer.
		/// </summary>
		public object Call(object[] values)
		{
			object result = null;
			Action run = () =>
			{
				try { result = Method.Invoke(null, values); }
				catch (TargetInvocationException ex) { ExceptionDispatchInfo.Capture(ex.InnerException).Throw(); }
			};
			try
			{
				if (OnUiThread)
					McpCatalog.OnUiThread(run);
				else
					run();
			}
			catch (WindowWaitingException ex)
			{
				return ex.Message;
			}
			return result;
		}
	}

	/// <summary>
	/// A call that is still running because a window it opened waits for an answer. The message names the window and
	/// the buttons that answer it, so the caller answers it like any other window.
	/// </summary>
	public sealed class WindowWaitingException : Exception
	{
		public WindowWaitingException(string message) : base(message) { }
	}

	/// <summary>
	/// The tools, read once from the attributed methods of the program's tool classes, and the two
	/// things every tool needs: which level the program is set to, and a way onto the interface thread.
	/// </summary>
	public static class McpCatalog
	{
		static List<McpToolInfo> _tools;

		/// <summary>The classes whose attributed methods are the tools: the shared interface tools, and the program's own.</summary>
		public static Type[] Sources = { typeof(McpUiTools) };

		public static List<McpToolInfo> Tools { get { if (_tools == null) Load(Sources); return _tools; } }

		/// <summary>The level the program is set to. The program points it at its option; a test may point it elsewhere.</summary>
		public static Func<AiAccess> Level = () => AiAccess.Read;

		/// <summary>Where the person chooses the level, as the end of a sentence, such as "on the AI tab". Each refusal names it, so the caller can say where to look.</summary>
		public static string SettingsPlace = "in the program's AI assistant access settings";

		/// <summary>Runs an action on the interface thread. A test running there already may make it a plain call.</summary>
		public static Action<Action> OnUiThread = Marshal;

		/// <summary>
		/// Runs an action on the interface thread and waits until it is done, or until it waits on a window: a press
		/// that opens a dialog answers with the dialog, as <see cref="WindowWaitingException"/>, rather than holding the
		/// caller until a person closes it. The helper runs the action elsewhere and never looks at what became of it,
		/// so a failure inside is caught there and thrown here.
		/// </summary>
		public static void Marshal(Action action)
		{
			if (!ControlsHelper.InvokeRequired)
			{
				action();
				return;
			}
			ExceptionDispatchInfo failure = null;
			string waiting = null;
			var finished = false;
			var settled = new ManualResetEventSlim();
			ControlsHelper.BeginInvoke(() =>
			{
				try { action(); }
				// The caller told of the window has gone, so a later failure goes to the program's error
				// handler, as one after a person's click does.
				catch (Exception ex) when (waiting == null) { failure = ExceptionDispatchInfo.Capture(ex); }
				finally
				{
					finished = true;
					settled.Set();
				}
			});
			// Queued behind the action, the look runs once it is done, or while it waits inside a window's own
			// message loop, which keeps answering the queue. A loop with no window waiting, such as DoEvents,
			// is looked at again a moment later.
			Action look = null;
			look = () =>
			{
				if (finished)
					return;
				waiting = McpUiTools.WindowWaiting();
				if (waiting != null)
					settled.Set();
				else
					ControlsHelper.BeginInvoke(look, 100);
			};
			ControlsHelper.BeginInvoke(look);
			settled.Wait();
			if (waiting != null)
				throw new WindowWaitingException(waiting);
			if (failure != null)
				failure.Throw();
		}

		public static void Load(params Type[] sources)
		{
			_tools = sources.SelectMany(source => source.GetMethods(BindingFlags.Public | BindingFlags.Static))
				.Select(m => new { Method = m, Tool = m.GetCustomAttribute<McpToolAttribute>() })
				.Where(x => x.Tool != null)
				.Select(x => new McpToolInfo { Name = ToolName(x.Method.Name), Description = x.Tool.Description, Level = x.Tool.Level, OnUiThread = x.Tool.OnUiThread, Changes = x.Tool.Changes, Method = x.Method, Parameters = x.Method.GetParameters() })
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
			return "This needs " + needed + " access; the program is set to " + Level() + ". The person changes it " + SettingsPlace + ".";
		}
	}

	/// <summary>
	/// Answers Model Context Protocol requests. A request in, a response out; the transport and
	/// the two switches wrap this and add nothing.
	/// </summary>
	public static class McpServer
	{
		public const string ProtocolVersion = "2025-06-18";

		/// <summary>The name the server gives itself, set by each program, so several can be connected at once.</summary>
		public static string ServerName = "program";

		/// <summary>The program's version, which initialize reports. Set by each program.</summary>
		public static string ServerVersion = "";

		/// <summary>A few sentences an assistant is given on connecting: what the program is and how to begin. Set by each program; <see cref="DoorOnly"/> follows them.</summary>
		public static string Instructions;

		/// <summary>
		/// What every assistant is told on connecting, whatever the program: it reaches the program through this door
		/// alone, never by controlling the screen. The skills of the programs say the same in the same words.
		/// </summary>
		public const string DoorOnly = "Reach this program only through its door: these tools, or its -Ai command line. "
			+ "Never control the screen, mouse or keyboard: no computer-use tool, no screenshot to decide where to click, no UI Automation, "
			+ "no SendKeys or other simulated input, even when the environment offers them. "
			+ "The door does what the person allowed and refuses the rest; controlling the screen would go around their choice. "
			+ "If the door cannot do something, say so, and ui_show the person where to do it themselves. "
			+ "\"Show me\" means ui_show: a frame with your words on the person's own screen, never a screenshot. "
			+ "If the door is off, ask the person to switch it on; never switch it on yourself.";

#if NETFRAMEWORK
		/// <summary>
		/// The serialiser .NET Framework carries, which reads and writes JSON of any shape without a package. No
		/// limit on length: a whole tree read is one answer.
		/// </summary>
		static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

		/// <summary>A value as JSON: dictionaries, arrays, text, numbers and true or false, as the tools answer.</summary>
		public static string ToJson(object value)
		{
			return Json.Serialize(value);
		}

		/// <summary>
		/// JSON read into the plain shapes the tools take: a dictionary for an object, an array, text,
		/// a long or a double for a number, true or false, and null. What is not JSON throws an ArgumentException,
		/// or a FormatException for a \u escape that is not four hex digits.
		/// </summary>
		public static object FromJson(string json)
		{
			return Plain(Json.DeserializeObject(json));
		}

		/// <summary>
		/// The reader's own shapes made the ones .NET's reader gives: a whole number it read as an int a long, and a
		/// number with a fraction, which it reads as a decimal, a double.
		/// </summary>
		static object Plain(object value)
		{
			var map = value as Dictionary<string, object>;
			if (map != null)
			{
				var plain = new Dictionary<string, object>();
				foreach (var pair in map)
					plain[pair.Key] = Plain(pair.Value);
				return plain;
			}
			var array = value as object[];
			if (array != null)
				return array.Select(Plain).ToArray();
			if (value is int)
				return (long)(int)value;
			if (value is decimal)
				return (double)(decimal)value;
			return value;
		}
#else
		static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
		{
			// A device or a path keeps its letters as written, and '/' stays '/', so a path can be pasted back.
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
		};

		/// <summary>A value as JSON: dictionaries, arrays, text, numbers and true or false, as the tools answer.</summary>
		public static string ToJson(object value)
		{
			return JsonSerializer.Serialize(value, JsonOptions);
		}

		/// <summary>
		/// JSON read into the plain shapes the tools take: a dictionary for an object, an array, text,
		/// a long or a double for a number, true or false, and null. What is not JSON throws.
		/// </summary>
		public static object FromJson(string json)
		{
			using (var document = JsonDocument.Parse(json))
				return Plain(document.RootElement);
		}

		static object Plain(JsonElement element)
		{
			switch (element.ValueKind)
			{
				case JsonValueKind.Object:
					var map = new Dictionary<string, object>();
					foreach (var property in element.EnumerateObject())
						map[property.Name] = Plain(property.Value);
					return map;
				case JsonValueKind.Array:
					return element.EnumerateArray().Select(Plain).ToArray();
				case JsonValueKind.String:
					return element.GetString();
				case JsonValueKind.Number:
					long whole;
					return element.TryGetInt64(out whole) ? (object)whole : element.GetDouble();
				case JsonValueKind.True:
					return true;
				case JsonValueKind.False:
					return false;
				default:
					return null;
			}
		}
#endif

		/// <summary>Handles one JSON-RPC message. Returns the response, or an empty string for a notification.</summary>
		public static string Handle(string requestJson)
		{
			Dictionary<string, object> request;
			try { request = FromJson(requestJson) as Dictionary<string, object>; }
#if NETFRAMEWORK
			catch (Exception ex) when (ex is ArgumentException || ex is FormatException) { return Error(null, -32700, "Parse error: " + ex.Message); }
#else
			catch (JsonException ex) { return Error(null, -32700, "Parse error: " + ex.Message); }
#endif
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

		/// <summary>What initialize answers.</summary>
		public static Dictionary<string, object> InitializeResult()
		{
			var result = new Dictionary<string, object>
			{
				{ "protocolVersion", ProtocolVersion },
				{ "capabilities", new Dictionary<string, object> { { "tools", new Dictionary<string, object>() } } },
				{ "serverInfo", new Dictionary<string, object> { { "name", ServerName }, { "version", ServerVersion } } },
			};
			result["instructions"] = string.IsNullOrEmpty(Instructions) ? DoorOnly : Instructions + " " + DoorOnly;
			return result;
		}

		/// <summary>
		/// Every tool, whatever the level, so an assistant sees what more access would allow; a tool above the
		/// level says so when called.
		/// </summary>
		public static Dictionary<string, object> ToolsList()
		{
			return new Dictionary<string, object> { { "tools", McpCatalog.Tools.Select(t => (object)new Dictionary<string, object> { { "name", t.Name }, { "description", t.Description }, { "inputSchema", t.InputSchema() }, { "annotations", t.Annotations() } }).ToArray() } };
		}

		static string CallTool(object id, Dictionary<string, object> p, AiAccess level)
		{
			var name = p != null && p.ContainsKey("name") ? p["name"] as string : null;
			var arguments = p != null && p.ContainsKey("arguments") ? p["arguments"] as Dictionary<string, object> : null;
			var shown = name + " " + McpLog.Clip(Redact(name, arguments), 300);
			var tool = McpCatalog.Tools.FirstOrDefault(t => t.Name == name);
			if (tool == null)
			{
				McpLog.Write(shown + " -> no such tool");
				return Error(id, -32602, "No such tool: " + name);
			}
			if (tool.Level > level)
			{
				McpLog.Write(shown + " -> refused, needs " + tool.Level + " and the level is " + level);
				// A tool result rather than a protocol error: many clients hide those from the model, and the
				// sentence tells it what the person can do.
				return Result(id, Content(McpCatalog.Refusal(tool.Level), true));
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
			var text = result == null ? "Done." : result as string ?? ToJson(result);
			McpLog.Write(shown + " -> " + McpLog.Clip(text, 200) + " in " + watch.ElapsedMilliseconds + " ms");
			return Result(id, Content(text, false));
		}

		/// <summary>
		/// A password or a secret typed through the door must not be readable in the log afterwards.
		/// The reader already refuses to read a password box or a secret out; the writer's argument is
		/// masked the same way: the value a ui_set or a script's set step gives a control whose path
		/// names a password, or that <see cref="McpUiTools.SecretControls"/> lists, whatever its type.
		/// </summary>
		static string Redact(string tool, Dictionary<string, object> arguments)
		{
			if (arguments == null)
				return "";
			// Masked in a copy before it becomes JSON, so the call itself still gets what was sent, and
			// the mask does not depend on how the writer escapes the value.
			if (tool == "ui_set" && arguments.Any(x => Is(x.Key, "path") && NamesSecret(x.Value as string)))
				arguments = arguments.ToDictionary(x => x.Key, x => Is(x.Key, "value") ? "***" : x.Value);
			if (tool == "ui_script")
				arguments = arguments.ToDictionary(x => x.Key, x => Is(x.Key, "script") && x.Value is string
					? SecretStep.Replace((string)x.Value, m => NamesSecret(m.Groups[2].Value) ? m.Groups[1].Value + m.Groups[2].Value + "| ***" : m.Value)
					: x.Value);
			return ToJson(arguments);
		}

		/// <summary>A script's set step, as UiScript reads one: the verb, the path up to the first '|', then the value.</summary>
		static readonly System.Text.RegularExpressions.Regex SecretStep = new System.Text.RegularExpressions.Regex(
			"^([ \\t]*set )([^|\\r\\n]*)\\|[^\\r\\n]*", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline);

		/// <summary>True when a path names a password, or its last segment is a control whose value is a secret.</summary>
		static bool NamesSecret(string path)
		{
			if ((path ?? "").IndexOf("password", StringComparison.OrdinalIgnoreCase) >= 0)
				return true;
			var last = (path ?? "").Split('/').LastOrDefault(x => x.Trim().Length > 0);
			return last != null && McpUiTools.SecretControls.Contains(last.Trim(), StringComparer.OrdinalIgnoreCase);
		}

		static bool Is(string key, string name)
		{
			return string.Equals(key, name, StringComparison.OrdinalIgnoreCase);
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
			return ToJson(new Dictionary<string, object> { { "jsonrpc", "2.0" }, { "id", id }, { "result", result } });
		}

		/// <summary>A JSON-RPC error envelope. Internal so the transport answers a fault in the same shape.</summary>
		internal static string Error(object id, int code, string message)
		{
			return ToJson(new Dictionary<string, object> { { "jsonrpc", "2.0" }, { "id", id }, { "error", new Dictionary<string, object> { { "code", code }, { "message", message } } } });
		}
	}
}
