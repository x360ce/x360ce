namespace JocysCom.ClassLibrary.Runtime
{
	/// <summary>
	/// Write logs lines to a single file.
	/// </summary>
	public partial class LogHelper
	{
		object _LogFileWriterLock = new object();

		public IO.LogFileWriter LogFileWriter { get { return _LogFileWriter; } }
		IO.LogFileWriter _LogFileWriter;

		public bool LogToFile
		{
			get
			{
				lock (_LogFileWriterLock)
				{
					return _LogFileWriter != null;
				}
			}
			set
			{
				lock (_LogFileWriterLock)
				{
					// If logging must be enabled but writer is empty then...
					if (value && _LogFileWriter == null)
					{
						_LogFileWriter = new IO.LogFileWriter(_configPrefix);
					}
					// If logging must be disabled but writer exists then....
					else if (!value && _LogFileWriter != null)
					{
						_LogFileWriter.Dispose();
						_LogFileWriter = null;
					}
				}
			}
		}

		public void WriteToLogFile(string format, params object[] args)
		{
			lock (_LogFileWriterLock)
			{
				if (_LogFileWriter != null)
					_LogFileWriter.WriteLine(format, args);
			}
		}

	}
}
