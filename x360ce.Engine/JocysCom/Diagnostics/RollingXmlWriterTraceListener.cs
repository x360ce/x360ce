using System;
using System.Diagnostics;
using System.IO;
using System.Security.Permissions;

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
				var path = Path.Combine(dir, nam + string.Format(SuffixPattern, DateTime.Now) + ext);
				var writer = (StreamWriter)writerField.GetValue(this);
				// If file is missing or name changed then...
				if (writer == null || !path.Equals(((FileStream)writer.BaseStream).Name, StringComparison.InvariantCultureIgnoreCase))
				{
					if (writer != null)
						writer.Close();
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
