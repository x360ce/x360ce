using System;
using System.Collections.Generic;
using System.Linq;

namespace JocysCom.ClassLibrary.Runtime
{
	public partial class LogHelper
	{
		static List<ExceptionGroup> fileExceptions = new List<ExceptionGroup>();

		public int MaxFiles { get { return ParseInt(_configPrefix + "MaxFiles", 10); } }

		/// <summary>
		/// Write exception details to file.
		/// </summary>
		/// <param name="ex">Exception to write.</param>
		/// <param name="subject">Use custom subject instead of generated from exception</param>
		public void WriteException(Exception ex, string subject = null, string body = null)
		{
			_GroupException(fileExceptions, ex, subject, body, _WriteFile);
		}


		void _WriteFile(Exception ex, string subject, string body)
		{
			var ev = WritingException;
			var le = new LogHelperEventArgs() { Exception = ex };
			if (ev != null)
				ev(this, le);
			if (le.Cancel)
				return;
			// Must wrap into lock so that process won't attempt to delete/write same file twice from different threads.
			lock (WriteLock)
			{
				// Create file.
				var prefix = "FCE_" + ex.GetType().Name;
				var ext = WriteAsHtml ? ".htm" : ".txt";
				var di = new System.IO.DirectoryInfo(_LogFolder);
				// Create folder if not exists.
				if (!di.Exists)
					di.Create();
				// Get exception files ordered with oldest on top.
				var files = di.GetFiles(prefix + "*." + ext).OrderBy(x => x.CreationTime).ToArray();
				// Remove excess files if necessary.
				if (MaxFiles > 0 && files.Count() > 0 && files.Count() > MaxFiles)
				{
					// Remove oldest file.
					files[0].Delete();
				}
				var fileTime = HiResDateTime.Current.Now;
				var fileName = string.Format("{0}\\{1}_{2:yyyyMMdd_HHmmss.ffffff}{3}",
					di.FullName, prefix, fileTime, ext);
				var fi = new System.IO.FileInfo(fileName);
				var content = WriteAsHtml ? ExceptionInfo(ex, body) : ex.ToString();
				System.IO.File.AppendAllText(fileName, content);
			}
		}

	}
}
