using JocysCom.ClassLibrary.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace JocysCom.ClassLibrary.Network.Sockets
{
	public class SocketServer : IDisposable
	{

		#region Initialise

		/// <summary>
		/// Buffers for sockets are unmanaged by .NET. So memory used for buffers gets "pinned",
		/// which makes the .NET garbage collector work around it, fragmenting the memory.
		/// Circumvent this problem by putting all buffers together in one block in memory.
		/// Then we will assign a part of that space to each SocketAsyncEventArgs object,
		/// and reuse that buffer space each time we reuse the SocketAsyncEventArgs object.
		/// Create a large reusable set of buffers for all socket operations.
		/// </summary>
		/// <remarks>
		/// It is impossible to prevent TIME_WAIT. Either server or client will have the problem any way,
		/// depending only on who initiates a closure of the connection first.
		/// If it's the client who closes the connection, there will be no TIME_WAIT on server.
		/// If it's the server who closes first, than there will be no TIME_WAIT on client.
		/// In other words: Closing first guarantees TIME_WAIT.
		/// So the only option that is left to do is using SO_REUSEADDR.
		/// IMPORTANT: Event with SO_REUSEADDR option it is still impossible
		/// to use the reused address for contacting previously disconnected host.
		///
		/// TCP: Ethernet, establishes no link-level connection, so
		/// if you unplug the network cable, a remote host can't tell that its peer is
		/// physically unable to communicate.
		/// </remarks>
		byte[] sendReceiveBuffer;

		public SocketSettings Settings;
		public delegate bool OnMessageAddRemoveDelegate(DataHolder holder, object userState = null);
		public OnMessageAddRemoveDelegate OnAddNewMessage;
		public OnMessageAddRemoveDelegate OnRemoveOldMessage;
		public event EventHandler<EventArgs<bool>> NetworkAvailabilityChanged;
		/// <summary>Pool of reusable SocketAsyncEventArgs objects for receive and send socket operations.</summary>
		//public BlockingStack<SocketAsyncEventArgs> EventArgsPool;


		object EventArgsLock = new object();
		public List<SocketAsyncEventArgs> BusyEventArgs;
		public List<SocketAsyncEventArgs> FreeEventArgs;

		public BindingList<DataHolder> SendQueue;
		object SendQueueLock = new object();

		public EventHandler<SocketDataEventArgs> OnData;
		public EventHandler<EventArgs<Exception>> ErrorReceived;

		public SocketServerState State = new SocketServerState();

		/// <summary>
		/// Initialise new socket server.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="writer"></param>
		/// <param name="isNetworkAvailable">Assume that network is available and connect from the start.</param>
		public SocketServer(SocketSettings settings, SocketLogFileWriter writer = null, bool isNetworkAvailable = true)
		{
			// Assign core properties first.
			Settings = settings;
			if (writer is null)
			{
				writer = new SocketLogFileWriter(settings);
				LogWriterDispose = true;
			}
			_LogWriter = writer;
			_IsNetworkAvailable = isNetworkAvailable;
			SendQueue = new BindingList<DataHolder>();
			// Assign default functions which will be overridden.
			ProcessHead = ProcessHeadDefault;
			// Start logging.
			LogWriter.WriteFlow("New Flow");
			LogWriter.WriteThreads("New Threads");
			// Preallocate one large byte buffer block for all send and receive operations.
			// This guards against memory fragmentation.
			var size = Settings.MaxSimultaneousOperations * Settings.BufferSize;
			LogWriter.WriteFlow("Initialize SendReceiveBuffer[{0}] // {1} operations * {2} bytes",
				size, Settings.MaxSimultaneousOperations, Settings.BufferSize);
			sendReceiveBuffer = new byte[size];
			// Initialize
			InitializeEventArgs();
			InitializeConnectionMonitor();
			InitializeQueueMonitor();
			// Flush current network state.
			var file = LogWriter.CurrentFileFileName ?? string.Format(LogWriter.LogFileName, DateTime.Now);
			var logFileName = System.IO.Path.GetFileNameWithoutExtension(file);
			var directoryName = System.IO.Path.GetDirectoryName(file);
			// File: Status.txt
			var stateFileName = logFileName + ".Status.txt";
			stateLogFullName = System.IO.Path.Combine(directoryName, stateFileName);
			// Create watcher which will recreate file if deleted.
			watcherStatus = new System.IO.FileSystemWatcher(directoryName, stateFileName);
			watcherStatus.Deleted += new System.IO.FileSystemEventHandler(watcherStatus_Deleted);
			watcherStatus.EnableRaisingEvents = true;
			// File: NetStat.txt
			var netstatFileName = logFileName + ".NetStat.txt";
			netstatLogFullName = System.IO.Path.Combine(directoryName, netstatFileName);
			// Create watcher which will recreate file if deleted.
			watcherNetStat = new System.IO.FileSystemWatcher(directoryName, netstatFileName);
			watcherNetStat.Deleted += new System.IO.FileSystemEventHandler(watcherNetStat_Deleted);
			watcherNetStat.EnableRaisingEvents = true;
			// Flush states.
			FlushCurrentState();
			FlushCurrentNetStat();
			// If protocol is UDP.
			if (!IsTcp) InitializeKeepAliveTimer();
			// Start timers.
			RestartQueueMonitorTimer();
			RestartConnectionMonitorTimer();
		}

		bool IsTcp { get { return Settings.ProtocolType == ProtocolType.Tcp; } }

		bool AllowToProcess { get { return IsNetworkAvailable && MustListen && !IsDisposing; } }

		/// <remarks>
		/// Initialize EventArgs pool which will be used for socket operations.
		/// </remarks>
		void InitializeEventArgs()
		{
			lock (EventArgsLock)
			{
				// Preallocate pool of SocketAsyncEventArgs objects for connect/accept/receive/send operations.
				LogWriter.WriteFlow("Create EventArgs[{0}] List", Settings.MaxSimultaneousOperations);
				BusyEventArgs = new List<SocketAsyncEventArgs>();
				FreeEventArgs = new List<SocketAsyncEventArgs>();
				for (int i = 0; i < Settings.MaxSimultaneousOperations; i++)
				{
					// If you have different needs for the send versus the receive sockets, then
					// you might want to allocate a separate pool of SocketAsyncEventArgs for send
					// operations instead of having SocketAsyncEventArgs that do both receiving
					// and sending operations.
					//
					// Allocate the SocketAsyncEventArgs object for this loop,
					// to go in its place in the stack which will be the pool
					// for receive/send operation context objects.
					var item = new SocketAsyncEventArgs();
					// TokenID must start from 0. It will be used for buffer offset.
					var bufferOffset = Settings.BufferSize * i;
					// Attach the SocketAsyncEventArgs object
					// to its event handler. Since this SocketAsyncEventArgs object is
					// used for both receive and send operations, whenever either of those
					// completes, the IO_Completed method will be called.
					item.Completed += new EventHandler<SocketAsyncEventArgs>(SocketAsyncEventArgs_Completed);
					// Store data in the UserToken property of SAEA object.
					var token = new UserToken(bufferOffset, i);
					// If clients must use specific port then...
					if (Settings.UseServerPortForClients)
					{
						token.ClientPort = Settings.ServerPort;
					}
					// If every client must use specific port then...
					else if (Settings.ClientPortRangeStart > 0)
					{
						token.ClientPort = Settings.ClientPortRangeStart + i;
					}
					// We'll have an object that we call DataHolder, that we can remove from
					// the UserToken when we are finished with it. So, we can hang on to the
					// DataHolder, pass it to an application, serialize it, or whatever.
					// token.theDataHolder = new DataHolder();
					item.UserToken = token;
					// Add this SocketAsyncEventArg object to the pool.
					FreeEventArgs.Add(item);
				}
			}
		}

		#endregion

		#region Client Lock

		TimeSpan lockTimeSpan = new TimeSpan(0, 0, 5);
		ReaderWriterLock ClientLock = new ReaderWriterLock();
		string _lockName = "";

		public void AcquireLock(string lockName)
		{
			ClientLock.AcquireWriterLock(lockTimeSpan);
			_lockName = lockName;
		}

		public Exception ReleaseLock(string lockName)
		{
			var isWriterLockHeld = ClientLock.IsWriterLockHeld;
			Exception ex = null;
			if (isWriterLockHeld)
			{
				ClientLock.ReleaseWriterLock();
			}
			else
			{
				ex = new Exception(string.Format("Exception WriterLock is no held! Current Lock = {0}, Lock attempted to release = {1}", _lockName, lockName));
			}
			_lockName = "";
			return ex;
		}

		#endregion

		#region Logging and Monitoring

		bool? IsWaitingPrevious;

		/// <summary>
		/// Returns true if at least one socket is in accepting or receiving state on ServerPort.
		/// </summary>
		public bool IsWaiting { get { return WaitCount > 0; } }

		bool _IsNetworkAvailable;
		object IsNetworkAvailableLock = new object();

		public bool IsNetworkAvailable
		{
			get
			{
				return _IsNetworkAvailable;
			}
			set
			{
				bool changed = false;
				lock (IsNetworkAvailableLock)
				{
					if (_IsNetworkAvailable != value)
					{
						changed = true;
						_IsNetworkAvailable = value;
						LogWriter.WriteFlow("{0,-29}: {1}", "IsNetworkAvailable", value);
					}
				}
				if (changed && !_IsNetworkAvailable) CloseListener();
			}
		}

		public SocketLogFileWriter LogWriter { get { return _LogWriter; } }
		SocketLogFileWriter _LogWriter;

		// True if log writer was created inside and must be disposed.
		bool LogWriterDispose;

		#endregion

		#region Log Network State

		void watcherStatus_Deleted(object sender, System.IO.FileSystemEventArgs e)
		{
			FlushCurrentState();
		}


		void watcherNetStat_Deleted(object sender, System.IO.FileSystemEventArgs e)
		{
			FlushCurrentNetStat();
		}

		System.IO.FileSystemWatcher watcherStatus;
		System.IO.FileSystemWatcher watcherNetStat;
		string stateLogFullName;
		string netstatLogFullName;

		public void FlushCurrentState()
		{
			LogWriter.Flush();
			var states = GetBusyStates();
			var sb = new StringBuilder();
			sb.AppendFormat("Tokens = {0}\r\n", states.Count());
			var totals =
				(from row in states
				 group row by new { Lo = row.LastOperation } into grouped
				 select new
				 {
					 LastOperation = grouped.Key.Lo,
					 RowsCount = grouped.Count()
				 }).ToArray();
			for (int i = 0; i < totals.Count(); i++)
			{
				var total = totals[i];
				sb.AppendFormat("  {0} = {1}\r\n", total.LastOperation, total.RowsCount);
			}
			for (int i = 0; i < states.Count; i++)
			{
				if (i == 0) sb.AppendLine(states[i].ToCsvLine(true));
				sb.AppendLine(states[i].ToCsvLine());
			}
			System.IO.File.WriteAllText(stateLogFullName, sb.ToString());
		}


		public void FlushCurrentNetStat()
		{
			LogWriter.Flush();
			System.IO.File.WriteAllText(netstatLogFullName, Network.NetworkHelper.GetExtendedTable());
		}

		public List<SocketState> GetBusyStates()
		{
			lock (EventArgsLock)
			{
				var list = BusyEventArgs;
				var states = new List<SocketState>();
				for (int i = 0; i < list.Count; i++)
				{
					var args = list[i];
					var state = new SocketState();
					var token = (UserToken)args.UserToken;
					var acceptSocket = args.AcceptSocket;
					if (acceptSocket != null)
					{
						var localEndpoint = (IPEndPoint)acceptSocket.LocalEndPoint;
						if (localEndpoint != null)
						{
							state.LocalAddress = localEndpoint.Address.ToString();
							state.LocalPort = localEndpoint.Port;
						}
						var remoteEndpoint = acceptSocket.Connected ? (IPEndPoint)acceptSocket.RemoteEndPoint : null;
						if (remoteEndpoint != null)
						{
							state.RemoteAddress = remoteEndpoint.Address.ToString();
							state.RemotePort = remoteEndpoint.Port;
						}
						state.Connected = acceptSocket.Connected;
						state.AvailableData = acceptSocket.Available;
					}
					state.LastOperation = args.LastOperation;
					state.LastOperationDate = token.LastOperationDate;
					state.TokenId = token.TokenId;
					state.SessionId = token.SessionId;
					states.Add(state);
				}
				return states;
			}
		}

		#endregion

		#region Connection Monitor

		QueueTimer connectionMonitor;
		object connectionMonitorLock = new object();

		void InitializeConnectionMonitor()
		{
			lock (connectionMonitorLock)
			{
				connectionMonitor = new QueueTimer(50, 5000);
				connectionMonitor.DoWork += connectionMonitor_DoWork;
			}
		}

		void DisposeConnectionMonitor()
		{
			lock (connectionMonitorLock)
			{
				if (connectionMonitor != null)
				{
					connectionMonitor.Dispose();
					connectionMonitor = null;
				}
			}
		}

		public void RestartConnectionMonitorTimer()
		{
			lock (connectionMonitorLock)
			{
				if (connectionMonitor != null)
				{
					connectionMonitor.DoActionNow();
				}
			}
		}

		/// <summary>
		/// One instance of this method will run at the time, because it is wrapped inside internal QueueTimer lock.
		/// </summary>
		void connectionMonitor_DoWork(object sender, QueueTimerEventArgs e)
		{
			// If network is unavailable then quit. This method will be called again when network availability changes.
			if (!IsNetworkAvailable) return;
			var value = IsWaiting;
			if (!IsWaitingPrevious.HasValue || (IsWaitingPrevious.Value != value))
			{
				IsWaitingPrevious = value;
				// For thread safety: Take a local copy.  If an unsubscribe on another thread
				// causes 'NetworkAvailabilityChanged' to become null, this will protect you from a null reference exception.
				// (The event will be raised to all subscribers as of the point in time that this line executes.)
				var ev = NetworkAvailabilityChanged;
				var e1 = new EventArgs<bool>(value);
				// Check for no subscribers
				if (ev != null) { ev(this, e1); }
			}
			CheckListener();
		}

		#endregion

		void CheckIdleSockets()
		{
			//if (Settings.ReceiveDisconnectTimeout <= 0) return;
			//for (int i = 0; i < BusyEventArgs.Count; i++)
			//{
			//	if (IsDisposing) return;
			//	var args = BusyEventArgs[i];
			//	var token = (UserToken)args.UserToken;
			//	if (DateTime.Now.Subtract(token.LastOperationDate).TotalSeconds > Settings.ReceiveDisconnectTimeout)
			//	{
			//		// Disconnect all sockets by default.
			//		var disconnect = true;
			//		// Do not disconnect main TCP accept or main UDP ReceiveFrom
			//		string localPrefix;
			//		IPEndPoint localEndpoint;
			//		try
			//		{
			//			GetLocalEndpoint(args, out localPrefix, out localEndpoint);
			//			if (localEndpoint != null)
			//			{
			//				var isServerPort = Settings.ServerPort == localEndpoint.Port;
			//				if (IsTcp)
			//				{
			//					// Don't disconnect accepting sockets.
			//					if (isServerPort && args.LastOperation == SocketAsyncOperation.Accept) disconnect = false;
			//				}
			//				else
			//				{
			//					// Don't disconnect receiving sockets.
			//					if (isServerPort && args.LastOperation == SocketAsyncOperation.ReceiveFrom) disconnect = false;
			//				}
			//			}
			//		}
			//		catch (Exception ex)
			//		{
			//			LogWriter.WriteError(ex.ToString());
			//		}
			//		if (disconnect) CloseAcceptSocket(args, "DisconnectMonitor_Elapsed");
			//	}
			//}
		}

		#region Queue Monitor

		QueueTimer queueMonitor;
		object queueMonitorLock = new object();

		void InitializeQueueMonitor()
		{
			lock (queueMonitorLock)
			{
				queueMonitor = new QueueTimer(50, 5000);
				queueMonitor.DoWork += queueMonitor_DoWork;
			}
		}

		void DisposeQueueMonitor()
		{
			lock (queueMonitorLock)
			{
				if (queueMonitor != null)
				{
					queueMonitor.Dispose();
					queueMonitor = null;
				}
			}
		}

		public void RestartQueueMonitorTimer()
		{
			lock (queueMonitorLock)
			{
				if (queueMonitor != null)
				{
					queueMonitor.DoActionNow();
				}
			}
		}

		public List<DataHolder> GetFromSendQueue(bool useLock, Func<DataHolder, bool> predictate, int? take = null)
		{
			var list = new List<DataHolder>();
			var queue = SendQueue.Where(predictate);
			if (take.HasValue) queue = queue.Take(take.Value);
			if (useLock)
			{
				lock (SendQueueLock) { list = queue.ToList(); }
			}
			else
			{
				list = queue.ToList();
			}
			return list;
		}

		public int SendQueueCount(Func<DataHolder, bool> predictate = null)
		{
			lock (SendQueueLock)
			{
				int count = 0;
				count = (predictate is null)
					? SendQueue.Count
					: SendQueue.Count(predictate);
				return count;
			}
		}


		public DataHolder[] SendQueueClone(Func<DataHolder, bool> predictate = null)
		{
			lock (SendQueueLock)
			{
				var list = (predictate is null)
					? SendQueue.ToArray()
					: SendQueue.Where(predictate).ToArray();
				return list;
			}
		}

		/// <summary>
		/// Begins sending operation if queue contains messages.
		/// </summary>
		void queueMonitor_DoWork(object sender, QueueTimerEventArgs e) //{CheckMessageQueueThread()
		{
			UpdateWaitCount();
		}

		#endregion

		#region Keep Alive / UDP hole punching

		System.Timers.Timer keepAliveTimer;
		object keepAliveTimerLock = new object();
		public event EventHandler<SocketAsyncEventArgs> KeepAlive;

		void InitializeKeepAliveTimer()
		{
			lock (keepAliveTimerLock)
			{
				if (keepAliveTimer is null)
				{
					keepAliveTimer = new System.Timers.Timer();
					keepAliveTimer.Interval = Settings.KeepAliveInterval;
					keepAliveTimer.Elapsed += KeepAliveTimer_Elapsed;
					keepAliveTimer.AutoReset = false;
					Settings.PropertyChanged += Settings_PropertyChanged;
				}
			}
		}

		void DisposeKeepAliveTimer()
		{
			lock (keepAliveTimerLock)
			{
				if (keepAliveTimer != null)
				{
					Settings.PropertyChanged -= Settings_PropertyChanged;
					keepAliveTimer.Dispose();
					keepAliveTimer = null;
				}
			}
		}

		private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			lock (keepAliveTimerLock)
			{
				if (keepAliveTimer != null)
				{
					if (e.PropertyName == nameof(Settings.KeepAliveEnabled))
					{
						keepAliveTimer.Enabled = Settings.KeepAliveEnabled;
					}
					else if (e.PropertyName == nameof(Settings.KeepAliveInterval))
					{
						// Call Close() to prevent timer auto-restart when Interval changed.
						keepAliveTimer.Close();
						keepAliveTimer.Interval = Settings.KeepAliveInterval;
					}
				}
			}
		}

		void ResetKeepAliveTimer()
		{
			lock (keepAliveTimerLock)
			{
				if (keepAliveTimer != null)
				{
					keepAliveTimer.Stop();
					if (Settings.KeepAliveEnabled) keepAliveTimer.Start();
				}
			}
		}

		void KeepAliveTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			lock (keepAliveTimerLock)
			{
				try
				{
					KeepAliveAction(e);
				}
				catch (Exception)
				{
					throw;
				}
				finally
				{
					// Finally - restart timer even if exception was thrown.
					if (keepAliveTimer != null)
					{
						keepAliveTimer.Start();
					}
				}
			}
		}

		void KeepAliveAction(System.Timers.ElapsedEventArgs e)
		{
			var ev = KeepAlive;
			// keepAliveTimer will be created only in UDP.
			if (ev != null && keepAliveTimer != null)
			{
				var args = new SocketAsyncEventArgs();
				var token = new UserToken(0, 0);
				token.MessageToSendBytes = new byte[0];
				args.UserToken = token;
				var ra = Settings.DefaultRemoteAddress;
				var rp = Settings.DefaultRemotePort;
				if (ra != null && rp > 0)
				{
					args.RemoteEndPoint = new IPEndPoint(ra, rp);
				}
				// Call event handled which can override some properties.
				ev(this, args);
				token = (UserToken)args.UserToken;
				var data = token.MessageToSendBytes;
				var rep = args.RemoteEndPoint;
				if (data != null && rep != null)
				{
					var localEndpoint = new IPEndPoint(Settings.ServerAddress, Settings.ServerPort);
					using (var socket = new Socket(Settings.ServerAddress.AddressFamily, SocketType.Dgram, Settings.ProtocolType))
					{
						socket.ExclusiveAddressUse = false;
						socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
						// Set IP and port which will be used locally.
						socket.Bind(localEndpoint);
						socket.SendTo(data, rep);
					}
				}
			}
		}

		#endregion

		#region Shared Functions

		public bool SingleMessageSendMode = false;


		/// <summary>
		/// Add new message to send.
		/// </summary>
		/// <param name="message">Message to send.</param>
		/// <param name="oriRE">Original remote endpoint. Default remote Address and Port will be updated.</param>
		/// <param name="delRE">Delivery remote endpoint. If specified, will be used as message delivery address.</param>
		public void AddMessageToSend(ISocketMessage message, IPEndPoint oriRE = null, IPEndPoint delRE = null)
		{
			if (oriRE is null && delRE is null)
				throw new ArgumentNullException("Original Remote Endpoint (oriRE) of Delivery Remote Endpoint (delRE) must be specified.");
			var newHolder = new DataHolder();
			newHolder.OriginalRemoteEndpoint = oriRE;
			newHolder.MessageToSend = message;
			// If delivery remote endpoint specified then...
			if (delRE != null)
			{
				newHolder.DeliveryRemoteEndpoint = delRE;
			}
			// If reply must be sent to different address or port number then...
			else if (Settings.DefaultRemoteAddress != null || Settings.DefaultRemotePort != 0)
			{
				IPEndPoint defaultDelRE = new IPEndPoint(
					oriRE.Address is null ? Settings.DefaultRemoteAddress : oriRE.Address,
					oriRE.Port == 0 ? Settings.DefaultRemotePort : oriRE.Port
				);
				newHolder.DeliveryRemoteEndpoint = defaultDelRE;
			}
			AddMessageToSend(newHolder);
		}

		/// <summary>
		/// Add one message to send.
		/// </summary>
		/// <param name="holders">Message holder to send.</param>
		/// <param name="userState">User state which will be passed to OnAddNewMessage method.</param>
		/// <param name="index">Index in the message queue where message will be inserter.</param>
		public void AddMessageToSend(DataHolder holder, object userState = null, int? index = null)
		{
			AddMessageToSend(new[] { holder }, userState, index);
		}

		/// <summary>
		/// Add messages to send. Start this message without parameters to initialize queue check.
		/// </summary>
		/// <param name="holders">Message holders to send.</param>
		/// <param name="userState">User state which will be passed to OnAddNewMessage method.</param>
		/// <param name="index">Index in the message queue where message will be inserter.</param>
		public void AddMessageToSend(IList<DataHolder> holders = null, object userState = null, int? index = null)
		{
			var allow = true;
			lock (SendQueueLock)
			{
				if (holders != null)
				{
					for (int i = 0; i < holders.Count; i++)
					{
						var holder = holders[i];
						var m = OnAddNewMessage;
						// If method attached then check for permission to send.
						if (m != null)
						{
							var er = ErrorReceived;
							if (er is null)
							{
								allow = m(holder, userState);
							}
							else
							{
								try
								{
									allow = m(holder, userState);
								}
								catch (Exception methodEx)
								{
									allow = false;
									methodEx.Data.Add("ErrorReceived", "AddMessageToSend");
									var e = new EventArgs<Exception>(methodEx);
									er(this, e);
								}
							}
						}
						if (allow)
						{
							// If index specified and position is in range then...
							if (index.HasValue && index.Value < SendQueue.Count)
							{
								// Index must be not less than zero.
								var ix = Math.Max(0, index.Value);
								SendQueue.Insert(ix, holder);
							}
							else
							{
								SendQueue.Add(holder);
							}
						}
					}
				}
			}
			// Restart monitor event id no items.
			RestartQueueMonitorTimer();
			if (allow) RestartConnectionMonitorTimer();
		}

		public void ClearMessages()
		{
			lock (SendQueueLock)
			{
				SendQueue.Clear();
			}
		}

		public void RemoveMessage(DataHolder holder, object userState = null)
		{
			var allow = true;
			lock (SendQueueLock)
			{
				var m = OnRemoveOldMessage;
				if (m != null)
				{
					var er = ErrorReceived;
					if (er is null)
					{
						allow = m(holder, userState);
					}
					else
					{
						try
						{
							allow = m(holder, userState);
						}
						catch (Exception methodEx)
						{
							allow = false;
							methodEx.Data.Add("ErrorReceived", "RemoveMessage");
							var e = new EventArgs<Exception>(methodEx);
							er(this, e);
						}
					}
				}
				if (allow)
				{
					if (SendQueue.Contains(holder))
					{
						SendQueue.Remove(holder);
						//LastRemovedData = message;
					}
					// If this is single send mode then...
					if (SingleMessageSendMode)
					{
						// ...only one message will be sent when queue timer elapses.
						// Check for next message in the queue to send.
						RestartQueueMonitorTimer();
					}
				}
			}
		}

		Socket GetNewSocket(string logPrefix, int localPort, SocketAsyncEventArgs args)
		{
			var socketType = IsTcp ? SocketType.Stream : SocketType.Dgram;
			var socket = new Socket(Settings.ServerAddress.AddressFamily, socketType, Settings.ProtocolType);
			if (IsTcp)
			{
				// Send operations will time-out if confirmation
				// is not received within 1000 milliseconds.
				//socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 1000);
				// The socket will linger for 5 seconds after Socket.Close is called.
				//LingerOption lingerOption = new LingerOption(true, 5);
				//socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, lingerOption);
				socket.ExclusiveAddressUse = false;
				socket.NoDelay = true;
				// Allow to listen on port on same address even if another application already has it open.
				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
			}
			else
			{
				socket.ExclusiveAddressUse = false;
				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
				// If this UDP socket ever sends a UDP packet to a remote destination that exists
				// but there is no socket to receive the packet, an ICMP port unreachable message
				// is returned to the sender. By default, when this is received the next operation
				// on the UDP socket that send the packet will receive a SocketException.
				// The native (Winsock) error that is received is WSAECONNRESET (10054).
				// http://msdn.microsoft.com/en-us/library/cc242275.aspx
				// http://msdn.microsoft.com/en-us/library/bb736550(VS.85).aspx
				// https://support.microsoft.com/en-us/kb/263823
				// SIO_UDP_CONNRESET IOCTL code (command) controls whether UDP PORT_UNREACHABLE
				// messages are reported.
				const uint IOC_IN = 0x80000000;
				const uint IOC_VENDOR = 0x18000000;
				const uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
				// 0x9800000C == 2550136844 (uint) == -1744830452 (int)
				int ioControlCode = unchecked((int)SIO_UDP_CONNRESET);
				// Set to TRUE (1) to enable reporting.
				// Set to FALSE (0) to disable reporting i.e. Ignore ICMP "port unreachable" messages when sending an UDP datagram.
				byte[] optionInValue = new byte[] { 0, 0, 0, 0 }; // { Convert.ToByte(false) };
				byte[] optionOutValue = null;
				socket.IOControl(ioControlCode, optionInValue, optionOutValue);
				// The default socket buffer size in Windows sockets is 8192 bytes.
				// Increase the receive buffer to 65535 bytes or some UDP data packets will be lost.
				if (Settings.BufferSize > 8192)
				{
					socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, Settings.BufferSize);
				}
			}
			// Bind it to the port.
			var localEndpoint = new IPEndPoint(Settings.ServerAddress, localPort);
			if (args is null)
			{
				LogWriter.WriteFlow("{0,-29}: LocalEndpoint = {1}.", logPrefix, localEndpoint);
			}
			else
			{
				var token = (UserToken)args.UserToken;
				ShowToken(logPrefix, token.TokenId, "LocalEndpoint = {0}.", localEndpoint);
			}
			// Set IP and port which will be used locally.
			socket.Bind(localEndpoint);
			return socket;
		}

		public IPEndPoint GetOriginalRemoteEndpoint(SocketAsyncEventArgs args, out string remoteEndpointString)
		{
			// Identify remote endpoint and set it onto holder.
			IPEndPoint oriRE;
			IPEndPoint accRE;
			IPEndPoint conRE;
			IPEndPoint argRE;
			GetRemoteEndpoint(args, out accRE, out conRE, out argRE);
			remoteEndpointString = IsTcp
				? string.Format("ArgRE = {0}, AccRE = {1}, ConRE = {2}", argRE, accRE, conRE)
				: string.Format("ArgRE = {0}", argRE);
			oriRE = accRE ?? conRE ?? argRE;
			return oriRE;
		}

		/// <summary>
		/// Get remote end point (client's IP address and port number) from SocketAsyncEventArgs.
		/// </summary>
		/// <param name="args">SocketAsyncEventArgs from which remote endpoint will be extracted.</param>
		/// <param name="remotePrefix">String prefix which shows part from which remote endpoint was extracted.</param>
		/// <param name="remoteEndpoint">RemoteEndPoint data.</param>
		static void GetRemoteEndpoint(SocketAsyncEventArgs args, out IPEndPoint accRE, out IPEndPoint conRE, out IPEndPoint argRE)
		{
			accRE = null;
			conRE = null;
			argRE = null;
			var acceptSocket = args.AcceptSocket;
			var connectSocket = args.ConnectSocket;
			if (acceptSocket != null && acceptSocket.ProtocolType == ProtocolType.Tcp && acceptSocket.Connected)
			{
				accRE = (IPEndPoint)acceptSocket.RemoteEndPoint;
			}
			if (connectSocket != null && connectSocket.ProtocolType == ProtocolType.Tcp && connectSocket.Connected)
			{
				conRE = (IPEndPoint)connectSocket.RemoteEndPoint;
			}
			argRE = (IPEndPoint)args.RemoteEndPoint;
		}

		/// <summary>
		/// Get local end point (IP address and port number to which client is connected on local machine) from SocketAsyncEventArgs.
		/// </summary>
		/// <param name="args">SocketAsyncEventArgs from which local endpoint will be extracted.</param>
		/// <param name="remotePrefix">String prefix which shows part from which local endpoint was extracted.</param>
		/// <param name="remoteEndpoint">LocalEndPoint data.</param>
		public static void GetLocalEndpoint(SocketAsyncEventArgs args, out string localPrefix, out IPEndPoint localEndpoint)
		{
			var acceptSocket = args.AcceptSocket;
			var connectSocket = args.ConnectSocket;
			var token = (UserToken)args.UserToken;
			IPEndPoint endpoint = null;
			string prefix = "";
			if (endpoint is null && connectSocket != null) //  && connectSocket.Connected
			{
				endpoint = (IPEndPoint)connectSocket.LocalEndPoint;
				prefix = "connectSocket.";
			}
			if (endpoint is null && acceptSocket != null) // && acceptSocket.Connected
			{
				endpoint = (IPEndPoint)acceptSocket.LocalEndPoint;
				prefix = "acceptSocket.";
			}
			localEndpoint = endpoint;
			localPrefix = prefix;
		}

		/// <summary>
		/// Write information about data token into log file.
		/// </summary>
		void ShowToken(string operationName, int tokenId, string format, params object[] args)
		{
			var part1 = string.Format("Token = {0}, {1,-18}: ", tokenId, operationName);
			var part2 = args != null && args.Length > 0 ? string.Format(format, args) : format;
			LogWriter.WriteFlow(part1 + part2);
		}

		/// <summary>
		/// Show asynchronous operation endpoints and write them to log file.
		/// </summary>
		/// <param name="operationName">Socket operation name.</param>
		/// <param name="args">Socket operation arguments.</param>
		void ShowEndpoints(string operationName, SocketAsyncEventArgs args)
		{
			var token = (UserToken)args.UserToken;
			var acceptSocket = args.AcceptSocket;
			var socketError = args.SocketError;
			Exception ex = socketError == SocketError.Success ? null : new SocketException((int)socketError);
			// Get endpoints.
			var localEndpointString = "LocalEndpoint (disposing)";
			var remoteEndpointString = "RemoteEndpoint (disposing)";
			if (!IsDisposing)
			{
				try
				{
					var localEndpoint = (acceptSocket != null && socketError != SocketError.OperationAborted) ? (IPEndPoint)acceptSocket.LocalEndPoint : null;
					localEndpointString = localEndpoint is null ? "LocalEndpoint = null" : string.Format("LocalEndpoint = {0}", localEndpoint);
					GetOriginalRemoteEndpoint(args, out remoteEndpointString);
				}
				catch (Exception ex2)
				{
					ShowToken("ShowEndpoints", token.TokenId, "// Exception: {0}", ex2.Message);
				}
			}
			LogWriter.WriteThreads(operationName, token);
			var error = socketError == SocketError.Success ? "" : string.Format(", SocketError = {0}", socketError);
			var busyCount = BusyEventArgs.Count;
			ShowToken(operationName, token.TokenId, "{0}, {1}, ClientsInUse = {2}{3}", localEndpointString, remoteEndpointString, busyCount, error);
		}

		public void RaiseOnData(SocketServerEventType status, SocketError error, DataHolder holder)
		{
			var e = new SocketDataEventArgs(status, holder, error);
			// Take a local copy -- this is for thread safety.  If an unsubscribe on another thread
			// causes TestEvent to become null, this will protect you from a null reference exception.
			// (The event will be raised to all subscribers as of the point in time that this line executes.)
			var ev = OnData;
			if (ev != null) ev(this, e);
		}

		#endregion

		#region Process

		/// <summary>
		/// This method is called when an operation is completed on a socket
		/// </summary>
		/// <summary>
		/// This method is called when an operation is completed on a socket.
		/// </summary>
		protected void SocketAsyncEventArgs_Completed(object sender, SocketAsyncEventArgs e)
		{
			//if (IsDisposing) return;
			// Any code that you put in this method will NOT be called if
			// the operation completes synchronously, which will probably happen when
			// there is some kind of socket error.
			var token = (UserToken)e.UserToken;
			//LogWriter.WriteFlow("Token = {0}, SocketAsyncEventArgs_Completed: LastOperation = {1}, ", token.TokenId, e.LastOperation);
			switch (e.LastOperation)
			{
				case SocketAsyncOperation.Connect: CompleteConnect(e); break;
				case SocketAsyncOperation.Accept: CompleteAccept(e); break;
				case SocketAsyncOperation.Receive: CompleteReceive(e); break;
				case SocketAsyncOperation.ReceiveFrom: CompleteReceive(e); break;
				case SocketAsyncOperation.Send: CompleteSend(e); break;
				case SocketAsyncOperation.SendTo: CompleteSend(e); break;
				case SocketAsyncOperation.Disconnect: CompleteDisconnect(e); break;
				default:
					throw new ArgumentException("The last operation completed on the socket was not a connect, accept, receive, send or disconnect!");
			}
		}

		#endregion

		#region Process: Open/Close

		/// <summary>The socket used to listen for incoming connection requests.</summary>
		Socket tcpListenSocket;
		object tcpListenSocketLock = new object();
		static AutoResetEvent closeAutoEvent = new AutoResetEvent(false);
		public object openCloseLock = new object();
		bool MustListen;

		/// <summary>
		/// Synchronous action.
		/// TCP: Starts Accept operation.
		/// UDP: Starts Receive operations.
		/// </summary>
		public void Open()
		{
			lock (openCloseLock)
			{
				LogWriter.WriteFlow("{0,-29}: IsNetworkAvailable = {1}", "Open", IsNetworkAvailable);
				MustListen = true;
				RaiseOnData(SocketServerEventType.Opening, SocketError.Success, null);
				if (IsNetworkAvailable) OpenListener();
				// At this point:
				// TCP: socket is listening and accepting connections.
				// UDP: socket is receiving.
				RaiseOnData(SocketServerEventType.Opened, SocketError.Success, null);
				RestartQueueMonitorTimer();
			}
		}

		public void CloseConnections()
		{
			lock (openCloseLock)
			{
				MustListen = false;
				RaiseOnData(SocketServerEventType.Closing, SocketError.Success, null);
				CloseListener();
				RaiseOnData(SocketServerEventType.Closed, SocketError.Success, null);
			}
		}

		/// <summary>
		/// Start the socket server and begin listening for incoming connection requests.
		/// </summary>
		void CheckListener()
		{
			//lock (openCloseLock)
			//{
			//	if (MustListen)
			//	{
			//		// Check if listener is broken.
			//		bool doClose = false;
			//		if (IsTcp)
			//		{
			//			lock (tcpListenSocketLock)
			//			{
			//				doClose = (tcpListenSocket != null && !tcpListenSocket.IsBound);
			//			}
			//		}
			//		if (doClose)
			//		{
			//			CloseListener();
			//		}
			//		OpenListener();
			//	}
			//	else CloseListener();
			//}
		}

		/// <summary>
		/// Start the socket server and begin listening for incoming connection requests.
		/// </summary>
		void OpenListener()
		{
			if (IsTcp)
			{
				lock (tcpListenSocketLock)
				{
					if (tcpListenSocket is null)
					{
						// Create the socket which listens for incoming connections.
						tcpListenSocket = GetNewSocket("OpenListener: TCP", Settings.ServerPort, null);
						// Start the listener with a backlog of however many connections.
						// "backlog" means pending connections.
						// The backlog number is the number of clients that can wait for a
						// SocketAsyncEventArg object that will do an accept operation.
						// The listening socket keeps the backlog as a queue. The backlog allows
						// for a certain # of excess clients waiting to be connected.
						// If the backlog is full, then the client will receive an error when
						// trying to connect.
						// Note: Maximum number for backlog can be limited by the operating system.
						LogWriter.WriteFlow("{0,-29}: Start listen with MaxPendingConnections = {1}", "OpenListener", Settings.MaxPendingConnections);
						// Use this command to find who is listening on same port: netstat -aon | find /i "18701"
						tcpListenSocket.Listen(Settings.MaxPendingConnections);
					}
				}
			}
		}

		/// <summary>
		/// Stop the socket server. Synchronous action.
		/// TCP: terminates all socket operations and stops listening.
		/// UDP: terminates all socket operations.
		/// </summary>
		/// <returns>Will raise events.</returns>
		void CloseListener()
		{
			if (IsTcp)
			{
				lock (tcpListenSocketLock)
				{
					if (tcpListenSocket != null)
					{
						tcpListenSocket.Dispose();
						tcpListenSocket = null;
					}
				}
				new System.Threading.ManualResetEvent(false).WaitOne(50);
			}
			lock (SendQueueLock)
			{
				foreach (var holder in SendQueue)
				{
					// Mark all items as available.
					if (!holder.Available)
					{
						holder.Exception = new Exception("Socket Closed");
						holder.Available = true;
					}
				}
			}
			SocketAsyncEventArgs[] items;
			// Only blocks if write lock held.
			lock (EventArgsSemaphoreLock)
			{
				// Make sure that nothing is pulled/popped from the Event Args pool.
				// if MustListen = false;
				items = BusyEventArgs.ToArray();
				EventArgsSemaphore = new SemaphoreSlim(1);
			}
			LogWriter.WriteFlow("{0,-18}: Items to close = {1}", "CloseListener", items.Count());
			if (items.Length > 0)
			{
				// Requires lock until all items returns to the pool.
				for (int i = 0; i < items.Length; i++)
				{
					var args = items[i];
					CloseAcceptSocket(args, "CloseListener");
				}
				// Wait here until all items returns to the pool.
				EventArgsSemaphore.Wait();
				LogWriter.WriteFlow("{0,-18}: All items were closed.", "CloseListener");
			}
		}


		#endregion

		#region Processes: In/Out

		int WaitCount;

		/// <summary>
		/// Will start accept (TCP) or receive (UDP) operation if necessary.
		/// </summary>
		void UpdateWaitCount(bool reduceWaitCount = false)
		{
			lock (EventArgsLock)
			{
				if (reduceWaitCount) WaitCount--;
				if (IsDisposing || !MustListen || !IsNetworkAvailable)
				{
					LogWriter.WriteFlow("UpdateWaitCount: IsDisposing = {0}, MustListen = {1}, IsNetworkAvailable = {2}",
						IsDisposing, MustListen, IsNetworkAvailable);
					return;
				}
				CheckIdleSockets();
				CheckWaitingSockets();
				CheckSendQueue();
			}
		}

		/// <summary>
		/// This function must run inside SendQueueLock.
		/// It will exit if there are no messages to send or free sockets available.
		/// </summary>
		void CheckSendQueue()
		{
			while (true)
			{
				if (!AllowToProcess) return;
				DataHolder holder = null;
				// Make sure that send queue is locked, before modifying any value inside collection.
				lock (SendQueueLock)
				{
					if (SingleMessageSendMode)
					{
						// Send ACK messages first.
						holder = SendQueue.FirstOrDefault(x => x.Available && x.MessageToSend.IsAck());
						// if no ACK messages in the queue then...
						if (holder is null)
						{
							// Get first item in the queue.
							var item = SendQueue.FirstOrDefault();
							// If message is in the queue and available then...
							if (item != null && item.Available) holder = item;
						}
					}
					else
					{
						// Get first available data holder.
						holder = SendQueue.FirstOrDefault(x => x.Available);
					}
					// If queue is empty then...
					if (holder is null)
					{
						// No messages to send. Exit function.
						return;
					}
				}
				// Get a SocketAsyncEventArgs object to connect with.
				// Get it from the pool if there is more than one.
				SocketAsyncEventArgs args = null;
				bool queryResult = false;
				if (IsTcp && holder.DeliveryRemoteEndpoint != null)
				{
					// Prefer sockets connected to the same remote host.
					args = FreeEventArgs.FirstOrDefault(x =>
						x.AcceptSocket != null &&
						x.AcceptSocket.Connected &&
						holder.DeliveryRemoteEndpoint.Equals(x.AcceptSocket.RemoteEndPoint
					));
					if (args != null) queryResult = true;
				}
				// If item was not found then take any item.
				if (args is null)
				{
					args = FreeEventArgs.FirstOrDefault();
				}
				// If argument was not found then...
				if (args is null)
				{
					// No available Sockets. Exit function.
					return;
				}
				else
				{
					// Take item from free list and move to busy list.
					MoveToItem(args, true, "CheckSendQueue");
					// Allow to reuse sockets.
					args.DisconnectReuseSocket = true;
					// If disposing started then return;
					var token = (UserToken)args.UserToken;
					// Show information about socket arguments endpoints.
					int availableMessages;
					int totalMesages;
					// Use lock on 'SendQueue' to prevent Exception during count:
					// Collection was modified; enumeration operation may not execute.
					lock (SendQueueLock)
					{
						availableMessages = SendQueue.Count(x => x.Available);
						totalMesages = SendQueue.Count();
					}
					var extraMessage = string.Format("SendQueue Available = {0}/{1}", availableMessages, totalMesages);
					extraMessage += IsTcp ? string.Format(", UseConnectedSocket = {0}", queryResult) : "";
					ShowToken("CheckMessageQueue", token.TokenId, extraMessage);
					// Mark it as non available.
					holder.Available = false;
					// Assign message holder.
					token.DataHolder = holder;
					//SocketAsyncEventArgs object that do connect operations on the client
					//are different from those that do accept operations on the server.
					//On the server the listen socket had EndPoint info. And that info was
					//passed from the listen socket to the SocketAsyncEventArgs object
					//that did the accept operation.
					//But on the client there is no listen socket. The connect socket
					//needs the info on the Remote Endpoint.
					args.RemoteEndPoint = token.DataHolder.DeliveryRemoteEndpoint;
					// If socket is missing or this is TCP connection then...
					if (args.AcceptSocket is null)
					{
						// Create new socket.
						var socket = GetNewSocket("StartOutOperation", token.ClientPort, args);
						// Make sure that accept operation is handled on same socket.
						args.AcceptSocket = socket;
					}
					// If this is UDP or socket is connected already to required host then...
					if (!IsTcp || queryResult)
					{
						// Go directly to sending.
						StartSend(args);
					}
					else
					{
						StartConnect(args);
					}
				}
			}
		}

		void MoveToItem(SocketAsyncEventArgs args, bool moveToBusy, string operationName)
		{
			var token = (UserToken)args.UserToken;
			if (moveToBusy)
			{
				ShowToken(operationName, token.TokenId, "Move 1 socket from FreeEventArgs[{0}] to BusyEventArgs[{1}]", FreeEventArgs.Count, BusyEventArgs.Count);
				FreeEventArgs.Remove(args);
				token.SessionId = CreateSessionId();
				BusyEventArgs.Add(args);
				State.MaximumItemsUsed = Math.Max(State.MaximumItemsUsed, BusyEventArgs.Count);
			}
			else
			{
				ShowToken(operationName, token.TokenId, "Move 1 socket from BusyEventArgs[{0}] to FreeEventArgs[{1}]", BusyEventArgs.Count, FreeEventArgs.Count);
				BusyEventArgs.Remove(args);
				token.SessionId = 0;
				FreeEventArgs.Add(args);
			}
		}

		/// <summary>
		/// This function must run inside EventArgsLock.
		/// </summary>
		void CheckWaitingSockets()
		{
			// Get amount of required receive/accept operations.
			var requiredWaitOperations = Math.Max(Settings.MaxPendingConnections - WaitCount, 0);
			var msg = string.Format("UpdateWaitCount: requiredWaitOperations {0}/{1}",
					requiredWaitOperations, Settings.MaxPendingConnections
				);
			// Note: Maximum number for backlog can be limited by the operating system.
			for (int i = 0; i < requiredWaitOperations; i++)
			{
				// Get a SocketAsyncEventArgs object for accept/receive operation.
				// Get it from the pool if there is more than one in the pool.
				// Prefer disconnected sockets.
				bool queryResult = false;
				SocketAsyncEventArgs args = null;
				if (IsTcp)
				{
					// Prefer unconnected sockets.
					args = FreeEventArgs.FirstOrDefault(x => x.AcceptSocket is null || !x.AcceptSocket.Connected);
					if (args != null) queryResult = true;
				}
				// Get arguments item from the pool.
				// If item was not found then take any item.
				if (args is null)
				{
					args = FreeEventArgs.FirstOrDefault();
				}
				// If available args item was found then...
				if (args != null)
				{
					var token = (UserToken)args.UserToken;
					// Create new data holder for new receive operation.
					token.DataHolder = new DataHolder();
					// Allow to reuse sockets on server side.
					args.DisconnectReuseSocket = true;
					if (!IsTcp && args.AcceptSocket is null)
					{
						// ShowToken will be called inside.
						args.AcceptSocket = GetNewSocket("StartInOperation", Settings.ServerPort, args);
					}
					else
					{
						ShowToken("StartInOperation", token.TokenId, "UseDisconnectedSocket = {0}", queryResult);
					}
					// Move token to busy list.
					MoveToItem(args, true, "CheckWaitingSockets");
					// Increase wait count.
					WaitCount++;
					if (IsTcp)
					{
						// Start accept will create new AcceptSocket.
						StartAccept(args);
					}
					else
					{
						// UDP can go directly to receiving.
						StartReceive(args);
					}
				}
			}
		}

		#endregion

		#region Process: Connect (Client, TCP)

		/// <summary>
		/// Used on SocketClient: Connect to remote the host.
		/// </summary>
		/// <param name="args">Connect EventArgs</param>
		/// <remarks>This method used by TCP only</remarks>
		protected void StartConnect(SocketAsyncEventArgs args)
		{
			// If socket is disposing then don't start new operation.
			if (!AllowToProcess)
			{
				if (IsTcp) StartDisconnect(args);
				else PushArgsToPool(args);
				return;
			}
			var token = (UserToken)args.UserToken;
			// Make sure that connect operation won't use send/receive buffer.
			args.SetBuffer(null, 0, 0);
			// Post the connect operation on the socket.
			// A local port is assigned by the Windows OS during connect op.
			token.LastOperationDate = DateTime.Now;
			RaiseOnData(SocketServerEventType.Connecting, SocketError.Success, token.DataHolder);
			ShowEndpoints("StartConnect", args);
			State.Increment(SocketAsyncOperation.Connect);
			bool willRaiseEvent = args.AcceptSocket.ConnectAsync(args);
			// If operation completed synchronously and the SocketAsyncEventArgs.Completed event
			// on the e parameter won't be raised then process results immediately.
			if (!willRaiseEvent) CompleteConnect(args);
		}

		/// <summary>
		/// Pass the connection info from the connecting object to the object
		/// that will do send/receive. And put the connecting object back in the pool.
		/// </summary>
		/// <param name="args">Connect EventArgs</param>
		/// <remarks>This method used by TCP only</remarks>
		protected void CompleteConnect(SocketAsyncEventArgs args)
		{
			State.Increment(args.LastOperation, args.SocketError);
			var token = (UserToken)args.UserToken;
			var holder = token.DataHolder;
			var socketError = args.SocketError;
			ShowEndpoints("CompleteConnect", args);
			RaiseOnData(SocketServerEventType.Connected, socketError, holder);
			if (socketError != SocketError.Success)
			{
				StartDisconnect(args);
				return;
			}
			StartSend(args);
		}

		#endregion

		#region Process: Accept (Server, TCP)

		/// <summary>
		/// Begins an operation to accept a connection request from the client.
		/// </summary>
		internal void StartAccept(SocketAsyncEventArgs args)
		{
			var token = (UserToken)args.UserToken;
			ShowEndpoints("StartAccept", args);
			// Socket.AcceptAsync begins asynchronous operation to accept the connection.
			// Note the listening socket will pass info to the SocketAsyncEventArgs
			// object that has the Socket that does the accept operation.
			// If you do not create a Socket object and put it in the SAEA object
			// before calling AcceptAsync and use the AcceptSocket property to get it,
			// then a new Socket object will be created for you by .NET.
			// exit if disposed
			// Make sure that accept operation won't use send/receive buffer.
			args.SetBuffer(null, 0, 0);
			// TCP Accept completion will create new AcceptSocket.
			args.AcceptSocket = null;
			token.LastOperationDate = DateTime.Now;
			RaiseOnData(SocketServerEventType.Accepting, SocketError.Success, token.DataHolder);
			State.Increment(SocketAsyncOperation.Accept);
			bool willRaiseEvent = tcpListenSocket.AcceptAsync(args);
			// Socket.AcceptAsync returns true if the I/O operation is pending, i.e. is
			// working asynchronously. The
			// SocketAsyncEventArgs.Completed event on the acceptEventArg parameter
			// will be raised upon completion of accept op.
			// AcceptAsync will call the AcceptEventArg_Completed
			// method when it completes, because when we created this SocketAsyncEventArgs
			// object before putting it in the pool, we set the event handler to do it.
			// AcceptAsync returns false if the I/O operation completed synchronously.
			// The SocketAsyncEventArgs.Completed event on the acceptEventArg
			// parameter will NOT be raised when AcceptAsync returns false.
			if (!willRaiseEvent) CompleteAccept(args);
		}

		/// <summary>
		/// The e parameter passed from the AcceptEventArg_Completed method
		/// represents the SocketAsyncEventArgs object that did
		/// the accept operation. in this method we'll do the hand-off from it to the
		/// SocketAsyncEventArgs object that will do receive/send.
		/// </summary>
		/// <param name="args"></param>
		/// <remarks>This method used by TCP only and fired by listenSocket only</remarks>
		protected void CompleteAccept(SocketAsyncEventArgs args)
		{
			State.Increment(args.LastOperation, args.SocketError);
			var token = (UserToken)args.UserToken;
			var holder = token.DataHolder;
			var socketError = args.SocketError;
			ShowEndpoints("CompleteAccept", args);
			RaiseOnData(SocketServerEventType.Accepted, socketError, holder);
			// Check if new accept (TCP) operation must be started.
			UpdateWaitCount(true);
			if (AllowToProcess && socketError == SocketError.Success)
			{
				StartReceive(args);
			}
			else
			{
				// This is when there was an error with the accept op. That should NOT
				// be happening often. It could indicate that there is a problem with
				// that socket. If there is a problem, then we would have an infinite
				// loop here, if we tried to reuse that same socket.
				StartDisconnect(args);
			}
		}

		#endregion

		#region Process: Receive

		/// <summary>
		///  Set the receive buffer and post a receive op.
		/// </summary>
		/// <param name="args"></param>
		protected void StartReceive(SocketAsyncEventArgs args, bool appendData = false)
		{
			// If socket is disposing then don't start new operation.
			if (!AllowToProcess)
			{
				if (IsTcp) StartDisconnect(args);
				else PushArgsToPool(args);
				return;
			}
			bool? willRaiseEvent = null;
			var token = (UserToken)args.UserToken;
			lock (token)
			{
				token.BufferOffsetCurrent = token.BufferOffset;
				if (!appendData)
				{
					token.PrepareNextMessageToReceive();
				}
				// Set the buffer for the receive operation.
				args.SetBuffer(sendReceiveBuffer, token.BufferOffset, Settings.BufferSize);
				// Post asynchronous receive operation on the socket.
				token.LastOperationDate = DateTime.Now;
				var acceptSocket = args.AcceptSocket;
				if (IsTcp)
				{
					acceptSocket.ReceiveTimeout = Settings.ReceiveTimeout;
				}
				else
				{
					// Creates an IpEndPoint to capture the identity of the sending host.
					IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
					args.RemoteEndPoint = sender;
				}
				// Raise event before writing log, because writing log could delay event and log it in wrong order.
				RaiseOnData(SocketServerEventType.Receiving, SocketError.Success, token.DataHolder);
				ShowEndpoints("StartReceive", args);
				try
				{
					if (IsTcp)
					{
						State.Increment(SocketAsyncOperation.Receive);
						willRaiseEvent = acceptSocket.ReceiveAsync(args);
					}
					else
					{
						State.Increment(SocketAsyncOperation.ReceiveFrom);
						willRaiseEvent = acceptSocket.ReceiveFromAsync(args);
					}
				}
				catch (Exception ex)
				{
					ShowToken("StartReceive", token.TokenId, "Error: {0}", ex.Message);
				}
			}
			// If exception happened then disconnect.
			if (!willRaiseEvent.HasValue)
			{
				if (IsTcp) StartDisconnect(args);
				else PushArgsToPool(args);
			}
			// If operation completed synchronously and the SocketAsyncEventArgs.Completed event
			// on the e parameter won't be raised then process results immediately.
			else if (!willRaiseEvent.Value) CompleteReceive(args);
		}

		// This method is invoked by the IO_Completed method
		// when an asynchronous receive operation completes.
		// If the remote host closed the connection, then the socket is closed.
		// Otherwise, we process the received data. And if a complete message was
		// received, then we do some additional processing, to
		// respond to the client.
		protected void CompleteReceive(SocketAsyncEventArgs args)
		{
			State.Increment(args.LastOperation, args.SocketError);
			// Store value now because data holder will be set to null during disconnect.
			var token = (UserToken)args.UserToken;
			var holder = token.DataHolder;
			var socketError = args.SocketError;
			// Identify remote endpoint and set it onto holder.
			string remoteEndpointString;
			IPEndPoint oriRE = GetOriginalRemoteEndpoint(args, out remoteEndpointString);
			holder.OriginalRemoteEndpoint = oriRE;
			var extraMessage = socketError == SocketError.Success
				? string.Format("BytesTransferred = {0}", args.BytesTransferred)
				: string.Format("SocketError = {0}", socketError);
			ShowToken("CompleteReceive", token.TokenId, "{0}, {1}", extraMessage, remoteEndpointString);
			// If there was a socket error, close the connection. This is NOT a normal
			// situation, if you get an error here.
			// In the Microsoft example code they had this error situation handled
			// at the end of CompleteReceive. Putting it here improves readability
			// by reducing nesting some.
			if (socketError != SocketError.Success)
			{
				// If UDP then.
				RaiseOnData(SocketServerEventType.Received, socketError, holder);
				if (IsTcp)
				{
					StartDisconnect(args);
				}
				else
				{
					PushArgsToPool(args, false, true);
				}
				return;
			}
			if (IsDisposing) return;
			// The BytesTransferred property tells us how many bytes we need to process.
			var bytesTransfered = args.BytesTransferred;
			token.ReceivedBytesToProcess = bytesTransfered;
			var isClient = args.ConnectSocket != null;
			// If no data was received, close the connection. This is a NORMAL
			// situation that shows when the client has finished sending data.
			if (bytesTransfered == 0)
			{
				// If disconnection was initialized from client.
				ShowToken("CompleteReceive", token.TokenId, "CompleteReceive: NO DATA - remote host has finished sending data.");
				// Complete disconnection.
				if (IsTcp)
				{
					if (isClient)
					{
						// There will be no more new messages from the server.
						ShowToken("CompleteReceive", token.TokenId, "SocketShutdown.Receive");
						args.AcceptSocket.Shutdown(SocketShutdown.Receive);
					}
					else
					{
						// Inform the client that there will be no more messages.
						ShowToken("CompleteReceive", token.TokenId, "SocketShutdown.Both");
						args.AcceptSocket.Shutdown(SocketShutdown.Both);
					}
					StartDisconnect(args);
				}
				else
				{
					// No data, in UDP case, probably means that sending of previous message failed instantly.
				}
				return;
			}
			var isHeadReady = false;
			var isBodyReady = false;
			var messagesToSend = new List<ISocketMessage>();
			// Loop trough messages until there is nothing to process.
			while (token.ReceivedBytesToProcess > 0)
			{
				isHeadReady = HandleMessageHead(args);
				isBodyReady = false;
				// If we have not got all of the head already then...
				if (isHeadReady)
				{
					// If we have processed the head, we can work on the message now.
					// We'll arrive here when we have received enough bytes to read
					// the first byte after the head.
					isBodyReady = token.HeadProcessError is null
						? HandleMessageBody(args)
						: true;
					if (isBodyReady)
					{
						// for testing only
						if (Settings.DelayAfterGettingMessage > 0)
						{
							//A Thread.Sleep here can be used to simulate delaying the
							//return of the SocketAsyncEventArgs object for receive/send
							//to the pool. Simulates doing some work here.
							new System.Threading.ManualResetEvent(false).WaitOne(Settings.DelayAfterGettingMessage);
						}
						// Pass the DataHolder object to the Mediator here.
						var receivedTransMissionId = GetReceivedTransmitionId();
						holder.ReceivedMessageBytes = token.MessageBytes;
						// Make sure that token message bytes is not modified.
						token.MessageBytes = null;
						holder.ReceivedDate = DateTime.Now;
						// If no errors when processing header then...
						if (token.HeadProcessError is null)
						{
							RaiseOnData(SocketServerEventType.Received, SocketError.Success, holder);
							// Process message.
							var m = ProcessMessage;
							// probably is disposing.
							if (m is null) return;
							var er = ErrorReceived;
							if (er is null)
							{
								m(this, args);
							}
							else
							{
								try
								{
									m(this, args);
								}
								catch (Exception methodEx)
								{
									methodEx.Data.Add("ErrorReceived", "RemoveMessage");
									var e = new EventArgs<Exception>(methodEx);
									er(this, e);
								}
							}
						}
						var extraMessage2 = "";
						ISocketMessage request = holder.ReceivedMessage;
						if (request != null)
						{
							extraMessage2 += string.Format("MessageType = {0}", request.GetMessageType());
							if (request.IsAck())
							{
								State.IncrementAckReceive();
							}
						}
						ISocketMessage response = holder.MessageToSend;
						if (response != null)
						{
							extraMessage2 += string.Format(", 1 MessageToSend: {0}", response.GetMessageType());
							if (!IsTcp || !IsCompatibilityMode)
							{
								messagesToSend.Add(response);
								holder.MessageToSend = null;
							}
						}
						if (holder.ExtraResponses.Count > 0)
						{
							if (!IsTcp || !IsCompatibilityMode)
							{
								var messageTypes = string.Join(", ", holder.ExtraResponses.Select(x => x.GetMessageType()));
								extraMessage2 += string.Format(" + {0} ExtraResponses: {1}", holder.ExtraResponses.Count, messageTypes);
								messagesToSend.AddRange(holder.ExtraResponses);
								holder.ExtraResponses.Clear();
							}
						}
						ShowToken("CompleteReceive", token.TokenId, "{0}", extraMessage2);
					}
				}
			}
			// If message is unfinished then...
			if (token.ReceivedBytesToProcess > 0)
			{
				// Do another receive operation.
				StartReceive(args, true);
			}
			else if (IsTcp && isClient && token.DataHolder.MessageToSend is null)
			{
				// Inform other side that no messages will be sent.
				ShowToken("CompleteReceive", token.TokenId, "SocketShutdown.Send (MessageToSend is null)");
				args.AcceptSocket.Shutdown(SocketShutdown.Send);
				// TCP: Wait for SocketShutdown message.
				StartReceive(args);
			}
			else if (IsTcp)
			{
				if (IsCompatibilityMode && token.DataHolder.MessageToSend != null)
				{
					// Don't send extra responses on separate sockets.
					StartSend(args);
				}
				else
				{
					PushArgsToPool(args);
					UpdateWaitCount();
				}
			}
			else
			{
				// UDP always goes to receive.
				StartReceive(args);
			}
			for (int i = 0; i < messagesToSend.Count; i++)
			{
				AddMessageToSend(messagesToSend[i], oriRE);
			}
		}

		/// <summary>Send extra responses on same socket.</summary>
		public bool IsCompatibilityMode = false;

		#endregion

		#region Process: Send

		//object ChecksumsLock = new object();
		//List<Guid> Checksums = new List<Guid>();

		//bool UpsertCheckSum(byte[] bytes)
		//{
		//	lock (ChecksumsLock)
		//	{
		//		var md5 = Security.Encryption.Current.ComputeMd5Hash(bytes);
		//		if (Checksums.Contains(md5))
		//		{
		//			return false;
		//		}
		//		else
		//		{
		//			Checksums.Add(md5);
		//			return true;
		//		}
		//	}
		//}


		/// <summary>
		/// Set the send buffer and post a send op.
		/// </summary>
		/// <param name="args">Connect EventArgs</param>
		protected void StartSend(SocketAsyncEventArgs args, bool resumeSending = false)
		{
			// If socket is disposing then don't start new operation.
			if (!AllowToProcess)
			{
				if (IsTcp) StartDisconnect(args);
				else PushArgsToPool(args);
				return;
			}
			bool? willRaiseEvent = null;
			var token = (UserToken)args.UserToken;
			lock (token)
			{
				if (!resumeSending)
				{
					// Earlier, in the UserToken of connectEventArgs we put an array
					// of messages to send. Now we move that array to the DataHolder in
					// the UserToken of receiveSendEventArgs.
					// Pass data holder to send/receive user token.
					token.PrepareMessageToSend();
				}
				// Set the buffer. You can see on Microsoft's page at
				// http://msdn.microsoft.com/en-us/library/system.net.sockets.socketasynceventargs.setbuffer.aspx
				// that there are two overloads. One of the overloads has 3 parameters.
				// When setting the buffer, you need 3 parameters the first time you set it,
				// which we did in the Initialize() method. The first of the three parameters
				// tells what byte array to use as the buffer. After we tell what byte array
				// to use we do not need to use the overload with 3 parameters any more.
				// (That is the whole reason for using the buffer block. You keep the same
				// byte array as buffer always, and keep it all in one block.)
				// Now we use the overload with two parameters. We tell
				//  (1) the offset and
				//  (2) the number of bytes to use, starting at the offset.
				//
				// The number of bytes to send depends on whether the message is larger than
				// the buffer or not. If it is larger than the buffer, then we will have
				// to post more than one send operation. If it is less than or equal to the
				// size of the send buffer, then we can accomplish it in one send op.
				//
				//We cannot try to set the buffer any larger than its size.
				//So since receiveSendToken.sendBytesRemaining > its size, we just
				//set it to the maximum size, to send the most data possible.
				var bytesToSend = Math.Min(token.MessageBytesToProcess, Settings.BufferSize);
				args.SetBuffer(sendReceiveBuffer, token.BufferOffset, bytesToSend);
				if (bytesToSend > 0)
				{
					// Copy the bytes to the buffer associated with this SAEA object.
					Buffer.BlockCopy(token.MessageToSendBytes, token.MessageBytesProcessed, args.Buffer, token.BufferOffset, bytesToSend);
				}
				IPEndPoint delRE = token.DataHolder.DeliveryRemoteEndpoint;
				IPEndPoint oriRE = token.DataHolder.OriginalRemoteEndpoint;
				// If delivery endpoint specified then...
				if (delRE != null)
				{
					// Use delivery endpoint.
					args.RemoteEndPoint = delRE;
				}
				// If original endpoint is known then...
				else if (oriRE != null)
				{
					// Use original endpoint
					args.RemoteEndPoint = oriRE;
				}
				var argRE = (IPEndPoint)args.RemoteEndPoint;
				var argReString = argRE is null ? "ArgRE is not set!" : string.Format("ArgRE = {0}", argRE);
				var hex = (Settings.LogData) ? ":\r\n" + LogWriter.GetHexBlock(args.Buffer, token.BufferOffset, bytesToSend) : "";
				ShowToken("StartSend", token.TokenId, "MessageType = {0}, {1}, Buffer[{2}]{3}",
					token.DataHolder.MessageToSend.GetMessageType(), argReString, bytesToSend, hex);
				// We'll change the value of sendUserToken.sendBytesRemaining in the CompleteSend method.
				//post the send
				token.LastOperationDate = DateTime.Now;
				var acceptSocket = args.AcceptSocket;
				if (IsTcp)
				{
					acceptSocket.SendTimeout = Settings.SendTimeout;
				}
				RaiseOnData(SocketServerEventType.Sending, SocketError.Success, token.DataHolder);
				try
				{
					if (IsTcp)
					{
						State.Increment(SocketAsyncOperation.Send);
						willRaiseEvent = acceptSocket.SendAsync(args);
					}
					else
					{
						State.Increment(SocketAsyncOperation.SendTo);
						willRaiseEvent = acceptSocket.SendToAsync(args);
					}
					ResetKeepAliveTimer();
				}
				catch (Exception ex)
				{
					ShowToken("StartSend", token.TokenId, "Error: {0}", ex.Message);
				}
			}
			// If exception happened then disconnect.
			if (!willRaiseEvent.HasValue)
			{
				if (IsTcp) StartDisconnect(args);
				else PushArgsToPool(args);
			}
			// If operation completed synchronously and the SocketAsyncEventArgs.Completed event
			// on the e parameter won't be raised then process results immediately.
			else if (!willRaiseEvent.Value) CompleteSend(args);
		}

		// This method is called by I/O Completed() when an asynchronous send completes.
		// If all of the data has been sent, then this method calls StartReceive
		// to start another receive op on the socket to read any additional
		// data sent from the client. If all of the data has NOT been sent, then it
		// calls StartSend to send more data.
		protected void CompleteSend(SocketAsyncEventArgs args)
		{
			State.Increment(args.LastOperation, args.SocketError);
			var token = (UserToken)args.UserToken;
			var holder = token.DataHolder;
			var socketError = args.SocketError;
			// Raise event before writing log, because writing log could delay event and log it in wrong order.
			RaiseOnData(SocketServerEventType.Sent, socketError, holder);
			LogWriter.WriteThreads("CompleteSend", token);
			if (!AllowToProcess || socketError != SocketError.Success)
			{
				// We'll just close the socket if there was a
				// socket error when receiving data from the client.
				ShowToken("CompleteSend", token.TokenId, "SocketError = {0}, AllowToProcess = {1}", args.SocketError, AllowToProcess);
				if (IsTcp) StartDisconnect(args);
				else PushArgsToPool(args);
				return;
			}
			// Get amount of transferred bytes if operation was success.
			var bytesTransferred = args.BytesTransferred;
			token.MessageBytesToProcess -= bytesTransferred;
			token.MessageBytesProcessed += bytesTransferred;
			var sentInOneOperation = token.MessageToSendBytes.Length == bytesTransferred && token.MessageBytesToProcess == 0;
			var extraMessage = sentInOneOperation
				? ""
				: string.Format("MessageType = {0}, BytesTransferred = {1}, MessageBytesToProcess = {2}",
				token.DataHolder.MessageToSend.GetMessageType(), bytesTransferred, token.MessageBytesToProcess);
			ShowToken("CompleteSend", token.TokenId, extraMessage);
			var isAck = token.DataHolder.MessageToSend.IsAck();
			var isClient = args.ConnectSocket != null;
			// If there are more unsent message bytes then continue to send.
			if (token.MessageBytesToProcess > 0)
			{
				StartSend(args, true);
			}
			// If there are no more bytes to send but this is TCP compatibility mode with more messages then...
			else if (IsTcp && IsCompatibilityMode && token.DataHolder.ExtraResponses.Count > 0)
			{
				var item = token.DataHolder.ExtraResponses.First();
				token.DataHolder.ExtraResponses.Remove(item);
				token.DataHolder.MessageToSend = item;
				StartSend(args);
			}
			// It this is TCP client which got ACK reply message (from server) then...
			else if (IsTcp && isClient && isAck)
			{
				// Begin connection shut-down procedure.
				ShowToken("CompleteSend", token.TokenId, "SocketShutdown.Send");
				args.ConnectSocket.Shutdown(SocketShutdown.Send);
				// Wait for an answer or shut-down message.
				StartReceive(args);
			}
			else if (IsTcp)
			{
				if (IsCompatibilityMode && token.DataHolder.ExtraResponses.Count > 0)
				{
					// Wait for shut-down message.
					var item = token.DataHolder.ExtraResponses.First();
					token.DataHolder.ExtraResponses.Remove(item);
					token.DataHolder.MessageToSend = item;
					StartSend(args, true);
				}
				else
				{
					// Wait for an answer or shut-down message.
					StartReceive(args);
				}
			}
			else
			{
				// UDP messages will be sent on another socket.
				PushArgsToPool(args);
			}
			UpdateWaitCount();
		}

		#endregion

		#region Process: Disconnect (TCP)

		/// <summary>
		/// Does the normal destroying of sockets after
		/// we finish receiving and sending on a connection.
		/// </summary>
		protected void StartDisconnect(SocketAsyncEventArgs args)
		{
			// Don't return on disposing, because sockets needs to be disposed.
			//if (IsDisposing) return;
			var token = (UserToken)args.UserToken;
			var socketError = args.SocketError;
			RaiseOnData(SocketServerEventType.Disconnecting, SocketError.Success, token.DataHolder);
			var aSocket = args.AcceptSocket;
			var cSocket = args.ConnectSocket;
			var aConnected = aSocket != null && aSocket.Connected;
			var cConnected = cSocket != null && cSocket.Connected;
			var isClient = args.ConnectSocket != null;
			var extraMessage = string.Format("[AcceptSocket: {0}]{1}[ConnectSocket: {2}]",
					aSocket is null ? "null" : aSocket.Connected ? "Connected" : "Disconnected",
					aSocket == cSocket ? " = " : ", ",
					cSocket is null ? "null" : cSocket.Connected ? "Connected" : "Disconnected"
				);
			// Connect socket must be same as accept socket.
			if (aSocket != null && cSocket != null && aSocket != cSocket)
			{
				extraMessage += ", Error: AcceptSocket != ConnectSocket";
			}
			// If socket is client (made connection first) then...
			if (isClient)
			{
				ShowToken("StartDisconnect", token.TokenId, extraMessage);
				token.LastOperationDate = DateTime.Now;
				State.Increment(SocketAsyncOperation.Disconnect);
				bool willRaiseEvent = cSocket.DisconnectAsync(args);
				// If operation completed synchronously and the SocketAsyncEventArgs.Completed event
				// on the e parameter won't be raised then process results immediately.
				if (!willRaiseEvent) CompleteDisconnect(args);
			}
			else if (aConnected)
			{
				ShowToken("StartDisconnect", token.TokenId, "Client must disconnect first!");
			}
			else
			{
				ShowToken("StartDisconnect", token.TokenId, extraMessage);
				PushArgsToPool(args);
			}
			//var noConnection = args.AcceptSocket is null ||
			//	socketError == SocketError.OperationAborted ||
			//	socketError == SocketError.ConnectionReset;
			//If there is no connection then...
		}

		/// <summary>
		/// Dispose AcceptSocket and return back to EventArgsPool.
		/// </summary>
		/// <param name="args"></param>
		/// <param name="raiseEvent"></param>
		protected void CompleteDisconnect(SocketAsyncEventArgs args)
		{
			State.Increment(args.LastOperation, args.SocketError);
			// Don't return on disposing, because sockets needs to be disposed.
			//if (IsDisposing) return;
			var token = (UserToken)args.UserToken;
			var holder = token.DataHolder;
			var socketError = args.SocketError;
			var acceptSocket = args.AcceptSocket;
			// Raise event before writing log, because writing log could delay event and log it in wrong order.
			RaiseOnData(SocketServerEventType.Disconnected, socketError, holder);
			var error = socketError == SocketError.Success ? "" : string.Format(", SocketError = {0}", socketError);
			//CloseAcceptSocket(args, "CompleteDisconnect");
			ShowToken("CompleteDisconnect", token.TokenId, error);
			PushArgsToPool(args, socketError != SocketError.Success);
		}

		SemaphoreSlim EventArgsSemaphore;
		object EventArgsSemaphoreLock = new object();

		void PushArgsToPool(SocketAsyncEventArgs args, bool returnWithError = false, bool reduceWaitCount = false)
		{
			lock (EventArgsLock)
			{
				var token = (UserToken)args.UserToken;
				var holder = token.DataHolder;
				if (holder != null && Settings.AutoRemoveFromSendQueue)
				{
					RemoveMessage(holder);
				}
				// Make sure holder is removed.
				token.Cleanup();
				// Dispose UDP socket or it will stay open in the memory.
				args.AcceptSocket.Dispose();
				args.AcceptSocket = null;
				// Push token back into the pool.
				MoveToItem(args, false, "PushArgsToPool");
				State.IncrementReturned(returnWithError);
				// If this was last item returned to the pool (no more items in use) and waiting is required then...
				if (BusyEventArgs.Count == 0 && EventArgsSemaphore != null)
				{
					// Unlock EventArgsSemaphore.Wait() line.
					EventArgsSemaphore.Release(1);
				}
			}
			UpdateWaitCount(reduceWaitCount);
		}

		void CloseAcceptSocket(SocketAsyncEventArgs args, string funtion)
		{
			var token = (UserToken)args.UserToken;
			// Make sure token is not closing when StartReceiving or StartSending action is performed.
			lock (token)
			{
				var acceptSocket = args.AcceptSocket;
				if (acceptSocket is null)
				{
					ShowToken(funtion, token.TokenId, "close = args.AcceptSocket is null");
				}
				else
				{
					// Close timed out socket.
					try
					{
						// If socket was not closed yet then...
						if (acceptSocket.Connected)
						{
							// Calling Close internally calls Dispose.
							acceptSocket.Close();
						}
						else
						{
							acceptSocket.Dispose();
						}
						// It is safe to clean accept socket, because it is not used.
						//args.AcceptSocket = null;
						ShowToken(funtion, token.TokenId, "Close = Success");
					}
					catch (Exception ex)
					{
						ShowToken(funtion, token.TokenId, "Close = Error: {1}", ex.Message);
					}
				}
			}
		}

		#endregion

		#region IDisposable

		public bool IsDisposing;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		// The bulk of the clean-up code is implemented in Dispose(bool)
		void Dispose(bool disposing)
		{
			LogWriter.WriteFlow("Dispose({0})", disposing);
			IsDisposing = true;
			if (disposing)
			{
				// If protocol is UDP.
				if (!IsTcp) DisposeKeepAliveTimer();
				if (watcherStatus != null)
				{
					watcherStatus.EnableRaisingEvents = false;
					watcherStatus.Dispose();
					watcherStatus = null;
				}
				if (watcherNetStat != null)
				{
					watcherNetStat.EnableRaisingEvents = false;
					watcherNetStat.Dispose();
					watcherNetStat = null;
				}
				DisposeConnectionMonitor();
				DisposeQueueMonitor();
				CloseConnections();
				//EventArgsPool.Dispose();
				lock (EventArgsSemaphoreLock)
				{
					if (EventArgsSemaphore != null)
					{
						EventArgsSemaphore.Dispose();
						EventArgsSemaphore = null;
					}
				}
				if (LogWriterDispose)
				{
					LogWriter.Dispose();
				}
				EventArgsSemaphore = null;
				// Log writer is external object. Dispose it outside.
				//LogWriter.Dispose();
			}
		}

		object MainSessionIdLock = new object();

		/// <summary>
		/// The session ID correlates with all the data sent in a connected session.
		/// It is different from the transmission ID in the DataHolder, which relates
		/// to one TCP message. A connected session could have many messages, if you
		/// set up your application to allow it.
		/// </summary>
		protected int CreateSessionId()
		{
			lock (MainSessionIdLock)
			{
				var sessionId = Settings.MainSessionId;
				Settings.MainSessionId++;
				return sessionId;
			}
		}

		#endregion

		#region Process Message

		public delegate void ProcessHeadDelegate(SocketServer server, SocketAsyncEventArgs args);
		public delegate void ProcessMessageDelegate(SocketServer server, SocketAsyncEventArgs args);

		/// <summary>
		/// Process header bytes. Value of this property must be set inside this method:
		/// args.UserToken.BodyBytesLength
		/// </summary>
		public ProcessHeadDelegate ProcessHead;

		/// <summary>
		/// Process body bytes in order to complete message. Called from CompleteReceive.
		/// </summary>
		public ProcessMessageDelegate ProcessMessage;

		/// <summary>
		///  This function will be overridden.
		/// </summary>
		void ProcessHeadDefault(SocketServer server, SocketAsyncEventArgs args)
		{
			var token = (UserToken)args.UserToken;
			// Must set to null if no errors.
			token.HeadProcessError = null;
			// Determine the length of the message that we are working on.
			// Use first 4 bytes of message.
			var length = BitConverter.ToInt32(token.MessageBytes, 0);
			token.BodyBytesLength = length;
		}

		int ServerMainTransMissionId = -1;
		object ServerMainTransMissionIdLock = new object();
		int GetReceivedTransmitionId()
		{
			{
				lock (ServerMainTransMissionIdLock)
				{
					if (ServerMainTransMissionId == -1)
					{
						ServerMainTransMissionId = Settings.ServerMainTransMissionId;
					}
					return ServerMainTransMissionId++;
				}
			}
		}

		/// <summary>
		/// Fill head bytes from received buffer.
		/// </summary>
		/// <returns>Returns number of bytes remaining to process.</returns>
		private bool HandleMessageHead(SocketAsyncEventArgs args)
		{
			var token = (UserToken)args.UserToken;
			// HeadBytesProcessed tells us how many head bytes were
			// processed during previous receive ops which contained data for
			// this message. Usually there will NOT have been any previous receive ops here.
			// So in that case, receiveSendToken.HeadBytesProcessed would equal 0.
			// Create a byte array to put the new head in, if we have not already done it in a previous loop.
			var headLength = Settings.MessageHeadLength;
			bool isComplete = token.HeadBytesProcessed == headLength;
			if (isComplete)
				return isComplete;
			if (token.HeadBytesProcessed == 0)
				token.MessageBytes = new byte[headLength];
			var bytesRequired = headLength - token.HeadBytesProcessed;
			var bytesToHandle = Math.Min(token.ReceivedBytesToProcess, bytesRequired);
			// Write the bytes to the array where we are putting the head data.
			Buffer.BlockCopy(args.Buffer, token.BufferOffsetCurrent, token.MessageBytes, token.HeadBytesProcessed, bytesToHandle);
			token.ReceivedBytesToProcess -= bytesToHandle;
			token.HeadBytesProcessed += bytesToHandle;
			token.BufferOffsetCurrent += bytesToHandle;
			// If there are not enough bytes for very small header then...
			if (bytesToHandle < bytesRequired)
			{
				token.HeadProcessError = new Exception(string.Format("Not enough bytes for header: {0}", bytesToHandle));
				var hex = (Settings.LogData) ? ":\r\n" + LogWriter.GetHexBlock(token.MessageBytes, 0, bytesToHandle) : "";
				ShowToken("HandleMessageHead", token.TokenId, "Error: {0}. HeadBytes[{1}]{2}", token.HeadProcessError.Message, headLength, hex);
				return true;
			}
			// If head is complete then...
			isComplete = token.HeadBytesProcessed == headLength;
			if (isComplete)
			{
				// Determine the length of the message that we are working on.
				// token.BodyBytesLength property will be set inside Process head method.
				var m = ProcessHead;
				// Probably is disposing.
				if (m is null) return false;
				var er = ErrorReceived;
				if (er is null)
				{
					m(this, args);
				}
				else
				{
					try
					{
						m(this, args);
					}
					catch (Exception methodEx)
					{
						methodEx.Data.Add("ErrorReceived", "RemoveMessage");
						var e = new EventArgs<Exception>(methodEx);
						er(this, e);
					}
				}
				if (token.HeadProcessError is null)
				{
					ShowToken("HandleMessageHead", token.TokenId, "HeadBytesProcessed = {0}, bytesToHandle = {1}", token.HeadBytesProcessed, bytesToHandle);
					Array.Resize(ref token.MessageBytes, Settings.MessageHeadLength + token.BodyBytesLength);
				}
				else
				{
					var hex = (Settings.LogData) ? ":\r\n" + LogWriter.GetHexBlock(token.MessageBytes, 0, headLength) : "";
					ShowToken("HandleMessageHead", token.TokenId, "Error: {0}. HeadBytes[{1}]{2}", token.HeadProcessError.Message, headLength, hex);
				}
			}
			else
			{
				ShowToken("HandleMessageHead", token.TokenId, "HeadBytesProcessed = {0}, bytesToHandle = {1}", token.HeadBytesProcessed, bytesToHandle);
			}
			return isComplete;
		}

		/// <summary>
		/// Fill body bytes from received buffer.
		/// </summary>
		/// <returns>Returns number of bytes remaining to process.</returns>
		private bool HandleMessageBody(SocketAsyncEventArgs args)
		{
			var token = (UserToken)args.UserToken;
			// Create the array where we'll store the complete message,
			// if it has not been created on a previous receive op.
			var length = token.BodyBytesLength;
			var headLength = Settings.MessageHeadLength;
			var bytesRequired = length - token.BodyBytesProcessed;
			var bytesToHandle = Math.Min(token.ReceivedBytesToProcess, bytesRequired);
			bool isComplete = token.BodyBytesProcessed == length;
			if (!isComplete)
			{
				// Write the bytes to the array where we are putting the message data.
				Buffer.BlockCopy(args.Buffer, token.BufferOffsetCurrent, token.MessageBytes, headLength + token.BodyBytesProcessed, bytesToHandle);
				token.ReceivedBytesToProcess -= bytesToHandle;
				token.BodyBytesProcessed += bytesToHandle;
				token.BufferOffsetCurrent += bytesToHandle;
			}
			// If message is complete then...
			isComplete = token.BodyBytesProcessed == length;
			if (isComplete)
			{
				var data = token.MessageBytes;
				var hex = (Settings.LogData) ? ":\r\n" + LogWriter.GetHexBlock(data, 0, data.Length) : "";
				ShowToken("HandleMessageBody", token.TokenId, "MessageBytes[{0}]{1}", data.Length, hex);
			}
			else
			{
				ShowToken("HandleMessageBody", token.TokenId, "BodyBytesProcessed = {0}, bytesToHandle = {1}", token.BodyBytesProcessed, bytesToHandle);
			}
			return isComplete;
		}

		#endregion

	}
}
