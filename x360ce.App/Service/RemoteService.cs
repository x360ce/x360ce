using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace x360ce.App.Service
{
	/// <summary>
	/// Use this class to receive controller states from remote clients.
	/// </summary>
	public partial class RemoteService : IDisposable
	{

		void AddLog(string format, params object[] args)
		{
			Trace.Write(string.Format(format, args));
		}

		UdpClient server;

		public bool IsRunning { get; private set; }

		public void StartServer()
		{
			var o = SettingsManager.Options;
			AddLog("Starting remote server... ");
			if (IsRunning)
			{
				AddLog(" already started.\r\n");
				return;
			}
			IsRunning = true;
			// Set local endpoint.
			var localEndpoint = new IPEndPoint(IPAddress.Any, o.RemotePort);
			// Create client.
			server = new UdpClient();
			// Workaround for: UDP SocketException - Only one usage of each socket address is normally permitted.
			server.ExclusiveAddressUse = false;
			server.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			server.Client.Bind(localEndpoint);
			AddLog("started.\r\n");
			Receive();
		}

		public void StopServer()
		{
			AddLog("Stopping remote server... ");
			if (!IsRunning)
			{
				AddLog("already stopped.\r\n");
				return;
			}
			IsRunning = false;
			server.Close();
			AddLog("stopped.\r\n");
		}

		void Receive()
		{
			try
			{
				AddLog("Receiving on {0}...\r\n", server.Client.LocalEndPoint);
				server.BeginReceive(new AsyncCallback(ReceiveCallBack), null);
			}
			catch (Exception ex)
			{
				AddLog("Receiving Error {0}.\r\n", ex.Message);
				IsRunning = false;
			}
		}

		private void ReceiveCallBack(IAsyncResult res)
		{
			// If disposed then simply return.
			if (server.Client == null)
			{
				IsRunning = false;
				return;
			}
			try
			{
				var remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
				var bytes = server.EndReceive(res, ref remoteEndpoint);
				var data = string.Join("", bytes.Select(x => x.ToString("X2")));
				AddLog("Received Data from {0}: {1}\r\n", remoteEndpoint, data);
				if (!IsRunning)
					Receive();
			}
			catch (ObjectDisposedException)
			{
				// If disposed then simply return.
				if (!IsRunning)
					return;
				throw;
			}
		}

		#region ■ IDisposable

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				// Dispose managed resources.
				if (server != null)
					server.Dispose();
			}
			// Free native resources.
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

	}
}
