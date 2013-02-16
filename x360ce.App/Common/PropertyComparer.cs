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
		readonly IComparer comparer;
		PropertyDescriptor propertyDescriptor;
		int reverse;

		public PropertyComparer(PropertyDescriptor property, ListSortDirection direction)
		{
			this.propertyDescriptor = property;
			Type comparerForPropertyType = typeof(Comparer<>).MakeGenericType(property.PropertyType);
			this.comparer = (IComparer)comparerForPropertyType.InvokeMember("Default", BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.Public, null, null, null);
			this.SetListSortDirection(direction);
		}

		#region IComparer<T> Members

		public int Compare(T x, T y)
		{
			return this.reverse * this.comparer.Compare(this.propertyDescriptor.GetValue(x), this.propertyDescriptor.GetValue(y));
		}

		#endregion

		void SetPropertyDescriptor(PropertyDescriptor descriptor)
		{
			this.propertyDescriptor = descriptor;
		}

		void SetListSortDirection(ListSortDirection direction)
		{
			this.reverse = direction == ListSortDirection.Ascending ? 1 : -1;
		}

		public void SetPropertyAndDirection(PropertyDescriptor descriptor, ListSortDirection direction)
		{
			this.SetPropertyDescriptor(descriptor);
			this.SetListSortDirection(direction);
		}
	}
}
