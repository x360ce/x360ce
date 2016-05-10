using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Data;
using System.Collections;
using System.Configuration;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Web.UI.WebControls;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace JocysCom.ClassLibrary.Runtime
{
	public partial class LogHelper
	{

		#region Parse

		public static bool ParseBool(string name, bool defaultValue)
		{
			var v = ConfigurationManager.AppSettings[name];
			return (v == null) ? defaultValue : bool.Parse(v);
		}

		public static string ParseString(string name, string defaultValue)
		{
			var v = ConfigurationManager.AppSettings[name];
			return (v == null) ? defaultValue : v;
		}

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
			if (!string.IsNullOrEmpty(ex.Message))
			{
				var useHtml = ParseBool("ErrorHtmlException", true);
				if (useHtml)
				{
					AddException(ref s, JocysCom.ClassLibrary.Helper.ExceptionToString(ex, true, JocysCom.ClassLibrary.TraceFormat.Html));
				}
				else
				{
					AddException(ref s, "<pre>" + ex.ToString() + "</pre>");
				}
			}
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
		public delegate void CustomInfoDelegate(ref string message);
		public static WriteLogDelegate WriteLogCustom;

		public static void WriteException(Exception ex, int maxFiles, string logsFolder, bool writeAsHtml, CustomInfoDelegate customUserInfo, CustomInfoDelegate customPageInfo)
		{
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
			var fileTime = DateTime.Now;
			var fileName = string.Format("{0}\\{1}_{2:yyyyMMdd_HHmmss.ffffff}.{3}", di.FullName, prefix, fileTime, ext);
			var content = writeAsHtml ? ExceptionInfo(ex, "", customUserInfo, customPageInfo) : ex.ToString();
			System.IO.File.AppendAllText(fileName, content);
		}

		public static string ExceptionInfo(Exception ex, string body, CustomInfoDelegate customUserInfo, CustomInfoDelegate customPageInfo)
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
			var rm = ParseString("RunMode", "");
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
				AddRow(ref s, "Name", ai.Company + " " + ai.Product + " " + ai.Version.ToString(4) + rm);
			}
			AddRow(ref s, "Machine", System.Environment.MachineName);
			AddRow(ref s, "Username", System.Environment.UserName);
			if (asm != null)
			{
				AddRow(ref s, "Executable", asm.Location);
				AddRow(ref s, "Build Date", Configuration.AssemblyInfo.GetBuildDateTime(asm.Location).ToString("yyyy-MM-dd HH:mm:ss"));
			}
			if (customUserInfo != null) customUserInfo(ref s);
			if (customPageInfo != null) customPageInfo(ref s);
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
			StackFrame frame = JocysCom.ClassLibrary.Helper.GetFormStackFrame(ex);
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

		#region Web Page Info

		public static void DefaultPageInfo(ref string s)
		{
			var req = System.Web.HttpContext.Current.Request;
			AddRow(ref s, "Request");
			AddRow(ref s, "Request.Url", req.Url.ToString());
			if (req.Form.Keys.Count > 0)
				AddRow(ref s, "Request.Form.Keys");
			foreach (string key in req.Form.Keys)
			{
				if (key == "__VIEWSTATE")
				{
					AddRow(ref s, key, req.Form[key]);
				}
				else
				{
					string value = req.Form[key];
					if (key.EndsWith("Pan") && !string.IsNullOrEmpty(value))
					{
						value = GetMasked(value);
					}
					AddRow(ref s, key, value);
				}
			}
			if (req.QueryString.HasKeys())
			{
				if (req.QueryString.Count > 0)
					AddRow(ref s, "Request.QueryString");
				foreach (string key in req.QueryString)
				{
					string value = req.QueryString[key];
					if (key.EndsWith("Pan") && !string.IsNullOrEmpty(value))
					{
						value = GetMasked(value);
					}
					AddRow(ref s, key, value);
				}
			}
			// Form controls.
			var pg = (Page)System.Web.HttpContext.Current.Handler;
			Control[] controls = null;
			//Need to find page on this
			if (pg != null)
			{
			// find the form
				foreach (Control ctrl in pg.Controls)
				{
					if (object.ReferenceEquals(ctrl.GetType(), typeof(HtmlForm)))
					{
						controls = Controls.ControlsHelper.GetAll<Control>((HtmlForm)ctrl);
					}
					else if (object.ReferenceEquals(ctrl.GetType().BaseType.BaseType, typeof(MasterPage)))
					{
						controls = Controls.ControlsHelper.GetAll<Control>((MasterPage)ctrl);
					}
				}
				if (controls != null)
				{
					if (controls.Length > 0)
					{
						AddRow(ref s, "Page.Controls");
					}
					foreach (Control ctrl in controls)
					{
						bool show = false;
						var type = ctrl.GetType();
						var interfaces = type.GetInterfaces();
						var values = new StringBuilder();
						if (interfaces.Contains(typeof(IPostBackDataHandler)))
						{
							if (interfaces.Contains(typeof(ITextControl)))
							{
								values.AppendFormat("Text={0}\r\n", ((ITextControl)ctrl).Text);
								show = true;
							}
							if (interfaces.Contains(typeof(ICheckBoxControl)))
							{
								values.AppendFormat("Checked={0}\r\n", ((ICheckBoxControl)ctrl).Checked);
								show = true;
							}
							if (typeof(ListControl).IsAssignableFrom(type))
							{
								var items = ((ListControl)ctrl).Items;
								foreach (ListItem item in items)
								{
									if (item != null)
									{
										var selected = item.Selected ? ", Selected = true" : "";
										values.AppendFormat("Value={0}, Text={1}, {2}\r\n", item.Value, item.Text, selected);
									}
								}
								show = true;
							}
						}
						if (typeof(HyperLink).IsAssignableFrom(type))
						{
							values.AppendFormat("NavigateUrl={0}\r\n", ((HyperLink)ctrl).NavigateUrl);
							show = true;
						}
						if (show)
						{
							AddRow(ref s, ctrl.ID, values.ToString());
						}
					}
				}
			}
			if (req.Cookies.Keys.Count > 0)
			{
				AddRow(ref s, "Cookies");
				foreach (string cookieKey in req.Cookies.Keys)
				{
					var cookie = req.Cookies[cookieKey];
					AddRow(ref s, cookie.Name, cookie.Value);
				}
			}
		}

		public static void DefaultUserInfo(ref string s)
		{
			
		}

		#endregion

	}
}
