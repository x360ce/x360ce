#if NETSTANDARD // .NET Standard
#else

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
	//[HostProtection(Synchronization = true)]
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
			Options = new RollingWriterOptions(filename);
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
			Options = new RollingWriterOptions(filename);
		}

		/// <summary>
		/// Gets the custom attributes supported by the trace listener.
		/// </summary>
		/// <returns>
		/// A string array naming the custom attributes supported by the trace listener, or null if there are no custom attributes.
		/// </returns>
		protected override string[] GetSupportedAttributes()
			=> RollingWriterOptions.GetSupportedAttributes();

		public RollingWriterOptions Options;

		#region Public override methods

		public override void Fail(string message)
		{
			RollingWriterOptions.CheckFile(this, Options);
			base.Fail(message);
		}

		public override void Fail(string message, string detailMessage)
		{
			RollingWriterOptions.CheckFile(this, Options);
			base.Fail(message, detailMessage);
		}

		public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
		{
			RollingWriterOptions.CheckFile(this, Options);
			base.TraceData(eventCache, source, eventType, id, data);
		}

		public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data)
		{
			RollingWriterOptions.CheckFile(this, Options);
			base.TraceData(eventCache, source, eventType, id, data);
		}

		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
		{
			RollingWriterOptions.CheckFile(this, Options);
			base.TraceEvent(eventCache, source, eventType, id);
		}

		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
		{
			RollingWriterOptions.CheckFile(this, Options);
			base.TraceEvent(eventCache, source, eventType, id, message);
		}

		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
		{
			RollingWriterOptions.CheckFile(this, Options);
			base.TraceEvent(eventCache, source, eventType, id, format, args);
		}

		public override void TraceTransfer(TraceEventCache eventCache, string source, int id, string message, Guid relatedActivityId)
		{
			RollingWriterOptions.CheckFile(this, Options);
			base.TraceTransfer(eventCache, source, id, message, relatedActivityId);
		}

		// Write[Line](object o...) calls Write[Line](string message...)
		// Write[Line](string message...) calls TraceEvent with TraceEventType.Information.

		public override void Write(object o)
			=> base.Write(o);

		public override void Write(object o, string category)
			=> base.Write(o, category);

		public override void Write(string message)
			=> base.Write(message);

		public override void Write(string message, string category)
			=> base.Write(message, category);

		public override void WriteLine(object o)
			=> base.WriteLine(o);

		public override void WriteLine(object o, string category)
			=> base.WriteLine(o, category);

		public override void WriteLine(string message)
			=> base.WriteLine(message);

		public override void WriteLine(string message, string category)
			=> base.WriteLine(message, category);

		#endregion

	}
}
#endif
