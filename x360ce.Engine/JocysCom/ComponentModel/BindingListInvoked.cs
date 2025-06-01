using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace JocysCom.ClassLibrary.ComponentModel
{
	/// <summary>Marshals list modifications and notifications to a TaskScheduler (e.g., UI thread) to prevent cross-thread errors.</summary>
	/// <remarks>
	/// Provides AddRange for bulk addition and overrides to dispatch operations via SynchronizingObject, with optional async invocation.
	/// Commented-out FixLeak method offers a WPF-specific memory leak workaround.
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

		protected override void RemoveItem(int index)
		{
			Invoke((Action<int>)base.RemoveItem, index);
		}

		protected override void InsertItem(int index, T item)
		{
			Invoke((ItemDelegate)base.InsertItem, index, item);
		}

		protected override void SetItem(int index, T item)
		{
			Invoke((ItemDelegate)base.SetItem, index, item);
		}

		protected override void OnListChanged(ListChangedEventArgs e)
		{
			Invoke((Action<ListChangedEventArgs>)base.OnListChanged, e);
		}

		protected override void OnAddingNew(AddingNewEventArgs e)
		{
			Invoke((Action<AddingNewEventArgs>)base.OnAddingNew, e);
		}

		//public void FixLeak()
		//{
		//	var flags = BindingFlags.Instance | BindingFlags.NonPublic;
		//	var fi = GetType().BaseType.BaseType.GetField("onListChanged", flags);
		//	var d = (Delegate)fi.GetValue(this);
		//	if (d != null)
		//	{
		//		if (d.Target is System.Windows.Data.BindingListCollectionView view)
		//		{
		//			view.DetachFromSourceCollection();
		//			var vfi = view.GetType().BaseType.GetField("_currentItem", flags);
		//			vfi.SetValue(view, null);
		//			fi.SetValue(this, null);
		//		}
		//	}
		//}

		#endregion
	}
}
