using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace JocysCom.ClassLibrary.ComponentModel
{
	public class ObservableCollectionInvoked<T> : ObservableCollection<T>
	{
		public ObservableCollectionInvoked() : base() { }

		public ObservableCollectionInvoked(IList<T> list)
			: base(list) { }

		public ObservableCollectionInvoked(IEnumerable<T> enumeration)
			: base(new List<T>(enumeration)) { }

		public void AddRange(IEnumerable<T> list)
		{
			foreach (T item in list)
				Add(item);
		}

		#region ISynchronizeInvoker

		public TaskScheduler SynchronizingObject { get; set; }

		delegate void ItemDelegate(int index, T item);

		public bool AsynchronousInvoke { get; set; }

		void Invoke(Delegate method, params object[] args)
		{
			var so = SynchronizingObject;
			if (so is null || !JocysCom.ClassLibrary.Controls.ControlsHelper.InvokeRequired)
			{
				DynamicInvoke(method, args);
				return;
			}
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
			{
				Task.Factory.StartNew(() =>
					DynamicInvoke(method, args),
					CancellationToken.None, TaskCreationOptions.None, so);
				return;
			}
			var task = new Task(() =>
				DynamicInvoke(method, args));
			task.RunSynchronously(so);
		}

		object OneChangeAtTheTime = new object();

		void DynamicInvoke(Delegate method, params object[] args)
		{
			try
			{
				lock (OneChangeAtTheTime)
					method.DynamicInvoke(args);
			}
			catch (Exception ex)
			{
				// Add data to help with debugging.
				var prefix = string.Format("{0}<T>", nameof(ObservableCollectionInvoked<T>)) + ".";
				ex.Data.Add(prefix + "T", typeof(T).FullName);
				ex.Data.Add(prefix + "SynchronizingObject", SynchronizingObject?.GetType().FullName);
				ex.Data.Add(prefix + "AsynchronousInvoke", AsynchronousInvoke);
				throw;
			}
		}

		protected override void RemoveItem(int index)
			=> Invoke((Action<int>)base.RemoveItem, index);

		protected override void InsertItem(int index, T item)
			=> Invoke((ItemDelegate)base.InsertItem, index, item);

		protected override void SetItem(int index, T item)
			=> Invoke((ItemDelegate)base.SetItem, index, item);

		protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
			=> Invoke((Action<NotifyCollectionChangedEventArgs>)base.OnCollectionChanged, e);

		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
			=> Invoke((Action<PropertyChangedEventArgs>)base.OnPropertyChanged, e);

		#endregion

	}
}
