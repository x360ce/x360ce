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
using System.Net;
using System.Net.Mail;

namespace JocysCom.ClassLibrary.Runtime
{
	public partial class LogHelper
	{
		public JocysCom.ClassLibrary.Mail.SmtpClientEx Smtp;

		public LogHelper()
		{
			Smtp = JocysCom.ClassLibrary.Mail.SmtpClientEx.Current;
		}


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

		public static string GetSubjectPrefix(Exception ex)
		{
			Assembly asm = Assembly.GetEntryAssembly();
			string a = "Unknown Entry Assembly";
			if (asm == null)
			{
				if (ex != null)
				{
					StackFrame[] frames = new StackTrace(ex).GetFrames();
					if (frames != null && frames.Length > 0)
					{
						asm = frames[0].GetMethod().DeclaringType.Assembly;
					}
				}
				else
				{
				}
			}
			if (asm == null)
			{
				asm = Assembly.GetCallingAssembly();
			}
			if (asm != null)
			{
				a = asm.GetName().Name.Replace("Dac.Volante.", "");
			}
			string s = string.Format("{0} Error", a);
			ApplyRunModeSuffix(ref s);
			s += ": ";
			return s;
		}

		public void WriteException(Exception ex, int maxFiles, string logsFolder, bool writeAsHtml)
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
			var content = writeAsHtml ? ExceptionInfo(ex, "") : ex.ToString();
			System.IO.File.AppendAllText(fileName, content);
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
				AddRow(ref s, "Executable", asm.Location);
				AddRow(ref s, "Build Date", Configuration.AssemblyInfo.GetBuildDateTime(asm.Location).ToString("yyyy-MM-dd HH:mm:ss"));
			}
			UserInfo(ref s);
			PageInfo(ref s);
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

		#region Web Page Info

		public virtual void PageInfo(ref string s)
		{
			var context = System.Web.HttpContext.Current;
			if (context != null)
			{
				// Form controls.
				var pg = (Page)System.Web.HttpContext.Current.Handler;
				Control[] controls = null;
				//Need to find page on this
				if (pg != null)
				{
					AddRow(ref s, "Page");
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
				var request = context.Request;
				if (request != null)
				{
					AddRow(ref s, "Request");
					AddRow(ref s, "User IP", request.UserHostName);
					AddRow(ref s, "Request.Url", request.Url.ToString());
					if (request.Form.Keys.Count > 0)
					{
						AddRow(ref s, "Request.Form.Keys");
					}
					foreach (string key in request.Form.Keys)
					{
						if (key == "__VIEWSTATE")
						{
							AddRow(ref s, key, request.Form[key]);
						}
						else
						{
							string value = request.Form[key];
							if (key.EndsWith("Pan") && !string.IsNullOrEmpty(value))
							{
								value = GetMasked(value);
							}
							AddRow(ref s, key, value);
						}
					}
					if (request.QueryString.HasKeys())
					{
						if (request.QueryString.Count > 0)
							AddRow(ref s, "Request.QueryString");
						foreach (string key in request.QueryString)
						{
							string value = request.QueryString[key];
							if (key.EndsWith("Pan") && !string.IsNullOrEmpty(value))
							{
								value = GetMasked(value);
							}
							AddRow(ref s, key, value);
						}
					}
				}
				var cookies = request.Cookies;
				if (cookies.Keys.Count > 0)
				{
					AddRow(ref s, "Cookies");
					foreach (string cookieKey in cookies.Keys)
					{
						var cookie = cookies[cookieKey];
						AddRow(ref s, cookie.Name, cookie.Value);
					}
				}

			}
		}

		public virtual void UserInfo(ref string s)
		{
			// get user info and return it as formatted html string
			AddRow(ref s, "Session");
			//var fa = System.Web.Security.FormsAuthentication.IsEnabled;
			//var user = System.Web.Security.Membership.GetUser();
			//var roles = System.Web.Security.Roles.GetRolesForUser();
			var now = DateTime.Now;
			var startTime = Process.GetCurrentProcess().StartTime;
			AddRow(ref s, "Current Time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
			AddRow(ref s, "Running Since", startTime.ToString("yyyy-MM-dd HH:mm:ss"));
			AddRow(ref s, "Running For", (now - startTime).ToString());
			var connections = ConfigurationManager.ConnectionStrings;
			foreach (ConnectionStringSettings item in connections)
			{
				string connectionString;
				if (string.Compare(item.ProviderName, "System.Data.EntityClient", true) == 0)
				{
					// Use entity connection.
					var e = new System.Data.EntityClient.EntityConnection(item.ConnectionString);
					connectionString = e.StoreConnection.ConnectionString;
				}
				else
				{
					// Use classic connection.
					connectionString = item.ConnectionString;
				}
				AddConnection(ref s, item.Name, connectionString);
			}
		}

		#endregion

		#region Preview

		public System.Net.Mail.MailMessage GetMailPreview(MailMessage message)
		{
			MailMessage mail = new MailMessage();
			mail.IsBodyHtml = true;
			ApplyRecipients(mail, message.From, Smtp.ErrorRecipients);
			var subject = message.Subject;
			ApplyRunModeSuffix(ref subject);
			mail.Subject = subject;
			string testBody = "";
			testBody += "In LIVE mode this email would be sent:<br />\r\n";
			foreach (var item in message.To)
			{
				testBody += "To:&nbsp;" + System.Web.HttpUtility.HtmlEncode(item.ToString()) + "<br />\r\n";
			}
			foreach (var item in message.CC)
			{
				testBody += "Cc:&nbsp;" + System.Web.HttpUtility.HtmlEncode(item.ToString()) + "<br />\r\n";
			}
			foreach (var item in message.Bcc)
			{
				testBody += "Bcc:&nbsp;" + System.Web.HttpUtility.HtmlEncode(item.ToString()) + "<br />\r\n";
			}

			testBody += "<hr />\r\n";
			var attachments = message.Attachments;
			if (attachments != null && attachments.Count() > 0)
			{
				testBody += "These files would be attached:<br />\r\n";
				if (attachments != null && attachments.Count() > 0)
				{
					for (int ctr = 0; ctr <= attachments.Count() - 1; ctr++)
					{
						string fileName = attachments[ctr].Name;
						if (fileName.Length > 3 && fileName.ToLower().Substring(fileName.Length - 4) == ".ics")
						{
							mail.Attachments.Add(attachments[ctr]);
						}
						testBody += "&nbsp;&nbsp;&nbsp;&nbsp;" + System.Web.HttpUtility.HtmlEncode(fileName);
						testBody += "<br />\r\n";
					}
				}
			}
			if (message.IsBodyHtml)
			{
				testBody += message.Body;
			}
			else
			{
				testBody += "<pre>";
				testBody += System.Web.HttpUtility.HtmlEncode(message.Body);
				testBody += "</pre>";
			}
			mail.Body = testBody;
			return mail;
		}

		public static void ApplyRecipients(MailMessage mail, string addFrom, string addTo, string addCc = null, string addBcc = null)
		{
			ApplyRecipients(mail, new MailAddress(addFrom), addTo, addCc, addBcc);
		}

		public static void ApplyRecipients(MailMessage mail, MailAddress addFrom, string addTo, string addCc = null, string addBcc = null)
		{
			mail.From = addFrom;
			string[] list = null;
			list = addTo.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
			if (!string.IsNullOrEmpty(addTo))
			{
				foreach (string item in list)
				{
					if (string.IsNullOrEmpty(item.Trim())) continue;
					mail.To.Add(new MailAddress(item.Trim()));
				}
			}
			if (!string.IsNullOrEmpty(addCc))
			{
				list = addCc.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string item in list)
				{
					if (string.IsNullOrEmpty(item.Trim())) continue;
					mail.CC.Add(new MailAddress(item.Trim()));
				}
			}
			if (!string.IsNullOrEmpty(addBcc))
			{
				list = addBcc.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string item in list)
				{
					if (string.IsNullOrEmpty(item.Trim())) continue;
					mail.Bcc.Add(new MailAddress(item.Trim()));
				}
			}
		}

		void ApplyAttachments(MailMessage message, params Attachment[] files)
		{

			if (files == null) return;
			for (int i = 0; i < files.Count(); i++)
			{
				//    string file = files[i].Name;
				//    if (string.IsNullOrEmpty(file)) continue;
				//    if (System.IO.File.Exists(file))
				//    {
				//        // Specify as "application/octet-stream" so attachment will never will be embedded in body of email.
				//        System.Net.Mail.Attachment att = new System.Net.Mail.Attachment(file, "application/octet-stream");
				message.Attachments.Add(files[i]);
				//}
			}
		}

		public static EmailResult EmailValid(string email)
		{
			// evaluate email address for formal validity
			email = email.Trim();
			if (string.IsNullOrEmpty(email)) return EmailResult.Empty;
			System.Text.RegularExpressions.Regex objRegX = new System.Text.RegularExpressions.Regex("^[\\w-\\.\\'+&]+@([\\w-]+\\.)+[\\w-]{2,6}$", System.Text.RegularExpressions.RegexOptions.None);
			string[] emails = null;
			emails = email.Split(';');
			// take care of list of addresses separated by semicolon
			foreach (string s in emails)
			{
				var sEmail = s.Trim();
				if (string.IsNullOrEmpty(sEmail))
				{
					//The email address cannot end with a semicolon"
					return EmailResult.Semicolon;
				}
				if (!objRegX.IsMatch(sEmail))
				{
					//"Not a valid email address."
					return EmailResult.Invalid;
				}
			}

			// If we got here, email is OK.
			return EmailResult.OK;
		}

		public void SendMail(string @to, string subject, string body, bool isBodyHtml = false, bool preview = false, bool rethrow = false)
		{
			SendMailFrom(Smtp.SmtpFrom, @to, null, null, subject, body, isBodyHtml, preview, rethrow, new Attachment[0]);
		}

		public void SendMailFrom(string @from, string @to, string cc, string bcc, string subject, string body, bool isBodyHtml = false, bool preview = false, bool rethrow = false)
		{
			SendMailFrom(@from, @to, cc, bcc, subject, body, isBodyHtml, preview, rethrow, new Attachment[0]);
		}

		public void SendMailFrom(string @from, string @to, string cc, string bcc, string subject, string body, bool isBodyHtml, bool preview, bool rethrow, string[] attachments)
		{
			SendMailFrom(@from, @to, cc, bcc, subject, body, isBodyHtml, preview, rethrow, GetAttachments(attachments));
		}

		/// <summary>
		/// Mail will be sent to error recipient if not LIVE.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="cc"></param>
		/// <param name="bcc"></param>
		/// <param name="subject"></param>
		/// <param name="body"></param>
		/// <param name="isBodyHtml"></param>
		/// <param name="preview">Force preview on LIVE system.</param>
		/// <param name="rethrow">Throw exception if sending fails. Must be set to false when sending exceptions.</param>
		/// <param name="attachments"></param>
		public void SendMailFrom(string @from, string @to, string cc, string bcc, string subject, string body, bool isBodyHtml, bool preview, bool rethrow, Attachment[] attachments)
		{
			// Re-throw - throw the error again to catch by a caller
			try
			{
				var mail = new MailMessage();
				ApplyRecipients(mail, @from, @to, cc, bcc);
				ApplyAttachments(mail, attachments);
				mail.IsBodyHtml = isBodyHtml;
				mail.Subject = subject;
				mail.Body = body;
				if (!IsLive || preview)
				{
					mail = GetMailPreview(mail);
				}
				Smtp.SendMessage(mail);
			}
			catch (Exception ex)
			{
				if (!string.IsNullOrEmpty(@to) && !ex.Data.Contains("Mail.To")) ex.Data.Add("Mail.To", @to);
				if (!string.IsNullOrEmpty(cc) && !ex.Data.Contains("Mail.Cc")) ex.Data.Add("Mail.Cc", cc);
				if (!string.IsNullOrEmpty(bcc) && !ex.Data.Contains("Mail.Bcc")) ex.Data.Add("Mail.Bcc", bcc);
				if (!string.IsNullOrEmpty(subject) && !ex.Data.Contains("Mail.Subject")) ex.Data.Add("Mail.Subject", subject);
				// Will be processed by the caller.
				if (rethrow)
				{
					throw;
				}
				else
				{
					ProcessException(ex);
				}
			}
		}

		public static Attachment[] GetAttachments(string[] files)
		{
			if (files == null) return null;
			var attachments = new List<Attachment>();
			for (int i = 0; i < files.Count(); i++)
			{
				string file = files[i];
				if (string.IsNullOrEmpty(file)) continue;
				if (System.IO.File.Exists(file))
				{
					// Specify as "application/octet-stream" so attachment will never will be embedded in body of email.
					var att = new System.Net.Mail.Attachment(file, "application/octet-stream");
					attachments.Add(att);
				}
			}
			return attachments.ToArray();
		}

		/// <summary>
		/// Mail will be sent to error recipient if not LIVE.
		/// </summary>
		/// <param name="message"></param>
		public void SendEmailWithCopyToErrorRecipients(MailMessage message)
		{
			var mail = IsLive ? message : GetMailPreview(message);
			Smtp.SendMessage(mail);
		}

		/// <summary>
		/// Send email to developers and show the exception box
		/// </summary>
		public string ProcessException(Exception ex, string subject = null, bool processExtraAction = true)
		{
			var allowSend = Smtp.AllowToSendException(ex);
			var body = ExceptionInfo(ex, "");
			if (allowSend && Smtp.ErrorNotifications)
			{
				Smtp.SendErrorEmail(ex, subject, body);
			}
			// Execute extra exception actions.
			var extra = ProcessExceptionExtra;
			if (processExtraAction && extra != null)
			{
				extra(ex);
			}
			return body;
		}

		#endregion


	}
}
