using System;
using System.Net.Sockets;
using System.Threading;
namespace JocysCom.ClassLibrary.Network.Sockets
{
	public class SocketDataEventArgs : EventArgs
	{

		public SocketDataEventArgs(SocketServerEventType status, DataHolder holder, SocketError error = SocketError.Success)
		{
			_Status = status;
			_Holder = holder;
			_Error = error;
		}

		SocketServerEventType _Status;
		public SocketServerEventType Status { get { return _Status; } }

		DataHolder _Holder;
		public DataHolder Holder { get { return _Holder; } }

		SocketError _Error;
		public SocketError Error { get { return _Error; } }

	}
}
