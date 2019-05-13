using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

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
			set { NotifyPropertyChanging("OriginalRemoteEndpoint"); _OriginalRemoteEndpoint = value; NotifyPropertyChanged("OriginalRemoteEndpoint"); }
		}

		IPEndPoint _DeliveryRemoteEndpoint;
		/// <summary>
		/// Override remote messages delivery IP address and port number.
		/// </summary>
		public IPEndPoint DeliveryRemoteEndpoint
		{
			get { return _DeliveryRemoteEndpoint; }
			set { NotifyPropertyChanging("DeliveryRemoteEndpoint"); _DeliveryRemoteEndpoint = value; NotifyPropertyChanged("DeliveryRemoteEndpoint"); }
		}

		public void IncreaseSendCount()
		{
			NotifyPropertyChanging("SendDate");
			_SendDate = DateTime.Now;
			NotifyPropertyChanged("SendDate");
			NotifyPropertyChanging("SendCount");
			_SendCount += 1;
			NotifyPropertyChanged("SendCount");
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
			set { NotifyPropertyChanging("Available"); _Available = value; NotifyPropertyChanged("Available"); }
		}

		DateTime _ReceivedDate;
		public DateTime ReceivedDate
		{
			get { return _ReceivedDate; }
			set { NotifyPropertyChanging("ReceivedDate"); _ReceivedDate = value; NotifyPropertyChanged("ReceivedDate"); }
		}

		Exception _Exception;
		public Exception Exception
		{
			get { return _Exception; }
			set { NotifyPropertyChanging("Exception"); _Exception = value; NotifyPropertyChanged("Exception"); }
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

		List<ISocketMessage> _ExtraResponses = new List<ISocketMessage>();
		public List<ISocketMessage> ExtraResponses { get { return _ExtraResponses; } }

		#region INotifyPropertyChanging

		public event PropertyChangingEventHandler PropertyChanging;

		void NotifyPropertyChanging(string propertyName)
		{
			var ev = PropertyChanging;
			if (ev == null) return;
			ev(this, new PropertyChangingEventArgs(propertyName));
		}

		#endregion

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		void NotifyPropertyChanged(string propertyName)
		{
			var ev = PropertyChanged;
			if (ev == null) return;
			ev(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion

	}
}
