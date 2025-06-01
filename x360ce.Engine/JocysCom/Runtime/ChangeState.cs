using System;
#if NETFRAMEWORK
using System.Data;
#else
using Microsoft.EntityFrameworkCore;
#endif
using System.Linq;

namespace JocysCom.ClassLibrary.Runtime
{
    /// <summary>Represents a change state for a value or property, used by RuntimeHelper to compare and report differences.</summary>
    /// <remarks>Captures the value type, old and new values, resulting <see cref="EntityState"/>, and optional multi-value edits.</remarks>
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