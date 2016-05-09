using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Resources;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Globalization;
using System.Security;
using System.Management;

namespace JocysCom.ClassLibrary
{
	public class Helper : IDisposable
	{
		#region Control Resources

		/// <summary>
		/// Write application header title to CLI interface.
		/// </summary>
		public static void WriteAppHeader()
		{
			var assembly = System.Reflection.Assembly.GetExecutingAssembly();
			WriteAppHeader(assembly);
		}

		public static void WriteAppHeader(System.Reflection.Assembly assembly)
		{
			// Write title.
			// Microsoft (R) SQL Server Database Publishing Wizard 1.1.1.0
			// Copyright (C) Microsoft Corporation. All rights reserved.
			var a = new JocysCom.ClassLibrary.Configuration.AssemblyInfo(assembly);
			Console.WriteLine(a.Title + " " + a.Version.ToString());
			Console.WriteLine(a.Copyright);
			Console.WriteLine(a.Description);
		}

		/// <summary>
		/// Write help text from help.txt file.
		/// </summary>
		public static void WriteAppHelp()
		{
			Console.Write(GetTextResource("Documents/Help.txt"));
		}

		public static string GetTextResource(string name)
		{
			return GetResource<string>(name);
		}

		public static string GetTextResource(Assembly assembly, string name)
		{
			return GetResource<string>(assembly, name);
		}

		/// <summary>
		/// Find resource in all loaded assemblies.
		/// </summary>
		public static T FindResource<T>(string name)
		{
			object results = default(T);
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (var assembly in assemblies)
			{
				T o = FindResource<T>(assembly, name);
				if (o != null) return o;
			}
			return default(T);
		}

		/// <summary>
		/// Find resource in all loaded assemblies.
		/// </summary>
		public static T FindResource<T>(Assembly assembly, string name)
		{
			object results = default(T);
			var resourceNames = assembly.GetManifestResourceNames();
			name = name.Replace("/", ".").Replace(@"\", ".").Replace(' ', '_');
			foreach (var resourceName in resourceNames)
			{
				if (resourceName.EndsWith(name))
				{
					results = GetResource<T>(assembly, resourceName);
					return (T)results;
				}
			}
			return default(T);
		}

		public static T GetResource<T>(string name)
		{
			object results;
			System.Reflection.Assembly assembly;
			// Look inside calling assembly.
			assembly = System.Reflection.Assembly.GetCallingAssembly();
			results = GetResource<T>(assembly, name);
			if (results != null) return (T)results;
			// Look inside executing assembly (class library of this method).
			assembly = System.Reflection.Assembly.GetExecutingAssembly();
			results = GetResource<T>(assembly, name);
			return (results == null) ? default(T) : (T)results;
		}

		public static T GetResource<T>(Assembly assembly, string name)
		{
			object results = default(T);
			name = name.Replace("/", ".").Replace(@"\", ".").Replace(' ', '_');
			var assemblyPrefix = assembly.GetName().Name;
			System.IO.Stream stream = assembly.GetManifestResourceStream(name);
			if (stream != null)
			{
				if (typeof(T) == typeof(System.Drawing.Image)
					|| typeof(T) == typeof(System.Drawing.Bitmap)
				)
				{
					return (T)(object)System.Drawing.Image.FromStream(stream);
				}
				else if (typeof(T) == typeof(string))
				{
					System.IO.StreamReader streamReader = new System.IO.StreamReader(stream);
					return (T)(object)streamReader.ReadToEnd();
				}
				else
				{
					byte[] bytes = new byte[stream.Length];
					stream.Read(bytes, 0, (int)stream.Length);
					results = bytes;
				}
			}
			return (T)(object)results;
		}

		#endregion

		#region Disk Activity

		// Sometimes it is good to pause if there is too much disk activity.
		// By letting windows/SQL to commit all changes to disk we can improve speed.

		PerformanceCounter _diskReadCounter = new PerformanceCounter();
		PerformanceCounter _diskWriteCounter = new PerformanceCounter();

		double GetCounterValue(PerformanceCounter pc, string categoryName, string counterName, string instanceName)
		{
			pc.CategoryName = categoryName;
			pc.CounterName = counterName;
			pc.InstanceName = instanceName;
			return pc.NextValue();
		}

		public enum DiskData { ReadAndWrite, Read, Write };

		public double GetDiskData(DiskData dd)
		{
			return dd == DiskData.Read ?
						GetCounterValue(_diskReadCounter, "PhysicalDisk", "Disk Read Bytes/sec", "_Total") :
						dd == DiskData.Write ?
						GetCounterValue(_diskWriteCounter, "PhysicalDisk", "Disk Write Bytes/sec", "_Total") :
						dd == DiskData.ReadAndWrite ?
						GetCounterValue(_diskReadCounter, "PhysicalDisk", "Disk Read Bytes/sec", "_Total") +
						GetCounterValue(_diskWriteCounter, "PhysicalDisk", "Disk Write Bytes/sec", "_Total") :
					0;
		}

		#endregion

		#region Comparisons

		private static Regex _GuidRegex;
		public static Regex GuidRegex
		{
			get
			{
				if (_GuidRegex == null)
				{
					_GuidRegex = new Regex(
				"^[A-Fa-f0-9]{32}$|" +
				"^({|\\()?[A-Fa-f0-9]{8}-([A-Fa-f0-9]{4}-){3}[A-Fa-f0-9]{12}(}|\\))?$|" +
				"^({)?[0xA-Fa-f0-9]{3,10}(, {0,1}[0xA-Fa-f0-9]{3,6}){2}, {0,1}({)([0xA-Fa-f0-9]{3,4}, {0,1}){7}[0xA-Fa-f0-9]{3,4}(}})$");
				}
				return _GuidRegex;
			}

		}

		public static bool IsGuid(string s)
		{
			return string.IsNullOrEmpty(s)
				? false
				: GuidRegex.IsMatch(s);
		}

		public static bool IsEmail(string s)
		{
			if (string.IsNullOrEmpty(s)) return false;
			var match = JocysCom.ClassLibrary.Mail.WebMail.EmailRegex.Match(s);
			return match.Success && match.Value.Length == s.Length;
		}

		#endregion

		#region Exception

		private static string GetClassName(Exception ex)
		{
			var method = typeof(Exception).GetMethod("GetClassName", BindingFlags.NonPublic | BindingFlags.Instance);
			return (string)method.Invoke(ex, null);
		}

		private static string getText(bool isHtml, string text)
		{
			return (isHtml) ? System.Web.HttpUtility.HtmlEncode(text) : text;
		}

		//System.Environment.OSVersion.Version.Major  < 6;
		public static bool UseStableExceptionVersion = true;

		public static string ExceptionToString(Exception ex, bool needFileLineInfo, TraceFormat tf)
		{
			bool isHtml = (tf == TraceFormat.Html);
			string newLine = isHtml ? "<br />" + Environment.NewLine : Environment.NewLine;
			var builder = new StringBuilder(0xff);
			if (isHtml)
			{
				var exText = (ex.GetType().Name + ": " + ex.Message).Replace("\r\n", "<br />");
				builder.Append("<hr/>" + exText + "<hr/>");
			}
			if (isHtml) builder.Append("<span style=\"font-family: Courier New; font-size: 10pt;\">");
			builder.Append(getText(isHtml, GetClassName(ex)));
			string message = ex.Message;
			if (!string.IsNullOrEmpty(message))
			{
				builder.Append(": ");
				builder.Append(getText(isHtml, message));
			}
			if (ex != null)
			{
				builder.Append(newLine);
				StackTrace stackTrace;
				// If use stable version.
				if (UseStableExceptionVersion)
				{
					// Stable version.
					stackTrace = new StackTrace(ex, needFileLineInfo);
					if (stackTrace != null)
					{
						builder.Append(newLine);
						builder.Append(TraceToString(stackTrace, tf));
					}
				}
				else
				{
					// Unstable version
					// Get full stack trace.
					stackTrace = new StackTrace(needFileLineInfo);
					int startFrameIndex = 0;
					if (ex != null)
					{
						var exTrace = new StackTrace(ex, needFileLineInfo);
						if (exTrace.FrameCount > 0)
						{
							var method = exTrace.GetFrame(0).GetMethod();
							if (method != null)
							{
								for (int i = 0; i < stackTrace.FrameCount; i++)
								{
									var m2 = stackTrace.GetFrame(i).GetMethod();
									if (m2 != null && m2.Equals(method))
									{
										startFrameIndex = i;
										break;
									}
								}
							}
						}
					}
					builder.Append(newLine);
					builder.Append(TraceToString(stackTrace, tf, startFrameIndex));
				}
				if (ex.InnerException != null)
				{
					builder.Append(getText(isHtml, " ---> "));
					builder.Append(ExceptionToString(ex.InnerException, needFileLineInfo, tf));
					builder.Append(newLine);
				}
			}
			if (isHtml) builder.Append("</span>");
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
			for (int i = 0; i < trace.FrameCount; i++)
			{
				var frame = trace.GetFrame(i);
				if (IsFormStackFrame(frame)) return frame;
			}
			return null;
		}

		/// <summary>
		/// Return true if method DeclaringType is Form.
		/// </summary>
		public static bool IsFormStackFrame(StackFrame sf)
		{
			var method = sf.GetMethod();
			if (method == null) return false;
			var declaringType = method.DeclaringType;
			if (declaringType == null) return false;
			Type t = declaringType;
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

		public static string TraceToString(StackTrace st, TraceFormat tf, int startFrameIndex = 0)
		{
			bool isHtml = (tf == TraceFormat.Html);
			string newLine = (isHtml) ? "<br />" + Environment.NewLine : Environment.NewLine;
			bool flag = true;
			string format;

			bool flag2 = true;
			var builder = new StringBuilder(0xff);
			if (isHtml) builder.Append("<span style=\"font-family: Courier New; font-size: 10pt;\">");
			for (int i = startFrameIndex; i < st.FrameCount; i++)
			{
				StackFrame frame = st.GetFrame(i);
				MethodBase method = frame.GetMethod();
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
					Type declaringType = method.DeclaringType;
					bool isForm = IsFormStackFrame(frame);
					if (declaringType != null)
					{
						string ns = declaringType.Namespace.Replace('+', '.');
						string name = declaringType.Name.Replace('+', '.');
						if (isHtml)
						{
							if (isForm) builder.Append("<span style=\"font-weight: bold;\">");
							builder.Append(System.Web.HttpUtility.HtmlEncode(ns));
							builder.Append(".");
							builder.AppendFormat("<span style=\"color: #2B91AF; \">{0}</span>", System.Web.HttpUtility.HtmlEncode(name));
							if (isForm) builder.Append("</span>");
						}
						else
						{
							builder.AppendFormat("{0}.{1}", ns, name);
						}
						builder.Append(".");
					}
					if (isHtml && isForm) builder.Append("<span style=\"font-weight: bold; color: #800000;\">");
					builder.Append(method.Name);
					if ((method is MethodInfo) && ((MethodInfo)method).IsGenericMethod)
					{
						Type[] genericArguments = ((MethodInfo)method).GetGenericArguments();
						builder.Append("[");
						int index = 0;
						bool flag3 = true;
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
					ParameterInfo[] parameters = method.GetParameters();
					bool flag4 = true;
					for (int j = 0; j < parameters.Length; j++)
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
						string paramType = "<UnknownType>";
						bool isClass = false;
						if (param.ParameterType != null)
						{
							isClass = param.ParameterType.IsClass;
							paramType = param.ParameterType.Name;
						}
						if (isHtml)
						{
							if (isClass) builder.Append("<span style=\"color: #2B91AF; \">");
							builder.Append(System.Web.HttpUtility.HtmlEncode(paramType));
							if (isClass) builder.Append("</span>");
							builder.Append(" ");
							builder.Append(System.Web.HttpUtility.HtmlEncode(paramType));
						}
						else
						{
							builder.AppendFormat("{0} {1}", paramType, param.Name);
						}
					}
					builder.Append(")");
					if (isHtml && isForm) builder.Append("</span>");
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
							if (isHtml)
							{
								builder.Append("<br />&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
								var l = System.Web.HttpUtility.UrlEncode(fileName.Replace("\\", "/")).Replace("+", "%20");
								var addLink = false;
								if (addLink) builder.AppendFormat("<a href=\"file:///{0}\" style=\"text-decoration: none; color: #000000;\">", l);
								else builder.AppendFormat("<span style=\"color: #000000;\">");
								if (isForm) builder.AppendFormat("<span style=\"color: #800000;\">{0}</span>", System.Web.HttpUtility.HtmlEncode(fileName));
								else builder.Append(fileName);
								if (addLink) builder.Append("</a>");
								else builder.Append("</span>");
								builder.Append("<span style=\"color: #808080;\">,</span> ");
								builder.Append(frame.GetFileLineNumber());
								builder.AppendFormat("<span style=\"color: #808080;\">:{0}</span>", frame.GetFileColumnNumber());
							}
							else
							{
								format = " in {0}, {1}:{2}";
								builder.AppendFormat(CultureInfo.InvariantCulture, format, fileName, frame.GetFileLineNumber(), frame.GetFileColumnNumber());
							}
						}
					}
				}
			}
			if (isHtml || tf == TraceFormat.TrailingNewLine) builder.Append(newLine);
			if (isHtml) builder.Append("</span>");
			return builder.ToString();
		}

		#endregion

		#region Shares

		static object sharedFolderLock = new object();
		static List<SharedFolder> sharedFolderCache;

		public static string ConvertShareToLocalPath(string path, bool cache = true)
		{
			var machine = Environment.MachineName.ToLower();
			// If path is remote.
			if (path.ToLower().StartsWith(@"\\" + machine))
			{
				// This is UNC path which point to local machine.
				List<SharedFolder> list;
				lock (sharedFolderLock)
				{
					if (!cache || sharedFolderCache == null) sharedFolderCache = GetSharedFolders();
					list = sharedFolderCache;
				}
				foreach (var item in list)
				{
					var share = @"\\" + machine + @"\" + item.ShareName;
					// If share was found then...
					if (path.ToLower().StartsWith(share.ToLower()))
					{
						path = item.Path + path.Substring(share.Length);
						path = path.Replace("/", "\\");
						break;
					}
				}
			}
			return path;
		}

		public class SharedFolder
		{
			public string ShareName { get; set; }
			public string Caption { get; set; }
			public string Path { get; set; }
			public string Domain { get; set; }
			public string User { get; set; }
			public uint AccessMask { get; set; }
			public uint AceType { get; set; }
		}

		static List<SharedFolder> GetSharedFolders()
		{
			var list = new List<SharedFolder>();
			var scope = new ManagementScope(@"\\.\root\cimv2");
			scope.Connect();
			var query = new ObjectQuery("SELECT * FROM Win32_LogicalShareSecuritySetting");
			var searcher = new ManagementObjectSearcher(scope, query);
			var results = searcher.Get();
			foreach (ManagementObject securitySetting in results)
			{
				{
					var shareName = (string)securitySetting["Name"];
					var caption = (string)securitySetting["Caption"];
					var localPath = string.Empty;
					var win32Share = new ManagementObjectSearcher("SELECT Path FROM Win32_share WHERE Name = '" + shareName + "'");
					foreach (ManagementObject ShareData in win32Share.Get())
					{
						localPath = (String)ShareData["Path"];
					}
					var method = securitySetting.InvokeMethod("GetSecurityDescriptor", null, new InvokeMethodOptions());
					var descriptor = (ManagementBaseObject)method["Descriptor"];
					var dacl = (ManagementBaseObject[])descriptor["DACL"];
					foreach (ManagementBaseObject ace in dacl)
					{
						var Trustee = (ManagementBaseObject)ace["Trustee"];
						// Full Access = 2032127, Modify = 1245631, Read Write = 118009, Read Only = 1179817
						var item = new SharedFolder();
						item.ShareName = shareName;
						item.Caption = caption;
						item.Path = localPath;
						item.Domain = (string)Trustee["Domain"];
						item.User = (string)Trustee["Name"];
						item.AccessMask = (uint)ace["AccessMask"];
						item.AceType = (uint)ace["AceType"];
						list.Add(item);
					}
				}
			}
			return list;
		}

		#endregion

		#region IDisposable

		// Dispose() calls Dispose(true)
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		// The bulk of the clean-up code is implemented in Dispose(bool)
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				// Free managed resources.
				if (_diskReadCounter != null)
				{
					_diskReadCounter.Dispose();
					_diskReadCounter = null;
				}
				if (_diskWriteCounter != null)
				{
					_diskWriteCounter.Dispose();
					_diskWriteCounter = null;
				}
			}
		}

		#endregion

	}

}
