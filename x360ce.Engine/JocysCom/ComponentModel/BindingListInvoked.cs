using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace JocysCom.ClassLibrary.ComponentModel
{
	public class BindingListInvoked<T> : BindingList<T>
	{
		public BindingListInvoked() : base() { }

		public BindingListInvoked(IList<T> list)
			: base(list) { }

		public BindingListInvoked(IEnumerable<T> enumeration)
			: base(new List<T>(enumeration)) { }

		public void AddRange(IEnumerable<T> list)
		{
			foreach (T item in list) { Add(item); }
		}

		#region ISynchronizeInvoker

		public ISynchronizeInvoke SynchronizingObject { get; set; }

		delegate void ItemDelegate(int index, T item);

		public bool AsynchronousInvoke { get; set; }

		void Invoke(Delegate method, params object[] args)
		{
			var so = SynchronizingObject;
			if (so != null && so.InvokeRequired)
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
					so.BeginInvoke(method, args);
				else
					so.Invoke(method, args);

			}
			else
			{
				method.DynamicInvoke(args);
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

		#endregion
	}
}
