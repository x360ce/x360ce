using JocysCom.ClassLibrary.ComponentModel;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace JocysCom.ClassLibrary.Threading
{

	/// <summary>
	/// Queue tasks for execution on a single thread in a synchronized order.
	/// </summary>
	public partial class QueueTimer<T> : QueueTimerSimple<T> where T : class
	{
		public QueueTimer(int delayInterval = 500, int sleepInterval = 5000, TaskScheduler listSynchronizingObject = null) : base(delayInterval, sleepInterval)
		{
			// Create main properties.
			Queue = new BindingListInvoked<T>();
			Queue.SynchronizingObject = listSynchronizingObject;
			SynchronizingObject = listSynchronizingObject;
		}

		public TaskScheduler SynchronizingObject { get; set; }

		public new BindingListInvoked<T> Queue { get { return (BindingListInvoked<T>)base.Queue; } set { base.Queue = value; } }

		public override string DoActionNow(T item = null)
		{
			var so = SynchronizingObject;
			if (so is null)
				// Run on current thread.
				return _DoActionNow(item);
			// Run on synchronizing object thread.
			var t = new Task<string>(() => _DoActionNow(item));
			t.RunSynchronously(so);
			return t.Result;
		}

		protected override void _StarThread()
		{
			if (IsDisposing)
				return;
			StartCount++;
			// If thread is not running and queue contains items to process then...
			if (!IsRunning)
			{
				SleepTimerStop();
				// Put into another variable for thread safety.
				var so = SynchronizingObject;
				if (so is null)
				{
					// Mark thread as running.
					IsRunning = true;
					// Start new thread.
					if (UseThreadPool)
					{
						// The thread pool job is to share and recycle threads.
						// It allows to avoid losing a few millisecond every time we need to create a new thread.
						ThreadPool.QueueUserWorkItem(ThreadAction, null);
					}
					else
					{
						// Perform check on a separate thread, because checking can take a while.
						_ThreadStart = new ParameterizedThreadStart(ThreadAction);
						_Thread = new Thread(_ThreadStart);
						_Thread.IsBackground = true;
						_Thread.Start();
					}
				}
				else if (!HasHandle)
				{
					// BeginInvoke will fail. Silently clear the queue.
					if (Queue.Count > 0)
						Queue.Clear();
				}
				else
				{
					try
					{
						// Mark thread as running.
						IsRunning = true;
						// Use asynchronous call to avoid 'queueLock' deadlock.
						// If handle exception then, maybe you forgot to dispose QueueTimer before 'so'.
						Task.Factory.StartNew(() => { ThreadAction(null); },
							CancellationToken.None, TaskCreationOptions.DenyChildAttach, so);
					}
					catch (Exception)
					{
						// Silently clear the queue.
						if (Queue.Count > 0)
							Queue.Clear();
						throw;
					}
				}
			}
		}

	}

}
