using System;

namespace JocysCom.ClassLibrary
{
	public class EventArgs<T> : EventArgs
	{
		public EventArgs()
		{
		}

		public EventArgs(T data)
		{
			_data = data;
		}

		T _data;
		public T Data { get { return _data; } }

	}
}
