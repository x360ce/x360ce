using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace JocysCom.ClassLibrary.Runtime
{
	public partial class LogHelper
	{
		static List<ExceptionGroup> fileExceptions = new List<ExceptionGroup>();

		/// <summary>
		/// Maximum number of files per error type and ex.HResult code.
		/// </summary>
		public int MaxFiles { get { return _SP.Parse("MaxFiles", 10); } }

		/// <summary>
		/// If Maximum count of error files reached then allow remove only files older than specified by this property.
		/// </summary>
		public TimeSpan MaxFilesDeleteAge { get { return _SP.Parse("MaxFilesRemoveAge", new TimeSpan(0, 0, 10) ); } }

		public long ExceptionsCount;

		/// <summary>
		/// Write exception details to file.
		/// </summary>
		/// <param name="ex">Exception to write.</param>
		/// <param name="subject">Use custom subject instead of generated from exception</param>
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

		public string FilePattern
		{
			get { return string.Format("FCE_*{0}", WriteAsHtml ? ".htm" : ".txt"); }
		}

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
