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
			_Sorted = true;
			OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
		}

		protected override void RemoveSortCore()
		{
			if (!_Sorted)
				return;
			Clear();
			foreach (var item in _OriginalCollection)
				Add(item);
			_OriginalCollection.Clear();
			_SortProperty = null;
			_SortDescriptions = null;
			_Sorted = false;
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

		protected override void InsertItem(int index, T item)
		{
			foreach (PropertyDescriptor propDesc in TypeDescriptor.GetProperties(item))
			{
				if (propDesc.SupportsChangeEvents)
					propDesc.AddValueChanged(item, OnItemChanged);
			}
			base.InsertItem(index, item);
		}

		protected override void RemoveItem(int index)
		{
			var item = Items[index];
			var propDescs = TypeDescriptor.GetProperties(item);
			foreach (PropertyDescriptor propDesc in propDescs)
			{
				if (propDesc.SupportsChangeEvents)
					propDesc.RemoveValueChanged(item, OnItemChanged);
			}
			base.RemoveItem(index);
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
