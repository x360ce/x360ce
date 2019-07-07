using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
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
			// This class can be inherited therefore make sure that prefix is different.
			// Get type will return derived class or this class if not derived.
			_configPrefix = GetType().Name;
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

		public static string RunMode { get { return ParseString("RunMode", ParseString("Environment", "TEST")); } }
		public static bool IsLive { get { return string.Compare(RunMode, "LIVE", true) == 0; } }

		#endregion

		#region Add To String

		public static void AddParameters(ref string s, IDictionary parameters)
		{
			// Add parameters.
			if (parameters != null)
			{
				foreach (string key in parameters.Keys)
				{
					var pv = parameters[key];
					string v = pv == null
						? "null"
						: string.Format("{0}", pv);
					v = System.Net.WebUtility.HtmlEncode(v);
					v = "<span class=\"Pre\">" + v + "</span>";
					AddRow(ref s, key, v);
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

		protected static void AddExceptionTrace(ref string s, Exception ex)
		{
			if (ex.Data.Count > 0)
			{
				AddParameters(ref s, ex.Data);
			}
			var useHtml = ParseBool(_configPrefix + "ErrorHtmlException", true);
			var html = useHtml
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

		#region Exceptions: SPAM Prevention

		int? ErrorFileLimitMax;
		TimeSpan? ErrorFileLimitAge;
		Dictionary<Type, List<DateTime>> ErrorFileList = new Dictionary<Type, List<DateTime>>();

		public bool AllowReportExceptionToFile(Exception error)
		{
			// Maximum 10 errors of same type per 5 minutes (2880 per day).
			if (!ErrorFileLimitMax.HasValue)
				ErrorFileLimitMax = ParseInt("ErrorFileLimitMax", 5);
			if (!ErrorFileLimitAge.HasValue)
				ErrorFileLimitAge = ParseSpan("ErrorFileLimitAge", new TimeSpan(0, 5, 0));
			return AllowToReportException(error, ErrorFileList, ErrorFileLimitMax.Value, ErrorFileLimitAge.Value);
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

		/// <summary>
		/// Write exception to file.
		/// </summary>
		/// <param name="ex">Exception to write.</param>
		/// <param name="maxFiles">Maximum files per same exception.</param>
		/// <param name="logsFolder">Folder where exception reports will be created.</param>
		/// <param name="writeAsHtml">True - write as HTML, false - write as TEXT.</param>
		public void WriteException(Exception ex, int maxFiles, string logsFolder, bool writeAsHtml)
		{
			var ev = WritingException;
			var le = new LogHelperEventArgs() { Exception = ex };
			if (ev != null)
				ev(this, le);
			if (le.Cancel)
				return;
			var allowToReport = AllowReportExceptionToFile(ex);
			if (!allowToReport)
				return;
			// Must wrap into lock so that process won't attempt to delete/write same file twice from different threads.
			lock (WriteLock)
			{
				// Create file.
				var prefix = "FCE_" + ex.GetType().Name;
				var ext = writeAsHtml ? "htm" : "txt";
				var di = new System.IO.DirectoryInfo(logsFolder);
				// Create folder if not exists.
				if (!di.Exists)
					di.Create();
				// Get exception files ordered with oldest on top.
				var files = di.GetFiles(prefix + "*." + ext).OrderBy(x => x.CreationTime).ToArray();
				// Remove excess files if necessary.
				if (maxFiles > 0 && files.Count() > 0 && files.Count() > maxFiles)
				{
					// Remove oldest file.
					files[0].Delete();
				}
				var fileTime = HiResDateTime.Current.Now;
				var fileName = string.Format("{0}\\{1}_{2:yyyyMMdd_HHmmss.ffffff}.{3}",
					di.FullName, prefix, fileTime, ext);
				var fi = new System.IO.FileInfo(fileName);
				var content = writeAsHtml ? ExceptionInfo(ex, "") : ex.ToString();
				System.IO.File.AppendAllText(fileName, content);
			}
		}

		#endregion

		#region Convert Exception to HTML String

		public string ExceptionInfo(Exception ex, string body)
		{
			//------------------------------------------------------
			// Body
			//------------------------------------------------------
			string s = string.Empty;
			if (!string.IsNullOrEmpty(body)) s += "<div>" + body + "</div>";
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

		public static void ExceptionInfoRecursive(ref string s, Exception ex)
		{
			if (ex == null)
				return;
			StackFrame frame = GetFormStackFrame(ex);
			AddRow(ref s, string.Format("{0}: <span class=\"Grey\">{1}</span>", GetClassName(ex), ex.Message));
			AddRow(ref s, "Exception Date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
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

		public static bool FillLoaderException(ref string s, Exception ex)
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
			AddParameters(ref s, parameters);
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

		#region SPAM prevention.

		/// <summary>
		/// Prevent SPAM by suppressing frequent exceptions.
		/// For example it can allow maximum 10 errors of same type per 5 minutes (2880 per day).
		/// Amount of suppressed exceptions will be included inside next exception which is not suppressed.
		/// </summary>
		/// <param name="error">Exception</param>
		/// <param name="errorList">Contains list of dates when exception type was reported.</param>
		/// <param name="errorLimitMax">Maximum exceptions per specified time.</param>
		/// <param name="errorLimitAge">Time for exceptions</param>
		/// <returns>True - Allow to send exception. False - suppress sending.</returns>
		public static bool AllowToReportException(Exception error, Dictionary<Type, List<DateTime>> errorList, int errorLimitMax, TimeSpan errorLimitAge)
		{
			if (errorLimitMax <= 0) return true;
			if (errorLimitAge.Ticks <= 0) return true;
			lock (errorList)
			{
				var errorType = error.GetType();
				List<DateTime> list;
				if (errorList.ContainsKey(errorType))
				{
					list = errorList[errorType];
				}
				else
				{
					list = new List<DateTime>();
					errorList.Add(errorType, list);
				}
				var n = DateTime.Now;
				var oldTime = n.Subtract(errorLimitAge);
				// Remove old exceptions.
				list.RemoveAll(x => x < oldTime);
				var count = list.Count();
				// If limit reached then return.
				if (count >= errorLimitMax)
					return false;
				list.Add(n);
				if (errorLimitMax == 1 || count > 0)
				{
					Upsert(error, "ErrorType", errorType);
					Upsert(error, "ErrorCount", count);
					Upsert(error, "Config: ErrorLimitMax", errorLimitMax);
					Upsert(error, "Config: ErrorLimitAge", errorLimitAge);
					Upsert(error, "Config: ErrorUseNewStackTrace", LogHelper.ErrorUseNewStackTrace);
				}
				return true;
			}
		}

		public static void Upsert(Exception ex, object key, object value)
		{
			if (ex.Data.Contains(key))
			{
				ex.Data[key] = value;
			}
			else
			{
				ex.Data.Add(key, value);
			}
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
