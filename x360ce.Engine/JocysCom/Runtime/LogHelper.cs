using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace JocysCom.ClassLibrary.Runtime
{
	public partial class LogHelper : IDisposable
	{

		public LogHelper()
		{
			_configPrefix = typeof(LogHelper).Name + "_";
			_FileWriter = new IO.LogFileWriter(_configPrefix);
		}

		static string _configPrefix;

		private static LogHelper _Current;
		private static object currentLock = new object();
		public static LogHelper Current
		{
			get
			{
				lock (currentLock)
				{
					if (_Current == null)
					{
						_Current = new LogHelper();
						// Won't trigger application is closing by using the close button.
						AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
					}
					return _Current;
				}
			}
		}

		private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
		{
			_Current.Dispose();
		}

		#region Settings

		/// <summary>
		/// If used then, can loose information about original line of exception, therefore option is 'false' by default.
		/// </summary>
		public bool ErrorUseNewStackTrace { get { return ParseBool(_configPrefix + "UseNewStackTrace", false); } }
		public bool WriteAsHtml { get { return ParseBool(_configPrefix + "WriteAsHtml", true); } }
		public bool LogThreadExceptions { get { return ParseBool(_configPrefix + "LogThreadExceptions", true); } }
		public bool LogUnhandledExceptions { get { return ParseBool(_configPrefix + "LogUnhandledExceptions", true); } }
		public bool LogFirstChanceExceptions { get { return ParseBool(_configPrefix + "LogFirstChanceExceptions", true); } }
		public bool GroupingEnabled { get { return ParseBool(_configPrefix + "GroupingEnabled", false); } }
		public TimeSpan GroupingDelay { get { return ParseSpan(_configPrefix + "GroupingDelay", new TimeSpan(0, 5, 0)); } }
		public static string RunMode { get { return ParseString("RunMode", ParseString("Environment", "TEST")); } }
		public static bool IsLive { get { return string.Compare(RunMode, "LIVE", true) == 0; } }

		#endregion

		#region Parse

		public static bool ParseBool(string name, bool defaultValue)
		{
			var v = ConfigurationManager.AppSettings[name];
			return (v == null) ? defaultValue : bool.Parse(v);
		}

		public static TimeSpan ParseSpan(string name, TimeSpan defaultValue)
		{
			var v = ConfigurationManager.AppSettings[name];
			return (v == null) ? defaultValue : TimeSpan.Parse(v);
		}

		public static T ParseEnum<T>(string name, T defaultValue)
		{
			var v = ConfigurationManager.AppSettings[name];
			return (v == null) ? defaultValue : (T)Enum.Parse(typeof(T), v);
		}

		public static string ParseString(string name, string defaultValue)
		{
			var v = ConfigurationManager.AppSettings[name];
			return (v == null) ? defaultValue : v;
		}

		public static int ParseInt(string name, int defaultValue)
		{
			var v = ConfigurationManager.AppSettings[name];
			return (v == null) ? defaultValue : int.Parse(v);
		}

		public static long ParseLong(string name, long defaultValue)
		{
			var v = ConfigurationManager.AppSettings[name];
			return (v == null) ? defaultValue : long.Parse(v);
		}

		public static IPAddress ParseIPAddress(string name, IPAddress defaultValue)
		{
			var v = ConfigurationManager.AppSettings[name];
			return (v == null) ? defaultValue : IPAddress.Parse(v);
		}

		#endregion

		#region Process Exceptions

		/// <summary>
		/// Windows forms can attach function which will be used when exception is thrown.
		/// For example it can open window to the user with exception details.
		/// </summary>
		public ProcessExceptionDelegate ProcessExceptionExtra;
		public delegate void ProcessExceptionDelegate(Exception ex);

		public static bool IsDebug
		{
			get
			{
				bool debug = false;
#if DEBUG
				debug = true;
#endif
				return debug;
			}
		}

		#endregion

		#region Add To String

		public static void AddParameters(ref string s, IDictionary parameters, TraceFormat tf)
		{
			if (parameters == null)
				return;
			bool isHtml = (tf == TraceFormat.Html);
			foreach (string key in parameters.Keys)
			{
				var pv = parameters[key];
				string v = pv == null
					? "null"
					: pv is DateTime
						? string.Format("{0:yyyy-MM-dd HH:mm:ss.fff}", pv)
						: string.Format("{0}", pv);
				if (isHtml)
				{
					v = System.Net.WebUtility.HtmlEncode(v);
					v = "<span class=\"Pre\">" + v + "</span>";
					AddRow(ref s, key, v);
				}
				else
				{
					s += string.Format("{0}: {1}\r\n", key, v);
				}

			}
		}

		public static void AddStyle(ref string s)
		{
			s += "<style type=\"text/css\">\r\n";
			s += "table tr td { font-family: Tahoma; font-size: 10pt; white-space: nowrap; }\r\n";
			s += "table tr th { font-family: Tahoma; font-size: 10pt; white-space: nowrap; text-align:left; }\r\n";
			s += ".Table { border: solid 1px #EEEEEE; border-collapse: collapse; empty-cells: show; }\r\n";
			s += ".Table tr td { border: solid 1px #EEEEEE; padding: 2px 4px 2px 4px; }\r\n";
			s += ".Table tr th { border: solid 1px #EEEEEE; padding: 2px 4px 2px 4px; background-color: #EEEEEE; }\r\n";
			s += ".Head { font-weight: bold; text-align:left; }\r\n";
			s += ".Body {  }\r\n";
			s += ".Grey { color: #606060; font-weight: normal; }\r\n";
			s += ".Name { padding-left: 16px; }\r\n";
			s += ".Mono { font-family: monospace; font-size: 10pt; }\r\n";
			s += ".Pre { white-space: pre; font-family: monospace; font-size: 10pt; }\r\n";
			s += ".Value { width: 100%; }\r\n";
			s += "</style>\r\n";
		}

		/// <summary>Add empty row.</summary>
		public static void AddRow(ref string s)
		{
			s += string.Format("<tr><td colspan=\"2\"> </td></tr>");
		}

		/// <summary>Add header row.</summary>
		public static void AddRow(ref string s, string name)
		{
			s += string.Format("<tr><th colspan=\"2\" class=\"Head\">{0}</th></tr>", name);
		}

		#region Table

		public static void AddStyle(StringBuilder sb)
		{
			var s = "";
			AddStyle(ref s);
			sb.Append(s);
		}

		public static void AddTable(StringBuilder sb)
		{
			sb.Append("<table class=\"Table\">");
		}

		public static void EndTable(StringBuilder sb)
		{
			sb.Append("</table>");
		}

		/// <summary>Add head rows.</summary>
		public static void AddHeadRows(StringBuilder sb, params object[] args)
		{
			sb.Append("<tr>");
			foreach (var arg in args)
			{
				var v = System.Net.WebUtility.HtmlEncode(string.Format("{0}", arg));
				sb.AppendFormat("<th class=\"Head\">{0}</th>", v);
			}
			sb.Append("</tr>");
		}

		/// <summary>Add body rows.</summary>
		public static void AddBodyRows(StringBuilder sb, params object[] args)
		{
			sb.Append("<tr>");
			foreach (var arg in args)
			{
				var v = System.Net.WebUtility.HtmlEncode(string.Format("{0}", arg));
				sb.AppendFormat("<td class=\"Body\">{0}</td>", v);
			}
			sb.Append("</tr>");
		}

		#endregion

		/// <summary>Add row with key and value cells.</summary>
		public static void AddRow(ref string s, string key, string value)
		{
			string sep = "";
			if (!string.IsNullOrEmpty(key))
				sep = ":";
			s += string.Format("<tr><td class=\"Name\" valign=\"top\">{0}{1}</td><td>{2}</td></tr>", key, sep, value);
		}

		void AddExceptionTrace(ref string s, Exception ex)
		{
			if (ex.Data.Count > 0)
			{
				AddParameters(ref s, ex.Data, TraceFormat.Html);
			}
			var html = WriteAsHtml
				? ExceptionToString(ex, true, TraceFormat.Html)
				: "<pre>" + ex.ToString() + "</pre>";
			AddRow(ref s, "StackTrace", html);
		}

		public static void AddConnection(ref string s, string name, string connectionString)
		{
			System.Data.SqlClient.SqlConnectionStringBuilder cb = null;
			cb = new System.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
			s += string.Format("<tr><td class=\"Name\"  valign=\"top\">{0}:</td><td class=\"Value\" valign=\"top\">{1}.{2}</td></tr>", name, cb.DataSource, cb.InitialCatalog);
		}

		private static Regex nonDigitsRx = new Regex("[^0-9]");

		public static string GetDigitsOnly(string number)
		{
			return nonDigitsRx.Replace(number, string.Empty);
		}

		/// <summary>
		/// ****-****-****-NNNN, only last number are revealed since they don't tell too much about card (type or issuer)
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		/// <remarks></remarks>
		public static string GetMasked(string number, char mask = '*')
		{
			if (string.IsNullOrEmpty(number))
			{
				return string.Empty;
			}
			string s = GetDigitsOnly(number);
			if (s.Length < 4 + 6)
				return string.Empty;
			StringBuilder sb = new StringBuilder();
			sb.Append(s.Substring(0, 6));
			sb.Append(mask, s.Length - 4 - 6);
			sb.Append(s.Substring(s.Length - 4, 4));
			return sb.ToString();
			//Return GetFormated(sb.ToString(), "-"c)
		}

		#endregion

		#region Write Log

		public delegate void WriteLogDelegate(string message, EventLogEntryType type);

		/// <summary>
		/// User can override these methods. Default methods are assigned.
		/// </summary>
		public WriteLogDelegate WriteLogCustom;
		public WriteLogDelegate WriteLogConsole = new WriteLogDelegate(_WriteConsole);
		public WriteLogDelegate WriteLogEvent = new WriteLogDelegate(_WriteEvent);
		public WriteLogDelegate WriteLogFile = new WriteLogDelegate(_WriteFile);

		internal static void _WriteConsole(string message, EventLogEntryType type)
		{
			// If user can see interface (console) then write to the console.
			if (Environment.UserInteractive)
				Console.WriteLine(message);
		}

		// Requires 'EventLogInstaller' requires reference to System.Configuration.Install.dll

		public static EventLogInstaller AppEventLogInstaller;

		internal static void _WriteEvent(string message, EventLogEntryType type)
		{
			var li = AppEventLogInstaller;
			if (li == null)
				return;
			var ei = new EventInstance(0, 0, type);
			var el = new EventLog();
			el.Log = li.Log;
			el.Source = li.Source;
			el.WriteEvent(ei, message);
			el.Close();
		}

		public IO.LogFileWriter FileWriter { get { return _FileWriter; } }
		IO.LogFileWriter _FileWriter;

		internal static void _WriteFile(string message, EventLogEntryType type)
		{
			// If LogStreamWriter is not null (check inside the function) then write to file.
			Current.FileWriter.WriteLine(message);
		}

		/// <summary>
		/// Writes log message to various destination types (console window, file, event and custom)
		/// </summary>
		/// <remarks>Appends line break.</remarks>
		public void WriteLog(string message, EventLogEntryType type)
		{
			// If console logging available then...
			if (WriteLogConsole != null)
				WriteLogConsole(message, type);
			// If file logging available then...
			if (WriteLogFile != null)
				WriteLogFile(message, type);
			// If custom logging is enabled then write custom log (can be used to send emails).
			if (WriteLogCustom != null)
				WriteLogCustom(message, type);
			// If event logging is enabled and important then write event.
			if (WriteLogEvent != null && type != EventLogEntryType.Information)
				WriteLogEvent(message, type);
		}

		public static void WriteError(Exception ex)
		{
			Current.WriteLog(ex.ToString(), EventLogEntryType.Error);
		}

		public static void WriteWarning(string format, params object[] args)
		{
			Current.WriteLog(args.Length > 0 ? string.Format(format, args) : format, EventLogEntryType.Warning);
		}

		public static void WriteInfo(string format, params object[] args)
		{
			Current.WriteLog(args.Length > 0 ? string.Format(format, args) : format, EventLogEntryType.Information);
		}

		#endregion

		#region Exceptions

		//public static string ExceptionInfo(Exception ex, string body)
		//{
		//	return ExceptionInfo(ex, body, DefaultPageInfo, DefaultPageInfo);
		//}

		//public static void WriteException(Exception ex, int maxFiles, string logsFolder, bool writeAsHtml)
		//{
		//	WriteException(ex, maxFiles, logsFolder, writeAsHtml, DefaultPageInfo, DefaultPageInfo);
		//}

		public static void ApplyRunModeSuffix(ref string s)
		{
			if (!IsLive)
			{
				string rm = "(" + RunMode + ")";
				if (s == null) s = rm;
				s = s.TrimEnd();
				if (!s.Contains(rm)) s += " " + rm;
			}
		}

		public static object WriteLock = new object();

		#endregion

		#region Group Same exceptions

		static readonly Regex RxBreaks = new Regex("[\r\n]", RegexOptions.Multiline);
		static readonly Regex RxMultiSpace = new Regex("[ \u00A0]+");

		bool _GroupException(List<ExceptionGroup> group, Exception ex, string subject, string body, Action<Exception, string, string> action)
		{
			//------------------------------------------------------
			// Subject
			//------------------------------------------------------
			// If exception found then...
			if (ex != null)
			{
				// If exception contains data.
				if (ex.Data != null)
				{
					// Remove "StackTrace" value if exists.
					var key = ex.Data.Keys.Cast<object>().FirstOrDefault(x => object.ReferenceEquals(x, "StackTrace"));
					if (key != null && ex.Data[key] is StackTrace)
						ex.Data.Remove(key);
				}
				// If subject was not specified then...
				if (string.IsNullOrEmpty(subject))
					// Generate subject from exception.
					subject = GetSubjectPrefix(ex, TraceEventType.Error) + ex.Message;
			}
			if (string.IsNullOrEmpty(subject))
				subject = "null";
			// Make subject one line and remove extra spaces.
			subject = RxBreaks.Replace(subject, " ");
			subject = RxMultiSpace.Replace(subject, " ");
			// Cut subject because some mail servers refuse to deliver messages when subject is too large.
			var maxLength = 255;
			if (subject.Length > maxLength)
				subject = subject.Substring(0, maxLength - 3) + "...";
			//------------------------------------------------------
			// Group.
			//------------------------------------------------------
			var notifyNow =
				ex == null ||
				!GroupingEnabled ||
				GroupExceptions(group, ex, subject, body, action);
			if (notifyNow)
				action(ex, subject, body);
			return notifyNow;
		}

		/// <summary>
		/// Prevent SPAM by suppressing frequent exceptions.
		/// For example it can allow maximum 10 errors of same type per 5 minutes (2880 per day).
		/// Amount of suppressed exceptions will be included inside next exception which is not suppressed.
		/// </summary>
		bool GroupExceptions(List<ExceptionGroup> group, Exception ex, string subject, string body, Action<Exception, string, string> action)
		{
			var value = ex.StackTrace == null
				? string.Format("{0}: {1}", ex.GetType().Name, ex.Message)
				: ex.StackTrace.ToString();
			// Get checksum.
			var algorithm = System.Security.Cryptography.SHA256.Create();
			var bytes = System.Text.Encoding.UTF8.GetBytes(value);
			var hash = algorithm.ComputeHash(bytes);
			var guidBytes = new byte[16];
			Array.Copy(hash, guidBytes, guidBytes.Length);
			var checksum = new Guid(guidBytes);
			// Try to get existing exception.
			lock (group)
			{
				var ei = group.FirstOrDefault(x => x.Checksum == checksum);
				var notifyNow = ei == null;
				if (ei == null)
				{
					ei = new ExceptionGroup(group, GroupingDelay, ex, checksum, subject, body, action);
					group.Add(ei);
				}
				ei.Increment();
				return notifyNow;
			}
		}

		static List<ExceptionGroup> mailExceptions = new List<ExceptionGroup>();

		public class ExceptionGroup : IDisposable
		{
			public ExceptionGroup(List<ExceptionGroup> group, TimeSpan delay, Exception ex, Guid checksum, string subject, string body, Action<Exception, string, string> action)
			{
				Error = ex;
				Checksum = checksum;
				Created = DateTime.Now;
				Updates = new List<DateTime>();
				// custom parameters.
				Subject = subject;
				Body = body;
				Group = group;
				Action = action;
				// Add timer to flush exceptions.
				_Timer = new System.Timers.Timer();
				_Timer.AutoReset = false;
				_Timer.Interval = delay.TotalMilliseconds;
				_Timer.Elapsed += _Timer_Elapsed;
				_Timer.Start();
			}
			void _Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
			{
				// If new exceptions were added then send.
				if (Updates.Count > 1)
				{
					for (int i = 0; i < Updates.Count; i++)
						Error.Data[string.Format("{0}.Time.{1}", GetType().Name, i)] = Updates[i];
					Action(Error, Subject, Body);
				}
				// Dispose exception group which was sent by email or written to the file.
				Dispose();
			}
			public void Increment()
			{
				// If new record then...
				if (Updates.Count == 0)
					Error.Data[string.Format("{0}.{1}", GetType().Name, "Checksum")] = Checksum.ToString("N");
				// Use created time for first record.
				Updates.Add(Updates.Count == 0 ? Created : DateTime.Now);
			}

			public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
			protected virtual void Dispose(bool disposing)
			{
				if (disposing)
				{
					_Timer.Dispose();
					// Remove this exception group from the list.
					if (Group.Contains(this))
						Group.Remove(this);
					Group = null;
					Action = null;
				}
			}

			System.Timers.Timer _Timer;
			public Guid Checksum;
			public Exception Error;
			public DateTime Created;
			public List<DateTime> Updates;
			public string Subject;
			public string Body;
			Action<Exception, string, string> Action;
			List<ExceptionGroup> Group;
		}

		#endregion

		#region Convert Exception to HTML String

		public static string GetSubjectPrefix(Exception ex = null, TraceEventType? type = null)
		{
			var asm = Assembly.GetEntryAssembly();
			string s = "Unknown Entry Assembly";
			if (asm == null && ex != null)
			{
				var frames = new StackTrace(ex).GetFrames();
				if (frames != null && frames.Length > 0)
				{
					asm = frames[0].GetMethod().DeclaringType.Assembly;
				}
			}
			if (asm == null)
			{
				asm = Assembly.GetCallingAssembly();
			}
			if (asm != null)
			{
				var last2Nodes = asm.GetName().Name.Split('.').Reverse().Take(2).Reverse();
				s = string.Join(".", last2Nodes);
			}
			if (type.HasValue)
				s += string.Format(" {0}", type);
			ApplyRunModeSuffix(ref s);
			s += ": ";
			return s;
		}

		public string ExceptionInfo(Exception ex, string body)
		{
			//------------------------------------------------------
			// Body
			//------------------------------------------------------
			string s = string.Empty;
			if (!string.IsNullOrEmpty(body))
				s += "<div>" + body + "</div><br /><br />";
			//------------------------------------------------------
			AddStyle(ref s);
			//------------------------------------------------------
			s += "<table border=\"0\" cellspacing=\"2\">";
			var rm = RunMode;
			if (!string.IsNullOrEmpty(rm))
			{
				rm = " (" + rm + ")";
			}
			var asm = System.Reflection.Assembly.GetEntryAssembly();
			if (asm == null) Assembly.GetCallingAssembly();
			if (asm == null) Assembly.GetExecutingAssembly();
			AddRow(ref s, "Product");
			if (asm != null)
			{
				var ai = new Configuration.AssemblyInfo(asm);
				var name = ai.Company + " " + ai.Product + " " + ai.Version.ToString(4);
				ApplyRunModeSuffix(ref name);
				AddRow(ref s, "Name", name);
			}
			AddRow(ref s, "Machine", System.Environment.MachineName);
			AddRow(ref s, "Username", System.Environment.UserName);
			if (asm != null)
			{
				var bd = Configuration.AssemblyInfo.GetBuildDateTime(asm.Location);
				AddRow(ref s, "Executable", asm.Location);
				AddRow(ref s, "Build Date", bd.ToString("yyyy-MM-dd HH:mm:ss"));
				//AddRow(ref s, "SVN LastCheckIn", "svn log -q \"%file%\" -r {"+ bd.ToString("yyyy-MM-ddTHH:mm:ss") + "}:{1970-01-01} -l 1");
				//AddRow(ref s, "SVN GetCodeFile", "svn cat -r %rev% \"%file%\" > \"%TEMP%\\code.cs\" && \"%TEMP%\\code.cs\"");
				/* Look for source code in SVN.

				:: Set build date.
				set date=2016-10-24T14:12:53
				:: Set file name to investigate.
				set file=C:\Projects\Volante\Dispatch\Engine\Messages\ClientStateMessage.cs
				:: Show last check-in before build (biggest chance of version we are looking for).
				svn log -q "%file%" -r {%date%}:{1970-01-01} -l 1
				:: Show first check-in after build.
				svn log -q "%file%" -r {%date%}:{9999-01-01} -l 1
				
				:: Save file revision to disk.
				svn cat -r r18594 "%file%" > "%TEMP%\svn.cs"
				:: Open file with Visual Studio.
				"%TEMP%\svn.cs"

				*/
			}
			// If LogHelper.Web.cs is included
			var methods = new string[] { "UserInfo", "PageInfo" };
			foreach (var method in methods)
			{
				var uiMethod = GetType().GetMethods().FirstOrDefault(x => x.Name == method);
				if (uiMethod != null)
				{
					var uim = GetType().GetMethod(method, new Type[] { typeof(string).MakeByRefType() });
					var args = new object[] { s };
					uim.Invoke(this, args);
					s = (string)args[0];
				}
			}
			AddRow(ref s);
			s += "</table>";
			s += "<table border=\"0\" cellspacing=\"2\" class=\"Table\">";
			ExceptionInfoRecursive(ref s, ex);
			//------------------------------------------------------
			s += "</table>";
			return s;
		}

		public void ExceptionInfoRecursive(ref string s, Exception ex)
		{
			if (ex == null)
				return;
			StackFrame frame = GetFormStackFrame(ex);
			AddRow(ref s, string.Format("{0}: <span class=\"Grey\">{1}</span>", GetClassName(ex), ex.Message));
			AddRow(ref s, "Exception Date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
			// Get targetType and TargetName
			var mb = ex.TargetSite;
			if (frame != null && frame.GetMethod() != null && frame.GetMethod().DeclaringType != null)
				mb = frame.GetMethod();
			if (mb != null)
			{
				AddRow(ref s, "Target.Type", mb.DeclaringType.ToString());
				AddRow(ref s, "Target.Name", mb.Name);
			}
			if (FillLoaderException(ref s, ex)) { }
			else if (FillSqlException(ref s, ex)) { }
			else FillOther(ref s, ex);
			AddExceptionTrace(ref s, ex);
			// Append inner exception to the end.
			if (ex.InnerException != null)
				ExceptionInfoRecursive(ref s, ex.InnerException);
		}

		public static bool FillSqlException(ref string s, Exception ex)
		{
			var ex2 = ex as SqlException;
			if (ex2 == null)
				return false;
			for (int i = 0; i <= ex2.Errors.Count - 1; i++)
			{
				var err = ex2.Errors[i];
				var prefix = string.Format("Errors[{0}]", i + 1);
				Add(ex2, prefix + ".Source", err.Source);
				Add(ex2, prefix + ".Server", err.Server);
				Add(ex2, prefix + ".Location", string.Format("Number {0}, Level {1}, State {2}, Line {3}", err.Number, err.Class, err.State, err.LineNumber));
				Add(ex2, prefix + ".Message", err.Message);
				if (!string.IsNullOrEmpty(err.Procedure))
				{
					Add(ex2, prefix + ".Procedure", err.Procedure);
					Add(ex2, prefix + ".Help", "SQL command to display lines of procedure: exec sp_helptext '" + err.Procedure + "'");
				}
			}
			return true;
		}

		public bool FillLoaderException(ref string s, Exception ex)
		{
			var ex2 = ex as ReflectionTypeLoadException;
			if (ex2 == null)
				return false;
			var i = 0;
			foreach (var ex4 in ex2.LoaderExceptions)
			{
				var s1 = "";
				ExceptionInfoRecursive(ref s1, ex4);
				var key = string.Format("LoaderExceptions[{0}]", i++);
				Add(ex2, key, s1);
			}
			return true;
		}

		public static void FillOther(ref string s, Exception ex)
		{
			if (ex == null)
				return;
			var parameters = new Dictionary<string, object>();
			foreach (var pi in ex.GetType().GetProperties())
			{
				if (!pi.CanRead)
					continue;
				// HTML stack trace will be added.
				if (new string[] { "StackTrace", "TargetSite", "Data", "Message", "InnerException" }.Contains(pi.Name))
					continue;
				var value = pi.GetValue(ex);
				if (value == null)
					continue;
				parameters.Add("." + pi.Name, value);
			}
			AddParameters(ref s, parameters, TraceFormat.Html);
		}

		static void Add(Exception ex, string name, object value)
		{
			var prefix = ex.GetType().Name;
			var key = string.Format("{0}.{1}", prefix, name);
			if (ex.Data.Contains(key))
				return;
			ex.Data.Add(key, value);
		}

		#endregion

		#region IDisposable

		// Dispose() calls Dispose(true)
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		bool IsDisposing;

		void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (IsDisposing)
					return;
				IsDisposing = true;
				if (_FileWriter != null)
					_FileWriter.Dispose();
			}
		}

		#endregion

	}
}
