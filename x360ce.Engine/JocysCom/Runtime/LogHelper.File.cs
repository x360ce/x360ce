using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Media;

namespace JocysCom.ClassLibrary.Runtime
{
	public partial class LogHelper
	{
		static List<ExceptionGroup> fileExceptions = new List<ExceptionGroup>();

		public int MaxFiles { get { return _SP.Parse("MaxFiles", 10); } }

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
				var files = di.GetFiles(prefix + "*." + ext).OrderBy(x => x.CreationTime).ToArray();
				// Remove excess files if necessary.
				if (MaxFiles > 0 && files.Length > 0 && files.Length > MaxFiles)
				{
					// Remove oldest file.
					files[0].Delete();
				}
#if NETSTANDARD
				var fileTime = DateTime.Now;
#else
				var fileTime = HiResDateTime.Current.Now;
#endif
				var fileName = string.Format("{0}\\{1}_{2:yyyyMMdd_HHmmss.ffffff}{3}",
					di.FullName, prefix, fileTime, ext);
				var fi = new System.IO.FileInfo(fileName);
				var content = WriteAsHtml ? ExceptionInfo(ex, body) : ex.ToString();
				// Wrap into html element and specify UTF-8 encoding.
				content = "<html><head><meta charset=\"UTF-8\" /></head><body>" + content + "</body></html>";
				System.IO.File.AppendAllText(fileName, content);
			}
		}

	}
}
