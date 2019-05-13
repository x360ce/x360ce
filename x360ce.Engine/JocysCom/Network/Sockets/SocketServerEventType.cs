namespace JocysCom.ClassLibrary.Network.Sockets
{
	public enum SocketServerEventType
	{
		None = 0,
		// Opening main socket listener.
		Opening,
		Opened,
		// Opening connection (client, TCP only).
		Connecting,
		Connected,
		// Accepting connection (server, TCP only).
		Accepting,
		Accepted,
		// Receiving data.
		Receiving,
		Received,
		// Sending Data.
		Sending,
		Sent,
		// Closing connection (stop receiving/sending).
		Disconnecting,
		Disconnected,
		// Closing main socket listener.
		Closing,
		Closed,
	}
}
