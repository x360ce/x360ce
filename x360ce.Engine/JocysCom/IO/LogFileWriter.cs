using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace JocysCom.ClassLibrary.IO
{

	// Create a log file writer, so you can see the flow easily.
	// It can be printed. Makes it easier to figure out complex program flow.
	// The log StreamWriter uses a buffer. So it will only work right if you close
	// the server console properly at the end of the test.
	public class LogFileWriter : IDisposable
	{

		object streamWriterLock = new object();

		public string LogFilePrefix { get; set; }
		public string LogFilePattern { get; set; }
		public string LogFileName { get; set; }
		public DateTime LogFileDate { get; set; }

		/// <summary>Time when new log file must be created.</summary>
		[DefaultValue(0)]
		public TimeSpan LogFileTimeout { get; set; }

		StreamWriter tw;
		bool _LogFileAutoFlush;

		public bool LogFileAutoFlush
		{
			get { return _LogFileAutoFlush; }
			set
			{
				lock (streamWriterLock)
				{
					if (tw != null && tw.AutoFlush != LogFileAutoFlush)
						tw.AutoFlush = value;
					_LogFileAutoFlush = value;
				}
			}
		}

		public LogFileWriter(string configPrefix = "")
		{
			_configPrefix = configPrefix;
			_LogFileAutoFlush = ParseBool("LogFileAutoFlush", false);
			LogFileTimeout = ParseSpan("LogFileTimeout", new TimeSpan());
			// Create default prefix.
			string assemblyFullName = Assembly.GetEntryAssembly().FullName;
			var index = assemblyFullName.IndexOf(',');
			var asmName = assemblyFullName.Substring(0, index);
			// Initialize prefix.
			LogFilePrefix = ParseString("LogFilePrefix", "Logs\\" + asmName + "_");
			// Initialize pattern
			LogFilePattern = ParseString("LogFilePattern", "{0:yyyyMMdd_HHmmss}.txt");
		}

		#region Parse Configuration Values

		string _configPrefix;
		public string ConfigPrefix { get { return _configPrefix; } }

		bool ParseBool(string name, bool defaultValue)
		{
			var v = ConfigurationManager.AppSettings[_configPrefix + "_" + name];
			return (v == null) ? defaultValue : bool.Parse(v);
		}

		string ParseString(string name, string defaultValue)
		{
			var v = ConfigurationManager.AppSettings[_configPrefix + "_" + name];
			return (v == null) ? defaultValue : v;
		}

		TimeSpan ParseSpan(string name, TimeSpan defaultValue)
		{
			var v = ConfigurationManager.AppSettings[_configPrefix + "_" + name];
			return (v == null) ? defaultValue : TimeSpan.Parse(v);
		}


		int ParseInt(string name, int defaultValue)
		{
			var v = ConfigurationManager.AppSettings[_configPrefix + "_" + name];
			return (v == null) ? defaultValue : int.Parse(v);
		}

		long ParseLong(string name, long defaultValue)
		{
			var v = ConfigurationManager.AppSettings[_configPrefix + "_" + name];
			return (v == null) ? defaultValue : long.Parse(v);
		}

		#endregion

		public void WriteLine(string format, params object[] args)
		{
			Write(format + "\r\n", args);
		}

		void Write(string format, params object[] args)
		{
			var message = args.Length > 0 ? string.Format(format, args) : format;
			lock (streamWriterLock)
			{
				if (IsDisposing)
					return;
				// If log file exists and can expire then...
				if (tw != null && LogFileTimeout.Ticks > 0)
				{
					// If log file expired then...
					if (DateTime.Now.Subtract(LogFileDate) > LogFileTimeout)
					{
						// Flush and dispose old file.
						tw.Flush();
						tw.Dispose();
						tw = null;
					}
				}
				if (tw == null)
				{
					var watch = new Stopwatch();
					watch.Start();
					// Loop till file not existing on the disk is found.
					while (true)
					{
						// Wipe old logs.
						var maxLogFiles = ParseInt("LogFileMaxFiles", 0);
						var maxLogBytes = ParseLong("LogFileMaxBytes", 0);
						WipeOldLogFiles(LogFilePrefix, maxLogFiles, maxLogBytes);
						// Create new possible file name.
						LogFileDate = DateTime.Now;
						LogFileName = LogFilePrefix + string.Format(LogFilePattern, LogFileDate);
						// Try to...
						try
						{
							// Create directory for new file.
							var fi = new FileInfo(LogFileName);
							if (!fi.Directory.Exists)
								fi.Directory.Create();
						}
						catch (Exception ex)
						{
							ex.Data.Add("FullPath", LogFileName);
							throw;
						}
						// If file don't exists then break.
						if (!File.Exists(LogFileName))
							break;
						// If took more than one second already then break.
						if (watch.ElapsedMilliseconds > 1200)
						{
							var ex = new Exception("Timeout when creating Log file!");
							ex.Data.Add("FullPath", LogFileName);
							throw ex;
						}
						// Wait a little bit before trying again.
						new ManualResetEvent(false).WaitOne(50);
					}
					// create a writer and open the file
					tw = new StreamWriter(LogFileName);
				}
				if (tw.AutoFlush != LogFileAutoFlush)
					tw.AutoFlush = LogFileAutoFlush;
				var datePrefix = string.Format("{0:yyyy-MM-dd HH:mm:ss} ", DateTime.Now);
				message = datePrefix + message;
				tw.Write(message);
			}
		}


		public void Flush()
		{
			lock (streamWriterLock)
			{
				if (tw != null)
					tw.Flush();
			}
		}

		#region Clean-Up


		/// <summary>
		/// Wipe old files.
		/// </summary>
		/// <param name="maxLogFiles">Number of files to keep.</param>
		/// <param name="maxLogBytes">Number of bytes to keep.</param>
		public static int WipeOldLogFiles(string logFilePrefix, int maxLogFiles, long maxLogBytes)
		{
			// If keep all then return.
			if (maxLogFiles == 0 && maxLogBytes == 0)
				return 0;
			var fi = new FileInfo(logFilePrefix);
			var di = fi.Directory;
			if (!di.Exists)
				return 0;
			var prefix = logFilePrefix.Split('\\').Last();
			// Get file list ordered by newest on the top.
			var files = di.GetFiles(prefix + "*" + fi.Extension).OrderByDescending(x => x.CreationTime).ToArray();
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

		#region IDisposable

		// Dispose() calls Dispose(true)
		public virtual void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		bool IsDisposing;

		void Dispose(bool disposing)
		{
			if (disposing)
			{
				lock (streamWriterLock)
				{
					// Don't dispose twice.
					if (IsDisposing)
						return;
					IsDisposing = true;
					if (tw != null)
					{
						tw.Close();
						tw.Dispose();
						tw = null;
					}
				}
			}
		}

		#endregion

	}
}


