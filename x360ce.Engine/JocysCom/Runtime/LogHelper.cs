using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Data;
using System.Collections;
using System.Configuration;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Net;
using System.Collections.Generic;

namespace JocysCom.ClassLibrary.Runtime
{
	public partial class LogHelper
	{

		private static LogHelper _Current;
		private static object currentLock = new object();
		public static LogHelper Current
		{
			get
			{
				lock (currentLock)
				{
					return _Current = _Current ?? new LogHelper();
				}
			}
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

		public static IPAddress ParseIPAddress(string name, IPAddress defaultValue)
		{
			var v = ConfigurationManager.AppSettings[name];
			return (v == null) ? defaultValue : IPAddress.Parse(v);
		}

		public static string RunMode { get { return ParseString("RunMode", "TEST"); } }
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
					string v;
					object pv = parameters[key];
					if (pv == null)
					{
						v = "<pre>null</pre>";
					}
					else
					{
						try
						{
							v = pv.ToString();
							if (v.Contains("<?xml"))
							{
								v = "<pre>" + System.Web.HttpUtility.HtmlEncode(v) + "</pre>";
							}
						}
						catch (Exception ex2)
						{
							v = ex2.Message;
						}
					}
					AddRow(ref s, key, v);
				}
			}
		}

		public static void AddStyle(ref string s)
		{
			s += "<style type=\"text/css\">\r\n";
			s += "table tr td { font-family: Tahoma; font-size: 10pt; white-space:nowrap; }\r\n";
			s += ".Head { font-weight: bold; }\r\n";
			s += ".Name { padding-left: 16px; }\r\n";
			s += ".Value { widht: 100%; }\r\n";
			s += ".Ex { font-family: Courier New; font-size: 10pt; }\r\n";
			s += "</style>\r\n";
		}

		public static void AddRow(ref string s)
		{
			s += string.Format("<tr><td colspan=\"2\"> </td></tr>");
		}

		public static void AddRow(ref string s, string name)
		{
			s += string.Format("<tr><td colspan=\"2\" class=\"Head\">{0}</td></tr>", name);
		}

		public static void AddRow(ref string s, string name, string value)
		{
			string sep = "";
			if (!string.IsNullOrEmpty(name))
				sep = ":";
			s += string.Format("<tr><td class=\"Name\" valign=\"top\">{0}{1}</td><td>{2}</td></tr>", name, sep, value);
		}

		protected static void AddException(ref string s, Exception ex)
		{
			if (ex.Data.Count > 0)
			{
				AddParameters(ref s, ex.Data);
				s += "</table>";
				s += "<table border=\"0\" cellspacing=\"2\">";
			}
			// if exception is not empty then 
			//if (!string.IsNullOrEmpty(ex.Message))
			//{
			var useHtml = ParseBool("LogHelper_ErrorHtmlException", true);
			if (useHtml)
			{
				AddException(ref s, ExceptionToString(ex, true, JocysCom.ClassLibrary.TraceFormat.Html));
			}
			else
			{
				AddException(ref s, "<pre>" + ex.ToString() + "</pre>");
			}
			//}
		}

		protected static void AddException(ref string s, string name)
		{
			s += string.Format("<tr><td colspan=\"2\" class=\"Ex\">{0}</td></tr>", name);
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

		#region Exceptions

		public delegate void WriteLogDelegate(string message, EventLogEntryType type);
		public static WriteLogDelegate WriteLogCustom;

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

		// Use dictionary to prevent mass writings of exceptions into disk.
		Dictionary<Type, DateTime> exeptionTimes = new Dictionary<Type, DateTime>();
		Dictionary<Type, int> exeptionCount = new Dictionary<Type, int>();

		public void WriteException(Exception ex, int maxFiles, string logsFolder, bool writeAsHtml)
		{
			var ev = WritingException;
			var le = new LogHelperEventArgs() { Exception = ex };
			if (ev != null)
				ev(this, le);
			if (le.Cancel)
				return;
			// Must wrap into lock so that process won't attempt to delete/write same file twice from different threads.
			lock (WriteLock)
			{
				var n = DateTime.Now;
				var type = ex.GetType();
				if (exeptionTimes.ContainsKey(type))
				{
					// Increase counter.
					exeptionCount[type] = exeptionCount[type] + 1;
					// Do not allow write if not enough time passed.
					if (n.Subtract(exeptionTimes[type]).Milliseconds < 500)
						return;
				}
				else
				{
					exeptionTimes.Add(type, n);
					exeptionCount.Add(type, 1);
				}
				var count = exeptionCount[type];
				// Reset count and update last write time.
				exeptionCount[type] = 0;
				exeptionTimes[type] = n;
				// Create file.
				var prefix = "FCE_" + ex.GetType().Name;
				var ext = writeAsHtml ? "htm" : "txt";
				var di = new System.IO.DirectoryInfo(logsFolder);
				if (di.Exists)
				{

					var files = di.GetFiles(prefix + "*." + ext).OrderBy(x => x.CreationTime).ToArray();
					if (maxFiles > 0 && files.Count() > 0 && files.Count() > maxFiles)
					{
						// Remove oldest file.
						files[0].Delete();
					}

				}
				else
				{
					di.Create();
				}
				//var fileTime = JocysCom.ClassLibrary.HiResDateTime.Current.Now;
				var fileTime = HiResDateTime.Current.Now;
				var fileName = string.Format("{0}\\{1}_{2:yyyyMMdd_HHmmss.ffffff}{3}.{4}",
					di.FullName, prefix, fileTime, count == 1 ? "" : "." + count.ToString(), ext);
				var fi = new System.IO.FileInfo(fileName);
				var content = writeAsHtml ? ExceptionInfo(ex, "") : ex.ToString();
				System.IO.File.AppendAllText(fileName, content);
			}
		}

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
				var ai = new JocysCom.ClassLibrary.Configuration.AssemblyInfo(asm);
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
			s += "<table border=\"0\" cellspacing=\"2\">";
			ExceptionInfoRecursive(ref s, ex);
			//------------------------------------------------------
			s += "</table>";
			return s;
		}

		public static void ExceptionInfoRecursive(ref string s, Exception ex)
		{
			if (ex == null) return;
			StackFrame frame = GetFormStackFrame(ex);
			AddRow(ref s, "Exception");
			AddRow(ref s, "Exception Date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
			if (frame != null && frame.GetMethod() != null && frame.GetMethod().DeclaringType != null)
			{
				Type dt = frame.GetMethod().DeclaringType;
				AddRow(ref s, "Target.Type", dt.FullName);
				AddRow(ref s, "Target.Name", frame.GetMethod().Name);
			}
			else if (ex.TargetSite != null)
			{
				AddRow(ref s, "Target.Type", ex.TargetSite.DeclaringType.ToString());
				AddRow(ref s, "Target.Name", ex.TargetSite.Name);
			}
			s += "</table>";
			s += "<table border=\"0\" cellspacing=\"2\">";
			FillLoaderException(ref s, ex);
			FillSocketException(ref s, ex);
			FillThreadAbortException(ref s, ex);
			FillSqlException(ref s, ex);
			if (ex.InnerException != null)
			{
				var s1 = "";
				ExceptionInfoRecursive(ref s1, ex.InnerException);
				var key = string.Format("InnerException");
				if (!ex.Data.Contains(key)) ex.Data.Add(key, s1);
			}
			AddException(ref s, ex);
		}


		public static void FillSocketException(ref string s, Exception ex)
		{
			if (ex.GetType().Equals(typeof(System.Net.Sockets.SocketException)))
			{
				var exIn = (System.Net.Sockets.SocketException)ex;
				var key = "SocketException.NativeErrorCode";
				if (ex.Data.Contains(key)) return;
				ex.Data.Add(key, exIn.NativeErrorCode);
				ex.Data.Add("SocketException.SocketErrorCode", exIn.SocketErrorCode);
			}
		}

		public static void FillThreadAbortException(ref string s, Exception ex)
		{
			if (ex.GetType().Equals(typeof(System.Threading.ThreadAbortException)))
			{
				var exIn = (System.Threading.ThreadAbortException)ex;
				var key = "ThreadAbortException.ExceptionState";
				if (ex.Data.Contains(key)) return;
				ex.Data.Add(key, exIn.ExceptionState);
			}
		}

		public static void FillSqlException(ref string s, Exception ex)
		{
			// Add Details
			if (ex.GetType().Equals(typeof(SqlException)))
			{
				var exIn = (SqlException)ex;
				for (int i = 0; i <= exIn.Errors.Count - 1; i++)
				{
					SqlError err = exIn.Errors[i];
					var prefix = string.Format("SqlException[{0}]", i + 1);
					var key = prefix + ".Source";
					if (ex.Data.Contains(key)) continue;
					ex.Data.Add(key, err.Source);
					ex.Data.Add(prefix + ".Server", err.Server);
					ex.Data.Add(prefix + ".Location", string.Format("Number {0}, Level {1}, State {2}, Line {3}", err.Number, err.Class, err.State, err.LineNumber));
					ex.Data.Add(prefix + ".Message", err.Message);
					if (!string.IsNullOrEmpty(err.Procedure))
					{
						ex.Data.Add(prefix + ".Procedure", err.Procedure);
						ex.Data.Add(prefix + ".Help", "SQL command to display lines of procedure: exec sp_helptext '" + err.Procedure + "'");
					}
				}
			}
		}

		public static void FillLoaderException(ref string s, Exception ex)
		{
			if (ex.GetType().Equals(typeof(ReflectionTypeLoadException)))
			{
				var exIn = (ReflectionTypeLoadException)ex;
				var i = 0;
				foreach (Exception ex4 in exIn.LoaderExceptions)
				{
					var s1 = "";
					ExceptionInfoRecursive(ref s1, ex4);
					var key = "LoaderExceptions[" + i.ToString() + "]";
					if (ex.Data.Contains(key)) return;
					ex.Data.Add(key, s1);
					i++;
				}
			}
		}

		#endregion

	}
}
