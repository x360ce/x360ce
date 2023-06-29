using System;
using System.ComponentModel;
using System.Configuration;
using System.Net;
using System.Runtime.CompilerServices;

namespace JocysCom.ClassLibrary.Network.Sockets
{

	public class SocketSettings : INotifyPropertyChanged
	{

		public SocketSettings(string configPrefix)
		{
			_configPrefix = configPrefix;
			//<add key="Sockets_LogsDirectory" value="Logs"/>
			//<add key="Sockets_LogFlow" value="True"/>
			//<add key="Sockets_LogData" value="True"/>
			//<add key="Sockets_LogErrors" value="True"/>
			//<add key="Sockets_LogThreads" value="True"/>
			//<add key="Sockets_LogFileAutoFlush" value="True"/>
			//<add key="Sockets_LogConnectAndDisconnect" value="True"/>
			//<add key="Sockets_DelayAfterGettingMessage" value="-1"/>
			//<add key="Sockets_MaxSimultaneousConnections" value="1000"/>
			//<add key="Sockets_MaxSimultaneousTransmission" value="1000"/>
			//<add key="Sockets_MaxPendingConnections" value="100"/>
			//<add key="Sockets_BufferSize" value="65535"/>
			//<add key="Sockets_WatchConsole" value="False"/>
			//<add key="Sockets_MainSessionId" value="1000000000"/>
			//<add key="Sockets_ServerMainTransMissionId" value="10000"/>
			//<add key="Sockets_MessageHeadLength" value="7"/>
			//<add key="Sockets_ServerPort value="100000"/>
			//<add key="Sockets_ServerAddress value="2"/>
			// Socket Client settings.
			//<add key="Sockets_TotalClientConnectionsToRun value="100000"/>
			//<add key="Sockets_TotalClientConnectionsToRun value="2"/>
			//<add key="Sockets_PlaySoundOnCompletion value="true"/>
			//<add key="Sockets_ContinuallyRetryConnectIfSocketError value="true"/>
			//<add key="Sockets_DelayMillisecondsBetweenConnections value="0"/>
			//<add key="Sockets_ClientPortRangeStart value="0"/>
			//<add key="Sockets_UseServerPortForClient value="false"/>
			LogFlow = ParseBool("LogFlow", false);
			LogData = ParseBool("LogData", false);
			LogErrors = ParseBool("LogErrors", true);
			LogThreads = ParseBool("LogThreads", false);
			DelayAfterGettingMessage = ParseInt("DelayAfterGettingMessage", 0);
			MaxPendingConnections = ParseInt("MaxPendingConnections", 100);
			MaxSimultaneousOperations = ParseInt("MaxSimultaneousOperations", 1000);
			ClientPortRangeStart = ParseInt("ClientPortRangeStart", 0);
			UseServerPortForClients = ParseBool("UseServerPortForClients", false);
			BufferSize = ParseInt("BufferSize", ushort.MaxValue);
			MainSessionId = ParseInt("MainSessionId", 1000000000);
			ServerMainTransMissionId = ParseInt("ServerMainTransMissionId", 10000);
			MessageHeadLength = ParseInt("MessageHeadLength", 7);
			// Server/Local endpoint address.
			ServerPort = ParseInt("ServerPort", 4440);
			var sa = ParseString("ServerAddress", "");
			ServerAddress = (sa == "") ? IPAddress.Any : IPAddress.Parse(sa);
			SendTimeout = ParseInt("SendTimeout", 0);
			ReceiveTimeout = ParseInt("ReceiveTimeout", 0);
			// Default Remote endpoint address.
			DefaultRemotePort = ParseInt("DefaultRemotePort", 0);
			var ra = ParseString("DefaultRemoteAddress", "");
			IPAddress remoteAddress;
			if (IPAddress.TryParse(ra, out remoteAddress))
			{
				DefaultRemoteAddress = remoteAddress;
			}
			ClientConnectionsToRun = ParseInt("ClientConnectionsToRun", 10000);
			ClientMessagesPerConnection = ParseInt("ClientMessagesPerConnection", 2);
			StayConnected = ParseBool("StayConnected", true);
			PlaySoundOnCompletion = ParseBool("PlaySoundOnCompletion", true);
			ContinuallyRetryConnectIfSocketError = ParseBool("ContinuallyRetryConnectIfSocketError", true);
			DelayMillisecondsBetweenConnections = ParseInt("DelayMillisecondsBetweenConnections", 0);
			ReceiveDisconnectTimeout = ParseInt("ReceiveDisconnectTimeout", 0);
			ProtocolType = ParseEnum("ProtocolType", System.Net.Sockets.ProtocolType.Tcp);
			KeepAliveInterval = ParseInt("KeepAliveInterval", 15000);
			KeepAliveEnabled = ParseBool("KeepAliveEnabled", false);
			AutoRemoveFromSendQueue = ParseBool("AutoRemoveFromSendQueue", true);
		}

		public void EnableFullLogging()
		{
			LogFlow = true;
			LogThreads = true;
			LogErrors = true;
			LogData = true;
		}

		#region Parse Configuration Values

		string _configPrefix;
		public string ConfigPrefix { get { return _configPrefix; } }

		public bool ParseBool(string name, bool defaultValue)
		{
			var v = ConfigurationManager.AppSettings[_configPrefix + "_" + name];
			return (v is null) ? defaultValue : bool.Parse(v);
		}

		public int ParseInt(string name, int defaultValue)
		{
			var v = ConfigurationManager.AppSettings[_configPrefix + "_" + name];
			return (v is null) ? defaultValue : int.Parse(v);
		}

		public TimeSpan ParseSpan(string name, TimeSpan defaultValue)
		{
			var v = ConfigurationManager.AppSettings[_configPrefix + "_" + name];
			return (v is null) ? defaultValue : TimeSpan.Parse(v);
		}

		public T ParseEnum<T>(string name, T defaultValue)
		{
			var v = ConfigurationManager.AppSettings[_configPrefix + "_" + name];
			return (v is null) ? defaultValue : (T)Enum.Parse(typeof(T), v);
		}

		public string ParseString(string name, string defaultValue)
		{
			var v = ConfigurationManager.AppSettings[_configPrefix + "_" + name];
			return (v is null) ? defaultValue : v;
		}

		#endregion

		public int DelayAfterGettingMessage { get; set; }
		public bool LogFlow { get; set; }
		public bool LogData { get; set; }
		public bool LogErrors { get; set; }
		public bool LogThreads { get; set; }
		public int MainSessionId { get; set; }
		public int ServerMainTransMissionId { get; set; }
		public int ClientConnectionsToRun { get; set; }
		public int ClientMessagesPerConnection { get; set; }
		public bool PlaySoundOnCompletion { get; set; }
		public bool ContinuallyRetryConnectIfSocketError { get; set; }
		public int DelayMillisecondsBetweenConnections { get; set; }

		/// <summary>
		/// This number must be the same as the value on the client.
		/// Tells what size the message head will be.
		/// </summary>
		public int MessageHeadLength { get; set; }

		/// <summary>Server IP address.</summary>
		public IPAddress ServerAddress { get; set; }

		/// <summary>Server port.</summary>
		[DefaultValue(4440)]
		public int ServerPort { get; set; }

		/// <summary>Default remote IP address.</summary>
		public IPAddress DefaultRemoteAddress { get; set; }

		/// <summary>Default remote port.</summary>
		[DefaultValue(4441)]
		public int DefaultRemotePort { get; set; }

		/// <summary>Operation idle time after which client will be disconnected.</summary>
		[DefaultValue(0)]
		public int ReceiveDisconnectTimeout { get; set; }

		/// <summary>
		/// Amount of time after which a synchronous Receive call will time out.
		/// The time-out value, in milliseconds. The default value is 0, which indicates an infinite time-out period.
		/// </summary>
		[DefaultValue(0)]
		public int ReceiveTimeout { get; set; }

		/// <summary>
		/// Amount of time after which a synchronous Send call will time out.
		/// The time-out value, in milliseconds. The default value is 0, which indicates an infinite time-out period.
		/// </summary>
		[DefaultValue(0)]
		public int SendTimeout { get; set; }

		/// <summary>
		/// You would want a buffer size equal to a maximum message size.
		/// </summary>
		[DefaultValue(ushort.MaxValue)]
		public int BufferSize { get; set; }

		/// <summary>
		/// This variable determines the number of 
		/// SocketAsyncEventArg objects put in the pool of objects
		/// </summary>
		[DefaultValue(1000)]
		public int MaxSimultaneousOperations { get; set; }

		/// <summary>
		/// Client port range start.
		/// Maximum number of ports used depends on MaxSimultaneousOperations.
		/// If value is set to 0 then random ports will be used.
		/// </summary>
		[DefaultValue(0)]
		public int ClientPortRangeStart { get; set; }

		/// <summary>
		/// Use Server port for Clients in UDP.
		/// </summary>
		[DefaultValue(false)]
		public bool UseServerPortForClients { get; set; }

		/// <summary>
		/// Auto remove from send queue.
		/// </summary>
		[DefaultValue(true)]
		public bool AutoRemoveFromSendQueue { get; set; }

		/// <summary>
		/// Server Only: Maximum number of pending incoming connections the listener can hold in queue.
		/// </summary>
		[DefaultValue(100)]
		public int MaxPendingConnections { get; set; }

		/// <summary>Socket Protocol type. Default: TCP.</summary>
		[DefaultValue(System.Net.Sockets.ProtocolType.Tcp)]
		public System.Net.Sockets.ProtocolType ProtocolType { get; set; }

		/// <summary>Stay connected to remote host when using TCP/IP connection.</summary>
		/// <remarks>
		/// Windows will reserve port in unusable TIME_WAIT state for 120 seconds for side which starts disconnection first.
		/// This lead to the issue when there are no available ports if messages are too frequent.
		/// In order to solve this SocketServer won't try to close connections.
		/// Socket Server will look for connected sockets, which completed all send/receive operations, first.</remarks>
		[DefaultValue(true)]
		public bool StayConnected { get; set; }

		/// <summary>
		/// Set interval between empty UDP packets which will be sent to server.
		/// This is required to maintain "UDP hole punching" mapping on NAT servers.
		/// </summary>
		public int KeepAliveInterval { get { return _KeepAliveInterval; } set { _KeepAliveInterval = value; OnPropertyChanged(); } }
		int _KeepAliveInterval;

		/// <summary>
		/// Enable sending of UDP packets at specified interval to the server.
		/// This is required to maintain "UDP hole punching" mapping on NAT servers.
		/// </summary>
		public bool KeepAliveEnabled { get { return _KeepAliveEnabled; } set { _KeepAliveEnabled = value; OnPropertyChanged(); } }
		bool _KeepAliveEnabled;

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion



	}
}
