using System;

namespace JocysCom.RemoteController
{
	public class MoveMonitorEventArgs<T>: EventArgs
	{
		public MoveMonitorEventArgs(T value, T delta)
		{
			Value = value;
			Delta = delta;
		}

		public T Value { get; }
		public T Delta { get;  }
}
}
