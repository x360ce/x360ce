#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace JocysCom.ClassLibrary.ComponentModel
{
	/// <summary>
	/// Be.Timvw.Framework.ComponentModel
	/// http://betimvwframework.codeplex.com/
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[Serializable]
	public class SortableBindingList<T> : BindingListInvoked<T>, IBindingListView, IRaiseItemChangedEvents
	{
		public SortableBindingList() : base() { }

		public SortableBindingList(IList<T> list)
			: base(list) { }

		public SortableBindingList(IEnumerable<T> enumeration)
			: base(new List<T>(enumeration)) { }

		public static SortableBindingList<T> From(IEnumerable<T> list)
		{
			return new SortableBindingList<T>(list);
		}

		protected override bool SupportsSearchingCore => true;
		protected override bool SupportsSortingCore => true;
		protected override bool IsSortedCore => _Sorted;
		protected override ListSortDirection SortDirectionCore => _SortDirection;
		protected override PropertyDescriptor SortPropertyCore => _SortProperty;

		ListSortDescriptionCollection IBindingListView.SortDescriptions => SortDescriptions;
		protected ListSortDescriptionCollection SortDescriptions => _SortDescriptions;

		bool IBindingListView.SupportsAdvancedSorting => SupportsAdvancedSorting;
		protected bool SupportsAdvancedSorting => true;

		bool IBindingListView.SupportsFiltering => SupportsFiltering;
		protected bool SupportsFiltering => true;

		bool IRaiseItemChangedEvents.RaisesItemChangedEvents => RaisesItemChangedEvents;
		protected bool RaisesItemChangedEvents => true;

		private bool _Sorted = false;
		private bool _Filtered = false;
		private string _FilterString = null;
		private ListSortDirection _SortDirection = ListSortDirection.Ascending;

		[NonSerialized]
		private PropertyDescriptor _SortProperty = null;

		[NonSerialized]
		private ListSortDescriptionCollection _SortDescriptions = new ListSortDescriptionCollection();

		[NonSerialized]
		private PropertyComparer<T> _SortComparer = null;
		private readonly List<T> _OriginalCollection = new List<T>();
		bool IBindingList.AllowNew => CheckReadOnly();
		bool IBindingList.AllowRemove => CheckReadOnly();
		private bool CheckReadOnly() { return !_Sorted && !_Filtered; }

		protected override int FindCore(PropertyDescriptor property, object key)
		{
			// Simple iteration:
			for (var i = 0; i < Count; i++)
			{
				var item = this[i];
				if (property.GetValue(item).Equals(key))
					return i;
			}
			return -1; // Not found
		}

		protected override void ApplySortCore(PropertyDescriptor property, ListSortDirection direction)
		{
			_SortDirection = direction;
			_SortProperty = property;
			var comparer = new PropertyComparer<T>(property, direction);
			ApplySortInternal(comparer);
		}

		void IBindingListView.ApplySort(ListSortDescriptionCollection sorts)
		{
			ApplySort(sorts);
		}

		protected void ApplySort(ListSortDescriptionCollection sorts)
		{
			_SortProperty = null;
			_SortDescriptions = sorts;
			var comparer = new PropertyComparer<T>(sorts);
			ApplySortInternal(comparer);
		}

		private void ApplySortInternal(PropertyComparer<T> comparer)
		{
			if (_OriginalCollection.Count == 0)
				_OriginalCollection.AddRange(this);
			var listRef = Items as List<T>;
			if (listRef is null)
				return;
			listRef.Sort(comparer);
			_SortComparer = comparer;
			_Sorted = true;
			OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
		}

		protected override void RemoveSortCore()
		{
			if (!_Sorted)
				return;
			// Clear the sort state BEFORE restoring the original items. Add(...) re-enters
			// the overridden InsertItem, and while _Sorted is still true that override would
			// re-sort the item and append it to _OriginalCollection — mutating the very
			// collection being enumerated here ("Collection was modified"). Mirrors the
			// already-safe ordering used by RemoveFilter below.
			_SortProperty = null;
			_SortDescriptions = null;
			_SortComparer = null;
			_Sorted = false;
			Clear();
			foreach (var item in _OriginalCollection)
				Add(item);
			_OriginalCollection.Clear();
		}

		string IBindingListView.Filter { get { return Filter; }  set { Filter = value; }  }

		protected string Filter
		{
			get { return _FilterString; }
			set
			{
				_FilterString = value;
				_Filtered = true;
				UpdateFilter();
			}
		}

		void IBindingListView.RemoveFilter() { RemoveFilter(); }
		protected void RemoveFilter()
		{
			if (!_Filtered)
				return;
			_FilterString = null;
			_Filtered = false;
			_Sorted = false;
			_SortDescriptions = null;
			_SortProperty = null;
			Clear();
			foreach (var item in _OriginalCollection)
				Add(item);
			_OriginalCollection.Clear();
		}

		protected virtual void UpdateFilter()
		{
			var equalsPos = _FilterString.IndexOf('=');
			// Get property name
			var propName = _FilterString.Substring(0, equalsPos).Trim();
			// Get filter criteria
			var criteria = _FilterString.Substring(equalsPos + 1,
			   _FilterString.Length - equalsPos - 1).Trim();
			// Strip leading and trailing quotes
			criteria = criteria.Trim('\'', '"');
			// Get a property descriptor for the filter property
			var propDesc = TypeDescriptor.GetProperties(typeof(T))[propName];
			if (_OriginalCollection.Count == 0)
				_OriginalCollection.AddRange(this);
			var currentCollection = new List<T>(this);
			Clear();
			foreach (var item in currentCollection)
			{
				var value = propDesc.GetValue(item);
				if (string.Format("{0}", value) == criteria)
					Add(item);
			}
		}

		/// <summary>True when a change made now is carried to the thread that owns the list.</summary>
		bool ChangedFromAnotherThread => SynchronizingObject != null
			&& JocysCom.ClassLibrary.Controls.ControlsHelper.InvokeRequired;

		protected override void InsertItem(int index, T item)
		{
			// While sorted, the position comes from the sort and the item is recorded for
			// RemoveSort. Both read and change the list, so an item from another thread is
			// carried to the thread that owns the list and placed there.
			if (_Sorted && ChangedFromAnotherThread)
			{
				Invoke((Action<T>)InsertCarriedItem, item);
				return;
			}
			foreach (PropertyDescriptor propDesc in TypeDescriptor.GetProperties(item))
			{
				if (propDesc.SupportsChangeEvents)
					propDesc.AddValueChanged(item, OnItemChanged);
			}
			// When a sort is active, ignore the caller's index and place the
			// item at the position that preserves the sort order. Otherwise a
			// caller doing Insert(0, ...) would always push new items to the
			// top of the view, regardless of their actual sort value.
			if (_Sorted && _SortComparer != null)
			{
				index = GetSortedInsertIndex(item);
				_OriginalCollection.Add(item);
			}
			base.InsertItem(index, item);
		}

		/// <summary>Adds an item carried from another thread: where the sort places it, or at the end.</summary>
		void InsertCarriedItem(T item)
		{
			InsertItem(Count, item);
		}

		private int GetSortedInsertIndex(T item)
		{
			var listRef = Items as List<T>;
			if (listRef is null || listRef.Count == 0)
				return 0;
			var foundIndex = listRef.BinarySearch(item, _SortComparer);
			return foundIndex < 0 ? ~foundIndex : foundIndex;
		}

		protected override void RemoveItem(int index)
		{
			// While sorted the item also leaves _OriginalCollection, so a removal from another
			// thread is carried, with its item, to the thread that owns the list.
			if (_Sorted && ChangedFromAnotherThread)
			{
				var items = Items;
				if (index >= 0 && index < items.Count)
					Invoke((Action<T>)RemoveCarriedItem, items[index]);
				return;
			}
			var item = Items[index];
			var propDescs = TypeDescriptor.GetProperties(item);
			foreach (PropertyDescriptor propDesc in propDescs)
			{
				if (propDesc.SupportsChangeEvents)
					propDesc.RemoveValueChanged(item, OnItemChanged);
			}
			// Mirror InsertItem: while sorted, every item is also tracked in
			// _OriginalCollection so RemoveSort can restore the pre-sort view. Drop the
			// removed item from that backing list too, otherwise it would silently
			// reappear at the bottom as soon as the user clears the sort.
			if (_Sorted)
				_OriginalCollection.Remove(item);
			base.RemoveItem(index);
		}

		/// <summary>Removes an item carried from another thread, unless a removal before it took it.</summary>
		void RemoveCarriedItem(T item)
		{
			var at = Items.IndexOf(item);
			if (at >= 0)
				RemoveItem(at);
		}

		protected override void ClearItems()
		{
			// A user-initiated Clear() while sorted must also drop _OriginalCollection,
			// otherwise every cleared row is resurrected the moment the sort is removed
			// (RemoveSortCore re-adds from it). Same bug class as RemoveItem above.
			// The internal Clear() calls inside RemoveSortCore/RemoveFilter are safe:
			// both set _Sorted = false first, so this guard is false there and the
			// restore buffer is preserved for re-adding.
			if (_Sorted)
				_OriginalCollection.Clear();
			base.ClearItems();
		}

		private void OnItemChanged(object sender, EventArgs args)
		{
			var index = Items.IndexOf((T)sender);
			OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, index));
		}

		public void RemoveAll(Func<object, bool> value)
		{
			throw new NotImplementedException();
		}
	}

}
