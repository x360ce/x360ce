using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace JocysCom.ClassLibrary.Diagnostics
{
	public partial class RollingWriterOptions

	{

		public RollingWriterOptions(string fileName)
		{
			_FileNameBasic = fileName;
		}

		// The basic trace file name configured in system.diagnostics section.
		protected string _FileNameBasic;
		protected System.Reflection.FieldInfo _WriterField;

		public string SuffixPattern { get; set; }

		//initializeData
		public string FolderPath { get; set; }

		public long? LogFileMaxFiles { get; set; }

		public long? LogFileMaxBytes { get; set; }

		/// <summary>
		/// Settings must be loaded after app initialization in order for settings to be available.
		/// </summary>
		/// <param name="attributes"></param>
		public void Load(StringDictionary attributes)
		{
			// File will roll over every day.
			SuffixPattern = ParseString(attributes, nameof(SuffixPattern), "_{0:yyyyMMdd}");
			LogFileMaxFiles = ParseLong(attributes, nameof(LogFileMaxFiles), null);
			LogFileMaxBytes = ParseLong(attributes, nameof(LogFileMaxBytes), null);
		}

		private static string ParseString(StringDictionary attributes, string name, string defaultValue)
		{
			// Note: Attributes will available only after class in constructed.
			if (!attributes.ContainsKey(name))
				return defaultValue;
			return attributes[name] ?? "";
		}

		private static long? ParseLong(StringDictionary attributes, string name, long? defaultValue)
		{
			// Note: Attributes will available only after class in constructed.
			if (!attributes.ContainsKey(name))
				return defaultValue;
			var s = attributes[name];
			long value;
			return long.TryParse(s, out value)
				? value
				: defaultValue;
		}

		public static string[] GetSupportedAttributes()
		{
			return new string[] {
				nameof(SuffixPattern),
				nameof(LogFileMaxFiles),
				nameof(LogFileMaxBytes),
			};
		}

		protected bool SettingsLoaded = false;

		public static void CheckFile(TraceListener listener, RollingWriterOptions options)
		{
			lock (listener)
			{
				// If first time then...
				if (!options.SettingsLoaded)
				{
					options.Load(listener.Attributes);
					var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
					// .NET Framework
					options._WriterField = listener.GetType().GetField("writer", flags);
						// .NET Core.
					if (options._WriterField is null)
						options._WriterField = listener.GetType().GetField("_writer", flags);
					options.SettingsLoaded = true;
				}
				var dir = Path.GetDirectoryName(options._FileNameBasic);
				var nam = Path.GetFileNameWithoutExtension(options._FileNameBasic);
				var ext = Path.GetExtension(options._FileNameBasic);
				var pathPattern = Path.Combine(dir, nam + options.SuffixPattern + ext);
				var expandedPath = Configuration.AssemblyInfo.ExpandPath(pathPattern);
				var path = string.Format(expandedPath, DateTime.Now);
				var writer = (StreamWriter)options._WriterField.GetValue(listener);
				// If file is missing or name changed then...
				if (writer is null || !path.Equals(((FileStream)writer.BaseStream).Name, StringComparison.OrdinalIgnoreCase))
				{
					if (writer != null)
						writer.Close();
					// Cleanup old files.
					// Get wipe conditions.
					WipeOldLogFiles(expandedPath, options.LogFileMaxFiles, options.LogFileMaxBytes);
					var fi = new FileInfo(path);
					if (!fi.Directory.Exists)
						fi.Directory.Create();
					// Create a new file stream and a new stream writer and pass it to the listener.
					var stream = new FileStream(path, FileMode.OpenOrCreate);
					writer = new StreamWriter(stream);
					stream.Seek(0, SeekOrigin.End);
					options._WriterField.SetValue(listener, writer);
				}
			}
		}

		/// <summary>
		/// Wipe old files.
		/// </summary>
		/// <param name="expandedPath">File which contains date pattern.</param>
		/// <param name="maxLogFiles">Limit folder content to maximum files.</param>
		/// <param name="maxLogFiles">Limit folder content to maximum bytes.</param>
		public static int WipeOldLogFiles(string expandedPath, long? maxLogFiles = null, long? maxLogBytes = null)
		{
			// If file is not specified then return.
			if (string.IsNullOrEmpty(expandedPath))
				return 0;
			var rx = new Regex("[{].*[}]");
			// If file don't have pattern then return.
			if (!rx.IsMatch(expandedPath))
				return 0;
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


	}
}
