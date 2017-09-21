using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Windows.Forms;

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

		protected override bool SupportsSearchingCore { get { return true; } }
		protected override bool SupportsSortingCore { get { return true; } }
		protected override bool IsSortedCore { get { return _Sorted; } }
		protected override ListSortDirection SortDirectionCore { get { return _SortDirection; } }
		protected override PropertyDescriptor SortPropertyCore { get { return _SortProperty; } }

		ListSortDescriptionCollection IBindingListView.SortDescriptions { get { return SortDescriptions; } }
		protected ListSortDescriptionCollection SortDescriptions { get { return _SortDescriptions; } }

		bool IBindingListView.SupportsAdvancedSorting { get { return SupportsAdvancedSorting; } }
		protected bool SupportsAdvancedSorting { get { return true; } }

		bool IBindingListView.SupportsFiltering { get { return SupportsFiltering; } }
		protected bool SupportsFiltering { get { return true; } }

		bool IRaiseItemChangedEvents.RaisesItemChangedEvents { get { return RaisesItemChangedEvents; } }
		protected bool RaisesItemChangedEvents { get { return true; } }

		bool _Sorted = false;
		bool _Filtered = false;
		string _FilterString = null;
		ListSortDirection _SortDirection = ListSortDirection.Ascending;

		[NonSerializedAttribute]
		PropertyDescriptor _SortProperty = null;

		[NonSerializedAttribute]
		ListSortDescriptionCollection _SortDescriptions = new ListSortDescriptionCollection();

		List<T> _OriginalCollection = new List<T>();
		bool IBindingList.AllowNew { get { return CheckReadOnly(); } }
		bool IBindingList.AllowRemove { get { return CheckReadOnly(); } }
		private bool CheckReadOnly() { return !_Sorted && !_Filtered; }

		protected override int FindCore(PropertyDescriptor property, object key)
		{
			// Simple iteration:
			for (int i = 0; i < Count; i++)
			{
				T item = this[i];
				if (property.GetValue(item).Equals(key))
				{
					return i;
				}
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
			{
				_OriginalCollection.AddRange(this);
			}
			var listRef = Items as List<T>;
			if (listRef == null) return;
			listRef.Sort(comparer);
			_Sorted = true;
			OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
		}

		protected override void RemoveSortCore()
		{
			if (!_Sorted) return;
			Clear();
			foreach (T item in _OriginalCollection)
			{
				Add(item);
			}
			_OriginalCollection.Clear();
			_SortProperty = null;
			_SortDescriptions = null;
			_Sorted = false;
		}

		string IBindingListView.Filter { get { return Filter; } set { Filter = value; } }

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
			if (!_Filtered) return;
			_FilterString = null;
			_Filtered = false;
			_Sorted = false;
			_SortDescriptions = null;
			_SortProperty = null;
			Clear();
			foreach (T item in _OriginalCollection)
			{
				Add(item);
			}
			_OriginalCollection.Clear();
		}

		protected virtual void UpdateFilter()
		{
			int equalsPos = _FilterString.IndexOf('=');
			// Get property name
			string propName = _FilterString.Substring(0, equalsPos).Trim();
			// Get filter criteria
			string criteria = _FilterString.Substring(equalsPos + 1,
			   _FilterString.Length - equalsPos - 1).Trim();
			// Strip leading and trailing quotes
			criteria = criteria.Trim('\'', '"');
			// Get a property descriptor for the filter property
			var propDesc = TypeDescriptor.GetProperties(typeof(T))[propName];
			if (_OriginalCollection.Count == 0)
			{
				_OriginalCollection.AddRange(this);
			}
			var currentCollection = new List<T>(this);
			Clear();
			foreach (T item in currentCollection)
			{
				object value = propDesc.GetValue(item);
				if (string.Format("{0}", value) == criteria)
				{
					Add(item);
				}
			}
		}

		protected override void InsertItem(int index, T item)
		{
			foreach (PropertyDescriptor propDesc in TypeDescriptor.GetProperties(item))
			{
				if (propDesc.SupportsChangeEvents)
				{
					propDesc.AddValueChanged(item, OnItemChanged);
				}
			}
			base.InsertItem(index, item);
		}

		protected override void RemoveItem(int index)
		{
			T item = Items[index];
			var propDescs = TypeDescriptor.GetProperties(item);
			foreach (PropertyDescriptor propDesc in propDescs)
			{
				if (propDesc.SupportsChangeEvents)
				{
					propDesc.RemoveValueChanged(item, OnItemChanged);
				}
			}
			base.RemoveItem(index);
		}

		void OnItemChanged(object sender, EventArgs args)
		{
			int index = Items.IndexOf((T)sender);
			OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, index));
		}

    }

}
