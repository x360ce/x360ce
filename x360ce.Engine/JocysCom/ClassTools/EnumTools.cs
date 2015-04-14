using System;
using System.Linq;
using System.Reflection;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;

namespace JocysCom.ClassLibrary.ClassTools
{
	public class EnumTools
	{

		// How To use StringToEnum(objValType, strValName)
		public void subAction()
		{
			System.DayOfWeek enuDay = (System.DayOfWeek)StringToEnum(typeof(System.DayOfWeek), "Friday");
			// Some actions with enuDay...
		}


		public object StringToEnum(Type type, string name, bool ignoreCase = false)
		{
			FieldInfo fi;
			StringComparison sc = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			foreach (FieldInfo tempLoopVar_objFieldInfo in type.GetFields())
			{
				fi = tempLoopVar_objFieldInfo;
				if (string.Equals(fi.Name, name, sc)) return fi.GetValue(null);
			}
			return null;
		}

		public static void BindEnum<T>(System.Windows.Forms.ComboBox box)
		{
			BindEnum<T>(box, "{0}", false, false, null);
		}

		public static void BindEnum<T>(System.Windows.Forms.ComboBox box, string format)
		{
			BindEnum<T>(box, format, false, false, null);
		}

		public static void BindEnum<T>(System.Windows.Forms.ComboBox box, string format, bool addEmpty, bool sort, object selected)
		{
			BindEnum<T>(box, format, addEmpty, sort, selected, null);
		}

		/// <summary>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="box"></param>
		/// <param name="format">{0} - name, {1} - integer value, {2} - description attribute.</param>
		/// <param name="addEmpty"></param>
		public static void BindEnum<T>(System.Windows.Forms.ComboBox box, string format, bool addEmpty, bool sort, object selected, T[] exclude)
		{
			var list = new List<DictionaryEntry>();
			if (string.IsNullOrEmpty(format)) format = "{0}";
			//System.Data.DataTable table = new System.Data.DataTable();
			//table.Columns.Add("Display", typeof(string));
			//table.Columns.Add("Value", typeof(T));
			string display;
			if (addEmpty) list.Add(new DictionaryEntry("", null));
			foreach (Enum value in Enum.GetValues(typeof(T)))
			{
				if (exclude != null && exclude.Contains((T)(object)value)) continue;
				display = string.Format(format, value.ToString(), (int)(object)value, GetDescription(value));
				list.Add(new DictionaryEntry(display, value));
			}
			if (sort) box.Sorted = sort;
			// table.DefaultView.Sort = table.Columns["Display"].ColumnName + " asc";
			box.DataSource = list;
			box.DisplayMember = "Key";
			box.ValueMember = "Value";
			SelectEnumValue(box, selected);
		}

		public static void SelectEnumValue(System.Windows.Forms.ComboBox box, object value)
		{
			if (value != null)
			{
				for (int i = 0; i < box.Items.Count; i++)
				{
					var val = ((DictionaryEntry)box.Items[i]).Value;
					if ((val == null && value == null) || (val != null && val.Equals(value)))
					{
						box.SelectedIndex = i;
						return;
					}
				}
			}
		}

		//public static void RemoveItem<T>(System.Windows.Forms.ComboBox box, T value)
		//{
		//    if (value != null)
		//    {
		//        for (int i = 0; i < box.Items.Count; i++)
		//        {
		//            if (((DictionaryEntry)box.Items[i]).Value.Equals(value))
		//            {
		//                box.Items.RemoveAt(i);
		//                return;
		//            }
		//        }
		//    }
		//}

		public static T GetValue<T>(System.Windows.Forms.ComboBox box, T value)
		{
			return (T)((DictionaryEntry)box.SelectedItem).Value;
		}

		public static string GetDescription(object value)
		{
			FieldInfo fi = value.GetType().GetField(value.ToString());
			DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
			return (attributes.Length > 0) ? attributes[0].Description : value.ToString();
		}

		// Old: Get Enumeration key names and values.
		public string fncServerConnectionStatus(Type enumType)
		{
			System.Text.StringBuilder objStringBuilder = new System.Text.StringBuilder();
			string strError = string.Empty;
			try
			{
				System.Reflection.FieldInfo objFieldInfo;
				foreach (System.Reflection.FieldInfo tempLoopVar_objFieldInfo in enumType.GetFields())
				{
					objFieldInfo = tempLoopVar_objFieldInfo;
					objStringBuilder.Append(objFieldInfo.Name).Append(" = ").Append(objFieldInfo.GetValue(null).ToString()).Append("<br>");
				}
			}
			catch (Exception ex)
			{
				strError = ex.Message;
			}
			if (strError == null)
			{
				return objStringBuilder.ToString();
			}
			else
			{
				return strError;
			}
		}

	}

}
