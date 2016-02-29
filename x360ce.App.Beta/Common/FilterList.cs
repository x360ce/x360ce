using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace x360ce.App
{
	/// <summary>
	/// Be.Timvw.Framework.ComponentModel
	/// http://betimvwframework.codeplex.com/
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class FilterList<T> : SortableBindingList<T>
	{
		readonly List<T> allItems = new List<T>();

		public FilterList()
		{
		}

		public FilterList(IEnumerable<T> elements)
			: base(elements)
		{
			allItems.AddRange(elements);
		}

		public void Filter(Predicate<T> filter)
		{
			if (ReferenceEquals(filter, null)) throw new ArgumentNullException("filter");

			ApplyFilter(filter);
			if (IsSortedCore) ApplySortCore(SortPropertyCore, SortDirectionCore);
			OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
		}

		protected virtual void ApplyFilter(Predicate<T> filter)
		{
			var wantedItems = this.allItems.FindAll(filter);

			Items.Clear();
			foreach (var item in wantedItems) Items.Add(item);
		}

		protected override void InsertItem(int index, T item)
		{
			base.InsertItem(index, item);
			allItems.Add(Items[index]);
		}

		protected override void RemoveItem(int index)
		{
			allItems.Remove(Items[index]);
			base.RemoveItem(index);
		}

		protected override void ClearItems()
		{
			base.ClearItems();
			allItems.Clear();
		}

		protected override void SetItem(int index, T item)
		{
			allItems[allItems.IndexOf(Items[index])] = item;
			base.SetItem(index, item);
		}
	}

}
