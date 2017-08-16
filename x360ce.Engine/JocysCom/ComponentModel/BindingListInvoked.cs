using System;
using System.ComponentModel;
using System.Threading;

namespace JocysCom.ClassLibrary.ComponentModel
{
	public class BindingListInvoked<T> : BindingList<T>
	{
		#region ISynchronizeInvoker

		public ISynchronizeInvoke SynchronizingObject { get; set; }

		delegate void InvokeDelegate();

		protected override void OnListChanged(ListChangedEventArgs e)
		{
			var so = SynchronizingObject;
			if (so != null && so.InvokeRequired)
			{
				var result = so.BeginInvoke((InvokeDelegate)delegate()
				{
					base.OnListChanged(e);
				}, new object[0]);
				so.EndInvoke(result);
			}
			else
			{
				base.OnListChanged(e);
			}
		}

		protected override void OnAddingNew(AddingNewEventArgs e)
		{
			var so = SynchronizingObject;
			if (so != null && so.InvokeRequired)
			{
				var x = (InvokeDelegate)delegate () { base.OnAddingNew(e); };
				var result = so.BeginInvoke(x, new object[0]);
				so.EndInvoke(result);
			}
			else
			{
				base.OnAddingNew(e);
			}
		}

		#endregion
	}
}
