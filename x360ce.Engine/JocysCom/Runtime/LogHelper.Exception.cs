using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace JocysCom.ClassLibrary.Runtime
{

	// Project -> Build -> [Advanced...] button -> Output - Debugging Information: 
	//    Full     - Stack Trace will have line numbers even when exception is thrown inside non main threads.
	//    Embedded - Stack Trace will have missing line numbers when thrown inside non-main threads.

	public class LogHelperEventArgs : CancelEventArgs
	{
		public Exception Exception { get; set; }
	}

	/// <summary>
	/// Write Exceptions to individual files.
	/// </summary>
	public partial class LogHelper
	{

		public string LogsFolder
		{
			get
			{
				if (!string.IsNullOrEmpty(OverrideLogFolder))
					return OverrideLogFolder;
				var ai = new Configuration.AssemblyInfo();
				var path = ai.GetAppDataFile(false, "Logs");
				return path.FullName;
			}
		}

		public string OverrideLogFolder = null;

		#region Handling

		public void InitExceptionHandlers(string overrideLogsFolder = null)
		{
			OverrideLogFolder = overrideLogsFolder;
			//if (LogExceptions && LogThreadExceptions)
			//	System.Windows.Forms.Application.ThreadException += Application_ThreadException;
			if (LogExceptions && LogUnhandledExceptions)
				AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			if (LogExceptions && LogFirstChanceExceptions)
				AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
			if (LogExceptions && LogUnobservedTaskExceptions)
				TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
		}

		public void UnInitExceptionHandlers()
		{
			//if (LogExceptions && LogThreadExceptions)
			//	System.Windows.Forms.Application.ThreadException -= Application_ThreadException;
			if (LogExceptions && LogUnhandledExceptions)
				AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
			if (LogExceptions && LogFirstChanceExceptions)
				AppDomain.CurrentDomain.FirstChanceException -= CurrentDomain_FirstChanceException;
			if (LogExceptions && LogUnobservedTaskExceptions)
				TaskScheduler.UnobservedTaskException -= TaskScheduler_UnobservedTaskException;
		}

		public event EventHandler<LogHelperEventArgs> WritingException;

		public void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
		{
			if (e == null)
				return;
			WriteException(e.Exception);
		}

		public void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			if (e == null)
				return;
			WriteException((Exception)e.ExceptionObject);
		}

		public void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
		{
			if (e == null)
				return;
			WriteException(e.Exception);
		}

		/// <summary>
		/// This is a "first chance exception", which means the debugger is simply notifying you
		/// that an exception was thrown, rather than that one was not handled.
		/// </summary>
		public void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
		{
			if (e == null || e.Exception == null)
				return;
			WriteException(e.Exception);
		}

		#endregion

		#region Exception

		/// <summary>Get native error code.</summary>
		public static int GetNativeErrorCode(Exception ex)
		{
			if (ex == null)
				throw new ArgumentNullException(nameof(ex));
			var code = 0;
			Win32Exception w32Ex;
			do
			{
				if ((w32Ex = ex as Win32Exception) != null)
					code = w32Ex.NativeErrorCode;
				ex = ex.InnerException;
			}
			// Do another check if inner exception is available.
			while (ex != null && code == 0);
			return code;
		}

		private static string GetClassName(Exception ex)
		{
			var method = typeof(Exception).GetMethod("GetClassName", BindingFlags.NonPublic | BindingFlags.Instance);
			return (string)method.Invoke(ex, null);
		}

		private static string getText(bool isHtml, string text)
		{
			return (isHtml) ? System.Net.WebUtility.HtmlEncode(text) : text;
		}

		private string ExceptionToString(Exception ex, bool needFileLineInfo, TraceFormat tf, out bool containsFileAndLineNumber)
		{
			containsFileAndLineNumber = false;
			var isHtml = (tf == TraceFormat.Html);
			var newLine = isHtml ? "<br />" + Environment.NewLine : Environment.NewLine;
			var builder = new StringBuilder(0xff);
			if (isHtml)
				builder.Append("<span class=\"Mono\">");
			builder.Append(getText(isHtml, GetClassName(ex)));
			if (!string.IsNullOrEmpty(ex.Message))
			{
				builder.Append(": ");
				builder.Append(getText(isHtml, ex.Message));
			}
			//builder.Append(newLine);
			var stackTrace = new StackTrace(ex, needFileLineInfo);
			StackTrace fullTrace = null;
			var startFrameIndex = 0;
			// If use unstable version.
			// Can loose information about original line of exception.
			if (ErrorUseNewStackTrace)
			{
				// Get full stack trace from root to the current line.
				fullTrace = new StackTrace(needFileLineInfo);
				// Get current class.
				var currentClass = fullTrace.GetFrame(0).GetMethod().DeclaringType;
				// Get deepest/top method from stack trace.
				var method = stackTrace.FrameCount > 0
					? stackTrace.GetFrame(0).GetMethod()
					: null;
				// Loop through full stack trace.
				for (var i = 0; i < fullTrace.FrameCount; i++)
				{
					// Get frame method.
					var m = fullTrace.GetFrame(i).GetMethod();
					if (m == null)
						continue;
					// If same method was found then...
					if (m.Equals(method))
					{
						// Start displaying stack trace from this method
						// when converting stack trace to string.
						startFrameIndex = i;
						break;
					}
					// If method of full frame is located inside this class then...
					if (m.DeclaringType.Equals(currentClass))
					{
						// Skip this frame.
						startFrameIndex = i + 1;
					}
				}
			}
			// Use full trace is exists.
			var trace = fullTrace ?? stackTrace;
			if (trace.FrameCount > 0)
			{
				builder.Append(newLine);
				builder.Append(TraceToString(trace, tf, startFrameIndex, out containsFileAndLineNumber));
			}
			//if (ex.InnerException != null)
			//{
			//	builder.Append(getText(isHtml, " ---> "));
			//	builder.Append(ExceptionToString(ex.InnerException, needFileLineInfo, tf));
			//	builder.Append(newLine);
			//}
			if (isHtml)
				builder.Append("</span>");
			return builder.ToString();
		}

		/// <summary>
		/// Get error line where method DeclaringType is Form.
		/// </summary>
		public static StackFrame GetFormStackFrame(Exception ex)
		{
			var trace = (ex == null)
				? new StackTrace(true)
				: new StackTrace(ex, true);
			for (var i = 0; i < trace.FrameCount; i++)
			{
				var frame = trace.GetFrame(i);
				if (IsFormStackFrame(frame))
					return frame;
			}
			return null;
		}

		/// <summary>
		/// Return true if method DeclaringType is Form.
		/// </summary>
		public static bool IsFormStackFrame(StackFrame sf)
		{
			if (sf == null)
				throw new ArgumentNullException(nameof(sf));
			var method = sf.GetMethod();
			if (method == null)
				return false;
			var declaringType = method.DeclaringType;
			if (declaringType == null)
				return false;
			var t = declaringType;
			while (t != null)
			{
				if (t.Name == "Form")
				{
					return true;
				}
				t = t.BaseType;
			}
			return false;
		}

		public static string TraceToString(StackTrace st, TraceFormat tf, int startFrameIndex, out bool containsFileAndLineNumber)
		{
			if (st == null)
				throw new ArgumentNullException(nameof(st));
			containsFileAndLineNumber = false;
			var isHtml = tf == TraceFormat.Html;
			var newLine = (isHtml) ? "<br />" + Environment.NewLine : Environment.NewLine;
			var flag = true;
			var flag2 = true;
			var builder = new StringBuilder(0xff);
			if (isHtml)
				builder.Append("<span class=\"Mono\">");
			for (var i = startFrameIndex; i < st.FrameCount; i++)
			{
				var frame = st.GetFrame(i);
				var method = frame.GetMethod();
				if (method != null)
				{
					if (flag2)
					{
						flag2 = false;
					}
					else
					{
						builder.Append(newLine);
					}
					if (isHtml)
					{
						builder.Append("&nbsp;&nbsp;&nbsp;<span style=\"color: #808080;\">at</span> ");
					}
					else
					{
						builder.Append("   at ");
					}
					var declaringType = method.DeclaringType;
					var isForm = IsFormStackFrame(frame);
					if (declaringType != null)
					{
						var ns = declaringType.Namespace.Replace('+', '.');
						var name = declaringType.Name.Replace('+', '.');
						if (isHtml)
						{
							if (isForm)
								builder.Append("<span style=\"font-weight: bold;\">");
							builder.Append(System.Net.WebUtility.HtmlEncode(ns));
							builder.Append(".");
							builder.AppendFormat("<span style=\"color: #2B91AF; \">{0}</span>", System.Net.WebUtility.HtmlEncode(name));
							if (isForm)
								builder.Append("</span>");
						}
						else
						{
							builder.AppendFormat("{0}.{1}", ns, name);
						}
						builder.Append(".");
					}
					if (isHtml && isForm)
						builder.Append("<span style=\"font-weight: bold; color: #800000;\">");
					builder.Append(method.Name);
					if ((method is MethodInfo) && ((MethodInfo)method).IsGenericMethod)
					{
						var genericArguments = ((MethodInfo)method).GetGenericArguments();
						builder.Append("[");
						var index = 0;
						var flag3 = true;
						while (index < genericArguments.Length)
						{
							if (!flag3)
							{
								builder.Append(",");
							}
							else
							{
								flag3 = false;
							}
							builder.Append(genericArguments[index].Name);
							index++;
						}
						builder.Append("]");
					}
					builder.Append("(");
					var parameters = method.GetParameters();
					var flag4 = true;
					for (var j = 0; j < parameters.Length; j++)
					{
						var param = parameters[j];
						if (!flag4)
						{
							builder.Append(", ");
						}
						else
						{
							flag4 = false;
						}
						var paramType = "<UnknownType>";
						var isClass = false;
						if (param.ParameterType != null)
						{
							isClass = param.ParameterType.IsClass;
							paramType = param.ParameterType.Name;
						}
						if (isHtml)
						{
							if (isClass)
								builder.Append("<span style=\"color: #2B91AF; \">");
							builder.Append(System.Net.WebUtility.HtmlEncode(paramType));
							if (isClass)
								builder.Append("</span>");
							builder.Append(" ");
							builder.Append(System.Net.WebUtility.HtmlEncode(paramType));
						}
						else
						{
							builder.AppendFormat("{0} {1}", paramType, param.Name);
						}
					}
					builder.Append(")");
					if (isHtml && isForm)
						builder.Append("</span>");
					if (flag && (frame.GetILOffset() != -1))
					{
						string fileName = null;
						try
						{
							fileName = frame.GetFileName();
						}
						catch (NotSupportedException)
						{
							flag = false;
						}
						catch (SecurityException)
						{
							flag = false;
						}
						if (fileName != null)
						{
							containsFileAndLineNumber = true;
							var lineNumber = frame.GetFileLineNumber();
							var columnNumber = frame.GetFileColumnNumber();
							string format;
							if (isHtml)
							{
								builder.Append("<br />&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
								var fnHtml = System.Net.WebUtility.HtmlEncode(fileName);
								var fnColor = isForm ? "#800000" : "#000000";
								var fnSpan = string.Format("<span style=\"color: {0};\">{1}</span>", fnColor, fnHtml);
								// Add file as link.
								var addLink = false;
								if (addLink)
								{
									var fnLink = System.Net.WebUtility.UrlEncode(fileName.Replace("\\", "/")).Replace("+", "%20");
									builder.AppendFormat("<a href=\"file:///{0}\" style=\"text-decoration: none; color: #000000;\">{1}</a>", fnLink, fnSpan);
								}
								else
								{
									builder.AppendFormat(fnSpan);
								}
								builder.AppendFormat("<span style=\"color: #808080;\">,</span> {0}", lineNumber);
								builder.AppendFormat("<span style=\"color: #808080;\">:{0}</span>", columnNumber);
							}
							else
							{
								format = " in {0}, {1}:{2}";
								builder.AppendFormat(format, fileName, lineNumber, columnNumber);
							}
						}
					}
				}
			}
			if (isHtml || tf == TraceFormat.TrailingNewLine)
				builder.Append(newLine);
			if (isHtml)
				builder.Append("</span>");
			return builder.ToString();
		}

		#endregion

		#region ExceptionToText

		public static string ExceptionToText(Exception ex)
		{
			var message = "";
			if (ex == null)
				throw new ArgumentNullException(nameof(ex));
			AddExceptionMessage(ex, ref message);
			if (ex.InnerException != null)
				AddExceptionMessage(ex.InnerException, ref message);
			return message;
		}

		/// <summary>Add information about missing libraries and DLLs</summary>
		private static void AddExceptionMessage(Exception ex, ref string message)
		{
			// Add separator if message already have content.
			if (message.Length > 0)
				message += "===============================================================\r\n";
			message += ex.ToString() + "\r\n";
			// Add extra exception details.
			var s = "";
			AddParameters(ref s, ex.Data, TraceFormat.TrailingNewLine);
#if NETSTANDARD // .NET Standard
#elif NETCOREAPP // .NET Core
#else // .NET Framework
			// Exception string to add.
			var ex1 = ex as System.Configuration.ConfigurationErrorsException;
			if (ex1 != null)
			{
				s += string.Format("FileName: {0}\r\n", ex1.Filename);
				s += string.Format("Line: {0}\r\n", ex1.Line);
			}
#endif
			var ex2 = ex as ReflectionTypeLoadException;
			if (ex2 != null)
			{
				foreach (var x in ex2.LoaderExceptions)
					s += x.Message + "\r\n";
			}
			// If details available then...
			if (s.Length > 0)
			{
				message += "===============================================================\r\n";
				message += s;
			}

		}

		#endregion

	}
}
