using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace JocysCom.ClassLibrary.Runtime
{
	/// <summary>
	/// Write logs lines to a single file.
	/// </summary>
	public partial class LogHelper
	{
		private object streamWriterLock = new object();
		public string LogFileName;
		StreamWriter LogStreamWriter;

		public bool LogToFile
		{
			get
			{
				return streamWriterLock != null;
			}
			set
			{
				lock (streamWriterLock)
				{
					// If logging must be enabled but stream writer is not set then...
					if (value && LogStreamWriter == null)
					{
						// We create a new log file every time we run the app.
						LogFileName = GetSaveFileName();
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

		string GetSaveFileName()
		{
			if (!string.IsNullOrEmpty(LogFileName)) return LogFileName;
			var dir = new DirectoryInfo("Logs");
			if (!dir.Exists) dir.Create();
			string assemblyFullName = Assembly.GetExecutingAssembly().FullName;
			Int32 index = assemblyFullName.IndexOf(',');
			string saveFile = assemblyFullName.Substring(0, index);
			// Save directory is created in ConfigFileHandler
			saveFile = string.Format("{0}\\{1}_{2:yyyyMMdd_HHmmss}.txt", dir.FullName, saveFile, DateTime.Now);
			return saveFile;
		}

		public bool LogFileAutoFlush;

		public void WriteToLogFile(string format, params object[] args)
		{
			lock (streamWriterLock)
			{
				if (LogStreamWriter != null)
				{
					var message = args.Length > 0 ? string.Format(format, args) : format;
					if (LogStreamWriter.AutoFlush != LogFileAutoFlush) LogStreamWriter.AutoFlush = LogFileAutoFlush;
					LogStreamWriter.WriteLine(message);
				}
			}
		}

		#region SPAM Prevention

		int? ErrorFileLimitMax;
		TimeSpan? ErrorFileLimitAge;
		Dictionary<Type, List<DateTime>> ErrorFileList = new Dictionary<Type, List<DateTime>>();

		public bool AllowReportExceptionToFile(Exception error)
		{
			// Maximum 10 errors of same type per 5 minutes (2880 per day).
			if (!ErrorFileLimitMax.HasValue)
				ErrorFileLimitMax = ParseInt("ErrorFileLimitMax", 5);
			if (!ErrorFileLimitAge.HasValue)
				ErrorFileLimitAge = ParseSpan("ErrorFileLimitAge", new TimeSpan(0, 5, 0));
			return AllowToReportException(error, ErrorFileList, ErrorFileLimitMax.Value, ErrorFileLimitAge.Value);
		}

		#endregion

	}
}
