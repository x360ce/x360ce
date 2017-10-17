using System;
using System.Linq;
using System.Reflection;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;

namespace JocysCom.ClassLibrary.ClassTools
{
	public class EnumTools
	{

		// How To use StringToEnum(objValType, strValName)
		// var dayEnum = (System.DayOfWeek)StringToEnum(typeof(System.DayOfWeek), "Friday");

		public object StringToEnum(Type type, string name, bool ignoreCase = false)
		{
			StringComparison sc = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			foreach (FieldInfo fi in type.GetFields())
			{
				if (string.Equals(fi.Name, name, sc)) return fi.GetValue(null);
			}
			return null;
		}

		/// <summary>
		/// Bing enum to combobox.
		/// </summary>
		/// <typeparam name="T">enum</typeparam>
		/// <param name="box">Combo box control</param>
		/// <param name="format">{0} - name, {1} - numeric value, {2} - description attribute.</param>
		/// <param name="addEmpty"></param>
		public static void BindEnum<T>(System.Windows.Forms.ComboBox box, string format = null, bool addEmpty = false, bool sort = false, T? selected = null, T[] exclude = null)
			// Declare T as same as Enum.
			where T : struct, IComparable, IFormattable, IConvertible
		{
			var list = new List<DictionaryEntry>();
			if (string.IsNullOrEmpty(format)) format = "{0}";
			string display;
			foreach (var value in (T[])Enum.GetValues(typeof(T)))
			{
				if (exclude != null && exclude.Contains(value))
					continue;
				display = string.Format(format, value, System.Convert.ToInt64(value), GetDescription(value));
				list.Add(new DictionaryEntry(display, value));
			}
			if (sort)
			{
				list = list.OrderBy(x => x.Key).ToList();
			}
			if (addEmpty && !list.Any(x => (string)x.Key == ""))
			{
				list.Insert(0, new DictionaryEntry("", null));
			}
			// Make sure sorted is disabled, because it is not allowed when using DataSource.
			if (box.Sorted) box.Sorted = false;
			box.DataSource = list;
			box.DisplayMember = "Key";
			box.ValueMember = "Value";
			if (selected.HasValue)
			{
				SelectEnumValue(box, selected.Value);
			}
		}

		public static void SelectEnumValue<T>(ComboBox box, T value)
			// Declare T as same as Enum.
			where T : struct, IComparable, IFormattable, IConvertible
		{
			for (var i = 0; i < box.Items.Count; i++)
			{
				var val = ((DictionaryEntry)box.Items[i]).Value;
				if (Equals(val, value))
				{
					box.SelectedIndex = i;
					return;
				}
			}
		}

		public static string GetDescription(object value)
		{
			FieldInfo fi = value.GetType().GetField(value.ToString());
			DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
			return (attributes.Length > 0) ? attributes[0].Description : value.ToString();
		}

	}

}
