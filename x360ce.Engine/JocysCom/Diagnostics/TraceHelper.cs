using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.XPath;

namespace JocysCom.ClassLibrary.Diagnostics
{
	public class TraceHelper
	{

		public static void AddLog(string sourceName, TraceEventType eventType, params object[] data)
		{
			// Add source
			var source = new TraceSource(sourceName);
			//source.Listeners.Add(_Listener);
			//source.Switch.Level = SourceLevels.All;
			source.TraceData(eventType, 0, data);
			source.Flush();
			source.Close();
		}

		public static void AddLog(string sourceName, TraceEventType eventType, NameValueCollection collection)
		{
			// Write Data.
			var settings = new XmlWriterSettings();
			settings.Indent = true;
			settings.IndentChars = ("\t");
			settings.OmitXmlDeclaration = true;
			var sb = new StringBuilder();
			// Create the XmlWriter object and write some content.
			var writer = XmlWriter.Create(sb, settings);
			writer.WriteStartElement("Data");
			foreach (var key in collection.AllKeys)
			{
				var keyString = string.Format("{0}", key);
				var valString = string.Format("{0}", collection[key]);
				writer.WriteElementString(keyString, valString);
			}
			writer.WriteEndElement();
			writer.Flush();
			AddXml(sourceName, eventType, sb.ToString());
			writer.Close();

		}

		static void AddXml(string sourceName, TraceEventType eventType, string xml)
		{
			using (var sr = new StringReader(xml))
			{
				using (var tr = new XmlTextReader(sr))
				{
					// Settings used to protect from
					// CWE-611: Improper Restriction of XML External Entity Reference('XXE')
					// https://cwe.mitre.org/data/definitions/611.html
					var settings = new XmlReaderSettings();
					settings.DtdProcessing = DtdProcessing.Ignore;
					settings.XmlResolver = null;
					using (var xr = XmlReader.Create(tr, settings))
					{
						var doc = new XPathDocument(tr);
						var nav = doc.CreateNavigator();
						AddLog(sourceName, eventType, nav);
					}
				}
			}
		}

		#region Execute with enabled System.Net.Logging

		// https://stackoverflow.com/questions/1049442/system-net-httpwebrequest-tracing-without-using-files-or-app-config

		/// <summary>
		/// Executes a action with enabled System.Net.Logging with listener(s) at the code-site
		/// 
		/// Message from Microsoft:
		/// To configure you the listeners and level of logging for a listener you need a reference to the listener that is going to be doing the tracing. 
		/// A call to create a new TraceSource object creates a trace source with the same name as the one used by the System.Net.Sockets classes, 
		/// but it's not the same trace source object, so any changes do not have an effect on the actual TraceSource object that System.Net.Sockets is using.
		/// </summary>
		/// <param name="webTraceSourceLevel">The sourceLevel for the System.Net traceSource</param>
		/// <param name="httpListenerTraceSourceLevel">The sourceLevel for the System.Net.HttpListener traceSource</param>
		/// <param name="socketsTraceSourceLevel">The sourceLevel for the System.Net.Sockets traceSource</param>
		/// <param name="cacheTraceSourceLevel">The sourceLevel for the System.Net.Cache traceSource</param>
		/// <param name="actionToExecute">The action to execute</param>
		/// <param name="listener">The listener(s) to use</param>
		public static void ExecuteWithEnabledSystemNetLogging(SourceLevels webTraceSourceLevel, SourceLevels httpListenerTraceSourceLevel, SourceLevels socketsTraceSourceLevel, SourceLevels cacheTraceSourceLevel, Action actionToExecute, params TraceListener[] listener)
		{
			if (listener == null)
				throw new ArgumentNullException(nameof(listener));
			if (actionToExecute == null)
				throw new ArgumentNullException(nameof(actionToExecute));
			var logging = typeof(WebRequest).Assembly.GetType("System.Net.Logging");
			var flags = BindingFlags.NonPublic | BindingFlags.Static;
			var isInitializedField = logging.GetField("s_LoggingInitialized", flags);
			if (!(bool)isInitializedField.GetValue(null))
			{
				// force initialization
				WebRequest.Create("http://localhost");
				var waitForInitializationThread = new Thread(() =>
				{
					while (!(bool)isInitializedField.GetValue(null))
						Thread.Sleep(100);
				});
				waitForInitializationThread.Start();
				waitForInitializationThread.Join();
			}

			var s_WebTraceSource = (TraceSource)logging.GetField("s_WebTraceSource", flags).GetValue(null);
			var s_HttpListenerTraceSource = (TraceSource)logging.GetField("s_HttpListenerTraceSource", flags).GetValue(null);
			var s_SocketsTraceSource = (TraceSource)logging.GetField("s_SocketsTraceSource", flags).GetValue(null);
			var s_CacheTraceSource = (TraceSource)logging.GetField("s_CacheTraceSource", flags).GetValue(null);
			var s_LoggingEnabledField = logging.GetField("s_LoggingEnabled", flags);
			bool wasEnabled = (bool)s_LoggingEnabledField.GetValue(null);
			var originalTraceSourceFilters = new Dictionary<TraceListener, TraceFilter>();
			// Save original Levels
			var originalWebTraceSourceLevel = s_WebTraceSource.Switch.Level;
			var originalHttpListenerTraceSourceLevel = s_HttpListenerTraceSource.Switch.Level;
			var originalSocketsTraceSourceLevel = s_SocketsTraceSource.Switch.Level;
			var originalCacheTraceSourceLevel = s_CacheTraceSource.Switch.Level;

			//System.Net
			s_WebTraceSource.Listeners.AddRange(listener);
			s_WebTraceSource.Switch.Level = SourceLevels.All;
			foreach (TraceListener tl in s_WebTraceSource.Listeners)
			{
				if (!originalTraceSourceFilters.ContainsKey(tl))
				{
					originalTraceSourceFilters.Add(tl, tl.Filter);
					tl.Filter = new ModifiedTraceFilter(tl, originalWebTraceSourceLevel, webTraceSourceLevel, originalHttpListenerTraceSourceLevel, httpListenerTraceSourceLevel, originalSocketsTraceSourceLevel, socketsTraceSourceLevel, originalCacheTraceSourceLevel, cacheTraceSourceLevel, listener.Contains(tl));
				}
			}

			//System.Net.HttpListener
			s_HttpListenerTraceSource.Listeners.AddRange(listener);
			s_HttpListenerTraceSource.Switch.Level = SourceLevels.All;
			foreach (TraceListener tl in s_HttpListenerTraceSource.Listeners)
			{
				if (!originalTraceSourceFilters.ContainsKey(tl))
				{
					originalTraceSourceFilters.Add(tl, tl.Filter);
					tl.Filter = new ModifiedTraceFilter(tl, originalWebTraceSourceLevel, webTraceSourceLevel, originalHttpListenerTraceSourceLevel, httpListenerTraceSourceLevel, originalSocketsTraceSourceLevel, socketsTraceSourceLevel, originalCacheTraceSourceLevel, cacheTraceSourceLevel, listener.Contains(tl));
				}
			}

			//System.Net.Sockets
			s_SocketsTraceSource.Listeners.AddRange(listener);
			s_SocketsTraceSource.Switch.Level = SourceLevels.All;
			foreach (TraceListener tl in s_SocketsTraceSource.Listeners)
			{
				if (!originalTraceSourceFilters.ContainsKey(tl))
				{
					originalTraceSourceFilters.Add(tl, tl.Filter);
					tl.Filter = new ModifiedTraceFilter(tl, originalWebTraceSourceLevel, webTraceSourceLevel, originalHttpListenerTraceSourceLevel, httpListenerTraceSourceLevel, originalSocketsTraceSourceLevel, socketsTraceSourceLevel, originalCacheTraceSourceLevel, cacheTraceSourceLevel, listener.Contains(tl));
				}
			}

			//System.Net.Cache
			s_CacheTraceSource.Listeners.AddRange(listener);
			s_CacheTraceSource.Switch.Level = SourceLevels.All;
			foreach (TraceListener tl in s_CacheTraceSource.Listeners)
			{
				if (!originalTraceSourceFilters.ContainsKey(tl))
				{
					originalTraceSourceFilters.Add(tl, tl.Filter);
					tl.Filter = new ModifiedTraceFilter(tl, originalWebTraceSourceLevel, webTraceSourceLevel, originalHttpListenerTraceSourceLevel, httpListenerTraceSourceLevel, originalSocketsTraceSourceLevel, socketsTraceSourceLevel, originalCacheTraceSourceLevel, cacheTraceSourceLevel, listener.Contains(tl));
				}
			}

			s_LoggingEnabledField.SetValue(null, true);

			try
			{
				actionToExecute();
			}
			finally
			{
				//// restore Settings
				s_WebTraceSource.Switch.Level = originalWebTraceSourceLevel;
				s_HttpListenerTraceSource.Switch.Level = originalHttpListenerTraceSourceLevel;
				s_SocketsTraceSource.Switch.Level = originalSocketsTraceSourceLevel;
				s_CacheTraceSource.Switch.Level = originalCacheTraceSourceLevel;
				foreach (var li in listener)
				{
					s_WebTraceSource.Listeners.Remove(li);
					s_HttpListenerTraceSource.Listeners.Remove(li);
					s_SocketsTraceSource.Listeners.Remove(li);
					s_CacheTraceSource.Listeners.Remove(li);
				}
				//// restore filters
				foreach (var kvP in originalTraceSourceFilters)
					kvP.Key.Filter = kvP.Value;

				s_LoggingEnabledField.SetValue(null, wasEnabled);
			}
		}

		public class ModifiedTraceFilter : TraceFilter
		{
			private readonly TraceListener _traceListener;
			private readonly SourceLevels _originalWebTraceSourceLevel;
			private readonly SourceLevels _originalHttpListenerTraceSourceLevel;
			private readonly SourceLevels _originalSocketsTraceSourceLevel;
			private readonly SourceLevels _originalCacheTraceSourceLevel;
			private readonly SourceLevels _modifiedWebTraceTraceSourceLevel;
			private readonly SourceLevels _modifiedHttpListenerTraceSourceLevel;
			private readonly SourceLevels _modifiedSocketsTraceSourceLevel;
			private readonly SourceLevels _modifiedCacheTraceSourceLevel;
			private readonly bool _ignoreOriginalSourceLevel;
			private readonly TraceFilter _filter = null;
			public ModifiedTraceFilter(TraceListener traceListener, SourceLevels originalWebTraceSourceLevel, SourceLevels modifiedWebTraceSourceLevel, SourceLevels originalHttpListenerTraceSourceLevel, SourceLevels modifiedHttpListenerTraceSourceLevel, SourceLevels originalSocketsTraceSourceLevel, SourceLevels modifiedSocketsTraceSourceLevel, SourceLevels originalCacheTraceSourceLevel, SourceLevels modifiedCacheTraceSourceLevel, bool ignoreOriginalSourceLevel)
			{
				_traceListener = traceListener;
				_filter = traceListener.Filter;
				_originalWebTraceSourceLevel = originalWebTraceSourceLevel;
				_modifiedWebTraceTraceSourceLevel = modifiedWebTraceSourceLevel;
				_originalHttpListenerTraceSourceLevel = originalHttpListenerTraceSourceLevel;
				_modifiedHttpListenerTraceSourceLevel = modifiedHttpListenerTraceSourceLevel;
				_originalSocketsTraceSourceLevel = originalSocketsTraceSourceLevel;
				_modifiedSocketsTraceSourceLevel = modifiedSocketsTraceSourceLevel;
				_originalCacheTraceSourceLevel = originalCacheTraceSourceLevel;
				_modifiedCacheTraceSourceLevel = modifiedCacheTraceSourceLevel;
				_ignoreOriginalSourceLevel = ignoreOriginalSourceLevel;
			}
			public override bool ShouldTrace(TraceEventCache cache, string source, TraceEventType eventType, int id, string formatOrMessage, object[] args, object data1, object[] data)
			{
				var originalTraceSourceLevel = SourceLevels.Off;
				var modifiedTraceSourceLevel = SourceLevels.Off;
				if (source == "System.Net")
				{
					originalTraceSourceLevel = _originalWebTraceSourceLevel;
					modifiedTraceSourceLevel = _modifiedWebTraceTraceSourceLevel;
				}
				else if (source == "System.Net.HttpListener")
				{
					originalTraceSourceLevel = _originalHttpListenerTraceSourceLevel;
					modifiedTraceSourceLevel = _modifiedHttpListenerTraceSourceLevel;
				}
				else if (source == "System.Net.Sockets")
				{
					originalTraceSourceLevel = _originalSocketsTraceSourceLevel;
					modifiedTraceSourceLevel = _modifiedSocketsTraceSourceLevel;
				}
				else if (source == "System.Net.Cache")
				{
					originalTraceSourceLevel = _originalCacheTraceSourceLevel;
					modifiedTraceSourceLevel = _modifiedCacheTraceSourceLevel;
				}
				var level = ConvertToSourceLevel(eventType);
				var should = (!_ignoreOriginalSourceLevel && (originalTraceSourceLevel & level) == level) ||
					(_ignoreOriginalSourceLevel && (modifiedTraceSourceLevel & level) == level);
				if (should)
				{
					return _filter == null
						? true
						: _filter.ShouldTrace(cache, source, eventType, id, formatOrMessage, args, data1, data);
				}
				return false;
			}

			static SourceLevels ConvertToSourceLevel(TraceEventType eventType)
			{
				switch (eventType)
				{
					case TraceEventType.Critical:
						return SourceLevels.Critical;
					case TraceEventType.Error:
						return SourceLevels.Error;
					case TraceEventType.Information:
						return SourceLevels.Information;
					case TraceEventType.Verbose:
						return SourceLevels.Verbose;
					case TraceEventType.Warning:
						return SourceLevels.Warning;
					default:
						return SourceLevels.ActivityTracing;
				}
			}
		}

		#endregion


	}
}
