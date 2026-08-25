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

	}

}
