using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace JocysCom.ClassLibrary.Network.Sockets
{
	public class SocketServerState
	{
		long Accept;
		long AcceptSuccess;
		long AcceptFailure;

		long Connect;
		long ConnectSuccess;
		long ConnectFailure;

		long Disconnect;
		long DisconnectSuccess;
		long DisconnectFailure;

		long Receive;
		long ReceiveSuccess;
		long ReceiveFailure;

		long ReceiveFrom;
		long ReceiveFromSuccess;
		long ReceiveFromFailure;

		long ReceiveMessageFrom;
		long ReceiveMessageFromSuccess;
		long ReceiveMessageFromFailure;

		long Send;
		long SendSuccess;
		long SendFailure;

		long SendPackets;
		long SendPacketsSuccess;
		long SendPacketsFailure;

		long SendTo;
		long SendToSuccess;
		long SendToFailure;

		// Acknowledgements sent and received.
		long AckSend;
		long AckReceive;

		// Sockets returned to pool.
		long Returned;
		long ReturnedSuccess;
		long ReturnedFailure;

		public int MaximumItemsUsed;

		public void IncrementAckSend()
		{
			Interlocked.Increment(ref AckSend);
		}

		public void IncrementAckReceive()
		{
			Interlocked.Increment(ref AckReceive);
		}

		public void IncrementReturned(bool error)
		{
			Interlocked.Increment(ref Returned);
			if (error) Interlocked.Increment(ref ReturnedFailure); 
			else Interlocked.Increment(ref ReturnedSuccess);
		}

		public void Increment(SocketAsyncOperation operation, SocketError? error = null)
		{
			var success = error.HasValue && error.Value == SocketError.Success;
			switch (operation)
			{
				case SocketAsyncOperation.Accept:
					if (!error.HasValue) Interlocked.Increment(ref Accept);
					else if (success) Interlocked.Increment(ref AcceptSuccess);
					else Interlocked.Increment(ref AcceptFailure);
					break;
				case SocketAsyncOperation.Connect:
					if (!error.HasValue) Interlocked.Increment(ref Connect);
					else if (success) Interlocked.Increment(ref ConnectSuccess);
					else Interlocked.Increment(ref ConnectFailure);
					break;
				case SocketAsyncOperation.Disconnect:
					if (!error.HasValue) Interlocked.Increment(ref Disconnect);
					else if (success) Interlocked.Increment(ref DisconnectSuccess);
					else Interlocked.Increment(ref DisconnectFailure);
					break;
				case SocketAsyncOperation.Receive:
					if (!error.HasValue) Interlocked.Increment(ref Receive);
					else if (success) Interlocked.Increment(ref ReceiveSuccess);
					else Interlocked.Increment(ref ReceiveFailure);
					break;
				case SocketAsyncOperation.ReceiveFrom:
					if (!error.HasValue) Interlocked.Increment(ref ReceiveFrom);
					else if (success) Interlocked.Increment(ref ReceiveFromSuccess);
					else Interlocked.Increment(ref ReceiveFromFailure);
					break;
				case SocketAsyncOperation.ReceiveMessageFrom:
					if (!error.HasValue) Interlocked.Increment(ref ReceiveMessageFrom);
					else if (success) Interlocked.Increment(ref ReceiveMessageFromSuccess);
					else Interlocked.Increment(ref ReceiveMessageFromFailure);
					break;
				case SocketAsyncOperation.Send:
					if (!error.HasValue) Interlocked.Increment(ref Send);
					else if (success) Interlocked.Increment(ref SendSuccess);
					else Interlocked.Increment(ref SendFailure);
					break;
				case SocketAsyncOperation.SendPackets:
					if (!error.HasValue) Interlocked.Increment(ref SendPackets);
					else if (success) Interlocked.Increment(ref SendPacketsSuccess);
					else Interlocked.Increment(ref SendPacketsFailure);
					break;
				case SocketAsyncOperation.SendTo:
					if (!error.HasValue) Interlocked.Increment(ref SendTo);
					else if (success) Interlocked.Increment(ref SendToSuccess);
					else Interlocked.Increment(ref SendToFailure);
					break;
				default: break;
			}

		}

		public string ToString(bool showAll)
		{
			var sb = new StringBuilder();
			if (showAll || Accept > 0) sb.AppendFormat("Accept: {0}\r\n", Accept);
			if (showAll || AcceptSuccess > 0) sb.AppendFormat("AcceptSuccess: {0}\r\n", AcceptSuccess);
			if (showAll || AcceptFailure > 0) sb.AppendFormat("AcceptFailure: {0}\r\n", AcceptFailure);
			if (showAll || Connect > 0) sb.AppendFormat("Connect: {0}\r\n", Connect);
			if (showAll || ConnectSuccess > 0) sb.AppendFormat("ConnectSuccess: {0}\r\n", ConnectSuccess);
			if (showAll || ConnectFailure > 0) sb.AppendFormat("ConnectFailure: {0}\r\n", ConnectFailure);
			if (showAll || Disconnect > 0) sb.AppendFormat("Disconnect: {0}\r\n", Disconnect);
			if (showAll || DisconnectSuccess > 0) sb.AppendFormat("DisconnectSuccess: {0}\r\n", DisconnectSuccess);
			if (showAll || DisconnectFailure > 0) sb.AppendFormat("DisconnectFailure: {0}\r\n", DisconnectFailure);
			if (showAll || Receive > 0) sb.AppendFormat("Receive: {0}\r\n", Receive);
			if (showAll || ReceiveSuccess > 0) sb.AppendFormat("ReceiveSuccess: {0}\r\n", ReceiveSuccess);
			if (showAll || ReceiveFailure > 0) sb.AppendFormat("ReceiveFailure: {0}\r\n", ReceiveFailure);
			if (showAll || ReceiveFrom > 0) sb.AppendFormat("ReceiveFrom: {0}\r\n", ReceiveFrom);
			if (showAll || ReceiveFromSuccess > 0) sb.AppendFormat("ReceiveFromSuccess: {0}\r\n", ReceiveFromSuccess);
			if (showAll || ReceiveFromFailure > 0) sb.AppendFormat("ReceiveFromFailure: {0}\r\n", ReceiveFromFailure);
			if (showAll || ReceiveMessageFrom > 0) sb.AppendFormat("ReceiveMessageFrom: {0}\r\n", ReceiveMessageFrom);
			if (showAll || ReceiveMessageFromSuccess > 0) sb.AppendFormat("ReceiveMessageFromSuccess: {0}\r\n", ReceiveMessageFromSuccess);
			if (showAll || ReceiveMessageFromFailure > 0) sb.AppendFormat("ReceiveMessageFromFailure: {0}\r\n", ReceiveMessageFromFailure);
			if (showAll || Send > 0) sb.AppendFormat("Send: {0}\r\n", Send);
			if (showAll || SendSuccess > 0) sb.AppendFormat("SendSuccess: {0}\r\n", SendSuccess);
			if (showAll || SendFailure > 0) sb.AppendFormat("SendFailure: {0}\r\n", SendFailure);
			if (showAll || SendPackets > 0) sb.AppendFormat("SendPackets: {0}\r\n", SendPackets);
			if (showAll || SendPacketsSuccess > 0) sb.AppendFormat("SendPacketsSuccess: {0}\r\n", SendPacketsSuccess);
			if (showAll || SendPacketsFailure > 0) sb.AppendFormat("SendPacketsFailure: {0}\r\n", SendPacketsFailure);
			if (showAll || SendTo > 0) sb.AppendFormat("SendTo: {0}\r\n", SendTo);
			if (showAll || SendToSuccess > 0) sb.AppendFormat("SendToSuccess: {0}\r\n", SendToSuccess);
			if (showAll || SendToFailure > 0) sb.AppendFormat("SendToFailure: {0}\r\n", SendToFailure);
			if (showAll || AckSend > 0) sb.AppendFormat("AckSend: {0}\r\n", AckSend);
			if (showAll || AckReceive > 0) sb.AppendFormat("AckReceive: {0}\r\n", AckReceive);
			if (showAll || (Send + SendTo - AckSend) > 0) sb.AppendFormat("MessageSend: {0}\r\n", Send + SendTo - AckSend);
			if (showAll || (Receive + ReceiveFrom - AckReceive) > 0) sb.AppendFormat("MessageReceive: {0}\r\n", Receive + ReceiveFrom - AckReceive);
			if (showAll || MaximumItemsUsed > 0) sb.AppendFormat("MaximumItemsUsed: {0}\r\n", MaximumItemsUsed);

			return sb.ToString();
		}

	}
}
