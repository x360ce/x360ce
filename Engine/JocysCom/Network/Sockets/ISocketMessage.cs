using System.Collections.Generic;

namespace JocysCom.ClassLibrary.Network.Sockets
{
	public interface ISocketMessage
	{
		byte[] ToBytes();
		bool IsAck();
		string GetMessageType();
		int FromBytes(byte[] value, int startIndex, out string error);
		string ToString();
		string ToString(string format, List<int> indexes = null);
	}
}
