using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Timers;

namespace JocysCom.ClassLibrary.Threading
{

	/// <summary>
	/// Queue tasks for execution on a single thread in a synchronized order.
	/// </summary>
	public partial class QueueTimer : IDisposable
	{

		/// <summary>
		/// Initialize new QueryTimer object. Default delay interval = 500 milliseconds. Default sleep interval = 5 seconds.
		/// </summary>
		/// <param name="delayInterval">Delay time between each run. If this value is set then some items won't be added to the queue, in order to avoid clogging.</param>
		/// <param name="sleepInterval">If set then action will auto-run automatically after specified amount of milliseconds.</param>
		public QueueTimer(int delayInterval = 500, int sleepInterval = 5000)
		{
			// Create main properties.
			queue = new List<object>();
			queueLock = new object();
			sleepTimerLock = new object();
			// Create delay timer.
			if (delayInterval > 0)
			{
				delayTimer = new Timer();
				delayTimer.AutoReset = false;
				delayTimer.Interval = delayInterval;
				delayTimer.Elapsed += DelayTimer_Elapsed;
			}
			// Create sleep timer.
			if (sleepInterval > 0)
			{
				sleepTimer = new Timer();
				sleepTimer.AutoReset = false;
				sleepTimer.Interval = sleepInterval;
				sleepTimer.Elapsed += SleepTimer_Elapsed;
			}
		}

		/// <summary>If delay timer is set then queue can contain only one item.</summary>
		List<object> queue;
		object queueLock;

		/// <summary>Last added item.</summary>
		object delayedItem;
		object sleepTimerLock;
		string lastException;
		DateTime lastExceptionDate;
		long exceptionCount;
		Stopwatch lastAddTime = new Stopwatch();
		long doActionCount;

		/// <summary>Thread action is running.</summary>
		public bool IsRunning { get { return _IsRunning; } }

		bool _IsRunning;

		/// <summary>
		/// Next run by sleep timer.
		/// </summary>
		public DateTime NextRunTime { get { return _NextRunTime; } }

		DateTime _NextRunTime;

		/// <summary>
		/// If SynchronizingObject is set then make sure that handle is created.
		/// var handle = control.Handle; // Creates handle if missing.
		/// var isCreated = control.IsHandleCreated;
		/// You can use 'HandleCreated' event.
		/// </summary>
		public Action<object> DoAction;

		public ISynchronizeInvoke SynchronizingObject { get; set; }

		/// <summary>
		/// Controls how long application must wait between actions.
		/// </summary>
		Timer delayTimer;

		/// <summary>
		/// Controls how long application must sleep if last action finished without doing anything.
		/// </summary>
		Timer sleepTimer;

		public void ChangeSleepInterval(int interval)
		{
			var t = sleepTimer;
			if (t != null)
			{
				t.Interval = interval;
				if (t.Enabled)
				{
					_NextRunTime = DateTime.Now.AddMilliseconds(t.Interval);
				}
			}
		}

		public string DoActionNow(object item = null)
		{
			if (item == null)
				item = new object();
			string status = "";
			var data = new List<string>();
			lock (queueLock)
			{
				if (IsDisposing) return status;
				// If there is no delay between actions then...
				if (delayTimer == null)
				{
					// Add item to the queue instantly.
					_AddToQueue(item);
					status = "Queue item added.";
				}
				else
				{
					// Get delay which is required.
					long delayTime = Math.Max(0, (long)delayTimer.Interval - lastAddTime.ElapsedMilliseconds);
					// If item is set already then...
					if (delayedItem != null)
					{
						delayedItem = item;
						status = "Delayed item updated. Delay timer is running.";
					}
					// If delay is needed then...
					else if (delayTime > 0)
					{
						delayedItem = item;
						delayTimer.Start();
						status = "Delayed item added. Delay timer started.";
					}
					// If queue is empty then...
					else if (queue.Count == 0)
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
					data.Add(string.Format("DelayTime = {0}", delayTime));
				}
			}
			data.Add(string.Format("DoActionCount = {0}", doActionCount));
			data.Add(string.Format("QueueCount = {0}", queue.Count));
			data.Add(string.Format("IsRunning = {0}", _IsRunning));
			if (exceptionCount > 0)
			{
				data.Add(string.Format("ExceptionCount = {0}", exceptionCount));
				if (lastExceptionDate.Ticks > 0)
				{
					data.Add(string.Format("LastException = {0}", lastException));
					if (DateTime.Now.Subtract(lastExceptionDate).TotalSeconds > 10) lastExceptionDate = new DateTime();
				}
			}
			return status + " // " + string.Join(", ", data);
		}

		/// <summary>
		/// This function will be called inside 'queueLock' lock.
		/// </summary>
		/// <remarks>http://blogs.msdn.com/b/jaredpar/archive/2008/01/07/isynchronizeinvoke-now.aspx</remarks>
		void _AddToQueue(object item)
		{
			lock (disposeLock)
			{
				if (IsDisposing) return;
				queue.Add(item);
				lastAddTime.Reset();
				lastAddTime.Start();
				// If thread is not running then...
				if (!_IsRunning)
				{
					lock (sleepTimerLock)
					{
						// Mark thread as running.
						_IsRunning = true;
						// If timer is used then...
						if (sleepTimer != null)
						{
							// Stop timer.
							sleepTimer.Stop();
							_NextRunTime = default(DateTime);
						}
					}
					// Put into another variable for thread safety.
					ISynchronizeInvoke so = SynchronizingObject;
					if (so == null)
					{
						// Start new thread.
						// The thread pool job is to share and recycle threads.
						// It allows to avoid losing a few millisecond every time we need to create a new thread.
						System.Threading.ThreadPool.QueueUserWorkItem(ThreadAction, null);
					}
					else
					{
						var process = Process.GetCurrentProcess();
						// If handle is missing then...
						if (process != null && process.Handle == IntPtr.Zero)
						{
							// BeginInvoke will fail. Silently remove the action.
							queue.Remove(item);
						}
						else
						{
							try
							{
								// Use asynchronous call to avoid 'queueLock' deadlock.
								var action = (System.Threading.WaitCallback)ThreadAction;
								// If handle exception then, maybe you forgot to dispose QueueTimer before 'so'.
								var ar = so.BeginInvoke(action, new object[] { null });
							}
							catch (Exception)
							{
								queue.Remove(item);
								throw;
							}
						}
					}
				}
			}
		}

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

		void ThreadAction(object state)
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
						lock (sleepTimerLock)
						{
							_IsRunning = false;
							// If timer is used then...
							if (sleepTimer != null)
							{
								// Start sleep timer.
								sleepTimer.Start();
								_NextRunTime = DateTime.Now.AddMilliseconds(sleepTimer.Interval);
							}
						}
						return;
					}
					item = queue[0];
					queue.RemoveAt(0);
				}
				// Do synchronous action.
				doActionCount++;
				var da = DoAction;
				if (da != null)
				{
					try
					{
						da(item);
					}
					catch (Exception ex)
					{
						lastException = ex.ToString();
						lastExceptionDate = DateTime.Now;
						exceptionCount++;
					}

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
				if (!_IsRunning) DoActionNow();
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


		object disposeLock = new object();


		// The bulk of the clean-up code is implemented in Dispose(bool)
		protected virtual void Dispose(bool disposing)
		{
			lock (disposeLock)
			{
				IsDisposing = true;
			}
			if (disposing)
			{
				// Dispose timers first
				lock (sleepTimerLock)
				{
					if (sleepTimer != null)
					{
						sleepTimer.Dispose();
						_NextRunTime = default(DateTime);
						sleepTimer = null;
					}
				}
				lock (queueLock)
				{
					if (delayTimer != null)
					{
						delayTimer.Dispose();
						delayTimer = null;
					}
					if (queue != null)
					{
						queue.Clear();
					}
				}
				// Make sure that outside objects are not holding this timer from disposal. 
				SynchronizingObject = null;
				DoAction = null;
			}
		}

		#endregion


	}
}
