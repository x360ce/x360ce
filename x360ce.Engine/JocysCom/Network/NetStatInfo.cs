using JocysCom.ClassLibrary.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace JocysCom.ClassLibrary.Network
{

	public class NetworkInfo
	{
		public bool IsNetworkAvailable = false;
		public bool IsMobileNicUp = false;
		public bool IsWirelessNicUp = false;
		public List<KeyValue> NicInfo = new List<KeyValue>();
		public string LocalIpAddress = "0.0.0.0";
		public string LocalGatewayIpAddress = "0.0.0.0";
		public string CurrentNicks = "";
	}

	public class NetStatInfo
	{
		public ProtocolType Proto { get; set; }
		public IPAddress LocalAddress { get; set; }
		public int LocalPort { get; set; }
		public IPAddress RemoteAddress { get; set; }
		public int RemotePort { get; set; }
		public TcpState? State { get; set; }
		public int ProcessId { get; set; }
		public string ProcessName { get; set; }

		#region To String

		public string ToLineString()
		{
			return string.Format("{0} {1,5} {2,-11} {3,-21} {4,-21} {5}",
				Proto.ToString().ToUpper(), ProcessId, State, new IPEndPoint(LocalAddress, LocalPort), new IPEndPoint(RemoteAddress, RemotePort), ProcessName);

		}
		public static string ToHeaderLine()
		{
			return
				"PRO PID   State       Local Endpoint        Remote Endpoint      \r\n" +
				"--- ----- ----------- --------------------- ---------------------\r\n";
		}

		#endregion

		#region Check Network

		public static List<IPAddress> GetAvailableIps()
		{
			var list = new List<IPAddress>();
			var nics = NetworkInterface.GetAllNetworkInterfaces();
			for (int i = 0; i < nics.Length; i++)
			{
				var ni = nics[i];
				var properties = ni.GetIPProperties();
				if (ni.OperationalStatus == OperationalStatus.Up)
				{
					// Loop trough IP address properties.
					for (var a = 0; a < properties.UnicastAddresses.Count; a++)
					{
						var address = properties.UnicastAddresses[a].Address;
						list.Add(address);
					}
				}
			}
			return list;
		}

		/// <summary>
		/// Returns true if IP address have match one of the patterns.
		/// </summary>
		public static bool IsMatch(string ip, params string[] ipWildcards)
		{
			var ips = string.Join(",", ipWildcards);
			var list = ips.Split(';', ',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToArray();
			foreach (string item in list)
			{
				// Convert wild-card pattern to regular expression.
				var rxString = Regex
					.Escape(item)
					.Replace("\\*", ".*")
					.Replace("\\?", ".");
				var rx = new Regex("^" + rxString + "$");
				var match = rx.Match(ip);
				if (match.Success)
					return true;
			}
			return false;
		}

		public static bool IsNetworkAvailable(string excludeIps = null)
		{
			// If no adapters available then return false;
			if (!NetworkInterface.GetIsNetworkAvailable())
				return false;
			var nics = NetworkInterface.GetAllNetworkInterfaces();
			var badTypes = new NetworkInterfaceType[] { NetworkInterfaceType.Tunnel, NetworkInterfaceType.Loopback };
			var goodFamily = new AddressFamily[] { AddressFamily.InterNetwork, AddressFamily.InterNetworkV6 };
			foreach (var nic in nics)
			{
				// If network interface is not up then skip.
				if (nic.OperationalStatus != OperationalStatus.Up)
					continue;
				// If interface type is invalid then skip.
				if (badTypes.Contains(nic.NetworkInterfaceType))
					continue;
				// Get IP4 and IP6 statistics.
				//var stats = nic.GetIPStatistics();
				// If no data send or received then skip.
				//if (stats.BytesSent == 0 || stats.BytesReceived == 0)
				//	continue;
				// Loop trough IP address properties.
				var properties = nic.GetIPProperties();
				for (var a = 0; a < properties.UnicastAddresses.Count; a++)
				{
					var address = properties.UnicastAddresses[a].Address;
					// If not IP4 or IP6 then continue.
					if (!goodFamily.Contains(address.AddressFamily))
						continue;
					// If loop back then continue.
					if (IPAddress.IsLoopback(address))
						continue;
					// If excluded IP then continue.
					if (excludeIps != null && IsMatch(address.ToString(), excludeIps))
						continue;
					// Address passed availability.
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// This method is time consuming and would freeze application if you run it on main thread.
		/// Returns IP4 only.
		/// </summary>
		public static NetworkInfo CheckNetwork(string excludeIps = null, bool includeAutomaticPrivateAddress = false)
		{
			var info = new NetworkInfo();
			var nicsList = new List<String>();
			var nics = NetworkInterface.GetAllNetworkInterfaces();
			var ips = new List<KeyValuePair<string, int>>();
			var badTypes = new NetworkInterfaceType[] { NetworkInterfaceType.Tunnel, NetworkInterfaceType.Loopback };
			for (int i = 0; i < nics.Length; i++)
			{
				var ni = nics[i];
				if (badTypes.Contains(ni.NetworkInterfaceType))
					continue;
				var nicsSb = new StringBuilder();
				nicsSb.AppendFormat("    Name = {0}, Status = {1}", ni.Name, ni.OperationalStatus);
				var sb = new StringBuilder();
				var properties = ni.GetIPProperties();
				FillAdapter(sb, ni);
				info.NicInfo.Add(new KeyValue(ni.Description, sb.ToString()));
				if (ni.OperationalStatus == OperationalStatus.Up)
				{
					var desc = ni.Description;
					// If state is still off then...
					if (!info.IsMobileNicUp)
						info.IsMobileNicUp = desc.Contains("HSPA Network");
					// If state is still off then...
					if (!info.IsWirelessNicUp)
						info.IsWirelessNicUp = desc.Contains("802.11") || desc.Contains("802.11");
					// Loop trough IP address properties.
					for (var a = 0; a < properties.UnicastAddresses.Count; a++)
					{
						var address = properties.UnicastAddresses[a].Address;
						// If not IP4 version then skip.
						if (address.AddressFamily != AddressFamily.InterNetwork)
							continue;
						// Exclude Automatic Private IP Address range (169.254.*.*).
						var addressBytes = address.GetAddressBytes();
						if (!includeAutomaticPrivateAddress && addressBytes[0] == 169 && addressBytes[1] == 254)
							continue;
						// If loop-back then skip.
						if (IPAddress.IsLoopback(address))
							continue;
						nicsSb.AppendFormat(", Address = {0}", address);
						// If excluded then skip.
						if (excludeIps != null && IsMatch(address.ToString(), excludeIps))
							continue;
						// Normal IP4 address was found.
						info.IsNetworkAvailable = true;
						// More configuration = higher priority of IP address.
						var priority =
							properties.GatewayAddresses.Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork).Count() +
							properties.DnsAddresses.Where(x => x.AddressFamily == AddressFamily.InterNetwork).Count() +
							properties.DhcpServerAddresses.Where(x => x.AddressFamily == AddressFamily.InterNetwork).Count() +
							properties.WinsServersAddresses.Where(x => x.AddressFamily == AddressFamily.InterNetwork).Count();
						ips.Add(new KeyValuePair<string, int>(address.ToString(), priority));
					}
				}
				nicsList.Add(nicsSb.ToString());
			}
			info.CurrentNicks = string.Join("\r\n", nicsList);
			var ipAddress = ips.OrderByDescending(x => x.Value).Select(x => x.Key).FirstOrDefault();
			if (ipAddress != null) info.LocalIpAddress = ipAddress;
			return info;
		}

		static void FillAdapter(StringBuilder sb, NetworkInterface adapter)
		{
			var properties = adapter.GetIPProperties();
			var ph = adapter.GetPhysicalAddress().GetAddressBytes().Select(x => x.ToString("X2")).ToArray();
			sb.AppendFormat("  Interface Type. . . . . . . . . . . . . . : {0}\r\n", adapter.NetworkInterfaceType);
			sb.AppendFormat("  Operational Status. . . . . . . . . . . . : {0}\r\n", adapter.OperationalStatus);
			sb.AppendFormat("  Physical Address. . . . . . . . . . . . . : {0}\r\n", string.Join("-", ph));
			FillIPAddresses(sb, properties, AddressFamily.InterNetwork);
			// The following information is not useful for loop-back adapters.
			if (adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback)
			{
				sb.AppendFormat("  DNS Suffix. . . . . . . . . . . . . . . . : {0}", properties.DnsSuffix);
			}
		}

		static void FillIPAddresses(StringBuilder sb, IPInterfaceProperties addresses, AddressFamily family)
		{
			var list = addresses.UnicastAddresses.Where(x => family == AddressFamily.Unspecified || x.Address.AddressFamily == family).ToArray();
			for (int i = 0; i <= list.Count() - 1; i++)
			{
				if (family == AddressFamily.InterNetwork)
				{
					sb.AppendFormat("  IPv4 Address. . . . . . . . . . . . . . . : {0}\r\n", list[i].Address);
					sb.AppendFormat("  Subnet Mask . . . . . . . . . . . . . . . : {0}\r\n", list[i].IPv4Mask);
				}
				else if (family == AddressFamily.InterNetworkV6)
				{
					sb.AppendFormat("  IPv6 Address. . . . . . . . . . . . . . . : {0}\r\n", list[i].Address);
				}
			}
		}

		#endregion


	}

}
