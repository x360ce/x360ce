using System;

namespace x360ce.App.DInput
{
	public class DInputEventArgs : EventArgs
	{
		public DInputEventArgs(Exception error = null)
		{
			_Error = error;
		}
		public Exception Error { get { return _Error; } }
		Exception _Error;

	}
}
