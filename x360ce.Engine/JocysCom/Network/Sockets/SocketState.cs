namespace JocysCom.ClassLibrary.Network.Sockets
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net.Sockets;
	using System.Text;

	public class SocketState
	{
		public int TokenId { get; set; }
		public int SessionId { get; set; }
		public int InboxSize { get; set; }
		public int OutboxSize { get; set; }
		public string LocalAddress { get; set; }
		public int LocalPort { get; set; }
		public string RemoteAddress { get; set; }
		public int RemotePort { get; set; }
		public SocketAsyncOperation LastOperation { get; set; }
		public DateTime? LastOperationDate { get; set; }
		public bool Connected { get; set; }
		public string Message { get; set; }
		public int AvailableData { get; set; }

		public string ToCsvLine(bool getHeaders = false)
		{
			if (getHeaders)
			{
				return
				"TokenId," +
				"SessionId," +
				"InboxSize," +
				"OutboxSize," +
				"LocalAddress," +
				"LocalPort," +
				"RemoteAddress," +
				"RemotePort," +
				"LastOperation," +
				"LastOperationDate," +
				"Connected," +
				"Message," +
				"AvailableData";
			}
			object[] args = {
			TokenId,
			SessionId,
			InboxSize,
			OutboxSize,
			LocalAddress,
			LocalPort,
			RemoteAddress,
			RemotePort,
			LastOperation,
			LastOperationDate,
			Connected,
			Message,
			AvailableData,
			};
			var sb = new StringBuilder();
			for (int i = 0; i < args.Length; i++)
			{
				if (i > 0) sb.Append(',');
				var arg = args[i];
				if (arg is DateTime) sb.AppendFormat("{0:yyyy-MM-dd HH:mm:ss}", args[i]);
				else sb.AppendFormat("{0}", args[i]);
			}
			return sb.ToString();
		}


	}
}
