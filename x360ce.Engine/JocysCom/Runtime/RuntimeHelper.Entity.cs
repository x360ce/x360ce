#if NETCOREAPP // .NET Core
using Microsoft.EntityFrameworkCore;
#elif NETSTANDARD // .NET Standard
using Microsoft.EntityFrameworkCore;
#else // .NET Framework
using System.Data.Objects.DataClasses;
#endif
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace JocysCom.ClassLibrary.Runtime
{
	public static partial class RuntimeHelper
	{

		#region Copy Properties with DataMember attribute

		private static readonly object DataMembersLock = new object();
		private static readonly Dictionary<Type, PropertyInfo[]> DataMembers = new Dictionary<Type, PropertyInfo[]>();
		private static readonly Dictionary<Type, PropertyInfo[]> DataMembersNoKey = new Dictionary<Type, PropertyInfo[]>();

		private static PropertyInfo[] GetDataMemberProperties<T>(T item, bool skipKey = false)
		{
			PropertyInfo[] ps;
			var t = item == null ? typeof(T) : item.GetType();
			lock (DataMembersLock)
			{
				var cache = skipKey ? DataMembersNoKey : DataMembers;
				if (cache.ContainsKey(t))
				{
					ps = cache[t];
				}
				else
				{
					var items = t.GetProperties(DefaultBindingFlags | BindingFlags.DeclaredOnly)
						.Where(p => p.CanRead && p.CanWrite)
						.Where(p => Attribute.IsDefined(p, typeof(DataMemberAttribute)));
#if NETCOREAPP // .NET Core
#elif NETSTANDARD // .NET Standard
#else // .NET Framework

					if (skipKey)
					{
						var keys = items
							.Where(p => Attribute.IsDefined(p, typeof(EdmScalarPropertyAttribute)))
							.Where(p => ((EdmScalarPropertyAttribute)Attribute.GetCustomAttribute(p, typeof(EdmScalarPropertyAttribute))).EntityKeyProperty);
						items = items.Except(keys);
					}
#endif
					ps = items
						// Order properties by name so list will change less with the code changed (important for checksums)
						.OrderBy(x => x.Name)
						.ToArray();
					cache.Add(t, ps);
				}
			}
			return ps;
		}


		/// <summary>
		/// Copy properties with [DataMemberAttribute].
		/// Only members declared at the level of the supplied type's hierarchy should be copied.
		/// Inherited members are not copied.
		/// </summary>
		public static void CopyDataMembers<T>(T source, T dest, bool skipKey = false)
		{
			var t = source == null ? typeof(T) : source.GetType();
			var ps = GetDataMemberProperties(source, skipKey);
			foreach (var p in ps)
			{
				var sValue = p.GetValue(source, null);
				var dValue = p.GetValue(dest, null);
				// If values are different then...
				if (!Equals(sValue, dValue))
					p.SetValue(dest, sValue, null);
			}
		}

		/// <summary>
		/// Get string representation of [DataMemberAttribute].
		/// Inherited members are not included.
		/// </summary>
		public static string GetDataMembersString<T>(T item, bool skipKey = true, bool skipTime = true)
		{
			var t = item == null ? typeof(T) : item.GetType();
			var ps = GetDataMemberProperties(item, skipKey);
			var sb = new StringBuilder();
			foreach (var p in ps)
			{
				var value = p.GetValue(item, null);
				if (value is null)
					continue;
				var defaultValue = p.PropertyType.IsValueType ? Activator.CreateInstance(p.PropertyType) : null;
				if (Equals(defaultValue, value))
					continue;
				if (skipTime && p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?))
					continue;
				if (p.PropertyType == typeof(string) && string.IsNullOrWhiteSpace((string)value))
					continue;
				if (p.Name.ToLower().Contains("checksum"))
					continue;
				sb.AppendFormat("{0}={1}", p.Name, value);
				sb.AppendLine();
			}
			return sb.ToString();
		}

		#endregion

		/// <summary>
		/// Get change state by comparing old and new values.
		/// </summary>
		/// <param name="type">Type of values.</param>
		/// <param name="oldValue">Old value.</param>
		/// <param name="newValue">New value.</param>
		/// <returns></returns>
		public static ChangeState GetValueChangeSet(Type type, object oldValue, object newValue)
		{
			var state = new ChangeState();
			state.ValueType = type;
			state.oldValue = oldValue;
			state.newValue = newValue;
			state.State = EntityState.Unchanged;
			if (oldValue != newValue)
				state.State = EntityState.Modified;
			var oldIsEmpty = oldValue is null || oldValue == Activator.CreateInstance(type);
			var newIsEmpty = newValue is null || newValue == Activator.CreateInstance(type);
			if (oldIsEmpty && !newIsEmpty)
				state.State = EntityState.Added;
			if (newIsEmpty && !oldIsEmpty)
				state.State = EntityState.Deleted;
			return state;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item1"></param>
		/// <param name="item2"></param>
		/// <returns></returns>
		public static EntityState GetClassChangeState(object item1, object item2)
		{
			var list = CompareProperties(item1, item2);
			var state = EntityState.Unchanged;
			var states = list.Select(x => x.State).Distinct().ToList();
			states.Remove(EntityState.Unchanged);
			if (states.Count == 0)
				return state;
			if (states.Count == 1)
				return states[0];
			return EntityState.Modified;
		}

		/// <summary>
		/// Compare all properties of two objects and return change state for every property and field.
		/// </summary>
		/// <param name="source">item1</param>
		/// <param name="target">item2</param>
		/// <returns></returns>
		public static List<ChangeState> CompareProperties(object source, object target)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));
			if (target is null)
				throw new ArgumentNullException(nameof(target));
			object oldValue;
			object newValue;
			ChangeState state;
			var list = new List<ChangeState>();
			var dstType = target.GetType();
			// Get Field Info.
			var sourceFields = GetFields(source.GetType());
			var targetFields = GetFields(target.GetType());
			foreach (var sf in sourceFields)
			{
				var tf = targetFields.FirstOrDefault(x => x.Name == sf.Name);
				if (tf == null || sf.FieldType != tf.FieldType)
					continue;
				oldValue = sf.GetValue(source);
				newValue = tf.GetValue(target);
				state = GetValueChangeSet(sf.FieldType, oldValue, newValue);
				list.Add(state);
			}
			// Get Property Info.
			var sourceProperties = GetProperties(source.GetType());
			var targetProperties = GetProperties(target.GetType());
			foreach (var sp in sourceProperties)
			{
				var tp = targetProperties.FirstOrDefault(x => x.Name == sp.Name);
				if (tp == null || sp.PropertyType != tp.PropertyType)
					continue;
				oldValue = sp.GetValue(source, null);
				newValue = tp.GetValue(target, null);
				state = GetValueChangeSet(sp.PropertyType, oldValue, newValue);
				list.Add(state);
			}
			return list;
		}

	}
}
