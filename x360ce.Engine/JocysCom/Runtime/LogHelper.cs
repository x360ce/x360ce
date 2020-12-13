using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
#if NETSTANDARD
#elif NETCOREAPP
#else
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Data.SqlClient;
#endif

namespace JocysCom.ClassLibrary.Runtime
{
	public partial class LogHelper : IDisposable
	{

		public LogHelper()
		{
			var _configPrefix = typeof(LogHelper).Name + "_";
			_SP = new Configuration.SettingsParser();
			_SP.ConfigPrefix = _configPrefix;
			_FileWriter = new IO.LogFileWriter(_configPrefix);
		}

		Configuration.SettingsParser _SP;

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
		public bool ErrorUseNewStackTrace { get { return _SP.Parse("UseNewStackTrace", false); } }
		public bool WriteAsHtml { get { return _SP.Parse("WriteAsHtml", true); } }

		public bool LogToFile
		{
			get { return _LogToFile ?? _SP.Parse("LogToFile", false); }
			set { _LogToFile = value; }
		}
		private bool? _LogToFile;

		public bool LogToMail
		{
			get { return _LogToMail ?? _SP.Parse("LogToMail", false); }
			set { _LogToMail = value; }
		}
		private bool? _LogToMail;

		public bool LogExceptions
		{
			get { return _LogExceptions ?? _SP.Parse("LogExceptions", false); }
			set { _LogExceptions = value; }
		}
		private bool? _LogExceptions;

		public bool LogThreadExceptions
		{
			get { return _LogThreadExceptions ?? _SP.Parse("LogThreadExceptions", true); }
			set { _LogThreadExceptions = value; }
		}
		private bool? _LogThreadExceptions;

		public bool LogUnhandledExceptions
		{
			get { return _LogUnhandledExceptions ?? _SP.Parse("LogUnhandledExceptions", true); }
			set { _LogUnhandledExceptions = value; }
		}
		private bool? _LogUnhandledExceptions;

		public bool LogFirstChanceExceptions
		{
			get { return _LogFirstChanceExceptions ?? _SP.Parse("LogFirstChanceExceptions", true); }
			set { _LogFirstChanceExceptions = value; }
		}
		private bool? _LogFirstChanceExceptions;

		public bool LogUnobservedTaskExceptions
		{
			get { return _LogUnobservedTaskExceptions ?? _SP.Parse("LogUnobservedTaskExceptions", true); }
			set { _LogUnobservedTaskExceptions = value; }
		}
		private bool? _LogUnobservedTaskExceptions;

		public bool GroupingEnabled { get { return _SP.Parse("GroupingEnabled", false); } }
		public TimeSpan GroupingDelay { get { return _SP.Parse("GroupingDelay", new TimeSpan(0, 5, 0)); } }
		public static string RunMode
		{
			get
			{
				// if "RunMode" key not found then try "Environment" key.
				return Configuration.SettingsParser.Current.Parse("RunMode", Configuration.SettingsParser.Current.Parse("Environment", "TEST"));
			}
		}
		public static bool IsLive { get { return string.Compare(RunMode, "LIVE", true) == 0; } }

		#endregion

		#region Process Exceptions

		/// <summary>
		/// Windows forms can attach function which will be used when exception is thrown.
		/// For example it can open window to the user with exception details.
		/// </summary>
		public ProcessExceptionDelegate ProcessExceptionExtra;
		public static ProcessExceptionDelegate ProcessExceptionExtraGlobal;
		public delegate void ProcessExceptionDelegate(Exception ex);

		/// <summary>
		/// Detect if Visual Studio is debugging the program.
		/// </summary>
		public static bool IsDebug
		{
			get
			{
#if DEBUG
				return true;
#else
				return false;
#endif
			}
		}

		#endregion

		#region Add To String

		public static void AddParameters(ref string s, IDictionary parameters, TraceFormat tf = TraceFormat.Html)
		{
			if (parameters == null)
				return;
			bool isHtml = (tf == TraceFormat.Html);
			foreach (var key in parameters.Keys)
			{
				var pv = parameters[key];
				var k = string.Format("{0}", key);
				string v = pv == null
					? "null"
					: pv is DateTime
						? string.Format("{0:yyyy-MM-dd HH:mm:ss.fff}", pv)
						: string.Format("{0}", pv);
				if (isHtml)
				{
					v = System.Net.WebUtility.HtmlEncode(v);
					v = "<span class=\"Pre\">" + v + "</span>";
					AddRow(ref s, k, v);
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

		#region Table

		public static void StartTable(ref string s)
		{
			s += "<table class=\"Table\">";
		}

		public static void EndTable(ref string s)
		{
			s += "</table>";
		}

		/// <summary>Add multiple head columns.</summary>
		public static void AddHeadRows(ref string s, params object[] args)
		{
			s += "<tr>";
			foreach (var arg in args)
			{
				var v = System.Net.WebUtility.HtmlEncode(string.Format("{0}", arg));
				s += string.Format("<th class=\"Head\">{0}</th>", v);
			}
			s += "</tr>";
		}

		/// <summary>Add multiple body columns.</summary>
		public static void AddBodyRows(ref string s, params object[] args)
		{
			s += "<tr>";
			foreach (var arg in args)
			{
				var v = System.Net.WebUtility.HtmlEncode(string.Format("{0}", arg));
				s += string.Format("<td class=\"Body\">{0}</td>", v);
			}
			s += "</tr>";
		}

		#endregion

		/// <summary>Add row with key and value cells.</summary>
		public static void AddRow(ref string s, string key = null, string value = null)
		{
			// If empty row then...
			if (key == null && value == null)
				s += "<tr><td colspan=\"2\"> </td></tr>";
			// If head row then...
			else if (key != null && value == null)
				s += string.Format("<tr><th colspan=\"2\" class=\"Head\">{0}</th></tr>", key);
			// if key anbd value specified.
			else
				s += string.Format("<tr><td class=\"Name\" valign=\"top\">{0}{1}</td><td>{2}</td></tr>",
					key,
					string.IsNullOrEmpty(key) ? "" : ":",
					value
				);
		}

		void AddExceptionTrace(ref string s, Exception ex)
		{
			if (ex.Data.Count > 0)
				AddParameters(ref s, ex.Data, TraceFormat.Html);
			bool containsFileAndLineNumber = false;
			var html = WriteAsHtml
				? ExceptionToString(ex, true, TraceFormat.Html, out containsFileAndLineNumber)
				: "<pre>" + ex.ToString() + "</pre>";
			if (ex.TargetSite != null && ex.TargetSite.DeclaringType != null && ex.TargetSite.DeclaringType.Assembly != null)
				AddRow(ref s, "Target.Declaring.Assembly", ex.TargetSite.DeclaringType.Assembly.FullName);
			if (containsFileAndLineNumber)
				AddRow(ref s, "StackTraceFile", "Yes");
			AddRow(ref s, "StackTrace", html);
		}

		public static void AddConnection(ref string s, string name, string connectionString)
		{
#if NETSTANDARD // .NET Standard
#elif NETCOREAPP // .NET Core
#else // .NET Framework
			var cb = new System.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
			s += string.Format("<tr><td class=\"Name\"  valign=\"top\">{0}:</td><td class=\"Value\" valign=\"top\">{1}.{2}</td></tr>", name, cb.DataSource, cb.InitialCatalog);
#endif
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
				return string.Empty;
			string s = GetDigitsOnly(number);
			if (s.Length < 4 + 6)
				return string.Empty;
			StringBuilder sb = new StringBuilder();
			sb.Append(s.Substring(0, 6));
			sb.Append(mask, s.Length - 4 - 6);
			sb.Append(s.Substring(s.Length - 4, 4));
			return sb.ToString();
		}

		#endregion

		#region Write Log

		public delegate void WriteLogDelegate(string message, TraceLevel type);

		/// <summary>
		/// User can override these methods. Default methods are assigned.
		/// </summary>
		public WriteLogDelegate WriteLogCustom;
		public WriteLogDelegate WriteLogConsole = new WriteLogDelegate(_WriteConsole);

#if NETSTANDARD
#elif NETCOREAPP
#else
		public WriteLogDelegate WriteLogEvent = new WriteLogDelegate(_WriteEvent);
#endif
		public WriteLogDelegate WriteLogFile = new WriteLogDelegate(_WriteFile);

		internal static void _WriteConsole(string message, TraceLevel type)
		{
			// If user can see interface (console) then write to the console.
			if (Environment.UserInteractive)
				Console.WriteLine(message);
		}



#if NETSTANDARD
#elif NETCOREAPP
#else
		// Requires 'EventLogInstaller' requires reference to System.Configuration.Install.dll
		public static EventLogInstaller AppEventLogInstaller;

		internal static void _WriteEvent(string message, TraceLevel type)
		{
			var li = AppEventLogInstaller;
			if (li == null)
				return;
			var ei = new EventInstance(0, 0, ConvertToEventLogEntryType(type));
			var el = new EventLog();
			el.Log = li.Log;
			el.Source = li.Source;
			el.WriteEvent(ei, message);
			el.Close();
		}

		public static EventLogEntryType ConvertToEventLogEntryType(TraceLevel level)
		{
			switch (level)
			{
				case TraceLevel.Error:
					return EventLogEntryType.Error;
				case TraceLevel.Warning:
					return EventLogEntryType.Warning;
				case TraceLevel.Info:
					return EventLogEntryType.Information;
				default:
					return default;
			}
		}

#endif

		public IO.LogFileWriter FileWriter { get { return _FileWriter; } }
		IO.LogFileWriter _FileWriter;

		internal static void _WriteFile(string message, TraceLevel type)
		{
			// If LogStreamWriter is not null (check inside the function) then write to file.
			Current.FileWriter.WriteLine(message);
		}

		/// <summary>
		/// Writes log message to various destination types (console window, file, event and custom)
		/// </summary>
		/// <remarks>Appends line break.</remarks>
		public void WriteLog(string message, TraceLevel type)
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
#if NETSTANDARD
#elif NETCOREAPP
#else
			// If event logging is enabled and important then write event.
			if (WriteLogEvent != null && type != TraceLevel.Info)
				WriteLogEvent(message, type);
#endif
		}

		public static void WriteError(Exception ex)
		{
			if (ex == null)
				throw new ArgumentNullException(nameof(ex));
			Current.WriteLog(ex.ToString(), TraceLevel.Error);
		}

		
		public static void WriteWarning(string format, params object[] args)
		{
			Current.WriteLog(args != null && args.Length > 0 ? string.Format(format, args) : format, TraceLevel.Warning);
		}

		public static void WriteInfo(string format, params object[] args)
		{
			Current.WriteLog(args != null && args.Length > 0 ? string.Format(format, args) : format, TraceLevel.Info);
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
				if (s == null)
					s = rm;
				s = s.TrimEnd();
				if (!s.Contains(rm))
					s += " " + rm;
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
					subject = GetSubjectPrefix(ex, TraceLevel.Error) + ex.Message;
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
			algorithm.Dispose();
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

		public static string GetSubjectPrefix(Exception ex = null, TraceLevel? type = null)
		{
			var asm = Assembly.GetEntryAssembly();
			string s = "Unknown Entry Assembly";
			if (asm == null && ex != null)
			{
				var frames = new StackTrace(ex).GetFrames();
				if (frames != null)
				{
					for (int i = 0; i < frames.Length; i++)
					{
						var method = frames[i].GetMethod();
						if (method != null)
						{
							asm = method.DeclaringType.Assembly;
							break;
						}
					}
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

		public const string XLogHelperErrorSource = "X-LogHelper-ErrorSource";
		public const string XLogHelperErrorType = "X-LogHelper-ErrorType";
		public const string XLogHelperErrorCode = "X-LogHelper-ErrorCode";

		public string ExceptionInfo(Exception ex, string body, bool addHead = false)
		{
			//------------------------------------------------------
			// Body
			//------------------------------------------------------
			var s = "";
			if (addHead)
			{
				// Wrap into html element and specify UTF-8 encoding.
				s += "<html><head>" +
					"<meta charset=\"UTF-8\" />" +
					"<meta name=\"" + XLogHelperErrorSource + "\" content=\"" + ex.Source + "\">" +
					"<meta name=\"" + XLogHelperErrorType + "\" content=\"" + ex.GetType().FullName + "\">" +
					"<meta name=\"" + XLogHelperErrorCode + "\" content=\"" + ex.HResult + "\">" +
					"</head><body>";
			}
			if (!string.IsNullOrEmpty(body))
				s += "<div>" + body + "</div><br /><br />";
			//------------------------------------------------------
			AddStyle(ref s);
			//------------------------------------------------------
			StartTable(ref s);
			var ai = Configuration.AssemblyInfo.Entry;
			var rm = RunMode;
			if (!string.IsNullOrEmpty(rm))
				rm = " (" + rm + ")";
			AddRow(ref s, "Product");
			var name = ai.Company + " " + ai.Product + " " + ai.Version.ToString(4);
			ApplyRunModeSuffix(ref name);
			AddRow(ref s, "Name", name);
			AddRow(ref s, "Machine", System.Environment.MachineName);
			AddRow(ref s, "Username", System.Environment.UserName);

#if NETSTANDARD // .NET Standard
#elif NETCOREAPP // .NET Core
#else // .NET Framework
			// Add OS Version.
			//AddRow(ref s, "OS Version", System.Environment.OSVersion.ToString());
			var subKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
			var key = Microsoft.Win32.Registry.LocalMachine;
			var skey = key.OpenSubKey(subKey);
			var osProductName = string.Format("{0}", skey.GetValue("ProductName"));
			//var osEditionID = string.Format("{0}", skey.GetValue("EditionID"));
			var osReleaseId = string.Format("{0}", skey.GetValue("ReleaseId"));
			var osCurrentVersion = string.Format("{0}.{1}", skey.GetValue("CurrentMajorVersionNumber"), skey.GetValue("CurrentMinorVersionNumber"));
			if (osCurrentVersion.Trim() == ".")
				osCurrentVersion = string.Format("{0}", skey.GetValue("currentVersion"));
			var osCurrentBuildNumber = string.Format("{0}", skey.GetValue("CurrentBuildNumber"));
			var osUBR = string.Format("{0}", skey.GetValue("UBR"));
			skey.Close();
			var osVersion = string.Format("{0} {1} [Version {2}.{3}.{4}]",
				osProductName, osReleaseId,
					osCurrentVersion,
					osCurrentBuildNumber,
					osUBR
			);
			AddRow(ref s, "OS Version", osVersion);
#endif
			var asm = ai.Assembly;
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
				// If method was found then...
				if (uiMethod != null)
				{
					// Add rows.
					var uim = GetType().GetMethod(method, new Type[] { typeof(string).MakeByRefType() });
					var args = new object[] { s };
					uim.Invoke(this, args);
					s = (string)args[0];
				}
			}
			AddRow(ref s);
			EndTable(ref s);
			StartTable(ref s);
			ExceptionInfoRecursive(ref s, ex);
			EndTable(ref s);
			if (addHead)
				s += "</body></html>";
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
			if (FillLoaderException(ref s, ex))
			{ }
			else if (FillSqlException(ref s, ex))
			{ }
			else
				FillOther(ref s, ex);
			AddExceptionTrace(ref s, ex);
			// Append inner exception to the end.
			if (ex.InnerException != null)
				ExceptionInfoRecursive(ref s, ex.InnerException);
		}

		public static bool FillSqlException(ref string s, Exception ex)
		{
#if NETSTANDARD // .NET Standard
			return false;
#elif NETCOREAPP // .NET Core
			return false;
#else // .NET Framework
			var ex2 = ex as SqlException;
			if (ex2 == null)
				return false;
			for (int i = 0; i <= ex2.Errors.Count - 1; i++)
			{
				var err = ex2.Errors[i];
				var prefix = string.Format("Errors[{0}].", i + 1);
				Add(ex2, prefix + nameof(err.Source), err.Source);
				Add(ex2, prefix + nameof(err.Server), err.Server);
				Add(ex2, prefix + "Location", string.Format("Number {0}, Level {1}, State {2}, Line {3}", err.Number, err.Class, err.State, err.LineNumber));
				Add(ex2, prefix + nameof(err.Message), err.Message);
				if (!string.IsNullOrEmpty(err.Procedure))
				{
					Add(ex2, prefix + nameof(err.Procedure), err.Procedure);
					Add(ex2, prefix + ".Help", "SQL command to display lines of procedure: exec sp_helptext '" + err.Procedure + "'");
				}
			}
			return true;
#endif
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
				ExceptionInfoRecursive(ref s, ex4);
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
			var pis = ex.GetType().GetProperties();
			foreach (var pi in pis)
			{
				if (!pi.CanRead)
					continue;
				// Skip some properties, like "StackTrace" or "Data",
				// because they will be added by other parts of this code.
				if (new string[] {
					nameof(Exception.StackTrace),
					nameof(Exception.TargetSite),
					nameof(Exception.Data),
					nameof(Exception.Message),
					nameof(Exception.InnerException),
				}.Contains(pi.Name))
					continue;
				object value = null;
				// Sometimes retrieving exception value throws exception.
				// Wrap into try catch so that error reporting won't brake on this line.
				try
				{
					value = pi.GetValue(ex);
				}
				catch (Exception) { }
				if (value == null)
					continue;
				var key = "." + pi.Name;
				if (pi.Name == nameof(Exception.HResult) && value is int)
					value = string.Format("{0} (0x{1:X8})", value, value);
				parameters.Add(key, value);
			}
			AddParameters(ref s, parameters, TraceFormat.Html);
		}

		public static void Add(IDictionary data, object name, object value)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));
			var i = 0;
			// Loop until success.
			while (true)
			{
				var key = string.Format("{0}{1}", name,
					i == 0 ? "" : string.Format(" ({0})", i));
				// If list already contains this key then...
				if (data.Contains(key))
				{
					// Increase index and try again.
					i++;
					continue;
				}
				data.Add(key, value);
				break;
			}
		}

		public static void Add(Exception ex, string name, object value)
		{
			if (ex == null)
				throw new ArgumentNullException(nameof(ex));
			var prefix = ex.GetType().Name;
			var key = string.Format("{0}.{1}", prefix, name);
			Add(ex.Data, key, value);
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

		protected virtual void Dispose(bool disposing)
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
