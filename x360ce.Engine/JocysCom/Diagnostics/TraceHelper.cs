#if NETCOREAPP
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
#endif
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Reflection;

namespace JocysCom.ClassLibrary.Diagnostics
{
	public class TraceHelper
	{

		public static string GetAsString(NameValueCollection collection)
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
			writer.Close();
			return sb.ToString();
		}

		public static void AddLog(string sourceName, TraceEventType eventType, NameValueCollection collection)
		{
			var xml = GetAsString(collection);
			AddXml(sourceName, eventType, xml);
		}

		static void AddXml(string sourceName, TraceEventType eventType, string xml)
		{
			using (var sr = new StringReader(xml))
			using (var tr = new XmlTextReader(sr))
			{
				// Settings used to protect from
				// SUPPRESS: CWE-611: Improper Restriction of XML External Entity Reference('XXE')
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

		public static void AddLog(string sourceName, TraceEventType eventType, params object[] data)
		{

			var source = new TraceSource(sourceName);
#if NETCOREAPP
			// Web.config is not available in .NET Core, therefore must manually config.
			Configure(source);
#endif
			source.TraceData(eventType, 0, data);
			source.Flush();
			source.Listeners.Clear();
			source.Close();
		}

#if NETCOREAPP

		#region TraceOptions

		private static IConfiguration _Configuration { get; set; }

		public static void Configure(IConfiguration configuration, ILogger logger = null)
		{
			_Configuration = configuration;
			Configure();
		}

		private static IConfigurationSection GetSection<T>()
			=> _Configuration?.GetSection(typeof(T).FullName.Replace('.', ':'));


		public static List<TraceListener> AllListeners = new List<TraceListener>();

		/// <summary>
		/// Configure specified or default Trace source.
		/// </summary>
		/// <param name="source">Trace source to configure. Configure default if null.</param>
		public static void Configure(TraceSource source = null)
		{
			var section = GetSection<TraceSource>();
			var isDefault = source is null;
			// If trace configuration do not exists then...
			if (section == null)
			{
				// Just use existing listeners.
				source.Listeners.Clear();
				source.Switch = new SourceSwitch("sourceSwitch", "All");
				foreach (TraceListener item in Trace.Listeners)
					source.Listeners.Add(item);
				return;
			}
			var sourceName = isDefault ? "Default" : source.Name;
			var sourceSection = section.GetSection(sourceName);
			// If source section configuration do not exists then return.
			if (!sourceSection.Exists())
				return;
			// If default / global source then...
			if (isDefault)
			{
				// Update default/global TraceSource.
				Trace.AutoFlush = sourceSection.GetValue(nameof(Trace.AutoFlush), false);
			}
			else
			{
				// Update specified source.
				sourceSection.Bind(nameof(TraceSource.Switch), source.Switch);
			}
			var listenersSection = GetSection<TraceListener>();
			// If listener section configuration do not exists then return.
			if (!listenersSection.Exists())
				return;
			// Get specified source or default source listeners.
			var listeners = isDefault ? Trace.Listeners : source.Listeners;
			listeners.Clear();
			// Get listener names as string array.
			var listenerNames = sourceSection.GetSection(nameof(TraceSource.Listeners)).Get<string[]>();
			if (listenerNames is null)
				return;
			foreach (var listenerName in listenerNames)
			{
				var listener = AllListeners.FirstOrDefault(x => x.Name == listenerName);
				// If listener is was not created then...
				if (listener is null)
				{
					// Create new listener from configuration.
					var lSection = listenersSection.GetSection(listenerName);
					var typeName = lSection.GetValue<string>(nameof(System.Type));
					var t = System.Type.GetType(typeName, true);
					object[] args = null;

					var initializeData = lSection.GetValue<string>("InitializeData");
					if (initializeData != null)
						args = new object[] { initializeData };
					listener = (TraceListener)System.Activator.CreateInstance(t, args);
					var attributes = lSection.GetSection(nameof(TraceListener.Attributes)).GetChildren();
					listener.Attributes.Clear();
					foreach (var a in attributes)
						listener.Attributes.Add(a.Key, a.Value);
				}
				listeners.Add(listener);
			}
		}

		#endregion

#endif



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
			if (listener is null)
				throw new ArgumentNullException(nameof(listener));
			if (actionToExecute is null)
				throw new ArgumentNullException(nameof(actionToExecute));
			var logging = typeof(System.Net.WebRequest).Assembly.GetType("System.Net.Logging");
			var flags = BindingFlags.NonPublic | BindingFlags.Static;
			var isInitializedField = logging.GetField("s_LoggingInitialized", flags);
			if (!(bool)isInitializedField.GetValue(null))
			{
				// Force initialization.
				var waitForInitializationThread = new System.Threading.Thread(() =>
				{
					while (!(bool)isInitializedField.GetValue(null))
						System.Threading.Thread.Sleep(100);
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
			var originalTraceSourceFilters = new System.Collections.Generic.Dictionary<TraceListener, TraceFilter>();
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
					return _filter is null
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
