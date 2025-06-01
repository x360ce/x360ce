using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace JocysCom.ClassLibrary.Runtime
{
	/// <summary>
	/// Partial LogHelper implementation for file-based exception logging
	/// with grouping by exception type and HResult code, configurable file count and deletion age.
	/// </summary>
	public partial class LogHelper
	{
		/// <summary>
		/// Cache of exception groups for file-based logging, keyed by exception type and HResult.
		/// </summary>
		static List<ExceptionGroup> fileExceptions = new List<ExceptionGroup>();

		/// <summary>
		/// Maximum number of log files to retain per exception type and HResult code; default 10.
		/// </summary>
		public int MaxFiles { get { return _SP.Parse("MaxFiles", 10); } }

		/// <summary>
		/// Time span that determines file deletion eligibility when MaxFiles limit is exceeded; default 00:00:10.
		/// </summary>
		public TimeSpan MaxFilesDeleteAge { get { return _SP.Parse("MaxFilesRemoveAge", new TimeSpan(0, 0, 10) ); } }

		/// <summary>
		/// Thread-safe count of exceptions processed for file logging, excluding those cancelled by WritingException event.
		/// </summary>
		public long ExceptionsCount;

		/// <summary>
		/// Logs an exception to file.
		/// Raises the WritingException event allowing cancellation, increments the thread-safe ExceptionsCount,
		/// and groups duplicates by exception type and HResult.
		/// </summary>
		/// <param name="ex">Exception to write.</param>
		/// <param name="subject">Custom subject instead of the default generated from the exception.</param>
		/// <param name="body">Optional additional information to include in the log entry.</param>
		public void WriteException(Exception ex, string subject = null, string body = null)
		{
			// Check if exception can be ignored.
			var le = new LogHelperEventArgs() { Exception = ex };
			WritingException?.Invoke(this, le);
			// If Exception reporting was cancelled then return.
			if (le.Cancel)
				return;
			Interlocked.Increment(ref ExceptionsCount);
			if (!LogToFile)
				return;
			_GroupException(fileExceptions, ex, subject, body, _WriteFile);
		}

		/// <summary>
		/// Search pattern for exception log files matching the current extension (.htm or .txt).
		/// </summary>
		public string FilePattern
		{
			get { return string.Format("FCE_*{0}", WriteAsHtml ? ".htm" : ".txt"); }
		}

		/// <summary>
		/// Writes exception details to a file in LogsFolder.
		/// Enforces the MaxFiles limit and MaxFilesDeleteAge retention policy under a concurrency lock.
		/// </summary>
		void _WriteFile(Exception ex, string subject, string body)
		{
			// Must wrap into lock so that process won't attempt to delete/write same file twice from different threads.
			lock (WriteLock)
			{
				// Create file. Group by Exception type and error code.
				var prefix = string.Format("FCE_{0}_{1:X8}", ex.GetType().Name, ex.HResult);
				var ext = WriteAsHtml ? ".htm" : ".txt";
				var di = new System.IO.DirectoryInfo(LogsFolder);
				// Create folder if not exists.
				if (!di.Exists)
					di.Create();
				// Get exception files ordered with oldest on top.
				var files = di.GetFiles(prefix + "*" + ext).OrderBy(x => x.CreationTime).ToArray();
				// If maximum number of files set limit reached then...
				if (MaxFiles > 0 && files.Length > MaxFiles)
				{
					var dateToDelete = DateTime.Now.Subtract(MaxFilesDeleteAge);
					// Get files allowed to remove.
					var filesToRemove = files
						// Make sure newest on the top.
						.OrderByDescending(x => x.CreationTime)
						// Keep allowed files.
						.Skip(MaxFiles - 1)
						// Get files older than specified date.
						.Where(x => x.CreationTime < dateToDelete).ToArray();
					// Remove oldest file.
					for (int f = 0; f < filesToRemove.Length; f++)
						filesToRemove[f].Delete();
					var filesLeft = files.Length - filesToRemove.Length;
					// If no free space for file exist then return.
					if (filesLeft >= MaxFiles)
						return;
				}
#if NETSTANDARD // .NET Standard
				var fileTime = DateTime.Now;
#elif NETCOREAPP // .NET Core
				var fileTime = DateTime.Now;
#else // .NET Framework
				var fileTime = HiResDateTime.Current.Now;
#endif
				var fileName = string.Format("{0}\\{1}_{2:yyyyMMdd_HHmmss.ffffff}{3}",
					di.FullName, prefix, fileTime, ext);
				var fi = new System.IO.FileInfo(fileName);
				var content = WriteAsHtml ? ExceptionInfo(ex, body, true) : ex.ToString();
				System.IO.File.AppendAllText(fileName, content);
			}
		}

	}
}