using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace JocysCom.ClassLibrary.Network
{

	public static class NetworkHelper
	{
		/// <summary>
		/// Original source.
		/// https://code.msdn.microsoft.com/C-Sample-to-list-all-the-4817b58f/sourcecode?fileId=147562&pathId=62315043
		/// </summary>

		public class NativeMethods
		{

			[DllImport("kernel32.dll")]
			internal static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

			[DllImport("psapi.dll", CharSet = CharSet.Unicode)]
			internal static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpBaseName, [In][MarshalAs(UnmanagedType.U4)] int nSize);

			[DllImport("kernel32.dll", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			internal static extern bool CloseHandle(IntPtr hObject);

			[DllImport("iphlpapi.dll", SetLastError = true)]
			internal static extern uint GetExtendedTcpTable(IntPtr tcpTable, ref int tableLength, bool sort, int ipVersion, TCP_TABLE_CLASS tableClass, uint reserved = 0);

			[DllImport("iphlpapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
			internal static extern uint GetExtendedUdpTable(IntPtr udpTable, ref int tableLength, bool sort, int ipVersion, UDP_TABLE_CLASS tableClass, uint reserved = 0);

		}

		#region Ping

		public static bool Ping(string hostNameOrAddress, int timeout = 1000)
		{
			Exception error;
			return Ping(hostNameOrAddress, timeout, out error);
		}

		public static bool Ping(string hostNameOrAddress, int timeout, out Exception error)
		{
			var success = false;
			error = null;
			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();
			PingReply reply = null;
			Exception replyError = null;
			// Use proper threading, because other asynchronous classes
			// like "Tasks" have problems with Ping.
			var ts = new System.Threading.ThreadStart(delegate ()
			{
				var ping = new Ping();
				try
				{
					reply = ping.Send(hostNameOrAddress);
				}
				catch (Exception ex)
				{
					replyError = ex;
				}
				ping.Dispose();
			});
			var t = new System.Threading.Thread(ts);
			t.Start();
			t.Join(timeout);
			if (reply != null)
			{
				success = (reply.Status == IPStatus.Success);
			}
			else if (replyError != null)
			{
				error = replyError;
			}
			else
			{
				error = new Exception("Ping timed out (" + timeout.ToString() + "): " + sw.Elapsed.ToString());
			}
			return success;
		}

		#endregion

		#region Check Network

		static bool IsIpAddress(string addr)
		{
			IPAddress ip;
			if (!IPAddress.TryParse(addr, out ip))
				return false;
			return ip.AddressFamily == AddressFamily.InterNetworkV6 || ip.AddressFamily == AddressFamily.InterNetwork;
		}

		public static bool IsPortOpen(string host, int port, int timeout = 20000, int retry = 1, bool isUdp = false)
		{
			var retryCount = 0;
			while (retryCount < retry)
			{
				if (retryCount > 0)
					// Logical delay without blocking the current hardware thread.
					Task.Delay(timeout).Wait();
				if (isUdp)
				{
					// UDP is connectionless by design, therefore method below is not 100 % reliable.
					// Firewall and network setups might influence the result.
					//
					// SUPPRESS: CWE-772: Missing Release of Resource after Effective Lifetime
					// Note: False Positive. Resources will be cleaned up, because finally block always runs.
					// The only case when finally clause don't run is when program immediately stopped.
					// More: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/try-finally
					var udp = new UdpClient();
					try
					{
						var result = udp.BeginSend(new byte[1], 1, host, port, null, null);
						var success = result.AsyncWaitHandle.WaitOne(timeout);
						if (success)
						{
							udp.BeginReceive(null, null);
							udp.EndSend(result);
							var result2 = udp.BeginReceive(null, null);
							var success2 = result.AsyncWaitHandle.WaitOne(timeout);
							if (success2)
							{
								IPEndPoint remoteEP = null;
								udp.EndReceive(result2, ref remoteEP);
								return true;
							}
						}
					}
					catch (SocketException udpEx)
					{
						// If port was forcibly closed (WSAECONNRESET) then...
						if (udpEx.ErrorCode == 10054)
							return false;
					}
					finally
					{
						udp.Close();
						retryCount++;
					}
					// Answer or timeout means that port is open.
					return true;
				}
				else
				{
					// SUPPRESS: CWE-772: Missing Release of Resource after Effective Lifetime
					// Note: False Positive. Resources will be cleaned up, because finally block always runs.
					// The only case when finally clause don't run is when program immediately stopped.
					// More: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/try-finally
					var tcp = new TcpClient();
					try
					{
						var result = tcp.BeginConnect(host, port, null, null);
						var success = result.AsyncWaitHandle.WaitOne(timeout);
						if (success)
						{
							tcp.EndConnect(result);
							return true;
						}
					}
					catch { }
					finally
					{
						tcp.Close();
						retryCount++;
					}
				}

			}
			return false;
		}

		public static CheckNetworkState CheckNetwork(string url, IList<string> log)
		{
			Uri u;
			DateTime start;
			// Check access to known and reliable public HTTP site.
			start = DateTime.Now;
			try
			{
				u = new Uri("http://www.google.co.uk");
				log.Add(string.Format("Test TCP/IP connection with {0}:{1}...", u.Host, u.Port));
				var client = new TcpClient(u.Host, u.Port);
				client.Close();
				CnsAddLog(log, start);
			}
			catch (Exception ex1)
			{
				return CnsAddLog(log, start, ex1, CheckNetworkState.PublicHttp);
			}
			// Check access to known and reliable public HTTPS site.
			start = DateTime.Now;
			try
			{
				u = new Uri("https://www.google.co.uk");
				log.Add(string.Format("Test TCP/IP connection with {0}:{1}...", u.Host, u.Port));
				var client = new TcpClient(u.Host, u.Port);
				client.Close();
				CnsAddLog(log, start);
			}
			catch (Exception ex2)
			{
				return CnsAddLog(log, start, ex2, CheckNetworkState.PublicHttps);
			}
			// Check custom URL.
			u = new Uri(url);
			// If URL is IP address then...
			if (IsIpAddress(u.Host))
			{
				// Try to get IP address (check if DNS works).
				log.Add(string.Format("Test DNS - Skipping. Host is IP address: {0}", u.Host));
			}
			else
			{
				// Test DNS - Try to resolve IP address.
				start = DateTime.Now;
				try
				{
					log.Add(string.Format("Test DNS - acquiring IP address for {0} host...", u.Host));
					var he = System.Net.Dns.GetHostEntry(u.Host);
					var ips = he.AddressList.Select(x => string.Join(".", x.GetAddressBytes().Select(y => ((int)y).ToString()).ToArray())).ToArray();
					if (ips.Length == 0)
					{
						var ex3a = new Exception("Host IP address is not available");
						return CnsAddLog(log, start, ex3a, CheckNetworkState.PublicDns);
					}
					else
					{
						CnsAddLog(log, start, null, null, "{0}", string.Join(", ", ips));
					}
				}
				catch (Exception ex3)
				{
					return CnsAddLog(log, start, ex3, CheckNetworkState.PublicDns);
				}
			}
			// Test TCP/IP connection with URL.
			start = DateTime.Now;
			try
			{
				// Check if web server is running.
				log.Add(string.Format("Test TCP/IP connection with {0}:{1}...", u.Host, u.Port));
				var client = new TcpClient(u.Host, u.Port);
				client.Close();
				CnsAddLog(log, start);
			}
			catch (Exception ex4)
			{
				return CnsAddLog(log, start, ex4, CheckNetworkState.RemoteWebServer);
			}
			// Test HTTP/HTTPS request.
			start = DateTime.Now;
			try
			{
				// Check if web service page works.
				log.Add(string.Format("Test URL - requesting {0} URL...", u.AbsoluteUri));
				var client = new HttpClient();
				// SUPPRESS: CWE-918: Server-Side Request Forgery (SSRF).
				// Note: External users do not have control over request URL.
				var response = Task.Run(() => client.GetAsync(u)).Result;
				var statusCode = (int)response.StatusCode;
				var statusText = response.ReasonPhrase;
				client.Dispose();
				CnsAddLog(log, start, null, null, "Response Status: {0} - {1}", statusCode, statusText);
			}
			catch (Exception ex5)
			{
				return CnsAddLog(log, start, ex5, CheckNetworkState.RemoteWebService);
			}
			log.Add("RESULT: no issues found.");
			return CheckNetworkState.OK;
		}

		static CheckNetworkState CnsAddLog(IList<string> log, DateTime start, Exception ex = null, CheckNetworkState? state = null, string format = null, params object[] args)
		{
			var sb = new StringBuilder();
			sb.Append(' ', 2);
			sb.Append(ex is null ? "PASS" : "FAIL");
			sb.AppendFormat(": {0:0.000} sec.", DateTime.Now.Subtract(start).TotalSeconds);
			if (ex != null)
				sb.AppendFormat(": {0}", ex.Message);
			if (format != null)
				sb.AppendFormat(": " + format, args);
			log.Add(sb.ToString());
			if (ex != null)
			{
				sb.AppendFormat(": {0}", ex.Message);
				var i = new[] {
					CheckNetworkState.PublicHttp,
					CheckNetworkState.PublicHttps,
					CheckNetworkState.PublicDns,
				};
				var e = new[] {
					CheckNetworkState.RemoteWebService,
					CheckNetworkState.RemoteWebServer
				};
				if (i.Contains(state.Value))
					log.Add("RESULT: internal issue found.");
				else if (e.Contains(state.Value))
					log.Add("RESULT: external issue found.");
			}
			return state.HasValue ? state.Value : CheckNetworkState.None;
		}

		#endregion

		#region Get Information

		public static string GetProcessName(int pid)
		{
			var processHandle = NativeMethods.OpenProcess(0x0400 | 0x0010, false, pid);
			if (processHandle == IntPtr.Zero)
			{
				return null;
			}
			const int lengthSb = 4000;
			var sb = new StringBuilder(lengthSb);
			string result = null;
			if (NativeMethods.GetModuleFileNameEx(processHandle, IntPtr.Zero, sb, lengthSb) > 0)
			{
				result = Path.GetFileName(sb.ToString());
			}
			NativeMethods.CloseHandle(processHandle);
			return result;
		}

		public static string GetExtendedTable(bool sorted = false)
		{
			var tcpList = GetExtendedTcpTable(true);
			var udpList = GetExtendedUdpTable(true);
			var list = new List<NetStatInfo>();
			list.AddRange(tcpList);
			list.AddRange(udpList);
			var items = list.Select(x => x.ToLineString());
			var result = string.Join("\r\n", items);
			return NetStatInfo.ToHeaderLine() + result;
		}

		public static NetStatInfo[] GetExtendedTcpTable(bool sorted = false)
		{
			IntPtr table = IntPtr.Zero;
			int tableLength = 0;
			NetStatInfo[] rows = null;
			int AfInet = 2;
			if (NativeMethods.GetExtendedTcpTable(table, ref tableLength, sorted, AfInet, TCP_TABLE_CLASS.OwnerPidAll, 0) != 0)
			{
				try
				{
					table = Marshal.AllocHGlobal(tableLength);
					if (NativeMethods.GetExtendedTcpTable(table, ref tableLength, true, AfInet, TCP_TABLE_CLASS.OwnerPidAll, 0) == 0)
					{
						var mibTable = (MIB_TCPTABLE_OWNER_PID)Marshal.PtrToStructure(table, typeof(MIB_TCPTABLE_OWNER_PID));
						rows = new NetStatInfo[mibTable.NumEntries];
						IntPtr rowPtr = (IntPtr)((long)table + Marshal.SizeOf(mibTable.NumEntries));
						for (int i = 0; i < mibTable.NumEntries; ++i)
						{
							var mibRow = (MIB_TCPROW_OWNER_PID)Marshal.PtrToStructure(rowPtr, typeof(MIB_TCPROW_OWNER_PID));
							var row = new NetStatInfo();
							row.Proto = ProtocolType.Tcp;
							row.State = mibRow.state;
							row.ProcessId = mibRow.owningPid;
							row.LocalAddress = new IPAddress(mibRow.localAddr);
							row.LocalPort = BitConverter.ToUInt16(new byte[2] { mibRow.localPort[1], mibRow.localPort[0] }, 0);
							row.RemoteAddress = new IPAddress(mibRow.remoteAddr);
							row.RemotePort = BitConverter.ToUInt16(new byte[2] { mibRow.remotePort[1], mibRow.remotePort[0] }, 0);
							if (row.ProcessId > 0)
							{
								row.ProcessName = GetProcessName(row.ProcessId);
							}
							rows[i] = row;
							rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf(typeof(MIB_TCPROW_OWNER_PID)));
						}
					}
				}
				finally
				{
					if (table != IntPtr.Zero)
					{
						Marshal.FreeHGlobal(table);
					}
				}
			}

			return rows;
		}

		public static NetStatInfo[] GetExtendedUdpTable(bool sorted = false)
		{
			IntPtr table = IntPtr.Zero;
			int tableLength = 0;
			NetStatInfo[] rows = null;
			int AfInet = 2;
			if (NativeMethods.GetExtendedUdpTable(table, ref tableLength, sorted, AfInet, UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID, 0) != 0)
			{
				try
				{
					table = Marshal.AllocHGlobal(tableLength);
					if (NativeMethods.GetExtendedUdpTable(table, ref tableLength, true, AfInet, UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID, 0) == 0)
					{
						var mibTable = (MIB_UDPTABLE_OWNER_PID)Marshal.PtrToStructure(table, typeof(MIB_UDPTABLE_OWNER_PID));
						rows = new NetStatInfo[mibTable.NumEntries];
						IntPtr rowPtr = (IntPtr)((long)table + Marshal.SizeOf(mibTable.NumEntries));
						for (int i = 0; i < mibTable.NumEntries; ++i)
						{
							var mibRow = (MIB_UDPROW_OWNER_PID)Marshal.PtrToStructure(rowPtr, typeof(MIB_UDPROW_OWNER_PID));
							var row = new NetStatInfo();
							row.Proto = ProtocolType.Udp;
							row.ProcessId = mibRow.owningPid;
							row.LocalAddress = new IPAddress(mibRow.localAddr);
							row.LocalPort = BitConverter.ToUInt16(new byte[2] { mibRow.localPort[1], mibRow.localPort[0] }, 0);
							row.RemoteAddress = IPAddress.Any;
							row.RemotePort = 0;
							if (row.ProcessId > 0)
							{
								row.ProcessName = GetProcessName(row.ProcessId);
							}
							rows[i] = row;
							rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf(typeof(MIB_UDPROW_OWNER_PID)));
						}
					}
				}
				finally
				{
					if (table != IntPtr.Zero)
					{
						Marshal.FreeHGlobal(table);
					}
				}
			}

			return rows;
		}

		#endregion

		#region TCP socket enumerations and structures

		/// <summary>
		/// <see cref="http://msdn2.microsoft.com/en-us/library/aa366386.aspx"/>
		/// </summary>
		public enum TCP_TABLE_CLASS
		{
			BasicListener,
			BasicConnections,
			BasicAll,
			OwnerPidListener,
			OwnerPidConnections,
			OwnerPidAll,
			OwnerModuleListener,
			OwnerModuleConnections,
			OwnerModuleAll,
		}

		/// <summary>
		/// The structure contains information that describes an IPv4 TCP connection with 
		/// IPv4 addresses, ports used by the TCP connection, and the specific process ID 
		/// (PID) associated with connection. 
		/// <see cref="http://msdn2.microsoft.com/en-us/library/aa366921.aspx"/>
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct MIB_TCPTABLE_OWNER_PID
		{
			public uint NumEntries;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)] //ArraySubType = UnmanagedType.Struct,
			public MIB_TCPROW_OWNER_PID[] table;
		}

		/// <summary>
		/// The structure contains information that describes an IPv4 TCP connection with 
		/// IPv4 addresses, ports used by the TCP connection, and the specific process ID 
		/// (PID) associated with connection. 
		/// <see cref="http://msdn2.microsoft.com/en-us/library/aa366913.aspx"/>
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct MIB_TCPROW_OWNER_PID
		{
			public TcpState state;
			public uint localAddr;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public byte[] localPort;
			public uint remoteAddr;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public byte[] remotePort;
			public int owningPid;
		}

		#endregion

		#region  UDP socket enumerations and structures

		/// <summary>
		/// Enum to define the set of values used to indicate the type of table returned by calls 
		/// made to the function GetExtendedUdpTable. 
		/// </summary>
		public enum UDP_TABLE_CLASS
		{
			UDP_TABLE_BASIC,
			UDP_TABLE_OWNER_PID,
			UDP_TABLE_OWNER_MODULE
		}

		/// <summary> 
		/// The structure contains an entry from the User Datagram Protocol (UDP) listener 
		/// table for IPv4 on the local computer. The entry also includes the process ID 
		/// (PID) that issued the call to the bind function for the UDP endpoint. 
		/// </summary> 
		[StructLayout(LayoutKind.Sequential)]
		public struct MIB_UDPROW_OWNER_PID
		{
			public uint localAddr;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public byte[] localPort;
			public int owningPid;
		}

		/// <summary> 
		/// The structure contains the User Datagram Protocol (UDP) listener table for IPv4 
		/// on the local computer. The table also includes the process ID (PID) that issued 
		/// the call to the bind function for each UDP endpoint. 
		/// </summary> 
		[StructLayout(LayoutKind.Sequential)]
		public struct MIB_UDPTABLE_OWNER_PID
		{
			public uint NumEntries;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)] //ArraySubType = UnmanagedType.Struct,
			public MIB_UDPROW_OWNER_PID[] table;
		}

		#endregion

		#region Validate

		/// <summary>
		/// Return true if IP Address falls inside the CIDR range.
		/// var inRange = IsInRange("192.168.10.1", "10.10.10.140, 10.16.11.128/25, 192.168.10.0/24"); // true  - 192.168.10.0/24
		/// </summary>
		/// <param name="address">IP address to check.</param>
		/// <param name="networks">IP Addresses or Networks list.</param>
		/// <returns>True if IP Address falls inside the CIDR range.</returns>
		/// <remarks>
		/// Example:
		/// bool inRange;
		/// var networks = "10.10.10.140, 10.16.11.128/25, 192.168.10.0/24";
		/// inRange = IsInRange("192.168.10.1", networks); // true  - 192.168.10.0/24
		/// inRange = IsInRange("10.16.11.120", networks); // false
		/// inRange = IsInRange("10.16.11.128", networks); // true - 10.16.11.128/25 
		/// inRange = IsInRange("10.16.11.255", networks); // true - 10.16.11.128/25
		/// inRange = IsInRange("10.16.12.130", networks); // false
		/// inRange = IsInRange("10.10.10.140", networks); // true - 10.10.10.140
		/// </remarks>
		public static bool IsInRange(string address, string networks)
		{
			IPAddress a;
			if (!IPAddress.TryParse(address, out a))
				return false;
			// Get address as integer.
			var aBytes = a.GetAddressBytes().Reverse().ToArray();
			var aInt = BitConverter.ToInt32(aBytes, 0);
			// Split list. Support various separators.
			var list = networks
				.Split(new char[] { ';', ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(x => x.Trim())
				.Where(x => !string.IsNullOrEmpty(x))
				.ToList();
			foreach (string network in list)
			{
				// If network specified.
				if (network.Contains("/"))
				{
					// Get network and mask strings.
					var nString = network.Split('/')[0].Trim();
					var mString = network.Split('/')[1].Trim();
					IPAddress n;
					if (!IPAddress.TryParse(nString, out n))
						continue;
					int m;
					if (!int.TryParse(mString, out m))
						continue;
					// Get network as integer.
					var nBytes = n.GetAddressBytes().Reverse().ToArray();
					var nInt = BitConverter.ToInt32(nBytes, 0);
					var networkMask = uint.MaxValue << 32 - m;
					// IF IP Address belong to the same network then...
					if ((aInt & networkMask) == (nInt & networkMask))
						return true;
				}
				// Compare with IP address IP Address.
				else
				{
					// If match then...
					if (network == address)
						return true;
				}
			}
			return false;
		}

		#endregion

	}

}
