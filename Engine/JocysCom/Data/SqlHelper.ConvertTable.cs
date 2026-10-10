#nullable disable

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;


namespace JocysCom.ClassLibrary.Data
{

	public partial class SqlHelper
	{

        #region Convert Table To/From List

        /// <summary>
        /// Convert DataTable to List of objects. Can be used to convert DataTable to list of framework entities. 
        /// </summary>
        public static List<T> ConvertToList<T>(DataTable table)
        {
            if (table is null) return null;
            var list = new List<T>();
            var props = GetPropertiesForTable(typeof(T));
            var columns = table.Columns.Cast<DataColumn>().ToArray();
            foreach (DataRow row in table.Rows)
            {
                var item = Convert<T>(row, props, columns);
                list.Add(item);
            }
            return list;
        }

        /// <summary>Convert DataRow to object.</summary>
        /// <param name="propsCache">Optional for cache reasons.</param>
        /// <param name="columnsCache">Optional for cache reasons.</param>
        public static T Convert<T>(DataRow row, PropertyInfo[] propsCache = null, DataColumn[] columnsCache = null)
        {
            var props = propsCache ?? GetPropertiesForTable(typeof(T));
            var columns = columnsCache ?? row.Table.Columns.Cast<DataColumn>().ToArray();
            var item = Activator.CreateInstance<T>();
            foreach (var prop in props)
            {
                var column = columns.FirstOrDefault(x => prop.Name.Equals(x.ColumnName, StringComparison.OrdinalIgnoreCase));
                if (column is null)
                    continue;
                if (!prop.CanWrite)
                    continue;
                if (row.IsNull(column.ColumnName))
                    continue;
                var value = row[column.ColumnName];
                // If type must be converted then...
                var columnType = row[column.ColumnName].GetType();
                if (columnType != prop.PropertyType)
                {
                    // Get type if nullable.
                    var underType = Nullable.GetUnderlyingType(prop.PropertyType);
                    if (underType != null)
                    {
                        if (columnType == typeof(string) && Equals(value, ""))
                            value = null;
                    }
                    var t = underType ?? prop.PropertyType;
                    if (value != null)
                        value = t.IsEnum
                        ? Enum.Parse(t, (string)value)
                        : System.Convert.ChangeType(value, t);
                }
                prop.SetValue(item, value, null);
            }
            return item;
        }

        /// <summary>
        /// Convert IEnumerable to DataTable.
        /// Table can be used to pass data into stored procedures.
        /// </summary>
        /// <param name="source">List to convert.</param>
        /// <param name="propertyNames">
        /// Properties to include.
        /// For example: nameof(class1.property1), nameof(class1.property2)
        /// </param>
        /// <remarks>
        /// LIST:
        ///	class1[] data = new {
        ///		{ P1 = "A", P2 = "B", P3 = "C" }
        ///		{ P1 = "E", P2 = "F", P3 = "G" }
        /// };
        /// WHERE:
        /// public class1 {
        ///		public string P1 { get;set; }
        ///		public string P2 { get;set; }
        ///		public string P3 { get;set; }
        /// }
        /// TABLE:
        /// P1,  P2,  P3
        /// "A", "B", "C"
        /// "E", "F", "G"
        /// </remarks>
        public static DataTable ConvertToTable<T>(IEnumerable<T> source, params string[] propertyNames)
        {
            if (source is null)
                return null;
            var table = new DataTable();
            var props = typeof(T).GetProperties().Where(x => x.CanRead).ToArray();
            if (propertyNames.Any())
                props = props.Where(x => propertyNames.Contains(x.Name)).ToArray();
            foreach (var prop in props)
            {
                var underType = Nullable.GetUnderlyingType(prop.PropertyType);
                var columnType = underType ?? prop.PropertyType;
                //var descriptionAttribute = prop.GetCustomAttribute<DescriptionAttribute>();
                //var columnName = descriptionAttribute?.Description ?? prop.Name;
                //table.Columns.Add(columnName, columnType);
                table.Columns.Add(prop.Name, columnType);
            }
            var values = new object[props.Length];
            foreach (T item in source)
            {
                for (int i = 0; i < props.Length; i++)
                    values[i] = props[i].GetValue(item, null);
                table.Rows.Add(values);
            }
            return table;
        }

        /// <summary>
        /// Convert object with IEnumerable propertied to DataTable.
        /// Table can be used to pass data into stored procedures.
        /// </summary>
        /// <param name="source">Object to convert.</param>
        /// <param name="propertyNames">
        /// Properties to include.
        /// For example: nameof(class1.property1), nameof(class1.property2)
        /// </param>
        /// <remarks>
        /// OBJECT:
        ///	var data = new class1(){
        ///		P1 = new[] { "A", "E" },
        ///		P2 = new[] { "B", "F" },
        ///		P3 = new[] { "C", "G" },
        ///	}
        /// WHERE:
        /// public class1 {
        ///		public string[] P1 { get;set; }
        ///		public string[] P2 { get;set; }
        ///		public string[] P3 { get;set; }
        /// }
        /// TABLE:
        /// P1,  P2,  P3
        /// "A", "B", "C"
        /// "E", "F", "G"
        /// </remarks>
        public static DataTable ConvertObjectToTable<T>(T source, params string[] propertyNames)
        {
            if (source == null)
                return null;

            var table = new DataTable();
            var typeInfo = typeof(T);

            // Get all properties of the specified type T.
            var props = typeof(T).GetProperties().Where(x => x.CanRead).ToArray();
            if (propertyNames.Any())
                props = props.Where(x => propertyNames.Contains(x.Name)).ToArray();

            // Filter properties to be included.
            var propertiesToInclude = props
                // Get properties that are IEnumerable. Exclude "string" type that is an enumerable of char.
                .Where(p => typeof(IEnumerable).IsAssignableFrom(p.PropertyType) && p.PropertyType != typeof(string))
                .ToArray();

            // Populate DataTable columns
            foreach (var property in propertiesToInclude)
            {
                // Determine the type of elements in the IEnumerable
                var elementType = GetElementType(property.PropertyType);
                var descriptionAttribute = property.GetCustomAttribute<DescriptionAttribute>();
                var columnName = descriptionAttribute?.Description ?? property.Name;
                table.Columns.Add(columnName, elementType ?? typeof(object));
            }

            // Determine maximum row count based on the largest list
            int maxRowCount = propertiesToInclude
                .Select(p => (IEnumerable)p.GetValue(source))
                .Where(l => l != null)
                .Select(l => l.Cast<object>().Count())
                .DefaultIfEmpty(0)
                .Max();

            // Populate DataTable with property values
            for (int rowIndex = 0; rowIndex < maxRowCount; rowIndex++)
            {
                var row = table.NewRow();
                for (int columnIndex = 0; columnIndex < propertiesToInclude.Length; columnIndex++)
                {
                    var property = propertiesToInclude[columnIndex];
                    var values = (IEnumerable)property.GetValue(source);
                    if (values != null)
                    {
                        var valueList = values.Cast<object>().ToList();
                        row[columnIndex] = rowIndex < valueList.Count ? valueList[rowIndex] : DBNull.Value;
                    }
                    else
                    {
                        row[columnIndex] = DBNull.Value;
                    }
                }
                table.Rows.Add(row);
            }
            return table;
        }

        /// <summary>
        /// Helper method to determine the element type of an IEnumerable property.
        /// </summary>
        private static Type GetElementType(Type enumerableType)
        {
            if (enumerableType.IsGenericType && enumerableType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return enumerableType.GetGenericArguments()[0];
            var iEnumerableInterface = enumerableType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            return iEnumerableInterface?.GetGenericArguments()[0];
        }

        #endregion

        /// <summary>Cache data for speed.</summary>
        /// <remarks>Cache allows for this class to work 20 times faster.</remarks>
        private static ConcurrentDictionary<Type, PropertyInfo[]> PropertiesForTable { get; } = new ConcurrentDictionary<Type, PropertyInfo[]>();

        private static PropertyInfo[] GetPropertiesForTable(Type t, bool cache = true)
        {
            var properties = cache
                ? PropertiesForTable.GetOrAdd(t, x => t.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
                : t.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            return properties;
        }

    }

}
