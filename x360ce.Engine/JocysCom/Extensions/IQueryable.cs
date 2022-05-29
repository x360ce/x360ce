using JocysCom.ClassLibrary.ComponentModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Transactions;

namespace JocysCom.ClassLibrary.Extensions
{
	public static class System_Collections_Generic_IQueryable
	{
		public static BindingList<T> ToBindingList<T>(this IQueryable<T> source)
		{
			var rtn = new BindingList<T>();
			foreach (T obj in source) rtn.Add(obj);
			return rtn;
		}

		public static SortableBindingList<T> ToSortableBindingList<T>(this IQueryable<T> source)
		{
			return new SortableBindingList<T>(source);
		}


		public static List<T> ToListReadUncommitted<T>(this IQueryable<T> query)
		{
			// Declare the transaction options.
			var transactionOptions = new TransactionOptions();
			// Set it to read uncommitted.
			transactionOptions.IsolationLevel = IsolationLevel.ReadUncommitted;
			// Create the transaction scope, passing options in.
			using (var transactionScope = new TransactionScope(TransactionScopeOption.Required, transactionOptions))
			{
				var result = query.ToList();
				// The Complete method commits the transaction.
				// Transaction will be rolled back if an exception has been thrown and complete is not called.
				// "SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED" will be executed just before "COMMIT TRANSACTION" command.
				transactionScope.Complete();
				return result;
			}
		}

		public enum SqlIsolationLevel { None, ReadUncommitted, ReadCommitted, RepeatableRead, Serializable, Snapshot }
		const string selectIsolationLevel = "SELECT transaction_isolation_level FROM sys.dm_exec_sessions WHERE session_id = @@SPID";

		/*
		public static void ReportIsolationLevel(System.Data.Entity.DbContext context, object? prefix = null) {
			var level = context.Database.SqlQuery<short>(selectIsolationLevel).First();
			System.Diagnostics.Debug.WriteLine(string.Format("{0}Current Isolation level: {1} - {2}", prefix, level, (SqlIsolationLevel)level));
		}

		// Example
		public async Task<IEnumerable<MyDataItem>> GetData() {
			// Output all SQL queries to debug window.
			MyContext.Database.Log += x => System.Diagnostics.Debug.WriteLine(x);
			// Show database issolation level.
			ReportIsolationLevel(MyContext, 1);
			IEnumerable<MyDataItem> items;
			using (var transaction = MyContext.Database.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted)) {
				ReportIsolationLevel(MyContext, 2);
				// No need to track some read-only data.
				items = await MyContext.MyDataItems.AsNoTracking().ToListAsync();
				transaction.Commit();
			}
			// ReadUncommitted will remain, even outside of the scope.
			ReportIsolationLevel(MyContext, 3);
			return items;
		}

		*/

	}

}
