using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace JocysCom.ClassLibrary.ComponentModel
{
	/// <summary>Marshals list modifications and notifications to a TaskScheduler (e.g., UI thread) to prevent cross-thread errors.</summary>
	/// <remarks>
	/// Provides AddRange for bulk addition and overrides to dispatch operations via SynchronizingObject, with optional async invocation.
	/// </remarks>
	public class BindingListInvoked<T> : BindingList<T>
	{
		public BindingListInvoked() : base() { }

		public BindingListInvoked(IList<T> list)
			: base(list) { }

		public BindingListInvoked(IEnumerable<T> enumeration)
			: base(new List<T>(enumeration)) { }

		public void AddRange(IEnumerable<T> list)
		{
			foreach (T item in list)
			{ Add(item); }
		}

		#region ISynchronizeInvoker

		/// <summary>TaskScheduler used to marshal list operations; null disables synchronization, executing operations on the calling thread.</summary>
		public TaskScheduler SynchronizingObject { get; set; }

		delegate void ItemDelegate(int index, T item);

		delegate void ReplaceDelegate(T oldItem, T newItem);

		/// <summary>True while a change made now will be applied later, on another thread.</summary>
		bool Deferred
		{
			get
			{
				return AsynchronousInvoke && SynchronizingObject != null
					&& JocysCom.ClassLibrary.Controls.ControlsHelper.InvokeRequired;
			}
		}

		/// <summary>When true, invocation uses Task.Factory.StartNew to queue asynchronously; when false, runs synchronously on the TaskScheduler.</summary>
		public bool AsynchronousInvoke { get; set; }

		// Dispatches the delegate to SynchronizingObject's TaskScheduler when required; respects AsynchronousInvoke for async vs sync execution.
		void Invoke(Delegate method, params object[] args)
		{
			var so = SynchronizingObject;
			if (so is null || !JocysCom.ClassLibrary.Controls.ControlsHelper.InvokeRequired)
			{
				DynamicInvoke(method, args);
			}
			else
			{
				// Note that Control.Invoke(...) is a synchronous action on the main GUI thread,
				// and will wait for EnableBackControl() to return.
				// so.Invoke(...) line could freeze if main GUI thread is busy and can't give
				// attention to any .Invoke requests from background threads.
				// 
				// Main GUI thread could be blocked because:
				// a) Modal dialog is up (which means that it's not listening to new requests).
				// b) It is checking something in a tight continuous loop.
				// c) Main thread crashed because of exception.
				// 
				// Try inserting a Application.DoEvents() in the loop, which will pause
				// execution and force the main thread to process messages and any outstanding .Invoke requests.
				if (AsynchronousInvoke)
					Task.Factory.StartNew(() =>
					{
						DynamicInvoke(method, args);
					}, CancellationToken.None, TaskCreationOptions.None, so);
				else
				{
					var task = new Task(() =>
					{
						DynamicInvoke(method, args);
					});
					task.RunSynchronously(so);
				}
			}
		}

		// Lock to serialize concurrent list modifications.
		object OneChangeAtTheTime = new object();

		/// <summary>Copy of the items, taken under the lock this list uses for its changes.</summary>
		/// <remarks>
		/// A plain ToArray can run while a change is resizing the list, which fails as
		/// "Collection was modified" or as a destination array that is no longer long enough.
		/// Only the copy is guarded. Changes are marshalled before the lock is taken, so the
		/// caller never waits for another thread while holding it.
		/// </remarks>
		public T[] ToArraySynchronized()
		{
			lock (OneChangeAtTheTime)
			{
				var copy = new T[Count];
				CopyTo(copy, 0);
				return copy;
			}
		}

		// Executes the delegate under a lock and enriches exceptions with type and SynchronizingObject context data.
		void DynamicInvoke(Delegate method, params object[] args)
		{
			try
			{
				lock (OneChangeAtTheTime)
				{
					method.DynamicInvoke(args);
				}
			}
			catch (Exception ex)
			{
				// Add data to help with debugging.
				var prefix = string.Format("{0}<T>", nameof(BindingListInvoked<T>)) + ".";
				ex.Data.Add(prefix + "T", typeof(T).FullName);
				ex.Data.Add(prefix + "SynchronizingObject", SynchronizingObject?.GetType().FullName);
				ex.Data.Add(prefix + "AsynchronousInvoke", AsynchronousInvoke);
				throw;
			}
		}

		// A position describes the list as it is at this moment. When the change is carried to
		// another thread it is applied at some later moment, by which time items may have come
		// or gone and the position means something else - or nothing at all. So a change that
		// will be applied later carries the item it is about, and finds its place again there.
		//
		// Nothing below waits for the thread that applies the changes. This runs on the loop
		// that reads the hardware, up to a thousand times a second, and a loop made to wait on
		// the interface is no longer that loop. The list itself is only ever changed by the
		// thread the changes are handed to, so reading one item out of it here needs no
		// agreement with anybody - only a check that the position is still one the list has.

		protected override void RemoveItem(int index)
		{
			if (!Deferred)
			{
				Invoke((Action<int>)base.RemoveItem, index);
				return;
			}
			var items = Items;
			if (index < 0 || index >= items.Count)
				return;
			Invoke((Action<T>)RemoveKnownItem, items[index]);
		}

		void RemoveKnownItem(T item)
		{
			var at = Items.IndexOf(item);
			// Already gone: the removal it was waiting behind took it.
			if (at >= 0)
				base.RemoveItem(at);
		}

		protected override void InsertItem(int index, T item)
		{
			if (!Deferred)
			{
				Invoke((ItemDelegate)base.InsertItem, index, item);
				return;
			}
			// Adding to the list asks for the position after the last item. Items added before
			// this one are still on their way, so that position is the same one they asked for,
			// and applying them all as written stacks them in reverse. What was meant is the
			// end of the list, wherever it has got to by then.
			var atTheEnd = index >= Count;
			Invoke(atTheEnd ? (ItemDelegate)AppendKnownItem : InsertKnownItem, index, item);
		}

		void AppendKnownItem(int index, T item)
		{
			base.InsertItem(Count, item);
		}

		void InsertKnownItem(int index, T item)
		{
			base.InsertItem(Math.Min(index, Count), item);
		}

		protected override void SetItem(int index, T item)
		{
			if (!Deferred)
			{
				Invoke((ItemDelegate)base.SetItem, index, item);
				return;
			}
			var items = Items;
			if (index < 0 || index >= items.Count)
				return;
			Invoke((ReplaceDelegate)SetKnownItem, items[index], item);
		}

		void SetKnownItem(T oldItem, T newItem)
		{
			var at = Items.IndexOf(oldItem);
			if (at >= 0)
				base.SetItem(at, newItem);
		}

		// Set while a notification is on its way to the owning thread, and raised to 2 when
		// further changes arrive before it is delivered.
		int _notifyState;

		// Counts changes, so a notification can tell whether the list still looks the way it
		// did when the notification was written.
		int _version;

		/// <summary>Raises the change notification without waiting for the thread that owns the list.</summary>
		/// <remarks>
		/// A notification exists for whoever displays the list. Waiting for that thread puts the
		/// thread which changed the list behind whatever is being drawn, which is how a device
		/// loop ends up running at a fraction of its rate while a window is busy.
		/// Only one notification is in flight at a time. Changes that arrive while it is pending
		/// are delivered as a single reset, so a fast writer cannot queue work without bound on a
		/// thread that cannot keep up. The same reset covers a notification whose row number no
		/// longer describes the list. A writer that is not outpacing the reader still gets its
		/// exact notification.
		/// </remarks>
		protected override void OnListChanged(ListChangedEventArgs e)
		{
			var version = System.Threading.Interlocked.Increment(ref _version);
			var so = SynchronizingObject;
			if (so is null || !JocysCom.ClassLibrary.Controls.ControlsHelper.InvokeRequired)
			{
				DynamicInvoke((Action<ListChangedEventArgs>)base.OnListChanged, e);
				return;
			}
			// Already one on its way: mark it so the pending one covers this change too.
			if (System.Threading.Interlocked.CompareExchange(ref _notifyState, 2, 1) != 0)
				return;
			if (System.Threading.Interlocked.CompareExchange(ref _notifyState, 1, 0) != 0)
				return;
			var first = e;
			Task.Factory.StartNew(() =>
			{
				System.Threading.Interlocked.Exchange(ref _notifyState, 0);
				// The row this names was counted in a list that has since changed, so naming it
				// now would point whoever draws the list at a row that may no longer be there.
				// Anything that arrived while this was queued is covered by the same reset.
				var stale = System.Threading.Volatile.Read(ref _version) != version;
				var args = stale ? new ListChangedEventArgs(ListChangedType.Reset, -1) : first;
				DynamicInvoke((Action<ListChangedEventArgs>)base.OnListChanged, args);
			}, CancellationToken.None, TaskCreationOptions.None, so);
		}

		protected override void OnAddingNew(AddingNewEventArgs e)
		{
			Invoke((Action<AddingNewEventArgs>)base.OnAddingNew, e);
		}


		#endregion
	}
}
