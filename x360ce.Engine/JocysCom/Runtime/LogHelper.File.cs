using System;
using System.IO;
using System.Reflection;
using System.Linq;

namespace JocysCom.ClassLibrary.Runtime
{
	/// <summary>
	/// Write logs lines to a single file.
	/// </summary>
	public partial class LogHelper
	{
		private object streamWriterLock = new object();

		public string LogFilePrefix;
		public string LogFilePattern;
		public string LogFileName;
		public string LogFileMaxLogFiles;
		StreamWriter LogStreamWriter;

		void InitFileName()
		{
			// If log file name is already set then return it.
			if (!string.IsNullOrEmpty(LogFileName))
				return;
			// Create default prefix.
			string assemblyFullName = Assembly.GetExecutingAssembly().FullName;
			var index = assemblyFullName.IndexOf(',');
			var asmName = assemblyFullName.Substring(0, index);
			// Initialize prefix.
			LogFilePrefix = ParseString(_configPrefix + "FilePrefix", "Logs\\" + asmName + "_");
			// Initialize pattern
			LogFilePattern = ParseString(_configPrefix + "FilePattern", "{0:yyyyMMdd_HHmmss}.txt");
			// Set log file name.
			LogFileName = LogFilePrefix + string.Format(LogFilePattern, DateTime.Now);
		}


		/// <summary>
		/// Wipe old files.
		/// </summary>
		/// <param name="maxLogFiles">Number of files to keep.</param>
		int WipeOldLogFiles(int maxLogFiles, long maxLogBytes)
		{

			var fi = new FileInfo(LogFileName);
			var di = fi.Directory;
			var prefix = LogFilePrefix.Split('\\').Last();
			// Get file list ordered by newest on the top.
			var files = di.GetFiles(prefix + "*" + fi.Extension).OrderByDescending(x => x.CreationTime).ToArray();
			var deleted = 0;
			long totalSize = 0;
			for (int i = 0; i < files.Length; i++)
			{
				totalSize += files[i].Length;
				// If maximum reached then...
				if (i + 1 >= maxLogFiles || totalSize >= maxLogBytes)
				{
					files[i].Delete();
					deleted++;
				}
			}
			return deleted;
		}

		public bool LogToFile
		{
			get
			{
				lock (streamWriterLock)
				{
					return LogStreamWriter != null;
				}
			}
			set
			{
				lock (streamWriterLock)
				{
					// If logging must be enabled but stream writer is not set then...
					if (value && LogStreamWriter == null)
					{
						// Wipe old logs.
						var maxLogFiles = ParseInt(_configPrefix + "MaxLogFiles", 1000);
						var maxLogBytes = ParseLong(_configPrefix + "MaxLogBytes", 100L * 1024 * 1024 * 1024); // 100 GB
						WipeOldLogFiles(maxLogFiles, maxLogBytes);
						// We create a new log file every time we run the application.
						InitFileName();
						// create a writer and open the file
						var fi = new FileInfo(LogFileName);
						if (!fi.Directory.Exists)
							fi.Directory.Create();
						LogStreamWriter = File.AppendText(LogFileName); // new StreamWriter(LogFileName)
					}
					else if (!value && LogStreamWriter != null)
					{
						// If logging must be disabled and stream writer is set then
						// Flush all cache into file.
						LogStreamWriter.Close();
						// Dispose stream writer.
						LogStreamWriter.Dispose();
						LogStreamWriter = null;
					}
				}
			}
		}


		public bool LogFileAutoFlush;

		public void WriteToLogFile(string format, params object[] args)
		{
			lock (streamWriterLock)
			{
				if (LogStreamWriter != null)
				{
					var message = args.Length > 0 ? string.Format(format, args) : format;
					if (LogStreamWriter.AutoFlush != LogFileAutoFlush)
						LogStreamWriter.AutoFlush = LogFileAutoFlush;
					LogStreamWriter.WriteLine(message);
				}
			}
		}

	}
}
