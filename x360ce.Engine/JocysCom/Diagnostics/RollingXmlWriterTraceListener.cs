using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text.RegularExpressions;

namespace JocysCom.ClassLibrary.Diagnostics
{

	/// <summary>
	/// An extended XmlWriterTraceListener that starts a new file after a configured trace file size. Trace files will be numbered with a four character number suffix.
	/// <example>
	///     <code>
	///         <sharedListeners>
	///             <add
	///					type="JocysCom.ClassLibrary.Diagnostics.RollingXmlWriterTraceListener, JocysCom.ClassLibrary"
	///					name="System.ServiceModel.XmlTrace.Listener"
	///					traceOutputOptions="None"
	///					initializeData="C:\Logs\MyTraceFileName.svclog"
	///					SuffixPattern="_{0:yyyyMMdd}" />
	///         </sharedListeners>
	///     </code>
	/// </example>
	/// </summary>
	[HostProtection(Synchronization = true)]
	public class RollingXmlWriterTraceListener : XmlWriterTraceListener
	{

		/// <summary>
		/// Initializes a new instance of the <see cref="RollingXmlWriterTraceListener"/> class by specifying the trace file
		/// name.
		/// </summary>
		/// <param name="filename">The trace file name.</param>
		public RollingXmlWriterTraceListener(string filename)
			: base(filename)
		{
			_FileNameBasic = filename;
			writerField = GetType().GetField("writer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RollingXmlWriterTraceListener"/> class by specifying the trace file
		/// name and the name of the new instance.
		/// </summary>
		/// <param name="filename">The trace file name.</param>
		/// <param name="name">The name of the new instance.</param>
		public RollingXmlWriterTraceListener(string filename, string name)
			: base(filename, name)
		{
			_FileNameBasic = filename;
			writerField = GetType().GetField("writer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		}

		/// <summary>
		/// Gets the custom attributes supported by the trace listener.
		/// </summary>
		/// <returns>
		/// A string array naming the custom attributes supported by the trace listener, or null if there are no custom attributes.
		/// </returns>
		protected override string[] GetSupportedAttributes()
		{
			return new string[1] { "SuffixPattern" };
		}

		// The basic trace file name configured in system.diagnostics section.
		string _FileNameBasic;

		System.Reflection.FieldInfo writerField;

		object checkLock = new object();

		void CheckFile()
		{
			lock (checkLock)
			{
				if (SuffixPattern == null)
					SuffixPattern = ParseString("SuffixPattern", "_{0:yyyyMMdd}");
				var dir = Path.GetDirectoryName(_FileNameBasic);
				var nam = Path.GetFileNameWithoutExtension(_FileNameBasic);
				var ext = Path.GetExtension(_FileNameBasic);
				var pathPattern = Path.Combine(dir, nam + SuffixPattern + ext);
				var expandedPath = Environment.ExpandEnvironmentVariables(pathPattern);
				var path = string.Format(expandedPath, DateTime.Now);
				var writer = (StreamWriter)writerField.GetValue(this);
				// If file is missing or name changed then...
				if (writer == null || !path.Equals(((FileStream)writer.BaseStream).Name, StringComparison.InvariantCultureIgnoreCase))
				{
					if (writer != null)
						writer.Close();
					// Cleanup old files.
					WipeOldLogFiles(expandedPath);
					var fi = new System.IO.FileInfo(path);
					if (!fi.Directory.Exists)
						fi.Directory.Create();
					// create a new file stream and a new stream writer and pass it to the listener
					var stream = new FileStream(path, FileMode.OpenOrCreate);
					writer = new StreamWriter(stream);
					stream.Seek(0, SeekOrigin.End);
					writerField.SetValue(this, writer);
				}
			}
		}

		string SuffixPattern;

		string ParseString(string name, string defaultValue)
		{
			// Note: Attributes will available only after class in constructed.
			if (!Attributes.ContainsKey(name))
				return defaultValue;
			return Attributes[name] ?? "";
		}

		long ParseLong(string name, long defaultValue)
		{
			// Note: Attributes will available only after class in constructed.
			if (!Attributes.ContainsKey(name))
				return defaultValue;
			var s = Attributes[name];
			long value;
			return long.TryParse(s, out value)
				? value
				: defaultValue;
		}

		#region Clean-Up

		/// <summary>
		/// Wipe old files.
		/// </summary>
		/// <param name="expandedPath">File which contains date pattern.</param>
		public int WipeOldLogFiles(string expandedPath)
		{
			// If file is not specified then return.
			if (string.IsNullOrEmpty(expandedPath))
				return 0;
			var rx = new Regex("[{].*[}]");
			// If file don't have pattern then return.
			if (!rx.IsMatch(expandedPath))
				return 0;
			// Get wipe conditions.
			var maxLogFiles = ParseLong("LogFileMaxFiles", 0);
			var maxLogBytes = ParseLong("LogFileMaxBytes", 0);
			// If keep all then return.
			if (maxLogFiles == 0 && maxLogBytes == 0)
				return 0;
			// Remove pattern to make a valid file name.
			var path = rx.Replace(expandedPath, "");
			var directory = Path.GetDirectoryName(path);
			var di = new DirectoryInfo(directory);
			if (!di.Exists)
				return 0;
			// Get file prefix.
			var fileName = expandedPath.Split('\\').Last();
			var pattern = rx.Replace(fileName, "*");
			// Get file list ordered by newest on the top.
			var files = di.GetFiles(pattern).OrderByDescending(x => x.CreationTime).ToArray();
			var deleted = 0;
			long totalSize = 0;
			for (int i = 0; i < files.Length; i++)
			{
				totalSize += files[i].Length;
				// Delete file if...
				var deleteFile =
					// maximum number of files specified or...
					(maxLogFiles > 0 && i + 1 >= maxLogFiles) ||
					// maximum number of bytes specified.
					(maxLogBytes > 0 && totalSize >= maxLogBytes);
				// If must delete then...
				if (deleteFile)
				{
					files[i].Delete();
					deleted++;
				}
			}
			return deleted;
		}

		#endregion

		#region Public override methods

		public override void Fail(string message)
		{
			CheckFile();
			base.Fail(message);
		}

		public override void Fail(string message, string detailMessage)
		{
			CheckFile();
			base.Fail(message, detailMessage);
		}

		public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
		{
			CheckFile();
			base.TraceData(eventCache, source, eventType, id, data);
		}

		public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data)
		{
			CheckFile();
			base.TraceData(eventCache, source, eventType, id, data);
		}

		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
		{
			CheckFile();
			base.TraceEvent(eventCache, source, eventType, id);
		}

		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
		{
			CheckFile();
			base.TraceEvent(eventCache, source, eventType, id, message);
		}

		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
		{
			CheckFile();
			base.TraceEvent(eventCache, source, eventType, id, format, args);
		}

		public override void TraceTransfer(TraceEventCache eventCache, string source, int id, string message, Guid relatedActivityId)
		{
			CheckFile();
			base.TraceTransfer(eventCache, source, id, message, relatedActivityId);
		}

		public override void Write(object o)
		{
			CheckFile();
			base.Write(o);
		}

		public override void Write(object o, string category)
		{
			CheckFile();
			base.Write(o, category);
		}

		public override void Write(string message)
		{
			CheckFile();
			base.Write(message);
		}

		public override void Write(string message, string category)
		{
			CheckFile();
			base.Write(message, category);
		}

		public override void WriteLine(object o)
		{
			CheckFile();
			base.WriteLine(o);
		}

		public override void WriteLine(object o, string category)
		{
			CheckFile();
			base.WriteLine(o, category);
		}

		public override void WriteLine(string message)
		{
			CheckFile();
			base.WriteLine(message);
		}

		public override void WriteLine(string message, string category)
		{
			CheckFile();
			base.WriteLine(message, category);
		}

		#endregion

	}
}
