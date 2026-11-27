#:include WheelTelemetry.Decoders.cs
#:include WheelTelemetry.Control.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using x360ce.Plugins;

namespace x360ce.Plugins.Samples
{
	/// <summary>
	/// Reads live vehicle telemetry from standard racing game protocols (Shared Memory and localhost UDP)
	/// and feeds it into x360ce's steering wheel centering spring without taking over other apps' ports.
	/// </summary>
	[Description("Reads live vehicle telemetry from racing games over Windows Shared Memory (Assetto Corsa, ACC, AC Evo, iRacing) and standard localhost UDP formats (Forza Data Out, EA F1, Codemasters, OutSim, Project CARS). Listens only on 127.0.0.1:37150 by default so SimHub, CrewChief, Pit House and other telemetry apps keep their ports.")]
	public partial class WheelTelemetryProvider : IPluginWheelTelemetry, IPluginWheelTelemetrySettings, IDisposable
	{
		/// <summary>x360ce's dedicated conflict-free loopback UDP port (37ACD plugin range).</summary>
		public const int DefaultPort = 37150;

		/// <summary>Common unicast game ports, OFF by default so x360ce never blocks SimHub, CrewChief or wheel dashboards.</summary>
		public static readonly int[] StandardUnicastPorts = { 5300, 20777, 4123 };

		/// <summary>Project CARS / AMS2 broadcast UDP port, shared via ReuseAddress only when standard ports are opted in.</summary>
		public const int BroadcastPcarsPort = 5606;

		/// <summary>How long without packets before the active protocol lock resets so another game can be detected automatically.</summary>
		public const long UnlockAfterMs = 1500;

		static readonly double TicksToMs = 1000.0 / Stopwatch.Frequency;

		public string Name { get { return "Wheel Telemetry"; } }

		// Configuration written from UI/host thread; worker detects _configVersion changes and rebinds cleanly.
		volatile int _dedicatedPort = DefaultPort;
		volatile bool _listenStandardPorts;
		volatile int _forwardPort;
		volatile string _forwardTargets = "";
		int _configVersion;

		// Lock-free seqlock snapshot read by the 1000 Hz engine thread.
		int _seq;
		WheelTelemetry _snapshot = WheelTelemetry.Neutral;

		// Worker lifecycle (lazy-started on first TryReadTelemetry so probe instances open no sockets).
		readonly object _startLock = new object();
		readonly ManualResetEventSlim _stop = new ManualResetEventSlim(false);
		Thread _worker;
		volatile bool _disposed;
		volatile bool _noWorker;

		// Live diagnostic state shown on the Wheel tab.
		volatile string _portStatus = "Idle";
		volatile int _packetRateHz;
		volatile int[] _boundPorts = new int[0];

		int _rateCount;
		long _rateWindowStartMs;

		public WheelTelemetryProvider()
		{
		}

		/// <summary>Test constructor that disables background threads and sockets.</summary>
		internal WheelTelemetryProvider(bool startWorker)
		{
			_noWorker = !startWorker;
		}

		public string PortStatus { get { return _portStatus; } }
		public int PacketRateHz { get { return _packetRateHz; } }

		/// <summary>UDP ports currently bound by this provider (for diagnostics and unit tests).</summary>
		public int[] BoundPorts { get { return _boundPorts; } }

		public void Configure(int port, bool listenStandardPorts, int forwardPort)
		{
			var cleanPort = port >= 1024 && port <= 65535 ? port : DefaultPort;
			var cleanForward = forwardPort >= 1024 && forwardPort <= 65535 && forwardPort != cleanPort ? forwardPort : 0;
			if (cleanPort == _dedicatedPort && listenStandardPorts == _listenStandardPorts && cleanForward == _forwardPort)
				return;
			_dedicatedPort = cleanPort;
			_listenStandardPorts = listenStandardPorts;
			_forwardPort = cleanForward;
			Interlocked.Increment(ref _configVersion);
		}

		public void ConfigureForwardTargets(string targets)
		{
			var clean = (targets ?? "").Trim();
			if (string.Equals(clean, _forwardTargets, StringComparison.Ordinal))
				return;
			_forwardTargets = clean;
			Interlocked.Increment(ref _configVersion);
		}

		/// <summary>Parses comma/semicolon/whitespace-separated <c>port</c> or <c>ip:port</c> forwarding targets, skipping self-loops.</summary>
		public static IPEndPoint[] ParseForwardTargets(string targets, int fallbackLoopbackPort, ICollection<int> ownBoundPorts)
		{
			var result = new List<IPEndPoint>();
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			Action<IPAddress, int> add = (ip, port) =>
			{
				if (ip == null || ip.AddressFamily != AddressFamily.InterNetwork || port < 1024 || port > 65535)
					return;
				// Never relay back to one of our own listening ports on loopback (prevents infinite UDP packet storms).
				if (IPAddress.IsLoopback(ip) && ownBoundPorts != null && ownBoundPorts.Contains(port))
					return;
				var key = ip.ToString() + ":" + port;
				if (seen.Add(key))
					result.Add(new IPEndPoint(ip, port));
			};

			if (!string.IsNullOrWhiteSpace(targets))
			{
				var tokens = targets.Split(new[] { ',', ';', '\r', '\n', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
				for (var i = 0; i < tokens.Length; i++)
				{
					var token = tokens[i].Trim();
					var colon = token.LastIndexOf(':');
					int port;
					if (colon < 0)
					{
						if (int.TryParse(token, out port))
							add(IPAddress.Loopback, port);
						continue;
					}
					var host = token.Substring(0, colon).Trim();
					if (!int.TryParse(token.Substring(colon + 1).Trim(), out port))
						continue;
					if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
					{
						add(IPAddress.Loopback, port);
						continue;
					}
					IPAddress parsed;
					if (IPAddress.TryParse(host, out parsed))
						add(parsed, port);
				}
			}
			else if (fallbackLoopbackPort >= 1024 && fallbackLoopbackPort <= 65535)
			{
				add(IPAddress.Loopback, fallbackLoopbackPort);
			}
			return result.ToArray();
		}

		/// <summary>Wait-free read called up to 1000 times a second on x360ce's engine thread.</summary>
		public bool TryReadTelemetry(ref WheelTelemetry telemetry)
		{
			if (_disposed)
				return false;
			if (!_noWorker && _worker == null)
				EnsureWorkerStarted();

			var s1 = Volatile.Read(ref _seq);
			if ((s1 & 1) != 0)
				return false;
			var copy = _snapshot;
			var s2 = Volatile.Read(ref _seq);
			if (s1 != s2 || copy.TimestampMs <= 0)
				return false;
			telemetry = copy;
			return true;
		}

		void EnsureWorkerStarted()
		{
			lock (_startLock)
			{
				if (_disposed || _worker != null)
					return;
				_worker = new Thread(WorkerLoop)
				{
					Name = "x360ce.WheelTelemetry",
					IsBackground = true,
				};
				_worker.Start();
			}
		}

		void Publish(ref WheelTelemetry sample)
		{
			Interlocked.Increment(ref _seq);
			_snapshot = sample;
			Interlocked.Increment(ref _seq);

			var now = sample.TimestampMs;
			if (_rateWindowStartMs == 0)
				_rateWindowStartMs = now;
			_rateCount++;
			var elapsed = now - _rateWindowStartMs;
			if (elapsed >= 1000)
			{
				_packetRateHz = (int)Math.Round(_rateCount * 1000.0 / elapsed);
				_rateCount = 0;
				_rateWindowStartMs = now;
			}
		}

		static long NowMs()
		{
			return (long)(Stopwatch.GetTimestamp() * TicksToMs);
		}

		/// <summary>
		/// Opens the configured UDP listeners cleanly without overtaking occupied ports.
		/// Exposed as a static-style helper for unit testing port-safety rules.
		/// </summary>
		sealed class BoundListener : IDisposable
		{
			public int Port;
			public bool IsBroadcast;
			public Socket Socket;

			public void Dispose()
			{
				if (Socket != null)
				{
					try { Socket.Close(); } catch { }
					Socket = null;
				}
			}
		}

		void WorkerLoop()
		{
			var listeners = new List<BoundListener>();
			var selectList = new List<Socket>();
			var buffer = new byte[2048];
			Socket forwardSocket = null;
			IPEndPoint[] forwardEndpoints = new IPEndPoint[0];
			var appliedConfig = -1;
			var lastShmScanMs = 0L;
			var lastShmPollMs = 0L;
			MemoryMappedFile activeMap = null;
			MemoryMappedViewAccessor activeAccessor = null;
			string activeMapName = null;
			byte[] shmBuffer = null;

			try
			{
				while (!_disposed && !_stop.IsSet)
				{
					var ver = Volatile.Read(ref _configVersion);
					if (ver != appliedConfig)
					{
						appliedConfig = ver;
						CloseListeners(listeners);
						CloseSocket(ref forwardSocket);
						BindConfiguredListeners(listeners);
						forwardEndpoints = ParseForwardTargets(_forwardTargets, _forwardPort, _boundPorts);
						if (forwardEndpoints.Length > 0)
						{
							forwardSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
							var fwdNote = forwardEndpoints.Length == 1
								? "relay -> " + forwardEndpoints[0]
								: "relay -> " + forwardEndpoints.Length + " targets";
							_portStatus = _portStatus + " | " + fwdNote;
						}
					}

					var nowMs = NowMs();
					if (_rateWindowStartMs > 0 && nowMs - _rateWindowStartMs > 1500)
					{
						_packetRateHz = 0;
						_rateCount = 0;
						_rateWindowStartMs = nowMs;
					}

					// 1. Poll any active Shared Memory map at ~125 Hz, or scan idle maps every 1500 ms.
					if (activeAccessor != null)
					{
						if (nowMs - lastShmPollMs >= 8)
						{
							lastShmPollMs = nowMs;
							if (!PollActiveSharedMemory(activeMapName, activeAccessor, ref shmBuffer, nowMs))
							{
								CloseSharedMemory(ref activeAccessor, ref activeMap, ref activeMapName);
								lastShmScanMs = nowMs;
							}
						}
					}
					else if (nowMs - lastShmScanMs >= 1500)
					{
						lastShmScanMs = nowMs;
						TryOpenKnownSharedMemory(ref activeMap, ref activeAccessor, ref activeMapName, ref shmBuffer, nowMs);
					}

					// 2. Non-blocking select across all bound UDP sockets (short wait so Shared Memory stays snappy).
					if (listeners.Count == 0)
					{
						_stop.Wait(50);
						continue;
					}

					selectList.Clear();
					for (var i = 0; i < listeners.Count; i++)
						selectList.Add(listeners[i].Socket);

					try
					{
						Socket.Select(selectList, null, null, activeAccessor != null ? 8000 : 50000);
					}
					catch (SocketException)
					{
						continue;
					}
					catch (ObjectDisposedException)
					{
						break;
					}

					if (selectList.Count == 0)
						continue;

					nowMs = NowMs();
					for (var i = 0; i < selectList.Count; i++)
					{
						var sock = selectList[i];
						var port = PortOf(listeners, sock);
						// Drain up to 8 queued packets so we always publish the newest physics frame.
						for (var drain = 0; drain < 8; drain++)
						{
							if (sock.Available <= 0 && drain > 0)
								break;
							int received;
							try
							{
								received = sock.Receive(buffer);
							}
							catch (SocketException)
							{
								break;
							}
							catch (ObjectDisposedException)
							{
								break;
							}
							if (received <= 0)
								break;

							if (forwardSocket != null && forwardEndpoints.Length > 0)
							{
								for (var f = 0; f < forwardEndpoints.Length; f++)
								{
									var ep = forwardEndpoints[f];
									if (IPAddress.IsLoopback(ep.Address) && ep.Port == port)
										continue;
									try
									{
										forwardSocket.SendTo(buffer, 0, received, SocketFlags.None, ep);
									}
									catch (SocketException)
									{
									}
								}
							}
							TakeUdp(port, buffer, received, nowMs);
						}
					}
				}
			}
			finally
			{
				CloseSharedMemory(ref activeAccessor, ref activeMap, ref activeMapName);
				CloseListeners(listeners);
				CloseSocket(ref forwardSocket);
				_boundPorts = new int[0];
				_packetRateHz = 0;
				_portStatus = "Stopped";
			}
		}

		void BindConfiguredListeners(List<BoundListener> listeners)
		{
			var occupied = GetActiveUdpPorts();
			var notes = new List<string>();
			var bound = new List<int>();
			var dedicated = _dedicatedPort;

			TryBindUnicast(listeners, dedicated, occupied, bound, notes, dedicated);

			if (_listenStandardPorts)
			{
				for (var i = 0; i < StandardUnicastPorts.Length; i++)
				{
					var port = StandardUnicastPorts[i];
					if (port != dedicated)
						TryBindUnicast(listeners, port, occupied, bound, notes, dedicated);
				}
				TryBindBroadcast(listeners, BroadcastPcarsPort, bound, notes);
			}

			_boundPorts = bound.ToArray();
			var prefix = bound.Count > 0
				? "UDP " + string.Join(", ", bound) + " + Shared Memory"
				: "Shared Memory only";
			_portStatus = notes.Count > 0 ? prefix + " (" + string.Join("; ", notes) + ")" : prefix;
		}

		static HashSet<int> GetActiveUdpPorts()
		{
			var set = new HashSet<int>();
			try
			{
				var endpoints = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners();
				for (var i = 0; i < endpoints.Length; i++)
					set.Add(endpoints[i].Port);
			}
			catch
			{
			}
			return set;
		}

		static void TryBindUnicast(List<BoundListener> listeners, int port, HashSet<int> occupied, List<int> bound, List<string> notes, int dedicatedPort)
		{
			if (occupied.Contains(port))
			{
				notes.Add(port == dedicatedPort
					? "port " + port + " in use"
					: "port " + port + " in use by another app — forward to " + dedicatedPort);
				return;
			}
			Socket socket = null;
			try
			{
				socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				socket.ExclusiveAddressUse = true;
				socket.Blocking = false;
				socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
				listeners.Add(new BoundListener { Port = port, IsBroadcast = false, Socket = socket });
				bound.Add(port);
				socket = null;
			}
			catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse || ex.SocketErrorCode == SocketError.AccessDenied)
			{
				notes.Add(port == dedicatedPort
					? "port " + port + " in use"
					: "port " + port + " in use by another app — forward to " + dedicatedPort);
			}
			catch (SocketException)
			{
			}
			finally
			{
				if (socket != null)
					socket.Close();
			}
		}

		static void TryBindBroadcast(List<BoundListener> listeners, int port, List<int> bound, List<string> notes)
		{
			Socket socket = null;
			try
			{
				socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
				socket.EnableBroadcast = true;
				socket.Blocking = false;
				socket.Bind(new IPEndPoint(IPAddress.Any, port));
				listeners.Add(new BoundListener { Port = port, IsBroadcast = true, Socket = socket });
				bound.Add(port);
				socket = null;
			}
			catch (SocketException)
			{
				notes.Add("broadcast port " + port + " unavailable");
			}
			finally
			{
				if (socket != null)
					socket.Close();
			}
		}

		static int PortOf(List<BoundListener> listeners, Socket socket)
		{
			for (var i = 0; i < listeners.Count; i++)
				if (listeners[i].Socket == socket)
					return listeners[i].Port;
			return 0;
		}

		static void CloseListeners(List<BoundListener> listeners)
		{
			for (var i = 0; i < listeners.Count; i++)
				listeners[i].Dispose();
			listeners.Clear();
		}

		static void CloseSocket(ref Socket socket)
		{
			if (socket == null)
				return;
			try { socket.Close(); } catch { }
			socket = null;
		}

		#region Shared Memory scanner

		static readonly string[] KnownSharedMemoryMaps =
		{
			AcPhysicsMapName,
			IRacingMapName,
		};

		const int AcPhysicsReadSize = 256;
		const int IRacingReadSize = 131072;

		void TryOpenKnownSharedMemory(ref MemoryMappedFile map, ref MemoryMappedViewAccessor accessor, ref string mapName, ref byte[] buffer, long nowMs)
		{
			for (var i = 0; i < KnownSharedMemoryMaps.Length; i++)
			{
				var name = KnownSharedMemoryMaps[i];
				MemoryMappedFile candidate = null;
				MemoryMappedViewAccessor view = null;
				try
				{
					candidate = MemoryMappedFile.OpenExisting(name, MemoryMappedFileRights.Read);
					var size = name == IRacingMapName ? IRacingReadSize : AcPhysicsReadSize;
					view = candidate.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
					var readLen = (int)Math.Min(view.Capacity, size);
					if (readLen <= 0)
					{
						view.Dispose();
						candidate.Dispose();
						continue;
					}
					if (buffer == null || buffer.Length < readLen)
						buffer = new byte[readLen];
					view.ReadArray(0, buffer, 0, readLen);
					// Keep the map open as soon as the header is recognised; publishing starts once its tick counter advances.
					if (IsKnownSharedMemoryHeader(name, buffer, readLen))
					{
						map = candidate;
						accessor = view;
						mapName = name;
						TakeSharedMemory(name, buffer, readLen, nowMs);
						return;
					}
					view.Dispose();
					candidate.Dispose();
				}
				catch (FileNotFoundException)
				{
				}
				catch (UnauthorizedAccessException)
				{
					if (view != null) view.Dispose();
					if (candidate != null) candidate.Dispose();
				}
				catch (IOException)
				{
					if (view != null) view.Dispose();
					if (candidate != null) candidate.Dispose();
				}
			}
		}

		bool PollActiveSharedMemory(string mapName, MemoryMappedViewAccessor accessor, ref byte[] buffer, long nowMs)
		{
			try
			{
				var size = mapName == IRacingMapName ? IRacingReadSize : AcPhysicsReadSize;
				var readLen = (int)Math.Min(accessor.Capacity, size);
				if (readLen <= 0)
					return false;
				if (buffer == null || buffer.Length < readLen)
					buffer = new byte[readLen];
				accessor.ReadArray(0, buffer, 0, readLen);
				TakeSharedMemory(mapName, buffer, readLen, nowMs);
				return true;
			}
			catch
			{
				return false;
			}
		}

		static void CloseSharedMemory(ref MemoryMappedViewAccessor accessor, ref MemoryMappedFile map, ref string mapName)
		{
			if (accessor != null)
			{
				try { accessor.Dispose(); } catch { }
				accessor = null;
			}
			if (map != null)
			{
				try { map.Dispose(); } catch { }
				map = null;
			}
			mapName = null;
		}

		#endregion

		/// <summary>Stops the background worker without blocking the engine thread.</summary>
		public void Dispose()
		{
			if (_disposed)
				return;
			_disposed = true;
			_stop.Set();
			var neutral = WheelTelemetry.Neutral;
			Interlocked.Increment(ref _seq);
			_snapshot = neutral;
			Interlocked.Increment(ref _seq);
		}
	}
}
