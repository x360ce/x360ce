using JocysCom.ClassLibrary;
using System;

namespace JocysCom.ClassLibrary.Threading
{
	public class QueueTimerEventArgs : EventArgs
	{

		/// <summary>
		/// Current item to process.
		/// </summary>
		public object Item { get; set; }

		/// <summary>
		/// Keep item in the queue after DoWorker completes.
		/// </summary>
		public bool Keep { get; set; }

		/// <summary>
		/// Exit from Thread wihch runs DoWork items on the Queue.
		/// </summary>
		public bool Cancel { get; set; }

	}
}
