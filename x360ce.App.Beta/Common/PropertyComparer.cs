using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.ComponentModel;
using System.Reflection;

namespace x360ce.App
{
	/// <summary>
	/// Be.Timvw.Framework.ComponentModel
	/// http://betimvwframework.codeplex.com/
	/// </summary>
	public class PropertyComparer<T> : IComparer<T>
	{
		ListSortDescriptionCollection _SortCollection = null;
		PropertyDescriptor _PropDesc = null;
		ListSortDirection _Direction = ListSortDirection.Ascending;

		public PropertyComparer(PropertyDescriptor propDesc, ListSortDirection direction)
		{
			_PropDesc = propDesc;
			_Direction = direction;
		}

		public PropertyComparer(ListSortDescriptionCollection sortCollection)
		{
			_SortCollection = sortCollection;
		}

		int IComparer<T>.Compare(T x, T y)
		{
			return Compare(x, y);
		}

		protected int Compare(T x, T y)
		{
			if (_PropDesc != null)
			{
				object xValue = _PropDesc.GetValue(x);
				object yValue = _PropDesc.GetValue(y);
				return CompareValues(xValue, yValue, _Direction);
			}
			else if (_SortCollection != null && _SortCollection.Count > 0)
			{
				return RecursiveCompareInternal(x, y, 0);
			}
			else return 0;
		}

		int CompareValues(object xValue, object yValue, ListSortDirection direction)
		{
			int retValue = 0;
			if (xValue is IComparable)
			{
				retValue = ((IComparable)xValue).CompareTo(yValue);
			}
			else if (yValue is IComparable)
			{
				retValue = ((IComparable)yValue).CompareTo(xValue);
			}
			// not comparable, compare String representations
			else if (!xValue.Equals(yValue))
			{
				retValue = xValue.ToString().CompareTo(yValue.ToString());
			}
			return (direction == ListSortDirection.Ascending ? 1 : -1) * retValue;
		}

		int RecursiveCompareInternal(T x, T y, int index)
		{
			if (index >= _SortCollection.Count) return 0;
			ListSortDescription listSortDesc = _SortCollection[index];
			object xValue = listSortDesc.PropertyDescriptor.GetValue(x);
			object yValue = listSortDesc.PropertyDescriptor.GetValue(y);
			int retValue = CompareValues(xValue, yValue, listSortDesc.SortDirection);
			return (retValue == 0)
				? RecursiveCompareInternal(x, y, ++index)
				: retValue;
		}
	}
}

