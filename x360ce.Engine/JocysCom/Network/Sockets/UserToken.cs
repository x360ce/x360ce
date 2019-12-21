using System;

namespace JocysCom.ClassLibrary.Network.Sockets
{
	/// <summary>
	/// Remember, if a socket uses a byte array for its buffer, that byte array is
	/// unmanaged in .NET and can cause memory fragmentation. So, first write to the
	/// buffer block used by the SAEA object. Then, you can copy that data to another
	/// byte array, if you need to keep it or work on it, and want to be able to put
	/// the SAEA object back in the pool quickly, or continue with the data 
	/// transmission quickly.         
	/// </summary>
	/// <remarks>
	/// The only reason to use this UserToken in application is to give it an identifier,
	/// so that you can see it in the program flow. Otherwise, you would not need it.
	/// </remarks>
	public class UserToken
	{

		/// <summary>
		/// Declare for send and receive operations.
		/// </summary>
		public UserToken(int bufferOffset, int tokenId)
		{
			_TokenId = tokenId;
			_BufferOffset = bufferOffset;
			BufferOffsetCurrent = BufferOffset;
		}

		/// <summary>Unique ID used for tracing and logging.</summary>
		public int TokenId { get { return _TokenId; } }
		int _TokenId;

		/// <summary>Position where receive/send message begins in the buffer</summary>
		public int BufferOffset { get { return _BufferOffset; } }
		int _BufferOffset;

		/// <summary>Current position in the buffer which separates processed and unprocessed data.</summary>
		public int BufferOffsetCurrent;
		/// <summary>Number of unprocessed bytes for Head and Body.</summary>
		public int ReceivedBytesToProcess;

		public int ClientPort { get; set; }

		/// <summary>Unique ID used for tracing and logging sessions.</summary>
		public int SessionId { get; set; }

		///<summary>Head and Body to send.</summary>
		public byte[] MessageToSendBytes;
		public int MessageBytesToProcess;
		public int MessageBytesProcessed;

		///<summary>Head and Body bytes of incoming message</summary>
		public byte[] MessageBytes;
		///<summary>Number of Head bytes which were processed already.</summary>
		public int HeadBytesProcessed;
		///<summary>Number of Body bytes which were processed already.</summary>
		public int BodyBytesProcessed;
		/// <summary>Length of the body set inside ProcessHeader method.</summary>
		public int BodyBytesLength;

		/// <summary>Head processing error.</summary>
		public Exception HeadProcessError;

		/// <summary>Date of last Connect, Accept, Send or Receive Operation</summary>
		public DateTime? LastOperationDate;

		/// <summary>Used for outgoing and incoming messages.</summary>
		public DataHolder DataHolder;

		/// <summary>Copies one output box message into MessageToSend array.</summary>
		/// <returns>message in the queue</returns>
		public void PrepareMessageToSend()
		{
			// Increase send count so programs can check
			// how many attempts were made to send the data inside the holder.
			DataHolder.IncreaseSendCount();
			var message = DataHolder.MessageToSend;
			MessageToSendBytes = message.ToBytes();
			MessageBytesToProcess = MessageToSendBytes.Length;
			MessageBytesProcessed = 0;
		}

		public void PrepareNextMessageToReceive()
		{
			MessageBytes = null;
			HeadBytesProcessed = 0;
			BodyBytesProcessed = 0;
		}

		/// <summary>
		/// Remove data holder from token and reset properties to their default values.
		/// </summary>
		/// <remarks>
		/// This method is called before pushing token into list of free EventArgs.
		/// </remarks>
		public void Cleanup()
		{
			BufferOffsetCurrent = BufferOffset;
			ReceivedBytesToProcess = 0;
			MessageToSendBytes = null;
			MessageBytesToProcess = 0;
			MessageBytesProcessed = 0;
			MessageBytes = null;
			HeadBytesProcessed = 0;
			BodyBytesProcessed = 0;
			BodyBytesLength = 0;
			HeadProcessError = null;
			LastOperationDate = null;
			DataHolder = null;
		}
	}
}