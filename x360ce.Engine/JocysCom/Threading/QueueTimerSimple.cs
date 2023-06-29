using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Timers;

namespace JocysCom.ClassLibrary.Threading
{

	public partial class QueueTimer : QueueTimerSimple<object>
	{
		public QueueTimer(int delayInterval = 500, int sleepInterval = 5000)
			: base(delayInterval, sleepInterval)
		{
		}

		public override string DoActionNow(object item = null)
		{
			return base.DoActionNow(item ?? new object());
		}

	}

	/// <summary>
	/// Queue tasks for execution on a single thread in a synchronized order.
	/// </summary>
	public partial class QueueTimerSimple<T> : IDisposable where T : class
	{

		/// <summary>
		/// Initialize new QueryTimer object. Default delay interval = 500 milliseconds. Default sleep interval = 5 seconds.
		/// </summary>
		/// <param name="delayInterval">Delay time between each run. If this value is set then some items won't be added to the queue, in order to avoid clogging.</param>
		/// <param name="sleepInterval">If set then action will auto-run automatically after specified amount of milliseconds.</param>
		public QueueTimerSimple(int delayInterval = 500, int sleepInterval = 5000)
		{
			Queue = new List<T>();
			_LastActionDoneTime = new Stopwatch();
			_LastActionDoneTime.Start();
			ChangeDelayInterval(delayInterval);
			ChangeSleepInterval(sleepInterval);
			SleepTimerStart();
		}

		/// <summary>If delay timer is set then queue can contain only one item.</summary>
		public IList<T> Queue { get; set; }

		private readonly object queueLock = new object();

		public bool ProcessImmediately = false;

		/// <summary>Last added item.</summary>
		private string lastException;
		private DateTime lastExceptionDate;
		private long exceptionCount;

		/// <summary>
		/// If SynchronizingObject is set then make sure that handle is created.
		/// var handle = control.Handle; // Creates handle if missing.
		/// var isCreated = control.IsHandleCreated;
		/// You can use 'HandleCreated' event.
		/// </summary>
		public EventHandler<QueueTimerEventArgs> DoWork;

		public event EventHandler<QueueTimerEventArgs> BeforeRemove;

		#region Synchronizing Object

		// var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
		// timer = new QueueTimer(520, 4000, scheduler);
		// HandleCreated += (sender, e) => { timer.HasHandle = true; };
		// HandleDestroyed += (sender, e) => { timer.HasHandle = false; };
		// timer.HasHandle = IsHandleCreated;
		public bool HasHandle;

		#endregion

		#region Status

		public long AddCount { get; protected set; }

		public long StartCount { get; protected set; }

		public long ThreadCount { get; protected set; }

		public long ActionCount { get; protected set; }

		public long ActionNoneCount { get; protected set; }

		public TimeSpan LastActionDoneTime => _LastActionDoneTime.Elapsed;

		private readonly Stopwatch _LastActionDoneTime = new Stopwatch();

		/// <summary>Thread action is running.</summary>
		public bool IsRunning { get; protected set; }

		#endregion

		/// <summary>
		/// Next run by sleep timer.
		/// </summary>
		public DateTime NextRunTime => (delayTimerNextRunTime.Ticks == 0 || (sleepTimerNextRunTime.Ticks > 0 && sleepTimerNextRunTime < delayTimerNextRunTime))
					? sleepTimerNextRunTime
					: delayTimerNextRunTime;

		#region Delay Timer

		/// <summary>
		/// Controls how long application must wait between actions.
		/// </summary>
		private System.Timers.Timer delayTimer;
		private DateTime delayTimerNextRunTime;
		private readonly object delayTimerLock = new object();

		public void ChangeDelayInterval(int interval)
		{
			lock (delayTimerLock)
			{
				// If delay timer exist then...
				if (delayTimer != null)
				{
					// Dispose delay timer.
					delayTimer.Elapsed -= DelayTimer_Elapsed;
					delayTimer.Dispose();
					delayTimerNextRunTime = default(DateTime);
					delayTimer = null;
				}
				// If interval is set then...
				if (interval > 0)
				{
					// Create delay timer.
					delayTimer = new System.Timers.Timer();
					delayTimer.AutoReset = false;
					delayTimer.Interval = interval;
					delayTimer.Elapsed += DelayTimer_Elapsed;
				}
			}
		}

		public void DelayTimerStop()
		{
			lock (delayTimerLock)
			{
				if (delayTimer != null)
				{
					delayTimer.Stop();
					delayTimerNextRunTime = default(DateTime);
				}
			}
		}

		public void DelayTimerStart()
		{
			lock (delayTimerLock)
			{
				if (delayTimer is null)
					return;
				delayTimerNextRunTime = DateTime.Now.AddMilliseconds(delayTimer.Interval);
				delayTimer.Start();
			}
		}

		private void DelayTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			lock (delayTimerLock)
			{
				// Clear delay time, because action will start now.
				delayTimerNextRunTime = default(DateTime);
			}
			lock (queueLock)
			{
				if (IsDisposing)
					return;
				_StarThread();
			}
		}

		#endregion

		#region Sleep Timer

		/// <summary>
		/// Controls how long application must sleep if last action finished without doing anything.
		/// </summary>
		private System.Timers.Timer sleepTimer;
		private DateTime sleepTimerNextRunTime;
		private readonly object sleepTimerLock = new object();

		public void ChangeSleepInterval(int interval)
		{
			lock (sleepTimerLock)
			{
				if (sleepTimer != null)
				{
					sleepTimer.Elapsed -= SleepTimer_Elapsed;
					sleepTimer.Dispose();
					sleepTimerNextRunTime = default(DateTime);
					sleepTimer = null;
				}
				if (interval > 0)
				{
					// Create delay timer.
					sleepTimer = new System.Timers.Timer();
					sleepTimer.AutoReset = false;
					sleepTimer.Interval = interval;
					sleepTimer.Elapsed += SleepTimer_Elapsed;
				}
			}
		}

		private void SleepTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			lock (sleepTimerLock)
			{
				sleepTimerNextRunTime = default(DateTime);
			}
			lock (queueLock)
			{
				if (IsDisposing)
					return;
				_StarThread();
			}
		}


		public void SleepTimerStop()
		{
			lock (sleepTimerLock)
			{
				if (sleepTimer != null)
				{
					sleepTimer.Stop();
					sleepTimerNextRunTime = default(DateTime);
				}
			}
		}

		public void SleepTimerStart()
		{
			lock (sleepTimerLock)
			{
				if (sleepTimer != null)
				{
					sleepTimerNextRunTime = DateTime.Now.AddMilliseconds(sleepTimer.Interval);
					sleepTimer.Start();
				}
			}
		}

		#endregion

		private delegate void InvokeDelegate();

		public virtual string DoActionNow(T item = null)
		{
			return _DoActionNow(item);
		}

		/// <summary>
		/// Trigger execution of DoAction as soon as possible.
		/// </summary>
		public string _DoActionNow(T item = null)
		{
			var data = new List<string>();
			lock (queueLock)
			{
				AddCount++;
				if (IsDisposing)
					return string.Empty;
				double delayTimerInterval = 0;
				lock (delayTimerLock)
				{
					delayTimerInterval = delayTimer is null ? 0 : delayTimer.Interval;
				}
				// If there is no delay between actions then...
				if (delayTimerInterval == 0)
				{
					if (item != null)
						// Simply add all job items to the queue.
						Queue.Add(item);
					_StarThread();
					data.Add("Queue item added");
				}
				else
				{
					if (item != null)
					{
						// If job queue is empty or contains one processing item then...
						if (Queue.Count == 0 || (Queue.Count == 1 && processingFirstItem))
						{
							// Add new job item.
							Queue.Add(item);
							data.Add("Queue item added");
						}
						else
						{
							// Update available item in the queue.
							Queue[Queue.Count - 1] = item;
							data.Add("Queue item updated");
						}
					}
					// If must process first job immediately and enough time passed from last execution then...
					if (ProcessImmediately && delayTimerInterval < _LastActionDoneTime.ElapsedMilliseconds)
					{
						_StarThread();
					}
					// If thread is not running and queue have items. then...
					// Note: If thread is still running then queue item will be processed on running thread.
					else if (!IsRunning && Queue.Count > 0)
					{
						double sleepTimerInterval = 0;
						lock (sleepTimerLock)
						{
							sleepTimerInterval = sleepTimer is null ? 0 : sleepTimer.Interval;
						}
						// Check if sleep timer expired.
						if (sleepTimerInterval <= _LastActionDoneTime.ElapsedMilliseconds)
							DelayTimerStop();
						// Restart delay.
						DelayTimerStart();
						data.Add("Delay timer started");
						data.Add(string.Format("DelayTime = {0}", delayTimerInterval));
					}
				}
			}
			data.Add(string.Format("DoActionCount = {0}", ThreadCount));
			data.Add(string.Format("QueueCount = {0}", Queue.Count));
			data.Add(string.Format("IsRunning = {0}", IsRunning));
			if (exceptionCount > 0)
			{
				data.Add(string.Format("ExceptionCount = {0}", exceptionCount));
				if (lastExceptionDate.Ticks > 0)
				{
					data.Add(string.Format("LastException = {0}", lastException));
					if (DateTime.Now.Subtract(lastExceptionDate).TotalSeconds > 10)
						lastExceptionDate = new DateTime();
				}
			}
			return string.Join(", ", data.ToArray());
		}

		public bool UseThreadPool = true;
		protected ParameterizedThreadStart _ThreadStart;
		protected Thread _Thread;

		/// <summary>
		/// This function will be called inside 'queueLock' lock.
		/// </summary>
		protected virtual void _StarThread()
		{
			if (IsDisposing)
				return;
			StartCount++;
			// If thread is not running and queue contains items to process then...
			if (!IsRunning)
			{
				SleepTimerStop();
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
		}

		private bool processingFirstItem = false;

		protected void ThreadAction(object state)
		{
			if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
				Thread.CurrentThread.Name = "QueueTimerThread";
			ThreadCount++;
			T item = null;
			var firstRun = true;
			var cancelExecution = false;
			while (true)
			{
				lock (queueLock)
				{
					// If no arguments left then leave the loop (except if this is firs run.
					if (!firstRun && (Queue.Count == 0 || IsDisposing || cancelExecution))
					{
						SleepTimerStart();
						// Start sleep timer.
						_LastActionDoneTime.Reset();
						_LastActionDoneTime.Start();
						// Mark thread as not running;
						IsRunning = false;
						return;
					}
					if (Queue.Count > 0)
					{
						item = Queue[0];
						processingFirstItem = true;
						ActionCount++;
					}
					else
					{
						ActionNoneCount++;
					}
					firstRun = false;
				}
				// Note: Queue could be cleared before next queueLock is taken.
				var e = new QueueTimerEventArgs();
				e.Item = item;
				// Do synchronous action.
				ExecuteAction(e);
				cancelExecution = e.Cancel;
				// Remove item after job complete.
				lock (queueLock)
				{
					// If thread item was taken then...
					if (item != null && !e.Keep)
					{
						// Fire event before removing.
						var br = BeforeRemove;
						if (br != null)
							br(this, e);
						// Remove item from the queue (mostly ALWAYS it will be the first item in the queue _Queue[0]).
						if (Queue.Contains(item))
							Queue.Remove(item);
						processingFirstItem = false;
					}
				}
			}
		}

		private void ExecuteAction(QueueTimerEventArgs e)
		{
			var dw = DoWork;
			if (dw != null)
			{
				try
				{
					// New jobs can be added to the queue during execution.
					dw(this, e);
				}
				catch (Exception ex)
				{
					lastException = ex.ToString();
					lastExceptionDate = DateTime.Now;
					exceptionCount++;
				}
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

		// The bulk of the clean-up code is implemented in Dispose(bool)
		protected virtual void Dispose(bool disposing)
		{
			lock (queueLock)
			{
				IsDisposing = true;
			}
			if (disposing)
			{
				ChangeDelayInterval(0);
				ChangeSleepInterval(0);
				// Dispose timers first
				lock (queueLock)
				{
					if (Queue.Count > 0)
						Queue.Clear();
				}
				DoWork = null;
			}
		}

		#endregion

	}


}
