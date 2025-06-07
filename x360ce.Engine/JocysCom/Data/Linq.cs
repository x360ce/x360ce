#if NETCOREAPP // .NET Core
#elif NETSTANDARD // .NET Standard
#else // .NET Framework
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace JocysCom.ClassLibrary.Data
{
	public class Linq
	{

		/// <summary>
		/// After dataContext.SubmitChanges() fails you can check details on which elements failed.
		/// Lists optimistic concurrency conflicts from a ChangeConflictCollection, showing original, current, and database values.
		/// </summary>
		/// <param name="ccc">Conflict collection (dataContext.ChangeConflicts).</param>
		/// <returns>String representation of conflicts.</returns>
		public static string ConflictsToString(ChangeConflictCollection ccc)
		{
			var sb = new StringBuilder();
			for (var oi = 0; oi < ccc.Count; oi++)
			{
				var occ = ccc[oi];
				sb.AppendFormat("---{0}\r\n", occ.Object.GetType().FullName);
				if (occ.Object != null)
					sb.AppendFormat(": {0}", occ.Object.ToString());
				for (var mi = 0; mi < occ.MemberConflicts.Count; mi++)
				{
					var mcc = occ.MemberConflicts[mi];
					var problem = mcc.OriginalValue != mcc.DatabaseValue ? " [db != or]" : "";
					sb.AppendFormat("{0}{1}\r\n", mcc.Member.Name, problem);
					sb.AppendFormat("    Original = {0}\r\n", FormatValue(mcc.OriginalValue));
					sb.AppendFormat("    Current = {0}\r\n", FormatValue(mcc.CurrentValue));
					sb.AppendFormat("    Database = {0}\r\n", FormatValue(mcc.DatabaseValue));
				}
			}
			return sb.ToString();
		}

		private static string FormatValue(object value)
		{
			if (value is null)
				return string.Empty;
			else if (value.GetType() == typeof(DateTime))
				return ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss.ffffffzzz");
			else
				return value.ToString();
		}

		/// <summary>Serializes a ChangeSet into a string listing inserted, updated, and deleted entities.</summary>
		/// <param name="cs">The ChangeSet containing inserts, updates, and deletes.</param>
		/// <returns>A string representation of the change set.</returns>
		public static string ChangesToString(ChangeSet cs)
		{
			var sb = new StringBuilder();
			sb.AppendFormat("---{0}\r\n", cs.ToString());
			sb.AppendFormat("\tInserts:\r\n");
			for (var i = 0; i < cs.Inserts.Count; i++)
				sb.AppendFormat("\t\t{0}\r\n", cs.Inserts[i].ToString());
			sb.AppendFormat("\tUpdates:\r\n");
			for (var i = 0; i < cs.Updates.Count; i++)
				sb.AppendFormat("\t\t{0}\r\n", cs.Updates[i].ToString());
			sb.AppendFormat("\tDeletes:\r\n");
			for (var i = 0; i < cs.Deletes.Count; i++)
				sb.AppendFormat("\t\t{0}\r\n", cs.Deletes[i].ToString());
			return sb.ToString();
		}

		/// <summary>
		/// Validate instance values. Useful before submitting data to SQL server with dataContext.SubmitChanges().
		/// </summary>
		/// <param name="cs">Changes which can be retrieved with context.GetChangeSet() method.</param>
		/// <exception cref="NullReferenceException">Thrown when a non-nullable property is null or string fields exceed database-defined length.</exception>
		/// <remarks>Validates ChangeSet entities against ColumnAttribute: throws if non-nullable props are null or strings exceed length constraints.</remarks>
		public static void ValidateChanges(ChangeSet cs)
		{
			for (var i = 0; i < cs.Inserts.Count; i++)
				CheckParams(cs.Inserts[i], false);
			for (var i = 0; i < cs.Updates.Count; i++)
				CheckParams(cs.Updates[i], false);
			for (var i = 0; i < cs.Deletes.Count; i++)
				CheckParams(cs.Deletes[i], false);
		}

		/// <summary>
		/// Get list of column attributes from instance.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static ColumnAttribute[] GetColumnsList(Type instanceType)
		{
			var list = new SortedDictionary<int, ColumnAttribute>();
			var properties = instanceType.GetProperties();
			foreach (var p in properties)
			{
				// Get attributes of ColumnAttribute type.
				IEnumerable<object> oa = p.GetCustomAttributes(false);
				// Convert results from object to ColumnAttribute.
				var ca = oa.Where(x => x is ColumnAttribute).Cast<ColumnAttribute>().FirstOrDefault();
				var ma = oa.Where(x => x is DataMemberAttribute).Cast<DataMemberAttribute>().FirstOrDefault();
				if (ca != null && ma != null)
					list.Add(ma.Order, ca);
			}
			return list.Select(x => x.Value).ToArray();
		}

		/// <summary>
		/// Throw error is value was not set for non nullable parameter.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="convertToEmpty">Convert null value to empty string</param>
		/// <exception cref="NullReferenceException">Thrown when a non-nullable property is null or string fields exceed database-defined length.</exception>
		/// <remarks>Validates ColumnAttribute.CanBeNull and string length, throwing exceptions on violations.</remarks>
		public static void CheckParams(object instance, bool convertToEmpty)
		{
			var properties = instance.GetType().GetProperties();
			foreach (var p in properties)
			{
				// Get attributes of ColumnAttribute type.
				var oa = p.GetCustomAttributes(false).Where(x => x.GetType() == typeof(ColumnAttribute));
				// Convert results from object to ColumnAttribute.
				var attributes = oa.Select<object, ColumnAttribute>(x => (ColumnAttribute)x).ToArray();
				foreach (var a in attributes)
				{
					if (!a.CanBeNull)
					{
						var propertyValue = p.GetValue(instance, null);
						if (propertyValue is null)
						{
							if (convertToEmpty)
							{
								p.SetValue(instance, string.Empty, null);
							}
							else
							{
								var msg = "The property '{0}' of {1} cannot be null";
								throw new System.NullReferenceException(string.Format(msg, p.Name, instance.GetType().FullName));
							}
						}
					}
					var t = a.DbType.ToLower();
					if (t.Contains("char"))
					{
						var index1 = t.IndexOf("(");
						var index2 = t.IndexOf(")");
						var dblen = t.Substring(index1 + 1, index2 - index1 - 1);
						var maxLength = 0;
						if (dblen == "max")
							maxLength = int.MaxValue;
						else
							int.TryParse(dblen, out maxLength);
						var propertyValue = p.GetValue(instance, null);
						if (propertyValue != null && propertyValue.ToString().Length > maxLength)
						{
							var msg = "The property '{0}' of {1} would be truncated";
							throw new System.NullReferenceException(string.Format(msg, p.Name, instance.GetType().FullName));
						}
					}
				}
			}
		}

		/// <summary>Filters a sequence by search terms in a query string, supporting quoted phrases and exclusion with leading '-'.</summary>
		/// <typeparam name="T">Type of elements.</typeparam>
		/// <param name="data">Sequence of items to search.</param>
		/// <param name="query">Search string containing terms optionally in quotes; terms prefixed with '-' exclude matches.</param>
		/// <param name="getText">Function to project an item to searchable text.</param>
		/// <returns>Filtered sequence containing items matching all include terms and none of the exclude terms.</returns>
		public static IEnumerable<T> ApplySearch<T>(IEnumerable<T> data, string query, Func<T, string> getText)
		{
			query = (query ?? "").Trim().ToUpper();
			if (string.IsNullOrEmpty(query))
				return data;
			// You can use quotes to look for words together.
			// word1 word2 "word3 word4" word5
			var re = new System.Text.RegularExpressions.Regex("(?<=\")[^\"]*(?=\")|[^\" ]+");
			var words = re.Matches(query).Cast<System.Text.RegularExpressions.Match>()
				.Select(m => m.Value.Trim())
				.Where(x=> !string.IsNullOrEmpty(x))
				.ToArray();
			if (words.Length == 0)
				return data;
			foreach (var word in words)
			{
				// If exclude then...
				data = word.Length > 1 && word.StartsWith("-")
					? data.Where(x => !getText(x).ToUpper().Contains(word.Substring(1)))
					: data.Where(x => getText(x).ToUpper().Contains(word));
			}
			return data;
		}

		/// <summary>Applies paging to an ordered query by skipping pageIndex * pageSize records and taking pageSize records.</summary>
		/// <typeparam name="T">Type of query elements.</typeparam>
		/// <param name="query">An IOrderedQueryable to page. Must be ordered to guarantee stable results.</param>
		/// <param name="pageSize">Number of records per page.</param>
		/// <param name="pageIndex">Zero-based page index.</param>
		/// <returns>IQueryable containing the specified page of results.</returns>
		public static IQueryable<T> ApplyPaging<T>(IOrderedQueryable<T> query, int pageSize, int pageIndex)
		{
			var max = pageSize * pageIndex;
			return query.Skip(max).Take(pageSize);
		}

		/// <summary>Applies paging to a query by skipping pageIndex * pageSize records and taking pageSize records.</summary>
		/// <typeparam name="T">Type of query elements.</typeparam>
		/// <param name="query">The queryable sequence to page.</param>
		/// <param name="pageSize">Number of records per page.</param>
		/// <param name="pageIndex">Zero-based page index.</param>
		/// <returns>IQueryable containing the specified page of results.</returns>
		public static IQueryable<T> ApplyPaging<T>(IQueryable<T> query, int pageSize, int pageIndex)
		{
			var max = pageSize * pageIndex;
			return query.Skip(max).Take(pageSize);
		}

		/// <summary>Applies paging to a query using an OrderedDictionary with optional 'RowsSkip', 'RowsTake', 'PageSize', and 'PageIndex' keys.</summary>
		/// <typeparam name="T">Type of query elements.</typeparam>
		/// <param name="query">The queryable sequence to page.</param>
		/// <param name="parameters">OrderedDictionary containing paging parameters.</param>
		/// <returns>IQueryable with Skip and Take applied based on present parameters.</returns>
		public static IQueryable<T> ApplyPaging<T>(IQueryable<T> query, OrderedDictionary parameters)
		{
			if (parameters.Contains("RowsSkip"))
				query = query.Skip((int)parameters["RowsSkip"]);
			if (parameters.Contains("RowsTake"))
				query = query.Take((int)parameters["RowsTake"]);
			if (parameters.Contains("PageSize"))
				query = query.Take((int)parameters["RowsTake"]);
			if (parameters.Contains("PageIndex"))
			{
				var rowsTake = parameters.Contains("RowsTake")
					? (int)parameters["RowsTake"]
					: (int)parameters["PageSize"];
				var rowsSkip = (int)parameters["PageIndex"] * rowsTake;
				query = query.Skip(rowsSkip);
			}
			return query;
		}

		/// <summary>
		/// Calculate number of pages.
		/// </summary>
		/// <param name="rowsTake">Number of row per page (page size).</param>
		/// <param name="rowsCount">Total number of rows: query.Count()</param>
		/// <returns></returns>
		/// <remarks>Calculates the total page count including any partial page.</remarks>
		public static int GetPageCount(int rowsTake, int rowsCount)
		{
			var pageCount = 0;
			if (rowsTake != 0 && rowsCount != 0)
			{
				var lastSize = rowsCount % rowsTake;
				pageCount = (rowsCount - lastSize) / rowsTake + Math.Min(1, lastSize);
			}
			return pageCount;
		}

		public static void TryToSubmit<T>(T db, string note)
		{
			var dc = (DataContext)(object)db;
			try
			{
				ValidateChanges(dc.GetChangeSet());
				dc.SubmitChanges();
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message + "\r\n" + JocysCom.ClassLibrary.Data.Linq.ConflictsToString(dc.ChangeConflicts));
			}
		}

		#region SQL DateTime

		/// <summary>
		/// Returns true if date time is in SqlDateTime range.
		/// </summary>
		/// <param name="dt"></param>
		/// <returns></returns>
		/// <remarks>true if dt is between SqlDateTime.MinValue and SqlDateTime.MaxValue; otherwise false.</remarks>
		public static bool SqlDateTimeIsValid(DateTime dt)
		{
			if (dt > System.Data.SqlTypes.SqlDateTime.MaxValue.Value)
				return false;
			if (dt < System.Data.SqlTypes.SqlDateTime.MinValue.Value)
				return false;
			return true;
		}

		public static DateTime SqlDateTimeTryParse(DateTime dt, DateTime isNullValue)
		{
			if (dt == null)
				return isNullValue;
			if (dt > System.Data.SqlTypes.SqlDateTime.MaxValue.Value)
				return System.Data.SqlTypes.SqlDateTime.MaxValue.Value;
			if (dt < System.Data.SqlTypes.SqlDateTime.MinValue.Value)
				return System.Data.SqlTypes.SqlDateTime.MinValue.Value;
			return dt;
		}

		#endregion

	}
}
#endif
