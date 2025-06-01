using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace JocysCom.ClassLibrary.ComponentModel
{
    /// <summary>Implements IComparer&lt;T&gt; to sort items by a single PropertyDescriptor or a ListSortDescriptionCollection.</summary>
    /// <remarks>
    /// Part of Be.Timvw.Framework.ComponentModel (http://betimvwframework.codeplex.com/).
    /// Used by SortableBindingList&lt;T&gt; to apply simple and advanced sorting in data-binding contexts.
    /// A parallel SortComparer&lt;T&gt; in the Collections namespace provides an alternative implementation.
    /// </remarks>
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

        /// <summary>
        /// Compares x and y using the configured sort criteria.
        /// Uses the single PropertyDescriptor and direction if provided; otherwise applies the ListSortDescriptionCollection recursively.
        /// Returns 0 when no sort criteria are specified.
        /// </summary>
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

        /// <summary>
        /// Compares two property values, using IComparable when available or falling back to string comparison, and applies sort direction.
        /// </summary>
        private int CompareValues(object xValue, object yValue, ListSortDirection direction)
        {
            int retValue;
            if (xValue is null && yValue is null)
                retValue = 0;
            else if (xValue is IComparable)
                retValue = ((IComparable)xValue).CompareTo(yValue);
            else if (yValue is IComparable)
                retValue = ((IComparable)yValue).CompareTo(xValue);
            else if (!xValue.Equals(yValue))
                retValue = xValue.ToString().CompareTo(yValue.ToString());
            else
                retValue = 0;
            return (direction == ListSortDirection.Ascending ? 1 : -1) * retValue;
        }

        /// <summary>
        /// Recursively compares items by each sort description in the collection until a non-zero result is found or the end is reached.
        /// </summary>
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