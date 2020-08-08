using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;

namespace JocysCom.ClassLibrary.Network.Sockets
{
	/// <summary>
	/// Important: If DataHolder is inside SendQueue then properties can't be modified unless inside SendQueueLock.
	/// </summary>
	public class DataHolder : INotifyPropertyChanged, INotifyPropertyChanging
	{

		public DataHolder()
		{
			_Available = true;
		}

		IPEndPoint _OriginalRemoteEndpoint;
		/// <summary>
		/// Original remote endpoint of the client connecting.
		/// </summary>
		public IPEndPoint OriginalRemoteEndpoint
		{
			get { return _OriginalRemoteEndpoint; }
			set { OnPropertyChanging(); _OriginalRemoteEndpoint = value; OnPropertyChanged(); }
		}

		IPEndPoint _DeliveryRemoteEndpoint;
		/// <summary>
		/// Override remote messages delivery IP address and port number.
		/// </summary>
		public IPEndPoint DeliveryRemoteEndpoint
		{
			get { return _DeliveryRemoteEndpoint; }
			set { OnPropertyChanging(); _DeliveryRemoteEndpoint = value; OnPropertyChanged(); }
		}

		public void IncreaseSendCount()
		{
			OnPropertyChanging(nameof(SendDate));
			_SendDate = DateTime.Now;
			OnPropertyChanged(nameof(SendDate));
			OnPropertyChanging(nameof(SendCount));
			_SendCount += 1;
			OnPropertyChanged(nameof(SendCount));
		}

		/// <summary>
		/// Can only by updated by attempt to send message
		/// </summary>
		public int SendCount { get { return _SendCount; } }
		int _SendCount;

		public DateTime SendDate { get { return _SendDate; } }
		DateTime _SendDate;

		/// <summary>
		/// DataHolder is used by SocketAsyncEventArgs
		/// </summary>
		public bool _Available;
		public bool Available
		{
			get { return _Available; }
			set { OnPropertyChanging(); _Available = value; OnPropertyChanged(); }
		}

		DateTime _ReceivedDate;
		public DateTime ReceivedDate
		{
			get { return _ReceivedDate; }
			set { OnPropertyChanging(); _ReceivedDate = value; OnPropertyChanged(); }
		}

		Exception _Exception;
		public Exception Exception
		{
			get { return _Exception; }
			set { OnPropertyChanging(); _Exception = value; OnPropertyChanged(); }
		}

		/// <summary>
		/// Will be set automatically by socket server when message is complete.
		/// </summary>
		public byte[] ReceivedMessageBytes;

		/// <summary>
		/// Must be set by the user inside Process Data.
		/// </summary>
		public ISocketMessage ReceivedMessage;
		public ISocketMessage MessageToSend;

		public List<ISocketMessage> ExtraResponses { get; } = new List<ISocketMessage>();

		#region INotifyPropertyChanging

		public event PropertyChangingEventHandler PropertyChanging;

		internal void OnPropertyChanging([CallerMemberName] string propertyName = null)
		{
			PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
		}

		#endregion

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion

	}
}
