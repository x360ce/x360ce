using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace JocysCom.ClassLibrary.ComponentModel
{
	/// <summary>
	/// Be.Timvw.Framework.ComponentModel
	/// http://betimvwframework.codeplex.com/
	/// </summary>
	public class PropertyComparer<T> : IComparer<T>
	{
		private readonly ListSortDescriptionCollection _SortCollection = null;
		private readonly PropertyDescriptor _PropDesc = null;
		private readonly ListSortDirection _Direction = ListSortDirection.Ascending;

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
				var xValue = _PropDesc.GetValue(x);
				var yValue = _PropDesc.GetValue(y);
				return CompareValues(xValue, yValue, _Direction);
			}
			else if (_SortCollection != null && _SortCollection.Count > 0)
				return RecursiveCompareInternal(x, y, 0);
			else
				return 0;
		}

		private int CompareValues(object xValue, object yValue, ListSortDirection direction)
		{
			int retValue;
			if (xValue is null && yValue is null)
				retValue = 0;
			else if (xValue is IComparable)
				retValue = ((IComparable)xValue).CompareTo(yValue);
			else if (yValue is IComparable)
				retValue = ((IComparable)yValue).CompareTo(xValue);
			// not comparable, compare String representations
			else if (!xValue.Equals(yValue))
				retValue = xValue.ToString().CompareTo(yValue.ToString());
			else
				retValue = 0;
			return (direction == ListSortDirection.Ascending ? 1 : -1) * retValue;
		}

		private int RecursiveCompareInternal(T x, T y, int index)
		{
			if (index >= _SortCollection.Count)
				return 0;
			var listSortDesc = _SortCollection[index];
			var xValue = listSortDesc.PropertyDescriptor.GetValue(x);
			var yValue = listSortDesc.PropertyDescriptor.GetValue(y);
			var retValue = CompareValues(xValue, yValue, listSortDesc.SortDirection);
			return (retValue == 0)
				? RecursiveCompareInternal(x, y, ++index)
				: retValue;
		}
	}
}

