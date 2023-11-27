using System;
using System.Data;
#if NETFRAMEWORK
using System.Data;
#else
using Microsoft.EntityFrameworkCore;
#endif
using System.Linq;

namespace JocysCom.ClassLibrary.Runtime
{
	public class ChangeState
	{
		// Used by comparison.
		public Type ValueType;
		public object oldValue;
		public object newValue;
		public EntityState State;
		// Extra info for multivalue edit.
		public bool IsMultiValue
		{
			get { return MultiValues != null && MultiValues.Count() > 1; }
		}
		public object[] MultiValues;
	}
}
