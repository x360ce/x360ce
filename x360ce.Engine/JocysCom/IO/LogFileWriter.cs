using System;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace JocysCom.ClassLibrary.IO
{
	
	/// <summary>
	/// Provides buffered file-based logging for tracing program flow.
	/// Supports file naming templates, automatic file rolling, and retention policies via SettingsParser.
	/// Output is written via a StreamWriter buffer; ensure proper disposal (e.g., closing the server console) to flush entries and produce printable logs.
	/// </summary>
	public class LogFileWriter : IDisposable
	{

		/// <summary>
		/// Sets configuration prefix (defaulting to the runtime type name with '_' or '-' suffix if missing) and initializes settings.
		/// </summary>
		public LogFileWriter(string configPrefix = null)
		{
			// This class can be inherited therefore use type to make sure that prefix is different.
			// Get type will return derived class or this class if not derived.
			var prefix = configPrefix ?? GetType().Name;
			// Add separator to the prefix if missing.
			if (!prefix.EndsWith("_", StringComparison.OrdinalIgnoreCase) && !prefix.EndsWith("-", StringComparison.OrdinalIgnoreCase))
				prefix += "_";
			_SP = new Configuration.SettingsParser();
			_SP.ConfigPrefix = prefix;
			_Init();
		}

		/// <summary>
		/// Template for the log file path, using {0} date/time format placeholders for rolling or timestamp-based naming.
		/// </summary>
		public string LogFileName { get; set; }

		/// <summary>
		/// Full path to the currently open log file.
		/// </summary>
		public string CurrentFileFileName { get; private set; }

		public bool LogFileEnabled { get; set; }
		public DateTime LogFileDate { get; set; }

		/// <summary>Time when new log file must be created.</summary>
		[DefaultValue(0)]
		public TimeSpan LogFileTimeout { get; set; }

		/// <summary>
		/// Enables rolling log files based on the date/time pattern in <see cref="LogFileName"/> (e.g., daily rolling).
		/// </summary>
		public bool LogFileRolling { get; set; }

		public StreamWriter BaseStream { get; private set; }

		bool _LogFileAutoFlush;

		object streamWriterLock = new object();

		/// <summary>
		/// Controls automatic flushing of the underlying <see cref="StreamWriter"/> after each write; thread-safe.
		/// </summary>
		public bool LogFileAutoFlush
		{
			get { return _LogFileAutoFlush; }
			set
			{
				lock (streamWriterLock)
				{
					if (BaseStream != null && BaseStream.AutoFlush != LogFileAutoFlush)
						BaseStream.AutoFlush = value;
					_LogFileAutoFlush = value;
				}
			}
		}

		Configuration.SettingsParser _SP;

		/// <summary>Returns the entry assembly's simple name (substring before the first comma).</summary>
		public static string GetAssemblyName()
		{
			string fullName = Assembly.GetEntryAssembly().FullName;
			var index = fullName.IndexOf(',');
			var name = fullName.Substring(0, index);
			return name;
		}

		string company { get { return GetAttribute<AssemblyCompanyAttribute>(a => a.Company); } }
		string product { get { return GetAttribute<AssemblyProductAttribute>(a => a.Product); } }
		string version { get { return GetAttribute<AssemblyFileVersionAttribute>(a => a.Version); } }

		string GetAttribute<T>(Func<T, string> value) where T : Attribute
		{
			var assembly = Assembly.GetEntryAssembly();
			T attribute = (T)Attribute.GetCustomAttribute(assembly, typeof(T));
			return value.Invoke(attribute);
		}

		/// <summary>Gets the path to the log folder in ApplicationData (user-level) or CommonApplicationData (machine-level), based on company and product names.</summary>
		public string GetLogFolder(bool userLevel = false)
		{
			// Get folder.
			var specialFolder = userLevel
				? Environment.SpecialFolder.ApplicationData
				: Environment.SpecialFolder.CommonApplicationData;
			var folder = Environment.GetFolderPath(specialFolder);
			var path = string.Format(CultureInfo.InvariantCulture, "{0}\\{1}\\{2}", folder, company, product);
			return path;
		}

		void _Init()
		{
			LogFileEnabled = _SP.Parse("LogFileEnabled", false);
			if (!LogFileEnabled)
				return;
			_LogFileAutoFlush = _SP.Parse("LogFileAutoFlush", false);
			// File reset options.
			LogFileTimeout = _SP.Parse<TimeSpan>("LogFileTimeout");
			LogFileName = _SP.Parse("LogFileName", "");
			// Enable rolling by default if file name looks like daily.
			var defaultRolling = LogFileName.Contains("MMdd}");
			LogFileRolling = _SP.Parse("LogFileRolling", defaultRolling);
			if (string.IsNullOrEmpty(LogFileName))
			{
				// Suffix pattern.
				var defautlSuffix = LogFileRolling
					// Rolling resets daily by default.
					? "{0:yyyyMMdd}.txt"
					// Use current date when creating new file.
					: "{0:yyyyMMdd_HHmmss}.txt";
				// File prefix to make it unique.
				var defaultPrefix = string.IsNullOrEmpty(_SP.ConfigPrefix)
					? GetAssemblyName() : _SP.ConfigPrefix;
				// Generate unique file name.
				var fileName =
					string.Format(CultureInfo.InvariantCulture, "{0}\\{1}{2}",
						GetLogFolder(), defaultPrefix, defautlSuffix
				);
				LogFileName = fileName;
			}
		}

		/// <summary>Writes a formatted line to the log if enabled and not disposing.</summary>
		/// <param name="format">Composite format string for the log message.</param>
		/// <param name="args">Format arguments.</param>
		public void WriteLine(string format, params object[] args)
		{
			if (!LogFileEnabled || IsDisposing)
				return;
			Write(format + "\r\n", args);
		}

		void Write(string format, params object[] args)
		{
			if (!LogFileEnabled || IsDisposing)
				return;
			var message = args != null && args.Length > 0 ? string.Format(CultureInfo.InvariantCulture, format, args) : format;
			lock (streamWriterLock)
			{
				if (IsDisposing)
					return;
				var n = DateTime.Now;
				if (BaseStream != null)
				{
					var reset =
						// If log file can expire and expired or...
						(LogFileTimeout.Ticks > 0 && n.Subtract(LogFileDate) > LogFileTimeout) ||
						// If file is rolling and name changed then...
						(LogFileRolling && !CurrentFileFileName.Equals(string.Format(CultureInfo.InvariantCulture, LogFileName, n), StringComparison.OrdinalIgnoreCase));
					if (reset)
					{
						// Flush and dispose old file.
						BaseStream.Flush();
						BaseStream.Dispose();
						BaseStream = null;
					}
				}
				if (BaseStream is null)
				{
					// Create new possible file name.
					LogFileDate = n;
					// Expand variables first.
					var expandedPath = Configuration.AssemblyInfo.ExpandPath(LogFileName);
					// Wipe old files.
					WipeOldLogFiles(expandedPath);
					var path = string.Format(CultureInfo.InvariantCulture, expandedPath, n);
					CurrentFileFileName = path;
					// Try to...
					try
					{
						// Create directory for new file.
						var fi = new FileInfo(path);
						if (!fi.Directory.Exists)
							fi.Directory.Create();
					}
					catch (Exception ex)
					{
						ex.Data.Add("LogFileName", CurrentFileFileName);
						ex.Data.Add("LogFullPath", path);
						throw;
					}
					// Create a writer and open the file.
					BaseStream = new StreamWriter(path, true);
					write(string.Format(CultureInfo.InvariantCulture, "#Software: {0} {1} {2}\r\n", company, product, version));
					write(string.Format(CultureInfo.InvariantCulture, "#Date: {0:yyyy-MM-dd HH:mm:ss.fff}\r\n", n));
					write(string.Format(CultureInfo.InvariantCulture, "#Log: Start\r\n"));
				}
				if (BaseStream.AutoFlush != LogFileAutoFlush)
					BaseStream.AutoFlush = LogFileAutoFlush;
				write(message);
			}
		}

		void write(string message, DateTime? date = null)
		{
			var value = string.Format(CultureInfo.InvariantCulture, "{0:yyyy-MM-dd HH:mm:ss} {1}", date ?? DateTime.Now, message);
			BaseStream.Write(value);
		}

		/// <summary>
		/// Flushes any buffered log entries to the file, ensuring they are written to disk.
		/// </summary>
		public void Flush()
		{
			lock (streamWriterLock)
			{
				if (BaseStream != null)
					BaseStream.Flush();
			}
		}

		#region Clean-Up

		/// <summary>
		/// Deletes log files matching the date pattern in the expanded path based on configured maximum file count or total byte size.
		/// </summary>
		/// <param name="expandedPath">Expanded file path template containing date placeholders.</param>
		/// <returns>The number of deleted files.</returns>
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
			var maxLogFiles = _SP.Parse("LogFileMaxFiles", 0);
			var maxLogBytes = _SP.Parse<long>("LogFileMaxBytes", 0);
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

		#region IDisposable

		/// <summary>
		/// Releases resources used by the log writer, writes an end marker if open, and closes the current log file.
		/// </summary>
		// Dispose() calls Dispose(true)
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		bool IsDisposing;

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				lock (streamWriterLock)
				{
					// Don't dispose twice.
					if (IsDisposing)
						return;
					IsDisposing = true;
					if (BaseStream != null)
					{
						write(string.Format(CultureInfo.InvariantCulture, "#Log: End\r\n"));
						BaseStream.Close();
						BaseStream.Dispose();
						BaseStream = null;
					}
				}
			}
		}

		#endregion

	}
}