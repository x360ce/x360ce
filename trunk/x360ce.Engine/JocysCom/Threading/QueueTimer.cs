using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Timers;

namespace JocysCom.ClassLibrary.Threading
{

	/// <summary>
	/// Queue tasks for execution on a single thread.
	/// </summary>
	public partial class QueueTimer : IDisposable
	{

		List<object> queue;
		object queueLock;
		object delayedItem;
		object delayedItemLock;
		object sleepTimerLock;
		object delayTimerLock;
		string LastException;
		DateTime LastExceptionDate;
		long ExceptionCount;
		Stopwatch LastAddTime = new Stopwatch();
		long DoActionCount;
		public Action<object> DoAction;
		public ISynchronizeInvoke SynchronizingObject { get; set; }

		bool _isRunning;
		bool isRunning
		{
			get { return _isRunning; }
			set
			{
				lock (sleepTimerLock)
				{
					_isRunning = value;
					// If timer is used then...
					if (SleepTimer != null)
					{
						// If thread running then...
						if (_isRunning)
						{
							// Stop timer.
							SleepTimer.Stop();
						}
						else
						{
							// Start sleep timer.
							SleepTimer.Start();
						}
					}
				}
			}
		}

		/// <summary>
		/// Controls how long application must sleep if last action finished without doing anything.
		/// </summary>
		public Timer SleepTimer;

		/// <summary>
		/// Controls how long applicaiton must wait between actions.
		/// </summary>
		public Timer DelayTimer;


		public QueueTimer()
		{
			// Default delay interval = 500 miliseconds.
			// Default sleep interval = 5 seconds.
			InitTimers(500, 5000);
		}

		/// <summary>
		/// Initialize new QueryTimer object.
		/// </summary>
		/// <param name="delayInterval">Delay time between each run. If this value is set then some items won't be added to the queue, in order to avoid clogging.</param>
		/// <param name="sleepInterval">If set then action will autorun automatically after specified ammount of milliseconds.</param>
		public QueueTimer(int delayInterval, int sleepInterval)
		{
			InitTimers(delayInterval, sleepInterval);
		}

		void InitTimers(int delayInterval, int sleepInterval)
		{
			// Crete main properties
			queue = new List<object>();
			queueLock = new object();
			delayedItemLock = new object();
			sleepTimerLock = new object();
			delayTimerLock = new object();
			// Create delay timer.
			if (delayInterval > 0)
			{
				DelayTimer = new Timer();
				DelayTimer.AutoReset = false;
				DelayTimer.Interval = delayInterval;
				DelayTimer.Elapsed += DelayTimer_Elapsed;
			}
			// Create sleep timer.
			if (sleepInterval > 0)
			{
				SleepTimer = new Timer();
				SleepTimer.AutoReset = false;
				SleepTimer.Interval = sleepInterval;
				SleepTimer.Elapsed += SleepTimer_Elapsed;
			}
		}

		public string DoActionNow()
		{
			return AddToQueue(new object());
		}

		public string AddToQueue(object item)
		{
			string status = "";
			lock (queueLock)
			{
				if (IsDisposing) return status;
				// If there is no delay between actions then...
				if (DelayTimer == null)
				{
					// Add item to the queue instantly.
					_AddToQueue(item);
					status = "Queue item added. // ";
				}
				else
				{
					// Get delay which is required.
					long delayTime = Math.Max(0, (long)DelayTimer.Interval - LastAddTime.ElapsedMilliseconds);
					// If item is not set then...
					if (delayedItem == null)
					{
						// Id delay is needed then...
						if (delayTime > 0)
						{
							delayedItem = item;
							DelayTimer.Start();
							status = "Delayed item updated. Delay timer started.";
						}
						else
						{
							// If queue is empty then...
							if (queue.Count == 0)
							{
								// Add item to the queue instantly.
								_AddToQueue(item);
								status = "Queue item added.";
							}
							else
							{
								// Queue already contains message.
								// Update message.
								queue[0] = item;
								status = "Queue item updated. Action thread is running.";
							}
						}
					}
					else
					{
						delayedItem = item;
						status = "Delay timer is running. Delayed item updated.";
					}
					status += string.Format(" // DelayTime = {0}, ", delayTime);
				}
			}
			status += string.Format("DoActionCount = {0}, queue[{1}], isRunning = {2}", DoActionCount, queue.Count, isRunning);
			if (ExceptionCount > 0)
			{
				status += string.Format(", ExceptionCount = {0}", ExceptionCount);
				if (LastExceptionDate.Ticks > 0)
				{
					status += string.Format("LastException = {0}", LastException);
					// 
					if (DateTime.Now.Subtract(LastExceptionDate).TotalSeconds > 10) LastExceptionDate = new DateTime();
				}
			}
			return status;
		}

		void _AddToQueue(object item)
		{
			queue.Add(item);
			LastAddTime.Reset();
			LastAddTime.Start();
			// If thread is not running then...
			if (!isRunning)
			{
				if (IsDisposing) return;
				isRunning = true;
				// Put into another variable for thread safety.
				ISynchronizeInvoke so = SynchronizingObject;
				if (so == null)
				{
					// Start new thread.
					// The thread pool job is to share and recycle threads.
					// It allows to avoid losing a few millisecond every time we need to create a new thread.
					System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(ThreadActionWithState));
				}
				else
				{
					if (so.InvokeRequired)
					{
						try
						{
							so.BeginInvoke((ThreadActionDelegate)ThreadAction, new object[0]);
						}
						catch (Exception)
						{
							queue.Remove(item);
							throw;
						}
					}
					else
					{
						ThreadAction();
					}

				}
			}
		}

		public delegate void ThreadActionDelegate();

		void DelayTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			lock (queueLock)
			{
				object item = delayedItem;
				delayedItem = null;
				// Ad item to queue instantly.
				_AddToQueue(item);
			}
		}

		void ThreadActionWithState(object state)
		{
			ThreadAction();
		}

		void ThreadAction()
		{
			object item = null;
			while (true)
			{
				lock (queueLock)
				{
					// If no arguments left then leave the loop.
					if (queue.Count == 0 || IsDisposing)
					{
						// Mark thread as not running;
						isRunning = false;
						return;
					}
					item = queue[0];
					queue.RemoveAt(0);
				}
				// Do synchronous action.
				DoActionCount++;
				try
				{
					DoAction(item);
				}
				catch (Exception ex)
				{
					LastException = ex.ToString();
					LastExceptionDate = DateTime.Now;
					ExceptionCount++;
				}
			}
		}

		void SleepTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			lock (sleepTimerLock)
			{
				if (IsDisposing) return;
				// If thread is not running then add task.
				// Use empty object as default task.
				if (!_isRunning) AddToQueue(new object());
			}
		}

		#region IDisposable

		public bool IsDisposing;

		// Dispose() calls Dispose(true)
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		//// NOTE: Leave out the finalizer altogether if this class doesn't 
		//// own unmanaged resources itself, but leave the other methods
		//// exactly as they are. 
		//~ModemDialer()
		//{
		//    // Finalizer calls Dispose(false)
		//    Dispose(false);
		//}

		// The bulk of the clean-up code is implemented in Dispose(bool)
		protected virtual void Dispose(bool disposing)
		{
			IsDisposing = true;
			if (disposing)
			{
				// Dispose timers first
				lock (sleepTimerLock)
				{
					if (SleepTimer != null)
					{
						SleepTimer.Dispose();
						SleepTimer = null;
					}
				}
				lock (queueLock)
				{
					if (DelayTimer != null)
					{
						DelayTimer.Dispose();
						DelayTimer = null;
					}
				}
			}
		}

		#endregion


	}
}
